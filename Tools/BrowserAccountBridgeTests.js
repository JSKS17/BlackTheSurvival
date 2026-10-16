"use strict";
const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

const root = path.resolve(__dirname, "..");
const bridgeSource = fs.readFileSync(path.join(root, "Assets/WebGLTemplates/BlackTheSurvival/TemplateData/account-bridge.js"), "utf8");
const pluginSource = fs.readFileSync(path.join(root, "Assets/Plugins/WebGL/BrowserAccount.jslib"), "utf8");
const sdkRoot = process.env.BTS_WEBGL_BUILD_TOOLS || "C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor/Data/PlaybackEngines/WebGLSupport/BuildTools";
const sdkIdbSource = fs.readFileSync(path.join(sdkRoot, "prejs/IdbFs.js"), "utf8");
const accountA = "11111111-1111-4111-8111-111111111111";
const accountB = "22222222-2222-4222-8222-222222222222";
const emptyBundle = '{"schemaVersion":1,"gameVersion":"1.0.6","run":null,"research":null,"pending":[],"tutorialSeen":false}';
const settings = { volume: .21, musicVolume: .32, effectsVolume: .43, sound: false, reduceMotion: true, fullScreen: false };
const hash = "a".repeat(64);
const tick = () => new Promise(resolve => setTimeout(resolve, 0));

function browser(options = {}) {
  const posts = [], commands = [], events = new Map(), timers = new Map();
  let timerId = 0, lockRelease;
  const parent = { postMessage: message => posts.push(message) };
  const window = { parent, addEventListener(type, callback) { events.set(type, callback); }, dispatchEvent() {} };
  if (options.standalone) window.parent = window;
  const context = vm.createContext({
    window, location: { origin: "https://game.example" }, console, TextEncoder, Promise, Map, Set,
    CustomEvent: class { constructor(type, init) { this.type = type; this.detail = init.detail; } },
    navigator: options.missingLocks ? {} : { locks: { request: async (name, settings, callback) => {
      assert.equal(name, "bts:game-account:" + accountA);
      assert.deepEqual(JSON.parse(JSON.stringify(settings)), { ifAvailable: true });
      lockRelease = callback(options.lockUnavailable ? null : { name });
      await lockRelease;
    } } },
    setTimeout(callback, delay) { const id = ++timerId; timers.set(id, { callback, delay }); return id; },
    clearTimeout(id) { timers.delete(id); },
    setInterval(callback, delay) { const id = ++timerId; timers.set(id, { callback, delay, interval: true }); return id; },
    clearInterval(id) { timers.delete(id); }
  });
  vm.runInContext(bridgeSource, context);
  const bridge = window.BTSAccountBridge;
  bridge.storage = { waitForPersist: options.waitForPersist || (() => Promise.resolve(true)) };
  const unity = { SendMessage(target, method, json) {
    assert.equal(target, "Black The Survival"); assert.equal(method, "BrowserAccountCommand");
    const command = JSON.parse(json); commands.push(command);
    if (options.onCommand) options.onCommand(command, bridge);
    else bridge.emitFromUnity(JSON.stringify({ kind: "local", requestId: command.requestId, ok: true, bundleJson: emptyBundle, revision: 0, lastSyncedHash: "" }));
  } };
  function message(data, source = parent, origin = "https://game.example") { events.get("message")({ data, source, origin }); }
  async function start() { const ready = bridge.prepare(); message({ type: "bts:bootstrap", accountId: accountA }); await ready; bridge.attachUnity(unity); await tick(); }
  function runTimers(delay) {
    for (const [id, timer] of [...timers]) if (timer.delay === delay) { if (!timer.interval) timers.delete(id); timer.callback(); }
  }
  function close() { if (events.has("pagehide")) events.get("pagehide")(); }
  return { context, bridge, posts, commands, message, start, runTimers, close, timers };
}

test("embedded bootstrap validates parent source/origin and never falls back to guest", async () => {
  const b = browser(); const ready = b.bridge.prepare();
  assert.equal(b.posts[0].type, "bts:hello");
  b.message({ type: "bts:bootstrap", accountId: accountA }, {}, "https://game.example");
  b.message({ type: "bts:bootstrap", accountId: accountA }, undefined, "https://evil.example");
  assert.equal(b.bridge.accountId, "");
  b.runTimers(30000);
  await assert.rejects(ready, /계정 연결 응답/);
  assert.equal(b.bridge.accountId, ""); assert.equal(b.commands.length, 0);
  assert.equal(b.posts.at(-1).type, "bts:blocked"); b.close();
});

