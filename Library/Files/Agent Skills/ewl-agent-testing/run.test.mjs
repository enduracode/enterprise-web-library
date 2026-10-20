import assert from 'node:assert/strict';
import { mkdtemp, readFile, readdir, rm, writeFile } from 'node:fs/promises';
import http from 'node:http';
import os from 'node:os';
import path from 'node:path';
import test from 'node:test';
import { connection, main } from './run.mjs';

test('rejects non-loopback destinations and embedded credentials', () => {
  for (const url of ['http://example.com', 'http://user:pass@127.0.0.1', 'http://127.0.0.1/path', 'http://127.0.0.1/?x=1'])
    assert.throws(() => connection(url, 'secret'), /loopback/);
  assert.throws(() => connection('http://127.0.0.1', ''), /password/);
});

for (const scenario of ['normal', 'timeout', 'interrupt failure', 'export failure', 'partial deletion']) {
test(`permanent-server runner preserves ownership and recovery: ${scenario}`, async t => {
  const directory = await mkdtemp(path.join(os.tmpdir(), 'ewl-runner-'));
  t.after(() => rm(directory, { recursive: true, force: true }));
  const credential = path.join(directory, 'connection.json');
  const prompt = path.join(directory, 'prompt.txt');
  await writeFile(credential, JSON.stringify({ password: 'test-secret-not-for-manifest' }));
  await writeFile(prompt, 'Unicode café—你好');
  let sessions = [{ id: 'ses_Unrelated', title: 'Unrelated', location: { directory } }];
  let noAuthentication = 0;
  let interrupted = false;
  let exportFailed = false;
  let deletionFailed = false;
  const deleted = [];
  const server = http.createServer(async (req, res) => {
    if (req.headers.authorization !== 'Basic ' + Buffer.from('opencode:test-secret-not-for-manifest').toString('base64')) {
      noAuthentication++; res.writeHead(401).end(); return;
    }
    const url = new URL(req.url, 'http://127.0.0.1');
    const bytes = [];
    for await (const chunk of req) bytes.push(chunk);
    const text = Buffer.concat(bytes).toString();
    const body = text ? JSON.parse(text) : undefined;
    let result;
    if (url.pathname === '/api/info') result = { version: '2.0.32' };
    else if (url.pathname === '/api/session' && req.method === 'POST') {
      const info = { id: 'ses_Primary', ...body };
      sessions.push(info, ...['A', 'B'].map(n => ({ id: `ses_Child${n}`, parentID: info.id, location: { directory } })));
      result = { data: info };
    } else if (url.pathname === '/api/session') {
      // Deliberately paginate the two children to catch silent first-page-only exports.
      const cursor = url.searchParams.get('cursor');
      const children = sessions.filter(s => s.parentID === (url.searchParams.get('parentID') ?? cursor));
      result = { data: cursor ? children.slice(1) : children.slice(0, 1), cursor: { next: !cursor && children.length > 1 ? url.searchParams.get('parentID') : null } };
    } else {
      const id = url.pathname.split('/').find(s => s.startsWith('ses_'));
      const info = sessions.find(s => s.id === id);
      if (!info) { res.writeHead(404).end('{}'); return; }
      if (req.method === 'DELETE') {
        if (scenario === 'partial deletion' && id === 'ses_ChildA' && !deletionFailed) {
          deletionFailed = true; res.writeHead(503).end('{}'); return;
        }
        deleted.push(id); sessions = sessions.filter(s => s.id !== id); res.writeHead(204).end(); return;
      }
      if (url.pathname.endsWith('/wait')) {
        if (scenario === 'timeout' || scenario === 'interrupt failure') return;
        res.writeHead(204).end(); return;
      }
      if (url.pathname.endsWith('/interrupt')) {
        interrupted = true;
        res.writeHead(scenario === 'interrupt failure' ? 503 : 204).end(); return;
      }
      if (url.pathname.endsWith('/export') && scenario === 'export failure' && id === 'ses_ChildA' && !exportFailed) {
        exportFailed = true; res.writeHead(503).end('{}'); return;
      }
      result = url.pathname.endsWith('/export') ? { data: { info, messages: [{ type: 'user', text: 'café—你好' }] } } : { data: info };
    }
    res.writeHead(200, { 'Content-Type': 'application/json' }).end(JSON.stringify(result));
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  t.after(() => new Promise(resolve => server.close(resolve)));
  const run = () => main(['run', '--server', `http://127.0.0.1:${server.address().port}`, '--connection-file', credential,
    '--dir', directory, '--model', 'fixture/model', '--title', 'runner', '--output', directory, '--prompt-file', prompt,
    '--timeout-ms', '100']);
  if (scenario === 'timeout') await assert.rejects(run(), /timeout|timed out/i);
  else if (scenario === 'interrupt failure') await assert.rejects(run(), /interrupt: HTTP 503/);
  else if (scenario === 'export failure') await assert.rejects(run(), /export: HTTP 503/);
  else await run();
  const artifacts = (await readdir(directory)).find(n => n.startsWith('agent-test-'));
  const file = path.join(directory, artifacts, 'manifest.json');
  const manifestText = await readFile(file, 'utf8');
  assert.ok(!manifestText.includes('test-secret-not-for-manifest'));
  let manifest = JSON.parse(manifestText);
  assert.deepEqual(deleted, []);
  assert.equal(sessions.length, 4);
  assert.equal(interrupted, scenario === 'timeout' || scenario === 'interrupt failure');
  if (interrupted) assert.match(manifest.failure, /timeout|timed out/i);
  if (scenario === 'export failure') {
    assert.equal(exportFailed, true);
    assert.ok(manifest.sessions.some(s => s.id === 'ses_ChildA'));
    // Retry must rediscover the remainder of the tree after a mid-export failure.
    await main(['export', '--manifest', file]);
    manifest = JSON.parse(await readFile(file, 'utf8'));
  }
  assert.equal(manifest.sessions.length, 3);
  const exported = JSON.parse(await readFile(path.join(directory, artifacts, 'ses_ChildB.json'), 'utf8'));
  assert.equal(exported.data.messages[0].text, 'café—你好');
  manifest.sessions.push({ id: 'ses_Unrelated', parentID: null });
  await writeFile(file, JSON.stringify(manifest));
  await assert.rejects(main(['cleanup', '--manifest', file]), /ownership mismatch/);
  assert.equal(sessions.length, 4);
  manifest.sessions.pop();
  await writeFile(file, JSON.stringify(manifest));
  if (scenario === 'partial deletion') {
    await assert.rejects(main(['cleanup', '--manifest', file]), /HTTP 503/);
    assert.deepEqual(deleted, ['ses_ChildB']);
    assert.ok(sessions.some(s => s.id === 'ses_Primary'));
    assert.ok(!JSON.parse(await readFile(file, 'utf8')).cleanedAt);
  }
  await main(['cleanup', '--manifest', file]);
  await main(['cleanup', '--manifest', file]);
  assert.deepEqual(sessions.map(s => s.id), ['ses_Unrelated']);
  assert.equal(noAuthentication, 0);
});
}
