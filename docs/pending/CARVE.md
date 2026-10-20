# P0-A CARVE+SEAMS

Branch `fix/p0-carve`, cut from main `34e87b1`. Source: FIX_PLAN.md §3 "P0-A CARVE+SEAMS" and FIX_PLAN_v2.md "P0-A CARVE+SEAMS: additions".

**What it is:** no player change. The big files are split into partial files, each with one owning track, and the seams the tracks code against now exist as stubs that behave as today.

**What it is not:** no behaviour, scene, asset or content change. `docs/FEATURES.md` does not change, because no behaviour changes. INTEGRATION folds this file in as the ownership table.

## Checks (outside Unity)

| Check | Baseline (34e87b1) | After |
|---|---|---|
| `compile_check_t5.py` | 0 errors, 161 warnings | 0 errors, 161 warnings |
| Offline runner | passed 3706, failed 0 | passed 3714, failed 0 (the 3706, plus 8 new seam tests) |
| `carve_check.py` (new, `E:\unity\NOPE-tools\scripts`) | - | PASS: 144 MOVED, 1332 SAME, 17 INSERTION, 4 LISTED, 29 NEW-SEAM, 0 CHANGED/VANISHED/DUPLICATED |

`carve_check.py <worktree> [--base origin/main] [--head HEAD | --worktree-files]` works like this:
- It finds every class declared in a changed `.cs` file.
- It collects the members of each such class from all of its files, on both sides. A partial class counts as one class.
- It keys each member by its signature and compares the full text, including doc comments and attributes.
- The only differences it allows are the insertions, listed changes and seams named in its header.

It was checked against planted faults: one changed character in a moved method is reported as CHANGED, and a copied member is reported as DUPLICATED.

**Deferred to Unity:** these need the editor, and Saleh was on the PC.
- The semantic scene dump: Build Office UI, then the OfficeGameplay dump compared with 34e87b1's.
- The smoke play: Title, New Run, day 1, one traveller, and no console lines that 34e87b1 doesn't log.
- The EditMode suite in Unity.
- The import of the shader include and of DeskAnime.shader's two new properties.

## File → owner map (the track ownership table)

Rule: a file that is not listed here, and not listed in a track's Owns in FIX_PLAN, is not editable (v1 §0 rule 1).

### OfficeSceneUIBuilder (one partial class, 42 files)

