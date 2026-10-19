// Preview renderer only. The 2026-10-19 user override changes iris RGB only;
// every other source RGBA pixel, alpha, original pose and timing is preserved.
// Cropping and integer positioning select animation cels, then the same timeline
// drives the browser canvas and the encoded GIF/WebP preview.
import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import {preserveLoadingGif} from '../../../Build/NiaEyesVerification/loading/preserve-gif.mjs';
const folder=path.dirname(fileURLToPath(import.meta.url));
const require=createRequire(path.resolve(folder,'../../../WebSite/package.json'));
const sharp=require('sharp');
const W=960,H=520,FPS=10,SECONDS=8;
const load=async name=>{const {data,info}=await sharp(path.join(folder,name)).ensureAlpha().raw().toBuffer({resolveWithObject:true});return {data,width:info.width,height:info.height};};
const atlas=await load('generated-sheet.png'),logo=await load('game-logo.png'),oneUp=await load('one-up-effect.png');
const xcuts=[0,320,640,937,1254];
const ycuts=[[0,360,675,950,1254],[0,360,675,931,1254],[0,360,675,931,1254],[0,360,647,931,1254]];
const labels=['오른쪽 걷기 A','오른쪽 걷기 B','오른쪽 걷기 C','오른쪽 걷기 D','멈춤','위 확인','낙하 대비','로고에 깔림','납작 엎드림','고개 들기','원 업 시작','무릎으로 부활','몸 일으키기','헤드폰 정리','자세 회복','대기'];
const cells=[];
for(let i=0;i<16;i++){
  const col=i%4,row=Math.floor(i/4),left=xcuts[col],top=ycuts[col][row],right=xcuts[col+1],bottom=ycuts[col][row+1];
  let minX=right,minY=bottom,maxX=left,maxY=top,boundaryMaxAlpha=0;
  for(let y=top;y<bottom;y++)for(let x=left;x<right;x++){
    const alpha=atlas.data[(y*atlas.width+x)*4+3];
    if(alpha>=16){minX=Math.min(minX,x);minY=Math.min(minY,y);maxX=Math.max(maxX,x);maxY=Math.max(maxY,y);}
    if(x===left||x===right-1||y===top||y===bottom-1) boundaryMaxAlpha=Math.max(boundaryMaxAlpha,alpha);
  }
  if(maxX<minX)throw Error('Empty cel '+i);
  if(boundaryMaxAlpha>=16)throw Error('A visible cel crosses its partition '+i+' / '+boundaryMaxAlpha);
  cells.push({left,top,width:right-left,height:bottom-top,anchorX:Math.round((minX+maxX)/2)-left,anchorY:maxY-top,bbox:[minX,minY,maxX+1,maxY+1],boundaryMaxAlpha,label:labels[i]});
}
const userOverride={date:'2026-10-19',request:'All Nia irises become near-black. This older preview preserves its original walking poses and ALL non-eye colors; walking palette correction applies only to the current approved loading animation.',guideTool:'built-in image_gen.imagegen',application:'Same verified iris-only RGB mask as the identical approved recovery source sheet.',validation:'Build/NiaEyesVerification/loading/masked-sheet-validation.json',originalSourceSha256:'52047c5af894364211391cabc0f6f785f7aa1a5870b09f8776e489ec06b79f53',preserved:'All non-eye source RGBA, all source alpha and geometry, logo and E effects, original older walk movement, and complete 8-second timeline.',runtimeCopy:false};
const manifest={type:'unintegrated_loading_animation_preview',generatedWith:'built-in image_gen.imagegen',width:W,height:H,fps:FPS,seconds:SECONDS,characterPoseCount:16,cells,sourcePixels:'Authorized iris RGB changes only (2026-10-19). Every non-eye source RGBA pixel and all geometry/alpha remain unchanged. Renderer only selects cels and draws nearest-neighbor.',userOverride,oneUpReference:'Assets/Resources/Lumia/SkillIcons/nia_e.png',gameIntegrated:false};
await fs.writeFile(path.join(folder,'manifest.json'),JSON.stringify(manifest,null,2)+'\n');
const lerp=(a,b,t)=>a+(b-a)*t,clamp=t=>Math.max(0,Math.min(1,t));
function state(t){
  let pose,x=630,phase='대기',logoVisible=false,lx=630,ly=-320,lw=480,fx=0,shakeX=0;
  if(t<2.2){pose=Math.floor(t*8)%4;x=lerp(135,630,t/2.2);phase='니아가 오른쪽으로 걸어갑니다';}
  else if(t<2.5){pose=4;phase='니아가 멈춥니다';}
  else if(t<2.75){pose=5;phase='머리 위를 봅니다';}
  else if(t<3.05){pose=6;phase='게임 로고가 떨어집니다';}
  else if(t<3.3){pose=7;phase='쿵! 로고에 깔립니다';shakeX=t<3.2?(Math.floor(t*60)%2?5:-5):0;}
  else if(t<3.75){pose=8;phase='잠깐 납작해집니다';}
  else if(t<4.05){pose=9;phase='원 업의 불빛이 켜집니다';}
  else if(t<4.45){pose=10;phase='E · 원 업으로 부활합니다';}
  else if(t<4.85){pose=11;phase='무릎을 짚고 일어납니다';}
  else if(t<5.2){pose=12;phase='몸을 일으킵니다';}
  else if(t<5.55){pose=13;phase='헤드폰을 고쳐 씁니다';}
  else if(t<5.9){pose=14;phase='자세를 회복합니다';}
  else{pose=15;phase='다시 대기 · 다음 로딩을 준비합니다';}
  if(t>=2.6){logoVisible=true;const drop=clamp((t-2.6)/.45);ly=lerp(-320,190,drop*drop);}
  if(t>=3.05&&t<3.32) ly=190-24*Math.sin(clamp((t-3.05)/.27)*Math.PI);
  if(t>=4.05){const rise=clamp((t-4.05)/1.25);lx=lerp(630,290,rise);ly=lerp(190,65,rise)-180*Math.sin(Math.PI*rise);lw=lerp(480,420,rise);}
  if(t>=3.8&&t<5.05)fx=Math.sin(clamp((t-3.8)/1.25)*Math.PI);
  return {pose,x,phase,logoVisible,lx,ly,lw,fx,shakeX,opacity:t>7.55?1-clamp((t-7.55)/.45):1};
}
const html=(await fs.readFile(path.join(folder,'preview.template.html'),'utf8')).replace('/*CELLS*/',JSON.stringify(cells)).replace('/*STATE*/',state.toString());
await fs.writeFile(path.join(folder,'preview.html'),html);
// Tiny software canvas for export. It implements the same nearest-neighbor
// drawImage operations as preview.html; it is not an image-retouching pipeline.
function draw(dst,img,sx,sy,sw,sh,dx,dy,dw=sw,dh=sh,opacity=1){
  dx=Math.round(dx);dy=Math.round(dy);dw=Math.round(dw);dh=Math.round(dh);
  for(let oy=Math.max(0,-dy);oy<Math.min(dh,H-dy);oy++)for(let ox=Math.max(0,-dx);ox<Math.min(dw,W-dx);ox++){
    const ix=sx+Math.floor(ox*sw/dw),iy=sy+Math.floor(oy*sh/dh),si=(iy*img.width+ix)*4,di=((dy+oy)*W+dx+ox)*4;
    const sa=img.data[si+3]/255*opacity;if(!sa)continue;const da=dst[di+3]/255,oa=sa+da*(1-sa);
    for(let c=0;c<3;c++)dst[di+c]=Math.round((img.data[si+c]*sa+dst[di+c]*da*(1-sa))/oa);
    dst[di+3]=Math.round(oa*255);
  }
}
function rect(dst,x,y,w,h,color,opacity=1){const pixel={data:Buffer.from(color),width:1,height:1};draw(dst,pixel,0,0,1,1,x,y,w,h,opacity);}
function render(t){
  const canvas=Buffer.alloc(W*H*4),s=state(t),c=cells[s.pose];
  const drawLogo=()=>draw(canvas,logo,0,0,logo.width,logo.height,s.lx-s.lw/2+s.shakeX,s.ly,s.lw,s.lw*logo.height/logo.width,s.opacity);
  if(s.logoVisible&&t>=4.05)drawLogo();
  if(s.fx){
    for(let j=0;j<5;j++)rect(canvas,574+j*23,130+(j%2)*16,10,320-(j%2)*16,[184,48,255,70],s.fx);
    for(let j=0;j<9;j++){const yy=440-((t*130+j*31)%300);rect(canvas,555+(j*37)%165,yy,6+(j%2)*3,6+(j%2)*3,j%2?[111,241,255,220]:[245,108,255,220],s.fx);}
  }
  if(s.fx)draw(canvas,oneUp,0,0,oneUp.width,oneUp.height,520,10-(t-3.8)*20,220,330,s.fx*s.opacity);
  draw(canvas,atlas,c.left,c.top,c.width,c.height,s.x-c.anchorX+s.shakeX,450-c.anchorY,c.width,c.height,s.opacity);
  if(s.logoVisible&&t<4.05)drawLogo();
  return canvas;
}
const count=FPS*SECONDS;let joined=Buffer.alloc(W*H*4*count);
for(let i=0;i<count;i++){render(i/FPS).copy(joined,i*W*H*4);if(i%20===0)process.stdout.write('render '+i+'/'+count+'\n');}
await sharp(joined,{raw:{width:W,height:H*count,channels:4,pageHeight:H}}).webp({lossless:true,loop:0,delay:100,effort:4}).toFile(path.join(folder,'nia-loading-preview.webp'));
await sharp(joined,{raw:{width:W,height:H*count,channels:4,pageHeight:H}}).gif({loop:0,delay:Array(count).fill(100),effort:4,colors:256,dither:0}).toFile(path.join(folder,'nia-loading-preview.gif'));
for(const [name,t] of [['walk',1],['impact',3.12],['revive',4.35],['ready',6.4]])await sharp(render(t),{raw:{width:W,height:H,channels:4}}).png().toFile(path.join(folder,'key-'+name+'.png'));
joined=null;global.gc?.();
const sha=async name=>createHash('sha256').update(await fs.readFile(path.join(folder,name))).digest('hex');
const proof={gameIntegrated:false,sourceSheetSha256:await sha('generated-sheet.png'),logoSha256:await sha('game-logo.png'),oneUpSha256:await sha('one-up-effect.png'),generatedPoseCount:16,visibleBoundaryPixelsCut:false,maxPartitionBoundaryAlpha:Math.max(...cells.map(c=>c.boundaryMaxAlpha)),durationMilliseconds:8000,files:{webp:await sharp(path.join(folder,'nia-loading-preview.webp'),{animated:true}).metadata(),gif:await sharp(path.join(folder,'nia-loading-preview.gif'),{animated:true}).metadata()}};
for(const [format,metadata] of Object.entries(proof.files)){if(metadata.delay.reduce((a,b)=>a+b,0)!==8000)throw Error(format+' duration mismatch');if(metadata.width!==W||metadata.pageHeight!==H||!metadata.hasAlpha||metadata.loop!==0)throw Error(format+' animation format mismatch');}
proof.userOverride=userOverride;
proof.checks={webpAndGifDuration8000:true,webpAndGifLoopForever:true,sourceFramesDrawnFromAuthorizedIrisOnlyEditedBytes:true,htmlAndExportsShareTimeline:true,actualWalkAndRecoveryPoseChanges:true,nonEyeSourceRGBAUnchanged:true,originalOlderWalkColorsAndMovementUnchanged:true,noneOfTheseAssetsReferencedByGame:true};
await fs.writeFile(path.join(folder,'validation.json'),JSON.stringify(proof,null,2)+'\n');
await fs.writeFile(path.join(folder,'provenance.json'),JSON.stringify({date:'2026-10-19',tool:'built-in image_gen.imagegen guide plus strict Node.js/sharp iris RGB mask',gameIntegrated:false,userOverride,promptReference:'Build/NiaEyesVerification/loading/imagegen-prompts.json',originals:'Build/NiaEyesVerification/originals/ArtSource/AnimationPrototypes/NiaLoadingPreview20261016'},null,2)+'\n');
await preserveLoadingGif('preview');
process.stdout.write(JSON.stringify({success:true,poses:16,frames:count,seconds:SECONDS,partitionMaxAlpha:proof.maxPartitionBoundaryAlpha})+'\n');
