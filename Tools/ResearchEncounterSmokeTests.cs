#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    static void ResearchEncounterCoverage()
    {
        Check(ResearchEncounters.Events.Count == ResearchProgression.ReportSubjects.Count(), "every subject has one research encounter");
        Check(ResearchEncounters.Events.Select(x => x.id).Distinct().Count() == ResearchProgression.ReportSubjects.Count(), "research encounter IDs are unique");
        Check(ResearchEncounters.Events.Select(x => x.title).Distinct().Count() == ResearchProgression.ReportSubjects.Count(), "research stories have distinct titles");
        foreach (var character in GameDatabase.Characters)
        {
            var story = ResearchEncounters.EventForSubject(character.id);
            Check(story != null && story.owner == character.name && GameDatabase.Event(story.id) == story, "research lookup resolves owner and global event: " + character.id);
            Check(ResearchEncounters.SubjectForEvent(story.id) == character.id && ResearchEncounters.SubjectForEvent("event_" + character.id) == null, "research lookup distinguishes existing events: " + character.id);
            Check(!string.IsNullOrWhiteSpace(story.story) && story.options.Length == 3 && !string.IsNullOrWhiteSpace(ResearchEncounters.ReportText(character.id)), "research encounter and report have complete narrative: " + character.id);
            foreach (var option in story.options)
            {
                Check(!string.IsNullOrWhiteSpace(option.label) && !string.IsNullOrWhiteSpace(option.description), "research decisions are described: " + story.id);
                if (option.effect == "card" || option.effect == "trade_card") Check(GameDatabase.Card(option.cardId) != null, "research reward card exists: " + option.cardId);
                if (option.effect == "food") Check(GameDatabase.Food(option.objectId) != null, "research food reward exists: " + option.objectId);
            }
            for (int choice = 0; choice < story.options.Length; ++choice)
            {
                var e = Ready(1200 + choice); e.State.research.unlockedReports.Add(character.id);
                Enter(e, ZoneKind.Encounter); e.State.encounterOffers = new List<string> { story.id };
                Check(e.SelectEncounter(story.id), "unlocked research story can be selected: " + character.id);
                e.State.credits = 1000; e.State.hp = e.State.maxHp; e.State.passives.Clear();
                Check(e.CanChooseEventOption(choice) && e.ChooseEventOption(choice), "research action succeeds: " + story.id + ":" + choice);
                Check(e.State.research.completedResearchEvents.Count(id => id == story.id) == 1, "research scene is marked consumed exactly once: " + story.id);
                Check(!e.ChooseEventOption(choice), "research action cannot repeat: " + story.id);
                if (e.State.cardUpgradePending) Check(e.SkipCardUpgrade(), "research upgrade can be declined without blocking the run");
                if (e.State.runeChangePending) Check(e.FinishRuneChange(), "research rune choice can finish without blocking the run");
                if (e.State.stage == RunStage.PassiveChoice) Check(e.ReplacePassive(), "research passive choice can be declined");
                if (e.State.rareRemovalPending) Check(e.SkipRareRemoval(), "research card removal can finish without removing a card");
                if (e.State.rareCopyPending) Check(e.SkipRareCopy(), "research card copies can be declined without blocking the run");
                Check(e.State.stage == RunStage.Map, "every research decision returns to map: " + story.id);
            }
        }
    }

    static void ResearchEncounterOffers()
    {
        int featured = 0;
        foreach (int seed in Enumerable.Range(18000, 250))
        {
            var e = new GameEngine(seed);
            Check(e.SetResearchUnlocks(GameDatabase.Characters.Select(c => c.id)), "all completed reports snapshot at preparation");
            var m = GameDatabase.Runes.First(r => r.main); e.SetRunes(m.id, GameDatabase.Runes.First(r => !r.main && r.tree == m.tree).id);
            e.SelectStartingPassive(e.State.passiveOffers[0]); foreach (string id in e.State.draftOffers.Take(3).ToArray()) e.ToggleDraft(id); e.BeginJourney();
            Check(!e.SetResearchUnlocks(new string[0]) && e.State.research.unlockedReports.Count == 91, "running expedition snapshot cannot be changed");
            Enter(e, ZoneKind.Encounter);
            var offers = e.State.encounterOffers.Select(GameDatabase.Event).ToArray();
            Check(offers.Length == 3 && offers.Select(x => x.owner).Distinct().Count() == 3, "research offers retain three distinct subjects");
            featured += offers.Count(x => ResearchEncounters.SubjectForEvent(x.id) != null);
            var saved = new GameEngine((RunState)Clone(e.State));
            Check(saved.State.encounterOffers.SequenceEqual(e.State.encounterOffers) && saved.State.rngState == e.State.rngState
                && saved.State.research.unlockedReports.SequenceEqual(e.State.research.unlockedReports), "research offers and unlocks persist without reroll");
        }
        Check(featured > 40 && featured < 145, "rare scenes replace around twelve percent of eligible ordinary offers");
        var locked = Ready(88); Enter(locked, ZoneKind.Encounter);
        var nia = ResearchEncounters.EventForSubject("nia"); locked.State.encounterOffers = new List<string> { nia.id };
        Check(!locked.SelectEncounter(nia.id), "locked research event cannot be injected into an offer list");
        locked.State.research.unlockedReports.Add("nia"); locked.State.research.completedResearchEvents.Add(nia.id);
        Check(!locked.SelectEncounter(nia.id), "completed research scene cannot be selected again this run");
    }
}
#endif
