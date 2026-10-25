// Final local evidence audit. Run only after build, browser verification,
// publication, canonical sync and cleanup have produced their required proofs.
// No network, account requests, process control, cleanup or deployment occurs.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { lstat, readFile, realpath, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const proofRoot=path.dirname(fileURLToPath(import.meta.url));
const workspace=path.resolve(proofRoot,'../..');
const project='appgprj_6ad077cf90708191a3e75367b4b3b78c';
const version='1.0.24',origin='https://bser-sts.com/';
const approvedTouchSource='165cbbfa78dae4346d2d82320234e2acad90d83669a562386ac8471fe25abd83';
const eventFailures=['EventOnlyTouchSurvivesRepaint','DelayedNativePollAdoptsEventGesture','NativePollAfterPlainMouseDown','EventOnlyTapUsesExactlyOnce','EventOnlyAfterCanceledNative'];
const geometryFailures=['ScaledCardSwipe','MeasuredMobileNestedBounds','MeasuredMobileNestedSwipe','ScrolledParentFractionalRounding','DoubleNestedMobileBoundsAndSwipe','NonzeroViewRectOrigins'];
const samePath=(a,b)=>path.resolve(a).toLowerCase()===path.resolve(b).toLowerCase();
const digest=bytes=>createHash('sha256').update(bytes).digest('hex');
const inside=(root,file)=>{const relative=path.relative(root,file);return relative!==''&&relative!=='..'&&!relative.startsWith('..'+path.sep)&&!path.isAbsolute(relative);};
async function checked(file){
  const absolute=path.resolve(file);assert(inside(workspace,absolute),'Evidence path outside workspace: '+absolute);
  let cursor=workspace;
  for(const segment of ['',...path.relative(workspace,absolute).split(path.sep)]){
    if(segment)cursor=path.join(cursor,segment);
    const info=await lstat(cursor);assert(!info.isSymbolicLink(),'Linked evidence path: '+cursor);
    assert(samePath(await realpath(cursor),cursor),'Resolved evidence path changed: '+cursor);
  }
  return absolute;
}
async function bytes(file){return readFile(await checked(file));}
async function json(name,expectedResult='PASS'){
  const result=JSON.parse((await bytes(path.join(proofRoot,name))).toString('utf8').replace(/^\uFEFF/,''));
  if(expectedResult!==null)assert.equal(result.result,expectedResult,'Required proof: '+name);
  return result;
}
async function optional(name){
  try{return JSON.parse((await bytes(path.join(proofRoot,name))).toString('utf8').replace(/^\uFEFF/,''));}
  catch(error){if(error.code==='ENOENT')return null;throw error;}
}
async function sourceHashes(map){
  assert(map&&typeof map==='object'&&!Array.isArray(map));
  for(const [relative,expected] of Object.entries(map)){
    assert(!path.isAbsolute(relative)&&!relative.split(/[\\/]/).includes('..'),'Frozen source must be workspace relative');
    assert.match(expected,/^[a-f0-9]{64}$/);
    assert.equal(digest(await bytes(path.join(workspace,relative))),expected,'Frozen source changed: '+relative);
  }
}
function publicationMatches(proof,publication,label){
  assert.equal(proof.versionId,publication.saved.id,label+' version');
  assert.equal(proof.commitSha,publication.package.commit_sha,label+' source');
  assert.equal(proof.deploymentId,publication.deployment.id,label+' deployment');
  if(proof.versionNumber!==undefined)assert.equal(proof.versionNumber,publication.saved.version_number,label+' Site version');
}

