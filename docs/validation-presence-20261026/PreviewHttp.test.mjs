import assert from "node:assert/strict";
import { createHash, randomUUID } from "node:crypto";
import { readFile, writeFile } from "node:fs/promises";
import { Script } from "node:vm";
import { closePreview, previewReady } from "./Preview.mjs";

const origin = "http://127.0.0.1:8249";
const startedAt = new Date().toISOString();
const counts = [];
try {
  await previewReady;
  const bootstrap = await fetch(`${origin}/qa-presence/bootstrap`);
  assert.equal(bootstrap.status, 200);
  const cookies = bootstrap.headers.getSetCookie();
  assert.equal(cookies.length, 1);
  assert.match(cookies[0], /^bts-dev-session=[A-Za-z0-9_-]{43}; Path=\/; HttpOnly; SameSite=Lax;/);
  assert.match(cookies[0], /Expires=[A-Z][a-z]{2}, \d\d /);
  const cookie = cookies[0].split(";")[0];
  const session = await bootstrap.json();
  const headers = { Cookie: cookie, "X-BTS-ACCOUNT": session.user.id, "X-BTS-CSRF": session.csrfToken };
  const readCount = async expected => {
    const response = await fetch(`${origin}/api/presence`, { headers });
    assert.equal(response.status, 200);
    const value = await response.json();
    counts.push(value.count); assert.equal(value.count, expected);
    assert.equal(response.headers.get("Cache-Control"), "private, no-store");
  };
  await readCount(0);
  const first = await fetch(`${origin}/api/presence`, { method: "POST", headers: { ...headers,
    Origin: origin, "Content-Type": "application/json" }, body: JSON.stringify({ clientId: randomUUID(), active: true }) });
  assert.equal(first.status, 200); assert.equal((await first.json()).count, 1);
  await readCount(1);
  for (const [action, expected] of [["start", 2], ["stop", 1]]) {
    const response = await fetch(`${origin}/qa-presence/second`, { method: "POST", headers: {
      Origin: origin, "Content-Type": "application/json" }, body: JSON.stringify({ action }) });
    assert.equal(response.status, 200); assert.equal((await response.json()).count, expected);
    await readCount(expected);
  }
  const currentSession = await fetch(`${origin}/api/session`, { headers: { Cookie: cookie } });
  assert.deepEqual(await currentSession.json(), session);
  assert.equal((await fetch(`${origin}/api/presence`)).status, 401);
  assert.equal((await fetch(`${origin}/qa-presence/second`, { method: "POST", headers: {
    Origin: "https://bser-sts.com", "Content-Type": "application/json" }, body: '{"action":"start"}' })).status, 403);
  assert.equal((await fetch(`${origin}/api/presence`, { method: "POST", headers: { ...headers,
    Origin: origin, "Content-Type": "application/json" }, body: "x".repeat(2048) })).status, 413);
  const page = await fetch(`${origin}/qa?fixture=research`);
  assert.equal(page.status, 200);
  const html = await page.text();
  const source = html.match(/<script>([\s\S]+)<\/script>/)?.[1];
  assert.ok(source); new Script(source);
  for (const text of ["/qa-presence/bootstrap", "bts:presence-session", "second-start", "second-stop", "requestAnimationFrame(fit)"]) {
    assert.ok(html.includes(text), `Missing real QA integration: ${text}`);
  }
  const sourceHashes = {};
  for (const file of ["Preview.mjs", "LocalPresenceBackend.mjs", "LocalPresenceBackend.test.mjs"]) {
    sourceHashes[file] = createHash("sha256").update(await readFile(new URL(file, import.meta.url))).digest("hex");
  }
  const result = { status: "PASS", startedAt, endedAt: new Date().toISOString(), actualD1Counts: counts,
    actualBuiltUnityTested: false, productionRequests: 0, database: "in-memory real Miniflare D1",
    cookieForwarding: "single Set-Cookie, HttpOnly, full Expires preserved", boundedApiBytes: 1024,
    apiGuards: ["authenticated session", "same-origin mutation", "bounded request"], qaEmbeddedScriptCompiled: true,
    sourceHashes };
  await writeFile(new URL("preview-http-smoke.json", import.meta.url), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify({ status: result.status, actualD1Counts: counts, actualBuiltUnityTested: false }));
} finally { await closePreview(); }
console.log(JSON.stringify({ closed: true }));
