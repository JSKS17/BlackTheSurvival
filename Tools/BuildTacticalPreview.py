"""Join live card definitions with independently verified pixel-art provenance.

Original references, generated sources and runtime PNGs have separate hashes.
No image pixels or game definitions are modified by this exporter.
"""
import argparse
import base64
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import re

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def project_path(relative, role):
    path = (ROOT / relative).resolve()
    assert path.is_relative_to(ROOT.resolve()), f"{role} must remain in project: {relative}"
    assert path.is_file(), f"Missing {role}: {relative}"
    return path


def validate_artwork(original_manifest, art_manifest):
    assert art_manifest.get("result") == "PASS", "Pixel-art generation must be complete and validated."
    originals = {entry.get("cardId", entry.get("itemId")): entry for entry in original_manifest["entries"]}
    artwork = {entry["id"]: entry for entry in art_manifest["entries"]}
    assert len(originals) == len(artwork) == len(art_manifest["entries"]) == 27
    assert set(artwork) == set(originals)
    checks, guids, runtime_hashes = [], set(), set()
    for asset_id, original in originals.items():
        entry = artwork[asset_id]
        reference = project_path(original["sourceReferencePath"], "original reference")
        assert entry["referencePath"] == original["sourceReferencePath"], asset_id
        assert sha(reference) == original["sha256"] == original["sourceSha256"] == entry["originalSha256"], asset_id
        assert reference.stat().st_size == original["bytes"], asset_id
        with Image.open(reference) as image:
            assert list(image.size) == original["size"], asset_id
        generated = project_path(entry["generatedPath"], "generated source")
        assert sha(generated) == entry["generatedSha256"] != original["sha256"], asset_id
        generation_record = project_path(entry["generationRecordPath"], "generation record")
        generation = json.loads(generation_record.read_text(encoding="utf-8-sig"))
        assert generation["id"] == asset_id and generation.get("prompt") and generation.get("tool"), asset_id
        assert project_path(generation["referencePath"], "generation's original reference") == reference, asset_id
        assert entry.get("processing"), f"Missing pixel processing description: {asset_id}"
        assert entry["runtimePath"] == original["runtimePath"], asset_id
        runtime = project_path(entry["runtimePath"], "runtime icon")
        runtime_sha = sha(runtime)
        assert runtime_sha == entry["runtimeSha256"] != original["sha256"], asset_id
        assert runtime_sha not in runtime_hashes, f"Duplicate runtime artwork: {asset_id}"
        runtime_hashes.add(runtime_sha)
        expected_size = [64, 64] if asset_id == "tactical_module" else [96, 96]
        assert entry["size"] == expected_size, asset_id
        with Image.open(runtime) as image:
            assert image.mode == "RGBA" and list(image.size) == expected_size, asset_id
            alpha_range = list(image.getchannel("A").getextrema())
        assert alpha_range == ([0, 255] if asset_id == "tactical_module" else [255, 255]), asset_id
        meta = runtime.with_suffix(runtime.suffix + ".meta").read_text(encoding="utf-8-sig")
        guid = re.search(r"^guid: ([0-9a-f]{32})$", meta, re.M).group(1)
        assert guid not in guids, f"Duplicate Unity GUID: {asset_id}"
        guids.add(guid)
        for pattern in (r"^\s+filterMode: 0$", r"^\s+textureType: 0$", r"^\s+enableMipMap: 0$"):
            assert re.search(pattern, meta, re.M), f"Unexpected Unity texture import: {asset_id}"
        checks.append({"id": asset_id, "sourceReferencePath": original["sourceReferencePath"],
                       "sourceUrl": original["sourceUrl"], "originalSha256": original["sha256"],
                       "originalSize": original["size"], "generatedPath": entry["generatedPath"],
                       "generatedSha256": entry["generatedSha256"], "generationRecordPath": entry["generationRecordPath"],
                       "generationRecordSha256": sha(generation_record), "runtimePath": entry["runtimePath"],
                       "runtimeSha256": runtime_sha, "runtimeSize": expected_size, "runtimeAlphaRange": alpha_range,
                       "processing": entry["processing"], "guid": guid, "pointTexture2D": True})
    return artwork, checks


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--data", type=Path, default=ROOT / "Temp/TacticalPreview/runtime-cards.json")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--art-manifest", type=Path, default=ROOT / "docs/art-tactical-pixel/output-manifest.json")
    args = parser.parse_args()
    payload = json.loads(args.data.read_text(encoding="utf-8-sig"))
    refs_path = ROOT / "docs/tactical-references/tactical-original-catalog.json"
    refs = json.loads(refs_path.read_text(encoding="utf-8"))
    original_manifest_path = ROOT / "docs/tactical-references/original-icons-manifest.json"
    manifest = json.loads(original_manifest_path.read_text(encoding="utf-8"))
    art_manifest = json.loads(args.art_manifest.read_text(encoding="utf-8-sig"))
    artwork, artwork_checks = validate_artwork(manifest, art_manifest)
    by_art_id = {check["id"]: check for check in artwork_checks}
    by_id = {card["cardId"]: card for card in refs["cards"]}
    status_labels = {
        "normal_catalog": "원작 일반 전술",
        "cobalt_catalog": "원작 코발트 전술",
        "legacy_removed": "과거 전술 · 원작에서 삭제",
        "legacy_retained": "과거 코발트 전술",
        "tactical_replacement_infusion": "코발트 전술 교체 인퓨전",
    }
    assert payload["total"] == 26 and payload["added"] == 23
    assert len(payload["cards"]) == len(by_id) == 26
    assert {card["id"] for card in payload["cards"]} == set(by_id)
    icon_checks = []
    for card in payload["cards"]:
        original = by_id[card["id"]]
        assert card["name"] in (original["displayNameKo"], original["originalNameKo"]), card["id"]
        check = by_art_id[card["id"]]
        assert original["sha256"] == check["originalSha256"] and original["sourceReferencePath"] == check["sourceReferencePath"], card["id"]
        icon = project_path(check["runtimePath"], "runtime skill icon")
        card["icon"] = "data:image/png;base64," + base64.b64encode(icon.read_bytes()).decode("ascii")
        card["originLabel"] = "코발트 출신" if card["cobalt"] else "일반 전술 출신"
        card["statusLabel"] = status_labels[original["sourceStatus"]]
        source_keys = [original.get("statusEvidence", {}).get("sourceKey")]
        source_keys += [entry.get("sourceKey") for entry in original["cooldownReferences"]]
        card["sourceUrls"] = list(dict.fromkeys(refs["sources"][key]["url"] for key in source_keys if key in refs["sources"]))[:2]
        assert card["sourceUrls"]
        for variant in ("base", "enhanced"):
            assert card[variant]["description"] and card[variant]["summary"]
            assert "\ufffd" not in card[variant]["description"]
        icon_checks.append(check)
    module = by_art_id["tactical_module"]
    module_path = project_path(module["runtimePath"], "runtime module icon")
    payload["moduleIcon"] = "data:image/png;base64," + base64.b64encode(module_path.read_bytes()).decode("ascii")
    encoded = json.dumps(payload, ensure_ascii=False, separators=(",", ":")).replace("<", "\\u003c")
    template = (ROOT / "docs/tactical-references/preview-template.html").read_text(encoding="utf-8")
    assert template.count("__TACTICAL_PREVIEW_DATA__") == 1
    fragment = template.replace("__TACTICAL_PREVIEW_DATA__", encoded)
    assert len(fragment.encode("utf-8")) < 1_000_000
    assert not fragment.lstrip().lower().startswith("<!doctype")
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(fragment, encoding="utf-8")

    lines = ["# 전술 스킬과 전술강화 모듈", "", "원작 이름이 있는 전술 26종(기존 3종 + 추가 23종)을 지원합니다. 초월은 제외합니다.",
             "과거에 삭제된 전술과 코발트의 전술 교체 인퓨전도 포함하며, 현재 원작에서 모두 선택 가능하다는 뜻은 아닙니다.",
             "아래 코스트와 효과는 실제 게임 코드에서 내보낸 턴제 밸런스입니다. 일반 +·레어 ++와 전술 강화는 각각 별도로 적용됩니다.", "",
             "| 카드 | 출신 / 원작 분류 | 코스트 | 기본 효과 요약 | 모듈 추가 효과 |", "|---|---|---:|---|---|"]
    for card in payload["cards"]:
        cells = [card["name"], card["statusLabel"], str(card["base"]["cost"]), card["base"]["summary"], card["moduleChange"]]
        lines.append("| " + " | ".join(cell.replace("|", "\\|").replace("\n", " ") for cell in cells) + " |")
    lines += ["", "## 조우 보상", "", "모듈: " + " · ".join(s["name"] for s in payload["moduleSubjects"]) + "의 일반 조우 1개 / 레어 조우 2개.",
              "가방에서 원하는 보유 F 카드 한 장을 선택해 확정할 때만 모듈 1개를 소비합니다. 취소, 대상 없음, 이미 전술 강화된 카드에는 소비하지 않습니다. 전투 중에는 사용할 수 없습니다.", "",
              "| 실험체 | 판매 카드 | 일반 조우 | 레어 조우 |", "|---|---|---:|---:|"]
    for card in payload["cards"]:
        for sale in card["sales"]:
            lines.append(f'| {sale["subject"]} | {card["name"]} | {sale["normalPrice"]} CR | 전술 강화된 한 장 / {sale["rareEnhancedPrice"]} CR |')
    lines += ["", "원작 아이콘을 참고해 새로 그린 도트 아이콘과 원작 자료: [참조·생성·런타임 검증 범위](tactical-references/README.md)."]
    (ROOT / "docs/TACTICAL_SKILLS.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    evidence = {"result": "PASS", "checkedAtUtc": datetime.now(timezone.utc).isoformat(), "cards": 26, "added": 23,
                "runtimeDataSha256": sha(args.data), "originalCatalogSha256": sha(refs_path),
                "originalReferenceManifestSha256": sha(original_manifest_path), "pixelArtManifestPath": str(args.art_manifest.resolve()),
                "pixelArtManifestSha256": sha(args.art_manifest),
                "fragmentPath": str(args.output.resolve()), "fragmentBytes": args.output.stat().st_size,
                "fragmentSha256": sha(args.output), "iconChecks": icon_checks,
                "moduleIconSha256": module["runtimeSha256"], "moduleOriginalReferenceSha256": module["originalSha256"],
                "exportedAtUtc": payload["generatedAtUtc"]}
    (ROOT / "docs/tactical-references/preview-validation.json").write_text(json.dumps(evidence, ensure_ascii=False, indent=2), encoding="utf-8")
    icon_evidence = {"schemaVersion": 2, "result": "PASS", "checkedAtUtc": evidence["checkedAtUtc"],
                     "images": 27, "uniqueGuids": len({check["guid"] for check in artwork_checks}),
                     "originalReferencesVerified": 27, "generatedSourcesVerified": 27, "runtimeRedrawsVerified": 27,
                     "allOriginalReferenceBytesMatch": True, "allRuntimeHashesMatchPixelArtManifest": True,
                     "runtimeIconsAreOriginalBytes": False, "allPointTexture2D": True,
                     "pixelArtManifestPath": "docs/art-tactical-pixel/output-manifest.json",
                     "pixelArtManifestSha256": sha(args.art_manifest), "entries": artwork_checks}
    (ROOT / "docs/tactical-references/icon-validation.json").write_text(json.dumps(icon_evidence, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    implementation_path = ROOT / "docs/tactical-references/implementation-validation.json"
    if implementation_path.is_file():
        implementation = json.loads(implementation_path.read_text(encoding="utf-8"))
        implementation["artworkValidation"] = {"status": "PASS", "checkedAtUtc": evidence["checkedAtUtc"],
            "report": "docs/tactical-references/icon-validation.json",
            "reportSha256": sha(ROOT / "docs/tactical-references/icon-validation.json"),
            "pixelArtManifest": "docs/art-tactical-pixel/output-manifest.json", "pixelArtManifestSha256": sha(args.art_manifest),
            "originalReferencesVerified": 27, "generatedSourcesVerified": 27, "runtimeRedrawsVerified": 27,
            "note": "Independent file/hash/import verification; this does not establish a new browser gameplay or release-build pass."}
        implementation_path.write_text(json.dumps(implementation, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: evidence[key] for key in ("result", "cards", "added", "fragmentPath", "fragmentBytes")}, ensure_ascii=True))


if __name__ == "__main__":
    main()
