# Ultimate preview frame isolation

Newly redrawn, normalized sheets opt into assembly v5 with this manifest contract:

```json
{
  "columns": 4,
  "rows": 3,
  "frame_layout": "fixed-grid",
  "frame_gutter": 24,
  "generated_source_file": "generated-sheet.png",
  "generated_source_sha256": "SHA256 of the unmodified generated PNG",
  "normalization": "normalization.json"
}
```

`frame_gutter` is a minimum of 20 **normalized-atlas pixels inside every side of
every cell**, including the outside sheet edges. The normalizer uses 24 pixels.
The generated sheet needs readable, separate poses with transparent gaps and no
drawn grid lines. It need not hit an exact mathematical boundary: the original
sheet stays byte-for-byte intact in `generated-sheet.png`.

Normalize one selected generated sheet with:

```powershell
python Tools/NormalizeUltimateAtlas.py --input RAW.png --output STAGE/atlas.png --manifest STAGE/manifest.json
```

The raw source is partitioned by shared row cuts and shared column cuts within
each row, never independent per-cell vertical cuts. Boxes cover every source
pixel exactly once. Alpha >= 160 touching the two pixels on either side of an
internal cut stops normalization. Fainter contacts are reported and copied;
source outer-edge contacts are reported as possible pre-existing clipping.
The normalizer first restricts cut candidates to two-pixel bands with no
alpha >= 160, within the original +/-22% nominal-cell search radius. Among
these it minimizes the weighted five-pixel gap score, then the distance to the
nominal boundary. This finds a real narrow gap that a wider score alone could
miss. If no clear solid-alpha band exists in the permitted range, it fails.
This constrained selection does not change the legacy v4 cutter.

FFmpeg crops those boxes, translates them by integer offsets, pads with
transparent pixels and stacks them into equal square cells. All original frame
RGBA pixels, including transparent pixels, are compared with their corresponding
normalized crop and hashed. By default, each pose retains its position relative
to the nominal source grid with only a shared row baseline correction. Ignored
and airborne frame indices are read from the manifest. No frame is horizontally
centered, painted or resampled. The normalized atlas canvas is larger, while its
art pixels keep their original dimensions and colours.

For a new sheet with unintended frame-to-frame foot-height drift, opt in before
running normalization:

```json
{
  "ground_alignment": "per-frame",
  "intended_airborne_frames": [2, 3],
  "alignment_ignore_frames": [4],
  "actor_absent_frames": [5],
  "foot_anchors": {
    "0": [180, 330],
    "1": [540, 333]
  }
}
```

Indices are zero-based. Anchor coordinates in this example are illustrative;
choose the actual foot baseline in **generated-sheet.png**, using absolute raw
atlas `[x, y]` integer coordinates. The point must lie within that frame's raw
source box. An anchor overrides the dark-silhouette `foot_y` heuristic, so a
lower weapon or dark effect does not determine the actor's baseline. The anchor
does not add, remove or alter a pixel. You may provide only the ambiguous frames.
Anchors always refer to the preserved raw source, not the larger normalized
atlas. Re-normalize from that raw source when revising anchors.

In `per-frame` mode, every measured grounded frame receives an integer vertical
translation to the same common foot height. Horizontal motion is untouched.
Without an explicit `airborne_lift` override below, frames in any of the three
exclusion lists keep the default shared-row translation and their original
intra-row vertical motion, preserving deliberate jumps, transformations and
actor-free effects. In particular, list Bihyung's
airborne/size-change poses explicitly rather than flattening those motions.
If a grounded frame has no detectable foot baseline, normalization fails and
requests an explicit anchor or an appropriate motion exclusion.

The report records each measurement method, source and output baseline,
exclusion and integer translation. It verifies that all included grounded
baselines agree, that complete source crops still match their RGBA hashes, and
that the transparent guard remains at least 24 pixels on all sides. The canvas
expands if necessary; an upward/downward translation never clips artwork.
For verified normalized sheets declaring `ground_alignment: "per-frame"`, the
preview assembler applies no second row correction, preventing its fallback
heuristic from undoing an explicit foot anchor. This verified mode is included
in its cache check. Existing sheets without this declaration keep their prior
alignment behavior and are not automatically regenerated.

