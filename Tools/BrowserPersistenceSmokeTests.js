// Checks the shipping WebGL rename bridge without launching a Unity build.
// Run with: node Tools/BrowserPersistenceSmokeTests.js
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

const project = path.resolve(__dirname, "..");
const scratchRoot = path.join(project, "Temp");
fs.mkdirSync(scratchRoot, { recursive: true });
const scratch = fs.mkdtempSync(path.join(scratchRoot, "BrowserPersistence-"));
const library = {};
const errors = [];
const pointerStrings = new Map();
let pointer = 1;
const utf8Pointer = value => {
  const key = pointer++;
  pointerStrings.set(key, value);
  return key;
};

try {
  const plugin = path.join(project, "Assets", "Plugins", "WebGL", "BrowserPersistence.jslib");
  vm.runInNewContext(fs.readFileSync(plugin, "utf8"), {
    LibraryManager: { library },
    mergeInto: Object.assign,
    FS: { rename: fs.renameSync },
    UTF8ToString: value => pointerStrings.get(value),
    console: { error: (...args) => errors.push(args) }
  }, { filename: plugin });

  const source = path.join(scratch, "임시 저장.json");
  const destination = path.join(scratch, "연구 기록.json");
  fs.writeFileSync(source, "new valid save");
  fs.writeFileSync(destination, "previous valid save");
  assert.equal(library.BTS_RenamePersistentFile(utf8Pointer(source), utf8Pointer(destination)), 1);
  assert.equal(fs.readFileSync(destination, "utf8"), "new valid save");
  assert.equal(fs.existsSync(source), false);
  assert.equal(errors.length, 0);

  // A failed replacement must surface to C# without deleting the valid primary.
  assert.equal(library.BTS_RenamePersistentFile(utf8Pointer(source), utf8Pointer(destination)), 0);
  assert.equal(fs.readFileSync(destination, "utf8"), "new valid save");
  assert.equal(errors.length, 1);
  console.log("PASS: browser rename overwrites a save, decodes Unicode paths, and preserves the primary on failure.");
} finally {
  const resolved = path.resolve(scratch);
  assert.ok(resolved.startsWith(path.resolve(scratchRoot) + path.sep));
  fs.rmSync(resolved, { recursive: true });
}
