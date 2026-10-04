# Hall and character art handover — 2026-10-04

Work is on `codex/hall-art-completion`, branched from freshly fetched `origin/main` at `5c7c2812adc2a750d733e388c1b7e43357b26f58`. Main was checked again at the end of generation and remained at that revision. It includes Claude's merged hall lighting, dust, day cycle and character pilot work.

The attached Unity project checkout is `C:\Users\Saleh\.codex\worktrees\hall-art-completion\NOPE`. Open that project to see these changes. The original `E:\unity\NOPE` checkout was on an older dirty `art` branch and was preserved. Its old handover was not present on main; this is the current replacement on the new branch.

## Completed priorities

1. **Camera gap and perspective:** the finite hall painting previously revealed a black strip between the railing and the 3D desk when entering desk view. `Hall Foreground Floor` now fills that area with a terracotta tile plane aligned to the painting's vanishing direction. The shader clips above the painting's lower edge and follows its lit bottom-strip colour. This is a visual foreground extension; it has no collider.
2. **Lights:** Claude's 28-light rig is included and enabled. The scene retains 6 fixture lights, 5 window shafts, 5 portal lights, 5 screens, 3 signs, global/sky lighting and the desk lamp/PC glow. Noon, sunset, night and lighting-off captures were checked.
3. **Ground colour:** the generated floor texture uses the approved hall palette; the shader matches the lit painting through the seam and day cycle. Tile scale and convergence were refined against the painted floor.
4. **All character art:** 455 new raw sheets are generated and processed, plus the 15 preserved pilot sheets: 470 source deliverables. The complete 944-key transparent sprite library is installed and natively validated in Unity. All 26 named characters have neutral, happy, angry and worried expressions (104 sprites). All required skin/face, wardrobe, modern and future layers are covered, including the sixteen cast additions absent from the older coverage document.

## Evidence and files

- Scene: `Assets/Art/Office/AnimeHallLayers/AnimeHall.unity`.
- Floor component: `Assets/Scripts/Office/HallForegroundFloor.cs`; shader: `Assets/Shaders/HallForegroundFloor.shader`; native floor assets: `Assets/Art/Office/AnimeHallLayers/Completion`.
- Hall report and 35 current camera captures: `ArtDeliverables/TimeDesk/HallLayers/Completion/README.md`. `Before` retains the original gap. Captures cover both pans, five positions along the actual desk-camera transition, three times of day, and four lighting-off endpoints. They are editor renders; a packaged game build and performance benchmark were not run.
- Character source files and their actual prompts: `ArtDeliverables/TimeDesk/Characters/Raw/batch02-*` through `batch12-*`.
- Character inventory, SHA-256 hashes, preserved pilot baseline, source diagnostics and 23 contact sheets: `ArtDeliverables/TimeDesk/Characters/Completion`. Run `validate_sources.py` there for the read-only inventory, PNG-canvas, hash and pilot-preservation checks.
- Native generated originals remain in the Codex generated-images folder. The deliverable copies are unmodified.

## Character processing completed

Game-ready layers are in `Assets/Art/Characters/Resources/Characters`, on the shared 1024x1536 canvas, with the foot pivot and importer settings used by CharacterArt. Skin and natural hair palettes, eye/nose registration, front/rear hair, transparent backgrounds, expression proportions and full accessories are processed. Reviewed isolation regions remove extra calibration garments and fitting marks. All raw sources and native generator originals remain unchanged. The two neutral future outfit sources are reference-only under the coverage plan, with no required output keys.

The 42 approved pilot keys and metadata are unchanged. Eight interim skin 2–5 face-a recolours were replaced by the new generated faces, keeping their Unity GUIDs; originals are archived under `Completion/InterimPilotHeads`. `interim-head-replacements.json` records the exact exceptions to the preserved-pilot baseline. Do not rerun `process_pilot.py` over this library.

Native Unity validation loaded all **944/944** imported sprites and runtime Resources keys, with zero errors. Independent checks verified 455 source records, 944 output keys, all installed hashes, alpha backgrounds, canvas borders and zero key-colour pixels. `Completion/ProcessedQA` contains 23 assembled source sheets and ten plates showing 96 actual wardrobe stacks with desk and passport crops; these were visually reviewed, along with isolated head pieces and accessories. See `Completion/README.md`, `processing-report.json`, `processed-validation.json`, `installed-validation.json` and `unity-import-validation.json`. Reproduction and validation menus are documented in `tools/characters/README.md`.

## Preserve the latest scene integration

The existing character EditMode suites passed **92/92 tests**, zero failures or skips; results are preserved as `Completion/character-tests.xml`. All three Python processing helper equivalence tests passed. No packaged build or performance benchmark was run.

Do not run the legacy AnimeHallLayerInstaller or desk rebuilds: they replace the current scene integration. Use native scene editing or the idempotent menus under `Tools > Terminal Art > Completion` for the foreground floor, camera captures and validation. The scene retains its 58 painted layers and gameplay hooks. Unity's native save regenerated derived Light2D bounds and two pillar-shadow meshes; gameplay objects were retained.

Floor commits: `f660424`, `9973d70`. Character generation checkpoints: `3e2316b`, `32f012f`, `f467936`; the processing/import completion follows them on this branch. Main was fetched again after installation and remains at `5c7c281`. No remote publication or merge was performed.
