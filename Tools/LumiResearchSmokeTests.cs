#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    public static void LumiResearchAndAppearance()
    {
        Check(GameDatabase.Character("lumi") == null && !GameDatabase.Events.Any(ev => ev.owner == "루미"), "Lumi stays out of the combat roster and ordinary encounter catalog");
        Check(ResearchProgression.ReportSubjects.Count() == GameDatabase.Characters.Count + 1
            && ResearchProgression.ForSubject("lumi") != null, "Lumi has one research-only report entry");
        Check(ResearchProgression.CostForReport("lumi") == 1000 && ResearchProgression.CostForReport("nia") == 500, "Lumi's report has its own 1000-data price without changing existing reports");
        var profile = new ResearchProfile { data = 1200, totalData = 1200 };
        Check(ResearchProgression.Tasks(profile, "lumi").Select(task => task.target).SequenceEqual(new[] { 3, 1, 50 }), "Lumi tasks require three discoveries, one object purchase and fifty earned credits");
        var first = ResearchRun(64001); int rng = first.rngState, hp = first.hp, credits = first.credits;
        ResearchProgression.LumiFound(first); ResearchProgression.LumiFound(first);
        ResearchProgression.LumiObjectPurchased(first); ResearchProgression.LumiCreditsEarned(first, 40);
        ResearchProgression.LumiCreditsEarned(first, -50);
        Check(first.rngState == rng && first.hp == hp && first.credits == credits, "Lumi research records observations without gameplay resource or RNG changes");
        first.stage = RunStage.Lost;
        ResearchProgression.Settle(profile, first);
        int data = profile.data;
        ResearchProgression.Settle(profile, first);
        Check(profile.lumiDiscoveries == 2 && profile.lumiObjectPurchases == 1 && profile.lumiCreditsEarned == 40 && profile.data == data, "Lumi progress merges once per settled run and ignores negative credits");
        Check(!ResearchProgression.CompleteReport(profile, "lumi"), "Two discoveries and forty credits cannot unlock Lumi early");
        var second = ResearchRun(64002); ResearchProgression.LumiFound(second); ResearchProgression.LumiCreditsEarned(second, 20);
        second.stage = RunStage.Lost; ResearchProgression.Settle(profile, second);
        Check(profile.lumiDiscoveries == 3 && profile.lumiCreditsEarned == 60 && ResearchProgression.Tasks(profile, "lumi").All(task => task.completed), "Lumi progress combines across expeditions and real twenty-credit payouts pass the fifty-credit threshold");
        profile.data = 999;
        Check(!ResearchProgression.CompleteReport(profile, "lumi") && !profile.completedReports.Contains("lumi"), "All Lumi tasks still need the full 1000-data payment");
        profile.data = 1000; profile.totalData = Math.Max(profile.totalData, 1000);
        Check(ResearchProgression.CompleteReport(profile, "lumi") && profile.data == 0, "Lumi report spends exactly 1000 data after all three tasks");
        Check(!ResearchProgression.CompleteReport(profile, "lumi") && profile.data == 0, "A completed Lumi report cannot be purchased twice");
        var snapshot = new GameEngine(64003);
        Check(snapshot.SetResearchUnlocks(profile.completedReports) && snapshot.State.research.unlockedReports.Contains("lumi"), "Completed Lumi report enters the next expedition's immutable unlock snapshot");

        var rare = ResearchEncounters.EventForSubject("lumi");
        Check(rare != null && rare.owner == "루미" && RareSkinCatalog.NameForSubject("lumi") == "버추얼 루미"
            && rare.options.Select(option => option.effect).SequenceEqual(new[] { "lumi_shop", "tactical_module", "lumi_cooked_foods" }), "Virtual Lumi has exactly the requested three authored choices");
        int appearances = 0;
        foreach (int seed in Enumerable.Range(65000, 2000))
        {
            var engine = Ready(seed); engine.State.research.unlockedReports.Add("lumi"); Enter(engine, ZoneKind.Encounter);
            if (!engine.State.encounterOffers.Contains(rare.id)) continue;
            appearances++;
            Check(engine.State.encounterOffers.Count == 3 && engine.State.encounterOffers.Distinct().Count() == 3, "Standalone rare Lumi keeps three distinct encounter offers");
            Check(engine.State.rareOfferedActs.Contains(engine.GlobalAct) && engine.State.rareRevealSourceEventId == null, "Lumi shares the act rare limit without inventing an ordinary Lumi scene");
            var restored = new GameEngine((RunState)Clone(engine.State));
            Check(restored.State.encounterOffers.SequenceEqual(engine.State.encounterOffers) && restored.State.rngState == engine.State.rngState, "Lumi rare offers survive reload without rerolling");
        }
        Check(appearances > 0 && appearances < 45, "Standalone Lumi uses the low ordinary-rare candidate scale: " + appearances + "/2000");

        string scratchRoot = Path.GetFullPath(Path.Combine("Temp", "LumiResearchTests"));
        string scratch = Path.GetFullPath(Path.Combine(scratchRoot, Guid.NewGuid().ToString("N")));
        Check(scratch.StartsWith(scratchRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "Lumi profile fixture stays inside its generated test directory");
        Directory.CreateDirectory(scratch);
        try
        {
            string file = Path.Combine(scratch, "research.bin"), error;
            var store = new ResearchStore(file, ResearchFixtureEncode, ResearchFixtureDecode);
            Check(!store.UsesRareAppearance("nia") && !store.TrySetRareAppearance("nia", true, out error), "An unfinished report cannot enable rare combat appearance");
            profile.completedReports.Add("nia");
            File.WriteAllText(file, ResearchFixtureEncode(profile));
            store = new ResearchStore(file, ResearchFixtureEncode, ResearchFixtureDecode);
            int beforeData = store.Profile.data, beforeRuns = store.Profile.totalRuns;
            Check(store.TrySetRareAppearance("nia", true, out error) && store.UsesRareAppearance("nia"), "A completed subject enables its rare appearance");
            var reload = new ResearchStore(file, ResearchFixtureEncode, ResearchFixtureDecode);
            Check(reload.UsesRareAppearance("nia") && reload.Profile.lumiDiscoveries == 3 && reload.Profile.lumiCreditsEarned == 60, "Appearance and Lumi research survive profile serialization and reload");
            Check(reload.TrySetRareAppearance("nia", false, out error) && !reload.UsesRareAppearance("nia")
                && reload.Profile.data == beforeData && reload.Profile.totalRuns == beforeRuns, "Disabling an appearance changes no research currency or settlement state");
            profile.rareAppearanceSubjects = new List<string> { "nia", "nia", "missing", "aya" };
            ResearchProgression.Normalize(profile);
            Check(profile.rareAppearanceSubjects.SequenceEqual(new[] { "nia" }), "Legacy and malformed appearance lists normalize to completed, distinct known reports");
            var legacy = ResearchProgression.Normalize(new ResearchProfile { rareAppearanceSubjects = null });
            Check(legacy.rareAppearanceSubjects.Count == 0 && legacy.lumiDiscoveries == 0, "Older accounts default to original appearance and zero Lumi progress");
        }
        finally
        {
            if (Path.GetFullPath(scratch).StartsWith(scratchRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) Directory.Delete(scratch, true);
        }
        Console.WriteLine("LUMI RESEARCH AND APPEARANCE PASS / standalone rare appearances=" + appearances);
    }
}
#endif
