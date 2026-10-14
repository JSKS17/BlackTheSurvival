"""Read-only validation of the four authored wildlife portraits; never edits pixels."""
import hashlib
import json
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "docs/art-refresh-v1.0.2/wildlife/manifest.json"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def validate():
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    assert manifest["tool"] == "built-in image_gen.imagegen"
    assert [item["id"] for item in manifest["items"]] == ["wolf", "boar", "bear", "chicken"]
    assert digest(ROOT / "Assets/Resources/Lumia/CharacterAtlas.png") == manifest["unchangedCharacterAtlasSha256"], "Existing atlas, including dog, changed"
    metrics = []
    for item in manifest["items"]:
        path = ROOT / item["output"]
        assert digest(path) == item["sha256"], item["id"] + " file hash changed"
        with Image.open(path) as image:
            assert image.mode == "RGBA" and image.format == "PNG"
            assert list(image.size) == item["dimensions"] == [1254, 1254]
            alpha = image.getchannel("A")
            hist = alpha.histogram()
            assert hist[0] > image.width * image.height // 5, "Missing transparent backdrop"
            assert sum(hist[32:]) > image.width * image.height // 10, "Empty animal"
            bounds = alpha.point(lambda a: 255 if a >= 32 else 0).getbbox()
            assert list(bounds) == item["alpha32Bounds"]
            assert bounds[0] > 0 and bounds[1] > 0 and bounds[2] < image.width and bounds[3] < image.height, "Clipped silhouette"
            metrics.append({"id": item["id"], "dimensions": list(image.size), "alpha32Bounds": list(bounds), "sha256": digest(path)})
        meta = path.with_suffix(".png.meta").read_text(encoding="utf-8-sig")
        for required in ("filterMode: 0", "enableMipMap: 0", "alphaIsTransparency: 1", "nPOTScale: 0", "textureCompression: 0"):
            assert required in meta, item["id"] + " importer missing " + required
        for reference in item["references"]:
            assert (ROOT / reference["path"]).is_file()
            assert digest(ROOT / reference["path"]) == reference["sha256"]
    pixel_art = (ROOT / "Assets/Scripts/Lumia/PixelArt.cs").read_text(encoding="utf-8-sig")
    assert 'var wildlife = Load("Wildlife/" + key);' in pixel_art
    assert 'wildlife ?? identity' in pixel_art
    assert 'if(wildlife!=null || identity==null' in pixel_art
    return {"status": "PASS", "animals": metrics, "logicalPixelHeight": 80, "oldAtlasUnchanged": True, "rasterPostprocessing": False}


if __name__ == "__main__":
    print(json.dumps(validate(), ensure_ascii=False, indent=2))
