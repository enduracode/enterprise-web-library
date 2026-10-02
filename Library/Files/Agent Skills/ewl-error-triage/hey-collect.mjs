import fs from 'node:fs';
import path from 'node:path';
import https from 'node:https';
import { fileURLToPath } from 'node:url';
import { stateDirectory } from './hey-paths.mjs';

const format = 'ewl-hey-collection-v1';
const unwrap = value => value.results ?? value;
const now = () => new Date().toISOString();
const sleep = ms => new Promise(resolve => setTimeout(resolve, Math.max(0, ms)));
const inFlight = item => ['attempting', 'accepted', 'uncertain', 'missing'].includes(item.outcome);
const states = ['pending', 'attempting', 'accepted', 'uncertain', 'retryable', 'verified', 'missing'];

function id(value) {
	if ((typeof value === 'number' && !Number.isSafeInteger(value)) || !/^[1-9]\d*$/.test(String(value)))
		throw new Error('Invalid or missing HEY identifier.');
	return String(value);
}

export function endpointUrl(value) {
	let url;
	try { url = new URL(value); } catch { throw new Error('Supply a valid --endpoint or SYSTEM_MANAGER_MCP_URL.'); }
	if (url.protocol !== 'https:' || url.username || url.password || url.search || url.hash)
		throw new Error('The MCP endpoint must be HTTPS, without credentials, query, or fragment.');
	return url.href;
}

export class RpcError extends Error {
	constructor(kind) { super(`MCP request failed (${kind}); no automatic retry was performed.`); this.kind = kind; }
}

// Never print upstream error text: it can contain credentials, stderr, or mailbox data.
export function decodeResponse(status, contentType, text, requestId) {
	if (status !== 200) throw new RpcError(status === 409 ? 'conflict' : status === 429 ? 'rate-limit' : `HTTP ${status}`);
	let response;
	try {
		const messages = contentType.includes('text/event-stream')
			? text.replaceAll('\r\n', '\n').split(/\n\n/).map(event => event.split('\n')
				.filter(line => line.startsWith('data:')).map(line => line.slice(5).trimStart()).join('\n'))
				.filter(Boolean).map(data => JSON.parse(data))
			: [JSON.parse(text)];
		response = messages.find(message => message.id === requestId);
	} catch { throw new RpcError('invalid response'); }
	if (!response || response.jsonrpc !== '2.0') throw new RpcError('missing response');
	if (response.error || response.result?.isError) {
		const error = JSON.stringify(response.error ?? response.result);
		throw new RpcError(/\b409\b|conflict/i.test(error) ? 'conflict' : /\b429\b|rate.limit/i.test(error) ? 'rate-limit' : 'tool error');
	}
	return response.result;
}

async function exchange(url, headers, body, insecure, signal) {
	return new Promise((resolve, reject) => {
		const request = https.request(url, { method: 'POST', headers, rejectUnauthorized: !insecure, signal }, response => {
			let text = '';
			response.setEncoding('utf8');
			response.on('data', chunk => { text += chunk; });
			response.on('error', () => reject(new RpcError('response interrupted')));
			response.on('end', () => resolve({ status: response.statusCode, contentType: response.headers['content-type'] ?? '', text,
				session: response.headers['mcp-session-id'] }));
		});
		request.on('error', () => reject(new RpcError('connection interrupted or timed out')));
		request.end(body);
	});
}

