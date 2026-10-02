import fs from 'node:fs';
import path from 'node:path';
import { DatabaseSync } from 'node:sqlite';
import { stateDirectory } from './hey-paths.mjs';

// Metadata only. This helper has no mailbox/network access.
function main() {
	const [command, ...args] = process.argv.slice(2);
	const common = ['state', 'mailbox'];
	const options = {
		location: ['state'],
		record: [...common, 'topic', 'message', 'system', 'installation', 'observed'],
		list: [...common, 'status', 'messages', 'limit', 'offset'],
		mark: [...common, 'messages', 'status', 'note'],
		mailbox: [...common, 'messages', 'action', 'outcome', 'note']
	};
	if (!options[command]) throw new Error('Use location, record, list, mark, or mailbox. See hey.md.');
	const values = {};
	for (let i = 0; i < args.length; i += 2) {
		const key = args[i].replace(/^--/, '');
		if (!args[i].startsWith('--') || !options[command].includes(key) || key in values || args[i + 1] === undefined)
			throw new Error('Unknown, duplicate, or incomplete option.');
		values[key] = args[i + 1];
	}
	const required = key => {
		if (!values[key]?.trim() || values[key].length > 2000) throw new Error(`Missing or oversized --${key}.`);
		return values[key];
	};
	const state = stateDirectory(values.state);
	const databasePath = path.join(state, 'hey-state.sqlite');
	if (command === 'location') return { stateDirectory: state, databasePath };
	const mailbox = required('mailbox');
	const statuses = ['pending', 'handled', 'resolved'];
	const status = values.status ?? 'pending';
	if (command === 'mark' && (!values.status || !statuses.includes(status))) throw new Error('Invalid disposition status.');
	if (command === 'list' && ![...statuses, 'all'].includes(status)) throw new Error('Invalid list status.');
	if (command === 'mailbox' && (!['set_aside', 'trash', 'restore'].includes(values.action) ||
		!['pending', 'verified', 'failed', 'uncertain'].includes(values.outcome))) throw new Error('Invalid mailbox action/outcome.');
	const number = (key, fallback, min, max) => {
		const value = Number(values[key] ?? fallback);
		if (!Number.isSafeInteger(value) || value < min || value > max) throw new Error(`Invalid --${key}.`);
		return value;
	};
	const limit = number('limit', 30, 1, 100), offset = number('offset', 0, 0, Number.MAX_SAFE_INTEGER);
	const messages = ['mark', 'mailbox'].includes(command) || (command === 'list' && values.messages !== undefined)
		? [...new Set(required('messages').split(','))] : [];
	if (messages.some(id => !id.trim()) || messages.length > 100) throw new Error('Supply 1–100 exact message IDs.');
	if (command === 'record') { required('topic'); required('message'); }
	if (command === 'mark' || command === 'mailbox') required('note');
	fs.mkdirSync(state, { recursive: true });
	const db = new DatabaseSync(databasePath);
	try {
		db.exec(`PRAGMA busy_timeout=5000;
			CREATE TABLE IF NOT EXISTS reports (
				mailbox TEXT NOT NULL, message_id TEXT NOT NULL, topic_id TEXT NOT NULL,
				system TEXT NOT NULL, installation TEXT NOT NULL, observed TEXT NOT NULL,
				status TEXT NOT NULL DEFAULT 'pending', note TEXT NOT NULL DEFAULT '',
				mailbox_action TEXT NOT NULL DEFAULT '', mailbox_outcome TEXT NOT NULL DEFAULT '',
				mailbox_note TEXT NOT NULL DEFAULT '', updated TEXT NOT NULL,
				PRIMARY KEY(mailbox, message_id));`);
		const now = new Date().toISOString();
		if (command === 'list') {
			let filter = status === 'all' ? 'mailbox = ?' : 'mailbox = ? AND status = ?';
			const bindings = status === 'all' ? [mailbox] : [mailbox, status];
			if (messages.length) { filter += ` AND message_id IN (${messages.map(() => '?').join(',')})`; bindings.push(...messages); }
			const total = db.prepare(`SELECT COUNT(*) AS count FROM reports WHERE ${filter}`).get(...bindings).count;
			const reports = db.prepare(`SELECT * FROM reports WHERE ${filter} ORDER BY updated DESC, message_id LIMIT ? OFFSET ?`).all(...bindings, limit, offset);
			return { total, reports, nextOffset: offset + reports.length < total ? offset + reports.length : null };
		}
		db.exec('BEGIN IMMEDIATE');
		try {
			const get = db.prepare('SELECT * FROM reports WHERE mailbox = ? AND message_id = ?');
			if (command === 'record') {
				const old = get.get(mailbox, values.message);
				if (old && old.topic_id !== values.topic) throw new Error('Known message has a different topic; reconcile before changing it.');
				db.prepare(`INSERT INTO reports (mailbox,message_id,topic_id,system,installation,observed,updated)
					VALUES (?,?,?,?,?,?,?) ON CONFLICT(mailbox,message_id) DO NOTHING`).run(
					mailbox, values.message, values.topic, values.system ?? '', values.installation ?? '', values.observed ?? '', now);
			}
			else {
				if (messages.some(id => !get.get(mailbox, id))) throw new Error('Unknown source ID; no dispositions changed.');
				const statement = command === 'mark'
					? db.prepare('UPDATE reports SET status = ?, note = ?, updated = ? WHERE mailbox = ? AND message_id = ?')
					: db.prepare('UPDATE reports SET mailbox_action = ?, mailbox_outcome = ?, mailbox_note = ?, updated = ? WHERE mailbox = ? AND message_id = ?');
				for (const id of messages) statement.run(...(command === 'mark'
					? [status, values.note, now, mailbox, id]
					: [values.action, values.outcome, values.note, now, mailbox, id]));
			}
			db.exec('COMMIT');
			return { recorded: command === 'record' ? 1 : 0, changed: messages.length };
		}
		catch (error) { db.exec('ROLLBACK'); throw error; }
	}
	finally { db.close(); }
}

try { console.log(JSON.stringify(main())); }
catch (error) { console.error(error.message); process.exitCode = 1; }
