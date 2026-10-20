# LIGHT: a warm lamp and one lighting model, day to night

Branch `fix/a-light`, cut from `fix/p0-cuts` (5aecb60). Source: FIX_PLAN.md §3 "LIGHT" (FIX_PLAN_v2: "LIGHT: unchanged"), BUGLIST BUG-14, 16, 25, 26, 31, 51, 65, 73, the split register (§1a) and GAME_FEEL_BIBLE.md. Merge slot #11.

**Saleh's rules this track honours (quoted, DECISIONS_1019a.md and memory):**
- D2: "everything should be animated and satisfying responsive and gives good feedback". The lamp switches on with a filament warm-up and a click, not a pop.
- Everything diegetic (memory): the lamp's switch-on is the desk's own light changing. No UI.
- DECISIONS_1019a.md holds no LIGHT-specific call, and v2 leaves LIGHT unchanged. Every design choice below that is not quoted is marked **proposal (ours)**.

## What changed, per BUG part

### BUG-14a: the lamp pool and the night mat (one shared desk light)
- **New `Visuals/DeskLighting.cs`** (pure, offline-tested). From the hour on the shared cycle it gives one level and one colour for the whole desk:
  - the room's light falls from the full day (level 1, the desk as drawn) to a night floor (cool tint);
  - the work lamp adds a warm share once it is on.
  - `ReadableNightLevel` = 0.49 is the rule: (55/76)^2.2, the mat at about luma 55-60 at night against 76 by day.
  - `LampOnHour` is the afternoon dip: the hour the hall's evening reaches `deskLampOnAt`. With the default knobs that is 16:03, inside the dusk and before the 16:30 sunset.
- **New `Office/DeskLightLevel.cs`.** Every frame it evaluates DeskLighting at the rig's hour and publishes the globals of `Shaders/Include/DeskLight.hlsl`. It writes only on change.
  - The lamp switches on at the dip with a filament warm-up (`HallDayCycle.WarmUp`; instant with reduced motion) and a click.
  - A lamp that is already on when the office opens is simply on: no click, no blink.
  - `Live.Light` is the uniform light the traveller is tinted by.
- **`Shaders/Include/DeskLight.hlsl`** is now real:
  - globals: `_DeskLightTint` (alpha = published), `_DeskLightLevel`, `_DeskLightKey`, `_DeskLampPool` (centre on the mat and radius), `_DeskLampShare`;
  - `DeskLight()`: uniform, for papers, photo and traveller;
  - `DeskLightAt(positionWS)`: the lamp's share falls off away from the pool;
  - `DeskLightPublished()`.
  - With nothing published (Home, Title, edit mode) every function returns 1, which is today's look.
