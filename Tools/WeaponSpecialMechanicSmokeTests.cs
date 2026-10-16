#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumia;

public static partial class CoreSmokeTests
{
    static void WeaponSpecialMechanics()
    {
        TonfaRepeatedReflection();
        IsaacArrestShieldDestruction();
    }

    static void ArmWeaponCounter(SkillActorState actor, string cardId)
    {
        var before = SkillMechanics.Clone(actor);
        SkillMechanics.AfterCard(actor, before, GameDatabase.Card(cardId), true);
    }
    static void WeaponEnemyPlan(GameEngine e, params string[] cards)
    {
        var c = e.State.combat; c.enemyLevel = 20;
        c.enemyPlan = cards.ToList();
        c.enemyPlanCosts = cards.Select(e.CardCost).ToList();
        c.enemyPlanFreeCast = cards.Select(_ => false).ToList();
    }
    static void TonfaRepeatedReflection()
    {
        var tonfa = GameDatabase.Card("weapon_tonfa");
        int reflection = tonfa.mechanics.rules.Single(x => x.op == "counter").amount;
        var actor = new SkillActorState(); ArmWeaponCounter(actor, tonfa.id);
        Check(SkillMechanics.Counters(actor, 3).Count == 3 && SkillMechanics.Counters(actor, 2).Count == 2,
            "Tonfa reflects every direct hit across multiple attacks and multiple hits in one attack");
        var effect = actor.effects.Single(x => x.kind == "counter"); effect.uses = 97;
        var saved = SkillMechanics.Clone(actor);
        Check(SkillMechanics.Counters(saved).Single().amount == reflection && saved.effects.Single().uses == 97,
            "Tonfa's old saved once-per-turn uses no longer limit reflection or grow without bound");
        Check(SkillMechanics.Counters(saved, 0).Count == 0, "Tonfa does not reflect a zero-hit attack");
        SkillMechanics.StartTurn(saved);
        Check(SkillMechanics.Counters(saved).Count == 0, "Tonfa still expires at its original next-own-turn timing");
        effect.remaining = 0;
        Check(SkillMechanics.Counters(actor).Count == 0, "a stale expired reflection cannot activate");
        actor = new SkillActorState(); ArmWeaponCounter(actor, "nicky_w");
        Check(SkillMechanics.Counters(actor, 3).Count == 1 && SkillMechanics.Counters(actor).Count == 0,
            "Nicky and other counter effects retain their original once-per-turn limit");
        SkillMechanics.StartTurn(actor);
        Check(SkillMechanics.Counters(actor).Count == 1, "other counters retain their second protected turn");

        var e = MechanicsFixture(); var c = e.State.combat;
        e.State.hp = e.State.maxHp = 999; c.hand.Add(tonfa.id);
        Check(e.PlayCard(0), "player casts the real Tonfa D to arm reflection");
        c.block = 999; e.CombatActions.Clear(); WeaponEnemyPlan(e, "basic_attack", "basic_attack", "basic_attack");
        Check(e.BeginEndTurn() && e.AdvanceEnemyAction(), "first enemy basic hits the protected player");
        var resumed = new GameEngine((RunState)Clone(e.State));
        Check(e.AdvanceEnemyAction() && e.AdvanceEnemyAction(), "later enemy basics hit in the same turn");
        Check(e.CombatActions.Count(x => x.kind == "counter" && !x.enemy) == 3
            && e.CombatActions.Where(x => x.kind == "counter").All(x => x.damage == reflection),
            "player reflects three fully shielded hits separately in a single enemy turn");
        Check(resumed.AdvanceEnemyAction() && resumed.AdvanceEnemyAction()
            && resumed.State.combat.enemyHp == c.enemyHp && resumed.State.hp == e.State.hp,
            "saving after the first reflected hit retains repeat reflection and exact outcomes");
        Check(e.AdvanceEnemyAction() && !c.playerSkills.effects.Any(x => x.sourceCard == tonfa.id && x.kind == "counter"),
            "the protection ends on return to the player's next turn");

        e = MechanicsFixture(); c = e.State.combat; ArmWeaponCounter(c.enemySkills, tonfa.id);
        ArmWeaponCounter(c.playerSkills, tonfa.id); c.block = 999;
        c.hand.AddRange(new[] { "basic_attack", "basic_attack" });
        Check(e.PlayCard(0) && e.PlayCard(0)
            && e.CombatActions.Count(x => x.kind == "counter" && x.enemy) == 2
            && !e.CombatActions.Any(x => x.kind == "counter" && !x.enemy),
            "enemy Tonfa repeats for player attacks while counter damage never recursively reflects");

        e = MechanicsFixture(); c = e.State.combat; ArmWeaponCounter(c.enemySkills, tonfa.id);
        c.hand.Add("aya_w"); Check(e.PlayCard(0), "the player uses a real multihit skill against enemy Tonfa");
        var direct = e.CombatActions.Last(x => x.cardId == "aya_w" && x.kind == null);
        int hitCount = GameDatabase.Card("aya_w").hits - direct.avoided - direct.missed;
        Check(hitCount > 1 && e.CombatActions.Count(x => x.kind == "counter" && x.enemy) == hitCount,
            "each landed hit of a multihit card produces its own Tonfa reflection");
        e = MechanicsFixture(); c = e.State.combat; c.enemyEvasion = 100;
        ArmWeaponCounter(c.enemySkills, tonfa.id); c.hand.Add("basic_attack");
        Check(e.PlayCard(0) && !e.CombatActions.Any(x => x.kind == "counter"), "a fully evaded attack does not reflect");
        string tonfaPreview = DescriptionSummary.CardPreview(tonfa);
        Check(tonfaPreview.Contains("직접 공격") && tonfaPreview.Contains("반격")
            && (tonfaPreview.Contains("맞을 때마다") || tonfaPreview.Contains("횟수 제한 없음"))
            && CardPresentation.Describe(tonfa).Contains("횟수 제한 없음")
            && !CardPresentation.Describe(tonfa).Contains("턴당 1회"),
            "Tonfa summary and full detail consistently explain unlimited direct-hit reflection");
        Check(CardPresentation.Describe(GameDatabase.Card("nicky_w")).Contains("턴당 1회"),
            "unrelated counter descriptions keep their restriction");
    }

