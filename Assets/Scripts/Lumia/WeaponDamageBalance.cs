using System.Collections.Generic;

namespace Lumia
{
    /// <summary>Applies the D/F damage budget once, after their original skill profiles are configured.</summary>
    public static class WeaponDamageBalance
    {
        static readonly HashSet<string> DamageOperations = new HashSet<string>
        {
            "bonus_damage", "bleed", "burn", "delayed_damage", "summon", "counter", "empower_basic"
        };

        // Nearest integer to 80%, with long arithmetic so even malformed large values cannot overflow.
        // Zero and negative profile sentinels retain their original meaning; positive damage stays positive.
        public static int ReducedDamage(int damage) => damage <= 0 ? damage : (int)((damage * 4L + 2L) / 5L);

        public static void Configure(IEnumerable<CardDef> cards)
        {
            if (cards == null) return;
            foreach (var card in cards)
            {
                if (card == null || (card.key != "D" && card.key != "F")) continue;
                card.damage = ReducedDamage(card.damage);
                foreach (var rule in card.mechanics?.rules ?? new SkillRule[0])
                    if (rule != null && DamageOperations.Contains(rule.op))
                        rule.amount = ReducedDamage(rule.amount);
            }
        }
    }
}