test("unsupported locks and an already-open account block Unity startup", async () => {
  for (const options of [{ missingLocks: true }, { lockUnavailable: true }]) {
    const b = browser(options); await assert.rejects(b.start(), /브라우저|다른 탭/);
    assert.equal(b.commands.length, 0); assert.equal(b.posts.at(-1).type, "bts:blocked"); b.close();
  }
});

test("standalone guest prepares without altering its account or requiring Web Locks", async () => {
  const b = browser({ standalone: true, missingLocks: true });
  await b.bridge.prepare(); assert.equal(b.bridge.accountId, ""); assert.equal(b.posts.length, 0); b.close();
});

test("export waits for actual local persistence before returning a usable snapshot", async () => {
  let persist;
  const b = browser({ waitForPersist: () => new Promise(resolve => { persist = resolve; }) });
  await b.start(); b.message({ type: "bts:export", requestId: "parent-export", freeze: true }); await tick();
  assert.equal(b.commands[0].action, "export"); assert.equal(b.commands[0].freeze, true);
  assert.equal(b.posts.filter(post => post.type === "bts:local").length, 0);
  persist(); await tick();
  const local = b.posts.at(-1); assert.equal(local.type, "bts:local"); assert.equal(local.requestId, "parent-export");
  assert.equal(local.bundleJson, emptyBundle); assert.equal(local.localPersisted, true); assert.equal(local.accountId, accountA); b.close();
});

test("persistence failure reports false and never acknowledges an export as durable", async () => {
  const b = browser({ waitForPersist: () => Promise.reject(new Error("simulated quota exceeded")) });
  await b.start(); b.message({ type: "bts:export", requestId: "no-loss" }); await tick();
  assert.equal(b.posts.at(-1).type, "bts:result"); assert.equal(b.posts.at(-1).ok, false);
  assert.equal(b.posts.at(-1).localPersisted, false); assert.equal(b.posts.at(-1).requestId, "no-loss"); b.close();
});

test("import preserves null slots and Unicode/raw bundle text across the Unity DTO", async () => {
  const b = browser(); await b.start();
  const json = '{ "schemaVersion":1,"gameVersion":"1.0.6","run":null,"research":"{\\\"한글\\\":true}","pending":[],"tutorialSeen":true }';
  b.message({ type: "bts:import", requestId: "import-1", bundleJson: json, revision: 7, sha256: hash }); await tick();
  const command = b.commands[0]; assert.equal(command.hasRun, false); assert.equal(command.runJson, "");
  assert.equal(command.hasResearch, true); assert.equal(command.researchJson, '{"한글":true}');
  assert.equal(command.hasSettings, false); assert.equal(command.settingsJson, "");
  assert.equal(command.bundleJson, json); assert.equal(command.revision, 7);
  assert.equal(b.posts.at(-2).type, "bts:result"); assert.equal(b.posts.at(-2).ok, true);
  assert.equal(b.posts.at(-1).type, "bts:local"); assert.equal(b.posts.at(-1).requestId, "import-1"); b.close();
});

test("settings imports preserve mute/zero values and explicitly distinguish legacy null settings", async () => {
  const b = browser(); await b.start();
  for (const value of [undefined, null, settings, { ...settings, volume: 0, musicVolume: 1, fullScreen: true }]) {
    const bundleJson = JSON.stringify({ ...JSON.parse(emptyBundle), ...(value === undefined ? {} : { settings: value }) });
    b.message({ type: "bts:import", requestId: "settings-import", bundleJson, revision: 7, sha256: hash }); await tick();
    const command = b.commands.at(-1);
    assert.equal(command.hasSettings, value != null);
    assert.equal(command.settingsJson, value == null ? "" : JSON.stringify(value));
    assert.equal(command.bundleJson, bundleJson, "Original cloud text is kept for its SHA acknowledgement");
    assert.equal(b.posts.at(-2).ok, true);
  }
  b.close();
});

test("invalid/incomplete settings never reach Unity or overwrite account preferences", async () => {
  const b = browser(); await b.start();
  const missing = { ...settings }; delete missing.fullScreen;
  const invalid = [[], {}, "settings", false, missing,
    { ...settings, volume: -0.01 }, { ...settings, volume: 1.01 },
    { ...settings, volume: "0.5" }, { ...settings, musicVolume: null },
    { ...settings, effectsVolume: Number.MAX_VALUE }, { ...settings, sound: 0 },
    { ...settings, reduceMotion: "false" }, { ...settings, fullScreen: null }];
  for (const value of invalid) {
    b.message({ type: "bts:import", requestId: "invalid-settings", bundleJson: JSON.stringify({ ...JSON.parse(emptyBundle), settings: value }), revision: 7, sha256: hash });
    await tick(); assert.equal(b.posts.at(-1).ok, false); assert.equal(b.commands.length, 0);
  }
  b.close();
});

