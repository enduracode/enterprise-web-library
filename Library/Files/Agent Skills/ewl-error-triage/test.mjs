import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import assert from 'node:assert/strict';
import { fileURLToPath } from 'node:url';

const root = fs.mkdtempSync(path.join(os.tmpdir(), 'ewl-mail-test-'));
const mailbox = path.join(root, 'errors.mbox');
const helper = fileURLToPath(new URL('./mailbox.mjs', import.meta.url));
const run = (command, ...args) => JSON.parse(execFileSync(process.execPath,
	[helper, command, mailbox, ...args], {encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe']}));
const message = (id, date, body, extra = '') => `From sender@example.com Thu Sep 10 00:00:00 2026\nMessage-ID: <${id}>\nDate: ${date}\nSubject: =?UTF-8?Q?Error_in_Test_System?=\n${extra}\n${body}\n`;
try {
	const recent = 'Thu, 10 Sep 2026 00:00:00 +0000';
	const old = message('old', 'Wed, 10 Sep 2025 00:00:00 +0000', 'old');
	const mime = message('mime', recent, '--b\nContent-Type: text/plain; charset=utf-8\nContent-Transfer-Encoding: base64\n\n' + Buffer.from('Exception: café\n at App.Save()').toString('base64') + '\n--b\nContent-Type: text/html\n\n<p>duplicate</p>\n--b--', 'Content-Type: multipart/alternative; boundary="b"\n');
	const qp = message('qp', recent, 'Exception: caf=C3=A9=\n continued', 'Content-Type: text/plain; charset=utf-8\nContent-Transfer-Encoding: quoted-printable\n');
	// Put a separator across the helper's overlapping block boundary.
	const padding = message('padding', 'Wed, 10 Sep 2025 00:00:00 +0000', 'x'.repeat(4 * 1024 * 1024 - 400));
	fs.writeFileSync(mailbox, old + padding + mime + qp + message('unknown', 'bad date', 'Unknown date') + mime);
	const scan = run('scan', '--since', '2025-09-11');
	assert.equal(scan.complete, true); assert.equal(scan.messagesScanned, 6); assert.equal(scan.indexed, 3); assert.equal(scan.unknownDates, 1);
	assert.equal(run('scan').messagesScanned, 6);
	const list = run('list', '--system', 'Test System');
	assert.equal(list.pending, 3);
	const ids = list.reports.map(r => r.id);
	const details = run('detail', '--ids', ids.join(','));
	assert(details.some(r => r.text.includes('café\n at App.Save()') && !r.text.includes('duplicate')));
	assert(details.some(r => r.text.includes('café continued')));
	run('mark', '--ids', ids[0], '--status', 'handled', '--note', 'test work item');
	assert.equal(run('list').pending, 2);
	assert.throws(() => run('mark', '--ids', ids[1] + ',unknown', '--status', 'handled'));
	assert.equal(run('list').pending, 2);
	fs.appendFileSync(mailbox, message('new', recent, 'New occurrence'));
	assert.throws(() => run('list'));
	assert.equal(run('scan', '--since', '2025-09-11').indexed, 4);
	assert.equal(run('list').pending, 3);
	run('mark', '--ids', ids[0], '--status', 'pending');
	assert.equal(run('list').pending, 4);
	assert.equal(run('list', '--system', '%').pending, 0);
	console.log('PASS: cutoff, MIME, UTF-8, duplicate IDs, block crossing, resume, source changes, atomic marking, reopening, literal filtering.');
} finally { fs.rmSync(root, {recursive: true, force: true}); }
