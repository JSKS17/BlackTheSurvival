# 짧은 미리보기와 상태 검증

2026-10-09, Unity 6000.3.11f1 Windows.

- `core-tests.log`: 45개 시나리오, 17,528개 단언 통과.
- `description-audit.log`: 짧은 미리보기와 전체 효과 설명 16,400개 단언 통과.
- `state-description-audit.log`: 상태를 수치로 표현하는 문구와 이진 상태 배율 사용 없음.
- `preview-layout.json`: 실제 Galmuri11 글꼴, 392개 카드 × 6개 크기 × 2개 수치 조건, 4,704개 측정 모두 본문 안에 들어감.
- `native-screens.json`, `native-ui/`: 게임 화면 62개, 빈 프레임·실행 오류 0. 카드 내부 스크롤 없음.
- `development-build.log`: 최종 개발 빌드 성공, 오류 0.
- `release-build.log`, `release-hashes.json`: 배포 빌드 성공, 설치 파일 4개가 배포 출력과 동일.
- `release-startup.json`: 창 제목·응답 정상, 시작 오류 0, 개인 이어하기 저장 보존.
- `archive-validation.json`: v0.1.1 ZIP의 런타임 180개 파일 CRC·SHA-256 일치, 누락 없음.

`preview_hand.png`에서 짧은 손패 설명을, `irem_detail.png`와 `irem_detail_full.png`에서 같은 요약 및 전체 설명을 확인할 수 있다. `irem_field.png`는 고양이 상태 활성, `irem_reverted.png`는 두 번째 R로 해제한 필드다.

자세한 결과는 [전체 검증 기록](../VALIDATION.md)의 최신 절에 있다. 테스트 캡처의 전투 상태는 재현용이며 실제 게임의 난이도 수치를 나타내지 않는다.
