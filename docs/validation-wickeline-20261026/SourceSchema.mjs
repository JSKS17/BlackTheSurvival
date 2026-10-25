// Pure validation of the root-owned source freeze. No files or state are changed.
import assert from 'node:assert/strict';
import path from 'node:path';

export const runtimePattern = /^Assets\/Scripts\/Lumia\/[^/]+\.cs$/;
function hashMap(value, label) {
  assert(value && typeof value === 'object' && !Array.isArray(value), label + ' must be a hash map');
  for (const [file, hash] of Object.entries(value)) {
    assert(!path.isAbsolute(file) && !file.includes('\\') && !file.split('/').includes('..'), label + ': workspace-relative forward-slash paths required');
    assert.match(hash, /^[a-f0-9]{64}$/, label + ': invalid hash for ' + file);
  }
}

export function sourceShape(frozen, previous) {
  assert.equal(frozen.result, 'PASS'); assert.equal(frozen.version, '1.0.27');
  assert.equal(frozen.previousVersion, '1.0.26');
  assert.equal(previous.result, 'PASS'); assert.equal(previous.version, '1.0.26');
  hashMap(frozen.files, 'files'); hashMap(frozen.testSourceHashes, 'testSourceHashes');
  hashMap(previous.files, 'previous files');
  hashMap(frozen.artSourceHashes, 'artSourceHashes');
  assert(Object.keys(frozen.artSourceHashes).length > 0, 'Wickeline art/meta hashes required');
  assert(frozen.files['ProjectSettings/ProjectSettings.asset'], 'Freeze must include root PlayerSettings');
  const runtime = Object.entries(frozen.files).filter(([file]) => runtimePattern.test(file));
  assert(runtime.length > 0 && Number.isSafeInteger(frozen.runtimeSources));
  assert.equal(frozen.runtimeSources, runtime.length, 'runtimeSources must match files');
  const previousRuntime = Object.keys(previous.files).filter(file => runtimePattern.test(file));
  const paths = [...new Set([...previousRuntime, ...runtime.map(([file]) => file)])].sort();
  const changed = paths.filter(file => previous.files[file] !== frozen.files[file]).map(file => path.basename(file)).sort();
  assert(Array.isArray(frozen.changedRuntimeFiles));
  assert.equal(new Set(frozen.changedRuntimeFiles).size, frozen.changedRuntimeFiles.length, 'Duplicate changedRuntimeFiles entry');
  assert.deepEqual([...frozen.changedRuntimeFiles].sort(), changed, 'Runtime delta must include changed, added, and removed files');
  const flags = Object.fromEntries([
    ['touchScrollUnchanged', 'Assets/Scripts/Lumia/LumiaTouchScroll.cs'],
    ['mobileTypographyUnchanged', 'Assets/Scripts/Lumia/LumiaMobileTypography.cs'],
  ].map(([name, file]) => [name, previous.files[file] === frozen.files[file]]));
  for (const [name, value] of Object.entries(flags)) if (frozen[name] !== undefined) assert.equal(frozen[name], value, name);
  return { runtime, changed, rootCount: Object.keys(frozen.files).length,
    testCount: Object.keys(frozen.testSourceHashes).length, artCount: Object.keys(frozen.artSourceHashes).length, ...flags };
}
