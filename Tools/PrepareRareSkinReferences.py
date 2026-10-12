"""Snapshot public live client skin records and official art for rare encounters.

Downloaded images are documentation/design references only, never runtime sprites.
Release facts and visual notes are kept separately in release-evidence.json.
"""
import concurrent.futures
import hashlib
import json
import urllib.request
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
DOC = ROOT / 'docs/art-rare-skins'
SOURCE = 'https://er.dakgg.io/api/v1/data/characters?hl=ko'
CHECKED = '2026-10-12'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf8')


def fetch(entry):
    target = ROOT / entry['referencePath']
    if not target.exists():
        request = urllib.request.Request(entry['imageURL'], headers={'User-Agent': 'Mozilla/5.0'})
        with urllib.request.urlopen(request, timeout=30) as response:
            target.write_bytes(response.read())
    with Image.open(target) as image:
        entry['referenceSize'] = list(image.size)
    entry['referenceSha256'] = hashlib.sha256(target.read_bytes()).hexdigest()
    return entry


def main():
    DOC.mkdir(parents=True, exist_ok=True)
    (DOC / 'references').mkdir(exist_ok=True)
    facts_path = DOC / 'release-evidence.json'
    facts = read(facts_path) if facts_path.exists() else {}
    visuals = read(DOC / 'visual-notes.json') if (DOC / 'visual-notes.json').exists() else {}
    outfits = read(DOC / 'custom-outfits.json') if (DOC / 'custom-outfits.json').exists() else {}
    roster = [r for r in read(ROOT / 'docs/art-chibi/reference-characters.json') if r['id'] != 'hana']
    characters = read(DOC / 'characters-ko.json')['characters']
    by_key = {c['key'].lower(): c for c in characters}
    entries = []
    for subject in roster:
        character = by_key[subject['cdn_key'].lower()]
        skin = max(character['skins'], key=lambda x: x['id'])
        sid = subject['id']
        custom = sid in ('lucia', 'ceres', 'craver')
        fact = facts.get(sid, {})
        skin_name = fact.get('skinNameKR', skin['name'])
        image_name = fact.get('imageName', skin['imageName'])
        image_url = f'https://cdn.dak.gg/assets/er/game-assets/12.5.0/ui/characterhalfsize/CharFull_{image_name}.png'
        reference = f'docs/art-rare-skins/references/{sid}.png'
        name = subject['name']
        skin_name = {'lucia':'별빛 산책 루치아', 'ceres':'느긋한 외출 세레스', 'craver':'골목의 휴일 크레이버'}.get(sid, skin_name)
        visual = visuals.get(sid, fact.get('visualDescription', 'Use the supplied official skin illustration to preserve this skin\'s actual hair, distinctive face, costume palette, garment silhouette and signature prop; do not inherit the character\'s base outfit or another subject\'s accessories.'))
        if custom:
            visual = {
                'lucia': 'Original casual outfit: pale gray twin buns and cyan eyes retained; lavender cardigan over a white high-neck blouse, navy pleated skirt, white tights and brown ankle boots; small star-shaped violet crossbody bag and closed silver umbrella. Calm dignified expression. No maid headband or weapon-sized bag.',
                'ceres': 'Original casual outfit: silver bob and focused gentle expression retained; ivory hooded jacket with charcoal lining, plum turtleneck, charcoal tapered trousers, white trainers, sturdy dark crossbody satchel and compact folded picnic blanket; no maid headband and no armor. Protective reliable body language.',
                'craver': 'Original casual outfit: vivid red tousled hair, original red round spectacles and mischievous confident grin retained; open charcoal bomber jacket with burgundy lining over ivory graphic-free T-shirt, rolled dark jeans, red high-top trainers, dark fingerless gloves and pocket coin. No long combat coat; no lettering.'
            }[sid]
            # The art pipeline's accepted original outfit is authoritative once
            # present; keep reference notes and the adopted sprite in agreement.
            visual = outfits.get(sid, {}).get('visualDescription', visual)
        entry = dict(subjectId=sid, id=sid, name=name, skinNameKR=skin_name, skinName=skin_name,
            skinCode=fact.get('skinCode', skin['id']) if not custom else None,
            skinId=fact.get('skinCode', skin['id']) if not custom else None,
            imageName=image_name, characterKey=character['key'], characterCode=character['id'],
            releaseDate=fact.get('releaseDate'), releasedByDate=fact.get('releasedByDate'),
            sourceURL=fact.get('sourceURL', f'https://dak.gg/er/characters/{character["key"]}/introduction?hl=ko'),
            sourceUrl=fact.get('sourceURL', f'https://dak.gg/er/characters/{character["key"]}/introduction?hl=ko'),
            imageURL=image_url, localReferencePath=reference, referencePath=reference,
            visualDescription=visual, status='casual_original' if custom else 'verified', customOutfit=custom,
            latestVerification=fact.get('latestVerification', 'Current public 12.5.0 skin registry candidate; release chronology is being cross-checked against official notices.'),
            releaseEvidence=fact.get('releaseEvidence', []), checkedAt=CHECKED,
            referenceRole='Official base character half-portrait identity reference only; costume is original casual design.' if custom else 'Official skin half-portrait identity and costume reference only; redraw as a new full-body chibi sprite with conservative continuation of cropped clothing.')
        entries.append(entry)
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        entries = list(pool.map(fetch, entries))
    manifest = dict(schema=1, checkedAt=CHECKED, gameDataVersion='12.5.0', sourceURL=SOURCE,
        selectionMethod='Latest released distinct skin appearance among current live 12.5.0 character records, checked against official release, reward-pass and delay notices as of 2026-10-12. Later quest-book color variants count as distinct skins. Future announced releases are excluded. Previously released legacy skins remain eligible for existing owners even when no longer sold. Three user-requested originals use official base identity references.',
        referencesShippedInGame=False, skins=entries)
    write(DOC / 'skin-manifest.json', manifest)
    font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 18)
    (DOC/'contact-sheets').mkdir(exist_ok=True)
    for start in range(0,len(entries),12):
        sheet = Image.new('RGB',(1280,1200),'#273342')
        draw = ImageDraw.Draw(sheet)
        for i, entry in enumerate(entries[start:start+12]):
            x,y=(i%4)*320,(i//4)*400
            image=Image.open(ROOT/entry['referencePath']).convert('RGBA')
            box=image.getchannel('A').getbbox()
            if box: image=image.crop(box)
            image.thumbnail((300,345),Image.Resampling.LANCZOS)
            sheet.paste(image,(x+(320-image.width)//2,y+4+(345-image.height)//2),image)
            draw.text((x+8,y+350),entry['id']+' · '+entry['name'],font=font,fill='white')
            draw.text((x+8,y+375),entry['skinNameKR'],font=font,fill='#cbd4e3')
        sheet.save(DOC/'contact-sheets'/f'references_{start//12+1:02}.png')
    print(json.dumps(dict(subjects=len(entries),officialSkins=sum(not e['customOutfit'] for e in entries),casualOriginals=sum(e['customOutfit'] for e in entries),downloadedBytes=sum((ROOT/e['referencePath']).stat().st_size for e in entries)),ensure_ascii=False))


if __name__ == '__main__': main()
