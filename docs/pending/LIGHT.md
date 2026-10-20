# LIGHT: a warm lamp and one lighting model, day to night

Branch `fix/a-light`, cut from `fix/p0-cuts` (5aecb60), with `fix/p0-qa-rig` merged in (e50cf09: QA-RIG merges before LIGHT, and LIGHT's checks use its DemoGate API). Source: FIX_PLAN.md §3 "LIGHT" (FIX_PLAN_v2: "LIGHT: unchanged"), BUGLIST BUG-14, 16, 25, 26, 31, 51, 65, 73, the split register (§1a) and GAME_FEEL_BIBLE.md. Merge slot #11.

**Saleh's rules this track honours (quoted, DECISIONS_1019a.md and memory):**
- D2: "everything should be animated and satisfying responsive and gives good feedback". The lamp switches on with a filament warm-up and a click, not a pop.
- Everything diegetic (memory): the lamp's switch-on is the desk's own light changing. No UI.
- DECISIONS_1019a.md holds no LIGHT-specific call, and v2 leaves LIGHT unchanged. Every design choice below that is not quoted is marked **proposal (ours)**.

## Needs the orchestrator's OK (flagged deviations)
1. **ShiftHoursTests.cs (FLOW's file), one assertion.** §0 rule 1 makes the file FLOW's; §0 rule 2 makes existing test files belong to the owner of the code under test, and the line tests LIGHT's `HallBakedCycle`. The old line (`Weights(16:30).z == 1`, the old hard-coded keys) fails on the one cycle, so it was rewritten to the cycle's sunset (evening + night = 1). No other line changed. **Needs FLOW's or the orchestrator's OK**; if refused, the line goes to `HallBakedCycleTests` (LIGHT's) and FLOW deletes it from its file.
2. **DuskHour stays 18.5 (the plan's).** The first LIGHT commit moved it to the cycle's sunset (16:30) and rewrote the pass line; that is reverted (verifier item 5). The plan's line stands: cheat Dusk vs a natural 18:30, by eye. **Open proposal (ours), for the orchestrator (still only a proposal; the code is at 18.5):** on the one cycle with the shipped knobs (sunset 16:30, 1.5 h twilights) 18:30 is full night, so the cheat named "Dusk" shows night (CODE-67's symptom, now consistent with a natural 18:30 but still mislabelled). Two ways out, neither taken here: DuskHour = `HourAtEvening(0.5)` (16:30, with RUN-SAFE's label and the U6 line changed), or the cheat's label becomes "Evening 18:30" (RUN-SAFE). Since round 2 the desk lamp switches on at the same 16:30 (the sunset), so a 16:30 Dusk would also land on the lamp's switch-on.

## What changed, per BUG part

### BUG-14a: the lamp pool and the night mat (one shared desk light)
- **`Visuals/DeskLighting.cs`** (pure, offline-tested). From the hour on the shared cycle it gives one level and one colour for the whole desk:
  - the room's light falls from the full day (level 1, the desk as drawn) to a night floor (cool tint);
  - the work lamp adds a warm share once it is on.
  - **The level never passes 1** (verifier round 2, item 1). The lamp's share is capped at the room's deficit: `share = min(deskLampLevel × warm, 1 − room)`. The full day's 1 is the desk as drawn, so the uniform `DeskLight()` (papers, photo, traveller) is never brighter than the drawn day. That keeps paper whites ≤ 1 under the bloom's threshold (BUG-51), keeps the traveller's sprite tint (8-bit, clamped at 1) equal to the photo's shader multiply (BUG-14c), and keeps the dusk from brightening above the drawn look (decision 4). Round 1 peaked at 1.33 (16:03, daylight 0.9 + the lamp's 0.38); the cap holds for any knobs (`TheLevel_NeverPassesTheFullDay_AtAnyMinute`).
  - **The pool's hue falls off with its level** (round 2, item 4). DeskLight.hlsl now gets the lamp's colour (`_DeskLampTint`), and `DeskLightAt` subtracts the lamp's colour × its share outside the pool (it used to subtract a level from the mixed tint). The pool is warm under the lamp and the room's cool at the mat's corners. `DeskLighting.LightAt` is the same maths. `DeskLight()` (uniform) is unchanged.
  - **The readable rule is judged after the office finish** (round 2, item 3). `DeskLighting.Finish` models the profile's post exposure (+0.1 EV), Neutral tonemapping and contrast (+12, in LogC around ACEScc's mid grey), in the URP asset's grading order (LDR: the tonemap, then the LUT's contrast; HDR is modelled too). It uses URP's own constants: `NeutralTonemap` from Color.hlsl and the LogC parameters. `GradedLuma(luminance, finish)` is the day mat's luma on screen under a light, and `ReadableAfterFinish` is the rule:
    - the mat's corners are ≥ 55 + `MarginLuma` (2);
    - the pool's centre is ≥ 60;
    - the pool steps by ≥ `PoolContrastLuma` (8) from the centre to the corners.
  - The margin covers what the model leaves out: saturation +13 and the colour filter on a tinted light (both keep a grey's luminance), the 32³ LUT's interpolation, and the mat texture's spread around its mean of 76.
  - `HallLightingSO.FinishModel()` reads the live profile and the active URP asset, so the gate and the runtime check use the real values. `LightSeamTests.DeskLightingsFinishModel_IsTheShippedFinish` holds the offline tests' `Finish.Office` equal to the assets' text.
  - **The vignette is not in the rule.** It is a frame that darkens a screen point by the same factor by day as by night. Ruling an absolute 55 on vignetted pixels would measure where the mat sits on screen, not the light. Example: a point 30 % of the screen off-centre on each axis shows the day mat at 67.2 instead of 75.9. `DeskLighting.VignetteAt` (URP's formula: `intensity × 3`, `smoothness × 5`) lets U3 divide the vignette out at each sample's uv, and U3 reports both values.
  - **If Saleh or the reviewer wants the vignetted corners on screen ≥ 55 as well**, the fix is the vignette, not the light (the light is at its cap at the pool's centre): profile Vignette smoothness 0.65 → 0.3. At that 30 % point the night corners go from 51.0 to 54.8, which is about the line, so the mat would also have to sit closer to the centre. **Proposal (ours), not done.**
  - The unfinished lines stay as seams: `ReadableNightLuminance` = sRGB-decode(55) / sRGB-decode(76) = **0.529** and `TargetNightLuminance` (luma 60) = **0.625**. After the finish, 0.529 shows at about luma 52.5, which is why the rule now runs after the finish.
  - `LuminanceAt(light, poolWeight)` and `PoolWeight(distance, radius)` are DeskLight.hlsl's `DeskLightAt` and `DeskLampPool`, the same maths. `DarkestOnMat` evaluates the mat's corners (the farthest from the pool). `LumaAt` turns a luminance back into a screen luma (no finish), and `GradedLuma` does the same with the finish.
  - `LampOnHour` is the afternoon dip: the hour the hall's evening reaches `deskLampOnAt`. **With the round-2 knob (0.5) that is 16:30, the sunset**, inside the dusk and before the plan's H3 (16:45, "the afternoon dip"). Under the cap a 16:03 switch-on would add only the room's 0.05 deficit (about +2 luma: invisible). At 16:30 the room has lost 0.23, so a minute after the switch the pool's centre has gone from luma 62.3 to 71.6 after the finish (+9.3, against `SwitchOnLuma` 5) and turned warm.
- **`Office/DeskLightLevel.cs`.** Every frame it evaluates DeskLighting at the rig's hour and publishes the globals of `Shaders/Include/DeskLight.hlsl`. It writes only on change.
  - The lamp switches on at the dip with a filament warm-up (`HallDayCycle.WarmUp`; instant with reduced motion) and a click.
  - A lamp that is already on when the office opens is simply on: no click, no blink.
  - `Live.Light` is the uniform light the traveller is tinted by. It is ≤ 1 on every channel (the cap), so the sprite's vertex colour never clamps.
  - When bound it checks the knobs against the art's real mat (the DeskSurface anchor's bounds) with `ReadableAfterFinish` and the live finish, and warns once, naming the knobs, if the rule fails.
  - It publishes `_DeskLampTint` (the lamp's colour) beside the other globals, written only on change.
- **`Shaders/Include/DeskLight.hlsl`** (round 2: `_DeskLampTint`, and the hue falls off):
  - globals: `_DeskLightTint` (alpha = published), `_DeskLightLevel`, `_DeskLightKey`, `_DeskLampPool` (centre on the mat and radius), `_DeskLampShare`, `_DeskLampTint`;
  - `DeskLight()`: uniform, for papers, photo and traveller (unchanged);
  - `DeskLightAt(positionWS)`: `tint × level − lampTint × share × (1 − pool)`, so the lamp's warm light falls off away from the pool, hue and all;
  - `DeskLightPublished()`.
  - With nothing published (Home, Title, edit mode) every function returns 1, which is today's look.
- **`DeskAnime.shader`** includes it. In the office the energy is `_DeskLightKey × DeskLightAt(p)` and the hue is the light's colour, instead of the hall daylight's intensity. The SH ambient term is scaled the same way. Outside the office the old key-light path is unchanged.
- **The art's desk lamp spot** ("Desk lamp", HallLightKind.DeskLamp, aimed behind the AVAILABLE sign) is set dark by the rig in play. In edit mode it is left as authored.
- **Knobs** (`HallLightingSO` → `HallLighting_Default.asset`, header "The desk"), **retuned in round 2** for the cap, the finish and a pool that reads, on the 1.45 × 1.02 m mat:

  | Knob | First | Round 1 | Round 2 |
  |---|---|---|---|
  | `deskKeyAtDay` | 0.68 | 0.68 | 0.68 (the old noon daylight's key energy) |
  | `deskNightFloor` | 0.35 | 0.5 | **0.55** |
  | `deskNightTint` | (0.78, 0.84, 1) | same | same |
  | `deskLampLevel` | 0.25 | 0.38 | **0.45** (floor + lamp = 1: the pool's centre at night is at the drawn level, warm) |
  | `deskLampColour` | (1, 0.82, 0.6) | same | same |
  | `deskLampRadius` | 0.45 m | 1.5 m | **1.35 m** (full out to 0.34 m, 0.47 of the lamp at the corners) |
  | `deskLampOnAt` (evening) | 0.1 (16:03) | 0.1 | **0.5 (16:30, the sunset)** |

  **What they give at full night** (computed with DeskLighting; luma after the office finish, with the unfinished value in brackets):

  | Point on the mat | Round 1 (1.5 m, flat hue) | Round 2 |
  |---|---|---|
  | pool centre | 64.1 (65.4) | **68.9** (69.7) |
  | edge midpoint (0.725 m) | 60.3 (62.0) | 62.9 (64.3) |
  | corners (0.886 m) | **56.8** (58.8): fails 55 + 2 | **58.2** (60.1) |
  | centre to corners | 7.3, and no hue step | **10.7**, warm to cool |

  **The day around the switch-on** (round 2, the pool's centre and corners after the finish):

  | Hour | Level | Lamp's share | Centre | Corners |
  |---|---|---|---|---|
  | 16:00 | 0.967 | 0 | 74.0 | 74.0 |
  | 16:05 | 0.943 | 0 | 72.6 | 72.6 |
  | 16:29 | 0.782 | 0 | 63.2 | 63.2 |
  | 16:31 (lamp on) | 1.000 | 0.232 | 71.6 | 66.5 |
  | 16:45 (H3) | 1.000 | 0.333 | 70.2 | 62.6 |
  | 17:00 | 1.000 | 0.417 | 69.2 | 59.4 |
  | 18:30 (H4) and later | 1.000 | 0.450 | 68.9 | 58.2 |

  The first LIGHT doc's "night level 0.35 + 0.25 = 0.60 against 0.49" was a level, not a luminance, and the first knobs gave about 54 at the centre and 40 at the corners. Round 1's corners cleared the unfinished line by 3.8, but after the finish they reached only 56.8, and its pool was a flat tint. Every value is **proposal (ours)**, tuned by eye in the Unity pass (U18).
- `deskLampWarmSeconds` (0.6) is kept. `deskLampDayShare` is removed: the lamp is off by day, so the switch-on shows.

### BUG-14b: no stray sun shadow inside the cubicle
- In play, `HallBakedLighting` sets the art daylight's shadows to None, and the rig sets the desk lamp's and the PC glow's 3D lights to None. They used to force Soft every frame.
- In edit mode nothing changes, so the art scene stays byte-identical if someone saves it from the editor.

### BUG-14c: the photo matches the traveller
- `AnimeHallShiftLink` tints the traveller by `DeskLightLevel.Live.Light` (tint × level). The passport photo takes the same value through `DeskLight()`, which DOC-LOOK adds to DocumentCel (seam request below).
- Both read one uniform, and since round 2 that uniform is ≤ 1 on every channel at every minute (the cap). The sprite's 8-bit vertex colour therefore never clamps where the photo's shader multiply would not, and the photo/traveller ratio is constant at every hour, dusk included (round 1 drifted by up to about 30 % between 16:03 and 16:45).
- **Open risk, checked at U9 (not LIGHT's code to change):** in this linear-colour-space project Unity may decode a SpriteRenderer's `color` from sRGB to linear before the vertex colour reaches the shader. If it does, the traveller gets about `light^2.2` while the photo gets `light`. The fix is one line in `DeskLightLevel`: publish `Light` as `.gamma`, so it decodes to the photo's value. LIGHT leaves it out until U9 shows which way Unity goes, because the wrong guess would cause the drift it is meant to fix. U9's read-back decides it.
- Removed (now dead): `HallBakedLighting.TravellerShade`, `HallLightingRig.TravellerShade`, `HallLightingSO.travellerShade` and its asset block.

### BUG-16b: SSAO noise
- `OfficeContactShading.asset` is cut: `m_Active: 0`, because blue-noise SSAO re-seeds every frame in the player and there is no TAA.
- DeskAnime drops the `_SCREEN_SPACE_OCCLUSION` keyword and its AO multiply. That code had no other reader.

### BUG-16a (STAMP's part; LIGHT's seam made real)
- `_PaintedHighlight` and `_InkRim` now do something in DeskAnime's forward pass:
  - `_PaintedHighlight`: a hard-edged painted highlight band, anti-aliased by its derivative;
  - `_InkRim`: the silhouette darkens to ink.
- Both default to 0, so every existing material is unchanged. STAMP sets them on the rack's brass.

### BUG-25a: real night pools in the hall, shared with the slots
- `HallDeepLayout.shader` is kept as the hall's one lighting model: **proposal (ours)**, see the decisions below.
- Each lit fixture throws real light, scaled by its live lit share (strike flicker and stagger included), using the four-state bake's own falloff and placement:
  - a glow on the wall around it (not on the window glass);
  - a pool on the floor receivers below it (`_Masks.b`).
- Gallery lamps use their authored `HallMountedFixture.floorPool`. Ceiling fixtures use the bake's floor placement.
- **The night knobs and the fixtures are shader globals now** (verifier item 2): `_HallNightTint`, `_HallPoolColour`, `_HallPoolStrength`, `_HallEmissionGain`, `_HallFixtureLevels[16]`, `_HallFixtureCenters[16]`, `_HallFixturePools[16]`, `_HallFixtureCount`.
  - They are declared outside the shader's Properties, so no material value hides them, and every renderer drawn with the Deep Layout material takes them: the painting's layers **and HALL-FX's slot sprites** (HallSlotsLink copies only the per-layer states, which stay in the painting's block). At night the slots and the painting around them now share one tint, and the wall glow and floor pools continue across slot art.
  - Until `HallBakedLighting` publishes (the tint's alpha is 0), the shader keeps a built-in night **equal to the knob as published**, no pools and the painted glow. **Round 2, item 2:** the project is in linear colour space, so `HallBakedLighting` now publishes the night tint and the pool colour explicitly as their linear values (`SetGlobalVector(knob.linear)`; `SetGlobalColor` did the same conversion implicitly). `HALL_DEFAULT_NIGHT` is the knob decoded from sRGB, half3(.0331, .0593, .1549). Before, it used the raw (.2, .27, .43), about 5× brighter in linear. The gate check and the seam test now compare the built-in with the knob's linear value (±0.002), so a text-equal but colour-unequal pair fails.
  - The old per-renderer names (`_FixtureLevels`, `_ScheduledFixtureCount`, `_NightTint`, ...) are gone; nothing else read them.
- The night tint away from the pools is a knob, `hallNightTint` (0.2, 0.27, 0.43), a little darker than the old (0.24, 0.32, 0.49), so the pools read. `hallPoolStrength` is 0.35.
- `HallBakedLighting` gathers the gallery lamps (under the art) as well as the rig's ceiling fixtures, and measures every fixture's wall-glow and floor-pool shapes once per gather on the first layer (the layers share one registered canvas).

### BUG-25b: one clock
- **`HallDayCycle.Active` / `Publish`.** The rig publishes its knobs every frame, and every reader shares that cycle.
- **`HallDayCycle.Defaults`** is the cycle's one source of default knobs (verifier item 7): `HallLightingSO`'s field initializers read it and `Active` starts from it. The shipped asset tunes its own values (`fixturesOnBelow` 0.8); they reach every reader once the rig publishes, each frame. Before that (no hall in the scene) nothing that matters reads `Active`: the painting, lamps and desk light all run under the rig.
- **`HallBakedCycle.Weights`** (the painting's four states, CityView's paintings and the daylight's direction) is read off the cycle instead of hard-coded keys:
  - night share = `HallDayCycle.Evening`, the value the lamps, signs, city windows and calendar ink switch on;
  - the daylight is split morning → noon → evening by the sun's place.
  - The painting is now night exactly when the lamps are. The old keys lagged by up to 2.5 h.
- **`HallDayCycle.DuskHour`** stays the seam's **18.5** (the plan's H4), so the cheat compares with a natural 18:30 as the plan says. See "Needs the orchestrator's OK" item 2.
- **The art's daylight write** (`AnimeHallPresentation.cs:52-56`, art-side) is overridden. `HallBakedLighting` writes the daylight every frame in LateUpdate with `DefaultExecutionOrder(200)`, after the presentation's Update. Its deletion is an art request (below).

### BUG-25c: no noon frame on load
- `HallLightingRig.Hour` goes through `HallDayCycle.ShownHour`. In play, before the shift clock exists, the hall shows **today's opening hour** (the day plan's shift, else the standard day, from `RunManager`), not the edit-mode noon.
- **The opening hour is cached only once found** (verifier item 9): while `RunManager`, its World or its Config is not ready it asks again next frame, so a rig that enables before the run shows the opening hour as soon as the run exists (before, it cached the miss and showed noon until the clock).
- The clock's first `SetClock` counts as a first frame, so lamps already on at that hour don't strike.

### BUG-25d: the left-pier diffuser
- `HallDeepLayout.shader` drops `step(_CanvasSize.x*.36, p.x)`. Every ceiling diffuser on the canvas can light, the one above the left elevator pier (canvas x ≈ 620) included. Recipe U20 checks that the cut's removal lights no false emitter in the left 36%.

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
- `HallLightingSO.finishProfile` references the profile. In play the rig puts it on a global Volume (made at runtime) and turns the office camera's post-processing on. The camera is `HallBackdrop.Source`. **New:** the rig also adds the Volume's layer to the camera's `volumeLayerMask` if the scene's mask leaves it out (verifier item 11d), and U22 checks it.
- **HDR, so only true emitters bloom.**
  - `HallBackdrop`'s texture is ARGBHalf with an HDR 2D camera, where the device supports it.
  - Lit diffusers glow past white (`hallEmissionGain` 2.5 on the Deep Layout's emission; 0 by day, because their level is 0).
  - The portal vortexes' additive glow can pass 1.0.
  - Paper whites (≤ 1) do not bloom. Since round 2 that holds at every minute: the desk light's level is capped at 1, so the dusk no longer lifts papers to 1.33.
- **Glow coverage (verifier item 6).** The plan's line is "signs, portals and the CRT glow". LIGHT's own emitters above the threshold are the lit ceiling diffusers and the portals' HDR backdrop. **The signs and the CRT are not LIGHT's files and today draw at ≤ 1, so they do not bloom.** The sign Light2Ds light nothing in the unlit Deep Layout pass (decision 1).
  - **Why the threshold stays 1.0:** below 1 the papers', the forms' and the UI's whites would bloom (U14's "papers do not bloom" and the UiContrast gate). The fix is HDR emission on the emitters, not a lower threshold.
  - **Requests filed:** HALL-FX (the AVAILABLE sign and the department signs) and UI-PC (the CRT), below. Until they land, U14's signs/CRT part is a known red owned by them, and LIGHT's part (diffusers, portals, papers not blooming) is measured alone.

### BUG-31b: hall layer memory (LIGHT decides)
- **Decision (ours): the Deep Layout layers stay.** The 58 FourState-material layers (all disabled) and the `Completion/FourState` light and glow textures go.
- That removal is art-side, so it is an art request (below).

### BUG-65h: hall GPU and per-frame writes
- `HallBakedLighting` writes the layers' property blocks (states, shadow ray, amount, motion) and the globals only when a value moves by more than 0.002 (half an 8-bit step). Before, all 58 blocks were rewritten every frame. The fixtures and night knobs are now one set of globals instead of 58 copies. Gathering allocates once per enable, so a frame allocates nothing.
- The **HallFourState shader** (19 fetches a fragment) is deleted with its installer.
- `HallBackdrop` **still redraws every frame.** The portal vortexes, the dust and the city traffic animate inside its texture, so "redraw only when the hour changes" would freeze them. The plan's premise (CODE-52) doesn't hold, and the GPU budget is measured instead (recipe U12).
- The 59 disabled layers leave the load through the art request.

### BUG-73h: stale installers and the stale material gate
- `Assets/Editor/TerminalArt/HallFourStateBaker.cs` is deleted. Its "Bake Four States" menu reassigned every hall layer's material and saved AnimeHall.unity, and "Capture Four States" depended on it.
- `Assets/Shaders/HallFourState.shader` is deleted too. The art-side `HallFourState.mat` now has no shader; its 58 renderers are disabled, and its removal is in the art request.
- **`HallBakedLighting.material` is gone** (verifier item 1). It was only a null check, and AnimeHall.unity:8802 points it at `Completion/FourState/HallFourState.mat`. Once art request 2 deleted that material, `Apply()` would have returned at its first line and the state weights, night pools, left-pier emission, daylight pose and the daylight's shadows=None (14b) would all have stopped with no error. `Apply()` now needs only the rig, the art and the knobs; the scene's stale `material:` entry is ignored by Unity (no scene edit). Its wrong doc ("NOPE/Hall Deep Layout") went with it.
- **`HallBakedLighting.Configure` is deleted** (verifier item 7): its only caller was the deleted baker.
- `HallMountedFixture.rig` is removed: only the baker read it. `floorPool` is now read by `HallBakedLighting`.
- **Another stale installer, not LIGHT's:** `Assets/Editor/TerminalArt/HallWaitingBayAuthoring.cs:26-31` clones `HallFourState.mat` as its baseline (`new Material(baseline)`) and then saves AnimeHall.unity. Once the material goes it throws on `new Material(null)`. It is on INTEGRATION's cleanup list below, and art request 2 names it.

## Decisions taken in the track (proposals, ours; Saleh's rules are quoted above)
1. **The hall's one model is the Deep Layout,** with per-fixture pools, not Lit2D.
   - The Light2D rig stays as the scheduler (fixture levels, signs, portals' night, city windows) and lights the dust.
   - Its Light2Ds light nothing else, because the Deep Layout's 2D pass is unlit.
   - Moving to Lit2D would re-light 58 art layers blind.
2. **No `OfficeSceneUIBuilder.Lights.cs`.** QA-RIG's builder root has no registration seam, and LIGHT may not add a call line to it.
   - The desk light and the finish Volume are made at runtime by `HallLightingRig` (the art office's own lighting root, LIGHT's file), the way `HallBackdrop` makes its camera.
   - No OfficeGameplay rebuild is needed for LIGHT. If a seam ships, they can move to `.Lights.cs`.
3. **The lamp pool is analytic** (`DeskLightAt` in the include), not a real spot light. It is centred exactly on the mat, has no shadow, isn't limited by URP's per-object light count, and is identical at every aspect.
   - The pool's centre is the art's DeskSurface anchor (the mat), resolved like the binder does.
4. **Levels are relative to the full day** (1 = the desk as drawn). Noon looks exactly as before for papers, the traveller and props.
   - Morning is now as bright as noon on the desk; the old daylight was dimmer at 08:00.
5. **DuskHour stays 18.5** (the plan's); the 16:30 alternative is flagged to the orchestrator.
6. **The lamp click is `SoundCues.RelayClack`** with its placeholder for now, until FEEDBACK adds a `desk_lamp_on` cue (request below). Its AudioSource now outputs to the sound bank's **Desk** mixer group, so the SFX volume governs the placeholder too (verifier item 11e; before, it had no group and ignored the volume). RelayClack is also RingPanel's relay click, so until the cue lands the lamp sounds like a relay.
7. **The SSAO is cut,** not reduced. A static contact pass can't be verified outside a built player, and the cut is noise-free by construction.
8. **The night knobs are globals,** not per-layer properties (so the slots need no copy). Stand-in plates (HallSlotsDev, dev builds only, off by default) are region sprites whose uv is not the canvas's, so they take the pools at the wrong place; real slot art is full-canvas and registered.
9. **The lamp fills only the room's deficit** (round 2), not a lamp lerped in with the evening. The cap is exact at every minute and any knobs; a lerp would still overshoot with stronger knobs.
10. **The lamp switches on at the sunset** (`deskLampOnAt` 0.5, 16:30), not at 16:03. Under the cap an early switch-on would show nothing, and 16:30 is before the plan's H3 (16:45, its "afternoon dip"). The gate's window is now from the dusk's start to H3, and it also requires the switch-on to lift the pool's centre by ≥ 5 luma.
11. **The readable rule runs after the finish (grade and tonemap modelled), with a 2-luma margin; the vignette is reported and divided out at U3, not ruled on** (see BUG-14a above for why, and for the vignette fallback).

## Seams this track provides (other tracks code against these)

| Seam | File | For |
|---|---|---|
| `DeskLight()`, `DeskLightAt(p)`, `DeskLightPublished()` and the globals (round 2: `_DeskLampTint`) | Shaders/Include/DeskLight.hlsl | DOC-LOOK (DocumentCel), any desk shader |
| `DeskLightLevel.Live.Light` | Office/DeskLightLevel.cs | the traveller (AnimeHallShiftLink, LIGHT's) |
| `DeskLighting.LuminanceAt`, `LightAt`, `DarkestOnMat`, `LumaAt`, `GradedLuma`, `Finish`, `VignetteAt`, `ReadableAfterFinish`, `ReadableNightLuminance` | Visuals/DeskLighting.cs | QA (U3's numbers), DOC-LOOK (the paper's night) |
| `HallLightingSO.FinishModel()` | Office/HallLightingSO.cs | the gate, DeskLightLevel's check, QA |
| `_PaintedHighlight`, `_InkRim` (now effective) | DeskAnime.shader | STAMP |
| The hall's night globals `_HallNightTint`, `_HallPoolColour`, `_HallPoolStrength`, `_HallEmissionGain`, `_HallFixture*` | HallDeepLayout.shader, written by HallBakedLighting | HALL-FX (slots: nothing to copy) |
| `HallDayCycle.DuskHour` (18.5, the seam's), `Defaults`, `Active`, `Publish`, `HourAtEvening`, `ShownHour` | Visuals/HallDayCycle.cs | RUN-SAFE (Dusk), anyone reading the cycle |
| `HallBakedCycle.Weights(hour)` (on the shared cycle) and `Weights(hour, settings)` | Visuals/HallBakedCycle.cs | CityView (CAM), editor tools |
| `HallBackdrop.Source` | Office/HallBackdrop.cs | the rig |

## Requests to other tracks (seams; LIGHT edits none of their files)
- **DOC-LOOK (BUG-14a/14c):**
  - `#include "Assets/Shaders/Include/DeskLight.hlsl"` in `DocumentCel.shader`, and multiply the paper's colour, the photo's included, by **`DeskLight()`** (uniform).
  - Not `DeskLightAt`: the photo must read the same value as the traveller, so the photo check stays fair.
- **STAMP (BUG-16a):** set `_PaintedHighlight` (about 0.5) and `_InkRim` (about 0.4) on the rack's DeskAnime brass. The final values are by eye against a dater at the LIGHT pass.
- **RUN-SAFE (BUG-25b):** `DevCheats` reads `HallDayCycle.DuskHour` when the cheat runs, not in a static initializer. The value stays 18.5, so the "Dusk 18:30" label stays (unless the orchestrator takes the open proposal above).
- **HALL-FX (seam, BUG-25a, the slots):** nothing to copy. The night knobs and the fixtures are globals (`_Hall*`), which the slots' sprites take through the painting's material. Please **don't** set any `_Hall*` name in the slots' property block (it would override the global), and keep copying the per-layer states as today. Check U21 (slots at H5) at LIGHT's pass. The stand-in plates are region sprites (uv not the canvas's), so they take misplaced pools; they are dev-only and off by default.
- **HALL-FX (BUG-51 glow, the signs):** the AVAILABLE sign's lit panel and the department signs' lit panels should draw in HDR at night, so the bloom (threshold 1.0) catches them: their lit glyphs' colour × a gain of about 2 (values > 1) once their night stage is on (the rig's sign schedule: `HallLightKind.Sign`, `signsOnAt`), and ≤ 1 by day. A knob on HALL-FX's own config is fine; LIGHT tunes the bloom against it at U14.
- **UI-PC (BUG-51 glow, the CRT):** the CRT's screen should emit in HDR at night (its screen colour × about 1.5 on the bright text and the phosphor), so it blooms; ≤ 1 for anything the player reads closely (the UiContrast gate). LIGHT checks it at U14.
- **FEEDBACK (cue request):** a `desk_lamp_on` cue (a lamp's switch click, short) in `SoundCues` and SOUND_LIST. LIGHT swaps `DeskLightLevel`'s `SoundCues.RelayClack` for it once it exists (one line, at INTEGRATION).
- **Art requests (GPT mailbox, filed by LIGHT; the orchestrator posts them, since LIGHT does not push):**
  1. *AnimeHallPresentation.cs:52-56*: delete the daylight write (`daylight.intensity` / `daylight.color` from `evening`). HallBakedLighting owns the daylight now (BUG-25b). Until then it is overwritten every frame, with no visible effect.
  2. *AnimeHall.unity*:
     - remove the 58 disabled layers that use `Completion/FourState/HallFourState.mat`;
     - remove that material (its shader is deleted) and the `Completion/FourState/*Light.png` / `*Glow.png` textures;
     - remove every other disabled layer that no longer ships (59 in total, BUG-65h / 31b);
     - **HallBakedLighting's `material:` field (line 8802) may stay or go**: the script no longer has the field and no longer gates on it, so deleting the material does not stop the hall's lighting;
     - **dependency:** `Assets/Editor/TerminalArt/HallWaitingBayAuthoring.cs` (unowned) clones `HallFourState.mat` as its baseline; it must be deleted or repointed (INTEGRATION's cleanup list) before or with this removal, or its menu throws.
     - The Deep Layout layers stay.
  3. *AnimeHall.unity, "Desk lamp" (HallLight DeskLamp, a 1.8 m spot aimed behind the AVAILABLE sign)*: it is dark in play, so it may be deleted, or kept for the editor's look.
- **INTEGRATION:**
  - **Cleanup list.**
    - `Assets/Editor/TerminalArt/HallLightingPreviewWindow.cs` (unowned) has a "Verify Time Slider" menu that expects the old keys (10:00 = morning/noon 50/50, 18:00 = evening/night 50/50). On the one cycle those hours are noon and full night. Its expectations go to 16:30 = evening/night 50/50, or the menu goes.
    - **`Assets/Editor/TerminalArt/HallWaitingBayAuthoring.cs`** (unowned, verifier item 8): lines 26-31 load `Completion/FourState/HallFourState.mat` as the baseline for `new Material(baseline)` and then save AnimeHall.unity. With the shader deleted it makes materials from a shaderless baseline; once art request 2 deletes the material it throws. Delete the menu (its waiting-bay repair is done and saved in the scene), or create its materials from `Shader.Find("NOPE/Hall Waiting Bay Repair")` directly.
    - LIGHT did not edit either file.
  - **Owner's note.** `ShiftHoursTests.cs`: see "Needs the orchestrator's OK" item 1.
  - **Folder metas.** Resolved by merging QA-RIG: `Assets/Editor/QA.meta` and `Assets/Editor/QA/Checks.meta` are QA-RIG's (59cbf507…, 1bf5dcab…); LIGHT's duplicates are gone.
  - **docs/HALL_LIGHTING.md** (no owner) still describes the traveller's shade gradient and the four-state bake. Fold this file's text in.

## Tests

| Test | Where | What | Runs |
|---|---|---|---|
| `DeskLightingTests` (15) | Tests/EditMode | full day = as drawn; the unfinished lines 0.529 / 0.625; **the finish is modelled** (none = drawn, the day mat stays about 76, 0.529 shows at about 52.5, both grading orders within a luma, monotonic); **the vignette is URP's** (0.525 at the screen's corner); the pool weight is the shader's falloff; **the night reads after the finish: corners ≥ 57, centre ≥ 60**; **the pool reads: centre to corners ≥ 8, cooler at the corners; round 1 did not**; the first and round-1 knobs fail; **the mat's corners ≥ 57 at every minute**; **the level ≤ 1 (and every channel × level ≤ 1) at every minute, every warm-up share, for the shipped, round-1 and extreme knobs, the verifier's 16:03 included**; the lamp-on hour is the dip (the sunset, by H3); **the switch-on is visible (≥ 5 luma) and round 1's 16:03 would not be**; the lamp's warm share up to the room's deficit; cool night, warmer with the lamp; odd knobs repaired | offline |
| `HallDayCycleTests` (+4 over the base) | Tests/EditMode | one cycle gives the hall, lamps, windows and calendar the same hour; `HourAtEvening` is the dusk's inverse; the shown hour is never noon before the clock exists; **the defaults are the cycle's one source**; `DuskHour_IsTheCheatMenusDusk` (18.5) is the base's, unchanged | offline |
| `HallBakedCycleTests` (rewritten, 11) | Tests/EditMode | night share = the cycle's evening at every minute; key hours; morning → noon → evening; continuous, normalized, cyclic; moving the sunset moves the night; the one-argument form reads the shared cycle; NaN/∞ = noon | offline |
| `ShiftHoursTests` (1 line, FLOW's file: needs OK) | Tests/EditMode | 16:30 is the sunset: evening + night = 1 | offline |
| `LightSeamTests` (6 text, 2 NeedsEditor) | Editor/SeamTests | the DeskAnime seam properties are declared and read; DeskAnime takes `DeskLightAt` and has no SSAO; DeskLight.hlsl's globals (`_DeskLampTint` included) fall back to 1 and **`DeskLightAt` subtracts the lamp's colour**; **the hall's night knobs and fixtures are globals (none in Properties) and the built-in night equals the asset's knob decoded from sRGB, published as `.linear`**; HallBakedLighting has no material gate and no `Configure`; **`DeskLighting.Finish.Office` equals OfficeFinishProfile's exposure, contrast and Neutral and UniversalRP's LDR grading** | text tests offline (QA-RIG's editor runner); NeedsEditor: Unity |
| `LightChecks` (7 gate checks) + `LightChecksTests` (8) | Editor/QA/Checks | **registered for QA-RIG's DemoGate** as `[GateCheck("light.*", "LIGHT")]` returning `GateResult`: `light.ssao.cut`, `light.desk.readableAtNight` (round 2: after the live finish; corners ≥ 57, centre ≥ 60, pool step ≥ 8; reports the unfinished corners too), `light.desk.lampAtTheDip` (round 2: from the dusk's start to H3 16:45, and the switch-on lifts the centre ≥ 5), `light.finish.profile`, `light.hall.everyDiffuser`, `light.hall.nightIsShared` (round 2: against the knob's linear value), `light.hall.fourStateGone`. The tests assert each passes and that DemoGate finds all seven with no registration problem | Unity (NeedsEditor) |

**Checks run outside Unity** (compile check with the untracked `Assets/Editor/_TimeDesk*.cs` scratch files excluded: they are not this branch's and don't compile against it):

| Check | Baseline (e50cf09, the QA-RIG merge) | Round 1 (5c28898) | Round 2 |
|---|---|---|---|
| Compile check | 0 errors, 154 warnings (Editor), 0/0 (TimeDeskEditMode) | 0 errors, 154 warnings, 0/0 | 0 errors, 154 warnings, 0/0 |
| Offline runner (from the worktree) | passed 3716, failed 0 | passed 3720, failed 0 | **passed 3725, failed 0** (round 2's baseline, before its edits: 3720/0) |
| QA-RIG's editor runner (Assembly-CSharp-Editor, NeedsEditor skipped) | - | passed 72, failed 0, skipped 24 | **passed 73, failed 0, skipped 24** |

## Verifier findings (round 1) and what answered each
1. Material gate: dropped (field and gate); doc corrected; art request 2 amended. Seam test `HallBakedLighting_DoesNotGateOnAMaterial`.
2. Slots: night knobs and fixtures made globals; built-in night = knob; HALL-FX seam request filed; recipe U21.
3. Night rule: luminance at the mat's corners with the pool's falloff; knobs retuned; numbers corrected (table above); the old knobs are a failing test case.
4. DemoGate: QA-RIG merged; checks converted to `[GateCheck("light.*","LIGHT")]` returning `GateResult.Of(...)`; LIGHT's duplicate folder metas dropped.
5. DuskHour: back to 18.5 and the plan's comparison; the 16:30 idea flagged to the orchestrator.
6. Glow: HDR-emission requests to HALL-FX (signs) and UI-PC (CRT); threshold kept and justified; U14 split by owner.
7. Dead code: `Configure` deleted; `HallDayCycle.Defaults` is the one source of the cycle's defaults.
8. Waiting-bay installer: on INTEGRATION's cleanup list; named in art request 2.
9. Opening hour: cached only once found.
10. ShiftHoursTests: flagged for FLOW's or the orchestrator's OK.
11. Recipes: U19 hall time-lapse, U20 left-36% false emitters, U21 slots at H5, U22 volume layer mask; the click's mixer group fixed in code and noted (decision 6).

## Verifier findings (round 2) and what answered each
1. **Overshoot (BUG-14c, BUG-51), medium.** The lamp now fills only the room's deficit, so the level is ≤ 1 at every minute for any knobs. Offline: `TheLevel_NeverPassesTheFullDay_AtAnyMinute` (every minute, warm-up shares 0 to 1, three knob sets, the verifier's 16:03 case). U9 and U14 gain captures at 16:05, 16:30, 16:35 and 16:45. The cap would have made a 16:03 switch-on invisible (about +2 luma), so the lamp now switches on at the sunset, 16:30 (+9.3 luma; decision 10, with a gate line).
2. **Built-in night in linear space, low.** The knobs are published as `.linear` vectors, and `HALL_DEFAULT_NIGHT` is the knob's linear value. The gate check and the seam test compare against the linear value.
3. **The finish in the rule, low.** Exposure, Neutral and contrast are modelled in URP's order (`DeskLighting.Finish`, `GradedLuma`), with a 2-luma margin, and round 1's knobs fail it (56.8). The vignette is modelled (`VignetteAt`) but not ruled on, for the reason given in BUG-14a; U3 divides it out and reports both values. A vignette fallback is offered and not taken.
4. **Flat pool, low.** The hue now falls off with the level (`_DeskLampTint`), and the radius is 1.35 m. The centre-to-corner step is 10.7 after the finish (round 1: 7.3, with no hue step). An offline test and the gate require ≥ 8, and U4 and U18 gain a by-eye pool line with the reviewer.
5. **ShiftHoursTests and DuskHour, open (process).** Unchanged: both still wait for FLOW's or the orchestrator's OK ("Needs the orchestrator's OK" items 1 and 2). The code stays at 18.5.
6. **Untracked `_TimeDesk*.cs` (environment).** Left untouched. They block the Unity pass (see RESULT, "Before the Unity pass").

## Feel-bible rows (acceptance)
LIGHT owns no feel card (Appendix). Its parts of other rows:
- **J5 / G3-shift-start:** no noon flash on load (25c). Recipe U5.
- **D3 Traveller:** photo and traveller lit alike (14c). Recipe U9.
- **B3 Rack:** no SSAO noise (16b). Recipes U13 and U11.
- **E1-E6 / Global:** bloom for the flare and the emissives (51). Recipes U10 and U14.
- **G3 flash safety:** the lamp's warm-up is one dim blink of 80 ms, then an ease-up. It is not a flash, and with reduced motion it is instant.

## RESULT

Everything that needs the editor or a built player is **deferred: Unity**. Saleh may be on the PC, so this track was code-only. Every recipe below runs in the NOPE-art editor through the job runner (`Library\ClaudeJobs\<name>.job`), or on a scratch built player with QA-RIG's driver where marked [BP].

**Before the Unity pass (blocker, not LIGHT's code):** the untracked `Assets/Editor/_TimeDeskHall.cs`, `_TimeDeskHallDays.cs`, `_TimeDeskHallSetup.cs` and `_TimeDeskHallShader.cs` (and their metas) in E:\unity\NOPE-art do not compile against this branch, with 12 errors. Examples: `_TimeDeskHallSetup.cs:17` uses the removed `HallLightingSO.deskLampDayShare`, and others use `DeskStampTray.BarOut`/`ToggleBar` and `DeskDocument.TryStampAreaOffset`. Assembly-CSharp-Editor would not compile in the editor, so DemoGate, QaJobs and the EditMode suite could not run. The orchestrator (or Saleh) moves them out of `Assets/` before U1. LIGHT left them untouched, and this round's offline compile excluded them.

Standard vocabulary: A1 1920×1080, A2 1280×720; H1 09:00, H2 12:00, H3 16:45, H4 18:30, H5 22:00; golden seed 12345; SEQ = 30 fps PNG + GIF.

- **U1. Import (deferred: Unity).**
  - Refresh the AssetDatabase.
  - Expected: `ShaderUtil.ShaderHasError` is false for `NOPE/Desk Anime` and `NOPE/Hall Deep Layout`; the console has no shader compile error; `OfficeFinishProfile` lists Tonemapping, ColorAdjustments, Vignette and Bloom; `OfficeContactShading.isActive` is false.
  - Open AnimeHall: the console shows no new error (the disabled `HallFourState.mat` renderers may show the missing-shader material; they are disabled; HallBakedLighting's stale `material:` entry is ignored).
- **U2. EditMode suite and the gate (deferred: Unity).**
  - Run TestRunnerApi EditMode. Expected: all green, except the known third-party `PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly`. It includes `LightSeamTests` (8), `LightChecksTests` (8), `DeskLightingTests`, `HallDayCycleTests`, `HallBakedCycleTests`, `ShiftHoursTests`.
  - Run `QaJobs.Gate` (DemoGate, merge mode). Expected: the seven `light.*` checks listed and green. `light.desk.readableAtNight` reports, after the finish (EV +0.1, contrast 12, Neutral, LDR), centre ≈ 68.9, corners ≈ 58.2 (before the finish ≈ 60.1) and a pool step ≈ 10.7. `light.desk.lampAtTheDip` reports the lamp on at 16:30 and the centre going from ≈ 62.7 to ≈ 71.7. `light.hall.nightIsShared` reports linear ≈ (0.0331, 0.0593, 0.1549).
- **U3. Time-lapse, the desk [BP] (deferred: Unity).**
  - Paths: A1 and A2; the office view and the reading view; day 12 played in sequence; cheat Time of Day or `ForcedHour` at H2, 15:00, H3, H4, 20:00 and 23:00.
  - Per capture, read `Shader.GetGlobalVector("_DeskLightTint")`, `_DeskLightLevel` and `_DeskLampPool`, and project the DeskSurface anchor's bounds to the screen for the mat rect.
  - Measure the mat's luma at its centre and in a 5 % inset at each of its four corners (not the mean of the rect). Record each sample's screen uv.
  - **The vignette is divided out** (decision 11): for each sample, compute `v = DeskLighting.VignetteAt(u − 0.5, v − 0.5, 0.2, 0.65)` (the profile's intensity and smoothness), decode the measured luma to linear, divide by `v`, and re-encode. Record both the measured and the divided value. The day capture at H2 gives the same factor at the same points: the day mat's corners should read ≈ 76 after the division.
  - **Pass:**
    - after the division, mat luma ≥ 55 at every hour at all four corners (model: 58.2 at night), and ≥ 60 at the centre at night (model: 68.9);
    - the measured centre and corners at H5 lie within 3 luma of the model's values once the vignette is divided out (otherwise the model is off: report the gap);
    - a passport readable at each hour (cap gate at 23:00);
    - the pool's centre (`_DeskLampPool.xyz`) inside the mat's world bounds;
    - the console shows no `[DeskLightLevel] At night, after the office finish, the mat reads ...` warning (the knobs pass on the real mat).
- **U4. Lamp switch-on (deferred: Unity).**
  - Day 12 (opens 16:00), clock at speed. SEQ from 16:00 to 17:00 game time (the plan's window), at 30 fps.
  - Expected switch-on at `DeskLighting.LampOnHour`, **16:30 with the round-2 defaults** (the sunset): the click plays and the pool warms over 0.6 s.
  - **Pass:**
    - a visible switch-on event at the dip: the pool's centre gains ≥ 5 luma within the warm-up (model: 62.3 → 71.6) and turns warm, by eye and by the frame diff;
    - **the pool reads as a pool** (round 2, item 4): at 17:00 and at H5, the centre-to-corner luma step on the mat is ≥ 8 (model: 9.8 at 17:00, 10.7 at night), measured with the vignette divided out (U3's method), and the reviewer agrees by eye that the lamp makes a warm pool with cooler corners, not a uniform warm tint;
    - no frame between 16:00 and 17:00 where the mat's centre is brighter than at H2 (the cap: no dusk brighter than the drawn day);
    - no click when the office opens already past the dip (day 12 jumped to 17:00 by the cheat: no click, lamp on at frame 0);
    - with the SFX volume at 0 in Settings, the click is silent (its source outputs to the Desk group).
- **U5. Load, no noon frame (deferred: Unity).**
  - SEQ of the office load at 13:00 (day 8) and 16:00 (day 12), each reached by playing in sequence and by the Jump cheat.
  - Log `HallLightingRig.Hour` in the first LateUpdate.
  - **Pass:** frame 0's hour equals the opening hour (13.0 or 16.0); no noon hall frame; no lamp strike flicker once the clock arrives.
- **U6. Dusk cheat (deferred: Unity; needs RUN-SAFE's DuskHour read).**
  - Compare cheat Dusk (`HallDayCycle.DuskHour`, 18:30) with a natural 18:30, both at A1, by eye and by image diff.
  - **Pass:** they match (the plan's line).
- **U7. Sun shadow (deferred: Unity).**
  - Reading view at H1 and H2.
  - Script: every enabled `Light` in the loaded office has `shadows == None`.
  - **Pass:** no diagonal band on the mat. Mat luma along the old lamp-arm shadow line is within ±3 of its neighbours.
- **U8. Left-pier diffuser (deferred: Unity).**
  - H5 capture of the hall at A1.
  - **Pass:** the diffuser above the left elevator pier (canvas x ≈ 620) is lit, with luma within 20 % of the mean of the other lit diffusers.
- **U9. Photo vs traveller (deferred: Unity; needs DOC-LOOK's DocumentCel include).**
  - Captures at H2, **16:05, 16:30, 16:35, 16:45**, H4 and H5 of the traveller and the passport photo (the dusk hours are where round 1 overshot).
  - Read back `DeskLightLevel.Live.Light`, and `DeskLight()` via `_DeskLightTint` × `_DeskLightLevel`.
  - **Also read back** one traveller layer's rendered colour: in a scratch scene in memory, a white 1×1 sprite on the traveller's material with `color = DeskLightLevel.Live.Light`, sampled from the camera's target. If it reads `Light` decoded from sRGB rather than `Light`, Unity linearises sprite colours. LIGHT's one-line fix is then to publish `Light` as `.gamma` (BUG-14c open risk).
  - **Pass:**
    - both read the same values, every channel ≤ 1 at every capture;
    - the face-region luma ratio photo/traveller at every capture stays within ±5 % of its H2 value.
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
  - Night capture (H5): lit diffusers, portals, the AVAILABLE and department signs, the CRT.
  - **Pass, LIGHT's part:** bloom present on the lit diffusers and the portals, by eye. Captures at H2, **16:05, 16:30, 16:35 and 16:45** (the dusk, where round 1's level reached 1.33): papers do not bloom, and `_DeskLightLevel` ≤ 1 at each. The UiContrast gate is still green with tonemapping on.
  - **Pass, the owners' part (needs HALL-FX's and UI-PC's HDR emission):** bloom present on the signs and the CRT. Until those land it is a known red owned by HALL-FX (signs) and UI-PC (CRT), not LIGHT.
- **U15. Note 12 (deferred: Unity).** Re-check note 12 against shot 13 and the audit's hour ladder, before and after.
- **U16. Art scene untouched (deferred: Unity).** After U3-U22, `git status` shows `AnimeHall.unity` and every `AnimeHallLayers/**` file unchanged, and no AnimeHallLayers `.meta` changed.
- **U17. OfficeGameplay (deferred: Unity).**
  - Build Office UI.
  - **Pass:** the semantic dump is identical to the base, since LIGHT changed no builder. The desk light and Volume are runtime objects under the rig, named "Desk light and office finish (made at runtime)".
- **U18. Tuning by eye at H5 with the adversarial reviewer (deferred: Unity).** The knobs, all in `HallLighting_Default.asset`:
  - desk: `deskNightFloor`, `deskLampLevel`, `deskLampRadius`, `deskNightTint`, `deskLampColour`, `deskLampOnAt` (re-run `light.desk.readableAtNight` and `light.desk.lampAtTheDip` after any change; keep floor + lamp ≤ 1, beyond which the cap only wastes the lamp);
  - **pool pass line:** the reviewer and LIGHT agree by eye that the lamp reads as a warm pool on the mat with cooler corners. The measured centre-to-corner step at H5 (vignette divided out) is ≥ 8 luma. If they want it stronger, shrink `deskLampRadius` only while the gate's corners stay ≥ 57;
  - the vignette fallback (decision 11): only if the vignetted corners must read ≥ 55 on screen, profile smoothness 0.65 → 0.3;
  - hall: `hallNightTint` (and `HALL_DEFAULT_NIGHT` in HallDeepLayout.shader with it: `light.hall.nightIsShared` holds them equal), `hallPoolStrength`, `hallEmissionGain`;
  - Bloom: threshold, intensity and scatter in `OfficeFinishProfile.asset`.
- **U19. Hall time-lapse, 25a (deferred: Unity; BUGLIST BUG-25's "hall time-lapse 16:00-22:00").**
  - The office view at A1, day 12 played in sequence; `ForcedHour` every 30 minutes from 16:00 to 22:00 (13 stills), plus a SEQ of 16:00-17:30 at clock speed.
  - **Pass:**
    - every lit fixture has a wall glow and a floor pool, and the floor away from the pools is darker than under them (luma under a pool ≥ 1.3× the floor midway between two pools at 20:00 and 22:00);
    - the painting's night share and the lamps switch on together (the stills where `HallDayCycle.Evening` ≥ 0.5 show night in the painting);
    - no frame where the floor is the brightest region at night (the old flat tint's symptom).
- **U20. No false emitters in the left 36 % (deferred: Unity; 25d).**
  - H5 capture of the hall at A1, and the same frame rendered with the old `step(_CanvasSize.x*.36, p.x)` restored in a scratch copy of the shader (not committed).
  - Diff the two in the left 36 % of the canvas (x < 782 px of 2172).
  - **Pass:** the only pixels that changed are the left-pier diffuser(s) and their glow (each changed blob lies on a bright neutral ceiling fixture of the reference painting, above `_CeilingCutoff`); no pale stone, sign or window blob newly glows.
- **U21. The slots at night (deferred: Unity; 25a with HALL-FX's slots).**
  - At H5 and H2, with the slots showing real art (a slot whose art is delivered; `HallSlotsDev` stand-ins off), capture the hall at A1.
  - Read `Shader.GetGlobalVector("_HallNightTint")` (alpha 1) and confirm no slot renderer's property block holds a `_Hall*` name.
  - **Pass:** across each slot's edge the painting and the slot share one tint (luma step at the slot's border ≤ 3 where the art itself is continuous); a wall glow or floor pool that the slot covers continues on the slot's art; no slot glows like a ceiling diffuser.
- **U22. The finish reaches the camera (deferred: Unity).**
  - In play, after the office loads: read the office camera's `UniversalAdditionalCameraData.volumeLayerMask` and `renderPostProcessing`, and the layer of "Desk light and office finish (made at runtime)".
  - **Pass:** post-processing on; the mask includes the Volume's layer; `VolumeManager.instance.stack.GetComponent<Bloom>().intensity` equals the profile's 0.6 while the office camera renders.
- **U23. No material gate (deferred: Unity; verifier item 1).**
  - In a scratch copy of the scene in memory (not saved), set HallBakedLighting's serialized `material` reference to None (or delete `HallFourState.mat` in a scratch import), enter play at H5.
  - **Pass:** the hall still shows the night pools, the left-pier diffuser and the daylight pose; `art.daylight.shadows` is None. Then discard the change (U16 still holds).

## FEATURES.md (for INTEGRATION to fold in)
- **Line 37 (the hall's lights).**
  - Replace "a downward spot under the green lamp's shade" and "the traveller's tint is multiplied by the cycle's shade" with the desk's one light: `DeskLighting`, `DeskLightLevel`, `DeskLight.hlsl`. Tested: `DeskLightingTests`.
  - Replace "the desk lamp first (`deskLampOnAt` 0.1, off by day, a filament's warm-up)" with: the work lamp's warm pool centred on the mat switches on at the afternoon dip (`DeskLighting.LampOnHour`, the sunset 16:30 with the defaults) with a click and a filament's warm-up. The lamp fills only what the room has lost, so the desk is never brighter than the drawn day. At night, after the office finish, the mat's corners keep luma ≈ 58 and the pool's centre ≈ 69, warm, with cooler corners. Tested: `DeskLightingTests.TheLampOnHour_IsTheAfternoonDip`, `TheSwitchOn_IsVisible`, `TheNight_KeepsTheWholeMatReadable_AfterTheFinish_ItsCornersIncluded`, `ThePool_ReadsAsAPool_NotATint`, `TheLevel_NeverPassesTheFullDay_AtAnyMinute`.
  - Add: the painting's four states on the one cycle (`HallBakedCycle`, night = the evening; tested: `HallBakedCycleTests`, `HallDayCycleTests.OneCycle_GivesTheHallLampsWindowsAndCalendarTheSameHour`).
  - Add: night pools under every lit fixture, shared by the hall's slots (the night knobs are shader globals); every ceiling diffuser lights, the left pier's included.
  - Add: the hall before the clock shows today's opening hour (tested: `HallDayCycleTests.TheShownHour_IsNeverNoonBeforeTheClockExists`).
  - Add: no real-time shadows in the cubicle.
  - Remove: the traveller's shade gradient from the knob list.
- **Line 172 (the hours grow).** "the hall's lights and baked four states" now read one cycle.
- **Line 255 (cheats).** "Dusk 18:30" is unchanged (it reads `HallDayCycle.DuskHour`, 18.5). This is RUN-SAFE's edit.
- **New line: the office finish** (BUG-51). One global Volume (tonemapping, a light grade, bloom for emissives, a gentle vignette; `OfficeFinishProfile.asset`, via `HallLightingSO.finishProfile`). The hall drawn in HDR so lit diffusers and portals glow (signs and CRT once HALL-FX and UI-PC emit in HDR). No SSAO (BUG-16b). Checked: DemoGate's `light.*` checks.
