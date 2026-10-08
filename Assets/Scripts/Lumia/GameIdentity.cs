using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Lumia
{
    /// <summary>Product identity and a non-destructive bridge from the previous release.</summary>
    public static class GameIdentity
    {
        public const string ProductName = "Black The Survival";
        public const string ExecutableName = "BlackTheSurvival.exe";
        public const string SaveFileName = "lumia-vf-loop-v1.json";
        public const string CompanyName = "Lumia Fan Project";
        internal const string LegacyProductName = "Lumia VF Loop";
        static readonly string[] SettingKeys = { "lumia.volume", "lumia.sound", "lumia.reduceMotion" };

        /// <summary>Call before reading PlayerPrefs. Verification never imports personal data.</summary>
        public static void MigrateLegacyData(string currentSavePath, bool verificationMode)
        {
            string[] args = Environment.GetCommandLineArgs();
            if (verificationMode || Array.IndexOf(args, "-lumia-verify") >= 0 ||
                Array.IndexOf(args, "-lumia-art-verify") >= 0 || Array.IndexOf(args, "-bts-identity-verify") >= 0) return;
            MigrateLegacySave(currentSavePath);
            MigrateLegacySettings();
        }

        // Separate from PlayerPrefs so verification can use disposable sibling folders.
        internal static bool MigrateLegacySave(string currentSavePath)
        {
            string temporaryPath = null;
            try
            {
                if (string.IsNullOrEmpty(currentSavePath) || File.Exists(currentSavePath)) return false;
                string destination = Path.GetFullPath(currentSavePath);
                var directory = new DirectoryInfo(Path.GetDirectoryName(destination));
                if (directory.Parent == null || directory.Name != ProductName || Path.GetFileName(destination) != SaveFileName) return false;
                string legacy = Path.Combine(directory.Parent.FullName, LegacyProductName, SaveFileName);
                if (!File.Exists(legacy)) return false;

                Directory.CreateDirectory(directory.FullName);
                temporaryPath = destination + ".legacy-import-" + Guid.NewGuid().ToString("N") + ".tmp";
                File.Copy(legacy, temporaryPath, false);
                ValidateSave(temporaryPath);
                // The temporary file is on the same volume. Move fails rather than overwriting
                // a newer save that another process might have created during validation.
                File.Move(temporaryPath, destination);
                temporaryPath = null;
                if (File.Exists(legacy + ".bak") && !File.Exists(destination + ".bak"))
                {
                    try { File.Copy(legacy + ".bak", destination + ".bak", false); }
                    catch (Exception e) { Warn("이전 저장 기록의 백업을 가져오지 못했습니다", e); }
                }
                return true;
            }
            catch (Exception e) { Warn("이전 저장 기록을 가져오지 못했습니다", e); return false; }
            finally
            {
                if (temporaryPath != null)
                {
                    try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
                    catch (Exception e) { Warn("저장 기록 가져오기의 임시 파일을 정리하지 못했습니다", e); }
                }
            }
        }

        static void ValidateSave(string path)
        {
            var state = JsonUtility.FromJson<RunState>(File.ReadAllText(path));
            if (state == null || state.version < 1 || state.version > 2 || state.level < 1 || state.level > 20 ||
                state.deck == null || state.map == null || !Enum.IsDefined(typeof(RunStage), state.stage))
                throw new InvalidDataException("저장 기록 형식이 올바르지 않습니다.");
            foreach (var node in state.map)
                if (node == null) throw new InvalidDataException("저장 지도의 구역이 손상되었습니다.");
        }

        static void MigrateLegacySettings()
        {
            if (Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.WindowsEditor) return;
            string prefix = Application.platform == RuntimePlatform.WindowsEditor ? @"Software\Unity\UnityEditor\" : @"Software\";
            try
            {
                using (var legacy = NativeLegacyRegistry.Open(prefix + CompanyName + "\\" + LegacyProductName))
                {
                    if (legacy == null) return;
                    if (ImportLegacySettings(PlayerPrefs.HasKey, key => ReadLegacySetting(legacy, key), PlayerPrefs.SetFloat, PlayerPrefs.SetInt))
                        PlayerPrefs.Save();
                }
            }
            catch (Exception e) { Warn("이전 환경 설정을 가져오지 못했습니다", e); }
        }

        internal enum LegacyValueKind { Unknown = 0, Binary = 3, DWord = 4 }

        internal struct LegacySetting
        {
            internal object Value;
            internal LegacyValueKind Kind;
            internal LegacySetting(object value, LegacyValueKind kind) { Value = value; Kind = kind; }
        }

        // The callbacks keep this three-setting policy testable without touching the registry.
        internal static bool ImportLegacySettings(Func<string, bool> hasKey, Func<string, LegacySetting> read,
            Action<string, float> setFloat, Action<string, int> setInt)
        {
            bool imported = false;
            foreach (string key in SettingKeys)
            {
                if (hasKey(key)) continue;
                LegacySetting old = read(key);
                if (old.Value == null) continue;
                float volume;
                int toggle;
                if (key == "lumia.volume" && TryDecodeLegacyFloat(old, out volume))
                { setFloat(key, volume); imported = true; }
                else if (key != "lumia.volume" && TryDecodeLegacyToggle(old, out toggle))
                { setInt(key, toggle); imported = true; }
                else Debug.LogWarning("이전 환경 설정 '" + key + "'의 값이 올바르지 않아 가져오지 않았습니다.");
            }
            return imported;
        }

        static LegacySetting ReadLegacySetting(NativeLegacyRegistry legacy, string key)
        {
            // Support Unity's suffixed decimal hash and older unsuffixed names, but read
            // only these three allowed values. Never write to the old registry namespace.
            LegacySetting setting;
            if (legacy.TryRead(key, out setting)) return setting;
            foreach (string name in legacy.ValueNames())
            {
                if (IsLegacySettingName(name, key) && legacy.TryRead(name, out setting)) return setting;
            }
            return default(LegacySetting);
        }

        static bool IsLegacySettingName(string name, string key)
        {
            if (name == key) return true;
            string prefix = key + "_h";
            if (name == null || !name.StartsWith(prefix, StringComparison.Ordinal) || name.Length == prefix.Length) return false;
            for (int i = prefix.Length; i < name.Length; i++)
                if (name[i] < '0' || name[i] > '9') return false;
            return true;
        }

        internal static bool IsAllowedLegacyValueName(string name)
        {
            foreach (string key in SettingKeys)
                if (IsLegacySettingName(name, key)) return true;
            return false;
        }

        internal static bool TryDecodeLegacyFloat(LegacySetting setting, out float value)
        {
            value = 0;
            byte[] binary = setting.Value as byte[];
            if (setting.Kind == LegacyValueKind.DWord && setting.Value is int)
                value = BitConverter.ToSingle(BitConverter.GetBytes((int)setting.Value), 0);
            else if (setting.Kind == LegacyValueKind.Binary && binary != null && binary.Length == 4)
                value = BitConverter.ToSingle(binary, 0);
            else if (setting.Kind == LegacyValueKind.Binary && binary != null && binary.Length == 8)
            {
                double precise = BitConverter.ToDouble(binary, 0);
                if (double.IsNaN(precise) || double.IsInfinity(precise) || precise < 0 || precise > 1) return false;
                value = (float)precise;
            }
            else return false;
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 1;
        }

        internal static bool TryDecodeLegacyToggle(LegacySetting setting, out int value)
        {
            value = 0;
            if (setting.Kind != LegacyValueKind.DWord || !(setting.Value is int)) return false;
            value = (int)setting.Value;
            return value == 0 || value == 1;
        }

        // Unity's .NET Standard profile omits Microsoft.Win32.Registry. These four
        // Windows APIs only open/query/enumerate/close; no write API is imported.
        sealed class NativeLegacyRegistry : IDisposable
        {
            IntPtr handle;
            NativeLegacyRegistry(IntPtr handle) { this.handle = handle; }

            public static NativeLegacyRegistry Open(string path)
            {
                IntPtr result;
                int status = RegOpenKeyExW(new IntPtr(unchecked((int)0x80000001)), path, 0, 0x0001, out result); // HKCU, KEY_QUERY_VALUE
                if (status == 2 || status == 3) return null;
                if (status != 0) throw Error("RegOpenKeyExW", status);
                return new NativeLegacyRegistry(result);
            }

            public IEnumerable<string> ValueNames()
            {
                var name = new StringBuilder(16384); // Registry's maximum Unicode value-name length plus terminator.
                for (uint index = 0; ; index++)
                {
                    name.Clear();
                    uint length = (uint)name.Capacity;
                    int status = RegEnumValueW(handle, index, name, ref length, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                    if (status == 259) yield break; // ERROR_NO_MORE_ITEMS
                    if (status != 0) throw Error("RegEnumValueW", status);
                    yield return name.ToString();
                }
            }

            public bool TryRead(string name, out LegacySetting setting)
            {
                if (!IsAllowedLegacyValueName(name)) throw new ArgumentException("허용되지 않은 이전 설정 키입니다.", "name");
                setting = default(LegacySetting);
                uint kind, size = 0;
                int status = RegQueryValueExW(handle, name, IntPtr.Zero, out kind, null, ref size);
                if (status == 2) return false;
                if (status != 0) throw Error("RegQueryValueExW", status);
                // Only a four/eight-byte float or a four-byte integer is accepted.
                if ((kind != 3 && kind != 4) || (size != 4 && size != 8))
                { setting = new LegacySetting(new byte[0], (LegacyValueKind)kind); return true; }
                byte[] data = new byte[size];
                status = RegQueryValueExW(handle, name, IntPtr.Zero, out kind, data, ref size);
                if (status == 2) return false;
                if (status != 0) throw Error("RegQueryValueExW", status);
                if (size != data.Length) { setting = new LegacySetting(new byte[0], (LegacyValueKind)kind); return true; }
                object value = kind == 4 && size == 4 ? (object)BitConverter.ToInt32(data, 0) : data;
                setting = new LegacySetting(value, (LegacyValueKind)kind);
                return true;
            }

            public void Dispose()
            {
                if (handle == IntPtr.Zero) return;
                RegCloseKey(handle);
                handle = IntPtr.Zero;
            }

            static IOException Error(string operation, int code) { return new IOException(operation + " failed (" + code + ")."); }

            [DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            static extern int RegOpenKeyExW(IntPtr root, string subKey, uint options, uint access, out IntPtr result);
            [DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            static extern int RegQueryValueExW(IntPtr key, string name, IntPtr reserved, out uint kind, [Out] byte[] data, ref uint size);
            [DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            static extern int RegEnumValueW(IntPtr key, uint index, StringBuilder name, ref uint length, IntPtr reserved, IntPtr kind, IntPtr data, IntPtr dataSize);
            [DllImport("advapi32.dll", ExactSpelling = true)]
            static extern int RegCloseKey(IntPtr key);
        }

        static void Warn(string context, Exception exception)
        {
            Debug.LogWarning(context + ": " + exception.Message);
        }
    }
}
