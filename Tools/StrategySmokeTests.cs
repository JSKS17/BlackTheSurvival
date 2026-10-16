#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumia;

public static partial class CoreSmokeTests
{
    private static GameEngine StrategyReady(int seed)
    {
        var e = Ready(seed);
        e.State.passives.Clear(); e.State.mainRune = e.State.supportRune = null;
        return e;
    }

    private static int StrategySelectEvent(GameEngine e, EventDef def, Func<EventOption, bool> predicate, bool supply = true)
    {
        Enter(e, ZoneKind.Encounter);
        e.State.encounterOffers = new List<string> { def.id };
        Check(e.SelectEncounter(def.id), "select authored strategy encounter");
        int index = Array.FindIndex(def.options, o => predicate(o));
        Check(index >= 0, "authored strategy choice exists");
        if (supply)
        {
            e.State.credits = 1000;
            var option = def.options[index];
            if (!string.IsNullOrEmpty(option.requiredFoodId)) e.State.foods.Add(option.requiredFoodId);
            if (!string.IsNullOrEmpty(option.requiredObjectId)) e.State.objects.Add(option.requiredObjectId);
        }
        return index;
    }

    private static void StrategicUpgradeChoice()
    {
        var def = GameDatabase.Events.First(ev => ev.options.Any(o => o.effect == "upgrade_card" && string.IsNullOrEmpty(o.followupId)));
        var e = StrategyReady(1401);
        e.State.deck = new List<string> { "nia_q", "nia_q", "nia_w", "basic_attack" };
        int option = StrategySelectEvent(e, def, o => o.effect == "upgrade_card");
        int rng = e.State.rngState;
        Check(e.ChooseEventOption(option) && e.State.cardUpgradePending && e.State.stage == RunStage.Encounter && e.State.upgrades.Count == 0,
            "upgrade encounter waits for explicit target instead of randomly changing a card");
        Check(e.State.message.Contains("선택한 한 장만 강화") && e.State.message.Contains("같은 종류의 다른 카드")
            && e.State.message.Contains("이후 획득하는 카드는 강화되지 않습니다") && e.State.log.Last() == e.State.message,
            "actual upgrade encounter guidance says only the selected copy is upgraded and excludes other or future copies");
        Check(e.State.rngState == rng && !e.ChooseEventOption(option), "pending upgrade cannot redraw or repeat the event reward");
        var choices = e.CardUpgradeOptions(1);
        Check(choices.Count >= 2 && e.UpgradeCardIndices().SequenceEqual(new[] { 0, 1, 2, 3 }), "duplicate cards remain independently selectable upgrade targets");
        var special = choices.First(o => o.id != "power");
        var baseCard = GameDatabase.Card("nia_q");
        int baseGain = baseCard.mechanics.rules.First(r => r.op == "gain").amount;
        var loaded = new GameEngine((RunState)Clone(e.State));
        Check(loaded.State.cardUpgradePending && loaded.UpgradeCardIndices().SequenceEqual(e.UpgradeCardIndices()), "pending target selection survives save and load");
        Check(!loaded.UpgradeCard(-1, special.id) && !loaded.UpgradeCard(loaded.State.deck.Count, special.id) && !loaded.UpgradeCard(1, "not_a_branch") && loaded.State.cardUpgradePending,
            "invalid target or branch keeps the upgrade pending without spending it");
        Check(loaded.UpgradeCard(1, special.id) && !loaded.State.cardUpgradePending && loaded.State.stage == RunStage.Map,
            "choosing a branch completes the encounter exactly once");
        string upgraded = loaded.State.deck[1];
        Check(upgraded != "nia_q" && GameDatabase.Card(upgraded) == baseCard && loaded.State.deck[0] == "nia_q",
            "only the selected second Nia Q copy receives an instance reference while database lookup still finds its base skill");
        Check(!loaded.UpgradeCard(1, "power") && !loaded.UpgradeCardIndices().Contains(1) && loaded.UpgradeCardIndices().Contains(0), "an upgraded copy rejects another branch while its unupgraded twin remains available");
        var modified = loaded.PlayerCard(upgraded);
        Check(GameDatabase.Card("nia_q").mechanics.rules.First(r => r.op == "gain").amount == baseGain && modified.mechanics.rules.First(r => r.op == "gain").amount == baseGain + 1,
            "resource branch changes only the selected player copy and preserves the shared database");
        Check(DescriptionSummary.CardPreview(modified).Contains("블록을 2 남깁니다") && DescriptionSummary.CardPreview(baseCard).Contains("블록을 1 남깁니다"),
            "upgraded Nia Q summary reflects the actual block gain without altering base previews");
        Check(DescriptionSummary.CardPreview(StrategyMechanics.Apply(GameDatabase.Card("nia_w"), "arcade")).Contains("블록당 피해 6"),
            "upgraded Nia W summary reflects its actual per-block damage");
        Check(loaded.PlayerCard("nia_q").mechanics.rules.First(r => r.op == "gain").amount == baseGain
            && !string.IsNullOrEmpty(loaded.CardUpgradeLabel(upgraded)) && string.IsNullOrEmpty(loaded.CardUpgradeLabel("nia_q")),
            "upgraded and unupgraded copies display distinct labels and resolve distinct skill effects");
        loaded = new GameEngine((RunState)Clone(loaded.State));
        Enter(loaded, ZoneKind.Wildlife); var c = loaded.State.combat;
        c.enemyHp = c.enemyMaxHp = 10000; c.enemyEvasion = 0; c.hand = new List<string> { upgraded, "nia_q" }; c.energy = 100;
        Check(loaded.PlayCard(0) && SkillMechanics.Resource(c.playerSkills, "니아", "blocks") == 2, "Nia branch changes actual block generation");
        Check(loaded.PlayCard(0) && SkillMechanics.Resource(c.playerSkills, "니아", "blocks") == 3, "resource upgrade keeps the original maximum of three");

        e = StrategyReady(1402); e.State.deck = new List<string> { "nia_e", "basic_attack" };
        option = StrategySelectEvent(e, def, o => o.effect == "upgrade_card"); e.ChooseEventOption(option);
        var efficiency = e.CardUpgradeOptions("nia_e").First(o => o.id == "efficiency");
        int sourceCost = GameDatabase.Card("nia_e").cost;
        Check(e.UpgradeCard(0, efficiency.id) && e.PlayerCard(e.State.deck[0]).cost == Math.Max(1, sourceCost - 1), "efficiency branch lowers the selected copy's cost with a one-cost floor");
        Check(GameDatabase.Card("nia_e").cost == sourceCost && !e.CardUpgradeOptions("basic_attack").Any(o => o.id == "efficiency"), "efficiency does not mutate enemy data or turn basic attacks into free cards");
        StrategicIndependentUpgradeBranches();
        StrategicUpgradePilesAndRetention();
        StrategicUpgradeExhaustAndRemoval();
        StrategicUpgradeLegacyMigration();
        StrategicUpgradedFreeRecall();
    }

