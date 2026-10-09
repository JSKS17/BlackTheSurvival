using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;

namespace Lumia
{
    /// <summary>Opt-in native mixer checks. Never changes personal settings or the normal run.</summary>
    public sealed class GameAudioVerification : MonoBehaviour
    {
        [Serializable] public sealed class ClipProof
        {
            public string id, loadType;
            public int sampleRate, channels, sampleFrames;
            public float duration;
            public bool decodedPcm;
            public double sampledRms, sampledPeak;
        }
        [Serializable] public sealed class PlaybackProof
        {
            public string name;
            public double mixerRms, mixerPeak;
            public GameAudioSnapshot snapshot;
        }
        [Serializable] public sealed class Report
        {
            public string status = "RUNNING", measurement = "Unity AudioListener.GetOutputData: the actual game mixer, before the operating-system output device";
            public int assertions, cardRoutes;
            public bool normalSaveUnchanged, normalPreferencesUnchanged, isolatedSettingsRoundTrip;
            public List<string> errors = new List<string>();
            public List<ClipProof> clips = new List<ClipProof>();
            public List<PlaybackProof> playback = new List<PlaybackProof>();
        }

        static readonly string[] PreferenceKeys = { "lumia.volume", "lumia.sound", "lumia.reduceMotion", "lumia.musicVolume", "lumia.effectsVolume", "lumia.music", "lumia.effects", "lumia.sfxVolume" };
        static readonly string[] ActionCues = { "ui.click", "ui.select", "ui.cancel", "ui.reroll", "ui.error", "game.start", "game.resume", "game.win", "game.lose", "map.enter", "combat.start", "combat.win", "boss.start", "level.up", "kiosk.enter", "kiosk.purchase", "camp.enter", "camp.rest", "camp.craft", "camp.cook", "item.heal", "encounter.enter", "encounter.choose", "reward.select", "reward.confirm", "turn.end" };
        Report report = new Report();
        GameAudio playerAudio;
        float[] mixer = new float[1024];
        double measuredRms, measuredPeak;
        string output;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-audio-verify") >= 0)
                new GameObject("Black The Survival audio verification").AddComponent<GameAudioVerification>();
        }

        IEnumerator Start()
        {
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Verification", "Audio"));
            Directory.CreateDirectory(output);
            string save = Path.Combine(Application.persistentDataPath, GameIdentity.SaveFileName);
            string initialSave = FileDigest(save);
            string[] initialPreferences = PreferenceKeys.Select(PreferenceValue).ToArray();
            yield return null;
            playerAudio = GameAudio.Attach(gameObject);
            playerAudio.Configure(true, .7f, .65f, .8f);
            VerifySettingsPersistence();
            Check(FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l => l.enabled) == 1, "Exactly one enabled audio listener is required.");
            Check(playerAudio.Ready, "The native player loaded the audio catalog.");
            foreach (string error in playerAudio.ValidateCatalog(false)) Check(false, error);
            foreach (var definition in playerAudio.Catalog.Clips.OrderBy(c => c.id, StringComparer.Ordinal))
            {
                var clip = playerAudio.LoadClip(definition.id);
                Check(clip != null, definition.id + ": imported AudioClip exists.");
                if (clip == null) continue;
                Check(clip.samples > 0 && clip.frequency >= 8000 && clip.length > .015f, definition.id + ": decoded duration and format are usable.");
                var proof = new ClipProof { id = definition.id, loadType = clip.loadType.ToString(), sampleRate = clip.frequency, channels = clip.channels, sampleFrames = clip.samples, duration = clip.length };
                if (clip.loadType != AudioClipLoadType.Streaming)
                {
                    clip.LoadAudioData();
                    float deadline = Time.realtimeSinceStartup + 8;
                    while (clip.loadState == AudioDataLoadState.Loading && Time.realtimeSinceStartup < deadline) yield return null;
                    Check(clip.loadState == AudioDataLoadState.Loaded, definition.id + ": native audio decoding completed.");
                    if (clip.loadState == AudioDataLoadState.Loaded)
                    {
                        var samples = new float[Math.Min(8192, clip.samples * clip.channels)];
                        double maximumRms = 0, maximumPeak = 0;
                        bool decoded = false;
                        foreach (float fraction in new[] { .05f, .35f, .7f })
                        {
                            int offset = Mathf.Clamp((int)(clip.samples * fraction), 0, Math.Max(0, clip.samples - samples.Length / clip.channels));
                            if (!clip.GetData(samples, offset)) continue;
                            decoded = true; double sum = 0;
                            foreach (float sample in samples) { sum += sample * sample; maximumPeak = Math.Max(maximumPeak, Math.Abs(sample)); }
                            maximumRms = Math.Max(maximumRms, Math.Sqrt(sum / samples.Length));
                        }
                        proof.decodedPcm = decoded; proof.sampledRms = maximumRms; proof.sampledPeak = maximumPeak;
                        Check(decoded && maximumRms > .000001, definition.id + ": decoded PCM contains audible samples.");
                        Check(maximumPeak <= 1.0001 && !double.IsNaN(maximumPeak), definition.id + ": decoded PCM has finite normalized samples.");
                    }
                }
                report.clips.Add(proof);
            }
            Check(playerAudio.DebugSnapshot().missingClipCount == 0, "Every catalog asset loaded in the native player.");
            yield return VerifyUiActions();

