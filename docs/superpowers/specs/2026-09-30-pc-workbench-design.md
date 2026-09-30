# The Investigation app as a workbench: guided steps, a document shelf, two documents and click-and-match: design

*2026-09-30 · Track A of wave 5 (`feat/pc-workbench`, from `main` `fe96fe7`) · built by Claude from the approved prototype (`SCRATCH/wave5/prototype_v3.html`, the claude.ai artifact 41Pd8SB36Vg7D8wzW7Pzfm v3) and the Papers, Please lessons 1, 2, D5, D6 and D10 · decisions taken for Saleh are listed in §9 · the code wins over this text*

Saleh on the PC that landed the night before (`2026-09-30-pc-ux-redesign.md`), verbatim:

> "there is a lot of information presented to the player immediately, no clear buttons no clear sections nothing is readable the design is outdated and painful to use. the process is incredibly overwhelming no clear user friendly steps"

His answers that shaped the prototype: the papers area shows **two documents at a time** and either side is easy to switch; comparing is **click-and-match on the documents themselves** ("this is still a game... no extracting values, he will click and match"); a **visible line** joins the two compared values with a label (Matching data, Data differs, Meets the rule, Breaks the rule, Different details), Papers, Please style.

This spec replaces the Investigation app's layout, navigation and compare surface of `2026-09-30-pc-ux-redesign.md` (its IA1-IA11, §3's navigator and toolbar, C2-C6, the compare dock). What stays is listed in §8. The skills Saleh chose for the pass, `redesign-existing-projects` and `minimalist-ui`, are applied within uGUI: the audit is §0, the look is §5.

## 0. Audit of the navigator build (`fe96fe7`), against the complaint

| # | What the clerk saw | Why it hurt |
|---|---|---|
| 1 | A 308-unit navigator of six sources, their items, a case summary and a six-step checklist, all visible at once, beside two panes, a toolbar, the dock and a toast | Everything at the same time: nothing said what to do first ("no clear user friendly steps") |
| 2 | The compare happened in a yellow dock at the bottom of the screen, far from the two values | The eye had to leave the documents to read the verdict; nothing on the documents showed what had been compared |
| 3 | Every compare vanished at the next pick; only proofs reached the Deviation Report, a separate source | The player's own work was not visible; "what have I checked?" had no answer |
| 4 | Accept and Deny sat in the toolbar from the first second | The decision competed with the investigation and could be pressed with nothing checked |
| 5 | Deep slate title bars, blue selection plates, green chips, yellow dock, grey sidebar | Several loud surfaces, none of them meaning a state ("outdated") |
| 6 | Search, Pinned and Recent in a palette under the toolbar field, over the panes | A second navigation competing with the first |

## 1. Principles

| Id | Principle |
|---|---|
| W1 | **One step at a time.** The app is a sequence of guided steps (Papers, Records, Books, Rules, Decision). Each step says in one sentence what to do and puts a sensible pair of documents up. Steps suggest; they never lock. |
| W2 | **Two documents, the player's choice.** Every document of the case and the day sits on one shelf. A click opens it on the target side; the one already open on the other side swaps. |
| W3 | **The player points, the game confirms** (lesson 2). A click on a value holds it (a dashed line follows the pointer); a click on another value compares them. A line joins the two values with a label; the result lands in the findings. Nothing is marked by itself. |
| W4 | **Your work stays visible.** The findings column lists every comparison that meant something, in order, and a click on one shows it again. |
| W5 | **The decision comes last** and is argued from the findings: Deny needs a logged difference or a broken rule. |
| W6 | **Quiet surfaces, colour only for state.** White and warm paper surfaces with hairlines; green only for a match, red only for a difference, blue only for the value being held, the culture's deep colour only for the primary action. |

## 2. Information architecture

