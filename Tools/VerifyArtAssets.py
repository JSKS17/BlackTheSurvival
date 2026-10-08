"""Verify complete generated sprite/skill-icon coverage and processing provenance."""
from __future__ import annotations

import argparse
import json
import sys
from collections import defaultdict
from pathlib import Path

from PIL import Image

from ProcessChibiArt import KINDS, ROOT, alpha_metrics, file_digest, pixel_digest, read_json, relative, resolve, write_json


def validate(root: Path, manifest_path: Path, index_path: Path, allow_incomplete: bool) -> dict:
    manifest, index = read_json(manifest_path), read_json(index_path)
    errors, warnings = [], list(index.get("warnings", []))
    expected, expected_by_kind = {}, {}
    for kind, (group, _, _) in KINDS.items():
        items = manifest.get(group, [])
        ids = [item["id"] for item in items]
        if len(ids) != len(set(ids)):
            errors.append(f"manifest {group} contains duplicate IDs")
        expected_by_kind[kind] = set(ids)
        expected.update({(kind, item["id"]): item for item in items})
    if index.get("manifestSha256") != file_digest(manifest_path):
        errors.append("the game manifest changed after processing; rerun the processor")
    entries = index.get("assets", [])
    seen, pixels, provenance, sources = set(), defaultdict(list), defaultdict(list), {}
    counts = {kind: 0 for kind in KINDS}
    for entry in entries:
        kind, asset_id = entry.get("kind"), entry.get("id")
        label = f"{kind}/{asset_id}"
        key = (kind, asset_id)
        if kind not in KINDS:
            errors.append(f"{label}: unknown asset kind")
            continue
        if key in seen:
            errors.append(f"{label}: duplicate index entry")
        seen.add(key)
        counts[kind] += 1
        if key not in expected:
            errors.append(f"{label}: ID is absent from the game manifest")
        group, directory, default_size = KINDS[kind]
        path = resolve(root, entry.get("path", ""))
        required_path = (root / "Assets/Resources/Lumia" / directory / f"{asset_id}.png").resolve()
        if path != required_path:
            errors.append(f"{label}: export path does not match the Resources contract")
        if not path.is_file():
            errors.append(f"{label}: PNG is missing")
            continue
        try:
            with Image.open(path) as opened:
                if opened.format != "PNG":
                    errors.append(f"{label}: file is not PNG")
                if opened.mode != "RGBA":
                    errors.append(f"{label}: PNG must have explicit RGBA channels")
                image = opened.convert("RGBA")
            required_size = (int(entry.get("width", default_size)), int(entry.get("height", default_size)))
            if image.size != required_size or image.width != image.height:
                errors.append(f"{label}: wrong or nonsquare size {image.size}, expected {required_size}")
            weapon_icon=kind=='icon' and expected.get(key,{}).get('category')=='weapon' and image.size==(64,64)
            if not weapon_icon and image.size != ((128, 128) if kind == "portrait" else (96, 96)):
                warnings.append(f"{label}: uses an alternative output size {image.size}")
            metrics = alpha_metrics(image)
            if metrics["visiblePixels"] < 4:
                errors.append(f"{label}: empty image")
            if kind == "portrait":
                if not metrics["transparentPixels"]:
                    errors.append(f"{label}: portrait background is opaque")
                if metrics["borderVisiblePixels"]:
                    errors.append(f"{label}: portrait touches the canvas boundary")
                if metrics["visiblePixels"] < image.width * image.height * 0.04:
                    errors.append(f"{label}: portrait is too small to read")
                if entry.get("sourceAlpha", {}).get("borderStrongPixels", 0):
                    errors.append(f"{label}: generated source cell touches its boundary; crop requires visual repair")
            elif len(image.getcolors(image.width * image.height) or []) < 2:
                errors.append(f"{label}: skill tile is a solid colour")
            digest = pixel_digest(image)
            pixels[digest].append(label)
            if digest != entry.get("pixelSha256") or file_digest(path) != entry.get("fileSha256"):
                errors.append(f"{label}: PNG differs from the recorded processing result")
            if metrics != entry.get("outputAlpha"):
                errors.append(f"{label}: alpha statistics differ from the processing record")
        except (OSError, ValueError) as error:
            errors.append(f"{label}: unreadable PNG: {error}")
        source = resolve(root, entry.get("source", ""))
        generated_root = (root / "docs/art-chibi/generated").resolve()
        if not (source.is_relative_to(generated_root) or source.is_relative_to((root/'docs/art-system/loadout-generated').resolve())):
            errors.append(f"{label}: original generated atlas must be preserved in docs/art-chibi/generated")
        if source not in sources:
            if not source.is_file():
                sources[source] = None
            else:
                try:
                    with Image.open(source) as atlas:
                        if atlas.format != "PNG":
                            errors.append(f"{relative(root, source)}: source atlas is not PNG")
                        sources[source] = (file_digest(source), atlas.size)
                except (OSError, ValueError) as error:
                    errors.append(f"{relative(root, source)}: unreadable source atlas: {error}")
                    sources[source] = None
        source_info = sources[source]
        if source_info is None:
            errors.append(f"{label}: original generated atlas is unavailable")
        else:
            digest, source_size = source_info
            if digest != entry.get("sourceSha256"):
                errors.append(f"{label}: original generated atlas changed")
            if list(source_size) != entry.get("sourceSize"):
                errors.append(f"{label}: original atlas dimensions changed")
            box = entry.get("cropBox", [])
            if (len(box) != 4 or box[0] < 0 or box[1] < 0 or box[2] > source_size[0]
                    or box[3] > source_size[1] or box[0] >= box[2] or box[1] >= box[3]):
                errors.append(f"{label}: invalid source crop coordinates")
            columns, rows = int(entry.get("columns", 0)), int(entry.get("rows", 0))
            if not (columns > 0 and rows > 0 and 0 <= int(entry.get("column", -1)) < columns
                    and 0 <= int(entry.get("row", -1)) < rows):
                errors.append(f"{label}: invalid source grid position")
            provenance[(str(source), tuple(box))].append(label)
        if not entry.get("batch"):
            errors.append(f"{label}: source batch ID is missing")
    missing = sorted(f"{kind}/{asset_id}" for kind, asset_id in set(expected) - seen)
    if missing and not allow_incomplete:
        errors.append(f"{len(missing)} required assets are missing")
    duplicate_pixels = [labels for labels in pixels.values() if len(labels) > 1]
    duplicate_crops = [labels for labels in provenance.values() if len(labels) > 1]
    for labels in duplicate_pixels:
        errors.append("identical pixel images: " + ", ".join(labels))
    for labels in duplicate_crops:
        errors.append("reused atlas cell: " + ", ".join(labels))
    return {
        "schema": 1, "passed": not errors, "complete": not missing,
        "counts": counts, "expectedCounts": {kind: len(ids) for kind, ids in expected_by_kind.items()},
        "totalAssets": len(seen), "sourceAtlases": len(sources), "missing": missing,
        "duplicatePixelGroups": duplicate_pixels, "duplicateCropGroups": duplicate_crops,
        "errors": errors, "warnings": list(dict.fromkeys(warnings)),
        "checks": ["full game-manifest coverage", "RGBA PNG dimensions", "nonempty readable sprites", "transparent sprite backgrounds",
                   "sprite boundary separation", "file and pixel SHA-256", "unique pixels and source cells", "original generated-atlas provenance"],
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--manifest", default="docs/art-chibi/game-art-manifest.json")
    parser.add_argument("--index", default="docs/art-chibi/processed-art-index.json")
    parser.add_argument("--report", default="docs/art-chibi/art-validation.json")
    parser.add_argument("--allow-incomplete", action="store_true", help="Use for partial progress only; final verification must omit this flag")
    args = parser.parse_args()
    root = args.root.resolve()
    try:
        result = validate(root, resolve(root, args.manifest), resolve(root, args.index), args.allow_incomplete)
        write_json(resolve(root, args.report), result)
        for kind, count in result["counts"].items():
            print(f"{kind}: {count}/{result['expectedCounts'][kind]}")
        for error in result["errors"]:
            print(f"ERROR: {error}", file=sys.stderr)
        for warning in result["warnings"]:
            print(f"WARNING: {warning}", file=sys.stderr)
        print(f"{'PASS' if result['passed'] else 'FAIL'}: {result['totalAssets']} assets from {result['sourceAtlases']} generated atlases; complete={result['complete']}")
        return 0 if result["passed"] else 1
    except (OSError, ValueError, KeyError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
