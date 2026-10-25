// Read-only production checks: no cookies, account creation or large game downloads.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { readFile, readdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const directory = path.dirname(fileURLToPath(import.meta.url));
const workspace = path.resolve(directory, '../..');
const stage = path.join(workspace, 'Temp/SiteLumiGrid20261022');
const origin = 'https://bser-sts.com';
const sha = bytes => createHash('sha256').update(bytes).digest('hex');
const json = async file => JSON.parse((await readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
const report = { result: 'RUNNING', startedAtUtc: new Date().toISOString(), origin,
  authenticatedRequests: false, productionWrites: false, largeGameDataDownloads: false,
  checks: {}, responses: [], sourceHashes: {}, errors: [] };

async function check(name, action) {
  try { await action(); report.checks[name] = true; }
  catch (error) { report.checks[name] = false; report.errors.push({ name, error: error.message }); }
}

async function request(route, method = 'GET', limit = 2 * 1024 * 1024) {
  const url = new URL(route, origin);
  assert.equal(url.origin, origin); assert(['GET', 'HEAD'].includes(method));
  url.searchParams.set('bts_verify', `lumi-1.0.22-${Date.now()}`);
  const response = await fetch(url, { method, credentials: 'omit', redirect: 'error',
    signal: AbortSignal.timeout(30000), headers: { 'Accept-Encoding': 'identity', 'Cache-Control': 'no-cache' } });
  const entry = { route, method, status: response.status, headers: Object.fromEntries(
    ['Content-Type', 'Content-Length', 'Content-Encoding', 'Cache-Control', 'ETag', 'Vary']
      .map(name => [name, response.headers.get(name)])) };
  report.responses.push(entry);
  const chunks = []; let length = 0;
  if (response.body) {
    const reader = response.body.getReader();
    try {
      for (;;) {
        const next = await reader.read(); if (next.done) break;
        length += next.value.length;
        if (length > limit) { await reader.cancel(); throw new Error(`Response too large: ${route}`); }
        chunks.push(Buffer.from(next.value));
      }
    } finally { reader.releaseLock(); }
  }
  const bytes = Buffer.concat(chunks);
  entry.bytes = length; if (method !== 'HEAD') entry.sha256 = sha(bytes);
  return { entry, bytes, text: bytes.toString('utf8') };
}

async function main() {
  const publication = await json(path.join(directory, 'publish-state.json'));
  assert.equal(publication.package.project_id, 'appgprj_6ad077cf90708191a3e75367b4b3b78c');
  assert.equal(path.resolve(publication.package.checkout_path), stage);
  assert.equal(publication.saved.source.commit_sha, publication.package.commit_sha);
  assert.equal(publication.deployment.version_id, publication.saved.id);
  assert.equal(publication.deployment.status, 'succeeded');
  assert(publication.saved.version_number > 43);
  report.publication = { commitSha: publication.package.commit_sha,
    versionNumber: publication.saved.version_number, versionId: publication.saved.id,
    deploymentId: publication.deployment.id, status: publication.deployment.status };
  report.checks.deploymentSucceeded = true;
  await check('preservedAccountConcurrencyFix', async () => {
    const expected = {
      'lib/game-accounts.mjs': '8e267ad2fd69c8fb892ef6e17efd0f49cea96a17986d428012ac85dbad373e33',
      'app/page.tsx': '16a10237fce86c4c6239a03c73f4812d077bb567e58b024d252ac3a4441e0b77',
      '.gitattributes': 'fcd4902b9b599e41443f819411a5c47fb77218e5efd2b7be19b62841675ccc5a'
    };
    for (const [relative, hash] of Object.entries(expected)) {
      const actual = sha(await readFile(path.join(stage, relative)));
      assert.equal(actual, hash, relative); report.sourceHashes[relative] = actual;
    }
  });
  const manifestBytes = await readFile(path.join(stage, 'lib/unity-assets-manifest.json'));
  const manifest = JSON.parse(manifestBytes.toString());
  await check('unityManifestReady', async () => {
    assert.equal(manifest.ready, true); assert.match(manifest.buildId, /^[a-f0-9]{24}$/);
    assert(manifest.files && manifest.buildFiles);
    report.gameBuildId = manifest.buildId; report.gameManifestSha256 = sha(manifestBytes);
    const data = manifest.files[manifest.buildFiles.dataUrl];
    assert.equal(data.storage, 'r2'); assert(data.size > 100000000);
    assert.match(data.sha256, /^[a-f0-9]{64}$/);
  });
  const clientRoot = path.join(stage, 'dist/client');
  const entryManifest = await json(path.join(clientRoot, 'vinext-client-entry-manifest.json'));
  await check('homeLoadsCurrentBootstrap', async () => {
    const response = await request('/'); assert.equal(response.entry.status, 200);
    assert.match(response.text, /<title>블더스<\/title>/);
    assert(response.text.includes('/' + entryManifest.appBrowserEntry));
  });
  for (const subdir of ['_next/static/chunks', '_next/static/css']) {
    const files = (await readdir(path.join(clientRoot, subdir))).filter(file => /\.(?:js|css)$/.test(file));
    assert(files.length > 0);
    for (const file of files) await check(`client:${subdir}/${file}`, async () => {
      const local = await readFile(path.join(clientRoot, subdir, file));
      assert(local.length <= 2 * 1024 * 1024);
      const response = await request(`/${subdir}/${file}`);
      assert.equal(response.entry.status, 200); assert.equal(sha(response.bytes), sha(local));
    });
  }
  await check('anonymousSession', async () => {
    const response = await request('/api/session', 'GET', 16384);
    assert.equal(response.entry.status, 200);
    assert.deepEqual(JSON.parse(response.text), { authenticated: false });
    assert.match(response.entry.headers['Cache-Control'], /no-store/);
  });
  for (const route of ['/api/save', '/api/preferences']) await check(`protected:${route}`, async () => {
    const response = await request(route, 'GET', 16384);
    assert.equal(response.entry.status, 401); assert.equal(JSON.parse(response.text).code, 'unauthenticated');
    assert.match(response.entry.headers['Cache-Control'], /private/);
    assert.match(response.entry.headers['Cache-Control'], /no-store/);
  });
  await check('gamePageMatchesLumiRelease', async () => {
    const local = await readFile(path.join(stage, 'public/game/index.html'));
    assert.equal(sha(local), manifest.files['/game/index.html'].sha256);
    const response = await request('/game/'); assert.equal(response.entry.status, 200);
    const application = response.text.replace(/<script\b[^>]*>[\s\S]*?<\/script\s*>/gi,
      script => /\/cdn-cgi\/challenge-platform\/|__CF\$cv\$params/.test(script) ? '' : script);
    assert.equal(application, local.toString('utf8'));
  });
  await check('gameManifestMatchesLumiRelease', async () => {
    const local = await readFile(path.join(stage, 'public/game/build-manifest.json'));
    assert.equal(sha(local), manifest.files['/game/build-manifest.json'].sha256);
    const response = await request('/game/build-manifest.json'); assert.equal(response.entry.status, 200);
    assert.equal(sha(response.bytes), sha(local));
    report.gameVersion = JSON.parse(response.text).productVersion;
    assert.equal(report.gameVersion, '1.0.22');
  });
  await check('uploadEndpointClosed', async () => {
    const response = await request('/api/unity-upload', 'GET', 16384);
    assert.equal(response.entry.status, 404);
  });
  await check('gameDataHeadOnly', async () => {
    const data = manifest.files[manifest.buildFiles.dataUrl];
    const response = await request(manifest.buildFiles.dataUrl, 'HEAD', 0);
    assert.equal(response.entry.status, 200);
    assert.equal(response.entry.headers['Content-Length'], String(data.size));
    assert.equal(response.entry.headers.ETag, `"${data.sha256}"`);
    assert.equal(response.entry.headers['Content-Encoding'], null);
  });
  await check('staticWasmBytesMatchRelease', async () => {
    const wasm = manifest.files[manifest.buildFiles.codeUrl];
    assert(wasm.size <= 16 * 1024 * 1024);
    const response = await request(manifest.buildFiles.codeUrl, 'GET', 16 * 1024 * 1024);
    assert.equal(response.entry.status, 200);
    assert.equal(response.bytes.length, wasm.size);
    assert.equal(sha(response.bytes), wasm.sha256);
    assert.equal(response.entry.headers['Content-Encoding'], null);
  });
}

try { await main(); }
catch (error) { report.errors.push({ name: 'setup', error: error.message }); }
report.result = report.errors.length ? 'FAIL' : 'PASS'; report.completedAtUtc = new Date().toISOString();
await writeFile(path.join(directory, 'live-validation.json'), JSON.stringify(report, null, 2));
console.log(JSON.stringify({ result: report.result, checks: Object.keys(report.checks).length,
  errors: report.errors, gameBuildId: report.gameBuildId, publication: report.publication }));
if (report.result !== 'PASS') process.exitCode = 1;
