# 명중률과 필중 변환 기준

확인일: 2026-10-16. 직접 공격은 공격자 명중 판정 → 방어자 회피 판정을 각각 수행한다. 기본 명중률은 100%, 실명은 25%p 감소시킨다. 따라서 명중률 75% / 회피율 50%일 때 실제 적중 확률은 37.5%다. 명중 실패는 `missed`, 실제 회피는 `avoided`로 기록하며 회피 성공 패시브는 후자에만 반응한다. 설치물·지속 피해·패시브 후속 피해에는 새로운 명중 굴림을 추가하지 않았다.

속박과 기절은 기존 사용 제한 / 코스트 감소를 유지하면서 회피율을 20%p 낮춘다. 동시에 걸려도 감소량은 20%p이며 최저 0%다. 지속 시간은 기존 상태이상과 같이 영향을 받은 대상의 턴 종료 기준이다. 이 수치와 명중률 변환은 팬 게임의 턴 전투 밸런스다.

## 필중 목록

아래의 직접 공격만 명중률 감소와 대상 회피를 모두 무시한다. 이동 카드, D 카드, 무료 사용권의 대상, 일반적인 ‘적에게 피해’ 문구로 추정하지 않는다. 기본 공격 강화 카드는 기본 공격의 명중·회피 규칙을 유지한다.

| 근거 | 직접 공격 카드 |
|---|---|
| [공식 무기 설명](https://playeternalreturn.com/posts/news/1283?hl=en-US), [12.0 무기 변경](https://playeternalreturn.com/posts/news/3743?hl=en-US) | 글러브 D `weapon_glove`, 레이피어 D `weapon_rapier`, 단검 D `weapon_dagger` |
| [공식 실험체 설명](https://playeternalreturn.com/posts/news/1280?hl=en-US) | 아야 Q, 수아 W, 엠마 E, 니키 R, 다니엘 W/R, 로지 E, 루크 E, 마이 E, 매그너스 E, 쇼이치 W, 아디나 E, 카밀로 E, 키아라 R |
| [아비게일 출시](https://playeternalreturn.com/posts/news/1381?hl=en-US) | 아비게일 E: 적이 있어야 순간이동이 성립하는 대상 획득 기술 |
| [알론소 출시](https://playeternalreturn.com/posts/news/1480?hl=en-US) | 알론소 E |
| [츠바메 출시](https://playeternalreturn.com/posts/news/1612?hl=en-US) | 츠바메 R |
| [카티야 출시](https://playeternalreturn.com/posts/news/1765?hl=en-US) | 카티야 R: 포착한 실험체를 향하는 사격 |
| [이슈트반 출시](https://playeternalreturn.com/posts/news/2623?hl=en-US) | 이슈트반 R |
| [펜리르 출시](https://playeternalreturn.com/posts/news/3340?hl=en-US) | 펜리르 R |

조건부 필중:

- 실비아 E는 바이크 상태의 적 지정 윌리일 때만 필중이다. 기본 형태의 방향 공격은 일반 판정을 한다. [공식 설명](https://playeternalreturn.com/posts/news/1280?hl=en-US)
- 가넷 R은 철처녀 표식이 남아 있고 이번 턴 무료 재사용권으로 재사용할 때만 필중이다. 첫 방향 사슬과 표식이 사라진 재사용은 일반 판정이다. 팬 게임의 기존 처형 체력 기준 20%와 소멸 / 무료권 소모는 유지한다. [가넷 출시](https://playeternalreturn.com/posts/news/2179?hl=en-US)

방향 / 범위 / 설치물은 필중으로 분류하지 않는다. 특히 블레어 R은 감지 대상의 위치에 도착한 뒤 범위 피해를 주므로 제외했고, 루치아 R도 방향 공격이므로 제외했다. 가넷 E는 [10.0에서 적 지정 → 방향 돌진으로 개편](https://playeternalreturn.com/posts/news/3306?hl=en-US)되어 필중에서 제외한다.

## 명중 감소 기술

카메라 D, 수아 W의 기존 실명을 새 명중률 계산에 연결하고 다니엘 W의 원작 시야 감소도 실명으로 변환했다. 시야 감소가 공격 실패율을 만드는 수치인 것은 이 팬 게임의 규칙이다. [카메라 설명](https://playeternalreturn.com/posts/news/1283?hl=en-US), [수아 / 다니엘 설명](https://playeternalreturn.com/posts/news/1280?hl=en-US)

투척 D는 과거 시야 감소 효과가 있었지만 [12.0 개편](https://playeternalreturn.com/posts/news/3743?hl=en-US)에서 두 번의 피해와 둔화로 바뀌어 이번 실명 대상에 추가하지 않았다. 이미 적용된 상태의 중복 부여는 기존처럼 지속 시간만 갱신하며 실명 감소량을 합산하지 않는다.

회피 가능한 보스 특수 공격과 야생동물 기술도 공격자 명중률을 사용한다. 보스 특수 기믹은 원천 카드와 별도의 공격이므로 원천 카드 ID가 필중 목록에 있다는 이유만으로 필중이 되지 않는다. 기존에 회피 불가능한 보스 반격 / 지속 피해는 그대로 확정 처리한다.

저장 형식의 버전은 올리지 않는다. 명중률 / 회피 감소는 저장된 상태이상에서 다시 계산하고, 이전 전투 기록에 없는 `missed` 필드는 0으로 읽는다.
