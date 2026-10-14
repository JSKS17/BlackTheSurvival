# 보컬 없는 원정 BGM

v1.0.4부터 로비는 `Eternal World (Instrumental)`의 정식 무보컬 녹음을 사용한다. 준비 화면부터 탈출·실패 화면까지는 보컬이 없는 프로젝트 제작 연주곡을 사용한다. 프로젝트 제작곡과 원본 녹음의 출처를 별도로 기록한다. 기존 캐릭터의 스킬·상황 대사는 효과음 및 보이스 설정에 따라 별도로 재생된다.

| 화면 | 곡 | 구성 |
| --- | --- | --- |
| 로비 | Eternal World (Instrumental) | 님블뉴런 정식 배포 녹음, 무보컬, 약 3분 36초 |
| 준비·지도·조우·실패 | Lumia Afterglow | 96 BPM, C 단조, 48마디·120초. 벨 아르페지오·일렉트릭 피아노·패드·플루트·베이스·절제된 리듬 |
| 모닥불·탈출 | Emberlight | 78 BPM, D 장조, 32마디·약 98초. 핑거 피킹 계열 플럭·플루트·피아노·패드·가벼운 타악기 |
| 키오스크 | Signal Exchange | 110 BPM, A 단조, 48마디·약 105초. 싱코페이션 피아노·말렛·플럭·움직이는 베이스·가벼운 리듬 |
| 야생동물·실험체 전투 | Break the Loop | 128 BPM, E 단조, 64마디·120초. 아날로그 리드·플럭 아르페지오·스트링·베이스·전자 드럼 |
| 보스전 | VF Overdrive | 144 BPM, D 단조, 64마디·약 107초. 낮은 베이스·스트링 오스티나토·리드·옥타브 변주·전자 드럼 |

각 연주곡에는 도입·A·B·마무리 전개가 있고 긴 곡에는 중간의 악기를 줄인 구간을 둔다. 단순한 4마디 알림음의 반복으로 구성하지 않는다. 멜로디·화성·베이스·타악기 역할을 별도로 작성했으며, 악기 소리는 녹음 샘플 없이 DSP로 합성한다. 기존 OST의 멜로디나 녹음을 사용하지 않는다.

[제작 도구](../../Tools/GenerateInstrumentalMusic.py)는 NumPy와 FFmpeg/libvorbis를 사용한다. Resources에는 압축 OGG만 저장하며 곡별 길이·템포·음량·압축 후 피크·해시·출처를 [음악 목록](music-sources.json)에 기록한다. 제작 도구는 실제 OGG를 다시 디코딩하여 샘플 길이·무음·클리핑·루프 시작과 끝을 검사한다. 순환하는 악기 잔향과 스테레오 반사음을 적용하고 경계의 4ms를 부드럽게 처리한다. 게임에서는 기존의 음악 음량 설정과 장면 간 0.9초 전환을 그대로 사용한다.

`Tools/AssembleAudioCatalog.py`는 로비 이외의 9개 음악 경로에서 `containsVocals: false`가 명시된 파일만 허용한다. 새 연주곡의 `kind`와 `origin`은 `instrumental_generated`이며 원본 음악·보이스·보완 효과음과 구분한다. 보컬 OST 3곡의 파일은 기존 출처 기록을 유지하지만 원정 경로에서는 재생하지 않는다.

## 원본 MR 확보 여부

2026년 10월 11일 다음 공식 출처를 확인했다.

- [Red Heart 공식 이벤트](https://event.playeternalreturn.com/S10/OST/RedHeart?hl=ko-KR), [Golden Willow 공식 이벤트](https://event.playeternalreturn.com/S9/GoldenWillow?hl=ko-KR), [Summertime 공식 이벤트](https://event.playeternalreturn.com/S8/SummertimeMelody?hl=ko-KR): MR 다운로드 버튼은 플랫폼 로그인 안내로 연결된다. 로그인이나 접근 제한을 우회하지 않았다.
- [Fearless 공식 커버 이벤트 공지](https://playeternalreturn.com/posts/news/2619?hl=ko-KR): 공지에 공개된 MR Google Drive 폴더는 확인 당시 일반 페이지와 공개 목록 모두 HTTP 404였다.
- [공식 팬키트](https://drive.google.com/drive/folders/1bgW32L09YPpRgQKtH4C_TAd3Kr0N9Y90): 추가 콘텐츠·행사 폴더에 공개 MR 파일이 없었다.
- [공식 Steam 사운드트랙](https://store.steampowered.com/app/1300410): instrumental 수록은 확인했으나 유료 상품이므로 구매·설치를 실행하지 않았다.

이 때문에 공개 원본 MR을 확보했다고 표시하지 않고 새 무보컬 연주곡으로 원정 BGM을 보완했다. 원본 음악·캐릭터·IP의 권리는 님블뉴런 및 해당 권리자에게 있다.

2026년 10월 14일에는 정식 유통으로 공개된 **Eternal World (Instrumental)**을 확보해 v1.0.4의 로비에 적용했다. [별도 확보·변환 기록](ETERNAL_WORLD.md)을 참고한다.
