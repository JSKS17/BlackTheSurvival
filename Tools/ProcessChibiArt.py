"""Mechanically split generated pixel-art atlases; never synthesize or repaint pixels.

Run with the bundled Python. Sources are kept intact. Processing is limited to
grid/explicit-box cropping, alpha-bound trimming, nearest-neighbour resizing and
transparent padding. No background removal, colour quantization or sharpening.
"""
from __future__ import annotations

import argparse
from array import array
import hashlib
import json
import re
import sys
from collections import deque
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
KINDS = {
    "portrait": ("sprites", "Portraits", 128),
    "icon": ("cards", "SkillIcons", 96),
    "passive": ("passives", "PassiveIcons", 96),
}
ID_PATTERN = re.compile(r"^[a-z0-9_-]+$")


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def resolve(root: Path, value: str) -> Path:
    path = Path(value)
    return path.resolve() if path.is_absolute() else (root / path).resolve()


def relative(root: Path, path: Path) -> str:
    try:
        return path.relative_to(root).as_posix()
    except ValueError:
        return str(path)


def file_digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def pixel_digest(image: Image.Image) -> str:
    return hashlib.sha256(f"{image.width}x{image.height}:RGBA:".encode() + image.convert("RGBA").tobytes()).hexdigest()


def alpha_metrics(image: Image.Image) -> dict:
    alpha = image.getchannel("A")
    histogram = alpha.histogram()
    pixel_count = image.width * image.height
    bounds = alpha.getbbox()
    border = []
    if image.width and image.height:
        border.extend(alpha.crop((0, 0, image.width, 1)).tobytes())
        border.extend(alpha.crop((0, image.height - 1, image.width, image.height)).tobytes())
        border.extend(alpha.crop((0, 0, 1, image.height)).tobytes())
        border.extend(alpha.crop((image.width - 1, 0, image.width, image.height)).tobytes())
    return {
        "alphaBounds": list(bounds) if bounds else None,
        "visiblePixels": pixel_count - histogram[0],
        "transparentPixels": histogram[0],
        "opaquePixels": histogram[255],
        "partialAlphaPixels": sum(histogram[1:255]),
        "borderVisiblePixels": sum(1 for value in border if value),
        "borderStrongPixels": sum(1 for value in border if value >= 32),
    }


