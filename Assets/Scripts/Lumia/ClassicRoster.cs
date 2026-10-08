using System;
using System.Collections.Generic;

namespace Lumia
{
    // Official names and cooldown snapshots: docs/ROSTER_CLASSIC.md.
    // Every damage/status number below is authored for this turn-based fan game.
    public static class ClassicRoster
    {
        public static void Populate(List<CardDef> cards, List<PassiveDef> passives, List<CharacterDef> characters)
        {
            // 나타폰
            Subject(cards, passives, characters, "nathapon", "나타폰",
                "슬로우 셔터", "attack_bonus", 3, "기본 공격 카드의 피해량이 3 증가합니다.",
                S("스냅샷", "Q", 5f, damage:8, vulnerable:1),
                S("타임 랩스", "W", 16f, damage:3, hits:3, weak:1),
                S("인스턴트 포토", "E", 15f, damage:8, vulnerable:2),
                S("셔터 찬스", "R", 80f, weak:3, draw:2, block:12, exhaust:true));

            // 니키
            Subject(cards, passives, characters, "nicky", "니키",
                "다혈질", "battle_start_strength", 2, "전투 시작 시 힘이 2 증가합니다.",
                S("격투 액션", "Q", 15f, damage:10, free:"q"),
                S("가드 & 카운터", "W", 10f, damage:6, block:12),
                S("강력한 펀치 / 분노의 펀치!", "E", 14f, damage:14, weak:1),
                S("분노의 어퍼컷!", "R", 80f, damage:30, vulnerable:2, exhaust:true));

            // 다니엘
            Subject(cards, passives, characters, "daniel", "다니엘",
                "고독한 예술가", "battle_start_evasion", 12, "전투 시작 시 회피율이 12% 증가합니다.",
                S("그림자 가위", "Q", 10f, damage:10, draw:1),
                S("영감", "W", 14f, vulnerable:3, poison:3),
                S("그림자 이동", "E", 12f, damage:8, evasion:25),
                S("걸작", "R", 70f, damage:6, hits:5, heal:4, exhaust:true));

            // 띠아
            Subject(cards, passives, characters, "tia", "띠아",
                "알록달록 컬러믹스", "skill_bonus", 3, "스킬 카드의 피해량이 3 증가합니다.",
                S("브러쉬 스트로크", "Q", 0.5f, damage:4, weak:1),
                S("팔레트", "W", 0.02f, block:4, poison:1),
                S("색칠놀이", "E", 18f, damage:12, evasion:25),
                S("무지개 드로잉", "R", 90f, damage:5, hits:5, weak:2, heal:5, exhaust:true));

            // 라우라
            Subject(cards, passives, characters, "laura", "라우라",
                "괴도", "skill_bonus", 2, "스킬 카드의 피해량이 2 증가합니다.",
                S("날카로운 꽃", "Q", 1f, damage:4),
                S("예고장", "W", 14f, damage:11, vulnerable:2),
                S("우아한 발걸음", "E", 0.5f, damage:3, evasion:15),
                S("황혼의 도둑", "R", 50f, damage:25, evasion:25, draw:1, exhaust:true));

            // 레녹스
            Subject(cards, passives, characters, "lenox", "레녹스",
                "위풍당당", "battle_start_block", 12, "전투 시작 시 방어도를 12 얻습니다.",
                S("회오리 비늘", "Q", 3.5f, damage:9, block:2, free:"q"),
                S("날카로운 독니", "W", 18f, damage:12, weak:2),
                S("휩쓸기", "E", 8.5f, damage:10, vulnerable:1),
                S("푸른뱀", "R", 80f, damage:5, hits:4, poison:5, exhaust:true));

            // 레온
            Subject(cards, passives, characters, "leon", "레온",
                "인간 어뢰", "battle_start_evasion", 10, "전투 시작 시 회피율이 10% 증가합니다.",
                S("물길", "Q", 10f, damage:10, weak:1),
                S("물보라", "W", 12f, block:10, strength:1),
                S("잠영", "E", 11f, evasion:30, heal:5),
                S("파도타기", "R", 80f, damage:26, weak:2, exhaust:true));

            // 로지
            Subject(cards, passives, characters, "rozzi", "로지",
                "더블샷", "attack_bonus", 3, "기본 공격 카드의 피해량이 3 증가합니다.",
                S("이지샷", "Q", 5.5f, damage:9, evasion:10, free:"q"),
                S("스핀샷", "W", 6f, damage:8, block:5, free:"w"),
                S("에어샷", "E", 22f, damage:6, hits:2, evasion:25),
                S("셈텍스탄 Mk-II", "R", 25f, damage:16, poison:3, free:"r"));

            // 루크
            Subject(cards, passives, characters, "luke", "루크",
                "청소 완료", "kill_heal", 9, "전투에서 승리하면 체력을 9 회복합니다.",
                S("클리닝 서비스", "Q", 16f, damage:11, vulnerable:1),
                S("강박증", "W", 6f, strength:2, damage:5),
                S("무소음 청소기", "E", 14f, damage:9, evasion:20),
                S("애프터 서비스", "R", 80f, damage:29, weak:2, free:"q", exhaust:true));

            // 리 다이린
            Subject(cards, passives, characters, "dailin", "리 다이린",
                "취기", "attack_bonus", 3, "기본 공격 카드의 피해량이 3 증가합니다.",
                S("호연각", "Q", 10f, damage:4, hits:3),
                S("술 마시기", "W", 10f, block:12, heal:4, evasion:15),
                S("술 뿌리기", "E", 10f, damage:9, weak:2),
                S("취호격파산", "R", 80f, damage:6, hits:5, weak:2, exhaust:true));

            // 리오
            Subject(cards, passives, characters, "rio", "리오",
                "카이", "skill_bonus", 3, "스킬 카드의 피해량이 3 증가합니다.",
                S("카에유미", "Q", 1f, strength:1, exhaust:true),
                S("하나레", "W", 5.5f, damage:10, vulnerable:1, free:"qwe"),
                S("비상", "E", 20f, damage:10, evasion:30),
                S("연사/정사필중", "R", 80f, damage:5, hits:4, free:"r"));

            // 마르티나
            Subject(cards, passives, characters, "martina", "마르티나",
                "재생", "battle_start_strength", 2, "전투 시작 시 힘이 2 증가합니다.",
                S("빨리감기", "Q", 8f, damage:9, vulnerable:1, free:"q"),
                S("일시정지", "W", 14f, weak:2, poison:2),
                S("되감기", "E", 17f, damage:10, evasion:20, draw:1),
                S("녹화", "R", 30f, damage:18, strength:2, draw:1, exhaust:true));

            // 마이
            Subject(cards, passives, characters, "mai", "마이",
                "오뜨꾸뛰르", "attack_bonus", 3, "기본 공격 카드의 피해량이 3 증가합니다.",
                S("드레이프", "Q", 5f, damage:8, block:4),
                S("숄 장막", "W", 28f, damage:10, block:16),
                S("캣 워크", "E", 18f, damage:10, block:8, evasion:25),
                S("익스클루시브", "R", 90f, block:24, heal:18, exhaust:true));

            // 마커스
            Subject(cards, passives, characters, "markus", "마커스",
                "전사의 투지", "skill_bonus", 3, "스킬 카드의 피해량이 3 증가합니다.",
                S("전투 교범", "Q", 7f, damage:11, block:3),
                S("파괴", "W", 14f, damage:13, vulnerable:2),
                S("전사의 돌격", "E", 11f, damage:12, weak:1),
                S("지각변동", "R", 70f, damage:20, vulnerable:2, free:"r"));

            // 매그너스
            Subject(cards, passives, characters, "magnus", "매그너스",
                "근성", "turn_block", 4, "매 턴 시작 시 방어도를 4 얻습니다.",
                S("파쇄탄", "Q", 13f, damage:12, weak:2),
                S("17대 1", "W", 14f, damage:3, hits:4, free:"w"),
                S("강타", "E", 11f, damage:13, weak:1),
                S("폭주 바이크", "R", 90f, damage:30, vulnerable:2, exhaust:true));

            // 바냐
            Subject(cards, passives, characters, "vanya", "바냐",
                "몽환 나비", "turn_block", 4, "매 턴 시작 시 방어도를 4 얻습니다.",
                S("꿈 길잡이", "Q", 11f, damage:10, block:5, free:"q"),
                S("쪽빛 바람", "W", 7f, block:12, evasion:15),
                S("염원", "E", 18f, damage:8, evasion:30),
                S("꿈결 가루", "R", 90f, damage:24, weak:3, block:10, exhaust:true));

            // 바바라
            Subject(cards, passives, characters, "barbara", "바바라",
                "개조", "skill_bonus", 3, "스킬 카드의 피해량이 3 증가합니다.",
                S("BT-Mk2 센트리건", "Q", 12f, damage:8, poison:3, free:"q"),
                S("이온 레이저", "W", 5f, damage:5, hits:2, free:"r"),
                S("자력 폭풍", "E", 18f, damage:12, block:8),
                S("오버클럭", "R", 70f, strength:3, draw:2, free:"qwe", exhaust:true));

            // 버니스
            Subject(cards, passives, characters, "bernice", "버니스",
                "산탄", "attack_bonus", 4, "기본 공격 카드의 피해량이 4 증가합니다.",
                S("레그샷", "Q", 7f, damage:11, weak:1),
                S("사냥 덫", "W", 18f, poison:5, weak:1),
                S("매의 눈", "E", 14f, evasion:20, vulnerable:1),
                S("올가미 탄", "R", 85f, damage:7, hits:4, weak:2, exhaust:true));

            // 비앙카
            Subject(cards, passives, characters, "bianca", "비앙카",
                "흡혈귀", "turn_heal", 3, "매 턴 시작 시 체력을 3 회복합니다.",
                S("선혈의 투창", "Q", 10f, damage:11, heal:4, weak:1),
                S("짧은 안식", "W", 18f, heal:12, block:8, free:"qer"),
                S("순환", "E", 16f, damage:12, heal:5, evasion:15),
                S("진조의 군림", "R", 90f, damage:6, hits:4, heal:12, exhaust:true));

            // 셀린
            Subject(cards, passives, characters, "celine", "셀린",
                "폭발물 전문가", "attack_bonus", 3, "기본 공격 카드의 피해량이 3 증가합니다.",
                S("플라즈마 폭탄", "Q", 7f, damage:10, poison:2),
                S("기폭", "W", 0f, damage:3, free:"q"),
                S("블라스트 웨이브", "E", 16f, damage:10, evasion:20, weak:1),
                S("자력 융합", "R", 1.25f, damage:5, hits:2, vulnerable:1));

            // 쇼우
            Subject(cards, passives, characters, "sho", "쇼우",
                "요리사의 열정", "turn_heal", 3, "매 턴 시작 시 체력을 3 회복합니다.",
                S("소스범벅", "Q", 8f, damage:10, weak:1),
                S("식사 시간", "W", 25f, block:15, heal:8),
                S("웍 돌진", "E", 22f, damage:11, weak:1, free:"e"),
                S("뜨거운 맛", "R", 80f, damage:5, hits:5, poison:4, exhaust:true));

            // 쇼이치
            Subject(cards, passives, characters, "shoichi", "쇼이치",
                "부당거래", "skill_bonus", 3, "스킬 카드의 피해량이 3 증가합니다.",
                S("표리", "Q", 8f, damage:10, free:"q"),
                S("비약", "W", 16f, damage:12, evasion:15, free:"w"),
                S("협상", "E", 14f, damage:12, vulnerable:2, free:"e"),
                S("무자비", "R", 90f, damage:5, hits:6, draw:1, exhaust:true));

            // 시셀라
            Subject(cards, passives, characters, "sissela", "시셀라",
                "삶은 고통이에요.", "turn_heal", 3, "매 턴 시작 시 체력을 3 회복합니다.",
                S("윌슨! 도와줘", "Q", 8f, damage:10, poison:2),
                S("어딨어 윌슨?", "W", 22f, block:18, evasion:20),
                S("나랑 놀자", "E", 12f, damage:11, block:6, free:"e"),
                S("모두 해방이에요.", "R", 100f, damage:32, heal:5, exhaust:true));

            // 실비아
            Subject(cards, passives, characters, "silvia", "실비아",
                "그란투리스모", "battle_start_strength", 2, "전투 시작 시 힘이 2 증가합니다.",
                S("스피드건", "Q", 6f, damage:9, heal:4),
                S("피니시라인", "W", 14f, damage:12, weak:2),
                S("스페어휠", "E", 18f, damage:11, evasion:25),
                S("기동전", "R", 8f, damage:11, evasion:30));

            // 아델라
            Subject(cards, passives, characters, "adela", "아델라",
                "퀸즈 갬빗 디클라인드", "skill_bonus", 3, "스킬 카드의 피해량이 3 증가합니다.",
                S("프로모션", "Q", 3.75f, damage:7),
                S("나이트 포크", "W", 14f, damage:6, hits:2, weak:1),
                S("캐슬링", "E", 20f, damage:12, evasion:20),
                S("체크메이트", "R", 80f, damage:30, block:10, exhaust:true));

            // 아드리아나
            Subject(cards, passives, characters, "adriana", "아드리아나",
                "활활", "skill_bonus", 3, "스킬 카드의 피해량이 3 증가합니다.",
                S("방화", "Q", 6f, damage:3, hits:3, poison:2),
                S("기름 뿌리기", "W", 13f, poison:5, vulnerable:2),
                S("불길 쇄도", "E", 20f, damage:12, evasion:20),
                S("화염 난사", "R", 26f, damage:7, hits:3, poison:4, exhaust:true));

            // 아디나
            Subject(cards, passives, characters, "adina", "아디나",
                "별읽기", "battle_start_evasion", 10, "전투 시작 시 회피율이 10% 증가합니다.",
                S("루미너리", "Q", 6f, damage:9, weak:1),
                S("트라인 에스펙트", "W", 9f, damage:11, block:6),
                S("폴 디그니티", "E", 10f, damage:5, hits:2, heal:4),
                S("수정구에 비친 운명", "R", 0f, draw:1, block:4, exhaust:true));

            // 아이작
            Subject(cards, passives, characters, "isaac", "아이작",
                "착취", "turn_heal", 3, "매 턴 시작 시 체력을 3 회복합니다.",
                S("현장 급습", "Q", 10f, damage:10, vulnerable:1),
                S("경화", "W", 9f, strength:2, block:5, free:"qwe"),
                S("추격 / 검거", "E", 15f, damage:12, evasion:20),
                S("강탈", "R", 80f, damage:28, heal:8, vulnerable:2, exhaust:true));

            // 알렉스
            Subject(cards, passives, characters, "alex", "알렉스",
                "잠입", "battle_start_evasion", 12, "전투 시작 시 회피율이 12% 증가합니다.",
                S("코일건", "Q", 7f, damage:10, strength:1),
                S("타겟 마커", "W", 13f, damage:12, vulnerable:2),
                S("펄스 스팅", "E", 12f, damage:10, evasion:30),
                S("정밀 폭격", "R", 90f, damage:3, hits:10, weak:2, exhaust:true));

            // 얀
            Subject(cards, passives, characters, "jan", "얀",
                "열혈의 의지", "skill_bonus", 3, "스킬 카드의 피해량이 3 증가합니다.",
                S("니 스트라이크", "Q", 12f, damage:11, free:"e"),
                S("토마호크 스핀", "W", 17f, damage:13, weak:2),
                S("위빙", "E", 16f, block:7, evasion:35, free:"e"),
                S("쿼드라곤", "R", 80f, damage:25, weak:2, vulnerable:2, exhaust:true));

            // 에스텔
            Subject(cards, passives, characters, "estelle", "에스텔",
                "사명감", "turn_heal", 3, "매 턴 시작 시 체력을 3 회복합니다.",
                S("진압", "Q", 7f, damage:9, weak:1),
                S("선제대응", "W", 11f, damage:12, block:8),
                S("방패방어", "E", 15f, block:22, weak:2),
                S("헬기호출", "R", 100f, heal:24, block:22, exhaust:true));

            // 에이든
            Subject(cards, passives, characters, "aiden", "에이든",
                "과전하", "attack_bonus", 3, "기본 공격 카드의 피해량이 3 증가합니다.",
                S("뇌격 / 전자포", "Q", 5f, damage:9),
                S("전하 소산", "W", 15f, damage:12, block:5),
                S("백스텝 / 볼트 러시", "E", 20f, damage:10, evasion:20),
                S("낙뢰", "R", 80f, damage:26, weak:2, draw:1, exhaust:true));

            // 에키온
            Subject(cards, passives, characters, "echion", "에키온",
                "카드모스의 부름", "skill_bonus", 3, "스킬 카드의 피해량이 3 증가합니다.",
                S("독사의 칼날", "Q", 5f, damage:5, hits:2, weak:1),
                S("뒤집힌 비늘", "W", 10f, block:14),
                S("메마른 송곳니", "E", 17f, damage:9, evasion:20, free:"e"),
                S("VF폭주 / 독사의 진노", "R", 3.5f, damage:14, poison:2, free:"e"));

            // 엘레나
            Subject(cards, passives, characters, "elena", "엘레나",
                "겨울여왕의 영지", "skill_bonus", 3, "스킬 카드의 피해량이 3 증가합니다.",
                S("크리스탈 엘레강스", "Q", 8f, damage:5, hits:2, weak:1),
                S("더블 악셀", "W", 16f, damage:11, block:8, evasion:15, free:"qw"),
                S("스파이럴", "E", 5f, evasion:25, block:4),
                S("죽음의 무도", "R", 80f, damage:28, weak:3, exhaust:true));

            // 요한
            Subject(cards, passives, characters, "johann", "요한",
                "빛의 가호", "turn_block", 4, "매 턴 시작 시 방어도를 4 얻습니다.",
                S("찬란한 광휘", "Q", 12f, damage:9, heal:6),
                S("신성의 향로", "W", 24f, heal:12, block:12, free:"w"),
                S("인도하는 빛", "E", 20f, block:8, evasion:25),
                S("구원의 성역", "R", 100f, heal:24, block:20, strength:1, exhaust:true));

            // 윌리엄
            Subject(cards, passives, characters, "william", "윌리엄",
                "캐치볼", "attack_bonus", 3, "기본 공격 카드의 피해량이 3 증가합니다.",
                S("쉐도우 볼", "Q", 7f, strength:2, block:4),
                S("와인드업", "W", 16f, damage:5, hits:2, vulnerable:1),
                S("슬라이딩 캐치", "E", 0f, evasion:20, damage:3),
                S("위닝샷", "R", 70f, damage:27, draw:1, exhaust:true));

            // 이렘
            Subject(cards, passives, characters, "irem", "이렘",
                "고양이의 습성", "turn_block", 3, "매 턴 시작 시 방어도를 3 얻습니다.",
                S("바운싱 볼", "Q", 7f, damage:10),
                S("친구할래요?", "W", 15f, damage:10, weak:2),
                S("사뿐~", "E", 20f, evasion:30, block:7),
                S("고양이로 펑!", "R", 1.5f, strength:1, block:4, exhaust:true));

            // 이바
            Subject(cards, passives, characters, "eva", "이바",
                "텔레키네시스", "skill_bonus", 3, "스킬 카드의 피해량이 3 증가합니다.",
                S("빛의 트라이어드", "Q", 9f, damage:11, free:"q"),
                S("위상의 소용돌이", "W", 11f, damage:12, vulnerable:2),
                S("자수정의 물결", "E", 10f, evasion:35, block:8),
                S("VF 방출", "R", 0f, damage:4, hits:4, poison:2, exhaust:true));

            // 이안
            Subject(cards, passives, characters, "ian", "이안",
                "사로잡힌 육신", "turn_heal", 3, "매 턴 시작 시 체력을 3 회복합니다.",
                S("피하세요!", "Q", 5f, damage:9, free:"q"),
                S("미안해요..", "W", 11f, damage:10, weak:2),
                S("비키세요!", "E", 16f, damage:8, evasion:20, free:"e"),
                S("해방 / 넌 벗어날 수 없어", "R", 76f, damage:27, heal:6, strength:2, exhaust:true));

            // 일레븐
            Subject(cards, passives, characters, "eleven", "일레븐",
                "힘내자고!", "turn_heal", 4, "매 턴 시작 시 체력을 4 회복합니다.",
                S("방해하지마!", "Q", 5.5f, damage:10),
                S("자~집중!", "W", 15f, block:15, weak:2),
                S("나 불렀어?", "E", 18f, damage:12, evasion:20),
                S("다 덤벼보라구!", "R", 80f, damage:25, heal:10, block:10, exhaust:true));

            // 자히르
            Subject(cards, passives, characters, "zahir", "자히르",
                "사신의 눈", "battle_start_evasion", 12, "전투 시작 시 회피율이 12% 증가합니다.",
                S("나라야나스트라", "Q", 7f, damage:11),
                S("간디바", "W", 0.45f, damage:3, free:"qe"),
                S("바이바야스트라", "E", 18f, damage:12, weak:2, evasion:10),
                S("바르가바스트라", "R", 80f, damage:6, hits:5, vulnerable:2, exhaust:true));

            // 제니
            Subject(cards, passives, characters, "jenny", "제니",
                "죽음의 연기", "battle_start_block", 15, "전투 시작 시 방어도를 15 얻습니다.",
                S("스포트라이트", "Q", 9f, damage:10, weak:1, free:"q"),
                S("레드 카펫", "W", 20f, damage:12, vulnerable:2),
                S("페르소나", "E", 12f, evasion:35, draw:1),
                S("시상식의 여왕", "R", 80f, damage:28, weak:3, free:"e", exhaust:true));

            // 카밀로
            Subject(cards, passives, characters, "camilo", "카밀로",
                "올레", "turn_block", 4, "매 턴 시작 시 방어도를 4 얻습니다.",
                S("브엘따", "Q", 5f, damage:10, heal:3),
                S("씨에레", "W", 18f, damage:13, weak:1),
                S("알 꼼빠스", "E", 0.5f, damage:3, evasion:15),
                S("두엔데", "R", 80f, damage:5, hits:5, heal:10, evasion:15, exhaust:true));

            // 칼라
            Subject(cards, passives, characters, "karla", "칼라",
                "작살 장전", "skill_bonus", 3, "스킬 카드의 피해량이 3 증가합니다.",
                S("관통 작살", "Q", 0.75f, damage:4),
                S("회수", "W", 2.5f, damage:3, vulnerable:1, free:"e"),
                S("작살 기동", "E", 22f, damage:12, evasion:30),
                S("구속의 사슬", "R", 80f, damage:28, weak:3, exhaust:true));

            // 캐시
            Subject(cards, passives, characters, "cathy", "캐시",
                "외과 전문의", "skill_bonus", 3, "스킬 카드의 피해량이 3 증가합니다.",
                S("동맥절제술", "Q", 16f, damage:10, poison:3, free:"q"),
                S("앰퓨테이션", "W", 12f, damage:12, poison:3),
                S("수쳐", "E", 15f, damage:11, weak:2, evasion:15),
                S("이머전시 OP", "R", 80f, damage:30, heal:12, poison:5, exhaust:true));

            // 클로에
            Subject(cards, passives, characters, "chloe", "클로에",
                "살아 있는 마리오네트", "attack_bonus", 3, "기본 공격 카드의 피해량이 3 증가합니다.",
                S("공격 명령", "Q", 5f, damage:9),
                S("인형극", "W", 15f, damage:6, hits:2, block:4),
                S("퀼트 리퍼", "E", 16f, damage:12, evasion:20, free:"q"),
                S("생명 공유", "R", 75f, block:20, damage:24, strength:2, free:"we", exhaust:true));

            // 키아라
            Subject(cards, passives, characters, "chiara", "키아라",
                "낙인", "skill_bonus", 3, "스킬 카드의 피해량이 3 증가합니다.",
                S("부정의 손길", "Q", 9f, damage:10, vulnerable:1),
                S("뒤틀린 기도", "W", 19f, damage:12, block:12),
                S("집착", "E", 15f, damage:12, weak:2),
                S("폭주", "R", 60f, damage:26, heal:10, poison:4, exhaust:true));

            // 타지아
            Subject(cards, passives, characters, "tazia", "타지아",
                "아르띠지아나토", "skill_bonus", 3, "스킬 카드의 피해량이 3 증가합니다.",
                S("스틸레토/스파다", "Q", 0.75f, damage:4),
                S("피아스코", "W", 24f, damage:13, vulnerable:2),
                S("프리치아", "E", 16f, damage:12, evasion:20),
                S("파라디소", "R", 80f, damage:30, weak:2, exhaust:true));

            // 테오도르
            Subject(cards, passives, characters, "theodore", "테오도르",
                "에너지 프로토콜", "battle_start_block", 14, "전투 시작 시 방어도를 14 얻습니다.",
                S("에너지 포", "Q", 12f, damage:10, heal:5),
                S("증폭 스크린", "W", 14f, strength:2, block:10),
                S("스파크탄", "E", 14f, damage:12, weak:2),
                S("에너지 필드", "R", 80f, damage:30, heal:18, strength:1, exhaust:true));

            // 펠릭스
            Subject(cards, passives, characters, "felix", "펠릭스",
                "연계 창술", "attack_bonus", 3, "기본 공격 카드의 피해량이 3 증가합니다.",
                S("선풍참", "Q", 10f, damage:11),
                S("질풍뇌격", "W", 10f, damage:12, vulnerable:1),
                S("반월참", "E", 10f, damage:11, heal:4, free:"qwe"),
                S("뇌룡격", "R", 60f, damage:28, block:10, exhaust:true));

            // 프리야
            Subject(cards, passives, characters, "priya", "프리야",
                "자연의 응답", "turn_block", 4, "매 턴 시작 시 방어도를 4 얻습니다.",
                S("개화의 선율", "Q", 1f, damage:4, heal:2),
                S("포르타멘토", "W", 15f, block:12, evasion:25),
                S("프리비티의 노래", "E", 14f, damage:12, heal:6, weak:1),
                S("대지의 메아리", "R", 100f, damage:28, heal:15, weak:2, exhaust:true));

            // 피오라
            Subject(cards, passives, characters, "fiora", "피오라",
                "뚜셰", "skill_bonus", 3, "스킬 카드의 피해량이 3 증가합니다.",
                S("팡뜨", "Q", 8f, damage:11, free:"q"),
                S("아따끄 꽁뽀제", "W", 8f, damage:5, hits:2),
                S("마르셰 & 롱빼", "E", 10f, damage:12, evasion:25, free:"qw"),
                S("플레슈", "R", 70f, damage:30, weak:2));

            // 피올로
            Subject(cards, passives, characters, "piolo", "피올로",
                "단련광", "attack_bonus", 3, "기본 공격 카드의 피해량이 3 증가합니다.",
                S("쌍절난격&내려치기", "Q", 10f, damage:5, hits:2),
                S("튕겨내기&휘두르기", "W", 14f, damage:9, block:14),
                S("사슬묶기&올려치기", "E", 15f, damage:13, weak:2),
                S("응징자", "R", 6f, damage:11));

            // 하트
            Subject(cards, passives, characters, "hart", "하트",
                "Feedback", "attack_bonus", 3, "기본 공격 카드의 피해량이 3 증가합니다.",
                S("Delay", "Q", 4f, damage:10),
                S("Overdrive", "W", 16f, strength:2, evasion:15),
                S("Flanger", "E", 18f, damage:11, evasion:25),
                S("Peacemaker", "R", 80f, weak:3, heal:18, block:16, exhaust:true));

            // 헤이즈
            Subject(cards, passives, characters, "haze", "헤이즈",
                "웨폰 케이스", "attack_bonus", 3, "기본 공격 카드의 피해량이 3 증가합니다.",
                S("40mm 유탄", "Q", 2.5f, damage:5),
                S("산탄 포화", "W", 16f, damage:4, hits:3, free:"w"),
                S("기관단총", "E", 13f, damage:3, hits:4, evasion:15),
                S("로켓 런처", "R", 30f, damage:5, hits:5, exhaust:true));

        }

