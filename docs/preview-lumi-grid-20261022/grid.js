// Deterministic rendering of Tools/PrepareReferencePortraits.py and PrepareRarePortraits.py.
// No generated image is repainted, recolored, smoothed or changed on disk.
const canvas=document.querySelector('#preview'),ctx=canvas.getContext('2d');
const status=document.querySelector('#status');
const colors={bg:'#0b1420',panel:'#142234',border:'#344b60',text:'#e3ebef',muted:'#91a7bb',mint:'#89dfcf',gold:'#efd18b'};
let mode='compare';
const load=src=>new Promise((resolve,reject)=>{const image=new Image();image.onload=()=>resolve(image);image.onerror=reject;image.src=src;});
const make=(w,h)=>{const c=document.createElement('canvas');c.width=w;c.height=h;return c;};
const roundEven=value=>{const low=Math.floor(value),fraction=value-low;return fraction===.5?(low%2===0?low:low+1):Math.round(value);};
function inspect(image){
  const c=make(image.width,image.height),g=c.getContext('2d',{willReadFrequently:true});g.drawImage(image,0,0);
  const data=g.getImageData(0,0,c.width,c.height);let left=c.width,top=c.height,right=-1,bottom=-1;
  for(let y=0;y<c.height;y++)for(let x=0;x<c.width;x++)if(data.data[(y*c.width+x)*4+3]>0){left=Math.min(left,x);right=Math.max(right,x);top=Math.min(top,y);bottom=Math.max(bottom,y);}
  if(right<left)throw Error('Empty sprite');
  const width=right-left+1,height=bottom-top+1,scale=Math.min(72/width,72/height);
  return{image,data,left,top,width,height,w:Math.max(1,roundEven(width*scale)),h:Math.max(1,roundEven(height*scale))};
}
function grid(sprite){
  const c=make(80,80),g=c.getContext('2d'),out=g.createImageData(80,80);
  const dx=Math.floor((80-sprite.w)/2),dy=76-sprite.h;
  for(let y=0;y<sprite.h;y++)for(let x=0;x<sprite.w;x++){
    const sx=sprite.left+Math.min(sprite.width-1,Math.floor((x+.5)*sprite.width/sprite.w));
    const sy=sprite.top+Math.min(sprite.height-1,Math.floor((y+.5)*sprite.height/sprite.h));
    const source=(sy*sprite.image.width+sx)*4,destination=((dy+y)*80+dx+x)*4;
    if(sprite.data.data[source+3]>0)for(let k=0;k<4;k++)out.data[destination+k]=sprite.data.data[source+k];
  }
  g.putImageData(out,0,0);return c;
}
function twice(logical){const c=make(160,160),g=c.getContext('2d');g.imageSmoothingEnabled=false;g.drawImage(logical,0,0,160,160);return c;}
function save(c,name){const a=document.createElement('a');a.href=c.toDataURL('image/png');a.download=name;a.click();}
function text(value,x,y,size,color=colors.text){ctx.font=`${size}px Galmuri`;ctx.fillStyle=color;ctx.fillText(value,x,y);}
function checker(x,y,w,h){ctx.fillStyle='#101d2c';ctx.fillRect(x,y,w,h);ctx.fillStyle='#162537';for(let yy=0;yy<h;yy+=12)for(let xx=0;xx<w;xx+=12)if(((xx+yy)/12)%2===0)ctx.fillRect(x+xx,y+yy,Math.min(12,w-xx),Math.min(12,h-yy));}
const [nia,jackie,normal,virtual]=await Promise.all(['/nia.png','/jackie.png','/normal.png','/virtual.png'].map(load));
await document.fonts.load('18px Galmuri');await document.fonts.ready;
const sprites={normal:inspect(normal),virtual:inspect(virtual)};
for(const sprite of Object.values(sprites)){sprite.logical=grid(sprite);sprite.runtime=twice(sprite.logical);}
function drawCard(index,item){
  const x=38+index*304,y=175,w=292,h=405;
  ctx.fillStyle=colors.panel;ctx.fillRect(x,y,w,h);ctx.strokeStyle=colors.border;ctx.lineWidth=1;ctx.strokeRect(x+.5,y+.5,w-1,h-1);
  text(item.title,x+18,y+30,20,item.isNew?colors.mint:colors.gold);
  text(item.subtitle,x+18,y+54,12,colors.muted);
  const bx=x+26,by=y+82;checker(bx,by,240,240);
  ctx.strokeStyle='#32485b';ctx.beginPath();ctx.moveTo(bx,by+228.5);ctx.lineTo(bx+240,by+228.5);ctx.stroke();
  ctx.imageSmoothingEnabled=false;
  if(item.raw){const s=item.raw;const left=Math.floor((80-s.w)/2),top=76-s.h;ctx.drawImage(s.image,s.left,s.top,s.width,s.height,bx+left*3,by+top*3,s.w*3,s.h*3);}
  else ctx.drawImage(item.image,bx,by,240,240);
  text(item.note,x+18,y+356,14,item.isNew?colors.mint:colors.text);
  text(item.detail,x+18,y+379,11,colors.muted);
}
function render(){
  ctx.clearRect(0,0,1280,720);ctx.fillStyle=colors.bg;ctx.fillRect(0,0,1280,720);
  text('BLACK THE SURVIVAL / LUMI',40,40,13,colors.mint);
  text(mode==='compare'?'기존 실험체와 같은 80×80 격자':'루미 · 격자 적용 전후',40,88,31,colors.gold);
  text('72px 안에 맞춤 · 최근접 샘플링 · 투명 배경 · 같은 크기로 표시',40,125,15,colors.muted);
  const cards=mode==='compare'?[
    {title:'니아',subtitle:'기존 실험체 · 기준 스프라이트',image:nia,note:'기존 80×80 논리 격자',detail:'160×160 원본 / 동일 크기 표시'},
    {title:'재키',subtitle:'기존 실험체 · 기준 스프라이트',image:jackie,note:'기존 80×80 논리 격자',detail:'160×160 원본 / 동일 크기 표시'},
    {title:'루미',subtitle:'일반 조우 · 격자 적용 미리보기',image:sprites.normal.logical,isNew:true,note:'80×80 논리 격자 적용',detail:'기존 실험체와 같은 2배 저장 규칙'},
    {title:'버추얼 루미',subtitle:'레어 조우 · 격자 적용 미리보기',image:sprites.virtual.logical,isNew:true,note:'80×80 논리 격자 적용',detail:'기존 실험체와 같은 2배 저장 규칙'},
  ]:[
    {title:'루미 · 적용 전',subtitle:'원본을 같은 크기로 표시',raw:sprites.normal,note:'고해상도 원본',detail:'격자 축소 전'},
    {title:'루미 · 적용 후',subtitle:'기존 실험체와 같은 필터',image:sprites.normal.logical,isNew:true,note:'80×80 논리 격자',detail:'색상 변경 없이 최근접 축소'},
    {title:'버추얼 · 적용 전',subtitle:'원본을 같은 크기로 표시',raw:sprites.virtual,note:'고해상도 원본',detail:'격자 축소 전'},
    {title:'버추얼 · 적용 후',subtitle:'기존 실험체와 같은 필터',image:sprites.virtual.logical,isNew:true,note:'80×80 논리 격자',detail:'색상 변경 없이 최근접 축소'},
  ];cards.forEach((item,index)=>drawCard(index,item));
  text('한 논리 픽셀 = 이 미리보기의 3×3픽셀',40,628,17,colors.mint);
  text('디자인·색상·투명도를 유지한 격자 처리 미리보기입니다.',40,661,14,colors.muted);
  text('웹 배포 전 미리보기',1040,692,12,colors.muted);
  document.querySelector('#compare').setAttribute('aria-pressed',String(mode==='compare'));
  document.querySelector('#before').setAttribute('aria-pressed',String(mode==='before'));
  const exportLink=document.querySelector('#download');
  exportLink.href=canvas.toDataURL('image/png');
  exportLink.download=mode==='compare'?'lumi-grid-comparison.png':'lumi-grid-before-after.png';
  status.textContent='준비 완료 · 80px 논리 격자';
}
document.querySelector('#compare').onclick=()=>{mode='compare';render();};
document.querySelector('#before').onclick=()=>{mode='before';render();};
document.querySelector('#normal-file').href=sprites.normal.runtime.toDataURL('image/png');
document.querySelector('#virtual-file').href=sprites.virtual.runtime.toDataURL('image/png');
render();
