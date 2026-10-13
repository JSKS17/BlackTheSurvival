# 컷신 전용 캐릭터 도트 스프라이트

니아 8종과 하나 4종을 기존 전투 스프라이트에서 각각 독립적으로 편집했습니다. 프롤로그·노말·하드 컷신의 배경, 캐릭터, 내레이션, 말풍선은 별도 레이어로 합성합니다.

## 입력과 제작 방식

- 도구: **내장 `image_gen.imagegen` 편집**, 각 자산마다 별도 호출, 모든 호출에 `transparent_background=true`를 지정했습니다. CLI/API 대체 경로는 사용하지 않았습니다.
- 실제 `PixelArt.Portrait`의 우선 자산인 `Assets/Resources/Lumia/PortraitsReference/nia.png`와 `hana.png`를 먼저 직접 확인한 뒤 편집 입력으로 사용했습니다. 두 원본은 160×160이며 수정하지 않았습니다.
- 원본의 큰 머리와 짧은 팔다리, 사각 픽셀 덩어리, 진한 외곽선, 색상, 헤어·의상 정체성을 유지하고 컷신에 맞게 표정과 팔·몸 자세를 바꿨습니다. 원본 전투 이미지를 그대로 배치하는 방식이 아닙니다.
- 생성 결과는 모두 **1254×1254 RGBA 투명 PNG**입니다. 최종 프로젝트 파일은 내장 도구 결과를 **바이트 그대로 복사**했습니다. 파일에 리샘플링·크롭·픽셀 채색·색 교체·알파 제거를 적용하지 않았습니다.
- 화면의 도트 크기는 Unity에서 80×80 논리 스프라이트의 Point 필터 RenderTexture로 표시합니다. 이는 런타임 표시 과정이며 디스크의 생성 원본 바이트를 바꾸지 않습니다.
- 새 텍스처 메타데이터는 Point, 밉맵 없음, 기본·Standalone 무압축 설정입니다. `Characters.meta`도 별도로 생성했습니다.
- 내레이션·말풍선·UI 문구는 이미지에 넣지 않았습니다. 니아의 VF 포즈 두 종만 손 가까이에 작은 분홍 픽셀 입자를 포함합니다.

## 자산과 표정

| ID | 컷신 표정·자세 | 최종 파일 |
| --- | --- | --- |
| nia_frustrated | PC 게임을 못 해 부루퉁한 표정·턱을 괸 손 | [nia_frustrated.png](../Assets/Resources/Lumia/Story/Characters/nia_frustrated.png) |
| nia_overload | VF 폭주·두 팔을 들어 올린 자세 | [nia_overload.png](../Assets/Resources/Lumia/Story/Characters/nia_overload.png) |
| nia_defeated | 패배 후 앉아서 숨을 고르는 표정 | [nia_defeated.png](../Assets/Resources/Lumia/Story/Characters/nia_defeated.png) |
| nia_taunt | 끝이 아니라며 한 손을 든 도발 | [nia_taunt.png](../Assets/Resources/Lumia/Story/Characters/nia_taunt.png) |
| nia_cocky | 팔짱·최약체 농담의 허세 | [nia_cocky.png](../Assets/Resources/Lumia/Story/Characters/nia_cocky.png) |
| nia_wake | 꿈에서 깬 졸리고 짜증 난 표정 | [nia_wake.png](../Assets/Resources/Lumia/Story/Characters/nia_wake.png) |
| nia_cast | 분홍 VF 입자를 손에 띄운 시전 | [nia_cast.png](../Assets/Resources/Lumia/Story/Characters/nia_cast.png) |
| nia_challenge | 게임패드·도전 권유 | [nia_challenge.png](../Assets/Resources/Lumia/Story/Characters/nia_challenge.png) |
| hana_work | 클립보드와 펜을 든 실험 자세 | [hana_work.png](../Assets/Resources/Lumia/Story/Characters/hana_work.png) |
| hana_startled | 눈을 크게 뜬 놀람·작은 열린 입 | [hana_startled.png](../Assets/Resources/Lumia/Story/Characters/hana_startled.png) |
| hana_determined | 주먹을 쥔 결의·굳은 눈썹 | [hana_determined.png](../Assets/Resources/Lumia/Story/Characters/hana_determined.png) |
| hana_relieved | 안도·편안한 눈과 웃는 입 | [hana_relieved.png](../Assets/Resources/Lumia/Story/Characters/hana_relieved.png) |

