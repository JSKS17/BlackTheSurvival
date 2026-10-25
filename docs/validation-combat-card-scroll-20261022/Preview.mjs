// Disposable loopback-only art / WebGL QA preview. No production requests or auth.
import http from 'node:http';
import path from 'node:path';
import { createReadStream } from 'node:fs';
import { readFile, stat, realpath } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import { Readable, Transform } from 'node:stream';
import { pipeline } from 'node:stream/promises';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const workspace = path.resolve(here, '../..');
const baseline = process.argv.includes('--baseline');
const gameRoot = path.join(workspace, baseline ? 'WebSite/public/game' : process.argv.includes('--trace') ? 'Build/WebTouchTrace' : 'Build/Web124');
const fixtureRoot = baseline ? path.join(workspace, 'docs/validation-mobile-fonts-20261022/fixtures') : path.join(here, 'fixtures');
const baselineManifest = baseline ? JSON.parse(await readFile(path.join(workspace, 'WebSite/lib/unity-assets-manifest.json'), 'utf8')) : null;
const baselinePublicOrigin = 'https://bser-sts.com';
if (baseline && (!baselineManifest?.ready || baselineManifest.buildId !== '39d2b2a4f91df8ba79967cdb')) throw new Error('Baseline must remain the exact deployed 1.0.23 build');
const host = '127.0.0.1', port = 8245;
const fixtureNames = new Set(['lumi-offer', 'lumi-shop', 'lumi-combat', 'lumi-rare', 'nia-rare-combat', 'research', 'combat-touch-wide']);
if(baseline)fixtureNames.delete('combat-touch-wide');
const art = new Map([
  ['/art/lumi.png', path.join(workspace, 'Assets/Resources/Lumia/Portraits/lumi.png')],
  ['/art/virtual-lumi.png', path.join(workspace, 'Assets/Resources/Lumia/PortraitsRare/lumi.png')],
  ['/art/telescope-camera.png', path.join(workspace, 'Assets/Resources/Lumia/SystemIcons/telescope_camera.png')],
  ['/art/portable-kiosk.png', path.join(workspace, 'Assets/Resources/Lumia/SystemIcons/portable_kiosk.png')],
  ['/art/font.ttf', path.join(workspace, 'Assets/Resources/Lumia/Galmuri11.ttf')],
]);
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
  const model = `<script data-qa-mobile-detection-model>(()=>{const chrome=(navigator.userAgent.match(/Chrome\\/[\\d.]+/)||['Chrome/130.0.0.0'])[0];const ua='Mozilla/5.0 (Linux; Android 14; QA Mobile) AppleWebKit/537.36 (KHTML, like Gecko) '+chrome+' Mobile Safari/537.36';Object.defineProperties(navigator,{userAgentData:{configurable:true,value:{mobile:true}},userAgent:{configurable:true,value:ua},appVersion:{configurable:true,value:ua.slice(8)},maxTouchPoints:{configurable:true,value:5}});if(!('ontouchstart' in window))window.ontouchstart=null;})();</script>` + qaTouchModelScript;
  send(res, req.method, 200, 'text/html; charset=utf-8', original.replace(head, opening => opening + model));
}

