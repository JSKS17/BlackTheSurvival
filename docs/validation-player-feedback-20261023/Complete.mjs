// Final local evidence audit; run after publication, sync, and cleanup.
// Reads proofs/source/screenshots and writes completion.json. No network/actions.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { lstat, readFile, realpath, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const proofRoot = path.dirname(fileURLToPath(import.meta.url)), workspace = path.resolve(proofRoot, '../..');
const version = '1.0.25', project = 'appgprj_6ad077cf90708191a3e75367b4b3b78c';
const samePath = (a, b) => path.resolve(a).toLowerCase() === path.resolve(b).toLowerCase();
const sha = bytes => createHash('sha256').update(bytes).digest('hex');
const inside = (root, file) => { const relative = path.relative(root, file); return relative && relative !== '..' && !relative.startsWith('..' + path.sep) && !path.isAbsolute(relative); };
async function checked(file) {
  const absolute = path.resolve(file); assert(inside(workspace, absolute), 'Outside workspace: ' + absolute);
  let current = workspace;
  for (const segment of ['', ...path.relative(workspace, absolute).split(path.sep)]) {
    if (segment) current = path.join(current, segment);
    assert(!(await lstat(current)).isSymbolicLink(), 'Linked evidence/source: ' + current);
    assert(samePath(await realpath(current), current));
  }
  return absolute;
}
async function bytes(file) { return readFile(await checked(file)); }
async function proof(name, result = 'PASS') {
  const value = JSON.parse((await bytes(path.join(proofRoot, name))).toString('utf8').replace(/^\uFEFF/, ''));
  if (result !== null) assert.equal(value.result, result, 'Required proof: ' + name);
  return value;
}
async function verifySources(hashes) {
  assert(hashes && typeof hashes === 'object' && !Array.isArray(hashes));
  for (const [relative, expected] of Object.entries(hashes)) {
    assert(!path.isAbsolute(relative) && !relative.split(/[\\/]/).includes('..'));
    assert.match(expected, /^[a-f0-9]{64}$/);
    assert.equal(sha(await bytes(path.join(workspace, relative))), expected, 'Frozen source changed: ' + relative);
  }
}
function matchesPublication(identity, publication, label) {
  assert.equal(identity.versionId, publication.saved.id, label + ' version');
  assert.equal(identity.commitSha, publication.package.commit_sha, label + ' commit');
  assert.equal(identity.deploymentId, publication.deployment.id, label + ' deployment');
  if (identity.versionNumber !== undefined) assert.equal(identity.versionNumber, publication.saved.version_number);
}

