# 변이 동물 전용 도트

원작의 [시즌3 개발자 자료](https://playeternalreturn.com/posts/news/1717?hl=en-US)와 [공식1.0.0 변이 무리](https://playeternalreturn.com/posts/news/1281?hl=en-US)를 참고해 닭·들개·멧돼지·늑대·곰의 전용 변이 스프라이트5종을 내장 `image_gen.imagegen`으로 제작했습니다. 일반 동물 PNG와 들개를 포함한 기존 아틀라스는 변경하지 않습니다.

| 변이 동물 | 외형 | 최종 PNG |
| --- | --- | --- |
| 닭 | 보라색 볏·날개·꼬리와 검붉은 몸 | `Assets/Resources/Lumia/Wildlife/mutant_chicken.png` |
| 들개 | 붉은 털·검은 등과 주둥이·실험용 목걸이 | `Assets/Resources/Lumia/Wildlife/mutant_dog.png` |
| 멧돼지 | 진홍색 몸·짙은 갈기·금색과 검은색 하네스 | `Assets/Resources/Lumia/Wildlife/mutant_boar.png` |
| 늑대 | 검붉은 털·붉은 구속구와 발광 상흔 | `Assets/Resources/Lumia/Wildlife/mutant_wolf.png` |
| 곰 | 붉은 적갈색 털·붉은 구속구와 상흔 | `Assets/Resources/Lumia/Wildlife/mutant_bear.png` |

모든 PNG는1254×1254 RGBA의 도구 출력을 바이트 그대로 보관하며, 별도의 래스터 후처리는 없습니다. 화면은 기존80픽셀 Point 표시와 같은 전투 영역을 사용합니다. 일반PNG를 기준으로 편집한4종은 실루엣 경계 차이가 논리1픽셀 이내입니다. 기존 들개는 아틀라스에서 여백을 잘라 표시하므로 변이 들개에도 런타임의 동일한 투명 여백 정렬 원리를 적용합니다. 일반 들개의 그림과 화면 크기는 유지합니다.

전투 본체, 상대 정보, 동물 기술 의도 아이콘·상세·발동 배너에서 저장된 변이 여부에 맞춰 해당 스프라이트를 표시합니다. 저장 후 다시 시작해도 같은 그림을 선택합니다. 등장 확률과 능력·보상 수치는 기존1.0.2 규칙 그대로입니다.

[통합 해시·출처·크기](manifest.json), [늑대·곰 프롬프트](../../art-mutant-wildlife/wolf-bear/README.md), [멧돼지·닭 프롬프트](../../art-mutant-wildlife/boar-chicken/README.md), [들개 프롬프트](../../art-mutant-wildlife/dog/manifest.json)를 보관합니다. 원작 IP와 이미지의 권리는 님블뉴런에 귀속됩니다.