## 투명도·원본 보존 검증

12종 모두 실제 알파 채널에 완전 투명 픽셀과 완전 불투명 픽셀이 존재합니다. 생성 원본 SHA-256과 프로젝트 최종 PNG SHA-256이 일치하고, 입력 전투 스프라이트의 현재 SHA-256도 제작 기록과 일치합니다.

생성 도구가 남긴 일부 낮은 알파 값도 그대로 보존했습니다. 전체 알파 바운즈와 16·64·128 이상 바운즈를 분석용으로 기록했으며, 이 임계값으로 PNG 픽셀을 지우거나 수정하지 않았습니다. 모든 개별 결과를 직접 확인하여 눈·입 구분, 캐릭터 정체성, 자세 및 투명 배경을 검토했습니다.

[전체 매니페스트](art-layered-story/characters/manifest.json)는 정확한 호출 프롬프트, 절대 입력·생성 경로, 프로젝트 출력 경로, 입력·생성·출력 SHA-256, 크기, 알파 검증 수치 및 시각 검토를 포함합니다.

| 자산 | 입력 SHA-256 | 최종 PNG SHA-256 |
| --- | --- | --- |
| nia_frustrated | `7709789d189894fc95a46ba17643ba7a0727d857fdf6142a135d00ca95a24d5c` | `27924ac37ea8c73729b48545653fd386b84e99b756ccce785d99f39437da1887` |
| nia_overload | `7709789d189894fc95a46ba17643ba7a0727d857fdf6142a135d00ca95a24d5c` | `d5711fb61bce924b4105a0dde54fd3ab8e5c3c7904e64b59e524a2efca79768d` |
| nia_defeated | `7709789d189894fc95a46ba17643ba7a0727d857fdf6142a135d00ca95a24d5c` | `b0439b4d8af9114f9b87cb4c84abf1032b9606cbd712fc1fcfe20ff00012aba1` |
| nia_taunt | `7709789d189894fc95a46ba17643ba7a0727d857fdf6142a135d00ca95a24d5c` | `4f47cead9ae1c67b7c31e100b9c68734a6995fbcff41b572e9fea2509c70520b` |
| nia_cocky | `7709789d189894fc95a46ba17643ba7a0727d857fdf6142a135d00ca95a24d5c` | `9bf7c363b9f7e60424dac1985802d1cc8fe76ffb08990121f7145cc81154fefd` |
| nia_wake | `7709789d189894fc95a46ba17643ba7a0727d857fdf6142a135d00ca95a24d5c` | `7b61b15dca50b5e09a6713703b4ba61b48d8330d5cd586d081096342f34764b3` |
| nia_cast | `7709789d189894fc95a46ba17643ba7a0727d857fdf6142a135d00ca95a24d5c` | `42c91bca9be53772b57e515a941a3e155414e1535785fa23914fd796f16f9a40` |
| nia_challenge | `7709789d189894fc95a46ba17643ba7a0727d857fdf6142a135d00ca95a24d5c` | `ceaf7cd5a0fc140b74681d70e76931db7386fe5cbb5499d3e3b22e16a8668b93` |
| hana_work | `b481c31267ae4f86616c471eb18328d13f4d79c1d2ee18e40a7b787ebca88268` | `411c7dd02ef1c89e78c51e34bb3fdb9bdf8c8fedcb0f36b9fb6fe73052b62633` |
| hana_startled | `b481c31267ae4f86616c471eb18328d13f4d79c1d2ee18e40a7b787ebca88268` | `620e537becc3a6ac5b24a34ca0cf3a1a2c1f438d35856d842f13cd73dfc7df77` |
| hana_determined | `b481c31267ae4f86616c471eb18328d13f4d79c1d2ee18e40a7b787ebca88268` | `3a269af33ca497d18f117553f84487e80399293ee7f1efba2b52237f5b7badc2` |
| hana_relieved | `b481c31267ae4f86616c471eb18328d13f4d79c1d2ee18e40a7b787ebca88268` | `f1cfe65a573f009dd92cd38d38bd0908336d497f2d6bb63e1667dea195635ab1` |

