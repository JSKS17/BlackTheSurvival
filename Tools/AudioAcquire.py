"""Acquire selected original Korean voice lines from Nimble Neuron's public fankit.

Only public download endpoints are used. ZIP byte ranges avoid downloading the
entire multi-gigabyte voice collection; selected members are CRC checked. Cached
source material stays in Temp. The original PCM WAV bytes are retained unchanged.
"""
from __future__ import annotations

import concurrent.futures
import hashlib
import html
import json
import pathlib
import re
import struct
import time
import unicodedata
import urllib.error
import urllib.parse
import urllib.request
import uuid
import zlib

ROOT = pathlib.Path(__file__).resolve().parents[1]
CACHE = ROOT / "Temp/AudioAcquisition"
OUT = ROOT / "Assets/Resources/Lumia/Audio"
FANKIT = "https://drive.google.com/drive/folders/1bgW32L09YPpRgQKtH4C_TAd3Kr0N9Y90"
CHARACTERS = "1m__ubKg-KY7TqnqbFqwHi1DVxrdBeTEW"
POLICY = "https://support.playeternalreturn.com/hc/en-us/articles/49503976113177"
SKILL_IDS = {r["id"]: r["original_number"] for r in json.loads((ROOT / "docs/art-chibi/reference-skills.json").read_text(encoding="utf8"))}
SCHEMA = 5


def fetch(url: str, headers=None) -> tuple[bytes, dict]:
    for attempt in range(4):
        try:
            with urllib.request.urlopen(urllib.request.Request(url, headers=headers or {}), timeout=60) as response:
                return response.read(), dict(response.headers)
        except (urllib.error.URLError, TimeoutError):
            if attempt == 3:
                raise
            time.sleep(attempt + 1)
    raise AssertionError("unreachable")


def folder(folder_id: str) -> list[dict]:
    cached = CACHE / (folder_id + ".json")
    if cached.exists():
        return json.loads(cached.read_text(encoding="utf8"))
    body, _ = fetch("https://drive.google.com/embeddedfolderview?id=" + folder_id)
    text = body.decode("utf8")
    rows = []
    for link, name in re.findall(r'<a href="([^"]+)"[^>]*>.*?<div class="flip-entry-title">(.*?)</div>', text, re.S):
        match = re.search(r"/(?:folders/|file/d/)([^/?]+)", link)
        if match:
            rows.append(dict(id=match.group(1), name=html.unescape(name), folder="/folders/" in link))
    if not rows:
        raise ValueError("Empty or inaccessible public folder: " + folder_id)
    cached.write_text(json.dumps(rows, ensure_ascii=False, indent=2), encoding="utf8")
    return rows


