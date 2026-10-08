# 픽셀 아트 자산

이 프로젝트의 화면은 래스터 픽셀 그림과 픽셀 글꼴로 구성된다. 하나와 실험체 91명은 큰 머리, 작은 몸, 짧은 팔다리의 치비 도트 스프라이트 92개를 사용한다. 카드 378종과 패시브 91종은 각각 다른 전용 스킬 아이콘을 가진다. 카드에는 큰 스킬 아이콘과 작은 소유 실험체 스프라이트를 함께 표시하며, 적 행동 예고·기술 배너·패시브 선택과 보유 목록에도 전용 아이콘이 나타난다.

`PixelArt`가 모든 텍스처에 `FilterMode.Point`를 적용한다. Unity 에디터의 픽셀 임포터는 밉맵과 압축을 끄고, 이미지 크기를 유지하며 알파 투명도를 보존한다. 개별 PNG는 CPU 읽기를 끄고, 런타임 슬라이스가 필요한 기존 아틀라스만 읽기 옵션을 켠다.

## 최종 저장 자산

| 경로 | 용도 |
|---|---|
| `Assets/Resources/Lumia/Lobby.png` | 아글라이아 연구실 로비. 오른쪽에 게임 의자에 앉은 니아, 왼쪽은 메뉴용 여백. |
| `Assets/Resources/Lumia/Battle.png` | 루미아 섬 연구시설 바깥의 폐허·숲 전투 배경. |
| `Assets/Resources/Lumia/Portraits/{id}.png` | 하나와 등록 실험체 91명의 투명 128×128 치비 스프라이트. 클로에·니나와 데비·마를렌은 각각 한 리소스에 함께 배치한다. |
| `Assets/Resources/Lumia/SkillIcons/{id}.png` | 카드 378종의 96×96 전용 도트 아이콘. 실험체 Q/W/E/R, 기본 공격·경계, 무기·전술 스킬을 포함한다. |
| `Assets/Resources/Lumia/PassiveIcons/{id}.png` | 실험체 91명의 96×96 전용 패시브 도트 아이콘. |
| `Assets/Resources/Lumia/CharacterAtlas.png` | 닭, 들개, 멧돼지, 늑대, 곰과 VF 드론의 기존 도트 그림 및 이전 하나·니아 그림 보관. 등록 실험체와 하나는 개별 PNG를 우선 사용한다. |
| `Assets/Resources/Lumia/SubjectAtlas.png` | 이전 실험체 9명의 도트 그림 보관. 등록 실험체는 모두 개별 치비 PNG를 우선 사용한다. |
| `Assets/Resources/Lumia/FoodAtlas.png` | 투명 음식 아이콘 16칸. 현재 음식·보조 그림 15칸과 사용하지 않는 이전 오므라이스 그림 1칸. |
| `Assets/Scripts/Lumia/PixelArt.cs` | 전용 치비·스킬·패시브 리소스 로드, 기존 아틀라스 슬라이스와 래스터 지도·구역·오브젝트 아이콘. |
| `Assets/Editor/LumiaProjectSetup.cs` | 픽셀 PNG 임포트와 빌드 전 임포트 설정 확인. |

공식 원본 그림은 재현할 외형을 확인하는 참고 자료로만 `docs/art-references/`와 `docs/art-chibi/references/`에 저장했다. 게임 리소스에는 도트로 새로 제작한 그림만 들어간다. 아글라이아의 특정 공식 내부 일러스트를 그대로 변환한 것은 아니며, 공식 세계관의 연구시설을 바탕으로 만든 배경이다. 캐릭터 의상·무기·색상과 음식의 형태·접시는 공식 자산을 참고했다. 스킬 아이콘도 원작의 주요 기호, 색상과 동작 방향을 새로운 도트 그림으로 재해석했다.

## 최신 치비·스킬 아트 기록

새 스프라이트와 모든 전용 아이콘은 **내장 `image_gen`**으로 제작했다. 원본 생성 PNG를 프로젝트에 보관한 뒤 알파 영역과 타일 경계를 분리하고 최근접 방식으로 축소했다. 얼굴·의상·스킬 그림을 코드로 다시 그리지 않는다. 최종 PNG 561개의 리소스 경로와 원본 아틀라스 좌표는 기록 파일에서 확인할 수 있다.

