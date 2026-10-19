"""Reproduce reviewed face-pixel corrections without resampling a game sprite.

Coordinates address the existing 80x80 logical pixels. Every edit becomes one
exact 2x2 block in the original 160x160 canvas; all other RGBA bytes are retained.
The immutable Git baseline makes repeated runs deterministic rather than
applying contrast changes cumulatively.
"""
import hashlib
import io
import json
import subprocess
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont
from VerifyFaceRefinements import git_objects

ROOT = Path(__file__).resolve().parents[1]
DOC = ROOT / 'docs/art-face-refinement'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf8')


def digest(data):
    return hashlib.sha256(data).hexdigest()


def apply():
    manifest = read(DOC / 'manifest.json')
    assets = {(a['id'], a['variant']): a for a in manifest['assets']}
    baseline = git_objects(ROOT, manifest['baselineCommit'], [a['path'] for a in assets.values()])
    patches = []
    for path in sorted(DOC.glob('*-patches.json')):
        patches += read(path)
    assert len(patches) == len({(p['id'], p['variant']) for p in patches}), 'duplicate asset patch'
    patched = {(p['id'], p['variant']): p for p in patches}
    for key, entry in assets.items():
        before = baseline[entry['path']]
        assert digest(before) == entry['beforeSha256']
        image = Image.open(io.BytesIO(before)).convert('RGBA')
        assert image.size == (160, 160)
        entry['editMaskLogicalRects'] = []
        entry['changeReason'] = ''
        entry['referencePaths'] = []
        patch = patched.get(key)
        if patch:
            entry['changeReason'] = patch['reason']
            entry['referencePaths'] = patch['referencePaths']
            for path in patch['referencePaths']:
                assert (ROOT / path).is_file(), path
            positions = set()
            for x, y, color in patch['pixels']:
                assert (x, y) not in positions, (key, x, y)
                positions.add((x, y))
                assert 0 <= x < 80 and 0 <= y < 80
                rgb = tuple(bytes.fromhex(color))
                assert len(rgb) == 3
                # Changing alpha would alter the silhouette; only opaque facial
                # pixels are eligible, including an existing eye or mouth.
                assert image.getpixel((x*2, y*2))[3] >= 128, (key, x, y, 'transparent pixel')
                for yy in (y*2, y*2+1):
                    for xx in (x*2, x*2+1):
                        alpha = image.getpixel((xx, yy))[3]
                        image.putpixel((xx, yy), (*rgb, alpha))
                entry['editMaskLogicalRects'].append([x, y, x+1, y+1])
            image.save(ROOT / entry['path'])
        else:
            assert (ROOT / entry['path']).read_bytes() == before, 'unreviewed asset changed: ' + str(key)
        if key[0] == 'nia':
            # The original face/mouth refinement remains deterministic; the
            # user's later black-iris patch is applied by Node, not Pillow.
            subprocess.run(['node', str(ROOT / 'Tools/ApplyNiaEyeOverrides.mjs'),
                            '--target', entry['path']], cwd=ROOT, check=True)
            eye_manifest = read(ROOT / 'docs/art-nia-black-eyes/manifest.json')
            eye_patch = next(item for item in eye_manifest['runtime'] if item['path'] == entry['path'])
            rects = {tuple(rect) for rect in entry['editMaskLogicalRects']}
            for x, y, *_ in eye_patch['pixels']:
                xx, yy = x // 2, y // 2
                rects.add((xx, yy, xx + 1, yy + 1))
            entry['editMaskLogicalRects'] = [list(rect) for rect in sorted(rects)]
        entry['afterSha256'] = digest((ROOT / entry['path']).read_bytes())
        entry['auditPath'] = 'docs/art-face-refinement/' + key[1] + '-audit.json'
    write(DOC / 'manifest.json', manifest)

    for variant, directory in [('default', 'art-reference-chibi'), ('rare', 'art-rare-skins')]:
        folder = ROOT / 'docs' / directory
        index = read(folder / 'asset-index.json')
        for record in index['assets']:
            entry = assets[(record['id'], variant)]
            record['sha256'] = entry['afterSha256']
            if (record['id'], variant) in patched:
                record['faceRefinement'] = dict(
                    manifest='docs/art-face-refinement/manifest.json',
                    baselineCommit=manifest['baselineCommit'], beforeSha256=entry['beforeSha256'],
                    method='Reviewed individual eye/mouth pixel corrections on the unchanged logical grid.',
                    preservesCanvas=True, preservesAlpha=True, preservesOutsideMask=True)
                # Rare sprites keep a processed mirror for future inspections.
                if record.get('processedPath'):
                    (ROOT / record['processedPath']).write_bytes((ROOT / record['path']).read_bytes())
            write(folder / 'generation' / (record['id'] + '.json'), record)
        write(folder / 'asset-index.json', index)
    comparisons(manifest, baseline, patched)
    print(json.dumps(dict(reviewed=len(assets), patched=len(patches),
                          logicalPixels=sum(len(p['pixels']) for p in patches))))


def comparisons(manifest, baseline, patched):
    folder = DOC / 'comparisons'
    folder.mkdir(parents=True, exist_ok=True)
    font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 14)
    names = {r['id']: r['name'] for r in read(ROOT / 'docs/art-chibi/reference-characters.json')}
    entries = [a for a in manifest['assets'] if (a['id'], a['variant']) in patched]
    for page in range((len(entries)+7)//8):
        sheet = Image.new('RGB', (1000, 1040), '#142231')
        draw = ImageDraw.Draw(sheet)
        draw.text((18, 10), '얼굴 픽셀 보정 · 이전 / 수정 · 동일한 160×160 크기', font=font, fill='#efc979')
        for i, entry in enumerate(entries[page*8:page*8+8]):
            x, y = (i%2)*500, 50+(i//2)*244
            draw.text((x+14, y), names[entry['id']]+' · '+entry['variant'], font=font, fill='#dce8e1')
            for j, im in enumerate([Image.open(io.BytesIO(baseline[entry['path']])).convert('RGBA'),
                                    Image.open(ROOT / entry['path']).convert('RGBA')]):
                sheet.paste(im, (x+20+j*240, y+22), im)
                # Inspection zoom only; the game texture and render size stay unchanged.
                rects = entry['editMaskLogicalRects']
                if rects:
                    cx = (min(r[0] for r in rects)+max(r[2] for r in rects))//2
                    cy = (min(r[1] for r in rects)+max(r[3] for r in rects))//2
                    box = (cx*2-20,cy*2-14,cx*2+20,cy*2+14)
                    crop = im.crop(box).resize((120,84),Image.Resampling.NEAREST)
                    sheet.paste(crop, (x+148+j*240, y+120), crop)
        sheet.save(folder / ('page-%02d.png' % (page+1)))


if __name__ == '__main__':
    apply()
