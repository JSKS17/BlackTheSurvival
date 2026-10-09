using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using Lumia;

// Audits the shipped source assets and the same pure lookups used by the player.
// Compressed-audio decoding and actual mixer output are covered by GameAudioVerification.
public static class AudioAudit
{
    static int assertions;
    static void Check(bool condition, string message) { ++assertions; if (!condition) throw new InvalidDataException(message); }
    static readonly string[] RequiredCues = {
        "ui.click", "ui.select", "ui.cancel", "ui.reroll", "ui.error", "game.start", "game.resume", "game.win", "game.lose",
        "map.enter", "combat.start", "combat.win", "boss.start", "level.up", "kiosk.enter", "kiosk.purchase",
        "camp.enter", "camp.rest", "camp.craft", "camp.cook", "item.heal", "encounter.enter", "encounter.choose",
        "reward.select", "reward.confirm", "turn.end", "music.lobby", "music.preparation", "music.map", "music.combat",
        "music.boss", "music.camp", "music.kiosk", "music.encounter", "music.win", "music.lose", "basic.attack", "basic.guard", "skill.default", "camp.fire.loop"
    };
    public static int Main(string[] args)
    {
        try
        {
            string root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
            string output = Path.GetFullPath(args.Length > 1 ? args[1] : Path.Combine(root, "Temp/AudioAudit/catalog-audit.json"));
            string path = Path.Combine(root, "Assets/Resources/Lumia/Audio/catalog.json");
            Check(File.Exists(path), "The shipped audio catalog is missing.");
            var json = new JavaScriptSerializer { MaxJsonLength = 64 * 1024 * 1024 };
            string manifestText = File.ReadAllText(path);
            var data = json.Deserialize<AudioCatalogData>(manifestText);
            var raw = json.Deserialize<Dictionary<string, object>>(manifestText);
            var metadata = ((IEnumerable)raw["clips"]).Cast<object>().Select(entry => (Dictionary<string, object>)entry)
                .ToDictionary(entry => (string)entry["id"], StringComparer.Ordinal);
            var catalog = new GameAudioCatalog(data);
            Check(catalog.Validate().Length == 0, string.Join("\n", catalog.Validate()));
            Check(catalog.ClipCount > 0, "The audio catalog contains no real clips.");
            var files = new List<object>();
            long bytes = 0;
            foreach (var clip in catalog.Clips.OrderBy(c => c.id, StringComparer.Ordinal))
            {
                Uri uri;
                bool webSource = Uri.TryCreate(clip.sourceUrl, UriKind.Absolute, out uri) && (uri.Scheme == "https" || uri.Scheme == "http");
                bool designedSource = false;
                if (clip.kind == "fan_created_effect" && (clip.sourceUrl ?? "").StartsWith("generated:Tools/", StringComparison.Ordinal))
                {
                    string script = Path.GetFullPath(Path.Combine(root, clip.sourceUrl.Substring("generated:".Length).Replace('/', Path.DirectorySeparatorChar)));
                    designedSource = script.StartsWith(Path.GetFullPath(Path.Combine(root, "Tools")) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && File.Exists(script);
                }
                Check(webSource || designedSource, clip.id + ": usable original source URL or reproducible designed-audio script is required.");
                Check(!string.IsNullOrWhiteSpace(clip.sourceName), clip.id + ": source name is required.");
                Check(!string.IsNullOrWhiteSpace(clip.license), clip.id + ": rights/provenance note is required.");
                Check(!string.IsNullOrWhiteSpace(clip.kind), clip.id + ": original/supplemental kind must be explicit.");
                Check(clip.channel == "music" || clip.channel == "effects" || clip.channel == "voice", clip.id + ": unknown playback channel.");
                string asset = AssetPath(root, clip.path);
                Check(File.Exists(asset + ".meta"), clip.id + ": the Unity import metadata must be committed with the clip.");
                var info = new FileInfo(asset); bytes += info.Length;
                Check(info.Length > 44, clip.id + ": the audio file is empty.");
                string digest = Hash(asset);
                var declared = metadata[clip.id];
                Check(declared.ContainsKey("sha256") && string.Equals(Convert.ToString(declared["sha256"]), digest, StringComparison.OrdinalIgnoreCase), clip.id + ": shipped bytes differ from declared source SHA-256.");
                Check(declared.ContainsKey("bytes") && Convert.ToInt64(declared["bytes"]) == info.Length, clip.id + ": shipped bytes differ from declared source size.");
                string origin = declared.ContainsKey("origin") ? Convert.ToString(declared["origin"]) : "unspecified";
                Check(origin != "unspecified", clip.id + ": original versus designed audio origin must be explicit.");
                var pcm = Path.GetExtension(asset).Equals(".wav", StringComparison.OrdinalIgnoreCase) ? ReadWave(asset) : null;
                if (pcm != null)
                {
                    Check(pcm.duration >= .015 && pcm.duration <= 1800, clip.id + ": invalid audio duration.");
                    Check(pcm.sampleRate >= 8000 && pcm.sampleRate <= 192000, clip.id + ": unsupported sample rate.");
                    Check(pcm.channels >= 1 && pcm.channels <= 8, clip.id + ": unsupported channel count.");
                    Check(pcm.rms > .00001, clip.id + ": silent PCM asset.");
                    Check(pcm.peak <= 1.00001, clip.id + ": invalid/clipped floating-point PCM.");
                    Check(pcm.clippedFraction < .02, clip.id + ": excessive hard-clipped samples.");
                }
                files.Add(new { clip.id, clip.path, clip.channel, clip.kind, clip.sourceUrl, clip.sourceName, clip.license, clip.volume,
                    file = asset.Substring(root.Length + 1).Replace('\\', '/'), size = info.Length, sha256 = digest, origin,
                    decoder = pcm == null ? "Unity native verification required" : "PCM RIFF", metrics = pcm });
            }
            var cues = new List<object>();
            foreach (string cue in RequiredCues.Concat(new[] { "chicken", "dog", "boar", "wolf", "bear" }.SelectMany(a => new[] { "wild." + a + ".enter", "wild." + a + ".attack" })))
            {
                var selected = catalog.ResolveCue(cue);
                Check(selected != null && selected.clip != null, "Missing required gameplay cue: " + cue);
                Check(selected.volume > 0 && selected.volume <= 2, "Invalid final gain: " + cue);
                cues.Add(new { requested = cue, resolved = selected.cue, clip = selected.clip.id, selected.volume });
            }
            var mapping = new List<object>();
            int direct = 0, fallback = 0;
            var state = new GameEngine(650229).State; int originalRandom = state.rngState;
            foreach (var card in GameDatabase.Cards)
            {
                var selected = catalog.ResolveCard(card, 0);
                Check(selected != null && selected.clip != null, "No playable sound or documented fallback for card: " + card.id);
                Check(selected.volume > 0 && selected.volume <= 2, "Invalid card sound gain: " + card.id);
                bool exact = data.cards.Any(binding => binding.cardId == card.id) || selected.cue == "skill." + card.id;
                if (exact) direct++; else fallback++;
                for (int i = 0; i < 3; i++)
                {
                    var a = catalog.ResolveCard(card, i); var b = catalog.ResolveCard(card, i);
                    Check(a != null && b != null && a.clip.id == b.clip.id && a.volume == b.volume, card.id + ": deterministic playback variation changed.");
                }
                mapping.Add(new { card = card.id, card.owner, card.key, resolution = exact ? "explicit card mapping" : "shared effect fallback", selected.cue, clip = selected.clip.id, selected.clip.kind });
            }
            Check(state.rngState == originalRandom, "Audio lookups changed gameplay RNG.");
            foreach (var binding in data.cards) Check(GameDatabase.Card(binding.cardId) != null, "Unknown mapped gameplay card: " + binding.cardId);
            NegativeCases();
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = new { status = "PASS", schemaVersion = data.schemaVersion, assertions, clips = catalog.ClipCount,
                cueBindings = catalog.BindingCount, cards = GameDatabase.Cards.Count, explicitCards = direct, sharedEffectFallbackCards = fallback,
                totalBytes = bytes, kinds = data.clips.GroupBy(clip => clip.kind).ToDictionary(group => group.Key, group => group.Count()), files, requiredCues = cues, cardMappings = mapping,
                scope = "Source-file provenance, physical assets, PCM metrics where available, deterministic mappings and invalid-catalog rejection. Native player report covers compressed decoding and actual Unity mixer output." };
            File.WriteAllText(output, json.Serialize(report), new UTF8Encoding(false));
            Console.WriteLine("AUDIO AUDIT PASS / clips=" + catalog.ClipCount + " / cards=" + mapping.Count + " / explicit=" + direct + " / shared=" + fallback + " / assertions=" + assertions);
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("AUDIO AUDIT FAILED: " + error); return 1; }
    }
    static string AssetPath(string root, string resource)
    {
        string prefix = Path.GetFullPath(Path.Combine(root, "Assets/Resources")) + Path.DirectorySeparatorChar;
        string basePath = Path.GetFullPath(Path.Combine(prefix, resource.Replace('/', Path.DirectorySeparatorChar)));
        Check(basePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase), "Audio resource escaped Assets/Resources: " + resource);
        if (File.Exists(basePath)) return basePath;
        var matches = new[] { ".wav", ".ogg", ".mp3", ".aiff", ".aif" }.Select(ext => basePath + ext).Where(File.Exists).ToArray();
        Check(matches.Length == 1, "Audio resource must resolve to exactly one physical clip: " + resource);
        return matches[0];
    }
    static string Hash(string file) { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(file)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
    static AudioClipDefinition Fixture(string id, string path = "Lumia/Audio/fixture", float volume = 1)
    {
        return new AudioClipDefinition { id = id, path = path, volume = volume, sourceUrl = "https://example.invalid/test-only", sourceName = "audit fixture", license = "test fixture", kind = "fixture" };
    }
    static void NegativeCases()
    {
        Check(new GameAudioCatalog(new AudioCatalogData { clips = new[] { Fixture("a"), Fixture("a") } }).Validate().Length > 0, "Duplicate clips were accepted.");
        Check(new GameAudioCatalog(new AudioCatalogData { clips = new[] { Fixture("a", "../outside") } }).Validate().Length > 0, "Unsafe resource paths were accepted.");
        Check(new GameAudioCatalog(new AudioCatalogData { clips = new[] { Fixture("a", volume: float.NaN) } }).Validate().Length > 0, "NaN gains were accepted.");
        Check(new GameAudioCatalog(new AudioCatalogData { clips = new[] { Fixture("a") }, bindings = new[] { new AudioCueBinding { cue = "missing", clipIds = new[] { "b" } } } }).Validate().Length > 0, "Missing clip references were accepted.");
        Check(new GameAudioCatalog(new AudioCatalogData { schemaVersion = 2 }).Validate().Length > 0, "Unknown schema versions were accepted.");
        var variants = new GameAudioCatalog(new AudioCatalogData { clips = new[] { Fixture("a"), Fixture("b") }, bindings = new[] { new AudioCueBinding { cue = "rotate", clipIds = new[] { "a", "b" } } } });
        Check(variants.ResolveCue("rotate", 0).clip.id == "a" && variants.ResolveCue("rotate", 1).clip.id == "b" && variants.ResolveCue("rotate", 2).clip.id == "a", "Variation selection does not wrap deterministically.");
        Check(variants.ResolveCue("unknown") == null && variants.ResolveCard(null) == null, "Unknown audio silently selected an unrelated clip.");
    }
    public sealed class PcmMetrics
    {
        public int sampleRate, channels, bits;
        public long samples;
        public double duration, rms, peak, clippedFraction;
    }
    static PcmMetrics ReadWave(string path)
    {
        using (var stream = File.OpenRead(path)) using (var reader = new BinaryReader(stream))
        {
            Check(new string(reader.ReadChars(4)) == "RIFF", path + ": not a RIFF file.");
            reader.ReadUInt32(); Check(new string(reader.ReadChars(4)) == "WAVE", path + ": not a WAVE file.");
            int format = 0, channels = 0, rate = 0, bits = 0, block = 0; long dataPosition = 0, length = 0;
            while (stream.Position + 8 <= stream.Length)
            {
                string name = new string(reader.ReadChars(4)); uint size = reader.ReadUInt32(); long next = stream.Position + size;
                Check(next <= stream.Length, path + ": truncated RIFF chunk.");
                if (name == "fmt ")
                {
                    Check(size >= 16, path + ": truncated fmt chunk."); format = reader.ReadUInt16(); channels = reader.ReadUInt16(); rate = reader.ReadInt32(); reader.ReadInt32(); block = reader.ReadUInt16(); bits = reader.ReadUInt16();
                    if (format == 65534 && size >= 40) { reader.ReadUInt16(); reader.ReadUInt16(); reader.ReadUInt32(); format = reader.ReadUInt16(); }
                }
                if (name == "data") { dataPosition = stream.Position; length = size; }
                stream.Position = next + (size & 1);
            }
            Check(dataPosition > 0 && length > 0 && channels > 0 && rate > 0, path + ": missing PCM data.");
            Check((format == 1 && (bits == 8 || bits == 16 || bits == 24 || bits == 32)) || (format == 3 && bits == 32), path + ": WAV compression must be decoded by the native verifier.");
            Check(block == channels * (bits / 8), path + ": inconsistent sample alignment.");
            stream.Position = dataPosition; long count = length / (bits / 8), clipped = 0; double square = 0, peak = 0;
            for (long i = 0; i < count; i++)
            {
                double sample;
                if (format == 3) sample = reader.ReadSingle();
                else if (bits == 8) sample = (reader.ReadByte() - 128) / 128.0;
                else if (bits == 16) sample = reader.ReadInt16() / 32768.0;
                else if (bits == 24) { int n = reader.ReadByte() | reader.ReadByte() << 8 | reader.ReadByte() << 16; if ((n & 0x800000) != 0) n |= unchecked((int)0xff000000); sample = n / 8388608.0; }
                else sample = reader.ReadInt32() / 2147483648.0;
                if (double.IsNaN(sample) || double.IsInfinity(sample)) throw new InvalidDataException(path + ": non-finite sample.");
                double absolute = Math.Abs(sample); peak = Math.Max(peak, absolute); square += sample * sample; if (absolute >= .999) clipped++;
            }
            return new PcmMetrics { sampleRate = rate, channels = channels, bits = bits, samples = count, duration = (double)count / channels / rate, rms = Math.Sqrt(square / count), peak = peak, clippedFraction = (double)clipped / count };
        }
    }
}
