"""Focused synthetic checks for the v5 sheet contract and transparent playback.

Fixtures contain geometric markers, never game artwork. They are deleted after
the test, and approved subject files/catalogues are not rebuilt or modified.
"""
import hashlib
import json
import shutil
import tempfile
import unittest
from pathlib import Path
from PIL import Image
import BuildUltimatePreviews as builder
import NormalizeUltimateAtlas as normalizer


class FixedGridTests(unittest.TestCase):
    def manifest(self):
        return dict(frame_layout="fixed-grid", frame_gutter=20, columns=4, rows=3)

    def foot_fixture(self, foot_positions, effect_frames=()):
        """Dark geometric actors with separately identifiable feet and effects."""
        image = Image.new("RGBA", (400, 300))
        boxes = [[index%4*100, index//4*100, (index%4+1)*100, (index//4+1)*100] for index in range(12)]
        for index, foot in enumerate(foot_positions):
            left, top, _, _ = boxes[index]
            for y in range(top+foot-18, top+foot+1):
                for x in range(left+40, left+50):
                    image.putpixel((x, y), (40+index*3, 70, 100, 255))
            if index in effect_frames:
                for y in range(top+88, top+91):
                    for x in range(left+15, left+25):
                        image.putpixel((x, y), (30, 90, 100, 255))
        return image, boxes, [image.crop(box) for box in boxes]

    def test_version_is_per_manifest(self):
        self.assertEqual(builder.desired_assembly_version({}), 4)
        self.assertEqual(builder.desired_assembly_version(self.manifest()), 5)
        with self.assertRaises(ValueError):
            builder.desired_assembly_version(dict(frame_layout="unknown"))

    def test_odd_dimensions_cover_once_with_disjoint_cells(self):
        image = Image.new("RGBA", (289, 217))
        boxes, rows, result = builder.fixed_grid_boxes(image, self.manifest())
        self.assertEqual(len(boxes), 12)
        self.assertEqual(rows, [0, 72, 145, 217])
        self.assertTrue(result["boxes_disjoint"])
        self.assertEqual(sum((b[2]-b[0])*(b[3]-b[1]) for b in boxes), 289*217)
        with self.assertRaisesRegex(AssertionError, "Overlapping frame cells"):
            builder.assert_disjoint_boxes([[0, 0, 30, 30], [20, 20, 40, 40]], (50, 50))

    def test_guard_rejects_visible_particle_in_each_direction(self):
        for name, position in [("left", (0, 30)), ("right", (71, 30)), ("top", (30, 0)), ("bottom", (30, 71))]:
            with self.subTest(side=name):
                image = Image.new("RGBA", (288, 216))
                image.putpixel(position, (255, 100, 0, 64))
                with self.assertRaisesRegex(AssertionError, '"frame": 1, "side": "'+name+'"'):
                    builder.fixed_grid_boxes(image, self.manifest())

    def test_guard_reports_but_does_not_erase_faint_alpha(self):
        image = Image.new("RGBA", (288, 216))
        image.putpixel((0, 30), (255, 100, 0, 63))
        before = image.tobytes()
        _, _, result = builder.fixed_grid_boxes(image, self.manifest())
        side = result["cells"][0]["sides"]["left"]
        self.assertEqual(side, dict(alpha_nonzero=1, alpha_ge_64=0, alpha_ge_160=0))
        self.assertEqual(image.tobytes(), before)

    def test_full_frame_encoders_do_not_keep_moving_effects(self):
        root = builder.ROOT / "Temp"
        root.mkdir(exist_ok=True)
        fixture = Path(tempfile.mkdtemp(prefix="ultimate-grid-test-", dir=root))
        fixture_id = "__fixed_grid_fixture_" + fixture.name.rsplit("-", 1)[-1]
        work = builder.ROOT / "Temp/UltimateAssembly" / fixture_id
        try:
            atlas = Image.new("RGBA", (288, 216))
            for index in range(12):
                left, top = index % 4 * 72, index // 4 * 72
                # Distinct moving blocks leave holes where the preceding frame
                # had solid pixels, making a disposal/compositing error observable.
                for y in range(top+27, top+43):
                    for x in range(left+24+index%8, left+29+index%8):
                        atlas.putpixel((x, y), (80+index*7, 90, 140, 255))
                atlas.putpixel((left+46-index%9, top+23), (230, 180, 20, 255))
                for alpha_index, alpha in enumerate([0, 1, 63, 64, 159, 160, 254, 255]):
                    atlas.putpixel((left+23+alpha_index, top+47), (30, 170, 90, alpha))
            source = fixture / "atlas.png"
            atlas.save(source)
            manifest = dict(self.manifest(), id=fixture_id, atlas_sha256=hashlib.sha256(source.read_bytes()).hexdigest())
            (fixture / "manifest.json").write_text(json.dumps(manifest), encoding="utf-8")
            self.assertEqual(builder.assemble(fixture), (fixture_id, "assembled"))
            report = json.loads((fixture / "validation.json").read_text(encoding="utf-8"))
            self.assertEqual(report["assembly_version"], 5)
            self.assertTrue(report["gif_full_frames"])
            self.assertTrue(report["gif_decoded_alpha_matches"])
            self.assertTrue(report["webp_visible_rgba_lossless"])
            self.assertEqual(report["gif_disposal"], [2]*12)
            self.assertEqual(builder.assemble(fixture), (fixture_id, "cached"))
        finally:
            # These are exact temporary fixture paths created by this test.
            # Both resolve below the intended workspace Temp directory.
            for temporary in [fixture, work]:
                if temporary.is_dir() and temporary.resolve().is_relative_to(root.resolve()):
                    shutil.rmtree(temporary)

    def test_shared_partition_repacking_preserves_all_rgba(self):
        root = builder.ROOT / "Temp"
        root.mkdir(exist_ok=True)
        fixture = Path(tempfile.mkdtemp(prefix="ultimate-repack-test-", dir=root))
        fixture_id = "__shared_partition_fixture_" + fixture.name.rsplit("-", 1)[-1]
        work = builder.ROOT / "Temp/UltimateAssembly" / fixture_id
        try:
            raw = Image.new("RGBA", (328, 255))
            for row, (top, bottom) in enumerate([(20, 90), (113, 171), (194, 245)]):
                for col in range(4):
                    for y in range(top, bottom):
                        for x in range(col*82+17, col*82+55):
                            raw.putpixel((x, y), (50+col*20, 50+row*30, 100, 255))
            source = fixture / "raw.png"
            raw.save(source)
            source_bytes = source.read_bytes()
            manifest_path = fixture / "manifest.json"
            manifest_path.write_text(json.dumps(dict(id=fixture_id, intended_airborne_frames=[4])), encoding="utf-8")
            output = fixture / "atlas.png"
            report = normalizer.normalize(source, output, manifest_path)
            self.assertTrue(report["source_pixels_partitioned_exactly_once"])
            self.assertTrue(report["source_rgba_preserved"])
            self.assertTrue(report["fixed_grid_guard"]["guard_passed"])
            self.assertEqual(len(report["frame_records"]), 12)
            self.assertEqual(source.read_bytes(), source_bytes)
            self.assertEqual((fixture/"generated-sheet.png").read_bytes(), source_bytes)
            for record in report["frame_records"]:
                self.assertEqual(record["source_rgba_sha256"], record["normalized_rgba_sha256"])
            manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
            self.assertEqual(manifest["frame_layout"], "fixed-grid")
            self.assertEqual(manifest["frame_gutter"], 24)
            self.assertEqual(manifest["intended_airborne_frames"], [4])
            self.assertEqual(manifest["atlas_sha256"], builder.digest(output))
            self.assertEqual(manifest["generated_source_sha256"], builder.digest(source))
            self.assertEqual(builder.assemble(fixture), (fixture_id, "assembled"))
            self.assertEqual(builder.assemble(fixture), (fixture_id, "cached"))
            # A matching atlas cache must not conceal a replaced generated source.
            (fixture/"generated-sheet.png").write_bytes(source_bytes+b"changed")
            with self.assertRaises(AssertionError):
                builder.assemble(fixture)
        finally:
            for temporary in [fixture, work]:
                if temporary.is_dir() and temporary.resolve().is_relative_to(root.resolve()):
                    shutil.rmtree(temporary)

    def test_shared_partition_rejects_solid_artwork_on_boundaries(self):
        with self.assertRaisesRegex(AssertionError, "Solid artwork touches a shared source partition"):
            normalizer.shared_partition(Image.new("RGBA", (288, 216), (50, 100, 150, 255)))

    def test_clear_two_pixel_gap_wins_over_opaque_five_pixel_score(self):
        # A heavy effect ends at row39; a small next pose starts at row42.
        # The old wider score chooses42, which cuts that pose. The actual
        # two-pixel gap is rows40/41 and must be split at41 without erasing it.
        image = Image.new("RGBA", (20, 72), (50, 100, 150, 255))
        for y, opaque_count in [(40, 0), (41, 0), (42, 1), (43, 2), (44, 2)]:
            for x in range(opaque_count, image.width):
                image.putpixel((x, y), (0, 0, 0, 0))
        before = image.tobytes()
        self.assertEqual(builder.transparent_cuts(image, "y", 2), [0, 42, 72])
        self.assertEqual(normalizer.constrained_transparent_cuts(image, "y", 2), [0, 41, 72])
        self.assertEqual(image.tobytes(), before)

    def test_per_frame_ground_alignment_removes_foot_variation(self):
        positions = [70, 74, 66, 72, 73, 68, 75, 71, 67, 76, 70, 72]
        image, boxes, frames = self.foot_fixture(positions)
        before = image.tobytes()
        row = normalizer.ground_alignment(frames, boxes, image.size, {})
        aligned = normalizer.ground_alignment(frames, boxes, image.size, dict(ground_alignment="per-frame"))
        row_baselines = [foot+relative[1] for foot, relative in zip(row["feet"], row["relative"])]
        final_baselines = [foot+relative[1] for foot, relative in zip(aligned["feet"], aligned["relative"])]
        self.assertGreater(len(set(row_baselines)), 1)
        self.assertEqual(len(set(final_baselines)), 1)
        self.assertEqual(final_baselines, [aligned["ground_reference_y"]]*12)
        self.assertEqual(image.tobytes(), before)

    def test_per_frame_alignment_preserves_airborne_and_ignored_motion(self):
        positions = [70, 74, 42, 72, 70, 93, 71, 73, 70, 89, 74, 72]
        image, boxes, frames = self.foot_fixture(positions)
        common = dict(intended_airborne_frames=[2], alignment_ignore_frames=[5], actor_absent_frames=[9],
                      foot_anchors={"2": [245, 42]})
        row = normalizer.ground_alignment(frames, boxes, image.size, common)
        aligned = normalizer.ground_alignment(frames, boxes, image.size, dict(common, ground_alignment="per-frame"))
        for index in [2, 5, 9]:
            self.assertEqual(aligned["relative"][index], row["relative"][index])
            self.assertTrue(aligned["measurements"][index]["exclusions"])
            self.assertNotIn(index, aligned["grounded_frames"])
        jump_height = aligned["ground_reference_y"]-(aligned["feet"][2]+aligned["relative"][2][1])
        self.assertEqual(jump_height, row["row_ground"][0]-positions[2])
        self.assertGreater(jump_height, 0)
        baselines = [aligned["feet"][index]+aligned["relative"][index][1] for index in aligned["grounded_frames"]]
        self.assertEqual(len(set(baselines)), 1)

    def test_manifest_foot_anchor_overrides_lower_weapon_or_effect(self):
        positions = [70, 74, 66, 72, 73, 68, 75, 71, 67, 76, 70, 72]
        image, boxes, frames = self.foot_fixture(positions, effect_frames=[0])
        self.assertEqual(builder.foot_y(frames[0]), 90)
        aligned = normalizer.ground_alignment(frames, boxes, image.size,
                                             dict(ground_alignment="per-frame", foot_anchors={"0": [45, 70]}))
        self.assertEqual(aligned["feet"][0], 70)
        self.assertEqual(aligned["measurements"][0]["method"], "manifest")
        self.assertEqual(aligned["feet"][0]+aligned["relative"][0][1], aligned["ground_reference_y"])
        with self.assertRaisesRegex(AssertionError, "Foot anchor lies outside its source frame"):
            normalizer.ground_alignment(frames, boxes, image.size, dict(foot_anchors={"0": [145, 70]}))

    def test_per_frame_alignment_requires_measured_grounded_feet(self):
        image, boxes, frames = self.foot_fixture([42]*12)
        with self.assertRaisesRegex(AssertionError, "Cannot align unmeasured grounded feet"):
            normalizer.ground_alignment(frames, boxes, image.size, dict(ground_alignment="per-frame"))

    def test_normalized_ground_alignment_survives_preview_assembly(self):
        root = builder.ROOT / "Temp"
        root.mkdir(exist_ok=True)
        fixture = Path(tempfile.mkdtemp(prefix="ultimate-ground-test-", dir=root))
        fixture_id = "__ground_alignment_fixture_"+fixture.name.rsplit("-", 1)[-1]
        work = root / "UltimateAssembly" / fixture_id
        try:
            positions = [70, 74, 66, 72, 73, 68, 75, 71, 67, 76, 70, 72]
            # Low dark effects on a complete row would fool a second heuristic
            # alignment after the actual feet have already been anchored.
            raw, boxes, _ = self.foot_fixture(positions, effect_frames=[4, 5, 6, 7])
            source = fixture/"raw.png"
            raw.save(source)
            source_bytes = source.read_bytes()
            anchors = {str(index): [index%4*100+45, index//4*100+foot] for index, foot in enumerate(positions)}
            manifest_path = fixture/"manifest.json"
            manifest_path.write_text(json.dumps(dict(id=fixture_id, ground_alignment="per-frame", foot_anchors=anchors)), encoding="utf-8")
            report = normalizer.normalize(source, fixture/"atlas.png", manifest_path)
            self.assertTrue(report["grounded_feet_aligned"])
            self.assertEqual(len(set(report["grounded_output_foot_y"].values())), 1)
            self.assertEqual([record["method"] for record in report["foot_measurements"]], ["manifest"]*12)
            self.assertTrue(report["source_rgba_preserved"])
            self.assertTrue(report["fixed_grid_guard"]["guard_passed"])
            self.assertEqual(source.read_bytes(), source_bytes)
            self.assertEqual((fixture/"generated-sheet.png").read_bytes(), source_bytes)
            self.assertEqual(builder.assemble(fixture), (fixture_id, "assembled"))
            validation = json.loads((fixture/"validation.json").read_text(encoding="utf-8"))
            self.assertTrue(validation["normalization_ground_alignment_preserved"])
            self.assertEqual(validation["row_ground_corrections"], [0, 0, 0])
            self.assertEqual(validation["offsets"], [[32, 32]]*12)
            self.assertGreater(len(set(validation["row_ground"])), 1, "Fixture must expose the misleading lower effect")
            self.assertEqual(builder.assemble(fixture), (fixture_id, "cached"))
        finally:
            for temporary in [fixture, work]:
                if temporary.is_dir() and temporary.resolve().is_relative_to(root.resolve()):
                    shutil.rmtree(temporary)

    def test_airborne_lift_targets_exact_ground_reference(self):
        image, boxes, frames = self.foot_fixture([70, 74, 42, 48, 73, 68, 75, 71, 67, 76, 70, 72])
        common = dict(ground_alignment="per-frame", intended_airborne_frames=[2, 3, 4],
                      foot_anchors={"2": [245, 42], "3": [345, 48]})
        unchanged = normalizer.ground_alignment(frames, boxes, image.size, common)
        lifted = normalizer.ground_alignment(frames, boxes, image.size,
                                            dict(common, airborne_lift={"2": 35, "3": 60}))
        for index, distance in [(2, 35), (3, 60)]:
            self.assertEqual(lifted["feet"][index]+lifted["relative"][index][1], lifted["ground_reference_y"]-distance)
        self.assertEqual(lifted["relative"][4], unchanged["relative"][4], "Unspecified airborne motion must remain unchanged")
        self.assertEqual(lifted["grounded_frames"], unchanged["grounded_frames"])
        zero = normalizer.ground_alignment(frames, boxes, image.size, dict(common, airborne_lift={"2": 0}))
        self.assertEqual(zero["feet"][2]+zero["relative"][2][1], zero["ground_reference_y"])

    def test_airborne_lift_rejects_invalid_or_unmeasured_motion(self):
        image, boxes, frames = self.foot_fixture([70]*12)
        common = dict(ground_alignment="per-frame", intended_airborne_frames=[2])
        for invalid in [-1, 1.5, True]:
            with self.subTest(height=invalid), self.assertRaisesRegex(AssertionError, "nonnegative integer"):
                normalizer.ground_alignment(frames, boxes, image.size, dict(common, airborne_lift={"2": invalid}))
        with self.assertRaisesRegex(AssertionError, "explicitly airborne"):
            normalizer.ground_alignment(frames, boxes, image.size, dict(common, airborne_lift={"1": 30}))
        with self.assertRaisesRegex(AssertionError, "per-frame alignment"):
            normalizer.ground_alignment(frames, boxes, image.size, dict(intended_airborne_frames=[2], airborne_lift={"2": 30}))
        image, boxes, frames = self.foot_fixture([70, 70, 42]+[70]*9)
        with self.assertRaisesRegex(AssertionError, "detected foot or explicit foot_anchors"):
            normalizer.ground_alignment(frames, boxes, image.size, dict(common, airborne_lift={"2": 30}))

    def test_airborne_lift_preserves_rgba_through_normalize_and_preview(self):
        root = builder.ROOT/"Temp"
        root.mkdir(exist_ok=True)
        fixture = Path(tempfile.mkdtemp(prefix="ultimate-airborne-test-", dir=root))
        fixture_id = "__airborne_lift_fixture_"+fixture.name.rsplit("-", 1)[-1]
        work = root/"UltimateAssembly"/fixture_id
        try:
            raw, _, _ = self.foot_fixture([70, 74, 42, 48, 73, 68, 75, 71, 67, 76, 70, 72])
            source = fixture/"raw.png"
            raw.save(source)
            raw_bytes = source.read_bytes()
            manifest_path = fixture/"manifest.json"
            manifest_path.write_text(json.dumps(dict(id=fixture_id, ground_alignment="per-frame",
                                                     intended_airborne_frames=[2, 3],
                                                     foot_anchors={"2": [245, 42], "3": [345, 48]},
                                                     airborne_lift={"2": 35, "3": 60})), encoding="utf-8")
            report = normalizer.normalize(source, fixture/"atlas.png", manifest_path)
            self.assertTrue(report["airborne_lift_verified"])
            self.assertTrue(report["source_rgba_preserved"])
            self.assertTrue(report["fixed_grid_guard"]["guard_passed"])
            self.assertEqual(source.read_bytes(), raw_bytes)
            self.assertEqual((fixture/"generated-sheet.png").read_bytes(), raw_bytes)
            with Image.open(fixture/"atlas.png") as opened:
                normalized = opened.convert("RGBA")
            for record in report["frame_records"]:
                self.assertEqual(raw.crop(record["source_box"]).tobytes(), normalized.crop(record["normalized_copy_box"]).tobytes())
            for index, distance in [(2, 35), (3, 60)]:
                proof = report["airborne_lift_proof"][index]
                self.assertEqual(proof["source_nominal_foot_y"]+proof["translation_y"], report["ground_reference_y"]-distance)
                self.assertEqual(proof["normalized_cell_foot_y"], proof["normalized_ground_reference_y"]-distance)
            self.assertEqual(builder.assemble(fixture), (fixture_id, "assembled"))
            validation = json.loads((fixture/"validation.json").read_text(encoding="utf-8"))
            self.assertTrue(validation["normalization_airborne_lift_preserved"])
            self.assertEqual(validation["row_ground_corrections"], [0, 0, 0])
            self.assertEqual(builder.assemble(fixture), (fixture_id, "cached"))
            stale = json.loads(manifest_path.read_text(encoding="utf-8"))
            stale["airborne_lift"]["2"] = 36
            manifest_path.write_text(json.dumps(stale), encoding="utf-8")
            with self.assertRaisesRegex(AssertionError, "Normalized airborne lifts differ"):
                builder.assemble(fixture)
        finally:
            for temporary in [fixture, work]:
                if temporary.is_dir() and temporary.resolve().is_relative_to(root.resolve()):
                    shutil.rmtree(temporary)


if __name__ == "__main__":
    unittest.main(verbosity=2)