| File | Owner | What it builds |
|---|---|---|
| `OfficeSceneUIBuilder.cs` (root) | QA-RIG | Build(), the canvas and layout helpers, the Check* gates, the desktop shell, the cursor feedback |
| `.Text.cs` *(new)* | TEXT-LANG | Text(), MakeButton() |
| `.Taskbar.cs` *(new)* | UI-PC | BuildTaskbar, TrayText, BuildWinControls, WindowControl, GlyphLine (+ XpGreen, XpRed, TrayRowFrom) |
| `.DeskPlan.cs` *(new)* | DESK-PLAN | CheckDeskPlan |
| `.Desk.cs` | DESK-PLAN | BuildOffice, BuildDesk, BuildDeskSlots, BuildCounter, PrimitivePart, UpgradePart, BuildDeskLines, the Ensure* helpers, the fallback HUD, the rest |
| `.DeskPaper.cs` *(new; v1 called it `.Paper.cs`)* | DOC-LOOK | BuildPaperTemplate, PaperText, FloatingNote, NoteBacking*, NoteMaterial, DocumentMesh, CelMaterial, EdgesMaterial, ShadowMaterial, DepthParts, BuildRulebook, FolderPoint, FolderSize (+ their Rulebook*/Note*/Photo* constants) |
| `.StampRack.cs` *(new)* | STAMP | BuildStampTray, DeskMaterial (+ Stamp* constants) |
| `.Chrome.cs` *(new)* | CAM | BuildInspectButton, BuildControls, Keyed, FlatText, BuildDeskViewBack, BuildDeskView, BuildCityView (+ their sizes) |
| `.Clock.cs` *(new)* | UI-PC | BuildCrtClock (+ CrtClockPlate) |
| `.Booth.cs` *(new)* | FEEDBACK | BuildTraveller, BuildTravellerWheel, BuildBubbleInput, BuildOverlayCallout, BuildOfficeCaseHud, TopStrip |
| `.PcFrame.cs` *(new)* | UI-PC | BuildPcDesktop, EnsureDesktopCamera, DeviceInk, BuildPcFrame, FramePixel, CloseButtonPixel, PowerButtonPixel, LedPixel (+ Desktop*/Frame* constants) |
| `.Kit.cs` | TEXT-LANG | the kit's fonts and roles (ApplyKit, ApplyKitByRole, RolePiece, Kit*) |
| `.KitTray.cs` *(new)* | UI-PC | ChromePiece: the chrome's kit pieces (taskbar, Menu button, tray glass, title bar, window body, menus, tooltips, toasts, badges, fields) |
| `.Verdict.cs` *(new)* | FEEDBACK | BuildVerdictRibbon, BuildCitationSlip (+ Citation*/CitedStamp* sizes) |
| `.Machines.cs` | DESK-PLAN | BuildDeskHardware, MachineArt, BuildChipReader, MachineReadout, the decal and chip reader materials |
| `.Upgrades.cs` | DESK-PLAN | BuildDeskUpgrades, the prop helpers, SupplyCrate, ProtocolRing, Rolodex, ScannerAddOns |
| `.Tools.cs` *(new)* | TOOLS | BuildUvTorch (its UvPool and its collider are inside it), UvTorchMaterial, BuildLensKit, Magnifier |
| `.Babel.cs` *(new)* | none in Phase A (reserved for BABEL-TUNER) | BuildBabelRack |
| `.Ring.cs` *(new)* | DESK-PLAN; leased to PORTALS after #5 | BuildRingPanel |
| `.Scanner.cs` | DESK-PLAN | (unchanged) |
| `.Drawer.cs`, `.Hardware.cs` | STAMP | (unchanged) |
| `.Forms.cs`, `.Art.cs` | DOC-LOOK | (unchanged) |
| `.Paper.cs` | DOC-LOOK (v1 §3 DOC-LOOK lists it by name) | (unchanged) **the morning paper** (see the decisions below) |
| `.HelixRiver.cs`, `.Portals.cs` | HALL-FX | (unchanged) |
| `.Guide.cs` | CONTENT | (unchanged) |
| `.Desktop.cs`, `.Windows.cs`, `.Apps.cs`, `.AppViews.cs`, `.App.cs`, `.Internet.cs`, `.Orders.cs`, `.CaseBoard.cs`, `.Panes.cs`, `.Search.cs`, `.Keys.cs`, `.PcForms.cs`, `.Tokens.cs`, `.Workbench.cs` | UI-PC | (unchanged) |
| `.Lights.cs` (to come) | LIGHT | new file in its track |

### Carved scripts

| File | Owner | Holds |
|---|---|---|
| `Scripts/Office/Desk/DeskDocument.cs` | DOC-HANDLE | zone, scale, dock, drag, feel, carry, input, fields (all serialized fields stay here) |
| `Scripts/Office/Desk/DeskDocument.Render.cs` *(new)* | DOC-LOOK | ApplySlotTint, ShowPhoto, ShowVerdict, Stamp, StampWord, PlaceMark, OverTheFills, ShapePaper, BuildDepth, BendNow, ShowBend, RestorePrints, Print, ShowPaperArt, PlacePatch, PlaceSeal, PrintLegend, PlacePhoto, HasPhotoItem, MeshBuilder |
| `Scripts/Office/Desk/DeskRulebook.cs` | DOC-LOOK | pages, print, look, "folder never bends", fields |
| `Scripts/Office/Desk/DeskRulebook.Motion.cs` *(new)* | DOC-HANDLE (BUG-6b) | Place, SetLift, SetCarryFloor, CarriedFrom, Refused (v1's "old :389-396 carry step"), Update, ShowMotion, Dropped (v1's ":892-896 PickUpPosition teleport"), and the motion state (_height/_restYaw, _hop, _floor, _carry, _physics; none serialized) |
| `Scripts/OfficeUIController.cs` | FEEDBACK | ShowVerdict, the ribbon and citation wiring, fields |
| `Scripts/OfficeUIController.Hud.cs` *(new)* | UI-PC (BUG-20a, 21a, 58a) | UpdateHud, CrtDate, Today, TrayDate |
| `Scripts/GameManager.cs` | FLOW | (unchanged but for the move) |
| `Scripts/GameManager.Bulletin.cs` *(new)* | TEXT-LANG | TodaysBulletin |
| `Scripts/Home/HomeManager.cs` | HOME | now calls NightRules for the starting care and the pay |
| `Scripts/Home/NightRules.cs` *(new)* | HOME (RUN-SAFE consumes it, BUG-28a) | StartingCare, AffordableCare, PayBills(ref care), DefaultNight |

