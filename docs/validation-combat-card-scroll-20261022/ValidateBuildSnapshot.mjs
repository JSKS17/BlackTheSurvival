// Local source snapshot check. Only isolated Unity build settings may differ.
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {readFile,readdir,writeFile,lstat} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const proofRoot=path.dirname(fileURLToPath(import.meta.url)),workspace=path.resolve(proofRoot,'../..');
const sha=bytes=>createHash('sha256').update(bytes).digest('hex');
const readJson=async file=>JSON.parse((await readFile(file,'utf8')).replace(/^\uFEFF/,''));
const frozen=await readJson(path.join(proofRoot,'source-validation.json'));
assert.equal(frozen.result,'PASS');assert.equal(frozen.version,'1.0.24');
assert.equal(frozen.files['Assets/Scripts/Lumia/LumiaTouchScroll.cs'],'165cbbfa78dae4346d2d82320234e2acad90d83669a562386ac8471fe25abd83','Final approved touch geometry source');
const entries=Object.entries(frozen.files);assert.equal(entries.length,120);
for(const [relative,expected] of entries){assert(!path.isAbsolute(relative)&&!relative.split(/[\\/]/).includes('..'));assert.equal(sha(await readFile(path.join(workspace,relative))),expected,'Frozen root changed: '+relative);}
const runtime=entries.filter(([file])=>/^Assets\/Scripts\/Lumia\/[^/]+\.cs$/.test(file));assert.equal(runtime.length,119);
const cloneRoot=path.join(workspace,'Build/UnityWebBuild');
const cloneRuntime=path.join(cloneRoot,'Assets/Scripts/Lumia');
assert(!(await lstat(cloneRuntime)).isSymbolicLink());
assert.deepEqual((await readdir(cloneRuntime)).filter(file=>file.endsWith('.cs')).sort(),runtime.map(([file])=>path.basename(file)).sort());
for(const [relative,expected] of runtime)assert.equal(sha(await readFile(path.join(cloneRoot,relative))),expected,'Isolated C# snapshot changed: '+relative);
const previous=await readJson(path.join(workspace,'docs/validation-mobile-fonts-20261022/source-validation.json'));
assert.equal(previous.runtimeSourceFiles,119);
const changed=runtime.filter(([file,hash])=>previous.runtimeSourceHashes[path.basename(file)]!==hash).map(([file])=>path.basename(file));
assert.deepEqual(changed,['LumiaTouchScroll.cs']);
const settingsPath='ProjectSettings/ProjectSettings.asset';
const rootBytes=await readFile(path.join(workspace,settingsPath)),cloneBytes=await readFile(path.join(cloneRoot,settingsPath));
let expected=rootBytes.toString('utf8').replace(/\r\n/g,'\n');
const replacements=[
  ['  m_BuildTargetBatching:\n  - m_BuildTarget: Standalone\n    m_StaticBatching: 1\n    m_DynamicBatching: 0\n','  m_BuildTargetBatching:\n  - m_BuildTarget: Standalone\n    m_StaticBatching: 1\n    m_DynamicBatching: 0\n  - m_BuildTarget: WebGL\n    m_StaticBatching: 0\n    m_DynamicBatching: 0\n'],
  ['  webGLNameFilesAsHashes: 0\n','  webGLNameFilesAsHashes: 1\n'],
  ['  webGLTemplate: APPLICATION:Default\n','  webGLTemplate: PROJECT:BlackTheSurvival\n'],
  ['  webGLCompressionFormat: 0\n','  webGLCompressionFormat: 1\n'],
  ['  webGLDecompressionFallback: 0\n','  webGLDecompressionFallback: 1\n'],
  ['  webGLInitialMemorySize: 32\n','  webGLInitialMemorySize: 256\n'],
  ['  scriptingBackend:\n    Android: 1\n    Standalone: 0\n','  scriptingBackend:\n    Android: 1\n    Standalone: 0\n    WebGL: 1\n'],
];
for(const [from,to] of replacements){assert.equal(expected.split(from).length,2,'Build override anchor must match once: '+from);expected=expected.replace(from,to);}
const actual=cloneBytes.toString('utf8').replace(/\r\n/g,'\n');
const preloadLine='  preloadedAssets:\n  - {fileID: -944628639613478452, guid: 2bcd2660ca9b64942af0de543d8d7100, type: 3}\n';
const expectedDuringBuild=expected.replace('  preloadedAssets: []\n',preloadLine);
assert(actual===expected||actual===expectedDuringBuild,'Only exact standard isolated WebGL build overrides and the authorized build preload asset are allowed');
const preloaded=actual===expectedDuringBuild;
const report={result:'PASS',checkedAtUtc:new Date().toISOString(),previousVersion:'1.0.23',version:'1.0.24',runtimeSources:119,
  changedRuntimeFiles:changed,mobileTypographyUnchanged:previous.runtimeSourceHashes['LumiaMobileTypography.cs']===frozen.files['Assets/Scripts/Lumia/LumiaMobileTypography.cs'],
  sourceSnapshotMatches:true,hashes:frozen.files,snapshotScope:'119 runtime C# files byte exact; all 120 frozen root files byte exact; cloned PlayerSettings validated against exact isolated WebGL build overrides',
  cloneSettingsSha256:sha(cloneBytes),rootSettingsSha256:sha(rootBytes),settingsOverrides:['WebGL batching 0/0','hashed asset names','PROJECT:BlackTheSurvival template','Gzip compression','decompression fallback','initial memory 256 MiB','WebGL IL2CPP backend; Android1 and Standalone0 preserved',...(preloaded?['Exact authorized build preload asset 2bcd2660ca9b64942af0de543d8d7100']:[])],preloadedAssets:preloaded?'Authorized build asset: fileID -944628639613478452 / guid 2bcd2660ca9b64942af0de543d8d7100 / type3':'Root and completed build snapshot both remain empty'};
await writeFile(path.join(proofRoot,'build-source-validation.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({result:'PASS',runtimeSources:119,frozenRootFiles:120,changedRuntimeFiles:changed,cloneSettingsSha256:report.cloneSettingsSha256}));
