# HALL-FX: nothing in the world flickers

Branch `fix/a-hall-fx`, cut from `fix/p0-cuts` (5aecb60). Phase A merge #4. Sources: FIX_PLAN.md §3 "HALL-FX" with FIX_PLAN_v2.md "HALL-FX: delta" (v2 wins), BUGLIST.md, DECISIONS_1019a.md, GAME_FEEL_BIBLE.md rows E8 and J4, PHASEB/LEASES.md.

**Goal (player):** the city, hall, monitor and signs are calm. Windows fade on once, headlights glide, the Helix monitor moves smoothly and glitches once on a drop, the AVAILABLE sign pulses only when it wants a click, and the portals fill their painted rings exactly (notes 5, 13, 14, 23, and the portal note).

**Code only.** No Unity editor or player was run (Saleh may be on the PC). Every check that needs Unity is listed under RESULT as "deferred: Unity", with its recipe, so the Unity pass can run them in one batch.

## Saleh's calls this track builds (quoted, DECISIONS_1019a.md)

- **D12 Firing:** "you are fired if you destroy the timeline, sometime we will have random events and the timeline will be more than 70 corrupted through no fault of the player". FIX_PLAN_v2's 56e delta: `SetWarning` "shows near the **destruction point**, read from CONTENT's config"; 70% is not a firing warning.
- **D20 Pause:** "you should be able to close the booth with the available sign if you want to pause". That is BELL-SIGN's (Phase B, `AvailableSignLink.cs` is leased to it after #4). HALL-FX only stops the pulse looping (BUG-67, FIX_PLAN_v2 1b: "67 AVAILABLE pulse: FIX (HALL-FX). BELL-SIGN later adds the sign's pause state").
- **[S] J4 (bible):** "An animated timeline with **no number**; calm, readable motion". No number was added anywhere.

Everything else below that is a design choice is marked **proposal (ours)**.

## The verifier's findings, and where each is fixed
| # | Finding | Fix (section) |
|---|---|---|
| 1 | BUG-67: the invite beats ran out behind the morning briefing | The beats start on the first frame the sign can be pressed (`InvitePulse.Since`, `AvailableSignLink.Pressable`); 2 new tests; `hall_fx_sign.json` re-timed (BUG-67; RESULT 8; decision 10) |
| 2 | BUG-26a: rings 4/5 openings disagreed with the audit's painted holes | Arches' holes go on through the glass pane to the lower rail; refitted (centres now at canvas y 218 and 214); the pass's hole definition is stated and measured by the new `portal_check.py` (BUG-26a; RESULT 10; decision 3) |
| 3 | BUG-13: the city scenario never ran a clock | New `hall_fx_dusk.json` on the running shift clock across dusk at A1 and A2 (`hallClock` rig op, `dusk_check.py`); step 5 text corrected (RESULT 5) |
| 4 | BUG-13: dust and crowd fades not covered | Dust split to LIGHT (split register); HALL-FX runs hide it ("Hall FX: dust"); crowd fades judged by the flicker rule (BUG-13; RESULT 5; decision 11) |
| 5 | PortalGlow drew the iris rim at full open | Faded out as the iris opens (BUG-26a; RESULT 10; decision 12) |
| 6 | Inexact Unity recipes | Every region is a tracked object (`region_luma --track`); new `hall_fx_crossfade.json`; tray strip and Home HUD Helix captures in `hall_fx_helix.json`; decisions renumbered (RESULT 6-9) |
| 7 | Dead `HallCity.Frac`; stale binder comment | `Frac` deleted; the binder's comment is on the cleanup list (DESK-PLAN) |

## What changed, per BUG id

