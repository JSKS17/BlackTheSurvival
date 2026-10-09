"""Create the project's supplemental fan-designed effects, never original ER audio.

The official fan kit supplies character voices but does not supply the combat,
animal or interface effects used here. These deterministic, project-authored
effects are clearly distinguished in the catalog and provenance manifest.

Requires Python 3.10+ and NumPy. No network, external samples or model service.
Run without arguments to regenerate; --check verifies the committed WAV bytes.
"""
from __future__ import annotations

import argparse
import hashlib
import io
import json
import math
import pathlib
import uuid
import wave

import numpy as np

ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/Resources/Lumia/Audio/Designed"
MANIFEST = ROOT / "docs/audio/designed-sources.json"
RATE = 44100
NAMESPACE = uuid.UUID("3544f537-d281-4dd2-965e-804395ebf4a9")
SOURCE = "generated:Tools/GenerateSupplementalAudio.py"


def count(duration):
    return max(2, int(round(float(duration) * RATE)))


def timeline(duration):
    return np.arange(count(duration), dtype=np.float64) / RATE


def rng(name):
    seed = int.from_bytes(hashlib.sha256(name.encode("utf-8")).digest()[:8], "little")
    return np.random.default_rng(seed)


def envelope(length, attack=.006, release=.025, decay=0):
    n = length if isinstance(length, int) else count(length)
    out = np.ones(n, dtype=np.float64)
    a, r = min(n, count(attack)), min(n, count(release))
    out[:a] *= np.sin(np.linspace(0, math.pi / 2, a)) ** 2
    out[-r:] *= np.cos(np.linspace(0, math.pi / 2, r)) ** 2
    if decay:
        out *= np.exp(-np.linspace(0, decay, n))
    return out


def smooth_band(signal, low=0, high=10000):
    # Smooth FFT shelves give each texture its own frequency range. Unlike a
    # clipped square wave this does not create harsh, unbounded high harmonics.
    frequencies = np.fft.rfftfreq(len(signal), 1 / RATE)
    lo = np.ones_like(frequencies) if low <= 0 else 1 / (1 + (low / np.maximum(frequencies, 1)) ** 8)
    hi = 1 / (1 + (frequencies / max(1, high)) ** 12)
    lo[0] = 0
    return np.fft.irfft(np.fft.rfft(signal) * lo * hi, len(signal))


def noise(duration, name, low=0, high=9000):
    signal = smooth_band(rng(name).normal(0, 1, count(duration)), low, high)
    return signal / max(1e-9, np.sqrt(np.mean(signal * signal)))


def tone(frequency, duration, gain=1, decay=4, attack=.004, harmonics=(1, .16, .05), vibrato=0):
    t = timeline(duration)
    f = np.asarray(frequency, dtype=np.float64)
    if f.ndim == 0:
        f = np.full(len(t), float(f))
    if vibrato:
        f = f * (1 + vibrato * np.sin(2 * math.pi * 5.2 * t))
    phase = np.cumsum(f) * (2 * math.pi / RATE)
    signal = np.zeros(len(t))
    for harmonic, amplitude in enumerate(harmonics, 1):
        signal += amplitude * np.sin(phase * harmonic)
    return signal * envelope(len(t), attack, .03, decay) * gain


def sweep(start, end, duration, name="sweep", gain=1, airy=.3, low=500, high=6500):
    t = timeline(duration)
    p = np.linspace(0, 1, len(t))
    frequency = start * (end / start) ** p
    phase = np.cumsum(frequency) * (2 * math.pi / RATE)
    body = np.sin(phase) + .13 * np.sin(phase * 2.01)
    air = noise(duration, name, low, high) * airy
    return (body * .35 + air) * np.sin(math.pi * p) ** .8 * gain


def room(signal, amount=.12):
    out = signal.copy()
    for seconds, gain in ((.023, amount), (.047, amount * .67), (.083, amount * .33)):
        delay = count(seconds)
        if delay < len(signal):
            out[delay:] += signal[:-delay] * gain
    return out


def add(buffer, sound, at=0, gain=1):
    offset = max(0, int(round(at * RATE)))
    length = min(len(sound), len(buffer) - offset)
    if length > 0:
        buffer[offset:offset + length] += sound[:length] * gain
    return buffer


def blank(duration):
    return np.zeros(count(duration), dtype=np.float64)


def impact(duration, name, low=80, bright=2500, weight=1):
    t = timeline(duration)
    bass = tone(low * np.exp(-3 * t) + low * .3, duration, decay=8, harmonics=(1, .22, .08))
    crunch = noise(duration, name, 300, bright) * envelope(len(t), .001, .015, 13)
    thud = noise(duration, name + " thud", 30, 330) * envelope(len(t), .001, .02, 9)
    return room((bass * .55 + crunch * .4 + thud * .5) * weight, .1)


