import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import { createHash } from 'node:crypto';
import { spawn } from 'node:child_process';

const root = path.resolve(import.meta.dirname, '../..');
const focused = process.argv.includes('--focused');
const mode = focused ? 'rune-offer-tests' : 'gameplay-tests';
const sha = bytes => createHash('sha256').update(bytes).digest('hex');
const sourceHashes = {};
for (const folder of ['Assets/Scripts/Lumia', 'Tools']) {
  for (const name of (await fs.readdir(path.join(root, folder))).sort()) {
    if (!/\.cs$/.test(name) && !(folder === 'Tools' && name === 'RunCoreTests.ps1')) continue;
    const file = folder + '/' + name;
    sourceHashes[file] = sha(await fs.readFile(path.join(root, file)));
  }
}
const startedAtUtc = new Date().toISOString();
const args = ['-NoProfile', '-File', path.join(root, 'Tools/RunCoreTests.ps1'), ...(focused ? ['-RuneOffersOnly'] : [])];
const result = await new Promise((resolve, reject) => {
  const child = spawn('pwsh.exe', args, { cwd: root, windowsHide: true });
  const chunks = [];
  child.stdout.on('data', value => { chunks.push(Buffer.from(value)); });
  child.stderr.on('data', value => { chunks.push(Buffer.from(value)); });
  child.on('error', reject);
  child.on('exit', (exitCode, signal) => {
    let bytes = Buffer.concat(chunks);
    while (bytes.subarray(0, 3).equals(Buffer.from([0xef, 0xbb, 0xbf]))) bytes = bytes.subarray(3);
    let output, outputEncoding = 'utf-8';
    try { output = new TextDecoder('utf-8', { fatal: true }).decode(bytes); }
    catch { output = new TextDecoder('euc-kr').decode(bytes); outputEncoding = 'cp949/euc-kr'; }
    resolve({ exitCode, signal, output, outputEncoding });
  });
});
await fs.writeFile(path.join(import.meta.dirname, mode + '.log'), result.output);
for (const [file, expected] of Object.entries(sourceHashes)) assert.equal(sha(await fs.readFile(path.join(root, file))), expected, 'Tested input changed while tests ran: ' + file);
let sourceFreezeSha256;
let testSourceAmendmentSha256;
try {
  const frozenBytes = await fs.readFile(path.join(import.meta.dirname, 'source-validation.json'));
  const frozen = JSON.parse(frozenBytes);
  let amendments = {};
  try {
    const amendedBytes = await fs.readFile(path.join(import.meta.dirname, 'test-source-amendment.json'));
    const amended = JSON.parse(amendedBytes);
    assert.equal(amended.result, 'PASS'); assert.equal(amended.sourceFreezeSha256, sha(frozenBytes));
    amendments = Object.fromEntries(amended.changes.map(change => { assert.equal(frozen.tools[change.path], change.frozenSha256); return [change.path, change.amendedSha256]; }));
    testSourceAmendmentSha256 = sha(amendedBytes);
  } catch (error) { if (error.code !== 'ENOENT') throw error; }
  for (const [file, actual] of Object.entries(sourceHashes)) assert.equal(amendments[file] ?? (file.startsWith('Tools/') ? frozen.tools : frozen.product)[file], actual, 'Tested source differs from freeze or approved test amendment: ' + file);
  sourceFreezeSha256 = sha(frozenBytes);
} catch (error) { if (error.code !== 'ENOENT') throw error; }
const assertions = Number(result.output.match(/PASS:\s*(\d+)\s+assertions?/)?.[1] || 0);
const proof = { result: result.exitCode === 0 && assertions > 0 ? 'PASS' : 'FAIL', version: '1.1.0', startedAtUtc,
  completedAtUtc: new Date().toISOString(), exitCode: result.exitCode, signal: result.signal, assertions, tests: assertions, sourceFreezeSha256, testSourceAmendmentSha256, outputEncoding: result.outputEncoding, sourceHashes,
  testSourceHashes: Object.fromEntries(Object.entries(sourceHashes).filter(([file]) => file.startsWith('Tools/'))),
  log: mode + '.log', command: ['pwsh.exe', ...args], physicalHardwareTested: false };
await fs.writeFile(path.join(import.meta.dirname, mode + '.json'), JSON.stringify(proof, null, 2) + '\n');
console.log(result.output.trim());
console.log(JSON.stringify({ result: proof.result, assertions, exitCode: result.exitCode, sourceFreezeSha256 }));
if (proof.result !== 'PASS') process.exitCode = 1;
