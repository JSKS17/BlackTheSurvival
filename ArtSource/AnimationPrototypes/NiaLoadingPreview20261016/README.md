# 니아 원 업 · 로딩 애니메이션 시안

`preview.html`에서 8초 루프를 재생합니다. 정지, 시간 이동, 속도 조절과 투명 배경 확인이 가능합니다. 공유용 애니메이션은 `nia-loading-preview.webp`(투명, 무손실)와 `nia-loading-preview.gif`입니다.

이 폴더는 미리보기 전용입니다. 사이트와 Unity 로딩 화면에는 적용하지 않았고 런타임 에셋도 변경하지 않았습니다.

니아가 오른쪽으로 걷기 → 게임 로고 낙하 → 만화적으로 납작해짐 → E 원 업의 1UP·보라색 기둥·상승 화살표 → 무릎을 짚고 일어나 헤드폰을 정리 → 대기 순서입니다. 부활할 때 로고는 위로 튕겨 왼쪽에 자리 잡습니다.

`generated-sheet.png`의 16종 몸동작과 `one-up-effect.png`는 built-in image_gen으로 생성했습니다. 기존 승인된 니아 R 아틀라스, 로비 니아, 캐스트 포즈, E 스킬 아이콘을 참고했습니다. 프롬프트는 `prompt.txt`, `one-up-prompt.txt`에 있습니다. 원본 PNG 바이트를 보존했습니다.

`game-logo.png`는 별도로 만든 Black / the / Survival 투명 도트 로고입니다. `one-up-reference.png`는 E 원 업의 실제 게임 스킬 아이콘이며, 색·1UP·상승 화살표의 참고 자료로만 보존합니다.

`assemble-preview.mjs`는 포즈 영역 선택, 정수 좌표 배치, nearest-neighbor 캔버스 렌더링 및 GIF/WebP 인코딩만 수행합니다. 포즈 픽셀을 다시 칠하거나 알파를 지우지 않습니다. 아주 희미한 생성 잔광을 포함한 원본 알파를 유지하며, 불투명도 16/255 이상의 가시 픽셀은 셀 분할선에 걸리지 않는 것을 확인했습니다. 같은 타임라인을 HTML과 내보낸 애니메이션에 사용합니다.

재생성: 프로젝트 루트에서 `node ArtSource/AnimationPrototypes/NiaLoadingPreview20261016/assemble-preview.mjs` (기존 WebSite의 sharp 의존성 사용). `manifest.json`은 셀 경계·앵커, `validation.json`은 해시·알파·반복·전체 재생 시간을 기록합니다.
