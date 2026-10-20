# STAMP (Phase A, merge #7)

Branch `fix/a-stamp`, cut from `fix/p0-cuts` (5aecb60). Sources: FIX_PLAN.md §3 "STAMP" and §0, FIX_PLAN_v2.md "STAMP: delta" (v2 wins), BUGLIST.md (BUG-15, 16, 40, 55, 73), DECISIONS_1019a.md, GAME_FEEL_BIBLE.md rows B1-B4, PHASEB/LEASES.md.

**What it is:** the stamp rack and the daters, fixed so the stamp lands where the player aims, with ink, on a rack that sits still at its home and looks like the cel desk around it. BUG parts: **15a, 16a, 40a, 55b (dater), 73b**.

**Code only.** Nothing here ran in Unity (Saleh may be on the PC). Everything that needs the editor or a built player is listed under RESULT as "deferred: Unity", with its recipe. `docs/FEATURES.md` is not edited (INTEGRATION folds this file in; the lines to rewrite are under "FEATURES and cues").

## Saleh's calls this track builds (quoted; FIX_PLAN_v2 rule 7)

- **D4 (DECISIONS_1019a.md, "Decided earlier"):** "a fixed stamp rack (desk plan v2)". FIX_PLAN_v2 STAMP delta: he "rejected the slide-out bar on 2026-10-08", so the rack never hides or slides; the pass line is "rack pixels over any paper = 0 in every frame, and the TAB/STAMPS label does what it says or is gone".
- **D2 (standing rule):** "everything should be animated and satisfying responsive and gives good feedback".
- **Everything diegetic (memory):** actions are in-world desk objects, never screen-space buttons; no step that only confirms a decision already made.
- **Saleh 2026-10-06 (in the code's history):** "the stamp mark must land exactly where the stamp is pressed"; 1016a: stamping "should be dragging".

Everything else below marked **proposal (ours)** is our reading, not his rule.

## What changed, per BUG id

### BUG-15a: the rack's behaviour and TAB (`DeskStampTray.cs`)
- The rack is fixed (D4): it is laid once on its home (`DeskStampTray.Lay`, bay `DeskLayoutSettings.stampRack`) and never moves, slides or hides. Moving the home off the mat is DESK-PLAN's 15b.
- **TAB and the STAMPS tab now do what they say** (proposal, ours): they take the player to the stamps. That is the reading view, which frames the rack (CAM's 7b and LensFit keep every in-use tool in frame), and both daters answer with a lift in their cradles (`Present`, a kick of the B3 hover lift). TAB again goes back. The tab still shows only in the office view. The tab is kept, not cut: its existence and placement are CAM's chrome (BUG-59), the FTUE basics already say "{stamps} or the grey STAMPS tab: the stamps", and GuideDirector points at it. The label is unchanged (its string and kit skin belong to TEXT-LANG/CONTENT).
- The class doc no longer describes a drawer that slides, a bar that comes out, or a "rack hides" state.

### BUG-16a: cel brass and a contact shadow (`OfficeSceneUIBuilder.Drawer.cs`, `.StampRack.cs`, `DeskStampTray.cs`, the rack materials, `BrassDrawer_Brass.png`)
- **Material.** `BrassMaterial` now makes the rack's brass in **NOPE/Desk Anime**, not URP Lit: two tones (band softness 0.02, the midtone lifted to 1, shadow value 0.5), a painted highlight tinted by the brass (`_Metallic` 1, strength 0.35 on the bright brass and 0.2 on the dark), a contour (`_EdgeStrength` 0.22), and LIGHT's seam properties `_PaintedHighlight` and `_InkRim` set to 1. They are read by nothing until LIGHT (#11) fills them; the style is judged then, per §2. The values are re-applied on every Build Office UI, which converts the existing `Drawer_Brass.mat` and `Drawer_BrassDark.mat` in place (same GUIDs, so the scene's references hold). The colours are proposals (ours): bright brass (0.88, 0.68, 0.31), dark brass (0.6, 0.43, 0.21).
- **Texture dropped** ("mipmapped or dropped"): the 256 px brushed photo texture (no mipmaps, the "crawl") is deleted, `BrassDrawer_Brass.png` and its meta. `Drawer_Brass.mat`'s two references to it are cleared now (`m_Texture: {fileID: 0}`), so no asset points at a missing GUID before the Unity pass rebuilds the material. No DOC-LOOK importer row is needed.
- **Contact shadow.** The builder adds `StampRack/ContactShadow` (MeshFilter and MeshRenderer, DOC-LOOK's `Paper_Shadow` material via `ShadowMaterial()`, inactive). `DeskStampTray.Lay` draws it under the rack box's footprint with the papers' own shadow maths (`PaperDepth.Shadow`, the cel kit's ink, `DeskConfigSO.shadowSpread`/`shadowOffset`/`shadowAlpha`), 0.4 mm over the desk. This reuses the existing shadow; there is no second shadow system. The rack still casts no light's shadow (the long dark shapes under the hall's low light).
- The SSAO reseed (16b) is LIGHT's. This track only checks the result (RESULT, R3).

### BUG-40a: aim, die depth and ink (`Domain/StampTarget.cs` new, `DeskStampTray.cs`)
- **Aim.** The press now picks along **the pointer's ray** that the carried dater last aimed by. The die hangs straight above the spot that ray met, so the ray is what the player sees under the die. Before, the press picked along a vertical ray from a die held at a fixed height over the desk plane, which could meet a different thing. `StampTarget.Pick` takes the top-most thing (the nearest along the view). It **passes over the rulebook folder when the ray meets it within `folderEdge` (2 cm, a serialized knob on the tray; proposal, ours) of its outline and a document lies beyond it**, which is the day-12 case. The aim during the drag uses the same `Pick`, so the die hangs over the paper it will land on. When the carry was clamped away from the pointer's spot (more than 2 cm off), the press falls back to straight down from the die.
- **Depth.** The stroke now drops the die exactly onto the surface it met (`StampTarget.StrokeDrop(dieHeight, surfaceHeight)`, measured along the desk's normal). Before, it dropped a fixed `stampHover − 3 mm`, so it sank about 3 mm into a stack and 7-8 mm into the folder. The 3 mm `PressFloor` constant is gone.
- **Ink.** Each dater owns a `StampInk`. The print is owed from the press that records the verdict (`StampFlow.Press`), and it lands exactly once: when the stroke bottoms out, or at whatever cuts the stroke first (`EndStroke`, `HandBack`, `BeginCase`/`EndCase`). It always lands on the paper it was pressed on.
- **No re-grab mid-stroke.** A dater takes input only while live, with its cradle locked **and no stroke running** (`Usable`). Its DeskDraggable is disabled from the press until the stroke ends, so a re-grab can no longer cut a stroke.
- **The bare desk** (proposal, ours, from `StampPress.Nothing`'s own doc, "it thumps on the bare desk", and feel row B4): a let-go over the bare desk now strokes the die down onto the wood. It plays `stamp_miss` (the refusal's thunk at pitch 0.8 until the clip is delivered), prints nothing, gives no hit, and the dater goes back.
- **A refusal** (another paper, an already-stamped passport, the folder) now bounces the dater 6 mm **up off the paper** (B4, Elastic) instead of shaking it sideways. The note and `ui_error` are kept.

### BUG-55b: the dater is IHoldable (`DeskStampTray.cs`)
- Each dater (`Handle`) implements `IHoldable`. Picking it up calls `HoldRegistry.Desk.Begin`. A let-go or a put-back calls `End`. `CancelHold` cuts the drag (`DeskDraggable.Cancel`), and the dater goes back unpressed. DOC-HANDLE (#8) wires focus loss and Esc to the registry; from then on Alt+Tab with a dater held cancels it instead of stamping.
- "Drag is disabled mid-stroke" (§3 Consumes): see 40a.

### BUG-73b: the invisible drawer slide, dead drawer cues, stale rack comments (`Domain/DrawerSequence.cs`, `DeskStampTray.cs`, `.Drawer.cs`)
- `DrawerSequence` is now the cradles only. The slide is gone: Travel, the carry, the stop, the shove, `ShoveCurve`, and the `Carried`/`Stopped`/`Shoved`/`Seated` events (not serialized; the remaining flags keep their numbers). The DENIED cradle starts to stand on the case's first frame, where the slide used to hold it back `drawerCarrySeconds × drawerRaiseFrom` = 0.34 s every case. APPROVED follows `drawerRaiseStagger` later. Folding, the hold while a dater is away, Reduced Motion and reversals are as before.
- The stale comments are rewritten in every STAMP file. Dead knobs and cues in other tracks' files are on the cleanup list below.

## Feel rows B1-B4 (GAME_FEEL_BIBLE §3B; numbers are the bible's proposals)

| Row | Column | State on this branch |
|---|---|---|
| B1/B2 | IR | Grab on the press frame; the stamp lands at the pointer's spot; a re-grab can't cut the stroke or lose the ink (40a). |
| B1/B2 | AN | **New:** the dater rises `stampLift` 25 mm over `stampRiseSeconds` 70 ms (out-cubic), then slams on the Dater spring. These knobs had no reader before. |
| B1/B2 | SS | **New:** the body squashes `stampSquash` (0.16) about its foot on impact and recovers on `stampFeel` (Firm, another knob that had no reader). The 12 mm `daterCompress` sink is unchanged. |
| B1/B2 | WP | **New:** the handle's 2° follow-through wobble as the stroke springs up (`daterReleaseFeel`). |
| B1/B2 | AU | **New:** `stamp_lift` plays on pick-up (bank only; silent until the clip is delivered). The press and release clacks are unchanged. |
| B1/B2 | HS, CS | FEEDBACK's: `FeelDirector.Punch(FeelHit.Stamp)` is still one value (75 ms). The bible's 60 ms and its ≤1.5 px down shake are FEEDBACK's §1.3 table (gap 4). |
| B1/B2 | PE, SF, TR | Not built. Ink specks, the paper-white pop and the slam smear have no owner or system (bible gaps 1 and 5, FEEL-KIT). Filed below. |
| B3 | IR, AN | **New:** the dater under the pointer lifts 3 mm in its cradle (Balanced). Within 6 cm of the hand it tips toward it, up to 5° as the hand nears. Only while it takes input; still under Reduced Motion. |
| B3 | WP, SS | **New:** the way home is a Firm spring (`stampFeel`), not Elastic. On arrival the dater seats with a 1.5 mm dip on the Cradle spring and a click. |
| B3 | AU | The seat plays the cradle's lock click (`dater_wheel_click` at pitch 0.62) **until `rack_seat` (NEW) exists**. That needs SoundCues (FEEDBACK) and SOUND_LIST (INTEGRATION). `drawer_open`/`drawer_close` are no longer meaningful and were never played. |
| B3 | AC | Cel brass, contact shadow (16a); no SSAO noise is LIGHT's 16b, checked here (R3). |
| B4 | all | **New:** a vertical 6 mm Elastic bounce off the paper on a refusal, `ui_error`, and `stamp_miss` on the bare desk. LANGUAGE's turn-away rule (Phase B) reuses the refusal. |

## Tests

**EditMode, offline-runnable (TimeDeskEditMode):**
- `StampTargetTests` (new, 7 tests): the top-most along the view whatever the list order; a folder edge passed over for the paper under it; the folder's middle taken (refused there); a folder edge taken with nothing under it, and a paper lying on the edge taking the stamp; nothing (and null) is -1; the die ends within 0.5 mm of the surface for a sheet, a stack and the folder, and never drops below; `StampInk` lands an owed print exactly once, also when cut before it bottoms out. Red first (compile error), then green.
- `DrawerSequenceTests` (rewritten; owner STAMP):
  - **added** `ACaseOpening_StandsTheDeniedCradle_OnItsFirstFrame_NoIdleDelay` (BUG-73b);
  - **deleted**, because they pinned the cut slide: `Opening_SetsOffSlowly_Accelerates_AndHitsTheStopOnTime`, `TheStop_OvershootsALittle_ThenSettlesOut`, `TheShove_IsFastAtFirst_AndStillMovingAsItSeats`;
  - **rewritten without the slide:** `Closing_FoldsApprovedThenDenied_AndTheyTakeNoInput` (was `…_ThenShoves_AndSeatsStillMoving`), `ADaterStillAway_HoldsTheFold` (was `…_AndTheDrawerOut`), `ReducedMotion_Snaps_AndStillSaysWhatHappened` and `AReversal_PicksUpFromWhereItIs`;
  - unchanged: `Cradles_StandDeniedThenApproved_…` (minus its slide-timing line) and `PropArt_UsesTheModel_OnlyWhenItCarriesEveryPart`.

**Editor-side (Assembly-CSharp-Editor, `Assets/Editor/StampTests/DaterStrokeTests.cs`, `[Category("NeedsEditor")]`, runs in Unity's EditMode suite):**
- `ACarriedDater_IsTheHold_AndMidStrokeTakesNoGrab`: a bare tray with one dater. A pick-up is the desk's hold. `HoldRegistry.Desk.Cancel` drops it back unpressed. A let-go ends the hold and strokes onto the bare desk (`StampPress.Nothing`, no verdict). Mid-stroke the dater's drag is disabled. It needs Unity objects and physics, so it is deferred (R1).

The PlayMode or harness test from §3 ("re-grab mid-stroke does not change the recorded verdict without ink") needs a real passport (`DeskDocument`) and QA-RIG's rig, which is not on this branch's base. Its two halves are covered by `StampInk` (pure) and `DaterStrokeTests` (no grab mid-stroke). The full case runs as the QA-RIG scenario in R4.

| Check | fix/p0-cuts (baseline) | fix/a-stamp |
|---|---|---|
| `compile_check_any.py E:\unity\NOPE-p5` | 0 errors, 161 warnings | 0 errors, 161 warnings |
| Offline runner (`cc_p5`) | passed 3710, failed 0 | passed 3715, failed 0 (−3 slide tests, +1 drawer, +7 StampTarget) |

## Cleanup list (dead after this track; each owner deletes at merge, per FIX_PLAN §0 rule 2 and BUG-73)

- **MotionKnobs (DOC-HANDLE, 73c) and `MotionTuning_Default` (FLOW):** `drawerCarrySeconds`, `drawerCarryPower`, `drawerStopCarry`, `drawerRaiseFrom`, `drawerShoveSeconds`, `drawerShoveKick`, `drawerStopHit` (never read), `drawerFeel`, and the `drawer` tuning (the `MotionFeel.Drawer` enum value stays, because serialized enums are append-only). Also fix their docs, which still cite `DrawerSequence.ShoveCurve`. `stampSlamSeconds` is still unread: the slam is the Dater spring.
  - The knobs **now read** are `stampLift`, `stampRiseSeconds`, `stampSquash` and `stampFeel`. QA-RIG's `so_reader_allowlist.txt` needs this delta at merge.
- **SoundCues (`Domain/HallAmbience.cs`, FEEDBACK):** `DrawerOpen`, `DrawerClose`. SOUND_LIST's `drawer_open`/`drawer_close` rows (INTEGRATION) and the bible's B3 note "drop drawer_open/close" say the same.
- **Stale comments in others' files:**
  - `BoothRules.cs:71` ("false slides the bar back") and :123 ("stamp bar"): DESK-PLAN.
  - `DeskConfigSO.stampHover` ("while the bar is out"): DOC-HANDLE.
  - The "Stamps (Papers, Please's stamp bar…)" header: DOC-HANDLE.
- **Art tool:** `tools/props/brass_drawer.py` lines 111-123 still write `BrassDrawer_Brass.png`. Re-running it would bring back an unused texture. This is for the art side; the script is not in any track's Owns.

## Requests to other owners (seams this track consumes)

1. **FEEDBACK:**
   - a `rack_seat` cue in SoundCues (and the placeholder case in CueSounds), then swap it into `DeskStampTray.Seated`;
   - the §1.3 hit-stop of 60 ms for the stamp (`FeelHit.Stamp`).
2. **DOC-HANDLE (#8):**
   - a shared desk instance of `DeskObstacles` (the seam has no `Desk` instance yet). STAMP then registers the rack's footprint and height from `Lay`, so carried papers keep over it (BUG-8);
   - wire `HoldRegistry.Desk.OnFocusLost` and Esc. The dater is ready for both.
3. **DESK-PLAN (#5):** the rack's home off the mat (15b). R2 measures it.
4. **LIGHT (#11):** read `_PaintedHighlight` and `_InkRim` in DeskAnime. The rack's materials already set both to 1.
5. **FEEL-KIT or FEEDBACK (bible gaps 1 and 5):** B1's ink specks, the paper-white pop and the slam smear.

## FEATURES and cues (for INTEGRATION)

Rewrite `docs/FEATURES.md` line 60 (the stamp rack) as follows:

- **The rack.** It is fixed at its home off the mat (D4), never slides or hides, and has a drawn contact shadow (`PaperDepth.Shadow`). Its brass is cel brass in NOPE/Desk Anime: two tones, a painted highlight, the ink-rim seam, no texture. It no longer uses URP Lit with metallic 0.55 and smoothness 0.72.
- **TAB.** TAB and the STAMPS tab take the player to the stamps, the reading view, and the daters lift to answer. "The art's old desk stamp" is gone (P0-C).
- **The cradles.** They stand on the case's first frame (no slide; tested: `DrawerSequenceTests.ACaseOpening_StandsTheDeniedCradle_OnItsFirstFrame_NoIdleDelay`). A dater takes input once its cradle is locked and while it is not mid-stroke.
- **Answering the hand.** In the rack a dater lifts 3 mm under the pointer and tips up to 5° toward a hand within 6 cm. It goes home on a Firm spring and seats with a dip and a click.
- **The press.** It picks along the pointer's ray (`StampTarget.Pick`, a folder's edge within `folderEdge` 2 cm passed over for the paper under it; tested: `StampTargetTests`). The die drops exactly onto the surface it met (`StampTarget.StrokeDrop`; tested), after a 25 mm, 70 ms rise. The body squashes `stampSquash` on impact, and the handle wobbles 2° on the way up.
- **The ink.** It is owed from the press and lands exactly once, even when the stroke is cut (`StampInk`; tested).
- **Refusals and misses.**
  - A refusal bounces the dater up off the paper (6 mm Elastic) instead of shaking it.
  - Over the bare desk the die thumps the wood (`stamp_miss`) and the dater goes back. It no longer "just goes back".
- **The hold.** A carried dater is the desk's hold (`HoldRegistry`; checked: `DaterStrokeTests`, editor).

Line 191 (the cues the game plays):
- `stamp_lift` (pick-up) and `stamp_miss` (bare desk) are now played;
- `drawer_open` / `drawer_close` are not played (they never were) and go with the cleanup.

## StampChecks (DemoGate) and the stamp scenario: added on top of QA-RIG

QA-RIG (#2) owns `GateCheck`, `GateResults` and the scenario runner. Its branch (`fix/p0-qa-rig`) is not in this branch's base (`fix/p0-cuts`), so `Assets/Editor/QA/Checks/StampChecks.cs` cannot compile here. When main has QA-RIG, add it with this content:

```csharp
/// <summary>STAMP's gate checks (BUG-15a, 16a, 40a; QA-RIG §5).</summary>
public static class StampChecks
{
    /// <summary>30/30 dater strokes land on the aimed paper, no sink, no ink/verdict mismatch (stamp_aim.json).</summary>
    [GateCheck("stamp.aim", "STAMP", GateKind.Play)]
    public static GateResult Aim() => GateResults.Require("stamp.aim", "STAMP", "stamp_aim.aim.*");

    /// <summary>Rack pixels over any paper = 0 in every frame of a full case at A1 and A2 (stamp_case.json, the rackOverPaper op).</summary>
    [GateCheck("stamp.rackClear", "STAMP", GateKind.Play)]
    public static GateResult RackClear() => GateResults.Require("stamp.rackClear", "STAMP", "stamp_case.rackOverPaper.*");

    /// <summary>10 s on the rack at H2 and H5 on the QA player: flicker pass (stamp_rack.json).</summary>
    [GateCheck("stamp.rackNoise", "STAMP", GateKind.BuiltPlayer, DemoOnly = true)]
    public static GateResult RackNoise() => GateResults.Require("stamp.rackNoise", "STAMP", "bp.stamp_rack.seq.rack*.flicker", true);

    /// <summary>The rack's brass is cel (NOPE/Desk Anime, no texture) and the old brushed texture is gone.</summary>
    [GateCheck("stamp.brass", "STAMP", GateKind.Static)]
    public static GateResult Brass()
    {
        string[] mats = { "Assets/Art/Office/Gameplay/Materials/Drawer_Brass.mat", "Assets/Art/Office/Gameplay/Materials/Drawer_BrassDark.mat" };
        var bad = new System.Collections.Generic.List<string>();
        foreach (string path in mats)
        {
            var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
            if (m == null || m.shader == null || m.shader.name != "NOPE/Desk Anime" || m.GetTexture("_BaseMap") != null)
                bad.Add(path);
        }
        if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/Art/Office/Props/BrassDrawer/BrassDrawer_Brass.png") != null)
            bad.Add("BrassDrawer_Brass.png present");
        return GateResult.Of("stamp.brass", "STAMP", bad.Count == 0, bad.Count, string.Join(", ", bad));
    }
}
```

Along with it, STAMP adds (QA-RIG §4 STAMP row):
- the scenarios `tools/qa/scenarios/stamp_aim.json`, `stamp_case.json` and `stamp_rack.json`;
- a rig op `stamp.rackOverPaper` under `Assets/Tests/PlayMode/STAMP/`. It is registered through `QaRunner.RegisterOp` and asserts, in every frame, that the rack's screen rect (from the `StampRack` box bounds) intersects no paper's rect.

## RESULT

Done outside Unity:
- compile_check: 0 errors, 161 warnings (unchanged).
- Offline runner: passed 3715, failed 0.

Each item below is **deferred: Unity**, to run in one batch on the NOPE-p5 editor (job runner, `Library/ClaudeJobs`). Before any scene work, run the `__EMPTY_SCENE__` job.

- **R0. Build and import.** deferred: Unity.
  1. Refresh: the deleted `BrassDrawer_Brass.png` reimports nothing, and no console error names its GUID.
  2. Run `Tools > TimeDesk > Build Office UI`.
  3. Pass:
     - `Drawer_Brass.mat` and `Drawer_BrassDark.mat` now use NOPE/Desk Anime, with no `_BaseMap` texture, and keep their GUIDs (`git diff` shows the shader GUID and the properties change, not the `guid:` line);
     - OfficeGameplay's semantic dump against fix/p0-cuts differs only by `Office/StampRack/ContactShadow` (MeshFilter, MeshRenderer, Paper_Shadow, inactive) and the `contactShadow` reference on `StampBar/DeskStampTray`. INTEGRATION commits the scene; this track does not.
  4. Revert the csproj/ProjectSettings/LiberationSans churn.
- **R1. EditMode suite in Unity.** deferred: Unity. Run the whole suite (TestRunnerApi, EditMode). Pass: green, apart from the known UnitySkills failure. It includes `DaterStrokeTests.ACarriedDater_IsTheHold_AndMidStrokeTakesNoGrab` (NeedsEditor), `StampTargetTests` and `DrawerSequenceTests`.
- **R2. Rack in play (v2 pass line).** deferred: Unity.
  1. QA-RIG `stamp_case.json`: golden seed 12345, day 1, one full case (hand-over, read, stamp, hand-back) at A1 and A2, as a 30 fps SEQ, with `track` on `StampRack` and every paper.
  2. Pass: rack-over-paper = 0 in every frame (the rect op);
  3. TAB and the STAMPS tab, pressed in the office view, end in the reading view with the whole rack inside the frame, and both daters lift and settle within 0.5 s;
  4. TAB again returns to the office view.
  - Needs DESK-PLAN's 15b home. Before #5 merges, record the measured overlap as known red.
- **R3. Rack noise and style [BP].** deferred: Unity (built player).
  1. QA player, `stamp_rack.json`: 10 s at 30 fps on the rack at H2 (12:00) and H5 (22:00), then `seqtools.py flicker` and a per-pixel luma std-dev over the rack region.
  2. Pass: flicker pass, std-dev < 2/255 while static (both need LIGHT's 16b SSAO fix; before #11, record the number).
  3. By eye against a dater: two tones and a visible contact shadow, with no olive-grey at H5. The painted highlight and ink rim are judged at LIGHT's merge (#11).
- **R4. Aiming (40a).** deferred: Unity.
  1. QA-RIG `stamp_aim.json` with the real virtual mouse (P-SLOW, P-FAST, P-WOBBLE): day 12 by play. 10 tries each:
     - (a) a passport partly under the moved rulebook folder, aimed within 1 cm of the folder's edge;
     - (b) a passport on a 3-paper stack;
     - (c) fast re-grabs: press the dater again within 100 ms of each let-go.
  2. Pass, 30/30:
     - the stamp lands on the aimed document, with `LastPressPoint` within 3 mm of the pointer's spot on it;
     - 0 sink: at the contact frame, the die's lowest point is within 0.5 mm of `LastPressPoint`'s height;
     - 0 ink/verdict mismatches: every `HasVerdict` passport carries exactly one print of its verdict's colour;
     - in (c), the re-grab is ignored until the stroke ends (drag disabled), and no stroke ends without its print.
- **R5. Holds (55b).** deferred: Unity. Per BUGLIST 55, on video:
  1. With a dater held, press Esc, right-click, Q, A, TAB and Alt+Tab.
  2. Pass: the dater goes back unpressed and nothing is stamped (`LastPress` unchanged, no verdict). Alt+Tab passes only after DOC-HANDLE wires `HoldRegistry.Desk.OnFocusLost` (#8); before that, record it as known red with that reason.
- **R6. No idle delay (73b).** deferred: Unity. In `stamp_case.json`, log the frame the case opens (the passport known) and the frame `LockedDenied` fires. Pass: the cradle visibly starts on the first frame and locks in under 0.3 s (about 0.34 s faster than fix/p0-cuts, measured on both).
- **R7. Feel cards B1-B4.** deferred: Unity. For `R7\FIX\FEEL\B1..B4.md`:
  1. The clips `clip_1x`, `clip_025x` and `clip_rm` (Reduced Motion) of:
     - a stamp on the passport;
     - a refusal on a permit;
     - a miss on the bare desk;
     - the hover lift and tip in the rack;
     - the seat on return.
  2. Checks 1-12 and V as in the bible §5.1.
  3. Known gaps, outside this track: the B1 specks, smear and paper-white pop, the 60 ms hit-stop, and the `rack_seat` cue.
- **R8. Smoke.** deferred: Unity. Title → New Run → day 1, one traveller stamped and handed back, and a cheat jump to day 12 with one stamp. Pass: 0 console errors, and no new warnings against fix/p0-cuts.

## Decisions taken here (proposals, ours, not Saleh's)

1. **TAB with a fixed rack.** "Does what it says" = it takes the player to the stamps (the reading view framing the rack), and the daters answer with a lift. The tab is not removed: it is CAM's chrome, and the FTUE and the guide use it.
2. **The aim.** The press picks along the pointer's ray (what the player sees under the die). Before, it used a vertical ray from a die held at a fixed height.
3. **The folder's edge band** is 2 cm (`folderEdge`, serialized on the tray). FIX_PLAN says only "a folder edge".
4. **The bare desk** thumps and prints nothing, instead of the dater silently going back (B4; `StampPress.Nothing`'s own doc).
5. **The texture is dropped**, not mipmapped: the flat cel look needs none, and that leaves no import setting to ask DOC-LOOK for.
6. **The contact shadow reuses the papers' drawn shadow** (`PaperDepth.Shadow`, `Paper_Shadow`, DeskConfigSO's shadow knobs). There is no rack-only shadow system and no light's shadow.
7. **A dater still needs its cradle locked** before it takes input (~0.2 s after the case opens, now that the slide is gone). Grabbing a dater whose cradle is still turning was not made possible (G1 says animations never gate input; the cradle's turn moves the dater's parent, and a grab mid-turn is untested). Flagged for the feel review.
