#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumia;

public static partial class CoreSmokeTests
{
    private static void EndlessCriticalCapsAndSave()
    {
        Check(GameEngine.CriticalChanceCap == 30 && GameEngine.EndlessCriticalChanceCap == 100, "finite-mode compatibility constant and endless cap are explicit");
        foreach (RunMode mode in new[] { RunMode.Normal, RunMode.Hard, RunMode.Endless })
        {
            var e = MechanicsFixture(); e.State.mode = mode; var c = e.State.combat;
            int cap = mode == RunMode.Endless ? 100 : 30;
            Check(e.DynamicCriticalChanceCap == cap, "current run selects the correct critical cap: " + mode);
            foreach (int count in new[] { 0, 1, 4, 6, 10, 16, 17, 20, 30 })
            {
                e.State.gear = Enumerable.Repeat("light_insignia", count).ToList();
                c.enemyGear = Enumerable.Repeat("light_insignia", count).ToList();
                int expected = Math.Min(cap, count * GameDatabase.Equipment("light_insignia").critChance);
                Check(e.CritChance == expected && e.EnemyCritChance == expected, "both actors use capped equipment accumulation: " + mode + " / " + count);
            }
            int random = e.State.rngState;
            var restored = new GameEngine((RunState)Clone(e.State));
            Check(restored.DynamicCriticalChanceCap == cap && restored.CritChance == cap && restored.EnemyCritChance == cap && restored.State.rngState == random,
                "mode-specific cap and accumulated equipment survive continue without rolling RNG: " + mode);
            c.animal = "wolf";
            Check(e.EnemyCritChance == 0, "wildlife remains noncritical even when an endless fixture supplies critical equipment: " + mode);
        }
        var item = GameDatabase.Equipment("light_insignia"); int original = item.critChance;
        try
        {
            item.critChance = -6;
            foreach (RunMode mode in new[] { RunMode.Normal, RunMode.Hard, RunMode.Endless })
            {
                var e = MechanicsFixture(); e.State.mode = mode; e.State.gear.Add(item.id); e.State.combat.enemyGear.Add(item.id);
                Check(e.CritChance == 0 && e.EnemyCritChance == 0, "negative combined chance is clamped at zero in each mode: " + mode);
            }
        }
        finally { item.critChance = original; }
    }

    private static void EndlessCriticalGuaranteedHits()
    {
        foreach (int seed in new[] { 1, 2, 7, 30, 404, 1337, 77123 })
        {
            var e = MechanicsFixture(); e.State.mode = RunMode.Endless; var c = e.State.combat;
            e.State.gear = Enumerable.Repeat("light_insignia", 20).ToList();
            c.hand.Add("basic_attack"); e.State.rngState = seed;
            int normal = e.CardDamage("basic_attack"), before = c.enemyHp;
            Check(e.CritChance == 100 && e.PlayCard(0), "a fully equipped endless basic can be played at exactly 100 percent: " + seed);
            var action = e.CombatActions.Last(x => x.cardId == "basic_attack" && x.kind == null);
            Check(action.critical && action.criticalHits == 1 && before - c.enemyHp == GameEngine.CriticalBaseDamage(normal),
                "every landed 100-percent basic is exactly a 1.5x critical with ordinary rounding: " + seed);
        }
        var passive = MechanicsFixture("isaac_p"); passive.State.mode = RunMode.Endless; var p = passive.State.combat;
        passive.State.gear = Enumerable.Repeat("light_insignia", 20).ToList(); p.enemyBlock = 4; p.hand.Add("basic_attack");
        int baseDamage = passive.CardDamage("basic_attack"), bonus = passive.CardTotalDamage("basic_attack") - baseDamage, hp = p.enemyHp;
        Check(passive.PlayCard(0) && hp - p.enemyHp == GameEngine.CriticalBaseDamage(baseDamage) + bonus - 4,
            "endless guaranteed critical still multiplies only the basic hit, then adds passive damage and subtracts block");
        foreach (string id in new[] { "nia_q", "weapon_dagger", "tactical_plasma" })
        {
            var e = MechanicsFixture(); e.State.mode = RunMode.Endless; e.State.gear = Enumerable.Repeat("light_insignia", 20).ToList();
            e.State.combat.hand.Add(id); int expected = e.CardTotalDamage(id), before = e.State.combat.enemyHp;
            Check(e.PlayCard(0) && !e.CombatActions.First(x => x.cardId == id && x.kind == null).critical && before - e.State.combat.enemyHp == expected,
                "QWER, weapon and tactical skills remain noncritical at 100-percent equipment chance: " + id);
        }
        var missed = MechanicsFixture(); missed.State.mode = RunMode.Endless; missed.State.gear = Enumerable.Repeat("light_insignia", 20).ToList();
        missed.State.combat.enemyEvasion = 100; missed.State.combat.hand.Add("basic_attack"); missed.State.rngState = 30;
        var zero = new GameEngine((RunState)Clone(missed.State)); zero.State.gear.Clear();
        Check(missed.PlayCard(0) && zero.PlayCard(0) && missed.State.combat.enemyHp == 999
            && !missed.CombatActions.Last(x => x.cardId == "basic_attack" && x.kind == null).critical,
            "100-percent critical chance never bypasses the existing evasion roll");
        Check(missed.State.rngState == zero.State.rngState, "an evaded 100-percent critical attempt does not make an extra critical roll");
    }

