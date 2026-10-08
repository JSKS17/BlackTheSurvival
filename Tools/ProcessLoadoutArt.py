"""Preserve imagegen originals and resize additional gear/D icons to the UI grid."""
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from ProcessChibiArt import alpha_metrics, pixel_digest

ROOT = Path(__file__).resolve().parents[1]
DOC = ROOT / 'docs/art-system'

def digest(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

def process():
    manifest = json.loads((DOC/'loadout-art-manifest.json').read_text(encoding='utf-8'))
    records = []
    missing = []
    originals = DOC/'loadout-generated'
    originals.mkdir(exist_ok=True)
    for e in manifest:
        path = DOC/'loadout-generation'/f"{e['id']}.json"
        if not path.exists():
            missing.append(e['id'])
            continue
        record = json.loads(path.read_text(encoding='utf-8'))
        if record.get('tool') != 'image_gen.imagegen':
            record['tool']='image_gen.imagegen'
            path.write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        source = originals/f"{e['id']}.png"
        if not source.exists():
            shutil.copyfile(record['originalGeneratedPath'], source)
        image = Image.open(source).convert('RGBA').resize((28,28),Image.Resampling.NEAREST)
        padded = Image.new('RGBA',(32,32),(0,0,0,0))
        padded.paste(image,(2,2))
        image = padded.resize((64,64),Image.Resampling.NEAREST)
        destination = ROOT/e['output']
        destination.parent.mkdir(parents=True,exist_ok=True)
        image.save(destination)
        low,high=image.getchannel('A').getextrema()
        if low!=0 or high<230:
            raise ValueError(f"Missing transparent or opaque alpha: {e['id']}")
        records.append(dict(id=e['id'],kind=e['kind'],path=e['output'],source=source.relative_to(ROOT).as_posix(),promptRecord=path.relative_to(ROOT).as_posix(),fileSha256=digest(destination),sourceSha256=digest(source),size=[64,64],logicalSize=[32,32]))
    (DOC/'loadout-asset-index.json').write_text(json.dumps(records,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    # Extend the existing skill-icon coverage without reprocessing historical atlases.
    current_manifest=ROOT/'docs/art-chibi/game-art-manifest.json'
    current_index=ROOT/'docs/art-chibi/processed-art-index.json'
    art_index=json.loads(current_index.read_text(encoding='utf-8'))
    new_ids={e['id'] for e in manifest if e['kind']=='weapon'}
    art_index['assets']=[e for e in art_index['assets'] if e['id'] not in new_ids]
    art_index['manifestSha256']=digest(current_manifest)
    for e in records:
        if e['kind']!='weapon':
            continue
        source=ROOT/e['source']
        original=Image.open(source).convert('RGBA')
        image=Image.open(ROOT/e['path']).convert('RGBA')
        art_index['assets'].append(dict(id=e['id'],kind='icon',name=next(a['name'] for a in manifest if a['id']==e['id']),path=e['path'],width=64,height=64,fileSha256=e['fileSha256'],pixelSha256=pixel_digest(image),batch='individual_loadout_'+e['id'],source=e['source'],sourceSha256=e['sourceSha256'],sourceSize=list(original.size),columns=1,rows=1,column=0,row=0,cropBox=[0,0,original.width,original.height],sourceAlpha=alpha_metrics(original),outputAlpha=alpha_metrics(image),extraction='individual-nearest-padded'))
    current_index.write_text(json.dumps(art_index,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    font=ImageFont.truetype(str(ROOT/'Assets/Resources/Lumia/Galmuri11.ttf'),13)
    sheet=Image.new('RGB',(8*160,max(1,(len(records)+7)//8)*143),'#17212f')
    draw=ImageDraw.Draw(sheet)
    names={e['id']:e['name'] for e in manifest}
    for i,e in enumerate(records):
        im=Image.open(ROOT/e['path']).resize((96,96),Image.Resampling.NEAREST)
        x,y=i%8*160,i//8*143
        sheet.paste(im,(x+32,y+3),im)
        draw.text((x+5,y+103),names[e['id']],fill='white',font=font)
        draw.text((x+5,y+121),e['id'],fill='#9cafc4',font=font)
    sheet.save(DOC/'loadout-contact-sheet.png')
    print(json.dumps(dict(complete=len(records),expected=len(manifest),missing=missing),ensure_ascii=False))
    return len(missing)==0

if __name__=='__main__':
    process()
