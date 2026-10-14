"""Record original game item identities and fetch icon references for v1.0.1.

The game-item snapshot is a source reference, not the fan game's balance.
Pixel artwork is authored with built-in imagegen in separate calls.
"""
import json
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'docs/art-system'
ITEMS = json.loads((ART / 'items-snapshot.json').read_text(encoding='utf-8-sig'))['items']
SELECTION = {
    'intervention': 118404,
    'joyeuse': 120405,
    'quad_eye': 201518,
    'mithril_armband': 203514,
    'racing_boots': 204509,
    'dice_of_destiny': 205501,
}
OFFICIAL = {
    'intervention': 'https://playeternalreturn.com/posts/news/2014?hl=ko-KR',
    'joyeuse': 'https://playeternalreturn.com/posts/news/3182?hl=ko-KR',
    'quad_eye': 'https://playeternalreturn.com/posts/news/2765?hl=ko-KR',
    'mithril_armband': 'https://playeternalreturn.com/posts/news/3163?hl=ko-KR',
    'racing_boots': 'https://playeternalreturn.com/posts/news/1868?hl=ko-KR',
    'dice_of_destiny': 'https://playeternalreturn.com/posts/news/3163?hl=ko-KR',
}
rows = []
for key, original_id in SELECTION.items():
    item = next(i for i in ITEMS if i['id'] == original_id)
    reference = Path('docs/art-system/references/expansion-v101') / (key + '.png')
    path = ROOT / reference
    path.parent.mkdir(parents=True, exist_ok=True)
    if not path.exists():
        request = urllib.request.Request(item['imageUrl'], headers={'User-Agent': 'Mozilla/5.0'})
        path.write_bytes(urllib.request.urlopen(request, timeout=40).read())
    rows.append(dict(id=key, name=item['name'], kind='gear', originalId=original_id,
        referenceUrl=item['imageUrl'], referencePath=reference.as_posix(),
        originalTooltip=item['tooltip'], sourceSnapshot='docs/art-system/items-snapshot.json',
        officialReference=OFFICIAL[key],
        output=f'Assets/Resources/Lumia/SystemIcons/{key}.png'))
(ART / 'equipment-expansion-manifest.json').write_text(
    json.dumps(rows, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('Prepared equipment source references:', len(rows))
