using System.Collections.Generic;

namespace Lumia
{
    // Turn-based adaptations of the stored 12.5.0 tooltips, not current live-game values.
    // Identity resources are isolated by the source card's owner by SkillMechanics.
    public static class SkillIdentityClassic
    {
        public static void Populate(Dictionary<string, SkillMechanicProfile> p)
        {
            // Picturesque, telephoto setup and the last frame of a time lapse.
            P(p, "nathapon_q", 7, G("picturesque", "구도", 1, 3, hit:true), B("bonus_damage", 4, need:"picturesque", min:3));
            P(p, "nathapon_w", 3, Fx("summon", "timelapse", "타임 랩스", 3, 2), Fx("delayed_damage", "last_frame", "마지막 사진", 6, delay:2), G("picturesque", "구도", 1, 3, hit:true));
            P(p, "nathapon_e", 7, Fx("empower_basic", "panorama", "파노라마", 7), G("picturesque", "구도", 1, 3, hit:true));
            P(p, "nathapon_r", 0, S("picturesque", "구도", 3), B("bonus_block", 12), D("nathapon_q", 1), D("nathapon_e", 1));

            // Rage, a prepared guard and the separate fury jab.
            P(p, "nicky_q", 9, G("rage", "분노", 1, 4, hit:true), B("bonus_damage", 4, previous:"nicky_q"));
            P(p, "nicky_w", 0, G("guard_ready", "가드 준비", 1, 1), Fx("counter", "reverse", "리버스", 5, 2), G("rage", "분노", 1, 4));
            P(p, "nicky_e", 11, B("bonus_damage", 8, need:"guard_ready"), Spend("guard_ready", "가드 준비"), G("rage", "분노", 1, 4, hit:true));
            P(p, "nicky_r", 20, B("bonus_damage", 4, scale:"rage", cap:4), Spend("rage", "분노"));

            // Inspiration stores completed scissors/shadow strokes until Masterpiece.
            P(p, "daniel_q", 8, Fx("empower_basic", "dusk", "그림자 가위", 4), G("inspiration", "영감", 1, 4, need:"subject", hit:true));
            P(p, "daniel_w", 0, S("subject", "영감의 대상", 1, 1), S("inspiration", "영감", 0, 4));
            P(p, "daniel_e", 6, Fx("empower_basic", "shadow", "그림자 이동", 7), G("inspiration", "영감", 1, 4, need:"subject", hit:true));
            P(p, "daniel_r", 14, B("bonus_damage", 4, scale:"inspiration", cap:4), Spend("inspiration", "영감"), Spend("subject", "영감의 대상"), Fx("delayed_damage", "masterpiece", "걸작 완성", 8, delay:2));

            // Yellow -> red -> blue, with genuinely different two-color combinations.
            Paint(p, "tia_q", 3);
            P(p, "tia_w", 0, G("brush", "붓 색", 1, 2), S("brush", "붓 색", 0, 2, need:"brush", min:2, exact:true)).block = 0;
            Paint(p, "tia_e", 8);
            P(p, "tia_r", 17, B("bonus_damage", 7, need:"paint"), Spend("paint", "물감"));

            // Victim is a persistent calling-card target; the heist has a second landing.
            P(p, "laura_q", 3, B("bonus_heal", 2, need:"victim"), Fx("empower_basic", "thief", "괴도", 3));
            P(p, "laura_w", 7, S("victim", "예고장", 1, 1));
            P(p, "laura_e", 5, B("bonus_heal", 3, need:"victim"), Fx("empower_basic", "thief", "괴도", 3));
            P(p, "laura_r", 15, Fx("delayed_damage", "heist", "황혼의 착지", 10), B("bonus_block", 8, need:"victim"), B("bonus_heal", 5, need:"victim"), Spend("victim", "예고장"));

            // Recoil ramps separately from Blue Viper's movement punishment.
            P(p, "lenox_q", 7, G("recoil", "회오리", 1, 3, hit:true), B("bonus_damage", 4, need:"recoil", min:2), Spend("recoil", "회오리", need:"recoil", min:2));
            P(p, "lenox_w", 10, B("bonus_damage", 5, need:"viper"), G("tackle", "태클", 1, 3, hit:true));
            P(p, "lenox_e", 8, B("bonus_damage", 6, need:"viper"), B("bonus_block", 2, scale:"tackle", cap:3), Spend("tackle", "태클"));
            P(p, "lenox_r", 13, Fx("bleed", "blue_viper", "푸른뱀", 4, 3), S("viper", "푸른뱀 표식", 1, 1));

            // Waterways and dives create pools; surfing converts the prepared water.
            P(p, "leon_q", 7, G("pool", "물웅덩이", 1, 3, hit:true), Fx("empower_basic", "pool_shark", "풀 샤크", 4, need:"pool"));
            P(p, "leon_w", 0, B("bonus_block", 3, scale:"pool", cap:3), Fx("empower_basic", "pool_shark", "풀 샤크", 4, need:"pool"));
            P(p, "leon_e", 5, G("pool", "물웅덩이", 1, 3), B("bonus_block", 6)).heal = 0;
            P(p, "leon_r", 19, B("bonus_damage", 4, scale:"pool", cap:3), Spend("pool", "물웅덩이"));

            // Separate attacks accelerate a stuck Semtex; the remaining fuse is cancelled.
            Rozzi(p, "rozzi_q", 7);
            Rozzi(p, "rozzi_w", 7);
            Rozzi(p, "rozzi_e", 10);
            P(p, "rozzi_r", 0, S("semtex", "부착 폭탄", 1, 1), Fx("delayed_damage", "semtex_fuse", "셈텍스탄", 15, delay:2)).freeCastCount = 0;

            // Cleaner hit -> chase -> pressure wash, not a permanent strength buff.
            P(p, "luke_q", 8, S("clean_target", "세제 표식", 1, 1), B("bonus_damage", 6, previous:"luke_q"));
            P(p, "luke_w", 6, B("bonus_heal", 5, need:"clean_target"), Fx("empower_basic", "elbow", "강박증", 4));
            P(p, "luke_e", 7, B("bonus_damage", 5, need:"clean_target"));
            P(p, "luke_r", 21, B("bonus_damage", 8, need:"clean_target"), Spend("clean_target", "세제 표식"));

            // BAC is gained by drinking and explicitly spent on tipsy moves.
            P(p, "dailin_q", 10, B("bonus_damage", 5, need:"bac"), Spend("bac", "취기", 1), Fx("empower_basic", "dialed_in", "쌍타", 4, need:"bac"));
            P(p, "dailin_w", 0, G("bac", "취기", 2, 4), Fx("empower_basic", "liquid", "술기운", 3, scale:"bac", cap:4));
            P(p, "dailin_e", 7, B("bonus_damage", 4, need:"bac"), Spend("bac", "취기", 1));
            P(p, "dailin_r", 18, B("bonus_damage", 5, scale:"bac", cap:4), Spend("bac", "취기"));

            // Bow stance is reversible; Soar and the ultimate fill Flow.
            P(p, "rio_q", 0, Toggle("bow", "장궁 자세"), Fx("empower_basic", "flow", "흐름", 6, need:"flow", min:3), Spend("flow", "흐름", need:"flow", min:3)).exhaust = 0;
            P(p, "rio_w", 8, B("bonus_damage", 4, need:"bow"), Fx("empower_basic", "hankyu", "단궁 연사", 4, need:"bow", min:0, exact:true));
            P(p, "rio_e", 9, S("flow", "흐름", 3), Fx("empower_basic", "soar", "비상", 5));
            P(p, "rio_r", 17, B("bonus_damage", 7, need:"bow"), Fx("delayed_damage", "hankyu_end", "마지막 화살", 7, need:"bow", min:0, exact:true), S("flow", "흐름", 3));

            // Cameras respond to Fast Forward; recordings unlock Broadcast in this combat.
            P(p, "martina_q", 7, B("bonus_damage", 4, scale:"camera", cap:2), B("bonus_damage", 4, need:"record", min:3));
            P(p, "martina_w", 0, G("camera", "숨겨진 카메라", 1, 2));
            P(p, "martina_e", 6, Fx("delayed_damage", "rewind", "되감기 귀환", 5), Fx("hot", "broadcast_rewind", "방송 귀환", 3, 2, need:"record", min:3));
            P(p, "martina_r", 6, G("record", "녹화", 1, 3), B("bonus_damage", 9, need:"record", min:3), D("martina_q", 1, need:"record", min:2)).exhaust = 0;

            // Veil protects then bursts; Exclusive supports that veil rather than raw healing.
            P(p, "mai_q", 6, S("pin", "핀", 1, 1), B("bonus_damage", 4, need:"pin"));
            P(p, "mai_w", 0, S("veil", "숄 장막", 1, 1), Fx("counter", "veil_guard", "숄 반격", 3, 1), Fx("delayed_damage", "veil_burst", "장막 폭발", 7));
            P(p, "mai_e", 7, B("bonus_block", 6, need:"veil"), Fx("empower_basic", "catwalk", "캣 워크", 4));
            P(p, "mai_r", 0, Fx("hot", "exclusive", "익스클루시브", 4, 3), B("bonus_heal", 6, need:"veil"), Spend("veil", "숄 장막")).heal = 6;

            // Rift enables displacement -> Rattled -> a fatal basic attack.
            P(p, "markus_q", 0, Fx("empower_basic", "war", "전투 교범", 5), Fx("empower_basic", "fatal", "치명타격", 9, need:"rattled"), Spend("rattled", "흔들림"));
            P(p, "markus_w", 10, G("rattled", "흔들림", 1, 1, need:"rift", hit:true), B("bonus_damage", 4, need:"rift"));
            P(p, "markus_e", 9, G("rattled", "흔들림", 1, 1, need:"rift", hit:true), B("bonus_block", 5, need:"rift"));
            P(p, "markus_r", 20, S("rift", "지각 균열", 1, 1), Fx("delayed_damage", "rift_impact", "균열 충격", 6));

            // Tough Body feeds spinning defense; a rock establishes a wall collision.
            P(p, "magnus_q", 8, S("wall_pressure", "벽 압박", 1, 1), G("tough", "근성", 1, 4, hit:true));
            P(p, "magnus_w", 4, Fx("summon", "spin", "17대 1", 3, 2), B("bonus_block", 2, scale:"tough", cap:4), G("tough", "근성", 1, 4, hit:true));
            P(p, "magnus_e", 10, B("bonus_damage", 7, need:"wall_pressure"), Spend("wall_pressure", "벽 압박"), G("tough", "근성", 1, 4, hit:true));
            P(p, "magnus_r", 17, S("tough", "근성", 4, 4), Fx("delayed_damage", "bike", "폭주 바이크", 12));

            // Returning butterfly, four dream hits and a breakable sleep fuse.
            Dream(p, "vanya_q", 6, Fx("delayed_damage", "returning", "귀환 나비", 4));
            Dream(p, "vanya_w", 0, Fx("summon", "embrace", "쪽빛 바람", 3, 2));
            Dream(p, "vanya_e", 9, B("bonus_damage", 7, need:"sleep"), Fx("clear_effect", "sleep_fuse", "잠 깨우기", 0, hit:true), Spend("sleep", "몽롱함", hit:true));
            P(p, "vanya_r", 15, S("sleep", "몽롱함", 1, 1), Fx("delayed_damage", "sleep_fuse", "꿈결 가루", 10, delay:2));

            // Sentry capacity, railgun charges and finite overclock discounts.
            P(p, "barbara_q", 0, G("sentry", "센트리건", 1, 2), Fx("summon", "sentry", "센트리건", 3, 3), Fx("summon", "sentry", "센트리건 2기", 6, 3, need:"sentry"));
            P(p, "barbara_w", 7, B("bonus_damage", 3, scale:"sentry", cap:2), G("railgun", "레일건", 1, 3, need:"sentry", hit:true));
            P(p, "barbara_e", 9, B("bonus_damage", 4, scale:"railgun", cap:3), Spend("railgun", "레일건"));
            P(p, "barbara_r", 0, Fx("summon", "sentry", "오버클럭 센트리", 7, 2, need:"sentry"), D("barbara_q", 2), D("barbara_w", 2), D("barbara_e", 2));

            // Rooted trap target and a separate falcon mark.
            P(p, "bernice_q", 8, B("bonus_damage", 7, need:"trap"), Spend("trap", "덫 속박"), Fx("empower_basic", "falcon", "매 추적", 4, need:"falcon"));
            P(p, "bernice_w", 0, S("trap", "덫 속박", 1, 1), Fx("delayed_damage", "foothold", "사냥 덫", 7));
            P(p, "bernice_e", 0, S("falcon", "매 표식", 1, 1), D("bernice_q", 1), D("bernice_r", 1));
            P(p, "bernice_r", 12, S("trap", "덫 속박", 1, 1), Fx("delayed_damage", "bola", "올가미 폭발", 12));

            // Blood bank, coffin consumption and the delayed reign explosion.
            P(p, "bianca_q", 8, G("blood", "혈액", 1, 4, hit:true), Fx("delayed_damage", "blood_pool", "혈액 웅덩이", 4)).heal = 0;
            P(p, "bianca_w", 0, B("bonus_heal", 3, scale:"blood", cap:4), Spend("blood", "혈액"), Fx("hot", "coffin", "관 속 회복", 3, 2), D("bianca_q", 1, need:"blood", min:3), D("bianca_e", 1, need:"blood", min:3)).heal = 3;
            P(p, "bianca_e", 9, G("blood", "혈액", 1, 4, hit:true), B("bonus_heal", 4, need:"blood", min:3));
            P(p, "bianca_r", 10, Fx("burn", "reign", "진조의 영역", 3, 2), Fx("delayed_damage", "reign_end", "진조의 폭발", 12, delay:2), G("blood", "혈액", 2, 4, hit:true)).heal = 4;

            // Bombs are actual setup: Q cannot inflict the former direct poison/damage.
            P(p, "celine_q", 0, G("bomb", "플라즈마 폭탄", 1, 4)).freeCastCount = 0;
            P(p, "celine_w", 0, B("bonus_damage", 5, scale:"bomb", cap:4), B("bonus_damage", 6, scale:"fusion", cap:4), G("shell", "빈 폭탄", 1, 4, need:"bomb"), Spend("bomb", "플라즈마 폭탄"), Spend("fusion", "자력 융합"));
            P(p, "celine_e", 8, B("bonus_block", 3, need:"bomb"));
            P(p, "celine_r", 0, S("fusion", "융합 폭탄", 0, 4), G("fusion", "융합 폭탄", 1, 4, need:"bomb"), G("fusion", "융합 폭탄", 1, 4, need:"bomb", min:2), G("fusion", "융합 폭탄", 1, 4, need:"bomb", min:3), G("fusion", "융합 폭탄", 1, 4, need:"bomb", min:4), G("fusion", "융합 폭탄", 1, 4, need:"shell"), Spend("bomb", "플라즈마 폭탄"), Spend("shell", "빈 폭탄"));

            // Sauce, food recovery and the wok's separate follow-up.
            P(p, "sho_q", 8, S("sauce", "소스범벅", 1, 1));
            P(p, "sho_w", 0, Fx("hot", "meal", "식사 시간", 3, 2), G("meal", "든든한 식사", 1, 3)).heal = 4;
            P(p, "sho_e", 9, B("bonus_damage", 5, need:"sauce"), D("sho_e", 2, previous:"sho_q"), G("meal", "든든한 식사", 1, 3, hit:true));
            P(p, "sho_r", 13, Fx("burn", "crisp", "뜨거운 맛", 4, 3), B("bonus_damage", 3, scale:"meal", cap:3), Spend("meal", "든든한 식사"));

            // Daggers are collected by Bottom Line, not an arbitrary Q damage stack.
            P(p, "shoichi_q", 8, G("dagger", "바닥 단검", 1, 4, previous:"shoichi_q", hit:true));
            P(p, "shoichi_w", 8, B("bonus_damage", 4, scale:"dagger", cap:4), D("shoichi_w", 2, need:"dagger"), B("bonus_damage", 4, need:"risk"), Spend("dagger", "바닥 단검"));
            P(p, "shoichi_e", 8, S("risk", "협상 표식", 1, 1), G("dagger", "바닥 단검", 1, 4, hit:true));
            P(p, "shoichi_r", 17, S("dagger", "바닥 단검", 4, 4), D("shoichi_w", 2));

            // Wilson's location changes bubble/pull outcomes and return empowers a basic.
            P(p, "sissela_q", 8, S("wilson", "윌슨 외출", 1, 1));
            P(p, "sissela_w", 0, Fx("delayed_damage", "bubble", "윌슨의 거품", 8), S("wilson", "윌슨 외출", 0, 1), Fx("empower_basic", "wilson_home", "윌슨 귀환", 5));
            P(p, "sissela_e", 7, B("bonus_damage", 6, need:"wilson"), B("bonus_block", 8, need:"wilson", min:0, exact:true));
            P(p, "sissela_r", 23, Fx("delayed_damage", "free", "모두 해방", 8), B("bonus_block", 6, need:"wilson", min:0, exact:true)).heal = 0;

            // Q/W/E generate finite fuel; Shift Gears toggles the bike and consumes it.
            Bike(p, "silvia_q", 7);
            Bike(p, "silvia_w", 8);
            Bike(p, "silvia_e", 8);
            P(p, "silvia_r", 0, S("bike", "바이크", 1, 1, need:"fuel"), S("bike", "바이크", 0, 1, need:"bike"), B("bonus_block", 6, need:"fuel"), Spend("fuel", "연료", 1), Fx("empower_basic", "dismount", "하차 사격", 6, need:"bike"));

            // Pawn, knight and rook stay distinct until the all-piece checkmate.
            P(p, "adela_q", 5, G("pawn", "폰", 1, 4, hit:true), B("bonus_damage", 6, need:"pawn", min:3));
            P(p, "adela_w", 7, S("knight", "나이트", 1, 1), B("bonus_damage", 3, scale:"pawn", cap:3));
            P(p, "adela_e", 9, S("rook", "룩", 1, 1), B("bonus_damage", 5, need:"knight"), B("bonus_block", 4, need:"pawn"));
            P(p, "adela_r", 17, B("bonus_damage", 3, scale:"pawn", cap:4), B("bonus_damage", 5, need:"knight"), B("bonus_damage", 5, need:"rook"), Spend("pawn", "폰"), Spend("knight", "나이트"), Spend("rook", "룩"));

            // Oil needs an igniter; flames use their own finite burn instead of poison.
            P(p, "adriana_q", 7, Fx("burn", "pyromania", "활활", 3, 2), Fx("burn", "oil_fire", "기름 화재", 5, 2, need:"oil"), Spend("oil", "기름"));
            P(p, "adriana_w", 0, G("oil", "기름", 1, 3));
            P(p, "adriana_e", 8, Fx("burn", "trail", "불길 흔적", 3, 2), Fx("burn", "oil_fire", "기름 화재", 5, 2, need:"oil"), Spend("oil", "기름"));
            P(p, "adriana_r", 15, Fx("burn", "cocktail", "화염 지대", 4, 3), B("bonus_damage", 3, scale:"oil", cap:3), Spend("oil", "기름"));

            // Sun/moon/star signs are cyclic, and prophecy preserves a conjunction.
            Star(p, "adina_q", 7, 0);
            Star(p, "adina_w", 8, 1);
            Star(p, "adina_e", 6, 2);
            P(p, "adina_r", 0, G("sign", "별자리", 1, 2), S("sign", "별자리", 0, 2, need:"sign", min:2, exact:true), S("conjunction", "합", 1, 1)).exhaust = 0;

            // Three strikes exploit; Reinforce primes only one future attack.
            Exploit(p, "isaac_q", 9);
            P(p, "isaac_w", 0, Fx("empower_basic", "reinforce", "경화", 6), S("reinforce", "경화 준비", 1, 1));
            Exploit(p, "isaac_e", 8, S("arrest", "검거", 1, 1), B("bonus_damage", 6, need:"arrest"), Spend("arrest", "검거", need:"arrest"));
            P(p, "isaac_r", 20, B("bonus_damage", 5, scale:"exploit", cap:3), Spend("exploit", "착취"));

            // Recognition -> infiltrating shot, plus actual successive EMP pulses.
            P(p, "alex_q", 8, G("gauss", "코일 충전", 1, 3, hit:true), Fx("empower_basic", "gauss", "코일건", 4), B("bonus_damage", 5, need:"infiltrate"), Spend("infiltrate", "잠입"));
            P(p, "alex_w", 8, S("recognition", "타겟 마커", 1, 1), Fx("empower_basic", "recognition", "표적 사격", 5));
            P(p, "alex_e", 7, S("infiltrate", "잠입", 1, 1), B("bonus_block", 5, need:"recognition"));
            P(p, "alex_r", 12, Fx("summon", "emp", "정밀 폭격", 5, 3), B("bonus_damage", 2, scale:"gauss", cap:3), Spend("gauss", "코일 충전"));

            // Ring ropes interact with roundhouse; weaving precedes a healing knee.
            P(p, "jan_q", 9, B("bonus_heal", 5, need:"weave"), Spend("weave", "위빙"), G("unyielding", "열혈", 1, 3, hit:true));
            P(p, "jan_w", 10, B("bonus_damage", 7, need:"ring"), B("bonus_damage", 3, scale:"unyielding", cap:3), Spend("unyielding", "열혈"), B("bonus_heal", 4, need:"weave"), Spend("weave", "위빙"));
            P(p, "jan_e", 0, S("weave", "위빙", 1, 1), Fx("empower_basic", "weave", "위빙 일격", 5), D("jan_q", 1));
            P(p, "jan_r", 14, S("ring", "링 로프", 1, 1), S("unyielding", "열혈", 3), Fx("counter", "ring_rope", "링 반동", 4, 2));

            // Hazard Shield changes First Response, then the helicopter arrives later.
            P(p, "estelle_q", 0, Fx("empower_basic", "suppression", "진압", 7));
            P(p, "estelle_w", 9, Fx("summon", "extinguisher", "소화기 분사", 4, 2, need:"hazard"));
            P(p, "estelle_e", 0, Toggle("hazard", "방패방어"), B("bonus_damage", 9, need:"hazard"), Fx("counter", "hazard", "방패 반격", 3, 2));
            P(p, "estelle_r", 0, Fx("delayed_damage", "helitack", "헬기 물폭탄", 18), Fx("hot", "rescue", "구조 지원", 4, 2)).heal = 5;

            // Ampere -> backstep converts charge, a charged ring and two thunderbolts.
            P(p, "aiden_q", 7, G("ampere", "암페어", 1, 4, hit:true), B("bonus_damage", 4, need:"overcharge"), D("aiden_q", 1, need:"overcharge"), Spend("overcharge", "과전하", 1));
            P(p, "aiden_w", 5, Fx("delayed_damage", "dissipation", "전하 소산", 8), Fx("burn", "sparkring", "전기 고리", 3, 2, need:"ampere", min:3));
            P(p, "aiden_e", 7, S("rush", "볼트 표식", 1, 1), B("bonus_damage", 6, need:"rush"), Spend("rush", "볼트 표식", need:"rush"), S("overcharge", "과전하", 2, 2, need:"ampere", min:3), Spend("ampere", "암페어", need:"ampere", min:3));
            P(p, "aiden_r", 16, S("ampere", "암페어", 4, 4), Fx("delayed_damage", "thunder", "두 번째 낙뢰", 13));

            // VF is distinct from overflow; shield uses it while E marks only once.
            P(p, "echion_q", 7, G("vf", "VF", 1, 4, hit:true), B("bonus_damage", 4, need:"overflow"), Spend("overflow", "VF 폭주", 1));
            P(p, "echion_w", 0, B("bonus_block", 3, scale:"vf", cap:4), Spend("vf", "VF"));
            P(p, "echion_e", 7, G("vf", "VF", 1, 4, hit:true), D("echion_e", 7, need:"bite", min:0, exact:true, hit:true), S("bite", "송곳니 표식", 1, 1, hit:true)).freeCastCount = 0;
            P(p, "echion_r", 5, S("overflow", "VF 폭주", 2, 2, need:"vf", min:3), Fx("burn", "overflow", "VF 폭주", 4, 2, need:"vf", min:3), B("bonus_damage", 4, scale:"vf", cap:4), Spend("vf", "VF"), D("echion_e", 7, need:"vf", min:3)).freeCastCount = 0;

            // Chill accumulates; the glacier can be broken by a following strike.
            Chill(p, "elena_q", 6, Fx("delayed_damage", "icicle", "고드름 파열", 4));
            Chill(p, "elena_w", 8, D("elena_q", 1, need:"field"));
            P(p, "elena_e", 0, S("field", "빙결 지대", 1, 1), G("steps", "스텝", 1, 3), B("bonus_block", 2, scale:"steps", cap:3));
            P(p, "elena_r", 17, S("field", "빙결 지대", 1, 1), S("chill", "냉기", 3), S("frozen", "빙결", 1, 1));

            // A censer is a shared amplifier for the light and the delayed scripture.
            P(p, "johann_q", 7, B("bonus_damage", 5, need:"censer"), B("bonus_heal", 4, need:"censer"));
            P(p, "johann_w", 0, S("censer", "신성의 향로", 1, 1), Fx("hot", "censer", "향로 회복", 3, 3), Fx("empower_basic", "censer", "향로의 가호", 4)).heal = 3;
            P(p, "johann_e", 0, Fx("delayed_damage", "scripture", "인도하는 빛", 8), B("bonus_block", 7, need:"censer"));
            P(p, "johann_r", 7, Fx("hot", "faith", "구원의 성역", 5, 3), B("bonus_block", 8)).heal = 5;

            // Ground balls are picked up with E before Fastball can be strengthened.
            P(p, "william_q", 0, Fx("empower_basic", "fastball", "쉐도우 볼", 4), Fx("empower_basic", "fastball", "완성된 패스트볼", 9, need:"catch", min:3), Spend("catch", "캐치볼", need:"catch", min:3));
            P(p, "william_w", 6, Fx("delayed_damage", "return_ball", "되돌아오는 공", 6), G("ball", "바닥 공", 1, 4, hit:true), S("mound", "마운드", 1, 1));
            P(p, "william_e", 0, G("catch", "캐치볼", 1, 3, need:"ball"), G("catch", "캐치볼", 1, 3, need:"ball", min:2), G("catch", "캐치볼", 1, 3, need:"ball", min:3), Spend("ball", "바닥 공"), B("bonus_block", 5, need:"mound"));
            P(p, "william_r", 20, S("ball", "바닥 공", 4, 4));

            // Fish marks bridge the reversible human/cat form and its basic attack.
            P(p, "irem_q", 8, S("fish", "생선 표식", 1, 1), B("bonus_damage", 4, need:"cat"));
            P(p, "irem_w", 8, B("bonus_block", 6, need:"cat"));
            P(p, "irem_e", 0, S("fish", "생선 표식", 1, 1), Fx("empower_basic", "cat_bell", "고양이 방울", 7, need:"cat"));
            P(p, "irem_r", 0, Toggle("cat", "고양이"), Fx("empower_basic", "fish", "생선 사냥", 8, need:"fish"), Spend("fish", "생선 표식")).exhaust = 0;

            // Vital Force is gathered independently of Eruption's dissonance.
            P(p, "eva_q", 7, G("vital", "생명력", 1, 4, hit:true), Fx("delayed_damage", "triad", "빛 구체 폭발", 5));
            P(p, "eva_w", 7, G("vital", "생명력", 1, 4, hit:true), Fx("delayed_damage", "vortex", "소용돌이 붕괴", 7));
            P(p, "eva_e", 0, G("vital", "생명력", 2, 4), S("amethyst", "자수정의 물결", 1, 1));
            P(p, "eva_r", 2, B("bonus_damage", 4, scale:"vital", cap:4), Spend("vital", "생명력"), G("dissonance", "VF 불협화음", 1, 3, hit:true), B("bonus_damage", 7, need:"dissonance", min:2), Spend("dissonance", "VF 불협화음", need:"dissonance", min:2), B("bonus_damage", 5, need:"amethyst"), Spend("amethyst", "자수정의 물결")).exhaust = 0;

            // Human cuts feed possession; the ghost grants short lifesteal-like ticks.
            Possession(p, "ian_q", 7);
            Possession(p, "ian_w", 8);
            Possession(p, "ian_e", 7);
            P(p, "ian_r", 18, S("possessed", "빙의", 3, 3), Fx("hot", "ghost", "유령의 회복", 3, 3), B("bonus_damage", 3, scale:"thrash", cap:3), Spend("thrash", "난동"));

            // Sliders can be gathered during taunt, then sustain a short cyclone.
            P(p, "eleven_q", 8, Fx("delayed_damage", "burger", "충전 버거", 5), G("slider", "미니 버거", 1, 4, hit:true));
            P(p, "eleven_w", 0, B("bonus_heal", 3, scale:"slider", cap:4), Spend("slider", "미니 버거"), Fx("counter", "fork", "집중 도발", 3, 2));
            P(p, "eleven_e", 9, G("slider", "미니 버거", 1, 4, hit:true), B("bonus_damage", 5, previous:"eleven_w"));
            P(p, "eleven_r", 12, Fx("summon", "cyclone", "칼로리 회오리", 4, 3), Fx("hot", "cyclone", "회오리 회복", 3, 3)).heal = 4;

            // Non-Gandiva hits make chakrams; Eye changes Gandiva's cooldown options.
            Eye(p, "zahir_q", 8, B("bonus_damage", 5, need:"eye"));
            P(p, "zahir_w", 2, B("bonus_damage", 3, scale:"chakram", cap:4), Spend("chakram", "차크람", 1), D("zahir_q", 7, need:"eye"), D("zahir_e", 7, need:"eye")).freeCastCount = 0;
            Eye(p, "zahir_e", 9, B("bonus_block", 4, need:"eye"));
            Eye(p, "zahir_r", 18, Fx("delayed_damage", "heaven", "하늘의 차크람", 7));

            // Red Wine/Black Tea remain reversible; the carpet is a recast install.
            P(p, "jenny_q", 7, Fx("empower_basic", "red_wine", "레드 와인", 5, need:"role", min:0, exact:true), B("bonus_block", 6, need:"role"));
            P(p, "jenny_w", 8, S("carpet", "레드 카펫", 1, 1), B("bonus_damage", 7, need:"carpet"), Spend("carpet", "레드 카펫", need:"carpet"));
            P(p, "jenny_e", 0, Toggle("role", "블랙 티 배역"), Fx("empower_basic", "persona", "페르소나", 5));
            P(p, "jenny_r", 14, Fx("delayed_damage", "stage", "시상식 무대", 13), D("jenny_e", 3));

            // Paso enhances the next sweep; two-step E and returning Duende are distinct.
            P(p, "camilo_q", 7, G("paso", "파소", 1, 2, hit:true), B("bonus_damage", 6, need:"paso", min:2), B("bonus_heal", 4, need:"paso", min:2), Spend("paso", "파소", need:"paso", min:2));
            P(p, "camilo_w", 10, D("camilo_w", 1, previous:"camilo_q"), B("bonus_block", 5, previous:"camilo_e"));
            P(p, "camilo_e", 3, G("step", "스텝", 1, 2, hit:true), B("bonus_damage", 4, need:"step", min:1), Spend("step", "스텝", need:"step", min:2));
            P(p, "camilo_r", 15, Fx("delayed_damage", "duende", "두엔데 귀환", 10), B("bonus_heal", 5, need:"step"));

            // Harpoons persist; salvage consumes them and restores readiness.
            P(p, "karla_q", 3, G("harpoon", "연결 작살", 1, 4, hit:true));
            P(p, "karla_w", 0, B("bonus_damage", 4, scale:"harpoon", cap:4), G("readiness", "장전", 1, 3, need:"harpoon"), D("karla_e", 1, need:"harpoon"), Spend("harpoon", "연결 작살"));
            P(p, "karla_e", 9, B("bonus_damage", 4, need:"harpoon"), Fx("empower_basic", "charged", "장전 사격", 3, scale:"readiness", cap:3), Spend("readiness", "장전"));
            P(p, "karla_r", 16, Fx("delayed_damage", "anchor", "구속의 사슬", 13), G("harpoon", "연결 작살", 1, 4, hit:true));

            // Suture deliberately does not create Wounded; OP forces severe wounds.
            Wound(p, "cathy_q", 8);
            Wound(p, "cathy_w", 9);
            P(p, "cathy_e", 8, B("bonus_damage", 5, need:"severe"), D("cathy_q", 1, need:"severe"));
            P(p, "cathy_r", 21, S("severe", "중상", 1, 1), Fx("bleed", "severe", "중상 출혈", 4, 3), Fx("hot", "surgery", "수술 구역", 3, 3)).heal = 4;

            // Nina attacks while commanded; theatre/recall relocate the doll.
            P(p, "chloe_q", 6, S("nina", "니나 전개", 1, 1), Fx("summon", "nina", "니나", 3, 3));
            P(p, "chloe_w", 7, Fx("summon", "threads", "인형극 실", 3, 2), B("bonus_damage", 5, need:"nina"));
            P(p, "chloe_e", 8, Fx("delayed_damage", "nina_recall", "니나 귀환", 6, need:"nina"), D("chloe_q", 1));
            P(p, "chloe_r", 12, Fx("counter", "soul_link", "생명 공유", 5, 2), Fx("summon", "nina", "연결된 니나", 6, 2, need:"nina"), D("chloe_w", 2), D("chloe_e", 2));

            // Stigma feeds Last Judgement; shield and shackles mature separately.
            Stigma(p, "chiara_q", 8, Fx("hot", "corruption", "부정의 회복", 2, 2));
            Stigma(p, "chiara_w", 0, Fx("delayed_damage", "prayer", "기도 폭발", 8));
            Stigma(p, "chiara_e", 6, Fx("delayed_damage", "mania", "집착의 속박", 7));
            P(p, "chiara_r", 15, B("bonus_damage", 4, scale:"stigma", cap:4), Spend("stigma", "낙인"), Fx("burn", "plague", "폭주", 3, 3), Fx("hot", "plague", "폭주의 회복", 3, 2)).heal = 3;

            // Shards -> collection -> sword, with barrier shards and separate Paradiso.
            P(p, "tazia_q", 3, G("shard", "유리 파편", 1, 4, hit:true), B("bonus_damage", 8, need:"sword"), Spend("sword", "유리 검"));
            P(p, "tazia_w", 7, Fx("delayed_damage", "barrier", "유리 장벽", 6), G("shard", "유리 파편", 2, 4));
            P(p, "tazia_e", 6, B("bonus_damage", 3, scale:"shard", cap:4), B("bonus_block", 2, scale:"shard", cap:4), G("collection", "파편 수집", 1, 4, need:"shard"), G("collection", "파편 수집", 1, 4, need:"shard", min:2), G("collection", "파편 수집", 1, 4, need:"shard", min:3), G("collection", "파편 수집", 1, 4, need:"shard", min:4), S("sword", "유리 검", 1, 1, need:"collection", min:3), Spend("collection", "파편 수집", need:"collection", min:3), Spend("shard", "유리 파편"));
            P(p, "tazia_r", 15, B("bonus_damage", 4, scale:"shard", cap:4), Spend("shard", "유리 파편"), Fx("delayed_damage", "paradiso", "파라디소 파괴", 9));

            // Screen duplicates cannon; a lock-on enables a later rooted hit.
            P(p, "theodore_q", 7, Fx("delayed_damage", "cannon", "충전 에너지 포", 6), B("bonus_damage", 7, need:"screen"), B("bonus_heal", 4, need:"screen"), B("bonus_damage", 5, need:"lock"), Spend("lock", "목표 고정"));
            P(p, "theodore_w", 0, S("screen", "증폭 스크린", 1, 1), Fx("empower_basic", "screen", "스크린 사격", 6));
            P(p, "theodore_e", 8, S("lock", "목표 고정", 1, 1), B("bonus_damage", 5, need:"screen"));
            P(p, "theodore_r", 19, D("theodore_q", 2), Fx("empower_basic", "overcharged", "과충전", 7)).heal = 5;

            // All three spear moves share a final-cast cadence, but finish differently.
            Spear(p, "felix_q", 8, "bonus_damage", 8);
            Spear(p, "felix_w", 8, "bonus_block", 8);
            Spear(p, "felix_e", 7, "bonus_heal", 6);
            P(p, "felix_r", 19, S("weaving", "연계 창술", 2, 3), Fx("delayed_damage", "hydra", "충전 뇌룡격", 9));

            // Flowers bloom one action later; Q detonates, W shields, R bears fruit.
            P(p, "priya_q", 3, G("flower", "사라스바티 꽃", 1, 3, need:"flower", min:0, exact:true), B("bonus_damage", 5, scale:"flower", cap:3), Spend("flower", "사라스바티 꽃", need:"flower"));
            P(p, "priya_w", 0, B("bonus_block", 4, scale:"flower", cap:3), Spend("flower", "사라스바티 꽃"), Fx("empower_basic", "portamento", "꽃 피우기", 4), G("flower", "사라스바티 꽃", 1, 3, need:"flower", min:0, exact:true));
            P(p, "priya_e", 8, Fx("delayed_damage", "harmony", "세 번째 선율", 6), G("flower", "사라스바티 꽃", 1, 3, hit:true));
            P(p, "priya_r", 16, Fx("delayed_damage", "echo", "대지의 메아리", 11), B("bonus_heal", 4, scale:"flower", cap:3), Spend("flower", "사라스바티 꽃")).heal = 4;

            // Touché is applied per move, with Q applying two and a distinct triple R.
            Touche(p, "fiora_q", 8, 2);
            Touche(p, "fiora_w", 8, 2);
            Touche(p, "fiora_e", 8, 1, D("fiora_e", 1, previous:"fiora_q"));
            P(p, "fiora_r", 18, Fx("delayed_damage", "fleche", "마지막 플레슈", 8), B("bonus_damage", 3, scale:"touche", cap:3), Spend("touche", "뚜셰"));

            // Each completed secondary technique produces Focus for The Punisher.
            Focus(p, "piolo_q", 8, Fx("delayed_damage", "chokeslam", "내려치기", 6));
            Focus(p, "piolo_w", 0, Fx("counter", "figure_eight", "튕겨내기", 5, 2));
            Focus(p, "piolo_e", 8, Fx("delayed_damage", "skyward", "올려치기", 6));
            P(p, "piolo_r", 4, B("bonus_damage", 6, need:"focus"), B("bonus_block", 5, need:"focus"), Spend("focus", "집중", 1), Fx("empower_basic", "punisher", "응징자", 5, need:"focus"));

            // Actual delayed notes, finite Overdrive and a defensive concert ending.
            P(p, "hart_q", 5, Fx("delayed_damage", "delay", "Delay 잔향", 5), B("bonus_damage", 4, previous:"hart_q"));
            P(p, "hart_w", 0, Fx("empower_basic", "overdrive", "Overdrive", 8), S("overdrive", "오버드라이브", 1, 1));
            P(p, "hart_e", 8, B("bonus_damage", 4, need:"overdrive"), Spend("overdrive", "오버드라이브"));
            P(p, "hart_r", 0, Fx("revive", "peacemaker", "Peacemaker", 12, 2), Fx("hot", "concert_end", "공연 회복", 4, 2)).heal = 4;

            // SMG and rockets change Q; Shotgun discards the active weapon form.
            P(p, "haze_q", 4,
                R("bonus_damage", null, "기관단총 점사", 4, need:"weapon_form", min:1, exact:true, need2:"ammo"),
                R("bonus_damage", null, "로켓 가속", 8, need:"weapon_form", min:2, exact:true, need2:"ammo"),
                Spend("ammo", "탄환", 1), S("weapon_form", "무기 형태", 0, 2, need:"ammo", min:1, exact:true),
                Fx("empower_basic", "merchant", "무기 교체", 4));
            P(p, "haze_w", 10, D("haze_e", 1, need:"weapon_form", min:1, exact:true), D("haze_r", 1, need:"weapon_form", min:2, exact:true), S("weapon_form", "무기 형태", 0, 2), Spend("ammo", "탄환"), Fx("empower_basic", "merchant", "무기 교체", 5));
            P(p, "haze_e", 6, S("weapon_form", "무기 형태", 1, 2), S("ammo", "탄환", 4, 4), Fx("empower_basic", "smg", "기관단총", 6));
            P(p, "haze_r", 12, S("weapon_form", "무기 형태", 2, 2), S("ammo", "탄환", 4, 4), Fx("empower_basic", "rocket", "로켓 탄환", 9)).exhaust = 0;
        }

