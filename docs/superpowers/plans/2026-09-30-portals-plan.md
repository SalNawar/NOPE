# Portals, the Departure Board and Orders: the plan

> **For agentic workers:** one phase is one agent session on its own branch. Start a phase by writing its detailed task list (superpowers:writing-plans) from its entry below and the spec sections it names, then execute it (superpowers:executing-plans or superpowers:subagent-driven-development).
> - Spec: `docs/superpowers/specs/2026-09-30-portals-design.md` (**S** below: S RT3 is its decision RT3, S §6.3 its section 6.3). Its §15 questions wait for Saleh; this plan builds the recommended options, and an answer that differs changes only the decisions it names (the phase that builds them re-reads §15 before it starts).
> - Also read: `docs/superpowers/specs/2026-09-26-traveller-types-design.md` (**T**), `docs/superpowers/specs/2026-09-26-pc-redesign-design.md` (**P**), the days 7-15 spec on `origin/design/days-7-15` (**D**), the redesign plan `docs/superpowers/plans/2026-09-26-redesign-plan.md` (its §0 "How every phase runs" applies to every phase here; its phase 9, 22 and 23 notes), `docs/FEATURES.md`, `docs/ENGINEERING_MANIFESTO.md`, `docs/CONTENT_SHEETS.md`, `docs/SCENE_CONTRACT_GAMEPLAY.md`, `docs/reviews/MERGE_CRITERIA.md`.
> - House rules: `SCRATCH/HOUSE_RULES.md` and `SCRATCH/wave1/COMMON_BRIEF.md` ("Saleh's design rules (2026-09-29)" at its end override any spec text that disagrees). `SCRATCH` = `C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad`.
> - The code wins over this text: each phase re-reads the files it names before it edits them. Written against `main` `cdf10f7` (phase 9 merged: the directives, their makers, L3-L5 and L10) and D at `24e7a8e`.

**Goal:** the hall's five rings become portals: the clerk sets each departure portal to an era and a place in a PC **Portals** app, a day ahead; the hall's departure board shows the day's routes; a citizen whose destination no open portal serves is a directive fault (and a citizen whose account books an unserved route while their papers name a served one is a new record lie, L11); and the Home shop's upgrades move to a PC **Orders** app that delivers at the start of the next day.

**Architecture:** the rules are pure `TimeDesk.Domain`, tested first: the schedule and its resolution (`PlaceKey`, `PortalSpec`, `PortalDay`, `PortalSchedule`), the demand weight (`PortalDraw`), the clerk's routes (`PortalSettings`), the routing predicate and makers (`Directives`, `CaseFacts`), L11 (`RecordLies`, `LieKinds`), the order log (`Orders`) and the delivery mail (`Mailbox`), each read by Generate World and the validator where it is a content check. Visuals: the board's text (`PortalBoardText`). Assembly-CSharp is glue: `ContentLibrarySO.BuildToday` builds the day's `PortalDay` for the game and the simulation alike; `CaseFactory` draws a citizen's destination over the routes; `DayCycle.AdvanceNight` delivers orders; two new windows; the board and the rings in the hall. Content lives in `world_source.json` through Generate World, with `ContentSheetMap` entries and the template rewritten.

**Order:** Orders first (it touches no traveller and gives the Portal 04 commission somewhere to be sold), then the routes and the generator, then the Portals app, then the routing directive and L11, then the hall and the one Unity session, then D's bookings and the balance hooks once D is on `main`.

---

## 0. How every phase runs

Everything in the redesign plan's §0 applies. In short, and what this plan adds:

