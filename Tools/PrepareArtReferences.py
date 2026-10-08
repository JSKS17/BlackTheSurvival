"""Prepare reproducible original-game visual references, never runtime assets."""
import argparse
import concurrent.futures
import html
import json
import re
import subprocess
import sys
import urllib.request
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'docs' / 'art-chibi'
TEMP = ROOT / 'Temp'
PYTHON = Path(sys.executable)

KEY_OVERRIDES = {'nia':'NiaH', 'dailin':'LiDailin', 'sho':'Xiukai', 'ian':'LyAnh',
                 'ceres':'Seres', 'debi_marlene':'DebiMarlene', 'yumin':'YuMin'}
VERSION = '12.5.0'
CDN = f'https://cdn.dak.gg/assets/er/game-assets/{VERSION}/'

# Visual notes checked against every contact-sheet cell, not invented outfits.
# Lower-body designs may need conservative continuation of the official bust art.
APPEARANCE = {
    'hana':'Warm brown bob, amber eyes, yellow zigzag hair clip, green sweater, white laboratory coat, brown plaid skirt, books.',
    'nia':'Dusty lavender bob with curled side bunches, pink headphones, lilac-pink hooded vest, magenta plaid sleeves, controller.',
    'jackie':'Spiky white hair, red eyes, red-and-black sleeveless outfit, black gloves, oversized red axe or chainsaw.',
    'aya':'Brown braids, round glasses, olive police jacket, navy skirt, dark firearm.',
    'hyunwoo':'Red-orange short hair, open brown school jacket, white shirt, clenched fists.',
    'yuki':'Blue-black short hair, black Japanese school uniform with gold trim, katana.',
    'hyejin':'Long black braids, white-and-black sailor uniform, purple talismans.',
    'sua':'Long brown hair, pale floral green dress, book and storybook hammer.',
    'isol':'Light brown hair, red headband and checked scarf, white sleeveless shirt, olive tactical harness, assault rifle.',
    'nadine':'Dark high ponytail, orange cap and hooded mantle, fitted dark hunting gear, bow or crossbow.',
    'emma':'Turquoise twin braids and bunny-shaped bow, white-and-cyan magician outfit, playing cards.',
    'charlotte':'Long pink hair, lace maid headband, white and black gothic dress, floating pale crystal blades.',
    'nathapon':'Brown side-swept hair, backward white cap, light-blue denim vest, white shirt, big camera.',
    'nicky':'Blond bob, crossed green/orange hair clips, bright orange cropped sports jacket, boxing gloves.',
    'daniel':'Long dark-purple hair, pale face, high-collar black strapped jacket, tailor scissors.',
    'tia':'Honey-blond long hair with rose-shaped side knot, yellow ribbon, brown artist outfit, broad paintbrush.',
    'laura':'Long silvery lilac hair, glossy black high-collar suit, dark blue mantle, hydrangea and whip.',
    'lenox':'Short green hair, red-brown blazer worn on shoulders, black crop top, sleeve tattoo, fishing whip.',
    'leon':'Brown hair, white headphones, blue-white open sports hoodie, orange shoulder strap, swimmer theme.',
    'rozzi':'Black wavy bob partly covering one eye, sleeveless black and gold tactical dress, dual pistols.',
    'luke':'Spiky light brown hair, yellow goggles around neck, white-red flame shirt, dark work overalls, broom.',
    'dailin':'Black twin hair buns with long orange ribbons, yellow-black cropped jacket, dragon embroidery, bottle.',
    'rio':'Long straight white hair and blunt bangs, yellow eyes, gray-black sailor uniform, yellow neckerchief, bow.',
    'martina':'Gray side-swept hair, black eyepatch, mustard trench coat, large shoulder video camera.',
    'mai':'Auburn bob, white sleeveless fashion blouse, black-and-white striped neck scarf, teal-gold draped sleeves.',
    'markus':'Short black hair, trimmed beard, dark gray tank top, huge silver shoulder guard, battle axe.',
    'magnus':'Bright blond-orange mohawk, black studded biker vest, broad build, bat and motorcycle theme.',
    'vanya':'Short pale blue hair, cyan butterfly hair ornaments, white-and-cyan ribbon dress, blue butterfly wings.',
    'barbara':'Brown ponytail, glasses, chunky white-gray visor helmet, cobalt blue mechanic suit, orange harness, robot tools.',
    'bernice':'Shaggy blond hair, fur-trimmed yellow field coat, green camouflage layer, hunting shotgun.',
    'bianca':'Dark teal hair, red eyes, maid lace headband, black gothic dress, dark parasol and blood bag.',
    'celine':'Straight icy blue bob, yellow eyes, orange utility jacket, black crop top, demolition gear.',
    'sho':'Round smiling chef, dark topknot, bamboo-rimmed straw hat, white chef coat, red scarf, bamboo cooking tools.',
    'shoichi':'Brown neatly styled hair, glasses, tan business suit, white shirt, olive tie, employee ID, knife.',
    'sissela':'Short pale white hair covering one eye, blue eyes, pale gray patient gown, black collar, IV drip and white orb.',
    'silvia':'Short dark blue hair with cyan accent, blue-white biker jacket, black gloves, motorbike theme.',
    'adela':'Long swept black hair, glasses, high-collar black-and-gold coat, large white chess motifs.',
    'adriana':'Bright orange-red ponytail, pipe, gray hooded utility coat, red lining, green bottle and yellow flamethrower tank.',
    'adina':'Long pale blue hair, dark blue hood with gold border, white-and-blue celestial outfit, star cards.',
    'isaac':'Swept white hair and white goatee, beige vest, red plaid shirt, dark coat, fighting baton.',
    'alex':'Straw-blond side-swept hair, black sunglasses, black spy trench coat, blue tie, radio and pistol.',
    'jan':'Dark skin, yellow eyes, thick white dreadlock topknot, bare muscular torso with dark tattoo, bandaged arms.',
    'estelle':'Chestnut bob, orange-red firefighter suit, black collar with pale pink quilted lining, neck respirator, axe and shield.',
    'aiden':'Messy white hair, red eyes, white military coat with blue collar and tie, silver belt and sword.',
    'echion':'Spiky silver-lilac hair, dark red shirt, black leather coat, black-white-magenta biomechanical arm.',
    'elena':'Pale blue long twin ponytails, ice-crown ornaments, bright blue layered ice-skating dress, large snowflake brooch.',
    'johann':'Neat dark blue-black hair, solemn face, black priest clothes, royal blue stole with gold crosses.',
    'william':'Brown hair, blue baseball cap, white pinstriped baseball jersey, blue lettering, brown arm guard, baseball.',
    'irem':'Peach-orange hair, cat ears and red headband, oversized cream-yellow cardigan, white-red cat-themed dress.',
    'eva':'Long beige-blond hair, mauve eyes, black-white sailor-like combat dress, dark arm gauntlets.',
    'ian':'Wavy pale sage-green hair, brown bow, white blouse and fitted brown dress, brass medallion, eerie book.',
    'eleven':'Pink hair, pink-white maid-idol outfit, heart brooch, pastel checked skirt, bright pink giant hammer.',
    'zahir':'Dark skin, dark wavy hair, gold-red ceremonial headpiece, white-pink-gold robes, floating gold blades.',
    'jenny':'Long blond hair, red sunglasses atop head, burgundy dress, white fur stole, movie-star elegance, pistol.',
    'camilo':'Cream-blond swept hair, teal eyes, open black pinstriped dance shirt, suspenders, twin swords.',
    'karla':'Long auburn braid, red military beret with silver rose badge, red-black cropped outfit, large crossbow.',
    'cathy':'Brown hair with blue ribbon, blue eyes, white medical coat, blue scarf, surgical tools.',
    'chloe':'Long ash gray hair and blunt fringe, white knit sleeveless sweater, brown skirt, flower belt, doll strings.',
    'chiara':'Long white hair with pale pink shading, pink eyes, black gothic puff-sleeve dress, white feather trim and violet gem.',
    'tazia':'Crimson side ponytail, gold eyes, black-red tailored coat, blue crystal flower accents and floating glass shards.',
    'theodore':'Swept dark blue-black hair, cyan eyes, dark blue tactical sniper uniform, large rifle.',
    'felix':'Short sandy-blond hair, green eyes, gray-green utility hoodie, white shirt, green patches, spear.',
    'priya':'Short green hair with braid crown, large white flower, warm brown skin, white-blue sari-like outfit, guitar.',
    'fiora':'Long brown ponytail with blue ribbons, blue-white fencing uniform, silver gauntlet and rapier.',
    'piolo':'Messy brown hair, dark green lined sleeveless hooded vest, bare torso, white waist band, nunchaku.',
    'hart':'Blond bob, black beret, bright pink rock jacket, patterned leggings, red electric guitar.',
    'haze':'Straight gray hair, black headset, charcoal business suit, white shirt and red tie, huge firearm case.',
    'debi_marlene':'Two sisters together: black short hair, baseball caps, black cropped outfits; blue jacket accents for Debi and red for Marlene.',
    'arda':'Fluffy golden hair, beige expedition shirt, green checked scarf, green backpack straps, relics and compass.',
    'abigail':'Long black-blue hair, green eyes, silver hairpin, fitted blue-white cape uniform, large blue battle axe.',
    'alonso':'Huge spiky orange mane, black mask with yellow eyes, gray-black strapped coat, amber mechanical chest accents.',
    'leni':'Light brown side bunches, black pointed bunny hat with yellow flowers, black-pink street outfit, pastel carrot bazooka.',
    'tsubame':'Dark purple hair in high knot with long strands, aqua cord, black cropped outfit, purple jacket, throwing darts.',
    'kenneth':'Gray-white hair with red earpiece, black-red hooded jacket, strapped gray combat trousers, large axe.',
    'katja':'Gray hair with long side ponytail, teal eyes, round glasses, dark gray-cyan tech sniper outfit, oversized rifle.',
    'darko':'Long swept green-gray hair, side partly shaved, dark blue open jacket, tattoos, chain pendant, dark baseball bat.',
    'lenore':'Blue hair under blue hood, white-blue bard outfit, silver armor shoulder, black chest straps, chains and guitar.',
    'garnet':'Shaggy silver-gray hair, red eyes, black strappy crop outfit, dark gauntlets, white accents, spiked metal bat.',
    'yumin':'Long black hair with side fringe, navy-white formal robe, violet neck ribbon, fan and red waist sash.',
    'hisui':'Black-teal twin ponytails with red bands, sharp grin, white-red cropped uniform, red-black neck ribbon, red-white katana.',
    'justyna':'Long bright red ponytail, visor goggles, white-gray mechanical coat with red trim, silver mechanical crossbow.',
    'istvan':'Long black hair with red-purple ends, stern face, long white military coat, black shirt, pink badge strip, long spear.',
    'xuelin':'Long dark teal hair with high loops, white-teal martial outfit, silver belt, flowing turquoise ribbons and energy rapier.',
    'henry':'Gray-lilac hair with white streak, purple eyes, brown-pink Victorian coat, burgundy tie, gold pocket watch and clock-hand throwing blades.',
    'blair':'Long cream-blond hair, black sleeveless high-neck crop outfit, silver necklace and chains, black coat, dual blades.',
    'mirka':'Long bright pink hair, black baseball cap, forest green bomber outfit, black strapped gear and silver plates.',
    'fenrir':'Olive-brown tousled hair, cyan eyes, black belted racing outfit, orange cable and motorcycle accents.',
    'coraline':'Half pale white and half dark hair, pink-violet eyes, white-black magical outfit with pink gems, black-pink angular wings.',
    'bihyung':'Black hair with bright teal strands and very long teal tail, round red glasses, white-black-red hooded street outfit, giant mallet.',
    'craver':'Messy vivid red hair, red circular glasses, black-red sharply angled coat, gray gloves, black-red revolver and red energy effects.',
    'lucia':'Pale gray twin buns and short strands, cyan eyes, white-purple ornate dress, purple ribbons, ornate long-barrel musket.',
    'ceres':'Silver bob, white lace headband, white-black armored maid outfit, black gauntlets, huge gold-black blade with pink crystal.',
}

