"""Repack one generated sheet with crop, integer translation and transparent pad.

The source is preserved as generated-sheet.png. Shared row/column cuts partition
every source pixel exactly once. No painting, filtering or resampling is used.
"""
import argparse
import hashlib
import json
import statistics
from pathlib import Path
from PIL import Image
import BuildUltimatePreviews as builder


def rgba_hash(image):
    return hashlib.sha256(image.tobytes()).hexdigest()


def boundary_record(atlas, axis, position, span, label):
    """Inspect both source pixels touching a proposed internal partition."""
    alpha = atlas.getchannel("A")
    left, top, right, bottom = span
    band = (position-1, top, position+1, bottom) if axis == "x" else (left, position-1, right, position+1)
    histogram = alpha.crop(band).histogram()
    return dict(label=label, axis=axis, position=position, band=list(band),
                alpha_nonzero=sum(histogram[1:]), alpha_ge_64=sum(histogram[64:]),
                alpha_ge_160=sum(histogram[160:]))


def constrained_transparent_cuts(image, axis, sections):
    """Find actual clear partition bands before ranking wider-gap scores.

    A five-pixel score can prefer a cut touching four opaque pixels over a
    neighbouring real two-pixel gap. Restrict candidates to clear two-pixel
    solid-alpha bands first. No source pixel or alpha value is modified.
    This policy is local to normalization; legacy v4 gutters are unchanged.
    """
    assert axis in ["x", "y"] and sections >= 2
    width, height = image.size
    length = height if axis == "y" else width
    alpha = image.getchannel("A")
    weighted, solid = [], []
    for position in range(length):
        band = (0, position, width, position+1) if axis == "y" else (position, 0, position+1, height)
        histogram = alpha.crop(band).histogram()
        solid.append(sum(histogram[160:]))
        weighted.append(solid[-1]*3+sum(histogram[64:160]))
    cuts = [0]
    for index in range(1, sections):
        expected = round(length*index/sections)
        radius = round(length/sections*.22)
        candidates = list(range(max(1, expected-radius), min(length-1, expected+radius)+1))
        clear = [position for position in candidates if solid[position-1]+solid[position] == 0]
        assert clear, ("Solid artwork touches a shared source partition; no zero-solid two-pixel cut exists "
                       f"within the original search range: axis={axis}, partition={index}, "
                       f"expected={expected}, range=[{candidates[0]}, {candidates[-1]}], "
                       f"minimum_alpha_ge_160={min(solid[position-1]+solid[position] for position in candidates)}")
        cut = min(clear, key=lambda position: (sum(weighted[max(0, position-2):min(length, position+3)]),
                                               abs(position-expected), position))
        cuts.append(cut)
    return cuts+[length]


def shared_partition(atlas):
    """Use a single common row partition, then common columns within each row."""
    width, height = atlas.size
    rows = constrained_transparent_cuts(atlas, "y", 3)
    boxes, columns, boundaries = [], [], []
    for index, position in enumerate(rows[1:-1]):
        boundaries.append(boundary_record(atlas, "y", position, (0, 0, width, height), f"row-{index+1}/{index+2}"))
    for row in range(3):
        top, bottom = rows[row:row+2]
        cuts = constrained_transparent_cuts(atlas.crop((0, top, width, bottom)), "x", 4)
        columns.append(cuts)
        for index, position in enumerate(cuts[1:-1]):
            boundaries.append(boundary_record(atlas, "x", position, (0, top, width, bottom), f"row-{row+1}-column-{index+1}/{index+2}"))
        boxes.extend([[cuts[col], top, cuts[col+1], bottom] for col in range(4)])
    builder.assert_disjoint_boxes(boxes, atlas.size)
    assert sum((right-left)*(bottom-top) for left, top, right, bottom in boxes) == width*height, "Source partition must preserve every pixel exactly once"
    violations = [record for record in boundaries if record["alpha_ge_160"]]
    assert not violations, "Solid artwork touches a shared source partition; regenerate the source without erasing pixels: " + json.dumps(violations, ensure_ascii=False)
    return boxes, rows, columns, boundaries


