# Current state — anime desk and background crowds

The user accepted the anime desk direction ("Perfect") and requested layered background crowd groups. `OfficeHallCrowds` now adds 19 groups using six anonymous merged illustrations, with morning/evening palettes. The user asked for less personal detail, accepted the merged upper-body direction, then required visible legs; the final atlas restores simple legs and feet. They found the groups too small against the room, so the final placement is 30% larger. See `../HallCrowds/README.md` for assets, reproduction, final captures and checks. The previous empty-hall constraint is superseded by this request.

## Rules for the art side (2026-09-26)

Saleh had Claude clean up the art-side code on 2026-09-26 (review: `docs/reviews/2026-09-26-chatgpt-code-triage.md`). From now on:

- **Don't edit gameplay code or gameplay assets; ask for a hook.** Gameplay code is everything under `Assets/Scripts` and `Assets/Editor` except the art's own components (`OfficeHallCrowdPalette`, `OfficeTrafficVehicle`, `OfficeWindowGlass`) and `Assets/Editor/OfficeArt`. Gameplay assets are `Assets/Scenes/OfficeGameplay.unity`, `Assets/Art/Office/Gameplay`, `Assets/Data` and what the TimeDesk builders write. When the art needs something from the game, ask the gameplay side for a read-only hook; `docs/SCENE_CONTRACT_GAMEPLAY.md` lists the hooks and the anchors. One case is still open: *Debt Relief > Save Scanner Finishes* recolours the gameplay placeholder's `Placeholder_Scanner*.mat` (triage B9). Deliver an art scanner marked `Anchor_Scanner` instead.
- **Commit sources, not debris.** Don't commit Unity or Blender logs, `Assets/_Recovery` scenes, backup copies of scenes, or one-off audit/inspect scripts. Keep Blender scripts path-relative: they find the repo from their own location.
- **The crowd palette reads the shift-clock hook.** `OfficeHallCrowdPalette` follows `ShiftClockDriver.Live` (`IShiftProgress`, the gameplay shift clock): morning until half the shift, fully evening from 90%. It needs no DayOrchestrator in the art scene. Leave *Hall Crowds > Follow Shift* selected.
- **The glass material owns the window alpha.** `Hall_ClearGlass` (set by the palette pass) decides how clear the windows are. `OfficeWindowGlass`'s transparency slider applies only while its *Override Material Alpha* toggle is on (off by default).
- **The character pilot is processed by Claude.** The art side generates and reviews the raw sheets (`../Characters/Raw`, with prompts and notes) and stops there. Claude does the registration, cut-out, colour variants, keys, import into `Assets/Art/Characters` and the in-game checks (`docs/CHARACTER_ART_CONTRACT.md`).
- Don't recreate the removed rebuilders (below). After an intentional change outside the art passes (for example deleting the leftover gameplay objects the scene contract lists), run *Debt Relief > Refresh Baseline Hashes* once. Keep the scene-contract names: `ImportedOfficeDress/Desk/Retro CRT`, `Clerk hotline`, `Desk calculator`, `Pen pot`, `Forms stapler`, `HybridOffice/Booth/Finish_Mat`, `Blender_Next`, the readout texts, `Main Camera`, `Cameras/OfficeVCam`.

Removed on 2026-09-26, because a rerun reverted the approved layout, palette or crowd clearance, or wrote the leftover gameplay objects: *Build Desk Props* and *Check Paint Shader* (ImportedOfficeBuilder), *Apply Clean Desktop*, *Apply Desk Anime Revision* and *Audit/Validate Clean Desktop* (OfficeDeskClean), *Build Hall Crowd Groups*, *Apply Blender CRT Study*, *Validate Desk Anime Revision*, *Review Imported Packs*, *Audit Surfaces*, the OfficePaint shader (its `*_Painted.mat` now name URP Lit) and 20 one-off or superseded Python scripts. The current tools are *Debt Relief* (palette, layout, validation, capture, Refresh Baseline Hashes), *Hall Crowds* (previews, Validate), *Install/Validate Hall Flag Sprites*, *Apply Desk Anime Shading* (the CRT2 presets) and *Apply Rebuilt CRT Study* (only on request: the PC is protected).

