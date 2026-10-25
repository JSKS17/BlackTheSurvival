mergeInto(LibraryManager.library, {
  BTS_PresenceState: function (ready, lobbyVisible) {
    var state = {
      ready: !!ready,
      lobbyVisible: !!lobbyVisible,
      logicalWidth: 1280,
      logicalHeight: 720,
      badge: { x: 340, y: 43, width: 220, height: 28 }
    };
    // Keep the latest render state available if the web UI initializes later.
    window.BTSGamePresenceState = state;
    var bridge = window.BTSPresenceBridge;
    if (bridge && typeof bridge.setGameState === "function") {
      try { bridge.setGameState(state); }
      catch (error) { console.warn("접속자 표시 상태를 전달하지 못했습니다.", error); }
    }
    window.dispatchEvent(new CustomEvent("bts:presence-state", { detail: state }));
  }
});
