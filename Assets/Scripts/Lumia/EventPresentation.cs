using System.Collections.Generic;

namespace Lumia
{
    public static class EventPresentation
    {
        // Called after all card/trait profiles are configured, so encounter rewards
        // use the same final definitions and descriptions as combat and the catalog.
        public static void Configure(IEnumerable<EventDef> events)
        {
            foreach (var encounter in events)
                foreach (var option in encounter.options)
                {
                    string summary = RewardSummary(option);
                    string narrative = (option.description ?? "").Trim();
                    // A second initialization must not duplicate the reward paragraph.
                    while (!string.IsNullOrEmpty(summary) && narrative.EndsWith(summary, System.StringComparison.Ordinal))
                        narrative = narrative.Substring(0, narrative.Length - summary.Length).TrimEnd();
                    option.description = string.IsNullOrEmpty(narrative) ? summary : narrative + " " + summary;
                }
        }

        public static CardDef RewardCard(EventOption option)
        {
            if (option == null || (option.effect != "card" && option.effect != "trade_card" && option.effect != "risky_card" && option.effect != "rare_trade_card")) return null;
            var card = GameDatabase.Card(option.cardId);
            return option.effect == "rare_trade_card" ? RareCardUpgrades.Apply(card) : card;
        }

        public static string RewardTitle(EventOption option)
        {
            var card = RewardCard(option);
            if (card != null) return card.owner + " · " + card.name + (option.effect == "rare_trade_card" ? "++" : "") + " [" + card.key + "] · " + card.cost + "코스트";
            if (option == null) return "";
            switch (option.effect)
            {
                case "food": return (GameDatabase.Food(option.objectId)?.name ?? "음식") + " 1개";
                case "object": case "trade_object": case "risky_object": return (GameDatabase.Object(option.objectId)?.name ?? "오브젝트") + " 1개";
                case "credits": return "크레딧 " + option.amount;
                case "heal": return "체력 " + option.amount + " 회복";
                case "max_health": return "최대 체력 +" + option.amount;
                case "max_energy": return "최대 코스트 +" + option.amount;
                case "upgrade_card": return "원하는 카드 1장 강화";
                case "rare_remove_cards": return "원하는 카드 최대 2장 제거";
                case "rare_support_runes": return "현재 메인 계열의 나머지 보조 룬 모두 획득";
                case "rare_copy_cards": return "미강화·원본 코스트 1 이하 카드 1종을 2장 복제";
                case "rare_tactical": return "무작위 전술 카드 1장";
                case "passive": return (GameDatabase.Passive(option.passiveId)?.name ?? "고유 능력") + " 패시브";
                case "remove_card": return "덱 정리";
                default: return "";
            }
        }

        public static string RewardSummary(EventOption option)
        {
            if (option == null) return "";
            string cost = CostSummary(option);
            string followup = FollowupSummary(option);
            return (string.IsNullOrEmpty(cost) ? "" : cost + " ") + ImmediateRewardSummary(option)
                + (option.bonusCredits > 0 ? " 추가로 크레딧 " + option.bonusCredits + "을 획득합니다." : "")
                + (string.IsNullOrEmpty(followup) ? "" : " " + followup);
        }

        public static string CostSummary(EventOption option)
        {
            if (option == null) return "";
            var costs = new List<string>();
            if (option.creditCost > 0) costs.Add("크레딧 " + option.creditCost);
            if (!string.IsNullOrEmpty(option.requiredFoodId)) costs.Add((GameDatabase.Food(option.requiredFoodId)?.name ?? option.requiredFoodId) + " 1개");
            if (!string.IsNullOrEmpty(option.requiredObjectId)) costs.Add((GameDatabase.Object(option.requiredObjectId)?.name ?? option.requiredObjectId) + " 1개");
            bool hasItem = !string.IsNullOrEmpty(option.requiredFoodId) || !string.IsNullOrEmpty(option.requiredObjectId);
            return costs.Count == 0 ? "" : string.Join(", ", costs) + (hasItem ? "를" : "을") + " 지불합니다.";
        }

