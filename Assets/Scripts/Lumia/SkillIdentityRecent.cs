using System.Collections.Generic;

namespace Lumia
{
    /// <summary>26 recent subjects: bounded, source-specific mechanics applied after baseline scaling.</summary>
    public static class SkillIdentityRecent
    {
        public static void Populate(Dictionary<string, SkillMechanicProfile> p)
        {
            // Debi / Marlene: stance 0=blue, 1=red; ink 1=blue, 2=red.
            p["debi_marlene_q"] = M(d:8, rules:TwinInk(8));
            p["debi_marlene_w"] = M(d:3, hits:2, block:8, rules:TwinInk(6));
            p["debi_marlene_e"] = M(d:7, evade:20, time:2, rules:new[]{
                R("bonus_damage",n:7,need:"stance",at:0,eq:true,other:"ink",oat:1,oeq:true), R("bonus_damage",n:7,need:"stance",at:1,eq:true,other:"ink",oat:2,oeq:true),
                R("set","ink","블루&레드 잉크",2,cap:2,need:"stance",at:0,eq:true,hit:true), R("set","ink","블루&레드 잉크",1,cap:2,need:"stance",at:1,eq:true,hit:true),
                R("set","stance","교대 색상",1,cap:1,need:"stance",at:0,eq:true), R("set","stance","교대 색상",0,cap:1,need:"stance",at:1,eq:true)});
            p["debi_marlene_r"] = M(d:5,hits:4,weak:1,rules:new[]{R("bonus_damage",n:8,need:"ink"),R("consume","ink",hit:true),R("discount",n:3,target:"debi_marlene_e",hit:true)});

            p["arda_q"] = M(d:8,rules:Relic(R("bonus_damage",n:10,need:"awakened")));
            p["arda_w"] = M(d:3,weak:1,rules:Relic(R("delayed_damage","cube","바빌론의 입방체",8,delay:1,hit:true),R("delayed_damage","awakened_cube","깨어난 입방체",10,delay:1,need:"awakened",hit:true)));
            p["arda_e"] = M(d:10,weak:1,rules:Relic(R("bonus_damage",n:10,need:"awakened")));
            p["arda_r"] = M(draw:1,rules:new[]{R("set","awakened","잠들어있는 힘",1,cap:1)});

            p["abigail_q"] = M(d:5,hits:2,evade:10,rules:new[]{R("empower_basic","tearing","티어링 블레이드",4,hit:true)});
            p["abigail_w"] = M(d:8,block:8,rules:new[]{R("set","coordinates","좌표",1,cap:1,hit:true),R("empower_basic","tearing","티어링 블레이드",4,hit:true)});
            p["abigail_e"] = M(d:8,evade:20,rules:new[]{R("bonus_damage",n:8,need:"coordinates"),R("discount",n:3,target:"abigail_e",need:"coordinates",hit:true),R("consume","coordinates",n:1,hit:true),R("empower_basic","tearing","티어링 블레이드",4,hit:true)});
            p["abigail_r"] = M(evade:35,rules:new[]{R("delayed_damage","dimension","디멘션 스트라이크",24,delay:1),R("set","coordinates","좌표",1,cap:1)});

            p["alonso_q"] = M(d:5,weak:1,rules:new[]{R("empower_basic","connection","자기 연결",8,hit:true)});
            p["alonso_w"] = M(block:12,rules:new[]{R("counter","magnetic_shield","바운싱 실드",4,time:2),R("delayed_damage","shield_release","자기장 방출",7,delay:2)});
            p["alonso_e"] = M(d:5,weak:1,rules:new[]{R("delayed_damage","attraction","어트랙션 슬램",10,delay:1,hit:true),R("bonus_heal",n:4)});
            p["alonso_r"] = M(d:5,block:6,weak:1,rules:new[]{R("summon","magnetic_field","게더링 필드",4,time:3,hit:true),R("hot","metal_fragments","금속 파편 흡수",3,time:3),R("delayed_damage","field_release","게더링 필드 방출",10,delay:3)});

            p["leni_q"] = M(d:7,heal:4,rules:new[]{R("empower_basic","teddy","곰돌이 지원",5,time:2),R("discount",n:1,target:"leni_e",hit:true)});
            p["leni_w"] = M(d:9,weak:1,evade:20,rules:new[]{R("empower_basic","teddy","곰돌이 지원",4,time:2)});
            p["leni_e"] = M(d:5,block:10,weak:1,rules:new[]{R("empower_basic","teddy","곰돌이 지원",5,time:2)});
            p["leni_r"] = M(d:8,evade:20,weak:1,rules:new[]{R("delayed_damage","spring","스프링 벽 충돌",12,delay:1,hit:true)});

            p["tsubame_q"] = M(d:3,hits:3,weak:1,rules:new[]{R("gain","seal","생사 각인",1,cap:4,need:"seal",at:0,eq:true,hit:true),R("gain","seal","생사 각인",2,cap:4,need:"seal",hit:true)});
            p["tsubame_w"] = M(evade:25,time:2,rules:new[]{R("gain","seal","생사 각인",1,cap:4),R("empower_basic","whirlwind","제비걸음 연속 공격",5,time:2)});
            p["tsubame_e"] = M(block:6,evade:30,time:2,rules:new[]{R("bonus_block",n:8,need:"wooden_log"),R("consume","wooden_log",n:1),R("set","wooden_log","바꿔치기 통나무",1,cap:1,need:"wooden_log",at:0,eq:true)});
            p["tsubame_r"] = M(d:14,rules:new[]{R("bonus_damage",n:5,scale:"seal",cap:4,need:"seal",at:4),R("discount",n:3,target:"tsubame_w",need:"seal",at:4,hit:true),R("consume","seal",hit:true)});

            p["kenneth_q"] = M(d:9,weak:1,rules:new[]{R("bonus_damage",n:6,need:"rage",at:3),R("bonus_heal",n:4,need:"rage",at:3),R("gain","rage","불타는 도끼",1,cap:4,hit:true)});
            p["kenneth_w"] = M(block:10,rules:new[]{R("burn","inferno","업화",3,time:2),R("empower_basic","inferno_axe","업화 연장 공격",4,time:2)});
            p["kenneth_e"] = M(d:7,evade:20,time:2,rules:new[]{R("empower_basic","fierce","맹공격 강화",6,time:2),R("gain","rage","불타는 도끼",1,cap:4,hit:true),R("discount",n:1,target:"kenneth_e",hit:true)});
            p["kenneth_r"] = M(d:5,hits:4,weak:1,rules:new[]{R("burn","flame_crush","화염 분쇄",4,time:2,hit:true),R("bonus_damage",n:8,need:"rage",at:3),R("bonus_heal",n:4,need:"rage",at:3),R("gain","rage","불타는 도끼",2,cap:4,hit:true)});

            p["katja_q"] = M(d:10,rules:new[]{R("bonus_damage",n:5,need:"distance"),R("consume","distance",n:1,hit:true),R("empower_basic","reaper","잿빛 사신",5,hit:true)});
            p["katja_w"] = M(draw:1,rules:new[]{R("set","scan","드론 스캔",1,cap:1)});
            p["katja_e"] = M(d:8,evade:25,time:2,weak:1,rules:new[]{R("gain","distance","사격 거리",1,cap:2),R("empower_basic","reaper","잿빛 사신",5,hit:true)});
            p["katja_r"] = M(d:8,hits:2,rules:new[]{R("bonus_damage",n:8,need:"scan"),R("delayed_damage","blitz_final","정밀 조준 마지막 탄환",12,delay:1,hit:true),R("consume","scan",n:1,hit:true)});

            p["charlotte_q"] = M(d:4,weak:1,rules:new[]{R("delayed_damage","light_sphere","빛무리 구체",8,delay:1,hit:true)});
            p["charlotte_w"] = M(heal:8,rules:new[]{R("hot","healing_light","치유의 빛",3,time:2)});
            p["charlotte_e"] = M(block:12,evade:25,rules:new[]{R("guard","hope","희망의 보호막",4,time:2)});
            p["charlotte_r"] = M(block:12,rules:new[]{R("revive","miracle","기적 실현",18,time:2),R("guard","divine_guard","기적의 보호",6,time:2)});

            p["darko_q"] = M(evade:10,rules:new[]{R("empower_basic","loan","고리대금 강화 공격",7,time:2),R("gain","debt","채무 표식",1,cap:3)});
            p["darko_w"] = M(block:10,weak:1,rules:new[]{R("bonus_block",n:3,scale:"debt",cap:3),R("empower_basic","collection","수금한 공격력",5,time:2)});
            p["darko_e"] = M(d:10,weak:1,rules:new[]{R("bonus_damage",n:4,scale:"debt",cap:3),R("consume","debt",n:1,hit:true)});
            p["darko_r"] = M(d:20,weak:2,rules:new[]{R("bonus_damage",n:6,scale:"debt",cap:3),R("consume","debt",hit:true)});

            p["lenore_q"] = M(d:2,hits:4,rules:Trill(R("bonus_damage",n:8,need:"trill",at:2)));
            p["lenore_w"] = M(d:5,block:8,weak:1,rules:Trill(R("bonus_block",n:8,need:"trill",at:2)));
            p["lenore_e"] = M(d:8,weak:1,rules:Trill(R("bonus_damage",n:8,need:"trill",at:2)));
            p["lenore_r"] = M(d:6,weak:2,rules:new[]{R("summon","recital","고뇌의 광시곡",4,time:2,hit:true),R("delayed_damage","finale","광시곡 피날레",12,delay:2,hit:true)});

            p["garnet_q"] = M(d:5,hits:2,weak:1,rules:Pain(R("bonus_damage",n:6,need:"embrace"),R("consume","embrace",n:1,hit:true)));
            p["garnet_w"] = M(block:8,heal:3,rules:new[]{R("bonus_heal",n:3,scale:"pain",cap:3),R("delayed_damage","pain_release","억누른 고통 방출",6,delay:1),R("delayed_damage","charged_pain","충전된 고통",10,delay:1,need:"pain",at:3),R("discount",n:1,target:"garnet_q",need:"pain"),R("discount",n:2,target:"garnet_e",need:"pain",at:2),R("consume","pain")});
            p["garnet_e"] = M(d:9,evade:15,weak:1,rules:Pain(R("bonus_damage",n:6,need:"embrace"),R("consume","embrace",n:1,hit:true)));
            p["garnet_r"] = M(d:12,weak:1,rules:new[]{R("set","embrace","철처녀 표식",1,cap:1,hit:true),R("delayed_damage","iron_maiden","처형식 압박",14,delay:1,hit:true)});

            p["yumin_q"] = M(d:3,hits:3,rules:Wind(R("bonus_damage",n:7,need:"wind")));
            p["yumin_w"] = M(d:8,weak:1,rules:Wind(R("bonus_damage",n:6,need:"wind")));
            p["yumin_e"] = M(d:6,evade:20,time:2,rules:new[]{R("gain","wind","바람 지대",1,cap:2)});
            p["yumin_r"] = M(d:8,weak:1,rules:new[]{R("delayed_damage","crane","풍류운산 재상승",12,delay:1,hit:true),R("discount",n:2,target:"yumin_e",hit:true)});

            p["hisui_q"] = M(d:5,hits:2,weak:1,rules:new[]{R("gain","iaido","거합 검기",1,cap:3,hit:true),R("empower_basic","memory","검의 기억",5,time:2,hit:true),R("bonus_damage",n:6,need:"long_sword"),R("consume","long_sword",n:1,hit:true)});
            p["hisui_w"] = M(block:4,rules:new[]{R("bonus_damage",n:6,scale:"iaido",cap:3),R("bonus_block",n:6,need:"iaido"),R("bonus_heal",n:6,need:"iaido",at:3),R("discount",n:1,target:"hisui_q",need:"iaido",at:2,hit:true),R("discount",n:2,target:"hisui_e",need:"iaido",at:3,hit:true),R("consume","iaido",hit:true)});
            p["hisui_e"] = M(d:8,evade:20,rules:new[]{R("gain","iaido","거합 검기",1,cap:3,hit:true),R("empower_basic","memory","검의 기억",5,time:2,hit:true),R("bonus_damage",n:6,need:"long_sword"),R("consume","long_sword",n:1,hit:true)});
            p["hisui_r"] = M(block:10,rules:new[]{R("set","long_sword","모노호시자오",2,cap:2),R("empower_basic","long_memory","장도의 기억",8,time:2),R("delayed_damage","tsubame_gaeshi","츠바메가에시",12,delay:1)});

            p["justyna_q"] = M(d:3,hits:2,rules:new[]{R("bonus_damage",n:4,scale:"astra",cap:4),R("consume","astra",n:1),R("bonus_damage",n:6,need:"justice"),R("gain","astra","아스트라 에너지",1,cap:4,need:"justice",hit:true),R("consume","justice",n:1,hit:true),R("discount",n:1,target:"justyna_w",hit:true)});
            p["justyna_w"] = M(d:4,hits:2,weak:1,rules:new[]{R("set","justice","정의의 표적",1,cap:1,hit:true),R("gain","astra","아스트라 에너지",2,cap:4)});
            p["justyna_e"] = M(evade:20,rules:new[]{R("empower_basic","boost","부스트 대쉬",7,time:2,need:"astra"),R("consume","astra",n:1),R("gain","astra","아스트라 에너지",1,cap:4,need:"astra",at:0,eq:true)});
            p["justyna_r"] = M(d:5,hits:4,evade:35,rules:new[]{R("gain","astra","아스트라 에너지",3,cap:4),R("delayed_damage","bombardment","아스트라 폭격",10,delay:1,hit:true)});

            p["istvan_q"] = M(d:8,rules:Variable(R("bonus_damage",n:8,need:"variable",at:2)));
            p["istvan_w"] = M(d:6,block:7,rules:Variable(R("bonus_heal",n:5,need:"variable",at:2)));
            p["istvan_e"] = M(d:8,vulnerable:1,evade:15,rules:Variable(R("bonus_damage",n:6,need:"variable",at:2),R("bonus_block",n:5,need:"variable",at:2)));
            p["istvan_r"] = M(d:6,weak:1,rules:new[]{R("delayed_damage","alter_one","첫 번째 얼터",7,delay:1,hit:true),R("delayed_damage","alter_two","두 번째 얼터",7,delay:1,hit:true),R("delayed_damage","alter_three","세 번째 얼터",7,delay:1,hit:true)});

            p["xuelin_q"] = M(d:4,rules:BladeField(R("set","sword_out","날아간 어검",1,cap:1,hit:true),R("summon","flying_sword","비검잔위",4,time:2,hit:true)));
            p["xuelin_w"] = M(d:3,block:14,rules:BladeField(R("counter","sacred_guard","무진검결 반격",5,time:2),R("delayed_damage","guard_sword","검막의 검격",8,delay:1,hit:true)));
            p["xuelin_e"] = M(d:5,hits:2,evade:20,rules:BladeField(R("bonus_damage",n:6,need:"sword_out"),R("bonus_heal",n:3,need:"sword_out"),R("clear_effect","flying_sword",need:"sword_out"),R("summon","orbiting_sword","회수한 어검",3,time:2,need:"sword_out",hit:true),R("consume","sword_out",n:1,hit:true),R("delayed_damage","falling_sword","비섬보 낙검",7,delay:1,hit:true)));
            p["xuelin_r"] = M(d:6,rules:new[]{R("summon","sword_field","만검귀종 검진",4,time:3,hit:true),R("set","field_strikes","검진 낙검",3,cap:3,hit:true),R("empower_basic","chosen_blade","결심응진 어검",6,time:2,hit:true)});

            p["henry_q"] = M(d:4,hits:2,rules:new[]{R("bonus_damage",n:5,need:"chronograph"),R("delayed_damage","time_fracture","시차 균열 타이머",6,delay:2,hit:true)});
            p["henry_w"] = M(rules:new[]{R("set","chronograph","시간 제어 장치",1,cap:1),R("summon","chronograph_field","시간 제어 장치 영역",4,time:3)});
            p["henry_e"] = M(d:5,evade:25,time:2,rules:new[]{R("delayed_damage","rewind_impact","시간장치 도약",9,delay:1,need:"chronograph",hit:true),R("consume","chronograph",n:1,hit:true),R("discount",n:2,target:"henry_q"),R("discount",n:2,target:"henry_w")});
            p["henry_r"] = M(block:12,weak:1,rules:new[]{R("summon","chrono_field","시간 재구축 영역",3,time:2),R("delayed_damage","chrono_explosion","시간 재구축 폭발",18,delay:2)});

            p["blair_q"] = M(d:4,hits:2,rules:Shift(R("bonus_heal",n:4,need:"form",at:1,eq:true)));
            p["blair_w"] = M(d:3,hits:2,rules:Shift(R("bonus_damage",n:6,need:"form",at:0,eq:true),R("bonus_block",n:10,need:"form",at:1,eq:true),R("counter","bladestorm","칼날폭풍",4,time:2,need:"form",at:1,eq:true)));
            p["blair_e"] = M(d:7,rules:new[]{R("bonus_block",n:8,need:"form",at:0,eq:true),R("bonus_damage",n:5,need:"form",at:1,eq:true),R("set","form","블레이드 형태",1,cap:1,need:"form",at:0,eq:true),R("set","form","블레이드 형태",0,cap:1,need:"form",at:1,eq:true),R("set","shift","시프트",1,cap:1),R("gain","vp","VF 추적력",1,cap:3,hit:true)});
            p["blair_r"] = M(d:10,rules:new[]{R("bonus_damage",n:6,scale:"vp",cap:3),R("consume","vp",hit:true),R("gain","vp","VF 추적력",1,cap:3,hit:true),R("set","shift","시프트",1,cap:1),R("hot","xms","XMS-5 회복",3,time:2)});

            p["mirka_q"] = M(d:9,weak:1,rules:Impulse(R("delayed_damage","aftershock_one","첫 여진",5,delay:1,need:"impulse",at:4,hit:true),R("delayed_damage","aftershock_two","둘째 여진",6,delay:2,need:"impulse",at:4,hit:true)));
            p["mirka_w"] = M(block:12,rules:new[]{R("gain","impulse","리펄스 게이지",2,cap:4),R("counter","repulse_barrier","리펄스 배리어",3,time:2)});
            p["mirka_e"] = M(d:5,hits:2,weak:1,rules:Impulse(R("discount",n:1,target:"mirka_q",hit:true)));
            p["mirka_r"] = M(d:14,evade:30,rules:new[]{R("delayed_damage","downburst","다운버스트 착지",16,delay:1,hit:true),R("gain","impulse","리펄스 게이지",2,cap:4,hit:true)});

            p["fenrir_q"] = M(d:6,rules:new[]{R("bonus_damage",n:6,need:"claw",at:2),R("bonus_heal",n:4,need:"claw",at:2),R("discount",n:2,target:"fenrir_e",need:"claw",at:2,hit:true),R("gain","claw","손톱",1,cap:3,hit:true),R("consume","claw",need:"claw",at:2,hit:true)});
            p["fenrir_w"] = M(d:6,evade:20,rules:new[]{R("burn","vf_absorption","VF 흡수",3,time:2,hit:true),R("hot","vf_recovery","VF 회복",3,time:2,hit:true),R("bonus_damage",n:5,prev:"fenrir_w"),R("discount",n:1,target:"fenrir_e",hit:true)});
            p["fenrir_e"] = M(evade:20,time:2,rules:new[]{R("empower_basic","hunt","사냥 개시",8,time:2)});
            p["fenrir_r"] = M(block:9,rules:new[]{R("delayed_damage","fatal_bite","숨통 끊기",18,delay:1),R("burn","fatal_absorption","숨통 VF 흡수",4,time:2),R("hot","fatal_recovery","숨통 VF 회복",4,time:2)});

            p["coraline_q"] = M(d:7,weak:1,rules:MirrorShard(R("bonus_damage",n:6,need:"mirror",at:1,eq:true),R("bonus_damage",n:8,need:"mirror",at:2,eq:true)));
            p["coraline_w"] = M(rules:new[]{R("set","mirror","백·흑 거울",1,cap:2,need:"mirror",at:0,eq:true),R("set","mirror","백·흑 거울",2,cap:2,need:"mirror",at:1,eq:true),R("set","mirror","백·흑 거울",1,cap:2,need:"mirror",at:2,eq:true)});
            p["coraline_e"] = M(d:8,weak:1,rules:MirrorShard(R("bonus_block",n:9,need:"mirror",at:1,eq:true),R("bonus_damage",n:7,need:"mirror",at:2,eq:true)));
            p["coraline_r"] = M(evade:30,time:2,rules:new[]{R("empower_basic","afterimage","굴절 잔상",8,time:2),R("discount",n:1,target:"coraline_q"),R("discount",n:1,target:"coraline_w"),R("discount",n:1,target:"coraline_e")});

            p["bihyung_q"] = M(rules:Divine(R("empower_basic","ttuk","뚝딱 강화 공격",6,time:2),R("empower_basic","ttuk_repeat","뚝딱 재공격",10,time:2,prev:"bihyung_q"),R("hot","ttuk_recovery","뚝딱 회복",2,time:2)));
            p["bihyung_w"] = M(d:8,block:7,evade:15,rules:Divine(R("burn","dokkaebi_ground","도깨비 불 지대",3,time:2,hit:true)));
            p["bihyung_e"] = M(d:9,weak:1,rules:Divine(R("counter","wager","내기 도발",4,time:2,hit:true)));
            p["bihyung_r"] = M(d:8,block:13,rules:new[]{R("delayed_damage","giant_slam","거대 도깨비 착지",12,delay:1,hit:true),R("counter","giant_mask","도깨비 가면",5,time:2),R("gain","divine","신력",2,cap:3,hit:true)});

            p["craver_q"] = M(d:3,hits:2,rules:new[]{R("bonus_damage",n:10,need:"magazine",at:2,eq:true),R("set","magazine","탄창 단계",1,cap:2,need:"magazine",at:0,eq:true,hit:true),R("set","magazine","탄창 단계",2,cap:2,need:"magazine",at:1,eq:true,hit:true),R("set","magazine","탄창 단계",0,cap:2,need:"magazine",at:2,eq:true,hit:true)});
            p["craver_w"] = M(d:4,hits:2,block:4,evade:15,weak:1,rules:new[]{R("bonus_damage",n:6,need:"magazine",at:2,eq:true),R("discount",n:2,target:"craver_e",need:"magazine",at:2,eq:true,hit:true)});
            p["craver_e"] = M(d:5,evade:20,rules:new[]{R("bonus_damage",n:5,need:"magazine"),R("set","magazine","탄창 단계",0,cap:2),R("discount",n:1,target:"craver_q")});
            p["craver_r"] = M(d:16,heal:3,rules:new[]{R("bonus_damage",n:8,prev:"craver_w"),R("set","magazine","탄창 단계",0,cap:2),R("discount",n:2,target:"craver_q"),R("discount",n:2,target:"craver_w"),R("discount",n:3,target:"craver_e")});

            p["lucia_q"] = M(d:7,rules:Crystal(R("bonus_damage",n:7,need:"glorious"),R("consume","glorious",n:1,hit:true)));
            p["lucia_w"] = M(d:2,hits:4,rules:Crystal());
            p["lucia_e"] = M(evade:25,time:2,rules:new[]{R("set","glorious","찬란한 낭만",1,cap:1),R("empower_basic","crystal_dash","수정 추격",7,time:2,need:"crystal"),R("discount",n:1,target:"lucia_e",need:"crystal"),R("consume","crystal",n:1)});
            p["lucia_r"] = M(d:20,weak:1,rules:Crystal(R("bonus_damage",n:9,need:"crystal"),R("consume","crystal",n:1,hit:true),R("set","crystal","수정",1,cap:1,hit:true)));

            p["ceres_q"] = M(block:9,evade:15,rules:new[]{R("bonus_damage",n:12,need:"rush"),R("consume","rush",n:1,hit:true),R("set","rush","쇄도 후 관철",1,cap:1,need:"rush",at:0,eq:true),R("delayed_damage","rush_shield","쇄도 보호막 폭발",6,delay:1,need:"rush",at:0,eq:true)});
            p["ceres_w"] = M(d:9,weak:1,rules:new[]{R("empower_basic","obsidian","흑요석 검격",5,time:2,hit:true)});
            p["ceres_e"] = M(block:10,evade:15,rules:new[]{R("guard","loyal_guard","경호",4,time:2),R("hot","guardian_recovery","경호 회복",3,time:2)});
            p["ceres_r"] = M(block:12,rules:new[]{R("guard","oath","빛에게 바치는 맹세",8,time:3)});
        }

