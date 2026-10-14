# Built-in image generation prompt record

Mode: built-in `image_gen.imagegen`, `transparent_background: true` for all seven calls. Every final PNG was copied byte-for-byte. No raster postprocessing. Supporting local reference images were inspected before use.

## Wolf — initial generation

References: `ArtSource/Release1.0.2/Wildlife/official-wolf-camp.png`, `Assets/Resources/Lumia/CharacterAtlas.png`, `Assets/Resources/Lumia/PortraitsReference/nia.png`.

> Use case: style-transfer. Asset: a single transparent-background pixel-art wildlife combat sprite for Black The Survival. Create ONE Eternal Return WOLF, not the bears or any people. References: image 1 official wolf camp screenshot supplies animal identity and restraint/collar design ONLY; image 2 CharacterAtlas supplies crisp chunky pixel outlines and shading style; image 3 Nia supplies LARGE visible square pixel size. Depict a compact alert gray-black wolf on all four paws facing LEFT in side-three-quarter view, angular pointed muzzle, small amber eyes, pointed ears, shaggy charcoal back, medium gray flanks and pale gray lower jaw/chest, long bushy dark tail. Preserve the distinctive Eternal Return dark research restraint collar with muted orange/yellow small fastening/strap detail visible near the neck from official model; do not invent a saddle, helmet or robot body. FULL BODY, no amputated paws or tail. Readable chibi game proportions while still looking like a wolf. Construct as an approximately 80 by 80 logical-pixel sprite with uniform large square pixel clusters: very crisp stepped dark outline, restrained 4-level fur shading, no thin hair strokes, no smooth gradients, no antialiasing. Output a square RGBA canvas, subject occupies about 82 percent width and 70 percent height with all paws on consistent baseline. Actual transparent alpha, no background, shadow, scenery, labels, text, extra creatures or UI.

Output: `exec-a93f4f85-e67d-4127-91f4-80b24aa44a51.png` (intermediate; retained in the generator directory only).

## Boar — initial generation

References: `ArtSource/Release1.0.2/Wildlife/official-boar-charge.png`, `Assets/Resources/Lumia/CharacterAtlas.png`, `Assets/Resources/Lumia/PortraitsReference/nia.png`.

> Use case: style-transfer. Asset: ONE transparent pixel-art BOAR wildlife combat sprite for Black The Survival / Eternal Return fan game. Input 1 is official original-game boar-charge screenshot, use boar identity only. Input 2 CharacterAtlas is the game's existing chunky dark outline and fur-shading style. Input 3 Nia defines LARGE square pixel clusters. Redraw the boar as a compact stocky wild boar facing LEFT in side three-quarter view, dark warm brown body with black-brown shaggy bristled ridge, small sharp ears and tiny amber eyes, heavy wedge-shaped snout, two large curved ivory tusks, short stout legs with dark split hooves. Include the Eternal Return distinctive DARK RESEARCH RESTRAINT HARNESS with YELLOW rectangular caution-like bands above shoulders and small dark/steel fittings as seen on the official boar, scaled to readable pixels. No saddle, cape, weapon, background or person. Full body and all feet visible, idle grounded stance ready to charge. Strong stepped dark outline and 4-level blocky fur shading/highlights. Uniform coarse pixel art using approximately 80 by 80 logical pixels, crisp big square clusters, no smoothing, no fine hairs or antialiasing. Square RGBA canvas with subject about 82 percent wide and 65 percent tall, paws on one baseline, visible transparent margin on all sides. Real transparent alpha with no floor shadow, scenery, labels or text.

Output: `exec-2f8359e7-6903-4100-a729-611b6da421f4.png` (intermediate).

## Bear — initial generation

References: `ArtSource/Release1.0.2/Wildlife/official-bear-camp.png`, `Assets/Resources/Lumia/CharacterAtlas.png`, `Assets/Resources/Lumia/PortraitsReference/nia.png`.

> Use case: style-transfer. Asset: ONE Eternal Return BEAR transparent-background chunky pixel-art wildlife combat sprite for Black The Survival. Reference 1 shows ORIGINAL GAME BEARS at a camp, not boars: broad brown bodies, rounded bear ears and small muzzle, heavy dark research restraint straps with yellow bands over shoulders. Translate that silhouette/design to game pixel art. Reference 2 CharacterAtlas gives the game's crisp dark stepped outlines, earthy palette and readable wildlife style. Reference 3 Nia gives LARGE visible square pixel size. Draw a single compact heavy dark-brown bear on all FOUR paws, facing LEFT in side-three-quarter combat idle pose. Massive shoulders, broad low head, short rounded ears, lighter tan muzzle and small dark nose, small amber eyes, short thick legs with a few ivory claw pixels. Add recognizable research collar and restraint straps in charcoal with three small muted-yellow rectangular panels across the shoulder harness, dark metal fasteners, retaining the animal silhouette. All paws, claws and short tail must fit inside margins. Friendly-stylized but dangerous wildlife, no human proportions or anthropomorphic clothes. Approximately 80 by 80 logical-pixel sprite with large crisp square pixel clusters, thick dark stepped silhouette, four-tone block fur shading and clearly separated brow/muzzle. Square RGBA output, animal occupies about 80 percent width and 68 percent height. Real transparent alpha, no floor shadow, no background, no other subjects, text, label, UI or watermark. No smooth gradients, thin hair texture, painterly strokes, antialiasing or blur.

Output: `exec-2ebfcb49-1bb4-4c80-9407-fa64e18b3bde.png` (intermediate).

## Chicken — final generation