    static void IsaacArrestShieldDestruction()
    {
        var e = MechanicsFixture(); var c = e.State.combat; var card = GameDatabase.Card("isaac_e");
        c.hand.AddRange(new[] { card.id, card.id }); c.enemyBlock = 50;
        string before = FieldSnapshot(e.State); int rng = e.State.rngState;
        var preview = e.PreviewCardUse(0);
        Check(!preview.conditions.Single(x => x.effect.Contains("방어도를 모두")).met
            && preview.damageNote.Contains("보호막을 파괴하지 않습니다")
            && before == FieldSnapshot(e.State) && rng == e.State.rngState,
            "first Isaac E forecast discloses unmet preparation without mutating state or RNG");
        Check(e.PlayCard(0) && c.enemyBlock == 50 - card.damage
            && e.CombatActions.Last(x => x.cardId == card.id && x.kind == null).destroyedBlock == 0,
            "first E deals its ordinary shielded damage and does not destroy protection");
        c.enemyBlock = 50; int hp = c.enemyHp; int damage = e.CardTotalDamage(card.id);
        preview = e.PreviewCardUse(0);
        Check(preview.conditions.Single(x => x.effect.Contains("방어도를 모두")).met
            && preview.damageNote.Contains("대상 방어도 50"), "prepared E preview exposes hit gate and exact current shield");
        Check(e.PlayCard(0) && c.enemyBlock == 0 && hp - c.enemyHp == damage,
            "prepared E destroys the complete shield before applying direct damage");
        var action = e.CombatActions.Last(x => x.cardId == card.id && x.kind == null);
        Check(action.destroyedBlock == 50 && action.absorbed == 0
            && e.State.combatHistory.Any(x => x.destroyedBlock == 50 && x.detail.Contains("직접 피해 전 제거")),
            "shield destruction is tracked separately from damage and absorption in persistent combat history");
        Check(!SkillMechanics.BreaksBlock(c.playerSkills, card), "E2 still consumes its prepared arrest state");

        e = MechanicsFixture(null, "aya"); c = e.State.combat; c.enemyBlock = 30;
        RecastResource(c.playerSkills, card.id, "arrest", 1, 1); c.hand.Add(card.id);
        Check(e.PlayCard(0) && c.enemyBlock == 0
            && e.CombatActions.Last(x => x.cardId == card.id && x.kind == null).destroyedBlock == 35,
            "E2 also destroys Aya's five shielding created immediately before the incoming hit");

        foreach (bool miss in new[] { false, true })
        {
            bool found = false;
            for (int seed = 1; seed < 80 && !found; seed++)
            {
                e = MechanicsFixture(); c = e.State.combat; c.enemyBlock = 50; e.State.rngState = seed;
                RecastResource(c.playerSkills, card.id, "arrest", 1, 1);
                if (miss) StatusMechanics.Add(c.playerStatuses, "blind", "weapon_camera"); else c.enemyEvasion = 100;
                c.hand.Add(card.id); e.PlayCard(0); action = e.CombatActions.Last(x => x.cardId == card.id && x.kind == null);
                found = miss ? action.missed > 0 : action.avoided > 0;
                if (found) Check(c.enemyBlock == 50 && action.destroyedBlock == 0 && action.damage == 0,
                    "a prepared E that " + (miss ? "misses accuracy" : "is evaded") + " never destroys a shield");
            }
            Check(found, "deterministic fixture exercised E2's " + (miss ? "accuracy miss" : "evasion"));
        }

        e = MechanicsFixture("aya_p"); c = e.State.combat; c.block = 30;
        e.State.hp = e.State.maxHp = 999; RecastResource(c.enemySkills, card.id, "arrest", 1, 1);
        WeaponEnemyPlan(e, card.id);
        before = FieldSnapshot(e.State); rng = e.State.rngState; preview = e.PreviewCardUse(card.id, true);
        Check(preview.conditions.Single(x => x.effect.Contains("방어도를 모두")).met
            && preview.damageNote.Contains("대상 방어도 30") && before == FieldSnapshot(e.State) && rng == e.State.rngState,
            "enemy E2 uses the same read-only shield destruction preview");
        Check(e.BeginEndTurn() && e.AdvanceEnemyAction() && c.block == 0
            && e.CombatActions.Last(x => x.cardId == card.id && x.kind == null).destroyedBlock == 35,
            "enemy E2 destroys the player's existing and immediate passive shielding before health damage");
        var loaded = new GameEngine((RunState)Clone(e.State));
        Check(loaded.State.combatHistory.Any(x => x.destroyedBlock == 35), "shield destruction history survives save/load");

        e = MechanicsFixture(); c = e.State.combat; c.block = 50; e.State.hp = e.State.maxHp = 999;
        WeaponEnemyPlan(e, card.id, card.id);
        before = FieldSnapshot(c.enemySkills); rng = e.State.rngState;
        string intent = e.EnemyIntent;
        Check(intent.Contains(card.name) && before == FieldSnapshot(c.enemySkills) && rng == e.State.rngState,
            "sequential E intent simulates preparation and destruction on cloned skills without consuming live state");
        Check(e.BeginEndTurn() && e.AdvanceEnemyAction(), "enemy first E prepares arrest through ordinary block");
        Check(c.block > 0 && e.AdvanceEnemyAction() && c.block == 0,
            "enemy second planned E destroys the shield left after the first E");
        Check(e.CombatActions.Last(x => x.cardId == card.id && x.kind == null).destroyedBlock > 0,
            "the sequential enemy plan and actual second E agree on prepared shield destruction");
        Check(DescriptionSummary.CardPreview(card).Contains("방어도를 모두")
            && CardPresentation.Describe(card).Contains("피해를 주기 전에 적의 방어도를 모두")
            && CardPresentation.Describe(card).Contains("회피되거나 빗나가면"),
            "Isaac E short and full descriptions disclose preparation, pre-damage shield removal and miss behavior");
        Check(!SkillMechanics.BreaksBlock(c.enemySkills, GameDatabase.Card("isaac_w")),
            "Isaac W does not accidentally inherit the old original-game shield destruction mechanic");
    }
}
#endif
