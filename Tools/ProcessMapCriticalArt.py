"""Preserve individual imagegen outputs and export kiosk/critical icons mechanically.

Only alpha-bound cropping, nearest-neighbour scaling and transparent padding are
performed here. Subject silhouettes and pixels originate in image_gen outputs.
"""
from __future__ import annotations
import hashlib
import json
import re
import shutil
import uuid
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
DOC = ROOT / 'docs/art-system'

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def save(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def process():
    records = read(DOC/'map-critical-generation-records.json')
    gear = {e['id']:e for e in read(DOC/'loadout-art-manifest.json')}
    gear.update({e['id']:dict(e,kind='gear') for e in read(DOC/'critical-gear-references.json')})
    names = {'kiosk':'키오스크','encounter':'실험체 조우','subject':'실험체 전투','wildlife':'야생동물 전투'}
    reference_style = dict(referencePath='Assets/Resources/Lumia/SystemIcons/radar.png',
        referenceUrl='https://github.com/JSKS17/BlackTheSurvival/blob/main/Assets/Resources/Lumia/SystemIcons/radar.png',
        referenceRole='existing project pixel UI palette and shape language; new map symbol')
    manifests, index = [], []
    originals, prompts = DOC/'map-critical-generated', DOC/'map-critical-generation'
    originals.mkdir(exist_ok=True)
    prompts.mkdir(exist_ok=True)
    meta_template = (ROOT/'Assets/Resources/Lumia/SystemIcons/empress.png.meta').read_text(encoding='utf-8')
    for record in records:
        asset_id = record['id']
        if asset_id in gear:
            manifest = dict(gear[asset_id])
            manifest['referenceRole'] = 'original Eternal Return equipment silhouette and color'
        else:
            manifest = dict(id=asset_id,name=names[asset_id],kind='map',**reference_style)
            if asset_id == 'kiosk':
                manifest.update(referencePath='docs/art-system/references/map-critical/kiosk-minimap-original.png',
                    referenceUrl='https://support.playeternalreturn.com/hc/article_attachments/43409459849369',
                    referenceRole='original Eternal Return minimap kiosk C-arrow marker')
            manifest['output'] = f'Assets/Resources/Lumia/SystemIcons/{asset_id}.png'
        source = originals/f'{asset_id}.png'
        if not source.exists():
            shutil.copyfile(record['originalGeneratedPath'], source)
        original = Image.open(source).convert('RGBA')
        # Ignore imperceptible transparent-edge noise when framing the authored icon.
        box = original.getchannel('A').point(lambda p: 255 if p >= 32 else 0).getbbox()
        if not box:
            raise ValueError(f'Empty generated image: {asset_id}')
        crop = original.crop(box)
        scale = min(28 / crop.width, 28 / crop.height)
        dimensions = (max(1,round(crop.width*scale)),max(1,round(crop.height*scale)))
        resized = crop.resize(dimensions, Image.Resampling.NEAREST)
        logical = Image.new('RGBA',(32,32),(0,0,0,0))
        logical.paste(resized,((32-dimensions[0])//2,(32-dimensions[1])//2))
        export = logical.resize((64,64),Image.Resampling.NEAREST)
        output = ROOT/manifest['output']
        export.save(output)
        meta = Path(str(output)+'.meta')
        if not meta.exists():
            meta.write_text(re.sub(r'^guid: [0-9a-f]+$', 'guid: '+uuid.uuid4().hex, meta_template, flags=re.M), encoding='utf-8')
        prompt = dict(manifest,**{k:v for k,v in record.items() if k not in manifest})
        inputs = [manifest['referencePath']]
        if manifest['referencePath'] != 'Assets/Resources/Lumia/SystemIcons/radar.png':
            inputs.append('Assets/Resources/Lumia/SystemIcons/radar.png')
        prompt.update(generatedSource=source.relative_to(ROOT).as_posix(),referenceInputs=inputs)
        prompt_path = prompts/f'{asset_id}.json'
        save(prompt_path,prompt)
        row = dict(id=asset_id,kind=manifest['kind'],path=manifest['output'],source=source.relative_to(ROOT).as_posix(),
            promptRecord=prompt_path.relative_to(ROOT).as_posix(),fileSha256=digest(output),sourceSha256=digest(source),
            referenceSha256=digest(ROOT/manifest['referencePath']),size=[64,64],logicalSize=[32,32],sourceSize=list(original.size),
            cropBox=list(box),contentSize=list(dimensions),extraction='alpha-bounds-crop-nearest-fit-28-pad-32-nearest-2x')
        manifests.append(manifest)
        index.append(row)
    save(DOC/'map-critical-art-manifest.json',manifests)
    save(DOC/'map-critical-asset-index.json',index)
    font=ImageFont.truetype(str(ROOT/'Assets/Resources/Lumia/Galmuri11.ttf'),13)
    sheet=Image.new('RGB',(5*230,2*172),'#17212f')
    draw=ImageDraw.Draw(sheet)
    for i,m in enumerate(manifests):
        x,y=i%5*230,i//5*172
        reference=Image.open(ROOT/m['referencePath']).convert('RGBA')
        reference.thumbnail((72,72),Image.Resampling.NEAREST)
        generated=Image.open(ROOT/m['output']).resize((96,96),Image.Resampling.NEAREST)
        sheet.paste(reference,(x+12,y+14),reference)
        sheet.paste(generated,(x+116,y+3),generated)
        draw.text((x+9,y+104),m['name'],fill='white',font=font)
        draw.text((x+9,y+124),m['id'],fill='#9cafc4',font=font)
        draw.text((x+9,y+145),'reference        runtime',fill='#9cafc4',font=font)
    sheet.save(DOC/'map-critical-contact-sheet.png')
    # Comparison atlas for the user's request to fix any clearly wrong gear icons.
    all_gear = {e['id']:e for e in read(DOC/'item-manifest.json') if e['kind']=='gear'}
    all_gear.update({e['id']:e for e in read(DOC/'loadout-art-manifest.json') if e['kind']=='gear'})
    all_gear.update({e['id']:e for e in manifests if e['kind']=='gear'})
    comparison=Image.new('RGB',(5*230,((len(all_gear)+4)//5)*147),'#17212f')
    draw=ImageDraw.Draw(comparison)
    for i,m in enumerate(all_gear.values()):
        x,y=i%5*230,i//5*147
        reference=Image.open(ROOT/m['referencePath']).convert('RGBA')
        reference.thumbnail((84,84),Image.Resampling.NEAREST)
        runtime=Image.open(ROOT/m['output']).convert('RGBA').resize((96,96),Image.Resampling.NEAREST)
        comparison.paste(reference,(x+10,y+7),reference)
        comparison.paste(runtime,(x+116,y+2),runtime)
        draw.text((x+6,y+101),m['name'],fill='white',font=font)
        draw.text((x+6,y+123),m['id'],fill='#9cafc4',font=font)
    comparison.save(DOC/'gear-silhouette-comparison.png')
    print(json.dumps(dict(individualImagegenIcons=len(index),newIcons=8,replacedIcons=2,gearCompared=len(all_gear)),ensure_ascii=False))

if __name__=='__main__':
    process()
