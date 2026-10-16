using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Lumia
{
    [Serializable] public sealed class BrowserPendingSave
    {
        public string name, json;
    }

    [Serializable] public sealed class BrowserGameSettings
    {
        public float volume = .4f, musicVolume = .65f, effectsVolume = .8f;
        public bool sound = true, reduceMotion, fullScreen;
    }

    [Serializable] public sealed class BrowserSaveBundle
    {
        public int schemaVersion = 1;
        public string gameVersion = "1.0.6", run, research;
        public BrowserPendingSave[] pending = new BrowserPendingSave[0];
        public bool tutorialSeen;
        // Omitted until the account explicitly saves settings. Older schema 1
        // bundles then retain their original byte-for-byte serialization/hash.
        public BrowserGameSettings settings;
    }

    // This class deliberately has no Unity dependency, so file replacement and
    // null-preserving bundle serialization can be exercised with real files.
    public static class BrowserAccountStorage
    {
        public const string ResearchFileName = "bts-research-v1.json";
        public const string MetadataFileName = "browser-cloud-state.json";
        public const string SettingsFileName = "browser-settings-v1.json";
        public const int MaxBundleBytes = 4 * 1024 * 1024;
        const string RecoveryDirectory = "cloud-recovery";

        public static string Hash(string value)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
        }

        public static bool IsHash(string value) => value != null && value.Length == 64
            && value.All(character => character >= '0' && character <= '9' || character >= 'a' && character <= 'f');

        public static string Serialize(BrowserSaveBundle bundle)
        {
            if (bundle == null || bundle.schemaVersion != 1 || bundle.pending == null)
                throw new InvalidDataException("저장 묶음의 형식이 올바르지 않습니다.");
            var text = new StringBuilder("{\"schemaVersion\":1,\"gameVersion\":");
            AppendString(text, bundle.gameVersion);
            text.Append(",\"run\":"); AppendString(text, bundle.run);
            text.Append(",\"research\":"); AppendString(text, bundle.research);
            text.Append(",\"pending\":[");
            bool first = true;
            foreach (var pending in bundle.pending.OrderBy(value => value.name, StringComparer.Ordinal))
            {
                if (!first) text.Append(',');
                first = false;
                text.Append("{\"name\":"); AppendString(text, pending.name);
                text.Append(",\"json\":"); AppendString(text, pending.json); text.Append('}');
            }
            text.Append("],\"tutorialSeen\":").Append(bundle.tutorialSeen ? "true" : "false");
            if (bundle.settings != null) text.Append(",\"settings\":").Append(SerializeSettings(bundle.settings));
            text.Append('}');
            string json = text.ToString();
            if (Encoding.UTF8.GetByteCount(json) > MaxBundleBytes)
                throw new InvalidDataException("저장 기록이 허용된 크기를 초과했습니다.");
            return json;
        }

        public static void ValidateSettings(BrowserGameSettings settings)
        {
            if (settings == null) return;
            foreach (float value in new[] { settings.volume, settings.musicVolume, settings.effectsVolume })
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0 || value > 1)
                    throw new InvalidDataException("환경 설정의 음량 값이 올바르지 않습니다.");
        }

        public static string SerializeSettings(BrowserGameSettings settings)
        {
            if (settings == null) throw new InvalidDataException("환경 설정이 없습니다.");
            ValidateSettings(settings);
            return "{\"volume\":" + settings.volume.ToString("R", CultureInfo.InvariantCulture)
                + ",\"musicVolume\":" + settings.musicVolume.ToString("R", CultureInfo.InvariantCulture)
                + ",\"effectsVolume\":" + settings.effectsVolume.ToString("R", CultureInfo.InvariantCulture)
                + ",\"sound\":" + (settings.sound ? "true" : "false")
                + ",\"reduceMotion\":" + (settings.reduceMotion ? "true" : "false")
                + ",\"fullScreen\":" + (settings.fullScreen ? "true}" : "false}");
        }

        static void AppendString(StringBuilder output, string value)
        {
            if (value == null) { output.Append("null"); return; }
            output.Append('"');
            foreach (char character in value)
            {
                switch (character)
                {
                    case '"': output.Append("\\\""); break;
                    case '\\': output.Append("\\\\"); break;
                    case '\b': output.Append("\\b"); break;
                    case '\f': output.Append("\\f"); break;
                    case '\n': output.Append("\\n"); break;
                    case '\r': output.Append("\\r"); break;
                    case '\t': output.Append("\\t"); break;
                    default:
                        if (character < 32) output.Append("\\u").Append(((int)character).ToString("x4"));
                        else output.Append(character);
                        break;
                }
            }
            output.Append('"');
        }

        public static string ReadSlot(string path, Action<string> validate)
        {
            Exception failure = null;
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    string json = File.ReadAllText(candidate);
                    validate(json);
                    return json;
                }
                catch (Exception error) { failure = error; }
            }
            if (failure != null) throw new InvalidDataException("브라우저의 저장 기록을 읽을 수 없습니다. 원본은 보존됩니다.", failure);
            return null;
        }

        public static BrowserSaveBundle Read(string directory, string runName, bool tutorialSeen,
            Action<string> validateRun, Action<string> validateResearch, BrowserGameSettings settings = null)
        {
            var bundle = new BrowserSaveBundle
            {
                run = ReadSlot(Path.Combine(directory, runName), validateRun),
                research = ReadSlot(Path.Combine(directory, ResearchFileName), validateResearch),
                tutorialSeen = tutorialSeen,
                settings = settings,
                pending = PendingFiles(directory, runName).OrderBy(Path.GetFileName, StringComparer.Ordinal)
                    .Select(path => new BrowserPendingSave { name = Path.GetFileName(path), json = File.ReadAllText(path) }).ToArray()
            };
            Validate(bundle, runName, validateRun, validateResearch);
            return bundle;
        }

        public static void Validate(BrowserSaveBundle bundle, string runName,
            Action<string> validateRun, Action<string> validateResearch)
        {
            if (bundle == null || bundle.schemaVersion != 1 || bundle.pending == null || bundle.pending.Length > 256
                || string.IsNullOrEmpty(bundle.gameVersion))
                throw new InvalidDataException("지원하지 않는 저장 묶음입니다.");
            if (bundle.run != null) validateRun(bundle.run);
            if (bundle.research != null) validateResearch(bundle.research);
            ValidateSettings(bundle.settings);
            var names = new HashSet<string>(StringComparer.Ordinal);
            string prefix = runName + ".research-pending-";
            foreach (var pending in bundle.pending)
            {
                if (pending == null || pending.name == null || pending.json == null
                    || !pending.name.StartsWith(prefix, StringComparison.Ordinal)
                    || !pending.name.EndsWith(".json", StringComparison.Ordinal) || !names.Add(pending.name))
                    throw new InvalidDataException("미정산 연구 기록의 이름이 올바르지 않습니다.");
                string id = pending.name.Substring(prefix.Length, pending.name.Length - prefix.Length - 5);
                if (id.Length < 1 || id.Length > 96 || id.Any(character => !(character >= 'a' && character <= 'z'
                    || character >= 'A' && character <= 'Z' || character >= '0' && character <= '9' || character == '-' || character == '_')))
                    throw new InvalidDataException("미정산 연구 기록의 경로가 올바르지 않습니다.");
                validateRun(pending.json);
            }
            Serialize(bundle);
        }

        // Validate first, retain the entire previous slot including backups and
        // pending ledgers, then replace the set. A failed write restores that set.
        public static void Apply(string directory, string runName, BrowserSaveBundle bundle,
            Action<string> validateRun, Action<string> validateResearch, Action<string, string> write = null, Action afterReplace = null)
        {
            Validate(bundle, runName, validateRun, validateResearch);
            Directory.CreateDirectory(directory);
            var original = KnownFiles(directory, runName).ToDictionary(Path.GetFileName, File.ReadAllBytes, StringComparer.Ordinal);
            string temporaryRecovery = Path.Combine(directory, RecoveryDirectory + ".tmp-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryRecovery);
            try
            {
                foreach (var file in original) File.WriteAllBytes(Path.Combine(temporaryRecovery, file.Key), file.Value);
                string recovery = Path.Combine(directory, RecoveryDirectory);
                if (Directory.Exists(recovery)) Directory.Delete(recovery, true);
                Directory.Move(temporaryRecovery, recovery);
            }
            finally { if (Directory.Exists(temporaryRecovery)) Directory.Delete(temporaryRecovery, true); }

            try
            {
                foreach (string path in KnownFiles(directory, runName))
                    if (Path.GetFileName(path) != MetadataFileName) File.Delete(path);
                Action<string, string> writeFile = write ?? ((path, json) => File.WriteAllText(path, json, new UTF8Encoding(false)));
                if (bundle.run != null) writeFile(Path.Combine(directory, runName), bundle.run);
                if (bundle.research != null) writeFile(Path.Combine(directory, ResearchFileName), bundle.research);
                if (bundle.settings != null) writeFile(Path.Combine(directory, SettingsFileName), SerializeSettings(bundle.settings));
                foreach (var pending in bundle.pending) writeFile(Path.Combine(directory, pending.name), pending.json);
                afterReplace?.Invoke();
            }
            catch
            {
                foreach (string path in KnownFiles(directory, runName)) File.Delete(path);
                foreach (var file in original) File.WriteAllBytes(Path.Combine(directory, file.Key), file.Value);
                throw;
            }
        }

        static string[] PendingFiles(string directory, string runName) => Directory.Exists(directory)
            ? Directory.GetFiles(directory, runName + ".research-pending-*.json", SearchOption.TopDirectoryOnly) : new string[0];

        static IEnumerable<string> KnownFiles(string directory, string runName)
        {
            if (!Directory.Exists(directory)) return new string[0];
            return new[] { runName, runName + ".bak", ResearchFileName, ResearchFileName + ".bak", SettingsFileName, SettingsFileName + ".bak", MetadataFileName }
                .Select(name => Path.Combine(directory, name)).Where(File.Exists).Concat(PendingFiles(directory, runName)).ToArray();
        }
    }
}
