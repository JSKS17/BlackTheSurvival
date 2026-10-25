using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lumia
{
    // Skin stories stay in their own catalog. This final pass changes only rare rewards.
    public static class RareEncounterRules
    {
        public const int BonusCredits = 0;
        public const int MinimumHealing = 28;
        public const int HealingPremium = 8;
        public const int MinimumCredits = 55;
        public const int CreditPremium = 10;

        static readonly HashSet<string> Removal = Set("yuki aya daniel laura rozzi shoichi alex william jenny camilo cathy tazia fiora haze abigail katja hisui justyna xuelin blair lucia ceres craver");
        static readonly HashSet<string> Support = Set("nia hyejin sua emma charlotte nathapon tia vanya bianca adela adina elena johann irem eva zahir chiara priya hart arda leni lenore yumin");
        static readonly HashSet<string> Copy = Set("isol nadine nicky mai barbara bernice celine jan aiden eleven chloe theodore piolo tsubame henry mirka coraline bihyung fenrir isaac echion ian sissela");
        sealed class Narrative { public string label, description; }
        static readonly Dictionary<string, Narrative[]> OriginalChoices = new Dictionary<string, Narrative[]>();
        static HashSet<string> Set(string ids) => new HashSet<string>(ids.Split(' '), StringComparer.Ordinal);
        public static string ThirdEffect(string subjectId) => Removal.Contains(subjectId) ? "rare_remove_cards"
            : Support.Contains(subjectId) ? "rare_support_runes" : Copy.Contains(subjectId) ? "rare_copy_cards" : "rare_tactical";

        public static void Configure(IEnumerable<EventDef> encounters)
        {
            foreach (var encounter in encounters)
            {
                if (ResearchEncounters.SubjectForEvent(encounter.id) == "lumi") continue;
                var subject = GameDatabase.Character(ResearchEncounters.SubjectForEvent(encounter.id));
                if (subject == null) throw new InvalidOperationException("Unknown rare encounter: " + encounter.id);
                var skills = (subject.cards ?? new string[0]).Select(GameDatabase.Card).Where(card => card != null).ToArray();
                var representative = (encounter.options ?? new EventOption[0]).Select(option => GameDatabase.Card(option.cardId))
                    .FirstOrDefault(card => card != null && skills.Any(own => own.id == card.id) && (card.key == "Q" || card.key == "W" || card.key == "E"))
                    ?? skills.FirstOrDefault(card => card.key == "Q" || card.key == "W" || card.key == "E");
                var ultimate = skills.FirstOrDefault(card => card.key == "R");
                var passive = GameDatabase.Passive(subject.passiveId);
                if (representative == null || ultimate == null || passive == null) throw new InvalidOperationException("Incomplete rare rewards: " + subject.id);
                if (!OriginalChoices.ContainsKey(encounter.id)) OriginalChoices.Add(encounter.id, encounter.options.Select(option => new Narrative { label = option.label, description = option.description }).ToArray());
                var original = OriginalChoices[encounter.id];
                var first = Option(original[0].label + " · ++ 구입", original[0].description, "rare_trade_card", RareCardUpgrades.Price(representative), representative.id);
                bool passiveReward = Support.Contains(subject.id) || Copy.Contains(subject.id);
                var second = passiveReward
                    ? Option("고유 능력의 비결을 배운다", subject.name + "의 경험에서 고유 능력을 익힙니다.", "passive", passiveId: passive.id)
                    : Option("필살기의 비결을 배운다", subject.name + "에게 필살기를 배웁니다.", "card", cardId: ultimate.id);
                string thirdEffect = ThirdEffect(subject.id);
                EventOption third;
                switch (thirdEffect)
                {
                    case "rare_remove_cards": third = Option("불필요한 동작을 함께 정리한다", subject.name + "와 불필요한 동작을 골라 덱을 정리합니다.", thirdEffect); break;
                    case "rare_support_runes": third = Option("룬의 남은 가능성을 깨운다", subject.name + "와 현재 메인 룬 계열의 보조 능력을 모두 익힙니다.", thirdEffect); break;
                    case "rare_copy_cards": third = Option("익숙한 기술의 반복을 배운다", subject.name + "와 이미 익힌 간단한 기술을 반복 연습합니다.", thirdEffect); break;
                    default: third = Option("새로운 전술에 도전한다", subject.name + "와 뜻밖의 상황에 대비할 전술을 배웁니다.", thirdEffect); break;
                }
                PreserveNarrative(second, original[1], second.effect == "passive" ? "패시브 습득" : "R 습득");
                string thirdLabel = thirdEffect == "rare_remove_cards" ? "최대 2장 제거" : thirdEffect == "rare_support_runes" ? "보조 룬 습득" : thirdEffect == "rare_copy_cards" ? "2장 복제" : "무작위 F 습득";
                PreserveNarrative(third, original[2], thirdLabel);
                encounter.options = new[] { first, second, third };
                TacticalEncounterOptions.ConfigureRare(encounter, subject.id);
            }
        }

        static EventOption Option(string label, string description, string effect, int amount = 0, string cardId = null, string passiveId = null)
            => new EventOption { label = label, description = description, effect = effect, amount = amount, cardId = cardId, passiveId = passiveId, bonusCredits = 0 };
        static void PreserveNarrative(EventOption option, Narrative original, string rewardLabel)
        {
            string action = original.label ?? "";
            bool compatible = option.effect == "rare_remove_cards"
                ? Regex.IsMatch(action, "정리|지우|버린|비운|덜어|줄인|내려놓|고친")
                : Regex.IsMatch(action, "배운|익힌|점검|확인|맞춘|읽|살핀|검토|기록|연습|재현|살펴|분석|조율|조정|측정|추적|듣|동작|자세|박자|원리|실험|신호|연결|타이밍|오차|빈틈");
            if (compatible) option.label = action;
            option.label += " · " + rewardLabel;
            // Keep thematic details when they do not promise the former reward.
            if (compatible && !Regex.IsMatch(original.description ?? "", "크레딧|회복|치료|응급|만년|수프|고기|간식|재료|운석|생명의 나무|미스릴|보수를|정산|지불|카드|덱|패를")) option.description = original.description;
        }
        public static string BonusSummary(EventOption option) => "";
    }
}
