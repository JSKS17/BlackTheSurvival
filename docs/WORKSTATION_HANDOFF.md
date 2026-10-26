# 다른 컴퓨터에서 작업 이어가기

2026-10-26 기준. 게임과 웹 배포가 완료된 시점의 인수인계 문서입니다. 이 문서 추가는 게임이나 운영 배포를 변경하지 않습니다.

## 보관 상태

| 대상 | 복원 위치 및 기준 |
| --- | --- |
| Unity 게임·아트·검증 소스 | https://github.com/JSKS17/BlackTheSurvival.git 의 `main` |
| 마지막 게임 수정 커밋 | `a1d4c0f3d2ce6fcff8ec81619c2eb420d84c3126` — 룬 설명에서 색상 대신 원래 계열명 사용 |
| 운영 웹 | https://bser-sts.com/ — 게임 **1.1.0**, Sites **61**, 환경 **64** |
| 별도 Sites 웹 소스 커밋 | `56fa4d79d6dbfbf376bc95c5aee3241c54fabde3` |
| Sites 프로젝트 ID | `appgprj_6ad077cf90708191a3e75367b4b3b78c` |
| Sites 저장 버전 ID | `appgprj_6ad077cf90708191a3e75367b4b3b78c~appgver_1a0377fdb4e88191adf935b655b566e7` |
| 최근 배포 ID | `appgdep_6adf2c1736dc81918d2a1b978a2dd941` |

게임 수정 커밋과 GitHub `main`의 일치는 실제 `git ls-remote`로 확인했습니다. Sites 61의 별도 소스 커밋 및 저장된 배포 아카이브도 공식 도구로 확인했습니다.

**`WebSite/`는 루트 GitHub 저장소에 포함되지 않습니다.** 웹 서버 소스는 아래의 기존 Sites 프로젝트에서 별도로 복원해야 합니다. 현재 컴퓨터의 `WebSite/.git`은 커밋이 없는 로컬 초기 저장소이므로 이것을 복제 원본으로 사용하지 마세요.

## 새 컴퓨터에서 시작

1. Git과 Unity Hub를 설치하고 Unity **6000.3.11f1** 및 **Web Build Support** 모듈을 준비합니다. 웹 서버를 수정하려면 Node.js **22.13.0 이상**과 기존 사이트를 소유한 계정의 Sites 연결도 필요합니다.
2. 원하는 작업 폴더에서 다음 명령으로 소스를 받습니다.

   ```powershell
   git clone https://github.com/JSKS17/BlackTheSurvival.git
   cd BlackTheSurvival
   ```

3. Unity Hub에서 이 프로젝트 루트를 열고 자동 패키지 설치·에셋 가져오기가 끝나면 Play를 누릅니다. 별도 장면 수동 배치 없이 로비가 시작됩니다.
4. 웹 수정이나 배포를 이어갈 때는 아래 절차로 **기존 사이트 소스**를 받습니다. 새 사이트나 데이터베이스를 만들지 않습니다.

`Assets`, `ArtSource`, `Packages`, `ProjectSettings`, `Tools`, `docs`는 GitHub에 포함됩니다. Unity `Library`·`Build`·`Temp`, `node_modules`와 개인 실행 환경은 다시 생성하므로 복사할 필요가 없습니다. 첫 에셋 가져오기와 WebGL 빌드는 오래 걸릴 수 있습니다.

## 웹 소스 복원 — 다음 작업 에이전트에게

현재 설치된 Sites의 `SKILL.md`를 읽고 공식 소스 helper를 사용합니다. 플러그인 설치 경로와 사용자 이름은 새 컴퓨터에서 다시 찾습니다.