def ground_alignment(frames, boxes, size, manifest):
    """Measure feet and choose integer translations without changing artwork.

    Row alignment remains the default. Per-frame alignment is opt-in, and
    explicitly excluded motion keeps its original intra-row vertical position.
    Manifest foot anchors use absolute generated-source atlas coordinates.
    """
    mode = manifest.get("ground_alignment", "row")
    assert mode in ["row", "per-frame"], f"Unsupported ground_alignment: {mode!r}"
    width, height = size
    exclusion_fields = ["intended_airborne_frames", "alignment_ignore_frames", "actor_absent_frames"]
    exclusions = {key: set(manifest.get(key, [])) for key in exclusion_fields}
    ignored = set().union(*exclusions.values())
    assert all(type(index) is int and 0 <= index < len(frames) for index in ignored), "Alignment exclusions must use zero-based frame indices"
    feet = [builder.foot_y(frame) for frame in frames]
    methods = ["heuristic" if foot is not None else "unmeasured" for foot in feet]
    anchors = manifest.get("foot_anchors", {})
    assert isinstance(anchors, dict), "foot_anchors must map zero-based frame indices to [source_x, source_y]"
    explicit = set()
    for key, point in anchors.items():
        assert (type(key) is int or isinstance(key, str) and key.isdigit()), f"Invalid foot anchor frame index: {key!r}"
        index = int(key)
        assert 0 <= index < len(frames) and index not in explicit, f"Invalid or duplicate foot anchor frame index: {key!r}"
        assert isinstance(point, (list, tuple)) and len(point) == 2 and all(type(value) is int for value in point), f"Foot anchor {key} must contain integer [source_x, source_y]"
        x, y = point
        left, top, right, bottom = boxes[index]
        assert left <= x < right and top <= y < bottom, ("Foot anchor lies outside its source frame", index, point, boxes[index])
        feet[index] = y-top
        methods[index] = "manifest"
        explicit.add(index)
    grounded = [index for index in range(len(frames)) if index not in ignored]
    unmeasured = [index for index in grounded if feet[index] is None]
    assert mode != "per-frame" or not unmeasured, ("Cannot align unmeasured grounded feet; add foot_anchors or correctly exclude airborne/absent frames", unmeasured)
    relative_feet = [None if foot is None else foot+boxes[index][1]-round(index//4*height/3)
                     for index, foot in enumerate(feet)]
    medians = []
    for row in range(3):
        candidates = [relative_feet[index] for index in range(row*4, row*4+4)
                      if index not in ignored and relative_feet[index] is not None]
        medians.append(int(statistics.median(candidates)) if candidates else None)
    ground = next((median for median in medians if median is not None), 0)
    medians = [ground if median is None else median for median in medians]
    shifts = [ground-medians[index//4] for index in range(len(frames))]
    if mode == "per-frame":
        for index in grounded:
            shifts[index] = ground-relative_feet[index]
    requested_lifts = manifest.get("airborne_lift", {})
    assert isinstance(requested_lifts, dict), "airborne_lift must map zero-based airborne frame indices to nonnegative integer pixel heights"
    lifts = {}
    if requested_lifts:
        assert mode == "per-frame" and grounded, "airborne_lift requires per-frame alignment and a measured grounded reference"
    for key, lift in requested_lifts.items():
        assert type(key) is int or isinstance(key, str) and key.isdigit(), f"Invalid airborne_lift frame index: {key!r}"
        index = int(key)
        assert 0 <= index < len(frames) and index not in lifts, f"Invalid or duplicate airborne_lift frame index: {key!r}"
        assert index in exclusions["intended_airborne_frames"] and index not in exclusions["actor_absent_frames"], ("airborne_lift can only move explicitly airborne, present actors", index)
        assert type(lift) is int and lift >= 0, ("airborne_lift height must be a nonnegative integer", index, lift)
        assert feet[index] is not None, ("airborne_lift requires a detected foot or explicit foot_anchors entry", index)
        lifts[index] = lift
        shifts[index] = ground-lift-relative_feet[index]
        assert relative_feet[index]+shifts[index] == ground-lift
    relative = [[left-round(index%4*width/4), top-round(index//4*height/3)+shifts[index]]
                for index, (left, top, right, bottom) in enumerate(boxes)]
    measurements = [dict(index=index, method=methods[index], cell_foot_y=feet[index],
                         source_foot_y=None if feet[index] is None else feet[index]+boxes[index][1],
                         nominal_foot_y=relative_feet[index], translation_y=shifts[index],
                         airborne_lift=lifts.get(index),
                         exclusions=[key for key in exclusion_fields if index in exclusions[key]])
                    for index in range(len(frames))]
    return dict(mode=mode, relative=relative, feet=feet, relative_feet=relative_feet, row_ground=medians,
                ground_reference_y=ground, frame_shifts=shifts, grounded_frames=grounded,
                unmeasured_grounded_frames=unmeasured, measurements=measurements, airborne_lift=lifts)


def normalize(source, output, manifest_path=None, padding=24):
    source, output = Path(source).resolve(), Path(output).resolve()
    assert source.is_file(), source
    assert output.is_relative_to(builder.ROOT.resolve()), "Output must stay inside the project workspace"
    assert output.suffix.lower() == ".png", "The normalized atlas must be a PNG"
    assert type(padding) is int and padding >= 24, "Use at least 24 source pixels of transparent padding"
    manifest_path = Path(manifest_path).resolve() if manifest_path else None
    assert manifest_path is None or manifest_path.parent == output.parent, "The manifest must share the normalized atlas folder"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig")) if manifest_path and manifest_path.is_file() else {}
    raw_bytes = source.read_bytes()
    raw_hash = hashlib.sha256(raw_bytes).hexdigest()
    with Image.open(source) as opened:
        atlas = opened.convert("RGBA")
    boxes, rows, columns, boundaries = shared_partition(atlas)
    frames = [atlas.crop(box) for box in boxes]
    width, height = atlas.size
    alignment = ground_alignment(frames, boxes, atlas.size, manifest)
    relative = alignment["relative"]
    min_x, min_y = min(point[0] for point in relative), min(point[1] for point in relative)
    max_x = max(point[0]+frame.width for point, frame in zip(relative, frames))
    max_y = max(point[1]+frame.height for point, frame in zip(relative, frames))
    content_width, content_height = max_x-min_x, max_y-min_y
    cell = max(content_width, content_height)+padding*2
    balance_x, balance_y = (cell-content_width)//2, (cell-content_height)//2
    offsets = [[x-min_x+balance_x, y-min_y+balance_y] for x, y in relative]
    assert all(x >= padding and y >= padding and x+frame.width <= cell-padding and y+frame.height <= cell-padding
               for frame, (x, y) in zip(frames, offsets))
    grounded_output_feet = {index: alignment["feet"][index]+offsets[index][1]
                            for index in alignment["grounded_frames"] if alignment["feet"][index] is not None}
    if alignment["mode"] == "per-frame":
        assert len(set(grounded_output_feet.values())) <= 1, ("Grounded frame baselines differ", grounded_output_feet)
    airborne_proof = {}
    for index, lift in alignment["airborne_lift"].items():
        nominal_target = alignment["ground_reference_y"]-lift
        applied_nominal = alignment["relative_feet"][index]+alignment["frame_shifts"][index]
        grounded_output_y = next(iter(grounded_output_feet.values()))
        normalized_foot_y = alignment["feet"][index]+offsets[index][1]
        assert applied_nominal == nominal_target and normalized_foot_y == grounded_output_y-lift, ("Airborne lift differs from the requested height", index, lift)
        airborne_proof[index] = dict(requested_lift=lift, source_nominal_foot_y=alignment["relative_feet"][index],
                                    translation_y=alignment["frame_shifts"][index], target_nominal_foot_y=nominal_target,
                                    applied_nominal_foot_y=applied_nominal, normalized_cell_foot_y=normalized_foot_y,
                                    normalized_ground_reference_y=grounded_output_y)
    output.parent.mkdir(parents=True, exist_ok=True)
    raw_copy = output.parent / "generated-sheet.png"
    assert output != raw_copy, "Normalized atlas and preserved generated source need different paths"
    if raw_copy != source:
        raw_copy.write_bytes(raw_bytes)
    assert builder.digest(raw_copy) == raw_hash
    filters = ["[0:v]format=rgba,split=12"+"".join(f"[source{index}]" for index in range(12))]
    for index, ((left, top, right, bottom), (x, y)) in enumerate(zip(boxes, offsets)):
        filters.append(f"[source{index}]crop={right-left}:{bottom-top}:{left}:{top},pad={cell}:{cell}:{x}:{y}:color=black@0[cell{index}]")
    for row in range(3):
        filters.append("".join(f"[cell{index}]" for index in range(row*4, row*4+4))+f"hstack=inputs=4[row{row}]")
    filters.append("[row0][row1][row2]vstack=inputs=3,format=rgba[atlas]")
    builder.run("-i", raw_copy, "-filter_complex", ";".join(filters), "-map", "[atlas]", "-frames:v", 1, "-threads", 1, output)
    with Image.open(output) as opened:
        normalized = opened.convert("RGBA")
    assert normalized.size == (cell*4, cell*3)
    records = []
    for index, (frame, (x, y)) in enumerate(zip(frames, offsets)):
        col, row = index%4, index//4
        copied_box = [col*cell+x, row*cell+y, col*cell+x+frame.width, row*cell+y+frame.height]
        copied = normalized.crop(copied_box)
        assert copied.tobytes() == frame.tobytes(), ("Normalized frame changed source RGBA pixels", index, boxes[index], copied_box)
        records.append(dict(index=index, source_box=boxes[index], normalized_copy_box=copied_box,
                            offset=offsets[index], source_rgba_sha256=rgba_hash(frame), normalized_rgba_sha256=rgba_hash(copied),
                            normalized_cell_foot_y=None if alignment["feet"][index] is None else alignment["feet"][index]+y))
    _, _, guard = builder.fixed_grid_boxes(normalized, dict(frame_layout="fixed-grid", frame_gutter=padding, columns=4, rows=3))
    edge_records = []
    alpha = atlas.getchannel("A")
    for side, band in [("left", (0, 0, 1, height)), ("right", (width-1, 0, width, height)),
                       ("top", (0, 0, width, 1)), ("bottom", (0, height-1, width, height))]:
        histogram = alpha.crop(band).histogram()
        edge_records.append(dict(side=side, alpha_nonzero=sum(histogram[1:]), alpha_ge_64=sum(histogram[64:]), alpha_ge_160=sum(histogram[160:])))
    report = dict(passed=True, source_file="generated-sheet.png", generated_source_sha256=raw_hash,
                  generated_source_size=[width, height], original_input_path=str(source),
                  normalized_atlas_file=output.name, atlas_sha256=builder.digest(output), atlas_size=list(normalized.size),
                  cell=cell, transparent_padding=padding, operation="FFmpeg crop + integer translation + transparent pad + hstack/vstack; no resampling or painting",
                  source_boxes_disjoint=True, source_pixels_partitioned_exactly_once=True, source_rgba_preserved=True,
                  partition_method="shared cuts; zero-solid two-pixel candidates within nominal +/-22%, then minimum weighted five-pixel score and distance",
                  shared_row_cuts=rows, shared_column_cuts=columns, internal_boundaries=boundaries,
                  boundary_solid_alpha_threshold=160, boundary_faint_pixels_preserved=True,
                  source_outer_edge_contacts=edge_records, source_may_be_clipped=any(edge["alpha_ge_160"] for edge in edge_records),
                  row_ground=alignment["row_ground"], normalized_feet=alignment["relative_feet"], intended_airborne_frames=manifest.get("intended_airborne_frames", []),
                  ground_alignment=alignment["mode"], ground_reference_y=alignment["ground_reference_y"],
                  frame_ground_translations=alignment["frame_shifts"], grounded_frames=alignment["grounded_frames"],
                  unmeasured_grounded_frames=alignment["unmeasured_grounded_frames"], foot_anchor_coordinate_space="generated-source-atlas",
                  foot_measurements=alignment["measurements"], grounded_output_foot_y=grounded_output_feet,
                  grounded_feet_aligned=len(set(grounded_output_feet.values())) <= 1,
                  airborne_lift=alignment["airborne_lift"], airborne_lift_proof=airborne_proof,
                  airborne_lift_verified=True,
                  frame_records=records, fixed_grid_guard=guard)
    report_path = output.parent / "normalization.json"
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    if manifest_path:
        manifest.update(atlas_sha256=report["atlas_sha256"], width=normalized.width, height=normalized.height,
                        generated_source_sha256=raw_hash, generated_source_file="generated-sheet.png",
                        normalization="normalization.json", frame_layout="fixed-grid", frame_gutter=padding,
                        columns=4, rows=3)
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--manifest", type=Path)
    parser.add_argument("--padding", type=int, default=24)
    args = parser.parse_args()
    result = normalize(args.input, args.output, args.manifest, args.padding)
    print(json.dumps({key: result[key] for key in ["passed", "atlas_size", "cell", "source_rgba_preserved", "source_may_be_clipped"]}, ensure_ascii=False))
