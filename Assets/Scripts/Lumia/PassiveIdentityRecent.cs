using System.Collections.Generic;

namespace Lumia
{
    /// <summary>Source-based global traits for the 26 recent subjects; no card-owner restrictions.</summary>
    public static class TraitIdentityRecent
    {
        public static void Populate(Dictionary<string, TraitMechanicProfile> p)
        {
            // Basic attacks paint blue, skills paint red. Changing color creates the proc.
            p["debi_marlene_p"] = P("기본 공격의 파란 잉크와 기술의 빨간 잉크를 교차시키면 추가 피해와 회피를 얻습니다.",
                R("before_basic","bonus_damage",n:4,need:"ink",at:2,eq:true,turn:1),
                R("before_skill","bonus_damage",n:4,need:"ink",at:1,eq:true,turn:1),
                R("after_basic","evasion_buff","cross_color","블루&레드 가속",5,time:2,need:"ink",at:2,eq:true,hit:true,cd:1),
                R("after_skill","evasion_buff","cross_color","블루&레드 가속",5,time:2,need:"ink",at:1,eq:true,hit:true,cd:1),
                R("after_basic","set","ink","블루&레드 잉크",1,cap:2,hit:true),
                R("after_skill","set","ink","블루&레드 잉크",2,cap:2,hit:true));

            p["arda_p"] = P("어느 실험체의 기술이든 세 번 적중시키면 모은 고대의 정수로 체력을 회복합니다.",
                R("after_skill","gain","antiquity","고대의 정수",1,cap:3,hit:true),
                R("after_skill","heal",n:4,need:"antiquity",at:2,hit:true,turn:1),
                R("after_skill","consume","antiquity",need:"antiquity",at:2,hit:true));

            p["abigail_p"] = P("기술을 적중시키면 다음 기본 공격이 강화되며, 그 공격은 적의 방어를 약화시킵니다.",
                R("after_skill","empower_basic","tearing","티어링 블레이드",4,hit:true,turn:1),
                R("after_skill","set","blade_ready","티어링 블레이드 준비",1,cap:1,hit:true),
                R("after_basic","vulnerable",n:1,need:"blade_ready",hit:true,cd:2),
                R("after_basic","consume","blade_ready",n:1,hit:true));

            // A barrier recharges after two owner turns without losing HP; control heals separately.
            p["alonso_p"] = P("체력 피해를 받지 않고 두 턴을 보내면 해로운 효과를 정화합니다. 적에게 제어 효과를 적용하면 체력을 회복합니다.",
                R("battle_start","cleanse"),
                R("turn_end","gain","barrier_charge","플라즈마 베리어 충전",1,cap:2),
                R("damaged","set","barrier_charge","플라즈마 베리어 충전",0,cap:2),
                R("turn_start","cleanse",need:"barrier_charge",at:2),
                R("turn_start","block",n:3,need:"barrier_charge",at:2),
                R("turn_start","consume","barrier_charge",need:"barrier_charge",at:2),
                R("control","heal",n:4,hit:true,turn:1));

            p["leni_p"] = P("회복이나 방어로 곰돌이를 준비합니다. 곰돌이가 있는 동안 다음 적중 기본 공격이 강화되고 사용 코스트를 회복합니다.",
                R("heal","set","teddy","곰돌이",1,cap:1,turn:1),
                R("block","set","teddy","곰돌이",1,cap:1,turn:1),
                R("before_basic","bonus_damage",n:4,need:"teddy",turn:1),
                R("after_basic","energy",n:1,need:"teddy",hit:true,cd:2),
                R("after_basic","consume","teddy",n:1,hit:true));

            p["tsubame_p"] = P("기본 공격이 생사 각인을 쌓습니다. 네 번째 적중 기본 공격은 각인을 터뜨려 추가 피해를 줍니다.",
                R("before_basic","bonus_damage",n:5,need:"seal",at:3),
                R("after_basic","gain","seal","생사 각인",1,cap:4,hit:true),
                R("after_basic","consume","seal",need:"seal",at:3,hit:true));

            p["kenneth_p"] = P("적중 공격으로 분노를 쌓습니다. 네 번째 적중 공격에는 불타는 도끼의 추가 피해와 회복이 붙습니다.",
                R("before_card","bonus_damage",n:3,need:"rage",at:3),
                R("hit","gain","rage","억압된 분노",1,cap:4,hit:true),
                R("hit","heal",n:3,need:"rage",at:3,hit:true,turn:1),
                R("hit","consume","rage",need:"rage",at:3,hit:true));

            p["katja_p"] = P("어느 실험체의 기술이든 적중시키면 다음 기본 공격이 강화됩니다. 잿빛 사신의 크레딧 보너스는 키오스크 할인으로 적용합니다.",
                R("after_skill","empower_basic","reaper","잿빛 사신",3,hit:true,turn:1),
                R("always","shop_discount",n:10));

            p["charlotte_p"] = P("체력 피해를 받을 때마다 고결한 마음을 쌓습니다. 다음 회복은 마음을 소비하여 보호막을 주며, 세 중첩이면 추가로 회복합니다.",
                R("damaged","gain","heart","고결한 마음",1,cap:3),
                R("heal","block",n:1,scale:"heart",cap:3,need:"heart",turn:1),
                R("heal","heal",n:3,need:"heart",at:3,turn:1),
                R("heal","consume","heart",need:"heart"));

            p["darko_p"] = P("기본 공격이 적중하면 적의 방어를 훔쳐 취약을 부여하고 자신의 방어도를 얻습니다. 이 효과는 두 턴마다 발동합니다.",
                R("after_basic","vulnerable",n:1,hit:true,cd:2),
                R("after_basic","block",n:3,hit:true,cd:2));

            p["lenore_p"] = P("세 번째 기술은 트릴로 강화되고 코스트를 회복합니다. 트릴이 모은 비명은 다음 R 카드의 피해를 강화합니다.",
                R("before_skill","bonus_damage",n:3,every:3),
                R("after_skill","energy",n:1,every:3,turn:1),
                R("after_skill","gain","scream","비명과 아첼레란도",1,cap:3,every:3,hit:true),
                R("before_ultimate","bonus_damage",n:1,scale:"scream",cap:3,need:"scream"),
                R("after_ultimate","consume","scream",hit:true));

            p["garnet_p"] = P("적이 기본 공격 카드를 사용할 때 첫 적중 피해를 2 줄입니다.",
                R("before_incoming","reduce_damage",n:2,category:"basic"));

            p["yumin_p"] = P("턴 시작에 바람 지대를 준비하여 잠시 회피율을 얻습니다. 그 턴의 첫 기술은 바람을 소비하여 방어도를 줍니다.",
                R("turn_start","set","wind_zone","바람 지대",1,cap:1),
                R("turn_start","evasion_buff","tao_speed","바람 지대 가속",5,time:1),
                R("after_skill","block",n:4,need:"wind_zone"),
                R("after_skill","consume","wind_zone",n:1));

            p["hisui_p"] = P("기술을 사용할 때마다 검의 기억을 쌓습니다. 기본 공격은 기억을 소비하여 피해와 회피율을 늘리고, 기억이 두 개 이상이면 코스트를 회복합니다.",
                R("after_skill","gain","memory","검의 기억",1,cap:3),
                R("before_basic","bonus_damage",n:1,scale:"memory",cap:3,need:"memory"),
                R("after_basic","evasion_buff","memory_speed","검의 기억 가속",5,time:2,need:"memory",hit:true),
                R("after_basic","energy",n:1,need:"memory",at:2,hit:true,cd:2),
                R("after_basic","consume","memory",hit:true));

            // First skill marks, second skill consumes the mark and returns Astra; charged basics release it.
            p["justyna_p"] = P("아스트라 에너지가 턴마다 충전됩니다. 기술은 정의의 표적을 붙이거나 소비하여 추가 피해와 에너지를 얻습니다. 에너지 세 개를 모으면 기본 공격으로 코스트를 회복합니다.",
                R("turn_start","gain","astra","아스트라 에너지",1,cap:3),
                R("before_skill","bonus_damage",n:3,need:"justice"),
                R("after_skill","set","justice","정의의 표적",1,cap:1,need:"justice",at:0,eq:true,hit:true),
                R("after_skill","gain","astra","아스트라 에너지",1,cap:3,need:"justice",hit:true),
                R("after_skill","consume","justice",n:1,need:"justice",hit:true),
                R("after_basic","energy",n:1,need:"astra",at:3,hit:true,cd:2),
                R("after_basic","consume","astra",need:"astra",at:3,hit:true));

            p["istvan_p"] = P("기본 공격과 기술을 합쳐 세 번 사용하면 변수가 준비됩니다. 다음 기술이 변수를 소비하여 추가 피해와 회피율을 얻습니다.",
                R("after_basic","gain","variable","변수",1,cap:3),
                R("after_skill","gain","variable","변수",1,cap:3),
                R("before_skill","bonus_damage",n:4,need:"variable",at:3),
                R("after_skill","evasion_buff","calculation_speed","변수 가속",5,time:2,need:"variable",at:3),
                R("after_skill","consume","variable",need:"variable",at:3));

            p["xuelin_p"] = P("적중 공격으로 검기를 세 개 모으면 다음 기본 공격이 강화됩니다. 강화 공격은 체력을 회복하고 사용 코스트를 돌려줍니다.",
                R("hit","gain","lingering","검기",1,cap:3,hit:true),
                R("before_basic","bonus_damage",n:4,need:"lingering",at:3),
                R("after_basic","heal",n:3,need:"lingering",at:3,hit:true),
                R("after_basic","energy",n:1,need:"lingering",at:3,hit:true,cd:2),
                R("after_basic","consume","lingering",need:"lingering",at:3,hit:true));

            p["henry_p"] = P("실제로 준 피해의 25%를 시간 균열에 저장합니다. 12가 모인 뒤 다음 적중 공격은 저장값을 소비하여 두 턴 뒤 폭발하는 타이머를 붙입니다.",
                R("hit","damage_resource","fracture","시간 균열 피해",25,cap:12,hit:true),
                R("hit","delayed_damage","fracture_timer","시간 균열 타이머",4,delay:2,need:"fracture",at:12,hit:true,cd:2,turn:1),
                R("hit","consume","fracture",need:"fracture",at:12,hit:true,cd:2,turn:1));

            p["blair_p"] = P("기술 뒤의 기본 공격은 강화되고 적중 시 회복합니다. R 카드는 무기 형태를 전환하여 다음 기술을 강화합니다.",
                R("after_skill","set","basic_ready","블레이드 시프트 기본 공격",1,cap:1),
                R("before_basic","bonus_damage",n:3,need:"basic_ready"),
                R("after_basic","heal",n:3,need:"basic_ready",hit:true,cd:1),
                R("after_basic","consume","basic_ready",n:1,hit:true),
                R("after_ultimate","set","form","무기 형태",1,cap:1,need:"form",at:0,eq:true),
                R("after_ultimate","set","form","무기 형태",0,cap:1,need:"form",at:1,eq:true),
                R("after_ultimate","set","shifting","시프팅",1,cap:1),
                R("before_skill","bonus_damage",n:3,need:"shifting"),
                R("after_skill","heal",n:3,need:"shifting",hit:true,cd:1),
                R("after_skill","consume","shifting",n:1,hit:true));

            p["mirka_p"] = P("적에게 제어 효과를 세 번 적용하면 임펄스 게이지가 찹니다. 다음 적중 기술이 게이지를 소비하여 추가 피해를 주며, 제어 세 번마다 코스트도 회복합니다.",
                R("control","gain","impulse","임펄스 게이지",1,cap:3,hit:true),
                R("control","energy",n:1,every:3,hit:true,cd:2),
                R("before_skill","bonus_damage",n:5,need:"impulse",at:3),
                R("after_skill","consume","impulse",need:"impulse",at:3,hit:true));

            p["fenrir_p"] = P("전투마다 한 번 치명상을 버티고 체력을 6 회복합니다. 어느 실험체의 R 카드든 적중시키면 잠시 VF 에너지를 흡수하여 지속 피해와 회복을 얻습니다.",
                R("battle_start","revive","last_ditch","라스트 디치",6,time:2,battle:1,persistent:true),
                R("after_ultimate","burn","vf_absorption","VF 흡수 피해",2,time:2,hit:true,cd:2),
                R("after_ultimate","hot","vf_recovery","VF 흡수 회복",3,time:2,hit:true,cd:2));

            p["coraline_p"] = P("기술 적중이 거울 조각 표식을 남깁니다. 표식이 있는 적에게 다음 공격이 적중하면 표식을 소비하여 추가 피해를 줍니다.",
                R("before_card","bonus_damage",n:3,need:"shard",turn:1),
                R("hit","consume","shard",n:1,hit:true),
                R("skill_hit","set","shard","거울 조각 표식",1,cap:1,hit:true));

            p["bihyung_p"] = P("적중 공격 세 번으로 신력을 채우면 도깨비불이 두 턴 동안 적을 공격하고 자신의 회피율을 높입니다.",
                R("hit","gain","divine","신력",1,cap:3,hit:true),
                R("hit","summon","dokkaebi","도깨비불",2,time:2,need:"divine",at:2,hit:true,cd:1),
                R("hit","evasion_buff","dokkaebi_resist","도깨비불 저항",5,time:2,need:"divine",at:2,hit:true,cd:1),
                R("hit","consume","divine",need:"divine",at:2,hit:true));

            // The original six rounds become three bounded ammunition stages, with no card-use lockout.
            p["craver_p"] = P("세 발 탄창을 기본 공격과 적중 기술이 함께 사용합니다. 마지막 탄환의 기본 공격은 강화되고, 마지막 탄환을 사용하면 재장전하며 코스트를 회복합니다.",
                R("battle_start","set","magazine","데스페라도 탄창",3,cap:3),
                R("before_basic","bonus_damage",n:2),
                R("before_basic","bonus_damage",n:3,need:"magazine",at:1,eq:true),
                R("after_basic","consume","magazine",n:1,hit:true),
                R("after_skill","consume","magazine",n:1,hit:true),
                R("after_basic","set","magazine","데스페라도 탄창",3,cap:3,need:"magazine",at:1,eq:true,hit:true),
                R("after_skill","set","magazine","데스페라도 탄창",3,cap:3,need:"magazine",at:1,eq:true,hit:true),
                R("after_basic","energy",n:1,need:"magazine",at:1,eq:true,hit:true,cd:2),
                R("after_skill","energy",n:1,need:"magazine",at:1,eq:true,hit:true,cd:2));

            p["lucia_p"] = P("기술 적중이 수정을 남깁니다. 다음 적중 기본 공격은 수정을 깨뜨려 추가 피해를 주고 잠시 회피율을 높입니다.",
                R("after_skill","set","crystal","수정",1,cap:1,hit:true),
                R("before_basic","bonus_damage",n:5,need:"crystal"),
                R("after_basic","evasion_buff","lady_speed","숙녀의 가속",8,time:2,need:"crystal",hit:true),
                R("after_basic","consume","crystal",n:1,hit:true));

            p["ceres_p"] = P("기본 공격이 적중하면 흑요석 조각을 남겨 두 턴 동안 지속 피해를 줍니다. 같은 조각의 지속 시간은 갱신됩니다.",
                R("after_basic","burn","obsidian","흑요석 조각",2,time:2,hit:true,cd:1,turn:1));
        }

        private static TraitMechanicProfile P(string summary, params TraitRule[] rules)
        {
            return new TraitMechanicProfile { summary=summary, replaceLegacy=true, rules=rules };
        }
        private static TraitRule R(string trigger, string op, string key=null, string label=null, int n=0,
            int cap=3, int time=2, int delay=1, string scale=null, string need=null, int at=1, bool eq=false,
            bool hit=false, int every=1, int cd=0, int turn=0, int battle=0, string category=null, bool persistent=false)
        {
            return new TraitRule { trigger=trigger, op=op, key=key, label=label, amount=n, cap=cap, duration=time,
                delay=delay, scaleKey=scale, conditionKey=need, conditionAmount=at, conditionExact=eq, onHit=hit,
                every=every, cooldown=cd, maxPerTurn=turn, maxPerBattle=battle, conditionCategory=category, persistent=persistent };
        }
    }
}