## 정확한 프롬프트와 경로

### nia_frustrated

- 입력: `C:/Users/azxar/BlackTheSurvival/Assets/Resources/Lumia/PortraitsReference/nia.png`
- 내장 도구 생성 원본: `C:/Users/azxar/.codex/generated_images/01a133f4-c725-73f2-8ad4-5d15dad4c27b/exec-482581e0-1b4b-4271-8f75-06f8e95838e5.png`
- 최종 파일: `Assets/Resources/Lumia/Story/Characters/nia_frustrated.png`
- 기록: [nia_frustrated.json](art-layered-story/characters/nia_frustrated.json)
- 생성·최종 크기: 1254×1254, RGBA, 투명 배경

```text
Use case: identity-preserve. Asset type: one transparent pixel-art character sprite for a layered game cutscene. Edit target: the attached ORIGINAL 160x160 Nia game sprite. Make exactly ONE variant named nia_frustrated: she cannot play PC games and sulks, sitting low or standing with shoulders drooped, one tiny hand resting under her chin, annoyed half-lidded eyes and a clearly visible small pout. Preserve this EXACT character's original sprite design: violet-pink curled short hair with dark pink headphones, black/pink cropped jacket and striped pink sleeves, tiny dark shorts/skirt, black/pink boots. Keep the reference's very squat 2-head-tall chibi proportions, oversized round head, extremely short arms/legs, side-view three-quarter facing right. Keep its low-resolution pixel count, blocky hard-edged pixel clusters, dark 1-logical-pixel outline, limited palette and tiny crisp facial features. Only change expression and arm/leg pose, not outfit, face identity, hair shape or body proportions. Eyes and mouth must be distinct and separated, not obscured by hair. True transparent background with intact alpha; no ground, no shadow plate, no scene, no UI, no lettering or speech bubble. One whole character fully inside canvas with margin. This is a sprite edit, NOT a high-detail anime illustration, NOT smooth painting, NOT a large-head portrait crop. Prefer matching the source 160x160 canvas/logical 80x80 pixel grid; if generated at higher resolution, maintain obviously large uniform square pixels and identical sprite silhouette scale/proportions.
```

### nia_overload

- 입력: `C:/Users/azxar/BlackTheSurvival/Assets/Resources/Lumia/PortraitsReference/nia.png`
- 내장 도구 생성 원본: `C:/Users/azxar/.codex/generated_images/01a133f4-c725-73f2-8ad4-5d15dad4c27b/exec-4c7b40f4-5c67-4b25-8120-ac2d29900b53.png`
- 최종 파일: `Assets/Resources/Lumia/Story/Characters/nia_overload.png`
- 기록: [nia_overload.json](art-layered-story/characters/nia_overload.json)
- 생성·최종 크기: 1254×1254, RGBA, 투명 배경

```text
Use case: identity-preserve. Asset type: exactly one transparent low-resolution pixel-art cutscene character. Edit target: the ORIGINAL 160x160 Nia game sprite supplied. Preserve its exact violet-pink short curled hair, pink headphones, black/pink cropped jacket with striped pink sleeves, tiny dark shorts/skirt, black/pink boots and skin/eye palette. Preserve its squat 2-head-tall chibi silhouette, oversized round head, extremely short little arms and legs, original low pixel count, uniform square pixel clusters, dark 1-logical-pixel outline and limited palette. Only modify expression and the specified pose. Keep hair/face/outfit identity; eyes and mouth must remain clearly separated and visible. Fully inside square canvas with margins, actual transparent alpha, no background/ground/scene/UI/text/bubble, no smooth anime illustration or high-detail redraw. Prefer source160x160/80logicalgrid; if larger output retain obvious uniformly large square pixels and the exact tiny-body proportions. Variant nia_overload: Nia's VF gamer power overflows; she raises both tiny arms outward and up, with a lively wide grin and bright excited sharp eyes. A few compact magenta arcade square sparks may hover close to her hands, staying a separate-looking sparse accent rather than covering face/body. Her feet remain planted; dramatic triumphant arms but no stretched long limbs. Face and both hands clearly readable.
```

