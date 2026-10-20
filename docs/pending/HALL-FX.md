# HALL-FX: nothing in the world flickers

Branch `fix/a-hall-fx`, cut from `fix/p0-cuts` (5aecb60). Phase A merge #4. Sources: FIX_PLAN.md §3 "HALL-FX" with FIX_PLAN_v2.md "HALL-FX: delta" (v2 wins), BUGLIST.md, DECISIONS_1019a.md, GAME_FEEL_BIBLE.md rows E8 and J4, PHASEB/LEASES.md.

**Goal (player):** the city, hall, monitor and signs are calm. Windows fade on once, headlights glide, the Helix monitor moves smoothly and glitches once on a drop, the AVAILABLE sign pulses only when it wants a click, and the portals fill their painted rings exactly (notes 5, 13, 14, 23, and the portal note).

**Code only.** No Unity editor or player was run (Saleh may be on the PC). Every check that needs Unity is listed under RESULT as "deferred: Unity", with its recipe, so the Unity pass can run them in one batch.

## Saleh's calls this track builds (quoted, DECISIONS_1019a.md)

- **D12 Firing:** "you are fired if you destroy the timeline, sometime we will have random events and the timeline will be more than 70 corrupted through no fault of the player". FIX_PLAN_v2's 56e delta: `SetWarning` "shows near the **destruction point**, read from CONTENT's config"; 70% is not a firing warning.
- **D20 Pause:** "you should be able to close the booth with the available sign if you want to pause". That is BELL-SIGN's (Phase B, `AvailableSignLink.cs` is leased to it after #4). HALL-FX only stops the pulse looping (BUG-67, FIX_PLAN_v2 1b: "67 AVAILABLE pulse: FIX (HALL-FX). BELL-SIGN later adds the sign's pause state").
- **[S] J4 (bible):** "An animated timeline with **no number**; calm, readable motion". No number was added anywhere.

Everything else below that is a design choice is marked **proposal (ours)**.

## What changed, per BUG id

