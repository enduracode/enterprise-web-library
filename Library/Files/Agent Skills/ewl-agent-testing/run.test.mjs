import assert from 'node:assert/strict';
import { mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import test from 'node:test';
import { execute, main } from './run.mjs';

const fakeRun = `
const fs = require('node:fs');
const path = require('node:path');
const args = process.argv.slice(2);
const title = args[args.indexOf('--title') + 1];
const prompt = args.at(-1);
const directory = fs.realpathSync(process.cwd());
const root = { id: 'ses_Primary', title, directory };
const child = { id: 'ses_Child', parentID: root.id, title: 'Child', directory };
const grandchild = { id: 'ses_Grandchild', parentID: child.id, title: 'Grandchild', directory };
fs.writeFileSync('sessions.json', JSON.stringify([root, child, grandchild, { id: 'ses_Unrelated', title: 'Personal session', directory }]));
process.stdin.resume();
process.stdin.on('end', () => {
  if (prompt === 'hang') { setInterval(() => {}, 1000); return; }
  const bytes = Buffer.from(JSON.stringify({ type: 'text', sessionID: root.id, part: { text: 'café—你好' } }) + '\\n');
  process.stdout.write(bytes.subarray(0, bytes.length - 8));
  setTimeout(() => { process.stdout.write(bytes.subarray(bytes.length - 8)); process.stderr.write('diagnostic only\\n'); }, 10);
});
`;

const fakeServe = `
const fs = require('node:fs');
const http = require('node:http');
const args = process.argv.slice(2);
const port = Number(args[args.indexOf('--port') + 1]);
http.createServer((req, res) => {
  const url = new URL(req.url, 'http://localhost');
  let sessions = fs.existsSync('sessions.json') ? JSON.parse(fs.readFileSync('sessions.json')) : [];
  let result;
  if (url.pathname === '/global/health') result = { healthy: true };
  else if (url.pathname === '/session') result = sessions;
  else {
    const [, , id, action] = url.pathname.split('/');
    const info = sessions.find(s => s.id === id);
    if (!info) { res.writeHead(404); res.end('{}'); return; }
    if (req.method === 'DELETE') {
      const removed = new Set([id]);
      for (let i = 0; i < sessions.length; i++) for (const s of sessions) if (removed.has(s.parentID)) removed.add(s.id);
      sessions = sessions.filter(s => !removed.has(s.id));
      fs.writeFileSync('sessions.json', JSON.stringify(sessions));
      result = true;
    } else if (action === 'children') result = sessions.filter(s => s.parentID === id);
    else if (action === 'message') result = [{ info: { role: 'assistant' }, parts: [{ type: 'text', text: 'café—你好' }] }];
    else result = info;
  }
  res.writeHead(200, { 'content-type': 'application/json' });
  res.end(JSON.stringify(result));
}).listen(port, '127.0.0.1');
`;

async function fixture(t) {
	const directory = await mkdtemp(path.join(os.tmpdir(), 'ewl-agent-runner-'));
	t.after(() => rm(directory, { recursive: true, force: true }));
	await writeFile(path.join(directory, 'run'), fakeRun);
	await writeFile(path.join(directory, 'serve'), fakeServe);
	return directory;
}

async function runFixture(t, prompt, timeoutMs = 10000) {
	const directory = await fixture(t);
	const { readdir } = await import('node:fs/promises');
	const result = await main([
		'run', '--exe', process.execPath, '--dir', directory, '--output', directory,
		'--model', 'fixture/model', '--title', 'runner-test', '--prompt', prompt, '--timeout-ms', String(timeoutMs),
	]);
	const artifact = (await readdir(directory)).find(p => p.startsWith('agent-test-'));
	const manifestPath = path.join(directory, artifact, 'manifest.json');
	return { result, directory, artifacts: path.dirname(manifestPath), manifestPath, manifest: JSON.parse(await readFile(manifestPath, 'utf8')) };
}

test('stdin is closed and Unicode event bytes, descendant exports, and unrelated sessions are preserved', async t => {
	const run = await runFixture(t, 'finish');
	assert.equal(run.result, 0);
	assert.equal(run.manifest.result.timedOut, false);
	assert.deepEqual(run.manifest.sessions.map(s => s.id), ['ses_Primary', 'ses_Child', 'ses_Grandchild']);
	const event = JSON.parse(await readFile(path.join(run.artifacts, 'events.jsonl'), 'utf8'));
	assert.equal(event.part.text, 'café—你好');
	assert.equal(await readFile(path.join(run.artifacts, 'stderr.log'), 'utf8'), 'diagnostic only\n');
	const exported = JSON.parse(await readFile(path.join(run.artifacts, 'ses_Grandchild.json'), 'utf8'));
	assert.equal(exported.messages[0].parts[0].text, 'café—你好');
	const statePath = path.join(run.directory, 'sessions.json');
	const state = JSON.parse(await readFile(statePath));
	state.push({ id: 'ses_LateChild', parentID: 'ses_Primary', directory: run.directory, title: 'Late child' });
	await writeFile(statePath, JSON.stringify(state));
	assert.equal(await main(['export', '--manifest', run.manifestPath]), 0);
	assert.equal(await main(['cleanup', '--manifest', run.manifestPath]), 0);
	assert.deepEqual(JSON.parse(await readFile(statePath)).map(s => s.id), ['ses_Unrelated']);
	assert.equal(await main(['cleanup', '--manifest', run.manifestPath]), 0);
	assert.ok((await readFile(path.join(run.artifacts, 'events.jsonl'))).length);
});

test('watchdog bounds a blocked run and recovers ownership with no CLI events', async t => {
	const run = await runFixture(t, 'hang', 1000);
	assert.equal(run.result, 1);
	assert.equal(run.manifest.result.timedOut, true);
	assert.equal(await readFile(path.join(run.artifacts, 'events.jsonl'), 'utf8'), '');
	assert.equal(run.manifest.sessions.length, 3);
	await main(['cleanup', '--manifest', run.manifestPath]);
});

test('cleanup refuses a recorded session whose ownership does not match', async t => {
	const run = await runFixture(t, 'finish');
	run.manifest.sessions.push({ id: 'ses_Unrelated', parentID: null });
	await writeFile(run.manifestPath, JSON.stringify(run.manifest));
	await assert.rejects(main(['cleanup', '--manifest', run.manifestPath]), /ownership mismatch/);
	assert.equal(JSON.parse(await readFile(path.join(run.directory, 'sessions.json'))).length, 4);
	run.manifest.sessions.pop();
	await writeFile(run.manifestPath, JSON.stringify(run.manifest));
	await main(['cleanup', '--manifest', run.manifestPath]);
});

test('failed process launch produces a bounded result instead of hanging', async () => {
	const result = await execute(path.join(os.tmpdir(), 'missing-agent-runner-executable'), [], {
		directory: os.tmpdir(), env: process.env, timeoutMs: 1000, stdout() {}, stderr() {},
	});
	assert.match(result.launchError, /ENOENT/);
	assert.equal(result.timedOut, false);
});

test('inherited output pipes return promptly or are bounded by the watchdog', async t => {
	const output = [];
	let descendant;
	t.after(() => { if (descendant) { try { process.kill(descendant); } catch (error) { if (error.code !== 'ESRCH') throw error; } } });
	const result = await execute(process.execPath, ['-e', `
const { spawn } = require('node:child_process');
const child = spawn(process.execPath, ['-e', 'setInterval(() => {}, 1000)'], { stdio: ['ignore', 'inherit', 'inherit'] });
console.log(child.pid);
child.unref();
`], {
		directory: os.tmpdir(), env: process.env, timeoutMs: 1000, stdout(data) { output.push(data); }, stderr() {},
	});
	descendant = Number(Buffer.concat(output).toString().trim());
	assert.ok(Number.isSafeInteger(descendant) && descendant > 0);
	assert.equal(result.exitCode, 0);
	// Node closes inherited pipes promptly on Windows; other hosts can retain them.
	if (result.timedOut) assert.match(result.terminationError, /inherited output handles/);
	else assert.equal(result.terminationError, undefined);
});
