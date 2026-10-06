# Scene review checklist

Updated 2026-10-06. This is the persistent list for subsequent scene work.

Status: floor shadow repair is UNRESOLVED. Saleh rejected the completion claim after commit cbab4f3. Captures and a clean console establish technical execution, not visual correctness.

## Before claiming completion

- [ ] Inspect the actual current Unity player view, not only generated source art or old captures.
- [ ] Compare the specific reported fault before and after the change at the same view and lighting hour.
- [ ] Inspect close views of affected joins and the whole composition.
- [ ] Sweep the lighting slider through transitions, not only four endpoints.
- [ ] Check normal view, left view, and camera movement/downward view for regressions.
- [ ] Record evidence and failures below; leave failed or unchecked criteria open.
- [ ] Say precisely what was checked and what remains unresolved. Do not equate compile success with a visual fix.

## Current priority: hall floor shadows

- [ ] Identify the remaining visible fault in the current scene; compare with the rejected repair.
- [ ] Shadows contact the correct portal base and railing/column feet; no detached shapes.
- [ ] Shadow shapes and direction agree with actual light sources and the painted floor perspective.
- [ ] No strips, blobs, hard rectangular cutouts or duplicate shadows across tiles.
- [ ] Shadows do not cross doors, stairs, glass or furniture incorrectly.
- [ ] Shadow movement and softness remain credible throughout morning/noon/evening blends.
- [ ] Night shadows agree with illuminated fixtures; daylight casts disappear appropriately.
- [ ] No floor-extension seam or shadow jump during camera movement.
- [ ] Preserve the accepted traveller/AVAILABLE sign/mat alignment, left elevator, stairs and right doors.

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
