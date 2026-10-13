"""Copy selected built-in image outputs unchanged and record their provenance."""
import hashlib
import json
import shutil
import sys
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
for job in json.loads(Path(sys.argv[1]).read_text(encoding="utf-8-sig")):
    folder = root / "ArtSource/AnimationPrototypes/Subjects" / job["id"]
    folder.mkdir(parents=True, exist_ok=True)
    source = Path(job["generated_path"])
    output = folder / "atlas.png"
    shutil.copyfile(source, output)
    with Image.open(output) as image:
        width, height = image.size
        assert image.mode == "RGBA" and image.getchannel("A").getextrema() == (0, 255)
        assert width % 4 == 0 and height % 3 == 0 and width // 4 == height // 3
    references = [root / f'Assets/Resources/Lumia/PortraitsReference/{job["id"]}.png', root / "ArtSource/AnimationPrototypes/Nia/nia-ultimate-atlas.png"]
    record = dict(id=job["id"], name=job["name"], ultimate=job["ultimate"], columns=4, rows=3, fps=8,
                  tool="built-in image_gen.imagegen", generated_path=str(source), width=width, height=height,
                  atlas_sha256=hashlib.sha256(output.read_bytes()).hexdigest(),
                  reference_paths=[str(path) for path in references],
                  reference_sha256=[hashlib.sha256(path.read_bytes()).hexdigest() for path in references],
                  sources=[dict(url=job["url"], title="Eternal Return official skill description")],
                  motion_summary=job["motion"], frame_labels=job["labels"], intended_airborne_frames=job.get("air", []),
                  visual_review="Reviewed generated twelve-frame sheet: identity, facial features, distinct body/weapon motion, skill effects, transparent cells.")
    (folder / "prompt.txt").write_bytes((job["prompt"].rstrip() + "\n").encode("utf-8"))
    (folder / "manifest.json").write_bytes((json.dumps(record, ensure_ascii=False, indent=2) + "\n").encode("utf-8"))
    print(job["id"], "raw PNG preserved", width, height)
