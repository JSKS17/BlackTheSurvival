// Disposable loopback-only WebGL QA preview. --baseline reads immutable public assets only.
import http from 'node:http';
import path from 'node:path';
import { createReadStream } from 'node:fs';
import { readFile, readdir, stat, realpath } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import { Readable, Transform } from 'node:stream';
import { pipeline } from 'node:stream/promises';
import { fileURLToPath } from 'node:url';
import { createLocalPresenceBackend } from './LocalPresenceBackend.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const workspace = path.resolve(here, '../..');
const baseline = process.argv.includes('--baseline');
const gameRoot = path.join(workspace, baseline ? 'WebSite/public/game' : 'Build/Web128');
const fixtureRoot = path.join(here, 'fixtures');
const baselineManifest = baseline ? JSON.parse(await readFile(path.join(workspace, 'WebSite/lib/unity-assets-manifest.json'), 'utf8')) : null;
const baselinePublicOrigin = 'https://bser-sts.com';
if (baseline && (!baselineManifest?.ready || baselineManifest.buildId !== '00a3d6117a640e6b4da0d5d1' || createHash('sha256').update(await readFile(path.join(workspace, 'WebSite/lib/unity-assets-manifest.json'))).digest('hex') !== '3f089e686f037fb8210499e8e37e868ee28c0f3483fab56335f430cfcbaf699e')) throw new Error('Baseline must remain the exact deployed 1.0.27 build');
const host = '127.0.0.1', port = 8249;
const origin = 'http://' + host + ':' + port;
const presenceBackend = await createLocalPresenceBackend({ origin });
const gameVersion = baseline ? '1.0.27' : '1.0.28';
async function fixtureNames() {
  return (await readdir(fixtureRoot, { withFileTypes: true }))
    .filter(entry => entry.isFile() && /^[a-z][a-z0-9-]{0,63}\.json$/.test(entry.name))
    .map(entry => entry.name.slice(0, -5)).sort();
}
const contentTypes = { '.html':'text/html; charset=utf-8', '.js':'text/javascript; charset=utf-8', '.json':'application/json; charset=utf-8', '.css':'text/css; charset=utf-8', '.png':'image/png', '.gif':'image/gif', '.jpg':'image/jpeg', '.jpeg':'image/jpeg', '.webp':'image/webp', '.svg':'image/svg+xml', '.ico':'image/x-icon', '.ttf':'font/ttf', '.woff':'font/woff', '.woff2':'font/woff2', '.wasm':'application/wasm', '.unityweb':'application/octet-stream' };
const commonHeaders = { 'Cache-Control':'no-store', 'X-Content-Type-Options':'nosniff', 'Referrer-Policy':'no-referrer' };

function send(res, method, status, type, body, extra = {}) {
  const bytes = Buffer.from(body);
  res.writeHead(status, { ...commonHeaders, 'Content-Type':type, 'Content-Length':bytes.length, ...extra });
  res.end(method === 'HEAD' ? undefined : bytes);
}

async function containedFile(root, relative) {
  if (!relative || relative.includes('\\') || relative.includes('\0') || relative.split('/').some(part => part === '..' || part.startsWith('.'))) throw Object.assign(new Error('Invalid path'), {status:404});
  const absolute = path.resolve(root, ...relative.split('/'));
  const prefix = path.resolve(root).toLowerCase() + path.sep;
  if (!absolute.toLowerCase().startsWith(prefix)) throw Object.assign(new Error('Outside preview root'), {status:404});
  const resolvedRoot = await realpath(root);
  const resolvedFile = await realpath(absolute);
  if (!resolvedFile.toLowerCase().startsWith(resolvedRoot.toLowerCase() + path.sep)) throw Object.assign(new Error('Linked preview path'), {status:404});
  const info = await stat(resolvedFile);
  if (!info.isFile()) throw Object.assign(new Error('File required'), {status:404});
  return {file:resolvedFile, info};
}

