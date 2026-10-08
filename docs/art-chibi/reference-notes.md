# 치비 도트 제작용 원작 참고자료

이 디렉터리의 원작 이미지는 도트 재해석을 위한 참고자료입니다. 게임 Assets에 원작 이미지를 복사하지 않습니다. 이터널 리턴의 IP와 원작 이미지의 권리는 님블뉴런에 귀속됩니다.

하나와 플레이 가능한 실험체 91명을 합쳐 외형 참고 92명, Q/W/E/R 카드 364개와 T 패시브 91개의 원본 아이콘 455개, 무기·전술·기본 카드용 시각 참고 14개를 확보했습니다.

외형은 원작 기본 스킨의 머리색, 장신구, 옷과 무기를 기준으로 확인했습니다. 대부분의 CDN 캐릭터 그림은 상반신 구도이므로 하반신은 상반신 의상·색상과 공식 캐릭터 장비에 맞게 간결하게 이어 그립니다. 니아는 공식 전신 콘셉트에서 앞모습을 잘라 사용하고, 하나는 공식 이벤트 그림에서 연구원 영역을 잘라 사용합니다.

스프라이트 몽타주 `references/sprites_01.png`~`sprites_12.png`는 4열×2행이며 게임 manifest sprites 순서입니다. 마지막 몽타주는 4명의 실험체와 빈칸 4개입니다.

스킬 몽타주 `references/skills_01.png`~`skills_23.png`는 5열×4행이며 각 캐릭터의 T, Q, W, E, R 순서입니다. 마지막 몽타주는 실험체 3명과 빈 행 1개입니다. `references/skills_extra.png`도 5열×4행이며 기본2·무기9·전술3 순서로 첫14칸만 사용합니다. 정확한 셀 ID는 `reference-groups.json`에 기록했습니다.

기본 공격은 글러브 무기군 주먹 픽토그램, 팬 게임 전용 경계는 포스 필드 방어막을 형상 참고로 사용합니다. 이 2개는 동일 이름의 원작 스킬 아이콘을 주장하지 않습니다. 나머지467개는 해당 실험체·무기·전술 스킬에 대응하는 실제 원작 아이콘입니다.

## 출처와 재현

