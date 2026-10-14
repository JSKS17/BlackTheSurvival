#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    static void RareSkinStoryIsolation()
    {
        var subjects = GameDatabase.Characters.Select(c => c.id).OrderBy(id => id).ToArray();
        Check(RareSkinCatalog.Count == 91 && RareSkinCatalog.SubjectIds.OrderBy(id => id).SequenceEqual(subjects), "rare skin catalog matches the complete current subject roster");
        Check(subjects.Where(RareSkinCatalog.IsCustomOutfit).SequenceEqual(new[] { "ceres", "craver", "lucia" }), "exactly the three user-requested subjects use original casual outfits");
        var fixtures = ResearchEncounters.Events.Select(e => new EventDef {
            id = e.id, owner = e.owner, title = e.title, story = e.story, chainOnly = e.chainOnly,
            options = e.options.Select(o => (EventOption)Clone(o)).ToArray()
        }).ToList();
        var choices = fixtures.SelectMany(e => e.options).ToArray();
        var fields = typeof(EventOption).GetFields().Where(f => f.Name != "label" && f.Name != "description").ToArray();
        var preserved = choices.Select(o => fields.Select(f => f.GetValue(o)).ToArray()).ToArray();
        var identities = fixtures.Select(e => new { e.id, e.owner, e.chainOnly, e.options }).ToArray();
        var reports = new Dictionary<string, string>();
        RareSkinStories.Configure(fixtures, reports);
        Check(RareSkinStories.Preview(null) == "" && RareSkinStories.Preview("  ") == ""
            && RareSkinStories.Preview("한 문장입니다.") == "한 문장입니다."
            && RareSkinStories.Preview("마침표 없는 설명") == "마침표 없는 설명", "rare scene previews handle absent, single-sentence and unpunctuated text safely");
        Check(fixtures.Count == subjects.Length && reports.Keys.OrderBy(id => id).SequenceEqual(subjects), "all ninety-one skin scenes provide exactly one report");
        for (int i = 0; i < choices.Length; i++)
            Check(fields.Select(f => f.GetValue(choices[i])).SequenceEqual(preserved[i]), "skin narrative replacement preserves every reward, cost, requirement and follow-up field: " + i);
        for (int i = 0; i < fixtures.Count; i++)
        {
            var scene = fixtures[i]; string subject = scene.id.Substring(ResearchEncounters.EventPrefix.Length);
            Check(scene.id == identities[i].id && scene.owner == identities[i].owner && scene.chainOnly == identities[i].chainOnly
                && ReferenceEquals(scene.options, identities[i].options), "skin writing preserves scene identity and the original ordered choice objects: " + subject);
            Check(!string.IsNullOrWhiteSpace(RareSkinCatalog.NameForSubject(subject)) && reports[subject].Contains(RareSkinCatalog.NameForSubject(subject)), "journal displays the exact selected skin name: " + subject);
            Check(scene.story.Contains("하나") && scene.options.Length == 3, "skin scene remains part of Hana's escape and has three choices: " + subject);
            string fullStory = scene.story;
            string firstSentence = fullStory.Substring(0, fullStory.IndexOf('.') + 1);
            Check(RareSkinStories.Preview(fullStory) == firstSentence + " …" && scene.story == fullStory,
                "rare preview shows the first sentence while keeping the complete story unchanged: " + subject);
            if (RareSkinCatalog.IsCustomOutfit(subject)) Check(reports[subject].Contains("팬 창작 사복"), "original casual scenes are identified as fan designs: " + subject);
        }
    }

    static void RareEncounterRewards()
    {
        RareEncounterV2();
    }

    static void RareEncounterAvailability()
    {
        var followup = EncounterChains.Events.First(e => e.chainOnly);
        int promised = 0, rare = 0;
        foreach (int seed in Enumerable.Range(28600, 250))
        {
            var engine = Ready(seed);
            engine.State.research.unlockedReports.AddRange(GameDatabase.Characters.Select(c => c.id));
            engine.State.pendingEncounterFollowups.Add(new PendingEncounterFollowup { eventId = followup.id, owner = followup.owner, priorityNext = true });
            Enter(engine, ZoneKind.Encounter);
            var offers = engine.CurrentEncounterOffers.ToArray();
            Check(offers.Length == 3 && offers.Select(e => e.owner).Distinct().Count() == 3, "rare replacement preserves the three unique owners alongside a promise");
            if (offers.Any(e => e.id == followup.id)) promised++;
            rare += offers.Count(e => ResearchEncounters.SubjectForEvent(e.id) != null);
        }
        Check(promised > 170 && promised < 230, "rare offers retain the existing eighty percent promise priority");
        Check(rare > 30 && rare < 135, "rare appearances stay low even alongside promised encounters");
        var locked = Ready(28899); Enter(locked, ZoneKind.Encounter);
        Check(locked.CurrentEncounterOffers.All(e => ResearchEncounters.SubjectForEvent(e.id) == null), "no report means no rare offer");
        var completed = Ready(28900); completed.State.research.unlockedReports.AddRange(GameDatabase.Characters.Select(c => c.id));
        completed.State.research.completedResearchEvents.AddRange(ResearchEncounters.Events.Select(e => e.id));
        Enter(completed, ZoneKind.Encounter);
        Check(completed.CurrentEncounterOffers.All(e => ResearchEncounters.SubjectForEvent(e.id) == null), "completed rare scenes cannot be offered twice in one expedition");
        Check(ResearchProgression.ForSubject("nia").unlockText.Contains("12%") && ResearchProgression.ForSubject("nia").unlockText.Contains("레어 조우"), "research reports explain rare availability and its conditional rate");
    }
}
#endif
