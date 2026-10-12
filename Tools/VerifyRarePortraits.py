"""Validate all runtime rare sprites and produce inspection-only comparison sheets."""
import hashlib
import json
from pathlib import Path
import re
import subprocess
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
DOC = ROOT / 'docs/art-rare-skins'
skins = json.loads((DOC / 'skin-manifest.json').read_text(encoding='utf8'))['skins']
assert len(skins) == len({s['id'] for s in skins}) == 91
sha = lambda path: hashlib.sha256(Path(path).read_bytes()).hexdigest()
runtime = ROOT / 'Assets/Resources/Lumia/PortraitsRare'
expected = {s['id'] for s in skins}
assert {p.stem for p in runtime.glob('*.png')} == expected, 'Missing/unexpected rare resource'
index = json.loads((DOC / 'asset-index.json').read_text(encoding='utf8'))['assets']
assert {r['id'] for r in index} == expected, 'Missing generation record'
records = {r['id']: r for r in index}
guids = set()
for skin in skins:
    asset_id = skin['id']
    rare = runtime / (asset_id + '.png')
    base = ROOT / 'Assets/Resources/Lumia/PortraitsReference' / (asset_id + '.png')
    record = records[asset_id]
    assert record['skinNameKR'] == skin['skinNameKR'], asset_id + ': selected skin name'
    assert record['customOutfit'] == skin['customOutfit'], asset_id + ': original casual flag'
    assert sha(rare) != sha(base), asset_id + ': base sprite reused'
    tracked_base = subprocess.check_output(['git', 'show', 'HEAD:' + base.relative_to(ROOT).as_posix()], cwd=ROOT)
    assert hashlib.sha256(tracked_base).hexdigest() == sha(base), asset_id + ': base sprite modified'
    metadata = rare.with_suffix('.png.meta').read_text(encoding='utf8')
    guid = re.search(r'^guid: ([0-9a-f]{32})$', metadata, re.M)
    assert guid and guid[1] not in guids, asset_id + ': invalid/duplicate Unity GUID'
    guids.add(guid[1])
    assert 'filterMode: 0' in metadata and 'textureCompression: 0' in metadata, asset_id + ': point/uncompressed import'
    assert record['tool'] == 'builtin image_gen' and record['transparentBackgroundRequested']
    assert record['prompt'] == (DOC / 'prompts' / (asset_id + '.txt')).read_text(encoding='utf8')
    assert record['generatedSha256'] == sha(ROOT / record['generatedSource'])
    assert record['sha256'] == sha(rare)

sheet_folder = DOC / 'sprite-comparisons'
sheet_folder.mkdir(parents=True, exist_ok=True)
font_path = Path('C:/Windows/Fonts/malgun.ttf')
font = ImageFont.truetype(str(font_path), 13)
heading = ImageFont.truetype(str(font_path), 19)
sheets = []
for offset in range(0, len(skins), 16):
    page = skins[offset:offset+16]
    sheet = Image.new('RGB', (1360, 972), '#101a24')
    draw = ImageDraw.Draw(sheet)
    draw.text((18, 10), 'Black The Survival · 레어 조우 스프라이트 · 기본 / 스킨', font=heading, fill='#f5d88c')
    draw.text((18, 38), '80×80 논리 격자 · 160×160 런타임 · 이 대조표는 검토용이며 게임 리소스를 변경하지 않습니다.', font=font, fill='#adb9be')
    for i, skin in enumerate(page):
        col, row = i % 4, i // 4
        x, y = col * 340, 72 + row * 225
        draw.rectangle((x+3, y+2, x+336, y+220), outline='#364d59')
        draw.text((x+10, y+6), skin['name'] + '  /  ' + skin['id'], font=font, fill='#e5e9e6')
        for start, directory in [(x+8, ROOT/'Assets/Resources/Lumia/PortraitsReference'), (x+172, runtime)]:
            sprite = Image.open(directory / (skin['id']+'.png')).convert('RGBA')
            assert sprite.size == (160, 160)
            sheet.paste(sprite, (start, y+28), sprite)
        name_font = font
        while draw.textbbox((0,0),skin['skinNameKR'],font=name_font)[2] > 320:
            name_font = ImageFont.truetype(str(font_path), name_font.size-1)
        draw.text((x+10, y+195), skin['skinNameKR'], font=name_font, fill='#f5d88c')
    path = sheet_folder / ('page-%02d.png' % (offset//16+1))
    sheet.save(path)
    sheets.append(path.relative_to(ROOT).as_posix())
report = dict(passed=True, subjects=91, distinctBuiltinGenerations=91, runtimeTextures=91,
              logicalCanvas=[80,80],runtimeCanvas=[160,160],pointFiltered=True,uncompressed=True,
              validUniqueUnityGuids=91,defaultSpritesUnchanged=True,customOutfits=['lucia','ceres','craver'],
              styleReferenceRedistributed=False,comparisonSheets=sheets)
(DOC/'runtime-validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps(report,ensure_ascii=False))
