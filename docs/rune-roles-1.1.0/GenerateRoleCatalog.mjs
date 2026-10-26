import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import { createHash } from 'node:crypto';

const root = path.resolve(import.meta.dirname, '../..');
const officialBytes = await fs.readFile(path.join(import.meta.dirname, 'official-roles.json'));
const official = JSON.parse(officialBytes);
const flags = { '전사': 'Fighter', '원거리 딜러': 'Marksman', '탱커': 'Tank', '스킬 딜러': 'Mage', '암살자': 'Assassin', '지원가': 'Support' };
assert.equal(official.characters.length, 91);
assert.equal(new Set(official.characters.map(character => character.id)).size, 91);
assert.ok(official.characters.every(character => character.roles.length >= 1 && character.roles.length <= 2 && character.roles.every(role => flags[role])));
assert.ok(official.characters.every(character => !['lumi', 'wickeline'].includes(character.id)));
const entries = official.characters.map(character => `            { "${character.id}", ${character.roles.map(role => 'SubjectRole.' + flags[role]).join(' | ')} }, // ${character.name}`);
const catalogPath = path.join(root, 'Assets/Scripts/Lumia/SubjectRoleCatalog.cs');
const source = await fs.readFile(catalogPath, 'utf8');
const block = '            // BEGIN OFFICIAL_ROLE_ENTRIES\n' + entries.join('\n') + '\n            // END OFFICIAL_ROLE_ENTRIES';
const next = source.includes('// BEGIN OFFICIAL_ROLE_ENTRIES')
  ? source.replace(/            \/\/ BEGIN OFFICIAL_ROLE_ENTRIES[\s\S]*?            \/\/ END OFFICIAL_ROLE_ENTRIES/, block)
  : source.replace('            // OFFICIAL_ROLE_ENTRIES', block);
assert.notEqual(next.indexOf('// END OFFICIAL_ROLE_ENTRIES'), -1);
await fs.writeFile(catalogPath, next);
const groups = [ ['파괴', ['전사', '원거리 딜러']], ['저항', ['탱커', '전사']], ['혼돈', ['스킬 딜러', '암살자']], ['지원', ['탱커', '지원가']] ].map(([tree, roles]) => {
  const count = official.characters.filter(character => character.roles.some(role => roles.includes(role))).length;
  return { tree, roles, favoredSubjects: count, totalSubjects: 91, ordinaryPassiveProbability: count / 91,
    weightedFirstPassiveProbability: 4 * count / (4 * count + 91 - count) };
});
await fs.writeFile(path.join(import.meta.dirname, 'role-catalog-generation.json'), JSON.stringify({ result: 'PASS', subjects: 91,
  officialSha256: createHash('sha256').update(officialBytes).digest('hex'), catalogSha256: createHash('sha256').update(next).digest('hex'), groups }, null, 2) + '\n');
console.log(JSON.stringify(groups, null, 2));