export function createCaller({ endpoint, token, insecureLocal = false, interval = 300, signal }, send = exchange) {
	const url = new URL(endpointUrl(endpoint));
	if (!token || /[\r\n]/.test(token)) throw new Error('Supply the raw API token through the configured environment variable.');
	if (insecureLocal && !['localhost', '127.0.0.1', '[::1]'].includes(url.hostname))
		throw new Error('--insecure-local is restricted to loopback endpoints.');
	let sequence = 0, nextStart = 0, rateLimited = false;
	async function rpc(method, params, notification = false) {
		const start = Math.max(Date.now(), nextStart); nextStart = start + interval;
		await sleep(start - Date.now()); signal?.throwIfAborted();
		if (rateLimited) throw new RpcError('rate-limit');
		const requestId = ++sequence;
		const response = await send(url, { Authorization: token, Accept: 'application/json, text/event-stream',
			'Content-Type': 'application/json', 'MCP-Protocol-Version': '2025-06-18' },
		JSON.stringify({ jsonrpc: '2.0', ...(notification ? {} : { id: requestId }), method, params }), insecureLocal,
		signal ? AbortSignal.any([signal, AbortSignal.timeout(120000)]) : AbortSignal.timeout(120000));
		if (response.session) throw new RpcError('expected stateless System Manager endpoint');
		if (notification && [200, 202, 204].includes(response.status)) return;
		try { return decodeResponse(response.status, response.contentType, response.text, requestId); }
		catch (error) { if (error instanceof RpcError && error.kind === 'rate-limit') rateLimited = true; throw error; }
	}
	return {
		async initialize() {
			const result = await rpc('initialize', { protocolVersion: '2025-06-18', capabilities: {},
				clientInfo: { name: 'ewl-hey-collection', version: '1' } });
			if (result?.protocolVersion !== '2025-06-18') throw new RpcError('unsupported protocol version');
			await rpc('notifications/initialized', {}, true);
		},
		async call(name, action, params = {}) {
			const allowed = { hey_identity: ['get_identity'], hey_boxes: ['list_boxes', 'get_box', 'get_box_posting_changes'], hey_threads: ['move_topic'] };
			if (!allowed[name]?.includes(action)) throw new Error('Operation is outside the collection surface.');
			const result = await rpc('tools/call', { name, arguments: { action, params } });
			const content = result?.content?.filter(item => item.type === 'text');
			if (content?.length !== 1) throw new RpcError('unexpected tool content');
			try { return JSON.parse(content[0].text); } catch { throw new RpcError('invalid tool JSON'); }
		}
	};
}

export function validateProfile(value) {
	if (!Array.isArray(value.categories) || !value.categories.length || value.categories.length > 100)
		throw new Error('Profile must contain 1–100 categories.');
	const names = new Set();
	for (const category of value.categories) {
		if (typeof category.name !== 'string' || !category.name.trim() || names.has(category.name)) throw new Error('Category names must be unique and nonempty.');
		names.add(category.name);
		for (const key of ['subjectPattern', 'senderPattern']) {
			if (key === 'senderPattern' && category[key] === undefined) continue;
			if (typeof category[key] !== 'string' || !category[key] || category[key].length > 1000) throw new Error('Invalid category pattern.');
			try { new RegExp(category[key], 'i'); } catch { throw new Error('Invalid category regular expression.'); }
		}
	}
	return { description: String(value.description ?? ''), categories: value.categories.map(c => ({ name: c.name,
		subjectPattern: c.subjectPattern, ...(c.senderPattern === undefined ? {} : { senderPattern: c.senderPattern }) })) };
}

function matcher(profile) {
	const categories = profile.categories.map(c => ({ ...c, subject: new RegExp(c.subjectPattern, 'i'),
		sender: c.senderPattern === undefined ? null : new RegExp(c.senderPattern, 'i') }));
	return row => categories.find(c => c.subject.test(row.name ?? '') && (!c.sender || c.sender.test(row.sender?.email_address ?? row.creator?.email_address ?? '')))?.name;
}

function topicId(row) {
	try {
		if (row.topic_id) return id(row.topic_id);
		const url = new URL(row.app_url);
		if (url.hostname === 'app.hey.com') return id(/^\/topics\/([1-9]\d*)\/?$/.exec(url.pathname)?.[1]);
	} catch { /* A bundle or an unknown URL cannot identify a topic safely. */ }
	return null;
}

function cursor(box) {
	for (const key of ['next_incremental_sync_url', 'posting_changes_url']) {
		try {
			const query = new URL(box[key], 'https://app.hey.com').searchParams;
			if (query.get('since')) return { since: query.get('since'), ...(query.get('v') ? { v: query.get('v') } : {}) };
		} catch { /* Full box reads remain available. */ }
	}
	return null;
}

export async function binding(call, endpoint, account) {
	const identity = await call('hey_identity', 'get_identity');
	const accounts = identity.accounts?.map(a => id(a.id));
	if (!accounts?.length || (!account && accounts.length !== 1)) throw new Error('Select --account explicitly when multiple accounts are linked.');
	const accountId = account ? id(account) : accounts[0];
	if (!accounts.includes(accountId)) throw new Error('Selected account is not available to the authenticated identity.');
	const result = unwrap(await call('hey_boxes', 'list_boxes'));
	const boxes = Array.isArray(result) ? result : result.boxes;
	if (!Array.isArray(boxes)) throw new RpcError('unexpected box catalog');
	const boxId = kind => {
		const matches = boxes.filter(b => b.kind === kind && (b.account_id === undefined || id(b.account_id) === accountId));
		if (matches.length !== 1) throw new Error('Mailbox box scope is ambiguous.');
		return id(matches[0].id);
	};
	const identityId = id(identity.id);
	return { endpoint: endpointUrl(endpoint), identityId, accountId, sourceBoxId: boxId('imbox'), destinationBoxId: boxId('asidebox'),
		mailboxKey: `${endpointUrl(endpoint)}|identity:${identityId}|account:${accountId}` };
}

