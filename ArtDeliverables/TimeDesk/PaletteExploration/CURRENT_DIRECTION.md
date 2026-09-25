# Current art direction — 2026-09-25
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
- Follow Saleh's latest full ReStory desk screenshot: smooth warm-brown board surfaces, subtle seams, barely visible grain, clean matte stylisation, distinct blue/mustard/pale-paper/charcoal/coral object colours. No dense photo-like wood grain. Keep a plain office pad; no cutting grid.

## Latest proposal
`ScreenStudies/Restory/01-restory-material-direction.png` is the current single SCREEN PAINT-OVER proposal. Its exact prompt and study notes are alongside it. It is not a Unity capture and is not approved yet. Its lamp geometry drifted toward the reference; preserve the existing model unless separately changed. Keep the real portal ring coherent metal.

The first ten colour boards, the ten multicolour revisions, the high-fidelity material-render trial, standalone stylised palette data and earlier green-containing screen paint-over are superseded research. They are preserved so the work is not lost; never treat them as active guidance.

## Applied in Unity
Only the flag conversion has been applied in this pass: four SpriteRenderers using `Assets/Art/Office/HallFlags/HallFlag_Ochre.asset`, old cloth children disabled, existing metal supports retained, original positions and visible dimensions preserved. Changes saved in OfficeScene. Verified in Play mode with four active sprites and zero console errors. See `ArtDeliverables/TimeDesk/HallFlags/validation.json` and its runtime screenshot.

The palette, PC colour, wood simplification and other prop finishes are still PROPOSALS, not applied runtime changes. No ten newly approved directions have been delivered. Do not resume character generation or claim the office colour task is closed merely because one revised paint-over exists.

