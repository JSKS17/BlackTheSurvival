import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
import assert from 'node:assert/strict';

const proof = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(proof, '../..');
const officialPath = 'docs/rune-roles-1.1.0/official-roles.json';
const catalogPath = 'Assets/Scripts/Lumia/SubjectRoleCatalog.cs';
const officialBytes = await fs.readFile(path.join(root, officialPath));
const catalogBytes = await fs.readFile(path.join(root, catalogPath));
const official = JSON.parse(officialBytes);
const source = catalogBytes.toString('utf8');
const sha = value => createHash('sha256').update(value).digest('hex');
const expected = new Map([
  ['전사', ['Fighter', 1]], ['원거리 딜러', ['Marksman', 2]], ['탱커', ['Tank', 4]],
  ['스킬 딜러', ['Mage', 8]], ['암살자', ['Assassin', 16]], ['지원가', ['Support', 32]],
]);
const enumText = source.match(/public enum SubjectRole\s*\{([^}]+)\}/)?.[1];
assert.ok(enumText, 'SubjectRole enum exists');
const enumValues = new Map([...enumText.matchAll(/(\w+)\s*=\s*(\d+)/g)].map(value => [value[1], Number(value[2])]));
assert.equal(enumValues.get('None'), 0);
for (const [name, value] of expected.values()) assert.equal(enumValues.get(name), value, `Role enum value: ${name}`);

const block = source.match(/\/\/ BEGIN OFFICIAL_ROLE_ENTRIES([\s\S]*?)\/\/ END OFFICIAL_ROLE_ENTRIES/)?.[1];
assert.ok(block, 'Official catalog block exists');
const rows = [...block.matchAll(/\{\s*"([^"]+)"\s*,\s*([^}]+)\}\s*,\s*\/\/\s*([^\r\n]+)/g)].map(value => {
  const expressions = value[2].trim().split(/\s*\|\s*/);
  const flags = expressions.map(expression => expression.match(/^SubjectRole\.(\w+)$/)?.[1]);
  assert.ok(flags.every(flag => enumValues.has(flag) && flag !== 'None'), `Known role flags for ${value[1]}`);
  assert.equal(new Set(flags).size, flags.length, `No repeated flags for ${value[1]}`);
  return { id: value[1], name: value[3].trim(), flags, bitmask: flags.reduce((mask, flag) => mask | enumValues.get(flag), 0) };
});
assert.equal(official.characters.length, 91);
assert.equal(rows.length, 91);
assert.equal(new Set(rows.map(value => value.id)).size, 91);
assert.equal(new Set(official.characters.map(value => value.id)).size, 91);
assert.deepEqual(rows.map(value => value.id).sort(), official.characters.map(value => value.id).sort());

const comparisons = official.characters.map(character => {
  const actual = rows.find(value => value.id === character.id);
  assert.equal(actual.name, character.name, `Exact Korean subject name: ${character.id}`);
  assert.ok(character.roles.every(role => expected.has(role)), `Known official roles: ${character.id}`);
  const expectedFlags = character.roles.map(role => expected.get(role)[0]).sort();
  assert.deepEqual([...actual.flags].sort(), expectedFlags, `Exact official role set: ${character.id}`);
  return { id: character.id, name: character.name, officialRoles: character.roles, runtimeFlags: actual.flags,
    bitmask: actual.bitmask, match: true };
});
assert.ok(rows.every(value => !['lumi', 'wickeline'].includes(value.id)), 'NPCs excluded from subject roles');
const multiple = comparisons.filter(value => value.officialRoles.length > 1);
assert.equal(multiple.length, 19);
const result = {
  result: 'PASS', checkedAtUtc: new Date().toISOString(), independentOfGenerator: true,
  method: 'Separately parse the checked-in C# enum and entry block; compare every ID, Korean name and complete role bit set against the official-linked database JSON without executing or modifying the catalog generator.',
  officialSource: officialPath, officialSha256: sha(officialBytes),
  runtimeCatalog: catalogPath, catalogFileSha256AtCheck: sha(catalogBytes), roleEntryBlockSha256: sha(block),
  checkedSubjects: comparisons.length, checkedRoleEnumFlags: expected.size,
  missingIds: [], extraIds: [], nameMismatches: [], roleMismatches: [], duplicateIds: [],
  multipleRoleSubjects: multiple.length, allMultipleRolesPreserved: true, npcsExcluded: true,
  runtimeSourcesModified: false, comparisons,
};
await fs.writeFile(path.join(proof, 'official-catalog-crosscheck.json'), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify({ result: result.result, checkedSubjects: result.checkedSubjects, checkedRoleEnumFlags: result.checkedRoleEnumFlags,
  multipleRoleSubjects: result.multipleRoleSubjects, officialSha256: result.officialSha256,
  catalogFileSha256AtCheck: result.catalogFileSha256AtCheck, roleEntryBlockSha256: result.roleEntryBlockSha256 }, null, 2));
