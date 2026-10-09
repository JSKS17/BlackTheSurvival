using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    // This manifest contains references and provenance only. It never changes game randomness.
    [Serializable] public sealed class AudioCatalogData
    {
        public int schemaVersion = 1;
        public AudioClipDefinition[] clips = new AudioClipDefinition[0];
        public AudioCueBinding[] bindings = new AudioCueBinding[0];
        public AudioCardBinding[] cards = new AudioCardBinding[0];
    }

    [Serializable] public sealed class AudioClipDefinition
    {
        public string id, path, sourceUrl, sourceName, license, kind;
        public string channel = "effects";
        public float volume = 1;
    }

    [Serializable] public sealed class AudioCueBinding
    {
        public string cue;
        public string[] clipIds = new string[0];
        public float volume = 1;
    }

    [Serializable] public sealed class AudioCardBinding
    {
        public string cardId;
        public string[] clipIds = new string[0];
        public float volume = 1;
    }

    public sealed class AudioSelection
    {
        public string cue;
        public AudioClipDefinition clip;
        public float volume;
    }

    /// <summary>Pure, deterministic lookup shared by playback and the asset audit.</summary>
    public sealed class GameAudioCatalog
    {
        readonly Dictionary<string, AudioClipDefinition> clips = new Dictionary<string, AudioClipDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, AudioCueBinding> cues = new Dictionary<string, AudioCueBinding>(StringComparer.Ordinal);
        readonly Dictionary<string, AudioCardBinding> cards = new Dictionary<string, AudioCardBinding>(StringComparer.Ordinal);
        readonly List<string> errors = new List<string>();
        public AudioCatalogData Data { get; private set; }
        public int ClipCount { get { return clips.Count; } }
        public int BindingCount { get { return cues.Count; } }
        public int CardBindingCount { get { return cards.Count; } }
        public IEnumerable<AudioClipDefinition> Clips { get { return clips.Values; } }

        static readonly Dictionary<string, string[]> Aliases = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            {"ui.click",new[]{"ui.select"}}, {"ui.select",new[]{"ui.click"}},
            {"ui.cancel",new[]{"ui.click"}}, {"ui.reroll",new[]{"ui.select"}}, {"ui.error",new[]{"ui.cancel"}},
            {"game.resume",new[]{"ui.select"}}, {"map.enter",new[]{"map.move"}},
            {"combat.start",new[]{"boss.enter"}}, {"boss.start",new[]{"boss.enter","combat.start"}},
            {"game.win",new[]{"combat.win"}}, {"game.lose",new[]{"combat.lose"}},
            {"camp.rest",new[]{"heal"}}, {"camp.craft",new[]{"craft.complete"}},
            {"camp.cook",new[]{"cook.complete"}}, {"item.heal",new[]{"heal"}},
            {"encounter.enter",new[]{"map.enter"}}, {"encounter.choose",new[]{"ui.select"}},
            {"reward.select",new[]{"reward.card","ui.select"}}, {"reward.confirm",new[]{"reward.open"}},
            {"turn.end",new[]{"ui.select"}}, {"combat.critical",new[]{"basic.attack"}},
            {"music.preparation",new[]{"music.lobby"}}, {"music.boss",new[]{"music.combat"}},
            {"music.camp",new[]{"music.map"}}, {"music.kiosk",new[]{"music.map"}},
            {"music.encounter",new[]{"music.map"}}, {"music.win",new[]{"music.map"}},
            {"music.lose",new[]{"music.lobby"}}
        };

        public GameAudioCatalog(AudioCatalogData data)
        {
            Data = data ?? new AudioCatalogData();
            if (Data.schemaVersion != 1) errors.Add("Unsupported audio catalog schema: " + Data.schemaVersion);
            foreach (var entry in Data.clips ?? new AudioClipDefinition[0])
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.id)) { errors.Add("Audio clip has no id."); continue; }
                if (clips.ContainsKey(entry.id)) { errors.Add("Duplicate audio clip: " + entry.id); continue; }
                clips.Add(entry.id, entry);
                if (string.IsNullOrWhiteSpace(entry.path) || !entry.path.StartsWith("Lumia/Audio/", StringComparison.Ordinal)
                    || entry.path.Contains("..") || entry.path.Contains("\\") || entry.path.Contains(":")) errors.Add("Invalid Resources path: " + entry.id);
                if (!ValidGain(entry.volume)) errors.Add("Invalid clip volume: " + entry.id);
                if (string.IsNullOrWhiteSpace(entry.sourceUrl) || string.IsNullOrWhiteSpace(entry.sourceName)) errors.Add("Missing audio provenance: " + entry.id);
            }
            foreach (var entry in Data.bindings ?? new AudioCueBinding[0])
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.cue)) { errors.Add("Audio binding has no cue."); continue; }
                if (cues.ContainsKey(entry.cue)) { errors.Add("Duplicate audio cue: " + entry.cue); continue; }
                cues.Add(entry.cue, entry);
                CheckReferences(entry.cue, entry.clipIds, entry.volume);
            }
            foreach (var entry in Data.cards ?? new AudioCardBinding[0])
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.cardId)) { errors.Add("Audio card binding has no card id."); continue; }
                if (cards.ContainsKey(entry.cardId)) { errors.Add("Duplicate audio card: " + entry.cardId); continue; }
                cards.Add(entry.cardId, entry);
                CheckReferences(entry.cardId, entry.clipIds, entry.volume);
            }
        }

        static bool ValidGain(float value) { return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 2; }
        void CheckReferences(string owner, string[] ids, float gain)
        {
            if (!ValidGain(gain)) errors.Add("Invalid binding volume: " + owner);
            if (ids == null || ids.Length == 0) { errors.Add("Empty audio binding: " + owner); return; }
            foreach (string id in ids)
                if (string.IsNullOrEmpty(id) || !clips.ContainsKey(id)) errors.Add("Missing clip reference: " + owner + " -> " + id);
        }

        public string[] Validate() { return errors.ToArray(); }
        public AudioClipDefinition FindClip(string id) { AudioClipDefinition value; return id != null && clips.TryGetValue(id, out value) ? value : null; }

        public AudioSelection ResolveCue(string cue, int sequence = 0)
        {
            return ResolveCue(cue, sequence, new HashSet<string>(StringComparer.Ordinal));
        }

        AudioSelection ResolveCue(string cue, int sequence, HashSet<string> visited)
        {
            if (string.IsNullOrEmpty(cue) || !visited.Add(cue)) return null;
            AudioCueBinding binding;
            if (cues.TryGetValue(cue, out binding))
            {
                var selection = Select(cue, binding.clipIds, binding.volume, sequence);
                if (selection != null) return selection;
            }
            string[] alternatives;
            if (Aliases.TryGetValue(cue, out alternatives))
                foreach (string alias in alternatives)
                {
                    var selection = ResolveCue(alias, sequence, visited);
                    if (selection != null) return selection;
                }
            return null;
        }

        AudioSelection Select(string cue, string[] ids, float gain, int sequence)
        {
            if (ids == null || ids.Length == 0) return null;
            int index = (int)((uint)sequence % (uint)ids.Length);
            for (int offset = 0; offset < ids.Length; offset++)
            {
                var clip = FindClip(ids[(index + offset) % ids.Length]);
                if (clip != null) return new AudioSelection { cue = cue, clip = clip, volume = gain * clip.volume };
            }
            return null;
        }

        public AudioSelection ResolveCard(CardDef card, int sequence = 0)
        {
            if (card == null) return null;
            AudioCardBinding binding;
            if (cards.TryGetValue(card.id ?? "", out binding))
            {
                var direct = Select("card." + card.id, binding.clipIds, binding.volume, sequence);
                if (direct != null) return direct;
            }
            foreach (string cue in CandidateCues(card))
            {
                var selection = ResolveCue(cue, sequence);
                if (selection != null) return selection;
            }
            return null;
        }

        public static string[] CandidateCues(CardDef card)
        {
            if (card == null) return new string[0];
            var result = new List<string>();
            string id = card.id ?? "";
            result.Add("skill." + id);
            int split = id.LastIndexOf('_');
            if (split > 0 && !string.IsNullOrEmpty(card.key)) result.Add("skill." + id.Substring(0, split) + "." + card.key.ToLowerInvariant());
            if (id.StartsWith("weapon_", StringComparison.Ordinal)) result.Add("weapon." + id.Substring(7));
            if (id.StartsWith("tactical_", StringComparison.Ordinal)) result.Add("tactical." + id.Substring(9));
            if (id == "basic_attack") result.Add("basic.attack");
            if (id == "basic_guard") result.Add("basic.guard");
            if (!string.IsNullOrEmpty(card.effect)) result.Add("effect." + card.effect);
            if (card.heal > 0 && card.damage == 0) result.Add("effect.heal");
            if (card.block > 0 && card.damage == 0) result.Add("effect.shield");
            if (card.movement) result.Add("effect.dash");
            result.Add("skill.default");
            return result.Distinct(StringComparer.Ordinal).ToArray();
        }
    }
}