### Seam files (new)

| File | Owner |
|---|---|
| `Scripts/Domain/InputGate.cs` | RUN-SAFE |
| `Scripts/Domain/WalletEvents.cs` | UI-PC |
| `Scripts/Domain/HoldRegistry.cs` (+ `IHoldable`) | DOC-HANDLE |
| `Scripts/Visuals/DeskObstacles.cs` | DOC-HANDLE |
| `Scripts/Visuals/DeskMat.cs` | DESK-PLAN |
| `Scripts/Office/Desk/DeskEvents.cs` | DOC-HANDLE |
| `Scripts/UI/Motion/Transit.cs` (`Transit`, `SceneTransit`) | FLOW |
| `Scripts/UI/Theme/UiKeys.cs` | TEXT-LANG |
| `Shaders/Include/DeskLight.hlsl` | LIGHT |
| Tests: `InputGateTests.cs` (RUN-SAFE), `WalletEventsTests.cs` (UI-PC), `HoldRegistryTests.cs` and `DeskObstaclesTests.cs` (DOC-HANDLE), `DeskMatTests.cs` (DESK-PLAN) | as named |

The insertion lines and seam stubs in other tracks' files belong to those files' owners from here on: OfficeControls (DOC-HANDLE), DeskInspect (CONTENT), DesktopWindowManager, PcFrame, OrderBook and ClerkAccountSource (UI-PC), HomeEconomy (HOME), ShiftScoring (TEXT-LANG), ShiftStrandings and TimelineService (FLOW), DevCheats (RUN-SAFE), CultureThemeService and ArabicShaper (TEXT-LANG), HallDayCycle and DeskAnime.shader (LIGHT), GuideDirector and Orders (CONTENT), HelixRiverMonitor (HALL-FX). The stub tests went into the owners' existing test files: HallDayCycleTests (LIGHT), OrdersTests (CONTENT), ArabicShaperTests (TEXT-LANG).

## Seams

