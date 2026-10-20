# DOC-HANDLE: papers behave like papers on a desk

Branch `fix/a-doc-handle`, cut from `fix/p0-cuts` (5aecb60). Phase A track #8.

Sources:
- FIX_PLAN.md §3 "DOC-HANDLE" and §0 rules.
- FIX_PLAN_v2.md §3 "DOC-HANDLE: delta (D1, D3)" (v2 wins).
- DECISIONS_1019a.md: D1, D3, R3/R4 (Esc), R6 (click).
- GAME_FEEL_BIBLE.md rows A1-A3 and A6-A11 (this track's acceptance rows), plus gap 8 (`paperDropSquash`).

`docs/FEATURES.md` is **not** edited here (INTEGRATION's). The lines to rewrite are in "FEATURES".

## Saleh's calls built (not the superseded defaults)

- **D1** ("we are doing the camera tilt no need to enlarge the documents when you click them"):
  - The drop-triggered `DeskView.TiltIn` **stays**.
  - Papers keep **one world size per zone, whatever the view**. The reading view's tilt never regrows a paper.
  - There is **no click-to-enlarge**: no flag, and v1's step 2 is not built.
- **R6** ("clicking a desk paper brings it to the top of the stack"): a click on any paper brings it to the top, in the same frame, and does nothing else.
  - **Our reading:** this includes a paper on the counter. The counter's old click-to-read (it slid the paper to the desk, regrew it and tilted the camera) was the only click that enlarged anything, so it is gone.
- **D3** ("other documents other than solid id cards should feel a bit like paper"):
  - ID cards are rigid (`PaperPhysics.Rigid`, and `cardFlex` is 0).
  - Every other paper keeps its bend and its settle, now scaled by speed.
- **R3/R4 Esc:** while something is held, Esc (and right-click) lets go of it first. Otherwise Esc falls through to the existing back-out chain, which becomes FLOW's Esc→Settings (D20) at #15.

## What changed, per BUG part

### BUG-1a: one physical size, no camera dive on a drop (S1)

**The rule:** `PaperSizes.SheetScale(zone, counterScale, deskScale)` (Domain/DeskPapers.cs).
- A paper has two world sizes: the counter's (`counterScale` of its size) and the desk's (its reading size, `DeskZones.ReadingScale` × the art's reading share).
- No view, click or drop enters the rule.

**Removed:**
- `DeskPapers.ShownFullSize`.
- `DeskDocument.SetFocused`, `ShownScale` and `SizeIn`.
- `DeskController.Focus`, the `deskView.Changed` subscription, `ToDesk`, the reading spots, `Spread`, `TryReadingArea` and `LastSpread`.
- `PaperClickAction.Read`.
- `Visuals/PaperSpread.cs` and its tests. Its only caller was the click-to-desk.

**The one size change:**
- It happens only while held, as the pointer crosses the counter's edge (`DeskController.HandleDragged` → `DeskDocument.SetZone`).
- It eases over `resizeSeconds` (0.15 s; a cut under Reduced Motion).
- The held point stays under the cursor: `DeskDraggable.GrabScale` = `PaperSizes.GrabRatio`.
- A desk paper held over the counter before the verdict keeps its desk size, because it could not stay there. So a refused drop never changes size after release.

**Clearance:**
- It is judged at the paper's zone size (`DeskDocument.ZoneSize`, `RestCorners`), never the size it is easing from.

**The drop's camera move:**
- It is the D1 tilt only: `Read` → `DeskView.TiltIn`, and none when the reading view is already on.
- **Deferred to the QA-RIG merge:** tagging it `QaSignals.TagCamera("DeskDropTilt", …)` in `DeskController.Read`. QA-RIG (fix/p0-qa-rig) is not on this branch's base, and `QaSignals` does not exist here.

### BUG-6a: papers rest where released

**`Visuals/DropResolver.cs` (new, pure):**
- `Rest(footprint, area, keepOuts)` gives the release centre unless the rect would leave `area` or overlap a keep-out.
- When it would, it gives the least move clear of **all** keep-outs at once: a candidate grid over every keep-out's sides, clamped into the area, nearest first. Where nothing is clear, the least overlap.
- `Inside` keeps the whole rect in the area.
- `DockOf` documents the footprint-centre dock rule.

**`DeskController`:**
- `Rest`/`RestOf` builds the keep-outs in the office view's frame:
  - every `DeskObstacles` footprint;
  - the scanner's hidden area (`ScannerClearance.Blocked`);
  - for a counter refusal, the counter from its near edge on (`DeskCounter.TryNearEdge`, new).
- The area is the desk (centres anywhere the drag may take them) ∩ what the reading view shows (`DeskConfigSO.restViewport`, new) for a desk paper.

**Deleted:**
- `ClearOfBlockers`, `CarryFloor` and `DeskDocument.SetCarryFloor` / `_carryFloor`.
- The rulebook's `SetCarryFloor` / `_floor`.
- `ScannerClearance.Clear`, `ClearProp`, `Eject` and `Overlaps`. They were unreachable once the push went, so `ScannerClearanceTests` shrinks to `Shadow`/`Blocked`.

**The dock:**
- It is picked by the paper's root, which is its footprint's centre, not the pointer (`DockAt(paper.transform.position)`).
- **The zone is still the pointer's** (our decision). The size switches when the pointer crosses the edge, so the drop's zone is the zone the paper is already sized for. Judging the zone by the footprint would make the size flip back after release, breaking matrix (c).

**PaperPhysics:**
- No release slide (`SlideStart`, `OffsetX/Z`, `paperSlideSeconds`, `paperSlideMax`: removed).
- No drop spin (`DropSpin`, `paperDropSpinPerSpeed/Max`: removed).

**Refusals:**
- `DropOutcome.Refused` and `NotStamped`, and the chip reader's "no", shake in place: `PaperPhysics.Shake`, ±4 mm, 3 cycles, 200 ms (`DeskConfigSO.refuseShakeDistance/Seconds/Cycles`, new; `refuseShake` removed).
- They then rest just clear of what refused them. They never fly back to the pick-up.
- A cancelled drag (Esc, right-click, a view key, focus loss) still slides back to where it was picked up: it is a cancel, not a drop.

### BUG-6b: the folder (`DeskRulebook.Motion.cs`)

- **One mover:** `Update` stops a running carry (`DeskCarry.Stop`, new) the moment the folder is grabbed. Only the drag moves it.
- **No teleport on a counter drop:**
  - `Dropped` now only shakes the folder's "no".
  - `DeskController.RulebookDropped` lays it just off the counter, beside a refusing machine or clear of any prop (`RestOf`, then `Refused`/`RestAt`, which carry the folder the short way).
- **Held:** it is drawn above every prop (`ShowHeld`, HeldOnTop), and its swing pivots on the held point.

### BUG-8: no shared obstacle model

**`Visuals/DeskObstacles.cs` is the registry:**
- `DeskObstacles.Desk` (static).
- Footprints in **world x/z** (DeskRect x = world x, y = world z).
- Heights over the desk, plus `MaxHeight` and `Footprints(list)`.
- `DeskController.MeasureMachines` registers:
  - `desk.scanner`: the casing, as high as its open lid's sweep (`DeskScanner.LidSweepTop`, new);
  - `desk.chipReader`;
  - `desk.uvTorchCradle`, until TOOLS registers its own torch (see the cross-track list).

**Drawn on top while held:**
- **The mechanism (our decision):** `Visuals/HeldOnTop.cs` (new) moves the held sheet toward the camera along the camera's own rays and shrinks it by the same ratio, a homothety centred on the eye. Its picture is pixel-identical, but it is physically in front of every prop up to `max(DeskObstacles.MaxHeight, DeskConfigSO.heldClearance 0.16 m)`. The ratio is never under `heldLeastRatio` 0.3.
- **Why not a render layer:** a render-layer or ZTest approach needed the URP renderer asset or DOC-LOOK's shaders, which this track does not own.
- **The drop:** let go, the ratio is 1 again, with no change on screen.
- **Ordering:** `DeskDocument.ShowFeel` applies the homothety, re-drawn after each move of the root (`Followed`, from `DeskDraggable.Dragged`).

**Resting papers never lie under a registered prop:** DropResolver.

### BUG-11a: motion

- **Settle scaled by speed:**
  - `PaperPhysics.SettleShare(releaseSpeed, paperSettleSpeed 1 m/s)` scales the hop, the landing curl and the landing tilt-flutter.
  - A slow drop barely settles.
  - `PaperPhysics.Land(settle)` is also the hand-over's slap.
- **No squash:** `paperDropSquash` and `DeskDocument._squash` are removed. A paper never changes size, which closes the feel bible's gap 8 and G5.
- **Yaw while held:**
  - v1 says "no yaw while held"; the feel bible's A6 keeps the sway (16°/(m/s), ≤12°). **Our reconciliation:** the swing stays but **pivots on the held point** (`PaperPhysics.PivotOffset`). The point under the cursor never moves (A7 "glued at the grab offset"), and after a drop it springs back to square about that same point, so the rest pose does not move.
  - The adversarial reviewer should judge this one by eye. If it reads as a yaw, set `paperSwayMax` to 0 in MotionTuning_Default (FLOW's asset).
- **ID cards rigid (D3):**
  - `PaperPhysics.Rigid` (set in `DeskDocument.Bind` for `PaperKind.Card`): no bow, twist or ripple, and a hop ≤ `cardDropHop` 1 mm.
  - `MotionKnobs.cardFlex` defaults to 0.

### BUG-24: machine docking

- **Upright on the pad:** a passport lies upright to the player on the reader's pad (`ChipReader.FaceThePlayer`, set from `DeskController.SetScannerView`). `DockFacing(Pad)` no longer returns the slot's way.
- **Fits the pad:**
  - It is sized to fit: `DockFit.Scale(paper, pad extents along the view's axes, counter scale, padOverhang 0.08)`, with `ChipReader.PadExtents` and `padSize` (0.12 × 0.092 m, `chip_reader_model.py` PAD_W/PAD_D, a new serialized field with that default).
  - **No portrait pad is modelled:** with the fit it isn't needed, so `chip_reader_model.py` is unchanged.
- **Docked size until clear:** `Undock` now runs in the carry-out's landing callback (it used to run before the carry, which regrew the paper on the pad). The scanner works the same way (`CarryOut`).
- **Dock state:**
  - It is the case's: `MachineOccupancy` (Domain/MachineDocks.cs, new) replaces `_docked`/`_inReader`, is cleared in `BeginCase` and `SendBack`, and the lift reads `DeskDocument.IsDocked`.
  - A new case's paper with the same index is never taken for docked.
  - **No queue steals:** `Landed` (the Auto-Feed's readiness) requires `MachineOccupancy.FreeToFeed`.
- **The lid:**
  - `SendBack` lets go of the lid (`HoldOpen(false)`, `SetNear(false)`).
  - `DeskCarry.Start` on a running carry, and `Stop`, run the replaced carry's **cut** callback. `StartScan` passes the lid's let-go as its cut. Before, the replaced callback was dropped and the lid stayed held open.
- **The lid's swept volume** is in DeskObstacles as the scanner's height, so a held paper is drawn above the open lid.

### BUG-40b: stack order, top lift

- `ApplyStack` lifts the top of the stack `DeskConfigSO.topLift` (3 mm) more.
- A reorder shows in the same frame: every resting paper's lift is set at once (`DeskDocument.SetLift(h, instant)`). Only the paper just let go and the dragged one ease.
- Each paper whose rank changed raises `DeskEvents.StackChanged(paper, rank)` for DOC-LOOK's contact shadow (40c).

### BUG-49b: the staged hand-over

- Each paper slides from the hand-over point to its counter spot in `DeskConfigSO.handOverSeconds` (0.42 s; range 0.35-0.5).
- It lands with a slap: `DeskDocument.Land`, a full landing curl and hop, with `DeskEvents.DocumentLanded` raised on the landing frame for FEEDBACK's sound.
- Arrivals come one per beat (`handOverStagger` 0.08 s, at most one per frame).
- **No single-frame appear:** a paper appears at the hand-over point and moves from its first frame.
- **The counter spot** is resolved like any rest: clear of obstacles, on the desk.

### BUG-55a: holds

- **Every desk drag holds** through `HoldRegistry.Desk`: `DeskDraggable` implements `IHoldable`, so papers, the folder and the daters are all covered (Begin on drag, End on drop or cancel).
- **Keys while holding:** `ControlRules.Resolve` returns `CancelHold` for a view key (TAB, Q, A, D, F1) while anything is held (`ControlState.Dragging` now includes `HoldRegistry.IsHolding`). SPACE still toggles inspect, because it changes no view.
- **Back-out:** `BackOut` gives `CancelDrag` first, and `OfficeControls.LetGo` cancels the registry's hold, falling back to the old paper and dater paths.
- **Focus loss:** `OfficeControls.OnApplicationFocus(false)` → `HoldRegistry.OnFocusLost`, and `DeskDraggable.OnApplicationFocus(false)` cancels its own drag. The Input System's release that follows finds nothing held, so nothing is stamped or handed back.
- **We chose not to poll `Application.isFocused`:** polling would cancel every drop the QA rig drives in an unfocused editor.
- **Re-projection:** `DeskDraggable.LateUpdate` re-follows the pointer through the moving camera whenever the camera or the grab scale changed (blends, tilts). That covers the daters too.

### BUG-58c: F1 with the screen off

- `OfficeControls.Do(ShowKeys)` returns while `!PcFrame.CanOpen`. The P0 carve inserted this line, and this file is DOC-HANDLE's from here on.
- It is behaviour-neutral until UI-PC (#13) gives `CanOpen` its real value. Kept as is.

### BUG-65d: the ChipReader string

- `ChipReader.Show` builds its text only when `ShownKey()` changes (state, dot count, line, result number).
- **Before:** it concatenated "READING" plus dots, and called `UiText.Format` on mismatch lines, every frame.

### BUG-65g: call-next instantiation

- `DeskController.PrepareSpare` makes one inactive clone of the paper template per frame, up to `DeskConfigSO.paperPool` (6), never on a hand-over's frame.
- `HandOverNow` takes a ready clone (`TakeSpare`), and the arrivals are bound one per beat.
- **Not done here:** the TmpFormText/DeskPaperDepth setup cost inside `Bind` is DOC-LOOK's (BUG-65g's other half).

### BUG-68b: the input gate

- `OfficeControls.Update` returns while `InputGate.Blocked`. The P0 carve inserted this; it is unchanged here, and RUN-SAFE (#3) fills the gate.

### BUG-20b and BUG-73c: dead knobs and comments

- **Removed `MotionKnobs` knobs:**
  - the release slide: `paperSlideSeconds`, which was the duplicate of `DeskConfigSO.paperSlideSeconds`, and `paperSlideMax`;
  - the drop spin and the squash: `paperDropSpinPerSpeed/Max`, `paperDropSquash`;
  - the knobs of the old stamp, cut in P0-C: `stampLift`, `stampRiseSeconds`, `stampSlamSeconds`, `stampSquash`, `inkBloomSeconds`, `stampFeel`.
- **New `MotionKnobs` knobs:** `paperSettleSpeed` and `cardDropHop`.
- **`DeskConfigSO`:**
  - removed: `refuseShake` and `readingArea`;
  - new, each with a code default (Desk_Default.asset needs no edit): `handOverSeconds`, `handOverStagger`, `paperPool`, `topLift`, `heldClearance`, `heldLeastRatio`, `restViewport`, `padOverhang` and `refuseShakeDistance/Seconds/Cycles`;
  - stale comments updated: `paperSlideSeconds`, `scannerShadowMax`, `counterScale`, `readingHeight` and the dock block.
- **Left in place (on the cleanup list):**
  - `MotionKnobs.drawerStopHit` is dead too, but it belongs to STAMP's 73b drawer cleanup.
  - **BUG-20b `DeskConfigSO.crtClockSize`** is still read by `CrtClock.cs` (UI-PC's). DOC-HANDLE deletes it when UI-PC's clock rework (BUG-20a, #13) drops its reader.
  - `MotionTuning_Default.asset` (FLOW's) still serializes `paperDropSquash` and the old stamp keys. Unity ignores them, and they disappear on the asset's next save.
- **`DeskDocument._keys`/`Share`** (the photo's share) have no reader now that the spread is gone. They stay only because `DeskDocument.Render.cs` (DOC-LOOK's) still writes them in `PlacePhoto`.
  - Cleanup: DOC-LOOK drops that line at #9, then DOC-HANDLE deletes the field and `Share`.

## Tests

**Offline:** compile_check 0 errors; runner `passed 3716, failed 0`. The baseline at 5aecb60 was `passed 3710, failed 0`.

**New (EditMode, offline):**
- `DropResolverTests`:
  - exact release;
  - edge clamp of the whole rect;
  - least move off a prop;
  - the joint solve (never a push into an earlier blocker);
  - a counter refusal rests just off it;
  - the folder;
  - the footprint, not the pointer, picks the dock;
  - the final size is judged;
  - least overlap when nothing is clear.
- `HeldOnTopTests`: the ratio and its limits; a held point stays on its ray, in front of the props.
- `MachineDocksTests` (additions):
  - `APassport_IsSizedToFitThePad`;
  - `TheDockState_IsEachPapers_AndResetsWithTheCase`;
  - `NoQueue_TakesAPaperOutOfAMachine`.
- `HoldRegistryTests` (additions): `AViewKeyWhileHolding_CancelsTheHoldFirst_AndChangesNoView`, `LosingTheFocus_CancelsWithoutCommitting_AndOnlyOnce`.
- `DeskObstaclesTests` (addition): `DeskObstacles_TheTallestAndTheFootprints`.
- `ControlRulesTests` (addition): `WhileHolding_AViewKey_LetsGoFirst_AndDoesNothingElse`.
- `DeskPapersTests` (additions): `APaper_HasOneWorldSizePerZone_WhateverTheView`, `APapersSize_ChangesOnlyAtTheCountersEdge_UnderTheCursor`.
- `PaperPhysicsTests` (new cases):
  - `LetGo_ItStaysExactlyWhereItWasLetGo_NoSlideNoSpin`;
  - `LetGo_ItSettlesAsMuchAsItWasMoving`;
  - `ARigidCard_NeverBends_AndHopsAtMostItsCardHop`;
  - `ALanding_CurlsAndHops_LikeAFullDrop`;
  - `ARefusal_ShakesInPlace_SideToSide_ThenLiesStill`;
  - `ASwing_PivotsOnTheHeldPoint`.
- `ScannerClearanceTests`: `TheBlockedArea_IsTheFootprintAndTheShadowBehindIt`.

**Rewritten or removed, because they pinned the bugs** (FIX_PLAN risk 3: listed for the review):

| Test | Why |
|---|---|
| `PaperPhysicsTests.LetGo_ItSlidesOnIntoItsPlace_FromBehind_NeverPastIt_AndStops` | Pinned BUG-6's back-slide (`SlideStart = -v*0.16`). Replaced by `LetGo_ItStaysExactlyWhereItWasLetGo_NoSlideNoSpin` |
| `PaperPhysicsTests.LetGo_ItSpinsOnceByItsSidewaysMomentum_WithinItsMost` | The drop spin is gone (it moved the grab point) |
| `PaperPhysicsTests.ANudge_ShakesItsSwing_AndItSettlesBack` | The yaw nudge became the in-place shake (`ARefusal_…`) |
| `PaperPhysicsTests.AHeavierBody_SwingsLess_AndTakesLongerToSlideToRest` | There is no slide any more. Now `AHeavierBody_SwingsAndBendsLess` |
| `PaperPhysicsTests.UnderReducedMotion_NothingMoves` | Adapted: hop instead of (spin, hop), and the shake is still too |
| `DeskPapersTests.APaper_ShowsFullSize_OnlyOnTheDesk_InTheReadingView` | Pinned BUG-1's regrow under the reading view (D1 reverses the 1016a rule) |
| `DeskPapersTests.PaperClicks_DecisionTable` | No `Read` row (D1/R6) |
| `ControlRulesTests.SpaceAndTab_ToggleInspectAndTheStamps_FromEveryDeskState` | Its dragging state pinned BUG-55 (TAB or Q under a held paper) |
| `PaperSpreadTests` (5 tests) | PaperSpread is deleted (the click-to-desk spread) |
| `ScannerClearanceTests` (5 tests: Clear, ClearProp and Eject) | Replaced by DropResolver's joint solve |

**Editor-side, for the Unity pass** (`Assets/Editor/SeamTests/DocHandleSeamTests.cs`, category `NeedsEditor`):
- `ADeskDrag_HoldsThroughTheRegistry_AndTheRegistrysCancelCancelsIt`;
- `ACarry_ReplacedOrStopped_RunsItsCut_NeverItsLanding`;
- `TheChipReader_LaysAPassportUprightToThePlayer_AndMeasuresItsPad`.

## Cross-track notes (seams and requests; no other track's file was edited)

- **QA-RIG (#2, merged before this track on main):**
  - Add the camera tag `QaSignals.TagCamera("DeskDropTilt", seconds, cap)` as the first line of `DeskController.Read`'s `TiltIn` branch. It is DOC-HANDLE's line, added at the integration merge once `QaSignals` is on the branch.
  - Add a registered harness op that finds a desk paper by case index, for the DOC audit port. `DeskController` exposes nothing new for it yet: the harness can find `DeskDocument` objects by `Index`.
  - Add `Assets/Editor/QA/Checks/DocHandleChecks.cs` (DOC-HANDLE's file), exactly as QA-RIG.md §5 shows, with `doc.drops` (Play) and `doc.callnext` (BuiltPlayer, demo). It cannot compile without `GateCheck` on the branch.
- **DESK-PLAN (#5):**
  - Register every prop on or near the mat in `DeskObstacles.Desk`: world x/z bounds, height over the desk, unregister when it leaves.
  - `DeskMat` is not consumed: a resting paper's area is the reading view's visible desk, not the mat. If DESK-PLAN wants papers kept on the mat, pass `DeskMat.Rect` (in the view's frame) to `RestArea`; it is one line.
- **STAMP (#7):**
  - Register the fixed rack in `DeskObstacles.Desk`. A held paper is then drawn above it, and a resting paper keeps off it.
  - The daters are holdables already: they are DeskDraggables.
  - `MotionKnobs.drawerStopHit` is dead (their 73b).
- **TOOLS (#10):**
  - Register the UV torch in `DeskObstacles.Desk`, then delete `DeskController`'s `TorchObstacle` registration. That is DOC-HANDLE's line; the integrator removes it at the TOOLS merge.
  - Torch holds: implement `IHoldable` and `Begin`/`End` on `HoldRegistry.Desk`.
- **DOC-LOOK (#9):**
  - Read `HoldRegistry.Desk.IsHolding` (no hover while holding) and `DeskEvents.StackChanged` (contact shadow by rank).
  - Drop `_keys.Add(Share(r))` from `PlacePhoto`.
  - `BendNow` keeps reading `cardFlex`, which is 0 now.
- **FEEDBACK (#16):** play the hand-over slap on `DeskEvents.DocumentLanded`, which is raised for hand-overs only; a drop plays `paper_drop` from `DeskDocument.Feel` as before. `PaperPhysics.ReleaseSpeed` is there for the louder fast drop.
- **CAM (#6):** read `HoldRegistry.Desk.IsHolding` for "no view change while holding" (55c). This track's keys already cancel a hold before any view key acts.
- **CONTENT / TEXT-LANG:** the day-1 scanner note (`desk.scanHint`, world_source.json, all languages) still says "Click a paper to read it; drag it onto the scanner to open it on the PC." Clicking no longer reads.
  - Proposed English: "Drag a paper onto the desk to read it; drag it onto the scanner to open it on the PC."
- **UI-PC (#13):** when `CrtClock.cs` stops reading `DeskConfigSO.crtClockSize`, DOC-HANDLE deletes the field (BUG-20b).

## FEATURES (for INTEGRATION to fold in)

- **Line 42 (Papers), the counter and desk sentences:**
  - Replace "a paper clicked to the desk lies where it covers the least … (`PaperSpread`, `DeskConfigSO.readingArea`) …" with:
    > "a click on a paper brings it to the top of the stack and does nothing else (Saleh 2026-10-20, D1 and R6; `PaperClicks`, tested: `DeskPapersTests`); a paper has one world size per zone, whatever the view: small on the counter, its reading size on the desk, changing only under the cursor as a held paper crosses the counter's edge (`PaperSizes`, tested: `DeskPapersTests`); a dropped paper rests exactly where it was released unless part of it would lie out of the reading view or under a prop, then the least move clear of every obstacle at once (`DropResolver`, `DeskObstacles`, tested: `DropResolverTests`, `DeskObstaclesTests`); a refusal shakes in place and rests just beside what refused it; a held paper is drawn above every prop with its picture unchanged (`HeldOnTop`, tested: `HeldOnTopTests`); papers handed over slide in for 0.42 s, one after another, and land with a slap (`DeskConfigSO.handOverSeconds`, `DeskEvents.DocumentLanded`)".
  - Also drop any wording that says a desk paper grows in the reading view.
- **Line 54 (Desk notes):** the quoted scanner note, once CONTENT changes the string.
- **Line 62 (Desk view):** add that the drop tilt is the one camera move a drop makes, and that the reading view never changes a paper's size.
- **Line 64 (Controls):** add:
  > "While anything is held (`HoldRegistry`: a paper, the folder, a dater), a view key (TAB, Q, A, D, F1) or a right-click or Esc lets go of it first and does nothing else, and losing the focus lets go without committing (tested: `ControlRulesTests`, `HoldRegistryTests`)".
- **Line 185 (Desk feel):** replace "drops with a soft squash and a flutter" with:
  > "lands where it was let go, settling (a hop and a curl of its ends) as much as it was moving, with no squash and no slide (`PaperPhysics`, tested: `PaperPhysicsTests`); ID cards are rigid; its swing pivots on the held point".
- **Chip reader and scanner (lines 50-51):** add:
  > "a passport lies on the reader's pad upright to the player, sized to fit the pad (`DockFit`), and every docked paper keeps its docked size until it is clear of the machine; no queue takes a paper from a machine (`MachineOccupancy`, tested: `MachineDocksTests`)".

## RESULT

**Measured here (outside Unity):**
- `compile_check_any.py E:\unity\NOPE-int`: 0 errors in every project, the Editor project at 161 warnings (as at the baseline).
- Offline runner: `passed 3716, failed 0`.

**Everything else is deferred: Unity.** Saleh may be on the PC, so no Unity editor or player was started.

The recipes below are for the Unity pass. They run in one batch, in the art office plus OfficeGameplay; QA-RIG's harness must be on the branch first. The capture vocabulary is FIX_PLAN §0: A1/A2, P-* paths, SEQ at 30 fps.

1. **deferred: Unity. Import and compile.**
   - Open the branch in the lane's editor and wait for the heartbeat to read idle.
   - Pass: 0 compile errors, and no missing-script warnings on the paper template, ChipReader or DeskScanner.
   - The new serialized fields (`ChipReader.padSize`, the DeskConfigSO knobs) take their code defaults.
2. **deferred: Unity. The EditMode suite.**
   - Run TestRunnerApi EditMode, everything, including the three `NeedsEditor` tests in `DocHandleSeamTests`.
   - Pass: green, except the known `UnitySkills…SceneSummarize_CountsObjectsCorrectly`.
3. **deferred: Unity. Build Office UI.**
   - The builder was not changed, so the OfficeGameplay semantic dump must equal the base's, apart from the new serialized defaults on ChipReader.
   - INTEGRATION rebuilds and commits the scene.
4. **deferred: Unity. Smoke.**
   - Title → New Run → day 1, one traveller decided.
   - Pass: no errors in the log, the papers arrive one by one with the slide, and `DeskEvents.DocumentLanded` fires once per paper.
5. **deferred: Unity. The drop matrix** (QA-RIG `doc_drops.json`; 40 drops).
   - Setup:
     - every P-* path: counter→desk, desk→desk, desk→counter (before and after the verdict), onto the folder, beside every machine, P-EDGE and P-OFF;
     - A1 and A2, days 1, 5 and 12 (`jumpDay` + `cheat GiveAllItems`), H2 and H5.
   - Pass (v2):
     - (a) the rest pose, projected through the **release-frame camera**, is ≤8 px from the release grab point, unless docked, handed back, refused, or the paper was released overlapping a prop or out of view (those are logged with their move);
     - (b) 0 backward jumps in the first 10 frames;
     - (c) world scale after release changes by 0%, the counter-edge crossing is the only size change, and the on-screen size after the tilt is the projection of the unchanged world rect ±1%;
     - (d) the only drop-caused camera change is one tagged D1 tilt, none when already tilted;
     - (e) the whole rect is in the viewport at rest (after the tilt);
     - (f) 10/10 clicks on a resting desk paper change neither its world scale nor the camera, and it comes to the top in the same frame.
6. **deferred: Unity. Obstacles.**
   - SEQ of the open passport dragged across every prop at its largest.
   - Pass: in 100% of frames no prop pixel over the held paper (ID buffer), and 0 resting papers under a prop.
   - Also check the held paper's picture does not jump at pick-up or drop (the HeldOnTop homothety): a frame diff at the press and release frames ≤1 px.
7. **deferred: Unity. Chip reader and scanner.**
   - 10 chip reads and 10 scans with Auto-Feed on, a case ended mid-scan, then the next case.
   - Pass:
     - passport-casing AABB intersection 0 in every frame;
     - the passport upright and within the pad;
     - the docked size held until clear;
     - the scanner lid shut after the mid-scan end;
     - the next case's papers at stack depth;
     - Auto-Feed never takes a paper out of the reader.
8. **deferred: Unity. Holds.**
   - Hold a paper, the folder and each dater; press Esc, Q, A, TAB, F1 and Alt+Tab (focus loss), and right-click.
   - Pass: the hold is cancelled, nothing is stamped or handed back, and there is no view change while holding.
   - Also run a camera blend while holding (call next from the tilted view): the held thing stays glued to the cursor in every frame.
9. **deferred: Unity. Settle.**
   - Pass:
     - a P-SLOW drop (≤150 px/s) peaks at ≤2 px of settle motion;
     - a P-FAST drop settles visibly but in ≤0.25 s;
     - an ID card does not bend (mesh-vertex diff) and its hop is ≤1 mm.
   - Judge the pivoted swing by eye (see BUG-11a).
10. **deferred: Unity. Hand-over.**
    - SEQ of 5 hand-overs at A1 and A2.
    - Pass:
      - each paper travels 0.35-0.5 s;
      - it is present and moving in every frame of that span;
      - it rests on the desk, wholly in view;
      - `DocumentLanded` fires on the landing frame ±1.
    - The thump is audible from FEEDBACK's merge (#16).
11. **deferred: Unity. Stack order.**
    - Stills of a 3-paper stack in 10 random orders at A2.
    - Pass: the top paper's visible edge moves ≥2 px from `topLift`, and a click-to-front reorders in the same frame.
    - The 10/10 naming happens at DOC-LOOK's merge.
12. **deferred: Unity. The folder.**
    - Drop it on the counter and beside every machine, and re-grab it during a refusal.
    - Pass: no single-frame teleport (its rect is continuous), a refusal shakes in place and lies just clear, and one mover (no jitter, no end-of-path jump while held).
13. **deferred: Unity [BP]. Call-next and allocations.**
    - Day 12 with all items.
    - Pass: no frame over 33 ms at call-next attributable to paper instantiation, and 0 B/frame from ChipReader and DeskController in `QaJobs.GcSites`.
14. **deferred: Unity. F1.**
    - With the screen off, press F1 at the desk.
    - Pass: no PC frame opens. This is UI-PC's `CanOpen` at #13, and a no-op until then.
15. **deferred: Unity. By eye.**
    - Watch all 40 GIFs at 1×, then side by side with the Papers, Please and ReStory clips.
    - The adversarial reviewer answers "does it pop, slide or snap anywhere?" with no.
    - Re-check notes 25, 20, 19, 6 and 17 with before/after GIFs.
    - Fill the feel cards for rows A1-A3 and A6-A11 in `R7\FIX\FEEL\`.
