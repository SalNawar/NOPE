> Superseded by the current desk-only rollback and CRT study. Do not run the old whole-office build. See RESTORY_STYLE_GUIDE.md and BlenderCRT/provenance.md.
> 2026-09-26: Claude removed the tools this document names (see the list in CURRENT_STATE.md) and the art tools no longer write the leftover gameplay objects. Read **Rules for the art side** in CURRENT_STATE.md first.

# Paused checkpoint — 2026-09-24

Paused at the user's request. Do not continue art iterations until asked.

## Saved in Unity

- OfficeScene rebuilt using 177 instances from the imported packs: desk, hall structure, floor, ceiling and portal supports.
- PC on the left, keyboard in front and mouse to its right; desk console faces the player.
- Historical/middle-ground furniture clutter remains removed.
- Painted megacity layer, separate existing animated traffic, and paper SpriteRenderer layers.
- Existing clock, calendar, stability, credits, NEXT and CRT interaction objects retained; CRT zoom/hit target repositioned.
- Second Game-view capture: `../DeskFinish/Iterations/41_pack_material_corrections.png`.
- That capture followed successful Play-mode entry; the console reported no errors. CRT click/navigation has not yet been exercised after the replacement.

## Saved files awaiting the next visual check

The builder and layout contain changes made after capture 41: shadow-bias/fill adjustments, ceiling bounce lights, lower banners and departures board, and the newly generated `lamp_painted.png` atlas. These latest edits have compiled, but the builder has **not** been rerun to apply them to the saved scene. Do not mistake the saved scene for the latest manifest output.

(Historical: that builder was removed on 2026-09-26.) Continue by running `Tools/Office Art/Build Imported Office` in Edit mode, then inspect the actual Game view before saving further changes. Check floor/shadow artifacts, lamp palette, banner suspension, portal supports and paper surface contact. Exercise CRT focus/back and NEXT. The result has not reached demonstrated ReStory visual parity.

## References and recovery

- Approved concept: `Mockups/office_mix_02_hybrid.png` (image-generation concept, not an engine render).
- Reference: `../DeskFinish/ReStory_current_reference.png`.
- Original vendor prefabs remain unmodified; materials are project-owned variants.
- Unity was restarted after its local API stopped responding; snapshots are under `Recovery/` and Unity's `Assets/_Recovery/`.
- UnitySkills can move ports after domain reload; the iteration helper now discovers the responsive NOPE server on ports 8090–8100.

No additional art work or tests should run as part of this pause; only save, commit and push.