- [치비 제작 설명](ART_CHIBI.md): 스타일, 두 몸체 실험체 처리, 리소스 규격과 재처리 방법.
- [전체 게임 아트 명세](art-chibi/game-art-manifest.json): 92개 스프라이트, 카드 378개와 패시브 91개의 ID.
- [실제 생성·교정 프롬프트](art-chibi/prompts.json): 내장 도구 모드와 실제 사용한 프롬프트.
- [스프라이트 아틀라스 배치](art-chibi/batches.json), [아이콘 아틀라스 배치](art-chibi/icon-batches.json): 생성 원본 파일과 각 셀의 ID·좌표.
- [최종 자산 인덱스](art-chibi/processed-art-index.json): 최종 파일 경로, 원본 좌표, 알파 통계와 SHA-256.
- [아트 검증 기록](art-chibi/art-validation.json): 561개 PNG의 명세 일치, 누락·중복·경계·투명도·해시 검사 결과.

개별 생성 작업의 원본 도구 출력 경로는 [통합 작업](art-chibi/icon-generation-integration.json), [파이프라인 작업](art-chibi/icon-generation-pipeline.json), [참고 자료 작업](art-chibi/icon-generation-references.json), [전체 작업](art-chibi/icon-generation-root.json)에 보존한다.

최종 데이터는 12.0에서 삭제된 오므라이스 대신 수박을 사용한다. `Icon("watermelon")`은 빨간 과육, 검은 씨, 연한 속껍질과 초록 겉껍질의 도트 수박 조각을 32×32 래스터 캔버스에 그린다. 아틀라스의 이전 오므라이스 칸은 현재 음식 ID와 연결하지 않는다. 최종 음식 14종은 감자, 고기, 연어, 고구마, 트러플, 감자튀김, 웰던 스테이크, 수박, 후라이드 치킨, 피쉬 앤 칩스, 연어 스테이크, 꿀고구마, 트러플 파스타, 만년 스프이다.

## 참고 출처

