# Chibi sprite and skill icon integration

The game's art manifest is exported from the initialized `GameDatabase` by `Tools/ExportArtManifest.ps1`. It contains 92 sprites (91 subjects and Hana), 378 card icons (subject skills, basic cards, weapon skills and tactical skills), and 91 passive icons. The manifest is UTF-8 without a BOM; readers should explicitly select UTF-8 on Windows.

Dedicated runtime textures are loaded from these paths:

- `Assets/Resources/Lumia/Portraits/{characterId}.png`
- `Assets/Resources/Lumia/SkillIcons/{cardId}.png`
- `Assets/Resources/Lumia/PassiveIcons/{passiveId}.png`

`PixelArt.Portrait` prefers a character's dedicated PNG before using older atlases. `SkillIcon` and `PassiveIcon` use their dedicated assets. Generic development placeholders remain available while assets are being authored; they do not satisfy the art coverage checks. `HasDedicatedPortrait`, `HasSkillIcon` and `HasPassiveIcon` check the real Resources paths directly.

Card faces show a large skill icon with a smaller chibi sprite for the skill's owner. The expanded card also shows both images. Enemy plans show their skill icons in execution order, with each icon opening the existing card detail panel. The combat effect banner shows the icon of the skill being used. Starting passive choices, passive replacement and the passive inventory show a dedicated passive icon alongside the character sprite.

`LumiaPixelImporter` applies Point sampling, no mipmaps, uncompressed pixels, no non-power-of-two resizing, clamped wrapping, input alpha and alpha transparency. Dedicated assets are not CPU readable; only the legacy atlases remain readable for their existing crop code. The build setup applies the same settings to previously imported images. These changes do not modify card rules, saves or run state.

A development player launched with `-lumia-art-verify` loads every required resource and writes `ArtVerification/coverage.txt`. It creates native Unity screenshots of all 92 sprites and all 469 icons, using eight sprite pages and twelve icon pages. Missing dedicated resources are logged as errors and result in exit code 1. The verification uses a separate save path, does not create or alter a run, and is absent from normal gameplay.

Validation completed for the integration source:

- Runtime C# compilation, including card faces, enemy plan icons, effect banners and the opt-in art verification component.
- Editor C# compilation against Unity 6000.3.11f1 modules, including texture postprocessing and build import settings.
- Manifest integrity: zero U+FFFD replacement characters; every subject skill and passive owner resolves to a sprite name.

Final art validation completed after all generated assets were installed:

- 561 PNGs from 36 generated atlases passed coverage, dimensions, transparency, boundary, source provenance and pixel uniqueness checks, with zero missing assets, errors or warnings.
- The native development player loaded 92 dedicated sprites and 469 dedicated icons with zero missing resources. Eight sprite pages and twelve icon pages were visually inspected; both paired-character sprites preserve both bodies and their props. Results are in `docs/art-chibi/native/`.
- Seventeen gameplay screenshots confirmed the new sprites and icons in card selection, combat, rewards, detail panels, encounters and the catalog, including player and enemy effect banners. No runtime exceptions were recorded. Screenshots are in `docs/screenshots/`.
- Windows development and release builds both completed with `LUMIA BUILD Succeeded / errors=0`.
