# 1.1.0 룬 계열명 문구 수정 배포

최종 공개 버전은 **1.1.0 / Sites 61 / 환경 64**, buildId `d44e9acd7c777ecff8356b81`입니다. 실제 WebGL의 계열 안내·선택 룬 가중치 문구·메인 및 보조 룬 상세가 원래 계열명만 사용하는 것을 확인했습니다. 브라우저 오류는 없었고 전체화면도 확인했습니다. 집중 1,922개와 공개 응답 11개 검증이 통과했습니다.

일시적인 배포 도구 내부 오류 후 정상 응답을 받아 완료했습니다. 업로드 마감은 환경63에서 과거 시점으로 닫았고, 임시 토큰 키도 환경64에서 제거했습니다. 최종 배포와 공개 검증은 환경64 기준입니다.

작업 시작 당시 공개 기준은 **1.1.0 / Sites 59 / 환경 61**, 소스 `73dec621568bf3ab9ddbf7aae2aac501c588595f`, buildId `9cd4b9c05da11eff0f692005`입니다. 게임 버전은 1.1.0을 유지하며 게임 설명의 색상 표현을 기존 룬 계열명으로 바꿨습니다. 원래 역할군·가중치·전투 기능은 유지합니다.

소유 경로는 `Build/Web110Labels`, `Build/UnityWebBuild`, `Temp/SiteRuneLabels110`, `Temp/RuneLabelsRelease110`입니다. `baseline.json`은 기존 manifest 및 보호 서버 7개의 SHA를 기록합니다. 이전 전체 회귀 **257,493개**의 원본 경로와 SHA도 참조하되 이전 검증 폴더의 파일은 수정하지 않습니다.

1. 공식 새 소스 체크아웃이 준비되고 제품 문구가 확정되면 `Freeze.mjs`를 한 번 실행합니다. 현재 Tools 실행본을 새로 동결하므로 이번 배포에는 테스트 소스 amendment 예외가 없습니다.
2. 메인 작업에서 `RunFocusedProof.mjs`를 실행하면 `Tools/RunCoreTests.ps1 -RuneOffersOnly`의 실제 집중 결과를 `gameplay-tests.json` 및 로컬 로그로 기록합니다. 예상 검증 수는 1,922개입니다. 동결 SHA·실행한 소스 SHA·이전 전체 회귀 출처를 함께 보존합니다. 실행 전후의 입력 파일이 달라지면 실패합니다.
3. `import-process.json`이 준비된 상태에서 PowerShell 7로 `StartBuild.ps1`, 이어 `ObserveUnityBuild.ps1`을 실행합니다. import 종료를 기다리고 동결본을 검증·재복사한 뒤 숨김/낮은 메모리 WebGL 빌드를 진행합니다. 새 출력의 `build-manifest.json`을 검증 폴더의 `unity-build-manifest.json`으로 보존합니다.
4. 완성된 실제 게임은 `Preview.mjs`를 실행해 `http://127.0.0.1:8251/qa?fixture=research`에서 검증합니다. 로비에서 새로운 실험으로 들어가 룬 선택·상세 설명에 계열명만 표기되는지 확인합니다. 일회성 실제 Miniflare D1은 기존 접속자 기능 검증용이며 운영 계정을 사용하지 않습니다. 브라우저 결과는 `browser-validation.json`의 PASS·버전·스크린샷 배열로 기록합니다.
5. `PrepareRelease.mjs prepare` 후 메인 작업이 공식 도구로 업로드 전환 버전을 패키징·저장·공개 배포하고 `bridge-state.json`을 기록합니다. `VerifyBridge.mjs`는 기존 1.1.0 게임의 정확한 이전 bytes를 확인합니다. 새 R2 업로드 및 업로드 게이트 개방·종료는 메인 작업에서만 수행합니다.
6. 업로드 후 `PrepareRelease.mjs finalize`로 새 패키지를 설치합니다. 메인 작업이 최종 공식 패키징·저장·배포를 완료해 `publish-state.json`을 기록하고 `VerifyLive.mjs`를 실행합니다. 버전이 같아도 manifest·buildId·스크립트·wasm·R2 metadata를 실제 새 패키지와 대조합니다.
7. 공개 검증 성공 후 `PrepareRelease.mjs sync`, `SyncWebSources.mjs` 순서로 실행합니다. 게임 원본을 트랜잭션으로 동기화하고 변경하지 않는 서버 파일 7개는 쓰기 없이 일치 여부만 확인합니다.
8. 작업이 만든 모든 Unity·테스트·미리보기 프로세스를 종료하고 PowerShell 7에서 `Cleanup.ps1 -VerifiedServersStopped`를 실행한 뒤 `Complete.mjs`로 완료 증거를 남깁니다. 정확한 소유 경로 5개만 정리하며 원본·아트·이전 검증 폴더·canonical 게임과 의존성은 보존합니다. Windows/GitHub release 게이트는 없습니다.

`ValidateHelpers.mjs`는 문법과 기준 파일만 검사하며 빌드·테스트·서버·배포를 실행하지 않습니다. `PrepareHelpers.mjs`는 최초 생성 출처 기록용이므로 이미 준비된 파일 위에 다시 실행하지 않습니다. 실제 Sites 도구·소스 helper·토큰·배포 작업은 메인 작업에서만 진행합니다.