async function streamFile(req, res, entry, forcedType) {
  const info = entry.info ?? await stat(entry.file);
  if (!info.isFile()) throw Object.assign(new Error('File required'), {status:404});
  const type = forcedType ?? contentTypes[path.extname(entry.file).toLowerCase()] ?? 'application/octet-stream';
  let start = 0, end = info.size - 1, status = 200;
  const headers = { ...commonHeaders, 'Content-Type':type, 'Accept-Ranges':'bytes' };
  if (req.headers.range) {
    const match = /^bytes=(\d*)-(\d*)$/.exec(req.headers.range);
    if (!match || (!match[1] && !match[2]) || info.size === 0) {
      send(res, req.method, 416, 'text/plain; charset=utf-8', 'Invalid byte range', {'Content-Range':`bytes */${info.size}`}); return;
    }
    if (!match[1]) { const suffix = Number(match[2]); start = Math.max(0, info.size - suffix); }
    else { start = Number(match[1]); if (match[2]) end = Math.min(end, Number(match[2])); }
    if (!Number.isSafeInteger(start) || !Number.isSafeInteger(end) || start < 0 || start > end || start >= info.size || (!match[1] && Number(match[2]) <= 0)) {
      send(res, req.method, 416, 'text/plain; charset=utf-8', 'Invalid byte range', {'Content-Range':`bytes */${info.size}`}); return;
    }
    status = 206; headers['Content-Range'] = `bytes ${start}-${end}/${info.size}`;
  }
  headers['Content-Length'] = info.size === 0 ? 0 : end - start + 1;
  res.writeHead(status, headers);
  if (req.method === 'HEAD' || info.size === 0) { res.end(); return; }
  const stream = createReadStream(entry.file, {start, end, highWaterMark:256*1024});
  res.on('close', () => stream.destroy());
  stream.on('error', () => res.destroy());
  stream.pipe(res);
}

// Baseline-only, read-only proxy of an exact immutable R2 asset declared by the
// canonical manifest. No cookies, tokens, account routes, redirects or disk cache.
async function streamBaselineR2(req, res, logicalPath, record) {
  if(!baseline || record?.storage!=='r2' || baselineManifest.files[logicalPath]!==record || !logicalPath.startsWith('/game/Build/')) throw new Error('Unlisted public baseline asset');
  if(!/^[a-f0-9]{64}$/.test(record.sha256) || !Number.isSafeInteger(record.size) || record.size<=0) throw new Error('Invalid immutable baseline record');
  if(req.headers.range){send(res,req.method,416,'text/plain; charset=utf-8','Baseline asset range not supported');return;}
  const url=new URL(logicalPath,baselinePublicOrigin);
  if(url.origin!==baselinePublicOrigin || url.pathname!==logicalPath || url.search) throw new Error('Invalid baseline asset URL');
  const abort=new AbortController();
  const timeout=setTimeout(()=>abort.abort(),300000);
  res.once('close',()=>abort.abort());
  try {
    const upstream=await fetch(url,{method:req.method,credentials:'omit',redirect:'error',signal:abort.signal,headers:{'Accept-Encoding':'identity'}});
    if(upstream.status!==200 || upstream.headers.get('Content-Encoding') || upstream.headers.get('Content-Length')!==String(record.size) || upstream.headers.get('ETag')!==('"'+record.sha256+'"')) throw new Error('Public baseline asset metadata mismatch');
    res.writeHead(200,{...commonHeaders,'Content-Type':record.contentType||'application/octet-stream','Content-Length':record.size,'ETag':'"'+record.sha256+'"','X-QA-Baseline-Public-Asset':'true'});
    if(req.method==='HEAD'){res.end();return;}
    if(!upstream.body)throw new Error('Missing baseline asset body');
    let size=0;const hash=createHash('sha256');
    const verify=new Transform({transform(chunk,encoding,done){size+=chunk.length;if(size>record.size){done(new Error('Oversized baseline asset'));return;}hash.update(chunk);done(null,chunk);},flush(done){done(size===record.size&&hash.digest('hex')===record.sha256?null:new Error('Baseline asset bytes mismatch'));}});
    await pipeline(Readable.fromWeb(upstream.body),verify,res);
  }finally{clearTimeout(timeout);}
}

// A detection model for this loopback QA harness only. The built HTML and
// production template remain unchanged, and ordinary index requests use the
// unmodified streamFile path below. Inject ahead of every product script.
async function streamMobileDetectionModel(req, res) {
  const entry = await containedFile(gameRoot, 'index.html');
  if (entry.info.size > 1024 * 1024) throw Object.assign(new Error('Oversized game HTML'), {status:413});
  const original = await readFile(entry.file, 'utf8');
  const head = /<head(?:\s[^>]*)?>/i;
  if (!head.test(original)) throw new Error('Game HTML has no head for the QA detection model');
  const model = `<script data-qa-mobile-detection-model>(()=>{const chrome=(navigator.userAgent.match(/Chrome\\/[\\d.]+/)||['Chrome/130.0.0.0'])[0];const ua='Mozilla/5.0 (Linux; Android 14; QA Mobile) AppleWebKit/537.36 (KHTML, like Gecko) '+chrome+' Mobile Safari/537.36';Object.defineProperties(navigator,{userAgentData:{configurable:true,value:{mobile:true}},userAgent:{configurable:true,value:ua},appVersion:{configurable:true,value:ua.slice(8)},maxTouchPoints:{configurable:true,value:5}});if(!('ontouchstart' in window))window.ontouchstart=null;})();</script>`;
  send(res, req.method, 200, 'text/html; charset=utf-8', original.replace(head, opening => opening + model));
}


