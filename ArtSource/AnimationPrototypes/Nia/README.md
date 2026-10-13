# 니아 궁극기 도트 애니메이션 시안

게임에 연결하지 않은 12프레임 아트 시안입니다. `preview.html`을 브라우저에서 열면 재생·일시 정지·프레임 이동·속도 변경을 사용할 수 있습니다.

- `nia-ultimate-atlas.png`: 내장 이미지 생성 결과 그대로 보존한 1448×1086 RGBA 원본입니다. 4열×3행이며 각 셀은 362×362입니다.
- `frames/frame-00.png`~`frame-11.png`: 원본의 사각 셀을 FFmpeg로 분리한 PNG입니다. 크기·위치·픽셀 색상을 보정하지 않았습니다.
- `aligned-frames/frame-00.png`~`frame-11.png`: 각 셀을 480×480 투명 재생 캔버스의 같은 발 기준점에 배치한 프레임입니다. 원래 픽셀을 리샘플링하거나 고치지 않고 정수 좌표로 옮겨 재생 중 흔들림을 줄였습니다. HTML도 같은 표시 오프셋을 적용합니다.
- `nia-ultimate-preview.webp`: 8fps, 1.5초, 무한 반복의 lossless animated WebP입니다. 투명도를 유지합니다.
- `nia-ultimate-preview.gif`: 같은 프레임의 GIF 미리보기입니다. GIF 형식의 256색·1비트 투명도 한계가 있으므로 반투명 효과는 WebP보다 거칠 수 있습니다.
- `prompt.txt`: 사용한 내장 이미지 생성 프롬프트입니다.
- `alignment-attempt-prompt.txt`: 정렬 재생성 시도에 사용한 프롬프트입니다. 해당 후보는 셀 경계를 넘는 문제가 있어 최종 선택하지 않았습니다.
- `validation.json`: 원본 해시, 프레임 동일성, 애니메이션 프레임 수·시간 검증 결과입니다.

설계와 출처는 [제작 기록](../../../docs/ART_NIA_ULTIMATE_PROTOTYPE.md)을 참고하세요.