def metal(duration, frequency, name, body=.8):
    t = timeline(duration)
    out = np.zeros(len(t))
    # Inharmonic ringing modes, with a short high-frequency impact, make metal
    # and tools sound distinct from the soft UI mallet tones.
    for multiple, gain, damping in ((1, .7, 5), (1.414, .27, 7), (2.15, .2, 9), (3.72, .1, 13)):
        out += np.sin(2 * math.pi * frequency * multiple * t) * np.exp(-damping * t / duration) * gain
    out *= envelope(len(t), .001, .025)
    out += noise(duration, name, 1800, 8000) * envelope(len(t), .001, .02, 24) * .15
    return room(out * body, .13)


def chime(notes, duration, interval=.1, name="chime"):
    out = blank(duration)
    for index, frequency in enumerate(notes):
        note_duration = max(.08, duration - index * interval)
        add(out, tone(frequency, note_duration, decay=6, harmonics=(1, .06, .015)), index * interval)
        add(out, metal(min(.25, note_duration), frequency * 2, name + str(index), .14), index * interval)
    return room(out, .11)


def whoosh(duration, name, center=1900, strength=1):
    p = np.linspace(0, 1, count(duration))
    air = noise(duration, name, max(80, center / 3), min(10000, center * 3))
    body = noise(duration, name + " body", 80, 650)
    return (air * .7 + body * .28) * np.sin(math.pi * p) ** 1.4 * strength


def sparkles(duration, name, total=10, descending=False):
    out = blank(duration)
    random = rng(name)
    for index in range(total):
        at = random.uniform(0, duration * .85)
        frequency = random.uniform(1700, 5200) * (1 - .45 * at / duration if descending else 1)
        add(out, tone(frequency, min(.16, duration - at), decay=9, harmonics=(1, .08)), at, random.uniform(.08, .2))
    return out


def match_found():
    out = blank(1.45)
    add(out, whoosh(.37, "match portal", 1300), 0, .65)
    add(out, chime([659.255, 879.999, 1318.51], 1.1, .14, "match motif"), .18, .8)
    add(out, tone(329.63, .72, decay=5), .47, .2)
    add(out, sparkles(.7, "match glints", 8), .48)
    return out


def ui_click():
    out = metal(.11, 1250, "ui glass", .2)
    out += noise(.11, "ui contact", 700, 4200) * envelope(len(out), .001, .015, 24) * .26
    return out


def ui_reroll():
    out = blank(.45)
    for index in range(5):
        add(out, noise(.075, "shuffle " + str(index), 1200, 6500) * envelope(.075, .004, .02, 5), index * .055, .13)
    add(out, chime([587.33, 739.99, 880], .25, .05, "shuffle finish"), .19, .35)
    return out


def travel():
    out = blank(.65)
    for index, at in enumerate((.01, .19, .37)):
        add(out, impact(.16, "map step " + str(index), 110, 1500), at, .3)
        add(out, noise(.11, "map grit " + str(index), 1100, 6500) * envelope(.11, .005, .05, 3), at + .02, .1)
    add(out, whoosh(.45, "map passing air", 1500), .15, .23)
    return out


def combat_start(boss=False):
    out = blank(1.3 if boss else .8)
    add(out, whoosh(.38, "boss approach" if boss else "battle approach", 1100), 0, .6)
    add(out, impact(.8 if boss else .42, "boss impact" if boss else "battle impact", 52 if boss else 92, 3300), .16, .85)
    add(out, metal(.75 if boss else .4, 184 if boss else 294, "battle gong"), .17, .3)
    if boss:
        add(out, tone(np.linspace(100, 52, count(.8)), .8, decay=3, harmonics=(1, .25, .08)), .3, .35)
        add(out, noise(.8, "boss darkness", 120, 1400) * envelope(.8, .12, .22, 2), .34, .22)
    return out


def register():
    out = blank(.8)
    add(out, metal(.2, 1850, "register contact"), 0, .25)
    add(out, chime([880, 1174.66, 1760], .55, .1, "register confirmed"), .08, .75)
    add(out, noise(.12, "credit paper", 1700, 6500) * envelope(.12, .003, .035, 5), .45, .15)
    return out


def fire_loop():
    duration = 3.3
    out = noise(duration, "camp warm bed", 140, 5500) * .11
    t = timeline(duration)
    out *= .72 + .18 * np.sin(2 * math.pi * 1.4 * t) + .1 * np.sin(2 * math.pi * 3.9 * t)
    random = rng("camp crackles")
    for index in range(28):
        at = random.uniform(.02, duration - .12)
        snap = noise(.055, "fire snap " + str(index), 800, 6500) * envelope(.055, .001, .02, 15)
        add(out, snap, at, random.uniform(.03, .14))
    # Start on the old tail and blend to the old head; the final sample then
    # meets the next sample from that same tail, giving a continuous loop.
    overlap = count(.12)
    ramp = np.sin(np.linspace(0, math.pi / 2, overlap)) ** 2
    overlap_signal = out[-overlap:] * (1 - ramp) + out[:overlap] * ramp
    return np.concatenate((overlap_signal, out[overlap:-overlap]))