## Desk shader checkpoint

The user rejected the previous phone and said the objects still lacked anime shading. A new custom shader, `NOPE/Desk Anime`, now replaces the PBR lighting on the desk and CRT. This is the current direction; the completion claims below describe the earlier checkpoint and are superseded by this revision.

The curved pack phone replaces the primitive reconstruction, has clean materials and is separated from the till. The mouse cable reaches the computer; floppy disks are removed. Spare forms are the first two-triangle 2D prop trial, rendered from our Blender assets and lit with the same shader. The original room, floor, desk, lighting, boards and exterior remain preserved.

See `DeskClean/ANIME_SHADER.md`, `DeskClean/anime_validation.json`, and the desk-only Game view `../DeskFinish/Iterations/65_anime_desk.png`. The shader is reusable for other game props; it is currently installed only on the desktop. The user accepted continuing this direction; do not claim exact ReStory parity.

## Previous PBR checkpoint (historical)

25 September 2026, branch `art`.

The user accepted the softer, cleaner direction, stopped requesting before/after sheets, and asked to finish every desk object, check relative size and placement, and close the desk task. They specifically identified the phone and pen cup as too small.

## Result

The CRT and 19 other desktop placements now use the clean Blender art pass. The phone and pen cup are enlarged and proportioned against the CRT/keyboard. Keyboard, mouse, calculator, lamp, pen pot, stapler, output tray/forms, spare forms, keys, binder, till, file sorter, ink pad, stamp, NEXT sign, data disks and office blotter have all received individual geometry/material treatment or a clean revision of editable source.

The final Game-view capture is `../DeskFinish/Iterations/60_desk_complete.png`. No more comparison sheets are needed. Full scope, sources, size/layout decisions and validation are in `DeskClean/COMPLETION.md`.

## Preservation and checks

The restored pre-pack room, original floor, ceiling, desk, boards, lighting, portal and exterior remain preserved. No runtime gameplay code changed. The buried duplicate newspaper underneath the output tray is inactive.

All replacement renderers are installed; the superseded renderers are disabled; materials are valid; every prop's bounds remain inside the desktop. Camera rays hit the CRT and NEXT targets. CRT, Back, NEXT, Back events still switch correctly. Unity console and URP Lit report zero errors. No full-day gameplay test was performed.

The scene is saved in Edit mode. `DeskClean/scene_scope_audit.json` lists all 36 existing scene documents changed from `23aa6e1`; all are desktop props or the CRT click/focus anchors. No original scene document was removed.

## Editable source and reproduction

- CRT: `BlenderCRT/CRT_Rebuilt_Study.blend`, authored by `BlenderCRT/rebuild_crt.py`.
- Other desktop props: `DeskClean/DeskClean.blend`, authored by `DeskClean/author_desk_clean.py`.
- Style: `RESTORY_STYLE_GUIDE.md`; screenshot analysis: `RESTORY_REFERENCE_ANALYSIS.md`.
- Unity: `Tools/Office Art/Apply Rebuilt CRT Study` for the CRT (only on request). Re-exported `Clean_*.fbx` update through import; their finishes and positions come from `Tools/Office Art/Debt Relief` (`../PaletteExploration/Applied/README.md`). `Apply Clean Desktop` and `Build Desk Props` were removed on 2026-09-26 (see the rules above).

The first ivory CRT study and its comparisons are historical. The user has approved continuing the direction across the desk. They have not claimed exact visual parity with ReStory, and neither should future handovers.

Earlier remote push approval was rejected and the destination/payload approval question was not answered. Work is saved locally; do not claim it was pushed.
