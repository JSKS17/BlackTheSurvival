(function () {
  "use strict";

  // Account credentials stay in memory and arrive only from the owning page.
  // Unity draws the label itself, including canvas-only native fullscreen.
  const embedded = window.parent !== window;
  const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
  const csrf = /^[0-9a-f]{64}$/;
  const heartbeatMs = 30000;
  const presenceTtlMs = 90000;
  const requestTimeoutMs = 10000;
  let session = null, hostActive = false, closed = false, run = null;
  let gameState = window.BTSGamePresenceState || { ready: false, lobbyVisible: false };
  let count = -1, lastConfirmedAt = 0, lastInstance = null, lastSent = null;

  function post(type) {
    if (embedded) window.parent.postMessage({ type }, location.origin);
  }

  function publish(value) {
    count = Number.isSafeInteger(value) && value >= 0 ? value : -1;
    const instance = window.blackTheSurvival && window.blackTheSurvival.unityInstance;
    if (!instance || typeof instance.SendMessage !== "function" || (instance === lastInstance && lastSent === count)) return;
    try {
      instance.SendMessage("Black The Survival", "BrowserPresenceCount", String(count));
      lastInstance = instance; lastSent = count;
    } catch {
      // A loading/replaced player receives the latest value on its ready hook.
      lastInstance = null; lastSent = null;
    }
  }

  function headers(account) {
    return { "Content-Type": "application/json", "X-BTS-ACCOUNT": account.accountId, "X-BTS-CSRF": account.csrfToken };
  }

  function leave(previous) {
    if (!previous || navigator.onLine === false) return;
    try {
      Promise.resolve(fetch("/api/presence", {
        method: "POST", credentials: "same-origin", cache: "no-store", keepalive: true,
        headers: headers(previous.session), body: JSON.stringify({ clientId: previous.clientId, active: false })
      })).catch(() => {});
    } catch { /* Expiry removes entries when an unloading/offline tab cannot leave. */ }
  }

  function stop() {
    const previous = run;
    run = null;
    publish(-1);
    if (!previous) return;
    clearTimeout(previous.timer);
    if (previous.controller) previous.controller.abort();
    leave(previous);
    // An already accepted heartbeat may finish after the first leave request.
    // A fresh activation uses another clientId, so this cannot remove it.
    if (previous.pending) previous.pending.finally(() => leave(previous)).catch(() => {});
  }

  function allowed() {
    return embedded && !closed && hostActive && session && gameState.ready === true && navigator.onLine !== false;
  }

  async function heartbeat(current) {
    if (run !== current || current.pending || !allowed()) return;
    clearTimeout(current.timer);
    const controller = new AbortController();
    current.controller = controller;
    const timeout = setTimeout(() => controller.abort(), requestTimeoutMs);
    current.pending = (async () => {
      try {
        const response = await fetch("/api/presence", {
          method: "POST", credentials: "same-origin", cache: "no-store", signal: controller.signal,
          headers: headers(current.session), body: JSON.stringify({ clientId: current.clientId, active: true })
        });
        if (run !== current) return;
        if (response.status === 401 || response.status === 403 || response.status === 409) {
          session = null; hostActive = false; stop(); post("bts:presence-refresh"); return;
        }
        if (!response.ok) throw new Error("Presence request unavailable");
        const data = await response.json();
        if (run !== current) return;
        if (!data || !Number.isSafeInteger(data.count) || data.count < 0) throw new Error("Invalid presence response");
        lastConfirmedAt = Date.now();
        publish(data.count);
      } catch {
        if (run === current) publish(-1);
      } finally {
        clearTimeout(timeout);
        current.controller = null;
      }
    })();
    await current.pending;
    current.pending = null;
    if (run === current && allowed()) current.timer = setTimeout(() => { void heartbeat(current); }, heartbeatMs);
  }

  function reconcile(refresh) {
    if (!allowed()) { stop(); return; }
    if (Date.now() - lastConfirmedAt >= presenceTtlMs) publish(-1);
    if (!run) {
      run = { session: { ...session }, clientId: crypto.randomUUID(), timer: null, controller: null, pending: null };
      void heartbeat(run);
    } else if (refresh) void heartbeat(run);
    publish(count);
  }

  function setGameState(value) {
    if (!value || typeof value !== "object" || typeof value.ready !== "boolean" || typeof value.lobbyVisible !== "boolean") return;
    gameState = value;
    reconcile(false);
  }

  window.BTSPresenceBridge = { setGameState };
  window.addEventListener("message", event => {
    if (!embedded || event.origin !== location.origin || event.source !== window.parent) return;
    const value = event.data;
    if (!value || typeof value !== "object" || value.type !== "bts:presence-session" || typeof value.active !== "boolean") return;
    if (!value.active) { hostActive = false; stop(); return; }
    if (!uuid.test(value.accountId) || !csrf.test(value.csrfToken)) return;
    const changed = !session || session.accountId !== value.accountId || session.csrfToken !== value.csrfToken;
    if (changed) stop();
    session = { accountId: value.accountId, csrfToken: value.csrfToken };
    hostActive = true;
    reconcile(changed);
  });
  window.addEventListener("blackthesurvivalready", () => { publish(count); reconcile(false); });
  window.addEventListener("blackthesurvivalaccountblocked", () => { hostActive = false; stop(); });
  window.addEventListener("offline", () => reconcile(false));
  window.addEventListener("online", () => reconcile(true));
  document.addEventListener("visibilitychange", () => { if (!document.hidden) reconcile(true); });
  window.addEventListener("pagehide", () => { closed = true; stop(); });
  window.addEventListener("pageshow", () => { closed = false; reconcile(true); });
  post("bts:presence-ready");
  reconcile(false);
})();
