import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { createHash } from 'node:crypto';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const directory = path.dirname(fileURLToPath(import.meta.url));
const workspace = path.resolve(directory, '../..');
const stage = path.join(workspace, 'Temp/SiteSaveConcurrency20261022');
const canonical = path.join(workspace, 'WebSite');
const baseline = '52378ec49d4b4469b077e6540551984d21c5ff96';
const sha = bytes => createHash('sha256').update(bytes).digest('hex');
const lf = bytes => bytes.toString('utf8').replace(/\r\n/g, '\n');
const proof = name => JSON.parse(fs.readFileSync(path.join(directory, name), 'utf8'));
const files = new Map();
for (const name of ['backend-validation.json', 'frontend-validation.json']) {
  const result = proof(name); assert.equal(result.result, 'PASS'); assert.equal(result.tests.failed, 0);
  for (const [relative, expected] of Object.entries(result.sourceHashes)) {
    assert(!path.isAbsolute(relative) && !relative.includes('\\') && !relative.split('/').includes('..'));
    assert(relative.startsWith('lib/') || relative.startsWith('app/') || relative.startsWith('scripts/'));
    assert(!relative.includes('node_modules') && !relative.endsWith('.tsbuildinfo'));
    const source = fs.readFileSync(path.join(stage, relative)); assert.equal(sha(source), expected);
    if (files.has(relative)) assert.equal(files.get(relative).expected, expected);
    const old = spawnSync('git', ['show', `${baseline}:${relative}`], { cwd: stage, encoding: null, windowsHide: true });
    const target = path.join(canonical, relative);
    const before = fs.existsSync(target) ? fs.readFileSync(target) : null;
    if (old.status === 0) assert(before && (lf(before) === lf(old.stdout) || sha(before) === expected), `Unexpected canonical edit: ${relative}`);
    else assert(!before || sha(before) === expected, `Unexpected existing added file: ${relative}`);
    files.set(relative, { expected, source, beforeHash: before && sha(before), guard: old.status === 0 ? 'baseline-or-already-current' : 'absent-or-already-current' });
  }
}
assert(files.size >= 4);
const preparation = proof('preparation.json');
assert.equal(sha(fs.readFileSync(path.join(canonical, 'lib/unity-assets-manifest.json'))), preparation.gameManifestSha256);
const changed = [];
for (const [relative, info] of files) {
  const target = path.join(canonical, relative); fs.mkdirSync(path.dirname(target), { recursive: true });
  fs.writeFileSync(target, info.source); assert.equal(sha(fs.readFileSync(target)), info.expected);
  changed.push({ path: relative, guard: info.guard, beforeSha256: info.beforeHash, sha256: info.expected });
}
const result = { result: 'PASS', completedAtUtc: new Date().toISOString(), baselineCommit: baseline,
  allFilesVerified: true, fileCount: changed.length, files: changed,
  gameBuildId: preparation.gameBuildId, gameManifestSha256: preparation.gameManifestSha256,
  excluded: ['.git', 'node_modules', 'build outputs', 'compiler cache', 'runtime game assets'] };
fs.writeFileSync(path.join(directory, 'canonical-sync.json'), JSON.stringify(result, null, 2));
console.log(JSON.stringify(result));
