import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { Cleanup, openJournal } from './hey-cleanup.mjs';
import { openReportStore } from './hey-state.mjs';
import { createReader, defaultRequestInterval, RpcError, withBothImboxSections } from './hey-collect.mjs';

const root = fs.mkdtempSync(path.join(os.tmpdir(), 'ewl-hey-cleanup-'));
const profile = { system: 'Example', rules: [{ subjectPattern: '^Error in Example$', senderPattern: '^errors@example.invalid$',
	bodyPattern: 'resolved cause', installationPattern: '^Testing$' }] };
let fixtures = 0;
function fixture(count = 1, options = {}) {
	const directory = path.join(root, String(++fixtures)); fs.mkdirSync(directory);
	const manifest = { mailboxKey: 'endpoint|identity:1|account:10', accountId: '10', sourceBoxId: '100', destinationBoxId: '200', startedAt: '2026-01-04T00:00:00Z',
		items: Array.from({ length: count }, (_, i) => ({ topicId: String(i + 1) })) };
	const data = new Map(manifest.items.map(item => [item.topicId, { status: 'active', entries: [{ id: Number(item.topicId) + 100,
		kind: 'message', ...(options.noTokens ? {} : { updated_at: '2026-01-01T00:00:00Z' }) }] }]));
	const messages = new Map(manifest.items.map(item => [String(Number(item.topicId) + 100), { id: Number(item.topicId) + 100,
		creator: { account_id: 10, email_address: 'errors@example.invalid' }, is_reply: false, subject: 'Error in Example',
		content: 'resolved cause\nInstallation: Testing', created_at: '2026-01-01T00:00:00Z', posting: { box: options.aside ? 'asidebox' : 'imbox' } }]));
	const f = { data, messages, calls: [], reads: 0, writes: 0, restores: 0 };
	const base = async (action, p) => {
		if (action === 'get_topic_entries') { f.reads++; return structuredClone(data.get(String(p.topicId)).entries); }
		if (action === 'get_message') {
			f.reads++; options.onMessage?.(f, p);
			if (!messages.has(String(p.messageId))) throw new RpcError('message unavailable');
			return structuredClone(messages.get(String(p.messageId)));
		}
		if (['get_box', 'get_imbox', 'get_imbox_seen'].includes(action)) {
			f.reads++; const boxId = String(p.boxId ?? '100');
			return { id: Number(boxId), postings: [...data].filter(([, row]) => row.status === 'active').map(([topic]) => ({
				id: Number(topic) + 2000, topic_id: Number(topic), account_id: 10, box_id: Number(boxId) })) };
		}
		if (action === 'get_topic') { f.reads++; return { account_id: options.accountId ?? 10, status: data.get(String(p.topicId)).status }; }
		if (action === 'trash_topic') {
			f.writes++; const row = data.get(String(p.topicId)); row.status = 'trashed';
			for (const entry of row.entries) messages.get(String(entry.id)).posting.box = 'trash';
			options.onTrash?.(f, row);
			if (options.failWrite) throw new RpcError('connection interrupted');
			return {};
		}
		if (action === 'restore_topic') {
			f.restores++; if (options.failRestore) throw new RpcError('connection interrupted');
			const row = data.get(String(p.topicId)); row.status = 'active';
			for (const entry of row.entries) messages.get(String(entry.id)).posting.box = 'imbox';
			if (options.failRestoreResponse) throw new RpcError('restore response lost');
			return {};
		}
		if (action === 'move_topic') { for (const e of data.get(String(p.topicId)).entries) messages.get(String(e.id)).posting.box = 'asidebox'; return {}; }
		throw new Error('Unexpected action');
	};
	f.call = async (name, action, params) => {
		f.calls.push(action);
		return base(action, params);
	};
	f.store = openReportStore(directory); f.journal = openJournal(path.join(directory, 'actions.sqlite'), { mailbox: manifest.mailboxKey, profile });
	f.cleanup = new Cleanup({ call: f.call, reader: createReader(f.call), manifest, journal: f.journal, store: f.store,
		profile, authorization: 'User explicitly resolved example cause.' });
	f.close = () => { f.journal.close(); f.store.close(); };
	return f;
}

