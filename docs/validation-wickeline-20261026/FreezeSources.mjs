// Root-owned source freeze, invoked only after implementation and tests finish.
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {lstat,readFile,readdir,writeFile} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {sourceShape} from './SourceSchema.mjs';
const here=path.dirname(fileURLToPath(import.meta.url)),workspace=path.resolve(here,'../..');
const old=JSON.parse(await readFile(path.join(workspace,'docs/validation-skill-references-20261025/source-validation.json'),'utf8'));
const sha=bytes=>createHash('sha256').update(bytes).digest('hex');
async function hash(relative){const file=path.join(workspace,relative);assert(!(await lstat(file)).isSymbolicLink());return sha(await readFile(file));}
const runtime=(await readdir(path.join(workspace,'Assets/Scripts/Lumia'))).filter(x=>x.endsWith('.cs')).sort().map(x=>'Assets/Scripts/Lumia/'+x);
const editor=(await readdir(path.join(workspace,'Assets/Editor'))).filter(x=>x.endsWith('.cs')||x.endsWith('.cs.meta')).sort().map(x=>'Assets/Editor/'+x);
const files={};for(const file of [...runtime,...editor,'ProjectSettings/ProjectSettings.asset','docs/SOURCES.md','README.md','.gitignore'])files[file]=await hash(file);
const testSourceHashes={};
const tests=[...new Set([...Object.keys(old.testSourceHashes),...(await readdir(path.join(workspace,'Tools'))).filter(x=>x.endsWith('.cs')).map(x=>'Tools/'+x)])].sort();
for(const file of tests)testSourceHashes[file]=await hash(file);
const allRuntime=[...new Set([...Object.keys(old.files).filter(x=>/^Assets\/Scripts\/Lumia\/[^/]+\.cs$/.test(x)),...runtime])].sort();
const changedRuntimeFiles=allRuntime.filter(file=>old.files[file]!==files[file]).map(file=>path.basename(file));
const artList=JSON.parse(await readFile(path.join(here,'art-source-paths.json'),'utf8'));
assert(Array.isArray(artList)&&artList.length>0&&new Set(artList).size===artList.length);
const artSourceHashes={};for(const file of [...artList].sort()){
  assert(typeof file==='string'&&!path.isAbsolute(file)&&!file.includes('\\')&&!file.split('/').includes('..'));
  artSourceHashes[file]=await hash(file);
}
const frozen={result:'PASS',frozenAtUtc:new Date().toISOString(),version:'1.0.27',previousVersion:'1.0.26',runtimeSources:runtime.length,
  changedRuntimeFiles,touchScrollUnchanged:old.files['Assets/Scripts/Lumia/LumiaTouchScroll.cs']===files['Assets/Scripts/Lumia/LumiaTouchScroll.cs'],
  mobileTypographyUnchanged:old.files['Assets/Scripts/Lumia/LumiaMobileTypography.cs']===files['Assets/Scripts/Lumia/LumiaMobileTypography.cs'],
  baselineSourceProof:'docs/validation-skill-references-20261025/source-validation.json',
  scope:'Wickeline encounter, combat, runtime art and reference documentation; game version increment',files,testSourceHashes,artSourceHashes};
const shape=sourceShape(frozen,old);assert(shape.changed.length>0);
await writeFile(path.join(here,'source-validation.json'),JSON.stringify(frozen,null,2)+'\n',{flag:'wx'});
console.log(JSON.stringify({result:'PASS',runtimeSources:runtime.length,frozenRootFiles:shape.rootCount,frozenTestSources:shape.testCount,frozenArtFiles:shape.artCount,changedRuntimeFiles}));