### nia_defeated

- 입력: `C:/Users/azxar/BlackTheSurvival/Assets/Resources/Lumia/PortraitsReference/nia.png`
- 내장 도구 생성 원본: `C:/Users/azxar/.codex/generated_images/01a133f4-c725-73f2-8ad4-5d15dad4c27b/exec-8425edf6-6e8d-4470-bf10-68636ca077cc.png`
- 최종 파일: `Assets/Resources/Lumia/Story/Characters/nia_defeated.png`
- 기록: [nia_defeated.json](art-layered-story/characters/nia_defeated.json)
- 생성·최종 크기: 1254×1254, RGBA, 투명 배경

```text
Use case: identity-preserve. Asset type: exactly one transparent low-resolution pixel-art cutscene character. Edit target: the ORIGINAL 160x160 Nia game sprite supplied. Preserve its exact violet-pink short curled hair, pink headphones, black/pink cropped jacket with striped pink sleeves, tiny dark shorts/skirt, black/pink boots and skin/eye palette. Preserve its squat 2-head-tall chibi silhouette, oversized round head, extremely short little arms and legs, original low pixel count, uniform square pixel clusters, dark 1-logical-pixel outline and limited palette. Only modify expression and the specified pose. Keep hair/face/outfit identity; eyes and mouth must remain clearly separated and visible. Fully inside square canvas with margins, actual transparent alpha, no background/ground/scene/UI/text/bubble, no smooth anime illustration or high-detail redraw. Prefer source160x160/80logicalgrid; if larger output retain obvious uniformly large square pixels and the exact tiny-body proportions. Variant nia_defeated: she is comically out of breath after losing a game battle, sitting on the ground with knees bent and tiny booted legs in front, shoulders slumped and both little hands resting beside her knees. Clear tired half-lidded magenta eyes and small open panting mouth, still recognizable rather than a blank face. Exhausted cartoon expression only: no injuries, no bruises, no tears, no violence. Keep the original big head and very tiny body/short limbs. Fully show both boots inside the transparent canvas.
```

### nia_taunt

- 입력: `C:/Users/azxar/BlackTheSurvival/Assets/Resources/Lumia/PortraitsReference/nia.png`
- 내장 도구 생성 원본: `C:/Users/azxar/.codex/generated_images/01a133f4-c725-73f2-8ad4-5d15dad4c27b/exec-99cc5a22-c91a-4ffb-9e0e-6c7afb8f0e7c.png`
- 최종 파일: `Assets/Resources/Lumia/Story/Characters/nia_taunt.png`
- 기록: [nia_taunt.json](art-layered-story/characters/nia_taunt.json)
- 생성·최종 크기: 1254×1254, RGBA, 투명 배경

```text
Use case: identity-preserve. Asset type: exactly one transparent low-resolution pixel-art cutscene character. Edit target: the ORIGINAL 160x160 Nia game sprite supplied. Preserve its exact violet-pink short curled hair, pink headphones, black/pink cropped jacket with striped pink sleeves, tiny dark shorts/skirt, black/pink boots and skin/eye palette. Preserve its squat 2-head-tall chibi silhouette, oversized round head, extremely short little arms and legs, original low pixel count, uniform square pixel clusters, dark 1-logical-pixel outline and limited palette. Only modify expression and the specified pose. Keep hair/face/outfit identity; eyes and mouth must remain clearly separated and visible. Fully inside square canvas with margins, actual transparent alpha, no background/ground/scene/UI/text/bubble, no smooth anime illustration or high-detail redraw. Prefer source160x160/80logicalgrid; if larger output retain obvious uniformly large square pixels and the exact tiny-body proportions. Variant nia_taunt: lively smug challenger announcing that the game is not over. Stand with one little hand raised by her shoulder with index finger lifted, the other hand on her hip. Confident sharp half-lidded magenta eyes and an unmistakable small playful open grin. Keep the same original ear/headphone and hair proportions, short curled hair silhouette and tiny body. Compact pose suitable next to a speech bubble, but draw NO bubble or text.
```

