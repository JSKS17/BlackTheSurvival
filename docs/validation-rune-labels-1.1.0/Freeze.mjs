// Run once after product, tests and role documentation are final.
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {readFile,readdir,writeFile,lstat} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const here=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(here,'../..'),stage=path.join(root,'Temp/SiteRuneLabels110');
const sha=x=>createHash('sha256').update(x).digest('hex');
const hash=async f=>{assert(!(await lstat(f)).isSymbolicLink(),f);return sha(await readFile(f));};
const baseline=JSON.parse(await readFile(path.join(here,'baseline.json'),'utf8'));
assert.match(await readFile(path.join(root,'ProjectSettings/ProjectSettings.asset'),'utf8'),/^\s*bundleVersion: 1\.1\.0\s*$/m);
const product={};
for(const dir of ['Assets/Scripts/Lumia','Assets/Editor'])for(const name of(await readdir(path.join(root,dir))).sort())if(/\.cs(?:\.meta)?$/.test(name))product[dir+'/'+name]=await hash(path.join(root,dir,name));
async function collect(relative,target,accept=()=>true){
 for(const entry of(await readdir(path.join(root,relative),{withFileTypes:true})).sort((a,b)=>a.name.localeCompare(b.name))){
  const name=relative+'/'+entry.name;if(entry.name.startsWith('.')||entry.name==='__pycache__')continue;
  assert(!entry.isSymbolicLink(),name);if(entry.isDirectory())await collect(name,target,accept);else if(entry.isFile()&&accept(name))target[name]=await hash(path.join(root,name));
 }
}
await collect('Assets/WebGLTemplates/BlackTheSurvival',product);
await collect('Assets/Plugins/WebGL',product);
for(const file of ['ProjectSettings/ProjectSettings.asset','Packages/manifest.json','Packages/packages-lock.json','README.md','docs/SOURCES.md','.gitignore'])product[file]=await hash(path.join(root,file));
const web={};for(const[file,expected]of Object.entries(baseline.files)){
 web[file]=await hash(path.join(root,'WebSite',file));assert.equal(web[file],expected,'Canonical server baseline changed: '+file);
 assert.equal(await hash(path.join(stage,file)),expected,'Publication server source changed: '+file);
}
const previous=JSON.parse(await readFile(path.join(root,'docs/validation-presence-20261026/source-validation.json'),'utf8'));
const unchangedPresence={};for(const file of ['Assets/Scripts/Lumia/LumiaBrowserPresence.cs','Assets/Plugins/WebGL/BrowserPresence.jslib','Assets/Plugins/WebGL/BrowserPresence.jslib.meta','Assets/WebGLTemplates/BlackTheSurvival/TemplateData/presence.js','Assets/WebGLTemplates/BlackTheSurvival/TemplateData/presence.js.meta']){
 assert.equal(product[file],previous.product[file],'Existing presence integration changed: '+file);unchangedPresence[file]=product[file];
}
const unchangedArt={};for(const[file,expected]of Object.entries(previous.unchangedArt)){
 unchangedArt[file]=await hash(path.join(root,file));assert.equal(unchangedArt[file],expected,'Unchanged art changed: '+file);
}
const tools={};await collect('Tools',tools,file=>/\.(?:cs|js|mjs|ps1|py|json)$/.test(file));
const sourceDocumentation={};await collect('docs/rune-roles-1.1.0',sourceDocumentation,file=>/\.(?:md|json|csv|txt)$/.test(file));assert(Object.keys(sourceDocumentation).length,'Public role source documentation must exist');
const value={result:'PASS',version:'1.1.0',previousVersion:'1.1.0',frozenAtUtc:new Date().toISOString(),runtimeSources:Object.keys(product).filter(f=>/^Assets\/Scripts\/Lumia\/.*\.cs$/.test(f)).length,product,web,unchangedArt,unchangedPresence,tools,sourceDocumentation,scope:'Rune family label wording only; gameplay, official role data, hosting/account/presence services and art remain unchanged'};
await writeFile(path.join(here,'source-validation.json'),JSON.stringify(value,null,2)+'\n',{flag:'wx'});
console.log(JSON.stringify({result:value.result,runtimeSources:value.runtimeSources,productFiles:Object.keys(product).length,webFiles:Object.keys(web).length,toolsFiles:Object.keys(tools).length,documentationFiles:Object.keys(sourceDocumentation).length,sha256:sha(await readFile(path.join(here,'source-validation.json')))}));
