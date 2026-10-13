"""Validate and package the standalone art gallery; never build or modify the game."""
import hashlib
import json
import subprocess
import zipfile
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / "ArtSource/AnimationPrototypes"
EVIDENCE = ROOT / "docs/validation-ultimate-collection"
ARCHIVE = ROOT / "Build/BlackTheSurvival-UltimateAnimationPrototypes-v1.zip"

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    roster = json.loads((BASE / "roster.json").read_text(encoding="utf-8-sig"))
    catalog = json.loads((BASE / "catalog.json").read_text(encoding="utf-8-sig"))
    assert len(roster) == len(catalog) == 91
    assert {x["id"] for x in roster} == {x["id"] for x in catalog}
    assert all(x["ready"] for x in catalog), "The gallery still contains pending entries."
    proof = []
    for item in catalog:
        prefix = BASE / ("Nia" if item["id"] == "nia" else "Subjects/" + item["id"])
        if item["id"] != "nia":
            meta = json.loads((prefix / "manifest.json").read_text(encoding="utf-8-sig"))
            report = json.loads((prefix / "validation.json").read_text(encoding="utf-8-sig"))
            assert report["passed"] and report["assembly_version"] >= 4
            assert report["atlas_sha256"] == meta["atlas_sha256"] == sha(prefix / "atlas.png")
            assert report["manifest_sha256"] == sha(prefix / "manifest.json")
            if meta.get("generated_source_file"):
                raw = prefix / meta["generated_source_file"]
                normalization = json.loads((prefix / meta["normalization"]).read_text(encoding="utf-8"))
                assert sha(raw) == meta["generated_source_sha256"] == normalization["generated_source_sha256"]
                assert normalization["source_rgba_preserved"] and normalization["source_boxes_disjoint"]
                assert normalization["atlas_sha256"] == meta["atlas_sha256"]
            else:
                assert sha(Path(meta["generated_path"])) == meta["atlas_sha256"], item["id"]
            assert meta["tool"] == "built-in image_gen.imagegen" and len(meta["frame_labels"]) == 12
            assert meta["sources"] and (prefix / "prompt.txt").stat().st_size > 100
            for name, digest in report["animation_sha256"].items():
                assert sha(prefix / name) == digest, item["id"]
        durations = []
        for kind in ["gif", "webp"]:
            with Image.open(BASE / item[kind]) as animation:
                assert animation.n_frames == 12
                for frame in range(12):
                    animation.seek(frame)
                    assert animation.convert("RGBA").getchannel("A").getextrema()[0] == 0
                    if kind == "gif": durations.append(animation.info["duration"])
        assert sum(durations) == 1500
        proof.append(dict(id=item["id"], name=item["name"], frames=12, duration_ms=1500,
                          atlas_sha256=sha(BASE / item["atlas"]), gif_sha256=sha(BASE / item["gif"]),
                          webp_sha256=sha(BASE / item["webp"])))
    baseline = "c41c48550b159a1c3e3159b12a66ff8280901fbb"
    game_diff = subprocess.check_output(["git", "diff", "--name-only", baseline, "--", "Assets", "Packages", "ProjectSettings"], cwd=ROOT, text=True)
    assert not game_diff.strip(), game_diff
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    collection = dict(passed=True, total_subjects=91, newly_created=90, retained_nia=1,
                      total_animation_frames=1092, frames_per_subject=12, fps=8, duration_ms=1500,
                      raw_generated_bytes_preserved=True, game_integration=False,
                      game_files_unchanged_from=baseline, subjects=proof)
    (EVIDENCE / "collection-validation.json").write_text(json.dumps(collection, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    files = sorted(path for path in BASE.rglob("*") if path.is_file())
    ARCHIVE.parent.mkdir(exist_ok=True)
    with zipfile.ZipFile(ARCHIVE, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for path in files:
            archive.write(path, "BlackTheSurvival-UltimateAnimationPrototypes/" + path.relative_to(BASE).as_posix())
    with zipfile.ZipFile(ARCHIVE) as archive:
        assert archive.testzip() is None
        for path in files:
            entry = "BlackTheSurvival-UltimateAnimationPrototypes/" + path.relative_to(BASE).as_posix()
            assert hashlib.sha256(archive.read(entry)).hexdigest() == sha(path)
        assert not any("/Assets/" in name or name.endswith(".exe") for name in archive.namelist())
    commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
    archive_report = dict(passed=True, name=ARCHIVE.name, sha256=sha(ARCHIVE), bytes=ARCHIVE.stat().st_size,
                          files=len(files), localCommit=commit, standalone=True, game_integration=False)
    (ROOT / "Temp/github-ultimate-archive-validation.json").write_text(json.dumps(archive_report, indent=2)+"\n", encoding="utf-8")
    print(json.dumps(archive_report, indent=2))

if __name__ == "__main__":
    main()
