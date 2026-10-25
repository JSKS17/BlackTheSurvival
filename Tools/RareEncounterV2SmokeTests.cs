#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumia;

public static partial class CoreSmokeTests
{
    static GameEngine RareV2Ready(EventDef story, int seed = 51901)
    {
        var engine = Ready(seed); engine.State.passives.Clear(); engine.State.credits = 10000;
        engine.State.research.unlockedReports.Add(ResearchEncounters.SubjectForEvent(story.id));
        Enter(engine, ZoneKind.Encounter); engine.State.encounterOffers = new List<string> { story.id };
        Check(engine.SelectEncounter(story.id), "rare V2 story selected: " + story.id); return engine;
    }
    static GameEngine RareV2Choice(string effect, out EventDef story)
    {
        story = ResearchEncounters.Events.First(ev => ev.options.Any(option => option.effect == effect));
        var engine = RareV2Ready(story);
        Check(engine.ChooseEventOption(Array.FindIndex(story.options, option => option.effect == effect)), "rare V2 choice begins: " + effect); return engine;
    }
    static void RareV2Roll(GameEngine engine) => typeof(GameEngine).GetMethod("RollEncounterOffers", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(engine, new object[0]);

    public static void RareEncounterV2()
    {
        int rosterCount = GameDatabase.Characters.Count;
        Check(ResearchEncounters.Events.Count == 92 && GameDatabase.Characters.Count == 91, "all ninety-one subjects retain a rare encounter: " + ResearchEncounters.Events.Count + "/" + GameDatabase.Characters.Count);
        var groups = ResearchEncounters.Events.Where(ev => ResearchEncounters.SubjectForEvent(ev.id) != "lumi").GroupBy(ev => ev.options[2].effect).ToDictionary(group => group.Key, group => group.Count());
        Check(groups.Count == 6 && groups["tactical_module"] == 6 && groups["trade_tactical_enhanced"] == 8
            && new[] { "rare_remove_cards", "rare_support_runes", "rare_copy_cards", "rare_tactical" }.All(effect => groups[effect] >= 15),
            "specialist tactical choices expand selected rare stories while retaining all four earlier reward families");
        Check(GameDatabase.Events.SelectMany(ev => ev.options).All(option => !option.effect.StartsWith("rare_", StringComparison.Ordinal)), "ordinary encounters do not inherit rare rewards");
        var representatives = ResearchEncounters.Events.Select(ev => ev.options[0].cardId).ToArray();
        RareEncounterRules.Configure(ResearchEncounters.Events); EventPresentation.Configure(ResearchEncounters.Events);
        Check(ResearchEncounters.Events.Select(ev => ev.options[0].cardId).SequenceEqual(representatives), "rare configuration is idempotent and retains original representative skills");
        foreach (var story in ResearchEncounters.Events.Where(ev => ResearchEncounters.SubjectForEvent(ev.id) != "lumi"))
        {
            var subject = GameDatabase.Character(ResearchEncounters.SubjectForEvent(story.id));
            var purchase = story.options[0]; var baseCard = GameDatabase.Card(purchase.cardId);
            Check(story.options.Length == 3 && purchase.effect == "rare_trade_card" && baseCard.owner == subject.name && new[] {"Q","W","E"}.Contains(baseCard.key), "each rare story sells its own representative QWE: " + subject.id);
            Check(purchase.amount == RareCardUpgrades.Price(baseCard) && story.options.All(option => option.bonusCredits == 0), "rare purchase price is explicit with no blanket credit bonus: " + subject.id);
            Check(EventPresentation.RewardSummary(purchase).Contains(purchase.amount.ToString()) && EventPresentation.RewardTitle(purchase).Contains("++"), "rare purchase preview states its price and upgrade level: " + subject.id);
            for (int index = 0; index < 3; index++)
            {
                var engine = RareV2Ready(story, 52000 + index); var option = story.options[index];
                int deck = engine.State.deck.Count, credits = engine.State.credits;
                if (index == 0)
                {
                    engine.State.credits = option.amount - 1; Check(!engine.CanChooseEventOption(index), "insufficient credits disable rare purchase: " + subject.id);
                    engine.State.credits = credits; engine.State.deck.Add(option.cardId);
                }
                Check(engine.CanChooseEventOption(index) && engine.ChooseEventOption(index), "every rare reward resolves for its subject: " + subject.id + ":" + index);
                Check(engine.State.research.completedResearchEvents.Count(id => id == story.id) == 1 && !engine.ChooseEventOption(index), "rare reward cannot be claimed twice: " + subject.id + ":" + index);
                if (index == 0)
                {
                    string rare = engine.State.deck.Last();
                    Check(engine.State.credits == credits - option.amount && engine.CardUpgradeLevel(rare) == 2 && CardInstances.BaseId(rare) == option.cardId && engine.CardUpgradeLevel(option.cardId) == 0,
                        "purchase pays exact price and creates only one independent ++ copy: " + subject.id);
                    engine.State.deck.Add(option.cardId); Check(engine.CardUpgradeLevel(engine.State.deck.Last()) == 0, "future duplicate of rare skill stays ordinary: " + subject.id);
                }
                else if (option.effect == "passive") Check(engine.State.passives.Contains(subject.passiveId), "rare passive belongs to encountered subject: " + subject.id);
                else if (option.effect == "card") Check(engine.State.deck.Count == deck + 1 && engine.State.deck.Last() == subject.cards.First(id => GameDatabase.Card(id).key == "R"), "second card reward is the subject's ordinary R: " + subject.id);
                else if (option.effect == "rare_tactical") Check(engine.State.deck.Count == deck + 1 && GameDatabase.Card(engine.State.deck.Last()).category == "tactical", "random tactical reward adds one real F card: " + subject.id);
                else if (option.effect == "tactical_module") Check(engine.State.tacticalModules == option.amount && engine.State.deck.Count == deck, "rare module reward is carried in the bag: " + subject.id);
                else if (option.effect == "trade_tactical_enhanced") Check(engine.State.deck.Count == deck + 1 && engine.IsTacticalUpgraded(engine.State.deck.Last())
                    && engine.State.credits == credits - option.amount, "rare tactical sale charges exactly and grants one enhanced F copy: " + subject.id);
                if (engine.State.rareRemovalPending) Check(engine.SkipRareRemoval(), "optional removal can be declined: " + subject.id);
                if (engine.State.rareCopyPending) Check(engine.SkipRareCopy(), "optional copies can be declined: " + subject.id);
                Check(engine.State.stage == RunStage.Map, "rare action does not softlock: " + subject.id + ":" + index);
            }
        }
        RareV2PendingAndCopies(); RareV2RunesAndPassives(); RareV2OffersAndMigration(); RareV2Reports();
    }

    static void RareV2PendingAndCopies()
    {
        EventDef story; var removal = RareV2Choice("rare_remove_cards", out story);
        int count = removal.State.deck.Count; var indices = removal.RareRemovalIndices().Take(2).ToArray();
        var loaded = new GameEngine((RunState)Clone(removal.State));
        Check(loaded.State.rareRemovalPending && !loaded.State.rareCopyPending && !loaded.SkipRareEncounter() && !loaded.ChooseEventOption(0), "saved removal resumes with other actions blocked");
        Check(!loaded.ConfirmRareRemoval(new[]{-1}) && !loaded.ConfirmRareRemoval(new[]{indices[0],indices[0]}) && !loaded.ConfirmRareRemoval(new[]{0,1,2}), "removal rejects invalid, duplicate and over-budget indices");
        Check(loaded.ConfirmRareRemoval(indices) && loaded.State.deck.Count == count - 2 && !loaded.ConfirmRareRemoval(indices), "removal removes only selected two copies once");
        removal = RareV2Choice("rare_remove_cards", out story); removal.State.deck = Enumerable.Repeat("basic_attack", 5).ToList();
        Check(!removal.ConfirmRareRemoval(new[]{0}) && removal.SkipRareRemoval(), "removal enforces five-card minimum but zero removals finishes");
        removal = RareV2Choice("rare_remove_cards", out story); string weapon = GameDatabase.Cards.First(card => card.category == "weapon").id; removal.State.deck.Add(weapon);
        Check(!removal.RareRemovalIndices().Contains(removal.State.deck.Count - 1) && !removal.ConfirmRareRemoval(new[]{removal.State.deck.Count - 1}) && removal.SkipRareRemoval(), "weapon skills cannot be removed by rare trimming");

        var copy = RareV2Choice("rare_copy_cards", out story);
        copy.State.deck = new List<string> {"basic_attack","basic_attack","basic_guard","basic_guard","basic_attack"};
        string rare = copy.AddRareRewardCard("nia_q");
        copy.State.deck.Add(GameDatabase.Cards.First(card => card.cost > 1).id);
        Check(!copy.RareCopyIndices().Contains(5) && !copy.RareCopyIndices().Contains(6), "copy excludes upgraded and original-cost-over-one cards");
        loaded = new GameEngine((RunState)Clone(copy.State)); count = loaded.State.deck.Count;
        Check(loaded.State.rareCopyPending && !loaded.SkipRareEncounter() && !loaded.ConfirmRareCopy(-1) && !loaded.ConfirmRareCopy(5), "saved copy resumes and rejects invalid or upgraded selections");
        Check(loaded.ConfirmRareCopy(0) && loaded.State.deck.Count == count + 2 && loaded.State.deck.Skip(count).All(id => id == "basic_attack") && !loaded.ConfirmRareCopy(0), "copy creates two ordinary base references exactly once");
        // Current D/F cards all cost more than one. A bounded temporary source
        // fixture checks category-neutral eligibility and restores the catalog.
        var tacticalCard = GameDatabase.Cards.First(card => card.category == "tactical"); int tacticalCost = tacticalCard.cost;
        try
        {
            tacticalCard.cost = 1; copy = RareV2Choice("rare_copy_cards", out story); string tactic = tacticalCard.id; copy.State.deck.Add(tactic);
            Check(copy.RareCopyIndices().Contains(copy.State.deck.Count - 1) && copy.ConfirmRareCopy(copy.State.deck.Count - 1) && copy.State.deck.Count(id => id == tactic) >= 3, "low-cost F cards are eligible for ordinary copies");
        }
        finally { tacticalCard.cost = tacticalCost; }
        var gear = GameDatabase.Gear.First(item => item.slot == GearSlot.Weapon && GameDatabase.Card(WeaponIdentity.CardFor(item.weaponClass) ?? item.cardId) != null);
        string d = WeaponIdentity.CardFor(gear.weaponClass) ?? gear.cardId; var weaponCard = GameDatabase.Card(d); int weaponCost = weaponCard.cost;
        try
        {
            weaponCard.cost = 1; copy = RareV2Ready(story); copy.State.gear.Add(gear.id); copy = new GameEngine((RunState)Clone(copy.State));
            count = copy.State.deck.Count(id => id == d);
            Check(copy.ChooseEventOption(2) && copy.RareCopyIndices().Contains(copy.State.deck.IndexOf(d)) && copy.ConfirmRareCopy(copy.State.deck.IndexOf(d)), "low-cost equipped D can be copied");
            copy.State.gear.Clear(); copy = new GameEngine((RunState)Clone(copy.State));
            Check(copy.State.deck.Count(id => id == d) == count + 1, "removing weapon removes only its one granted D and preserves both copied D cards");
        }
        finally { weaponCard.cost = weaponCost; }

        var impossible = RareV2Ready(ResearchEncounters.EventForSubject("nia")); impossible.State.credits = 0;
        impossible.State.passives.Add("nia_p"); impossible.State.additionalSupportRunes.AddRange(GameDatabase.Runes.Where(r => !r.main && r.tree == GameDatabase.Rune(impossible.State.mainRune).tree).Select(r => r.id));
        Check(Enumerable.Range(0,3).All(index => !impossible.CanChooseEventOption(index)) && impossible.SkipRareEncounter() && impossible.State.stage == RunStage.Map && !impossible.SkipRareEncounter(), "unavailable rare choices can be abandoned without a reward or softlock");
    }

    static void RareV2RunesAndPassives()
    {
        EventDef story; var engine = RareV2Choice("rare_support_runes", out story);
        var main = GameDatabase.Rune(engine.State.mainRune); var all = GameDatabase.Runes.Where(rune => !rune.main && rune.tree == main.tree).Select(rune => rune.id).ToArray();
        Check(all.All(id => engine.OwnedRuneIds().Contains(id)) && engine.OwnedRuneIds().Distinct().Count() == engine.OwnedRuneIds().Count, "support gift activates all remaining supports exactly once");
        var granted = engine.State.additionalSupportRunes.ToArray();
        foreach (string id in granted) engine.State.persistentTraits.counters.Add(new TraitCounter { sourceId = id, persistent = true, count = 7 });
        var another = GameDatabase.Runes.First(rune => rune.main && rune.tree != main.tree); Enter(engine, ZoneKind.Encounter); engine.State.runeChangePending = true;
        Check(engine.SetRunes(another.id, GameDatabase.Runes.First(rune => !rune.main && rune.tree == another.tree).id), "later rune change remains allowed");
        Check(granted.All(id => engine.OwnedRuneIds().Contains(id) && engine.State.persistentTraits.counters.Any(counter => counter.sourceId == id && counter.count == 7)), "gifted supports and their persistent state survive changing main trees");
        var sources = (List<TraitSource>)typeof(GameEngine).GetMethod("ActorTraits", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(engine, new object[]{false});
        Check(granted.All(id => sources.Any(source => source.id == id)), "every gifted support is a live trait source");
        var reloaded = new GameEngine((RunState)Clone(engine.State)); Check(reloaded.OwnedRuneIds().SequenceEqual(engine.OwnedRuneIds()), "all gifted runes persist across save reload");
        story = ResearchEncounters.Events.First(ev => ev.options[1].effect == "passive"); engine = RareV2Ready(story); string passive = story.options[1].passiveId;
        engine.State.passives.Add(passive); Check(!engine.CanChooseEventOption(1), "already-owned rare passive is disabled");
        engine.State.passives = GameDatabase.Passives.Where(p => p.id != passive).Take(3).Select(p => p.id).ToList();
        Check(engine.ChooseEventOption(1) && engine.State.stage == RunStage.PassiveChoice && engine.State.pendingPassive == passive, "fourth rare passive opens existing replacement screen");
        Check(engine.ReplacePassive(1) && engine.State.passives[1] == passive && engine.State.passives.Count == 3, "rare passive replacement preserves three-slot limit");
    }

    static void RareV2OffersAndMigration()
    {
        Check(GameEngine.RareOfferChanceForAct(1) == 12 && GameEngine.RareOfferChanceForAct(2) == 18 && GameEngine.RareOfferChanceForAct(3) == 25 && GameEngine.RareOfferChanceForAct(100) == 25, "rare conditional replacement chance follows global act 12/18/25");
        var engine = Ready(56001); engine.State.research.unlockedReports.AddRange(GameDatabase.Characters.Select(character => character.id));
        Enter(engine, ZoneKind.Encounter);
        for (int attempt = 0; attempt < 100 && engine.State.rareRevealEventId == null; attempt++)
        {
            Check(!engine.State.rareOfferedActs.Contains(1), "unselected rare pool replacements do not consume the act allowance"); RareV2Roll(engine);
        }
        Check(engine.State.rareRevealEventId != null && engine.State.encounterOffers.Count(id => ResearchEncounters.SubjectForEvent(id) != null) == 1 && engine.State.rareOfferedActs.SequenceEqual(new[]{1}), "only a final visible rare candidate consumes the once-per-act allowance");
        var reveal = new GameEngine((RunState)Clone(engine.State));
        Check(reveal.State.rareRevealEventId == engine.State.rareRevealEventId && reveal.State.rareRevealSourceEventId == engine.State.rareRevealSourceEventId && !reveal.State.rareRevealSeen && reveal.State.encounterOffers.SequenceEqual(engine.State.encounterOffers), "reveal target, original ordinary candidate and unseen animation state persist");
        string subject = ResearchEncounters.SubjectForEvent(engine.State.rareRevealEventId);
        Check(GameDatabase.Event(engine.State.rareRevealSourceEventId).owner == GameDatabase.Character(subject).name, "rare reveal transforms the same subject's ordinary encounter");
        RareV2Roll(engine); Check(engine.State.encounterOffers.All(id => ResearchEncounters.SubjectForEvent(id) == null), "ignoring a visible rare still consumes the act appearance allowance");
        engine.State.mode = RunMode.Endless; engine.State.endlessCycle = 1; engine.State.act = 1;
        Check(engine.GlobalAct == 4 && engine.CurrentRareOfferChance == 25, "infinite mode's first act of second cycle is global act four");
        for (int attempt = 0; attempt < 100 && engine.State.rareRevealEventId == null; attempt++) RareV2Roll(engine);
        Check(engine.State.rareRevealEventId != null && engine.State.rareOfferedActs.Contains(4), "new infinite cycle has a fresh global-act appearance allowance");
        var legacy = (RunState)Clone(reveal.State); legacy.rareEncounterVersion = 0; legacy.rareOfferedActs = null; legacy.additionalSupportRunes = null;
        var migrated = new GameEngine(legacy); Check(migrated.State.rareOfferedActs.Contains(1) && migrated.State.rareRevealSeen && migrated.State.rareEncounterVersion == 2, "old in-progress rare offers count as already appeared without replaying their reveal");
        RareV2Roll(migrated); Check(migrated.State.encounterOffers.All(id => ResearchEncounters.SubjectForEvent(id) == null), "legacy migration cannot create a second rare appearance in the same act");
        var sample = Ready(56871); sample.State.research.unlockedReports.AddRange(GameDatabase.Characters.Select(character => character.id));
        int[] appearances = new int[3];
        for (int act = 1; act <= 3; act++)
        {
            sample.State.act = act;
            for (int attempt = 0; attempt < 300; attempt++)
            {
                sample.State.rareOfferedActs.Clear(); RareV2Roll(sample);
                int visible = sample.State.encounterOffers.Count(id => ResearchEncounters.SubjectForEvent(id) != null);
                Check(visible <= 1 && sample.State.encounterOffers.Distinct().Count() == 3, "every statistical offer keeps three distinct options and at most one rare");
                if (visible > 0) appearances[act - 1]++;
            }
        }
        Check(appearances[0] > 45 && appearances[0] < 150 && appearances[1] > 75 && appearances[1] < 190 && appearances[2] > 110 && appearances[2] < 235
            && appearances[0] < appearances[1] && appearances[1] < appearances[2], "real candidate sampling grows with the 12/18/25 conditional probabilities");
        Console.WriteLine("Rare V2 visible-candidate samples (300 encounters per act): " + string.Join("/", appearances));
    }

    static void RareV2Reports()
    {
        var subject = GameDatabase.Character("nia");
        var profile = new ResearchProfile { data = 499, subjects = new List<ResearchSubjectProgress> { new ResearchSubjectProgress { subjectId = "nia", observed = true, uniqueCompleted = true, usedSkills = subject.cards.ToList() } } };
        Check(ResearchProgression.ReportCost == 500 && !ResearchProgression.CompleteReport(profile, "nia") && profile.data == 499, "new reports require full five hundred observation data");
        profile.data = 500; Check(ResearchProgression.CompleteReport(profile, "nia") && profile.data == 0 && profile.completedReports.Contains("nia") && !ResearchProgression.CompleteReport(profile, "nia"), "report completion spends exactly five hundred once");
        var old = ResearchProgression.Normalize(new ResearchProfile { completedReports = new List<string>{"nia"} });
        Check(old.completedReports.Contains("nia") && !ResearchProgression.CompleteReport(old, "nia"), "previously completed twelve-data reports remain unlocked without repayment");
    }
}
#endif
