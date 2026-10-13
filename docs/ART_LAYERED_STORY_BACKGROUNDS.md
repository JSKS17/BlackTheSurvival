# 레이어 컷신용 독립 배경 아트

컷신의 배경·캐릭터 스프라이트·말풍선·텍스트를 독립적으로 렌더링하기 위한 인물 없는 배경 6장입니다. built-in `image_gen`으로 각각 독립 호출해 제작했으며 CLI/API fallback은 사용하지 않았습니다. 기존 인물이 포함된 12개의 Story PNG는 교체하거나 삭제하지 않았습니다.

## 참조와 제작 규칙

- 직접 확인한 기존 `Assets/Resources/Lumia/Battle.png`, `Lobby.png`, `Story/prologue_1.png`의 환경·색·픽셀 군집을 참조했습니다. 참조 속 니아나 탱크 속 실루엣 등은 출력에 포함하지 않도록 명시했습니다.
- 처음 만든 인물 없는 `lab_night.png`를 나머지 세 후반 배경의 추가 스타일 참조로 사용했습니다.
- 큼직한 픽셀 군집·계단형 경계·제한된 남색/시안/자홍 팔레트를 유지했습니다. 모든 인물·사람 실루엣·얼굴·화면 속 얼굴·동물·인형·캐릭터 투영·말풍선·내레이션·사용자 UI·서명·워터마크를 제외했습니다.
- 모니터는 꺼진 검정 유리와 추상 반사로, 실험 탱크는 빈 액체와 거품만으로 표현했습니다.
- 중앙·왼쪽 중단과 전경 바닥을 비워 별도 캐릭터 레이어를 배치할 수 있게 했습니다. 상단 내레이션과 오른쪽 아래 대사는 Unity에서 별도로 표시합니다.

## 통합 배치 참고

- `lab_night`: 왼쪽 뒤에 빈 PC/의자가 있고, 중앙의 넓은 타일 바닥은 니아·하나 배치 공간입니다.
- `vf_island`, `island_restored`: 중앙·왼쪽 전경 테라스가 넓어 하나와 니아를 별도 배치할 수 있습니다.
- `vf_arena`: 왼쪽·중앙에 낮은 빈 플랫폼 3개가 있고 오른쪽 뒤에 빈 포털이 있습니다. 분신 니아를 배경에 굽지 않았습니다.
- `bedroom_night`: 왼쪽에 빈 침대가 있으며 원본 기준 대략 x=180~900, y=430~630에 있습니다. 베개는 왼쪽, 보이는 매트리스는 x=300~850, y=460~560 정도입니다. 잠자거나 깨어나는 니아는 이 영역 위에 별도 캐릭터 레이어로 배치합니다. 배경의 이불에는 사람이 누운 형태가 없습니다.
- `endless_portal`: 포털은 중앙 오른쪽, 왼쪽 전경은 캐릭터 자리이며 포털 안에도 인물을 넣지 않았습니다.

## 파일과 검증

| 리소스 | 크기 | 용도 |
|---|---|---|
| [lab_night.png](../Assets/Resources/Lumia/Story/Backgrounds/lab_night.png) | 1672×941 | 빈 연구소·꺼진 PC·하나 실험 공간 |
| [vf_island.png](../Assets/Resources/Lumia/Story/Backgrounds/vf_island.png) | 1672×941 | 게임 세계로 변형된 루미아 섬 |
| [island_restored.png](../Assets/Resources/Lumia/Story/Backgrounds/island_restored.png) | 1672×941 | 복구된 섬과 밝은 탈출 후 공간 |
| [vf_arena.png](../Assets/Resources/Lumia/Story/Backgrounds/vf_arena.png) | 1672×941 | 니아 재등장·분신 허세용 빈 VF 무대 |
| [bedroom_night.png](../Assets/Resources/Lumia/Story/Backgrounds/bedroom_night.png) | 1672×941 | 꿈에서 깨어나는 장면용 빈 침실 |
| [endless_portal.png](../Assets/Resources/Lumia/Story/Backgrounds/endless_portal.png) | 1672×941 | 무한 도전으로 이어지는 빈 연구소 포털 |

