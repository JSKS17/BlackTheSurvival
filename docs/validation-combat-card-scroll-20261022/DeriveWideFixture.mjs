// Docs-only QA fixture derivation. No Unity or production files are edited.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { readFile, writeFile, lstat } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const proofRoot=path.dirname(fileURLToPath(import.meta.url));
const fixtureRoot=path.join(proofRoot,'fixtures');
const source=path.join(fixtureRoot,'nia-rare-combat.json');
const target=path.join(fixtureRoot,'combat-touch-wide.json');
const sha=bytes=>createHash('sha256').update(bytes).digest('hex');
const original=await readFile(source);
const originalHash=sha(original);
assert.equal(originalHash,'1bcbc5e94648d83a288170d9336a262dca02813ad009426bc01485ef3dd1de66','Original exported normal-hand fixture changed');
assert(!(await lstat(source)).isSymbolicLink());
let targetExists=false;
try{
  const info=await lstat(target);targetExists=true;assert(!info.isSymbolicLink()&&info.isFile());
  assert(process.argv.includes('--replace-derived'),'Refusing to replace an existing wide-hand fixture');
  const previous=JSON.parse(await readFile(path.join(proofRoot,'wide-fixture-derivation.json'),'utf8'));
  assert.equal(previous.result,'PASS');assert.equal(previous.sourceSha256,originalHash);assert.equal(previous.target,'fixtures/combat-touch-wide.json');
  assert.equal(sha(await readFile(target)),previous.targetSha256,'Existing derived fixture no longer matches its own proof');
}catch(error){if(error.code!=='ENOENT')throw error;}
const normal=JSON.parse(original.toString('utf8').replace(/^\uFEFF/,''));
assert.equal(normal.combat.hand.length,5);assert.equal(normal.combat.energy,5);assert.equal(normal.combat.enemyHp,48);
const wide=structuredClone(normal);
wide.combat.hand=Array.from({length:3},()=>normal.combat.hand).flat();
// This disposable wide-hand scene places the entire 15-card test deck in hand.
wide.deck=[...wide.combat.hand];
wide.combat.drawPile=[];wide.combat.discardPile=[];wide.combat.exhaustPile=[];
assert.equal(wide.combat.hand.length,15);
assert(wide.combat.hand.every(card=>normal.deck.includes(card)),'All test cards must already be real cards in the exported deck');
assert.equal(wide.combat.energy,normal.combat.energy);assert.equal(wide.combat.enemyHp,normal.combat.enemyHp);
const counts=cards=>cards.reduce((map,card)=>(map[card]=(map[card]||0)+1,map),{});
assert.deepEqual(counts(wide.deck),counts([...wide.combat.hand,...wide.combat.drawPile,...wide.combat.discardPile,...wide.combat.exhaustPile]));
const unchanged=structuredClone(wide);unchanged.deck=normal.deck;
for(const pile of ['hand','drawPile','discardPile','exhaustPile'])unchanged.combat[pile]=normal.combat[pile];
assert.deepEqual(unchanged,normal,'Unexpected fixture fields changed');
const output=Buffer.from(JSON.stringify(wide,null,2)+'\n');
await writeFile(target,output,{flag:targetExists?'w':'wx'});
assert.equal(sha(await readFile(source)),originalHash,'Original fixture bytes must remain unchanged');
const proof={result:'PASS',checkedAtUtc:new Date().toISOString(),scope:'Docs-only optional wide-hand QA fixture',productionChanges:false,
  source:'fixtures/nia-rare-combat.json',sourceSha256:originalHash,originalFixturePreserved:true,
  target:'fixtures/combat-touch-wide.json',targetSha256:sha(output),normalHand:normal.combat.hand,normalHandCount:5,wideHandCount:15,
  handRepeated:3,deckCount:wide.deck.length,energy:wide.combat.energy,enemyHp:wide.combat.enemyHp,changedFields:['deck','combat.hand','combat.drawPile'],
  clearedPiles:['drawPile','discardPile','exhaustPile'],allCardsExistInOriginalDeck:true,deckCountsMatchCombatPiles:true,baselineModeOmitsNewFixture:true};
await writeFile(path.join(proofRoot,'wide-fixture-derivation.json'),JSON.stringify(proof,null,2)+'\n');
console.log(JSON.stringify({result:'PASS',normalHandCount:5,wideHandCount:15,energy:wide.combat.energy,enemyHp:wide.combat.enemyHp,originalFixturePreserved:true}));
