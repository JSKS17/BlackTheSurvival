// Generate local release/QA helpers only; never build, publish, or clean up.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { lstat, mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const workspace = path.resolve(here, '../..');
const previous = path.join(workspace, 'docs/validation-combat-card-scroll-20261022');
const sha = bytes => createHash('sha256').update(bytes).digest('hex');
assert(!(await lstat(here)).isSymbolicLink(), 'Helper destination must be ordinary');
const canonicalManifest = await readFile(path.join(workspace, 'WebSite/lib/unity-assets-manifest.json'));
assert.equal(sha(canonicalManifest), '1e0633db77b024d6f4d85f46c92d4269ce80723e92bf21057d4361c9994b73d4');
const replacements = [
  ['validation-combat-card-scroll-20261022', 'validation-player-feedback-20261023'],
  ['SiteCombatCardScroll20261022', 'SitePlayerFeedback20261023'],
  ['CombatCardScrollRelease20261022', 'PlayerFeedbackRelease20261023'],
  ['CombatCardScrollRelease', 'PlayerFeedbackRelease'],
  ['Web124', 'Web125'],
  ['1.0.24', '1.0.25'],
  ['1.0.23', '1.0.24'],
  ['3555cdf0a36169907fffca5dbd7b8f6befdf796fffe49d95eca4ef3abbbe1c13', '1e0633db77b024d6f4d85f46c92d4269ce80723e92bf21057d4361c9994b73d4'],
  ['16d05d61ab7099b7692216e97e8de82798d47fa2', '1c0f270115c544860886030bf1028a0aec060e5d'],
  ['versionNumber: 47', 'versionNumber: 49'],
  ['version_number > 47', 'version_number > 49'],
  ['version_number -le 47', 'version_number -le 49'],
  ['baseline Site version 47.', 'baseline Site version 49.'],
  ['combat-card-scroll', 'player-feedback'],
];
async function writeNew(name, bytes) {
  await writeFile(path.join(here, name), bytes, { flag: 'wx' });
}
for (const name of ['PrepareRelease.mjs', 'VerifyBridge.mjs', 'VerifyLive.mjs', 'Cleanup.ps1']) {
  let source = await readFile(path.join(previous, name), 'utf8');
  for (const [before, after] of replacements) source = source.replaceAll(before, after);
  if (name === 'Cleanup.ps1') {
    const start = source.indexOf("    $traceOutput = Checked-Path");
    const end = source.indexOf("    $unexpected =", start);
    assert(start > 0 && end > start);
    source = source.slice(0, start) + `    $rawOutput = Checked-Path (Join-Path $build 'Web125')
    if (Test-Path -LiteralPath $rawOutput) {
        $manifest = Read-Proof 'unity-build-manifest.json'
        if ($manifest.result -ne 'Succeeded' -or $manifest.productVersion -ne '1.0.25' -or $manifest.errors -ne 0) { throw 'Raw build ownership proof must match this release.' }
        $actual = Checked-Path (Join-Path $rawOutput 'build-manifest.json')
        $saved = Checked-Path (Join-Path $proofRoot 'unity-build-manifest.json')
        if ((Get-FileHash -LiteralPath $actual -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $saved -Algorithm SHA256).Hash) { throw 'Raw output no longer matches the saved release build proof.' }
    }
    # Extend only after inspecting a named task test output and its proof.
    $allowed = @('UnityWebBuild','Web125','LumiaWebBuild.log')
` + source.slice(end);
  }
  await writeNew(name, source);
}

// Keep the proven local file/range server and optional immutable baseline proxy.
// The old touch/art-specific UI is replaced by a generic fixture selector.
const oldPreview = await readFile(path.join(previous, 'Preview.mjs'), 'utf8');
let preview = oldPreview.slice(0, oldPreview.indexOf('// Explicit, visible browser-TouchEvent QA controls.'));
preview = preview.replace("readFile, stat, realpath", "readFile, readdir, stat, realpath");
preview = preview.replace("// Disposable loopback-only art / WebGL QA preview. No production requests or auth.", "// Disposable loopback-only WebGL QA preview. --baseline reads immutable public assets only.");
preview = preview.replace("baseline ? 'WebSite/public/game' : process.argv.includes('--trace') ? 'Build/WebTouchTrace' : 'Build/Web124'", "baseline ? 'WebSite/public/game' : 'Build/Web125'");
preview = preview.replace(/const fixtureRoot = .*;/, "const fixtureRoot = path.join(here, 'fixtures');");
preview = preview.replace(/if \(baseline && .*\n/, "if (baseline && (!baselineManifest?.ready || baselineManifest.buildId !== '8fa247b1755a774777d8e213' || createHash('sha256').update(await readFile(path.join(workspace, 'WebSite/lib/unity-assets-manifest.json'))).digest('hex') !== '1e0633db77b024d6f4d85f46c92d4269ce80723e92bf21057d4361c9994b73d4')) throw new Error('Baseline must remain the exact deployed 1.0.24 build');\n");
preview = preview.replace('port = 8245', 'port = 8246');
const artStart = preview.indexOf('const fixtureNames =');
const artEnd = preview.indexOf('const contentTypes =', artStart);
preview = preview.slice(0, artStart) + `const gameVersion = baseline ? '1.0.24' : '1.0.25';
async function fixtureNames() {
  return (await readdir(fixtureRoot, { withFileTypes: true }))
    .filter(entry => entry.isFile() && /^[a-z][a-z0-9-]{0,63}\\.json$/.test(entry.name))
    .map(entry => entry.name.slice(0, -5)).sort();
}
` + preview.slice(artEnd);
preview = preview.replace(' + qaTouchModelScript;', ';');
await writeNew('Preview.mjs', preview + await readFile(path.join(here, 'PreviewUi.txt'), 'utf8'));
await mkdir(path.join(here, 'fixtures'), { recursive: true });
console.log(JSON.stringify({ result: 'PASS', generated: ['PrepareRelease.mjs', 'VerifyBridge.mjs', 'VerifyLive.mjs', 'Cleanup.ps1', 'Preview.mjs'], productSourceChanged: false, releaseCommandsExecuted: false }));
