# 원본 자료와 팬 게임 변환 기준

확인일: 2026-10-08. 구현 데이터: `Assets/Scripts/Lumia/GameDatabase.cs`.

이 프로젝트는 이터널 리턴을 소재로 만든 비공식 팬 게임이다. 캐릭터·스킬·장비·음식의 이름과 세계관은 님블뉴런의 IP에 속한다. 하나가 니아의 VF 능력으로 게임 세계에 들어가 탈출하는 줄거리, 조우 대사, 턴 전투 수치와 보상 설계는 이 프로젝트를 위한 창작이다. 로비 문구는 사용자의 요청에 따라 “이 게임은 님블뉴런이 개발한 IP ‘이터널 리턴’을 기반으로 한 비공식 팬 게임입니다. 모든 저작권은 님블뉴런에 귀속됩니다.”를 사용한다.

## 구현 범위

- 카드 378종: 실험체 Q/W/E/R 364종, 무기 스킬 9종, 전술 스킬 3종, 기본 카드 2종.
- 등록된 실험체 91명 모두 전투·보스·패시브·고유 조우 이벤트에 사용한다. 샬럿을 추가하여 기존 이벤트 90명 명단의 누락을 보완했다.
- 추가한 클래식 55명은 [ROSTER_CLASSIC.md](ROSTER_CLASSIC.md), 최근 26명은 [ROSTER_RECENT.md](ROSTER_RECENT.md)에 이름·쿨다운·패시브·조건부 연계의 원문 근거를 기록했다. 기존 이벤트의 타인 스킬 보상 29개도 각 실험체 자신의 카드로 교체했다.
- 특성 16종, 전설/초월 장비 29종, 음식/재료 14종, 오브젝트 5종.
- 원본의 모든 최신 수치를 일괄 복제한 데이터가 아니다. 아래의 공개 공식 문서에서 확인한 스킬 쿨다운 스냅샷을 비용의 근거로 사용하고, 카드의 실제 피해·회복·지속 시간은 턴제 전투에 맞게 다시 정했다.

## 카드와 패시브

비용은 원본 쿨다운 3초 이하: 0, 7초 이하: 1, 12초 이하: 2, 20초 이하: 3, 35초 이하: 4, 60초 이하: 5, 100초 이하: 6, 그 이상: 7로 변환한다. 여러 스킬 레벨의 값이 있으면 첫 번째 값을 사용한다. 원본의 중첩·에너지 요구를 생략한 츠바메 R, 유스티나 Q, 히스이 W는 각각 3·2·3으로 보정했다. 소멸하지 않는 0코스트 드로우·코스트 회복 카드는 반복을 막기 위해 최소 1코스트로 설정한다. 블링크는 기존 1.0 모듈 강화 값 45초를 유지한다.

쿨다운 감소·초기화는 특정 대상 카드의 이번 턴 무료 사용권으로 변환한다. 대상 카드가 덱에 있어야 하며, 손패에 없으면 뽑을 카드 또는 버린 카드에서 실제 한 장을 가져온다. 소멸한 카드를 복구하거나 새 카드를 복제하지 않는다. 스킬별·대상별 턴 제한을 두며, 턴을 종료하면 사용권이 사라진다. 수아 R은 수아가 마지막으로 사용한 Q·W·E 한 장을 다시 사용할 수 있게 한다. 이동속도 효과는 기존과 같이 일정 턴 회피율로 바꾸며 가장 높은 카드 보너스만 적용한다. 실제 피해·방어·회복량은 고코스트의 턴 투자에 맞춰 팬 게임용으로 조정했다.