6장 모두 불투명 PNG이며 약 16:9입니다. 선택한 built-in 생성 PNG를 바이트 그대로 복사했습니다. 별도 이미지 리샘플링·픽셀 편집·합성 후처리는 하지 않았습니다. 각 PNG와 Backgrounds 폴더에 독립 GUID 메타를 만들었고 Point 필터, 밉맵 끄기, 무압축, NPOT 리사이즈 없음, 최대 4096을 적용했습니다.

직접 시각 검토로 인물·실루엣·얼굴·동물·대사·UI 오버레이가 없는지 확인했습니다. 파일 수·불투명도·원본 바이트 동일성·메타 규칙·6개 GUID 중복 없음 및 기존 Story 12PNG의 SHA-256 보존도 확인했습니다. 실제 캐릭터·말풍선과의 Unity 합성 화면 검증은 별도 네이티브 검증에서 진행합니다.

기계 검증과 참조 SHA-256은 [art-layered-story-backgrounds.json](art-layered-story-backgrounds.json)에 기록합니다.

## 정확한 프롬프트와 선정 원본

모든 호출은 built-in `image_gen`, `transparent_background=false`입니다. 아래 원본 파일명은 `$CODEX_HOME/generated_images/...`에 저장된 출력 중 선정한 파일이며 해당 바이트를 리소스로 사용합니다.

### lab_night

**최종 파일:** `Assets/Resources/Lumia/Story/Backgrounds/lab_night.png`  
**생성 원본:** `exec-e3d06dee-ed27-43f3-b00f-99a46239dc65.png`  
**크기:** 1672×941  
**SHA-256:** `8faba071901b5d928b55f1e07334afa2274314ceb8e7f93336d656947b5587e4`

```text
Use case: illustration-story. Asset type: environment-only background layer for a 2D pixel-art game cutscene. Edit/recompose the environment from the supplied game art references into one entirely UNPOPULATED wide 16:9 background, around 1672x941. Reference 1 is the game's Battle background and controls the coarse crisp pixel geometry, limited colors and blocky scenery. Reference 2 is the laboratory atmosphere; reference 3 is a previous scene only for environmental continuity. Every human and character present in any reference must be removed and must NOT be copied. IMPORTANT OUTPUT CONTAINS ZERO PEOPLE, characters, human silhouettes, figures in glass tanks, faces, faces on screens, dolls, animal toys, animals, character statues, hologram figures, portraits, reflection people, cartoon symbols of faces or bunny logos. No words, letters, digits, subtitles, speech bubbles, dialogue panels, user interface, watermark or signature. All screens are blank dark glass or simple soft cyan reflections, no data/interface graphics. Premium retro RPG pixel-art background: chunky visible square pixels, hard stair-step edges, clean large geometric clusters, limited dark navy/teal/magenta palette, slight blocky dithering, no smooth painted gradients, no photorealism, no vector outlines, no blur, no 3D. This is a background-only layer: separate small chibi sprites will be rendered in front by Unity. Preserve a broad unobstructed staging area in the center-left and middle foreground (approximately x=12..65%, y=35..85%), with readable ground or floor for separate characters. Keep the top 15 percent and the lower-right 35 percent visually quiet enough for separate text overlays, but do not draw any overlays. Keep foreground props outside the main character staging area. Full-bleed opaque environment; no transparent cutouts, no panels. Do not reproduce the prior full-scene character artwork. Specific scene: An empty Aglaia laboratory at night, dark navy structural walls, a few large cyan glass experiment cylinders containing ONLY CLEAR FLUID and bubbles with NOTHING alive inside, magenta door lighting, a black inactive PC monitor and tidy keyboard at a workstation near the back-left, laboratory bench and vials in the background, an unoccupied plain black-and-magenta gaming chair near the back-left workstation with no logos. A broad reflective tiled floor fills the center-left and foreground, ready for separately drawn Nia and researcher Hana. Wide side-on story staging, cool night lighting, empty windows overlook the distant island.
```

### vf_island

**최종 파일:** `Assets/Resources/Lumia/Story/Backgrounds/vf_island.png`  
**생성 원본:** `exec-e6260853-d537-44e2-a4c7-31f02d1263ad.png`  
**크기:** 1672×941  
**SHA-256:** `817f6c1f6fcf048d53c7653750dd4231906c93c92c7e1d0118a2cde3bdcfbb8e`

