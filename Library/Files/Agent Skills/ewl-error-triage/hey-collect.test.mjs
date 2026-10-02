import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { RpcError, decodeResponse, createCaller, binding, assertBinding, newCollection, discover, move,
	verify, reconcile, audit, summary, saveManifest } from './hey-collect.mjs';

const endpoint = 'https://localhost:8700/SystemManager/mcp/hey';
const profile = { categories: [{ name: 'Example errors', subjectPattern: '^Error in Example$', senderPattern: '^errors@example\\.invalid$' }] };
const row = (posting, overrides = {}) => ({ id: posting, kind: 'topic', box_id: 100, account_id: 10,
	app_url: `https://app.hey.com/topics/${posting + 1000}`, name: 'Error in Example', seen: false,
	creator: { email_address: 'errors@example.invalid' }, created_at: '2026-01-01T00:00:00Z',
	summary: 'SECRET BODY MUST NOT BE PERSISTED', ...overrides });

function fixture(rows) {
	const f = { rows, calls: [], writes: 0, pageSize: 2, failBefore: false, failAfter: false, expired: false, identity: 1 };
	f.call = async (name, action, params = {}) => {
		f.calls.push({ name, action, params });
		if (action === 'get_identity') return { id: f.identity, accounts: [{ id: 10 }, ...(f.multiAccount ? [{ id: 20 }] : [])] };
		if (action === 'list_boxes') return [{ id: 100, kind: 'imbox' }, { id: 200, kind: 'asidebox' }];
		if (action === 'get_box') {
			const offset = params.page ? Number(params.page.slice(2)) : 0;
			const entries = f.rows.filter(r => String(r.box_id) === params.boxId);
			const next = offset + f.pageSize < entries.length ? `p:${offset + f.pageSize}` : null;
			return { results: { id: Number(params.boxId), postings: entries.slice(offset, offset + f.pageSize),
				posting_changes_url: 'https://app.hey.com/changes?since=2026-01-01T00%3A00%3A00Z&v=2' },
				...(next || f.repeatCursor ? { next_page: f.repeatCursor ? 'p:2' : next } : {}) };
		}
		if (action === 'get_box_posting_changes') {
			if (f.expired) throw new RpcError('conflict');
			return { results: { added: f.rows.filter(r => r.box_id === 200), updated: [], deleted: [] },
				next_since: '2026-01-02T00:00:00Z', next_v: '3' };
		}
		if (action === 'move_topic') {
			f.writes++;
			assert.equal(params.box_id, 200); assert.equal(params.body, undefined);
			if (f.failBefore) throw new RpcError(f.rateLimited ? 'rate-limit' : 'connection interrupted');
			const posting = f.rows.find(r => r.app_url.endsWith('/' + params.topicId));
			if (posting && !f.noop) posting.box_id = 200;
			if (f.failAfter) throw new RpcError('response interrupted');
			return { status: 204 };
		}
		throw new Error('Unexpected operation');
	};
	return f;
}

async function selected(f, customProfile = profile) {
	const current = await binding(f.call, endpoint, '10');
	const m = newCollection(current, customProfile);
	await discover(m, f.call, () => {});
	return m;
}

