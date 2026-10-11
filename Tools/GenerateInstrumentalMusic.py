"""Compose the project's five vocal-free exploration/battle BGM scores.

These are new compositions, not Eternal Return MR files or vocal-separated OST.
NumPy renders the written score; FFmpeg encodes the shipped Ogg Vorbis files.
Install imageio-ffmpeg==0.6.0 (a local Temp/AudioTools install also works).
There are no sampled instruments, speech models, or external audio recordings.
"""
from __future__ import annotations

import hashlib
import json
import math
from pathlib import Path
import subprocess
import sys
import uuid

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "Temp/AudioTools"))
import imageio_ffmpeg

SR = 32000
OUTPUT = ROOT / "Assets/Resources/Lumia/Audio/Music"
SOURCE = "https://github.com/JSKS17/BlackTheSurvival/blob/main/Tools/GenerateInstrumentalMusic.py"
TAU = 2 * np.pi


def midi(note):
    return 440 * 2 ** ((note - 69) / 12)


def envelope(count, attack=.008, release=.10):
    env = np.ones(count)
    a, r = min(count // 2, int(attack * SR)), min(count // 2, int(release * SR))
    if a:
        env[:a] = np.sin(np.linspace(0, np.pi / 2, a)) ** 2
    if r:
        env[-r:] = np.cos(np.linspace(0, np.pi / 2, r)) ** 2
    return env


def instrument(note, seconds, voice, velocity=1):
    n = max(4, int(seconds * SR))
    t = np.arange(n) / SR
    f = midi(note)
    if voice == "keys":
        # Warm, struck electric piano: transient tines above a sustained body.
        y = np.sin(TAU * f * t) * np.exp(-t * 2.2)
        y += .28 * np.sin(TAU * f * 2 * t) * np.exp(-t * 5)
        y += .14 * np.sin(TAU * f * 3.003 * t) * np.exp(-t * 8)
        env = envelope(n, .004, .18)
    elif voice == "bell":
        y = np.sin(TAU * f * t) * np.exp(-t * 3.1)
        y += .33 * np.sin(TAU * f * 2.003 * t) * np.exp(-t * 6)
        y += .10 * np.sin(TAU * f * 3.99 * t) * np.exp(-t * 9)
        env = envelope(n, .003, .12)
    elif voice == "pluck":
        y = np.zeros(n)
        for h in range(1, 9):
            if f * h >= SR * .44:
                break
            y += np.sin(TAU * f * h * t + .08 * h) / h ** 1.4 * np.exp(-t * (2.3 + h * 1.7))
        env = envelope(n, .002, .12)
    elif voice == "bass":
        y = np.sin(TAU * f * t) + .30 * np.sin(TAU * f * 2 * t) + .10 * np.sin(TAU * f * 3 * t)
        y *= .8 + .2 * np.exp(-t * 14)
        env = envelope(n, .006, .07)
    elif voice == "pad":
        y = np.zeros(n)
        for h, amp in ((1, .6), (2, .18), (3, .08), (4, .025)):
            y += amp * (np.sin(TAU * f * .998 * h * t) + np.sin(TAU * f * 1.002 * h * t)) * .5
        y *= .94 + .06 * np.sin(TAU * .35 * t)
        env = envelope(n, .18, .28)
    elif voice == "lead":
        # Band-limited analog lead; slow vibrato is an instrument oscillator.
        phase = TAU * f * t + .022 * np.sin(TAU * 5.2 * t)
        y = np.zeros(n)
        for h in range(1, 10):
            if f * h >= SR * .40:
                break
            y += np.sin(phase * h) / h ** 1.65 * np.exp(-t * .25 * h)
        env = envelope(n, .015, .11)
    elif voice == "flute":
        phase = TAU * f * t + .019 * np.sin(TAU * 4.6 * t)
        y = np.sin(phase) + .11 * np.sin(phase * 2) + .035 * np.sin(phase * 3)
        env = envelope(n, .045, .15)
    elif voice == "strings":
        y = np.zeros(n)
        for h in range(1, 13):
            if f * h >= SR * .40:
                break
            y += (np.sin(TAU * f * h * .999 * t) + np.sin(TAU * f * h * 1.001 * t)) / h ** 1.8 * .5
        env = envelope(n, .025, .11)
    else:
        raise ValueError(voice)
    return (y * env * velocity).astype(np.float32)


def drum(kind, seed):
    rng = np.random.default_rng(seed)
    seconds = {"kick": .48, "snare": .24, "hat": .085, "open": .23, "rim": .12, "crash": 1.4}[kind]
    t = np.arange(int(seconds * SR)) / SR
    if kind == "kick":
        phase = TAU * (44 * t + (96 / 28) * (1 - np.exp(-28 * t)))
        y = np.sin(phase) * np.exp(-t * 9.5) + .08 * rng.standard_normal(len(t)) * np.exp(-t * 110)
    elif kind in ("hat", "open", "crash"):
        noise = rng.standard_normal(len(t))
        high = noise - np.convolve(noise, np.ones(9) / 9, mode="same")
        metallic = sum(np.sin(TAU * x * t) for x in (4151, 5573, 6341, 8273)) * .12
        decay = {"hat": 48, "open": 14, "crash": 3.6}[kind]
        y = (high * .42 + metallic) * np.exp(-t * decay)
    elif kind == "snare":
        noise = rng.standard_normal(len(t))
        noise = np.convolve(noise, np.ones(3) / 3, mode="same")
        y = noise * .62 * np.exp(-t * 19) + .25 * np.sin(TAU * 174 * t) * np.exp(-t * 28)
    else:
        y = (np.sin(TAU * 1180 * t) + .55 * np.sin(TAU * 1717 * t)) * np.exp(-t * 62)
    return (y * envelope(len(t), .001, .015)).astype(np.float32)


class Score:
    def __init__(self, tempo, bars, seed):
        self.tempo, self.bars, self.seed = tempo, bars, seed
        self.beat = 60 / tempo
        self.count = round(bars * 4 * self.beat * SR)
        self.mix = np.zeros((self.count, 2), np.float32)
        self.drums = {kind: drum(kind, seed + i * 101) for i, kind in enumerate(("kick", "snare", "hat", "open", "rim", "crash"))}

    def add(self, samples, beat, volume, pan=0):
        start = int(round(beat * self.beat * SR)) % self.count
        # Circular accumulation preserves instrument and reverb tails on looping.
        pos = np.arange(len(samples)) + start
        gains = (math.cos((pan + 1) * np.pi / 4), math.sin((pan + 1) * np.pi / 4))
        for channel in range(2):
            np.add.at(self.mix[:, channel], pos % self.count, samples * volume * gains[channel])

    def note(self, note, beat, length, voice, volume, pan=0):
        self.add(instrument(note, length * self.beat, voice), beat, volume, pan)

    def chord(self, notes, beat, length, voice, volume, width=.5):
        for i, note in enumerate(notes):
            self.note(note, beat, length, voice, volume, width * (2 * i / max(1, len(notes) - 1) - 1))

    def hit(self, kind, beat, volume, pan=0):
        self.add(self.drums[kind], beat, volume, pan)

    def finish(self):
        # Short stereo reflections wrap at the loop seam, rather than truncating.
        dry = self.mix.copy()
        for seconds, gain, swap in ((.113, .12, True), (.191, .085, False), (.293, .065, True), (.401, .045, False)):
            reflected = np.roll(dry[:, ::-1] if swap else dry, int(SR * seconds), axis=0)
            self.mix += reflected * gain
        self.mix -= np.mean(self.mix, axis=0)
        self.mix = np.tanh(self.mix * 1.05)
        peak = float(np.max(np.abs(self.mix)))
        rms = float(np.sqrt(np.mean(self.mix ** 2)))
        target_rms = .10 if self.tempo < 80 else .11 if self.tempo < 100 else .12 if self.tempo < 120 else .135 if self.tempo < 140 else .145
        self.mix *= min(.78 / max(peak, .00001), target_rms / max(rms, .00001))
        # The first/last 4 ms land exactly on zero, with no audible pause.
        seam = round(SR * .004)
        curve = np.sin(np.linspace(0, np.pi / 2, seam)) ** 2
        self.mix[:seam] *= curve[:, None]
        self.mix[-seam:] *= curve[::-1, None]
        return self.mix


def level(bar, bars):
    # Eight-bar intro, A, breakdown, B, and short resolution; not a four-bar loop.
    if bar < 8:
        return "intro", .62 + bar * .035
    if bars == 32:
        return ("A", .9) if bar < 16 else (("B", 1.0) if bar < 28 else ("outro", .72))
    if bar < 24:
        return "A", .96
    if bar < 32:
        return "break", .64
    if bar < bars - 8:
        return "B", 1.0
    return "outro", .84


def electronic_drums(s, bar, energy, busy=False, gentle=False):
    b = bar * 4
    for beat in (0, 2, 2.75) if busy else (0, 2):
        s.hit("kick", b + beat, (.18 if gentle else .30) * energy)
    for beat in (1, 3):
        s.hit("rim" if gentle else "snare", b + beat, (.12 if gentle else .21) * energy, -.08)
    for i in range(8):
        s.hit("hat", b + i * .5, (.029 if gentle else .045) * energy * (1 if i % 2 == 0 else .65), .28)
    if busy and bar % 4 == 3:
        for beat in (3.25, 3.5, 3.75):
            s.hit("snare", b + beat, .075 * energy)
    if not gentle and bar % 8 == 0 and bar:
        s.hit("crash", b, .07 * energy, -.25)


def afterglow():
    s = Score(96, 48, 101)
    chords = [(48, 55, 58, 62, 67), (44, 51, 55, 58, 63), (51, 58, 62, 65, 70), (46, 53, 58, 60, 65),
              (41, 48, 51, 55, 60), (44, 51, 55, 58, 62), (43, 50, 55, 58, 62), (43, 50, 53, 59, 62)]
    melody_a = [(0, 79, .75), (1, 77, .5), (1.75, 74, .75), (3, 72, .8)]
    melody_b = [(0, 79, .5), (.75, 82, .65), (1.5, 81, .5), (2.25, 77, .75), (3.25, 74, .55)]
    for bar in range(s.bars):
        part, energy = level(bar, s.bars)
        ch = chords[bar % 8]
        s.chord(ch[1:], bar * 4, 4.25, "pad", .048 * energy)
        for i, tone in enumerate((ch[2], ch[3], ch[4], ch[3], ch[1], ch[3], ch[2], ch[4])):
            s.note(tone + 12, bar * 4 + i * .5, .85, "bell", .030 * energy, -.45 if i % 2 else .45)
        if bar >= 4:
            for beat in (0, 1.75, 2.5):
                s.note(ch[0] - 12, bar * 4 + beat, .8, "bass", .16 * energy)
        if part not in ("intro", "break"):
            electronic_drums(s, bar, energy, gentle=True)
            if bar % 2 == 0:
                for beat, tone, length in melody_b if part == "B" else melody_a:
                    s.note(tone + (ch[0] - 48 if bar % 8 < 4 else 0), bar * 4 + beat, length, "flute", .075, -.10)
        elif part == "break":
            s.chord([tone + 12 for tone in ch[1:4]], bar * 4, 3.2, "keys", .045)
    return s, "Lumia Afterglow", "Exploration / preparation / encounters", "C minor; 48 bars; bell arpeggio, warm electric piano, pad, flute lead, bass and restrained drums. Intro / A / breakdown / B / resolution."


def emberlight():
    s = Score(78, 32, 203)
    chords = [(50, 57, 61, 64, 69), (47, 54, 57, 61, 66), (43, 50, 54, 57, 62), (45, 52, 57, 59, 64)]
    melodies = [((0, 78, 1.2), (1.5, 76, .8), (2.75, 73, 1.1)), ((.5, 74, .7), (1.5, 73, .7), (2.5, 69, 1.2)),
                ((0, 71, 1.3), (1.5, 74, .8), (3, 78, .8)), ((.5, 76, .8), (1.75, 73, .8), (3, 69, .8))]
    for bar in range(s.bars):
        part, energy = level(bar, s.bars)
        ch = chords[(bar // 2) % 4]
        for i, tone in enumerate((ch[0], ch[2], ch[1], ch[3], ch[2], ch[4], ch[3], ch[1])):
            s.note(tone + 12, bar * 4 + i * .5, 1.2, "pluck", .10 * energy, (-.32 if i % 2 else .32))
        s.chord(ch[1:4], bar * 4, 4.4, "pad", .035 * energy)
        s.note(ch[0] - 12, bar * 4, 3.6, "bass", .11 * energy)
        if bar >= 8 and part != "outro":
            for beat, tone, length in melodies[(bar // 2) % 4]:
                s.note(tone + (12 if part == "B" and bar % 4 == 3 else 0), bar * 4 + beat, length, "flute", .075 * energy)
        if part == "B":
            s.chord(ch[2:], bar * 4 + 1, 1.8, "keys", .028)
        if bar >= 8:
            for beat in (1, 3):
                s.hit("rim", bar * 4 + beat, .032, -.25)
            for i in range(4):
                s.hit("hat", bar * 4 + i + .5, .018, .35)
    return s, "Emberlight", "Campfire / escape", "D major; 32 bars; finger-picked plucks, flute and electric piano over warm pads and sparse hand-percussion-style rhythm. Intro / A / octave B / resolution."


def exchange():
    s = Score(110, 48, 307)
    chords = [(45, 52, 55, 59, 64), (41, 48, 52, 55, 60), (48, 55, 59, 62, 67), (43, 50, 53, 57, 62)]
    motif = [(0, 76, .4), (.75, 79, .4), (1.5, 81, .45), (2.5, 76, .4), (3.25, 74, .5)]
    for bar in range(s.bars):
        part, energy = level(bar, s.bars)
        ch = chords[bar % 4]
        for beat in (.5, 1.5, 2.5, 3.5):
            s.chord(ch[1:], bar * 4 + beat, .6, "keys", .047 * energy)
        for beat, tone in ((0, ch[0] - 12), (.75, ch[0]), (1.5, ch[1] - 12), (2, ch[0] - 12), (2.75, ch[2] - 12), (3.5, ch[1] - 12)):
            s.note(tone, bar * 4 + beat, .44, "bass", .17 * energy)
        if part != "break":
            electronic_drums(s, bar, energy * .8, gentle=True)
        if bar >= 8 and part not in ("break", "outro"):
            for beat, tone, length in motif:
                shift = (ch[0] - 45) if bar % 4 in (0, 1) else -3
                s.note(tone + shift, bar * 4 + beat, length + .25, "bell", .075, -.18)
            if part == "B":
                for beat, tone in ((.25, ch[4]), (1.25, ch[3]), (2.25, ch[2]), (3.25, ch[4])):
                    s.note(tone + 12, bar * 4 + beat, .45, "pluck", .032, .45)
        else:
            s.chord(ch[1:], bar * 4, 4.2, "pad", .035)
    return s, "Signal Exchange", "Kiosk", "A minor; 48 bars; syncopated electric piano, mallets, plucks, moving bass and light rhythm. Intro / A / filtered-feel breakdown / counter-melody B / resolution."


def combat():
    s = Score(128, 64, 409)
    chords = [(40, 47, 52, 55, 59), (36, 43, 48, 52, 55), (43, 50, 55, 59, 62), (38, 45, 50, 54, 57),
              (40, 47, 52, 55, 59), (36, 43, 48, 52, 55), (45, 52, 57, 60, 64), (35, 42, 47, 51, 54)]
    motif_a = [(0, 76, .7), (1, 79, .45), (1.75, 78, .6), (2.75, 74, .5), (3.5, 71, .4)]
    motif_b = [(0, 83, .45), (.75, 81, .5), (1.5, 79, .7), (2.5, 78, .4), (3.25, 76, .65)]
    for bar in range(s.bars):
        part, energy = level(bar, s.bars)
        ch = chords[bar % 8]
        s.chord(ch[1:], bar * 4, 4.2, "pad", .036 * energy)
        for i in range(8):
            tone = ch[0] - 12 if i % 4 != 3 else ch[1] - 12
            s.note(tone, bar * 4 + i * .5, .42, "bass", .17 * energy)
            s.note(ch[1 + i % 4] + 12, bar * 4 + i * .5, .38, "pluck", .036 * energy, .36 if i % 2 else -.36)
        if part != "break":
            electronic_drums(s, bar, energy, busy=part in ("A", "B"))
        if bar >= 8 and part not in ("break", "outro"):
            for beat, tone, length in motif_b if part == "B" else motif_a:
                s.note(tone + (ch[0] - 40 if bar % 8 < 4 else 0), bar * 4 + beat, length, "lead", .072)
            if part == "B":
                s.chord([ch[1] + 12, ch[2] + 12], bar * 4 + 1.5, .6, "strings", .034, .5)
                s.chord([ch[2] + 12, ch[3] + 12], bar * 4 + 3.5, .6, "strings", .034, .5)
        elif part == "break":
            s.chord([x + 12 for x in ch[1:4]], bar * 4, 3.7, "keys", .054)
            s.hit("kick", bar * 4, .15)
    return s, "Break the Loop", "Wildlife / subject combat", "E minor; 64 bars; analog lead melody, arpeggiated plucks, sustained pad, staccato strings, bass and full electronic drums. Eight-bar intro / A / breakdown / extended B / resolution."


def overdrive():
    s = Score(144, 64, 503)
    chords = [(38, 45, 50, 53, 57), (34, 41, 46, 50, 53), (41, 48, 53, 57, 60), (36, 43, 48, 52, 55),
              (38, 45, 50, 53, 57), (34, 41, 46, 50, 53), (43, 50, 55, 58, 62), (33, 40, 45, 49, 52)]
    melody_a = [(0, 74, .65), (1, 77, .5), (1.75, 81, .7), (2.75, 79, .4), (3.5, 77, .45)]
    melody_b = [(0, 86, .7), (1, 84, .5), (1.75, 81, .5), (2.5, 79, .5), (3.25, 81, .6)]
    for bar in range(s.bars):
        part, energy = level(bar, s.bars)
        ch = chords[bar % 8]
        s.chord(ch[1:], bar * 4, 4.2, "pad", .044 * energy)
        for i in range(8):
            s.note(ch[0] - 12 + (12 if i % 4 == 3 else 0), bar * 4 + i * .5, .42, "bass", .18 * energy)
            s.chord([ch[1] + 12, ch[2] + 12], bar * 4 + i * .5, .36, "strings", .033 * energy, .45)
        if part != "break":
            electronic_drums(s, bar, energy, busy=True)
            if part == "B" and bar % 2:
                s.hit("open", bar * 4 + 3.5, .045, .3)
        else:
            s.hit("kick", bar * 4, .17)
            s.chord([x + 12 for x in ch[1:4]], bar * 4 + 1.5, 2, "keys", .05)
        if bar >= 8 and part not in ("break", "outro"):
            for beat, tone, length in melody_b if part == "B" else melody_a:
                shift = 0 if bar % 8 in (0, 4, 6, 7) else (ch[0] - 38)
                s.note(tone + shift, bar * 4 + beat, length, "lead", .079)
                if part == "B":
                    s.note(tone + shift - 12, bar * 4 + beat, length, "strings", .032, -.2)
        if bar % 8 == 7 and part != "break":
            for i in range(4):
                s.note(ch[3] + 12 + i * 2, bar * 4 + 3 + i * .25, .22, "bell", .045, .2)
    return s, "VF Overdrive", "Boss combat", "D minor; 64 bars; driving low bass, staccato string ostinato, layered analog lead and brighter second-theme octave, full drums and fills. Intro / A / halftime breakdown / B / resolution."


def meta(path):
    target = Path(str(path) + ".meta")
    if target.exists():
        return
    guid = uuid.uuid5(uuid.NAMESPACE_URL, "BlackTheSurvival:" + path.relative_to(ROOT).as_posix()).hex
    target.write_text(f"""fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 7
  defaultSettings:
    serializedVersion: 2
    loadType: 2
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 0.75
    conversionMode: 0
    preloadAudioData: 0
  platformSettingOverrides: {{}}
  forceToMono: 0
  normalize: 0
  ambisonic: 0
  loadInBackground: 1
  userData: Project-composed instrumental music. No vocal sample or original OST audio.
  assetBundleName:
  assetBundleVariant:
""", encoding="utf8")


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    records = []
    for identity, compose in (("music_lumia_afterglow", afterglow), ("music_emberlight", emberlight), ("music_signal_exchange", exchange), ("music_break_the_loop", combat), ("music_vf_overdrive", overdrive)):
        score, title, scenes, arrangement = compose()
        samples = score.finish()
        path = OUTPUT / (identity + ".ogg")
        ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
        encoded = subprocess.run([ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-f", "f32le", "-ar", str(SR), "-ac", "2", "-i", "pipe:0",
                                  "-fflags", "+bitexact", "-flags:a", "+bitexact", "-map_metadata", "-1", "-c:a", "libvorbis", "-q:a", "5", str(path)],
                                 input=samples.astype("<f4").tobytes(), stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        if encoded.returncode:
            raise RuntimeError(encoded.stderr.decode("utf8", errors="replace"))
        # Reopen the actual shipped compressed file, not just the mix buffer.
        restored = subprocess.run([ffmpeg, "-hide_banner", "-loglevel", "error", "-i", str(path), "-f", "f32le", "-ar", str(SR), "-ac", "2", "pipe:1"],
                                  stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        if restored.returncode:
            raise RuntimeError(restored.stderr.decode("utf8", errors="replace"))
        decoded = np.frombuffer(restored.stdout, dtype="<f4").reshape(-1, 2)
        peak = float(np.max(np.abs(decoded)))
        rms = float(np.sqrt(np.mean(decoded ** 2)))
        assert len(decoded) == len(samples)
        assert .015 < rms < .25 and peak < .95, (identity, rms, peak)
        assert abs(decoded[0]).max() < .005 and abs(decoded[-1]).max() < .005
        meta(path)
        data = path.read_bytes()
        entry = dict(id=identity, title=title, performer="Black The Survival project composition", path="Lumia/Audio/Music/" + identity,
                     file=path.relative_to(ROOT).as_posix(), sourcePage=SOURCE, sourceUrl=SOURCE, sourceName="GenerateInstrumentalMusic.py: " + title,
                     origin="instrumental_generated", kind="instrumental_generated", license="Project-composed fan-game music; not an original Eternal Return recording or MR.",
                     channel="music", volume=.67, containsVocals=False, loop=True, sceneUse=scenes, arrangement=arrangement,
                     alteration="New written score rendered with deterministic DSP instruments. No existing recording, vocal separation, voice model, or music sample is used.",
                     durationSeconds=round(len(decoded) / SR, 6), sampleRate=SR, channels=2, tempo=score.tempo, bars=score.bars,
                     peak=round(peak, 8), rms=round(rms, 8), dcOffset=round(float(np.mean(decoded)), 9), loopBoundaryJump=round(float(np.max(np.abs(decoded[0] - decoded[-1]))), 9),
                     encoder="imageio-ffmpeg " + imageio_ffmpeg.__version__ + "; FFmpeg libvorbis quality 5; bitexact encoding and metadata stripped",
                     bytes=len(data), sha256=hashlib.sha256(data).hexdigest())
        records.append(entry)
        print(identity, entry["durationSeconds"], "seconds /", len(data), "bytes / peak", entry["peak"], "RMS", entry["rms"], flush=True)
    manifest = ROOT / "docs/audio/music-sources.json"
    prior = json.loads(manifest.read_text(encoding="utf8")) if manifest.exists() else []
    original = [row for row in prior if row.get("origin") != "instrumental_generated"]
    manifest.write_text(json.dumps(original + records, ensure_ascii=False, indent=2) + "\n", encoding="utf8")


if __name__ == "__main__":
    main()
