// Prepare local helper sources only. Never build, publish, upload, or start a server.
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {readFile,writeFile,mkdir,copyFile} from 'node:fs/promises';
import {constants} from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const here=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(here,'../..'),old=path.join(root,'docs/validation-presence-20261026');
const sha=b=>createHash('sha256').update(b).digest('hex');
const manifestBytes=await readFile(path.join(root,'WebSite/lib/unity-assets-manifest.json'));
const manifest=JSON.parse(manifestBytes),manifestSha256=sha(manifestBytes);
assert.equal(JSON.parse(await readFile(path.join(root,'WebSite/public/game/build-manifest.json'),'utf8')).productVersion,'1.0.28');
const baseline={version:'1.0.28',siteVersion:57,environmentVersion:59,siteSourceCommit:'73b44728095b8aaae3edf8ada1ebe3409ef62ab2',manifestSha256,buildId:manifest.buildId,files:{}};
for(const file of ['lib/game-accounts.mjs','app/page.tsx','.gitattributes','app/game.css','package-lock.json','scripts/package-unity.mjs','scripts/upload-unity.mjs'])baseline.files[file]=sha(await readFile(path.join(root,'WebSite',file)));
await writeFile(path.join(here,'baseline.json'),JSON.stringify(baseline,null,2)+'\n',{flag:'wx'});
const common=s=>s.replaceAll('validation-presence-20261026','validation-rune-roles-1.1.0').replaceAll('SitePresence20261026','SiteRuneRoles110').replaceAll('PresenceRelease20261026','RuneRolesRelease110').replaceAll('Build/Web128','Build/Web110').replaceAll("'Web128'","'Web110'").replaceAll('raw Web128','raw Web110').replaceAll('1.0.28','__RUNE_NEXT__').replaceAll('1.0.27','1.0.28').replaceAll('__RUNE_NEXT__','1.1.0').replaceAll('46862e68b80dfcda579905deca700c6da5a1d68c',baseline.siteSourceCommit).replaceAll('3f089e686f037fb8210499e8e37e868ee28c0f3483fab56335f430cfcbaf699e',manifestSha256).replaceAll('versionNumber: 55','versionNumber: 57').replaceAll('version_number > 55','version_number > 57').replaceAll('version_number>55','version_number>57').replaceAll('version_number -le 55','version_number -le 57').replaceAll('Site version 55','Site version 57');
const created=[];
for(const name of ['PrepareRelease.mjs','VerifyBridge.mjs','VerifyLive.mjs','ObserveUnityBuild.ps1','Cleanup.ps1','Preview.mjs','LocalPresenceBackend.mjs']){
 let source=common(await readFile(path.join(old,name),'utf8'));
 if(name==='PrepareRelease.mjs')source=source.replace(/const protectedHashes = \{[\s\S]+?\n\};/,`const protectedHashes = ${JSON.stringify(baseline.files,null,2)};`);
 if(name==='VerifyLive.mjs')source=source.replace('/1\\.0\\.28/','/1\\.1\\.0/');
 if(name==='Preview.mjs')source=source.replaceAll('8249','8250').replaceAll("'00a3d6117a640e6b4da0d5d1'",JSON.stringify(manifest.buildId)).replaceAll('접속자 표시','룬 역할군').replaceAll('접속자 집계','접속자 집계').replaceAll("baseline ? '1.0.28' : '1.1.0'","baseline ? '1.0.28' : '1.1.0'");
 if(name==='LocalPresenceBackend.mjs')source=source.replaceAll('8249','8250');
 if(name==='Cleanup.ps1'){
  source=source.replace("foreach ($presenceProofName in @('backend-tests.json','presence-client-tests.json','host-regressions.json','unity-build-observer-result.json'))", "foreach ($presenceProofName in @('gameplay-tests.json','unity-build-observer-result.json'))");
 }
 await writeFile(path.join(here,name),source,{flag:'wx'});created.push(name);
}
await mkdir(path.join(here,'fixtures'),{recursive:true});
try{await copyFile(path.join(old,'fixtures/research.json'),path.join(here,'fixtures/research.json'),constants.COPYFILE_EXCL);}catch(e){if(e.code!=='EEXIST')throw e;}
await writeFile(path.join(here,'helper-provenance.json'),JSON.stringify({result:'PREPARED',preparedAtUtc:new Date().toISOString(),baseline,created,productionRequests:false,productChanges:false},null,2)+'\n',{flag:'wx'});
console.log(JSON.stringify({result:'PREPARED',created,manifestSha256,protectedWebFiles:Object.keys(baseline.files).length}));
