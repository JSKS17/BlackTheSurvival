"""Copy imagegen originals, then perform format-only pixel-grid resizing.

Generated alpha is preserved. No repainting, compositing, background removal,
colour quantization, sharpening or synthesis occurs in this processor.
"""
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art-system'
ASSETS=ROOT/'Assets/Resources/Lumia/SystemIcons'

def digest(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def process():
    ASSETS.mkdir(parents=True,exist_ok=True)
    (OUT/'generated').mkdir(parents=True,exist_ok=True)
    entries=[]
    for record in sorted((OUT/'generation').glob('*.json')):
        e=json.loads(record.read_text(encoding='utf8'))
        asset_id=e['id']
        source=OUT/'generated'/f'{asset_id}.png'
        if not source.exists(): shutil.copyfile(e['originalGeneratedPath'],source)
        image=Image.open(source).convert('RGBA').resize((28,28),Image.Resampling.NEAREST)
        padded=Image.new('RGBA',(32,32),(0,0,0,0))
        padded.paste(image,(2,2))
        image=padded.resize((64,64),Image.Resampling.NEAREST)
        dest=ASSETS/f'{asset_id}.png'
        image.save(dest)
        alpha=image.getchannel('A').histogram()
        entries.append(dict(id=asset_id,path=dest.relative_to(ROOT).as_posix(),
                            source=source.relative_to(ROOT).as_posix(),
                            promptRecord=record.relative_to(ROOT).as_posix(),
                            fileSha256=digest(dest),sourceSha256=digest(source),
                            size=[64,64],logicalSize=[32,32],contentSize=[28,28],padding=[2,2,2,2],
                            transparentPixels=alpha[0],visiblePixels=4096-alpha[0]))
    (OUT/'asset-index.json').write_text(json.dumps(entries,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    font=ImageFont.truetype(str(ROOT/'Assets/Resources/Lumia/Galmuri11.ttf'),14)
    sheet=Image.new('RGB',(8*180,((len(entries)+7)//8)*155),'#17212f')
    draw=ImageDraw.Draw(sheet)
    for i,e in enumerate(entries):
        im=Image.open(ROOT/e['path']).resize((96,96),Image.Resampling.NEAREST)
        x,y=i%8*180,i//8*155
        sheet.paste(im,(x+42,y+5),im)
        draw.text((x+4,y+106),e['id'],fill='white',font=font)
    sheet.save(OUT/'system-contact-sheet.png')
    print('SYSTEM ICONS',len(entries),flush=True)

if __name__=='__main__': process()
