"""Prepare separately generated chibi sprites using crop/padding/nearest resize only.

The user-provided style illustration is never shipped or copied into an asset.
Each character is independently generated from the official identity reference.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import uuid
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
DOC = ROOT / 'docs/art-reference-chibi'
OUT = ROOT / 'Assets/Resources/Lumia/PortraitsReference'
REFS = ROOT / 'docs/art-chibi/reference-characters.json'
STYLE = ROOT / 'Temp/ReferenceChibi/style-reference.png'
NAMESPACE = uuid.UUID('844d6983-46f7-4479-9ba3-8d35f533f52d')
COMMON = '''Use case: stylized-concept. Asset type: one NEW transparent pixel-art character sprite for a cute chibi deckbuilding game.
Input image 1 is ONLY the official character identity reference: preserve its distinctive hair, eye color, age, face features, outfit, and signature prop. Input image 2 is ONLY a style guide supplied by the user, not their artwork: study its compact proportions and pixel clustering but do NOT trace, copy its pose, reproduce its exact sprite, include its background or watermark, or borrow Rio's bow/clothes for another character.
Draw a fresh original sprite in this style: very cute compact 2.1 to 2.3 heads tall, head approximately 43 to 47 percent of full character height, broad rounded head, tiny compact torso, very short legs and small hands. Soft muted material colors, clean dark-purple/brown 1-pixel contours and selective colored interior outlines, chunky deliberate square pixel clusters, 3 flat shades per material, small restrained highlights. Pixel craft suitable for an 80 x 80 logical grid with the complete character fitting inside about 72 x 72 pixels. Eyes and mouth stay clearly readable: use distinct upper eyelids and 4-6 pixel irises, restrained cheeks, character-specific eyebrows and expression. Never give everyone identical round shiny eyes. Adult men retain facial hair, mature eye shapes, and defining features while remaining chibi.
One full-body character only, three-quarter front view facing slightly right, a fresh balanced idle/battle-ready pose, complete feet and signature prop visible with transparent padding on all sides. Square composition. Genuine transparent background. No text, label, watermark, signature, scenery, floor, cast shadow, glow cloud or frame. No smooth painting, anti-aliasing, gradients, 3D render, realistic anatomy, tall long-legged body, or oversized weapon hiding the face. Keep color clusters economical and edges crisp.
'''

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(path.name + '.' + str(os.getpid()) + '.tmp')
    temporary.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf8')
    temporary.replace(path)

def save_png(image, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(path.name + '.' + str(os.getpid()) + '.tmp')
    image.save(temporary, format='PNG')
    temporary.replace(path)

def prepare():
    faces = read(ROOT / 'docs/art-identity/face-designs.json')
    override_path = DOC / 'prompt-overrides.json'
    overrides = read(override_path) if override_path.exists() else {}
    (DOC / 'prompts').mkdir(parents=True, exist_ok=True)
    (DOC / 'common-prompt.txt').write_text(COMMON, encoding='utf8')
    for ref in read(REFS):
        prompt = COMMON + '\nCharacter: ' + ref['name'] + ' (' + ref['id'] + ').\n'
        prompt += 'Identity/outfit: ' + ref['appearance'] + '\n'
        prompt += 'Signature prop: ' + ref.get('signature_prop', '') + '\n'
        prompt += 'Prop constraints: ' + ref.get('prop_avoid', '') + '\n'
        prompt += 'Character-specific face from the original: ' + faces[ref['id']] + '\n'
        prompt = overrides.get(ref['id'], prompt)
        (DOC / 'prompts' / (ref['id'] + '.txt')).write_text(prompt, encoding='utf8')
    write_json(DOC / 'style-reference.json', dict(
        role='Style guidance only; user states this Rio sprite is not their own creation.',
        source='User-provided attachment, 2026-10-11', sha256=sha(STYLE),
        redistributed=False, copiedIntoGame=False,
        interpretation='Compact 2.1-2.3-head proportions, soft material palettes, distinct faces and economical square pixel clusters. New poses and drawings for every character.',
        logicalCanvas=[80,80], runtimeCanvas=[160,160], integerPixelScale=2))
    print('Prepared 92 independent image-generation prompts.')

def adopt(asset_id, source):
    ref = next(r for r in read(REFS) if r['id'] == asset_id)
    raw = DOC / 'generated' / (asset_id + '.png')
    source = Path(source).resolve()
    if raw.exists() and sha(raw) != sha(source):
        archive = DOC / 'generated/versions'
        archive.mkdir(parents=True, exist_ok=True)
        shutil.copy2(raw, archive / (asset_id + '_' + sha(raw)[:12] + '.png'))
    raw.parent.mkdir(parents=True, exist_ok=True)
    if source != raw.resolve(): shutil.copy2(source, raw)
    image = Image.open(raw).convert('RGBA')
    alpha = image.getchannel('A')
    if alpha.getextrema()[0] != 0: raise ValueError('The generated image has no real transparent background: ' + asset_id)
    box = alpha.getbbox()
    if not box: raise ValueError('Empty image: ' + asset_id)
    crop = image.crop(box)
    scale = min(72 / crop.width, 72 / crop.height)
    size = max(1,round(crop.width*scale)),max(1,round(crop.height*scale))
    sprite = crop.resize(size, Image.Resampling.NEAREST)
    logical = Image.new('RGBA',(80,80))
    logical.alpha_composite(sprite,((80-size[0])//2,76-size[1]))
    runtime = logical.resize((160,160),Image.Resampling.NEAREST)
    target = OUT / (asset_id + '.png')
    save_png(runtime,target)
    metadata = (ROOT / 'Assets/Resources/Lumia/PortraitsIdentity' / (asset_id+'.png.meta')).read_text(encoding='utf8')
    metadata = re.sub(r'^guid: .+$','guid: '+uuid.uuid5(NAMESPACE,asset_id).hex,metadata,flags=re.M)
    target.with_suffix('.png.meta').write_text(metadata,encoding='utf8')
    prompt = (DOC/'prompts'/(asset_id+'.txt')).read_text(encoding='utf8')
    record = dict(id=asset_id,name=ref['name'],tool='builtin image_gen',intent='new illustration with style and identity references',
        originalReference=ref['reference_path'],originalSourceUrl=ref['original_url'],referenceSha256=sha(ROOT/ref['reference_path']),
        styleReferenceSha256=sha(STYLE),styleReferenceCopied=False,prompt=prompt,
        generatedSource=raw.relative_to(ROOT).as_posix(),generatedSha256=sha(raw),generatedSize=list(image.size),alphaCropBox=list(box),
        logicalCanvas=[80,80],resizedSize=list(size),integerPixelScale=2,path=target.relative_to(ROOT).as_posix(),
        width=160,height=160,sha256=sha(target),processing='Alpha crop, nearest-neighbour resize to fit 72px, transparent padding to 80px and exact 2x nearest enlargement only.')
    write_json(DOC/'generation'/(asset_id+'.json'),record)
    print(json.dumps(dict(id=asset_id,path=record['path'],sha256=record['sha256']),ensure_ascii=False))

def rebuild():
    refs=read(REFS)
    records=[]
    errors=[]
    for ref in refs:
        path=DOC/'generation'/(ref['id']+'.json')
        if path.exists(): records.append(read(path))
    for rec in records:
        for key, digest in [('path','sha256'),('generatedSource','generatedSha256'),('originalReference','referenceSha256')]:
            if sha(ROOT/rec[key])!=rec[digest]: errors.append(rec['id']+': '+digest)
        for source in rec.get('inputReferences', []):
            if source['role'].startswith('style guidance'): continue
            if sha(ROOT/source['path'])!=source['sha256']: errors.append(rec['id']+': input reference '+source['path'])
        image=Image.open(ROOT/rec['path']).convert('RGBA')
        if image.size!=(160,160): errors.append(rec['id']+': runtime size')
        test=image.resize((80,80),Image.Resampling.NEAREST).resize((160,160),Image.Resampling.NEAREST)
        if image.tobytes()!=test.tobytes(): errors.append(rec['id']+': broken 2x grid')
        if image.getchannel('A').getextrema()!=(0,255): errors.append(rec['id']+': alpha')
        if (DOC/'prompts'/(rec['id']+'.txt')).read_text(encoding='utf8')!=rec['prompt']: errors.append(rec['id']+': prompt')
    if len({r['sha256'] for r in records})!=len(records): errors.append('duplicate runtime images')
    if len({r['generatedSha256'] for r in records})!=len(records): errors.append('duplicate generated images')
    expected={r['id'] for r in refs};actual={r['id'] for r in records}
    report=dict(expected=len(refs),actual=len(records),missing=sorted(expected-actual),errors=errors,complete=expected==actual and not errors,
        logicalCanvas=[80,80],runtimeCanvas=[160,160],integerPixelScale=2,styleReferenceRedistributed=False)
    write_json(DOC/'asset-index.json',dict(schema=1,assets=records))
    write_json(DOC/'validation.json',report)
    font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',15)
    title=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',22)
    for page in range((len(records)+11)//12):
        sheet=Image.new('RGB',(1024,760),(25,30,42));draw=ImageDraw.Draw(sheet)
        draw.text((18,8),'새 치비 도트 · 80px 논리 격자 · '+str(page+1),font=title,fill=(235,239,252))
        for i,rec in enumerate(records[page*12:page*12+12]):
            x,y=(i%4)*256,48+(i//4)*236
            sprite=Image.open(ROOT/rec['path']).convert('RGBA').resize((208,208),Image.Resampling.NEAREST)
            sheet.paste(sprite,(x+24,y),sprite)
            draw.text((x+16,y+209),rec['name']+' · '+rec['id'],font=font,fill=(215,223,238))
        save_png(sheet,DOC/'contact-sheets'/('sprites_%02d.png'%(page+1)))
    print(json.dumps(report,ensure_ascii=False))

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--prepare',action='store_true');parser.add_argument('--adopt',action='store_true')
    parser.add_argument('--rebuild',action='store_true');parser.add_argument('--id');parser.add_argument('--source')
    args=parser.parse_args()
    if args.prepare: prepare()
    if args.adopt: adopt(args.id,args.source)
    if args.rebuild: rebuild()
