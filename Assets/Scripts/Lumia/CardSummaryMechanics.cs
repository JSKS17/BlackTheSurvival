using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    /// <summary>Complete compact sentences derived from the card's actual rules, never a ranked subset.</summary>
    public static class CardSummaryMechanics
    {
        static readonly HashSet<string> BasicOps = new HashSet<string>(StringComparer.Ordinal) {
            "gain", "set", "consume", "state_on", "state_off", "state_toggle", "bonus_damage", "bonus_block", "bonus_heal",
            "discount", "bleed", "burn", "summon", "hot", "guard", "delayed_damage", "counter", "break_block", "empower_basic", "revive", "clear_effect"
        };

        public static bool Supports(string operation) => BasicOps.Contains(operation ?? "");
        static string Id(CardDef card) => CardInstances.BaseId(card?.id) ?? "";
        static string Particle(string value) => value.EndsWith("R", StringComparison.Ordinal) || value.EndsWith("F", StringComparison.Ordinal)
            ? value + "을" : CardPresentation.WithParticle(value, "를", "을");
        static string CardKey(string reference) => GameDatabase.Card(reference)?.key ?? reference ?? "기술";
        static string Resource(CardDef card, string key) => SkillMechanics.ResourceName(card, key ?? "자원");

        // All clauses keep their activation identity. A later hit, an earlier setup hit,
        // and a direct on-hit rule must not be coalesced merely because their prose is short.
        public static List<ConditionEffect> Effects(CardDef card, int damage = -1, int block = -1, int heal = -1)
        {
            var result = new List<ConditionEffect>();
            if (card == null) return result;
            if (WildlifeMechanics.IsAnimalSkill(card.id))
            {
                Add(result, "wildlife", "", card.id == WildlifeMechanics.BoarCharge
                    ? "2턴마다 기본 공격보다 피해를 4 더 주며 적중 시 넉백을 1턴 부여합니다. 회피·강한 제어·속박으로 돌진이 실패하면 다음 내 턴까지 좌절하여 받는 공격 피해가 50% 증가하고 둔화됩니다"
                    : "3턴마다 기본 공격보다 피해를 5 더 주며 적중 시 기절을 1턴 부여합니다. 강한 제어로 중단할 수 있습니다");
                return result;
            }
            int d = damage < 0 ? card.damage : damage, b = block < 0 ? card.block : block, h = heal < 0 ? card.heal : heal;
            bool target = AdvancedCardMechanics.HasTargetChoice(card), x = AdvancedCardMechanics.IsX(card);
            if (target)
                Add(result, "advanced:target", "", AdvancedSummary(card));
            else
            {
                if (!x && d > 0) Add(result, "cast|False||", "", Particle("피해 " + d) + (card.hits > 1 ? " " + card.hits + "회" : "") + " 줍니다");
                if (b > 0) Add(result, "cast|False||", "", Particle("방어도 " + b) + " 얻습니다");
                if (h > 0) Add(result, "cast|False||", "", Particle("체력 " + h) + " 회복합니다");
                if (card.draw > 0) Add(result, "cast|False||", "", "카드 " + card.draw + "장을 뽑습니다");
                if (card.energy > 0) Add(result, "cast|False||", "", "이번 턴 코스트를 " + card.energy + " 회복합니다");
                if (card.evasion > 0) Add(result, "cast|False||", "", card.duration + "턴간 회피율을 " + card.evasion + "% 높입니다");
                if (card.strength > 0) Add(result, "cast|False||", "", "이번 전투의 힘을 " + card.strength + " 높입니다");
                if (card.poison > 0) Add(result, "cast|False||", "", "중독을 " + card.poison + " 부여합니다");
                if (card.weak > 0) Add(result, "cast|False||", "", "약화를 " + card.weak + "턴 부여합니다");
                if (card.vulnerable > 0) Add(result, "cast|False||", "", "취약을 " + card.vulnerable + "턴 부여합니다");
            }
            if (AccuracyMechanics.GuaranteedHit(card) && (!target || !AdvancedSummary(card).Contains("필중"))) Add(result, "accuracy", "", (target ? "적 대상 공격은 " : "이 공격은 ") + "필중합니다");
            else if (Id(card) == "silvia_e") Add(result, "accuracy", "바이크 상태일 때", "필중합니다");
            else if (Id(card) == "garnet_r") Add(result, "accuracy", "철처녀 표식이 남은 적에게 이번 턴 무료 재사용할 때", "필중합니다");

            foreach (var rule in card.mechanics?.rules ?? new SkillRule[0])
            {
                if (rule == null) continue;
                string effect = RuleText(card, rule);
                if (effect.Length == 0) continue;
                string condition = Condition(card, rule, rule.onHit);
                if (target) condition = "적을 선택하고 " + (condition.Length > 0 ? condition : "사용했을 때");
                Add(result, DescriptionSummary.ConditionKey(rule), condition, effect);
            }
            if (!target)
                foreach (var rule in card.statuses ?? new CardStatusRule[0]) AddStatus(result, card, rule);
            else
                foreach (var rule in card.statuses ?? new CardStatusRule[0])
                {
                    // Choice metadata already names the mutually exclusive control.
                    // Keep nonstandard conditional controls, which metadata cannot summarize.
                    if (rule == null || string.IsNullOrEmpty(rule.conditionKey) && string.IsNullOrEmpty(rule.conditionKey2)
                        && string.IsNullOrEmpty(rule.conditionPrevious) && rule.timing == "cast") continue;
                    AddStatus(result, card, rule, "적을 선택하고 ");
                }
            if (card.freeCastCount > 0 && card.freeCastTargets != null && card.freeCastTargets.Length > 0)
            {
                var free = FreeCastEffect(card);
                if (target) free.condition = "적을 선택하고 " + (free.condition.Length > 0 ? free.condition : "사용했을 때");
                result.Add(free);
            }
            if (Id(card) == "garnet_r")
                Add(result, "garnet:execution", "이번 턴 무료 재사용으로 철처녀 표식이 있고 사용 전 체력 " + FreeCastMechanics.GarnetExecutionPercent + "% 이하인 적을 적중했을 때",
                    "방어도를 무시하고 처형합니다");
            string advanced = target ? "" : AdvancedSummary(card);
            if (advanced.Length > 0) Add(result, "advanced:" + AdvancedCardMechanics.Kind(card), "", advanced);
            string companion = CompanionMechanics.CardSummary(card);
            if (companion.Length > 0) Add(result, "companion", "", companion);
            if (card.exhaust)
                Add(result, "exhaust", "", Id(card) == "garnet_r"
                    ? "재사용·첫 공격 실패·턴 종료 시 소멸합니다" : "사용 후 이번 전투에서 소멸합니다");
            if (Id(card) == "basic_attack") Add(result, "basic:critical", "", "장비 공격력이 적용되며 기본 치명타 피해는 150%입니다");
            if (result.Count == 0) Add(result, "none", "", "추가 효과 없이 행동합니다");
            return result;
        }

        public static string Text(CardDef card, int damage = -1, int block = -1, int heal = -1)
        {
            if (Id(card) == "nia_r" || Id(card) == "nia_w")
            {
                string niaIdentity = ComplexIdentity(card, damage);
                if (niaIdentity.Length > 0 && Fits(niaIdentity)) return niaIdentity;
            }
            string complete = Render(Effects(card, damage, block, heal));
            if (Fits(complete)) return complete;
            string compact = Render(CompactEffects(card, damage, block, heal));
            if (Fits(compact)) return compact;
            string dense = Render(CompactEffects(card, damage, block, heal, true));
            if (Fits(dense)) return dense;
            string identity = ComplexIdentity(card, damage);
            return identity.Length > 0 ? identity : dense;
        }

        static bool Fits(string text) => DescriptionSummary.PreviewWidth(text) <= DescriptionSummary.CardPreviewMaxWidth
            && DescriptionSummary.PreviewLines(text) <= DescriptionSummary.CardPreviewMaxLines;

        static string Render(IEnumerable<ConditionEffect> effects) => string.Join(" ", DescriptionSummary.GroupEffects(effects));

        // Complex skills often encode an exhaustive table (paint colors, combo stages,
        // forms) as many additive rules. A preview preserves each behavior family and
        // its eligibility rather than spelling out every coefficient and table row.
        public static List<ConditionEffect> CompactEffects(CardDef card, int damage = -1, int block = -1, int heal = -1, bool dense = false)
        {
            if (card == null) return new List<ConditionEffect>();
            if (WildlifeMechanics.IsAnimalSkill(card.id)) return Effects(card, damage, block, heal);
            var result = new List<ConditionEffect>();
            int d = damage < 0 ? card.damage : damage, b = block < 0 ? card.block : block, h = heal < 0 ? card.heal : heal;
            bool target = AdvancedCardMechanics.HasTargetChoice(card), x = AdvancedCardMechanics.IsX(card);
            if (target) Add(result, "advanced", "", AdvancedSummary(card));
            else
            {
                if (dense)
                {
                    string[] actions = new[] { !x && d > 0 ? "공격" : "", b > 0 ? "방어" : "", h > 0 ? "회복" : "", card.evasion > 0 ? "회피" : "" }.Where(s => s.Length > 0).ToArray();
                    if (actions.Length > 0) Add(result, "cast", "", string.Join("·", actions) + "합니다");
                }
                if (!dense && !x && d > 0) Add(result, "cast", "", Particle("피해 " + d) + (card.hits > 1 ? " " + card.hits + "회" : "") + " 줍니다");
                if (!dense && b > 0) Add(result, "cast", "", Particle("방어도 " + b) + " 얻습니다");
                if (!dense && h > 0) Add(result, "cast", "", Particle("체력 " + h) + " 회복합니다");
                if (card.draw > 0) Add(result, "cast", "", "카드 " + card.draw + "장을 뽑습니다");
                if (card.energy > 0) Add(result, "cast", "", "코스트를 " + card.energy + " 회복합니다");
                if (!dense && card.evasion > 0) Add(result, "cast", "", card.duration + "턴간 회피율을 높입니다");
                if (card.strength > 0) Add(result, "cast", "", "이번 전투의 힘을 높입니다");
            }
            var rules = (card.mechanics?.rules ?? new SkillRule[0]).Where(r => r != null).ToArray();
            var gains = rules.Where(r => r.op == "gain" || r.op == "set").ToArray();
            foreach (var group in gains.GroupBy(r => r.onHit))
            {
                var selected = group.ToArray(); string[] keys = selected.Select(r => r.key).Distinct().ToArray();
                bool physical = keys.All(key => CardPresentation.IsFieldResource(card, key));
                bool singleSet = keys.Length == 1 && selected.All(r => r.op == "set");
                string label = !singleSet && dense || keys.Length > 1 ? physical ? "설치물" : "자원" : Resource(card, keys[0]);
                string operation = physical ? "남깁니다" : selected.All(r => r.op == "gain") ? "쌓습니다"
                    : singleSet && selected.Select(r => r.amount).Distinct().Count() == 1 ? selected[0].amount + "로 만듭니다"
                    : selected.All(r => r.op == "set") ? "바꿉니다" : "쌓거나 바꿉니다";
                AddCompact(result, card, selected, Particle(label) + " " + operation, terse: dense && !singleSet);
            }
            var modifiers = rules.Where(r => r.op == "bonus_damage" || r.op == "bonus_block" || r.op == "bonus_heal").ToArray();
            var spends = rules.Where(r => r.op == "consume").ToArray();
            // A resource payment and a bonus are independent source operations.
            // Even matching guards do not establish that one causes the other.
            // Dense previews summarize tables as tables, without inventing a cost.
            var conditionalTable = dense ? modifiers.Where(r => HasEligibility(r) || !string.IsNullOrEmpty(r.scaleKey))
                .Concat(spends.Where(HasEligibility)).ToArray() : new SkillRule[0];
            if (conditionalTable.Length > 0)
            {
                string outcomes = string.Join("·", new[] {
                    conditionalTable.Any(r => r.op == "bonus_damage") ? "피해" : "",
                    conditionalTable.Any(r => r.op == "bonus_block") ? "방어" : "",
                    conditionalTable.Any(r => r.op == "bonus_heal") ? "회복" : ""
                }.Where(value => value.Length > 0));
                bool spend = conditionalTable.Any(r => r.op == "consume");
                string effect = !spend ? Particle(outcomes) + " 강화합니다" : outcomes.Length == 0 ? "자원을 소모합니다"
                    : outcomes + " 강화·자원 소모를 적용합니다";
                Add(result, "conditional:table", "조건별로", effect);
            }
            foreach (var modifierGroup in modifiers.Where(r => !conditionalTable.Contains(r)).GroupBy(r => DescriptionSummary.ConditionKey(r)))
            {
                var selected = modifierGroup.ToArray();
                string outcomes = string.Join("·", new[] { selected.Any(r => r.op == "bonus_damage") ? "피해" : "", selected.Any(r => r.op == "bonus_block") ? "방어" : "", selected.Any(r => r.op == "bonus_heal") ? "회복" : "" }.Where(value => value.Length > 0));
                AddCompact(result, card, selected, Particle(outcomes) + " 강화합니다", terse: dense);
            }
            foreach (var group in spends.Where(r => !conditionalTable.Contains(r)).GroupBy(r => DescriptionSummary.ConditionKey(r)))
            {
                var selected = group.ToArray();
                AddCompact(result, card, selected, (!dense && selected.Select(r => r.key).Distinct().Count() == 1 ? Particle(Resource(card, selected[0].key)) : "자원을") + " 소모합니다", terse: dense);
            }
            var states = rules.Where(r => r.op.StartsWith("state_", StringComparison.Ordinal)).ToArray();
            foreach (var group in states.GroupBy(r => r.key))
            {
                var selected = group.ToArray(); string key = selected[0].key;
                string name = SkillMechanics.StateName(card, key);
                var enter = selected.FirstOrDefault(r => r.op == "state_on");
                var exit = selected.FirstOrDefault(r => r.op == "state_off");
                bool toggle = selected.Any(r => r.op == "state_toggle") || enter != null && exit != null
                    && enter.conditionKey == key && enter.conditionExact && enter.conditionAmount == 0
                    && exit.conditionKey == key && exit.conditionAmount == 1 && enter.onHit == exit.onHit
                    && enter.conditionKey2 == exit.conditionKey2 && enter.conditionAmount2 == exit.conditionAmount2 && enter.conditionExact2 == exit.conditionExact2
                    && enter.conditionPrevious == exit.conditionPrevious;
                if (toggle)
                    Add(result, "form:" + key, "", Id(card).StartsWith("debi_marlene_", StringComparison.Ordinal)
                        ? "레드·블루 자세를 교대합니다" : name + "로 진입하거나 해제합니다");
                else if (enter != null && exit != null && DescriptionSummary.ConditionKey(enter) == DescriptionSummary.ConditionKey(exit))
                {
                    bool endsActive = Array.IndexOf(rules, exit) < Array.IndexOf(rules, enter);
                    string effect = endsActive ? SkillMechanics.StateEnterText(card, key).Replace("부여합니다", "다시 부여합니다") : SkillMechanics.StateExitText(card, key);
                    AddCompact(result, card, selected, effect);
                }
                else
                    foreach (var operation in selected.GroupBy(r => r.op))
                        AddCompact(result, card, operation, operation.Key == "state_on" ? SkillMechanics.StateEnterText(card, key) : SkillMechanics.StateExitText(card, key));
            }
            foreach (var group in rules.Where(r => r.op == "discount").GroupBy(r => r.onHit))
            {
                var selected = group.ToArray();
                string targets = string.Join("·", selected.Select(r => CardKey(r.targetCard)).Distinct());
                AddCompact(result, card, selected, "다음 " + targets + (targets.EndsWith("R", StringComparison.Ordinal) ? "을" : "를") + " 할인합니다" + (dense ? "" : "(턴당 1회)"), terse: dense);
            }
            foreach (var group in rules.Where(r => r.op == "summon" || r.op == "bleed" || r.op == "burn").GroupBy(r => r.op))
            {
                var selected = group.ToArray(); string kind = group.Key == "summon" ? "소환" : group.Key == "bleed" ? "출혈" : "화상";
                AddCompact(result, card, selected, "턴 끝에 " + kind + " 공격을 " + selected.Max(r => SkillMechanics.Clamp(r.duration, 1, 3)) + "회 남깁니다", terse: dense);
            }
            var delays = rules.Where(r => r.op == "delayed_damage").ToArray();
            if (delays.Length > 0)
                AddCompact(result, card, delays, delays.Select(r => r.delay).Distinct().Count() == 1
                    ? "턴 끝 " + SkillMechanics.Clamp(delays[0].delay, 1, 3) + "회 뒤 설치 공격합니다" : "여러 턴 뒤 설치 공격합니다", terse: dense);
            foreach (var group in rules.Where(r => r.op == "hot" || r.op == "guard").GroupBy(r => r.op))
            {
                var selected = group.ToArray();
                AddCompact(result, card, selected, "턴 끝에 " + selected.Max(r => SkillMechanics.Clamp(r.duration, 1, 3)) + "회 " + (group.Key == "hot" ? "회복" : "방어") + " 효과를 얻습니다", terse: dense);
            }
            foreach (string operation in new[] { "counter", "empower_basic", "revive", "clear_effect", "break_block" })
            {
                var selected = rules.Where(r => r.op == operation).ToArray(); if (selected.Length == 0) continue;
                string effect = operation == "counter" ? selected.Max(r => SkillMechanics.Clamp(r.duration, 1, 3)) + "턴 반격합니다" + (selected.All(r => SkillMechanics.CounterEveryHit(card.id, r.key)) ? "(횟수 제한 없음)" : "(턴당 1회)")
                    : operation == "empower_basic" ? "다음 기본 공격을 강화합니다(1회)"
                    : operation == "revive" ? selected.Max(r => SkillMechanics.Clamp(r.duration, 1, 3)) + "턴간 치명상 1회 방어·회복 효과를 얻습니다"
                    : operation == "clear_effect" ? "설치 효과를 회수합니다" : "피해 전에 적의 방어도를 모두 파괴합니다";
                AddCompact(result, card, selected, effect, terse: dense);
            }
            foreach (var group in (card.statuses ?? new CardStatusRule[0]).Where(r => r != null).GroupBy(r => (r.timing ?? "cast") + "|" + r.onHit))
            {
                var selected = group.ToArray();
                if (target && selected.All(r => string.IsNullOrEmpty(r.conditionKey) && string.IsNullOrEmpty(r.conditionKey2) && string.IsNullOrEmpty(r.conditionPrevious) && r.timing == "cast")) continue;
                string names = string.Join("·", selected.Select(r => StatusMechanics.Name(r.key)).Distinct());
                string timing = selected[0].timing == "next_basic" ? "다음 기본 공격 적중 시 " : selected[0].timing != "cast" ? "설치 적중 시 " : "";
                string duration = !dense && selected.Select(r => r.duration).Distinct().Count() == 1 ? " " + selected[0].duration + "턴" : "";
                AddCompact(result, card, selected, timing + Particle(names) + duration + " 부여합니다", target ? "적을 선택하고 " : "", dense);
            }
            if (!target)
            {
                string[] legacy = new[] { card.poison > 0 ? "중독" : "", card.weak > 0 ? "약화" : "", card.vulnerable > 0 ? "취약" : "" }.Where(s => s.Length > 0).ToArray();
                if (legacy.Length > 0) Add(result, "cast", "", Particle(string.Join("·", legacy)) + " 부여합니다");
            }
            if (card.freeCastCount > 0 && card.freeCastTargets?.Length > 0)
            {
                var free = FreeCastEffect(card);
                if (target) free.condition = "적을 선택하고 " + (free.condition.Length > 0 ? free.condition : "사용했을 때");
                result.Add(free);
            }
            if (AccuracyMechanics.GuaranteedHit(card) && (!target || !AdvancedSummary(card).Contains("필중"))) Add(result, "accuracy", "", (target ? "적 대상 공격은 " : "") + "필중합니다");
            else if (Id(card) == "silvia_e") Add(result, "accuracy", "바이크 상태일 때", "필중합니다");
            else if (Id(card) == "garnet_r") Add(result, "accuracy", "철처녀 표식이 남은 적에게 이번 턴 무료 재사용할 때", "필중합니다");
            if (Id(card) == "garnet_r") Add(result, "execution", "이번 턴 무료 재사용으로 표식이 있고 사용 전 체력 " + FreeCastMechanics.GarnetExecutionPercent + "% 이하인 적을 적중했을 때", "처형합니다");
            string advanced = target ? "" : AdvancedSummary(card), companion = CompanionMechanics.CardSummary(card);
            if (advanced.Length > 0) Add(result, "advanced", "", advanced);
            if (companion.Length > 0) Add(result, "companion", "", companion);
            if (card.exhaust) Add(result, "exhaust", "", Id(card) == "garnet_r" ? "재사용·첫 공격 실패·턴 종료 시 소멸합니다" : "사용 후 소멸합니다");
            if (Id(card) == "basic_attack") Add(result, "basic:critical", "", "장비 공격력이 적용되며 기본 치명타 피해는 150%입니다");
            return result;
        }

        static string AdvancedSummary(CardDef card)
        {
            if (Id(card) == "sua_w")
                return "대상을 선택합니다. 자신에게 방어와 보호 방어도가 남은 동안 군중 제어 면역을 줍니다. 적에게 필중 피해·실명을 줍니다.";
            string text = AdvancedCardMechanics.Summary(card);
            if (AdvancedCardMechanics.CanCharge(card))
                text = text.Replace("1턴 충전하면", "충전 시 다음 내 턴부터 무료 발동하며");
            return text;
        }

        // These cards encode several interacting state machines. Their sentences are
        // semantic templates, while thresholds, damage, recast turn and target keys
        // still come from the passed instance. They never replace the complete rules.
        static string ComplexIdentity(CardDef card, int damage)
        {
            var rules = card.mechanics?.rules ?? new SkillRule[0];
            var baseline = GameDatabase.Card(card.id);
            if (baseline == null || rules.Any(rule => !(baseline.mechanics?.rules ?? new SkillRule[0]).Any(original => original.op == rule.op))) return "";
            int d = damage < 0 ? card.damage : damage;
            string attack = Particle("피해 " + d) + " 줍니다. ";
            string free = (card.freeCastNextTurn ? "다음 내 턴" : "이번 턴") + " " + card.freeCastCount + "회 무료 연계";
            string targets = string.Join("·", (card.freeCastTargets ?? new string[0]).Select(CardKey));
            switch (Id(card))
            {
                case "nia_w":
                    var blocks = rules.FirstOrDefault(r => r.op == "bonus_damage" && r.scaleKey == "blocks");
                    var qDiscount = rules.FirstOrDefault(r => r.op == "discount" && CardKey(r.targetCard) == "Q");
                    var batteryGain = rules.FirstOrDefault(r => r.op == "gain" && r.key == "battery");
                    if (blocks == null || qDiscount == null || batteryGain == null) return "";
                    return Particle("피해 " + d) + " 주고 블록당 피해 " + blocks.amount + "(최대 " + Math.Min(24, blocks.amount * blocks.cap) + ")를 더하며 모두 소모합니다. Q 코스트를 " + qDiscount.amount + " 줄입니다. 적중 시 배터리를 " + batteryGain.amount + " 쌓고 끌어당김을 주며 블록이 있을 때 둔화합니다.";
                case "nia_r":
                    var battery = rules.FirstOrDefault(r => r.op == "bonus_block" && r.scaleKey == "battery");
                    if (battery == null) return "";
                    int maximum = Math.Min(24, battery.amount * battery.cap);
                    int summons = rules.Where(r => r.op == "summon").Select(r => SkillMechanics.Clamp(r.duration, 1, 3)).DefaultIfEmpty(2).Max();
                    return "공격·방어하며 배터리 ×" + battery.amount + "(최대 " + maximum + ")의 추가 방어를 얻고 모두 소모합니다. 적중 시 기절시킵니다. 게임월드로 턴 끝에 " + summons + "회 공격합니다. Q를 이번 턴 무료 " + card.freeCastCount + "회 쓰고 소멸합니다.";
                case "cathy_q":
                    int wound = card.freeCastCondition?.conditionAmount ?? 2;
                    return "공격하며 다음 평타를 강화하고 적중 시 상처·출혈을 줍니다. 상처 " + wound + " 이상에서 적중해 최대면 소모해 중상·강한 출혈을 주며 이번 턴 Q " + card.freeCastCount + "회 무료로 씁니다. 상처·중상에 따라 방어·Q 할인을 얻습니다.";
                case "cathy_w":
                    return attack + "다음 기본 공격을 강화하며 적중 시 상처·출혈·둔화를 줍니다. 상처 조건을 채워 적중하면 소모해 중상·강한 출혈을 줍니다. 상처·중상에 따라 방어·Q 할인을 얻습니다.";
                case "garnet_r":
                    return "공격하며 적중 시 표식·속박·지연 공격을 주고 " + (card.freeCastNextTurn ? "다음 내 턴" : "이번 턴") + " R " + card.freeCastCount + "회 무료 재사용합니다. 표식 무료 재사용은 필중하며 사용 전 체력 " + FreeCastMechanics.GarnetExecutionPercent + "% 이하인 적을 처형합니다. 재사용·실패·턴 종료 시 소멸합니다.";
                case "shoichi_w":
                    return "필중 공격하며 회피합니다. 단검·표식에 따라 피해·W 할인을 얻습니다. 단검이 있고 적중하면 모두 소모해 " + free + "하며, 표식도 있으면 E를 할인합니다.";
                case "isaac_e":
                    return "공격·회피합니다. 적중 시 착취를 쌓고 기절시키며 경화를 해제합니다. 착취·경화 조건별로 피해·회복·QWE 할인과 소모를 적용합니다. 검거를 준비하며 준비된 E 적중 피해 전에 방어도를 모두 파괴합니다.";
                case "elena_w":
                    int cold = rules.Where(r => r.conditionKey == "chill" && r.op == "consume").Select(r => r.conditionAmount).DefaultIfEmpty(2).Min();
                    string discount = string.Join("·", rules.Where(r => r.op == "discount").Select(r => CardKey(r.targetCard)).Distinct());
                    return "공격·방어·회피하며 적중 시 냉기를 쌓습니다. 냉기 " + cold + " 이상 적중 시 소모해 빙결시킵니다. 빙결을 맞히면 강화·해제합니다. 빙결 지대에서 " + discount + "를 할인합니다. 적중 시 이번 턴 " + targets + "를 각 " + card.freeCastCount + "회 무료로 씁니다.";
                case "elena_q":
                    int coldGate = rules.Where(r => r.op == "consume" && r.key == "chill").Select(r => r.conditionAmount).DefaultIfEmpty(2).Min();
                    int icicleDelay = rules.Where(r => r.op == "delayed_damage").Select(r => SkillMechanics.Clamp(r.delay, 1, 3)).DefaultIfEmpty(1).Min();
                    return "공격하며 턴 끝 " + icicleDelay + "회 뒤 공격할 고드름을 설치합니다. 적중 시 냉기를 쌓으며 사용 전 냉기 " + coldGate + " 이상이면 소모해 빙결시킵니다. 기존 빙결 상대에겐 추가 피해를 주고 적중 시 빙결을 해제합니다.";
                case "garnet_w":
                    var painDiscounts = rules.Where(r => r.op == "discount").ToArray();
                    int painGate = rules.Where(r => r.op == "delayed_damage" && !string.IsNullOrEmpty(r.conditionKey)).Select(r => r.conditionAmount).DefaultIfEmpty(3).Max();
                    string painTargets = string.Join("·", painDiscounts.Select(r => CardKey(r.targetCard)).Distinct());
                    string painThresholds = string.Join("/", painDiscounts.Select(r => r.conditionAmount).Distinct());
                    return "방어·회복하고 고통을 모두 소모합니다. 소모 전 고통만큼 회복을 강화하며 " + painThresholds + " 이상에서 각각 다음 " + painTargets + "를 할인합니다. 턴 끝 둔화 공격을 설치하고 사용 전 고통 " + painGate + " 이상이면 속박 공격을 더합니다.";
                case "ceres_q":
                    return "방어·회피합니다. 사용 전 관철 준비 중이면 강화 공격하며 적중 시 기절시키고 준비를 해제합니다. 준비 전이면 관철을 준비하고 턴 끝 공격을 설치합니다.";
                case "chloe_r":
                    return "공격·방어하며 니나를 살려 " + CompanionMechanics.RLinkTurns + "턴간 협공·피해 " + CompanionMechanics.LinkedSharePercent + "% 분담합니다. "
                        + CompanionMechanics.RLethalGuardTurns + "턴간 분담 후 니나 생존 시 치명상을 체력 1로 버팁니다. " + targets + "를 할인하며 적중 시 " + (card.freeCastNextTurn ? "다음 내 턴" : "이번 턴") + " 각 " + card.freeCastCount + "회 무료로 씁니다. 사용 후 소멸합니다.";
                case "xuelin_e":
                    return "공격하며 회피합니다. 어검·검진에 따라 회수·소모하며 피해·회복을 강화합니다. 적중 시 외출 해제·둔화·지연 낙검을 적용합니다. 외출 어검이 있으면 적중 시 지속 어검도 남깁니다.";
                case "rozzi_q": case "rozzi_w":
                    return (Id(card) == "rozzi_w" ? "공격·방어하며 " : "공격·회피하며 ") + "다음 기본 공격을 강화합니다. 폭탄이 있으면 추가 피해·R 할인을 얻습니다. 적중 시 폭탄을 소모·회수하고 "
                        + (Id(card) == "rozzi_w" ? "치유 감소·방어력 감소를 주며 " : "") + targets + " " + free + "를 얻습니다.";
                case "arda_w":
                    return "공격하며 정수 조건에 따라 회복·소모합니다. 적중 시 정수·둔화·지연 입방체를 남기고 각성을 해제합니다. 각성 상태에서 적중하면 입방체를 더 설치합니다. 입방체 적중 시 기절시킵니다.";
                case "lenox_q":
                    return "공격·방어하며 최대 체력·회오리에 따라 피해를 강화합니다. 적중 시 회오리를 쌓고 " + targets + " " + free + "합니다. 회오리 조건을 채우면 모두 소모합니다.";
                case "rio_r":
                    return "공격하고 활 자세에 따라 추가 피해 또는 지연 공격을 주며 흐름을 바꿉니다. 자세에 따라 적중 시 둔화·기절·넉백을 주며 " + targets + " " + free + "합니다.";
                case "vanya_e":
                    return "공격·회피하며 꿈을 쌓습니다. 꿈 조건을 채우면 소모해 방어·지속 나비 소환을 얻습니다. 몽롱한 적에게 추가 피해를 주고 적중 시 몽롱함을 해제하며 지속 효과를 회수하고 둔화합니다.";
                case "vanya_q":
                    return "공격·방어하며 꿈을 쌓고 귀환 나비를 지연 설치합니다. 꿈 조건을 채우면 소모해 방어·지속 나비 공격을 얻습니다. 적중 시 " + targets + " " + free + "를 예약합니다.";
                case "barbara_r":
                    return "카드 " + card.draw + "장을 뽑고 Q·W·E를 할인하며 " + free + "합니다. 조건에 따라 지속 소환 공격을 남깁니다. 이 카드 적중 시 둔화·기절을 줍니다. 사용 후 소멸합니다.";
                case "felix_e":
                    return "공격·회복하며 다음 기본 공격을 강화합니다. 적중 시 자원을 쌓고 조건에 따라 소모해 더 회복합니다. 적중 시 " + targets + " 각각 " + free + "합니다.";
                default: return "";
            }
        }

        static void AddCompact(List<ConditionEffect> result, CardDef card, IEnumerable<SkillRule> rules, string effect, string prefix = "", bool terse = false)
        {
            var selected = rules.ToArray();
            string condition = CompactCondition(card, selected, terse);
            if (AdvancedCardMechanics.HasTargetChoice(card) && !prefix.StartsWith("적을 선택하고", StringComparison.Ordinal)) prefix = "적을 선택하고 " + prefix;
            if (prefix.Length > 0) condition = prefix + (condition.Length > 0 ? condition : "사용했을 때");
            string identity = string.Join("/", selected.Select(r => DescriptionSummary.ConditionKey(r)).Distinct().OrderBy(key => key));
            Add(result, "compact:" + identity + "|" + prefix, condition, effect);
        }

        static bool HasEligibility(SkillRule rule) => rule.onHit || !string.IsNullOrEmpty(rule.conditionKey)
            || !string.IsNullOrEmpty(rule.conditionKey2) || !string.IsNullOrEmpty(rule.conditionPrevious);

        static string CompactCondition(CardDef card, SkillRule[] rules, bool terse)
        {
            if (rules.Length == 0) return "";
            if (terse)
            {
                if (rules.Select(r => DescriptionSummary.ConditionKey(r)).Distinct().Count() > 1)
                    return "조건별로";
                bool eligible = rules.Any(r => !string.IsNullOrEmpty(r.conditionKey) || !string.IsNullOrEmpty(r.conditionKey2) || !string.IsNullOrEmpty(r.conditionPrevious));
                bool hit = rules.Any(r => r.onHit);
                return hit && rules.Any(r => !r.onHit) ? "적중 등 조건에 따라"
                    : eligible ? hit ? "조건부 적중 시" : "조건부로" : hit ? "적중 시" : "";
            }
            if (!terse && rules.Select(r => DescriptionSummary.ConditionKey(r)).Distinct().Count() == 1) return Condition(card, rules[0], rules[0].onHit);
            var conditions = new List<string>();
            if (rules.Any(r => r.onHit)) conditions.Add("적중");
            if (rules.Any(r => !string.IsNullOrEmpty(r.conditionPrevious))) conditions.Add("직전 기술");
            string[] keys = rules.SelectMany(r => new[] { r.conditionKey, r.conditionKey2 }).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToArray();
            if (keys.Any(k => !SkillMechanics.IsState(card, k))) conditions.Add("자원");
            if (keys.Any(k => SkillMechanics.IsState(card, k))) conditions.Add("상태");
            if (conditions.Count == 1 && conditions[0] == "적중") return "적중 시";
            return conditions.Count == 0 ? "" : string.Join("·", conditions) + " 조건에 따라";
        }

        static void Add(List<ConditionEffect> list, string key, string condition, string effect)
        {
            if (string.IsNullOrWhiteSpace(effect)) return;
            list.Add(new ConditionEffect { key = key, condition = condition, effect = effect.Trim().TrimEnd('.') });
        }

        public static string Condition(CardDef card, SkillRule rule, bool onHit, bool eitherResource = false)
        {
            var conditions = new List<string>();
            if (!string.IsNullOrEmpty(rule.conditionKey)) conditions.Add(ResourceCondition(card, rule.conditionKey, rule.conditionAmount, rule.conditionExact));
            if (!string.IsNullOrEmpty(rule.conditionKey2)) conditions.Add(ResourceCondition(card, rule.conditionKey2, rule.conditionAmount2, rule.conditionExact2));
            if (eitherResource && conditions.Count == 2)
            {
                string alternatives = string.Join(" 또는 ", conditions.Select(x => x.EndsWith("이면", StringComparison.Ordinal) ? x.Substring(0, x.Length - 2) : x));
                conditions = new List<string> { "(" + alternatives + ")이면" };
            }
            if (!string.IsNullOrEmpty(rule.conditionPrevious)) conditions.Add("직전 " + CardKey(rule.conditionPrevious) + "이면");
            if (onHit) conditions.Add("적중 시");
            return DescriptionSummary.JoinConditions(conditions);
        }

        static string ResourceCondition(CardDef card, string key, int amount, bool exact)
        {
            if (SkillMechanics.IsState(card, key)) return SkillMechanics.StateCondition(card, key, amount, exact);
            return Resource(card, key) + " " + amount + (exact ? "" : " 이상") + "이면";
        }

        static void AddStatus(List<ConditionEffect> result, CardDef card, CardStatusRule rule, string prefix = "")
        {
            if (rule == null) return;
            string condition = Condition(card, rule, rule.timing == "cast" && rule.onHit);
            string trigger = rule.timing == "next_basic" ? "다음 기본 공격 적중 시 " : rule.timing != "cast" ? "해당 설치물·추가 효과 적중 시 " : "";
            if (rule.timing != "cast" && rule.onHit)
                condition = Condition(card, rule, false) + (condition.Length > 0 ? "이고 " : "") + "이 카드가 적중했을 때";
            string key = DescriptionSummary.ConditionKey(rule, rule.timing == "cast" ? "cast" : rule.timing + "|castHit:" + rule.onHit,
                rule.timing != "cast" || rule.onHit);
            Add(result, key, prefix + condition, trigger + Particle(StatusMechanics.Name(rule.key)) + " " + rule.duration + "턴 부여합니다");
        }

        static string Amount(CardDef card, SkillRule rule)
        {
            if (string.IsNullOrEmpty(rule.scaleKey)) return rule.amount.ToString();
            int maximum = rule.op == "bonus_damage" || rule.op == "bonus_block" || rule.op == "delayed_damage" ? 24
                : rule.op == "bonus_heal" ? 20 : rule.op == "counter" ? 8 : rule.op == "discount" ? 7 : rule.op == "revive" ? 40 : rule.op == "empower_basic" ? 16 : 10;
            return Resource(card, rule.scaleKey) + " ×" + rule.amount + "(최대 " + Math.Min(maximum, rule.amount * rule.cap) + ")";
        }

        static string RuleText(CardDef card, SkillRule rule)
        {
            string label = Resource(card, rule.key), amount = Amount(card, rule);
            string effectLabel = string.IsNullOrWhiteSpace(rule.label) ? label : rule.label;
            int duration = SkillMechanics.Clamp(rule.duration, 1, 3);
            switch (rule.op)
            {
                case "gain": return Particle(label) + " " + rule.amount + (CardPresentation.IsFieldResource(card, rule.key) ? " 남깁니다" : " 늘립니다") + "(최대 " + SkillMechanics.Clamp(rule.cap, 1, 8) + ")";
                case "set": return CardPresentation.IsFieldResource(card, rule.key) ? Particle(label) + (rule.amount > 0 ? " " + rule.amount + " 남깁니다" : " 모두 없앱니다") : Particle(label) + " " + rule.amount + "로 만듭니다";
                case "consume": return Particle(label) + " " + (rule.amount <= 0 ? "모두" : rule.amount.ToString()) + " 소모합니다";
                case "state_on": return SkillMechanics.StateEnterText(card, rule.key);
                case "state_off": return SkillMechanics.StateExitText(card, rule.key);
                case "state_toggle": return SkillMechanics.StateName(card, rule.key) + "로 진입하거나 해제합니다";
                case "bonus_damage": return "첫 적중 피해에 " + amount + "을 더합니다";
                case "bonus_block": return "추가 방어도 " + amount + "을 얻습니다";
                case "bonus_heal": return "체력을 " + amount + " 추가 회복합니다";
                case "discount": return "다음 " + CardKey(rule.targetCard) + " 코스트를 " + amount + " 줄입니다(턴당 1회)";
                case "bleed": case "burn": case "summon":
                    return "내 턴 끝마다 " + (rule.op == "bleed" ? "출혈" : rule.op == "burn" ? "화상" : effectLabel)
                        + " 피해 " + amount + "을 " + duration + "회 주는 효과를 남깁니다" + (rule.op == "summon" ? "" : "(방어도 무시)");
                case "hot": return "내 턴 끝마다 체력을 " + amount + "씩 " + duration + "회 회복하는 효과를 얻습니다";
                case "guard": return "내 턴 끝마다 방어도 " + amount + "을 " + duration + "회 얻는 효과를 얻습니다";
                case "delayed_damage": return "내 턴 종료 " + SkillMechanics.Clamp(rule.delay, 1, 3) + "회 후 피해 " + amount + "을 주는 " + Particle(effectLabel) + " 설치합니다";
                case "counter": return duration + "턴간 직접 공격에 맞을 때 피해 " + amount + "으로 반격하는 효과를 얻습니다"
                    + (SkillMechanics.CounterEveryHit(card.id, rule.key) ? "(횟수 제한 없음)" : "(턴당 1회)");
                case "break_block": return "피해 전에 적의 방어도를 모두 파괴합니다";
                case "empower_basic": return "다음 기본 공격의 첫 적중 피해를 " + amount + " 늘립니다(1회)";
                case "revive": return duration + "턴간 치명적인 피해를 1회 막고 체력 " + amount + "을 회복하는 효과를 얻습니다";
                case "clear_effect": return effectLabel + "의 남은 효과를 회수합니다";
                default: return "";
            }
        }

        static ConditionEffect FreeCastEffect(CardDef card)
        {
            var rule = card.freeCastCondition ?? new SkillRule();
            string condition = Condition(card, rule, card.freeCastOnHit || rule.onHit, card.freeCastEitherResource).Replace("적중했을 때", "적중 시");
            string targets = string.Join("·", card.freeCastTargets.Select(CardKey));
            string effect = card.freeCastLastSkill ? "직전 Q·W·E를 이번 턴에 무료로 " + card.freeCastCount + "회 재사용합니다"
                : Particle(targets) + " " + (card.freeCastNextTurn ? "다음 내 턴" : "이번 턴") + " 무료 " + (card.freeCastTargets.Length > 1 ? "각 " : "") + card.freeCastCount + "회 씁니다";
            effect += "(턴당 1회)";
            return new ConditionEffect { key = DescriptionSummary.ConditionKey(rule, onHit: card.freeCastOnHit || rule.onHit)
                + (card.freeCastEitherResource ? "|resource_or" : ""), condition = condition, effect = effect };
        }
    }
}
