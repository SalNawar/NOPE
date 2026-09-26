# Hall sprite layers — displayed art and left-side lockers

2026-09-26. Current direction: a neglected government departure hall with deliberate institutional decoration, smaller side windows, belongings lockers on the left, and MEDBAY / JAIL / C-SUITES wayfinding. The user requested a mockup based on the actual game screen before Unity changes.

## Current screen mockup

The exact built-in image-generation prompt is saved in `displayed-art-lockers-screen.prompt.md`. Its inspected edit reference is the actual Unity capture at `../PaletteExploration/Applied/LayoutWear/runtime.png`.

The user-requested retry succeeded on 2026-09-26 using the built-in image generator and the saved prompt. The revised image is `displayed-art-lockers-screen.png`.

![Displayed art, left lockers and hall wayfinding](displayed-art-lockers-screen.png)

The desk, PC, existing desk-object arrangement, corkboards, original floor, central teleporter and central rear glazing are protected composition references. This generated composition preview is not a screenshot of installed hall changes or a finished transparent sprite set. Use actual scene geometry for final placement rather than treating generated pixel measurements as authoritative.

## Three depth layers

1. **Back:** more solid side wall, fewer/smaller side windows, deliberately hung framed paintings and directional signs. Famous artworks are quiet Easter eggs in a neglected official collection: dusty glass, faded colour, chipped frames and failed picture lights. They must never be scattered on furniture or the floor.
2. **Middle:** numbered belongings lockers on the left, fixed waiting seats on the right. Subtle repairs, a forgotten coat/bag and one sealed locker communicate use without explanatory story text. Materials retain distinct, plausible colours.
3. **Near:** sparse queue posts around the sides, preserving the original floor and a generous clear central approach to the portal.

Furnish first, then place fewer merged anonymous crowd groups around the furniture. The prompt targets five small groups rather than the current nineteen. The image is a layout proposal; it does not precisely resolve all five groups and includes a lone distant silhouette. For production, use merged group assets, preserve full legs, correct human scale and indistinct internal shapes. Check locker height against people in world units. Neither people nor furniture should obscure the portal.

## Superseded concepts

- `three-layer-screen-mockup.png`: rejected side service counters/booths.
- `oppressive-hall-easter-eggs-mockup-v1.png` and the earlier Easter-egg prompts: rejected side booths and improperly scattered/propped artwork.

Keep these as historical explorations only. They are not implementation references. Do not add side booths or return to scattered art.

## Coordination and state

This checkpoint changes only HallLayers deliverables. It does not edit OfficeScene, gameplay, character assets, existing art authoring code, shaders, materials or Blender scripts. The existing in-game crowd count remains nineteen until a later scene application.

Claude is cleaning up the art code and has installed 42 character game layers. Do not process the raw pilot again. Avoid the deprecated rebuild menus and the files listed by the user in the conversation. The user authorized continuing independent hall art while Claude handles the cleanup; any later scene work must respect the existing gameplay hooks and preserve approved art.
