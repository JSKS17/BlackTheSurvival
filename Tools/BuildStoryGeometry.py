"""Read transparent PNGs and export crop metadata; never change image pixels."""
import hashlib
import json
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
folder = root / "Assets/Resources/Lumia/Story/Characters"
records = []
for path in sorted(folder.glob("*.png")):
    with Image.open(path) as image:
        assert image.mode == "RGBA", path
        alpha = image.getchannel("A")
        assert alpha.getextrema() == (0, 255), path
        bounds = alpha.point(lambda value: 255 if value >= 32 else 0).getbbox()
        assert bounds, path
        records.append(dict(id=path.stem, width=image.width, height=image.height,
                            left=bounds[0], top=bounds[1], right=bounds[2], bottom=bounds[3]))
    manifest = json.loads((root / f"docs/art-layered-story/characters/{path.stem}.json").read_text(encoding="utf-8-sig"))
    assert hashlib.sha256(path.read_bytes()).hexdigest() == manifest["generated_sha256"] == manifest["output_sha256"], path
assert len(records) == 12
output = {"alphaThreshold": 32, "characters": records}
(folder / "geometry.json").write_bytes((json.dumps(output, indent=2) + "\n").encode("utf-8"))
print("PASS: 12 original PNG hashes/alpha channels; crop metadata exported without image changes.")
