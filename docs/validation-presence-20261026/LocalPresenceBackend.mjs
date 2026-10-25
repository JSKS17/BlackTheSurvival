// Disposable QA only. No persisted database, production URL, or production account.
import { createHash, randomBytes, randomUUID } from "node:crypto";
import { readFile } from "node:fs/promises";
import { createRequire } from "node:module";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const proofRoot = path.dirname(fileURLToPath(import.meta.url));
const workspace = path.resolve(proofRoot, "../..");
const hash = value => createHash("sha256").update(value).digest("hex");
const json = (value, status = 200, extra = {}) => Response.json(value, { status, headers: {
  "Cache-Control": "private, no-store", "X-Content-Type-Options": "nosniff", "Vary": "Cookie", ...extra,
} });

export async function createLocalPresenceBackend({ origin = "http://127.0.0.1:8249",
  siteRoot = path.join(workspace, "Temp/SitePresence20261026") } = {}) {
  const local = new URL(origin);
  if (local.protocol !== "http:" || !["127.0.0.1", "localhost", "[::1]"].includes(local.hostname)
    || local.origin !== origin || local.pathname !== "/" || local.username || local.password || !local.port) {
    throw new Error("A fixed loopback-only HTTP origin is required");
  }
  const requireSite = createRequire(path.join(siteRoot, "package.json"));
  const { build } = await import(pathToFileURL(requireSite.resolve("esbuild")));
  const { Miniflare } = await import(pathToFileURL(requireSite.resolve("miniflare")));
  const built = await build({
    stdin: { contents: `
      import { handleGameAccountRequest } from './lib/game-accounts.mjs';
      export default { async fetch(request, env) {
        return await handleGameAccountRequest(request, env) ?? new Response('Not found', { status: 404 });
      } };
    `, resolveDir: siteRoot, sourcefile: "local-presence-qa-worker.js" },
    bundle: true, write: false, format: "esm", platform: "browser", external: ["node:crypto"],
  });
  const mf = new Miniflare({
    modules: true, cf: false, script: built.outputFiles[0].text,
    compatibilityDate: "2026-05-15", compatibilityFlags: ["nodejs_compat"], d1Databases: ["DB"],
    d1Persist: false, r2Persist: false, kvPersist: false, cachePersist: false,
  });
  let timer, closed = false;
  const inFlight = new Set();
  const dispatch = async (url, options) => {
    if (closed) throw new Error("Local QA backend is closed");
    const pending = mf.dispatchFetch(url, options);
    inFlight.add(pending);
    try { return await pending; } finally { inFlight.delete(pending); }
  };
  const close = async () => {
    if (closed) return;
    closed = true;
    clearInterval(timer); timer = undefined;
    await Promise.allSettled([...inFlight]);
    await mf.dispose();
  };
  try {
    await mf.ready;
    const db = await mf.getD1Database("DB");
    const journal = JSON.parse(await readFile(path.join(siteRoot, "drizzle/meta/_journal.json"), "utf8"));
    for (const migration of journal.entries) {
      const sql = await readFile(path.join(siteRoot, "drizzle", `${migration.tag}.sql`), "utf8");
      for (const statement of sql.split("--> statement-breakpoint").map(value => value.trim()).filter(Boolean)) {
        await db.prepare(statement).run();
      }
    }
    const now = Date.now(), expiresAt = now + 3_600_000;
    async function seed(label) {
      const id = randomUUID(), token = randomBytes(32).toString("base64url");
      const username = `qa_presence_${label}`;
      // These users intentionally have no usable password login. Only the two
      // short-lived session cookies seeded into this in-memory DB authenticate.
      await db.prepare("INSERT INTO bts_users (id,username,password_hash,created_at) VALUES (?,?,?,?)")
        .bind(id, username, "disposable-local-qa-no-password-login", now).run();
      await db.prepare("INSERT INTO bts_save_state (user_id,revision) VALUES (?,0)").bind(id).run();
      await db.prepare("INSERT INTO bts_sessions (token_hash,user_id,created_at,expires_at) VALUES (?,?,?,?)")
        .bind(hash(token), id, now, expiresAt).run();
      return { cookie: `bts-dev-session=${token}`, public: { authenticated: true,
        user: { id, username }, csrfToken: hash(`bts-csrf:${token}`), expiresAt: new Date(expiresAt).toISOString() } };
    }
    const first = await seed("first"), second = await seed("second"), secondClientId = randomUUID();
    const secondHeartbeat = active => dispatch(`${origin}/api/presence`, { method: "POST", headers: {
      Cookie: second.cookie, Origin: origin, "Content-Type": "application/json",
      "X-BTS-ACCOUNT": second.public.user.id, "X-BTS-CSRF": second.public.csrfToken,
    }, body: JSON.stringify({ clientId: secondClientId, active }) });

    async function control(request) {
      if (request.method !== "POST") return json({ error: "POST required" }, 405, { Allow: "POST" });
      if (request.headers.get("Origin") !== origin) return json({ error: "Same-origin QA request required" }, 403);
      if (request.headers.get("Content-Type")?.split(";")[0].trim().toLowerCase() !== "application/json") {
        return json({ error: "JSON required" }, 415);
      }
      const reader = request.body?.getReader();
      if (!reader) return json({ error: "QA action required" }, 400);
      let body = "", bytes = 0;
      const decoder = new TextDecoder("utf-8", { fatal: true });
      try {
        while (true) {
          const chunk = await reader.read();
          if (chunk.done) break;
          bytes += chunk.value.byteLength;
          if (bytes > 256) { await reader.cancel(); return json({ error: "QA request too large" }, 413); }
          body += decoder.decode(chunk.value, { stream: true });
        }
        body += decoder.decode();
      } catch { return json({ error: "Invalid QA request" }, 400); }
      finally { reader.releaseLock(); }
      let payload;
      try { payload = JSON.parse(body); } catch { return json({ error: "Invalid QA JSON" }, 400); }
      if (!payload || Array.isArray(payload) || Object.keys(payload).length !== 1 || !["start", "stop"].includes(payload.action)) {
        return json({ error: "Only start or stop is supported" }, 400);
      }
      clearInterval(timer); timer = undefined;
      const active = payload.action === "start";
      const response = await secondHeartbeat(active);
      if (active && response.ok) {
        timer = setInterval(() => { secondHeartbeat(true).then(response => response.arrayBuffer()).catch(() => {}); }, 30_000);
        timer.unref();
      }
      return response;
    }

    return { firstSession: first.public, close, async handle(request) {
      if (closed) return json({ error: "Local QA backend closed" }, 503);
      const url = new URL(request.url);
      const pathname = url.pathname;
      if (!["/api/presence", "/api/session", "/qa-presence/bootstrap", "/qa-presence/second"].includes(pathname)) return null;
      const suppliedHost = request.headers.get("Host");
      if (url.origin !== origin || (suppliedHost !== null && suppliedHost !== local.host)
        || request.headers.get("Sec-Fetch-Site") === "cross-site"
        || (request.headers.has("Origin") && request.headers.get("Origin") !== origin)) {
        return json({ error: "Loopback QA origin required" }, 403);
      }
      if (pathname === "/qa-presence/bootstrap") {
        if (request.method !== "GET") return json({ error: "GET required" }, 405, { Allow: "GET" });
        return json(first.public, 200, { "Set-Cookie": `${first.cookie}; Path=/; HttpOnly; SameSite=Lax; Max-Age=3600; Expires=${new Date(expiresAt).toUTCString()}` });
      }
      if (pathname === "/qa-presence/second") return await control(request);
      return await dispatch(request.url, { method: request.method, headers: request.headers,
        ...(request.body ? { body: await request.arrayBuffer() } : {}) });
    } };
  } catch (error) { await close(); throw error; }
}
