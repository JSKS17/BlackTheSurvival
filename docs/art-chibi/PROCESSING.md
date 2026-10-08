# 생성 도트 아트 분리와 검증

게임 명세 `game-art-manifest.json`은 실험체 91명과 하나의 스프라이트 92개, 카드 아이콘 378개, 패시브 아이콘 91개를 식별자로 관리합니다. 그림은 내장 이미지 생성 도구로 새로 만든 결과만 사용합니다. 원작 실험체와 스킬 자료는 디자인 참고용이며 원작 아이콘 이미지가 게임 에셋으로 복사되지 않습니다.

`Tools/ProcessChibiArt.py`는 생성된 투명 아틀라스를 캐릭터별로 분리합니다. 기본 `islands` 모드는 전역 알파 영역의 몸체를 찾고 해당 셀의 캐릭터에 할당합니다. 각 영역의 바깥쪽 6픽셀을 함께 추출해 원본 가장자리 알파를 보존하며, 다른 캐릭터에 속한 몸체 픽셀은 그 캐릭터의 에셋에만 할당합니다. 분리 후 유지되는 모든 픽셀의 RGBA 값은 원본 그대로입니다. 이미지를 도색하거나 색·알파 값을 양자화하지 않으며, 원본 이미지는 보존합니다.

셀을 넘는 무기와 발도 같은 몸체 영역에 속하면 온전히 추출됩니다. 데비&마를렌처럼 같은 셀에 몸체 두 개가 있는 실험체는 하나의 에셋으로 묶습니다. 완전히 떨어진 작은 소품은 가까운 몸체에 할당합니다. 캐릭터 바깥의 먼 낮은 알파 흔적은 크롭 범위에 포함하지 않아 실제 몸체가 작아지는 문제를 피합니다. 그림 형태를 수정하거나 잘린 몸체를 복원해야 하는 경우에는 이미지 생성 도구로 다시 제작합니다.

## 입력

원본 PNG를 `docs/art-chibi/generated/`에 보존한 뒤 `batches.json`에 기록합니다. 경로는 프로젝트 루트를 기준으로 해석합니다. 캐릭터 아틀라스는 4열×2행, 혼합 스킬 아틀라스는 5열×4행(T,Q,W,E,R 순), 기타 카드 아틀라스는 4열×4행을 사용할 수 있습니다. 실제 생성 이미지의 크기가 달라도 행과 열 비율로 셀 경계를 계산합니다.

```json
{
  "schema": 1,
  "batches": [
    {
      "id": "portraits-01",
      "kind": "portrait",
      "source": "docs/art-chibi/generated/portraits-01.png",
      "columns": 4,
      "rows": 2,
      "ids": ["hana", "nia", "jackie", "aya", "hyunwoo", "yuki", "hyejin", "sua"]
    },
    {
      "id": "skills-01",
      "kind": "skill",
      "source": "docs/art-chibi/generated/skills-01.png",
      "columns": 5,
      "rows": 4,
      "entries": [
        {"id": "nia_passive", "kind": "passive"},
        {"id": "nia_q", "kind": "icon"},
        {"id": "nia_w", "kind": "icon"},
        {"id": "nia_e", "kind": "icon"},
        {"id": "nia_r", "kind": "icon"},
        null
      ]
    }
  ]
}
```

위 식별자는 형식 예시입니다. 실제 패시브 ID는 게임 명세의 `passives` 목록에서 확인합니다. `ids`와 `entries`의 `null` 슬롯은 행·열 순서를 유지하면서 건너뜁니다. 혼합 시트는 각 항목에 `kind`를 지정합니다. 동일 종류만 있는 시트는 배치의 `kind`를 `icon` 또는 `passive`로 지정할 수 있습니다.

균일 셀 분리가 필요한 시트는 `portrait_mode`를 `grid`로 지정하거나 `--portrait-mode grid`로 실행합니다. 기본 영역 분리가 실제 의도와 맞지 않는 경우에는 생성 이미지를 확인하고 필요한 시트만 다시 생성합니다. 정확한 크롭 좌표를 지정하려면 `grid` 모드에서 `cells`를 사용합니다.

```json
"cells": [{"id": "hana", "column": 0, "row": 0, "box": [10, 8, 380, 506]}]
```

## 실행

```powershell
python Tools/ProcessChibiArt.py
./Tools/VerifyArtAssets.ps1
```

특정 생성 배치만 처리하려면 `--batch portraits-01`을 지정합니다. 여러 배치 ID를 지정할 때는 `--batch`를 반복합니다. 기존 인덱스의 다른 배치는 보존됩니다. 생성 중인 상태에서는 `VerifyArtAssets.ps1 -AllowIncomplete`로 누락 목록과 진행률을 확인할 수 있으나 최종 검증에서는 이 옵션을 사용하지 않습니다.

## 출력

| 종류 | 경로 | 기본 크기 |
| --- | --- | --- |
| 실험체와 하나 | `Assets/Resources/Lumia/Portraits/{id}.png` | 128×128 RGBA |
| Q·W·E·R·기본·무기·전술 카드 | `Assets/Resources/Lumia/SkillIcons/{id}.png` | 96×96 RGBA |
| 패시브 T | `Assets/Resources/Lumia/PassiveIcons/{id}.png` | 96×96 RGBA |

캐릭터는 알파 경계로 잘라 8픽셀 여백 안에 비율을 유지해 배치하고, 가장 아래에 보이는 발이나 소품의 위치를 통일합니다. 아이콘은 생성된 어두운 배경을 보존하며 정사각형이 아닌 셀은 비율을 유지해 가운데에 배치합니다. 여백 색은 원본 모서리 색을 사용합니다. 모든 크기 조정은 최근접 샘플링입니다.

`processed-art-index.json`에는 원본 이미지 SHA-256, 행·열, 크롭 박스, 원본 알파 통계, 출력 파일과 픽셀 SHA-256을 기록합니다. `portrait-contact-sheet.png`, `icon-contact-sheet.png`, `passive-contact-sheet.png`는 전체 아트의 시각 점검용 모음입니다. 라벨은 점검용 이미지에만 들어갑니다.

## 검증 기준

`Tools/VerifyArtAssets.py`는 명세에 있는 모든 ID가 출력·인덱스에 존재하는지, PNG 크기와 RGBA 형식, 투명 스프라이트 배경과 충분한 크기, 경계 여백, 빈 이미지, 중복 픽셀, 중복 셀 매핑, 원본 보존 및 SHA-256 일치를 검사합니다. 결과는 `art-validation.json`으로 저장하며 하나라도 실패하면 종료 코드가 1입니다.

추출한 원본 영역 가장자리의 불투명 픽셀은 실제 캔버스 밖 잘림이나 인접 캐릭터 혼입 가능성을 알려줍니다. 큰 알파 영역 두 개가 나란히 떨어져 있는 경우에도 경고합니다. 몸체와 무기가 떨어져 있거나 쌍둥이 실험체일 수 있으므로 경고가 있는 셀은 반드시 원본 및 접촉 시트를 시각적으로 확인합니다. `--allow-border-contact`는 검토용 처리를 허용할 뿐 최종 검증을 통과시키지 않습니다. 배경이나 아트 형태의 수정이 필요하면 이미지 생성 도구로 다시 생성합니다.

픽셀과 파일의 기계적 검증은 캐릭터 디자인의 정확성이나 아이콘의 의미까지 판정하지 않습니다. 의상·헤어·무기와 스킬 모티프가 원작 참고와 일치하는지는 생성 배치와 최종 접촉 시트를 시각적으로 함께 확인해야 합니다.
