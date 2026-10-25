// Docs-only, one-time adaptation. Does not build, upload, publish, or clean up.
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {lstat,mkdir,readFile,writeFile} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const here=path.dirname(fileURLToPath(import.meta.url)),workspace=path.resolve(here,'../..');
const previousRoot=path.join(workspace,'docs/validation-skill-references-20261025');
const sha=bytes=>createHash('sha256').update(bytes).digest('hex');
const json=async file=>JSON.parse((await readFile(file,'utf8')).replace(/^\uFEFF/,''));
const old=await json(path.join(previousRoot,'completion.json'));
assert.equal(old.result,'PASS');assert.equal(old.gameVersion,'1.0.26');assert.equal(old.siteVersion,53);
assert.equal(old.sourceCommit,'3618d496050cce352223ca1098b4ea43b1f0dc4d');
const manifest=await readFile(path.join(workspace,'WebSite/lib/unity-assets-manifest.json'));
assert.equal(sha(manifest),old.manifestSha256);
assert.equal(JSON.parse(manifest).buildId,old.gameBuildId);
assert.equal((await json(path.join(workspace,'WebSite/public/game/build-manifest.json'))).productVersion,'1.0.26');
assert(!(await lstat(here)).isSymbolicLink());
const previousHelper=await json(path.join(previousRoot,'helper-prep-validation.json'));
const hashes={};
for(const file of Object.keys(previousHelper.baselineHashes)){
  hashes[file]=sha(await readFile(path.join(workspace,file)));
  if(!file.endsWith('unity-assets-manifest.json'))assert.equal(hashes[file],previousHelper.baselineHashes[file],file);
}
async function writeNew(name,value){await writeFile(path.join(here,name),value,{flag:'wx'});}
function once(source,from,to){assert.equal(source.split(from).length,2,'Adaptation anchor: '+from.slice(0,90));return source.replace(from,to);}
function adapt(source){
  for(const [from,to] of [
    ['validation-skill-references-20261025','validation-wickeline-20261026'],
    ['validation-player-feedback-20261023','validation-skill-references-20261025'],
    ['SiteSkillReferences20261025','SiteWickeline20261026'],['SkillReferencesRelease20261025','WickelineRelease20261026'],
    ['SkillReferencesRelease','WickelineRelease'],
    ['1.0.26','1.0.27'],['1.0.25','1.0.26'],['Web126','Web127'],
    ['4f2bdd240435265a6ad38e49e67469f478664109463f91535c0603de1d147373',old.manifestSha256],
    ['24f431c9fdbb006f28a476aff6302ba41e89054d',old.sourceCommit],
    ['0f171eb9c2bb8ec4c369e905',old.gameBuildId],
    ['versionNumber: 51','versionNumber: 53'],['version_number > 51','version_number > 53'],
    ['version_number -le 51','version_number -le 53'],['baseline Site version 51.','baseline Site version 53.'],
    ['port = 8247','port = 8248'],['previewPort: 8247','previewPort: 8248'],['실험체 연계 설명','위클라인'],
  ])source=source.replaceAll(from,to);
  return source.replaceAll('`skill-references-', '`wickeline-');
}
const inputs={};
for(const name of ['SourceSchema.mjs','PrepareRelease.mjs','VerifyBridge.mjs','VerifyLive.mjs','Cleanup.ps1','Preview.mjs',
  'ValidateBuildSnapshot.mjs','Complete.mjs','ValidateHelpers.mjs','FreezeSources.mjs','RecordCoreResults.mjs']){
  const input=await readFile(path.join(previousRoot,name));inputs[name]=sha(input);
  let source=adapt(input.toString('utf8'));
  if(name==='SourceSchema.mjs'){
    source=once(source,"hashMap(previous.files, 'previous files');","hashMap(previous.files, 'previous files');\n  hashMap(frozen.artSourceHashes, 'artSourceHashes');\n  assert(Object.keys(frozen.artSourceHashes).length > 0, 'Wickeline art/meta hashes required');");
    source=once(source,'testCount: Object.keys(frozen.testSourceHashes).length, ...flags','testCount: Object.keys(frozen.testSourceHashes).length, artCount: Object.keys(frozen.artSourceHashes).length, ...flags');
  }
  if(name==='ValidateBuildSnapshot.mjs'){
    source=once(source,'await verifyMap(frozen.files); await verifyMap(frozen.testSourceHashes);',
      `await verifyMap(frozen.files); await verifyMap(frozen.testSourceHashes); await verifyMap(frozen.artSourceHashes);
let cloneArtFilesVerified = 0;
for (const [relative, expected] of Object.entries(frozen.artSourceHashes)) if (relative.startsWith('Assets/')) {
  assert.equal(sha(await bytes(path.join(cloneRoot, relative))), expected, 'Cloned art/meta changed: ' + relative);
  cloneArtFilesVerified++;
}`);
    source=once(source,'hashes: frozen.files, testSourceHashes: frozen.testSourceHashes,',
      'hashes: frozen.files, testSourceHashes: frozen.testSourceHashes, artSourceHashes: frozen.artSourceHashes,\n  artSourceFiles: shape.artCount, cloneArtFilesVerified,');
  }
  if(name==='Complete.mjs'){
    source=once(source,'await verifySources(frozen.files); await verifySources(frozen.testSourceHashes);',
      'await verifySources(frozen.files); await verifySources(frozen.testSourceHashes); await verifySources(frozen.artSourceHashes);');
    source=once(source,'assert.deepEqual(snapshot.hashes, frozen.files); assert.deepEqual(snapshot.testSourceHashes, frozen.testSourceHashes);',
      `assert.deepEqual(snapshot.hashes, frozen.files); assert.deepEqual(snapshot.testSourceHashes, frozen.testSourceHashes);
  assert.deepEqual(snapshot.artSourceHashes, frozen.artSourceHashes); assert.equal(snapshot.artSourceFiles, shape.artCount);
  assert.equal(snapshot.cloneArtFilesVerified, Object.keys(frozen.artSourceHashes).filter(file => file.startsWith('Assets/')).length);`);
    source=once(source,'frozenTestSourcesVerified: Object.keys(frozen.testSourceHashes).length,',
      'frozenTestSourcesVerified: Object.keys(frozen.testSourceHashes).length, frozenArtFilesVerified: shape.artCount,');
  }
  if(name==='Preview.mjs'){
    source=once(source,"const labels = { 'yuki-combat': '유키 W·E 기준 장면', research: '연구 기록' };",
      "const labels = { 'wickeline-combat': '위클라인 전투', 'wickeline-map': '위클라인 지도', research: '연구 기록' };");
    source=source.replaceAll("names.has('yuki-combat')","names.has('wickeline-combat')").replaceAll("select.value='yuki-combat'","select.value='wickeline-combat'");
  }
  if(name==='ValidateHelpers.mjs'){
    source=once(source,"baseline.gameVersion, '1.0.26'); assert.equal(baseline.siteVersion, 51)","baseline.gameVersion, '1.0.26'); assert.equal(baseline.siteVersion, 53)");
    source=once(source,"'Preview.mjs', 'ValidateHelpers.mjs', 'ValidateBuildSnapshot.mjs', 'Complete.mjs']","'Preview.mjs', 'ValidateHelpers.mjs', 'ValidateBuildSnapshot.mjs', 'Complete.mjs', 'FreezeSources.mjs', 'RecordCoreResults.mjs']");
    source=source.replaceAll("'yuki-combat'","'wickeline-combat'").replaceAll('value="yuki-combat">유키 W·E 기준 장면','value="wickeline-combat">위클라인 전투');
  }
  if(name==='FreezeSources.mjs'){
    source=once(source,"const frozen={result:'PASS'",`const artList=JSON.parse(await readFile(path.join(here,'art-source-paths.json'),'utf8'));
assert(Array.isArray(artList)&&artList.length>0&&new Set(artList).size===artList.length);
const artSourceHashes={};for(const file of [...artList].sort()){
  assert(typeof file==='string'&&!path.isAbsolute(file)&&!file.includes('\\\\')&&!file.split('/').includes('..'));
  artSourceHashes[file]=await hash(file);
}
const frozen={result:'PASS'`);
    source=once(source,'scope:\'Skill target/scope presentation and matching reference documentation; game version increment\',files,testSourceHashes',
      "scope:'Wickeline encounter, combat, runtime art and reference documentation; game version increment',files,testSourceHashes,artSourceHashes");
    source=once(source,'frozenTestSources:shape.testCount,changedRuntimeFiles','frozenTestSources:shape.testCount,frozenArtFiles:shape.artCount,changedRuntimeFiles');
  }
  if(name==='RecordCoreResults.mjs'){
    source=once(source,'for(const entry of manifest.files){',`const expectedTestPaths=[...new Set([
  ...Object.keys(frozen.files).filter(file=>/^Assets\\/Scripts\\/Lumia\\/[^/]+\\.cs$/.test(file)),
  ...Object.keys(frozen.testSourceHashes),
])].sort();
assert.deepEqual(manifest.files.map(entry=>entry.path).sort(),expectedTestPaths,'Exact tested source set');
assert.equal(new Set(manifest.files.map(entry=>entry.path)).size,manifest.files.length);
for(const entry of manifest.files){`);
    source=once(source,'assert.equal(manifest.files.length,Object.keys(inputs).length-2);','assert.equal(manifest.files.length,expectedTestPaths.length);');
    source=once(source,'referenceScope:{subjects:91,skills:364,fixedRecastSources:42,discountRules:70,previousSkillChecks:23,sharedResourcePassives:36}',
      'scope:frozen.scope');
  }
  await writeNew(name,source);
}
await mkdir(path.join(here,'fixtures'),{recursive:true});
const research=await readFile(path.join(previousRoot,'fixtures/research.json'));
await writeNew('fixtures/research.json',research);
await writeNew('baseline-provenance.json',JSON.stringify({result:'PASS',observedAtUtc:new Date().toISOString(),
  scope:'Canonical local baseline corroborated by preserved publication proofs',gameVersion:old.gameVersion,siteVersion:old.siteVersion,
  sourceCommit:old.sourceCommit,environmentRevision:old.environmentRevision,gameBuildId:old.gameBuildId,manifestSha256:sha(manifest),hashes,
  helperInputs:inputs,copiedFixtures:[{source:'docs/validation-skill-references-20261025/fixtures/research.json',target:'fixtures/research.json',bytes:research.length,sha256:sha(research),byteExact:true}],
  baselineSourceProof:'docs/validation-skill-references-20261025/source-validation.json',baselineSourceProofSha256:sha(await readFile(path.join(previousRoot,'source-validation.json'))),
  productSourceChanged:false,oldProofsChanged:false,releaseCommandsExecuted:false},null,2)+'\n');
console.log(JSON.stringify({result:'PASS',helpers:Object.keys(inputs).length,baseline:'1.0.26 / Site53',version:'1.0.27'}));
