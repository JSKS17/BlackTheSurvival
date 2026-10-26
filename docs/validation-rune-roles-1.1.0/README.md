# 룬 역할군 1.1.0 배포 검증 보조 도구

기준은 웹 1.0.28 / Sites 57 / 소스 `73b44728095b8aaae3edf8ada1ebe3409ef62ab2`입니다. 기준 manifest 및 변경하지 않는 서버 파일 7개는 `baseline.json`에 기록되어 있습니다. 새 게임 출력은 `Build/Web110`, 격리 Unity 프로젝트는 `Build/UnityWebBuild`, 공식 Sites 소스는 `Temp/SiteRuneRoles110`, 임시 배포 패키지는 `Temp/RuneRolesRelease110`입니다.

이 도구들은 공식 Sites 소스 쓰기 자격 증명, 환경 변수 변경, 업로드 토큰 발급, 버전 저장 또는 실제 배포를 수행하지 않습니다. 해당 작업은 메인 작업에서 공식 도구로 진행합니다. Windows 빌드와 GitHub release는 이 웹 전용 배포의 필수 조건이 아닙니다.

1. 모든 제품·테스트·공식 역할군 문서가 확정되고 공식 소스 체크아웃이 준비되면 `node docs/validation-rune-roles-1.1.0/Freeze.mjs`를 한 번 실행합니다. `product`, `web`, `unchangedArt`와 함께 `tools`, `sourceDocumentation`을 동결하고 기존 접속자 브리지 5개 파일이 1.0.28과 같은지 검사합니다. 문서의 최종 `.md/.json/.csv/.txt`만 포함합니다. 동결 파일은 덮어쓰지 않습니다.
2. 메인 작업이 `gameplay-tests.json`을 작성합니다. `result` 또는 `status`는 `PASS`이며 각 테스트 로그·소스 SHA 및 `sourceFreezeSha256`을 포함하는 것이 권장됩니다. 브라우저 검증은 `browser-validation.json`의 `result: PASS`, `gameVersion: 1.1.0`, 상대 파일 이름의 `screenshots` 배열을 사용합니다.
3. PowerShell 7에서 `StartBuild.ps1`을 실행하면 `import-process.json`의 전처리 종료를 기다리고 동결본을 확인한 뒤 재복사하여 낮은 메모리 설정으로 숨김 Unity 빌드를 시작합니다. 이어 `ObserveUnityBuild.ps1`을 실행해 실제 종료 코드와 소스 일치를 기록합니다. `Build/Web110/build-manifest.json`을 검증 폴더의 `unity-build-manifest.json`으로 보존합니다. 반복 빌드는 이전 출력과 증거를 먼저 따로 검토해야 합니다.
4. 빌드 성공 후 `PrepareRelease.mjs prepare`가 새 출력물을 패키징하고 현재 게임을 유지하는 업로드 전환 manifest를 준비합니다. 메인 작업은 이 전환 버전을 공식 helper로 패키징·저장·공개 배포한 뒤 `bridge-state.json`을 기록하고 `VerifyBridge.mjs`를 실행합니다. 전환 중 게임은 1.0.28이어야 합니다.
5. 메인 작업이 임시 업로드 게이트를 열고 격리된 `scripts/upload-unity.mjs`로 `Build/Web110`을 업로드한 뒤 게이트를 닫습니다. 토큰은 로그나 파일에 저장하지 않습니다.
6. `PrepareRelease.mjs finalize`로 체크아웃에 1.1.0을 설치합니다. 메인 작업이 공식 helper로 최종 패키징·저장·공개 배포하여 `publish-state.json`을 기록한 뒤 `VerifyLive.mjs`를 실행합니다. 기존 익명 세션·로그인 보호·접속자 API·접속자 스크립트·업로드 게이트와 새 게임의 버전·wasm·R2 metadata를 확인합니다. 비로그인 검증이며 실제 사용자 계정을 만들지 않습니다.
7. 공개 검증 성공 후 `PrepareRelease.mjs sync`, `SyncWebSources.mjs` 순서로 실행합니다. 게임은 트랜잭션으로 canonical 경로에 설치하고 서버 7개는 내용이 같음을 검사합니다. 서버 소스는 다시 쓰지 않습니다.
8. 작업이 만든 Unity·테스트·미리보기 서버를 종료하고 PowerShell 7에서 `Cleanup.ps1 -VerifiedServersStopped`를 실행합니다. 최종 배포·공개 검증·소스/테스트·게임 원본 동기화 검사를 모두 통과해야 합니다. 정확한 소유 경로 5개만 지우며 source, art, canonical 웹 게임 및 의존성을 보존합니다. 마지막으로 `Complete.mjs`를 실행합니다.

`Preview.mjs`는 실제 완성 빌드와 연구 fixture를 `http://127.0.0.1:8250/qa?fixture=research`에서 보여줍니다. 일회성 Miniflare D1 두 계정과 실제 접속자 API를 사용하고 운영 계정·데이터는 사용하지 않습니다. 기본 로비에서 새로운 실험으로 들어가 룬 설명과 역할군 선택을 확인할 수 있습니다. `--baseline`은 고정된 기존 1.0.28 정적 게임만 읽으며 해당 옵션에서만 공개 R2 파일을 읽을 수 있습니다. 서버 종료 시 SIGINT/SIGTERM으로 HTTP 서버와 일회성 D1을 함께 정리합니다.

작성 후 `node docs/validation-rune-roles-1.1.0/ValidateHelpers.mjs`는 문법과 기준 파일만 검사합니다. 빌드, 서버, 네트워크 검증 또는 배포를 시작하지 않습니다. `PrepareHelpers.mjs`는 복사 출처 기록용 생성기로, 이미 준비된 파일 위에 다시 실행하지 않습니다.
