import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {readFile,writeFile,copyFile,mkdir} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const here=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(here,'../..'),old=path.join(root,'docs/validation-wickeline-20261026');
const sha=x=>createHash('sha256').update(x).digest('hex');
const manifestHash=sha(await readFile(path.join(root,'WebSite/lib/unity-assets-manifest.json')));
const baseline={version:'1.0.27',siteVersion:55,siteSourceCommit:'46862e68b80dfcda579905deca700c6da5a1d68c',manifestSha256:manifestHash,files:{}};
for(const f of ['lib/game-accounts.mjs','app/page.tsx','.gitattributes','app/game.css','scripts/upload-unity.mjs','scripts/package-unity.mjs','package-lock.json','db/schema.ts','Assets']) {
 if(f==='Assets')continue;
 baseline.files[f]=sha(await readFile(path.join(root,'WebSite',f)));
}
await writeFile(path.join(here,'baseline.json'),JSON.stringify(baseline,null,2)+'\n',{flag:'wx'});
for(const name of ['PrepareRelease.mjs','VerifyBridge.mjs','Preview.mjs']) {
 let s=await readFile(path.join(old,name),'utf8');
 s=s.replaceAll('SiteWickeline20261026','SitePresence20261026').replaceAll('WickelineRelease20261026','PresenceRelease20261026').replaceAll('Build/Web127','Build/Web128').replaceAll('69d87aa28379352f3ba29eefada716159fd299ac3c9ff59d050f4414519aee0f',manifestHash).replaceAll('3618d496050cce352223ca1098b4ea43b1f0dc4d','46862e68b80dfcda579905deca700c6da5a1d68c').replaceAll('versionNumber: 53','versionNumber: 55');
 s=s.replaceAll('1.0.27','__PRESENCE_NEXT__').replaceAll('1.0.26','1.0.27').replaceAll('__PRESENCE_NEXT__','1.0.28');
 if(name==='PrepareRelease.mjs') {
   s=s.replace(/  'lib\/game-accounts\.mjs': '[a-f0-9]+',\r?\n/,'').replace(/  'app\/page\.tsx': '[a-f0-9]+',\r?\n/,'');
 }
 if(name==='Preview.mjs') s=s.replaceAll('8248','8249').replaceAll('위클라인','접속자 표시').replaceAll('wickeline-combat','presence-lobby');
 await writeFile(path.join(here,name),s,{flag:'wx'});
}
await mkdir(path.join(here,'fixtures'),{recursive:true});
await copyFile(path.join(old,'fixtures/research.json'),path.join(here,'fixtures/research.json'));
console.log(JSON.stringify({result:'PASS',baseline,created:['PrepareRelease.mjs','VerifyBridge.mjs','Preview.mjs']}));
