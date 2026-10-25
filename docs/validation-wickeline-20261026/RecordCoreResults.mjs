import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {readFile,writeFile} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const here=path.dirname(fileURLToPath(import.meta.url)),workspace=path.resolve(here,'../..');
const sha=bytes=>createHash('sha256').update(bytes).digest('hex');
const frozen=JSON.parse(await readFile(path.join(here,'source-validation.json'),'utf8'));
const manifestBytes=await readFile(path.join(here,'source-manifest.json'));
const manifest=JSON.parse(manifestBytes.toString('utf8').replace(/^\uFEFF/,''));
const inputs={...frozen.files,...frozen.testSourceHashes};
const expectedTestPaths=[...new Set([
  ...Object.keys(frozen.files).filter(file=>/^Assets\/Scripts\/Lumia\/[^/]+\.cs$/.test(file)),
  ...Object.keys(frozen.testSourceHashes),
])].sort();
assert.deepEqual(manifest.files.map(entry=>entry.path).sort(),expectedTestPaths,'Exact tested source set');
assert.equal(new Set(manifest.files.map(entry=>entry.path)).size,manifest.files.length);
for(const entry of manifest.files){
  assert.equal(entry.sha256.toLowerCase(),inputs[entry.path],entry.path+' tested/frozen mismatch');
  const current=await readFile(path.join(workspace,entry.path));assert.equal(sha(current),inputs[entry.path]);assert.equal(current.length,entry.bytes);
}
assert.equal(manifest.files.length,expectedTestPaths.length);
const logBytes=await readFile(path.join(here,'core-tests.log')),log=logBytes.toString('utf8');
const count=Number(log.match(/^PASS: (\d+) assertions/m)?.[1]);assert(count>0);
assert.match(log,/^EXIT_CODE=0\r?$/m);assert(!/^FAIL:/m.test(log));
const result={result:'PASS',version:'1.0.27',assertions:count,exitCode:0,startedAtUtc:log.match(/START_UTC=([^\r\n]+)/)?.[1],endedAtUtc:log.match(/END_UTC=([^\r\n]+)/)?.[1],
  sourceHashes:frozen.files,testSourceHashes:frozen.testSourceHashes,verifiedTestedSourceFiles:manifest.files.length,
  testedSourceManifestSha256:sha(manifestBytes),logSha256:sha(logBytes),
  scope:frozen.scope};
assert(result.startedAtUtc&&result.endedAtUtc);
await writeFile(path.join(here,'core-tests.json'),JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify({result:'PASS',assertions:count,verifiedTestedSourceFiles:manifest.files.length}));
