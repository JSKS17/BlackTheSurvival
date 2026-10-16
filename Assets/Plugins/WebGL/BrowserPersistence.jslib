mergeInto(LibraryManager.library, {
  BTS_RenamePersistentFile__deps: ["$FS", "$UTF8ToString"],
  BTS_RenamePersistentFile: function (source, destination) {
    try {
      FS.rename(UTF8ToString(source), UTF8ToString(destination));
      return 1;
    } catch (error) {
      console.error("Black The Survival: could not replace browser save file.", error);
      return 0;
    }
  }
});
