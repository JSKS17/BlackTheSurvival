"""Locate authored rectangular tile panels without repainting any source pixels."""
import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]


def intervals(values, minimum):
    selected = values > .08
    starts, spans, start = [], [], None
    for index, active in enumerate(selected):
        if active and start is None:
            start = index
        if start is not None and (not active or index == len(selected) - 1):
            end = index if not active else index + 1
            if end - start >= minimum:
                spans.append((start, end))
            start = None
    return spans


def detect(path, columns, needed_rows, nominal_rows):
    with Image.open(path) as image:
        pixels = np.asarray(image.convert("RGB"), dtype=np.int32)
    height, width = pixels.shape[:2]
    # Uniform navy gutters are compared with the actual outer matte on each row.
    # Detection only affects crop coordinates; it never removes backgrounds.
    matte = np.median(pixels[:, :3], axis=1).astype(np.int32)[:, None, :]
    differs = np.sum((pixels - matte) ** 2, axis=2) > 400
    x_spans = intervals(differs.mean(axis=0), width / columns * .4)
    y_spans = intervals(differs.mean(axis=1), height / nominal_rows * .4)
    if len(x_spans) != columns or len(y_spans) != needed_rows:
        return None
    for axis, spans, count in ((width, x_spans, columns), (height, y_spans, nominal_rows)):
        if any(end - start < axis / count * .45 or end - start > axis / count * 1.1
               for start, end in spans):
            return None
    # Keep one pixel of the original matte around panel edges as a crop margin.
    x_spans = [(max(0, start - 1), min(width, end + 1)) for start, end in x_spans]
    y_spans = [(max(0, start - 1), min(height, end + 1)) for start, end in y_spans]
    return x_spans, y_spans


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--batches", default="docs/art-chibi/icon-batches.json")
    args = parser.parse_args()
    path = ROOT / args.batches
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    reports = []
    for batch in data["batches"]:
        source = ROOT / batch["source"]
        if not source.is_file():
            continue
        items = batch.get("entries", batch.get("ids", []))
        columns, rows = int(batch["columns"]), int(batch["rows"])
        last = max(index for index, item in enumerate(items) if item)
        detected = detect(source, columns, last // columns + 1, rows)
        if detected is None:
            batch.pop("cells", None)
            reports.append({"batch": batch["id"], "method": "equal-grid", "source": batch["source"]})
            print(batch["id"] + ": equal grid (no separate authored gutters detected)")
            continue
        xs, ys = detected
        cells = []
        for index, item in enumerate(items):
            if not item:
                continue
            column, row = index % columns, index // columns
            cell = dict(item) if isinstance(item, dict) else {"id": item}
            cell.update(column=column, row=row, box=[xs[column][0], ys[row][0], xs[column][1], ys[row][1]])
            cells.append(cell)
        batch["cells"] = cells
        reports.append({"batch": batch["id"], "method": "authored-panel-gutters", "source": batch["source"], "xSpans": xs, "ySpans": ys})
        print(batch["id"] + ": located " + str(len(cells)) + " complete authored panels")
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (ROOT / "docs/art-chibi/icon-tile-layout.json").write_text(json.dumps(reports, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
