# Current state — desk task completed

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
- Unity: `Apply Rebuilt CRT Study`, then `Apply Clean Desktop`, in `Tools/Office Art`.
- `Build Desk Props` restores the vendor placement scaffold. If intentionally using it, apply both finished-art menus afterward; do not leave the scaffold as the final scene.

The first ivory CRT study and its comparisons are historical. The user has approved continuing the direction across the desk. They have not claimed exact visual parity with ReStory, and neither should future handovers.

Earlier remote push approval was rejected and the destination/payload approval question was not answered. Work is saved locally; do not claim it was pushed.
