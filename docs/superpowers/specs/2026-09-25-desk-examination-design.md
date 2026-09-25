# Examine the papers at the desk: design (piece 10)

*2026-09-25 · decisions E1–E6 made by the coordinator under Saleh's request, refined here by Claude (the X rows), open to his review · spec only (no code); to be built in `E:\unity\NOPE-art` on `feat/desk-exam` from `origin/main` at `d5844d0` (pieces 0–9; piece 9 merged as a fast-forward of `feat/translation`)*

Saleh, verbatim: "I want the dialagoue of the person saying where they want to go and giving the documents to happen in the office not in the pc. the player will use the cursor to pick the documents examane them like in paper please against each other. then then can scan them one by one and perform more complex things in the computer if they want".

The desk becomes the place where a case is worked. The traveller says where they are going in the bubble, and the claim (with their role) stays on a tag at the top of the office. They hand their papers over onto the desk. A click on a paper lifts it into the player's hands, readable, in front of the camera. Two papers can be held side by side, as in Papers, Please, and read against each other. A click on a field row, on an answer in the bubble, or on a garment (through the wheel's Look menu) picks it for the same compare that the PC uses. Both picks light up and an office strip shows MATCH, MISMATCH or DEVIATION LOGGED. When the PC frame opens, the held papers move beside it, so a reference-book row and a paper row can be compared in one view. A stamp on the desk gives the verdict. The PC keeps every function it has (scans, books, Citizen Records, translation, the transcript, the Deviation Report), and the player uses it for the complex work only if they want to.

**Base and names.** Pieces 0–8 were checked in `E:\unity\NOPE-feat-clock` at `d998ab5` (`main` before piece 9, pieces 0–8 in the art office). Piece 9 was checked at `origin/main` = `d5844d0`, its merge, together with its spec (`docs/superpowers/specs/2026-09-25-translation-design.md`, verification record §8). Between `d998ab5` and `d5844d0`, piece 9 changed only the files its spec lists. Piece 9 names used here:
- `CaseTranslation`: `Field`, `Line`, `Bubble`, `Shown`, `PapersTranslated`, `SpeechTranslated`;
- `TextFlip`: `Show`, `Tick`, `Complete`, `Release`, `Write`, and its private `Fit`;
- `Reveal`/`RevealKind`, `DisplayText.For(canonical, Reveal, FlipTiming, reducedMotion)`;
- `Translation.InTongue(ClueCategory)`/`(DialogSpeaker)`;
- `DocumentWindowController`: `Reveal()`, its private `_revealedAt` and `FillRest`;
- `SpeechQueue`: `Say(text, expression, revealSeconds)`, `LineSeconds`, `EndReveal`;
- `TravellerWheel.SetTranslation`, `OverlayCallout.Label`.

Line numbers below are `d998ab5`'s unless marked. The plan's first task re-reads every file at `d5844d0`. **The code wins over this text.**

**Piece 9's open items this piece takes up** (its §8):
1. The scan wakes the PC but does not open the frame, so a papers flip mostly plays on the small office screen. X25 moves the written reveal to the first time the player sees the document up close.
2. Hieroglyphs are thin and faint at paper and bubble size. A held paper draws its values about 1.7× the size of the scanned page's rows in the frame (§9). The weight lever stays piece 9's follow-up.

**Saleh's build.** Saleh plays on `art`, which does not contain `main` yet, so he still sees the older PC-first flow. This piece lands on `main` and reaches him when `art` merges `main` (open item §10).

## 0. What `main` already does (verified at `d998ab5`)

| Path | Office | PC only | Where |
|---|---|---|---|
| Arrival: the claim | typed in the bubble above the traveller ("I request passage home to {place}.") | the claim banner, **with the role**: `claim.banner` = `"{0}\n\"{1}\""`, `{0}` = `visitorDisplayName` = "Lysimache (Merchant)" | `InvestigationUIController.cs:344`, `CaseFactory.cs:231-232`, `world_source.json` `claim.banner` |
| Arrival: the hand-over | the passport slides onto the mat, with the photo | (the scanned copy after a scan) | `DeskController.BeginCase` |
| A desk paper | title, holder's given name, photo; lit quad on the mat (about 120 × 67 px at 1080p) | every field row | `DeskDocument.Bind` (`DeskDocument.cs:69-76`) |
| A click on a paper | brings it to the top of the stack | – | `DeskController.cs:136, 210` |
| Wheel requests | the reply in the bubble; the requested paper slides in | the transcript row | `InvestigationUIController.Choose` (:499-540) |
| Questions | the answer in the bubble (read-only) | the answer row is the only compare-clickable answer | `TranscriptWindowController.cs:48-59` |
| A garment (Look >) | the wheel closes | the pick shows only in the PC compare bar | `InvestigationUIController.LookAt` (:547-556) |
| Premades, dialogs | lines and expressions in the bubble | the premade's Citizen Records note | `TravellerWheel.ShowSpeech` |
| Compare | – | the compare bar, highlights (uGUI `Image` only) | `CompareController.Select(label, value, Image, evidence)` (:61) |
| Proof | – | the Deviation Report; every proof needs a book row or a record row | `DiscrepancyLog.Prove` (:206-289) |
| Verdict | – | Accept/Deny, the verdict strip, the citation slip (all on the desktop canvas) | `OfficeSceneUIBuilder.cs:143-155, 312-317` |
| Directives | the morning paper | the Directives window | – |

- **The PC never opens by itself.** `OfficeViewController.FocusMonitor` has one caller, the PC's click box (`OfficeSceneUIBuilder.Desk.cs:525-526`). A scan wakes the screen but does not open the frame (`BoothCoordinator.HandleScanFinished`). Windows that open by themselves open on the desktop (the transcript at `InvestigationUIController.cs:530`, the Deviation Report on the first proof), which the office PC shows on its glass.
- **Proofs.** `DiscrepancyLog.Prove` accepts only one statement (a paper field, an answer or a garment) against one truth (a book row or a record row). A paper against a paper, or a paper against an answer, never logs anything. It can only show MATCH or MISMATCH. The truth rows live on the PC.
- **With piece 9 (`d5844d0`), the bubble speaks the claim in the claimed place's tongue** from day 2 (pseudo-script unless the Speech translator is owned), while the claim banner is always readable (piece 9 T2). So the only readable claim is on the PC.
- **Piece 9's written reveal is the scan** (`InvestigationUIController.OpenDocumentWindow` → `DocumentWindowController.Reveal()`), which does not open the frame, so the flip mostly plays unseen on the office PC's glass (piece 9 §7, §8).

## 1. Decisions

The coordinator made E1–E6. They are kept, with each refinement and deviation stated. Claude made the X rows while working from the code above.

| Id | Decision | Why |
|---|---|---|
| E1 | **Office first.** The traveller's claim, their answers and the hand-over play in the office bubble and on the desk. The PC never opens by itself (verified, §0). The claim stays readable in the office on a **claim tag** that shows the same text as the PC banner: name, role (the "why") and the claim (X12). The verdict is given at the desk **stamp** (X13). The verdict line and the citation slip move to the office overlay (X14). **Deviation:** a *proven* denial still needs one truth row (a book row or a Citizen Record) from the PC, because E5 keeps the books and Records there and `Prove` needs a truth side. An accept, a rule-violator denial and the verdict itself never need the PC. The remaining PC-only items are listed in §7. Open question Q1 asks about books on the desk. | The bubble fades after 4 s (`bubbleSeconds`), and from piece 9's day 2 it speaks the tongue. The claim and role were PC-only (§0). Desk books would be a new mechanic, outside this piece's request. |
| E2 | **Readable papers.** A desk paper shows its document's whole face: the title, every field row the scanned copy shows (all pages, in page order: the same labels and values), and the photo. Both views read their rows from one Domain source (`DocumentRows`) and write them with one helper (`DocumentRowView`). The holder line goes (X21). Written text follows piece 9: values in the tongue show untranslated. **With the Papers translator, the values flip into English the first time the player sees the document up close**: examined at the desk, or its scanned window shown in the open PC frame (X25). **One reveal per document** (a `RevealClock` shared by the paper and its scanned copy) means whichever is seen first reveals both. A row click finishes the flip on both. | Piece 7 K12/R19 and piece 9 T2 kept the paper untranslated only because it showed no values. Once it shows them, a translator that works only through the scanner would push every read back to the PC, against Saleh's request. The translator is the clerk's own device (piece 9 T6 sells it at Home, not as a scanner part). One clock keeps piece 9's "reopening never replays" (R7) across both surfaces. |
| E3 | **Examine.** A click on a desk paper (press and release, no drag) lifts it into an examine pose in front of the camera, in the lower part of the screen. There are two slots, left and right. Picking a third paper sends back the one held longest. A held paper goes back to where it lay (it slides back) on a right-click, on Escape, on a left click that lands on no field row of it, or on a click on the desk. Held papers are evenly lit (X3) and readable from 1280×720 to 2560×1080. Papers stay draggable on the desk. Scanning stays a desk drop, one at a time, and a held paper can be dragged straight out of the hand onto the scanner (X6). The pose maths (X2) and the knobs (`DeskConfigSO.examine`) are given in §3.3. | Papers, Please: take the document into the inspection area and read it against another. |
| E4 | **Compare at the desk.** A field row on a held paper, the answer the bubble shows (Answer evidence), or a garment through the Look menu is picked for **the same compare the PC uses**. One Domain `ComparePair` holds the two picks. One `CompareController` drives the PC bar and a new office strip. One evidence builder (`EvidencePicks`) serves every row on either surface. `PairCompared` → `DiscrepancyLog.Prove` → one Deviation Report. Both picks light up where they were picked. A pending PC pick pairs with a desk pick. The same field picked on the other surface is the same pick (X19). | No duplicate logic. Evidence stays canonical (piece 9 T5): an untranslated side shows the placeholder. |
| E5 | **The PC is for the complex work.** Scanned copies keep every current function: citizen records, reference books, translation, compare against books and records, the transcript, the Deviation Report, Accept/Deny. Desk and PC proofs land in the same Deviation Report. | Saleh: "perform more complex things in the computer if they want". |
| E6 | **Confront: deferred.** Pieces 3 and 8 have no confront or ask-about hook. Piece 3 Q5 makes authored topics flavour, and a provable topic becomes a fact category. Piece 8 W4 allows "no new mechanics" behind a request. Dialogs are gated at day start (`InterviewDay.OfferedDialogs`), so a proof made mid-case cannot offer one. Listed in §8 with its seam. | "Don't invent a dialog system." |

