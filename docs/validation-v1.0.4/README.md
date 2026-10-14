# v1.0.4 로비 BGM 검증

로비의 Summertime을 이터널 리턴 OST **Eternal World (Instrumental)**의 정식 무보컬 녹음으로 교체했다. [음원 출처·변환 기록](../audio/ETERNAL_WORLD.md)에 공개 배포 페이지와 원본·게임용 파일 해시를 보존한다.

## 파일과 라우팅

- OGG 전체 디코딩: 216.169773초, 44,100 Hz 스테레오, 피크 0.88741404, 클리핑 샘플 0.
- 카탈로그 감사: 음원 427개, 카드 라우팅 392개, 15,885개 검사 통과. [카탈로그 감사](catalog-audit.json)
- 기존 음원 레코드와 모든 카드·효과음·다른 장면의 연결은 동일하며 `music.lobby`만 `music_eternal_world`로 변경됐다. [라우팅 비교](lobby-routing.json)
- 음원 재확보 및 카탈로그 재생성 도구는 새 음악 항목을 보존한다.

## Windows 실제 재생

Unity 6000.3.11f1 Windows 개발 빌드에서 기존 `-bts-audio-verify` 검증을 실행해 **3,131개 검사**를 통과했다. [전체 측정](native-audio.json), [요약](native-summary.json)

- 실제 로비가 `music_eternal_world`를 선택하고 Unity AudioListener 믹서에서 RMS 0.041953734를 측정했다.
- Unity가 가져온 파일은 Streaming 방식, 길이 216.169769초다.
- 음악 전환·반복 재생 유지·음량 조절·음소거·일시정지·설정 복원과 모든 카드 라우팅 검사가 통과했다.
- 기존 원정·연구 저장 파일 4개의 전후 해시가 일치하며 일반 환경 설정도 유지됐다.

이 측정은 운영체제 오디오 출력 전의 실제 게임 믹서 결과다.

## 배포 빌드

[배포 빌드](release-build.json), [시작 검사](release-startup.json), [Windows ZIP 파일·SHA-256 검사](windows-package.json)에 결과를 기록한다. ZIP은 실행 파일과 모든 런타임 데이터를 포함한다.

임시 개발 빌드와 캐시는 검증 후 정리하고, 이전 정식 배포본은 GitHub Releases에 보존한다. [빌드 공간 확보](build-space.json), [최종 캐시 정리](storage-cleanup.json)
