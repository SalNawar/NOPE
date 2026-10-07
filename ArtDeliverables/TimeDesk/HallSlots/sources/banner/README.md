# First culture-banner design sources

Scope: three flat front-view designs, Egypt/Greece/Japan, under Claude's mailbox entry #5 (2026-10-07). Each design is reused for BOTH 13-flag-left-cloth and 14-flag-right-cloth. These are raw design sources, not the six registered runtime overlays.

Files: egypt.png, greece.png, japan.png; each 724x2172 RGB/RGBA PNG (portrait1:3). Plain magenta margins surround the straight rectangular print design. Emblems sit in the upper third; Egypt uses winged sun and lotus/papyrus borders, Greece a laurel wreath and meander borders, Japan16-petal chrysanthemum and seigaiha borders. No words, numbers, hanging hardware, hall, perspective, folds or painted cast lighting.

Generated using built-in imagegen; prompts.json records the original and targeted corrections. Saving from the tool's returned PNG bytes did not alter image pixels. No Python or .NET image editing was performed; .NET only inspected dimensions, colours and hashes.

## Verified on sources

- All three PNGs decode and have exact1:3 aspect.
- Visually inspected emblems, upper-third placement, border motifs, straight print outlines, absence of text/hardware/folds and full surrounding magenta background.
- Field samples excluding ornaments are approximately uniform, with only1-3RGB-value variation; minor generative pixel noise remains, not a cloth/lighting pattern.
- Corner samples are consistently near magenta and suitable for the existing keyer's tolerance. Final margins were enlarged after finding that earlier tiny/full-bleed margins were unsafe for corner-based detection.
- Earlier full-canvas and narrow-margin attempts are excluded from this branch.

## Claude processing and final verification still required

Please key the backdrop, trim to print content, normalize generated palette approximations to the canonical swatches below if needed, fit each design to BOTH exact WarmStone cloth masks using tools/hall/fit_slot_art.py, and retain the painting's own outlines, folds and edge shading. The generated PNGs approximate the requested colours; they are NOT claimed to have exact hex values. Any palette correction belongs in the processing stage, before applying the painting's brightness shading.

Canonical swatches: Egypt field#1C4E80/decoration#C8962E; Greece field#0D5EAF/accent#1F6F8B with ivory/white; Japan field#1F2F4A/decoration#B23A1A. Be especially careful that the palette remains restrained after fitting and night tint.

Write only the processed overlays to Assets/Art/UI/Resources/Hall/Slots/<slot>/<nation>.png, exact2172x724 RGBA. Verify neutral/culture toggle, pan, night tint, no old-red fringe, occlusion/window exclusion, and Capture Combinations; inspect full hall composites. The fitter's margin handling must be reviewed against the actual cloth masks and template boxes. None of these final-overlay or Unity checks is claimed complete by this source delivery.

SHA256:
- egypt.png:75B07A974050F39BF33568ADA1F180164B8ED8BFECD489A78965440238C70067
- greece.png:49E9877924081C34E7C79E75B21DCF70442F713E41005F31972F917CC8F4560C
- japan.png:6C81E0A75C2F64F9D855BCE552CE8853719BF5ACB33CC209E0C2431993A83518

This branch contains only the approved three-nation hall source pilot. Character production, its paused-task request and the disputed large roster are outside this delivery.
