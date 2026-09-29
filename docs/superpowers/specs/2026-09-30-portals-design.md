# Portals, the Departure Board and Orders: design

*2026-09-30 · drafted by Claude from Saleh's request of 2026-09-29 and his design rules of the same day · read-only map of `main` at `cdf10f7` (`E:\unity\NOPE-docs`, branch `design/portals`) and of `origin/design/days-7-15` at `24e7a8e` (the days 7-15 spec, **D** below, not on `main` yet) · the code wins over this text · the traveller-types spec (`2026-09-26-traveller-types-design.md`, **T**) owns the kinds, papers, lies, directives and verdicts; the PC redesign spec (`2026-09-26-pc-redesign-design.md`, **P**) owns the desktop, the windows and the forms; this spec owns the portals, the Departure Board, the routing directive and its lie, and the Orders app; §17 is the seam · built by `docs/superpowers/plans/2026-09-30-portals-plan.md` · every decision is recorded with its reason; §15's questions wait for Saleh, and the plan builds the recommended options*

Saleh, verbatim (2026-09-29):

> "there is now multiple portals and a destination board. new portals will unlock. we will have the upgrade tree be in the pc now as another icon: you can order the upgrades and they arrive next day. destination must match portal in the papers and destination board should also show it. you will have an icon that shows the portals and the destination. this icon will set the portal to an era and a location. lets try that"

**The art.** The anime terminal hall (`Assets/Art/Office/AnimeHallLayers/AnimeHall.unity`, art `c75e1fe`, the live art office) already draws five portal rings and a departure board. Each ring is three of the `AnimeHallPresentation`'s registered layers (a secure bay, a metal ring, a painted glass): `Portal 01 front` (the large ring in the pit behind the traveller), `Portal 02 rear left` and `Portal 03 rear right` (on the concourse floor), `Portal 04 upper left` and `Portal 05 upper right` (on the gallery). The board is `15 Departure board frame` with `16 Departure board blank display`: about 313 × 107 px of the 1080p view, hanging just under the claim strip (`SCRATCH/t5/hall/play_smoke4/hall_opening_1080.png`). Nothing uses either today.

**Saleh's design rules (2026-09-29), which override any spec text that disagrees:**
1. One penalty for any wrong decision, approval or rejection alike, after one free warning a day (`GameConfigSO.freeWarningsPerDay`).
2. Personalities, not traveller types, drive claim lines, reactions and small talk.
3. The same interactions for every traveller type.
4. Balance knobs are editable in the Unity editor (ScriptableObjects that Generate World does not overwrite, or the content spreadsheet), and authored cases stay outside the random draws.
5. A premade keeps its extra stability cost; stability is a two-decimal value; days 7-15 are authored.

## 0. The map: what exists and what this spec does with it

