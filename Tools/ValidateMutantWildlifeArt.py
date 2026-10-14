"""Read-only checks for authored mutant wildlife, their provenance and 80-pixel readability.

Never repairs, saves, recolors or otherwise edits a raster. Native wildlife coverage
also compares the actual Unity-rendered normal/mutant 80-pixel grids for all five
species, including the ordinary dog's unchanged legacy atlas portrait.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SPECIES = ("wolf", "boar", "bear", "chicken", "dog")
EXPECTED = {"mutant_" + animal for animal in SPECIES}


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def source_grid(path: Path):
    with Image.open(path) as opened:
        assert opened.format == "PNG" and opened.mode == "RGBA", str(path) + ": RGBA PNG required"
        image = opened.copy()
    alpha = image.getchannel("A")
    strong = alpha.point(lambda value: 255 if value >= 32 else 0)
    bounds = strong.getbbox()
    assert bounds, str(path) + ": empty silhouette"
    histogram = alpha.histogram()
    assert histogram[0] > image.width * image.height // 8, str(path) + ": missing transparent background"
    assert sum(histogram[32:]) > image.width * image.height // 10, str(path) + ": insufficient visible body"
    assert 0 < bounds[0] < bounds[2] < image.width and 0 < bounds[1] < bounds[3] < image.height, str(path) + ": clipped silhouette"
    # In-memory analysis only. Runtime downsamples with Point; authored PNG bytes stay intact.
    grid = np.asarray(image.resize((80, 80), Image.Resampling.NEAREST), dtype=np.int16)
    visible = grid[:, :, 3] >= 32
    rows, columns = np.where(visible)
    logical_bounds = [int(columns.min()), int(rows.min()), int(columns.max() + 1), int(rows.max() + 1)]
    return image, grid, list(bounds), logical_bounds


def validate(manifest_path: Path, coverage_path: Path | None = None):
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    assert manifest["tool"] == "built-in image_gen.imagegen", "Authored art must preserve built-in generation provenance"
    items = manifest["items"]
    assert len(items) == 5 and {item["id"] for item in items} == EXPECTED, "Exactly five mutant species are required"
    ordinary = json.loads((ROOT / "docs/art-refresh-v1.0.2/wildlife/manifest.json").read_text(encoding="utf-8-sig"))
    assert digest(ROOT / "Assets/Resources/Lumia/CharacterAtlas.png") == ordinary["unchangedCharacterAtlasSha256"], "Ordinary dog atlas changed"
    for item in ordinary["items"]:
        assert digest(ROOT / item["output"]) == item["sha256"], "Ordinary wildlife changed: " + item["id"]
    metrics, hashes = [], set()
    for item in items:
        animal = item["id"].removeprefix("mutant_")
        expected = "Assets/Resources/Lumia/Wildlife/mutant_" + animal + ".png"
        assert item["output"].replace("\\", "/") == expected, "Resources path differs from runtime lookup"
        path = ROOT / expected
        checksum = digest(path)
        assert checksum == item["sha256"], item["id"] + ": PNG hash changed"
        assert checksum not in hashes, item["id"] + ": duplicate mutant asset"
        hashes.add(checksum)
        image, grid, bounds, logical_bounds = source_grid(path)
        assert list(image.size) == item["dimensions"], item["id"] + ": source dimensions differ from manifest"
        assert image.width == image.height and image.height >= 80, item["id"] + ": square source required for 80 x 80 presentation"
        assert bounds == item["alpha32Bounds"], item["id"] + ": alpha bounds differ from manifest"
        meta = path.with_suffix(".png.meta").read_text(encoding="utf-8-sig")
        for required in ("filterMode: 0", "enableMipMap: 0", "alphaIsTransparency: 1", "nPOTScale: 0", "textureCompression: 0"):
            assert required in meta, item["id"] + ": importer missing " + required
        assert item["references"], item["id"] + ": original-game reference record required"
        for reference in item["references"]:
            reference_path = ROOT / reference["path"]
            assert reference_path.is_file() and digest(reference_path) == reference["sha256"], item["id"] + ": reference missing or changed"
            assert reference.get("url", "").startswith("https://"), item["id"] + ": reference URL missing"
        record = {"id": item["id"], "sha256": checksum, "dimensions": list(image.size), "alpha32Bounds": bounds,
                  "logicalGrid": [80, 80], "alpha80Bounds": logical_bounds}
        if animal != "dog":
            _, normal, _, normal_bounds = source_grid(ROOT / "Assets/Resources/Lumia/Wildlife" / (animal + ".png"))
            visible = (normal[:, :, 3] >= 32) | (grid[:, :, 3] >= 32)
            difference = np.sum(np.abs(normal - grid), axis=2)
            changed = int(np.count_nonzero(visible & (difference > 32)))
            assert changed > 64, item["id"] + ": mutation not distinct at the actual 80-pixel grid"
            record.update({"differentLogicalPixels": changed, "normalAlpha80Bounds": normal_bounds,
                           "visibleHeightChange80": (logical_bounds[3] - logical_bounds[1]) - (normal_bounds[3] - normal_bounds[1]),
                           "groundPositionChange80": logical_bounds[3] - normal_bounds[3]})
        else:
            record["ordinaryPortrait"] = "Unchanged 334 x 348 legacy atlas crop; native runner compares its rendered 80-pixel sample"
        metrics.append(record)
    native = None
    if coverage_path is not None:
        native = json.loads(coverage_path.read_text(encoding="utf-8-sig"))
        assert native["result"] == "PASS" and native["screenshots"] == 18, "Native wildlife coverage did not complete all existing cases"
        for flag in ("dedicatedMutantPortraits", "distinctMutantLogicalPixels", "mutantPortraitsUse80Grid",
                     "stableMutationPortraitAfterReload", "ordinaryPortraitIdentityPreserved", "personalSavesUnchanged", "renderDoesNotChangeState"):
            assert native[flag] is True, "Native coverage flag failed: " + flag
        mutants = [animal for animal in native["animals"] if animal["mutant"]]
        assert {animal["animal"] for animal in mutants} == set(SPECIES), "Native coverage missed a mutant species"
        for animal in mutants:
            assert animal["logicalWidth"] == animal["logicalHeight"] == 80
            assert animal["comparedLogicalPixels"] == 6400 and animal["differentLogicalPixels"] > 64
            assert animal["battleRectWidth"] == 218 and animal["battleRectHeight"] == 216
            assert animal["portraitResource"] == "Lumia/Wildlife/mutant_" + animal["animal"]
    return {"status": "PASS", "mutants": metrics, "ordinaryWildlifeUnchanged": True,
            "logicalPixelHeight": 80, "rasterPostprocessing": False, "nativeCoverageChecked": native is not None}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, default=ROOT / "docs/art-refresh-v1.0.3/wildlife/manifest.json")
    parser.add_argument("--coverage", type=Path)
    options = parser.parse_args()
    print(json.dumps(validate(options.manifest, options.coverage), ensure_ascii=False, indent=2))