            // Every real stage uses the controller's production music selection and crossfade.
            var state = new RunState { stage = RunStage.Preparation };
            string[] themes = { "music.lobby", "music.preparation", "music.map", "music.combat", "music.boss", "music.kiosk", "music.camp", "music.encounter", "music.win", "music.lose" };
            foreach (string theme in themes)
            {
                bool lobby = theme == "music.lobby";
                state.stage = theme == "music.preparation" ? RunStage.Preparation : theme == "music.kiosk" ? RunStage.Kiosk : theme == "music.camp" ? RunStage.Campfire
                    : theme == "music.encounter" ? RunStage.Encounter : theme == "music.win" ? RunStage.Won : theme == "music.lose" ? RunStage.Lost
                    : theme == "music.combat" || theme == "music.boss" ? RunStage.Combat : RunStage.Map;
                state.activeNodeId = 9;
                state.map = new List<MapNode> { new MapNode { id = 9, kind = theme == "music.boss" ? ZoneKind.Boss : ZoneKind.Subject } };
                playerAudio.Tick(lobby, state);
                Check(playerAudio.DebugSnapshot().musicTheme == theme, theme + ": stage selects the intended music theme.");
                Check(playerAudio.DebugSnapshot().activeMusicCount <= 2, theme + ": crossfade keeps at most two music sources.");
                yield return Sample(GameAudio.MusicFadeSeconds + .3f);
                var snapshot = playerAudio.DebugSnapshot();
                Check(snapshot.activeMusicCount == 1, theme + ": one loop remains after the fade.");
                Check(snapshot.activeAmbientCount == (theme == "music.camp" ? 1 : 0), theme + ": location ambience follows the actual stage.");
                Check(measuredRms > .000001, theme + ": the real game mixer outputs music.");
                Record(theme);
                var source = playerAudio.GetComponents<AudioSource>().FirstOrDefault(s => s.loop && s.isPlaying);
                float before = source == null ? 0 : source.time;
                for (int i = 0; i < 5; i++) { playerAudio.Tick(lobby, state); yield return null; }
                Check(source != null && source.isPlaying && source.time >= before, theme + ": repeated Update synchronization does not restart music.");
            }

