# 실험체별 R 애니메이션 컬렉션

게임 등록 실험체 91명 전원의 승인된 R 애니메이션을 제공합니다. 기존 니아 1종을 유지하고 나머지 90종을 새로 제작했습니다. 로스터는 실제 게임 데이터에서 추출한 `roster.json`을 기준으로 합니다. **v1.0.0부터 플레이어와 상대의 직접 R 사용 시 게임에서 재생합니다.** 카드 확대 표시가 끝나면 손패와 겹치지 않는 전투 중앙에 표시하며, 마지막 R을 마친 뒤 승리 보상이나 패배 화면으로 넘어갑니다. [런타임 적용과 운영 규약](ULTIMATE_ANIMATIONS.md)을 참고하세요.

이 문서 아래의 아트 제작·갤러리 검사는 게임 적용 전 각 시점의 기록입니다. 원본 프롬프트·manifest의 `game_integrated: false`와 `unintegrated_art_prototype`도 제작 당시 상태로 보존합니다. 현재 런타임 자산의 바이트 일치·프레임 규약은 [자산 감사](runtime-ultimate-assets.json)에 기록하고, 실제 Unity import와 Windows 플레이어 검증은 별도 결과로 관리합니다.

게임 적용 전 마지막 아트 수정은 2026-10-14의 **마르티나·띠아·다르코·나딘 최초 프롬프트 재생성**입니다. [로컬 갤러리](http://127.0.0.1:61624/index.html?revision=runtime1-20261014#martina)와 [당시 최초 원문·실제 생성 프롬프트·참고 자료·검증 기록](../ArtSource/AnimationPrototypes/ORIGINAL_PROMPT_REGEN_20261014.md)을 참고하세요. 당시 네 명·48프레임 자산 검사와 갤러리 91명·1,140개 프레임 표시 검사를 통과했고, 나머지 87명의 682개 파일과 게임 폴더의 변경은 0건이었습니다. 가넷의 머리색 보정은 유지합니다.

[앞선 다섯 명의 부분 보정](../ArtSource/AnimationPrototypes/POLISH_NOTES_20261014.md)과 아래 검증 수치는 이전 시점의 기록입니다. 당시 나머지 86명의 664개 파일과 게임 폴더의 변경이 없음을 확인했으며, 이번 네 명의 새 제작본에 대한 검사 수치로 사용하지 않습니다.

이에 앞서 아야·비형·다르코·에키온·엘레나·가넷·이렘·아이솔·재키·마르티나·나딘·시셀라·띠아의 13종을 **2등신과 큰 도트**로 다시 제작했습니다. 아래 13종의 제작·검증 설명은 [앞선 단계의 기록](../ArtSource/AnimationPrototypes/REDRAW_NOTES_20261014.md)이며, 이번 네 명의 새 제작본 검사 결과와 구분합니다.

## 확인하기

[갤러리 사용법](../ArtSource/AnimationPrototypes/README.md)을 참고하세요. 최신 로컬 폴더의 `index.html`을 열면 검색·정지·프레임 이동·속도 변경을 사용할 수 있습니다. 인터넷 연결이나 게임 설치가 필요하지 않습니다. 배포 ZIP은 전체를 먼저 풀어야 하며, 업로드 당시 버전이므로 최신 로컬 수정본과 다를 수 있습니다.

## 원본과 제작 기록

앞선 13종은 내장 `image_gen.imagegen`으로 실험체마다 새로 제작했습니다. [2등신 스타일 기준](../ArtSource/AnimationPrototypes/Style/chibi2-reference.png)은 큰 머리와 짧은 몸의 비율·큰 사각 픽셀의 표현을, 처음 승인한 [니아 첫 프레임](../ArtSource/AnimationPrototypes/Nia/frames/frame-00.png)은 원본 362×362 이미지에 대한 픽셀 밀도를 참고합니다. 머리의 3~4단계 굵은 명암 면, 이어진 짙은 윤곽과 개별 눈·입을 표현했습니다. 일부 무기·옷에는 더 가는 선이 남으므로 모든 픽셀이 균일한 64×64 논리 격자라고 설명하지 않습니다.

캐릭터 정체성은 13명의 실제 공식 기본 전신 원화를 직접 확인해 반영했습니다. 각 `Subjects/{id}/official-reference.png`에 원본 바이트 사본이 있고, [원작 분석 문서](../ArtSource/AnimationPrototypes/ORIGINAL_IDENTITY_NOTES_20261014.md)와 [JSON](../ArtSource/AnimationPrototypes/ORIGINAL_IDENTITY_ANALYSIS_20261014.json)에 전신 출처·크기·SHA-256과 외형·R 동작을 기록했습니다. 폐기한 이전 시트를 새 제작의 스타일 기준으로 사용하지 않았고, 리오 참고 이미지는 이번 최종 생성 입력에서 제외했습니다.

원작 공식 R의 핵심 동작을 짧은 모션으로 해석한 창작 시안입니다. 비형은 도약→공중 거대화→거대 상태 망치 내려치기→지면 충격파→원래 크기 회수를 표현합니다. 변신형·강화형 R에는 활성화와 대표 후속 행동을 함께 담았습니다. 원작 애니메이션 파일을 추출하거나 모든 원작 모션을 프레임별로 복제한 자료는 아닙니다.

앞선 13종의 `generated-sheet.png`는 생성 PNG의 원본 바이트를 보존했습니다. 이번 가넷만 예외로, 실제 내장 도구 출력의 머리 RGB를 원본 시트에 한정 합성한 결과를 최종 `generated-sheet.png`로 사용합니다. 원본 알파·선택 영역 밖 RGBA·관에 가려진 두 프레임의 동일성은 [hair-color-proof.json](../ArtSource/AnimationPrototypes/Subjects/garnet/hair-color-proof.json)에서 검증하며, 도구 출력은 `hair-color-tool-output.png`로 따로 보존합니다. 이번 나머지 네 명은 도구 출력 원본 바이트입니다.

`atlas.png`는 겹치지 않는 전체 원본 분할·정수 좌표 이동·투명 패딩으로 조립한 4열×3행 고정 셀 시트입니다. 원본 RGBA를 정확히 보존하며 리사이즈하지 않았습니다. 셀마다 최소 24px 투명 여백과 외곽 잘림 없음을 검사했습니다. 수동 발 위치 기준을 포함해 일반 접지 프레임의 높이를 맞추고, 비형의 공중 프레임은 정수 상향 이동으로 도약을 유지했습니다. 이렘의 변신이나 가넷의 관 가림 구간은 일반 접지 정렬의 예외입니다. 갤러리의 장면 표시 영역은 유지합니다.

아래 **프롬프트** 링크에 실험체별 실제 생성 지시문을 보관했습니다. 각 폴더의 `manifest.json`에는 생성 원본 SHA-256, 참고 이미지와 역할, 공식 출처, 12프레임 동작과 보정 이력이 있습니다.

## 이전 다섯 명 부분 보정 검증

- [최종 설치본 감사](validation-polish5-20261014/asset-scope-audit.json): 다섯 명·60프레임 검사를 통과했으며, 나머지 86명의 664개 파일과 게임 폴더의 변경은 0건입니다.
- [갤러리 브라우저 검사](validation-polish5-20261014/gallery-browser-validation.json): 91명·1,140개 프레임 표시·12개 기능 검사 통과, 스크린샷 7개 보존, 오류 0건입니다.
- [HTTP 자산 검사](validation-polish5-20261014/http-assets.json): 다섯 명 × 원본 시트·재생 시트·GIF·WebP·공식 원화, 총 25개 파일이 로컬 바이트와 같습니다.
- [HTTP 프레임 복귀 검사](validation-polish5-20261014/http-browser.json): 다섯 명 모두 1→8→1프레임 복귀 시 첫 화면이 같았으며, 장면 캡처 12개를 보존했고 오류는 0건입니다. 가넷의 머리색, 세 명의 픽셀 명암, 띠아의 비율과 물감 흐름, 마르티나의 실제 투명 배경도 직접 확인했습니다.

최종 자료를 보존한 뒤 [이번 임시 파일 정리](storage-cleanup-polish5-staging-20261014.json)로 156개 파일·61,476,814바이트(약 61MB)를 제거했습니다. 다음 절의 13명 검증과 134MB 정리는 앞선 단계의 기록입니다.

## 앞선 13명 재제작 검증

- 총 91종 × 12프레임 = 1,092프레임입니다. 그 단계의 수정 대상은 13종·156프레임입니다. 8fps, GIF·WebP 모두 1.5초 반복입니다.
- 당시 13종 모두 생성 원본 바이트 보존·RGBA 추출 일치·겹치지 않는 전체 원본 분할·최소 24px 투명 패딩·외곽 잘림 없음 검사를 통과했습니다. 원본과 재생용 정규화 시트를 구분해 보존합니다. 이후 가넷은 위 머리색 한정 합성 결과로 교체했습니다.
- 당시 [갤러리 브라우저 검증](validation-chibi2-20261014/gallery-browser-validation.json)은 91명·1,140개 프레임 표시·12개 기능 검사에 통과했습니다. 7개 스크린샷을 보존했고 브라우저 오류는 없었습니다.
- 당시 설치본 감사에서 수정한 13명·156프레임과 기존 78명의 576개 추적 파일 변경 0건을 확인했습니다.
- [HTTP 자산 검사](validation-chibi2-20261014/http-assets.json)는 수정한 13명 × 생성 원본·재생 시트·GIF·WebP·공식 전신, 총 65개 자산의 바이트가 로컬 파일과 일치함을 확인했습니다.
- [HTTP 프레임 복귀 검사](validation-chibi2-20261014/http-browser.json)는 91명 목록과 수정한 13명 각각의 1→8→1프레임 전환에 통과했습니다. 돌아온 첫 프레임과 처음 표시한 첫 프레임의 스크린샷 바이트가 같아 해당 검사에서 효과 누적이 없었습니다. 장면 캡처 18개를 보존했고 13명 장면도 직접 검토했습니다.
- [원본 분리·RGBA·발 위치 검토](validation-chibi2-20261014/ground-review.json)는 13명·156프레임의 분리와 최소 24px 여백을 확인했습니다.
- [이전 니아 스타일 검증](validation-nia-style-20261014/)의 91명·1,140개 프레임 표시, HTTP 재생·자산 검사 수치는 앞선 제작본의 기록입니다.
- 니아를 포함한 기존 78명의 576개 추적 파일, 게임의 `Assets`, `Packages`, `ProjectSettings`는 변경하지 않았습니다. Windows 게임을 다시 빌드하거나 런타임에 연결하지 않습니다.

재조립 도구는 `Tools/NormalizeUltimateAtlas.py`와 `Tools/BuildUltimatePreviews.py`, 배포 검증은 `Tools/PackageUltimatePreviews.py`, 갤러리 기능 검증은 `Tools/ValidateUltimateGallery.mjs`입니다. 재조립에는 FFmpeg와 Pillow가 필요하며 갤러리 자체에는 필요하지 않습니다.

앞선 13명 제작에서 최종 원본·프롬프트·공식 참고 자료·스타일 기준·검증 증거를 보존한 뒤 폐기한 제작본과 중복 임시 파일에서 최소 134MB를 정리했습니다. [2등신 제작 임시 파일 정리 기록](storage-cleanup-chibi2-staging-20261014.json)에 삭제 대상과 실제 바이트 수가 있습니다.

## 전체 목록

| 실험체 | R | 미리보기 | 프롬프트 |
|---|---|---|---|
| 아비게일 | 디멘션 스트라이크 | [GIF](../ArtSource/AnimationPrototypes/Subjects/abigail/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/abigail/prompt.txt) |
| 아델라 | 체크메이트 | [GIF](../ArtSource/AnimationPrototypes/Subjects/adela/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/adela/prompt.txt) |
| 아디나 | 수정구에 비친 운명 | [GIF](../ArtSource/AnimationPrototypes/Subjects/adina/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/adina/prompt.txt) |
| 아드리아나 | 화염 난사 | [GIF](../ArtSource/AnimationPrototypes/Subjects/adriana/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/adriana/prompt.txt) |
| 에이든 | 낙뢰 | [GIF](../ArtSource/AnimationPrototypes/Subjects/aiden/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/aiden/prompt.txt) |
| 알렉스 | 정밀 폭격 | [GIF](../ArtSource/AnimationPrototypes/Subjects/alex/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/alex/prompt.txt) |
| 알론소 | 게더링 필드 | [GIF](../ArtSource/AnimationPrototypes/Subjects/alonso/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/alonso/prompt.txt) |
| 아르다 | 잠들어있는 힘 | [GIF](../ArtSource/AnimationPrototypes/Subjects/arda/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/arda/prompt.txt) |
| 아야 | 공포탄 | [GIF](../ArtSource/AnimationPrototypes/Subjects/aya/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/aya/prompt.txt) |
| 바바라 | 오버클럭 | [GIF](../ArtSource/AnimationPrototypes/Subjects/barbara/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/barbara/prompt.txt) |
| 버니스 | 올가미 탄 | [GIF](../ArtSource/AnimationPrototypes/Subjects/bernice/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/bernice/prompt.txt) |
| 비앙카 | 진조의 군림 | [GIF](../ArtSource/AnimationPrototypes/Subjects/bianca/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/bianca/prompt.txt) |
| 비형 | 신명나게 놀아보자! | [GIF](../ArtSource/AnimationPrototypes/Subjects/bihyung/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/bihyung/prompt.txt) |
| 블레어 | XMS-5 활성화 / 끝없는 추적 | [GIF](../ArtSource/AnimationPrototypes/Subjects/blair/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/blair/prompt.txt) |
| 카밀로 | 두엔데 | [GIF](../ArtSource/AnimationPrototypes/Subjects/camilo/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/camilo/prompt.txt) |
| 캐시 | 이머전시 OP | [GIF](../ArtSource/AnimationPrototypes/Subjects/cathy/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/cathy/prompt.txt) |
| 셀린 | 자력 융합 | [GIF](../ArtSource/AnimationPrototypes/Subjects/celine/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/celine/prompt.txt) |
| 세레스 | 빛에게 바치는 맹세 | [GIF](../ArtSource/AnimationPrototypes/Subjects/ceres/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/ceres/prompt.txt) |
| 샬럿 | 기적 실현 | [GIF](../ArtSource/AnimationPrototypes/Subjects/charlotte/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/charlotte/prompt.txt) |
| 키아라 | 폭주 | [GIF](../ArtSource/AnimationPrototypes/Subjects/chiara/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/chiara/prompt.txt) |
| 클로에 | 생명 공유 | [GIF](../ArtSource/AnimationPrototypes/Subjects/chloe/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/chloe/prompt.txt) |
| 코렐라인 | 이세계의 잔영 | [GIF](../ArtSource/AnimationPrototypes/Subjects/coraline/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/coraline/prompt.txt) |
| 크레이버 | 쇼다운! | [GIF](../ArtSource/AnimationPrototypes/Subjects/craver/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/craver/prompt.txt) |
| 리 다이린 | 취호격파산 | [GIF](../ArtSource/AnimationPrototypes/Subjects/dailin/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/dailin/prompt.txt) |
| 다니엘 | 걸작 | [GIF](../ArtSource/AnimationPrototypes/Subjects/daniel/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/daniel/prompt.txt) |
| 다르코 | 강제집행 | [GIF](../ArtSource/AnimationPrototypes/Subjects/darko/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/darko/prompt.txt) |
| 데비&마를렌 | 트윈즈 러시 | [GIF](../ArtSource/AnimationPrototypes/Subjects/debi_marlene/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/debi_marlene/prompt.txt) |
| 에키온 | VF폭주 / 독사의 진노 | [GIF](../ArtSource/AnimationPrototypes/Subjects/echion/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/echion/prompt.txt) |
| 엘레나 | 죽음의 무도 | [GIF](../ArtSource/AnimationPrototypes/Subjects/elena/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/elena/prompt.txt) |
| 일레븐 | 다 덤벼보라구! | [GIF](../ArtSource/AnimationPrototypes/Subjects/eleven/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/eleven/prompt.txt) |
| 엠마 | Change★ | [GIF](../ArtSource/AnimationPrototypes/Subjects/emma/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/emma/prompt.txt) |
| 에스텔 | 헬기호출 | [GIF](../ArtSource/AnimationPrototypes/Subjects/estelle/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/estelle/prompt.txt) |
| 이바 | VF 방출 | [GIF](../ArtSource/AnimationPrototypes/Subjects/eva/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/eva/prompt.txt) |
| 펠릭스 | 뇌룡격 | [GIF](../ArtSource/AnimationPrototypes/Subjects/felix/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/felix/prompt.txt) |
| 펜리르 | 숨통 끊기 | [GIF](../ArtSource/AnimationPrototypes/Subjects/fenrir/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/fenrir/prompt.txt) |
| 피오라 | 플레슈 | [GIF](../ArtSource/AnimationPrototypes/Subjects/fiora/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/fiora/prompt.txt) |
| 가넷 | 처형식 | [GIF](../ArtSource/AnimationPrototypes/Subjects/garnet/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/garnet/prompt.txt) |
| 하트 | Peacemaker | [GIF](../ArtSource/AnimationPrototypes/Subjects/hart/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/hart/prompt.txt) |
| 헤이즈 | 로켓 런처 | [GIF](../ArtSource/AnimationPrototypes/Subjects/haze/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/haze/prompt.txt) |
| 헨리 | 시간 재구축 영역 | [GIF](../ArtSource/AnimationPrototypes/Subjects/henry/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/henry/prompt.txt) |
| 히스이 | 모노호시자오 / 츠바메가에시 | [GIF](../ArtSource/AnimationPrototypes/Subjects/hisui/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/hisui/prompt.txt) |
| 혜진 | 오대존명왕진 | [GIF](../ArtSource/AnimationPrototypes/Subjects/hyejin/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/hyejin/prompt.txt) |
| 현우 | 핵펀치 | [GIF](../ArtSource/AnimationPrototypes/Subjects/hyunwoo/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/hyunwoo/prompt.txt) |
| 이안 | 해방 / 넌 벗어날 수 없어 | [GIF](../ArtSource/AnimationPrototypes/Subjects/ian/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/ian/prompt.txt) |
| 이렘 | 고양이로 펑! | [GIF](../ArtSource/AnimationPrototypes/Subjects/irem/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/irem/prompt.txt) |
| 아이작 | 강탈 | [GIF](../ArtSource/AnimationPrototypes/Subjects/isaac/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/isaac/prompt.txt) |
| 아이솔 | Mok제 폭탄 | [GIF](../ArtSource/AnimationPrototypes/Subjects/isol/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/isol/prompt.txt) |
| 이슈트반 | 파동 함수 붕괴 | [GIF](../ArtSource/AnimationPrototypes/Subjects/istvan/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/istvan/prompt.txt) |
| 재키 | 전기톱 살인마 | [GIF](../ArtSource/AnimationPrototypes/Subjects/jackie/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/jackie/prompt.txt) |
| 얀 | 쿼드라곤 | [GIF](../ArtSource/AnimationPrototypes/Subjects/jan/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/jan/prompt.txt) |
| 제니 | 시상식의 여왕 | [GIF](../ArtSource/AnimationPrototypes/Subjects/jenny/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/jenny/prompt.txt) |
| 요한 | 구원의 성역 | [GIF](../ArtSource/AnimationPrototypes/Subjects/johann/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/johann/prompt.txt) |
| 유스티나 | 아스트라 버스트 | [GIF](../ArtSource/AnimationPrototypes/Subjects/justyna/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/justyna/prompt.txt) |
| 칼라 | 구속의 사슬 | [GIF](../ArtSource/AnimationPrototypes/Subjects/karla/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/karla/prompt.txt) |
| 카티야 | 정밀 조준 | [GIF](../ArtSource/AnimationPrototypes/Subjects/katja/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/katja/prompt.txt) |
| 케네스 | 화염 분쇄 | [GIF](../ArtSource/AnimationPrototypes/Subjects/kenneth/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/kenneth/prompt.txt) |
| 라우라 | 황혼의 도둑 | [GIF](../ArtSource/AnimationPrototypes/Subjects/laura/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/laura/prompt.txt) |
| 레니 | 스프링! 트랩 | [GIF](../ArtSource/AnimationPrototypes/Subjects/leni/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/leni/prompt.txt) |
| 르노어 | 고뇌의 광시곡 | [GIF](../ArtSource/AnimationPrototypes/Subjects/lenore/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/lenore/prompt.txt) |
| 레녹스 | 푸른뱀 | [GIF](../ArtSource/AnimationPrototypes/Subjects/lenox/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/lenox/prompt.txt) |
| 레온 | 파도타기 | [GIF](../ArtSource/AnimationPrototypes/Subjects/leon/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/leon/prompt.txt) |
| 루치아 | 총사의 예법 | [GIF](../ArtSource/AnimationPrototypes/Subjects/lucia/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/lucia/prompt.txt) |
| 루크 | 애프터 서비스 | [GIF](../ArtSource/AnimationPrototypes/Subjects/luke/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/luke/prompt.txt) |
| 매그너스 | 폭주 바이크 | [GIF](../ArtSource/AnimationPrototypes/Subjects/magnus/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/magnus/prompt.txt) |
| 마이 | 익스클루시브 | [GIF](../ArtSource/AnimationPrototypes/Subjects/mai/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/mai/prompt.txt) |
| 마커스 | 지각변동 | [GIF](../ArtSource/AnimationPrototypes/Subjects/markus/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/markus/prompt.txt) |
| 마르티나 | 녹화 | [GIF](../ArtSource/AnimationPrototypes/Subjects/martina/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/martina/prompt.txt) |
| 미르카 | 다운버스트 | [GIF](../ArtSource/AnimationPrototypes/Subjects/mirka/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/mirka/prompt.txt) |
| 나딘 | 늑대 맹습 | [GIF](../ArtSource/AnimationPrototypes/Subjects/nadine/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/nadine/prompt.txt) |
| 나타폰 | 셔터 찬스 | [GIF](../ArtSource/AnimationPrototypes/Subjects/nathapon/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/nathapon/prompt.txt) |
| 니아 | 니아의 게임월드 | [GIF](../ArtSource/AnimationPrototypes/Nia/nia-ultimate-preview.gif) | [제작 기록](ART_NIA_ULTIMATE_PROTOTYPE.md) |
| 니키 | 분노의 어퍼컷! | [GIF](../ArtSource/AnimationPrototypes/Subjects/nicky/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/nicky/prompt.txt) |
| 피올로 | 응징자 | [GIF](../ArtSource/AnimationPrototypes/Subjects/piolo/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/piolo/prompt.txt) |
| 프리야 | 대지의 메아리 | [GIF](../ArtSource/AnimationPrototypes/Subjects/priya/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/priya/prompt.txt) |
| 리오 | 연사/정사필중 | [GIF](../ArtSource/AnimationPrototypes/Subjects/rio/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/rio/prompt.txt) |
| 로지 | 셈텍스탄 Mk-II | [GIF](../ArtSource/AnimationPrototypes/Subjects/rozzi/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/rozzi/prompt.txt) |
| 쇼우 | 뜨거운 맛 | [GIF](../ArtSource/AnimationPrototypes/Subjects/sho/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/sho/prompt.txt) |
| 쇼이치 | 무자비 | [GIF](../ArtSource/AnimationPrototypes/Subjects/shoichi/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/shoichi/prompt.txt) |
| 실비아 | 기동전 | [GIF](../ArtSource/AnimationPrototypes/Subjects/silvia/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/silvia/prompt.txt) |
| 시셀라 | 모두 해방이에요. | [GIF](../ArtSource/AnimationPrototypes/Subjects/sissela/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/sissela/prompt.txt) |
| 수아 | 기억력 | [GIF](../ArtSource/AnimationPrototypes/Subjects/sua/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/sua/prompt.txt) |
| 타지아 | 파라디소 | [GIF](../ArtSource/AnimationPrototypes/Subjects/tazia/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/tazia/prompt.txt) |
| 테오도르 | 에너지 필드 | [GIF](../ArtSource/AnimationPrototypes/Subjects/theodore/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/theodore/prompt.txt) |
| 띠아 | 무지개 드로잉 | [GIF](../ArtSource/AnimationPrototypes/Subjects/tia/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/tia/prompt.txt) |
| 츠바메 | 오의 - 생사 각인 | [GIF](../ArtSource/AnimationPrototypes/Subjects/tsubame/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/tsubame/prompt.txt) |
| 바냐 | 꿈결 가루 | [GIF](../ArtSource/AnimationPrototypes/Subjects/vanya/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/vanya/prompt.txt) |
| 윌리엄 | 위닝샷 | [GIF](../ArtSource/AnimationPrototypes/Subjects/william/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/william/prompt.txt) |
| 슈린 | 만검귀종 | [GIF](../ArtSource/AnimationPrototypes/Subjects/xuelin/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/xuelin/prompt.txt) |
| 유키 | 화무십일홍 | [GIF](../ArtSource/AnimationPrototypes/Subjects/yuki/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/yuki/prompt.txt) |
| 유민 | 풍류운산 | [GIF](../ArtSource/AnimationPrototypes/Subjects/yumin/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/yumin/prompt.txt) |
| 자히르 | 바르가바스트라 | [GIF](../ArtSource/AnimationPrototypes/Subjects/zahir/preview.gif) | [프롬프트](../ArtSource/AnimationPrototypes/Subjects/zahir/prompt.txt) |