// Explicit, visible browser-TouchEvent QA controls. They drive the real WebGL
// canvas listeners, but untrusted synthetic events do not prove physical-phone
// input. This script is injected only by the loopback mobile-model route above.
const qaTouchModelScript = `<script data-qa-synthetic-browser-touch>
(() => {
  'use strict';
  const presets = {
    'description-up': [[115,655],[115,578]],
    'description-down': [[115,578],[115,655]],
    'hand-left': [[875,620],[385,620]],
    'hand-right': [[385,620],[875,620]],
    'first-card-tap': [[115,615],[115,615]],
  };
  let busy=false, identifier=7000, total=0;
  const frames = count => new Promise(resolve => {
    const next=()=>{if(--count<=0)resolve();else requestAnimationFrame(next);};
    requestAnimationFrame(next);
  });
  const reply = value => window.parent.postMessage({type:'bts:qa-touch-result',...value},location.origin);
  function mapPoint(canvas, logical) {
    const box=canvas.getBoundingClientRect();
    const width=canvas.width,height=canvas.height;
    if(width<=0||height<=0||box.width<=0||box.height<=0)throw new Error('Canvas has no visible dimensions');
    const scale=Math.min(width/1280,height/720);
    return {clientX:box.left+((width-1280*scale)/2+logical[0]*scale)*box.width/width,
      clientY:box.top+((height-720*scale)/2+logical[1]*scale)*box.height/height};
  }
  window.addEventListener('message',async event=>{
    if(event.source!==window.parent||event.origin!==location.origin||event.data?.type!=='bts:qa-touch')return;
    const {requestId,action}=event.data;
    if(!Object.hasOwn(presets,action)){reply({requestId,ok:false,error:'Unknown touch preset'});return;}
    if(busy){reply({requestId,ok:false,error:'Another synthetic gesture is running'});return;}
    busy=true;
    let sent=0,canceled=0,last=null,canvas=null;
    const touchId=++identifier;
    try {
      if(typeof Touch!=='function'||typeof TouchEvent!=='function')throw new Error('Touch/TouchEvent constructors unavailable');
      canvas=document.querySelector('#unity-canvas');
      if(!canvas||window.blackTheSurvival?.status!=='ready')throw new Error('Unity canvas is not ready');
      const box=canvas.getBoundingClientRect();
      const [from,to]=presets[action],start=mapPoint(canvas,from),end=mapPoint(canvas,to);
      const steps=action==='first-card-tap'?0:12;
      canvas.focus({preventScroll:true});
      const emit=(type,point)=>{
        const touch=new Touch({identifier:touchId,target:canvas,clientX:point.clientX,clientY:point.clientY,
          pageX:point.clientX+scrollX,pageY:point.clientY+scrollY,
          screenX:point.clientX+window.screenX,screenY:point.clientY+window.screenY,
          radiusX:1,radiusY:1,rotationAngle:0,force:type==='touchend'||type==='touchcancel'?0:.5});
        const ended=type==='touchend'||type==='touchcancel';
        const native=new TouchEvent(type,{bubbles:true,cancelable:true,composed:true,view:window,
          touches:ended?[]:[touch],targetTouches:ended?[]:[touch],changedTouches:[touch]});
        if(!canvas.dispatchEvent(native))canceled++;
        sent++;total++;last=point;
      };
      emit('touchstart',start);
      await frames(3);
      for(let index=1;index<=steps;index++){
        if(!canvas.isConnected)throw new Error('QA canvas was replaced during gesture');
        const fraction=index/steps;
        emit('touchmove',{clientX:start.clientX+(end.clientX-start.clientX)*fraction,
          clientY:start.clientY+(end.clientY-start.clientY)*fraction});
        await frames(2);
      }
      emit('touchend',end);
      await frames(3);
      reply({requestId,action,ok:true,events:sent,totalEvents:total,preventedEvents:canceled,
        isTrusted:false,limit:'Synthetic browser TouchEvents; physical mobile hardware is not emulated',
        logicalFrom:from,logicalTo:to,clientFrom:start,clientTo:end,
        canvas:{width:canvas.width,height:canvas.height,clientWidth:box.width,clientHeight:box.height}});
    }catch(error){
      if(canvas?.isConnected&&last){
        try{const touch=new Touch({identifier:touchId,target:canvas,clientX:last.clientX,clientY:last.clientY});
          canvas.dispatchEvent(new TouchEvent('touchcancel',{bubbles:true,cancelable:true,touches:[],targetTouches:[],changedTouches:[touch]}));
          sent++;total++;}catch{}
      }
      reply({requestId,action,ok:false,events:sent,totalEvents:total,isTrusted:false,error:error.message});
    }finally{busy=false;}
  });
})();
</script>`;