async function qaHtml() {
  const names = await fixtureNames();
  const labels = { 'presence-lobby': '접속자 표시 로비', 'wickeline-map': '접속자 표시 지도', research: '연구 기록' };
  const options = names.map(name => '<option value="' + name + '">' + (labels[name] ?? name) + '</option>').join('');
  return `<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>접속자 표시 WebGL ${gameVersion} 로컬 검증</title><style>
*{box-sizing:border-box}body{margin:0;background:#0b1420;color:#d7e4ee;font:13px system-ui,sans-serif}.bar{min-height:72px;padding:10px 16px;display:flex;align-items:center;gap:12px;flex-wrap:wrap}select,button{background:#192e40;border:1px solid #456174;color:#edf3f7;border-radius:4px;padding:7px 10px}button{cursor:pointer}#status{color:#8bdec5}#status[data-error=true]{color:#ff9b98}#game{height:calc(100vh - var(--qa-toolbar-height,72px))}iframe{display:block;border:0;width:100%;height:100%;background:#02060d}small{color:#9eb0c1}
</style><div class="bar"><strong>접속자 표시 · 로컬 QA / ${gameVersion}${baseline ? ' / 기준 버전' : ''}</strong><label>장면 <select id="fixture">${options}</select></label><button id="reload" type="button">로컬 QA 기록 다시 불러오기</button><button id="audit" type="button">검증 저장 요약</button><button id="second-start" type="button">두 번째 로컬 접속 시작</button><button id="second-stop" type="button">두 번째 로컬 접속 종료</button><span id="presence-status" role="status">실제 로컬 D1 접속 집계 대기</span><span id="status" role="status">준비 중</span><small>로컬 검증 기록 전용 · 숫자는 실제 D1 응답이며 게임 표시는 정상 30초 갱신을 기다리세요.</small></div><div id="game"></div><pre id="audit-output" hidden style="position:fixed;right:12px;bottom:12px;max-height:220px;overflow:auto;z-index:9;background:#101d2f;padding:12px;border:1px solid #89dfcf;font:14px monospace"></pre><script>
'use strict';
const names=new Set(${JSON.stringify(names)}),gameVersion=${JSON.stringify(gameVersion)};
const mobileDetectionModel=new URLSearchParams(location.search).get('mobile')==='1';
const select=document.querySelector('#fixture'),status=document.querySelector('#status'),holder=document.querySelector('#game');
let frame=null,generation=0,accountId='',bundleJson='',bundleHash='',booting=false,gameReady=false,sequence=0,revision=1,qaSession=null;
const waiting=new Map(); // Exports return bts:local; other commands return bts:result.
function show(text,error=false){status.textContent=(mobileDetectionModel?'[모바일 감지 모형] ':'')+text;status.dataset.error=String(error);}
function assertCurrent(value){if(value!==generation)throw new Error('이전 QA 장면 요청이 종료됐습니다.');}
function post(type,payload={}){if(frame)frame.contentWindow.postMessage({type,...payload},location.origin);}
function postPresence(){post('bts:presence-session',gameReady&&qaSession?{active:true,accountId:qaSession.user.id,csrfToken:qaSession.csrfToken}:{active:false});}
async function bootstrapSession(){const response=await fetch('/qa-presence/bootstrap',{credentials:'same-origin',cache:'no-store'});if(!response.ok)throw new Error('실제 로컬 QA 세션을 만들지 못했습니다.');const value=await response.json();if(!value.authenticated||!value.user||!/^[-a-f0-9]{36}$/.test(value.user.id)||!/^[-a-f0-9]{64}$/.test(value.csrfToken))throw new Error('로컬 QA 세션 형식이 올바르지 않습니다.');return value;}
function command(type,payload={}){const own=generation,requestId='qa-'+(++sequence);return new Promise((resolve,reject)=>{const timer=setTimeout(()=>{waiting.delete(requestId);reject(new Error('로컬 저장 브리지 응답 시간 초과'));},30000);waiting.set(requestId,{own,resolve,reject,timer});post(type,{...payload,requestId});});}
async function fixtureJson(name){const response=await fetch('/fixtures/'+name+'.json',{cache:'no-store',credentials:'omit'});if(!response.ok)throw new Error('검증용 fixture가 아직 없습니다: '+name);const text=await response.text();if(new TextEncoder().encode(text).length>4*1024*1024)throw new Error('Fixture가 너무 큽니다.');return {text,value:JSON.parse(text)};}
async function reload(){const own=++generation;booting=false;gameReady=false;postPresence();for(const p of waiting.values()){clearTimeout(p.timer);p.reject(new Error('QA 장면 교체'));}waiting.clear();if(frame){frame.src='about:blank';frame.remove();frame=null;}show('검증 기록을 읽는 중');const name=select.value;try{if(!names.has(name))throw new Error('허용되지 않은 장면');if(!names.has('research'))throw new Error('fixtures/research.json이 필요합니다.');qaSession=await bootstrapSession();assertCurrent(own);accountId=qaSession.user.id;const profile=await fixtureJson('research');assertCurrent(own);const run=name==='research'?null:await fixtureJson(name);assertCurrent(own);const bundle={schemaVersion:1,gameVersion,run:run?run.text:null,research:profile.text,pending:[],tutorialSeen:true,settings:{volume:0,musicVolume:0,effectsVolume:0,sound:false,reduceMotion:!new URLSearchParams(location.search).has('motion'),fullScreen:false}};bundleJson=JSON.stringify(bundle);if(new TextEncoder().encode(bundleJson).length>4*1024*1024)throw new Error('검증 저장 묶음이 너무 큽니다.');bundleHash=[...new Uint8Array(await crypto.subtle.digest('SHA-256',new TextEncoder().encode(bundleJson)))].map(v=>v.toString(16).padStart(2,'0')).join('');assertCurrent(own);const revisionKey='bts:local-qa-import-revision:'+accountId;const previous=Number(localStorage.getItem(revisionKey)||0);revision=Math.max(Date.now(),Number.isSafeInteger(previous)?previous+1:1);localStorage.setItem(revisionKey,String(revision));frame=document.createElement('iframe');frame.title='접속자 표시 검증용 게임';frame.allow='autoplay; fullscreen';frame.allowFullscreen=true;frame.src='/qa/game/index.html'+(mobileDetectionModel?'?mobile=1':'');holder.append(frame);show('Unity 게임을 불러오는 중');}catch(error){if(own===generation)show(error.message,true);}}
async function ready(){if(booting)return;booting=true;const own=generation;try{show('검증 기록을 적용하는 중');await command('bts:import',{bundleJson,revision,sha256:bundleHash});assertCurrent(own);await command('bts:ack',{revision,sha256:bundleHash});assertCurrent(own);await command('bts:resume');assertCurrent(own);gameReady=true;postPresence();show(select.value==='research'?'준비 완료 · 실제 로비 접속자 표시를 확인하세요.':'준비 완료 · 로비에서 이어하기를 누르세요.');}catch(error){if(own===generation){gameReady=false;postPresence();show(error.message,true);}}}
window.addEventListener('message',event=>{if(!frame||event.source!==frame.contentWindow||event.origin!==location.origin||!event.data||typeof event.data!=='object')return;const message=event.data;if(message.type==='bts:hello'){post('bts:bootstrap',{accountId});return;}if(message.type==='bts:presence-ready'){postPresence();return;}if(message.type==='bts:presence-refresh'){void bootstrapSession().then(value=>{qaSession=value;postPresence();}).catch(error=>show(error.message,true));return;}if(message.type==='bts:ready'){void ready();return;}if(message.type==='bts:load-progress'){show('Unity 로딩 '+Number(message.progress||0)+'%');return;}if(message.type==='bts:error'||message.type==='bts:blocked'||message.type==='bts:load-error'){gameReady=false;postPresence();show(message.error||'로컬 게임 오류',true);return;}if(message.type==='bts:dirty'){return;}const pending=waiting.get(message.requestId);if(!pending||pending.own!==generation||!['bts:result','bts:local'].includes(message.type))return;waiting.delete(message.requestId);clearTimeout(pending.timer);if((message.ok||message.type==='bts:local')&&message.localPersisted)pending.resolve(message);else pending.reject(new Error(message.error||'로컬 기록 보관 실패'));});
document.querySelector('#audit').addEventListener('click',async()=>{try{const exported=await command('bts:export');const bundle=JSON.parse(exported.bundleJson),run=bundle.run?JSON.parse(bundle.run):null;const node=run?.map?.find(n=>n.id===run.activeNodeId);const output=document.querySelector('#audit-output');output.textContent=JSON.stringify({stage:run?['Preparation','Map','Combat','Rewards','Kiosk','Campfire','Encounter','PassiveChoice','Won','Lost','AugmentChoice','EquipmentSlotChoice','LumiOffer','LumiShop'][run.stage]:null,credits:run?.credits,objects:run?.objects,activeNodeId:run?.activeNodeId,wickeline:node?.wickeline,wickelineDefeated:node?.wickelineDefeated,combatPresent:Boolean(run?.combat)},null,2);output.hidden=false;}catch(error){show(error.message,true);}});
let fitQueued=false;function fit(){const next=Math.ceil(document.querySelector('.bar').getBoundingClientRect().height)+'px';if(document.documentElement.style.getPropertyValue('--qa-toolbar-height')!==next)document.documentElement.style.setProperty('--qa-toolbar-height',next);fitQueued=false;}
new ResizeObserver(()=>{if(!fitQueued){fitQueued=true;requestAnimationFrame(fit);}}).observe(document.querySelector('.bar'));fit();
async function second(action){const output=document.querySelector('#presence-status');try{const response=await fetch('/qa-presence/second',{method:'POST',credentials:'same-origin',cache:'no-store',headers:{'Content-Type':'application/json'},body:JSON.stringify({action})});const value=await response.json();if(!response.ok||!Number.isSafeInteger(value.count))throw new Error('실제 로컬 D1 집계를 읽지 못했습니다.');output.textContent='실제 API 집계 '+value.count+'명 · 게임의 30초 갱신 대기';}catch(error){output.textContent=error.message;}}
document.querySelector('#second-start').addEventListener('click',()=>void second('start'));document.querySelector('#second-stop').addEventListener('click',()=>void second('stop'));window.addEventListener('pagehide',()=>{gameReady=false;postPresence();});
document.querySelector('#reload').addEventListener('click',()=>void reload());select.addEventListener('change',()=>{const u=new URL(location.href);u.searchParams.set('fixture',select.value);history.replaceState(null,'',u);void reload();});const requested=new URLSearchParams(location.search).get('fixture');if(names.has(requested))select.value=requested;else if(names.has('presence-lobby'))select.value='presence-lobby';if(names.size)void reload();else show('fixtures 폴더에 검증 기록을 추가한 뒤 새로고침하세요.',true);
</script></html>`;
}

