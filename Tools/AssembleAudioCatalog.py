"""Build the Resources audio routing catalog from independently sourced assets."""
import hashlib
import json
import pathlib

from AudioAcquire import ROOT, OUT, meta


def card_bindings(clip_ids):
    # Q/W/E/R palettes follow original skill actions and elements. The raw card
    # effect field is too broad (223 cards were slash), so this is deliberately
    # separate from the mechanical status/effect classification.
    palettes = {
        "nia": "arcade arcade arcade arcade",
        "jackie": "slash dash dash multi",
        "aya": "gun gun dash explosion",
        "hyunwoo": "blunt shield dash blunt",
        "yuki": "blade shield dash multi",
        "hyejin": "arcane trap dash arcane",
        "sua": "blunt shield dash arcane",
        "isol": "explosion gun dash trap",
        "nadine": "bow trap dash multi",
        "emma": "projectile arcane trap dash",
        "charlotte": "projectile heal dash heal",
        "nathapon": "arcane arcane trap ice",
        "nicky": "blunt shield blunt blunt",
        "daniel": "blade shield dash multi",
        "tia": "projectile arcade dash arcane",
        "laura": "slash projectile dash dash",
        "lenox": "slash slash multi poison",
        "leon": "water water dash water",
        "rozzi": "gun multi dash explosion",
        "luke": "blunt shield dash projectile",
        "dailin": "blunt heal projectile multi",
        "rio": "bow bow dash bow",
        "martina": "projectile shield dash trap",
        "mai": "slash shield dash heal",
        "markus": "blunt blunt dash blunt",
        "magnus": "projectile multi blunt explosion",
        "vanya": "arcane shield dash poison",
        "barbara": "trap lightning explosion lightning",
        "bernice": "gun trap projectile explosion",
        "bianca": "projectile heal dash poison",
        "celine": "trap explosion dash arcane",
        "sho": "projectile blunt dash flame",
        "shoichi": "blade dash projectile multi",
        "sissela": "projectile shield trap arcane",
        "silvia": "projectile trap projectile dash",
        "adela": "projectile blunt shield arcane",
        "adriana": "flame trap dash flame",
        "adina": "arcane arcane arcane arcane",
        "isaac": "blunt shield dash blunt",
        "alex": "gun trap dash explosion",
        "jan": "blunt blunt dash trap",
        "estelle": "blunt water shield water",
        "aiden": "lightning lightning dash lightning",
        "echion": "slash shield dash arcane",
        "elena": "ice ice dash ice",
        "johann": "projectile trap heal shield",
        "william": "projectile shield dash flame",
        "irem": "projectile shield dash arcade",
        "eva": "arcane arcane dash lightning",
        "ian": "slash multi dash arcane",
        "eleven": "blunt shield dash multi",
        "zahir": "projectile projectile multi multi",
        "jenny": "projectile trap dash arcane",
        "camilo": "multi slash dash multi",
        "karla": "bow multi dash explosion",
        "cathy": "blade blade projectile heal",
        "chloe": "blunt trap dash heal",
        "chiara": "arcane shield trap poison",
        "tazia": "projectile shield dash projectile",
        "theodore": "lightning shield explosion lightning",
        "felix": "slash blade dash multi",
        "priya": "guitar shield guitar guitar",
        "fiora": "blade multi dash blade",
        "piolo": "multi shield dash blunt",
        "hart": "guitar guitar dash guitar",
        "haze": "explosion gun gun explosion",
        "debi_marlene": "blade multi dash multi",
        "arda": "projectile trap blunt arcane",
        "abigail": "slash shield dash arcane",
        "alonso": "blunt shield dash lightning",
        "leni": "explosion blunt projectile dash",
        "tsubame": "projectile multi dash multi",
        "kenneth": "slash flame dash flame",
        "katja": "gun projectile trap gun",
        "darko": "blunt blunt dash blunt",
        "lenore": "bowed_music bowed_music dash bowed_music",
        "garnet": "blunt shield dash poison",
        "yumin": "projectile multi dash multi",
        "hisui": "blade multi dash blade",
        "justyna": "gun explosion dash lightning",
        "istvan": "arcane arcane dash arcane",
        "xuelin": "blade blade dash multi",
        "henry": "projectile shield dash arcane",
        "blair": "blade multi dash arcane",
        "mirka": "blunt shield dash blunt",
        "fenrir": "slash dash shield multi",
        "coraline": "lightning shield trap arcane",
        "bihyung": "blunt flame dash arcane",
        "craver": "gun blunt dash gun",
        "lucia": "blade gun dash blade",
        "ceres": "dash multi shield shield",
    }
    subjects = json.loads((ROOT / "docs/art-chibi/reference-characters.json").read_text(encoding="utf8"))[1:]
    if set(palettes) != {subject["id"] for subject in subjects}:
        raise ValueError("Audio palette must cover every playable subject")
    originals = {row["id"]: row["original_name"] for row in json.loads((ROOT / "docs/art-chibi/reference-skills.json").read_text(encoding="utf8"))}
    bindings = []
    def add(card_id, clip_id, reason):
        if clip_id not in clip_ids:
            raise ValueError("Card audio texture missing: " + clip_id)
        bindings.append(dict(cardId=card_id, clipIds=[clip_id], volume=1, basis=reason))
    for owner, palette in palettes.items():
        textures = palette.split()
        if len(textures) != 4:
            raise ValueError("Q/W/E/R palette needs exactly four textures: " + owner)
        for slot, texture in zip("qwer", textures):
            card_id = owner + "_" + slot
            group = "weapon" if texture in ("blade", "blunt", "gun", "bow", "guitar") else "skill"
            add(card_id, "designed." + group + "." + texture, originals[card_id])
    add("basic_attack", "designed.skill.slash", "Basic attack impact")
    add("basic_guard", "designed.skill.shield", "Basic guard")
    weapons = {
        "blade": "dagger axe sword rapier whip dual spear",
        "blunt": "glove hammer tonfa bat nunchaku",
        "gun": "pistol sniper rifle",
        "bow": "bow crossbow throw shuriken",
        "guitar": "guitar",
        "arcana": "camera arcana vf",
    }
    for texture, classes in weapons.items():
        for weapon in classes.split():
            add("weapon_" + weapon, "designed.weapon." + texture, "Original weapon class: " + weapon)
    for tactical in ("blink", "wind", "plasma"):
        add("tactical_" + tactical, "designed.tactical." + tactical, "Original tactical skill: " + tactical)
    if len(bindings) != 392:
        raise ValueError("Expected 392 distinct card audio routes")
    return sorted(bindings, key=lambda row: row["cardId"])


