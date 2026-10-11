# v0.1.11 치비 스프라이트·무보컬 BGM 검증

검증 환경: Windows x64, Unity 6000.3.11f1, 2026-10-11.

## 새 캐릭터 자산

실험체 91명과 하나의 새 스프라이트 92개를 개별 생성했다. 사용자 첨부 그림은 스타일 참고만 사용하고 배포하지 않는다. [전체 생성·해시 검증](../art-reference-chibi/validation.json)은 누락·중복·오류 0개이며, 모든 PNG는 투명 배경과 80×80 논리 격자의 정확한 2배 확대를 사용한다. [8페이지 비교 시트](../art-reference-chibi/contact-sheets/sprites_01.png)를 모두 육안으로 확인했다. 얼굴·피부색·눈매·안경·수염·의상·대표 소품을 구분한다.

실제 개발 플레이어는 새 리소스 폴더의 Texture2D 객체와 일치하는지 검사한다. [native-art/coverage.txt](native-art/coverage.txt)는 새 스프라이트 92개·누락 0개를 기록한다. [Unity 출력 PNG 비교](native-art/png-verification.json)는 모든 캐릭터의 알파 경계와 검은색·흰색 배경에 합성한 가시 픽셀을 소스와 비교한다. 완전 투명 픽셀의 RGB 차이는 제외한다. [PNG·임포터의 소스/빌드 해시](native-art/build-asset-hashes.json)도 92명 모두 일치한다. 실제 연락판 8페이지는 1280×720이며 비어 있는 페이지와 실행 오류가 없다.

## 무보컬 음악

로비만 기존 공식 공개 보컬 OST를 사용한다. 그 외 9개 음악 경로는 새 프로젝트 작곡 연주곡 5개에 연결한다. 원본 MR 또는 보컬 분리본으로 표시하지 않는다. [곡·출처·확보 시도](../audio/INSTRUMENTAL_MUSIC.md)를 기록한다.

[소스 음원 검사](catalog-audit.json)는 426개 클립·447개 상황 연결·392개 카드 명시 라우팅과 출처·해시·PCM·비로비 무보컬 조건에서 15,873개 검사를 통과했다. [실제 게임 믹서 검사](native-audio.json)는 3,129개 검사를 통과했다. 압축 음원의 Unity 가져오기, 10개 화면 테마의 출력·전환·반복, 효과음·보이스, 음량·음소거·일시정지·설정 저장을 검사한다. 이는 OS 출력 이전의 AudioListener 측정이며 실제 스피커 청취를 증명하는 검사는 아니다.

## 게임 화면과 배포

개발 플레이어의 전체 UI 검증은 기존 카드·전투·보상·조우·장비·연계 화면과 음악 설정 131개를 실제로 실행했다. 카드 392종의 4,704개 레이아웃에서 본문 넘침·내부 스크롤 0개, 실행 오류 0개다. 결과는 [native-screens.json](native-screens.json), 실제 C# 및 음원 빌드 일치는 [build-source-hashes.json](build-source-hashes.json)과 [build-audio-hashes.json](build-audio-hashes.json)에 보존한다. 개인 저장과 일반 설정도 변경되지 않았다.

[개발 빌드](development-build.log)와 [배포 빌드](release-build.log)는 모두 오류 0개로 완료했다. 배포 `boot.config`에 개발 플레이어 연결 옵션이 없다. [설치 파일 해시](release-hashes.json)가 빌드 출력과 일치하고, [실제 정상 시작](release-startup.json)과 개인 저장의 설치·시작 전후 일치를 확인했다. [ZIP 검사](archive-validation.json)는 181개 런타임 파일의 CRC·SHA-256·누락·추가 파일·개인 저장 제외를 모두 통과했다. ZIP은 92,908,907바이트다. 최종 실행 파일은 [v0.1.11 릴리스](https://github.com/JSKS17/BlackTheSurvival/releases/tag/v0.1.11)에서 받을 수 있다.
