using System;
using System.Collections.Generic;

namespace Lumia
{
    // Unit-targeted casts are explicit source identities, never inferred from card
    // category, movement, damage text or a free-recast target. See docs/ACCURACY.md.
    public static class AccuracyMechanics
    {
        public const int BaseAccuracy = 100;
        static readonly HashSet<string> Targeted = new HashSet<string>(StringComparer.Ordinal) {
            "weapon_glove", "weapon_rapier", "weapon_dagger",
            "aya_q", "sua_w", "emma_e", "nicky_r", "daniel_w", "daniel_r",
            "rozzi_e", "luke_e", "mai_e", "magnus_e", "shoichi_w", "adina_e",
            "camilo_e", "chiara_r", "abigail_e", "alonso_e", "tsubame_r", "katja_r", "istvan_r", "fenrir_r"
        };

        public static bool GuaranteedHit(CardDef card, SkillActorState actor = null, bool freeCast = false)
        {
            if (card == null) return false;
            string id = CardInstances.BaseId(card.id);
            return Targeted.Contains(id) || id == "silvia_e" && SkillMechanics.Resource(actor, card.owner, "bike") > 0
                || id == "garnet_r" && freeCast && SkillMechanics.Resource(actor, card.owner, "embrace") > 0;
        }

        public static int Accuracy(StatusActorState actor)
            => Math.Max(0, Math.Min(100, BaseAccuracy - StatusMechanics.AccuracyPenalty(actor)));

        public static int Evasion(int baseEvasion, StatusActorState actor)
            => Math.Max(0, baseEvasion - StatusMechanics.EvasionPenalty(actor));

        public static string Description(CardDef card)
        {
            if (GuaranteedHit(card)) return "필중: 이 카드의 직접 공격은 명중률 감소와 대상의 회피율을 무시하고 반드시 적중합니다.";
            if (card?.id == "silvia_e") return "바이크 상태의 윌리는 필중하여 명중률 감소와 회피율을 무시합니다. 바이크가 아니면 일반 명중·회피 판정을 적용합니다.";
            if (card?.id == "garnet_r") return "첫 시전은 일반 명중·회피 판정을 합니다. 철처녀 표식이 남은 상대에게 이번 턴 무료 재사용할 때만 필중하여 명중률 감소와 회피율을 무시합니다.";
            return "";
        }
    }
}