def large_components(image: Image.Image) -> list[dict]:
    """Report disconnected large alpha islands without dropping or editing them."""
    width, height = image.size
    alpha = image.getchannel("A").tobytes()
    visited = bytearray(width * height)
    results = []
    minimum = max(20, width * height // 250)
    for start, value in enumerate(alpha):
        if value < 32 or visited[start]:
            continue
        visited[start] = 1
        queue = deque([start])
        count = 0
        left = right = start % width
        top = bottom = start // width
        while queue:
            current = queue.popleft()
            x, y = current % width, current // width
            count += 1
            left, right = min(left, x), max(right, x)
            top, bottom = min(top, y), max(bottom, y)
            for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < width and 0 <= ny < height:
                    neighbour = ny * width + nx
                    if not visited[neighbour] and alpha[neighbour] >= 32:
                        visited[neighbour] = 1
                        queue.append(neighbour)
        if count >= minimum:
            results.append({"pixels": count, "bounds": [left, top, right + 1, bottom + 1]})
    return sorted(results, key=lambda item: item["pixels"], reverse=True)


def cells(batch: dict, image: Image.Image):
    columns, rows = int(batch["columns"]), int(batch["rows"])
    if columns < 1 or rows < 1:
        raise ValueError("columns and rows must be positive")
    explicit = batch.get("cells")
    if explicit is None:
        ids = batch.get("entries", batch.get("ids", []))
        if len(ids) > columns * rows:
            raise ValueError("more IDs than grid cells")
        explicit = []
        for index, item in enumerate(ids):
            if not item:
                continue
            cell = dict(item) if isinstance(item, dict) else {"id": item}
            cell.setdefault("column", index % columns)
            cell.setdefault("row", index // columns)
            explicit.append(cell)
    for cell in explicit:
        asset_id = cell["id"]
        if not ID_PATTERN.fullmatch(asset_id):
            raise ValueError(f"unsafe asset id: {asset_id!r}")
        column, row = int(cell["column"]), int(cell["row"])
        if not 0 <= column < columns or not 0 <= row < rows:
            raise ValueError(f"cell outside grid: {asset_id}")
        grid_box = [column * image.width // columns, row * image.height // rows,
                    (column + 1) * image.width // columns, (row + 1) * image.height // rows]
        box = list(map(int, cell.get("box", grid_box)))
        if (len(box) != 4 or box[0] < 0 or box[1] < 0 or box[2] > image.width
                or box[3] > image.height or box[0] >= box[2] or box[1] >= box[3]):
            raise ValueError(f"invalid crop box: {asset_id}: {box}")
        yield asset_id, column, row, box, cell


def normalize_portrait(cell: Image.Image, size: int, padding: int) -> Image.Image:
    bounds = cell.getchannel("A").getbbox()
    if not bounds:
        raise ValueError("sprite cell is entirely transparent")
    sprite = cell.crop(bounds)
    available = size - padding * 2
    if available < 1:
        raise ValueError("padding consumes the output canvas")
    scale = min(available / sprite.width, available / sprite.height)
    width = max(1, min(available, round(sprite.width * scale)))
    height = max(1, min(available, round(sprite.height * scale)))
    sprite = sprite.resize((width, height), Image.Resampling.NEAREST)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    # The lowest visible foot/prop is aligned across all full-body sprites.
    canvas.paste(sprite, ((size - width) // 2, size - padding - height))
    return canvas


def normalize_icon(cell: Image.Image, size: int) -> Image.Image:
    """Keep the complete tile and its aspect ratio; letterbox mismatched grids."""
    scale = min(size / cell.width, size / cell.height)
    width, height = max(1, round(cell.width * scale)), max(1, round(cell.height * scale))
    resized = cell.resize((width, height), Image.Resampling.NEAREST)
    corner = cell.getpixel((0, 0))
    background = corner if corner[3] == 255 else (18, 27, 43, 255)
    canvas = Image.new("RGBA", (size, size), background)
    canvas.paste(resized, ((size - width) // 2, (size - height) // 2))
    return canvas


def portrait_islands(image: Image.Image, batch: dict, threshold: int = 128, fringe: int = 6) -> dict:
    """Unpack existing transparent atlas islands, retaining original RGBA pixels.

    The opaque core identifies ownership; a small surrounding fringe preserves
    original edge alpha. Other characters are omitted from an asset's crop.
    Neither retained pixel colours nor their alpha values are recalculated.
    """
    width, height = image.size
    alpha = image.getchannel("A").tobytes()
    visited = bytearray(width * height)
    components = []
    for start, value in enumerate(alpha):
        if value < threshold or visited[start]:
            continue
        visited[start] = 1
        queue = deque([start])
        positions = array("I")
        left = right = start % width
        top = bottom = start // width
        while queue:
            current = queue.popleft()
            positions.append(current)
            x, y = current % width, current // width
            left, right, top, bottom = min(left, x), max(right, x), min(top, y), max(bottom, y)
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    nx, ny = x + dx, y + dy
                    if (dx or dy) and 0 <= nx < width and 0 <= ny < height:
                        neighbour = ny * width + nx
                        if not visited[neighbour] and alpha[neighbour] >= threshold:
                            visited[neighbour] = 1
                            queue.append(neighbour)
        components.append({"pixels": len(positions), "bounds": [left, top, right + 1, bottom + 1], "positions": positions})
    columns, rows = int(batch["columns"]), int(batch["rows"])
    layout = list(cells(batch, image))
    units = {(column, row): {"id": asset_id, "components": [], "cell": cell_config}
             for asset_id, column, row, _, cell_config in layout}
    minimum = max(32, width * height // (columns * rows * 300))
    substantial = [component for component in components if component["pixels"] >= minimum]
    for component in substantial:
        box = component["bounds"]
        column = min(columns - 1, max(0, int(((box[0] + box[2]) * .5) * columns / width)))
        row = min(rows - 1, max(0, int(((box[1] + box[3]) * .5) * rows / height)))
        if (column, row) not in units:
            raise ValueError(f"unexpected substantial sprite island in an unassigned cell ({column},{row})")
        units[(column, row)]["components"].append(component)
    for unit in units.values():
        if not unit["components"]:
            raise ValueError(f"{unit['id']}: no substantial alpha island near its expected cell")
    # Detached tiny accessories are allocated to the nearest existing body.
    for component in components:
        if component["pixels"] >= minimum or component["pixels"] < 2:
            continue
        box = component["bounds"]
        def distance(item):
            minimum_distance = float("inf")
            for body in item[1]["components"]:
                other = body["bounds"]
                dx = max(0, box[0] - other[2], other[0] - box[2])
                dy = max(0, box[1] - other[3], other[1] - box[3])
                minimum_distance = min(minimum_distance, dx * dx + dy * dy)
            return minimum_distance
        key, unit = min(units.items(), key=distance)
        if distance((key, unit)) <= (max(width / columns, height / rows) * .2) ** 2:
            unit["components"].append(component)
    # A fringe may reach a neighbouring core in a crowded source sheet. Keep
    # every core pixel allocated to exactly its original character.
    owners = array("H", [0]) * (width * height)
    for number, unit in enumerate(units.values(), 1):
        unit["number"] = number
        for component in unit["components"]:
            for position in component["positions"]:
                owners[position] = number
    extracted = {}
    for key, unit in units.items():
        assigned = unit["components"]
        box = [max(0, min(item["bounds"][0] for item in assigned) - fringe),
               max(0, min(item["bounds"][1] for item in assigned) - fringe),
               min(width, max(item["bounds"][2] for item in assigned) + fringe),
               min(height, max(item["bounds"][3] for item in assigned) + fringe)]
        crop = image.crop(tuple(box))
        mask_bytes = bytearray(crop.width * crop.height)
        for component in assigned:
            for position in component["positions"]:
                x, y = position % width - box[0], position // width - box[1]
                mask_bytes[y * crop.width + x] = 255
        mask = Image.frombytes("L", crop.size, bytes(mask_bytes))
        if fringe:
            mask = mask.filter(ImageFilter.MaxFilter(fringe * 2 + 1))
        separated = bytearray(mask.tobytes())
        for y in range(crop.height):
            source_offset = (y + box[1]) * width + box[0]
            crop_offset = y * crop.width
            for x in range(crop.width):
                owner = owners[source_offset + x]
                if owner and owner != unit["number"]:
                    separated[crop_offset + x] = 0
        mask = Image.frombytes("L", crop.size, bytes(separated))
        unpacked = Image.new("RGBA", crop.size, (0, 0, 0, 0))
        unpacked.paste(crop, (0, 0), mask)
        extracted[unit["id"]] = {
            "box": box, "image": unpacked,
            "components": [{"pixels": item["pixels"], "bounds": item["bounds"]} for item in assigned],
        }
    return extracted


def process_batch(root: Path, batch: dict, expected: dict, args) -> tuple[list[dict], list[str]]:
    batch_kind = batch["kind"]
    if batch_kind not in KINDS and batch_kind != "skill":
        raise ValueError(f"unknown batch kind: {batch_kind}")
    source = resolve(root, batch["source"])
    if not source.is_file():
        raise ValueError(f"missing generated atlas: {source}")
    source_hash = file_digest(source)
    with Image.open(source) as opened:
        if opened.format != "PNG":
            raise ValueError(f"source must be PNG: {source}")
        image = opened.convert("RGBA")
    size = int(batch.get("size", args.portrait_size if batch_kind == "portrait" else args.icon_size))
    padding = int(batch.get("padding", args.padding))
    if size < 16 or size > 1024:
        raise ValueError("export size must be between 16 and 1024")
    entries, warnings = [], []
    portrait_mode = batch.get("portrait_mode", getattr(args, "portrait_mode", "islands"))
    core_alpha = int(batch.get("island_alpha", 128))
    fringe = int(batch.get("island_fringe", 6))
    unpacked = portrait_islands(image, batch, core_alpha, fringe) if batch_kind == "portrait" and portrait_mode == "islands" else None
    for asset_id, column, row, box, cell_config in cells(batch, image):
        kind = cell_config.get("kind", batch_kind)
        if kind not in KINDS:
            raise ValueError(f"{asset_id}: mixed skill sheets must specify kind icon or passive per entry")
        group, directory, _ = KINDS[kind]
        if asset_id not in expected[group]:
            raise ValueError(f"{asset_id} is not a {group} ID in the game manifest")
        if unpacked is not None:
            box = unpacked[asset_id]["box"]
            crop = unpacked[asset_id]["image"]
        else:
            crop = image.crop(tuple(box))
        source_metrics = alpha_metrics(crop)
        if not source_metrics["visiblePixels"]:
            raise ValueError(f"{asset_id}: source cell is empty")
        islands = []
        if kind == "portrait":
            if not source_metrics["transparentPixels"]:
                raise ValueError(f"{asset_id}: sprite source lacks a transparent background; regenerate it")
            if source_metrics["borderStrongPixels"]:
                message = (f"{asset_id}: {source_metrics['borderStrongPixels']} nontransparent border pixels; "
                           "check for a clipped sprite or neighbouring-cell contamination")
                if not (cell_config.get("allow_border_contact", batch.get("allow_border_contact", False))
                        or args.allow_border_contact):
                    raise ValueError(message)
                warnings.append(message)
            islands = unpacked[asset_id]["components"] if unpacked is not None else large_components(crop)
            if len(islands) > 1:
                primary, second = islands[:2]
                p, s = primary["bounds"], second["bounds"]
                primary_height, second_height = p[3] - p[1], s[3] - s[1]
                separation = max(0, max(p[0], s[0]) - min(p[2], s[2]))
                if (second["pixels"] >= primary["pixels"] * 0.35
                        and second_height >= primary_height * 0.6
                        and separation >= crop.width * 0.04):
                    warnings.append(f"{asset_id}: two large full-height alpha islands; visually check the crop")
            output = normalize_portrait(crop, size, padding)
        else:
            # Icons retain their generated tile background and all source pixels.
            output = normalize_icon(crop, size)
        destination = root / "Assets/Resources/Lumia" / directory / f"{asset_id}.png"
        destination.parent.mkdir(parents=True, exist_ok=True)
        output.save(destination, format="PNG", optimize=False)
        metrics = alpha_metrics(output)
        entries.append({
            "id": asset_id, "kind": kind, "name": expected[group][asset_id].get("name", asset_id),
            "path": relative(root, destination), "width": size, "height": size,
            "fileSha256": file_digest(destination), "pixelSha256": pixel_digest(output),
            "batch": batch["id"], "source": relative(root, source), "sourceSha256": source_hash,
            "sourceSize": list(image.size), "columns": int(batch["columns"]), "rows": int(batch["rows"]),
            "column": column, "row": row, "cropBox": box,
            "sourceAlpha": source_metrics, "sourceComponents": islands, "outputAlpha": metrics,
            "extraction": "global-alpha-islands" if unpacked is not None else "grid-crop",
            "sourceComponentCoordinates": "atlas" if unpacked is not None else "cell",
            "islandCoreAlpha": core_alpha if unpacked is not None else None,
            "islandFringe": fringe if unpacked is not None else None,
            "warnings": [warning for warning in warnings if warning.startswith(asset_id + ":")],
            "processing": "island-unpack/crop/alpha-trim/nearest-resize/transparent-padding" if unpacked is not None else ("crop/alpha-trim/nearest-resize/transparent-padding" if kind == "portrait" else "crop/nearest-resize/aspect-preserving-padding"),
        })
    return entries, warnings


def contact_sheets(root: Path, entries: list[dict]):
    font = ImageFont.load_default(size=11)
    for kind, (_, _, size) in KINDS.items():
        group = sorted((entry for entry in entries if entry["kind"] == kind), key=lambda item: item["id"])
        if not group:
            continue
        columns = 8 if kind == "portrait" else 16
        tile_width, tile_height = size + 12, size + 30
        rows = (len(group) + columns - 1) // columns
        sheet = Image.new("RGB", (columns * tile_width, rows * tile_height), "#121b2b")
        draw = ImageDraw.Draw(sheet)
        for index, entry in enumerate(group):
            x, y = index % columns * tile_width, index // columns * tile_height
            draw.rectangle((x + 4, y + 4, x + size + 7, y + size + 7), fill="#283548")
            with Image.open(resolve(root, entry["path"])) as image:
                image = image.convert("RGBA")
                if image.size != (size, size):
                    image = image.resize((size, size), Image.Resampling.NEAREST)
                sheet.paste(image, (x + 6, y + 6), image)
            label = entry["id"]
            if len(label) > 18:
                label = label[:17] + "~"
            draw.text((x + 6, y + size + 11), label, font=font, fill="#dce8e1")
        destination = root / "docs/art-chibi" / f"{kind}-contact-sheet.png"
        destination.parent.mkdir(parents=True, exist_ok=True)
        sheet.save(destination)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--manifest", default="docs/art-chibi/game-art-manifest.json")
    parser.add_argument("--batches", default="docs/art-chibi/batches.json")
    parser.add_argument("--index", default="docs/art-chibi/processed-art-index.json")
    parser.add_argument("--batch", action="append", help="Process only the named batch; may be repeated")
    parser.add_argument("--portrait-size", type=int, default=128)
    parser.add_argument("--icon-size", type=int, default=96)
    parser.add_argument("--padding", type=int, default=8)
    parser.add_argument("--portrait-mode", choices=("islands", "grid"), default="islands")
    parser.add_argument("--allow-border-contact", action="store_true", help="Record border warnings instead of stopping; inspect every affected crop")
    parser.add_argument("--no-contact-sheets", action="store_true")
    args = parser.parse_args()
    root = args.root.resolve()
    try:
        manifest = read_json(resolve(root, args.manifest))
        expected = {group: {item["id"]: item for item in manifest.get(group, [])} for group, _, _ in KINDS.values()}
        batches_file = read_json(resolve(root, args.batches))
        batches = batches_file["batches"] if isinstance(batches_file, dict) else batches_file
        selected = [batch for batch in batches if not args.batch or batch["id"] in args.batch]
        if args.batch and set(args.batch) - {batch["id"] for batch in selected}:
            raise ValueError("one or more requested batch IDs were not found")
        if not selected:
            raise ValueError("no batches selected")
        index_path = resolve(root, args.index)
        existing = read_json(index_path) if index_path.exists() else {"schema": 1, "assets": [], "warnings": []}
        selected_batch_ids = {batch["id"] for batch in selected}
        entries = [entry for entry in existing.get("assets", []) if entry.get("batch") not in selected_batch_ids]
        warnings = []
        seen = {(entry["kind"], entry["id"]) for entry in entries}
        for batch in selected:
            processed, batch_warnings = process_batch(root, batch, expected, args)
            for entry in processed:
                key = (entry["kind"], entry["id"])
                if key in seen:
                    raise ValueError(f"duplicate asset assignment: {key}")
                seen.add(key)
            entries.extend(processed)
            warnings.extend(batch_warnings)
            print(f"{batch['id']}: exported {len(processed)} {batch['kind']} assets")
        warnings = list(dict.fromkeys(warning for entry in entries for warning in entry.get("warnings", [])))
        result = {
            "schema": 1, "manifest": relative(root, resolve(root, args.manifest)),
            "manifestSha256": file_digest(resolve(root, args.manifest)),
            "processingPolicy": "Only generated atlases, mechanical cropping, nearest-neighbour scaling and transparent padding. Source files are preserved.",
            "assets": sorted(entries, key=lambda item: (item["kind"], item["id"])), "warnings": warnings,
        }
        write_json(index_path, result)
        if not args.no_contact_sheets:
            contact_sheets(root, entries)
        for warning in warnings:
            print(f"WARNING: {warning}", file=sys.stderr)
        print(f"Index: {relative(root, index_path)}; {len(entries)} total assets")
        return 0
    except (OSError, ValueError, KeyError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
