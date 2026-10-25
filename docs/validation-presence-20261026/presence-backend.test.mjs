import assert from "node:assert/strict";
import { createHash, randomBytes, randomUUID } from "node:crypto";
import { readFile } from "node:fs/promises";
import { createRequire } from "node:module";
import path from "node:path";
import test from "node:test";
import { pathToFileURL } from "node:url";

const siteRoot = path.resolve(process.env.PRESENCE_SITE_ROOT || "Temp/SitePresence20261026");
const requireSite = createRequire(path.join(siteRoot, "package.json"));
const { build } = await import(pathToFileURL(requireSite.resolve("esbuild")));
const { Miniflare } = await import(pathToFileURL(requireSite.resolve("miniflare")));
const origin = "https://game.test";
const baseNow = 1_800_000_000_000;
const hash = value => createHash("sha256").update(value).digest("hex");

async function runtime({ migrated = true } = {}) {
  const built = await build({
    stdin: { contents: `
      import { handleGameAccountRequest } from './lib/game-accounts.mjs';
      let now = ${baseNow};
      Date.now = () => now;
      export default { async fetch(request, env) {
        const value = request.headers.get('X-Test-Time');
        if (value !== null) now = Number(value);
        if (request.headers.has('X-Test-No-DB')) env = { ...env, DB: undefined };
        return await handleGameAccountRequest(request, env) ?? new Response('Not found', { status: 404 });
      } };
    `, resolveDir: siteRoot, sourcefile: "presence-backend-test-worker.js" },
    bundle: true, write: false, format: "esm", platform: "browser", external: ["node:crypto"],
  });
  const mf = new Miniflare({
    modules: true, cf: false, script: built.outputFiles[0].text,
    compatibilityDate: "2026-05-15", compatibilityFlags: ["nodejs_compat"], d1Databases: ["DB"],
  });
  await mf.ready;
  const db = await mf.getD1Database("DB");
  const journal = JSON.parse(await readFile(path.join(siteRoot, "drizzle/meta/_journal.json"), "utf8"));
  for (const migration of migrated ? journal.entries : journal.entries.slice(0, 2)) {
    const sql = await readFile(path.join(siteRoot, "drizzle", `${migration.tag}.sql`), "utf8");
    for (const statement of sql.split("--> statement-breakpoint").map(value => value.trim()).filter(Boolean)) {
      await db.prepare(statement).run();
    }
  }
  return { mf, db, dispose: () => mf.dispose() };
}

// Test-only seeded sessions exercise the real cookie lookup and guards without
// spending test time re-testing password KDFs covered by game-accounts.test.mjs.
async function session(r, { userId = randomUUID(), expiresAt = baseNow + 3_600_000 } = {}) {
  await r.db.prepare("INSERT OR IGNORE INTO bts_users (id,username,password_hash,created_at) VALUES (?,?,?,?)")
    .bind(userId, `player_${userId}`, "not-a-login-password-record", baseNow).run();
  const token = randomBytes(32).toString("base64url");
  const tokenHash = hash(token);
  await r.db.prepare("INSERT INTO bts_sessions (token_hash,user_id,created_at,expires_at) VALUES (?,?,?,?)")
    .bind(tokenHash, userId, baseNow, expiresAt).run();
  return { userId, tokenHash, cookie: `__Host-bts-session=${token}`, csrfToken: hash(`bts-csrf:${token}`) };
}

function api(r, session, { method = "GET", body, headers = {}, now = baseNow, route = "presence", rawBody } = {}) {
  const text = rawBody ?? (body === undefined ? undefined : JSON.stringify(body));
  const auth = session ? { Cookie: session.cookie, "X-BTS-ACCOUNT": session.userId, "X-BTS-CSRF": session.csrfToken } : {};
  return r.mf.dispatchFetch(`${origin}/api/${route}`, {
    method, headers: { ...auth, "X-Test-Time": String(now),
      ...(method === "GET" ? {} : { Origin: origin, "Content-Type": "application/json" }), ...headers },
    ...(text === undefined ? {} : { body: text }),
  });
}

async function heartbeat(r, s, clientId = randomUUID(), options = {}) {
  return api(r, s, { method: "POST", body: { clientId, active: true }, ...options });
}