### BUG-13 City windows strobe, headlights twinkle, daylight cars at night (HALL-FX's whole cluster)
- **The window hash never returns 0, and no crowd of cells shares a moment.** The old float hash was exactly 0 on 600 of the 28,800 cells (they blinked at 6 Hz all day) and gave up to 324 cells one moment. It is now an integer Wang hash (`HallCity.WindowHash` in C#, `CityHash` in `HallLivingCity.shader`, bit for bit), mapped into [1/256, 1). Exhaustive test over the grid.
- **The catch-up blink is gone.** Each window fades in once: its light is `saturate((lit - hash x 0.97) / fadeBand)` (`HallCity.WindowLevel`), a function of the lit share alone. The lit share is `_HallCityLights.x`, which LIGHT's `HallLightingRig` computes from the shift clock's hour (never `Time.time`), so a frozen clock or a forced hour freezes every window. The add is gated on `lit > 0`.
- **The fade takes about 0.4 s at the clock's pace.** `HallCityExterior` measures how fast the lit share moves and sizes the fade band so a window crosses it in `HallCity.FadeSeconds` (0.4 s; FIX_PLAN: 0.3-0.5 s). The band is clamped to [0.0005, 0.05], eases over 1 s when the pace changes, and holds still while the share holds, so nothing moves on a frozen clock. It is the global `_CityWindowFade`, so the city view's copy of the material reads it too.
- **Headlights are at least about 2.7 px across.** The Gaussian is widened by the pixel's size (`fwidth(uv)`, at least 1.35 screen px each side; 0.9 until the integration's Unity pass, where daylight headlights still toggled a few city-view pixels three times in 10 s), and dimmed a little as it widens (`pow(area ratio, 0.35)`) so a widened light keeps about its weight.
- **They fade at the lane ends** in the hall window as in the city view (before, only the city view faded).
- **Clouds, the airship and the bus cross from off one edge to off the other** in the hall window too. Before, `Fly` kept the raw `frac` in the hall, and the second cloud wrapped with a plain `frac`, so they popped in view.
- **The airship and the bus** (the Living City shader's own small flyers, drawn from the unmipped atmosphere atlas about ten times smaller than its cell) average a 4 x 4 grid over each pixel's footprint (`FxFine`): with one sample a pixel the bus's windows and outline sparkled as it moved and failed every city sequence at 1280 px wide (integration finding).
- **The flying traffic sprites** (`HallCityTraffic.shader`) take four rotated-grid samples over each pixel's footprint (sub-pixel smoothing for the unmipped art), and fade their outermost screen pixel (the margin), so their quad's edge never crawls.
- **The traffic is tinted by the hour (CODE-69).** Each car's colour is `HallCity.HourLight` of the hall lights' hour: the same four-state light the city painting is lit by, and the light CityView already gave its own traffic. It also fades at its lane's ends (`HallCity.LaneEndFade`, 6% of the lane), so the lane's wrap never pops it.
- **The dust motes are LIGHT's (split, filed to LIGHT at #11).** BUG-13 also lists "flyers and dust shimmer" (VIS-9: motes 1-2 px across, worse at night). The flyers are fixed here (above). The dust lives in `HallLightingRig`'s ParticleSystems (`dust`, `ApplyDust`), which LIGHT owns, so HALL-FX leaves it: see the split register under For INTEGRATION.
  - HALL-FX's flicker sequences run with the dust hidden, through a new dev cheat, "Hall FX: dust" (`Assets/Scripts/DevTools/HallFxCheats.cs`, DevCheats.Register; it disables the dust's particle renderers under every `HallLightingRig` and touches nothing the rig drives). So HALL-FX's gate never waves the dust through, and the dust never fails HALL-FX's gate; LIGHT measures it with the dust shown.
  - `HallWhiteCrowds`' fades need no mask: each group fades in over 4 s once per 80 s cycle and holds 38 s (`Fade`), so a pixel crosses at most one fade in and one fade out in 10 s, which the flicker rule ("more than twice") already allows.
- **Dead code:** `HallCity.Frac` (private, no caller) is deleted.
- **Files:** `Assets/Shaders/HallLivingCity.shader`, `HallCityTraffic.shader`, `Assets/Scripts/Office/HallCityExterior.cs`, new `Assets/Scripts/Visuals/HallCity.cs`, new `Assets/Scripts/DevTools/HallFxCheats.cs` (the dust cheat, QA only).

### BUG-54 Helix River monitor strobes (and BUG-56e, the warning state)
- **One readable glitch on a drop, then calm.** The glitch used to grow with the warning band and with the day's losses. The day's-loss part never decayed (the "gain per correct call is 0" root cause), so after one mistake the glass tore at 10 Hz all shift. Now any fall of the hidden stability starts one burst: `glitchWild` x (1 - u)^2 over `glitchSeconds` 0.2 s (bible J4: "1 burst of 200 ms per drop"), then 0.
  - The bands that slip are chosen by the burst's number (`GlitchSeed`), so they kink once and slide back as it fades. Before, they were re-rolled at 10 Hz. A band slips at most 8 reference px (16 until the integration's Unity pass: sliding back over the rungs, 16 toggled a desk-river pixel three times in the burst).
  - The chroma split shows only while the burst is over half strength: about 2 frames at 30 fps (bible J4: "a 2-frame RGB split on the monitor only").
  - No glitch under Reduced Motion.
  - The knob `agitationGlitch` is gone. The day's losses still speed the motes (`agitationFlow`).