        static SkillRule[] TwinInk(int damage)
        {
            return new[]{R("bonus_damage",n:damage,need:"stance",at:0,eq:true,other:"ink",oat:2,oeq:true),R("bonus_damage",n:damage,need:"stance",at:1,eq:true,other:"ink",oat:1,oeq:true),R("set","ink","블루&레드 잉크",1,cap:2,need:"stance",at:0,eq:true,hit:true),R("set","ink","블루&레드 잉크",2,cap:2,need:"stance",at:1,eq:true,hit:true)};
        }
        static SkillRule[] Relic(params SkillRule[] extra) { return Join(extra,R("bonus_heal",n:6,need:"antiquity",at:2),R("gain","antiquity","고대의 정수",1,cap:3,hit:true),R("consume","antiquity",need:"antiquity",at:2,hit:true),R("consume","awakened",n:1,hit:true)); }
        static SkillRule[] Trill(params SkillRule[] extra) { return Join(extra,R("gain","trill","트릴 연주",1,cap:3),R("consume","trill",need:"trill",at:2)); }
        static SkillRule[] Pain(params SkillRule[] extra) { return Join(extra,R("gain","pain","고통",1,cap:3)); }
        static SkillRule[] Wind(params SkillRule[] extra) { return Join(extra,R("bonus_block",n:8,need:"wind"),R("consume","wind",n:1,hit:true),R("discount",n:2,target:"yumin_e",hit:true)); }
        static SkillRule[] Variable(params SkillRule[] extra) { return Join(extra,R("gain","variable","연산 변수",1,cap:3),R("consume","variable",need:"variable",at:2)); }
        static SkillRule[] BladeField(params SkillRule[] extra) { return Join(extra,R("bonus_damage",n:4,need:"field_strikes"),R("consume","field_strikes",n:1,hit:true)); }
        static SkillRule[] Shift(params SkillRule[] extra) { return Join(extra,R("bonus_damage",n:6,need:"shift"),R("bonus_heal",n:4,need:"shift"),R("consume","shift",n:1,hit:true),R("gain","vp","VF 추적력",1,cap:3,hit:true),R("empower_basic","blade_followup","블레이드 시프트 후속타",4,time:2,hit:true)); }
        static SkillRule[] Impulse(params SkillRule[] extra) { return Join(extra,R("bonus_damage",n:3,scale:"impulse",cap:4,need:"impulse",at:4),R("consume","impulse",need:"impulse",at:4,hit:true),R("gain","impulse","리펄스 게이지",1,cap:4,need:"impulse",at:3,eq:true,hit:true),R("gain","impulse","리펄스 게이지",1,cap:4,need:"impulse",at:0,eq:true,hit:true),R("gain","impulse","리펄스 게이지",1,cap:4,need:"impulse",at:1,eq:true,hit:true),R("gain","impulse","리펄스 게이지",1,cap:4,need:"impulse",at:2,eq:true,hit:true)); }
        static SkillRule[] MirrorShard(params SkillRule[] extra) { return Join(extra,R("bonus_damage",n:4,need:"shard"),R("consume","shard",n:1,hit:true),R("set","shard","거울 파편",1,cap:1,hit:true)); }
        static SkillRule[] Divine(params SkillRule[] extra) { return Join(extra,R("summon","divine_light","신력 도깨비 불",3,time:2,need:"divine",at:2),R("gain","divine","신력",1,cap:3),R("consume","divine",need:"divine",at:2)); }
        static SkillRule[] Crystal(params SkillRule[] extra) { return Join(extra,R("set","crystal","수정",1,cap:1,hit:true),R("empower_basic","crystal_shot","수정 사격",6,time:2,hit:true)); }
        static SkillRule[] Join(SkillRule[] start, params SkillRule[] end) { var list=new List<SkillRule>(start); list.AddRange(end); return list.ToArray(); }

        static SkillMechanicProfile M(int d=0,int hits=1,int block=0,int heal=0,int draw=0,int weak=0,int vulnerable=0,int evade=0,int time=1,SkillRule[] rules=null)
        {
            return new SkillMechanicProfile { damage=d,hits=hits,block=block,heal=heal,draw=draw,weak=weak,vulnerable=vulnerable,evasion=evade,duration=time,energy=0,poison=0,strength=0,freeCastCount=0,freeCastTargets=new string[0],freeCastOnHit=false,freeCastLastSkill=false,rules=rules??new SkillRule[0] };
        }
        static SkillRule R(string op,string key="",string label="",int n=0,int cap=3,int time=2,int delay=1,string scale="",string need="",int at=1,bool eq=false,string other="",int oat=1,bool oeq=false,string prev="",string target="",bool hit=false)
        {
            return new SkillRule { op=op,key=key,label=label,amount=n,cap=cap,duration=time,delay=delay,scaleKey=scale,conditionKey=need,conditionAmount=at,conditionExact=eq,conditionKey2=other,conditionAmount2=oat,conditionExact2=oeq,conditionPrevious=prev,targetCard=target,onHit=hit };
        }
    }
}