test("settings-only edits use the durable dirty path and remain bound to the current account", async () => {
  const bundleJson = JSON.stringify({ ...JSON.parse(emptyBundle), settings });
  const b = browser({ onCommand(command, bridge) {
    assert.equal(command.action, "dirty");
    bridge.emitFromUnity(JSON.stringify({ kind: "local", requestId: command.requestId, ok: true, bundleJson, revision: 5, lastSyncedHash: hash }));
  } });
  await b.start(); b.bridge.markDirty(); b.bridge.markDirty(); b.runTimers(250); await tick();
  assert.equal(b.commands.length, 1);
  const update = b.posts.at(-1); assert.equal(update.type, "bts:dirty");
  assert.equal(update.accountId, accountA); assert.equal(update.bundleJson, bundleJson); assert.equal(update.localPersisted, true);
  b.close();
});

test("invalid import paths/null top-level and missing slot fields are rejected before Unity", async () => {
  const b = browser(); await b.start();
  for (const bundleJson of ["null", '{"schemaVersion":1,"gameVersion":"1.0.6","pending":[],"tutorialSeen":false}',
    JSON.stringify({ ...JSON.parse(emptyBundle), pending: [{ name: "../other-account.json", json: "{}" }] })]) {
    b.message({ type: "bts:import", requestId: "bad", bundleJson, revision: 1, sha256: hash }); await tick();
    assert.equal(b.posts.at(-1).ok, false); assert.equal(b.commands.length, 0);
  }
  b.close();
});

test("commands are serialized while a previous persistence transaction is pending", async () => {
  const resolves = []; const b = browser({ waitForPersist: () => new Promise(resolve => resolves.push(resolve)) });
  await b.start(); b.message({ type: "bts:export", requestId: "first" }); b.message({ type: "bts:ack", requestId: "second", revision: 2, sha256: hash });
  await tick(); assert.equal(b.commands.length, 1); resolves.shift()(); await tick(); assert.equal(b.commands.length, 2);
  assert.equal(b.commands[1].action, "ack"); resolves.shift()(); await tick(); assert.equal(b.posts.at(-1).requestId, "second"); b.close();
});

test("resume keeps enemy updates frozen throughout delayed IndexedDB persistence", async () => {
  let persist, frozen = true, settlements = 0;
  const b = browser({
    waitForPersist: () => new Promise(resolve => { persist = resolve; }),
    onCommand(command, bridge) {
      if (command.action === "prepare-resume") {
        frozen = true; settlements++;
        bridge.emitFromUnity(JSON.stringify({ kind: "local", requestId: command.requestId, ok: true, bundleJson: emptyBundle, revision: 3, lastSyncedHash: hash }));
      } else if (command.action === "resume") {
        frozen = false;
        bridge.emitFromUnity(JSON.stringify({ kind: "result", requestId: command.requestId, ok: true }));
      }
    }
  });
  await b.start(); b.message({ type: "bts:resume", requestId: "resume-delayed" }); await tick();
  assert.deepEqual(b.commands.map(command => command.action), ["prepare-resume"]);
  assert.equal(frozen, true); assert.equal(settlements, 1);
  assert.equal(b.posts.filter(post => post.type === "bts:result").length, 0);
  await tick(); assert.equal(frozen, true);
  persist(); await tick();
  assert.deepEqual(b.commands.map(command => command.action), ["prepare-resume", "resume"]);
  assert.equal(frozen, false); assert.equal(settlements, 1);
  assert.equal(b.posts.at(-2).type, "bts:result"); assert.equal(b.posts.at(-2).localPersisted, true);
  assert.equal(b.posts.at(-1).bundleJson, emptyBundle); b.close();
});

test("resume persistence failure never sends the game release command", async () => {
  let frozen = true;
  const b = browser({
    waitForPersist: () => Promise.reject(new Error("simulated IndexedDB failure")),
    onCommand(command, bridge) {
      if (command.action === "resume") frozen = false;
      bridge.emitFromUnity(JSON.stringify({ kind: "local", requestId: command.requestId, ok: true, bundleJson: emptyBundle, revision: 0, lastSyncedHash: "" }));
    }
  });
  await b.start(); b.message({ type: "bts:resume", requestId: "resume-failed" }); await tick();
  assert.deepEqual(b.commands.map(command => command.action), ["prepare-resume"]);
  assert.equal(frozen, true); assert.equal(b.posts.at(-1).type, "bts:result");
  assert.equal(b.posts.at(-1).ok, false); assert.equal(b.posts.at(-1).localPersisted, false); b.close();
});