- [공식 니아 콘셉트 아트](https://cdn.playeternalreturn.com/event/season6/roadmap/rd6/img_concept01.png): 보라색 단발/낮은 양갈래, 핑크 헤드셋, 체크 셔츠와 보라·핑크 후드, 컨트롤러.
- [공식 Dr. 하나 이벤트](https://playeternalreturn.com/posts/news/3330?hl=ko-KR): 갈색 단발과 노란 지그재그 머리핀, 초록 스웨터, 흰 연구 가운, 갈색 체크 치마.
- [공식 하나 이모티콘 안내](https://playeternalreturn.com/posts/news/3293): 하나 표정과 색상 확인.
- [공식 음식 이미지가 포함된 11.0 패치노트](https://playeternalreturn.com/posts/news/3531?hl=ko-KR): 오므라이스.
- [공식 음식 이미지가 포함된 1.43 패치노트](https://playeternalreturn.com/posts/news/2585?hl=ko-KR): 트러플, 트러플 파스타, 만년 수프.
- 주요 전투 실험체의 실제 게임 자산은 [닥지지 실험체 페이지](https://dak.gg/er/characters/Jackie/introduction)와 같은 사이트의 `cdn.dak.gg/assets/er/game-assets/10.7.0/ui/characterhalfsize/CharFull_{Character}_S000.png` 자산을 참고했다. 파일별 원본은 `docs/art-references/`에서 확인할 수 있다.

## 기존 배경·동물·음식의 생성 기록

모든 생성 그림은 **내장 ImageGen**으로 제작했고 CLI/API 키 경로는 사용하지 않았다. 투명 아틀라스는 실제 알파를 가진다. 참조 몽타주는 원본들의 외형을 한눈에 확인하기 위한 배열이며 게임에서 사용하지 않는다.

1. **로비**: “Authentic 16-bit pixel-art 16:9 AGLAIA bio-research control room. Preserve the official NiaH reference: lavender bob with curled low side bunches, pink headphones, pink/lavender hoodie vest over magenta plaid sleeves, pink shorts and pink black sneakers; seated curled up on a black-pink gaming chair on the right, bubblegum and controller. Teal monitors, containment pod, cables, metallic tiled floor, pink pixel portal. Left 50% dark quiet menu space. Limited charcoal/teal/cyan/pink/purple palette, crisp nearest-neighbour clusters around a 480×270 base grid. No text, logo, blur or antialiasing.”
2. **하나·니아·야생동물**: “Exactly eight isolated transparent 16-bit battle sprites in a 4×2 equal-cell atlas. Row one: official-reference Dr. Hana, official-reference NiaH, chicken, wild dog. Row two: boar, wolf, bear, teal-red VF drone. Preserve researcher and gamer appearance. Full body, generous padding, crisp stepped outlines and limited colours, approximately 64×96 base sprite pixels; no labels, frames, blur or smoothing.”
3. **전투 배경**: “True pixel art 16:9 deserted Lumia industrial forest edge at violet-blue dusk. Laboratory wall, overgrown fence, broken kiosk, rusty lamp, distant apartment silhouettes and cyan virtual-world streak. Empty concrete clearing for combat sprites, lower quarter dark for card UI. Charcoal blue/teal, dusty purple, cyan and amber pixels. 480×270 base-grid appearance, no people, animals, text, logo, smooth gradient or antialiasing.”
4. **음식**: “Transparent exact 4×4 inventory atlas. Row one: potato, raw meat, salmon fillet, sweet potato. Row two: truffle, french fries, well done steak, omelet rice. Row three: fried chicken, fish and chips, salmon steak, honey sweet potato. Row four: truffle pasta, everlasting soup, Hawaiian pizza, fish cutlet. Preserve supplied official omelet/truffle/pasta/soup silhouettes, plating and toppings. Approximately 48×48 base icon pixels, hard stepped outlines and warm limited colours. Equal cells, transparent padding, no text/gridlines/smoothing.”
5. **실험체 9명**: “Exact transparent 3×3 full-body pixel battle atlas following supplied official reference montage. Top: Jackie white spiky hair and red axe, Aya brown braids/glasses/olive jacket/rifle, Hyunwoo red hair/brown school uniform/fists. Middle: Yuki blue-black hair/black Japanese uniform/katana, Hyejin long braids/black-white sailor uniform/purple talisman, Sua long brown hair/green floral dress/book/hammer. Bottom: Isol orange-brown hair/olive tactical gear/red-black scarf/rifle, Nadine dark ponytail/orange cap and hood/bow, Emma turquoise twin tails/white-cyan magician dress/playing cards. Distinct official appearance, generous cell padding, 64×96 base-pixel look, no duplicated characters, no text, smoothing or blur.”

`MapBackground()`와 구역·오브젝트 UI 아이콘은 코드에서 작은 픽셀 캔버스에 그린 실제 `Texture2D` 비트맵이다. 등록된 실험체 91명과 하나는 모두 전용 치비 PNG가 있으며, 카드와 패시브도 모두 전용 아이콘 PNG를 사용한다. 개발 중 누락 파일을 위한 대체 그림은 남아 있지만, 아트 검증은 대체 그림을 완성 자산으로 인정하지 않는다.

이터널 리턴의 원본 IP, 캐릭터와 아이템의 권리는 님블뉴런에 귀속된다. 이 자산들은 비공식 팬 게임의 시각적 재해석이다.

## 카드 전투 효과 추가

378장의 카드에 `CardPresentation`이 효과 유형, 실험체별 색상과 카드 ID별 고유 패턴을 지정한다. `CombatEffects`는 정수 픽셀 격자에 참격·탄환·폭발·번개·불꽃·얼음·아케이드 블록·마법진·방어막·회복 십자·돌진 잔상·덫·중독 입자를 그린다. 외부 이미지 없이 UI와 같은 코드 도트 방식을 사용한다. 공격 반동, 피해·회피·방어·회복 숫자와 사용 기술 이름도 함께 표시한다. 상대는 엔진의 실제 카드 사용 순서에 맞춰 한 기술씩 재생한다. 음향은 카드별로 다른 주파수·노이즈·연타 펄스를 갖는 짧은 8비트 파형으로 만든다. 움직임 최소화 설정은 효과 시간을 줄이고 반동·입자 이동을 제한한다.