export function assertBinding(manifest, current) {
	for (const key of ['endpoint', 'identityId', 'accountId', 'sourceBoxId', 'destinationBoxId', 'mailboxKey'])
		if (manifest[key] !== current[key]) throw new Error('Collection endpoint, identity, account, or box binding changed; no moves performed.');
}

export function newCollection(current, profile) {
	return { format, ...current, profile: validateProfile(profile), startedAt: now(), discovery: { complete: false, pages: 0,
		nextPage: null, cursors: [], seen: [], newerSkipped: 0, unknownDates: 0, unsupported: [] }, items: [], verificationCursor: null };
}

function boxRows(data, boxId) {
	const box = unwrap(data);
	if (!Array.isArray(box.postings) || id(box.id) !== boxId) throw new RpcError('unexpected box page');
	return box;
}

function checkNext(next, used) {
	if (next !== undefined && next !== null && (typeof next !== 'string' || !next || used.has(next)))
		throw new RpcError('invalid or repeated pagination cursor');
	return next ?? null;
}

export async function discover(m, call, save, maxPages = 100) {
	if (m.discovery.complete) return;
	const match = matcher(m.profile), seen = new Set(m.discovery.seen), used = new Set(m.discovery.cursors);
	for (let i = 0; i < maxPages; i++) {
		const current = m.discovery.nextPage;
		const data = await call('hey_boxes', 'get_box', { boxId: m.sourceBoxId, ...(current ? { page: current } : {}) });
		const box = boxRows(data, m.sourceBoxId);
		if (current) used.add(current);
		const next = checkNext(data.next_page, used);
		for (const row of box.postings) {
			const postingId = id(row.id);
			if (seen.has(postingId)) continue;
			seen.add(postingId);
			if (id(row.account_id) !== m.accountId || id(row.box_id) !== m.sourceBoxId) continue;
			const category = match(row);
			if (!category) continue;
			const created = Date.parse(row.created_at);
			if (created > Date.parse(m.startedAt)) { m.discovery.newerSkipped++; continue; }
			if (!Number.isFinite(created)) m.discovery.unknownDates++;
			const topic = topicId(row);
			if (row.kind !== 'topic' || !topic) {
				m.discovery.unsupported.push({ postingId, category, reason: 'No unambiguous topic posting' }); continue;
			}
			m.items.push({ postingId, topicId: topic, accountId: m.accountId, category,
				subject: String(row.name ?? '').slice(0, 500), sender: String(row.sender?.email_address ?? row.creator?.email_address ?? '').slice(0, 320),
				createdAt: row.created_at ?? null, originalSeen: row.seen ?? null, outcome: 'pending' });
		}
		m.discovery = { ...m.discovery, nextPage: next, pages: m.discovery.pages + 1, complete: !next,
			seen: [...seen], cursors: [...used] };
		save(m);
		if (!next) break;
	}
}

async function readBox(call, boxId, accept) {
	let page, initialCursor; const used = new Set();
	do {
		const data = await call('hey_boxes', 'get_box', { boxId, ...(page ? { page } : {}) });
		const box = boxRows(data, boxId);
		if (initialCursor === undefined) initialCursor = cursor(box);
		for (const row of box.postings) accept(row);
		if (page) used.add(page);
		page = checkNext(data.next_page, used);
	} while (page);
	return initialCursor;
}

