import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {readFile,writeFile,lstat,readdir} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const here=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(here,'../..'),sha=b=>createHash('sha256').update(b).digest('hex');
const json=async name=>JSON.parse((await readFile(path.join(here,name),'utf8')).replace(/^\uFEFF/,''));
const frozen=await json('source-validation.json');assert.equal(frozen.result,'PASS');assert.equal(frozen.version,'1.1.0');
const freezeSha256=sha(await readFile(path.join(here,'source-validation.json')));
const tests=await json('gameplay-tests.json');assert.equal(tests.result??tests.status,'PASS');assert.equal(tests.sourceFreezeSha256,freezeSha256);
let amendment=null,amendmentRecord=null;
const amendedTests=new Map(),allowedAmendedTests=new Set(['Tools/CoreSmokeTests.cs','Tools/SharedPassiveResourceSmokeTests.cs']);
try{amendment=await json('test-source-amendment.json');}catch(error){if(error.code!=='ENOENT')throw error;}
if(amendment){
 assert.equal(amendment.result,'PASS');assert.equal(amendment.sourceFreezeSha256,freezeSha256);assert.equal(amendment.productSourceChanged,false);assert.equal(amendment.approvedByRoot,true);
 assert(Array.isArray(amendment.changes)&&amendment.changes.length>=1&&amendment.changes.length<=allowedAmendedTests.size,'Only the approved one- or two-test source subset may be amended');
 const amendmentSha256=sha(await readFile(path.join(here,'test-source-amendment.json')));assert.equal(tests.testSourceAmendmentSha256,amendmentSha256);
 for(const change of amendment.changes){
  assert(allowedAmendedTests.has(change.path)&&!amendedTests.has(change.path),'Unapproved or duplicate amended path: '+change.path);assert.equal(change.frozenSha256,frozen.tools[change.path]);assert.match(change.amendedSha256,/^[a-f0-9]{64}$/);assert.notEqual(change.amendedSha256,change.frozenSha256);
  assert.equal(sha(await readFile(path.join(root,change.path))),change.amendedSha256);assert.equal(tests.testSourceHashes?.[change.path],change.amendedSha256);assert.equal(tests.sourceHashes?.[change.path],change.amendedSha256);
  const original=change.preservedOriginalTest??(change.path==='Tools/CoreSmokeTests.cs'?amendment.preservedOriginalTest:null);assert(typeof original==='string'&&!path.isAbsolute(original)&&!original.includes('..'));assert.equal(sha(await readFile(path.join(here,original))),change.frozenSha256);
  amendedTests.set(change.path,{path:change.path,frozenSha256:change.frozenSha256,amendedSha256:change.amendedSha256,preservedOriginalTest:original});
 }
 amendmentRecord={amendmentSha256,changes:[...amendedTests.values()]};
}else assert(!tests.testSourceAmendmentSha256,'Test proof refers to a missing amendment');
for(const[name,map]of Object.entries({product:frozen.product,unchangedArt:frozen.unchangedArt,tools:frozen.tools,sourceDocumentation:frozen.sourceDocumentation}))for(const[file,expected]of Object.entries(map??{})){
 const permitted=name==='tools'&&amendedTests.has(file)?amendedTests.get(file).amendedSha256:expected;
 assert.equal(sha(await readFile(path.join(root,file))),permitted,file);
}
for(const[file,executedHash]of Object.entries(tests.testSourceHashes??{})){assert(Object.hasOwn(frozen.tools,file),'Unfrozen executed test source: '+file);assert.equal(executedHash,amendedTests.has(file)?amendedTests.get(file).amendedSha256:frozen.tools[file],file);}
assert.equal((await readdir(path.join(root,'Assets/Scripts/Lumia'))).filter(file=>file.endsWith('.cs')).length,frozen.runtimeSources);
const web=await json('web-source-sync.json');assert.equal(web.result,'PASS');assert.deepEqual(web.files,frozen.web);for(const[file,expected]of Object.entries(web.files))assert.equal(sha(await readFile(path.join(root,'WebSite',file))),expected,file);
const build=await json('unity-build-manifest.json');assert.equal(build.result,'Succeeded');assert.equal(build.productVersion,'1.1.0');assert.equal(build.errors,0);
const observer=await json('unity-build-observer-result.json');assert.equal(observer.result??observer.status,'PASS');assert.equal(observer.sourceFreezeSha256,sha(await readFile(path.join(here,'source-validation.json'))));assert.equal(observer.manifestSha256,sha(await readFile(path.join(here,'unity-build-manifest.json'))));
const browser=await json('browser-validation.json');assert.equal(browser.result??browser.status,'PASS');assert.equal(browser.gameVersion,'1.1.0');assert(browser.screenshots?.length);for(const file of browser.screenshots){assert(!path.isAbsolute(file)&&!file.includes('..'));assert((await lstat(path.join(here,file))).isFile());}
const live=await json('live-validation.json'),pub=await json('publish-state.json'),sync=await json('release-sync.json'),cleanup=await json('cleanup.json');for(const value of[live,sync,cleanup])assert.equal(value.result,'PASS');assert.equal(live.gameVersion,'1.1.0');assert.equal(pub.deployment.status,'succeeded');assert.equal(pub.saved.id,pub.deployment.version_id);assert.equal(pub.saved.source.commit_sha,pub.package.commit_sha);assert.equal(live.publication.versionId,pub.saved.id);assert.equal(live.publication.commitSha,pub.package.commit_sha);
assert.equal(sha(await readFile(path.join(root,'WebSite/lib/unity-assets-manifest.json'))),live.gameManifestSha256);
for(const relative of['Build','Temp/SiteRuneRoles110','Temp/RuneRolesRelease110','docs/validation-rune-roles-1.1.0/site.tar.gz','docs/validation-rune-roles-1.1.0/bridge.tar.gz']){try{await lstat(path.join(root,relative));assert.fail('Disposable target remains: '+relative);}catch(error){if(error.code!=='ENOENT')throw error;}}
const result={result:'PASS',completedAtUtc:new Date().toISOString(),version:'1.1.0',site:'https://bser-sts.com/',siteVersion:pub.saved.version_number,deploymentId:pub.deployment.id,siteSourceCommit:pub.package.commit_sha,sourceFreezeSha256:freezeSha256,runtimeSources:frozen.runtimeSources,gameplayTests:tests.tests??tests.assertions??tests.totalAssertions??null,testSourceAmendment:amendmentRecord,screenshots:browser.screenshots,physicalMobileTested:browser.physicalMobileTested??false,liveChecks:Object.keys(live.checks).length,removedBytes:cleanup.removedBytes};
await writeFile(path.join(here,'completion.json'),JSON.stringify(result,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(result));
