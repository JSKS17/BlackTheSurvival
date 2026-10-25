// Offline syntax/provenance checks only. Never execute release or cleanup modes.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import vm from 'node:vm';

const directory = path.dirname(fileURLToPath(import.meta.url));
const workspace = path.resolve(directory, '../..');
const hashes = {
  'WebSite/lib/unity-assets-manifest.json': '1e0633db77b024d6f4d85f46c92d4269ce80723e92bf21057d4361c9994b73d4',
  'WebSite/lib/game-accounts.mjs': '8e267ad2fd69c8fb892ef6e17efd0f49cea96a17986d428012ac85dbad373e33',
  'WebSite/app/page.tsx': '16a10237fce86c4c6239a03c73f4812d077bb567e58b024d252ac3a4441e0b77',
  'WebSite/.gitattributes': 'fcd4902b9b599e41443f819411a5c47fb77218e5efd2b7be19b62841675ccc5a',
  'WebSite/app/game.css': '22704ccabea784d856a9e068cb7e3d34d983a36804363a80c0e33d512f7aa1da',
  'WebSite/package-lock.json': 'e224d3aa65e17c80c62fbfb9c38b871a4d675608ea7ce1373a979ff27990fd96',
  'WebSite/scripts/upload-unity.mjs': '2d75155c56ac8fde907ebcefda16af24e1aad2d18bf2b81182b14aabac740d11',
  'WebSite/scripts/package-unity.mjs': '31417f232e8262c05597a95a9124af42cf38fee06879420ae293d1861b27754a',
};
const report = { result: 'RUNNING', checkedAtUtc: new Date().toISOString(),
  scope: 'Release helper preparation and offline syntax/provenance only',
  productSourceChanged: false, releaseCommandsExecuted: false, productionRequests: false, serverStarted: false,
  gameVersion: '1.0.25', baselineGameVersion: '1.0.24', baselineSiteVersion: 49,
  baselineSourceCommit: '1c0f270115c544860886030bf1028a0aec060e5d', baselineEnvironmentRevision: 51,
  baselineGameBuildId: '8fa247b1755a774777d8e213',
  build: 'Build/Web125', stage: 'Temp/SitePlayerFeedback20261023', release: 'Temp/PlayerFeedbackRelease20261023',
  previewPort: 8246, syntaxChecks: [], baselineHashes: {}, embeddedBrowserScripts: [] };

for (const [relative, expected] of Object.entries(hashes)) {
  const actual = createHash('sha256').update(await readFile(path.join(workspace, relative))).digest('hex');
  assert.equal(actual, expected, relative); report.baselineHashes[relative] = actual;
}
for (const name of ['CreateHelpers.mjs', 'PrepareRelease.mjs', 'VerifyBridge.mjs', 'VerifyLive.mjs', 'Preview.mjs', 'ValidateHelpers.mjs', 'DeriveFixtures.mjs']) {
  const result = spawnSync(process.execPath, ['--check', path.join(directory, name)], { encoding: 'utf8', windowsHide: true });
  assert.equal(result.status, 0, `${name}: ${result.stderr}`);
  report.syntaxChecks.push({ file: name, validation: 'node --check', result: 'PASS' });
}
const psPath = path.join(directory, 'Cleanup.ps1').replaceAll("'", "''");
const psResult = spawnSync('powershell.exe', ['-NoProfile', '-NonInteractive', '-Command',
  `$tokens=$null; $errors=$null; [Management.Automation.Language.Parser]::ParseFile('${psPath}',[ref]$tokens,[ref]$errors) | Out-Null; if ($errors.Count) { $errors | ConvertTo-Json; exit 1 }`],
  { encoding: 'utf8', windowsHide: true });
assert.equal(psResult.status, 0, psResult.stdout + psResult.stderr);
report.syntaxChecks.push({ file: 'Cleanup.ps1', validation: 'PowerShell Parser.ParseFile', result: 'PASS' });

const preview = await readFile(path.join(directory, 'Preview.mjs'), 'utf8');
const qaStart = preview.indexOf('async function qaHtml() {');
const qaEnd = preview.indexOf('const server =', qaStart);
assert(qaStart > 0 && qaEnd > qaStart);
for (const baseline of [false, true]) {
  const gameVersion = baseline ? '1.0.24' : '1.0.25';
  const html = await vm.runInNewContext(preview.slice(qaStart, qaEnd) + '\nqaHtml();', {
    baseline, gameVersion, fixtureNames: async () => ['crafting-materials', 'feedback-crafting', 'feedback-combat', 'gear-scope', 'research', 'subject-combo'],
  });
  assert(html.includes('value="crafting-materials"'));
  assert(html.includes('value="subject-combo"'));
  assert(html.includes('value="feedback-crafting">제작 재료·장비 효과 범위'));
  assert(html.includes('value="feedback-combat">유키 W·E / 아야 E 연계'));
  assert(html.includes("else if(names.has('feedback-crafting'))select.value='feedback-crafting'"));
  assert(html.includes(JSON.stringify(gameVersion)));
  const script = html.match(/<script[^>]*>([\s\S]*?)<\/script>/)?.[1];
  assert(script); new vm.Script(script);
  report.embeddedBrowserScripts.push({ script: 'generic fixture QA', gameVersion, result: 'PASS' });
}
const mobileLiteral = preview.match(/const model = (`[^`]*`);/)?.[1];
assert(mobileLiteral, 'Missing mobile detection model');
const mobileHtml = vm.runInNewContext(mobileLiteral);
const mobileScript = mobileHtml.match(/<script[^>]*>([\s\S]*?)<\/script>/)?.[1];
assert(mobileScript);
const mobileContext = { navigator: { userAgent: 'Mozilla/5.0 QA Chrome/145.0.7780.12 Safari/537.36' }, window: {} };
new vm.Script(mobileScript).runInNewContext(mobileContext);
assert.equal(mobileContext.navigator.userAgentData.mobile, true);
assert.equal(mobileContext.navigator.maxTouchPoints, 5);
assert.match(mobileContext.navigator.userAgent, /Android 14/);
report.embeddedBrowserScripts.push({ script: 'local mobile detection model', result: 'PASS', runtimeAssertions: 3 });
report.result = 'PASS';
await writeFile(path.join(directory, 'helper-prep-validation.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({ result: report.result, syntaxChecks: report.syntaxChecks.length,
  embeddedBrowserScripts: report.embeddedBrowserScripts.length, baselineHashes: Object.keys(report.baselineHashes).length,
  productSourceChanged: false, releaseCommandsExecuted: false }));
