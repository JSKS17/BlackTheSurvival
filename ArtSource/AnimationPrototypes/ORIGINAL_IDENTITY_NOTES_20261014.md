# 원작 전신 원화·R 동작 검토

13명의 실제 기본 전신 원화 PNG를 모두 확보하고 직접 확인했습니다. 영구 사본은 이 폴더를 기준으로 `Subjects/{id}/official-reference.png`에 있습니다. 생성된 PortraitsReference, 인게임 도트나 폐기한 애니메이션을 원작 원화로 취급하지 않았습니다.

[분석 JSON](ORIGINAL_IDENTITY_ANALYSIS_20261014.json)의 `reference_path`와 `full_body_path`는 모두 실제 공식 전신 사본을 가리킵니다. 크기·SHA-256·출처 URL도 해당 전신 파일과 일치합니다. 앞서 검토한 상반신 원화의 경로·크기·해시는 `bust_reference`로 구분했습니다. `Subjects/`로 시작하는 경로의 기준은 `ArtSource/AnimationPrototypes/`이고, `docs/` 경로의 기준은 저장소 루트입니다. 참고 자료는 삭제 예정 임시 폴더를 가리키지 않습니다.

각 실험체의 얼굴·머리·복장·무기·R 동작과 하반신 검토 내용을 보존했습니다. 비형·다르코는 공식 기사에 포함된 R 시연도 확인했습니다. 핵심 사항은 비형의 황금색 눈·흰/회색 부츠·기계식 망치, 재키의 전체 백발·주황 허리 재킷·맨다리, 이렘의 노랑 카디건·큰 흰 리본·체크 스커트·흰 양말/리본 신발, 엘레나의 연청색 타이츠/스케이트, 마르티나의 청록 찢어진 청바지/갈색 부츠, 시셀라의 환자복·붕대/흰 부츠입니다. 가넷의 대검은 공식 방망이 무기군과 구분한 사용자 요청의 재해석입니다. 아야는 둥근 안경과 불투명 스타킹의 연속성을 유지합니다.

최신 생성 기준은 **약 2등신, 큰 픽셀, 처음 승인한 니아 원본의 이미지 대비 픽셀 크기, 머리의 3~4단계 굵은 명암 면**입니다. [새 스타일 기준](Style/chibi2-reference.png)은 비율·큰 도트 표현을, [니아 원본](Nia/frames/frame-00.png)은 픽셀 밀도를, 공식 전신 원화는 캐릭터 정체성을 참고합니다. 이전 2.5등신과 그 이전 제작본은 이번 최종 시트로 취급하지 않습니다. 실제 결과의 모든 픽셀이 균일한 논리 격자라는 뜻은 아니며 일부 소품·의상에 가는 선이 남습니다.

원작 정체성 자료와 새로운 생성 시트는 구분해 보존합니다. 새 시트는 내장 `image_gen.imagegen`으로 제작한 아트 시안이며 **게임에는 적용하지 않았습니다.** 최신 [갤러리 브라우저 검사](../../docs/validation-chibi2-20261014/gallery-browser-validation.json)는 91명·1,140개 프레임 표시·12개 기능 검사에 통과했습니다. [HTTP 자산 검사](../../docs/validation-chibi2-20261014/http-assets.json)에서 65개 최종 파일의 바이트 일치를 확인했고, [프레임 복귀 검사](../../docs/validation-chibi2-20261014/http-browser.json)에서 수정한 13명의 1→8→1 전환 후 첫 화면이 일치했습니다. 니아를 포함한 기존 78명의 576개 추적 파일을 유지했습니다. 공식 참고 자료와 최종 제작 자료를 보존한 뒤 중복 임시 파일은 [정리](../../docs/storage-cleanup-chibi2-staging-20261014.json)했습니다. 전체 제작·분할·검증 범위는 [재제작 기록](REDRAW_NOTES_20261014.md)을 참고하세요.

출처: [공식 팬키트](https://playeternalreturn.com/fankit), [1.0.0 실험체 스킬](https://playeternalreturn.com/posts/news/1280?hl=ko-KR), [다르코](https://playeternalreturn.com/posts/news/1954?hl=ko-KR), [비형](https://playeternalreturn.com/posts/news/3572?hl=ko-KR), [가넷](https://playeternalreturn.com/posts/news/2179?hl=ko-KR). 원작 이미지의 권리는 님블뉴런에 귀속됩니다.