| Seam | File | Owner (merge #) | Consumers | Stub (= today) | Test |
|---|---|---|---|---|---|
| `InputGate.Blocked` | Domain/InputGate.cs | RUN-SAFE (#3) | OfficeControls, DeskInspect, DesktopWindowManager (checks inserted) | always false | InputGateTests |
| `WalletEvents.Changed` / `Raise()` | Domain/WalletEvents.cs | UI-PC (#13) | raised after all 13 wallet writes | raised; no listener | WalletEventsTests |
| `HoldRegistry` + `IHoldable` (Begin, End, Cancel, IsHolding, OnFocusLost, `Desk`) | Domain/HoldRegistry.cs | DOC-HANDLE (#8) | STAMP, TOOLS, DESK-PLAN, DOC-LOOK, CAM, FEEDBACK | tracks one hold; nothing registers | HoldRegistryTests |
| `DeskObstacles` (Register, Unregister, HeightAt, Count) | Visuals/DeskObstacles.cs | DOC-HANDLE (#8) | DESK-PLAN, STAMP, TOOLS, FEEDBACK | empty registry | DeskObstaclesTests |
| `DeskMat` (Rect, IsOnMat) | Visuals/DeskMat.cs | DESK-PLAN (#5) | DOC-HANDLE, others | holds the rect it is given | DeskMatTests |
| `_DeskLightTint`, `_DeskLightLevel`, `DeskLight()` | Shaders/Include/DeskLight.hlsl | LIGHT (#11) | desk shaders | returns 1; included nowhere yet | deferred to Unity (shader import) |
| `Transit.Fade/Slide/Wait` | UI/Motion/Transit.cs | FLOW (#15) | every screen change | cuts instantly | deferred (Assembly-CSharp) |
| `SceneTransit.Load(scene)` | UI/Motion/Transit.cs | FLOW (#15) | RUN-SAFE (#3) | `SceneManager.LoadScene(scene)` | deferred (Assembly-CSharp) |
| `UiKeys` (VerdictCorrect, VerdictWrong, VerdictWrongPenalty, HomeTitle, HomePay) | UI/Theme/UiKeys.cs | TEXT-LANG (#12) | HOME, UI-PC, FEEDBACK | constants equal to today's keys | deferred (Assembly-CSharp) |
| `DeskEvents` (DocumentLanded, CaseEnded, UpgradeArrived, StackChanged + Raise*) | Office/Desk/DeskEvents.cs | DOC-HANDLE (#8) | DOC-LOOK (stack rank, BUG-40c), FEEDBACK | declared; nothing raises | deferred (Assembly-CSharp) |
| `CultureThemeService.ResetForNewDay()` | UI/Theme/CultureThemeService.cs | TEXT-LANG (#12) | RUN-SAFE (#3) | clears `DevToolsState.ForcedLeaderId`, the culture part of ResetAll | deferred (Assembly-CSharp) |
| `OrderBook.CancelPendingFor(world, item)` | UI/Apps/OrderBook.cs | UI-PC (#13) | RUN-SAFE (#3) | `Cancel(world, item)`, the existing cancel and refund | deferred (Assembly-CSharp) |
| `HallDayCycle.DuskHour` | Visuals/HallDayCycle.cs | LIGHT (#11) | RUN-SAFE (#3) | 18.5 (the cheat menu's Dusk) | HallDayCycleTests |
| `GuideDirector.IsShowing` | Office/GuideDirector.cs | CONTENT (#17) | FEEDBACK (#16) | true while the guide plate shows (`_showing != None`) | deferred (Assembly-CSharp) |
| `Orders.CanOrder(day)` | Domain/Orders.cs | CONTENT (#17) | UI-PC (#13), DESK-PLAN (#5) | true | OrdersTests |
| `NightRules` (StartingCare, AffordableCare, PayBills, DefaultNight) | Home/NightRules.cs | HOME (#14) | RUN-SAFE (#3) | today's starting care, pay with fallback and PetNight, moved out of HomeManager | deferred (Assembly-CSharp) |
| `PcFrame.CanOpen` (static) | Office/PcFrame.cs | UI-PC (#13) | DOC-HANDLE (#8): the F1 guard | true | deferred (Assembly-CSharp) |
| `HelixRiverMonitor.SetWarning(bool)` | UI/HelixRiverMonitor.cs | HALL-FX (#4) | FLOW (#15), CONTENT (#17) | no-op | deferred (Assembly-CSharp) |
| DeskAnime.shader `_PaintedHighlight`, `_InkRim` | Art/Office/DeskClean/DeskAnime.shader | LIGHT (#11) | STAMP (#7) | declared (Properties + UnityPerMaterial), read by nothing | deferred to Unity (shader compile) |
| `ArabicShaper.ToVisualLines(lines)` (per-line RTL) | Visuals/ArabicShaper.cs | TEXT-LANG (#12) | DOC-LOOK (#9), CONTENT (#17) | `ToVisual` on each line | ArabicShaperTests |

**Pass criterion "every stub is called from at least one test":**
- **Met** for the stubs in the pure assemblies (Domain, Visuals).
- **Not yet met** for the Assembly-CSharp stubs. TimeDeskEditMode references only TimeDesk.Domain and TimeDesk.Visuals, so it cannot reach them. Their tests wait for QA-RIG's Editor-side test assembly (P0-B) or a Unity EditMode job.

## Behaviour-neutral insertions

- **InputGate (BUG-68b).** `if (InputGate.Blocked) return;` goes at the top of three input handlers:
  - `OfficeControls.Update`;
  - `DeskInspect.MoveOn`, its polled left-press. LateUpdate only draws, and it stays ungated;
  - `DesktopWindowManager.Update`.
- **F1 guard (BUG-58c).** `if (!PcFrame.CanOpen) break;` goes first in `OfficeControls.Do`'s `ShowKeys` case. That is the action F1 (ControlKey.Help) resolves to.
- **Wallet signal (BUG-58a).** `WalletEvents.Raise();` goes right after each of the 13 wallet writes:
  - DevCheats.Run;
  - HomeEconomy: RollBreakIn, ApplyDailyExpenses, PayBills, BuyHouseUpgrade;
  - HomeManager.HandleSpin (2);
  - ShiftScoring: ApplyWrongDecision, ApplyCorrect;
  - ShiftStrandings.Strand;
  - TimelineService.ActivateEffect (AddMoney, inline in its `case`);
  - ClerkAccountSource.TakeInstalment;
  - OrderBook: Order, Cancel.

  Two money assignments do not raise:
  - DayCycle.NewWorld's object initializer (a new world, not a write to a live wallet);
  - a save's load (the whole world is replaced).

## Decisions taken in the carve (orchestrator's, not Saleh's)

1. **`.Paper.cs` already existed: it is the morning paper.** v1's desk-paper carve went to a new `.DeskPaper.cs` (DOC-LOOK). The existing `.Paper.cs` keeps the owner v1 names for it, DOC-LOOK. The morning paper's content is CONTENT's (BUG-44), so the orchestrator may move `.Paper.cs` to CONTENT.
2. **The magnifiers, BABEL and RING builds were in `.Upgrades.cs`, not `.Machines.cs`.**
   - `.Tools.cs` takes BuildLensKit and Magnifier from `.Upgrades.cs`, and BuildUvTorch and UvTorchMaterial from `.Machines.cs`.
   - `.Babel.cs` and `.Ring.cs` come from `.Upgrades.cs`.
   - "UvPool" and "torch collider" are blocks inside BuildUvTorch, not separate methods.
3. **`.KitTray.cs` needed the one non-move in the builder.** "The tray skin at ~644" is a case inside RolePiece's switch.
   - RolePiece's chrome cases (Taskbar through InputField) moved verbatim to `ChromePiece`, and RolePiece's `default` now returns `ChromePiece(tag, image)`.
   - The cases are disjoint, so the result is identical. carve_check's split check proves every old line is kept and only the scaffold is new.
   - Two other tray-related lines sit inside larger TEXT-LANG methods: the tray's text sizing (a case in ApplyKitByRole) and the taskbar button's width (in ApplyKit). They stay in `.Kit.cs`, and UI-PC asks TEXT-LANG for changes there.
4. **`.Verdict.cs` also takes the citation slip** (BuildCitationSlip and its sizes) as well as the ribbon. FEEDBACK rebuilds the slip (D13), and it should not edit TEXT-LANG's Kit.cs to do it.
5. **Helpers follow their only users.** An unlisted member moved with a group only under two conditions: every reference to it, across all 42 builder files, was in that group, and (for a static field) no static initializer depends on it either way. This avoids changing partial-class static initialization order. The followers are listed in the table above in parentheses, for example DeskMaterial → StampRack, TopStrip → Booth, and EnsureDesktopCamera and DeviceInk → PcFrame.
6. **Serialized fields never moved.** All fields of DeskDocument, OfficeUIController and GameManager stay in their main file, so Unity's field order is unchanged. Only DeskRulebook's non-serialized motion state moved, with its methods.
7. **DeskDocument's material swaps stay with DOC-HANDLE for now.** The examine material swap is inside `SetZone` (DOC-HANDLE's logic). OverTheFills (the watermark's material) moved to Render.
8. **NightRules shape.**
   - It is static in `Home/`, not in Domain, because HomeEconomy uses UnityEngine.
   - HomeManager.Start and HandlePay call it. HandleSleep still calls `HomeEconomy.PetNight` directly (unchanged).
   - `DefaultNight` chains the three steps for RUN-SAFE.
9. **One pre-existing stray doc line moved as it was.** It belonged to PrimitivePart and sits above BuildCrtClock's summary in `.Clock.cs`. Moves never edit text.
10. **Each carved source file's summary gained one line** naming where its parts went. These are type doc comments, not members.

## Merge note

The four editor-side moves change no scene. The OfficeGameplay rebuild must still be compared by semantic dump in Unity: see "Deferred to Unity".
