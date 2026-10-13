# R 도트 애니메이션 운영

승인된 91명 실험체의 R 애니메이션을 전투 화면에 표시합니다. 하나가 다른 실험체의 R 카드를 사용해도 해당 카드 주인의 애니메이션이 재생되며, 상대 실험체의 직접 R 사용도 같은 경로를 따릅니다. 패시브·장비·지속 피해 같은 후속 효과에는 R 연출을 중복으로 붙이지 않습니다.

## 표시와 전투 진행

- 기존 카드 확대 표시가 먼저 나오고, 그 표시가 끝나면 R 애니메이션을 재생합니다. 일반 설정은 카드 표시 0.32초와 12프레임·8fps의 애니메이션 1.5초로 구성됩니다.
- R 영역은 1280×720 기준 `(420, 81, 440, 357)`입니다. 손패 영역은 y=485부터 시작하고, 양쪽 필드 정보는 R 영역 바깥에 있습니다. 애니메이션을 보여 주는 동안 중앙의 적 행동·코스트·보스 패널을 가립니다.
- 이미지가 들어가는 영역은 `(432, 110, 416, 316)`이며, 원래 미리보기의 정사각형 캔버스와 프레임별 오프셋을 유지해 맞춥니다. 텍스처의 해당 프레임을 매번 새로 그리므로 이전 프레임을 누적하지 않습니다. 이미지 영역 밖은 GUI 클리핑으로 가립니다.
- 피해와 카드 비용은 기존 `GameEngine` 경로에서 즉시 계산합니다. 연출은 전투 수치나 난수를 바꾸지 않습니다. R 재생이 끝날 때까지 다음 카드와 턴 종료 입력, 상대의 다음 행동을 기다립니다.
- R로 전투가 끝나도 마지막 연출이 끝날 때까지 전투 화면을 유지하고, 그 뒤 승리 보상이나 패배 화면을 표시합니다. 이때 전투 화면의 조작은 비활성화합니다.
- 설정·상세 정보·인벤토리 등 전투 위의 창을 열면 재생 시간을 일시 정지하고, 닫으면 같은 시점에서 이어집니다. 새 전투·새 원정·로드·로비·스토리·튜토리얼로 전환하면 남아 있는 연출을 정리합니다.
- 동작 줄이기 설정에서는 이동·확대 대신 짧은 카드 표시와 R의 여덟 번째 프레임을 정적으로 표시합니다. 재생 도중 설정을 바꾸면 다음 카드 사용부터 반영해 현재 연출의 길이와 프레임을 유지합니다.

## 자산과 좌표 규약

원본은 `ArtSource/AnimationPrototypes/catalog.json`과 그 카탈로그의 승인된 atlas PNG입니다. `Tools/ExportUltimateAnimations.py`가 PNG를 바이트 그대로 `Assets/Resources/Lumia/UltimateAnimations/{id}.png`로 복사합니다. PNG를 다시 그리거나 크기를 바꾸거나 재압축하지 않습니다. GIF·WebP·생성 원본·썸네일·원작 참고 이미지는 런타임 폴더에 포함하지 않습니다.

Unity의 `JsonUtility`로 읽는 런타임 카탈로그는 `Assets/Resources/Lumia/UltimateAnimations/catalog.json`입니다.

```json
{
  "items": [
    {
      "id": "nia",
      "name": "니아",
      "resource": "Lumia/UltimateAnimations/nia",
      "atlasWidth": 1448,
      "atlasHeight": 1086,
      "canvasWidth": 480,
      "canvasHeight": 480,
      "framesPerSecond": 8,
      "frameCount": 12,
      "frames": [
        { "x": 0, "y": 0, "width": 362, "height": 362, "offsetX": 3, "offsetY": 46 }
      ]
    }
  ]
}
```

위 예시는 첫 프레임만 보여 줍니다. 실제 데이터에는 각 실험체별 12프레임이 있습니다. `x/y`는 atlas 왼쪽 위에서 잰 픽셀 좌표이고, `width/height`는 해당 프레임의 잘라 볼 영역입니다. `offsetX/offsetY`는 표시 캔버스 왼쪽 위에서 잰 배치 좌표입니다. Unity UV의 y축 변환은 `UltimateAnimations.FrameUV`에서 한 번만 수행합니다. 승인된 기존 atlas 일부는 프레임 경계가 불규칙하므로 단순한 4×3 균등 칸 계산으로 대체하면 안 됩니다.

