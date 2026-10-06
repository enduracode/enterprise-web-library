import { spawn } from 'node:child_process';
import { randomUUID } from 'node:crypto';
import { createWriteStream } from 'node:fs';
import { mkdir, readFile, realpath, rename, stat, writeFile } from 'node:fs/promises';
import net from 'node:net';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const help = `Headless OpenCode agent verification (Node 22 or newer).

node run.mjs run --exe <absolute executable> --dir <system directory>
  --model <provider/model> --title <scenario> --output <existing workspace>
  (--prompt <text> | --prompt-file <UTF-8 file>) [--agent build]
  [--timeout-ms 300000] [--config-file <OpenCode config>]
node run.mjs export --manifest <manifest.json>
node run.mjs cleanup --manifest <manifest.json>

run creates a unique artifact directory, captures stdout/stderr separately,
exports primary/child transcripts, and retains sessions for review.
export refreshes transcripts. cleanup deletes only recorded session trees via
the HTTP API and verifies removal. Artifacts and repository changes remain
caller-owned. No configuration is modified and no approvals are answered.`;

function options(args) {
	const [command, ...rest] = args;
	if (command === '--help' || !command) return { command: 'help' };
	if (!['run', 'export', 'cleanup'].includes(command)) throw new Error(`Unknown command: ${command}`);
	const allowed = command === 'run'
		? ['exe', 'dir', 'model', 'title', 'output', 'prompt', 'prompt-file', 'agent', 'timeout-ms', 'config-file']
		: ['manifest'];
	const result = { command };
	for (let i = 0; i < rest.length; i += 2) {
		const key = rest[i]?.replace(/^--/, '');
		if (!rest[i]?.startsWith('--') || !allowed.includes(key) || rest[i + 1] === undefined || key in result)
			throw new Error(`Invalid or duplicate option: ${rest[i]}`);
		result[key] = rest[i + 1];
	}
	return result;
}

async function existing(value, kind) {
	if (!value || !path.isAbsolute(value)) throw new Error(`Expected an absolute ${kind} path: ${value ?? '(missing)'}`);
	const resolved = await realpath(value);
	const info = await stat(resolved);
	if (kind === 'directory' ? !info.isDirectory() : !info.isFile()) throw new Error(`Not a ${kind}: ${resolved}`);
	return resolved;
}

function samePath(a, b) {
	if (!a || !b) return false;
	const normalize = p => process.platform === 'win32' ? path.resolve(p).toLowerCase() : path.resolve(p);
	return normalize(a) === normalize(b);
}

async function save(manifestPath, manifest) {
	const temporary = `${manifestPath}.${randomUUID()}.tmp`;
	await writeFile(temporary, `${JSON.stringify(manifest, null, 2)}\n`, { flag: 'wx', encoding: 'utf8' });
	await rename(temporary, manifestPath);
}

function environment(manifest) {
	return { ...process.env, ...(manifest.configFile ? { OPENCODE_CONFIG: manifest.configFile } : {}) };
}

async function killOwned(child) {
	if (child.exitCode !== null || child.signalCode !== null || !child.pid) return;
	if (process.platform === 'win32') {
		const code = await new Promise((resolve, reject) => {
			const killer = spawn('taskkill', ['/PID', String(child.pid), '/T', '/F'], { stdio: 'ignore', windowsHide: true });
			killer.once('error', reject);
			killer.once('close', resolve);
		});
		if (code !== 0 && child.exitCode === null && child.signalCode === null)
			throw new Error(`Could not terminate owned process tree ${child.pid}: taskkill exited ${code}.`);
	} else {
		try { process.kill(-child.pid, 'SIGKILL'); } catch (error) { if (error.code !== 'ESRCH') throw error; }
	}
}

export async function execute(exe, args, { directory, env, timeoutMs, stdout, stderr }) {
	const child = spawn(exe, args, {
		cwd: directory, env, stdio: ['ignore', 'pipe', 'pipe'], windowsHide: true,
		detached: process.platform !== 'win32',
	});
	let launchError;
	child.stdout.on('data', stdout);
	child.stderr.on('data', stderr);
	return await new Promise(resolve => {
		let settled = false;
		let timedOut = false;
		let terminationError;
		let terminating = false;
		const finish = () => {
			if (settled || terminating) return;
			settled = true;
			clearTimeout(timer);
			resolve({ exitCode: child.exitCode, signal: child.signalCode, timedOut, launchError, terminationError });
		};
		const timer = setTimeout(() => {
			timedOut = true;
			terminating = true;
			if (child.exitCode !== null || child.signalCode !== null)
				terminationError = 'CLI exited but inherited output handles remained open; descendants could not be identified for termination.';
			void killOwned(child).catch(error => { terminationError = error.message; }).finally(() => {
				child.stdout.destroy();
				child.stderr.destroy();
				child.unref();
				terminating = false;
				finish();
			});
		}, timeoutMs);
		child.once('error', error => { launchError = error.message; });
		child.once('close', finish);
	});
}

