# v1.0.1 로비와 프롤로그 아트

내장 `image_gen.imagegen`으로 기존 연구소 배경을 더 굵은 도트로 다시 만들고, 프롤로그 두 번째 컷의 니아 얼굴을 교정했습니다. 생성 PNG를 바이트 그대로 런타임 경로에 복사했습니다. 래스터 리사이즈·채색·알파 편집은 하지 않았습니다.

- 배경: `Assets/Resources/Lumia/Lobby/Background.png`, 1672×941 RGB. 기존 연구소의 구도, 빈 게이밍 의자, 모니터·배양통·좌측 메뉴 여백을 유지하고 명암과 윤곽을 큰 픽셀 덩어리로 정리했습니다. 표시할 때 256×144 Point RenderTexture를 사용해 1280×720에서 약5px 단위의 도트가 보입니다. 배경 RenderTexture의 손실 복구와 해제 경로도 연결했습니다.
- 니아 얼굴: `Assets/Resources/Lumia/Story/Characters/nia_overload.png`, 1254×1254 RGBA. 기존 포즈·복장·이펙트를 유지하면서 눈매를 수평으로 낮추고 입과 눈썹을 절제해 무표정을 유지합니다. 기존80×80 논리 스프라이트 표시와 독립 배경·텍스트·말풍선 구성을 유지합니다. 알파32 기준 위치 메타데이터만 새 PNG에 맞춰 갱신했습니다.
- 로비의 니아 스프라이트는 변경하지 않았으며 SHA-256도 동일합니다. 두 입력의 이전 바이트는 v1.0.0 Git 태그에 남아 있습니다.

니아 얼굴은 [공식 니아 출시 이벤트의 월페이퍼](https://event.playeternalreturn.com/S7/NIAH?hl=ko-KR)와 기존 게임 도트를 참고했습니다. 공식 참고 이미지는 `ArtSource/Release1.0.1/nia-official-wallpaper.png`에만 보관하며 게임 런타임에는 포함하지 않습니다. 원작 이미지와 IP의 권리는 님블뉴런에 귀속됩니다.

정확한 내장 도구 프롬프트는 [배경](background-prompt.txt)과 [니아 얼굴](nia-face-prompt.txt), 생성·입력·출력 경로 및 SHA-256·알파 위치는 [매니페스트](manifest.json)에 보관합니다. 실제 게임 화면 검증은 `docs/validation-v1.0.1/`에 기록합니다.