    private static string StrategyUpgradeCopy(GameEngine e, int index, string branch)
    {
        var upgradeEvent = GameDatabase.Events.First(ev => ev.options.Any(o => o.effect == "upgrade_card" && string.IsNullOrEmpty(o.followupId)));
        int choice = StrategySelectEvent(e, upgradeEvent, o => o.effect == "upgrade_card");
        Check(e.ChooseEventOption(choice) && e.State.cardUpgradePending, "an actual encounter supplies the pending copy upgrade");
        Check(e.CardUpgradeOptions(index).Any(o => o.id == branch), "selected copy offers requested upgrade branch");
        Check(e.UpgradeCard(index, branch), "selected copy upgrade completes");
        return e.State.deck[index];
    }

    private static void StrategicIndependentUpgradeBranches()
    {
        var e = StrategyReady(1450);
        e.State.deck = new List<string> { "nia_e", "nia_e", "basic_attack", "basic_attack" };
        string efficiency = StrategyUpgradeCopy(e, 0, "efficiency");
        var upgradeEvent = GameDatabase.Events.First(ev => ev.options.Any(o => o.effect == "upgrade_card" && string.IsNullOrEmpty(o.followupId)));
        int upgradeChoice = StrategySelectEvent(e, upgradeEvent, o => o.effect == "upgrade_card");
        Check(e.ChooseEventOption(upgradeChoice) && e.State.cardUpgradePending, "a second real encounter offers another individual upgrade");
        Check(!e.UpgradeCard(0, "power") && !e.UpgradeCard(efficiency, "power") && e.State.cardUpgradePending,
            "an already upgraded copy cannot spend another pending upgrade or redirect it to its ordinary twin");
        Check(e.UpgradeCard("nia_e", "power"), "legacy string callers upgrade only the first eligible ordinary copy");
        string power = e.State.deck[1];
        int baseCost = GameDatabase.Card("nia_e").cost, baseBlock = GameDatabase.Card("nia_e").block;
        Check(efficiency != power && e.PlayerCard(efficiency).cost == Math.Max(1, baseCost - 1)
            && e.PlayerCard(power).cost == baseCost && e.PlayerCard(power).block == baseBlock + 3
            && e.PlayerCard(efficiency).block == baseBlock,
            "two Nia E copies retain independent exclusive branches with no cross-copy cost or block changes");
        string attack = StrategyUpgradeCopy(e, 2, "power");
        int baseDamage = e.CardDamage("basic_attack");
        Check(e.CardDamage(attack) == baseDamage + 3 && e.State.deck[3] == "basic_attack", "power increases only the selected basic attack's damage");
        e.State.deck.Add("nia_e");
        var rewardEvent = GameDatabase.Events.First(ev => ev.options.Any(o => o.effect == "card" && o.cardId == "basic_attack"));
        int rewardChoice = StrategySelectEvent(e, rewardEvent, o => o.effect == "card" && o.cardId == "basic_attack");
        Check(e.ChooseEventOption(rewardChoice) && e.State.deck.Count == 6, "a real encounter grants one new basic attack after an earlier copy was upgraded");
        Check(e.PlayerCard(e.State.deck[4]).cost == baseCost && e.PlayerCard(e.State.deck[4]).block == baseBlock
            && e.CardDamage(e.State.deck[5]) == baseDamage && string.IsNullOrEmpty(e.CardUpgradeLabel(e.State.deck[4]))
            && string.IsNullOrEmpty(e.CardUpgradeLabel(e.State.deck[5])), "newly obtained copies never inherit an earlier copy's branch or display label");
        var saved = new GameEngine((RunState)Clone(e.State));
        Check(saved.State.deck.SequenceEqual(e.State.deck) && saved.PlayerCard(efficiency).cost == Math.Max(1, baseCost - 1)
            && saved.PlayerCard(power).block == baseBlock + 3 && saved.CardDamage(attack) == baseDamage + 3
            && saved.UpgradeCardIndices().SequenceEqual(new[] { 3, 4, 5 }), "saving multiple branches preserves exact upgraded copies and all remaining upgrade targets");
        Enter(saved, ZoneKind.Wildlife); var c = saved.State.combat;
        c.enemyHp = c.enemyMaxHp = 10000; c.enemyEvasion = c.enemyBlock = 0; c.hand = new List<string> { attack, "basic_attack", efficiency, power }; c.energy = 100;
        int hp = c.enemyHp;
        Check(saved.PlayCard(0) && hp - c.enemyHp == baseDamage + 3, "upgraded basic attack deals its additional damage in actual combat");
        hp = c.enemyHp;
        Check(saved.PlayCard(0) && hp - c.enemyHp == baseDamage, "unupgraded duplicate deals original damage in the same battle");
        Check(saved.EffectiveCardCost(efficiency) == Math.Max(1, baseCost - 1) && saved.EffectiveCardCost(power) == baseCost,
            "combat prices resolve individual upgrade branches without changing the matching base skill");
        int energy = c.energy;
        Check(saved.PlayCard(0) && c.energy == energy - Math.Max(1, baseCost - 1) && c.exhaustPile.Contains(efficiency),
            "cost reduction is spent on the selected copy and its identity survives skill exhaustion");
    }

