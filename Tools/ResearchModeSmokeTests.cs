#if LUMIA_TEST_RUNNER
using System;
using System.IO;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    static void ModeUnlockMigration()
    {
        var fresh = ResearchProgression.Normalize(new ResearchProfile());
        Check(RunModes.IsUnlocked(fresh, RunMode.Normal) && !RunModes.IsUnlocked(fresh, RunMode.Hard)
            && !RunModes.IsUnlocked(fresh, RunMode.Endless), "a fresh player can only start normal mode");
        var old = new ResearchProfile { data = 17, totalData = 24, totalRuns = 3, totalWins = 1 };
        old.completedReports.Add("nia");
        ResearchProgression.Normalize(old); ResearchProgression.Normalize(old);
        Check(old.normalCleared && !old.hardCleared && RunModes.IsUnlocked(old, RunMode.Hard)
            && !RunModes.IsUnlocked(old, RunMode.Endless), "a pre-mode normal escape unlocks hard, but not endless");
        Check(old.data == 17 && old.totalData == 24 && old.totalRuns == 3 && old.totalWins == 1
            && old.completedReports.SequenceEqual(new[] { "nia" }), "mode migration never pays rewards again or changes existing research");
        var hard = ResearchProgression.Normalize(new ResearchProfile { hardCleared = true });
        Check(hard.normalCleared && hard.hardCleared && RunModes.IsUnlocked(hard, RunMode.Endless), "hard completion monotonically implies the lower unlock");
    }

    static void ModeSettlementProgression()
    {
        var profile = new ResearchProfile();
        foreach (RunMode mode in new[] { RunMode.Normal, RunMode.Hard, RunMode.Endless })
        {
            var failed = Ready(62010 + (int)mode).State; failed.mode = mode; failed.stage = RunStage.Lost;
            var lossProfile = new ResearchProfile(); ResearchProgression.Settle(lossProfile, failed);
            Check(!lossProfile.normalCleared && !lossProfile.hardCleared && lossProfile.totalWins == 0,
                "losing any mode never unlocks a higher mode: " + mode);
        }
        var normal = Ready(62020).State; normal.mode = RunMode.Normal; normal.stage = RunStage.Won;
        var receipt = ResearchProgression.Settle(profile, normal);
        Check(profile.normalCleared && !profile.hardCleared && receipt.mode == RunMode.Normal && profile.totalWins == 1,
            "a normal escape permanently unlocks hard and records its mode");
        int data = profile.data;
        var replay = ResearchProgression.Settle(profile, normal);
        Check(replay.alreadySettled && replay.mode == RunMode.Normal && profile.totalWins == 1 && profile.data == data,
            "replaying a normal clear neither pays nor unlocks a second mode");
        var hard = Ready(62021).State; hard.mode = RunMode.Hard; hard.stage = RunStage.Won;
        receipt = ResearchProgression.Settle(profile, hard);
        Check(profile.hardCleared && RunModes.IsUnlocked(profile, RunMode.Endless) && receipt.mode == RunMode.Hard
            && profile.settlements.Last().mode == RunMode.Hard, "hard escape records the mode and unlocks endless");
        var stored = ResearchFixtureDecode(ResearchFixtureEncode(profile));
        Check(stored.normalCleared && stored.hardCleared && stored.history[0].mode == RunMode.Hard,
            "mode unlocks and receipt identity survive profile serialization");
        var endless = Ready(62022).State; endless.mode = RunMode.Endless; endless.stage = RunStage.Won;
        var impossible = new ResearchProfile(); receipt = ResearchProgression.Settle(impossible, endless);
        Check(!receipt.escaped && receipt.escapeBonus == 0 && impossible.totalWins == 0
            && !impossible.hardCleared, "an invalid endless-Won record cannot impersonate a hard clear");
    }

    static void ModeUnlockTransactions()
    {
        string parent = Path.GetFullPath(Path.Combine("Temp", "ModeUnlockTests"));
        string scratch = Path.GetFullPath(Path.Combine(parent, Guid.NewGuid().ToString("N")));
        Check(scratch.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "mode fixture stays in its private workspace");
        Directory.CreateDirectory(scratch);
        try
        {
            string path = Path.Combine(scratch, "profile.bin"); int serializations = 0;
            var store = new ResearchStore(path, p => { if (++serializations == 2) throw new IOException("injected mode write failure"); return ResearchFixtureEncode(p); }, ResearchFixtureDecode);
            var normal = Ready(62030).State; normal.stage = RunStage.Won;
            ResearchSettlement report; string error;
            Check(!store.TrySettle(normal, out report, out error) && error != "" && !normal.research.settled
                && !store.Profile.normalCleared && !File.Exists(path), "a failed disk commit never publishes a normal unlock");
            Check(store.TrySettle(normal, out report, out error) && error == "" && store.Profile.normalCleared,
                "retry commits the normal clear and unlock together");
            var hard = Ready(62031).State; hard.mode = RunMode.Hard; hard.stage = RunStage.Won;
            using (var held = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                Check(!store.TrySettle(hard, out report, out error) && error != "" && !store.Profile.hardCleared
                    && !hard.research.settled, "a locked profile defers hard unlock without losing the pending clear");
            Check(store.TrySettle(hard, out report, out error) && store.Profile.hardCleared,
                "hard clear unlocks endless only after its durable retry");
            var reloaded = new ResearchStore(path, ResearchFixtureEncode, ResearchFixtureDecode);
            Check(RunModes.IsUnlocked(reloaded.Profile, RunMode.Endless) && reloaded.Profile.totalWins == 2,
                "a fresh store sees both durable clears");
            string bytes = File.ReadAllText(path);
            Check(!reloaded.TrySettle(hard, out report, out error) && error == "" && File.ReadAllText(path) == bytes,
                "duplicate hard settlement leaves the permanent profile byte-for-byte intact");
        }
        finally
        {
            if (Path.GetFullPath(scratch).StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                Directory.Delete(scratch, true);
        }
    }

    static void EndlessArchiveResearch()
    {
        var state = Ready(62040).State; state.mode = RunMode.Endless;
        var oldBoss = new MapNode { id = state.map.Max(n => n.id) + 100, act = 3, row = 11, lane = 1,
            kind = ZoneKind.Boss, visited = true, assignedOpponent = "nia" };
        state.archivedMapNodes.Add(oldBoss);
        state.research.bossInterruptions.Add(oldBoss.id + ":1");
        state.research.bossInterruptions.Add(oldBoss.id + ":1");
        state.research.bossInterruptions.Add(oldBoss.id + ":999");
        state.research.bossInterruptions.Add((oldBoss.id + 1) + ":1");
        state.stage = RunStage.Lost;
        var report = ResearchProgression.CalculateReport(new ResearchProfile(), state);
        Check(report.interruptions == 1 && report.mode == RunMode.Endless && !report.escaped,
            "endless settlement retains valid previous-cycle boss interruptions and rejects duplicates or invalid phases");
    }
}
#endif