```text
Use case: illustration-story. Asset type: environment-only background layer for a 2D pixel-art game cutscene. Edit/recompose the environment from the supplied game art references into one entirely UNPOPULATED wide 16:9 background, around 1672x941. Reference 1 is the game's Battle background and controls the coarse crisp pixel geometry, limited colors and blocky scenery. Reference 2 is the laboratory atmosphere; reference 3 is a previous scene only for environmental continuity. Every human and character present in any reference must be removed and must NOT be copied. IMPORTANT OUTPUT CONTAINS ZERO PEOPLE, characters, human silhouettes, figures in glass tanks, faces, faces on screens, dolls, animal toys, animals, character statues, hologram figures, portraits, reflection people, cartoon symbols of faces or bunny logos. No words, letters, digits, subtitles, speech bubbles, dialogue panels, user interface, watermark or signature. All screens are blank dark glass or simple soft cyan reflections, no data/interface graphics. Premium retro RPG pixel-art background: chunky visible square pixels, hard stair-step edges, clean large geometric clusters, limited dark navy/teal/magenta palette, slight blocky dithering, no smooth painted gradients, no photorealism, no vector outlines, no blur, no 3D. This is a background-only layer: separate small chibi sprites will be rendered in front by Unity. Preserve a broad unobstructed staging area in the center-left and middle foreground (approximately x=12..65%, y=35..85%), with readable ground or floor for separate characters. Keep the top 15 percent and the lower-right 35 percent visually quiet enough for separate text overlays, but do not draw any overlays. Keep foreground props outside the main character staging area. Full-bleed opaque environment; no transparent cutouts, no panels. Do not reproduce the prior full-scene character artwork. Specific scene: An entirely empty Lumia Island that is transforming into a deck-building game world. A foreground stone terrace and broad empty path in the center-left are stable and clear for separate chibi characters. Beyond it, familiar research buildings and tree-covered island blocks split into floating geometric islands, connected by blank magenta-bordered rectangular stone/card platforms with absolutely no symbols or writing. A great violet-magenta VF vortex rings the distant sky over Aglaia lab. Scenic sea and dark blue sky establish the island. No actors or character silhouettes anywhere, even in the vortex.
```

### island_restored

**최종 파일:** `Assets/Resources/Lumia/Story/Backgrounds/island_restored.png`  
**생성 원본:** `exec-b1b97b93-c340-4350-942d-7ed3103b6aea.png`  
**크기:** 1672×941  
**SHA-256:** `1ffebcae728215a97a75fee6d6318a7ae5c96383f7774ce1c13c85838d45d209`

```text
Use case: illustration-story. Asset type: environment-only background layer for a 2D pixel-art game cutscene. Edit/recompose the environment from the supplied game art references into one entirely UNPOPULATED wide 16:9 background, around 1672x941. Reference 1 is the game's Battle background and controls the coarse crisp pixel geometry, limited colors and blocky scenery. Reference 2 is the laboratory atmosphere; reference 3 is a previous scene only for environmental continuity. Every human and character present in any reference must be removed and must NOT be copied. IMPORTANT OUTPUT CONTAINS ZERO PEOPLE, characters, human silhouettes, figures in glass tanks, faces, faces on screens, dolls, animal toys, animals, character statues, hologram figures, portraits, reflection people, cartoon symbols of faces or bunny logos. No words, letters, digits, subtitles, speech bubbles, dialogue panels, user interface, watermark or signature. All screens are blank dark glass or simple soft cyan reflections, no data/interface graphics. Premium retro RPG pixel-art background: chunky visible square pixels, hard stair-step edges, clean large geometric clusters, limited dark navy/teal/magenta palette, slight blocky dithering, no smooth painted gradients, no photorealism, no vector outlines, no blur, no 3D. This is a background-only layer: separate small chibi sprites will be rendered in front by Unity. Preserve a broad unobstructed staging area in the center-left and middle foreground (approximately x=12..65%, y=35..85%), with readable ground or floor for separate characters. Keep the top 15 percent and the lower-right 35 percent visually quiet enough for separate text overlays, but do not draw any overlays. Keep foreground props outside the main character staging area. Full-bleed opaque environment; no transparent cutouts, no panels. Do not reproduce the prior full-scene character artwork. Specific scene: The fully restored and entirely empty Lumia Island outside Aglaia laboratory after an apparent escape. Calm warm morning light over blue sea, normal intact research buildings, natural green trees, a quiet island courtyard and steps, a distant lab tower. There are NO floating cards, portals, broken buildings or VF explosions. The center-left and foreground form a broad empty paved terrace on which separate Hana and Nia sprites can stand. Colorful but restrained chunky pixel clusters; a few pale pink blossoms are environmental only. The lower right remains a quiet empty paving area for later dialogue.
```

