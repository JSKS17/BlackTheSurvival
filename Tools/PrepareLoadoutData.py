"""Export explicit, reviewed source mappings without modifying raster artwork."""
import json
from pathlib import Path
from urllib.request import urlretrieve

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'docs/art-system'
items = json.loads((ART / 'items-snapshot.json').read_text(encoding='utf-8-sig'))['items']
skills = json.loads((ART / 'skills-snapshot-ko.json').read_text(encoding='utf-8-sig'))['skills']
props = json.loads((ROOT / 'docs/art-chibi/character-props.json').read_text(encoding='utf-8-sig'))['characters']
classes = {
    'Glove': ('글러브','glove',1), 'Tonfa': ('톤파','tonfa',2), 'Bat': ('방망이','bat',3),
    'Whip': ('채찍','whip',4), 'HighAngleFire': ('투척','throw',5), 'DirectFire': ('암기','shuriken',6),
    'Bow': ('활','bow',7), 'CrossBow': ('석궁','crossbow',8), 'Pistol': ('권총','pistol',9),
    'AssaultRifle': ('돌격 소총','rifle',10), 'SniperRifle': ('저격총','sniper',11),
    'Hammer': ('망치','hammer',13), 'Axe': ('도끼','axe',14), 'OneHandSword': ('단검','dagger',15),
    'TwoHandSword': ('양손검','sword',16), 'DualSword': ('쌍검','dual',18), 'Spear': ('창','spear',19),
    'Nunchaku': ('쌍절곤','nunchaku',20), 'Rapier': ('레이피어','rapier',21), 'Guitar': ('기타','guitar',22),
    'Camera': ('카메라','camera',23), 'Arcana': ('아르카나','arcana',24), 'VFArm': ('VF의수','vf',25),
}
added = {
    'fragarach': 101405, 'doomsday': 104503, 'ruyi': 108502, 'uranus': 109406,
    'eternal_frost': 111502, 'loigor': 103502, 'judgement': 117406, 'sharanga': 115501,
    'death_book': 113412, 'fireball': 112405, 'empress': 130501, 'ultravision': 122501,
    'cosmic_bident': 107404, 'cerberus': 119403, 'wonderful_tonight': 121405, 'deathadder_mt': 131501,
    'angel_halo': 201501, 'radar': 203502,
}
existing = {'glove','pistol','dagger','axe','sword','bow','hammer','rapier','sniper'}
entries = []
refs = ART / 'references/loadout'
refs.mkdir(parents=True, exist_ok=True)
for asset_id, original in added.items():
    item = next(x for x in items if x['id'] == original)
    assert item['grade'] in ('Legend','Mythic')
    filename = refs / (asset_id + '.png')
    urlretrieve(item['imageUrl'], filename)
    entries.append(dict(id=asset_id,name=item['name'],kind='gear',originalId=original,
                        weaponClass=classes.get(item.get('weaponType'), ('','',''))[0],
                        referenceUrl=item['imageUrl'],referencePath=str(filename.relative_to(ROOT)).replace('\\','/'),
                        originalTooltip=item['tooltip'],output='Assets/Resources/Lumia/SystemIcons/'+asset_id+'.png'))
for enum, (label, key, numeric) in classes.items():
    if key in existing:
        continue
    skill = next(x for x in skills if x.get('weaponTypeId') == numeric and x['slot']=='D')
    asset_id = 'weapon_' + key
    filename = refs / (asset_id + '.png')
    urlretrieve(skill['imageUrl'], filename)
    entries.append(dict(id=asset_id,name=skill['name'],kind='weapon',originalId=skill['id'],weaponClass=label,
                        referenceUrl=skill['imageUrl'],referencePath=str(filename.relative_to(ROOT)).replace('\\','/'),
                        originalTooltip=skill['tooltip'],output='Assets/Resources/Lumia/SkillIcons/'+asset_id+'.png'))
(ART / 'loadout-art-manifest.json').write_text(json.dumps(entries,ensure_ascii=False,indent=2),encoding='utf-8')
mapping = ['            { "'+key+'", new[]{'+','.join('"'+classes[c][0]+'"' for c in item['weapon_types'])+'} },' for key,item in props.items() if key!='hana']
(ART / 'character-weapon-mapping.txt').write_text('\n'.join(mapping),encoding='utf-8')
print('Prepared original reference assets:',len(entries),'and mappings:',len(mapping))
