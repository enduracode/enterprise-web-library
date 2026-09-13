import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { DatabaseSync } from 'node:sqlite';

// All offsets are byte offsets. Never read a whole mailbox into memory.
const [command, mailbox, ...args] = process.argv.slice(2);
const option = (name, fallback) => args.includes(name) ? args[args.indexOf(name) + 1] : fallback;
const hash = value => crypto.createHash('sha256').update(value).digest('hex');
function headers(text) {
	const end = text.search(/\r?\n\r?\n/);
	const result = {};
	for (const line of text.slice(0, end < 0 ? text.length : end).replace(/\r?\n[ \t]+/g, ' ').split(/\r?\n/)) {
		const match = /^([^:]+):\s*(.*)$/.exec(line);
		if (match) result[match[1].toLowerCase()] = match[2];
	}
	return { values: result, body: end < 0 ? '' : text.slice(end + (text[end] === '\r' ? 4 : 2)) };
}
function decode(value, encoding, charset = 'utf-8') {
	let bytes;
	if (/base64/i.test(encoding)) bytes = Buffer.from(value.replace(/\s/g, ''), 'base64');
	else if (/quoted-printable/i.test(encoding)) bytes = Buffer.from(value.replace(/=\r?\n/g, '').replace(/=([\da-f]{2})/gi, (_, n) => String.fromCharCode(parseInt(n, 16))), 'latin1');
	else bytes = Buffer.from(value, 'latin1');
	try { return new TextDecoder(charset).decode(bytes); } catch { return bytes.toString('utf8'); }
}
function subject(value = '') {
	return value.replace(/\?=\s+(?==\?)/g, '?=').replace(/=\?([^?]+)\?([bq])\?([^?]*)\?=/gi,
		(_, charset, encoding, data) => decode(encoding.toLowerCase() === 'q' ? data.replace(/_/g, ' ') : data,
			encoding.toLowerCase() === 'b' ? 'base64' : 'quoted-printable', charset));
}
function bodyText(raw, depth = 0) {
	if (depth > 12) return '';
	const { values: h, body } = headers(raw);
	if (/attachment/i.test(h['content-disposition'] || '')) return '';
	const type = h['content-type'] || 'text/plain';
	const boundary = /boundary=(?:"([^"]+)"|([^;\s]+))/i.exec(type);
	if (/multipart\//i.test(type) && boundary) {
		const parts = body.split('--' + (boundary[1] || boundary[2])).slice(1).filter(p => !p.startsWith('--'));
		const plain = parts.filter(p => /content-type:\s*text\/plain/i.test(p));
		return (plain.length ? plain : parts).map(p => bodyText(p.replace(/^\r?\n/, ''), depth + 1)).join('\n');
	}
	if (!/^text\/(plain|html)/i.test(type)) return '';
	const charset = /charset=(?:"([^"]+)"|([^;\s]+))/i.exec(type);
	const text = decode(body, h['content-transfer-encoding'] || '', charset?.[1] || charset?.[2] || 'utf-8');
	return /text\/html/i.test(type) ? text.replace(/<br\s*\/?\s*>|<\/p>/gi, '\n').replace(/<[^>]*>/g, '').replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&quot;/g, '"').replace(/&#39;/g, "'").replace(/&amp;/g, '&') : text;
}
function main() {
	if (!['scan', 'list', 'detail', 'mark'].includes(command) || !mailbox) throw Error('Usage: node mailbox.mjs scan|list|detail|mark <mbox> [options]');
	const source = path.resolve(mailbox), stat = fs.statSync(source);
	const state = path.resolve(option('--state', source + '.triage'));
	fs.mkdirSync(state, { recursive: true });
	const db = new DatabaseSync(path.join(state, 'index.sqlite'));
	db.exec(`PRAGMA journal_mode=WAL;
		CREATE TABLE IF NOT EXISTS meta(key TEXT PRIMARY KEY,value TEXT);
		CREATE TABLE IF NOT EXISTS reports(id TEXT PRIMARY KEY,start INTEGER,length INTEGER,date TEXT,subject TEXT,preview TEXT,handled INTEGER DEFAULT 0,note TEXT DEFAULT '');
		CREATE INDEX IF NOT EXISTS reports_date ON reports(handled,date);
		CREATE TABLE IF NOT EXISTS dispositions(id TEXT PRIMARY KEY,handled INTEGER,note TEXT);`);
	const get = key => db.prepare('SELECT value FROM meta WHERE key=?').get(key)?.value;
	const set = (key, value) => db.prepare('INSERT OR REPLACE INTO meta VALUES(?,?)').run(key, String(value));
	const identity = JSON.stringify([source, stat.size, stat.mtimeMs]);
	const now = new Date(); now.setUTCFullYear(now.getUTCFullYear() - 1);
	const since = option('--since', get('since') || now.toISOString().slice(0, 10));
	if (!/^\d{4}-\d{2}-\d{2}$/.test(since) || !Number.isFinite(Date.parse(since))) throw Error('Invalid --since date');
	if (get('identity') !== identity || get('since') !== since) {
		if (command !== 'scan') throw Error('Mailbox or cutoff changed; run scan first using the same state directory.');
		db.exec('DELETE FROM reports; DELETE FROM meta;');
		set('identity', identity); set('since', since);
	}
	const fd = fs.openSync(source, 'r');
	const read = (start, length) => { const b = Buffer.alloc(Math.min(length, 2 * 1024 * 1024)); return b.subarray(0, fs.readSync(fd, b, 0, b.length, start)).toString('latin1'); };
	try {
		if (command === 'scan') {
			const seconds = Number(option('--seconds', '90'));
			if (!(seconds > 0 && seconds <= 120)) throw Error('--seconds must be between 0 and 120');
			const deadline = performance.now() + seconds * 1000;
			let position = Number(get('position') || 0), start = Number(get('start') || -1);
			let scanned = Number(get('scanned') || 0), unknownDates = Number(get('unknownDates') || 0);
			const insert = db.prepare('INSERT OR IGNORE INTO reports(id,start,length,date,subject,preview,handled,note) VALUES(?,?,?,?,?,?,COALESCE((SELECT handled FROM dispositions WHERE id=?),0),COALESCE((SELECT note FROM dispositions WHERE id=?),\'\'))');
			const processMessage = end => {
				if (start < 0) return;
				scanned++;
				const rawHeaders = read(start, Math.min(end - start, 65536));
				const h = headers(rawHeaders).values;
				const time = Date.parse(h.date);
				if (!Number.isFinite(time)) unknownDates++;
				else if (time < Date.parse(since)) return;
				const title = subject(h.subject);
				if (!/\b(error|exception|fault)\b/i.test(title)) return;
				const raw = read(start, end - start), text = bodyText(raw);
				const id = hash(h['message-id']?.trim() || raw);
				insert.run(id, start, end - start, Number.isFinite(time) ? new Date(time).toISOString() : null, title, text.slice(0, 1400), id, id);
			};
			const buffer = Buffer.alloc(4 * 1024 * 1024);
			while (position < stat.size && performance.now() < deadline) {
				const count = fs.readSync(fd, buffer, 0, buffer.length, position);
				if (!count) break;
				const final = position + count === stat.size;
				// Retain overlap so a separator split across reads is recognized exactly once.
				const advance = final ? count : count - 256;
				const text = buffer.toString('latin1', 0, count);
				const previous = position ? read(position - 1, 1) : '\n';
				const re = /(?:^|\n)From [^\r\n]*\r?\n/g;
				db.exec('BEGIN');
				for (const match of text.matchAll(re)) {
					const offset = match.index + (match[0][0] === '\n' ? 1 : 0);
					if (offset >= advance || (offset === 0 && previous !== '\n')) continue;
					const next = position + offset;
					if (next <= start) continue;
					processMessage(next); start = next;
				}
				position += advance;
				if (final) { processMessage(stat.size); start = stat.size; }
				for (const [key, value] of Object.entries({position, start, scanned, unknownDates})) set(key, value);
				db.exec('COMMIT');
			}
			console.log(JSON.stringify({bytesScanned: position, totalBytes: stat.size, complete: position === stat.size, messagesScanned: scanned, unknownDates, since, indexed: db.prepare('SELECT count(*) AS count FROM reports').get().count, state}));
		} else if (command === 'list') {
			const limit = Number(option('--limit', '30')), offset = Number(option('--offset', '0'));
			if (!Number.isInteger(limit) || limit < 1 || limit > 100 || !Number.isInteger(offset) || offset < 0) throw Error('Invalid pagination');
			const filter = '%' + option('--system', '').replace(/[\%_]/g, '\\$&') + '%';
			const where = "handled=0 AND subject LIKE ? ESCAPE '\\'";
			const order = option('--order', 'recent');
			if (!['recent', 'frequent'].includes(order)) throw Error('--order must be recent|frequent');
			const query = order === 'frequent'
				? 'SELECT id,MAX(date) AS date,subject,substr(preview,1,700) AS preview,count(*) AS exactPreviewCount FROM reports WHERE ' + where + ' GROUP BY subject,preview ORDER BY exactPreviewCount DESC,date DESC,id LIMIT ? OFFSET ?'
				: 'SELECT id,date,subject,substr(preview,1,700) AS preview FROM reports WHERE ' + where + ' ORDER BY date DESC,id LIMIT ? OFFSET ?';
			console.log(JSON.stringify({complete: Number(get('position')) === stat.size, since, pending: db.prepare('SELECT count(*) AS count FROM reports WHERE ' + where).get(filter).count,
				reports: db.prepare(query).all(filter, limit, offset)}));
		} else {
			const ids = option('--ids', '').split(',').filter(Boolean);
			if (!ids.length || ids.length > 20) throw Error('Supply 1–20 exact --ids');
			const rows = ids.map(id => { const row = db.prepare('SELECT * FROM reports WHERE id=?').get(id); if (!row) throw Error('Unknown report ID: ' + id); return row; });
			if (command === 'detail') {
				if (rows.length > 5) throw Error('Detail accepts at most five IDs');
				console.log(JSON.stringify(rows.map(row => {
					const text = bodyText(read(row.start, row.length));
					return {id: row.id, subject: row.subject, date: row.date, text: text.slice(0, 12000), possiblyTruncated: row.length > 2 * 1024 * 1024 || text.length > 12000};
				})));
			}
			else {
				const status = option('--status', ''); if (!['handled', 'pending'].includes(status)) throw Error('Supply --status handled|pending');
				const note = option('--note', '');
				db.exec('BEGIN');
				for (const row of rows) {
					db.prepare('INSERT OR REPLACE INTO dispositions VALUES(?,?,?)').run(row.id, Number(status === 'handled'), note);
					db.prepare('UPDATE reports SET handled=?,note=? WHERE id=?').run(Number(status === 'handled'), note, row.id);
				}
				db.exec('COMMIT'); console.log(JSON.stringify({ids, status, note}));
			}
		}
	} finally { fs.closeSync(fd); db.close(); }
}
try { main(); } catch (error) { console.error(error.message); process.exitCode = 1; }
