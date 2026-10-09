using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    /// <summary>One vocabulary for the catalog, rewards, hand and detailed card view.</summary>
    public static class CardPresentation
    {
        static readonly string[] Palette = { "91dfbd", "efc979", "dd88ae", "8fbff0", "d6a5f4", "f29475", "80d9e3", "bedb85" };

        public static int Cost(float seconds)
        {
            return seconds <= 3 ? 0 : seconds <= 7 ? 1 : seconds <= 12 ? 2 : seconds <= 20 ? 3 : seconds <= 35 ? 4 : seconds <= 60 ? 5 : seconds <= 100 ? 6 : 7;
        }

        public static void Configure(List<CardDef> cards)
        {
            foreach (var c in cards)
            {
                if (c.category != "basic") c.cost = Cost(c.cooldown);
                // These original skills require marks, energy or a stance resource despite their short cooldown.
                if (c.id == "tsubame_r") c.cost = 3;
                if (c.id == "justyna_q") c.cost = 2;
                if (c.id == "hisui_w") c.cost = 3;
                if (c.cost == 0 && !c.exhaust && (c.draw > 0 || c.energy > 0)) c.cost = 1;
                // A long cooldown represents a substantial turn investment, with matching impact.
                if (c.category != "basic")
                {
                    c.damage = Scale(c.damage, .75f + c.cost * .25f);
                    c.block = Scale(c.block, 1f + c.cost * .12f);
                    c.heal = Scale(c.heal, 1f + c.cost * .12f);
                }
                int hash = StableHash(c.id);
                c.effectVariant = hash;
                if (string.IsNullOrEmpty(c.effect)) c.effect = EffectFor(c);
                if (string.IsNullOrEmpty(c.effectColor)) c.effectColor = Palette[StableHash(c.owner) % Palette.Length];
                c.description = Describe(c, cards);
            }
        }

        static int Scale(int value, float factor) => value <= 0 ? 0 : Math.Max(1, (int)Math.Round(value * factor));
        public static int StableHash(string value)
        {
            uint hash = 2166136261;
            foreach (char c in value ?? "") hash = (hash ^ c) * 16777619;
            return (int)(hash & 0x7fffffff);
        }

        public static string WithParticle(string value, string vowel, string consonant)
        {
            if (string.IsNullOrEmpty(value)) return "효과" + consonant;
            char last=value[value.Length-1];
            bool final=last>='가' && last<='힣' ? (last-'가')%28!=0 : char.IsDigit(last) ? "013678".IndexOf(last)>=0 : false;
            return value+(final?consonant:vowel);
        }

        public static string Describe(CardDef c, List<CardDef> all = null, int damage = -1, int block = -1, int heal = -1)
        {
            return string.Join("\n", Sentences(c, all, damage, block, heal));
        }

        public static List<string> Sentences(CardDef c, List<CardDef> all = null, int damage = -1, int block = -1, int heal = -1)
        {
            var lines = new List<string>();
            int d = damage < 0 ? c.damage : damage, b = block < 0 ? c.block : block, h = heal < 0 ? c.heal : heal;
            if (d > 0) lines.Add(c.hits > 1 ? CardPresentation.WithParticle($"적에게 피해 {d}","를","을")+$" {c.hits}회 줍니다." : CardPresentation.WithParticle($"적에게 피해 {d}","를","을")+" 줍니다.");
            if (b > 0) lines.Add(CardPresentation.WithParticle($"방어도 {b}","를","을")+" 얻습니다.");
            if (h > 0) lines.Add(CardPresentation.WithParticle($"체력 {h}","를","을")+" 회복합니다.");
            if (c.draw > 0) lines.Add($"카드 {c.draw}장을 뽑습니다.");
            if (c.energy > 0) lines.Add(CardPresentation.WithParticle($"이번 턴에 사용할 코스트 {c.energy}","를","을")+" 회복합니다.");
            if (c.evasion > 0) lines.Add($"{c.duration}턴 동안 회피율이 {c.evasion}% 증가합니다.");
            if (c.poison > 0) lines.Add($"적에게 중독을 {c.poison} 부여합니다.");
            if (c.weak > 0) lines.Add($"적에게 {c.weak}턴 동안 약화를 부여합니다.");
            if (c.vulnerable > 0) lines.Add($"적에게 {c.vulnerable}턴 동안 취약을 부여합니다.");
            if (c.strength > 0) lines.Add($"이번 전투에서 힘이 {c.strength} 증가합니다.");
            var effects = SkillMechanics.DescriptionEffects(c).Concat(StatusMechanics.DescriptionEffects(c)).ToList();
            string freeCastNote = "";
            if (c.freeCastCount > 0 && c.freeCastTargets != null && c.freeCastTargets.Length > 0)
            {
                var names = c.freeCastTargets.Select(id => all == null ? id : all.Find(x => x.id == id)?.name ?? id);
                effects.Add(new ConditionEffect { key=DescriptionSummary.ConditionKey(new SkillRule { onHit=c.freeCastOnHit }), condition=c.freeCastOnHit?"공격이 적중하면":"",
                    effect=c.freeCastLastSkill ? "이번 전투에서 마지막으로 사용한 수아의 Q·W·E 카드 한 장을 이번 턴에 코스트 없이 다시 사용할 수 있습니다"
                    : "이번 턴에 " + string.Join("·", names) + " 카드를 " + (c.freeCastTargets.Length > 1 ? "각각 " : "") + $"{c.freeCastCount}회 코스트 없이 사용할 수 있습니다" });
                freeCastNote="덱에 보유한 대상 카드가 손패에 없다면 뽑을 카드 또는 버린 카드에서 한 장을 가져옵니다.";
            }
            lines.AddRange(DescriptionSummary.GroupEffects(effects));
            if (freeCastNote.Length > 0) lines.Add(freeCastNote);
            if (c.exhaust) lines.Add("사용한 카드는 이번 전투에서 소멸합니다.");
            if (c.id == "basic_attack") lines.Add("장비의 공격력과 기본 공격 강화 효과를 받습니다. 치명타 적중 시 이 공격의 피해가 1.5배가 됩니다. 별도로 발동하는 추가 피해에는 치명타가 적용되지 않습니다.");
            if (lines.Count == 0) lines.Add("턴을 준비하며 행동 기회를 얻습니다.");
            return DescriptionSummary.CleanLines(lines);
        }

        public static string Rules(CardDef c)
        {
            var lines = new List<string>();
            if (c.block > 0) lines.Add("방어도는 피해를 먼저 막고 다음 내 턴이 시작될 때 사라집니다.");
            if (c.strength > 0) lines.Add("힘은 각 공격의 피해를 높입니다.");
            if (c.evasion > 0) lines.Add("카드의 회피 보너스는 가장 높은 값만 적용되며, 최종 회피율은 65%를 넘지 않습니다.");
            if (c.freeCastCount > 0) lines.Add("연계 사용권은 이번 턴에만 유효하며, 같은 스킬에서 받는 횟수에는 턴별 제한이 있습니다. 소멸한 카드는 돌아오지 않습니다.");
            string mechanics = SkillMechanics.Rules(c);
            if (!string.IsNullOrEmpty(mechanics)) lines.Add(mechanics);
            if(c.movement)lines.Add("이 카드는 이동 기술이므로 속박의 사용 제한과 둔화의 추가 코스트가 적용됩니다.");
            string debuffs = DebuffRules(c);
            if (!string.IsNullOrEmpty(debuffs)) lines.Add(debuffs);
            return string.Join("\n", lines);
        }

        // Read the actual application paths instead of searching prose: conditional control,
        // next-basic preparations and installation-triggered control all retain their definitions.
        public static string DebuffRules(CardDef c)
        {
            if (c == null) return "";
            var kinds = new List<string>();
            if (c.poison > 0) kinds.Add("poison");
            if (c.weak > 0) kinds.Add("weak");
            if (c.vulnerable > 0) kinds.Add("vulnerable");
            var statuses = (c.statuses ?? new CardStatusRule[0])
                .Where(x => x != null && !string.IsNullOrEmpty(x.key)).ToArray();
            kinds.AddRange(statuses.Select(x => x.key));
            var mechanics = c.mechanics?.rules ?? new SkillRule[0];
            kinds.AddRange(mechanics.Where(x => x != null && x.amount > 0 && (x.op == "bleed" || x.op == "burn"))
                .Select(x => x.op));
            kinds = kinds.Distinct().ToList();
            if (kinds.Count == 0) return "";

            var lines = new List<string> { "디버프 설명" };
            foreach (string kind in kinds)
            {
                string name, explanation;
                switch (kind)
                {
                    case "poison":
                        name = "중독";
                        explanation = "대상의 턴 시작에 중독 수치만큼 방어도를 무시하는 피해를 주고 수치가 1 줄어듭니다. 다시 부여하면 수치가 더해집니다.";
                        break;
                    case "weak":
                        name = "약화";
                        explanation = "공격 피해가 25% 감소합니다. 대상의 턴 종료마다 남은 턴이 1 줄어들며, 다시 부여해도 기간은 더해지지 않고 남은 턴 중 큰 값을 유지합니다.";
                        break;
                    case "vulnerable":
                        name = "취약";
                        explanation = "받는 공격 피해가 50% 증가합니다. 대상의 턴 종료마다 남은 턴이 1 줄어들며, 다시 부여해도 기간은 더해지지 않고 남은 턴 중 큰 값을 유지합니다.";
                        break;
                    case "bleed":
                    case "burn":
                        name = kind == "bleed" ? "출혈" : "화상";
                        explanation = "시전자의 턴 종료마다 적에게 방어도를 무시하는 피해를 주고 남은 적용 횟수가 1 줄어듭니다.";
                        break;
                    default:
                        name = StatusMechanics.Name(kind);
                        explanation = StatusMechanics.Explain(kind);
                        break;
                }
                lines.Add(name + ": " + explanation);
            }
            if (statuses.Length > 0)
                lines.Add("상태이상의 남은 턴은 대상의 턴 종료마다 1 줄어듭니다. 같은 상태는 중첩되지 않으며, 다시 부여하면 남은 턴 중 큰 값을 유지합니다. 다음 기본 공격이나 설치물로 부여하는 상태는 해당 효과가 발동한 뒤부터 적용됩니다.");
            if (kinds.Contains("bleed") || kinds.Contains("burn"))
                lines.Add("같은 실험체의 같은 지속 피해 효과를 다시 부여하면 피해와 남은 적용 횟수 중 각각 큰 값을 유지합니다. 기술·패시브·룬을 합친 시전자의 턴 종료 피해는 총 40까지 적용됩니다.");
            return string.Join("\n", lines);
        }

        static string EffectFor(CardDef c)
        {
            string text = c.name + " " + c.owner;
            if (Contains(text, "폭탄", "폭죽", "핵", "공포탄", "플라즈마", "폭발")) return "explosion";
            if (Contains(text, "번개", "전기", "벽력", "볼트", "스파크")) return "lightning";
            if (Contains(text, "불", "화염", "화망", "작열", "불꽃")) return "flame";
            if (Contains(text, "얼음", "빙", "눈꽃", "서리", "동결")) return "ice";
            if (Contains(text, "저격", "사격", "연발", "탄", "총", "화살", "활", "황소")) return "projectile";
            if (Contains(text, "덫", "지뢰", "트랩")) return "trap";
            if (Contains(text, "니아", "게임", "아케이드", "1UP")) return "arcade";
            if (Contains(text, "부", "마술", "마법", "요정", "오대존", "영혼", "별")) return "arcane";
            if (c.heal > 0 && c.damage == 0) return "heal";
            if (c.evasion > 0 && c.damage == 0) return "dash";
            if (c.block > 0 && c.damage == 0) return "shield";
            if (c.poison > 0 && c.damage == 0) return "poison";
            if (c.hits > 1) return "multi";
            if (c.damage > 0) return "slash";
            return "arcane";
        }

        static bool Contains(string text, params string[] fragments) => fragments.Any(text.Contains);
    }
}
