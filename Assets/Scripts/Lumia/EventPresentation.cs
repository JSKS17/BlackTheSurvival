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
                    option.description = option.description + " " + RewardSummary(option);
        }

        public static CardDef RewardCard(EventOption option)
        {
            if (option == null || (option.effect != "card" && option.effect != "trade_card" && option.effect != "risky_card")) return null;
            return GameDatabase.Card(option.cardId);
        }

        public static string RewardTitle(EventOption option)
        {
            var card = RewardCard(option);
            return card == null ? "" : card.owner + " · " + card.name + " [" + card.key + "] · " + card.cost + "코스트";
        }

        public static string RewardSummary(EventOption option)
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
                case "risky_card": return "체력 " + option.amount + "을 소모하고 " + cardName + " 1장을 덱에 추가합니다.";
                case "heal": return "체력을 " + option.amount + " 회복합니다.";
                case "credits": return "크레딧 " + option.amount + "을 획득합니다.";
                case "damage": return "체력을 " + option.amount + " 소모합니다.";
                case "max_health": return "최대 체력이 " + option.amount + " 증가합니다.";
                case "object": return material + " 1개를 획득합니다.";
                case "trade_object": return "크레딧 " + option.amount + "을 지불하고 " + material + " 1개를 획득합니다.";
                case "risky_object": return "체력 " + option.amount + "을 소모하고 " + material + " 1개를 획득합니다.";
                case "food": return (food?.name ?? "지정 음식") + " 1개를 가방에 넣습니다. 사용하면 " + (food != null && food.fullHeal ? "체력을 모두 회복합니다." : "체력을 " + (food?.heal ?? 0) + " 회복합니다.");
                case "passive": return (GameDatabase.Passive(option.passiveId)?.name ?? "지정 패시브") + " 패시브를 획득합니다.";
                case "rune_change": case "swap_rune": return "메인 룬과 같은 계열의 보조 룬을 새로 선택합니다.";
                case "remove_card": return "무기 카드 이외의 무작위 카드 1장을 제거합니다. 덱에는 최소 5장을 남깁니다.";
                case "upgrade_card": return "무기 카드 이외의 강화되지 않은 무작위 카드 1종을 강화합니다. 피해·방어도는 3, 회복은 2 증가합니다.";
                default: return "이야기를 나눈 뒤 다음 구역으로 이동합니다.";
            }
        }
    }
}
