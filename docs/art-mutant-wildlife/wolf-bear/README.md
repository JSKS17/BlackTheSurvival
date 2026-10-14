# 변이 늑대·곰 도트

내장 `image_gen.imagegen`의 정밀 편집으로 일반 늑대와 곰의 자세, 도트 크기, 실루엣, 장구 위치를 유지한 전용 변이 PNG를 만들었습니다. 일반 동물 파일은 바꾸지 않았습니다. 생성 PNG는 바이트 그대로 런타임에 복사했으며 별도의 래스터 후처리는 없습니다.

## 원작 외형 참고

- [이터널 리턴 공식 1.0.0 게임플레이](https://playeternalreturn.com/posts/news/1281?hl=en-US): 변이 늑대 무리와 변이 곰 무리의 구속구, 붉은 상흔, 등 장치를 확인했습니다. 참고 스크린샷은 `ArtSource/Release1.0.3/Mutants/official-mutant-{wolf,bear}-camp.png`입니다.
- [이터널 리턴 공식 시즌 3 개발자 방송 요약](https://playeternalreturn.com/posts/news/1717?hl=en-US): 변이 무리 스크린샷의 검붉은 늑대와 붉은 곰의 털 색을 최종 팔레트 기준으로 사용했습니다. `ArtSource/Release1.0.3/Mutants/official-dev-mutant-packs.png`에 원본 참고를 보관합니다.
- [공식 시즌 3 변이 무리 이미지](https://cdn.playeternalreturn.com/2024/02/07/1707282949502-9.png).

초안은 2023년 자료의 짙은 털과 붉은 구속구를 적용했습니다. 최종 편집에서는 시즌 3 자료에 맞춰 늑대의 털을 검붉은 색, 곰의 털을 적갈색으로 수정했습니다. 큰 사각 도트, 단계형 명암, 눈과 주둥이의 형태는 기존 스프라이트를 유지합니다.

프롬프트는 각 `*-initial-prompt.txt`와 `*-final-prompt.txt`에, 최종 파일의 크기·투명 여백·해시는 `manifest.json`에 기록합니다. 런타임은 기존 동물과 같은 80 논리 픽셀 높이의 Point 샘플링을 사용합니다.