        public static string FollowupSummary(EventOption option)
        {
            if (option == null || string.IsNullOrEmpty(option.followupId)) return "";
            var next = EncounterChains.Event(option.followupId);
            if (next == null) return "";
            return "다음 실험체 조우에서 " + next.owner + "의 ‘" + next.title + "’ 선택지가 80% 확률로 등장합니다.";
        }

        public static string ImmediateRewardSummary(EventOption option)
        {
            if (option == null) return "";
            var card = RewardCard(option);
            string cardName = card == null ? "지정 카드" : "‘" + card.owner + " · " + card.name + " [" + card.key + "]’ 카드";
            string material = GameDatabase.Object(option.objectId)?.name ?? "지정 오브젝트";
            var food = GameDatabase.Food(option.objectId);
            switch (option.effect)
            {
                case "card": return cardName + " 1장을 덱에 추가합니다.";
                case "trade_card": return "크레딧 " + option.amount + "을 지불하고 " + cardName + " 1장을 덱에 추가합니다.";
                case "rare_trade_card": return "크레딧 " + option.amount + "을 지불하고 특별 강화된 ‘" + card.owner + " · " + card.name + "++ [" + card.key + "]’ 카드 1장을 덱에 추가합니다. 같은 종류의 다른 카드는 강화되지 않습니다.";
                case "risky_card": return "체력 " + option.amount + "을 소모하고 " + cardName + " 1장을 덱에 추가합니다.";
                case "heal": return "체력을 " + option.amount + " 회복합니다.";
                case "credits": return "크레딧 " + option.amount + "을 획득합니다.";
                case "damage": return "체력을 " + option.amount + " 소모합니다.";
                case "max_health": return "최대 체력이 " + option.amount + " 증가합니다.";
                case "max_energy": return "이번 탈출 동안 턴마다 사용할 최대 코스트가 " + option.amount + " 증가합니다.";
                case "object": return material + " 1개를 획득합니다.";
                case "trade_object": return "크레딧 " + option.amount + "을 지불하고 " + material + " 1개를 획득합니다.";
                case "risky_object": return "체력 " + option.amount + "을 소모하고 " + material + " 1개를 획득합니다.";
                case "food": return (food?.name ?? "지정 음식") + " 1개를 가방에 넣습니다. 사용하면 " + (food != null && food.fullHeal ? "체력을 모두 회복합니다." : "체력을 " + (food?.heal ?? 0) + " 회복합니다.");
                case "passive": return (GameDatabase.Passive(option.passiveId)?.name ?? "지정 패시브") + " 패시브를 획득합니다.";
                case "rune_change": case "swap_rune": return "메인 룬과 같은 계열의 보조 룬을 새로 선택합니다.";
                case "remove_card": return "무기 카드 이외의 무작위 카드 1장을 제거합니다. 덱에는 최소 5장을 남깁니다.";
                case "upgrade_card": return "덱에서 원하는 카드 1장과 강화 방향을 선택합니다. 선택한 한 장만 강화되며, 같은 종류의 다른 카드와 이후 획득하는 카드는 그대로입니다. 한 장에는 한 강화 방향만 적용할 수 있습니다.";
                case "rare_remove_cards": return "무기 카드 이외의 원하는 카드를 최대 2장 제거합니다. 덱에는 최소 5장을 남기며, 제거하지 않고 넘어갈 수 있습니다.";
                case "rare_support_runes": return "현재 메인 룬과 같은 계열의 나머지 보조 룬을 모두 얻습니다. 획득한 보조 룬은 이번 원정 동안 유지되며, 이후 메인 룬을 변경해도 잃지 않습니다.";
                case "rare_copy_cards": return "강화되지 않은 카드 중 원본 코스트가 1 이하인 카드 1장을 고릅니다. 같은 종류의 강화되지 않은 카드 2장을 덱에 추가합니다.";
                case "rare_tactical": return "모든 전술 카드 중 무작위로 선택한 카드 1장을 덱에 추가합니다. 각 전술 카드의 등장 확률은 같습니다.";
                default: return "이야기를 나눈 뒤 다음 구역으로 이동합니다.";
            }
        }
    }
}
