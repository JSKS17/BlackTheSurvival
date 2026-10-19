// Apply the explicitly approved Nia eye patch after mechanical art preparation.
// Source hashes, alpha and every pixel outside bounded eye windows must agree.
import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const require=createRequire(path.join(root,'WebSite/package.json')),sharp=require('sharp');
const sha=b=>createHash('sha256').update(b).digest('hex');
const manifest=JSON.parse(await fs.readFile(path.join(root,'docs/art-nia-black-eyes/manifest.json'),'utf8'));
for(const source of manifest.sources)assert.equal(sha(await fs.readFile(path.join(root,source.path))),source.afterSha256,'Approved generated source '+source.path);
if(process.argv.includes('--verify-reproduction')){
 for(const asset of manifest.runtime){
  const beforeBytes=await fs.readFile(path.join(root,asset.beforeBackup));assert.equal(sha(beforeBytes),asset.beforeSha256);
  const {data,info}=await sharp(beforeBytes).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  for(const w of asset.restorationWindows){const approved=Buffer.from(w.approvedRgbaBase64,'base64');for(let y=w.top;y<w.top+w.height;y++)for(let x=w.left;x<w.left+w.width;x++){const i=(y*info.width+x)*4,j=((y-w.top)*w.width+x-w.left)*4;assert.equal(data[i+3],approved[j+3]);approved.copy(data,i,j,j+3);}}
  const output=await sharp(data,{raw:{width:info.width,height:info.height,channels:4}}).png({compressionLevel:9}).toBuffer();assert.equal(sha(output),asset.afterSha256,'Read-only reproduction '+asset.path);
 }
 console.log(JSON.stringify({result:'PASS',readOnlyReproductions:manifest.runtime.length,filesWritten:0}));
}else if(process.argv.includes('--check')){
 for(const asset of[...manifest.runtime,...manifest.mirrors])assert.equal(sha(await fs.readFile(path.join(root,asset.path))),asset.afterSha256,'Approved current asset '+asset.path);
 console.log(JSON.stringify({result:'PASS',runtime:manifest.runtime.length,sources:manifest.sources.length,mirrors:manifest.mirrors.length}));
}else{
 const index=process.argv.indexOf('--target'),target=index>=0?process.argv[index+1]:null;
 const asset=manifest.runtime.find(a=>a.path===target);assert(asset,'Use --check or --target with one exact approved runtime path.');
 const destination=path.join(root,asset.path),currentBytes=await fs.readFile(destination);
 if(sha(currentBytes)===asset.afterSha256){console.log(JSON.stringify({result:'PASS',path:asset.path,alreadyApplied:true}));}
 else{
  const {data,info}=await sharp(currentBytes).ensureAlpha().raw().toBuffer({resolveWithObject:true});assert.equal(info.width,asset.width);assert.equal(info.height,asset.height);
  const alpha=Buffer.alloc(info.width*info.height),windows=Buffer.alloc(alpha.length),outside=[];
  for(const w of asset.restorationWindows)for(let y=w.top;y<w.top+w.height;y++)for(let x=w.left;x<w.left+w.width;x++)windows[y*info.width+x]=255;
  for(let p=0;p<alpha.length;p++){alpha[p]=data[p*4+3];if(!windows[p])outside.push(data.subarray(p*4,p*4+4));}
  assert.equal(sha(alpha),asset.alphaSha256,'A generated image changed alpha.');
  assert.equal(sha(Buffer.concat(outside)),asset.outsideRestorationWindowsSha256,'Generated pose/outfit/framing or non-eye pixels differ from the approved original.');
  for(const w of asset.restorationWindows){const approved=Buffer.from(w.approvedRgbaBase64,'base64');for(let y=w.top;y<w.top+w.height;y++)for(let x=w.left;x<w.left+w.width;x++){const i=(y*info.width+x)*4,j=((y-w.top)*w.width+x-w.left)*4;assert.equal(data[i+3],approved[j+3]);approved.copy(data,i,j,j+3);}}
  const output=await sharp(data,{raw:{width:info.width,height:info.height,channels:4}}).png({compressionLevel:9}).toBuffer();assert.equal(sha(output),asset.afterSha256,'Approved patch reproduction hash');await fs.writeFile(destination,output);
  console.log(JSON.stringify({result:'PASS',path:asset.path,applied:true,sha256:sha(output)}));
 }
}