def camp_enter():
    out = blank(.8)
    add(out, impact(.24, "camp firewood down", 165, 1800), 0, .4)
    add(out, noise(.6, "camp first crackle", 450, 6000) * envelope(.6, .07, .2, 1), .12, .15)
    for index, at in enumerate((.18, .3, .43)):
        add(out, noise(.04, "camp arrival snap " + str(index), 1700, 6500) * envelope(.04, .001, .015, 8), at, .12)
    return out


def craft_metal():
    out = blank(1.05)
    for index, (at, frequency) in enumerate(((0, 590), (.24, 710), (.49, 850))):
        add(out, metal(.45, frequency, "craft strike " + str(index)), at, .7 - index * .1)
        add(out, impact(.12, "craft anvil " + str(index), 150, 2700), at, .2)
    return out


def cook_sizzle():
    duration = 1.3
    t = timeline(duration)
    out = noise(duration, "frying oil", 650, 8000) * envelope(duration, .06, .22) * .2
    out *= .75 + .14 * np.sin(2 * math.pi * 13.2 * t) + .11 * np.sin(2 * math.pi * 29 * t)
    random = rng("frying bubbles")
    for index in range(24):
        at = random.uniform(.03, 1.08)
        pop = tone(random.uniform(380, 900), .035, decay=11, harmonics=(1, .2))
        add(out, pop, at, random.uniform(.04, .15))
    add(out, metal(.2, 1700, "spoon on pan"), 1.04, .18)
    return out


def eat():
    out = blank(.75)
    for index, at in enumerate((.03, .16, .29)):
        crunch = noise(.12, "food crunch " + str(index), 450, 5200) * envelope(.12, .002, .04, 8)
        add(out, crunch, at, .18 - .025 * index)
    add(out, tone(np.linspace(230, 100, count(.2)), .2, decay=8, harmonics=(1, .2)), .36, .16)
    add(out, chime([659.25, 987.77], .3, .08, "food recovery"), .43, .24)
    return out


def slash():
    out = blank(.55)
    add(out, whoosh(.22, "blade edge", 3800), 0, .65)
    add(out, metal(.36, 1450, "blade contact", .5), .11, .4)
    add(out, impact(.22, "blade body", 155, 3300), .12, .28)
    return out


def projectile():
    out = blank(.48)
    add(out, impact(.12, "projectile release", 230, 5200), 0, .6)
    add(out, sweep(1600, 430, .29, "projectile flight", .7, .36, 900, 7500), .025)
    add(out, metal(.2, 1900, "projectile casing", .25), .07)
    return out


def explosion():
    out = blank(.95)
    add(out, impact(.6, "explosion body", 63, 6500), 0, 1.1)
    air = noise(.85, "explosion dust", 130, 4700) * envelope(.85, .012, .2, 4)
    add(out, air, .04, .3)
    for index, at in enumerate((.13, .22, .34)):
        add(out, metal(.2, 730 + 310 * index, "explosion debris " + str(index), .2), at)
    return room(out, .11)


def lightning():
    out = blank(.72)
    for index, (at, duration) in enumerate(((0, .07), (.09, .11), (.23, .17))):
        zap = noise(duration, "arc snap " + str(index), 1200, 9000) * envelope(duration, .001, .012, 5)
        add(out, zap, at, .7 - index * .1)
        add(out, sweep(2200, 180, duration, "electric pitch " + str(index), .4, .15), at)
    add(out, impact(.35, "lightning thunder", 75, 1300), .26, .25)
    return room(out, .18)


def flame():
    out = blank(.8)
    add(out, whoosh(.65, "flame jet", 2200), 0, .85)
    add(out, impact(.23, "flame ignition", 110, 3000), 0, .4)
    t = timeline(.65)
    hiss = noise(.65, "flame oxygen", 350, 7200) * envelope(.65, .025, .12, 1)
    add(out, hiss * (.7 + .3 * np.sin(2 * math.pi * 22 * t)), .05, .2)
    return out


def ice():
    out = blank(.85)
    add(out, noise(.23, "ice shatter", 1800, 9500) * envelope(.23, .001, .05, 7), 0, .32)
    add(out, metal(.65, 1750, "ice crystal", .45), .015)
    add(out, metal(.53, 2600, "ice small crystal", .3), .06)
    add(out, sparkles(.65, "ice fragments", 15, True), .1)
    return room(out, .17)


def arcade():
    # A short mallet arpeggio has the playful arcade identity without the old
    # all-purpose square-wave beep. The quiet bit-grain is band limited.
    out = chime([523.25, 783.99, 1046.5, 1567.98], .62, .075, "arcade block")
    add(out, metal(.26, 460, "block landing", .25), .29)
    add(out, noise(.16, "arcade grain", 2200, 8000) * envelope(.16, .002, .02, 9), .01, .055)
    return out


