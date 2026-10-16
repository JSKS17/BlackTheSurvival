#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    static void CompanionMechanicTests()
    {
        var actor = new SkillActorState();
        Check(!CompanionMechanics.Alive(actor) && actor.companion.version == 0,
            "a read-only liveness query does not initialize the actor");
        CompanionMechanics.Ensure(actor);
        Check(actor.companion.version == 1
            && actor.companion.maxHp == CompanionMechanics.NinaMaxHp,
            "an unused companion has a valid combat-local HP record without being deployed");
        var empty = CompanionMechanics.Absorb(actor, 9);
        Check(empty.remaining == 9 && empty.absorbed == 0 && !empty.destroyed,
            "an undeployed companion cannot prevent incoming damage");
        Check(CompanionMechanics.Absorb(actor, -99).remaining == 0,
            "negative incoming values never heal or damage Nina");
        CompanionCast(actor, "chloe_q");
        Check(actor.companion.hp == 18 && CompanionMechanics.Alive(actor)
            && SkillMechanics.Resource(actor, GameDatabase.Card("chloe_q").owner, "nina") == 1,
            "Chloe Q deploys Nina at 18 separate HP and activates the Nina state");
        Check(actor.effects.Any(x => x.key == "nina" && x.kind == "summon"),
            "deploying Nina retains the existing finite summon attack schedule");
        var damage = CompanionMechanics.Absorb(actor, 8);
        Check(damage.remaining == 6 && damage.absorbed == 2 && actor.companion.hp == 16,
            "ordinary Nina absorbs 25 percent of damage after owner block");
        damage = CompanionMechanics.Absorb(actor, 1);
        Check(damage.remaining == 0 && damage.absorbed == 1 && actor.companion.hp == 15,
            "fractional damage sharing rounds up while still spending actual Nina HP");
        CompanionCast(actor, "chloe_q");
        Check(actor.companion.hp == 18, "repeat Chloe Q repairs four HP capped at Nina maximum");
        CompanionMechanics.Absorb(actor, 28);
        CompanionCast(actor, "chloe_e");
        Check(actor.companion.hp == 17 && actor.effects.Any(x => x.key == "nina_recall"),
            "Chloe E repairs six HP and keeps its living-Nina return attack");
        CompanionCast(actor, "chloe_w");
        Check(actor.effects.Any(x => x.key == "threads"), "living Nina enables W support");

        var otherResource = new SkillResource { owner = "니아", key = "arcade", amount = 2, cap = 3 };
        actor.resources.Add(otherResource);
        actor.effects.Add(new SkillTimedEffect { owner = "니아", sourceCard = "nia_q", key = "nina",
            kind = "summon", amount = 2, remaining = 2 });
        damage = CompanionMechanics.Absorb(actor, 999);
        Check(damage.absorbed == 17 && damage.remaining == 982 && damage.destroyed
            && actor.companion.hp == 0 && actor.companion.linkTurns == 0,
            "lethal pet damage is capped by remaining pet HP with all surplus reaching its owner");
        Check(!actor.resources.Any(x => x.owner == "클로에" && x.key == "nina")
            && !actor.effects.Any(x => x.sourceCard != null && x.sourceCard.StartsWith("chloe_")),
            "Nina death immediately removes her state, attacks, threads and pending recall");
        Check(actor.resources.Contains(otherResource) && actor.effects.Any(x => x.owner == "니아" && x.key == "nina"),
            "Nina death does not remove another experiment subject's unrelated state or summon");
        damage = CompanionMechanics.Absorb(actor, 4);
        Check(damage.remaining == 4 && damage.absorbed == 0 && !damage.destroyed,
            "a destroyed Nina never repeatedly absorbs or emits duplicate destruction events");
        CompanionCast(actor, "chloe_w"); CompanionCast(actor, "chloe_e");
        Check(actor.companion.hp == 0
            && !actor.effects.Any(x => x.sourceCard == "chloe_w" || x.sourceCard == "chloe_e"),
            "W and E cannot recreate dead Nina support or repair an absent Nina");

        CompanionCast(actor, "chloe_r");
        Check(actor.companion.hp == 12 && actor.companion.linkTurns == 2 && actor.companion.lethalGuardTurns == 1
            && SkillMechanics.Resource(actor, GameDatabase.Card("chloe_r").owner, "nina") == 1,
            "Chloe R revives a dead Nina at 12 HP and starts two owner turns of life sharing");
        Check(actor.effects.Any(x => x.key == "nina" && x.amount == 6)
            && !actor.effects.Any(x => x.kind == "counter" && x.key == "soul_link"),
            "R's stronger Nina attacks remain but the obsolete reflection substitute is removed");
        damage = CompanionMechanics.Absorb(actor, 10);
        Check(damage.remaining == 5 && damage.absorbed == 5 && actor.companion.hp == 7,
            "linked Nina absorbs half of the damage using her own limited HP");
        CompanionMechanics.StartTurn(actor);
        Check(actor.companion.linkTurns == 1 && actor.companion.lethalGuardTurns == 0, "first following owner turn ends lethal protection but preserves the second linked enemy turn");
        damage = CompanionMechanics.Absorb(actor, 2);
        Check(damage.absorbed == 1 && actor.companion.hp == 6, "life sharing continues through the next owner turn");
        CompanionMechanics.StartTurn(actor);
        Check(actor.companion.linkTurns == 0, "link expires at the second following owner turn start");
        damage = CompanionMechanics.Absorb(actor, 4);
        Check(damage.absorbed == 1 && actor.companion.hp == 5, "expired life sharing falls back to ordinary 25 percent interception");
        CompanionMechanics.StartTurn(actor);
        Check(actor.companion.linkTurns == 0, "link duration never underflows");
        CompanionCast(actor, "chloe_r");
        Check(actor.companion.hp == 5 && actor.companion.linkTurns == 2 && actor.companion.lethalGuardTurns == 1,
            "recasting R refreshes the link without granting free healing to a surviving Nina");
        damage = CompanionMechanics.Absorb(actor, int.MaxValue);
        Check(damage.absorbed == 5 && damage.remaining == int.MaxValue - 5 && damage.destroyed && actor.companion.lethalGuardTurns == 0,
            "even maximum integer damage cannot overflow the share calculation or grant invulnerability");

        var fresh = new SkillActorState(); CompanionCast(fresh, "chloe_r");
        Check(fresh.companion.hp == 12 && fresh.effects.Any(x => x.key == "nina"),
            "R can revive and deploy Nina without needing a preceding Q in the deck");
        var cloned = SkillMechanics.Clone(fresh);
        CompanionMechanics.Absorb(cloned, 8); CompanionMechanics.StartTurn(cloned);
        Check(fresh.companion.hp == 12 && fresh.companion.linkTurns == 2 && fresh.companion.lethalGuardTurns == 1
            && cloned.companion.hp == 8 && cloned.companion.linkTurns == 1 && cloned.companion.lethalGuardTurns == 0
            && !ReferenceEquals(fresh.companion, cloned.companion),
            "forecast clones have independent pet HP and link duration");
        var saved = (SkillActorState)Clone(fresh);
        Check(saved.companion.hp == 12 && saved.companion.linkTurns == 2 && saved.companion.lethalGuardTurns == 1
            && CompanionMechanics.Absorb(saved, 6).absorbed == 3 && fresh.companion.hp == 12,
            "serialized combat actor state preserves Nina HP without sharing a mutable companion object");

        var legacy = new SkillActorState { companion = null };
        legacy.resources.Add(new SkillResource { owner = "클로에", key = "nina", amount = 1, cap = 1 });
        legacy.effects.Add(new SkillTimedEffect { owner = "클로에", sourceCard = "chloe_r", key = "soul_link",
            kind = "counter", amount = 5, remaining = 2 });
        SkillMechanics.Ensure(legacy);
        Check(legacy.companion.hp == 18 && legacy.companion.linkTurns == 2
            && !legacy.effects.Any(x => x.key == "soul_link"),
            "legacy active-Nina saves migrate once and replace the old life-link counter");
        CompanionMechanics.Absorb(legacy, 999);
        legacy.resources.Add(new SkillResource { owner = "클로에", key = "nina", amount = 1, cap = 1 });
        SkillMechanics.Ensure(legacy);
        Check(legacy.companion.hp == 0, "stale Nina resources cannot resurrect an already migrated dead pet");
        CompanionMechanics.StartTurn(legacy);
        Check(!legacy.resources.Any(x => x.key == "nina"), "turn cleanup removes stale support for a dead companion");
        var legacyWithoutPet = new SkillActorState { companion = null };
        SkillMechanics.Ensure(legacyWithoutPet);
        Check(legacyWithoutPet.companion.hp == 0, "legacy runs without Chloe do not receive a free companion");

        var snapshot = CompanionMechanics.Snapshot(fresh).Single(x=>x.kind=="companion");
        Check(snapshot.amount == 12 && snapshot.cap == 18 && snapshot.remaining == 2 && snapshot.kind == "companion",
            "field information exposes separate Nina HP and link duration");
        Check(CompanionMechanics.Summary(fresh).Contains("12/18") && CompanionMechanics.Summary(fresh).Contains("50%"),
            "companion status text displays actual HP and the current sharing rate");
        foreach (string id in new[] { "chloe_q", "chloe_w", "chloe_e", "chloe_r" })
            Check(CompanionMechanics.CardSummary(GameDatabase.Card(id)).Length <= (id=="chloe_r"?110:50)
                && CompanionMechanics.Description(GameDatabase.Card(id)).Contains("체력 소모 비용은 분담하지 않습니다"),
                "Chloe companion summaries stay compact while full details state finite HP and self-cost exclusion: " + id);
        Check(CompanionMechanics.CardSummary(GameDatabase.Card("nia_q")) == ""
            && CompanionMechanics.Description(null) == "" && CompanionMechanics.Summary(null) == "",
            "unrelated cards and missing actors have no companion explanations");
        var readOnlyLegacy = new SkillActorState { companion = null };
        readOnlyLegacy.effects.Add(new SkillTimedEffect { owner = "클로에", sourceCard = "chloe_r", key = "soul_link",
            kind = "counter", amount = 5, remaining = 2 });
        Check(CompanionMechanics.Snapshot(readOnlyLegacy).Count == 1
            && CompanionMechanics.Summary(readOnlyLegacy).Contains("18/18")
            && CompanionMechanics.Alive(readOnlyLegacy)
            && readOnlyLegacy.companion == null && readOnlyLegacy.effects.Count == 1,
            "field UI can display legacy companion status without migrating or pruning the live save");
        var invalid = new SkillActorState { companion = new CompanionState { version = 1, hp = -5, maxHp = 999, linkTurns = 999 } };
        CompanionMechanics.Ensure(invalid);
        Check(invalid.companion.hp == 0 && invalid.companion.maxHp == 18 && invalid.companion.linkTurns == 0,
            "invalid persisted companion fields normalize to the finite intended pool");
        CompanionLethalProtectionRules();
    }

    static void CompanionLethalProtectionRules()
    {
        var actor=new SkillActorState();CompanionCast(actor,"chloe_r");
        Check(CompanionMechanics.HasLethalProtection(actor) && actor.companion.lethalGuardTurns==1,
            "R immediately starts one owner-turn of Nina-dependent lethal protection");
        string before=FieldSnapshot(actor);
        Check(CompanionMechanics.ClampOwnerDamage(actor,3,4)==2 && CompanionMechanics.ClampOwnerDamage(actor,10,4)==4
            && CompanionMechanics.ClampOwnerDamage(actor,0,99)==0 && FieldSnapshot(actor)==before,
            "lethal protection caps only a living owner's lethal damage without healing, spending or mutating the actor");
        var shared=CompanionMechanics.Absorb(actor,8);
        Check(shared.absorbed==4 && actor.companion.hp==8 && CompanionMechanics.ClampOwnerDamage(actor,3,shared.remaining)==2,
            "damage sharing is paid before a surviving Nina leaves her owner at one HP");
        actor.companion.hp=4;shared=CompanionMechanics.Absorb(actor,8);
        Check(shared.destroyed && actor.companion.lethalGuardTurns==0 && CompanionMechanics.ClampOwnerDamage(actor,3,shared.remaining)==3,
            "Nina dying to this exact shared hit immediately cancels the owner's lethal protection");
        CompanionCast(actor,"chloe_r");CompanionMechanics.StartTurn(actor);
        Check(actor.companion.lethalGuardTurns==0 && actor.companion.linkTurns==1 && !CompanionMechanics.HasLethalProtection(actor)
            && CompanionMechanics.ClampOwnerDamage(actor,3,4)==3,
            "lethal protection ends at the next owner turn while the second sharing turn continues");
        CompanionCast(actor,"chloe_r");
        Check(actor.companion.lethalGuardTurns==1 && actor.companion.hp==12,"recasting R refreshes protection without healing its surviving Nina");
        var tokens=CompanionMechanics.Snapshot(actor);
        Check(tokens.Single(x=>x.kind=="companion_guard").remaining==1 && tokens.Single(x=>x.kind=="companion").remaining==2,
            "field tokens distinguish one-turn survival protection from two-turn damage sharing");
        var copy=SkillMechanics.Clone(actor);CompanionMechanics.StartTurn(copy);
        Check(actor.companion.lethalGuardTurns==1 && copy.companion.lethalGuardTurns==0,"cloned survival duration is independent of the live state");
        var legacy=new SkillActorState {companion=new CompanionState {version=1,hp=12,linkTurns=2}};
        Check(!CompanionMechanics.HasLethalProtection(legacy),"a pre-feature saved link never gains a retroactive uncast survival window");
        actor=new SkillActorState();CompanionCast(actor,"chloe_q");
        Check(CompanionMechanics.SimulateExecutionDamage(actor,10)==10 && actor.companion.hp==18,
            "Nina without R protection preserves the previous guaranteed execution behavior");
        CompanionCast(actor,"chloe_r");
        Check(CompanionMechanics.SimulateExecutionDamage(actor,10)==9 && actor.companion.hp==13,
            "active R pays execution sharing and blocks lethal execution only while Nina survives");
        actor.companion.hp=1;
        Check(CompanionMechanics.SimulateExecutionDamage(actor,10)==10 && actor.companion.hp==0 && !CompanionMechanics.HasLethalProtection(actor),
            "an execution that kills Nina's last shared HP is no longer protected");
    }

    static void CompanionCast(SkillActorState actor, string cardId)
    {
        var before = SkillMechanics.Clone(actor); var card = GameDatabase.Card(cardId);
        SkillMechanics.AfterCard(actor, before, card, true);
        CompanionMechanics.AfterCard(actor, card);
    }
}
#endif
