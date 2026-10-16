mergeInto(LibraryManager.library, {
  $BTS_AccountFS__deps: ["$FS", "$IDBFS"],
  $BTS_AccountFS__postset: "Module['preRun'].unshift(function () { BTS_AccountFS.install(); });",
  $BTS_AccountFS: {
    completed: 0,
    lastError: null,
    initialError: null,
    installed: false,
    install: function () {
      if (BTS_AccountFS.installed) return;
      BTS_AccountFS.installed = true;
      var bridge = window.BTSAccountBridge;
      var accountId = bridge ? bridge.accountId : "";
      if (accountId && !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(accountId))
        throw new Error("Invalid account storage namespace.");

      // IDBFS reconciles the entire mount, not Application.persistentDataPath's
      // URL-derived subfolder. Give each account its own IndexedDB database.
      // Keep the old database name for a standalone guest build.
      var getDB = IDBFS.getDB;
      if (accountId) IDBFS.getDB = function (name, callback) {
        return getDB.call(IDBFS, "bts-account-" + accountId + ":" + name, callback);
      };
      var syncfs = IDBFS.syncfs;
      IDBFS.syncfs = function (mount, populate, callback) {
        return syncfs.call(IDBFS, mount, populate, function (error) {
          if (!populate && Module.__unityIdbfsMount && mount === Module.__unityIdbfsMount.mount) {
            BTS_AccountFS.completed++;
            BTS_AccountFS.lastError = error || null;
          }
          callback(error);
        });
      };

      // The SDK calls this hook after installing its automatic, serialized
      // persistence queue, and before Unity's main/Awake can read any save.
      Module["unityFileSystemInit"] = function () {
        FS.mkdir("/idbfs");
        Module.__unityIdbfsMount = FS.mount(IDBFS, { autoPersist: !!Module["autoSyncPersistentDataPath"] }, "/idbfs");
        Module.addRunDependency("JS_FileSystem_Mount");
        FS.syncfs(true, function (error) {
          BTS_AccountFS.initialError = error || null;
          Module.removeRunDependency("JS_FileSystem_Mount");
        });
      };
      if (bridge) bridge.storage = {
        waitForPersist: function () { return BTS_AccountFS.waitForPersist(); }
      };
    },
    waitForPersist: function () {
      return new Promise(function (resolve, reject) {
        if (BTS_AccountFS.initialError || !Module.__unityIdbfsMount) {
          reject(new Error("브라우저 저장 공간을 사용할 수 없습니다. 저장 공간과 브라우저 설정을 확인해 주세요."));
          return;
        }
        var mount = Module.__unityIdbfsMount.mount;
        var previous = BTS_AccountFS.completed;
        var deadline = Date.now() + 30000;
        // Use Unity's existing queue; a separate FS.syncfs(false) could race it.
        IDBFS.queuePersist(mount);
        function check() {
          if (!mount.idbPersistState && BTS_AccountFS.completed > previous) {
            if (BTS_AccountFS.lastError) reject(new Error("브라우저에 기록을 저장하지 못했습니다. 저장 공간을 확인한 뒤 다시 시도해 주세요."));
            else resolve(true);
          } else if (Date.now() >= deadline) {
            reject(new Error("브라우저 저장이 끝나지 않았습니다. 잠시 후 다시 시도해 주세요."));
          } else setTimeout(check, 10);
        }
        check();
      });
    }
  },

  BTS_AccountId__deps: ["$BTS_AccountFS", "$stringToNewUTF8"],
  BTS_AccountId: function () {
    return stringToNewUTF8(window.BTSAccountBridge ? window.BTSAccountBridge.accountId || "" : "");
  },
  BTS_AccountEmit__deps: ["$UTF8ToString", "$BTS_AccountFS"],
  BTS_AccountEmit: function (json) {
    if (window.BTSAccountBridge) window.BTSAccountBridge.emitFromUnity(UTF8ToString(json));
  },
  BTS_AccountDirty__deps: ["$BTS_AccountFS"],
  BTS_AccountDirty: function () {
    if (window.BTSAccountBridge) window.BTSAccountBridge.markDirty();
  }
});
