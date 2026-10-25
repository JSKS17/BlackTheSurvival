import fs from 'node:fs/promises';
import path from 'node:path';
import { createHash } from 'node:crypto';
import { spawn } from 'node:child_process';

const root = path.resolve(import.meta.dirname, '../..');
const stage = path.join(root, 'Temp/SitePresence20261026');
const specs = [
  ['game-session', 'scripts/game-session.test.mjs'],
  ['cloud-sync-client', 'scripts/cloud-sync-client.test.mjs'],
  ['display-preferences-client', 'scripts/display-preferences-client.test.mjs'],
  ['game-shell', 'scripts/game-shell.test.mjs'],
  ['game-loader', 'scripts/game-loader.test.mjs'],
  ['browser-account-bridge', path.join(root, 'Tools/BrowserAccountBridgeTests.js')]
];
const sourceFiles = [
  'app/page.tsx', 'lib/cloud-sync-client.ts', 'lib/display-preferences-client.ts', 'lib/game-session-state.ts',
  'components/game-shell.tsx', 'public/game/index.html'
];
const sourceHashes = {};
for (const relative of sourceFiles) sourceHashes[`stage/${relative}`] = createHash('sha256').update(await fs.readFile(path.join(stage, relative))).digest('hex');
for (const [, relative] of specs) {
  if (path.isAbsolute(relative)) continue;
  sourceHashes[`stage/${relative}`] = createHash('sha256').update(await fs.readFile(path.join(stage, relative))).digest('hex');
}
for (const relative of ['Assets/WebGLTemplates/BlackTheSurvival/index.html', 'Assets/WebGLTemplates/BlackTheSurvival/TemplateData/account-bridge.js', 'Assets/WebGLTemplates/BlackTheSurvival/TemplateData/presence.js', 'Assets/Plugins/WebGL/BrowserAccount.jslib', 'Tools/BrowserAccountBridgeTests.js']) {
  sourceHashes[relative] = createHash('sha256').update(await fs.readFile(path.join(root, relative))).digest('hex');
}
const startedAt = new Date().toISOString();
const frozenBytes = await fs.readFile(path.join(import.meta.dirname, 'source-validation.json'));
const frozen = JSON.parse(frozenBytes);
const frozenInputs = Object.fromEntries(Object.entries(sourceHashes).filter(([file]) =>
  file.startsWith('stage/') ? frozen.web[file.slice(6)] : frozen.product[file]));
const frozenInputsMatch = Object.entries(frozenInputs).every(([file, actual]) => actual ===
  (file.startsWith('stage/') ? frozen.web[file.slice(6)] : frozen.product[file]));
const runs = await Promise.all(specs.map(async ([name, file]) => {
  const start = new Date().toISOString();
  const result = await new Promise((resolve, reject) => {
    const child = spawn(process.execPath, ['--test', file], { cwd: stage, windowsHide: true });
    let output = '';
    child.stdout.on('data', value => { output += value; });
    child.stderr.on('data', value => { output += value; });
    child.on('error', reject);
    child.on('exit', (exitCode, signal) => resolve({ exitCode, signal, output }));
  });
  await fs.writeFile(path.join(import.meta.dirname, `host-${name}.log`), result.output);
  const number = key => { const value = result.output.match(new RegExp(`^# ${key} (\\d+)$`, 'm')); return value ? Number(value[1]) : null; };
  return { name, command: ['node', '--test', file], startedAt: start, completedAt: new Date().toISOString(), exitCode: result.exitCode, signal: result.signal,
    tests: number('tests'), passed: number('pass'), failed: number('fail'), cancelled: number('cancelled'), skipped: number('skipped'), log: `host-${name}.log` };
}));
for (const run of runs) console.log(`${run.name}: exit=${run.exitCode}, pass=${run.passed}, fail=${run.failed}`);
const status = frozenInputsMatch && runs.every(run => run.exitCode === 0 && run.failed === 0 && run.cancelled === 0) ? 'PASS' : 'FAIL';
await fs.writeFile(path.join(import.meta.dirname, 'host-regressions.json'), JSON.stringify({ status, startedAt, completedAt: new Date().toISOString(), runs,
  passed: runs.reduce((sum, run) => sum + (run.passed || 0), 0), sourceHashes,
  frozenInputsMatch, frozenInputs, sourceValidationSha256: createHash('sha256').update(frozenBytes).digest('hex'),
  harnessUpdate: 'Dependency-aware multiple React effects and cleanup; presence control messages are recorded separately from account save commands. Original failure evidence is preserved in host-regressions.before-harness-update.json and host-game-session.before-harness-update.log.',
  scope: 'Existing account lifecycle, cloud save, display preference, fullscreen and loading regressions; local Node only.' }, null, 2) + '\n');
if (status !== 'PASS') process.exitCode = 1;