try {
	const basic = fixture();
	try {
		await basic.cleanup.run(); assert.equal(basic.journal.get('1').outcome, 'verified');
		assert.equal(basic.store.get(basic.cleanup.manifest.mailboxKey, '101').status, 'resolved');
		basic.data.get('1').status = 'active';
		basic.data.get('1').entries.push({ id: 999, kind: 'message', updated_at: '2026-01-03T00:00:00Z' });
		await basic.cleanup.run(); assert.equal(basic.writes, 1);
		assert.equal(basic.store.get(basic.cleanup.manifest.mailboxKey, '999'), undefined); // Later replies never inherit resolution.
	} finally { basic.close(); }
	const added = fixture(1, { aside: true, onTrash(f, row) {
		row.entries.push({ id: 999, kind: 'message', updated_at: '2026-01-02T00:00:00Z' });
		f.messages.set('999', { id: 999, posting: { box: 'trash' } });
	} });
	try {
		await added.cleanup.run(); assert.equal(added.restores, 1); assert.equal(added.journal.get('1').outcome, 'restored-changed');
		assert.equal(added.messages.get('101').posting.box, 'asidebox');
		assert.equal(added.store.get(added.cleanup.manifest.mailboxKey, '101').status, 'resolved');
		assert.equal(added.store.get(added.cleanup.manifest.mailboxKey, '999'), undefined);
	} finally { added.close(); }
	for (const mode of ['edit', 'removed', 'no-token-edit', 'resurface']) {
		const f = fixture(1, { noTokens: mode === 'no-token-edit', onTrash(f, row) {
			if (mode === 'edit') row.entries[0].updated_at = '2026-01-02T00:00:00Z';
			if (mode === 'removed') row.entries = [];
			if (mode === 'no-token-edit') f.messages.get('101').content = 'different cause';
			if (mode === 'resurface') { row.status = 'active'; f.messages.get('101').posting.box = 'imbox'; }
		} });
		try {
			await f.cleanup.run(); assert.notEqual(f.journal.get('1').outcome, 'verified');
			assert.equal(f.restores, mode === 'resurface' ? 0 : 1);
			assert.equal(f.store.get(f.cleanup.manifest.mailboxKey, '101').status, mode === 'resurface' ? 'resolved' : 'pending');
		} finally { f.close(); }
	}
	const failed = fixture(1, { failWrite: true });
	try {
		await assert.rejects(failed.cleanup.run()); assert.equal(failed.journal.get('1').outcome, 'uncertain');
		await assert.rejects(failed.cleanup.run(), /Reconcile/); assert.equal(failed.writes, 1);
		await failed.cleanup.reconcile(); assert.equal(failed.writes, 1); assert.equal(failed.journal.get('1').outcome, 'verified');
	} finally { failed.close(); }
	const restoreFailure = fixture(1, { failRestore: true, onTrash(f, row) { row.entries[0].updated_at = '2026-01-02T00:00:00Z'; } });
	try {
		await assert.rejects(restoreFailure.cleanup.run()); assert.equal(restoreFailure.journal.get('1').outcome, 'uncertain-restore');
		await assert.rejects(restoreFailure.cleanup.reconcile()); assert.equal(restoreFailure.restores, 1);
	} finally { restoreFailure.close(); }
	const excluded = fixture();
	for (const mode of ['none-survive', 'first-removed']) {
		const f = fixture(1, { noTokens: true, aside: true, onTrash(f, row) {
			row.entries = mode === 'none-survive' ? [] : [{ id: 999, kind: 'message' }];
			f.messages.delete('101');
			if (mode === 'first-removed') f.messages.set('999', { id: 999, posting: { box: 'trash' } });
		} });
		try {
			await f.cleanup.run(); assert.equal(f.restores, 1); assert.equal(f.journal.get('1').outcome, 'restored-changed');
			assert.equal(f.store.get(f.cleanup.manifest.mailboxKey, '101').status, 'pending');
			assert.equal(f.journal.get('1').pendingContentCheck, false);
			assert.equal(f.store.get(f.cleanup.manifest.mailboxKey, '999'), undefined);
			await f.cleanup.run(); assert.equal(f.writes, 1);
		} finally { f.close(); }
	}
	const unreadable = fixture(1, { noTokens: true, onMessage(f) { if (f.writes) throw new RpcError('read failed'); } });
	try {
		await assert.rejects(unreadable.cleanup.run(), /read failed/);
		assert.equal(unreadable.journal.get('1').outcome, 'accepted');
		await assert.rejects(unreadable.cleanup.run(), /Reconcile/); assert.equal(unreadable.writes, 1);
	} finally { unreadable.close(); }
	let failComparison = true;
	const originalSurvivor = fixture(1, { noTokens: true, aside: true, onTrash(f, row) {
		row.entries = row.entries.filter(e => e.id !== 101); f.messages.delete('101');
	} });
	try {
		originalSurvivor.data.get('1').entries.push({ id: 102, kind: 'message' });
		originalSurvivor.messages.set('102', { ...structuredClone(originalSurvivor.messages.get('101')), id: 102 });
		await originalSurvivor.cleanup.run();
		assert.equal(originalSurvivor.restores, 1); assert.equal(originalSurvivor.messages.get('102').posting.box, 'asidebox');
		assert.equal(originalSurvivor.store.get(originalSurvivor.cleanup.manifest.mailboxKey, '101').status, 'pending');
		assert.equal(originalSurvivor.store.get(originalSurvivor.cleanup.manifest.mailboxKey, '102').status, 'resolved');
	} finally { originalSurvivor.close(); }
	const deferred = fixture(1, { noTokens: true, onTrash(f, row) {
		row.entries.unshift({ id: 999, kind: 'message' }); f.messages.set('999', { id: 999, posting: { box: 'trash' } });
	}, onMessage(f, p) { if (f.restores && p.messageId === 101 && failComparison) throw new RpcError('comparison failed'); } });
	try {
		await assert.rejects(deferred.cleanup.run(), /comparison failed/);
		assert.equal(deferred.restores, 1); assert.equal(deferred.journal.get('1').outcome, 'restored-changed');
		assert.equal(deferred.journal.get('1').pendingContentCheck, true);
		assert.equal(deferred.store.get(deferred.cleanup.manifest.mailboxKey, '101').status, 'pending');
		await assert.rejects(deferred.cleanup.run(), /Reconcile/);
		failComparison = false; await deferred.cleanup.reconcile();
		assert.equal(deferred.store.get(deferred.cleanup.manifest.mailboxKey, '101').status, 'resolved');
		assert.equal(deferred.journal.get('1').pendingContentCheck, false);
		assert.equal(deferred.restores, 1); assert.equal(deferred.writes, 1);
	} finally { deferred.close(); }
	const recoveryRestore = fixture(1, { failWrite: true, aside: true, onTrash(f, row) { row.entries[0].updated_at = '2026-01-02T00:00:00Z'; } });
	try {
		await assert.rejects(recoveryRestore.cleanup.run()); await recoveryRestore.cleanup.reconcile();
		assert.equal(recoveryRestore.restores, 1); assert.equal(recoveryRestore.writes, 1);
		assert.equal(recoveryRestore.messages.get('101').posting.box, 'asidebox');
		assert.equal(recoveryRestore.journal.get('1').outcome, 'restored-changed');
		await recoveryRestore.cleanup.reconcile(); assert.equal(recoveryRestore.restores, 1);
	} finally { recoveryRestore.close(); }
	const restoredResponseLost = fixture(1, { failRestoreResponse: true, onTrash(f, row) { row.entries[0].updated_at = '2026-01-02T00:00:00Z'; } });
	try {
		await assert.rejects(restoredResponseLost.cleanup.run(), /restore response lost/);
		assert.equal(restoredResponseLost.journal.get('1').outcome, 'uncertain-restore');
		await restoredResponseLost.cleanup.reconcile();
		assert.equal(restoredResponseLost.journal.get('1').outcome, 'restored-changed');
		assert.equal(restoredResponseLost.restores, 1); assert.equal(restoredResponseLost.writes, 1);
	} finally { restoredResponseLost.close(); }
	const wrongRestoredPlacement = fixture(1, { aside: true, failRestoreResponse: true, onTrash(f, row) { row.entries[0].updated_at = '2026-01-02T00:00:00Z'; } });
	try {
		await assert.rejects(wrongRestoredPlacement.cleanup.run(), /restore response lost/);
		const writesBefore = wrongRestoredPlacement.calls.filter(a => ['restore_topic', 'trash_topic', 'move_topic'].includes(a)).length;
		await assert.rejects(wrongRestoredPlacement.cleanup.reconcile(), /placement not verified/);
		assert.equal(wrongRestoredPlacement.calls.filter(a => ['restore_topic', 'trash_topic', 'move_topic'].includes(a)).length, writesBefore);
		assert.equal(wrongRestoredPlacement.journal.get('1').outcome, 'uncertain-restore');
	} finally { wrongRestoredPlacement.close(); }
	try {
		excluded.messages.get('101').content = 'unrelated cause\nInstallation: Testing';
		await excluded.cleanup.run(); assert.equal(excluded.writes, 0); assert.equal(excluded.journal.get('1').outcome, 'excluded');
	} finally { excluded.close(); }
	const restart = fixture();
	const wrongAccount = fixture(1, { accountId: 20 });
	try { await assert.rejects(wrongAccount.cleanup.run(), /account mismatch/); assert.equal(wrongAccount.writes, 0); }
	finally { wrongAccount.close(); }
	const future = fixture();
	try { future.messages.get('101').created_at = '2026-01-05T00:00:00Z'; await future.cleanup.run(); assert.equal(future.writes, 0); }
	finally { future.close(); }
	try {
		await restart.cleanup.assess(['1']); restart.messages.get('101').content = 'unrelated cause\nInstallation: Testing';
		await restart.cleanup.run(); assert.equal(restart.writes, 0); // Reassess interrupted-but-unsent work.
	} finally { restart.close(); }
	const readRequests = Array.from({ length: 20 }, (_, i) => ({ id: String(i), action: 'get_topic', params: { topicId: i + 1 } }));
	let submitted = 0;
	const failedReader = createReader(async () => { submitted++; throw new RpcError('rate-limit'); });
	await assert.rejects(failedReader(readRequests), error => error.kind === 'rate-limit');
	assert(submitted <= 4); // Finish submitted reads, but do not submit more after failure.
	await assert.rejects(createReader(async () => ({}))([readRequests[0], readRequests[0]]), /duplicate/);
	let seenRead = false;
	const both = withBothImboxSections(async (_, action) => {
		if (action === 'get_imbox_seen') seenRead = true;
		return { id: 100, postings: [] };
	}, '100');
	const first = await both('hey_boxes', 'get_box', { boxId: '100' });
	assert.equal(first.next_page, 'seen:'); await both('hey_boxes', 'get_box', { boxId: '100', page: first.next_page }); assert.equal(seenRead, true);
	const perf = fixture(1000);
	try {
		const start = performance.now(); await perf.cleanup.run();
		assert.equal(perf.writes, 1000); assert.equal(perf.calls.length, 6000);
		assert.equal(perf.reads, 5000); assert.equal(perf.journal.list().filter(r => r.outcome === 'verified').length, 1000);
		assert.equal(JSON.stringify(perf.journal.list()).includes('resolved cause'), false);
		console.log(JSON.stringify({ syntheticTopics: 1000, outerMcpCalls: perf.calls.length, underlyingReads: perf.reads,
			legacyOneMessageWorkflowCalls: 8000, callReductionPercent: 25, defaultRequestInterval, fixtureMilliseconds: Math.round(performance.now() - start),
			note: 'Fixture call counts, not a live service throughput benchmark.' }));
	} finally { perf.close(); }
	console.log('PASS: post-trash additions/edits/removals, unreadable removed messages, surviving-message/box placement, deferred tokenless comparisons, reconciliation restoration without trash/restore replay, resurfacing, restart reassessment, exact dispositions, metadata privacy, both Imbox sections, and stopping concurrent reads after failure.');
} finally { fs.rmSync(root, { recursive: true, force: true }); }
