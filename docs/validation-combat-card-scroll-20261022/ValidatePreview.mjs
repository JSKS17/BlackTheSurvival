// Offline validation of the local-only browser-touch driver. This validates the
// test driver, not Unity's touch handling or physical mobile hardware.
import assert from 'node:assert/strict';
import { readFile, writeFile } from 'node:fs/promises';
import vm from 'node:vm';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const directory=path.dirname(fileURLToPath(import.meta.url));
const source=await readFile(path.join(directory,'Preview.mjs'),'utf8');
function embedded(name){
  const match=source.match(new RegExp('const '+name+' = `([\\s\\S]*?)`;'));
  assert(match, 'Missing '+name);
  const script=match[1].match(/<script[^>]*>([\s\S]*?)<\/script>/)?.[1];
  assert(script,'Missing script in '+name);new vm.Script(script);return script;
}
const driver=embedded('qaTouchModelScript');embedded('qaTouchControlsScript');
const mobileLiteral=source.match(/const model = (`[^`]*`) \+ qaTouchModelScript;/)?.[1];
assert(mobileLiteral,'Missing full local mobile model');
const mobileHtml=vm.runInNewContext(mobileLiteral);
const mobileScript=mobileHtml.match(/<script[^>]*>([\s\S]*?)<\/script>/)?.[1];assert(mobileScript);
const mobileContext={navigator:{userAgent:'Mozilla/5.0 QA Chrome/145.0.7780.12 Safari/537.36'},window:{}};
new vm.Script(mobileScript).runInNewContext(mobileContext);
assert.match(mobileContext.navigator.userAgent,/Android 14/);assert.match(mobileContext.navigator.userAgent,/Chrome\/145\.0\.7780\.12/);
assert.match(mobileContext.navigator.userAgent,/Mobile Safari/);assert.equal(mobileContext.navigator.userAgentData.mobile,true);
assert.equal(mobileContext.navigator.maxTouchPoints,5);assert('ontouchstart' in mobileContext.window);
const actions=['description-up','description-down','hand-left','hand-right','first-card-tap'];
const reports=[];
for(const [width,height,cssWidth,cssHeight] of [[1280,720,640,360],[1000,1000,500,500],[1800,720,900,360]]) {
  const listeners=[],events=[],messages=[];let frame=0,focuses=0;
  const canvas={width,height,isConnected:true,getBoundingClientRect:()=>({left:17,top:29,width:cssWidth,height:cssHeight}),
    focus:()=>focuses++,dispatchEvent:event=>{events.push({type:event.type,frame,touch:event.changedTouches[0],touches:event.touches.length});return false;}};
  const parent={postMessage:(message,origin)=>messages.push(message)};
  const context={window:{parent,screenX:20,screenY:40,blackTheSurvival:{status:'ready'},addEventListener:(name,listener)=>listeners.push({name,listener})},
    location:{origin:'http://127.0.0.1:8245'},document:{querySelector:()=>canvas},scrollX:0,scrollY:0,
    Touch:class Touch{constructor(init){Object.assign(this,init);}},TouchEvent:class TouchEvent{constructor(type,init){this.type=type;Object.assign(this,init);}},
    requestAnimationFrame:callback=>{frame++;queueMicrotask(callback);}};
  vm.runInNewContext(driver,context);
  assert.equal(listeners.length,1);
  for(const action of actions){
    const start=events.length;
    await listeners[0].listener({source:parent,origin:context.location.origin,data:{type:'bts:qa-touch',requestId:action,action}});
    const result=messages.at(-1),sent=events.slice(start);
    assert.equal(result.ok,true);assert.equal(result.isTrusted,false);
    assert.equal(sent.length,action==='first-card-tap'?2:14);
    assert.equal(sent[0].type,'touchstart');assert.equal(sent.at(-1).type,'touchend');
    assert.equal(sent.at(-1).touches,0);
    assert(sent.every(event=>event.touch.identifier===sent[0].touch.identifier));
    for(let index=1;index<sent.length;index++)assert(sent[index].frame-sent[index-1].frame>=2);
    const scale=Math.min(width/1280,height/720),logical=result.logicalFrom;
    assert.equal(sent[0].touch.clientX,17+((width-1280*scale)/2+logical[0]*scale)*cssWidth/width);
    assert.equal(sent[0].touch.clientY,29+((height-720*scale)/2+logical[1]*scale)*cssHeight/height);
    reports.push({canvas:[width,height],css:[cssWidth,cssHeight],action,events:sent.length,result:'PASS'});
  }
  assert.equal(focuses,actions.length);
  const count=events.length;
  await listeners[0].listener({source:{},origin:context.location.origin,data:{type:'bts:qa-touch',action:'first-card-tap'}});
  assert.equal(events.length,count,'Untrusted message source must be ignored');
}
const report={result:'PASS',checkedAtUtc:new Date().toISOString(),scope:'Local QA driver only',productionRequests:false,
  injectedScriptSyntaxChecks:3,fullMobileModelChecks:6,driverScenarios:reports.length,physicalMobileHardwareTested:false,UnityInputTested:false,
  limitation:'TouchEvents are untrusted synthetic browser events. Unity behavior must be checked separately in the actual browser build.',scenarios:reports};
await writeFile(path.join(directory,'preview-driver-validation.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({result:report.result,driverScenarios:report.driverScenarios,injectedScriptSyntaxChecks:3,fullMobileModelChecks:6,UnityInputTested:false}));
