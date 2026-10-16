using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lumia
{
    public sealed class ConditionEffect
    {
        public string key, condition, effect, note;
    }

    /// <summary>Brief card previews and complete quantitative effect digests.</summary>
    public static class DescriptionSummary
    {
        public const int CardPreviewMaxLines = 8;
        public const int CardPreviewMaxWidth = 108;
        // Keep activation data separate from Korean prose. A matching first condition
        // alone must never merge a hit, miss, delayed hit or a different resource check.
        public static string ConditionKey(SkillRule rule, string timing = "cast", bool? onHit = null)
        {
            var checks = new List<string>();
            if (!string.IsNullOrEmpty(rule.conditionKey)) checks.Add(rule.conditionKey.Length + ":" + rule.conditionKey + (rule.conditionExact ? "=" : ">=") + rule.conditionAmount);
            if (!string.IsNullOrEmpty(rule.conditionKey2)) checks.Add(rule.conditionKey2.Length + ":" + rule.conditionKey2 + (rule.conditionExact2 ? "=" : ">=") + rule.conditionAmount2);
            checks.Sort(StringComparer.Ordinal);
            return timing + "|" + (onHit ?? rule.onHit) + "|" + (rule.conditionPrevious ?? "") + "|" + string.Join("&", checks.Distinct());
        }

        public static string JoinEffects(IEnumerable<string> effects)
        {
            var clauses = (effects ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().TrimEnd('.')).Distinct().ToList();
            var endings = new[] { "증가시킵니다", "만듭니다", "바꿉니다", "줍니다", "얻습니다", "뽑습니다", "늘립니다", "줄입니다", "높입니다", "남깁니다", "쌓습니다", "있습니다", "합니다", "됩니다" };
            var joins = new[] { "증가시키고", "만들고", "바꾸고", "주고", "얻고", "뽑고", "늘리고", "줄이고", "높이고", "남기고", "쌓고", "있고", "하고", "되고" };
            for (int i = 0; i < clauses.Count - 1; ++i)
            {
                // A per-effect restriction stays attached to its own verb.
                for (int j = 0; j < endings.Length; ++j)
                {
                    var match = Regex.Match(clauses[i], Regex.Escape(endings[j]) + @"(?<notes>(?:\s*\([^\n]*\))*)$");
                    if (!match.Success) continue;
                    clauses[i] = clauses[i].Substring(0, match.Index) + joins[j] + match.Groups["notes"].Value;
                    break;
                }
            }
            return string.Join(", ", clauses);
        }

        public static string JoinConditions(IEnumerable<string> conditions)
        {
            var clauses = conditions.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            for (int i = 0; i < clauses.Count - 1; ++i)
            {
                if (clauses[i].EndsWith("이면", StringComparison.Ordinal)) clauses[i] = clauses[i].Substring(0, clauses[i].Length - 2) + "이고";
                else if (clauses[i].EndsWith("아니면", StringComparison.Ordinal)) clauses[i] = clauses[i].Substring(0, clauses[i].Length - 3) + "아니고";
            }
            return string.Join(" ", clauses);
        }

        public static List<string> GroupEffects(IEnumerable<ConditionEffect> effects)
        {
            var groups = new List<List<ConditionEffect>>();
            foreach (var effect in effects ?? Enumerable.Empty<ConditionEffect>())
            {
                if (effect == null || string.IsNullOrWhiteSpace(effect.effect)) continue;
                var group = groups.FirstOrDefault(x => x[0].key == effect.key);
                if (group == null) { group = new List<ConditionEffect>(); groups.Add(group); }
                group.Add(effect);
            }
            return groups.Select(group => ConditionPrefix(group[0].condition)
                + JoinEffects(group.Select(x => x.effect)) + "."
                + (group.Any(x => !string.IsNullOrWhiteSpace(x.note)) ? " " + string.Join(" ", group.Select(x => x.note).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()) : ""))
                .Select(Normalize).ToList();
        }

        public static string ConditionPrefix(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition)) return "";
            string text = condition.Trim().TrimEnd(':', '：', ',', ' ');
            string[] endings = { "적중하면", "적중했다면", "했다면", "아니라면", "있으면", "있다면", "없으면", "없다면", "아니면", "이하면", "낮으면", "높으면", "같으면", "많으면", "적으면", "된다면", "이라면", "이면", "라면", "하면" };
            string[] replacements = { "적중했을 때", "적중했을 때", "했을 때", "아닐 때", "있을 때", "있을 때", "없을 때", "없을 때", "아닐 때", "이하일 때", "낮을 때", "높을 때", "같을 때", "많을 때", "적을 때", "될 때", "일 때", "일 때", "일 때", "할 때" };
            for (int i = 0; i < endings.Length; i++)
                if (text.EndsWith(endings[i], StringComparison.Ordinal))
                { text = text.Substring(0, text.Length - endings[i].Length) + replacements[i]; break; }
            return text + ", ";
        }

        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            // Resources are values. Cards, attacks and equipment retain their real counting units.
            text = Regex.Replace(text, @"(\d+)\s*개\s*쌓습니다", "$1 증가시킵니다");
            text = Regex.Replace(text, @"(\d+)\s*개로\s*(바꿉니다|합니다|설정합니다)", "$1로 만듭니다");
            text = Regex.Replace(text, @"(\d+)\s*개(?=마다|이면| 이상|\)|\.|로)", "$1");
            text = Regex.Replace(text, @"(\d+)\s*개\s+소모", "$1 소모");
            text = Regex.Replace(text, @"(\d+)로(?= 만(?:듭니다|들고))", m=>m.Groups[1].Value+("036".IndexOf(m.Groups[1].Value.Last())>=0?"으로":"로"));
            text = Regex.Replace(text, @"(\d+)의 (추가 )?피해를", m => CardPresentation.WithParticle(m.Groups[2].Value + "피해 " + m.Groups[1].Value, "를", "을"));
            text = text.Replace("이면 적중하면", "이고 공격이 적중하면").Replace("이면 사용하면", "이면")
                .Replace("이면, 공격이 적중하면", "이고 공격이 적중하면").Replace("이면, 적중하면", "이고 적중하면")
                .Replace("아니면, 공격이 적중하면", "아니고 공격이 적중하면").Replace("아니면, 적중하면", "아니고 적중하면");
            text = Regex.Replace(text, @"(?<condition>[^\n:.]+(?:이면|라면|다면|하면|있으면|없으면|아니면))\s*:\s*",
                match => ConditionPrefix(match.Groups["condition"].Value));
            text = Regex.Replace(text, @"[^\S\n]+", " ");
            return text.Trim();
        }

        public static List<string> CleanLines(IEnumerable<string> lines)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string raw in lines ?? Enumerable.Empty<string>())
            {
                foreach (var line in Normalize(raw).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    // Keep a repeated effect with a different condition; remove exact repeated prose only.
                    string key = Regex.Replace(line, @"\s+", " ").Trim().TrimEnd('.', '。');
                    if (key.Length > 0 && seen.Add(key)) result.Add(key + ".");
                }
            }
            return result;
        }

        public static string Card(CardDef card, List<CardDef> all = null, int damage = -1, int block = -1, int heal = -1)
        {
            if (card == null) return "";
            if (AdvancedCardMechanics.HasTargetChoice(card)) return string.Join("\n", TargetChoiceLines(card));
            int d = damage < 0 ? card.damage : damage;
            int b = block < 0 ? card.block : block;
            int h = heal < 0 ? card.heal : heal;
            var lines = new List<string>();
            var immediate = new List<string>();
            if (d > 0 && !AdvancedCardMechanics.IsX(card)) immediate.Add(card.hits > 1 ? CardPresentation.WithParticle($"피해 {d}","를","을")+$" {card.hits}회 줍니다" : CardPresentation.WithParticle($"피해 {d}","를","을")+" 줍니다");
            if (b > 0) immediate.Add(CardPresentation.WithParticle($"방어도 {b}","를","을")+" 얻습니다");
            if (h > 0) immediate.Add(CardPresentation.WithParticle($"체력 {h}","를","을")+" 회복합니다");
            if (card.draw > 0) immediate.Add($"카드 {card.draw}장을 뽑습니다");
            if (card.energy > 0) immediate.Add(CardPresentation.WithParticle($"이번 턴 코스트 {card.energy}","를","을")+" 회복합니다");
            for (int i = 0; i < immediate.Count; i += 3)
                lines.Add(string.Join(". ", immediate.Skip(i).Take(3)) + ".");
            if (card.evasion > 0) lines.Add($"{card.duration}턴 동안 회피율이 {card.evasion}% 증가합니다.");
            if (card.poison > 0) lines.Add($"적에게 중독 {card.poison}을 부여합니다.");
            if (card.weak > 0) lines.Add($"적에게 약화를 {card.weak}턴 부여합니다.");
            if (card.vulnerable > 0) lines.Add($"적에게 취약을 {card.vulnerable}턴 부여합니다.");
            if (card.strength > 0) lines.Add($"이번 전투의 힘이 {card.strength} 증가합니다.");
            var effects = SkillMechanics.DescriptionEffects(card).Concat(StatusMechanics.DescriptionEffects(card)).ToList();
            string freeCastNote = "";
            if (card.freeCastCount > 0 && card.freeCastTargets != null && card.freeCastTargets.Length > 0)
            {
                effects.Add(FreeCastMechanics.Effect(card));
                freeCastNote=(card.freeCastNextTurn ? "다음 내 턴 시작에, " : "") + "대상 카드가 손패에 없으면 덱·버린 카드에서 가져옵니다(덱에 보유한 카드만).";
            }
            lines.AddRange(GroupEffects(effects));
            if (freeCastNote.Length > 0) lines.Add(freeCastNote);
            lines.AddRange(CrossSubjectSynergies.FullDescription(card));
            string advanced = AdvancedCardMechanics.Description(card), companion = CompanionMechanics.Description(card);
            if (!string.IsNullOrEmpty(advanced)) lines.Add(advanced);
            if (!string.IsNullOrEmpty(companion)) lines.Add(companion);
            if (card.exhaust) lines.Add(FreeCastMechanics.ExhaustDescription(card));
            if (card.id == "basic_attack") lines.Add("장비 공격력과 기본 공격 강화가 적용됩니다. 기본 치명타 피해는 150%이며, 장비로 증가합니다.");
            if (lines.Count == 0) lines.Add("추가 효과 없이 행동합니다.");
            return string.Join("\n", CleanLines(lines));
        }

        // Targeted defense and attack are alternatives. Do not print their base
        // numbers as simultaneous unconditional effects before the branch rules.
        public static List<string> TargetChoiceLines(CardDef card)
        {
            var lines = new List<string> { AdvancedCardMechanics.Description(card) };
            string accuracy = AccuracyMechanics.Description(card);
            if (!string.IsNullOrEmpty(accuracy)) lines.Add("적을 선택했을 때, " + accuracy);
            var effects = SkillMechanics.DescriptionEffects(card).ToList();
            if (card.freeCastCount > 0 && card.freeCastTargets != null && card.freeCastTargets.Length > 0)
                effects.Add(FreeCastMechanics.Effect(card));
            foreach (var effect in effects)
                effect.condition = string.IsNullOrEmpty(effect.condition) ? "적을 선택했을 때" : "적을 선택했으며 " + effect.condition;
            lines.AddRange(GroupEffects(effects));
            if (card.freeCastCount > 0)
                lines.Add("적을 선택했을 때, 덱에 보유한 연계 대상 카드가 손패에 없으면 뽑을 카드 또는 버린 카드에서 한 장을 가져옵니다.");
            lines.AddRange(CrossSubjectSynergies.FullDescription(card));
            if (card.exhaust) lines.Add(FreeCastMechanics.ExhaustDescription(card));
            return CleanLines(lines);
        }

        // Every core behavior survives compacting. Complex condition tables are summarized
        // by behavior family; no rule is dropped because it ranks below a fixed part count.
        public static string CardPreview(CardDef card, List<CardDef> all = null, int damage = -1, int block = -1, int heal = -1)
        {
            return CardSummaryMechanics.Text(card, damage, block, heal);
        }

        // Korean glyphs use almost twice the width of Latin digits in the game's pixel font.
        // This budget corresponds to about five lines in the narrowest hand card.
        public static double PreviewWidth(string text)
        {
            return (text ?? "").Sum(ch=>ch=='\n' ? 0 : ch <= 127 ? .55 : 1.0);
        }

        // Pixel advances from the bundled Galmuri11.ttf at size 11. The minimum card body
        // is 144 px wide; five lines fit its 76 px height. A 10% margin also covers word
        // wrapping and the native GUI's glyph rounding. Native QA verifies every card.
        static readonly int[] PreviewAsciiAdvances = {
            5,4,4,8,8,9,8,4,4,4,6,8,4,6,4,6,8,6,8,7,8,8,8,8,8,8,4,4,5,6,5,6,
            9,8,8,8,8,6,6,8,8,4,7,7,6,9,8,8,8,8,8,8,8,8,8,9,8,8,7,4,6,4,6,6,
            4,7,8,6,8,7,6,8,7,4,6,7,4,10,7,8,8,8,6,7,6,8,8,9,8,8,6,5,4,5,7
        };
        public static int PreviewLines(string text)
        {
            return (text ?? "").Split('\n').Sum(line=>Math.Max(1,(int)Math.Ceiling(line.Sum(ch=>ch>=32&&ch<=126?PreviewAsciiAdvances[ch-32]:ch=='·'?4:ch=='×'||ch=='−'?7:11)*1.1/144)));
        }
        public static string Trait(TraitMechanicProfile profile, string fallback = "")
        {
            return profile == null ? Normalize(fallback) : TraitPresentation.Summary(profile);
        }
        public static string Passive(PassiveDef passive) => passive == null ? "" : Trait(passive.mechanics, passive.description);
        public static string Rune(RuneDef rune) => rune == null ? "" : Trait(rune.mechanics, rune.description);
        public static string Gear(GearDef gear)
        {
            if (gear == null) return "";
            var lines = new List<string>();
            var stats = new List<string>();
            if (gear.attack > 0) stats.Add("공격력이 " + gear.attack + " 증가합니다");
            if (gear.block > 0) stats.Add(CardPresentation.WithParticle("매 턴 방어도 " + gear.block,"를","을")+" 얻습니다");
            if (gear.health > 0) stats.Add("최대 체력이 " + gear.health + " 증가합니다");
            if (gear.evasion > 0) stats.Add("회피율이 " + gear.evasion + "% 증가합니다");
            if (gear.critChance > 0) stats.Add("일반 공격의 치명타 확률이 " + gear.critChance + "% 증가합니다");
            if (gear.critDamageBonus > 0) stats.Add("일반 공격의 치명타 피해 배율이 " + gear.critDamageBonus + "%p 증가합니다");
            if (stats.Count > 0) lines.Add(string.Join(". ", stats) + ".");
            if (gear.mechanics?.rules?.Length > 0) lines.Add(Trait(gear.mechanics));
            if (gear.controlResistance > 0) lines.Add("군중 제어로 줄어드는 코스트를 " + gear.controlResistance + " 줄입니다.");
            if (gear.damageDeferral > 0) lines.Add("방어도로 막은 뒤 체력 피해의 " + gear.damageDeferral + "%를 다음 3턴 시작에 나눠 받습니다. 미룬 피해는 방어도를 무시하며 전투 종료 시 사라집니다.");
            if (gear.slot == GearSlot.Weapon) lines.Add("장착하면 " + (GameDatabase.Card(gear.cardId)?.name ?? gear.cardId) + " D 카드 1장이 덱에 자동으로 추가됩니다.");
            lines.Add("같은 장비의 특수 효과는 중첩되지 않습니다.");
            return string.Join("\n", CleanLines(lines));
        }
    }
}
