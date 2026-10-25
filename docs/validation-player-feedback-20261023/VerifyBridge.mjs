// Read-only bridge deployment verification. Run only after the bridge is live.
// GET public pages and the bounded static wasm; HEAD the large R2 data only.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { createReadStream } from 'node:fs';
import { lstat, readFile, readdir, realpath, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const directory = path.dirname(fileURLToPath(import.meta.url));
const workspace = path.resolve(directory, '../..');
const stage = path.join(workspace, 'Temp/SitePlayerFeedback20261023');
const canonical = path.join(workspace, 'WebSite');
const release = path.join(workspace, 'Temp/PlayerFeedbackRelease20261023');
const origin = 'https://bser-sts.com';
const manifestPath = root => path.join(root, 'lib/unity-assets-manifest.json');
const expectedOldManifest = '1e0633db77b024d6f4d85f46c92d4269ce80723e92bf21057d4361c9994b73d4';
const expectedAttributes = 'fcd4902b9b599e41443f819411a5c47fb77218e5efd2b7be19b62841675ccc5a';
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const samePath = (a, b) => path.resolve(a).toLowerCase() === path.resolve(b).toLowerCase();
const report = { result: 'RUNNING', startedAtUtc: new Date().toISOString(), origin,
  authenticatedRequests: false, productionWrites: false, largeGameDataDownloads: false,
  checks: {}, responses: [], errors: [] };

async function checked(filename, { absent = false } = {}) {
  const absolute = path.resolve(filename), relative = path.relative(workspace, absolute);
  assert(relative && relative !== '..' && !relative.startsWith(`..${path.sep}`) && !path.isAbsolute(relative));
  let cursor = workspace;
  for (const segment of ['', ...relative.split(path.sep)]) {
    if (segment) cursor = path.join(cursor, segment);
    let info;
    try { info = await lstat(cursor); }
    catch (error) { if (error.code === 'ENOENT' && absent) continue; throw error; }
    assert(!info.isSymbolicLink(), `Linked local path refused: ${cursor}`);
    assert(samePath(await realpath(cursor), cursor), `Resolved local path differs: ${cursor}`);
  }
  return absolute;
}

async function localBytes(filename) { return readFile(await checked(filename)); }
async function json(filename) { return JSON.parse((await localBytes(filename)).toString('utf8').replace(/^\uFEFF/, '')); }
async function fileHash(filename) {
  await checked(filename);
  const digest = createHash('sha256');
  for await (const bytes of createReadStream(filename)) digest.update(bytes);
  return digest.digest('hex');
}

async function tree(root) {
  await checked(root); const files = [];
  async function walk(current, relative = '') {
    for (const entry of (await readdir(current, { withFileTypes: true })).sort((a, b) => a.name.localeCompare(b.name))) {
      const filename = path.join(current, entry.name); await checked(filename);
      if (entry.isDirectory()) await walk(filename, path.join(relative, entry.name));
      else {
        assert(entry.isFile());
        files.push({ path: path.join(relative, entry.name).split(path.sep).join('/'),
          size: (await lstat(filename)).size, sha256: await fileHash(filename) });
      }
    }
  }
  await walk(root);
  return { sha256: hash(Buffer.from(JSON.stringify(files))), files };
}

async function check(name, action) {
  try { await action(); report.checks[name] = true; }
  catch (error) { report.checks[name] = false; report.errors.push({ name, error: error.message }); }
}

async function request(route, method = 'GET', limit = 2 * 1024 * 1024, extraHeaders = {}) {
  const url = new URL(route, origin); assert.equal(url.origin, origin);
  assert(['GET', 'HEAD'].includes(method));
  url.searchParams.set('bts_verify', `player-feedback-bridge-${Date.now()}`);
  const response = await fetch(url, { method, credentials: 'omit', redirect: 'error',
    signal: AbortSignal.timeout(30000), headers: { 'Accept-Encoding': 'identity', 'Cache-Control': 'no-cache', ...extraHeaders } });
  const entry = { route, method, status: response.status, headers: Object.fromEntries(
    ['Content-Type', 'Content-Length', 'Content-Encoding', 'Content-Range', 'Cache-Control', 'ETag']
      .map(name => [name, response.headers.get(name)])) };
  report.responses.push(entry);
  const chunks = []; let length = 0;
  if (response.body) {
    const reader = response.body.getReader();
    try {
      for (;;) {
        const { done, value } = await reader.read(); if (done) break;
        length += value.length;
        if (length > limit) { await reader.cancel(); throw new Error(`Response exceeds small-page limit: ${route}`); }
        chunks.push(Buffer.from(value));
      }
    } finally { reader.releaseLock(); }
  }
  const bytes = Buffer.concat(chunks); entry.bytes = length;
  if (method !== 'HEAD') entry.sha256 = hash(bytes);
  return { entry, bytes, text: bytes.toString('utf8') };
}

async function main() {
  const publication = await json(path.join(directory, 'bridge-state.json'));
  assert.equal(publication.package.project_id, 'appgprj_6ad077cf90708191a3e75367b4b3b78c');
  assert(samePath(publication.package.checkout_path, stage));
  assert.match(publication.package.commit_sha, /^[a-f0-9]{40}$/);
  assert.equal(publication.saved.source.commit_sha, publication.package.commit_sha);
  assert.equal(publication.deployment.version_id, publication.saved.id);
  assert.equal(publication.deployment.status, 'succeeded');
  assert(publication.saved.version_number > 49);
  report.publication = { commitSha: publication.package.commit_sha, versionId: publication.saved.id,
    versionNumber: publication.saved.version_number, deploymentId: publication.deployment.id,
    status: publication.deployment.status };
  report.checks.bridgeDeploymentSucceeded = true;

  const prepared = await json(path.join(directory, 'release-prepare.json'));
  assert.equal(prepared.result, 'PASS'); assert.equal(prepared.workspace, workspace);
  assert.equal(prepared.version, '1.0.25');
  const oldBytes = await localBytes(manifestPath(canonical));
  assert.equal(hash(oldBytes), expectedOldManifest);
  const old = JSON.parse(oldBytes.toString('utf8'));
  assert.equal(old.ready, true); assert.equal(old.basePath, '/game');
  const unionBytes = await localBytes(manifestPath(stage));
  assert.equal(hash(unionBytes), prepared.baseline.stageUnionManifestSha256);
  const union = JSON.parse(unionBytes.toString('utf8'));
  const newBytes = await localBytes(manifestPath(release));
  assert.equal(hash(newBytes), prepared.release.manifestSha256);
  const next = JSON.parse(newBytes.toString('utf8'));
  assert.equal(next.ready, true);
  report.oldBuildId = old.buildId; report.nextBuildId = next.buildId;
  report.oldManifestSha256 = hash(oldBytes); report.bridgeManifestSha256 = hash(unionBytes);
  await check('existingAttributesPreserved', async () => {
    assert.equal(await fileHash(path.join(canonical, '.gitattributes')), expectedAttributes);
    assert.equal(await fileHash(path.join(stage, '.gitattributes')), expectedAttributes);
    report.gitAttributesSha256 = expectedAttributes;
  });

  await check('bridgeOnlyAddsNewR2Assets', async () => {
    const oldMetadata = { ...old }, unionMetadata = { ...union };
    delete oldMetadata.files; delete unionMetadata.files;
    assert.deepEqual(unionMetadata, oldMetadata, 'Old build metadata changed');
    for (const [url, record] of Object.entries(old.files)) assert.deepEqual(union.files[url], record, url);
    const added = [];
    for (const [url, record] of Object.entries(union.files)) {
      if (Object.hasOwn(old.files, url)) continue;
      assert.equal(record.storage, 'r2', `Added asset must be R2: ${url}`);
      assert.deepEqual(record, next.files[url], `Added asset differs from new package: ${url}`);
      assert.equal(record.objectKey, `unity-assets/${record.sha256}`);
      added.push({ path: url, size: record.size, sha256: record.sha256 });
    }
    const expected = Object.entries(next.files).filter(([url, record]) => record.storage === 'r2' && !Object.hasOwn(old.files, url))
      .map(([url, record]) => ({ path: url, size: record.size, sha256: record.sha256 }));
    assert.deepEqual(added.sort((a, b) => a.path.localeCompare(b.path)), expected.sort((a, b) => a.path.localeCompare(b.path)));
    assert.deepEqual(added, [...prepared.release.r2Assets].sort((a, b) => a.path.localeCompare(b.path)));
    report.addedR2Assets = added;
  });

  await check('oldStaticAssetsByteExact', async () => {
    const canonicalTree = await tree(path.join(canonical, 'public/game'));
    const stagedTree = await tree(path.join(stage, 'public/game'));
    assert.equal(canonicalTree.sha256, prepared.baseline.canonicalGameTreeSha256);
    assert.equal(stagedTree.sha256, canonicalTree.sha256);
    assert.equal(stagedTree.sha256, prepared.baseline.stageGameTreeSha256);
    let count = 0;
    for (const [url, record] of Object.entries(old.files)) {
      if (record.storage === 'r2') continue;
      assert(Array.isArray(record.parts) && record.parts.length > 0);
      const digest = createHash('sha256'); let size = 0;
      for (const part of record.parts) {
        assert(part.path.startsWith('/game/'));
        const segments = part.path.slice('/game/'.length).split('/').map(decodeURIComponent);
        assert(segments.every(s => s && s !== '.' && s !== '..' && !/[\\/\0]/.test(s)));
        const source = await checked(path.join(stage, 'public/game', ...segments));
        assert.equal((await lstat(source)).size, part.size, part.path);
        for await (const bytes of createReadStream(source)) { digest.update(bytes); size += bytes.length; }
      }
      assert.equal(size, record.size, url); assert.equal(digest.digest('hex'), record.sha256, url); count++;
    }
    report.staticAssetsVerified = count; report.staticGameTreeSha256 = stagedTree.sha256;
  });

  // No public request is made until local safety/bridge preparation is valid.
  if (report.errors.length) return;
  await check('homeStillLoads', async () => {
    const response = await request('/'); assert.equal(response.entry.status, 200);
    assert.match(response.text, /<title>블더스<\/title>/);
  });
  await check('oldGameManifestStillServed', async () => {
    const response = await request('/game/build-manifest.json', 'GET', 65536);
    assert.equal(response.entry.status, 200);
    assert.equal(hash(response.bytes), old.files['/game/build-manifest.json'].sha256);
    assert.equal(response.bytes.length, old.files['/game/build-manifest.json'].size);
    assert.equal(JSON.parse(response.text).productVersion, '1.0.24');
    report.gameVersion = '1.0.24';
  });
  for (const [name, url] of [['oldDataHeadOnly', old.buildFiles.dataUrl], ['oldWasmHeadOnly', old.buildFiles.codeUrl]]) {
    await check(name, async () => {
      assert(url && old.files[url]);
      const response = await request(url, 'HEAD', 0);
      assert.equal(response.entry.status, 200);
      if (response.entry.headers['Content-Length'] !== null) {
        assert.equal(response.entry.headers['Content-Length'], String(old.files[url].size));
      } else {
        assert.equal(name, 'oldWasmHeadOnly');
        const sample = await request(url, 'GET', 16 * 1024 * 1024);
        assert.equal(sample.entry.status, 200);
        assert.equal(sample.bytes.length, old.files[url].size);
        assert.equal(hash(sample.bytes), old.files[url].sha256);
        report.staticWasmFullHashVerified = true;
      }
      assert.equal(response.entry.headers['Content-Encoding'], null);
    });
  }
}

try { await main(); }
catch (error) { report.errors.push({ name: 'setup', error: error.message }); }
report.result = report.errors.length ? 'FAIL' : 'PASS';
report.completedAtUtc = new Date().toISOString();
await writeFile(await checked(path.join(directory, 'bridge-validation.json'), { absent: true }),
  JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({ result: report.result, checks: Object.keys(report.checks).length,
  errors: report.errors, oldBuildId: report.oldBuildId, nextBuildId: report.nextBuildId, publication: report.publication }));
if (report.result !== 'PASS') process.exitCode = 1;
