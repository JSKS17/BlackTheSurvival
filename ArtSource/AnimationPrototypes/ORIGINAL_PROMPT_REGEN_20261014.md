# 4명 필살기 시안 최초 프롬프트 재제작 · 2026-10-14

**마르티나·띠아·다르코·나딘의 직전 애니메이션은 폐기하고, 최초 제작 프롬프트와 참고 이미지로 돌아가 새로 생성했습니다.** 기존 시트를 다시 편집한 결과가 아닙니다. 가넷의 머리색 보정과 나머지 실험체는 이번 교체 대상에서 제외하며, 게임에는 적용하지 않습니다.

[새 로컬 갤러리](http://127.0.0.1:61624/index.html?revision=original-prompt4-20261014-final#martina) · [최초 프롬프트 출처](../../docs/validation-original-prompt4-20261014/original-prompt-provenance.json)

## 처음 제작 조건으로 복원

최초 컬렉션을 추가한 Git 커밋 `41ab261d3e12c6d8079292a03b73000750afcc53`에서 네 명의 원문을 추출했습니다. 다르코·마르티나·띠아는 당시 `Subjects/{id}/prompt.txt`이며, **나딘은 당시 최초 시안인 `Subjects/nadine/prompt-first-pass.txt`**를 사용했습니다. 나딘의 대응 제작 기록도 `manifest-first-pass.json`입니다. 출처 기록에는 실제 Git 경로·blob·바이트 수·SHA-256을 남깁니다.

각 폴더의 `original-prompt.txt`는 추출한 최초 원문이고, `prompt.txt`는 이번 내장 도구에 전달한 실제 지시문입니다. 원문을 유지한 뒤 안경·눈 색·머리와 의상·무기처럼 앞선 문장에 잘못 적힌 캐릭터 정보만 마지막에 정정했습니다. 최근의 엄격한 2등신 수치, 추가 픽셀 격자와 별도 스타일 마스터, 반복 디테일 편집 지시는 사용하지 않았습니다.

생성 입력은 최초와 같은 두 이미지뿐입니다.

- `Assets/Resources/Lumia/PortraitsReference/{id}.png`: 기존 128px 인게임 스프라이트로, 캐릭터 정체성을 참고합니다. 공식 원화라는 뜻은 아닙니다.
- [처음 승인한 니아 R 시트](Nia/nia-ultimate-atlas.png): 큰 머리·짧은 팔다리, 사각 픽셀군·짙은 윤곽·제한된 명암의 스타일을 참고합니다. 니아의 얼굴이나 복장을 다른 실험체에 복사하지 않습니다.

직전의 부분 보정 시트, 폐기한 재제작 시트와 2등신 스타일 마스터는 생성 입력에 넣지 않았습니다. 공식 원화는 잘못된 캐릭터 정보를 확인하는 용도였으며, 새 생성의 참고 이미지로 추가하지 않았습니다. 네 명 모두 내장 `image_gen.imagegen`으로 **처음부터 한 번씩 생성한 결과**를 선택했습니다.

## 캐릭터와 동작

| 실험체 | 최초 동작 구성과 최소 정정 | 이번 생성 자료 |
|---|---|---|
| 다르코 | 손 뻗기→붙잡기→끌기→들어올리기→지면에 내리꽂기→복귀를 유지합니다. 잘못 적힌 안경을 없애고 회청색 머리, 무늬 코트와 금색 장식·어깨에 멘 무기, 청록·금색 효과를 기준으로 정정했습니다. | [최초 원문](Subjects/darko/original-prompt.txt) · [실제 프롬프트](Subjects/darko/prompt.txt) · [제작 기록](Subjects/darko/generation.json) |
| 마르티나 | 카메라 들기→조준→촬영·좌우 팬→렌즈 조절→마지막 촬영광→내리기 순서를 유지합니다. 안경 대신 한쪽의 불투명 안대와 보이는 청안, 겨자색 코트·청록 청바지를 정정하고 렌즈 앞뒤가 바뀌지 않게 했습니다. | [최초 원문](Subjects/martina/original-prompt.txt) · [실제 프롬프트](Subjects/martina/prompt.txt) · [제작 기록](Subjects/martina/generation.json) |
| 나딘 | 세 차례 발사→세 번째 발사 뒤 작은 늑대 돌진→무기 회수·늑대 소멸을 유지합니다. 갈색 피부와 짙은 포니테일·주황 사냥 복장, 기존 스프라이트의 석궁 형태를 정정했습니다. | [최초 원문](Subjects/nadine/original-prompt.txt) · [실제 프롬프트](Subjects/nadine/prompt.txt) · [제작 기록](Subjects/nadine/generation.json) |
| 띠아 | 붓 들기→큰 붓 소환→휘두르며 RGB 곡선 그리기→물감 타격→남은 방울→복귀를 유지합니다. 베레모·금색 땋은 머리 대신 연갈색 옆머리 번과 노란 리본, 호박색 눈·갈색 작가 복장으로 정정했습니다. 마지막 물방울은 앞선 물감 흐름에서 이어지게 했습니다. | [최초 원문](Subjects/tia/original-prompt.txt) · [실제 프롬프트](Subjects/tia/prompt.txt) · [제작 기록](Subjects/tia/generation.json) |

## 파일과 재생 시트

선택한 도구 출력은 `generated-sheet.png`로 원본 바이트와 RGBA를 보존합니다. 재생용 `atlas.png`는 분리한 원본 영역을 정수 좌표로 옮기고 투명 패딩을 붙여 조립합니다. 그림을 다시 그리거나 확대·축소하지 않으며, 각 프레임에 최소 24px 투명 여백을 두는 규칙을 적용합니다. **4열×3행·12프레임·8fps·1.5초 반복**은 기존과 같습니다.

원본과 재생 시트의 SHA-256, 겹치지 않는 분할과 픽셀 보존, 접지 높이·셀 경계·재생 검사는 `manifest.json`, `normalization.json`, `validation.json`과 새 검증 폴더에 기록합니다. 생성 시트의 아주 옅은 알파 흔적과 실제로 보이는 외곽 잘림은 구분해 검사합니다.

## 최종 검증

네 명의 새 시안을 설치하고 갤러리를 갱신했습니다. 다음 검사는 모두 새 제작본의 결과이며, [이번 검증 폴더](../../docs/validation-original-prompt4-20261014/)에 증거를 보존했습니다.

- [설치 자산 검사](../../docs/validation-original-prompt4-20261014/asset-audit.json): 네 명·48프레임의 최초 Git 원문과 실제 프롬프트, 도구 출력 바이트·RGBA 보존, 최소 24px 투명 여백과 보이는 외곽 잘림 없음에 통과했습니다. 다르코의 발이 가려진 한 장면은 일반 접지 정렬에서 제외합니다.
- [갤러리 브라우저 검사](../../docs/validation-original-prompt4-20261014/gallery-browser-validation.json): 91명·1,140개 프레임 표시·12개 기능 검사에 통과했습니다. 스크린샷 7개를 보존했고 브라우저 오류는 0건입니다.
- [HTTP 자산 검사](../../docs/validation-original-prompt4-20261014/http-assets.json): 실제 서버 응답으로 24개 자산의 로컬 바이트 일치를 확인했습니다.
- [HTTP 프레임 복귀 검사](../../docs/validation-original-prompt4-20261014/http-browser.json): 네 명 모두 1→8→1프레임 전환 뒤 첫 화면이 동일했습니다. 해당 검사에서 효과 누적이 없었으며, 장면 캡처 12개를 보존했고 브라우저 오류는 0건입니다.
- [발 위치·프레임 분리 검토](../../docs/validation-original-prompt4-20261014/ground-review.json)와 [변경 범위 검사](../../docs/validation-original-prompt4-20261014/scope-verification.json)를 보존했습니다. 작업 시작 시점의 해시와 비교해 나머지 **87명의 682개 파일**, 게임 폴더 **3,268개 파일**의 변경은 0건입니다. 게임 빌드와 런타임 연결은 하지 않았습니다.

[13명 재제작 기록](REDRAW_NOTES_20261014.md)과 [5명 부분 보정 기록](POLISH_NOTES_20261014.md)의 검증 수치는 각 이전 시점의 결과입니다. 그 문서의 수치를 이번 새 제작본에 대한 검사로 사용하지 않습니다. 이번에 폐기한 네 명의 이전 프롬프트·편집 메타데이터만 작은 역사 기록으로 보존하며, 이전 PNG를 새 생성에 참조하거나 프로젝트에 계속 보관하지 않습니다.

최종 제작 자료와 검증 결과를 보존한 뒤 중복 생성·조립 임시 파일 125개, 39,671,531바이트를 정리했습니다. [이번 임시 파일 정리 기록](../../docs/storage-cleanup-original-prompt4-20261014.json)에 삭제 범위를 기록했습니다.
