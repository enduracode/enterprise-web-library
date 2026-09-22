import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { execute } from './review.mjs';

function fixture(t) {
	const folder = fs.mkdtempSync(path.join(os.tmpdir(), 'ewl-review-test-'));
	t.after(() => fs.rmSync(folder, { recursive: true, force: true }));
	const repo = path.join(folder, 'repository with spaces'); fs.mkdirSync(repo);
	const git = (...args) => {
		const p = spawnSync('git', args, { cwd: repo, encoding: 'utf8', windowsHide: true });
		assert.equal(p.status, 0, p.stderr); return p.stdout.trim();
	};
	const write = (file, text) => fs.writeFileSync(path.join(repo, file), text);
	git('init', '-q');
	write('A.txt', 'keep\nold\nstay\ntail\n'); write('Deleted.txt', 'obsolete\nimplementation\n');
	git('add', '--', 'A.txt', 'Deleted.txt');
	git('-c', 'user.name=Review Test', '-c', 'user.email=test@example.invalid', 'commit', '-qm', 'Fixture baseline');
	const baseline = git('rev-parse', 'HEAD');
	write('A.txt', 'keep\nnew\nstay\nlast\n'); write('New.txt', 'brand new\nfunctionality\n');
	fs.unlinkSync(path.join(repo, 'Deleted.txt'));
	let document = `# Migration Review
## Setup
- Baseline: \`${baseline}\`
<!-- review-files:start -->
| Old path | Current path | Baseline lines | Current lines | Review item | File change |
|---|---|---|---|---|---|
| A.txt | A.txt | 2 | 2 | Feature | no |
| A.txt | A.txt | 4 | 4 | Other | no |
| - | New.txt | - | 1-2 | Feature | yes |
| Deleted.txt | - | 1-2 | - | Feature | yes |
<!-- review-files:end -->
<!-- review-exclusions:start -->
| Path | Reason |
|---|---|
<!-- review-exclusions:end -->
## Review item: Feature
Supporting references: Other (not in scope).
## Review item: Other
`;
	write('Migration Review.md', document);
	const invoke = (command = 'inspect', extra = {}) => execute({ command, repo, state: path.join(folder, 'state'), ...extra });
	return { repo, folder, git, write, invoke, setDocument: fn => { document = fn(document); write('Migration Review.md', document); } };
}

test('exact coverage, multi-file approval, references, persistence across commits', t => {
	const f = fixture(t);
	const index = f.git('ls-files', '--stage');
	assert.deepEqual(f.invoke().errors, []);
	f.invoke('approve', { item: 'Feature' });
	let report = f.invoke();
	assert.equal(report.items.find(i => i.name === 'Other').status, 'Pending');
	assert.equal(report.files.find(i => i.newPath === 'New.txt').ready, true);
	assert.equal(report.files.find(i => i.oldPath === 'Deleted.txt').ready, true);
	assert.equal(report.files.find(i => i.oldPath === 'A.txt').ready, false);
	assert.equal(f.git('ls-files', '--stage'), index);
	f.invoke('approve', { item: 'Other' });
	assert.ok(f.invoke().files.every(file => file.ready));
	f.git('add', '--', 'A.txt', 'Deleted.txt', 'New.txt');
	f.git('-c', 'user.name=Review Test', '-c', 'user.email=test@example.invalid', 'commit', '-qm', 'Reviewed migration');
	assert.ok(f.invoke().files.every(file => file.ready));
	f.write('A.txt', 'keep\nnewer\nstay\nlast\n');
	report = f.invoke();
	assert.equal(report.items.find(i => i.name === 'Feature').status, 'Changed since approval');
	assert.equal(report.items.find(i => i.name === 'Other').status, 'Approved');
	f.invoke('reopen', { item: 'Other' });
	assert.equal(f.invoke().items.find(i => i.name === 'Other').status, 'Pending');
});

test('index approval does not approve subsequent working-tree edits', t => {
	const f = fixture(t);
	f.git('add', '--', 'A.txt', 'Deleted.txt', 'New.txt');
	f.write('A.txt', 'keep\nnot approved\nstay\nlast\n');
	f.invoke('approve', { source: 'index', item: 'Feature' });
	assert.equal(f.invoke().items.find(i => i.name === 'Feature').status, 'Changed since approval');
	assert.equal(f.invoke('inspect', { source: 'index' }).items.find(i => i.name === 'Feature').status, 'Approved');
});

test('staged file regions persist without approving the rest of a multi-file item', t => {
	const f = fixture(t);
	f.git('add', '--', 'New.txt');
	let report = f.invoke('sync-staged');
	assert.equal(report.files.find(file => file.newPath === 'New.txt').ready, true);
	assert.equal(report.items.find(item => item.name === 'Feature').status, 'Pending');
	assert.equal(report.items.find(item => item.name === 'Other').status, 'Pending');
	f.git('-c', 'user.name=Review Test', '-c', 'user.email=test@example.invalid', 'commit', '-qm', 'Staged new file');
	assert.equal(f.invoke().files.find(file => file.newPath === 'New.txt').ready, true);
	f.git('add', '--', 'A.txt');
	f.write('A.txt', 'keep\nnewer\nstay\nlast\n');
	report = f.invoke('sync-staged');
	assert.equal(report.stagingDeferred.length, 1);
	assert.equal(report.files.find(file => file.newPath === 'A.txt').ready, false);
});

