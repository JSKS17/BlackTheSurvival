using System;
using System.Collections.Generic;

namespace Lumia
{
    // A module is an inventory item, rather than a combat card or a global
    // upgrade. Its level belongs to one physical copy in the saved deck.
    public static class TacticalModules
    {
        public const string ItemId = "tactical_module";
        public const string Name = "전술강화 모듈";
        public const string UpgradeLabel = "전술 강화";
        public const string Description = "가방에서 사용하면 보유한 전술 스킬(F) 카드 1장을 선택해 고유 강화 효과를 적용합니다. 한 장에는 한 번만 적용하며, 같은 종류의 다른 카드와 이후 얻는 카드는 강화되지 않습니다. 일반·레어 강화와 함께 적용할 수 있습니다. 전투 중에는 사용할 수 없습니다.";

        // Module output never unlocks a different ordinary branch. Both the
        // engine and cached UI describe the same canonical composition used by
        // PlayerCard: original -> ordinary branch -> tactical module.
        public static List<CardUpgradeOption> UpgradeOptions(CardDef baseSource, bool tacticalEnhanced)
        {
            var options = StrategyMechanics.Options(baseSource);
            if (!tacticalEnhanced || !TacticalSkills.IsTactical(baseSource)) return options;
            const string heading = "\n\n강화 후 전체 효과\n";
            var result = new List<CardUpgradeOption>();
            foreach (var option in options)
            {
                var card = TacticalSkills.ApplyModuleUpgrade(StrategyMechanics.Apply(baseSource, option.id));
                string rules = CardPresentation.Rules(card);
                int at = (option.description ?? "").IndexOf(heading, StringComparison.Ordinal);
                string change = at < 0 ? option.description : option.description.Substring(0, at);
                result.Add(new CardUpgradeOption { id = option.id, name = option.name,
                    description = change + heading + card.description + (string.IsNullOrEmpty(rules) ? "" : "\n\n" + rules) });
            }
            return result;
        }
    }
}
