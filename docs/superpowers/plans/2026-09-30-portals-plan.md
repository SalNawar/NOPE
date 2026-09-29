# Portals, the Departure Board and Orders: the plan (v3)

> **For agentic workers:** one phase is one agent session on its own branch. Start a phase by writing its detailed task list (superpowers:writing-plans) from its entry below and the spec sections it names, then execute it (superpowers:executing-plans or superpowers:subagent-driven-development).
> - Spec: `docs/superpowers/specs/2026-09-30-portals-design.md` v3 (**S** below: S PO4 is its decision PO4, S §6.2 its section 6.2), with Saleh's answers to every question (S §14.1). S §14.2 lists the decisions made on his behalf; this plan builds their first options, and a different answer changes only the decision it names.
> - **Out of scope, by Saleh's decision:** the destination check (no routing directive, no L11, no evidence rule, no change to how destinations are drawn). Its seams are S SK1-SK4; build nothing for it. Nor the clerk-set routes or breakdowns (S SM1, SM2).
> - Also read: `docs/superpowers/specs/2026-09-26-pc-redesign-design.md` (**P**), the days 7-15 spec on `origin/design/days-7-15` (**D**), the redesign plan `docs/superpowers/plans/2026-09-26-redesign-plan.md` (its §0 "How every phase runs" applies to every phase here; its phase 22 and 23 notes), `docs/FEATURES.md`, `docs/ENGINEERING_MANIFESTO.md`, `docs/CONTENT_SHEETS.md`, `docs/SCENE_CONTRACT_GAMEPLAY.md`, `docs/reviews/MERGE_CRITERIA.md`.
> - House rules: `SCRATCH/HOUSE_RULES.md` and `SCRATCH/wave1/COMMON_BRIEF.md` ("Saleh's design rules (2026-09-29)" at its end override any spec text that disagrees). `SCRATCH` = `C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad`.
> - The code wins over this text: each phase re-reads the files it names before it edits them. Written against `main` `cdf10f7` and D at `24e7a8e`.

**Goal:** the hall's five rings become portals: 01 open from day 1; 02, the Return Gate (03), 04 and 05 under maintenance until the clerk orders their repair in the PC; the Directorate sets each portal's route, the PC's Portals app shows them, and the hall's board lists them (CLOSED, UNDER MAINTENANCE); each ring glows, dims or stays empty by its state and pulses when a traveller leaves through it, the Return Gate with its own effect. The Home shop's office upgrades move to a PC **Orders** app drawn as an upgrade tree with prerequisites and next-day delivery; Home keeps its evening and sells house upgrades. No traveller changes: `cases.txt` stays byte-identical.

**Architecture:** the rules are pure `TimeDesk.Domain`, tested first: the schedule and the departure rule (`PlaceKey`, `PortalSpec`, `PortalDay`, `PortalSchedule`), the tree (`UpgradeTree`: layout, unlocking, checks), the order log (`Orders`), the delivery mail (`Mailbox`), the household adjustment (`HomeRules.Adjusted`). Visuals: the board's text (`PortalBoardText`). Assembly-CSharp is glue: `ContentLibrarySO.BuildToday` builds the day's `PortalDay`; `DayCycle.AdvanceNight` delivers orders; `HomeEconomy` reads the household effect ops; the Orders and Portals windows; the board and the rings in the hall (`AnimeHallPortalLink`, `PortalEffect`). Content lives in `world_source.json` through Generate World, with `ContentSheetMap` entries and the template rewritten; prices, prerequisites, house effects and the hall's knobs are Inspector assets.

**Order:** the Orders tree first (the repairs need a place to be sold), then the House step, then the portals' schedule and the Portals app, then the hall with the one Unity session, then D's days and the balance policy.

---

## 0. How every phase runs

Everything in the redesign plan's §0 applies. In short, and what this plan adds:

