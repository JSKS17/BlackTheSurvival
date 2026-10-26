// Prepare the wording-only 1.1.0 release helpers. No build, server, or network work.
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {readFile,writeFile,mkdir,copyFile} from 'node:fs/promises';
import {constants} from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const here=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(here,'../..'),old=path.join(root,'docs/validation-rune-roles-1.1.0');
const sha=b=>createHash('sha256').update(b).digest('hex');
const manifestBytes=await readFile(path.join(root,'WebSite/lib/unity-assets-manifest.json')),manifest=JSON.parse(manifestBytes);
assert.equal(sha(manifestBytes),'ed5d614e627e50bc492133aed6eb717b228c22c1d5f8a90afe3d41cac05e5929');assert.equal(manifest.buildId,'9cd4b9c05da11eff0f692005');
assert.equal(JSON.parse(await readFile(path.join(root,'WebSite/public/game/build-manifest.json'),'utf8')).productVersion,'1.1.0');
const previousBytes=await readFile(path.join(old,'gameplay-tests.json')),previous=JSON.parse(previousBytes);assert.equal(previous.result,'PASS');
const baseline={version:'1.1.0',siteVersion:59,environmentVersion:61,siteSourceCommit:'73dec621568bf3ab9ddbf7aae2aac501c588595f',manifestSha256:sha(manifestBytes),buildId:manifest.buildId,files:{},priorFullRegression:{path:'docs/validation-rune-roles-1.1.0/gameplay-tests.json',sha256:sha(previousBytes),assertions:previous.assertions??previous.tests,result:previous.result}};
for(const file of ['lib/game-accounts.mjs','app/page.tsx','.gitattributes','app/game.css','package-lock.json','scripts/package-unity.mjs','scripts/upload-unity.mjs'])baseline.files[file]=sha(await readFile(path.join(root,'WebSite',file)));
await writeFile(path.join(here,'baseline.json'),JSON.stringify(baseline,null,2)+'\n',{flag:'wx'});
const adapt=s=>s.replaceAll('validation-rune-roles-1.1.0','validation-rune-labels-1.1.0').replaceAll('SiteRuneRoles110','SiteRuneLabels110').replaceAll('RuneRolesRelease110','RuneLabelsRelease110').replaceAll('Web110','Web110Labels').replaceAll('8250','8251').replaceAll('1.0.28','1.1.0').replaceAll('73b44728095b8aaae3edf8ada1ebe3409ef62ab2',baseline.siteSourceCommit).replaceAll('51e5ec5120bc56fd0ff2f0f2719a0410718049c699580f657a3742cd7fee7c4e',baseline.manifestSha256).replaceAll('e5ed02dac650693ec7fab3b6',baseline.buildId).replaceAll('versionNumber: 57','versionNumber: 59').replaceAll('version_number > 57','version_number > 59').replaceAll('version_number>57','version_number>59').replaceAll('version_number -le 57','version_number -le 59').replaceAll('Site version 57','Site version 59');
const names=['Freeze.mjs','PrepareRelease.mjs','VerifyBridge.mjs','VerifyLive.mjs','SyncWebSources.mjs','Cleanup.ps1','Complete.mjs','Preview.mjs','LocalPresenceBackend.mjs','StartBuild.ps1','ObserveUnityBuild.ps1','ValidateHelpers.mjs','ValidatePowerShell.ps1'];
for(const name of names){
 let source=adapt(await readFile(path.join(old,name),'utf8'));
 if(name==='Complete.mjs'){
  const from=source.indexOf('let amendment=null,amendmentRecord=null;'),to=source.indexOf('assert.equal((await readdir');assert(from>0&&to>from);
  source=source.slice(0,from)+`assert(!tests.testSourceAmendmentSha256,'This fresh label release permits no test source amendment');
for(const map of[frozen.product,frozen.unchangedArt,frozen.tools,frozen.sourceDocumentation])for(const[file,expected]of Object.entries(map??{}))assert.equal(sha(await readFile(path.join(root,file))),expected,file);
for(const[file,executedHash]of Object.entries(tests.testSourceHashes??{})){assert(Object.hasOwn(frozen.tools,file),'Unfrozen executed test source: '+file);assert.equal(executedHash,frozen.tools[file],file);}
`+source.slice(to);
  source=source.replace('testSourceAmendment:amendmentRecord,','');
 }
 if(name==='Cleanup.ps1'){
  const from=source.indexOf('$testAmendment = $null'),to=source.indexOf("foreach ($presenceProofName in @('gameplay-tests.json'");assert(from>0&&to>from);
  source=source.slice(0,from)+`if ($executedTests.testSourceAmendmentSha256) { throw 'This fresh label release permits no test source amendment.' }
foreach ($presenceName in @('product','unchangedArt','tools','sourceDocumentation')) {
    foreach ($presenceField in $presenceFrozen.$presenceName.PSObject.Properties) {
        $presenceSource = Checked-Path (Join-Path $taskRoot $presenceField.Name)
        if ((Get-FileHash -LiteralPath $presenceSource -Algorithm SHA256).Hash.ToLowerInvariant() -cne $presenceField.Value) { throw ('Frozen source changed before cleanup: ' + $presenceField.Name) }
    }
}
foreach ($executedTest in $executedTests.testSourceHashes.PSObject.Properties) {
    $executedExpected = $presenceFrozen.tools.PSObject.Properties[$executedTest.Name].Value
    if (-not $executedExpected -or $executedTest.Value -cne $executedExpected) { throw ('Executed test source does not match its fresh freeze: ' + $executedTest.Name) }
}
`+source.slice(to);
  source=source.replace(/\s+testSourceAmendment=\$testAmendmentRecord\r?\n/,'\n');
 }
 if(name==='Freeze.mjs')source=source.replace("scope:'Rune role groups, subject role catalogue and related game UI only; hosting/account/presence services and art remain unchanged'","scope:'Rune family label wording only; gameplay, official role data, hosting/account/presence services and art remain unchanged'");
 if(name==='Preview.mjs')source=source.replaceAll('룬 역할군','룬 계열명 문구');
 await writeFile(path.join(here,name),source,{flag:'wx'});
}
await mkdir(path.join(here,'fixtures'),{recursive:true});
try{await copyFile(path.join(old,'fixtures/research.json'),path.join(here,'fixtures/research.json'),constants.COPYFILE_EXCL);}catch(error){if(error.code!=='EEXIST')throw error;}
await writeFile(path.join(here,'helper-provenance.json'),JSON.stringify({result:'PREPARED',preparedAtUtc:new Date().toISOString(),baseline,created:names,productionRequests:false,productChanges:false,priorProofsModified:false},null,2)+'\n',{flag:'wx'});
console.log(JSON.stringify({result:'PREPARED',created:names.length,manifestSha256:baseline.manifestSha256,priorFullAssertions:baseline.priorFullRegression.assertions}));
