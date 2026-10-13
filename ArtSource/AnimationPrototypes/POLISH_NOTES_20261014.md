# 5명 필살기 시안 부분 보정 · 2026-10-14

**이 문서는 이전 부분 보정의 역사 기록입니다.** 이후 마르티나·띠아·다르코·나딘의 시안은 폐기하고 최초 제작 프롬프트로 새로 생성했습니다. 현재 제작본은 [최초 프롬프트 재제작 기록](ORIGINAL_PROMPT_REGEN_20261014.md)을 참고하세요. 가넷의 머리색 보정은 유지합니다. 아래 검증 수치는 당시 시안의 결과이며, 새로 생성한 네 명의 검사 결과가 아닙니다.

2등신 재제작 시안에서 **가넷·다르코·마르티나·나딘·띠아만** 추가 보정했습니다. 기존 그림 스타일과 12프레임의 동작 순서를 유지하는 편집이며, **게임에는 적용하지 않았습니다.**

[당시 갤러리 주소](http://127.0.0.1:61624/index.html?revision=polish5-20261014-final#garnet)는 현재 로컬 파일을 표시하므로 폐기한 시안을 재현하지 않습니다. [앞선 13명 재제작 기록](REDRAW_NOTES_20261014.md)의 검증 수치는 더 이전 단계의 기록입니다.

## 수정 범위

| 실험체 | 이번 보정 | 프롬프트·이력 |
|---|---|---|
| 가넷 | 공식 원화의 차가운 청회색·강철색 머리 중간톤, 깊은 그림자와 작은 은청색 하이라이트를 참고해 머리색만 변경했습니다. 기존 굵은 명암 면과 머리 윤곽을 유지합니다. | [prompt.txt](Subjects/garnet/prompt.txt) · [edit-history.json](Subjects/garnet/edit-history.json) |
| 다르코 | 기존 회청색 머리카락의 계단형 명암, 무늬 코트의 테두리와 접힌 면, 짙은 천과 금속 장식의 작은 반사를 보강했습니다. 기존 얼굴·무기 방향과 잡기→끌기→내리치기 동작을 유지합니다. | [prompt.txt](../../docs/validation-polish5-20261014/rejected-generation-records/darko-prompt.txt) · [edit-history.json](../../docs/validation-polish5-20261014/rejected-generation-records/darko.json) |
| 마르티나 | 회갈색 머리와 크림색 앞머리의 면 명암, 겨자색 코트·청록 청바지·가죽과 금속 부위의 반사를 보강했습니다. 기존 표정·불투명 안대·카메라 렌즈 방향과 동작 순서를 유지합니다. | [prompt.txt](../../docs/validation-polish5-20261014/rejected-generation-records/martina-prompt.txt) · [edit-history.json](../../docs/validation-polish5-20261014/rejected-generation-records/martina.json) |
| 나딘 | 짙은 갈색 머리, 주황 후드와 망토, 바지·무릎 보호대의 제한된 픽셀 명암과 재질 표현을 보강했습니다. 피부색·석궁·작은 볼트와 늑대의 기존 동작을 유지합니다. | [prompt.txt](../../docs/validation-polish5-20261014/rejected-generation-records/nadine-prompt.txt) · [edit-history.json](../../docs/validation-polish5-20261014/rejected-generation-records/nadine.json) |
| 띠아 | 기존 얼굴·복장·붓과 물감 동작을 유지하면서 머리는 크게, 몸과 다리는 짧게 조정해 2등신에 가깝게 했습니다. 도트도 더 굵게 정리했습니다. 프레임 분리를 위해 중간 행 물감 효과의 가로 폭과 여백을 추가 보정했습니다. | [prompt.txt](../../docs/validation-polish5-20261014/rejected-generation-records/tia-prompt.txt) · [edit-history.json](../../docs/validation-polish5-20261014/rejected-generation-records/tia.json) |

다르코·마르티나·나딘은 부드러운 혼합 명암을 늘리는 대신, 한정된 사각·계단형 하이라이트와 그림자 묶음을 추가하는 방향으로 편집했습니다. 모든 선이 하나의 균일한 논리 픽셀 격자라는 뜻은 아니며, 장비의 작은 윤곽에는 더 가는 선이 남습니다. 각 실험체의 `official-reference.png`는 머리색·복장·재질을 확인한 실제 공식 원화이며, 실사적 비율이나 새로운 포즈를 옮기는 기준으로 사용하지 않았습니다.

## 가넷의 머리색 외 픽셀 보존

가넷은 내장 `image_gen.imagegen`으로 기존 시트를 편집한 뒤, **선택한 머리 영역의 RGB만** FFmpeg의 이진 마스크 합성으로 기존 시트에 반영했습니다. 도구가 머리 이외의 픽셀을 미세하게 다시 그릴 가능성을 막기 위한 한정 합성입니다. 새로운 머리색은 내장 이미지 편집 출력에서 가져왔고, 다른 영역이나 알파를 다시 그리지 않았습니다.

[hair-color-proof.json](Subjects/garnet/hair-color-proof.json)에 다음 검사를 보존했습니다.

- 원본 알파 채널이 전체 이미지에서 정확히 같습니다.
- 머리 선택 영역 밖의 RGBA가 정확히 같습니다.
- 선택 영역의 RGB는 도구 편집 출력과 정확히 같습니다.
- 캐릭터가 관에 가려진 프레임 인덱스 7·8, 즉 8·9번째 프레임은 전체 RGBA가 원본과 같습니다.

가넷의 최종 `generated-sheet.png`는 이 합성 결과이므로 도구 출력 PNG와 바이트가 동일하다고 설명하지 않습니다. `hair-color-tool-output.png`는 실제 도구 출력, `hair-original-sheet.png`는 편집 전 원본입니다. 마스크·선택 영역·보존 검사도 같은 폴더에 남겼습니다. 나머지 네 명의 `generated-sheet.png`는 선택한 내장 도구 출력의 원본 바이트입니다.

## 제작 자료와 재생 시트

이번 편집은 모두 내장 `image_gen.imagegen`을 사용했습니다. 실험체별 `prompt.txt`에 최종 도구 지시문, `edit-history.json`에 입력 자료·역할·해시와 반복 편집 이력이 있습니다. `manifest.json`에는 최종 소스와 정규화 설정이 있습니다. 가넷의 한정 합성은 위 보존 검사로 별도 구분합니다.

각 애니메이션은 기존과 같이 **12프레임·8fps·1.5초 반복**입니다. 재생용 `atlas.png`는 분리한 원본 영역을 정수 좌표로 옮기고 투명 여백을 붙인 4열×3행 고정 셀 시트입니다. 프레임 정규화 과정에서 그림을 다시 그리거나 확대·축소하지 않습니다. 접지 높이와 셀 경계는 최종 설치본에서 확인합니다.

## 최종 검증

갤러리 91명 모두 재생 자료가 준비되었습니다. 이번 다섯 명·60프레임은 [최종 설치본 감사](../../docs/validation-polish5-20261014/asset-scope-audit.json)를 통과했습니다. 가넷의 머리색 한정 합성은 위 별도 검사로, 나머지 네 명의 도구 출력 바이트는 설치본의 SHA-256으로 확인합니다.

- [갤러리 브라우저 검사](../../docs/validation-polish5-20261014/gallery-browser-validation.json): 91명·1,140개 프레임 표시·12개 기능 검사 통과, 스크린샷 7개 보존, 브라우저 오류 0건입니다.
- [HTTP 자산 검사](../../docs/validation-polish5-20261014/http-assets.json): 다섯 명의 최종 원본 시트·재생 시트·GIF·WebP·공식 원화, 총 25개 파일의 로컬 바이트가 실제 서버 응답과 같습니다.
- [HTTP 프레임 복귀 검사](../../docs/validation-polish5-20261014/http-browser.json): 다섯 명 모두 1→8→1프레임 전환 후 첫 화면 스크린샷이 같았습니다. 해당 검사에서 효과 누적은 없었으며, 장면 캡처 12개를 보존했고 브라우저 오류는 0건입니다.
- [발 위치 권고와 검토](../../docs/validation-polish5-20261014/ground-recommendations.json), [발 픽셀 분석](../../docs/validation-polish5-20261014/foot-pixel-analysis.json)을 보존했습니다. 최종 HTTP 화면에서 다섯 명의 첫 프레임, 띠아의 물감 동작과 마지막 물방울, 마르티나의 실제 투명 배경을 직접 확인했습니다.

이번에 바꾸지 않은 **86명의 664개 파일**은 작업 시작 시점의 SHA-256과 비교해 변경 0건을 확인했습니다. 게임의 `Assets`, `Packages`, `ProjectSettings`도 변경 0건입니다. 게임을 다시 빌드하거나 런타임에 연결하지 않았습니다.

최종 원본·프롬프트·가넷의 합성 증거·검증 자료를 보존한 뒤 이번 임시 폴더 2곳의 156개 파일, **61,476,814바이트(약 61MB)**를 정리했습니다. [이번 정리 기록](../../docs/storage-cleanup-polish5-staging-20261014.json)에 실제 대상과 크기가 있습니다. 앞선 13명 제작의 약 134MB는 [이전 정리 기록](../../docs/storage-cleanup-chibi2-staging-20261014.json)의 수치이며 별도입니다.
