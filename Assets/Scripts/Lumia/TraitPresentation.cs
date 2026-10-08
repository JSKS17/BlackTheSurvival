using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    public static class TraitPresentation
    {
        public static string Describe(TraitMechanicProfile profile)
        {
            if (profile == null) return "";
            var lines = EffectLines(profile, false);
            var rules = profile.rules ?? new TraitRule[0];
            if (rules.Any(r => new[] { "bonus_damage", "bonus_block", "bonus_heal" }.Contains(r.op)))
                lines.Add("패시브·룬·장비가 카드 한 번에 더하는 피해와 방어도는 각각 합계 10, 추가 회복은 합계 12까지 적용됩니다.");
            if (rules.Any(r => r.op == "discount" || r.op == "discount_last")) lines.Add("할인은 다음 대상 카드 한 번에 적용되며 중첩되지 않습니다. 같은 카드가 주는 할인은 자신의 한 턴에 한 번만 적용됩니다.");
            return string.Join("\n", DescriptionSummary.CleanLines(lines));
        }

        public static string Summary(TraitMechanicProfile profile)
        {
            return profile == null ? "" : string.Join("\n", DescriptionSummary.CleanLines(EffectLines(profile, true)));
        }

        sealed class EffectGroup
        {
            public string key, when, limit;
            public List<string> effects = new List<string>();
        }

        static List<string> EffectLines(TraitMechanicProfile profile, bool compact)
        {
            var rules = (profile.rules ?? new TraitRule[0]).Where(r => r != null).ToArray();
            var labels = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var r in rules.OrderBy(r => r.op == "gain" || r.op == "set" || r.op == "damage_resource" ? 0 : 1))
                if (!string.IsNullOrEmpty(r.key) && !string.IsNullOrEmpty(r.label) && !labels.ContainsKey(r.key)) labels[r.key] = r.label;
            Func<string, string> label = key => !string.IsNullOrEmpty(key) && labels.ContainsKey(key) ? labels[key] : string.IsNullOrEmpty(key) ? "자원" : key;
            var groups = new List<EffectGroup>();
            foreach (var r in rules)
            {
                var conditions = new List<string>();
                if (!string.IsNullOrEmpty(r.conditionKey)) conditions.Add(Condition(label(r.conditionKey), r.conditionAmount, r.conditionExact));
                if (!string.IsNullOrEmpty(r.conditionKey2)) conditions.Add(Condition(label(r.conditionKey2), r.conditionAmount2, r.conditionExact2));
                if (!string.IsNullOrEmpty(r.conditionPrevious)) conditions.Add("직전에 사용한 기술이 " + (GameDatabase.Card(r.conditionPrevious)?.name ?? r.conditionPrevious) + "이면");
                if (!string.IsNullOrEmpty(r.conditionCategory)) conditions.Add(Category(r.conditionCategory) + "일 때");
                if (r.hpBelowPercent > 0) conditions.Add("체력이 최대 체력의 " + SkillMechanics.Clamp(r.hpBelowPercent, 1, 100) + "% 이하이면");
                if(r.hpBelowDenominator>0)conditions.Add("체력이 최대 체력의 "+r.hpBelowNumerator+"/"+r.hpBelowDenominator+" 이하이면");
                if (r.onHit && !IsHitTrigger(r.trigger)) conditions.Add("그 공격이 적중했으면");
                string effect = Effect(r, label);
                if (string.IsNullOrEmpty(effect)) continue;
                string timing = Trigger(r.trigger);
                int every = SkillMechanics.Clamp(r.every, 1, 8);
                if (every > 1) timing = Event(r.trigger) + " " + every + "회마다";
                if (r.op == "shop_discount") timing = "";
                if (compact) timing = timing.Replace("어느 실험체의 기술 카드든", "기술을")
                    .Replace("어느 실험체의 기술 카드", "기술 카드").Replace("어느 실험체의 기술이든", "기술이").Replace("어느 실험체의 R 카드든", "R 카드를")
                    .Replace("사용을 마친 후", "사용 후").Replace("자신의 ", "").Replace("전투가 시작될 때", "전투 시작 시")
                    .Replace("체력 피해를 받아 체력이 낮아질 때", "피해를 받은 후").Replace("체력 피해를 받을 때", "피해를 받을 때");
                string when = timing + (conditions.Count == 0 ? "" : (timing.Length == 0 ? "" : ", ") + string.Join(", ", conditions));
                string limit = Limits(r, compact);
                string key = when + "|" + limit;
                var group = groups.FirstOrDefault(g => g.key == key);
                if (group == null) { group = new EffectGroup { key=key, when=when, limit=limit }; groups.Add(group); }
                if (!group.effects.Contains(effect)) group.effects.Add(effect);
            }
            var lines = new List<string>();
            foreach (var group in groups)
                lines.Add((group.when.Length == 0 ? "" : group.when + ": ") + string.Join(". ", group.effects) + (compact && group.limit.Length > 0 ? group.limit : "." + group.limit));
            // This equipment-dependent Alex rule is resolved by the engine outside the trigger table.
            if ((profile.summary ?? "").Contains("장착한 무기마다"))
                lines.Add("장착한 무기마다 해당 무기군과 다른 D 스킬 카드 1장을 추가로 받습니다. 장비·패시브를 바꾸면 자동으로 갱신됩니다.");
            if (rules.Length == 0 && !string.IsNullOrWhiteSpace(profile.summary)) lines.Add(profile.summary);
            return lines;
        }

        private static string Condition(string label, int amount, bool exact)
        {
            return CardPresentation.WithParticle(label, "가", "이") + " " + amount + (exact ? "이면" : " 이상이면");
        }
        private static string Category(string value)
        {
            switch (value) { case "basic": return "기본 카드(기본 공격·경계)"; case "skill": return "실험체 기술 카드"; case "weapon": return "무기 기술 카드"; case "tactical": return "전술 기술 카드"; default: return value + " 카드"; }
        }
        private static bool IsHitTrigger(string trigger)
        {
            return new[] { "hit", "skill_hit", "install_hit", "wildlife_hit", "incoming_hit", "control" }.Contains(trigger);
        }
        private static string Event(string trigger)
        {
            switch (trigger)
            {
                case "before_basic": case "after_basic": return "기본 공격 사용";
                case "before_skill": case "after_skill": return "기술 카드 사용";
                case "after_movement": return "이동 기술 사용";
                case "before_ultimate": case "after_ultimate": return "R 카드 사용";
                case "before_attack": return "공격 카드 사용";
                case "hit": return "공격 적중"; case "skill_hit": return "기술 적중";
                case "install_hit": return "설치물·소환물 적중"; case "wildlife_hit": return "야생동물 공격 적중";
                case "evade": return "공격 회피"; case "damaged": return "체력 피해를 받은";
                case "incoming_hit": return "공격에 맞은"; case "before_incoming": return "적에게 공격받은";
                case "block": return "방어도를 얻은"; case "heal": return "체력을 회복한";
                case "turn_start": return "자신의 턴 시작"; case "turn_end": return "자신의 턴 종료";
                case "battle_start": return "전투 시작"; case "battle_win": return "전투 승리";
                case "control": return "군중 제어 부여"; case "block_break": return "방어도 파괴";
                case "wildlife_before_attack": case "wildlife_before_card": return "야생동물 공격";
                default: return "카드 사용";
            }
        }
        private static string Trigger(string trigger)
        {
            switch (trigger)
            {
                case "before_basic": return "기본 공격을 사용할 때"; case "after_basic": return "기본 공격 사용을 마친 후";
                case "before_skill": return "어느 실험체의 기술 카드든 사용할 때"; case "after_skill": return "어느 실험체의 기술 카드든 사용을 마친 후";
                case "before_ultimate": return "어느 실험체의 R 카드든 사용할 때"; case "after_ultimate": return "어느 실험체의 R 카드든 사용을 마친 후";
                case "before_attack": return "공격 카드를 사용할 때";
                case "after_movement": return "이동 기술 사용 후";
                case "hit": return "공격이 적중할 때"; case "skill_hit": return "어느 실험체의 기술이든 적중할 때";
                case "install_hit": return "설치물이나 소환물이 적을 맞힐 때";
                case "evade": return "공격을 회피할 때"; case "damaged": return "체력 피해를 받을 때";
                case "incoming_hit": return "적의 공격에 맞을 때"; case "before_incoming": return "적의 공격을 받기 직전에";
                case "block": return "방어도를 얻을 때"; case "heal": return "체력을 회복할 때";
                case "turn_start": return "자신의 턴이 시작될 때"; case "turn_end": return "자신의 턴이 끝날 때";
                case "battle_start": return "전투가 시작될 때"; case "battle_win": return "전투에서 승리할 때";
                case "control": return "적에게 군중 제어 상태를 부여할 때"; case "block_break": return "방어도가 깨질 때";
                case "low_health": return "체력 피해를 받아 체력이 낮아질 때";
                case "wildlife_before_attack": case "wildlife_before_card": return "야생동물을 공격할 때";
                case "wildlife_hit": return "야생동물에게 공격이 적중할 때"; case "always": return "탐사 중";
                case "after_card": return "카드 사용을 마친 후"; default: return "카드를 사용할 때";
            }
        }
        private static int Cap(string op)
        {
            switch (op)
            {
                case "bonus_damage": case "bonus_block": case "block": return 5;
                case "bonus_heal": case "heal": return 6; case "reduce_damage": return 3;
                case "energy": case "draw": case "energy_buff": case "weak": case "vulnerable": case "strength": return 1;
                case "damage_buff": case "poison": return 2;
                case "evasion": case "evasion_buff": case "exposure": case "bleed": case "burn": case "summon": case "hot": case "guard": return 10;
                case "heal_reduction": return 20; case "delayed_damage": return 24;
                case "counter": return 8; case "revive": return 40; case "discount": case "discount_last": return 7; case "empower_basic": return 16;
                default: return 8;
            }
        }
        private static string Amount(TraitRule r, Func<string, string> label)
        {
            int amount = Math.Max(0, r.amount), maximum = Math.Min(Cap(r.op), amount * SkillMechanics.Clamp(r.cap, 1, 8));
            return string.IsNullOrEmpty(r.scaleKey) ? Math.Min(Cap(r.op), amount).ToString() : label(r.scaleKey) + " × " + amount + "(최대 " + maximum + ")";
        }
        private static string Effect(TraitRule r, Func<string, string> label)
        {
            string n = Amount(r, label), name = string.IsNullOrEmpty(r.label) ? label(r.key) : r.label;
            string obj = CardPresentation.WithParticle(name, "를", "을"); int turns = SkillMechanics.Clamp(r.duration, 1, 3);
            switch (r.op)
            {
                case "gain": return obj + " " + Math.Max(0, r.amount) + " 증가시킵니다(최대 " + SkillMechanics.Clamp(r.cap, 1, 8) + ")";
                case "set": return obj + " " + SkillMechanics.Clamp(r.amount, 0, SkillMechanics.Clamp(r.cap, 1, 8)) + "로 만듭니다";
                case "consume": return obj + (r.amount <= 0 ? " 모두" : " 최대 " + r.amount) + " 소모합니다";
                case "bonus_damage": return "첫 적중에 추가 피해를 " + n + "만큼 줍니다";
                case "bonus_block": case "block": return "방어도를 " + n + "만큼 얻습니다";
                case "bonus_heal": case "heal": return "체력을 " + n + "만큼 회복합니다";
                case "energy": return "사용 가능한 코스트를 " + n + " 회복합니다"; case "draw": return "카드를 " + n + "장 뽑습니다";
                case "damage_buff": return turns + "턴 동안 모든 공격 카드의 첫 적중 피해를 " + n + " 늘립니다";
                case "energy_buff": return turns + "턴 동안 최대 코스트를 " + n + " 늘리고 현재 사용 가능한 코스트도 증가분만큼 회복합니다";
                case "evasion_buff": return turns + "턴 동안 회피율을 " + n + "% 높입니다";
                case "evasion": return "2턴 동안 회피율을 최소 " + n + "%로 높입니다";
                case "reduce_damage": return "그 공격의 첫 적중 피해를 " + n + " 줄입니다";
                case "poison": return "적에게 중독을 " + n + " 부여합니다"; case "strength": return "공격력을 " + n + " 높입니다";
                case "weak": return "적에게 약화를 " + n + "턴 부여합니다"; case "vulnerable": return "적에게 취약을 " + n + "턴 부여합니다";
                case "status":return "적에게 "+turns+"턴 동안 "+CardPresentation.WithParticle(StatusMechanics.Name(r.key),"를","을")+" 부여합니다";
                case "cleanse": return "자신의 중독과 군중 제어 등 해로운 상태를 해제합니다";
                case "heal_reduction": return SkillMechanics.Clamp(r.duration, 1, 2) + "턴 동안 적의 치유를 " + n + "% 줄입니다";
                case "exposure": return SkillMechanics.Clamp(r.duration, 1, 2) + "턴 동안 적이 받는 공격 피해를 " + n + "% 늘립니다";
                case "shop_discount": return "키오스크 구매 가격을 10% 줄입니다(중첩되지 않음)";
                case "damage_resource": return "실제로 준 피해의 " + SkillMechanics.Clamp(r.amount, 0, 25) + "%를 " + name + "에 축적합니다(최대 " + SkillMechanics.Clamp(r.cap, 1, 12) + ")";
                case "execute": return "적의 체력과 방어도의 합이 " + label(r.conditionKey ?? r.key) + " 이하이면 적을 처형합니다";
                case "discount": return "다음 " + (GameDatabase.Card(r.targetCard)?.name ?? r.targetCard) + "의 코스트를 " + n + " 줄입니다(1회)";
                case "discount_last": return "마지막으로 사용한 Q·W·E 카드의 다음 코스트를 " + n + " 줄입니다(1회)";
                case "bleed": case "burn": return name + " 효과로 자신의 턴 종료마다 방어도를 무시하고 " + n + "의 피해를 " + turns + "회 줍니다";
                case "summon": return name + " 효과로 자신의 턴 종료마다 " + n + "의 피해를 " + turns + "회 줍니다";
                case "hot": return name + " 효과로 자신의 턴 종료마다 체력을 " + n + "씩 " + turns + "회 회복합니다";
                case "guard": return name + " 효과로 자신의 턴 종료마다 방어도를 " + n + "씩 " + turns + "회 얻습니다";
                case "delayed_damage": return obj + " 설치하여 자신의 턴 종료 " + SkillMechanics.Clamp(r.delay, 1, 3) + "회 후 " + n + "의 피해를 줍니다";
                case "counter": return turns + "턴 동안 적의 공격이 자신에게 적중하면 " + n + "의 피해로 반격합니다(자신의 턴마다 1회)";
                case "empower_basic": return "다음 기본 공격 한 번을 강화하여 첫 적중에 " + n + "의 추가 피해를 줍니다";
                case "revive": return (r.persistent ? "이 전투에서 사용될 때까지" : turns + "턴 동안") + " 치명적인 피해를 한 번 막고 체력을 " + n + " 회복합니다";
                case "clear_effect": return name + "의 남은 지속 효과를 회수합니다"; default: return "";
            }
        }
        private static string Limits(TraitRule r, bool compact)
        {
            var limits = new List<string>();
            if (r.cooldown > 0) limits.Add(compact ? "쿨다운 " + SkillMechanics.Clamp(r.cooldown, 0, 6) + "턴" : "발동 후 자신의 턴 기준 " + SkillMechanics.Clamp(r.cooldown, 0, 6) + "턴의 쿨다운이 적용됩니다");
            if (r.maxPerTurn > 0) limits.Add(compact ? "턴당 " + r.maxPerTurn + "회" : "자신의 한 턴에 최대 " + r.maxPerTurn + "회 발동합니다");
            if (r.maxPerBattle > 0) limits.Add(compact ? "전투당 " + r.maxPerBattle + "회" : "전투마다 최대 " + r.maxPerBattle + "회 발동합니다");
            if (r.persistent && r.op != "revive") limits.Add((new[] { "gain", "set", "consume", "damage_resource" }.Contains(r.op) ? "이 자원과 누적 횟수는" : "이 효과의 누적 횟수는") + " 다음 전투에도 유지됩니다");
            return limits.Count == 0 ? "" : compact ? " (" + string.Join(" · ", limits) + ")." : " " + string.Join(". ", limits) + ".";
        }
    }
}