        static SkillMechanicProfile P(Dictionary<string, SkillMechanicProfile> profiles, string id, int damage, params SkillRule[] rules)
        {
            var profile = new SkillMechanicProfile { damage=damage, hits=1, poison=0, strength=0, blockMultiplier=.7f, healMultiplier=.7f, rules=rules };
            profiles[id] = profile;
            return profile;
        }

        static SkillRule R(string op, string key, string label, int amount, int cap=3, int duration=2,
            string scale=null, string need=null, int min=1, bool exact=false, string previous=null,
            string target=null, bool hit=false, int delay=1, string need2=null, int min2=1, bool exact2=false)
        {
            return new SkillRule { op=op, key=key, label=label, amount=amount, cap=cap, duration=duration,
                scaleKey=scale, conditionKey=need, conditionAmount=min, conditionExact=exact,
                conditionPrevious=previous, targetCard=target, onHit=hit, delay=delay,
                conditionKey2=need2, conditionAmount2=min2, conditionExact2=exact2 };
        }
        static SkillRule G(string key, string label, int amount, int cap=3, string need=null, int min=1, bool exact=false, string previous=null, bool hit=false)
            => R("gain", key, label, amount, cap, need:need, min:min, exact:exact, previous:previous, hit:hit);
        static SkillRule S(string key, string label, int amount, int cap=3, string need=null, int min=1, bool exact=false, bool hit=false)
            => R("set", key, label, amount, cap, need:need, min:min, exact:exact, hit:hit);
        static SkillRule Toggle(string key, string label) => R("state_toggle", key, label, 1, 1);
        static SkillRule Spend(string key, string label, int amount=0, string need=null, int min=1, bool hit=false)
            => R("consume", key, label, amount, need:need, min:min, hit:hit);
        static SkillRule B(string op, int amount, string scale=null, int cap=3, string need=null, int min=1, bool exact=false, string previous=null)
            => R(op, null, null, amount, cap, scale:scale, need:need, min:min, exact:exact, previous:previous);
        static SkillRule Fx(string op, string key, string label, int amount, int duration=2, string need=null,
            int min=1, bool exact=false, string scale=null, int cap=3, int delay=1, bool hit=false)
            => R(op, key, label, amount, cap, duration, scale:scale, need:need, min:min, exact:exact, delay:delay, hit:hit);
        static SkillRule D(string target, int amount=1, string need=null, int min=1, bool exact=false, string previous=null, bool hit=false)
            => R("discount", null, null, amount, need:need, min:min, exact:exact, previous:previous, target:target, hit:hit);