### Refinements settled by this spec

| Id | Refinement | Why |
|---|---|---|
| X1 | **The examine slots flank the screen's centre line, low, below the traveller's face.** Left and right boxes 0.44 screen-heights tall, centres ±0.2 screen-heights from the centre, bottom edge 0.02 up. At 1080p: left 562–925 px, right 995–1358 px, from 22 to 497 px above the bottom. | Measured on `move_office_papers.png` (art `23aa6e1`, 1920×1080): the PC spans x 70–470, the scanner 1310–1540, the stamp 1590–1660, the traveller's head 610–670 px above the bottom, the bubble 783–893 px. These slots keep the PC, the stamp, the traveller's face and the bubble visible and clickable, and cover only the scanner's left 48 px. They cover the mat. With the two documents the game hands over today, the mat is empty whenever both slots are full (X22). |
| X2 | **The pose is computed in screen-height units in camera space.** For a box (centre x from the screen centre, centre y from the bottom, height h, all in screen heights) at depth d, with h_d = 2·d·tan(fov/2): camera-space centre (x·h_d, (y − ½)·h_d, d), world height h·h_d. The sheet faces the camera. d is the `distance` knob (0.6 m). It is raised to at least 1.5 × the near clip, and lowered to at most 0.8 × the depth at which the box's lower corners' rays meet the desk plane. | The vertical field of view is fixed, so screen-height units are aspect-independent (a 21:9 screen only adds room at the sides). One formula serves both layouts. The desk plane (from the `DeskSurface` anchor) guarantees the paper never sinks into the art. |
| X3 | **"Lit" means evenly lit: while held, the paper quad uses an unlit copy of its material** (`Paper_Examine`, the same texture, tinted by `examineTint`). The photo takes the same tint. The texts are TMP (already unlit). | A lit quad 0.6 m from the camera takes whatever light the art puts there, and time-of-day lighting is planned (cursor-hover-shift-clock spec). Unlit reads the same in every office. It is still tonemapped and vignetted by the art's volume (§9). |
| X4 | **Slot choice is Domain:** `DeskPapers.Hold(i, preferRight)`. A paper right of the screen centre prefers the right slot. It takes the preferred slot when free, else the other free one. With both full, the paper held longest goes back and the new one takes its slot. | Short moves; a tested rule. |
| X5 | **Click routing on a paper is Domain:** `PaperClicks.Decide(held, secondary, onRow)`. On a desk paper, a left click examines it and a right click does nothing. On a held paper, a left click on a row picks the row, a left click off every row puts it back, and a right click puts it back. `PaperFace.RowAt` finds the row from the hit point. The row under the pointer tints (hover). | E3's "clicking it again" is kept for the parts of the paper that are not rows (title, photo, margins). Rows must stay clickable. |
| X6 | **Drag-out.** Pressing on a held paper and dragging (the EventSystem's 10 px threshold) puts it back at once and continues as a desk drag with the paper centred under the pointer. `DeskDraggable` gains `GrabAtCentre`. So a held paper can go straight onto the scanner. | A drag that did nothing would read as broken. This keeps scanning a desk drop. |
| X7 | **A click on the desk puts every held paper back.** A thin box over the desk's clamp area (`DeskCatcher`, a `ClickCatcher` on the Interactable layer, 1 mm under the plane) is live only while papers are held. Papers and props sit above it and still win the raycast. | "Clicking the desk". `ClickCatcher` shows neither a hand cursor nor an outline, so the desk does not light up. |
| X8 | **One Escape does one thing:** it closes the PC frame, else the wheel or the stamp tray, else it puts every held paper back. An Escape that closed something in the same frame is not taken again: each handler requires its input to have been live since the previous frame. | Three `Update`s read Escape today (frame, wheel), and a fourth (papers) would otherwise double-fire. |
| X9 | **While the wheel is open, held papers dip 0.2 screen-heights** (their tops to about 281 px at 1080p, under the ring's lowest items at 365 px) and take no input. They rise again when it closes. | The ring and its catcher sit on the overlay and would cover the papers' tops. Dipping also uncovers the traveller's torso while the Look menu is open. |
| X10 | **The frame region.** While the PC frame is open, held papers move beside it: into the region between the frame's right edge and the screen's right edge, side by side when they fit at `frameHeight` (0.62), else stacked. Their rows stay clickable through a hole in the frame's click-outside catcher (a second `RectHoleRaycastFilter` whose rect the examiner sizes to the papers). A book row in the glass and a paper row beside it pair in one view. At an aspect where the region cannot hold a paper at `frameMinHeight` (0.3), held papers wait behind the frame (inert). | Overlay hits always outrank world hits (physical-desk R6), so without the hole a click on a held paper beside the frame would close the frame. The desktop's own compare bar shows the result while the frame is open. |
| X11 | **The bubble takes clicks.** An answer line is compare-clickable: the bubble panel carries a `Button` that is interactable only while an answer shows, which gives the hand cursor and the hover ring. Hovering the bubble holds its line (`SpeechQueue.Hold`: a fully shown line never ends while held, and its hold runs from release). The bubble is ordered above the wheel's catcher, so an answer can be picked while the wheel stays open. `SpeechQueue` carries a tag per line, and the wheel maps the tag to its `DialogLine`. | "The bubble line vanishing before it can be clicked". The bubble's bottom (297 px from the top at 1080p) clears the ring's top item (310 px). |
| X12 | **The office case HUD**, top centre of the office overlay: the **claim tag** (the claim-banner text, from the one `UiText.Format("claim.banner", …)` call, also written to the PC banner) and the **office compare strip**. It shows while a traveller is at the desk and the frame is closed (the PC shows its own banner and bar then). | E1. It sits over the art's dark board above the portal, clear of the bubble. |
| X13 | **The stamp tray.** While a traveller is at the desk, a click on the stamp opens a small tray above it with Accept (left, tick) and Deny (right, cross), in piece 6's themed decision roles with their fixed glyphs and English gloss. A choice decides through the same `InvestigationUIController.Decide`, so the evidence gate, scoring and the ledger are unchanged. It is modal like the wheel (a catcher, and Escape closes it). | E1 "a verdict never requires it". The wheel's hub is full in the worst case (8 of 8), so the verdict cannot go there. |
| X14 | **The verdict strip and the citation slip move from the desktop canvas to the office overlay** (the same objects, themes and wiring; `OfficeUIController` unchanged). The citation's screen hold (piece 7 R31) stays: it still makes the power buttons inert while the day is paused. | A desk verdict's citation must be seen without the PC. On the overlay it also shows over the frame. |
| X15 | **`BoothRules` grows.** The context gains `StampOpen` and `PapersHeld`. The outputs gain `HeldPapersLive`, `DeskCatcherLive`, `ExamineEscapeLive`, `StampTrayAllowed` and `CaseHudVisible`. `CrtFocusable`, `PowerButtonLive`, `PropsLive`, `PapersLive` and `TravellerLive` also require `!StampOpen`. | The input table stays one tested Domain table (piece 7 V5). |
| X16 | **The face layout is `PaperFace` (Visuals):** a title band; rows of a label over a value; the photo at the top right beside the first rows (the PC page's `photoInset` rule); knobs in `DeskConfigSO.face`. Capacity is 6 rows with the defaults (passport 4, permit 2). Build Office UI reports a template with more fields than the face holds. | The same page as the PC copy, measured once. |
| X17 | **The day-1 desk note** reads "Click a paper to read it; drag it onto the scanner to open it on the PC." (`desk.scanHint`). It shows until the day's first read or scan (`DeskHints.ScanHintVisible(…, usesToday, …)`). | The desk is now where papers are read. The note should not stay all day for a player who never scans. |
| X18 | **`RevealClock` (Visuals)** replaces `DocumentWindowController._revealedAt`: NaN before the reveal, then seconds since it, +∞ once finished. The condition to start it (foreign, Papers translated, the document has a field in the tongue: `DocumentRows.HasTongue`) lives in one helper, `DocumentReveal.Begin`, called at every sighting (X25). | One source for the reveal state (E2), one for its rule. |
| X19 | **Pick keys (Domain `PickKeys`)** name every pickable value: `field:{doc}:{index}`, `line:{index}`, `garment:{index}`, `book:{category}:{nation}:{era}`, `record:{category}`. The same field on the desk and on the PC has one key, so picking it on the other surface clears it: today's "same row again clears" rule, across surfaces. | Otherwise a field compared with its own copy would show a trivial MATCH. |
| X20 | **Highlights go through `ICompareHighlight`**: an `Image` tint (PC rows, as today), a row quad on the desk paper, and the bubble panel. Each one restores its own colour and is null-safe for a paper destroyed at the decision. | The PC's highlight was a uGUI `Image` only (`CompareController.cs:39, 124-128`). |
| X21 | **No holder line on the paper.** The Full Name row carries the name on the passport, and the PC copy has no holder line either. `CaseDocument.holder` goes and `CaseDocument.fields` (the document's `DocumentField` list) arrives. | E2's "the same face". No dead field. |
| X22 | **A paper handed over while both slots are full lands under them** (on its spawn slot on the mat). The player puts a paper back to take it. Accepted and listed (Q7). | Auto-examining a requested paper would take control from the player. |
| X23 | **No random draws, no generation change, no save change.** `InterviewReachable`/`AppearanceReachable` are unchanged, so tells are unchanged. | Determinism (house rules). |
| X24 | **The builder** builds the paper face (a row template with a highlight quad), `Paper_Examine`, the desk catcher, the `PaperExaminer`, the frame's second hole filter and its `ExamineHole` rect, the office case HUD, the stamp tray, the moved verdict strip and citation slip, and the bubble's input and order. It wires them and checks face capacity. | The builder stays authoritative (house rules). |
| X25 | **The written reveal happens on first sight, not at the scan.** A document's reveal starts the first time the player sees it up close: when its paper is examined, when the PC frame opens while its scanned window is open, or when a scan opens its window while the frame is open. A scan with the frame closed opens the window untranslated on the office PC's small glass, and the flip waits for a sighting. Where no desk is wired, the window opened at the hand-over reveals the same way. Reduced motion shows English at the sighting. | Piece 9 §7 names this lever for its "flip plays unseen" item: "start the papers' flip when the frame first shows the window". With the paper in hand now the main reading surface, a flip spent on the small screen would leave the player only English, never the flip. Nothing is lost: evidence stays canonical, and the glyphs on the small screen were never readable. |

## 2. Behaviour

### 2.1 The arrival

- READY brings the traveller in. The claim types in the bubble (in the tongue from piece 9's day 2), and the **claim tag** appears at the top of the office: "Lysimache (Merchant)" and, below it, "I request passage home to Periclean Athens (Ancient)." in the PC banner's words (always English, piece 9 T2). It stays until the decision.
- The passport slides onto the mat, face up, showing its title, all its rows and the photo.

### 2.2 Reading a paper

- **Click a paper** (press and release without moving 10 px): it rises from the desk into the player's hands in 0.18 s, turned to face the camera. It stops in the left or right slot (the side of the screen it lay on, if free). It reads like the scanned page: the title, then each row's label (small) over its value, and on the passport the photo at the top right.
- **Two papers** can be held side by side. Clicking a third sends the one held longest back to where it lay.
- **Put it back:** right-click it, click it anywhere that is not a row (the title, the photo, a margin), click the desk, or press Escape (all held papers). It slides back to where it lay and lands on top of the stack.
- **Hover** a row of a held paper: the row tints. The paper keeps its white hover outline and the hand cursor.
- **Drag a held paper:** it drops back onto the desk under the pointer and follows it like any desk drag, so it can go straight onto the scanner.
- A sliding paper (handed over, coming back from the scanner, or leaving) and a scanning paper cannot be picked up.
- **Translation (piece 9):** from day 2, a foreign traveller's place-fact values (Coin of Issue, Native Tongue, Declared Device, Bond Currency) show in the tongue's script on the paper, exactly as on the scanned copy. Labels, the title, the name and the date of birth stay readable. With the region's Papers translator, the first time the player sees the document up close, its values flip into English letter by letter, rows staggered. That is when the paper is examined, or when its scanned window shows in the open PC frame (X25). A scan alone no longer starts the flip on the small office screen. The other surface then shows the same state and never replays. A click on a row finishes the flip on both. Reduced motion shows English at the first sighting.

### 2.3 Comparing at the desk

- **Pick** a row of a held paper, an **answer** in the bubble, or a **garment** (wheel ▸ Look ▸ a garment, as today). The pick lights up in the theme's highlight colour (the paper row, the bubble; a garment has no paper), and the **office compare strip** shows "Travel Passport · Coin of Issue: Silver drachma (owl)   vs   (pick another value to compare)".
- **The second pick** shows MATCH or MISMATCH in the theme's colours, exactly as the PC bar does. A statement against a truth row logs a deviation ("DEVIATION LOGGED — CURRENCY INCORRECT — …") or reports "ALREADY DOCUMENTED". A third pick starts a new comparison. Picking a picked value again clears the comparison, on either surface.
- **Against each other:** paper against paper (the passport's Coin of Issue against the permit's Bond Currency) and paper against answer show MATCH or MISMATCH as hints. They never log anything (junk rule, unchanged). An answer that disagrees with the papers is piece 3's hint that the answer is the tell.
- **Across surfaces:** a pick made on the PC (a book row, a Citizen Record, a transcript row, a scanned-copy row) waits while the player closes the frame, and pairs with the next desk pick, and the reverse. The strip and the PC bar always show the same comparison.
- **Untranslated values** show in the strip as "(untranslated Egyptian; Near East Translator)" (piece 9 R11). The evidence stays canonical, so MATCH and proofs are unchanged.

### 2.4 The bubble

- The bubble sits above the traveller as today, and above the open wheel.
- **An answer line** (a reply to a question) shows the hand cursor and the hover ring, and a click picks it. Other lines (the claim, small talk, request replies, dialog lines) are not evidence and take no click.
- **Hovering the bubble holds its line**: it stays up while the pointer is on it, and its 4 s hold (1.5 s when a line waits) starts when the pointer leaves. Queued lines wait.
- As today, opening the PC or the traveller leaving clears it. The PC transcript keeps every line, and its answer rows stay compare-clickable.

### 2.5 The PC frame and held papers

- Opening the PC (a click on it) moves held papers beside the frame: at 1920×1080 into the 660 px right of the frame, one paper centred (0.62 screen-heights tall) or two stacked; at 2560×1080 side by side. Their rows stay clickable. A click anywhere else outside the frame still closes it.
- The desktop's compare bar shows the comparison while the frame is open. The office claim tag and strip hide then, because the PC shows its own banner and bar.
- Closing the frame (X, Escape, a click on the room, "< Desk") sends the held papers back to their office slots, still held.
- Desk papers, props and the traveller stay inert while the frame is open (unchanged).

### 2.6 The wheel, the stamp and the verdict

- **The wheel** opens as today (the traveller or the intercom). Held papers dip below the ring and take no input until it closes.
- **The stamp:** while a traveller is at the desk, a click on it squashes it (as today) and opens the stamp tray above it: **Accept** (left, tick) and **Deny** (right, cross), labelled like the PC's buttons (the culture's language with the English gloss, piece 6). A choice decides the case. Escape or a click outside the tray closes it. The PC's Accept and Deny still work.
- **After the decision** the papers leave (held ones first drop back to the desk), the traveller leaves, and the claim tag hides. The verdict line and a citation slip show over the office. The slip still pauses the day until Acknowledge.
- **The evidence gate is unchanged:** denying a liar with no documented deviation is an unproven denial. At the desk a deviation is documented by pairing a desk statement with a truth row picked on the PC (a book or Citizen Records).

### 2.7 Who takes input when (additions to physical-desk §1.9 as the office move left it)

| Situation | Desk papers (drag, examine) | Held papers (rows, put back) | Desk catcher | Escape puts papers back | Stamp tray may open | Case HUD |
|---|---|---|---|---|---|---|
| Newsletter up | no | – | – | no | no | no |
| Office view, no traveller | – | – | – | – | no | no |
| Office view, traveller at desk | yes | yes | while papers are held | while papers are held | yes | yes |
| Wheel open | no | no (dipped) | no | no (Escape closes the wheel) | no | yes |
| Stamp tray open | no | no | no | no (Escape closes the tray) | – | yes |
| PC frame open | no | yes, in the frame region (X10) | no | no (Escape closes the frame) | no | no |
| Citation pending (traveller gone) | – | – | – | – | no | no |

The props, the PC, the power buttons and the traveller also go inert while the stamp tray is open. Otherwise their rows are unchanged.

### 2.8 Edge cases

- **Decision while papers are held or one is scanning:** every held paper snaps back to its stack pose, and every paper slides away (a running scan is cancelled, as today).
- **Closing time** with the traveller at the desk: papers, holds and picks stay until the decision (unchanged).
- **A pick on a paper that later leaves** (the decision): the next case's `Clear` finds its highlight destroyed and skips it.
- **A held paper is dragged onto a busy scanner:** refused, and it slides back to the desk spot it was held from.
- **Screen resize** while holding: the examiner re-poses on the next frame (it compares the screen size, the camera pose and the field of view it last used).
- **The same field on both surfaces:** one key, so picking it on the other surface clears the comparison (X19).
- **A narrow window** (below about 4:3) with the frame open: held papers that cannot fit beside the frame wait behind it, inert (X10).

### 2.9 Determinism, time and saves

Desk interactions draw no random numbers, and generation is unchanged (X23). The shift clock keeps running while papers are examined (the examine is part of working the case, like a scan). Nothing is saved mid-shift, and `SaveVersion` stays 2.

## 3. Design

### 3.1 Where things live

- **TimeDesk.Domain** (tested):
  - new `DocumentRows.cs` (`DocumentRow`, `DocumentRows`);
  - new `ComparePair.cs` (`ComparePick`, `CompareStep`, `ComparePair`, `PickKeys`);
  - changed `DeskPapers.cs` (`CaseDocument.fields`, holder removed, `ExamineSlot`, `HoldResult`, the hold members, `PaperClickAction`, `PaperClicks`, `DeskHints` rename);
  - changed `BoothRules.cs` (X15).
- **TimeDesk.Visuals** (engine-free, tested):
  - new `ExamineLayout.cs` (`ExamineTuning`, `ScreenBox`, `ExamineLayout`);
  - new `PaperFace.cs` (`PaperFaceTuning`, `FaceRect`, `FaceRow`, `FaceLayout`, `PaperFace`);
  - new `RevealClock.cs`;
  - changed `SpeechQueue.cs` (the tag and the hold).
- **Assembly-CSharp:**
  - new `UI/EvidencePicks.cs`, `UI/CompareHighlights.cs` (`ICompareHighlight`, `ImageHighlight`), `UI/DocumentRowView.cs` (`DocumentRowView`, `DocumentReveal`), `UI/SpeechBubbleInput.cs`, `UI/StampTray.cs`, `UI/OfficeCaseHud.cs`, `Office/Desk/PaperExaminer.cs`;
  - changed `UI/CompareController.cs`, `UI/DocumentWindowController.cs`, `UI/TranscriptWindowController.cs`, `UI/ReferenceBookWindowController.cs`, `UI/CitizenRecordsWindowController.cs`, `UI/InvestigationUIController.cs`, `UI/TravellerWheel.cs`, `Office/Desk/DeskDocument.cs`, `Office/Desk/DeskController.cs`, `Office/Desk/DeskDraggable.cs`, `Office/BoothCoordinator.cs`, `Office/PcFrame.cs`, `Office/DeskConfigSO.cs`, `Office/OfficeSceneBinder.cs`, `GameManager.cs` (one subscription, X25), `CaseInstance.cs` (`DocumentInstance.PageCount` → `DocumentRows.PageCount`).
- **Assembly-CSharp-Editor:** `OfficeSceneUIBuilder.cs`, `OfficeSceneUIBuilder.Desk.cs`.
- **Content:** `world_source.json` (`desk.scanHint` text), the generated UI table in `ContentLibrary_Main` (Generate World), the rebuilt `OfficeGameplay.unity`, `Desk_Default.asset` (new knobs keep their defaults), `Assets/Art/Office/Gameplay/Materials/{Paper_Examine,PaperRow_Highlight}.mat`.
- Line endings: each file keeps its own. The working tree mixes CRLF and LF (see `git ls-files --eol`). Edits go through `SCRATCH/subs.py`.

### 3.2 Domain

**`DocumentRows.cs`** (new):
```csharp
/// One row of a document as every view shows it: the field, its index in the document's field list, and its place among the rows in the tongue (piece 9's row stagger).
public readonly struct DocumentRow { public int Index { get; } public DocumentField Field { get; } public int TongueRow { get; } }

/// The one row source of a document (the PC's scanned page and the desk paper).
public static class DocumentRows
{
    /// Every field, page by page (page ascending, then authored order), nulls skipped; TongueRow counts the rows in the tongue (Translation.InTongue) before it on the whole document.
    public static IReadOnlyList<DocumentRow> Ordered(IReadOnlyList<DocumentField> fields);
    /// The fields of one page in authored order; TongueRow counts within the page (piece 9's DocumentWindowController rule).
    public static IReadOnlyList<DocumentRow> OnPage(IReadOnlyList<DocumentField> fields, int page);
    /// Pages the document spans: the highest page + 1, at least 1 (DocumentInstance.PageCount's rule, moved).
    public static int PageCount(IReadOnlyList<DocumentField> fields);
    /// True when a field is in the tongue (a written reveal has something to flip; piece 9's Reveal() check, moved).
    public static bool HasTongue(IReadOnlyList<DocumentField> fields);
}
```

**`ComparePair.cs`** (new):
```csharp
/// One pickable value: its identity (PickKeys), the bar's label, the text the bar shows (canonical, or piece 9's placeholder), and the typed evidence.
public readonly struct ComparePick { public ComparePick(string key, string label, string shown, CompareEvidence evidence); public string Key; public string Label; public string Shown; public CompareEvidence Evidence; }

public enum CompareStep { Cleared, Pending, Paired }

/// The two sides of a comparison, from any surface: a value picked again clears it; a first pick waits; a second pairs; a pick after a pair starts a new comparison with it.
public sealed class ComparePair
{
    public CompareStep Select(ComparePick pick);   // same non-blank key as A or B -> Cleared (both emptied)
    public void Clear();
    public bool HasA { get; } public bool IsPaired { get; }
    public ComparePick A { get; } public ComparePick B { get; }
    /// MATCH on each side's MatchValue (DiscrepancyLog.ValuesMatch): the one match rule, moved from CompareController.Refresh.
    public bool Matches { get; }
}

/// The keys of pickable values; a value has one key on every surface.
public static class PickKeys
{
    public static string Field(int document, int field);     // "field:0:2"
    public static string Line(int transcriptIndex);          // "line:5"
    public static string Garment(int index);                 // "garment:1"
    public static string BookRow(ClueCategory c, string nationId, string eraId); // "book:Currency:greece:ancient"
    public static string Record(ClueCategory c);             // "record:BirthDate"
}
```

**`DeskPapers.cs`** (changed):
- `CaseDocument`: `holder` removed; `public IReadOnlyList<DocumentField> fields` added ("the document's fields, the rows its paper shows").
- `public enum ExamineSlot { None, Left, Right }`, `public readonly struct HoldResult { bool Held; ExamineSlot Slot; int Evicted; }`.
- `DeskPapers`: a new private state `Held`, between `OnDesk` and `Scanning` in the private enum (not serialized).
  - `HoldResult Hold(int i, bool preferRight)`: only an `OnDesk` paper; slot choice per X4; evicting puts the paper held longest back `OnDesk`.
  - `bool PutBack(int i)`: `Held` → `OnDesk`.
  - `IReadOnlyList<int> PutBackAll()`: in slot order, left first.
  - `bool IsHeld(int i)`, `ExamineSlot SlotOf(int i)`, `int HeldCount`.
  - `CanDrag(i)` is true for `OnDesk` and `Held` (the drag-out). `Drop(i, …)` still needs `OnDesk`: the controller puts a held paper back when its drag begins.
  - `OnDeskCount` counts `Held` too (a held paper is still on the desk for the day-1 note's rule).
  - `ReturnAll()` clears the holds.
- `public enum PaperClickAction { None, Examine, PutBack, Pick }` and `public static class PaperClicks { public static PaperClickAction Decide(bool held, bool secondary, bool onRow); }` (X5).
- `DeskHints.ScanHintVisible(string hint, int day, int untilDay, int usesToday, bool paperOnDesk)`: the parameter is renamed and documented ("papers read or scanned today", X17). The rule is unchanged.

**`BoothRules.cs`** (changed, X15), with `props = !Focused && !newsletter && !WheelOpen && !StampOpen` and `atDesk = Phase == TravellerAtDesk`:
- `PapersLive = props && atDesk`; `HeldPapersLive = atDesk && !newsletter && !WheelOpen && !StampOpen` (either view);
- `DeskCatcherLive = ExamineEscapeLive = PapersLive && PapersHeld`;
- `StampTrayAllowed = !Focused && atDesk` (false closes an open tray); `CaseHudVisible = !Focused && atDesk`;
- `CrtFocusable`, `PowerButtonLive` and `TravellerLive` also require `!StampOpen`.

### 3.3 Visuals

**`ExamineLayout.cs`** (new). The knobs, held by `DeskConfigSO.examine`:

| Knob | Default | Meaning |
|---|---|---|
| `officeHeight` | 0.44 | a held paper's height in the office, in screen heights |
| `officeCentreX` | 0.2 | each slot's centre from the screen centre (left −, right +), in screen heights |
| `officeBottom` | 0.02 | the slots' bottom edge above the screen's bottom |
| `wheelDip` | 0.2 | how far held papers drop while the wheel is open |
| `frameHeight` | 0.62 | the largest paper height beside the PC frame |
| `frameMinHeight` | 0.3 | below this, papers wait behind the frame |
| `margin` | 0.02 | from screen and region edges |
| `gap` | 0.03 | between two papers |
| `distance` | 0.6 | metres from the camera |
| `nearMargin` | 1.5 | the least distance, in near-clip planes |
| `deskMargin` | 0.8 | the greatest distance, as a share of the depth where the paper's lower corners' rays meet the desk |
| `roll` | 0 | degrees (left −, right +) |
| `seconds` | 0.18 | the rise and the return |

```csharp
public readonly struct ScreenBox { public float CentreX, CentreY, Height; }  // screen heights: x from the centre, y from the bottom

public static class ExamineLayout
{
    /// An office slot's box; kept inside the screen (|x| + w/2 <= aspect/2 - margin); dipped lowers it by wheelDip.
    public static ScreenBox OfficeSlot(bool right, float paperAspect, float screenAspect, bool dipped, ExamineTuning t);
    /// The index-th of count (1 or 2) held papers in the region [left, right] (screen heights from the centre): the larger of side by side and stacked, capped at frameHeight; fits = false below frameMinHeight.
    public static ScreenBox InRegion(int index, int count, float left, float right, float paperAspect, ExamineTuning t, out bool fits);
    /// Camera-space centre and world height of a box at depth d (X2): h_d = 2 d tan(fov/2).
    public static (float x, float y, float z, float height) Pose(ScreenBox box, float verticalFovDegrees, float distance);
    /// The depth to use (X2): wanted, raised to nearMargin * near, lowered to deskMargin * the depth at which the box's lower corners' rays meet the desk plane
    /// (the camera's height above the plane and the plane normal's dot with the camera's right, up and forward); no desk limit when those rays never meet it.
    public static float SafeDistance(ScreenBox box, float paperAspect, float verticalFovDegrees, float nearClip,
                                     float heightAboveDesk, float normalDotRight, float normalDotUp, float normalDotForward, ExamineTuning t);
    /// Smoothstep: the rise and the return.
    public static float Ease(float t);
}
```
Worked example (tested): fov 55°, d 0.6 → h_d = 0.62468 m. Left office slot (x −0.2, y 0.24, h 0.44): camera-space (−0.12494, −0.16242, 0.6), world height 0.27486 m (scale 0.808 of a 0.34 m paper). SafeDistance with the art camera (1.09 m above the mat, pitched 10° down; normal · right 0, · up 0.98481, · forward −0.17365): the lower corners' rays (y 0.02) meet the desk at depth 1.637 m, so the cap is 1.31 m and 0.6 stands.

**`PaperFace.cs`** (new). The knobs, held by `DeskConfigSO.face`, are fractions of the paper's width W or height H, measured from the top-left:

| Knob | Default |
|---|---|
| side margin | 0.07 W |
| title top, height | 0.03 H, 0.08 H |
| rows top | 0.13 H |
| row pitch | 0.125 H |
| label and value shares of a row's pitch | 0.36, 0.52 |
| bottom margin | 0.04 H |
| photo top, height | 0.13 H, 0.25 H (width from `LookCanvas.PhotoAspect`) |
| gap left of the photo | 0.03 W |

`PaperFace.Layout(int rows, bool photo, float paperAspect, PaperFaceTuning t)` returns the title, photo and per-row label, value and hit rects in the sheet's local space (paper units, centre origin, y up). A row whose band overlaps the photo is narrowed to end left of it. `RowAt(layout, x, y)` returns a row index, or −1 on the title, the photo, a margin or a gap. `Capacity(photo, t)` is 6 with these defaults.

At 1280×720 a held paper is 317 px tall, so value boxes are 21 px (about 16 px text) and labels 14 px (about 11 px text). At 1080p they are 31 and 21 px.

**`RevealClock.cs`** (new): `bool Started`, `void Start(float now)` (the first call wins), `void Finish()` (after a start: elapsed is +∞ from then), `float Elapsed(float now)` (NaN before a start, which is piece 9's "not yet revealed").

**`SpeechQueue.cs`** (changed):
- `Say(string text, string expression, float revealSeconds = 0f, int tag = -1)`; `int Tag` is the shown line's tag, or −1.
- `void Hold(bool held)`: while held, a fully shown line's clock stops at the moment it became fully shown (typing and the reveal still run), so it never ends. On release, its minimum or hold runs from there. Queued lines wait.
- Every existing call passes no tag and never holds, so today's pacing is unchanged.

### 3.4 Assembly-CSharp

**`EvidencePicks`** (new, static; the one evidence builder, E4). Each method returns a `ComparePick` with its `PickKeys` key, the bar label, the shown text and the canonical evidence:
- `ForField(int doc, DocumentRow row, string docName, CaseTranslation tr)`: label `document.compareLabel`; shown `tr.Shown(InTongue(category), tr.PapersTranslated, value)`; `CompareEvidence.FromDocumentField`.
- `ForAnswer(int lineIndex, DialogLine line, CaseTranslation tr)`: label `compare.travellerLabel`; shown `tr.Shown(true, tr.SpeechTranslated, line.Value)`; `CompareEvidence.ForAnswer`.
- `ForGarment(int index, Garment g)`, `ForBookRow(ReferenceBookSO book, FactRow fact)`, `ForRecord(ClueCategory c, string label, string value)`: today's labels and evidence, moved.

Callers: `DocumentWindowController`, `TranscriptWindowController`, `ReferenceBookWindowController`, `CitizenRecordsWindowController`, `InvestigationUIController.LookAt`, and the desk and bubble handlers. Their five inline builds go.

**`ICompareHighlight`** (new): `void Show(bool picked, Color colour)`. Implementations:
- `ImageHighlight` wraps an `Image` and restores its colour.
- `PaperRowHighlight` (in `DeskDocument`) tints the row's quad through a `MaterialPropertyBlock`. It wins over the hover tint.
- `BubbleHighlight` (in `TravellerWheel`) tints the bubble panel while the picked line shows, and restores it when the line goes.

**`CompareController`** (refactor):
- `Select(ComparePick pick, ICompareHighlight highlight)`. The pair logic is `ComparePair`. Highlights are kept per side. `PairCompared` is raised on `Paired`.
- It draws two bars with the same text and colours: the PC `compareBar`/`compareText` and a new `officeBar`/`officeText`. Each bar is active while a pick exists. The office bar's parent is the case HUD, shown by the coordinator.
- `ShowDeviation`, `ShowAlreadyDocumented` and `ApplyTheme` write both bars.
- `Select(string, string, Image, CompareEvidence)` goes.

**`DocumentRowView`** (new, static): `Write(TMP_Text label, TMP_Text value, DocumentRow row, CaseTranslation tr, RevealClock clock, float now, List<TextFlip> running)`. It writes the label, and writes the value through `TextFlip` with `tr.Field(category, clock.Elapsed(now), row.TongueRow)`, adding a running flip to `running`. It also applies piece 9's width rule to a value in the tongue: `FillRest` moves here from `DocumentWindowController` and is used only where the row has a layout. Callers: `DocumentWindowController.Rebuild` and `DeskDocument`. **`DocumentReveal.Begin(RevealClock, CaseTranslation, IReadOnlyList<DocumentField>, float now)`** starts the clock when `tr.Foreign && tr.PapersTranslated && DocumentRows.HasTongue(fields)` (X18). Its callers are the sightings of X25.

**`DocumentWindowController`**:
- `SetDocument(DocumentInstance doc, int index, CompareController compare, TravellerLook look, CharacterArt art, CaseTranslation tr, RevealClock clock)`.
- `Reveal()` goes: the controller calls `DocumentReveal.Begin` at a sighting. `Refresh()` rebuilds the page from the clock (called after any reveal starts, on either surface).
- `Rebuild` iterates `DocumentRows.OnPage`, writes through `DocumentRowView`, keeps piece 9's `FillRest`, and picks through `EvidencePicks.ForField` with an `ImageHighlight`.
- A row click finishes the clock (`clock.Finish()`), then selects. `_revealedAt` goes.

**`DeskDocument`** (face and pose target):
- `Bind(int index, CaseDocument doc, CaseTranslation tr, RevealClock clock, PaperFaceTuning face, Vector2 paperSize)` clones its row template per `DocumentRows.Ordered` row and places the clones, the title and the photo by `PaperFace.Layout`.
- `Refresh()` rewrites the values from the clock. `Update` runs only while a flip runs.
- `SetExamined(bool)` swaps the quad to `examineMaterial` and the photo to `examineTint`, and routes clicks and hover.
- It implements `IPointerClickHandler` and `IPointerMoveHandler`/`IPointerExitHandler`. A click raises `Clicked(DeskDocument, bool secondary, int row)`, with the row from `PaperFace.RowAt` of the hit point in the sheet's space. The move handlers tint the hovered row while examined.
- `Sheet` is exposed for the examiner. `SetLift` does nothing while the examiner owns the sheet. `RowHighlight(int row)` returns the row's `ICompareHighlight`.
- The `holder` text goes.

**`PaperExaminer`** (new, on `Office/Desk`):
- It owns the held sheets' poses: `Hold(DeskDocument, ExamineSlot)`, `Release(DeskDocument, bool instant, Action landed)`, `SetMode(bool frameOpen, bool dipped)`, `Clear()`.
- The camera comes from the binder (`SetCamera`) and the desk plane from `DeskSurface`. It reads `PcFrame.RightEdgePixels` and sizes the frame's hole (`PcFrame.SetExamineHole`).
- It animates in `LateUpdate` only while a pose changes, and re-poses when the screen size, the camera pose or the field of view changes.
- Targets come from `ExamineLayout` (office slots, or the frame region while the frame is open). World pose: `cam.TransformPoint(x, y, z)`, rotation `cam.rotation * Euler(0, 0, ±roll)`, uniform scale `height / paperSize.y`. The sheet stays a child of its paper root, which stays at its desk spot, so a put-back lands where the paper lay.

**`DeskController`**:
- `BeginCase(docs, look, art, CaseTranslation tr, IReadOnlyList<RevealClock> clocks)`.
- It routes `DeskDocument.Clicked` through `PaperClicks.Decide`. Examine calls `DeskPapers.Hold`, then the examiner, then raises `PaperExamined(int)`. A pick raises `FieldPicked(int paper, DocumentRow row, ICompareHighlight h)`. A put-back calls `DeskPapers.PutBack`, then the examiner's release, then `PaperStack.BringToFront`.
- The drag-out: on `DragBegan` of a held paper it puts the paper back instantly, and `DeskDraggable.GrabAtCentre` is set while the paper is held.
- `SetHeldLive(bool)`; the desk catcher (on `DeskCatcherLive`: a click puts every held paper back); Escape (on `ExamineEscapeLive`, with the same-frame guard of X8); `EndCase` puts back instantly, then slides as today.
- `BringToFront` on a click goes.
- `RefreshHint` counts reads and scans.

**`DeskDraggable`**: `public bool GrabAtCentre { get; set; }` (the grab offset is zero while it is true).

**`TravellerWheel`**:
- It keeps the lines it queued (the tag is their index), `CurrentLine` (the shown line's `DialogLine`, or null), `event Action<DialogLine> LineClicked` and `SetBubbleHovered(bool)` → `SpeechQueue.Hold`.
- `ShowSpeech` makes the bubble's button interactable only while `CurrentLine.IsAnswer`.
- The same-frame Escape guard (X8).

**`SpeechBubbleInput`** (new, on the bubble's panel): pointer enter/exit → `SetBubbleHovered`; the button's click → the wheel's `LineClicked` (a click that arrives while no answer shows does nothing).

**`StampTray`** (new; the wheel's host pattern, R33): an always-active host with a `Catcher` child (full screen; a click outside closes) holding the panel, placed by `OverlayProjection` at the stamp's click box plus `DeskConfigSO.stampTrayOffset` (0, 140). `Open()` (the stamp's persistent call) works only while allowed. `SetCanOpen(bool)`, `IsOpen`, `event Action OpenChanged`, `event Action<bool> Decided`. Escape closes with the same-frame guard.

**`OfficeCaseHud`** (new): the claim strip and its text, and the office compare strip's host. `SetClaim(string)`, `SetVisible(bool)`.

**`InvestigationUIController`**:
- `ShowRich` formats the claim banner once and writes it to `claimText` and to the HUD. It builds a `RevealClock` per document and passes `fields`, the translation and the clocks to the desk.
- It handles `desk.FieldPicked`: `clock.Finish()`, then `compare.Select(EvidencePicks.ForField(…), h)`.
- **Sightings (X25)** go through one private `Sighted(int document)`: `DocumentReveal.Begin`, then refresh the paper and the window. Its callers:
  - `desk.PaperExamined`;
  - `SetFrameOpen(true)`, called through `GameManager` when the view changes: every scanned window that is open;
  - `OpenDocumentWindow` while the frame is open. With the frame closed, it only opens the window, untranslated.
- It handles `wheel.LineClicked` (`EvidencePicks.ForAnswer` with the wheel's bubble highlight) and `stampTray.Decided` (`Decide`).
- `LookAt` uses `EvidencePicks.ForGarment`. `Hide` clears the claim tag.

**`BoothCoordinator`**:
- The new context: `stampTray.IsOpen` and `desk.HeldCount > 0` (the desk raises `HoldsChanged`).
- It applies `HeldPapersLive`, `DeskCatcherLive`, `ExamineEscapeLive`, `StampTrayAllowed` (`stampTray.SetCanOpen`) and `CaseHudVisible` (`hud.SetVisible`), and gives the examiner its mode: frame open, and dipped while the wheel is open.

**`GameManager`**: it already holds `officeView` and `investigationUI`. It subscribes `officeView.ViewChanged` → `investigationUI.SetFrameOpen(view == MonitorFocus)` (X25). This needs no new scene wiring, so it also works before the gameplay scene is rebuilt.

**`PcFrame`**: `float RightEdgePixels` (the frame's right edge on the screen) and `SetExamineHole(float xMin, float yMin, float xMax, float yMax)` (screen px; an empty rect for none). The hole is a second `RectHoleRaycastFilter` on `ExitCatcher`. A graphic's raycast fails if any filter on it says so, so the class does not change.

**`DeskConfigSO`**: a `[Header("Examine (piece 10)")]` section with `ExamineTuning examine`, `PaperFaceTuning face`, `Color examineTint` (white), `Color rowHoverTint` (0, 0, 0, 0.06) and `Vector2 stampTrayOffset` (0, 140).

**`OfficeSceneBinder`**:
- `examiner.SetCamera(office)` and `stampTray.SetCamera(office)`; the stamp tray follows the stamp's click box;
- `BindDesk` sizes the desk catcher to the clamp area, 1 mm thick and 1 mm under the plane (`PlaceBox`), right after `surface.Configure`.

### 3.5 Builder (`OfficeSceneUIBuilder.cs` + `.Desk.cs`)

- **Paper template** (`BuildPaperTemplate`):
  - `Holder` goes. A `Rows` root holds one inactive `RowTemplate` (a `Label` TMP, a `Value` TMP, a `Highlight` quad with `PaperRow_Highlight`, an unlit transparent material driven by a property block).
  - `Paper_Examine` (URP Unlit, the paper's texture) is created once and wired as `examineMaterial`.
- **The desk**: `Desk/Catcher` (a `BoxCollider` over the clamp area 1 mm under the plane, Interactable layer, `ClickCatcher`, inactive), wired to `DeskController`. `Desk/Examiner` (`PaperExaminer`), wired to the config, the surface and the frame.
- **The frame**: `PcFrame/Root/ExamineHole` (a RectTransform with no graphic, bottom-left anchors) and a second `RectHoleRaycastFilter` on `ExitCatcher`.
- **The office overlay, bottom to top**: newsletters, fallback HUD, `OfficeCaseHud` (the claim strip (1100 × 64 px, 16 px from the top, `ClaimStrip` role, 26 pt, fit) and the office compare strip (1200 × 56 px, 88 px from the top, `CompareBar` role, 22 pt, fit)), `PcFrame`, the wheel, the speech bubble (now after the wheel, with a `Button`, a transition-free `SpeechBubbleInput` and `raycastTarget` on its panel image), the desk tooltip, `StampTray` (Accept and Deny via `MakeButton` and `BuildDecisionGlyph` with the `accept`/`deny` keys and the decision roles), `VerdictStrip`/`VerdictText` (moved from the desktop: top centre, as the claim strip's place, because they never show together) and `CitationPanel` (moved: centre). Every new graphic is tagged (the builder's untagged check).
- **Wiring**: `compareController.officeBar`/`officeText`; `invest.hud`/`stampTray`; `coordinator.stampTray`/`hud`/`examiner`; the stamp's `onClick` → `StampTray.Open` (next to its reaction); the binder's examiner and tray.
- **Checks**: an error for a document template whose field count exceeds `PaperFace.Capacity` (with and without its photo), next to the spawn-slot check. Re-run after changing `face`.

### 3.6 Content and copy

| Key | Text | Change |
|---|---|---|
| `desk.scanHint` | "Click a paper to read it; drag it onto the scanner to open it on the PC." | text (Full tier, ASCII, 72 characters; the note auto-sizes) |

The stamp tray reuses `accept` and `deny` (piece 6 flavour labels with gloss), the claim tag `claim.banner`, and the strip `compare.*`. No other string is added. Generate World regenerates the library's UI table.

### 3.7 Why some logic stays outside Domain and Visuals

The glue writes TMP text, poses transforms, reads the camera and routes pointer events. Every rule it applies is tested code:
- the row order and tongue rows (`DocumentRows`);
- pairing, clearing and match (`ComparePair`, `PickKeys`);
- holds, slots, eviction and click routing (`DeskPapers`, `PaperClicks`);
- the input table (`BoothRules`);
- the pose and the region layout (`ExamineLayout`);
- the face and its hit test (`PaperFace`);
- the reveal clock (`RevealClock`);
- the bubble's tag and hold (`SpeechQueue`).

`EvidencePicks` is glue because its labels come from `UiText` and its shown text from `CaseTranslation` (Assembly-CSharp). Its keys and evidence are the tested `PickKeys` and `CompareEvidence` factories.

### 3.8 Intent audit: the existing mechanism tried for each new one

| New | Existing mechanism reused or tried | Why something new was still needed |
|---|---|---|
| Examine pose | `DeskDocument.SetLift` (the sheet child already moves off the plane) | reused: the examiner moves the same `Sheet` child, so the root, its drag and its put-back spot are untouched |
| Readable face | `DocumentWindowController.Rebuild` (the row logic) | its logic moves into `DocumentRows` + `DocumentRowView`, used by both |
| Desk compare | `CompareController` (one scene instance, its bar, its colours, `PairCompared`) | its pair state moves to Domain and it gains a second bar and highlight kinds; no second controller |
| Evidence at the desk | the five inline `CompareEvidence` builds | gathered into `EvidencePicks`; nothing duplicated |
| Clicking a paper | `Clickable.onClick` (a parameterless event) | needs the button and hit point, so `DeskDocument` handles `OnPointerClick` itself; `Clickable` stays for hover, cursor and interactable gating |
| Put back on the desk | `ClickCatcher` (the frame's surround) | reused on a 3D box |
| Frame hole | `RectHoleRaycastFilter` | reused as a second component |
| Stamp tray | `TravellerWheel` (host + catcher + projection) | same pattern and `OverlayProjection`; the wheel itself is traveller-bound and its hub is full |
| Claim tag | the PC's `claimText` | the same formatted string, written twice |
| Reveal state | piece 9's `_revealedAt` | moved into a shared `RevealClock` |
| Bubble hold | `SpeechQueue` pacing | a hold flag and a tag; no second queue |

## 4. Piece 9 interplay

- **Supersedes** in piece 9's spec: T2's "The physical paper shows only its title and holder, so it does not change". The paper now shows every value by T2's rules, from the same `CaseTranslation.Field`. Also §2.11's "Checked and unchanged: `DeskDocument`".
- **Supersedes** T8's written reveal point ("the scan (`OpenDocumentWindow`, the first time each document opens this case)"). The reveal is now the first sighting: the paper examined, or the scanned window shown in the open frame (X25). This takes piece 9 §7's own lever for its "flip plays unseen" risk.
- **Extends** R7: one reveal per document, shared by the paper and the scanned copy (X18). With reduced motion, both show English from the first sighting. A row click on either surface finishes both.
- **FEATURES:** piece 9's line "With the Papers translator, a scanned document's foreign values flip into English letter by letter from the scan …" becomes "… from the first time the document is seen up close: its paper examined at the desk, or its scanned window shown in the open PC frame; the paper and its scanned copy share one reveal …".
- **Unchanged:** T5, R10 and R11. The strip, like the PC bar, shows the placeholder for an untranslated side and never prints pseudo-script. Evidence stays canonical.
- **Fonts (R6):** `TextFlip` swaps the paper's world `TextMeshPro` texts to the script's runtime font asset and back. This is untested on a world text. The plan's first Unity step is a spike: hieroglyphs, Arabic and Han on a held paper, with the tracked TMP assets unchanged afterwards.
- **Faint hieroglyphs** (piece 9 §8): a held paper's values are about 24 px at 1080p and 16 px at 720p. The scanned page's rows in the frame are 18 desktop units on a 1080-unit desktop drawn 840 px tall, so about 14 px at 1080p and 9 px at 720p. The paper in hand therefore draws glyphs about 1.7× larger than the PC. The weight lever (a face dilate per script, or Noto first in the chain) stays piece 9's follow-up. A hieroglyph passport at 720p is screenshotted (§6.5.12).
- **Wide glyphs** (piece 9's `FillRest` fix): the desk rows have fixed boxes from `PaperFace` and auto-size, so wide glyphs shrink into the value box. The label keeps its own line above, so it can never be squeezed.
- **The bubble:** piece 9's flip and hold (`revealSeconds`) run before the X11 hold. Clicking an untranslated answer puts the placeholder in the strip. Opening the wheel still finishes the bubble's flip.
- **The claim tag** is English (T2 "the claim banner" is always readable), so the destination stays readable at the desk from day 2 without the Speech translator.
- **Order:** this piece builds after piece 9 merges. If piece 9's names moved in its merge, the plan's Task 0 records them and this spec's glue follows. Its rules do not change.

## 5. Tests (EditMode, `Assets/Tests/EditMode`)

- **`DocumentRowsTests`** (new):
  - page-major order with authored order within a page;
  - nulls skipped, and `Index` is the index in the field list;
  - `TongueRow` counts only rows in the tongue, across the document for `Ordered` and within the page for `OnPage` (the permit: Declared Device page 0, Bond Currency page 1);
  - `PageCount` for no fields (1), one page, and page 1 fields (2);
  - `HasTongue`: a passport (Coin of Issue) true; Name and Date of Birth only false; no fields false.
- **`ComparePairTests`** (new):
  - the first pick is Pending and the second Paired;
  - picking A's or B's key again gives Cleared and empties both;
  - a third pick after a pair gives Pending with it as A;
  - blank keys never match each other;
  - `Matches` uses each side's `MatchValue` (a garment's shown item against its Culture value), case-insensitive and trimmed;
  - `Clear`;
  - `PickKeys`: the same field gives the same key; kinds never collide; the format examples above.
- **`DeskPapersTests`** (extended):
  - `Hold` only from `OnDesk` (with the traveller, scanning, returned and out of range are refused);
  - the preferred free slot, then the other;
  - both full: the paper held longest is evicted, its slot taken, and it is `OnDesk` and draggable again;
  - `PutBack`, and `PutBackAll` in slot order;
  - a held paper `CanDrag` but its `Drop` is Refused;
  - scanning is independent of holds;
  - `ReturnAll` clears holds;
  - `OnDeskCount` counts held papers;
  - `PaperClicks.Decide` as a full 2×2×2 decision table;
  - the renamed `DeskHints.ScanHintVisible` cases.
- **`BoothRulesTests`** (extended): one case per new output per row of §2.7; `StampOpen` makes the CRT, power, props, papers and traveller inert; `HeldPapersLive` while the frame is open; `DeskCatcherLive` and `ExamineEscapeLive` need `PapersHeld`; `CaseHudVisible` is false while focused.
- **`ExamineLayoutTests`** (new):
  - the `Pose` worked example;
  - office slots are symmetric, their bottom sits at `officeBottom`, and they never overlap at 16:9 and 21:9;
  - a dip lowers by `wheelDip`;
  - the narrow-aspect clamp (5:4);
  - `InRegion`: one paper centred; two side by side in a 1.2-heights region (21:9); two stacked in a 0.611-heights region (16:9); `fits` false below `frameMinHeight`; no box leaves the region;
  - `SafeDistance`: the worked example (1.637 m depth, no cap at 0.6); a desk close enough to cap; the near-clip floor; rays going up give no cap;
  - `Ease` endpoints and monotonic.
- **`PaperFaceTests`** (new):
  - no row overlaps the title or the photo;
  - rows beside the photo end left of it, and later rows are full width;
  - `RowAt` inside each hit rect gives its index, and the title, photo, margins and gaps give −1;
  - `Capacity` is 6 with the defaults, with and without a photo;
  - more rows than capacity lay out only the capacity;
  - no photo means full-width rows.
- **`RevealClockTests`** (new): NaN before a start; the first `Start` wins; `Elapsed` counts from it; `Finish` gives +∞; a `Finish` before `Start` does nothing.
- **`SpeechQueueTests`** (extended):
  - `Tag` follows the shown line and is −1 when none; `Clear` resets it;
  - a held, fully shown line never ends; typing continues while held; after release it ends a hold (or the minimum, when a line waits) later;
  - queued lines wait while held;
  - every existing test is unchanged.
- **Unchanged and green:** `DiscrepancyLogTests` (the proof rules are untouched), `InterviewScriptTests` (their `CaseDocument` helper loses `holder`), and the rest.

## 6. Verification plan (in the art office)

**Offline after every task,** in `E:\unity\NOPE-art`:
- `SCRATCH/compile_check_art.py` reports 0 errors in every project;
- the reflection runner on `SCRATCH/cc_art/Temp/Bin/Debug` passes every test (piece 9's record: `passed 946` at its final code; the plan's Task 0 measures `d5844d0` and each task states the new total).

**In Unity 6000.4.11f1** on `E:\unity\NOPE-art`, with temporary `_TimeDeskP10*` scripts (the office move's `SCRATCH/move/final_automation` pattern and piece 9's `p9` automation), never committed, with reports and screenshots in `SCRATCH/p10_*`. The office is the art `OfficeScene` with the additive `OfficeGameplay`, loaded by Title → New Run and Home → Sleep. **No 2D scene is opened.**

0. **Baseline:** the generation dump (seeds 12345 and 999 × days 1–6, piece 9's columns). After the piece it must be byte-identical (X23).
1. **Font spike on a world text** (§4). Stop and report if a script does not draw.
2. **Content:** Generate World twice (the second run changes nothing); Validate Content Library clean; `desk.scanHint` has the new text.
3. **EditMode suite** through `TestRunnerApi`: every TimeDesk test passes (the two known UnitySkills failures are reported and ignored).
4. **Builder:** two builds give equal semantic dumps; the new objects exist and are wired; no untagged graphic; the capacity check is silent (4 and 2 fields); the art office file is byte-unchanged.
5. **Play-through** (seed 12345; days 1–3; day 2 without translators, day 3 with Near East and Mediterranean Papers and Speech granted as piece 9's run did). Each check logs pass/fail, and each resolution step sets the Game view to **1920×1080, 2560×1080 and 1280×720**:
   1. **Arrival:** the claim tag reads the PC banner's text; the passport lands with 4 rows and the photo; the day-1 note has its new text. Screenshots `p10_arrival_{1080,2560,720}`.
   2. **Examine one:** a click lifts it into the left or right slot by its side. Its projected screen rect equals `OfficeSlot` within 2 px, and it covers neither the traveller's head, the bubble, the PC's nor the stamp's click box centre. Each value text's rendered height is at least 14 px at 720p, and each text is drawn inside its box (piece 9's bounds check). Screenshots `p10_examine_one_{…}`.
   3. **Examine two:** request the permit and pick it. Side by side, with no overlap. A third paper (a debug hand-over of a copy) evicts the one held longest. Screenshots `p10_examine_two_{…}`.
   4. **Put back** each way (right-click, a click on the title, a click on the desk, Escape): each slides back to where it lay and lands on top. A click on a prop while holding reacts and keeps the papers.
   5. **Drag-out:** drag a held paper onto the scanner. It scans, then returns to the spot it was held from. A drag onto a busy scanner is refused.
   6. **Compare at the desk:** passport Coin of Issue against permit Bond Currency gives MATCH with nothing logged, and both rows lit (`p10_compare_papers`). A question's answer in the bubble (hover holds it past 4 s) against a paper row gives MISMATCH for a spoken-tell liar, nothing logged (`p10_bubble_pick`). Picking the same row again clears.
   7. **Frame region:** holding the passport, open the PC. The paper sits right of the frame (`p10_frame_region_{…}`, both papers stacked at 1080p and side by side at 2560). Click the Currency Ledger's claim row in the glass, then the paper's Coin of Issue beside it: DEVIATION LOGGED in the PC bar and in the Deviation Report. A click beside the papers closes the frame, and the papers return to their office slots.
   8. **Cross-surface pending:** pick a book row in the frame, close it, and pick a desk row. They pair, and the office strip shows the verdict (`p10_cross_pending`). The same field picked on the scanned copy, then on the paper, clears.
   9. **Wheel:** opening it dips the held papers under the ring (their projected tops below the ring's lowest item), and they are inert (`p10_wheel_dip_{…}`). Look ▸ a garment: the office strip shows "Traveller · OUTFIT: …" (`p10_look_strip`). An answer picked in the bubble while the wheel is open keeps it open.
   10. **Stamp:** the tray opens above the stamp (`p10_stamp_tray_{…}`). Deny with no deviation gives an unproven denial, and the citation slip shows over the office with the clock paused (`p10_citation_office`). Acknowledge resumes. Accept on an honest traveller is correct and pays. The PC's Accept/Deny still work.
   11. **Escape order:** with the frame open and papers held, one Escape closes only the frame. With the wheel open, one Escape closes only the wheel. Then one Escape puts the papers back.
   12. **Day 2, untranslated:** a foreign passport's values show glyphs on the held paper in the script's runtime font, with labels, name and date in English (`p10_untranslated_paper`). This includes a hieroglyph passport at 720p (`p10_hieroglyphs_720`): its glyphs are drawn, not blank or tofu, and it is judged by eye against piece 9's `p9_day2_papers_hieroglyphs`. A pick puts the placeholder in the strip (`p10_placeholder_strip`), and the proof against the book logs in English.
   13. **Day 3, translated (X25):**
       - the first examine flips the values (sampled every 0.1 s: English by the longest row's duration), and a later scan shows English at once, with no replay;
       - the permit, scanned with the frame closed, stays untranslated on the office PC's glass (its window's value text is the foreign form); opening the frame starts its flip in the glass; examining it afterwards shows English;
       - a scan made while the frame is open flips at once;
       - a row click mid-flip finishes both surfaces;
       - reduced motion shows English at the first sighting.
       Screenshots `p10_flip_mid`, `p10_flip_done`, `p10_flip_frame`.
   14. **Closing and decision edges:** decide while holding two papers and while a paper scans. Every paper leaves, and the next case starts clear (no stale highlight, empty strip).
   15. **Log:** only the known warnings (the binder's anchor warning; day-1 closing notes). `git status` shows no change to the tracked TMP font assets.
6. **Hygiene:** revert Unity-touched files that are not part of the work, commit the regenerated content and the rebuilt `OfficeGameplay.unity`, delete the `_TimeDeskP10*` files, and append the record to this spec.

## 7. What is still PC-only after this piece

| Item | Why it stays | Office path |
|---|---|---|
| Reference books and Citizen Records (the truth rows) | E5: the complex work. Proofs need a truth row. | Pair a PC truth row with a desk statement in one view (X10) or across the frame (pending picks). Q1 asks about desk books. |
| The transcript (re-reading or picking a missed answer) | the record | the bubble while it shows, held by hovering (X11); Q2 asks about a desk transcript slip |
| The Deviation Report (the list) | the record | the office strip announces each deviation and each "already documented" |
| Directives during the shift | the morning paper carries them; the window is on the PC | Q5 asks about a desk bulletin |
| Scanning's extras: desktop icons, reopening windows | PC functions | – |
| A premade's Citizen Records note | E5 | – |
| The desk's own lines (the opener, the questions) in the transcript | the player's own words (wheel labels) | – |
| The placeholder apps (Internet, Lexicon, Dialect, Material, Notes), Settings, Turn off screen, Quit | the PC's own | – |

**Moved into the office by this piece:** the claim and role (the claim tag), field reading (examine), answers as evidence (bubble picks), the compare bar (the office strip), garments' picks (the strip), the verdict (the stamp tray), the verdict line and the citation slip (the overlay), and the papers' translation flip (first sighting: in hand, or in the open frame).

## 8. Retired, superseded, out of scope

**Code removed:**
- `CompareController.Select(string, string, Image, CompareEvidence)` and its `Slot` struct;
- the five inline evidence builds;
- `DeskController.BringToFront` on a click;
- `DeskDocument.holder` and `CaseDocument.holder`;
- `DocumentWindowController._revealedAt`, `Reveal()` and `FinishFlips`' private sentinel; its `FillRest` moves to `DocumentRowView`;
- `DocumentInstance.PageCount`'s loop (it calls `DocumentRows.PageCount`);
- the desktop's `VerdictStrip`, `VerdictText` and `CitationPanel` (rebuilt on the overlay).

**Earlier spec lines superseded** (kept as approved records, not edited):
- physical-desk K11 ("the verdict stays on the PC"), K12 ("a physical paper shows its title, the holder's name and a reserved photo slot; every field is read and compared on the scanned PC copy"), K19/R19's "the physical paper keeps its source text", §1.5's "A click without a drag brings it to the top", §1.7's "The bubble never takes clicks" and "Only transcript answer rows are compare-clickable";
- office-move M7 (papers as lit quads: kept on the desk, unlit while held);
- piece 8 W6's pacing (a hovered line holds);
- piece 9 T2's paper sentence, §2.11's `DeskDocument` line, and T8/R7's reveal at the scan (§4, X25).

### 8.1 `docs/FEATURES.md` (in the commit of each behaviour; builder-driven lines in the scene commit)

Line numbers are `d5844d0`'s. The plan re-finds each line.
- **:18 (3D input):** add "while the PC frame is open, papers held in the hand sit beside it and their rows stay clickable (a hole in its click-outside area); a click elsewhere outside the frame still closes it".
- **:30 (Papers), rewritten:**
  - "papers show their document's whole face (the title, every field row in page order, the passport's photo; the rows the scanned copy shows, from one row source; tested: `DocumentRowsTests`, `PaperFaceTests`)";
  - "a click on a paper lifts it into the hand, in front of the camera: two slots low beside the screen's centre, the side it lay on first; a third sends back the one held longest; right-click, a click on it off its rows, a click on the desk or Escape puts it back where it lay; a held paper evenly lit (unlit while held) and readable from 1280×720 to 2560×1080; dragging a held paper drops it back onto the desk under the pointer and on, e.g. to the scanner; while the wheel is open held papers dip below it (holds, slots and click routing tested: `DeskPapersTests`; the pose and layouts tested: `ExamineLayoutTests`)";
  - "(the click no longer brings a paper to the top; a paper put back lands on top)".
- **:31 (scanner):** add "a held paper can be dragged straight onto it".
- **:32 (Day 1 notes):** "the scanner note ("Click a paper to read it; drag it onto the scanner to open it on the PC.") until the first read or scan".
- **:33 (wheel):** add "papers held in the hand dip below the ring while it is open".
- **:34 (bubble):** replace "read-only; the PC transcript is the evidence" with "an answer line is compare-clickable (hand cursor; it lights up when picked); hovering the bubble holds its line; the bubble sits above the open wheel (tag and hold tested: `SpeechQueueTests`)".
- **:35 (desk objects):** "stamp: while a traveller is at the desk, opens the stamp tray (Accept left, Deny right, the PC buttons' labels and glyphs); a choice decides the case like the PC's buttons".
- **New bullet under Office scene:** "The office case HUD, top centre, while a traveller is at the desk and the PC frame is closed: the claim tag (the PC banner's text: name, role and claim) and the office compare strip (the PC compare bar's text and colours); the verdict line and the citation slip show over the office (the slip still pauses the day until Acknowledge)".
- **:45 (Clue Log):** add "the bubble's answer lines are compare-clickable too".
- **:62 (Claim banner):** "on the PC and on the office claim tag".
- **:70 (Click-to-compare):** "any two values from the desk and the PC: a held paper's rows, the bubble's answer, a garment (Look), and every PC row; one comparison shown in the PC bar and the office strip; picking a picked value again clears it, on either surface; a pick waits across the PC frame's opening and closing (pairing, clearing and keys tested: `ComparePairTests`)".
- **:79 (Accept / Deny):** "on the PC and on the desk stamp's tray".
- **:88 (Papers translator):** see §4.
- **:151 (Build Office UI):** add "the paper face (its row template and highlight), the examine material, the desk catcher and examiner, the frame's second hole, the office case HUD, the stamp tray, the verdict line and citation slip on the office overlay, the bubble's input above the wheel; it reports a document template with more fields than the paper face holds".

**Out of scope, deferred with their seams:**
- **E6 confront.** After `HandlePairCompared` logs a proof, a wheel entry could ask about it. The seam is piece 3's dialog system, with a mid-case gate (`InterviewDay.OfferedDialogs` reads only day-start gates today).
- **Physical stamping** (the passport under the stamp, ink, handing it back): `StampTray.Decided` is the one entry.
- **Desk reference books or claim cards** (Q1), a desk transcript slip (Q2), a directives bulletin (Q5).
- **Decoration** (piece 7 item 7): held papers never touch `DeskSlot`s.

## 9. Risks

- **Overlay versus world clicks.** Held papers are world objects, and any overlay graphic above them eats their clicks. Handled: the frame's hole (X10), the modal wheel and tray (inert by rule), and the HUD strips placed at the top, clear of the slots. **Check every new overlay against the slots** (the play-through's click checks).
- **Readability at 720p:** labels are about 11 px. The slot height is a knob. If the screenshots judge labels too small, raise `officeHeight` (0.48 still clears the head at 1080p: top 540 px against the head's 610 px) or drop the label share.
- **The art has moved on** (`art` `a1ec7d2` rescaled and re-laid the desk). The slot numbers were measured on `23aa6e1`. They are knobs; re-check the screenshots after the art merge.
- **The art's volume** vignettes (0.2) and tonemaps the unlit paper. The slots are central, so the vignette barely reaches them. The camera uses SMAA (no TAA jitter on text).
- **World TMP with runtime script fonts** is unverified (§4). The spike runs first.
- **Faint hieroglyphs** (piece 9 §8): they are larger in hand than on the PC (§4). Hieroglyphs that are still too faint are piece 9's weight lever, not a reason to hold this piece.
- **The reveal moves (X25):** a player who scans and never opens the frame nor examines keeps untranslated text on the small screen, where it was unreadable anyway. The transcript's and the bubble's reveals are unchanged.
- **Mat coverage:** with more than two documents, a third paper can hide under the held ones (X22, Q7).
- **Click versus drag on a held paper:** a 10 px wobble turns a row click into a drag-out. It is the EventSystem's threshold. If playtests complain, raise `pixelDragThreshold` while papers are held.
- **Escape's same-frame guard** depends on each handler's own live-since frame. §6.5.11 tests the order.
- **Evidence gate at the desk:** a player who never opens the PC cannot prove a liar (E1's deviation). The new day-1 note names the PC. Q1 is the design lever.
- **Scope:** this piece touches the compare path the whole game uses. Task order (plan) keeps the PC path behaviour-identical through the refactor commit before any desk behaviour lands.

## 10. Open questions for Saleh

- **Q1.** **Proof at the desk:** every proof needs a book row or a Citizen Record, which live on the PC. Should the books (or a card with the claimed place's rows) be desk objects, like Papers, Please's rule book, so a liar can be proven without the PC?
- **Q2.** **A desk transcript slip** (Papers, Please's printed interview) so a missed answer can be picked at the desk, instead of only on the PC's transcript?
- **Q3.** **"Why" in speech:** the role shows only on the claim tag ("Lysimache (Merchant)"). Should the traveller also say their purpose in the bubble (one authored line per role)?
- **Q4.** **Stamping:** is the stamp tray enough, or do you want physical stamping (drag the passport under the stamp, ink mark, hand it back)?
- **Q5.** **Directives at the desk:** a bulletin paper with the day's rules on the desk?
- **Q6.** **Translation at the desk:** confirm that the Papers translator reads the paper in your hands (decided yes, E2), not only the scanned copy, and that the flip waits until you look at the document (X25) instead of playing on the small screen at the scan.
- **Q7.** **The mat under held papers:** with two documents the mat is empty when both are held. When more documents come, is putting one back to reach a hidden paper acceptable?
- **Q8.** **Confront** (E6): after a proof, should the wheel offer "About your currency…" (a small dialog piece)?
- **Q9.** **Diegetic HUD:** should the claim tag become art (the dark board above the portal as a departures board, an art anchor), instead of an overlay strip?
- **Q10.** **Merging into `art`:** you play on `art`, which does not contain `main` (pieces 0–9). When should `art` take `main`, so you see the desk flow?
