using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Lumia
{
    /// <summary>Development-only migration fixture; never creates real save or registry values.</summary>
    public sealed class GameIdentityVerification : MonoBehaviour
    {
        [Serializable] sealed class Result
        {
            public bool passed;
            public int checks;
            public string fixtureDirectory;
            public List<string> failures = new List<string>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-identity-verify") >= 0)
                new GameObject("Identity verification").AddComponent<GameIdentityVerification>();
        }

        void Start()
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "IdentityVerification"));
            var result = new Result { fixtureDirectory = Path.Combine(output, "fixtures-" + Guid.NewGuid().ToString("N")) };
            try
            {
                Directory.CreateDirectory(result.fixtureDirectory);
                Action<bool, string> check = (condition, name) => { result.checks++; if (!condition) result.failures.Add(name); };
                RunFileChecks(result.fixtureDirectory, check);
                RunSettingsChecks(check);
            }
            catch (Exception e) { result.failures.Add(e.ToString()); }
            result.passed = result.failures.Count == 0;
            try
            {
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, "results.json"), JsonUtility.ToJson(result, true));
            }
            catch (Exception e) { result.passed = false; Debug.LogError("Identity verification report: " + e); }
            Debug.Log("Identity verification " + (result.passed ? "PASS" : "FAIL") + ": " + result.checks + " checks; " + output);
            int exitCode = result.passed ? 0 : 1;
#if UNITY_EDITOR
            if (Application.isBatchMode) UnityEditor.EditorApplication.Exit(exitCode);
#else
            Application.Quit(exitCode);
