"""Combine separately authored image-generation records for final asset processing."""
from pathlib import Path
import argparse
import json

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "docs/art-chibi"


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sprites-only", action="store_true")
    args = parser.parse_args()
    batches = {}
    current = read(ART / "batches.json")
    for batch in current["batches"]:
        if args.sprites_only and batch["kind"] != "portrait":
            continue
        batches[batch["id"]] = batch
    files = ("sprite-agent-batches.json",) if args.sprites_only else ("sprite-agent-batches.json", "icon-batches.json")
    for filename in files:
        for batch in read(ART / filename)["batches"]:
            source = ROOT / batch["source"]
            if not source.is_file():
                raise SystemExit("Generated source missing: " + str(source))
            batches[batch["id"]] = batch
    write(ART / "batches.json", {"schema": 1, "batches": list(batches.values())})
    prompts = read(ART / "prompts.json")
    prompts["prompts"].update(read(ART / "sprite-agent-batches.json").get("prompts", {}))
    for filename in sorted(ART.glob("icon-generation-*.json")):
        record = read(filename)
        prompts["prompts"].update(record.get("prompts", {}))
    prompts["mode"] = "built-in image_gen"
    write(ART / "prompts.json", prompts)
    print("Combined", len(batches), "generated atlases and", len(prompts["prompts"]), "prompts.")


if __name__ == "__main__":
    main()