async function boundedBody(req, limit = 1024) {
  const declared = req.headers['content-length'];
  if (declared !== undefined && (!/^\d+$/.test(declared) || Number(declared) > limit)) {
    req.resume(); throw Object.assign(new Error('Oversized QA API request'), {status:413});
  }
  return await new Promise((resolve, reject) => {
    let bytes = 0, oversized = false;
    const chunks = [];
    req.on('data', chunk => { bytes += chunk.length;if(bytes > limit){oversized=true;chunks.length=0;}else if(!oversized)chunks.push(chunk); });
    req.once('end', () => oversized ? reject(Object.assign(new Error('Oversized QA API request'), {status:413})) : resolve(Buffer.concat(chunks)));
    req.once('error', reject);
    req.once('aborted', () => reject(Object.assign(new Error('Aborted QA API request'), {status:400})));
  });
}

async function serveLocalApi(req, res) {
  const headers = new Headers();
  for (const [name,value] of Object.entries(req.headers)) if (value !== undefined) headers.set(name, Array.isArray(value) ? value.join(', ') : value);
  const body = ['GET','HEAD'].includes(req.method) ? undefined : await boundedBody(req);
  const response = await presenceBackend.handle(new Request(origin + req.url, {method:req.method, headers, ...(body ? {body} : {})}));
  if (!response) return false;
  const forwarded = Object.fromEntries(Object.entries(commonHeaders).map(([name,value]) => [name.toLowerCase(),value]));
  const transportHeaders = new Set(['set-cookie','content-length','transfer-encoding','connection','keep-alive','trailer','upgrade','proxy-authenticate','proxy-authorization','te']);
  for (const [name,value] of response.headers) if (!transportHeaders.has(name.toLowerCase())) forwarded[name] = value;
  const cookies = response.headers.getSetCookie?.() ?? (response.headers.has('set-cookie') ? [response.headers.get('set-cookie')] : []);
  if (cookies.length) forwarded['set-cookie'] = cookies;
  const bytes = Buffer.from(await response.arrayBuffer());
  res.writeHead(response.status, {...forwarded,'content-length':bytes.length});
  res.end(req.method === 'HEAD' ? undefined : bytes);
  return true;
}