- **The 15 Hz critical-band flicker is gone.** `HelixRiverFrame.Flicker` and the shader's `_Flicker` are deleted.
- **Speed cap: no detail crosses more than 0.4 px a frame at 30 fps** (FIX_PLAN: "capped to ≤0.5 px per frame at the RT scale"; the knob was 0.5 until the integration's Unity pass: at 0.5 the rungs' passing still toggled a few desk-river pixels three times in 10 s, so it is 0.4, inside the plan's bound).
  - The river steps by the capture's frame while frames are captured at a fixed rate (`UiMotion.Delta`, as the UI's motion does), else by real time: a recording ran it on real time, about 45 ms a captured frame, so it moved 1.3-1.5 times the cap a frame and strobed under the flicker check (integration finding).
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
  - New knobs: `glitchSeconds` 0.2, `warningSettleSeconds` 0.8, `warningHysteresis` 0.5, `maxPixelsPerFrame` 0.4 (0.5 until the integration's Unity pass), `captureFps` 30.
  - `agitationGlitch` is removed. `glitchWild` now means the burst's peak.

### BUG-67 The AVAILABLE sign pulses forever
- From the first frame the sign can be pressed, it pulses `InvitePulse.Cycles` beats (`DeskConfigSO.readyInviteSeconds` 1.6 s each), then holds lit and steady. Each beat is a cosine dip from the lit ink toward the paused ink and back, so the pulse starts and ends lit with no jump. The component stops running when it settles. Under Reduced Motion it is lit and steady at once.
- **The beats run on the invitation's own clock** (real time, or the capture's frame while frames are captured at a fixed rate: `UiMotion.Delta`), so a recording shows them at their pace. On `Time.unscaledTime` a capture (about 45-100 ms of real time a frame) squeezed the 3 beats into the first 1.6 s of `sign_invite` (integration finding).
- **The beats wait for the briefing (verifier finding 1).** The binder adds and configures the link at office load, while the morning briefing ("The Temporal Times") still covers the office. The first version started the beats there, so all 4.8 s of them ran out behind the briefing and the player never saw them. Now:
  - `InvitePulse.Since` is the start rule: NaN while the sign does not invite; the first moment it invites **and can be pressed**; held from then on, and cleared when the first press (or closing) ends the invitation.
  - `AvailableSignLink` never starts the beats at `Configure` or `Apply`. Its `Update` polls `Pressable` each frame until they start: the gameplay layer's `BoothCoordinator.Phase` is not `Newsletter`. GameManager sets `Newsletter` before it shows the briefing and `NoTraveller` in `BeginShift` (Start Shift), the same call that makes the sign's `Clickable` interactable. With no briefing panel, `BeginShift` runs at once, so the beats start on the first frame. The sign holds lit and steady while it waits.
  - Reading `BoothCoordinator.Phase` is read-only use of DESK-PLAN's public property; no other track's file changed. The binder's call is unchanged.