const artHtml = `<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>루미 · 새 아트 미리보기</title><style>
@font-face{font-family:Galmuri;src:url('/art/font.ttf')}*{box-sizing:border-box}html,body{margin:0;min-height:720px;background:#0b1420;color:#e6ebef;font-family:Galmuri,monospace}body{background-image:linear-gradient(#21334b25 1px,transparent 1px),linear-gradient(90deg,#21334b25 1px,transparent 1px);background-size:28px 28px}.wrap{max-width:1160px;margin:auto;padding:27px 26px 24px}.eyebrow{font-size:11px;letter-spacing:3px;color:#6fdfcf;margin:0 0 9px}header{display:flex;justify-content:space-between;align-items:end;margin-bottom:20px}h1{margin:0;font-size:28px;color:#f0e0ae;font-weight:normal}header p{font-size:12px;color:#9daebf;margin:0 0 3px}.grid{display:grid;grid-template-columns:1fr 1fr;gap:16px}.card{position:relative;border:1px solid #385069;background:#132134e8;box-shadow:inset 0 0 0 3px #0c1624;overflow:hidden}.portrait{height:378px;display:grid;grid-template-rows:1fr 61px;justify-items:center}.sprite{height:310px;width:310px;object-fit:contain;image-rendering:pixelated;align-self:center}.label{align-self:stretch;width:100%;background:#0c1726;border-top:1px solid #30445b;padding:12px 20px}.label strong{font-weight:normal;font-size:18px}.label small{display:block;color:#95a5b8;margin-top:5px;font-size:11px}.item{height:143px;display:flex;align-items:center;padding:10px 25px;gap:25px}.item img{width:119px;height:119px;object-fit:contain;image-rendering:pixelated}.item strong{font-size:18px;font-weight:normal}.item p{font-size:12px;color:#9daebf;margin:8px 0 0}.price{display:block;color:#f4d57a;margin-top:12px;font-size:13px}footer{font-size:10px;color:#7d91a5;margin-top:13px;text-align:right}
</style><main class="wrap"><header><div><p class="eyebrow">BLACK THE SURVIVAL / LUMI</p><h1>루미 · 도트 아트 미리보기</h1></div><p>원작 디자인 참조 · 기존 도트 스타일 · 투명 배경</p></header><section class="grid"><article class="card portrait"><img class="sprite" src="/art/lumi.png" alt="일반 루미 안내 로봇"><div class="label"><strong>루미</strong><small>전투 후 찾아오는 안내 로봇</small></div></article><article class="card portrait"><img class="sprite" src="/art/virtual-lumi.png" alt="버추얼 루미"><div class="label"><strong>버추얼 루미</strong><small>루미 연구 보고서 · 레어 조우 모습</small></div></article><article class="card item"><img src="/art/telescope-camera.png" alt="망원 카메라"><div><strong>망원 카메라</strong><p>사용하면 보안 카메라와 같은 효과</p><span class="price">20 크레딧</span></div></article><article class="card item"><img src="/art/portable-kiosk.png" alt="휴대용 키오스크"><div><strong>휴대용 키오스크</strong><p>사용하면 키오스크 이용</p><span class="price">100 크레딧</span></div></article></section><footer>루미 스프라이트 2종 · 80px 도트 보정 · 기존 도구 아이콘 2종</footer></main></html>`;

