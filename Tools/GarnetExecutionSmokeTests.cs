#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumia;

public static partial class CoreSmokeTests
{
    static GameEngine GarnetFixture()
    {
        var e = MechanicsFixture(); var c = e.State.combat;
        e.State.deck = new List<string> { "garnet_r" };
        c.hand.Add("garnet_r"); c.energy = e.CardCost("garnet_r");
        c.enemyHp = 60; c.enemyMaxHp = 100;
        return e;
    }
    static void GarnetExecutionAndRecall()
    {
        var card = GameDatabase.Card("garnet_r");
        Check(card.damage == 8 && card.mechanics.rules.Single(r => r.op == "delayed_damage").amount == 8 && card.exhaust,
            "Garnet reduces the initial 12+14 damage to 8+8 while preserving exhaustion");
        Check(card.freeCastCount == 1 && card.freeCastOnHit && card.freeCastTargets.SequenceEqual(new[] { card.id }),
            "Garnet gains only one hit-gated self-recast");
        var e = GarnetFixture(); var c = e.State.combat;
        Check(e.PlayCard(0) && c.enemyHp == 52 && c.energy == 0 && c.hand.SequenceEqual(new[] { card.id })
            && c.exhaustPile.Count == 0 && PileCount(c) == 1 && e.EffectiveCardCost(card.id) == 0,
            "first Garnet R recalls the same owned copy at zero energy without creating or exhausting it");
        Check(c.playerSkills.effects.Single(x => x.key == "iron_maiden").amount == 8,
            "first R installs the reduced bounded delayed damage");
        string saved = FieldSnapshot(e.State); int rng = e.State.rngState;
        var notReady = e.PreviewCardUse(0).conditions.Single(x => x.effect.Contains("남은 체력"));
        Check(!notReady.met && saved == FieldSnapshot(e.State) && rng == e.State.rngState,
            "Garnet preview explains the unmet health threshold without changing the run");
        Check(e.PlayCard(0) && c.enemyHp == 44 && c.exhaustPile.SequenceEqual(new[] { card.id })
            && c.hand.Count == 0 && !RecastAvailable(c, card.id) && !e.CombatActions.Any(x => x.execution) && PileCount(c) == 1,
            "early free recast applies ordinary reduced damage once, exhausts, and cannot grant itself again");
        e = GarnetFixture(); c = e.State.combat; e.State.deck.Add(card.id); c.hand.Add(card.id);
        Check(e.PlayCard(0) && c.hand.Count == 2 && c.discardPile.Count == 0 && PileCount(c) == 2,
            "first R recalls its temporarily unexhausted copy even when a separately owned duplicate is already in hand");
        Check(e.PlayCard(0) && c.hand.Count == 1 && c.exhaustPile.Count == 1 && c.discardPile.Count == 0 && PileCount(c) == 2,
            "duplicate R cards preserve one unplayed owned copy and one exhausted free recast without leaking a used discard");

        foreach (var mode in new[] { RunMode.Normal, RunMode.Hard, RunMode.Endless })
        {
            e = GarnetFixture(); c = e.State.combat; e.State.mode = mode;
            e.PlayCard(0); c.enemyHp = 20; c.enemyBlock = 500;
            Check(e.PreviewCardUse(0).conditions.Single(x => x.effect.Contains("남은 체력")).met,
                "Garnet preview recognizes the inclusive 20% threshold in " + mode);
            Check(e.CanPlayCard(0) && e.PlayCard(0) && c.enemyHp == 0 && c.enemyBlock == 500
                && e.State.stage == RunStage.Rewards && e.CombatActions.Last(x => x.cardId == card.id).execution,
                "Garnet executes at 20% despite shields and zero energy in " + mode);
            Check(e.State.combatHistory.Any(x => x.execution && x.detail.Contains("처형")),
                "Garnet execution is explicit in saved combat history in " + mode);
        }
        e = GarnetFixture(); c = e.State.combat; e.PlayCard(0); c.enemyHp = 21;
        Check(e.PlayCard(0) && c.enemyHp == 13 && !e.CombatActions.Last().execution,
            "recast damage bringing 21% below the threshold does not retroactively execute");
        e = GarnetFixture(); c = e.State.combat; e.PlayCard(0); c.enemyHp = 21; c.enemyMaxHp = 103;
        Check(!e.PreviewCardUse(0).conditions.Single(x => x.effect.Contains("남은 체력")).met && e.PlayCard(0) && !e.CombatActions.Last().execution,
            "threshold compares exact health ratios instead of rounding 21/103 down to 20%");
        e = GarnetFixture(); c = e.State.combat; c.enemyHp = 20; c.enemyBlock = 500;
        Check(e.PlayCard(0) && c.enemyHp == 20 && !e.CombatActions.Last().execution && RecastAvailable(c, card.id),
            "initial paid R never executes even when the target is already at 20%");
        foreach (string consumer in new[] { "garnet_q", "garnet_e" })
        {
            e = GarnetFixture(); c = e.State.combat; e.PlayCard(0); c.hand.Add(consumer); c.energy = e.CardCost(consumer);
            Check(e.PlayCard(c.hand.IndexOf(consumer)) && SkillMechanics.Resource(c.playerSkills, card.owner, "embrace") == 0,
                consumer + " consumes the original R target mark");
            c.enemyHp = 20;
            Check(!e.PreviewCardUse(c.hand.IndexOf(card.id)).conditions.Single(x => x.effect.Contains("남은 체력")).met
                && e.PlayCard(c.hand.IndexOf(card.id)) && c.enemyHp == 12 && !e.CombatActions.Last().execution,
                "free R remains usable after " + consumer + " but cannot execute without its original mark");
        }
        e = GarnetFixture(); c = e.State.combat; e.PlayCard(0); c.enemyHp = 20;
        var resumed = new GameEngine((RunState)Clone(e.State));
        Check(resumed.EffectiveCardCost(card.id) == 0 && resumed.State.combat.energy == 0
            && resumed.PlayCard(resumed.State.combat.hand.IndexOf(card.id)) && resumed.State.stage == RunStage.Rewards,
            "saved first-hit mark, one-turn free grant, and owned recalled card resume into an execution");
        e = GarnetFixture(); c = e.State.combat; e.PlayCard(0); c.retainIndex = 0;
        Check(e.BeginEndTurn() && c.hand.Count == 0 && c.exhaustPile.SequenceEqual(new[] { card.id })
            && c.retainIndex == -1 && c.retainedCardId == null && c.freeCasts.Count == 0 && PileCount(c) == 1,
            "an unused recalled R exhausts when its free window closes and cannot be retained across turns");

        bool firstMiss = false, recastHit = false;
        for (int seed = 1; seed <= 200 && (!firstMiss || !recastHit); ++seed)
        {
            if (!firstMiss)
            {
                e = GarnetFixture(); c = e.State.combat; c.enemyEvasion = 100; e.State.rngState = seed;
                e.PlayCard(0);
                if (e.CombatActions.Last().avoided > 0)
                {
                    Check(!RecastAvailable(c, card.id) && c.hand.Count == 0 && c.exhaustPile.SequenceEqual(new[] { card.id })
                        && SkillMechanics.Resource(c.playerSkills, card.owner, "embrace") == 0,
                        "a fully evaded first R exhausts normally without a mark or a free recast"); firstMiss = true;
                }
            }
            if (!recastHit)
            {
                e = GarnetFixture(); c = e.State.combat; e.PlayCard(0); c.enemyHp = 20; c.enemyEvasion = 100; e.State.rngState = seed;
                StatusMechanics.Add(c.playerStatuses,"blind","weapon_camera");
                Check(e.PreviewCardUse(0).guaranteedHit,"marked free Garnet recast preview discloses guaranteed hit");
                e.PlayCard(0);
                    Check(c.enemyHp == 0 && e.CombatActions.Last().execution && e.CombatActions.Last().avoided==0 && e.CombatActions.Last().missed==0 && !RecastAvailable(c, card.id)
                        && c.exhaustPile.SequenceEqual(new[] { card.id }),
                        "marked free Garnet recast executes through blind and total evasion, spending its one free opportunity"); recastHit = true;
            }
        }
        Check(firstMiss && recastHit, "deterministic evasion fixtures distinguish Garnet's skillshot from targeted recast");

        e = GarnetFixture(); c = e.State.combat; e.PlayCard(0); c.enemyHp = 20;
        c.enemySkills.effects.Add(new SkillTimedEffect { owner="fixture", key="life", kind="revive", sourceCard="nia_e", amount=18, remaining=2 });
        Check(e.PlayCard(0) && e.State.stage == RunStage.Combat && c.enemyHp == 18
            && e.CombatActions.Any(x => x.execution) && e.CombatActions.Any(x => x.kind == "revive"),
            "Garnet execution keeps the existing one-use lethal prevention rule");
        var boss = BossFixture("garnet"); c = boss.State.combat;
        boss.State.deck = new List<string> { card.id }; c.hand.Add(card.id); c.enemyHp = 600; c.enemyMaxHp = 1000;
        boss.PlayCard(0); c.enemyHp = 200;
        Check(boss.PlayCard(c.hand.IndexOf(card.id)) && boss.State.stage == RunStage.Rewards && boss.State.rewards.boss,
            "a boss executes at the same ratio and settles the boss reward path once");

        e = MechanicsFixture(null, "garnet"); c = e.State.combat; e.State.hp = 28; e.State.maxHp = 100;
        c.enemyDeck.Add(card.id); c.enemyDrawPile.Add(card.id); c.enemyLevel = 1;
        // Give the first R exactly its real cost without adding level-scaled damage.
        c.enemyOpeningEnergy = Math.Max(0, e.CardCost(card.id) - GameEngine.EnergyForLevel(c.enemyLevel));
        typeof(GameEngine).GetMethod("PlanEnemyTurn", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(e, null);
        Check(c.enemyPlan.SequenceEqual(new[] { card.id, card.id }) && c.enemyPlanCosts.SequenceEqual(new[] { e.CardCost(card.id), 0 })
            && c.enemyPlanFreeCast.SequenceEqual(new[] { false, true }) && c.enemyExhaustPile.SequenceEqual(new[] { card.id }),
            "enemy planner forecasts one paid R and one free R from a single exhausted card");
        Check(c.intentDamage == 28 && c.enemySkills.resources.Count == 0,
            "enemy intent includes an earned threshold execution without mutating live target marks");
        e.BeginEndTurn(); c.enemyAvailableEnergy = e.CardCost(card.id);
        Check(e.AdvanceEnemyAction() && e.State.hp == 20 && c.enemyAvailableEnergy == 0
            && c.enemyFreeCasts.Any(x => x.cardId == card.id && x.uses == 1),
            "enemy first actual hit earns its free recast at zero remaining energy");
        Check(e.AdvanceEnemyAction() && e.State.stage == RunStage.Lost && e.CombatActions.Last(x => x.enemy).execution,
            "enemy free recast uses the same exact target mark, threshold, and execution resolution");
        var summary = DescriptionSummary.CardPreview(card);
        var full = CardPresentation.Describe(card) + "\n" + CardPresentation.Rules(card);
        Check(summary.Contains("20%") && summary.Contains("무료") && summary.Contains("표식") && summary.Contains("처형"),
            "Garnet compact description includes the defining threshold, mark and free recast");
        Check(full.Contains("사용 전") && full.Contains("턴당 1회") && full.Contains("소멸") && full.Contains("디버프 설명")
            && full.LastIndexOf("디버프 설명", StringComparison.Ordinal) > full.LastIndexOf("처형", StringComparison.Ordinal),
            "Garnet full explanation retains timing, one-turn limit, exhaustion, and debuff glossary at the bottom");
    }
}
#endif
