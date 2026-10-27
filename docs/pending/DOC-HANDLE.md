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
- **Both halves are pure and tested (verifier findings 1 and 5):**
  - `PaperSizes.HeldZone(under, recorded, verdict, overMachine)` is the size while held (it was `HandleDragged`'s inline `canLie`). `overMachine` is round 2's (below).
  - `PaperSizes.ZoneAfterDrop(outcome, released, before)` is the zone, and so the size, a paper takes after its release, for every `DropOutcome`. Every drop branch in `HandleDragEnded` and `RefuseAt` now sets that zone, and DeskPapers records the same one.
- **The fix for finding 1 (a counter paper dragged straight onto a machine):**
  - Over a machine (machines stand on the desk) it is held at its desk size.
  - If the chip reader refuses it (`DropOutcome.MachineRefused`, new; DeskPapers.Drop never returns it), it lies on the desk beside the reader. It becomes a desk paper (`DeskPapers.Lay`, new) at the size it was held at.
  - If the busy scanner queues it (`Queued`), DeskPapers.Drop records the desk's zone, and it waits beside the scanner on the desk at its desk size. Both rests are kept off the counter (`Rest(…, offCounter: true)`).
  - Once its scan ends, the desk carries it back to where it was picked up, as before. DeskPapers.Tick gives it back the zone it was picked up in (`_comesBackTo`), so a counter paper goes back to its counter size there. The change happens while it is still docked, so the size eases only once it is clear of the scanner, as every carry-out does.
  - Before: both paths called `SetZone(_state.ZoneOf(index))`, which was still Counter, so the paper eased down to counter size while lying on the desk.
- **Verifier round 2 (the queued paper's regression, S2):** `_comesBackTo` is the zone of the desk's pick-up position (`DeskDraggable.PickUpPosition`), and that position moves with every drag. Two holes followed:
  - *Repro A:* a queued paper lifted from its waiting spot and let go by a cancel (Esc, right-click, Alt+Tab) stayed queued with `_comesBackTo` = Counter, but its pick-up was now the waiting spot. Scanned, it was carried to the waiting spot on the desk and eased to counter size there.
  - *Repro B:* dropped on the chip reader, a waiting paper stayed in the feed (`_queue`, `_dropped`, `_waiting`), so it was later fed from beside the reader and came back to the waiting spot at counter size.
  - **The fix:**
    - `DeskPapers.Lift(i)` (new), called from `DeskController.HandleDragBegan`, forgets `_comesBackTo` whenever a drag begins, which is exactly when the pick-up position moves. The paper keeps its place in the feed: a cancel lays it back where it waited, and only a drop changes the feed. Once scanned it is carried to where it was last lifted from, in the zone it lay in there (the desk's, beside the scanner).
    - `DeskPapers.LeaveFeed(i)` (new) is Drop's off-the-scanner rule on its own: a paper that waited by hand leaves the feed (an Auto-Fed paper keeps its place), and nothing carries it back. `Drop` now calls it, and so does the controller's chip-reader branch, before it reads or refuses the paper, together with `_waiting.Remove`.
  - Why not "the zone of where it comes to rest" (the verifier's other option): a paper's recorded zone is the pointer's at its last drop (deviation 1), not `ZoneAt` of its root, so re-deriving it from the carry-out target could disagree with the size it was last held and laid at. Forgetting at the lift keeps the zone and the pick-up position in step by construction.
- **Verifier round 2 (low): the held size over a machine.** The dock is judged at the footprint's centre (`DockOf`), but the held size followed the pointer's zone. A counter paper held by its far edge, the pointer just inside the counter and its centre over a machine, was held at counter size and lay at desk size after a refusal or queue. Now `HandleDragged` asks `DockAt` for the paper's footprint every frame, and `HeldZone` returns Desk whenever the centre is over a dock, whatever the pointer.
  - `DockAt` now lists the machines itself (no `Docks()` enumerator) and `WorldFootprint` takes an array, so the per-frame check allocates nothing.
  - Flicker at a dock's edge: growing to the desk size scales the grab offset away from the pointer. When the centre came into the dock from the counter's side or across a side edge, that carries it deeper in, not out, and shrinking carries it further out, so the size settles. Only a centre coming in across the dock's far edge, with the dock shallower than that move, could toggle; it needs a counter paper whose centre lies beyond a machine while the pointer is still on the counter, and recipe 5(g) watches for any flicker.
  - Whether the case arises at all depends on the scene (a machine's drop area within half a paper's depth of the counter's near edge); recipe 5(g) checks it.

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
- `DockOf(footprint, docks, holds)` is the dock rule that ships. `DeskController.DockAt` calls it (verifier finding 3; before, `DockAt` used `IPaperDock.Contains` directly, and `DockOf` was dead code that only its test called).

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
- It is picked by the footprint's centre, not the pointer: `DockAt(PaperFootprint(paper, at), at.y)` → `DropResolver.DockOf`.
  - Each machine's own drop-area test (`IPaperDock.Contains`) is the `holds` predicate.
  - The chip reader is listed first, so it wins where its drop area meets the scanner's, as before.
- The rulebook folder is judged the same way, at the centre of its renderers' footprint. Before, it was judged at its root.
- `MachineOccupancy.IsIn` had no caller and is deleted.
- **The zone is still the pointer's. This is a deviation from v1 step 3, listed for sign-off below.** The size switches when the pointer crosses the edge, so the drop's zone is the zone the paper is already sized for. Judging the zone by the footprint would make the size flip after release, breaking matrix (c). This rule is now tested: `DeskPapersTests.TheSizeAfterTheRelease_IsTheSizeItWasHeldAt_ForEveryDropThatDoesNotDock` runs every pointer zone × outcome.

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
- **Held:** it is drawn above every prop (`ShowHeld`, HeldOnTop). Any swing pivots on the held point; by default there is none (`paperSwayMax` 0, BUG-11a).

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
- **No yaw while held (verifier finding 2):**
  - BUG-11a's "Fix must achieve" and v1 step 4 say "no yaw while held". The feel bible's A6 keeps a sway (16°/(m/s), ≤12°). Only Saleh or the orchestrator can reconcile the two, so the default now follows the plan.
  - `MotionKnobs.paperSwayMax` defaults to **0** (this track owns `MotionKnobs`). `MotionTuning_Default.asset` does not serialize the key, so the code default is what runs.
  - What this costs: the corners' twist follows the swing, so it rests too. The bow, the flutter and the landing curl stay, so a paper still bends.
  - The pivot machinery stays (`PaperPhysics.PivotOffset`), so a swing never moves the point under the cursor or the rest pose. If the call is "keep A6's sway", set `paperSwayMax` back to 12 and nothing else changes. `PaperPhysicsTests` runs the swing tests with `paperSwayMax` 12 (`Swaying()`), and `ByDefault_AHeldPaper_NeverYaws_ButStillBows` pins the default.
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
  - **The hand-off (verifier finding 4):**
    - The problem: in the Auto-Feed chain a finished paper stayed in `MachineOccupancy` as the scanner's until its carry-out landed. The next frame `StartScan(next)` called `_docks.Dock(next, Scanner)`, which returned false (and the result was ignored). The fed paper was never recorded as docked.
    - The fix: `CarryOut` now calls `MachineOccupancy.HandOff` (new) as the paper leaves the bed. A handed-off paper is still the machine's until it lands clear: `IsDocked` stays true, `FreeToFeed` false, and it keeps its docked size. But it no longer fills the machine (`Busy`), so the next paper docks and is recorded.
    - `StartScan` now checks `Dock`'s result, and logs an error if the bed was not free, so a regression shows in the log.
    - The chip reader does not hand off: it keeps its paper until the carry-out lands, because a card leaves the slot the way the next one would come in.
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

**Offline:** compile_check 0 errors; runner `passed 3726, failed 0`.
- The baseline at 5aecb60 was `passed 3710, failed 0`.
- The first DOC-HANDLE commit (b465a0f) was at `passed 3716`.
- The verifier-fix commit (4363642) adds 7 tests (`passed 3723`).
- The round-2 fix adds 3 tests (`passed 3726`).

**Added for verifier round 2:**
- `DeskPapersTests.AQueuedPaper_LiftedThenCancelled_ThenScanned_LiesInTheDeskZone` (repro A): queued, lifted, cancelled, then scanned: it lies in the Desk zone. Also: lifted and re-queued, it comes back to the desk; `Lift` refuses a paper out of range, on the bed or given back.
- `DeskPapersTests.AQueuedPaper_DroppedAtTheReader_LeavesTheFeed` (repro B): refused (laid on the desk) or accepted, it leaves the feed, is never fed from beside the reader and stays a desk paper; an Auto-Fed paper keeps its place, as with `Drop`.
- `DeskPapersTests.AHeldPaper_OverAMachine_ShowsAtItsDeskSize_WhateverThePointer` (the low finding).
- `TheSizeAfterTheRelease_IsTheSizeItWasHeldAt_ForEveryDropThatDoesNotDock` no longer skips "pointer over the counter, centre over a machine": those rows now run, with `HeldZone`'s `overMachine`.

**Added for the verifier findings:**
- `DeskPapersTests.TheSizeAfterTheRelease_IsTheSizeItWasHeldAt_ForEveryDropThatDoesNotDock` (findings 1, 3 and 5):
  - It covers every recorded zone × verdict × pointer zone × machine (none, the scanner, the chip reader) × scanner idle or busy.
  - It checks the held size (`HeldZone`) against the size after the release (`ZoneAfterDrop`) and DeskPapers' recorded zone, for every outcome.
  - The counter paper refused or queued at a machine fails it on the old rule.
- `DeskPapersTests.ACounterPaper_RefusedOrQueuedAtAMachine_LiesOnTheDesk_AtItsDeskSize`: the finding's own scenario, and the queued paper's zone back on the counter once it is scanned.
- `DeskPapersTests.AQueuedPaper_PickedUpAgain_ComesBackToWhereItWasLastPickedUp`.
- `DropResolverTests.WhereTwoDropAreasMeet_TheFirstListed_TakesIt`.
- `DropResolverTests.TheFootprint_NotThePointer_DecidesTheDock`, now through the `DockOf` signature that `DockAt` calls.
- `MachineDocksTests.TheAutoFeedChain_DocksEachPaper_WhileTheLastIsCarriedOut` (finding 4).
- `MachineDocksTests.AMachine_HoldsOnePaper_UntilItIsHandedOff`.
- `PaperPhysicsTests.ByDefault_AHeldPaper_NeverYaws_ButStillBows` (finding 2).

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
- **CONTENT / TEXT-LANG: a merge blocker for the demo.**
  - The problem: the day-1 scanner note (`desk.scanHint`, in all 7 string tables and world_source.json) still says "Click a paper to read it; drag it onto the scanner to open it on the PC." Clicking no longer reads (R6).
  - **INTEGRATION must not build a demo with DOC-HANDLE merged until CONTENT has changed this string** in every table.
  - The string is correctly left to CONTENT; this track does not own those files.
  - Proposed English: "Drag a paper onto the desk to read it; drag it onto the scanner to open it on the PC."
- **UI-PC (#13):** when `CrtClock.cs` stops reading `DeskConfigSO.crtClockSize`, DOC-HANDLE deletes the field (BUG-20b).

## Deviations and readings that need sign-off (orchestrator or Saleh) before merge

These are listed so nobody has to find them in the recipes. None is one of Saleh's decisions.

1. **The drop's zone is judged by the pointer, not the footprint.**
   - v1 step 3 and the v1 test list say the zone and the dock are both judged by the footprint. The dock is (`DockOf`); the zone is not.
   - Why: the size changes under the cursor as the pointer crosses the counter's edge, so judging by the pointer keeps the size after release equal to the size held (matrix (c)). That rule is tested over every outcome.
   - Effect: a paper released with the pointer on the desk can lie partly over the counter strip, and the reverse.
2. **BUG-11a's yaw: the default is now no yaw while held (`paperSwayMax` 0), as the plan says.**
   - The open call is whether A6's ≤12° sway (pivoted on the held point) comes back. That is one knob.
   - With the sway at 0, the corners' twist also rests. **For the sign-off:** the twist is part of A6, which does not conflict with BUG-11a's no-yaw rule, but `PaperPhysics` drives it from the yaw's velocity, so it goes with the yaw. If the sign-off wants the twist (≤6 mm) without the yaw, it can be driven by the drag speed or the bow instead; that is a `PaperPhysics` change, not a knob, and it is not built.
3. **Recipe 5(a) widens v2's pass line.** v2 says the rest pose is ≤8 px from the release grab point "unless the paper was docked or handed back". Recipe 5(a) also exempts:
   - a refused drop, which v1 step 3 and the v2 refusal rule have resting beside what refused it;
   - a paper released overlapping a prop, or partly out of the reading view, which the obstacles pass line and matrix (e) move.
   
   Each exempt drop is logged with its move, and its move must be the least clear move (DropResolver). If the sign-off keeps v2's narrower line, those drops count as failures by definition, so the line has to change or the matrix cannot pass.
4. **R6 includes the counter (our reading of D1 and R6).** A click on a counter paper only brings it to the top. It no longer slides it to the desk and enlarges it. The day-1 note that says "Click a paper to read it" is the CONTENT blocker above.
5. **A counter paper refused or queued at a machine becomes a desk paper.**
   - Why: it lies on the desk beside the machine, so it takes the desk's zone, and before the verdict it cannot be dropped back on the counter (`NotStamped`), like any desk paper.
   - Exception: a queued counter paper that gets scanned is carried back to the counter, and is a counter paper again, unless it was lifted from its waiting spot since (then it comes back to that spot, a desk paper: round 2).
   - Held over a machine, a paper shows at its desk size even with the pointer over the counter (round 2), because that is where it lies if the machine refuses or queues it.
6. **The rulebook folder's dock is judged at its footprint's centre.** Before, it was judged at its root. Same rule as a paper.

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
  > "lands where it was let go, settling (a hop and a curl of its ends) as much as it was moving, with no squash and no slide (`PaperPhysics`, tested: `PaperPhysicsTests`); ID cards are rigid; a held paper never turns (no yaw while held, `MotionKnobs.paperSwayMax` 0, pending the A6 call)".
- **Chip reader and scanner (lines 50-51):** add:
  > "a passport lies on the reader's pad upright to the player, sized to fit the pad (`DockFit`), and every docked paper keeps its docked size until it is clear of the machine; no queue takes a paper from a machine (`MachineOccupancy`, tested: `MachineDocksTests`)".

## RESULT

**Measured here (outside Unity):**
- `compile_check_any.py E:\unity\NOPE-int`: 0 errors in every project, the Editor project at 161 warnings (as at the baseline).
- Offline runner: `passed 3726, failed 0`.

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
     - (a) the rest pose, projected through the **release-frame camera**, is ≤8 px from the release grab point, unless the paper was docked or handed back (v2's line).
       - **Pending sign-off (deviation 3):** this recipe also exempts refused drops and papers released overlapping a prop or partly out of view.
       - Log each exempt drop with its move, and check that the move is DropResolver's least clear move.
       - Report the (a) count both ways: under v2's line and under the widened line.
     - (b) 0 backward jumps in the first 10 frames;
     - (c) world scale after release changes by 0%, the counter-edge crossing is the only size change, and the on-screen size after the tilt is the projection of the unchanged world rect ±1%;
     - (d) the only drop-caused camera change is one tagged D1 tilt, none when already tilted;
     - (e) the whole rect is in the viewport at rest (after the tilt);
     - (f) 10/10 clicks on a resting desk paper change neither its world scale nor the camera, and it comes to the top in the same frame;
     - (g) **a counter paper refused or queued at each machine keeps its size** (verifier findings 1 and 6). Four drops, each straight from the counter:
       - a fresh counter permit with no chip onto the chip reader;
       - a counter passport onto the chip reader while it reads another;
       - a counter paper onto the busy scanner;
       - the same with Auto-Feed on.
       
       Pass for each:
       - world scale at release = world scale 1 s later (0% change: the desk size it was held at);
       - it rests on the desk beside the machine, wholly off the counter;
       - the queued one, once scanned, is carried back to its counter spot and eases to counter size only as it lands clear of the scanner.

       Round 2 adds three more, each a counter paper queued at the busy scanner first:
       - lift it from beside the scanner and cancel with Esc, then with right-click, then with Alt+Tab; once scanned it comes back to the waiting spot on the desk and its world scale is the desk's before and after (0% change);
       - drag it from beside the scanner onto the chip reader, refused (no chip) and accepted (a passport); it leaves the feed: the scanner never takes it from beside the reader, and it keeps its desk size;
       - a counter paper held by its far edge, the pointer just inside the counter's near edge and its centre over the reader, then the scanner: log whether the case arises in the scene (a drop area within half a paper's depth of the counter). If it does, the paper shows at its desk size while its centre is over the machine and keeps that size after the refusal or queue, with no size flicker at the drop area's edge.
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
     - Auto-Feed never takes a paper out of the reader;
     - in the Auto-Feed chain, no `[Desk] paper … went onto the scanner's bed while the bed was not free` error in the log;
     - each fed paper is inert, with no hover and no drag, while it is on the bed (MachineOccupancy recorded it: verifier finding 4).
8. **deferred: Unity. Holds.**
   - Hold a paper, the folder and each dater; press Esc, Q, A, TAB, F1 and Alt+Tab (focus loss), and right-click.
   - Pass: the hold is cancelled, nothing is stamped or handed back, and there is no view change while holding.
   - Also run a camera blend while holding (call next from the tilted view): the held thing stays glued to the cursor in every frame.
9. **deferred: Unity. Settle.**
   - Pass:
     - a P-SLOW drop (≤150 px/s) peaks at ≤2 px of settle motion;
     - a P-FAST drop settles visibly but in ≤0.25 s;
     - an ID card does not bend (mesh-vertex diff) and its hop is ≤1 mm.
     - a held paper's yaw is 0 in every frame (`paperSwayMax` 0, deviation 2), while it still bows.
   - If the call restores A6's sway (`paperSwayMax` 12), judge the pivoted swing by eye instead: the held point never moves.
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
    - Also while a paper is held and moved over both machines: 0 B/frame from DeskController (`HandleDragged` now runs `DockAt` every frame, round 2).
14. **deferred: Unity. F1.**
    - With the screen off, press F1 at the desk.
    - Pass: no PC frame opens. This is UI-PC's `CanOpen` at #13, and a no-op until then.
15. **deferred: Unity. By eye.**
    - Watch all 40 GIFs at 1×, then side by side with the Papers, Please and ReStory clips.
    - The adversarial reviewer answers "does it pop, slide or snap anywhere?" with no.
    - Re-check notes 25, 20, 19, 6 and 17 with before/after GIFs.
    - Fill the feel cards for rows A1-A3 and A6-A11 in `R7\FIX\FEEL\`.
