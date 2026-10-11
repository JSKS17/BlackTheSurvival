# v0.1.12 세 실험체 외형 교정

블레어는 공식 기본 복장의 검정 긴바지·앵클부츠·벨트와 체인으로 교정했다. 카티야와 비형은 잘못 추가된 안경테와 코다리를 제거했다. 세 명의 치비 비율·머리·표정·의상 색을 유지하며, 나머지 89개 게임 스프라이트는 변경하지 않는다.

내장 `image_gen`의 `precise-object-edit`를 캐릭터별로 사용했다. 기존 그림을 편집 대상으로, 공식 이미지를 외형 기준으로, 사용자 첨부는 스타일 기준으로 지정했다. 블레어의 [공식 전신 컨셉시트](https://cdn.playeternalreturn.com/event/season9/blair/blair01.png)는 하의·부츠까지 확인하는 데 사용했다.

교정된 게임 파일은 다음과 같다.

- `Assets/Resources/Lumia/PortraitsReference/blair.png`
- `Assets/Resources/Lumia/PortraitsReference/katja.png`
- `Assets/Resources/Lumia/PortraitsReference/bihyung.png`

[실제 편집 프롬프트](../art-reference-chibi/prompt-overrides.json)와 `generation/{id}.json`에 편집 대상·공식 참고·스타일 참고의 역할 및 해시를 기록했다. 이전 그림은 `generated/versions/`에 보존한다. `Tools/PrepareReferencePortraits.py --prepare`는 교정 프롬프트를 유지하고, `--rebuild`는 92개 PNG와 참조·생성 해시·투명도·정확한 2배 픽셀 격자를 검사한다.

실제 Windows 개발 빌드의 `-lumia-portrait-verify`에서 92명의 새 리소스 로드와 8페이지 연락판을 확인했다. [네이티브 검증](native-art/png-verification.json)은 92개 PNG·임포터의 소스/빌드 해시와 Unity가 내보낸 가시 픽셀이 모두 일치하며, 누락·실행 오류는 0개다. 카티야가 포함된 `native-art/sprites_07.png`와 블레어·비형이 포함된 `native-art/sprites_08.png`에서 교정 외형도 확인했다.

[개발 빌드](development-build.log)와 [배포 빌드](release-build.log)는 오류 0개로 완료했다. 개인 이어하기 저장이 [native 검사 전후](native-save-preservation.json)와 [설치·정상 시작](release-startup.json)에서 유지됐다. [배포 파일 해시](release-hashes.json)는 빌드 출력과 일치한다. [ZIP 검증](archive-validation.json)은 181개 파일의 CRC·SHA·누락·추가 파일·개인 저장 제외를 통과했다. ZIP 크기는 92,909,101바이트다. [v0.1.12 Windows 릴리스](https://github.com/JSKS17/BlackTheSurvival/releases/tag/v0.1.12)에서 실행 파일을 받을 수 있다.
