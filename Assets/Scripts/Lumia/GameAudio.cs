using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lumia
{
    [Serializable] public sealed class GameAudioSnapshot
    {
        public string musicTheme, musicClipId, ambientCue, ambientClipId, lastCue, lastClipId, lastCardId;
        public int activeMusicCount, activeAmbientCount, activeEffectCount, activeVoiceCount, playedEventCount, missingClipCount;
        public int primaryActionsPlayed, suppressedSecondaryActions, catalogClipCount, catalogBindingCount, catalogCardCount, loadedClipCount;
        public bool enabled, paused;
        public float master, music, effects, ambientVolume;
        public string[] recentEvents;
    }

    /// <summary>One music session, quiet location ambience and a bounded pool of catalogued sounds.</summary>
    public sealed class GameAudio : MonoBehaviour
    {
        public const int EffectVoiceLimit = 10;
        public const int SpokenVoiceLimit = 2;
        public const float MusicFadeSeconds = .9f;
        const string CatalogPath = "Lumia/Audio/catalog";

        sealed class Voice
        {
            public AudioSource source;
            public float gain, started;
            public int importance;
            public string cue, clipId;
        }

        readonly Dictionary<string, AudioClip> loaded = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        readonly HashSet<string> missing = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<string, float> nextCueAt = new Dictionary<string, float>(StringComparer.Ordinal);
        readonly Dictionary<string, int> sequences = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly Queue<string> recent = new Queue<string>();
        readonly List<Voice> effects = new List<Voice>(), voices = new List<Voice>();
        readonly AudioSource[] musicSources = new AudioSource[2];
        readonly string[] musicIds = new string[2];
        readonly float[] musicGains = new float[2], musicFade = new float[2], fadeFrom = new float[2], fadeTo = new float[2];
        float fadeAt, masterVolume = .4f, musicVolume = .65f, effectsVolume = .8f, nextSpokenAt, musicDuck = 1;
        AudioSource ambientSource;
        float ambientFade, ambientTarget, ambientGain;
        bool audioEnabled = true, requestedPause, applicationPause, isPaused, initialized;
        int currentMusic = -1, played, primaryPlayed, secondarySuppressed;
        string currentTheme = "", lastCue = "", lastClipId = "", lastCardId = "";
        string currentAmbient = "", ambientId = "";

        public GameAudioCatalog Catalog { get; private set; }
        public bool Ready { get { return Catalog != null && Catalog.ClipCount > 0; } }

        public static GameAudio Attach(GameObject host)
        {
            if (host == null) throw new ArgumentNullException("host");
            var existing = host.GetComponent<GameAudio>();
            return existing != null ? existing : host.AddComponent<GameAudio>();
        }

        void Awake() { Initialize(); }

        void Initialize()
        {
            if (initialized) return;
            initialized = true;
            AudioCatalogData data = null;
            var manifest = Resources.Load<TextAsset>(CatalogPath);
            if (manifest != null)
            {
                try { data = JsonUtility.FromJson<AudioCatalogData>(manifest.text); }
                catch (Exception exception) { Debug.LogWarning("BTS audio catalog could not be read: " + exception.Message); }
            }
            Catalog = new GameAudioCatalog(data);
            if (manifest == null) Missing("catalog", "The audio catalog is unavailable.");
            foreach (string error in Catalog.Validate()) Debug.LogWarning("BTS audio catalog: " + error);
            for (int i = 0; i < musicSources.Length; i++)
            {
                musicSources[i] = NewSource(32);
                musicSources[i].loop = true;
            }
            for (int i = 0; i < EffectVoiceLimit; i++) effects.Add(new Voice { source = NewSource(96) });
            for (int i = 0; i < SpokenVoiceLimit; i++) voices.Add(new Voice { source = NewSource(80) });
            ambientSource = NewSource(64);
            ambientSource.loop = true;
        }

        AudioSource NewSource(int priority)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.dopplerLevel = 0;
            source.priority = priority;
            source.volume = 0;
            source.ignoreListenerPause = false;
            return source;
        }

        public void Configure(bool enabled, float master, float music = .65f, float effects = .8f)
        {
            Initialize();
            bool changed = audioEnabled != enabled;
            audioEnabled = enabled;
            masterVolume = SafeVolume(master);
            musicVolume = SafeVolume(music);
            effectsVolume = SafeVolume(effects);
            if (changed)
            {
                if (!audioEnabled)
                {
                    foreach (var voice in this.effects.Concat(voices)) voice.source.Stop();
                    foreach (var source in musicSources) source.Pause();
                    ambientSource.Pause();
                }
                else if (!isPaused)
                {
                    foreach (var source in musicSources) if (source.clip != null) source.UnPause();
                    StartCurrentMusicIfNeeded();
                    StartAmbientIfNeeded();
                }
            }
            ApplyVolumes();
        }

        static float SafeVolume(float value) { return float.IsNaN(value) || float.IsInfinity(value) ? 0 : Mathf.Clamp01(value); }
        static float SafeGain(float value) { return float.IsNaN(value) || float.IsInfinity(value) ? 0 : Mathf.Clamp(value, 0, 2); }

        /// <summary>Called once per Update; opening a panel does not restart or mute music.</summary>
        public void Tick(bool lobby, RunState state, bool paused = false)
        {
            Initialize();
            SetPaused(paused);
            string theme = Theme(lobby, state);
            if (!string.Equals(theme, currentTheme, StringComparison.Ordinal)) RequestMusic(theme);
            RequestAmbient(!lobby && state != null && state.stage == RunStage.Campfire);
        }

        public static string Theme(bool lobby, RunState state)
        {
            if (lobby || state == null) return "music.lobby";
            switch (state.stage)
            {
                case RunStage.Preparation: return "music.preparation";
                case RunStage.Combat:
                    var node = state.map == null ? null : state.map.Find(x => x != null && x.id == state.activeNodeId);
                    return node != null && node.kind == ZoneKind.Boss ? "music.boss" : "music.combat";
                case RunStage.Kiosk: return "music.kiosk";
                case RunStage.Campfire: return "music.camp";
                case RunStage.Encounter: return "music.encounter";
                case RunStage.Won: return "music.win";
                case RunStage.Lost: return "music.lose";
                default: return "music.map";
            }
        }

        void RequestMusic(string theme)
        {
            currentTheme = theme;
            var selected = Catalog.ResolveCue(theme);
            if (selected == null) { Missing("cue:" + theme, "No music binding for " + theme); return; }
            var clip = LoadClip(selected.clip.id);
            if (clip == null) return;
            int destination = -1;
            for (int i = 0; i < musicSources.Length; i++)
                if (musicIds[i] == selected.clip.id && musicSources[i].clip == clip) { destination = i; break; }
            if (destination < 0)
            {
                destination = currentMusic == 0 ? 1 : 0;
                musicSources[destination].Stop();
                musicSources[destination].clip = clip;
                musicIds[destination] = selected.clip.id;
                musicFade[destination] = 0;
            }
            currentMusic = destination;
            musicGains[destination] = SafeGain(selected.volume);
            for (int i = 0; i < musicSources.Length; i++)
            {
                fadeFrom[i] = musicFade[i];
                fadeTo[i] = i == destination ? 1 : 0;
            }
            fadeAt = Time.unscaledTime;
            ApplyVolumes();
            if (audioEnabled && !isPaused && !musicSources[destination].isPlaying) musicSources[destination].Play();
        }

        void RequestAmbient(bool camp)
        {
            ambientTarget = camp ? 1 : 0;
            if (!camp) return;
            const string cue = "camp.fire.loop";
            if (currentAmbient == cue && ambientSource.clip != null) { StartAmbientIfNeeded(); return; }
            var selection = Catalog.ResolveCue(cue);
            if (selection == null) { Missing("cue:" + cue, "No ambience binding for " + cue); return; }
            var clip = LoadClip(selection.clip.id);
            if (clip == null) return;
            ambientSource.Stop();
            ambientSource.clip = clip;
            currentAmbient = cue;
            ambientId = selection.clip.id;
            ambientGain = SafeGain(selection.volume) * .32f;
            ambientFade = 0;
            ApplyVolumes();
            StartAmbientIfNeeded();
        }

        void StartAmbientIfNeeded()
        {
            if (!audioEnabled || isPaused || ambientTarget <= 0 || ambientSource.clip == null) return;
            ambientSource.UnPause();
            if (!ambientSource.isPlaying) ambientSource.Play();
        }

        void Update()
        {
            if (!initialized) return;
            float progress = Mathf.Clamp01((Time.unscaledTime - fadeAt) / MusicFadeSeconds);
            for (int i = 0; i < musicSources.Length; i++)
            {
                musicFade[i] = Mathf.Lerp(fadeFrom[i], fadeTo[i], progress);
                if (progress >= 1 && fadeTo[i] == 0 && musicSources[i].isPlaying) musicSources[i].Stop();
            }
            musicDuck = Mathf.MoveTowards(musicDuck, voices.Any(v => v.source.isPlaying) ? .82f : 1, Time.unscaledDeltaTime * 2);
            ambientFade = Mathf.MoveTowards(ambientFade, ambientTarget, Time.unscaledDeltaTime * 2);
            if (ambientFade <= 0 && ambientTarget == 0 && ambientSource.clip != null)
            {
                ambientSource.Stop();
                ambientSource.clip = null;
                currentAmbient = ambientId = "";
            }
            ApplyVolumes();
        }

        void ApplyVolumes()
        {
            float master = audioEnabled ? masterVolume : 0;
            for (int i = 0; i < musicSources.Length; i++)
                if (musicSources[i] != null) musicSources[i].volume = Mathf.Clamp01(master * musicVolume * musicGains[i] * musicFade[i] * musicDuck);
            foreach (var voice in effects) voice.source.volume = Mathf.Clamp01(master * effectsVolume * voice.gain);
            foreach (var voice in voices) voice.source.volume = Mathf.Clamp01(master * effectsVolume * voice.gain * .72f);
            if (ambientSource != null) ambientSource.volume = Mathf.Clamp01(master * effectsVolume * ambientGain * ambientFade);
        }

        public void SetPaused(bool paused)
        {
            requestedPause = paused;
            ApplyPause();
        }

        void OnApplicationPause(bool paused)
        {
            applicationPause = paused;
            ApplyPause();
        }

        void ApplyPause()
        {
            bool pause = requestedPause || applicationPause;
            if (!initialized || pause == isPaused) return;
            isPaused = pause;
            if (pause)
            {
                foreach (var source in musicSources) source.Pause();
                foreach (var voice in effects.Concat(voices)) voice.source.Pause();
                ambientSource.Pause();
            }
            else if (audioEnabled)
            {
                foreach (var source in musicSources) if (source.clip != null) source.UnPause();
                foreach (var voice in effects.Concat(voices)) voice.source.UnPause();
                StartCurrentMusicIfNeeded();
                StartAmbientIfNeeded();
            }
        }

        void StartCurrentMusicIfNeeded()
        {
            if (currentMusic >= 0 && musicSources[currentMusic].clip != null && !musicSources[currentMusic].isPlaying)
                musicSources[currentMusic].Play();
        }

        int Sequence(string cue)
        {
            int sequence;
            sequences.TryGetValue(cue, out sequence);
            sequences[cue] = sequence == int.MaxValue ? 0 : sequence + 1;
            return sequence;
        }

        public bool PlayCue(string cue, float gain = 1)
        {
            Initialize();
            if (!audioEnabled || isPaused || string.IsNullOrEmpty(cue)) return false;
            var selection = Catalog.ResolveCue(cue, Sequence(cue));
            if (selection == null) { Missing("cue:" + cue, "No sound binding for " + cue); return false; }
            return Play(selection, cue, gain, CueImportance(cue));
        }

        static int CueImportance(string cue)
        {
            return cue == "game.start" || cue == "game.win" || cue == "game.lose" || cue == "combat.win" || cue == "boss.start" ? 3
                : cue.StartsWith("ui.", StringComparison.Ordinal) ? 0 : 1;
        }

        bool Play(AudioSelection selection, string requestedCue, float gain, int importance)
        {
            if (selection == null || selection.clip == null) return false;
            float now = Time.unscaledTime, cooldown = requestedCue.StartsWith("ui.", StringComparison.Ordinal) ? .045f
                : requestedCue.StartsWith("wild.", StringComparison.Ordinal) ? .28f : .055f;
            float next;
            if (nextCueAt.TryGetValue(requestedCue, out next) && now < next) return true;
            bool spoken = selection.clip.channel == "voice" || selection.clip.kind == "voice";
            if (spoken && now < nextSpokenAt) return true;
            var clip = LoadClip(selection.clip.id);
            if (clip == null) return false;
            var pool = spoken ? voices : effects;
            var slot = pool.Find(v => !v.source.isPlaying);
            if (slot == null)
            {
                slot = pool.Where(v => v.importance <= importance).OrderBy(v => v.started).FirstOrDefault();
                if (slot == null) return true;
            }
            nextCueAt[requestedCue] = now + (spoken ? 2.4f : cooldown);
            if (spoken) nextSpokenAt = now + Mathf.Clamp(clip.length * .65f, 1.1f, 2);
            slot.source.Stop();
            slot.source.clip = clip;
            slot.gain = SafeGain(selection.volume * SafeVolume(gain));
            slot.importance = importance;
            slot.started = now;
            slot.cue = requestedCue;
            slot.clipId = selection.clip.id;
            ApplyVolumes();
            slot.source.Play();
            played++;
            lastCue = requestedCue;
            lastClipId = selection.clip.id;
            recent.Enqueue(requestedCue + " -> " + selection.clip.id);
            while (recent.Count > 16) recent.Dequeue();
            return true;
        }

        /// <returns>True also means a secondary pulse was intentionally silent; only a missing primary clip requests the legacy fallback.</returns>
        public bool PlayCombatAction(CombatAction action, CombatState combat)
        {
            Initialize();
            if (action == null || !audioEnabled || isPaused) return true;
            if (!string.IsNullOrEmpty(action.kind) || !string.IsNullOrEmpty(action.traitId)
                || (action.cardId ?? "").StartsWith("status_", StringComparison.Ordinal))
            {
                secondarySuppressed++;
                if (action.kind == "revive") PlayCue("combat.revive", .6f);
                return true;
            }
            var card = GameDatabase.Card(action.cardId);
            if (card == null) { secondarySuppressed++; return true; }
            lastCardId = card.id;
            string cue = "card." + card.id;
            AudioSelection selection = null;
            if (action.enemy && combat != null && !string.IsNullOrEmpty(combat.animal))
            {
                cue = "wild." + combat.animal + ".attack";
                selection = Catalog.ResolveCue(cue, Sequence(cue));
            }
            if (selection == null) selection = Catalog.ResolveCard(card, Sequence("card." + card.id));
            if (selection == null) { Missing("card:" + card.id, "No skill sound for " + card.id); return false; }
            bool handled = Play(selection, cue, action.enemy ? .9f : 1, 2);
            if (handled) primaryPlayed++;
            // Voice lines are optional and retain their own stricter rate and voice limits.
            var voice = Catalog.ResolveCue("voice." + card.id, Sequence("voice." + card.id));
            if (voice != null) Play(voice, "voice." + card.id, .8f, 1);
            if (action.critical) PlayCue("combat.critical", .32f);
            return handled;
        }

        public AudioClip LoadClip(string clipId)
        {
            Initialize();
            AudioClip clip;
            if (loaded.TryGetValue(clipId ?? "", out clip)) return clip;
            var definition = Catalog.FindClip(clipId);
            if (definition == null) { Missing("clip:" + clipId, "Unknown audio clip " + clipId); return null; }
            if (string.IsNullOrWhiteSpace(definition.path)) { Missing("clip:" + clipId, "Empty audio path for " + clipId); return null; }
            clip = Resources.Load<AudioClip>(definition.path);
            loaded[clipId] = clip;
            if (clip == null) Missing("clip:" + clipId, "Audio asset unavailable at Resources/" + definition.path);
            return clip;
        }

        void Missing(string key, string message)
        {
            if (missing.Add(key)) Debug.LogWarning("BTS audio: " + message);
        }

        public string[] ValidateCatalog(bool loadAll = true)
        {
            Initialize();
            var errors = new List<string>(Catalog.Validate());
            if (Catalog.ClipCount == 0) errors.Add("Audio catalog contains no clips.");
            if (loadAll)
                foreach (var definition in Catalog.Clips)
                {
                    var clip = LoadClip(definition.id);
                    if (clip == null) errors.Add("Missing Unity AudioClip: " + definition.id);
                    else if (clip.samples <= 0 || clip.frequency <= 0 || clip.length <= 0) errors.Add("Empty Unity AudioClip: " + definition.id);
                }
            return errors.ToArray();
        }

        public GameAudioSnapshot DebugSnapshot()
        {
            Initialize();
            return new GameAudioSnapshot
            {
                musicTheme = currentTheme, musicClipId = currentMusic < 0 ? "" : musicIds[currentMusic],
                ambientCue = currentAmbient, ambientClipId = ambientId, activeAmbientCount = ambientSource.isPlaying ? 1 : 0,
                ambientVolume = ambientSource.volume,
                lastCue = lastCue, lastClipId = lastClipId, lastCardId = lastCardId,
                activeMusicCount = musicSources.Count(s => s != null && s.isPlaying),
                activeEffectCount = effects.Count(v => v.source.isPlaying), activeVoiceCount = voices.Count(v => v.source.isPlaying),
                playedEventCount = played, missingClipCount = missing.Count, primaryActionsPlayed = primaryPlayed,
                suppressedSecondaryActions = secondarySuppressed, catalogClipCount = Catalog.ClipCount,
                catalogBindingCount = Catalog.BindingCount, catalogCardCount = Catalog.CardBindingCount,
                loadedClipCount = loaded.Count(x => x.Value != null), enabled = audioEnabled, paused = isPaused,
                master = masterVolume, music = musicVolume, effects = effectsVolume, recentEvents = recent.ToArray()
            };
        }

        void OnDestroy()
        {
            foreach (var source in musicSources) if (source != null) source.Stop();
            foreach (var voice in effects.Concat(voices)) if (voice.source != null) voice.source.Stop();
            if (ambientSource != null) ambientSource.Stop();
        }
    }
}