    private static void StrategicUpgradePilesAndRetention()
    {
        var e = StrategyReady(1451); e.State.deck = new List<string> { "basic_attack", "basic_attack", "basic_guard", "basic_guard" };
        string upgraded = StrategyUpgradeCopy(e, 0, "power");
        Enter(e, ZoneKind.Wildlife); var c = e.State.combat; e.State.hp = e.State.maxHp = 9999;
        c.enemyHp = c.enemyMaxHp = 10000; c.enemyEvasion = 0; c.energy = 100;
        c.hand = new List<string> { "basic_attack", upgraded }; c.drawPile = new List<string> { "basic_guard", "basic_guard" }; c.discardPile.Clear(); c.exhaustPile.Clear();
        Check(e.ToggleRetainCard(1) && e.BeginEndTurn() && c.retainedCardId == upgraded, "retention reserves the exact upgraded copy instead of a same-kind twin");
        e = new GameEngine((RunState)Clone(e.State)); c = e.State.combat;
        Check(c.retainedCardId == upgraded && e.CardDamage(c.retainedCardId) == e.CardDamage("basic_attack") + 3,
            "mid-enemy-turn save preserves the retained upgraded copy and its strength");
        StrategyFinishEnemyTurn(e);
        Check(c.hand.Count(id => id == upgraded) == 1 && c.hand.Contains("basic_attack") && !c.drawPile.Contains(upgraded) && !c.discardPile.Contains(upgraded),
            "retained upgraded card returns once while other copies reshuffle and draw separately");
        c.energy = 100; int before = PileCount(c);
        Check(e.PlayCard(c.hand.IndexOf(upgraded)) && c.discardPile.Contains(upgraded) && !c.hand.Contains(upgraded) && PileCount(c) == before,
            "playing the upgraded card moves its exact reference to discard without changing card count");
        c.discardPile.AddRange(c.hand); c.hand.Clear(); c.drawPile.Clear();
        var expected = c.discardPile.OrderBy(id => id).ToArray();
        typeof(GameEngine).GetMethod("DrawCards", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(e, new object[] { 12 });
        Check(c.hand.OrderBy(id => id).SequenceEqual(expected) && c.hand.Count(id => id == upgraded) == 1
            && c.discardPile.Count == 0 && c.drawPile.Count == 0, "discard reshuffle and draw preserve the multiset of upgraded and ordinary references");
        var restored = new GameEngine((RunState)Clone(e.State));
        Check(restored.State.combat.hand.SequenceEqual(c.hand) && restored.CardUpgradeLabel(upgraded) == e.CardUpgradeLabel(upgraded),
            "drawn upgraded card order and label persist after reloading combat");
    }

    private static void StrategicUpgradeExhaustAndRemoval()
    {
        var e = StrategyReady(1452); e.State.deck = new List<string> { "tactical_blink", "tactical_blink", "basic_attack", "basic_attack" };
        string upgraded = StrategyUpgradeCopy(e, 0, "efficiency");
        Enter(e, ZoneKind.Wildlife); var c = e.State.combat; c.enemyHp = c.enemyMaxHp = 10000;
        c.hand = new List<string> { upgraded, "tactical_blink" }; c.drawPile.Clear(); c.discardPile.Clear(); c.exhaustPile.Clear(); c.energy = 100;
        Check(e.PlayCard(0) && c.exhaustPile.SequenceEqual(new[] { upgraded }) && c.hand.SequenceEqual(new[] { "tactical_blink" }),
            "exhausting an upgraded F card leaves the same-type unupgraded card in hand");
        e = new GameEngine((RunState)Clone(e.State)); c = e.State.combat;
        Check(c.exhaustPile.SequenceEqual(new[] { upgraded }) && !string.IsNullOrEmpty(e.CardUpgradeLabel(upgraded))
            && string.IsNullOrEmpty(e.CardUpgradeLabel(c.hand[0])), "exhaust pile save retains upgraded identity without promoting another copy");
        e.State.stage = RunStage.Map; e.State.combat = null;
        Enter(e, ZoneKind.Campfire); Check(e.ChooseCamp(false) && e.RemoveCampCards(new[] { 0 }), "camp rest can remove precisely the upgraded deck copy");
        Check(!e.State.deck.Contains(upgraded) && e.State.deck[0] == "tactical_blink" && string.IsNullOrEmpty(e.CardUpgradeLabel(e.State.deck[0]))
            && e.PlayerCard(e.State.deck[0]).cost == GameDatabase.Card("tactical_blink").cost,
            "removing an upgraded copy never transfers its upgrade to the adjacent ordinary copy");
        Check(e.LeaveCamp(), "camp removal returns to the route");
        e = new GameEngine((RunState)Clone(e.State));
        e.State.deck.Add("tactical_blink");
        string second = StrategyUpgradeCopy(e, e.State.deck.Count - 1, "efficiency");
        Check(second != upgraded && e.State.deck[0] == "tactical_blink" && string.IsNullOrEmpty(e.CardUpgradeLabel(e.State.deck[0])),
            "new upgrade identities are not reused after removing a copy and reloading");
    }

    private static void StrategicUpgradeLegacyMigration()
    {
        var legacy = StrategyReady(1453);
        legacy.State.cardInstanceVersion = 0; legacy.State.nextCardInstanceId = 1;
        legacy.State.deck = new List<string> { "nia_q", "nia_q", "nia_e", "nia_e", "basic_attack", "basic_attack" };
        legacy.State.upgrades.Add("nia_q"); legacy.State.upgrades.Add("basic_attack");
        legacy.State.cardUpgrades.Add(new CardUpgradeSelection { cardId = "nia_e", branchId = "efficiency" });
        Enter(legacy, ZoneKind.Wildlife); var c = legacy.State.combat;
        c.hand = new List<string> { "nia_q", "nia_e" }; c.drawPile = new List<string> { "nia_q" };
        c.discardPile = new List<string> { "nia_e", "basic_attack" }; c.exhaustPile.Clear(); c.retainedCardId = "basic_attack";
        var migrated = new GameEngine((RunState)Clone(legacy.State)); c = migrated.State.combat;
        Check(migrated.State.cardInstanceVersion > 0 && migrated.State.deck.Distinct().Count() == 6
            && migrated.State.deck.All(id => !string.IsNullOrEmpty(migrated.CardUpgradeLabel(id))),
            "legacy type-wide upgrades migrate once to six independently identified current deck copies");
        var actual = c.hand.Concat(c.drawPile).Concat(c.discardPile).Concat(c.exhaustPile).Concat(new[] { c.retainedCardId }).ToArray();
        Check(actual.OrderBy(id => id).SequenceEqual(migrated.State.deck.OrderBy(id => id)),
            "legacy in-progress hand, draw, discard, and retained card map to owned upgraded copies one-to-one");
        Check(migrated.State.deck.Where(id => GameDatabase.Card(id).id == "nia_q").All(id => migrated.CardDamage(id) == migrated.CardDamage("nia_q") + 3)
            && migrated.State.deck.Where(id => GameDatabase.Card(id).id == "nia_e").All(id => migrated.PlayerCard(id).cost == Math.Max(1, GameDatabase.Card("nia_e").cost - 1)),
            "legacy numeric and branch strengths are preserved on every copy the old save already owned");
        migrated.State.deck.Add("nia_q"); migrated.State.deck.Add("nia_e"); migrated.State.deck.Add("basic_attack");
        var once = migrated.State.deck.ToArray();
        migrated = new GameEngine((RunState)Clone(migrated.State));
        Check(migrated.State.deck.SequenceEqual(once) && migrated.UpgradeCardIndices().SequenceEqual(new[] { 6, 7, 8 })
            && migrated.State.deck.Skip(6).All(id => string.IsNullOrEmpty(migrated.CardUpgradeLabel(id))),
            "reloading migrated saves is idempotent and future newly obtained same-kind cards stay unupgraded");
    }

    private static void StrategicUpgradedFreeRecall()
    {
        var source = GameDatabase.Card("basic_guard");
        var oldTargets = source.freeCastTargets; int oldCount = source.freeCastCount;
        bool oldHit = source.freeCastOnHit, oldDelay = source.freeCastNextTurn, oldLast = source.freeCastLastSkill;
        try
        {
            source.freeCastTargets = new[] { "basic_attack" }; source.freeCastCount = 1;
            source.freeCastOnHit = source.freeCastNextTurn = source.freeCastLastSkill = false;
            var e = StrategyReady(1454); e.State.deck = new List<string> { "basic_guard", "basic_attack", "basic_attack" };
            string upgraded = StrategyUpgradeCopy(e, 1, "power");
            Enter(e, ZoneKind.Wildlife); var c = e.State.combat; c.enemyHp = c.enemyMaxHp = 10000; c.enemyEvasion = 0;
            c.hand = new List<string> { "basic_guard" }; c.drawPile.Clear(); c.discardPile = new List<string> { "basic_attack", upgraded }; c.exhaustPile.Clear(); c.energy = e.EffectiveCardCost("basic_guard");
            var expected = c.hand.Concat(c.discardPile).OrderBy(id => id).ToArray();
            Check(e.PlayCard(0) && c.hand.SequenceEqual(new[] { upgraded }) && c.discardPile.Contains("basic_attack")
                && c.energy == 0 && e.EffectiveCardCost(upgraded) == 0 && e.CanPlayCard(0),
                "free recall finds an upgraded owned target by base skill and keeps its exact identity at zero energy");
            Check(c.hand.Concat(c.drawPile).Concat(c.discardPile).Concat(c.exhaustPile).OrderBy(id => id).SequenceEqual(expected),
                "free recall moves a real upgraded copy without dropping or duplicating its ordinary twin");
            e = new GameEngine((RunState)Clone(e.State)); c = e.State.combat;
            Check(e.EffectiveCardCost(upgraded) == 0 && e.PlayCard(0) && c.discardPile.Contains(upgraded)
                && e.EffectiveCardCost(upgraded) == e.CardCost(upgraded), "upgraded free target survives save, consumes its single grant, and returns to discard");
            Check(c.hand.Concat(c.drawPile).Concat(c.discardPile).Concat(c.exhaustPile).OrderBy(id => id).SequenceEqual(expected),
                "playing the upgraded free target preserves both copies and total pile composition");
        }
        finally
        {
            source.freeCastTargets = oldTargets; source.freeCastCount = oldCount;
            source.freeCastOnHit = oldHit; source.freeCastNextTurn = oldDelay; source.freeCastLastSkill = oldLast;
        }
    }

    private static void StrategyFinishEnemyTurn(GameEngine e)
    {
        int safety = 100;
        while (e.State.stage == RunStage.Combat && e.State.combat.enemyTurn && safety-- > 0) e.AdvanceEnemyAction();
        Check(e.State.stage == RunStage.Combat && !e.State.combat.enemyTurn && safety > 0, "enemy turn reaches the next playable turn");
    }

    private static void StrategicRetain()
    {
        var e = StrategyReady(1410);
        Check(!e.ToggleRetainCard(0), "hand retention is unavailable outside combat");
        Enter(e, ZoneKind.Wildlife); var c = e.State.combat;
        c.enemyHp = c.enemyMaxHp = 10000; c.enemyEvasion = 0; c.energy = 100;
        c.hand = new List<string> { "basic_guard", "nia_q", "basic_guard" };
        c.drawPile = Enumerable.Repeat("basic_attack", 20).ToList(); c.discardPile.Clear();
        Check(e.RetainAvailable == 2 && !e.ToggleRetainCard(-1) && !e.ToggleRetainCard(3), "retention has two uses and rejects invalid hand indices");
        Check(e.ToggleRetainCard(2) && c.retainIndex == 2 && e.RetainAvailable == 2, "reserving one duplicate card does not spend a use");
        Check(e.ToggleRetainCard(2) && c.retainIndex == -1 && e.RetainAvailable == 2, "reselecting the exact copy cancels retention for free");
        e.ToggleRetainCard(2);
        Check(e.PlayCard(0) && c.retainIndex == 1, "playing an earlier hand card keeps the reservation on the same later copy");
        var restored = new GameEngine((RunState)Clone(e.State)); c = restored.State.combat;
        Check(c.retainIndex == 1 && restored.RetainAvailable == 2, "pending hand reservation survives a mid-turn save");
        int drawBefore = c.drawPile.Count;
        Check(restored.BeginEndTurn() && c.retainedCardId == "basic_guard" && restored.RetainAvailable == 1 && c.hand.Count == 0, "ending the turn commits exactly one retained copy and spends one use");
        Check(!restored.ToggleRetainCard(0), "enemy turn cannot change the committed reservation");
        restored = new GameEngine((RunState)Clone(restored.State)); c = restored.State.combat;
        StrategyFinishEnemyTurn(restored);
        Check(c.hand.Count == 5 && c.hand.Count(id => id == "basic_guard") == 1 && c.drawPile.Count == drawBefore - 4,
            "retained card replaces one new draw rather than providing an extra card");
        Check(restored.ToggleRetainCard(0) && restored.BeginEndTurn() && restored.RetainAvailable == 0, "second committed retention exhausts this battle's allowance");
        StrategyFinishEnemyTurn(restored);
        Check(!restored.ToggleRetainCard(0), "retention cannot be committed a third time in one battle");

        e = StrategyReady(1411); Enter(e, ZoneKind.Wildlife); c = e.State.combat; c.energy = 100;
        c.enemyHp = c.enemyMaxHp = 10000; c.hand = new List<string> { "basic_guard", "nia_w" };
        e.ToggleRetainCard(0);
        Check(e.PlayCard(0) && c.retainIndex == -1 && e.RetainAvailable == 2, "playing the selected copy cancels its retention without spending a use");
        e.BeginEndTurn(); Check(e.RetainAvailable == 2, "ending without a retained copy does not spend a use");

        e = StrategyReady(1412); Enter(e, ZoneKind.Wildlife); c = e.State.combat;
        c.enemyHp = c.enemyMaxHp = 10000; c.hand = new List<string> { "basic_guard" };
        c.drawPile = Enumerable.Repeat("basic_attack", 40).ToList(); c.discardPile.Clear();
        var passive = GameDatabase.Passives[0]; var originalProfile = passive.mechanics;
        try
        {
            passive.mechanics = new TraitMechanicProfile { rules = Enumerable.Range(0, 12).Select(i => new TraitRule { trigger = "turn_start", op = "draw", amount = 1 }).ToArray() };
            e.State.passives.Add(passive.id);
            e.ToggleRetainCard(0); e.BeginEndTurn(); StrategyFinishEnemyTurn(e);
            Check(c.hand.Count == 12 && c.hand.Contains("basic_guard"), "retained card is restored before turn-start draws and respects the twelve-card hand limit");
        }
        finally { passive.mechanics = originalProfile; }
    }

    private static void StrategicMapServices()
    {
        var e = StrategyReady(1420); e.State.row = 2; e.State.lane = 0; e.State.credits = 39;
        int rng = e.State.rngState;
        Check(!e.CanUseSecurityConsole && !e.UseSecurityConsole() && e.State.credits == 39 && e.State.rngState == rng,
            "unaffordable scouting does not spend money or consume random outcomes");
        e.State.credits = 200;
        var future = e.State.map.Where(n => n.act == 1 && n.row > 2 && n.row <= 4).ToArray();
        Check(future.Where(n => n.kind == ZoneKind.Subject || n.kind == ZoneKind.Wildlife).All(n => string.IsNullOrEmpty(e.NodeIntel(n))), "unscouted combat identities remain hidden");
        Check(e.CanUseSecurityConsole && e.UseSecurityConsole() && e.State.credits == 160 && e.State.scoutedActs.Contains(1), "console costs forty credits once per act");
        var revealed = future.Where(n => n.kind == ZoneKind.Subject || n.kind == ZoneKind.Wildlife).ToArray();
        Check(revealed.Length > 0 && revealed.All(n => !string.IsNullOrEmpty(e.NodeIntel(n))), "console reveals actual combat identities across the next two rows");
        var intel = revealed.Select(e.NodeIntel).ToArray(); rng = e.State.rngState;
        for (int i = 0; i < 10; ++i) foreach (var node in future) e.NodeIntel(node);
        Check(e.State.rngState == rng && !e.UseSecurityConsole() && e.State.credits == 160, "viewing intel and repeated console attempts do not reroll or repay");
        var loaded = new GameEngine((RunState)Clone(e.State));
        Check(loaded.State.rngState == rng && revealed.Select(n => loaded.NodeIntel(loaded.State.map.First(m => m.id == n.id))).SequenceEqual(intel), "saved reconnaissance remains identical after load");
        var combat = loaded.AvailableNodes().First(n => n.kind == ZoneKind.Subject || n.kind == ZoneKind.Wildlife);
        string forecast = loaded.NodeIntel(combat);
        Check(loaded.EnterNode(combat.lane) && forecast.Contains(loaded.State.combat.enemyName), "entering a scouted node fights the revealed opponent");
        Check(!loaded.UseSecurityConsole(), "console cannot be used during battle");

        e = StrategyReady(1421); e.State.row = 3; e.State.lane = 0;
        var targets = e.AvailableHyperloopNodes();
        Check(e.CanUseHyperloop && targets.Count == 1 && targets[0].row == 4 && targets[0].lane == 2, "hyperloop reveals only an otherwise unreachable lane in the immediate next row");
        Check(!e.EnterNode(2) && !e.EnterHyperloopNode(1) && !e.EnterHyperloopNode(-1) && !e.State.hyperloopActs.Contains(1), "invalid or normally connected destinations do not consume a hyperloop charge");
        Check(e.EnterHyperloopNode(2) && e.State.activeNodeId == targets[0].id && e.State.hyperloopActs.Contains(1), "hyperloop enters exactly the chosen next-row node and consumes one use");
        Check(!e.EnterHyperloopNode(2), "hyperloop cannot be used again while inside a node");
        loaded = new GameEngine((RunState)Clone(e.State)); loaded.State.stage = RunStage.Map; loaded.State.row = 5; loaded.State.lane = 0;
        Check(!loaded.CanUseHyperloop, "hyperloop usage persists across saves within its act");
        loaded.State.act = 2;
        Check(loaded.CanUseHyperloop, "a new act provides its own hyperloop use");
    }

    private static void StrategicEncounterChains()
    {
        Check(GameDatabase.Events.Count == 91 && EncounterChains.Events.Count >= 8, "linked encounters preserve all ninety-one regular subject encounters");
        foreach (var followup in EncounterChains.Events)
        {
            Check(followup.chainOnly && followup.options.Length == 3 && GameDatabase.Event(followup.id) == followup, "followups are separately addressable authored three-choice encounters");
            Check(followup.options.Any(o => o.effect == "card" && GameDatabase.Card(o.cardId) != null), "each followup offers at least one named valid skill reward");
            for (int choice = 0; choice < followup.options.Length; ++choice)
            {
                var option = followup.options[choice];
                Check(option.effect != "food" || GameDatabase.Food(option.objectId) != null, "every authored followup food reward exists");
                var authored = StrategyReady(1440 + choice);
                authored.State.pendingEncounterFollowups.Add(new PendingEncounterFollowup { eventId = followup.id, owner = followup.owner });
                StrategySelectEvent(authored, followup, o => ReferenceEquals(o, option));
                Check(authored.CanChooseEventOption(choice) && authored.ChooseEventOption(choice), "every authored followup action is executable with its stated prerequisites");
                Check(authored.State.completedChainEvents.Contains(followup.id) && !authored.State.pendingEncounterFollowups.Any(p => p.eventId == followup.id), "each authored reward clears exactly its completed promise");
                if (option.effect == "food") Check(authored.State.foods.Contains(option.objectId), "authored food reward enters the food bag");
                if (authored.State.cardUpgradePending)
                {
                    authored = new GameEngine((RunState)Clone(authored.State));
                    Check(!authored.ChooseEventOption(choice) && authored.SkipCardUpgrade(), "a pending followup upgrade can be forfeited after reload without replaying the reward");
                }
                Check(authored.State.stage == RunStage.Map && !authored.ChooseEventOption(choice), "authored followup completion returns to the map and pays only once");
            }
        }
        var def = GameDatabase.Events.First(ev => ev.options.Any(o => !string.IsNullOrEmpty(o.followupId) && o.creditCost > 0));
        var e = StrategyReady(1430);
        int index = StrategySelectEvent(e, def, o => !string.IsNullOrEmpty(o.followupId) && o.creditCost > 0, false);
        var start = def.options[index]; e.State.credits = start.creditCost - 1;
        Check(!e.CanChooseEventOption(index) && !e.ChooseEventOption(index) && string.IsNullOrEmpty(e.PendingFollowupDescription), "unpaid chain promises cannot enqueue a followup");
        e.State.credits = 100; int paid = e.State.credits;
        if (!string.IsNullOrEmpty(start.requiredFoodId)) e.State.foods.Add(start.requiredFoodId);
        if (!string.IsNullOrEmpty(start.requiredObjectId)) e.State.objects.Add(start.requiredObjectId);
        Check(e.ChooseEventOption(index) && e.State.credits == paid - start.creditCost && e.PendingFollowupDescription.Contains(def.owner), "accepting a promise pays exactly once and tracks its subject");
        Check(!e.ChooseEventOption(index), "starting a chain cannot replay the initial reward");
        var promise = (RunState)Clone(e.State); int hits = 0;
        const int samples = 500;
        for (int i = 0; i < samples; ++i)
        {
            var state = (RunState)Clone(promise); state.rngState = 9001 + i * 971;
            var sample = new GameEngine(state); var node = sample.AvailableNodes().First(); node.kind = ZoneKind.Encounter;
            Check(sample.EnterNode(node.lane), "enter the next actual encounter after a promise");
            if (sample.State.encounterOffers.Contains(start.followupId)) hits++;
            var offers = sample.CurrentEncounterOffers.ToArray();
            Check(offers.Length == 3 && offers.Select(ev => ev.owner).Distinct().Count() == 3, "followup selection still offers three distinct subjects");
            var reload = new GameEngine((RunState)Clone(sample.State));
            Check(reload.State.encounterOffers.SequenceEqual(sample.State.encounterOffers) && reload.State.rngState == sample.State.rngState, "reloading pending offers never rerolls the followup chance");
        }
        Check(hits > samples * .70 && hits < samples * .90, "next-encounter inclusion is strongly boosted around eighty percent");
        Console.WriteLine("Linked encounter sample: " + hits + "/" + samples + " next encounters featured the promised subject.");
        var second = GameDatabase.Events.First(ev => ev.owner != def.owner && ev.options.Any(o => !string.IsNullOrEmpty(o.followupId)));
        var multiple = new GameEngine((RunState)Clone(promise));
        index = StrategySelectEvent(multiple, second, o => !string.IsNullOrEmpty(o.followupId));
        string secondId = second.options[index].followupId;
        Check(multiple.ChooseEventOption(index) && multiple.PendingFollowupDescription.Contains(second.owner), "a newly accepted promise takes next-encounter priority over an older unresolved promise");
        var multiplePromise = (RunState)Clone(multiple.State); int secondHits = 0, oldHits = 0;
        for (int i = 0; i < samples; ++i)
        {
            var state = (RunState)Clone(multiplePromise); state.rngState = 19001 + i * 971;
            var sample = new GameEngine(state); var node = sample.AvailableNodes().First(); node.kind = ZoneKind.Encounter;
            sample.EnterNode(node.lane);
            if (sample.State.encounterOffers.Contains(secondId)) secondHits++;
            if (sample.State.encounterOffers.Contains(start.followupId)) oldHits++;
            Check(sample.CurrentEncounterOffers.Select(ev => ev.owner).Distinct().Count() == 3, "multiple pending chains retain distinct encounter subjects");
            Check(!sample.State.pendingEncounterFollowups.Any(p => p.eventId == secondId && p.priorityNext), "only an actual encounter roll consumes the new promise priority");
            Check(sample.PendingFollowupDescription.Contains(def.owner), "unresolved older promises rotate back to the boosted position");
        }
        Check(secondHits > samples * .70 && secondHits < samples * .90 && oldHits > 0, "new promise keeps eighty-percent priority while older followups remain available");
        Console.WriteLine("Multiple promise sample: " + secondHits + "/" + samples + " newest; " + oldHits + " older followups still offered.");
        e = new GameEngine((RunState)Clone(promise));
        Enter(e, ZoneKind.Kiosk); e.LeaveKiosk();
        Check(e.PendingFollowupDescription.Contains(def.owner), "non-encounter services do not consume a pending promise");
        var follow = GameDatabase.Event(start.followupId);
        index = StrategySelectEvent(e, follow, o => o.effect == "card");
        int copies = e.State.deck.Count(id => id == follow.options[index].cardId);
        Check(e.ChooseEventOption(index) && e.State.deck.Count(id => id == follow.options[index].cardId) == copies + 1, "followup grants exactly its displayed card");
        Check(string.IsNullOrEmpty(e.PendingFollowupDescription) && !e.ChooseEventOption(index), "completed followup is cleared and cannot pay twice");
        e = new GameEngine((RunState)Clone(e.State));
        index = StrategySelectEvent(e, def, o => o.followupId == start.followupId);
        Check(!e.CanChooseEventOption(index), "completed promise cannot be restarted for repeat rewards after loading");
    }
}
#endif
