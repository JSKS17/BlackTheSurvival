// One-time docs/ helper generation only. Does not build, serve, upload, or publish.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { copyFile, lstat, mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const workspace = path.resolve(here, '../..');
const previousRoot = path.join(workspace, 'docs/validation-player-feedback-20261023');
const sha = bytes => createHash('sha256').update(bytes).digest('hex');
const json = async file => JSON.parse((await readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
assert(!(await lstat(here)).isSymbolicLink());
const completion = await json(path.join(previousRoot, 'completion.json'));
const publication = await json(path.join(previousRoot, 'publish-state.json'));
assert.equal(completion.result, 'PASS'); assert.equal(completion.gameVersion, '1.0.25');
assert.equal(completion.siteVersion, 51); assert.equal(completion.sourceCommit, '24f431c9fdbb006f28a476aff6302ba41e89054d');
assert.equal(publication.saved.version_number, 51); assert.equal(publication.package.commit_sha, completion.sourceCommit);
assert.equal(publication.saved.source.commit_sha, completion.sourceCommit); assert.equal(publication.deployment.status, 'succeeded');
const manifestBytes = await readFile(path.join(workspace, 'WebSite/lib/unity-assets-manifest.json'));
assert.equal(sha(manifestBytes), completion.manifestSha256);
const baselineManifest = JSON.parse(manifestBytes);
assert.equal(baselineManifest.buildId, completion.gameBuildId);
assert.equal((await json(path.join(workspace, 'WebSite/public/game/build-manifest.json'))).productVersion, '1.0.25');
const baselineHashes = {};
for (const relative of ['WebSite/lib/unity-assets-manifest.json', 'WebSite/lib/game-accounts.mjs', 'WebSite/app/page.tsx',
  'WebSite/.gitattributes', 'WebSite/app/game.css', 'WebSite/package-lock.json', 'WebSite/scripts/upload-unity.mjs', 'WebSite/scripts/package-unity.mjs'])
  baselineHashes[relative] = sha(await readFile(path.join(workspace, relative)));
const previousHelper = await json(path.join(previousRoot, 'helper-prep-validation.json'));
for (const [relative, hash] of Object.entries(baselineHashes)) if (!relative.endsWith('unity-assets-manifest.json'))
  assert.equal(hash, previousHelper.baselineHashes[relative], 'Protected canonical baseline changed: ' + relative);

async function writeNew(name, value) {
  const target = path.join(here, name);
  try { await writeFile(target, value, { flag: 'wx' }); }
  catch (error) {
    if (error.code !== 'EEXIST') throw error;
    assert.equal((await readFile(target)).toString('utf8'), value, 'Existing helper differs; refusing to overwrite ' + name);
  }
}
const replacements = [
  ['validation-player-feedback-20261023', 'validation-skill-references-20261025'],
  ['SitePlayerFeedback20261023', 'SiteSkillReferences20261025'],
  ['PlayerFeedbackRelease20261023', 'SkillReferencesRelease20261025'],
  ['PlayerFeedbackRelease', 'SkillReferencesRelease'], ['player-feedback', 'skill-references'],
  ['Web125', 'Web126'], ['1.0.25', '1.0.26'], ['1.0.24', '1.0.25'],
  ['1e0633db77b024d6f4d85f46c92d4269ce80723e92bf21057d4361c9994b73d4', completion.manifestSha256],
  ['1c0f270115c544860886030bf1028a0aec060e5d', completion.sourceCommit],
  ['8fa247b1755a774777d8e213', completion.gameBuildId],
  ['versionNumber: 49', 'versionNumber: 51'], ['version_number > 49', 'version_number > 51'],
  ['version_number -le 49', 'version_number -le 51'], ['baseline Site version 49.', 'baseline Site version 51.'],
  ['port = 8246', 'port = 8247'], ['플레이어 피드백', '실험체 연계 설명'],
];
function adapt(source) { for (const [before, after] of replacements) source = source.replaceAll(before, after); return source; }
function replaceOnce(source, from, to) {
  assert.equal(source.split(from).length, 2, 'Expected one helper adaptation anchor: ' + from.slice(0, 100));
  return source.replace(from, to);
}
const inputs = {};
for (const name of ['PrepareRelease.mjs', 'VerifyBridge.mjs', 'VerifyLive.mjs', 'Cleanup.ps1', 'Preview.mjs', 'ValidateBuildSnapshot.mjs', 'Complete.mjs']) {
  const input = await readFile(path.join(previousRoot, name)); inputs[name] = sha(input);
  let source = adapt(input.toString('utf8'));
  if (name === 'Cleanup.ps1') source = '#requires -Version 7.0\n' + source;
  if (name === 'Preview.mjs') {
    source = replaceOnce(source,
      "const labels = { 'feedback-crafting': '제작 재료·장비 효과 범위', 'feedback-combat': '유키 W·E / 아야 E 연계', research: '연구 기록' };",
      "const labels = { 'yuki-combat': '유키 W·E 기준 장면', research: '연구 기록' };");
    source = source.replaceAll("names.has('feedback-crafting')", "names.has('yuki-combat')").replaceAll("select.value='feedback-crafting'", "select.value='yuki-combat'");
  }
  if (name === 'ValidateBuildSnapshot.mjs') {
    source = replaceOnce(source, "import assert from 'node:assert/strict';", "import assert from 'node:assert/strict';\nimport { sourceShape } from './SourceSchema.mjs';");
    const start = source.indexOf("const frozen = await json(");
    const end = source.indexOf("const settings =", start);
    assert(start > 0 && end > start);
    source = source.slice(0, start) + `const frozen = await json(path.join(proofRoot, 'source-validation.json'));
const previous = await json(path.join(workspace, 'docs/validation-player-feedback-20261023/source-validation.json'));
const shape = sourceShape(frozen, previous);
const { runtime, changed, touchScrollUnchanged, mobileTypographyUnchanged } = shape;
await verifyMap(frozen.files); await verifyMap(frozen.testSourceHashes);
const expectedNames = runtime.map(([file]) => path.basename(file)).sort();
for (const root of [workspace, cloneRoot]) {
  const directory = await checked(path.join(root, 'Assets/Scripts/Lumia'));
  assert.deepEqual((await readdir(directory)).filter(name => name.endsWith('.cs')).sort(), expectedNames);
}
for (const [relative, expected] of runtime)
  assert.equal(sha(await bytes(path.join(cloneRoot, relative))), expected, 'Cloned C# changed: ' + relative);

` + source.slice(end);
    source = replaceOnce(source,
      '119 cloned runtime C# files and all 120 frozen root files byte exact; frozen Tools sources byte exact; cloned settings differ only by exact standard WebGL overrides',
      'All runtime C# and frozen root/Tools files match the actual source-validation maps; cloned settings differ only by exact standard WebGL overrides');
  }
  if (name === 'Complete.mjs') {
    source = replaceOnce(source, "import assert from 'node:assert/strict';", "import assert from 'node:assert/strict';\nimport { sourceShape } from './SourceSchema.mjs';");
    const start = source.indexOf("  const frozen = await proof('source-validation.json');");
    const end = source.indexOf("  const core =", start);
    assert(start > 0 && end > start);
    source = source.slice(0, start) + `  const frozen = await proof('source-validation.json');
  const previous = JSON.parse((await bytes(path.join(workspace, 'docs/validation-player-feedback-20261023/source-validation.json'))).toString('utf8').replace(/^\\uFEFF/, ''));
  const shape = sourceShape(frozen, previous);
  const runtime = shape.runtime.map(([file]) => file);
  await verifySources(frozen.files); await verifySources(frozen.testSourceHashes);
  const snapshot = await proof('build-source-validation.json');
  assert.equal(snapshot.version, version); assert.equal(snapshot.previousVersion, '1.0.25');
  assert.equal(snapshot.runtimeSources, runtime.length); assert.equal(snapshot.frozenRootFiles, shape.rootCount);
  assert.equal(snapshot.testSourceFiles, shape.testCount); assert.equal(snapshot.sourceSnapshotMatches, true);
  assert.equal(snapshot.touchScrollUnchanged, shape.touchScrollUnchanged);
  assert.equal(snapshot.mobileTypographyUnchanged, shape.mobileTypographyUnchanged);
  assert.deepEqual(snapshot.changedRuntimeFiles, shape.changed);
  assert.deepEqual(snapshot.hashes, frozen.files); assert.deepEqual(snapshot.testSourceHashes, frozen.testSourceHashes);
` + source.slice(end);
    source = replaceOnce(source, "assert.equal(live.gameVersion, version); assert.equal(Object.keys(live.checks).length, 21);",
      `assert.equal(live.gameVersion, version);
  for (const name of ['deploymentSucceeded', 'preservedAccountConcurrencyFix', 'unityManifestReady', 'homeLoadsCurrentBootstrap',
    'anonymousSession', 'protected:/api/save', 'protected:/api/preferences', 'gamePageMatchesSkillReferencesRelease',
    'gameManifestMatchesSkillReferencesRelease', 'uploadEndpointClosed', 'gameDataHeadOnly', 'staticWasmBytesMatchRelease'])
    assert.equal(live.checks[name], true, 'Required live check: ' + name);
  assert(Object.keys(live.checks).some(name => name.startsWith('client:')), 'Compiled client verification missing');`);
  }
  await writeNew(name, source);
}
await mkdir(path.join(here, 'fixtures'), { recursive: true });
const fixtures = [];
for (const [from, to] of [['research.json', 'research.json'], ['feedback-combat.json', 'yuki-combat.json']]) {
  const source = path.join(previousRoot, 'fixtures', from), target = path.join(here, 'fixtures', to);
  const bytes = await readFile(source); await copyFile(source, target);
  assert.equal(sha(await readFile(target)), sha(bytes));
  fixtures.push({ source: path.relative(workspace, source).replaceAll('\\', '/'), target: 'fixtures/' + to,
    bytes: bytes.length, sha256: sha(bytes), byteExact: true });
}
const baseline = { result: 'PASS', observedAtUtc: new Date().toISOString(),
  scope: 'Canonical local baseline corroborated by preserved publication/completion proofs; no live request',
  gameVersion: '1.0.25', siteVersion: 51, sourceCommit: completion.sourceCommit,
  environmentRevision: completion.environmentRevision, gameBuildId: completion.gameBuildId,
  manifestSha256: sha(manifestBytes), hashes: baselineHashes, helperInputs: inputs, copiedFixtures: fixtures,
  baselineSourceProof: 'docs/validation-player-feedback-20261023/source-validation.json',
  baselineSourceProofSha256: sha(await readFile(path.join(previousRoot, 'source-validation.json'))),
  productSourceChanged: false, oldProofsChanged: false, productionRequests: false, releaseCommandsExecuted: false };
await writeNew('baseline-provenance.json', JSON.stringify(baseline, null, 2) + '\n');
console.log(JSON.stringify({ result: 'PASS', helperFiles: Object.keys(inputs), baselineVersion: '1.0.25', version: '1.0.26',
  copiedFixtures: fixtures.map(file => file.target), productSourceChanged: false, releaseCommandsExecuted: false }));
