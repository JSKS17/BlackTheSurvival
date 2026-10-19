#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    public static void TacticalModuleMechanics()
    {
        TacticalModuleIdentityAndPersistence();
        TacticalModuleLocksAndLayering();
        TacticalModuleOrdinaryUpgradePreviews();
        TacticalEncounterTransactions();
    }

    static GameEngine TacticalReady(int seed = 78100)
    {
        var engine = Ready(seed); engine.State.passives.Clear();
        engine.State.mainRune = engine.State.supportRune = null;
        engine.State.stage = RunStage.Map; engine.State.tacticalModules = 2;
        return engine;
    }

    static void TacticalModuleIdentityAndPersistence()
    {
        foreach (var source in GameDatabase.Cards.Where(TacticalSkills.IsTactical))
        {
            var engine = TacticalReady();
            engine.State.deck = new List<string> { source.id, source.id, "basic_attack" };
            string before = FieldSnapshot(engine.State), original = FieldSnapshot(source);
            var preview = engine.TacticalModulePreview(1);
            Check(preview != null && TacticalSkills.IsModuleUpgraded(preview) && FieldSnapshot(engine.State) == before,
                "opening, inspecting or cancelling a tactical picker consumes nothing: " + source.id);
            Check(!engine.UpgradeTacticalCard(-1) && !engine.UpgradeTacticalCard(2) && engine.State.tacticalModules == 2,
                "invalid tactical targets consume no module: " + source.id);
            Check(engine.UpgradeTacticalCard(1) && engine.State.tacticalModules == 1,
                "module confirms one owned F copy: " + source.id);
            string reference = engine.State.deck[1];
            Check(CardInstances.IsInstance(reference) && engine.State.deck[0] == source.id
                && engine.IsTacticalUpgraded(reference) && !engine.IsTacticalUpgraded(source.id)
                && !engine.TacticalModuleCardIndices().Contains(1), "tactical enhancement belongs only to selected copy: " + source.id);
            Check(FieldSnapshot(engine.PlayerCard(reference)) == FieldSnapshot(preview) && FieldSnapshot(source) == original
                && !TacticalSkills.IsModuleUpgraded(engine.PlayerCard(source.id)), "module actual effect matches preview and leaves original/enemy definitions intact: " + source.id);
            Check(!engine.UpgradeTacticalCard(1) && engine.State.tacticalModules == 1, "same physical card cannot consume another module: " + source.id);
            engine.State.deck.Add(source.id);
            var loaded = new GameEngine((RunState)Clone(engine.State));
            Check(loaded.State.tacticalModules == 1 && loaded.IsTacticalUpgraded(loaded.State.deck[1])
                && !loaded.IsTacticalUpgraded(loaded.State.deck.Last())
                && FieldSnapshot(loaded.PlayerCard(reference)) == FieldSnapshot(preview), "save/load preserves one tactical enhancement without upgrading future copies: " + source.id);
        }
        var missing = TacticalReady(); missing.State.deck = new List<string> { "basic_attack", "basic_guard" };
        Check(!missing.CanUseTacticalModule() && !missing.UpgradeTacticalCard(0) && missing.State.tacticalModules == 2,
            "a bag module with no eligible F card is retained");
        missing.State.tacticalModules = -3; missing.State.tacticalCardUpgrades = null;
        var repaired = new GameEngine((RunState)Clone(missing.State));
        Check(repaired.State.tacticalModules == 0 && repaired.State.tacticalCardUpgrades.Count == 0,
            "legacy or malformed module inventory initializes safely");
    }

    static void TacticalModuleLocksAndLayering()
    {
        foreach (RunStage stage in Enum.GetValues(typeof(RunStage)))
        {
            var engine = TacticalReady(); engine.State.deck = new List<string> { "tactical_plasma" }; engine.State.stage = stage;
            bool safe = new[] { RunStage.Map, RunStage.Kiosk, RunStage.Campfire, RunStage.Encounter }.Contains(stage);
            Check(engine.CanUseTacticalModule() == safe, "module use follows safe stage whitelist: " + stage);
            if (!safe) Check(!engine.UpgradeTacticalCard(0) && engine.State.tacticalModules == 2 && engine.State.deck[0] == "tactical_plasma", "locked stage does not spend module or change card: " + stage);
        }
        var locked = TacticalReady(); locked.State.deck = new List<string> { "tactical_plasma" };
        foreach (string flag in new[] { "runeChangePending", "cardUpgradePending", "rareRemovalPending", "rareCopyPending" })
        {
            var field = typeof(RunState).GetField(flag); field.SetValue(locked.State, true);
            Check(!locked.CanUseTacticalModule() && !locked.UpgradeTacticalCard(0) && locked.State.tacticalModules == 2,
                "pending selection blocks module use: " + flag);
            field.SetValue(locked.State, false);
        }
        locked.State.tacticalModules = 0;
        Check(!locked.CanUseTacticalModule() && !locked.UpgradeTacticalCard(0), "zero module inventory cannot be bypassed");

        var first = TacticalReady(); first.State.deck = new List<string> { "tactical_plasma", "tactical_plasma" };
        Check(first.UpgradeTacticalCard(0), "module-first layering begins"); string firstReference = first.State.deck[0];
        StrategyUpgradeCopy(first, 0, "power");
        Check(first.State.deck[0] != firstReference && first.IsTacticalUpgraded(first.State.deck[0])
            && first.CardUpgradeLevel(first.State.deck[0]) == 1 && !first.IsTacticalUpgraded(first.State.deck[1]),
            "ordinary upgrade transfers module flag to the replacement reference only");

        var second = TacticalReady(); second.State.deck = new List<string> { "tactical_plasma", "tactical_plasma" };
        StrategyUpgradeCopy(second, 0, "power");
        Check(second.UpgradeTacticalCard(0) && second.CardUpgradeLevel(second.State.deck[0]) == 1,
            "module applied after ordinary upgrade preserves that upgrade branch");
        Check(FieldSnapshot(first.PlayerCard(first.State.deck[0])) == FieldSnapshot(second.PlayerCard(second.State.deck[0])),
            "module and ordinary enhancement resolve the same effect in either acquisition order");

        var rare = TacticalReady(); string rareReference = rare.AddRareRewardCard("tactical_plasma"); int at = rare.State.deck.IndexOf(rareReference);
        Check(rare.UpgradeTacticalCard(at) && rare.CardUpgradeLevel(rare.State.deck[at]) == 2 && rare.IsTacticalUpgraded(rare.State.deck[at]),
            "module retains an existing independent rare ++ upgrade");
        var loaded = new GameEngine((RunState)Clone(rare.State));
        Check(loaded.CardUpgradeLevel(loaded.State.deck[at]) == 2 && loaded.IsTacticalUpgraded(loaded.State.deck[at]),
            "combined rare and tactical enhancements survive load");
        Check(!loaded.RareCopyIndices().Contains(at), "enhanced F copy is not misclassified as unenhanced for rare copying");
    }

    static GameEngine TacticalEventReady(EventDef encounter)
    {
        var engine = Ready(78400); engine.State.credits = 1000;
        string subject = ResearchEncounters.SubjectForEvent(encounter.id);
        if (subject != null) engine.State.research.unlockedReports.Add(subject);
        Enter(engine, ZoneKind.Encounter); engine.State.encounterOffers = new List<string> { encounter.id };
        Check(engine.SelectEncounter(encounter.id), "tactical encounter selected: " + encounter.id);
        return engine;
    }

    static void TacticalModuleOrdinaryUpgradePreviews()
    {
        var upgradeEvent = GameDatabase.Events.First(encounter => encounter.options.Any(option => option.effect == "upgrade_card" && string.IsNullOrEmpty(option.followupId)));
        var cache = new CardDisplayCatalog(GameDatabase.Cards, GameDatabase.Characters);
        foreach (var source in GameDatabase.Cards.Where(TacticalSkills.IsTactical))
        {
            var original = TacticalReady(78300); original.State.deck = new List<string> { source.id, source.id };
            Check(original.UpgradeTacticalCard(0), "module upgrade preview fixture: " + source.id);
            var options = original.CardUpgradeOptions(0);
            var cached = cache.UpgradeOptions(source, true);
            Check(options.Select(option => option.id).SequenceEqual(StrategyMechanics.Options(source).Select(option => option.id)),
                "tactical module does not introduce or remove ordinary upgrade branches: " + source.id);
            Check(options.Select(option => option.id + "|" + option.description).SequenceEqual(cached.Select(option => option.id + "|" + option.description)),
                "engine and cached UI show identical module-aware ordinary upgrade options: " + source.id);
            foreach (var option in options)
            {
                var engine = new GameEngine((RunState)Clone(original.State));
                int choice = StrategySelectEvent(engine, upgradeEvent, reward => reward.effect == "upgrade_card");
                Check(engine.ChooseEventOption(choice) && engine.UpgradeCard(0, option.id), "module card ordinary branch can be confirmed: " + source.id + "/" + option.id);
                var actual = engine.PlayerCard(engine.State.deck[0]); string rules = CardPresentation.Rules(actual);
                string actualDetails = "강화 후 전체 효과\n" + actual.description + (string.IsNullOrEmpty(rules) ? "" : "\n\n" + rules);
                Check(option.description.EndsWith(actualDetails, StringComparison.Ordinal),
                    "ordinary branch description matches the actual final module-enhanced effect: " + source.id + "/" + option.id);
                Check(engine.IsTacticalUpgraded(engine.State.deck[0]) && engine.CardUpgradeLevel(engine.State.deck[0]) == 1
                    && !engine.IsTacticalUpgraded(engine.State.deck[1]), "preview confirmation preserves one tactical enhancement and leaves its twin ordinary: " + source.id);
            }
        }
    }

    static void TacticalEncounterTransactions()
    {
        Check(TacticalEncounterOptions.ModuleSubjects.Length == 6 && TacticalEncounterOptions.Sales.Length == 8,
            "module and specific F rewards are assigned to selected specialists");
        foreach (string subject in TacticalEncounterOptions.ModuleSubjects)
            foreach (bool rare in new[] { false, true })
            {
                var encounter = rare ? ResearchEncounters.EventForSubject(subject) : GameDatabase.Event("event_" + subject);
                var engine = TacticalEventReady(encounter); int index = Array.FindIndex(encounter.options, option => option.effect == "tactical_module");
                int credits = engine.State.credits, count = engine.State.deck.Count;
                Check(index >= 0 && encounter.options[index].amount == (rare ? 2 : 1) && engine.ChooseEventOption(index),
                    "character-themed encounter awards correct module count: " + encounter.id);
                Check(engine.State.tacticalModules == (rare ? 2 : 1) && engine.State.credits == credits
                    && engine.State.deck.Count == count && !engine.ChooseEventOption(index), "module reward resolves once without silently upgrading or charging: " + encounter.id);
            }
        foreach (var sale in TacticalEncounterOptions.Sales)
            foreach (bool rare in new[] { false, true })
            {
                var encounter = rare ? ResearchEncounters.EventForSubject(sale.subjectId) : GameDatabase.Event("event_" + sale.subjectId);
                int index = rare ? 2 : sale.optionIndex; var option = encounter.options[index];
                var engine = TacticalEventReady(encounter); int price = rare ? sale.rarePrice : sale.price;
                int count = engine.State.deck.Count, modules = engine.State.tacticalModules;
                engine.State.credits = price - 1;
                Check(!engine.CanChooseEventOption(index) && !engine.ChooseEventOption(index) && engine.State.credits == price - 1
                    && engine.State.deck.Count == count && engine.State.stage == RunStage.Encounter,
                    "unaffordable tactical sale is atomic and remains open: " + encounter.id);
                engine.State.credits = price;
                Check(engine.CanChooseEventOption(index) && engine.ChooseEventOption(index) && engine.State.credits == 0
                    && engine.State.deck.Count == count + 1 && CardInstances.BaseId(engine.State.deck.Last()) == sale.cardId
                    && engine.IsTacticalUpgraded(engine.State.deck.Last()) == rare && engine.State.tacticalModules == modules,
                    "tactical sale grants exactly named physical card at displayed price: " + encounter.id);
                Check(!engine.ChooseEventOption(index), "specific F purchase cannot be claimed twice: " + encounter.id);
                var card = GameDatabase.Card(sale.cardId);
                Check(option.label.Contains(card.name) && option.label.Contains(price.ToString())
                    && EventPresentation.RewardSummary(option).Contains(card.name)
                    && EventPresentation.RewardSummary(option).Contains("효과:"), "specific F sale states name, cost and actual effect: " + encounter.id);
                if (rare) Check(encounter.options[0].effect == "rare_trade_card" && new[] { "passive", "card" }.Contains(encounter.options[1].effect),
                    "rare specialist keeps its skin story, ++ skill sale and passive/R choice: " + encounter.id);
            }
        foreach (var promise in GameDatabase.Events.SelectMany(encounter => encounter.options).Where(option => !string.IsNullOrEmpty(option.followupId)))
            Check(GameDatabase.Event(promise.followupId) != null && promise.effect != "tactical_module" && promise.effect != "trade_tactical_enhanced",
                "tactical rewards preserve every existing follow-up promise");
        var definition = GameDatabase.Event("event_alex"); var transaction = definition.options[2];
        int priorFee = transaction.creditCost; string priorFood = transaction.requiredFoodId, priorObject = transaction.requiredObjectId, priorCard = transaction.cardId;
        try
        {
            transaction.creditCost = 20; transaction.requiredFoodId = "potato"; transaction.requiredObjectId = "meteorite";
            var engine = TacticalEventReady(definition); int count = engine.State.deck.Count;
            engine.State.credits = transaction.amount; engine.State.foods.Add("potato"); engine.State.objects.Add("meteorite");
            Check(!engine.CanChooseEventOption(2) && !engine.ChooseEventOption(2) && engine.State.credits == transaction.amount
                && engine.State.foods.Contains("potato") && engine.State.objects.Contains("meteorite") && engine.State.deck.Count == count,
                "tactical purchase validates combined fees before consuming money or required items");
            engine.State.credits += transaction.creditCost; transaction.cardId = "missing_tactical";
            Check(!engine.CanChooseEventOption(2) && !engine.ChooseEventOption(2) && engine.State.deck.Count == count
                && engine.State.foods.Contains("potato") && engine.State.objects.Contains("meteorite"), "invalid tactical sale target consumes no inventory");
            transaction.cardId = priorCard;
            Check(engine.ChooseEventOption(2) && engine.State.credits == 0 && !engine.State.foods.Contains("potato")
                && !engine.State.objects.Contains("meteorite") && engine.State.deck.Count == count + 1,
                "validated tactical sale consumes each required resource and grants one card atomically");
        }
        finally { transaction.creditCost = priorFee; transaction.requiredFoodId = priorFood; transaction.requiredObjectId = priorObject; transaction.cardId = priorCard; }
    }
}
#endif