            // Test individual actions without background music masking a silent effect.
            playerAudio.Configure(true, .7f, 0, .8f);
            yield return VerifyAmbience();
            foreach (string cue in ActionCues)
            {
                int before = playerAudio.DebugSnapshot().playedEventCount;
                Check(playerAudio.PlayCue(cue), cue + ": event cue resolves and plays.");
                var snapshot = playerAudio.DebugSnapshot();
                Check(snapshot.playedEventCount == before + 1 && snapshot.lastCue == cue, cue + ": exactly one successful event starts one sound.");
                Check(snapshot.activeEffectCount + snapshot.activeVoiceCount > 0, cue + ": an actual AudioSource is playing.");
                yield return Sample(.13f);
                if (cue == "game.start" || cue == "kiosk.purchase" || cue == "camp.craft" || cue == "camp.cook" || cue == "item.heal")
                {
                    Check(measuredRms > .000001, cue + ": the real mixer outputs the action effect."); Record(cue);
                }
            }
            int clickEvents = playerAudio.DebugSnapshot().playedEventCount;
            for (int i = 0; i < 8; i++) playerAudio.PlayCue("ui.click");
            Check(playerAudio.DebugSnapshot().playedEventCount == clickEvents + 1, "Repeated same-frame UI callbacks do not emit eight duplicate click sounds.");

            var combat = new CombatState();
            foreach (string animal in new[] { "chicken", "dog", "boar", "wolf", "bear" })
            {
                combat.animal = animal;
                Check(playerAudio.PlayCue("wild." + animal + ".enter"), animal + ": encounter animal cue resolves.");
                yield return Sample(.08f);
                Check(playerAudio.PlayCombatAction(new CombatAction { cardId = "basic_attack", enemy = true, damage = 4 }, combat), animal + ": the actual enemy route uses its animal sound.");
                Check(playerAudio.DebugSnapshot().lastCue == "wild." + animal + ".attack", animal + ": the enemy action selects the correct species.");
                yield return Sample(.22f);
                Check(measuredRms > .000001, animal + ": animal attack reaches the mixer."); Record("animal." + animal);
            }
            combat.animal = null;
            foreach (string cardId in new[] { "nia_q", "cathy_q", "celine_w", "aya_r", "basic_attack", "basic_guard" })
            {
                Check(GameDatabase.Card(cardId) != null, cardId + ": representative card exists.");
                foreach (bool enemy in new[] { false, true })
                {
                    Check(playerAudio.PlayCombatAction(new CombatAction { cardId = cardId, enemy = enemy, damage = 5 }, combat), cardId + ": player/enemy card action plays.");
                    yield return Sample(.15f);
                    Check(measuredRms > .000001, cardId + ": card action reaches the mixer."); Record((enemy ? "enemy." : "player.") + cardId);
                }
            }

            // Mapping coverage and saturation are tested with zero effect volume to keep the sweep quiet.
            playerAudio.Configure(true, .7f, 0, 0);
            int primaries = playerAudio.DebugSnapshot().primaryActionsPlayed;
            foreach (var card in GameDatabase.Cards)
            {
                Check(playerAudio.PlayCombatAction(new CombatAction { cardId = card.id, enemy = false }, combat), card.id + ": production card route is covered.");
                Check(playerAudio.DebugSnapshot().lastCardId == card.id, card.id + ": card identity survives audio routing.");
                report.cardRoutes++;
            }
            var bounded = playerAudio.DebugSnapshot();
            Check(bounded.primaryActionsPlayed == primaries + report.cardRoutes, "Every card produces one handled primary action.");
            Check(bounded.activeEffectCount <= GameAudio.EffectVoiceLimit && bounded.activeVoiceCount <= GameAudio.SpokenVoiceLimit, "Rapid card effects respect both voice pools.");
            Check(bounded.activeEffectCount > 1, "Independent skill effects can overlap instead of cutting all previous sounds.");
            Record("all_cards_pool_limit");
            int events = bounded.playedEventCount, secondary = bounded.suppressedSecondaryActions;
            for (int i = 0; i < 400; i++) playerAudio.PlayCombatAction(new CombatAction { cardId = "nia_q", kind = i % 2 == 0 ? "bleed" : "trait_bonus_damage", traitId = i % 2 == 0 ? null : "cathy" }, combat);
            Check(playerAudio.DebugSnapshot().playedEventCount == events && playerAudio.DebugSnapshot().suppressedSecondaryActions == secondary + 400, "Secondary passive/status pulses do not replay their source skill 400 times.");

