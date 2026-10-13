# 실험체별 R 애니메이션 컬렉션

게임 등록 실험체 91명 전원의 시안을 제공합니다. 기존 니아 1종을 유지하고 나머지 90종을 새로 제작했습니다. 로스터는 실제 게임 데이터에서 추출한 `roster.json`을 기준으로 합니다. `Assets`, `Packages`, `ProjectSettings`에는 변경이 없습니다. 게임 버전은 v0.1.27을 유지합니다.

## 확인하기

[갤러리 사용법](../ArtSource/AnimationPrototypes/README.md)을 참고하세요. 배포 ZIP 전체를 풀고 `index.html`을 열면 검색·정지·프레임 이동·속도 변경을 사용할 수 있습니다. 인터넷 연결이나 게임 설치가 필요하지 않습니다.

## 원본과 제작 기록

각 시트는 내장 `image_gen.imagegen`을 사용해 개별 제작했습니다. 기존 니아 시트는 스타일 참고로만, 각 실험체 참고 이미지는 얼굴·복장·소품의 기준으로 사용했습니다. 원작 공식 R 설명에서 핵심 동작을 확인하고 짧은 도트 모션으로 해석한 창작 시안이며 원작 애니메이션 파일을 추출한 자료가 아닙니다.

원본 PNG를 바이트 단위로 보존했습니다. 실제 투명 여백을 따라 프레임 경계를 찾고, 투명 패딩과 정수 좌표 배치로 재생 파일을 만듭니다. 행 위치의 차이는 보정하며 의도된 점프·돌진·퇴장은 남깁니다. 발이나 이웃 프레임의 효과가 잘못 포함되지 않도록 경계 사례를 별도로 검토했습니다. 변신형·강화형 R은 활성화와 대표 후속 행동을 함께 표시합니다.

아래 **프롬프트** 링크에 실험체별 실제 생성 지시문 전체를 보관했습니다. 각 폴더의 `manifest.json`에는 원본 SHA-256, 사용한 이미지 경로, 공식 출처, 12프레임 동작 설명과 시각 검토가 있습니다. 실비아의 캐릭터 누락과 라우라·비형·가넷·나딘의 프레임 간 효과 침범은 내장 도구로 시트를 수정했으며 초기 시트와 프롬프트도 참고 자료로 보존합니다.

## 검증

- 총 91종 × 12프레임 = 1,092프레임입니다. 8fps, GIF·WebP 모두 1.5초 반복입니다.
- 새 90종의 원본 해시, RGBA 추출, 투명도, 프레임 구분, WebP 무손실 색상과 알파를 검사했습니다.
- 실제 브라우저에서 전 실험체 선택과 프레임 이동, 검색·재생 조작, 375px 화면을 확인합니다. 결과는 [검증 폴더](validation-ultimate-collection/)에 보관합니다.
- Windows 게임을 다시 빌드하거나 런타임에 연결하지 않습니다. ZIP은 HTML 갤러리와 이미지 자료만 포함합니다.

재조립 도구는 `Tools/BuildUltimatePreviews.py`, 배포 검증은 `Tools/PackageUltimatePreviews.py`, 갤러리 기능 검증은 `Tools/ValidateUltimateGallery.mjs`입니다. 재조립에는 FFmpeg와 Pillow가 필요하며 갤러리 자체에는 필요하지 않습니다.

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