async function main(){
  const frozen=await json('source-validation.json');assert.equal(frozen.version,version);await sourceHashes(frozen.files);
  const runtime=Object.keys(frozen.files).filter(file=>/^Assets\/Scripts\/Lumia\/[^/]+\.cs$/.test(file));
  assert.equal(runtime.length,119,'Frozen runtime source count');
  const previous=JSON.parse((await bytes(path.join(workspace,'docs/validation-mobile-fonts-20261022/source-validation.json'))).toString('utf8').replace(/^\uFEFF/,''));
  assert.equal(previous.result,'PASS');assert.equal(previous.gameVersion,'1.0.23');assert.equal(previous.runtimeSourceFiles,119);
  assert.deepEqual(runtime.map(file=>path.basename(file)).sort(),Object.keys(previous.runtimeSourceHashes).sort(),'Runtime file set changed');
  const actualChanged=runtime.filter(file=>frozen.files[file]!==previous.runtimeSourceHashes[path.basename(file)]).map(file=>path.basename(file));
  assert.deepEqual(actualChanged,['LumiaTouchScroll.cs'],'Only touch integration source may change from the deployed baseline');
  for(const [relative,expected] of Object.entries(previous.extraSourceHashes)){
    if(relative.replaceAll('\\','/')==='ProjectSettings/ProjectSettings.asset')continue;
    assert.equal(digest(await bytes(path.join(workspace,relative))),expected,'Mobile presentation baseline changed: '+relative);
  }
  const snapshot=await json('build-source-validation.json');assert.equal(snapshot.version,version);
  assert.equal(snapshot.previousVersion,'1.0.23');assert.equal(snapshot.runtimeSources,119);
  assert.deepEqual(snapshot.changedRuntimeFiles,['LumiaTouchScroll.cs']);
  assert.equal(snapshot.mobileTypographyUnchanged,true);assert.equal(snapshot.sourceSnapshotMatches,true);
  assert.deepEqual(snapshot.hashes,frozen.files,'Built source snapshot and frozen root source must agree');
  const regression=await json('combat-card-touch-tests.json');
  assert.equal(regression.assertions,88);assert.equal(regression.scenarios,23);
  assert.deepEqual(regression.failedScenarios,[]);await sourceHashes(regression.sourceHashes);
  assert.equal(regression.sourceHashes['Assets/Scripts/Lumia/LumiaTouchScroll.cs'],approvedTouchSource,'Approved final geometry source');
  const core=await json('touch-scroll-tests.json');assert.equal(core.assertions,114);assert.equal(core.scenarios,17);await sourceHashes(core.sourceHashes);
  const baseline=await json('combat-card-touch-baseline-tests.json','FAIL');
  assert.equal(baseline.scenarios,23);
  assert.deepEqual([...baseline.failedScenarios].sort(),[...eventFailures,...geometryFailures].sort(),'Expected eleven original-code failures');
  const geometryBaseline=await json('combat-card-touch-geometry-baseline-tests.json','FAIL');
  assert.equal(geometryBaseline.scenarios,23);assert.deepEqual([...geometryBaseline.failedScenarios].sort(),[...geometryFailures].sort(),'Expected six pre-geometry failures');
  const geometrySource=Object.entries(geometryBaseline.sourceHashes).filter(([file])=>file.endsWith('/LumiaTouchScroll.cs'));
  assert.equal(geometrySource.length,1);assert.equal(geometrySource[0][1],'6dec57870b95cb38bbb89591cab6b9e3c5afa917e7d8b27c756706fd25b7f867');
  const rootCause=await json('touch-root-cause-review.json','CONFIRMED');
  assert.equal(rootCause.traceSHA256,digest(await bytes(path.join(proofRoot,'browser-touch-trace.json'))),'Root cause must cite the actual saved browser trace');
  assert.equal(rootCause.reviewedProductionSHA256,approvedTouchSource);assert.equal(rootCause.fixReview.verdict,'APPROVED');
  assert.equal(rootCause.nativeTouch.inputCount,1);assert.deepEqual(rootCause.parent.maximumScroll,[0,0]);
  assert.equal(rootCause.childOriginal.registeredBounds[3],0);assert.equal(rootCause.childOriginal.containsNativeTouch,false);
  assert.equal(rootCause.childCorrected.containsNativeTouch,true);assert(rootCause.childCorrected.visibleSize[1]>0);
  assert.equal(rootCause.fixReview.nestedComposition.result,'PASS');
  const baselineBrowser=await json('baseline-browser-touch.json','REPRODUCED');
  assert.equal(baselineBrowser.gameVersion,'1.0.23');assert.equal(baselineBrowser.syntheticBrowserTouch,true);assert.equal(baselineBrowser.physicalHardwareTested,false);
  const build=await json('unity-build-manifest.json','Succeeded');assert.equal(build.productVersion,version);assert.equal(build.errors,0);
  const browser=await json('browser-validation.json');assert.equal(browser.gameVersion,version);
  const physicalHardwareTested=browser.physicalHardwareTested??browser.physicalMobileHardwareTested;
  assert.equal(physicalHardwareTested,false,'Browser proof must explicitly preserve the physical-hardware limitation');
  const live=await json('live-validation.json');assert.equal(live.gameVersion,version);
  assert(Object.keys(live.checks??{}).length>0);assert(Object.values(live.checks).every(value=>value===true),'Live checks must all pass');
  assert.deepEqual(live.errors,[]);
  const publication=await json('publish-state.json',null);
  assert.equal(publication.package.project_id,project);assert.equal(publication.saved.project_id,project);assert.equal(publication.deployment.project_id,project);
  assert.equal(publication.saved.source.commit_sha,publication.package.commit_sha);assert.match(publication.package.commit_sha,/^[a-f0-9]{40}$/);
  assert.equal(publication.deployment.version_id,publication.saved.id);assert.equal(publication.deployment.status,'succeeded');
  assert(publication.saved.version_number>47);assert(samePath(publication.package.checkout_path,path.join(workspace,'Temp/SiteCombatCardScroll20261022')));
  publicationMatches(live.publication,publication,'Live proof');
  const sync=await json('release-sync.json');assert.equal(sync.version,version);publicationMatches(sync.publication,publication,'Canonical sync');
  const manifestBytes=await bytes(path.join(workspace,'WebSite/lib/unity-assets-manifest.json'));
  const manifest=JSON.parse(manifestBytes.toString('utf8'));const manifestSha256=digest(manifestBytes);
  assert.equal(manifestSha256,live.gameManifestSha256);assert.equal(manifestSha256,sync.installed.manifestSha256);
  assert.equal(manifest.ready,true);assert.equal(manifest.buildId,live.gameBuildId);assert.equal(manifest.buildId,sync.installed.buildId);
  const packagedBuild=JSON.parse((await bytes(path.join(workspace,'WebSite/public/game/build-manifest.json'))).toString('utf8'));
  assert.equal(packagedBuild.productVersion,version);assert.equal(packagedBuild.result,'Succeeded');
  const helper=await json('helper-prep-validation.json');
  for(const relative of ['WebSite/lib/game-accounts.mjs','WebSite/app/page.tsx','WebSite/.gitattributes','WebSite/app/game.css']){
    assert.equal(digest(await bytes(path.join(workspace,relative))),helper.baselineHashes[relative],'Protected web source changed: '+relative);
  }
  const cleanup=await json('cleanup.json');assert(samePath(cleanup.workspace,workspace));
  assert(Number.isSafeInteger(cleanup.removedBytes)&&cleanup.removedBytes>=0);
  assert.equal(cleanup.removedBytes,cleanup.removed.reduce((total,target)=>total+target.bytes,0));
  const allowed=new Set(['Build','Temp/SiteCombatCardScroll20261022','Temp/CombatCardScrollRelease20261022','docs/validation-combat-card-scroll-20261022/site.tar.gz','docs/validation-combat-card-scroll-20261022/bridge.tar.gz'].map(file=>path.resolve(workspace,file).toLowerCase()));
  for(const target of cleanup.removed){
    assert(allowed.has(path.resolve(target.path).toLowerCase()),'Unexpected cleanup target');
    try{await lstat(target.path);assert.fail('Cleanup target remains: '+target.path);}catch(error){if(error.code!=='ENOENT')throw error;}
  }
  let siteMetadata=null,siteMetadataFile=null;
  for(const name of ['site-final-metadata.json','site-final.json']){
    const metadata=await optional(name);if(!metadata)continue;
    const site=metadata.site??metadata.structuredContent??metadata;
    assert.equal(site.id??site.project_id,project,'Final Site project');
    assert.equal(site.version??site.version_number,publication.saved.version_number,'Final Site version');
    assert.equal(site.access??site.visibility,'public','Final Site access');
    assert.equal(new URL(site.url).origin,new URL(origin).origin,'Final custom domain');
    siteMetadata=site;siteMetadataFile=name;break;
  }
  const screenshots=Array.isArray(browser.screenshots)?browser.screenshots.filter(file=>typeof file==='string'):[];
  for(const file of screenshots){assert(inside(proofRoot,path.resolve(proofRoot,file)),'Screenshot outside proof directory');await checked(path.join(proofRoot,file));}
  const completion={result:'PASS',completedAtUtc:new Date().toISOString(),site:origin,gameVersion:version,
    siteVersion:publication.saved.version_number,sourceCommit:publication.package.commit_sha,deploymentId:publication.deployment.id,
    environmentRevision:publication.deployment.env_set_revision,gameBuildId:manifest.buildId,manifestSha256,
    runtimeSourceFiles:runtime.length,frozenFilesVerified:Object.keys(frozen.files).length,changedRuntimeFiles:snapshot.changedRuntimeFiles,
    tests:{combatCardTouch:{assertions:regression.assertions,scenarios:regression.scenarios,result:regression.result},
      touchScrollCore:{assertions:core.assertions,scenarios:core.scenarios,result:core.result},
      baselineExpectedFailures:baseline.failedScenarios,preGeometryExpectedFailures:geometryBaseline.failedScenarios,baselineSyntheticBrowserBugReproduced:true},
    rootCauseProof:'touch-root-cause-review.json',approvedTouchSourceSha256:approvedTouchSource,
    browserProof:'browser-validation.json',physicalHardwareTested,liveChecks:Object.keys(live.checks).length,
    removedBytes:cleanup.removedBytes,removedTargets:cleanup.removed.length,freeBytesAfter:cleanup.freeBytesAfter,
    publicSiteMetadataVerified:Boolean(siteMetadata),...(siteMetadata?{siteAccess:'public',siteMetadataFile}:{}),screenshots,
    limitation:'Browser touch verification uses synthetic TouchEvents; physical mobile hardware was not tested.'};
  await writeFile(path.join(proofRoot,'completion.json'),JSON.stringify(completion,null,2)+'\n');
  console.log(JSON.stringify({result:'PASS',gameVersion:version,siteVersion:completion.siteVersion,sourceCommit:completion.sourceCommit,
    verifiedFrozenFiles:completion.frozenFilesVerified,removedBytes:completion.removedBytes,physicalHardwareTested:false,publicSiteMetadataVerified:completion.publicSiteMetadataVerified}));
}
try{await main();}catch(error){console.error(JSON.stringify({result:'FAIL',error:error.message}));process.exitCode=1;}