class PublicZip:
    def __init__(self, file_id: str):
        self.file_id = file_id
        self.url = "https://drive.google.com/uc?export=download&id=" + file_id
        body, headers = fetch(self.url, {"Range": "bytes=-65536"})
        if "text/html" in headers.get("Content-Type", ""):
            text = body.decode("utf8")
            form = re.search(r'<form id="download-form" action="([^"]+)"', text)
            fields = dict(re.findall(r'<input type="hidden" name="([^"]+)" value="([^"]+)"', text))
            if not form or fields.get("id") != file_id:
                raise ValueError("Public ZIP download unavailable: " + file_id)
            self.url = html.unescape(form.group(1)) + "?" + urllib.parse.urlencode(fields)
            body, headers = fetch(self.url, {"Range": "bytes=-65536"})
        end = body.rfind(b"PK\x05\x06")
        if end < 0:
            raise ValueError("ZIP directory missing: " + file_id)
        count, size, offset = struct.unpack_from("<HII", body, end + 10)
        range_header = headers.get("Content-Range", "")
        if range_header:
            total = int(range_header.rsplit("/", 1)[1])
            base = total - len(body)
            if offset < base:
                body, _ = fetch(self.url, {"Range": f"bytes={offset}-{offset + size - 1}"})
                pos = 0
            else:
                pos = offset - base
        else:
            pos = offset
        self.entries = []
        for _ in range(count):
            if body[pos:pos + 4] != b"PK\x01\x02":
                raise ValueError("Invalid ZIP central header")
            record = struct.unpack_from("<4s6H3I5H2I", body, pos)
            name = body[pos + 46:pos + 46 + record[10]].decode("utf8" if record[3] & 2048 else "cp437")
            self.entries.append(dict(name=name, method=record[4], crc=record[7], compressed=record[8], size=record[9], offset=record[16]))
            pos += 46 + record[10] + record[11] + record[12]

    def read(self, entry: dict) -> bytes:
        start = entry["offset"]
        head, _ = fetch(self.url, {"Range": f"bytes={start}-{start + 29}"})
        if head[:4] != b"PK\x03\x04":
            raise ValueError("Invalid ZIP member header")
        namesize, extrasize = struct.unpack_from("<HH", head, 26)
        start += 30 + namesize + extrasize
        compressed, _ = fetch(self.url, {"Range": f"bytes={start}-{start + entry['compressed'] - 1}"})
        if entry["method"] == 0:
            data = compressed
        elif entry["method"] == 8:
            data = zlib.decompress(compressed, -15)
        else:
            raise ValueError("Unsupported ZIP method")
        if len(data) != entry["size"] or zlib.crc32(data) & 0xFFFFFFFF != entry["crc"]:
            raise ValueError("ZIP member integrity failure: " + entry["name"])
        return data


class PublicLoose:
    def __init__(self, folder_id: str):
        self.entries = [dict(name=r["name"], file_id=r["id"]) for r in folder(folder_id) if not r["folder"] and r["name"].lower().endswith(".wav")]

    def read(self, entry: dict) -> bytes:
        data, _ = fetch("https://drive.google.com/uc?export=download&id=" + entry["file_id"])
        return data


def norm(text: str) -> str:
    return re.sub(r"[^a-z0-9]", "", unicodedata.normalize("NFKD", text).encode("ascii", "ignore").decode().lower())


def meta(path: pathlib.Path, audio=False):
    target = pathlib.Path(str(path) + ".meta")
    if target.exists():
        return
    guid = uuid.uuid5(uuid.NAMESPACE_URL, "BlackTheSurvival:" + path.relative_to(ROOT).as_posix()).hex
    if path.is_dir():
        body = f"fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    elif audio:
        body = f"""fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 7
  defaultSettings:
    serializedVersion: 2
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 0.7
    conversionMode: 0
    preloadAudioData: 0
  platformSettingOverrides: {{}}
  forceToMono: 0
  normalize: 1
  ambisonic: 0
  loadInBackground: 1
  m_OriginalIsTrackerFormat: 0
  userData: Original Eternal Return Korean voice line from official fan kit.
  assetBundleName:
  assetBundleVariant:
"""
    else:
        body = f"fileFormatVersion: 2\nguid: {guid}\nTextScriptImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    target.write_text(body, encoding="utf8")


