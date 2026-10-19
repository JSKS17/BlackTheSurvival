# 니아 궁극기 도트 애니메이션

게임에서 사용하는 12프레임 승인 아트입니다. `preview.html`에서 같은 프레임·오프셋·속도로 확인할 수 있습니다.

- `nia-ultimate-atlas.png`: 1448×1086 RGBA, 4열×3행, 각 셀 362×362. 2026-10-19 사용자 요청으로 홍채 RGB만 흑안으로 수정했습니다. 눈 밖 RGBA와 전체 알파는 원본과 완전히 같습니다.
- `frames/frame-00.png`~`frame-11.png`: 수정 승인 atlas를 정확히 분리한 셀입니다. 위치·크기·눈 밖 픽셀은 유지했습니다.
- `aligned-frames/frame-00.png`~`frame-11.png`: 기존 480×480 투명 캔버스의 발 기준점과 정수 오프셋을 유지합니다.
- `nia-ultimate-preview.webp`: 8fps, 12프레임, 1.5초, 투명 lossless WebP입니다. 기존 디코드 프레임에서 홍채 RGB만 치환했고 눈 밖 가시 RGBA·알파·타이밍은 같습니다.
- `nia-ultimate-preview.gif`: 기존 GIF의 눈 밖 가시 RGBA·알파·12프레임·교대 130/120ms를 그대로 보존했습니다. 추가 팔레트 양자화 없이 홍채 RGB만 치환했습니다.
- `prompt.txt`: 최초 생성 프롬프트. `alignment-attempt-prompt.txt`는 미채택 정렬 생성 이력입니다.
- `eye-color-prompt.txt`, `provenance.json`: 흑안 수정 프롬프트와 승인된 원본·최종 해시 및 재생성 경로입니다.
- `validation.json`: 현재 승인 atlas와 각 프레임의 해시, 눈 밖 불변·재생 조건 검증 결과입니다. 원본 백업과 좌표 마스크·비교 증거는 `Build/NiaEyesVerification/`에 있습니다.

다시 내보낼 때는 현재 승인 atlas를 사용합니다. 최초 imagegen 출력을 복원하면 승인된 눈색 수정을 잃으므로 복원하지 않습니다.
