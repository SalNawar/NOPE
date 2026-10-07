# Shared-arm feasibility pilot — not production accepted

Scope: existing male and female New Kingdom Egypt silhouettes, Thebes c.1470 BCE. No new roster, eras or historical premades. No Unity/runtime library files changed.

`prototype.cjs` builds a self-contained interactive `comparison.html`, plus actual assembled `comparison.svg` and cropped `seams.svg`. Run it with Node from any working directory. `node verify.cjs` checks layer contracts; it does not check visual seams.

The images are actual reusable layers, not concept renders: existing RGBA body regions are runtime-clipped into torso, upper arm, forearm and hand regions; the same outfit and torso references remain fixed for every pose. SVG joint transforms swap instantly between neutral, explaining_a, thinking_a, thinking_b and objecting_a. This is articulated reuse of neutral arm pixels, not finished separately authored arm poses. Shoulder/forearm overlap uses 12 px joint overlap. Both linen outfits are sleeveless, so sleeve compatibility is untested.

Thinking hand regions render after head/hair/accessory, matching the mailbox's final Hands order. These prototype masks are not runtime `hands_{g}_skin{N}__{pose}` deliverables. Unknown poses fall back as an entire set to neutral. There is no tweening.

Complexion selects the project's existing baked body/head PNGs (1,3,5 in the preview). The actual Unity renderer's SetTint tints every layer for room light; it does not selectively recolour hair or skin. Hair colours are baked by tools/characters; Egyptian tripartite wigs remain fixed-colour. The pilot does not implement facial expressions, moustache/glasses/hat toggles or runtime integration, and does not claim them complete.

## QA and blockers

- Existing art and raw prompt references inspected. Existing body/outfit alpha shows broad brown/green edge contamination at close scale; retained unchanged for honest proof.
- Node structural checks pass. All input references exist and no original art bytes are changed.
- Browser/desktop surfaces are absent in this delegated environment, so rendered sheet and seam quality have NOT been visually tested. Parent must inspect before showing as an accepted result.
- Neutral hand reuse can produce poor raised-hand anatomy. Thinking_a/b transform targets, shoulder caps and elbow joints require visual review and likely authored arm replacements.
- One built-in imagegen thinking-arm candidate was inspected and rejected: shoulder/hand placement did not match contract and it added a glow. It is not included in the proof or installed.
- Deterministic raster layer extraction/registration and QA compositing were requested explicitly; approval has not arrived. No Python pixel editing or alternate pixel-processing workflow was used.

This is a reviewable feasibility batch. It must not be announced as a finished visually passed Egypt art batch or counted against the unapproved 1,153-image plan.
