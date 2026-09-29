# Portals, the Departure Board and Orders: design (v3)

*2026-09-30 · drafted by Claude from Saleh's request of 2026-09-29 and his design rules of the same day; v2 applied his answers to Q1, Q2, Q6 and Q7; **v3 applies his answers to every remaining question and his scope decision: the destination check is delayed to a later pass** (§14 records each answer and where it is applied) · read-only map of `main` at `cdf10f7` (`E:\unity\NOPE-docs`, branch `design/portals`) and of `origin/design/days-7-15` at `24e7a8e` (the days 7-15 spec, **D** below, not on `main` yet) · the code wins over this text · the traveller-types spec (`2026-09-26-traveller-types-design.md`, **T**) owns the kinds, papers, lies, directives and verdicts, none of which this version changes; the PC redesign spec (`2026-09-26-pc-redesign-design.md`, **P**) owns the desktop, the windows and the forms; this spec owns the portals, their repairs, the Departure Board, the rings' effects, the Orders upgrade tree and Home's house upgrades · built by `docs/superpowers/plans/2026-09-30-portals-plan.md` · the earlier versions stay in git as the design of the delayed pieces: `097f20c` (v1: clerk-set routes) and `78c5e60` (v2: the destination check, L11, the destinations drawn over the routes)*

Saleh, verbatim (2026-09-29):

> "there is now multiple portals and a destination board. new portals will unlock. we will have the upgrade tree be in the pc now as another icon: you can order the upgrades and they arrive next day. destination must match portal in the papers and destination board should also show it. you will have an icon that shows the portals and the destination. this icon will set the portal to an era and a location. lets try that"

His answers (2026-09-30, relayed by the orchestrator), in full in §14.1. The ones that set this version's scope:
- **Scope:** "only the destination check" is **delayed** to a later pass: no check that a traveller's destination is on an open portal or not CLOSED, no L11, no evidence rule, no change to how destinations are drawn. Day 2's closure therefore stays on day 2. Every seam for that pass is named (§1.9).
- **Build now:** the five portals; 01 open from day 1; 02, 04, 05 **and the Return Gate (03)** under maintenance until the clerk pays for their repair in Orders ("the Return Gate to repair, it will have its own VFX"); until the Return Gate is repaired, accepted displaced travellers leave through portal 01; routes set by the Directorate, the Portals app read-only; the board as gameplay-drawn rows with a tooltip; the rings' effects (an open ring glows, a ring under maintenance is dimmed, a CLOSED portal shows nothing inside its ring, a departure pulses its ring, the Return Gate has its own effect), drawn on the right sorting layer under each ring's frame.
- **Orders:** a real upgrade tree **with prerequisites**, drawn visually (nodes, links, icons, states: locked, orderable, in transit, owned); paid when ordered, delivered the next day, with a Mail notice; the portal repairs are nodes in it.
- **Home:** keeps the expenses, family care, the slot machine and sleep, **plus house upgrades that reduce sickness and similar household improvements**, which stay at Home; everything else moves to Orders.