### vf_arena

**최종 파일:** `Assets/Resources/Lumia/Story/Backgrounds/vf_arena.png`  
**생성 원본:** `exec-26b9b450-4dff-404c-b307-967c76327cd9.png`  
**크기:** 1672×941  
**SHA-256:** `9adda8b69297be339fdf1b1a6907ef38c33b9b6e491d66c94de51c0ad4106868`

```text
Use case: illustration-story. Asset type: environment-only background layer for a 2D pixel-art game cutscene. Edit/recompose the environment from the supplied game art references into one entirely UNPOPULATED wide 16:9 background, around 1672x941. Reference 1 is the game's Battle background and controls the coarse crisp pixel geometry, limited colors and blocky scenery. Reference 2 is the laboratory atmosphere; reference 3 is a previous scene only for environmental continuity. Every human and character present in any reference must be removed and must NOT be copied. IMPORTANT OUTPUT CONTAINS ZERO PEOPLE, characters, human silhouettes, figures in glass tanks, faces, faces on screens, dolls, animal toys, animals, character statues, hologram figures, portraits, reflection people, cartoon symbols of faces or bunny logos. No words, letters, digits, subtitles, speech bubbles, dialogue panels, user interface, watermark or signature. All screens are blank dark glass or simple soft cyan reflections, no data/interface graphics. Premium retro RPG pixel-art background: chunky visible square pixels, hard stair-step edges, clean large geometric clusters, limited dark navy/teal/magenta palette, slight blocky dithering, no smooth painted gradients, no photorealism, no vector outlines, no blur, no 3D. This is a background-only layer: separate small chibi sprites will be rendered in front by Unity. Preserve a broad unobstructed staging area in the center-left and middle foreground (approximately x=12..65%, y=35..85%), with readable ground or floor for separate characters. Keep the top 15 percent and the lower-right 35 percent visually quiet enough for separate text overlays, but do not draw any overlays. Keep foreground props outside the main character staging area. Full-bleed opaque environment; no transparent cutouts, no panels. Do not reproduce the prior full-scene character artwork. Specific scene: An entirely empty theatrical VF arena used when Nia reappears and boasts about her duplicates. This is a real environment layer, with no Nia or duplicates in it. A wide dark navy stone stage in the foreground, three simple low magenta-lit empty platforms across the middle-left, a large empty glowing magenta rectangular portal farther back to the right, and distant Aglaia research buildings over the ocean seen through violet haze. Geometric pixel blocks and magenta rim lights suggest a mischievous arcade challenge. No character outlines, faces, projections, logos or writing in the portal or on platforms. The foreground character space is open and clear. Reference 4 is the approved completely empty laboratory layer: match this clean environment-only pixel style and wide aspect, with absolutely no characters or faces anywhere.
```

### bedroom_night

**최종 파일:** `Assets/Resources/Lumia/Story/Backgrounds/bedroom_night.png`  
**생성 원본:** `exec-25dc5f28-c5d0-4bf3-b265-f7f9dcae017f.png`  
**크기:** 1672×941  
**SHA-256:** `30f90354dcbe7fe2159be6f944c1885b980966d7713e2eb5543964d8d7d147a3`

