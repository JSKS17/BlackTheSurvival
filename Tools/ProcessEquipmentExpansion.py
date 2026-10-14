"""Export six authored equipment icons by alpha crop and nearest scaling only."""
import hashlib
import json
import re
import shutil
import uuid
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'docs/art-system'
def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def write(path, value): path.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def digest(path): return hashlib.sha256(path.read_bytes()).hexdigest()
manifest = {m['id']:m for m in read(ART/'equipment-expansion-manifest.json')}
records = read(ART/'equipment-expansion-generation-records.json')
sources = ART/'expansion-generated'
prompts = ART/'expansion-generation'
sources.mkdir(exist_ok=True)
prompts.mkdir(exist_ok=True)
meta_template = (ROOT/'Assets/Resources/Lumia/SystemIcons/radar.png.meta').read_text(encoding='utf-8')
index=[]
for record in records:
    entry=manifest[record['id']]
    source=sources/(record['id']+'.png')
    if not source.exists(): shutil.copyfile(record['originalGeneratedPath'],source)
    original=Image.open(source).convert('RGBA')
    box=original.getchannel('A').point(lambda x:255 if x>=32 else 0).getbbox()
    if not box: raise ValueError('Empty image: '+record['id'])
    crop=original.crop(box)
    scale=min(28/crop.width,28/crop.height)
    size=(max(1,round(crop.width*scale)),max(1,round(crop.height*scale)))
    fitted=crop.resize(size,Image.Resampling.NEAREST)
    logical=Image.new('RGBA',(32,32),(0,0,0,0))
    logical.paste(fitted,((32-size[0])//2,(32-size[1])//2))
    output=ROOT/entry['output']
    logical.resize((64,64),Image.Resampling.NEAREST).save(output)
    meta=Path(str(output)+'.meta')
    if not meta.exists(): meta.write_text(re.sub(r'^guid: [0-9a-f]+$','guid: '+uuid.uuid4().hex,meta_template,flags=re.M),encoding='utf-8')
    prompt_path=prompts/(record['id']+'.json')
    write(prompt_path,dict(entry,**record,generatedSource=source.relative_to(ROOT).as_posix()))
    index.append(dict(id=record['id'],kind='gear',path=entry['output'],
        source=source.relative_to(ROOT).as_posix(),promptRecord=prompt_path.relative_to(ROOT).as_posix(),
        referenceSha256=digest(ROOT/entry['referencePath']),sourceSha256=digest(source),
        fileSha256=digest(output),size=[64,64],logicalSize=[32,32],sourceSize=list(original.size),
        cropBox=list(box),contentSize=list(size),extraction='alpha-bounds-crop-nearest-fit-28-pad-32-nearest-2x'))
write(ART/'equipment-expansion-asset-index.json',index)
sheet=Image.new('RGB',(6*192,176),'#17212f')
draw=ImageDraw.Draw(sheet)
font=ImageFont.truetype(str(ROOT/'Assets/Resources/Lumia/Galmuri11.ttf'),13)
for i,row in enumerate(records):
    entry=manifest[row['id']]
    ref=Image.open(ROOT/entry['referencePath']).convert('RGBA')
    ref.thumbnail((76,76),Image.Resampling.NEAREST)
    export=Image.open(ROOT/entry['output']).resize((96,96),Image.Resampling.NEAREST)
    sheet.paste(ref,(i*192+4,16),ref)
    sheet.paste(export,(i*192+91,4),export)
    draw.text((i*192+5,110),entry['name'],font=font,fill='white')
    draw.text((i*192+5,132),entry['id'],font=font,fill='#9cafc4')
    draw.text((i*192+5,153),'original      runtime',font=font,fill='#9cafc4')
sheet.save(ART/'equipment-expansion-contact-sheet.png')
print('Exported six distinct equipment icons (32 logical / 64 runtime).')
