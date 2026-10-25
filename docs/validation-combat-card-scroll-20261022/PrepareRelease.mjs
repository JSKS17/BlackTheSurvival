// Local release preparation only. This file never contacts Sites or uploads data.
// Usage: node docs/validation-combat-card-scroll-20261022/PrepareRelease.mjs prepare|finalize|sync
import assert from 'node:assert/strict';
import { createHash, randomUUID } from 'node:crypto';
import { createReadStream } from 'node:fs';
import { copyFile, lstat, mkdir, readFile, readdir, realpath, rename, rm, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const proofRoot = path.dirname(fileURLToPath(import.meta.url));
const workspace = path.resolve(proofRoot, '../..');
const canonical = path.join(workspace, 'WebSite');
const stage = path.join(workspace, 'Temp/SiteCombatCardScroll20261022');
const release = path.join(workspace, 'Temp/CombatCardScrollRelease20261022');
const build = path.join(workspace, 'Build/Web124');
const game = root => path.join(root, 'public/game');
const manifestPath = root => path.join(root, 'lib/unity-assets-manifest.json');
const expectedManifest = '3555cdf0a36169907fffca5dbd7b8f6befdf796fffe49d95eca4ef3abbbe1c13';
const baselinePublication = { versionNumber: 47, commitSha: '16d05d61ab7099b7692216e97e8de82798d47fa2' };
const protectedHashes = {
  'lib/game-accounts.mjs': '8e267ad2fd69c8fb892ef6e17efd0f49cea96a17986d428012ac85dbad373e33',
  'app/page.tsx': '16a10237fce86c4c6239a03c73f4812d077bb567e58b024d252ac3a4441e0b77',
  '.gitattributes': 'fcd4902b9b599e41443f819411a5c47fb77218e5efd2b7be19b62841675ccc5a',
  'app/game.css': '22704ccabea784d856a9e068cb7e3d34d983a36804363a80c0e33d512f7aa1da',
};
const mode = process.argv[2];
const report = { mode, result: 'RUNNING', startedAtUtc: new Date().toISOString(), workspace,
  version: '1.0.24', baselinePublication, productionRequests: false, steps: {}, removed: [] };
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const samePath = (a, b) => path.resolve(a).toLowerCase() === path.resolve(b).toLowerCase();
const inside = (parent, child) => {
  const relative = path.relative(parent, child);
  return relative !== '' && relative !== '..' && !relative.startsWith(`..${path.sep}`) && !path.isAbsolute(relative);
};

// Inspect every existing ancestor, including the target. Junctions/symlinks are
// refused before reading a tree, copying, moving, replacing or deleting it.
async function checked(filename, { absent = false } = {}) {
  const absolute = path.resolve(filename);
  assert(inside(workspace, absolute), `Outside workspace: ${absolute}`);
  let cursor = workspace;
  for (const segment of ['', ...path.relative(workspace, absolute).split(path.sep)]) {
    if (segment) cursor = path.join(cursor, segment);
    let info;
    try { info = await lstat(cursor); }
    catch (error) { if (error.code === 'ENOENT' && absent) continue; throw error; }
    assert(!info.isSymbolicLink(), `Linked path refused: ${cursor}`);
    assert(samePath(await realpath(cursor), cursor), `Resolved path changed: ${cursor}`);
  }
  return absolute;
}

async function exists(filename) {
  try { await lstat(filename); return true; }
  catch (error) { if (error.code === 'ENOENT') return false; throw error; }
}

async function fileHash(filename) {
  await checked(filename);
  const digest = createHash('sha256');
  for await (const bytes of createReadStream(filename)) digest.update(bytes);
  return digest.digest('hex');
}

async function json(filename) {
  await checked(filename);
  return JSON.parse((await readFile(filename, 'utf8')).replace(/^\uFEFF/, ''));
}

async function tree(directory) {
  await checked(directory);
  assert((await lstat(directory)).isDirectory(), `Expected directory: ${directory}`);
  const files = [];
  async function walk(current, relative = '') {
    for (const entry of (await readdir(current, { withFileTypes: true })).sort((a, b) => a.name.localeCompare(b.name))) {
      const filename = path.join(current, entry.name);
      await checked(filename);
      if (entry.isDirectory()) await walk(filename, path.join(relative, entry.name));
      else {
        assert(entry.isFile(), `Nonordinary release entry: ${filename}`);
        files.push({ path: path.join(relative, entry.name).split(path.sep).join('/'),
          size: (await lstat(filename)).size, sha256: await fileHash(filename) });
      }
    }
  }
  await walk(directory);
  return { sha256: hash(Buffer.from(JSON.stringify(files))), bytes: files.reduce((n, f) => n + f.size, 0), files };
}

async function makeDirectory(directory) {
  await checked(directory, { absent: true });
  await mkdir(directory, { recursive: true });
  await checked(directory);
}

async function copyTree(source, destination) {
  const inventory = await tree(source);
  assert(!await exists(destination), `Copy destination already exists: ${destination}`);
  await makeDirectory(destination);
  for (const entry of inventory.files) {
    const from = path.join(source, ...entry.path.split('/'));
    const to = path.join(destination, ...entry.path.split('/'));
    await checked(from); await makeDirectory(path.dirname(to)); await checked(to, { absent: true });
    await copyFile(from, to);
    assert.equal(await fileHash(to), entry.sha256, entry.path);
  }
  assert.equal((await tree(destination)).sha256, inventory.sha256);
  return inventory;
}

async function move(source, destination) {
  await checked(source); await checked(destination, { absent: true });
  assert(!await exists(destination), `Move destination exists: ${destination}`);
  await makeDirectory(path.dirname(destination));
  await rename(source, destination);
}

async function removeOwned(filename) {
  const absolute = await checked(filename);
  assert(inside(release, absolute), `Deletion must remain inside owned release Temp: ${absolute}`);
  const info = await lstat(absolute);
  const before = info.isDirectory() ? await tree(absolute) : { bytes: info.size };
  await checked(absolute);
  await rm(absolute, { recursive: info.isDirectory(), force: false });
  assert(!await exists(absolute));
  report.removed.push({ path: absolute, bytes: before.bytes });
}

async function protectedSources() {
  const checkedHashes = {};
  for (const root of [canonical, stage]) for (const [relative, expected] of Object.entries(protectedHashes)) {
    const filename = path.join(root, relative);
    checkedHashes[path.relative(workspace, filename)] = await fileHash(filename);
    assert.equal(checkedHashes[path.relative(workspace, filename)], expected, `Protected source changed: ${filename}`);
  }
  return checkedHashes;
}

function validateManifest(manifest) {
  assert.equal(manifest.ready, true); assert.equal(manifest.schemaVersion, 1);
  assert.equal(manifest.basePath, '/game'); assert.match(manifest.buildId, /^[a-f0-9]{24}$/);
  assert(manifest.files && manifest.buildFiles);
  for (const [url, record] of Object.entries(manifest.files)) {
    assert(url.startsWith('/game/') && !url.includes('\\'));
    const segments = url.slice('/game/'.length).split('/').map(decodeURIComponent);
    assert(segments.every(s => s && s !== '.' && s !== '..' && !/[\\/\0]/.test(s)));
    assert(Number.isSafeInteger(record.size) && record.size > 0); assert.match(record.sha256, /^[a-f0-9]{64}$/);
    if (record.storage === 'r2') {
      assert.equal(record.objectKey, `unity-assets/${record.sha256}`);
      assert(Array.isArray(record.uploadParts) && record.uploadParts.length > 0);
      let size = 0;
      for (const [index, part] of record.uploadParts.entries()) {
        assert.equal(part.partNumber, index + 1); assert.match(part.sha256, /^[a-f0-9]{64}$/);
        assert(Number.isSafeInteger(part.size) && part.size > 0 && part.size <= 16 * 1024 * 1024);
        if (index < record.uploadParts.length - 1) assert(part.size >= 5 * 1024 * 1024);
        size += part.size;
      }
      assert.equal(size, record.size);
    }
  }
  for (const url of Object.values(manifest.buildFiles)) assert(manifest.files[url]);
}

async function baseline() {
  assert.equal(await fileHash(manifestPath(canonical)), expectedManifest, 'Canonical baseline manifest changed');
  const manifest = await json(manifestPath(canonical)); validateManifest(manifest);
  return manifest;
}

// Check physical direct/chunk bytes against logical records, including ordered
// concatenation. This catches CRLF conversion without guessing file extensions.
async function validatePackagedAssets(root, manifest) {
  validateManifest(manifest);
  let count = 0;
  for (const [url, record] of Object.entries(manifest.files)) {
    if (record.storage === 'r2') continue;
    assert(Array.isArray(record.parts) && record.parts.length > 0, `Missing physical parts: ${url}`);
    const digest = createHash('sha256'); let total = 0;
    for (const part of record.parts) {
      assert(typeof part.path === 'string' && part.path.startsWith('/game/'));
      const segments = part.path.slice('/game/'.length).split('/').map(decodeURIComponent);
      assert(segments.every(s => s && s !== '.' && s !== '..' && !/[\\/\0]/.test(s)));
      const filename = await checked(path.join(game(root), ...segments));
      assert((await lstat(filename)).isFile(), `Expected physical part: ${part.path}`);
      assert.equal((await lstat(filename)).size, part.size, `Part size: ${part.path}`);
      for await (const bytes of createReadStream(filename)) { digest.update(bytes); total += bytes.length; }
    }
    assert.equal(total, record.size, `Logical size: ${url}`);
    assert.equal(digest.digest('hex'), record.sha256, `Logical bytes: ${url}`);
    count++;
  }
  return count;
}

async function proof(name) {
  const value = await json(path.join(proofRoot, name));
  assert.equal(value.result, 'PASS', `Required proof is not PASS: ${name}`);
  return value;
}

async function releaseState() {
  const prepared = await proof('release-prepare.json');
  assert.equal(prepared.workspace, workspace); assert.equal(prepared.version, '1.0.24');
  const manifest = await json(manifestPath(release)); validateManifest(manifest);
  assert.equal(await fileHash(manifestPath(release)), prepared.release.manifestSha256);
  assert.equal((await tree(game(release))).sha256, prepared.release.gameTreeSha256);
  assert.equal((await json(path.join(game(release), 'build-manifest.json'))).productVersion, '1.0.24');
  await validatePackagedAssets(release, manifest);
  return { prepared, manifest };
}

async function prepare() {
  report.sourceHashes = await protectedSources();
  const oldManifest = await baseline();
  report.verifiedOldStaticAssets = await validatePackagedAssets(canonical, oldManifest);
  const buildReport = await json(path.join(build, 'build-manifest.json'));
  assert.equal(buildReport.result, 'Succeeded'); assert.equal(buildReport.errors, 0);
  assert.equal(buildReport.productVersion, '1.0.24');
  await tree(build);
  if (await exists(release)) await tree(release);
  await makeDirectory(release);
  const stageManifestBefore = await json(manifestPath(stage)); validateManifest(stageManifestBefore);
  const stageManifestBeforeSha256 = await fileHash(manifestPath(stage));
  const stageBefore = await tree(game(stage));
  const canonicalBefore = await tree(game(canonical));
  const { packageUnityBuild } = await import(pathToFileURL(await checked(path.join(canonical, 'scripts/package-unity.mjs'))).href);
  const manifest = await packageUnityBuild({ source: await checked(build),
    target: game(release), publicRoot: path.join(release, 'public'),
    manifestPath: manifestPath(release), basePath: '/game', storageMode: 'r2' });
  validateManifest(manifest);
  report.verifiedNewStaticAssets = await validatePackagedAssets(release, manifest);
  const packagedReport = await json(path.join(game(release), 'build-manifest.json'));
  assert.equal(packagedReport.productVersion, '1.0.24'); assert.equal(packagedReport.result, 'Succeeded');
  const uploader = path.join(canonical, 'scripts/upload-unity.mjs');
  await checked(uploader); await makeDirectory(path.join(release, 'scripts'));
  const uploaderCopy = path.join(release, 'scripts/upload-unity.mjs');
  await checked(uploaderCopy, { absent: true }); await copyFile(uploader, uploaderCopy);
  assert.equal(await fileHash(uploaderCopy), await fileHash(uploader));

  // Retain every old lookup/metadata field, adding only new immutable R2 records.
  // A prior prepare rerun is accepted only when it is exactly this same union.
  const union = structuredClone(oldManifest);
  const added = [];
  for (const [url, record] of Object.entries(manifest.files)) {
    if (record.storage !== 'r2') continue;
    if (union.files[url]) assert.equal(union.files[url].sha256, record.sha256, `Immutable path collision: ${url}`);
    else { union.files[url] = record; added.push({ path: url, size: record.size, sha256: record.sha256 }); }
  }
  const beforeWithoutFiles = { ...stageManifestBefore }; delete beforeWithoutFiles.files;
  const expectedWithoutFiles = { ...oldManifest }; delete expectedWithoutFiles.files;
  assert.deepEqual(beforeWithoutFiles, expectedWithoutFiles, 'Stage must still serve the old build');
  for (const [url, record] of Object.entries(stageManifestBefore.files)) {
    assert(union.files[url], `Unexpected stage asset: ${url}`);
    assert.deepEqual(record, union.files[url], `Stage asset changed: ${url}`);
  }
  for (const [url, record] of Object.entries(oldManifest.files)) assert.deepEqual(stageManifestBefore.files[url], record);
  const unionBytes = Buffer.from(JSON.stringify(union, null, 2) + '\n');
  await swap(stage, stageBefore.sha256, stageManifestBeforeSha256, {
    release: { gameTreeSha256: canonicalBefore.sha256, manifestSha256: hash(unionBytes), buildId: oldManifest.buildId }
  }, { sourceGame: game(canonical), manifestBytes: unionBytes });
  assert.deepEqual((await json(manifestPath(stage))).buildFiles, oldManifest.buildFiles);
  assert.equal((await tree(game(stage))).sha256, canonicalBefore.sha256, 'Old public game is not byte-exact canonical');
  await validatePackagedAssets(stage, union);
  assert.equal(await fileHash(manifestPath(canonical)), expectedManifest);
  report.sourceHashesAfter = await protectedSources();
  report.release = { root: release, manifestSha256: await fileHash(manifestPath(release)),
    buildId: manifest.buildId, gameTreeSha256: (await tree(game(release))).sha256,
    uploaderSha256: await fileHash(uploaderCopy), r2Assets: added };
  report.baseline = { manifestSha256: expectedManifest, canonicalGameTreeSha256: canonicalBefore.sha256,
    stageOriginalGameTreeSha256: stageBefore.sha256, stageOriginalManifestSha256: stageManifestBeforeSha256,
    stageGameTreeSha256: canonicalBefore.sha256, stageUnionManifestSha256: await fileHash(manifestPath(stage)) };
  report.steps = { successfulBuildPackaged: true, isolatedUploaderCopiedUnchanged: true,
    oldPublicGameRestoredToVerifiedCanonicalBytes: true, oldBuildMetadataPreserved: true, newR2RecordsAllowed: true };
}

// The two local paths are committed by rename with rollback. Superseded files
// are moved to owned Temp before deletion; canonical paths are never rm targets.
async function swap(root, expectedTree, expectedOldManifest, prepared,
  { sourceGame = game(release), manifestBytes = null } = {}) {
  const oldGame = game(root), oldManifest = manifestPath(root);
  assert.equal(await fileHash(oldManifest), expectedOldManifest);
  assert.equal((await tree(oldGame)).sha256, expectedTree);
  const transaction = path.join(release, 'transactions', `${mode}-${randomUUID()}`);
  await makeDirectory(transaction);
  const candidateGame = path.join(transaction, 'new-game');
  const candidateManifest = path.join(transaction, 'new-manifest.json');
  const backupGame = path.join(transaction, 'old-game');
  const backupManifest = path.join(transaction, 'old-manifest.json');
  await copyTree(sourceGame, candidateGame);
  await checked(candidateManifest, { absent: true });
  if (manifestBytes) await writeFile(candidateManifest, manifestBytes);
  else await copyFile(await checked(manifestPath(release)), candidateManifest);
  assert.equal(await fileHash(candidateManifest), prepared.release.manifestSha256);
  let movedGame = false, movedManifest = false, installedGame = false, installedManifest = false;
  try {
    // Recheck immediately before moving any destination files.
    assert.equal(await fileHash(oldManifest), expectedOldManifest);
    assert.equal((await tree(oldGame)).sha256, expectedTree);
    await move(oldGame, backupGame); movedGame = true;
    await move(oldManifest, backupManifest); movedManifest = true;
    await move(candidateGame, oldGame); installedGame = true;
    await move(candidateManifest, oldManifest); installedManifest = true;
    assert.equal((await tree(oldGame)).sha256, prepared.release.gameTreeSha256);
    assert.equal(await fileHash(oldManifest), prepared.release.manifestSha256);
    await validatePackagedAssets(root, await json(oldManifest));
    report.sourceHashesAfter = await protectedSources();
  } catch (error) {
    if (installedManifest) await move(oldManifest, candidateManifest);
    if (installedGame) await move(oldGame, candidateGame);
    if (movedManifest) await move(backupManifest, oldManifest);
    if (movedGame) await move(backupGame, oldGame);
    report.rollback = 'Previous game and manifest restored';
    throw error;
  }
  // Cleanup failure cannot revert an already validated installation.
  await removeOwned(transaction);
  return { root, manifestSha256: prepared.release.manifestSha256,
    gameTreeSha256: prepared.release.gameTreeSha256, buildId: prepared.release.buildId };
}

async function finalize() {
  report.sourceHashes = await protectedSources(); await baseline();
  const { prepared } = await releaseState();
  report.installed = await swap(stage, prepared.baseline.stageGameTreeSha256,
    prepared.baseline.stageUnionManifestSha256, prepared);
  report.steps = { isolatedPackageVerified: true, stageGameAndManifestReplaced: true,
    rollbackAvailableDuringSwap: true, protectedSourceHashesPreserved: true };
}

async function sync() {
  report.sourceHashes = await protectedSources(); await baseline();
  const { prepared, manifest } = await releaseState();
  const finalized = await proof('release-finalize.json');
  assert.equal(finalized.installed.manifestSha256, prepared.release.manifestSha256);
  assert.equal(await fileHash(manifestPath(stage)), prepared.release.manifestSha256);
  assert.equal((await tree(game(stage))).sha256, prepared.release.gameTreeSha256);
  const publication = await json(path.join(proofRoot, 'publish-state.json'));
  assert.equal(publication.deployment.status, 'succeeded');
  assert.equal(publication.package.project_id, 'appgprj_6ad077cf90708191a3e75367b4b3b78c');
  assert(samePath(publication.package.checkout_path, stage));
  assert.match(publication.package.commit_sha, /^[a-f0-9]{40}$/);
  assert.equal(publication.saved.source.commit_sha, publication.package.commit_sha);
  assert.equal(publication.deployment.version_id, publication.saved.id);
  assert(publication.saved.version_number > baselinePublication.versionNumber);
  const live = await proof('live-validation.json');
  assert.equal(live.gameVersion, '1.0.24'); assert.equal(live.gameBuildId, manifest.buildId);
  assert.equal(live.gameManifestSha256, prepared.release.manifestSha256);
  assert.equal(live.publication.versionId, publication.saved.id);
  assert.equal(live.publication.commitSha, publication.package.commit_sha);
  report.publication = { versionId: publication.saved.id, commitSha: publication.package.commit_sha,
    deploymentId: publication.deployment.id };
  // Existing attributes are protected in every mode and never rewritten.
  report.installed = await swap(canonical, prepared.baseline.canonicalGameTreeSha256, expectedManifest, prepared);
  report.gitAttributesSha256 = protectedHashes['.gitattributes'];
  report.steps = { succeededPublicationAndLivePassVerified: true, oldCanonicalBaselineVerified: true,
    canonicalMatchesPublishedPackage: true, protectedSourceHashesPreserved: true };
}

let proofFile;
try {
  assert(process.argv.length === 3 && ['prepare', 'finalize', 'sync'].includes(mode),
    'Usage: PrepareRelease.mjs prepare|finalize|sync');
  proofFile = await checked(path.join(proofRoot, `release-${mode}.json`), { absent: true });
  await checked(release, { absent: true }); await checked(stage); await checked(canonical);
  await ({ prepare, finalize, sync })[mode]();
  report.result = 'PASS';
} catch (error) {
  report.result = 'FAIL'; report.error = error.message; process.exitCode = 1;
}
report.completedAtUtc = new Date().toISOString();
if (proofFile) await writeFile(proofFile, JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({ result: report.result, mode, proof: proofFile,
  buildId: report.release?.buildId ?? report.installed?.buildId, error: report.error }));