- **An epic branch.** Phases PR1-PR5 change content and builders, so none can reach `main` without Generate World and Build Office UI, and Saleh's rule is one batched Unity session ("never launch Unity without my OK; batch the Unity checks into one session"). So the epic lives on `redesign/portals` from the current `main`; each phase works on `redesign/portals-<n>-<name>` from the epic and merges into the epic after its offline gates; PR5's Unity session verifies the whole epic; then the orchestrator merges the epic to `main` once. PR6 is its own branch from `main` after D lands.
- **Worktree and editor:** the ones the orchestrator names. Never edit `E:\unity\NOPE`, never edit `Assets/Scenes/OfficeScene.unity` or the art scene `Assets/Art/Office/AnimeHallLayers/AnimeHall.unity`, never push, rebase or amend, never commit `_TimeDesk*` files or the `.sln`.
- **Tests first:** each task names its failing tests. Write them, see them fail (a compile error counts), implement, see them pass. Assembly-CSharp is not unit-testable (the test assembly sees Domain and Visuals only): a glue task's rules are the Domain calls tested before it, and Unity checks it.
- **Offline gates after every change:** `python SCRATCH/compile_check.py '<worktree>'` (0 errors in every project), then `SCRATCH/runner/bin/Debug/net10.0/runner.exe '<the checker's cc folder>\Temp\Bin\Debug'` (`passed N, failed 0`, N at least the phase's starting count plus its new tests).
- **Content:** `Assets/Data/World/world_source.json` (2-space indent, LF; edit it by script, keeping its formatting; a new field is omitted when blank) through Generate World; generated assets are never hand-edited. Every JSON field added gets its `ContentSheetMap` entry in the same commit (`ContentSheetMapTests` fails otherwise). Generate World and the template rewrite run in PR5's session.
- **Serialized enums are append-only** and pinned in `SerializedEnumsTests`. This plan appends `TravelRuleType.PortalRouting`, `LieKind.Rerouted`, `OfficeAnchorId.DepartureBoard` and `MailKind.Delivery`, and adds `UpgradeBranch` and `PortalRole`. If another track appended first (D appends `TravelRuleType.TransponderRecall`), keep `main`'s numbers and renumber ours after them, then rebuild what stores them.
- **Unity, one session** (PR5, with Saleh's OK). PR1-PR4 run the offline gates and, only through an editor the orchestrator already has open, the player-script compile and the EditMode suite. Generate World, the content template, the validator, Build Office UI, Build Home, the smoke play and every feature probe of the epic are batched into PR5. If no editor is open, ask Saleh before any launch. Plays run in the art office (the anime hall with `OfficeGameplay`, as the game loads them), never the old 2D booth. Screenshots at 1920 × 1080 and 1280 × 720 into `SCRATCH/portals/`. Afterwards revert what Unity touched outside the phase (`*.csproj`, `ProjectSettings/*`, the LiberationSans fallback asset) and delete the `_TimeDesk*` files and their metas.
- **Golden masters:** every day of `cases.txt` changes (S §13.2), and the play transcript, the saves, two scene dumps and the data hashes too. PR5 re-packs once, with the diff explained in its commit.
- **Docs in the commit of the behaviour:** `docs/FEATURES.md` (S §14 lists the lines), `docs/CONTENT_SHEETS.md` when sheets change, P's and T's rows the phase overrides (S §17), `docs/ART_ASSET_LIST.md` and `docs/SCENE_CONTRACT_GAMEPLAY.md` where named; "(tested: X)" only when test X exists and covers it.
- **Commits:** small, conventional, each compiling; every message ends with a blank line and `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>` (or the line the orchestrator names).
- **Before reporting:** merge the latest epic (or `main` for PR6), re-run the gates, and report `PORTALS PR<n> READY <hash>` with the numbers, the files carrying decisions made on Saleh's behalf, the open questions and the screenshots' folder.

## 1. The phases at a glance

| # | Phase | Scope | Size | Depends on |
|---|---|---|---|---|
| PR1 | Orders: the upgrade tree on the PC | `Orders`, the order log, delivery at the night's turn, the delivery mail, `UpgradeSO.branch`, the Orders icon and window; the Home shop retires | L | `main` |
| PR2 | Routes: the portals serve the destinations | `PlaceKey`, `PortalSpec`, `PortalSchedule`, `PortalDay` in `BuildToday`; a citizen's destination drawn over the routes (the kind first); the Rules memo's schedule; the morning notices; the Portal 04 commission; days 1-6 content | L | PR1 |
| PR3 | The Portals app | `PortalSchedule.Choices`, `PortalSettings`, the Portals icon and window, tomorrow's preview | M | PR2 |
| PR4 | The routing directive and the rerouted booking | `PortalRouting` (predicate, makers, roll, fault, citation), day 2's closure out, L11 `Rerouted`, L5's worksite among the routes | L | PR2 |
| PR5 | The hall, and the one Unity session | `DepartureBoard` anchor, the board's rows and tooltip, the ring link (Q12); the scene contract and the art list; the epic's Unity verification and golden re-pack | M | PR1-PR4 |
| PR6 | Days 7-15 bookings and the balance hooks | Directorate bookings for citizen premades, days 7-15's routes and L11, `desk4_closes`, the simulation's portal report | M | the epic on `main`; D's D2 and D4 on `main` (bookings); phase 23 part 2 (the simulation) |

PR3 and PR4 are independent of each other and may run in either order after PR2.

## 2. The phases

### Phase PR1 — Orders: the upgrade tree on the PC (L)

- **Specs:** S OR1-OR10, §8, §9.1 (`Orders`, `UpgradeBranch`, `MailKind.Delivery`), §9.2 (`orders`), §12.1 (the icons), §14 (Run & meta, Desk, Fake-OS desktop, Translation).
- **Scope:**
  - `UpgradeBranch` (Domain, serialized: `Desk`, `Interview`, `Portals`, `Contacts`) and `UpgradeSO.branch`; the hand-authored assets set theirs; Generate World writes `Interview` on the four translators.
  - Domain `Orders` (`OrderState`, `StateOf`, `Place`, `Cancel`, `Due`) over `WorldState.orders` (`OrderEntry`, additive).
  - Delivery in `DayCycle.AdvanceNight` after the day turns: unlock, the unlock effect from the new day (the call `HomeManager.HandleBuyUpgrade` made), `deliveredDay`.
  - Mail: `MailKind.Delivery`, `Mailbox` builds one memo per delivery day from the log.
  - The desktop: `DesktopAppIds.Orders` (`orders`) in `DefaultOrder` after `CitizenAccount`; its placeholder glyph; the flavour label `icon.orders` in each culture table; the window title `window.orders`.
  - The Orders window: a `FormSpecSO` TC-980 (branches as sections, items as rows), `OrdersWindow` (the Order and Cancel buttons over each row's action cell, the wallet line), its builder, `DesktopConfigSO`'s size; the statement's purchases at order and cancel.
  - Home: `HomeManager` loses `ShowShop` and `HandleBuyUpgrade` (Expenses → Slot → Sleep); `HomeUIController` loses the shop and its page row; `HomeSceneBuilder` removes `ShopPanel`; `Paging` and `PagingTests` go.
  - The translation notice (`translation.announce`) reads "Order a Speech translator in Orders today and it arrives tomorrow."
  - The upgrade icons' slot moves: `git mv` `Assets/Art/UI/Resources/Home/upgrade_*.png` (with metas) to `Assets/Art/UI/Resources/Orders/`; the branch glyph slots `Orders/branch_<id>.png` with code-drawn placeholders.
- **Files:**
  - Domain: new `Orders.cs`, new `UpgradeBranch.cs` (in Domain, so the tests pin it), `Mailbox.cs`, `DesktopAppIds.cs`.
  - Visuals: `DesktopIconPlaceholder.cs` (the `orders` glyph; the branch glyphs).
  - Scripts: `UpgradeSO.cs`, `WorldState.cs`, `Core/DayCycle.cs`, `Home/HomeManager.cs`, `UI/HomeUIController.cs`, new `UI/Apps/OrdersWindow.cs`, `UI/Apps/DesktopApps.cs`, the Mail window's list (the new kind's glyph), `Office/DesktopConfigSO.cs`.
  - Editor: `HomeSceneBuilder.cs`, `OfficeSceneUIBuilder*.cs` (the icon, the window, the TC-980 spec ensured like the other page forms), `WorldContentGenerator.cs` (the translators' branch).
  - Content: `Assets/Data/Upgrades/*.asset` (branch), `world_source.json` (`translation.announce`, the `ui.strings` keys `icon.orders` (Flavour, every culture table), `window.orders`, `app.orders.*`, `mail.delivery.*`).
  - Tests: new `OrdersTests.cs`, `MailboxTests.cs`, `DesktopLayoutTests.cs`, `DesktopIconPlaceholderTests.cs`, `SerializedEnumsTests.cs`; `PagingTests.cs` deleted.
  - Docs: `docs/FEATURES.md` (Run & meta: the save's `orders`, the Home phase; Desk: the scanner line; Fake-OS desktop: seven icons, the Orders app, the delivery memo; Translation: the translators), P DK1 (count) and SC1 (the shop) with a pointer to S, `docs/ART_ASSET_LIST.md` (§3 and §6 rows of S §12.1 for the upgrade icons and branch glyphs; the Orders icon).
- **Tasks:**
  1. **The branch.** Tests first, `SerializedEnumsTests`: `UpgradeBranch_KeepsItsSerializedInts` (Desk 0, Interview 1, Portals 2, Contacts 3, and the count). Then the enum, `UpgradeSO.branch` with its doc comment, each hand-authored asset's branch, the generator's Interview on the translators. Gates; commit.
  2. **The order log.** Tests first, `OrdersTests`: `StateOf_OrderableWhenNotOwnedNotInTransitAndAffordable`, `StateOf_TooDearWhenTheWalletIsShort`, `StateOf_InTransitWhilePending`, `StateOf_OwnedWhenOwned`, `Place_AddsAPendingEntryWithTheDayAndPrice`, `Place_RefusesAnOwnedOrPendingUpgrade`, `Cancel_ReturnsTodaysPendingOrdersPriceAndRemovesIt`, `Cancel_RefusesAnOrderFromAnEarlierDay`, `Cancel_RefusesADeliveredOrder`, `Due_OrdersPlacedBeforeTheDayNotYetDelivered`, `Due_KeepsLogOrder`. Then `Orders`, `OrderEntry`, `WorldState.orders` (doc: additive, an older save loads it empty). Gates; commit.
  3. **Delivery.** `DayCycle.AdvanceNight` delivers `Orders.Due(world.orders, world.day)` after `day++`: `UnlockUpgrade`, `TimelineService.ActivateEffect(world, fx, "Upgrade: …", world.day, fx.defaultDurationDays, applyInstantOps: true)`, `deliveredDay`. The rule is task 2's; Unity checks the glue (PR5). Gates; commit.
  4. **The delivery memo.** Tests first, `MailboxTests`: `Deliveries_OneMemoPerDeliveryDay`, `Deliveries_ListTheItemsInLogOrder`, `Deliveries_APendingOrderSendsNothing`, `Deliveries_IdIsDeliveryAndTheDay`; `SerializedEnumsTests` pins `MailKind` if it is stored (else a comment says why not). Then `MailKind.Delivery` and its builder. Gates; commit.
  5. **The icon.** Tests first: `DesktopLayoutTests` (the default arrange with seven ids; a saved layout without `orders` places it in the first free spot), `DesktopIconPlaceholderTests` (`orders` has a glyph). Then `DesktopAppIds.Orders` and `DefaultOrder`, the placeholder glyph, the strings. Gates; commit.
  6. **The window.** The TC-980 `FormSpecSO` (ensured by the builder), `OrdersWindow` (rows from `ContentLibrarySO.Upgrades` grouped by branch in enum order, then by cost; states from `Orders.StateOf`; the price from `HomeEconomy.UpgradeCost`; Order: `world.money -= price`, `Orders.Place`, the statement's purchases; Cancel: the refund, `Orders.Cancel`, the purchases back), `DesktopApps` opens it, the builder builds it with the button templates. Gates; commit.
  7. **Home without the shop.** `HomeManager` (Expenses → Slot → Sleep), `HomeUIController` and `HomeSceneBuilder` (remove `ShopPanel` with `DestroyChildIfPresent`), `Paging.cs` and `PagingTests.cs` deleted, `ShopPricesTests` kept (the price rule stays). Gates; commit.
  8. **Art slots and docs.** The icons' `git mv`, the branch glyph slots, the translation notice, `docs/FEATURES.md`, P's DK1 and SC1 rows, `docs/ART_ASSET_LIST.md`. Commit.
- **Unity** (only in an open editor; else PR5): the player scripts compile; the EditMode suite passes except the known third-party `UnitySkills…SceneSummarize_CountsObjectsCorrectly`.
- **Golden effect** (re-packed in PR5): `scene_OfficeGameplay` (the icon and the window), `scene_HomeScene` (no shop), `data_hashes` (the upgrades' branch), the play transcript if the audit play job bought at Home (it now orders at the PC: update `tools/audit/unity/_TimeDeskAuditPlay.cs.txt`).
- **FEATURES:** Run & meta (the save, the Home phase), Desk (the scanners), Fake-OS desktop (the icon count, the Orders app, the delivery memo), Translation.
- **Split seam:** tasks 1-4 (the Domain and the delivery) can land before 5-8 (the UI and Home).
- **Depends on:** `main`.

### Phase PR2 — Routes: the portals serve the destinations (L)

- **Specs:** S PO1-PO6, RT1-RT5, RT8, GN1-GN6, CN1-CN3, §2, §3, §6.6 (the Rules memo), §7, §9.1-9.4, §10 (days 1-6).
- **Scope:**
  - Domain: `PlaceKey`, `PortalRole` (serialized), `PortalSpec`, `RouteSource`, `RouteNote`, `PortalRoute`, `PortalDay`, `PortalSchedule` (`InService`, `Resolve`, `Unserved`, `PortalProblems`, `DayProblems`), `PortalDraw.EraWeight`.
  - Content: `agency.portals` (five rows), `days[].portals` for days 1-6 (S §10), the notices `news.portals`, `news.portalRetuned`, `news.portalInService`; `ContentSheetMap` (`agencyPortals`, `dayPortals`, the news fields); `Upgrade_Portal04.asset` (branch Portals, 300 cr).
  - Generate World writes the `PortalSpec`s into the library's agency block and each day's routes into `DayPlanSO.directorateRoutes` (place profiles); the validator and the generator read `PortalSchedule.PortalProblems` and `DayProblems`.
  - `WorldState.portalSettings` (`PortalSettingEntry`, additive; empty until PR3). `ContentLibrarySO.BuildToday` builds the day's `PortalDay` (`TodaysWorld.Portals`) from the plan, the world (the clerk's routes, the owned upgrades, the day) and the closures, for the game and the simulation.
  - `CaseFactory`: the kind drawn before the destination; a citizen's era over the routes (`PortalDraw.EraWeight`) and a route of that era; the displaced over the world, as today; planned slots claim a route (a citizen) or an open place.
  - The Rules memo (TC-940) gains the "Portal schedule" section (the rows link to their reference row; day-layer search entries `rule:portal:{n}`); the mail memo carries it (the same builder).
  - The morning notices: the day's routes, each Directorate retune of a clerk route, each portal entering service.
- **Files:**
  - Domain: new `Portals/PlaceKey.cs`, `Portals/PortalSchedule.cs` (with the types), `Portals/PortalDraw.cs`; `ContentSheets/ContentSheetMap.cs`; `EntryKeys.cs` (the portal row key).
  - Scripts: `ContentLibrarySO.cs` (the agency block's portals; `BuildToday`), `TodaysWorld.cs`, `DayPlanSO.cs` (`directorateRoutes`), `WorldState.cs`, `CaseFactory.cs` (`PickEraFromPlan`, `PickPlace`, the order of the kind draw), `GameManager.cs` (the notices, `SetDirectives` with the schedule), the Rules view and the memo builder, `Editor/BalanceSimulation.cs` (it calls `BuildToday` with the world: no other change).
  - Editor: `WorldContentGenerator.cs`, `ContentLibraryValidator.cs`.
  - Content: `world_source.json`; `Assets/Data/Upgrades/Upgrade_Portal04.asset` (+ meta; listed in the content library's upgrades).
  - Tests: new `PortalScheduleTests.cs`, `PortalDrawTests.cs`; `SerializedEnumsTests.cs`, `ContentSheetMapTests.cs`, `EntryKeysTests.cs` (if the key has a test file).
  - Docs: `docs/FEATURES.md` (World; Investigation loop's memo line; Run & meta's save line), `docs/CONTENT_SHEETS.md`.
- **Tasks:**
  1. **Places and portals.** Tests first, `PortalScheduleTests`: `PlaceKey_ParsesCountryColonEra`, `PlaceKey_RefusesABlankPart`, `InService_ByDayFromItsFirstDay`, `InService_ByUpgradeOnceOwned`, `InService_WithBothWhicheverComesFirst`, `InService_NeverWithNeither`; `SerializedEnumsTests.PortalRole_KeepsItsSerializedInts`. Then the types. Gates; commit.
  2. **Resolution.** Tests first, `PortalScheduleTests`: `Resolve_AClerkRouteRunsWhenInTheWorldAndOpen`, `Resolve_AClosedClerkRouteRunsTheFirstFreeDirectorateRoute`, `Resolve_NotesClosedOutsideWorldTakenAndUnset`, `Resolve_AFallbackNeverDisplacesAValidClerkRoute` (the two passes), `Resolve_TwoPortalsNeverShareAPlace`, `Resolve_TheLowerNumberKeepsADuplicatedClerkRoute`, `Resolve_TheReturnsPortalServesNoRoute`, `Resolve_NotInServicePortalsAreListedWithoutARoute`, `Serves_OnlyTheDeparturePortalsRoutes`, `PortalFor_TheServingPortalOrZero`, `Unserved_OpenPlacesNoPortalServes`, `Unserved_ByEra`. Then `Resolve`, `Serves`, `PortalFor`, `Unserved`. Gates; commit.
  3. **Demand.** Tests first, `PortalDrawTests`: `EraWeight_IsThePlanWeightTimesTheRoutes`, `EraWeight_ZeroWithoutARoute`, `EraWeight_ZeroForAZeroPlanWeight`, and a worked table (S §7.2: routes 2 : 1 : 2). Then `PortalDraw`. Gates; commit.
  4. **The content checks.** Tests first, `PortalScheduleTests`: `PortalProblems_NumbersUnique`, `PortalProblems_NeverInServiceIsAnError`, `PortalProblems_UnknownUpgradeIsAnError`, `PortalProblems_AtMostOneReturnsPortal`, `PortalProblems_ADeparturePortalOnDayOne`, `DayProblems_FewerRoutesThanPortalsIsAnError`, `DayProblems_ARouteOutsideTheWorldIsAnError`, `DayProblems_AClosedRouteIsAnError`, `DayProblems_ADuplicateRouteIsAnError`, `DayProblems_DisplacedBeforeTheReturnGateWarns`. Then `PortalProblems` and `DayProblems` (the routing-first-day check arrives with PR4). Gates; commit.
  5. **The content and the sheet.** Tests first, `ContentSheetMapTests`: the `agencyPortals` and `dayPortals` round trips; the news fields. Then `world_source.json` (by script: `agency.portals`, days 1-6's `portals`, the three notices), the map, the generator (the agency block's `PortalSpec`s, `DayPlanSO.directorateRoutes`, the checks, refusing to write on an error), the validator, `CONTENT_SHEETS.md`; `Upgrade_Portal04.asset` in the library's upgrades. Gates; commit.
  6. **The day's schedule.** `WorldState.portalSettings` (additive), `TodaysWorld.Portals`, `ContentLibrarySO.BuildToday(plan, world)` resolving it (the owned upgrades, the day, `plan.ClaimAllowed` as `closed`, the day's world as `inWorld`); every caller (`GameManager`, `BalanceSimulation`, `TimelineService`'s tomorrow-world read if it builds a day) passes the world. Gates; commit.
  7. **The generator.** `CaseFactory`: the kind draw moved before the destination; for a citizen, `PickEraFromPlan` weighs each era by `PortalDraw.EraWeight(weight, routes in the era)` and `PickPlace` draws among the era's routes; for the displaced, both as today; a planned slot of a citizen claims a route. The rules are tasks 2-3's; PR5's generation sweep checks the glue. Gates; commit.
  8. **The Rules memo and the notices.** The memo's "Portal schedule" table (TC-940's spec gains the section; rows linked like a closure's place; day-layer entries `rule:portal:{n}`), the morning notices from `PortalDay` (`news.portals`, `news.portalRetuned`, `news.portalInService`). Gates; commit.
  9. **Docs.** FEATURES (World: the portals, their unlocks, the Directorate's routes, the resolution, a citizen's destination drawn over the routes, the kind first, the Return Gate; Investigation loop: the memo's schedule; Run & meta: `portalSettings`). Commit.
- **Unity** (only in an open editor; else PR5): compile and the EditMode suite.
- **Golden effect** (re-packed in PR5): `cases.txt`, every day (the kind first; citizens' destinations are routes); the play transcript (destinations, notices); `data_hashes` (the day plans, the library, `Upgrade_Portal04`).
- **FEATURES:** World, Investigation loop (the memo), Run & meta (the save).
- **Split seam:** tasks 1-4 (Domain) before 5-9.
- **Depends on:** PR1 (the commission is an Orders item; `UpgradeBranch.Portals`).

### Phase PR3 — The Portals app (M)

- **Specs:** S PA1-PA6, RT2, RT5, RT6, RT8, §4, §9.1 (`Choices`, `PortalSettings`).
- **Scope:**
  - Domain: `PortalSchedule.Choices` (every place of tomorrow's world, enabled, or disabled with its reason: closed tomorrow, on another portal tomorrow, booked); `PortalSettings` (`Set`, `Clear`, `Read`) over `WorldState.portalSettings`.
  - The desktop: `DesktopAppIds.Portals` (`portals`) in `DefaultOrder` after `Investigation` (eight icons); its placeholder glyph; `icon.portals` (Flavour, every culture table), `window.portals`, `app.portals.*`.
  - The Portals window: the TC-970 `FormSpecSO` (today, read-only; tomorrow's notices), the themed Tomorrow panel (per departure portal in service tomorrow, one arriving tomorrow included: "Directorate route", or Era then Location), "Keep today's routes", "Use the Directorate's schedule"; tomorrow's preview is `PortalSchedule.Resolve` over tomorrow's plan, the current settings and the upgrades owned or in transit; changes save at once.
- **Files:** Domain `Portals/PortalSchedule.cs`, new `Portals/PortalSettings.cs`, `DesktopAppIds.cs`; Visuals `DesktopIconPlaceholder.cs`; Scripts new `UI/Apps/PortalsWindow.cs`, `UI/Apps/DesktopApps.cs`, `Office/DesktopConfigSO.cs`; Editor `OfficeSceneUIBuilder*.cs`; content `world_source.json` (`ui.strings`); tests `PortalScheduleTests.cs`, new `PortalSettingsTests.cs`, `DesktopLayoutTests.cs`, `DesktopIconPlaceholderTests.cs`; docs `docs/FEATURES.md` (Fake-OS desktop: eight icons, the Portals app), P DK1, `docs/ART_ASSET_LIST.md` (the Portals icon).
- **Tasks:**
  1. **Choices.** Tests first, `PortalScheduleTests`: `Choices_ListEveryPlaceOfTomorrowsWorld`, `Choices_DisableAPlaceClosedTomorrow`, `Choices_DisableAPlaceOnAnotherPortalTomorrow`, `Choices_KeepTheChoosingPortalsOwnRouteEnabled`, `Choices_NeverOfferThePresent`. Then `Choices`. Gates; commit.
  2. **The clerk's routes.** Tests first, `PortalSettingsTests`: `Set_AddsARoute`, `Set_ReplacesThePortalsRoute`, `Clear_HandsThePortalBack`, `Read_ByPortal`, `Read_SkipsAnUnparseableEntry`. Then `PortalSettings`. Gates; commit.
  3. **The icon.** Tests first: `DesktopLayoutTests` (eight ids; the arrange grid's second column; a saved layout gains `portals` in the first free spot), `DesktopIconPlaceholderTests` (`portals` has a glyph). Then the id, the order, the glyph, the strings. Gates; commit.
  4. **The window.** The TC-970 spec, `PortalsWindow` (the form from today's `PortalDay`; the Tomorrow panel from `Choices` and the preview; `PortalSettings` on change; the two buttons), the builder (the dropdown templates reuse the input field's role), `DesktopConfigSO`'s size. Gates; commit.
  5. **Docs.** FEATURES, P DK1, the art list. Commit.
- **Unity** (only in an open editor; else PR5): compile and the EditMode suite.
- **Golden effect** (re-packed in PR5): `scene_OfficeGameplay`; the play transcript only if the job sets a route (it does not).
- **FEATURES:** Fake-OS desktop.
- **Depends on:** PR2.

### Phase PR4 — The routing directive and the rerouted booking (L)

- **Specs:** S RC1-RC9, §6, §7.3, §9.1 (the grown Directives, `RecordLies`, `CitizenAccount.BookedDeparture`), §10 (day 2's rules), §13.1.
- **Scope:**
  - `TravelRuleType.PortalRouting` (appended), `DirectiveFault.NoOpenPortal` (appended), `Faults.Unserved` and `citation.acceptedWrong.unserved`; `CaseFacts.UnservedDestination`; `Directives.FaultOf`, `HasMaker`, `IsRolled`, `CanBreak` and `RuleProblems` for it; `PortalSchedule.DayProblems`' first-day check.
  - The makers: `PlanViolators` treats the routing rule like a closure (its first-day breaker's place on the violator stream among `PortalSchedule.Unserved`), its slot's kind among the rule's kinds; the rolled swap (on the fault stream, a route-less open place of the destination's era) before the papers print; `CaseFactory.Facts` sets `UnservedDestination` from the day's `PortalDay`.
  - L11: `LieKind.Rerouted` (appended), `LieKinds` (a record lie, citizens only), `RecordLies` (its variant: the booked place from `RecordLieContext.UnservedInEra` on the lie stream, the tells every Destination field), `CitizenAccount.BookedDeparture`, `AccountRecords.Record` prints it.
  - L5's worksite: `RecordLieContext.OpenPlaces` becomes the day's other routes.
  - Content: `Rule_PortalRouting` (kinds, line), listed from day 2 after the closures; day 2 drops `Rule_NoAncientEgypt`; `Rerouted` in the `lies` of day 6; the citation; `steps.rules` reads "Today's rules and the Departure Board for the destination".
- **Files:**
  - Domain: `TravelRuleType.cs`, `Faults.cs`, `Directives.cs`, `LieKind.cs` (`LieKind`, `LieKinds`), `RecordLies.cs`, `AccountMaker.cs` (`CitizenAccount.BookedDeparture`, `AccountRecords.Record`), `Portals/PortalSchedule.cs` (`DayProblems`).
  - Scripts: `CaseFactory.cs` (`PlanViolators`, `PlanViolation`, `Facts`, the L11 path in `Forge`, the L5 context), `Investigation/TravelRuleSO.cs` (doc comment).
  - Editor: `WorldContentGenerator.cs` (the rule type through `ParseEnum`; the day checks), `ContentLibraryValidator.cs`.
  - Content: `world_source.json` (`rules[]`, day 2's rules, days 2-6's rule lists, day 6's lies, `ui.strings`); `ContentSheetMap.cs` (the `lies` note).
  - Tests: `DirectivesTests.cs`, `FaultsTests.cs`, `RecordLiesTests.cs`, `LieKindsTests.cs`, `AccountMakerTests.cs` (the record's rows), `SerializedEnumsTests.cs`, `PortalScheduleTests.cs`, `LiesTests.cs` (the kind pick's goldens on days that enable Rerouted), `ContentSheetMapTests.cs`.
  - Docs: `docs/FEATURES.md` (Investigation loop: the routing directive, the closures from day 3, L11, L5's worksite, the citation; Shift clock & queue: day 2's guarantees), T §2.3 (day 2's closure), §5.2, §5.3 and §6.1 rows with a pointer to S.
- **Tasks:**
  1. **The rule and its fault.** Tests first: `SerializedEnumsTests.TravelRuleType_KeepsItsSerializedInts` (PortalRouting after the last member on the epic's `main`); `DirectivesTests`: `PortalRouting_BreaksOnAnUnservedDestination`, `PortalRouting_HoldsOnAServedDestination`, `PortalRouting_IsNotReadForTheDisplaced` (through the rule's kinds), `FaultOf_PortalRoutingIsNoOpenPortal`, `HasMaker_IncludesPortalRouting`, `IsRolled_IncludesPortalRouting`, `CanBreak_PortalRoutingForCitizensOnly`, `Fault_AClosedAndUnservedDestinationReadsClosedFirst`, `RuleProblems_PortalRoutingNeedsKinds`, `RuleProblems_PortalRoutingRefusesTheDisplaced`; `FaultsTests.Reason_NoOpenPortalIsUnserved`. Then the enum members, `CaseFacts.UnservedDestination`, the `Directives` rows, `Faults.Unserved`. Gates; commit.
  2. **The first-day check.** Tests first, `PortalScheduleTests`: `DayProblems_RoutingsFirstDayNeedsAnUnservedOpenPlace`, `DayProblems_NoWarningWhenOneExists`. Then the check, read by the generator and the validator. Gates; commit.
  3. **The makers.** `PlanViolators` (the routing rule's breakers: `PortalSchedule.Unserved` of the day's world and schedule; the slot's place on the violator stream; the slot's kind among the rule's kinds, never an honest entry), `PlanViolation` (the rolled routing: breakable only when `Unserved(…, era)` is not empty; the swap's one Range draw on the fault stream; `place`, `claimedNation`, `originLabel` updated before the papers print), `Facts` (`UnservedDestination`). The rules are task 1's and `Unserved`'s; PR5's generation sweep checks the glue. Gates; commit.
  4. **L11.** Tests first: `SerializedEnumsTests.LieKind_KeepsItsSerializedInts` (Rerouted after ForgedProof, or after `main`'s last); `LieKindsTests`: `Rerouted_IsARecordLie`, `AppliesTo_ReroutedCitizensOnly`; `RecordLiesTests`: `Plan_ReroutedBooksAnUnservedPlaceOfTheClaimsEra`, `Plan_ReroutedDrawsOnceForTheBooking`, `Plan_ReroutedIsNoPossibleLieWithoutAnUnservedPlaceAndDrawsNothing`, `Plan_ReroutedTellsEveryDestinationField`, `Plan_ReroutedRewritesNoPaperValue`; `AccountMakerTests`: `Record_TheBookedDepartureRowPrintsTheBookingWhenSet`, `Record_TheBookedDepartureRowPrintsTheClaimOtherwise`. Then the member, `LieKinds`, the `RecordLies` variant and `RecordLieContext.UnservedInEra`, `CitizenAccount.BookedDeparture`, `AccountRecords.Record`, `CaseFactory.Forge`'s context. Gates; commit.
  5. **L5 among the routes.** Tests first, `RecordLiesTests`: `Plan_ForgedContractsWorksiteIsAnotherRoute`, `Plan_ForgedContractsWorksiteVariantCannotShowWithoutAnotherRoute`. Then `CaseFactory` fills `OpenPlaces` from the day's other routes. Gates; commit.
  6. **Content.** `world_source.json` by script: `Rule_PortalRouting`; days 2-6 list it after their closures; day 2 drops `Rule_NoAncientEgypt`; day 6's `lies` gain `Rerouted`; the citation and `steps.rules`; the sheet's notes. `LiesTests`' goldens rewritten where Rerouted is enabled (explain in the commit). Gates; commit.
  7. **Docs.** FEATURES, T's rows. Commit.
- **Unity** (only in an open editor; else PR5): compile and the EditMode suite.
- **Golden effect** (re-packed in PR5): `cases.txt` days 2-6 (again: the breakers, day 2's closure, L11); `data_hashes` (`Rule_PortalRouting`, the day plans).
- **FEATURES:** Investigation loop, Shift clock & queue.
- **Split seam:** tasks 1-3 (the directive) before 4-5 (the lies).
- **Depends on:** PR2.

### Phase PR5 — The hall, and the one Unity session (M)

- **Specs:** S BD1-BD6, §5, §12, §13, §14 (Office scene).
- **Scope:**
  - `OfficeAnchorId.DepartureBoard` (appended: 23) and its fallback `16 Departure board blank display` in the contract asset (`Assets/Data/Config/OfficeSceneContract.asset`, `OfficeSceneContractSO`; ensured by `OfficeSceneContractTools`); the binder places the board's text and click box at its bounds and follows them.
  - Visuals `PortalBoardText` (`Rows`, `Tooltip`).
  - The builder: `BoardRows` (world-space TMP on the `Gameplay` sorting layer, auto-sized, `hallBoardInk`), the click box on `Interactable` with the reacting-prop tooltip (the tooltip template grows with its lines).
  - The `rules` step counts the board's tooltip shown while a traveller is at the desk.
  - The rings (Q12 A, only with the art side's OK recorded): `AnimeHallPortalLink` (the idle tint at the day's start, the pulse on an accept, reduced motion), the `DeskConfigSO` knobs (`hallBoardInk`, `hallBoardInset`, `hallPortalGlassLayers`, `hallPortalIdleTint`, `hallPortalPulseTint`, `hallPortalPulseSeconds`). Without the OK: the knobs for the board only, and no link.
  - Docs: `docs/SCENE_CONTRACT_GAMEPLAY.md` (S §5.3), `docs/ART_ASSET_LIST.md` (S §12.1's section 1 rows, the totals, the retired shop panel).
  - **The epic's Unity session** (below).
- **Files:** Domain `OfficeContract.cs`; Visuals new `PortalBoardText.cs`; Scripts `Office/OfficeSceneBinder.cs`, new `Office/DepartureBoardView.cs`, new `Office/AnimeHallPortalLink.cs`, `Office/DeskConfigSO.cs`, the steps' event source; Scripts `Office/OfficeSceneContractSO.cs` (the anchor's entry); Editor `OfficeSceneUIBuilder.Desk.cs` (or the office part), `OfficeSceneContractTools.cs` (the contract asset's fallback); tests `SerializedEnumsTests.cs`, new `PortalBoardTextTests.cs`; docs as above and `docs/FEATURES.md` (Office scene).
- **Tasks:**
  1. **The anchor.** Tests first, `SerializedEnumsTests.OfficeAnchorId_KeepsItsSerializedInts` (DepartureBoard = 23, the count 24). Then the member and the contract asset's fallback (no default pose). Gates; commit.
  2. **The board's text.** Tests first, `PortalBoardTextTests`: `Rows_TheDeparturePortalsInServiceInNumberOrder`, `Rows_UpperCaseLabelsWithTwoDigitNumbers`, `Rows_NoReturnGateAndNoIdlePortal`, `Rows_EmptyWithoutARoute`, `Tooltip_ListsEveryPortalWithItsState`. Then `PortalBoardText`. Gates; commit.
  3. **The board in the hall.** The builder's `BoardRows` and click box, `DepartureBoardView` (fit to the bounds inset, `LateUpdate` re-fit, text from the day's `PortalDay`), the tooltip, the binder's placement, the `rules` step's source. Gates; commit.
  4. **The rings** (only with the art side's OK). `AnimeHallPortalLink`, its knobs, the accept hook (the portal from `PortalDay.PortalFor`, the Return Gate for the displaced). Gates; commit.
  5. **Docs.** The scene contract, the art list, FEATURES (Office scene). Commit.
  6. **The Unity session** (with Saleh's OK; one session, jobs queued one at a time):
     - Merge the latest `main` into the epic; offline gates.
     - Generate World; Tools > TimeDesk > Write Content Spreadsheet Template; the validator (0 errors; the warnings read and explained).
     - Build Office UI and Build Home: commit `OfficeGameplay.unity` and `HomeScene.unity` (compared by semantic dump).
     - The player scripts compile; the EditMode suite passes except the known third-party failure.
     - A generation sweep (80 seeds × days 1-6, a `_TimeDesk` job): every citizen's destination is a route unless they are a closure's violator or a routing breaker; one fault per traveller; every first-day guarantee planned (day 2's routing breaker); every L11 liar's booking unserved and in the claim's era; the displaced's origins over the world.
     - The smoke play in the hall (Title → New Run, seed 12345 → day 1, one traveller decided, no errors in the log).
     - The feature probes, screenshots at 1920 × 1080 and 1280 × 720 into `SCRATCH/portals/`:
       1. Day 1: the board shows `01  PERICLEAN ATHENS (ANCIENT)` and `02  REPUBLICAN ROME (ANCIENT)`; the tooltip lists all five portals; the morning notice lists the routes; every citizen's destination is on the board.
       2. The Portals app on day 1: set 01 to another place for tomorrow; a place closed tomorrow is disabled with its reason; the next day the board shows the new route and the app's Today section says "Clerk".
       3. Orders: order the Auto-Feed Scanner and cancel it (the wallet and the statement go back); order it again; the next day the delivery memo, the scanner's tray, the Orders row "Delivered day 2"; the Home scene has no shop (Expenses → Slot → Sleep).
       4. Day 2: the routing breaker (a destination off the board) denied without evidence is correct; accepted in a second run, the citation reads "Approved a departure no portal serves."
       5. Day 3: a clerk route on a closed place runs the Directorate's route; the notice says so; the closure's violator is off the board.
       6. Day 6 (L11 forced from the debug panel if the seed has none): the visa's Destination against the record's Booked departure logs a deviation; denied, correct.
       7. The Portal 04 commission ordered on day 3: on day 4 its ring is lit and the board has three rows.
       8. With the rings' OK: the departure pulse on an accept (and one step with reduced motion); a ring not in service dimmed.
       9. The hall's full evening: the board's rows still read (contrast measured on screen, the linear-colour note in the common brief).
       10. Continue from a Home save keeps a pending order and the clerk's routes; the night delivers the order.
     - The golden re-pack (`golden.py pack … --out docs/reviews/audit-baseline/golden`), with the diff explained in the commit (S §13.2); the audit play job updated to order at the PC.
     - Revert what Unity touched outside the phase; delete the `_TimeDesk*` files.
- **Golden effect:** the epic's, S §13.2, re-packed once here.
- **FEATURES:** Office scene.
- **Depends on:** PR1-PR4.

### Phase PR6 — Days 7-15 bookings and the balance hooks (M)

- **Specs:** S RT7, GN5, CN2-CN3 (`portalBooked`), §10 (days 7-15), §11.2 (BX1-BX4), §17 (D's row). D B2, §3, §5 (`desk4_closes`), X5.
- **Scope:**
  - Bookings (Q3 A, and only if D's Q1 is B or C): `PortalSchedule.Resolve`'s bookings pass; `Premades.Bookings(plan, dayStart)` (the day's standing forced citizen premades' places, through D's `Premades.SlotSource` with its conditions); `BuildToday` passes them; the Portals app's tomorrow preview shows them; the notice `news.portalBooked`; the validator: a booked place in the day's world and open (with D's V11).
  - Content: days 7-15's `portals` (S §10), `Rerouted` in every day 7-15's `lies`, `desk4_closes`' line ("From today its queue and its portal (05, upper right) join Desk 3."), the `ContentSheetMap` news field.
  - The balance hooks in `BalanceSimulation` (BX1-BX4): the routes per day and each route's share of the accepted; per run the places history moved, the leader's first day and its dominance; the contamination count per place; the thresholds re-proposed. BX5 stays for later.
- **Files:** Domain `Portals/PortalSchedule.cs`, `Premades.cs`, `ContentSheets/ContentSheetMap.cs`; Scripts `ContentLibrarySO.cs`, `UI/PortalsWindow.cs`, `GameManager.cs` (the notice); Editor `WorldContentGenerator.cs`, `ContentLibraryValidator.cs`, `BalanceSimulation.cs`; content `world_source.json`; tests `PortalScheduleTests.cs`, `PremadesTests.cs`, `ContentSheetMapTests.cs`; docs `docs/FEATURES.md` (World: bookings; Content & tooling: the simulation's report), D's §15 seam row.
- **Tasks:**
  1. **Bookings in the resolution.** Tests first, `PortalScheduleTests`: `Resolve_ABookingKeepsAClerkRouteThatServesIt`, `Resolve_ABookingTakesAPortalWhoseClerkRouteCannotRunFirst`, `Resolve_ABookingTakesTheHighestDeparturePortalOtherwise`, `Resolve_ABookedPortalNotesBooked`, `Choices_DisableAPlaceBookedTomorrow`. Then the pass. Gates; commit.
  2. **The day's bookings.** Tests first, `PremadesTests`: `Bookings_ACitizenPremadeStandingTodayIsBooked`, `Bookings_AFamousPremadeIsNeverBooked`, `Bookings_AFailedConditionBooksNothing`, `Bookings_AMetOncePerRunPremadeBooksNothing`. Then `Premades.Bookings`, `BuildToday`, the app's preview, the notice. Gates; commit.
  3. **Content.** Days 7-15's routes, lies and `desk4_closes` by script; the sheet; the validator's booking check. Gates; commit.
  4. **The simulation's report.** BX1-BX4 in the summary (a pure summary helper in Domain if the numbers need rules; its tests first). Gates; commit.
  5. **Docs.** FEATURES, D's seam row. Commit.
- **Unity** (with Saleh's OK; batched with D5's session if both are pending): Generate World; the static dump to day 15; the 50-run simulation over days 1-15 (its summary into `SCRATCH/portals/`); a probe of day 7's booking (Pell's Periclean Athens on a portal when the clerk's routes lack it; the app's preview the night before).
- **Golden effect:** `cases.txt` days 7-15; the balance summary.
- **FEATURES:** World, Content & tooling.
- **Depends on:** the epic on `main`; D's D2 and D4 on `main` (tasks 1-3); phase 23 part 2 on `main` (task 4).

## 3. Why this order

- **Orders first** because it changes no traveller, retires the Home shop cleanly, and gives the Portal 04 commission (PR2) a place to be sold, so no interim state sells a portal at Home.
- **Routes before the app and the directive** because both read the day's `PortalDay`: the app previews tomorrow's, the directive reads today's. With PR2 alone every citizen is served, so the epic is coherent at every step even before the directive exists.
- **The app and the directive in either order** after PR2: the app writes `portalSettings`, the directive reads the resolved schedule; neither needs the other.
- **The hall last** because it only shows what PR2-PR4 decide, and because it carries the epic's single Unity session: Generate World, both builders, the sweep, the probes and the golden re-pack happen once, over the finished epic.
- **D's bookings after D** because they need D's citizen premades; until then no premade needs a route (the famous use the Return Gate), so building bookings earlier would be code no content reaches.
