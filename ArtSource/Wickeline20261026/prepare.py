"""Apply the requested existing 80px grid and derive its face icon; never repaint."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import hashlib, json, re, shutil, uuid

ROOT = Path(__file__).resolve().parents[2]
ART = Path(__file__).resolve().parent
raw = ART / 'generated/wickeline.png'
if not raw.exists():
    shutil.copy2(Path(r'C:\Users\azxar\.codex\generated_images\01a13e49-1228-7931-8aa7-feb1794908fb\exec-4a932b6e-9d31-4a11-b564-651babdf6771.png'), raw)
source = Image.open(raw).convert('RGBA')
assert source.getchannel('A').getextrema() == (0, 255)
# Ignore near-invisible generator edge haze when measuring, preserving sampled RGBA.
box = source.getchannel('A').point(lambda value: 255 if value >= 128 else 0).getbbox()
crop = source.crop(box)
scale = min(72 / crop.width, 72 / crop.height)
size = (max(1, round(crop.width * scale)), max(1, round(crop.height * scale)))
logical = Image.new('RGBA', (80, 80))
logical.alpha_composite(crop.resize(size, Image.Resampling.NEAREST), ((80-size[0])//2, 76-size[1]))
portrait = ROOT / 'Assets/Resources/Lumia/Portraits/wickeline.png'
logical.resize((160,160),Image.Resampling.NEAREST).save(portrait)
# Crop exactly the already-filtered head, including the respirator. No redrawing.
face_box = (17, 4, 61, 48)
face = logical.crop(face_box)
icon = ROOT / 'Assets/Resources/Lumia/SystemIcons/wickeline.png'
face_frame = Image.new('RGBA',(48,48))
face_frame.alpha_composite(face,(2,2))
face_frame.resize((96,96),Image.Resampling.NEAREST).save(icon)
for target in (portrait, icon):
    meta = target.with_suffix('.png.meta')
    if not meta.exists():
        template = (ROOT/'Assets/Resources/Lumia/Portraits/lumi.png.meta').read_text(encoding='utf8')
        template = re.sub(r'^guid: .+$', 'guid: '+uuid.uuid5(uuid.NAMESPACE_URL, target.relative_to(ROOT).as_posix()).hex, template, flags=re.M)
        template = template.replace('textureCompression: 1', 'textureCompression: 0')
        if target == icon:
            template = template.replace('maxTextureSize: 256', 'maxTextureSize: 128')
        meta.write_text(template,encoding='utf8')
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
records = []
for path in (portrait,icon):
    image = Image.open(path).convert('RGBA')
    assert image.getchannel('A').getextrema()[0] == 0 and image.getchannel('A').getextrema()[1] >= 250
    assert image.tobytes() == image.resize((image.width//2,image.height//2),Image.Resampling.NEAREST).resize(image.size,Image.Resampling.NEAREST).tobytes()
    records.append(dict(path=path.relative_to(ROOT).as_posix(),sha256=sha(path),size=list(image.size),matching2xBlocks=True,transparent=True,metaSha256=sha(path.with_suffix('.png.meta'))))
proof = dict(result='PASS',generator='builtin image_gen',prompt='prompts.json',finalRefinementPrompt='refinement-prompt.json',raw='generated/wickeline.png',rawSha256=sha(raw),alphaCropBox=list(box),alphaBoundingThreshold=128,logicalGrid=[80,80],subjectFits=[72,72],resizedSize=list(size),anchor='x=(80-w)//2,y=76-h',faceCropLogical=list(face_box),processing='Alpha-bounds crop ignoring near-invisible generator haze, nearest-neighbour sampling preserving sampled RGBA, transparent framing, exact 2x enlargement, face crop only; no recoloring or painting.',assets=records,sources=[dict(role='official character concept',page='https://store.steampowered.com/news/posts/?appids=1049590&enddate=1575619255',image='https://clan.akamai.steamstatic.com/images/34875433/c8f9adedba055b7d81058373871823c5c68c6388.png',local='references/official-concept.png'),dict(role='official model',image='https://clan.akamai.steamstatic.com/images/34875433/db3a650519a6f449ec76f9e129aaf91d754599ed.png',local='references/official-model.png'),dict(role='existing game pixel style',local='../../Assets/Resources/Lumia/Portraits/nia.png')])
(ART/'validation.json').write_text(json.dumps(proof,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
preview=Image.new('RGB',(1120,560),'#0b1420'); d=ImageDraw.Draw(preview)
font=ImageFont.truetype(r'C:\Windows\Fonts\malgun.ttf',23)
small=ImageFont.truetype(r'C:\Windows\Fonts\malgun.ttf',16)
d.text((32,20),'BLACK THE SURVIVAL / 위클라인 격자 적용 미리보기',fill='#efd18b',font=font)
for x,label,path in [(32,'니아 · 기존 스프라이트',ROOT/'Assets/Resources/Lumia/Portraits/nia.png'),(306,'루미 · 기존 스프라이트',ROOT/'Assets/Resources/Lumia/Portraits/lumi.png'),(580,'위클라인 · 신규 스프라이트',portrait)]:
    d.rectangle((x,78,x+250,452),fill='#142234',outline='#344b60')
    d.text((x+12,94),label,fill='#e3ebef',font=small)
    sprite=Image.open(path).convert('RGBA').resize((240,240),Image.Resampling.NEAREST)
    preview.paste(sprite,(x+5,150),sprite)
    d.text((x+12,419),'80×80 논리 격자 / 투명 배경',fill='#89dfcf',font=small)
d.text((862,94),'지도 얼굴 아이콘',fill='#e3ebef',font=small)
icon_image=Image.open(icon).resize((192,192),Image.Resampling.NEAREST)
preview.paste(icon_image,(862,174),icon_image)
d.text((32,490),'기존 실험체와 같은 최근접 격자 필터 · Point 렌더링 · 색상 변경 없음',fill='#91a7bb',font=small)
preview.save(ART/'grid-preview.png')
print(json.dumps(proof,ensure_ascii=False))