async function count(r, s, expected, options = {}) {
  const response = await api(r, s, options);
  assert.equal(response.status, 200, await response.clone().text());
  const value = await response.json();
  assert.deepEqual(value, { count: expected, heartbeatMs: 30_000, ttlMs: 90_000 });
  assert.equal(response.headers.get("Cache-Control"), "private, no-store");
  assert.equal(response.headers.get("Vary"), "Cookie");
  return value;
}

test("real D1 counts distinct authenticated accounts and leaves only the departing tab", async () => {
  const r = await runtime();
  try {
    const a = await session(r), sameAccount = await session(r, { userId: a.userId }), b = await session(r);
    const a1 = randomUUID(), a2 = randomUUID(), a3 = randomUUID(), b1 = randomUUID();
    await count(r, a, 0);
    for (const id of [a1, a1, a2]) assert.equal((await heartbeat(r, a, id)).status, 200);
    assert.equal((await r.db.prepare("SELECT count(*) AS count FROM bts_presence").first()).count, 2);
    await count(r, a, 1);
    assert.equal((await heartbeat(r, sameAccount, a3)).status, 200);
    await count(r, a, 1);
    assert.equal((await heartbeat(r, b, b1)).status, 200);
    await count(r, b, 2);
    const leave = id => api(r, a, { method: "POST", body: { clientId: id, active: false } });
    assert.equal((await leave(a1)).status, 200);
    await count(r, a, 2);
    assert.equal((await leave(a2)).status, 200);
    await count(r, a, 2, {});
    assert.equal((await api(r, sameAccount, { method: "POST", body: { clientId: a3, active: false } })).status, 200);
    await count(r, b, 1);
    assert.equal((await leave(a2)).status, 200, "Leaving a tab twice is safe");
    const text = JSON.stringify(await count(r, b, 1));
    for (const secret of [a.userId, b.userId, a.tokenHash, a.cookie]) assert.equal(text.includes(secret), false);
  } finally { await r.dispose(); }
});

test("server clock TTL expires silent/offline tabs at exactly 90 seconds and permits reconnection", async () => {
  const r = await runtime();
  try {
    const a = await session(r), b = await session(r), a1 = randomUUID(), b1 = randomUUID();
    assert.equal((await heartbeat(r, a, a1)).status, 200);
    assert.equal((await heartbeat(r, b, b1, { now: baseNow + 30_000 })).status, 200);
    await count(r, a, 2, { now: baseNow + 89_999 });
    await count(r, a, 1, { now: baseNow + 90_000 });
    await count(r, a, 0, { now: baseNow + 120_000 });
    assert.equal((await heartbeat(r, a, a1, { now: baseNow + 121_000 })).status, 200);
    await count(r, a, 1, { now: baseNow + 121_000 });
    const row = await r.db.prepare("SELECT last_seen_at, expires_at FROM bts_presence WHERE token_hash=? AND client_id=?")
      .bind(a.tokenHash, a1).first();
    assert.deepEqual(row, { last_seen_at: baseNow + 121_000, expires_at: baseNow + 211_000 });
  } finally { await r.dispose(); }
});

test("logout and session expiration exclude accounts immediately without another heartbeat", async () => {
  const r = await runtime();
  try {
    const a = await session(r), otherDevice = await session(r, { userId: a.userId }), b = await session(r);
    const ending = await session(r, { expiresAt: baseNow + 50_000 });
    for (const value of [a, otherDevice, b, ending]) assert.equal((await heartbeat(r, value)).status, 200);
    await count(r, b, 3);
    const capped = await r.db.prepare("SELECT expires_at FROM bts_presence WHERE token_hash=?").bind(ending.tokenHash).first();
    assert.equal(capped.expires_at, baseNow + 50_000);
    assert.equal((await api(r, a, { route: "logout", method: "POST", body: {} })).status, 200);
    await count(r, b, 3, { now: baseNow + 1 });
    assert.equal((await r.db.prepare("SELECT count(*) AS count FROM bts_presence WHERE token_hash=?").bind(a.tokenHash).first()).count, 0);
    assert.equal((await api(r, a)).status, 401, "Logged-out cookie cannot read or renew presence");
    assert.equal((await api(r, otherDevice, { route: "logout", method: "POST", body: {} })).status, 200);
    await count(r, b, 2, { now: baseNow + 1 });
    await count(r, b, 1, { now: baseNow + 50_000 });
    assert.equal((await heartbeat(r, ending, randomUUID(), { now: baseNow + 50_000 })).status, 401);
  } finally { await r.dispose(); }
});

