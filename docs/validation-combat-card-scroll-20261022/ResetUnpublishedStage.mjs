// One-time guarded local reset of the unpublished attempt-1 asset allowlist.
// Canonical files, source history and production hosting are never modified.
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {execFileSync} from 'node:child_process';
import {readFile,writeFile,readdir,lstat,realpath,rename} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const proofRoot=path.dirname(fileURLToPath(import.meta.url)),workspace=path.resolve(proofRoot,'../..');
const canonical=path.join(workspace,'WebSite'),stage=path.join(workspace,'Temp/SiteCombatCardScroll20261022');
const expectedHead='16d05d61ab7099b7692216e97e8de82798d47fa2';
const expectedCanonicalManifest='3555cdf0a36169907fffca5dbd7b8f6befdf796fffe49d95eca4ef3abbbe1c13';
const oldFuture='/game/Build/19919064a8058a6e6e330c71c437d5f3.data.unityweb';
const sha=bytes=>createHash('sha256').update(bytes).digest('hex');
async function checked(file){
  const absolute=path.resolve(file),relative=path.relative(workspace,absolute);
  assert(relative&&!relative.startsWith('..')&&!path.isAbsolute(relative));
  let cursor=workspace;
  for(const segment of ['',...relative.split(path.sep)]){
    if(segment)cursor=path.join(cursor,segment);
    const info=await lstat(cursor);assert(!info.isSymbolicLink(),'Linked path: '+cursor);
    assert.equal((await realpath(cursor)).toLowerCase(),cursor.toLowerCase(),'Resolved path changed');
  }
  return absolute;
}
async function absent(file){try{await lstat(file);return false;}catch(error){if(error.code==='ENOENT')return true;throw error;}}
async function bytes(file){return readFile(await checked(file));}
const parse=bytes=>JSON.parse(bytes.toString('utf8').replace(/^\uFEFF/,''));
async function tree(root){
  const result=[];
  async function walk(directory,prefix=''){
    await checked(directory);
    for(const entry of (await readdir(directory,{withFileTypes:true})).sort((a,b)=>a.name.localeCompare(b.name))){
      const file=path.join(directory,entry.name),relative=prefix+entry.name;
      await checked(file);
      if(entry.isDirectory())await walk(file,relative+'/');
      else{assert(entry.isFile());const content=await bytes(file);result.push({path:relative,size:content.length,sha256:sha(content)});}
    }
  }
  await walk(root);return result;
}
const report={result:'RUNNING',checkedAtUtc:new Date().toISOString(),productionRequests:false,productionChanges:false,canonicalChanged:false};
try{
  for(const name of ['bridge-state.json','publish-state.json','bridge-validation.json','live-validation.json','release-finalize.json','release-sync.json','site.tar.gz','bridge.tar.gz'])assert(await absent(path.join(proofRoot,name)),'Publication already exists; reset refused: '+name);
  await checked(stage);
  const head=execFileSync('C:/Program Files/Git/bin/git.exe',['-C',stage,'rev-parse','HEAD'],{encoding:'utf8',windowsHide:true}).trim();assert.equal(head,expectedHead,'Unpublished checkout moved from baseline');
  const canonicalFile=path.join(canonical,'lib/unity-assets-manifest.json'),stageFile=path.join(stage,'lib/unity-assets-manifest.json');
  const canonicalBytes=await bytes(canonicalFile),stageBytes=await bytes(stageFile);
  assert.equal(sha(canonicalBytes),expectedCanonicalManifest,'Canonical baseline changed');
  const original=parse(canonicalBytes),staged=parse(stageBytes);
  const originalMeta={...original},stagedMeta={...staged};delete originalMeta.files;delete stagedMeta.files;
  assert.deepEqual(stagedMeta,originalMeta,'Stage must still point to the old public build');
  for(const [url,record] of Object.entries(original.files))assert.deepEqual(staged.files[url],record,'Old immutable record changed: '+url);
  const added=Object.keys(staged.files).filter(url=>!Object.hasOwn(original.files,url));assert.deepEqual(added,[oldFuture],'Only known unpublished attempt-1 data may be removed');
  const oldBuild=parse(await bytes(path.join(proofRoot,'attempt1-unity-build-manifest.json')));assert.equal(oldBuild.result,'Succeeded');assert.equal(oldBuild.productVersion,'1.0.24');
  const data=oldBuild.files.find(file=>'/game/'+file.path===oldFuture);assert(data,'Attempt-1 manifest must contain the exact future data filename');
  const extra=staged.files[oldFuture];assert.equal(extra.storage,'r2');assert.equal(extra.size,data.bytes);assert.match(extra.sha256,/^[a-f0-9]{64}$/);assert.equal(extra.objectKey,'unity-assets/'+extra.sha256);
  assert.equal(extra.uploadParts.reduce((total,part)=>total+part.size,0),extra.size);
  const canonicalTree=await tree(path.join(canonical,'public/game')),stageTree=await tree(path.join(stage,'public/game'));assert.deepEqual(stageTree,canonicalTree,'Unpublished stage game must be byte exact canonical');
  for(const relative of ['lib/game-accounts.mjs','app/page.tsx','.gitattributes','app/game.css'])assert.equal(sha(await bytes(path.join(stage,relative))),sha(await bytes(path.join(canonical,relative))),'Protected source changed: '+relative);
  const failedFile=path.join(proofRoot,'release-prepare.json'),failedBytes=await bytes(failedFile),failed=parse(failedBytes);
  assert.equal(failed.result,'FAIL');assert.equal(failed.error,'Unexpected stage asset: '+oldFuture);
  const backupManifest=path.join(proofRoot,'unpublished-attempt1-unity-assets-manifest.json'),backupFailure=path.join(proofRoot,'attempt2-failed-release-prepare.json');
  assert(await absent(backupManifest));assert(await absent(backupFailure));
  const candidate=path.join(stage,'lib/.combat-card-scroll-baseline-reset.json');assert(await absent(candidate));
  report.gates={unpublishedBaselineHead:true,noPublicationArtifacts:true,stageGameByteExactCanonical:true,onlyKnownAttempt1R2Record:true,oldBuildMetadataUnchanged:true,protectedSourcesUnchanged:true};
  report.stageHead=head;report.canonicalManifestSha256=sha(canonicalBytes);report.stageManifestBeforeSha256=sha(stageBytes);report.removedAllowlistRecord={path:oldFuture,size:extra.size,sha256:extra.sha256};
  await writeFile(backupManifest,stageBytes,{flag:'wx'});await writeFile(backupFailure,failedBytes,{flag:'wx'});
  await writeFile(candidate,canonicalBytes,{flag:'wx'});await checked(candidate);await rename(candidate,stageFile);
  assert.equal(sha(await bytes(stageFile)),expectedCanonicalManifest);assert.equal(sha(await bytes(canonicalFile)),expectedCanonicalManifest);
  assert.deepEqual(await tree(path.join(stage,'public/game')),canonicalTree);
  report.result='PASS';report.stageManifestAfterSha256=sha(await bytes(stageFile));report.prepareCanRerun=true;
}catch(error){report.result='FAIL';report.error=error.message;process.exitCode=1;}
report.completedAtUtc=new Date().toISOString();await writeFile(path.join(proofRoot,'stage-reset-validation.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({result:report.result,prepareCanRerun:report.prepareCanRerun,canonicalChanged:false,error:report.error}));
