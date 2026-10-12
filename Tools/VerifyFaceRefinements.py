"""Verify surgical face edits against immutable pre-edit Git objects.

This validator proves canvas, alpha, logical pixels, Unity import metadata and
all pixels outside each reviewed edit mask are preserved. It deliberately does
not claim that automated measurements establish artistic likeness/readability;
the manifest's audit/comparison sheets and native Unity captures support that
separate visual review.
"""
from __future__ import annotations

import argparse
import hashlib
import io
import json
from pathlib import Path
import re
import subprocess
import sys

from PIL import Image, ImageChops, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
VARIANTS = {
    "default": "Assets/Resources/Lumia/PortraitsReference",
    "rare": "Assets/Resources/Lumia/PortraitsRare",
}


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def git_objects(root: Path, commit: str, paths: list[str]) -> dict[str, bytes]:
    """Read binary objects in one Git process without shell interpolation."""
    queries = [commit + ":" + path for path in paths]
    result = subprocess.run(
        ["git", "cat-file", "--batch"], cwd=root,
        input=("\n".join(queries) + "\n").encode("utf8"),
        stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True,
    )
    stream, objects = io.BytesIO(result.stdout), {}
    for path, query in zip(paths, queries):
        header = stream.readline().decode("utf8").rstrip("\n")
        if header == query + " missing":
            raise ValueError("baseline Git object is missing: " + path)
        fields = header.split()
        if len(fields) != 3 or fields[1] != "blob":
            raise ValueError("unexpected Git object header: " + header)
        count = int(fields[2])
        payload = stream.read(count)
        if len(payload) != count or stream.read(1) != b"\n":
            raise ValueError("truncated baseline Git object: " + path)
        objects[path] = payload
    return objects


def open_rgba(data: bytes, label: str) -> Image.Image:
    with Image.open(io.BytesIO(data)) as source:
        if source.format != "PNG" or source.mode != "RGBA":
            raise ValueError(label + ": explicit RGBA PNG is required")
        return source.copy()


def is_integer_grid(image: Image.Image) -> bool:
    logical = image.resize((80, 80), Image.Resampling.NEAREST)
    return logical.resize((160, 160), Image.Resampling.NEAREST).tobytes() == image.tobytes()


def changed_mask(before: Image.Image, after: Image.Image) -> Image.Image:
    # getbbox() on RGBA can ignore RGB changes where alpha is equal. Reduce
    # every difference channel explicitly so equal alpha cannot hide edits.
    channels = ImageChops.difference(before, after).split()
    mask = channels[0]
    for channel in channels[1:]:
        mask = ImageChops.lighter(mask, channel)
    return mask.point(lambda value: 255 if value else 0)