async function main() {
  const frozen = await proof('source-validation.json');
  assert.equal(frozen.version, version); assert.equal(frozen.previousVersion, '1.0.24');
  assert.equal(Object.keys(frozen.files).length, 120);
  const runtime = Object.keys(frozen.files).filter(file => /^Assets\/Scripts\/Lumia\/[^/]+\.cs$/.test(file));
  assert.equal(runtime.length, 119); assert.equal(frozen.changedRuntimeFiles.length, 14);
  await verifySources(frozen.files); await verifySources(frozen.testSourceHashes);
  const snapshot = await proof('build-source-validation.json');
  assert.equal(snapshot.version, version); assert.equal(snapshot.previousVersion, '1.0.24');
  assert.equal(snapshot.runtimeSources, 119); assert.equal(snapshot.frozenRootFiles, 120);
  assert.equal(snapshot.sourceSnapshotMatches, true); assert.equal(snapshot.touchScrollUnchanged, true);
  assert.equal(snapshot.mobileTypographyUnchanged, true);
  assert.deepEqual(snapshot.changedRuntimeFiles, [...frozen.changedRuntimeFiles].sort());
  assert.deepEqual(snapshot.hashes, frozen.files); assert.deepEqual(snapshot.testSourceHashes, frozen.testSourceHashes);
  const core = await proof('core-tests.json');
  assert(Number.isSafeInteger(core.assertions) && core.assertions > 0);
  assert.deepEqual(core.sourceHashes, frozen.files); assert.deepEqual(core.testSourceHashes, frozen.testSourceHashes);
  const build = await proof('unity-build-manifest.json', 'Succeeded');
  assert.equal(build.productVersion, version); assert.equal(build.errors, 0);
  const browser = await proof('browser-validation.json');
  assert.equal(browser.gameVersion, version); assert.equal(browser.physicalHardwareTested, false);
  assert(Array.isArray(browser.screenshots) && browser.screenshots.length > 0);
  for (const file of browser.screenshots) {
    assert(typeof file === 'string' && inside(proofRoot, path.resolve(proofRoot, file)), 'Screenshot must stay in this proof directory');
    assert((await lstat(await checked(path.join(proofRoot, file)))).isFile());
  }
  const live = await proof('live-validation.json');
  assert.equal(live.gameVersion, version); assert.equal(Object.keys(live.checks).length, 21);
  assert(Object.values(live.checks).every(value => value === true)); assert.deepEqual(live.errors, []);
  const publication = await proof('publish-state.json', null);
  for (const value of [publication.package, publication.saved, publication.deployment]) assert.equal(value.project_id, project);
  assert.match(publication.package.commit_sha, /^[a-f0-9]{40}$/);
  assert.equal(publication.saved.source.commit_sha, publication.package.commit_sha);
  assert.equal(publication.deployment.version_id, publication.saved.id);
  assert.equal(publication.deployment.status, 'succeeded'); assert(publication.saved.version_number > 49);
  assert(samePath(publication.package.checkout_path, path.join(workspace, 'Temp/SitePlayerFeedback20261023')));
  matchesPublication(live.publication, publication, 'Live');
  const sync = await proof('release-sync.json'); assert.equal(sync.version, version);
  matchesPublication(sync.publication, publication, 'Canonical sync');
  const manifestBytes = await bytes(path.join(workspace, 'WebSite/lib/unity-assets-manifest.json'));
  const manifest = JSON.parse(manifestBytes.toString('utf8')), manifestSha256 = sha(manifestBytes);
  assert.equal(manifest.ready, true); assert.equal(manifest.buildId, live.gameBuildId);
  assert.equal(manifest.buildId, sync.installed.buildId);
  assert.equal(manifestSha256, live.gameManifestSha256); assert.equal(manifestSha256, sync.installed.manifestSha256);
  const packagedBytes = await bytes(path.join(workspace, 'WebSite/public/game/build-manifest.json'));
  const packaged = JSON.parse(packagedBytes.toString('utf8').replace(/^\uFEFF/, ''));
  assert.equal(packaged.productVersion, version); assert.equal(packaged.result, 'Succeeded'); assert.equal(packaged.errors, 0);
  assert.equal(sha(packagedBytes), manifest.files['/game/build-manifest.json'].sha256);
  const helper = await proof('helper-prep-validation.json');
  const protectedHashes = Object.fromEntries(Object.entries(helper.baselineHashes)
    .filter(([file]) => file !== 'WebSite/lib/unity-assets-manifest.json'));
  assert.equal(Object.keys(protectedHashes).length, 7); await verifySources(protectedHashes);
  const cleanup = await proof('cleanup.json'); assert(samePath(cleanup.workspace, workspace));
  const targets = ['Build', 'Temp/SitePlayerFeedback20261023', 'Temp/PlayerFeedbackRelease20261023',
    'docs/validation-player-feedback-20261023/site.tar.gz', 'docs/validation-player-feedback-20261023/bridge.tar.gz'];
  const expectedTargets = targets.map(file => path.resolve(workspace, file).toLowerCase()).sort();
  assert.deepEqual(cleanup.targets.map(target => path.resolve(target.path).toLowerCase()).sort(), expectedTargets);
  assert.deepEqual(cleanup.removed.map(target => path.resolve(target.path).toLowerCase()).sort(), expectedTargets);
  assert.deepEqual(cleanup.absentTargets, []); assert.equal(cleanup.noProcessWasStopped, true);
  assert(Number.isSafeInteger(cleanup.removedBytes) && cleanup.removedBytes >= 0);
  assert.equal(cleanup.removedBytes, cleanup.removed.reduce((total, target) => total + target.bytes, 0));
  for (const file of targets) {
    try { await lstat(path.join(workspace, file)); assert.fail('Cleanup target remains: ' + file); }
    catch (error) { if (error.code !== 'ENOENT') throw error; }
  }
  const completion = { result: 'PASS', completedAtUtc: new Date().toISOString(), site: 'https://bser-sts.com/',
    gameVersion: version, siteVersion: publication.saved.version_number, sourceCommit: publication.package.commit_sha,
    deploymentId: publication.deployment.id, environmentRevision: publication.deployment.env_set_revision,
    gameBuildId: manifest.buildId, manifestSha256, runtimeSourceFiles: runtime.length,
    frozenFilesVerified: Object.keys(frozen.files).length, frozenTestSourcesVerified: Object.keys(frozen.testSourceHashes).length,
    changedRuntimeFiles: snapshot.changedRuntimeFiles, tests: { result: core.result, assertions: core.assertions },
    build: { result: build.result, errors: build.errors, warnings: build.warnings },
    browserProof: 'browser-validation.json', physicalHardwareTested: false, screenshots: browser.screenshots,
    liveChecks: Object.keys(live.checks).length, protectedWebFilesVerified: Object.keys(protectedHashes).length,
    cleanupTargetsAbsent: targets.length, removedTargets: cleanup.removed.length, removedBytes: cleanup.removedBytes,
    freeBytesAfter: cleanup.freeBytesAfter, limitation: 'Browser verification completed; physical mobile hardware was not tested.' };
  await writeFile(path.join(proofRoot, 'completion.json'), JSON.stringify(completion, null, 2) + '\n');
  console.log(JSON.stringify({ result: completion.result, gameVersion: version, siteVersion: completion.siteVersion,
    verifiedFrozenFiles: completion.frozenFilesVerified, assertions: core.assertions, liveChecks: completion.liveChecks,
    removedTargets: completion.removedTargets, physicalHardwareTested: false }));
}
try { await main(); }
catch (error) { console.error(JSON.stringify({ result: 'FAIL', error: error.message })); process.exitCode = 1; }
