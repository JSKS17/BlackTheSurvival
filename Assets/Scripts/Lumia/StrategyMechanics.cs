using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    [Serializable] public sealed class CardUpgradeSelection { public string cardId, instanceId, branchId; }
    [Serializable] public sealed class PendingEncounterFollowup { public string eventId, owner; public bool priorityNext = true; }
    public sealed class CardUpgradeOption { public string id, name, description; }

    // Run-specific card changes always use copies. Shared database cards and enemy skills remain unchanged.
    public static class StrategyMechanics
    {
        public static CardDef Copy(CardDef source)
        {
            if (source == null) return null;
            var copy = new CardDef();
            foreach (var field in typeof(CardDef).GetFields()) field.SetValue(copy, field.GetValue(source));
            if (source.mechanics != null)
            {
                copy.mechanics = new SkillMechanicProfile();
                foreach (var field in typeof(SkillMechanicProfile).GetFields()) field.SetValue(copy.mechanics, field.GetValue(source.mechanics));
                copy.mechanics.rules = (source.mechanics.rules ?? new SkillRule[0]).Select(rule =>
                {
                    var clone = new SkillRule();
                    foreach (var field in typeof(SkillRule).GetFields()) field.SetValue(clone, field.GetValue(rule));
                    return clone;
                }).ToArray();
            }
            return copy;
        }

        public static List<CardUpgradeOption> Options(CardDef source)
        {
            var options = new List<CardUpgradeOption>();
            if (source == null || source.category == "weapon") return options;
            if (HasPowerUpgrade(source)) options.Add(Option(source, "power", "출력 강화"));
            if (source.id == "nia_q") options.Add(Option(source, "arcade", "블록 증설"));
            else if (source.id == "nia_w") options.Add(Option(source, "arcade", "블록 연산 강화"));
            else if (source.cost >= 2) options.Add(Option(source, "efficiency", "코스트 절약"));
            return options;
        }

        public static bool CanUpgrade(CardDef source)
        {
            return source != null && source.category != "weapon" && (HasPowerUpgrade(source) || source.cost >= 2 || source.id == "nia_q" || source.id == "nia_w");
        }

        public static string BranchName(string cardId, string branchId)
        {
            return branchId == "power" || branchId == "legacy" ? "출력 강화" : branchId == "efficiency" ? "코스트 절약"
                : branchId == "arcade" ? cardId == "nia_q" ? "블록 증설" : "블록 연산 강화" : "강화";
        }

        private static CardUpgradeOption Option(CardDef source, string id, string name)
        {
            var card = Apply(source, id);
            string change = id == "efficiency" ? "기본 코스트를 " + card.cost + "로 줄입니다."
                : id == "arcade" ? source.id == "nia_q" ? "블록 생성량을 2로 늘립니다. 블록 최대치는 3으로 유지합니다." : "회수하는 블록마다 추가 피해를 6 줍니다."
                : PowerDescription(source);
            string rules = CardPresentation.Rules(card);
            return new CardUpgradeOption { id = id, name = name, description = change + "\n\n강화 후 전체 효과\n" + card.description + (string.IsNullOrEmpty(rules) ? "" : "\n\n" + rules) };
        }

        public static bool HasPowerUpgrade(CardDef card)
        {
            return card.damage > 0 || card.block > 0 || card.heal > 0 || card.evasion > 0 || card.poison > 0
                || card.weak > 0 || card.vulnerable > 0 || (card.mechanics != null && card.mechanics.rules.Any(IsPowerRule));
        }

        private static bool IsPowerRule(SkillRule rule)
        {
            return rule != null && !rule.isState && rule.amount > 0 &&
                new[] { "bonus_damage", "bonus_block", "bonus_heal", "bleed", "burn", "summon", "guard", "hot" }.Contains(rule.op);
        }

        private static string PowerDescription(CardDef card)
        {
            var changes = new List<string>();
            if (card.damage > 0) changes.Add("기본 피해 +3");
            if (card.block > 0) changes.Add("방어 +3");
            if (card.heal > 0) changes.Add("회복 +2");
            if (changes.Count == 0)
            {
                if (card.evasion > 0) changes.Add("회피율 +5%p");
                else if (card.poison > 0) changes.Add("중독 +1");
                else if (card.weak > 0) changes.Add("약화 지속 +1턴");
                else if (card.vulnerable > 0) changes.Add("취약 지속 +1턴");
                else changes.Add("첫 추가 효과의 수치 +1");
            }
            return string.Join(" · ", changes) + ".";
        }

        public static CardDef Apply(CardDef source, string branch)
        {
            var card = Copy(source);
            if (card == null) return null;
            if (branch == "efficiency") card.cost = Math.Max(1, card.cost - 1);
            else if (branch == "arcade" && card.mechanics != null)
            {
                foreach (var rule in card.mechanics.rules)
                {
                    if (card.id == "nia_q" && rule.op == "gain" && rule.key == "blocks") rule.amount = 2;
                    if (card.id == "nia_w" && rule.op == "bonus_damage" && rule.scaleKey == "blocks") rule.amount = 6;
                }
            }
            else if (branch == "power" || branch == "legacy")
            {
                bool numeric = card.damage > 0 || card.block > 0 || card.heal > 0;
                if (card.damage > 0) card.damage += 3;
                if (card.block > 0) card.block += 3;
                if (card.heal > 0) card.heal += 2;
                if (!numeric && branch != "legacy")
                {
                    if (card.evasion > 0) card.evasion = Math.Min(90, card.evasion + 5);
                    else if (card.poison > 0) card.poison++;
                    else if (card.weak > 0) card.weak++;
                    else if (card.vulnerable > 0) card.vulnerable++;
                    else if (card.mechanics != null)
                    {
                        var rule = card.mechanics.rules.FirstOrDefault(IsPowerRule);
                        if (rule != null) rule.amount++;
                    }
                }
            }
            card.description = CardPresentation.Describe(card, GameDatabase.Cards);
            return card;
        }
    }
}