            // Existing preferences are read only: live changes affect the mixer immediately.
            playerAudio.Configure(false, .7f, .65f, .8f);
            events = playerAudio.DebugSnapshot().playedEventCount;
            Check(!playerAudio.PlayCue("game.start"), "Global mute blocks new sound events.");
            yield return new WaitForSecondsRealtime(.15f); // Let the already-submitted device buffer drain.
            yield return Sample(.2f);
            Check(measuredRms < .000001, "Global mute silences the actual mixer.");
            Check(playerAudio.DebugSnapshot().activeMusicCount + playerAudio.DebugSnapshot().activeAmbientCount + playerAudio.DebugSnapshot().activeEffectCount + playerAudio.DebugSnapshot().activeVoiceCount == 0, "Global mute pauses music/ambience and stops effects.");
            Check(playerAudio.DebugSnapshot().playedEventCount == events, "Mute does not enqueue an unwanted start effect."); Record("muted");
            playerAudio.Configure(true, 0, .65f, .8f);
            playerAudio.Tick(true, null);
            yield return Sample(1.1f);
            Check(measuredRms < .000001, "Zero master volume silences music and effects."); Record("master_zero");
            playerAudio.Configure(true, .7f, .65f, 0);
            yield return Sample(.25f);
            Check(measuredRms > .000001, "Music remains audible with effects volume at zero."); Record("music_only");
            playerAudio.SetPaused(true);
            yield return new WaitForSecondsRealtime(.15f);
            yield return Sample(.15f);
            Check(playerAudio.DebugSnapshot().paused && measuredRms < .000001, "Application pause suspends all actual output."); Record("paused");
            playerAudio.SetPaused(false);
            playerAudio.Configure(true, .7f, 0, .8f);
            Check(playerAudio.PlayCue("camp.craft"), "Effects can resume independently of music.");
            yield return Sample(.2f);
            Check(measuredRms > .000001, "Effects remain audible with music volume at zero."); Record("effects_only");
            playerAudio.Configure(true, .7f, .65f, .8f);
            yield return Sample(.25f);
            Check(measuredRms > .000001 && playerAudio.DebugSnapshot().activeMusicCount == 1, "Restored preferences resume the existing music session."); Record("restored");