**The art.** The anime terminal hall (`Assets/Art/Office/AnimeHallLayers/AnimeHall.unity`, art `c75e1fe`, the live art office; its layers are the prefab `AnimeHallArt.prefab`'s) draws five portal rings and a departure board. Every layer is a `SpriteRenderer` on the **Default** sorting layer whose sorting order is its layer number (measured in the prefab and the scene):

| Portal | Secure bay (the enclosure: its fence and glass panels, a semicircular cut where the ring stands) | Metal ring (the gate frame; its centre is transparent) | Painted glass (the front panels' highlights) | Other layers inside the ring's centre (pixels measured) |
|---|---|---|---|---|
| 01 front (the pit behind the traveller) | `38 Portal 01 front secure bay` | `39 Portal 01 front metal ring` | `53 …painted glass` | floor 6 (behind); bay 38 and glass 53 (in front) |
| 02 rear left | `40 …secure bay` | `41 …metal ring` | `54` | floor 6; bay 40, glass 54 |
| 03 rear right (the Return Gate) | `42 …secure bay` | `43 …metal ring` | `55` | floor 6; bay 42, glass 55 |
| 04 upper left (the gallery) | `44 …secure bay` | `45 …metal ring` | `56` | service walls 4; bay 44, glass 56 |
| 05 upper right (the gallery) | `46 …secure bay` | `47 …metal ring` | `57` | service walls 4, gallery floor 7; bay 46, bridge railing 49 and glass 57 (in front) |

The board is `15 Departure board frame` with `16 Departure board blank display` (orders 15 and 16): about 313 × 107 px of the 1080p view, just under the claim strip (`SCRATCH/t5/hall/play_smoke4/hall_opening_1080.png`). Nothing uses the rings or the board today.

**Saleh's design rules (2026-09-29), which override any spec text that disagrees:**
1. One penalty for any wrong decision, approval or rejection alike, after one free warning a day (`GameConfigSO.freeWarningsPerDay`).
2. Personalities, not traveller types, drive claim lines, reactions and small talk.
3. The same interactions for every traveller type.
4. Balance knobs are editable in the Unity editor (ScriptableObjects that Generate World does not overwrite, or the content spreadsheet), and authored cases stay outside the random draws.
5. A premade keeps its extra stability cost; stability is a two-decimal value; days 7-15 are authored.

## 0. The map: what exists and what this version does with it

| What (where) | Today (`main` `cdf10f7`) | This version |
|---|---|---|
| Travellers, destinations, closures, directives, lies, verdicts (`CaseFactory`, `Directives`, `RecordLies`, `DayPlanSO`) | as T and phase 9 built them | **unchanged**: no new check, no new lie, no change to how a destination is drawn; `cases.txt` stays byte-identical. The delayed check pass plugs in at the seams of §1.9 |
| The hall's rings and board (`AnimeHallPresentation`'s layers; `SCENE_CONTRACT_GAMEPLAY.md`; `OfficeAnchorId`, 23 values; `AnimeHallShiftLink` drives the art's `SetTime`) | art only | five portals with a service state and a Directorate route each (PO, RT); the board's rows and tooltip (BD, new anchor `DepartureBoard`); the rings' effects (VX) |
| The Home shop (`HomeManager.ShowShop`, `HandleBuyUpgrade`; `HomeUIController`'s shop panel and its `Paging`; `HomeSceneBuilder`'s `ShopPanel`) | eight office upgrades, bought at night, in force from the next office day | the panel becomes the **House** panel and sells only house upgrades (HM); the eight office upgrades move to Orders (OR) |
| Upgrades (`UpgradeSO`: id, name, blurb, cost, unlock effect; `WorldState.unlockedUpgradeIds`) | a flat list | + `venue` (Orders or Home), `branch` and `requires` (the tree's prerequisites); + four portal repairs; + four house upgrades |
| Effects (`EffectSO`, `EffectOpType`, `TimelineEffects.SumFloat`) | pay rate, shop discount, liar and legendary chances, … | + three household ops (sickness chance, care cost, household expense), read by `HomeEconomy` (HM3) |
| The desktop (`DesktopAppIds`; P DK1: "Six icons, nothing else, ever") | six icons | **eight**: + Portals (read-only) and Orders. Saleh's request overrides DK1's count; "no icon is added at run time" still holds |
| Mail (`Mailbox`, `MailKind`: 4 kinds) | | + `Delivery` |
| Saves (`WorldState`, save version 3) | | + `orders`, additive: the version stays 3 |

## 1. Decisions

### 1.1 The portals (PO)

| Id | Decision | Why |
|---|---|---|
| PO1 | **Five portals, one per ring of the art, numbered as the art numbers them**: 01 front, 02 rear left, 03 rear right, 04 upper left, 05 upper right. Content: `agency.portals[]` = `{number, name, role, fromDay, repair}` (§8.3). | The hall already shows five rings; numbering them by the art's own layer names keeps the board, the app, the tree and the rings one list. |
| PO2 | **Two roles.** *Departures* (01, 02, 04, 05): each runs the route the Directorate sets for it that day (RT2). *Returns* (03, the **Return Gate**): the displaced go home through it; it has no route. | A displaced person's destination is their origin, anywhere in the day's world (T K2); the Return Gate "tunes to their Return Order's incident", so the fiction costs no field. |
| PO3 | **Service** (Saleh's Q2 and his Return Gate answer). 01 is in service from day 1. **02, 03 (the Return Gate), 04 and 05 start under maintenance**; each enters service at the start of the day after its **repair** is delivered (a node of the Orders tree, OR5). A portal row may carry both `fromDay` and `repair`: it is then in service from whichever comes first. | "One portal is open on day 1; the rest are under maintenance and you need to pay for them in the PC"; "the Return Gate to repair, it will have its own VFX". |
| PO4 | **Where an accepted traveller leaves** (`PortalDay.DepartureFor`): the displaced through the Return Gate when it is in service, **else through portal 01**; a 2150 citizen through the in-service departure portal whose route is their destination and open, **else through portal 01**. A denied traveller does not leave through a portal. | Saleh: "until the Return Gate is repaired, accepted displaced travellers leave through portal 01". With the check delayed, most citizens' destinations are not on the board; 01 is the default departures portal and is always open (RT4). **What should happen instead to a traveller no open portal serves (hold, deny) belongs to the delayed check pass** (SK3). |
| PO5 | **Desk 4's portal.** D's day 9 (`desk4_closes`: Desk 4 closes and its queue joins Desk 3) also says that its portal, 05, passes to Desk 3 still under maintenance. | Saleh's rule makes 05 a repair like the others; the story keeps its line. Nothing on day 9 depends on 05 (PO4). |
| PO6 | **What a repair earns, for now: nothing more** than the ring in service, its glow and its row on the board (Saleh's Q15 A). The check pass gives the repairs their weight. | Saleh's answer. Repairs change no traveller, pay or fault in this version. |
| PO7 | **Under maintenance** shows everywhere the portals do: the ring dimmed (VX2), the board's row "UNDER MAINTENANCE" (BD3), the tooltip, the Portals app ("repair it in Orders", its price, or "locked: repair 02 first"), and the Orders tree's repair node. | "new portals will unlock": the player sees what is coming and what it costs. |

### 1.2 Routes: the schedule (RT)

| Id | Decision | Why |
|---|---|---|
| RT1 | **A route is a place** (a country × an era) of the day's world, never the present or the Future, keyed `country:era` (`PlaceKey`), the pair `rules[]` already names places by. | "set the portal to an era and a location": an era and a country name exactly one of the 40 places. |
| RT2 | **The Directorate sets every route** (Saleh's Q1, "for now"): each day of `days[]` names the route of each departure portal (`days[].portals[]` = `{portal, country, era}`); a portal in service runs its route; a route a closure forbids today is **CLOSED** (Saleh's Q7). **Resolution** (Domain `PortalSchedule.Resolve`, pure, no draw): each portal's state (in service or under maintenance), each in-service departure portal's route, and whether it is closed. | Content carries the routes, so Saleh edits them in the sheet (rule 4); the resolution is a lookup any test can pin. |
| RT3 | **The day's schedule is fixed at the day's start** (`PortalDay`, built with today's world in `ContentLibrarySO.BuildToday`, the pattern of `ScannerDay` and `TranslationDay`) and holds all day: the board, the tooltip, the Portals app, the departures and the rings read that one object. A repair delivered at the night's turn counts from that day's start. | One object, so the hall and the PC never disagree; nothing moves under a queued traveller. |
| RT4 | **Portal 01's route is open every day** (a content check), and a day's routes are distinct. The routes of 02, 04 and 05 may be closed on purpose. | 01 is where every fallback departure goes (PO4) and it always glows; a closed route on a repaired portal shows the CLOSED state. |

### 1.3 The Portals app (PA)

| Id | Decision | Why |
|---|---|---|
| PA1 | **A desktop icon, Portals** (`portals`), and its window: the form TC-970 "Portal Schedule", **read-only**. The default icon order becomes Investigation, Portals, Internet, Mail, Citizen Account, Orders, Notes, Settings (`DesktopAppIds.DefaultOrder`); a saved layout gains the two new icons in the first free spots (P DK4). | "an icon that shows the portals and the destination"; setting routes waits for Saleh's next pass (SM1). |
| PA2 | **What it shows** (§4): one row per portal: number, ring, **era** and **place** of its route, and its state: In service; CLOSED today (with the closure's line); Under maintenance (the repair's price, "repair it in Orders", or "locked: repair 02 first", or "repair in transit · in service tomorrow"); Return Gate (in service, or under maintenance). Its footer: "Routes are set by the Directorate. Until the Return Gate is repaired, the displaced leave through Portal 01." | Saleh: the app shows "each open portal's route (era and place)"; the state column is what the player can act on (a repair). |
| PA3 | The window opens restored at 880 × 600 u (a `DesktopConfigSO` knob) and can be maximised; the form scrolls (P AP7) and is reached with the keyboard like any page (P KB4). Themed chrome, an unthemed form (P TH1). | The PC's window and accessibility rules. |

### 1.4 The Departure Board (BD)

| Id | Decision | Why |
|---|---|---|
| BD1 | **New anchor `DepartureBoard`** (`OfficeAnchorId`, appended as 23). Fallback: the bare name `16 Departure board blank display` (its sprite's bounds are the place). The 3D room has no board: the anchor is missing there and the board is skipped. | The contract's own pattern; the art scene is not edited. |
| BD2 | **The rows are drawn by the gameplay layer** (Saleh's Q11 A): a world-space TextMeshPro built by Build Office UI in `OfficeGameplay` (sorting layer `Gameplay`), fitted each frame to the anchor's bounds inset by `DeskConfigSO.hallBoardInset`, so it follows the display if the art pans. Ink `hallBoardInk` (FFF2D9, the ivory of the hall's other glasses); CLOSED and UNDER MAINTENANCE in `hallBoardStateInk`. | The gameplay owns the text and its layout; the contract places gameplay objects by world position and never parents them into the art. |
| BD3 | **What it shows**: one row per portal, in number order: `01  ANCIENT  PERICLEAN ATHENS`; a closed route `02  ANCIENT  NEW KINGDOM EGYPT  CLOSED`; `03  RETURN GATE` (or `03  RETURN GATE  UNDER MAINTENANCE`); `04  UNDER MAINTENANCE`. | Saleh: "portal, era, place, or CLOSED / UNDER MAINTENANCE". Five rows keep one layout all run. They are about 21 px tall at 1080p and 14 px at 720p, below the 720p floor, so the tooltip is the readable surface there (BD4). |
| BD4 | **The board is a reacting prop**: a click box on `Interactable` over it (the hover outline), whose tooltip lists the same rows in UI text, the ring's name and the closure's line included. The tooltip grows to fit its lines. | The office's readable surface at 720p; the PC is the full one. |
| BD5 | The board, the tooltip, the app and the rings read the day's `PortalDay` (RT3). | They never disagree. |

### 1.5 The rings' effects (VX)

| Id | Decision | Why |
|---|---|---|
| VX1 | **An open departure portal's ring glows**: a gameplay effect (`PortalEffect`, one per ring) inside the ring's centre, an unlit additive sprite (the art slot `Office/portal_glow`, a code-drawn radial glow until it lands) tinted `hallPortalGlowTint`, slowly turning; still with reduced motion. | Saleh: "an open ring glows". Unlit, so it glows through the evening the hall's light brings. |
| VX2 | **A ring under maintenance shows nothing** (amended 2026-09-29): nothing glows inside it and its metal-ring layer keeps the art's colour. First built as a 45 % grey tint (`hallPortalIdleTint`) through `AnimeHallPresentation.SetLayerTint`, removed: each ring layer (`39`, `41`, `43`, `45`, `47`) also carries the wall, pillar and bay pixels around and below its frame (the registered layers are mutually exclusive masks), so the tint greyed that whole disc. | Saleh (2026-09-29): "a closed portal has nothing in the ring"; hide a closed portal's halo. A ring layer holding only the metal (art side) would allow a dimmed frame again. |
| VX3 | **A CLOSED portal shows nothing inside its ring**: no glow, and the ring at its own colour. | Saleh: "a CLOSED portal shows NOTHING inside its ring". Since VX2's amendment the hall draws CLOSED and under maintenance alike; the board and the Portals app tell them apart. |
| VX4 | **A departure pulses its ring**: when an accepted traveller leaves (PO4), that portal's effect flares (`hallPortalPulseScale`, `hallPortalPulseSeconds` 0.8 s); one step up and down with reduced motion. | Saleh: "a departure pulses the ring"; the player sees where the traveller went. |
| VX5 | **The Return Gate has its own effect**: a slow inward spiral (the art slot `Office/portal_return_glow`, a code-drawn placeholder until it lands) tinted `hallReturnGateTint` (amber), with its own pulse on a displaced traveller's departure; dimmed and empty under maintenance. | Saleh: "it will have its own VFX". |
| VX6 | **Where the effects draw.** Each effect is a gameplay object placed each frame at its ring's bounds (the metal-ring layer's renderer bounds; diameter `hallPortalGlowSize` × the ring's, 0.85), on **the art's sorting layer (Default)**, **sorting order = its ring's secure-bay order − 1** (37, 39, 41, 43, 45), read at run time from the bay's renderer, never hard-coded. So it draws over the floor and walls seen through the ring and **under the secure bay's front fence and panels, the metal ring (the gate frame) and the painted glass**; on portal 05, under the bridge railing too. The orders it shares (the stone bust's 37, and each lower-numbered ring's order) belong to layers with no pixel inside any ring's centre (measured at art `c75e1fe`; the PR4 probe re-checks it). | Saleh: the effects "sit on the correct sorting layer UNDER the gate frame that surrounds each ring". Every art layer is on Default with its layer number as its order; the bay's front parts stand in front of the ring's lower half (the ring layer is cut where they cross), so an effect inside the ring must draw under the bay too. The contract's "gameplay sprites use the Gameplay layer" rule gets this one written exception (§5.3). |
| VX7 | **The link** (`AnimeHallPortalLink`, added by the binder to its own object when the art office carries an `AnimeHallPresentation`, beside `AnimeHallShiftLink`): it sets the tints and the effects at the day's start and fires the pulse on an accept. The five portals' layer names are one `DeskConfigSO` list (`hallPortalLayers`: bay, ring and glass ids per portal), so an art rename is a knob edit. | The gameplay drives the art only through the art's own public hooks (`SetTime`, `SetLayerTint`, `FindLayer`), as the shift link does. |

### 1.6 Orders: the upgrade tree (OR)

| Id | Decision | Why |
|---|---|---|
| OR1 | **Every office upgrade moves to the PC's Orders app**: the Auto-Feed Scanner, the Analysis Scanner, Interview Protocols, Diplomatic Contacts and the four Speech translators, with the same ids, prices and effects, plus the four portal repairs. House upgrades stay at Home (HM). An upgrade's `venue` (`UpgradeVenue`: Orders or Home, serialized) says which. | Saleh: "everything else moves to Orders"; "the upgrade tree be in the pc now as another icon". Keeping the ids keeps every save's owned upgrades. |
| OR2 | **A real tree with prerequisites** (Saleh's Q9): `UpgradeSO.requires` (upgrade ids, Inspector-edited; the translators' come from the content, `translation.packs[].requires`), four branches (`UpgradeSO.branch`, `UpgradeBranch`: Desk equipment, Interview, Portals, Contacts). A node is **locked** until every prerequisite is **owned** (delivered). The first cut (§6.1): the Analysis Scanner needs the Auto-Feed Scanner; Portal 04's repair needs 02's, and 05's needs 04's; the Return Gate's repair, Interview Protocols, the four translators and Diplomatic Contacts need nothing. | Chains where the fiction reads naturally (the analysis lamp fits the feeder's frame; the gallery's power runs through the concourse), none where it would hurt the timing the game relies on: the translators stay open so a translator ordered on day 4 still arrives with the displaced on day 5 (T I3). Every prerequisite is a knob. |
| OR3 | **Drawn visually** (§6.2): the tree is nodes and links on a scrolling canvas: one horizontal band per branch, prerequisite tiers left to right, siblings stacked; each node an icon, a name and its price or state; links from a prerequisite to its dependants. **States**: Locked (greyed, a padlock, "Needs: …"), Orderable (its price; the price in the alert ink when the wallet is short, "Not enough cr"), In transit (a clock badge, "arrives day N"), Owned (a tick). A detail card beside the canvas (the form TC-980's row: blurb, price, prerequisites, state) holds the Order and Cancel buttons. `UpgradeTree.Layout` (Domain) places the nodes. | Saleh: "drawn VISUALLY (nodes and links, icons, states: locked, orderable, in transit, owned), not a text list". A pure layout keeps the drawing testable. |
| OR4 | **Ordering.** "Order" charges the price at once (the one price rule, `HomeEconomy.UpgradeCost` over `ShopPrices.Discounted`) and marks the node In transit; "Cancel" refunds it the same day only. A node locked, owned or in transit cannot be ordered. | Paying at order is today's shop rule; Cancel is the undo for a misclick. |
| OR5 | **Delivery at the start of the next day**: in `DayCycle.AdvanceNight`, after the day turns, every order placed before the new day is delivered: the upgrade is owned (`WorldState.UnlockUpgrade`), its unlock effect is activated from the new day, and the order is marked delivered; it is then in the day-start snapshot, so every upgrade is in force from the same office day a Home purchase was, and a repaired portal is in service that day. The four repairs (`repair_portal_02`, `repair_return_gate`, `repair_portal_04`, `repair_portal_05`: 150, 200, 250 and 350 cr, branch Portals, no effect) are ordinary nodes. | "they arrive next day"; the game and the balance simulation share `DayCycle`. |
| OR6 | **A delivery notice** in Mail (`MailKind.Delivery`, appended): one memo per delivery day listing what arrived, regenerated from the order log by id (P ML2). | Saleh: "a Mail notice". |
| OR7 | **The clerk's statement** (TC-960): an order joins the day's purchases when placed, a cancellation takes it out. The **order log** is `WorldState.orders` (additive; a cancelled order is removed). | The statement already has a purchases column. |
| OR8 | **The translators' notice** (day 4's paper): "Speech translators are sold at Home tonight" becomes "Order a Speech translator in Orders today and it arrives tomorrow." | The timing is unchanged (OR2's reason). |
| OR9 | The window opens maximised (1440 × 988 u; restored 1120 × 820, `DesktopConfigSO`); the canvas scrolls both ways; the arrow keys move between nodes along the links and Enter orders (P KB4). The debug panel's "unlock upgrade" stays immediate. | A tree needs room; every mouse action has a key. |

### 1.7 Home: house upgrades (HM)

| Id | Decision | Why |
|---|---|---|
| HM1 | **Home keeps** the expenses, family care, the slot machine and sleep, and its shop step becomes the **House** step (Expenses → House → Slot → Sleep): the same panel (`ShopPanel`, the art `panel_shop.png`), its rows and its page row, listing only `venue = Home` upgrades. | Saleh's Q10: house upgrades "stay at Home, not in Orders". Reusing the panel keeps the art and the paging. |
| HM2 | **The first house upgrades** (§7; hand-authored `UpgradeSO`s, every number an Inspector knob): Air Filter (120 cr, the nightly sickness chance −0.05), Water Purifier (160 cr, −0.05), Medicine Cabinet (100 cr, each treatment 3 cr cheaper), Insulation (180 cr, the daily household expense 3 cr lower). Bought at Home: owned at once, in force from the next night (tonight's bills and sickness were settled when Home opened). | "house upgrades that reduce sickness and similar household improvements". The effects act on the family knobs that exist (`conditionWorsenChance`, `conditionCareCost`, `baseDailyExpense`). |
| HM3 | **How they act**: each house upgrade's unlock effect carries one of three new timed effect ops (`EffectOpType`, appended: `SicknessChance`, `CareCost`, `HouseholdExpense`), summed by `TimelineEffects.SumFloat` like the pay rate; `HomeEconomy` adds the sums to the config's values through one Domain rule, `HomeRules.Adjusted(value, bonus)` (never below 0). | The effect system already carries Diplomatic Contacts' pay boost; no second modifier path. |

### 1.8 Content (CN)

| Id | Decision | Why |
|---|---|---|
| CN1 | `agency.portals[]`, `days[].portals[]` and `translation.packs[].requires` are row-shaped for the content spreadsheet (T §15's row rules), with `ContentSheetMap` entries and the template rewritten. | Rule 4: Saleh edits the service rules and the routes in the sheet. |
| CN2 | **The Directorate's routes** (§9): every day names one route for each departure portal (01, 02, 04, 05), distinct, each in that day's world; 01's route is open (RT4); the routes of 02, 04 and 05 are sometimes closed on purpose. | Every repaired portal always has a route, and the CLOSED state shows on the rings a player chose to repair. |
| CN3 | **No new news lines**: the delivery memo announces a repair, and the board and the rings show the day's routes. | Nothing said twice. |

### 1.9 The seams (SK: the delayed destination check; SM: Saleh's next portals pass)

| Id | Seam | What the later pass adds there (its design: git, this file at `78c5e60` for SK, `097f20c` for SM1) |
|---|---|---|
| SK1 | **The destination check.** `PortalDay` already knows every in-service route and whether it is closed. | A `TravelRuleType.PortalRouting` directive (a citizen's destination must be on the board: `CaseFacts.UnservedDestination`, `DirectiveFault.NoOpenPortal`, a citation, its first-day maker and roll), and the closure's planned violator bound for a CLOSED route. v2's RC1-RC6. |
| SK2 | **The destinations.** `CaseFactory.PickEraFromPlan` and `PickPlace` still draw over the whole world. | A citizen's destination drawn over the open routes by era weight, the kind drawn first (`PortalDraw`); v2's GN1-GN3. |
| SK3 | **The departure fallback.** `PortalDay.DepartureFor` sends a traveller no open portal serves, and the displaced before the Return Gate's repair, through portal 01. | The hold or deny rule for those travellers replaces the fallback there. |
| SK4 | **The lie and the evidence rule.** Nothing is built. | L11 `Rerouted` (the account books a route off the board; proven against the record's Booked departure) and L5's worksite among the routes; v2's RC7-RC9. |
| SM1 | **Route requests** (clerk-set routes; Saleh: "knowing we are going to make it change"). `PortalSchedule.Resolve` takes the day's route requests (portal → place); `BuildToday` fills them from the Directorate's routes only. | The clerk's requests read before the Directorate's, a `WorldState` store, the app's Era and Location choosers; v1's RT3-RT8 and PA1-PA6. |
| SM2 | **The service state** (breakdowns; "They might break in the future again"). `PortalSchedule.InService(portal, day, repaired)` is the one place a portal's state is decided. | A breakdown rule there, a record of breakdowns, a repair that can be ordered again. |

## 2. The portals over the run

| Day | In service by day | Can be repaired (Orders) | What the player sees |
|---|---|---|---|
| 1 | 01 (Periclean Athens) | 02, the Return Gate, then 04 (after 02), then 05 (after 04) | the board: `01` glowing with its route, four rows UNDER MAINTENANCE; every accepted traveller leaves through 01 |
| 2 on | 01, plus each portal whose repair was delivered | the rest | each repaired ring glows with its route; a repaired ring whose route is closed that day shows nothing inside and reads CLOSED |
| 5 | as day 2 | | the displaced arrive: through the Return Gate if it is repaired (its own effect), else through 01 |
| 9 | as day 2 | | D's Desk 4 closes; its portal 05 is Desk 3's, under maintenance unless repaired |

A careful player can afford a repair from day 2 (the run starts with 50 cr, and a perfect wallet grows about 110 cr a day, D E4).

## 3. The schedule, worked (day 2)

`PortalSchedule.Resolve(portals, day, repaired, requests, closed)`: each portal's state (`InService`: its `fromDay` reached, or its `repair` owned at the day's start); each in-service departure portal runs its request (the Directorate's route), marked closed when a closure of the day forbids it. `DepartureFor(claim, displaced)` then reads it (PO4).

Day 2: the world is Ancient and Medieval × Egypt, Iraq, Greece, Italy, China and Britain; the closure is New Kingdom Egypt. The Directorate's routes: 01 Periclean Athens, 02 New Kingdom Egypt, 04 Florentine Republic, 05 Anglo-Saxon England. This player repaired 02 and the Return Gate on day 1.
- In service: 01, 02, 03. Under maintenance: 04, 05 (04 orderable, 05 locked).
- The board: `01  ANCIENT  PERICLEAN ATHENS` / `02  ANCIENT  NEW KINGDOM EGYPT  CLOSED` / `03  RETURN GATE` / `04  UNDER MAINTENANCE` / `05  UNDER MAINTENANCE`.
- The rings: 01 glows; 02 is empty, at its own colour; 03 shows the Return Gate's effect; 04 and 05 are dimmed.
- An accepted citizen bound for Periclean Athens leaves through 01 (its route); one bound for Babylonia also leaves through 01 (the fallback); one bound for New Kingdom Egypt is the day's closure violator, as today, and if accepted by mistake leaves through 01 (02 is closed). No displaced traveller comes before day 5.

## 4. The Portals app

```
╔═ PRT Portals ═══════════════════════════════════════════════════════════════════[_][□][X]╗
║ ┌─ TC-970 ─────────────────────────────────────────────────────────────────────────────┐ ║
║ │ (seal) TEMPORAL CUSTOMS · Debt Relief Departures                          TC-970      │ ║
║ │ PORTAL SCHEDULE · DESK 3                                   15 MAR 2150 · DAY 2        │ ║
║ │ No.  Ring         Era        Place                 State                              │ ║
║ │ 01   Front        Ancient    Periclean Athens      In service                         │ ║
║ │ 02   Rear left    Ancient    New Kingdom Egypt     CLOSED today: Embargo: no travel   │ ║
║ │                                                    to New Kingdom Egypt today.        │ ║
║ │ 03   Rear right   –          Return Gate           In service                         │ ║
║ │ 04   Upper left   –          –                     Under maintenance · repair it in   │ ║
║ │                                                    Orders (250 cr)                    │ ║
║ │ 05   Upper right  –          –                     Under maintenance · locked: repair │ ║
║ │                                                    04 first                           │ ║
║ │ ──────────────────────────────────────────────────────────────────────────────────── │ ║
║ │ Routes are set by the Directorate. Until the Return Gate is repaired, the displaced   │ ║
║ │ leave through Portal 01.            Issued by Temporal Customs · Desk 3  ║║│║║ …     │ ║
║ └──────────────────────────────────────────────────────────────────────────────────────┘ ║
╚══════════════════════════════════════════════════════════════════════════════════════════╝
```

- The form is drawn by `FormView` from a `FormSpecSO` (TC-970), like every PC page (P FO9), from the day's `PortalDay`, the order log and the tree's states.
- A portal under maintenance shows no era or place: it runs nothing today.
- Its rows are not search entries and not compare-pickable (the check pass decides whether the Rules tab carries the schedule, SK1).

## 5. The Departure Board and the rings

### 5.1 The board

| Part | Rule |
|---|---|
| Anchor | `DepartureBoard`: `Anchor_DepartureBoard`, else the bare name `16 Departure board blank display` (its sprite's bounds). No default pose: missing in the 3D room. |
| Text | `BoardRows` (world-space TMP, `Gameplay` sorting layer, built in `OfficeGameplay` by Build Office UI; auto-sized, every row the same size), its rect the anchor's bounds inset by `DeskConfigSO.hallBoardInset` (first cut 8 % a side), re-fitted in `LateUpdate` when the bounds move. Inks `hallBoardInk` and `hallBoardStateInk` (rich text). Text: `PortalBoardText.Rows(PortalDay, eraName, placeName)` (Visuals, pure). |
| Click box | on `Interactable` over the bounds: hover outline; tooltip `PortalBoardText.Tooltip(…)`: "DEPARTURE BOARD", then one line per portal ("01 Front: Periclean Athens, Ancient", "02 Rear left: New Kingdom Egypt, Ancient: CLOSED today", "03 Rear right: Return Gate, under maintenance", "05 Upper right: under maintenance"). The tooltip template grows with its lines (a content size fitter), checked at 720p. |
| Evening | The text is lit; the display darkens with the evening, so the contrast only rises; the probe measures both inks at the full evening. |
| The claim strip | It covers the display's top few pixels at 1080p; the inset keeps the rows below it; the probe checks both resolutions. |

### 5.2 The rings

| State | Metal ring (`SetLayerTint`) | Inside the ring (`PortalEffect`) |
|---|---|---|
| Departure portal in service, route open | the art's colour | the glow (VX1); a departure flares it (VX4) |
| Departure portal in service, route CLOSED | the art's colour | nothing (VX3) |
| Under maintenance (any portal) | the art's colour (VX2, amended) | nothing (VX2) |
| The Return Gate in service | the art's colour | the Return Gate's spiral (VX5); a displaced departure flares it |

Placement (VX6): the effect's centre is the metal ring's renderer bounds' centre and its diameter `hallPortalGlowSize` (0.85) × the bounds' width, so its edge hides under the frame; its sorting layer is Default and its order the bay's order − 1, read from `AnimeHallPresentation.FindLayer(bay id).sortingOrder` at the day's start; it follows the bounds each `LateUpdate` if the art pans. Its material is unlit and additive (the evening does not dim it).

### 5.3 The scene contract's new rows (`docs/SCENE_CONTRACT_GAMEPLAY.md`, in plan phase PR4)

- The anchors table: `DepartureBoard` | the Departure Board: its rows (a gameplay text) and its click box with the portals' tooltip | no fallback in the room | `Anchor_DepartureBoard` over a board's display.
- The hall's table: `DepartureBoard` | `16 Departure board blank display` | the display's sprite bounds; the rows sit inset below the claim strip.
- The hooks section: "The gameplay layer drives **time** and **the portal rings**: `AnimeHallPortalLink` tints each metal ring through `SetLayerTint` (dimmed under maintenance) and draws a `PortalEffect` inside each ring. The effects are the one gameplay drawing on the art's **Default** sorting layer: sorting order = the ring's secure-bay order − 1, read from the bay's renderer, so they draw under the bay's front fence, the metal ring (the gate frame) and the painted glass. A renamed or renumbered layer is a `DeskConfigSO.hallPortalLayers` edit; the art side keeps each ring's centre free of other layers at the bay's order − 1."
- The rules for the gameplay side: "One sorting layer is the gameplay layer's: `Gameplay` … except the portal effects (above)."

## 6. Orders: the upgrade tree

### 6.1 The catalogue (the first cut; every price and prerequisite is a knob)

| Branch | Node (id) | Price | Needs | Effect (unchanged unless new) |
|---|---|---|---|---|
| Desk equipment | Auto-Feed Scanner (`scanner_autofeed`) | 200 cr | – | handed-over papers scan themselves (P SC3) |
| Desk equipment | Analysis Scanner (`adv_scanner`) | 300 cr | Auto-Feed Scanner | a scan by hand marks one contradiction (P SC4) |
| Interview | Interview Protocols (`interview_protocols`) | 120 cr | – | the birth-date question |
| Interview | Near East, Mediterranean, East Asia, Northern Europe Translators: Speech (`tr_<pack>_spoken`) | 80 cr each (`translation.packs[].spokenCost`) | – (`translation.packs[].requires`, blank) | the region's speech in English (T I4) |
| Portals | Portal 02 repair (`repair_portal_02`), rear left | 150 cr | – | 02 in service (new) |
| Portals | Return Gate repair (`repair_return_gate`), rear right | 200 cr | – | 03 in service: the displaced leave through it (new) |
| Portals | Portal 04 repair (`repair_portal_04`), upper left | 250 cr | Portal 02 repair | 04 in service (new) |
| Portals | Portal 05 repair (`repair_portal_05`), upper right | 350 cr | Portal 04 repair | 05 in service (new) |
| Contacts | Diplomatic Contacts (`diplo_contacts`) | 350 cr | – | a better pay rate (its unlock effect) |

### 6.2 The window

```
╔═ ORD Orders ════════════════════════════════════════════════════════════════════════════════[_][□][X]╗
║ Wallet 312 cr · Orders arrive at the start of tomorrow's shift.                                     ║
║ ┌──────────────────────────────────────────────────────────────────────┐ ┌─ TC-980 ───────────────┐ ║
║ │ DESK EQUIPMENT                                                       │ │ PORTAL 04 REPAIR        │ ║
║ │  ┌────────────────┐        ┌────────────────┐                        │ │ Upper-left ring         │ ║
║ │  │[ic] Auto-Feed  │───────▶│[ic] Analysis   │                        │ │ 250 cr                  │ ║
║ │  │ ✓ Owned        │        │ 300 cr         │                        │ │ Needs: Portal 02 repair │ ║
║ │  └────────────────┘        └────────────────┘                        │ │  (owned)                │ ║
║ │ INTERVIEW                                                            │ │ The ring back in        │ ║
║ │  ┌────────────────┐  ┌────────────────┐                              │ │ service from the day    │ ║
║ │  │[ic] Protocols  │  │[ic] Near East  │ … three more translators     │ │ the repair arrives.     │ ║
║ │  │ 120 cr         │  │ ⏱ arrives day 4│                              │ │                         │ ║
║ │  └────────────────┘  └────────────────┘                              │ │ [ Order ]               │ ║
║ │ PORTALS                                                              │ └─────────────────────────┘ ║
║ │  ┌────────────────┐        ┌────────────────┐       ┌──────────────┐ │                             ║
║ │  │[ic] Portal 02  │───────▶│[ic] Portal 04  │──────▶│🔒 Portal 05  │ │                             ║
║ │  │ ✓ Owned        │        │ 250 cr  ▣      │       │ Needs 04     │ │                             ║
║ │  └────────────────┘        └────────────────┘       └──────────────┘ │                             ║
║ │  ┌────────────────┐                                                  │                             ║
║ │  │[ic] Return Gate│                                                  │                             ║
║ │  │ 200 cr         │                                                  │                             ║
║ │  └────────────────┘                                                  │                             ║
║ │ CONTACTS                                                             │                             ║
║ │  ┌────────────────┐                                                  │                             ║
║ │  │[ic] Diplomatic │                                                  │                             ║
║ │  │ Not enough cr  │                                                  │                             ║
║ │  └────────────────┘                                                  │                             ║
║ └──────────────────────────────────────────────────────────────────────┘                             ║
╚═══════════════════════════════════════════════════════════════════════════════════════════════════════╝
```

- **Layout** (`UpgradeTree.Layout`, Domain): one band per branch in `UpgradeBranch` order; in a band, a node's tier is the length of its longest prerequisite chain inside the band (tier 0 at the left); nodes of a tier stack top to bottom by cost, then id. Cells are `DesktopConfigSO` knobs (first cut 220 × 96 u, a node 200 × 80 u, a band head 32 u). A prerequisite in another branch draws its link across the bands.
- **A node**: its icon (the upgrade icon slot, 48 u), its name (at most two lines), and its price or its state line; the plate and badges code-drawn in the theme's roles (themed chrome: the tree is the app's own UI; the detail card is a form, unthemed).
- **Links**: 3 u elbow lines from a prerequisite's right edge to a dependant's left edge (one `Graphic` drawing every segment); dim until the prerequisite is owned, the accent colour after.
- **States** (`Orders.StateOf`): Locked (greyed plate, padlock, "Needs: Portal 04 repair"), Orderable (price), TooDear (price in the alert ink, "Not enough cr"; the Order button disabled), InTransit (clock badge, "arrives day N"; Cancel on the day it was placed), Owned (tick; "Delivered day N" from the log, or "Owned" when it came another way).
- **Keys**: the arrows move the selection along links and within a tier, Enter opens the detail card's Order, Esc returns to the canvas (the Escape chain, P KB3).

### 6.3 Order, cancel, deliver

| Step | Rule |
|---|---|
| Order | `Orders.StateOf` must be Orderable. The money moves now, the entry `{upgradeId, orderedDay = today, price}` joins `WorldState.orders`, and the day's statement row's purchases grow. |
| Cancel | Only an order placed today and not delivered: the charged price comes back and the entry leaves the log. |
| Deliver | `DayCycle.AdvanceNight`, after `day++`: `Orders.Due(log, day)` in log order: `UnlockUpgrade`; its unlock effect activated with start day = the new day (`TimelineService.ActivateEffect`, instant ops applied); `deliveredDay = day`. The snapshot and the schedule are built when the office loads, so each delivery counts that day. |
| Mail | `Mailbox` builds one Delivery memo per `deliveredDay` from the log ("Delivered: Auto-Feed Scanner, Portal 02 repair."), id `delivery:{day}`. |
| Saves | The end-of-shift save holds pending orders; Continue from Home keeps them; the night delivers them. An older save loads an empty log and keeps every upgrade it owns. |
| The simulation | orders nothing (it bought nothing at Home either); the night's delivery runs through the same `DayCycle` step. |

## 7. Home: the House step

| House upgrade (id) | Price | Its unlock effect (a permanent timed op) | What it changes |
|---|---|---|---|
| Air Filter (`house_air_filter`) | 120 cr | `SicknessChance` −0.05 | each family member's nightly chance to worsen: 0.25 → 0.20 |
| Water Purifier (`house_water_purifier`) | 160 cr | `SicknessChance` −0.05 | 0.20 → 0.15 with the filter |
| Medicine Cabinet (`house_medicine_cabinet`) | 100 cr | `CareCost` −3 | a treatment: 8 → 5 cr |
| Insulation (`house_insulation`) | 180 cr | `HouseholdExpense` −3 | the daily base expense: 10 → 7 cr |

- **The flow:** Expenses (tonight's bills and sickness, then Treat) → **House** (the four rows, Buy; "in force from tomorrow night") → Slot → Sleep. A purchase is owned at once, charged at once, written to the day's purchases on the statement (as the shop's were).
- **The rules:** `HomeEconomy.ApplyDailyExpenses` adds `SumFloat(HouseholdExpense)` to the base expense, `GetCareCost` adds `SumFloat(CareCost)`, `AdvanceFamilyConditions` adds `SumFloat(SicknessChance)` to the worsen chance, each through `HomeRules.Adjusted(value, bonus)` (never below 0; a cost rounded half to even).
- **The assets:** four hand-authored `UpgradeSO`s (`venue` Home) and their four `EffectSO`s (`Effect_House_*`, one op each, duration −1), listed in the content library; every number is an Inspector knob.
- **The art:** the House rows keep the Home upgrade icon slot (`Assets/Art/UI/Resources/Home/upgrade_<id>.png`), now for the four house items.

## 8. Data models

### 8.1 Domain (pure, EditMode-tested; the names are proposals, and the build re-reads the code)

```csharp
/// A place of the world by its content keys: a route (RT1).
public readonly struct PlaceKey : IEquatable<PlaceKey> { public readonly string Country, Era; public static bool TryParse(string text, out PlaceKey key); }

/// A portal's job (agency.portals[].role). Serialized in the content library: append only, pinned.
public enum PortalRole { Departures, Returns }

/// One ring of the hall (agency.portals[], written by Generate World into the library's agency block).
[Serializable] public sealed class PortalSpec { public int number; public string name; public PortalRole role; public int fromDay; public string repair; }

/// A portal's state today. Runtime only; a breakdown pass may append (SM2).
public enum PortalState { InService, UnderMaintenance }

public readonly struct PortalRoute { public int Number; public string Name; public PortalRole Role; public PortalState State; public PlaceKey? Place; public bool Closed; }

/// The day's schedule, fixed at the day's start (RT3).
public sealed class PortalDay
{
    public IReadOnlyList<PortalRoute> Portals { get; }   // every portal, in number order
    /// Where an accepted traveller leaves (PO4): the displaced by the Returns portal in service, else the default
    /// (01); a citizen by the in-service departure portal whose open route is the claim, else the default. SK3.
    public int DepartureFor(PlaceKey claim, bool displaced);
}

public static class PortalSchedule
{
    bool InService(PortalSpec portal, int day, Func<string, bool> repaired);             // SM2
    PortalDay Resolve(IReadOnlyList<PortalSpec> portals, int day, Func<string, bool> repaired,
                      IReadOnlyDictionary<int, PlaceKey> requests, Func<PlaceKey, bool> closed);   // SM1
    List<string> PortalProblems(IReadOnlyList<PortalSpec> portals, Func<string, bool> upgradeExists);
    List<string> DayProblems(string day, IReadOnlyList<PortalSpec> portals, IReadOnlyDictionary<int, PlaceKey> directorate,
                             Func<PlaceKey, bool> inWorld, Func<PlaceKey, bool> closed);
}

// The tree
public enum UpgradeVenue { Orders, Home }                                  // UpgradeSO.venue: serialized, pinned
public enum UpgradeBranch { Desk, Interview, Portals, Contacts }           // UpgradeSO.branch: serialized, pinned
public readonly struct TreeNode { public string Id; public UpgradeBranch Branch; public IReadOnlyList<string> Requires; public int Cost; }
public readonly struct TreeCell { public string Id; public int Band, Tier, Slot; }
public static class UpgradeTree
{
    IReadOnlyList<TreeCell> Layout(IReadOnlyList<TreeNode> nodes);
    bool Unlocked(TreeNode node, Func<string, bool> owned);                 // every prerequisite owned
    List<string> Problems(IReadOnlyList<TreeNode> nodes, Func<string, UpgradeVenue?> venueOf);   // unknown id, cycle, another venue
}

public enum OrderState { Locked, Orderable, TooDear, InTransit, Owned }
public static class Orders
{
    OrderState StateOf(string upgradeId, bool owned, bool unlocked, IReadOnlyList<OrderEntry> log, int money, int price);
    OrderEntry Place(List<OrderEntry> log, string upgradeId, int day, int price);   // null unless orderable
    int Cancel(List<OrderEntry> log, string upgradeId, int day);                   // the refund; 0 when nothing placed today is pending
    IReadOnlyList<OrderEntry> Due(IReadOnlyList<OrderEntry> log, int day);         // ordered before the day, not delivered
}

// Home
// EffectOpType (appended; pinned from now on): SicknessChance, CareCost, HouseholdExpense.
// HomeRules.Adjusted(float value, float bonus): value + bonus, never below 0.

// MailKind.Delivery (appended); Mailbox builds one memo per delivery day from the log.
// OfficeAnchorId.DepartureBoard (appended: 23), pinned.
```

Visuals: `PortalBoardText.Rows(...)` and `Tooltip(...)` (pure strings, tested).

### 8.2 Saves (`WorldState`, additive; the save version stays 3)

```csharp
/// Every order placed and not cancelled (the Orders app): pending until delivered at the start of the day after it was placed, then kept as the log. An older save loads it empty.
public List<OrderEntry> orders = new();
[Serializable] public sealed class OrderEntry { public string upgradeId; public int orderedDay; public int price; public int deliveredDay; }
```

A repair and a house upgrade are owned upgrades (`unlockedUpgradeIds`); nothing else is saved for the portals, whose schedule is recomputed at each day's start.

### 8.3 Content (`world_source.json`, through Generate World)

```json
"agency": {
  "portals": [
    {"number": 1, "name": "Front",       "role": "Departures", "fromDay": 1},
    {"number": 2, "name": "Rear left",   "role": "Departures", "repair": "repair_portal_02"},
    {"number": 3, "name": "Rear right",  "role": "Returns",    "repair": "repair_return_gate"},
    {"number": 4, "name": "Upper left",  "role": "Departures", "repair": "repair_portal_04"},
    {"number": 5, "name": "Upper right", "role": "Departures", "repair": "repair_portal_05"}
  ]
},
"days": [
  {"day": 2, "portals": [{"portal": 1, "country": "greece", "era": "ancient"}, {"portal": 2, "country": "egypt", "era": "ancient"},
                         {"portal": 4, "country": "italy", "era": "medieval"}, {"portal": 5, "country": "britain", "era": "medieval"}]}
],
"translation": {"packs": [{"id": "near_east", "displayName": "Near East", "spokenCost": 80}]}
```

- Generate World writes `agency.portals` into the library's agency block (`PortalSpec`s), each day's routes into `DayPlanSO.directorateRoutes` (portal number and place profile reference, so a renamed place cannot dangle), and on the four generated translators `venue` Orders, `branch` Interview and `requires` from `translation.packs[].requires` (blank today). `fromDay`, `repair` and `requires` are omitted when blank.
- **`ContentSheetMap`** (and the template rewritten): under `agency`, `Rows("agencyPortals", "portals", Key("number"), Int("number").Required(), Text("name"), Text("role").OneOf("Departures", "Returns"), Int("fromDay").Omit().Note("the day it enters service by itself; blank: only by its repair"), Text("repair").Omit().Note("the upgrade id whose delivery puts it in service"))`; under `days`, `Rows("dayPortals", "portals", Int("portal").Required(), Text("country").Ref("countries"), Text("era").Ref("eras")).Note("the Directorate's route for each departure portal that day")`; on `translation.packs`, `List("requires").Omit().Note("upgrade ids the translator's node needs")`.

### 8.4 Knob assets (Inspector; Generate World never writes them)

- New `Assets/Data/Upgrades/`: `Upgrade_RepairPortal02`, `Upgrade_RepairReturnGate`, `Upgrade_RepairPortal04`, `Upgrade_RepairPortal05` (venue Orders, branch Portals; 150, 200, 250, 350 cr; `requires` as §6.1); `Upgrade_House_AirFilter`, `…WaterPurifier`, `…MedicineCabinet`, `…Insulation` (venue Home; §7), with `Assets/Data/Effects/Effect_House_*`.
- Every hand-authored `UpgradeSO` gains `venue`, `branch` and `requires` (§6.1).
- `DeskConfigSO` (its anime hall group): `hallBoardInk`, `hallBoardStateInk`, `hallBoardInset`, `hallPortalLayers` (per portal: bay, ring, glass layer ids), `hallPortalIdleTint`, `hallPortalGlowTint`, `hallReturnGateTint`, `hallPortalGlowSize`, `hallPortalPulseScale`, `hallPortalPulseSeconds`.
- `DesktopConfigSO`: the Portals and Orders windows' sizes; the tree's cell, node and band sizes.

## 9. The Directorate's routes, days 1-15

One route per departure portal per day. 01's route is open every day (RT4); **(closed)** marks a route closed that day on purpose, so a player who repaired that portal sees it CLOSED. With D, 01 runs each citizen beat's place on its day, ready for the check pass (SK1).

| Day | Closures that day | 01 Front | 02 Rear left | 04 Upper left | 05 Upper right |
|---|---|---|---|---|---|
| 1 | – | Periclean Athens | Republican Rome | New Kingdom Egypt | Babylonia |
| 2 | New Kingdom Egypt | Periclean Athens | New Kingdom Egypt (closed) | Florentine Republic | Anglo-Saxon England |
| 3 | Northern Song Kaifeng, Tokugawa Edo | Elizabethan England | Northern Song Kaifeng (closed) | Abbasid Baghdad | Late Ming Suzhou |
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

Days 1-6 land with this spec (plan phase PR3); days 7-15 with D's content (plan phase PR5), replaying day 6's until then (`DayPlans.Pick`). The closures are today's (days 1-6) and D §3's (days 7-15), unchanged.

## 10. Balance knobs

### 10.1 Where each one lives

| Knob | Where Saleh edits it | First cut |
|---|---|---|
| Each portal's first day, repair and role | the sheet `agencyPortals` | §8.3 |
| The four repairs' prices and prerequisites | their `UpgradeSO` assets (Inspector) | 150, 200, 250, 350 cr; §6.1 |
| Every other Orders node's price and prerequisites | its `UpgradeSO` asset; the translators' `translation.packs[].spokenCost` and `requires` (the sheet) | §6.1 |
| The house upgrades' prices and effects | their `UpgradeSO` and `EffectSO` assets (Inspector) | §7 |
| The Directorate's routes per day (which are closed on purpose) | the sheet `dayPortals` | §9 |
| The board's inks and inset; the rings' tints, glow size, pulse; the layer names | `DeskConfigSO` (Inspector) | FFF2D9 and an amber-red; 8 %; a 45 % grey, a pale cyan, an amber; 0.85; 1.3 × for 0.8 s |
| The two windows' and the tree's sizes | `DesktopConfigSO` (Inspector) | §1.3, §1.6, §6.2 |
| The one penalty, the free warning, the family knobs | `GameConfigSO` (unchanged) | – |

### 10.2 Balance hooks for phase 23's simulation

| Id | Hook | Why |
|---|---|---|
| BX1 | The simulation orders nothing and buys no house upgrade by default; it runs the same `BuildToday` and `DayCycle` steps. | The measured run is the game's own; portals change no traveller in this version. |
| BX2 | A "buyer" policy beside the three ways to play: each night buys the cheapest affordable house upgrade, and each shift orders the cheapest orderable node. | Measures the house upgrades' payback against the family's drain and the wallet curve, and what the tree's prerequisites cost a run. |

## 11. Art

### 11.1 `docs/ART_ASSET_LIST.md` changes (made in the plan's phases)

**Section 1, the office**, two rows:

| Prop | Anchor, and what the game finds | Status |
|---|---|---|
| The departure board: its display stays blank; the game draws the rows | `DepartureBoard`: the hall's `16 Departure board blank display` | done: keep the display text-free |
| The five portal rings: the game dims a ring under maintenance and draws the glow and the Return Gate's spiral inside it, under the bay, the ring and the glass (orders: the bay's − 1) | the layers `38`-`47` and `53`-`57` | done; keep each ring's centre free of other layers at the bay's order − 1 |

**Section 2, the 2D layers over the office**, two rows: **Portal glow** (inside an open departure ring; `Assets/Art/UI/Resources/Office/portal_glow.png`; now a code-drawn radial glow; deliver 512 × 512, a soft swirl of light on transparent, greyscale, the game tints it; Tier 1; missing) and **Return Gate spiral** (`Office/portal_return_glow.png`; the same format, an inward spiral; Tier 1; missing).

**Section 3, the PC desktop:** "Desktop icons × 6" becomes "× 8" (+ `portals`, a ring; `orders`, a parcel with a tick); a new row **Orders branch glyphs × 4** (`Assets/Art/UI/Resources/Orders/branch_<id>.png` for `desk`, `interview`, `portals`, `contacts`; 128 × 128 greyscale; Tier 1; missing); a new row **Orders node icons × 12** (`Assets/Art/UI/Resources/Orders/upgrade_<id>.png`; the eight office ids, moved from `Resources/Home/` with their metas, and the four repairs, one wrench-over-ring glyph may serve all four; 256 × 256; Tier 1; 2 interim, 10 missing).

**Section 6, Home:** the Shop panel row becomes the **House panel** (the same file, `panel_shop.png`); the "Upgrade icons × 8" row becomes **House upgrade icons × 4** (`Assets/Art/UI/Resources/Home/upgrade_<id>.png` for `house_air_filter`, `house_water_purifier`, `house_medicine_cabinet`, `house_insulation`; 256 × 256; Tier 1; missing).

**Totals:** section 2 goes from 5 to 7; section 3 from 16 + 11 (27) to 34 + 11 (45: the eight office icons moved in from section 6, plus 10 new files); section 6 from 19 + 7 (26) to 15 + 7 (22); the total from 81 + 19 (100) to 97 + 19 (116).

### 11.2 The scene contract

§5.3's rows, in `docs/SCENE_CONTRACT_GAMEPLAY.md`.

## 12. Tests and golden masters

### 12.1 EditMode (Domain and Visuals first; the plan names each test)

- `PortalScheduleTests`: `InService` (by day; by a delivered repair; both; neither); `Resolve` (in service runs its request; under maintenance runs nothing; a closed route is marked; the Returns portal has no route; every portal listed); `DepartureFor` (the displaced by the Return Gate, else 01; a citizen by the open route's portal, else 01; never a closed route's portal); `PortalProblems`; `DayProblems` (one route per departure portal, distinct, in the world, 01's open, none for the Returns portal).
- `UpgradeTreeTests`: `Layout` (bands in branch order, tiers by the longest chain, slots by cost then id, a cross-branch link); `Unlocked` (every prerequisite owned; in transit is not owned); `Problems` (unknown id, cycle, a prerequisite at another venue).
- `OrdersTests`: every state (Locked first); `Place` (refuses locked, owned, in transit); `Cancel` (today only, the price back); `Due`.
- `MailboxTests`: one Delivery memo per delivery day, listing the items; a pending order sends nothing.
- `HomeRulesTests`: `Adjusted` (adds the bonus; never below 0).
- `PortalBoardTextTests` (Visuals): the rows (number order, era then place, CLOSED, RETURN GATE, UNDER MAINTENANCE); the tooltip.
- `DesktopLayoutTests` and `DesktopIconPlaceholderTests`: eight icons; the two new glyphs.
- `SerializedEnumsTests`: `OfficeAnchorId.DepartureBoard`, `PortalRole`, `UpgradeVenue`, `UpgradeBranch`, `EffectOpType` (pinned from now on, with its three new members), `MailKind` (if stored).
- `ContentSheetMapTests`: `agencyPortals`, `dayPortals`, `translation.packs[].requires`.

### 12.2 Golden masters (the audit baseline, `docs/reviews/audit-baseline`)

| Golden | Effect | Why |
|---|---|---|
| `cases.txt` (the static case dump) | **unchanged, byte-identical** | no traveller, destination, closure or lie changes (the check is delayed) |
| The play transcript and saves | change | Orders instead of the Home shop (the audit play job orders at the PC), the House step, the delivery memo; `orders` in the save |
| Scene dumps | `OfficeGameplay` (two icons and windows, the tree's templates, the board's text and click box, the portal effects and link); `HomeScene` (the House panel's labels) | the builders |
| Data hashes | the upgrades (venue, branch, requires; four repairs; four house upgrades), the house effects, the translators, the day plans (routes), the library (portals, the new upgrades and effects) | Generate World and the new assets |
| Static metrics | new types | – |
| The balance summary | re-run with BX2 | the house upgrades and the tree move money |

## 13. `docs/FEATURES.md` changes (in the commits of the behaviour)

| Section | Line (its first words) | Change |
|---|---|---|
| Run & meta | the save line | + `orders`, additive |
| Run & meta | "Home phase between days" | Expenses → House → Slot → Sleep; the House step sells house upgrades (their effects); the office upgrades move to Orders |
| Office scene — the art office | (new lines) | the Departure Board (anchor, rows, tooltip); the rings' dimming and effects, their sorting under each ring's frame, the departure pulse and the Return Gate's effect |
| Desk | "Desk scanner" | the two scanners are Orders nodes (the Analysis after the Auto-Feed), delivered at the start of the next office day |
| Fake-OS desktop | "Six desktop icons" | eight icons: + Portals, Orders |
| Fake-OS desktop | (new lines) | the Portals app (read-only); the Orders tree (prerequisites, states, order, cancel, delivery); Mail's delivery memo |
| World | the day plans' line | the portals, their service (01 from day 1, the four repairs), the Directorate's routes and their CLOSED state; where an accepted traveller leaves |
| Translation | "Translators" | ordered in Orders, arriving the next office day; the day-4 notice's words |
| Content & tooling | Generate World, the validator, the sheets, the simulation | the new inputs and checks (`PortalProblems`, `DayProblems`, `UpgradeTree.Problems`) and sheets; BX2 |

## 14. Saleh's answers, and the decisions to confirm

### 14.1 Answered (2026-09-30)

| Q | His answer | Applied in |
|---|---|---|
| Scope | "only the destination check" is delayed to a later pass | §0, §1.9 (SK1-SK4); T, D and `cases.txt` untouched |
| Q1 Who sets the routes | the Directorate, for now; the app shows them | RT2, PA1-PA3, SM1 |
| Q2 Unlocks | one portal open on day 1, the rest under maintenance, paid for in the PC; breakdowns later | PO3, OR5, SM2 |
| Q3 The Return Gate | a repair like the others, with its own VFX; until then the displaced leave through 01 | PO3, PO4, VX5, §6.1 |
| Q4 The fault kind; Q13 demand; Q14 L11 | delayed with the check | SK1, SK2, SK4 |
| Q5 A portal on the papers | no | no form changes |
| Q6 The routing rule's first day | delayed with the check; day 2's closure stays on day 2 | §9 (day 2) |
| Q7 Closures | the board shows CLOSED | RT2, BD3, VX3 |
| Q8 A route change's cost | moot | SM1 |
| Q9 Orders | a real tree with prerequisites, drawn visually; paid when ordered; next-day delivery; a Mail notice; repairs are nodes | OR1-OR9, §6 |
| Q10 Home | expenses, care, slots, sleep, plus house upgrades that reduce sickness and similar | HM1-HM3, §7 |
| Q11 The board | gameplay-drawn rows and a tooltip | BD1-BD5 |
| Q12 The rings | open glows, under maintenance dimmed, CLOSED empty, a departure pulses, the Return Gate its own effect, under each ring's frame | VX1-VX7 |
| Q15 What a repair earns | A: nothing more for now | PO6 |

### 14.2 Decisions made on Saleh's behalf (the plan builds the first option; each is a knob or a small edit)

| D | Decision | Options (built first) |
|---|---|---|
| D1 | The tree's prerequisites (OR2) | **A** Analysis after Auto-Feed; repairs 02 → 04 → 05; everything else a root (the translators open for day 4's order). **B** A, plus the translators after Interview Protocols (the day-4 notice then moves to day 3). **C** no prerequisites outside the repairs. |
| D2 | The repairs' prices (OR5) | **A** 150 (02), 200 (Return Gate), 250 (04), 350 (05). **B** one price for all (200). |
| D3 | The house upgrades (HM2, §7) | **A** Air Filter, Water Purifier, Medicine Cabinet, Insulation, with §7's effects and prices. **B** A, plus a Heated Bedroom (the medical drain per condition point −1). **C** only the two sickness items. |
| D4 | The board's rows (BD3) | **A** all five portals, UNDER MAINTENANCE included (one layout all run; the tooltip reads at 720p). **B** only the portals in service (fewer, larger rows). |
| D5 | Where a traveller no open portal serves leaves (PO4) | **A** through 01, until the check pass decides (SK3). **B** through no portal (no pulse). |

## 15. Intent audit

**Against Saleh's words.**
- *"multiple portals and a destination board"*: five portals (PO1) and the board's rows (BD1-BD4).
- *"new portals will unlock"* and *"One portal is open on day 1; the rest are under maintenance and you need to pay for them in the PC"*: 01 from day 1; 02, the Return Gate, 04 and 05 by repairs ordered in the PC (PO3, §6.1). *"They might break in the future again"*: SM2, no behaviour.
- *"the upgrade tree be in the pc now as another icon: you can order the upgrades and they arrive next day"* and Q9: the Orders icon, a visual tree with prerequisites and four states, paid when ordered, delivered the next day, a Mail notice (OR1-OR9).
- *"destination must match portal in the papers"*: **delayed by Saleh** to the check pass; the seams SK1-SK4 hold its design.
- *"destination board should also show it"* and *"an icon that shows the portals and the destination"*: the board and the read-only Portals app (BD3, PA2); setting routes waits (SM1).
- *"the Return Gate to repair, it will have its own VFX"*: a repair node (§6.1), its own effect (VX5), and the displaced through 01 until then (PO4).
- The rings: open glows, under maintenance dimmed, CLOSED empty, a departure pulses, under each ring's frame (VX1-VX6).
- Q10: the House step with sickness-reducing and household upgrades (HM1-HM3).

**Against his design rules.** Rule 1: no fine, fee or penalty is added. Rules 2 and 3: no line, wheel entry or verb is added. Rule 4: every number is in the sheet or an Inspector asset (§10); no authored case changes. Rule 5: untouched.

**Redo or override checks.**
- Overrides P DK1's "six icons" (eight now) and P SC1's "two scanner upgrades in the Home shop" (Orders nodes now; the Analysis Scanner after the Auto-Feed, which P SC2 did not require: D1).
- Replaces the Home shop's contents with house upgrades (the panel, its art and its paging kept).
- Withdraws v2's destination check, L11, generator change and day-2/3 closure move (Saleh's scope decision); T, D and `cases.txt` are untouched.
- The scene contract gains one exception: the portal effects draw on the art's Default sorting layer (VX6).
- Reuses, never duplicates: owned upgrades for repairs and house items, the effect system for the house effects, `HomeEconomy.UpgradeCost` over `ShopPrices.Discounted`, `TimelineService.ActivateEffect`, `DayCycle`, `Mailbox`, `FormView` and `FormSpecSO`, the contract's fallback resolution, the prop tooltip, the art's `SetLayerTint` and `FindLayer`, the Home panel and its paging.

**No-code-path checks.** The service rules, routes, prices, prerequisites, house effects, inks, tints and sizes are data or Inspector knobs. New code is limited to what data cannot express: the schedule and the departure rule, the tree's layout, states and drawing, the order log and delivery, the household ops' sums, the two windows, the board's text and binding, and the ring link and effects.

## 16. The seams

| With | What this spec assumes or asks |
|---|---|
| T (traveller types) | nothing changes in this version; the check pass (SK1-SK4) will change §5.2, §5.3, §5.4 and §6 as `78c5e60` describes. |
| P (the PC redesign) | DK1's count (eight icons); DK4's restore; TH3's flavour labels (+ `icon.portals`, `icon.orders`: 30); SC1 and SC2 (the scanners as Orders nodes, D1); ML1's mail kinds (+ Delivery); FO9's PC pages (+ TC-970, TC-980). The plan updates P's rows in the phase that changes them. |
| D (days 7-15, on `design/days-7-15`) | §3's days gain their routes (§9); `desk4_closes` reads "From today its queue joins Desk 3, and its portal (05, upper right) is Desk 3's, still under maintenance."; nothing else. |
| Phase 23 (balance) | `DayCycle` is the one day path (delivery joins `AdvanceNight`); the house upgrades, the repairs and the tree's prices are phase 23's to tune (BX2). |
| The delayed check pass | SK1-SK4 (its design at `78c5e60`). |
| Saleh's next portals pass | SM1 (clerk-set routes, `097f20c`) and SM2 (breakdowns). |
| The art side | the board's display and each ring's centre stay as they are (§11.1); the two effect textures are new slots; a renamed or renumbered layer is a `DeskConfigSO` edit. |
