#if LUMIA_TEST_RUNNER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Lumia;

public static partial class CoreSmokeTests
{
    static void CardSummaryRegression()
    {
        var catalog = GameDatabase.Cards.Concat(new[] { WildlifeMechanics.Card(WildlifeMechanics.BoarCharge), WildlifeMechanics.Card(WildlifeMechanics.BearSlam) }).ToArray();
        Check(catalog.Length == 417 && catalog.Select(c => c.id).Distinct().Count() == 417, "summary regression covers all 415 playable and both animal-only cards");
        foreach (var source in catalog)
        {
            string before = SummarySnapshot(source);
            var variants = new List<CardDef> { source };
            if (source.category != "wildlife")
            {
                variants.AddRange(StrategyMechanics.Options(source).Select(option => StrategyMechanics.Apply(source, option.id)));
                if (source.category == "skill") variants.Add(RareCardUpgrades.Apply(source));
                if (AdvancedCardMechanics.CanCharge(source)) variants.Add(AdvancedCardMechanics.PreparedCard(source, charge: 1));
                if (AdvancedCardMechanics.IsX(source)) variants.Add(AdvancedCardMechanics.PreparedCard(source, x: 3));
            }
            foreach (var card in variants)
            foreach (bool live in new[] { false, true })
            {
                string copyBefore = SummarySnapshot(card);
                string text = live ? DescriptionSummary.CardPreview(card, GameDatabase.Cards, card.damage > 0 ? 99 : 0, card.block > 0 ? 99 : 0, card.heal > 0 ? 99 : 0)
                    : DescriptionSummary.CardPreview(card, GameDatabase.Cards);
                string label = source.id + (live ? " live99" : " base") + " / " + card.damage + "/" + card.block;
                Check(!string.IsNullOrWhiteSpace(text) && text.EndsWith(".", StringComparison.Ordinal), label + ": complete sentence summary");
                Check(!text.Contains("…") && !text.Contains("...") && !text.Contains("디버프 설명"), label + ": no truncated sentence or full glossary");
                Check(DescriptionSummary.PreviewLines(text) <= DescriptionSummary.CardPreviewMaxLines && DescriptionSummary.PreviewWidth(text) <= DescriptionSummary.CardPreviewMaxWidth,
                    label + ": bounded summary / " + DescriptionSummary.PreviewLines(text) + " lines " + DescriptionSummary.PreviewWidth(text) + " / " + text);
                Check(!Regex.IsMatch(text, @"(?:이면|라면|다면|하면|있으면|없으면)\s*:"), label + ": conditions retain readable sentence clauses");
                if (card.damage > 0 && !AdvancedCardMechanics.IsX(card) && !AdvancedCardMechanics.HasTargetChoice(card) && card.category != "wildlife")
                    Check(Regex.IsMatch(SummarySentences(text)[0], @"^(?:(?:고정 )?피해 \d+|(?:필중 )?공격)"),
                        label + ": the opening sentence preserves the direct attack rather than only a next-basic, delayed or summoned attack");
                if (card.block > 0) Check(ContainsAny(text, "방어", "보호"), label + ": defensive action remains visible");
                if (card.heal > 0) Check(ContainsAny(text, "회복", "치유"), label + ": recovery action remains visible");
                if (card.evasion > 0) Check(text.Contains("회피"), label + ": evasion action remains visible");
                if (card.draw > 0) Check(ContainsAny(text, "뽑", "교환"), label + ": card draw remains visible");
                if (card.energy > 0) Check(ContainsAny(text, "코스트", "에너지"), label + ": usable energy gain remains visible");
                if (card.exhaust) Check(text.Contains("소멸"), label + ": exhaust is never displaced");
                if (AccuracyMechanics.GuaranteedHit(card)) Check(text.Contains("필중"), label + ": guaranteed hit remains visible");
                foreach (var status in card.statuses ?? new CardStatusRule[0])
                    Check(text.Contains(StatusMechanics.Name(status.key)), label + ": actual debuff name remains visible / " + status.key);
                if (card.freeCastCount > 0)
                {
                    Check(ContainsAny(text, "무료", "연계", "재사용") && text.Contains(card.freeCastCount + "회"), label + ": bounded free recast remains visible");
                    Check(text.Contains(card.freeCastNextTurn ? "다음 내 턴" : "이번 턴"), label + ": recast turn remains explicit");
                    foreach (string target in card.freeCastTargets ?? new string[0]) Check(text.Contains(GameDatabase.Card(target)?.key ?? target), label + ": every free target remains visible / " + target);
                }
                if (AdvancedCardMechanics.IsX(card))
                    Check(text.Contains("1당 피해 " + AdvancedCardMechanics.XCoefficient(card)), label + ": X uses original copy coefficient rather than scaled damage or the live override");
                foreach (var rule in card.mechanics?.rules ?? new SkillRule[0])
                {
                    Check(CardSummaryMechanics.Supports(rule.op), label + ": new operations require a summary renderer / " + rule.op);
                    if (rule.op == "consume") Check(text.Contains("소모"), label + ": resource spending remains visible");
                    if (rule.op == "counter") Check(text.Contains("반격"), label + ": retaliation remains visible");
                    if (rule.op == "empower_basic") Check(ContainsAny(text, "기본 공격", "평타"), label + ": next-basic empowerment remains visible");
                    if (rule.op == "revive") Check(ContainsAny(text, "치명", "부활"), label + ": lethal protection remains visible");
                    if (rule.op == "clear_effect") Check(text.Contains("회수"), label + ": effect retrieval remains visible");
                    if (rule.op == "discount") Check(text.Contains(GameDatabase.Card(rule.targetCard)?.key ?? rule.targetCard), label + ": every discounted skill remains visible");
                }
                foreach (bool dense in new[] { false, true })
                    Check(!string.Join(" ", DescriptionSummary.GroupEffects(CardSummaryMechanics.CompactEffects(card, dense: dense))).Contains("소모해"),
                        label + ": generic aggregation never invents a resource-payment cause for a separately guarded bonus");
                Check(SummarySnapshot(card) == copyBefore, label + ": summary does not rewrite actual effects or copied data");
            }
            Check(SummarySnapshot(source) == before, source.id + ": variants and summaries preserve the shared original definition");
        }
        string sua = DescriptionSummary.CardPreview(GameDatabase.Card("sua_w"));
        Check(sua.Contains("선택") && sua.Contains("보호") && sua.Contains("남은 동안") && sua.Contains("실명"), "Sua W exposes selection and the remaining specific protection condition");
        Check(CountSummaryWord(sua, "필중") == 1, "Sua W does not duplicate guaranteed-hit prose");
        string sissela = DescriptionSummary.CardPreview(GameDatabase.Card("sissela_e"));
        Check(sissela.Contains("적을 선택하고"), "Sissela's modifier and recast do not leak into the self-defense branch");
        string twins = DescriptionSummary.CardPreview(GameDatabase.Card("debi_marlene_e"));
        Check(twins.Contains("교대") || twins.Contains("진입하거나 해제"), "twin stance transition is not mistaken for a spent bonus state");
        string chloe = DescriptionSummary.CardPreview(GameDatabase.Card("chloe_r"));
        Check(chloe.Contains("분담 후") && ContainsAny(chloe, "생존", "살아") && chloe.Contains("1턴") && chloe.Contains("체력 1"), "Nina's finite lethal protection requires survival after sharing this hit");
        string garnet = DescriptionSummary.CardPreview(GameDatabase.Card("garnet_r"));
        Check(garnet.Contains("표식") && garnet.Contains("필중") && garnet.Contains("사용 전") && garnet.Contains(FreeCastMechanics.GarnetExecutionPercent + "%") && garnet.Contains("처형"), "Garnet retains independent mark/recast hit and pre-damage execution conditions");
        string cathy = DescriptionSummary.CardPreview(GameDatabase.Card("cathy_q"));
        Check(cathy.Contains("상처") && cathy.Contains("최대") && cathy.Contains("적중") && cathy.Contains("이번 턴"), "Cathy's reset retains the wound maximum transition caused by this hit");
        foreach (string id in new[] { "eleven_q", "eleven_w", "eleven_e", "aiden_w", "felix_r" })
        {
            string charge = DescriptionSummary.CardPreview(GameDatabase.Card(id));
            Check(charge.Contains("충전") && charge.Contains("다음 내 턴") && charge.Contains("무료"), id + ": charging is distinguished from immediate free recasting");
        }
        string aiden = DescriptionSummary.CardPreview(GameDatabase.Card("aiden_e"));
        Check(aiden.Contains("암페어 3") && aiden.Contains("과전하") && aiden.Contains("2로") && !aiden.Contains("암페어를 소모해 피해"),
            "Aiden E retains the separate ampere-to-overcharge gate without making it the cause of the mark bonus");
        string william = DescriptionSummary.CardPreview(GameDatabase.Card("william_e"));
        Check(SummarySentences(william).Any(sentence => sentence.Contains("마운드") && !sentence.Contains("소모"))
            && SummarySentences(william).Any(sentence => sentence.Contains("바닥 공") && sentence.Contains("소모") && !sentence.Contains("마운드")),
            "William E always collects the floor balls independently of the conditional mound defense");
        foreach (string id in new[] { "silvia_q", "silvia_w", "silvia_e" })
        {
            string bike = DescriptionSummary.CardPreview(GameDatabase.Card(id));
            Check(SummarySentences(bike).Any(sentence => sentence.Contains("연료 0") && sentence.Contains("해제")) && !bike.Contains("상태를 소모해"),
                id + ": dismounting keeps the separate empty-fuel guard rather than becoming every attack's payment");
        }
        foreach (string id in new[] { "coraline_q", "coraline_e" })
        {
            string mirror = DescriptionSummary.CardPreview(GameDatabase.Card(id));
            Check(mirror.Contains("거울 조각 표식") && ContainsAny(mirror, "다시 부여", "갱신") && !mirror.Contains("진입하거나 해제"),
                id + ": same-hit clear then apply refreshes the mark rather than toggling it");
        }
        foreach (string id in new[] { "theodore_q", "blair_q", "blair_w" })
            Check(!DescriptionSummary.CardPreview(GameDatabase.Card(id)).Contains("상태를 소모해"), id + ": a state exit is not inferred to pay for unrelated bonuses");
        string elena = DescriptionSummary.CardPreview(GameDatabase.Card("elena_q"));
        Check(SummarySentences(elena).Any(sentence => sentence.Contains("설치") && !sentence.Contains("적중")),
            "Elena Q installs the icicle independently of whether the direct attack hits");
        string arda = DescriptionSummary.CardPreview(GameDatabase.Card("arda_w"));
        Check(SummarySentences(arda).Any(sentence => sentence.Contains("회복") && !sentence.Contains("입방체")),
            "Arda W's immediate resource recovery is independent of the later cube hit");
        string isaac = DescriptionSummary.CardPreview(GameDatabase.Card("isaac_e"));
        Check(isaac.Contains("준비된 E") && isaac.Contains("모두 파괴"), "Isaac's prepared shield break belongs to an E hit and destroys all block");
        string niaR = DescriptionSummary.CardPreview(GameDatabase.Card("nia_r"));
        Check(niaR.StartsWith("공격·방어", StringComparison.Ordinal) && SummarySentences(niaR).Any(sentence => sentence.Contains("기절") && !sentence.Contains("턴 끝")),
            "Nia R retains its immediate attack and defense, and the cast stun does not migrate to the summoned attack");
    }
    static string[] SummarySentences(string text) => text.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
    static bool ContainsAny(string text, params string[] words) => words.Any(text.Contains);
    static int CountSummaryWord(string text, string word) => (text.Length - text.Replace(word, "").Length) / word.Length;
    static string SummarySnapshot(object value)
    {
        if (value == null) return "null";
        if (value is string) return "s:" + value;
        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum || value is decimal) return type.Name + ":" + Convert.ToString(value, CultureInfo.InvariantCulture);
        if (value is IEnumerable) return "[" + string.Join(",", ((IEnumerable)value).Cast<object>().Select(SummarySnapshot)) + "]";
        return type.Name + "{" + string.Join("|", type.GetFields().OrderBy(f => f.Name).Select(f => f.Name + "=" + SummarySnapshot(f.GetValue(value)))) + "}";
    }
}
#endif
