"""Collect original game icon references and a reproducible ID manifest.

These downloads are source references for newly generated pixel art, never
copied into runtime assets. Art output is produced with built-in imagegen.
"""
import concurrent.futures
import hashlib
import json
import re
import urllib.request
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'docs/art-system'
REF = OUT / 'references'

def fetch(url, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists(): return True
    try:
        req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
        path.write_bytes(urllib.request.urlopen(req, timeout=35).read())
        return True
    except Exception as ex:
        print('MISSING', url, type(ex).__name__, flush=True)
        return False

def write(path, obj):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(obj, ensure_ascii=False, indent=2) + '\n', encoding='utf8')

def contact(entries, name):
    font = ImageFont.truetype(str(ROOT / 'Assets/Resources/Lumia/Galmuri11.ttf'), 18)
    cols = 6
    sheet = Image.new('RGB', (cols*200, ((len(entries)+cols-1)//cols)*180), '#17212f')
    draw = ImageDraw.Draw(sheet)
    for n, item in enumerate(entries):
        path = ROOT / item['referencePath']
        x, y = n % cols*200, n // cols*180
        if path.is_file():
            art = Image.open(path).convert('RGBA')
            art.thumbnail((128,128))
            sheet.paste(art, (x+(200-art.width)//2,y+6), art)
        draw.text((x+5,y+138), item['name'], fill='white', font=font)
        draw.text((x+5,y+158), item['id'], fill='#8acfd5', font=font)
    sheet.save(OUT / name)

def prepare_items():
    source='https://er.dakgg.io/api/v1/data/items?hl=ko'
    fetch(source, REF/'original-items.json')
    items=json.loads((REF/'original-items.json').read_text(encoding='utf8'))['items']
    text=(ROOT/'Assets/Scripts/Lumia/GameDatabase.cs').read_text(encoding='utf-8-sig')
    gear=re.findall(r'G\("([^"]+)","([^"]+)"', text)
    objects=re.findall(r'Objects.Add\(new ObjectDef \{ id="([^"]+)",name="([^"]+)"', text)
    aliases={'dainsleif':'다인슬라이프 - 진홍'}
    entries=[]
    for kind, defs in [('gear',gear),('object',objects)]:
        for asset_id, name in defs:
            matches=[x for x in items if x['name'].replace(' ','')==name.replace(' ','')]
            if not matches and asset_id=='dainsleif':
                matches=[x for x in items if '다인슬라이프' in x['name'] and '진홍' in x['name']]
            if not matches:
                print('UNMATCHED',asset_id,name,flush=True)
                continue
            item=matches[0]
            reference=f'docs/art-system/references/{asset_id}.png'
            entries.append(dict(id=asset_id,name=name,kind=kind,originalItemId=item['id'],
                                referenceUrl=item['imageUrl'],referencePath=reference,
                                dataSource=source,output=f'Assets/Resources/Lumia/SystemIcons/{asset_id}.png'))
    with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:
        list(pool.map(lambda e: fetch(e['referenceUrl'],ROOT/e['referencePath']),entries))
    write(OUT/'item-manifest.json',entries)
    contact(entries,'item-references.png')
    print('ITEM REFERENCES',len(entries),flush=True)

def prepare_traits():
    source='https://er.dakgg.io/api/v1/data/trait-skills?hl=ko'
    fetch(source, REF/'original-traits.json')
    traits=json.loads((REF/'original-traits.json').read_text(encoding='utf8'))['traitSkills']
    text=(ROOT/'Assets/Scripts/Lumia/GameDatabase.cs').read_text(encoding='utf-8-sig')
    defs=re.findall(r'R\("([^"]+)","([^"]+)"',text)
    entries=[]
    for asset_id,name in defs:
        matches=[x for x in traits if x['name'].replace(' ','')==name.replace(' ','')]
        if not matches:
            print('UNMATCHED TRAIT',asset_id,name,flush=True)
            continue
        item=matches[0]
        reference=f'docs/art-system/references/{asset_id}.png'
        entries.append(dict(id=asset_id,name=name,kind='rune',originalTraitId=item['id'],
                            referenceUrl=item['imageUrl'],referencePath=reference,
                            dataSource=source,output=f'Assets/Resources/Lumia/SystemIcons/{asset_id}.png'))
    with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:
        list(pool.map(lambda e:fetch(e['referenceUrl'],ROOT/e['referencePath']),entries))
    write(OUT/'rune-manifest.json',entries)
    contact(entries,'rune-references.png')
    print('RUNE REFERENCES',len(entries),flush=True)

if __name__=='__main__':
    prepare_items()
    prepare_traits()
