"""Assemble generated cells into animations without painting or resampling them."""
import argparse
import concurrent.futures
import hashlib
import json
import statistics
import subprocess
import struct
import traceback
from pathlib import Path
from PIL import Image
from MediaRuntime import ffmpeg_path

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / "ArtSource/AnimationPrototypes"
FFMPEG = ffmpeg_path()
ASSEMBLY_VERSION = 4
FIXED_GRID_ASSEMBLY_VERSION = 5

def desired_assembly_version(manifest):
    """Keep the approved legacy previews cached while opting new sheets into v5."""
    layout = manifest.get("frame_layout")
    if layout is None:
        return ASSEMBLY_VERSION
    if layout == "fixed-grid":
        return FIXED_GRID_ASSEMBLY_VERSION
    raise ValueError(f"Unsupported frame_layout: {layout!r}")

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def run(*args):
    subprocess.run([str(FFMPEG), "-hide_banner", "-loglevel", "error", "-y", *map(str, args)], check=True, capture_output=True)

def foot_y(frame):
    """Read the dark silhouette near the ground, ignoring bright skill particles."""
    w, h = frame.size
    pixels = frame.load()
    for y in range(h - 1, h // 2, -1):
        count = sum(1 for x in range(w // 12, w * 3 // 4)
                    if pixels[x, y][3] >= 180 and max(pixels[x, y][:3]) < 165)
        if count >= max(3, w // 90):
            return y
    return None

def transparent_cuts(image, axis, sections):
    """Locate real gutters instead of cutting through a generated row's feet."""
    alpha = image.getchannel("A")
    width, height = image.size
    length = height if axis == "y" else width
    projection = []
    for position in range(length):
        line = alpha.crop((0, position, width, position+1) if axis == "y" else (position, 0, position+1, height))
        histogram = line.histogram()
        projection.append(sum(histogram[160:]) * 3 + sum(histogram[64:160]))
    cuts = [0]
    for index in range(1, sections):
        expected = round(length * index / sections)
        radius = round(length / sections * .22)
        low, high = expected-radius, expected+radius
        scores = {position: sum(projection[max(0,position-2):min(length,position+3)]) for position in range(low, high+1)}
        best = min(scores.values())
        eligible = [position for position in range(low,high+1) if scores[position] <= best + max(1, best*.03)]
        runs = []
        for position in eligible:
            if not runs or position > runs[-1][-1]+1: runs.append([position])
            else: runs[-1].append(position)
        centers = [(run[0]+run[-1])//2 for run in runs]
        cut = min(centers, key=lambda position:(abs(position-expected),scores[position]))
        cuts.append(cut)
    return cuts + [length]

def frame_boxes(atlas):
    """Refine each cell's gutters; adjacent poses need not share a row baseline.

    An effect may extend below its neighbours while the next row's first pose
    starts higher. A single sheet-wide horizontal cut then clips one pose and
    puts its fragments into another. Alternating the two projections confines
    each search to the actual cell's orthogonal span, without masking pixels.
    """
    width, height = atlas.size
    coarse_rows = transparent_cuts(atlas, "y", 3)
    boxes = []
    for row in range(3):
        top, bottom = coarse_rows[row:row+2]
        columns = transparent_cuts(atlas.crop((0, top, width, bottom)), "x", 4)
        boxes.extend([[columns[col], top, columns[col+1], bottom] for col in range(4)])
    for _ in range(3):
        refined = []
        for index, (left, top, right, bottom) in enumerate(boxes):
            row, col = divmod(index, 4)
            rows = transparent_cuts(atlas.crop((left, 0, right, height)), "y", 3)
            top, bottom = rows[row:row+2]
            columns = transparent_cuts(atlas.crop((0, top, width, bottom)), "x", 4)
            left, right = columns[col:col+2]
            rows = transparent_cuts(atlas.crop((left, 0, right, height)), "y", 3)
            top, bottom = rows[row:row+2]
            assert 0 <= left < right <= width and 0 <= top < bottom <= height
            refined.append([left, top, right, bottom])
        if refined == boxes:
            break
        boxes = refined
    return boxes, coarse_rows

def assert_disjoint_boxes(boxes, size):
    """A source pixel must never belong to more than one animation frame."""
    width, height = size
    for index, (left, top, right, bottom) in enumerate(boxes):
        assert 0 <= left < right <= width and 0 <= top < bottom <= height, (index, boxes[index], size)
        for other_index, (other_left, other_top, other_right, other_bottom) in enumerate(boxes[:index]):
            overlap_width = min(right, other_right) - max(left, other_left)
            overlap_height = min(bottom, other_bottom) - max(top, other_top)
            assert overlap_width <= 0 or overlap_height <= 0, ("Overlapping frame cells", other_index, index, boxes[other_index], boxes[index])

def fixed_grid_boxes(atlas, manifest):
    """Read exact 4 x 3 cells; fail rather than erase artwork crossing a guard."""
    assert manifest.get("columns", 4) == 4 and manifest.get("rows", 3) == 3, "Fixed-grid sheets require 4 columns and 3 rows"
    gutter = manifest.get("frame_gutter")
    assert type(gutter) is int and gutter >= 20, "Fixed-grid frame_gutter must be an integer of at least 20 source pixels"
    width, height = atlas.size
    columns = [round(width * index / 4) for index in range(5)]
    rows = [round(height * index / 3) for index in range(4)]
    boxes = [[columns[col], rows[row], columns[col+1], rows[row+1]] for row in range(3) for col in range(4)]
    assert_disjoint_boxes(boxes, atlas.size)
    assert sum((right-left)*(bottom-top) for left, top, right, bottom in boxes) == width*height
    records, violations = [], []
    for index, box in enumerate(boxes):
        alpha = atlas.getchannel("A").crop(box)
        cell_width, cell_height = alpha.size
        assert gutter * 2 < min(alpha.size), ("Gutter leaves no cell interior", index, gutter, alpha.size)
        bands = {
            "left": (0, 0, gutter, cell_height),
            "right": (cell_width-gutter, 0, cell_width, cell_height),
            "top": (0, 0, cell_width, gutter),
            "bottom": (0, cell_height-gutter, cell_width, cell_height),
        }
        sides = {}
        for side, band in bands.items():
            histogram = alpha.crop(band).histogram()
            sides[side] = dict(alpha_nonzero=sum(histogram[1:]), alpha_ge_64=sum(histogram[64:]), alpha_ge_160=sum(histogram[160:]))
            if sides[side]["alpha_ge_64"]:
                violations.append(dict(frame=index+1, side=side, **sides[side]))
        records.append(dict(index=index, box=box, gutter=gutter, sides=sides))
    assert not violations, "Fixed-grid visible artwork enters the transparent guard; regenerate this sheet without masking pixels: " + json.dumps(violations, ensure_ascii=False)
    return boxes, rows, dict(layout="fixed-grid", gutter=gutter, alpha_failure_threshold=64,
                             alpha_solid_threshold=160, boxes_disjoint=True, cells_cover_atlas=True,
                             guard_passed=True, cells=records)

def assemble(folder):
    manifest = json.loads((folder / "manifest.json").read_text(encoding="utf-8-sig"))
    assembly_version = desired_assembly_version(manifest)
    source = folder / "atlas.png"
    original_hash = digest(source)
    assert original_hash == manifest["atlas_sha256"], source
    normalization_sha256 = None
    normalization = None
    if manifest.get("normalization"):
        assert assembly_version == FIXED_GRID_ASSEMBLY_VERSION, "Normalized sheets require fixed-grid assembly"
        normalization_path = folder / manifest["normalization"]
        generated_path = folder / manifest["generated_source_file"]
        assert normalization_path.resolve().is_relative_to(folder.resolve()) and generated_path.resolve().is_relative_to(folder.resolve()), "Normalized source references must stay in their subject folder"
        normalization = json.loads(normalization_path.read_text(encoding="utf-8-sig"))
        assert normalization.get("passed") and normalization.get("source_rgba_preserved") and normalization.get("source_pixels_partitioned_exactly_once"), normalization_path
        assert normalization.get("atlas_sha256") == original_hash, normalization_path
        assert normalization.get("generated_source_sha256") == manifest["generated_source_sha256"] == digest(generated_path), generated_path
        assert len(normalization.get("frame_records", [])) == 12 and all(record["source_rgba_sha256"] == record["normalized_rgba_sha256"] for record in normalization["frame_records"]), normalization_path
        normalization_sha256 = digest(normalization_path)
    preserve_normalized_ground = manifest.get("ground_alignment") == "per-frame"
    requested_airborne_lifts = manifest.get("airborne_lift", {})
    assert isinstance(requested_airborne_lifts, dict), "airborne_lift must be a frame-to-height object"
    requested_airborne_lifts = {str(int(index)): lift for index, lift in requested_airborne_lifts.items()}
    assert not requested_airborne_lifts or preserve_normalized_ground, "airborne_lift requires verified per-frame ground alignment"
    if preserve_normalized_ground:
        assert normalization is not None and normalization.get("ground_alignment") == "per-frame" and normalization.get("grounded_feet_aligned"), "Per-frame ground alignment requires verified normalization evidence"
        ground_values = normalization.get("grounded_output_foot_y", {})
        assert set(ground_values) == {str(index) for index in normalization["grounded_frames"]} and len(set(ground_values.values())) <= 1, "Normalized grounded-foot evidence is incomplete or inconsistent"
        assert normalization.get("airborne_lift", {}) == requested_airborne_lifts, "Normalized airborne lifts differ from the current manifest; normalize the original source again"
        if requested_airborne_lifts:
            proof = normalization.get("airborne_lift_proof", {})
            assert normalization.get("airborne_lift_verified") and ground_values and set(proof) == set(requested_airborne_lifts), "Normalized airborne-lift proof is missing or incomplete"
            for index, lift in requested_airborne_lifts.items():
                record = proof[index]
                assert type(lift) is int and lift >= 0 and int(index) in manifest.get("intended_airborne_frames", []), ("Invalid airborne-lift declaration", index, lift)
                assert record["requested_lift"] == lift and record["source_nominal_foot_y"]+record["translation_y"] == record["applied_nominal_foot_y"] == record["target_nominal_foot_y"] == normalization["ground_reference_y"]-lift, ("Airborne nominal foot height does not match its proof", index)
                assert record["normalized_ground_reference_y"] == next(iter(ground_values.values())) and record["normalized_cell_foot_y"] == record["normalized_ground_reference_y"]-lift, ("Airborne output foot height does not match its proof", index)
    report_path = folder / "validation.json"
    if report_path.exists():
        previous = json.loads(report_path.read_text(encoding="utf-8-sig"))
        if previous.get("passed") and previous.get("assembly_version") == assembly_version and previous.get("atlas_sha256") == original_hash and previous.get("manifest_sha256") == digest(folder / "manifest.json") and previous.get("normalization_sha256") == normalization_sha256 and (assembly_version != FIXED_GRID_ASSEMBLY_VERSION or previous.get("outer_padding") == 32) and (not preserve_normalized_ground or previous.get("normalization_ground_alignment_preserved")) and (not requested_airborne_lifts or previous.get("normalization_airborne_lift_preserved")) and all((folder / x).is_file() for x in ["preview.gif", "preview.webp", "thumbnail.png"]):
            return manifest["id"], "cached"
    with Image.open(source) as original:
        atlas = original.convert("RGBA")
    w, h = atlas.size
    # Built-in edits can differ by one pixel at the outer edge. Keep their raw
    # bytes and find real cell gutters instead of resampling to a divisible size.
    assert w >= 4 and h >= 3 and abs(w * 3 - h * 4) <= 12, source
    grid_report = None
    if assembly_version == FIXED_GRID_ASSEMBLY_VERSION:
        boxes, row_cuts, grid_report = fixed_grid_boxes(atlas, manifest)
    else:
        boxes, row_cuts = frame_boxes(atlas)
    frames = [atlas.crop(box) for box in boxes]
    cw, ch = max(frame.width for frame in frames), max(frame.height for frame in frames)
    assert all(frame.getchannel("A").histogram()[0] > 0 for frame in frames), source
    assert len({hashlib.sha256(frame.tobytes()).hexdigest() for frame in frames}) >= 10, source
    airborne = set(manifest.get("intended_airborne_frames", []))
    ignored = airborne | set(manifest.get("alignment_ignore_frames", [])) | set(manifest.get("actor_absent_frames", []))
    feet = [foot_y(frame) for frame in frames]
    # Measure relative to the nominal row origin, not each independently cut
    # rectangle. Otherwise gutter differences would change the animation pose.
    normalized_feet = [None if foot is None else foot + boxes[i][1] - round(i//4*h/3)
                       for i, foot in enumerate(feet)]
    # Correct whole-row layout drift, while preserving jumps, dashes and intra-row motion.
    medians = []
    for row in range(3):
        candidates = [normalized_feet[i] for i in range(row * 4, row * 4 + 4)
                      if i not in ignored and normalized_feet[i] is not None]
        medians.append(int(statistics.median(candidates)) if candidates else None)
    ground = medians[0] or next((value for value in medians if value is not None), ch - 20)
    medians = [value if value is not None else ground for value in medians]
    row_corrections = [0, 0, 0] if preserve_normalized_ground else [ground-value for value in medians]
    # v5 already has at least 20px of empty guard inside every normalized cell.
    # A second large legacy margin would needlessly shrink the art in the fixed
    # preview viewport. Keep a modest outer margin without resampling any pixels.
    pad = 32 if assembly_version == FIXED_GRID_ASSEMBLY_VERSION else max(96, max(cw,ch) // 4)
    relative = [[boxes[i][0] - round(i%4*w/4),
                 boxes[i][1] - round(i//4*h/3) + row_corrections[i // 4]] for i in range(12)]
    origin_x = max(pad, -min(offset[0] for offset in relative))
    origin_y = max(pad, -min(offset[1] for offset in relative))
    offsets = [[origin_x+x, origin_y+y] for x,y in relative]
    # Allow the full row correction. Clamping it leaves a visible jump at row transitions.
    canvas = max(max(cw,ch) + pad*2, max(x+frame.width for frame,(x,y) in zip(frames,offsets))+pad,
                 max(y+frame.height for frame,(x,y) in zip(frames,offsets))+pad)
    work = ROOT / "Temp/UltimateAssembly" / manifest["id"]
    work.mkdir(parents=True, exist_ok=True)
    records = []
    for i, (frame, (dx, dy)) in enumerate(zip(frames, offsets)):
        output = work / f"frame-{i:02d}.png"
        left, top, right, bottom = boxes[i]
        run("-i", source, "-vf", f"crop={right-left}:{bottom-top}:{left}:{top},format=rgba,pad={canvas}:{canvas}:{dx}:{dy}:color=black@0", "-frames:v", 1, "-threads", 1, output)
        with Image.open(output) as decoded:
            aligned = decoded.convert("RGBA")
            assert aligned.crop((dx, dy, dx + frame.width, dy + frame.height)).tobytes() == frame.tobytes(), (source, i)
        records.append(dict(index=i, box=boxes[i], offset=[dx, dy], cell_rgba_sha256=hashlib.sha256(frame.tobytes()).hexdigest()))
    pattern = work / "frame-%02d.png"
    run("-framerate", 8, "-i", pattern, "-c:v", "libwebp_anim", "-lossless", 1, "-compression_level", 4, "-loop", 0, "-threads", 1, folder / "preview.webp")
    # v5 writes the entire transparent canvas on every GIF frame. Combined with
    # disposal=2 this prevents a previous frame's effect from surviving a redraw.
    # Keep v4's encoding unchanged for the approved, already assembled sheets.
    gif_options = ["-gifflags", 0] if assembly_version == FIXED_GRID_ASSEMBLY_VERSION else []
    run("-framerate", 8, "-i", pattern, "-filter_complex", "[0:v]split[a][b];[a]palettegen=reserve_transparent=1[p];[b][p]paletteuse=dither=bayer:alpha_threshold=64", *gif_options, "-loop", 0, "-final_delay", 12, "-threads", 1, folder / "preview.gif")
    (folder / "thumbnail.png").write_bytes((work / "frame-00.png").read_bytes())
    durations, gif_disposal, gif_rectangles = [], [], []
    with Image.open(folder / "preview.gif") as gif, Image.open(folder / "preview.webp") as webp:
        assert gif.n_frames == webp.n_frames == 12, source
        for i in range(12):
            gif.seek(i); durations.append(gif.info["duration"])
            gif_disposal.append(gif.disposal_method)
            gif_rectangles.append(list(gif.tile[0][1]))
            webp.seek(i)
            with Image.open(work / f"frame-{i:02d}.png") as image:
                reference = image.convert("RGBA")
            decoded = webp.convert("RGBA")
            assert decoded.getchannel("A").tobytes() == reference.getchannel("A").tobytes(), (source, i)
            assert all(a == b for a, b in zip(reference.get_flattened_data(), decoded.get_flattened_data()) if a[3]), (source, i)
            if assembly_version == FIXED_GRID_ASSEMBLY_VERSION:
                assert gif.disposal_method == 2, ("GIF must clear its preceding frame", source, i, gif.disposal_method)
                assert gif_rectangles[-1] == [0, 0, canvas, canvas], ("GIF must encode a full frame", source, i, gif_rectangles[-1])
                # GIF has one-bit transparency. Check its decoded mask against
                # the actual palette threshold rather than assuming disposal works.
                expected_alpha = reference.getchannel("A").point(lambda value: 255 if value >= 64 else 0)
                assert gif.convert("RGBA").getchannel("A").tobytes() == expected_alpha.tobytes(), ("GIF alpha contains a previous-frame fragment", source, i)
    assert sum(durations) == 1500, source
    encoded = (folder / "preview.webp").read_bytes()
    position = 12; webp_durations = []
    while position + 8 <= len(encoded):
        tag = encoded[position:position+4]; length = struct.unpack_from("<I", encoded, position+4)[0]
        if tag == b"ANMF": webp_durations.append(int.from_bytes(encoded[position+20:position+23], "little"))
        position += 8 + length + length % 2
    assert len(webp_durations) == 12 and sum(webp_durations) == 1500, (source, webp_durations)
    report = dict(passed=True, assembly_version=assembly_version, id=manifest["id"], atlas_sha256=original_hash, manifest_sha256=digest(folder / "manifest.json"), original_bytes_preserved=digest(source) == original_hash,
                  frames=12, fps=8, duration_ms=sum(durations), canvas=canvas, outer_padding=pad, cell=[cw, ch], atlas_size=[w,h], boxes=boxes, row_cuts=row_cuts,
                  row_ground=medians, normalized_feet=normalized_feet, independent_cell_gutters=grid_report is None,
                  intended_airborne_frames=sorted(airborne), offsets=offsets,
                  frame_assembly_exact_rgba=True, webp_visible_rgba_lossless=True, frames_distinct=True,
                  game_integration=False, webp_frame_ms=webp_durations, animation_sha256={name: digest(folder / name) for name in ["preview.gif", "preview.webp"]}, frame_records=records)
    if grid_report is not None:
        report.update(fixed_grid=grid_report, gif_full_frames=True, gif_disposal=gif_disposal,
                      gif_frame_rectangles=gif_rectangles, gif_decoded_alpha_matches=True)
    if normalization_sha256 is not None:
        report.update(normalization_sha256=normalization_sha256, generated_source_sha256=manifest["generated_source_sha256"],
                      generated_source_rgba_preserved=True)
    if preserve_normalized_ground:
        report.update(normalization_ground_alignment_preserved=True, row_ground_corrections=row_corrections,
                      normalization_grounded_output_foot_y=normalization["grounded_output_foot_y"])
    if requested_airborne_lifts:
        report.update(normalization_airborne_lift_preserved=True,
                      normalization_airborne_lift_proof=normalization["airborne_lift_proof"])
    report_path.write_bytes((json.dumps(report, ensure_ascii=False, indent=2) + "\n").encode("utf-8"))
    return manifest["id"], "assembled"

def catalog():
    roster = json.loads((BASE / "roster.json").read_text(encoding="utf-8-sig"))
    output = []
    for character in roster:
        item = {key: character[key] for key in ["id", "name", "ultimate"]}
        if character["id"] == "nia":
            item.update(ready=True, gif="Nia/nia-ultimate-preview.gif", webp="Nia/nia-ultimate-preview.webp", atlas="Nia/nia-ultimate-atlas.png", detail="Nia/preview.html", labels=["대기", "입력 준비", "첫 입력", "게임월드 전개", "보호막", "블록 낙하", "연속 입력", "블록 축적", "최종 타격", "효과 분산", "입력 회수", "대기 복귀"], cols=4, rows=3, cell=362, canvas=480, atlas_size=[1448,1086], boxes=[[i%4*362,i//4*362,(i%4+1)*362,(i//4+1)*362] for i in range(12)], offsets=[[170-a,400-b] for a,b in [[167,354],[163,354],[158,354],[103,353],[125,327],[123,327],[126,327],[124,328],[126,294],[125,294],[135,294],[161,294]]])
        else:
            folder = BASE / "Subjects" / character["id"]
            meta = json.loads((folder / "manifest.json").read_text(encoding="utf-8-sig")) if (folder / "manifest.json").is_file() else {}
            result = json.loads((folder / "validation.json").read_text(encoding="utf-8-sig")) if (folder / "validation.json").is_file() else {}
            item["ready"] = bool(result.get("passed")) and result.get("assembly_version") == desired_assembly_version(meta)
            if desired_assembly_version(meta) == FIXED_GRID_ASSEMBLY_VERSION:
                item["ready"] = item["ready"] and (folder / "atlas.png").is_file() and result.get("atlas_sha256") == digest(folder / "atlas.png") and result.get("manifest_sha256") == digest(folder / "manifest.json")
            if item["ready"]:
                prefix = "Subjects/" + character["id"] + "/"
                item.update(gif=prefix+"preview.gif", webp=prefix+"preview.webp", atlas=prefix+"atlas.png", thumb=prefix+"thumbnail.png", labels=meta["frame_labels"], motion=" → ".join(meta["frame_labels"][i] for i in [1,4,8,10]), sources=meta["sources"], cols=4, rows=3, cell=result["cell"][0], canvas=result["canvas"], offsets=result["offsets"], atlas_size=result["atlas_size"], boxes=result["boxes"])
                if result.get("fixed_grid"):
                    item.update(frame_layout="fixed-grid", frame_gutter=result["fixed_grid"]["gutter"])
                if (folder / "original-reference.png").is_file():
                    item["original"] = prefix + "original-reference.png"
                if (folder / "official-reference.png").is_file():
                    item["official"] = prefix + "official-reference.png"
                if meta.get("preview_revision", "").startswith(("nia-style-", "chibi2-")):
                    item["style_ref"] = "Nia/frames/frame-00.png"
                if meta.get("generated_source_file"):
                    item["raw"] = prefix + meta["generated_source_file"]
                if meta.get("preview_revision"):
                    item["revision"] = meta["preview_revision"] + "-" + result["atlas_sha256"][:12]
        output.append(item)
    template = (ROOT / "Tools/UltimateGallery.template.html").read_text(encoding="utf-8")
    data = json.dumps(output, ensure_ascii=False).replace("</", "<\\/")
    (BASE / "index.html").write_bytes(template.replace("__CATALOG__", data).encode("utf-8"))
    (BASE / "catalog.json").write_bytes((json.dumps(output, ensure_ascii=False, indent=2)+"\n").encode("utf-8"))
    print(f"Gallery: {sum(item['ready'] for item in output)} / {len(output)} ready", flush=True)

if __name__ == "__main__":
    parser = argparse.ArgumentParser(); parser.add_argument("--id"); parser.add_argument("--catalog-only", action="store_true"); args = parser.parse_args()
    folders = [folder for folder in (BASE / "Subjects").iterdir() if folder.is_dir() and (folder / "manifest.json").is_file() and (not args.id or folder.name == args.id)]
    failures = []
    if not args.catalog_only:
        with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
            futures = {pool.submit(assemble, folder): folder.name for folder in folders}
            for future in concurrent.futures.as_completed(futures):
                try: print(*future.result(), flush=True)
                except Exception as exception: failures.append(dict(id=futures[future], error=traceback.format_exc(limit=3))); print("FAIL", failures[-1], flush=True)
    catalog()
    (BASE / "assembly-status.json").write_bytes((json.dumps(dict(failures=failures), indent=2)+"\n").encode())
    if failures: raise SystemExit(1)
