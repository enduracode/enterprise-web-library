import fs from 'node:fs';
import path from 'node:path';
import { createHash } from 'node:crypto';
import { DatabaseSync } from 'node:sqlite';
import { fileURLToPath } from 'node:url';
import { stateDirectory } from './hey-paths.mjs';
import { openReportStore } from './hey-state.mjs';
import { createCaller, createReader, defaultRequestInterval, binding, assertBinding, withBothImboxSections, RpcError } from './hey-collect.mjs';

const unwrap = value => value.results ?? value;
const now = () => new Date().toISOString();
const id = value => {
	if (!/^[1-9]\d*$/.test(String(value)) || !Number.isSafeInteger(Number(value))) throw new Error('Invalid identifier.');
	return String(value);
};
const body = message => (message.content ?? '').replace(/<br\s*\/?\s*>/gi, '\n').replace(/<[^>]*>/g, '')
	.replaceAll('&gt;', '>').replaceAll('&lt;', '<').replaceAll('&amp;', '&').replaceAll('&quot;', '"');
const hash = message => createHash('sha256').update(JSON.stringify([message.subject, message.content, message.is_reply,
	message.sender?.email_address ?? message.creator?.email_address])).digest('hex');
const signature = entries => JSON.stringify(entries.map(e => [id(e.id), e.kind, e.updated_at ?? null]).sort((a, b) => a[0].localeCompare(b[0])));

export function resolutionMatcher(profile) {
	if (!profile.system || !Array.isArray(profile.rules) || !profile.rules.length) throw new Error('A confirmed system and resolution rules are required.');
	const patterns = profile.rules.map(rule => {
		for (const key of ['subjectPattern', 'senderPattern', 'bodyPattern', 'installationPattern'])
			if (typeof rule[key] !== 'string' || !rule[key] || rule[key].length > 2000) throw new Error('Each rule requires bounded subject, sender, body, and installation patterns.');
		return Object.fromEntries(Object.entries(rule).map(([key, value]) => [key, new RegExp(value, 'i')]));
	});
	return message => {
		const text = body(message), installation = text.match(/Installation:\s*([^\r\n]+)/)?.[1]?.trim();
		return !message.is_reply && ['imbox', 'asidebox'].includes(message.posting?.box) && installation && patterns.some(rule =>
			rule.subjectPattern.test(message.subject ?? '') && rule.senderPattern.test(message.sender?.email_address ?? message.creator?.email_address ?? '') &&
			rule.bodyPattern.test(text) && rule.installationPattern.test(installation));
	};
}

// Separate metadata-only action journal. Checkpoints touch one row rather than rewriting the whole inventory.
export function openJournal(file, bindingValue) {
	const db = new DatabaseSync(file);
	db.exec(`PRAGMA busy_timeout=5000; PRAGMA journal_mode=WAL;
		CREATE TABLE IF NOT EXISTS binding(value TEXT NOT NULL);
		CREATE TABLE IF NOT EXISTS actions(topic TEXT PRIMARY KEY, value TEXT NOT NULL);`);
	const old = db.prepare('SELECT value FROM binding').get();
	if (old && old.value !== JSON.stringify(bindingValue)) { db.close(); throw new Error('Cleanup source/profile/authorization changed.'); }
	if (!old) db.prepare('INSERT INTO binding VALUES(?)').run(JSON.stringify(bindingValue));
	const get = db.prepare('SELECT value FROM actions WHERE topic=?');
	const put = db.prepare('INSERT INTO actions VALUES(?,?) ON CONFLICT(topic) DO UPDATE SET value=excluded.value');
	return {
		close: () => db.close(),
		get: topic => { const row = get.get(String(topic)); return row ? JSON.parse(row.value) : null; },
		save: row => put.run(row.topicId, JSON.stringify({ ...row, updatedAt: now() })),
		list: () => db.prepare('SELECT value FROM actions').all().map(row => JSON.parse(row.value))
	};
}