```text
Use case: illustration-story. Asset type: environment-only background layer for a 2D pixel-art game cutscene. Edit/recompose the environment from the supplied game art references into one entirely UNPOPULATED wide 16:9 background, around 1672x941. Reference 1 is the game's Battle background and controls the coarse crisp pixel geometry, limited colors and blocky scenery. Reference 2 is the laboratory atmosphere; reference 3 is a previous scene only for environmental continuity. Every human and character present in any reference must be removed and must NOT be copied. IMPORTANT OUTPUT CONTAINS ZERO PEOPLE, characters, human silhouettes, figures in glass tanks, faces, faces on screens, dolls, animal toys, animals, character statues, hologram figures, portraits, reflection people, cartoon symbols of faces or bunny logos. No words, letters, digits, subtitles, speech bubbles, dialogue panels, user interface, watermark or signature. All screens are blank dark glass or simple soft cyan reflections, no data/interface graphics. Premium retro RPG pixel-art background: chunky visible square pixels, hard stair-step edges, clean large geometric clusters, limited dark navy/teal/magenta palette, slight blocky dithering, no smooth painted gradients, no photorealism, no vector outlines, no blur, no 3D. This is a background-only layer: separate small chibi sprites will be rendered in front by Unity. Preserve a broad unobstructed staging area in the center-left and middle foreground (approximately x=12..65%, y=35..85%), with readable ground or floor for separate characters. Keep the top 15 percent and the lower-right 35 percent visually quiet enough for separate text overlays, but do not draw any overlays. Keep foreground props outside the main character staging area. Full-bleed opaque environment; no transparent cutouts, no panels. Do not reproduce the prior full-scene character artwork. Specific scene: An entirely empty modest Aglaia dormitory bedroom at night, with a plain low bed at the left-middle of the composition, headboard and pillow to the left, dark violet blanket lying FLAT with absolutely no person-shaped bulge. The mattress surface is clearly visible around x=18..51%, y=58..72%, so a separate sleeping/waking Nia sprite can later be drawn on top of it. Empty cyan moonlit window behind the bed shows the sea and distant lab tower. A small unused desk with a black powered-off monitor and plain closed cabinet sit in the right background. No plush toys, posters, portraits, characters, slippers shaped like faces, or text. Quiet cool-blue night palette with subtle magenta ambient lights. Keep center and lower-right uncluttered for other sprite and dialogue layers. Reference 4 is the approved completely empty laboratory layer: match this clean environment-only pixel style and wide aspect, with absolutely no characters or faces anywhere.
```

### endless_portal

**최종 파일:** `Assets/Resources/Lumia/Story/Backgrounds/endless_portal.png`  
**생성 원본:** `exec-e7c87dc0-e746-4f7e-9f03-36ead89b4171.png`  
**크기:** 1672×941  
**SHA-256:** `d12ff9baf4df73cd3b917df4f01125a7d0f9ac456765d1f46157660a482a8267`

```text
Use case: illustration-story. Asset type: environment-only background layer for a 2D pixel-art game cutscene. Edit/recompose the environment from the supplied game art references into one entirely UNPOPULATED wide 16:9 background, around 1672x941. Reference 1 is the game's Battle background and controls the coarse crisp pixel geometry, limited colors and blocky scenery. Reference 2 is the laboratory atmosphere; reference 3 is a previous scene only for environmental continuity. Every human and character present in any reference must be removed and must NOT be copied. IMPORTANT OUTPUT CONTAINS ZERO PEOPLE, characters, human silhouettes, figures in glass tanks, faces, faces on screens, dolls, animal toys, animals, character statues, hologram figures, portraits, reflection people, cartoon symbols of faces or bunny logos. No words, letters, digits, subtitles, speech bubbles, dialogue panels, user interface, watermark or signature. All screens are blank dark glass or simple soft cyan reflections, no data/interface graphics. Premium retro RPG pixel-art background: chunky visible square pixels, hard stair-step edges, clean large geometric clusters, limited dark navy/teal/magenta palette, slight blocky dithering, no smooth painted gradients, no photorealism, no vector outlines, no blur, no 3D. This is a background-only layer: separate small chibi sprites will be rendered in front by Unity. Preserve a broad unobstructed staging area in the center-left and middle foreground (approximately x=12..65%, y=35..85%), with readable ground or floor for separate characters. Keep the top 15 percent and the lower-right 35 percent visually quiet enough for separate text overlays, but do not draw any overlays. Keep foreground props outside the main character staging area. Full-bleed opaque environment; no transparent cutouts, no panels. Do not reproduce the prior full-scene character artwork. Specific scene: An entirely empty Aglaia laboratory with a grand rectangular magenta-and-violet VF portal on the center-right. Through the portal an endless succession of blank card-shaped stone path segments and floating Lumia Island landscapes winds into the far horizon, suggesting repeated adventure cycles. No infinity glyph, numbers, interface, characters or figures. The center-left foreground is a wide clear tiled lab floor ready for separate Nia inviting the player and Hana approaching. Glass tanks at the far left contain only clear cyan fluid with no silhouette. Plain lab walls, softly glowing magenta edges, distant island buildings and ocean depth; no foreground bench blocks the staging floor. Reference 4 is the approved completely empty laboratory layer: match this clean environment-only pixel style and wide aspect, with absolutely no characters or faces anywhere.
```
