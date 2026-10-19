# Nia black iris override — 2026-10-19

The user explicitly requested changing only Nia iris colors to near-black. Built-in image_gen supplied a palette guide; reviewed original iris coordinates were applied with Node.js sharp, preserving every alpha byte and every RGBA pixel outside the recorded masks. Existing white reflections, sclera, lashes, expression and anatomy are preserved.

`manifest.json` records approved runtime hashes, generated-source hashes, prior metadata records, reproduction patches and bounded restoration windows. Original file bytes are retained under `Build/NiaEyesVerification/originals/`. Official artwork, previous QA captures and prior release packages remain unchanged.

Run `node Tools/ApplyNiaEyeOverrides.mjs --check` to validate current hashes, or pass `--target` followed by one exact recorded runtime path after its historical mechanical preparation. The target mode requires unchanged dimensions, alpha and every pixel outside the recorded eye restoration windows, validates all updated source hashes, then restores approved eye-window pixels. It never adopts a changed outfit, pose, crop or other subject.

The preparation scripts retain their existing historical crop/padding workflow; this explicitly authorized local override is applied by Node.js after that workflow. It does not use Python to perform the new iris edits.

Later on 2026-10-19, the user additionally requested removing only the eye shine from prologue page 2. The `nia_overload.png` entry now includes that follow-up: the two existing eye-glint clusters plus their dark anti-alias rims (395 pixels) are filled using the surrounding black-iris colors, with all alpha and all other RGBA pixels retained. The final mask adds 72 dark edge pixels to the first 323-pixel core mask; both proofs are preserved. The first override's preserved-reflections description above refers to the earlier change; the later authorization applies only to this pose. See `Build/WindowedPrologueVerification/prologue/` for the exact mask, guide, prompt, immutable originals and verification.
