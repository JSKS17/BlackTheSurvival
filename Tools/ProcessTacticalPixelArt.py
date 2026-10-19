"""Format-only processing of individually generated tactical pixel icons.

No repainting, quantization, sharpening, smoothing or background removal.
Original reference PNGs and generated PNGs remain byte-for-byte intact.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "docs/art-tactical-pixel"


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def relative(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def normalized(source: Path, module: bool) -> Image.Image:
    image = Image.open(source).convert("RGBA")
    if module:
        # Exactly the established ProcessSystemArt.py transform; keep generated alpha.
        if not image.getchannel("A").getextrema()[0] < 255:
            raise ValueError(f"Module source is opaque: {source}")
        content = image.resize((28, 28), Image.Resampling.NEAREST)
        padded = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
        padded.paste(content, (2, 2))
        return padded.resize((64, 64), Image.Resampling.NEAREST)
    # Exactly the established ProcessChibiArt.normalize_icon convention.
    scale = min(96 / image.width, 96 / image.height)
    width, height = max(1, round(image.width * scale)), max(1, round(image.height * scale))
    resized = image.resize((width, height), Image.Resampling.NEAREST)
    corner = image.getpixel((0, 0))
    canvas = Image.new("RGBA", (96, 96), corner if corner[3] == 255 else (18, 27, 43, 255))
    canvas.paste(resized, ((96 - width) // 2, (96 - height) // 2))
    return canvas


def process(finalize: bool):
    snapshot = read(OUT / "references/original-icons-manifest.json")
    originals = snapshot["entries"]
    if len(originals) != 27:
        raise ValueError(f"Expected 27 originals, got {len(originals)}")
    preview = OUT / "runtime-preview"
    preview.mkdir(parents=True, exist_ok=True)
    entries, missing = [], []
    for original in originals:
        asset_id = original.get("cardId") or original["itemId"]
        reference = OUT / "references" / f"{asset_id}.png"
        generated = OUT / "generated" / f"{asset_id}.png"
        record = OUT / "generation" / f"{asset_id}.json"
        if digest(reference) != original["sha256"]:
            raise ValueError(f"Original reference bytes changed: {asset_id}")
        if not generated.exists() or not record.exists():
            missing.append(asset_id)
            continue
        metadata = read(record)
        if metadata["id"] != asset_id or metadata["tool"] != "built-in image_gen":
            raise ValueError(f"Invalid generation provenance: {asset_id}")
        source_hash = digest(generated)
        if source_hash == original["sha256"]:
            raise ValueError(f"Original icon copied as generated artwork: {asset_id}")
        tool_output = Path(metadata["originalGeneratedPath"])
        if not tool_output.is_file() or digest(tool_output) != source_hash:
            raise ValueError(f"Preserved generated PNG differs from tool output: {asset_id}")
        runtime = ROOT / original["targetPath"]
        module = asset_id == "tactical_module"
        image = normalized(generated, module)
        candidate = preview / f"{asset_id}.png"
        image.save(candidate)
        alpha = image.getchannel("A").histogram()
        if not module and alpha[255] != 96 * 96:
            raise ValueError(f"Tactical skill tile must be opaque: {asset_id}")
        meta = runtime.with_suffix(runtime.suffix + ".meta")
        meta_text = meta.read_text(encoding="utf-8")
        guid = re.search(r"^guid: (\w+)$", meta_text, re.MULTILINE).group(1)
        if not re.search(r"filterMode: 0\b", meta_text) or not re.search(r"enableMipMap: 0\b", meta_text):
            raise ValueError(f"Pixel texture importer must use Point and no mipmaps: {asset_id}")
        processing = ("ProcessSystemArt convention: nearest resize generated full canvas to 28x28; "
                      "transparent 2px padding inside 32x32; nearest resize to 64x64; generated alpha preserved."
                      if module else "ProcessChibiArt.normalize_icon convention: full generated tile nearest resize to 96x96; aspect-preserving letterbox only if needed.")
        entries.append(dict(id=asset_id, nameKo=original["originalNameKo"], referencePath=relative(reference),
                            originalSha256=original["sha256"], generatedPath=relative(generated),
                            generatedSha256=source_hash, runtimePath=relative(runtime),
                            toolOutputPath=str(tool_output),
                            runtimeSha256=digest(candidate), size=list(image.size), generationRecordPath=relative(record),
                            processing=processing, preservedGuid=guid, transparentPixels=alpha[0],
                            filterMode="Point", mipmaps=False,
                            partialAlphaPixels=sum(alpha[1:255]), colorCount=len(image.getcolors(image.width * image.height) or [])))
    if finalize and missing:
        raise ValueError("Missing generated icons: " + ", ".join(missing))
    # Runtime PNGs are replaced only after all source/record/reference checks above pass.
    if finalize:
        for entry in entries:
            runtime = ROOT / entry["runtimePath"]
            runtime.write_bytes((preview / f"{entry['id']}.png").read_bytes())
            if digest(runtime) != entry["runtimeSha256"]:
                raise ValueError(f"Runtime write verification failed: {entry['id']}")
            meta = runtime.with_suffix(runtime.suffix + ".meta")
            guid = re.search(r"^guid: (\w+)$", meta.read_text(encoding="utf-8"), re.MULTILINE).group(1)
            if guid != entry["preservedGuid"]:
                raise ValueError(f"Unity GUID changed: {entry['id']}")
    make_sheet(entries)
    result = dict(schemaVersion=1, result="PASS" if finalize else "PREVIEW", tool="built-in image_gen",
                  expectedCount=27, completedCount=len(entries), missing=missing, entries=entries,
                  contactSheetPath="docs/art-tactical-pixel/tactical-pixel-contact-sheet.png")
    manifest = OUT / ("output-manifest.json" if finalize else "preview-manifest.json")
    manifest.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"{result['result']}: {len(entries)}/27; " + relative(manifest))


def make_sheet(entries):
    font = ImageFont.truetype(str(ROOT / "Assets/Resources/Lumia/Galmuri11.ttf"), 14)
    label_font = ImageFont.truetype(str(ROOT / "Assets/Resources/Lumia/Galmuri11.ttf"), 12)
    columns, cell_w, cell_h = 5, 240, 154
    rows = max(1, (len(entries) + columns - 1) // columns)
    sheet = Image.new("RGB", (columns * cell_w, 58 + rows * cell_h), "#111b2b")
    draw = ImageDraw.Draw(sheet)
    draw.text((12, 8), "TACTICAL PIXEL ART / ORIGINAL -> GENERATED", fill="white", font=font)
    draw.text((12, 30), "F skills: 96px opaque | Module: 64px alpha | Nearest-neighbour only", fill="#95b2c9", font=label_font)
    for index, entry in enumerate(entries):
        x, y = (index % columns) * cell_w, 58 + (index // columns) * cell_h
        reference = Image.open(ROOT / entry["referencePath"]).convert("RGBA").resize((96, 96), Image.Resampling.NEAREST)
        output = Image.open(OUT / "runtime-preview" / f"{entry['id']}.png").convert("RGBA").resize((96, 96), Image.Resampling.NEAREST)
        sheet.paste(reference, (x + 10, y + 4), reference)
        sheet.paste(output, (x + 126, y + 4), output)
        draw.text((x + 110, y + 42), ">", fill="#75dcce", font=font)
        draw.text((x + 8, y + 105), entry["nameKo"], fill="white", font=font)
        draw.text((x + 8, y + 127), entry["id"].replace("tactical_", ""), fill="#95b2c9", font=label_font)
    sheet.save(OUT / "tactical-pixel-contact-sheet.png")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--finalize", action="store_true")
    process(parser.parse_args().finalize)