### BUG-13 City windows strobe, headlights twinkle, daylight cars at night (HALL-FX's whole cluster)
- **The window hash never returns 0, and no crowd of cells shares a moment.** The old float hash was exactly 0 on 600 of the 28,800 cells (they blinked at 6 Hz all day) and gave up to 324 cells one moment. It is now an integer Wang hash (`HallCity.WindowHash` in C#, `CityHash` in `HallLivingCity.shader`, bit for bit), mapped into [1/256, 1). Exhaustive test over the grid.
- **The catch-up blink is gone.** Each window fades in once: its light is `saturate((lit - hash x 0.97) / fadeBand)` (`HallCity.WindowLevel`), a function of the lit share alone. The lit share is `_HallCityLights.x`, which LIGHT's `HallLightingRig` computes from the shift clock's hour (never `Time.time`), so a frozen clock or a forced hour freezes every window. The add is gated on `lit > 0`.
- **The fade takes about 0.4 s at the clock's pace.** `HallCityExterior` measures how fast the lit share moves and sizes the fade band so a window crosses it in `HallCity.FadeSeconds` (0.4 s; FIX_PLAN: 0.3-0.5 s). The band is clamped to [0.0005, 0.05], eases over 1 s when the pace changes, and holds still while the share holds, so nothing moves on a frozen clock. It is the global `_CityWindowFade`, so the city view's copy of the material reads it too.
- **Headlights are at least about 1.8 px across.** The Gaussian is widened by the pixel's size (`fwidth(uv)`, at least 0.9 screen px each side), and dimmed a little as it widens (`pow(area ratio, 0.35)`) so a widened light keeps about its weight.
- **They fade at the lane ends** in the hall window as in the city view (before, only the city view faded).
- **Clouds, the airship and the bus cross from off one edge to off the other** in the hall window too. Before, `Fly` kept the raw `frac` in the hall, and the second cloud wrapped with a plain `frac`, so they popped in view.
- **The flying traffic sprites** (`HallCityTraffic.shader`) take four rotated-grid samples over each pixel's footprint (sub-pixel smoothing for the unmipped art), and fade their outermost screen pixel (the margin), so their quad's edge never crawls.
- **The traffic is tinted by the hour (CODE-69).** Each car's colour is `HallCity.HourLight` of the hall lights' hour: the same four-state light the city painting is lit by, and the light CityView already gave its own traffic. It also fades at its lane's ends (`HallCity.LaneEndFade`, 6% of the lane), so the lane's wrap never pops it.
- **Files:** `Assets/Shaders/HallLivingCity.shader`, `HallCityTraffic.shader`, `Assets/Scripts/Office/HallCityExterior.cs`, new `Assets/Scripts/Visuals/HallCity.cs`.

### BUG-54 Helix River monitor strobes (and BUG-56e, the warning state)
- **One readable glitch on a drop, then calm.** The glitch used to grow with the warning band and with the day's losses. The day's-loss part never decayed (the "gain per correct call is 0" root cause), so after one mistake the glass tore at 10 Hz all shift. Now any fall of the hidden stability starts one burst: `glitchWild` x (1 - u)^2 over `glitchSeconds` 0.2 s (bible J4: "1 burst of 200 ms per drop"), then 0.
  - The bands that slip are chosen by the burst's number (`GlitchSeed`), so they kink once and slide back as it fades. Before, they were re-rolled at 10 Hz.
  - The chroma split shows only while the burst is over half strength: about 2 frames at 30 fps (bible J4: "a 2-frame RGB split on the monitor only").
  - No glitch under Reduced Motion.
  - The knob `agitationGlitch` is gone. The day's losses still speed the motes (`agitationFlow`).
- **The 15 Hz critical-band flicker is gone.** `HelixRiverFrame.Flicker` and the shader's `_Flicker` are deleted.
- **Speed cap: no detail crosses more than 0.5 px a frame at 30 fps** (FIX_PLAN: "capped to ≤0.5 px per frame at the RT scale").
  - The monitor measures its glass's height in pixels where it is drawn: the screen, or the render texture its canvas's camera draws to (`HelixRiverMonitor.ScreenScale`).
  - `HelixRiver.SpeedCap` turns that height into a speed. The river's clock slows by cap / `FastestOnClock` (80 reference px/s: the current streaks, the rungs and the twist).
  - The motes' travel is capped and integrated (`MoteTravel`), so a change of speed never jumps them. Before, the shader used `t * _Flow`, and a change of `_Flow` swept every mote.
  - On the desk monitor (about 96 px tall at 1080p) the river runs at about half speed. In the small tray strip it keeps its speed.
- **The other strobe sources are gone:**
  - Rungs (16 px apart) and current streaks (6 px rows) fade out before they are packed closer than about 2 screen pixels, instead of aliasing.
  - The scanlines are on the glass (uv space, 6 px period, soft) and fade out when finer than 3 screen pixels. Before, they were hard steps in screen space, which crawl when the monitor moves.
  - The grain is still: it was re-seeded at 30 Hz.
- **The warning state (56e).**
  - It shows near the destruction point: when the shown stability is in the critical band, at or under `GameConfigSO.firedAtStability + stabilityCriticalMargin` (63 today).
  - It goes off only once the shown stability climbs `warningHysteresis` 0.5 points past that line, so it never flickers on the line.
  - It eases in and out over `warningSettleSeconds` 0.8 s.
  - Its look is steady: the banks turn red, the glass warms, and the amber strand drifts red. There is no pulse or strobe.
  - `HelixRiverMonitor.SetWarning(bool)` (the P0 seam) holds it on for FLOW, CONTENT and TIMELINE-FAIL whatever the stability. It is runtime state only.
  - **Proposal (ours):** the critical band, not the warning band, is the warning line. The config's `stabilityWarningMargin` (70 today) is exactly the 70% D12 rules out ("70% is not a firing warning"); the critical band is "near the destruction point". CONTENT (#17) owns both values (56d) and may retune them; the monitor reads whatever the config says.
- **Files:** `Assets/Scripts/Domain/HelixRiver.cs`, `Assets/Scripts/UI/HelixRiverMonitor.cs`, `Assets/Shaders/HelixRiver.shader`, `Assets/Data/Config/HelixRiver_Default.asset`.
  - New knobs: `glitchSeconds` 0.2, `warningSettleSeconds` 0.8, `warningHysteresis` 0.5, `maxPixelsPerFrame` 0.5, `captureFps` 30.
  - `agitationGlitch` is removed. `glitchWild` now means the burst's peak.

### BUG-67 The AVAILABLE sign pulses forever
- From the moment the sign starts wanting its first press, it pulses `InvitePulse.Cycles` beats (`DeskConfigSO.readyInviteSeconds` 1.6 s each), then holds lit and steady. Each beat is a cosine dip from the lit ink toward the paused ink and back, so the pulse starts and ends lit with no jump. The component stops running when it settles. Under Reduced Motion it is lit and steady at once.
- **Proposal (ours):** 3 cycles (FIX_PLAN: "2-3"). It is a constant in `InvitePulse`, because `DeskConfigSO` is DOC-HANDLE's: see the knob request below.
- BELL-SIGN (Phase B, lease after #4) adds the sign's pause state (D20).
- **Files:** `Assets/Scripts/Office/AvailableSignLink.cs`, new `Assets/Scripts/Visuals/InvitePulse.cs`.

### BUG-71 The hall slot crossfade shows the painting through
- **Unscaled time.** It was scaled time, so a hit-stop froze it.
- **Two phases** (`SlotCrossfade`). The incoming face fades in over the first half while the outgoing face stays opaque, then the outgoing face fades out over the second half under the opaque incoming one. Wherever both faces cover, one of them is opaque at every moment, so the painting never shows through. This holds whichever face draws on top: both share the slot's sorting order, and their order is not otherwise defined. Before, Fade / 1 - Fade showed 25% of the painting at mid-fade.
- **Files:** `Assets/Scripts/Office/HallSlotsLink.cs`, new `Assets/Scripts/Visuals/SlotCrossfade.cs`.

### BUG-26a Portals: masks, openings, occlusion, the probe (26b, the bloom, is LIGHT's at #11)
- **Per-ring painted-aperture masks, generated from the painting, not a colour test at run time.** `tools/hall/portal_masks.py` reads the hall drawing (`HallWarmStone.png`, the layer "62 Approved deeper architecture", read only) and writes one RGBA crop per ring into `Assets/Data/Config/HallPortalMasks.png` (linear, no mips, uncompressed). How it finds each hole:
  - the wall and glass seen through it are bright, or the warm unsaturated wall in an arch's shade, never brass or the ring's cool grey;
  - the hole is the convex hull of those pixels' components that meet the ring's seed ellipse;
  - an arch's hole (rings 4 and 5) ends at the bay fence's top rail.

  The four channels:
  - **R, the hole:** antialiased.
  - **G, what stands in front:** rows across the hole that are mostly not open (the fence rails), and on the full rings columns that are (the mullion bar). Dark things that do not span the hole, such as a pipe on the wall behind ring 5, are behind it, so the vortex covers them.
  - **B, behind the glass:** the part below the fence rail.
  - **A, the edge depth:** 6 canvas px deep, for the rim.
- **The openings come from mask fits.** Each ring's opening in `HallPortals.asset` is the R channel's moment fit: centre, and radii of twice the standard deviations. Its `reach` is the farthest hole pixel in opening radii, so the iris uncovers it all. The old hand measures were 9% too tall on ring 1 and far off on rings 4 and 5. The tool's overlays were checked by eye, ring by ring.

  | Portal | Opening (centre, radii; canvas px) | Old hand measure |
  |---|---|---|
  | 1 | (1094.2, 486.6), 53.3 x 47.9 | (1096, 488), 49 x 56 |
  | 2 | (969.4, 365.2), 23.3 x 18.9 | (968, 364), 25 x 23 |
  | 3 | (1210.4, 366.5), 23.4 x 20.9 | (1210, 363), 25 x 23 |
  | 4 | (1764.7, 192.5), 17.8 x 12.3 | (1762, 213), 16 x 41 |
  | 5 | (2004.9, 187.9), 25.7 x 21.1 | (2003, 213), 28 x 50 |
- **One portal source (BUG-73d).** The new `HallPortalsSO` (`Assets/Scripts/Office/HallPortalsSO.cs`, `Assets/Data/Config/HallPortals.asset`) holds:
  - the drawing, its canvas, the masks;
  - each ring's layers, opening, reach and mask crops;
  - the vortex tuning and the pulse seconds.

  How it reaches the game:
  - Build Office UI hands it to every `PortalEffect`, one per ring (`OfficeSceneUIBuilder.Portals.cs`).
  - `AnimeHallPortalLink` reads the rings from the effects. The binder's `config` argument is no longer read: see the cleanup list.
  - The opening is scaled from the canvas to the drawing as imported, so a downsized import still fits.
  - The DeskConfigSO portal fields are no longer read: see the cleanup list.
- **The vortex is clipped by the mask.** `TimeDesk/PortalGlow`:
  - It fills exactly the hole: `inside(iris) x R x (1 - G) x lerp(1, glassOpacity, B)`.
  - The old colour classifier (`Painted()`) is deleted, and so are the drawing sample, `_DrawingRect` and the fence row.
  - **No halo is drawn on the metal.** The ring-glow band is deleted. The glow around the ring is LIGHT's bloom on the vortex's core, whose heart now lifts past 1 by `ringGlow` (BUG-26b, at #11).
- **A layered cel vortex.**
  - A slow back layer (one more arm, turning the other way, dim) under the front layer's two hard tones.
  - The ink line and the bright rim follow the painted edge (the mask's A channel).
  - Every hard edge is antialiased on its unwrapped coordinate: the arms' bands use the periodic distance, so there is no seam where the angle wraps, and the AA width is analytic, not `fwidth(frac())`.
  - The sparkles are antialiased and fade out when the opening is under about 20 px on screen (they shimmered on the rear rings).
- **BUG-64c.** `PortalEffect` reads Reduced Motion live every frame. Before, it was cached at Show and at each flare. Turning it on stills an open vortex at once and finishes its iris.
- **The probe.** The circular config-against-config check is replaced by:
  - a data test: `HallPortalsTests` re-fits the PNG's pixels in C#;
  - an effect-on vs effect-off capture: `HallFxRig`'s `hallPortalsShot`, read by the audit's `portals.py`; see RESULT.
- `PortalVortex.TextureQuad` and `FenceV` are deleted (no caller). `PortalVortex.MaskMap` is new.
- **Files:** `Assets/Art/Office/Gameplay/Shaders/PortalGlow.shader`, `Assets/Scripts/Office/PortalEffect.cs`, `AnimeHallPortalLink.cs`, `Assets/Scripts/Visuals/PortalVortex.cs`, `Assets/Editor/OfficeSceneUIBuilder.Portals.cs`, new `HallPortalsSO.cs`, `HallPortals.asset`, `HallPortalMasks.png`, `tools/hall/portal_masks.py`.

### BUG-65l HallCityExterior's per-frame property writes
- The clock (`_CitySeconds`, `_CityMotion`) and the fade band are now globals: one `Shader.SetGlobalFloat` each a frame, instead of a property-block write on every living-city renderer.
- The pan and depth reach the window, its panels and the hall's Deep Layout and Waiting Bay layers only when they change.
- The cars' aperture registration is written only when the window's transform changes. The cars' positions and colours still change every frame, because they move.
- Nothing allocates: the property IDs are cached, and the lights rig is found once.

### BUG-73d Dead code, one portal source, the city rain path, HallCityAuthoring
- **Done:**
  - `HallPortals.asset` is the single portal source for HALL-FX's code.
  - The rain path is gone from the city shader: the four rain textures, `_CityRain`, the wet blend and the rain streaks.
  - The stale installer `Assets/Editor/TerminalArt/HallCityAuthoring.cs` (CODE-136: it overwrote and saved the shipped hall; no caller) is deleted.
- **Left, because files outside HALL-FX still read them (cleanup list):** `HallCityExterior.rain` (now inert), and the ring coordinates in three tools.
- **Blocked by the art side (art request):** `HallDeepPortalRegistration` and `HallDeepPortalGlow.shader` are dead. The registration finds no SpriteRenderer under a `PortalEffect`, so it never assigns its material. But `AnimeHall.unity` holds the component, and the art-side `PortalGlowClipped.mat` uses the shader. Deleting them now would leave a missing script in the shipped art scene. The same goes for the old `OfficePortal.shader`: `Portal_Energy.mat` uses it in the retired OfficeScene.

### Feel cards (GAME_FEEL_BIBLE rows HALL-FX owns: E8, J4) — the evidence is Unity's (deferred)
- **J4 Timeline corruption (the monitor part).** The rest of J4 is TIMELINE-FAIL's, CAM's and FEEDBACK's: the breach stack, the shake, the desaturation and the cues.

  | Check | What the code does |
  |---|---|
  | SS | The band kinks once, then settles (one seeded burst) |
  | PE | Glitch blocks: one 200 ms burst per drop |
  | TR | A 2-frame RGB split, on the monitor only |
  | AN | The warning state eases in near the destruction point. The river's warm drift as stability nears the line is TIMELINE-FAIL's |
  | AC | No number, calm motion: the speed cap |
  | Flicker | The `hall_fx_helix` sequences (RESULT) |
- **E8 Departure board.** HALL-FX's part is "no strobe": the board's rows are unlit text on the hall's `HallDisplays` layer, and they pass the city and office flicker sequences. The split-flap riffle, `board_flip`, and the desk duplicate's removal (BUG-36) are PORTALS', FEEDBACK's and DESK-PLAN's.

## Tests

The baseline (fix/p0-cuts) was passed 3710, failed 0, with compile_check at 0 errors and 161 warnings. After this branch it is **passed 3723, failed 0** and **0 errors** in every project.

| Test | Covers |
|---|---|
| `HallCityTests.TheWindowHash_IsNeverZero_OverTheWholeGrid_AndSpreads` | Exhaustive over 240 x 120 cells: no 0, at most 2 cells share a moment, none on the floor (BUG-13) |
| `HallCityTests.AWindowsLight_IsAFunctionOfTheLitShareAlone_RisingOnce` | The window state as a function of shift time only; off by day; monotone |
| `HallCityTests.TheFadeBand_FollowsTheClocksPace_AndHoldsWhileItHolds` | About 0.4 s at the clock's pace; held while frozen; a jump of the hour never smears |
| `HallCityTests.AFlyer_FadesAtItsLanesEnds_SoTheWrapNeverPops` | The lane-end fade |
| `HallCityTests.TheNightsTraffic_IsLitDarkerAndBluerThanNoons` | H5 traffic luma is at most 50% of H2's, and blue (CODE-69) |
| `HelixRiverTests.ADrop_GlitchesOnce_AndTheGlitchReturnsToZero` | The glitch gain returns to 0; one burst; one seed; calm for the rest of the shift; a gain starts none |
| `HelixRiverTests.TheWarning_ShowsSteadilyNearTheDestructionPoint_AndCanBeHeld` | Not at the old 70% band; on at the critical line; eased; hysteresis; held |
| `HelixRiverTests.NoDetail_OutrunsHalfAPixelAFrame_OnAnyDisplay` | The speed cap for the clock and the motes |
| `HelixRiverTests` (5 changed) | No flicker any more; agitation speeds only the motes; Reduced Motion: no glitch, warning kept |
| `InvitePulseTests` (2) | 3 beats, starts and ends lit, then steady lit; steady under Reduced Motion (BUG-67) |
| `SlotCrossfadeTests.ThePainting_NeverShowsThrough_WhereBothFacesCover` | Either draw order; each face moves one way (BUG-71) |
| `HallPortalsTests` (4) | The asset's openings match a C# re-fit of the mask PNG (a data test, not a self-comparison); holes lie inside their crops and the quad; occluders and glass lie on the hole; crops apart; the atlas wired, linear, without mips |
| `PortalVortexTests.MaskMap_PutsTheQuadOnItsRingsCropOfTheAtlas` | The quad's uv to the atlas, at the centre and both corners. `TextureQuad` and `FenceV` tests are deleted with the code |
| `Assets/Editor/SeamTests/HallFxSeamTests.SetWarning_IsRuntimeStateOnly` (NeedsEditor) | The seam holds the warning without touching serialized state |

**Waiting for QA-RIG (#2; not on this branch's base):**
- `docs/pending/HALL-FX/HallFxChecks.cs.txt` holds the DemoGate checks: `hallfx.city.flicker`, `hallfx.helix.flicker`, `hallfx.sign.flicker`, `hallfx.bp.city.flicker`, `hallfx.bp.helix.flicker` and the static `hallfx.portals.source`. It goes to `Assets/Editor/QA/Checks/HallFxChecks.cs`.
- `docs/pending/HALL-FX/HallFxRig.cs.txt` holds the ops `hallPortals` and `hallPortalsShot`. It goes to `Assets/Tests/PlayMode/HALL-FX/HallFxRig.cs`.
- `docs/pending/HALL-FX/hall_fx_portals.json` goes to `tools/qa/scenarios/` after the rig is in.
- The scenarios `tools/qa/scenarios/hall_fx_city.json`, `hall_fx_helix.json` and `hall_fx_sign.json` use only QA-RIG's built-in ops, so they are on this branch.
- **Tool:** `tools/hall/region_luma.py` measures a region's luma range in a sequence, and compares two sequences: the sign's ±3, the crossfade's ±5% and the traffic tint.

## RESULT

| Step | Status |
|---|---|
| compile_check_any | **0 errors**; 158 warnings (the baseline's 161, less the deleted `HallCityAuthoring`'s 3) |
| Offline runner | **passed 3723, failed 0** |
| Portal mask overlays, by eye | done (ring by ring, at 6x: holes, rails and bar, glass) |
| Everything below | **deferred: Unity** |

Run them in one batch in the NOPE-feat-clock editor (job runner), then the built QA player. The capture vocabulary is FIX_PLAN §0.

1. **deferred: Unity — import and compile.**
   - Refresh, so `HallPortalMasks.png` imports with its meta (linear, no mips, uncompressed RGBA32) and `HallPortals.asset` loads.
   - The shaders `NOPE/Hall Living City`, `NOPE/Hall City Traffic`, `TimeDesk/HelixRiver` and `TimeDesk/PortalGlow` compile with 0 errors and 0 warnings in the console. Integer ops in `CityHash` need target 3.5: the passes declare it.
   - Confirm `HallPortalsSO` shows in the Inspector with 5 rings.
2. **deferred: Unity — Build Office UI**, then the semantic dump of OfficeGameplay.unity against fix/p0-cuts's. The diff must hold only:
   - each `PortalEffect_0N`'s new `portals` reference (HallPortals.asset);
   - nothing else.

   Do not commit the scene: INTEGRATION rebuilds it.
3. **deferred: Unity — the EditMode suite.** All green except the known UnitySkills failure. It includes `HallFxSeamTests.SetWarning_IsRuntimeStateOnly`, and the carve's 12 NeedsEditor seam tests.
4. **deferred: Unity — the smoke play** (QA-RIG `QaJobs.Smoke`). 0 console lines new against fix/p0-cuts. In particular:
   - no `[AnimeHallPortalLink] ... carry no HallPortalsSO` after step 2;
   - no shader errors.
5. **deferred: Unity [BP] — city and hall flicker** (BUG-13).
   - Run `tools/qa/scenarios/hall_fx_city.json` (`QaJobs.Scenario`). It records the office and the city view at H1, H2, H3, 17:00 (forced, so frozen) and H5, at A1 and A2, and at H2 and H5 at A3 and A4. The 8 flying cars are tracked as the expected-motion mask.
   - Then run the same file on the built QA player: `python tools/qa/run_player.py tools/qa/scenarios/hall_fx_city.json`.
   - **Pass:** `seqtools flicker` on every sequence: zero pixels toggling more than twice in 10 s outside the cars, on the player.
   - Watch the GIFs at full size (`seqtools gif`).
   - **Headlights:** the luma variance of a headlight along its path is ≤10%. Track one headlight by eye on `city_a1_h5` with `region_luma --roi` on a short path box.
6. **deferred: Unity — traffic tint (CODE-69).**
   - `region_luma --compare <run>/seq/city_a1_h5 <run>/seq/city_a1_h2 --roi <a car's lane box> --max-ratio 0.5` → PASS.
   - The car's hue lies within the hall's night tint, and no car is brighter than the brightest lit window at H5. Check both on the `city_a1_h5` still by eye.
7. **deferred: Unity [BP] — the Helix monitor** (BUG-54, 56e).
   - Run `tools/qa/scenarios/hall_fx_helix.json`: before a drop, after a drop, the warning, the warning off. Then run it on the player.
   - **Pass:**
     - flicker on every sequence (the audit measured 3,268 px changing more than 100 times in 10 s);
     - in `helix_after_drop`, one glitch, then calm within 2 s: `region_luma --roi <the monitor> --from 2 --max-range 6`;
     - `helix_warning` shows the red banks and the warm glass, and is steady (`region_luma` range ≤6 from 1 s);
     - `helix_warning_off` is back to green;
     - the cheat line "just past the config warning value and back" (FIX_PLAN_v2) is the 62.5 and 66 steps, with firedAt 60 and critical 3.
   - Watch the GIF at full size.
8. **deferred: Unity — the AVAILABLE sign** (BUG-67).
   - Run `tools/qa/scenarios/hall_fx_sign.json`.
   - **Pass:**
     - `sign_invite` shows about 3 dips;
     - `sign_idle` has a luma range ≤6 (±3) over the sign: `region_luma <run>/seq/sign_idle --roi <the NEXT sign> --max-range 6`;
     - `sign_mid_case` is steady too;
     - flicker passes.
9. **deferred: Unity — the hall slot crossfade** (BUG-71).
   - Turn the cheat "Hall: tier" (or "culture") to force a slot change while recording 3 s (`recStart` / `cheat` / `recStop` in a scratch scenario, or by hand in the editor).
   - **Pass:** over a region both faces cover, the luma moves monotonically from the old art to the new (no dip toward the painting), within ±5% of the straight blend: `region_luma` per-frame values.
   - Repeat with a hit-stop (a citation) during the fade: it still completes, on unscaled time.
10. **deferred: Unity — portals** (BUG-26a).
    - Install `HallFxRig` and `hall_fx_portals.json` (above), then run it.
    - Then `python R7/AUDIT/VIS/scripts/portals.py <run folder> <out>`.
    - **Pass, per ring, at A1, A3 and A4 (scaled):**
      - the vortex's centre is within ≤2 px of the painted hole's;
      - the axes are within ±3%;
      - 0 halo pixels: effect pixels outside the painted hole. LIGHT's bloom at #11 adds glow around the ring; this count is taken before LIGHT's Volume exists.
    - Make 3x crops at H2 and H5 as GIFs from `portals_<a>_h12/h22`, reviewed by eye.
    - **Reduced Motion live (64c):** open the Settings window mid-swirl and turn Reduced Motion on. The swirl and sparkles stop that frame, and an opening iris finishes at once.
11. **deferred: Unity [BP] — allocations** (BUG-65l). QA-RIG's `perf_day12` run with the per-call-site GC log on the player. **Pass:** 0 B/frame from `HallCityExterior.Apply` and `HelixRiverMonitor.Update`.
12. **deferred: Unity — Saleh's notes 5, 13, 14 and 23** against his shots 6, 11 and 12, before and after, from steps 5 and 7.
13. **deferred: review** — the adversarial reviewer (definition of done #6) watches every GIF at full size, and the feel cards E8 and J4 are filled in `R7\FIX\FEEL\`.

## For INTEGRATION

### FEATURES.md (fold into the merge commit)
- **Line 31 (AVAILABLE sign):** "...the caption invites the press with a gentle pulse between the two inks (`readyInviteSeconds` 1.6 s a beat; steady under Reduced Motion..." becomes "...the caption invites the press: from the moment it starts inviting it pulses between the two inks for three beats (`readyInviteSeconds` 1.6 s each; `InvitePulse`, tested: `InvitePulseTests`), then holds lit and steady (BUG-67); lit and steady at once under Reduced Motion...".
- **Line 36 (portal rings):** replace the placement and look sentences with this.

  > Each vortex fills exactly the hole its ring's metal encloses, from the ring's painted-aperture mask (`HallPortalsSO`, `Assets/Data/Config/HallPortals.asset`, the one source of the rings: the drawing, each ring's layers, its opening fitted to its mask, its reach, the masks `HallPortalMasks.png` made from the painting by `tools/hall/portal_masks.py`; tested: `HallPortalsTests`, `PortalVortexTests`): the fence rails and the mullion in front keep their painted pixels, the part below the fence rail shows through the glass (`glassOpacity` 0.72), and nothing is drawn on the metal (the glow around the ring is the bloom on the core). The vortex is a layered cel swirl: a slow, dim back layer turning against the front's two hard tones, a light core with a white-hot heart, an ink line and a hard rim along the painted edge, and antialiased sparkles that fade out on a ring under about 20 px on screen.

  Also:
  - "the motion preference is read when it is shown and at each flare, never per frame" becomes "the motion preference is read live (BUG-64c)";
  - `DeskConfigSO.hallPortalVortex` / `hallPortalPulseSeconds` become `HallPortalsSO.vortex` / `pulseSeconds`.
- **Line 37 (hall lights):** "the city's windows lighting one by one ... each cell at its own moment, blinking as it catches" becomes "...each cell at its own moment (`HallCity.WindowHash`, never 0), fading in once over about 0.4 s at the clock's pace and holding still while the clock does (`HallCity.WindowLevel`, `NextFadeWidth`; tested: `HallCityTests`; BUG-13)".
- **Line 38 (hall slots):**
  - "Breaching from the warning line, where its glitch starts; Collapsing in the critical band, where it flickers" becomes "Breaching from the warning line; Collapsing in the critical band, where the monitor shows its warning";
  - "crossfades each changed slot to its art ... over `crossfadeSeconds` 0.6" gains "on unscaled time, the outgoing face opaque while the incoming one comes in, so the painting never shows through (`SlotCrossfade`, tested: `SlotCrossfadeTests`; BUG-71)".
- **Line 58 (city view):**
  - "headlights and rain" becomes "headlights";
  - "and the exterior's rain" is cut;
  - add "the headlights at least about 1.8 px across and faded at their lanes' ends in the hall window as in the view; the hall window's flying cars lit by the hour and faded at their lanes' ends (BUG-13, CODE-69)".
- **Line 202 (Helix River):** replace "and from the warning line ... the CRT glitches (band shifts, chroma split), flickering in the critical band (`stabilityCriticalMargin`); a loss today makes the motes race and the glass twitch on top of the damage" with this.

  > A drop of stability glitches the CRT once: bands slip and slide back, with a chroma split in its first frames, over `glitchSeconds` 0.2 s, then calm. In the critical band (`stabilityCriticalMargin` above the firing line: near the destruction point, D12) the monitor eases into a steady warning (red banks, a warm glass; `HelixRiverMonitor.SetWarning` holds it); a loss today makes the motes race. No detail crosses more than half a pixel a frame (the river slows on a small display). Nothing strobes (BUG-54, 56e; tested: `HelixRiverTests`).

  Also: "drops the flicker" becomes "drops the glitch".

### Cleanup list (the owners delete at merge; BUG-73)
| Item | Owner | Why |
|---|---|---|
| `DeskConfigSO.hallPortalDrawing`, `hallPortalLayers`, `hallPortalVortex`, `hallPortalPulseSeconds`, class `HallPortalLayers` (+ their `Desk_Default.asset` values) | DOC-HANDLE | No reader since HALL-FX: `HallPortalsSO` holds them. **Move** class `PortalVortexTuning` (`DeskConfigSO.cs`) into `HallPortalsSO.cs` as a pure move (HALL-FX's file); `HallPortalsSO.vortex` uses it. Its `ringGlow` doc becomes "how far the core's heart lifts past 1 (the bloom that lights the ring)". QA-RIG's `so_reader_allowlist.txt` lists the four fields until then |
| `AnimeHallPortalLink.Configure`'s `config` argument; `BuildPortalEffects`'s `config` argument | DESK-PLAN (OfficeSceneBinder.cs, .Desk.cs callers), then HALL-FX drops the parameters | Not read |
| `CityView.cs`: the `RainId` write (`_exterior.rain`) | CAM | The shader has no rain; then HALL-FX deletes `HallCityExterior.rain` |
| `HallCityCrowdVariationsAuthoring.cs`, `HallLightingPreviewWindow.cs` (`city.rain`), `HallDeepRoomAuthoring.cs:212`, `HallFloorShadowGeometry.cs:37`, `AnimeHallLightingHooks.cs:463-470` (ring coordinates) | Unowned Terminal Art tools: the orchestrator assigns them | They read `rain`, or hold their own ring coordinates. They should read `HallPortals.asset`'s openings, or be retired as stale installers (CODE-136) |
| `docs/SCENE_CONTRACT_GAMEPLAY.md:178-180`, `docs/ART_ASSET_LIST.md:58,75` | INTEGRATION | They name `DeskConfigSO.hallPortalLayers` and the old glow placement |

### Knob request (DOC-HANDLE, `DeskConfigSO`)
- `readyInviteCycles` (int, 3): would replace the `InvitePulse.Cycles` constant. BELL-SIGN (Phase B) may take it with the sign.

### Art request (GPT mailbox, filed by the orchestrator: HALL-FX cannot push)
1. `AnimeHall.unity`:
   - remove the `HallDeepPortalRegistration` component (dead: it never finds a sprite);
   - delete `Completion/DeepRoom/PortalGlowClipped.mat`.

   Then HALL-FX deletes `HallDeepPortalRegistration.cs` and `Assets/Shaders/HallDeepPortalGlow.shader`.
2. `Completion/City/TimeWeather/LivingLeft.mat` and `LivingFront.mat`: clear the four `_*Rain` texture slots, and delete `City/TimeWeather/City*Rain.png`. No shader reads them, but a material still referencing them can drag them into the build.
3. The retired OfficeScene's `DebtRelief/Materials/Portal_Energy.mat` and `Hybrid/Materials/PortalFlow.mat` use the old `OfficePortal.shader`. Once OfficeScene is retired from the build list, delete all three.

### Leases (PHASEB/LEASES.md)
- `AvailableSignLink.cs` → BELL-SIGN.
- `HelixRiverMonitor.cs`, `Domain/HelixRiver.cs`, `HelixRiver.shader` → TIMELINE-FAIL.
- `AnimeHallPortalLink.cs`, `PortalEffect.cs`, `PortalVortex.cs`, `OfficeSceneUIBuilder.Portals.cs` → PORTALS. `HallPortals.asset` is read-only for PORTALS.

New HALL-FX files PORTALS will read: `HallPortalsSO.cs`, `HallPortals.asset`, `HallPortalMasks.png`, `tools/hall/portal_masks.py`.

## Decisions (ours, for the adversarial reviewer)
1. **The warning line is the critical band, not the warning band** (see 56e). The values are CONTENT's to retune.
2. **3 invite cycles**, held in a constant until DOC-HANDLE adds the knob.
3. **Arches end at the fence rail.** An arch (rings 4 and 5) has no painted lower half, so its hole ends at the fence's top rail; the full rings continue behind the glass. The vortex's swirl centres on the hole's centroid.
4. **Occluders are what spans the hole** (rails, the mullion). Dark details on the wall behind (the pipe in ring 5) are covered by the vortex, as they are behind the opening.
5. **The speed cap slows the whole river** on a small display, rather than dropping detail. The desk monitor runs at about half speed: calmer, which J4 asks for. The tray keeps its speed and loses its rungs, which fade out when they are packed under 2 px apart.
6. **The crossfade does not depend on draw order.** The two faces share a sorting order: rather than impose one (the crowds sit at 61, the next order up), the two-phase schedule is correct either way.
7. **`HallCityExterior.rain` stays as an inert field** until its four readers outside HALL-FX drop it. Deleting it would break CAM's file and three unowned tools.
8. **`HallCity.WindowHash` and `WindowLevel` are the shader's rules in C#,** called by the tests only. The windows are drawn per pixel by `NOPE/Hall Living City`, which no EditMode test can run. They are its reference implementation, and the shader's comments name them, so a change to one shows in the other's review. Every other new public member has a game caller.
