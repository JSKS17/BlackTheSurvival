import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {readFile,writeFile,copyFile,constants} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const here=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(here,'../..');
const filename=path.join(here,'source-validation.json'),bytes=await readFile(filename),frozen=JSON.parse(bytes);
const sha=b=>createHash('sha256').update(b).digest('hex');
const allowed=new Set(['Assets/Scripts/Lumia/GameEngineWickeline.cs','Tools/WickelineSmokeTests.cs']),changed=[];
for(const map of [frozen.files,frozen.testSourceHashes,frozen.artSourceHashes])for(const [p,h]of Object.entries(map)){
  const actual=sha(await readFile(path.join(root,p)));
  if(actual!==h){assert(allowed.has(p),'Unexpected source change: '+p);map[p]=actual;changed.push({path:p,oldSha256:h,newSha256:actual});}
}
assert.deepEqual(changed.map(x=>x.path).sort(),[...allowed].sort());
await copyFile(filename,path.join(here,'source-validation.before-victory-heal.json'),constants.COPYFILE_EXCL);
frozen.initialFrozenAtUtc=frozen.frozenAtUtc;frozen.frozenAtUtc=new Date().toISOString();
frozen.finalCorrection='Preserve existing first-victory healing after the Wickeline combat has ended, with duplicate guards.';
const output=Buffer.from(JSON.stringify(frozen,null,2)+'\n');await writeFile(filename,output);
await writeFile(path.join(here,'victory-heal-freeze-revision.json'),JSON.stringify({result:'PASS',changedAtUtc:frozen.frozenAtUtc,oldFreezeSha256:sha(bytes),newFreezeSha256:sha(output),changed,initialBuildMustBeRebuilt:true},null,2)+'\n');
console.log(JSON.stringify({result:'PASS',changed}));
