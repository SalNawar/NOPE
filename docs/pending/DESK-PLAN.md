# DESK-PLAN (Phase A #5): the desk home map

Branch `fix/a-desk-plan`, cut from main `9b8ab5f` (P0: CARVE + CUTS + QA-RIG): the home map (`4a6a0bf`), then the review fixes (the second commit: the RING SERVICE stickers wait for the crate, the click-box checks, A2 and the intercom in the scenario, the phone as an open call, the hand-offs below). Sources: FIX_PLAN.md §3 DESK-PLAN, FIX_PLAN_v2.md §3 "DESK-PLAN: delta" (v2 wins), BUGLIST.md, DECISIONS_1019a.md, GAME_FEEL_BIBLE.md (rows C2, C6, C7, C8, D1), PHASEB/LEASES.md, PHASEB/DESK-PLAN-HOMEMAP/SPEC.md + desk_simple_1.png, `E:\unity\NOPE-tools\design\desk\canva\` (concept B).

**What it is:** every desk object has exactly one home off the big paper mat, bound by id (`DeskLayout.HomeOf(id)`). The art props with no purpose are cut. The machines stand on A's rail right of the mat in the order of use. The papers come in and go back over a deal tray at the counter. The homes of locked and Phase B objects are kept as positions only. C's next-tool lamp is a data hook. Code only: no Unity was run (another agent holds the Unity lane), so every in-engine check is a recipe in RESULT below.

## Saleh's calls this builds (verbatim, DECISIONS_1019a)

- **DESK APPROVED (2026-10-20):** "Yes, build it". The big papers mat sits in the centre. The traveller is behind glass, with a DEAL TRAY that carries papers in and out over the counter. DETAIN, AVAILABLE and the bell sit on the counter. The intercom, the rulebook pocket and the PC go on the left; the stamps (front-to-back), the scanner and the chip reader on the right. Empty homes wait for future tools. Direction: "Mix: B + A rail + C lamps".
- **Home map answers:** props, "we will cut all but decor objects later will go on clear places like restory" (DECISIONS_1019a: "Cut the phone, calculator, binder, pen pot, stapler and ink pad"); mat, "keep a big mat size"; stamp rack, daters front-to-back. The phone is **not** cut in this build: see open call 1.
- **D2:** "I want the desk to have a place for everything even if it is not unlocked yet ... our rule is to have a clean organized desk."
- **D5:** "intercom to bring out dialogue options" (the one talk route).
- **Desk camera and fit:** "the pc facing the wrong way? stick to the concept". The art PC (`CRT2_*` in AnimeHall) already stands at yaw 160°, 20° turned toward the clerk as in concept B, so nothing is turned here. Recipe 9 checks it on screen. The rest of that call (the desk set's look, the glass divider) is the art pass (art requests below).

## Open calls for Saleh (before the demo)

1. **The phone (demo blocker until answered).** Saleh said "Cut the phone". This build keeps the art's phone (`Clean_Phone`) on the desk: it is moved to the intercom's home, shrunk, and its click box opens the wheel (`OfficeSceneBinder.BindIntercom`). That follows SPEC Q1 A, which is **our** proposal ("its model can serve as the intercom's fallback art"), and FIX_PLAN_v2's fallback wording. It is not his call. It also undoes CUTS' P0 state, where the phone was squash-only. FIX_PLAN_v2's check "The traveller and the phone open nothing (re-run CUTS #4)" now passes only because the phone has become the intercom. A phone model on the desk needs his OK, or the A2 intercom art, before the demo.
   - **If he says cut:** two data edits, no code. In `DeskHomeMap.Approved()`, set the intercom home's `artParts` to `""`. In `DeskLayoutSettings.cutParts`, add `"Clean_Phone"`. Then the phone's parts are switched off at load, the `Intercom` anchor has no bounds, the builder's `Props/Intercom` box switches itself off (`BindProps`), and `DeskFurniture` builds the stand-in console on the same home, with the same wheel call and guide target. Re-run recipe 4 and CUTS #4.
   - **If he OKs the phone:** nothing changes. Strike this call.
   - **When A2 lands:** DESK-HOMES swaps the model onto the `intercom` home. The phone is cut as above.

## The home map (`Visuals/DeskHomeMap.cs`, `DeskHomeMap.Approved()`)

The desk frame is metres right of (x) and away from the chair (y), measured from the art mat's centre (`DeskFrame`, unchanged). The mat is the art's big blotter, 1.46 x 1.03 m (x ±0.73, y ±0.515), and holds papers and the open rulebook only. Footprints are the object's own (across x front to back) before its yaw; + yaw turns the front toward the viewer's left.

| Id | Centre (x, y) | Size | Yaw | Height | Shows | Slides | Group |
|---|---|---|---|---|---|---|---|
| `chipReader` | 0.895, -0.375 | 0.21 x 0.21 | 0 | 0.07 | when owned | left 6 cm | rail, front (1st use) |
| `scanner` | 1.02, -0.07 | 0.46 x 0.34 | 0 | 0.13 | when owned | left 6 cm | rail, middle |
| `stampRack` | 0.83, 0.35 | 0.32 x 0.12 | **90** (daters front to back) | 0.08 | always | - | rail, back, by the counter |
| `dealTray` | 0, 0.545 | 0.44 x 0.06 | 0 | 0.01 | always | - | counter, right above the mat |
| `detain` | -0.51, 0.74 | 0.26 x 0.28 | 0 | 0.09 | always | - | counter row |
| `bell` | 0.43, 0.67 | 0.14 x 0.14 | 0 | 0.08 | reserved (BELL-SIGN) | - | counter row |
| `rolodex` | 0.91, 0.71 | 0.16 x 0.16 | 0 | 0.17 | when owned | - | counter row, right of the bell, behind the stamp rack |
| `glasses` | -0.73, 0.575 | 0.14 x 0.09 | 0 | 0.03 | reserved (GLASSES) | - | counter row, left |
| `protocolRing` | -0.74, 0.74 | 0.12 x 0.12 | 0 | 0.21 | when owned | - | counter row, left |
| `intercom` | -0.88, 0.40 | 0.24 x 0.20 | 0 | 0.12 | always (the art's `Clean_Phone`, **pending open call 1**) | - | left: ask |
| `babelTuner` | -0.90, 0.12 | 0.18 x 0.20 | 0 | 0.15 | when owned (the BABEL rack until BABEL-TUNER) | right 6 cm | left |
| `rulebookPocket` | -0.88, -0.12 | 0.24 x 0.26 | 0 | 0.03 | reserved (DESK-HOMES) | - | left: look up |
| `uvTorch` | -0.84, -0.44 | 0.24 x 0.12 | -90 | 0.06 | when owned | - | left, by the mouse |
| `mouse` | -1.145, -0.58 | 0.19 x 0.28 | 0 | 0.04 | always (the art's `Clean_Mouse`) | - | the PC's |
| `thumbprint` | 1.16, -0.375 | 0.20 x 0.21 | 0 | 0.05 | later (not in days 1-15) | - | beside the rail |
| `transponderTuner` | 1.45, -0.43 | 0.28 x 0.26 | 0 | 0.10 | later | - | beside the rail |
| `boothCamera` | 1.08, 1.225 | 0.36 x 0.45 | 0 | 0.30 | later | - | back right |
| `xray` | 1.57, 1.225 | 0.50 x 0.45 | 0 | 0.30 | later | - | back right |
| `switchboard` | 2.22, 1.30 | 0.56 x 0.45 | -20 | 0.40 | when owned (the RING SERVICE panel until PORTALS) | - | booth's right end |
| `crate` | 1.56, 0 | 0.48 x 0.34 | 8 | 0.30 | delivery mornings | - | right of the rail |
| `decor` | 1.28, 0.545 | 0.56 x 0.65 | 0 | - | decor start zone (the form sorter behind the tray) | - | right of the rail, left of the camera's line to the switchboard |

Fixed art the homes keep clear of (`DeskHomeMap.FixedArt()`, measured on the R7 day-12 top-down capture, rounded out): `pc` (-1.57, 0.26; 1.10 x 1.16; 0.62 tall), `till` (-1.085, 0.90; 0.49 x 0.42; 0.30), `sign` (0, 0.685; 0.56 x 0.21; 0.23), `lamp` (0.46, 1.25; 0.52 x 0.50; 0.50).

**Choices that are ours (proposals, data in `DeskHomeMap`; a change is a data change):**
- **The positions.** They follow desk_simple_1.png and concept B around the big mat. SPEC.md's numbers were drawn for the 1.00 x 0.72 mat Saleh turned down.
- **The Rolodex's 16 cm home** (was 18 cm at y 0.60). The click-box check found that its box (+1 cm) reached into the till's. At 16 cm it clears every other box. Its model is shrunk to fit, about 11% smaller. **Integration (recipe 4):** at (-0.91, 0.595), behind the intercom, the ID buffer saw 60-63% of it at day 12 (the phone in front of it hid 40%), so it moved to recipe 4's fallback, (0.91, 0.71): the counter row right of the bell, behind the stamp rack. There it is 100% seen at A1 and A2, and recipe 3 stays green.
- **The phone as the intercom.** Not decided: see open call 1.
- **The rail order, front to back:** chip reader, scanner, stamp rack. The rack ends beside the deal tray (`DeskHomeMap.Rail`), as concept B's "ending beside the deal tray".
- **Slide axes sideways toward the mat** (SPEC P8, the declared change to Bible C6's "toward the chair"). Each sweep is checked to stay off the mat, other homes and the fixed art. The slide-in motion itself is DESK-HOMES'; only the axis is data here.
- **The RING SERVICE panel stands on the switchboard's home.** One home serves the board's id now, and PORTALS swaps the switchboard in (Saleh: "on the booth wall, right of the desk").
- **The decor zone** at (1.28, 0.545), 0.56 x 0.65 m. **Integration (recipe 4):** from (1.80, 0.55), 0.80 x 0.60, the form sorter (36 cm tall, the decorations' start) stood on the office camera's line to the switchboard's home and hid 80% of the RING SERVICE panel (20% seen at A1 and A2), and its handle covered the panel's breaker. The zone now stands left of that line, deep enough for the sorter behind the tray (each settles nearest its art spot), and clear of the crate. The panel is 100% seen. The plan's sight check cannot see this (a decoration's height is not a home's); the ID buffer is the measure.
- **The BABEL rack stands on the tuner's home** (SPEC P6). BABEL-TUNER swaps the object in place.
- **The C lamp hook.** `NextToolLamp.For(CaseLampState)` picks the home that lights:
  - between travellers while the booth is open, the bell;
  - a paper owed to a machine, that machine (chip reader before scanner, the rail's order);
  - a stamped passport, the deal tray;
  - otherwise none.

  `DeskLayout.Lamps` (a `DeskLamps`: `Lit`, `Changed`) holds the answer, and `DeskLayout.TryLampPoint(id)` gives the world point (the middle of the home's front edge). `BoothCoordinator` feeds it. The lamp's look, light and sound belong to FEEDBACK and DESK-HOMES. `ChipOwed`/`ScanOwed` are false until FEEDBACK fills them.

## Changes by BUG id

| BUG | Part | Change |
|---|---|---|
| 4 (S1) | 4a placement, occlusion, plan check | Every object stands on its home (`DeskLayoutSettings.homes`). `DeskArrangement.HomeProblems` is yaw-aware and checks each home against: the desk top; the mat; every other home; the fixed art; the 6 cm slide sweeps; and the office camera's line of sight (a 5 x 5 ray grid over each shown object's top, against every taller box: `VisibleShare` ≥ 90%). `CheckDeskPlan` **throws**, so Build Office UI stops on any problem and never saves over a broken plan. The Rolodex left the till's shadow (counter row, left). The cut props are switched off. Gate checks: `desk.plan.homes`, `desk.plan.everyHome`, `desk.plan.cutProps` (static), plus `desk.play.visible`/`clicks` (ID buffer: `deskvisible` op). **Click-box overlap, both halves.** Static: `DeskArrangement.ClickBoxProblems` is part of `DeskLayoutSettings.Problems`, so `CheckDeskPlan` and `desk.plan.homes` fail on it. It grows every shown home's footprint by the builder's pad, `DeskHomeMap.ClickPad` 1 cm, which is now `PropClick`'s default. Art objects keep their own bounds: the moved phone and mouse, and the fixed art. The deal tray and the mouse have no box (`DeskHomeMap.NoClickBox`). It found the Rolodex's box reaching into the till's, so the Rolodex home is now 16 cm. Play: the rig op `deskclickboxes` takes every shown `Clickable` `BoxCollider` over the desk and off the mat (the art props' boxes from `PlaceBox` included), groups them by desk object (`DeskLayout.HomeIdOf`), and measures each pair with `Physics.ComputePenetration`. `DeskArrangement.ClickBoxClashes` fails any pair of different objects deeper than 5 mm. Gate `desk.play.clickBoxes` (days 1 and 12). The upgrades' objects and the moved art props are shrunk to fit their home when bigger (`DeskLayout.FitScale`, `MoveArtProp`). The torch's box (4b) is TOOLS'. |
| 7 (S2) | 7a layout, mat, plan check | Nothing but papers on the mat: no home overlaps it, checked by the gate and by `TheApprovedMap_HasNoProblem_OffTheMat_ApartAndSeen`. The machines face the chair (yaw 0) on A's rail right of the mat. `DeskLayout.Mat` (`DeskMat`) is the art mat in the frame at bind, for DOC-HANDLE. |
| 15 (S2) | 15b rack bay off the mat | The stamp rack's home is at x 0.77..0.89, which is 4 cm off the mat's right edge, by the counter, its daters front to back (yaw 90). |
| 17 (S2) | 17b Rolodex button filter | `Rolodex` takes only the left button for clicks and drags; a right-click does nothing (it backs out, ControlRules). |
| 23 (S2) | decorations | A decoration rests where it is let go on any free spot off the mat: on the desk, clear of the mat, every home, the fixed art and the other decorations, nudged at most 3 cm (`decorNudge`) on a 5 mm grid, else it goes back (`DeskArrangement.TrySettleFree`). It keeps its grab offset, lifts over obstacles (`DeskLayout.Obstacles.HeightAt` + 3 cm) and shows a shadow quad where it will rest. It registers as an obstacle where it rests. `Clickable` and the props skip `eventData.dragging`, so a drag never fires a click. The two remaining decorations (form sorter, tray) start in the decor zone. |
| 36 (S2) | 36b intercom art, primitives, risers, home map | **Pending Saleh (open call 1):** the art's phone (`Clean_Phone`) is moved to the intercom's home and serves as the intercom. This is our reading of FIX_PLAN_v2's fallback, against his "Cut the phone". Its click box opens the wheel (`OfficeSceneBinder.BindIntercom`), and the guide points at it. The phone's parts (body, dial, handset, cord) are grouped under one root at load (`Art_intercom`, `OfficeSceneBinder.GroupArt`), so the click box spans the whole phone, the hover outlines all of it and its reaction moves it as one (integration fix: the box, the outline and the reaction had covered the body alone, the one mesh the anchor names, and the ID buffer saw 80% of the intercom). The primitive console is built only when the art office has no intercom bounds, which is also what cutting the phone gives. The two shelves and their risers are gone, and locked homes show nothing (v2, R8). The calculator, binder, pen pot, stapler and ink pad are cut: their art parts are switched off at bind (`cutParts`), their contract specs and click boxes are removed, and the old stamp was already cut by CUTS. The deal tray is new: a brass plate with its slot, built in the art's materials until its model comes (art request A1). |
| 60 (S3) | 60a crate path | One arrival path. Everything delivered this morning waits at zero scale until the crate opens (`DeskUpgrades.Wait`). That covers the scanner's Auto-Feed tray and Analysis beacon, and the SERVICED sticker of a ring repair delivered to a RING SERVICE panel already on the desk (`DeskDeliveries.StickersToWait`; `RingPanel.Sticker`). Before, `RingPanel.SetDay` turned that sticker on at once, because the panel waited only when every serviced module was new (`UpgradeObjects.PanelNew`). That was the "some parts skip the crate" case CUTS handed over under 60a. A sticker left waiting gets its own size back at the next day (`RingPanel.SetDay`). The crate opens, then leaves in one motion: the tag drops back, the flaps fold, and the crate shrinks into its spot on an ease-in cubic over `leaveSeconds` 0.3 s (Bible C7: "InCubic 300 ms"). It never sinks through the desk, and the tag is hidden as it goes. It stands on its home right of the rail, in the office view. |
| 64 (S3) | 64a prop kicks | `Spring.Kick(speed, MotionAmount)` scales a kick by the comfort share, so Reduced Motion or Motion 0 adds no velocity. Used by the crate, the delivery pop, the protocol ring, the RING SERVICE breakers and the scanner's beacon. **Still open: the BABEL needle.** `BabelRack.cs:167` calls `_needle.Kick(4f)` in `Test` (the test chirp), which ignores Reduced Motion. The file is BABEL-TUNER's (LEASES; v2 removes it from DESK-PLAN), so it is handed off below. Until BABEL-TUNER lands, a Reduced Motion run still shows the BABEL needle overshoot. |
| 6 | 6c lens footprint | Moot: CUTS removed the lens stand and `TryLensFootprint`. Every shown object registers its home's footprint and real height in `DeskLayout.Obstacles` (`DeskObstacles`, DOC-HANDLE's registry): at bind, at each day's start and when the crate opens (`RefreshObstacles`), plus the fixed art and the decorations. |
| 28 (S2) | 28c/28d Deliver skips owned | `DeskDeliveries.ToUnpack`: the crate brings each of the day's deliveries once and skips any upgrade whose object already stood on the desk at the previous day start of this session. A cheat grant restarts the shift, so a grant made while an order is pending marks the item as on the desk, and next morning's delivery does not come in a second crate. The order book's cancel is RUN-SAFE's and UI-PC's; `Orders.CanOrder` is CONTENT's stub. |
| 55 (S3) | 55b decor and Rolodex holds | Each decoration (`DeskDecor.Item`) and the Rolodex are `IHoldable`s on `HoldRegistry.Desk`: Begin on drag, End on release. On `CancelHold`, a decoration goes back where its drag began and the drum lets go and settles. |
| v2 D5 | intercom in both views (P12) | `BoothRules`: the wheel open no longer forbids the reading view (`deskViewAllowed` = not in the PC frame and no newsletter), so the intercom opens the wheel where the player is and the reading view stays. The other live switches keep their wheel-closed rule. |

## Tests (outside Unity)

| Check | Result |
|---|---|
| `compile_check_any.py E:\unity\NOPE-t5` | 0 errors (Assembly-CSharp-Editor, TimeDeskEditMode; the rig op compiles in Assembly-CSharp) |
| Offline runner (`cc_t5`) | passed 3742, failed 0 (3733 at `4a6a0bf`, plus 9 new) |
| `tools/qa/editor_runner` (QA_PROJECT = t5) | passed 69, failed 0, 16 NeedsEditor skipped |
| `python -m unittest discover -s tools/qa` | 35 tests OK (the scenario-op test reads `desk_plan_homes.json` and `desk_plan_decor.json`, whose ops `deskvisible`/`deskexpect` are registered) |

New and changed EditMode tests:
- **`DeskArrangementTests`:** `TheApprovedMap_KeepsEveryTwoShownObjectsClickBoxesApart`, `ClickBoxProblems_GrowBuiltBoxesByThePad_NotTheArts`, `ClickBoxProblems_LeaveFootprintOverlapsToHomeProblems`, `ClickBoxClashes_NameEveryPairOfDifferentObjectsDeeperThanTheTolerance`, `ClickBoxClashes_ABoxWithNoOwner_IsItsOwnObject` (review fixes); `AFootprint_IsYawAware`, `HomeProblems_NameEveryKindOfFault`, `TheSightCheck_SeesOverLowThings_AndNotThroughTallOnes`, `ADecoration_LetGoOnAFreeSpot_RestsExactlyThere`, `ADecoration_LetGoJustOverAnEdge_IsNudgedAFewCentimetresAtMost`, `TheApprovedMap_GivesEveryObjectExactlyOneHome`, `TheApprovedMap_HasNoProblem_OffTheMat_ApartAndSeen`, `TheMat_IsTheBigArtMat_PapersOnly`, `TheRail_RunsRightOfTheMat_InTheOrderOfUse_FrontToBack_EndingByTheDealTray`, `TheStampRack_StandsWithItsDatersFrontToBack`, `TheMachines_SlideSidewaysTowardTheMat_AndOnlyThey`, `TheCountersRow_HoldsDetainAndTheBell_AndTheLeftHoldsTalkAndLookUp`, `ALampPoint_IsTheMiddleOfTheFootprintsFrontEdge`.
- **`DeskLampsTests`:** the bell between travellers only while open; the owed machine in the rail's order, then the deal tray once stamped; one lamp at a time, with change events; every lamp home is in the map.
- **`DeskDeliveriesTests`:** skips what already stood on the desk; each delivery once, in order; nothing delivered means no crate. Review fixes: a second ring repair on a standing panel waits for the crate; the first repair waits with the whole panel, so no sticker waits on its own; no new repair means no sticker waits; two new repairs both wait.
- **`PropKickTests`:** a kick under Reduced Motion and at Motion 0 adds no velocity; it scales with intensity.
- **`BoothRulesTests`:** the DeskViewAllowed column; the wheel opened in the reading view keeps the reading view allowed.

Play tests for the rig: `Assets/Tests/PlayMode/DeskPlan/Rig/DeskPlanOps.cs`.
- `deskvisible` renders an ID buffer from the pointer camera: own pixels versus pixels still seen over the opaque depth, asserting `visible.<name>` ≥ f (0.9).
- `deskexpect` asserts a bool member reads true.
- `deskclickboxes` asserts `clickboxes.<name>`: no two desk objects' real click boxes overlap deeper than f (5 mm). It reads every shown `Clickable` `BoxCollider` over the desk and off the mat, grouped by `DeskLayout.HomeIdOf`.

Scenarios: `tools/qa/scenarios/desk_plan_homes.json` and `desk_plan_decor.json`. `desk_plan_homes` covers day 1 at A1 **and** A2: visibility, the intercom from the office view, and the intercom from the reading view with its camera checks. It runs `deskclickboxes` on days 1 and 12, and day 12 at A1 and A2. Gate checks: `Assets/Editor/QA/Checks/DeskPlanChecks.cs`.

## Files (all DESK-PLAN's, FIX_PLAN v1 §3 + v2 delta + CARVE's map)

- **Modified:**
  - Scripts: `Office/DeskLayout.cs`, `Visuals/DeskArrangement.cs`, `Visuals/DeskMat.cs` (doc only), `Office/OfficeSceneBinder.cs`, `Office/OfficeSceneContractSO.cs`, `Office/BoothCoordinator.cs`, `Domain/BoothRules.cs`, `Domain/Springs.cs`, `UI/Clickable.cs`;
  - desk objects: `Office/Desk/{DeskFurniture, DeskDecor, DecorHandle, DeskHardware, DeskUpgrades, SupplyCrate, Rolodex, ProtocolRing, RingPanel, ScannerAddOns}.cs`;
  - builders: `Editor/OfficeSceneUIBuilder.{Desk, DeskPlan, Upgrades}.cs`;
  - data and tests: `Data/Config/OfficeSceneContract.asset` (the three cut specs; a shared data asset, flagged for INTEGRATION below), `Tests/EditMode/{BoothRulesTests, DeskArrangementTests}.cs`;
  - the rig scenario `tools/qa/scenarios/desk_plan_homes.json` (rewritten for A2 day 1 and the click boxes in the review fixes).
- **New:** `Visuals/DeskHomeMap.cs`, `Visuals/DeskLamps.cs`, `Domain/DeskDeliveries.cs`, `Editor/QA/Checks/DeskPlanChecks.cs`, `Tests/EditMode/{DeskLampsTests, DeskDeliveriesTests, PropKickTests}.cs`, `Tests/PlayMode/DeskPlan/Rig/DeskPlanOps.cs`, `tools/qa/scenarios/desk_plan_{homes,decor}.json`, this file.
- **Not touched:** `BabelRack.cs` (BABEL-TUNER's), `UpgradeObjects.cs` (CONTENT's: `StickersToWait` lives in `DeskDeliveries` and only calls `PanelNew`), `Tests/EditMode/DeskMatTests.cs`, `DeskConfigSO.cs`, `DeskCounter.cs`, `DeskSurface.cs`, `DeskStampTray.cs`, `DeskObstacles.cs`, `HoldRegistry.cs`, `docs/FEATURES.md` and `docs/SCENE_CONTRACT_GAMEPLAY.md` (for INTEGRATION, below). OfficeScene/AnimeHall are not edited: every move happens at bind.

## Hand-offs (other owners)

- **STAMP (#7):** the rack's home turns it 90° so the two daters stand front to back (Saleh's call). `DeskStampTray.Lay` already applies the bay's yaw. STAMP signs off on which dater is in front and on the hand-approach tip.
- **CAM (#6):** the big mat (Saleh's "keep a big mat size") means the reading pose backs out to frame 1.46 x 1.03 m plus the inner edges of both side columns. Homes 1-3 and 10-13 sit at x ±0.77..1.25, so the reading view needs ≥ 8 x 8 cm of each. The visibility criterion at A3/A4 is CAM's merge.
- **DOC-HANDLE (#8):**
  - Clamp papers to `DeskLayout.Mat.Rect` ∪ the scanner's and chip reader's drop rects (`HomeOf(scanner|chipReader).Footprint`).
  - Read `DeskLayout.Obstacles` for carry heights. It is filled here: the fixed art, every shown object, and the decorations.
- **DOC-LOOK (#9):** papers on screen shrink with the backed-out reading pose (SPEC Q5 B, about 67%), so text must grow about 1.5x. That re-layout is DOC-LOOK's.
- **FEEDBACK (#16) and DESK-HOMES (B3):**
  - draw C's lamp from `DeskLayout.Lamps` and `TryLampPoint`;
  - fill `CaseLampState.ChipOwed`/`ScanOwed`;
  - design the reserved and locked homes' look (felt cut-outs, brass plates), the rulebook pocket, and the slide-in along each home's `HomeSlide`.
- **BABEL-TUNER (B7), BUG-64a's last kick:** in `BabelRack.cs:167`, change `_needle.Kick(4f)` to `_needle.Kick(4f, UiMotion.Amount)` (`Spring.Kick(speed, MotionAmount)`, tested by `PropKickTests`). Do the same for every kick in the tuner that replaces the rack. Pass: under Reduced Motion, a click on the rack with no line to replay (`BabelRack.Test`, the test chirp) shows the needle with no overshoot frame (recipe 7, Reduced Motion line).
- **INTEGRATION (data steward, LEASES rule 11):** `Assets/Data/Config/OfficeSceneContract.asset` is a shared config asset. This track removed its three cut anchor specs (ids 20-22: Calculator, PenPot, Stapler) directly. CUTS edited the same asset before, and `OfficeSceneContractSO.cs` is DESK-PLAN's. Please confirm the edit at merge, or replay it as steward.
- **BELL-SIGN, GLASSES, BABEL-TUNER, PORTALS:** bind to `HomeOf("bell" | "glasses" | "babelTuner" | "switchboard")` and never edit `DeskLayout`. `BoothCoordinator.cs` is leased to BELL-SIGN, and `RingPanel.cs`/`.Ring.cs` to PORTALS.
- **DIALOGUE:** the intercom is the art's phone model under the `Intercom` anchor's click box, pending open call 1. If the phone is cut, it is the `DeskFurniture` stand-in on the same home, and the wheel call and guide target are unchanged. An `IntercomLamp` seam is not shipped; DIALOGUE takes the `DeskFurniture.cs` lease for the lamp hookup only (LEASES row).
- **Known limit (3D room only):** with `RunConfig.officeSceneName = OfficeScene` (not live; AnimeHall is), the intercom anchor resolves to `ImportedOfficeDress/Desk/Clerk hotline`, which is not a `Clean_Phone` part. It stays where the art put it and still opens the wheel.

## Art requests (target: Canva concept B, `E:\unity\NOPE-tools\design\desk\canva\B.png`, with A's bolted rail and C's lamps)

Nothing below is built here: the code uses the existing models plus the deal-tray plate. Each request goes through the GPT mailbox / the Blender desk-set pass that Saleh asked for ("please make the desk look like that with the divider"). Each model keeps its object's home footprint (table above) so it drops in as a data-free swap.

| # | Object | Brief | Fallback in game now |
|---|---|---|---|
| A1 | **Deal tray + glass divider** | The bank-teller deal tray recessed under the counter glass, brass-edged, 0.44 x 0.06 m on the desk (the dip runs under the glass to the traveller). The glass divider with its brass frame spans the counter between clerk and traveller (Saleh: "with the divider"). | A brass plate with a dark slot (`DeskFurniture.DealTray`), no glass |
| A2 | **Intercom** | A 1980s bakelite booth intercom (Bible D1): speaker grille, a TALK key that sinks 3 mm, a TALK lamp. 0.24 x 0.20 m, ≤ 0.12 m tall. | The art's phone (`Clean_Phone`) moved and shrunk to the home |
| A3 | **A's machine rail** | One bolted brass/steel rail right of the mat, carrying the chip reader, scanner and stamp rack front to back in fitted recesses (x 0.77..1.25, y -0.48..0.51). | None (machines stand on the desk top) |
| A4 | **B's fitted homes (shadow board)** | A walnut/felt recess a little larger than each object, with a blank brass plate, for every home id (always, when-owned and reserved), so a lifted or missing object shows. The felt cut-outs of locked homes are shaped like the tool to come: Rolodex, glasses, protocol cards, the five-slot BABEL tray, the UV torch pouch, and the rulebook pocket. DESK-HOMES places them. | Nothing (locked homes are positions only, v2/R8) |
| A5 | **C's next-tool lamps** | A small brass-rimmed indicator lamp at each lamp home's front edge (`LampPoint`): bell, chip reader, scanner, deal tray (more as FEEDBACK adds steps). Off, lit and pulse states. | Data only (`DeskLayout.Lamps`) |
| A6 | **Stamp rack, front-to-back** | The existing two-click self-inking daters (green APPROVED, red DENIED; Saleh: "the old stamp design that was satisfying"), in a brass two-bay rack laid front to back, flat (no incline: "i dont like that the stuff are tilted"). | The current rack turned 90° |
| A7 | **The bell** | The brass counter bell (concept B), for BELL-SIGN's home. | Nothing (reserved) |
| A8 | **Desk set materials** | Warm walnut top, brass trims, bakelite machines and a green leather blotter (the big mat), flat items throughout. | The art's current desk |

## FEATURES and shared docs (for INTEGRATION)

Per CARVE's and CUTS' rule, `docs/FEATURES.md` is not edited here. Rewrite these lines (numbers as of main 9b8ab5f).

**Line 53 ("The desk's plan ..."): replace the whole line with:**
- [ ] The desk home map (Saleh 2026-10-20, DESK APPROVED: "Yes, build it"; "Mix: B + A rail + C lamps"; "keep a big mat size"; the stamp rack's daters front-to-back; "we will cut all but decor objects later will go on clear places like restory"). Every object has exactly one home in the desk's own frame: `DeskHomeMap.Approved()` in `DeskLayoutSettings.homes`, read by id through `DeskLayout.HomeOf`, `TryPose` and `TryLampPoint`, placed by the office binder at load. The homes:
  - the big art mat (1.46 x 1.03 m) in the middle holds papers and the open rulebook only (`DeskLayout.Mat`);
  - the counter row: the deal tray right above the mat (the papers come in and go back over it), DETAIN, the bell's home and the Rolodex right of it (behind the stamp rack), and the glasses and protocol cards left of DETAIN;
  - left of the mat: the intercom (the art's phone, moved, until Saleh's open call 1; else the stand-in console), the BABEL tuner's home (the BABEL rack until then), the rulebook pocket's home and the UV torch's pouch;
  - right of the mat: A's rail in the order of use, front to back: the chip reader, the scanner, then the stamp rack with its daters standing front to back beside the deal tray;
  - the four later machines' homes and the switchboard's (the RING SERVICE panel stands there);
  - the crate's spot and the decor zone, right of the rail.

  Machines face the chair. A home whose object has not come shows nothing. Build Office UI stops on a broken plan (`DeskArrangement.HomeProblems`: a home off the desk, on the mat, over another home or the fixed art (PC, till, sign, lamp), a 6 cm slide sweep reaching any of them, or a shown object the office camera sees less than 90% of; `ClickBoxProblems`: two shown objects' click boxes, grown by the builder's 1 cm pad, overlapping; tested: `DeskArrangementTests`). The calculator, binder, pen pot, stapler and ink pad are cut (their art parts switched off at load). The two remaining decorations (the form sorter, the tray) start in the decor zone. Dragged (the desk's one hold, by the point grabbed), one lifts over what lies under it and shows its shadow where it will rest. Let go on any free spot off the mat, it rests there, nudged 3 cm at most, else it goes back (`DeskArrangement.TrySettleFree`; tested). A drag never fires a click. The places are saved per player (PlayerPrefs `TimeDesk.DeskDecor`). The next-tool lamp (C's lamps; `NextToolLamp`, `DeskLayout.Lamps`; tested: `DeskLampsTests`) names the bell between travellers while the booth is open and the deal tray once the passport is stamped; FEEDBACK draws it. The office view frames the whole desk (58 degrees, 5 further down). Waits for its Unity check (docs/pending/DESK-PLAN.md, RESULT).

**Line 52 (the upgrades' desk objects):**
- Replace "each on its bay of the desk's plan (`DeskLayoutSettings`: the crate off the mat's front right, the lens stand ... on the hall shelf; `upgradeScale` 1.25; papers keep clear of the lens stand, `DeskUpgrades.TryLensFootprint`; see The desk's plan)" with "each on its home of the desk home map (the crate right of the rail, the Rolodex and the protocol cards on the counter's row, the BABEL rack on the BABEL tuner's home, the RING SERVICE panel on the switchboard's home; `upgradeScale` 1.25, shrunk to fit its home; see The desk home map)".
- Replace "stands on its bay off the mat's front right once the briefing closes" with "stands on its spot right of the rail once the briefing closes".
- Replace "the day's new objects pop onto the desk and the crate sinks away" with "the day's new objects (the scanner's Auto-Feed tray and Analysis beacon too, and a ring repair's SERVICED sticker on a panel already on the desk, tested: `DeskDeliveriesTests`) pop onto their homes; the tag drops back, the flaps fold and the crate shrinks into its spot (ease-in cubic, 0.3 s); an upgrade already on the desk is never brought again (`DeskDeliveries`, tested: `DeskDeliveriesTests`); every prop kick follows Reduced Motion and the Motion intensity (`Spring.Kick`, tested: `PropKickTests`)".
- In the Rolodex sentence, after "a click flicks it (rolodex_flick)", add "(the left button only; a drag is the desk's one hold)".

**Line 55 (Traveller wheel):** until open call 1 is answered, replace "the desk's intercom on the counter (see The desk's plan) or the art's phone on the comms shelf while they are at the desk" with "the intercom left of the mat (the art's phone on its home, see The desk home map), from the office view or the reading view, which stays (tested: `BoothRulesTests`), while they are at the desk".

**`docs/SCENE_CONTRACT_GAMEPLAY.md`:**
- Row `Intercom` (line 62): "the art's phone: moved at load onto the desk home map's intercom home, its parts grouped as one (`Art_intercom`), where it is the intercom (its click box over the whole phone opens the wheel, `OfficeSceneBinder.BindIntercom`); `DeskFurniture` builds a stand-in only when the art has none".
- Row `Calculator`, `PenPot`, `Stapler` (line 65) and its AnimeHall row (line 106): replace with "cut (the desk home map, 2026-10-20): no spec; their art parts (`Clean_Calculator`, `Clean_Binder`, `Clean_PenPot`, `Clean_Stapler`, `Clean_Inkpad`) are switched off at load (`DeskLayoutSettings.cutParts`); the art side may delete them".

## RESULT

**Done outside Unity:**
- the home map and its gate (it fails the build);
- every owned BUG part above;
- the C lamp data hook;
- the tests listed (3742/0 offline, 69/0 editor-side, 35 python).

**Not decided here:** open call 1 (the phone). It blocks the demo until Saleh answers or A2 lands.

**Deferred: Unity.** Code only (another agent runs Unity in NOPE-t6). Run these in order in t5's editor through the job runner. Each pass line is a QA-rig result in `Library/QA/results` or a still checked by eye.

1. **Rebuild OfficeGameplay** (the builder changed: `BoothCoordinator.game` and `DeskUpgrades.scannerAddOns` are wired; the Calculator, PenPot and Stapler click boxes are removed).
   - Steps: `__EMPTY_SCENE__`, then Tools > TimeDesk > Build Office UI. Commit `Assets/Scenes/OfficeGameplay.unity`. Revert churn (csproj, ProjectSettings, LiberationSans Fallback).
   - Pass: the build completes with no `[TimeDesk] The desk's plan:` error. The semantic dump against main differs only by: `Props/Calculator`, `PenPot` and `Stapler` gone; `BoothCoordinator.game` set; `DeskUpgrades.scannerAddOns` set.
