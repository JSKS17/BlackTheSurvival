import assert from 'node:assert/strict';
import {readFile,writeFile} from 'node:fs/promises';
const file=new URL('./Preview.mjs',import.meta.url);let source=await readFile(file,'utf8');
function replace(from,to){assert.equal(source.split(from).length,2,from);source=source.replace(from,to);}
replace('<span id="status" role="status">준비 중</span>','<button id="audit" type="button">검증 저장 요약</button><span id="status" role="status">준비 중</span>');
replace('<div id="game"></div><script>','<div id="game"></div><pre id="audit-output" hidden style="position:fixed;right:12px;bottom:12px;max-height:220px;overflow:auto;z-index:9;background:#101d2f;padding:12px;border:1px solid #89dfcf;font:14px monospace"></pre><script>');
replace('reduceMotion:true,fullScreen:false',"reduceMotion:!new URLSearchParams(location.search).has('motion'),fullScreen:false");
replace('function fit(){',`document.querySelector('#audit').addEventListener('click',async()=>{try{const exported=await command('bts:export');const bundle=JSON.parse(exported.bundleJson),run=bundle.run?JSON.parse(bundle.run):null;const node=run?.map?.find(n=>n.id===run.activeNodeId);const output=document.querySelector('#audit-output');output.textContent=JSON.stringify({stage:run?['Preparation','Map','Combat','Rewards','Kiosk','Campfire','Encounter','PassiveChoice','Won','Lost','AugmentChoice','EquipmentSlotChoice','LumiOffer','LumiShop'][run.stage]:null,credits:run?.credits,objects:run?.objects,activeNodeId:run?.activeNodeId,wickeline:node?.wickeline,wickelineDefeated:node?.wickelineDefeated,combatPresent:Boolean(run?.combat)},null,2);output.hidden=false;}catch(error){show(error.message,true);}});
function fit(){`);
await writeFile(file,source);console.log('Local QA export summary and optional R animation enabled.');