const qaHtml = `<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>전투 카드 터치 스크롤 WebGL 1.0.24 로컬 검증</title><style>*{box-sizing:border-box}body{margin:0;background:#0b1420;color:#d7e4ee;font:13px system-ui,sans-serif}.bar{height:72px;padding:10px 16px;display:flex;align-items:center;gap:12px;flex-wrap:wrap}select,button{background:#192e40;border:1px solid #456174;color:#edf3f7;border-radius:4px;padding:7px 10px}button{cursor:pointer}#status{color:#8bdec5}#status[data-error=true]{color:#ff9b98}iframe{display:block;border:0;width:100vw;height:calc(100vh - 72px);background:#02060d}small{color:#9eb0c1}a{color:#a1e2d2}</style><div class="bar"><strong>전투 카드 터치 스크롤 · 로컬 QA / 1.0.24</strong><label>장면 <select id="fixture"><option value="lumi-offer">루미 발견</option><option value="lumi-shop">루미 상점</option><option value="lumi-combat">루미 3턴 전투</option><option value="lumi-rare">버추얼 루미 조우</option><option value="nia-rare-combat">니아 레어 외형 전투</option><option value="research">보고서·외형 설정</option></select></label><button id="reload" type="button">새 QA 계정으로 불러오기</button><a href="/art">아트 미리보기</a><span id="status" role="status">준비 중</span><small>로컬 전용 · 운영 계정/서버와 연결하지 않습니다. 게임 로비의 ‘이어하기’로 장면에 진입하세요.</small></div><div id="game"></div><script>
'use strict';
const names=new Set(['lumi-offer','lumi-shop','lumi-combat','lumi-rare','nia-rare-combat','research','combat-touch-wide']);
if(${baseline})names.delete('combat-touch-wide');
const mobileDetectionModel=new URLSearchParams(location.search).get('mobile')==='1';
const select=document.querySelector('#fixture'),status=document.querySelector('#status'),holder=document.querySelector('#game');
let frame=null,generation=0,accountId='',bundleJson='',bundleHash='',booting=false,sequence=0,revision=1;
const waiting=new Map();
function show(text,error=false){status.textContent=(mobileDetectionModel?'[모바일 감지 모형] ':'')+text;status.dataset.error=String(error);}
function assertCurrent(value){if(value!==generation)throw new Error('이전 QA 장면 요청이 종료됐습니다.');}
function post(type,payload={}){if(frame)frame.contentWindow.postMessage({type,...payload},location.origin);}
function command(type,payload={}){const own=generation,requestId='qa-'+(++sequence);return new Promise((resolve,reject)=>{const timer=setTimeout(()=>{waiting.delete(requestId);reject(new Error('로컬 저장 브리지 응답 시간 초과'));},30000);waiting.set(requestId,{own,resolve,reject,timer});post(type,{...payload,requestId});});}
async function fixtureJson(name){const response=await fetch('/fixtures/'+name+'.json',{cache:'no-store',credentials:'omit'});if(!response.ok)throw new Error('검증용 fixture가 아직 없습니다: '+name);const text=await response.text();if(new TextEncoder().encode(text).length>4*1024*1024)throw new Error('Fixture가 너무 큽니다.');return {text,value:JSON.parse(text)};}
async function reload(){const own=++generation;booting=false;for(const p of waiting.values()){clearTimeout(p.timer);p.reject(new Error('QA 장면 교체'));}waiting.clear();if(frame){frame.src='about:blank';frame.remove();frame=null;}show('검증 기록을 읽는 중');const name=select.value;try{if(!names.has(name))throw new Error('허용되지 않은 장면');const profile=await fixtureJson('research');assertCurrent(own);const run=name==='research'?null:await fixtureJson(name);assertCurrent(own);const bundle={schemaVersion:1,gameVersion:'1.0.24',run:run?run.text:null,research:profile.text,pending:[],tutorialSeen:true,settings:{volume:0,musicVolume:0,effectsVolume:0,sound:false,reduceMotion:true,fullScreen:false}};bundleJson=JSON.stringify(bundle);if(new TextEncoder().encode(bundleJson).length>4*1024*1024)throw new Error('검증 저장 묶음이 너무 큽니다.');bundleHash=[...new Uint8Array(await crypto.subtle.digest('SHA-256',new TextEncoder().encode(bundleJson)))].map(v=>v.toString(16).padStart(2,'0')).join('');assertCurrent(own);accountId=crypto.randomUUID();revision=1;frame=document.createElement('iframe');frame.title='전투 카드 터치 스크롤 검증용 게임';frame.allow='autoplay; fullscreen';frame.allowFullscreen=true;frame.src='/qa/game/index.html'+(mobileDetectionModel?'?mobile=1':'');holder.append(frame);show('Unity 게임을 불러오는 중');}catch(error){if(own===generation)show(error.message,true);}}
async function ready(){if(booting)return;booting=true;const own=generation;try{show('검증 기록을 적용하는 중');await command('bts:import',{bundleJson,revision,sha256:bundleHash});assertCurrent(own);await command('bts:ack',{revision,sha256:bundleHash});assertCurrent(own);await command('bts:resume');assertCurrent(own);show(select.value==='research'?'준비 완료 · 연구 기록 메뉴를 여세요.':'준비 완료 · 로비에서 이어하기를 누르세요.');}catch(error){if(own===generation)show(error.message,true);}}
window.addEventListener('message',event=>{if(!frame||event.source!==frame.contentWindow||event.origin!==location.origin||!event.data||typeof event.data!=='object')return;const message=event.data;if(message.type==='bts:hello'){post('bts:bootstrap',{accountId});return;}if(message.type==='bts:ready'){void ready();return;}if(message.type==='bts:load-progress'){show('Unity 로딩 '+Number(message.progress||0)+'%');return;}if(message.type==='bts:error'||message.type==='bts:blocked'){show(message.error||'로컬 게임 오류',true);return;}if(message.type==='bts:dirty'){return;}const pending=waiting.get(message.requestId);if(!pending||pending.own!==generation||message.type!=='bts:result')return;waiting.delete(message.requestId);clearTimeout(pending.timer);if(message.ok&&message.localPersisted)pending.resolve(message);else pending.reject(new Error(message.error||'로컬 기록 보관 실패'));});
document.querySelector('#reload').addEventListener('click',()=>void reload());select.addEventListener('change',()=>{const u=new URL(location.href);u.searchParams.set('fixture',select.value);history.replaceState(null,'',u);void reload();});const requested=new URLSearchParams(location.search).get('fixture');if(names.has(requested))select.value=requested;void reload();
</script></html>`;

