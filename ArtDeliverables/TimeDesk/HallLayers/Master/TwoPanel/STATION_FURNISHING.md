# Station furnishing checkpoint — 2026-09-27

Final checkpoint in the old rendering style: `right-hall-v11-clear-arrow.png`. The user rejected accumulated image degradation and requested a complete fresh 1990s 2D anime redraw. This folder preserves the prior composition and its edits; do not keep retouching this rendering chain as the final master.

This is an art-only painted study before layer extraction. The existing Unity desk and all gameplay remain untouched. The full left-pan extension is retained in `panorama-preview.html`. City, sky and all exterior scenery count as ONE future exterior layer. The visible city remains a placeholder; it has not been regenerated or extracted in this pass.

## Direction incorporated

- Grounded retro-futurist station equipment: CRT service consoles upstairs, compact portal service modules, a worn vending machine, public communication handset, modular oxblood waiting seats, waste sorting bin and noticeboard.
- Upper-right platform remains open and deeper; lower corridor entrances and museum displays remain.
- Both freestanding lower-right ticket kiosks and their mat were removed because they blocked a corridor entrance. Keep every doorway approach clear in subsequent passes.
- Rear-left glazing junction was repainted with a continuous corner frame through bridge level. This is a painted architectural revision, not a surveyed 3D reconstruction.
- Rear ground-floor portal pair is set farther back at a shared depth. Front and upstairs portals remain the intended composition anchors.
- Clock above the elevator removed; pier and conduits restored.
- Security requested: five controlled portal bays with transparent partitions, closed access gates and card readers. Gate leaves must become independently movable sprites during extraction; barriers cannot block public circulation or corridor doors.

## Source sequence

- v3: open upper platform architecture.
- v4: furnishings, museum displays, safety/electrical equipment and wear.
- v5: station equipment study; superseded because kiosks blocked a doorway.
- v6: clear entrances and revise glazing junction.
- v7: set rear portals back and remove clock.
- v8: transparent security bays and closed access gates around all five portals.
- v9: close three department doors and add matching pictogram headers, replacing the misleading adjacent exit badges.
- v10: attempted upper-right exit pictogram repair; close-up review still showed two malformed figures. Superseded.
- v11: replace that plaque with one bold right arrow. Final reference layout for the fresh redraw.

Originals are preserved. Built-in imagegen prompts are in `station-furnishing-prompts.json` and `station-security-signage-prompts.json`, with the final arrow pass in `station-clear-arrow-prompt.txt`. v7 was 2171 x 724; v11 is 2172 x 724, matching the preview hall slot. These generative revisions are not pixel-identical preservation of the original drawing.

## Production constraints

Composition comes before extraction. Maintain registration and reconstruct surfaces behind extracted objects. Exterior is one replaceable layer; window glass and frames belong to separate hall layers. Add a security-equipment layer group: fixed posts/partitions, movable gate leaves, card readers and their runtime indicators must be separable. Existing twenty-group layer plan otherwise remains.

No crowds, desk, portal energy effects or new text. Runtime board/sign content, flag colors/logos and emission remain independent future controls. Fixtures are illustrated unlit but painted form shading remains: this is not verified shadow-free albedo or a tested Unity lighting setup.

Current join uses the opaque v2 continuity patch with hard boundaries at shared mullions, never broad alpha blending across portal contours. When extracting transparency, cut underlying base images out beneath the patch footprint; otherwise old frames will show through. Earlier sharpness/lighting notes describe historical checkpoints; this note and registration.json track the current source.
