// Git review coverage and disposable approvals. Only the sibling approval JSON is written in the repository.
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import crypto from 'node:crypto';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const digest = value => crypto.createHash('sha256').update(value).digest('hex');
const normalize = bytes => bytes.toString('utf8').replace(/^\uFEFF/, '').replace(/\r\n/g, '\n');
function run(command, args, cwd, allowed = [0]) {
	const result = spawnSync(command, args, { cwd, windowsHide: true, maxBuffer: 64 * 1024 * 1024,
		env: { ...process.env, GIT_OPTIONAL_LOCKS: '0', LC_ALL: 'C' } });
	if (result.error || !allowed.includes(result.status))
		throw new Error(result.error?.message ?? `${command} failed: ${result.stderr}`);
	return result.stdout;
}
function argumentsFrom(argv) {
	const options = { command: argv[0], source: 'working' };
	for (let i = 1; i < argv.length; i += 2) {
		if (!argv[i].startsWith('--') || argv[i + 1] === undefined) throw new Error('Use --name value options.');
		options[argv[i].slice(2)] = argv[i + 1];
	}
	if (!['inspect', 'sync-staged', 'approve', 'reopen', 'prepare-diffs'].includes(options.command)) throw new Error('Unknown command.');
	if (!['working', 'index'].includes(options.source)) throw new Error('Source must be working or index.');
	return options;
}
function table(text, name, columns) {
	const start = `<!-- ${name}:start -->`, end = `<!-- ${name}:end -->`;
	if (text.split(start).length !== 2 || text.split(end).length !== 2) throw new Error(`Exactly one ${name} table is required.`);
	const body = text.split(start)[1].split(end)[0];
	return body.split(/\r?\n/).filter(line => line.trim().startsWith('|')).slice(2).map(line => {
		const cells = line.trim().slice(1, -1).split('|').map(cell => cell.trim().replace(/^`(.*)`$/, '$1'));
		if (cells.length !== columns) throw new Error(`Invalid ${name} row: ${line}`);
		return cells;
	});
}
function ranges(value) {
	if (value === '-') return [];
	return value.split(',').map(part => {
		const match = /^(\d+)(?:-(\d+))?$/.exec(part.trim());
		if (!match) throw new Error(`Invalid inclusive line range: ${value}`);
		const start = Number(match[1]), end = Number(match[2] ?? match[1]);
		if (start < 1 || end < start) throw new Error(`Invalid inclusive line range: ${value}`);
		return [start, end];
	});
}
function safePath(root, relative) {
	if (relative === '-') return null;
	const absolute = path.resolve(root, relative);
	if (path.isAbsolute(relative) || !absolute.startsWith(root + path.sep)) throw new Error(`Path outside repository: ${relative}`);
	return absolute;
}
function key(oldPath, newPath) { return `${oldPath}\0${newPath}`; }
function parseMap(text, root) {
	const baseline = /^- Baseline: `([a-f0-9]{40}|[a-f0-9]{64})`\s*$/m.exec(text)?.[1];
	if (!baseline) throw new Error('Setup needs - Baseline: `<full commit hash>`');
	const items = [...text.matchAll(/^## Review item: (.+)\r?$/gm)].map(match => match[1].trim());
	if (!items.length || new Set(items).size !== items.length) throw new Error('Review item titles must be unique and nonempty.');
	const rows = table(text, 'review-files', 6).map(([oldPath, newPath, old, current, item, file]) => {
		safePath(root, oldPath); safePath(root, newPath);
		if (!items.includes(item) || !['yes', 'no'].includes(file)) throw new Error(`Invalid review item/file-change owner: ${item}`);
		return { oldPath, newPath, old: ranges(old), current: ranges(current), item, file: file === 'yes' };
	});
	const excluded = new Map(table(text, 'review-exclusions', 2).map(([file, reason]) => {
		safePath(root, file);
		if (!reason || reason === '-') throw new Error(`Exclusion needs a reason: ${file}`);
		return [file, reason];
	}));
	return { baseline, items, rows, excluded };
}
function span(lines) {
	const sorted = [...lines].sort((a, b) => a - b), result = [];
	for (const n of sorted) {
		const last = result.at(-1);
		if (last && last[1] === n - 1) last[1] = n;
		else result.push([n, n]);
	}
	return result.map(([a, b]) => a === b ? `${a}` : `${a}-${b}`).join(', ') || '-';
}

export function execute(options) {
	options = { source: 'working', ...options };
	const requestedRoot = fs.realpathSync(options.repo ?? process.cwd());
	const git = (...args) => run('git', args, requestedRoot);
	const root = fs.realpathSync(git('rev-parse', '--show-toplevel').toString().trim());
	if (root !== requestedRoot) throw new Error('--repo must name the Git repository root.');
	if (git('ls-files', '-u').length) throw new Error('Resolve index conflicts before review coverage checks.');
	const document = path.join(root, 'Migration Review.md');
	const map = parseMap(fs.readFileSync(document, 'utf8'), root);
	git('cat-file', '-e', `${map.baseline}^{commit}`);
	const state = path.resolve(options.state ?? path.join(os.tmpdir(), 'opencode', 'migration-review', digest(root + '\0' + map.baseline)));
	if (state === root || state.startsWith(root + path.sep)) throw new Error('Navigation state must be outside the repository.');
	const identity = { version: 1, repository: root, baseline: map.baseline };
	const approvalsPath = path.join(root, 'Migration Review.approvals.json');
	let approvals = { ...identity, items: {}, stagedParts: {} };
	const originalApprovalText = fs.existsSync(approvalsPath) ? fs.readFileSync(approvalsPath, 'utf8') : null;
	if (fs.existsSync(approvalsPath)) {
		approvals = JSON.parse(originalApprovalText);
		if (JSON.stringify(identity) !== JSON.stringify({ version: approvals.version, repository: approvals.repository, baseline: approvals.baseline }))
			throw new Error('Approval state belongs to another repository/baseline or schema.');
	}
	approvals.stagedParts ??= {};
	const scratch = fs.mkdtempSync(path.join(os.tmpdir(), 'migration-diff-'));
	try {
		const memo = new Map();
		function blob(version, relative) {
			if (relative === '-') return { bytes: Buffer.alloc(0), mode: '-' };
			const id = version + '\0' + relative;
			if (memo.has(id)) return memo.get(id);
			let entry;
			if (version === 'working') {
				const absolute = safePath(root, relative);
				if (!fs.existsSync(absolute)) entry = { bytes: Buffer.alloc(0), mode: '-' };
				else {
					const stat = fs.lstatSync(absolute);
					if (!stat.isFile()) throw new Error(`Unsupported working-tree resource: ${relative}`);
					const indexed = blob('index', relative);
					const mode = process.platform === 'win32' ? (indexed.mode === '-' ? '100644' : indexed.mode) : (stat.mode & 0o111 ? '100755' : '100644');
					entry = { bytes: fs.readFileSync(absolute), mode };
				}
			} else {
				const record = version === 'index' ? git('ls-files', '--stage', '-z', '--', relative).toString() : git('ls-tree', '-z', version, '--', relative).toString();
				const first = record.split('\0').find(value => value.slice(value.indexOf('\t') + 1) === relative);
				if (!first) entry = { bytes: Buffer.alloc(0), mode: '-' };
				else {
					const fields = first.split('\t')[0].split(' '), mode = fields[0];
					if (!['100644', '100755'].includes(mode)) throw new Error(`Unsupported Git resource mode ${mode}: ${relative}`);
					entry = { bytes: git('cat-file', 'blob', fields[version === 'index' ? 1 : 2]), mode };
				}
			}
			memo.set(id, entry);
			return entry;
		}
		const parts = git('diff', '--no-ext-diff', '--no-textconv', '--name-status', '-z', '--find-renames', ...(options.source === 'index' ? ['--cached'] : []), map.baseline, '--').toString().split('\0').filter(Boolean);
		const changes = [];
		for (let i = 0; i < parts.length;) {
			const status = parts[i++], first = parts[i++];
			if (!first) throw new Error('Invalid Git change inventory.');
			const oldPath = status[0] === 'A' ? '-' : first;
			const newPath = status[0] === 'D' ? '-' : status[0] === 'R' ? parts[i++] : first;
			changes.push({ oldPath, newPath, status });
		}
		if (options.source === 'working')
			for (const file of git('ls-files', '--others', '--exclude-standard', '-z').toString().split('\0').filter(Boolean))
				changes.push({ oldPath: '-', newPath: file, status: 'A' });
		const automaticExclusions = new Set(['Migration Review.md', 'Migration Followup.md', 'Migration Review.approvals.json']);
		const relevant = changes.filter(change => ![change.oldPath, change.newPath].filter(file => file !== '-').every(file => automaticExclusions.has(file)) &&
			![change.oldPath, change.newPath].filter(file => file !== '-').every(file => map.excluded.has(file)));
		const errors = [], itemParts = new Map(map.items.map(item => [item, []])), files = [];
		const encountered = new Set(), invalidItems = new Set();
		for (const change of relevant) {
			const id = key(change.oldPath, change.newPath);
			encountered.add(id);
			const before = blob(map.baseline, change.oldPath), after = blob(options.source, change.newPath);
			let binary = before.bytes.includes(0) || after.bytes.includes(0);
			try { for (const bytes of [before.bytes, after.bytes]) new TextDecoder('utf-8', { fatal: true }).decode(bytes); }
			catch { binary = true; }
			const oldLines = new Map(), newLines = new Map();
			if (!binary) {
				const left = path.join(scratch, 'old'), right = path.join(scratch, 'new');
				fs.writeFileSync(left, normalize(before.bytes)); fs.writeFileSync(right, normalize(after.bytes));
				const diff = run('git', ['-c', 'core.autocrlf=false', 'diff', '--no-index', '--no-ext-diff', '--no-textconv', '--unified=0', '--', left, right], scratch, [0, 1]).toString();
				let old = 0, current = 0, inHunk = false, lastMap, lastLine;
				for (const line of diff.split('\n')) {
					const match = /^@@ -(\d+)(?:,\d+)? \+(\d+)(?:,\d+)? @@/.exec(line);
					if (match) { old = Number(match[1]); current = Number(match[2]); inHunk = true; }
					else if (inHunk && line.startsWith('-')) { lastMap = oldLines; lastLine = old++; oldLines.set(lastLine, { text: line.slice(1), noFinalNewline: false }); }
					else if (inHunk && line.startsWith('+')) { lastMap = newLines; lastLine = current++; newLines.set(lastLine, { text: line.slice(1), noFinalNewline: false }); }
					else if (inHunk && line.startsWith('\\ No newline') && lastMap) lastMap.get(lastLine).noFinalNewline = true;
					else if (inHunk && line.startsWith(' ')) { old++; current++; }
				}
			}
			// A file-level owner covers existence, renames, mode, binary bytes, and final-newline-only changes.
			const fileChange = change.oldPath !== change.newPath || before.mode !== after.mode || binary ||
				(!oldLines.size && !newLines.size && digest(before.bytes) !== digest(after.bytes));
			const rows = map.rows.filter(row => key(row.oldPath, row.newPath) === id);
			const claimedOld = new Map(), claimedNew = new Map(), owners = new Set();
			const fileErrors = [];
			function claim(selection, actual, claims, side, item) {
				const content = [];
				for (const [start, end] of selection) {
					if (end - start > 1000000) throw new Error('Unreasonably large mapped range.');
					for (let line = start; line <= end; line++) {
						if (!actual.has(line)) fileErrors.push(`${side} line ${line} is not a changed line (${item}); refresh mapping.`);
						if (claims.has(line)) fileErrors.push(`${side} line ${line} has multiple owners: ${claims.get(line)}, ${item}.`);
						claims.set(line, item); content.push(actual.get(line));
					}
				}
				return content;
			}
			for (const row of rows) {
				owners.add(row.item);
				const old = claim(row.old, oldLines, claimedOld, 'baseline', row.item);
				const current = claim(row.current, newLines, claimedNew, 'current', row.item);
				if (row.file && !fileChange) fileErrors.push(`No file-level change to assign to ${row.item}.`);
				itemParts.get(row.item).push({ oldPath: change.oldPath, newPath: change.newPath, oldRanges: row.old, currentRanges: row.current, old, current,
					...(row.file ? { file: { beforeMode: before.mode, afterMode: after.mode,
						...(binary || (!oldLines.size && !newLines.size) ? { before: digest(before.bytes), after: digest(after.bytes) } : {}) } } : {}) });
			}
			const missingOld = [...oldLines.keys()].filter(line => !claimedOld.has(line));
			const missingNew = [...newLines.keys()].filter(line => !claimedNew.has(line));
			if (missingOld.length) fileErrors.push(`Unassigned baseline lines: ${span(missingOld)}`);
			if (missingNew.length) fileErrors.push(`Unassigned current lines: ${span(missingNew)}`);
			if (fileChange && rows.filter(row => row.file).length !== 1) fileErrors.push('File-level change needs exactly one owner.');
			for (const error of fileErrors) errors.push(`${change.oldPath} -> ${change.newPath}: ${error}`);
			if (fileErrors.length) for (const owner of owners) invalidItems.add(owner);
			files.push({ ...change, baselineLines: span(oldLines.keys()), currentLines: span(newLines.keys()), fileChange,
				items: [...owners], errors: fileErrors });
		}
		for (const row of map.rows)
			if (!encountered.has(key(row.oldPath, row.newPath))) {
				errors.push(`Stale or excluded file-index row: ${row.oldPath} -> ${row.newPath} (${row.item}).`);
				invalidItems.add(row.item);
			}
		const signatures = Object.fromEntries([...itemParts].map(([item, parts]) => [item, digest(JSON.stringify(parts.sort((a, b) => JSON.stringify(a).localeCompare(JSON.stringify(b)))))]));
		const partKey = (name, part) => digest(JSON.stringify({ name, part }));
		const stagedPaths = git('diff', '--cached', '--name-only', '-z', '--').toString().split('\0').filter(Boolean);
		const stagingDeferred = [];
		function save() {
			const lock = approvalsPath + '.lock';
			fs.mkdirSync(lock); // Refuse concurrent writers rather than lose another review decision.
			try {
				const latest = fs.existsSync(approvalsPath) ? fs.readFileSync(approvalsPath, 'utf8') : null;
				if (latest !== originalApprovalText) throw new Error('Approval state changed during this command; inspect again.');
				const temp = approvalsPath + '.' + crypto.randomUUID();
				fs.writeFileSync(temp, JSON.stringify(approvals, null, 2)); fs.renameSync(temp, approvalsPath);
			} finally { fs.rmdirSync(lock); }
		}
		if (['approve', 'reopen', 'prepare-diffs'].includes(options.command) && !map.items.includes(options.item)) throw new Error('Supply --item with an exact review-item title.');
		if (options.command === 'approve') {
			if (errors.length) throw new Error('Fix coverage errors before approving: ' + errors.join('\n'));
			if (!itemParts.get(options.item).length) throw new Error('Review item has no owned changes.');
			approvals.items[options.item] = { signature: signatures[options.item], source: options.source,
				approvedAt: new Date().toISOString(), content: itemParts.get(options.item) };
			save();
		}
		if (options.command === 'sync-staged') {
			if (options.source !== 'working' || errors.length) throw new Error('Staging synchronization requires valid working-copy coverage.');
			const equal = (a, b) => a.mode === b.mode && digest(a.bytes) === digest(b.bytes);
			for (const file of files) {
				if (![file.oldPath, file.newPath].some(p => stagedPaths.includes(p))) continue;
				const oldHead = blob('HEAD', file.oldPath), oldBase = blob(map.baseline, file.oldPath);
				const newHead = blob('HEAD', file.newPath), newBase = blob(map.baseline, file.newPath);
				// Only infer full-file approval when the ENTIRE baseline change is in the index.
				// Partial staging and earlier unobserved commits need human scope verification.
				if (!equal(oldHead, oldBase) || !equal(newHead, newBase) ||
					!equal(blob('index', file.newPath), blob('working', file.newPath)) ||
					!equal(blob('index', file.oldPath), blob('working', file.oldPath))) {
					stagingDeferred.push(`${file.oldPath} -> ${file.newPath}: partial staging, later edits, or earlier commits require scope verification.`);
					continue;
				}
				for (const [name, parts] of itemParts) for (const part of parts)
					if (part.oldPath === file.oldPath && part.newPath === file.newPath)
						approvals.stagedParts[partKey(name, part)] = { name, part, source: 'index', approvedAt: new Date().toISOString() };
			}
			save();
		}
		if (options.command === 'reopen') {
			delete approvals.items[options.item];
			for (const [id, approval] of Object.entries(approvals.stagedParts)) if (approval.name === options.item) delete approvals.stagedParts[id];
			save();
		}
		const partApproved = (name, part) => approvals.items[name]?.signature === signatures[name] || !!approvals.stagedParts[partKey(name, part)];
		const items = map.items.map(name => ({ name, coverageValid: !invalidItems.has(name), status: !approvals.items[name] ? 'Pending' :
			approvals.items[name].signature === signatures[name] ? 'Approved' : 'Changed since approval',
			approvedRegions: itemParts.get(name).filter(part => partApproved(name, part)).length, totalRegions: itemParts.get(name).length }));
		for (const item of items) if (item.totalRegions && item.approvedRegions === item.totalRegions) item.status = 'Approved';
		for (const file of files) file.ready = !file.errors.length && file.items.length > 0 &&
			file.items.every(name => !invalidItems.has(name) && itemParts.get(name)
				.filter(part => part.oldPath === file.oldPath && part.newPath === file.newPath).every(part => partApproved(name, part)));
		const report = { ...identity, source: options.source, state, approvalsPath, errors, items, files,
			exclusions: Object.fromEntries(map.excluded), stagedPaths, stagingDeferred };
		if (options.command === 'prepare-diffs') {
			if (errors.length) throw new Error('Fix coverage errors before opening a review item: ' + errors.join('\n'));
			const view = path.join(state, 'views', crypto.randomUUID()); fs.mkdirSync(view, { recursive: true });
			const selected = files.filter(file => file.items.includes(options.item));
			const diffs = selected.map((file, i) => {
				const name = path.basename(file.newPath === '-' ? file.oldPath : file.newPath);
				const left = path.join(view, `${i}-baseline`, name); fs.mkdirSync(path.dirname(left), { recursive: true });
				fs.writeFileSync(left, blob(map.baseline, file.oldPath).bytes);
				fs.chmodSync(left, 0o444);
				let right = safePath(root, file.newPath);
				if (options.source === 'index' || file.newPath === '-') {
					right = path.join(view, `${i}-${options.source}`, name); fs.mkdirSync(path.dirname(right), { recursive: true });
					fs.writeFileSync(right, blob(options.source, file.newPath).bytes);
					fs.chmodSync(right, 0o444);
				}
				return { oldPath: file.oldPath, newPath: file.newPath, left, right };
			});
			const workspace = path.join(view, 'Migration Review.code-workspace');
			const windowTitle = `Migration review: ${options.item} [${path.basename(view)}]`;
			fs.writeFileSync(workspace, JSON.stringify({ folders: [{ path: root }], settings: {
				'workbench.editor.enablePreview': false, 'workbench.editor.enablePreviewFromQuickOpen': false,
				'window.title': windowTitle, 'diffEditor.renderSideBySide': true,
				'files.readonlyInclude': { [`${view.replaceAll('\\', '/')}/**`]: true }
			} }, null, 2));
			report.navigation = { item: options.item, workspace, windowTitle, diffs };
		}
		return report;
	} finally { fs.rmSync(scratch, { recursive: true, force: true }); }
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
	try { console.log(JSON.stringify(execute(argumentsFrom(process.argv.slice(2))), null, 2)); }
	catch (error) { console.error(error.message); process.exitCode = 1; }
}