| What (where) | Today (`main` `cdf10f7`) | This spec |
|---|---|---|
| Destinations (`ContentLibrarySO.TodaysProfiles`; `CaseFactory.PickEraFromPlan`, then the kind draw, then `PickPlace`) | every place of the day's world (the plan's eras, weighted, × its countries; never the Future) | a 2150 citizen's destination is one of the day's **routes**, the places the open portals serve (GN2); the displaced keep the whole world through the Return Gate (PO2) |
| Closures (`TravelRuleSO` Era/Nation/NationEraForbidden, `DayPlanSO.ClaimAllowed`, one planned violator each in `PlanViolators`) | directive faults read on the Directives, from day 2 | unchanged as rules; a portal set to a closed place runs the Directorate's route that day (RT4), so a closed place is never on the board; day 2's closure moves out (RC6) |
| Directives (`Directives`, `CaseFacts`, `DirectiveFault`, `Faults`) | closures, the paper set, the debt standing, the papers' dates; dress, return home, no 2150 goods, the procedure lines | + `PortalRouting`: a citizen's destination must be a route of an open portal (RC1) |
| Record lies (`RecordLies`: L1-L5, L10) | L5's worksite variant swaps in another open place | + **L11 `Rerouted`** (RC7); L5's swap names a route (RC8) |
| The Destination field | TC-101 row 4, TC-415 and TC-417 row 3, TC-520 row 4 (Worksite), TC-610 row 4 (Origin), TC-630 row 3 (Return To); compared with the record's Booked departure or the registry's Origin | unchanged fields; read against the board (RC1) and still compared with the record (RC7) |
| The Home shop (`HomeManager.ShowShop`, `HandleBuyUpgrade`; `HomeUIController`'s shop panel and its `Paging`; `HomeSceneBuilder`'s `ShopPanel`) | eight upgrades: Auto-Feed Scanner 200 cr, Analysis Scanner 300, Diplomatic Contacts 350, Interview Protocols 120, four Speech translators 80; bought at night, in force from the next office day (the day-start `GateSnapshot`) | moves to the PC's **Orders** app: ordered during the shift, delivered at the start of the next one, so in force from the same office day as before (OR1-OR10); Home keeps expenses, care, the slot machine and sleep |
| Upgrades (`UpgradeSO`: id, name, blurb, cost, unlock effect; `WorldState.unlockedUpgradeIds`; `ScannerDay` and `TranslationDay` read the day-start snapshot) | a flat list | + `branch` (the tree), + the Portal 04 commission (OR6) |
| The desktop (`DesktopAppIds`; P DK1: "Six icons, nothing else, ever") | Investigation, Internet, Mail, Citizen Account, Notes, Settings | **eight**: + Portals and Orders (PA1, OR2). Saleh's request overrides DK1's count; "no icon is added at run time" still holds |
| The hall (`SCENE_CONTRACT_GAMEPLAY.md`; `OfficeAnchorId`, 23 values; `AnimeHallShiftLink` drives the art's `SetTime`) | the rings and the board are art only | anchor `DepartureBoard` (BD1); the board's rows drawn by the gameplay layer (BD2-BD4); each ring's glass tinted by its state and pulsed on a departure through the art's own `SetLayerTint` (BD5, with the art side's OK) |
| Mail (`Mailbox`, `MailKind`: 4 kinds) | the directive memo, the Times, citations, authored mail | + `Delivery` (OR5) |
| Saves (`WorldState`, save version 3) | | + `portalSettings`, `orders`, both additive: the version stays 3 |

## 1. Decisions

### 1.1 The portals (PO)

| Id | Decision | Why |
|---|---|---|
| PO1 | **Five portals, one per ring of the art, numbered as the art numbers them**: 01 front, 02 rear left, 03 rear right, 04 upper left, 05 upper right. Content: `agency.portals[]` = `{number, name, role, fromDay, upgrade}` (§9.3). | The hall already shows five rings. Numbering them by the art's own layer names keeps the board, the app and the rings one list. |
| PO2 | **Two roles.** *Departures* (01, 02, 04, 05): the clerk sets each to an era and a place. *Returns* (03, the **Return Gate**): the displaced go home through it, "tuned by their Return Order's incident"; it is never set and never on the board. | A displaced person's destination is their origin, anywhere in the day's world (T K2). Routing them through the clerk's routes would deny people their way home for the clerk's configuration, and would force every famous premade's authored place onto a route. The Return Order already carries the incident the gate tunes to, so the fiction costs no field. Q3. |
| PO3 | **Unlocks, by day and by order ("both").** 01 and 02 from day 1; 03 from day 5 (the displaced arrive, T K3); 04 by order (the Portal 04 commission, 300 cr, OR6), in service from the day after it is ordered; 05 from day 9 (Desk 4's portal joins Desk 3 with its queue, D §5's `desk4_closes`). A portal row may carry both `fromDay` and `upgrade`: it is then in service from whichever comes first, so Saleh can make any portal "both" in the sheet. | Two routes keep day 1 readable. The Return Gate opens with the people it serves. One portal gives the Orders app a purchase the old shop never had. The fifth arrives with the story's one jump in the queue (D A3). Q2. |
| PO4 | **A departure portal in service always runs a route**: the clerk's, else the Directorate's (RT3). There is no "off". | An idle portal would only shrink the day's routes, and a day with no open route would leave honest citizens nowhere to go. |
| PO5 | **What another portal buys**: one more destination a day, and more say over where accepted travellers land, which is where history moves (impacts, carries and influence land on the destination, T H3, and the leading country becomes 2150's culture). The number of portals never changes the queue size, the pay or how many travellers are faulty (RC4). | The queue and the faults are authored per day. Tying them to the portal count would make a portal either a trap or a must-buy. The history lever is the game's own "your choices change history". |
| PO6 | **Not in service**: before its first day, or before its commission is delivered, a portal's ring is dimmed (BD5), and the app says how it comes into service ("Order its commission in Orders" / "In service from day 9"). | "new portals will unlock": the player can see what is coming. |

### 1.2 Routes: the schedule (RT)

| Id | Decision | Why |
|---|---|---|
| RT1 | **A route is a place** (a country × an era) of the day's world, never the present or the Future, keyed `country:era` (`PlaceKey`), the pair `rules[]` already names places by. | "set the portal to an era and a location": an era and a country name exactly one of the 40 places. |
| RT2 | **The day's schedule is fixed at the day's start** (`PortalDay`, built with today's world in `ContentLibrarySO.BuildToday`, the pattern of `ScannerDay` and `TranslationDay`) and holds all day: the board, the tooltip, the Rules memo, the generator, the verdicts and the rings read that one object. Changes made in the Portals app are **tomorrow's**. What happens to travellers already queued: nothing, ever. | The whole queue is generated at the day's start (`CaseFactory.GenerateDayCases`). A route changed mid-shift would turn an honest traveller already in the queue into a directive fault, and a clerk could close every route and deny the whole queue without evidence. Q1. |
| RT3 | **Resolution** (Domain `PortalSchedule.Resolve`, pure, no draw), over the departure portals in service, in two passes. Pass 1: each Directorate booking (RT7) and each clerk route that is in the day's world, open that day (no closure forbids it) and not already claimed by a lower-numbered portal. Pass 2: every portal still without a route takes the day plan's first **Directorate route** (`days[].portals`, in order) that no portal serves yet. A clerk route that could not run records why (closed, outside the day's world, taken, booked). | The clerk's choice wins whenever the Directorate can run it, and a fallback never displaces a valid clerk route (hence two passes). The fallback is authored content, never a draw: a player who never opens the app gets the Directorate's schedule every day. |
| RT4 | **Closures retune.** A portal whose clerk route is closed that day runs a Directorate route instead; the app shows it the night before ("Closed tomorrow: the Directorate runs its route"), and the morning's notices repeat it. A closed place is therefore never on the board. | An embargoed route left on the board would either carry no honest traveller (an idle portal) or a queue of violators, and the second is an exploit: set every route to tomorrow's closures and deny everyone without evidence. Q7 offers the embargo on the board instead. |
| RT5 | **The clerk's routes persist**: a route stays until the clerk changes it or hands the portal back ("Directorate route"). On a day it cannot run, the Directorate's runs, and the clerk's resumes the next day it can. Saved in `WorldState.portalSettings`. | A route is a standing instruction; re-entering four routes a day would be busywork. |
| RT6 | **Free and unlimited** changes, until the shift ends (the PC closes with the shift; the app says so). | The choice already costs what matters: a portal serves one place a day. The economy is phase 23's, and a fee would punish trying things, which is what Saleh asked for ("lets try that"). Q8 offers a fee (a knob) instead. |
| RT7 | **Directorate bookings** (built with D, plan phase PR6): on a day a forced **citizen** premade stands (D B2), its place must be a route. If no valid clerk route serves it, the booking takes a portal for that day: first one whose clerk route cannot run, else the highest-numbered departure portal. The app shows "Directorate booking tomorrow: Periclean Athens" on that row; the clerk's route resumes the day after. The Directorate's routes list each day's booked place first, so a clerk who never sets a route never meets a booking. | Authored cases stay outside the random draws (rule 4) and keep their authored right calls: Ines is accepted and Callum denied for his frozen account, never for a route the clerk happened not to run. |
| RT8 | **Two portals never serve one place** (RT3's "not already claimed"; the app disables the choice). | A second portal on one place adds nothing and would let a clerk halve their routes. |

### 1.3 The Portals app (PA)

| Id | Decision | Why |
|---|---|---|
| PA1 | **A desktop icon, Portals** (`portals`), and its window: the form TC-970 "Portal Schedule" (today, read-only) beside a themed **Tomorrow** panel with two choosers per departure portal, Era then Location. The default icon order becomes Investigation, Portals, Internet, Mail, Citizen Account, Orders, Notes, Settings (`DesktopAppIds.DefaultOrder`); a saved layout gains the two new icons in the first free spots (P DK4). | "you will have an icon that shows the portals and the destination. this icon will set the portal to an era and a location." The work apps come first; Orders sits beside the account it spends from. |
| PA2 | **What it shows** (§4): section 1, today: each portal's number, ring, route (or Return Gate, or not in service) and who set it (Clerk; Directorate, with why; Booking). Section 2, tomorrow's notices: tomorrow's closures and its busy eras (era weights above 1, "Industrial: busy ×2"). The Tomorrow panel: each departure portal in service tomorrow, one arriving tomorrow included. | The clerk needs tomorrow's closures and demand to choose well, and both are authored in tomorrow's plan. |
| PA3 | **The choosers.** Era lists tomorrow's eras that have a choosable place; Location lists the countries with a place in that era tomorrow, each shown as "Greece: Periclean Athens". A place closed tomorrow, on another portal tomorrow, or booked, is listed disabled with its reason. Each row also offers "Directorate route". | The choice is made in Saleh's words (era, location), but the result is always shown as the place the papers print. |
| PA4 | **Changes save at once** into `WorldState.portalSettings` (the end-of-shift save keeps them). Two buttons: "Keep today's routes" and "Use the Directorate's schedule". The footer: "Routes take effect at the start of tomorrow's shift. Set them before the shift ends." | No confirm step is needed: nothing takes effect until tomorrow, and every change can be changed back. |
| PA5 | The window opens restored at 1040 × 800 u (a `DesktopConfigSO` knob) and can be maximised. The form scrolls (P AP7). The choosers are reached with Tab and work with the keyboard (P KB4). | The PC's window and accessibility rules. |
| PA6 | Themed chrome, an unthemed form (P TH1). The choosers reuse the input field's theme role, so no role is appended. | No churn in the serialized `ThemeRole` enum. |

### 1.4 The Departure Board (BD)

| Id | Decision | Why |
|---|---|---|
| BD1 | **New anchor `DepartureBoard`** (`OfficeAnchorId`, appended as 23). Fallback: the bare name `16 Departure board blank display` (the hall's sprite; its bounds are the place). The 3D room has no board: the anchor is missing there, the board is skipped, and the PC shows the schedule. | The contract's own pattern (a bare-name fallback whose renderer bounds are the place). The art scene is not edited. |
| BD2 | **The rows are drawn by the gameplay layer**: a world-space TextMeshPro built by Build Office UI in `OfficeGameplay` (sorting layer `Gameplay`), fitted each frame to the anchor's bounds inset by `DeskConfigSO.hallBoardInset`, so it follows the display if the art pans. Ink: `hallBoardInk` (FFF2D9, the ivory the hall's other glasses use) on the art's dark display. | The gameplay owns the text and its layout. The contract places gameplay objects by world position and never parents them into the art. The readouts' texts live in the art scene only because the art's preview texts had to be replaced there; the board has none. |
| BD3 | **What it shows**: one row per departure portal in service, in number order, "01  PERICLEAN ATHENS (ANCIENT)": the papers' own label, upper case. No Return Gate row, no "not in service" rows, no header. | The board answers the one question the desk asks of it (is this destination served today?) with the largest text the display allows: 2 to 4 rows are about 27 to 53 px tall at 1080p and 18 to 35 px at 720p. |
| BD4 | **The board is a reacting prop**: a click box on `Interactable` over it (the hover outline), whose tooltip lists the whole schedule in UI text (every portal: its route, the Return Gate, not in service). The tooltip grows to fit its lines. Showing it while a traveller is at the desk counts as viewing the rules for the `rules` step (P ST3). | A long label ("Ottoman Iraq (Baghdad Vilayet) (Industrial)") shrinks on the board below the 720p floor. The tooltip is the readable surface in the office; the PC is the full one. |
| BD5 | **The rings** (with the art side's OK, Q12): an `AnimeHallPortalLink` (added by the binder beside `AnimeHallShiftLink`) tints each ring's painted-glass layer through the art's own `AnimeHallPresentation.SetLayerTint`: the art's colour when in service, `hallPortalIdleTint` (a 45 % grey) when not, and a pulse (`hallPortalPulseTint` for `hallPortalPulseSeconds`, 0.8 s; one step with reduced motion) on the ring an accepted traveller leaves through: their route's portal, or the Return Gate for the displaced, and none for a traveller no portal serves. The glass layer ids are a `DeskConfigSO` list (`hallPortalGlassLayers`, in portal order). | The hall shows which rings work and where a traveller went, through a hook the art already offers and with no new art. It changes the art's look, so the art side decides; without its OK, BD5 is skipped and nothing else changes. |
| BD6 | The board, the tooltip and the rings read the day's `PortalDay` (RT2). | They can never disagree with the verdicts. |

### 1.5 The routing check (RC)

| Id | Decision | Why |
|---|---|---|
| RC1 | **A new standing procedure, `TravelRuleType.PortalRouting`** (appended), content `Rule_PortalRouting`: kinds RichTourist, PoorTourist and Labourer; its line "Departures leave only through an open portal: a citizen's destination must be on today's Departure Board." Predicate: `CaseFacts.UnservedDestination` (the claim is not a route of today's `PortalDay`) gives `DirectiveFault.NoOpenPortal` (appended; runtime only). | "destination must match portal in the papers": the destination the papers and the claim name must be the route of an open portal. A rule asset, a Domain predicate and a directive fault is how every destination rule already works (T P3). |
| RC2 | **A directive fault**: read on the board, denied with no evidence. A wrong accept's citation is "Approved a departure no portal serves." (`Faults.Unserved`, "unserved"; `citation.acceptedWrong.unserved`) at the one wrong-decision penalty, after the day's free warning; denying an honest traveller costs the same one penalty. No portal-specific fine and no extra stability loss. | It is the same kind of fact as a closure (a daily list the papers are read against), and the closures are directive faults (T P1). Rule 1. |
| RC3 | **Not a compare**: portal rows are not compare-pickable. The compare path carries the lie (RC7). | A MISMATCH against one row would mislead when another row serves the place; a MATCH against the whole board would do the glance for the player. The closures are read the same way. Q4. |
| RC4 | **Makers.** Its first day plans one breaker in the first half of the queue (T P4, `Directives.HasMaker`): the slot's place is drawn on the violator stream among the day's open places that no portal serves (the closures' pattern), and its kind among the rule's kinds. On later days the violation roll may break it (`Directives.IsRolled`) for a citizen when an open, unserved place of their destination's era exists: the maker swaps the destination for one (one Range draw on the fault stream) before the papers print. | The guarantee teaches the rule on its first day; the roll keeps it alive. Keeping the era keeps the labourer's registered contract (its employer is the era's) and the era's small talk right. |
| RC5 | **A closure's violator is off the board too.** `Directives.Fault` reads the day's rules in order and closures are listed first, so the reason is "closed". One fault source (the destination), one reason (T K5). The displaced are read by the closures only, as today (the Return Gate). | No second fault on one traveller; the citation names the stronger reason. |
| RC6 | **First day: day 2**, with its guaranteed breaker. Day 2's closure (`Rule_NoAncientEgypt`) moves out, so day 2 still plans three faulty travellers in its five first-half slots (routing, the tourists' paper set, a costume error), and the closures start on day 3 (two of them). | The board appears on day 1 with two routes and every traveller served, so T's "no directive can be broken on day 1" holds. Day 2 is the destination lesson. T §2.3 planned three of day 2's five first-half slots, and four would crowd it. Q6. |
| RC7 | **L11 `Rerouted`** (`LieKind`, appended; a record lie): a citizen whose Citizen Account books a place no portal serves today travels on papers that name a route that is served (the claim). The papers agree with each other and with the board; the record's Booked departure disproves them. Proof: the compare path, a Destination field (the visa's, the contract's Worksite, a proof of means') against the record's Booked departure: a RecordMismatch, DEVIATION LOGGED. The Destination's smart link already jumps to that row (P LK1). The booked place is one Range draw on the lie stream among today's places of the claim's era that no portal serves (closed ones included); if there is none, the lie cannot show (no draw). Enabled from day 6 (`days[].lies`), and on every day 7-15. | "a mismatch as a new lie": the board alone must not be enough, and the account is where a booking lives. Keeping the era keeps the labourer's contract right. Day 6 is the first week's lightest day for new material (one new lie). Q14. |
| RC8 | **L5's worksite variant draws among the day's other routes**, not every open place. | A forged worksite off the board would look like a routing fault; a player who denied it without evidence would take an unproven denial for reading the board right. |
| RC9 | **The verdict table** (T §5.2) gains two rows (§6.4). | |

### 1.6 The generator (GN)

| Id | Decision | Why |
|---|---|---|
| GN1 | **The kind is drawn before the destination.** The case stream keeps its three draws per ordinary traveller (kind, era, place), in a new order. | A citizen's destinations are the day's routes and a displaced person's the whole world, so the kind decides which list the destination comes from. |
| GN2 | **A citizen's destination** is drawn among today's routes: the era by the plan's era weight × the number of routes in that era, then one route of that era uniformly, so each route draws in proportion to its era's weight. **The displaced** draw as today, over the day's world. | The day plans' era weights keep their meaning (D A4's busy Industrial and Modern eras become busy routes), and two draws keep the stream's count. Q13. |
| GN3 | **Planned faulty slots** (a closure's violator, the routing breaker, a planned liar or procedure breaker) claim their planned place or, as today, a place open that day; for a citizen, "open" means a route. | One fault per traveller (T K5); phase 9's fix (a planned slot claims an open place) carries over. |
| GN4 | **The books, the Costume Guide and the registry keep the whole day's world.** | The displaced's origins, a false origin's tells and the foreign-origin proofs name any place of the day. |
| GN5 | **Premades**: a famous (displaced) premade leaves through the Return Gate, so no route is needed and the pool is unchanged. A citizen premade (D B2) is booked (RT7). | Authored cases stay outside the draws (rule 4). |
| GN6 | **No new stream.** The schedule draws nothing; the routing breaker's place is on the violator stream, the rolled swap on the fault stream, L11's booking on the lie stream. | House rules: a new random concern gets a new salt; none of these is a new concern. |

### 1.7 Orders (OR)

| Id | Decision | Why |
|---|---|---|
| OR1 | **Everything the Home shop sold moves to the PC**: the Auto-Feed Scanner, the Analysis Scanner, Interview Protocols, Diplomatic Contacts and the four Speech translators, with the same ids, prices and effects, plus the Portal 04 commission. | "we will have the upgrade tree be in the pc now as another icon". Keeping the ids keeps every save's owned upgrades. |
| OR2 | **The Orders icon** (`orders`) and window: the form TC-980 "Requisition · Temporal Customs Supply", the catalogue as a tree of four branches (Desk equipment, Interview, Portals, Contacts), each item a row: its icon, name, blurb, price, and its state with one button. `UpgradeSO.branch` (a new serialized enum, `UpgradeBranch`); Generate World writes Interview on the translators. No prerequisites. | "the upgrade tree": branches read as a tree without inventing requirements the balance never had (P SC2: the two scanners combine, and neither is a tier). Q9. |
| OR3 | **Ordering.** "Order" charges the price at once (the one price rule, `HomeEconomy.UpgradeCost` over `ShopPrices.Discounted`: the price shown is the price charged) and marks the item "In transit · arrives day N+1". "Cancel" refunds it, the same day only. An item owned or in transit cannot be ordered; one the wallet cannot cover shows "Not enough cr". | Paying at order is today's shop rule; Cancel is the undo for a misclick. |
| OR4 | **Delivery at the start of the next day**: in `DayCycle.AdvanceNight`, after the day turns, every order placed before the new day is delivered: the upgrade is owned (`WorldState.UnlockUpgrade`), its unlock effect is activated from the new day (as `HandleBuyUpgrade` did), and the order is marked delivered. It is then in the day-start snapshot, so every upgrade is in force from the same office day a Home purchase was. | "they arrive next day". The game and the balance simulation share `DayCycle`. |
| OR5 | **A delivery notice** in Mail (`MailKind.Delivery`, appended): one memo per delivery day listing what arrived, regenerated from the order log by id (P ML2). | The player learns what arrived without a new screen. |
| OR6 | **The Portal 04 commission** (`Upgrade_Portal04.asset`: id `portal_04`, 300 cr, branch Portals, no effect): owning it puts portal 04 in service (PO3). | One more route is the most visible thing Orders can sell. |
| OR7 | **Home keeps** the expenses, family care, the slot machine and sleep (Expenses → Slot → Sleep). The shop panel, `HomeManager.ShowShop` and `HandleBuyUpgrade`, `HomeUIController`'s shop and its page row, `Paging` and `PagingTests` retire (the shop is their last caller). | No dead code. The shop's one live rule, the price, moves with the catalogue. Q10. |
| OR8 | **The clerk's statement** (TC-960): an order joins the day's purchases when placed, and a cancellation takes it out. The **order log** is `WorldState.orders` (additive; a cancelled order is removed). | The statement already has a purchases column. |
| OR9 | **The translators' notice** (day 4's paper): "Speech translators are sold at Home tonight" becomes "Order a Speech translator in Orders today and it arrives tomorrow." | The timing is unchanged: a translator ordered on day 4 arrives on day 5 with the displaced, so T I3's reason for the notice's day holds. |
| OR10 | The debug panel's "unlock upgrade" stays immediate. | A development tool, not the game's economy. |

### 1.8 Content (CN)

| Id | Decision | Why |
|---|---|---|
| CN1 | `agency.portals[]` and `days[].portals[]` are row-shaped for the content spreadsheet (T §15's row rules), with `ContentSheetMap` entries and the template rewritten. Every day lists its Directorate routes. | Rule 4: Saleh edits the unlocks and the routes in the sheet. |
| CN2 | **The Directorate's routes** (§10): at least four distinct places a day (the most departure portals that can be in service), each in that day's world and open that day, the day's booked places first (PR6). | RT3's fallback must always find a free route. |
| CN3 | **The morning notices** (the `news` block): `portals` ("Today's departures: {routes}."), `portalRetuned` ("Portal {number} runs the Directorate's route today: {place} is closed."), `portalInService` ("Portal {number} ({name}) enters service today."), and with D, `portalBooked` ("Portal {number} carries a Directorate booking today: {place}."). | The paper already carries the day's notices; the player reads the day's routes before the first traveller. |

## 2. The portals over the run

| Day | Departure portals in service (the Directorate's routes when the clerk sets none) | Return Gate | What is new at the desk |
|---|---|---|---|
| 1 | 01 Periclean Athens, 02 Republican Rome | – | the board, the Portals app (tomorrow's routes), Orders; every citizen served |
| 2 | 01, 02 (the clerk's routes from here on); + 04 from the day after it is ordered | – | the routing directive and its guaranteed breaker; no closure |
| 3 | as day 2 | – | the closures begin (two); a clerk route on a closed place is retuned |
| 4 | as day 2 | – | |
| 5 | as day 2 | 03 | the displaced, through the Return Gate |
| 6 | as day 2 | 03 | L11 (the rerouted booking) |
| 7-8 | as day 2; with D, the beats' Directorate bookings | 03 | |
| 9 | 01, 02, 05 (+ 04) | 03 | Desk 4's portal 05 |
| 10-15 | as day 9 | 03 | |

A careful player can afford the commission around day 3 (the run starts with 50 cr, and a perfect wallet grows about 110 cr a day, D E4); ordered on day 3, it gives three routes from day 4 and four from day 9.

## 3. The schedule: resolution, worked

`PortalSchedule.Resolve(portals, day, owned, clerkRoutes, directorateRoutes, bookings, inWorld, closed)`:

1. The portals in service: `fromDay` reached, or `upgrade` owned at the day's start (after the night's deliveries). The Returns portal is listed with no route.
2. Pass 1, in portal number order: a booking (PR6); then the clerk's route when it is in the world, open and not yet claimed.
3. Pass 2, in portal number order: each departure portal still without a route takes the first Directorate route no portal serves yet, noting why its clerk route did not run (Unset, Closed, OutsideWorld, Taken, Booked).
4. The routes (the departure portals' places, in number order) are what `Serves`, the board, the generator and the predicate read.

**Day 4, worked.** The world: Ancient to Industrial × all eight countries; closure: Victorian Britain. Directorate routes: Wilhelmine Germany, Periclean Athens, Muromachi Kyoto, Ottoman Egypt. In service: 01, 02 and 04 (its commission ordered on day 3). The clerk's routes: 01 Victorian Britain, 02 Periclean Athens, 04 none.
- Pass 1: 01's route is closed (not placed); 02 runs Periclean Athens.
- Pass 2: 01 takes the first free Directorate route, Wilhelmine Germany (noted: closed); 04 skips Wilhelmine Germany and Periclean Athens (both served) and takes Muromachi Kyoto (noted: unset).
- The board: `01  WILHELMINE GERMANY (INDUSTRIAL)` / `02  PERICLEAN ATHENS (ANCIENT)` / `04  MUROMACHI KYOTO (MEDIEVAL)`. The notices: "Today's departures: 01 Wilhelmine Germany (Industrial), 02 Periclean Athens (Ancient), 04 Muromachi Kyoto (Medieval)." and "Portal 01 runs the Directorate's route today: Victorian Britain is closed." On day 5, 01 runs Victorian Britain again.

## 4. The Portals app

```
╔═ PRT Portals ═══════════════════════════════════════════════════════════════════════════[_][□][X]╗
║ ┌─ TC-970 ──────────────────────────────────────────────┐  TOMORROW · DAY 4                        ║
║ │ (seal) TEMPORAL CUSTOMS · Debt Relief Departures       │  Routes take effect at the start of      ║
║ │ PORTAL SCHEDULE · DESK 3         16 MAR 2150 · DAY 3   │  tomorrow's shift.                       ║
║ │ 1  TODAY                                               │                                          ║
║ │ No.  Ring         Route                      Set by    │  01 Front                                ║
║ │ 01   Front        Periclean Athens (Ancient) Clerk     │  Era [Industrial v] Location [Britain v] ║
║ │ 02   Rear left    Elizabethan England        Directorate│  → Victorian Britain: CLOSED TOMORROW.  ║
║ │                   (Early modern)  (your route is closed)│    The Directorate runs its route.      ║
║ │ 03   Rear right   Return Gate · from day 5   –         │  02 Rear left                            ║
║ │ 04   Upper left   Not in service · Orders    –         │  Era [Ancient v]  Location [Greece v]    ║
║ │ 05   Upper right  Not in service · day 9     –         │  → Periclean Athens                      ║
║ │ 2  TOMORROW'S NOTICES                                  │  04 Upper left · arrives tomorrow        ║
║ │ Embargo: no travel to Victorian Britain.               │  (•) Directorate route                   ║
║ │ Busy: none (every era ×1).                             │                                          ║
║ │ ────────────────────────────────────────────────────── │  [Keep today's routes]                   ║
║ │ Issued by Temporal Customs · Desk 3   ║║│║║ TC-970/…   │  [Use the Directorate's schedule]        ║
║ └────────────────────────────────────────────────────────┘                                          ║
╚════════════════════════════════════════════════════════════════════════════════════════════════════╝
```

- The form (left, about 580 u, the pane width P AP2 sized forms for) is read-only and drawn by `FormView` from a `FormSpecSO` (TC-970), like every PC page (P FO9). The Tomorrow panel (right, themed chrome) holds the choosers.
- A disabled Location entry reads "Egypt: New Kingdom Egypt · closed tomorrow", "Italy: Republican Rome · on portal 02", or (with D) "Greece: Periclean Athens · Directorate booking".
- Changing the era keeps the location when that country has a place in the new era, else it takes the first enabled one.
- The app works with or without a traveller at the desk. Its rows are not search entries: the Investigation app's Rules tab carries the day's schedule for search (§6.6).

## 5. The Departure Board and the rings

### 5.1 The board

| Part | Rule |
|---|---|
| Anchor | `DepartureBoard`: `Anchor_DepartureBoard`, else the bare name `16 Departure board blank display` (its sprite's bounds). No default pose: missing in the 3D room. |
| Text | `BoardRows` (world-space TMP, `Gameplay` sorting layer, built in `OfficeGameplay` by Build Office UI; auto-sized, every row the same size), its rect the anchor's bounds inset by `DeskConfigSO.hallBoardInset` (first cut 8 % a side), re-fitted in `LateUpdate` when the bounds move. Ink `hallBoardInk` (FFF2D9). Text: `PortalBoardText.Rows(PortalDay, label)` (Visuals, pure). |
| Click box | on `Interactable` over the bounds: hover outline, tooltip `PortalBoardText.Tooltip(PortalDay, label)`: "DEPARTURE BOARD" then one line per portal ("01 Front: Periclean Athens (Ancient)", "03 Rear right: Return Gate, the displaced by Return Order", "04 Upper left: not in service"). The tooltip template grows with its lines (a content size fitter), checked at 720p. |
| Evening | The text is lit: the hall's evening dims the art, not the board. The display darkens, so the contrast only rises; the probe measures it at the full evening. |
| The claim strip | The HUD's claim strip covers the display's top few pixels at 1080p; the inset keeps the rows below it, and the probe checks both resolutions. |

### 5.2 The rings (BD5, with the art side's OK)

`AnimeHallPortalLink` (gameplay, added by the binder to its own object when the art office carries an `AnimeHallPresentation`, as the shift link is): at the day's start it sets each ring's glass (`hallPortalGlassLayers[number - 1]`, first cut `53 Portal 01 front painted glass` to `57 Portal 05 upper right painted glass`) to white (the art's own colour) when in service and to `hallPortalIdleTint` when not. On an accept it pulses the traveller's portal (`PortalDay.PortalFor(claim)`; the Return Gate for the displaced; none for an unserved destination) to `hallPortalPulseTint` and back over `hallPortalPulseSeconds`; with reduced motion, one step up and one down. `SetLayerTint` sets the sprite's colour, which multiplies with the lighting the art applies through its property block, so the evening still darkens the rings.

### 5.3 The scene contract's new rows (`docs/SCENE_CONTRACT_GAMEPLAY.md`, in plan phase PR5)

- The anchors table: `DepartureBoard` | the Departure Board: its rows (a gameplay text) and its click box with the schedule's tooltip | no fallback in the room | `Anchor_DepartureBoard` over a board's display.
- The hall's table: `DepartureBoard` | `16 Departure board blank display` | the display's sprite bounds; the rows sit inset below the claim strip.
- The hooks section: "The gameplay layer drives **time** and, with the art side's OK, **the portal rings' glass**: `AnimeHallPortalLink` calls `SetLayerTint` on the layers named in `DeskConfigSO.hallPortalGlassLayers` (dimmed when a portal is not in service, a pulse on a departure). A renamed layer is a knob edit."

## 6. The routing check

### 6.1 The rule

| Part | Rule |
|---|---|
| Type | `TravelRuleType.PortalRouting`, appended after the last member on `main` at build time (`DebtStanding` = 9 today; after D's `TransponderRecall` if that lands first); pinned in `SerializedEnumsTests` |
| Asset | `Rule_PortalRouting`: a `rules[]` row, `kinds` RichTourist, PoorTourist, Labourer; no place; the line of RC1 |
| Days | listed from day 2 on, after the day's closures |
| Predicate | `Directives.FaultOf(PortalRouting, facts)` = `NoOpenPortal` when `facts.UnservedDestination`, else None; the kinds filter is `Directive.AppliesTo`. `CaseFactory.Facts` sets `UnservedDestination = !portalDay.Serves(claim)` |
| Fault | `DirectiveFault.NoOpenPortal` (appended), `Faults.Unserved` ("unserved"), `citation.acceptedWrong.unserved` "Approved a departure no portal serves." |
| Guarantee | `Directives.HasMaker(PortalRouting)`: its first day plans one breaker (RC4) |
| Roll | `Directives.IsRolled(PortalRouting)`: from its second day, among the rolled procedures the traveller can break (`CanBreak`: a citizen kind; the caller adds "an open, unserved place of the destination's era exists") |
| Content checks | `Directives.RuleProblems`: a routing rule lists kinds and never `Displaced` (the Return Gate serves them). `PortalSchedule.DayProblems`: on its first day, at least one open place of the day's world is not a Directorate route (else the guarantee plans none: a warning) |

### 6.2 Where the desk reads it

The board (its rows) and its tooltip, the Investigation app's Rules tab (the memo's schedule, §6.6), the Portals app, the morning notices and the Directives line. The destination itself is on the claim strip, the primary form and the record's Travel group.

### 6.3 L11, the rerouted booking

| Part | Rule |
|---|---|
| Kinds | RichTourist, PoorTourist, Labourer (`LieKinds.AppliesTo`); a record lie (`LieKinds.IsRecordLie`) |
| What is false | The account's **Booked departure** (`CitizenAccount.BookedDeparture`, new: the booked place's label when it differs from the claim; empty means the claim) is a place of the claim's era that no portal serves today; the papers print the claim, a route |
| The draw | on the lie stream in `RecordLies.Plan`: one variant (no variant draw), then the booked place (one Range draw over `RecordLieContext.UnservedInEra`, new); NoPossibleLie, with no draw, when that list is empty |
| Tells | every Destination field on the papers (`recordTells`: TC-101's; TC-415's or TC-417's; TC-520's Worksite); the papers are not rewritten, the record is what differs |
| Proof | a Destination field against the record's Booked departure: `DiscrepancyLog.Prove`'s RecordMismatch, unchanged; the report line reads like "DESTINATION: the papers say Republican Rome (Ancient); the Citizen Account books Babylonia (Ancient)" |
| Not proof | the Analysis Scanner (papers against papers) finds nothing: the papers agree (P SC5 holds) |
| Look, claim, impacts | the claim's (unchanged code): dressed for the route, the claim line names it, an accept's impacts land on it |
| Days | `days[].lies` gains `Rerouted` from day 6; D's days 7-15 list it every day |

### 6.4 The verdict table's new rows (T §5.2)

| Traveller | Accept | Deny, ≥1 deviation logged | Deny, none logged |
|---|---|---|---|
| Unserved destination (honest papers; a directive fault) | wrong: "Approved a departure no portal serves." | impossible (nothing false to log) | correct |
| L11 rerouted (a deviation fault) | wrong: "Approved forged papers." | correct (the Destination × Booked departure deviation) | wrong: an unproven denial |

Every wrong call costs the one penalty (rule 1). A wrongly accepted unserved traveller's impacts land on their destination, as a closure violator's do today (unchanged code); their ring does not pulse (BD5).

### 6.5 Closures and routing on one day

| Traveller | Board | Directives | Reason on a wrong accept |
|---|---|---|---|
| A citizen bound for a closed place (a closure's violator) | not on the board (RT4) | the closure's line | "closed" (closures come first) |
| A citizen bound for an open place no portal serves | not on the board | the routing line | "unserved" |
| A displaced person bound for a closed origin | the board does not apply (the Return Gate) | the closure's line | "closed" |
| An L11 citizen | on the board | – | "forged" |

### 6.6 The Investigation app and the rest of the PC

- **Rules** (TC-940, the Directive Memo): a new section "Portal schedule" (a table: No., Ring, Route, Set by) after the numbered directives. Each route row links to its place's row in the first reference book (the closures' existing ↗, P LK1). The rows are search entries of the day layer (P SE2), keyed `rule:portal:{n}`. Not compare-pickable (RC3).
- **Mail**: the day's directive memo is built by the same memo builder, so it carries the schedule.
- **Steps** (P ST): `steps.rules` ("Today's rules for the destination") becomes "Today's rules and the Departure Board for the destination"; the board's tooltip counts as viewing the rules (BD4).
- **Records**: the Travel group's Booked departure row prints `BookedDeparture` when set (L11), else the claim, as today.

## 7. The case generator

### 7.1 One ordinary traveller, in order (the changes in bold)

1. The forced blueprint, the premade (forced, or rolled from the pool), the planned slot (a closure's or **the routing rule's** violator place; a planned liar or procedure breaker): unchanged.
2. **The kind** (one weighted draw over the day's kinds on the case stream; `TravellerKinds.PickWeight` and its filters unchanged). *Moved before the destination.*
3. The destination: a premade's authored place; a planned violator's place; else **for a citizen, the era over the routes** (weight = the plan's era weight × the routes in that era) **and a route of that era**; for the displaced, the era over the world and a place of that era, as today.
4. Everything after (the name, the birth date, the look, the account, the lie roll, the violation roll with **the routing swap**, the costume roll, the papers, **L11's booking**, the directive fault read over `CaseFacts`): as today.

### 7.2 Demand, worked

Day 8 (D §3: Industrial 2, the other eras 1) with the routes 01 Victorian Britain (Industrial), 02 Periclean Athens (Ancient), 04 Wilhelmine Germany (Industrial): Industrial weighs 2 × 2 = 4 and Ancient 1 × 1 = 1, so a citizen goes to an Industrial route 4 times in 5, each of the two 2 times in 5, and to Periclean Athens 1 time in 5: the routes draw 2 : 1 : 2, their eras' weights. `PortalDraw.EraWeight(planWeight, routesInEra)` is the one rule, tested.

### 7.3 Streams

| Stream | Draws | Change |
|---|---|---|
| Case (`Seeds.ForCase`) | kind, era, place, then as today | the order: the kind first; a citizen's era and place over the routes |
| Violators (`Seeds.ForViolators`) | the slots, then each planned rule's maker in plan order | + the routing rule's first-day place (the closures' pattern) |
| Faults (`Seeds.ForFaults`) | the violation roll, the rule, its maker | + the routing swap (one Range draw) |
| Lies (`Seeds.ForLies`) | the roll, the kind, the planner | + L11's booked place (one Range draw) |
| Every other stream | unchanged | none; no new salt |

## 8. Orders

### 8.1 The catalogue

| Branch | Item (id) | Price | Effect (unchanged) | Asset |
|---|---|---|---|---|
| Desk equipment | Auto-Feed Scanner (`scanner_autofeed`) | 200 cr | handed-over papers scan themselves (P SC3) | hand-authored |
| Desk equipment | Analysis Scanner (`adv_scanner`) | 300 cr | a scan by hand marks one contradiction (P SC4) | hand-authored |
| Interview | Interview Protocols (`interview_protocols`) | 120 cr | the birth-date question | hand-authored |
| Interview | Near East, Mediterranean, East Asia and Northern Europe Translators: Speech (`tr_<pack>_spoken`) | 80 cr each (`translation.packs[].spokenCost`) | the region's speech in English (T I4) | generated |
| Portals | Portal 04 commission (`portal_04`) | 300 cr | portal 04 in service (PO3) | hand-authored, new |
| Contacts | Diplomatic Contacts (`diplo_contacts`) | 350 cr | a better pay rate (its unlock effect) | hand-authored |

### 8.2 The window

```
╔═ ORD Orders ═══════════════════════════════════════════════════════════════════════════════[_][□][X]╗
║ ┌─ TC-980 ───────────────────────────────────────────────────────────────────────────────────────┐ ║
║ │ (seal) TEMPORAL CUSTOMS SUPPLY · REQUISITION                           16 MAR 2150 · DAY 3     │ ║
║ │ Wallet 612 cr · Orders arrive at the start of tomorrow's shift.                                 │ ║
║ │ 1  DESK EQUIPMENT                                                                               │ ║
║ │  ├ [icon] Auto-Feed Scanner     200 cr  Handed-over papers feed the scanner…  Delivered day 2  │ ║
║ │  └ [icon] Analysis Scanner      300 cr  A scan by hand marks one contradiction.  [ Order ]     │ ║
║ │ 2  INTERVIEW                                                                                    │ ║
║ │  ├ [icon] Interview Protocols   120 cr  Ask travellers when they were born.   Owned            │ ║
║ │  ├ [icon] Near East Translator: Speech  80 cr  …        In transit · arrives day 4  [ Cancel ] │ ║
║ │  └ …                                                                                            │ ║
║ │ 3  PORTALS                                                                                      │ ║
║ │  └ [icon] Portal 04 commission  300 cr  The upper-left ring: one more route a day.  [ Order ]  │ ║
║ │ 4  CONTACTS                                                                                     │ ║
║ │  └ [icon] Diplomatic Contacts   350 cr  Favours owed: a better pay rate.     Not enough cr     │ ║
║ │ ─────────────────────────────────────────────────────────────────────────────────────────────── │ ║
║ │ Issued by Temporal Customs · Desk 3                                ║║│║║ TC-980/…   [stamp]   │ ║
║ └─────────────────────────────────────────────────────────────────────────────────────────────────┘ ║
╚══════════════════════════════════════════════════════════════════════════════════════════════════════╝
```

- The form is a `FormSpecSO` (TC-980) drawn by `FormView`; the tree is the form's sections (a branch) and indented rows (an item). The Order and Cancel buttons are themed chrome laid over each row's action cell, as the Records lookup field sits over its form.
- The window opens restored at 1040 × 800 u (`DesktopConfigSO`).
- States (`Orders.StateOf`): Orderable ("Order"), TooDear ("Not enough cr", disabled), InTransit ("In transit · arrives day N+1", with "Cancel" on the day it was placed), Owned ("Delivered day N" from the log, or "Owned" when the upgrade came another way: an effect or the debug panel).

### 8.3 Order, cancel, deliver

| Step | Rule |
|---|---|
| Order | `Orders.StateOf` must be Orderable. The money moves now (`world.money -= price`), the entry `{upgradeId, orderedDay = today, price}` joins `WorldState.orders`, and the day's statement row's purchases grow. |
| Cancel | Only an order placed today and not delivered: the charged price comes back and the entry leaves the log. |
| Deliver | `DayCycle.AdvanceNight`, after `day++`: `Orders.Due(log, day)` (ordered before the day, not delivered), in log order: `UnlockUpgrade`; its unlock effect activated with start day = the new day (`TimelineService.ActivateEffect`, instant ops applied); `deliveredDay = day`. The day's snapshot is taken later, when the office loads, so each delivery counts that day. |
| Mail | `Mailbox` builds one Delivery memo per `deliveredDay` from the log ("Delivered: Auto-Feed Scanner, Near East Translator: Speech."), id `delivery:{day}`. |
| Saves | The end-of-shift save holds pending orders; Continue from Home keeps them; the night delivers them. An older save loads an empty log and keeps every upgrade it owns. |
| The simulation | orders nothing, as it bought nothing at Home; the night's delivery runs through the same `DayCycle` step. |

### 8.4 Home

Expenses → Slot → Sleep. `HomeSceneBuilder` removes `ShopPanel` (`DestroyChildIfPresent`); `HomeUIController` loses the shop, its rows and its page row; `HomeManager` loses `ShowShop` and `HandleBuyUpgrade` (the unlock-effect call moves into the delivery step); `Paging` and `PagingTests` retire with their last caller; the Home art's shop panel and the upgrade icons' Home slot retire or move (§12).

## 9. Data models

### 9.1 Domain (pure, EditMode-tested; the names are proposals, and the build re-reads the code)

```csharp
/// A place of the world by its content keys: a route (RT1).
public readonly struct PlaceKey : IEquatable<PlaceKey>
{
    public readonly string Country, Era;                          // ids, as rules[] and places name them
    public static bool TryParse(string text, out PlaceKey key);   // "greece:ancient"
    public override string ToString();                            // "greece:ancient"
}

/// A portal's job (agency.portals[].role). Serialized in the content library: append only, pinned.
public enum PortalRole { Departures, Returns }

/// One ring of the hall (agency.portals[], written by Generate World into the library's agency block).
[Serializable] public sealed class PortalSpec { public int number; public string name; public PortalRole role; public int fromDay; public string upgrade; }

/// Who set what a portal runs today. Runtime only.
public enum RouteSource { Clerk, Directorate, Booking, Returns, NotInService }

/// Why the Directorate's route ran instead of the clerk's. Runtime only.
public enum RouteNote { None, Unset, Closed, OutsideWorld, Taken, Booked }

public readonly struct PortalRoute { public int Number; public string Name; public PortalRole Role; public RouteSource Source; public RouteNote Note; public PlaceKey? Place; public PlaceKey? ClerkRoute; }

/// The day's schedule, fixed at the day's start (RT2).
public sealed class PortalDay
{
    public IReadOnlyList<PortalRoute> Portals { get; }   // every portal, in number order
    public IReadOnlyList<PlaceKey> Routes { get; }       // the departure portals' places, in number order
    public int ReturnsPortal { get; }                    // 0 when not in service
    public bool Serves(PlaceKey place);
    public int PortalFor(PlaceKey place);                // the departure portal serving it; 0 when none
}

public static class PortalSchedule
{
    bool InService(PortalSpec portal, int day, Func<string, bool> owned);
    PortalDay Resolve(IReadOnlyList<PortalSpec> portals, int day, Func<string, bool> owned,
                      IReadOnlyDictionary<int, PlaceKey> clerk, IReadOnlyList<PlaceKey> directorate,
                      IReadOnlyList<PlaceKey> bookings, Func<PlaceKey, bool> inWorld, Func<PlaceKey, bool> closed);
    IReadOnlyList<RouteChoice> Choices(int portal, IReadOnlyList<PlaceKey> tomorrowsPlaces, PortalDay tomorrow,
                                       Func<PlaceKey, bool> closedTomorrow);   // every place, enabled or with its reason
    IReadOnlyList<PlaceKey> Unserved(IReadOnlyList<PlaceKey> world, PortalDay day, Func<PlaceKey, bool> closed, string era = null);
    List<string> PortalProblems(IReadOnlyList<PortalSpec> portals, Func<string, bool> upgradeExists);
    List<string> DayProblems(string day, int dayNumber, IReadOnlyList<PortalSpec> portals, IReadOnlyList<PlaceKey> directorate,
                             Func<PlaceKey, bool> inWorld, Func<PlaceKey, bool> closed, bool routingFirstDay, bool displacedWeighted);
}

public readonly struct RouteChoice { public PlaceKey Place; public bool Enabled; public RouteNote Why; public int OnPortal; }

public static class PortalDraw
{
    /// An era's weight for a citizen's destination (GN2): its plan weight × its routes today; 0 without a route.
    float EraWeight(float planWeight, int routesInEra);
}

/// The clerk's routes over WorldState.portalSettings (RT5): set, hand back, read.
public static class PortalSettings
{
    void Set(List<PortalSettingEntry> settings, int portal, PlaceKey place);
    void Clear(List<PortalSettingEntry> settings, int portal);
    IReadOnlyDictionary<int, PlaceKey> Read(IReadOnlyList<PortalSettingEntry> settings);
}

// Directives (grown): CaseFacts.UnservedDestination; FaultOf(PortalRouting); HasMaker, IsRolled and CanBreak include it;
//   RuleProblems checks its kinds. DirectiveFault.NoOpenPortal (appended); Faults.Unserved = "unserved".
// LieKind.Rerouted (appended, pinned); LieKinds: a record lie, citizens only.
// RecordLies: the Rerouted variant; RecordLieContext.UnservedInEra (new); RecordLieContext.OpenPlaces is now the day's other routes (RC8).
// CitizenAccount.BookedDeparture (new; empty = the claim); AccountRecords.Record prints it on the Booked departure row.

public enum OrderState { Orderable, TooDear, InTransit, Owned }
public static class Orders
{
    OrderState StateOf(string upgradeId, bool owned, IReadOnlyList<OrderEntry> log, int money, int price);
    OrderEntry Place(List<OrderEntry> log, string upgradeId, int day, int price);   // null when owned or in transit
    int Cancel(List<OrderEntry> log, string upgradeId, int day);                   // the refund; 0 when nothing placed today is pending
    IReadOnlyList<OrderEntry> Due(IReadOnlyList<OrderEntry> log, int day);         // ordered before the day, not delivered
}

// UpgradeBranch { Desk, Interview, Portals, Contacts }: UpgradeSO.branch, serialized, pinned.
// MailKind.Delivery (appended); Mailbox builds one memo per delivery day from the log.
// OfficeAnchorId.DepartureBoard (appended: 23), pinned.
```

Visuals: `PortalBoardText.Rows(PortalDay, Func<PlaceKey, string> label)` and `Tooltip(...)` (pure strings, tested).

### 9.2 Saves (`WorldState`, additive; the save version stays 3)

```csharp
/// The clerk's standing routes (the Portals app, RT5): one entry per departure portal set; a portal without one runs the Directorate's route. An older save loads it empty.
public List<PortalSettingEntry> portalSettings = new();
[Serializable] public sealed class PortalSettingEntry { public int portal; public string country; public string era; }

/// Every order placed and not cancelled (the Orders app, OR8): pending until delivered at the start of the day after it was placed, then kept as the log. An older save loads it empty.
public List<OrderEntry> orders = new();
[Serializable] public sealed class OrderEntry { public string upgradeId; public int orderedDay; public int price; public int deliveredDay; }
```

### 9.3 Content (`world_source.json`, through Generate World)

```json
"agency": {
  "portals": [
    {"number": 1, "name": "Front",       "role": "Departures", "fromDay": 1},
    {"number": 2, "name": "Rear left",   "role": "Departures", "fromDay": 1},
    {"number": 3, "name": "Rear right",  "role": "Returns",    "fromDay": 5},
    {"number": 4, "name": "Upper left",  "role": "Departures", "upgrade": "portal_04"},
    {"number": 5, "name": "Upper right", "role": "Departures", "fromDay": 9}
  ]
},
"rules": [
  {"asset": "Rule_PortalRouting", "type": "PortalRouting",
   "description": "Departures leave only through an open portal: a citizen's destination must be on today's Departure Board.",
   "kinds": ["RichTourist", "PoorTourist", "Labourer"]}
],
"days": [
  {"day": 1, "portals": [{"country": "greece", "era": "ancient"}, {"country": "italy", "era": "ancient"},
                         {"country": "egypt", "era": "ancient"},  {"country": "iraq", "era": "ancient"}]}
],
"news": {
  "portals": "Today's departures: {routes}.",
  "portalRetuned": "Portal {number} runs the Directorate's route today: {place} is closed.",
  "portalInService": "Portal {number} ({name}) enters service today."
}
```

Generate World writes `agency.portals` into the library's agency block (`PortalSpec`s), each day's routes into `DayPlanSO.directorateRoutes` (place profile references, so a renamed place cannot dangle), `Rule_PortalRouting` like any rule, and `UpgradeSO.branch = Interview` on the translators. `fromDay` and `upgrade` are omitted when blank.

**`ContentSheetMap`** (and the template rewritten):
- under `agency`: `Rows("agencyPortals", "portals", Key("number"), Int("number").Required(), Text("name"), Text("role").OneOf("Departures", "Returns"), Int("fromDay").Omit().Note("the day it enters service; blank: by order only"), Text("upgrade").Omit().Note("the upgrade id whose delivery commissions it; blank: by day only"))`;
- under `days`: `Rows("dayPortals", "portals", Text("country").Ref("countries"), Text("era").Ref("eras")).Note("the Directorate's routes in order: a portal with no clerk route takes the first one no portal serves")`;
- `news`: `portals`, `portalRetuned`, `portalInService` (and `portalBooked` with D);
- the note of `days[].lies` gains `Rerouted`; `rules[].type` gains `PortalRouting`.

### 9.4 Knob assets (Inspector; Generate World never writes them)

- `Assets/Data/Upgrades/Upgrade_Portal04.asset` (new): id `portal_04`, displayName "Portal 04 commission", description "Commission the upper-left ring: one more route a day, set in Portals.", cost 300, branch Portals.
- Every hand-authored `UpgradeSO` gains its `branch` (the Auto-Feed and Analysis Scanners: Desk; Interview Protocols: Interview; Diplomatic Contacts: Contacts).
- `DeskConfigSO` (its anime hall group): `hallBoardInk`, `hallBoardInset`, `hallPortalGlassLayers` (5 layer ids), `hallPortalIdleTint`, `hallPortalPulseTint`, `hallPortalPulseSeconds`.
- `DesktopConfigSO`: the Portals and Orders windows' restored sizes.

## 10. The Directorate's routes, days 1-15

Each list is in order: portal 01 takes the first free route, and so on. Every entry is in that day's world and open that day; with D, the day's booked place comes first.

| Day | Closures that day | Booking (with D) | Directorate routes |
|---|---|---|---|
| 1 | – | – | Periclean Athens, Republican Rome, New Kingdom Egypt, Babylonia |
| 2 | – (RC6) | – | Periclean Athens, Northern Song Kaifeng, Florentine Republic, Anglo-Saxon England |
| 3 | Northern Song Kaifeng, Tokugawa Edo | – | Elizabethan England, Republican Rome, Abbasid Baghdad, Late Ming Suzhou |
| 4 | Victorian Britain | – | Wilhelmine Germany, Periclean Athens, Muromachi Kyoto, Ottoman Egypt |
| 5 | Weimar Berlin, New Kingdom Egypt | – | Post-war Britain, Qing Shanghai, Florentine Republic, Babylonia |
| 6 | Showa Tokyo | – | Periclean Athens, Victorian Britain, Tokugawa Edo, Nasser's Egypt |
| 7 | New Kingdom Egypt, Showa Tokyo | Pell: Periclean Athens | Periclean Athens, Victorian Britain, Late Ming Suzhou, Dolce Vita Rome |
| 8 | Northern Song Kaifeng, Weimar Berlin | Ines: Victorian Britain | Victorian Britain, Wilhelmine Germany, Tokugawa Edo, Abbasid Baghdad |
| 9 | Byzantine Mystras, Tokugawa Edo | – (Gutenberg: the Return Gate) | Khedivate of Egypt, Victorian Britain, Meiji Nagoya, Republican Rome |
| 10 | Babylonia | Pell: Periclean Athens | Periclean Athens, Victorian Britain, Showa Tokyo, Qing Shanghai |
| 11 | Japan, every era | Rook: Victorian Britain | Victorian Britain, Post-war Britain, Wilhelmine Germany, Dolce Vita Rome |
| 12 | New Kingdom Egypt, Tokugawa Edo | – ("Ada": the Return Gate) | Weimar Berlin; Bologna, Kingdom of Italy; Qing Shanghai; Byzantine Mystras |
| 13 | Weimar Berlin, Northern Song Kaifeng | Callum: Wilhelmine Germany | Wilhelmine Germany; Post-war Britain; Athens, Kingdom of Greece; Kingdom of Iraq |
| 14 | the Modern era | Quill: Republican Rome | Republican Rome, Victorian Britain, Meiji Nagoya, Khedivate of Egypt |
| 15 | Victorian Britain, Showa Tokyo | Pell: Periclean Athens | Periclean Athens, Republican Rome, Babylonia, Iron Age Britain |

- Days 1-6 land with this spec (plan phase PR2); days 7-15 land with D's content (plan phase PR6), and replay day 6's until then (`DayPlans.Pick`).
- The lists rotate countries and eras, so a run on the Directorate's schedule alone spreads history across the map over a week; the simulation checks how much (BX2).
- Days 7-15's closures are D §3's, unchanged; no Directorate route is ever on one of them.

## 11. Balance knobs

### 11.1 Where each one lives

| Knob | Where Saleh edits it | First cut |
|---|---|---|
| Each portal's first day, commission and role | the sheet `agencyPortals` | §9.3 |
| The Portal 04 commission's price | `Upgrade_Portal04.asset` (Inspector) | 300 cr |
| Every other upgrade's price | its `UpgradeSO` asset; the translators' `translation.packs[].spokenCost` (the sheet) | unchanged |
| The Directorate's routes per day | the sheet `dayPortals` | §10 |
| Demand per route | `days[].eras` weights (existing) | unchanged |
| The routing rule's first day | which days list `Rule_PortalRouting` in `days[].rules` | day 2 |
| The later-day roll | `days[].violationChance` (existing; the routing rule joins the rolled rules) | unchanged |
| L11's days | `days[].lies` | day 6 on |
| The board's ink and inset; the rings' idle and pulse tints and the pulse's length | `DeskConfigSO` (Inspector) | FFF2D9; 8 %; a 45 % grey; a pale cyan; 0.8 s |
| The two windows' sizes | `DesktopConfigSO` (Inspector) | 1040 × 800 u |
| The one penalty, the free warning | `GameConfigSO` (unchanged) | 10 cr; 1 |

### 11.2 Balance hooks for phase 23's simulation

| Id | Hook | Why |
|---|---|---|
| BX1 | The simulation runs the Directorate's routes (it sets none) and orders nothing, through the same `BuildToday` and `DayCycle` steps. | The measured run is the game's own. |
| BX2 | The summary lists each day's routes and each route's share of the accepted travellers, and per run the places history moved, the leader's first day and its dominance. | Travellers now land on 2 to 4 places a day instead of up to 40, so history concentrates. First watch line: the leader's median first day moving more than two days earlier than the pre-portal baseline. |
| BX3 | The 2150 contamination count (D X5) per place, not only per run. | Carries from smugglers and strandings concentrate on the routes too. |
| BX4 | The epilogue thresholds re-proposed over the new destinations (D E2). | The attribute totals move with the destinations. |
| BX5 (later) | A "steering" policy: every night set the routes to the places whose roles add most to one attribute. | Measures the most a clerk can move history; not needed for the first cut. |

## 12. Art

### 12.1 `docs/ART_ASSET_LIST.md` changes (made in the plan's phases)

**Section 1, the office**, two rows:

| Prop | Anchor, and what the game finds | Status |
|---|---|---|
| The departure board: its display stays blank, the game draws the rows | `DepartureBoard`: the hall's `16 Departure board blank display` | done: keep the display text-free |
| The five portal rings: the game tints their glass (dimmed when a portal is not in service, a pulse on a departure) | the presentation's layers `53` to `57 Portal 0n … painted glass` | done; optional, Tier 3: a lit-glass variant per ring if the tint reads flat |

**Section 3, the PC desktop:**
- "Desktop icons × 6" becomes "× 8"; the ids line gains `portals` (a portal ring, perhaps with an arrow through it) and `orders` (a parcel or a crate with a tick). Same format: 128 × 128, a bold greyscale glyph on transparent, no plate.
- New row, **Orders branch glyphs × 4**: the Orders app's branch heads, 40 × 40 u; `Assets/Art/UI/Resources/Orders/branch_<id>.png` for `desk`, `interview`, `portals`, `contacts`; now code-drawn placeholders; deliver 128 × 128 greyscale glyphs, no plate; Tier 1; missing.
- New row, moved from section 6, **Upgrade icons × 9**: at the left of each Orders row, 44 × 44 u; `Assets/Art/UI/Resources/Orders/upgrade_<id>.png` (moved from `Resources/Home/` with their metas); the eight ids plus `portal_04`; 256 × 256; Tier 1; 2 interim, 7 missing.

**Section 6, Home:** the Shop panel and Upgrade icons rows go (the icons move to section 3); the Retired table gains `panel_shop.png` ("the Home shop moved to the PC's Orders app").

**Totals:** section 3 goes from 16 + 11 (27 files) to 31 + 11 (42); section 6 from 19 + 7 (26) to 10 + 7 (17); the total from 81 + 19 (100) to 87 + 19 (106).

### 12.2 The scene contract

§5.3's rows, in `docs/SCENE_CONTRACT_GAMEPLAY.md`.

## 13. Tests and golden masters

### 13.1 EditMode (Domain and Visuals first; the plan names each test)

- `PortalScheduleTests`: `InService` (by day; by an owned upgrade; both, whichever first); `Resolve` (a clerk route runs when open and in the world; a closed one runs the first free Directorate route and notes it; outside the world; unset; a fallback never displaces a valid clerk route; two portals never share a place; the Returns portal serves no route; the not-in-service rows are listed; with D, bookings); `Serves` and `PortalFor`; `Choices` (closed, taken and booked choices disabled with their reason); `Unserved` (open places only; by era); `PortalProblems`; `DayProblems`.
- `PortalDrawTests`: `EraWeight` is the plan weight × the routes; 0 without a route.
- `PortalSettingsTests`: set, hand back, read; a second set replaces the first.
- `DirectivesTests`: routing's decision table (unserved: breaks; served: holds; the displaced: not read), `FaultOf` is `NoOpenPortal`, `HasMaker`, `IsRolled`, `CanBreak` (citizens), a closed and unserved destination reads "closed" first, `RuleProblems` (kinds required; `Displaced` refused).
- `FaultsTests`: `NoOpenPortal`'s reason is "unserved".
- `RecordLiesTests`: Rerouted books a place of the claim's era that no portal serves; one draw; no possible lie (no draw) without one; its tells are every Destination field; L5's worksite draws among the other routes.
- `LieKindsTests`: Rerouted is a record lie, for citizens only.
- `OrdersTests`: every state; `Place` (refuses owned or in transit); `Cancel` (today only, returns the price); `Due` (ordered before the day, not delivered).
- `MailboxTests`: one Delivery memo per delivery day, listing the items; a pending order sends nothing.
- `PortalBoardTextTests` (Visuals): the rows (departure portals in service, in number order, upper case); the tooltip (every portal).
- `DesktopLayoutTests` and `DesktopIconPlaceholderTests`: eight icons (the arrange grid's second column); the two new glyphs.
- `SerializedEnumsTests`: `TravelRuleType.PortalRouting`, `LieKind.Rerouted`, `OfficeAnchorId.DepartureBoard`, `UpgradeBranch`, `PortalRole`.
- `ContentSheetMapTests`: `agencyPortals`, `dayPortals`, the news fields.
- Retired: `PagingTests` (with `Paging`).

### 13.2 Golden masters (the audit baseline, `docs/reviews/audit-baseline`)

| Golden | Effect | Why |
|---|---|---|
| `cases.txt` (the static case dump) | **every day changes** | the kind is drawn before the destination; citizens' destinations are routes; day 2 loses its closure and gains the routing breaker; L11 from day 6. Unlike D's plan, no day stays byte-identical; the one re-pack explains it |
| The play transcript and saves | change | the destinations and notices; `portalSettings` and `orders` in the save (empty unless the job sets or orders); the audit play job orders at the PC instead of buying at Home |
| Scene dumps | `OfficeGameplay` (two icons and windows, the board's text and click box, the ring link); `HomeScene` (no shop panel) | the builders |
| Data hashes | the upgrades (branch; Portal 04), the translators (branch), `Rule_PortalRouting`, the day plans (routes; day 2's rules), the library (portals) | Generate World |
| Static metrics | new types; `Paging` and the shop's code gone | – |
| The balance summary | re-run (BX1-BX4) | phase 23's thresholds move |

## 14. `docs/FEATURES.md` changes (in the commits of the behaviour)

| Section | Line (its first words) | Change |
|---|---|---|
| Run & meta | the save line | + `portalSettings` and `orders`, additive |
| Run & meta | "Home phase between days" | the shop goes: Expenses → Slot → Sleep; the upgrade list moves to Orders |
| Office scene — the art office | (a new line) | the Departure Board (anchor, rows, tooltip) and the rings' tint and pulse |
| Desk | "Desk scanner" | the two scanners are Orders items, delivered at the start of the next office day |
| Fake-OS desktop | "Six desktop icons" | eight icons: + Portals, Orders |
| Fake-OS desktop | (new lines) | the Portals app (§4); the Orders app and delivery (§8); Mail's delivery memo |
| World | the day plans' line | the portals, their unlocks, the Directorate's routes and the resolution; a citizen's destination is a route (by era weight), the kind drawn first; the displaced's Return Gate |
| Investigation loop | the Directives memo line | the portal schedule section |
| Investigation loop | the directives and faults lines | `PortalRouting` (its breaker on day 2, the roll after), the closures from day 3 retuning routes, L11, L5's worksite among the routes, the new citation |
| Translation | "Translators" | ordered in Orders, arriving the next office day; the day-4 notice's words |
| Shift clock & queue | the guarantees line | day 2: routing instead of a closure |
| Content & tooling | Generate World, the validator, the sheets, the simulation | the new inputs, checks (§6.1, `PortalProblems`, `DayProblems`) and sheets; BX1-BX4 |

## 15. Open questions for Saleh

Each lists the recommended option first; the plan builds it and changes only the decisions a different answer names.

| Q | Question | Options (recommended first) |
|---|---|---|
| Q1 | When does a route change take effect? (RT2) | **A** at the start of tomorrow's shift; today's board never changes. **B** at once, for travellers not yet called (the rest of the queue is regenerated, with a guard so that closing every route cannot turn the queue into easy denials). **C** never by the clerk: the Directorate sets every route and the app only shows them. |
| Q2 | How many portals, and how do they unlock? (PO3) | **A** five rings: 01 and 02 on day 1, 03 (the Return Gate) on day 5, 04 by order (300 cr), 05 on day 9 with Desk 4. **B** as A, but 04 and 05 both by order (300 and 450 cr). **C** as A, but every portal by day (04 from day 6; the Orders app sells no portal). |
| Q3 | The displaced, and the authored beats (PO2, RT7) | **A** the displaced go home through the Return Gate (no route needed); a forced citizen premade's place gets a Directorate booking. **B** the displaced need a route too: their origins are drawn from the routes, the famous need bookings, and the premade pool is filtered to served places. **C** A without bookings: a beat whose place is not a route becomes a routing fault that day. |
| Q4 | What kind of fault is "not on the board"? (RC2, RC3) | **A** a directive fault, read like a closure (no evidence), with its lie twin L11 proven through the compare path. **B** a deviation: compare the Destination with a board row to log "NOT ON THE BOARD", and deny with that evidence. **C** A without L11. |
| Q5 | Do the papers name a portal? | **A** no: the Destination is matched against the board, and the forms stay at six fields. **B** a "Portal" row on the manifest and the return order (the booked portal; a mismatch with the board is a second directive fault); the manifest is full, so one of its rows has to move. |
| Q6 | The routing rule's first day (RC6) | **A** day 2, and day 2's closure moves out (three faulty travellers in day 2's first half, as now; closures from day 3). **B** day 2, closure kept (four of day 2's first five travellers faulty). **C** day 6 (the board is information only on days 1-5). |
| Q7 | Closures and routes (RT4) | **A** a closure retunes: the Directorate runs its route on a portal set to a closed place, shown the night before; closed places are never on the board. **B** the board shows CLOSED on that portal's row, and no honest traveller is drawn there. **C** the portal stays tuned and the board shows nothing: the Directives' embargo line is a second check on travellers bound for a board route. |
| Q8 | What a route change costs (RT6) | **A** nothing, as often as wanted, until the shift ends. **B** a fee per changed portal (a knob, first cut 20 cr), charged at the change. **C** one change per portal per day. |
| Q9 | The Orders catalogue (OR2) | **A** every Home item plus the Portal 04 commission, in four branches drawn as a tree, no prerequisites, paid at order, cancellable the same day. **B** A with prerequisites (for example the Analysis Scanner needs the Auto-Feed; the translators need Interview Protocols). **C** A, but paid on delivery (charged the next morning; refused if the wallet cannot cover it). |
| Q10 | What stays at Home (OR7) | **A** expenses and care, the slot machine and sleep; the shop goes. **B** A, plus a "Deliveries tomorrow" line on the expenses panel. **C** a small Home shop for the translators only. |
| Q11 | The board in the hall (BD1-BD4) | **A** gameplay-drawn LED rows on the art's blank display (the open departure routes) and a hover tooltip with the whole schedule. **B** a text object the art scene carries (`Add Anime Hall Hooks` adds it; the art scene is edited once more). **C** a HUD chip listing the routes, not on the board. |
| Q12 | The rings (BD5) | **A** the gameplay tints each ring's glass through the art's `SetLayerTint`: dimmed when not in service, a pulse on a departure (needs the art side's OK). **B** no ring feedback. **C** the art side adds lit-glass layers and its own hook component. |
| Q13 | Demand per route (GN2) | **A** each route draws by its era's weight in the day plan. **B** every route draws equally. |
| Q14 | L11, the rerouted booking (RC7) | **A** from day 6, and every day 7-15. **B** from day 3, with that day's other record lies. **C** not at all. |

## 16. Intent audit

**Against Saleh's words.**
- *"there is now multiple portals and a destination board"*: the hall's five rings are five portals (PO1), and its board shows the day's routes (BD1-BD4).
- *"new portals will unlock"*: 01 and 02 on day 1, the Return Gate on day 5, 04 by order, 05 on day 9 (PO3); the rings and the app show what is not in service yet (PO6).
- *"the upgrade tree be in the pc now as another icon: you can order the upgrades and they arrive next day"*: the Orders icon, its catalogue in four branches, paid at order, delivered at the start of the next day (OR1-OR6).
- *"destination must match portal in the papers"*: the routing directive: the destination the papers and the claim name must be an open portal's route (RC1).
- *"destination board should also show it"*: the board lists every open departure route (BD3); its tooltip, the Rules memo and the app list the whole schedule.
- *"an icon that shows the portals and the destination ... will set the portal to an era and a location"*: the Portals icon, today's schedule, and two choosers per portal, Era then Location (PA1-PA3).
- *"lets try that"*: free changes (RT6), every knob in the sheet or the Inspector (§11), and a player who never opens the app gets the Directorate's schedule.

**Against his design rules.**
- Rule 1: one wrong-decision penalty for a routing fault or an L11 liar, accepted or denied; no route fee, no delivery fee, no portal fine (RC2).
- Rules 2 and 3: no new wheel entry, verb or kind-keyed line; the routing breaker and the L11 liar speak their personality's lines.
- Rule 4: every knob is in the sheet (`agencyPortals`, `dayPortals`, `days[]`) or an Inspector asset (`Upgrade_Portal04`, `DeskConfigSO`, `DesktopConfigSO`); the authored beats are booked, never drawn (RT7).
- Rule 5: the free warning, the premades' extra stability loss and the stability model are untouched.

**Redo or override checks.**
- Overrides P DK1's "six icons" (eight now) and P SC1's "two scanner upgrades in the Home shop" (in Orders now): Saleh's request.
- Overrides T §2.3's day-2 closure (moved out, RC6) and FEATURES' "closures from day 2": the recommended answer to Q6.
- Changes T's destination draw (a citizen's destination is a route, the kind drawn first: GN1-GN2). T K2's claim model (the claim is the destination) is kept.
- Changes L5's worksite maker (the other routes, RC8), the only change to an existing lie.
- Re-baselines every day of `cases.txt`, where D kept days 1-6 byte-identical (§13.2).
- Closures keep their rules but stop being a separate desk check for citizens: their violators are off the board (RC5). They stay a desk check for the displaced and become a planning constraint in the app (RT4). Q7 offers the alternatives.
- Reuses, never duplicates: the directive machinery (`TravelRuleSO`, `Directives`, `CaseFacts`, `ViolatorSlots`, `Directives.Roll`), `RecordLies` and `RecordMismatch`, the day-start snapshot pattern, `HomeEconomy.UpgradeCost` over `ShopPrices.Discounted`, `TimelineService.ActivateEffect`, `DayCycle`, `Mailbox`, `FormView` and `FormSpecSO`, the contract's fallback resolution, the prop tooltip, the art's `SetLayerTint`.

**Exploits checked.**
- Retuning mid-shift to create easy denials: impossible, the day's schedule is fixed (RT2).
- Setting every route on tomorrow's closures to fill the queue with violators: impossible, closures retune (RT4).
- Fewer routes for an easier day: impossible, every portal in service runs a route (PO4), and the fault count never depends on the routes (PO5).
- Serving places whose books the player knows: intended; that is skill.
- Steering history through the routes: intended ("your choices change history"); watched by BX2 and BX5.

**No-code-path checks.** The unlocks, routes, rule days, lie days, demand, prices, tints and window sizes are data or Inspector knobs. New code is limited to what data cannot express: the schedule's resolution and choices, the demand weight, the routing predicate and makers, L11's maker, the order log and delivery, the two windows, the board's text and binding, and the ring link.

## 17. The seams

| With | What this spec assumes or asks |
|---|---|
| T (traveller types) | §2.3's day-2 closure moves (RC6); §3's forms are unchanged (the Destination rows exist); §5.2's verdict table gains two rows (§6.4); §5.3's directives gain `PortalRouting`; §6.1's catalogue gains L11; §6.3's Destination maker (L5) draws among the routes. A change to one of those rows updates this spec. |
| P (the PC redesign) | DK1's count (eight icons); DK4's restore (a new id takes the first free spot, so the two new icons join saved layouts there); TH3's flavour labels (+ `icon.portals`, `icon.orders`: 30); SC1's shop (Orders); ML1's mail kinds (+ Delivery); FO9's PC pages (+ TC-970, TC-980); §2.9's Rules memo (the schedule section). The plan updates P's rows in the phase that changes them. |
| D (days 7-15, on `design/days-7-15`) | B2's citizen premades are booked (RT7); §3's days gain their routes (§10) and `Rerouted` in `lies`; `desk4_closes` reads "From today its queue and its portal (05, upper right) join Desk 3."; A4's era weights are the routes' demand; X5's contamination count is kept per place (BX3). If D's Q1 is answered A (no citizen premades), bookings are not built. `TransponderRecall` and `PortalRouting` both append to `TravelRuleType`: whichever lands second renumbers after the first. |
| Phase 23 (balance, on `redesign/p23-balance`) | `DayCycle` is the one day path (delivery joins `AdvanceNight`); the simulation builds the day's schedule through `BuildToday` (BX1); the thresholds stay phase 23's to set (BX4). |
| The art side | the board's display stays blank and text-free (§12.1); the rings' tint needs its OK (Q12); a renamed layer is a `DeskConfigSO` edit. |
| The personalities spec (S) | nothing: no kind-keyed line is added. |