const directory = fs.mkdtempSync(path.join(os.tmpdir(), 'ewl-hey-collect-'));
try {
	const f = fixture([row(1), row(2, { account_id: 20 }), row(3, { name: 'Personal mail' }),
		row(4, { kind: 'bundle' }), row(5, { created_at: '2099-01-01T00:00:00Z' }), row(6)]);
	const current = await binding(f.call, endpoint, '10'), m = newCollection(current, profile);
	let checkpoint;
	await discover(m, f.call, value => { checkpoint = structuredClone(value); }, 1);
	assert.equal(m.discovery.complete, false); assert.equal(m.discovery.nextPage, 'p:2');
	const resumed = structuredClone(checkpoint);
	await discover(resumed, f.call, () => {});
	assert.deepEqual(resumed.items.map(i => i.postingId), ['1', '6']);
	assert.equal(resumed.discovery.unsupported.length, 1); assert.equal(resumed.discovery.newerSkipped, 1);
	assert.equal(JSON.stringify(resumed).includes('SECRET BODY'), false);
	await move(resumed, f.call, () => {}, { batches: 2, batchSize: 1 });
	assert.equal(f.writes, 2); assert.equal(summary(resumed).allMovesVerified, true);
	assert.equal(resumed.verificationCursor.v, '3');
	await move(resumed, f.call, () => {}); assert.equal(f.writes, 2);
	f.rows.push(row(7));
	await audit(resumed, f.call, () => {});
	assert.equal(resumed.audit.complete, true); assert.equal(resumed.audit.otherMatchingThreads.length, 3);
	assert.equal(f.rows.find(r => r.id === 7).box_id, 100); // Audit does not collect new arrivals.
	assert.throws(() => assertBinding(resumed, { ...current, identityId: '99' }));
	assert.throws(() => assertBinding(resumed, { ...current, endpoint: 'https://another.invalid/mcp/hey' }));
	f.multiAccount = true;
	await assert.rejects(binding(f.call, endpoint));
	await assert.rejects(binding(f.call, endpoint, '30'));

	const interrupted = fixture([row(10)]), interruptedManifest = await selected(interrupted);
	let durable;
	await assert.rejects(move(interruptedManifest, interrupted.call, value => {
		durable = structuredClone(value); if (value.items.some(i => i.outcome === 'attempting')) throw new Error('Simulated crash after checkpoint');
	}));
	assert.equal(interrupted.writes, 0);
	await assert.rejects(move(durable, interrupted.call, () => {}));
	assert.equal(interrupted.writes, 0);
	await reconcile(durable, interrupted.call, () => {});
	assert.equal(durable.items[0].outcome, 'retryable');
	await move(durable, interrupted.call, () => {}); assert.equal(interrupted.writes, 0);
	await move(durable, interrupted.call, () => {}, { retryReconciled: true });
	assert.equal(interrupted.writes, 1); assert.equal(durable.items[0].outcome, 'verified');

	const committed = fixture([row(20)]), committedManifest = await selected(committed);
	committed.failAfter = true;
	await assert.rejects(move(committedManifest, committed.call, () => {}));
	assert.equal(committedManifest.items[0].outcome, 'verified');
	await reconcile(committedManifest, committed.call, () => {});
	await move(committedManifest, committed.call, () => {});
	assert.equal(committed.writes, 1); // Lost mutation response must never cause replay.

	const limited = fixture([row(30), row(31)]), limitedManifest = await selected(limited);
	limited.failBefore = true; limited.rateLimited = true;
	await assert.rejects(move(limitedManifest, limited.call, () => {}, { concurrency: 1 }), e => e.kind === 'rate-limit');
	assert.equal(limited.writes, 1); assert.equal(limitedManifest.items[1].outcome, 'pending');
	assert.equal(limited.calls.at(-1).action, 'move_topic'); // No extra requests after 429.
	limited.failBefore = false;
	await reconcile(limitedManifest, limited.call, () => {});
	assert.equal(limitedManifest.items[0].outcome, 'retryable');

	const expired = fixture([row(40)]), expiredManifest = await selected(expired);
	expired.expired = true;
	await move(expiredManifest, expired.call, () => {});
	assert.equal(expiredManifest.items[0].outcome, 'verified');
	assert(expired.calls.some(c => c.action === 'get_box' && c.params.boxId === '200'));
	const partial = fixture([row(45)]), partialManifest = await selected(partial);
	partialManifest.items[0].outcome = 'uncertain';
	partialManifest.verificationCursor = { since: '2026-01-01T00:00:00Z', v: '2' };
	const partialCall = async (name, action, params) => {
		if (action !== 'get_box_posting_changes') return partial.call(name, action, params);
		if (params.page) throw new RpcError('conflict');
		return { results: { added: [row(45, { box_id: 200 })], updated: [], deleted: [] }, next_page: 'stale-page' };
	};
	await verify(partialManifest, partialCall, () => {});
	assert.equal(partialManifest.items[0].outcome, 'uncertain'); // Discard partial feed evidence on 409.

	const missing = fixture([row(50)]), missingManifest = await selected(missing);
	missing.noop = true;
	await assert.rejects(move(missingManifest, missing.call, () => {}));
	assert.notEqual(missingManifest.items[0].outcome, 'verified');
	missing.rows[0].box_id = 300;
	await reconcile(missingManifest, missing.call, () => {});
	assert.equal(missingManifest.items[0].outcome, 'missing');
	missing.rows[0].box_id = 200;
	await verify(missingManifest, missing.call, () => {});
	assert.equal(missingManifest.items[0].outcome, 'verified');

	const repeating = fixture([row(60), row(61), row(62)]);
	repeating.repeatCursor = true;
	await assert.rejects(selected(repeating), /repeated pagination/);
	const mixed = fixture([row(70), row(71), row(72), row(73, { name: 'Warning in Example' })]);
	const mixedManifest = await selected(mixed, { categories: [...profile.categories, { name: 'Warnings', subjectPattern: '^Warning' }] });
	assert.deepEqual(summary(mixedManifest, 2).sample.map(i => i.category), ['Example errors', 'Warnings']);

	const json = JSON.stringify({ jsonrpc: '2.0', id: 1, result: { protocolVersion: '2025-06-18' } });
	assert.equal(decodeResponse(200, 'application/json', json, 1).protocolVersion, '2025-06-18');
	assert.equal(decodeResponse(200, 'text/event-stream', `: comment\r\nevent: message\r\ndata: ${json}\r\n\r\n`, 1).protocolVersion, '2025-06-18');
	assert.throws(() => decodeResponse(200, 'application/json', json, 2));
	assert.throws(() => decodeResponse(302, 'application/json', 'SECRET TOKEN', 1), e => !e.message.includes('SECRET'));
	assert.throws(() => decodeResponse(200, 'application/json', JSON.stringify({ jsonrpc: '2.0', id: 1,
		result: { isError: true, content: [{ type: 'text', text: 'HTTP 429 SECRET TOKEN' }] } }), 1), e => e.kind === 'rate-limit' && !e.message.includes('SECRET'));
	assert.throws(() => createCaller({ endpoint: 'http://localhost/mcp', token: 'x' }));
	assert.throws(() => createCaller({ endpoint: 'https://remote.invalid/mcp', token: 'x', insecureLocal: true }));
	let sent = 0;
	const client = createCaller({ endpoint, token: 'never-log-this', interval: 20 }, async (url, headers, body) => {
		sent++; assert.equal(headers.Authorization, 'never-log-this');
		const request = JSON.parse(body);
		if (request.method === 'initialize') return { status: 200, contentType: 'application/json', text: JSON.stringify({ jsonrpc: '2.0', id: request.id, result: { protocolVersion: '2025-06-18' } }) };
		if (request.method === 'notifications/initialized') return { status: 202 };
		return { status: 429, contentType: 'text/plain', text: 'never-log-this' };
	});
	await client.initialize();
	const limitedCalls = await Promise.allSettled([
		client.call('hey_threads', 'move_topic', { topicId: '1', box_id: 2 }),
		client.call('hey_threads', 'move_topic', { topicId: '3', box_id: 2 })
	]);
	assert(limitedCalls.every(result => result.status === 'rejected' && result.reason.kind === 'rate-limit'));
	assert.equal(sent, 3);
	await assert.rejects(client.call('hey_threads', 'trash_topic')); assert.equal(sent, 3);

	const collections = path.join(directory, 'collections'); fs.mkdirSync(collections);
	const file = path.join(collections, 'test.json'); saveManifest(file, mixedManifest);
	assert.equal(fs.existsSync(file + '.tmp'), false);
	const cli = fileURLToPath(new URL('./hey-collect.mjs', import.meta.url));
	const output = JSON.parse(execFileSync(process.execPath, [cli, 'summarize', '--collection', 'test', '--state', directory, '--limit', '2'],
		{ encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }));
	assert.equal(output.sample.length, 2); assert.equal(output.sampleComplete, false);
	fs.writeFileSync(file + '.lock', 'test lock');
	assert.throws(() => execFileSync(process.execPath, [cli, 'summarize', '--collection', 'test', '--state', directory], { stdio: 'pipe' }));
	console.log('PASS: scoped pagination/resume, account binding, interrupted batches, no replay, explicit reconciled retry, rate-limit stop, 409 fallback, no-op detection, audit/new arrivals, balanced summaries, protocol/error privacy, and atomic/locked storage.');
} finally { fs.rmSync(directory, { recursive: true, force: true }); }
