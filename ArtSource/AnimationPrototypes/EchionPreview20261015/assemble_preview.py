"""Preview-only crop / integer translation / transparent-padding assembly.

No repainting, resampling, alpha cleanup, or writes to runtime assets.
"""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
FOLDER = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "Tools"))
from PIL import Image
import BuildUltimatePreviews as builder
import NormalizeUltimateAtlas as normalizer


def clear_alpha_cuts(image, axis, sections):
    """Require an actual clear gutter, including every translucent pixel."""
    width, height = image.size
    length = height if axis == "y" else width
    alpha = image.getchannel("A")
    counts = []
    for position in range(length):
        box = (0, position, width, position + 1) if axis == "y" else (position, 0, position + 1, height)
        counts.append(sum(alpha.crop(box).histogram()[1:]))
    cuts = [0]
    for index in range(1, sections):
        expected = round(length * index / sections)
        radius = round(length / sections * .22)
        low, high = max(1, expected - radius), min(length - 1, expected + radius)
        clear = [position for position in range(low, high + 1) if not counts[position]]
        runs = []
        for position in clear:
            if not runs or position != runs[-1][-1] + 1:
                runs.append([position])
            else:
                runs[-1].append(position)
        runs = [run for run in runs if len(run) >= 2]
        assert runs, (axis, index, "No fully transparent two-pixel gutter")
        # The widest truly empty run avoids cutting beside stray translucent FX.
        run = min(runs, key=lambda run: (-len(run), abs((run[0] + run[-1]) / 2 - expected)))
        cut = (run[0] + run[-1] + 1) // 2
        assert counts[cut - 1] == counts[cut] == 0
        cuts.append(cut)
    return cuts + [length]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


if __name__ == "__main__":
    runtime = ROOT / "Assets/Resources/Lumia/UltimateAnimations/echion.png"
    runtime_before = digest(runtime)
    # Verify the actual runtime remains byte-for-byte the tracked game version.
    tracked_runtime = subprocess.check_output(
        ["git", "show", "HEAD:Assets/Resources/Lumia/UltimateAnimations/echion.png"], cwd=ROOT)
    tracked_hash = hashlib.sha256(tracked_runtime).hexdigest()
    assert runtime_before == tracked_hash
    normalizer.constrained_transparent_cuts = clear_alpha_cuts
    result = normalizer.normalize(FOLDER / "generated-sheet.png", FOLDER / "atlas.png", FOLDER / "manifest.json")
    assert all(item["alpha_nonzero"] == 0 for item in result["internal_boundaries"])
    result["partition_method"] = "Widest fully alpha-zero gutter within nominal +/-22%; every translucent pixel participates"
    (FOLDER / "normalization.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(builder.assemble(FOLDER))
    manifest = json.loads((FOLDER / "manifest.json").read_text(encoding="utf-8"))
    work = ROOT / "Temp/UltimateAssembly" / manifest["id"]
    (FOLDER / "frames").mkdir(exist_ok=True)
    for index in range(12):
        shutil.copyfile(work / f"frame-{index:02d}.png", FOLDER / "frames" / f"frame-{index:02d}.png")
    proof = dict(
        runtime_asset=str(runtime.relative_to(ROOT)),
        tracked_head_sha256=tracked_hash,
        sha256_before_assembly=runtime_before,
        sha256_after_assembly=digest(runtime),
        unchanged=runtime_before == digest(runtime),
        game_integrated=False,
        source_art_edited_in_code=False,
        every_translucent_pixel_in_partition_audit=True,
        all_partition_bands_completely_transparent=True,
        only_operations="crop, integer translation, transparent padding, GIF/WebP encoding",
    )
    assert proof["unchanged"]
    (FOLDER / "scope-verification.json").write_text(json.dumps(proof, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(proof))
