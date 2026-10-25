// Preserve prior evidence and approve only the observer-free intent regression test.
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {readFile,writeFile,copyFile,constants} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const here=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(here,'../..');
const sha=bytes=>createHash('sha256').update(bytes).digest('hex');
const file=path.join(here,'source-validation.json'),oldBytes=await readFile(file),frozen=JSON.parse(oldBytes);
const protectedInputs={...frozen.files,...frozen.artSourceHashes};
for(const [p,h] of Object.entries(protectedInputs))
  assert.equal(sha(await readFile(path.join(root,p))),h,'Runtime/art/build input changed: '+p);
const changed=[];
for(const [p,h] of Object.entries(frozen.testSourceHashes)){
  const actual=sha(await readFile(path.join(root,p)));
  if(actual!==h){
    assert.equal(p,'Tools/WickelineSmokeTests.cs','Only the approved test file may differ');
    assert.equal(actual,'e46fd2fd60323130c345f655ea267a6a5f806266bcfc92bc8f61942743e1a56a');
    changed.push({path:p,oldSha256:h,newSha256:actual});frozen.testSourceHashes[p]=actual;
  }
}
assert.equal(changed.length,1);
const buildProcess=JSON.parse((await readFile(path.join(here,'build-process-web.json'),'utf8')).replace(/^\uFEFF/,''));
assert.equal(buildProcess.sourceCaptureSha256,sha(oldBytes),'Existing Web build must bind to preserved prior freeze');
for(const name of ['source-validation.json','core-tests.log','core-tests.json','source-manifest.json','build-source-validation.json']){
  const extension=path.extname(name),base=name.slice(0,-extension.length);
  await copyFile(path.join(here,name),path.join(here,base+'.before-intent-regression'+extension),constants.COPYFILE_EXCL);
}
frozen.testFreezeRevisedAtUtc=new Date().toISOString();
const next=Buffer.from(JSON.stringify(frozen,null,2)+'\n');
await writeFile(file,next);
const report={result:'PASS',changedAtUtc:frozen.testFreezeRevisedAtUtc,
  reason:'Add observer-free assertions that committed NPC intent is correct immediately after turn transitions and save/load.',
  runtimeAndArtByteIdentical:true,verifiedProtectedInputs:Object.keys(protectedInputs).length,
  oldFreezeSha256:sha(oldBytes),newFreezeSha256:sha(next),changed,
  build:{rebuildRequired:false,existingWebBuildFreezeSha256:buildProcess.sourceCaptureSha256,
    preservedFreeze:'source-validation.before-intent-regression.json',
    unityBuildManifestSha256:sha(await readFile(path.join(here,'unity-build-manifest.json'))),
    explanation:'Only test bytes changed. The existing Web build runtime, art, settings and build sources retain their exact hashes; the original build-process freeze binding is preserved.'}};
await writeFile(path.join(here,'intent-regression-test-freeze-revision.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report));
