# 지도 및 치명타 장비 아이콘

모든 최종 아이콘은 `Assets/Resources/Lumia/SystemIcons/`의 투명 RGBA 64 × 64 PNG입니다. 32 × 32 논리 픽셀에서 최대 28 × 28 영역에 원본 생성 이미지를 맞춘 후, 여백을 두고 최근접 보간으로 두 배 확대했습니다.

`kiosk`, `encounter`, `subject`, `wildlife` 지도 아이콘 4개, `meteor_sword`, `light_insignia`, `ghillie_suit`, `alexandros` 치명타 장비 아이콘 4개를 추가했습니다. `empress`는 원작의 파란 원형 고리와 연꽃으로, `death_book`은 원작의 녹색 종이 부적으로 교정했습니다. 기존 생성 이력은 보존했습니다.

각 아이콘은 내장 `image_gen.imagegen` 도구로 개별 생성했습니다. 실제 프롬프트, 입력 참조, 도구 출력 경로는 `map-critical-generation/{id}.json`, 생성 원본은 `map-critical-generated/{id}.png`, 런타임 및 원본 해시는 `map-critical-asset-index.json`에 있습니다. 전체 프롬프트 세트는 `map-critical-generation-records.json`에 있습니다. 후처리 도구 `Tools/ProcessMapCriticalArt.py`는 알파 경계 자르기, 최근접 리사이즈, 투명 패딩만 수행하며 그림을 그리지 않습니다.

키오스크는 [이터널 리턴 공식 고객센터의 크레딧·키오스크 설명](https://support.playeternalreturn.com/hc/ko/articles/20584542119321)의 미니맵 C 화살표 표식을 참조했습니다. 장비 6개는 원작 아이템 아이콘의 실루엣과 색상을 참조했습니다. 조우·실험체 전투·야생동물 전투 아이콘은 프로젝트의 기존 도트 UI 색상을 참조한 새 기호이며, 원작 아이콘을 복사한 것으로 기록하지 않았습니다.

`map-critical-contact-sheet.png`에서 10개 최종 아이콘을 확인했고, `gear-silhouette-comparison.png`에서 장비 51개의 원작과 런타임을 비교했습니다. 여제와 생사부 외에 다른 종류의 물건으로 잘못 표현된 명백한 실루엣은 발견하지 않았습니다. 시스템 아트 검증은 아이콘 78개와 실험체 스프라이트 92개의 투명도·픽셀 격자·해시·개별 생성 이력을 검사했습니다.