        static CardDef S(string name, string key, float cooldown, int damage=0, int block=0, int heal=0, int draw=0, int energy=0, int poison=0, int vulnerable=0, int weak=0, int strength=0, int evasion=0, int duration=2, int hits=1, bool exhaust=false, string free=null)
        {
            return new CardDef { name=name, key=key, cooldown=cooldown, cost=1, damage=damage, block=block, heal=heal, draw=draw, energy=energy, poison=poison, vulnerable=vulnerable, weak=weak, strength=strength, evasion=evasion, duration=duration, hits=hits, exhaust=exhaust, category="skill", description="", freeCastTargets=string.IsNullOrEmpty(free) ? Array.Empty<string>() : Slots(free), freeCastCount=string.IsNullOrEmpty(free) ? 0 : 1, freeCastOnHit=!string.IsNullOrEmpty(free) && damage>0 };
        }

        static string[] Slots(string slots)
        {
            var result = new string[slots.Length];
            for (int i=0; i<slots.Length; i++) result[i]=slots[i].ToString();
            return result;
        }

        static void Subject(List<CardDef> cards, List<PassiveDef> passives, List<CharacterDef> characters, string id, string owner, string passiveName, string trigger, int amount, string description, params CardDef[] skills)
        {
            var subject = characters.Find(x => x.id==id);
            if (subject==null) { subject=new CharacterDef { id=id, name=owner }; characters.Add(subject); }
            if (subject.cards!=null && subject.cards.Length>0) return;
            subject.passiveId=id+"_p";
            subject.cards=new string[skills.Length];
            for (int i=0; i<skills.Length; i++)
            {
                var card=skills[i]; card.id=id+"_"+card.key.ToLowerInvariant(); card.owner=owner;
                for (int t=0; t<card.freeCastTargets.Length; t++) card.freeCastTargets[t]=id+"_"+card.freeCastTargets[t];
                subject.cards[i]=card.id; cards.Add(card);
            }
            passives.Add(new PassiveDef { id=subject.passiveId, name=passiveName, owner=owner, trigger=trigger, amount=amount, description=description });
        }
    }
}