def shield():
    out = blank(.68)
    add(out, metal(.58, 540, "shield rim", .6), 0)
    add(out, tone(270, .6, decay=3, harmonics=(1, .08)), .02, .35)
    add(out, sweep(270, 1080, .35, "shield bloom", .4, .12), .06)
    return room(out, .12)


def heal():
    out = chime([523.25, 659.25, 987.77], .9, .11, "heal light") * .6
    add(out, sweep(400, 1400, .65, "healing breath", .3, .18), .03)
    add(out, sparkles(.65, "healing motes", 8), .18, .6)
    return out


def dash():
    out = blank(.44)
    add(out, whoosh(.34, "dash air", 2800), .01, .85)
    add(out, impact(.13, "dash push", 130, 1400), 0, .25)
    add(out, sweep(1500, 400, .27, "dash motion", .15, .13), .02)
    return out


def trap():
    out = blank(.65)
    add(out, metal(.29, 780, "trap latch", .5), 0)
    add(out, metal(.37, 1280, "trap spring", .35), .11)
    add(out, noise(.18, "trap ratchet", 1000, 6000) * envelope(.18, .002, .03, 5), .07, .17)
    add(out, tone(np.linspace(760, 180, count(.25)), .25, decay=7, harmonics=(1, .2)), .1, .2)
    return out


def arcane():
    out = blank(.85)
    add(out, sweep(210, 760, .6, "vf energy", .65, .2, 200, 3800), 0)
    add(out, tone(392, .7, decay=4, harmonics=(1, .13, .07), vibrato=.012), .12, .25)
    add(out, tone(587.33, .6, decay=5, harmonics=(1, .08)), .2, .2)
    add(out, sparkles(.55, "vf dust", 7), .25, .6)
    return room(out, .18)


def multi():
    out = blank(.75)
    for index, at in enumerate((0, .13, .28)):
        add(out, whoosh(.16, "combo " + str(index), 2400 + 600 * index), at, .5)
        add(out, impact(.19, "combo hit " + str(index), 170 - 20 * index, 3800), at + .05, .4)
        add(out, metal(.2, 900 + 240 * index, "combo ring " + str(index), .16), at + .05)
    return out


def poison():
    out = blank(.8)
    add(out, noise(.58, "poison spray", 900, 6500) * envelope(.58, .02, .18, 2), 0, .23)
    random = rng("poison bubbles")
    for index in range(9):
        at = random.uniform(.04, .6)
        frequency = random.uniform(170, 520)
        add(out, tone(np.linspace(frequency * 1.7, frequency, count(.12)), .12, decay=9, harmonics=(1, .35)), at, .24)
    return room(out, .12)


def water():
    out = blank(.95)
    splash = noise(.48, "water broad splash", 300, 7500) * envelope(.48, .008, .2, 2)
    add(out, splash, .015, .38)
    add(out, impact(.25, "water heavy body", 145, 900), 0, .23)
    random = rng("water droplets")
    for index in range(18):
        at = random.uniform(.1, .73)
        duration = random.uniform(.045, .1)
        frequency = random.uniform(550, 1700)
        drop = tone(np.linspace(frequency * 1.8, frequency, count(duration)), duration, decay=9, harmonics=(1, .16))
        add(out, drop, at, random.uniform(.045, .12))
    add(out, noise(.5, "water fine mist", 1800, 9000) * envelope(.5, .025, .2, 2), .24, .065)
    return room(out, .08)


def bowed_music():
    out = blank(1.12)
    for index, (frequency, at, duration) in enumerate(((587.33, .01, .57), (739.99, .27, .58), (880, .55, .52))):
        t = timeline(duration)
        f = frequency * (1 + .0065 * np.sin(2 * math.pi * 5.6 * t))
        phase = np.cumsum(f) * (2 * math.pi / RATE)
        note = np.zeros(len(t))
        for harmonic in range(1, int(8200 / frequency) + 1):
            hz = harmonic * frequency
            body = .75 + .4 * math.exp(-.5 * ((hz - 1100) / 350) ** 2) + .3 * math.exp(-.5 * ((hz - 2600) / 650) ** 2)
            note += np.sin(phase * harmonic) * body / harmonic ** 1.35
        note += noise(duration, "bow friction " + str(index), 1800, 7500) * .018
        note *= envelope(duration, .065, .13, 1.3)
        add(out, note, at, .42)
    return room(out, .15)


def weapon_gun():
    out = blank(.52)
    add(out, noise(.035, "gun powder", 800, 9200) * envelope(.035, .0005, .012, 4), 0, .75)
    add(out, impact(.32, "gun chamber", 82, 4600), .001, .7)
    add(out, metal(.24, 1850, "gun mechanism", .16), .055)
    return room(out, .2)


def weapon_bow():
    out = blank(.62)
    add(out, tone(np.linspace(310, 190, count(.3)), .3, decay=8, harmonics=(1, .32, .2, .1)), 0, .55)
    add(out, whoosh(.33, "arrow flight", 3600), .025, .48)
    add(out, impact(.15, "arrow target", 185, 4100), .26, .25)
    return out