- [원작 실험체 소개 및 기본 스킨](https://dak.gg/er/characters/Jackie/introduction): 공개 페이지가 사용하는 실제 원작 이미지 CDN 파일명을 확인했습니다.
- [공개 실험체 스킬 데이터](https://er.dakgg.io/api/v1/data/skills?hl=en): `characterId`, `slot`, `imageUrl`을 사용해 같은 캐릭터의 T/Q/W/E/R을 연결했습니다. 파생·변신 슬롯이 여러 개인 경우 기본형의 가장 낮은 원본 ID를 참고했습니다.
- [공개 전술 스킬 데이터](https://er.dakgg.io/api/v1/data/tactical-skills?hl=en): 블링크, 치유의 바람, 플라즈마 대시의 실제 아이콘 주소를 확인했습니다.
- [공식 니아 콘셉트](https://cdn.playeternalreturn.com/event/season6/roadmap/rd6/img_concept01.png), [공식 하나 이벤트](https://playeternalreturn.com/posts/news/3330?hl=ko-KR).

확인한 게임 CDN 버전은 `12.5.0`입니다. `Tools/PrepareArtReferences.py`가 자료를 다시 만들며, `reference-characters.json`, `reference-skills.json`, `reference-extra-skills.json`에 개별 원본 URL과 게임 ID를 저장합니다. 다운로드된 JSON은 `skill-references/original-skills.json`과 `original-tactical-skills.json`에 보존합니다.

## 캐릭터 외형 요약

| 게임 ID | 이름 | CDN 키 | 확인한 외형 |
|---|---|---|---|
| hana | 하나 | Official Hana event | Warm brown bob, amber eyes, yellow zigzag hair clip, green sweater, white laboratory coat, brown plaid skirt, books. |
| nia | 니아 | Niah | Dusty lavender bob with curled side bunches, pink headphones, lilac-pink hooded vest, magenta plaid sleeves, controller. |
| jackie | 재키 | Jackie | Spiky white hair, red eyes, red-and-black sleeveless outfit, black gloves, oversized red axe or chainsaw. |
| aya | 아야 | Aya | Brown braids, round glasses, olive police jacket, navy skirt, dark firearm. |
| hyunwoo | 현우 | Hyunwoo | Red-orange short hair, open brown school jacket, white shirt, clenched fists. |
| yuki | 유키 | Yuki | Blue-black short hair, black Japanese school uniform with gold trim, katana. |
| hyejin | 혜진 | Hyejin | Long black braids, white-and-black sailor uniform, purple talismans. |
| sua | 수아 | Sua | Long brown hair, pale floral green dress, book and storybook hammer. |
| isol | 아이솔 | Isol | Light brown hair, red headband and checked scarf, white sleeveless shirt, olive tactical harness, assault rifle. |
| nadine | 나딘 | Nadine | Dark high ponytail, orange cap and hooded mantle, fitted dark hunting gear, bow or crossbow. |
| emma | 엠마 | Emma | Turquoise twin braids and bunny-shaped bow, white-and-cyan magician outfit, playing cards. |
| charlotte | 샬럿 | Charlotte | Long pink hair, lace maid headband, white and black gothic dress, floating pale crystal blades. |
| nathapon | 나타폰 | Nathapon | Brown side-swept hair, backward white cap, light-blue denim vest, white shirt, big camera. |
| nicky | 니키 | Nicky | Blond bob, crossed green/orange hair clips, bright orange cropped sports jacket, boxing gloves. |
| daniel | 다니엘 | Daniel | Long dark-purple hair, pale face, high-collar black strapped jacket, tailor scissors. |
| tia | 띠아 | Tia | Honey-blond long hair with rose-shaped side knot, yellow ribbon, brown artist outfit, broad paintbrush. |
| laura | 라우라 | Laura | Long silvery lilac hair, glossy black high-collar suit, dark blue mantle, hydrangea and whip. |
| lenox | 레녹스 | Lenox | Short green hair, red-brown blazer worn on shoulders, black crop top, sleeve tattoo, fishing whip. |
| leon | 레온 | Leon | Brown hair, white headphones, blue-white open sports hoodie, orange shoulder strap, swimmer theme. |
| rozzi | 로지 | Rozzi | Black wavy bob partly covering one eye, sleeveless black and gold tactical dress, dual pistols. |
| luke | 루크 | Luke | Spiky light brown hair, yellow goggles around neck, white-red flame shirt, dark work overalls, broom. |
| dailin | 리 다이린 | LiDailin | Black twin hair buns with long orange ribbons, yellow-black cropped jacket, dragon embroidery, bottle. |
| rio | 리오 | Rio | Long straight white hair and blunt bangs, yellow eyes, gray-black sailor uniform, yellow neckerchief, bow. |
| martina | 마르티나 | Martina | Gray side-swept hair, black eyepatch, mustard trench coat, large shoulder video camera. |
| mai | 마이 | Mai | Auburn bob, white sleeveless fashion blouse, black-and-white striped neck scarf, teal-gold draped sleeves. |
| markus | 마커스 | Markus | Short black hair, trimmed beard, dark gray tank top, huge silver shoulder guard, battle axe. |
| magnus | 매그너스 | Magnus | Bright blond-orange mohawk, black studded biker vest, broad build, bat and motorcycle theme. |
| vanya | 바냐 | Vanya | Short pale blue hair, cyan butterfly hair ornaments, white-and-cyan ribbon dress, blue butterfly wings. |
| barbara | 바바라 | Barbara | Brown ponytail, glasses, chunky white-gray visor helmet, cobalt blue mechanic suit, orange harness, robot tools. |
| bernice | 버니스 | Bernice | Shaggy blond hair, fur-trimmed yellow field coat, green camouflage layer, hunting shotgun. |
| bianca | 비앙카 | Bianca | Dark teal hair, red eyes, maid lace headband, black gothic dress, dark parasol and blood bag. |
| celine | 셀린 | Celine | Straight icy blue bob, yellow eyes, orange utility jacket, black crop top, demolition gear. |
| sho | 쇼우 | Xiukai | Round smiling chef, dark topknot, bamboo-rimmed straw hat, white chef coat, red scarf, bamboo cooking tools. |
| shoichi | 쇼이치 | Shoichi | Brown neatly styled hair, glasses, tan business suit, white shirt, olive tie, employee ID, knife. |
| sissela | 시셀라 | Sissela | Short pale white hair covering one eye, blue eyes, pale gray patient gown, black collar, IV drip and white orb. |
| silvia | 실비아 | Silvia | Short dark blue hair with cyan accent, blue-white biker jacket, black gloves, motorbike theme. |
| adela | 아델라 | Adela | Long swept black hair, glasses, high-collar black-and-gold coat, large white chess motifs. |
| adriana | 아드리아나 | Adriana | Bright orange-red ponytail, pipe, gray hooded utility coat, red lining, green bottle and yellow flamethrower tank. |
| adina | 아디나 | Adina | Long pale blue hair, dark blue hood with gold border, white-and-blue celestial outfit, star cards. |
| isaac | 아이작 | Isaac | Swept white hair and white goatee, beige vest, red plaid shirt, dark coat, fighting baton. |
| alex | 알렉스 | Alex | Straw-blond side-swept hair, black sunglasses, black spy trench coat, blue tie, radio and pistol. |
| jan | 얀 | Jan | Dark skin, yellow eyes, thick white dreadlock topknot, bare muscular torso with dark tattoo, bandaged arms. |
| estelle | 에스텔 | Estelle | Chestnut bob, orange-red firefighter suit, black collar with pale pink quilted lining, neck respirator, axe and shield. |
| aiden | 에이든 | Aiden | Messy white hair, red eyes, white military coat with blue collar and tie, silver belt and sword. |
| echion | 에키온 | Echion | Spiky silver-lilac hair, dark red shirt, black leather coat, black-white-magenta biomechanical arm. |
| elena | 엘레나 | Elena | Pale blue long twin ponytails, ice-crown ornaments, bright blue layered ice-skating dress, large snowflake brooch. |
| johann | 요한 | Johann | Neat dark blue-black hair, solemn face, black priest clothes, royal blue stole with gold crosses. |
| william | 윌리엄 | William | Brown hair, blue baseball cap, white pinstriped baseball jersey, blue lettering, brown arm guard, baseball. |
| irem | 이렘 | Irem | Peach-orange hair, cat ears and red headband, oversized cream-yellow cardigan, white-red cat-themed dress. |
| eva | 이바 | Eva | Long beige-blond hair, mauve eyes, black-white sailor-like combat dress, dark arm gauntlets. |
| ian | 이안 | Lyanh | Wavy pale sage-green hair, brown bow, white blouse and fitted brown dress, brass medallion, eerie book. |
| eleven | 일레븐 | Eleven | Pink hair, pink-white maid-idol outfit, heart brooch, pastel checked skirt, bright pink giant hammer. |
| zahir | 자히르 | Zahir | Dark skin, dark wavy hair, gold-red ceremonial headpiece, white-pink-gold robes, floating gold blades. |
| jenny | 제니 | Jenny | Long blond hair, red sunglasses atop head, burgundy dress, white fur stole, movie-star elegance, pistol. |
| camilo | 카밀로 | Camilo | Cream-blond swept hair, teal eyes, open black pinstriped dance shirt, suspenders, twin swords. |
| karla | 칼라 | Karla | Long auburn braid, red military beret with silver rose badge, red-black cropped outfit, large crossbow. |
| cathy | 캐시 | Cathy | Brown hair with blue ribbon, blue eyes, white medical coat, blue scarf, surgical tools. |
| chloe | 클로에 | Chloe | Long ash gray hair and blunt fringe, white knit sleeveless sweater, brown skirt, flower belt, doll strings. |
| chiara | 키아라 | Chiara | Long white hair with pale pink shading, pink eyes, black gothic puff-sleeve dress, white feather trim and violet gem. |
| tazia | 타지아 | Tazia | Crimson side ponytail, gold eyes, black-red tailored coat, blue crystal flower accents and floating glass shards. |
| theodore | 테오도르 | Theodore | Swept dark blue-black hair, cyan eyes, dark blue tactical sniper uniform, large rifle. |
| felix | 펠릭스 | Felix | Short sandy-blond hair, green eyes, gray-green utility hoodie, white shirt, green patches, spear. |
| priya | 프리야 | Priya | Short green hair with braid crown, large white flower, warm brown skin, white-blue sari-like outfit, guitar. |
| fiora | 피오라 | Fiora | Long brown ponytail with blue ribbons, blue-white fencing uniform, silver gauntlet and rapier. |
| piolo | 피올로 | Piolo | Messy brown hair, dark green lined sleeveless hooded vest, bare torso, white waist band, nunchaku. |
| hart | 하트 | Hart | Blond bob, black beret, bright pink rock jacket, patterned leggings, red electric guitar. |
| haze | 헤이즈 | Haze | Straight gray hair, black headset, charcoal business suit, white shirt and red tie, huge firearm case. |
| debi_marlene | 데비&마를렌 | DebiMarlene | Two sisters together: black short hair, baseball caps, black cropped outfits; blue jacket accents for Debi and red for Marlene. |
| arda | 아르다 | Arda | Fluffy golden hair, beige expedition shirt, green checked scarf, green backpack straps, relics and compass. |
| abigail | 아비게일 | Abigail | Long black-blue hair, green eyes, silver hairpin, fitted blue-white cape uniform, large blue battle axe. |
| alonso | 알론소 | Alonso | Huge spiky orange mane, black mask with yellow eyes, gray-black strapped coat, amber mechanical chest accents. |
| leni | 레니 | Leni | Light brown side bunches, black pointed bunny hat with yellow flowers, black-pink street outfit, pastel carrot bazooka. |
| tsubame | 츠바메 | Tsubame | Dark purple hair in high knot with long strands, aqua cord, black cropped outfit, purple jacket, throwing darts. |
| kenneth | 케네스 | Kenneth | Gray-white hair with red earpiece, black-red hooded jacket, strapped gray combat trousers, large axe. |
| katja | 카티야 | Katja | Gray hair with long side ponytail, teal eyes, round glasses, dark gray-cyan tech sniper outfit, oversized rifle. |
| darko | 다르코 | Darko | Long swept green-gray hair, side partly shaved, dark blue open jacket, tattoos, chain pendant, dark baseball bat. |
| lenore | 르노어 | Lenore | Blue hair under blue hood, white-blue bard outfit, silver armor shoulder, black chest straps, chains and guitar. |
| garnet | 가넷 | Garnet | Shaggy silver-gray hair, red eyes, black strappy crop outfit, dark gauntlets, white accents, spiked metal bat. |
| yumin | 유민 | YuMin | Long black hair with side fringe, navy-white formal robe, violet neck ribbon, fan and red waist sash. |
| hisui | 히스이 | Hisui | Black-teal twin ponytails with red bands, sharp grin, white-red cropped uniform, red-black neck ribbon, red-white katana. |
| justyna | 유스티나 | Justyna | Long bright red ponytail, visor goggles, white-gray mechanical coat with red trim, silver mechanical crossbow. |
| istvan | 이슈트반 | Istvan | Long black hair with red-purple ends, stern face, long white military coat, black shirt, pink badge strip, long spear. |
| xuelin | 슈린 | Xuelin | Long dark teal hair with high loops, white-teal martial outfit, silver belt, flowing turquoise ribbons and energy rapier. |
| henry | 헨리 | Henry | Gray-lilac hair with white streak, purple eyes, brown-pink Victorian coat, burgundy tie, gold pocket watch and clock-hand throwing blades. |
| blair | 블레어 | Blair | Long cream-blond hair, black sleeveless high-neck crop outfit, silver necklace and chains, black coat, dual blades. |
| mirka | 미르카 | Mirka | Long bright pink hair, black baseball cap, forest green bomber outfit, black strapped gear and silver plates. |
| fenrir | 펜리르 | Fenrir | Olive-brown tousled hair, cyan eyes, black belted racing outfit, orange cable and motorcycle accents. |
| coraline | 코렐라인 | Coraline | Half pale white and half dark hair, pink-violet eyes, white-black magical outfit with pink gems, black-pink angular wings. |
| bihyung | 비형 | Bihyung | Black hair with bright teal strands and very long teal tail, round red glasses, white-black-red hooded street outfit, giant mallet. |
| craver | 크레이버 | Craver | Messy vivid red hair, red circular glasses, black-red sharply angled coat, gray gloves, black-red revolver and red energy effects. |
| lucia | 루치아 | Lucia | Pale gray twin buns and short strands, cyan eyes, white-purple ornate dress, purple ribbons, ornate long-barrel musket. |
| ceres | 세레스 | Seres | Silver bob, white lace headband, white-black armored maid outfit, black gauntlets, huge gold-black blade with pink crystal. |
