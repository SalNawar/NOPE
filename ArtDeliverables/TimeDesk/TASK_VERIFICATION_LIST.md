# Task verification list

Applies to every task in this project, per Saleh's instruction on 2026-10-06.

For each task, keep a written list of requirements, implementation status, checks and remaining failures. Implemented, verified and complete are separate states. Inspect actual results before claiming completion; technical success alone cannot close a visual requirement. Reopen a rejected result and continue the work. Keep user-facing updates brief and substantive.

Current task: hall-floor shadow geometry. Implemented upright volume/ring ray casts, exact shared desk sunlight, floor-only receiver and continuous light motion. Verified same-view details, half-hour slider sheets, pan and desk camera pose samples. Night local fixture contact shading and overall painted perspective remain open in [scene checklist](SCENE_REVIEW_CHECKLIST.md).

White silhouette crowd task (2026-10-06): approved original style reused; nine complementary compositions, scattered fixed placements, lower opacity, independent fades and shared hall-hour palette. Scene captures and live Play inspected; detailed requirements/checks in [crowd checklist](HallLayers/WhiteCrowds/CHECKLIST.md).


2026-10-07 — Crowd placement review reopened after user reported wall-standing groups. Expanded from 15 to 25 placements, moved door-side groups into clear floor lanes, preserved stationary fades and light tint, added foreground rail occlusion. Reviewed all groups together in front/left views instead of relying on fading samples. Evidence and status: HallLayers/WhiteCrowds/CHECKLIST.md.

2026-10-07 — Station-life follow-up: generated two 4×4 silhouette sheets (32 activity compositions), broader station population than the user examples alone. Installed 37 fixed groups, 15 on bridge and five in the distant hall, with stronger staggered background occupancy. Reviewed all placements simultaneously; corrected neighboring atlas-cell artifact. Evidence: WhiteCrowds/StationLifeReview-2026-10-07-Final.

2026-10-07 — Density clarification: user meant specifically behind the two rear portals, with reduced density around the front portal. Redistributed to 24 rear groups / 15 bridge / 4 middle / 2 near. Added source-registered rear-hoop and glass occlusion. All-group front/left and night inspection recorded in RearPortalPopulationReview-2026-10-07.

2026-10-07 — Desk prop reactions reopened after cashier appeared to animate only its highlight. Confirmed single-part bindings in the flattened copied desk. Restored complete prop anchors and attached readouts, retaining art poses and authored reaction tuning. Evidence and verification: [desk animation checklist](DeskAnimationRepair/CHECKLIST.md).