const qaTouchControlsHtml = `<div class="touchbar" id="touch-controls" hidden><strong>합성 브라우저 터치</strong><button type="button" data-touch="description-up">첫 카드 설명 위로</button><button type="button" data-touch="description-down">첫 카드 설명 아래로</button><button type="button" data-touch="hand-left">손패 왼쪽으로</button><button type="button" data-touch="hand-right">손패 오른쪽으로</button><button type="button" data-touch="first-card-tap">첫 카드 탭</button><button type="button" id="touch-reset">QA 장면 초기화</button><span id="touch-status" role="status">터치 이벤트 0개</span><small>실제 Unity canvas에 TouchEvents를 전송합니다. 물리 모바일 하드웨어를 재현하지는 않습니다.</small><details><summary>최근 이벤트 정보</summary><pre id="touch-result"></pre></details></div>`;
const qaTouchControlsScript = `<script>
(() => {
  const bar=document.querySelector('#touch-controls'),state=document.querySelector('#touch-status'),output=document.querySelector('#touch-result');
  bar.hidden=!mobileDetectionModel;
  const sizeFrame=()=>document.documentElement.style.setProperty('--qa-toolbar-height',holder.offsetTop+'px');
  new ResizeObserver(sizeFrame).observe(bar);new ResizeObserver(sizeFrame).observe(document.querySelector('.bar'));sizeFrame();
  let pendingId='',timer=null,sequence=0;
  const buttons=[...bar.querySelectorAll('button')];
  function release(){if(timer)clearTimeout(timer);timer=null;pendingId='';for(const button of buttons)button.disabled=false;}
  for(const button of bar.querySelectorAll('[data-touch]'))button.addEventListener('click',()=>{
    if(!frame){state.textContent='게임 프레임이 없습니다.';return;}
    if(pendingId)return;
    pendingId='touch-'+(++sequence);state.textContent='터치 이벤트 전송 중';output.textContent='';
    for(const item of buttons)item.disabled=true;
    timer=setTimeout(()=>{release();state.textContent='합성 터치 응답 시간 초과';},15000);
    frame.contentWindow.postMessage({type:'bts:qa-touch',requestId:pendingId,action:button.dataset.touch},location.origin);
  });
  window.addEventListener('message',event=>{
    if(!frame||event.source!==frame.contentWindow||event.origin!==location.origin||event.data?.type!=='bts:qa-touch-result'||event.data.requestId!==pendingId)return;
    const result=event.data;release();
    state.textContent=result.ok?'이번 '+result.events+'개 / 누적 '+result.totalEvents+'개 / 기본 동작 차단 '+result.preventedEvents+'개':'합성 터치 오류: '+result.error;
    output.textContent=JSON.stringify(result,null,2);
  });
  document.querySelector('#touch-reset').addEventListener('click',()=>{release();state.textContent='QA 장면을 초기화합니다.';output.textContent='';void reload();});
})();
</script>`;
const qaInteractiveHtml=(baseline?qaHtml.replaceAll('1.0.24','1.0.23'):qaHtml)
  .replace('<option value="research">',(!baseline?'<option value="combat-touch-wide">터치 검증 · 손패 15장</option>':'')+'<option value="research">')
  .replace('</style>','.touchbar{min-height:72px;padding:8px 16px;display:flex;align-items:center;gap:7px;flex-wrap:wrap;border-top:1px solid #385069}.touchbar[hidden]{display:none}.touchbar small{flex-basis:100%;font-size:11px}.touchbar details{font-size:11px}.touchbar pre{max-height:100px;overflow:auto}#game{height:calc(100vh - var(--qa-toolbar-height,72px))}#game iframe{height:100%}</style>')
  .replace('<div id="game"></div>',qaTouchControlsHtml+'<div id="game"></div>')
  .replace('</script></html>','</script>'+qaTouchControlsScript+'</html>');