### nia_cocky

- 입력: `C:/Users/azxar/BlackTheSurvival/Assets/Resources/Lumia/PortraitsReference/nia.png`
- 내장 도구 생성 원본: `C:/Users/azxar/.codex/generated_images/01a133f4-c725-73f2-8ad4-5d15dad4c27b/exec-7958faaf-2cae-42ea-bbc9-ed0e24f57005.png`
- 최종 파일: `Assets/Resources/Lumia/Story/Characters/nia_cocky.png`
- 기록: [nia_cocky.json](art-layered-story/characters/nia_cocky.json)
- 생성·최종 크기: 1254×1254, RGBA, 투명 배경

```text
Use case: identity-preserve. Asset type: exactly one transparent low-resolution pixel-art cutscene character. Edit target: the ORIGINAL 160x160 Nia game sprite supplied. Preserve its exact violet-pink short curled hair, pink headphones, black/pink cropped jacket with striped pink sleeves, tiny dark shorts/skirt, black/pink boots and skin/eye palette. Preserve its squat 2-head-tall chibi silhouette, oversized round head, extremely short little arms and legs, original low pixel count, uniform square pixel clusters, dark 1-logical-pixel outline and limited palette. Only modify expression and the specified pose. Keep hair/face/outfit identity; eyes and mouth must remain clearly separated and visible. Fully inside square canvas with margins, actual transparent alpha, no background/ground/scene/UI/text/bubble, no smooth anime illustration or high-detail redraw. Prefer source160x160/80logicalgrid; if larger output retain obvious uniformly large square pixels and the exact tiny-body proportions. Variant nia_cocky: a smug gamer boss boasts about stronger versions of herself. Stand planted with both very short arms folded across her little jacket, chin slightly raised. Confident half-lidded magenta eyes with asymmetric sly eyebrows and a crisp small smug side-smile. Two clearly visible eyes and a separate mouth, not identical neutral eye expression. Keep reference head/ear/headphone/hair proportions exactly. No extra characters or clones; ONE full-body transparent sprite only.
```

### nia_wake

- 입력: `C:/Users/azxar/BlackTheSurvival/Assets/Resources/Lumia/PortraitsReference/nia.png`
- 내장 도구 생성 원본: `C:/Users/azxar/.codex/generated_images/01a133f4-c725-73f2-8ad4-5d15dad4c27b/exec-cb530328-3aeb-46ee-a148-d482c76cdcca.png`
- 최종 파일: `Assets/Resources/Lumia/Story/Characters/nia_wake.png`
- 기록: [nia_wake.json](art-layered-story/characters/nia_wake.json)
- 생성·최종 크기: 1254×1254, RGBA, 투명 배경

```text
Use case: identity-preserve. Asset type: exactly one transparent low-resolution pixel-art cutscene character. Edit target: the ORIGINAL 160x160 Nia game sprite supplied. Preserve its exact violet-pink short curled hair, pink headphones, black/pink cropped jacket with striped pink sleeves, tiny dark shorts/skirt, black/pink boots and skin/eye palette. Preserve its squat 2-head-tall chibi silhouette, oversized round head, extremely short little arms and legs, original low pixel count, uniform square pixel clusters, dark 1-logical-pixel outline and limited palette. Only modify expression and the specified pose. Keep hair/face/outfit identity; eyes and mouth must remain clearly separated and visible. Fully inside square canvas with margins, actual transparent alpha, no background/ground/scene/UI/text/bubble, no smooth anime illustration or high-detail redraw. Prefer source160x160/80logicalgrid; if larger output retain obvious uniformly large square pixels and the exact tiny-body proportions. Variant nia_wake: Nia has just awoken from an annoying dream. Sleepy grumpy half-open sharp magenta eyes, lowered brows and a clearly separate small open crooked irritated mouth. One tiny hand rests against the side of her headphone/cheek, NOT covering either eye; the other hangs at her side. Slightly slouched posture with knees relaxed. Keep BOTH eyes and mouth visible and the original tiny limbs/head ratio. No bed, no chair, no dream cloud, no sleep letters or other text.
```

