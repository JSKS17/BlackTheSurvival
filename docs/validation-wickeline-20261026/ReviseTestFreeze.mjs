// Record a test-only revision while the byte-identical runtime/art is building.
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {readFile,writeFile,copyFile,constants} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const here=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(here,'../..');
const file=path.join(here,'source-validation.json'),oldBytes=await readFile(file),frozen=JSON.parse(oldBytes);
const sha=b=>createHash('sha256').update(b).digest('hex');
for(const [p,h] of Object.entries({...frozen.files,...frozen.artSourceHashes}))assert.equal(sha(await readFile(path.join(root,p))),h,'Runtime/art/build input changed: '+p);
const changed=[];
for(const [p,h] of Object.entries(frozen.testSourceHashes)){
  const actual=sha(await readFile(path.join(root,p)));
  if(actual!==h){assert.equal(p,'Tools/CoreSmokeTests.cs');changed.push({path:p,oldSha256:h,newSha256:actual});frozen.testSourceHashes[p]=actual;}
}
assert.equal(changed.length,1);
await copyFile(file,path.join(here,'source-validation.at-build-start.json'),constants.COPYFILE_EXCL);
frozen.testFreezeRevisedAtUtc=new Date().toISOString();
const next=Buffer.from(JSON.stringify(frozen,null,2)+'\n');
await writeFile(file,next);
await writeFile(path.join(here,'test-freeze-revision.json'),JSON.stringify({result:'PASS',changedAtUtc:frozen.testFreezeRevisedAtUtc,reason:'Existing automatic route helper now accepts the requested immediate Wickeline-to-camp victory flow.',runtimeAndArtByteIdentical:true,oldFreezeSha256:sha(oldBytes),newFreezeSha256:sha(next),changed},null,2)+'\n');
console.log(JSON.stringify({result:'PASS',runtimeAndArtByteIdentical:true,changed}));