def acquire_subject(subject: dict, subject_folder: dict) -> dict:
    subject_id = subject["id"]
    voice_folders = [r for r in folder(subject_folder["id"]) if r["folder"] and ("voice" in r["name"].lower() or "volce" in r["name"].lower())]
    if not voice_folders:
        return dict(subject=subject_id, missing="No voice folder")
    archives = [r for r in folder(voice_folders[0]["id"]) if re.search(r"(?:KOR|KR|KO)(?:\.zip|_|$)", r["name"], re.I)]
    if not archives:
        return dict(subject=subject_id, missing="No Korean archive")
    archive = archives[0]
    cached = CACHE / (subject_id + "_selected.json")
    if cached.exists():
        saved = json.loads(cached.read_text(encoding="utf8"))
        enough_nia_actions = subject_id != "nia" or all(any(c["id"] == "voice.nia." + action for c in saved["clips"]) for action in ("cook", "camp"))
        if enough_nia_actions and (saved.get("acquisitionSchema") == SCHEMA or (saved.get("acquisitionSchema", 0) >= 2 and not saved.get("missing"))) and all((ROOT / row["asset"]).exists() and hashlib.sha256((ROOT / row["asset"]).read_bytes()).hexdigest() == row["sha256"] for row in saved["clips"]):
            return saved
    remote = PublicLoose(archive["id"]) if archive["folder"] else PublicZip(archive["id"])
    (CACHE / (subject_id + "_zip_entries.json")).write_text(json.dumps(remote.entries, ensure_ascii=False, indent=2), encoding="utf8")
    clips, cards, bindings, missing = [], [], [], []
    selected = []
    for index, slot in enumerate("qwer", 1):
        patterns = [r"_skill" + slot + r"(?:[0-9_-]|\.wav)", r"_useSkillActive" + str(index) + r"(?:Seq|_)"]
        original_id = SKILL_IDS.get(subject_id + "_" + slot)
        if original_id:
            patterns.append(r"_PlaySkill" + str(original_id) + r"(?:seq|_|\.)")
        choices = [e for e in remote.entries if any(re.search(pattern, pathlib.PurePosixPath(e["name"]).name, re.I) for pattern in patterns)]
        if choices:
            selected.append((slot, sorted(choices, key=lambda e: (len(e["name"]), e["name"].lower()))[0], "skill"))
        else:
            missing.append(subject_id + "_" + slot)
    if subject_id == "nia":
        actions = {"lobby": "lobby", "start": "firstMove", "craft": "craftLegend", "cook": "makeFood", "camp": "bonfire", "rest": "rest", "kiosk": "callTransferConsole", "heal": "useFood", "win": "victory", "lose": "lost", "level": "levelUp", "hunt": "killMonster"}
        for key, token in actions.items():
            choices = [e for e in remote.entries if re.search(r"_" + token + r"(?:[0-9_]|\.wav)", pathlib.PurePosixPath(e["name"]).name, re.I)]
            if choices:
                selected.append((key, sorted(choices, key=lambda e: (len(e["name"]), e["name"].lower()))[0], "action"))
    directory = OUT / "Voice" / subject_id
    directory.mkdir(parents=True, exist_ok=True)
    meta(directory)
    for key, entry, kind in selected:
        path = directory / (key + ".wav")
        previous = next((c for c in saved.get("clips", []) if c["id"] == "voice." + subject_id + "." + key and c["sourceName"].endswith(" / " + entry["name"])), None) if cached.exists() else None
        existing = path.read_bytes() if previous and path.exists() else None
        data = existing if existing is not None and hashlib.sha256(existing).hexdigest() == previous["sha256"] else remote.read(entry)
        if data[:4] != b"RIFF" or data[8:12] != b"WAVE":
            raise ValueError("Selected original is not a WAV: " + entry["name"])
        path.write_bytes(data)
        meta(path, audio=True)
        clip_id = "voice." + subject_id + "." + key
        source_url = "https://drive.google.com/file/d/" + entry.get("file_id", archive["id"]) + "/view"
        row = dict(id=clip_id, path="Lumia/Audio/Voice/" + subject_id + "/" + key, volume=0.8, channel="voice", kind="original_character_voice", origin="official", sourceUrl=source_url, sourceName=archive["name"] + " / " + entry["name"], sha256=hashlib.sha256(data).hexdigest(), bytes=len(data), license="Nimble Neuron IP Usage Policy; Eternal Return fan creation", asset=path.relative_to(ROOT).as_posix())
        clips.append(row)
        if kind == "skill":
            bindings.append(dict(cue="voice." + subject_id + "_" + key, clipIds=[clip_id], volume=1))
        else:
            bindings.append(dict(cue="voice.nia." + key, clipIds=[clip_id], volume=1))
    result = dict(acquisitionSchema=SCHEMA, subject=subject_id, archive=archive, clips=clips, cards=cards, bindings=bindings, missing=missing)
    cached.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf8")
    return result


