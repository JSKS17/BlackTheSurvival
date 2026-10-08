# 시스템 아이콘 제작

2026-10-09에 기존 이터널 리턴의 시각적 형태와 색을 참고해 `Black The Survival`용 도트 아이콘 52개를 새로 제작했다. 기존 아이콘을 런타임에 그대로 복사하지 않고 기본 제공 `image_gen` 도구로 각 자산을 개별 생성했다.

| 종류 | 개수 | ID / 자료 |
|---|---:|---|
| 메인·보조 룬 | 16 | `rune-manifest.json` |
| 전설·초월 장비 | 29 | `item-manifest.json` |
| 오브젝트 | 5 | `item-manifest.json` |
| 크레딧·모닥불 | 2 | `misc-manifest.json` |

최종 PNG는 `Assets/Resources/Lumia/SystemIcons/{id}.png`에 저장했다. 64×64 RGBA이며, 그림 전체를 28×28 논리 픽셀로 최근접 축소한 뒤 32×32 투명 캔버스의 중앙에 놓고 최근접 2배 확대했다. 외곽 2 논리 픽셀의 여백으로 UI 배치 시 그림이 경계에 닿지 않게 했다. 생성된 알파는 보존하며 배경 제거, 색 양자화, 수작업 재색칠은 하지 않았다. Unity에서는 Point 필터로 표시한다.

`generated/{id}.png`는 수정하지 않은 생성 원본이며 `generation/{id}.json`은 정확한 프롬프트, 기본 제공 도구 사용 정보, 처음 저장된 원본 경로를 기록한다. `asset-index.json`은 최종/원본 SHA-256과 해상도·알파 통계를 기록한다. 원본 참조 이미지는 `references/`에 따로 보존했다. `system-contact-sheet.png`에 전체 결과를 모았다.

## 원본 시각 자료

- [공식 1.0 아이템 소개](https://playeternalreturn.com/posts/news/1282?hl=ko-KR)와 [공식 특성 소개](https://playeternalreturn.com/posts/news/1283?hl=ko-KR)를 명칭과 계열 근거로 사용했다.
- [공개 아이템 데이터](https://er.dakgg.io/api/v1/data/items?hl=ko)의 `name`, `id`, `imageUrl`로 등록 장비 29개와 오브젝트 5개의 정확한 원본 이미지를 연결했다. `imageUrl`은 이터널 리턴 12.5.0 게임 자산을 공개 CDN에 호스팅한 것이다.
- [공개 특성 데이터](https://er.dakgg.io/api/v1/data/trait-skills?hl=ko)의 `name`, `id`, `imageUrl`로 룬 16개의 정확한 원본 특성 이미지를 연결했다. 해당 엔드포인트는 [공개 캐릭터 특성 페이지](https://dak.gg/er/characters/Sissela/augments?hl=ko)의 실제 JavaScript 번들에서 확인했다. 이름이 같은 대체 그림을 임의로 사용하지 않았다.
- 크레딧은 [공식 1.36 안내](https://playeternalreturn.com/posts/news/2364?hl=ko-KR)의 크레딧+ 그림을 참고했다. 녹색 육각 토큰, 금색 테두리와 C 문양을 유지했다.
- 모닥불은 [공식 Steam 뉴스 API](https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=1049590&count=50&maxlength=0&enddate=1685600000)의 3rd Dev Journal에서 제공한 간이 모닥불 설치물 및 게임플레이 그림을 참고했다. 어두운 장작이 원뿔처럼 둘러지는 형태와 주황 불꽃을 유지했다.

이터널 리턴의 이름, 세계관과 원본 게임 시각 자료의 권리는 님블뉴런에 귀속된다. 이 아이콘들은 비공식 팬 게임의 도트 표현을 위한 새 그림이다.

## 재현

`Tools/PrepareSystemArt.py`는 공개 데이터에서 참조 이미지와 ID 목록을 준비한다. `Tools/ProcessSystemArt.py`는 생성 원본을 프로젝트 안에 보존하고 최종 PNG, 확인표, 해시 목록을 출력한다. 생성 호출을 재실행하는 CLI나 API 키 방식은 사용하지 않았다. 각 그림을 다시 생성하려면 저장된 프롬프트와 해당 참조 이미지로 기본 제공 이미지 생성 도구를 사용한다.
