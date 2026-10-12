#if LUMIA_TEST_RUNNER
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Lumia;

public static partial class CoreSmokeTests
{
    // Fixture-only serializer exercises disk transactions outside Unity. The native
    // verification separately exercises the shipping JsonUtility representation.
    static string ResearchFixtureEncode(ResearchProfile profile)
    {
        using (var stream = new MemoryStream())
        { new BinaryFormatter().Serialize(stream, profile); return Convert.ToBase64String(stream.ToArray()); }
    }

    static ResearchProfile ResearchFixtureDecode(string text)
    {
        using (var stream = new MemoryStream(Convert.FromBase64String(text)))
            return (ResearchProfile)new BinaryFormatter().Deserialize(stream);
    }

    static RunState ResearchStoreRun(int seed)
    {
        var engine = Ready(seed);
        ResearchProgression.ObserveSubject(engine.State, "nia");
        engine.State.stage = RunStage.Lost;
        return engine.State;
    }

    static void ResearchStoreTransactions()
    {
        string scratchRoot = Path.GetFullPath(Path.Combine("Temp", "ResearchStoreTests"));
        string scratch = Path.GetFullPath(Path.Combine(scratchRoot, Guid.NewGuid().ToString("N")));
        Check(scratch.StartsWith(scratchRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "research fixture remains within its private test folder");
        Directory.CreateDirectory(scratch);
        try
        {
            string path = Path.Combine(scratch, "research.bin");
            var store = new ResearchStore(path, ResearchFixtureEncode, ResearchFixtureDecode);
            Check(!store.IsReadOnly && store.Profile.data == 0 && !File.Exists(path), "missing account starts empty without writing a profile");
            var stale = new ResearchStore(path, ResearchFixtureEncode, ResearchFixtureDecode);
            var state = ResearchStoreRun(9911);
            ResearchSettlement settlement; string error;
            Check(store.TrySettle(state, out settlement, out error) && error == "" && store.Profile.data > 0, "real observed failed run is durably settled");
            string first = File.ReadAllText(path); int initialData = store.Profile.data;
            Check(File.Exists(path + ".bak") && state.research.settled, "first settlement creates backup and caches its receipt on the run");
            Check(!store.TrySettle(state, out settlement, out error) && error == "" && File.ReadAllText(path) == first, "duplicate settlement never writes or pays again");
            var next = ResearchStoreRun(9911);
            Check(stale.TrySettle(next, out settlement, out error) && stale.Profile.totalRuns == 2 && stale.Profile.data >= initialData,
                "stale second store reloads latest account and preserves both same-seed distinct runs");
            int before = stale.Profile.data;
            Check(!stale.TryCompleteReport("nia", out error) && error != "" && stale.Profile.data == before,
                "incomplete research cannot spend data or unlock a report");

            string validBackup = File.ReadAllText(path + ".bak");
            File.WriteAllText(path, "damaged primary");
            var recovered = new ResearchStore(path, ResearchFixtureEncode, ResearchFixtureDecode);
            Check(recovered.RecoveredFromBackup && !recovered.IsReadOnly, "damaged primary recovers a valid backup");
            Check(recovered.TrySettle(ResearchStoreRun(9913), out settlement, out error), "recovered account can commit a new run");
            Check(File.ReadAllText(path + ".bak") == validBackup, "recovery preserves valid backup instead of replacing it with damaged primary");

            var future = new ResearchProfile { version = 2 };
            File.WriteAllText(path, ResearchFixtureEncode(future));
            var futureStore = new ResearchStore(path, ResearchFixtureEncode, ResearchFixtureDecode);
            Check(futureStore.IsReadOnly && !futureStore.RecoveredFromBackup, "future schema is preserved rather than rolled back to an old backup");
            string futureBytes = File.ReadAllText(path);
            Check(!futureStore.TrySettle(ResearchStoreRun(9914), out settlement, out error) && error != "" && File.ReadAllText(path) == futureBytes,
                "future schema blocks writes and retains pending run for retry");

            File.WriteAllText(path, "broken"); File.WriteAllText(path + ".bak", "also broken");
            var broken = new ResearchStore(path, ResearchFixtureEncode, ResearchFixtureDecode);
            Check(broken.IsReadOnly && !broken.TrySettle(ResearchStoreRun(9915), out settlement, out error) && error != ""
                && File.ReadAllText(path) == "broken" && File.ReadAllText(path + ".bak") == "also broken", "unrecoverable account is never silently reset or overwritten");

            string faultPath = Path.Combine(scratch, "fault.bin"); int serializeCalls = 0;
            var failing = new ResearchStore(faultPath, p => { if (++serializeCalls == 2) throw new IOException("injected persistence failure"); return ResearchFixtureEncode(p); }, ResearchFixtureDecode);
            var pending = ResearchStoreRun(9916);
            Check(!failing.TrySettle(pending, out settlement, out error) && error != "" && !pending.research.settled
                && failing.Profile.totalRuns == 0 && !File.Exists(faultPath), "failed persistence rolls back both account and run receipt");
            Check(failing.TrySettle(pending, out settlement, out error) && pending.research.settled && failing.Profile.totalRuns == 1,
                "failed settlement retries successfully exactly once");
            using (var held = new FileStream(faultPath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var blockedRun = ResearchStoreRun(9917); string committed = File.ReadAllText(faultPath);
                Check(!failing.TrySettle(blockedRun, out settlement, out error) && error != "" && !blockedRun.research.settled
                    && File.ReadAllText(faultPath) == committed, "concurrent writer lock defers settlement without losing data");
            }
        }
        finally
        {
            if (Path.GetFullPath(scratch).StartsWith(scratchRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                Directory.Delete(scratch, true);
        }
    }
}
#endif
