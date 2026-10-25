// Derive disposable local QA saves from a previously browser-validated run.
// No Unity execution, production requests, account requests or source mutation.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { copyFile, mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const workspace = path.resolve(here, '../..');
const oldRoot = path.join(workspace, 'docs/validation-combat-card-scroll-20261022/fixtures');
const fixtureRoot = path.join(here, 'fixtures');
const sha = bytes => createHash('sha256').update(bytes).digest('hex');
const sourceBytes = await readFile(path.join(oldRoot, 'nia-rare-combat.json'));
const source = JSON.parse(sourceBytes);
const profileBytes = await readFile(path.join(oldRoot, 'research.json'));
const modelBytes = await readFile(path.join(workspace, 'Assets/Scripts/Lumia/DataModels.cs'));
const engineBytes = await readFile(path.join(workspace, 'Assets/Scripts/Lumia/GameEngine.cs'));
const databaseBytes = await readFile(path.join(workspace, 'Assets/Scripts/Lumia/GameDatabase.cs'));
const equipmentBytes = await readFile(path.join(workspace, 'Assets/Scripts/Lumia/EquipmentIdentity.cs'));
const models = modelBytes.toString('utf8');
const database = databaseBytes.toString('utf8');
const equipment = equipmentBytes.toString('utf8');
const stages = models.match(/enum RunStage\s*\{([^}]+)\}/)?.[1].split(',').map(value => value.trim());
const zones = models.match(/enum ZoneKind\s*\{([^}]+)\}/)?.[1].split(',').map(value => value.trim());
assert(stages && zones);
assert.equal(stages.indexOf('Campfire'), 5); assert.equal(stages.indexOf('Combat'), 2);
assert.equal(zones.indexOf('Campfire'), 3);
assert.equal(source.version, 2); assert.equal(source.stage, stages.indexOf('Combat'));
assert.equal(source.combat.energy, 5);
for (const key of ['deck', 'gear', 'objects', 'foods', 'map', 'weaponGrantedCards']) assert(Array.isArray(source[key]), key);
assert(source.combat.playerSkills && source.combat.enemySkills && source.combat.playerTraits && source.research);
const materialIds = [...database.matchAll(/Objects\.Add\(new ObjectDef\s*\{\s*id="([^"]+)"/g)].map(match => match[1]);
assert.deepEqual(materialIds, ['meteorite', 'tree', 'mithril', 'force', 'blood']);
const basicDeck = ['basic_attack', 'basic_attack', 'basic_attack', 'basic_attack', 'basic_attack',
  'basic_guard', 'basic_guard', 'basic_guard', 'yuki_w', 'yuki_e', 'aya_e'];
for (const id of new Set(basicDeck)) assert(database.includes('"' + id + '"'), `Unknown card ${id}`);
const multiset = cards => [...cards.reduce((counts, id) => counts.set(id, (counts.get(id) ?? 0) + 1), new Map())]
  .sort(([a], [b]) => a.localeCompare(b));

const crafting = structuredClone(source);
crafting.stage = stages.indexOf('Campfire'); crafting.campChoice = 2; crafting.campActions = 3;
crafting.campRemovalsRemaining = 0;
const camp = crafting.map.find(node => node.act === 1 && node.kind === zones.indexOf('Campfire'));
assert(camp); camp.visited = true;
crafting.activeNodeId = camp.id; crafting.act = camp.act; crafting.row = camp.row; crafting.lane = camp.lane;
crafting.objects = materialIds.flatMap(id => Array(5).fill(id));
crafting.gear = ['altair', 'altair', 'titan_armor', 'ao_dai', 'mithril_helm', 'tactical_visor',
  'mithril_armband', 'auto_arms', 'hermes', 'galaxy_step'];
const slotGroups = {
  Weapon: ['altair', 'altair'], Clothes: ['titan_armor', 'ao_dai'], Head: ['mithril_helm', 'tactical_visor'],
  Arm: ['mithril_armband', 'auto_arms'], Legs: ['hermes', 'galaxy_step'],
};
for (const [slot, ids] of Object.entries(slotGroups)) {
  assert.equal(ids.length, 2);
  for (const id of ids) {
    const original = database.match(new RegExp(`G\\("${id}",[^\\n]+GearSlot\\.${slot}\\b`));
    const expanded = equipment.match(new RegExp(`Add\\(gear,"${id}",[^\\n]+slot:GearSlot\\.${slot}\\b`));
    assert(original || expanded, `${id} must occupy ${slot}`);
  }
}
crafting.weaponGrantedCards = ['weapon_pistol', 'weapon_pistol'];
crafting.deck = [...basicDeck, ...crafting.weaponGrantedCards]; crafting.weaponGrantVersion = 1;
crafting.foods = ['potato', 'meat', 'salmon'];
const health = crafting.gear.reduce((sum, id) => {
  const configured = equipment.match(new RegExp(`Set\\(gear,"${id}",([^\\n]+)`))?.[1];
  assert(configured, `Missing configured equipment ${id}`);
  return sum + Number(configured.match(/\bhealth:(\d+)/)?.[1] ?? 0);
}, 0);
crafting.maxHp = 80 + health; crafting.hp = crafting.maxHp;
crafting.message = '로컬 QA · 제작 재료·장비 효과 범위 확인 · 남은 작업 3회';
crafting.log = [...source.log.slice(0, 4), crafting.message];
crafting.combat = null;
crafting.research.runId = sha(Buffer.from('player-feedback-crafting-20261023')).slice(0, 32);
crafting.research.completedNodeIds = crafting.research.completedNodeIds.filter(id => id !== camp.id);

const combat = structuredClone(source);
combat.stage = stages.indexOf('Combat'); combat.campChoice = 0; combat.campActions = 0;
combat.deck = [...basicDeck]; combat.gear = []; combat.objects = []; combat.foods = [];
combat.weaponGrantedCards = []; combat.weaponGrantVersion = 1;
combat.hp = 80; combat.maxHp = 80;
combat.message = '로컬 QA · 유키 W→E 연계 및 아야 E 설명 확인 · 코스트 5/5';
combat.log = [...source.log.slice(0, 4), combat.message];
combat.research.runId = sha(Buffer.from('player-feedback-combat-20261023')).slice(0, 32);
combat.combat.hand = ['yuki_w', 'yuki_e', 'aya_e', 'basic_attack', 'basic_guard'];
const remaining = [...basicDeck];
for (const id of combat.combat.hand) { const index = remaining.indexOf(id); assert(index >= 0); remaining.splice(index, 1); }
combat.combat.drawPile = remaining;
combat.combat.discardPile = []; combat.combat.exhaustPile = []; combat.combat.chargedCards = [];
combat.combat.energy = 5; combat.combat.enemyTurn = false;
combat.combat.lastSkillCard = ''; combat.combat.lastSkills = [];
combat.combat.freeCasts = []; combat.combat.pendingFreeCasts = []; combat.combat.freeCastSources = [];
combat.combat.retainIndex = -1; combat.combat.retainUses = 0; combat.combat.retainedCardId = '';
assert.deepEqual(multiset(combat.deck), multiset([
  ...combat.combat.hand, ...combat.combat.drawPile, ...combat.combat.discardPile, ...combat.combat.exhaustPile,
]));
assert.deepEqual(Object.keys(crafting).sort(), Object.keys(source).sort());
assert.deepEqual(Object.keys(combat).sort(), Object.keys(source).sort());
assert.deepEqual(Object.keys(combat.combat).sort(), Object.keys(source.combat).sort());
const changedTopLevelKeys = value => Object.keys(source).filter(key => JSON.stringify(source[key]) !== JSON.stringify(value[key]));

await mkdir(fixtureRoot, { recursive: true });
await copyFile(path.join(oldRoot, 'research.json'), path.join(fixtureRoot, 'research.json'));
assert.deepEqual(await readFile(path.join(fixtureRoot, 'research.json')), profileBytes, 'Research profile must be byte-exact');
const outputs = [];
for (const [name, value] of [['feedback-crafting', crafting], ['feedback-combat', combat]]) {
  const bytes = Buffer.from(JSON.stringify(value, null, 2) + '\n');
  await writeFile(path.join(fixtureRoot, name + '.json'), bytes);
  assert.deepEqual(JSON.parse(await readFile(path.join(fixtureRoot, name + '.json'), 'utf8')), value);
  outputs.push({ name, bytes: bytes.length, sha256: sha(bytes), stage: value.stage,
    changedTopLevelKeys: changedTopLevelKeys(value), deckCards: value.deck.length,
    handCards: value.combat?.hand.length ?? 0, pileMultisetMatchesDeck: value.combat ? true : 'not in combat' });
}
const report = {
  result: 'PASS', derivedAtUtc: new Date().toISOString(), scope: 'Disposable local browser QA fixtures only',
  productionRequests: false, accountRequests: false, unityExecuted: false, serverStarted: false, productSourceChanged: false,
  source: { path: path.relative(workspace, path.join(oldRoot, 'nia-rare-combat.json')).replaceAll('\\', '/'),
    bytes: sourceBytes.length, sha256: sha(sourceBytes) },
  research: { sourcePath: path.relative(workspace, path.join(oldRoot, 'research.json')).replaceAll('\\', '/'),
    bytes: profileBytes.length, sha256: sha(profileBytes), copiedByteExact: true },
  inspectedSchema: {
    'Assets/Scripts/Lumia/DataModels.cs': sha(modelBytes),
    'Assets/Scripts/Lumia/GameEngine.cs': sha(engineBytes),
    'Assets/Scripts/Lumia/GameDatabase.cs': sha(databaseBytes),
    'Assets/Scripts/Lumia/EquipmentIdentity.cs': sha(equipmentBytes),
  },
  enumValues: { 'RunStage.Campfire': stages.indexOf('Campfire'), 'RunStage.Combat': stages.indexOf('Combat'), 'ZoneKind.Campfire': zones.indexOf('Campfire') },
  crafting: { activeNodeId: crafting.activeNodeId, row: crafting.row, lane: crafting.lane,
    campChoice: 2, campActions: 3, materialCounts: Object.fromEntries(materialIds.map(id => [id, 5])),
    legalSlotGroups: slotGroups, hp: crafting.hp, maxHp: crafting.maxHp,
    sameItemExample: ['altair', 'altair'], sameNamedEffectsAcrossItems: {
      '가벼운 발걸음': ['altair', 'mithril_helm', 'mithril_armband'], '전자기 충격': ['titan_armor', 'tactical_visor'],
      '방해 효과 저항': ['auto_arms', 'hermes'],
    }, weaponGrantedCards: crafting.weaponGrantedCards },
  combat: { enemyId: combat.combat.enemyId, enemyHp: combat.combat.enemyHp,
    energy: 5, hand: combat.combat.hand, drawPile: combat.combat.drawPile, playerTurn: true,
    enemyStatePreserved: true, startsBeforeAnyCombo: true, pileMultisetMatchesDeck: true },
  outputs,
  limitation: 'Schema and fixture derivation were checked offline. Root agent performs actual Unity WebGL browser interactions.',
};
await writeFile(path.join(here, 'fixture-derivation.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({ result: report.result, researchByteExact: true, fixtures: outputs.map(({ name, stage, deckCards }) => ({ name, stage, deckCards })), campHp: crafting.hp, materialTypes: materialIds.length, combatPileConsistent: true }));
