# 최종 얼굴·설명·장비 검증

2026-10-09, Unity 6000.3.11f1 Windows.

- `core-tests.log`: 44개 시나리오, 17,065개 단언 통과.
- `description-audit.log`: 카드·패시브·룬·장비 설명 8,441개 단언 통과.
- `balance.log`: 고정 전략 40개 시드, 탈출 13·패배 27·정체 0.
- `native-screens.json`: 실제 개발 플레이어 게임 화면 57개, 아트 23페이지, 빈 프레임·실행 오류 0.
- `native-ui/`: 요약/전체, 시작 선택 고정, 양측 필드, 상대 장비·D/F, 알렉스 D, 장비 상세를 포함한다.
- `native-art/`: 스프라이트 92개, 기술·패시브 아이콘 483개, 시스템 아이콘 70개, 누락 0.
- `development-build.log`, `release-build.log`: 최종 빌드 성공, 오류 0.
- `release-hashes.json`: 배포 출력과 설치 파일 4개의 SHA-256 일치.
- `release-startup.json`: 설치된 게임 창·응답·시작 오류·개인 이어하기 저장 보존 확인.

자세한 결과는 [전체 검증 기록](../VALIDATION.md)의 최신 절에 있다. 테스트 캡처에는 재현을 위한 전용 상태를 사용하며 실제 전투의 체력 수치를 나타내지 않는다.