References: `ArtSource/Release1.0.2/Wildlife/official-alpha-animal-1.png`, `Assets/Resources/Lumia/CharacterAtlas.png`, `Assets/Resources/Lumia/PortraitsReference/nia.png`.

> Use case: style-transfer. Asset: ONE Eternal Return CHICKEN / ROOSTER transparent-background pixel-art wildlife combat sprite for Black The Survival. Reference 1 is ORIGINAL NIMBLE NEURON concept sheet for the game's chicken and defines the design: GOLDEN TAN / WARM ORANGE breast and neck, bright RED upright comb and red dangling wattle, YELLOW beak and feet, distinct TEAL TURQUOISE wing feathers and curved dark charcoal/teal tail, not a plain white hen. Match those actual colors and silhouette. Reference 2 CharacterAtlas defines crisp dark stepped pixel outline and shading of existing wildlife. Reference 3 Nia defines LARGE readable square pixel clusters. Draw one compact plump rooster standing upright facing LEFT in three-quarter side view, one round dark-brown alert eye with a tiny highlight, neatly folded teal wing, gracefully upward curved teal/dark tail, two sturdy yellow feet visible and grounded on same baseline. Simple chibi game proportions, readable head and expressive animal silhouette. Approximately 80 by 80 logical-pixel construction, uniform big square pixel clusters, 4-level hard feather-shading, thick dark pixel outline, no smooth curves or gradients, no thin lines, no antialiasing. Square RGBA canvas with chicken about 70 percent wide and 80 percent high, comfortable transparent margin. Genuine transparent background; no scenery, floor shadow, text, concept sheet, labels, UI, people or additional animals. Do not add armor or accessories absent from the original concept.

Final: `exec-dad65081-5e7c-41d6-8c6a-04e27cc328a4.png` → `Assets/Resources/Lumia/Wildlife/chicken.png`.

## Wolf — final identity detail edit

References: initial wolf output above (edit target), `ArtSource/Release1.0.2/Wildlife/official-alpha-wolf.jpg` (identity reference).

> Use case: precise-object-edit. Edit target image 1 ONLY. Image 2 is official original Eternal Return wolf concept artwork, used ONLY for missing restraint details. Keep image 1's exact chunky square pixel art, composition, wolf pose, body shape, expression, entire fur shading, amber eye, canvas and transparent alpha. Make small identity corrections to match image 2: add dark chunky research cuffs around the lower legs, with a pair of silver buckle pixel highlights; on the front cuffs add short pointed dark metal claw extensions extending only a little forward along the paw. Give the neck collar two small yellow horizontal strips with a tiny red accent and a small dangling yellow rectangular ID tag. Add one visible ivory saber canine at the mouth if readable at this pixel scale. No text on the tag. Avoid turning the wolf into a robot or changing pose. Keep the existing gray/ivory fur pattern and long dark tail. Do not add extra backdrop or floor shadow. Uniform large stepped pixel clusters, zero smoothing, zero antialiasing, genuine transparent background.

Final: `exec-ff824883-d12b-47e0-a29e-45a71a63a2ee.png` → `Assets/Resources/Lumia/Wildlife/wolf.png`.

## Boar — final identity detail edit

References: initial boar output above (edit target), `ArtSource/Release1.0.2/Wildlife/official-alpha-animal-3.png` (identity reference).

> Use case: precise-object-edit. Edit target image 1 ONLY. Image 2 is the original official Eternal Return boar concept sheet and supplies ONLY missing fur markings and harness identification. Preserve image 1's pose, exact chibi proportions, face expression, large crisp square pixel art, chunky outline, canvas framing, all paws, tusks, entire brown body shading and transparent alpha. Correct only these visual identity details from image 2: give the boar a CREAM / IVORY vertical forehead stripe leading onto the upper snout; cream jagged cheek fringe around lower cheeks and neck; a narrow cream broken jagged ridge along the dark back mane; a few cream toe patches while keeping dark hooves. On the existing dark shoulder restraint retain yellow panels and add one tiny red accent and a small dangling yellow rectangular ID tag without lettering. Do not repaint the body white; keep warm brown fur dominant. Keep image 1's current motion and silhouette unchanged. No scenery, background, shadow, label, UI or other animal. Zero smoothing, zero gradients, zero antialiasing. Real transparent background.

Final: `exec-c974b7b2-b760-4d2a-8cd7-1e90b9a8d0e6.png` → `Assets/Resources/Lumia/Wildlife/boar.png`.

## Bear — final identity detail edit

References: initial bear output above (edit target), `ArtSource/Release1.0.2/Wildlife/official-alpha-bear.png` (identity reference).

> Use case: precise-object-edit. Edit target image 1 ONLY. Reference image 2 is the official Eternal Return original bear model, used only for missing restraint details. Keep image 1's exact chunky big-square pixel art, bear pose on all four paws, full silhouette, dark brown fur shading, face, small amber eye, neutral-dangerous expression, transparent canvas and composition unchanged. Add just the original model's two most recognizable features: chunky charcoal/black restraint cuffs around the front wrists with small silver metal bolt pixels and one GOLD / YELLOW open ring hanging from each visible outer wrist; three yellow caution bands over the existing shoulder collar with a tiny red accent. Retain brown fur as dominant color, tan muzzle, ivory claws and large stepped dark outlines. No anthropomorphic costume, robot parts, saddle, scenery, floor shadow, labels, text or extra animals. Uniform large crisp square pixel clusters, no smooth gradients, no thin hair strokes, no antialiasing. Genuine transparent alpha.

Final: `exec-7944d4c9-f000-490a-be8e-7fe29249d670.png` → `Assets/Resources/Lumia/Wildlife/bear.png`.
