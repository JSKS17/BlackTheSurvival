import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import { createHash } from 'node:crypto';

const here = import.meta.dirname;
const root = path.resolve(here, '../..');
const sha = bytes => createHash('sha256').update(bytes).digest('hex');
const focused = JSON.parse(await fs.readFile(path.join(here, 'rune-offer-tests.json'), 'utf8'));
assert.equal(focused.result, 'PASS'); assert.equal(focused.assertions, 1922);
const logBytes = await fs.readFile(path.join(here, 'rune-offer-tests.log'));
const log = logBytes.toString('utf8');
const rows = [...log.matchAll(/Rune preparation distribution:[^\r\n]*?passive (\d+)->(\d+)\/(\d+), card (\d+)->(\d+)\/(\d+)/g)];
assert.equal(rows.length, 4);
const treeOrder = ['파괴', '저항', '혼돈', '지원'];
const official = JSON.parse(await fs.readFile(path.join(root, 'docs/rune-roles-1.1.0/official-roles.json'), 'utf8'));
const labels = [['전사', '원거리 딜러'], ['탱커', '전사'], ['스킬 딜러', '암살자'], ['탱커', '지원가']];
const groups = rows.map((row, i) => {
  const [basePassiveHits, rolePassiveHits, passiveSamples, baseCardHits, roleCardHits, cardSamples] = row.slice(1).map(Number);
  assert.equal(passiveSamples, 480); assert.equal(cardSamples, 960);
  const favoredSubjects = official.characters.filter(character => character.roles.some(role => labels[i].includes(role))).length;
  return { tree: treeOrder[i], roles: labels[i], favoredSubjects, totalSubjects: 91, candidateWeight: 4,
    firstPassiveProbabilityWithoutRoleWeight: favoredSubjects / 91,
    firstPassiveProbabilityWithRoleWeight: 4 * favoredSubjects / (4 * favoredSubjects + 91 - favoredSubjects),
    empiricalPreparation: { independentRunSeeds: { first: 2101, last: 2260, count: 160 },
      passive: { samples: passiveSamples, beforeHits: basePassiveHits, afterHits: rolePassiveHits, beforeFraction: basePassiveHits / passiveSamples, afterFraction: rolePassiveHits / passiveSamples },
      card: { samples: cardSamples, beforeHits: baseCardHits, afterHits: roleCardHits, beforeFraction: baseCardHits / cardSamples, afterFraction: roleCardHits / cardSamples } } };
});
const pair = log.match(/Rune pair distribution: plain=(\d+)\/(\d+), role=(\d+)\/(\d+), passive=(\d+)\/(\d+), combined=(\d+)\/(\d+)/);
assert.ok(pair);
const pairValues = pair.slice(1).map(Number);
const pairNames = ['unweightedCard', 'roleWeightedCard', 'roleWeightedPassive', 'roleAndOwnedPassiveCard'];
const pairResults = Object.fromEntries(pairNames.map((name, i) => [name, { hits: pairValues[2 * i], samples: pairValues[2 * i + 1], fraction: pairValues[2 * i] / pairValues[2 * i + 1] }]));
const proof = { result: 'PASS', gameVersion: '1.1.0', sourceFreezeSha256: focused.sourceFreezeSha256,
  focusedAssertions: focused.assertions, log: 'rune-offer-tests.log', logSha256: sha(logBytes),
  roleLabelRecovery: 'The native Mono child emitted CP949 Korean labels through a UTF-8 Node pipe. Numeric output is unchanged. Structured row labels use the fixed four-tree loop order in the frozen RuneOfferSmokeTests.cs source.',
  testSourceSha256: focused.sourceHashes['Tools/RuneOfferSmokeTests.cs'],
  method: 'Actual GameEngine preparation before and after selecting a rune, 160 fixed seeds per color, no owned passive selected; unique weighted sampling without replacement. Pair tests use 16,000 draws per case.',
  pairResults, combinedCandidateWeight: 7.2, dualRoleMatchesStack: false, fixedSubjectBossAndEncounterRewardsReplaced: false, groups };
await fs.writeFile(path.join(here, 'rune-offer-statistics.json'), JSON.stringify(proof, null, 2) + '\n');
console.log(JSON.stringify({ result: proof.result, groups: groups.map(group => ({ tree: group.tree, passiveBefore: group.empiricalPreparation.passive.beforeFraction, passiveAfter: group.empiricalPreparation.passive.afterFraction, cardBefore: group.empiricalPreparation.card.beforeFraction, cardAfter: group.empiricalPreparation.card.afterFraction })) }, null, 2));