async function availablePort() {
	const server = net.createServer();
	await new Promise((resolve, reject) => { server.once('error', reject); server.listen(0, '127.0.0.1', resolve); });
	const port = server.address().port;
	await new Promise((resolve, reject) => server.close(error => error ? reject(error) : resolve()));
	return port;
}

async function withApi(manifest, operation) {
	const port = await availablePort();
	const child = spawn(manifest.executable, ['serve', '--hostname', '127.0.0.1', '--port', String(port)], {
		cwd: manifest.directory, env: environment(manifest), stdio: 'ignore', windowsHide: true,
		detached: process.platform !== 'win32',
	});
	let launchError;
	child.on('error', error => { launchError = error; });
	const authorization = process.env.OPENCODE_SERVER_PASSWORD
		? `Basic ${Buffer.from(`${process.env.OPENCODE_SERVER_USERNAME || 'opencode'}:${process.env.OPENCODE_SERVER_PASSWORD}`).toString('base64')}`
		: undefined;
	const request = async (route, method = 'GET', allowMissing = false, timeoutMs = 30000) => {
		const url = new URL(route, `http://127.0.0.1:${port}`);
		url.searchParams.set('directory', manifest.directory);
		const response = await fetch(url, {
			method, headers: authorization ? { Authorization: authorization } : {}, signal: AbortSignal.timeout(timeoutMs),
		});
		if (allowMissing && response.status === 404) return undefined;
		if (!response.ok) throw new Error(`${method} ${route}: HTTP ${response.status}`);
		return await response.json();
	};
	try {
		let ready = false;
		const deadline = Date.now() + 30000;
		while (Date.now() < deadline) {
			if (launchError) throw launchError;
			if (child.exitCode !== null) throw new Error(`Owned API server exited: ${child.exitCode}`);
			try { await request('/global/health', 'GET', false, 1000); ready = true; break; } catch {}
			await new Promise(resolve => setTimeout(resolve, 500));
		}
		if (!ready) throw new Error('Owned API server did not become ready within 30 seconds.');
		return await operation(request);
	} finally {
		await killOwned(child);
	}
}

function verifySession(info, manifest, parentID) {
	if (!/^ses_[A-Za-z0-9]+$/.test(info.id) || !samePath(info.directory, manifest.directory))
		throw new Error('Session does not belong to the recorded target directory.');
	if (parentID ? info.parentID !== parentID : info.parentID || info.title !== manifest.title)
		throw new Error(`Session ownership mismatch: ${info.id}`);
}

async function discover(request, manifest, manifestPath) {
	const recordedRoot = manifest.sessions.find(s => s.parentID === null);
	const known = recordedRoot ? await request(`/session/${recordedRoot.id}`, 'GET', true) : undefined;
	const roots = recordedRoot ? (known ? [known] : [])
		: (await request('/session')).filter(info => !info.parentID && info.title === manifest.title && samePath(info.directory, manifest.directory));
	if (roots.length > 1) throw new Error('Multiple roots match the unique run title; refusing ambiguous ownership.');
	if (!roots.length) {
		if (manifest.sessions.length) throw new Error('Recorded root is missing. Use cleanup to verify deletion.');
		return;
	}
	const visit = async (info, parentID) => {
		verifySession(info, manifest, parentID);
		const recorded = manifest.sessions.find(s => s.id === info.id);
		if (!recorded) {
			manifest.sessions.push({ id: info.id, parentID: info.parentID ?? null });
			await save(manifestPath, manifest);
		}
		const messages = await request(`/session/${info.id}/message`);
		await writeFile(path.join(path.dirname(manifestPath), `${info.id}.json`), `${JSON.stringify({ info, messages }, null, 2)}\n`, 'utf8');
		for (const child of await request(`/session/${info.id}/children`)) await visit(child, info.id);
	};
	await visit(roots[0]);
}

async function load(manifestPath) {
	const resolved = await existing(manifestPath, 'file');
	const manifest = JSON.parse(await readFile(resolved, 'utf8'));
	if (manifest.schema !== 1 || !manifest.title?.startsWith('scen-') || !Array.isArray(manifest.sessions))
		throw new Error('Not an agent-testing runner manifest.');
	await existing(manifest.executable, 'file');
	await existing(manifest.directory, 'directory');
	if (manifest.configFile) await existing(manifest.configFile, 'file');
	return { manifestPath: resolved, manifest };
}

