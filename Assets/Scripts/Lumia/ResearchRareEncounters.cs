using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    // Rare stories improve their immediate choices without adding permanent stats
    // or forcing extra copies of cards into a carefully trimmed deck.
    public static class RareEncounterRules
    {
        public const int BonusCredits = 30;
        public const int MinimumHealing = 28;
        public const int HealingPremium = 8;
        public const int MinimumCredits = 55;
        public const int CreditPremium = 10;

        public static void Configure(IEnumerable<EventDef> encounters)
        {
            foreach (var encounter in encounters)
            {
                var ordinary = GameDatabase.Events.Where(e => !e.chainOnly && e.owner == encounter.owner)
                    .SelectMany(e => e.options ?? new EventOption[0]).ToArray();
                foreach (var option in encounter.options ?? new EventOption[0])
                {
                    // The catalog can safely be configured again during audits.
                    if (option.bonusCredits == BonusCredits) continue;
                    switch (option.effect)
                    {
                        case "heal":
                            option.amount = Math.Max(option.amount, Math.Max(MinimumHealing,
                                ordinary.Where(o => o.effect == "heal").Select(o => o.amount).DefaultIfEmpty(0).Max() + HealingPremium));
                            break;
                        case "credits":
                            option.amount = Math.Max(option.amount, Math.Max(MinimumCredits,
                                ordinary.Where(o => o.effect == "credits").Select(o => o.amount).DefaultIfEmpty(0).Max() + CreditPremium));
                            break;
                        case "trade_card": case "trade_object":
                            option.amount = Math.Max(0, option.amount * 3 / 4);
                            break;
                    }
                    option.bonusCredits = BonusCredits;
                }
            }
        }

        public static string BonusSummary(EventOption option) => option != null && option.bonusCredits > 0
            ? " · 크레딧 +" + option.bonusCredits : "";
    }
}