def main():
    voices_path = OUT / "voice-sources.json"
    voices = json.loads(voices_path.read_text(encoding="utf8"))
    clips = list(voices["clips"])
    bindings = list(voices["bindings"])
    sources = list(voices["sources"])
    music_path = ROOT / "docs/audio/music-sources.json"
    music = json.loads(music_path.read_text(encoding="utf8")) if music_path.exists() else []
    for entry in music:
        clips.append(dict(entry, channel="music", kind="original_music", volume=0.55, license="Nimble Neuron IP Usage Policy; Eternal Return fan creation"))
    available_music = {row["id"] for row in music}
    music_routes = {
        "lobby": "music_summertime", "preparation": "music_summertime",
        "map": "music_golden_willow", "camp": "music_golden_willow",
        "encounter": "music_golden_willow", "lose": "music_golden_willow",
        "combat": "music_red_heart", "boss": "music_red_heart",
        "kiosk": "music_summertime", "win": "music_summertime",
    }
    for scene, clip_id in music_routes.items():
        if clip_id in available_music:
            bindings.append(dict(cue="music." + scene, clipIds=[clip_id], volume=1))
    designed_path = ROOT / "docs/audio/designed-sources.json"
    if designed_path.exists():
        designed = json.loads(designed_path.read_text(encoding="utf8"))
        if isinstance(designed, dict):
            clips.extend(designed.get("clips", []))
            bindings.extend(designed.get("bindings", []))
            sources.extend(designed.get("sources", []))
        else:
            for entry in designed:
                row = dict(entry)
                cues = row.pop("cues", row.pop("suggestedCues", []))
                clips.append(row)
                for cue in cues:
                    bindings.append(dict(cue=cue, clipIds=[row["id"]], volume=1))
    ids = [row["id"] for row in clips]
    if len(set(ids)) != len(ids):
        raise ValueError("Duplicate audio clip ID")
    cues = [row["cue"] for row in bindings]
    if len(set(cues)) != len(cues):
        raise ValueError("Duplicate audio cue")
    for clip in clips:
        matches = [p for p in (ROOT / "Assets/Resources" / clip["path"]).parent.glob((ROOT / "Assets/Resources" / clip["path"]).name + ".*") if p.suffix != ".meta"]
        if len(matches) != 1:
            raise ValueError("Missing or ambiguous audio asset: " + clip["path"])
        actual = hashlib.sha256(matches[0].read_bytes()).hexdigest()
        if clip.get("sha256") and actual != clip["sha256"]:
            raise ValueError("Audio source hash changed: " + clip["id"])
        clip.setdefault("volume", 1)
    for binding in bindings:
        if not binding.get("clipIds") or any(i not in ids for i in binding["clipIds"]):
            raise ValueError("Invalid cue reference: " + binding["cue"])
        binding.setdefault("volume", 1)
    routes = card_bindings(ids) if designed_path.exists() else []
    catalog = dict(schemaVersion=1, sources=sources, clips=sorted(clips, key=lambda row: row["id"]), bindings=sorted(bindings, key=lambda row: row["cue"]), cards=routes, unavailableOriginalVoiceCards=voices["missing"], note="Official original OST and Korean voices are identified separately from fan-authored nonvoice effects. No game RNG is consumed by audio routing.")
    path = OUT / "catalog.json"
    path.write_text(json.dumps(catalog, ensure_ascii=False, indent=2) + "\n", encoding="utf8")
    meta(path)
    print("Audio catalog:", len(clips), "clips,", len(bindings), "cues")


if __name__ == "__main__":
    main()
