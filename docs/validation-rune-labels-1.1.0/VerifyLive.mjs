import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {readFile,writeFile} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const here=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(here,'../..'),stage=path.join(root,'Temp/SiteRuneLabels110'),release=path.join(root,'Temp/RuneLabelsRelease110'),origin='https://bser-sts.com';
const sha=x=>createHash('sha256').update(x).digest('hex');
const json=async f=>JSON.parse((await readFile(f,'utf8')).replace(/^\uFEFF/,''));
const report={result:'RUNNING',checkedAtUtc:new Date().toISOString(),gameVersion:'1.1.0',authenticatedRequests:false,productionWrites:false,checks:{},responses:[],errors:[]};
async function check(name,fn){try{await fn();report.checks[name]=true;}catch(e){report.checks[name]=false;report.errors.push({name,error:e.message});}}
async function request(route,method='GET',limit=2*1024*1024){
  const url=new URL(route,origin);assert.equal(url.origin,origin);url.searchParams.set('presence_verify',String(Date.now()));
  const options={method,redirect:'manual',credentials:'omit',headers:{'Cache-Control':'no-cache','Accept-Encoding':'identity'},signal:AbortSignal.timeout(45000)};
  let r=await fetch(url,options);
  // Cloudflare canonicalizes index.html to its directory. Permit exactly the
  // observed same-origin game redirect, then hash the actual served HTML.
  if(route==='/game/index.html'&&r.status===307){
    const canonical=new URL(r.headers.get('location'),url);
    assert.equal(canonical.origin,origin);assert.equal(canonical.pathname,'/game/');assert.equal(canonical.search,url.search);assert.equal(canonical.hash,'');
    report.canonicalGameRedirect={status:r.status,from:route,to:canonical.pathname};
    await r.body?.cancel();r=await fetch(canonical,{...options,redirect:'error'});
  }
  const chunks=[];let size=0;for await(const chunk of r.body??[]){size+=chunk.length;assert(size<=limit,'Response exceeded bound');chunks.push(Buffer.from(chunk));}const b=Buffer.concat(chunks);report.responses.push({route,method,status:r.status,bytes:size,sha256:method==='GET'?sha(b):undefined});return {r,b,text:b.toString('utf8')};
}
const publication=await json(path.join(here,'publish-state.json'));
const manifest=await json(path.join(release,'lib/unity-assets-manifest.json'));
const frozen=await json(path.join(here,'source-validation.json'));
await check('deploymentSucceeded',async()=>{assert.equal(publication.deployment.status,'succeeded');assert(publication.saved.version_number>59);assert.equal(publication.saved.id,publication.deployment.version_id);assert.equal(publication.saved.source.commit_sha,publication.package.commit_sha);});
report.publication={versionId:publication.saved.id,versionNumber:publication.saved.version_number,commitSha:publication.package.commit_sha,deploymentId:publication.deployment.id};
await check('frozenServerSources',async()=>{for(const [f,h]of Object.entries(frozen.web))assert.equal(sha(await readFile(path.join(stage,f))),h,f);});
await check('homeAndUpdatedClientBootstrap',async()=>{const p=await request('/');assert.equal(p.r.status,200);assert.match(p.text,/블더스/);});
await check('anonymousSessionUnchanged',async()=>{const p=await request('/api/session');assert.equal(p.r.status,200);assert.equal(JSON.parse(p.text).authenticated,false);});
await check('presenceRequiresAccount',async()=>{const p=await request('/api/presence');assert.equal(p.r.status,401);assert.match(p.r.headers.get('cache-control'),/no-store/);assert.equal(JSON.parse(p.text).code,'unauthenticated');});
await check('gameIndexVersionAndPresenceScript',async()=>{
  const p=await request('/game/index.html');assert.equal(p.r.status,200);assert.match(p.text,/1\.1\.0/);assert.match(p.text,/TemplateData\/presence\.js/);
  const expected=manifest.files['/game/index.html'].sha256;
  if(sha(p.b)!==expected){
    const local=await readFile(path.join(release,'public/game/index.html'),'utf8');assert.equal(sha(Buffer.from(local)),expected);
    const at=local.lastIndexOf('</body>');assert(at>0);const prefix=local.slice(0,at),suffix=local.slice(at);
    assert(p.text.startsWith(prefix));assert(p.text.endsWith(suffix));
    const injected=p.text.slice(prefix.length,p.text.length-suffix.length);
    assert(injected.length<2048);assert(injected.startsWith('<script>(function(){function c(){var b=a.contentDocument'));
    assert(injected.endsWith('</script>'));assert.equal((injected.match(/<script>/g)??[]).length,1);
    assert(injected.includes("a.src='/cdn-cgi/challenge-platform/scripts/jsd/main.js'"));
    report.hostingHtmlInjection={provider:'Cloudflare JavaScript detection',bytes:Buffer.byteLength(injected),gameSourceSha256:sha(Buffer.from(prefix+suffix))};
  }else report.gameHtmlByteExact=true;
});
await check('gameBuildManifest',async()=>{const p=await request('/game/build-manifest.json');assert.equal(p.r.status,200);const v=JSON.parse(p.text);assert.equal(v.productVersion,'1.1.0');assert.equal(v.result,'Succeeded');assert.equal(v.errors,0);assert.equal(sha(p.b),manifest.files['/game/build-manifest.json'].sha256);});
await check('presenceClientBytes',async()=>{const p=await request('/game/TemplateData/presence.js');assert.equal(p.r.status,200);assert.equal(sha(p.b),manifest.files['/game/TemplateData/presence.js'].sha256);assert.equal(sha(p.b),frozen.product['Assets/WebGLTemplates/BlackTheSurvival/TemplateData/presence.js']);});
await check('uploadGateClosed',async()=>{const p=await request('/api/unity-upload');assert.equal(p.r.status,404);});
await check('dataHeadMatchesRelease',async()=>{const route=manifest.buildFiles.dataUrl,record=manifest.files[route],p=await request(route,'HEAD');assert.equal(p.r.status,200);assert.equal(Number(p.r.headers.get('content-length')),record.size);assert.equal(p.r.headers.get('etag'),'"'+record.sha256+'"');});
await check('wasmBytesMatchRelease',async()=>{const route=manifest.buildFiles.codeUrl,p=await request(route,'GET',128*1024*1024);assert.equal(p.r.status,200);assert.equal(sha(p.b),manifest.files[route].sha256);});
report.gameBuildId=manifest.buildId;report.gameManifestSha256=sha(await readFile(path.join(release,'lib/unity-assets-manifest.json')));
report.result=report.errors.length?'FAIL':'PASS';await writeFile(path.join(here,'live-validation.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({result:report.result,checks:Object.keys(report.checks).length,errors:report.errors,gameBuildId:report.gameBuildId}));if(report.errors.length)process.exitCode=1;