- **An epic branch.** Phases PR1-PR4 change content and builders, so none can reach `main` without Generate World and the builders, and Saleh's rule is one batched Unity session ("never launch Unity without my OK; batch the Unity checks into one session"). So the epic lives on `redesign/portals` from the current `main`; each phase works on `redesign/portals-<n>-<name>` from the epic and merges into the epic after its offline gates; PR4's Unity session verifies the whole epic; then the orchestrator merges the epic to `main` once. PR5 is its own branch from `main`.
- **Worktree and editor:** the ones the orchestrator names. Never edit `E:\unity\NOPE`, never edit `Assets/Scenes/OfficeScene.unity` or the art (`Assets/Art/Office/AnimeHallLayers/**`: the scene, the prefab, the layers), never push, rebase or amend, never commit `_TimeDesk*` files or the `.sln`.
- **Tests first:** each task names its failing tests. Write them, see them fail (a compile error counts), implement, see them pass. Assembly-CSharp is not unit-testable (the test assembly sees Domain and Visuals only): a glue task's rules are the Domain calls tested before it, and Unity checks it.
- **Offline gates after every change:** `python SCRATCH/compile_check.py '<worktree>'` (0 errors in every project), then `SCRATCH/runner/bin/Debug/net10.0/runner.exe '<the checker's cc folder>\Temp\Bin\Debug'` (`passed N, failed 0`, N at least the phase's starting count plus its new tests).
- **Content:** `Assets/Data/World/world_source.json` (2-space indent, LF; edit it by script, keeping its formatting; a new field is omitted when blank) through Generate World; generated assets are never hand-edited. Every JSON field added gets its `ContentSheetMap` entry in the same commit (`ContentSheetMapTests` fails otherwise). Generate World and the template rewrite run in PR4's session.
- **Serialized enums are append-only** and pinned in `SerializedEnumsTests`. This plan appends `OfficeAnchorId.DepartureBoard`, `MailKind.Delivery` and three `EffectOpType` members (and pins `EffectOpType` from now on), and adds `UpgradeVenue`, `UpgradeBranch` and `PortalRole`. If another track appended first, keep `main`'s numbers and renumber ours after them, then rebuild what stores them.
- **The seams stay seams:** S SK1-SK4 (the delayed destination check) and S SM1-SM2 (clerk-set routes, breakdowns). Build nothing they would use: no routing rule, no destination draw over routes, no clerk route store, no breakdown rule.
- **Unity, one session** (PR4, with Saleh's OK). PR1-PR3 run the offline gates and, only through an editor the orchestrator already has open, the player-script compile and the EditMode suite. Generate World, the content template, the validator, Build Office UI, Build Home, the smoke play and every feature probe of the epic are batched into PR4. If no editor is open, ask Saleh before any launch. Plays run in the art office (the anime hall with `OfficeGameplay`, as the game loads them), never the old 2D booth. Screenshots at 1920 × 1080 and 1280 × 720 into `SCRATCH/portals/`. Afterwards revert what Unity touched outside the phase (`*.csproj`, `ProjectSettings/*`, the LiberationSans fallback asset) and delete the `_TimeDesk*` files and their metas.
- **Golden masters:** `cases.txt` must stay **byte-identical** (no traveller changes); the play transcript, the saves, two scene dumps and the data hashes change on purpose (S §12.2). PR4 re-packs once, with the diff explained in its commit.
- **Docs in the commit of the behaviour:** `docs/FEATURES.md` (S §13 lists the lines), `docs/CONTENT_SHEETS.md` when sheets change, P's rows the phase overrides (S §16), `docs/ART_ASSET_LIST.md` and `docs/SCENE_CONTRACT_GAMEPLAY.md` where named; "(tested: X)" only when test X exists and covers it.
- **Commits:** small, conventional, each compiling; every message ends with a blank line and `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>` (or the line the orchestrator names).
- **Before reporting:** merge the latest epic (or `main` for PR5), re-run the gates, and report `PORTALS PR<n> READY <hash>` with the numbers, the files carrying decisions made on Saleh's behalf, the open questions and the screenshots' folder.

## 1. The phases at a glance

| # | Phase | Scope | Size | Depends on |
|---|---|---|---|---|
| PR1 | Orders: the upgrade tree | `UpgradeVenue`, `UpgradeBranch`, `requires`; `UpgradeTree` (layout, unlocking, checks); `Orders` with Locked; delivery at the night's turn; the delivery mail; the Orders icon and its visual tree; the Home panel lists only Home items | L | `main` |
| PR2 | Home: the House step | the three household effect ops, `HomeRules.Adjusted`, `HomeEconomy` reading them; the four house upgrades and their effects; the panel renamed House | M | PR1 |
| PR3 | Portals: the schedule, the repairs, the Portals app | `PlaceKey`, `PortalSpec`, `PortalDay` (`DepartureFor`), `PortalSchedule`; days 1-6's Directorate routes; the four repair nodes; `BuildToday`; the Portals icon and its read-only window | L | PR1 |
| PR4 | The hall, and the one Unity session | `DepartureBoard` anchor, the board's rows and tooltip; `PortalEffect` and `AnimeHallPortalLink` (dim, glow, empty, pulse, the Return Gate's spiral, the sorting under each ring's frame); the scene contract and the art list; the epic's Unity verification and golden re-pack | L | PR1-PR3 |
| PR5 | Days 7-15's routes and the buyer policy | days 7-15's Directorate routes and `desk4_closes`; the simulation's buyer policy (BX2) | S | the epic on `main`; D's D4 on `main` (routes); phase 23 part 2 (policy) |

PR2 and PR3 are independent of each other and may run in either order after PR1.

## 2. The phases

### Phase PR1 — Orders: the upgrade tree (L)

- **Specs:** S OR1-OR9, §6, §8.1 (the tree, `Orders`, `MailKind.Delivery`), §8.2, §8.3 (`translation.packs[].requires`), §8.4 (the office upgrades' venue, branch, requires), §11.1 (section 3), §13 (Run & meta, Desk, Fake-OS desktop, Translation).
- **Scope:**
  - Domain: `UpgradeVenue` and `UpgradeBranch` (serialized); `UpgradeTree` (`TreeNode`, `TreeCell`, `Layout`, `Unlocked`, `Problems`); `Orders` (`OrderState` with `Locked` first, `StateOf`, `Place`, `Cancel`, `Due`); `MailKind.Delivery` and the delivery memos in `Mailbox`.
  - `UpgradeSO` gains `venue`, `branch`, `requires` (ids); the hand-authored office upgrades set theirs (the Analysis Scanner requires `scanner_autofeed`); Generate World writes venue Orders, branch Interview and `requires` (from `translation.packs[].requires`, blank) on the translators; the validator and the generator read `UpgradeTree.Problems`.
  - `WorldState.orders` (additive); delivery in `DayCycle.AdvanceNight` after the day turns (unlock, the unlock effect from the new day, `deliveredDay`); the statement's purchases at order and cancel.
  - The desktop: `DesktopAppIds.Orders` (`orders`) in `DefaultOrder` after `CitizenAccount`; its placeholder glyph; `icon.orders` (Flavour, every culture table), `window.orders`, `app.orders.*`, `mail.delivery.*`.
  - The Orders window (maximised by default): the tree canvas (`UpgradeTreeView`: pooled node templates, one `Graphic` for the links, the four states, scrolling, the keys) and the detail card (the TC-980 `FormSpecSO` row with Order and Cancel); `DesktopConfigSO`'s sizes.
  - Home: the panel lists only `venue = Home` upgrades (none until PR2; the step shows "Nothing for the house yet" and continues); `HandleBuyUpgrade` stays for them.
  - The translation notice (`translation.announce`): "Order a Speech translator in Orders today and it arrives tomorrow."
  - Art slots: `git mv` the office upgrade icons (`Assets/Art/UI/Resources/Home/upgrade_adv_scanner.png`, `upgrade_diplo_contacts.png`, with metas) to `Assets/Art/UI/Resources/Orders/`; the branch glyph slots `Orders/branch_<id>.png` with code-drawn placeholders.
- **Files:**
  - Domain: new `UpgradeTree.cs` (with `UpgradeVenue`, `UpgradeBranch`, `TreeNode`, `TreeCell`), new `Orders.cs`, `Mailbox.cs`, `DesktopAppIds.cs`, `ContentSheets/ContentSheetMap.cs`.
  - Visuals: `DesktopIconPlaceholder.cs` (the `orders` glyph; the branch glyphs; the padlock, clock and tick badges if drawn as glyphs).
  - Scripts: `UpgradeSO.cs`, `WorldState.cs`, `Core/DayCycle.cs`, `Home/HomeManager.cs`, `UI/HomeUIController.cs`, new `UI/Apps/OrdersWindow.cs`, new `UI/Apps/UpgradeTreeView.cs`, `UI/Apps/DesktopApps.cs`, the Mail window's list, `Office/DesktopConfigSO.cs`.
  - Editor: `OfficeSceneUIBuilder*.cs` (the icon, the window, the tree's templates, the TC-980 spec), `WorldContentGenerator.cs`, `ContentLibraryValidator.cs`.
  - Content: `Assets/Data/Upgrades/*.asset` (venue, branch, requires), `world_source.json` (`translation.announce`, `ui.strings`).
  - Tests: new `UpgradeTreeTests.cs`, new `OrdersTests.cs`, `MailboxTests.cs`, `DesktopLayoutTests.cs`, `DesktopIconPlaceholderTests.cs`, `SerializedEnumsTests.cs`, `ContentSheetMapTests.cs`.
  - Docs: `docs/FEATURES.md` (Run & meta: `orders`, the Home step; Desk: the scanners as nodes; Fake-OS desktop: seven icons, the Orders tree, the delivery memo; Translation), P DK1, SC1 and SC2 rows with a pointer to S, `docs/CONTENT_SHEETS.md`, `docs/ART_ASSET_LIST.md` (§3's Orders rows).
- **Tasks:**
  1. **Venue and branch.** Tests first, `SerializedEnumsTests`: `UpgradeVenue_KeepsItsSerializedInts` (Orders 0, Home 1), `UpgradeBranch_KeepsItsSerializedInts` (Desk 0, Interview 1, Portals 2, Contacts 3), each with its count. Then the enums and `UpgradeSO`'s three fields with doc comments; the hand-authored assets' values. Gates; commit.
  2. **The tree.** Tests first, `UpgradeTreeTests`: `Layout_OneBandPerBranchInEnumOrder`, `Layout_TierIsTheLongestChainInTheBand`, `Layout_SiblingsByCostThenId`, `Layout_ACrossBranchPrerequisiteKeepsItsOwnBand`, `Unlocked_EveryPrerequisiteOwned`, `Unlocked_APrerequisiteInTransitIsNotOwned`, `Problems_AnUnknownPrerequisite`, `Problems_ACycle`, `Problems_APrerequisiteAtAnotherVenue`. Then `UpgradeTree`. Gates; commit.
  3. **The order log.** Tests first, `OrdersTests`: `StateOf_LockedBeforeAnyOtherState`, `StateOf_OrderableWhenUnlockedNotOwnedNotInTransitAndAffordable`, `StateOf_TooDearWhenTheWalletIsShort`, `StateOf_InTransitWhilePending`, `StateOf_OwnedWhenOwned`, `Place_AddsAPendingEntryWithTheDayAndPrice`, `Place_RefusesALockedOwnedOrPendingUpgrade`, `Cancel_ReturnsTodaysPendingOrdersPriceAndRemovesIt`, `Cancel_RefusesAnOrderFromAnEarlierDay`, `Cancel_RefusesADeliveredOrder`, `Due_OrdersPlacedBeforeTheDayNotYetDelivered`, `Due_KeepsLogOrder`. Then `Orders`, `OrderEntry`, `WorldState.orders` (doc: additive). Gates; commit.
  4. **Delivery and the memo.** Tests first, `MailboxTests`: `Deliveries_OneMemoPerDeliveryDay`, `Deliveries_ListTheItemsInLogOrder`, `Deliveries_APendingOrderSendsNothing`, `Deliveries_IdIsDeliveryAndTheDay`; `SerializedEnumsTests` pins `MailKind` if it is stored (else a comment says why not). Then `MailKind.Delivery`, the memo builder, and `DayCycle.AdvanceNight`'s delivery (`Orders.Due`, `UnlockUpgrade`, `TimelineService.ActivateEffect(world, fx, "Upgrade: …", world.day, fx.defaultDurationDays, applyInstantOps: true)`, `deliveredDay`). Gates; commit.
  5. **The content.** Tests first, `ContentSheetMapTests`: `translation.packs[].requires` round-trips. Then the column, the generator's venue, branch and requires on the translators, `UpgradeTree.Problems` in the generator and the validator, `CONTENT_SHEETS.md`. Gates; commit.
  6. **The icon.** Tests first: `DesktopLayoutTests` (seven ids arranged; a saved layout without `orders` gains it in the first free spot), `DesktopIconPlaceholderTests` (`orders` has a glyph). Then the id, the order, the glyph, the strings. Gates; commit.
  7. **The window.** `UpgradeTreeView` (cells from `UpgradeTree.Layout` over the `venue = Orders` upgrades; node plates and badges by `Orders.StateOf`; links dim or accent; selection and keys), the detail card (TC-980; the price from `HomeEconomy.UpgradeCost`; Order: `world.money -= price`, `Orders.Place`, the statement; Cancel: the refund, `Orders.Cancel`, the statement), `OrdersWindow`, `DesktopApps`, the builder's templates. Gates; commit.
  8. **Home lists Home items.** `HomeUIController` filters `venue = Home`; the empty-list line; `HomeManager` unchanged otherwise. Gates; commit.
  9. **Art slots and docs.** The icons' `git mv`, the branch glyph slots, the translation notice, FEATURES, P's rows, the art list. Commit.
- **Unity** (only in an open editor; else PR4): the player scripts compile; the EditMode suite passes except the known third-party `UnitySkills…SceneSummarize_CountsObjectsCorrectly`.
- **Golden effect** (re-packed in PR4): `scene_OfficeGameplay` (the icon, the window, the tree's templates), `data_hashes` (the upgrades), the play transcript (the audit play job orders at the PC instead of buying at Home: update `tools/audit/unity/_TimeDeskAuditPlay.cs.txt`). `cases.txt` unchanged.
- **FEATURES:** Run & meta, Desk, Fake-OS desktop, Translation.
- **Split seam:** tasks 1-5 (the Domain, the delivery, the content) before 6-9 (the UI).
- **Depends on:** `main`.

### Phase PR2 — Home: the House step (M)

- **Specs:** S HM1-HM3, §7, §8.1 (Home), §8.4 (the house assets), §11.1 (section 6), §13 (Run & meta).
- **Scope:**
  - `EffectOpType` gains `SicknessChance`, `CareCost`, `HouseholdExpense` (appended; the enum pinned from now on); `TimelineEffects.SumFloat` sums them as it sums the pay rate; the validator's list of timed ops (the ops a dialog may not carry) includes them.
  - `HomeRules.Adjusted(value, bonus)` (never below 0); `HomeEconomy.ApplyDailyExpenses` (the base expense + `HouseholdExpense`), `GetCareCost` (+ `CareCost`, rounded half to even), `AdvanceFamilyConditions` (the worsen chance + `SicknessChance`) through it.
  - The four house upgrades (`Upgrade_House_AirFilter`, `…WaterPurifier`, `…MedicineCabinet`, `…Insulation`: venue Home; S §7's prices) and their effects (`Effect_House_*`, one op each, duration −1), listed in the content library.
  - The Home step is named House (the panel's title, its rows' "in force from tomorrow night" line); the Home upgrade icon slot now serves the house ids.
- **Files:** Domain `EffectOps.cs`, `HomeRules.cs`; Scripts `Timeline/TimelineEffects.cs` (if the sum needs no change, none), `Home/HomeEconomy.cs`, `UI/HomeUIController.cs`, `Home/HomeManager.cs` (the step's name); Editor `HomeSceneBuilder.cs` (the panel's title), the dialog-effect check in `WorldContentGenerator.cs` / `ContentLibraryValidator.cs`; content `Assets/Data/Upgrades/Upgrade_House_*.asset`, `Assets/Data/Effects/Effect_House_*.asset`, the library's lists; tests `SerializedEnumsTests.cs`, `HomeRulesTests.cs`; docs `docs/FEATURES.md` (the Home line), `docs/ART_ASSET_LIST.md` (§6).
- **Tasks:**
  1. **The ops.** Tests first, `SerializedEnumsTests.EffectOpType_KeepsItsSerializedInts` (every member today, then the three new ones, and the count). Then the members with doc comments; the dialog-effect check refuses them (timed ops). Gates; commit.
  2. **The adjustment.** Tests first, `HomeRulesTests`: `Adjusted_AddsTheBonus`, `Adjusted_NeverBelowZero`, `Adjusted_ANegativeBonusLowersTheChance`. Then `HomeRules.Adjusted` and the three `HomeEconomy` reads. Gates; commit.
  3. **The house items.** The four upgrades and four effects (Inspector assets), the library's lists; the panel's title and row line; the icon slot. Gates; commit.
  4. **Docs.** FEATURES, the art list. Commit.
- **Unity** (only in an open editor; else PR4): compile and the EditMode suite.
- **Golden effect** (re-packed in PR4): `scene_HomeScene` (the panel's labels), `data_hashes` (the house upgrades and effects), the play transcript if the job buys a house item (it does not).
- **FEATURES:** Run & meta (the Home line).
- **Depends on:** PR1 (the venue).

### Phase PR3 — Portals: the schedule, the repairs, the Portals app (L)

- **Specs:** S PO1-PO7, RT1-RT4, PA1-PA3, OR5 (the repair nodes), CN1-CN2, SM1-SM2, §2, §3, §4, §8.1 (portals), §8.3, §8.4 (the repairs), §9 (days 1-6).
- **Scope:**
  - Domain: `PlaceKey`, `PortalRole` (serialized), `PortalSpec`, `PortalState`, `PortalRoute`, `PortalDay` (`Portals`, `DepartureFor`), `PortalSchedule` (`InService`: the service-state seam; `Resolve` over route requests: the route-request seam; `PortalProblems`; `DayProblems`).
  - Content: `agency.portals` (01 from day 1; 02, 03, 04, 05 by their repairs), `days[].portals` for days 1-6 (S §9); `ContentSheetMap` (`agencyPortals`, `dayPortals`); Generate World writes the `PortalSpec`s into the library's agency block and each day's routes into `DayPlanSO.directorateRoutes`; the validator and the generator read the checks.
  - The four repairs (`Upgrade_RepairPortal02`, `…ReturnGate`, `…Portal04`, `…Portal05`: venue Orders, branch Portals; 150, 200, 250, 350 cr; 04 requires 02, 05 requires 04), in the library's upgrades, so the Orders tree shows them.
  - `ContentLibrarySO.BuildToday` builds the day's `PortalDay` (`TodaysWorld.Portals`) from the plan's Directorate routes (the only route requests), the owned repairs, the day and the closures, for the game and the simulation; nothing in `CaseFactory` reads it.
  - The desktop: `DesktopAppIds.Portals` (`portals`) after `Investigation` (eight icons); its glyph; `icon.portals`, `window.portals`, `app.portals.*`. The Portals window: the TC-970 `FormSpecSO`, read-only (S §4): one row per portal from the day's `PortalDay`, the repairs' tree states and prices; `DesktopConfigSO`'s size (880 × 600 u).
- **Files:**
  - Domain: new `Portals/PlaceKey.cs`, new `Portals/PortalSchedule.cs` (with the types), `DesktopAppIds.cs`, `ContentSheets/ContentSheetMap.cs`.
  - Visuals: `DesktopIconPlaceholder.cs` (the `portals` glyph).
  - Scripts: `ContentLibrarySO.cs` (the agency block's portals; `BuildToday`), `TodaysWorld.cs`, `DayPlanSO.cs` (`directorateRoutes`), `GameManager.cs` (passes the world to `BuildToday`), `Editor/BalanceSimulation.cs` (the same), new `UI/Apps/PortalsWindow.cs`, `UI/Apps/DesktopApps.cs`, `Office/DesktopConfigSO.cs`.
  - Editor: `WorldContentGenerator.cs`, `ContentLibraryValidator.cs`, `OfficeSceneUIBuilder*.cs`.
  - Content: `world_source.json`; `Assets/Data/Upgrades/Upgrade_Repair*.asset` (+ metas).
  - Tests: new `PortalScheduleTests.cs`; `SerializedEnumsTests.cs`, `ContentSheetMapTests.cs`, `DesktopLayoutTests.cs`, `DesktopIconPlaceholderTests.cs`.
  - Docs: `docs/FEATURES.md` (World: the portals, their service, the routes, the departure rule; Fake-OS desktop: eight icons, the Portals app), P DK1, `docs/CONTENT_SHEETS.md`, `docs/ART_ASSET_LIST.md` (the repair icons, the Portals icon).
- **Tasks:**
  1. **Places and service.** Tests first, `PortalScheduleTests`: `PlaceKey_ParsesCountryColonEra`, `PlaceKey_RefusesABlankPart`, `InService_ByDayFromItsFirstDay`, `InService_ByADeliveredRepair`, `InService_WithBothWhicheverComesFirst`, `InService_NeverWithNeither`; `SerializedEnumsTests.PortalRole_KeepsItsSerializedInts`. Then the types and `InService` (its doc names the service-state seam, S SM2). Gates; commit.
  2. **Resolution and departures.** Tests first, `PortalScheduleTests`: `Resolve_APortalInServiceRunsItsRequest`, `Resolve_APortalUnderMaintenanceRunsNothing`, `Resolve_AClosedRouteIsMarked`, `Resolve_TheReturnsPortalHasNoRoute`, `Resolve_ListsEveryPortalInNumberOrder`, `DepartureFor_TheDisplacedByTheReturnGateInService`, `DepartureFor_TheDisplacedByPortal01BeforeTheRepair`, `DepartureFor_ACitizenByTheOpenRoutesPortal`, `DepartureFor_ACitizenByPortal01WhenNoOpenRouteServesThem`, `DepartureFor_NeverThroughAClosedRoute`. Then `Resolve` (its doc names the route-request seam, S SM1) and `DepartureFor` (its doc names the check pass's seam, S SK3). Gates; commit.
  3. **The content checks.** Tests first, `PortalScheduleTests`: `PortalProblems_NumbersUnique`, `PortalProblems_NeverInServiceIsAnError`, `PortalProblems_AnUnknownRepairIsAnError`, `PortalProblems_AtMostOneReturnsPortal`, `PortalProblems_ADeparturePortalInServiceOnDayOne`, `DayProblems_EachDeparturePortalNeedsOneRoute`, `DayProblems_ARouteOutsideTheWorldIsAnError`, `DayProblems_ADuplicateRouteIsAnError`, `DayProblems_TheReturnsPortalTakesNoRoute`, `DayProblems_Portal01sRouteMustBeOpen`, `DayProblems_AClosedRouteOnARepairPortalIsAllowed`. Then the checks. Gates; commit.
  4. **The content and the sheet.** Tests first, `ContentSheetMapTests`: the `agencyPortals` and `dayPortals` round trips. Then `world_source.json` (by script: `agency.portals`, days 1-6's `portals`), the map, the generator (`PortalSpec`s, `DayPlanSO.directorateRoutes`, the checks, refusing to write on an error), the validator, `CONTENT_SHEETS.md`; the four repair assets. Gates; commit.
  5. **The day's schedule.** `TodaysWorld.Portals`; `ContentLibrarySO.BuildToday(plan, world)` resolving it (the Directorate's routes as the requests, the owned repairs, the day, `plan.ClaimAllowed` as `closed`); every caller passes the world. Gates; commit.
  6. **The icon.** Tests first: `DesktopLayoutTests` (eight ids; the arrange grid's second column; a saved layout gains `portals` in the first free spot), `DesktopIconPlaceholderTests` (`portals` has a glyph). Then the id, the order, the glyph, the strings. Gates; commit.
  7. **The window.** The TC-970 spec, `PortalsWindow` (the rows from today's `PortalDay`; a repair's state and price from `Orders.StateOf` and `HomeEconomy.UpgradeCost`; the footer), the builder, `DesktopConfigSO`'s size. Gates; commit.
  8. **Docs.** FEATURES, P DK1, the art list. Commit.
- **Unity** (only in an open editor; else PR4): compile and the EditMode suite.
- **Golden effect** (re-packed in PR4): `scene_OfficeGameplay` (the icon and window), `data_hashes` (the day plans, the library, the four repairs). `cases.txt` unchanged.
- **FEATURES:** World, Fake-OS desktop.
- **Split seam:** tasks 1-5 (the schedule) before 6-8 (the app).
- **Depends on:** PR1 (the repairs are tree nodes; `UpgradeBranch.Portals`).

### Phase PR4 — The hall, and the one Unity session (L)

- **Specs:** S BD1-BD5, VX1-VX7, PO4, §5, §11, §12, §13 (Office scene).
- **Scope:**
  - `OfficeAnchorId.DepartureBoard` (appended: 23) and its fallback `16 Departure board blank display` in the contract asset (`Assets/Data/Config/OfficeSceneContract.asset`, `OfficeSceneContractSO`; ensured by `OfficeSceneContractTools`); the binder places the board's text and click box at its bounds and follows them.
  - Visuals `PortalBoardText` (`Rows`: number, era, place, CLOSED, RETURN GATE, UNDER MAINTENANCE; `Tooltip`).
  - The builder: `BoardRows` (world-space TMP on the `Gameplay` sorting layer, auto-sized, `hallBoardInk` and `hallBoardStateInk`) and the click box on `Interactable` with the reacting-prop tooltip (the tooltip template grows with its lines).
  - The rings: `PortalEffect` (one per portal, built in `OfficeGameplay`: an unlit additive sprite from the art slots `Office/portal_glow` and `Office/portal_return_glow`, code-drawn radial and spiral placeholders until they land) and `AnimeHallPortalLink` (added by the binder when the art office carries an `AnimeHallPresentation`): at the day's start, each effect's sorting layer Default and order = `FindLayer(bay id).sortingOrder − 1`, its place and size from the metal ring's bounds (re-fitted in `LateUpdate`), its state (glow, Return Gate spiral, or nothing); each ring's tint (`SetLayerTint`: `hallPortalIdleTint` under maintenance, white otherwise); on an accept, the pulse on `PortalDay.DepartureFor(claim, displaced)`; reduced motion. The `DeskConfigSO` knobs of S §8.4.
  - Docs: `docs/SCENE_CONTRACT_GAMEPLAY.md` (S §5.3, the Default-sorting exception included), `docs/ART_ASSET_LIST.md` (S §11.1's sections 1 and 2 rows, the totals).
  - **The epic's Unity session** (below).
- **Files:** Domain `OfficeContract.cs`; Visuals new `PortalBoardText.cs`; Scripts `Office/OfficeSceneBinder.cs`, `Office/OfficeSceneContractSO.cs`, new `Office/DepartureBoardView.cs`, new `Office/PortalEffect.cs`, new `Office/AnimeHallPortalLink.cs`, `Office/DeskConfigSO.cs`, the accept hook (`GameManager` or the booth's decision path, whichever raises the verdict); Editor `OfficeSceneUIBuilder.Desk.cs` (or the office part), `OfficeSceneContractTools.cs`; art slots `Assets/Art/UI/Resources/Office/` (the two placeholders are code-drawn, not files); tests `SerializedEnumsTests.cs`, new `PortalBoardTextTests.cs`; docs as above and `docs/FEATURES.md` (Office scene).
- **Tasks:**
  1. **The anchor.** Tests first, `SerializedEnumsTests.OfficeAnchorId_KeepsItsSerializedInts` (DepartureBoard = 23, the count 24). Then the member and the contract asset's fallback (no default pose). Gates; commit.
  2. **The board's text.** Tests first, `PortalBoardTextTests`: `Rows_EveryPortalInNumberOrder`, `Rows_NumberEraThenPlaceUpperCase`, `Rows_AClosedRouteEndsWithClosed`, `Rows_TheReturnGateRow`, `Rows_UnderMaintenance`, `Tooltip_ListsEveryPortalWithItsRingAndState`. Then `PortalBoardText`. Gates; commit.
  3. **The board in the hall.** The builder's `BoardRows` and click box, `DepartureBoardView` (fit to the bounds inset, `LateUpdate` re-fit, text from the day's `PortalDay`), the tooltip, the binder's placement. Gates; commit.
  4. **The rings.** `PortalEffect`, `AnimeHallPortalLink`, the placeholders, the knobs, the accept hook. Its rules are PR3's (`PortalDay`, `DepartureFor`); the session's probes check the drawing. Gates; commit.
  5. **Docs.** The scene contract, the art list, FEATURES (Office scene). Commit.
  6. **The Unity session** (with Saleh's OK; one session, jobs queued one at a time):
     - Merge the latest `main` into the epic; offline gates.
     - Generate World; Tools > TimeDesk > Write Content Spreadsheet Template; the validator (0 errors; the warnings read and explained).
     - Build Office UI and Build Home: commit `OfficeGameplay.unity` and `HomeScene.unity` (compared by semantic dump).
     - The player scripts compile; the EditMode suite passes except the known third-party failure.
     - **`cases.txt` byte-identical** to the baseline (the static case dump job); any difference is a bug in this epic.
     - The smoke play in the hall (Title → New Run, seed 12345 → day 1, one traveller decided, no errors in the log).
     - The feature probes, screenshots at 1920 × 1080 and 1280 × 720 into `SCRATCH/portals/`:
       1. Day 1: the board's five rows (01 with its era and place, four UNDER MAINTENANCE); the tooltip; ring 01 glows under its frame (the bay's front fence, the metal ring and the glass drawn over the glow; nothing of the glow outside the ring); rings 02-05 dimmed and empty; an accepted traveller pulses 01 (and one step with reduced motion).
       2. The Orders tree: every node's state (the Analysis Scanner and Portal 04 and 05 locked, their links dim); order the Portal 02 repair and the Return Gate's, cancel one (the wallet and the statement go back), order it again; keyboard navigation along the links.
       3. Day 2: the delivery memo; the nodes Owned; ring 02 at its colour and empty with its row CLOSED (day 2's route New Kingdom Egypt is closed); the Return Gate's spiral; the Portals app's rows and footer; Portal 04 now orderable.
       4. Home: the House step's four rows; buy the Air Filter; the next night's worsen chance and the Medicine Cabinet's treatment price read through the rules (a scripted check of `HomeEconomy`'s values).
       5. Day 5 (the Return Gate repaired in one run, not in another): an accepted displaced traveller pulses the Return Gate, or 01.
       6. The hall's full evening: the glows still read (unlit), the board's inks read (contrast measured on screen, the linear-colour note in the common brief).
       7. The pixel check behind VX6: no art layer at a glow's sorting order has pixels inside that ring's centre (a job reading the sprites and the glow bounds).
       8. Continue from a Home save keeps a pending order; the night delivers it.
     - The golden re-pack (`golden.py pack … --out docs/reviews/audit-baseline/golden`), with the diff explained in the commit (S §12.2; `cases.txt` unchanged); the audit play job updated to order at the PC.
     - Revert what Unity touched outside the phase; delete the `_TimeDesk*` files.
- **Golden effect:** the epic's, S §12.2, re-packed once here.
- **FEATURES:** Office scene.
- **Depends on:** PR1-PR3.

### Phase PR5 — Days 7-15's routes and the buyer policy (S)

- **Specs:** S §9 (days 7-15), PO5, §10.2 (BX2), §16 (D's row). D §3, §5 (`desk4_closes`).
- **Scope:**
  - Content (after D's D4 is on `main`): days 7-15's `portals` (S §9), `desk4_closes`' line ("From today its queue joins Desk 3, and its portal (05, upper right) is Desk 3's, still under maintenance.").
  - The balance simulation's buyer policy (after phase 23 part 2): each night the cheapest affordable house upgrade, each shift the cheapest orderable node, through the same `Orders` and Home rules; the summary reports what each purchase cost and when.
- **Files:** content `world_source.json`; Editor `BalanceSimulation.cs`; Domain a pure pick helper if the policy needs one; tests for it; docs `docs/FEATURES.md` (Content & tooling: the policy), D's seam row.
- **Tasks:**
  1. **The routes.** Days 7-15's `portals` and `desk4_closes` by script (the generator's checks, PR3's, cover them). Gates; commit.
  2. **The buyer policy.** Tests first for its pick (`BuyerPolicy_TheCheapestAffordableHouseUpgrade`, `BuyerPolicy_TheCheapestOrderableNode`, `BuyerPolicy_NothingWhenNothingIsAffordable`). Then the policy in the simulation. Gates; commit.
  3. **Docs.** FEATURES, D's seam row. Commit.
- **Unity** (with Saleh's OK; batched with D's D5 session if both are pending): Generate World; the 50-run simulation with and without the buyer policy (its summary into `SCRATCH/portals/`).
- **Golden effect:** `data_hashes` (days 7-15's plans); the balance summary. `cases.txt` unchanged.
- **FEATURES:** Content & tooling.
- **Depends on:** the epic on `main`; D's D4 on `main` (task 1); phase 23 part 2 on `main` (task 2).

## 3. Why this order

- **The Orders tree first** because it changes no traveller and every later phase sells something through it (the house items are its sibling venue, the repairs are its nodes).
- **The House step and the portals' schedule in either order** after PR1: one reads the venue, the other the tree's nodes.
- **The hall last** because it only shows what PR3 decides, and because it carries the epic's single Unity session: Generate World, both builders, the probes, the byte-identical `cases.txt` and the golden re-pack happen once, over the finished epic.
- **D's days after D** because their routes sit on D's closures.
- **Nothing for the delayed passes:** the destination check (S SK1-SK4) and Saleh's next portals pass (S SM1-SM2) plug into named seams; no phase builds ahead of his decisions.