### nia_cast

- 입력: `C:/Users/azxar/BlackTheSurvival/Assets/Resources/Lumia/PortraitsReference/nia.png`
- 내장 도구 생성 원본: `C:/Users/azxar/.codex/generated_images/01a133f4-c725-73f2-8ad4-5d15dad4c27b/exec-d8ec722b-aac2-462d-b338-f750f28da729.png`
- 최종 파일: `Assets/Resources/Lumia/Story/Characters/nia_cast.png`
- 기록: [nia_cast.json](art-layered-story/characters/nia_cast.json)
- 생성·최종 크기: 1254×1254, RGBA, 투명 배경

```text
Use case: identity-preserve. Asset type: exactly one transparent low-resolution pixel-art cutscene character. Edit target: the ORIGINAL 160x160 Nia game sprite supplied. Preserve its exact violet-pink short curled hair, pink headphones, black/pink cropped jacket with striped pink sleeves, tiny dark shorts/skirt, black/pink boots and skin/eye palette. Preserve its squat 2-head-tall chibi silhouette, oversized round head, extremely short little arms and legs, original low pixel count, uniform square pixel clusters, dark 1-logical-pixel outline and limited palette. Only modify expression and the specified pose. Keep hair/face/outfit identity; eyes and mouth must remain clearly separated and visible. Fully inside square canvas with margins, actual transparent alpha, no background/ground/scene/UI/text/bubble, no smooth anime illustration or high-detail redraw. Prefer source160x160/80logicalgrid; if larger output retain obvious uniformly large square pixels and the exact tiny-body proportions. Variant nia_cast: mischievously using her VF arcade powers again. Extend ONE short forearm forward with open tiny palm, the other hand held compactly near her waist. Sly excited half-lidded magenta eyes and a clearly visible little open smile. A very small cluster of magenta square arcade particles close above the open palm, not a background, no ring or full portal, no particles on her face. Stand three-quarter right; keep the exact source silhouette, ear/headphone/head/body ratio and original outfit.
```

### nia_challenge

- 입력: `C:/Users/azxar/BlackTheSurvival/Assets/Resources/Lumia/PortraitsReference/nia.png`
- 내장 도구 생성 원본: `C:/Users/azxar/.codex/generated_images/01a133f4-c725-73f2-8ad4-5d15dad4c27b/exec-b2937a26-4688-4acf-a81c-499e0bbfd53b.png`
- 최종 파일: `Assets/Resources/Lumia/Story/Characters/nia_challenge.png`
- 기록: [nia_challenge.json](art-layered-story/characters/nia_challenge.json)
- 생성·최종 크기: 1254×1254, RGBA, 투명 배경

```text
Use case: identity-preserve. Asset type: exactly one transparent low-resolution pixel-art cutscene character. Edit target: the ORIGINAL 160x160 Nia game sprite supplied. Preserve its exact violet-pink short curled hair, pink headphones, black/pink cropped jacket with striped pink sleeves, tiny dark shorts/skirt, black/pink boots and skin/eye palette. Preserve its squat 2-head-tall chibi silhouette, oversized round head, extremely short little arms and legs, original low pixel count, uniform square pixel clusters, dark 1-logical-pixel outline and limited palette. Only modify expression and the specified pose. Keep hair/face/outfit identity; eyes and mouth must remain clearly separated and visible. Fully inside square canvas with margins, actual transparent alpha, no background/ground/scene/UI/text/bubble, no smooth anime illustration or high-detail redraw. Prefer source160x160/80logicalgrid; if larger output retain obvious uniformly large square pixels and the exact tiny-body proportions. Variant nia_challenge: the gamer girl inviting the player to an endless-mode challenge. Hold a small black-and-pink game controller in one tiny hand near her waist, and extend the other short arm toward the viewer invitingly. Crisp playful confident magenta eyes with raised eyebrow and a distinctly visible friendly smug open smile. Controller stays well below her face. Keep source head, ears/headphones, curled hair silhouette, body proportions and costume exactly. No text on controller, no scene or speech bubble.
```

