using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lumia
{
    /// <summary>Compact effect text shared by previews and the detail panel's summary tab.</summary>
    public static class DescriptionSummary
    {
        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            // Resources are values. Cards, attacks and equipment retain their real counting units.
            text = Regex.Replace(text, @"(\d+)\s*개\s*쌓습니다", "$1 증가시킵니다");
            text = Regex.Replace(text, @"(\d+)\s*개로\s*(바꿉니다|합니다|설정합니다)", "$1로 만듭니다");
            text = Regex.Replace(text, @"(\d+)\s*개(?=마다|이면| 이상|\)|\.|로)", "$1");
            text = Regex.Replace(text, @"(\d+)\s*개\s+소모", "$1 소모");
            text = Regex.Replace(text, @"(\d+)의 (추가 )?피해를", m => CardPresentation.WithParticle(m.Groups[2].Value + "피해 " + m.Groups[1].Value, "를", "을"));
            text = text.Replace("이면 적중하면", "이고 공격이 적중하면").Replace("이면 사용하면", "이면");
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
            int d = damage < 0 ? card.damage : damage;
            int b = block < 0 ? card.block : block;
            int h = heal < 0 ? card.heal : heal;
            var lines = new List<string>();
            var immediate = new List<string>();
            if (d > 0) immediate.Add(card.hits > 1 ? CardPresentation.WithParticle($"피해 {d}","를","을")+$" {card.hits}회 줍니다" : CardPresentation.WithParticle($"피해 {d}","를","을")+" 줍니다");
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
            var mechanics = SkillMechanics.Describe(card);
            var rules = card.mechanics?.rules ?? new SkillRule[0];
            // A scaled effect and its immediately following unconditional resource spend belong together.
            for (int i = 0; i < mechanics.Count; ++i)
            {
                bool spendAfterScaling = mechanics.Count == rules.Length && i > 0 && i < rules.Length && rules[i].op == "consume"
                    && !rules[i].onHit && string.IsNullOrEmpty(rules[i].conditionKey) && string.IsNullOrEmpty(rules[i].conditionKey2)
                    && string.IsNullOrEmpty(rules[i].conditionPrevious) && rules[i - 1].scaleKey == rules[i].key;
                if (spendAfterScaling && lines.Count > 0) lines[lines.Count - 1] += " " + mechanics[i];
                else lines.Add(mechanics[i]);
            }
            lines.AddRange(StatusMechanics.Describe(card));
            if (card.freeCastCount > 0 && card.freeCastTargets != null && card.freeCastTargets.Length > 0)
            {
                string[] targets = card.freeCastTargets.Select(id => all == null ? GameDatabase.Card(id)?.name ?? id : all.Find(c => c.id == id)?.name ?? id).ToArray();
                if (card.freeCastLastSkill)
                    lines.Add("이번 턴에 마지막으로 사용한 수아의 Q·W·E 중 하나를 코스트 없이 1회 다시 사용합니다.");
                else
                    lines.Add((card.freeCastOnHit ? "적중하면 " : "") + "이번 턴에 " + string.Join("·", targets) + (targets.Length > 1 ? " 각각" : "") + $" 코스트 없이 {card.freeCastCount}회 사용권을 얻습니다.");
                lines.Add("대상 카드가 손패에 없으면 덱·버린 카드에서 가져옵니다(덱에 보유한 카드만).");
            }
            if (card.exhaust) lines.Add("사용 후 이번 전투에서 소멸합니다.");
            if (card.id == "basic_attack") lines.Add("장비 공격력과 기본 공격 강화가 적용됩니다.");
            if (lines.Count == 0) lines.Add("추가 효과 없이 행동합니다.");
            return string.Join("\n", GroupConditions(CleanLines(lines)));
        }

        static List<string> GroupConditions(List<string> lines)
        {
            var result = new List<string>();
            var positions = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string line in lines)
            {
                var match = Regex.Match(line, @"^(.+?(?:이면|하면)) (.+)$");
                if (!match.Success) { result.Add(line); continue; }
                string condition = match.Groups[1].Value.Replace("공격이 적중하면", "적중하면");
                int position;
                if (positions.TryGetValue(condition, out position)) result[position] += " " + match.Groups[2].Value;
                else
                {
                    positions[condition] = result.Count;
                    result.Add(condition + ": " + match.Groups[2].Value);
                }
            }
            return result;
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
