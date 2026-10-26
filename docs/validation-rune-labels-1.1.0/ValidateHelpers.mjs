// Parse helpers only. This deliberately does not execute build/deploy/preview helpers.
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {createHash} from 'node:crypto';
import {readFile,readdir,writeFile} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {Script} from 'node:vm';
const here=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(here,'../..'),sha=b=>createHash('sha256').update(b).digest('hex');
const names=(await readdir(here)).filter(name=>/\.(?:mjs|ps1)$/.test(name)).sort();
for(const name of names.filter(name=>name.endsWith('.mjs'))){const child=spawnSync(process.execPath,['--check',path.join(here,name)],{encoding:'utf8'});assert.equal(child.status,0,name+': '+child.stderr);}
const ps=spawnSync('pwsh',['-NoProfile','-File',path.join(here,'ValidatePowerShell.ps1')],{encoding:'utf8'});assert.equal(ps.status,0,ps.error?.message??ps.stderr);const parsed=JSON.parse(ps.stdout.replace(/^\uFEFF/,''));assert.equal(parsed.result,'PASS');
const preview=await readFile(path.join(here,'Preview.mjs'),'utf8');
const htmlFunction=preview.slice(preview.indexOf('async function qaHtml()'),preview.indexOf('\nasync function boundedBody'));
assert(htmlFunction.startsWith('async function qaHtml()'));
const html=await new Function('fixtureNames','gameVersion','baseline',`return (${htmlFunction})();`)(async()=>['research'],'1.1.0',false);
const embedded=html.match(/<script>([\s\S]+)<\/script>/)?.[1];assert(embedded);new Script(embedded);
const baseline=JSON.parse(await readFile(path.join(here,'baseline.json'),'utf8'));
for(const[file,expected]of Object.entries(baseline.files))assert.equal(sha(await readFile(path.join(root,'WebSite',file))),expected,file);
assert.equal(sha(await readFile(path.join(root,'WebSite/lib/unity-assets-manifest.json'))),baseline.manifestSha256);
const forbidden=[/SitePresence20261026/,/PresenceRelease20261026/,/Build\/Web128/,/backend-tests\.json/,/presence-client-tests\.json/,/host-regressions\.json/,/github-publish-state\.json/,/windows-build-manifest\.json/];
for(const name of ['PrepareRelease.mjs','VerifyBridge.mjs','VerifyLive.mjs','SyncWebSources.mjs','Cleanup.ps1','Complete.mjs','Preview.mjs','ObserveUnityBuild.ps1','StartBuild.ps1'])for(const pattern of forbidden)assert(!pattern.test(await readFile(path.join(here,name),'utf8')),name+' retains stale dependency '+pattern);
const files=Object.fromEntries(await Promise.all(names.map(async name=>[name,sha(await readFile(path.join(here,name)))])));
const result={result:'PASS',validatedAtUtc:new Date().toISOString(),nodeScripts:names.filter(name=>name.endsWith('.mjs')).length,powerShellScripts:parsed.parsed.length,embeddedPreviewScript:true,protectedWebFiles:Object.keys(baseline.files).length,baselineVersion:baseline.version,baselineManifestSha256:baseline.manifestSha256,buildStarted:false,serverStarted:false,productionRequests:false,files};
await writeFile(path.join(here,'helper-prep-validation.json'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
