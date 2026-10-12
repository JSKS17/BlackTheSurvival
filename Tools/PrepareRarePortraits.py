"""Preserve independently generated rare sprites; alpha-crop/pad/nearest-scale only.

Generation uses the built-in image_gen tool, once per character. This script does
not generate, redraw, recolor, remove backgrounds, or synthesize an atlas asset.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import uuid
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
DOC = ROOT / 'docs/art-rare-skins'
OUT = ROOT / 'Assets/Resources/Lumia/PortraitsRare'
BASE_REFS = ROOT / 'docs/art-chibi/reference-characters.json'
MANIFEST = DOC / 'skin-manifest.json'
STYLE = ROOT / 'Temp/ReferenceChibi/style-reference.png'
NAMESPACE = uuid.UUID('a3fe068c-3790-4c13-8db9-b1d11cf59994')
COMMON = '''Use case: stylized-concept. Asset type: one NEW transparent pixel-art rare-encounter character sprite for Black The Survival.
Draw a fresh original sprite, matching the game's compact cute chibi style: 2.1 to 2.3 heads tall, head about 43-47 percent of full height, broad rounded head, tiny compact torso, very short legs and small hands. Muted material colors, crisp dark-purple/brown one-pixel contours, selective colored interior outlines, deliberate square pixel clusters, about three flat shades per material and restrained highlights. Design for an 80 x 80 logical grid, with the entire character and prop fitting within about 72 x 72 pixels.
Keep each character's own recognizable face: readable distinct upper eyelids and 4-6-pixel irises, eyebrows, small mouth, restrained cheeks. Preserve character-specific eye shapes, age, facial hair, glasses and expression rather than repeating a generic face.
One full-body character only, three-quarter front view facing slightly right, a newly drawn balanced idle/battle-ready pose. Entire feet and signature prop visible, transparent padding on all sides. Square composition with a genuinely transparent background. No text, label, watermark, signature, scenery, floor, shadow, glow cloud, UI, frame, sprite sheet or additional characters. No smooth painting, anti-aliasing, gradients, 3D, realistic anatomy, long-legged body or weapon obscuring the face.
The user-provided Rio fan sprite is STYLE GUIDANCE ONLY: study compact proportions and pixel clustering, never trace its pixels, copy its pose, reproduce its sprite, watermark, bow or clothes. It must not become part of any shipped asset.
'''
CUSTOM = {
    'lucia': {
        'skinNameKR': '별빛 산책 루치아',
        'visualDescription': 'Original casual outfit: a short cream cardigan over a pale-lavender blouse, dark violet pleated skirt, white ankle socks and violet loafers. Keep her pale-silver side buns, violet hair ribbons and clear cyan eyes. A small violet shoulder bag has a tiny pink crystal clasp. Carry her recognizable ornate silver-and-violet long-barrel musket with a shoulder stock, held low beside the body; no pair of short pistols. Earnest lively expression with raised inner brows and a small open mouth. This is a newly designed fan casual outfit, not an official skin.'},
    'ceres': {
        'skinNameKR': '느긋한 외출 세레스',
        'visualDescription': 'Original casual outfit: a soft ivory ribbed turtleneck, short slate-blue open jacket, dark navy tailored shorts over charcoal tights, and simple black ankle boots. Keep her neat silver bob and narrow pale-blue eyes with flat upper lids, straight low brows and a composed stern closed mouth. Replace the maid lace headpiece with a plain narrow black hairband. Carry her recognizable black-and-gold greatsword with one magenta crystal near the guard, resting diagonally beside the body without hiding the face. This is a newly designed fan casual outfit, not an official skin.'},
    'craver': {
        'skinNameKR': '골목의 휴일 크레이버',
        'visualDescription': 'Original casual outfit: a light-gray open bomber jacket over a burgundy hoodie, charcoal cargo trousers, black-and-red sneakers and dark fingerless gloves. Preserve his messy spiky vivid red-black hair, round red-tinted spectacles over sharply narrow amber eyes, low stern brows and cold straight mouth. Hold his recognizable large black-and-red revolver low, with a visible cylinder and no glow effects replacing its silhouette. This is a newly designed fan casual outfit, not an official skin.'}
}


def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def write_json(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(path.name + '.' + str(os.getpid()) + '.tmp')
    temporary.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf8')
    temporary.replace(path)


def save_png(image, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(path.name + '.' + str(os.getpid()) + '.tmp')
    image.save(temporary, format='PNG')
    temporary.replace(path)


def entries():
    base = {ref['id']: ref for ref in read(BASE_REFS) if ref['id'] != 'hana'}
    data = read(MANIFEST) if MANIFEST.exists() else {'skins': []}
    skins = data.get('skins', data if isinstance(data, list) else [])
    result = {}
    for skin in skins:
        asset_id = skin.get('id', skin.get('subjectId'))
        if asset_id not in base:
            continue
        entry = dict(base[asset_id], **skin)
        entry['id'] = asset_id
        entry['referencePath'] = skin.get('referencePath', skin.get('localReferencePath', entry['reference_path']))
        entry['skinNameKR'] = skin.get('skinNameKR', skin.get('skinName', ''))
        entry['sourceURL'] = skin.get('sourceURL', skin.get('sourceUrl', entry['original_url']))
        entry['customOutfit'] = bool(skin.get('customOutfit', asset_id in CUSTOM))
        result[asset_id] = entry
    # The three new subjects have no alternate official skin. Their identity
    # references remain official, but their casual clothing is newly designed.
    for asset_id, custom in CUSTOM.items():
        entry = result.get(asset_id, dict(base[asset_id]))
        entry.update(custom)
        entry.update(id=asset_id, customOutfit=True, status='original casual design',
                     referencePath=base[asset_id]['reference_path'], sourceURL=base[asset_id]['original_url'])
        result[asset_id] = entry
    return result, base


def reference_inputs(entry):
    refs = [dict(path=entry['referencePath'], role='official character identity reference' if entry['customOutfit'] else 'official selected skin appearance reference')]
    if not entry['customOutfit'] and entry['referencePath'] != entry['reference_path'] and entry['id'] != 'alonso':
        refs.append(dict(path=entry['reference_path'], role='official default identity reference; face and identity only, not outfit'))
    if STYLE.exists():
        refs.append(dict(path=STYLE.relative_to(ROOT).as_posix(), role='style guidance only; user-provided fan art, not copied or redistributed'))
    for ref in refs:
        path = ROOT / ref['path']
        if not path.exists():
            raise FileNotFoundError('Missing input reference: ' + str(path))
        ref['sha256'] = sha(path)
    return refs


def prepare(asset_ids=None, force_prompt=False):
    selected, base = entries()
    faces = read(ROOT / 'docs/art-identity/face-designs.json')
    overrides = read(DOC / 'prompt-overrides.json') if (DOC / 'prompt-overrides.json').exists() else {}
    (DOC / 'prompts').mkdir(parents=True, exist_ok=True)
    (DOC / 'common-prompt.txt').write_text(COMMON, encoding='utf8')
    write_json(DOC / 'custom-outfits.json', CUSTOM)
    for asset_id, entry in selected.items():
        if asset_ids and asset_id not in asset_ids:
            continue
        refs = reference_inputs(entry)
        common = COMMON
        if asset_id == 'debi_marlene':
            common = common.replace('One full-body character only,', 'Exactly two full-body sisters together as one paired Debi-and-Marlene sprite asset,')
            common = common.replace('or additional characters.', 'or a third character.')
        prompt = common + '\nInput references, in this exact order:\n'
        prompt += '\n'.join('Image ' + str(index + 1) + ': ' + ref['role'] + '.' for index, ref in enumerate(refs)) + '\n'
        prompt += '\nCharacter: ' + entry['name'] + ' (' + asset_id + ').\n'
        prompt += 'Rare outfit: ' + entry['skinNameKR'] + '.\n'
        prompt += 'Outfit/weapon reference: ' + entry.get('visualDescription', '') + '\n'
        face = faces[asset_id]
        if asset_id == 'ceres':
            face = face.replace('White lace headpiece.', 'Plain narrow black hairband for this new casual outfit; no maid lace headpiece.')
        if asset_id == 'alonso':
            face = 'The selected skin has a crimson RED DEMON SKULL MASK with a bony nose opening, bared gold fangs, swept red horns and glowing amber eyes. It is NOT the default blue-gray metal respirator. No blue-gray faceplate, police mask or human mouth.'
        prompt += 'Character-specific face from the original: ' + face + '\n'
        if not entry['customOutfit']:
            prompt += 'Recreate the selected skin\'s visible costume, color placements, hairstyle, accessories and weapon silhouette; the default reference must not override the skin outfit. Do not invent sunglasses, hats or props absent from the selected skin. Selected skin appearance and its written description override the default face note for hair style/color, eye color and accessories. Keep character-specific expression/eye shape, but do not add default glasses/hats absent from the selected skin.\n'
        prompt = overrides.get(asset_id, prompt)
        existing = DOC / 'prompts' / (asset_id + '.txt')
        generated = DOC / 'generation' / (asset_id + '.json')
        if not force_prompt and generated.exists() and existing.exists() and existing.read_text(encoding='utf8') != prompt:
            continue  # Never silently replace the actual prompt of an adopted asset.
        existing.write_text(prompt, encoding='utf8')
    old_style = read(ROOT / 'docs/art-reference-chibi/style-reference.json')
    write_json(DOC / 'style-reference.json', old_style)
    write_json(DOC / 'pipeline.json', dict(schema=1, expectedSubjects=len(base), preparedSubjects=len(selected),
               generator='builtin image_gen; one call per individual asset', logicalCanvas=[80,80], runtimeCanvas=[160,160],
               processing='Alpha bounding-box crop, nearest-neighbour fit within 72px, transparent 80px padding and exact 2x nearest enlargement only.',
               originalSpritesModified=False, styleReferenceRedistributed=False))
    print('Prepared', len(asset_ids or selected), 'rare sprite prompts; expected roster:', len(base))


def adopt(asset_id, source):
    selected, _ = entries()
    entry = selected[asset_id]
    prompt_file = DOC / 'prompts' / (asset_id + '.txt')
    prompt = prompt_file.read_text(encoding='utf8')
    source = Path(source).resolve()
    raw = DOC / 'generated' / (asset_id + '.png')
    if raw.exists() and sha(raw) != sha(source):
        previous_hash = sha(raw)[:12]
        archive = DOC / 'generated/versions' / (asset_id + '_' + previous_hash + '.png')
        archive.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(raw, archive)
        old_record = DOC / 'generation' / (asset_id + '.json')
        if old_record.exists():
            old = read(old_record)
            write_json(DOC/'generation/versions'/(asset_id+'_'+previous_hash+'.json'), old)
            old_prompt = DOC/'prompts/versions'/(asset_id+'_'+previous_hash+'.txt')
            old_prompt.parent.mkdir(parents=True, exist_ok=True)
            old_prompt.write_text(old['prompt'], encoding='utf8')
    raw.parent.mkdir(parents=True, exist_ok=True)
    if source != raw.resolve():
        shutil.copy2(source, raw)
    image = Image.open(raw).convert('RGBA')
    alpha = image.getchannel('A')
    if alpha.getextrema()[0] != 0 or alpha.getextrema()[1] != 255:
        raise ValueError('The generated asset needs real transparent background and opaque subject pixels: ' + asset_id)
    box = alpha.getbbox()
    if not box:
        raise ValueError('Empty image: ' + asset_id)
    crop = image.crop(box)
    scale = min(72 / crop.width, 72 / crop.height)
    size = max(1, round(crop.width * scale)), max(1, round(crop.height * scale))
    logical = Image.new('RGBA', (80,80))
    logical.alpha_composite(crop.resize(size, Image.Resampling.NEAREST), ((80-size[0])//2, 76-size[1]))
    runtime = logical.resize((160,160), Image.Resampling.NEAREST)
    target = OUT / (asset_id + '.png')
    save_png(logical, DOC / 'processed' / (asset_id + '_80.png'))
    save_png(runtime, DOC / 'processed' / (asset_id + '.png'))
    save_png(runtime, target)
    template = (ROOT / 'Assets/Resources/Lumia/PortraitsReference' / (asset_id + '.png.meta')).read_text(encoding='utf8')
    template = re.sub(r'^guid: .+$', 'guid: ' + uuid.uuid5(NAMESPACE, asset_id).hex, template, flags=re.M)
    template = re.sub(r'textureCompression: \d+', 'textureCompression: 0', template)
    target.with_suffix('.png.meta').write_text(template, encoding='utf8')
    folder_meta = OUT.with_suffix('.meta')
    if not folder_meta.exists():
        folder_meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid5(NAMESPACE, 'PortraitsRare').hex + '\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n', encoding='utf8')
    refs = reference_inputs(entry)
    record = dict(id=asset_id, name=entry['name'], skinNameKR=entry['skinNameKR'], skinCode=entry.get('skinCode', entry.get('skinId')),
                  customOutfit=entry['customOutfit'], sourceURL=entry['sourceURL'], releaseDate=entry.get('releaseDate'),
                  tool='builtin image_gen', toolOutputPath=str(source), transparentBackgroundRequested=True,
                  inputReferences=refs, styleReferenceCopied=False, prompt=prompt, promptSha256=sha(prompt_file),
                  generatedSource=raw.relative_to(ROOT).as_posix(), generatedSha256=sha(raw), generatedSize=list(image.size), alphaCropBox=list(box),
                  path=target.relative_to(ROOT).as_posix(), sha256=sha(target), processedPath=(DOC/'processed'/(asset_id+'.png')).relative_to(ROOT).as_posix(),
                  width=160, height=160, logicalCanvas=[80,80], resizedSize=list(size), integerPixelScale=2,
                  originalDefaultSprite='Assets/Resources/Lumia/PortraitsReference/'+asset_id+'.png',
                  originalDefaultSpriteSha256=sha(ROOT/'Assets/Resources/Lumia/PortraitsReference'/(asset_id+'.png')),
                  processing='Original generated alpha preserved; alpha crop, nearest resize to fit 72px, transparent 80px padding, exact 2x nearest enlargement only.')
    write_json(DOC / 'generation' / (asset_id + '.json'), record)
    print(json.dumps(dict(id=asset_id, path=record['path'], sha256=record['sha256']), ensure_ascii=False))


def rebuild():
    _, base = entries()
    records = [read(path) for path in sorted((DOC / 'generation').glob('*.json'))] if (DOC / 'generation').exists() else []
    errors = []
    for record in records:
        asset_id = record['id']
        for key, digest in [('path','sha256'), ('generatedSource','generatedSha256'), ('originalDefaultSprite','originalDefaultSpriteSha256')]:
            path = ROOT / record[key]
            if not path.exists() or sha(path) != record[digest]:
                errors.append(asset_id + ': ' + digest)
        for ref in record['inputReferences']:
            if ref['role'].startswith('style guidance'):
                continue
            if not (ROOT/ref['path']).exists() or sha(ROOT/ref['path']) != ref['sha256']:
                errors.append(asset_id + ': input reference hash ' + ref['path'])
        prompt_file = DOC / 'prompts' / (asset_id + '.txt')
        if prompt_file.read_text(encoding='utf8') != record['prompt'] or sha(prompt_file) != record['promptSha256']:
            errors.append(asset_id + ': actual prompt')
        image = Image.open(ROOT / record['path']).convert('RGBA')
        alpha_min, alpha_max = image.getchannel('A').getextrema()
        # Preserve generated alpha; nearest sampling may omit the few pixels at 255.
        if image.size != (160,160) or alpha_min != 0 or alpha_max < 250:
            errors.append(asset_id + ': size or alpha')
        if image.tobytes() != image.resize((80,80),Image.Resampling.NEAREST).resize((160,160),Image.Resampling.NEAREST).tobytes():
            errors.append(asset_id + ': broken 2x pixel grid')
        meta = (ROOT / record['path']).with_suffix('.png.meta')
        if not meta.exists() or ('guid: ' + uuid.uuid5(NAMESPACE,asset_id).hex) not in meta.read_text(encoding='utf8'):
            errors.append(asset_id + ': Unity metadata')
    for key in ('sha256', 'generatedSha256'):
        if len({record[key] for record in records}) != len(records):
            errors.append('Duplicate ' + key)
    expected = set(base); actual = {record['id'] for record in records}
    report = dict(expected=len(expected), actual=len(actual), missing=sorted(expected-actual), unexpected=sorted(actual-expected), errors=errors,
                  complete=expected==actual and not errors, logicalCanvas=[80,80], runtimeCanvas=[160,160], integerPixelScale=2,
                  customOutfits=sorted(record['id'] for record in records if record['customOutfit']), originalSpritesModified=False, styleReferenceRedistributed=False,
                  runtimeAlphaPolicy='Transparent background 0; maximum >=250. Generated alpha is preserved without repainting.')
    write_json(DOC/'asset-index.json', dict(schema=1, assets=records))
    write_json(DOC/'validation.json', report)
    print(json.dumps(report, ensure_ascii=False))
    if errors:
        raise SystemExit(1)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--prepare', action='store_true'); parser.add_argument('--adopt', action='store_true'); parser.add_argument('--rebuild', action='store_true')
    parser.add_argument('--id'); parser.add_argument('--source'); parser.add_argument('--force-prompt', action='store_true')
    args = parser.parse_args()
    if args.prepare: prepare({args.id} if args.id else None, args.force_prompt)
    if args.adopt: adopt(args.id, args.source)
    if args.rebuild: rebuild()