def guitar():
    out = blank(.95)
    for index, frequency in enumerate((164.81, 246.94, 329.63, 493.88)):
        add(out, tone(frequency, .85, decay=6, harmonics=(1, .45, .24, .12, .06)), .017 * index, .3)
    add(out, noise(.07, "guitar pick", 1400, 6500) * envelope(.07, .001, .02, 12), 0, .08)
    return room(out, .14)


def vocal(duration, name, fundamental, formants, roughness=.12, breath=.08, growl=0):
    t = timeline(duration)
    f = np.asarray(fundamental, dtype=np.float64)
    if f.ndim == 0:
        f = np.full(len(t), float(f))
    # Smooth jitter and inharmonic throat modulation keep these creature calls
    # away from a clean musical synthesizer tone. They are authored imitations,
    # not recordings or original Eternal Return wildlife samples.
    jitter = 1 + .018 * np.sin(2 * math.pi * 7.3 * t) + .009 * np.sin(2 * math.pi * 19.2 * t)
    phase = np.cumsum(f * jitter) * (2 * math.pi / RATE)
    out = np.zeros(len(t))
    average = float(np.mean(f))
    for harmonic in range(1, min(55, int(9500 / max(1, average))) + 1):
        hz = harmonic * average
        weight = .12 / harmonic ** .8
        for center, width, gain in formants:
            weight += math.exp(-.5 * ((hz - center) / width) ** 2) * gain / math.sqrt(harmonic)
        out += np.sin(phase * harmonic + .035 * harmonic * np.sin(2 * math.pi * 23 * t)) * weight
    out *= 1 + roughness * np.sin(2 * math.pi * 31 * t)
    if growl:
        out *= .7 + .3 * np.sin(phase * .5 + 2 * np.sin(2 * math.pi * 8 * t))
    out += noise(duration, name + " breath", 180, 7000) * breath
    return smooth_band(out, 35, 9200) * envelope(duration, .012, .08, 1.2)


def chicken(attack=False):
    out = blank(.85 if attack else 1.15)
    calls = ((.01, .11, 760), (.18, .11, 700), (.35, .26, 920)) if attack else ((.03, .13, 620), (.26, .16, 570), (.53, .36, 840))
    for index, (at, duration, frequency) in enumerate(calls):
        f = np.linspace(frequency * 1.3, frequency * .62, count(duration))
        call = vocal(duration, "chicken " + str(index), f, ((1700, 500, 1), (3300, 700, .45)), .2, .08)
        add(out, call, at, .65)
    if attack:
        for index, at in enumerate((.06, .19, .31)):
            add(out, whoosh(.09, "chicken flap " + str(index), 2500), at, .25)
    return out


def dog(attack=False):
    out = blank(.88 if attack else 1.1)
    for index, at in enumerate((.02, .29) if attack else (.03, .47)):
        duration = .24 if attack else .29
        f = np.linspace(185 if attack else 155, 82, count(duration))
        bark = vocal(duration, "dog bark " + str(index), f, ((570, 250, 1), (1350, 450, .7), (2800, 900, .2)), .4, .14, 1)
        bark *= envelope(duration, .006, .08, 2)
        add(out, bark, at, .8)
    if attack:
        add(out, vocal(.48, "dog growl", 88, ((350, 160, 1), (900, 350, .7)), .6, .1, 1), .34, .4)
    return out


def wolf(attack=False):
    duration = .98 if attack else 1.6
    out = blank(duration)
    if attack:
        f = np.linspace(170, 92, count(.8))
        add(out, vocal(.8, "wolf snarl", f, ((440, 230, 1), (1150, 470, .65)), .6, .2, 1), 0, .9)
        add(out, noise(.16, "wolf teeth", 1300, 5600) * envelope(.16, .004, .06, 4), .02, .13)
    else:
        t = timeline(1.4)
        f = 325 + 130 * np.sin(np.linspace(0, math.pi, len(t))) + 7 * np.sin(2 * math.pi * 4.2 * t)
        call = vocal(1.4, "wolf howl", f, ((670, 200, 1), (1100, 350, .3)), .06, .025)
        call *= np.sin(np.linspace(0, math.pi, len(t))) ** .8
        add(out, call, .04, .75)
        add(out, vocal(.42, "wolf low ending", 110, ((390, 200, 1),), .3, .04, 1), 1.09, .2)
    return room(out, .16)


def boar(attack=False):
    out = blank(.95 if attack else 1.1)
    for index, at in enumerate((.01, .24, .48) if attack else (.03, .42)):
        duration = .23 if attack else .38
        f = np.linspace(125, 78, count(duration))
        grunt = vocal(duration, "boar grunt " + str(index), f, ((300, 150, .8), (650, 260, 1), (1650, 550, .3)), .55, .12, 1)
        add(out, grunt, at, .7)
        snort = noise(.15, "boar nostril " + str(index), 650, 4000) * envelope(.15, .008, .05, 3)
        add(out, snort, at + .04, .1)
    if attack:
        add(out, whoosh(.42, "boar charge", 1000), .14, .22)
        add(out, impact(.25, "boar stamp", 80, 900), .53, .3)
    return out


