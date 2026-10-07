# Famous traveller art handoff

Branch: `codex/famous-travellers-33`, created from `origin/main` at `dd2bb64`.

## Scope and status

This delivery targets the current 33-person roster in `../Production/FAMOUS_PREMADES_REQUEST.md`: four expressions per traveller, plus three coherent full-body gesture poses requested by Saleh. All 231 original PNGs are saved and visually reviewed: 33 neutral, 99 additional expressions, and 99 full-body gesture poses. The complete file audit passed on 7 October 2026. See `CHECKLIST.md`, `manifest.json`, and `VERIFICATION.md` for coverage and verification limits.

Open `REVIEW.html` locally and select a character to compare their available images. `Raw/{id}/` contains the source images and an exact `.prompt.md` beside each image. `roster.json` preserves names, ages, places, and identifying features.

## File contract

Every PNG is 1024 x 1536, full body, on the requested flat green background. Names are `premade_{id}_{variant}.png`. Variants are `neutral`, `happy`, `angry`, `worried`, `pose_explaining`, `pose_thinking`, and `pose_objecting`.

The four expression files share the neutral stance. Gesture images redraw connected shoulders, sleeves, elbows, and hands as part of the whole character. Use complete sprite swaps for these poses. Do not extract and independently animate hands.

The three amendments to Block P are applied: rulers use court or travelling dress without crowns, sceptres, or uraeus; photography-era figures are stylised; soldiers are unarmed. Specific permitted identifiers such as Caesar's laurel and Cleopatra's simple diadem are retained.

## Integration boundary

These are raw commission originals, not imported Unity sprites. Green removal, edge cleanup, contract registration, Unity import settings, runtime expression/pose selection, and in-scene verification remain integration work. No scene or character animation code is changed by this delivery. The separate new-era garment-layer library is not included in this whole-character set.

The first Ramesses, Kurosawa, and Saladin sets have higher head placement than the later sets. Review registration against `docs/CHARACTER_ART_CONTRACT.md` before use. Expression edits preserve the visual design, but generated pixels and details are not guaranteed identical outside the face. Review transitions at gameplay scale before integrating. Keep the originals unchanged and put processed sprites in the project's established output locations.

## Verification records

`manifest.json` records each source, exact prompt, output path, dimensions, SHA-256, and visual review state. `reviewed; registration pending` means visual inspection occurred and technical alignment is still pending; it does not mean tested in Unity. The save helper at `tools/characters/save_famous33.py` checks PNG headers and dimensions, copies the original bytes, and updates the checklist and gallery.

Reproduce the delivery audit from the repository root with `python tools/characters/audit_famous33.py`.
