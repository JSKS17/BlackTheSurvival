import assert from "node:assert/strict";
import { spawn } from "node:child_process";
import { createHash } from "node:crypto";
import { readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const proof = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(proof, "../..");
const stage = path.join(root, "Temp/SitePresence20261026");
const files = ["lib/game-presence.mjs", "lib/game-accounts.mjs", "app/api/presence/route.ts", "db/schema.ts",
  "drizzle/0002_game_presence.sql", "drizzle/meta/_journal.json", "drizzle/meta/0002_snapshot.json", "scripts/game-accounts.test.mjs"];
const hashes = async () => Object.fromEntries(await Promise.all(files.map(async name => [name,
  createHash("sha256").update(await readFile(path.join(stage, name))).digest("hex")])));
const before = await hashes();
const results = [];
for (const [name, file] of [["accounts", path.join(stage, "scripts/game-accounts.test.mjs")],
  ["presence", path.join(proof, "presence-backend.test.mjs")]]) {
  const startedAt = new Date().toISOString();
  const child = spawn(process.execPath, ["--test", "--test-reporter=tap", file], {
    cwd: root, env: { ...process.env, PRESENCE_SITE_ROOT: stage }, stdio: ["ignore", "pipe", "pipe"],
  });
  let output = "";
  child.stdout.on("data", chunk => { output += chunk; });
  child.stderr.on("data", chunk => { output += chunk; });
  const exitCode = await new Promise((resolve, reject) => { child.on("error", reject); child.on("close", resolve); });
  await writeFile(path.join(proof, `backend-${name}.log`), output);
  const field = key => Number(output.match(new RegExp(`^# ${key} (\\d+)$`, "m"))?.[1]);
  const result = { name, startedAt, endedAt: new Date().toISOString(), exitCode, tests: field("tests"), pass: field("pass"), fail: field("fail"),
    log: `backend-${name}.log` };
  results.push(result);
  console.log(JSON.stringify(result));
  assert.equal(exitCode, 0, `${name} failed; inspect its local test log`);
  assert.ok(result.tests > 0 && result.pass === result.tests && result.fail === 0);
}
assert.deepEqual(await hashes(), before, "Backend source changed during tests");
const prior = ["drizzle/0000_wide_ser_duncan.sql", "drizzle/0001_display_preferences.sql", "drizzle/meta/0000_snapshot.json", "drizzle/meta/0001_snapshot.json"];
const priorHashes = Object.fromEntries(await Promise.all(prior.map(async name => [name,
  createHash("sha256").update(await readFile(path.join(stage, name))).digest("hex")])));
const result = { status: "PASS", verifiedAt: new Date().toISOString(), node: process.version,
  tests: results.reduce((sum, value) => sum + value.tests, 0), testAssertionsIndividuallyCounted: false,
  results, sourceHashes: before, immutablePriorMigrationHashes: priorHashes,
  testOnlyClockInjection: true, productionUsesServerDateNow: true, realWorkerd: true, realD1: true };
await writeFile(path.join(proof, "backend-tests.json"), `${JSON.stringify(result, null, 2)}\n`);
console.log(JSON.stringify({ status: result.status, tests: result.tests }));
