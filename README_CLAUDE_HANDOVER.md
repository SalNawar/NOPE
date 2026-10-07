# Claude handover — hall-art-completion

Updated 2026-10-07. Read this before older art handovers: several historical approaches were rejected or superseded.

## Branch and project

- Branch: `codex/hall-art-completion`; remote: `https://github.com/SalNawar/NOPE.git`.
- Main implementation checkpoint: `d9016ed` (city/weather/parallax and crowd alternatives).
- Active Unity checkout: `C:/Users/Saleh/.codex/worktrees/hall-art-completion/NOPE`.
- `E:/unity/NOPE` is a separate older dirty checkout. Do not copy over or reset it.
- Unity: 6000.4.11f1. Main scene: `Assets/Art/Office/AnimeHallLayers/AnimeHall.unity`.
- This branch originally started from main at `5c7c2812adc2a750d733e388c1b7e43357b26f58`, including Claude's merged pilot character and lighting work. This is historical provenance, not a claim that main is still at that revision.
- Full commit inventory and changed-file inventory accompany this README under `ArtDeliverables/TimeDesk/BranchHandover/`.

## What the user wants and how to work

Keep a written task checklist. Distinguish implemented, technically verified, visually verified and approved. Inspect actual Unity output before saying done. The user has repeatedly rejected premature completion claims. Keep updates short and do the work.

Preserve the hall's details, perspective, left elevator/stair landing, right-hand doors and central traveller/desk focus. Do not solve perspective by zooming/cropping away architecture or moving the traveller closer. Do not add detached animated hands or deform arms to simulate poses.

Crowds are stationary silhouettes, currently BLACK, with adjustable opacity and independent appearance/disappearance. Earlier white/grey names in classes and assets are historical; do not automatically restore white. Most population belongs behind the two rear portals and on the bridge. The latest request adds some bottom-left population, not dense groups across the entire front.

The user requested muted wall/floor palette options BEFORE generating further hall art. Do not launch another hall repaint without a selection. Current warm-stone version is described below.

## Current completion status — read carefully

The latest task is partially verified. The user stopped Computer Use with physical Escape during final verification, then requested that work be pushed. Do not describe the latest city/crowd task as fully visually complete.

Completed automated evidence in `ArtDeliverables/TimeDesk/CityAndCrowdVariations/verification.txt`:
- 48 crowd locations, exactly three unique alternatives at each (144 alternatives).
- All alternatives have fixed feet and unchanged object transforms; changes happen at an invisible fade boundary.
- Black tint and opacity endpoints 0/40/65/100 percent checked.
- Eight imported 2172x724 city states, registered depth map and atmosphere atlas present.
- Eight time/weather captures produced. Rainy-night capture inspected.

Still required:
- Visually inspect every crowd composition set, specifically the new bottom-left positions and walls/stairs/rail/portal occlusion.
- Confirm latest `HallCityLookView` is attached to the scene and verify LEFT/RIGHT input in live Play mode. Its follow-up Enable Left View command timed out during the interrupted session; do not assume it completed.
- Verify motion in actual consecutive Play frames: clouds, smoke origins, airship, bus, existing taxis, rain and road lights; confirm no detached effects or depth-warp artifacts.
- Inspect all city weather/time transitions and both window regions. No packaged-build/performance validation was performed for this update.
- The saved scene currently has `crowdOpacity: 0`, `rain: 0`, `depthStrength: 0.7`. Zero opacity hides crowds. Raise the Hall lighting crowd slider for visual review, and deliberately choose whether to persist that change. Earlier all-crowd captures may be empty because the first verification preserved zero opacity.

Latest checklist: `ArtDeliverables/TimeDesk/CityAndCrowdVariations/CHECKLIST.md`.

## City assets and live rendering

Folder: `Assets/Art/Office/AnimeHallLayers/Completion/City/TimeWeather/`.

Eight panorama textures, all 2172x724:
- `CityMorningClear.png` (copy of the existing approved dense morning city).
- `CityNoonClear.png`, `CityEveningClear.png`, `CityNightClear.png`.
- `CityMorningRain.png`, `CityNoonRain.png`, `CityEveningRain.png`, `CityNightRain.png`.

Generated variants preserve the existing connected panorama's landmark framing. The source is `Completion/DeepRoom/CityDenseMorning.png`. Keep a high-floor view, grounded retro art-deco/cyberpunk human city, distinct districts, megatowers and landmarks, lived-in detail and limited water. Old city candidates remain as history, not current choices.

Additional assets:
- `CityDepth.png`: registered grayscale distance map, black far/white near.
- `CityAtmosphereAtlas.png`: 2x2 transparent atlas: cloud, smoke, cargo airship, flying bus.
- `LivingLeft.mat`, `LivingFront.mat`: existing hall aperture masks retained.
- Exact built-in image_gen prompts: `ArtDeliverables/TimeDesk/CityAndCrowdVariations/prompts.json`.