### hana_work

- 입력: `C:/Users/azxar/BlackTheSurvival/Assets/Resources/Lumia/PortraitsReference/hana.png`
- 내장 도구 생성 원본: `C:/Users/azxar/.codex/generated_images/01a133f4-c725-73f2-8ad4-5d15dad4c27b/exec-956ea0e4-3622-42cc-a9d1-98c06544c9a7.png`
- 최종 파일: `Assets/Resources/Lumia/Story/Characters/hana_work.png`
- 기록: [hana_work.json](art-layered-story/characters/hana_work.json)
- 생성·최종 크기: 1254×1254, RGBA, 투명 배경

```text
Use case: identity-preserve. Asset type: ONE transparent low-resolution pixel-art cutscene character. Edit target: ORIGINAL 160x160 Hana game sprite supplied. Preserve her exact warm brown short bob/side tufts, little yellow hair clips, round amber eyes, fair skin, white lab coat with gold trim over green blouse, dark shorts and small black boots. Preserve the source's squat 2-head-tall chibi silhouette, oversized round head, tiny short arms/legs, original low pixel count, square hard-edged clusters, dark 1-logical-pixel outline and limited original palette. Only change expression and specified small-arm pose; keep hair/head/ear proportions and character/costume identity. Make eye whites, amber irises and small separate mouth clear. Full body inside square canvas with margin, genuine transparent alpha, no ground/shadow plate/scene/UI/text/bubbles. No generic detailed anime, smooth painting, long limbs or mature tall body. Prefer source160x160/80logicalgrid; higher-resolution output must keep uniformly large square pixels and original tiny-body proportions. Variant hana_work: calmly conducting a routine experiment. Stand three-quarter facing right, holding a small dark clipboard near her waist with one tiny hand and a little pen in the other. Focused friendly amber eyes and a clearly visible small attentive closed smile. Clipboard must not cover face and contains no readable lettering. Original outfit and short arms unchanged.
```

### hana_startled

- 입력: `C:/Users/azxar/BlackTheSurvival/Assets/Resources/Lumia/PortraitsReference/hana.png`
- 내장 도구 생성 원본: `C:/Users/azxar/.codex/generated_images/01a133f4-c725-73f2-8ad4-5d15dad4c27b/exec-00e96ab2-c13b-4d09-96f4-3fd8bb39c811.png`
- 최종 파일: `Assets/Resources/Lumia/Story/Characters/hana_startled.png`
- 기록: [hana_startled.json](art-layered-story/characters/hana_startled.json)
- 생성·최종 크기: 1254×1254, RGBA, 투명 배경

```text
Use case: identity-preserve. Asset type: ONE transparent low-resolution pixel-art cutscene character. Edit target: ORIGINAL 160x160 Hana game sprite supplied. Preserve her exact warm brown short bob/side tufts, little yellow hair clips, round amber eyes, fair skin, white lab coat with gold trim over green blouse, dark shorts and small black boots. Preserve the source's squat 2-head-tall chibi silhouette, oversized round head, tiny short arms/legs, original low pixel count, square hard-edged clusters, dark 1-logical-pixel outline and limited original palette. Only change expression and specified small-arm pose; keep hair/head/ear proportions and character/costume identity. Make eye whites, amber irises and small separate mouth clear. Full body inside square canvas with margin, genuine transparent alpha, no ground/shadow plate/scene/UI/text/bubbles. No generic detailed anime, smooth painting, long limbs or mature tall body. Prefer source160x160/80logicalgrid; higher-resolution output must keep uniformly large square pixels and original tiny-body proportions. Variant hana_startled: startled as a VF portal interrupts her experiment. Lean her tiny body slightly backward, both little arms lifted near her chest with open hands, clutching a small clipboard in one hand but below her face. Wide clearly drawn amber eyes, raised brows, and small separate round surprised O-shaped mouth. One boot shifted back, still short compact legs and source 2-head proportions. No portal/background, no flying paper or punctuation symbols, only this full character.
```

### hana_determined

