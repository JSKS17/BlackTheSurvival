# 새 치비 스프라이트

v0.1.11에서는 실험체 91명과 하나, 총 92명의 스프라이트를 새로 제작한다. 사용자가 첨부한 리오 팬 도트는 스타일 참고로만 사용했다. 해당 팬 그림의 픽셀·포즈·워터마크를 게임 자산으로 복사하지 않았으며 원본 참고 파일은 배포하지 않는다. [참고 용도·해시](style-reference.json)를 기록한다.

큰 둥근 머리와 짧은 몸의 약 2.2등신 비율, 차분한 재질별 색상, 어두운 윤곽선과 뭉쳐진 정사각형 도트를 공통 표현으로 삼는다. 얼굴은 공식 캐릭터 이미지와 [개별 얼굴 설계](../art-identity/face-designs.json)를 참고한다. 머리색·눈매·눈썹·피부색·안경·가면·수염 및 대표 무기를 개별 구성한다. 캐릭터의 포즈는 새로 만든다.

## 파일과 제작

- 게임 자산: `Assets/Resources/Lumia/PortraitsReference/{id}.png`.
- 새 생성 원본: `generated/{id}.png`. 내장 `image_gen` 편집 모드로 캐릭터마다 별도 생성하며 투명 배경을 요청한다.
- 실제 프롬프트: `prompts/{id}.txt`, 공통 지침: [common-prompt.txt](common-prompt.txt).
- 개별 생성·참조·파일 해시: `generation/{id}.json`.
- 전체 파일과 검증: [asset-index.json](asset-index.json), [validation.json](validation.json).
- 캐릭터별 연락 시트: `contact-sheets/sprites_*.png`.

`Tools/PrepareReferencePortraits.py --adopt --id ID --source ABSOLUTE_PATH`는 생성된 투명 원본을 보관하고 알파 경계를 자른 뒤 최근접 보간으로 72×72 이내에 맞춘다. 80×80 투명 캔버스에 여백을 두고 정확히 2배 확대하여 160×160 게임 파일을 만든다. 색 재도색·배경 제거·수동 얼굴 합성은 하지 않는다. Unity는 Point 필터·무압축·밉맵 없음으로 불러온다.

`--prepare`로 프롬프트를 다시 만들려면 사용자의 스타일 참고 파일을 로컬 `Temp/ReferenceChibi/style-reference.png`에 제공해야 한다. 이 파일은 Git과 배포본에 포함하지 않는다. `--rebuild`는 저장된 개별 기록으로 누락·해시·투명도·정수 픽셀 격자·중복을 검증하고 연락 시트를 만든다. 게임은 새 폴더를 우선 로드하고 이전 자산을 호환용으로 보존한다.

실제 Windows 빌드의 `-lumia-portrait-verify`는 모든 캐릭터가 새 리소스를 사용했는지도 검사한다. 실행·화면·배포 검증은 [v0.1.11 기록](../validation-reference-chibi/README.md)에 정리한다.

## v0.1.12 기본 외형 교정

카티야와 비형의 외형 자료에 잘못 들어간 안경 지시를 제거했다. 내장 `image_gen`의 `precise-object-edit`로 기존 치비의 얼굴에서 안경테·코다리만 제거하고 눈 색·표정·의상·소품을 유지한다. 비형의 붉은 볼 표시는 안경과 구분하여 남긴다.

블레어는 [공식 기본 복장 전신 컨셉시트](https://cdn.playeternalreturn.com/event/season9/blair/blair01.png), [공식 공개 페이지](https://event.playeternalreturn.com/S9/Blair?hl=ko-KR)를 기준으로 검정 스키니 긴바지와 앵클부츠, 은색 벨트·체인 및 옆골반 레이스업을 교정했다. 상반신의 민소매 하이넥 크롭탑·어깨 아래로 걸친 짧은 퍼 재킷과 기존 얼굴·머리·치비 비율을 유지한다. 참고 시트는 `references/blair-default-concept.png`에 보관한다.

세 교정의 실제 프롬프트는 `prompts/{id}.txt` 및 [prompt-overrides.json](prompt-overrides.json)에 있다. `--prepare`도 이 교정을 보존한다. `generation/{id}.json`의 `inputReferences`에는 편집 대상의 이전 버전, 공식 참고, 스타일 참고의 역할과 SHA를 기록한다. 이전 그림은 `generated/versions/`에 보관하며 게임은 교정된 세 PNG를 사용한다. [Windows 교정 검증](../validation-sprite-corrections/README.md)을 함께 보관한다.