        static void Paint(Dictionary<string, SkillMechanicProfile> p, string id, int damage)
        {
            P(p, id, damage,
                Mix(0, 2, "bonus_damage", 6), Mix(1, 1, "bonus_damage", 6),
                Mix(1, 3, "bonus_damage", 4), Mix(2, 2, "bonus_damage", 4),
                Mix(1, 3, "bonus_heal", 4), Mix(2, 2, "bonus_heal", 4),
                Mix(0, 3, "bonus_damage", 4), Mix(2, 1, "bonus_damage", 4),
                Mix(0, 3, "bonus_block", 6), Mix(2, 1, "bonus_block", 6),
                S("paint", "물감", 1, 3, need:"brush", min:0, exact:true, hit:true),
                S("paint", "물감", 2, 3, need:"brush", min:1, exact:true, hit:true),
                S("paint", "물감", 3, 3, need:"brush", min:2, exact:true, hit:true),
                Mix(0, 2, "consume", 0), Mix(0, 3, "consume", 0),
                Mix(1, 1, "consume", 0), Mix(1, 3, "consume", 0),
                Mix(2, 1, "consume", 0), Mix(2, 2, "consume", 0));
        }
        static SkillRule Mix(int brush, int paint, string op, int amount)
            => R(op, op=="consume" ? "paint" : null, "컬러믹스", amount,
                need:"brush", min:brush, exact:true, need2:"paint", min2:paint, exact2:true, hit:true);