def bear(attack=False):
    duration = 1.35 if attack else 1.5
    out = blank(duration)
    call_duration = 1.1 if attack else 1.2
    t = timeline(call_duration)
    f = np.linspace(95 if attack else 68, 48, len(t)) + 4 * np.sin(2 * math.pi * 2.6 * t)
    growl = vocal(call_duration, "bear roar" if attack else "bear rumble", f,
                  ((310, 180, 1), (760, 390, .75), (1550, 700, .35)), .7, .18 if attack else .08, 1)
    growl *= .7 + .3 * np.sin(2 * math.pi * 4.1 * t) ** 2
    add(out, growl, .02, .9)
    if attack:
        add(out, impact(.4, "bear swipe", 58, 2000), .08, .3)
        add(out, whoosh(.38, "bear heavy paw", 1500), .13, .23)
    else:
        add(out, noise(.25, "bear exhale", 250, 2300) * envelope(.25, .02, .08, 3), 1.04, .13)
    return room(out, .12)


def emit_definitions():
    # path, suggested cues, renderer, target RMS, seamless loop
    return [
        ("UI/click", ["ui.click"], ui_click, .072, False),
        ("UI/select", ["ui.select"], lambda: chime([783.99, 1046.5], .32, .06, "UI selection"), .085, False),
        ("UI/cancel", ["ui.cancel"], lambda: chime([659.25, 440], .29, .075, "UI cancel"), .065, False),
        ("UI/reroll", ["ui.reroll"], ui_reroll, .08, False),
        ("UI/error", ["ui.error"], lambda: chime([220, 207.65], .36, .11, "UI reject"), .07, False),
        ("Game/match_found", ["game.start"], match_found, .12, False),
        ("Game/resume", ["game.resume"], lambda: chime([493.88, 659.25, 783.99], .62, .075, "resume"), .10, False),
        ("Game/win", ["game.win"], lambda: chime([523.25, 659.25, 783.99, 1046.5], 1.5, .16, "run victory"), .13, False),
        ("Game/lose", ["game.lose"], lambda: chime([392, 329.63, 246.94, 196], 1.4, .18, "run loss"), .10, False),
        ("Game/level_up", ["level.up"], lambda: chime([523.25, 783.99, 1046.5], .87, .09, "level up"), .11, False),
        ("Game/revive", ["combat.revive"], lambda: room(heal() + sweep(160, 520, .9, "revive lift", .35), .15), .12, False),
        ("Map/travel", ["map.enter", "map.move"], travel, .085, False),
        ("Combat/start", ["combat.start"], combat_start, .12, False),
        ("Combat/boss_start", ["boss.start"], lambda: combat_start(True), .135, False),
        ("Combat/win", ["combat.win"], lambda: chime([659.25, 783.99, 987.77], .9, .12, "battle won"), .11, False),
        ("Combat/critical", ["combat.critical"], lambda: metal(.42, 1300, "critical shard") + impact(.42, "critical crunch", 150, 4500) * .4, .10, False),
        ("Combat/turn_end", ["turn.end"], lambda: whoosh(.3, "turn page", 2100) * .4 + chime([523.25, 392], .3, .065, "turn down") * .3, .08, False),
        ("Kiosk/enter", ["kiosk.enter"], lambda: chime([659.25, 880, 659.25], .72, .13, "kiosk terminal"), .10, False),
        ("Kiosk/purchase", ["kiosk.purchase"], register, .105, False),
        ("Camp/fire_loop", ["camp.fire.loop"], fire_loop, .055, True),
        ("Camp/enter", ["camp.enter"], camp_enter, .085, False),
        ("Camp/rest", ["camp.rest"], lambda: heal() * .7 + whoosh(.9, "rest breath", 1000) * .15, .085, False),
        ("Camp/craft_metal", ["camp.craft.metal"], craft_metal, .115, False),
        ("Camp/craft_complete", ["camp.craft"], lambda: add(add(blank(1.25), craft_metal(), 0, .65), chime([659.25, 987.77, 1318.5], .6, .08, "craft done"), .6, .55), .12, False),
        ("Camp/cook_sizzle", ["camp.cook"], cook_sizzle, .10, False),
        ("Camp/eat", ["item.heal"], eat, .085, False),
        ("Skill/slash", ["effect.slash", "basic.attack"], slash, .115, False),
        ("Skill/projectile", ["effect.projectile"], projectile, .11, False),
        ("Skill/explosion", ["effect.explosion"], explosion, .13, False),
        ("Skill/lightning", ["effect.lightning"], lightning, .115, False),
        ("Skill/flame", ["effect.flame"], flame, .115, False),
        ("Skill/ice", ["effect.ice"], ice, .11, False),
        ("Skill/arcade", ["effect.arcade"], arcade, .105, False),
        ("Skill/shield", ["effect.shield", "basic.guard"], shield, .105, False),
        ("Skill/heal", ["effect.heal"], heal, .10, False),
        ("Skill/dash", ["effect.dash"], dash, .10, False),
        ("Skill/trap", ["effect.trap"], trap, .10, False),
        ("Skill/arcane", ["effect.arcane", "skill.default"], arcane, .105, False),
        ("Skill/multi", ["effect.multi"], multi, .115, False),
        ("Skill/poison", ["effect.poison"], poison, .10, False),
        ("Skill/water", ["effect.water"], water, .11, False),
        ("Skill/bowed_music", ["effect.bowed_music"], bowed_music, .105, False),
        ("Weapon/blade", ["weapon.dagger", "weapon.sword", "weapon.dual", "weapon.spear", "weapon.rapier", "weapon.axe", "weapon.whip"], lambda: room(slash(), .2), .12, False),
        ("Weapon/blunt", ["weapon.glove", "weapon.tonfa", "weapon.bat", "weapon.hammer", "weapon.nunchaku"], lambda: impact(.55, "weapon heavy impact", 86, 2800) + metal(.55, 390, "weapon blunt ring", .2), .12, False),
        ("Weapon/gun", ["weapon.pistol", "weapon.rifle", "weapon.sniper"], weapon_gun, .12, False),
        ("Weapon/bow", ["weapon.bow", "weapon.crossbow", "weapon.throw", "weapon.shuriken"], weapon_bow, .115, False),
        ("Weapon/arcana", ["weapon.arcana", "weapon.vf", "weapon.camera"], lambda: arcane() + sparkles(.85, "weapon sigil", 15) * .4, .115, False),
        ("Weapon/guitar", ["weapon.guitar"], guitar, .11, False),
        ("Tactical/blink", ["tactical.blink"], lambda: add(add(blank(.7), sweep(1500, 230, .25, "blink out", .7), 0), sweep(250, 1800, .3, "blink in", .7), .25), .11, False),
        ("Tactical/wind", ["tactical.wind"], lambda: heal() * .65 + whoosh(.9, "healing wind", 1600) * .35, .105, False),
        ("Tactical/plasma", ["tactical.plasma"], lambda: add(add(blank(.9), dash(), 0, .8), explosion(), .08, .55), .12, False),
        ("Wildlife/chicken_enter", ["wild.chicken.enter"], chicken, .105, False),
        ("Wildlife/chicken_attack", ["wild.chicken.attack"], lambda: chicken(True), .12, False),
        ("Wildlife/dog_enter", ["wild.dog.enter"], dog, .11, False),
        ("Wildlife/dog_attack", ["wild.dog.attack"], lambda: dog(True), .125, False),
        ("Wildlife/wolf_enter", ["wild.wolf.enter"], wolf, .115, False),
        ("Wildlife/wolf_attack", ["wild.wolf.attack"], lambda: wolf(True), .13, False),
        ("Wildlife/boar_enter", ["wild.boar.enter"], boar, .11, False),
        ("Wildlife/boar_attack", ["wild.boar.attack"], lambda: boar(True), .125, False),
        ("Wildlife/bear_enter", ["wild.bear.enter"], bear, .115, False),
        ("Wildlife/bear_attack", ["wild.bear.attack"], lambda: bear(True), .13, False),
        ("Reward/select", ["reward.select"], lambda: chime([880, 1318.5], .41, .06, "reward card"), .09, False),
        ("Reward/confirm", ["reward.confirm"], lambda: chime([659.25, 987.77, 1318.5], .72, .085, "reward packed"), .105, False),
        ("Encounter/enter", ["encounter.enter"], lambda: chime([493.88, 659.25, 587.33], .85, .15, "encounter welcome"), .095, False),
        ("Encounter/choose", ["encounter.choose"], lambda: metal(.18, 1100, "encounter choice", .3) + chime([739.99, 987.77], .18, .05, "encounter response") * .4, .085, False),
    ]


