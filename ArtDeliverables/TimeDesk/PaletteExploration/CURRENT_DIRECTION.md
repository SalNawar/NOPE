# Current art direction — 2026-09-26
This file supersedes earlier palette boards and material studies in this folder.

## Active constraints
- Character generation is explicitly PAUSED until this office art-direction task is resolved.
- Show proposed colour changes ON THE GAME SCREEN, preserving its stylised rendering. Neither standalone swatch charts nor realistic/high-fidelity room mockups answer the request.
- No green, teal, jade, olive, sage or mint on the walls, separators, pads, lamp, till, NEXT sign, keyboard, flags, portal or other listed props. Do not just replace that green with one shared grey/blue hue.
- Each object needs a believable material and individual finish, balanced in the whole composition. Controls can use functional colour groups.
- Buildings retain several independent plausible brick/stone/concrete colours. Glass should read as glass through transmission and simple reflections.
- Keep the restored original floor and scene geometry.
- Flags are actual 2D sprites in game, not modelled cloth.
- The PC must not be the depressing yellowed beige treatment.
- Follow Saleh's latest full ReStory desk screenshot: smooth warm-brown board surfaces, subtle seams, barely visible grain and clean matte stylisation. No dense photo-like wood grain. Keep a plain office pad; no cutting grid.
- Latest correction: too many blues. Use the twenty material-assigned base colours in `Palette20/palette-20.json`. Blue is reserved for portal energy and tiny signals; the office pad is burgundy, phone coral, till aubergine, tray lavender, cup mustard, lamp charcoal and PC neutral porcelain. Do not apply a global colour cast.

## Latest proposal
`Palette20/screen-02.png` is the latest SCREEN PAINT-OVER proposal, accompanied by twenty exact sRGB swatches in `Palette20/palette-20.svg`, JSON material assignments, and the generation prompt. It is not a Unity capture and is not approved yet. The previous `ScreenStudies/Restory/01-restory-material-direction.png` was rejected for repeated blue. Preserve existing game geometry and the original floor; paint-over geometry is illustrative. Keep the portal ring coherent metal.

Mood assessment: the current palette reads warm, nostalgic and welcoming with a sci-fi focal point. Its dystopian character is still weak. Proposed mood target, not yet approved: a comfortable personal desk inside an imposing, impersonal institution. Develop that tension through lighting, architectural scale and crowd repetition, rather than dulling every object colour. Character generation remains paused.

The first ten colour boards, the ten multicolour revisions, the high-fidelity material-render trial, standalone stylised palette data and earlier green-containing screen paint-over are superseded research. They are preserved so the work is not lost; never treat them as active guidance.

## Applied in Unity
Only the flag conversion has been applied in this pass: four SpriteRenderers using `Assets/Art/Office/HallFlags/HallFlag_Ochre.asset`, old cloth children disabled, existing metal supports retained, original positions and visible dimensions preserved. Changes saved in OfficeScene. Verified in Play mode with four active sprites and zero console errors. See `ArtDeliverables/TimeDesk/HallFlags/validation.json` and its runtime screenshot.

The palette, PC colour, wood simplification and other prop finishes are still PROPOSALS, not applied runtime changes. No ten newly approved directions have been delivered. Do not resume character generation or claim the office colour task is closed merely because one revised paint-over exists.
