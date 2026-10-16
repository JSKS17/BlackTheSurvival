(function () {
  "use strict";
  const embedded = window.parent !== window;
  const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;
  const hash = /^[0-9a-f]{64}$/;
  const maxBundleBytes = 4 * 1024 * 1024;
  let instance = null, preparePromise, bootstrapResolve, bootstrapReject;
  let helloTimer, bootstrapTimer, releaseLock, sequence = 0, queue = Promise.resolve();
  let dirtyTimer, dirtyAgain = false, commandActive = false;
  const waiting = new Map();

  function post(type, payload) {
    if (embedded) window.parent.postMessage({ type, accountId: bridge.accountId, ...payload }, location.origin);
  }
  function failure(error) {
    const message = error instanceof Error ? error.message : String(error);
    post("bts:blocked", { error: message });
    window.dispatchEvent(new CustomEvent("blackthesurvivalaccountblocked", { detail: message }));
    return message;
  }
  function validRevision(value) { return Number.isSafeInteger(value) && value >= 1; }

  function validSettings(value) {
    return value && typeof value === "object" && !Array.isArray(value)
      && ["volume", "musicVolume", "effectsVolume"].every(name =>
        typeof value[name] === "number" && Number.isFinite(value[name]) && value[name] >= 0 && value[name] <= 1)
      && ["sound", "reduceMotion", "fullScreen"].every(name => typeof value[name] === "boolean");
  }

  function acquireLock(accountId) {
    if (!navigator.locks || typeof navigator.locks.request !== "function")
      return Promise.reject(new Error("이 브라우저에서는 안전한 계정 저장을 사용할 수 없습니다. 최신 Chrome 또는 Edge에서 열어 주세요."));
    return new Promise((resolve, reject) => {
      navigator.locks.request("bts:game-account:" + accountId, { ifAvailable: true }, async lock => {
        if (!lock) { reject(new Error("이 계정의 게임이 다른 탭에서 열려 있습니다. 기존 게임 탭을 닫고 다시 불러와 주세요.")); return; }
        resolve();
        await new Promise(release => { releaseLock = release; });
      }).catch(reject);
    });
  }

  function prepare() {
    if (preparePromise) return preparePromise;
    if (!embedded) { bridge.accountId = ""; preparePromise = Promise.resolve(); return preparePromise; }
    preparePromise = new Promise((resolve, reject) => { bootstrapResolve = resolve; bootstrapReject = reject; });
    const hello = () => post("bts:hello", {});
    hello(); helloTimer = setInterval(hello, 500);
    bootstrapTimer = setTimeout(() => {
      clearInterval(helloTimer);
      bootstrapReject(new Error("계정 연결 응답을 받지 못했습니다. 게임 페이지에서 다시 로그인해 주세요."));
    }, 30000);
    preparePromise.catch(failure);
    return preparePromise;
  }

  function parseBundle(bundleJson) {
    if (typeof bundleJson !== "string" || new TextEncoder().encode(bundleJson).byteLength > maxBundleBytes)
      throw new Error("서버 저장 기록의 크기가 올바르지 않습니다.");
    const bundle = JSON.parse(bundleJson);
    if (!bundle || bundle.schemaVersion !== 1 || typeof bundle.gameVersion !== "string" || !bundle.gameVersion
      || !(bundle.run === null || typeof bundle.run === "string")
      || !(bundle.research === null || typeof bundle.research === "string")
      || !Array.isArray(bundle.pending) || bundle.pending.length > 256 || typeof bundle.tutorialSeen !== "boolean")
      throw new Error("지원하지 않는 서버 저장 형식입니다.");
    if (bundle.settings != null && !validSettings(bundle.settings))
      throw new Error("서버 환경설정의 형식이 올바르지 않습니다.");
    const names = new Set();
    for (const item of bundle.pending) {
      if (!item || typeof item.name !== "string" || typeof item.json !== "string"
        || !/^lumia-vf-loop-v1\.json\.research-pending-[A-Za-z0-9_-]{1,96}\.json$/.test(item.name) || names.has(item.name))
        throw new Error("미정산 연구 기록의 형식이 올바르지 않습니다.");
      names.add(item.name);
    }
    return {
      schemaVersion: bundle.schemaVersion, gameVersion: bundle.gameVersion,
      hasRun: bundle.run !== null, runJson: bundle.run || "",
      hasResearch: bundle.research !== null, researchJson: bundle.research || "",
      pending: bundle.pending, tutorialSeen: bundle.tutorialSeen,
      // JsonUtility cannot distinguish a missing object from an object whose
      // fields have default values. Pass presence separately from its JSON.
      hasSettings: bundle.settings != null, settingsJson: bundle.settings == null ? "" : JSON.stringify(bundle.settings)
    };
  }

  function invokeUnity(command) {
    return new Promise((resolve, reject) => {
      const requestId = "bridge-" + (++sequence);
      const timer = setTimeout(() => { waiting.delete(requestId); reject(new Error("게임의 저장 응답을 받지 못했습니다. 페이지를 닫지 말고 다시 시도해 주세요.")); }, 30000);
      waiting.set(requestId, { resolve, reject, timer });
      try { instance.SendMessage("Black The Survival", "BrowserAccountCommand", JSON.stringify({ ...command, requestId })); }
      catch (error) { clearTimeout(timer); waiting.delete(requestId); reject(error); }
    });
  }

  function emitFromUnity(json) {
    let response;
    try { response = JSON.parse(json); }
    catch (error) { console.error("Invalid game bridge response."); return; }
    const pending = waiting.get(response.requestId);
    if (!pending) return;
    waiting.delete(response.requestId); clearTimeout(pending.timer);
    if (response.ok !== true) pending.reject(new Error(response.error || "게임 기록을 처리하지 못했습니다."));
    else pending.resolve(response);
  }

  async function perform(message) {
    const requestId = message.requestId;
    const action = message.type.slice(4);
    if (!instance) { post("bts:result", { requestId, ok: false, error: "게임이 아직 준비되지 않았습니다.", localPersisted: false }); return; }
    let command = { action };
    commandActive = true;
    dirtyAgain = false;
    try {
      if (action === "export") command.freeze = message.freeze === true;
      else if (action === "import") {
        if (!validRevision(message.revision) || !hash.test(message.sha256)) throw new Error("서버 저장 버전이 올바르지 않습니다.");
        command = { ...command, ...parseBundle(message.bundleJson), bundleJson: message.bundleJson, revision: message.revision, sha256: message.sha256 };
      } else if (action === "ack") {
        if (!validRevision(message.revision) || !hash.test(message.sha256)) throw new Error("동기화 확인 값이 올바르지 않습니다.");
        command = { ...command, revision: message.revision, sha256: message.sha256 };
      }
      // Keep combat frozen while settlement and IndexedDB persistence finish.
      // The final resume command only releases the game; it performs no writes.
      const response = await invokeUnity(action === "resume" ? { action: "prepare-resume" } : command);
      if (!bridge.storage) throw new Error("브라우저 저장 공간이 준비되지 않았습니다.");
      await bridge.storage.waitForPersist();
      if (action === "resume") await invokeUnity({ action: "resume" });
      const snapshot = { bundleJson: response.bundleJson, revision: response.revision, lastSyncedHash: response.lastSyncedHash, localPersisted: true };
      if (action === "export" || action === "dirty") post(action === "dirty" ? "bts:dirty" : "bts:local", { requestId, ...snapshot });
      else {
        post("bts:result", { requestId, ok: true, localPersisted: true });
        post("bts:local", { requestId, ...snapshot });
      }
    } catch (error) {
      post("bts:result", { requestId, ok: false, error: error instanceof Error ? error.message : String(error), localPersisted: false });
    } finally {
      commandActive = false;
      if (dirtyAgain) scheduleDirty();
    }
  }

  function enqueue(message) { queue = queue.then(() => perform(message)).catch(error => failure(error)); }
  function scheduleDirty() {
    clearTimeout(dirtyTimer);
    dirtyTimer = setTimeout(() => {
      if (commandActive) { dirtyAgain = true; return; }
      dirtyAgain = false;
      enqueue({ type: "bts:dirty" });
    }, 250);
  }
  function markDirty() {
    if (!embedded || !instance || !bridge.accountId) return;
    dirtyAgain = true;
    if (!commandActive) scheduleDirty();
  }

  window.addEventListener("message", event => {
    if (!embedded || event.source !== window.parent || event.origin !== location.origin || !event.data || typeof event.data !== "object") return;
    const message = event.data;
    if (message.type === "bts:bootstrap") {
      if (!bootstrapResolve || bridge.accountId || !uuid.test(message.accountId)) return;
      bridge.accountId = message.accountId;
      clearInterval(helloTimer); clearTimeout(bootstrapTimer);
      acquireLock(bridge.accountId).then(bootstrapResolve, bootstrapReject);
    } else if (bridge.accountId && ["bts:export", "bts:import", "bts:ack", "bts:resume"].includes(message.type)) {
      enqueue(message);
    }
  });
  window.addEventListener("pagehide", () => { if (releaseLock) releaseLock(); });

  const bridge = window.BTSAccountBridge = {
    accountId: "", storage: null, prepare, emitFromUnity, markDirty,
    attachUnity(unity) { instance = unity; if (embedded) post("bts:ready", {}); }
  };
})();