def finish(signal, target_rms, loop=False):
    signal = np.asarray(signal, dtype=np.float64)
    if not np.all(np.isfinite(signal)):
        raise ValueError("Non-finite DSP output")
    signal -= np.mean(signal)
    signal = smooth_band(signal, 25, 10500)
    if not loop:
        signal *= envelope(len(signal), .003, .018)
    rms = float(np.sqrt(np.mean(signal * signal)))
    if rms <= 1e-8:
        raise ValueError("Silent DSP output")
    signal *= target_rms / rms
    # Smooth saturation catches a transient before the final common peak cap.
    signal = np.tanh(signal * 1.12) / 1.12
    signal -= np.mean(signal)
    peak = float(np.max(np.abs(signal)))
    if peak > .82:
        signal *= .82 / peak
    if not loop:
        signal *= envelope(len(signal), .0015, .004)
    pcm = np.rint(np.clip(signal, -.82, .82) * 32767).astype("<i2")
    if not loop:
        pcm[0] = pcm[-1] = 0
    return pcm


def wav_bytes(pcm):
    stream = io.BytesIO()
    with wave.open(stream, "wb") as writer:
        writer.setnchannels(1)
        writer.setsampwidth(2)
        writer.setframerate(RATE)
        writer.writeframes(pcm.tobytes())
    return stream.getvalue()


