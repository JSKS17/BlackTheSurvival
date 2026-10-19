"""Copy the approved R atlases into Unity without changing their image bytes.

The gallery is the single source of frame boxes and canvas offsets. No generated
sheet, preview GIF/WebP, character reference or image-processing step is shipped.
Run this before Unity imports/builds; --check verifies the installed data only.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import struct
import uuid


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource/AnimationPrototypes"
DESTINATION = ROOT / "Assets/Resources/Lumia/UltimateAnimations"
AUDIT = ROOT / "docs/runtime-ultimate-assets.json"
GUID_NAMESPACE = uuid.UUID("a9292088-9d27-4a30-977c-d9a7fb481543")


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def png_size(path: Path) -> tuple[int, int]:
    with path.open("rb") as stream:
        header = stream.read(24)
    assert header[:8] == b"\x89PNG\r\n\x1a\n" and header[12:16] == b"IHDR", path
    return struct.unpack(">II", header[16:24])


def guid(asset_path: Path) -> str:
    return uuid.uuid5(GUID_NAMESPACE, asset_path.relative_to(ROOT).as_posix()).hex


def folder_meta(path: Path) -> str:
    return (f"fileFormatVersion: 2\nguid: {guid(path)}\nfolderAsset: yes\n"
            "DefaultImporter:\n  externalObjects: {}\n  userData: \n"
            "  assetBundleName: \n  assetBundleVariant: \n")


def text_meta(path: Path) -> str:
    return (f"fileFormatVersion: 2\nguid: {guid(path)}\nTextScriptImporter:\n"
            "  externalObjects: {}\n  userData: \n"
            "  assetBundleName: \n  assetBundleVariant: \n")


def texture_meta(path: Path) -> str:
    # Same format as the project's Unity 6 TextureImporter. These rules also
    # agree with LumiaProjectSetup.PixelImportRules and survive its build pass.
    template = (ROOT / "Assets/Resources/Lumia/SystemIcons/wildlife.png.meta").read_text(encoding="utf-8-sig")
    template = re.sub(r"^guid: \w+$", f"guid: {guid(path)}", template, flags=re.M)
    template = re.sub(r"^(\s*)maxTextureSize: \d+$", r"\g<1>maxTextureSize: 4096", template, flags=re.M)
    template = re.sub(r"^(\s*)textureCompression: \d+$", r"\g<1>textureCompression: 0", template, flags=re.M)
    template = re.sub(r"^(\s*)spriteMode: \d+$", r"\g<1>spriteMode: 0", template, flags=re.M)
    return template


def build_catalog() -> tuple[dict, list[dict]]:
    source_catalog = read_json(SOURCE / "catalog.json")
    assert len(source_catalog) == 91, "Expected all 91 approved R animations"
    ids = [item["id"] for item in source_catalog]
    assert len(set(ids)) == len(ids)
    items, audit = [], []
    for source_item in source_catalog:
        subject_id = source_item["id"]
        assert re.fullmatch(r"[a-z][a-z0-9_]*", subject_id), subject_id
        assert source_item["ready"], subject_id
        source_path = (SOURCE / source_item["atlas"]).resolve()
        assert source_path.is_relative_to(SOURCE.resolve()), source_path
        width, height = png_size(source_path)
        assert [width, height] == source_item["atlas_size"], subject_id
        assert max(width, height) <= 4096, "Atlas exceeds project import size"
        validation_path = source_path.parent / "validation.json"
        validation = read_json(validation_path)
        source_hash = sha256(source_path)
        if subject_id == "nia":
            assert validation["atlas"]["sha256"] == source_hash
            assert validation["checks"]["frame_extraction_exact_rgba"]
            fps = validation["animation"]["fps"]
        else:
            assert validation["passed"] and validation["original_bytes_preserved"], subject_id
            assert validation["atlas_sha256"] == source_hash, subject_id
            assert validation["boxes"] == source_item["boxes"], subject_id
            assert validation["offsets"] == source_item["offsets"], subject_id
            assert validation["canvas"] == source_item["canvas"], subject_id
            fps = validation["fps"]
        boxes, offsets = source_item["boxes"], source_item["offsets"]
        canvas = source_item["canvas"]
        assert len(boxes) == len(offsets) == 12 and fps == 8, subject_id
        frames = []
        for box, offset in zip(boxes, offsets):
            x, y, right, bottom = box
            ox, oy = offset
            assert all(isinstance(v, int) for v in (*box, *offset)), subject_id
            assert 0 <= x < right <= width and 0 <= y < bottom <= height, subject_id
            assert 0 <= ox and 0 <= oy and ox + right - x <= canvas and oy + bottom - y <= canvas, subject_id
            frames.append(dict(x=x, y=y, width=right-x, height=bottom-y, offsetX=ox, offsetY=oy))
        # Source cells may use irregular transparent cuts; copying those exact
        # boxes prevents a regular-grid assumption from borrowing a neighbour.
        # v5 normalized cells are disjoint. Older approved v4 sheets use
        # independent transparent cut boundaries, which can geometrically
        # overlap. Retain their validated boxes rather than recutting artwork.
        if validation.get("assembly_version", 0) >= 5:
            for index, first in enumerate(boxes):
                for second in boxes[:index]:
                    assert min(first[2], second[2]) <= max(first[0], second[0]) or min(first[3], second[3]) <= max(first[1], second[1]), subject_id
        item = dict(id=subject_id, name=source_item["name"],
                    resource=f"Lumia/UltimateAnimations/{subject_id}",
                    atlasWidth=width, atlasHeight=height,
                    canvasWidth=canvas, canvasHeight=canvas,
                    framesPerSecond=fps, frameCount=len(frames), frames=frames)
        items.append(item)
        audit.append(dict(id=subject_id, name=source_item["name"],
                          source=source_path.relative_to(ROOT).as_posix(),
                          destination=f"Assets/Resources/Lumia/UltimateAnimations/{subject_id}.png",
                          sourceSha256=source_hash, bytes=source_path.stat().st_size,
                          atlasSize=[width, height], canvas=canvas,
                          frameCount=len(frames), framesPerSecond=fps,
                          validation=validation_path.relative_to(ROOT).as_posix()))
    return dict(items=items), audit


def verify_installed(expected: dict, records: list[dict]) -> None:
    assert read_json(DESTINATION / "catalog.json") == expected, "Runtime frame data differs from approved gallery"
    assert {path.stem for path in DESTINATION.glob("*.png")} == {item["id"] for item in expected["items"]}
    assert not list(DESTINATION.glob("*.gif")) and not list(DESTINATION.glob("*.webp"))
    for record in records:
        destination = ROOT / record["destination"]
        assert sha256(destination) == record["sourceSha256"], destination
        assert png_size(destination) == tuple(record["atlasSize"])
        metadata = destination.with_suffix(".png.meta").read_text(encoding="utf-8-sig")
        for setting in ("textureType: 0", "filterMode: 0", "enableMipMap: 0", "isReadable: 0", "nPOTScale: 0", "wrapU: 1", "wrapV: 1", "alphaUsage: 1", "alphaIsTransparency: 1", "maxTextureSize: 4096"):
            assert setting in metadata, (destination, setting)
        assert not re.search(r"textureCompression: [1-9]", metadata), destination
        record["runtimeSha256"] = sha256(destination)
        record["sourceBytesIdentical"] = True


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Verify current runtime files without writing them")
    args = parser.parse_args()
    catalog, records = build_catalog()
    if not args.check:
        DESTINATION.mkdir(parents=True, exist_ok=True)
        DESTINATION.with_suffix(".meta").write_text(folder_meta(DESTINATION), encoding="utf-8")
        for record in records:
            source, destination = ROOT / record["source"], ROOT / record["destination"]
            if not destination.exists() or sha256(destination) != record["sourceSha256"]:
                shutil.copyfile(source, destination)
            destination.with_suffix(".png.meta").write_text(texture_meta(destination), encoding="utf-8")
        catalog_path = DESTINATION / "catalog.json"
        catalog_path.write_text(json.dumps(catalog, ensure_ascii=False, separators=(",", ":")) + "\n", encoding="utf-8")
        catalog_path.with_suffix(".json.meta").write_text(text_meta(catalog_path), encoding="utf-8")
    verify_installed(catalog, records)
    if not args.check:
        AUDIT.write_text(json.dumps(dict(
            passed=True, approvedAnimations=len(records), totalFrames=sum(item["frameCount"] for item in records),
            totalPngBytes=sum(item["bytes"] for item in records), sourceCatalogSha256=sha256(SOURCE / "catalog.json"),
            runtimeCatalogSha256=sha256(DESTINATION / "catalog.json"),
            frameCoordinates="Top-left source pixels; offsets are top-left placement within the recorded square canvas.",
            artChanges="All current approved atlas PNGs copied byte-for-byte without export-time image processing. Nia has an approved iris RGB edit; outside-iris RGBA, alpha, effects and layout are unchanged (ArtSource/AnimationPrototypes/Nia/provenance.json).",
            importing="Default Texture2D, Point, Clamp, no mipmaps, no NPOT scaling, max 4096, uncompressed, input alpha, non-readable.",
            excluded="GIF, WebP, generated raw sheets, thumbnails, official references and prompt files remain source-only.",
            items=records), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(dict(passed=True, animations=len(records), frames=sum(item["frameCount"] for item in records), bytes=sum(item["bytes"] for item in records), checkOnly=args.check)))


if __name__ == "__main__":
    main()
