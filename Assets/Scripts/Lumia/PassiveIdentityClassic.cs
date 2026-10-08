using System.Collections.Generic;

namespace Lumia
{
    // Passive identity uses global events. No rule filters cards by their original owner.
    public static class TraitIdentityClassic
    {
        public static void Populate(Dictionary<string, TraitMechanicProfile> p)
        {
            T(p, "nathapon_p", "기본 공격이 느린 셔터의 추가 피해를 주며, 적중한 스킬로 구도를 잡아 다음 스킬을 강화합니다.",
                R("before_basic", "bonus_damage", 2), R("after_skill", "gain", 1, "picturesque", "구도", 3, hit:true, turn:2),
                R("before_skill", "bonus_damage", 1, scale:"picturesque", cap:3));

            T(p, "nicky_p", "피해를 받으면 분노를 얻습니다. 다음 기본 공격은 저장한 분노를 소비하여 강해집니다.",
                R("damaged", "gain", 1, "rage", "분노", 3, turn:1),
                R("before_basic", "bonus_damage", 2, scale:"rage", cap:3), R("after_basic", "consume", 0, "rage", "분노", hit:true));

            T(p, "daniel_p", "전투 초반의 어둠에서 회피율이 증가하고, 회피 후에는 다음 기본 공격이 강화됩니다.",
                R("battle_start", "evasion_buff", 8, duration:2), R("evade", "empower_basic", 3, "recluse", "고독한 예술가", turn:1));

            T(p, "tia_p", "어느 스킬이든 물감 순환을 진행합니다. 빨강·파랑·노랑의 혼합 순서에 따라 회복, 보호, 추가 피해가 달라집니다.",
                R("before_skill", "bonus_heal", 2, need:"mix", min:1, exact:true),
                R("before_skill", "bonus_block", 3, need:"mix", min:2, exact:true),
                R("before_skill", "bonus_damage", 4, need:"mix", min:3, exact:true),
                R("after_skill", "gain", 1, "mix", "색 순환", 3, turn:2),
                R("after_skill", "consume", 0, "mix", "색 순환", need:"mix", min:3));

            T(p, "laura_p", "스킬을 사용하면 다음 기본 공격에 괴도의 추가 타격을 준비합니다.",
                R("after_skill", "empower_basic", 4, "phantom", "괴도", turn:1));

            T(p, "lenox_p", "기본 공격이나 스킬이 적중하면 태클 보호막을 얻습니다. 보호막에는 2턴의 재사용 대기시간이 있습니다.",
                R("hit", "block", 4, "tackle", "태클", cooldown:2, turn:1));

            T(p, "leon_p", "기본 공격이 물웅덩이를 만들며, 물이 있는 다음 기본 공격이 강화됩니다. 물은 턴 종료마다 줄어듭니다.",
                R("after_basic", "gain", 1, "pool", "물웅덩이", 3, hit:true, turn:2),
                R("before_basic", "bonus_damage", 2, need:"pool"), R("turn_end", "consume", 1, "pool", "물웅덩이"));

            T(p, "rozzi_p", "스킬 뒤의 기본 공격은 더블샷으로 강화됩니다. 회복을 받으면 잠시 공격력이 증가합니다.",
                R("after_skill", "empower_basic", 3, "dual", "더블샷", turn:2),
                R("heal", "damage_buff", 1, "chocolate", "초콜릿 기운", duration:1, cooldown:3, turn:1));

            T(p, "luke_p", "승리할 때마다 청소 완료를 영구적으로 쌓습니다. 이후 어느 스킬이든 청소 성장의 피해 보너스를 받습니다.",
                R("battle_win", "gain", 1, "tidy", "청소 완료", 3, persistent:true),
                R("before_skill", "bonus_damage", 1, scale:"tidy", cap:3));

            T(p, "dailin_p", "회복을 받으면 취기를 얻습니다. 어느 스킬이든 취기 중 쌍타를 준비하며, 기본 공격은 저장 취기를 조금 소비합니다.",
                R("heal", "gain", 1, "bac", "취기", 3, cooldown:1, turn:1),
                R("after_skill", "empower_basic", 2, "dialed", "쌍타", need:"bac", turn:1),
                R("before_basic", "bonus_damage", 1, scale:"bac", cap:3), R("after_basic", "consume", 1, "bac", "취기", hit:true));

            T(p, "rio_p", "카이의 관통 사격으로 모든 기본 공격에 일정한 추가 피해를 줍니다.",
                R("before_basic", "bonus_damage", 3));

            T(p, "martina_p", "기본 공격으로 인터뷰를 3회 쌓습니다. 다음 스킬이 인터뷰 표식을 소비하여 추가 사진 피해를 줍니다.",
                R("after_basic", "gain", 1, "interview", "인터뷰", 3, hit:true),
                R("before_skill", "bonus_damage", 5, need:"interview", min:3),
                R("after_skill", "consume", 0, "interview", "인터뷰", need:"interview", min:3, hit:true));

            T(p, "mai_p", "오뜨꾸뛰르의 건강한 체격이 어느 기본 공격이든 추가 피해로 이어집니다.",
                R("before_basic", "bonus_damage", 2));

            T(p, "markus_p", "약화나 취약으로 적을 제압하면 흔들림을 준비합니다. 다음 기본 공격이 흔들림을 소비하여 치명타격을 줍니다.",
                R("control", "set", 1, "rattled", "흔들림", 1, turn:1),
                R("before_basic", "bonus_damage", 5, need:"rattled"),
                R("after_basic", "consume", 0, "rattled", "흔들림", hit:true));

            T(p, "magnus_p", "기본 공격이나 스킬 적중으로 근성을 쌓습니다. 근성은 다음 턴의 방어와 최대 근성 상태의 회복을 돕습니다.",
                R("hit", "gain", 1, "tough", "근성", 4, turn:2),
                R("turn_start", "block", 1, scale:"tough", cap:4),
                R("heal", "heal", 2, need:"tough", min:4, cooldown:2, turn:1));

            T(p, "vanya_p", "스킬이 네 번 적중하면 몽환 나비가 나타나 보호막과 짧은 지속 피해를 줍니다.",
                R("skill_hit", "gain", 1, "dream", "나비의 꿈", 4, hit:true),
                R("skill_hit", "block", 4, need:"dream", min:3, hit:true, turn:1),
                R("skill_hit", "summon", 2, "butterfly", "몽환 나비", duration:2, need:"dream", min:3, hit:true, turn:1),
                R("skill_hit", "consume", 0, "dream", "나비의 꿈", need:"dream", min:3, hit:true));

            T(p, "barbara_p", "개조된 세 번째 기본 공격이 강해집니다. 그 공격은 다음 기술을 위한 코스트도 조금 회복합니다.",
                R("before_basic", "bonus_damage", 4, every:3, turn:1),
                R("after_basic", "energy", 1, every:3, hit:true, cooldown:2, turn:1));

            T(p, "bernice_p", "산탄이 기본 공격마다 추가 피해를 주고, 세 번째 사격에서는 한 발이 더 나갑니다.",
                R("before_basic", "bonus_damage", 2), R("before_basic", "bonus_damage", 3, every:3, turn:1));

            T(p, "bianca_p", "적중과 받은 피해를 혈액으로 저장합니다. 턴 종료에 혈액을 써서 회복하며 주기적으로 지혈 기본 공격을 냅니다.",
                R("hit", "gain", 1, "blood", "혈액 은행", 3, turn:1), R("damaged", "gain", 1, "blood", "혈액 은행", 3, turn:1),
                R("turn_end", "heal", 1, scale:"blood", cap:3), R("turn_end", "consume", 0, "blood", "혈액 은행"),
                R("before_basic", "bonus_damage", 3, every:3, turn:1));

            T(p, "celine_p", "스킬을 세 번 사용하면 폭발물 전문가의 다음 기본 공격이 강화됩니다.",
                R("after_skill", "empower_basic", 5, "engineer", "폭발물 전문가", every:3, turn:1));

            T(p, "sho_p", "요리사의 회복 보너스로 받은 회복이 조금 커집니다. 회복 경험을 쌓으면 이후 전투의 시작 방어가 강화됩니다.",
                R("heal", "heal", 2, "chef", "요리사의 열정", cooldown:2, turn:1),
                R("heal", "gain", 1, "feast", "든든한 체격", 3, cooldown:2, turn:1, persistent:true),
                R("battle_start", "block", 1, scale:"feast", cap:3));

            T(p, "shoichi_p", "스킬 적중으로 부당거래를 쌓습니다. 완성된 다음 기본 공격은 추가 피해와 단검을 만들고, 다음 스킬이 단검을 회수합니다.",
                R("after_skill", "gain", 1, "deal", "부당거래", 3, hit:true),
                R("before_basic", "bonus_damage", 4, need:"deal", min:3),
                R("after_basic", "gain", 1, "dagger", "떨어진 단검", 2, need:"deal", min:3, hit:true),
                R("after_basic", "consume", 0, "deal", "부당거래", need:"deal", min:3, hit:true),
                R("before_skill", "bonus_damage", 3, need:"dagger"), R("after_skill", "consume", 1, "dagger", "떨어진 단검", hit:true));

            T(p, "sissela_p", "체력이 절반 아래일 때 고통이 회복과 스킬 피해를 높입니다. 방어를 얻으면 윌슨의 귀환 기본 공격을 준비합니다.",
                R("turn_start", "heal", 3, hp:50), R("before_skill", "bonus_damage", 2, hp:50),
                R("block", "empower_basic", 3, "wilson", "윌슨 귀환", cooldown:2, turn:1));

            T(p, "silvia_p", "전투 구역을 새로 경험할 때 탐방을 쌓습니다. 충분히 탐방하면 모든 스킬의 피해와 전투 시작 행동력이 늘어납니다.",
                R("battle_start", "gain", 1, "visited", "지역 탐방", 4, persistent:true),
                R("battle_start", "energy", 1, need:"visited", min:3), R("before_skill", "bonus_damage", 1, need:"visited", min:3));

            T(p, "adela_p", "중앙 장악의 고정 공격 속도 이점이 모든 스킬의 소폭 피해 증가로 바뀝니다.",
                R("before_skill", "bonus_damage", 2));

            T(p, "adriana_p", "어느 스킬이든 적중하면 적을 태우고 방어를 무너뜨립니다. 같은 턴에 화재를 계속 중첩하지 않습니다.",
                R("after_skill", "burn", 2, "pyromania", "활활", duration:2, hit:true, cooldown:2, turn:1),
                R("after_skill", "vulnerable", 1, hit:true, cooldown:2, turn:1));

            T(p, "adina_p", "어느 스킬이든 별자리 순환을 읽습니다. 태양은 피해, 달은 방어, 별은 회복을 주며 한 순환을 마치면 회피율이 증가합니다.",
                R("before_skill", "bonus_damage", 2, need:"star", min:0, exact:true),
                R("before_skill", "bonus_block", 3, need:"star", min:1, exact:true),
                R("before_skill", "bonus_heal", 2, need:"star", min:2, exact:true),
                R("after_skill", "gain", 1, "star", "별읽기", 3),
                R("after_skill", "evasion_buff", 6, duration:2, need:"star", min:3, turn:1),
                R("after_skill", "consume", 0, "star", "별읽기", need:"star", min:3));

            T(p, "isaac_p", "모든 기본 공격이 착취 피해를 더합니다. 세 번째 기본 공격은 추가 피해를 주고 체력을 회복합니다.",
                R("before_basic", "bonus_damage", 2), R("before_basic", "bonus_damage", 4, every:3, turn:1),
                R("after_basic", "heal", 4, every:3, hit:true, turn:1));

            T(p, "alex_p", "잠입으로 전투 초반의 회피율이 증가합니다. 적의 공격을 회피하면 다음 기습 기본 공격을 준비합니다. 장착한 무기마다 다른 무기군의 D 스킬 카드 1장을 추가로 받습니다.",
                R("battle_start", "evasion_buff", 8, duration:2), R("evade", "empower_basic", 4, "infiltrate", "잠입", turn:1));

            T(p, "jan_p", "기본 공격이나 스킬 적중으로 열혈을 쌓습니다. 다음 스킬은 완성된 열혈을 써서 강해지고 코스트를 조금 회복합니다.",
                R("hit", "gain", 1, "unyielding", "열혈", 3, turn:2),
                R("before_skill", "bonus_damage", 4, need:"unyielding", min:3),
                R("before_skill", "energy", 1, need:"unyielding", min:3, cooldown:2, turn:1),
                R("after_skill", "consume", 0, "unyielding", "열혈", need:"unyielding", min:3, hit:true));

            T(p, "estelle_p", "사명감으로 두 턴마다 체력을 조금 회복합니다.", R("turn_start", "heal", 3, every:2));

            T(p, "aiden_p", "스킬 적중으로 암페어를 얻습니다. 충분히 모은 다음 기본 공격은 과전하 타격이며, 사용 후 기동력이 증가합니다.",
                R("after_skill", "gain", 1, "ampere", "암페어", 3, hit:true, turn:2),
                R("before_basic", "bonus_damage", 5, need:"ampere", min:3),
                R("before_basic", "set", 1, "overcharged", "과전하 완료", 1, need:"ampere", min:3),
                R("after_basic", "consume", 0, "ampere", "암페어", need:"ampere", min:3, hit:true),
                R("after_basic", "evasion_buff", 5, duration:1, need:"overcharged", hit:true, turn:1),
                R("after_basic", "consume", 0, "overcharged", "과전하 완료", hit:true));

            T(p, "echion_p", "카드모스의 부름이 전투 승리마다 무기를 진화시킵니다. 어느 기본 공격이든 진화한 무기의 보너스를 받습니다.",
                R("battle_win", "gain", 1, "evolution", "무기 진화", 3, persistent:true),
                R("battle_start", "empower_basic", 2, "cadmus", "카드모스", turn:1),
                R("before_basic", "bonus_damage", 1, scale:"evolution", cap:3));

            T(p, "elena_p", "공격과 스킬이 냉기를 쌓아 적을 얼립니다. 다음 공격이나 스킬이 빙결을 깨고 추가 피해를 줍니다.",
                R("hit", "gain", 1, "chill", "냉기", 3, turn:2),
                R("hit", "set", 1, "frozen", "빙결", 1, need:"chill", min:2, cooldown:1, turn:1),
                R("hit", "weak", 1, need:"chill", min:2, cooldown:1, turn:1),
                R("hit", "consume", 0, "chill", "냉기", need:"chill", min:2),
                R("before_basic", "bonus_damage", 4, need:"frozen"), R("before_skill", "bonus_damage", 4, need:"frozen"),
                R("hit", "consume", 0, "frozen", "빙결", need:"frozen"));

            T(p, "johann_p", "빛의 가호로 보호막이나 회복을 받으면 약화와 취약을 정화합니다. 정화는 각각 2턴마다 한 번 가능합니다.",
                R("block", "cleanse", 1, cooldown:2, turn:1), R("heal", "cleanse", 1, cooldown:2, turn:1));

            T(p, "william_p", "기본 공격은 바닥 공을 남깁니다. 어느 스킬이든 그 공을 회수하여 다음 기본 공격을 강화할 수 있습니다.",
                R("after_basic", "gain", 1, "ball", "바닥 공", 3, hit:true),
                R("after_skill", "empower_basic", 4, "catch", "캐치볼", need:"ball", turn:1),
                R("after_skill", "consume", 1, "ball", "바닥 공", need:"ball", turn:1));

            T(p, "irem_p", "새 전투에서는 고양이의 관찰력으로 회피율이 증가합니다. 오래 머무르면 보호와 다음 기본 공격이 강화됩니다.",
                R("battle_start", "evasion_buff", 6, duration:2),
                R("turn_start", "block", 3, every:3), R("turn_start", "empower_basic", 3, "catitude", "고양이의 습성", every:3));

            T(p, "eva_p", "스킬을 맞히거나 기본 공격을 받으면 VF 구슬이 발사됩니다. 구슬은 짧은 지연 피해를 주며 재사용 대기시간이 있습니다.",
                R("after_skill", "delayed_damage", 3, "bead", "VF 구슬", delay:1, hit:true, cooldown:2, turn:1),
                R("incoming_hit", "delayed_damage", 3, "bead", "VF 구슬", delay:1, cooldown:2, turn:1, category:"basic"));

            T(p, "ian_p", "체력이 낮으면 빙의가 회복을 돕습니다. 빙의 중에는 어느 스킬이든 추가 피해와 소량 회복을 얻습니다.",
                R("turn_start", "heal", 3, hp:40), R("before_skill", "bonus_damage", 2, hp:40),
                R("after_skill", "heal", 3, hp:40, hit:true, cooldown:1, turn:1));

            T(p, "eleven_p", "스킬 적중이 미니 버거를 남깁니다. 기본 공격이나 회피로 버거를 하나 주워 회복합니다.",
                R("after_skill", "gain", 1, "slider", "미니 버거", 3, hit:true, turn:2),
                R("after_basic", "heal", 2, need:"slider", hit:true, turn:1), R("after_basic", "consume", 1, "slider", "미니 버거", need:"slider", hit:true, turn:1),
                R("evade", "heal", 3, need:"slider", turn:1), R("evade", "consume", 1, "slider", "미니 버거", need:"slider", turn:1));

            T(p, "zahir_p", "스킬이 적중하면 사신의 눈으로 적을 드러냅니다. 다음 스킬은 추가 피해를 얻고 주기적으로 회피율도 증가합니다.",
                R("after_skill", "set", 1, "eye", "사신의 눈", 1, hit:true),
                R("before_skill", "bonus_damage", 3, need:"eye", turn:1),
                R("after_skill", "evasion_buff", 6, duration:1, need:"eye", hit:true, cooldown:2, turn:1));

            T(p, "jenny_p", "기본 공격으로 리허설을 진행해 주기적으로 코스트를 회복합니다. 전투당 한 번 죽은 척하여 치명상을 피하고 회복합니다.",
                R("after_basic", "energy", 1, every:3, hit:true, cooldown:2, turn:1),
                R("battle_start", "revive", 18, "play_dead", "죽음의 연기", battle:1, persistent:true));

            T(p, "camilo_p", "기본 공격과 스킬을 번갈아 사용하면 올레 보호막을 얻습니다. 완성된 교대 기본 공격은 코스트도 조금 회복합니다.",
                R("after_basic", "set", 1, "basic_step", "기본 스텝", 1, hit:true),
                R("after_skill", "block", 4, need:"basic_step", turn:1),
                R("after_skill", "consume", 0, "basic_step", "기본 스텝"),
                R("after_skill", "set", 1, "skill_step", "스킬 스텝", 1),
                R("after_basic", "block", 3, need:"skill_step", hit:true, turn:1),
                R("after_basic", "energy", 1, need:"skill_step", hit:true, cooldown:2, turn:1),
                R("after_basic", "consume", 0, "skill_step", "스킬 스텝", hit:true));

            T(p, "karla_p", "매 턴 장전을 모읍니다. 기본 공격을 기다릴수록 그 공격이 강해지며 발사하면 장전을 모두 소비합니다.",
                R("turn_start", "gain", 1, "readiness", "작살 장전", 3),
                R("before_basic", "bonus_damage", 2, scale:"readiness", cap:3),
                R("after_basic", "consume", 0, "readiness", "작살 장전", hit:true));

            T(p, "cathy_p", "스킬 적중은 상처 출혈을 남깁니다. 세 번째 적중은 중상 출혈, 회복 감소와 보호막을 만듭니다.",
                R("skill_hit", "gain", 1, "wound_stacks", "상처 횟수", 3, hit:true),
                R("skill_hit", "bleed", 2, "wounded", "상처", duration:2, hit:true, turn:1),
                R("skill_hit", "bleed", 3, "severe", "중상", duration:3, need:"wound_stacks", min:2, hit:true, turn:1),
                R("skill_hit", "heal_reduction", 10, duration:2, need:"wound_stacks", min:2, hit:true, turn:1),
                R("skill_hit", "block", 4, need:"wound_stacks", min:2, hit:true, turn:1),
                R("skill_hit", "consume", 0, "wound_stacks", "상처 횟수", need:"wound_stacks", min:2, hit:true));

            T(p, "chloe_p", "니나가 전투 시작부터 짧게 지원 공격을 합니다. 잠시 물러난 뒤 몇 턴마다 다시 전개됩니다.",
                R("battle_start", "summon", 2, "nina", "니나", duration:3),
                R("turn_start", "summon", 2, "nina", "니나", duration:2, every:3, battle:3));

            T(p, "chiara_p", "어느 스킬이든 적중하면 낙인을 쌓고 회복을 줄입니다. 최대 낙인에서는 접근 기동력도 증가합니다.",
                R("after_skill", "gain", 1, "stigma", "낙인", 4, hit:true, turn:1),
                R("after_skill", "heal_reduction", 10, duration:2, hit:true, turn:1),
                R("after_skill", "evasion_buff", 6, duration:1, need:"stigma", min:4, hit:true, turn:1));

            T(p, "tazia_p", "주기적인 기본 공격이 유리 파편을 남깁니다. 스킬로 파편을 모아 유리 검을 만들면 다음 스킬이 강화됩니다.",
                R("before_basic", "bonus_damage", 3, every:3, turn:1),
                R("after_basic", "gain", 1, "shard", "유리 파편", 4, every:3, hit:true),
                R("after_skill", "gain", 1, "collection", "파편 수집", 4, need:"shard", turn:1),
                R("after_skill", "consume", 1, "shard", "유리 파편", need:"shard", turn:1),
                R("before_skill", "bonus_damage", 5, need:"collection", min:4),
                R("after_skill", "consume", 0, "collection", "파편 수집", need:"collection", min:4, hit:true));

            T(p, "theodore_p", "전투 진입 시 은폐 회피와 보호막을 얻습니다. 주기적인 기본 공격 적중은 다음 기술을 위한 코스트를 회복합니다.",
                R("battle_start", "evasion_buff", 6, duration:2), R("battle_start", "block", 4),
                R("after_basic", "energy", 1, every:3, hit:true, cooldown:2, turn:1));

            T(p, "felix_p", "스킬은 다음 기본 공격을 강화하고, 그 기본 공격은 연계 창술을 쌓습니다. 다음 스킬은 창술을 써서 보호와 행동력을 얻습니다.",
                R("after_skill", "empower_basic", 3, "double", "연계 평타", turn:1),
                R("after_basic", "gain", 1, "weaving", "연계 창술", 3, hit:true),
                R("before_skill", "bonus_block", 1, scale:"weaving", cap:3),
                R("before_skill", "energy", 1, need:"weaving", min:3, cooldown:2, turn:1),
                R("after_skill", "consume", 0, "weaving", "연계 창술"));

            T(p, "priya_p", "스킬을 두 번 사용하면 꽃이 자랍니다. 다음 스킬은 준비된 꽃을 피워 소량의 회복과 보호를 줍니다.",
                R("after_skill", "gain", 1, "flower", "사라스바티 꽃", 3, every:2, turn:1),
                R("before_skill", "bonus_heal", 2, need:"flower", turn:1),
                R("before_skill", "bonus_block", 3, need:"flower", turn:1),
                R("after_skill", "consume", 1, "flower", "사라스바티 꽃", need:"flower", turn:1));

            T(p, "fiora_p", "스킬이 뚜셰를 쌓습니다. 최대 뚜셰에서 다음 스킬은 추가 피해와 회복을 주고 다음 행동의 코스트를 조금 돌려줍니다.",
                R("after_skill", "gain", 1, "touche", "뚜셰", 3, hit:true),
                R("before_skill", "bonus_damage", 4, need:"touche", min:3),
                R("after_skill", "heal", 3, need:"touche", min:3, hit:true, turn:1),
                R("after_skill", "energy", 1, need:"touche", min:3, hit:true, cooldown:2, turn:1),
                R("after_skill", "consume", 0, "touche", "뚜셰", need:"touche", min:3, hit:true));

            T(p, "piolo_p", "전투를 준비하면서 단련을 쌓습니다. 어느 궁극기든 저장한 단련을 하나 써서 다음 기본 공격을 강화합니다.",
                R("battle_start", "gain", 1, "trained", "단련", 3, persistent:true),
                R("after_ultimate", "empower_basic", 5, "punisher", "단련된 응징자", need:"trained", turn:1),
                R("after_ultimate", "consume", 1, "trained", "단련", need:"trained", turn:1, persistent:true));

            T(p, "hart_p", "모든 기본 공격이 Feedback의 추가 음파 피해를 줍니다.", R("before_basic", "bonus_damage", 2));

            T(p, "haze_p", "어느 스킬을 사용해도 무기 케이스에서 새 총을 꺼내 다음 기본 공격에 추가 피해를 준비합니다.",
                R("after_skill", "empower_basic", 4, "merchant", "웨폰 케이스", turn:1));
        }

        static void T(Dictionary<string, TraitMechanicProfile> profiles, string id, string summary, params TraitRule[] rules)
        {
            profiles[id] = new TraitMechanicProfile { summary=summary, rules=rules, replaceLegacy=true };
        }

        static TraitRule R(string trigger, string op, int amount, string key=null, string label=null, int cap=3,
            int duration=2, int delay=1, int every=1, int cooldown=0, int battle=0, int turn=0, int hp=0,
            string scale=null, string need=null, int min=1, bool exact=false, bool hit=false,
            bool persistent=false, string category=null)
        {
            return new TraitRule { trigger=trigger, op=op, amount=amount, key=key, label=label, cap=cap,
                duration=duration, delay=delay, every=every, cooldown=cooldown, maxPerBattle=battle,
                maxPerTurn=turn, hpBelowPercent=hp, scaleKey=scale, conditionKey=need,
                conditionAmount=min, conditionExact=exact, onHit=hit, persistent=persistent, conditionCategory=category };
        }
    }
}
