# Eight culture departure-board header sources

Raw source batch for slot15-departure-board-frame under HALL_SLOTS_ART_REQUEST.md on main50831a42ba8a49706033e955c79f1a7f465eab77. Exact final slot:226x40 at canvas950,36 on2172x724; sorting order60. These are isolated source designs, NOT registered runtime overlays. They add no runtime assets or code and do not regenerate the completed banners.

| Source | PNG canvas | Emblem / motif |
|---|---|---|
| egypt.png |2098x749 RGBA |Winged sun; lotus/papyrus |
| iraq.png |2172x724 RGB |Eight-point Ishtar star; stepped lions-gate motif |
| greece.png |2106x747 RGB |Laurel; Greek meander |
| italy.png |2033x773 RGB |Star in laurel; Roman civic arches |
| china.png |2172x724 RGBA |One large and four small stars; cloud scroll |
| japan.png |2005x784 RGBA |Sixteen-petal chrysanthemum; seigaiha |
| britain.png |2112x745 RGBA |Crown over plain shield; railway lining |
| germany.png |2146x733 RGBA |Plain civic heraldic eagle; geometric bars |

Canonical field/accent: Egypt#1C4E80/#C8962E; Iraq#1B4F72/#D4A017; Greece#0D5EAF/#1F6F8B; Italy#7A2E1F/#2E6B3F; China#A31F1F/#D4AF37; Japan#1F2F4A/#B23A1A; Britain#1F2A44/#00563F; Germany#2B2B2B/#E0B000. Generated fills approximate these colors. Palette normalization belongs in processing.

Built-in imagegen produced all sources. Saving decoded the returned PNG bytes without changing pixels. prompts.json records generation and targeted opacity corrections. Earlier Iraq/Greece drafts contained substantial holes/transparency in their colored face; Italy had some partial-alpha face pixels. Those drafts are excluded. Their corrected opaque images use magenta outside. The other five retain genuine alpha outside the printed plate.

## Source acceptance evidence

All eight decode and match the requested culture emblem/motif with a centered landscape print layout, no words/numbers/hall/screen/rods, and no cloth folds or cast-light painting. Source review accepts them as raw designs for processing, not as final overlays. qa.json records SHA256, dimensions, background kind, analysis bounds, interior opacity and color samples. Interior opacity checks pass for all eight: alpha minima252-255, no pixels below250 in the measured interior. No script edited raster pixels.

The printed rectangles are wide and shallow, but vary in aspect about4.78-6.83 rather than exactly5.65. Full canvases include removable outside margins. The source preview preserves original aspect, crops to measured plate bounds with10px margin, and labels each unchanged source. It is not a hall composite, in-game image, or final exact-size simulation.

## Claude processing required

1. The current tools/hall/fit_slot_art.py supports ONLY the two banner slots. Extend/settle board-plate fitting before installing these sources. The canonical plate content/position is settled; a specific flat-source fitting contract has not yet been acknowledged in mailbox.
2. Clean the outside margin/halo and measure the actual straight printed rectangle. Transparent sources contain faint low-alpha pixels beyond the face; raw alpha>0 getbbox is therefore unsafe. RGB sources need magenta keying. Preserve antialiasing on the real outline, fully cover the new opaque plate face and do not retain halo or magenta fringe. No deterministic pixel cleanup was performed here.
3. Normalize palette approximations and final plate proportions while keeping central emblem readable and appropriately proportioned. Use the226x40 box at950,36 in the current WarmStone template, preserving both hanging rods and the board display below. Do not just stretch the whole source canvas or include the removed outside margin.
4. Write EXACT2172x724 RGBA registered overlays only to Assets/Art/UI/Resources/Hall/Slots/15-departure-board-frame/<nation>.png. Keep artwork inside the slot; preserve foreground/window exclusion. Because this is in the ceiling band, inspect brightest neutral pixels for accidental night glow.
5. Verify neutral/culture toggle, camera pan, night tint, rod occlusion, clean edges, board screen visibility and Capture Combinations in Unity. Report accepted/rejected variants and merged hash in TO_GPT.md.

coordination-and-qa-limits.md records the mailbox and live Unity investigation. No duplicate waiting nudge or external message was sent. Lamp full-scene acceptance remains pending: live Unity is the older d2f65d5 checkout, not published lamp branch698e266f.

## Counts

Delivered source designs:8 banners from earlier batches plus8 board plates here =16. Potential final coverage:16 banner overlays +8 board overlays =24 of104. That is prospective source coverage, not final completion. Verified fitted/Unity-accepted/main-integrated overlay counts remain0 until processing/acceptance is observed. Lamp correction stays outside104.
