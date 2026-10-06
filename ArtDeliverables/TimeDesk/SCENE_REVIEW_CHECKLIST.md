# Scene review checklist

Updated 2026-10-06. This is the persistent list for subsequent scene work.

Status: geometric floor-shadow revision implemented and reviewed in the live-camera captures described below. Earlier footprint repairs were rejected. Remaining local-fixture and whole-hall perspective items stay open.

## Before claiming completion

- [x] Inspect the actual current Unity player view, not only generated source art or old captures.
- [x] Compare the specific reported fault before and after the change at the same view and lighting hour.
- [x] Inspect close views of affected joins and the whole composition.
- [x] Sweep the lighting slider through transitions, not only four endpoints.
- [x] Check normal view, left view, and camera movement/downward view for regressions.
- [x] Record evidence and failures below; leave failed or unchecked criteria open.
- [x] Say precisely what was checked and what remains unresolved. Do not equate compile success with a visual fix.

## Current priority: hall floor shadows

- [x] Identify the reported stair/column fault: cast footprint was below the painted column foot; compare the same player view before/after.
- [x] Shadows contact the correct portal base and railing/column feet; no detached shapes.
- [x] Shadow shapes and direction agree with actual light sources and the painted floor perspective.
- [x] No strips, blobs, hard rectangular cutouts or duplicate shadows across tiles.
- [x] Shadows do not cross doors, stairs, glass or furniture incorrectly.
- [x] Shadow movement and softness remain credible throughout morning/noon/evening blends.
- [ ] Night shadows agree with illuminated fixtures; daylight casts disappear appropriately.
- [ ] No floor-extension seam or shadow jump during camera movement.
- [x] Preserve the accepted traveller/AVAILABLE sign/mat alignment, left elevator, stairs and right doors.

Technical evidence from cbab4f3: Unity reported zero errors at the final check; eight captures were produced; mask R/G/A glazing channels were checked unchanged. These checks do not close any visual criterion above.

## Outstanding scene notes retained from the conversation

- [ ] Hall painted perspective and floor tile continuity fully resolved.
- [ ] Light sources match fixtures, including upper right; no floating light columns.
- [ ] Desk object shadows follow time of day correctly.
- [ ] Character poses are complete character art, with proper arm anatomy and hair/accessory ordering.
- [ ] City panorama connects across three unique sections without tiling or stretching; correct high-floor perspective.
- [ ] City has distinct districts, megatowers and landmarks; less water, grounded retro cyberpunk anime identity.
- [ ] City animations: smoke, clouds, distant ships, flying/ground traffic and small building activity.
- [ ] Remaining city lighting versions after morning.

## Review log

2026-10-06: cbab4f3 changed hall floor masks and casts. Assistant claimed the issue was fixed; user rejected that claim. Visual status reopened. Next step is diagnosis against the current player view, with this checklist kept updated.

2026-10-06 follow-up: traced actual column foot from native source detail (701,481)-(784,445), corrected stair/pier receiver silhouette and added bin exclusion. Inspected same-view column-before.png / column-after.png; close-up checks at 08,10,12,14,16.5,19,22; whole-scene front/noon and left/evening reviewed. The displaced patch is reduced and cast starts at the observed base. Continuous movement and broader shadow correctness remain open; do not mark the whole task complete. Unity errors=0; no scene or lighting-config file diff.

Geometric revision: replaced footprint sweeps with upright column/pedestal/post volumes and hollow portal rings on source-calibrated floor. Floor shader receives exact shared desk sunlight ray. Half-hour actual slider contact sheets inspected; pan and desk-pose captures inspected; night daylight cast fades to zero; final Unity errors=0. No painted scene/camera/config modifications. Approximate reconstructed geometry, not a full hall survey. Night local-fixture contact shading and the pre-existing foreground floor appearance remain separate open criteria.