export async function verify(m, call, save) {
	const outstanding = new Map(m.items.filter(inFlight).map(item => [item.postingId, item]));
	if (!outstanding.size) return;
	let found = new Map(), nextCursor = m.verificationCursor;
	const accept = row => {
		if (outstanding.has(String(row.id)) && id(row.box_id) === m.destinationBoxId && id(row.account_id) === m.accountId)
			found.set(id(row.id), { seen: row.seen ?? null });
	};
	let fullRead = !nextCursor;
	if (!fullRead) {
		try {
			let page; const used = new Set(), start = nextCursor;
			do {
				const data = await call('hey_boxes', 'get_box_posting_changes', { boxId: m.destinationBoxId, ...start,
					per_page: '100', ...(page ? { page } : {}) });
				const changes = unwrap(data);
				if (!['added', 'updated', 'deleted'].every(key => Array.isArray(changes[key]))) throw new RpcError('unexpected changes response');
				for (const row of [...changes.added, ...changes.updated]) accept(row.posting ?? row);
				for (const row of changes.deleted) found.delete(id(typeof row === 'object' ? row.id : row));
				if (page) used.add(page);
				page = checkNext(data.next_page, used);
				if (!page && data.next_since) nextCursor = { since: data.next_since, ...((data.next_v ?? start.v) ? { v: data.next_v ?? start.v } : {}) };
			} while (page);
		} catch (error) {
			if (!(error instanceof RpcError) || error.kind !== 'conflict') throw error;
			fullRead = true;
		}
	}
	if (fullRead) { found = new Map(); nextCursor = await readBox(call, m.destinationBoxId, accept); }
	for (const [postingId, observation] of found) {
		const item = outstanding.get(postingId); item.outcome = 'verified'; item.verifiedAt = now(); item.seenAfter = observation.seen;
	}
	m.verificationCursor = nextCursor; save(m);
}

export async function move(m, call, save, { batchSize = 50, batches = 1, concurrency = 4, retryReconciled = false, progress = () => {} } = {}) {
	if (!m.discovery.complete) throw new Error('Finish discovery before moving the selected snapshot.');
	const destination = Number(m.destinationBoxId);
	if (!Number.isSafeInteger(destination) || destination < 1) throw new Error('Destination box ID exceeds the supported JSON integer range.');
	if (m.items.some(inFlight)) { await verify(m, call, save); if (m.items.some(inFlight)) throw new Error('Reconcile uncertain moves before starting more.'); }
	if (!m.verificationCursor) m.verificationCursor = cursor(boxRows(await call('hey_boxes', 'get_box', { boxId: m.destinationBoxId }), m.destinationBoxId));
	for (let batch = 0; batch < batches; batch++) {
		const selected = m.items.filter(item => item.outcome === 'pending' || (retryReconciled && item.outcome === 'retryable')).slice(0, batchSize);
		if (!selected.length) break;
		for (const item of selected) { item.outcome = 'attempting'; item.attemptedAt = now(); }
		save(m); // Every potentially submitted write is durable before the first request leaves.
		let index = 0, failure;
		await Promise.all(Array.from({ length: Math.min(concurrency, selected.length) }, async () => {
			while (index < selected.length && !failure) {
				const item = selected[index++];
				try {
					await call('hey_threads', 'move_topic', { topicId: item.topicId, box_id: destination });
					item.outcome = 'accepted';
				} catch (error) {
					item.outcome = 'uncertain'; item.failureAt = now();
					item.failureKind = error instanceof RpcError ? error.kind : 'request interrupted'; failure = error;
				}
			}
		}));
		for (let i = index; i < selected.length; i++) selected[i].outcome = 'pending';
		save(m);
		// A 429 stops the command immediately; do not hammer the server with verification calls.
		if (failure instanceof RpcError && failure.kind === 'rate-limit') throw failure;
		await verify(m, call, save); progress(summary(m));
		if (failure || m.items.some(inFlight)) throw new Error('Batch stopped; inspect summary and reconcile before continuing.');
	}
}

export async function reconcile(m, call, save) {
	await verify(m, call, save);
	const unresolved = new Map(m.items.filter(item => inFlight(item) || item.outcome === 'missing').map(item => [item.postingId, item]));
	if (!unresolved.size) return;
	await readBox(call, m.sourceBoxId, row => {
		const item = unresolved.get(String(row.id));
		if (item && id(row.account_id) === m.accountId && id(row.box_id) === m.sourceBoxId && topicId(row) === item.topicId) {
			item.outcome = 'retryable'; item.reconciledAt = now(); unresolved.delete(item.postingId);
		}
	});
	for (const item of unresolved.values()) { item.outcome = 'missing'; item.reconciledAt = now(); }
	save(m); // Retryable is not pending: a later move requires explicit --retry-reconciled.
}

