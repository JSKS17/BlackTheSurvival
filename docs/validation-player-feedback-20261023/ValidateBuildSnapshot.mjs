// Local read-only source/settings audit. Writes only its task evidence JSON.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { lstat, readFile, readdir, realpath, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const proofRoot = path.dirname(fileURLToPath(import.meta.url));
const workspace = path.resolve(proofRoot, '../..');
const cloneRoot = path.join(workspace, 'Build/UnityWebBuild');
const sha = bytes => createHash('sha256').update(bytes).digest('hex');
async function checked(file) {
  const absolute = path.resolve(file), relative = path.relative(workspace, absolute);
  assert(relative && relative !== '..' && !relative.startsWith('..' + path.sep) && !path.isAbsolute(relative));
  let current = workspace;
  for (const segment of ['', ...relative.split(path.sep)]) {
    if (segment) current = path.join(current, segment);
    assert(!(await lstat(current)).isSymbolicLink(), 'Linked source refused: ' + current);
    assert.equal((await realpath(current)).toLowerCase(), current.toLowerCase());
  }
  return absolute;
}
async function bytes(file) { return readFile(await checked(file)); }
async function json(file) { return JSON.parse((await bytes(file)).toString('utf8').replace(/^\uFEFF/, '')); }
async function verifyMap(hashes) {
  assert(hashes && typeof hashes === 'object' && !Array.isArray(hashes));
  for (const [relative, expected] of Object.entries(hashes)) {
    assert(!path.isAbsolute(relative) && !relative.split(/[\\/]/).includes('..'));
    assert.match(expected, /^[a-f0-9]{64}$/);
    assert.equal(sha(await bytes(path.join(workspace, relative))), expected, 'Frozen root changed: ' + relative);
  }
}

const frozen = await json(path.join(proofRoot, 'source-validation.json'));
assert.equal(frozen.result, 'PASS'); assert.equal(frozen.version, '1.0.25');
assert.equal(frozen.previousVersion, '1.0.24'); assert.equal(frozen.runtimeSources, 119);
assert.equal(Object.keys(frozen.files).length, 120);
await verifyMap(frozen.files); await verifyMap(frozen.testSourceHashes);
const runtime = Object.entries(frozen.files).filter(([file]) => /^Assets\/Scripts\/Lumia\/[^/]+\.cs$/.test(file));
assert.equal(runtime.length, 119);
const expectedNames = runtime.map(([file]) => path.basename(file)).sort();
for (const root of [workspace, cloneRoot]) {
  const directory = await checked(path.join(root, 'Assets/Scripts/Lumia'));
  assert.deepEqual((await readdir(directory)).filter(name => name.endsWith('.cs')).sort(), expectedNames);
}
for (const [relative, expected] of runtime)
  assert.equal(sha(await bytes(path.join(cloneRoot, relative))), expected, 'Cloned C# changed: ' + relative);

const previous = await json(path.join(workspace, 'docs/validation-combat-card-scroll-20261022/source-validation.json'));
assert.equal(previous.result, 'PASS'); assert.equal(previous.version, '1.0.24');
assert.equal(Object.keys(previous.files).length, 120);
const previousRuntime = Object.keys(previous.files).filter(file => /^Assets\/Scripts\/Lumia\/[^/]+\.cs$/.test(file));
assert.deepEqual(previousRuntime.sort(), runtime.map(([file]) => file).sort());
const changed = runtime.filter(([file, hash]) => previous.files[file] !== hash).map(([file]) => path.basename(file)).sort();
assert.equal(changed.length, 14);
assert.deepEqual(changed, [...frozen.changedRuntimeFiles].sort(), 'Changed runtime list must match the frozen proof');
const touchScrollUnchanged = previous.files['Assets/Scripts/Lumia/LumiaTouchScroll.cs'] === frozen.files['Assets/Scripts/Lumia/LumiaTouchScroll.cs'];
const mobileTypographyUnchanged = previous.files['Assets/Scripts/Lumia/LumiaMobileTypography.cs'] === frozen.files['Assets/Scripts/Lumia/LumiaMobileTypography.cs'];
assert.equal(touchScrollUnchanged, true); assert.equal(mobileTypographyUnchanged, true);

