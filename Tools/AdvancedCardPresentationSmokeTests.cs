#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    static void AdvancedCardPresentation()
    {
        string[] cards = { "sissela_e", "adina_r", "eleven_q", "eleven_w", "eleven_e", "aiden_w", "felix_r",
            "tazia_r", "echion_w", "eva_r", "sho_r", "sissela_r", "chiara_w", "lenox_q",
            "chloe_q", "chloe_w", "chloe_e", "chloe_r" };
        var coreTerms = new Dictionary<string, string[]> {
            {"sissela_e",new[]{"자신","적","기절"}}, {"adina_r",new[]{"교환"}},
            {"eleven_q",new[]{"충전","버거"}}, {"eleven_w",new[]{"충전","도발"}}, {"eleven_e",new[]{"충전","버거"}},
            {"aiden_w",new[]{"충전","속박"}}, {"felix_r",new[]{"충전","기절"}},
            {"tazia_r",new[]{"손패","소멸"}}, {"echion_w",new[]{"손패","소멸"}},
            {"eva_r",new[]{"코스트"}}, {"sho_r",new[]{"코스트"}}, {"sissela_r",new[]{"체력","피해"}},
            {"chiara_w",new[]{"방어도","피해"}}, {"lenox_q",new[]{"최대 체력","피해"}},
            {"chloe_q",new[]{"니나","소환"}}, {"chloe_w",new[]{"니나","협공"}},
            {"chloe_e",new[]{"니나","다음","무료"}}, {"chloe_r",new[]{"니나","1턴","생존"}}
        };
        foreach (string id in cards)
        {
            var card = GameDatabase.Card(id);
            string preview = DescriptionSummary.CardPreview(card, GameDatabase.Cards);
            Check(!string.IsNullOrEmpty(preview) && DescriptionSummary.PreviewWidth(preview) <= DescriptionSummary.CardPreviewMaxWidth && DescriptionSummary.PreviewLines(preview) <= DescriptionSummary.CardPreviewMaxLines,
                "advanced card summary remains within the readable hand-card budget: " + id);
            Check(coreTerms[id].All(preview.Contains),
                "the new defining mechanic is visible before opening details: " + id);
            string full = CardPresentation.Describe(card, GameDatabase.Cards);
            string metadata = AdvancedCardMechanics.Description(card);
            if (metadata.Length == 0) metadata = CompanionMechanics.Description(card);
            Check(full.Contains(DescriptionSummary.Normalize(metadata)), "full card text retains the complete new mechanic: " + id);
        }
        foreach (string id in new[] { "eva_r", "sho_r" })
        {
            var card = GameDatabase.Card(id);
            string full = CardPresentation.Describe(card, GameDatabase.Cards), preview = DescriptionSummary.CardPreview(card, GameDatabase.Cards);
            Check(full.Contains("X 코스트") && preview.Contains("남은 코스트를 모두"), "X descriptions explain the current-energy commitment: " + id);
            Check(!full.StartsWith("적에게 피해", StringComparison.Ordinal), "X card text does not promise one fixed unscaled attack: " + id);
        }
        foreach (string id in new[] { "aiden_w", "felix_r" })
        {
            var card = GameDatabase.Card(id);
            string kind = id == "aiden_w" ? "root" : "stun";
            Check(!card.statuses.Any(x => x.key == kind) && !CardPresentation.DebuffKinds(card).Contains(kind),
                "charge-only control is documented without becoming an immediate effect: " + id);
            string rules = CardPresentation.Rules(card);
            Check(rules.Contains("디버프 설명") && rules.LastIndexOf(StatusMechanics.Name(kind) + ":", StringComparison.Ordinal) > rules.IndexOf("디버프 설명", StringComparison.Ordinal),
                "charge-only debuff explanation remains at the bottom of the full rules: " + id);
        }
        var felix = GameDatabase.Card("felix_r");
        Check(!BossMechanics.IsDebuffSkill(AdvancedCardMechanics.PreparedCard(felix))
            && BossMechanics.IsDebuffSkill(AdvancedCardMechanics.PreparedCard(felix, charge: 1)),
            "Felix R boss-debuff classification follows its actual immediate or fully charged form");
        string target = DescriptionSummary.CardPreview(GameDatabase.Card("sissela_e"), GameDatabase.Cards);
        Check((target.Contains("선택") || target.Contains("또는")) && target.Contains("자신") && target.Contains("방어") && target.Contains("적") && target.Contains("피해"),
            "Sissela's card face distinguishes mutually exclusive friendly and hostile targets");
        foreach (string id in new[] { "chloe_q", "chloe_w", "chloe_e", "chloe_r" })
            Check(CompanionMechanics.CardSummary(GameDatabase.Card(id)).EndsWith("니다.", StringComparison.Ordinal),
                "Nina card summaries retain complete sentence prose: " + id);
    }
}
#endif
