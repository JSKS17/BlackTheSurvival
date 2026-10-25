import assert from "node:assert/strict";
import { randomUUID } from "node:crypto";
import { createLocalPresenceBackend } from "./LocalPresenceBackend.mjs";

const origin = "http://127.0.0.1:8249";
const backend = await createLocalPresenceBackend({ origin });
const request = (pathname, options) => new Request(origin + pathname, options);
try {
  const bootstrap = await backend.handle(request("/qa-presence/bootstrap"));
  assert.equal(bootstrap.status, 200);
  assert.deepEqual(await bootstrap.json(), backend.firstSession);
  const setCookie = bootstrap.headers.get("Set-Cookie");
  assert.match(setCookie, /^bts-dev-session=[A-Za-z0-9_-]{43}; Path=\/; HttpOnly; SameSite=Lax;/);
  const cookie = setCookie.split(";")[0];
  const sessionHeaders = { Cookie: cookie, "X-BTS-ACCOUNT": backend.firstSession.user.id,
    "X-BTS-CSRF": backend.firstSession.csrfToken };
  const readCount = async expected => {
    const response = await backend.handle(request("/api/presence", { headers: sessionHeaders }));
    assert.equal(response.status, 200);
    assert.equal((await response.json()).count, expected);
  };
  await readCount(0);
  const firstHeartbeat = await backend.handle(request("/api/presence", { method: "POST", headers: {
    ...sessionHeaders, Origin: origin, "Content-Type": "application/json",
  }, body: JSON.stringify({ clientId: randomUUID(), active: true }) }));
  assert.equal(firstHeartbeat.status, 200); assert.equal((await firstHeartbeat.json()).count, 1);
  const action = async (action, expected) => {
    const response = await backend.handle(request("/qa-presence/second", { method: "POST", headers: {
      Origin: origin, "Content-Type": "application/json",
    }, body: JSON.stringify({ action }) }));
    assert.equal(response.status, 200); assert.equal((await response.json()).count, expected);
  };
  await action("start", 2); await readCount(2);
  await action("stop", 1); await readCount(1);
  const session = await backend.handle(request("/api/session", { headers: { Cookie: cookie } }));
  assert.deepEqual(await session.json(), backend.firstSession);
  const badOrigin = await backend.handle(request("/qa-presence/second", { method: "POST", headers: {
    Origin: "https://bser-sts.com", "Content-Type": "application/json",
  }, body: '{"action":"start"}' }));
  assert.equal(badOrigin.status, 403);
  assert.equal((await backend.handle(new Request("https://bser-sts.com/api/presence"))).status, 403);
  assert.equal((await backend.handle(request("/qa-presence/bootstrap", { headers: { "Sec-Fetch-Site": "cross-site" } }))).status, 403);
  assert.equal(await backend.handle(request("/not-handled")), null);
  console.log(JSON.stringify({ status: "PASS", actualD1Sequence: [0, 1, 2, 1], productionRequests: 0,
    storage: "in-memory Miniflare D1", rawTokensLogged: false, guards: ["loopback-origin", "same-origin-control", "cross-site-bootstrap"] }));
} finally { await backend.close(); }
assert.equal((await backend.handle(request("/api/session"))).status, 503);
console.log(JSON.stringify({ closed: true }));
