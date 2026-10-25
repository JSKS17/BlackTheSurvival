# 현재 접속자 표시 — 웹 1.0.28

로비 로고 오른쪽 위에 초록 원과 `현재 접속자 n명`을 표시한다. 실제 로그인 계정을 D1에서 중복 없이 집계한다. 게임이 실행 중인 모든 화면은 집계에 포함하지만 표시는 가려지지 않은 로비에서만 한다. 30초 heartbeat, 마지막 갱신 후90초 lease, 탭별 퇴장, 세션 로그아웃/만료 처리를 사용한다. 서버 값을 모르면 표시하지 않는다. Unity 캔버스가 직접 그리므로 기존 일반·창 전체화면·브라우저 전체화면을 유지한다.

변경 범위는 웹 접속 집계와 로비 표시이며 게임 규칙·스프라이트는 바꾸지 않았다. Windows 정식 릴리스는1.0.27을 유지한다.

검증 자료:

- `source-validation.json`: 123개 runtime C#을 포함한 동결 입력과14개 서버 입력, 기존 아트13개 불변 해시.
- `backend-tests.json`: 실제 workerd/D1 계정·저장·접속자18개 테스트.
- `presence-client-tests.json`: 네트워크/수명/만료 클라이언트25개 검증.
- `host-regressions.json`: 계정 생명주기·저장·화면 모드·로더·브리지115개 회귀 검증. 다중 React effect에 맞춘 기존 테스트 하네스 보정 증거도 보존했다.
- `unity-presence-hook-tests.json`: 일반/WebGL C# 사전 컴파일 및 JS 라이브러리 연결 검증.
- `preview-http-smoke.json`: 운영 서버와 분리한 실제 임시 D1의0→1→2→1 집계 및 세션 쿠키 전달 검증.
- `unity-build-observer-result.json`: 빌드 종료 코드와 root/clone 자산 동결 검증.
- `browser-validation.json`: 완성한 실제 WebGL+임시 D1에서1→2→1 표시와 화면 모드를 확인한 증거. 물리 모바일 기기는 에뮬레이션하지 않는다.
- `live-validation.json`: 공개 버전·정적 파일·R2 데이터·API 인증·업로드 종료 검증.
- `completion.json`: 배포·동기화·정리가 모두 끝난 뒤 실행하는 최종 감사.

로컬 QA는 `node docs/validation-presence-20261026/Preview.mjs`로 시작한다. `http://127.0.0.1:8249/qa?fixture=research`에서 로비가 열린다. 두 번째 로컬 계정 시작/종료 버튼은 실제 임시 D1에 heartbeat/leave를 보내며 표시 숫자를 조작하지 않는다. 운영 계정을 만들거나 운영 DB를 변경하지 않는다. 종료는SIGINT로 하며 worker·타이머를 모두 정리한다.

배포는 기존 Site55/1.0.27을 기준으로 한다. 공식 Sites source helper로 연 `Temp/SitePresence20261026`을 사용하며, 2단계 immutable R2 업로드와 최종 배포 후 공개 검증을 마친다. `PrepareRelease.mjs sync`가 게임 파일을, `SyncWebSources.mjs`가 검증한 서버 소스만 canonical WebSite로 복사한다.

`Cleanup.ps1`은 성공한 배포·브라우저·소스·서버·클라이언트 검증과 중지된 프로세스를 확인한 뒤 이 작업의Build, 두 임시 배포 폴더와 두 패키지만 제거한다. 소스·아트·검증 자료·의존성과 공개 파일은 보존한다.

최종 공개 버전은 Site57 / 게임1.0.28이다. 실제 완성 WebGL과 임시 D1에서1→2→1 표시, 설정 창을 열었을 때 표시 숨김, Unity 캔버스 전체화면 표시를 확인했다. 브라우저 오류 로그는 없었다. 로컬 QA 창을 닫고 서버에 SIGINT를 보낸 후8249번 포트의 리스너가 없는 것도 확인했다. Windows의 콘솔 중지 종료 코드는1이며, 게임/브라우저 테스트 실패를 의미하지 않는다.

공개 검증11개 항목이 통과했다. 초기 검증 도구는 `/game/index.html`에서 `/game/`으로 향하는 호스팅의307 정규화와 Cloudflare JavaScript detection 스크립트 주입을 게임 변경으로 보아 실패했다. 실패 보고서를 보존하고, 같은 origin·정확한 경로·쿼리를 유지하는 정규화만 허용하도록 검사 도구를 보정했다. 추가된 한 개의 알려진 보안 스크립트 앞뒤 HTML은 원본과 완전히 같아야 하며 원본 게임 HTML 해시도 검증한다. 게임 제품 소스나 호스팅 보안 설정은 바꾸지 않았다.