1. 위 프로젝트 ID로 `get_site`와 `create_source_repository_write_credential`을 호출합니다. 기존 공개 범위와 DB/R2 바인딩을 유지합니다.
2. 프로젝트 안의 비어 있는 `WebSite` 디렉터리에서 공식 `site-workflow.mjs`를 실행합니다. 첫 열기에서는 `archivePath`와 임의의 `source` 객체를 넣지 않습니다. 자격 증명은 메모리에만 두고 stdin으로 전달합니다.
3. helper가 반환한 정확한 `checkout_path`와 전체 소스 결과를 유지합니다. 여기의 최신 소스를 확인한 뒤 수정합니다. 재개 시점에 새 배포가 생겼다면 위 기록보다 새 운영 상태를 우선합니다.
4. 설치된 Sites의 의존성 helper로 웹 의존성을 복원합니다. `.openai/hosting.json`의 기존 프로젝트 ID와 `DB`, `BUCKET` 바인딩을 보존합니다.
5. 웹 소스의 `build/`에는 필수 플러그인·Worker 소스가 있으므로 일반 빌드 캐시로 간주해 삭제하지 않습니다. `.git`, `.sites-runtime`, `.vinext`, `node_modules`, `.wrangler`, `dist`와 개인 토큰을 다른 컴퓨터로 옮길 필요는 없습니다.

운영 계정·진행 기록·환경설정은 클라우드 D1에, 큰 Unity 데이터 자산은 R2에 있습니다. 기존 프로젝트를 재사용하면 컴퓨터 이동과 무관하게 유지됩니다. Unity 에디터·Windows 실행판의 개인 로컬 이어하기나 브라우저에만 남은 미동기화 기록은 Git에 포함되지 않습니다.

## 이어서 수정할 때 확인할 내용

- 현재 완료된 변경: 공식 역할군에 맞는 메인 룬의 스킬·패시브 등장 가중치 **4배**. 파괴=전사/원거리 딜러, 저항=탱커/전사, 혼돈=스킬 딜러/암살자, 지원=탱커/지원가. 설명에는 색상 대신 원래 룬 계열명을 사용합니다.
- 역할군 91명과 복수 역할군을 반영하며, 기존 보유 패시브의 스킬 등장 보정 1.8배와 함께 적용됩니다. 룬 변경으로 무료 재추첨되지 않도록 선택·시드를 보존합니다.
- 전체 회귀 257,493개와 룬 집중 검증 1,922개, 최종 WebGL 빌드·화면·공개 검증을 마쳤습니다. 근거는 [역할군 검증](validation-rune-roles-1.1.0/README.md)과 [최종 계열명 수정 검증](validation-rune-labels-1.1.0/README.md)에 있습니다.
- 웹은 1.1.0이며, GitHub의 다운로드용 Windows 정식 릴리스는 아직 **1.0.27**입니다. 소스를 받는 것과 이전 Windows 실행 파일을 받는 것을 구분합니다.
- 검증 폴더의 배포 helper는 당시 경로·기준 해시를 고정한 기록입니다. 새 컴퓨터에서 그대로 실행하지 말고 새 경로와 새 기준에 맞게 준비합니다. 자격 증명·업로드 토큰을 파일이나 Git에 기록하지 않습니다.
- 새 Unity WebGL 빌드는 큰 `.data`를 R2에 올리는 기존 2단계 배포 절차가 필요합니다. [최종 배포 기록](validation-rune-labels-1.1.0/publish-state.json)과 해당 검증 폴더의 `PrepareRelease.mjs`를 참고하고, 기존 자산·계정 API·접속자 표시를 보존합니다.
- 불필요해진 빌드·캐시는 검증과 배포가 끝난 뒤 즉시 정리하되 원본, 아트, 필수 웹 소스, 검증 증거는 보존합니다. 마지막 작업의 임시 빌드·배포 파일은 이미 정리했습니다.

프로젝트 루트 기준의 Windows 명령 예시는 다음과 같습니다. Unity 설치 위치가 다르면 `-UnityEditor`를 지정합니다.

```powershell
# 룬 추첨 관련 집중 검증
./Tools/RunCoreTests.ps1 -RuneOffersOnly

# WebGL 빌드. 출력은 이 프로젝트의 Build 아래에 생성됩니다.
./Tools/BuildWeb.ps1 -LowMemory -Wait
```

새 컴퓨터의 작업 에이전트에게는 다음과 같이 전달하면 됩니다.

> docs/WORKSTATION_HANDOFF.md를 먼저 읽고 현재 소스와 기존 Sites 프로젝트를 확인해. 최신 웹 1.1.0 상태에서 이어서 작업하고, 원본을 보존하면서 불필요한 빌드·캐시를 정리해.
