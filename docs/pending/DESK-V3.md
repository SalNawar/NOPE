# DESK-V3: the desk's slide-in for real, and the painted-desk shader

Branch `feat/desk-v3`, cut from `fix/a-desk-plan` (DESK-PLAN #5, the desk home map: `fdde9f9`). **Feature freeze:** this branch is not merged until Phase A has landed, and it is never pushed. Owners: the slide-in is **DESK-HOMES** (Phase B B3; its leases on `DeskLayout.cs`, `DeskArrangement.cs` and `OfficeSceneBinder.cs` after #5); the shader is **DESK-ART** (`Assets/Art/Office/DeskPainted/**`). Sources: `E:\unity\NOPE-tools\design\desk\pipeline\UNITY_PLAN.md` sections 4.4 and 5 (sections 6.x, the switchboard, are obsolete: it is being redesigned as the Time Organ, so nothing of it is here), `render/slides.json` and `scripts/layout.py` `SLIDES`, the last slide review, `PHASEB/DESK-HOMES/SPEC.md` (P2, P5), `GAME_FEEL_BIBLE.md` (C6, G2), `docs/pending/CARVE.md` and `PHASEB/LEASES.md`.

**What it is:** the 2D previz is finished. This is the real slide-in in the game: when the cursor nears a machine or a tool on its home, only the nearest one slides toward the mat (a track) or rises in place (a lift), on the springs the offline gate passed, and goes home after the cursor leaves. Code only: no Unity was run (this step's rule), so every in-engine check is a recipe in RESULT below.

## Saleh's words

- **Desk v3 (DECISIONS_1019a):** "yes but they need to slide" (on `mock_office.png`: flat brass trays, the game's daters, the low camera, every machine in its home).
- **D2:** "I want the mouse to auto summon the machine when the player goes near it the machine will slide in a little bit to help the player ... everything should be animated and satisfying responsive and gives good feedback".
- On the 2D work: "why are you still working on the mock?" (so this builds the real thing).

## The three corrections from the last review (all in the data and tested)

| Correction | Where | Test |
|---|---|---|
| The way home arrives **at speed** (ease-in, cubic), so the seat bounce starts on the arrival frame | `SlideCurve.Back`; `slides.json` `back.ease` = easeInCubic | `SlideCurveTests.TheWayHome_ArrivesAtSpeed_AndTheSeatBounceStartsOnTheArrivalFrame`, `ProximityAssistTests.Back_AfterTheLinger_ArrivesAtSpeed_WithTheSeatClackOnTheArrivalFrame` |
| **Only the nearest slides**, and a hand-off starts the winner **only once the loser is nearly seated** (under 10% of its travel, about 101 ms into its 130 ms retract) | `ProximityAssist.Pick`, `Summon`, `Seated`; `winnerBelow` 0.1 | `ProximityAssistTests.HandOff_TheLoserRetractsAtOnce_TheWinnerStartsOnlyOnceTheLoserIsNearlySeated` (no frame has two sliders half out) |
| The **chip reader's overshoot stays 3 px clear of its lamp** | `DeskSlides.ApprovedSliders`: the chip reader travels 3.7 cm (at 4.5 cm it touched `lamp_chip` at its peak) | `DeskSlidesTests.EverySlider_KeepsItsLampsClear_TheChipReader3pxAtItsPeak` (the gate's own measure: 3 px at the peak, 6 px settled) |

`UNITY_PLAN.md` 5.1's table still prints the chip reader at 4.5 cm; `render/slides.json` and `layout.SLIDES` say 3.7 cm, and this build follows them.

## What was built

### Data: `DeskSlides` (Visuals, pure; DESK-HOMES)
`Assets/Scripts/Visuals/DeskSlides.cs`. The whole of `render/slides.json` as the existing desk config's own section: `DeskLayoutSettings.slides` (a section of `DeskConfigSO`, `Desk_Default.asset`; the asset holds no layout section yet, so the approved defaults load without an asset edit).
- **Knobs:** the trigger (120 px around the silhouette at 1080p, 40 px hysteresis, scaled by the screen height), out (250 ms, 40 ms anticipation), back (150 ms linger, 300 ms ease-in, 90 ms seat), the hand-off (130 ms retract, winner below 0.1 or after 130 ms), the felt gap (0.4 cm and 3 px), the lamp gap (3 px), the lift squash (1.5 px, 60 ms release) and shadow, the track shadow, the lamp pulse (+40% for 100 ms), the five sound ids, and the drop hold (below).
- **Sliders** (`DeskSlider`): the scanner 4.1 cm and the chip reader 3.7 cm left along their 6 cm tracks; the intercom 2.5 cm (its mic sways 4 px at 7 Hz) and the BABEL tuner 3.5 cm right; the UV torch, the glasses (7 degree tip toward the chair), the Rolodex, the protocol cards, both daters and the rulebook rise in place; DETAIN, AVAILABLE, the bell and the deal tray never move. Each has its own spring as the gate solved it (anticipation, overshoot, seat, damping, peak time, its px over the travel) and, for a track, its painted cut and the felt it shows at rest.
- **Rules:** `Problems(homes, reach)` (the builder's plan gate runs it: `DeskLayoutSettings.Problems`, so Build Office UI stops on bad slide data): an id twice; a slider with no home; a track not sideways, against its home's declared direction, beyond the 6 cm reach, past its felt gap or out of its painted track at the peak; a home declaring a slide with no track; a spring that does not overshoot once or whose damping disagrees with its overshoot; a winner that may wait longer than the retract. `FeltHolds` and `LampHolds` are the gate's clearance rules on a measure (the QA rig's slide gate will call them on the real meshes). `TrackTravel` is each track's travel for the plan's sweep. `Obstacle` is a home's swept box.

### Logic: `ProximityAssist` and `SlideCurve` (Visuals, pure; DESK-HOMES)
- **Nearest wins, one at a time** (`Pick`): the front-most silhouette under the cursor wins at once, whatever is out; else a home footprint under the cursor's desk point (the one out keeps it if it is among them); else the nearest within the trigger, the one out keeping it unless another is nearer by more than the hysteresis.
- **Hand-off:** the loser retracts at once (ease-in-out, no linger, no seat; the reversed shush); the winner waits until the loser is nearly seated. **Decision (mine):** a slider still going home after its linger also holds the next one back until it is nearly seated, so one machine moves at a time even when the cursor reaches the next machine after the first left (the prototype started the second at once).
- **Out:** the anticipation (a track backs away from the mat; a lift never dips, it squashes), then its own spring with one overshoot, settled within max(2%, 0.5 px) from 250 ms. **Back:** after the linger, the ease-in home and the seat bounce on the arrival frame.
- **Busy or occupied stays as it is:** out, it stays out (it never lingers home and never loses a hand-off); at home, it is not summoned; another empty machine may slide meanwhile.
- **Taken in hand** (`Release`): it gives way at once as a hand-off loser, so the hand lifts it from a pose that keeps moving.
- **Reduced Motion or Motion intensity 0:** every move is a one-frame cut (no anticipation, overshoot, seat, squash, sway or tip); the travel never scales; the cues still sound. **Decision (mine):** UNITY_PLAN 5.2's "a 120 ms cut both ways" is read as G2's cut (DESK-HOMES P5: "a one-frame cut"), with no delay before it.
- **Felt gap:** a track's position is clamped so it never goes past its felt gap (`DeskSlider.Reach`); the approved overshoots never reach the clamp (tested).
- **Cues** (`SlideCueKind`): the unlatch on the trigger frame, the shush as the base moves (40 ms), the knock at the peak, the reversed shush going home, the clack on the arrival frame, the hand-off. The out cues sound on time through a linger.
- **Pose** (`SlidePose`): the position, a lift's squash, the sway, the contact shadow (a track's leaves the well with the base; a lift's stays at its rest footprint, fading and widening), the shadow's press at the peak and the lamp's gain.
- `Assets/Scripts/Visuals/ProximityAssist.cs`, `Assets/Scripts/Visuals/SlideCurve.cs`. **Decision (mine):** they live in Visuals, not Domain as DESK-HOMES' spec proposed, because the slide data is checked against the home map (`DeskHome`, Visuals) and Visuals cannot see Domain.

### Unity: `DeskSlideIn` and `MachineSlideIn` (DESK-HOMES)
- `Assets/Scripts/Office/Desk/DeskSlideIn.cs`: one per desk. Each frame it finds every present slider under the cursor on the office camera (the front-most silhouette among the Interactable click boxes, the desk point in a home's footprint, the px to each silhouette at 1080p), steps the assist, sounds the cues (`Sounds.Play` of DeskSlides' ids; a machine without its clip yet plays `paper_slide` for the shushes and `drawer_close` for the seat, DESK-HOMES' stand-ins; a lift plays only its own clips) and raises `Cued` for every listener, and gives each slider its pose. While the PC is open (or `InputGate.Blocked`) nothing is near.
  - **Decision (mine): the drop hold.** A machine a paper was let go on stays out as if busy for `dropHoldSeconds` (1 s), while the paper lands on its bed or pad, until the machine's own busy state takes over. Without it, the usual "drop and go" (the cursor leaving right after the drop) sent the scanner home under a landing paper.
- `Assets/Scripts/Office/Desk/MachineSlideIn.cs`: one per slider; `MachineSlideIn.Attach(id, root, ...)` is the hook every owner binds through (UNITY_PLAN 5.3: BABEL-TUNER, GLASSES). It writes only its slide transform: the object's `Slide` child when it has one (the painted desk's prefabs: home root, slide child, mesh), else the object itself; its rest is where it stands when attached. A track moves along its axis in `DeskLayout.Frame`, a lift rises; a lift squashes (its base down) and tips (the glasses); a secondary part sways (the intercom's mic); a docked paper rides along (`Rider`). For an object whose owner poses it every frame (the UV torch's body, set by `UvTorch.Update`), the offset is a layer put on after the owner (LateUpdate) and taken off before it (an early Update), so the two never fight; the tip and the squash are always such a layer.
- **Bound by the office binder** (`OfficeSceneBinder.BindSlideIn`, after the desk is laid): the gameplay scanner when it stands on its home (an art office's own scanner stays put: its meshes are the art's), the chip reader (busy while it reads: `ChipReader.Busy`), the intercom only when it is the built stand-in (the art's phone is several moved parts: open call 1 of DESK-PLAN), the BABEL rack, the Rolodex and the protocol cards (`DeskLayout.ObjectOf`, new), and the UV torch's body as a layer that gives way when the torch is picked up (`UvTorch.Held`).
- **Obstacles:** `DeskLayout.RefreshObstacles` registers each home's object swept by its own slide (a track to its overshoot peak toward the mat, a lift's top raised by its peak: `DeskSlides.Obstacle`), so a carried paper never needs a live read (DESK-HOMES' carry-floor precondition).

### The plan (DESK-PLAN's data, DESK-HOMES' lease)
- `DeskArrangement.HomeProblems` sweeps each track by its own travel (`DeskSlides.TrackTravel`), not the 6 cm reach, and flags a travel past the reach (UNITY_PLAN 5.1: "No swept rect touches the mat or another home", as `layout.slid_rect` checks it).
- `DeskHomeMap`: the intercom's home declares `HomeSlide.Right` (desk v3 slides it 2.5 cm toward the mat; on today's map it stops 5 mm short of the mat). The DESK-PLAN test `TheMachines_SlideSidewaysTowardTheMat_AndOnlyThey` now counts it.

### The shader: `NOPE/Desk Painted` (DESK-ART)
`Assets/Art/Office/DeskPainted/Shaders/DeskPainted.shader` (UNITY_PLAN 4.4): `paint * lerp(1, Band(N.L hour) / Band(N.L ref), relight * G) * DeskLight() * shadow + paint * R * glow`, then the locked-home desaturation by `_Locked * B`. The paint is baked (UV `bake`); the band is a two-tone cel band; the reference light is the one the paint was painted under (`_RefLightDir`); the hour's light is URP's main light. It includes LIGHT's `Shaders/Include/DeskLight.hlsl` and never edits it. It takes the main light's cast shadow (moving objects cast; the shell's renderer casts none). The mask (`_MaskMap`, linear, behind `_MASKMAP`): R glow, G time-of-day light mask, B locked-home desaturation; without it, no glow, the full relight, nothing locked. Passes: Forward, ShadowCaster and DepthOnly, on one UnityPerMaterial buffer (SRP Batcher), as DeskAnime's. No material uses it yet: the textures and the importer are DESK-ART's next offline steps (UNITY_PLAN 12).

## Tests (outside Unity)

| Check | Result |
|---|---|
| `compile_check_any.py E:\unity\NOPE-art` | 0 errors; warnings as before (167 in Assembly-CSharp-Editor, none from these files) |
| Offline runner (`cc_art`) | passed 3780, failed 0 (3742 on `fix/a-desk-plan`, plus 38 new) |
| `tools/qa/editor_runner` (QA_PROJECT = NOPE-art; the SO-field-reader scan included) | passed 69, failed 0, 16 NeedsEditor skipped |
| `python -m unittest discover -s tools/qa` | 35 tests OK |

New EditMode tests:
- **`DeskSlidesTests`** (9): the approved slides are the pipeline's `slides.json` (its copy, `tools/props/desk/slides.json`, so the tests hold the gate's own numbers); the curves play the gate's 60 fps frames out and back (within 0.001); every track keeps its felt at the peak in cm and px, and the clamp never cuts the approved overshoot; every slider keeps its lamps clear, the chip reader 3 px at its peak; the machines keep the screen-px minimums or their cap; the sliders agree with the home map; `Problems` names every kind of fault; the obstacle is the swept footprint or a lift's raised top; the serializer's default is the approved set, a fresh copy each time.
- **`SlideCurveTests`** (8): a track backs away, overshoots once and settles from 250 ms (a lift never dips); the way home arrives at speed and the seat bounce starts on the arrival frame; the hand-off loser's first frame is 0.6 px and its fastest 4.1 px on the scanner, nearly seated about 101 ms in; the squash; the mic's sway; a lift's shadow; a track's shadow; the glasses' tip.
- **`ProximityAssistTests`** (17): the pick (silhouette first, footprint, nearest with hysteresis); out on the trigger frame, moving the next, the shush and the knock on time; back after the linger with the clack on the arrival frame; the hand-off; a slider going home holding the next back; the knock heard through a linger; re-entering a linger; busy machines; Reduced Motion cuts; the felt-gap clamp under a planted overshoot; the pose (squash, lamp pulse, shadows); a forced home; a lift taken in hand; a waiting winner the cursor leaves; fixed objects never move.
- **`DeskPaintedShaderTests`** (3, source text): it consumes the DeskLight seam and never redefines it; the relight, the cast shadow and the mask; the three passes and every knob in UnityPerMaterial.
- **`DeskArrangementTests`** (+1): a sweep is each track's own travel, never past the reach.

## Hand-offs (other owners) and what is not here

- **The switchboard** (UNITY_PLAN 6.x): obsolete (the Time Organ redesign); nothing built.
- **D6 framing focus** (`FramingFocus.cs`, CAM's seam): not in this step.
- **DOC-HANDLE (#8):**
  - expose the scanner's scan state (`DeskScanner.Scanning`, one getter): until then the binder reads the scan's own light (the art's `SweepBar` and the builder's `Spill`, shown only while a scan runs; `OfficeSceneBinder.ScanRunning`);
  - set `MachineSlideIn.Rider` while a paper lies docked on a slider (so a forced retract carries it: one mover) and clear it on undock.
- **FEEDBACK (#16):** draw the next-tool lamp with `DeskSlideIn.LampGain(homeId)` (+40% for 100 ms after its slider's peak when lit) or from `DeskSlideIn.Cued`.
- **Sound list (the steward):** the five new cues `machine_slide_unlatch`, `machine_slide`, `machine_slide_stop`, `machine_slide_back`, `machine_seat` (slides.json's cue lines describe each). Until they have clips, the machines play `paper_slide` and `drawer_close`.
- **DESK-ART:** the contact-shadow decal under each slider reads `MachineSlideIn.Pose` (`Shadow`, `ShadowBlur`, `ShadowWiden`, `Tight`); the painted prefabs keep the home root, `Slide` child, mesh structure; the textures, meshes, masks and `DeskPaintedImporter` (UNITY_PLAN 9, 12).
- **LIGHT (#11):** `DeskLight.hlsl`'s header still says nothing includes it; `NOPE/Desk Painted` now does.
- **STAMP (#7):** the daters attach themselves (`MachineSlideIn.Attach("stamp_approve" | "stamp_deny", dater)`), with the 5 degree tip toward the hand that is STAMP's B3; their data is here.
- **GLASSES (B8), BABEL-TUNER (B7):** attach the glasses and the tuner on their homes (`MachineSlideIn.Attach`); the BABEL rack slides until then.
- **The rulebook pocket** (DESK-HOMES P4): the book's lift attaches when the pocket is built.
- **The intercom:** it slides only as the built stand-in; the art's phone (several moved parts) stays until DESK-PLAN's open call 1 is answered or the A2 intercom lands.

## FEATURES (for INTEGRATION)

Per CARVE's and DESK-PLAN's rule, `docs/FEATURES.md` is not edited here. Add after the desk home map line:
- [ ] The desk's slide-in (desk v3, Saleh: "yes but they need to slide"; D2: "the machine will slide in a little bit to help the player"). When the cursor comes within 120 px (at 1080p, scaled) of a machine or a tool on its home, only the nearest one moves: the scanner and the chip reader slide toward the mat (4.1 and 3.7 cm), the intercom stand-in and the BABEL rack slide right (2.5 and 3.5 cm), the UV torch, the Rolodex and the protocol cards rise in place (1.5 cm). A silhouette under the cursor wins at once; the one out keeps it by 40 px. The out is a 40 ms anticipation then a spring with one overshoot, settled from 250 ms; 150 ms after the cursor leaves it goes home in 300 ms, arriving at speed with a seat bounce and a clack. A hand-off retracts the loser in 130 ms and starts the winner only once the loser is nearly seated (one machine moves at a time). A machine that is reading or scanning, or a paper was just let go on, stays out. The UV torch gives way when picked up. Under Reduced Motion every move is a cut. A track never passes its felt gap. (`ProximityAssist`, `SlideCurve`, `DeskSlides`, `DeskSlideIn`, `MachineSlideIn`; tested: `ProximityAssistTests`, `SlideCurveTests`, `DeskSlidesTests`.) Waits for its Unity check (docs/pending/DESK-V3.md, RESULT).

## RESULT

**Done outside Unity:** the slide data, the decisions, the curves, the runtime components and their binding, the plan's per-track sweep, the swept obstacles, the shader, and the tests above.

**Deferred: Unity** (this step ran none). Run in NOPE-art's editor through the job runner, in order. Each pass line is a QA-rig result or a frame sequence reviewed by eye (memory: "review motion myself").
1. **Import and compile.** `NOPE/Desk Painted` compiles with no error (`ShaderUtil.ShaderHasError` false), with and without `_MASKMAP`. The EditMode suite is green except the known `UnitySkills...SceneSummarize_CountsObjectsCorrectly`.
2. **Static gate** (`QaJobs.Gate`): `desk.plan.homes` green (now with the slide data: "21 homes, 0 problem(s)").
3. **Slide-in, each slider** (scanner, chip reader, BABEL rack, Rolodex, protocol cards, UV torch; day 12 with all items; A1 and A2; office and reading views): approach slowly, fast and wobbling for 10 s. Pass: the first slide frame within 1 frame of entry, the unlatch on that frame; the travel as the table; never past the mat edge, 0 px over a resting paper; no frame step over a quarter of the travel; one cycle at most while wobbling; never two sliders more than half out in any frame; the way home's last frame fast and the seat bounce on the next.
4. **Hand-off:** sweep from the scanner onto the chip reader's lid. Pass: the hand-off fires on the first frame the cursor touches the chip reader; the chip reader starts once the scanner is under 10% of its travel (about 7 frames later).
5. **Busy and drop:** drop a passport on the slid chip reader and leave at once. Pass: 0 retract frames while it reads; it goes home after the read and the linger. Drop a paper on the slid scanner and leave at once: it stays out until the scan ends.
6. **The UV torch:** hover, then pick it up. Pass: it rises 1.5 cm in its pouch with the squash; picked up, no frame jumps (the lift eases off as the hand lifts it); the torch's cone and pool unchanged.
7. **Reduced Motion and Motion 0:** the same approaches. Pass: one-frame cuts both ways, no squash or sway frames, the clacks still sound.
8. **Sound:** `Sounds.Fired` logs the five cue ids on time (the unlatch on the trigger frame, the shush 40 ms later, the knock at the peak, the reversed shush, the clack on arrival); the stand-ins play for the machines.
9. **Docking regression** (DOC-HANDLE's drop matrix and the chip reader AABB test with the machines slid, mid-slide and mid-retract): 8 px, 0 backward, 0% scale.
10. **My own look** at every frame sequence before it reaches Saleh.
