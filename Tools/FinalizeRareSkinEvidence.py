"""Keep manually checked official release chronology separate from art metadata."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOC = ROOT / 'docs/art-rare-skins'

# Dates are in KST. Reward skins use the first day their pass/event was usable,
# including immediate season-pack access rather than the later standalone sale.
FACTS = '''
nia 2026-08-06 3742
jackie 2024-11-07 2304
aya 2025-12-23 3242
hyunwoo 2024-08-01 2072
yuki 2025-10-23 3111
hyejin 2026-04-30 3530
sua 2024-07-18 2038
isol 2024-02-22 1749
nadine 2026-01-29 3327
emma 2024-10-01 2201
charlotte 2024-12-05 2363
nathapon 2026-03-10 3406
nicky 2026-09-03 3813
daniel 2024-03-05 1765
tia 2026-04-30 3530
laura 2026-04-02 3480
lenox 2024-12-12 2381
leon 2023-07-20 1286
rozzi 2026-07-28 3713
luke 2025-04-17 2645
dailin 2025-12-23 3242
rio 2026-07-23 3713
martina 2025-04-03 2623
mai 2026-01-29 3327
markus 2025-07-17 2852
magnus 2025-09-18 3030
vanya 2026-04-30 3530
barbara 2025-06-03 2736
bernice 2023-11-09 1545
bianca 2026-07-23 3713
celine 2026-06-02 3606
sho 2025-11-27 3182
shoichi 2026-08-25 3783
sissela 2026-05-07 3555
silvia 2026-04-02 3480
adela 2024-04-25 1868
adriana 2025-12-11 3209
adina 2025-07-17 2852
isaac 2026-03-24 3444
alex 2026-02-19 3374
jan 2025-11-18 3163
estelle 2025-03-27 2598
aiden 2025-12-23 3242
echion 2024-04-30 1868
elena 2026-03-05 3406
johann unknown 1593
william 2025-04-30 2673
irem 2026-05-28 3606
eva 2025-08-07 2916
ian 2025-10-23 3111
eleven 2026-08-13 3769
zahir 2024-04-01 1827
jenny 2026-05-14 3572
camilo 2026-03-19 3444
karla 2023-09-14 1424
cathy 2025-01-07 2431
chloe 2025-07-17 2852
chiara 2026-01-29 3327
tazia 2026-08-06 3742
theodore 2025-03-27 2598
felix 2025-10-23 3111
priya 2026-10-01 3867
fiora 2025-04-22 2654
piolo 2023-11-09 1545
hart 2026-07-23 3713
haze 2026-01-22 3305
debi_marlene 2025-05-29 2736
arda 2026-05-19 3572
abigail 2025-08-07 2916
alonso 2024-12-12 2381
leni 2026-04-30 3530
tsubame 2025-07-10 2827
kenneth 2025-10-23 3111
katja 2025-10-30 3127
darko 2025-08-12 2916
lenore 2026-02-05 3340
garnet 2026-08-20 3783
yumin 2026-08-06 3742
hisui 2025-11-13 3163
justyna 2026-01-29 3327
istvan 2025-08-21 2952
xuelin 2026-08-06 3742
henry 2026-01-08 3275
blair 2026-02-20 3389
mirka 2026-04-16 3504
fenrir 2026-06-11 3629
coraline 2026-06-25 season11
bihyung 2026-09-17 3838
'''


def main():
    manifest = json.loads((DOC / 'skin-manifest.json').read_text(encoding='utf8'))
    rows = {s['id']: s for s in manifest['skins']}
    facts = {}
    for line in FACTS.splitlines():
        if not line.strip():
            continue
        sid, date, post = line.split()
        source = ('https://event.playeternalreturn.com/S11/PetitMignon/Teasing?hl=ko-KR'
                  if post == 'season11' else f'https://playeternalreturn.com/posts/news/{post}?hl=ko-KR')
        note = 'Official release notice or reward-pass availability date, checked against the current 12.5.0 character skin registry on 2026-10-12.'
        if sid in ('nia', 'tazia', 'yumin', 'xuelin'):
            note = 'Season 12 pack included immediate ER Pass access on August 6. The separately sold pass opened August 13; the earlier available date is used.'
        elif sid in ('hyejin', 'tia', 'vanya', 'leni'):
            note = 'Season 11 pack and its included ER Pass opened April 30; these reward skins were obtainable from that pass/pack.'
        elif sid in ('rio', 'hart'):
            note = 'The anniversary base skins opened July 9, but the Dolce/Vivace quest-book color variants opened July 23 and are the latest distinct registered skins.'
        elif sid == 'eva':
            note = 'The base anniversary skin preceded the Blossom quest-book color variant; the August 7 patch explicitly releases the Blossom variant.'
        elif sid == 'blair':
            note = 'The skin was delayed after the February 19 patch. The official follow-up confirms actual availability on February 20 at 14:00 KST; the delay overrides the patch date.'
        elif sid == 'coraline':
            note = 'The official Season 11 skin schedule lists Coraline for June 25. Her displayed client skin name uses 설렘 가득; announcement searches also vary spelling/spacing.'
        elif sid == 'johann':
            note = 'The latest skin in the current character registry is a previously released legacy skin. The official November 27, 2023 refund notice proves it had been owned in-game. Its initial release day was not recovered, so releaseDate remains null. The notice records its discontinued sale; ownership and existing in-game use are not a claim of present store availability.'
        facts[sid] = dict(releaseDate=None if date == 'unknown' else date,
                          sourceURL=source,
                          latestVerification=note,
                          releaseEvidence=[dict(url=source, date=None if date == 'unknown' else date, note=note)])
        if sid == 'johann':
            facts[sid]['releasedByDate'] = '2023-11-27'
    for sid in ('lucia', 'ceres', 'craver'):
        row = rows[sid]
        facts[sid] = dict(releaseDate=None, sourceURL=row['sourceURL'],
            latestVerification='The live 12.5.0 registry has only the base appearance for this subject. The user requested a fan-original casual outfit. Announced future skins are excluded.',
            releaseEvidence=[dict(url='https://er.dakgg.io/api/v1/data/characters?hl=ko', date='2026-10-12', note='Only the default skin is present in current live character data; original casual design requested by the user.')])
    assert len(facts) == 91 and set(facts) == set(rows)
    (DOC / 'release-evidence.json').write_text(json.dumps(facts, ensure_ascii=False, indent=2) + '\n', encoding='utf8')
    print('Wrote 91 release records; 87 dated official skins, 1 previously released legacy skin, 3 original casual outfits.')


if __name__ == '__main__':
    main()
