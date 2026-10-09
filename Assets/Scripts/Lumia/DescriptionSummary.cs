using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lumia
{
    /// <summary>Brief card previews and complete quantitative effect digests.</summary>
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
            text = Regex.Replace(text, @"(\d+)로(?= 만듭니다)", m=>m.Groups[1].Value+("036".IndexOf(m.Groups[1].Value.Last())>=0?"으로":"로"));
            text = Regex.Replace(text, @"(\d+)의 (추가 )?피해를", m => CardPresentation.WithParticle(m.Groups[2].Value + "피해 " + m.Groups[1].Value, "를", "을"));
            text = text.Replace("이면 적중하면", "이고 공격이 적중하면").Replace("이면 사용하면", "이면")
                .Replace("이면, 공격이 적중하면", "이고 공격이 적중하면").Replace("이면, 적중하면", "이고 적중하면")
                .Replace("아니면, 공격이 적중하면", "아니고 공격이 적중하면").Replace("아니면, 적중하면", "아니고 적중하면");
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

        // A preview explains the immediate action and its defining mechanic. Additional rules,
        // secondary effects and exact shared limits remain in the full detail panel.
        // Sentences are selected semantically; text is never cut in the middle of a sentence.
        public static string CardPreview(CardDef card, List<CardDef> all = null, int damage = -1, int block = -1, int heal = -1)
        {
            if (card == null) return "";
            int d = damage < 0 ? card.damage : damage, b = block < 0 ? card.block : block, h = heal < 0 ? card.heal : heal;
            var parts = new List<PreviewPart>();
            string immediate = PreviewImmediate(card, d, b, h);
            if (immediate.Length > 0) parts.Add(new PreviewPart { text=immediate, priority=1000 });
            string curated = PreviewIdentity(card);
            if (curated != null) parts.Add(new PreviewPart { text=curated, priority=950 });
            else
            {
                var rules = card.mechanics?.rules ?? new SkillRule[0];
                foreach (var rule in rules)
                {
                    if (rule.op == "consume" || rule.op == "clear_effect") continue;
                    string text = PreviewRule(card, rule, rules);
                    if (!string.IsNullOrEmpty(text)) parts.Add(new PreviewPart { text=text, priority=PreviewPriority(rule) });
                }
                if (card.freeCastCount > 0)
                {
                    string target=card.freeCastLastSkill ? "직전 Q·W·E" : string.Join("·", (card.freeCastTargets ?? new string[0]).Select(id=>PreviewCardKey(id)));
                    parts.Add(new PreviewPart { text=(card.freeCastOnHit?"적중 시 ":"")+target+(target.EndsWith("R",StringComparison.Ordinal)?"을":"를")+" 이번 턴에 무료로 "+card.freeCastCount+"회 사용합니다.", priority=880 });
                }
            }
            foreach (var rule in card.statuses ?? new CardStatusRule[0])
            {
                string when = rule.timing == "next_basic" ? "다음 기본 공격으로 " : rule.timing != "cast" ? "설치 효과로 " : rule.onHit ? "적중 시 " : "";
                parts.Add(new PreviewPart { text=PreviewCondition(card,rule)+when+CardPresentation.WithParticle(StatusMechanics.Name(rule.key),"를","을")+" "+rule.duration+"턴 부여합니다.", priority=420 });
            }
            if (card.evasion > 0) parts.Add(new PreviewPart { text=card.duration+"턴 동안 회피율이 "+card.evasion+"% 증가합니다.",priority=460 });
            if (card.draw > 0) parts.Add(new PreviewPart { text="카드 "+card.draw+"장을 뽑습니다.",priority=480 });
            if (card.energy > 0) parts.Add(new PreviewPart { text="이번 턴 코스트를 "+card.energy+" 회복합니다.",priority=490 });
            if (card.poison > 0) parts.Add(new PreviewPart { text="중독 "+card.poison+"을 부여합니다.",priority=410 });
            if (card.weak > 0) parts.Add(new PreviewPart { text="약화를 "+card.weak+"턴 부여합니다.",priority=400 });
            if (card.vulnerable > 0) parts.Add(new PreviewPart { text="취약을 "+card.vulnerable+"턴 부여합니다.",priority=400 });
            if (card.strength > 0) parts.Add(new PreviewPart { text="이번 전투의 힘이 "+card.strength+" 증가합니다.",priority=500 });
            if (card.exhaust) parts.Add(new PreviewPart { text="사용 후 소멸합니다.",priority=390 });
            var identity = parts.Where(x=>x.priority>=600 && x.priority<1000).OrderByDescending(x=>x.priority).FirstOrDefault();
            if(identity!=null && immediate.Length>0 && !PreviewFits(immediate+"\n"+identity.text))
            {
                // A secondary base heal/shield must not crowd out the defining transform/combo.
                // Retain the leading immediate action; all three live numbers remain in full detail.
                parts[0].text = d>0 ? CardPresentation.WithParticle("피해 "+d,"를","을")+(card.hits>1?" "+card.hits+"회":"")+" 줍니다."
                    : b>0 ? CardPresentation.WithParticle("방어도 "+b,"를","을")+" 얻습니다."
                    : CardPresentation.WithParticle("체력 "+h,"를","을")+" 회복합니다.";
            }
            var selected = new List<string>();
            foreach (var part in parts.OrderByDescending(x=>x.priority))
            {
                string text=Normalize(part.text);
                if(selected.Contains(text))continue;
                if (selected.Count == 0 || PreviewFits(string.Join("\n",selected)+"\n"+text))
                    selected.Add(text);
                if (selected.Count >= 2) break;
            }
            if (selected.Count == 0) selected.Add("추가 효과 없이 행동합니다.");
            return string.Join("\n",selected);
        }

        sealed class PreviewPart { public string text; public int priority; }

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
        static bool PreviewFits(string text) => PreviewWidth(text)<=66 && PreviewLines(text)<=5;

        static string PreviewImmediate(CardDef card,int d,int b,int h)
        {
            var clauses=new List<string>();
            if(d>0)clauses.Add(CardPresentation.WithParticle("피해 "+d,"를","을")+(card.hits>1?" "+card.hits+"회":"")+" 줍니다");
            if(b>0)clauses.Add(CardPresentation.WithParticle("방어도 "+b,"를","을")+" 얻습니다");
            if(h>0)clauses.Add(CardPresentation.WithParticle("체력 "+h,"를","을")+" 회복합니다");
            return string.Join(". ",clauses)+(clauses.Count>0?".":"");
        }

        static string PreviewIdentity(CardDef card)
        {
            switch(card.id)
            {
                case "nia_q":return "아케이드 블록을 1 늘립니다(최대 3).";
                case "nia_w":return "블록을 모두 소모해 블록당 피해 4를 더하고 다음 Q 코스트를 1 줄입니다.";
                case "nia_r":return "배터리 ×5(최대20)의 방어도를 더 얻고 배터리를 모두 소모합니다.";
                case "emma_r":return "직전 Q·W·E의 마술을 소모해 피해 또는 방어도를 추가합니다.";
                case "sua_r":return "직전 수아 Q·W·E를 이번 턴에 무료로 1회 다시 사용합니다.";
                case "tia_q":case "tia_e":return "붓 색의 물감을 남기며 다른 색과 섞으면 피해·회복·방어가 강화됩니다.";
                case "debi_marlene_q":return "다른 색 잉크가 있으면 추가 피해 8을 주고 현재 색 잉크를 남깁니다.";
                case "debi_marlene_w":return "다른 색 잉크가 있으면 추가 피해 6을 주고 현재 색 잉크를 남깁니다.";
                case "debi_marlene_e":return "현재 색과 잉크가 같으면 추가 피해 7을 주고 색상을 교대합니다.";
                case "tia_w":return "붓을 노랑·빨강·파랑 순으로 바꿉니다.";
                case "silvia_r":return "연료가 있으면 바이크에 타고 탑승 중이면 내립니다. 연료를 1 소모합니다.";
                case "coraline_w":return "백색 거울과 흑색 거울을 교대로 설치합니다.";
                case "basic_attack":return "기본 공격 강화와 장비 공격력이 적용됩니다.";
                default:return null;
            }
        }

        static int PreviewPriority(SkillRule rule)
        {
            if(rule.op=="state_toggle")return 990;
            if(!string.IsNullOrEmpty(rule.scaleKey))return 900;
            switch(rule.op)
            {
                case "revive":return 940;
                case "discount":return 850;
                case "bonus_damage":case "bonus_block":case "bonus_heal":return 820;
                case "delayed_damage":case "summon":case "bleed":case "burn":return 800;
                case "counter":case "hot":case "guard":case "empower_basic":return 780;
                case "state_on":case "state_off":return 760;
                case "gain":case "set":return 720;
                default:return 600;
            }
        }

        static string PreviewCardKey(string id)
        {
            var target=GameDatabase.Card(id);
            return target==null?id:target.key;
        }

        static string PreviewCondition(CardDef card,SkillRule rule)
        {
            var conditions=new List<Func<bool,string>>();
            if(!string.IsNullOrEmpty(rule.conditionKey))conditions.Add(last=>PreviewConditionValue(card,rule.conditionKey,rule.conditionAmount,rule.conditionExact,last));
            if(!string.IsNullOrEmpty(rule.conditionKey2))conditions.Add(last=>PreviewConditionValue(card,rule.conditionKey2,rule.conditionAmount2,rule.conditionExact2,last));
            if(!string.IsNullOrEmpty(rule.conditionPrevious))conditions.Add(last=>"직전 기술이 "+PreviewCardKey(rule.conditionPrevious)+(last?"이면":"이고"));
            return conditions.Count==0?"":string.Join(" ",conditions.Select((f,i)=>f(i==conditions.Count-1)))+" ";
        }

        static string PreviewConditionValue(CardDef card,string key,int amount,bool exact,bool last)
        {
            if(SkillMechanics.IsState(card,key))
            {
                string condition=SkillMechanics.StateCondition(card,key,amount,exact);
                return last?condition:condition.Replace("이면","이고").Replace("아니면","아니고");
            }
            return CardPresentation.WithParticle(SkillMechanics.ResourceName(card,key),"가","이")+" "+amount+(exact?"":" 이상")+(last?"이면":"이고");
        }

        static string PreviewRule(CardDef card,SkillRule rule,SkillRule[] rules)
        {
            string label=SkillMechanics.ResourceName(card,rule.key), condition=PreviewCondition(card,rule);
            string hit=rule.onHit?"적중 시 ":"";
            string scale=string.IsNullOrEmpty(rule.scaleKey)?rule.amount.ToString():SkillMechanics.ResourceName(card,rule.scaleKey)+" ×"+rule.amount;
            if(!string.IsNullOrEmpty(rule.scaleKey))
            {
                int limit=rule.op=="bonus_damage"||rule.op=="bonus_block"||rule.op=="delayed_damage"?24:rule.op=="bonus_heal"?20:rule.op=="counter"?8:rule.op=="discount"?7:rule.op=="revive"?40:rule.op=="empower_basic"?16:10;
                scale+="(최대"+Math.Min(limit,rule.amount*rule.cap)+")";
            }
            string text;
            switch(rule.op)
            {
                case "state_on":text=SkillMechanics.StateEnterText(card,rule.key);break;
                case "state_off":text=SkillMechanics.StateExitText(card,rule.key);break;
                case "state_toggle":return SkillMechanics.StateName(card,rule.key)+"로 진입하거나 해제합니다.";
                case "gain":text=CardPresentation.WithParticle(label,"를","을")+" "+rule.amount+" 늘립니다(최대 "+rule.cap+")";break;
                case "set":text=CardPresentation.WithParticle(label,"를","을")+" "+rule.amount+"로 만듭니다";break;
                case "bonus_damage":text="추가 피해를 "+scale+"만큼 줍니다";break;
                case "bonus_block":text="추가 방어도를 "+scale+"만큼 얻습니다";break;
                case "bonus_heal":text="체력을 "+scale+"만큼 추가 회복합니다";break;
                case "discount":text="다음 "+PreviewCardKey(rule.targetCard)+" 코스트를 "+scale+" 줄입니다";break;
                case "bleed":case "burn":case "summon":text=(rule.label??label)+" 효과로 턴 종료마다 "+CardPresentation.WithParticle("피해 "+scale,"를","을")+" "+rule.duration+"회 줍니다";break;
                case "hot":text="턴 종료마다 체력을 "+scale+"씩 "+rule.duration+"회 회복합니다";break;
                case "guard":text="턴 종료마다 방어도를 "+scale+"씩 "+rule.duration+"회 얻습니다";break;
                case "delayed_damage":text="턴 종료 "+rule.delay+"회 후 "+(rule.label??label)+" 효과로 "+CardPresentation.WithParticle("피해 "+scale,"를","을")+" 줍니다";break;
                case "counter":text=rule.duration+"턴 동안 턴당 1회 반격 피해를 "+scale+"만큼 줍니다";break;
                case "empower_basic":text="다음 기본 공격 피해를 "+scale+" 늘립니다";break;
                case "revive":text=rule.duration+"턴 동안 치명상을 1회 막고 "+CardPresentation.WithParticle("체력 "+scale,"를","을")+" 회복합니다";break;
                default:return null;
            }
            if(!string.IsNullOrEmpty(rule.scaleKey) && rules.Any(r=>r.op=="consume" && r.key==rule.scaleKey && r.amount<=0 && !r.onHit && string.IsNullOrEmpty(r.conditionKey)))
                text+="(해당 자원 모두 소모)";
            return condition+hit+text+".";
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