export const server = http.createServer(async (req, res) => {
  try {
    if (req.headers.host !== `${host}:${port}`) { send(res, req.method, 421, 'text/plain; charset=utf-8', 'Fixed loopback host required'); return; }
    if (req.headers.origin !== undefined && req.headers.origin !== origin) { send(res, req.method, 403, 'text/plain; charset=utf-8', 'Same-origin preview request required'); return; }
    const rawPath = (req.url ?? '/').split('?')[0];
    let pathname; try { pathname=decodeURIComponent(rawPath); } catch { throw Object.assign(new Error('Bad URL'),{status:400}); }
    if (pathname.includes('\\') || pathname.includes('\0') || pathname.split('/').some(part=>part==='..')) throw Object.assign(new Error('Bad path'),{status:404});
    if (['/api/presence','/api/session','/qa-presence/bootstrap','/qa-presence/second'].includes(pathname)) {
      if (await serveLocalApi(req,res)) return;
    }
    if (!['GET','HEAD'].includes(req.method)) { send(res, req.method, 405, 'text/plain; charset=utf-8', 'Read-only asset preview', {Allow:'GET, HEAD'}); return; }
    if (['/','/qa','/qa/'].includes(pathname)) { send(res, req.method, 200, 'text/html; charset=utf-8', await qaHtml()); return; }
    const fixture=/^\/fixtures\/([a-z][a-z0-9-]{0,63})\.json$/.exec(pathname);
    if (fixture && (await fixtureNames()).includes(fixture[1])) {
      const entry=await containedFile(fixtureRoot, fixture[1]+'.json');
      if(entry.info.size>4*1024*1024)throw Object.assign(new Error('Oversized fixture'),{status:413});
      await streamFile(req,res,entry);return;
    }
    if (pathname === '/qa/game/index.html' && new URL(req.url, 'http://' + host + ':' + port).searchParams.get('mobile') === '1') {
      await streamMobileDetectionModel(req, res); return;
    }
    if (pathname.startsWith('/qa/game/')) {
      const relative=pathname.slice('/qa/game/'.length),logicalPath='/game/'+relative;
      if(baseline && baselineManifest.files[logicalPath]?.storage==='r2'){
        await streamBaselineR2(req,res,logicalPath,baselineManifest.files[logicalPath]);return;
      }
      await streamFile(req,res,await containedFile(gameRoot,relative));return;
    }
    send(res, req.method, 404, 'text/plain; charset=utf-8', 'Unknown preview route');
  } catch (error) {
    if (res.headersSent) { res.destroy(); return; }
    const status=error.status??(['ENOENT','ENOTDIR'].includes(error.code)?404:500);
    send(res,req.method,status,'text/plain; charset=utf-8',status===500?'Preview read failed':status===404?'Preview file not available':'Preview request rejected');
  }
});
server.requestTimeout=30000;server.headersTimeout=10000;
export const previewReady = new Promise((resolve,reject) => {
  server.once('error',reject);
  server.listen(port,host,()=>{console.log(JSON.stringify({ready:true,origin,qa:'/qa',gameVersion,gameRoot,fixtureRoot,localOnly:true,productionRequests:baseline,readOnlyBaseline:baseline,remoteAssetOrigin:baseline?baselinePublicOrigin:null,diskCaching:false,presenceBackend:'disposable real Miniflare D1',mobileModelLimit:'Mobile browser detection is modeled; physical mobile hardware is not emulated'}));resolve();});
});
let closing;
export function closePreview(){
  if(closing)return closing;
  closing=(async()=>{const timeout=setTimeout(()=>server.closeAllConnections?.(),15000);timeout.unref();try{await Promise.all([new Promise((resolve,reject)=>server.close(error=>error&&error.code!=='ERR_SERVER_NOT_RUNNING'?reject(error):resolve())),presenceBackend.close()]);}finally{clearTimeout(timeout);}})();
  return closing;
}
for(const signal of ['SIGINT','SIGTERM'])process.once(signal,()=>{void closePreview().then(()=>process.exit(0),()=>process.exit(1));});
