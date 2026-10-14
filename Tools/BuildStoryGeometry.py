"""Read transparent PNGs and export crop metadata; never change image pixels."""
import hashlib
import json
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
folder = root / "Assets/Resources/Lumia/Story/Characters"
records = []
current_overrides = {}
for version in ("1.0.1", "1.0.2", "1.0.3"):
    current_art = json.loads((root / f"docs/art-refresh-v{version}/manifest.json").read_text(encoding="utf-8-sig"))
    current_overrides.update({item["output"]: item["sha256"] for item in current_art["items"]})
for path in sorted(folder.glob("*.png")):
    with Image.open(path) as image:
        assert image.mode == "RGBA", path
        alpha = image.getchannel("A")
        assert alpha.getextrema() == (0, 255), path
        bounds = alpha.point(lambda value: 255 if value >= 32 else 0).getbbox()
        assert bounds, path
        records.append(dict(id=path.stem, width=image.width, height=image.height,
                            left=bounds[0], top=bounds[1], right=bounds[2], bottom=bounds[3]))
    override = current_overrides.get(path.relative_to(root).as_posix())
    if override:
        assert hashlib.sha256(path.read_bytes()).hexdigest() == override, path
    else:
        manifest = json.loads((root / f"docs/art-layered-story/characters/{path.stem}.json").read_text(encoding="utf-8-sig"))
        assert hashlib.sha256(path.read_bytes()).hexdigest() == manifest["generated_sha256"] == manifest["output_sha256"], path
assert len(records) == 12
output = {"alphaThreshold": 32, "characters": records}
geometry_path = folder / "geometry.json"
line_ending = "\r\n" if geometry_path.exists() and b"\r\n" in geometry_path.read_bytes() else "\n"
geometry_path.write_bytes((json.dumps(output, indent=2) + "\n").replace("\n", line_ending).encode("utf-8"))
print("PASS: 12 original PNG hashes/alpha channels; crop metadata exported without image changes.")
