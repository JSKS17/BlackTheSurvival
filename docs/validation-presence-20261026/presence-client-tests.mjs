import assert from 'node:assert/strict';
import { createHash, randomUUID } from 'node:crypto';
import fs from 'node:fs/promises';
import vm from 'node:vm';
import path from 'node:path';

const root = path.resolve(import.meta.dirname, '../..');
const sourcePath = path.join(root, 'Assets/WebGLTemplates/BlackTheSurvival/TemplateData/presence.js');
const source = await fs.readFile(sourcePath, 'utf8');
const checks = [];
const accountId = '12345678-1234-1234-1234-123456789abc';
const csrfToken = 'a'.repeat(64);
function check(name, fn) { fn(); checks.push(name); }
async function flush() { for (let i = 0; i < 16; i++) await Promise.resolve(); }

function harness(options = {}) {
  let now = 1000000, sequence = 0;
  const listeners = new Map(), documentListeners = new Map(), timers = new Map();
  const calls = [], messages = [], sent = [];
  const parent = { postMessage: (value, origin) => messages.push({ value, origin }) };
  const window = {
    parent, addEventListener(type, cb) { const list = listeners.get(type) || []; list.push(cb); listeners.set(type, list); },
    blackTheSurvival: options.withoutUnity ? undefined : { unityInstance: { SendMessage: (...args) => sent.push(args) } },
    BTSGamePresenceState: options.queuedState
  };
  if (options.standalone) window.parent = window;
  const document = { hidden: false, addEventListener(type, cb) { documentListeners.set(type, cb); } };
  const navigator = { onLine: true };
  const context = vm.createContext({
    window, document, navigator, location: { origin: 'https://example.test' }, crypto: { randomUUID }, AbortController,
    Date: { now: () => now },
    setTimeout(cb, delay) { const id = ++sequence; timers.set(id, { at: now + delay, cb }); return id; },
    clearTimeout(id) { timers.delete(id); },
    fetch(url, init) {
      let resolve, reject;
      const promise = new Promise((a, b) => { resolve = a; reject = b; });
      const call = { url, init, body: JSON.parse(init.body), resolve: (status, value) => resolve({ status, ok: status >= 200 && status < 300, json: async () => value }), reject };
      calls.push(call);
      if (init.signal) init.signal.addEventListener('abort', () => reject(new Error('Aborted')));
      if (!call.body.active) call.resolve(200, { count: 0 });
      return promise;
    }
  });
  vm.runInContext(source, context, { filename: sourcePath });
  function event(type, value = {}) { for (const cb of listeners.get(type) || []) cb(value); }
  function message(value, origin = 'https://example.test', sender = parent) { event('message', { origin, source: sender, data: value }); }
  function session(active = true, fields = {}) { message({ type: 'bts:presence-session', active, accountId, csrfToken, ...fields }); }
  function ready(value = true, lobbyVisible = true) { window.BTSPresenceBridge.setGameState({ ready: value, lobbyVisible }); }
  async function advance(amount) {
    now += amount;
    for (const [id, timer] of [...timers]) if (timers.has(id) && timer.at <= now) { timers.delete(id); timer.cb(); }
    await flush();
  }
  return { window, document, navigator, calls, messages, sent, timers, parent, event, message, session, ready, advance,
    visible() { document.hidden = false; documentListeners.get('visibilitychange')?.(); },
    jump(amount) { now += amount; }
  };
}

const secured = harness();
secured.ready();
secured.message({ type: 'bts:presence-session', active: true, accountId, csrfToken }, 'https://evil.test');
secured.message({ type: 'bts:presence-session', active: true, accountId, csrfToken }, 'https://example.test', {});
secured.session(true, { csrfToken: 'bad' });
check('wrong origin, wrong frame, and malformed credential cannot create heartbeats', () => assert.equal(secured.calls.length, 0));
secured.session();
check('only ready authenticated player starts a heartbeat', () => {
  assert.equal(secured.calls.length, 1);
  assert.equal(secured.calls[0].init.credentials, 'same-origin');
  assert.equal(secured.calls[0].init.headers['X-BTS-ACCOUNT'], accountId);
  assert.equal(secured.calls[0].init.headers['X-BTS-CSRF'], csrfToken);
  assert.match(secured.calls[0].body.clientId, /^[a-f0-9-]{36}$/);
  assert.equal(secured.calls[0].body.active, true);
});
secured.calls[0].resolve(200, { count: 7, heartbeatMs: 30000, ttlMs: 90000 }); await flush();
check('server-confirmed count reaches the fullscreen-safe Unity receiver', () => assert.deepEqual(secured.sent.at(-1), ['Black The Survival', 'BrowserPresenceCount', '7']));
secured.ready(true, false);
check('leaving the lobby continues counting the active player without another request', () => assert.equal(secured.calls.length, 1));
await secured.advance(29999);
check('heartbeat does not spam before thirty seconds', () => assert.equal(secured.calls.length, 1));
await secured.advance(1);
check('heartbeat repeats after thirty seconds with the same active-tab identifier', () => {
  assert.equal(secured.calls.length, 2);
  assert.equal(secured.calls[1].body.clientId, secured.calls[0].body.clientId);
});
secured.calls[1].resolve(503, { error: 'busy' }); await flush();
check('temporary network/server failure clears the count instead of inventing zero', () => assert.equal(secured.sent.at(-1)[2], '-1'));
await secured.advance(30000);
secured.calls.at(-1).resolve(200, { count: 3 }); await flush();
check('next successful heartbeat restores an actual count', () => assert.equal(secured.sent.at(-1)[2], '3'));
secured.session(false); await flush();
check('logout/host overlay stops heartbeats and sends authenticated best-effort leave', () => {
  assert.equal(secured.sent.at(-1)[2], '-1');
  assert.equal(secured.calls.at(-1).body.active, false);
  assert.equal(secured.calls.at(-1).init.keepalive, true);
});
const before = secured.calls.length; await secured.advance(60000);
check('stopped player has no background timer', () => assert.equal(secured.calls.length, before));
secured.session(); await flush();
check('new activation isolates late leave requests with a new tab identifier', () => assert.notEqual(secured.calls.at(-1).body.clientId, secured.calls[0].body.clientId));
secured.calls.at(-1).resolve(401, { error: 'unauthenticated' }); await flush();
check('expired session clears count and requests host session validation', () => {
  assert.equal(secured.sent.at(-1)[2], '-1');
  assert.equal(secured.messages.at(-1).value.type, 'bts:presence-refresh');
});