텍스처 설정은 Default Texture2D, Point, Clamp, 밉맵 없음, NPOT 크기 유지, 입력 알파, 무압축, 최대 4096, Read/Write 비활성화입니다. 현재 최대 atlas 변은 2088px이므로 import 시 축소되지 않습니다. `LumiaProjectSetup.PixelImportRules`의 공통 설정과 일치합니다.

`UltimateAnimations`는 사용하는 순간에 텍스처를 읽고 최대 3개만 유지합니다. 오래된 텍스처는 `Resources.UnloadAsset`으로 해제하고, 게임 객체가 파괴되면 남은 캐시도 해제합니다. 91개 전체를 한 번에 GPU 메모리에 올리지 않습니다. 현재 atlas의 최댓값을 기준으로 3개 텍스처의 RGBA32 합계는 약 37.4MiB 이하입니다. PNG 파일 합계는 163,621,225바이트입니다.

등록되지 않은 미래의 R 카드나 손상된 카탈로그 항목은 기존 기술 효과 경로로 돌아갑니다. Q/W/E, 무기 D, 전술 F, 일반 공격의 연출은 기존 경로를 사용합니다.

## 재생 관련 코드

- `UltimateAnimations.cs`: 카탈로그 읽기, 카드 주인 연결, 텍스처 캐시, 프레임 좌표·UV·실제 그리기.
- `CombatEffects.cs`: 직접 사용 카드의 R 판정, 카드 표시와 R의 순서, 재생 시간, 다음 행동 대기, 일시 정지.
- `LumiaUltimatePresentation.cs`: 전투/화면 전환 때 초기화, 창을 열 때 일시 정지, 마지막 R 이후 결과 화면으로 넘기기.
- `LumiaGame.cs`: 실제 플레이어·상대 액션 수집과 전투 화면 연결.

## 검증과 재내보내기

프로젝트 루트에서 다음 명령으로 승인 원본과 런타임 파일의 SHA-256, 91개 ID, 1092프레임, 좌표 범위, import 메타를 검사합니다. `--check`는 파일을 수정하지 않습니다.

```powershell
python Tools/ExportUltimateAnimations.py --check
```

승인 atlas나 갤러리 프레임 배치가 변경된 경우에는 다음 명령으로 런타임 자산을 갱신한 뒤 Unity에서 다시 import합니다.

```powershell
python Tools/ExportUltimateAnimations.py
```

원본과 복사본의 SHA 감사 기록은 `docs/runtime-ultimate-assets.json`에 있습니다. 이 파일은 이미지 복사와 데이터 검사의 결과이며, 실제 플레이어 실행 결과를 대신하지 않습니다.

Unity 메뉴 `Black The Survival/Validate ultimate animation assets` 또는 batch 실행 메서드 `Lumia.Editor.UltimateAnimationAssetValidation.Validate`는 `Resources`를 통해 실제 import된 91개 텍스처를 순서대로 읽고 검사합니다. 이 검사는 실제 크기·필터·알파·밉맵·1092프레임 범위를 확인하고 텍스처를 바로 해제합니다. 결과 파일은 `docs/runtime-ultimate-import.json`입니다. Windows 빌드에서도 import 설정 적용 후 이 검사를 호출합니다.

Development Player의 `-bts-ultimate-verify` 옵션은 개인 저장 파일과 분리된 검증 슬롯을 사용해 91개 플레이어/상대 R 경로, 프레임 표시, 손패와의 분리, 카드 표시 순서, 승리·패배 마지막 R, 가넷 무료 재사용 처형, 동작 줄이기, 창 일시 정지·재개, 로비 초기화와 화면 비율을 검사하도록 구성되어 있습니다. 이 실행의 결과와 실제 캡처는 `UltimateVerification/coverage.json` 및 같은 폴더에 생성됩니다. 네이티브 실행 통과 여부는 해당 결과 파일을 확인해야 합니다.