async function run(opts) {
	const executable = await existing(opts.exe, 'file');
	const directory = await existing(opts.dir, 'directory');
	const workspace = await existing(opts.output, 'directory');
	if (!opts.model?.includes('/') || !opts.title) throw new Error('--model and --title are required.');
	if ((opts.prompt === undefined) === (opts['prompt-file'] === undefined)) throw new Error('Supply exactly one of --prompt and --prompt-file.');
	const prompt = opts.prompt ?? (await readFile(await existing(opts['prompt-file'], 'file'), 'utf8')).replace(/^\uFEFF/, '');
	if (!prompt.trim()) throw new Error('Prompt must not be empty.');
	const timeoutMs = Number(opts['timeout-ms'] ?? 300000);
	if (!Number.isSafeInteger(timeoutMs) || timeoutMs < 1 || timeoutMs > 2147483647) throw new Error('Invalid --timeout-ms.');
	const configFile = opts['config-file'] ? await existing(opts['config-file'], 'file') : undefined;
	const id = randomUUID();
	const artifacts = path.join(workspace, `agent-test-${id}`);
	await mkdir(artifacts);
	const manifestPath = path.join(artifacts, 'manifest.json');
	const manifest = {
		schema: 1, executable, directory, model: opts.model, agent: opts.agent ?? 'build',
		title: `scen-${opts.title}-${id}`, configFile, startedAt: new Date().toISOString(), sessions: [],
	};
	await save(manifestPath, manifest);
	console.log(`Manifest: ${manifestPath}`);
	const stdout = createWriteStream(path.join(artifacts, 'events.jsonl'), { flags: 'wx' });
	const stderr = createWriteStream(path.join(artifacts, 'stderr.log'), { flags: 'wx' });
	let streamError;
	for (const stream of [stdout, stderr]) stream.on('error', error => { streamError = error.message; });
	try {
		manifest.result = await execute(executable, [
			'run', '--format', 'json', '--model', manifest.model, '--agent', manifest.agent, '--title', manifest.title, '--', prompt,
		], { directory, env: environment(manifest), timeoutMs, stdout: data => stdout.write(data), stderr: data => stderr.write(data) });
	} finally {
		await Promise.all([stdout, stderr].map(stream => new Promise(resolve => {
			if (stream.destroyed) resolve(); else stream.end(resolve);
		})));
		manifest.finishedAt = new Date().toISOString();
		if (streamError) manifest.artifactError = streamError;
		await save(manifestPath, manifest);
	}
	try {
		await withApi(manifest, request => discover(request, manifest, manifestPath));
	} catch (error) {
		manifest.collectionError = error.message;
		await save(manifestPath, manifest);
	}
	console.log(JSON.stringify({ manifest: manifestPath, result: manifest.result, sessions: manifest.sessions, collectionError: manifest.collectionError, artifactError: manifest.artifactError }, null, 2));
	return manifest.result?.exitCode === 0 && !manifest.result.timedOut && !manifest.collectionError && !streamError && manifest.sessions.length ? 0 : 1;
}

async function cleanup(request, manifest, manifestPath) {
	// Refresh the ownership tree before deletion, including children from a timed-out run.
	const root = manifest.sessions.find(s => s.parentID === null);
	const current = root ? await request(`/session/${root.id}`, 'GET', true) : undefined;
	if (current || !manifest.sessions.length) await discover(request, manifest, manifestPath);
	for (const session of [...manifest.sessions].reverse()) {
		const info = await request(`/session/${session.id}`, 'GET', true);
		if (!info) continue;
		verifySession(info, manifest, session.parentID);
		await request(`/session/${session.id}`, 'DELETE');
	}
	for (const session of manifest.sessions) {
		if (await request(`/session/${session.id}`, 'GET', true)) throw new Error(`Session remains: ${session.id}`);
	}
	if ((await request('/session')).some(s => s.title === manifest.title)) throw new Error('Matching run session remains.');
	manifest.cleanedAt = new Date().toISOString();
	await save(manifestPath, manifest);
	console.log(`Deleted and verified ${manifest.sessions.length} recorded sessions. Artifacts retained at ${path.dirname(manifestPath)}.`);
}

export async function main(args = process.argv.slice(2)) {
	const opts = options(args);
	if (opts.command === 'help') { console.log(help); return 0; }
	if (opts.command === 'run') return await run(opts);
	const { manifest, manifestPath } = await load(opts.manifest);
	await withApi(manifest, request => opts.command === 'cleanup'
		? cleanup(request, manifest, manifestPath)
		: discover(request, manifest, manifestPath));
	if (opts.command === 'export') console.log(`Exported ${manifest.sessions.length} recorded sessions beside ${manifestPath}.`);
	return 0;
}

if (process.argv[1] && samePath(process.argv[1], fileURLToPath(import.meta.url))) {
	main().then(code => { process.exitCode = code; }).catch(error => { console.error(error.message); process.exitCode = 1; });
}
