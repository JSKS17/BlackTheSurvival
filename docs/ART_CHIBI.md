# 데포르메 도트 아트 제작 기록

실험체 외형은 큰 머리, 작은 몸, 짧은 팔다리와 선명한 픽셀 외곽선으로 통일한다. 캐릭터별 머리 모양, 의상 색, 장신구와 대표 무기는 공식 그림을 기준으로 구분한다. 클로에의 니나 인형과 데비·마를렌 자매는 각각 하나의 게임 리소스 안에 함께 들어간다.

## 리소스 경로

| 경로 | 내용 |
|---|---|
| `Assets/Resources/Lumia/Portraits/{id}.png` | 연구원 하나와 실험체 91명의 투명 128×128 스프라이트 |
| `Assets/Resources/Lumia/SkillIcons/{id}.png` | 카드 378종의 96×96 전용 아이콘 |
| `Assets/Resources/Lumia/PassiveIcons/{id}.png` | 실험체 91명의 96×96 패시브 아이콘 |
| `docs/art-chibi/game-art-manifest.json` | 실행 게임 데이터에서 내보낸 전체 ID와 소유 실험체 명세 |
| `docs/art-chibi/generated/` | 내장 이미지 생성 도구로 제작한 원본 PNG 아틀라스 |
| `docs/art-chibi/prompts.json` | 실제 사용한 생성·교정 프롬프트와 도구 모드 |
| `docs/art-chibi/processed-art-index.json` | 원본 좌표, 출력 경로, 알파 통계와 SHA-256 기록 |

## 제작 방식

**내장 `image_gen`**으로 그림을 새로 생성하고 잘못된 무기나 소품은 같은 도구로 교정한다. CLI/API 키 경로는 사용하지 않는다. 공식 원본은 참고용이며 게임 리소스에는 새로 제작한 도트 그림이 들어간다.

캐릭터 참고 시트는 4열×2행, 스킬 참고 시트는 5열×4행이다. 실험체 스킬의 열은 T/Q/W/E/R 순서이며 마지막 스킬 시트는 3명과 빈 마지막 행으로 구성한다. 기본·무기·전술 스킬 14종은 별도 시트에 정렬한다. 원본 주소와 대응 ID는 `docs/art-chibi/reference-characters.json`, `reference-skills.json`, `reference-groups.json`, `reference-notes.md`에 기록한다. 공식 무기군과 대표 소품은 `character-props.json`에 있다.

`Tools/ProcessChibiArt.py`는 기존 PNG에서 캐릭터별 알파 영역을 분리하고, 잘라낸 원본 픽셀을 최근접 방식으로 축소한 뒤 투명 여백을 붙인다. 캐릭터 색이나 얼굴을 코드로 다시 그리지 않는다. 전역 알파 영역 분리를 사용하므로 셀 경계를 넘은 머리카락·무기도 유지하면서 이웃 캐릭터를 제외한다. 스킬 아이콘은 각 셀 전체를 비율 유지하여 축소한다.

아이콘 아틀라스에 외곽·셀 사이 여백이 있으면 `Tools/LocateSkillTiles.py`가 원래 타일의 직사각형 경계를 찾아 자르기 좌표만 기록한다. 실제 원본 픽셀과 배경색은 바꾸지 않는다. 이를 통해 인접 행의 픽셀이 아이콘 가장자리에 섞이지 않도록 한다. 좌표는 `icon-batches.json`, 검출 방식은 `icon-tile-layout.json`에 남는다.

Unity는 Point 필터, 밉맵 없음, 압축 없음, NPOT 크기 유지와 알파 투명도를 적용한다. 카드에는 큰 스킬 아이콘과 작은 소유 실험체 스프라이트를 함께 표시한다. 적 행동 예고·기술 배너·패시브 선택 및 보유 화면도 전용 아이콘을 사용한다.

## 재처리와 검증

```powershell
./Tools/ExportArtManifest.ps1
& '<Python executable>' Tools/LocateSkillTiles.py
& '<Python executable>' Tools/FinalizeChibiArt.py
& '<Python executable>' Tools/ProcessChibiArt.py
./Tools/VerifyArtAssets.ps1
./Tools/VerifyRuntime.ps1
./Tools/BuildVerification.ps1
```

아트 검증은 ID 전체 일치, 누락·중복 이미지, 잘린 경계, 투명도, 원본·출력 파일 해시를 검사한다. 개발 빌드의 `-lumia-art-verify`는 모든 그림을 실제 Unity 리소스로 불러와 8개 스프라이트 페이지와 12개 아이콘 페이지를 캡처한다. `-lumia-verify`는 실제 플레이 화면을 캡처한다. 검증은 일반 플레이 저장 슬롯을 변경하지 않는다.

이터널 리턴의 IP·실험체·원본 스킬과 아이템의 권리는 님블뉴런에 귀속된다. 새 그림은 비공식 팬 게임을 위한 시각적 재해석이다. 한글 글꼴은 기존 Galmuri11과 OFL 라이선스를 유지한다.