const loading = harness({ withoutUnity: true, queuedState: { ready: true, lobbyVisible: true } });
loading.session();
loading.calls[0].resolve(200, { count: 12 }); await flush();
loading.window.blackTheSurvival = { unityInstance: { SendMessage: (...args) => loading.sent.push(args) } };
loading.event('blackthesurvivalready');
check('queued Unity state and late Unity instance receive the last confirmed count', () => assert.equal(loading.sent.at(-1)[2], '12'));
loading.jump(90001); loading.visible();
check('return from a suspended tab hides an expired count before refresh finishes', () => assert.equal(loading.sent.at(-1)[2], '-1'));
loading.calls.at(-1).resolve(200, { count: 4 }); await flush();
loading.navigator.onLine = false; loading.event('offline'); await flush();
check('offline transition immediately clears count', () => assert.equal(loading.sent.at(-1)[2], '-1'));
loading.navigator.onLine = true; loading.event('online');
loading.calls.at(-1).resolve(200, { count: 5 }); await flush();
check('online transition refreshes without waiting thirty seconds', () => assert.equal(loading.sent.at(-1)[2], '5'));
loading.event('pagehide'); await flush();
check('pagehide releases presence', () => assert.equal(loading.calls.at(-1).body.active, false));
const leaveId = loading.calls.at(-1).body.clientId;
loading.event('pageshow');
check('back-forward cache restore uses a fresh presence identifier', () => assert.notEqual(loading.calls.at(-1).body.clientId, leaveId));
loading.calls.at(-1).resolve(200, { count: 0 }); await flush();
check('zero is displayed only when the backend explicitly confirms it', () => assert.equal(loading.sent.at(-1)[2], '0'));
loading.ready(false); await flush();
check('frozen/failed Unity player releases presence', () => assert.equal(loading.calls.at(-1).body.active, false));

const racing = harness(); racing.ready(); racing.session();
const firstId = racing.calls[0].body.clientId;
racing.session(false); racing.session(); await flush();
const newId = racing.calls.find(call => call.body.active && call.body.clientId !== firstId)?.body.clientId;
check('in-flight old heartbeat cleanup cannot remove a newly activated session', () => {
  assert.ok(newId);
  assert.ok(racing.calls.filter(call => !call.body.active).every(call => call.body.clientId === firstId));
});
const standalone = harness({ standalone: true }); standalone.ready(); standalone.session();
check('standalone local preview never contacts production presence', () => assert.equal(standalone.calls.length, 0));

const page = await fs.readFile(path.join(root, 'Temp/SitePresence20261026/app/page.tsx'), 'utf8');
const template = await fs.readFile(path.join(root, 'Assets/WebGLTemplates/BlackTheSurvival/index.html'), 'utf8');
check('host validates iframe origin and source before presence messages', () => assert.match(page, /event\.origin !== window\.location\.origin \|\| event\.source !== iframe\.current\?\.contentWindow/));
check('host enables presence only for a ready game with no host overlay', () => assert.match(page, /view === 'game' && gameStage === 'ready' && !overlay && !conflict/));
check('presence script loads before the Unity loader without altering fullscreen', () => {
  assert.ok(template.indexOf('TemplateData/presence.js') < template.indexOf('createUnityInstance'));
  assert.match(template, /unityInstance\.SetFullscreen\(1\)/);
});
const proof = { status: 'PASS', assertions: checks.length, checks, source: { path: path.relative(root, sourcePath).replaceAll('\\', '/'), sha256: createHash('sha256').update(source).digest('hex') }, checkedAt: new Date().toISOString(), backend: 'deterministic mocked API, no production requests', physicalHardwareTested: false };
await fs.writeFile(path.join(import.meta.dirname, 'presence-client-tests.json'), JSON.stringify(proof, null, 2) + '\n');
console.log(`PASS ${checks.length} presence client assertions`);
