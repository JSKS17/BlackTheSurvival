using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    public sealed class TacticalEncounterSale
    {
        public readonly string subjectId, cardId, action, narrative;
        public readonly int optionIndex, price, rarePrice;
        public TacticalEncounterSale(string subjectId, string cardId, int optionIndex, int price, int rarePrice, string action, string narrative)
        { this.subjectId = subjectId; this.cardId = cardId; this.optionIndex = optionIndex; this.price = price; this.rarePrice = rarePrice; this.action = action; this.narrative = narrative; }
    }

    public static class TacticalEncounterOptions
    {
        // Selected specialists share equipment, rather than every subject handing
        // out the same reward. Existing promise choices keep their original slot.
        public static readonly string[] ModuleSubjects = { "barbara", "theodore", "aiden", "echion", "justyna", "blair" };
        public static readonly TacticalEncounterSale[] Sales =
        {
            new TacticalEncounterSale("alex", "tactical_blink", 2, 60, 130, "접선 뒤의 비상 이탈 장치를 구입한다", "알렉스가 추적 신호를 끊을 비상 이탈 장치를 암호화해 건넨다."),
            new TacticalEncounterSale("haze", "tactical_plasma", 2, 65, 140, "돌입용 플라즈마 장비를 구입한다", "헤이즈가 포격 뒤의 빈틈에 돌입할 플라즈마 장비를 검수한다."),
            new TacticalEncounterSale("isol", "tactical_gravity", 2, 75, 145, "퇴로를 지키는 중력 장치를 구입한다", "아이솔이 도주로와 폭탄 사이에 적을 묶어 둘 중력 장치를 조용히 내려놓는다."),
            new TacticalEncounterSale("laura", "tactical_strider", 1, 55, 130, "괴도의 이동 장치를 거래한다", "라우라가 예고장 뒤에 적힌 가격을 가리키며 빠른 침입과 탈출을 위한 이동 장치를 내민다."),
            new TacticalEncounterSale("sissela", "tactical_wind", 2, 60, 125, "윌슨 곁의 응급 회복 장치를 구입한다", "시셀라가 윌슨을 끌어안고, 여정 중 고통을 잠시 덜어 줄 회복 장치를 건넨다."),
            new TacticalEncounterSale("johann", "tactical_prayer", 2, 70, 135, "빛을 전하는 기도 장치를 구입한다", "요한이 누구도 홀로 두지 않을 기도의 빛을 작은 장치에 담아 준다."),
            new TacticalEncounterSale("adriana", "tactical_meteor", 2, 70, 150, "상공에서 불을 부르는 장치를 구입한다", "아드리아나의 시선이 위를 향한다. 넓은 땅에 불빛을 남길 낙하 장치를 조심스럽게 건넨다."),
            new TacticalEncounterSale("nicky", "tactical_punch", 2, 65, 140, "결정적인 액션을 위한 장치를 구입한다", "니키가 스턴트의 마무리에 쓸 타격 장치를 시연하고, 다치지 않게 안전핀부터 확인해 준다.")
        };

        static readonly Dictionary<string, string> ModuleNarratives = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "barbara", "바바라가 오차를 바로잡은 전술강화 모듈을 건네며, 어느 전술에 적용할지는 사용자가 결정해야 한다고 강조한다." },
            { "theodore", "테오도르가 지원 장치에서 분리한 전술강화 모듈을 건네고, 다음 작전에 필요한 전술부터 점검하자고 한다." },
            { "aiden", "에이든이 불안정한 전압을 낮춘 뒤 안전하게 조정한 전술강화 모듈을 하나에게 맡긴다." },
            { "echion", "에키온이 VF 제어 장치의 부하를 줄인 조정법을 전술강화 모듈에 기록하고, 힘을 쓸 곳은 직접 고르라고 한다." },
            { "justyna", "유스티나가 귀환 작전의 지원품으로 전술강화 모듈을 내밀며, 하나만의 필승 전술을 준비하자고 말한다." },
            { "blair", "블레어가 추적 장치의 검증을 마치고 전술강화 모듈을 건넨다. 선택할 전술이 사냥의 목적에 맞는지 다시 확인한다." }
        };

        public static void ConfigureNormal(IEnumerable<EventDef> encounters)
        {
            foreach (var encounter in encounters)
            {
                if (encounter == null || encounter.chainOnly || encounter.options == null || encounter.options.Length != 3) continue;
                string subjectId = encounter.id.StartsWith("event_", StringComparison.Ordinal) ? encounter.id.Substring(6) : "";
                if (ModuleSubjects.Contains(subjectId)) encounter.options[2] = ModuleOption(subjectId, false);
                var sale = Sales.FirstOrDefault(entry => entry.subjectId == subjectId);
                if (sale != null)
                {
                    if (!string.IsNullOrEmpty(encounter.options[sale.optionIndex].followupId))
                        throw new InvalidOperationException("Tactical reward must preserve encounter promise: " + encounter.id);
                    encounter.options[sale.optionIndex] = SaleOption(sale, false);
                }
            }
        }

        public static void ConfigureRare(EventDef encounter, string subjectId)
        {
            if (encounter == null || encounter.options == null || encounter.options.Length != 3) return;
            // Keep the rare skin story, its ++ representative and its passive/R.
            if (ModuleSubjects.Contains(subjectId)) encounter.options[2] = ModuleOption(subjectId, true);
            var sale = Sales.FirstOrDefault(entry => entry.subjectId == subjectId);
            if (sale != null) encounter.options[2] = SaleOption(sale, true);
        }

        static EventOption ModuleOption(string subjectId, bool rare) => new EventOption
        {
            label = rare ? "특별한 조정 기록과 모듈을 받는다" : "전술 장치의 조정 기록을 받는다",
            description = ModuleNarratives[subjectId], effect = "tactical_module", amount = rare ? 2 : 1
        };

        static EventOption SaleOption(TacticalEncounterSale sale, bool rare)
        {
            var card = GameDatabase.Card(sale.cardId);
            if (!TacticalSkills.IsTactical(card)) throw new InvalidOperationException("Unknown tactical sale: " + sale.cardId);
            int price = rare ? sale.rarePrice : sale.price;
            return new EventOption { label = sale.action + " · " + card.name + (rare ? " [전술 강화]" : "") + " · " + price + " CR",
                description = sale.narrative, effect = rare ? "trade_tactical_enhanced" : "trade_card", cardId = sale.cardId, amount = price };
        }
    }
}
