import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {copyFile,mkdir,readFile,writeFile,lstat} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const here=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(here,'../..'),stage=path.join(root,'Temp/SitePresence20261026'),target=path.join(root,'WebSite');
const json=async f=>JSON.parse((await readFile(f,'utf8')).replace(/^\uFEFF/,'')),sha=x=>createHash('sha256').update(x).digest('hex');
const live=await json(path.join(here,'live-validation.json')),sync=await json(path.join(here,'release-sync.json')),frozen=await json(path.join(here,'source-validation.json')),tests=await json(path.join(here,'host-regressions.json'));
assert.equal(live.result,'PASS');assert.equal(sync.result,'PASS');assert.equal(tests.status,'PASS');
const files={...frozen.web,'scripts/game-session.test.mjs':tests.sourceHashes['stage/scripts/game-session.test.mjs']};
const before={};
for(const [relative,expected]of Object.entries(files)) {
 assert(relative&&!path.isAbsolute(relative)&&!relative.split('/').includes('..'));const source=path.join(stage,relative),destination=path.join(target,relative);assert(!(await lstat(source)).isSymbolicLink());assert.equal(sha(await readFile(source)),expected);
 try{assert(!(await lstat(destination)).isSymbolicLink());before[relative]=sha(await readFile(destination));}catch(e){if(e.code!=='ENOENT')throw e;before[relative]=null;}
 await mkdir(path.dirname(destination),{recursive:true});await copyFile(source,destination);assert.equal(sha(await readFile(destination)),expected);
}
await writeFile(path.join(here,'web-source-sync.json'),JSON.stringify({result:'PASS',copiedAtUtc:new Date().toISOString(),files,before,gameAssetsSyncedSeparately:true,livePublication:live.publication},null,2)+'\n');console.log(JSON.stringify({result:'PASS',files:Object.keys(files).length}));
