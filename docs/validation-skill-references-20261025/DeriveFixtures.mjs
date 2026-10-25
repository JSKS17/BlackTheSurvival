// Disposable local QA runs derived from the last validated combat schema.
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {readFile,writeFile,mkdir} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const here=path.dirname(fileURLToPath(import.meta.url));
const workspace=path.resolve(here,'../..');
const original=await readFile(path.join(workspace,'docs/validation-player-feedback-20261023/fixtures/feedback-combat.json'));
const source=JSON.parse(original),fixtures=path.join(here,'fixtures');
const sha=bytes=>createHash('sha256').update(bytes).digest('hex');
const multiset=items=>[...items.reduce((counts,id)=>counts.set(id,(counts.get(id)||0)+1),new Map())].sort(([a],[b])=>a.localeCompare(b));
assert.equal(source.stage,2); assert.equal(source.combat.energy,5);
await mkdir(fixtures,{recursive:true});
const outputs=[];
for(const [name,skills] of [
  ['chloe-combat',['chloe_q','chloe_e','aya_q']],
  ['emma-combat',['emma_q','emma_r','aya_q']],
  ['tazia-combat',['tazia_e','tazia_q','aya_e']],
  ['advanced-combat',['adina_r','debi_marlene_e','aya_e']],
]) {
  const run=structuredClone(source);
  run.deck=['basic_attack','basic_attack','basic_attack','basic_attack','basic_attack','basic_guard','basic_guard','basic_guard',...skills];
  if(name==='advanced-combat')run.deck.unshift('debi_marlene_r');
  run.combat.hand=[...skills,'basic_attack','basic_guard'];
  const remaining=[...run.deck];
  for(const id of run.combat.hand){const index=remaining.indexOf(id);assert(index>=0);remaining.splice(index,1);}
  run.combat.drawPile=remaining;run.combat.discardPile=[];run.combat.exhaustPile=[];
  if(name==='tazia-combat')run.combat.playerSkills.resources=[
    {owner:'타지아',key:'shard',label:'유리 파편',amount:3,cap:4,fraction:0,persistent:false,isState:false},
    {owner:'타지아',key:'collection',label:'파편 수집',amount:0,cap:4,fraction:0,persistent:false,isState:false},
    {owner:'타지아',key:'sword',label:'유리 검',amount:0,cap:1,fraction:0,persistent:false,isState:false},
  ];
  run.message='로컬 QA · 실험체 연계 설명 · '+name;
  run.log=[...source.log.slice(0,4),run.message];
  run.research.runId=sha(Buffer.from('skill-references-20261025-'+name)).slice(0,32);
  assert.deepEqual(Object.keys(run).sort(),Object.keys(source).sort());
  assert.deepEqual(multiset(run.deck),multiset([...run.combat.hand,...run.combat.drawPile]));
  const bytes=Buffer.from(JSON.stringify(run,null,2)+'\n');
  await writeFile(path.join(fixtures,name+'.json'),bytes);
  outputs.push({name,sha256:sha(bytes),bytes:bytes.length,hand:run.combat.hand,deckSize:run.deck.length,pilesMatch:true,initialResources:run.combat.playerSkills.resources});
}
const result={result:'PASS',derivedAtUtc:new Date().toISOString(),scope:'Disposable localhost QA only',source:'docs/validation-player-feedback-20261023/fixtures/feedback-combat.json',sourceSha256:sha(original),productionRequests:false,accountRequests:false,outputs};
await writeFile(path.join(here,'fixture-derivation.json'),JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify({result:'PASS',fixtures:outputs.map(x=>x.name)}));
