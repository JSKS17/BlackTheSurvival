using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    [Serializable] public sealed class CompanionState
    {
        public int version, hp, maxHp, linkTurns;
    }

    public struct CompanionAbsorption
    {
        public int remaining, absorbed;
        public bool destroyed;
    }

    // Nina's life is combat-local and is shared by all Chloe cards in the deck.
    // Absorb receives damage left after the owner's block; self-paid HP is excluded
    // by the caller. The finite pet HP pool can never grant owner invulnerability.
    public static class CompanionMechanics
    {
        public const int NinaMaxHp = 18;
        public const int QRepair = 4;
        public const int ERepair = 6;
        public const int RReviveHp = 12;
        public const int RLinkTurns = 2;
        public const int NormalSharePercent = 25;
        public const int LinkedSharePercent = 50;
        const int CurrentVersion = 1;

        public static CompanionState Ensure(SkillActorState actor)
        {
            if (actor == null) return null;
            if (actor.resources == null) actor.resources = new List<SkillResource>();
            if (actor.effects == null) actor.effects = new List<SkillTimedEffect>();
            if (actor.companion == null) actor.companion = new CompanionState();
            var state = actor.companion;
            if (state.version < CurrentVersion)
            {
                int legacyLink = actor.effects.Where(x => ChloeEffect(x) && x.kind == "counter" && x.key == "soul_link")
                    .Select(x => Math.Max(0, x.remaining)).DefaultIfEmpty(0).Max();
                bool legacyNina = actor.resources.Any(x => ChloeOwner(x.owner) && x.key == "nina" && x.amount > 0)
                    || actor.effects.Any(x => ChloeEffect(x) && x.key == "nina" && x.remaining > 0);
                state.maxHp = NinaMaxHp;
                if (legacyNina || legacyLink > 0) state.hp = NinaMaxHp;
                state.linkTurns = Math.Max(state.linkTurns, Math.Min(RLinkTurns, legacyLink));
                state.version = CurrentVersion;
                // Old saves used a generic counter to stand in for life sharing.
                // It must not survive alongside the actual finite HP mechanic.
                actor.effects.RemoveAll(x => ChloeEffect(x) && x.kind == "counter" && x.key == "soul_link");
            }
            state.maxHp = NinaMaxHp;
            state.hp = Math.Max(0, Math.Min(state.maxHp, state.hp));
            state.linkTurns = state.hp > 0 ? Math.Max(0, Math.Min(RLinkTurns, state.linkTurns)) : 0;
            return state;
        }

        public static CompanionState Clone(CompanionState state)
        {
            return state == null ? null : new CompanionState {
                version = state.version, hp = state.hp, maxHp = state.maxHp, linkTurns = state.linkTurns
            };
        }

        public static bool Alive(SkillActorState actor)
        {
            var state = ViewState(actor);
            return state != null && state.hp > 0;
        }

        // Run after the ordinary skill rules so a newly deployed/revived Nina
        // supervises their summon effects, while W/E cannot conjure a dead pet.
        public static void AfterCard(SkillActorState actor, CardDef card)
        {
            if (actor == null || card == null) return;
            string id = CardInstances.BaseId(card.id);
            if (id != "chloe_q" && id != "chloe_w" && id != "chloe_e" && id != "chloe_r") return;
            var state = Ensure(actor);
            if (id == "chloe_q")
                state.hp = state.hp > 0 ? Math.Min(state.maxHp, state.hp + QRepair) : state.maxHp;
            else if (id == "chloe_e" && state.hp > 0)
                state.hp = Math.Min(state.maxHp, state.hp + ERepair);
            else if (id == "chloe_r")
            {
                if (state.hp <= 0) state.hp = Math.Min(state.maxHp, RReviveHp);
                state.linkTurns = RLinkTurns;
                actor.effects.RemoveAll(x => ChloeEffect(x) && x.kind == "counter" && x.key == "soul_link");
            }
            if (state.hp > 0) ActivateNina(actor, card.owner);
            else RemoveSupport(actor);
        }

        public static CompanionAbsorption Absorb(SkillActorState actor, int incoming)
        {
            var result = new CompanionAbsorption { remaining = Math.Max(0, incoming) };
            var state = Ensure(actor);
            if (state == null || state.hp <= 0 || result.remaining == 0) return result;
            int percent = state.linkTurns > 0 ? LinkedSharePercent : NormalSharePercent;
            int share = (int)(((long)result.remaining * percent + 99) / 100);
            result.absorbed = Math.Min(state.hp, share);
            result.remaining -= result.absorbed;
            state.hp -= result.absorbed;
            if (state.hp == 0)
            {
                result.destroyed = true;
                state.linkTurns = 0;
                RemoveSupport(actor);
            }
            return result;
        }

        public static void StartTurn(SkillActorState actor)
        {
            var state = Ensure(actor);
            if (state == null) return;
            if (state.linkTurns > 0) state.linkTurns--;
            if (state.hp <= 0) RemoveSupport(actor);
        }

        public static string Summary(SkillActorState actor)
        {
            var state = ViewState(actor);
            if (state == null || state.hp <= 0) return "";
            return "니나 체력 " + state.hp + "/" + state.maxHp + " · 피해 분담 "
                + (state.linkTurns > 0 ? LinkedSharePercent : NormalSharePercent) + "%"
                + (state.linkTurns > 0 ? " · 생명 공유 " + state.linkTurns + "턴" : "");
        }

        public static List<SkillMechanicToken> Snapshot(SkillActorState actor)
        {
            var result = new List<SkillMechanicToken>();
            var state = ViewState(actor);
            if (state != null && state.hp > 0)
                result.Add(new SkillMechanicToken { owner = "클로에", key = "nina", label = "니나 체력",
                    kind = "companion", sourceCard = state.linkTurns > 0 ? "chloe_r" : "chloe_q",
                    amount = state.hp, cap = state.maxHp, remaining = state.linkTurns });
            return result;
        }

        public static string CardSummary(CardDef card)
        {
            switch (card == null ? "" : CardInstances.BaseId(card.id))
            {
                case "chloe_q": return "니나를 체력 18로 소환하거나 체력을 4 회복합니다.";
                case "chloe_w": return "니나가 살아 있을 때만 협공합니다.";
                case "chloe_e": return "니나를 6 회복하고 다음 내 턴 Q 무료 연계를 준비합니다.";
                case "chloe_r": return "니나를 부활시키고 2턴간 피해를 50% 분담합니다.";
                default: return "";
            }
        }

        public static string Description(CardDef card)
        {
            string text;
            switch (card == null ? "" : CardInstances.BaseId(card.id))
            {
                case "chloe_q":
                    text = "니나를 체력 18로 소환합니다. 이미 살아 있을 때, 니나의 체력을 4 회복합니다."; break;
                case "chloe_w":
                    text = "니나가 살아 있을 때만 니나의 협공과 인형극 실의 지속 공격이 적용됩니다."; break;
                case "chloe_e":
                    text = "니나가 살아 있을 때, 귀환 공격을 준비하고 니나의 체력을 6 회복합니다. 소멸한 니나는 회복되지 않습니다."; break;
                case "chloe_r":
                    text = "니나가 소멸한 상태일 때, 체력 12로 부활시킵니다. 2턴 동안 생명을 공유하여 방어도를 뚫고 들어오는 피해의 50%를 니나가 대신 받습니다. 살아 있는 니나의 체력은 그대로 유지됩니다."; break;
                default: return "";
            }
            return text + " 니나는 최대 체력이 18이며, 생명 공유가 없을 때도 방어도 이후 피해의 25%를 분담합니다. 분담할 피해는 올림하며 니나의 남은 체력까지만 대신 받습니다. 니나의 체력이 0이 되면 협공과 귀환 공격은 종료됩니다. 체력 소모 비용은 분담하지 않습니다.";
        }

        static bool ChloeOwner(string owner) { return owner == "클로에" || owner == "chloe"; }
        static CompanionState ViewState(SkillActorState actor)
        {
            if (actor == null) return null;
            // GUI inspection and forecasts must never migrate or sanitize the
            // live actor. Ensure only touches the private lists and state copy.
            var view = new SkillActorState {
                companion = Clone(actor.companion),
                resources = actor.resources == null ? new List<SkillResource>() : actor.resources.ToList(),
                effects = actor.effects == null ? new List<SkillTimedEffect>() : actor.effects.ToList()
            };
            return Ensure(view);
        }
        static bool ChloeEffect(SkillTimedEffect effect)
        {
            return effect != null && (ChloeOwner(effect.owner)
                || (effect.sourceCard != null && CardInstances.BaseId(effect.sourceCard).StartsWith("chloe_", StringComparison.Ordinal)));
        }
        static void ActivateNina(SkillActorState actor, string owner)
        {
            var state = actor.resources.FirstOrDefault(x => ChloeOwner(x.owner) && x.key == "nina");
            if (state == null)
            {
                state = new SkillResource { owner = ChloeOwner(owner) ? owner : "클로에", key = "nina" };
                actor.resources.Add(state);
            }
            state.label = "니나 전개 상태"; state.amount = state.cap = 1; state.isState = true;
        }
        static void RemoveSupport(SkillActorState actor)
        {
            actor.resources.RemoveAll(x => ChloeOwner(x.owner) && x.key == "nina");
            actor.effects.RemoveAll(x => ChloeEffect(x)
                && (x.key == "nina" || x.key == "threads" || x.key == "nina_recall" || x.key == "soul_link"));
        }
    }
}