const settings = 'ProjectSettings/ProjectSettings.asset';
const rootBytes = await bytes(path.join(workspace, settings));
const cloneBytes = await bytes(path.join(cloneRoot, settings));
let expected = rootBytes.toString('utf8').replace(/\r\n/g, '\n');
const replacements = [
  ['  m_BuildTargetBatching:\n  - m_BuildTarget: Standalone\n    m_StaticBatching: 1\n    m_DynamicBatching: 0\n', '  m_BuildTargetBatching:\n  - m_BuildTarget: Standalone\n    m_StaticBatching: 1\n    m_DynamicBatching: 0\n  - m_BuildTarget: WebGL\n    m_StaticBatching: 0\n    m_DynamicBatching: 0\n'],
  ['  webGLNameFilesAsHashes: 0\n', '  webGLNameFilesAsHashes: 1\n'],
  ['  webGLTemplate: APPLICATION:Default\n', '  webGLTemplate: PROJECT:BlackTheSurvival\n'],
  ['  webGLCompressionFormat: 0\n', '  webGLCompressionFormat: 1\n'],
  ['  webGLDecompressionFallback: 0\n', '  webGLDecompressionFallback: 1\n'],
  ['  webGLInitialMemorySize: 32\n', '  webGLInitialMemorySize: 256\n'],
  ['  scriptingBackend:\n    Android: 1\n    Standalone: 0\n', '  scriptingBackend:\n    Android: 1\n    Standalone: 0\n    WebGL: 1\n'],
];
for (const [from, to] of replacements) {
  assert.equal(expected.split(from).length, 2, 'Isolated build override must match exactly once: ' + from);
  expected = expected.replace(from, to);
}
assert.equal(expected.split('  preloadedAssets: []\n').length, 2);
const withPreload = expected.replace('  preloadedAssets: []\n',
  '  preloadedAssets:\n  - {fileID: -944628639613478452, guid: 2bcd2660ca9b64942af0de543d8d7100, type: 3}\n');
const actual = cloneBytes.toString('utf8').replace(/\r\n/g, '\n');
assert(actual === expected || actual === withPreload,
  'Clone settings must equal the root plus only standard WebGL overrides and the exact optional preload asset');
const report = { result: 'PASS', checkedAtUtc: new Date().toISOString(), previousVersion: '1.0.24', version: '1.0.25',
  runtimeSources: runtime.length, frozenRootFiles: Object.keys(frozen.files).length,
  testSourceFiles: Object.keys(frozen.testSourceHashes).length, changedRuntimeFiles: changed,
  touchScrollUnchanged, mobileTypographyUnchanged, sourceSnapshotMatches: true,
  hashes: frozen.files, testSourceHashes: frozen.testSourceHashes,
  cloneSettingsSha256: sha(cloneBytes), rootSettingsSha256: sha(rootBytes),
  settingsOverrides: ['WebGL batching 0/0', 'hashed asset names', 'PROJECT:BlackTheSurvival template',
    'Gzip compression', 'decompression fallback', 'initial memory 256 MiB', 'WebGL IL2CPP backend; Android 1 and Standalone 0 preserved'],
  preloadedAssets: actual === withPreload ? 'Exact build preload: fileID -944628639613478452 / guid 2bcd2660ca9b64942af0de543d8d7100 / type 3' : 'Empty, matching root',
  snapshotScope: '119 cloned runtime C# files and all 120 frozen root files byte exact; frozen Tools sources byte exact; cloned settings differ only by exact standard WebGL overrides',
};
await writeFile(path.join(proofRoot, 'build-source-validation.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({ result: report.result, runtimeSources: report.runtimeSources, frozenRootFiles: report.frozenRootFiles,
  testSourceFiles: report.testSourceFiles, changedRuntimeFiles: changed, cloneSettingsSha256: report.cloneSettingsSha256 }));