            report.normalSaveUnchanged = FileDigest(save) == initialSave;
            report.normalPreferencesUnchanged = PreferenceKeys.Select(PreferenceValue).SequenceEqual(initialPreferences);
            Check(report.playback.Any(proof => proof.snapshot.activeVoiceCount > 0), "At least one original character voice plays alongside a primary skill effect.");
            Check(report.normalSaveUnchanged, "Audio verification leaves the personal game save unchanged.");
            Check(report.normalPreferencesUnchanged, "Audio verification never writes normal audio preferences.");
            Check(playerAudio.DebugSnapshot().missingClipCount == 0, "No primary sound or required cue was missing during native playback.");
            report.status = report.errors.Count == 0 ? "PASS" : "FAIL";
            File.WriteAllText(Path.Combine(output, "native-audio.json"), JsonUtility.ToJson(report, true));
            Debug.Log("BTS AUDIO " + report.status + " / clips=" + report.clips.Count + " / cards=" + report.cardRoutes + " / assertions=" + report.assertions + " / errors=" + report.errors.Count + " / actualMixer=Unity AudioListener.GetOutputData / personalSaveUnchanged=" + report.normalSaveUnchanged + " / preferencesUnchanged=" + report.normalPreferencesUnchanged);
            playerAudio.Configure(false, 0, 0, 0);
            Application.Quit(report.errors.Count == 0 ? 0 : 1);
        }
        IEnumerator Sample(float seconds)
        {
            measuredRms = measuredPeak = 0;
            float until = Time.realtimeSinceStartup + seconds;
            do
            {
                yield return null;
                AudioListener.GetOutputData(mixer, 0);
                double sum = 0;
                foreach (float sample in mixer) { sum += sample * sample; measuredPeak = Math.Max(measuredPeak, Math.Abs(sample)); }
                measuredRms = Math.Max(measuredRms, Math.Sqrt(sum / mixer.Length));
            }
            while (Time.realtimeSinceStartup < until);
        }
        void Record(string name, GameAudio owner = null) { report.playback.Add(new PlaybackProof { name = name, mixerRms = measuredRms, mixerPeak = measuredPeak, snapshot = (owner ?? playerAudio).DebugSnapshot() }); }
        void Check(bool value, string reason) { report.assertions++; if (!value) { report.errors.Add(reason); Debug.LogWarning("BTS AUDIO CHECK FAILED: " + reason); } }
        IEnumerator VerifyAmbience()
        {
            var state = new RunState { stage = RunStage.Campfire };
            playerAudio.Tick(false, state);
            yield return Sample(.8f);
            var camp = playerAudio.DebugSnapshot();
            Check(camp.activeAmbientCount == 1 && camp.ambientCue == "camp.fire.loop" && camp.ambientVolume > 0, "The camp has one quiet fire loop on the effects channel.");
            Check(measuredRms > .000001, "The camp fire loop reaches the mixer with music volume at zero."); Record("ambient_camp");
            var source = playerAudio.GetComponents<AudioSource>().FirstOrDefault(s => s.priority == 64);
            float before = source == null ? 0 : source.time;
            for (int i = 0; i < 8; i++) { playerAudio.Tick(false, state); yield return null; }
            Check(source != null && source.isPlaying && source.time >= before && playerAudio.GetComponents<AudioSource>().Count(s => s.priority == 64) == 1, "Repeated camp Update ticks keep the same ambient source without restarting it.");
            playerAudio.Configure(true, .7f, 0, 0);
            yield return new WaitForSecondsRealtime(.15f);
            yield return Sample(.15f);
            Check(measuredRms < .000001 && playerAudio.DebugSnapshot().ambientVolume == 0, "Effects volume also controls the actual ambient output."); Record("ambient_effects_zero");
            playerAudio.Configure(true, .7f, 0, .8f);
            playerAudio.SetPaused(true);
            yield return new WaitForSecondsRealtime(.15f);
            yield return Sample(.15f);
            Check(playerAudio.DebugSnapshot().activeAmbientCount == 0 && measuredRms < .000001, "Pause suspends the fire loop too.");
            playerAudio.Configure(false, .7f, 0, .8f);
            playerAudio.SetPaused(false);
            Check(playerAudio.DebugSnapshot().activeAmbientCount == 0, "A muted fire loop stays paused when the application resumes.");
            playerAudio.Configure(true, .7f, 0, .8f);
            yield return Sample(.2f);
            Check(playerAudio.DebugSnapshot().activeAmbientCount == 1 && measuredRms > .000001, "Unmuting resumes one existing ambient loop."); Record("ambient_restored");
            state.stage = RunStage.Map; playerAudio.Tick(false, state);
            yield return Sample(.8f);
            var exited = playerAudio.DebugSnapshot();
            Check(exited.activeAmbientCount == 0 && exited.ambientCue == "" && exited.ambientVolume == 0, "Leaving the camp fades out and releases its ambient loop."); Record("ambient_exit");
        }
        IEnumerator VerifyUiActions()
        {
            var game = LumiaGame.Instance;
            var act = typeof(LumiaGame).GetMethod("Act", BindingFlags.Instance | BindingFlags.NonPublic);
            var mode = typeof(LumiaGame).GetField("audioVerification", BindingFlags.Instance | BindingFlags.NonPublic);
            var engineProperty = typeof(LumiaGame).GetProperty("Engine", BindingFlags.Instance | BindingFlags.Public);
            Check(game != null && game.Audio != null && act != null && mode != null && engineProperty != null, "The actual game UI action and audio hooks are available.");
            if (game == null || game.Audio == null || act == null || mode == null || engineProperty == null) yield break;
            var oldEngine = game.Engine; var oldAudio = game.Audio.DebugSnapshot(); bool oldMode = (bool)mode.GetValue(game);
            Check(oldMode, "UI audio action tests run only inside the isolated verification mode.");
            if (!oldMode) yield break;
            var engine = new GameEngine(650229);
            try
            {
                playerAudio.Configure(false, 0, 0, 0);
                engineProperty.SetValue(game, engine, null);
                game.Audio.Configure(true, .7f, 0, .8f);
                engine.State.stage = RunStage.Kiosk; engine.State.credits = 1200;
                string item = GameDatabase.Objects.First(o => o.id != "blood").id;
                int before = game.Audio.DebugSnapshot().playedEventCount;
                Check(InvokeUiAction(game, act, mode, () => engine.BuyObject(item), "kiosk.purchase"), "The real UI Act path completes a kiosk purchase.");
                Check(engine.State.objects.Contains(item) && game.Audio.DebugSnapshot().playedEventCount > before && game.Audio.DebugSnapshot().recentEvents.Any(e => e.StartsWith("kiosk.purchase ->", StringComparison.Ordinal)), "A successful purchase emits the real UI purchase cue.");
                yield return Sample(.2f);
                Check(measuredRms > .000001, "The UI purchase cue reaches the actual mixer."); Record("ui_action_purchase", game.Audio);

                engine.State.stage = RunStage.Campfire; engine.State.campChoice = 2; engine.State.campActions = 3;
                var gear = GameDatabase.Gear.First(); engine.State.objects.Add(gear.objectId);
                Check(InvokeUiAction(game, act, mode, () => engine.Craft(gear.id), "camp.craft"), "The real UI Act path completes equipment crafting.");
                var crafted = game.Audio.DebugSnapshot();
                Check(engine.State.gear.Contains(gear.id) && crafted.recentEvents.Any(e => e.StartsWith("camp.craft ->", StringComparison.Ordinal)), "Successful equipment crafting emits the real crafting cue.");
                Check(crafted.activeVoiceCount > 0 && crafted.recentEvents.Any(e => e.StartsWith("voice.nia.craft ->", StringComparison.Ordinal)), "The real crafting hook also plays Nia's original crafting voice.");
                yield return Sample(.2f);
                Check(measuredRms > .000001, "The UI crafting sound and original voice reach the mixer."); Record("ui_action_craft", game.Audio);
                int craftEvents = crafted.recentEvents.Count(e => e.StartsWith("camp.craft ->", StringComparison.Ordinal));
                engine.State.objects.Clear();
                Check(!InvokeUiAction(game, act, mode, () => engine.Craft(gear.id), "camp.craft"), "The real UI rejects crafting without its object.");
                Check(game.Audio.DebugSnapshot().recentEvents.Count(e => e.StartsWith("camp.craft ->", StringComparison.Ordinal)) == craftEvents, "A failed UI crafting action does not emit a success cue.");
                yield return new WaitForSecondsRealtime(2.1f); // The next voice is a separate completed action.
                var rawFood = GameDatabase.Foods.First(food => GameDatabase.Food(food.upgradeTo) != null);
                engine.State.foods.Add(rawFood.id);
                Check(InvokeUiAction(game, act, mode, () => engine.Cook(0), "camp.cook"), "The real UI Act path cooks an upgradable food.");
                Check(engine.State.foods[0] == rawFood.upgradeTo && game.Audio.DebugSnapshot().recentEvents.Any(e => e.StartsWith("camp.cook ->", StringComparison.Ordinal)), "Successful cooking emits the real UI cooking cue.");
                yield return Sample(.2f);
                Check(measuredRms > .000001, "The UI cooking sound reaches the actual mixer."); Record("ui_action_cook", game.Audio);
            }
            finally
            {
                mode.SetValue(game, oldMode);
                engineProperty.SetValue(game, oldEngine, null);
                game.Audio.Configure(oldAudio.enabled, oldAudio.master, oldAudio.music, oldAudio.effects);
                playerAudio.Configure(true, .7f, .65f, .8f);
            }
        }
        static bool InvokeUiAction(LumiaGame game, MethodInfo act, FieldInfo mode, Func<bool> action, string cue)
        {
            bool previous = (bool)mode.GetValue(game);
            try { mode.SetValue(game, false); return (bool)act.Invoke(game, new object[] { action, cue }); }
            finally { mode.SetValue(game, previous); }
        }
        void VerifySettingsPersistence()
        {
            var game = LumiaGame.Instance;
            if (game == null) { Check(false, "The real game settings owner exists."); return; }
            string[] names = { "volume", "musicVolume", "effectsVolume", "sound" };
            string[] keys = { "bts.audio.verify.lumia.volume", "bts.audio.verify.lumia.musicVolume", "bts.audio.verify.lumia.effectsVolume", "bts.audio.verify.lumia.sound" };
            FieldInfo[] fields = names.Select(name => typeof(LumiaGame).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)).ToArray();
            var verify = typeof(LumiaGame).GetField("audioVerification", BindingFlags.Instance | BindingFlags.NonPublic);
            bool isolated = verify != null && (bool)verify.GetValue(game);
            Check(isolated && fields.All(field => field != null), "Persistence verification is isolated from normal settings.");
            if (!isolated || fields.Any(field => field == null)) return;
            object[] previous = fields.Select(field => field.GetValue(game)).ToArray();
            bool[] existed = keys.Select(PlayerPrefs.HasKey).ToArray();
            float[] oldFloats = keys.Select(key => PlayerPrefs.GetFloat(key, 0)).ToArray();
            int oldSound = PlayerPrefs.GetInt(keys[3], 0);
            try
            {
                object[] sample = { .21f, .32f, .43f, false };
                for (int i = 0; i < fields.Length; i++) fields[i].SetValue(game, sample[i]);
                game.SaveAudioSettings(); PlayerPrefs.Save();
                bool roundTrip = Mathf.Approximately(PlayerPrefs.GetFloat(keys[0], -1), .21f) && Mathf.Approximately(PlayerPrefs.GetFloat(keys[1], -1), .32f)
                    && Mathf.Approximately(PlayerPrefs.GetFloat(keys[2], -1), .43f) && PlayerPrefs.GetInt(keys[3], -1) == 0;
                playerAudio.Configure(PlayerPrefs.GetInt(keys[3]) != 0, PlayerPrefs.GetFloat(keys[0]), PlayerPrefs.GetFloat(keys[1]), PlayerPrefs.GetFloat(keys[2]));
                var restored = playerAudio.DebugSnapshot();
                roundTrip &= !restored.enabled && Mathf.Approximately(restored.master, .21f) && Mathf.Approximately(restored.music, .32f) && Mathf.Approximately(restored.effects, .43f);
                report.isolatedSettingsRoundTrip = roundTrip;
                Check(roundTrip, "Actual game settings save and reload all three volumes and mute through isolated PlayerPrefs keys.");
            }
            finally
            {
                for (int i = 0; i < fields.Length; i++) fields[i].SetValue(game, previous[i]);
                for (int i = 0; i < keys.Length; i++)
                {
                    if (!existed[i]) PlayerPrefs.DeleteKey(keys[i]);
                    else if (i == 3) PlayerPrefs.SetInt(keys[i], oldSound);
                    else PlayerPrefs.SetFloat(keys[i], oldFloats[i]);
                }
                PlayerPrefs.Save();
                playerAudio.Configure(true, .7f, .65f, .8f);
            }
        }
        static string PreferenceValue(string key) { return PlayerPrefs.HasKey(key) + "|" + PlayerPrefs.GetFloat(key, -8173).ToString("R") + "|" + PlayerPrefs.GetInt(key, -8173) + "|" + PlayerPrefs.GetString(key, "<absent>"); }
        static string FileDigest(string path) { if (!File.Exists(path)) return "absent"; using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)); }
    }

}
