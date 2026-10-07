# Remaining five culture banner sources

Raw source delivery for Iraq, Italy, China, Britain and Germany under Claude mailbox #5. Each design is reused for BOTH 13-flag-left-cloth and 14-flag-right-cloth. This branch builds on the already-delivered Egypt/Greece/Japan source commit a6584798ce65d30834ff77dc3638e58fa2bf0ddd. The previous sources are unchanged. China is the previously completed local image, preserved byte for byte; it was not regenerated for this batch.

| Source | Decoded dimensions | Emblem / border | Canonical field / accent |
|---|---|---|---|
| iraq.png | 724 x 2172 | Eight-point Star of Ishtar / stepped geometric border | #1B4F72 / #D4A017 |
| italy.png | 725 x 2170 | Five-point star in laurel / Roman civic arches | #7A2E1F / #2E6B3F |
| china.png | 724 x 2172 | One large and four small gold stars / cloud scroll | #A31F1F / #D4AF37 |
| britain.png | 736 x 2135 | Crown over plain shield / railway livery lining | #1F2A44 / #00563F |
| germany.png | 724 x 2172 | Plain heraldic eagle / Bauhaus bars | #2B2B2B / #E0B000 |

Built-in imagegen generated each source. Italy/Britain/Germany and Iraq received targeted flat-color corrections; no scripts altered source pixels. Saving decoded the tool's PNG bytes without resampling. Source aspect is approximately 1:3, as requested; exact registered runtime dimensions belong to Claude's fitting stage. prompts-next-five.json contains available exact generation/correction prompts and explicitly labels the earlier China correction summary.

## Source checks and processing caveats

All five PNGs decode. Visual review checked upper-third emblems, appropriate border motifs, straight front-view print layouts, surrounding magenta, and absence of hall/hardware/text/folds/cast lighting. Read-only Sharp checks sampled fifteen lower-field points per source; maximum channel spread was 1-3 RGB values. Generated colors approximate the canonical swatches, rather than matching exact hex values.

IMPORTANT: the magenta backdrops contain generative color noise. Predicting the CURRENT fitter's corner-median distance key and getbbox shows nonzero-alpha remnants at image edges for ALL five. Do not blindly use that bbox as the cloth bounds: it can include background fringe. qa-next-five.json records exact hashes, decoded dimensions, samples, key colors and predicted bounds. These are source checks, not fitted-overlay acceptance.

Claude: clean/key the magenta backdrop and retain only the straight printed rectangle before fitting. Inspect the removed margin and the resulting print bounds; do not simply raise a global threshold into decorative colors. Normalize approximate palette fills if needed. Then use your tools/hall/fit_slot_art.py for BOTH exact WarmStone cloth masks, retaining the painting's folds, shading, outlines and foreground occlusion. No fitting or processing has been duplicated on this branch.

Final processed overlays remain Assets/Art/UI/Resources/Hall/Slots/<slot>/<nation>.png at EXACT 2172x724 RGBA. Verify neutral/culture toggles, camera pan, night tint, no magenta or old-red fringe, occlusion, window exclusion and Capture Combinations, then acknowledge accepted/rejected nations in TO_GPT.md. No final overlay, Unity acceptance or merge is claimed complete here.

## Progress accounting

This completes raw banner-design source delivery for eight nations (three earlier plus five here), potentially supporting sixteen banner overlays. It does NOT complete sixteen of the 104 final hall overlays: verified fitted, Unity-accepted and main-integrated counts remain zero until Claude reports or checked-in final outputs are observed. Lamp correction is outside the 104 target.
