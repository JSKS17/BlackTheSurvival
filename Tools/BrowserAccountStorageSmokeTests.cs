using System;
using System.IO;
using System.Linq;
using System.Globalization;
using Lumia;

static class BrowserAccountStorageSmokeTests
{
    static int checks;
    const string RunName = "lumia-vf-loop-v1.json";
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    static void Valid(string json) { if (json == null || !json.StartsWith("{\"valid\":", StringComparison.Ordinal)) throw new InvalidDataException("fixture invalid"); }
    static void Throws(Action action, string message) { bool threw = false; try { action(); } catch (Exception error) { threw = true; Console.WriteLine(message + ": " + error.Message); } Check(threw, message); }
    static string[] Names(string root) => Directory.GetFiles(root).Select(Path.GetFileName).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    static BrowserSaveBundle Bundle(string run = null, string research = null, params BrowserPendingSave[] pending) =>
        new BrowserSaveBundle { run = run, research = research, pending = pending };

    static int Main()
    {
        string root = Path.Combine(Environment.CurrentDirectory, "Temp", "bts-account-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string empty = BrowserAccountStorage.Serialize(Bundle());
            Check(empty == "{\"schemaVersion\":1,\"gameVersion\":\"1.0.6\",\"run\":null,\"research\":null,\"pending\":[],\"tutorialSeen\":false}", "explicit JSON null");
            string unicode = "한글🙂\n\r\t\"\\\0";
            Check(BrowserAccountStorage.Serialize(Bundle(unicode)).Contains("한글🙂\\n\\r\\t\\\"\\\\\\u0000"), "JSON escape and Unicode preservation");
            Check(BrowserAccountStorage.Hash("") == "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", "SHA256 exact bytes");
            string a = RunName + ".research-pending-a.json", b = RunName + ".research-pending-b.json";
            var sorted = Bundle(null, null, new BrowserPendingSave { name = b, json = "{\"valid\":2}" }, new BrowserPendingSave { name = a, json = "{\"valid\":1}" });
            Check(BrowserAccountStorage.Serialize(sorted).IndexOf(a, StringComparison.Ordinal) < BrowserAccountStorage.Serialize(sorted).IndexOf(b, StringComparison.Ordinal), "stable ordinal pending ordering");
            Check(BrowserAccountStorage.Serialize(sorted) == BrowserAccountStorage.Serialize(sorted), "no volatile bundle fields");
            var accountSettings = new BrowserGameSettings { volume = .21f, musicVolume = .32f, effectsVolume = .43f, sound = false, reduceMotion = true, fullScreen = true };
            var settingsOnly = Bundle(); settingsOnly.settings = accountSettings;
            const string settingsJson = "{\"volume\":0.21,\"musicVolume\":0.32,\"effectsVolume\":0.43,\"sound\":false,\"reduceMotion\":true,\"fullScreen\":true}";
            Check(BrowserAccountStorage.SerializeSettings(accountSettings) == settingsJson, "all six account preferences serialized exactly");
            Check(BrowserAccountStorage.Serialize(settingsOnly).EndsWith(",\"settings\":" + settingsJson + "}", StringComparison.Ordinal), "settings-only bundle retains explicit user preferences");
            var culture = CultureInfo.CurrentCulture;
            try { CultureInfo.CurrentCulture = new CultureInfo("de-DE"); Check(BrowserAccountStorage.SerializeSettings(accountSettings) == settingsJson, "settings JSON uses invariant decimal points"); }
            finally { CultureInfo.CurrentCulture = culture; }
            foreach (float invalid in new[] { -.01f, 1.01f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                var invalidSettings = new BrowserGameSettings { volume = invalid };
                Throws(() => BrowserAccountStorage.SerializeSettings(invalidSettings), "invalid settings volume rejected");
            }

            File.WriteAllText(Path.Combine(root, RunName), "broken");
            File.WriteAllText(Path.Combine(root, RunName + ".bak"), "{\"valid\":\"backup\"}");
            Check(BrowserAccountStorage.ReadSlot(Path.Combine(root, RunName), Valid) == "{\"valid\":\"backup\"}", "recover valid backup without changing primary");
            Check(File.ReadAllText(Path.Combine(root, RunName)) == "broken", "damaged primary preserved");
            File.WriteAllText(Path.Combine(root, BrowserAccountStorage.ResearchFileName), "{\"valid\":\"research-old\"}");
            File.WriteAllText(Path.Combine(root, BrowserAccountStorage.ResearchFileName + ".bak"), "{\"valid\":\"research-backup\"}");
            File.WriteAllText(Path.Combine(root, a), "{\"valid\":\"pending-old\"}");
            File.WriteAllText(Path.Combine(root, BrowserAccountStorage.MetadataFileName), "metadata-old");
            File.WriteAllText(Path.Combine(root, BrowserAccountStorage.SettingsFileName), "settings-old");
            File.WriteAllText(Path.Combine(root, BrowserAccountStorage.SettingsFileName + ".bak"), "settings-old-backup");
            string[] original = Names(root);
            var replacement = Bundle("{\"valid\":\"new-run\"}", null, new BrowserPendingSave { name = b, json = "{\"valid\":\"pending-new\"}" });
            replacement.settings = accountSettings;
            int writes = 0;
            Throws(() => BrowserAccountStorage.Apply(root, RunName, replacement, Valid, Valid, (path, json) =>
            { File.WriteAllText(path, json); if (++writes == 2) throw new IOException("simulated write failure"); }), "replacement failure reported");
            Check(Names(root).SequenceEqual(original), "failure restores complete file set");
            Check(File.ReadAllText(Path.Combine(root, RunName)) == "broken" && File.ReadAllText(Path.Combine(root, a)) == "{\"valid\":\"pending-old\"}", "failure restores exact raw originals");
            Check(File.ReadAllText(Path.Combine(root, BrowserAccountStorage.SettingsFileName)) == "settings-old"
                && File.ReadAllText(Path.Combine(root, BrowserAccountStorage.SettingsFileName + ".bak")) == "settings-old-backup", "write failure restores exact account settings and backup");
            Check(File.ReadAllText(Path.Combine(root, "cloud-recovery", RunName + ".bak")) == "{\"valid\":\"backup\"}", "complete recovery backup retained");
            Throws(() => BrowserAccountStorage.Apply(root, RunName, replacement, Valid, Valid, afterReplace: () =>
            { File.WriteAllText(Path.Combine(root, BrowserAccountStorage.MetadataFileName), "new-meta"); throw new IOException("ack failure"); }), "metadata failure aborts transaction");
            Check(File.ReadAllText(Path.Combine(root, BrowserAccountStorage.MetadataFileName)) == "metadata-old", "metadata rollback");
            Check(Names(root).SequenceEqual(original), "post-replacement failure restores original pending and backups");
            Check(File.ReadAllText(Path.Combine(root, BrowserAccountStorage.SettingsFileName)) == "settings-old", "metadata failure rolls account settings back");

            var traversal = Bundle(null, null, new BrowserPendingSave { name = "../bad.json", json = "{\"valid\":1}" });
            Throws(() => BrowserAccountStorage.Apply(root, RunName, traversal, Valid, Valid), "path traversal rejected");
            Check(Names(root).SequenceEqual(original), "validation occurs before mutation");
            Throws(() => BrowserAccountStorage.Apply(root, RunName, Bundle("not-json"), Valid, Valid), "invalid primary rejected before mutation");
            Throws(() => BrowserAccountStorage.Validate(Bundle(null, null, new BrowserPendingSave { name = a, json = "{\"valid\":1}" }, new BrowserPendingSave { name = a, json = "{\"valid\":2}" }), RunName, Valid, Valid), "duplicate pending rejected");

            BrowserAccountStorage.Apply(root, RunName, replacement, Valid, Valid);
            Check(File.ReadAllText(Path.Combine(root, RunName)) == replacement.run, "run replacement");
            Check(!File.Exists(Path.Combine(root, RunName + ".bak")), "old run backup removed from active slot");
            Check(!File.Exists(Path.Combine(root, BrowserAccountStorage.ResearchFileName)) && !File.Exists(Path.Combine(root, BrowserAccountStorage.ResearchFileName + ".bak")), "inner research null clears primary and backup");
            Check(!File.Exists(Path.Combine(root, a)) && File.Exists(Path.Combine(root, b)), "pending set replaced completely");
            Check(File.ReadAllText(Path.Combine(root, BrowserAccountStorage.MetadataFileName)) == "metadata-old", "metadata retained until new acknowledgement");
            Check(File.ReadAllText(Path.Combine(root, BrowserAccountStorage.SettingsFileName)) == settingsJson, "account settings replaced in file transaction");
            Check(!File.Exists(Path.Combine(root, BrowserAccountStorage.SettingsFileName + ".bak")), "previous account settings backup removed from active slot");
            Check(BrowserAccountStorage.Read(root, RunName, false, Valid, Valid, accountSettings).settings == accountSettings, "saved preferences included when exporting account slot");
            var invalidBundle = Bundle(); invalidBundle.settings = new BrowserGameSettings { effectsVolume = float.NaN };
            Throws(() => BrowserAccountStorage.Apply(root, RunName, invalidBundle, Valid, Valid), "invalid imported settings rejected before any file mutation");
            Check(File.ReadAllText(Path.Combine(root, BrowserAccountStorage.SettingsFileName)) == settingsJson, "invalid import retains saved settings");
            BrowserAccountStorage.Apply(root, RunName, Bundle(), Valid, Valid);
            Check(!File.Exists(Path.Combine(root, RunName)) && !File.Exists(Path.Combine(root, b)), "inner run null clears active slot and pending");
            Check(!File.Exists(Path.Combine(root, BrowserAccountStorage.SettingsFileName)), "null settings clears active settings slot at storage layer");
            Check(File.ReadAllText(Path.Combine(root, "cloud-recovery", BrowserAccountStorage.SettingsFileName)) == settingsJson, "complete settings retained in recovery generation");
            Check(BrowserAccountStorage.Read(root, RunName, false, Valid, Valid).settings == null, "new/legacy settings absent stays null instead of eagerly marking dirty");
            Check(File.ReadAllText(Path.Combine(root, "cloud-recovery", RunName)) == replacement.run, "one complete previous generation retained");
            Check(!Directory.GetDirectories(root).Any(path => Path.GetFileName(path).StartsWith("cloud-recovery.tmp-", StringComparison.Ordinal)), "temporary recovery directories cleaned");
            Check(BrowserAccountStorage.Read(root, RunName, false, Valid, Valid).run == null, "empty slot exports JSON null");
            Throws(() => BrowserAccountStorage.Serialize(Bundle(new string('x', BrowserAccountStorage.MaxBundleBytes))), "maximum byte size enforced");
            Console.WriteLine("Browser account storage: " + checks + " assertions passed.");
            return 0;
        }
        finally { Directory.Delete(root, true); }
    }
}