        static void Rozzi(Dictionary<string, SkillMechanicProfile> p, string id, int damage)
        {
            P(p, id, damage, Fx("empower_basic", "dual_wield", "더블샷", 4),
                B("bonus_damage", 15, need:"semtex"), Fx("clear_effect", "semtex_fuse", "조기 기폭", 0, hit:true),
                Spend("semtex", "부착 폭탄", hit:true), D("rozzi_r", 1, need:"semtex"));
        }
        static void Dream(Dictionary<string, SkillMechanicProfile> p, string id, int damage, params SkillRule[] extra)
        {
            var rules = new List<SkillRule> { G("dream", "나비의 꿈", 1, 4),
                B("bonus_block", 8, need:"dream", min:3), Fx("summon", "dream_butterfly", "몽환 나비", 3, 2, need:"dream", min:3),
                Spend("dream", "나비의 꿈", need:"dream", min:3) };
            rules.AddRange(extra); P(p, id, damage, rules.ToArray());
        }
        static void Bike(Dictionary<string, SkillMechanicProfile> p, string id, int damage)
        {
            P(p, id, damage, G("fuel", "연료", 1, 4, need:"bike", min:0, exact:true, hit:true),
                B("bonus_damage", 5, need:"bike"), Spend("fuel", "연료", 1, need:"bike"),
                S("bike", "바이크", 0, 1, need:"fuel", min:0, exact:true));
        }
        static void Star(Dictionary<string, SkillMechanicProfile> p, string id, int damage, int conjunction)
        {
            P(p, id, damage, B("bonus_damage", 4, need:"sign", min:0, exact:true),
                B("bonus_block", 5, need:"sign", min:1, exact:true), B("bonus_heal", 4, need:"sign", min:2, exact:true),
                R(conjunction==2 ? "bonus_heal" : "bonus_damage", null, "별자리 합", conjunction==2 ? 7 : 8,
                    need:"sign", min:conjunction, exact:true, need2:"conjunction"),
                Spend("conjunction", "합"), G("sign", "별자리", 1, 2), S("sign", "별자리", 0, 2, need:"sign", min:2, exact:true));
        }
        static void Exploit(Dictionary<string, SkillMechanicProfile> p, string id, int damage, params SkillRule[] extra)
        {
            var rules = new List<SkillRule> { G("exploit", "착취", 1, 3, hit:true), B("bonus_damage", 6, need:"exploit", min:2),
                B("bonus_heal", 4, need:"exploit", min:2), Spend("exploit", "착취", need:"exploit", min:2),
                B("bonus_damage", 5, need:"reinforce"), Spend("reinforce", "경화 준비"), D("isaac_w", 1, need:"exploit", min:2) };
            rules.AddRange(extra); P(p, id, damage, rules.ToArray());
        }
        static void Chill(Dictionary<string, SkillMechanicProfile> p, string id, int damage, params SkillRule[] extra)
        {
            var rules = new List<SkillRule> { G("chill", "냉기", 1, 3, hit:true), S("frozen", "빙결", 1, 1, need:"chill", min:2, hit:true),
                Spend("chill", "냉기", need:"chill", min:2, hit:true), B("bonus_damage", 7, need:"frozen"), Spend("frozen", "빙결", need:"frozen", hit:true) };
            rules.AddRange(extra); P(p, id, damage, rules.ToArray());
        }
        static void Possession(Dictionary<string, SkillMechanicProfile> p, string id, int damage)
        {
            P(p, id, damage, G("thrash", "난동", 1, 3, hit:true), B("bonus_damage", 4, need:"possessed"), B("bonus_heal", 3, need:"possessed"), Spend("possessed", "빙의", 1));
        }
        static void Eye(Dictionary<string, SkillMechanicProfile> p, string id, int damage, params SkillRule[] extra)
        {
            var rules = new List<SkillRule> { S("eye", "사신의 눈", 1, 1), G("chakram", "차크람", 2, 4, hit:true) };
            rules.AddRange(extra); P(p, id, damage, rules.ToArray());
        }
        static void Wound(Dictionary<string, SkillMechanicProfile> p, string id, int damage)
        {
            P(p, id, damage, G("wounded", "상처", 1, 3, hit:true),
                Fx("bleed", "wound", "상처 출혈", 2, 2, hit:true), S("severe", "중상", 1, 1, need:"wounded", min:2, hit:true),
                Fx("bleed", "severe", "중상 출혈", 4, 3, need:"wounded", min:2, hit:true),
                B("bonus_block", 6, need:"wounded", min:2), Spend("wounded", "상처", need:"wounded", min:2),
                D("cathy_q", 1, need:"severe"), Fx("empower_basic", "knife", "외과 절개", 4));
        }
        static void Stigma(Dictionary<string, SkillMechanicProfile> p, string id, int damage, params SkillRule[] extra)
        {
            var rules = new List<SkillRule> { G("stigma", "낙인", 1, 4) };
            rules.AddRange(extra); P(p, id, damage, rules.ToArray());
        }
        static void Spear(Dictionary<string, SkillMechanicProfile> p, string id, int damage, string finish, int amount)
        {
            P(p, id, damage, G("weaving", "연계 창술", 1, 3, hit:true), B(finish, amount, need:"weaving", min:2),
                Spend("weaving", "연계 창술", need:"weaving", min:2), Fx("empower_basic", "spear", "연계 평타", 4));
        }
        static void Touche(Dictionary<string, SkillMechanicProfile> p, string id, int damage, int stacks, params SkillRule[] extra)
        {
            var rules = new List<SkillRule> { G("touche", "뚜셰", stacks, 3, hit:true), B("bonus_damage", 6, need:"touche", min:3),
                B("bonus_heal", 4, need:"touche", min:3), Spend("touche", "뚜셰", need:"touche", min:3) };
            rules.AddRange(extra); P(p, id, damage, rules.ToArray());
        }
        static void Focus(Dictionary<string, SkillMechanicProfile> p, string id, int damage, params SkillRule[] extra)
        {
            var rules = new List<SkillRule> { G("focus", "집중", 1, 3) };
            rules.AddRange(extra); P(p, id, damage, rules.ToArray());
        }
    }
}