2. **EditMode suite in Unity** (TestRunnerApi job).
   - Pass: every test green except the known `UnitySkills...SceneSummarize_CountsObjectsCorrectly`.
3. **Static gate** (`QaJobs.Gate`).
   - Pass: `desk.plan.homes` ("21 homes, 0 problem(s)": the click-box lines `The click boxes of 'x' and 'y' overlap` included), `desk.plan.everyHome` ("0 home(s) missing") and `desk.plan.cutProps` ("0 cut prop(s) still in the contract") all green.
4. **Homes: visibility, clicks, click boxes and the intercom** (`QaJobs.Scenario desk_plan_homes`). Day 1 runs at A1 then A2, from the office view and from the reading view. Day 12, with all items, runs at A1 then A2.
   - Pass, day 1 at A1 and A2 (stamp rack, DETAIN, intercom, deal tray): `visible.a1_d1_*` and `visible.a2_d1_*` ≥ 90%.
   - Pass, day 12 at A1 and A2 (stamp rack, DETAIN, intercom, deal tray, scanner, chip reader, UV torch, Rolodex, protocol ring, BABEL rack, RING SERVICE panel): every `visible.a1_d12_*`/`visible.a2_d12_*` ≥ 90%. If the Rolodex fails, move its home to the fallback x 0.80..1.02, y 0.60..0.82 (a data change in `DeskHomeMap`; recipe 3 must stay green) and re-run.
   - Pass: `clickboxes.d1` and `clickboxes.d12` green ("N desk click box(es), 0 clash(es)"). A red one names each pair and its depth in mm. Move or shrink the home in `DeskHomeMap` and re-run recipes 3 and 4.
   - Pass: `presses` green (every click reached its object under the pointer).
   - Pass, A1 and A2: `expect.a1_d1_intercom_office`, `expect.a1_d1_intercom_reading_wheel`, `expect.a1_d1_intercom_reading_view_kept`, and the same three `expect.a2_d1_*`, all green.
   - Pass: `camera.intercom_reading` and `camera.a2_intercom_reading` show 0 camera moves from the press.
   - Pass (CUTS #4 re-run): a virtual click on the traveller opens no wheel. The phone opens the wheel only as the intercom, pending open call 1.
   - A3/A4: re-run at CAM's merge (#6).
5. **Decorations** (`QaJobs.Scenario desk_plan_decor`, A1, day 12).
   - Pass: the four `drop.*` steps ≤ 8 px at 1080p (≈ 3 cm).
   - Pass: in the `decor_after` still, the form sorter let go onto the scanner's home and the tray let go onto the mat are back where their drags began.
   - Pass: `camera.decor` shows 0 moves, and `presses` is green (no click reaction fired by a drag: the frames show no squash/pulse on release).
6. **Mat overlay**, days 1, 5, 12 and 15 (`cheat GiveAllItems`, then `jumpDay n`), office view and reading view at A1.
   - Take `still`s and overlay the mat rect (the blotter's screen rect).
   - Pass: 0 prop pixels inside the mat (papers and the rulebook only).
7. **Crate mornings**, one per desk upgrade (Rolodex, Interview Protocols, a Speech translator, a ring repair, **a second ring repair**, Auto-Feed, Analysis), at A1 and A2.
   - Steps: new run; `cheat` credits; order it on the PC's Orders app; finish the shift and the night; then `recStart crate`, click the crate, `wait 4`, `recStop`. For the second ring repair, order another ring's repair on a later day, with the panel already on the desk.
   - Pass: the object (the scanner's tray or beacon included) has zero scale until the flaps pop.
   - Pass, second ring repair: the panel stays on the desk, and the new module's SERVICED sticker is at zero scale until the flaps pop, then pops in with the crate. The other stickers stay as they were.
   - Pass: the crate never goes below the desk top: no frame shows it intersecting the desk, and no floating tag.
   - Pass: the crate's last frame is scale 0 within 0.3 s of leaving, and the new object is on screen at its home in the last frame.
   - Pass under Reduced Motion: the same morning shows the objects at once, with no overshoot frames.
   - Known red until BABEL-TUNER: under Reduced Motion, a click on the BABEL rack with no line to replay (the test chirp) still kicks its needle into an overshoot (`BabelRack.cs:167`, hand-off above). Record it, but do not fail this track on it.
8. **Delivery once** (BUG-28c/28d).
   - Steps: order a desk upgrade on sale that day (the Rolodex, else any desk object), then `cheat GiveAllItems` (the shift restarts with it), finish the day and the night, then play the morning.
   - Pass: no crate stands that morning when nothing else was ordered, the object stands once on its home and pops in no second time.
9. **Concept B fit and contact sheet.**
   - Steps: `still`s of the office view at A1, day 1 and day 15 (all items), and of the reading view (after `DeskView.TiltIn`).
   - Pass: the layout reads like concept B and desk_simple_1.png: the rail right, ask and look-up left, the counter row at the back. The PC is angled toward the clerk (its screen readable, not edge-on). No riser, shelf or empty bay shows: the ArtSlots placeholder scan reports 0 on the desk.
   - Send day 1 and day 15 to Saleh with every home labelled; his look is on the day-1 sheet.
10. **Rolodex controls.**
    - Steps: right-click the Rolodex; drag it; press Esc mid-drag.
    - Pass: the right-click flicks nothing (the drum angle is unchanged). The drag turns it. Esc lets go and the drum settles on a card, with `HoldRegistry.Desk.IsHolding` false after.
11. **Smoke** (`QaJobs.Smoke`).
    - Pass: the smoke day's console shows 0 errors, `presses` green, the next-tool lamp's `DeskLayout.Lamps.Lit` reads `bell` with the sign on and no traveller, then `dealTray` after stamping (read through `call`/`deskexpect` or the log).