# Exact visible prop instructions for image generation, cross-checked against
# the original game's weaponTypes and skill tooltips (not inherited style props).
PROPS = {
    'hana':'Hold a small brown laboratory notebook and a pencil.',
    'nia':'Hold a small black-and-pink game controller with visible round buttons.',
    'jackie':'Carry an oversized red-and-black battle axe over one shoulder.',
    'aya':'Hold a compact black assault rifle with a visible stock and barrel.',
    'hyunwoo':'Show clenched fists with red-and-white boxing wraps.',
    'yuki':'Hold a slender black-and-gold katana in its scabbard.',
    'hyejin':'Hold a purple paper talisman with a dark ink rune.',
    'sua':'Hold a green storybook and a small green storybook hammer.',
    'isol':'Carry a black assault rifle over one shoulder.',
    'nadine':'Hold a compact hunting crossbow with clearly visible transverse bow limbs.',
    'emma':'Fan out a few white playing cards with black suit symbols.',
    'charlotte':'Float a few pale blue crystal blades beside her hands.',
    'nathapon':'Hold a black DSLR camera with a long black-and-white lens.',
    'nicky':'Wear bulky orange-and-black boxing gloves and hold both fists up.',
    'daniel':'Hold an open pair of large silver tailor scissors with dark handles.',
    'tia':'Hold a long broad paintbrush with a brown handle and visible bristles.',
    'laura':'Hold a coiled black-and-violet whip decorated with a small violet flower.',
    'lenox':'Hold a long fishing-line whip with a dark grip and a visible curved line.',
    'leon':'Show wrapped bare fists with a few small turquoise water droplets.',
    'rozzi':'Hold two compact black pistols, one in each hand.',
    'luke':'Hold a janitor broom with a long handle and a wide brush head.',
    'dailin':'Hold a small brown liquor bottle with a red neck cord.',
    'rio':'Hold a tall traditional archery bow with a visible curved limb and bowstring.',
    'martina':'Carry a large blue-and-white camcorder on one shoulder.',
    'mai':'Hold a long teal-and-gold measuring ribbon that curls like a soft whip.',
    'markus':'Carry a heavy silver battle axe with a very broad axe head.',
    'magnus':'Carry a thick black metal baseball bat across his shoulder.',
    'vanya':'Let a few small bright blue butterflies float around her open hand.',
    'barbara':'Hold a small steel engineering wrench beside a tiny blue robotic device.',
    'bernice':'Hold a hunting rifle with a brown wooden stock and a long black barrel.',
    'bianca':'Hold a black gothic parasol and a small red blood bag.',
    'celine':'Hold one green hand grenade with a visible pin ring.',
    'sho':'Hold an oversized steel cooking cleaver and a small dark wok.',
    'shoichi':'Hold a short silver dagger with a dark handle.',
    'sissela':'Float a round white companion orb beside a thin medical IV stand.',
    'silvia':'Hold a compact blue-and-black pistol beside her motorcycle helmet.',
    'adela':'Hold a slim chess-themed rapier with a white chess-piece guard.',
    'adriana':'Carry a yellow flamethrower tank with a short hose and spray nozzle.',
    'adina':'Hold a few dark blue tarot cards decorated with pale gold stars.',
    'isaac':'Hold a black police tonfa with a clearly visible side handle.',
    'alex':'Hold a compact black pistol and a small black spy radio.',
    'jan':'Show both fists clenched with white bandages around his forearms.',
    'estelle':'Hold a red fire axe and a broad gray firefighter shield.',
    'aiden':'Hold a long silver sword with small blue electrical sparks.',
    'echion':'Show his large black-white-magenta biomechanical VF arm as a curved claw.',
    'elena':'Hold a slender ice-blue rapier with a small snowflake-shaped guard.',
    'johann':'Hold a small gold Christian cross in front of his blue priest stole.',
    'william':'Hold a white baseball and wear a brown baseball glove.',
    'irem':'Wear cream cat-paw mittens with visible pink paw pads.',
    'eva':'Float a small violet VF energy orb above one open hand.',
    'ian':'Hold a short ritual dagger with a dark monster shadow behind her shoulder.',
    'eleven':'Carry a huge pastel pink hammer with a bright heart motif.',
    'zahir':'Float a few pointed gold throwing blades around one hand.',
    'jenny':'Hold an elegant compact black pistol with a small gold accent.',
    'camilo':'Hold two slim silver dancing swords, one in each hand.',
    'karla':'Hold an oversized red-and-black crossbow with visible curved bow limbs.',
    'cathy':'Hold a small steel surgical scalpel and a compact medical pouch.',
    'chloe':'Use fingertip puppet strings connected to Nina, a small gray twin-tailed doll in a white ruffle dress with lilac bows and black mechanical blade legs.',
    'chiara':'Hold an ornate black rapier with a violet gemstone at the guard.',
    'tazia':'Float a few sharp translucent blue and lilac glass shards near one hand.',
    'theodore':'Hold a long dark navy sniper rifle with a visible scope.',
    'felix':'Hold a long silver spear with a single pointed spearhead.',
    'priya':'Hold a blue acoustic guitar decorated with small white flowers.',
    'fiora':'Hold a slender silver fencing rapier with a round guard.',
    'piolo':'Hold black-and-silver nunchaku with two short sticks joined by a chain.',
    'hart':'Hold a bright red electric guitar with a dark neck.',
    'haze':'Carry a very large gray assault weapon with a boxy barrel and stock.',
    'debi_marlene':'Give the blue sister a blue-accented greatsword and the red sister a red-accented greatsword.',
    'arda':'Hold a small carved ancient stone relic and a rolled parchment scroll.',
    'abigail':'Hold a large blue battle axe with a broad crescent-shaped axe head.',
    'alonso':'Wear very large dark magnetic gauntlets with amber glowing panels.',
    'leni':'Hold a pastel pink carrot-themed handheld launcher with a short visible barrel.',
    'tsubame':'Hold a few black-and-turquoise shuriken with visible pointed blades.',
    'kenneth':'Carry a huge red-and-black battle axe with a broad axe head.',
    'katja':'Hold an oversized black-and-teal sniper rifle with a scope.',
    'darko':'Hold a rugged dark baseball bat with a thick wrapped grip.',
    'lenore':'Hold a dark blue electric guitar decorated with small silver chains.',
    'garnet':'Carry a thick black metal bat covered with silver spikes.',
    'yumin':'Hold an open violet-and-teal hand fan with clearly visible folds.',
    'hisui':'Hold a red-and-white katana with a dark handle.',
    'justyna':'Hold a futuristic silver crossbow with broad red mechanical bow limbs.',
    'istvan':'Hold a long dark silver spear with a narrow pointed head and red ribbon.',
    'xuelin':'Hold a slender turquoise energy sword shaped like a fencing rapier.',
    'henry':'Hold a gold pocket watch while pointed gold clock hands float as throwing blades.',
    'blair':'Hold two slender black-and-silver blades, one in each hand.',
    'mirka':'Carry a heavy square-headed steel hammer with dark green accents.',
    'fenrir':'Wear black-and-orange racing gloves and show both fists raised.',
    'coraline':'Float angular pink crystal arcana beside her black-and-pink fairy wings.',
    'bihyung':'Carry a giant black-and-red mallet with a square mechanical head.',
    'craver':'Hold a large black-and-red revolver with a clearly visible cylinder.',
    'lucia':'Hold an ornate silver-and-violet long-barrel musket with a shoulder stock.',
    'ceres':'Hold a huge black-and-gold greatsword with a large magenta crystal near the guard.',
}