```
Investigation (window, maximised the first time)
├─ Header: the traveller's face (the person at the desk), their name, the papers' counters · the five steps
├─ Shelf (wraps): TRAVELLER each paper, Transcript · AGENCY Citizen records, Today's rules, Calendar · BOOKS each book
├─ Work
│   ├─ Lead: the step's title · [Search Ctrl K]
│   ├─ Status line: the step's sentence (the hint), the value being held (Cancel, Esc), or the last result
│   ├─ Two panes (Left, Right): header (side tag, document name, "Shelf opens here" on the target) and the document
│   │   └─ the line layer over both panes: the labelled line, or the dashed line to the pointer
│   └─ (Decision step) the decision: what was logged, Accept and Deny
├─ Findings column: every logged comparison; "Open the report" (the Deviation Report form)
├─ Foot: Back · "Step 2 of 5 · To check: Visa class against the account" · Next: <step>
└─ Search drawer (Ctrl+K, Ctrl+F; from the right, over a dim): the field, Pinned and Recent before typing, results grouped by source; a result opens on the target side
```

| Id | Decision | Replaces |
|---|---|---|
| IA1 | **The header** holds who is at the desk: the face (the traveller's look drawn from the character layers, the person, not the paper's photo: lesson D8 compares them), the name (40 u, bold, shrinking to 24 u to fit), and one muted line of counters ("Papers received 2 of 3 · scanned 2"; "Waiting for the next traveller" between travellers). No claim: the traveller says it (Saleh's Q5, personalities B1-B3); it is on the shelf as the transcript's line. | The navigator's case summary; the prototype's claim line |
| IA2 | **Five steps** at the header's right: Papers, Records, Books, Rules, Decision (`CaseGuide`). A step is a numbered pill; the current one is filled with the primary colour, a finished one carries a tick. A step is finished when every check of the day's steps checklist that belongs to it is done (`CaseGuide.StageOf` over `CaseSteps.Evaluate`: papers received, read and asked for, answers, the look and paper-against-paper compares belong to Papers; record lookups and compares against a record to Records; compares against a book to Books; the rules read to Rules). A step with no checks is finished once it was left. Ctrl+1…5 go to a step. | The checklist (StepsPanel) and its toggle |
| IA3 | **Each step suggests a pair** (it never locks): Papers: the first two papers (the second slot: the transcript when there is one paper); Records: the first paper and Citizen records, looked up by the first paper's Citizen ID (else the name: the checklist's PrimaryName jump, `CaseSteps.Target`); Books: the first paper and the first book, turned to the claimed place; Rules: Today's rules and the first paper; Decision: no panes, the decision. Going to a step puts its pair up and makes the right side the target. | The checklist's jumps |
| IA4 | **The shelf** lists every document in three groups, in this order: Traveller (each paper by its name, one not readable yet dimmed and saying why when opened; the Transcript), Agency (Citizen records, Today's rules, the Calendar), Books (each reference book). A chip shows which side it is open on ("Left", "Right") and a dot until it was first opened this case or while something new waits in it. The shelf wraps onto as many rows as it needs (a group's word never ends a row); the work area takes the rest. As built it takes three rows at 1440 u in the neutral font (11 chips): see §9.11. | The navigator's sources and items |
| IA5 | **Target side.** One pane is the target (its header says "Shelf opens here", its side tag filled); a click on the other pane's header makes that one the target; F6 too. A shelf chip opens its document on the target; a document already open on the other side swaps sides. The window restored (1120 u) holds one pane: it is always the target. | Open beside / Close, the active pane's underline |
| IA6 | **The Calendar** is a new shelf document (lesson D10): the agency calendar's month with today marked and "Today" as a value to click and match (with a departure date: same day or not; with a Valid Until: still valid or expired). | — (the date lived only in the tray) |
| IA7 | **Today's rules** are clickable: a rule is held like a value and matched with the value it is about (§4.3). The transcript's answers were already values. | The Directive Memo read-only |
| IA8 | **The findings column** (280 u, right; newest first) replaces the Deviation Report as the place to see the work; the report form stays one click away ("Open the report", on the target side). | The Deviation report source |
| IA9 | **Search is a drawer** from the right (Ctrl+K, Ctrl+F, or Search at the lead's right): the field, Pinned and Recent while it is empty, results grouped by source; a result opens on the target side and closes the drawer; Esc or a click on the dim closes it. | The toolbar field and its palette |
| IA10 | **The decision** is the last step: "You logged 3 findings; 1 difference to cite." Accept (the traveller goes through) and Deny (cites the first difference or broken rule; greyed with "Log a difference or a broken rule first" until there is one). The desk's stamp tray still decides too, unchanged. | Accept and Deny in the toolbar |

## 3. Layout (desktop units; the window maximised is 1440 × 1020 under a 44 u title bar)

| Part | Size |
|---|---|
| Header | 96 u: 24 u side padding; face 64 u; name 40 u bold over the counters at 24 u; the step pills 52 u high in an 800 u row at the right |
| Shelf | 12 u padding above and below, 24 u at the sides; chips 40 u high, 8 u apart, rows 6 u apart (group word 24 u caps, muted); the work area starts under its last row |
| Findings | 280 u wide at the right of the work area, a hairline to its left |
| Lead | 44 u: the step's title at 30 u bold; Search (Ctrl K, 230 u) at its right |
| Status line | 68 u (two lines at 24 u in the tallest culture font): a plate (blue while holding, green or red after a result, warm grey otherwise) with the line at 24 u and, while holding, Cancel (Esc) |
| Panes | the rest: two panes 14 u apart; each a 64 u header (side tag, the document's name at 26 u bold on up to two lines, the target hint at 24 u) over the document; a scanned copy is 520 u wide, the pane at least 520 u |
| Foot | 76 u: Back (a text link), the progress line (24 u), Next (the primary button, 52 u) |
| Search drawer | 580 u wide, the window's body high, over a dim |

At 1280 × 720 one unit is 0.519 px: the 24 u floor is 12.5 px (the project's minimum), every label keeps it. Long words wrap; nothing is cut with an ellipsis but a search hit's one-line snippet (the whole line is one click away).

## 4. Click-and-match

### 4.1 Holding and matching

- A click on a pickable value (a paper's box, a record's row, a book's row, a transcript answer, a rule, the calendar's Today) **holds** it: its box takes the holding colour, the status line reads "Holding **Premium** (Leisure Departure Visa · Visa Class). Click another value to compare." with Cancel (Esc), and a dashed line in the holding colour runs from the box's side to the pointer. A garment looked at on the desk's wheel is held the same way (the status line names it; there is no box to draw from).
- A click on the same value again lets go. A click on another value **compares** the two: the pair goes to the one compare (`CompareController.Select` twice: the Domain's `ComparePair`, `PairCompared`, `DiscrepancyLog.Prove` and the Deviation Report are untouched), and the workbench classifies it (§4.2).
- The two boxes are outlined in the result's colour and **a line joins them** (a curve from the side of each box facing the other, 3 u, dots at both ends) with its **label** on a plate at its middle. It stays while both boxes are on screen (it follows scrolling) until the next hold.
- A comparison that means something is **logged** in the findings (once per pair of values; comparing them again says "You already compared those two. Shown again.").

### 4.2 What a pair of values says (`FindingRules.Classify`, Domain, tested)

| Pair | Label | Logged | Colour |
|---|---|---|---|
| Different categories (a name against a date) | Different details | no | grey, dashed |
| A book row for another place than the claim, not proving anything | Not the claimed place | no | grey, dashed |
| A record row of another person | Another person's record | no | grey, dashed |
| Two truths (two book rows, a book row and a record row) | Both are reference data | no | grey, dashed |
| A proof (`DiscrepancyLog.Prove` returns a deviation): mismatch against the claim's book row or the record, two papers that disagree | Data differs | yes, as evidence ("Logged as evidence") | red |
| A proof by a foreign origin (the stated value belongs to another place's row) | Belongs elsewhere | yes, as evidence | red |
| Same category, same value | Matching data | yes | green |
| Same category, different values, no proof (an answer against a paper: a hint) | Data differs | yes | red |

A logged difference that is not a proof enables Deny but is not a deviation: denying on it alone is still an unproven denial (the existing rule, `VerdictRules`).

### 4.3 Rules and the calendar (`RuleChecks`, `FindingRules.AgainstToday`, Domain, tested)

A rule is held against a value (or a value against a rule):

| Rule type | About | Verdict |
|---|---|---|
| A closure (era, nation, both) | Destination | Breaks when it closes the traveller's claimed place, else Meets |
| Paper set | Visa class (AccountStatus), transponder class, signature, waiver, proof of means, contract rows | Breaks when the traveller's paper set is incomplete or wrong (`Directives.FaultOf(PaperSet)`), else Meets |
| Debt standing | Status, Debt | Breaks on a Frozen account |
| Papers' dates | Departure date, Valid Until | Breaks when that value is not today (a departure) or has passed (a Valid Until) (`Directives.PaperDates` over that value alone) |
| Transponder recall | Transponder | Breaks when the manifest prints the recalled model |
| Dress, return home, no 2150 goods, a procedure line | — | "Check this rule by comparing: hold the value against the books or the record." Nothing logged |
| Any rule that does not apply to the traveller's kind | — | "This rule is not for this traveller." Nothing logged |
| A rule against a value it is not about | — | "That rule is not about the <category>." Nothing logged |

Meets and Breaks are logged (green "Meets the rule", red "Breaks the rule"). The calendar's Today against a departure date logs "Departs today" (green) or "Not today's date" (red); against a Valid Until "Still valid" (green) or "Expired" (red); against anything else "Different details". Two rules, a rule and the calendar, say "Hold a rule against the value it is about." The rule is read from the case's `CaseFacts` (`CaseInstance.facts`, kept by `CaseFactory`; the waiver pad's signature updates them).

### 4.4 The findings (`FindingLog`, Domain, tested)

Each entry: its kind, the two values' keys, the category. The column draws a plate per entry, newest first (a new one is always in view): a match in green ("Visa class matches" · "Premium (Visa) against Premium (Citizen record)"), a difference in red and bold ("Visa class differs" · …, and "Logged as evidence" for a proof). A click on an entry opens both documents (left: the first value's, right: the second's) and shows its line again. `FindingLog.HasDifference` enables Deny.

## 5. Look (the skills' principles in uGUI)

- **Surfaces.** Five new theme roles (appended): `Surface` (white panels: header, shelf, findings, pane headers, foot, drawer; ink the culture's ink), `SurfaceMuted` (muted captions on them, AA 4.5:1), `Hairline` and `HairlineStrong` (1 and 2 u borders), `PrimaryAction` (the step's Next, the current step's number, the target side's tag: the culture's deep colour and its ink). Three for state: `FindingMatch`, `FindingDiffer` (the findings' plates, the result's status line, the line's colours), `Holding` (the held value, the dashed line, the status line). One for notes: `Info` (the teaching line, the grey labels). Their seeds: `line`, `lineStrong`, `matchBg`, `mismatchBg`, `infoBg`, `holdBg`, `hold` (neutral only; the cultures take them from it; their `match`, `mismatch`, `ink`, `chromeDeep` stay theirs).
- **Neutral colours.** The window body is `#FBFBFA` (screen), surfaces `#FFFFFF`, hairlines `#E6E4DF`/`#C9C6BF`, ink `#1D1F21`, muted `#6F6E6A`, match `#2F5E33` on `#EDF3EC`, difference `#9A2E2C` on `#FDEBEC`, holding `#1F6C9F` on `#E1F3FE`, notes `#6F6E6A` on `#F3F2EF`. The neutral window title bars go white with dark ink (the macOS-like faux chrome of the minimalist skill); the taskbar stays the deep slate. Every other window of the PC takes the same body, surfaces and hairlines (§6).
- **Type.** The culture's font (no new font: every culture's runtime font must still fit Arabic and CJK). Hierarchy by size and weight only: 44 u name, 30 u step title, 26 u document names and buttons, 24 u captions; sentence case everywhere, no letter-spacing tricks; numbers keep the font.
- **Buttons.** Primary: filled `PrimaryAction`, 52 u. Quiet: white with a 1 u hairline. Text link: underlined muted text (Back). Accept and Deny keep their roles (green and red with white ink) as large plates with a second line of explanation.
- **Motion.** None beyond the existing (the line is drawn at once; no tweening in this pass).

## 6. The rest of the PC

Desktop, Mail, Orders, Portals, Internet, Notes, Settings and the Citizen Account keep their flows. They take the new look through the shared roles: the window body, the white title bars (neutral), `Sidebar` surfaces now white with a hairline, buttons with hairlines. No layout changes there.

## 7. Keys

| Keys | Now |
|---|---|
| Ctrl+K, Ctrl+F | open the search drawer (its field focused) |
| Ctrl+1…5 | go to step 1…5 |
| Ctrl+Tab, Ctrl+Shift+Tab | next and previous step |
| F6 | the other side becomes the target |
| Ctrl+\ | two panes or one (as before) |
| Ctrl+B | hide or show the findings column (the panes take its width) |
| Alt+← → | the target pane's history (as before) |
| Esc | the Escape chain: the menus, the drawer's text then the drawer, a field, then a held value, then the frame |
| Tab, Shift+Tab | the regions: search (drawer), its hits, the steps, the shelf, the target pane's values, the other pane's values, the findings, the status line's Cancel, Accept and Deny (at the decision) |
| Space / Enter / Ctrl+C / Ctrl+P | pick, open, copy, pin (as before) |
| Ctrl+Shift+S | show or hide the step hints (the step's sentence on the status line) |

Retired: Ctrl+Shift+S's checklist (it now toggles the hints), Ctrl+1…6 (sources; now steps), Ctrl+Shift+PgUp/PgDn (the navigator's order), the navigator's drag and Move up/down menu, Pin and Open beside in the pane headers (Ctrl+P and the row menu pin; Ctrl+\ splits).

## 8. What stays exactly as it was

The Domain compare and evidence model (`ComparePick`, `ComparePair`, `CompareEvidence`, `DiscrepancyLog.Prove`/`Add`, `PairCompared`, `PicksChanged`, `EvidencePicks`, `PickKeys`), the Deviation Report form and its entries, the scoring rules (the unproven denial), the documents' renderer (FormView, the scanned copy: track B's), the views (Documents, Records, Reference, Transcript, Report, Rules), smart links, search's index and matching, pins' and recents' scopes, the panes' histories, zoom, copy, the scan toast and arrival rules, the desk's compare strip and the stamp tray.

## 9. Decisions taken for Saleh (each reversible)

1. **Five steps, not four:** Books between Records and Rules (the game's tells are proven against the books; the prototype had no books step). Reverse: drop the Books stage from `CaseGuide.Stages`.
2. **No claim in the header** (his Q5 stands over the prototype's claim line): the header shows the face, the name and the counters.
3. **A step's tick is earned** by the day's checklist items that belong to it (not by visiting it); the checklist itself no longer shows as a list.
4. **Deny on the PC needs a logged difference or broken rule** (the prototype); the desk's stamp tray stays ungated so a moral choice can still deny anyone (lesson 7: same verbs). Reverse: drop the gate in `InvestigationUIController.RefreshDecision`.
5. **Findings log matches too** (the prototype's "what you compare becomes your evidence"); only differences and broken rules count for Deny.
6. **The Deviation Report leaves the shelf**; "Open the report" in the findings column opens it.
7. **Rule verdicts read the case** (e.g. a missing waiver breaks the paper-set rule whichever paper-set value it is held against): a missing paper is not a value one can click.
8. **Pins and Recent live in the search drawer**; the pane headers carry no buttons.
9. **Neutral title bars are white**; the cultures keep their deep title bars (their identity).
10. **No new font** (no download; every culture's font must fit).
11. **The shelf wraps to three rows** in the neutral font at 1440 u (two papers, the transcript, three agency documents, six books). Kept over a scrolling strip or a Books menu: every document stays one click away and visible. Reverse: fold the books into one chip that opens the Reference view.
12. **The step's sentence lives on the status line** while nothing is held (the prototype's lead line and status line merged: one line to read, not two); Ctrl+Shift+S or Settings hides it.
13. **A new case puts its first pair up without opening the app** (nothing steals the view); the scan toast still opens a closed app, as before.
14. **The scan toast sits over the foot** for its 4 s (above the windows, as before); it can cover the progress line meanwhile.

## 10. Verification

compile_check 0 errors · the offline runner 0 failures · EditMode (TimeDeskEditMode) · Generate World twice (the second run changes nothing) and the validator · Build Office UI twice (equal by semantic dump) with the readability check · the audit play days 1-3 driving the new app · the tour at 1080p and 720p in the neutral and one Arabic or Japanese theme: each step, a match line, a difference line, the drawer, the decision (`SCRATCH/wave5/A/tour`).
