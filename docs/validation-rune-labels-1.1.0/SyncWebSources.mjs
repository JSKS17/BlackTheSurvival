// No server changes in 1.1.0: verify byte identity rather than rewrite files.
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {readFile,writeFile} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const here=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(here,'../..'),stage=path.join(root,'Temp/SiteRuneLabels110');
const json=async f=>JSON.parse((await readFile(f,'utf8')).replace(/^\uFEFF/,'')),sha=b=>createHash('sha256').update(b).digest('hex');
const live=await json(path.join(here,'live-validation.json')),sync=await json(path.join(here,'release-sync.json')),frozen=await json(path.join(here,'source-validation.json'));
assert.equal(live.result,'PASS');assert.equal(sync.result,'PASS');assert.equal(frozen.result,'PASS');assert.equal(live.gameVersion,'1.1.0');
for(const[file,expected]of Object.entries(frozen.web))for(const site of[stage,path.join(root,'WebSite')])assert.equal(sha(await readFile(path.join(site,file))),expected,file);
await writeFile(path.join(here,'web-source-sync.json'),JSON.stringify({result:'PASS',verifiedAtUtc:new Date().toISOString(),files:frozen.web,serverFilesChanged:false,gameAssetsSyncedSeparately:true,livePublication:live.publication},null,2)+'\n',{flag:'wx'});
console.log(JSON.stringify({result:'PASS',files:Object.keys(frozen.web).length,serverFilesChanged:false}));