| 실험체 | Q / W / E / R 쿨다운(초) | 근거 |
|---|---|---|
| 니아 | 6 / 12 / 32 / 80 | [1.47 출시](https://playeternalreturn.com/posts/news/2702?hl=ko-KR), [8.1 W 변경](https://playeternalreturn.com/posts/news/2852?hl=ko-KR) |
| 재키 | 11 / 6 / 16 / 60 | [8.0 리워크](https://playeternalreturn.com/posts/news/2828?hl=ko-KR) |
| 아야 | 10 / 13 / 19 / 80 | [1.0 실험체 자료](https://playeternalreturn.com/posts/news/1280?hl=ko-KR) |
| 현우 | 8 / 20 / 14 / 60 | [1.0 실험체 자료](https://playeternalreturn.com/posts/news/1280?hl=ko-KR) |
| 유키 | 5 / 15 / 14 / 90 | [1.0 자료](https://playeternalreturn.com/posts/news/1280?hl=ko-KR), [8.1 W 변경](https://playeternalreturn.com/posts/news/2852?hl=ko-KR) |
| 혜진 | 11 / 14 / 13 / 100 | [1.0 자료](https://playeternalreturn.com/posts/news/1280?hl=ko-KR), [8.1 E 변경](https://playeternalreturn.com/posts/news/2852?hl=ko-KR) |
| 수아 | 12 / 19 / 16 / 30 | [8.1 스킬 변경](https://playeternalreturn.com/posts/news/2852?hl=ko-KR) |
| 아이솔 | 15 / 13 / 18 / 30 | [1.0 실험체 자료](https://playeternalreturn.com/posts/news/1280?hl=ko-KR) |
| 나딘 | 8 / 18 / 24 / 80 | [1.0 실험체 자료](https://playeternalreturn.com/posts/news/1280?hl=ko-KR) |
| 엠마 | 5 / 11 / 18 / 9 | [1.0 실험체 자료](https://playeternalreturn.com/posts/news/1280?hl=ko-KR) |

예를 들어 재키의 Q는 이후 [9.1 패치에서 12초로 변경](https://playeternalreturn.com/posts/news/3111?hl=ko-KR)되었지만 본 카드 세트는 리워크 시점의 11초를 사용한다. 두 값 모두 비용 2다. 출처 스냅샷을 유지하면서 게임의 카드 수치를 독립적으로 조정할 수 있다.

이동·투명화·순간이동은 1~2턴 회피 확률로, 군중 제어는 약화/취약으로, 출혈·화상·트랩은 하나의 지속 피해 수치로 단순화한다. 니아 1UP은 원본의 사망 방지 상태를 그대로 구현하지 않고 즉시 회복·방어도·회피로 바꿨다. 수아 기억력은 마지막 스킬을 복사하는 대신 드로우·에너지·방어도로 바꿨다. 공간, 스태미나, 실시간 재사용과 아군 대상 효과는 모델링하지 않는다.

패시브 명칭은 K.O., 피의 축제, 아야의 정의, 도그파이트, 완벽한 옷매무새, 삼재, 마음의 양식, 유격전, 야성, CheerUP♥이다. 원본의 추가 피해·보호막·회복·사냥 성장 개념을 전투 시작/매 턴/승리 시 보너스로 변환했다. 명칭과 원형은 [1.0 실험체 자료](https://playeternalreturn.com/posts/news/1280?hl=ko-KR), [니아 출시 문서](https://playeternalreturn.com/posts/news/2702?hl=ko-KR), [재키 리워크](https://playeternalreturn.com/posts/news/2828?hl=ko-KR)를 참고했다.

무기 카드 쿨다운은 글러브 15, 권총 40, 단검 40, 도끼 8, 양손검 30, 활 35, 망치 30, 레이피어 30, 저격총 40초 스냅샷이다. 원형은 [공식 1.0 무기/전술 자료](https://playeternalreturn.com/posts/news/1283?hl=ko-KR)다. 치유의 바람과 플라즈마 대시는 [11.0 전술 개편](https://playeternalreturn.com/posts/news/3530?hl=ko-KR) 및 [11.5 쿨다운 변경](https://playeternalreturn.com/posts/news/3657?hl=ko-KR)을 반영한 50초다.

## 룬에 해당하는 원본 특성

사용자가 말한 룬은 원본의 **특성** 이름과 파괴/혼돈/저항/지원 계열을 사용한다. 핵심 1개와 같은 계열의 보조 1개를 고르는 구성은 요청한 팬 게임 규칙이다. 원본의 전체 특성 편성 수를 복제하지 않는다.

| 계열 | 핵심 선택지 | 보조 선택지 |
|---|---|---|
| 파괴 | 흡혈마, 취약 | 들개 탈, 상흔 |
| 혼돈 | 벽력, 와류 | 속사, 서큘러 시스템 |
| 저항 | 금강, 빛의 수호 | 경계심, 담금질 |
| 지원 | 치유 드론, 증폭 드론 | 할인 쿠폰, 사냥의 전율 |

원형은 [1.0 특성 자료](https://playeternalreturn.com/posts/news/1283?hl=ko-KR), [10.0 신규 보조 특성 및 계열](https://playeternalreturn.com/posts/news/3305?hl=ko-KR), [혼돈 벽력 확인](https://playeternalreturn.com/posts/news/3107?hl=ko-KR), [사냥의 전율 출시](https://playeternalreturn.com/posts/news/1515?hl=ko-KR)다. 철갑탄은 이후 혼돈 계열로 이동했으므로 파괴 보조로 넣지 않았다. 숫자는 원본의 백분율과 조건부 효과를 턴 전투 수치로 바꾼 팬 밸런스다.

## 장비와 제작

장비의 이름·부위·전설/초월 분류·희귀 재료를 참고했다. 공격력, 방어도, 최대 체력, 회피율과 발동 보너스의 숫자는 본 게임용이다. 원본의 일반 재료는 모닥불에 기본 제공된다는 설정으로 생략하여 특별 오브젝트 1개만 소모한다. 진홍 등 파생 장비의 샤드 공정도 이 제작 단계로 합친다. 슬롯마다 최대 2개 착용은 사용자의 규칙이다.

| 부위 | 포함 장비 |
|---|---|
| 무기 | 레바테인, 다인슬라이프 - 진홍, 알타이르, 악켈테, 블러디 핸즈, 저거너트, 미스틸테인, 이글 아이, 안드로메다 |
| 옷 | 미스릴 갑옷, 카바나, 타이탄 아머, 아오자이, 퀸 오브 하트 |
| 머리 | 미스릴 투구, 인사이트, 택티컬 바이저, 황야의 별, 변검 |
| 팔/장식 | 미스릴 방패, 큐브 워치, 스카디의 팔찌, 오토-암즈, 프로미넌스 |
| 다리 | 미스릴 부츠, 글레이셜 슈즈, 갤럭시 스텝, 헤르메스의 부츠, 분홍신 |

기본 명단과 재료는 [1.0 아이템 안내](https://playeternalreturn.com/posts/news/1282?hl=ko-KR)에서 연결한 [공식 공개 아이템 표](https://docs.google.com/spreadsheets/d/e/2PACX-1vTjbw1TSUfgr0KwrMIW0YSjvYvtQEHnFrHX54Qgz-DpbPPtxz801OAL7dQKgkigAB6EHktuYYgZGRsG/pubhtml)를 확인했다. 이후 변경은 다음 문서를 적용했다.

- [1.13 저거너트 변경](https://playeternalreturn.com/posts/news/1673?hl=ko-KR): 전설 등급 도끼, 생명의 나무.
- [1.15 초월 개편](https://playeternalreturn.com/posts/news/1727?hl=ko-KR), [1.15 아이템 표](https://playeternalreturn.com/posts/news/1728?hl=ko-KR): VF 혈액 샘플 장비와 파생 무기, 알타이르/악켈테의 미스릴 제작.
- [1.15.1 카바나](https://playeternalreturn.com/posts/news/1749?hl=ko-KR), [1.18 타이탄 아머/갤럭시 스텝](https://playeternalreturn.com/posts/news/1812?hl=ko-KR), [1.22 아오자이](https://playeternalreturn.com/posts/news/1920?hl=ko-KR).
- [9.0 제작 간소화](https://playeternalreturn.com/posts/news/3085?hl=ko-KR): 레바테인·스카디의 팔찌·글레이셜 슈즈는 생명의 나무, 큐브 워치는 운석.
- [10.0 아이템](https://playeternalreturn.com/posts/news/3306?hl=ko-KR): 알타이르와 이글 아이는 미스릴, 안드로메다는 운석.
- [12.0 아이템](https://playeternalreturn.com/posts/news/3743?hl=ko-KR): 인사이트는 운석, 택티컬 바이저는 생명의 나무. [12.4](https://playeternalreturn.com/posts/news/3838?hl=ko-KR)에서 레바테인·다인슬라이프 - 진홍·갤럭시 스텝의 존재도 확인했다.

## 음식과 모닥불

모닥불 업그레이드는 원본의 재료 음식 + 모닥불 관계를 유지한다. 임의의 완성 음식에 더 높은 회복량을 붙이는 방식으로 만들지 않았다.

| 재료 | 모닥불 결과 |
|---|---|
| 감자 | 감자튀김 |
| 고기 | 웰던 스테이크 |
| 연어 | 연어 스테이크 |
| 호박고구마 | 꿀고구마 |
| 트러플 | 트러플 파스타 |

감자와 고기는 [공식 아이템 표](https://docs.google.com/spreadsheets/d/e/2PACX-1vTjbw1TSUfgr0KwrMIW0YSjvYvtQEHnFrHX54Qgz-DpbPPtxz801OAL7dQKgkigAB6EHktuYYgZGRsG/pubhtml), 연어와 호박고구마는 [1.0 게임 플레이](https://playeternalreturn.com/posts/news/1281?hl=ko-KR), 트러플 파스타와 만년 스프는 [1.43 아이템](https://playeternalreturn.com/posts/news/2585?hl=ko-KR)을 참고했다. 원본의 한 번에 여러 개 생산하는 수량은 이 게임에서는 1개로 바꿨다. 제작과 요리를 합쳐 모닥불당 3번 가능한 제한은 사용자 규칙이다.

상점 완성 음식은 후라이드 치킨·피쉬 앤 칩스·수박과 위 결과 음식이다. 회복량·판매가는 팬 게임 체력과 크레딧에 맞춰 조정했다. 최신 변경은 [11.0 음식 개편](https://playeternalreturn.com/posts/news/3531?hl=ko-KR), [12.0 음식 개편](https://playeternalreturn.com/posts/news/3743?hl=ko-KR)을 확인했다. 12.0에서 삭제된 오므라이스는 포함하지 않는다. 만년 스프의 **1회 완전 회복**은 원본의 고정량 지속 회복을 변경한 사용자 요청이다.

## 오브젝트와 보상

운석/생명의 나무/미스릴/포스코어/VF 혈액 샘플의 가격은 각각 200/200/250/350/500 크레딧으로 사용자의 값을 그대로 적용했다. 이 값과 전투 크레딧·경험치·확률은 원본 패치 데이터가 아닌 이 게임의 경제 설계다. 늑대의 운석/생명의 나무와 곰의 네 가지 오브젝트 획득 원형은 [1.0 게임 플레이](https://playeternalreturn.com/posts/news/1281?hl=ko-KR)를 참고했다. 닭·들개·멧돼지에서는 특별 오브젝트를 주지 않고, VF 혈액 샘플은 일반 전투 드롭에서 제외한다.

## 출시 명단과 조우 이벤트

출시 명단은 [1.0 실험체 자료](https://playeternalreturn.com/posts/news/1280?hl=ko-KR)의 64명에 공식 출시 자료에서 확인한 27명을 더했다. 샬럿은 [1.19 출시 자료](https://playeternalreturn.com/posts/news/1843?hl=ko-KR)에서 보완했다. 캐릭터별 이벤트는 원본 스킬·설정의 소재를 참고한 별도의 짧은 팬 이야기다. 공식 대사를 복사하지 않았다.

기존 64명: 재키, 아야, 현우, 유키, 혜진, 수아, 아이솔, 나딘, 엠마, 나타폰, 니키, 다니엘, 띠아, 라우라, 레녹스, 레온, 로지, 루크, 리 다이린, 리오, 마르티나, 마이, 마커스, 매그너스, 바냐, 바바라, 버니스, 비앙카, 셀린, 쇼우, 쇼이치, 시셀라, 실비아, 아델라, 아드리아나, 아디나, 아이작, 알렉스, 얀, 에스텔, 에이든, 에키온, 엘레나, 요한, 윌리엄, 이렘, 이바, 이안, 일레븐, 자히르, 제니, 카밀로, 칼라, 캐시, 클로에, 키아라, 타지아, 테오도르, 펠릭스, 프리야, 피오라, 피올로, 하트, 헤이즈.

| 추가 실험체 | 공식 자료 |
|---|---|
| 데비&마를렌 | [출시 안내](https://playeternalreturn.com/posts/news/1286?hl=ko-KR) |
| 아르다 | [1.2 출시](https://playeternalreturn.com/posts/news/1354?hl=ko-KR) |
| 아비게일 | [1.3 출시](https://playeternalreturn.com/posts/news/1381?hl=ko-KR) |
| 알론소 | [1.6 출시](https://playeternalreturn.com/posts/news/1480?hl=ko-KR) |
| 레니 | [출시 소개](https://event.playeternalreturn.com/newcharacter/Leni?hl=ko-KR) |
| 츠바메 | [70번째 실험체 소개](https://event.playeternalreturn.com/newcharacter/70th/Tsubame?hl=ko-KR) |
| 케네스 | [71번째 실험체 소개](https://event.playeternalreturn.com/newcharacter/71/Kenneth?hl=ko-KR) |
| 카티야 | [1.16 출시](https://playeternalreturn.com/posts/news/1765?hl=ko-KR) |
| 샬럿 | [1.19 출시](https://playeternalreturn.com/posts/news/1843?hl=ko-KR) |
| 다르코 | [1.23 출시](https://playeternalreturn.com/posts/news/1954?hl=ko-KR) |
| 르노어 | [1.26 출시](https://playeternalreturn.com/posts/news/2038?hl=ko-KR) |
| 가넷 | [실험체 소개](https://playeternalreturn.com/posts/news/2170?hl=ko-KR) |
| 유민 | [1.33 출시](https://playeternalreturn.com/posts/news/2277?hl=ko-KR) |
| 히스이 | [1.37 출시](https://playeternalreturn.com/posts/news/2401?hl=ko-KR) |
| 유스티나 | [1.40 출시](https://playeternalreturn.com/posts/news/2513?hl=ko-KR) |
| 이슈트반 | [1.44 출시](https://playeternalreturn.com/posts/news/2623?hl=ko-KR) |
| 니아 | [1.47 출시](https://playeternalreturn.com/posts/news/2702?hl=ko-KR) |
| 슈린 | [8.2 출시](https://playeternalreturn.com/posts/news/2885?hl=ko-KR) |
| 헨리 | [시즌 8 실험체 소개](https://event.playeternalreturn.com/S8/Henry?hl=ko-KR) |
| 블레어 | [시즌 9 실험체 소개](https://event.playeternalreturn.com/S9/Blair?hl=ko-KR) |
| 미르카 | [시즌 9 실험체 소개](https://event.playeternalreturn.com/S9/Mirka?hl=ko-KR) |
| 펜리르 | [10.2 출시](https://playeternalreturn.com/posts/news/3340?hl=ko-KR) |
| 코렐라인 | [10.5 출시](https://playeternalreturn.com/posts/news/3444?hl=ko-KR) |
| 비형 | [시즌 11 실험체 소개](https://event.playeternalreturn.com/S11/Bihyung?hl=ko-KR) |
| 크레이버 | [11.5 출시](https://playeternalreturn.com/posts/news/3657?hl=ko-KR) |
| 루치아 | [12.2 출시](https://playeternalreturn.com/posts/news/3783?hl=ko-KR) |
| 세레스 | [공식 신규 캐릭터 소개와 2026-10-01 출시 이벤트](https://event.playeternalreturn.com/ER/CHARACTER%26SKIN?hl=ko-KR) |

위클라인 같은 NPC는 플레이 가능한 실험체 91명 명단에 섞지 않는다. 조우의 카드 보상은 해당 실험체 자신의 원본 스킬에서 변환한 카드를 사용한다.
