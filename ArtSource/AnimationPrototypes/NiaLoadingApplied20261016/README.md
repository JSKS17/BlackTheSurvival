# 니아 원 업 · 적용 승인된 로딩 연출

`preview.html`에서 수정된 8초 루프를 확인합니다. 정지, 시간 이동, 속도 조절 및 투명 배경 확인을 지원합니다. 공유용 애니메이션은 `nia-loading-preview.webp`(투명·무손실)와 `nia-loading-preview.gif`입니다.

사용자가 2026-10-16 승인한 수정은 초반 걷기입니다. 양팔과 두 손으로 게임기를 계속 잡고 상반신을 안정시킨 상태에서, 양다리가 번갈아 접지하고 발목 부근을 통과하는 보행으로 바꿨습니다. 별도로 생성한 6개 보행 셀 중 반대발 접지/통과가 잘 맞는 1·2·5·6번 셀 4개를 루프로 사용합니다. 머리 중심을 기준으로 정렬해 부츠의 폭 변화가 상반신을 흔들지 않도록 했습니다. 보행 포즈는 초당 6장, 이동·합성 프레임은 초당 20장입니다.

초반 2.2초 이후의 로고 낙하 → 만화적으로 깔림 → 원 업 1UP·보라색 기둥·상승 화살표 → 무릎을 짚고 일어나 헤드폰을 정리 → 대기 연출은 승인된 원본 포즈와 타이밍을 유지합니다. 총 재생 시간은 8,000ms, 무한 반복입니다.

`walk-sheet.png`는 built-in image_gen으로 생성한 보행 수정본입니다. 기존 승인된 니아 R 아틀라스와 로딩 포즈 시트를 참고했습니다. `walk-prompt.txt`와 `walk-correction-prompt.txt`에 실제 프롬프트를 보존했습니다. `generated-sheet.png`는 승인된 깔림·부활·기상 포즈 시트, `one-up-effect.png`는 앞서 생성한 E 원 업 이펙트, `game-logo.png`는 승인된 투명 도트 로고입니다.

2026-10-19 사용자 요청에 따라 모든 홍채의 유색 RGB만 흑안으로 바꾸고, 앞 걷기 6셀은 뒷 회복 포즈의 소재별 색감을 참고해 머리·피부·의상·어두운 재질의 RGB를 보정했습니다. built-in image_gen의 가이드를 검수한 뒤, 원본 좌표의 제한된 RGB 마스크로 적용했습니다. 회복 시트의 눈 밖 RGBA와 두 시트의 모든 알파·형태·위치는 보존합니다. 흰자·반사광·속눈썹·원 업 파티클·로고 원본은 유지합니다. 이전 원본과 마스크·프롬프트·전 프레임 검증은 `Build/NiaEyesVerification`에 보관합니다. `manifest.json`과 `provenance.json`의 `userOverride`에 예전 원본 바이트 보존 기록을 대체하는 이번 변경 범위를 명시했습니다.

GIF는 기존 GIF의 전역·지역 팔레트, 프레임 위치, 투명 인덱스와 disposal·delay를 유지하고, 허용 마스크 안의 색 인덱스만 목표 RGB에 가장 가까운 기존 색으로 교체합니다. 새 팔레트 양자화로 눈 밖 색이 바뀌는 것을 방지하며, 실제 합성·디코드 97프레임의 마스크 밖 RGBA 변경이 0임을 검증했습니다. `assemble-preview.mjs`는 이 보존 후처리를 함께 실행하므로 `Build/NiaEyesVerification`의 원본·마스크·재생성 helper를 유지해야 합니다. WebP의 모든 가시 픽셀과 알파도 같은 보존 검증을 통과했습니다. 완전히 투명한 알파 0 영역의 표시되지 않는 RGB만 무손실 WebP 인코더가 최적화합니다.

`assemble-preview.mjs`는 수정이 승인된 포즈 영역 선택, 정수 좌표 배치, nearest-neighbor 캔버스 렌더링 및 GIF/WebP 인코딩을 수행합니다. 소스 포즈의 형태와 알파는 추가로 수정하지 않으며, GIF 후처리도 허용된 RGB 마스크 안에서만 작동합니다. 아주 희미한 생성 잔광을 포함한 원본 알파를 유지하며 불투명도 16/255 이상의 가시 픽셀은 셀 분할선에 걸리지 않습니다. HTML과 내보낸 애니메이션은 같은 타임라인을 사용합니다.

WebP를 아래 경로에 동일 바이트로 복사해 https://bser-sts.com 의 실제 로딩 화면에 적용했습니다. 부모 사이트와 iframe은 같은 `/game/TemplateData/nia-loading.webp` URL을 사용합니다. 2026-10-16 배포 v7에서 실제 브라우저 표시와 준비 완료 후 화면 전환, HTTP MIME·크기·SHA를 검증했습니다. 증거는 `Build/WebVerification/gameplay-browser-verification.json`과 `gameplay-live-http.json`에 있습니다.

- `Assets/WebGLTemplates/BlackTheSurvival/TemplateData/nia-loading.webp`

재생성: 프로젝트 루트에서 `node ArtSource/AnimationPrototypes/NiaLoadingApplied20261016/assemble-preview.mjs` (기존 WebSite의 sharp 사용). `manifest.json`은 셀 경계·앵커·선택 순서, `validation.json`은 해시·투명도·반복·재생 시간, `provenance.json`은 생성·참고·적용 파일 정보를 기록합니다.
