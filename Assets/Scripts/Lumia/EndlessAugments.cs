using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Lumia
{
    public enum AugmentTier { Silver = 0, Gold = 1, Prismatic = 2 }

    public sealed class EndlessAugmentDef
    {
        public string id, name, description, effect;
        public AugmentTier tier;
        public int amount, secondaryAmount, threshold, maxStacks;
        internal string descriptionFormat;
    }

    /// <summary>Endless-only choices. Amounts are per acquisition; support choices resolve immediately.</summary>
    public static class EndlessAugments
    {
        public const int PersistentStackLimit = 3;
        public const int SilverChance = 25;
        public const int GoldChance = 45;
        public const int PrismaticChance = 30;

        static EndlessAugmentDef Def(string id, string name, AugmentTier tier, string effect, int amount,
            string format, int secondaryAmount = 0, int threshold = 0, bool support = false)
        {
            var value = new EndlessAugmentDef
            {
                id = id, name = name, tier = tier, effect = effect, amount = amount,
                secondaryAmount = secondaryAmount, threshold = threshold,
                maxStacks = support ? 0 : PersistentStackLimit, descriptionFormat = format
            };
            value.description = Format(value, 1);
            return value;
        }

        public static readonly IReadOnlyList<EndlessAugmentDef> All = Array.AsReadOnly(new[]
        {
            Def("aug_s_basic_guard", "든든한 한 방", AugmentTier.Silver, "first_basic_block", 3,
                "매 턴 첫 유료 기본 공격을 사용하면 방어를 {0} 얻습니다."),
            Def("aug_s_guard_draw", "방어적 탐색", AugmentTier.Silver, "first_guard_draw", 1,
                "매 턴 카드 자체 효과로 방어를 얻는 첫 유료 카드를 사용하면 카드를 {0}장 뽑습니다."),
            Def("aug_s_weapon_damage", "무기 예열", AugmentTier.Silver, "first_weapon_damage", 6,
                "매 턴 첫 유료 D 공격에 피해를 {0} 추가합니다. 추가 피해는 카드당 한 번 적용됩니다."),
            Def("aug_s_tactical_guard", "전술 엄폐", AugmentTier.Silver, "first_tactical_block", 5,
                "매 턴 첫 유료 F 카드를 사용하면 방어를 {0} 얻습니다."),
            Def("aug_s_reserve_guard", "잔여 전력", AugmentTier.Silver, "reserve_block", 5,
                "턴 종료 시 남은 코스트가 {2} 이상이면 방어를 {0} 얻습니다.", threshold: 2),
            Def("aug_s_victory_heal", "간이 수복", AugmentTier.Silver, "victory_heal", 4,
                "전투에서 승리할 때마다 체력을 {0} 회복합니다."),
            Def("aug_s_credits", "보통 보급금", AugmentTier.Silver, "support_credits", 200,
                "즉시 크레딧을 {0} 얻습니다. 이 지원은 반복해서 선택할 수 있습니다.", support: true),
            Def("aug_s_heal", "보통 응급 수복", AugmentTier.Silver, "support_heal", 60,
                "즉시 최대 체력의 {0}%만큼 체력을 회복합니다. 이 지원은 반복해서 선택할 수 있습니다.", support: true),
            Def("aug_s_health", "보통 생체 확장", AugmentTier.Silver, "support_max_health", 8,
                "최대 체력이 {0} 증가하고 체력을 {0} 회복합니다. 이 지원은 반복해서 선택할 수 있습니다.", support: true),

            Def("aug_g_qwe_energy", "기초 연계 회로", AugmentTier.Gold, "qwe_energy", 1,
                "실험체 구분 없이 한 턴에 Q, W, E를 모두 유료로 사용하면 코스트를 {0} 회복합니다. 턴당 한 번 발동합니다."),
            Def("aug_g_ultimate_draw", "궁극의 재정비", AugmentTier.Gold, "first_ultimate_draw", 2,
                "실험체 구분 없이 매 턴 첫 유료 R 카드를 사용하면 카드를 {0}장 뽑습니다."),
            Def("aug_g_debuffed_damage", "노출된 빈틈", AugmentTier.Gold, "debuffed_damage", 7,
                "이미 디버프가 있는 적에게 매 턴 첫 유료 공격의 피해를 {0} 추가합니다. 추가 피해는 카드당 한 번 적용됩니다."),
            Def("aug_g_low_health", "위기 대응 장치", AugmentTier.Gold, "low_health_block", 8,
                "턴 시작 시 체력이 최대 체력의 절반 이하면 방어를 {0} 얻습니다."),
            Def("aug_g_weapon_basic", "교차 타격", AugmentTier.Gold, "weapon_basic_damage", 10,
                "이번 턴에 D 카드를 사용했다면, 그 뒤의 첫 유료 기본 공격에 피해를 {0} 추가합니다."),
            Def("aug_g_small_deck", "압축 덱", AugmentTier.Gold, "small_deck_draw", 1,
                "턴 시작 시 덱이 {2}장 이하면 카드를 {0}장 더 뽑습니다. 장비가 제공한 카드도 덱에 포함됩니다.", threshold: 18),
            Def("aug_g_credits", "희귀 보급금", AugmentTier.Gold, "support_credits", 350,
                "즉시 크레딧을 {0} 얻습니다. 이 지원은 반복해서 선택할 수 있습니다.", support: true),
            Def("aug_g_heal", "희귀 응급 수복", AugmentTier.Gold, "support_heal", 80,
                "즉시 최대 체력의 {0}%만큼 체력을 회복합니다. 이 지원은 반복해서 선택할 수 있습니다.", support: true),
            Def("aug_g_health", "희귀 생체 확장", AugmentTier.Gold, "support_max_health", 14,
                "최대 체력이 {0} 증가하고 체력을 {0} 회복합니다. 이 지원은 반복해서 선택할 수 있습니다.", support: true),

            Def("aug_p_heavy_refund", "초고출력 환류", AugmentTier.Prismatic, "heavy_refund", 2,
                "매 턴 코스트를 {2} 이상 지불한 첫 카드 사용 후 코스트를 최대 {0} 회복합니다. 지불한 코스트를 넘겨 회복하지 않습니다.", threshold: 4),
            Def("aug_p_arsenal", "완성형 연계", AugmentTier.Prismatic, "arsenal_block", 14,
                "실험체 구분 없이 한 턴에 Q, W, E, R을 모두 유료로 사용하면 방어를 {0} 얻습니다. 턴당 한 번 발동합니다."),
            Def("aug_p_debuffed_r", "붕괴 임계점", AugmentTier.Prismatic, "debuffed_ultimate_damage", 18,
                "이미 디버프가 있는 적에게 실험체 구분 없이 매 턴 첫 유료 R 공격의 피해를 {0} 추가합니다. 추가 피해는 카드당 한 번 적용됩니다."),
            Def("aug_p_shield", "방패를 창으로", AugmentTier.Prismatic, "shield_conversion", 12,
                "방어가 {2} 이상일 때 매 턴 첫 유료 공격을 사용하면 방어를 {1} 소모하고 피해를 {0} 추가합니다. 빗나가도 방어는 소모합니다.", secondaryAmount: 5, threshold: 15),
            Def("aug_p_all_in", "최후의 한 수", AugmentTier.Prismatic, "all_in_block", 10,
                "유료 카드로 남은 코스트를 전부 사용하면 방어를 {0} 얻습니다. 턴당 한 번 발동합니다."),
            Def("aug_p_boss_energy", "보스 전용 전원", AugmentTier.Prismatic, "boss_opening_energy", 1,
                "보스전의 첫 턴에만 사용할 수 있는 코스트를 {0} 추가로 얻습니다. 최대 코스트는 바뀌지 않습니다."),
            Def("aug_p_credits", "전설 보급금", AugmentTier.Prismatic, "support_credits", 550,
                "즉시 크레딧을 {0} 얻습니다. 이 지원은 반복해서 선택할 수 있습니다.", support: true),
            Def("aug_p_heal", "전설 응급 수복", AugmentTier.Prismatic, "support_heal", 100,
                "즉시 체력을 모두 회복합니다. 이 지원은 반복해서 선택할 수 있습니다.", support: true),
            Def("aug_p_health", "전설 생체 확장", AugmentTier.Prismatic, "support_max_health", 22,
                "최대 체력이 {0} 증가하고 체력을 {0} 회복합니다. 이 지원은 반복해서 선택할 수 있습니다.", support: true)
        });

        public static EndlessAugmentDef Find(string id) { return All.FirstOrDefault(x => x.id == id); }
        public static bool IsSupport(EndlessAugmentDef value) { return value != null && value.effect.StartsWith("support_", StringComparison.Ordinal); }

        public static string TierName(AugmentTier tier)
        {
            switch (tier)
            {
                case AugmentTier.Gold: return "희귀";
                case AugmentTier.Prismatic: return "전설";
                default: return "보통";
            }
        }

        /// <param name="offeringCycle">The one-based number of a completed three-act cycle.</param>
        /// <param name="roll">A deterministic integer from 0 through 99.</param>
        public static AugmentTier TierForOffering(int offeringCycle, int roll)
        {
            if (offeringCycle <= 1) return AugmentTier.Silver;
            if (offeringCycle == 2) return AugmentTier.Gold;
            if (offeringCycle == 3) return AugmentTier.Prismatic;
            roll = Math.Max(0, Math.Min(99, roll));
            return roll < SilverChance ? AugmentTier.Silver
                : roll < SilverChance + GoldChance ? AugmentTier.Gold : AugmentTier.Prismatic;
        }

        static string Format(EndlessAugmentDef value, int stacks)
        {
            int count = IsSupport(value) ? 1 : Math.Max(1, Math.Min(value.maxStacks, stacks));
            return string.Format(CultureInfo.InvariantCulture, value.descriptionFormat,
                value.amount * count, value.secondaryAmount, value.threshold);
        }

        public static string Description(string id, int stacks = 1)
        {
            var value = Find(id);
            return value == null ? "" : Format(value, stacks);
        }
    }
}
