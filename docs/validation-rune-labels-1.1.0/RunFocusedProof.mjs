// Root invokes this after Freeze.mjs. The preparation agent only parses it.
import assert from 'node:assert/strict';
import {readFile,readdir,writeFile} from 'node:fs/promises';
import path from 'node:path';
import {createHash} from 'node:crypto';
import {spawn} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const here=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(here,'../..'),sha=b=>createHash('sha256').update(b).digest('hex');
const frozenBytes=await readFile(path.join(here,'source-validation.json')),frozen=JSON.parse(frozenBytes);assert.equal(frozen.result,'PASS');assert.equal(frozen.version,'1.1.0');
const sourceHashes={};
for(const folder of ['Assets/Scripts/Lumia','Tools'])for(const name of(await readdir(path.join(root,folder))).sort()){
 if(!/\.cs$/.test(name)&&!(folder==='Tools'&&name==='RunCoreTests.ps1'))continue;
 const file=folder+'/'+name;sourceHashes[file]=sha(await readFile(path.join(root,file)));assert.equal(sourceHashes[file],(folder==='Tools'?frozen.tools:frozen.product)[file],file);
}
const baseline=JSON.parse(await readFile(path.join(here,'baseline.json'),'utf8'));
assert.equal(sha(await readFile(path.join(root,baseline.priorFullRegression.path))),baseline.priorFullRegression.sha256);
const startedAtUtc=new Date().toISOString(),args=['-NoProfile','-File',path.join(root,'Tools/RunCoreTests.ps1'),'-RuneOffersOnly'];
const result=await new Promise((resolve,reject)=>{
 const child=spawn('pwsh.exe',args,{cwd:root,windowsHide:true}),chunks=[];
 child.stdout.on('data',chunk=>chunks.push(Buffer.from(chunk)));child.stderr.on('data',chunk=>chunks.push(Buffer.from(chunk)));child.once('error',reject);
 child.once('close',(exitCode,signal)=>{let bytes=Buffer.concat(chunks);while(bytes.subarray(0,3).equals(Buffer.from([0xef,0xbb,0xbf])))bytes=bytes.subarray(3);let output,outputEncoding='utf-8';try{output=new TextDecoder('utf-8',{fatal:true}).decode(bytes);}catch{output=new TextDecoder('euc-kr').decode(bytes);outputEncoding='cp949/euc-kr';}resolve({exitCode,signal,output,outputEncoding});});
});
await writeFile(path.join(here,'gameplay-tests.log'),result.output,{flag:'wx'});
for(const[file,expected]of Object.entries(sourceHashes))assert.equal(sha(await readFile(path.join(root,file))),expected,'Input changed while focused tests ran: '+file);
assert.equal(sha(await readFile(path.join(here,'source-validation.json'))),sha(frozenBytes));
const assertions=Number(result.output.match(/PASS:\s*(\d+)\s+assertions?/)?.[1]??0);
const proof={result:result.exitCode===0&&assertions>0?'PASS':'FAIL',version:'1.1.0',scope:'Rune role offers and wording-only regression; full unchanged-gameplay baseline retained separately',startedAtUtc,completedAtUtc:new Date().toISOString(),exitCode:result.exitCode,signal:result.signal,assertions,tests:assertions,sourceFreezeSha256:sha(frozenBytes),sourceHashes,testSourceHashes:Object.fromEntries(Object.entries(sourceHashes).filter(([file])=>file.startsWith('Tools/'))),priorFullRegression:baseline.priorFullRegression,log:'gameplay-tests.log',logSha256:sha(Buffer.from(result.output)),outputEncoding:result.outputEncoding,command:['pwsh.exe',...args],physicalHardwareTested:false};
await writeFile(path.join(here,'gameplay-tests.json'),JSON.stringify(proof,null,2)+'\n',{flag:'wx'});console.log(result.output.trim());console.log(JSON.stringify({result:proof.result,assertions,sourceFreezeSha256:proof.sourceFreezeSha256}));if(proof.result!=='PASS')process.exitCode=1;