def validate(root: Path, manifest_path: Path) -> dict:
    manifest = read_json(manifest_path)
    commit = manifest.get("baselineCommit", "")
    if not re.fullmatch(r"[0-9a-fA-F]{40}", commit):
        raise ValueError("baselineCommit must be an exact 40-character Git commit SHA")
    if manifest.get("schema") != 1:
        raise ValueError("unsupported face-refinement manifest schema")
    if (manifest.get("logicalCanvas"), manifest.get("runtimeCanvas"), manifest.get("integerPixelScale")) != ([80, 80], [160, 160], 2):
        raise ValueError("the existing 80px logical / 160px runtime / exact 2x contract must be recorded")

    subjects = read_json(root / "docs/art-chibi/reference-characters.json")
    skins = read_json(root / "docs/art-rare-skins/skin-manifest.json")["skins"]
    expected = {(item["id"], "default") for item in subjects}
    expected.update((item["id"], "rare") for item in skins)
    if len(subjects) != 92 or len(skins) != 91 or len(expected) != 183:
        raise ValueError("the independently recorded 92 default / 91 rare roster changed")
    assets = manifest.get("assets", [])
    entries = {}
    errors, results, guids = [], [], set()
    for entry in assets:
        key = (entry.get("id"), entry.get("variant"))
        if key in entries:
            errors.append("duplicate manifest asset: " + str(key))
        entries[key] = entry
    if set(entries) != expected:
        errors.append("manifest roster mismatch; missing=" + str(sorted(expected - set(entries)))
                      + "; unexpected=" + str(sorted(set(entries) - expected)))
    paths = []
    for asset_id, variant in sorted(expected):
        path = VARIANTS[variant] + "/" + asset_id + ".png"
        paths += [path, path + ".meta"]
    baseline = git_objects(root, commit, paths)
    provenance = {
        "default": {entry["id"]: entry for entry in read_json(root / "docs/art-reference-chibi/asset-index.json")["assets"]},
        "rare": {entry["id"]: entry for entry in read_json(root / "docs/art-rare-skins/asset-index.json")["assets"]},
    }
    for variant, directory in VARIANTS.items():
        actual = {path.stem for path in (root / directory).glob("*.png")}
        wanted = {asset_id for asset_id, kind in expected if kind == variant}
        if actual != wanted:
            errors.append(variant + ": runtime PNG roster differs from the manifest")

    for asset_id, variant in sorted(expected):
        label = variant + "/" + asset_id
        entry = entries.get((asset_id, variant))
        if not entry:
            continue
        local_errors = []
        path = VARIANTS[variant] + "/" + asset_id + ".png"
        if entry.get("path") != path:
            local_errors.append("path does not match the runtime Resources contract")
        try:
            before_bytes, after_bytes = baseline[path], (root / path).read_bytes()
            before, after = open_rgba(before_bytes, label + " baseline"), open_rgba(after_bytes, label)
            if sha(before_bytes) != entry.get("beforeSha256"):
                local_errors.append("beforeSha256 differs from the immutable baseline")
            if sha(after_bytes) != entry.get("afterSha256"):
                local_errors.append("afterSha256 differs from the current runtime PNG")
            if before.size != (160, 160) or after.size != before.size:
                local_errors.append("canvas dimensions changed")
                raise ValueError("expected unchanged 160x160 canvases")
            if not is_integer_grid(before) or not is_integer_grid(after):
                local_errors.append("a pixel is not an exact 2x2 logical block")
            if before.getchannel("A").tobytes() != after.getchannel("A").tobytes():
                local_errors.append("alpha changed: silhouette, padding or opacity was modified")
            mask = Image.new("L", (160, 160))
            drawing = ImageDraw.Draw(mask)
            rects = entry.get("editMaskLogicalRects")
            if not isinstance(rects, list):
                raise ValueError("editMaskLogicalRects must be a list, including for unchanged assets")
            for rect in rects:
                if (not isinstance(rect, list) or len(rect) != 4
                        or any(type(value) is not int for value in rect)
                        or not (0 <= rect[0] < rect[2] <= 80 and 0 <= rect[1] < rect[3] <= 80)):
                    raise ValueError("invalid logical edit rectangle: " + str(rect))
                drawing.rectangle((rect[0] * 2, rect[1] * 2, rect[2] * 2 - 1, rect[3] * 2 - 1), fill=255)
            delta = changed_mask(before, after)
            outside = ImageChops.multiply(delta, ImageChops.invert(mask))
            if outside.getbbox():
                local_errors.append("pixels changed outside the declared reviewed face mask")
            changed_pixels = delta.histogram()[255]
            logical_changes = changed_pixels // 4
            if changed_pixels and not entry.get("changeReason"):
                local_errors.append("a changed asset lacks its character-specific changeReason")
            if changed_pixels % 4:
                local_errors.append("changed pixel count is not a whole number of logical blocks")
            metadata = (root / (path + ".meta")).read_bytes()
            if metadata != baseline[path + ".meta"]:
                local_errors.append("Unity .meta bytes changed")
            text = metadata.decode("utf8")
            guid = re.search(r"^guid: ([0-9a-f]{32})\r?$", text, re.M)
            if not guid or guid[1] in guids:
                local_errors.append("missing or duplicate Unity GUID")
            else:
                guids.add(guid[1])
            if not all(value in text for value in ("filterMode: 0", "enableMipMap: 0", "textureCompression: 0")):
                local_errors.append("point / no-mipmap / uncompressed import contract is missing")
            record = provenance[variant].get(asset_id)
            if not record or record.get("path") != path or record.get("sha256") != sha(after_bytes):
                local_errors.append("existing generation index does not record the refined runtime hash")
            generation_path = root / ("docs/art-reference-chibi" if variant == "default" else "docs/art-rare-skins") / "generation" / (asset_id + ".json")
            generation = read_json(generation_path)
            if generation != record:
                local_errors.append("per-asset generation record differs from the generation index")
            source_path = root / generation["generatedSource"]
            if sha(source_path.read_bytes()) != generation["generatedSha256"]:
                local_errors.append("preserved original image generation source changed")
            results.append({
                "id": asset_id, "variant": variant, "path": path,
                "changedLogicalPixels": logical_changes, "changedRuntimePixels": changed_pixels,
                "editMaskLogicalPixels": mask.histogram()[255] // 4,
                "alphaUnchanged": before.getchannel("A").tobytes() == after.getchannel("A").tobytes(),
                "outsideMaskUnchanged": outside.getbbox() is None,
                "metadataUnchanged": metadata == baseline[path + ".meta"],
                "beforeSha256": sha(before_bytes), "afterSha256": sha(after_bytes),
            })
        except (OSError, ValueError, KeyError) as error:
            local_errors.append(str(error))
        errors += [label + ": " + error for error in local_errors]
    return {
        "schema": 1, "passed": not errors, "baselineCommit": commit,
        "manifest": manifest_path.relative_to(root).as_posix(), "manifestSha256": sha(manifest_path.read_bytes()),
        "expectedAssets": 183, "checkedAssets": len(results),
        "defaultAssets": sum(item["variant"] == "default" for item in results),
        "rareAssets": sum(item["variant"] == "rare" for item in results),
        "changedAssets": sum(item["changedLogicalPixels"] > 0 for item in results),
        "changedLogicalPixels": sum(item["changedLogicalPixels"] for item in results),
        "logicalCanvas": [80, 80], "runtimeCanvas": [160, 160], "integerPixelScale": 2,
        "errors": errors, "assets": results,
        "checks": ["complete actual runtime Resources roster", "immutable Git before hashes", "current runtime after hashes",
                   "160x160 RGBA canvas", "exact 80x80 logical 2x grid", "byte-exact alpha preservation",
                   "byte-exact pixels outside reviewed face masks", "byte-exact Unity .meta / GUID preservation",
                   "point-filtered no-mipmap uncompressed imports", "current generation/index hashes",
                   "original generated image sources preserved"],
        "visualReview": "Character likeness, eyes and mouths must also be reviewed in the recorded comparison sheets and native Unity captures.",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--manifest", default="docs/art-face-refinement/manifest.json")
    parser.add_argument("--report", default="docs/art-face-refinement/validation.json")
    args = parser.parse_args()
    root = args.root.resolve()
    try:
        result = validate(root, root / args.manifest)
        report = root / args.report
        report.parent.mkdir(parents=True, exist_ok=True)
        report.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf8")
        for error in result["errors"]:
            print("ERROR: " + error, file=sys.stderr)
        print(("PASS" if result["passed"] else "FAIL") + ": "
              + str(result["checkedAssets"]) + "/183 runtime sprites; "
              + str(result["changedAssets"]) + " face refinements; "
              + str(result["changedLogicalPixels"]) + " changed logical pixels")
        return 0 if result["passed"] else 1
    except (OSError, ValueError, KeyError, subprocess.CalledProcessError) as error:
        print("ERROR: " + str(error), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
