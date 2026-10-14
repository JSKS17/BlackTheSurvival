using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    // A purchased rare encounter card is a single ++ copy of its original skill.
    // Conditions, resource caps, timing, form changes and recast limits stay intact.
    public static class RareCardUpgrades
    {
        public const string BranchId = "rare";
        public const string Label = "레어 강화";

        public static int Price(CardDef source)
        {
            return source == null ? 120 : 120 + 20 * Math.Max(0, source.cost);
        }

        public static string SuffixForLabel(string upgradeLabel)
        {
            return string.IsNullOrEmpty(upgradeLabel) ? "" : upgradeLabel == Label ? " ++" : " +";
        }

        public static CardDef Apply(CardDef source)
        {
            var card = StrategyMechanics.Copy(source);
            if (card == null) return null;
            bool improved = false;
            if (card.damage > 0) { card.damage += 6; improved = true; }
            if (card.block > 0) { card.block += 6; improved = true; }
            if (card.heal > 0) { card.heal += 4; improved = true; }
            if (card.evasion > 0 && card.evasion < 90) { card.evasion = Math.Min(90, card.evasion + 10); improved = true; }
            if (card.poison > 0) { card.poison += 2; improved = true; }
            if (card.weak > 0) { card.weak += 2; improved = true; }
            if (card.vulnerable > 0) { card.vulnerable += 2; improved = true; }

            var rules = card.mechanics?.rules ?? new SkillRule[0];
            // Generate resources faster, without turning binary forms into quantities
            // or raising any original cap. Existing condition/on-hit checks are copied.
            var resource = rules.FirstOrDefault(rule => rule != null && !rule.isState && rule.op == "gain"
                && rule.amount > 0 && rule.amount < Math.Min(8, rule.cap) && !IsCyclicSelector(rules, rule));
            if (resource != null) { resource.amount++; improved = true; }
            var numeric = rules.FirstOrDefault(rule => rule != null && !rule.isState && rule.amount > 0
                && NumericLimit(rule.op) > rule.amount);
            if (numeric != null)
            {
                int originalAmount = numeric.amount;
                var numericTargets = new[] { numeric }.AsEnumerable();
                // The twins encode one symmetric action as two color-conditioned
                // rules. Strengthen both sides together, keeping their color checks.
                if (new[] { "debi_marlene_q", "debi_marlene_w", "debi_marlene_e" }.Contains(card.id) && numeric.op == "bonus_damage")
                    numericTargets = rules.Where(rule => rule != null && rule.op == numeric.op && rule.amount == originalAmount);
                foreach (var target in numericTargets)
                    target.amount = Math.Min(NumericLimit(target.op), originalAmount + (target.op == "discount" ? 1 : 2));
                improved = true;
            }
            // Expensive cards receive the single existing efficiency step, with no
            // change to zero-cost cards or to per-turn recast permissions.
            if (card.cost >= 3) card.cost--;

            if (!improved)
            {
                // State-only utility needs a benefit too. This effect refreshes one
                // bounded timed guard; replaying a free card never stacks or draws it.
                if (card.mechanics == null) card.mechanics = new SkillMechanicProfile();
                var utility = new List<SkillRule>(card.mechanics.rules ?? new SkillRule[0]);
                utility.Add(new SkillRule { op = "guard", key = "rare_guard", label = "레어 방호", amount = 4, duration = 2 });
                card.mechanics.rules = utility.ToArray();
            }
            card.description = CardPresentation.Describe(card, GameDatabase.Cards);
            return card;
        }

        static int NumericLimit(string operation)
        {
            switch (operation)
            {
                case "bonus_damage": case "bonus_block": case "delayed_damage": return 24;
                case "bonus_heal": return 20;
                case "bleed": case "burn": case "summon": case "guard": case "hot": return 10;
                case "counter": return 8;
                case "empower_basic": return 16;
                case "discount": return 7;
                default: return 0;
            }
        }

        static bool IsCyclicSelector(IEnumerable<SkillRule> rules, SkillRule gain)
        {
            // Tia's brush color and Adina's constellation are three-way selectors,
            // encoded as 0 -> 1 -> 2 -> 0 rather than as expendable resources.
            return rules.Any(rule => rule != null && rule.op == "set" && rule.key == gain.key && rule.amount == 0
                && rule.conditionKey == gain.key && rule.conditionExact && rule.conditionAmount >= gain.cap);
        }
    }
}
