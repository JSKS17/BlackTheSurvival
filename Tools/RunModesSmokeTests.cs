#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    static GameEngine ReadyMode(RunMode mode, int seed = 32101)
    {
        var engine = new GameEngine(seed, mode);
        var main = GameDatabase.Runes.First(r => r.main);
        var support = GameDatabase.Runes.First(r => !r.main && r.tree == main.tree);
        Check(engine.SetRunes(main.id, support.id), "mode fixture rune pair");
        Check(engine.SelectStartingPassive(engine.State.passiveOffers[0]), "mode fixture passive");
        foreach (var id in engine.State.draftOffers.Take(3).ToArray()) engine.ToggleDraft(id);
        Check(engine.BeginJourney(), "mode fixture starts");
        engine.State.passives.Clear(); engine.State.mainRune = engine.State.supportRune = null;
        return engine;
    }

    static GameEngine ModeBattle(RunMode mode, ZoneKind kind = ZoneKind.Wildlife, int cycle = 0)
    {
        var engine = ReadyMode(mode); engine.State.endlessCycle = cycle;
        engine.State.hp = engine.State.maxHp = 9999;
        var node = engine.AvailableNodes().First(n => n.lane == 1);
        node.assignedAnimal = "bear"; node.assignedOpponent = "nia";
        Enter(engine, kind);
        return engine;
    }

    static void ModeBossRewards(GameEngine engine, int act, string subject = "nia")
    {
        engine.State.act = act; engine.State.row = engine.State.mapRows - 2;
        engine.State.hp = engine.State.maxHp = 9999;
        engine.AvailableNodes().First(n => n.lane == 1).assignedOpponent = subject;
        Enter(engine, ZoneKind.Boss); Win(engine);
    }

    static void CompleteModeBoss(GameEngine engine, int act, string subject = "nia")
    {
        ModeBossRewards(engine, act, subject);
        Check(engine.FinishRewards(), "mode boss reward confirmation");
        if (engine.State.stage == RunStage.PassiveChoice) Check(engine.ReplacePassive(-1), "mode boss passive may be declined");
        if (engine.State.stage == RunStage.AugmentChoice) Check(engine.SkipAugment(), "mode baseline fixture declines the optional endless augment");
        if (engine.State.stage == RunStage.EquipmentSlotChoice)
            Check(engine.ToggleEquipmentSlot(GearSlot.Weapon) && engine.ConfirmEquipmentSlot(), "mode baseline fixture chooses its endless equipment expansion");
    }

    static void RunModeSelectionAndLegacy()
    {
        Check((int)RunMode.Normal == 0 && (int)RunMode.Hard == 1 && (int)RunMode.Endless == 2, "saved mode identifiers remain stable");
        Check(RunModes.Name(RunMode.Normal) == "노말" && RunModes.Name(RunMode.Hard) == "하드" && RunModes.Name(RunMode.Endless) == "무한", "three explicit mode labels");
        Check(RunModes.IsUnlocked(null, RunMode.Normal) && !RunModes.IsUnlocked(null, RunMode.Hard) && !RunModes.IsUnlocked(null, RunMode.Endless), "new profile can start only normal");
        Check(RunModes.IsUnlocked(new ResearchProfile { normalCleared = true }, RunMode.Hard)
            && !RunModes.IsUnlocked(new ResearchProfile { normalCleared = true }, RunMode.Endless)
            && RunModes.IsUnlocked(new ResearchProfile { hardCleared = true }, RunMode.Endless), "each lower clear opens the next mode");
        var old = new GameEngine(32102); var normal = new GameEngine(32102, RunMode.Normal);
        Check(old.State.mode == RunMode.Normal && old.State.endlessCycle == 0
            && old.State.rngState == normal.State.rngState && old.State.passiveOffers.SequenceEqual(normal.State.passiveOffers)
            && old.State.draftOffers.SequenceEqual(normal.State.draftOffers) && old.State.deck.SequenceEqual(normal.State.deck)
            && old.State.map.Select(n => n.kind).SequenceEqual(normal.State.map.Select(n => n.kind))
            && BossPathSnapshot(old) == BossPathSnapshot(normal), "old seed constructor and explicit normal preserve identical map, bosses, offers and RNG");
        var legacy = (RunState)Clone(old.State); legacy.archivedMapNodes = null; legacy.nextMapNodeId = 0;
        var restored = new GameEngine(legacy);
        Check(restored.State.mode == RunMode.Normal && restored.State.endlessCycle == 0 && restored.State.archivedMapNodes.Count == 0
            && restored.State.nextMapNodeId > restored.State.map.Max(n => n.id), "missing mode and node sequence fields migrate as normal");
        legacy = (RunState)Clone(old.State); legacy.mode = (RunMode)999; legacy.endlessCycle = 4;
        restored = new GameEngine(legacy);
        Check(restored.State.mode == RunMode.Normal && restored.State.endlessCycle == 0, "invalid mode normalizes without applying endless difficulty");
        Check(RunModes.EnemyHealthPercent(RunMode.Hard, 999) == 125 && RunModes.EnemyDamagePercent(RunMode.Hard, 999) == 110
            && RunModes.EnemyHealthPercent(RunMode.Endless, 2) == 165 && RunModes.EnemyDamagePercent(RunMode.Endless, 2) == 120,
            "hard is fixed and endless grows by percentage points");
    }

    static void RunModeDifficultyAndForecast()
    {
        foreach (var kind in new[] { ZoneKind.Wildlife, ZoneKind.Subject, ZoneKind.Boss })
        {
            var normal = ModeBattle(RunMode.Normal, kind); var hard = ModeBattle(RunMode.Hard, kind);
            var endless = ModeBattle(RunMode.Endless, kind);
            Check(hard.State.combat.enemyMaxHp == (normal.State.combat.enemyMaxHp * 125 + 99) / 100
                && endless.State.combat.enemyMaxHp == hard.State.combat.enemyMaxHp, "hard HP applies once to the complete spawned opponent: " + kind);
            int hardHp = hard.State.combat.enemyHp, maximum = hard.State.combat.enemyMaxHp;
            hard.State.combat.enemyHp -= 7;
            var restored = new GameEngine((RunState)Clone(hard.State));
            Check(restored.State.combat.enemyHp == hardHp - 7 && restored.State.combat.enemyMaxHp == maximum, "loading hard combat never reapplies its health multiplier: " + kind);
            Win(normal); Win(hard); Win(endless);
            Check(normal.State.rewards.credits == hard.State.rewards.credits && normal.State.rewards.credits == endless.State.rewards.credits,
                "difficulty preserves credit reward values and reward RNG: " + kind);
        }
        foreach (var mode in new[] { RunMode.Normal, RunMode.Hard, RunMode.Endless })
        {
            var engine = ModeBattle(mode); var combat = engine.State.combat;
            int rng = engine.State.rngState; string intent = engine.EnemyIntent; int expected = combat.intentDamage;
            Check(engine.State.rngState == rng, "difficulty forecast consumes no randomness");
            int before = engine.State.hp; Check(engine.EndTurn(), "mode enemy acts");
            Check(before - engine.State.hp == expected && expected == (mode == RunMode.Normal ? 11 : 13), "bear direct attack preview matches rounded mode damage");

            engine = ModeBattle(mode); combat = engine.State.combat;
            combat.animal = null; combat.enemyId = "numeric_fixture"; combat.enemyLevel = 20; combat.enemyGear.Clear();
            combat.enemyTraits = new TraitActorState(); combat.enemySkills = new SkillActorState();
            combat.enemyDeck.Clear(); combat.enemyDrawPile.Clear(); combat.enemyDiscardPile.Clear(); combat.enemyExhaustPile.Clear();
            var multiple = GameDatabase.Cards.First(c => c.damage > 0 && c.hits > 1 && c.cost <= 7);
            combat.enemyPlan = new List<string> { multiple.id }; combat.enemyPlanCosts = new List<int> { multiple.cost }; combat.enemyPlanFreeCast = new List<bool> { false };
            intent = engine.EnemyIntent; expected = combat.intentDamage; before = engine.State.hp;
            Check(engine.EndTurn() && before - engine.State.hp == expected, "multihit and conditional card damage use the same per-hit mode arithmetic as the forecast");

            engine = ModeBattle(mode); combat = engine.State.combat; combat.animal = null; combat.enemyId = "numeric_fixture";
            combat.enemyDeck.Clear(); combat.enemyDrawPile.Clear(); combat.enemyDiscardPile.Clear(); combat.enemyPlan.Clear();
            combat.enemyPlanCosts.Clear(); combat.enemyPlanFreeCast.Clear(); combat.poison = 7;
            combat.enemySkills.effects.Add(new SkillTimedEffect { owner = "numeric_fixture", key = "mode_bleed", kind = "bleed", sourceCard = "basic_attack", amount = 7, remaining = 1 });
            Check(engine.EndTurn() && engine.CombatActions.Any(a => a.enemy && a.kind == "bleed" && a.damage == 7)
                && engine.CombatActions.Any(a => a.enemy && a.cardId == "status_poison" && a.damage == 7), "poison and bleed are not multiplied as direct attacks");
        }
        foreach (var mode in new[] { RunMode.Normal, RunMode.Hard })
        {
            var engine = ReadyMode(mode); CompleteModeBoss(engine, 3);
            Check(engine.State.stage == RunStage.Won, "three-act final clear ends normal and hard only");
        }
    }

    static void EndlessCycleAndRewards()
    {
        var engine = ReadyMode(RunMode.Endless); engine.State.passives.AddRange(new[] { "isaac_p", "jackie_p", "aya_p" });
        CompleteModeBoss(engine, 1); CompleteModeBoss(engine, 2);
        var followup = EncounterChains.Events.First(e => e.chainOnly);
        engine.State.pendingEncounterFollowups.Add(new PendingEncounterFollowup { owner = followup.owner, eventId = followup.id, priorityNext = true });
        engine.State.research.unlockedReports.Add("nia"); engine.State.gear.Add("fragarach"); engine.State.foods.Add("meat");
        engine.State.scoutedActs.Add(1); engine.State.hyperloopActs.Add(2);
        ModeBossRewards(engine, 3);
        string card = engine.State.rewards.choices.First(); Check(engine.ClaimCard(card), "endless final boss card can be selected");
        var deck = engine.State.deck.ToArray(); var gear = engine.State.gear.ToArray(); var foods = engine.State.foods.ToArray();
        var passives = engine.State.passives.ToArray(); int hp = engine.State.hp, credits = engine.State.credits;
        string runId = engine.State.research.runId; int oldMax = engine.State.map.Max(n => n.id);
        Check(engine.FinishRewards() && engine.State.stage == RunStage.PassiveChoice && engine.State.endlessCycle == 0,
            "final endless boss waits for card and passive confirmation instead of escaping or immediately replacing its map");
        Check(engine.ReplacePassive(-1), "endless final passive may be declined");
        Check(engine.State.stage == RunStage.AugmentChoice && engine.State.endlessCycle == 0,
            "final passive completion waits for the endless augment before generating its next map");
        Check(engine.SkipAugment(), "old endless baseline can decline its optional augment");
        Check(engine.State.stage == RunStage.EquipmentSlotChoice && engine.ToggleEquipmentSlot(GearSlot.Head)
            && engine.ConfirmEquipmentSlot(), "a completed three-act cycle also awards one chosen equipment capacity");
        Check(engine.State.stage == RunStage.Map && engine.State.act == 1 && engine.State.row == -1 && engine.State.endlessCycle == 1
            && engine.State.activeNodeId == -1 && engine.State.combat == null && engine.State.rewards == null, "final reward completion starts the next complete three-act map");
        Check(engine.State.map.Count == GameEngine.RowsPerAct * 9 && engine.State.map.Min(n => n.id) > oldMax
            && engine.State.map.Where(n => n.kind == ZoneKind.Boss && n.act == 3).All(n => n.assignedOpponent == "nia"), "new cycle has fresh node identities and all three final paths remain Nia");
        Check(engine.State.deck.SequenceEqual(deck.Concat(new[] { card })) && engine.State.gear.SequenceEqual(gear)
            && engine.State.foods.SequenceEqual(foods) && engine.State.passives.SequenceEqual(passives) && engine.State.hp == hp && engine.State.credits == credits,
            "cycle transition preserves all rewards, equipment, passives, food, health and credits without another heal or payout");
        Check(engine.State.research.runId == runId && engine.State.research.unlockedReports.Contains("nia") && !engine.State.research.settled
            && engine.State.pendingEncounterFollowups.Any(p => p.eventId == followup.id) && engine.State.scoutedActs.Count == 0 && engine.State.hyperloopActs.Count == 0,
            "research and promises continue while per-map services reset");
        var node = engine.AvailableNodes().First(n => n.lane == 1); node.assignedAnimal = "bear"; Enter(engine, ZoneKind.Wildlife);
        Check(engine.State.combat.enemyLevel == 20 && RunModes.EnemyHealthPercent(engine.State) == 145 && RunModes.EnemyDamagePercent(engine.State) == 115,
            "later cycles keep enemy level twenty and raise difficulty");
    }

    static void EndlessSaveAndNodeIdentity()
    {
        var engine = ReadyMode(RunMode.Endless); string runId = engine.State.research.runId;
        var seen = new HashSet<int>(engine.State.map.Select(n => n.id));
        for (int cycle = 0; cycle < 2; cycle++)
        {
            for (int act = 1; act <= 3; act++) CompleteModeBoss(engine, act);
            Check(engine.State.endlessCycle == cycle + 1 && !engine.State.map.Any(n => seen.Contains(n.id)), "every new endless map uses completely new node identities");
            foreach (var location in engine.State.map) seen.Add(location.id);
            Check(engine.State.archivedMapNodes.All(n => n.visited) && engine.State.archivedMapNodes.Select(n => n.id).Distinct().Count() == engine.State.archivedMapNodes.Count
                && engine.State.research.bossNodeIds.Distinct().Count() == (cycle + 1) * 3, "visited historical nodes preserve every unique observed boss for settlement");
        }
        var resumed = new GameEngine((RunState)Clone(engine.State));
        Check(resumed.State.mode == RunMode.Endless && resumed.State.endlessCycle == 2 && resumed.State.research.runId == runId
            && BossPathSnapshot(resumed) == BossPathSnapshot(engine) && resumed.State.rngState == engine.State.rngState
            && resumed.State.archivedMapNodes.Select(n => n.id).SequenceEqual(engine.State.archivedMapNodes.Select(n => n.id)), "save retains cycle, map, journal identity, history and RNG");
        var node = resumed.AvailableNodes().First(n => n.lane == 1); node.assignedAnimal = "bear"; Enter(resumed, ZoneKind.Wildlife);
        resumed.State.combat.enemyHp -= 9; int hp = resumed.State.combat.enemyHp, maximum = resumed.State.combat.enemyMaxHp, rng = resumed.State.rngState;
        var loaded = new GameEngine((RunState)Clone(resumed.State));
        Check(loaded.State.combat.enemyHp == hp && loaded.State.combat.enemyMaxHp == maximum && loaded.State.rngState == rng
            && loaded.State.mode == RunMode.Endless && loaded.State.endlessCycle == 2, "active endless save never reapplies enemy scaling or consumes RNG");
    }

    static void EndlessEnergyAndRetirement()
    {
        var engine = ReadyMode(RunMode.Endless);
        var encounter = GameDatabase.Events.First(e => !e.chainOnly && e.options.Any(o => o.effect == "max_energy"));
        int option = Array.FindIndex(encounter.options, o => o.effect == "max_energy");
        Enter(engine, ZoneKind.Encounter); engine.State.encounterOffers = new List<string> { encounter.id };
        Check(engine.SelectEncounter(encounter.id) && engine.ChooseEventOption(option), "endless receives a real encounter energy bonus");
        int eventBonus = engine.State.maxEnergyBonus;
        for (int boss = 1; boss <= 9; boss++)
        {
            CompleteModeBoss(engine, (boss - 1) % 3 + 1);
            Check(engine.State.bossVictories == boss && engine.PlayerBaseEnergy == 5 + eventBonus + Math.Min(6, boss), "only first six endless bosses increase energy; encounter growth remains independent");
        }
        Check(RunModes.Description(RunMode.Endless).Contains("+6") && RunModes.Description(RunMode.Endless).Contains("조우"), "endless description explains the boss cap and separate encounter growth");
        foreach (var stage in new[] { RunStage.Map, RunStage.Combat, RunStage.Rewards, RunStage.PassiveChoice, RunStage.Kiosk, RunStage.Campfire, RunStage.Encounter, RunStage.AugmentChoice })
        {
            var sample = new GameEngine((RunState)Clone(engine.State)); sample.State.stage = stage;
            int hp = sample.State.hp;
            Check(sample.EndEndlessRun() && sample.State.endlessRetired && sample.State.stage == RunStage.Lost && sample.State.hp == hp
                && !sample.EndEndlessRun(), "voluntary endless retirement works once from every active stage and preserves HP: " + stage);
        }
        foreach (var mode in new[] { RunMode.Normal, RunMode.Hard, RunMode.Endless })
        {
            var sample = new GameEngine(32199, mode); Check(!sample.EndEndlessRun(), "preparation cannot settle an endless run");
            sample = ReadyMode(mode); if (mode != RunMode.Endless) Check(!sample.EndEndlessRun(), "normal and hard cannot retire as endless");
            sample.State.stage = RunStage.Won; Check(!sample.EndEndlessRun(), "won run cannot retire again");
            sample.State.stage = RunStage.Lost; Check(!sample.EndEndlessRun(), "lost run cannot retire again");
        }
        var defeated = ModeBattle(RunMode.Endless); defeated.State.hp = 1; defeated.State.combat.block = 0;
        Check(defeated.EndTurn() && defeated.State.stage == RunStage.Lost && !defeated.State.endlessRetired && defeated.State.hp == 0, "endless lethal damage is a real defeat rather than retirement");
    }
}
#endif
