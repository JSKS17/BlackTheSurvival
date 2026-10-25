import assert from 'node:assert/strict';
import {readFile,writeFile} from 'node:fs/promises';
const here=new URL('./',import.meta.url);
let text=await readFile(new URL('../validation-wickeline-20261026/Cleanup.ps1',here),'utf8');
text=text.replaceAll('validation-wickeline-20261026','validation-presence-20261026').replaceAll('SiteWickeline20261026','SitePresence20261026').replaceAll('WickelineRelease20261026','PresenceRelease20261026').replaceAll('Web127','Web128').replaceAll('1.0.27','1.0.28').replaceAll('version_number -le 53','version_number -le 55').replaceAll('Site version 53','Site version 55');
const block=/\$github = Read-Proof 'github-publish-state\.json'\r?\nif \([^\n]+\r?\n\$windows = Read-Proof 'windows-build-manifest\.json'\r?\nif \([^\n]+\r?\n/;
assert(block.test(text));text=text.replace(block,'');
await writeFile(new URL('Cleanup.ps1',here),text,{flag:'wx'});
console.log('Presence-only cleanup gates prepared; no Windows/GitHub release requirement for this web UI update.');
