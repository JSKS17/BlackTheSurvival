#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
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
        Check(e.State.rngState == rng && !e.ChooseEventOption(option), "pending upgrade cannot redraw or repeat the event reward");
        var choices = e.CardUpgradeOptions("nia_q");
        Check(choices.Count >= 2 && e.UpgradeCandidates().Count(id => id == "nia_q") == 1, "Nia Q offers exclusive branches with one target for duplicate copies");
        var special = choices.First(o => o.id != "power");
        var baseCard = GameDatabase.Card("nia_q");
        int baseGain = baseCard.mechanics.rules.First(r => r.op == "gain").amount;
        var loaded = new GameEngine((RunState)Clone(e.State));
        Check(loaded.State.cardUpgradePending && loaded.UpgradeCandidates().SequenceEqual(e.UpgradeCandidates()), "pending target selection survives save and load");
        Check(!loaded.UpgradeCard("not_a_card", special.id) && !loaded.UpgradeCard("nia_q", "not_a_branch") && loaded.State.cardUpgradePending,
            "invalid target or branch keeps the upgrade pending without spending it");
        Check(loaded.UpgradeCard("nia_q", special.id) && !loaded.State.cardUpgradePending && loaded.State.stage == RunStage.Map,
            "choosing a branch completes the encounter exactly once");
        Check(!loaded.UpgradeCard("nia_q", "power") && !loaded.UpgradeCandidates().Contains("nia_q"), "an upgraded card cannot receive its other branch");
        var modified = loaded.PlayerCard("nia_q");
        Check(GameDatabase.Card("nia_q").mechanics.rules.First(r => r.op == "gain").amount == baseGain && modified.mechanics.rules.First(r => r.op == "gain").amount == baseGain + 1,
            "resource branch changes only this run's player card and preserves the shared database");
        Check(DescriptionSummary.CardPreview(modified).Contains("블록을 2 늘립니다") && DescriptionSummary.CardPreview(baseCard).Contains("블록을 1 늘립니다"),
            "upgraded Nia Q summary reflects the actual block gain without altering base previews");
        Check(DescriptionSummary.CardPreview(StrategyMechanics.Apply(GameDatabase.Card("nia_w"), "arcade")).Contains("블록당 피해 6"),
            "upgraded Nia W summary reflects its actual per-block damage");
        Check(loaded.State.deck.Count(id => id == "nia_q") == 2 && !string.IsNullOrEmpty(loaded.CardUpgradeLabel("nia_q")), "both owned copies share their chosen upgrade and display its label");
        loaded = new GameEngine((RunState)Clone(loaded.State));
        Enter(loaded, ZoneKind.Wildlife); var c = loaded.State.combat;
        c.enemyHp = c.enemyMaxHp = 10000; c.enemyEvasion = 0; c.hand = new List<string> { "nia_q", "nia_q" }; c.energy = 100;
        Check(loaded.PlayCard(0) && SkillMechanics.Resource(c.playerSkills, "니아", "blocks") == 2, "Nia branch changes actual block generation");
        Check(loaded.PlayCard(0) && SkillMechanics.Resource(c.playerSkills, "니아", "blocks") == 3, "resource upgrade keeps the original maximum of three");

        e = StrategyReady(1402); e.State.deck = new List<string> { "nia_e", "basic_attack" };
        option = StrategySelectEvent(e, def, o => o.effect == "upgrade_card"); e.ChooseEventOption(option);
        var efficiency = e.CardUpgradeOptions("nia_e").First(o => o.id == "efficiency");
        int sourceCost = GameDatabase.Card("nia_e").cost;
        Check(e.UpgradeCard("nia_e", efficiency.id) && e.PlayerCard("nia_e").cost == Math.Max(1, sourceCost - 1), "efficiency branch lowers cost with a one-cost floor");
        Check(GameDatabase.Card("nia_e").cost == sourceCost && !e.CardUpgradeOptions("basic_attack").Any(o => o.id == "efficiency"), "efficiency does not mutate enemy data or turn basic attacks into free cards");
        var legacy = StrategyReady(1403); legacy.State.upgrades.Add("nia_q");
        int oldDamage = legacy.CardDamage("nia_q"); legacy = new GameEngine((RunState)Clone(legacy.State));
        Check(legacy.CardDamage("nia_q") == oldDamage && !legacy.UpgradeCandidates().Contains("nia_q"), "legacy upgrades retain their strength and cannot be stacked with new branches");
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
