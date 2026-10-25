// Root-owned source freeze, invoked only after implementation and tests finish.
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {lstat,readFile,readdir,writeFile} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {sourceShape} from './SourceSchema.mjs';
const here=path.dirname(fileURLToPath(import.meta.url)),workspace=path.resolve(here,'../..');
const old=JSON.parse(await readFile(path.join(workspace,'docs/validation-player-feedback-20261023/source-validation.json'),'utf8'));
const sha=bytes=>createHash('sha256').update(bytes).digest('hex');
async function hash(relative){const file=path.join(workspace,relative);assert(!(await lstat(file)).isSymbolicLink());return sha(await readFile(file));}
const runtime=(await readdir(path.join(workspace,'Assets/Scripts/Lumia'))).filter(x=>x.endsWith('.cs')).sort().map(x=>'Assets/Scripts/Lumia/'+x);
const files={};for(const file of [...runtime,'ProjectSettings/ProjectSettings.asset','docs/SOURCES.md'])files[file]=await hash(file);
const testSourceHashes={};
const tests=[...new Set([...Object.keys(old.testSourceHashes),...(await readdir(path.join(workspace,'Tools'))).filter(x=>x.endsWith('.cs')).map(x=>'Tools/'+x)])].sort();
for(const file of tests)testSourceHashes[file]=await hash(file);
const allRuntime=[...new Set([...Object.keys(old.files).filter(x=>/^Assets\/Scripts\/Lumia\/[^/]+\.cs$/.test(x)),...runtime])].sort();
const changedRuntimeFiles=allRuntime.filter(file=>old.files[file]!==files[file]).map(file=>path.basename(file));
const frozen={result:'PASS',frozenAtUtc:new Date().toISOString(),version:'1.0.26',previousVersion:'1.0.25',runtimeSources:runtime.length,
  changedRuntimeFiles,touchScrollUnchanged:old.files['Assets/Scripts/Lumia/LumiaTouchScroll.cs']===files['Assets/Scripts/Lumia/LumiaTouchScroll.cs'],
  mobileTypographyUnchanged:old.files['Assets/Scripts/Lumia/LumiaMobileTypography.cs']===files['Assets/Scripts/Lumia/LumiaMobileTypography.cs'],
  baselineSourceProof:'docs/validation-player-feedback-20261023/source-validation.json',
  scope:'Skill target/scope presentation and matching reference documentation; game version increment',files,testSourceHashes};
const shape=sourceShape(frozen,old);assert(shape.changed.length>0);
await writeFile(path.join(here,'source-validation.json'),JSON.stringify(frozen,null,2)+'\n',{flag:'wx'});
console.log(JSON.stringify({result:'PASS',runtimeSources:runtime.length,frozenRootFiles:shape.rootCount,frozenTestSources:shape.testCount,changedRuntimeFiles}));