export class Cleanup {
	constructor({ call, reader, manifest, journal, store, profile, authorization }) {
		Object.assign(this, { call, reader, manifest, journal, store, profile, authorization });
		if (!Number.isFinite(Date.parse(manifest.startedAt))) throw new Error('A frozen source snapshot timestamp is required.');
		this.matches = resolutionMatcher(profile);
	}
	async entries(topics) {
		const rows = new Map(topics.map(topic => [topic, []])), pages = new Map(topics.map(topic => [topic, null])), used = new Map(topics.map(t => [t, new Set()]));
		while (pages.size) {
			const requests = [...pages].map(([topic, page]) => ({ id: topic, action: 'get_topic_entries', params: { topicId: Number(topic), ...(page ? { page } : {}) } }));
			const result = await this.reader(requests);
			for (const [topic, data] of result) {
				const value = unwrap(data), entries = Array.isArray(value) ? value : value.entries;
				if (!Array.isArray(entries)) throw new RpcError('invalid entry response');
				rows.get(topic).push(...entries);
				if (data.next_page) {
					if (used.get(topic).has(data.next_page)) throw new RpcError('repeated entry cursor');
					used.get(topic).add(data.next_page); pages.set(topic, data.next_page);
				} else pages.delete(topic);
			}
		}
		for (const entries of rows.values()) if (new Set(entries.map(e => id(e.id))).size !== entries.length) throw new RpcError('duplicate entry identity');
		return rows;
	}
	async assess(topics) {
		const metadata = await this.reader(topics.map(topic => ({ id: topic, action: 'get_topic', params: { topicId: Number(topic) } })));
		for (const topic of topics) {
			const value = unwrap(metadata.get(topic));
			if (value.account_id !== Number(this.manifest.accountId)) throw new RpcError('assessment topic account mismatch');
			if (value.status !== 'active') this.journal.save({ topicId: topic, outcome: 'excluded-inactive', messages: [] });
		}
		topics = topics.filter(topic => unwrap(metadata.get(topic)).status === 'active');
		const entryRows = await this.entries(topics);
		const requests = [], owners = new Map();
		for (const [topic, entries] of entryRows) for (const entry of entries) if (entry.kind === 'message') {
			const key = id(entry.id); owners.set(key, topic); requests.push({ id: key, action: 'get_message', params: { messageId: Number(key) } });
		}
		const messages = await this.reader(requests);
		for (const topic of topics) {
			const entries = entryRows.get(topic), list = entries.filter(e => e.kind === 'message').map(e => unwrap(messages.get(id(e.id))));
			const eligible = entries.length > 0 && list.length === entries.length && list.every(m => this.matches(m) && owners.get(id(m.id)) === topic &&
				m.creator?.account_id === Number(this.manifest.accountId) && Number.isFinite(Date.parse(m.created_at)) && Date.parse(m.created_at) <= Date.parse(this.manifest.startedAt));
			const row = { topicId: topic, outcome: eligible ? 'assessed' : 'excluded', assessedAt: now(), signature: signature(entries),
				messages: list.map(m => ({ id: id(m.id), topicId: topic, system: this.profile.system,
					installation: body(m).match(/Installation:\s*([^\r\n]+)/)?.[1]?.trim() ?? '', createdAt: m.created_at,
					originalBox: m.posting?.box ?? null, hash: hash(m) })),
				// Missing update tokens require full-content comparison after trash, not a weaker identity-only check.
				needsContentCheck: entries.some(e => !e.updated_at) };
			this.store.record(this.manifest.mailboxKey, row.messages);
			const previous = this.journal.get(topic);
			const changedOld = previous?.messages.filter(old => !row.messages.some(m => m.id === old.id && m.hash === old.hash)).map(m => m.id) ?? [];
			if (changedOld.length) this.store.reopen(this.manifest.mailboxKey, changedOld, 'Assessed content changed on restart; resolution requires reassessment.');
			if (eligible) this.store.resolve(this.manifest.mailboxKey, row.messages.map(m => m.id), this.authorization);
			this.journal.save(row);
		}
	}
	async verifyPlacement(row) {
		const entries = (await this.entries([row.topicId])).get(row.topicId);
		const survivor = entries.find(e => e.kind === 'message');
		const aside = row.messages.every(m => m.originalBox === 'asidebox');
		if (survivor) {
			const message = unwrap(await this.call('hey_threads', 'get_message', { messageId: Number(id(survivor.id)) }));
			if (id(message.id) !== id(survivor.id) || !['imbox', 'asidebox'].includes(message.posting?.box) || aside && message.posting.box !== 'asidebox')
				throw new RpcError('restored placement not verified');
			return;
		}
		// All original messages may have disappeared. Use fresh topic postings, never old posting IDs.
		const call = withBothImboxSections(this.call, this.manifest.sourceBoxId);
		for (const boxId of aside ? [this.manifest.destinationBoxId] : [this.manifest.sourceBoxId, this.manifest.destinationBoxId]) {
			let page; const used = new Set();
			do {
				const data = await call('hey_boxes', 'get_box', { boxId, ...(page ? { page } : {}) }), box = unwrap(data);
				if (String(box.id) !== String(boxId) || !Array.isArray(box.postings)) throw new RpcError('invalid placement box response');
				if (box.postings.some(p => {
					let topic = p.topic_id;
					if (!topic && p.app_url) {
						try { const url = new URL(p.app_url); if (url.hostname === 'app.hey.com') topic = /^\/topics\/([1-9]\d*)\/?$/.exec(url.pathname)?.[1]; } catch {}
					}
					return String(topic) === row.topicId && String(p.account_id) === this.manifest.accountId && String(p.box_id) === String(boxId);
				})) return;
				page = data.next_page;
				if (page && (typeof page !== 'string' || used.has(page))) throw new RpcError('invalid placement cursor');
				if (page) used.add(page);
			} while (page);
		}
		throw new RpcError('restored placement not verified');
	}
	async compareContents(row, entries) {
		const surviving = new Set(entries.filter(e => e.kind === 'message').map(e => id(e.id)));
		const messages = row.messages.filter(m => surviving.has(m.id));
		const result = await this.reader(messages.map(m => ({ id: m.id, action: 'get_message', params: { messageId: Number(m.id) } })));
		const changed = messages.filter(m => hash(unwrap(result.get(m.id))) !== m.hash).map(m => m.id);
		if (changed.length) this.store.reopen(this.manifest.mailboxKey, changed, 'Message content changed after resolution assessment; reassess exact source.');
		if (row.pendingContentCheck) {
			const oldTokens = new Map(JSON.parse(row.signature).map(e => [e[0], JSON.stringify(e.slice(1))]));
			const unchangedTokens = new Set(entries.filter(e => JSON.stringify([e.kind, e.updated_at ?? null]) === oldTokens.get(id(e.id))).map(e => id(e.id)));
			const unchanged = messages.filter(m => !changed.includes(m.id) && unchangedTokens.has(m.id)).map(m => m.id);
			if (unchanged.length) this.store.resolve(this.manifest.mailboxKey, unchanged, this.authorization);
			row.pendingContentCheck = false; this.journal.save(row);
		}
		return changed.length > 0;
	}
	async restore(row) {
		const ids = row.messages.map(m => m.id), mailbox = this.manifest.mailboxKey;
		row.outcome = 'restoring'; row.restoreAttemptedAt = now(); this.journal.save(row);
		this.store.mailbox(mailbox, ids, 'restore', 'pending', 'Content changed since assessment; restore and reassess.');
		try {
			await this.call('hey_threads', 'restore_topic', { topicId: Number(row.topicId) });
			const topic = unwrap(await this.call('hey_threads', 'get_topic', { topicId: Number(row.topicId) }));
			if (topic.account_id !== Number(this.manifest.accountId) || topic.status !== 'active') throw new RpcError('restore not verified');
			// A shared topic or new reply may require a fresh posting. Never reuse old posting IDs.
			if (row.messages.every(m => m.originalBox === 'asidebox')) await this.call('hey_threads', 'move_topic', {
				topicId: Number(row.topicId), box_id: Number(this.manifest.destinationBoxId) });
			await this.verifyPlacement(row);
			row.outcome = 'restored-changed'; row.restoredAt = now(); this.journal.save(row);
			this.store.mailbox(mailbox, ids, 'restore', 'verified', 'Restored changed thread; new content has not been resolved. Seen state is not guaranteed.');
		} catch (error) {
			row.outcome = 'uncertain-restore'; this.journal.save(row);
			this.store.mailbox(mailbox, ids, 'restore', 'uncertain', 'Restore/placement could not be verified; reconcile before further mutation.'); throw error;
		}
	}
	async verify(rows) {
		if (!rows.length) return;
		const entryRows = await this.entries(rows.map(r => r.topicId));
		const requests = rows.map(r => ({ id: 'topic:' + r.topicId, action: 'get_topic', params: { topicId: Number(r.topicId) } }));
		const result = await this.reader(requests);
		for (const row of rows) {
			const topic = unwrap(result.get('topic:' + row.topicId));
			if (topic.account_id !== Number(this.manifest.accountId)) throw new RpcError('topic account mismatch');
			if (!['active', 'trashed'].includes(topic.status)) throw new RpcError('unexpected topic status');
			const currentEntries = entryRows.get(row.topicId);
			let changed = signature(currentEntries) !== row.signature;
			if (changed) {
				const oldTokens = new Map(JSON.parse(row.signature).map(e => [e[0], e.slice(1)]));
				const changedOld = row.messages.filter(m => {
					const entry = currentEntries.find(e => id(e.id) === m.id);
					return row.needsContentCheck || !entry || JSON.stringify([entry.kind, entry.updated_at ?? null]) !== JSON.stringify(oldTokens.get(m.id));
				}).map(m => m.id);
				if (changedOld.length) this.store.reopen(this.manifest.mailboxKey, changedOld, 'Message changed/removed or tokenless content awaits comparison; reassess exact source.');
				// Restore known-changed threads before content reads that may fail for removed messages.
				row.pendingContentCheck = row.needsContentCheck; this.journal.save(row);
			}
			else if (row.needsContentCheck) changed = await this.compareContents(row, currentEntries);
			if (topic.status === 'active') {
				row.outcome = 'resurfaced'; this.journal.save(row);
				this.store.mailbox(this.manifest.mailboxKey, row.messages.map(m => m.id), 'trash', 'failed', 'Thread active on read-back; no trash replay. Reassess.');
			} else if (changed) await this.restore(row);
			else {
				const ids = row.messages.map(m => m.id);
				// Commit tracking before journal completion. Crash recovery may repeat tracking, never the mutation.
				this.store.resolve(this.manifest.mailboxKey, ids, this.authorization);
				this.store.mailbox(this.manifest.mailboxKey, ids, 'trash', 'verified', 'Topic trashed; all entry identities/change tokens match assessment.');
				row.outcome = 'verified'; row.verifiedAt = now(); this.journal.save(row);
			}
			if (row.pendingContentCheck) await this.compareContents(row, (await this.entries([row.topicId])).get(row.topicId));
		}
	}
	async run(batchSize = 20) {
		if (this.journal.list().some(r => r.pendingContentCheck || ['attempting', 'accepted', 'uncertain', 'restoring', 'uncertain-restore'].includes(r.outcome)))
			throw new Error('Reconcile incomplete actions before more mutations.');
		const topics = [...new Set(this.manifest.items.map(item => id(item.topicId)))].filter(t => {
			const old = this.journal.get(t); return !old || old.outcome === 'assessed';
		});
		for (let offset = 0; offset < topics.length; offset += batchSize) {
			const batch = topics.slice(offset, offset + batchSize); await this.assess(batch);
			const rows = batch.map(t => this.journal.get(t)).filter(r => r.outcome === 'assessed');
			let index = 0, failure;
			await Promise.all(Array.from({ length: Math.min(4, rows.length) }, async () => {
				while (index < rows.length && !failure) {
					const row = rows[index++], ids = row.messages.map(m => m.id);
					row.outcome = 'attempting'; row.attemptedAt = now(); this.journal.save(row);
					this.store.mailbox(this.manifest.mailboxKey, ids, 'trash', 'pending', 'Assessed thread; post-trash comparison and restoration required.');
					try {
						await this.call('hey_threads', 'trash_topic', { topicId: Number(row.topicId), confirm_destroy: 'true' });
						row.outcome = 'accepted'; this.journal.save(row);
					} catch (error) { row.outcome = 'uncertain'; this.journal.save(row); failure = error; }
				}
			}));
			if (failure) throw failure; // Rate limits and uncertain writes stop before further network calls.
			await this.verify(rows.filter(r => r.outcome === 'accepted'));
		}
	}
	async reconcile() {
		const rows = this.journal.list();
		await this.verify(rows.filter(r => ['attempting', 'accepted', 'uncertain'].includes(r.outcome)));
		for (const row of rows.filter(r => ['restoring', 'uncertain-restore'].includes(r.outcome))) {
			const topic = unwrap(await this.call('hey_threads', 'get_topic', { topicId: Number(row.topicId) }));
			if (topic.account_id !== Number(this.manifest.accountId) || topic.status !== 'active') throw new Error('Restore still uncertain; no automatic replay.');
			await this.verifyPlacement(row);
			row.outcome = 'restored-changed'; this.journal.save(row);
			this.store.mailbox(this.manifest.mailboxKey, row.messages.map(m => m.id), 'restore', 'verified', 'Restore reconciled by reads; content awaits reassessment.');
		}
		for (const row of this.journal.list().filter(r => r.pendingContentCheck && ['restored-changed', 'resurfaced'].includes(r.outcome)))
			await this.compareContents(row, (await this.entries([row.topicId])).get(row.topicId));
	}
}

