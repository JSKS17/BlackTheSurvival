mergeInto(LibraryManager.library, {
  BTS_IsMobilePresentation: function () {
    var nav = window.navigator;
    return (nav.userAgentData && nav.userAgentData.mobile) ||
      /Android|iPhone|iPad|iPod/i.test(nav.userAgent || "") ||
      ((nav.maxTouchPoints || 0) > 1 && window.matchMedia("(pointer: coarse)").matches) ? 1 : 0;
  }
});
