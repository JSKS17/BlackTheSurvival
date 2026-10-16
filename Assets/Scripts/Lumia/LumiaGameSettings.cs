using System;
using System.IO;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        bool fullScreenPreference;

        bool BrowserSettingsActive
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return !audioVerification && browserAccountId.Length != 0;
#else
                return false;
#endif
            }
        }

        BrowserGameSettings CurrentGameSettings() => new BrowserGameSettings
        {
            volume = volume, musicVolume = musicVolume, effectsVolume = effectsVolume,
            sound = sound, reduceMotion = reduceMotion, fullScreen = fullScreenPreference
        };

        void ApplyGameSettings(BrowserGameSettings value)
        {
            var restored = value ?? new BrowserGameSettings();
            BrowserAccountStorage.ValidateSettings(restored);
            volume = restored.volume; musicVolume = restored.musicVolume; effectsVolume = restored.effectsVolume;
            sound = restored.sound; reduceMotion = restored.reduceMotion; fullScreenPreference = restored.fullScreen;
            // Fullscreen requires a browser user gesture. Restore the preference,
            // and let the explicit screen-mode button apply it when clicked.
            if (Audio) Audio.Configure(!audioVerification && sound, volume, musicVolume, effectsVolume);
        }

        void LoadGameSettings()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (BrowserSettingsActive)
            {
                ApplyGameSettings(ReadBrowserSettings());
                return;
            }
#endif
            string prefix = audioVerification ? "bts.audio.verify." : "";
            volume = Mathf.Clamp01(PlayerPrefs.GetFloat(prefix + "lumia.volume", .4f));
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(prefix + "lumia.musicVolume", .65f));
            effectsVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(prefix + "lumia.effectsVolume", .8f));
            sound = PlayerPrefs.GetInt(prefix + "lumia.sound", 1) != 0;
            reduceMotion = PlayerPrefs.GetInt("lumia.reduceMotion", 0) != 0;
            fullScreenPreference = Screen.fullScreen;
        }

        void SaveGameSettings()
        {
            try
            {
                SaveAudioSettings();
                if (!BrowserSettingsActive)
                {
                    if (!audioVerification) PlayerPrefs.SetInt("lumia.reduceMotion", reduceMotion ? 1 : 0);
                    PlayerPrefs.Save();
                }
                BrowserPersistence.RequestSync();
                settings = false;
            }
            catch (Exception error)
            {
                Notify("환경 설정을 저장하지 못했습니다. 다시 시도해 주세요. " + error.Message);
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        static BrowserGameSettings ParseBrowserSettings(string json)
        {
            var parsed = JsonUtility.FromJson<BrowserGameSettings>(json);
            if (parsed == null || BrowserAccountStorage.SerializeSettings(parsed) != json)
                throw new InvalidDataException("브라우저의 환경 설정 기록이 올바르지 않습니다.");
            return parsed;
        }

        BrowserGameSettings ReadBrowserSettings()
        {
            string path = Path.Combine(Path.GetDirectoryName(savePath), BrowserAccountStorage.SettingsFileName);
            string json = BrowserAccountStorage.ReadSlot(path, value => ParseBrowserSettings(value));
            return json == null ? null : ParseBrowserSettings(json);
        }

        static bool ValidBrowserSettingsPrimary(string path)
        {
            try { ParseBrowserSettings(File.ReadAllText(path)); return true; }
            catch { return false; }
        }

        void WriteBrowserSettings()
        {
            string path = Path.Combine(Path.GetDirectoryName(savePath), BrowserAccountStorage.SettingsFileName);
            // Do not overwrite a good recovery backup with a damaged primary.
            WriteAtomicRunFile(path, BrowserAccountStorage.SerializeSettings(CurrentGameSettings()), ValidBrowserSettingsPrimary(path));
        }
#endif
    }
}