def guid(path):
    return uuid.uuid5(NAMESPACE, path).hex


def audio_meta(path):
    return f"""fileFormatVersion: 2
guid: {guid(path)}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 7
  defaultSettings:
    serializedVersion: 2
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 0
    quality: 1
    conversionMode: 0
    preloadAudioData: 1
  platformSettingOverrides: {{}}
  forceToMono: 1
  normalize: 0
  ambisonic: 0
  loadInBackground: 0
  userData: fan_created_effect
  assetBundleName:
  assetBundleVariant:
"""


def folder_meta(path):
    return f"""fileFormatVersion: 2
guid: {guid(path)}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: fan_created_effect
  assetBundleName:
  assetBundleVariant:
"""


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Verify deterministic bytes without writing")
    args = parser.parse_args()
    issues, manifest = [], []
    categories = set()
    total_bytes = 0
    for name, cues, render, target, loop in emit_definitions():
        pcm = finish(render(), target, loop)
        blob = wav_bytes(pcm)
        relative = "Assets/Resources/Lumia/Audio/Designed/" + name + ".wav"
        path = ROOT / relative
        categories.add(name.split("/")[0])
        meta = audio_meta(relative)
        if args.check:
            if not path.exists() or path.read_bytes() != blob:
                issues.append("WAV bytes differ: " + relative)
            if not path.with_suffix(".wav.meta").exists() or path.with_suffix(".wav.meta").read_text(encoding="utf8") != meta:
                issues.append("AudioImporter differs: " + relative)
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(blob)
            path.with_suffix(".wav.meta").write_text(meta, encoding="utf8", newline="\n")
        decoded = pcm.astype(np.float64) / 32768
        peak = float(np.max(np.abs(decoded)))
        rms = float(np.sqrt(np.mean(decoded * decoded)))
        dc = float(np.mean(decoded))
        if peak > .821 or rms < .035 or rms > .16 or abs(dc) > .0004:
            issues.append(f"Invalid mix levels {name}: peak={peak}, RMS={rms}, DC={dc}")
        if not loop and (pcm[0] != 0 or pcm[-1] != 0):
            issues.append("Unfaded edge: " + name)
        manifest.append({
            "id": "designed." + name.lower().replace("/", "."),
            "path": "Lumia/Audio/Designed/" + name,
            "file": relative,
            "volume": 1,
            "sourceUrl": SOURCE,
            "sourceName": "Black The Survival project-authored effect: " + name,
            "origin": "fan_created_effect",
            "kind": "fan_created_effect",
            "channel": "effects",
            "license": "Project-authored fan-game sound; not an original Eternal Return recording.",
            "alteration": "Deterministic layered DSP synthesis; band limited, DC removed, faded and peak controlled.",
            "cues": cues,
            "loop": loop,
            "sha256": hashlib.sha256(blob).hexdigest(),
            "bytes": len(blob),
            "durationSeconds": round(len(pcm) / RATE, 6),
            "sampleRate": RATE,
            "channels": 1,
            "bitsPerSample": 16,
            "peak": round(peak, 6),
            "rms": round(rms, 6),
            "dcOffset": round(dc, 8),
            "loopBoundaryJump": round(float(abs(decoded[0] - decoded[-1])), 6) if loop else 0,
        })
        total_bytes += len(blob)
    for category in sorted(categories):
        path = OUT / category
        relative = path.relative_to(ROOT).as_posix()
        meta_path = path.with_suffix(".meta")
        contents = folder_meta(relative)
        if args.check:
            if not meta_path.exists() or meta_path.read_text(encoding="utf8") != contents:
                issues.append("Folder metadata differs: " + relative)
        else:
            meta_path.write_text(contents, encoding="utf8", newline="\n")
    contents = json.dumps(manifest, ensure_ascii=False, indent=2) + "\n"
    if args.check:
        if not MANIFEST.exists() or MANIFEST.read_text(encoding="utf8") != contents:
            issues.append("Provenance manifest differs")
    else:
        MANIFEST.parent.mkdir(parents=True, exist_ok=True)
        MANIFEST.write_text(contents, encoding="utf8", newline="\n")
    if issues:
        for issue in issues:
            print(issue)
        raise SystemExit(1)
    print(json.dumps({"passed": True, "mode": "check" if args.check else "generate", "clips": len(manifest),
                      "bytes": total_bytes, "minimumRms": min(x["rms"] for x in manifest),
                      "maximumPeak": max(x["peak"] for x in manifest), "manifest": str(MANIFEST)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
