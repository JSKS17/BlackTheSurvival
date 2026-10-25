// Offline helper syntax/provenance only. Never execute release, server or cleanup.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import vm from 'node:vm';

const directory = path.dirname(fileURLToPath(import.meta.url));
const workspace = path.resolve(directory, '../..');
const sha = bytes => createHash('sha256').update(bytes).digest('hex');
const baseline = JSON.parse(await readFile(path.join(directory, 'baseline-provenance.json'), 'utf8'));
assert.equal(baseline.result, 'PASS'); assert.equal(baseline.gameVersion, '1.0.25'); assert.equal(baseline.siteVersion, 51);
const report = { result: 'RUNNING', checkedAtUtc: new Date().toISOString(),
  scope: 'Release helper preparation, offline syntax and local baseline provenance only',
  productSourceChanged: false, oldProofsChanged: false, releaseCommandsExecuted: false,
  productionRequests: false, serverStarted: false, buildExecuted: false, cleanupExecuted: false,
  gameVersion: '1.0.26', baselineGameVersion: baseline.gameVersion, baselineSiteVersion: baseline.siteVersion,
  baselineSourceCommit: baseline.sourceCommit, baselineEnvironmentRevision: baseline.environmentRevision,
  baselineGameBuildId: baseline.gameBuildId, baselineManifestSha256: baseline.manifestSha256,
  build: 'Build/Web126', stage: 'Temp/SiteSkillReferences20261025', release: 'Temp/SkillReferencesRelease20261025',
  previewPort: 8247, syntaxChecks: [], baselineHashes: {}, copiedFixtures: baseline.copiedFixtures, embeddedBrowserScripts: [] };

for (const [relative, expected] of Object.entries(baseline.hashes)) {
  const actual = sha(await readFile(path.join(workspace, relative)));
  assert.equal(actual, expected, relative); report.baselineHashes[relative] = actual;
}
assert.equal(sha(await readFile(path.join(workspace, baseline.baselineSourceProof))), baseline.baselineSourceProofSha256);
for (const [name, expected] of Object.entries(baseline.helperInputs))
  assert.equal(sha(await readFile(path.join(workspace, 'docs/validation-player-feedback-20261023', name))), expected, 'Old helper changed: ' + name);
for (const fixture of baseline.copiedFixtures) {
  assert.equal(sha(await readFile(path.join(workspace, fixture.source))), fixture.sha256);
  assert.equal(sha(await readFile(path.join(directory, fixture.target))), fixture.sha256);
}
for (const name of ['CreateHelpers.mjs', 'SourceSchema.mjs', 'PrepareRelease.mjs', 'VerifyBridge.mjs', 'VerifyLive.mjs',
  'Preview.mjs', 'ValidateHelpers.mjs', 'ValidateBuildSnapshot.mjs', 'Complete.mjs']) {
  const result = spawnSync(process.execPath, ['--check', path.join(directory, name)], { encoding: 'utf8', windowsHide: true });
  assert.equal(result.status, 0, `${name}: ${result.stderr}`);
  report.syntaxChecks.push({ file: name, validation: 'node --check', result: 'PASS' });
}
const pwsh = 'C:/Users/azxar/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe';
const psPath = path.join(directory, 'Cleanup.ps1').replaceAll("'", "''");
const cleanup = await readFile(path.join(directory, 'Cleanup.ps1'), 'utf8');
assert(cleanup.startsWith('#requires -Version 7.0\n'));
const psResult = spawnSync(pwsh, ['-NoProfile', '-NonInteractive', '-Command',
  `if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'pwsh 7 required' }; $tokens=$null; $errors=$null; [Management.Automation.Language.Parser]::ParseFile('${psPath}',[ref]$tokens,[ref]$errors) | Out-Null; if ($errors.Count) { $errors | ConvertTo-Json; exit 1 }`],
  { encoding: 'utf8', windowsHide: true });
assert.equal(psResult.status, 0, psResult.stdout + psResult.stderr);
report.syntaxChecks.push({ file: 'Cleanup.ps1', validation: 'pwsh 7 Parser.ParseFile only', result: 'PASS' });

const preview = await readFile(path.join(directory, 'Preview.mjs'), 'utf8');
const qaStart = preview.indexOf('async function qaHtml() {'), qaEnd = preview.indexOf('const server =', qaStart);
assert(qaStart > 0 && qaEnd > qaStart);
for (const baselineMode of [false, true]) {
  const gameVersion = baselineMode ? '1.0.25' : '1.0.26';
  const html = await vm.runInNewContext(preview.slice(qaStart, qaEnd) + '\nqaHtml();', {
    baseline: baselineMode, gameVersion, fixtureNames: async () => ['research', 'subject-reference-sample', 'yuki-combat'],
  });
  assert(html.includes('value="yuki-combat">유키 W·E 기준 장면'));
  assert(html.includes('value="subject-reference-sample"'));
  assert(html.includes("else if(names.has('yuki-combat'))select.value='yuki-combat'"));
  assert(html.includes(JSON.stringify(gameVersion)));
  const script = html.match(/<script[^>]*>([\s\S]*?)<\/script>/)?.[1];
  assert(script); new vm.Script(script);
  report.embeddedBrowserScripts.push({ script: 'dynamic fixture QA', gameVersion, result: 'PASS' });
}
const mobileLiteral = preview.match(/const model = (`[^`]*`);/)?.[1]; assert(mobileLiteral);
const mobileHtml = vm.runInNewContext(mobileLiteral);
const mobileScript = mobileHtml.match(/<script[^>]*>([\s\S]*?)<\/script>/)?.[1]; assert(mobileScript);
const context = { navigator: { userAgent: 'Mozilla/5.0 QA Chrome/145.0.7780.12 Safari/537.36' }, window: {} };
new vm.Script(mobileScript).runInNewContext(context);
assert.equal(context.navigator.userAgentData.mobile, true); assert.equal(context.navigator.maxTouchPoints, 5);
assert.match(context.navigator.userAgent, /Android 14/);
report.embeddedBrowserScripts.push({ script: 'local mobile detection model', result: 'PASS', runtimeAssertions: 3 });
report.result = 'PASS';
await writeFile(path.join(directory, 'helper-prep-validation.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({ result: report.result, syntaxChecks: report.syntaxChecks.length,
  embeddedBrowserScripts: report.embeddedBrowserScripts.length, baselineHashes: Object.keys(report.baselineHashes).length,
  productSourceChanged: false, oldProofsChanged: false, releaseCommandsExecuted: false }));