Implementation:
- `Assets/Shaders/HallLivingCity.shader`: blends four times and clear/rain textures; performs conservative depth-map UV reprojection; composites moving atmosphere/ships, subtle window variation, road headlight points and rain. This is **2.5D depth-map parallax**, not a completed set of independently inpainted building cutout layers or a reconstructed 3D city. Assess depth discontinuities visually.
- `Assets/Scripts/Office/HallCityExterior.cs`: supplies rain, depth strength, time and pan; retains existing eight window-clipped flying vehicle lanes. Reduced Motion freezes city animation.
- `Assets/Scripts/Office/HallCityLookView.cs`: Left requests existing full left pan, Right returns; smooth transition, cut under Reduced Motion; ignores input during PC focus, desk view, traveller wheel or active text fields. Needs final live-input verification.
- `Assets/Editor/TerminalArt/HallCityCrowdVariationsAuthoring.cs`: import/install and verification menus.

Menus under `Tools > Terminal Art > City Variations`:
- `Install`: imports assets, assigns living city materials and rebuilds crowd locations. Run outside Play; this rebuilds the crowd hierarchy, so do not use casually after hand edits.
- `Enable Left View`: attaches the look controller without rebuilding crowds; saves via native scene authoring.
- `Verify`: checks alternatives/opacity and generates evidence captures. Latest source also checks left-view endpoints; the saved report predates that final addition.

`Tools > Terminal Art > Lighting > Time Slider` opens **Hall lighting** with time presets, Crowd opacity (%) and City rain (%). Play-mode slider edits are temporary. The rain slider currently changes the exterior only; it does not implement a complete indoor weather-lighting simulation.

## Crowd assets, density and behavior

Runtime: `Assets/Scripts/Office/HallWhiteCrowds.cs`.
Authoring: `Assets/Editor/TerminalArt/HallWhiteCrowdAuthoring.cs`.
Shader: `Assets/Shaders/HallWhiteCrowdFade.shader`; contacts: `HallWhiteCrowdContact.shader`.
Assets: `Assets/Art/Office/AnimeHallLayers/Completion/WhiteCrowds/`.

Reuse the approved silhouette style. Existing original groups are under `Assets/Art/Office/HallCrowds/`; three prior atlases provide variations, activities and station life. New `StationAlternatives.png` adds 16 compositions: porter/trolley, ticket couple, umbrella commuter, monks, cleaner, police, parent/child, newspaper reader, wheelchair/companion, students, courier, elderly pair, tourists, mechanic, instrument traveler and parent with children. The atlas was regenerated to remove stray speckles; final visual checks remain necessary.

`StationNormalized_00` through `_62` normalize meshes around the feet. Each location holds three mesh/material alternatives. At each full appearance cycle the next alternative replaces the previous one at zero visibility. People do not walk or slide. Reduced Motion holds alternative zero.

48 placements: previous 45 (24 rear, 15 bridge, 4 middle, 2 near), plus three left-lane floor placements at source-pixel feet (650,570), (745,510), (795,550). Do not regard those numeric anchors as proof of correct placement; inspect them.

Opacity: 40% reproduces authored per-group alpha; 65% increases visibility; 100% is opaque at full fade; 0% hides. This control is independent of arrival/departure fades. Crowds remain black, so opacity controls their apparent grey over light backgrounds.

## Hall composition, materials and floor

Final active materials come from `Completion/WarmStone/`:
- `HallWarmStone.png`: cream/ivory stone walls and polished pale floor with painted architectural reflections, burgundy inlay, graphite bridge/left column and brass details.
- `WarmStoneArchitecture.mat`: uses the registered original painting as fixture-luminance reference so pale walls do not glow at night.
- `Assets/Editor/TerminalArt/HallWarmStoneAuthoring.cs` installs and captures this state.
- Evidence: `ArtDeliverables/TimeDesk/WarmStone/`.

These reflections are painted, not live reflections of changing crowds. Earlier petrol/teal walls and grey floor are superseded by the warm-stone request, but remain in history. Several camera-redraw experiments were rejected because they lost elevators, doors and hall depth. Preserve the restored detailed hall and existing desk/traveller alignment.

Relevant systems: `HallForegroundFloor.cs` and its shader, `HallBackdrop.cs`, `AnimeHallPresentation.cs`, `HallDeepPortalRegistration.cs`, and editor authoring for DeepRoom, DetailedCalibration and FocusAlignment. Foreground floor continuity is registered to the painted floor rather than an unrelated repeating tile grid. Camera transitions must be checked, not just one still frame.

## Lighting and shadows