test('final newline changes invalidate approved content', t => {
	const f = fixture(t);
	f.invoke('approve', { item: 'Other' });
	f.write('A.txt', 'keep\nnew\nstay\nlast');
	assert.equal(f.invoke().items.find(item => item.name === 'Other').status, 'Changed since approval');
});

test('unassigned, duplicate, stale, and new untracked regions block readiness', t => {
	const f = fixture(t);
	f.setDocument(doc => doc.replace('| A.txt | A.txt | 4 | 4 | Other | no |\n', ''));
	assert.ok(f.invoke().errors.some(error => error.includes('Unassigned baseline lines: 4')));
	assert.throws(() => f.invoke('approve', { item: 'Feature' }), /coverage errors/);
	f.setDocument(doc => doc.replace('<!-- review-files:end -->', '| A.txt | A.txt | 2, 4 | 2, 4 | Other | no |\n<!-- review-files:end -->'));
	assert.ok(f.invoke().errors.some(error => error.includes('multiple owners')));
	f.setDocument(doc => doc.replace('| A.txt | A.txt | 2, 4 | 2, 4 | Other | no |', '| A.txt | A.txt | 4 | 4 | Other | no |'));
	f.write('Surprise.txt', 'unmapped addition\n');
	assert.ok(f.invoke().errors.some(error => error.includes('Surprise.txt')));
	f.setDocument(doc => doc.replace('<!-- review-exclusions:end -->', '| Surprise.txt | Unrelated fixture change. |\n<!-- review-exclusions:end -->'));
	assert.deepEqual(f.invoke().errors, []);
	f.setDocument(doc => doc.replace('| 2 | 2 | Feature |', '| 2 | 3 | Feature |'));
	assert.ok(f.invoke().errors.some(error => error.includes('not a changed line')));
});

test('navigation makes one workspace, persistent tabs, no deleted source recreation', t => {
	const f = fixture(t);
	const index = f.git('ls-files', '--stage');
	const report = f.invoke('prepare-diffs', { item: 'Feature' });
	assert.equal(report.navigation.diffs.length, 3);
	const workspace = JSON.parse(fs.readFileSync(report.navigation.workspace, 'utf8'));
	assert.equal(workspace.settings['workbench.editor.enablePreview'], false);
	const deletion = report.navigation.diffs.find(diff => diff.oldPath === 'Deleted.txt');
	assert.equal(fs.readFileSync(deletion.right).length, 0);
	assert.equal(fs.existsSync(path.join(f.repo, 'Deleted.txt')), false);
	const addition = report.navigation.diffs.find(diff => diff.newPath === 'New.txt');
	assert.equal(fs.readFileSync(addition.left).length, 0);
	assert.equal(addition.right, path.join(f.repo, 'New.txt'));
	assert.equal(f.git('ls-files', '--stage'), index);
	assert.ok(!fs.existsSync(path.join(f.repo, 'Migration Followup.md')));
});

test('empty files, binary files, and renames require file-level ownership', t => {
	const f = fixture(t);
	f.write('Empty.txt', ''); f.write('Binary.dat', Buffer.from([0, 1, 2, 3]));
	f.git('mv', 'A.txt', 'Renamed.txt');
	f.setDocument(doc => doc.replaceAll('| A.txt | A.txt |', '| A.txt | Renamed.txt |')
		.replace('| 2 | 2 | Feature | no |', '| 2 | 2 | Feature | yes |')
		.replace('<!-- review-files:end -->', '| - | Empty.txt | - | - | Feature | yes |\n| - | Binary.dat | - | - | Feature | yes |\n<!-- review-files:end -->'));
	assert.deepEqual(f.invoke().errors, []);
	f.invoke('approve', { item: 'Feature' });
	f.write('Binary.dat', Buffer.from([0, 1, 9, 3]));
	assert.equal(f.invoke().items.find(i => i.name === 'Feature').status, 'Changed since approval');
});

test('sibling approval storage retains content and staged parts without affecting coverage or the index', t => {
	const f = fixture(t);
	const approvalPath = path.join(f.repo, 'Migration Review.approvals.json');
	assert.equal(f.invoke().approvalsPath, approvalPath);
	assert.equal(fs.existsSync(approvalPath), false);
	f.invoke('approve', { item: 'Other' });
	f.git('add', '--', 'New.txt');
	const index = f.git('ls-files', '--stage');
	f.invoke('sync-staged');
	const stored = JSON.parse(fs.readFileSync(approvalPath, 'utf8'));
	assert.ok(stored.items.Other.content.some(part => part.current.some(line => line.text === 'last')));
	assert.ok(Object.values(stored.stagedParts).some(value => value.part.current.some(line => line.text === 'brand new')));
	assert.deepEqual(f.invoke().errors, []);
	assert.equal(f.git('ls-files', '--stage'), index);
	assert.equal(f.git('ls-files', '--', 'Migration Review.approvals.json'), '');
	f.setDocument(doc => doc.replace('Supporting references: Other (not in scope).', 'Reorganized file notes.'));
	assert.equal(f.invoke().items.find(item => item.name === 'Other').status, 'Approved');
});