PROP_WARNINGS = {
    'nicky':'No tablets, knives, books, cameras, or glowing handheld devices; use boxing gloves.',
    'daniel':'No magic books, talismans, or cards; use clearly open tailor scissors.',
    'rio':'No camera, camcorder, grenade, or firearm; show an unmistakable archery bow.',
    'luke':'No wrench or sword; use the wide-bristled cleaning broom.',
    'leon':'No guitar, violin case, firearm, or sword; use wrapped fists and water droplets.',
    'laura':'Do not copy Bianca\'s parasol; her distinctive weapon is a flexible whip.',
    'markus':'No narrow sword; the weapon must have a very broad axe or hammer head.',
    'abigail':'Do not mistake her large axe for a straight sword.',
    'darko':'Do not add a firearm; his weapon class is a baseball bat.',
    'garnet':'Use a spiked metal bat, not an axe.',
    'justyna':'Show the crossbow\'s transverse limbs, not only gauntlets.',
    'istvan':'Use a long spear, not a short sword.',
    'xuelin':'Use the slender energy sword, not a polearm.',
    'craver':'His red energy effects must not replace the revolver silhouette.',
    'lucia':'Use an ornate long-barrel musket rather than two short pistols.',
}


def fetch(url, destination):
    destination = Path(destination)
    if destination.exists():
        return True
    try:
        request = urllib.request.Request(url, headers={'User-Agent':'Mozilla/5.0'})
        data = urllib.request.urlopen(request, timeout=35).read()
        if len(data) < 100:
            return False
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(data)
        return True
    except Exception as error:
        print('REFERENCE MISSING', url, type(error).__name__, flush=True)
        return False


