# 변이 멧돼지와 변이 닭 도트

기존 일반 동물 도트를 편집 대상으로 삼아 전용 변이 스프라이트를 내장 `image_gen.imagegen`으로 각각 제작했습니다. PNG는 생성 결과를 바이트 그대로 프로젝트에 복사하며 별도 래스터 후처리를 하지 않습니다.

일반 동물의 왼쪽을 보는 자세, 발 위치, 실루엣 크기, 투명한 1254×1254 캔버스를 유지하도록 요청했습니다. 게임의 기존 높이 80 논리 도트 격자와 Point 표시를 그대로 사용합니다.

- 변이 멧돼지: 공식 게임플레이에서 확인되는 진홍색 몸, 짙은 등갈기, 검정·금색 실험 하네스와 밝은 상아색 엄니를 반영했습니다.
- 변이 닭: 공식 게임플레이에서 확인되는 보라·자주색 깃털과 어두운 몸, 밝은 보라색 깃털 하이라이트로 일반 닭의 황갈색·청록색 외형과 구분했습니다.

## 원작 자료

외형은 님블뉴런의 [Season 3 Dev Stream Overview](https://playeternalreturn.com/posts/news/1717?hl=en-US)에 실린 스크린샷과 게임플레이 GIF를 참고했습니다. 자료는 `ArtSource/Release1.0.3/Mutants/`에 보관하고 원본 이미지는 게임에 그대로 사용하지 않습니다.

- `official-dev-mutant-packs.png`: 변이 동물 무리의 색상·외형.
- `official-dev-mutant-packs2.png`: 일반 멧돼지와 변이 멧돼지의 비교.
- `official-dev-boar-charge.gif`: 변이 멧돼지의 인게임 외형.
- `official-dev-chicken-aggro.gif`: 변이 닭의 인게임 외형.

전체 입력 프롬프트는 [prompts.json](prompts.json), 입력·출력 해시와 투명도·크기 기록은 [manifest.json](manifest.json)에 기록합니다. 일반 동물 PNG, 기존 실험체 및 승인한 R 애니메이션은 변경하지 않습니다. 원작 IP와 참고 이미지의 권리는 님블뉴런에 귀속됩니다.
