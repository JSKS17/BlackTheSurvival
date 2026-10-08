"""Mechanical asset preparation for individually generated identity portraits.

Only source copies, alpha-bound crops, nearest-neighbour resize, transparent
padding and documentation sheets. No drawing or face modification occurs here.
"""
import argparse
import hashlib
import json
import os
import shutil
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
DOC = ROOT / "docs/art-identity"
OUT = ROOT / "Assets/Resources/Lumia/PortraitsIdentity"
REFS = ROOT / "docs/art-chibi/reference-characters.json"

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def write_json(path, value):
    temporary = Path(str(path) + "." + str(os.getpid()) + ".tmp")
    temporary.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding="utf-8")
    temporary.replace(path)

def write_png(image, path):
    temporary = Path(str(path) + "." + str(os.getpid()) + ".tmp")
    image.save(temporary, format="PNG")
    temporary.replace(path)

def save_asset(asset_id, source, prompt, face):
    refs = json.loads(REFS.read_text(encoding="utf-8-sig"))
    ref = next(v for v in refs if v["id"] == asset_id)
    source = Path(source).resolve()
    raw = DOC / "generated" / (asset_id + ".png")
    raw.parent.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    if raw.exists() and sha(raw) != sha(source):
        archive = DOC / "generated" / "versions"
        archive.mkdir(parents=True, exist_ok=True)
        shutil.copy2(raw, archive / (asset_id + "_" + sha(raw)[:12] + ".png"))
        prior_record = DOC / "generation" / (asset_id + ".json")
        if prior_record.exists():
            record_archive = DOC / "generation" / "versions"
            record_archive.mkdir(parents=True, exist_ok=True)
            shutil.copy2(prior_record, record_archive / (asset_id + "_" + sha(raw)[:12] + ".json"))
    shutil.copy2(source, raw)
    im = Image.open(raw).convert("RGBA")
    box = im.getchannel("A").getbbox()
    if not box:
        raise ValueError("Empty alpha: " + asset_id)
    crop = im.crop(box)
    scale = min(152 / crop.width, 152 / crop.height)
    size = (max(1, round(crop.width * scale)), max(1, round(crop.height * scale)))
    sprite = crop.resize(size, Image.Resampling.NEAREST)
    canvas = Image.new("RGBA", (160, 160))
    canvas.alpha_composite(sprite, ((160 - size[0]) // 2, 156 - size[1]))
    target = OUT / (asset_id + ".png")
    write_png(canvas, target)
    record = {
        "id": asset_id, "name": ref["name"], "tool": "builtin image_gen",
        "intent": "style-transfer", "faceDesign": face,
        "originalReference": ref["reference_path"],
        "originalSourceUrl": ref["original_url"],
        "referenceSha256": sha(ROOT / ref["reference_path"]),
        "prompt": prompt, "generatedSource": str(raw.relative_to(ROOT)).replace("\\", "/"),
        "generatedSha256": sha(raw), "generatedSize": list(im.size),
        "alphaCropBox": list(box), "resizedSize": list(size),
        "path": str(target.relative_to(ROOT)).replace("\\", "/"),
        "width": 160, "height": 160, "sha256": sha(target),
        "processing": "Alpha-bound crop, nearest-neighbour resize, transparent padding."
    }
    generation = DOC / "generation" / (asset_id + ".json")
    generation.parent.mkdir(parents=True, exist_ok=True)
    write_json(generation, record)
    prompts = DOC / "prompts"
    prompts.mkdir(parents=True, exist_ok=True)
    (prompts / (asset_id + ".txt")).write_text(prompt, encoding="utf-8")
    (prompts / (asset_id + "-face.txt")).write_text(face, encoding="utf-8")
    rebuild()
    print(json.dumps({"id": asset_id, "path": record["path"], "size": list(canvas.size), "sha256": record["sha256"]}))

def rebuild():
    records = [json.loads(p.read_text(encoding="utf-8")) for p in sorted((DOC / "generation").glob("*.json"))]
    refs = json.loads(REFS.read_text(encoding="utf-8-sig"))
    order = {v["id"]: i for i, v in enumerate(refs)}
    records.sort(key=lambda v: order[v["id"]])
    prompts = DOC / "prompts"
    prompts.mkdir(parents=True, exist_ok=True)
    for rec in records:
        for suffix, content in ((".txt", rec["prompt"]), ("-face.txt", rec["faceDesign"])):
            path = prompts / (rec["id"] + suffix)
            if not path.exists() or path.read_text(encoding="utf-8") != content:
                temporary = Path(str(path) + "." + str(os.getpid()) + ".tmp")
                temporary.write_text(content, encoding="utf-8")
                temporary.replace(path)
    index = {"schema": 1, "expected": 92, "completed": len(records), "logicalCanvas": [160, 160],
             "processingPolicy": "Individually generated portraits; mechanical crop, nearest scaling and padding only.",
             "assets": records}
    write_json(DOC / "asset-index.json", index)
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 15)
        title = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 23)
    except OSError:
        font = title = ImageFont.load_default()
    sheets = DOC / "contact-sheets"
    sheets.mkdir(parents=True, exist_ok=True)
    for batch in range((len(records) + 11) // 12):
        sheet = Image.new("RGB", (1024, 760), (25, 30, 42))
        draw = ImageDraw.Draw(sheet)
        draw.text((18, 8), "캐릭터 얼굴과 눈매 · 160px · " + str(batch + 1), font=title, fill=(235, 239, 252))
        for j, rec in enumerate(records[batch * 12:(batch + 1) * 12]):
            x, y = (j % 4) * 256, 48 + (j // 4) * 236
            sprite = Image.open(ROOT / rec["path"]).convert("RGBA").resize((208, 208), Image.Resampling.NEAREST)
            sheet.paste(sprite, (x + 24, y), sprite)
            draw.text((x + 16, y + 209), rec["name"] + " · " + rec["id"], font=font, fill=(215, 223, 238))
        write_png(sheet, sheets / ("sprites_%02d.png" % (batch + 1)))
    facesheets = DOC / "face-sheets"
    facesheets.mkdir(parents=True, exist_ok=True)
    for batch in range((len(records) + 11) // 12):
        sheet = Image.new("RGB", (1024, 760), (25, 30, 42))
        draw = ImageDraw.Draw(sheet)
        draw.text((18, 8), "눈매·표정 확대 검토 · " + str(batch + 1), font=title, fill=(235, 239, 252))
        for j, rec in enumerate(records[batch * 12:(batch + 1) * 12]):
            x, y = (j % 4) * 256, 48 + (j // 4) * 236
            sprite = Image.open(ROOT / rec["path"]).convert("RGBA")
            # Top half shows the authored face and hair. This is a QA crop only.
            crop = sprite.crop((25, 0, 135, 92))
            crop = crop.resize((220, 184), Image.Resampling.NEAREST)
            sheet.paste(crop, (x + 18, y + 12), crop)
            draw.text((x + 16, y + 209), rec["name"] + " · " + rec["id"], font=font, fill=(215, 223, 238))
        write_png(sheet, facesheets / ("faces_%02d.png" % (batch + 1)))
    record_ids = {r["id"] for r in records}
    runtime_ids = {p.stem for p in OUT.glob("*.png")}
    validation = {"expected": 92, "actual": len(records), "runtimePngCount": len(runtime_ids),
                  "missing": [v["id"] for v in refs if v["id"] not in record_ids], "issues": [],
                  "duplicateGeneratedSourceHashes": len(records) - len({r["generatedSha256"] for r in records}),
                  "duplicateRuntimeHashes": len(records) - len({r["sha256"] for r in records}),
                  "generatedSourceHashMismatches": 0, "originalReferenceHashMismatches": 0,
                  "runtimeHashMismatches": 0, "promptMismatches": 0}
    if runtime_ids != record_ids:
        validation["issues"].append("Runtime PNG ids and generation records do not match.")
    if len({r["generatedSha256"] for r in records}) != len(records):
        validation["issues"].append("Generated source hashes are duplicated.")
    if len({r["sha256"] for r in records}) != len(records):
        validation["issues"].append("Runtime PNG hashes are duplicated.")
    for rec in records:
        p = ROOT / rec["path"]
        im = Image.open(p).convert("RGBA")
        a = im.getchannel("A")
        if im.size != (160, 160): validation["issues"].append(rec["id"] + ": size")
        if a.getextrema() != (0, 255): validation["issues"].append(rec["id"] + ": no real transparency/opacity")
        if sha(p) != rec["sha256"]:
            validation["issues"].append(rec["id"] + ": hash")
            validation["runtimeHashMismatches"] += 1
        if sha(ROOT / rec["generatedSource"]) != rec["generatedSha256"]:
            validation["issues"].append(rec["id"] + ": generated source hash")
            validation["generatedSourceHashMismatches"] += 1
        if sha(ROOT / rec["originalReference"]) != rec["referenceSha256"]:
            validation["issues"].append(rec["id"] + ": original reference hash")
            validation["originalReferenceHashMismatches"] += 1
        for extra in rec.get("additionalReferences", []):
            if sha(ROOT / extra["path"]) != extra["sha256"]:
                validation["issues"].append(rec["id"] + ": additional reference hash")
                validation["originalReferenceHashMismatches"] += 1
        if not rec["faceDesign"].strip() or not rec["prompt"].strip():
            validation["issues"].append(rec["id"] + ": missing face design/prompt")
        if (DOC / "prompts" / (rec["id"] + ".txt")).read_text(encoding="utf-8") != rec["prompt"]:
            validation["issues"].append(rec["id"] + ": recorded prompt does not match")
            validation["promptMismatches"] += 1
        box = a.getbbox()
        if box is None or box[0] < 4 or box[1] < 4 or box[2] > 156 or box[3] > 156:
            validation["issues"].append(rec["id"] + ": transparent padding")
    validation["complete"] = len(records) == len(refs) and not validation["missing"] and not validation["issues"]
    write_json(DOC / "validation.json", validation)

if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--id")
    ap.add_argument("--source")
    ap.add_argument("--prompt-file")
    ap.add_argument("--face-file")
    ap.add_argument("--rebuild", action="store_true")
    args = ap.parse_args()
    if args.rebuild:
        rebuild()
    else:
        save_asset(args.id, args.source, Path(args.prompt_file).read_text(encoding="utf-8"), Path(args.face_file).read_text(encoding="utf-8"))