- 입력: `C:/Users/azxar/BlackTheSurvival/Assets/Resources/Lumia/PortraitsReference/hana.png`
- 내장 도구 생성 원본: `C:/Users/azxar/.codex/generated_images/01a133f4-c725-73f2-8ad4-5d15dad4c27b/exec-d6453d32-bae0-4efa-8748-6aab679cc213.png`
- 최종 파일: `Assets/Resources/Lumia/Story/Characters/hana_determined.png`
- 기록: [hana_determined.json](art-layered-story/characters/hana_determined.json)
- 생성·최종 크기: 1254×1254, RGBA, 투명 배경

```text
Use case: identity-preserve. Asset type: ONE transparent low-resolution pixel-art cutscene character. Edit target: ORIGINAL 160x160 Hana game sprite supplied. Preserve her exact warm brown short bob/side tufts, little yellow hair clips, round amber eyes, fair skin, white lab coat with gold trim over green blouse, dark shorts and small black boots. Preserve the source's squat 2-head-tall chibi silhouette, oversized round head, tiny short arms/legs, original low pixel count, square hard-edged clusters, dark 1-logical-pixel outline and limited original palette. Only change expression and specified small-arm pose; keep hair/head/ear proportions and character/costume identity. Make eye whites, amber irises and small separate mouth clear. Full body inside square canvas with margin, genuine transparent alpha, no ground/shadow plate/scene/UI/text/bubbles. No generic detailed anime, smooth painting, long limbs or mature tall body. Prefer source160x160/80logicalgrid; higher-resolution output must keep uniformly large square pixels and original tiny-body proportions. Variant hana_determined: the researcher steels herself to restore Lumia Island. Stand three-quarter right in a firm compact stance, a tiny clenched fist held near her chest and a small clipboard tucked low under the other arm. Clearly visible amber eyes with slightly determined lowered brows, and separate small resolute closed smile. Keep her friendly identity, not angry/hostile. No adventure backpack or new costume; source white/gold lab coat, green blouse and tiny legs unchanged. No environmental objects or text.
```

### hana_relieved

- 입력: `C:/Users/azxar/BlackTheSurvival/Assets/Resources/Lumia/PortraitsReference/hana.png`
- 내장 도구 생성 원본: `C:/Users/azxar/.codex/generated_images/01a133f4-c725-73f2-8ad4-5d15dad4c27b/exec-eebe9784-8985-4cb6-af37-030e3b6205f4.png`
- 최종 파일: `Assets/Resources/Lumia/Story/Characters/hana_relieved.png`
- 기록: [hana_relieved.json](art-layered-story/characters/hana_relieved.json)
- 생성·최종 크기: 1254×1254, RGBA, 투명 배경

```text
Use case: identity-preserve. Asset type: ONE transparent low-resolution pixel-art cutscene character. Edit target: ORIGINAL 160x160 Hana game sprite supplied. Preserve her exact warm brown short bob/side tufts, little yellow hair clips, round amber eyes, fair skin, white lab coat with gold trim over green blouse, dark shorts and small black boots. Preserve the source's squat 2-head-tall chibi silhouette, oversized round head, tiny short arms/legs, original low pixel count, square hard-edged clusters, dark 1-logical-pixel outline and limited original palette. Only change expression and specified small-arm pose; keep hair/head/ear proportions and character/costume identity. Make eye whites, amber irises and small separate mouth clear. Full body inside square canvas with margin, genuine transparent alpha, no ground/shadow plate/scene/UI/text/bubbles. No generic detailed anime, smooth painting, long limbs or mature tall body. Prefer source160x160/80logicalgrid; higher-resolution output must keep uniformly large square pixels and original tiny-body proportions. Variant hana_relieved: relaxed relieved after escaping the game world. One tiny hand rests gently on her upper chest, the other lowers her small clipboard beside her hip; shoulders relaxed and boots together. Soft openly visible amber eyes with clear whites and round irises, relaxed brows and a clearly separate small warm happy smile. BOTH eyes remain open rather than generic closed happy arcs. Preserve source hair/head/ear ratio, tiny body and exact original coat/blouse/shorts/boots. No celebratory symbols, background, text or speech bubble.
```