const server = http.createServer(async (req, res) => {
  try {
    if (!['GET','HEAD'].includes(req.method)) { send(res, req.method, 405, 'text/plain; charset=utf-8', 'Read-only preview', {Allow:'GET, HEAD'}); return; }
    if (![`127.0.0.1:${port}`,`localhost:${port}`].includes(req.headers.host)) { send(res, req.method, 421, 'text/plain; charset=utf-8', 'Loopback host required'); return; }
    const rawPath = (req.url ?? '/').split('?')[0];
    let pathname; try { pathname=decodeURIComponent(rawPath); } catch { throw Object.assign(new Error('Bad URL'),{status:400}); }
    if (pathname.includes('\\') || pathname.includes('\0') || pathname.split('/').some(part=>part==='..')) throw Object.assign(new Error('Bad path'),{status:404});
    if (['/','/art','/art/'].includes(pathname)) { send(res, req.method, 200, 'text/html; charset=utf-8', artHtml); return; }
    if (['/qa','/qa/'].includes(pathname)) { send(res, req.method, 200, 'text/html; charset=utf-8', qaInteractiveHtml); return; }
    if (art.has(pathname)) { await streamFile(req,res,{file:art.get(pathname)}); return; }
    const fixture=/^\/fixtures\/([a-z-]+)\.json$/.exec(pathname);
    if (fixture && fixtureNames.has(fixture[1])) {
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
server.listen(port,host,()=>console.log(JSON.stringify({ready:true,origin:'http://'+host+':'+port,art:'/art',qa:'/qa?mobile=1&fixture=nia-rare-combat',gameRoot,fixtureRoot,localOnly:true,productionRequests:baseline,readOnlyBaseline:baseline,remoteAssetOrigin:baseline?baselinePublicOrigin:null,diskCaching:false,syntheticTouchLimit:'Browser TouchEvents are untrusted; physical mobile hardware is not emulated'})));
for(const signal of ['SIGINT','SIGTERM'])process.once(signal,()=>server.close(()=>process.exit(0)));
