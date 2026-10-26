import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
import assert from 'node:assert/strict';

const proof = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(proof, '../..');
const hash = value => createHash('sha256').update(value).digest('hex');
const utf8 = file => fs.readFile(file, 'utf8');
const parse = async file => JSON.parse(await utf8(path.join(proof, file)));
const write = (file, value) => fs.writeFile(path.join(proof, file), JSON.stringify(value, null, 2) + '\n', { flag: 'wx' });
const apiRoot = 'https://comm-api.game.naver.com/nng_main/v1/game/db/character';
const roles = ['탱커', '전사', '암살자', '스킬 딜러', '원거리 딜러', '지원가'];
const startedAtUtc = new Date().toISOString();
async function publicSource(file, url) {
  try { return await utf8(path.join(proof, file)); }
  catch (error) {
    if (error.code !== 'ENOENT') throw error;
    const response = await fetch(url, { signal: AbortSignal.timeout(30_000) });
    if (!response.ok) throw new Error(`Official source HTTP ${response.status}: ${url}`);
    return response.text();
  }
}
const sourceFiles = ['GameDatabase.cs', 'ClassicRoster.cs', 'RecentRoster.cs'];
const rosterSources = {};
const roster = [];
for (const file of sourceFiles) {
  const text = await utf8(path.join(root, 'Assets/Scripts/Lumia', file));
  rosterSources[`Assets/Scripts/Lumia/${file}`] = hash(text);
  if (file === 'GameDatabase.cs') {
    const line = text.split(/\r?\n/).find(value => value.includes('foreach (var pair in new []'));
    for (const match of line.matchAll(/new\[\]\s*\{\s*"([^"]+)"\s*,\s*"([^"]+)"\s*\}/g)) roster.push({ id: match[1], name: match[2] });
  } else {
    const pattern = file === 'ClassicRoster.cs'
      ? /\bSubject\(\s*cards\s*,\s*passives\s*,\s*characters\s*,\s*"([^"]+)"\s*,\s*"([^"]+)"/g
      : /\bP\(\s*passives\s*,\s*characters\s*,\s*"([^"]+)"\s*,\s*"([^"]+)"/g;
    for (const match of text.matchAll(pattern)) roster.push({ id: match[1], name: match[2] });
  }
}
assert.equal(roster.length, 91);
assert.equal(new Set(roster.map(value => value.id)).size, 91);
assert.equal(new Set(roster.map(value => value.name)).size, 91);

const indexSource = `${apiRoot}/GM_670/dataType/character?order=ranking&offset=0&limit=200`;
const indexRaw = await publicSource('official-db-characters.json', indexSource);
const index = JSON.parse(indexRaw);
assert.equal(index.totalCount, 91);
assert.equal(index.result.character.length, 91);
const officialIndex = index.result.character.map(value => ({ objectId: value.objectId, name: value.name }));
assert.deepEqual([...roster.map(value => value.name)].sort(), [...officialIndex.map(value => value.name)].sort());
await write('official-db-character-index.json', { source: indexSource, responseSha256: hash(indexRaw), totalCount: 91, characters: officialIndex });

const membership = [];
for (const role of roles) {
  const source = `${indexSource}&f1Name=${encodeURIComponent('역할군')}&f1Value=${encodeURIComponent(role)}`;
  const raw = await publicSource(`official-role-${role.replaceAll(' ', '-')}.json`, source);
  const data = JSON.parse(raw);
  const values = data.result.character.map(value => ({ objectId: value.objectId, name: value.name }));
  assert.equal(values.length, data.result.countWithIsRanking.filteredTotalCount);
  membership.push({ role, source, responseSha256: hash(raw), count: values.length, characters: values });
}
await write('official-db-role-membership.json', { roles: membership });

let cursor = 0, completed = 0;
const details = new Array(roster.length);
await Promise.all(Array.from({ length: 4 }, async () => {
  while (cursor < roster.length) {
    const position = cursor++;
    const character = roster[position];
    const official = officialIndex.find(value => value.name === character.name);
    const url = `${apiRoot}/objectId/${official.objectId}`;
    let text;
    for (let attempt = 0; attempt < 3; attempt++) {
      try {
        const response = await fetch(url, { signal: AbortSignal.timeout(30_000) });
        if (!response.ok) throw new Error(`Official detail HTTP ${response.status}: ${official.objectId}`);
        text = await response.text();
        break;
      } catch (error) {
        if (attempt === 2) throw error;
      }
    }
    const data = JSON.parse(text);
    assert.equal(data.intro.name, character.name);
    assert.equal(data.intro.objectId, official.objectId);
    const rawRoles = data.intro.filters.filter(value => value.name === '역할군').map(value => value.value);
    const detailRoles = roles.filter(role => rawRoles.some(value => value.split(/\s*[,/|·]\s*/).includes(role)));
    const filterRoles = membership.filter(value => value.characters.some(item => item.objectId === official.objectId)).map(value => value.role);
    assert.ok(detailRoles.length > 0, `Missing explicit official role: ${character.id}`);
    assert.deepEqual(detailRoles, filterRoles, `Detail and role filters differ: ${character.id} / ${rawRoles.join('; ')}`);
    details[position] = { ...character, officialObjectId: official.objectId, roles: detailRoles,
      source: `https://game.naver.com/lounge/Eternal_Return/db/character/${official.objectId}`,
      apiSource: url, responseSha256: hash(text), roleFields: rawRoles,
      weaponFields: data.intro.filters.filter(value => value.name === '무기').map(value => value.value) };
    completed++;
    if (completed % 10 === 0 || completed === roster.length) console.log(`Official role detail verified: ${completed}/91`);
  }
}));

const categoryRaw = await publicSource('official-db-category.json', 'https://comm-api.game.naver.com/nng_main/v1/game/category/GM_670?mid=db&dataType=character');
const output = {
  schemaVersion: 1,
  retrievedAtUtc: startedAtUtc,
  sourceScope: 'Role labels from the character database directly linked by the official Eternal Return homepage; every detail role is cross-checked against all six official-linked database role filters.',
  sources: [
    { id: 'official-home', title: '이터널 리턴 공식 홈페이지: 게임 → 실험체', url: 'https://playeternalreturn.com/main?hl=ko-KR', linksTo: 'https://game.naver.com/lounge/Eternal_Return/db/character' },
    { id: 'official-linked-character-database', title: '공식 홈페이지 연결 실험체 도감', url: 'https://game.naver.com/lounge/Eternal_Return/db/character' },
    { id: 'official-role-categories', url: 'https://comm-api.game.naver.com/nng_main/v1/game/category/GM_670?mid=db&dataType=character', responseSha256: hash(categoryRaw), roles },
    { id: 'official-character-index', url: indexSource, responseSha256: hash(indexRaw), count: 91 },
    ...membership.map(value => ({ id: `official-role-${value.role}`, url: value.source, responseSha256: value.responseSha256, count: value.count }))
  ],
  characters: details.map(({ roleFields, weaponFields, responseSha256, ...value }) => value)
};
await write('official-roles.json', output);
await write('official-role-detail-evidence.json', { retrievedAtUtc: startedAtUtc, characters: details });
const multiRole = details.filter(value => value.roles.length > 1).map(({ id, name, roles }) => ({ id, name, roles }));
const validation = { result: 'PASS', startedAtUtc, completedAtUtc: new Date().toISOString(), rosterSources,
  gameCharacters: roster.length, officialCharacters: officialIndex.length,
  matchedNames: details.length, missingNames: [], missingRoles: [], ambiguousMappings: [],
  officialRoleFiltersCrossChecked: roles.length, detailsCrossChecked: details.length,
  roleCounts: Object.fromEntries(membership.map(value => [value.role, value.count])),
  singleRoleCharacters: details.length - multiRole.length, multipleRoleCharacters: multiRole.length, multiRole,
  ignoredMalformedRoleFields: details.flatMap(value => value.roleFields.filter(role => !roles.includes(role)).map(role => ({ id: value.id, name: value.name, field: '역할군', value: role }))),
  externalRuntimeCallsRequired: false,
  officialRolesSha256: hash(JSON.stringify(output, null, 2) + '\n') };
await write('validation.json', validation);
console.log(JSON.stringify({ result: validation.result, gameCharacters: validation.gameCharacters, detailsCrossChecked: validation.detailsCrossChecked,
  roleCounts: validation.roleCounts, singleRoleCharacters: validation.singleRoleCharacters, multipleRoleCharacters: validation.multipleRoleCharacters }, null, 2));