function sdk(accountId, options = {}) {
  const order = [], dbNames = [];
  let inflight = 0, maxInflight = 0, failNext = false;
  const window = { BTSAccountBridge: { accountId } };
  const Module = { preRun: [], autoSyncPersistentDataPath: true,
    addRunDependency() { order.push("dependency-added"); }, removeRunDependency() { order.push("population-complete"); } };
  const MEMFS = { mount: () => ({ node_ops: { mknod() {}, rmdir() {}, unlink() {}, mkdir() {}, symlink() {}, rename() {} } }) };
  const IDBFS = { getDB(name, callback) { dbNames.push(name); callback(null, {}); },
    syncfs(mount, populate, callback) {
      order.push(populate ? "populate" : "persist"); inflight++; maxInflight = Math.max(maxInflight, inflight);
      IDBFS.getDB(mount.mountpoint, () => setTimeout(() => { inflight--; const error = failNext ? new Error("quota") : null; failNext = false; callback(error); }, 2));
    } };
  const FS = { mkdir() { order.push("mkdir"); }, mount(type, opts, mountpoint) {
    const mount = { type, opts, mountpoint }, node = type.mount(mount); node.mount = mount; this.root = node; order.push("mount"); return node;
  }, syncfs(populate, callback) { IDBFS.syncfs(this.root.mount, populate, callback); } };
  const library = {};
  const context = vm.createContext({ window, Module, FS, IDBFS, MEMFS, Promise, Date, console, setTimeout, clearTimeout,
    LibraryManager: { library }, mergeInto(target, source) { Object.assign(target, source); }, stringToNewUTF8: value => value, UTF8ToString: value => value });
  // The real Unity SDK pre-JS registers its initialization callback first. The
  // shipping plugin postset then unshifts its hook ahead of that callback.
  vm.runInContext(sdkIdbSource, context);
  vm.runInContext(pluginSource, context);
  context.BTS_AccountFS = library.$BTS_AccountFS;
  vm.runInContext(library.$BTS_AccountFS__postset, context);
  for (const preRun of Module.preRun) preRun();
  return { window, Module, context, library, order, dbNames, IDBFS, FS,
    maxInflight: () => maxInflight, fail() { failNext = true; } };
}

test("real Unity SDK initialization uses account DB namespace before initial population", async () => {
  for (const accountId of [accountA, accountB, ""]) {
    const s = sdk(accountId); await new Promise(resolve => setTimeout(resolve, 5));
    assert.equal(s.Module.preRun.length, 2);
    assert.deepEqual(s.order.slice(0, 4), ["mkdir", "mount", "dependency-added", "populate"]);
    assert.equal(s.order.at(-1), "population-complete");
    assert.equal(s.dbNames[0], accountId ? "bts-account-" + accountId + ":/idbfs" : "/idbfs");
    assert.equal(s.library.BTS_AccountId(), accountId);
    await s.window.BTSAccountBridge.storage.waitForPersist();
    assert.ok(s.dbNames.every(name => name === s.dbNames[0]));
  }
});

test("SDK queue remains serialized and a new mutation is persisted before ACK", async () => {
  const s = sdk(accountA); await new Promise(resolve => setTimeout(resolve, 5));
  const pending = s.window.BTSAccountBridge.storage.waitForPersist();
  await new Promise(resolve => setTimeout(resolve, 1));
  s.IDBFS.queuePersist(s.Module.__unityIdbfsMount.mount);
  await pending;
  assert.equal(s.maxInflight(), 1);
  assert.ok(s.order.filter(value => value === "persist").length >= 2);
  assert.equal(s.Module.__unityIdbfsMount.mount.idbPersistState, 0);
});

test("SDK's ignored persistence errors still cause a failed durability ACK", async () => {
  const s = sdk(accountA); await new Promise(resolve => setTimeout(resolve, 5));
  s.fail(); await assert.rejects(s.window.BTSAccountBridge.storage.waitForPersist(), /保存|저장/);
  await s.window.BTSAccountBridge.storage.waitForPersist();
  assert.equal(s.maxInflight(), 1);
});