async function main() {
	const [command, ...args] = process.argv.slice(2);
	if (!['run', 'reconcile', 'summary'].includes(command)) throw new Error('Use run|reconcile|summary --collection NAME --profile FILE --authorization NOTE [--endpoint URL] [--interval-ms 125] [--state DIR].');
	const options = {};
	for (let i = 0; i < args.length; i += 2) {
		const key = args[i].slice(2);
		if (!args[i].startsWith('--') || !['collection', 'profile', 'authorization', 'endpoint', 'state', 'interval-ms'].includes(key) || key in options || !args[i + 1]) throw new Error('Invalid options.');
		options[key] = args[i + 1];
	}
	if (!/^[a-zA-Z0-9][a-zA-Z0-9._-]{0,79}$/.test(options.collection ?? '') || !options.profile || !options.authorization) throw new Error('Collection, trusted resolution profile, and explicit authorization note required.');
	const interval = Number(options['interval-ms'] ?? defaultRequestInterval);
	if (!Number.isSafeInteger(interval) || interval < 100 || interval > 60000) throw new Error('Invalid request interval.');
	const directory = stateDirectory(options.state), file = path.join(directory, 'collections', options.collection + '.json');
	const manifest = JSON.parse(fs.readFileSync(file, 'utf8')), profile = JSON.parse(fs.readFileSync(options.profile, 'utf8'));
	if (!manifest.discovery.complete) throw new Error('Complete collection discovery first; moving to Set Aside is optional for this explicit cleanup.');
	let lock, journal, store;
	try {
		lock = fs.openSync(file + '.cleanup.lock', 'wx'); fs.writeFileSync(lock, JSON.stringify({ pid: process.pid, startedAt: now() }));
		journal = openJournal(file + '.cleanup.sqlite', { mailbox: manifest.mailboxKey, source: manifest.startedAt, profile, authorization: options.authorization });
		if (command !== 'summary') {
			const client = createCaller({ endpoint: options.endpoint ?? manifest.endpoint, token: process.env.SYSTEM_MANAGER_API_TOKEN, interval });
			await client.initialize(); assertBinding(manifest, await binding(client.call, options.endpoint ?? manifest.endpoint, manifest.accountId));
			store = openReportStore(directory);
			const cleanup = new Cleanup({ call: client.call, reader: createReader(client.call), manifest, journal, store, profile, authorization: options.authorization });
			if (command === 'run') await cleanup.run(); else await cleanup.reconcile();
		}
		console.log(JSON.stringify({ mailbox: manifest.mailboxKey, snapshotStartedAt: manifest.startedAt, selectedTopics: new Set(manifest.items.map(i => i.topicId)).size,
			outcomes: journal.list().reduce((counts, r) => { counts[r.outcome] = (counts[r.outcome] ?? 0) + 1; return counts; }, {}) }));
	} finally {
		store?.close(); journal?.close(); if (lock !== undefined) { fs.closeSync(lock); fs.unlinkSync(file + '.cleanup.lock'); }
	}
}
if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) main().catch(error => {
	console.error(error instanceof RpcError ? error.message : 'Cleanup stopped; ' + (error.code ? 'local file operation failed' : error.message)); process.exitCode = 1;
});
