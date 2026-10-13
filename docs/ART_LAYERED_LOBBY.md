# Layered lobby artwork — v0.1.27

The lobby now has two independent image assets. The laboratory keeps the composition of the v0.1.26 lobby, and Nia is a new dedicated character sprite. The gameplay portrait is a style and identity reference; it is not copied into the lobby as the final image.

| Layer | Asset | Raw image | SHA-256 |
|---|---|---|---|
| Background | `Assets/Resources/Lumia/Lobby/Background.png` | 1672 × 941, RGB, opaque | `9373489e3cdd7ed87a151e918981800e5d6445b530bba699d01fd9d5e5210496` |
| Nia | `Assets/Resources/Lumia/Lobby/Nia.png` | 1254 × 1254, RGBA, transparent | `d524b438a310b6c0d17f5f5473b6d77a4d9ca8891e401d557ca1b485f0016d8b` |

Both assets were generated with the built-in `image_gen.imagegen` tool. The selected generated PNG files were copied byte for byte into the project. No cropping, resizing, palette conversion, alpha editing, or other image postprocessing was applied. Unity imports use Point filtering, no mipmaps, no compression, no non-power-of-two scaling, and a 4096 size limit.

## Composition and pose

The backdrop preserves the dark left-side menu space, laboratory windows and railings, cyan specimen cylinder, right-side scientific monitors, desks, pink drink can, rabbit decorations, floor reflections, and cyan/magenta palette. Only Nia was removed. Her empty pink-and-black gaming chair remains in the background, with the previously occluded cushions and seat reconstructed.

The dedicated sprite preserves the original knees-up gaming posture: Nia leans back, pulls both knees up, grips a pink controller with both hands, and holds both black/pink open-toe sandals together in front. Her headset, checked sleeves, lavender hood, shorts, and pink gum bubble remain. The new rendering uses the game's coarse chibi pixel clusters, large head, restrained half-lidded magenta eyes, and compact face. The chair belongs exclusively to the background layer.

The sprite has faint alpha dust outside its silhouette. Its significant alpha bounds at alpha ≥ 64 are **left 314, top 171, right 991, bottom 1178** in the 1254 × 1254 raw source; right and bottom are exclusive. The regular nonzero-alpha bounds must not be used as the visible character rectangle. Rendering geometry may crop the UVs to significant bounds and use Point sampling without changing the source asset.

Initial composition reference in the 1672 × 941 background coordinates: place the visible Nia rectangle near **x 1015, y 176, width 375, height 558**. This sets her sandal baseline near y 734 and her hips on the right side of the seat around y 660. At 1280 × 720 this is approximately **x 777, y 135, width 287, height 427**. The reconstructed seat occupies approximately x 1015–1370 and y 620–735. These are integration starting anchors, not a constraint against adjusting the final native screen after visual inspection.

## Exact prompts and provenance

- [Background prompt](art-lobby-refresh/background-prompt.txt)
- [Nia prompt](art-lobby-refresh/nia-prompt.txt)
- [Machine-readable asset manifest](art-lobby-refresh/manifest.json)

The manifest records the complete prompts, input reference paths and SHA-256 hashes, raw generated output paths, final paths and SHA-256 hashes, dimensions, image modes, and alpha counts/bounds. The old combined lobby file is not edited by this artwork task; archiving and runtime layer integration are handled by the main implementation.

## Runtime integration

The former combined lobby is archived at `ArtSource/Lobby/v0.1.26/Lobby.png`, outside Unity Assets. `PixelArt.Background()` loads the character-free layer. `LumiaLobbyArt.cs` separately renders Nia through the shared 80×80 Point-sampled pixel layer renderer, using alpha≥64 crop metadata and an integer display scale. UI text and the clickable dialogue remain independent. Final native placement is recorded in `docs/validation-art-refresh/`.