    private static void EndlessCriticalEnemyAndPreview()
    {
        foreach (int seed in new[] { 1, 30, 1337 })
        {
            var e = MechanicsFixture(); e.State.mode = RunMode.Endless; e.State.hp = e.State.maxHp = 5000; var c = e.State.combat;
            c.enemyGear = Enumerable.Repeat("light_insignia", 20).ToList();
            c.enemyPlan = new List<string> { "basic_attack" }; c.enemyPlanCosts = new List<int> { 1 }; c.enemyPlanFreeCast = new List<bool> { false };
            int raw = (int)typeof(GameEngine).GetMethod("EnemyCardDamage", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(e, new object[] { GameDatabase.Card("basic_attack") });
            int expected = RunModes.ScaleEnemyDirectDamage(e.State, GameEngine.CriticalBaseDamage(raw));
            Check(e.EnemyCritChance == 100 && e.EnemyMechanicAssumptions.Contains("100%"), "enemy intent reports the actual endless critical chance");
            Check(e.BeginEndTurn(), "enemy guaranteed critical fixture begins its normal paced turn");
            e.State.rngState = seed; var restored = new GameEngine((RunState)Clone(e.State)); int before = e.State.hp;
            Check(e.AdvanceEnemyAction() && restored.AdvanceEnemyAction(), "live and restored endless enemy act normally");
            Check(before - e.State.hp == expected && e.CombatActions.Last(x => x.enemy && x.cardId == "basic_attack" && x.kind == null).critical,
                "landed enemy basic is guaranteed critical and retains mode scaling order: " + seed);
            Check(restored.State.hp == e.State.hp && restored.State.rngState == e.State.rngState
                && restored.CombatActions.Last(x => x.enemy && x.cardId == "basic_attack" && x.kind == null).critical,
                "saved enemy action preserves the 100-percent critical result and deterministic stream: " + seed);
        }
        var preview = MechanicsFixture(); preview.State.mode = RunMode.Endless; var combat = preview.State.combat;
        preview.State.gear = Enumerable.Repeat("light_insignia", 20).ToList(); combat.enemyGear = Enumerable.Repeat("light_insignia", 20).ToList();
        combat.hand.Add("basic_attack"); combat.enemyHand.Add("basic_attack");
        string beforeState = FieldSnapshot(preview.State);
        Check(preview.PreviewCardUse(0).criticalChance == 100 && preview.PreviewCardUse("basic_attack", true).criticalChance == 100,
            "both card information previews expose the full endless chance");
        Check(beforeState == FieldSnapshot(preview.State), "reading 100-percent preview leaves every saved field unchanged");
        combat.hand[0] = "nia_q";
        Check(preview.PreviewCardUse(0).criticalChance == 0, "skill information does not inherit the 100-percent basic chance");
    }
}
#endif