The branch retains Claude's lighting rig and clock hooks. It adds `HallBakedCycle`, `HallBakedLighting`, four-state art, fixture correction and the time slider. `HallDeepLayout.shader` uses original fixture luminance to isolate ceiling emitters.

Hall floor shadows were repeatedly revised after user feedback. The current geometric approach projects approximate source-calibrated upright columns/posts/pedestals and hollow portal rings onto the floor, using the same daylight direction as the desk. Key files: `HallFloorShadowGeometry.cs`, `HallFloorShadowGeometry.hlsl`, `HallFloorShadowAuthoring.cs`, `HallGroundShadows.cs`, `HallBakedLighting.cs`.

Older shadow-blob/footprint approaches are superseded. Approximate reconstructed geometry is not a full 3D survey. Night local-fixture contact shading and overall painted perspective still have open criteria in `ArtDeliverables/TimeDesk/SCENE_REVIEW_CHECKLIST.md`. Do not close them based solely on compilation or old reports.

## Cashier animation — preserve this fix

`624425a` is the accepted implementation checkpoint: cashier grows uniformly then shrinks, anchored at its base on the desk, without dipping down.

The broad nine-prop assembly change `1dd2b4e` was explicitly reverted by `0747570`. Do not reinstate it. The final fix is cashier-only: Anchor_Till contains its seven mesh pieces plus CreditsNumber; Pulse amplitude .08, duration .35. Default builder updated in `Assets/Editor/OfficeSceneUIBuilder.Desk.cs`.

Native helper: `Assets/Editor/TerminalArt/CashierPulseAuthoring.cs`. Evidence in `ArtDeliverables/TimeDesk/CashierPulse/`: two click cycles checked, peak 1.08, no downward base movement, exact return, rest/peak/returned captures inspected.

## Character library and poses

- Raw sources: `ArtDeliverables/TimeDesk/Characters/Raw/`.
- Inventory, manifests, hashes and QA: `ArtDeliverables/TimeDesk/Characters/Completion/`.
- Installed library: `Assets/Art/Characters/Resources/Characters/`.
- Processing/reproduction: `tools/characters/README.md`.

Historical completed library validation: 455 new source sheets plus 15 pilot sheets, 944 runtime sprite keys, 26 named characters with four expressions (104 sprites), 92/92 Unity character tests and three processing helper tests. These are recorded earlier results, not reruns of the entire latest branch. Preserve pilot GUIDs and the documented narrow exceptions; do not rerun process_pilot over the complete library.

Whole-character poses are separate from expression coverage. `ArtDeliverables/TimeDesk/Characters/Poses/README.md` records only TWO complete poses each for the specified Egyptian female and Greek male looks. Most cast members do NOT yet have complete pose variations. `CharacterPoseLibrary.cs`, `LookSpriteStack.cs`, `CharacterPoseInstaller.cs` and `CharacterPoseSources.cs` integrate those exact-look replacements. No detached hand animation. The user raised hair/necklace layering problems; do not treat all generated layered art as visually approved.

The older `ArtDeliverables/TimeDesk/HallLayers/AnimeRegistered/NEW_CHAT_HANDOVER.md` contains provenance but also superseded traveller distance, gestures, palette and completion statements. Use this README and newer specific checklists as the authority for current status.

## Verification, saving and safety for continuing work

1. Open the actual worktree scene. Inspect current branch and dirty files before editing.
2. Read this README and task checklists; reproduce the specific complaint with captures.
3. Check latest code compilation, shader errors and live scene state. Then inspect visuals at time/weather endpoints and during camera motion.
4. Save scene changes through Unity APIs/menu actions, not direct YAML editing. Source code edits are normal text changes.
5. Native `HallWhiteCrowdAuthoring.Save()` settles pan, art, lighting and crowd registration before saving. It sets pan to forward; do not invoke it expecting to retain a left-view preview.
6. Existing `HallFocusAlignmentAuthoring.Capture` calls `HallCityExterior.Apply(0)` during capture. Its stills do NOT prove animation over time. Use live frames or an explicitly time-controlled capture when testing motion.
7. Stop Play before authoring installs. Avoid regenerating unchanged assets or running old installers that replace current materials/layout.
8. Do not broad-stage or reset: unrelated dirty screenshots, generated project files, scanner materials and font fallback changes exist in the worktree. They were excluded from the latest commit.
9. Record incomplete checks honestly. No fresh full regression suite, packaged build or performance benchmark is claimed for d9016ed.

Unity Skills REST registry is `C:/Users/Saleh/.unity_skills/registry.json`. Historically port 8090, occasionally a stale 8091 listener appears after reload. Use registry plus health checks; add request timeouts. A timeout is not proof an editor action failed or succeeded. Reinspect before repeating destructive/rebuilding actions. Computer Use was stopped by the user during the latest session; do not resume UI control without a new task authorizing it.