#endif
        }

        static void RunFileChecks(string root, Action<bool, string> check)
        {
            string json = JsonUtility.ToJson(new GameEngine(91357).State, true);
            string backup = JsonUtility.ToJson(new GameEngine(91358).State, true);
            string current = CurrentPath(root, "valid");
            string legacy = WriteLegacy(root, "valid", json);
            File.WriteAllText(legacy + ".bak", backup);
            check(GameIdentity.MigrateLegacySave(current), "valid save imported");
            check(File.ReadAllText(current) == json, "import preserves exact save contents");
            check(File.ReadAllText(current + ".bak") == backup, "legacy backup copied");
            check(File.ReadAllText(legacy) == json && File.ReadAllText(legacy + ".bak") == backup, "legacy files remain unchanged");
            File.WriteAllText(current, "existing new save");
            check(!GameIdentity.MigrateLegacySave(current) && File.ReadAllText(current) == "existing new save", "existing new save never overwritten");

            current = CurrentPath(root, "existing-backup");
            legacy = WriteLegacy(root, "existing-backup", json);
            File.WriteAllText(legacy + ".bak", backup);
            Directory.CreateDirectory(Path.GetDirectoryName(current));
            File.WriteAllText(current + ".bak", "existing new backup");
            check(GameIdentity.MigrateLegacySave(current) && File.ReadAllText(current + ".bak") == "existing new backup", "existing new backup never overwritten");

            current = CurrentPath(root, "malformed");
            legacy = WriteLegacy(root, "malformed", "not a JSON save");
            check(!GameIdentity.MigrateLegacySave(current) && !File.Exists(current), "malformed save rejected");
            check(File.ReadAllText(legacy) == "not a JSON save", "malformed original preserved");
            check(Directory.GetFiles(Path.GetDirectoryName(current), "*.tmp").Length == 0, "failed import leaves no temporary file");

            var unsupported = new GameEngine(91359).State;
            unsupported.version = 999;
            current = CurrentPath(root, "unsupported-version");
            WriteLegacy(root, "unsupported-version", JsonUtility.ToJson(unsupported));
            check(!GameIdentity.MigrateLegacySave(current) && !File.Exists(current), "unsupported save version rejected");
            unsupported.version = 2; unsupported.level = 21;
            current = CurrentPath(root, "invalid-level");
            WriteLegacy(root, "invalid-level", JsonUtility.ToJson(unsupported));
            check(!GameIdentity.MigrateLegacySave(current) && !File.Exists(current), "out-of-range level rejected");

            current = CurrentPath(root, "verification-skip");
            legacy = WriteLegacy(root, "verification-skip", json);
            GameIdentity.MigrateLegacyData(current, true);
            check(!File.Exists(current) && File.ReadAllText(legacy) == json, "verification mode skips migration");
        }

        static string CurrentPath(string root, string name)
        {
            return Path.Combine(root, name, GameIdentity.ProductName, GameIdentity.SaveFileName);
        }

        static string WriteLegacy(string root, string name, string json)
        {
            string path = Path.Combine(root, name, GameIdentity.LegacyProductName, GameIdentity.SaveFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);
            return path;
        }

        static void RunSettingsChecks(Action<bool, string> check)
        {
            float value;
            int toggle;
            check(GameIdentity.TryDecodeLegacyFloat(new GameIdentity.LegacySetting(BitConverter.GetBytes(.4f), GameIdentity.LegacyValueKind.Binary), out value) && Math.Abs(value - .4f) < .00001f, "IEEE754 binary float decoded");
            check(GameIdentity.TryDecodeLegacyFloat(new GameIdentity.LegacySetting(BitConverter.GetBytes(.625), GameIdentity.LegacyValueKind.Binary), out value) && value == .625f, "IEEE754 binary double decoded");
            check(GameIdentity.TryDecodeLegacyFloat(new GameIdentity.LegacySetting(BitConverter.ToInt32(BitConverter.GetBytes(.8f), 0), GameIdentity.LegacyValueKind.DWord), out value) && Math.Abs(value - .8f) < .00001f, "IEEE754 DWORD decoded");
            check(!GameIdentity.TryDecodeLegacyFloat(new GameIdentity.LegacySetting(BitConverter.GetBytes(float.NaN), GameIdentity.LegacyValueKind.Binary), out value), "NaN volume rejected");
            check(!GameIdentity.TryDecodeLegacyFloat(new GameIdentity.LegacySetting(BitConverter.GetBytes(double.PositiveInfinity), GameIdentity.LegacyValueKind.Binary), out value), "infinite volume rejected");
            check(!GameIdentity.TryDecodeLegacyFloat(new GameIdentity.LegacySetting(BitConverter.GetBytes(1.00000001), GameIdentity.LegacyValueKind.Binary), out value), "double outside volume range rejected before narrowing");
            check(!GameIdentity.TryDecodeLegacyFloat(new GameIdentity.LegacySetting(BitConverter.GetBytes(-.1f), GameIdentity.LegacyValueKind.Binary), out value), "negative volume rejected");
            check(!GameIdentity.TryDecodeLegacyFloat(new GameIdentity.LegacySetting(new byte[3], GameIdentity.LegacyValueKind.Binary), out value), "wrong binary length rejected");
            check(GameIdentity.TryDecodeLegacyToggle(new GameIdentity.LegacySetting(1, GameIdentity.LegacyValueKind.DWord), out toggle) && toggle == 1, "DWORD toggle decoded");
            check(!GameIdentity.TryDecodeLegacyToggle(new GameIdentity.LegacySetting(2, GameIdentity.LegacyValueKind.DWord), out toggle), "invalid toggle rejected");

            var old = new Dictionary<string, GameIdentity.LegacySetting>
            {
                { "lumia.volume", new GameIdentity.LegacySetting(BitConverter.GetBytes(.4f), GameIdentity.LegacyValueKind.Binary) },
                { "lumia.sound", new GameIdentity.LegacySetting(0, GameIdentity.LegacyValueKind.DWord) },
                { "lumia.reduceMotion", new GameIdentity.LegacySetting(1, GameIdentity.LegacyValueKind.DWord) },
                { "unrelated.setting", new GameIdentity.LegacySetting(999, GameIdentity.LegacyValueKind.DWord) }
            };
            var preferences = new Dictionary<string, object> { { "lumia.volume", .9f } };
            var readKeys = new List<string>();
            Func<string, GameIdentity.LegacySetting> read = key => { readKeys.Add(key); return old[key]; };
            check(GameIdentity.ImportLegacySettings(preferences.ContainsKey, read, (key, v) => preferences[key] = v, (key, v) => preferences[key] = v), "missing known settings imported");
            check((float)preferences["lumia.volume"] == .9f && !readKeys.Contains("lumia.volume"), "existing new setting preserved and not read from legacy");
            check((int)preferences["lumia.sound"] == 0 && (int)preferences["lumia.reduceMotion"] == 1, "both missing toggles copied");
            check(readKeys.Count == 2 && !readKeys.Contains("unrelated.setting") && !preferences.ContainsKey("unrelated.setting"), "only known settings read or imported");
            readKeys.Clear();
            check(!GameIdentity.ImportLegacySettings(preferences.ContainsKey, read, (key, v) => preferences[key] = v, (key, v) => preferences[key] = v) && readKeys.Count == 0, "repeat migration is idempotent");
        }
    }
}