- **Proposal (ours):** 3 cycles (FIX_PLAN: "2-3"). It is a constant in `InvitePulse`, because `DeskConfigSO` is DOC-HANDLE's: see the knob request below.
- BELL-SIGN (Phase B, lease after #4) adds the sign's pause state (D20).
- **Files:** `Assets/Scripts/Office/AvailableSignLink.cs`, new `Assets/Scripts/Visuals/InvitePulse.cs`.
- **The scenario is re-timed.** `hall_fx_sign.json` forces the hour before Start Shift, so `sign_invite` starts on the frame after the briefing closes and holds all 3 beats (6 s). It tracks the sign's click box (`ReadySign`) as its region, records 3 s under the briefing (`sign_steady_briefing`), and names the steady sequences `sign_steady_*`: the flicker gate reads those, not the invitation, whose 3 beats are motion meant to be seen.

### BUG-71 The hall slot crossfade shows the painting through
- **Unscaled time.** It was scaled time, so a hit-stop froze it.
- **Two phases** (`SlotCrossfade`). The incoming face fades in over the first half while the outgoing face stays opaque, then the outgoing face fades out over the second half under the opaque incoming one. Wherever both faces cover, one of them is opaque at every moment, so the painting never shows through. This holds whichever face draws on top: both share the slot's sorting order, and their order is not otherwise defined. Before, Fade / 1 - Fade showed 25% of the painting at mid-fade.
- **Files:** `Assets/Scripts/Office/HallSlotsLink.cs`, new `Assets/Scripts/Visuals/SlotCrossfade.cs`.

### BUG-26a Portals: masks, openings, occlusion, the probe (26b, the bloom, is LIGHT's at #11)
- **Per-ring painted-aperture masks, generated from the painting, not a colour test at run time.** `tools/hall/portal_masks.py` reads the hall drawing (`HallWarmStone.png`, the layer "62 Approved deeper architecture", read only) and writes one RGBA crop per ring into `Assets/Data/Config/HallPortalMasks.png` (linear, no mips, uncompressed). How it finds each hole:
  - the wall and glass seen through it are bright, or the warm unsaturated wall in an arch's shade, never brass or the ring's cool grey;
  - the hole is the convex hull of those pixels' components that meet the ring's seed ellipse;
  - an arch's hole (rings 4 and 5) goes on below the bay fence's top rail, through the glass pane under the arch, down to the fence's lower rail, as a full ring's goes on behind its glass (verifier finding 2: the first version ended it at the top rail, a third of the painted height and 27-31 canvas px too high).

  The four channels:
  - **R, the hole:** antialiased.
  - **G, what stands in front:** rows across the hole that are mostly not open (the fence rails), and columns that are (the mullion bar; on an arch, counted below the top rail only: the fence's posts in front of the glass). On an arch, the fence's brass (rails, posts, the ball on ring 5's post) is in front too. Dark things that do not span the hole, such as a pipe on the wall behind ring 5 above its rail, are behind it, so the vortex covers them.
  - **B, behind the glass:** the part below the fence rail.
  - **A, the edge depth:** 6 canvas px deep, for the rim.
- **The openings come from mask fits.** Each ring's opening in `HallPortals.asset` is the R channel's moment fit: centre, and radii of twice the standard deviations. Its `reach` is the farthest hole pixel in opening radii, so the iris uncovers it all. The old hand measures were 9% too tall on ring 1 and far off on rings 4 and 5. The tool's overlays were checked by eye, ring by ring.

  | Portal | Opening (centre, radii; canvas px) | Old hand measure |
  |---|---|---|
  | 1 | (1094.2, 486.6), 53.3 x 47.9 | (1096, 488), 49 x 56 |
  | 2 | (969.4, 365.2), 23.3 x 18.9 | (968, 364), 25 x 23 |
  | 3 | (1210.4, 366.5), 23.4 x 20.9 | (1210, 363), 25 x 23 |
  | 4 | (1766.3, 217.7), 22.1 x 38.3 | (1762, 213), 16 x 41 |
  | 5 | (2004.6, 214.2), 34.6 x 46.2 | (2003, 213), 28 x 50 |

  Rings 4 and 5 now sit where the audit measured their painted holes (VIS-2, `portal_report.txt` at 1080 day: centres about canvas y 219-220, 6-7 screen px below the old 213): their centres' y are within 1.2 and 5.8 canvas px of it. Their heights stay under the audit's figure, because the audit's ray rule counts more than the aperture: see "The probe" below.
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
  - The iris's own ink line and rim band (at 0.88 and past 0.9 of the iris) show only while the iris springs open: they fade out as `_Life.x` goes from 0.85 to 1 (verifier finding 5). Before, they drew on every frame, so a full-open arch had bright rim patches in its lower corners at the fence rail (reach about 1.2), and ring 1 a second ink line just inside the painted rim. Shader-only: checked by eye at RESULT step 10.
  - Every hard edge is antialiased on its unwrapped coordinate: the arms' bands use the periodic distance, so there is no seam where the angle wraps, and the AA width is analytic, not `fwidth(frac())`.
  - The sparkles are antialiased and fade out when the opening is under about 20 px on screen (they shimmered on the rear rings).
- **BUG-64c.** `PortalEffect` reads Reduced Motion live every frame. Before, it was cached at Show and at each flare. Turning it on stills an open vortex at once and finishes its iris.
- **The probe.** The circular config-against-config check is replaced by:
  - a data test: `HallPortalsTests` re-fits the PNG's pixels in C#;
  - an effect-on vs effect-off capture: `HallFxRig`'s `hallPortalsShot`, read by the new `tools/hall/portal_check.py`; see RESULT step 10.
- **Which hole the pass measures (decision 3, revised).** The pass compares the vortex with the **painted aperture**: the ring's mask laid on the off frame where the painting really is (the ring's crop of the drawing registered by cross-correlation, never trusting the game's own placement). It does not use the audit's `portals.py` ray rule as the pass, because that rule's "painted hole" ends at the first run of 3 px under luma 105, which counts lit metal as hole: a ring's inner lower lip, seen from above, and the arches' shaded lower panel. Run offline on the painting itself from the fitted openings, the aperture's height is this share of the ray rule's: ring 1 0.96, ring 2 0.84, ring 3 0.91, ring 4 0.89, ring 5 0.91 (and ring 5's width 0.90). So the FIX_PLAN numbers (centre within 2 px, axes within 3 %) applied to the ray rule would fail rings 2-5 whatever the masks, and would pass only a vortex painted over metal, which BUG-26a forbids. `portal_check.py` prints the ray rule beside each ring as a reference column. **Proposal (ours); a director call may overrule it** (then step 10 says which).
- **`HallFxRig`'s shots** are now named `h<hour>_<width>x<height>_on|off.png`: A1 (1920x1080) and A3 (2560x1080) are both 1080 tall, so the old `h<hour>_<height>` names overwrote A1's with A3's.
- `PortalVortex.TextureQuad` and `FenceV` are deleted (no caller). `PortalVortex.MaskMap` is new.
- **Files:** `Assets/Art/Office/Gameplay/Shaders/PortalGlow.shader`, `Assets/Scripts/Office/PortalEffect.cs`, `AnimeHallPortalLink.cs`, `Assets/Scripts/Visuals/PortalVortex.cs`, `Assets/Editor/OfficeSceneUIBuilder.Portals.cs`, new `HallPortalsSO.cs`, `HallPortals.asset`, `HallPortalMasks.png`, `tools/hall/portal_masks.py`, `tools/hall/portal_check.py`.

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

The baseline (fix/p0-cuts) was passed 3710, failed 0, with compile_check at 0 errors and 161 warnings. After this branch it is **passed 3725, failed 0** and **0 errors** in every project (3723 before the verifier fixes, plus the 2 new `InvitePulseTests`).

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
| `InvitePulseTests` (4) | 3 beats, starts and ends lit, then steady lit; steady under Reduced Motion (BUG-67) |
| `InvitePulseTests.TheBeats_StartWhenTheSignCanBePressed_NotBehindTheBriefing` | A 30 fps run, frame by frame as the link polls it: under the briefing (0-20 s) the beats wait and the sign holds lit; they start on the first frame it can be pressed; all 3 dips come after Start Shift, the first half a beat in; then steady (verifier finding 1) |
| `InvitePulseTests.TheInvitation_HoldsOnceStarted_AndRestartsOnlyAfterItStopsInviting` | `InvitePulse.Since`: held once started (a briefing up again never restarts it), cleared by the first press or closing |
| `SlotCrossfadeTests.ThePainting_NeverShowsThrough_WhereBothFacesCover` | Either draw order; each face moves one way (BUG-71) |
| `HallPortalsTests` (4) | The asset's openings match a C# re-fit of the mask PNG (a data test, not a self-comparison); holes lie inside their crops and the quad; occluders and glass lie on the hole; crops apart; the atlas wired, linear, without mips |
| `PortalVortexTests.MaskMap_PutsTheQuadOnItsRingsCropOfTheAtlas` | The quad's uv to the atlas, at the centre and both corners. `TextureQuad` and `FenceV` tests are deleted with the code |
| `Assets/Editor/SeamTests/HallFxSeamTests.SetWarning_IsRuntimeStateOnly` (NeedsEditor) | The seam holds the warning without touching serialized state |

**Waiting for QA-RIG (#2; not on this branch's base). Installed at the integration merge (integrate/a):**
- The DemoGate checks, installed as `Assets/Editor/QA/Checks/HallFxChecks.cs`: `hallfx.city.flicker`, `hallfx.dusk.flicker`, `hallfx.helix.flicker`, `hallfx.sign.flicker` (the `sign_steady_*` sequences), `hallfx.crossfade.flicker`, `hallfx.bp.city.flicker`, `hallfx.bp.dusk.flicker`, `hallfx.bp.helix.flicker` and the static `hallfx.portals.source`.
- The rig ops, installed as `Assets/Tests/PlayMode/HALL-FX/HallFxRig.cs`: `hallPortals`, `hallPortalsShot` and `hallClock` (moves the running shift clock forward to an hour and adds the `hallHour` and `cityLit` columns to every captured frame).
- `hall_fx_portals.json` and `hall_fx_dusk.json`: installed in `tools/qa/scenarios/` with the rig.
- The scenarios `tools/qa/scenarios/hall_fx_city.json`, `hall_fx_helix.json`, `hall_fx_sign.json` and `hall_fx_crossfade.json` use only QA-RIG's built-in ops (and registered cheats through `cheat` `Extra`), so they are on this branch. Every HALL-FX scenario hides LIGHT's dust for its run ("Hall FX: dust") and shows it again at the end.
- **Tools** (all offline, Python, reading QA-RIG run folders; self-tested on synthetic sequences in the scratchpad):
  - `tools/hall/region_luma.py`: a region's luma over a sequence. The region is a fixed `--roi` in screen fractions, or `--track NAME[,NAME]`, the rects the scenario tracked (frames.tsv), so no recipe needs a hand-drawn box. Modes: steady range (`--max-range`), `--dips N` (the sign's invitation), `--monotonic` (the crossfade), `--compare` (the traffic tint; `--stat p90` reads a car's lit body, not the sky in its rect).
  - `tools/hall/portal_check.py`: the portal pass (step 10).
  - `tools/hall/dusk_check.py`: the dusk sequences ran on a running clock with the lit share rising (step 5).

## RESULT

| Step | Status |
|---|---|
| compile_check_any | **0 errors**; 158 warnings (the baseline's 161, less the deleted `HallCityAuthoring`'s 3) |
| Offline runner | **passed 3725, failed 0** |
| Portal mask overlays, by eye | done (ring by ring, at 6x: holes, rails and bar, glass); rings 4 and 5 redone with the glass pane, the posts and the brass |
| `portal_check.py`, `region_luma.py`, `dusk_check.py` | self-tested offline on synthetic run folders: a ring's effect misplaced by 4 px fails (72 halo px), the others pass; a crossfade with a dip fails `--monotonic`; a 3-beat pulse passes `--dips 3`. `dusk_check.py` is not exercised until a dusk run exists |
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
   - **Frozen clock:** run `tools/qa/scenarios/hall_fx_city.json` (`QaJobs.Scenario`). Every hour in it is forced (the cheat's `DevToolsState.ForcedHour`): 9 (H1), 12 (H2), 16.75 (H3), 17 and 22 (H5), for the office and the city view at A1 and A2, and at H2 and H5 at A3 and A4. A forced hour freezes the lit share, so these show that nothing toggles while the clock holds.
   - **Running clock (the BUG-13 symptom: windows fading in while recorded):** install `HallFxRig` and `docs/pending/HALL-FX/hall_fx_dusk.json` (see Tests), then run it. It clears any forced hour, moves the shift clock to 16:10 (`hallClock`) and lets it run at its own pace (1 game minute a second) through dusk: `dusk_office_a1` (about 16:10-16:20), `dusk_office_a2`, `dusk_city_a2`, `dusk_city_a1` (about 16:47-16:57), all before closing time. Worked out offline, the lit share rises from about 0.05 to 0.23 in the first and from 0.78 to 0.93 in the last.
     - `python tools/hall/dusk_check.py <run folder>` → PASS: in every `dusk_*` sequence the hour never runs back and the lit share (`cityLit`) rises by at least 0.05, so windows faded in during the capture.
   - Then run both files on the built QA player: `python tools/qa/run_player.py tools/qa/scenarios/hall_fx_city.json`, and the same for `hall_fx_dusk.json`.
   - **Pass:** `seqtools flicker` on every sequence of both: zero pixels toggling more than twice in 10 s outside the expected-motion mask, on the player. On the running clock each window crosses its 0.4 s fade once, so it toggles at most once.
   - **What is masked, and what is judged:**
     - the 8 flying cars are tracked (`car0` to `car7`): the mask;
     - LIGHT's dust is hidden for every HALL-FX run (cheat "Hall FX: dust"): LIGHT measures it with the dust shown (split register below), so this gate neither fails on it nor waves it through;
     - `HallWhiteCrowds`' fades are not masked: a group fades in over 4 s once per 80 s and holds 38 s, so a pixel crosses at most one fade in and one fade out in 10 s, which the rule allows. A crowd pixel toggling more than twice is a fail.
   - Watch the GIFs at full size (`seqtools gif`).
   - **Headlights:** the luma variance of a headlight along its path is ≤10%. The city's headlights are drawn by the shader, with no object to track: judge them by eye on `city_a1_h5` and `dusk_city_a1` at full size (no twinkle on any lane).
6. **deferred: Unity — traffic tint (CODE-69).**
   - `python tools/hall/region_luma.py --compare <run>/seq/city_a1_h5 <run>/seq/city_a1_h2 --track car0,car1,car2,car3,car4,car5,car6,car7 --stat p90 --max-ratio 0.5` → PASS (from `hall_fx_city`; each car's own rect, the mean over the cars on screen).
   - The car's hue lies within the hall's night tint, and no car is brighter than the brightest lit window at H5. Check both on the `city_a1_h5` still by eye.
7. **deferred: Unity [BP] — the Helix monitor** (BUG-54, 56e).
   - Run `tools/qa/scenarios/hall_fx_helix.json`. It records the desk monitor (tracked `desk_river`: the `DeskRiver` quad) before a drop, after a drop, in the warning and with the warning off. Then it records the PC's tray strip river (tracked `tray_river`: `TimelineStrip`; the PC is opened with Q) through a drop. After the shift, it records the Home HUD's river (tracked `home_river`: Home's `StabilitySlot`). Then run it on the player.
   - **Pass:**
     - flicker on every sequence (the audit measured 3,268 px changing more than 100 times in 10 s);
     - `helix_after_drop`: one glitch, then calm within 2 s: `region_luma <run>/seq/helix_after_drop --track desk_river --from 2 --max-range 6`;
     - `helix_warning` shows the red banks and the warm glass, and is steady: `region_luma <run>/seq/helix_warning --track desk_river --from 1 --max-range 6`;
     - `helix_warning_off` is back to green (by eye on its still);
     - `helix_tray_drop` (the drop comes at 2 s): calm 2 s after it, `region_luma <run>/seq/helix_tray_drop --track tray_river --from 4.5 --max-range 6`;
     - `helix_home`: steady, `region_luma <run>/seq/helix_home --track home_river --from 1 --max-range 6`;
     - the cheat line "just past the config warning value and back" (FIX_PLAN_v2) is the 62.5 and 66 steps, with firedAt 60 and critical 3.
   - Watch the GIFs at full size.
8. **deferred: Unity — the AVAILABLE sign** (BUG-67).
   - Run `tools/qa/scenarios/hall_fx_sign.json`. The hour is forced before Start Shift, so `sign_invite` starts on the frame after the briefing closes.
   - **Pass:**
     - `region_luma <run>/seq/sign_invite --track sign --dips 3` → PASS: exactly 3 dips, the first about 0.8 s in, so none ran out behind the briefing. On day 1 the tutorial's arrow bobs over the top of the `ReadySign` click box and splits the first dip in two, and the click box dilutes each dip to about 8-9 luma; the integration measured the caption itself, `--roi 0.458,0.644,0.542,0.689` (A1), which gives the 3 dips at the default depth;
     - `region_luma <run>/seq/sign_steady_idle --track sign --max-range 6` → PASS (±3);
     - `region_luma <run>/seq/sign_steady_mid_case --track sign --max-range 6` → PASS (it starts 4 s after the traveller arrives: the integration found the speech bubble re-centring its text with each typed letter, FEEDBACK's, which failed the flicker check on the bubble);
     - flicker passes on the `sign_steady_*` sequences (`hallfx.sign.flicker`; the invitation's beats are meant to be seen).
   - `sign` is the tracked `ReadySign` click box, which the binder lays over the art's NEXT sign.
9. **deferred: Unity — the hall slot crossfade** (BUG-71).
   - Run `tools/qa/scenarios/hall_fx_crossfade.json`. The cheat "Hall: culture" (`cheat` `Extra`) steps the culture to neutral, then to the first nation (the flags take their art). The scenario then records `crossfade_art_to_art` over the next step (art to art: both faces cover). Finally it records `crossfade_hitstop` over the step after, with the game's hit-stop on top (a hit-stop during the fade: HallFxRig's `hallHitStop`, MotionKnobs' scale and seconds as a citation's punch, without the punch's camera shake; the integration replaced "Feel: citation", whose shake, FEEDBACK's and CAM's, toggled 491 px three times and failed this sequence's flicker on their motion).
   - **Pass:**
     - `region_luma <run>/seq/crossfade_art_to_art --track flag --monotonic` → PASS: the left flag's luma goes from the old art's level to the new one's and never turns back by more than 5 % of that span (no dip toward the painting). Run it with `--track board` (the departure-board frame) too, where its art changes;
     - `region_luma <run>/seq/crossfade_hitstop --track flag --monotonic` → PASS, and it reaches the new level inside the 2.5 s (unscaled time: the hit-stop never freezes it);
     - flicker passes (`hallfx.crossfade.flicker`).
   - If the culture step changes no flag on this seed (a nation with no flag art), the still pair `crossfade_before` / `crossfade_after` shows it: then add one more `Hall: culture` step before `recStart`.
10. **deferred: Unity — portals** (BUG-26a).
    - Install `HallFxRig` and `hall_fx_portals.json` (see Tests), then run it. Its shots are `shots/portals/h<hour>_<w>x<h>_on|off.png`. The integration's first run measured one ring only, half hidden: day 1 opens ring 1 alone, the tutorial's hint plate covered its upper half and the traveller its lower half. The scenario now skips the tutorial (`cheat Tutorial 0`), opens every ring (`hallPortalsOpen`) and shoots before the AVAILABLE sign is pressed (no traveller at the counter). `portal_check.py` reports a ring whose crop registers outside the frame (ring 5 at 1280 x 1024, at the screen's edge) as a FAIL to look at, instead of crashing.
    - Then `python tools/hall/portal_check.py <run folder>` (writes `<run>/portal_check/portal_check.txt` and overlays).
    - **The hole it measures:** the ring's painted aperture (its mask), laid where the painting really is on the off frame. The ring's crop of the drawing is registered by cross-correlation (`match` in the report; under 0.6, look by eye), so the game's own placement is never taken on trust. The audit's `portals.py` ray rule is printed as a reference column and is not the pass: it counts lit metal and the arches' lower panel as hole (see BUG-26a and decision 3).
    - **Pass, per ring, at A1, A3 and A4 (scaled):**
      - the vortex's centre is within ≤2 px of the hole's;
      - the axes (2 standard deviations) are within ±3 %;
      - 0 halo pixels: effect pixels outside the aperture grown by 2 px. LIGHT's bloom at #11 adds glow around the ring; this count is taken before LIGHT's Volume exists.
    - Make 3x crops at H2 and H5 as GIFs from `portals_<a>_h12/h22`, reviewed by eye. With the iris fully open there is no ink line or bright band inside the painted rim (ring 1), and no bright patch in an arch's lower corners at the fence rail (rings 4, 5): the iris's own rim shows only while it springs open.
    - **Reduced Motion live (64c):** open the Settings window mid-swirl and turn Reduced Motion on. The swirl and sparkles stop that frame, and an opening iris finishes at once.
11. **deferred: Unity [BP] — allocations** (BUG-65l). QA-RIG's `perf_day12` run with the per-call-site GC log on the player. **Pass:** 0 B/frame from `HallCityExterior.Apply` and `HelixRiverMonitor.Update`.
12. **deferred: Unity — Saleh's notes 5, 13, 14 and 23** against his shots 6, 11 and 12, before and after, from steps 5 and 7.
13. **deferred: review** — the adversarial reviewer (definition of done #6) watches every GIF at full size, and the feel cards E8 and J4 are filled in `R7\FIX\FEEL\`.

## For INTEGRATION

### FEATURES.md (fold into the merge commit) - folded at the integration (integrate/a), with the INTEGRATION rows of the cleanup list (SCENE_CONTRACT_GAMEPLAY.md, ART_ASSET_LIST.md)
- **Line 31 (AVAILABLE sign):** "...the caption invites the press with a gentle pulse between the two inks (`readyInviteSeconds` 1.6 s a beat; steady under Reduced Motion..." becomes "...the caption invites the press: lit and steady while the morning briefing covers the office, then from Start Shift (the first frame it can be pressed) it pulses between the two inks for three beats (`readyInviteSeconds` 1.6 s each; `InvitePulse`, tested: `InvitePulseTests`), then holds lit and steady (BUG-67); lit and steady at once under Reduced Motion...".
- **Line 36 (portal rings):** replace the placement and look sentences with this.

  > Each vortex fills exactly the hole its ring's metal encloses, from the ring's painted-aperture mask (`HallPortalsSO`, `Assets/Data/Config/HallPortals.asset`, the one source of the rings: the drawing, each ring's layers, its opening fitted to its mask, its reach, the masks `HallPortalMasks.png` made from the painting by `tools/hall/portal_masks.py`; tested: `HallPortalsTests`, `PortalVortexTests`): the fence rails, the mullion and an arch's fence posts in front keep their painted pixels, the part below the fence rail (an arch's glass pane too) shows through the glass (`glassOpacity` 0.72), and nothing is drawn on the metal (the glow around the ring is the bloom on the core). The vortex is a layered cel swirl: a slow, dim back layer turning against the front's two hard tones, a light core with a white-hot heart, an ink line and a hard rim along the painted edge (the iris's own rim only while it springs open), and antialiased sparkles that fade out on a ring under about 20 px on screen.

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
| `OfficeSceneBinder.cs:184`, the `portalEffects` field's comment: "one per DeskConfigSO.hallPortalLayers entry, in its order" | DESK-PLAN | Stale: the effects are one per `HallPortalsSO` ring (`HallPortals.asset`), in its order |

### Split register (parts of HALL-FX's bugs that belong to another track)
| Part | To | Why, and what HALL-FX leaves for it |
|---|---|---|
| BUG-13 "dust shimmer" (VIS-9: dust motes 1-2 px across, worse at night) | LIGHT (#11) | The dust is `HallLightingRig`'s ParticleSystems (`dust`, `ApplyDust`, the dust knobs in `HallLighting_Default.asset`), LIGHT's files. LIGHT's fix must keep each mote at least about 2 px across or fade it below that, and pass its own flicker gate with the dust shown, on `hall_fx_city.json` and `hall_fx_dusk.json` with the "Hall FX: dust" steps removed (or toggled to shown). HALL-FX's gate runs with the dust hidden |
| BUG-26b, the portal glow lighting its surroundings | LIGHT (#11) | As before: the bloom on the vortex's core (FIX_PLAN) |

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
3. **The hole is the painted aperture, and an arch's goes on behind the glass (revised after the verifier's finding 2).** An arch (rings 4 and 5) has no painted lower half. Its hole is the arch above the bay fence's top rail plus the glass pane under it, down to the fence's lower rail, as a full ring's hole goes on behind its glass. The vortex's swirl centres on the hole's centroid. The portal pass measures the vortex against this aperture, registered on the off frame (`portal_check.py`), not against the audit's ray rule, which counts lit metal and the arches' shaded lower panel as hole (BUG-26a, "Which hole the pass measures"). A director call may overrule this; step 10 then names the hole definition the pass uses.
4. **Occluders are what spans the hole** (rails, the mullion; on an arch, the fence's posts below its top rail and its brass). Dark details on the wall behind (the pipe in ring 5, above its rail) are covered by the vortex, as they are behind the opening.
5. **The speed cap slows the whole river** on a small display, rather than dropping detail. The desk monitor runs at about half speed: calmer, which J4 asks for. The tray keeps its speed and loses its rungs, which fade out when they are packed under 2 px apart.
6. **The crossfade does not depend on draw order.** The two faces share a sorting order: rather than impose one (the crowds sit at 61, the next order up), the two-phase schedule is correct either way.
7. **`HallCityExterior.rain` stays as an inert field** until its four readers outside HALL-FX drop it. Deleting it would break CAM's file and three unowned tools.
8. **`HallCity.WindowHash` and `WindowLevel` are the shader's rules in C#,** called by the tests only. The windows are drawn per pixel by `NOPE/Hall Living City`, which no EditMode test can run. They are its reference implementation, and the shader's comments name them, so a change to one shows in the other's review. Every other new public member has a game caller.
9. **`CityTraffic.mat` is untouched.** FIX_PLAN lists it under HALL-FX, but it lives in `Assets/Art/Office/AnimeHallLayers/Completion/City/`, which is art-side (v1 §0 rule 2). The traffic fix is all in the shader and `HallCityExterior`, so the material needs no change.
10. **The invitation starts when the booth leaves its Newsletter phase** (verifier finding 1). The sign's `Clickable.Interactable` would be the exact signal, but the link cannot reach it: GameManager (FLOW) and the binder (DESK-PLAN) hold it privately, and the binder's call is theirs. `BoothCoordinator.Phase` is public, read-only to us, and GameManager sets it to `NoTraveller` in the same `BeginShift` call that makes the sign interactable. The link polls it once a frame only until the beats start. If BELL-SIGN (Phase B, which takes the sign) adds a "pressable" signal, `Pressable` should read that instead.
11. **HALL-FX's flicker runs hide LIGHT's dust** (verifier finding 4), through a dev cheat ("Hall FX: dust"), rather than masking it. The motes are 1-2 px and spread over the hall, so a tracked mask would cover most of the hall and wave HALL-FX's own pixels through. LIGHT owns the dust and measures it with the dust shown (split register).
12. **The iris's own rim fades out over the last 15 % of its opening** (`_Life.x` 0.85 to 1, verifier finding 5), so a full-open vortex keeps only the rim along the painted edge.