- **`DeskAnime.shader`** includes it. In the office the energy is `_DeskLightKey × DeskLightAt(p)` and the hue is the light's colour, instead of the hall daylight's intensity. The SH ambient term is scaled the same way. Outside the office the old key-light path is unchanged.
- **The art's desk lamp spot** ("Desk lamp", HallLightKind.DeskLamp, aimed behind the AVAILABLE sign) is set dark by the rig in play. In edit mode it is left as authored.
- **Knobs** (`HallLightingSO` → `HallLighting_Default.asset`, header "The desk"):

  | Knob | Value |
  |---|---|
  | `deskKeyAtDay` | 0.68 (the old noon daylight's key energy) |
  | `deskNightFloor` | 0.35 |
  | `deskNightTint` | (0.78, 0.84, 1) |
  | `deskLampLevel` | 0.25 |
  | `deskLampColour` | (1, 0.82, 0.6) |
  | `deskLampRadius` | 0.45 m |

  `deskLampOnAt` (0.1) and `deskLampWarmSeconds` (0.6) are kept. Every value is **proposal (ours)**, to be tuned by eye in the Unity pass. Night level is 0.35 + 0.25 = 0.60 against the 0.49 needed.
- `deskLampDayShare` is removed: the lamp is off by day, so the switch-on shows.

### BUG-14b: no stray sun shadow inside the cubicle
- In play, `HallBakedLighting` sets the art daylight's shadows to None, and the rig sets the desk lamp's and the PC glow's 3D lights to None. They used to force Soft every frame.
- In edit mode nothing changes, so the art scene stays byte-identical if someone saves it from the editor.

### BUG-14c: the photo matches the traveller
- `AnimeHallShiftLink` tints the traveller by `DeskLightLevel.Live.Light` (tint × level). The passport photo takes the same value through `DeskLight()`, which DOC-LOOK adds to DocumentCel (seam request below).
- Both read one uniform, so the photo/traveller ratio is constant at every hour.
- Removed (now dead): `HallBakedLighting.TravellerShade`, `HallLightingRig.TravellerShade`, `HallLightingSO.travellerShade` and its asset block.

### BUG-16b: SSAO noise
- `OfficeContactShading.asset` is cut: `m_Active: 0`, because blue-noise SSAO re-seeds every frame in the player and there is no TAA.
- DeskAnime drops the `_SCREEN_SPACE_OCCLUSION` keyword and its AO multiply. That code had no other reader.

### BUG-16a (STAMP's part; LIGHT's seam made real)
- `_PaintedHighlight` and `_InkRim` now do something in DeskAnime's forward pass:
  - `_PaintedHighlight`: a hard-edged painted highlight band, anti-aliased by its derivative;
  - `_InkRim`: the silhouette darkens to ink.
- Both default to 0, so every existing material is unchanged. STAMP sets them on the rack's brass.

### BUG-25a: real night pools in the hall
- `HallDeepLayout.shader` is kept as the hall's one lighting model: **proposal (ours)**, see the decisions below.
- Each lit fixture now throws real light, scaled by its live lit share (strike flicker and stagger included), using the four-state bake's own falloff and placement:
  - a glow on the wall around it (not on the window glass);
  - a pool on the floor receivers below it (`_Masks.b`).
- Gallery lamps use their authored `HallMountedFixture.floorPool`. Ceiling fixtures use the bake's floor placement.
- The night tint away from the pools is a knob, `hallNightTint` (0.2, 0.27, 0.43), a little darker than the old (0.24, 0.32, 0.49), so the pools read. `hallPoolStrength` is 0.35.
- `HallBakedLighting` now gathers the gallery lamps (under the art) as well as the rig's ceiling fixtures, and passes every fixture's wall-glow and floor-pool shapes.

### BUG-25b: one clock
- **`HallDayCycle.Active` / `Publish`.** The rig publishes its knobs every frame, and every reader shares that cycle.
- **`HallBakedCycle.Weights`** (the painting's four states, CityView's paintings and the daylight's direction) is read off the cycle instead of hard-coded keys:
  - night share = `HallDayCycle.Evening`, the value the lamps, signs, city windows and calendar ink switch on;
  - the daylight is split morning → noon → evening by the sun's place.
  - The painting is now night exactly when the lamps are. The old keys lagged by up to 2.5 h.
- **`HallDayCycle.DuskHour`** (the seam RUN-SAFE's cheat reads) is real: `HourAtEvening(0.5)`, the sunset of the active knobs. That is **16:30** with the defaults; the stub was 18.5.
  - Under the one cycle, 18:30 is full night, which was CODE-67's "cheat Dusk shows full night".
- **The art's daylight write** (`AnimeHallPresentation.cs:52-56`, art-side) is overridden. `HallBakedLighting` writes the daylight every frame in LateUpdate with `DefaultExecutionOrder(200)`, after the presentation's Update. Its deletion is an art request (below).

### BUG-25c: no noon frame on load
- `HallLightingRig.Hour` now goes through `HallDayCycle.ShownHour`. In play, before the shift clock exists, the hall shows **today's opening hour** (the day plan's shift, else the standard day, from `RunManager`), not the edit-mode noon.
- The clock's first `SetClock` counts as a first frame, so lamps already on at that hour don't strike.

### BUG-25d: the left-pier diffuser
- `HallDeepLayout.shader` drops `step(_CanvasSize.x*.36, p.x)`. Every ceiling diffuser on the canvas can light, the one above the left elevator pier (canvas x ≈ 620) included.

### BUG-26b and BUG-51: one global Volume with bloom
- `OfficeFinishProfile.asset` keeps its Tonemapping (Neutral), Color Adjustments (the light grade) and Vignette (0.2), and gains **Bloom**:

  | Setting | Value |
  |---|---|
  | threshold | 1.0 |
  | intensity | 0.6 |
  | scatter | 0.65 |
  | tint | warm (1, 0.95, 0.88) |
  | high-quality filtering | on |

  These are **proposal (ours)**, tuned by eye at H5.
- `HallLightingSO.finishProfile` references the profile. In play the rig puts it on a global Volume (made at runtime) and turns the office camera's post-processing on. The camera is `HallBackdrop.Source`.
- **HDR, so only true emitters bloom.**
  - `HallBackdrop`'s texture is now ARGBHalf with an HDR 2D camera, where the device supports it.
  - Lit diffusers glow past white (`hallEmissionGain` 2.5 on the Deep Layout's emission; 0 by day, because their level is 0).
  - The portal vortexes' additive glow can pass 1.0.
  - Paper whites (≤1) do not bloom.

### BUG-31b: hall layer memory (LIGHT decides)
- **Decision (ours): the Deep Layout layers stay.** The 58 FourState-material layers (all disabled) and the `Completion/FourState` light and glow textures go.
- That removal is art-side, so it is an art request (below).

### BUG-65h: hall GPU and per-frame writes
- `HallBakedLighting` writes the layers' property blocks only when a value moves by more than 0.002 (half an 8-bit step). Before, all 58 were rewritten every frame. Gathering allocates once per enable, so a frame allocates nothing.
- The **HallFourState shader** (19 fetches a fragment) is deleted with its installer.
- `HallBackdrop` **still redraws every frame.** The portal vortexes, the dust and the city traffic animate inside its texture, so "redraw only when the hour changes" would freeze them. The plan's premise (CODE-52) doesn't hold, and the GPU budget is measured instead (recipe U12).
- The 59 disabled layers leave the load through the art request.

### BUG-73h: stale installers
- `Assets/Editor/TerminalArt/HallFourStateBaker.cs` is deleted. Its "Bake Four States" menu reassigned every hall layer's material and saved AnimeHall.unity, and "Capture Four States" depended on it.
- `Assets/Shaders/HallFourState.shader` is deleted too. The art-side `HallFourState.mat` now has no shader; its 58 renderers are disabled, and its removal is in the art request.
- `HallMountedFixture.rig` is removed: only the baker read it. `floorPool` is now read by `HallBakedLighting`.

## Decisions taken in the track (proposals, ours; Saleh's rules are quoted above)
1. **The hall's one model is the Deep Layout,** with per-fixture pools, not Lit2D.
   - The Light2D rig stays as the scheduler (fixture levels, signs, portals' night, city windows) and lights the dust.
   - Its Light2Ds light nothing else, because the Deep Layout's 2D pass is unlit.
   - Moving to Lit2D would re-light 58 art layers blind.
2. **No `OfficeSceneUIBuilder.Lights.cs`.** The builder's root (QA-RIG's) has no registration seam, and LIGHT may not add a call line to it.
   - The desk light and the finish Volume are made at runtime by `HallLightingRig` (the art office's own lighting root, LIGHT's file), the way `HallBackdrop` makes its camera.
   - No OfficeGameplay rebuild is needed for LIGHT. If QA-RIG ships the seam, they can move to `.Lights.cs`.
3. **The lamp pool is analytic** (`DeskLightAt` in the include), not a real spot light. It is centred exactly on the mat, has no shadow, isn't limited by URP's per-object light count, and is identical at every aspect.
   - The pool's centre is the art's DeskSurface anchor (the mat), resolved like the binder does.
4. **Levels are relative to the full day** (1 = the desk as drawn). Noon looks exactly as before for papers, the traveller and props.
   - Morning is now as bright as noon on the desk; the old daylight was dimmer at 08:00.
5. **DuskHour = the sunset (16:30)**, the hour the hall's evening is half. The cheat now shows dusk.
6. **The lamp click is `SoundCues.RelayClack`** with its placeholder for now, until FEEDBACK adds a `desk_lamp_on` cue (request below).
7. **The SSAO is cut,** not reduced. A static contact pass can't be verified outside a built player, and the cut is noise-free by construction.

## Seams this track provides (other tracks code against these)

| Seam | File | For |
|---|---|---|
| `DeskLight()`, `DeskLightAt(p)`, `DeskLightPublished()` and the globals | Shaders/Include/DeskLight.hlsl | DOC-LOOK (DocumentCel), any desk shader |
| `DeskLightLevel.Live.Light` | Office/DeskLightLevel.cs | the traveller (AnimeHallShiftLink, LIGHT's) |
| `_PaintedHighlight`, `_InkRim` (now effective) | DeskAnime.shader | STAMP |
| `HallDayCycle.DuskHour` (real: 16:30 with the defaults), `Active`, `Publish`, `HourAtEvening`, `ShownHour` | Visuals/HallDayCycle.cs | RUN-SAFE (Dusk), anyone reading the cycle |
| `HallBakedCycle.Weights(hour)` (now on the shared cycle) and `Weights(hour, settings)` | Visuals/HallBakedCycle.cs | CityView (CAM), editor tools |
| `HallBackdrop.Source` | Office/HallBackdrop.cs | the rig |

## Requests to other tracks (seams; LIGHT edits none of their files)
- **DOC-LOOK (BUG-14a/14c):**
  - `#include "Assets/Shaders/Include/DeskLight.hlsl"` in `DocumentCel.shader`, and multiply the paper's colour, the photo's included, by **`DeskLight()`** (uniform).
  - Not `DeskLightAt`: the photo must read the same value as the traveller, so the photo check stays fair.
- **STAMP (BUG-16a):** set `_PaintedHighlight` (about 0.5) and `_InkRim` (about 0.4) on the rack's DeskAnime brass. The final values are by eye against a dater at the LIGHT pass.
- **RUN-SAFE (BUG-25b):**
  - `DevCheats` must read `HallDayCycle.DuskHour` when the cheat runs, not in a static initializer, so a knob change follows.
  - The menu's "Dusk 18:30" label becomes the hour it reads (16:30 with the defaults). FEATURES line 255 and CHEATS.md follow.
- **FEEDBACK (cue request):** a `desk_lamp_on` cue (a lamp's switch click, short) in `SoundCues` and SOUND_LIST. LIGHT swaps `DeskLightLevel`'s `SoundCues.RelayClack` for it once it exists (one line, at INTEGRATION).
- **Art requests (GPT mailbox, filed by LIGHT; the orchestrator posts them, since LIGHT does not push):**
  1. *AnimeHallPresentation.cs:52-56*: delete the daylight write (`daylight.intensity` / `daylight.color` from `evening`). HallBakedLighting owns the daylight now (BUG-25b). Until then it is overwritten every frame, with no visible effect.
  2. *AnimeHall.unity*:
     - remove the 58 disabled layers that use `Completion/FourState/HallFourState.mat`;
     - remove that material (its shader is deleted) and the `Completion/FourState/*Light.png` / `*Glow.png` textures;
     - remove every other disabled layer that no longer ships (59 in total, BUG-65h / 31b).
     - The Deep Layout layers stay.
  3. *AnimeHall.unity, "Desk lamp" (HallLight DeskLamp, a 1.8 m spot aimed behind the AVAILABLE sign)*: it is dark in play, so it may be deleted, or kept for the editor's look.
- **INTEGRATION:**
  - **Cleanup list.** `Assets/Editor/TerminalArt/HallLightingPreviewWindow.cs` (unowned) has a "Verify Time Slider" menu that expects the old keys (10:00 = morning/noon 50/50, 18:00 = evening/night 50/50). On the one cycle those hours are noon and full night. Its expectations go to 16:30 = evening/night 50/50, or the menu goes. LIGHT did not edit it.
  - **Owner's note.** `Assets/Tests/EditMode/ShiftHoursTests.cs` (FLOW's file) had one assertion on LIGHT's code (`HallBakedCycle.Weights(16:30).z == 1`, the old keys). LIGHT rewrote that one line to the cycle's sunset: half evening and half night, so evening + night = 1. No other line changed.
  - **Folder metas.** `Assets/Editor/QA.meta` and `Assets/Editor/QA/Checks.meta` are new here. If QA-RIG's branch made the same folders, keep QA-RIG's metas and drop these.
  - **docs/HALL_LIGHTING.md** (no owner) still describes the traveller's shade gradient and the four-state bake. Fold this file's text in.

## Tests

| Test | Where | What | Runs |
|---|---|---|---|
| `DeskLightingTests` (7, new) | Tests/EditMode | full day = as drawn; night floor ≥ ReadableNightLevel; never below it once the lamp is on; the lamp-on hour is the dip (off a minute before, on a minute after, never earlier); the lamp's warm share; cool night, warmer with the lamp; odd knobs repaired | offline |
| `HallDayCycleTests` (+3, 1 rewritten) | Tests/EditMode | **one cycle gives the hall, lamps, windows and calendar the same hour** (every 5 min: painting night share = evening; every stage switches on the same evening); DuskHour = the shared cycle's sunset and follows the knobs; `HourAtEvening` is the dusk's inverse; **the shown hour is never noon before the clock exists** | offline |
| `HallBakedCycleTests` (rewritten, 11) | Tests/EditMode | night share = the cycle's evening at every minute; key hours; morning → noon → evening; continuous, normalized, cyclic; moving the sunset moves the night; the one-argument form reads the shared cycle; NaN/∞ = noon | offline |
| `ShiftHoursTests` (1 line) | Tests/EditMode | 16:30 is the sunset: evening + night = 1 | offline |
| `LightSeamTests` (3 text, 2 NeedsEditor) | Editor/SeamTests | the seam properties are declared **and read** by the forward pass; DeskAnime includes DeskLight.hlsl, takes `DeskLightAt`, and has no SSAO; DeskLight.hlsl declares the globals and falls back to 1 | text tests offline (scratch editor runner); NeedsEditor: Unity |
| `LightChecks` + `LightChecksTests` (7, new) | Editor/QA/Checks | demo-gate checks: SSAO cut; desk readable at night; lamp on at the dip; finish has tonemapping, grade, bloom and vignette; no diffuser cut at x < 36%; the four-state installer and shader gone. `LightChecks.Failures()` is ready for QA-RIG's DemoGate to register | Unity (NeedsEditor) |

**Checks run outside Unity:**

| Check | Baseline (fix/p0-cuts 5aecb60) | After |
|---|---|---|
| Compile check (the untracked `Assets/Editor/_TimeDesk*.cs` scratch files, which are not this branch's and don't compile, excluded) | 0 errors, 161 warnings | 0 errors, 153 warnings (none in LIGHT's files) |
| Offline runner | passed 3710, failed 0 | passed 3716, failed 0 |
| Editor-side text tests (scratch runner over Assembly-CSharp-Editor.dll, NeedsEditor skipped) | - | LightSeamTests and LightChecksTests: passed 3, failed 0, skipped 9 |

## Feel-bible rows (acceptance)
LIGHT owns no feel card (Appendix). Its parts of other rows:
- **J5 / G3-shift-start:** no noon flash on load (25c). Recipe U5.
- **D3 Traveller:** photo and traveller lit alike (14c). Recipe U9.
- **B3 Rack:** no SSAO noise (16b). Recipes U13 and U11.
- **E1-E6 / Global:** bloom for the flare and the emissives (51). Recipes U10 and U14.
- **G3 flash safety:** the lamp's warm-up is one dim blink of 80 ms, then an ease-up. It is not a flash, and with reduced motion it is instant.

## RESULT

Everything that needs the editor or a built player is **deferred: Unity**. Saleh may be on the PC, so this track was code-only. Every recipe below runs in the NOPE-art editor through the job runner (`Library\ClaudeJobs\<name>.job`), or on a scratch built player with QA-RIG's driver where marked [BP].

Standard vocabulary: A1 1920×1080, A2 1280×720; H1 09:00, H2 12:00, H3 16:45, H4 18:30, H5 22:00; golden seed 12345; SEQ = 30 fps PNG + GIF.

- **U1. Import (deferred: Unity).**
  - Refresh the AssetDatabase.
  - Expected: `ShaderUtil.ShaderHasError` is false for `NOPE/Desk Anime` and `NOPE/Hall Deep Layout`; the console has no shader compile error; `OfficeFinishProfile` lists Tonemapping, ColorAdjustments, Vignette and Bloom; `OfficeContactShading.isActive` is false.
  - Open AnimeHall: the console shows no new error (the disabled `HallFourState.mat` renderers may show the missing-shader material; they are disabled).
- **U2. EditMode suite (deferred: Unity).** Run TestRunnerApi EditMode. Expected: all green, except the known third-party `PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly`. It includes:
  - `LightSeamTests` (5, including 2 NeedsEditor);
  - `LightChecksTests` (7);
  - `DeskLightingTests`, `HallDayCycleTests`, `HallBakedCycleTests`, `ShiftHoursTests`.
- **U3. Time-lapse [BP] (deferred: Unity).**
  - Paths: A1 and A2; the office view and the reading view; day 12 played in sequence; cheat Time of Day or `ForcedHour` at H2, 15:00, H3, H4, 20:00 and 23:00.
  - Per capture, read `Shader.GetGlobalVector("_DeskLightTint")`, `_DeskLightLevel` and `_DeskLampPool`, and project the DeskSurface anchor's bounds to the screen for the mat rect.
  - **Pass:**
    - mat luma ≥ 55 at every hour;
    - a passport readable at each hour (cap gate at 23:00);
    - the pool's centre (`_DeskLampPool.xyz`) inside the mat's world bounds.
- **U4. Lamp switch-on (deferred: Unity).**
  - Day 12 (opens 16:00), clock at speed. SEQ from 16:00 to 16:10 game time, about 10 s real.
  - Expected switch-on at `DeskLighting.LampOnHour`, 16:03 with the defaults: the click plays and the pool warms over 0.6 s.
  - **Pass:**
    - a visible switch-on event at the dip;
    - no click when the office opens already past the dip (day 12 jumped to 17:00 by the cheat: no click, lamp on at frame 0).
- **U5. Load, no noon frame (deferred: Unity).**
  - SEQ of the office load at 13:00 (day 8) and 16:00 (day 12), each reached by playing in sequence and by the Jump cheat.
  - Log `HallLightingRig.Hour` in the first LateUpdate.
  - **Pass:** frame 0's hour equals the opening hour (13.0 or 16.0); no noon hall frame; no lamp strike flicker once the clock arrives.
- **U6. Dusk cheat (deferred: Unity; needs RUN-SAFE's DuskHour read).**
  - Compare cheat Dusk with a natural `HallDayCycle.DuskHour` (16:30), both at A1, by eye and by image diff.
  - **Pass:** they match. Cheat Dusk is no longer full night.
- **U7. Sun shadow (deferred: Unity).**
  - Reading view at H1 and H2.
  - Script: every enabled `Light` in the loaded office has `shadows == None`.
  - **Pass:** no diagonal band on the mat. Mat luma along the old lamp-arm shadow line is within ±3 of its neighbours.
- **U8. Left-pier diffuser (deferred: Unity).**
  - H5 capture of the hall at A1.
  - **Pass:** the diffuser above the left elevator pier (canvas x ≈ 620) is lit, with luma within 20 % of the mean of the other lit diffusers.
- **U9. Photo vs traveller (deferred: Unity; needs DOC-LOOK's DocumentCel include).**
  - Captures at H2, H4 and H5 of the traveller and the passport photo.
  - Read back `DeskLightLevel.Live.Light`, and `DeskLight()` via `_DeskLightTint` × `_DeskLightLevel`.
  - **Pass:**
    - both read the same values;
    - the face-region luma ratio photo/traveller at H4 and H5 stays within ±5 % of its H2 value.
- **U10. Portal glow (deferred: Unity).**
  - Night capture (H5) of the five rings.
  - **Pass:**
    - bloom is visible around each vortex and on the ring's surroundings, by eye;
    - 0 glow pixels drawn on the metal (HALL-FX's mask test still green).
- **U11. Rack style (deferred: Unity; needs STAMP's values).**
  - Reading view at H2 and H5.
  - **Pass:** the rack's painted highlight and ink rim are visible and match a dater for style, by eye.
- **U12. GPU [BP] (deferred: Unity).**
  - Built-player profile of day 12 with all items at A1.
  - **Pass:**
    - hall GPU time ≤ 4 ms on the reference mid-range GPU (proposal, ours);
    - 0 B/frame from `HallBakedLighting`, `HallBackdrop`, `DeskLightLevel` and `HallLightingRig` in the per-call-site GC log.
  - Also record the HDR backdrop texture's memory: ARGBHalf, about 16 MB at A1.
- **U13. Noise [BP] (deferred: Unity).** 10 s SEQ on the rack and the mat at H2 and H5. **Pass:** flicker pass, and temporal luma σ < 2/255 in static regions.
- **U14. Glow and contrast (deferred: Unity).**
  - Night capture: portals, signs, lit diffusers and the CRT. Bloom present, by eye.
  - Day capture: papers do not bloom.
  - **Pass:** the UiContrast gate is still green with tonemapping on.
- **U15. Note 12 (deferred: Unity).** Re-check note 12 against shot 13 and the audit's hour ladder, before and after.
- **U16. Art scene untouched (deferred: Unity).** After U3-U14, `git status` shows `AnimeHall.unity` and every `AnimeHallLayers/**` file unchanged, and no AnimeHallLayers `.meta` changed.
- **U17. OfficeGameplay (deferred: Unity).**
  - Build Office UI.
  - **Pass:** the semantic dump is identical to the base, since LIGHT changed no builder. The desk light and Volume are runtime objects under the rig, named "Desk light and office finish (made at runtime)".
- **U18. Tuning by eye at H5 with the adversarial reviewer (deferred: Unity).** The knobs, all in `HallLighting_Default.asset`:
  - desk: `deskNightFloor`, `deskLampLevel`, `deskLampRadius`, `deskNightTint`, `deskLampColour`;
  - hall: `hallNightTint`, `hallPoolStrength`, `hallEmissionGain`;
  - Bloom: threshold, intensity and scatter in `OfficeFinishProfile.asset`.

## FEATURES.md (for INTEGRATION to fold in)
- **Line 37 (the hall's lights).**
  - Replace "a downward spot under the green lamp's shade" and "the traveller's tint is multiplied by the cycle's shade" with the desk's one light: `DeskLighting`, `DeskLightLevel`, `DeskLight.hlsl`. Tested: `DeskLightingTests`.
  - Replace "the desk lamp first (`deskLampOnAt` 0.1, off by day, a filament's warm-up)" with: the work lamp's warm pool centred on the mat switches on at the afternoon dip (`DeskLighting.LampOnHour`, 16:03 with the defaults), with a click and a filament's warm-up. Tested: `DeskLightingTests.TheLampOnHour_IsTheAfternoonDip`.
  - Add: the painting's four states on the one cycle (`HallBakedCycle`, night = the evening; tested: `HallBakedCycleTests`, `HallDayCycleTests.OneCycle_GivesTheHallLampsWindowsAndCalendarTheSameHour`).
  - Add: night pools under every lit fixture; every ceiling diffuser lights, the left pier's included.
  - Add: the hall before the clock shows today's opening hour (tested: `HallDayCycleTests.TheShownHour_IsNeverNoonBeforeTheClockExists`).
  - Add: no real-time shadows in the cubicle.
  - Remove: the traveller's shade gradient from the knob list.
- **Line 172 (the hours grow).** "the hall's lights and baked four states" now read one cycle.
- **Line 255 (cheats).** "Dusk 18:30" becomes "Dusk (`HallDayCycle.DuskHour`, the sunset: 16:30 with the default knobs)". This is RUN-SAFE's edit.
- **New line: the office finish** (BUG-51). One global Volume (tonemapping, a light grade, bloom for emissives, a gentle vignette; `OfficeFinishProfile.asset`, via `HallLightingSO.finishProfile`). The hall drawn in HDR so lit diffusers and portals glow. No SSAO (BUG-16b). Checked: `LightChecks`.
