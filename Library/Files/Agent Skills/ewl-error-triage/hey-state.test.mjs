import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const state = fs.mkdtempSync(path.join(os.tmpdir(), 'ewl-hey-state-'));
const helper = fileURLToPath(new URL('./hey-state.mjs', import.meta.url));
const cleanEnvironment = { ...process.env };
for (const key of Object.keys(cleanEnvironment))
	if (key.toLowerCase() === 'ewlfolderpath') delete cleanEnvironment[key];
const invoke = (args, env) => JSON.parse(execFileSync(process.execPath, [helper, ...args],
	{ encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'], env }));
const run = (command, mailbox, ...args) => JSON.parse(execFileSync(process.execPath,
	[helper, command, '--state', state, '--mailbox', mailbox, ...args], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }));
try {
	const platformRoot = process.platform === 'win32' ? 'C:\\Enterprise Web Library' : '/opt/enterprise-web-library';
	assert.equal(invoke(['location'], cleanEnvironment).databasePath,
		path.join(platformRoot, 'Error Triage', 'HEY', 'hey-state.sqlite'));
	const ewlFolder = path.join(state, 'Custom EWL – local data');
	const environment = { ...cleanEnvironment, EwlFolderPath: ewlFolder };
	const expectedDirectory = path.join(ewlFolder, 'Error Triage', 'HEY');
	assert.equal(invoke(['location'], environment).stateDirectory, expectedDirectory);
	assert.equal(fs.existsSync(ewlFolder), false); // Location discovery must not create data.
	invoke(['record', '--mailbox', 'environment-test', '--topic', '1', '--message', '2'], environment);
	assert.equal(fs.existsSync(path.join(expectedDirectory, 'hey-state.sqlite')), true);
	assert.equal(invoke(['list', '--mailbox', 'environment-test'], environment).total, 1);
	const override = path.join(state, 'explicit override');
	assert.equal(invoke(['location', '--state', override], environment).stateDirectory, override);
	invoke(['record', '--state', override, '--mailbox', 'override-test', '--topic', '1', '--message', '2'], environment);
	assert.equal(invoke(['list', '--mailbox', 'override-test'], environment).total, 0);
	assert.equal(invoke(['list', '--state', override, '--mailbox', 'override-test'], environment).total, 1);
	assert.throws(() => invoke(['location'], { ...cleanEnvironment, EwlFolderPath: 'relative-folder' }));
	assert.throws(() => invoke(['location'], { ...cleanEnvironment, EwlFolderPath: '' }));
	assert.throws(() => invoke(['location', '--state', ''], environment));
	for (const mailbox of ['owner-a/account-1', 'owner-b/account-1'])
		run('record', mailbox, '--topic', '10', '--message', '100');
	const owner = 'owner-a/account-1';
	run('mark', owner, '--messages', '100', '--status', 'handled', '--note', 'Work item created');
	run('record', owner, '--topic', '10', '--message', '100');
	assert.equal(run('list', owner).total, 0);
	assert.equal(run('list', 'owner-b/account-1').total, 1);
	run('record', owner, '--topic', '10', '--message', '101');
	assert.equal(run('list', owner).total, 1); // A new entry in the same topic is new work.
	assert.throws(() => run('mark', owner, '--messages', '101,unknown', '--status', 'resolved', '--note', 'Must roll back'));
	assert.equal(run('list', owner).reports[0].status, 'pending');
	run('mark', owner, '--messages', '100', '--status', 'resolved', '--note', 'Verified fix');
	run('mailbox', owner, '--messages', '100', '--action', 'trash', '--outcome', 'failed', '--note', 'Read-back unavailable');
	const resolved = run('list', owner, '--status', 'resolved').reports[0];
	assert.equal(resolved.mailbox_outcome, 'failed'); assert.equal(resolved.status, 'resolved');
	run('mark', owner, '--messages', '100', '--status', 'pending', '--note', 'Reopened');
	assert.equal(run('list', owner).total, 2);
	assert.equal(run('list', owner, '--status', 'all', '--messages', '100').reports[0].message_id, '100');
	assert.equal(run('list', owner, '--status', 'all', '--messages', 'unknown').total, 0);
	assert.equal(run('list', owner, '--limit', '1').nextOffset, 1);
	assert.equal(run('list', owner, '--limit', '1', '--offset', '1').nextOffset, null);
	assert.throws(() => run('record', owner, '--topic', 'different', '--message', '100'));
	assert.throws(() => run('mark', owner, '--messages', '100', '--status', 'handled'));
	assert.throws(() => run('list', owner, '--limit', '0'));
	console.log('PASS: EWL folder/default/override resolution, persistent exact-ID dispositions, mailbox isolation, new occurrences, atomic marking, resolution/cleanup independence, reopening, pagination, and validation.');
}
finally { fs.rmSync(state, { recursive: true, force: true }); }
