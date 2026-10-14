# v1.0.2 얼굴과 야생동물 도트

내장 `image_gen.imagegen`으로 프롤로그 두 번째 컷의 니아 얼굴과 야생동물 4종을 제작했습니다. 생성된 PNG를 바이트 그대로 게임 리소스에 복사하며 래스터 후처리는 하지 않습니다.

니아는 무표정, 포즈, 의상과 VF 블록을 유지하면서 양쪽 눈의 윤곽·자주색 홍채·반사광을 다시 정리했습니다. 오른쪽 눈이 머리카락과 구분되도록 다듬고 작은 닫힌 입을 유지합니다. 출력은 기존과 같은 1254×1254 RGBA이며, 게임의 기존 80×80 Point 표시와 배경·캐릭터·텍스트 분리 구성을 유지합니다. 위치 메타데이터만 알파32 기준으로 갱신했습니다.

- 게임 리소스: `Assets/Resources/Lumia/Story/Characters/nia_overload.png`
- 정확한 입력·출력 해시와 생성 경로: [매니페스트](manifest.json)
- 내장 도구에 전달한 전체 프롬프트: [니아 얼굴](nia-face-prompt.txt)
- 외형 참고: [공식 니아 출시 페이지와 월페이퍼](https://event.playeternalreturn.com/S7/NIAH?hl=ko-KR), 기존 `PortraitsReference/nia.png`
- 야생동물 4종의 외형 자료·프롬프트·해시: [야생동물 제작 기록](wildlife/README.md)

로비와 다른 컷신 캐릭터, 들개 스프라이트, 승인한 R 애니메이션은 변경하지 않습니다. 원작 이미지와 IP의 권리는 님블뉴런에 귀속됩니다.
