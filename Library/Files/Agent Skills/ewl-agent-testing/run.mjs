import { randomUUID } from 'node:crypto';
import { mkdir, readFile, realpath, rename, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// Connect only to the permanent server. No executable discovery, service startup,
// credential values in artifacts, global config changes, or automatic approvals.
export function connection(server, password) {
  const url = new URL(server);
  if (url.protocol !== 'http:' || !['127.0.0.1', '[::1]'].includes(url.hostname) || url.username || url.password || url.pathname !== '/' || url.search || url.hash)
    throw new Error('Use an explicit HTTP loopback server URL without credentials or a path.');
  if (typeof password !== 'string' || !password) throw new Error('Connection file must contain a nonempty password.');
  return async (route, method = 'GET', body, timeout = 30000, missing = false) => {
    const response = await fetch(new URL(route, url), {
      method, headers: { Authorization: `Basic ${Buffer.from(`opencode:${password}`).toString('base64')}`, 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body), signal: AbortSignal.timeout(timeout), redirect: 'error',
    });
    if (missing && response.status === 404) return undefined;
    if (!response.ok) throw new Error(`${method} ${route}: HTTP ${response.status}`);
    return response.status === 204 ? undefined : response.json();
  };
}

export async function listSessions(request, filters) {
  const result = [];
  let query = new URLSearchParams({ limit: '100', ...filters });
  do {
    const page = await request(`/api/session?${query}`);
    result.push(...page.data);
    query = page.cursor.next ? new URLSearchParams({ cursor: page.cursor.next }) : undefined;
  } while (query);
  return result;
}

async function save(file, manifest) {
  const temporary = `${file}.${randomUUID()}.tmp`;
  await writeFile(temporary, JSON.stringify(manifest, null, 2), { flag: 'wx' });
  await rename(temporary, file);
}

function verify(info, manifest, parentID) {
  const normalize = p => process.platform === 'win32' ? path.resolve(p).toLowerCase() : path.resolve(p);
  if (!/^ses_[A-Za-z0-9]+$/.test(info.id) || normalize(info.location.directory) !== normalize(manifest.directory) ||
      (parentID ? info.parentID !== parentID : info.parentID || info.title !== manifest.title))
    throw new Error('Session ownership mismatch; refusing to operate on it.');
}

async function collect(request, file, manifest) {
  const visit = async (id, parentID) => {
    const { data: info } = await request(`/api/session/${id}`);
    verify(info, manifest, parentID);
    if (!manifest.sessions.some(s => s.id === id)) {
      manifest.sessions.push({ id, parentID });
      await save(file, manifest);
    }
    const exported = await request(`/api/experimental/session/${id}/export`);
    await writeFile(path.join(path.dirname(file), `${id}.json`), JSON.stringify(exported, null, 2));
    for (const child of await listSessions(request, { parentID: id })) await visit(child.id, id);
  };
  await visit(manifest.root, null);
}

export async function main(args = process.argv.slice(2)) {
  const [command, ...rest] = args;
  if (!command || command === '--help') {
    console.log('run.mjs run --server http://127.0.0.1:45278 --connection-file <private JSON> --dir <system> --model <provider/model> --title <scenario> --output <workspace> --prompt-file <UTF-8 file> [--timeout-ms 300000]\nrun.mjs export|cleanup --manifest <manifest.json>');
    return;
  }
  if (!['run', 'export', 'cleanup'].includes(command)) throw new Error('Unknown command.');
  const options = {};
  const allowed = command === 'run' ? ['server', 'connection-file', 'dir', 'model', 'title', 'output', 'prompt-file', 'timeout-ms'] : ['manifest'];
  for (let i = 0; i < rest.length; i += 2) {
    const key = rest[i].replace(/^--/, '');
    if (!rest[i].startsWith('--') || !allowed.includes(key) || key in options || rest[i + 1] === undefined) throw new Error('Invalid or duplicate option.');
    options[key] = rest[i + 1];
  }
  let file, manifest;
  if (command === 'run') {
    for (const key of allowed.filter(key => key !== 'timeout-ms')) if (!options[key]) throw new Error(`Missing --${key}.`);
    const timeout = Number(options['timeout-ms'] ?? 300000);
    if (!Number.isSafeInteger(timeout) || timeout < 1 || timeout > 2147483647) throw new Error('Invalid timeout.');
    const id = randomUUID();
    const directory = await realpath(options.dir);
    const artifacts = path.join(await realpath(options.output), `agent-test-${id}`);
    await mkdir(artifacts);
    file = path.join(artifacts, 'manifest.json');
    manifest = { schema: 2, server: options.server, connectionFile: await realpath(options['connection-file']), directory,
      title: `scen-${options.title}-${id}`, model: options.model, timeout, sessions: [] };
    await save(file, manifest);
  } else {
    file = await realpath(options.manifest);
    manifest = JSON.parse(await readFile(file, 'utf8'));
    if (manifest.schema !== 2 || !manifest.title?.startsWith('scen-') || !manifest.root || !Array.isArray(manifest.sessions)) throw new Error('Invalid runner manifest.');
  }
  const credential = JSON.parse(await readFile(manifest.connectionFile, 'utf8'));
  const request = connection(manifest.server, credential.password);
  const info = await request('/api/info');
  if (!info.version.startsWith('2.')) throw new Error('Unsupported OpenCode server version.');
  if (command === 'run') {
    console.log(`Manifest: ${file}`);
    const [providerID, model] = manifest.model.split(/\/(.*)/s);
    const [id, variant] = model?.split('#') ?? [];
    if (!providerID || !id) throw new Error('Expected provider/model[#variant].');
    const prompt = (await readFile(options['prompt-file'], 'utf8')).replace(/^\uFEFF/, '');
    if (!prompt.trim()) throw new Error('Empty prompt.');
    const { data: session } = await request('/api/session', 'POST', {
      title: manifest.title, location: { directory: manifest.directory }, agent: 'build', model: { providerID, id, ...(variant ? { variant } : {}) },
    });
    manifest.root = session.id;
    manifest.sessions.push({ id: session.id, parentID: null });
    await save(file, manifest);
    try {
      await request(`/api/session/${session.id}/prompt`, 'POST', { text: prompt });
      await request(`/api/experimental/session/${session.id}/wait`, 'POST', undefined, manifest.timeout);
    } catch (error) {
      manifest.failure = error.message;
      await request(`/api/session/${session.id}/interrupt`, 'POST', { continue: false });
    } finally {
      await save(file, manifest);
      await collect(request, file, manifest);
    }
    console.log(JSON.stringify({ manifest: file, root: manifest.root, failure: manifest.failure }));
    if (manifest.failure) throw new Error(manifest.failure);
  } else if (command === 'export') {
    await collect(request, file, manifest);
  } else {
    if (await request(`/api/session/${manifest.root}`, 'GET', undefined, 30000, true)) await collect(request, file, manifest);
    // Verify every recorded entry before deleting any, including after a partial prior cleanup.
    for (const session of manifest.sessions) {
      const found = await request(`/api/session/${session.id}`, 'GET', undefined, 30000, true);
      if (found) verify(found.data, manifest, session.parentID);
    }
    for (const session of [...manifest.sessions].reverse()) {
      if (await request(`/api/session/${session.id}`, 'GET', undefined, 30000, true)) await request(`/api/session/${session.id}`, 'DELETE');
    }
    for (const session of manifest.sessions) if (await request(`/api/session/${session.id}`, 'GET', undefined, 30000, true)) throw new Error('Session remains after deletion.');
    manifest.cleanedAt = new Date().toISOString();
    await save(file, manifest);
    console.log(`Deleted ${manifest.sessions.length} owned sessions; artifacts retained.`);
  }
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main().catch(error => { console.error(error.message); process.exitCode = 1; });
}
