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

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / "ArtSource/AnimationPrototypes"
FFMPEG = ROOT / "Temp/AudioTools/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe"
ASSEMBLY_VERSION = 4

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

def assemble(folder):
    manifest = json.loads((folder / "manifest.json").read_text(encoding="utf-8-sig"))
    source = folder / "atlas.png"
    original_hash = digest(source)
    assert original_hash == manifest["atlas_sha256"], source
    report_path = folder / "validation.json"
    if report_path.exists():
        previous = json.loads(report_path.read_text(encoding="utf-8-sig"))
        if previous.get("assembly_version") == ASSEMBLY_VERSION and previous.get("atlas_sha256") == original_hash and previous.get("manifest_sha256") == digest(folder / "manifest.json") and all((folder / x).is_file() for x in ["preview.gif", "preview.webp", "thumbnail.png"]):
            return manifest["id"], "cached"
    with Image.open(source) as original:
        atlas = original.convert("RGBA")
    w, h = atlas.size
    assert w % 4 == 0 and h % 3 == 0 and w // 4 == h // 3, source
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
    pad = max(96, max(cw,ch) // 4)
    relative = [[boxes[i][0] - round(i%4*w/4),
                 boxes[i][1] - round(i//4*h/3) + ground - medians[i // 4]] for i in range(12)]
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
    run("-framerate", 8, "-i", pattern, "-filter_complex", "[0:v]split[a][b];[a]palettegen=reserve_transparent=1[p];[b][p]paletteuse=dither=bayer:alpha_threshold=64", "-loop", 0, "-final_delay", 12, "-threads", 1, folder / "preview.gif")
    (folder / "thumbnail.png").write_bytes((work / "frame-00.png").read_bytes())
    durations = []
    with Image.open(folder / "preview.gif") as gif, Image.open(folder / "preview.webp") as webp:
        assert gif.n_frames == webp.n_frames == 12, source
        for i in range(12):
            gif.seek(i); durations.append(gif.info["duration"])
            webp.seek(i)
            with Image.open(work / f"frame-{i:02d}.png") as image:
                reference = image.convert("RGBA")
            decoded = webp.convert("RGBA")
            assert decoded.getchannel("A").tobytes() == reference.getchannel("A").tobytes(), (source, i)
            assert all(a == b for a, b in zip(reference.get_flattened_data(), decoded.get_flattened_data()) if a[3]), (source, i)
    assert sum(durations) == 1500, source
    encoded = (folder / "preview.webp").read_bytes()
    position = 12; webp_durations = []
    while position + 8 <= len(encoded):
        tag = encoded[position:position+4]; length = struct.unpack_from("<I", encoded, position+4)[0]
        if tag == b"ANMF": webp_durations.append(int.from_bytes(encoded[position+20:position+23], "little"))
        position += 8 + length + length % 2
    assert len(webp_durations) == 12 and sum(webp_durations) == 1500, (source, webp_durations)
    report = dict(passed=True, assembly_version=ASSEMBLY_VERSION, id=manifest["id"], atlas_sha256=original_hash, manifest_sha256=digest(folder / "manifest.json"), original_bytes_preserved=digest(source) == original_hash,
                  frames=12, fps=8, duration_ms=sum(durations), canvas=canvas, cell=[cw, ch], atlas_size=[w,h], boxes=boxes, row_cuts=row_cuts,
                  row_ground=medians, normalized_feet=normalized_feet, independent_cell_gutters=True,
                  intended_airborne_frames=sorted(airborne), offsets=offsets,
                  frame_assembly_exact_rgba=True, webp_visible_rgba_lossless=True, frames_distinct=True,
                  game_integration=False, webp_frame_ms=webp_durations, animation_sha256={name: digest(folder / name) for name in ["preview.gif", "preview.webp"]}, frame_records=records)
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
            item["ready"] = (folder / "validation.json").exists() and json.loads((folder / "validation.json").read_text(encoding="utf-8-sig")).get("assembly_version") == ASSEMBLY_VERSION
            if item["ready"]:
                meta = json.loads((folder / "manifest.json").read_text(encoding="utf-8-sig"))
                result = json.loads((folder / "validation.json").read_text(encoding="utf-8-sig"))
                prefix = "Subjects/" + character["id"] + "/"
                item.update(gif=prefix+"preview.gif", webp=prefix+"preview.webp", atlas=prefix+"atlas.png", thumb=prefix+"thumbnail.png", labels=meta["frame_labels"], motion=" → ".join(meta["frame_labels"][i] for i in [1,4,8,10]), sources=meta["sources"], cols=4, rows=3, cell=result["cell"][0], canvas=result["canvas"], offsets=result["offsets"], atlas_size=result["atlas_size"], boxes=result["boxes"])
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
