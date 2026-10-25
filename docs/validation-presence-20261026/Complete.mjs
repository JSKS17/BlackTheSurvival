import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {readFile,writeFile,lstat,readdir} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const here=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(here,'../..'),sha=x=>createHash('sha256').update(x).digest('hex');
const json=async n=>JSON.parse((await readFile(path.join(here,n),'utf8')).replace(/^\uFEFF/,''));
const frozen=await json('source-validation.json');assert.equal(frozen.result,'PASS');assert.equal(frozen.version,'1.0.28');
for(const mapping of [frozen.product,frozen.unchangedArt])for(const[f,h]of Object.entries(mapping))assert.equal(sha(await readFile(path.join(root,f))),h,f);
assert.equal((await readdir(path.join(root,'Assets/Scripts/Lumia'))).filter(f=>f.endsWith('.cs')).length,frozen.runtimeSources);
const serverSync=await json('web-source-sync.json');assert.equal(serverSync.result,'PASS');for(const[f,h]of Object.entries(serverSync.files))assert.equal(sha(await readFile(path.join(root,'WebSite',f))),h,f);
const build=await json('unity-build-manifest.json');assert.equal(build.result,'Succeeded');assert.equal(build.productVersion,'1.0.28');assert.equal(build.errors,0);
const observer=await json('unity-build-observer-result.json');assert.equal(observer.result??observer.status,'PASS');
const backend=await json('backend-tests.json'),client=await json('presence-client-tests.json'),host=await json('host-regressions.json');for(const v of[backend,client,host])assert.equal(v.result??v.status,'PASS');
const browser=await json('browser-validation.json');assert.equal(browser.result,'PASS');assert.equal(browser.gameVersion,'1.0.28');assert.deepEqual(browser.observedCounts,[1,2,1]);for(const file of browser.screenshots){assert(!path.isAbsolute(file)&&!file.includes('..'));assert((await lstat(path.join(here,file))).isFile());}
const live=await json('live-validation.json'),pub=await json('publish-state.json'),sync=await json('release-sync.json'),cleanup=await json('cleanup.json');for(const v of[live,sync,cleanup])assert.equal(v.result,'PASS');assert.equal(pub.deployment.status,'succeeded');assert.equal(pub.saved.id,pub.deployment.version_id);assert.equal(pub.saved.source.commit_sha,pub.package.commit_sha);assert.equal(live.publication.versionId,pub.saved.id);
assert.equal(sha(await readFile(path.join(root,'WebSite/lib/unity-assets-manifest.json'))),live.gameManifestSha256);
for(const relative of['Build','Temp/SitePresence20261026','Temp/PresenceRelease20261026','docs/validation-presence-20261026/site.tar.gz','docs/validation-presence-20261026/bridge.tar.gz']){try{await lstat(path.join(root,relative));assert.fail('Disposable target remains: '+relative);}catch(e){if(e.code!=='ENOENT')throw e;}}
const result={result:'PASS',completedAtUtc:new Date().toISOString(),version:'1.0.28',site:'https://bser-sts.com/',siteVersion:pub.saved.version_number,deploymentId:pub.deployment.id,siteSourceCommit:pub.package.commit_sha,sourceFreezeSha256:sha(await readFile(path.join(here,'source-validation.json'))),runtimeSources:frozen.runtimeSources,backendTests:backend.tests,clientAssertions:client.assertions,hostTests:host.tests??host.totalTests??host.runs.reduce((n,r)=>n+r.tests,0),browserObservedCounts:browser.observedCounts,screenshots:browser.screenshots,physicalMobileTested:false,liveChecks:Object.keys(live.checks).length,removedBytes:cleanup.removedBytes};await writeFile(path.join(here,'completion.json'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
