# Portals, the Departure Board and Orders: design

*2026-09-30 · drafted by Claude from Saleh's request of 2026-09-29 and his design rules of the same day, then revised the same day with his answers to Q1, Q2, Q6 and Q7 (§15.1 records them and where each is applied) · read-only map of `main` at `cdf10f7` (`E:\unity\NOPE-docs`, branch `design/portals`) and of `origin/design/days-7-15` at `24e7a8e` (the days 7-15 spec, **D** below, not on `main` yet) · the code wins over this text · the traveller-types spec (`2026-09-26-traveller-types-design.md`, **T**) owns the kinds, papers, lies, directives and verdicts; the PC redesign spec (`2026-09-26-pc-redesign-design.md`, **P**) owns the desktop, the windows and the forms; this spec owns the portals, the Departure Board, the routing directive and its lie, and the Orders app; §17 is the seam · built by `docs/superpowers/plans/2026-09-30-portals-plan.md` · every decision is recorded with its reason; §15.2's questions wait for Saleh, and the plan builds the recommended options*

Saleh, verbatim (2026-09-29):

> "there is now multiple portals and a destination board. new portals will unlock. we will have the upgrade tree be in the pc now as another icon: you can order the upgrades and they arrive next day. destination must match portal in the papers and destination board should also show it. you will have an icon that shows the portals and the destination. this icon will set the portal to an era and a location. lets try that"