An animation with an intended jump can also specify its selected foot's height
above the common grounded baseline, in original image pixels:

```json
{
  "ground_alignment": "per-frame",
  "intended_airborne_frames": [2, 3, 4, 5, 6],
  "airborne_lift": {"2": 40, "3": 70, "4": 60, "5": 45, "6": 100}
}
```

Only explicitly airborne, present actors may receive a lift. Heights must be
nonnegative integers (zero means the grounded baseline), and at least one
measured grounded reference must exist. The foot must be measured by the
heuristic or an explicit raw-source `foot_anchors` entry. Use an anchor whenever
a hanging weapon or effect extends below the real boot; a knee-tucked pose's
actual foot can be much higher than its downward-pointing weapon.

For each requested frame the integer translation is exactly
`ground_reference_y - lift - source_nominal_foot_y`. It moves the entire original
crop together, preserving every source RGBA pixel. It does not change pose,
scale or anatomy. Unspecified airborne frames keep their previous shared-row
translation. The square cell expands if necessary to retain all artwork and
the transparent guard.

`airborne_lift_proof` records both coordinate checks:
`applied_nominal_foot_y == ground_reference_y - lift` and
`normalized_cell_foot_y == normalized_ground_reference_y - lift`. The preview
assembler validates this proof against the current manifest and keeps the
normalized positions, including during cache reuse. Editing lift values without
re-normalizing the raw source is rejected rather than displaying a stale layout.

`normalization.json` records raw/crop/normalized hashes and the exact source and
destination rectangles. The manifest receives the normalized atlas hash and
dimensions as well as raw-source provenance. The assembler verifies these
records and source hashes before accepting a normalized cache.

Cell boundaries are `round(width * column / 4)` and
`round(height * row / 3)`. The 12 boxes cover the sheet exactly once and cannot
overlap, even when dimensions are not divisible by 4 and 3. Layout does not move
boundaries to chase particles or neighbouring poses.

The validator counts alpha values of at least 64 and at least 160 on all four
guard bands. Any alpha >= 64 in a band stops assembly and identifies the frame,
side and counts. It never removes artwork to make the test pass. Fainter alpha
is reported as `alpha_nonzero` and preserved; the check does not claim every
guard pixel has alpha zero. A visibly wrong source sheet needs regeneration.

Each crop is padded onto its own empty transparent canvas. Lossless WebP is
decoded back and compared with every independently assembled frame, including
the entire alpha channel. v5 GIF disables `offsetting` and `transdiff`, encodes a
full canvas for all 12 frames, and verifies disposal method 2 (clear to
background). Its decoded alpha must equal the independent frame at GIF's
alpha threshold of 64. A moving effect cannot survive into a later frame unless
that later source cell actually contains it.

v5 adds only 32 outer pixels beyond the already padded normalized cell, avoiding
the extra large v4 border that would shrink character faces in the unchanged
preview viewport. This translates and pads artwork; it does not resize pixels.
The selected outer padding is part of v5's cache validation.

The gallery replaces a single element's atlas background and clipping rectangle
for each frame; it does not accumulate drawings. Fixed, disjoint source boxes
and their transparent margins keep neighbouring effects out of that rectangle,
including at fractional responsive display sizes. Browser verification still
needs to run against the actual completed catalogue.

Legacy manifests without `frame_layout` retain assembly v4 and their existing
cache keys. This change does not regenerate approved animations. Only the new
fixed-grid manifests use v5. A stale v5 validation hash cannot mark a revised
sheet ready in the catalogue.

Run the focused geometric/encoding fixture checks with:

```powershell
python Tools/TestUltimateFrameIsolation.py
```

The test creates temporary geometric marker sheets, validates actual GIF/WebP
output and removes only its own verified temporary fixture directories. It does
not read, redraw or replace character assets, and does not rebuild the gallery.
