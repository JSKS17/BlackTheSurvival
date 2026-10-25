import {readFile,writeFile} from 'node:fs/promises';
import assert from 'node:assert/strict';
const file=new URL('./Preview.mjs',import.meta.url);let text=await readFile(file,'utf8');
for(const [before,after] of [["message.type!=='bts:result')return;","!['bts:result','bts:local'].includes(message.type))return;"],["if(message.ok&&message.localPersisted)","if((message.ok||message.type==='bts:local')&&message.localPersisted)"]]) {assert.equal(text.split(before).length,2);text=text.replace(before,after);}
await writeFile(file,text);console.log('Local QA export response handler corrected. Product files unchanged.');
