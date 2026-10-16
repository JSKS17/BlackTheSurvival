# 에키온 R 승인 시안 통합 · 2026-10-15

사용자가 별도 미리보기의 에키온 R을 승인한 뒤 게임에 적용했습니다. 승인된 `ArtSource/AnimationPrototypes/EchionPreview20261015/atlas.png`를 그대로 복사했으며, 다시 생성하거나 크기·색·그림을 수정하지 않았습니다.

- 승인 시안, 정식 원본과 런타임 PNG의 SHA-256은 모두 `ebd402686b030241dd23f4524992f1cb6012c1290cabdabe16a6b6d327cad132`입니다.
- 기존 `BuildUltimatePreviews.py`와 `ExportUltimateAnimations.py`를 사용했습니다. `ExportUltimateAnimations.py --check`에서 91종·1,092프레임을 검증했습니다.
- 런타임 좌표와 오프셋으로 복원한 에키온 12프레임은 승인 미리보기와 정식 원본 프레임의 RGBA 바이트에 일치합니다. 8fps·1.5초 반복과 GIF/WebP도 승인 시안과 같습니다.
- 나머지 90종의 런타임 PNG, 기존 Unity 메타데이터의 모든 바이트, 원본·런타임·검증 카탈로그의 해당 항목은 바뀌지 않았습니다. 에키온의 기존 GUID, Point, Clamp, 밉맵 비활성화 설정도 유지합니다.
- 전체 갤러리 HTML의 공통 부분과 다른 90종 항목은 같으며, 에키온 항목만 갱신했습니다.
- 최초 미리보기의 이미지·프레임·메타데이터와 제작 당시 `scope-verification.json`은 역사 기록으로 보존했습니다. 승인 이후 미리보기 HTML의 상태 표시는 별도로 갱신할 수 있습니다.

기계 검증 결과와 파일별 해시는 [integration.json](integration.json), exporter 최종 검사는 [export-check.log](export-check.log)에 있습니다. 빌드·배포 검증과는 별도인 원본/런타임 자산 통합 검증입니다.
