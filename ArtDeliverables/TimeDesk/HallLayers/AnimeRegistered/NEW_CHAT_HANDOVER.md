# Hall and character art handover — 2026-10-03

Work is on `codex/hall-art-completion`, branched from freshly fetched `origin/main` at `5c7c2812adc2a750d733e388c1b7e43357b26f58`. Main was checked again at the end of generation and remained at that revision. It includes Claude's merged hall lighting, dust, day cycle and character pilot work.

The attached Unity project checkout is `C:\Users\Saleh\.codex\worktrees\hall-art-completion\NOPE`. Open that project to see these changes. The original `E:\unity\NOPE` checkout was on an older dirty `art` branch and was preserved. Its old handover was not present on main; this is the current replacement on the new branch.

## Completed priorities

1. **Camera gap and perspective:** the finite hall painting previously revealed a black strip between the railing and the 3D desk when entering desk view. `Hall Foreground Floor` now fills that area with a terracotta tile plane aligned to the painting's vanishing direction. The shader clips above the painting's lower edge and follows its lit bottom-strip colour. This is a visual foreground extension; it has no collider.
2. **Lights:** Claude's 28-light rig is included and enabled. The scene retains 6 fixture lights, 5 window shafts, 5 portal lights, 5 screens, 3 signs, global/sky lighting and the desk lamp/PC glow. Noon, sunset, night and lighting-off captures were checked.
3. **Ground colour:** the generated floor texture uses the approved hall palette; the shader matches the lit painting through the seam and day cycle. Tile scale and convergence were refined against the painted floor.
4. **All character source art:** 455 new raw sheets are generated and saved, plus the 15 preserved pilot sheets: 470 planned source deliverables. All 26 named characters have all four expressions (104 sheets). All current skin/face, wardrobe, modern and future source requirements are covered. The combined plan covers all 944 logical required game keys, including the sixteen cast additions absent from the older coverage document.

## Evidence and files

- Scene: `Assets/Art/Office/AnimeHallLayers/AnimeHall.unity`.
- Floor component: `Assets/Scripts/Office/HallForegroundFloor.cs`; shader: `Assets/Shaders/HallForegroundFloor.shader`; native floor assets: `Assets/Art/Office/AnimeHallLayers/Completion`.
- Hall report and 35 current camera captures: `ArtDeliverables/TimeDesk/HallLayers/Completion/README.md`. `Before` retains the original gap. Captures cover both pans, five positions along the actual desk-camera transition, three times of day, and four lighting-off endpoints. They are editor renders; a packaged game build and performance benchmark were not run.
- Character source files and their actual prompts: `ArtDeliverables/TimeDesk/Characters/Raw/batch02-*` through `batch12-*`.
- Character inventory, SHA-256 hashes, preserved pilot baseline, source diagnostics and 23 contact sheets: `ArtDeliverables/TimeDesk/Characters/Completion`. Run `validate_sources.py` there for the read-only inventory, PNG-canvas, hash and pilot-preservation checks.
- Native generated originals remain in the Codex generated-images folder. The deliverable copies are unmodified.

## Character processing stage still separate

The new character files are opaque green/magenta source sheets, not finished registered transparent sprites. Green/magenta extraction, exact skin/hair palette normalization, template registration, head crops, hair front/back splitting, colour variants, game-stack QA and Unity import remain for the existing processing pipeline. Some supports include extra non-magenta calibration clothing; extract only the named item. Tall hats and hair need scalp/eye-line registration. Ada's wide hoop skirt needs fitting to the production safe zone. No character source is being claimed as final game-ready output.

The installed 50 resource PNGs and pilot source files were preserved. `tools/characters/process_pilot.py` was not rerun. Do not overwrite that approved pilot while processing the new sources. `generation-manifest.json` explicitly distinguishes completed source generation from incomplete game-layer processing.

## Preserve the latest scene integration

Do not run the legacy AnimeHallLayerInstaller or desk rebuilds: they replace the current scene integration. Use native scene editing or the idempotent menus under `Tools > Terminal Art > Completion` for the foreground floor, camera captures and validation. The scene retains its 58 painted layers and gameplay hooks. Unity's native save regenerated derived Light2D bounds and two pillar-shadow meshes; gameplay objects were retained.

Floor commits: `f660424`, `9973d70`. Character generation checkpoints: `3e2316b`, `32f012f`; the final source-completion commit follows them on this branch. No remote publication or merge was performed.