export async function audit(m, call, save) {
	const selected = new Map(m.items.map(item => [item.postingId, item])), destination = new Set(), source = new Set(), otherMatches = [];
	const match = matcher(m.profile);
	const destinationCursor = await readBox(call, m.destinationBoxId, row => {
		if (id(row.account_id) === m.accountId && id(row.box_id) === m.destinationBoxId && selected.has(String(row.id))) destination.add(id(row.id));
	});
	await readBox(call, m.sourceBoxId, row => {
		if (id(row.account_id) !== m.accountId || id(row.box_id) !== m.sourceBoxId) return;
		if (selected.has(String(row.id))) source.add(id(row.id));
		else if (match(row)) otherMatches.push({ postingId: id(row.id), topicId: topicId(row), category: match(row), createdAt: row.created_at ?? null });
	});
	m.verificationCursor = destinationCursor;
	m.audit = { at: now(), selectedInDestination: [...destination], selectedStillInSource: [...source], otherMatchingThreads: otherMatches,
		missingFromBoth: [...selected.keys()].filter(key => !source.has(key) && !destination.has(key)),
		complete: destination.size === selected.size && source.size === 0 };
	save(m);
}

export function summary(m, limit = 0) {
	const outcomes = {}, categories = {};
	for (const item of m.items) {
		outcomes[item.outcome] = (outcomes[item.outcome] ?? 0) + 1;
		const category = categories[item.category] ??= { selected: 0, verified: 0 };
		category.selected++; if (item.outcome === 'verified') category.verified++;
	}
	const groups = Object.keys(categories).map(category => m.items.filter(item => item.category === category));
	const sample = [];
	for (let index = 0; sample.length < limit && groups.some(group => index < group.length); index++)
		for (const group of groups) if (group[index] && sample.length < limit) sample.push(group[index]);
	return { mailboxKey: m.mailboxKey, snapshotStartedAt: m.startedAt, discoveryComplete: m.discovery.complete,
		pages: m.discovery.pages, postingsScanned: m.discovery.seen.length, newerSkipped: m.discovery.newerSkipped,
		unknownDates: m.discovery.unknownDates, unsupportedCount: m.discovery.unsupported.length, selected: m.items.length,
		outcomes, categories, allMovesVerified: m.discovery.complete && m.items.every(item => item.outcome === 'verified'),
		lastAudit: m.audit ? { at: m.audit.at, complete: m.audit.complete, selectedStillInSource: m.audit.selectedStillInSource.length,
			missingFromBoth: m.audit.missingFromBoth.length, otherMatchingThreads: m.audit.otherMatchingThreads.length } : null,
		...(limit ? { sample, sampleComplete: m.items.length <= limit, sampleMethod: 'round-robin across categories; ledger metadata, not a fresh mailbox read' } : {}) };
}

export function saveManifest(file, value) {
	fs.writeFileSync(file + '.tmp', JSON.stringify(value, null, 2), { flush: true });
	fs.renameSync(file + '.tmp', file);
}

const help = `HEY collection through the stateless System Manager MCP endpoint (Node.js 22.13+).
Commands: discover, move, reconcile, audit, summarize
Required: --collection NAME; discover also requires --profile FILE.
Connection: --endpoint URL or SYSTEM_MANAGER_MCP_URL; token from SYSTEM_MANAGER_API_TOKEN.
Options: --token-env NAME, --account ID, --state DIR, --insecure-local,
         --pages 100 (discover), --batches 1 --batch-size 50 --concurrency 4 (move),
         --retry-reconciled (move), --interval-ms 300, --limit 20 (summarize).
Default storage: <EwlFolderPath or platform EWL folder>/Error Triage/HEY/collections/NAME.json.
Summarize is offline. No commands read bodies, send, draft, trash, or change dispositions.
See hey-collect.md for discovery, recovery, auditing, and profile semantics.`;

