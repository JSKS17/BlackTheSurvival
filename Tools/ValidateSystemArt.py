"""Independently validate system icons and the 80-pixel character portrait set.

Reads PNGs and provenance; never rewrites or repairs artwork. A progress run can
allow missing assets, but it does not count as a completed final verification.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from collections import Counter, defaultdict
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
UI_IDS = {"credits", "campfire"}


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def relative(root: Path, path: Path) -> str:
    return path.resolve().relative_to(root.resolve()).as_posix()


def manifest_entries(value) -> list[dict]:
    if isinstance(value, list):
        return value
    if not isinstance(value, dict):
        raise ValueError("manifest must be a JSON list or object")
    for name in ("assets", "icons", "items", "runes"):
        if isinstance(value.get(name), list):
            return value[name]
    raise ValueError("expected a list or an assets/icons/items/runes list")


def png_metrics(path: Path) -> tuple[Image.Image, dict]:
    with Image.open(path) as opened:
        image_format, image_mode = opened.format, opened.mode
        image = opened.convert("RGBA")
    alpha = image.getchannel("A")
    histogram = alpha.histogram()
    pixels = image.get_flattened_data() if hasattr(image, "get_flattened_data") else image.getdata()
    visible_colors = {color for color in pixels if color[3] > 0}
    strong_alpha = alpha.point(lambda value: 255 if value >= 32 else 0)
    return image, {
        "format": image_format,
        "mode": image_mode,
        "size": list(image.size),
        "transparentPixels": histogram[0],
        "visiblePixels": image.width * image.height - histogram[0],
        "partialAlphaPixels": sum(histogram[1:255]),
        "visibleColors": len(visible_colors),
        "alphaBounds": list(alpha.getbbox()) if alpha.getbbox() else None,
        "strongAlphaBounds": list(strong_alpha.getbbox()) if strong_alpha.getbbox() else None,
    }


def nearest_blocks(image: Image.Image) -> bool:
    """Exact RGBA equality proves every export cell is a 2x2 square cluster."""
    if image.width % 2 or image.height % 2:
        return False
    pixels = image.load()
    return all(
        pixels[x, y] == pixels[x + 1, y] == pixels[x, y + 1] == pixels[x + 1, y + 1]
        for y in range(0, image.height, 2)
        for x in range(0, image.width, 2)
    )


def validate(root: Path, allow_incomplete: bool = False) -> dict:
    errors, warnings, missing, entries = [], [], [], []
    expected: dict[str, dict] = {}
    manifests = ("docs/art-system/item-manifest.json", "docs/art-system/rune-manifest.json", "docs/art-system/misc-manifest.json")
    for manifest_name in manifests:
        path = root / manifest_name
        if not path.is_file():
            missing.append("manifest/" + manifest_name)
            continue
        try:
            rows = manifest_entries(read_json(path))
        except (OSError, ValueError, TypeError) as error:
            errors.append(f"{manifest_name}: {error}")
            continue
        for row in rows:
            asset_id = row.get("id")
            if not isinstance(asset_id, str) or not asset_id:
                errors.append(f"{manifest_name}: invalid asset ID")
                continue
            if asset_id in expected:
                errors.append(f"duplicate icon manifest ID: {asset_id}")
            expected[asset_id] = row
    for asset_id in UI_IDS:
        expected.setdefault(asset_id, {"id": asset_id, "kind": "ui", "output": f"Assets/Resources/Lumia/SystemIcons/{asset_id}.png"})
    additions=read_json(root/'docs/art-system/loadout-art-manifest.json')
    expected.update({e['id']:e for e in additions if e['kind']=='gear'})
    if len(expected) != 70:
        errors.append(f"icon contract contains {len(expected)} IDs; expected 70")

    # Cross-check the item/rune export against the actual database declarations.
    # This is a narrow parser for this repository's stable registration helpers.
    database = root / "Assets/Scripts/Lumia/GameDatabase.cs"
    try:
        source = database.read_text(encoding="utf-8-sig")
        declarations = {
            "gear": set(re.findall(r'\bG\("([^"\n]+)"\s*,', source)),
            "rune": set(re.findall(r'\bR\("([^"\n]+)"\s*,', source)),
            "object": set(re.findall(r'new\s+ObjectDef\s*\{\s*id\s*=\s*"([^"\n]+)"', source)),
        }
        equipment_source=(root/'Assets/Scripts/Lumia/EquipmentIdentity.cs').read_text(encoding='utf-8-sig')
        declarations['gear'].update(re.findall(r'\bAdd\(gear,"([^"\n]+)"',equipment_source))
        for kind, game_ids in declarations.items():
            manifest_ids = {asset_id for asset_id, row in expected.items() if row.get("kind") == kind}
            if not game_ids:
                errors.append(f"GameDatabase {kind} declarations could not be read; update the validator for a changed registration format")
            if game_ids != manifest_ids:
                errors.append(f"{kind} manifest differs from GameDatabase: missing={sorted(game_ids - manifest_ids)}, extra={sorted(manifest_ids - game_ids)}")
    except OSError as error:
        errors.append(f"GameDatabase declarations: {error}")

    manifest_path = root / "docs/art-chibi/game-art-manifest.json"
    try:
        portrait_rows = read_json(manifest_path)["sprites"]
        portrait_ids = [row["id"] for row in portrait_rows]
        if len(portrait_ids) != 92 or len(set(portrait_ids)) != 92 or "hana" not in portrait_ids:
            errors.append("portrait manifest must contain Hana plus 91 distinct subjects")
    except (OSError, ValueError, KeyError, TypeError) as error:
        errors.append(f"game-art-manifest sprites: {error}")
        portrait_ids = []

    index_path = root / "docs/art-system/asset-index.json"
    try:
        indexed_rows = manifest_entries(read_json(index_path))
        indexed_rows += [e for e in read_json(root/'docs/art-system/loadout-asset-index.json') if e['kind']=='gear']
    except (OSError, ValueError, TypeError) as error:
        errors.append(f"asset-index: {error}")
        indexed_rows = []
    index, source_groups, call_groups, pixel_groups = {}, defaultdict(list), defaultdict(list), defaultdict(list)
    for row in indexed_rows:
        asset_id = row.get("id")
        if asset_id in index:
            errors.append(f"duplicate provenance entry: {asset_id}")
        index[asset_id] = row
        if asset_id not in expected:
            errors.append(f"provenance ID is absent from the game contract: {asset_id}")

    icon_directory = root / "Assets/Resources/Lumia/SystemIcons"
    actual_icon_ids = {path.stem for path in icon_directory.glob("*.png")}
    for extra in sorted(actual_icon_ids - expected.keys()):
        errors.append(f"unexpected SystemIcons PNG: {extra}")
    for asset_id, reference in sorted(expected.items()):
        label = "icon/" + asset_id
        path = icon_directory / (asset_id + ".png")
        required_path = f"Assets/Resources/Lumia/SystemIcons/{asset_id}.png"
        if reference.get("output", required_path).replace("\\", "/") != required_path:
            errors.append(f"{label}: manifest export path differs from the Resources contract")
        if not path.is_file():
            missing.append(label)
            continue
        try:
            image, metrics = png_metrics(path)
            metrics.update({"id": asset_id, "kind": "icon", "path": relative(root, path), "fileSha256": digest(path)})
            metrics["nearest2x2"] = nearest_blocks(image)
            entries.append(metrics)
            if metrics["format"] != "PNG" or metrics["mode"] != "RGBA" or image.size != (64, 64):
                errors.append(f"{label}: expected explicit RGBA PNG 64x64, got {metrics['mode']} {image.size}")
            if not metrics["transparentPixels"] or metrics["visiblePixels"] < 64:
                errors.append(f"{label}: expected a nonempty icon on a transparent background")
            if not metrics["nearest2x2"]:
                errors.append(f"{label}: not a nearest-neighbour 32-to-64 pixel export")
            bounds = metrics["strongAlphaBounds"]
            if bounds and (bounds[0] == 0 or bounds[1] == 0 or bounds[2] == image.width or bounds[3] == image.height):
                errors.append(f"{label}: artwork touches the canvas edge; transparent margin is required")
            pixel_hash = hashlib.sha256(image.tobytes()).hexdigest()
            pixel_groups[pixel_hash].append(label)
        except (OSError, ValueError) as error:
            errors.append(f"{label}: unreadable PNG: {error}")
            continue

        row = index.get(asset_id)
        if row is None:
            missing.append("provenance/" + asset_id)
            continue
        if row.get("path", "").replace("\\", "/") != required_path:
            errors.append(f"{label}: provenance export path differs from the Resources contract")
        if row.get("fileSha256") != digest(path):
            errors.append(f"{label}: exported file hash differs from the processing record")
        if row.get("size") != [64, 64] or row.get("logicalSize") != [32, 32]:
            errors.append(f"{label}: provenance must record 32x32 logical pixels exported at 64x64")
        source_path = (root / row.get("source", "")).resolve()
        generated_root = (root / "docs/art-system/generated").resolve()
        if not (source_path.is_relative_to(generated_root) or source_path.is_relative_to((root/'docs/art-system/loadout-generated').resolve())) or not source_path.is_file():
            errors.append(f"{label}: original generated source is missing from docs/art-system/generated")
        else:
            source_hash = digest(source_path)
            source_groups[source_hash].append(label)
            if row.get("sourceSha256") != source_hash:
                errors.append(f"{label}: generated-source hash differs from the processing record")
        prompt_path = (root / row.get("promptRecord", "")).resolve()
        if not (prompt_path.is_relative_to((root / "docs/art-system/generation").resolve()) or prompt_path.is_relative_to((root/'docs/art-system/loadout-generation').resolve())) or not prompt_path.is_file():
            errors.append(f"{label}: individual generation prompt record is missing")
        else:
            try:
                prompt = read_json(prompt_path)
                if prompt.get("id") != asset_id or "image_gen" not in prompt.get("tool", "") or not prompt.get("prompt"):
                    errors.append(f"{label}: prompt must name this asset and the image-generation tool")
                if not prompt.get("originalGeneratedPath"):
                    errors.append(f"{label}: original image-generation result path is missing")
                else:
                    call_groups[prompt["originalGeneratedPath"]].append(label)
            except (OSError, ValueError, TypeError) as error:
                errors.append(f"{label}: unreadable prompt record: {error}")
        reference_path = root / reference.get("referencePath", "")
        if not reference_path.is_file() or not reference.get("referenceUrl"):
            errors.append(f"{label}: original-game reference image or URL is missing")

    portrait_directory = root / "Assets/Resources/Lumia/PortraitsCoarse"
    actual_portrait_ids = {path.stem for path in portrait_directory.glob("*.png")}
    for extra in sorted(actual_portrait_ids - set(portrait_ids)):
        errors.append(f"unexpected PortraitsCoarse PNG: {extra}")
    for asset_id in portrait_ids:
        label = "portrait/" + asset_id
        path = portrait_directory / (asset_id + ".png")
        if not path.is_file():
            missing.append(label)
            continue
        try:
            image, metrics = png_metrics(path)
            metrics.update({"id": asset_id, "kind": "portrait", "path": relative(root, path), "fileSha256": digest(path)})
            entries.append(metrics)
            if metrics["format"] != "PNG" or metrics["mode"] != "RGBA" or image.height != 80:
                errors.append(f"{label}: expected explicit RGBA PNG with logical height 80, got {metrics['mode']} {image.size}")
            if not metrics["transparentPixels"] or metrics["visiblePixels"] < image.width * image.height * 0.04:
                errors.append(f"{label}: expected a visible character and transparent background")
            pixel_groups[hashlib.sha256(image.tobytes()).hexdigest()].append(label)
        except (OSError, ValueError) as error:
            errors.append(f"{label}: unreadable PNG: {error}")

    duplicates = [labels for labels in pixel_groups.values() if len(labels) > 1]
    duplicate_sources = [labels for labels in source_groups.values() if len(labels) > 1]
    duplicate_calls = [labels for labels in call_groups.values() if len(labels) > 1]
    errors.extend("identical exported artwork: " + ", ".join(labels) for labels in duplicates)
    errors.extend("generated source reused between icons: " + ", ".join(labels) for labels in duplicate_sources)
    errors.extend("image-generation result reused between icons: " + ", ".join(labels) for labels in duplicate_calls)
    if missing and not allow_incomplete:
        errors.append(f"{len(missing)} required files or provenance entries are missing")
    counts = Counter(entry["kind"] for entry in entries)
    return {
        "schema": 1, "passed": not errors, "complete": not missing,
        "allowIncomplete": allow_incomplete,
        "expected": {"icons": 70, "portraits": 92},
        "counts": {"icons": counts["icon"], "portraits": counts["portrait"]},
        "missing": sorted(set(missing)), "errors": errors, "warnings": warnings,
        "duplicateArtwork": duplicates, "duplicateGeneratedSources": duplicate_sources, "duplicateGenerationResults": duplicate_calls,
        "checks": ["database and manifest ID coverage", "RGBA PNG dimensions", "nonempty transparent background and icon margin", "exact 2x2 icon pixel blocks", "individual icon generation records", "generated source and export SHA-256", "92 character portraits with height 80", "unique exported artwork"],
        "assets": entries,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--report", default="docs/art-system/validation.json")
    parser.add_argument("--allow-incomplete", action="store_true", help="Progress audit only; missing artwork does not pass final completion")
    args = parser.parse_args()
    root = args.root.resolve()
    report = validate(root, args.allow_incomplete)
    report_path = root / args.report
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: report[key] for key in ("passed", "complete", "counts", "expected", "errors", "warnings")}, ensure_ascii=False, indent=2))
    print("Report: " + str(report_path))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    sys.exit(main())