def dak_data(path):
    text = path.read_text(encoding='utf8')
    match = re.search(r'<script\s+id="__NEXT_DATA__"\s+type="application/json">(.*?)</script>', text)
    return json.loads(match.group(1))['props']['pageProps']


def original_characters():
    path=OUT/'skill-references/original-characters.json'
    fetch('https://er.dakgg.io/api/v1/data/characters?hl=en',path)
    return json.loads(path.read_text(encoding='utf8'))['characters']


def font(size):
    return ImageFont.truetype(str(ROOT / 'Assets/Resources/Lumia/Galmuri11.ttf'), size)


def sprite_montage(entries, number):
    cell_width, cell_height = 310, 450
    sheet = Image.new('RGB', (4 * cell_width, 2 * cell_height), '#273342')
    draw = ImageDraw.Draw(sheet)
    for index, entry in enumerate(entries):
        file = OUT / 'references' / 'subjects' / f"{entry['id']}.png"
        if not file.exists():
            continue
        image = Image.open(file).convert('RGBA')
        bbox = image.getbbox()
        if bbox: image = image.crop(bbox)
        image.thumbnail((cell_width - 22, cell_height - 44), Image.Resampling.LANCZOS)
        x, y = (index % 4) * cell_width, (index // 4) * cell_height
        sheet.paste(image, (x + (cell_width - image.width)//2, y + 10 + (cell_height - 42 - image.height)//2), image)
        draw.text((x + 12, y + cell_height - 28), f"{entry['id']}  {entry['name']}", fill='white', font=font(18))
    path = OUT / 'references' / f'sprites_{number:02}.png'
    sheet.save(path)
    print('MONTAGE', path, flush=True)
    return str(path.relative_to(ROOT)).replace('\\','/')


def prepare_sprites():
    manifest = json.loads((OUT / 'game-art-manifest.json').read_text(encoding='utf8'))
    available=original_characters()
    keys = {entry['key'].lower(): entry['key'] for entry in available}
    records = []
    for entry in manifest['sprites']:
        identifier = entry['id']
        key = keys.get(KEY_OVERRIDES.get(identifier,identifier).replace('_','').lower())
        record = dict(entry, cdn_key=key, original_url=CDN + f'ui/characterhalfsize/CharFull_{key}_S000.png' if key else None,
                      appearance=APPEARANCE.get(identifier),reference_path=f'docs/art-chibi/references/subjects/{identifier}.png')
        records.append(record)
    reference_dir = OUT / 'references' / 'subjects'
    reference_dir.mkdir(parents=True,exist_ok=True)
    # Existing official Dr. Hana art; crop only the full-height researcher region.
    hana = Image.open(ROOT / 'docs/art-references/HanaEvent.png').convert('RGBA')
    hana.crop((565, 22, 947, hana.height)).save(reference_dir / 'hana.png')
    records[0]['original_url'] = 'https://playeternalreturn.com/posts/news/3330?hl=ko-KR'
    records[0]['reference_note']='Official Dr. Hana event art, cropped to researcher figure.'
    nia=next(r for r in records if r['id']=='nia')
    nia['cdn_art_url']=nia['original_url']
    nia['original_url']='https://cdn.playeternalreturn.com/event/season6/roadmap/rd6/img_concept01.png'
    nia['reference_note']='Official NiaH concept sheet, front-view character cropped for full outfit continuity.'
    nia_image=Image.open(ROOT / 'docs/art-references/NiaConcept.png').convert('RGBA')
    nia_image.crop((145,105,555,1030)).save(reference_dir/'nia.png')
    chloe=next(r for r in records if r['id']=='chloe')
    chloe['companion_reference']='docs/art-chibi/references/chloe_nina_full.png'
    chloe['companion_source']='https://store.steampowered.com/news/app/1049590/view/3044989230676877444'
    def download_record(record):
        if record['id'] in ['hana','nia']: return True
        if not record['cdn_key']: return False
        return fetch(record['original_url'], reference_dir / f"{record['id']}.png")
    with concurrent.futures.ThreadPoolExecutor(max_workers=12) as executor:
        for offset in range(0, len(records), 8):
            group = records[offset:offset+8]
            for record, success in zip(group, executor.map(download_record, group)):
                record['reference_available'] = success
            sprite_montage(group, offset//8+1)
            (OUT / 'reference-characters.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf8')
    print('SPRITE REFERENCES',sum(r.get('reference_available',False) for r in records),'/',len(records),flush=True)
    assert all(r['appearance'] for r in records), 'Every reference needs an inspected visual summary.'
    return records


def skill_montage(rows, number):
    cell_width, cell_height = 192, 216
    sheet = Image.new('RGB', (5 * cell_width, 4 * cell_height), '#202a39')
    draw = ImageDraw.Draw(sheet)
    for row_index, row in enumerate(rows):
        for column, slot in enumerate(['T','Q','W','E','R']):
            record = row.get(slot)
            if not record: continue
            image = Image.open(ROOT / record['reference_path']).convert('RGBA')
            image.thumbnail((168,168),Image.Resampling.LANCZOS)
            x,y = column*cell_width, row_index*cell_height
            sheet.paste(image,(x+(cell_width-image.width)//2,y+22+(168-image.height)//2),image)
            draw.text((x+10,y+3),f"{row['id']}  {slot}",font=font(16),fill='white')
            draw.text((x+10,y+192),record['card_name'][:12],font=font(14),fill='#c6d5e4')
    path = OUT / 'references' / f'skills_{number:02}.png'
    sheet.save(path)
    print('SKILL MONTAGE',path,flush=True)


def prepare_skills():
    references=json.loads((OUT / 'reference-characters.json').read_text(encoding='utf8'))[1:]
    characters=original_characters()
    number_by_key={r['key']:r['id'] for r in characters}
    skills_source=OUT / 'skill-references' / 'original-skills.json'
    fetch('https://er.dakgg.io/api/v1/data/skills?hl=en',skills_source)
    original_skills=json.loads(skills_source.read_text(encoding='utf8'))['skills']
    manifest=json.loads((OUT / 'game-art-manifest.json').read_text(encoding='utf8'))
    card_names={r['id']:r['name'] for r in manifest['cards']+manifest['passives']}
    records=[]
    for character in references:
        original_number=number_by_key[character['cdn_key']]
        for slot in ['T','Q','W','E','R']:
            matches=[r for r in original_skills if r.get('characterId')==original_number and r.get('slot')==slot]
            if not matches:
                print('MISSING SKILL',character['id'],slot,flush=True)
                continue
            original=min(matches,key=lambda r:r['id'])
            card_id=character['id']+('_p' if slot=='T' else '_'+slot.lower())
            records.append({'id':card_id,'subject_id':character['id'],'subject_name':character['name'],
                            'slot':slot,'card_name':card_names.get(card_id,original['name']),
                            'original_name':original['name'],'original_number':original['id'],
                            'image_url':original['imageUrl'],'reference_path':f'docs/art-chibi/skill-references/{card_id}.png',
                            'tooltip':original['tooltip']})
    def download(record): return fetch(record['image_url'],ROOT / record['reference_path'])
    with concurrent.futures.ThreadPoolExecutor(max_workers=18) as executor:
        for record,success in zip(records,executor.map(download,records)):
            record['reference_available']=success
    rows=[]
    for character in references:
        row={'id':character['id']}
        for r in records:
            if r['subject_id']==character['id'] and r['reference_available']: row[r['slot']]=r
        rows.append(row)
    for offset in range(0,len(rows),4): skill_montage(rows[offset:offset+4],offset//4+1)
    (OUT / 'reference-skills.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf8')
    print('SKILL REFERENCES',sum(r['reference_available'] for r in records),'/',len(records),flush=True)
    return records


def prepare_extras():
    manifest=json.loads((OUT / 'game-art-manifest.json').read_text(encoding='utf8'))
    source=json.loads((OUT / 'skill-references/original-skills.json').read_text(encoding='utf8'))['skills']
    tactics_path=OUT / 'skill-references/original-tactical-skills.json'
    fetch('https://er.dakgg.io/api/v1/data/tactical-skills?hl=en',tactics_path)
    tactics=json.loads(tactics_path.read_text(encoding='utf8'))['tacticalSkills']
    weapon_ids={'weapon_glove':1,'weapon_pistol':9,'weapon_dagger':15,'weapon_axe':14,
                'weapon_sword':16,'weapon_bow':7,'weapon_hammer':13,'weapon_rapier':21,'weapon_sniper':11}
    tactical_ids={'tactical_blink':30,'tactical_wind':150,'tactical_plasma':170}
    records=[]
    for card in manifest['cards']:
        if card['category']=='skill': continue
        if card['id'] in weapon_ids:
            original=next(r for r in source if r.get('weaponTypeId')==weapon_ids[card['id']])
            note='원작 무기군 스킬 아이콘입니다.'
        elif card['id'] in tactical_ids:
            original=next(r for r in tactics if r['id']==tactical_ids[card['id']])
            note='원작 전술 스킬 아이콘입니다.'
        elif card['id']=='basic_attack':
            original={'name':'Glove mastery pictogram','imageUrl':CDN+'Ico_Ability_Glove.png'}
            note='기본 공격에는 독립된 원작 QWER 아이콘이 없어 글러브 무기군의 주먹 픽토그램을 시각적 참고로 사용합니다.'
        elif card['id']=='basic_guard':
            original=next(r for r in tactics if r['id']==70)
            note='이 팬 게임 전용 경계 카드에는 독립된 원작 스킬 아이콘이 없어 포스 필드의 방어막 형상을 시각적 참고로 사용합니다.'
        else: continue
        r={'id':card['id'],'card_name':card['name'],'original_name':original['name'],
           'image_url':original['imageUrl'],'reference_path':f"docs/art-chibi/skill-references/{card['id']}.png",'note':note}
        r['reference_available']=fetch(r['image_url'],ROOT/r['reference_path'])
        records.append(r)
    sheet=Image.new('RGB',(960,864),'#202a39')
    draw=ImageDraw.Draw(sheet)
    for index,r in enumerate(records):
        if not r['reference_available']:continue
        image=Image.open(ROOT/r['reference_path']).convert('RGBA')
        image.thumbnail((168,168),Image.Resampling.LANCZOS)
        x,y=(index%5)*192,(index//5)*216
        sheet.paste(image,(x+(192-image.width)//2,y+22+(168-image.height)//2),image)
        draw.text((x+8,y+3),r['id'].replace('weapon_','D ').replace('tactical_','F '),font=font(16),fill='white')
        draw.text((x+8,y+192),r['card_name'][:12],font=font(14),fill='#c6d5e4')
    sheet.save(OUT/'references/skills_extra.png')
    (OUT/'reference-extra-skills.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf8')
    print('EXTRA REFERENCES',sum(r['reference_available'] for r in records),'/',len(records),flush=True)
    return records


def reference_groups():
    manifest=json.loads((OUT / 'game-art-manifest.json').read_text(encoding='utf8'))
    sprites=manifest['sprites']
    characters=sprites[1:]
    groups={'schema':1,'sprites':[], 'skills':[]}
    for offset in range(0,len(sprites),8):
        groups['sprites'].append({'group':offset//8+1,'reference_path':f'docs/art-chibi/references/sprites_{offset//8+1:02}.png',
                                 'layout':'4 columns x 2 rows, row-major; unused tail cells blank',
                                 'ids':[r['id'] for r in sprites[offset:offset+8]]})
    for offset in range(0,len(characters),4):
        rows=characters[offset:offset+4]
        groups['skills'].append({'group':offset//4+1,'reference_path':f'docs/art-chibi/references/skills_{offset//4+1:02}.png',
                                'layout':'5 columns T,Q,W,E,R x 4 character rows; unused tail row blank',
                                'subjects':[r['id'] for r in rows],
                                'ids':[r['id']+('_p' if s=='T' else '_'+s.lower()) for r in rows for s in ['T','Q','W','E','R']]})
    extras=json.loads((OUT/'reference-extra-skills.json').read_text(encoding='utf8'))
    groups['extra']={'reference_path':'docs/art-chibi/references/skills_extra.png','layout':'5 columns x 4 rows, row-major; 14 occupied then 6 blank cells',
                     'ids':[r['id'] for r in extras]}
    (OUT/'reference-groups.json').write_text(json.dumps(groups,ensure_ascii=False,indent=2),encoding='utf8')


def write_reference_documentation():
    characters=json.loads((OUT/'reference-characters.json').read_text(encoding='utf8'))
    skills=json.loads((OUT/'reference-skills.json').read_text(encoding='utf8'))
    extras=json.loads((OUT/'reference-extra-skills.json').read_text(encoding='utf8'))
    text=[
        '# 치비 도트 제작용 원작 참고자료',
        '',
        '이 디렉터리의 원작 이미지는 도트 재해석을 위한 참고자료입니다. 게임 Assets에 원작 이미지를 복사하지 않습니다. 이터널 리턴의 IP와 원작 이미지의 권리는 님블뉴런에 귀속됩니다.',
        '',
        f'하나와 플레이 가능한 실험체 91명을 합쳐 외형 참고 {len(characters)}명, Q/W/E/R 카드 364개와 T 패시브 91개의 원본 아이콘 {len(skills)}개, 무기·전술·기본 카드용 시각 참고 {len(extras)}개를 확보했습니다.',
        '',
        '외형은 원작 기본 스킨의 머리색, 장신구, 옷과 무기를 기준으로 확인했습니다. 대부분의 CDN 캐릭터 그림은 상반신 구도이므로 하반신은 상반신 의상·색상과 공식 캐릭터 장비에 맞게 간결하게 이어 그립니다. 니아는 공식 전신 콘셉트에서 앞모습을 잘라 사용하고, 하나는 공식 이벤트 그림에서 연구원 영역을 잘라 사용합니다.',
        '',
        '스프라이트 몽타주 `references/sprites_01.png`~`sprites_12.png`는 4열×2행이며 게임 manifest sprites 순서입니다. 마지막 몽타주는 4명의 실험체와 빈칸 4개입니다.',
        '',
        '스킬 몽타주 `references/skills_01.png`~`skills_23.png`는 5열×4행이며 각 캐릭터의 T, Q, W, E, R 순서입니다. 마지막 몽타주는 실험체 3명과 빈 행 1개입니다. `references/skills_extra.png`도 5열×4행이며 기본2·무기9·전술3 순서로 첫14칸만 사용합니다. 정확한 셀 ID는 `reference-groups.json`에 기록했습니다.',
        '',
        '기본 공격은 글러브 무기군 주먹 픽토그램, 팬 게임 전용 경계는 포스 필드 방어막을 형상 참고로 사용합니다. 이 2개는 동일 이름의 원작 스킬 아이콘을 주장하지 않습니다. 나머지467개는 해당 실험체·무기·전술 스킬에 대응하는 실제 원작 아이콘입니다.',
        '',
        '## 출처와 재현',
        '',
        '- [원작 실험체 소개 및 기본 스킨](https://dak.gg/er/characters/Jackie/introduction): 공개 페이지가 사용하는 실제 원작 이미지 CDN 파일명을 확인했습니다.',
        '- [공개 실험체 스킬 데이터](https://er.dakgg.io/api/v1/data/skills?hl=en): `characterId`, `slot`, `imageUrl`을 사용해 같은 캐릭터의 T/Q/W/E/R을 연결했습니다. 파생·변신 슬롯이 여러 개인 경우 기본형의 가장 낮은 원본 ID를 참고했습니다.',
        '- [공개 전술 스킬 데이터](https://er.dakgg.io/api/v1/data/tactical-skills?hl=en): 블링크, 치유의 바람, 플라즈마 대시의 실제 아이콘 주소를 확인했습니다.',
        '- [공식 니아 콘셉트](https://cdn.playeternalreturn.com/event/season6/roadmap/rd6/img_concept01.png), [공식 하나 이벤트](https://playeternalreturn.com/posts/news/3330?hl=ko-KR).',
        '',
        f'확인한 게임 CDN 버전은 `{VERSION}`입니다. `Tools/PrepareArtReferences.py`가 자료를 다시 만들며, `reference-characters.json`, `reference-skills.json`, `reference-extra-skills.json`에 개별 원본 URL과 게임 ID를 저장합니다. 다운로드된 JSON은 `skill-references/original-skills.json`과 `original-tactical-skills.json`에 보존합니다.',
        '',
        '## 캐릭터 외형 요약',
        '',
        '| 게임 ID | 이름 | CDN 키 | 확인한 외형 |',
        '|---|---|---|---|',
    ]
    for r in characters:text.append(f"| {r['id']} | {r['name']} | {r['cdn_key'] or 'Official Hana event'} | {r['appearance']} |")
    (OUT/'reference-notes.md').write_text('\n'.join(text)+'\n',encoding='utf8')


def prepare_character_props():
    originals=original_characters()
    original_by_key={c['key']:c for c in originals}
    characters=json.loads((OUT/'reference-characters.json').read_text(encoding='utf8'))
    result={'schema':1,'source':'https://er.dakgg.io/api/v1/data/characters?hl=en',
            'skill_source':'https://er.dakgg.io/api/v1/data/skills?hl=en',
            'note':'Weapon classes are original-game data; signature props are visual instructions checked against default artwork and ability actions. Do not inherit a different character\'s prop from the style master.',
            'characters':{}}
    for r in characters:
        c=original_by_key.get(r['cdn_key'])
        weapons=[w['key'] for w in c['weaponTypes']] if c else []
        entry={'name':r['name'],'cdn_key':r['cdn_key'],'weapon_types':weapons,
               'signature_prop':PROPS[r['id']],
               'avoid':PROP_WARNINGS.get(r['id'],'Do not inherit a camera, book, rifle, or glowing device from another character unless explicitly specified here.')}
        result['characters'][r['id']]=entry
        r['weapon_types']=weapons
        r['signature_prop']=entry['signature_prop']
        r['prop_avoid']=entry['avoid']
    assert len(result['characters'])==92
    (OUT/'character-props.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    (OUT/'reference-characters.json').write_text(json.dumps(characters,ensure_ascii=False,indent=2),encoding='utf8')
    text=['# 실험체 스프라이트 대표 소품','',
          '원작 공개 캐릭터 데이터의 무기군과 기본 스킨 외형, 스킬 행동을 함께 확인한 생성용 소품 지시입니다. 스타일 참고 시트는 머리·몸 비율과 도트 기법만 따르며, 다른 셀의 책·카메라·발광 태블릿을 재사용하지 않습니다. 하나는 연구 노트, 니아는 컨트롤러처럼 무기군 자체보다 캐릭터성을 드러내는 대표 소품을 우선할 수 있습니다.','',
          '원작 데이터에서 확인한 수정점: 아비게일은 도끼, 가넷과 다르코는 방망이, 유스티나는 석궁, 이슈트반은 창, 슈린은 레이피어, 크레이버는 권총, 루치아는 저격총입니다. 헨리는 스킬에서 실제 시곗바늘을 투척하므로 금색 시곗바늘과 회중시계를 지정합니다.','',
          '| ID | 원작 무기군 | 셀에 넣을 소품 |','|---|---|---|']
    for identifier,r in result['characters'].items():text.append(f"| {identifier} | {', '.join(r['weapon_types']) or 'Researcher'} | {r['signature_prop']} |")
    (OUT/'reference-character-props.md').write_text('\n'.join(text)+'\n',encoding='utf8')
    print('CHARACTER PROPS',len(result['characters']),flush=True)


def inspect_dak():
    text = (TEMP / 'dak-jackie.html').read_text(encoding='utf8')
    match = re.search(r'<script\s+id="__NEXT_DATA__"\s+type="application/json">(.*?)</script>', text)
    data = json.loads(match.group(1))
    (TEMP / 'dak-jackie.json').write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf8')
    print(data['props']['pageProps'].keys())
    print(str(data['props']['pageProps'])[:4500])
    print('\n'.join(sorted(set(re.findall(r'https[^\s<>]*?(?:png|webp)',text)))[-35:]))


if __name__ == '__main__':
    prepare_sprites()
    prepare_skills()
    prepare_extras()
    reference_groups()
    write_reference_documentation()
    prepare_character_props()