async function main() {
	const [command, ...args] = process.argv.slice(2);
	if (['--help', 'help', undefined].includes(command)) { console.log(help); return; }
	if (!['discover', 'move', 'reconcile', 'audit', 'summarize'].includes(command)) throw new Error('Unknown command. Use --help.');
	const values = {}, flags = new Set(['insecure-local', 'retry-reconciled']);
	const names = new Set(['collection', 'profile', 'endpoint', 'token-env', 'account', 'state', 'pages', 'batches', 'batch-size', 'concurrency', 'interval-ms', 'limit', ...flags]);
	for (let i = 0; i < args.length; i++) {
		const key = args[i].slice(2);
		if (!args[i].startsWith('--') || !names.has(key) || key in values) throw new Error('Unknown or duplicate option.');
		values[key] = flags.has(key) ? true : args[++i];
		if (values[key] === undefined || values[key] === '') throw new Error('Missing option value.');
	}
	const permitted = new Set(['collection', 'state', ...(command === 'summarize' ? ['limit'] :
		['endpoint', 'token-env', 'account', 'insecure-local', 'interval-ms']),
		...(command === 'discover' ? ['profile', 'pages'] : command === 'move' ? ['batches', 'batch-size', 'concurrency', 'retry-reconciled'] : [])]);
	if (Object.keys(values).some(key => !permitted.has(key))) throw new Error('An option does not apply to this command.');
	if (!/^[a-zA-Z0-9][a-zA-Z0-9._-]{0,79}$/.test(values.collection ?? '')) throw new Error('Supply a simple --collection name, not a path.');
	const number = (key, fallback, maximum) => {
		const result = Number(values[key] ?? fallback);
		if (!Number.isSafeInteger(result) || result < 1 || result > maximum) throw new Error(`Invalid --${key}.`);
		return result;
	};
	const settings = { batchSize: number('batch-size', 50, 500), batches: number('batches', 1, 10000), concurrency: number('concurrency', 4, 8),
		retryReconciled: !!values['retry-reconciled'], progress: value => console.log(JSON.stringify(value)) };
	const interval = number('interval-ms', 300, 60000);
	if (interval < 100) throw new Error('--interval-ms must be at least 100; honor server rate limits.');
	const directory = path.join(stateDirectory(values.state), 'collections'), file = path.join(directory, values.collection + '.json');
	fs.mkdirSync(directory, { recursive: true });
	let lock;
	try { lock = fs.openSync(file + '.lock', 'wx'); }
	catch { throw new Error('Collection is locked. See recovery instructions before removing a stale lock.'); }
	const controller = new AbortController(), interrupt = () => controller.abort();
	process.on('SIGINT', interrupt);
	try {
		fs.writeFileSync(lock, JSON.stringify({ pid: process.pid, startedAt: now() }));
		let m = fs.existsSync(file) ? JSON.parse(fs.readFileSync(file, 'utf8')) : null;
		if (m && (m.format !== format || !Array.isArray(m.items) || m.items.some(item => !states.includes(item.outcome))))
			throw new Error('Unsupported or invalid collection manifest; do not import an ad hoc ledger by renaming it.');
		if (m) {
			validateProfile(m.profile);
			const identifiers = new Set();
			for (const item of m.items) {
				id(item.topicId);
				const posting = id(item.postingId);
				if (id(item.accountId) !== m.accountId || identifiers.has(posting)) throw new Error('Manifest contains a duplicate posting or account mismatch.');
				identifiers.add(posting);
			}
		}
		if (command === 'summarize') {
			if (!m) throw new Error('Collection not found.');
			console.log(JSON.stringify(summary(m, number('limit', 20, 100)))); return;
		}
		const endpoint = endpointUrl(values.endpoint ?? process.env.SYSTEM_MANAGER_MCP_URL);
		if (m && m.endpoint !== endpoint) throw new Error('Collection endpoint changed.');
		const client = createCaller({ endpoint, token: process.env[values['token-env'] ?? 'SYSTEM_MANAGER_API_TOKEN'],
			insecureLocal: !!values['insecure-local'], interval, signal: controller.signal });
		await client.initialize();
		const call = client.call;
		const current = await binding(call, endpoint, values.account ?? m?.accountId);
		if (m) assertBinding(m, current);
		const save = value => saveManifest(file, value);
		if (command === 'discover') {
			if (!values.profile) throw new Error('Discover requires a trusted local --profile file.');
			const profile = validateProfile(JSON.parse(fs.readFileSync(values.profile, 'utf8')));
			if (m && JSON.stringify(m.profile) !== JSON.stringify(profile)) throw new Error('Profile changed; use a new collection name.');
			if (!m) { m = newCollection(current, profile); save(m); }
			await discover(m, call, save, number('pages', 100, 100000));
		} else {
			if (!m) throw new Error('Discover a collection first.');
			if (command === 'move') await move(m, call, save, settings);
			else if (command === 'reconcile') await reconcile(m, call, save);
			else await audit(m, call, save);
		}
		console.log(JSON.stringify({ manifest: file, ...summary(m) }));
	} finally {
		process.removeListener('SIGINT', interrupt); fs.closeSync(lock); fs.unlinkSync(file + '.lock');
	}
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url))
	main().catch(error => { console.error(error instanceof RpcError ? error.message : 'Collection stopped: ' +
		(error instanceof SyntaxError ? 'invalid JSON; contents were not logged' : error.code ? 'local file operation failed' : error.message)); process.exitCode = 1; });
