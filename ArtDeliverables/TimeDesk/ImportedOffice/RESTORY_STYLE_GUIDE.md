# ReStory-inspired office props — working style guide

Reference: `../DeskFinish/ReStory_current_reference.png`.
This is an interpretation of the supplied screenshot, not an official ReStory production guide.

## Visual target

Readable, illustrated hardware with solid three-dimensional form. Broad warm colours, quiet surfaces, soft manufactured edges and deliberate dark recesses. The object should read clearly at normal gameplay distance before its small details become visible.

## Shape

- Preserve each object's function, silhouette, proportions and familiar retro construction.
- Round manufactured edges enough to catch a narrow, soft highlight. Use real bevels and controlled normals; do not inflate the whole object.
- Keep large planes calm. Vents, controls and panel seams provide the detail hierarchy.
- Preserve pivots, desk contact, screen position and interaction anchors during the first pass.

## Palette

| Surface | Target colour family | Finish |
| --- | --- | --- |
| Aged ABS computer casing | Warm ivory `#D7C7A7`, restrained ochre variation | Satin, broad subdued highlight |
| Secondary plastic panels | Warm grey `#A59B88` | Slightly rougher than the casing |
| Recesses and rubber | Charcoal brown `#343530` | Matte; readable even in shadow |
| Painted metal | Deep petrol green `#28564E` | Enamel, tighter highlight than plastic |
| Small hardware accents | Muted brass `#A68A59`, amber `#BA7947` | Limited use on controls and fittings |
| Paper | Warm off-white `#DED4B9` | Matte, little normal detail |

Palette values are guides, not a blanket colour multiply over every material.

## Texture

- Preserve UV islands and functional markings. No blurry replacement of vents, buttons or drive slots.
- Use broad, low-contrast colour variation. Remove cloudy grey dirt that obscures form.
- Wear belongs at handled corners and edges. Avoid uniform scratches, exaggerated chipped paint and photoreal grain.
- Keep colour/albedo free of fixed sunlight, cast shadows and specular streaks.
- Do not bake game UI, changing text, logos, dates or screen content into textures.

## Material and light response

- Plastic: metallic 0, roughness approximately .62–.76; very weak micro-normal response.
- Enamel: metallic 0, roughness approximately .40–.58; colour comes from paint, not bare metal.
- Rubber/recesses: metallic 0, roughness approximately .85–.95.
- Blank CRT glass: dark neutral blue-green, roughness approximately .30–.45. A restrained reflection, no image painted onto the screen.
- Prefer contact shadows and real local lighting over baked dark borders or indiscriminate ambient occlusion.
- The first comparison must use identical camera, exposure, lights and object placement. A lighting change cannot stand in for an asset improvement.

## First object: CRT computer

Source: `Assets/80s_Office/Models/Electronics/SM_Monitor.FBX`, as used by the desk's `Retro CRT` prefab.

1. Bring the actual pack mesh into Blender and keep its UVs.
2. Give the monitor bezel and lower case a coherent warm ivory plastic treatment; retain dark drive bays, vent openings and screen gasket.
3. Add restrained manufactured edge rounding where geometry needs it, with clean normals.
4. Keep screen geometry and physical screen position unchanged. The game still supplies the focused desktop.
5. Export a separate project-owned FBX and materials; leave vendor assets untouched.
6. Compare original and revised assets under the same light, both in Blender and in the actual Unity desk view.

## Acceptance

- The first read is warm ivory computer, dark blank glass, clear recessed controls.
- Silhouette and screen remain aligned; no mirrored, floating or stretched parts.
- Fine noise is reduced without erasing useful detail.
- Highlights describe bevels without making the case look wet or metallic.
- Only the computer changes in the Unity comparison. Restored floor, ceiling, room, lighting and other desk props remain unchanged.
- Show the real before/after. Do not label the pass an exact ReStory match.
