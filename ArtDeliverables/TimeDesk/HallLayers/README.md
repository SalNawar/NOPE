# Hall sprite layers — displayed art and left-side lockers

2026-09-26. Current direction: a neglected government departure hall with deliberate institutional decoration, smaller side windows, belongings lockers on the left, and MEDBAY / JAIL / C-SUITES wayfinding. The user requested a mockup based on the actual game screen before Unity changes.

## Current screen mockup — generation blocked

The exact built-in image-generation prompt is saved in `displayed-art-lockers-screen.prompt.md`. Its inspected edit reference is the actual Unity capture at `../PaletteExploration/Applied/LayoutWear/runtime.png`.

The built-in image generator returned HTTP 401 / invalid_api_key on 2026-09-26. No revised image was produced, so `displayed-art-lockers-screen.png` does not exist. Do not present the superseded images as this revised mockup. Retry the saved prompt with the same reference when the image service is available.

The desk, PC, existing desk-object arrangement, corkboards, original floor, central teleporter and central rear glazing are protected composition references. The planned output is a visual proposal, not a screenshot of installed hall changes or a finished transparent sprite set.

## Three depth layers

1. **Back:** more solid side wall, fewer/smaller side windows, deliberately hung framed paintings and directional signs. Famous artworks are quiet Easter eggs in a neglected official collection: dusty glass, faded colour, chipped frames and failed picture lights. They must never be scattered on furniture or the floor.
2. **Middle:** numbered belongings lockers on the left, fixed waiting seats on the right. Subtle repairs, a forgotten coat/bag and one sealed locker communicate use without explanatory story text. Materials retain distinct, plausible colours.
3. **Near:** sparse queue posts around the sides, preserving the original floor and a generous clear central approach to the portal.

Furnish first, then place fewer merged anonymous crowd groups around the furniture. The mockup targets five small groups rather than the current nineteen. Preserve full legs, correct human scale and indistinct internal shapes. Neither people nor furniture should obscure the portal.

## Superseded concepts

- `three-layer-screen-mockup.png`: rejected side service counters/booths.
- `oppressive-hall-easter-eggs-mockup-v1.png` and the earlier Easter-egg prompts: rejected side booths and improperly scattered/propped artwork.

Keep these as historical explorations only. They are not implementation references. Do not add side booths or return to scattered art.

## Coordination and state

This checkpoint changes only HallLayers deliverables. It does not edit OfficeScene, gameplay, character assets, existing art authoring code, shaders, materials or Blender scripts. The existing in-game crowd count remains nineteen until a later scene application.

Claude is cleaning up the art code and has installed 42 character game layers. Do not process the raw pilot again. Avoid the deprecated rebuild menus and the files listed by the user in the conversation. The user authorized continuing independent hall art while Claude handles the cleanup; any later scene work must respect the existing gameplay hooks and preserve approved art.
