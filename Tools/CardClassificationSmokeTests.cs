#if LUMIA_TEST_RUNNER
using System;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    private static void CardClassificationCanonicalRules()
    {
        // The whole real catalog must present the exact predicates used by status costs
        // and boss challenges, including conditional and next-basic debuff preparations.
        foreach (var card in GameDatabase.Cards)
        {
            var tags = CardPresentation.ClassificationLabel(card).Split(new[] { " · " }, StringSplitOptions.None);
            Check(tags.Contains("이동") == card.movement, "movement label matches canonical flag: " + card.id);
            Check(tags.Contains("디버프") == BossMechanics.IsDebuffSkill(card), "debuff label matches boss predicate: " + card.id);
            Check(tags.Length <= 3 && tags.Distinct().Count() == tags.Length, "classification stays compact and unique: " + card.id);
            var rules = CardPresentation.ClassificationRules(card);
            Check(rules.Contains("이동 기술 " + (card.movement ? "해당" : "해당 없음"))
                && rules.Contains("디버프 기술 " + (BossMechanics.IsDebuffSkill(card) ? "해당" : "해당 없음")),
                "details explicitly report both boss predicates: " + card.id);
        }

        foreach (string id in new[] { "nia_q", "celine_q", "celine_r", "adela_q", "adela_w", "theodore_w", "katja_w", "hyejin_w", "cathy_r" })
            Check(CardPresentation.ClassificationLabel(GameDatabase.Card(id)).Contains("설치"), "physical field resource or effect is an installation: " + id);
        foreach (string id in new[] { "nia_w", "aya_e", "yuki_r", "magnus_w", "jackie_r", "katja_r", "theodore_q" })
            Check(!CardPresentation.ClassificationLabel(GameDatabase.Card(id)).Contains("설치"), "personal resource, delayed attack or body attack is not an installation: " + id);

        var mixed = new CardDef { id = "nia_q", key = "Q", movement = true, weak = 1,
            mechanics = new SkillMechanicProfile { rules = new[] { new SkillRule { op = "gain", key = "blocks", amount = 1 } } } };
        Check(CardPresentation.ClassificationLabel(mixed) == "이동 · 설치 · 디버프", "all three independent classifications are preserved in stable order");
        string mixedRules = CardPresentation.ClassificationRules(mixed);
        Check(mixedRules.Contains("속박") && mixedRules.Contains("둔화") && mixedRules.Contains("끌어당김")
            && mixedRules.Contains("조건부") && mixedRules.Contains("예약") && mixedRules.Contains("직접 사용하는 시점"),
            "classification details explain status restrictions and conditional manual-use boss checks");

        var proseOnly = new CardDef { id = "fixture_q", key = "Q", name = "이동 설치 디버프", description = "소환하고 이동합니다.", evasion = 99 };
        Check(CardPresentation.ClassificationLabel(proseOnly) == "일반", "names, description and evasion cannot invent gameplay classifications");
        var meter = new CardDef { id = "echion_q", key = "Q", mechanics = new SkillMechanicProfile {
            rules = new[] { new SkillRule { op = "gain", key = "blocks", amount = 1 }, new SkillRule { op = "gain", key = "vf", amount = 3 } } } };
        Check(CardPresentation.ClassificationLabel(meter) == "일반", "installation keys are subject-scoped and personal VF is not a field object");
        mixed.movement = false; mixed.weak = 0;
        mixed.mechanics.rules = new[] { new SkillRule { op = "consume", key = "blocks", amount = 1 }, new SkillRule { op = "set", key = "blocks", amount = 0 } };
        Check(CardPresentation.ClassificationLabel(mixed) == "일반", "consuming or clearing an installation does not create a new one");
        mixed.mechanics.rules = new[] { new SkillRule { op = "bonus_damage", key = "blocks", amount = 5 }, null };
        Check(CardPresentation.ClassificationLabel(mixed) == "일반", "referencing a field resource for a bonus is not placement");
        mixed.mechanics.rules = new[] { new SkillRule { op = "state_on", key = "blocks", amount = 1, conditionKey = "fixture" } };
        Check(CardPresentation.ClassificationLabel(mixed) == "설치", "binary and conditional field preparations retain installation identity");

        var basic = new CardDef { id = "basic_attack", key = "ATK", category = "basic", poison = 2 };
        Check(CardPresentation.ClassificationLabel(basic) == "기본 공격" && !BossMechanics.IsDebuffSkill(basic), "basic poison still follows boss exclusion of basic cards");
        Check(CardPresentation.ClassificationLabel(GameDatabase.Card("basic_guard")) == "방어", "basic defense fallback is explicit");
        Check(CardPresentation.ClassificationLabel(new CardDef { key = "D" }) == "무기", "unclassified D uses weapon fallback");
        Check(CardPresentation.ClassificationLabel(new CardDef { key = "F" }) == "전술", "unclassified F uses tactical fallback");
        Check(CardPresentation.ClassificationLabel(null) == "일반" && CardPresentation.ClassificationRules(null).Contains("해당 없음"), "null definitions are safe in both presentation APIs");
    }
}
#endif