test("shared origin, CSRF, account and bounded JSON guards reject forged presence without changes", async () => {
  const r = await runtime();
  try {
    const a = await session(r), payload = { clientId: randomUUID(), active: true };
    const reject = async (options, expected) => {
      const response = await api(r, a, { method: "POST", body: payload, ...options });
      assert.equal(response.status, expected, await response.clone().text());
      assert.equal(Object.hasOwn(await response.json(), "count"), false);
    };
    assert.equal((await api(r, null)).status, 401);
    assert.equal((await api(r, null, { method: "POST", body: payload })).status, 401);
    await reject({ headers: { Origin: "https://evil.test" } }, 403);
    await reject({ headers: { Origin: "" } }, 403);
    await reject({ headers: { "Content-Type": "text/plain" } }, 415);
    await reject({ headers: { "X-BTS-CSRF": "0".repeat(64) } }, 403);
    await reject({ headers: { "X-BTS-CSRF": "" } }, 403);
    await reject({ headers: { "X-BTS-ACCOUNT": "another-account" } }, 409);
    await reject({ headers: { "X-BTS-ACCOUNT": "" } }, 400);
    await reject({ rawBody: "not json" }, 400);
    for (const body of [null, [], {}, { clientId: "not-uuid", active: true }, { ...payload, active: "true" }, { ...payload, now: baseNow + 999_999 }]) {
      await reject({ body }, 400);
    }
    await reject({ body: { ...payload, padding: "a".repeat(1024) } }, 413);
    const method = await api(r, a, { method: "PUT", body: payload });
    assert.equal(method.status, 405); assert.equal(method.headers.get("Allow"), "GET, POST");
    await count(r, a, 0);
    assert.equal((await r.db.prepare("SELECT count(*) AS count FROM bts_presence").first()).count, 0);
  } finally { await r.dispose(); }
});

test("active leases are bounded per session and expired cleanup deletes no more than 64 rows", async () => {
  const r = await runtime();
  try {
    const a = await session(r), ids = Array.from({ length: 17 }, () => randomUUID());
    for (const id of ids.slice(0, 16)) assert.equal((await heartbeat(r, a, id)).status, 200);
    const limited = await heartbeat(r, a, ids[16]);
    assert.equal(limited.status, 429); assert.equal(limited.headers.get("Retry-After"), "30");
    assert.equal(Object.hasOwn(await limited.json(), "count"), false);
    assert.equal((await heartbeat(r, a, ids[0])).status, 200, "Renewing an existing tab is accepted at the limit");
    await count(r, a, 1);
    await r.db.prepare("DELETE FROM bts_presence").run();
    await r.db.batch(Array.from({ length: 100 }, () => r.db.prepare(`INSERT INTO bts_presence
      (token_hash, client_id, user_id, last_seen_at, expires_at) VALUES (?,?,?,?,?)`)
      .bind(a.tokenHash, randomUUID(), a.userId, baseNow - 200_000, baseNow - 100_000)));
    assert.equal((await heartbeat(r, a, ids[16], { now: baseNow + 60_000 })).status, 200);
    assert.equal((await r.db.prepare("SELECT count(*) AS count FROM bts_presence WHERE expires_at <= ?").bind(baseNow).first()).count, 36);
    assert.equal((await heartbeat(r, a, ids[16], { now: baseNow + 60_001 })).status, 200);
    assert.equal((await r.db.prepare("SELECT count(*) AS count FROM bts_presence WHERE expires_at <= ?").bind(baseNow).first()).count, 36,
      "Cleanup is throttled, while expired rows never contribute to the count");
    await count(r, a, 1, { now: baseNow + 60_001 });
  } finally { await r.dispose(); }
});

test("missing migration or database reports unavailable without fabricated zero or runtime DDL", async () => {
  const r = await runtime({ migrated: false });
  try {
    const a = await session(r);
    for (const options of [{}, { method: "POST", body: { clientId: randomUUID(), active: true } }, { headers: { "X-Test-No-DB": "1" } }]) {
      const response = await api(r, a, options);
      assert.equal(response.status, 503);
      assert.equal(Object.hasOwn(await response.json(), "count"), false);
    }
    assert.equal(await r.db.prepare("SELECT name FROM sqlite_master WHERE type='table' AND name='bts_presence'").first(), null);
  } finally { await r.dispose(); }
});