His answers to the first draft (2026-09-30, relayed by the orchestrator):
- **Q1, who sets the routes:** the Directorate sets every route, for now: "I need to think about this system more; for now let's go with 3 knowing we are going to make it change". The Portals app shows each open portal's route (era and place) and does not set it; the data model stays ready for clerk-set routes (one named seam: SM1).
- **Q2, unlocks:** "One portal is open on day 1; the rest are under maintenance and you need to pay for them in the PC. They might break in the future again; we will figure it out in another pass." Breakdowns get a seam and no behaviour now (SM2).
- **Q6:** the routing rule starts on day 2, and day 2's closure moves to day 3.
- **Q7:** "Board shows CLOSED": a portal whose route is closed today stays on the board marked CLOSED, and travellers bound for it must be denied (a check the player applies).

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
| Destinations (`ContentLibrarySO.TodaysProfiles`; `CaseFactory.PickEraFromPlan`, then the kind draw, then `PickPlace`) | every place of the day's world (the plan's eras, weighted, × its countries; never the Future) | a 2150 citizen's destination is one of the day's open **routes**: the places the portals in service run, not closed today (GN2); the displaced keep the whole world through the Return Gate (PO2) |
| Closures (`TravelRuleSO` Era/Nation/NationEraForbidden, `DayPlanSO.ClaimAllowed`, one planned violator each in `PlanViolators`) | directive faults read on the Directives, from day 2 | unchanged as rules; a route a closure forbids stays on the board marked CLOSED, and the closure's violator is bound for it when it can be (RT4, RC5); day 2's closure moves to day 3 (RC6) |
| Directives (`Directives`, `CaseFacts`, `DirectiveFault`, `Faults`) | closures, the paper set, the debt standing, the papers' dates; dress, return home, no 2150 goods, the procedure lines | + `PortalRouting`: a citizen's destination must be on today's board (RC1) |
| Record lies (`RecordLies`: L1-L5, L10) | L5's worksite variant swaps in another open place | + **L11 `Rerouted`** (RC7); L5's swap names another open route (RC8) |
| The Destination field | TC-101 row 4, TC-415 and TC-417 row 3, TC-520 row 4 (Worksite), TC-610 row 4 (Origin), TC-630 row 3 (Return To); compared with the record's Booked departure or the registry's Origin | unchanged fields; read against the board (RC1) and still compared with the record (RC7) |
| The Home shop (`HomeManager.ShowShop`, `HandleBuyUpgrade`; `HomeUIController`'s shop panel and its `Paging`; `HomeSceneBuilder`'s `ShopPanel`) | eight upgrades: Auto-Feed Scanner 200 cr, Analysis Scanner 300, Diplomatic Contacts 350, Interview Protocols 120, four Speech translators 80; bought at night, in force from the next office day (the day-start `GateSnapshot`) | moves to the PC's **Orders** app: ordered during the shift, delivered at the start of the next one, so in force from the same office day as before; + the three portal repairs (OR1-OR10); Home keeps expenses, care, the slot machine and sleep |
| Upgrades (`UpgradeSO`: id, name, blurb, cost, unlock effect; `WorldState.unlockedUpgradeIds`; `ScannerDay` and `TranslationDay` read the day-start snapshot) | a flat list | + `branch` (the tree), + three portal repairs (OR6) |
| The desktop (`DesktopAppIds`; P DK1: "Six icons, nothing else, ever") | Investigation, Internet, Mail, Citizen Account, Notes, Settings | **eight**: + Portals (it shows the routes) and Orders (PA1, OR2). Saleh's request overrides DK1's count; "no icon is added at run time" still holds |
| The hall (`SCENE_CONTRACT_GAMEPLAY.md`; `OfficeAnchorId`, 23 values; `AnimeHallShiftLink` drives the art's `SetTime`) | the rings and the board are art only | anchor `DepartureBoard` (BD1); the board's rows drawn by the gameplay layer, a closed route marked CLOSED (BD2-BD4); each ring's glass tinted by its state and pulsed on a departure through the art's own `SetLayerTint` (BD5, with the art side's OK) |
| Mail (`Mailbox`, `MailKind`: 4 kinds) | the directive memo, the Times, citations, authored mail | + `Delivery` (OR5) |
| Saves (`WorldState`, save version 3) | | + `orders`, additive: the version stays 3 |

## 1. Decisions

### 1.1 The portals (PO)

| Id | Decision | Why |
|---|---|---|
| PO1 | **Five portals, one per ring of the art, numbered as the art numbers them**: 01 front, 02 rear left, 03 rear right, 04 upper left, 05 upper right. Content: `agency.portals[]` = `{number, name, role, fromDay, repair}` (§9.3). | The hall already shows five rings. Numbering them by the art's own layer names keeps the board, the app and the rings one list. |
| PO2 | **Two roles.** *Departures* (01, 02, 04, 05): each runs the route the Directorate sets for it that day (RT3). *Returns* (03, the **Return Gate**): the displaced go home through it, "tuned by their Return Order's incident"; it has no route and is never on the board. | A displaced person's destination is their origin, anywhere in the day's world (T K2). Routing them through the departure portals would deny people their way home for a missing repair, and would force every famous premade's authored place onto a route. The Return Order already carries the incident the gate tunes to, so the fiction costs no field. Q3. |
| PO3 | **Service (Saleh's Q2).** 01 is in service from day 1. **02, 04 and 05 start under maintenance**: each enters service at the start of the day after its **repair** is delivered (an Orders item, OR6; prices are Inspector knobs, first cuts 150, 250 and 350 cr). **03, the Return Gate, is repaired by the Directorate for day 5**, when the displaced arrive (T K3); it is not for sale (Q3). A portal row may carry both `fromDay` and `repair`: it is then in service from whichever comes first. | "One portal is open on day 1; the rest are under maintenance and you need to pay for them in the PC." The Return Gate is the exception because the displaced must get home whatever the clerk's wallet: a Return Gate the clerk had to buy would either hide days 5-15's displaced and famous travellers from a player who did not buy it, or turn every displaced traveller into a denial with no evidence needed, which is free money. Q3 offers the alternatives. |
| PO4 | **Desk 4's portal.** D's day 9 (`desk4_closes`: Desk 4 closes and its queue joins Desk 3) now also says that its portal, 05, passes to Desk 3 still under maintenance. Unrepaired, nothing on day 9 depends on it: the day's beat (Gutenberg) is a famous displaced person and leaves through the Return Gate, and the queue's two extra travellers go to the open routes like everyone else. | Saleh's rule makes 05 a repair like 02 and 04; the story keeps its line, and no beat needs a route a player may not have repaired (GN5). |
| PO5 | **A departure portal in service always runs its route.** There is no "off". | An idle portal would only shrink the board; the Directorate's schedule names a route for every departure portal every day (CN2). |
| PO6 | **What a repair buys**, for now: one more route on the board a day, so travellers spread over more destinations and history lands on more places. It never changes the queue size, the pay or how many travellers are faulty (RC4). **Flagged:** that is variety, not a reward, so a player has little reason to pay; Saleh's next pass on portals decides what an open portal earns (Q15). | The queue and the faults are authored per day, and money rules are phase 23's; this spec does not invent an economy for a system Saleh has said he will rethink. |
| PO7 | **Under maintenance** shows everywhere the portals do: the ring dimmed (BD5), the board without its row (BD3), the tooltip and the Portals app with "Under maintenance · repair it in Orders (150 cr)", or for the Return Gate before day 5 "Under maintenance · back in service on day 5". | "new portals will unlock": the player can see what is coming and what it costs. |

### 1.2 Routes: the schedule (RT)

| Id | Decision | Why |
|---|---|---|
| RT1 | **A route is a place** (a country × an era) of the day's world, never the present or the Future, keyed `country:era` (`PlaceKey`), the pair `rules[]` already names places by. | "set the portal to an era and a location": an era and a country name exactly one of the 40 places. |
| RT2 | **The day's schedule is fixed at the day's start** (`PortalDay`, built with today's world in `ContentLibrarySO.BuildToday`, the pattern of `ScannerDay` and `TranslationDay`) and holds all day: the board, the tooltip, the Portals app, the Rules memo, the generator, the verdicts and the rings read that one object. A repair delivered at the night's turn counts from that day's start. Travellers already queued never change. | The whole queue is generated at the day's start (`CaseFactory.GenerateDayCases`); a schedule that moved during the shift could turn an honest queued traveller into a fault. |
| RT3 | **The Directorate sets every route** (Saleh's Q1, "for now"): each day of `days[]` names the route of each departure portal (`days[].portals[]` = `{portal, country, era}`), and a portal in service runs its route. **Resolution** (Domain `PortalSchedule.Resolve`, pure, no draw): each portal's state (in service, or under maintenance; the Return Gate), each in-service departure portal's route, and whether a closure forbids it today. | Saleh's answer. Content carries the routes, so Saleh edits them in the sheet (rule 4), and the resolution is a lookup any test can pin. |
| RT4 | **Closures and the board** (Saleh's Q7): a route a closure forbids today stays on the board, marked **CLOSED**, and travellers bound for it must be denied: the closure's directive fault, read on the board or in the Directives (RC5). | "Board shows CLOSED": the player applies the check. |
| RT5 | **There is always an open route**: every day, each departure portal in service by its day alone (01) runs a route that is in the day's world and open (a content check). The routes of 02, 04 and 05 may be closed on purpose. | A player who repaired nothing must still have somewhere to send honest citizens; the closures' lesson lives on the routes a player chose to repair. |
| RT6 | **A day's routes are distinct.** | Two portals on one place add nothing and would split a closure's mark across two rows. |

### 1.3 The Portals app (PA)

| Id | Decision | Why |
|---|---|---|
| PA1 | **A desktop icon, Portals** (`portals`), and its window: the form TC-970 "Portal Schedule", **read-only**. The default icon order becomes Investigation, Portals, Internet, Mail, Citizen Account, Orders, Notes, Settings (`DesktopAppIds.DefaultOrder`); a saved layout gains the two new icons in the first free spots (P DK4). | "you will have an icon that shows the portals and the destination"; setting routes waits for Saleh's next pass (Q1, SM1). The work apps come first; Orders sits beside the account it spends from. |
| PA2 | **What it shows** (§4): one row per portal: number, ring, **era** and **place** of its route, and its state: In service; CLOSED today (with the closure's line); Under maintenance (with the repair's price and "repair it in Orders", or "in transit: in service tomorrow" while the repair is on its way); Return Gate. The footer: "Routes are set by the Directorate." | Saleh: the app shows "each open portal's route (era and place)". The era and the place in two columns keep his words; the state column is what the player acts on (repair, deny). |
| PA3 | The window opens restored at 880 × 600 u (a `DesktopConfigSO` knob) and can be maximised; the form scrolls (P AP7) and is reached with the keyboard like any page (P KB4). Themed chrome, an unthemed form (P TH1). | The PC's window and accessibility rules. |

### 1.4 The Departure Board (BD)

| Id | Decision | Why |
|---|---|---|
| BD1 | **New anchor `DepartureBoard`** (`OfficeAnchorId`, appended as 23). Fallback: the bare name `16 Departure board blank display` (the hall's sprite; its bounds are the place). The 3D room has no board: the anchor is missing there, the board is skipped, and the PC shows the schedule. | The contract's own pattern (a bare-name fallback whose renderer bounds are the place). The art scene is not edited. |
| BD2 | **The rows are drawn by the gameplay layer**: a world-space TextMeshPro built by Build Office UI in `OfficeGameplay` (sorting layer `Gameplay`), fitted each frame to the anchor's bounds inset by `DeskConfigSO.hallBoardInset`, so it follows the display if the art pans. Ink: `hallBoardInk` (FFF2D9, the ivory the hall's other glasses use); the CLOSED mark in `hallBoardClosedInk`. | The gameplay owns the text and its layout. The contract places gameplay objects by world position and never parents them into the art. The readouts' texts live in the art scene only because the art's preview texts had to be replaced there; the board has none. |
| BD3 | **What it shows**: one row per departure portal in service, in number order, "01  PERICLEAN ATHENS (ANCIENT)": the papers' own label, upper case; a closed route's row ends with "CLOSED". No Return Gate row, no row for a portal under maintenance, no header. | The board answers the two questions the desk asks of it (is this destination on the board? is it closed today?) with the largest text the display allows: 1 to 4 rows are about 27 to 107 px tall at 1080p and 18 to 71 px at 720p. |
| BD4 | **The board is a reacting prop**: a click box on `Interactable` over it (the hover outline), whose tooltip lists every portal in UI text (its route, CLOSED, under maintenance, the Return Gate). The tooltip grows to fit its lines. Showing it while a traveller is at the desk counts as viewing the rules for the `rules` step (P ST3). | A long label ("Ottoman Iraq (Baghdad Vilayet) (Industrial)  CLOSED") shrinks on the board below the 720p floor. The tooltip is the readable surface in the office; the PC is the full one. |
| BD5 | **The rings** (with the art side's OK, Q12): an `AnimeHallPortalLink` (added by the binder beside `AnimeHallShiftLink`) tints each ring's painted-glass layer through the art's own `AnimeHallPresentation.SetLayerTint`: the art's colour in service, `hallPortalClosedTint` when its route is closed today, `hallPortalIdleTint` (a 45 % grey) under maintenance; and a pulse (`hallPortalPulseTint` for `hallPortalPulseSeconds`, 0.8 s; one step with reduced motion) on the ring an accepted traveller leaves through: their route's portal, or the Return Gate for the displaced, and none for a traveller no open route serves. The glass layer ids are a `DeskConfigSO` list (`hallPortalGlassLayers`, in portal order). | The hall shows which rings work and where a traveller went, through a hook the art already offers and with no new art. It changes the art's look, so the art side decides; without its OK, BD5 is skipped and nothing else changes. |
| BD6 | The board, the tooltip, the app and the rings read the day's `PortalDay` (RT2). | They can never disagree with the verdicts. |

### 1.5 The routing check (RC)

| Id | Decision | Why |
|---|---|---|
| RC1 | **A new standing procedure, `TravelRuleType.PortalRouting`** (appended), content `Rule_PortalRouting`: kinds RichTourist, PoorTourist and Labourer; its line "Departures leave only through the portals on today's Departure Board: a citizen's destination must be on the board, and not CLOSED." Predicate: `CaseFacts.UnservedDestination` (the claim is not the route of a departure portal in service, closed or not) gives `DirectiveFault.NoOpenPortal` (appended; runtime only). | "destination must match portal in the papers": the destination the papers and the claim name must be on the board. A closed route is on the board, so the closure reads it (RC5): one rule per fact. A rule asset, a Domain predicate and a directive fault is how every destination rule already works (T P3). |
| RC2 | **A directive fault**: read on the board, denied with no evidence. A wrong accept's citation is "Approved a departure no portal serves." (`Faults.Unserved`, "unserved"; `citation.acceptedWrong.unserved`) at the one wrong-decision penalty, after the day's free warning; denying an honest traveller costs the same one penalty. No portal-specific fine and no extra stability loss. | It is the same kind of fact as a closure (a daily list the papers are read against), and the closures are directive faults (T P1). Rule 1. |
| RC3 | **Not a compare**: board rows are not compare-pickable. The compare path carries the lie (RC7). | A MISMATCH against one row would mislead when another row carries the place; a MATCH against the whole board would do the glance for the player. The closures are read the same way. Q4. |
| RC4 | **Makers.** Its first day plans one breaker in the first half of the queue (T P4, `Directives.HasMaker`): the slot's place is drawn on the violator stream among the day's open places that are not on the board (the closures' pattern), and its kind among the rule's kinds. On later days the violation roll may break it (`Directives.IsRolled`) for a citizen when an open place of their destination's era is off the board: the maker swaps the destination for one (one Range draw on the fault stream) before the papers print. | The guarantee teaches the rule on its first day; the roll keeps it alive. Keeping the era keeps the labourer's registered contract (its employer is the era's) and the era's small talk right. |
| RC5 | **Closures on the board** (Saleh's Q7). A citizen bound for a CLOSED route is on the board, so only the closure breaks: denied with no evidence, reason "closed". A citizen bound for a closed place off the board breaks both rules; `Directives.Fault` reads the day's rules in order and closures are listed first, so the reason is "closed" (one fault source, T K5). **The closure's planned violator** is bound for a CLOSED route on today's board when one exists (drawn among the closure's forbidden places that are routes of portals in service, on the violator stream), else for any forbidden place as today. **Random citizens are never drawn to a CLOSED route.** The displaced are read by the closures only, as today. | The CLOSED mark becomes a check the player applies, as Saleh asked, and the guaranteed violator is where the lesson shows. Drawing honest citizens to a closed route at its share would make a third or a half of the queue violators; one per closure is T P4's number. |
| RC6 | **First day: day 2** (Saleh's Q6), with its guaranteed breaker; **day 2's closure (`Rule_NoAncientEgypt`, New Kingdom Egypt) moves to day 3**, which then lists three closures. Day 2 plans three faulty travellers in its five first-half slots (routing, the tourists' paper set, a costume error). **Flagged:** day 3 then plans five in its six first-half slots (three closures, the labourers' paper set, the debt standing); the room check (D V10) passes, and dropping one of day 3's closures is a sheet edit if it plays heavy. | Saleh's answer. The board appears on day 1 with one route and every traveller served, so T's "no directive can be broken on day 1" holds. |
| RC7 | **L11 `Rerouted`** (`LieKind`, appended; a record lie): a citizen whose Citizen Account books a place that is not on the board today travels on papers that name an open route (the claim). The papers agree with each other and with the board; the record's Booked departure disproves them. Proof: the compare path, a Destination field (the visa's, the contract's Worksite, a proof of means') against the record's Booked departure: a RecordMismatch, DEVIATION LOGGED. The Destination's smart link already jumps to that row (P LK1). The booked place is one Range draw on the lie stream among today's places of the claim's era that are not on the board (closed ones included); if there is none, the lie cannot show (no draw). Enabled from day 6 (`days[].lies`), and on every day 7-15. | "a mismatch as a new lie": the board alone must not be enough, and the account is where a booking lives. Keeping the era keeps the labourer's contract right. Day 6 is the first week's lightest day for new material (one new lie). Q14. |
| RC8 | **L5's worksite variant draws among the day's other open routes**, not every open place; with only one open route the variant cannot show (L5's other variants still can). | A forged worksite off the board, or on a CLOSED row, would look like a directive fault; a player who denied it without evidence would take an unproven denial for reading the board right. |
| RC9 | **The verdict table** (T §5.2) gains two rows (§6.4). | |

### 1.6 The generator (GN)

| Id | Decision | Why |
|---|---|---|
| GN1 | **The kind is drawn before the destination.** The case stream keeps its three draws per ordinary traveller (kind, era, place), in a new order. | A citizen's destinations are the day's open routes and a displaced person's the whole world, so the kind decides which list the destination comes from. |
| GN2 | **A citizen's destination** is drawn among today's open routes (portals in service, not closed): the era by the plan's era weight × the number of open routes in that era, then one open route of that era uniformly, so each route draws in proportion to its era's weight. **The displaced** draw as today, over the day's world. | The day plans' era weights keep their meaning (D A4's busy Industrial and Modern eras become busy routes), and two draws keep the stream's count. Q13. |
| GN3 | **Planned faulty slots** claim their planned place: a closure's violator a CLOSED route when there is one (RC5), else a forbidden place; the routing breaker an open place off the board (RC4); a planned liar or procedure breaker, as today, a place open that day, which for a citizen means an open route. | One fault per traveller (T K5); phase 9's fix (a planned slot claims an open place) carries over. |
| GN4 | **The books, the Costume Guide and the registry keep the whole day's world.** | The displaced's origins, a false origin's tells and the foreign-origin proofs name any place of the day. |
| GN5 | **Premades**: a famous (displaced) premade leaves through the Return Gate, so no route is needed and the pool is unchanged. A citizen premade (D B2) is bound for its authored place, which the Directorate's schedule makes **portal 01's route that day**, open (a content check, PR6). | Authored cases stay outside the draws (rule 4) and keep their authored right calls (Ines accepted, Callum denied for his frozen account) whatever the player repaired, with no booking mechanism: the Directorate already sets every route. |
| GN6 | **No new stream.** The schedule draws nothing; the routing breaker's place and the closure violator's CLOSED route are on the violator stream, the rolled swap on the fault stream, L11's booking on the lie stream. | House rules: a new random concern gets a new salt; none of these is a new concern. |

### 1.7 Orders (OR)

| Id | Decision | Why |
|---|---|---|
| OR1 | **Everything the Home shop sold moves to the PC**: the Auto-Feed Scanner, the Analysis Scanner, Interview Protocols, Diplomatic Contacts and the four Speech translators, with the same ids, prices and effects, plus the three portal repairs. | "we will have the upgrade tree be in the pc now as another icon"; "you need to pay for them in the PC". Keeping the ids keeps every save's owned upgrades. |
| OR2 | **The Orders icon** (`orders`) and window: the form TC-980 "Requisition · Temporal Customs Supply", the catalogue as a tree of four branches (Desk equipment, Interview, Portals, Contacts), each item a row: its icon, name, blurb, price, and its state with one button. `UpgradeSO.branch` (a new serialized enum, `UpgradeBranch`); Generate World writes Interview on the translators. No prerequisites. | "the upgrade tree": branches read as a tree without inventing requirements the balance never had (P SC2: the two scanners combine, and neither is a tier). Q9. |
| OR3 | **Ordering.** "Order" charges the price at once (the one price rule, `HomeEconomy.UpgradeCost` over `ShopPrices.Discounted`: the price shown is the price charged) and marks the item "In transit · arrives day N+1". "Cancel" refunds it, the same day only. An item owned or in transit cannot be ordered; one the wallet cannot cover shows "Not enough cr". | Paying at order is today's shop rule; Cancel is the undo for a misclick. |
| OR4 | **Delivery at the start of the next day**: in `DayCycle.AdvanceNight`, after the day turns, every order placed before the new day is delivered: the upgrade is owned (`WorldState.UnlockUpgrade`), its unlock effect is activated from the new day (as `HandleBuyUpgrade` did), and the order is marked delivered. It is then in the day-start snapshot, so every upgrade is in force from the same office day a Home purchase was, and a repaired portal is in service that day. | "they arrive next day". The game and the balance simulation share `DayCycle`. |
| OR5 | **A delivery notice** in Mail (`MailKind.Delivery`, appended): one memo per delivery day listing what arrived, regenerated from the order log by id (P ML2). | The player learns what arrived without a new screen. |
| OR6 | **The portal repairs** (`Upgrade_RepairPortal02.asset`, `…04`, `…05`: ids `repair_portal_02`, `repair_portal_04`, `repair_portal_05`; 150, 250 and 350 cr; branch Portals; no effect): a delivered repair puts its portal in service (PO3). Each price is an Inspector knob. | Saleh: "you need to pay for them in the PC". Rising prices make the three a progression; the numbers are first cuts for phase 23. |
| OR7 | **Home keeps** the expenses, family care, the slot machine and sleep (Expenses → Slot → Sleep). The shop panel, `HomeManager.ShowShop` and `HandleBuyUpgrade`, `HomeUIController`'s shop and its page row, `Paging` and `PagingTests` retire (the shop is their last caller). | No dead code. The shop's one live rule, the price, moves with the catalogue. Q10. |
| OR8 | **The clerk's statement** (TC-960): an order joins the day's purchases when placed, and a cancellation takes it out. The **order log** is `WorldState.orders` (additive; a cancelled order is removed). | The statement already has a purchases column. |
| OR9 | **The translators' notice** (day 4's paper): "Speech translators are sold at Home tonight" becomes "Order a Speech translator in Orders today and it arrives tomorrow." | The timing is unchanged: a translator ordered on day 4 arrives on day 5 with the displaced, so T I3's reason for the notice's day holds. |
| OR10 | The debug panel's "unlock upgrade" stays immediate (so it can also repair a portal for testing). | A development tool, not the game's economy. |

### 1.8 Content (CN)

| Id | Decision | Why |
|---|---|---|
| CN1 | `agency.portals[]` and `days[].portals[]` are row-shaped for the content spreadsheet (T §15's row rules), with `ContentSheetMap` entries and the template rewritten. | Rule 4: Saleh edits the service rules and the routes in the sheet. |
| CN2 | **The Directorate's routes** (§10): every day names one route for each departure portal (01, 02, 04, 05), distinct, each in that day's world; 01's route is open that day (RT5); with D, a citizen beat's place is 01's route on its day (GN5). The routes of 02, 04 and 05 may be closed on purpose (RT4). | Every repaired portal always has a route, and the closures' lesson is authored where a repairing player will see it. |
| CN3 | **The morning notices** (the `news` block): `portals` ("Today's departures: {routes}.", a closed route written "{place} (closed today)") and `portalInService` ("Portal {number} ({name}) is in service from today."), for a repair's delivery and for the Return Gate on day 5. | The paper already carries the day's notices; the player reads the day's board before the first traveller. |

### 1.9 The seams for Saleh's next pass (SM)

| Id | Seam | What a later pass adds there | What stays unchanged |
|---|---|---|---|
| SM1 | **The route-request seam** (clerk-set routes; Saleh: "knowing we are going to make it change"). `PortalSchedule.Resolve` takes the day's **route requests** (portal number → place); today `ContentLibrarySO.BuildToday` fills them from the day plan's Directorate routes (`DayPlanSO.directorateRoutes`) and nothing else. | The clerk's requests, read before the Directorate's: a `WorldState` store of the clerk's routes, the Portals app's Era and Location choosers, and the rules for a clerk route the Directorate cannot run (outside the day's world, taken, or closed). The first draft of this spec designed them (git: this file at `097f20c`, RT3-RT8 and PA1-PA6 there). | Everything downstream reads the resolved `PortalDay`: the board, the app's form, the generator, the directive, the verdicts, the rings. |
| SM2 | **The service-state seam** (breakdowns; Saleh: "They might break in the future again; we will figure it out in another pass"). `PortalSchedule.InService(portal, day, repaired)` is the one place a portal's state is decided; today it reads the portal's first day and whether its repair was delivered. | A breakdown rule there, a record of breakdowns in `WorldState`, and a repair that can be ordered again (`Orders.StateOf` reads what is owned: a breakdown pass takes the repair back off that list, or keys repairs by breakdown). | The schedule's resolution, the board, the generator and the verdicts. |

## 2. The portals over the run

| Day | Departure portals in service | Return Gate | What is new at the desk |
|---|---|---|---|
| 1 | 01 (Periclean Athens); 02, 04, 05 under maintenance | under maintenance | the board, the Portals app, Orders (the three repairs among its items); every citizen served |
| 2 | 01 (+ each portal whose repair was delivered) | under maintenance | the routing directive and its guaranteed breaker; no closure |
| 3 | as day 2 | under maintenance | the closures begin (three: New Kingdom Egypt, moved from day 2, Northern Song Kaifeng, Tokugawa Edo); 02's route is New Kingdom Egypt, CLOSED on the board for a player who repaired 02 |
| 4 | as day 2 | under maintenance | |
| 5 | as day 2 | in service (the Directorate's repair) | the displaced, through the Return Gate |
| 6 | as day 2 | in service | L11 (the rerouted booking) |
| 7-15 | as day 2; with D, 01 runs each citizen beat's place on its day | in service | day 9: Desk 4's queue joins, and its portal 05 is Desk 3's, still under maintenance unless repaired (PO4) |

A careful player can afford portal 02's repair on day 2 (the run starts with 50 cr, and a perfect wallet grows about 110 cr a day, D E4); ordered on day 2, it is in service from day 3.

## 3. The schedule: resolution, worked

`PortalSchedule.Resolve(portals, day, repaired, requests, closed)`:

1. Each portal's state: in service when its `fromDay` is reached or its `repair` is owned at the day's start (after the night's deliveries: `InService`, SM2); otherwise under maintenance. The Returns portal has a state and no route.
2. Each departure portal in service runs its request (today, the Directorate's route for it, SM1), marked closed when a closure of the day forbids it.
3. **On the board**: the in-service departure portals' routes, closed or not. **Open routes**: those not closed. `OnBoard`, `IsOpenRoute`, `PortalFor` and the lists are what the board, the generator, the predicate and the rings read.

**Day 3, worked.** The world: Ancient, Medieval and Early modern × all eight countries. Closures: New Kingdom Egypt (moved from day 2), Northern Song Kaifeng, Tokugawa Edo. The Directorate's routes: 01 Elizabethan England, 02 New Kingdom Egypt, 04 Abbasid Baghdad, 05 Late Ming Suzhou. This player repaired 02 on day 2.
- In service: 01 and 02. Under maintenance: 03, 04, 05.
- The board: `01  ELIZABETHAN ENGLAND (EARLY MODERN)` / `02  NEW KINGDOM EGYPT (ANCIENT)  CLOSED`.
- Honest citizens all go to Elizabethan England (the one open route). The New Kingdom Egypt closure's planned violator is bound for New Kingdom Egypt, a CLOSED row on the board; the other two closures' violators are bound for places off the board. The displaced do not arrive until day 5.
- The notice: "Today's departures: 01 Elizabethan England (Early modern), 02 New Kingdom Egypt (Ancient) (closed today)."
- A player who repaired nothing sees one row, `01  ELIZABETHAN ENGLAND (EARLY MODERN)`, and meets all three closures' violators off the board.

## 4. The Portals app

```
╔═ PRT Portals ═══════════════════════════════════════════════════════════════════[_][□][X]╗
║ ┌─ TC-970 ─────────────────────────────────────────────────────────────────────────────┐ ║
║ │ (seal) TEMPORAL CUSTOMS · Debt Relief Departures                          TC-970      │ ║
║ │ PORTAL SCHEDULE · DESK 3                                   16 MAR 2150 · DAY 3        │ ║
║ │ No.  Ring         Era            Place                 State                          │ ║
║ │ 01   Front        Early modern   Elizabethan England   In service                     │ ║
║ │ 02   Rear left    Ancient        New Kingdom Egypt     CLOSED today: Embargo: no       │ ║
║ │                                                        travel to New Kingdom Egypt.   │ ║
║ │ 03   Rear right   –              Return Gate           Under maintenance · back in    │ ║
║ │                                                        service on day 5               │ ║
║ │ 04   Upper left   –              –                     Under maintenance · repair it  │ ║
║ │                                                        in Orders (250 cr)             │ ║
║ │ 05   Upper right  –              –                     Repair in transit · in service │ ║
║ │                                                        tomorrow                       │ ║
║ │ ──────────────────────────────────────────────────────────────────────────────────── │ ║
║ │ Routes are set by the Directorate.   Issued by Temporal Customs · Desk 3  ║║│║║ …   │ ║
║ └──────────────────────────────────────────────────────────────────────────────────────┘ ║
╚══════════════════════════════════════════════════════════════════════════════════════════╝
```

- The form is drawn by `FormView` from a `FormSpecSO` (TC-970), like every PC page (P FO9), from the day's `PortalDay` and the order log.
- A portal under maintenance shows no era or place: it runs nothing today. Its Directorate route becomes visible the day it enters service.
- The app works with or without a traveller at the desk. Its rows are not search entries: the Investigation app's Rules tab carries the day's schedule for search (§6.6).

## 5. The Departure Board and the rings

### 5.1 The board

| Part | Rule |
|---|---|
| Anchor | `DepartureBoard`: `Anchor_DepartureBoard`, else the bare name `16 Departure board blank display` (its sprite's bounds). No default pose: missing in the 3D room. |
| Text | `BoardRows` (world-space TMP, `Gameplay` sorting layer, built in `OfficeGameplay` by Build Office UI; auto-sized, every row the same size), its rect the anchor's bounds inset by `DeskConfigSO.hallBoardInset` (first cut 8 % a side), re-fitted in `LateUpdate` when the bounds move. Ink `hallBoardInk` (FFF2D9); the CLOSED mark in `hallBoardClosedInk` (rich text). Text: `PortalBoardText.Rows(PortalDay, label)` (Visuals, pure). |
| Click box | on `Interactable` over the bounds: hover outline, tooltip `PortalBoardText.Tooltip(PortalDay, label)`: "DEPARTURE BOARD" then one line per portal ("01 Front: Elizabethan England (Early modern)", "02 Rear left: New Kingdom Egypt (Ancient), CLOSED today", "03 Rear right: Return Gate, under maintenance until day 5", "04 Upper left: under maintenance"). The tooltip template grows with its lines (a content size fitter), checked at 720p. |
| Evening | The text is lit: the hall's evening dims the art, not the board. The display darkens, so the contrast only rises; the probe measures both inks at the full evening. |
| The claim strip | The HUD's claim strip covers the display's top few pixels at 1080p; the inset keeps the rows below it, and the probe checks both resolutions. |

### 5.2 The rings (BD5, with the art side's OK)

`AnimeHallPortalLink` (gameplay, added by the binder to its own object when the art office carries an `AnimeHallPresentation`, as the shift link is): at the day's start it sets each ring's glass (`hallPortalGlassLayers[number - 1]`, first cut `53 Portal 01 front painted glass` to `57 Portal 05 upper right painted glass`) to white (the art's own colour) in service, `hallPortalClosedTint` when its route is closed today, and `hallPortalIdleTint` under maintenance. On an accept it pulses the traveller's portal (`PortalDay.PortalFor(claim)`; the Return Gate for the displaced; none for a destination off the board or closed) to `hallPortalPulseTint` and back over `hallPortalPulseSeconds`; with reduced motion, one step up and one down. `SetLayerTint` sets the sprite's colour, which multiplies with the lighting the art applies through its property block, so the evening still darkens the rings.

### 5.3 The scene contract's new rows (`docs/SCENE_CONTRACT_GAMEPLAY.md`, in plan phase PR5)

- The anchors table: `DepartureBoard` | the Departure Board: its rows (a gameplay text) and its click box with the schedule's tooltip | no fallback in the room | `Anchor_DepartureBoard` over a board's display.
- The hall's table: `DepartureBoard` | `16 Departure board blank display` | the display's sprite bounds; the rows sit inset below the claim strip.
- The hooks section: "The gameplay layer drives **time** and, with the art side's OK, **the portal rings' glass**: `AnimeHallPortalLink` calls `SetLayerTint` on the layers named in `DeskConfigSO.hallPortalGlassLayers` (dimmed under maintenance, tinted when the route is closed, a pulse on a departure). A renamed layer is a knob edit."

## 6. The routing check

### 6.1 The rule

| Part | Rule |
|---|---|
| Type | `TravelRuleType.PortalRouting`, appended after the last member on `main` at build time (`DebtStanding` = 9 today; after D's `TransponderRecall` if that lands first); pinned in `SerializedEnumsTests` |
| Asset | `Rule_PortalRouting`: a `rules[]` row, `kinds` RichTourist, PoorTourist, Labourer; no place; the line of RC1 |
| Days | listed from day 2 on, after the day's closures |
| Predicate | `Directives.FaultOf(PortalRouting, facts)` = `NoOpenPortal` when `facts.UnservedDestination`, else None; the kinds filter is `Directive.AppliesTo`. `CaseFactory.Facts` sets `UnservedDestination = !portalDay.OnBoard(claim)` |
| Fault | `DirectiveFault.NoOpenPortal` (appended), `Faults.Unserved` ("unserved"), `citation.acceptedWrong.unserved` "Approved a departure no portal serves." |
| Guarantee | `Directives.HasMaker(PortalRouting)`: its first day plans one breaker (RC4) |
| Roll | `Directives.IsRolled(PortalRouting)`: from its second day, among the rolled procedures the traveller can break (`CanBreak`: a citizen kind; the caller adds "an open place of the destination's era is off the board") |
| Content checks | `Directives.RuleProblems`: a routing rule lists kinds and never `Displaced` (the Return Gate serves them). `PortalSchedule.DayProblems`: on its first day, at least one open place of the day's world is off the board even with every portal repaired (else the guarantee could plan none: a warning) |

### 6.2 Where the desk reads it

The board (its rows and CLOSED marks) and its tooltip, the Investigation app's Rules tab (the memo's schedule, §6.6), the Portals app, the morning notice and the Directives lines. The destination itself is on the claim strip, the primary form and the record's Travel group.

### 6.3 L11, the rerouted booking

| Part | Rule |
|---|---|
| Kinds | RichTourist, PoorTourist, Labourer (`LieKinds.AppliesTo`); a record lie (`LieKinds.IsRecordLie`) |
| What is false | The account's **Booked departure** (`CitizenAccount.BookedDeparture`, new: the booked place's label when it differs from the claim; empty means the claim) is a place of the claim's era that is not on the board today; the papers print the claim, an open route |
| The draw | on the lie stream in `RecordLies.Plan`: one variant (no variant draw), then the booked place (one Range draw over `RecordLieContext.OffBoardInEra`, new); NoPossibleLie, with no draw, when that list is empty |
| Tells | every Destination field on the papers (`recordTells`: TC-101's; TC-415's or TC-417's; TC-520's Worksite); the papers are not rewritten, the record is what differs |
| Proof | a Destination field against the record's Booked departure: `DiscrepancyLog.Prove`'s RecordMismatch, unchanged; the report line reads like "DESTINATION: the papers say Republican Rome (Ancient); the Citizen Account books Babylonia (Ancient)" |
| Not proof | the Analysis Scanner (papers against papers) finds nothing: the papers agree (P SC5 holds) |
| Look, claim, impacts | the claim's (unchanged code): dressed for the route, the claim line names it, an accept's impacts land on it |
| Days | `days[].lies` gains `Rerouted` from day 6; D's days 7-15 list it every day |

### 6.4 The verdict table's new rows (T §5.2)

| Traveller | Accept | Deny, ≥1 deviation logged | Deny, none logged |
|---|---|---|---|
| Destination off the board (honest papers; a directive fault) | wrong: "Approved a departure no portal serves." | impossible (nothing false to log) | correct |
| L11 rerouted (a deviation fault) | wrong: "Approved forged papers." | correct (the Destination × Booked departure deviation) | wrong: an unproven denial |

A citizen bound for a CLOSED route is the closure's existing row ("Approved a closed destination."). Every wrong call costs the one penalty (rule 1). A wrongly accepted traveller's impacts land on their destination, as a closure violator's do today (unchanged code); their ring does not pulse (BD5).

### 6.5 Closures and routing on one day

| Traveller | Board | Rules broken | Reason on a wrong accept |
|---|---|---|---|
| A citizen bound for a CLOSED route (the closure's violator when the board has one) | on the board, marked CLOSED | the closure | "closed" |
| A citizen bound for a closed place off the board | not on the board | the closure and routing (closures first) | "closed" |
| A citizen bound for an open place off the board (the routing breaker) | not on the board | routing | "unserved" |
| A displaced person bound for a closed origin | the board does not apply (the Return Gate) | the closure | "closed" |
| An L11 citizen | on the board, open | – (a record lie) | "forged" |

### 6.6 The Investigation app and the rest of the PC

- **Rules** (TC-940, the Directive Memo): a new section "Portal schedule" (a table: No., Ring, Era, Place, State) after the numbered directives. Each route row links to its place's row in the first reference book (the closures' existing ↗, P LK1). The rows are search entries of the day layer (P SE2), keyed `rule:portal:{n}`. Not compare-pickable (RC3).
- **Mail**: the day's directive memo is built by the same memo builder, so it carries the schedule.
- **Steps** (P ST): `steps.rules` ("Today's rules for the destination") becomes "Today's rules and the Departure Board for the destination"; the board's tooltip counts as viewing the rules (BD4).
- **Records**: the Travel group's Booked departure row prints `BookedDeparture` when set (L11), else the claim, as today.

## 7. The case generator

### 7.1 One ordinary traveller, in order (the changes in bold)

1. The forced blueprint, the premade (forced, or rolled from the pool), the planned slot (a closure's violator place, **a CLOSED route when there is one**; **the routing rule's** violator place; a planned liar or procedure breaker): otherwise unchanged.
2. **The kind** (one weighted draw over the day's kinds on the case stream; `TravellerKinds.PickWeight` and its filters unchanged). *Moved before the destination.*
3. The destination: a premade's authored place; a planned violator's place; else **for a citizen, the era over the open routes** (weight = the plan's era weight × the open routes in that era) **and an open route of that era**; for the displaced, the era over the world and a place of that era, as today.
4. Everything after (the name, the birth date, the look, the account, the lie roll, the violation roll with **the routing swap**, the costume roll, the papers, **L11's booking**, the directive fault read over `CaseFacts`): as today.

### 7.2 Demand, worked

Day 8 (D §3: Industrial 2, the other eras 1) for a player who repaired 02 and 05, so the open routes are 01 Victorian Britain (Industrial), 02 Wilhelmine Germany (Industrial) and 05 Abbasid Baghdad (Medieval): Industrial weighs 2 × 2 = 4 and Medieval 1 × 1 = 1, so a citizen goes to an Industrial route 4 times in 5, each of the two 2 times in 5, and to Abbasid Baghdad 1 time in 5: the routes draw 2 : 2 : 1, their eras' weights. `PortalDraw.EraWeight(planWeight, openRoutesInEra)` is the one rule, tested. With only 01 open, every citizen goes to Victorian Britain.

### 7.3 Streams

| Stream | Draws | Change |
|---|---|---|
| Case (`Seeds.ForCase`) | kind, era, place, then as today | the order: the kind first; a citizen's era and place over the open routes |
| Violators (`Seeds.ForViolators`) | the slots, then each planned rule's maker in plan order | a closure's place among its CLOSED routes first; + the routing rule's first-day place (the closures' pattern) |
| Faults (`Seeds.ForFaults`) | the violation roll, the rule, its maker | + the routing swap (one Range draw) |
| Lies (`Seeds.ForLies`) | the roll, the kind, the planner | + L11's booked place (one Range draw) |
| Every other stream | unchanged | none; no new salt |

## 8. Orders

### 8.1 The catalogue

| Branch | Item (id) | Price | Effect (unchanged unless new) | Asset |
|---|---|---|---|---|
| Desk equipment | Auto-Feed Scanner (`scanner_autofeed`) | 200 cr | handed-over papers scan themselves (P SC3) | hand-authored |
| Desk equipment | Analysis Scanner (`adv_scanner`) | 300 cr | a scan by hand marks one contradiction (P SC4) | hand-authored |
| Interview | Interview Protocols (`interview_protocols`) | 120 cr | the birth-date question | hand-authored |
| Interview | Near East, Mediterranean, East Asia and Northern Europe Translators: Speech (`tr_<pack>_spoken`) | 80 cr each (`translation.packs[].spokenCost`) | the region's speech in English (T I4) | generated |
| Portals | Portal 02 repair (`repair_portal_02`), rear left | 150 cr | portal 02 in service from the day of delivery | hand-authored, new |
| Portals | Portal 04 repair (`repair_portal_04`), upper left | 250 cr | portal 04 in service | hand-authored, new |
| Portals | Portal 05 repair (`repair_portal_05`), upper right | 350 cr | portal 05 in service | hand-authored, new |
| Contacts | Diplomatic Contacts (`diplo_contacts`) | 350 cr | a better pay rate (its unlock effect) | hand-authored |

### 8.2 The window

```
╔═ ORD Orders ═══════════════════════════════════════════════════════════════════════════════[_][□][X]╗
║ ┌─ TC-980 ───────────────────────────────────────────────────────────────────────────────────────┐ ║
║ │ (seal) TEMPORAL CUSTOMS SUPPLY · REQUISITION                           16 MAR 2150 · DAY 3     │ ║
║ │ Wallet 312 cr · Orders arrive at the start of tomorrow's shift.                                 │ ║
║ │ 1  DESK EQUIPMENT                                                                               │ ║
║ │  ├ [icon] Auto-Feed Scanner     200 cr  Handed-over papers feed the scanner…     [ Order ]     │ ║
║ │  └ [icon] Analysis Scanner      300 cr  A scan by hand marks one contradiction.  [ Order ]     │ ║
║ │ 2  INTERVIEW                                                                                    │ ║
║ │  ├ [icon] Interview Protocols   120 cr  Ask travellers when they were born.   Owned            │ ║
║ │  ├ [icon] Near East Translator: Speech  80 cr  …        In transit · arrives day 4  [ Cancel ] │ ║
║ │  └ …                                                                                            │ ║
║ │ 3  PORTALS                                                                                      │ ║
║ │  ├ [icon] Portal 02 repair      150 cr  The rear-left ring back in service.   Delivered day 3  │ ║
║ │  ├ [icon] Portal 04 repair      250 cr  The upper-left ring back in service.     [ Order ]     │ ║
║ │  └ [icon] Portal 05 repair      350 cr  The upper-right ring back in service.  Not enough cr   │ ║
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
| Deliver | `DayCycle.AdvanceNight`, after `day++`: `Orders.Due(log, day)` (ordered before the day, not delivered), in log order: `UnlockUpgrade`; its unlock effect activated with start day = the new day (`TimelineService.ActivateEffect`, instant ops applied); `deliveredDay = day`. The day's snapshot and schedule are built later, when the office loads, so each delivery (a repaired portal included) counts that day. |
| Mail | `Mailbox` builds one Delivery memo per `deliveredDay` from the log ("Delivered: Auto-Feed Scanner, Portal 02 repair."), id `delivery:{day}`. |
| Saves | The end-of-shift save holds pending orders; Continue from Home keeps them; the night delivers them. An older save loads an empty log and keeps every upgrade it owns. |
| The simulation | orders nothing, as it bought nothing at Home, so it plays with portal 01 and the Return Gate (BX1); the night's delivery runs through the same `DayCycle` step. |

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
[Serializable] public sealed class PortalSpec { public int number; public string name; public PortalRole role; public int fromDay; public string repair; }

/// A portal's state today. Runtime only; a breakdown pass may append (SM2).
public enum PortalState { InService, UnderMaintenance }

public readonly struct PortalRoute { public int Number; public string Name; public PortalRole Role; public PortalState State; public PlaceKey? Place; public bool Closed; }

/// The day's schedule, fixed at the day's start (RT2).
public sealed class PortalDay
{
    public IReadOnlyList<PortalRoute> Portals { get; }   // every portal, in number order
    public IReadOnlyList<PlaceKey> Board { get; }        // the in-service departure portals' routes, closed or not, in number order
    public IReadOnlyList<PlaceKey> OpenRoutes { get; }   // Board without the closed ones: what a citizen is drawn over
    public int ReturnsPortal { get; }                    // 0 when not in service
    public bool OnBoard(PlaceKey place);
    public bool IsOpenRoute(PlaceKey place);
    public int PortalFor(PlaceKey place);                // the in-service departure portal running it, open; 0 otherwise
}

public static class PortalSchedule
{
    /// The service-state seam (SM2): the one place a portal's state is decided.
    bool InService(PortalSpec portal, int day, Func<string, bool> repaired);

    /// The route-request seam (SM1): requests are portal number → place; today only the Directorate's.
    PortalDay Resolve(IReadOnlyList<PortalSpec> portals, int day, Func<string, bool> repaired,
                      IReadOnlyDictionary<int, PlaceKey> requests, Func<PlaceKey, bool> closed);

    IReadOnlyList<PlaceKey> OffBoard(IReadOnlyList<PlaceKey> world, PortalDay day, Func<PlaceKey, bool> closed, bool openOnly, string era = null);
    List<string> PortalProblems(IReadOnlyList<PortalSpec> portals, Func<string, bool> upgradeExists);
    List<string> DayProblems(string day, int dayNumber, IReadOnlyList<PortalSpec> portals, IReadOnlyDictionary<int, PlaceKey> directorate,
                             Func<PlaceKey, bool> inWorld, Func<PlaceKey, bool> closed, bool routingFirstDay, bool displacedWeighted);
}

public static class PortalDraw
{
    /// An era's weight for a citizen's destination (GN2): its plan weight × its open routes today; 0 without one.
    float EraWeight(float planWeight, int openRoutesInEra);
}

// Directives (grown): CaseFacts.UnservedDestination; FaultOf(PortalRouting); HasMaker, IsRolled and CanBreak include it;
//   RuleProblems checks its kinds. DirectiveFault.NoOpenPortal (appended); Faults.Unserved = "unserved".
// LieKind.Rerouted (appended, pinned); LieKinds: a record lie, citizens only.
// RecordLies: the Rerouted variant; RecordLieContext.OffBoardInEra (new); RecordLieContext.OpenPlaces is now the day's other open routes (RC8).
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
/// Every order placed and not cancelled (the Orders app, OR8): pending until delivered at the start of the day after it was placed, then kept as the log. An older save loads it empty.
public List<OrderEntry> orders = new();
[Serializable] public sealed class OrderEntry { public string upgradeId; public int orderedDay; public int price; public int deliveredDay; }
```

A repaired portal is an owned upgrade (`unlockedUpgradeIds`), so nothing else is saved for the portals: the schedule is recomputed at each day's start from the content and the owned repairs.

### 9.3 Content (`world_source.json`, through Generate World)

```json
"agency": {
  "portals": [
    {"number": 1, "name": "Front",       "role": "Departures", "fromDay": 1},
    {"number": 2, "name": "Rear left",   "role": "Departures", "repair": "repair_portal_02"},
    {"number": 3, "name": "Rear right",  "role": "Returns",    "fromDay": 5},
    {"number": 4, "name": "Upper left",  "role": "Departures", "repair": "repair_portal_04"},
    {"number": 5, "name": "Upper right", "role": "Departures", "repair": "repair_portal_05"}
  ]
},
"rules": [
  {"asset": "Rule_PortalRouting", "type": "PortalRouting",
   "description": "Departures leave only through the portals on today's Departure Board: a citizen's destination must be on the board, and not CLOSED.",
   "kinds": ["RichTourist", "PoorTourist", "Labourer"]}
],
"days": [
  {"day": 1, "portals": [{"portal": 1, "country": "greece", "era": "ancient"}, {"portal": 2, "country": "italy", "era": "ancient"},
                         {"portal": 4, "country": "egypt", "era": "ancient"},  {"portal": 5, "country": "iraq", "era": "ancient"}]}
],
"news": {
  "portals": "Today's departures: {routes}.",
  "portalInService": "Portal {number} ({name}) is in service from today."
}
```

Generate World writes `agency.portals` into the library's agency block (`PortalSpec`s), each day's routes into `DayPlanSO.directorateRoutes` (portal number and place profile reference, so a renamed place cannot dangle), `Rule_PortalRouting` like any rule, and `UpgradeSO.branch = Interview` on the translators. `fromDay` and `repair` are omitted when blank.

**`ContentSheetMap`** (and the template rewritten):
- under `agency`: `Rows("agencyPortals", "portals", Key("number"), Int("number").Required(), Text("name"), Text("role").OneOf("Departures", "Returns"), Int("fromDay").Omit().Note("the day it enters service by itself; blank: only by its repair"), Text("repair").Omit().Note("the upgrade id whose delivery puts it in service; blank: by day only"))`;
- under `days`: `Rows("dayPortals", "portals", Int("portal").Required(), Text("country").Ref("countries"), Text("era").Ref("eras")).Note("the Directorate's route for each departure portal that day; a portal in service runs it")`;
- `news`: `portals`, `portalInService`;
- the note of `days[].lies` gains `Rerouted`; `rules[].type` gains `PortalRouting`.

### 9.4 Knob assets (Inspector; Generate World never writes them)

- `Assets/Data/Upgrades/Upgrade_RepairPortal02.asset`, `…04`, `…05` (new): ids `repair_portal_02`, `_04`, `_05`; displayNames "Portal 02 repair (rear left)", "Portal 04 repair (upper left)", "Portal 05 repair (upper right)"; descriptions "The ring back in service from the day the repair arrives."; costs 150, 250, 350; branch Portals.
- Every hand-authored `UpgradeSO` gains its `branch` (the Auto-Feed and Analysis Scanners: Desk; Interview Protocols: Interview; Diplomatic Contacts: Contacts).
- `DeskConfigSO` (its anime hall group): `hallBoardInk`, `hallBoardClosedInk`, `hallBoardInset`, `hallPortalGlassLayers` (5 layer ids), `hallPortalIdleTint`, `hallPortalClosedTint`, `hallPortalPulseTint`, `hallPortalPulseSeconds`.
- `DesktopConfigSO`: the Portals and Orders windows' restored sizes.

## 10. The Directorate's routes, days 1-15

One route per departure portal per day. 01's route is open every day (RT5); a route marked **(closed)** is closed that day on purpose, so a player who repaired that portal sees a CLOSED row. With D, 01 runs each citizen beat's place.

| Day | Closures that day | 01 Front | 02 Rear left | 04 Upper left | 05 Upper right |
|---|---|---|---|---|---|
| 1 | – | Periclean Athens | Republican Rome | New Kingdom Egypt | Babylonia |
| 2 | – (RC6) | Periclean Athens | Northern Song Kaifeng | Florentine Republic | Anglo-Saxon England |
| 3 | New Kingdom Egypt, Northern Song Kaifeng, Tokugawa Edo | Elizabethan England | New Kingdom Egypt (closed) | Abbasid Baghdad | Late Ming Suzhou |
| 4 | Victorian Britain | Wilhelmine Germany | Victorian Britain (closed) | Muromachi Kyoto | Ottoman Egypt |
| 5 | Weimar Berlin, New Kingdom Egypt | Post-war Britain | Qing Shanghai | Weimar Berlin (closed) | Babylonia |
| 6 | Showa Tokyo | Periclean Athens | Victorian Britain | Tokugawa Edo | Showa Tokyo (closed) |
| 7 | New Kingdom Egypt, Showa Tokyo | Periclean Athens (Pell) | Victorian Britain | Late Ming Suzhou | Showa Tokyo (closed) |
| 8 | Northern Song Kaifeng, Weimar Berlin | Victorian Britain (Ines) | Wilhelmine Germany | Weimar Berlin (closed) | Abbasid Baghdad |
| 9 | Byzantine Mystras, Tokugawa Edo | Khedivate of Egypt | Byzantine Mystras (closed) | Meiji Nagoya | Republican Rome |
| 10 | Babylonia | Periclean Athens (Pell) | Babylonia (closed) | Showa Tokyo | Qing Shanghai |
| 11 | Japan, every era | Victorian Britain (Rook) | Post-war Britain | Meiji Nagoya (closed) | Dolce Vita Rome |
| 12 | New Kingdom Egypt, Tokugawa Edo | Weimar Berlin | Bologna, Kingdom of Italy | Qing Shanghai | Tokugawa Edo (closed) |
| 13 | Weimar Berlin, Northern Song Kaifeng | Wilhelmine Germany (Callum) | Post-war Britain | Athens, Kingdom of Greece | Weimar Berlin (closed) |
| 14 | the Modern era | Republican Rome (Quill) | Victorian Britain | Showa Tokyo (closed) | Khedivate of Egypt |
| 15 | Victorian Britain, Showa Tokyo | Periclean Athens (Pell) | Republican Rome | Victorian Britain (closed) | Babylonia |

- Days 1-6 land with this spec (plan phase PR2, day 3's closures in PR4); days 7-15 land with D's content (plan phase PR6), and replay day 6's until then (`DayPlans.Pick`).
- Day 9's beat (Gutenberg) and day 12's ("Ada") are displaced-kind and leave through the Return Gate, so 01 runs no beat those days.
- Days 7-15's closures are D §3's, unchanged; 01 is never on one of them.

## 11. Balance knobs

### 11.1 Where each one lives

| Knob | Where Saleh edits it | First cut |
|---|---|---|
| Each portal's first day, repair and role | the sheet `agencyPortals` | §9.3 |
| The three repairs' prices | `Upgrade_RepairPortal02/04/05.asset` (Inspector) | 150, 250, 350 cr |
| Every other upgrade's price | its `UpgradeSO` asset; the translators' `translation.packs[].spokenCost` (the sheet) | unchanged |
| The Directorate's routes per day (which are closed on purpose) | the sheet `dayPortals` | §10 |
| Demand per route | `days[].eras` weights (existing) | unchanged |
| The routing rule's first day | which days list `Rule_PortalRouting` in `days[].rules` | day 2 |
| The later-day roll | `days[].violationChance` (existing; the routing rule joins the rolled rules) | unchanged |
| L11's days | `days[].lies` | day 6 on |
| The board's inks and inset; the rings' idle, closed and pulse tints and the pulse's length | `DeskConfigSO` (Inspector) | FFF2D9 and an amber-red for CLOSED; 8 %; a 45 % grey, a dull red, a pale cyan; 0.8 s |
| The two windows' sizes | `DesktopConfigSO` (Inspector) | Portals 880 × 600 u, Orders 1040 × 800 u |
| The one penalty, the free warning | `GameConfigSO` (unchanged) | 10 cr; 1 |

### 11.2 Balance hooks for phase 23's simulation

| Id | Hook | Why |
|---|---|---|
| BX1 | The simulation orders nothing, so it plays with portal 01 and the Return Gate: every citizen goes to 01's route each day. It runs through the same `BuildToday` and `DayCycle` steps. | The measured run is the game's own, and a non-repairing player's. |
| BX2 | The summary lists each day's open routes and each route's share of the accepted travellers, and per run the places history moved, the leader's first day and its dominance. | Citizens now land on one place a day (up to four with repairs) instead of up to 40, so history concentrates on the Directorate's schedule. First watch line: the leader's median first day moving more than two days earlier than the pre-portal baseline. |
| BX3 | The 2150 contamination count (D X5) per place, not only per run. | Carries from smugglers and strandings concentrate on the routes too. |
| BX4 | The epilogue thresholds re-proposed over the new destinations (D E2). | The attribute totals move with the destinations. |
| BX5 | A "repairs" policy beside the three ways to play: order each repair as soon as the wallet allows. | Measures what the repairs cost a run and how the spread of destinations changes history; it also feeds Q15. |

## 12. Art

### 12.1 `docs/ART_ASSET_LIST.md` changes (made in the plan's phases)

**Section 1, the office**, two rows:

| Prop | Anchor, and what the game finds | Status |
|---|---|---|
| The departure board: its display stays blank, the game draws the rows and the CLOSED marks | `DepartureBoard`: the hall's `16 Departure board blank display` | done: keep the display text-free |
| The five portal rings: the game tints their glass (dimmed under maintenance, tinted when closed, a pulse on a departure) | the presentation's layers `53` to `57 Portal 0n … painted glass` | done; optional, Tier 3: a lit-glass variant per ring if the tint reads flat, and a "maintenance" dressing (scaffold, tape) for the art side to consider |

**Section 3, the PC desktop:**
- "Desktop icons × 6" becomes "× 8"; the ids line gains `portals` (a portal ring, perhaps with an arrow through it) and `orders` (a parcel or a crate with a tick). Same format: 128 × 128, a bold greyscale glyph on transparent, no plate.
- New row, **Orders branch glyphs × 4**: the Orders app's branch heads, 40 × 40 u; `Assets/Art/UI/Resources/Orders/branch_<id>.png` for `desk`, `interview`, `portals`, `contacts`; now code-drawn placeholders; deliver 128 × 128 greyscale glyphs, no plate; Tier 1; missing.
- New row, moved from section 6, **Upgrade icons × 11**: at the left of each Orders row, 44 × 44 u; `Assets/Art/UI/Resources/Orders/upgrade_<id>.png` (moved from `Resources/Home/` with their metas); the eight ids plus `repair_portal_02`, `repair_portal_04`, `repair_portal_05` (one wrench-over-ring glyph may serve all three); 256 × 256; Tier 1; 2 interim, 9 missing.

**Section 6, Home:** the Shop panel and Upgrade icons rows go (the icons move to section 3); the Retired table gains `panel_shop.png` ("the Home shop moved to the PC's Orders app").

**Totals:** section 3 goes from 16 + 11 (27 files) to 33 + 11 (44); section 6 from 19 + 7 (26) to 10 + 7 (17); the total from 81 + 19 (100) to 89 + 19 (108).

### 12.2 The scene contract

§5.3's rows, in `docs/SCENE_CONTRACT_GAMEPLAY.md`.

## 13. Tests and golden masters

### 13.1 EditMode (Domain and Visuals first; the plan names each test)

- `PortalScheduleTests`: `InService` (by day; by a delivered repair; both, whichever first; never with neither); `Resolve` (a portal in service runs its request; under maintenance it runs nothing; a closed route is on the board and marked; the Returns portal has no route; every portal is listed); `OnBoard`, `IsOpenRoute`, `PortalFor`; `OffBoard` (open only or all; by era); `PortalProblems`; `DayProblems`.
- `PortalDrawTests`: `EraWeight` is the plan weight × the open routes; 0 without one.
- `DirectivesTests`: routing's decision table (off the board: breaks; on the board, open or closed: holds; the displaced: not read), `FaultOf` is `NoOpenPortal`, `HasMaker`, `IsRolled`, `CanBreak` (citizens), a closed destination off the board reads "closed" first, `RuleProblems` (kinds required; `Displaced` refused).
- `FaultsTests`: `NoOpenPortal`'s reason is "unserved".
- `RecordLiesTests`: Rerouted books an off-board place of the claim's era; one draw; no possible lie (no draw) without one; its tells are every Destination field; L5's worksite draws among the other open routes.
- `LieKindsTests`: Rerouted is a record lie, for citizens only.
- `AccountMakerTests`: the record's Booked departure row prints the booking when set, the claim otherwise.
- `OrdersTests`: every state; `Place` (refuses owned or in transit); `Cancel` (today only, returns the price); `Due` (ordered before the day, not delivered).
- `MailboxTests`: one Delivery memo per delivery day, listing the items; a pending order sends nothing.
- `PortalBoardTextTests` (Visuals): the rows (in-service departure portals, in number order, upper case, CLOSED marked); the tooltip (every portal and its state).
- `DesktopLayoutTests` and `DesktopIconPlaceholderTests`: eight icons (the arrange grid's second column); the two new glyphs.
- `SerializedEnumsTests`: `TravelRuleType.PortalRouting`, `LieKind.Rerouted`, `OfficeAnchorId.DepartureBoard`, `UpgradeBranch`, `PortalRole`.
- `ContentSheetMapTests`: `agencyPortals`, `dayPortals`, the news fields.
- Retired: `PagingTests` (with `Paging`).

### 13.2 Golden masters (the audit baseline, `docs/reviews/audit-baseline`)

| Golden | Effect | Why |
|---|---|---|
| `cases.txt` (the static case dump) | **every day changes** | the kind is drawn before the destination; citizens go to 01's route (the dump, like the simulation, repairs nothing); day 2 loses its closure and gains the routing breaker; day 3 gains New Kingdom Egypt's closure; L11 from day 6. Unlike D's plan, no day stays byte-identical; the one re-pack explains it |
| The play transcript and saves | change | the destinations and notices; `orders` in the save (empty unless the job orders); the audit play job orders at the PC instead of buying at Home |
| Scene dumps | `OfficeGameplay` (two icons and windows, the board's text and click box, the ring link); `HomeScene` (no shop panel) | the builders |
| Data hashes | the upgrades (branch; the three repairs), the translators (branch), `Rule_PortalRouting`, the day plans (routes; days 2 and 3's rules), the library (portals) | Generate World |
| Static metrics | new types; `Paging` and the shop's code gone | – |
| The balance summary | re-run (BX1-BX5) | phase 23's thresholds move |

## 14. `docs/FEATURES.md` changes (in the commits of the behaviour)

| Section | Line (its first words) | Change |
|---|---|---|
| Run & meta | the save line | + `orders`, additive |
| Run & meta | "Home phase between days" | the shop goes: Expenses → Slot → Sleep; the upgrade list moves to Orders |
| Office scene — the art office | (a new line) | the Departure Board (anchor, rows, CLOSED marks, tooltip) and the rings' tints and pulse |
| Desk | "Desk scanner" | the two scanners are Orders items, delivered at the start of the next office day |
| Fake-OS desktop | "Six desktop icons" | eight icons: + Portals, Orders |
| Fake-OS desktop | (new lines) | the Portals app (read-only, §4); the Orders app and delivery (§8); Mail's delivery memo |
| World | the day plans' line | the portals, their service (01 from day 1, the repairs, the Return Gate on day 5), the Directorate's routes; a citizen's destination is an open route (by era weight), the kind drawn first; the displaced's Return Gate |
| Investigation loop | the Directives memo line | the portal schedule section |
| Investigation loop | the directives and faults lines | `PortalRouting` (its breaker on day 2, the roll after), CLOSED routes on the board and the closure's violator bound for one, the closures from day 3 (New Kingdom Egypt moved there), L11, L5's worksite among the open routes, the new citation |
| Translation | "Translators" | ordered in Orders, arriving the next office day; the day-4 notice's words |
| Shift clock & queue | the guarantees line | day 2: routing instead of a closure; day 3: three closures |
| Content & tooling | Generate World, the validator, the sheets, the simulation | the new inputs, checks (§6.1, `PortalProblems`, `DayProblems`) and sheets; BX1-BX5 |

## 15. Questions for Saleh

### 15.1 Answered (2026-09-30)

| Q | His answer | Applied in |
|---|---|---|
| Q1 Who sets the routes | C, for now: the Directorate sets every route; the app shows them; "we are going to make it change" | RT3, PA1-PA3, SM1; the first draft's clerk-set design is kept in git (`097f20c`) for the next pass |
| Q2 Unlocks | "One portal is open on day 1; the rest are under maintenance and you need to pay for them in the PC. They might break in the future again." | PO3, PO4, PO7, OR6, SM2; the Return Gate is proposed as the Directorate's repair (Q3) |
| Q6 The routing rule's first day | Day 2; day 2's closure moves to day 3 | RC6 (day 3 then lists three closures: flagged there) |
| Q7 Closures and routes | B: the board shows CLOSED; travellers bound for a closed route must be denied | RT4, RC5, BD3, BD5 |

### 15.2 Open

Each lists the recommended option first; the plan builds it and changes only the decisions a different answer names.

| Q | Question | Options (recommended first) |
|---|---|---|
| Q3 | The Return Gate and the authored beats under Q2 (PO3, GN5) | **A** the Directorate repairs the Return Gate for day 5 (not for sale), and a citizen beat's place is portal 01's route on its day. **B** the clerk pays for the Return Gate like the others; until it is repaired no displaced traveller comes to Desk 3, and the famous (Senenmut, "Socrates", Gutenberg, "Ada") wait. **C** the clerk pays for it; until it is repaired every displaced traveller must be turned away (a directive fault: a free denial, so not recommended). |
| Q4 | What kind of fault is "not on the board"? (RC2, RC3) | **A** a directive fault, read like a closure (no evidence), with its lie twin L11 proven through the compare path. **B** a deviation: compare the Destination with a board row to log "NOT ON THE BOARD", and deny with that evidence. **C** A without L11. |
| Q5 | Do the papers name a portal? | **A** no: the Destination is matched against the board, and the forms stay at six fields. **B** a "Portal" row on the manifest and the return order (the booked portal; a mismatch with the board is a second directive fault); the manifest is full, so one of its rows has to move. |
| Q8 | What a route change costs | Moot while the Directorate sets every route (Q1); it returns with the clerk-set pass (SM1). |
| Q9 | The Orders catalogue (OR2) | **A** every Home item plus the three repairs, in four branches drawn as a tree, no prerequisites, paid at order, cancellable the same day. **B** A with prerequisites (for example the repairs in order, 02 before 04 before 05; the Analysis Scanner after the Auto-Feed). **C** A, but paid on delivery (charged the next morning; refused if the wallet cannot cover it). |
| Q10 | What stays at Home (OR7) | **A** expenses and care, the slot machine and sleep; the shop goes. **B** A, plus a "Deliveries tomorrow" line on the expenses panel. **C** a small Home shop for the translators only. |
| Q11 | The board in the hall (BD1-BD4) | **A** gameplay-drawn LED rows on the art's blank display (the in-service routes, CLOSED marked) and a hover tooltip with every portal. **B** a text object the art scene carries (`Add Anime Hall Hooks` adds it; the art scene is edited once more). **C** a HUD chip listing the routes, not on the board. |
| Q12 | The rings (BD5) | **A** the gameplay tints each ring's glass through the art's `SetLayerTint`: dimmed under maintenance, tinted when closed, a pulse on a departure (needs the art side's OK). **B** no ring feedback. **C** the art side adds lit-glass and maintenance layers and its own hook component. |
| Q13 | Demand per route (GN2) | **A** each open route draws by its era's weight in the day plan. **B** every open route draws equally. |
| Q14 | L11, the rerouted booking (RC7) | **A** from day 6, and every day 7-15. **B** from day 3, with that day's other record lies. **C** not at all. |
| Q15 | What should a repaired portal earn? (PO6; new with Q2's answer) | **A** nothing more for now: one more route (variety, history spread over more places); the next portals pass decides. **B** each portal in service adds a traveller to the day's queue (more pay, more work). **C** a departure bonus per accepted traveller through a repaired portal (a knob). **D** the Directorate books travellers on every portal, and those whose portal is under maintenance are turned away (a directive fault: free denials, so not recommended). |

## 16. Intent audit

**Against Saleh's words.**
- *"there is now multiple portals and a destination board"*: the hall's five rings are five portals (PO1), and its board shows the day's routes, CLOSED marked (BD1-BD4).
- *"new portals will unlock"* and his Q2 answer, *"One portal is open on day 1; the rest are under maintenance and you need to pay for them in the PC"*: 01 from day 1; 02, 04 and 05 by paid repairs in Orders, delivered the next day; the Return Gate repaired by the Directorate for day 5 (PO3, recorded with its reason, Q3). *"They might break in the future again"*: SM2, no behaviour now.
- *"the upgrade tree be in the pc now as another icon: you can order the upgrades and they arrive next day"*: the Orders icon, its catalogue in four branches, paid at order, delivered at the start of the next day (OR1-OR6).
- *"destination must match portal in the papers"*: the routing directive: the destination the papers and the claim name must be on the board (RC1), and a CLOSED route must be denied (RC5).
- *"destination board should also show it"*: the board lists every in-service route (BD3); its tooltip, the Rules memo and the app list every portal.
- *"an icon that shows the portals and the destination"* and his Q1 answer: the Portals icon shows each portal's era, place and state; setting routes waits for the next pass (PA1-PA2, SM1).
- *"Board shows CLOSED"* (Q7): the CLOSED mark, and the closure's violator bound for a CLOSED route when there is one (RT4, RC5).
- *"lets try that"* and *"knowing we are going to make it change"*: every knob in the sheet or the Inspector (§11); the two seams name where the next pass plugs in (§1.9).

**Against his design rules.**
- Rule 1: one wrong-decision penalty for a routing fault, a closed route or an L11 liar, accepted or denied; no route fee, delivery fee or portal fine (RC2).
- Rules 2 and 3: no new wheel entry, verb or kind-keyed line; the routing breaker and the L11 liar speak their personality's lines.
- Rule 4: every knob is in the sheet (`agencyPortals`, `dayPortals`, `days[]`) or an Inspector asset (the repairs, `DeskConfigSO`, `DesktopConfigSO`); the authored beats' places are authored routes, never drawn (GN5).
- Rule 5: the free warning, the premades' extra stability loss and the stability model are untouched.

**Redo or override checks.**
- Overrides P DK1's "six icons" (eight now) and P SC1's "two scanner upgrades in the Home shop" (in Orders now): Saleh's request.
- Overrides T §2.3's closures on days 2 and 3 (New Kingdom Egypt moves to day 3) and FEATURES' "closures from day 2": Saleh's Q6. Day 3's load is flagged (RC6).
- Changes T's destination draw (a citizen's destination is an open route, the kind drawn first: GN1-GN2). T K2's claim model (the claim is the destination) is kept.
- Changes the closures' planned violator (bound for a CLOSED route when the board has one, RC5) and L5's worksite maker (the other open routes, RC8).
- Replaces the first draft's clerk-set routes, closure retuning and Directorate bookings (Saleh's Q1 and Q7): the bookings are unnecessary once the Directorate sets every route (GN5).
- Re-baselines every day of `cases.txt`, where D kept days 1-6 byte-identical (§13.2).
- Reuses, never duplicates: the directive machinery (`TravelRuleSO`, `Directives`, `CaseFacts`, `ViolatorSlots`, `Directives.Roll`), `RecordLies` and `RecordMismatch`, the day-start snapshot pattern, owned upgrades for the repairs, `HomeEconomy.UpgradeCost` over `ShopPrices.Discounted`, `TimelineService.ActivateEffect`, `DayCycle`, `Mailbox`, `FormView` and `FormSpecSO`, the contract's fallback resolution, the prop tooltip, the art's `SetLayerTint`.

**Exploits and incentives checked.**
- A schedule that moves mid-shift: impossible, it is fixed at the day's start (RT2).
- A day with no open route: impossible, 01's route is always open (RT5).
- Easier days by not repairing: the number of faulty travellers never depends on the repairs (PO6, RC4, RC5: one violator per closure, the routing guarantee and the roll are per traveller).
- **Flagged:** a repair buys variety, not a reward, so a careful player has little reason to pay for it (PO6); Q15 asks what an open portal should earn, and BX5 measures the repairs.

**No-code-path checks.** The service rules, routes, closed-on-purpose routes, rule days, lie days, demand, prices, inks, tints and window sizes are data or Inspector knobs. New code is limited to what data cannot express: the schedule's resolution, the demand weight, the routing predicate and makers, the closure violator's preference, L11's maker, the order log and delivery, the two windows, the board's text and binding, and the ring link.

## 17. The seams

| With | What this spec assumes or asks |
|---|---|
| T (traveller types) | §2.3's closures on days 2 and 3 (RC6); §3's forms are unchanged (the Destination rows exist); §5.2's verdict table gains two rows (§6.4); §5.3's directives gain `PortalRouting`; §5.4's closure maker prefers a CLOSED route (RC5); §6.1's catalogue gains L11; §6.3's Destination maker (L5) draws among the open routes. A change to one of those rows updates this spec. |
| P (the PC redesign) | DK1's count (eight icons); DK4's restore (a new id takes the first free spot, so the two new icons join saved layouts there); TH3's flavour labels (+ `icon.portals`, `icon.orders`: 30); SC1's shop (Orders); ML1's mail kinds (+ Delivery); FO9's PC pages (+ TC-970, TC-980); §2.9's Rules memo (the schedule section). The plan updates P's rows in the phase that changes them. |
| D (days 7-15, on `design/days-7-15`) | B2's citizen premades are bound for portal 01's route on their days (GN5, §10); §3's days gain their routes (§10) and `Rerouted` in `lies`; `desk4_closes` reads "From today its queue joins Desk 3, and its portal (05, upper right) is Desk 3's, still under maintenance."; A4's era weights are the routes' demand; X5's contamination count is kept per place (BX3). `TransponderRecall` and `PortalRouting` both append to `TravelRuleType`: whichever lands second renumbers after the first. |
| Phase 23 (balance, on `redesign/p23-balance`) | `DayCycle` is the one day path (delivery joins `AdvanceNight`); the simulation builds the day's schedule through `BuildToday` (BX1); the repairs' prices and the thresholds are phase 23's to tune (BX4, BX5). |
| Saleh's next portals pass | SM1 (clerk-set routes) and SM2 (breakdowns); Q15 (what an open portal earns). |
| The art side | the board's display stays blank and text-free (§12.1); the rings' tint needs its OK (Q12); a renamed layer is a `DeskConfigSO` edit. |
| The personalities spec (S) | nothing: no kind-keyed line is added. |