def acquire_announcer() -> dict:
    clips, bindings = [], []
    rows = folder("1iKu4QmD0EJffAY9zboqlbXV8fgGQs9gJ")
    for action, token in [("start", "게임 시작"), ("end", "게임 종료")]:
        selected = [r for r in rows if token in r["name"]]
        ids = []
        directory = OUT / "Voice/announcer"
        directory.mkdir(parents=True, exist_ok=True)
        meta(directory)
        for index, entry in enumerate(selected, 1):
            path = directory / (action + str(index) + ".wav")
            if not path.exists():
                body, _ = fetch("https://drive.google.com/uc?export=download&id=" + entry["id"])
                if body[:4] != b"RIFF" or body[8:12] != b"WAVE":
                    raise ValueError("Announcer original is not a WAV")
                path.write_bytes(body)
            data = path.read_bytes()
            meta(path, audio=True)
            clip_id = "voice.announcer." + action + str(index)
            ids.append(clip_id)
            clips.append(dict(id=clip_id, path="Lumia/Audio/Voice/announcer/" + action + str(index), volume=0.8, channel="voice", kind="original_announcer_voice", origin="official", sourceUrl="https://drive.google.com/file/d/" + entry["id"] + "/view", sourceName=entry["name"], sha256=hashlib.sha256(data).hexdigest(), bytes=len(data), license="Nimble Neuron IP Usage Policy; Eternal Return fan creation", asset=path.relative_to(ROOT).as_posix()))
        if ids:
            bindings.append(dict(cue="voice.announcer." + action, clipIds=ids, volume=1))
    return dict(subject="announcer", clips=clips, bindings=bindings, cards=[])


def main():
    CACHE.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "Voice").mkdir(exist_ok=True)
    meta(OUT)
    meta(OUT / "Voice")
    subjects = json.loads((ROOT / "docs/art-chibi/reference-characters.json").read_text(encoding="utf8"))[1:]
    folders = folder(CHARACTERS)
    lookup = {norm(re.sub(r"^\d+\.\s*", "", r["name"])): r for r in folders}
    jobs = []
    for subject in subjects:
        key = norm(subject["cdn_key"])
        key = {"debimarlene": "debimarlene"}.get(key, key)
        if key not in lookup:
            raise ValueError("Subject folder not found: " + subject["id"])
        jobs.append((subject, lookup[key]))
    results = []
    def task(job):
        try:
            result = acquire_subject(*job)
            print("ACQUIRED", result["subject"], len(result.get("clips", [])), "missing", result.get("missing", []), flush=True)
            return result
        except Exception as exc:
            print("FAILED", job[0]["id"], repr(exc), flush=True)
            return dict(subject=job[0]["id"], error=repr(exc))
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        results = list(pool.map(task, jobs))
    results.append(acquire_announcer())
    results.sort(key=lambda r: r["subject"])
    manifest = dict(schemaVersion=1, sources=[dict(name="Official Eternal Return fan kit", url=FANKIT, policy=POLICY, attribution="Eternal Return © Nimble Neuron Corporation. All rights reserved.")], clips=[c for r in results for c in r.get("clips", [])], bindings=[c for r in results for c in r.get("bindings", [])], cards=[c for r in results for c in r.get("cards", [])], missing=[r for r in results if r.get("missing") or r.get("error")])
    (CACHE / "voices_catalog.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf8")
    (OUT / "voice-sources.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf8")
    meta(OUT / "voice-sources.json")
    print("TOTAL", len(manifest["clips"]), "cards", len(manifest["cards"]), "issues", len(manifest["missing"]), flush=True)


if __name__ == "__main__":
    main()
