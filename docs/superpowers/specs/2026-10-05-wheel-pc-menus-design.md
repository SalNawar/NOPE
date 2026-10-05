# The minimal wheel, the PC's menu bar and the date in the taskbar: design

*2026-10-05 · the desk-first redesign, track C (`feat/wheel-pc-menus`) · Saleh's items 7 and 8 and his note on the date · decided and built by Claude for Saleh's review (he was asleep: no questions) · the code wins over this text*

Saleh, verbatim:

> (7) "the dialog wheel no longer shows all of these dialog options; you must find an issue to unlock a question; even asking for a document must be highlighted by flagging missing on the app"
>
> (8) "the real estate of viewing documents is too small on the PC; we need menus at the top: you press one and it has a drop-down, to reduce clutter"
>
> "also calendar should be in pc too" · "date I mean": today's date (14 MAR 2150) beside the clock in the taskbar, comparable for expiry and ticket dates.

The skills Saleh chose for the PC (`minimalist-ui`, `redesign-existing-projects`) are applied within uGUI as in the workbench spec §5: white surfaces, hairlines, colour only for state, sentence case, slim chrome so the documents get the room.

## 1. The wheel starts minimal and unlocks (item 7)

| # | Decision | Why |
|---|---|---|
| M1 | A dialog choice may be **locked** (`DialogChoice.Unlock`, a key): the runner offers it only once that key was given (`DialogRunner.Unlock`); a sub-menu entry that hides when spent (`HideWhenSpent`) counts only unlocked choices. Keys are `InterviewUnlocks.Missing(requestId)` and `InterviewUnlocks.About(category)`, the same for every traveller. | One mechanism in the pure runner, tested headless; the graph is still built once per traveller, so the menus stay "the same for everyone" (rule 3). |
| M2 | From the start the hub offers **small talk** (moved from the ask menu to the hub: the greeting, "How is life back home?", the traveller's small talk and slip), the two spoken requests (Step closer, Speak up), **Look >** (their face from day 1, their clothes from the dress rule's day) and the day's narrative dialogs. Nothing else. | The brief named "greeting / papers, please / the hand-back". Papers arrive when the traveller steps up (the desk hands the arrival papers over in `DeskController.BeginCase`, Track B's code, untouched), so a "Papers, please" entry would do nothing; no hand-back verb exists (the stamp tray decides and the papers leave with the traveller). The spoken requests stay: they are no questions and no document requests, and their 40 authored voice rows are personality flavour. **Reverse:** move `act:` entries behind a key in `InterviewScript.Build`. |
| M3 | **Every document request is locked until its paper is flagged missing** (`InterviewUnlocks.Missing(request.Id)`); the entry reads "Hand me your Entry Ticket" (`interview.requestLabel`); with two or more requests on the day's menu they sit in "Ask for papers >" (`papersLabel`), shown only while a flagged request is left. The waiver pad unlocks with the waiver's flag (it is the cure for a missing waiver). | Saleh's words. The papers menu keeps its place so the capacity rules (`DialogChecks.MenuProblems`) are unchanged. |
| M4 | **Every trip question is locked until the clerk logs a finding about its detail** (`InterviewUnlocks.About(category)`): any logged finding (a match or a difference, a rule's or the calendar's verdict, on the PC or through the desk's compare) whose detail is that category (`Finding.Category`, the statement's category). "Ask >" (was "Ask about the trip >") shows only while an unlocked question is left. The confrontation (a proved difference adds "VISA CLASS: Premium?") now joins the same ask menu instead of its own "Ask about a difference >" menu (`interview.confront.label` removed): the hub's worst case had grown to 10 with small talk on it, and one ask entry for every question is also the leaner wheel. The hub's worst case is 9 again (the papers menu, 2 spoken requests, small talk, ask, look, 2 dialogs, a premade's dialog). | "You must find an issue to unlock a question." A match unlocks too, because a liar's tell is in the answer: the clerk has to have looked at the detail first, not to have proved it already. **Reverse:** unlock only on `FindingRules.IsDifference` in `InvestigationUIController.Confront`. |
| M5 | Asked for and **not carried**, a request logs a finding, "Entry Ticket missing" · "Asked for, not carried" (`FindingKind.PaperMissing`, appended; red; `MatchBoard.LogMissing`), which is a difference (Deny can cite it) and a directive fault's evidence (`FindingRules.IsDirectiveEvidence`, so a denial on it earns no unproven-denial citation). An honest "I don't need one" (a Premium tourist and the waiver) logs it too: whether the rule asks it of this traveller is the clerk's call (hold the rule against the class). | The flag has to lead somewhere the clerk can argue from; one evidence model (the findings). |

## 2. Flagging a paper missing

`MissingPapers` (Domain, tested: `MissingPapersTests`), one per traveller, built from the day's papers menu (`InterviewDay.AskableForms`: only papers the ramp has introduced) and the traveller's `FormRequests.Build`. Its open list is **the same for every traveller** (every request of the menu not handed over yet), so it never tells the clerk what the traveller holds back; a paper the traveller carries outside the menu (not introduced) is never offered.

- **On the PC:** the Papers menu lists, under "Not handed over", each open request with "Flag missing"; a click flags it (the row reads "Flagged: ask for it", later "Not carried"). Papers not handed over no longer appear as dimmed documents (that leaked what the traveller carries).
- **On the desk:** Track B owns the desk. The façade offers it the same list and the same verb: `InvestigationUIController.MissingPapers` (with `Papers`, `MissingPapers.Open(papers)`), `FlagMissing(requestId)` and the `MissingChanged` event, so the desk's rulebook line that requires a paper can carry "Entry ticket missing". Not wired by track C.

## 3. The menu bar (item 8)

The header (the face, the name, the counters, the steps' pills) and the shelf (a row of chips) are gone; one **56-unit menu bar** replaces them (`MenuBarView`, built by Build Office UI), and the work area reaches the window's foot (the foot's 60 units are gone too: the steps run headless since track A). The two document panes gained 84 + 56 + 60 − 56 = **144 units of height** at every size (at 1440 × 1020: the panes' area went from about 720 to 864 units).

| Part | Now |
|---|---|
| Titles | Papers, Records, Rules, Books, Calendar (`AppMenu`): a clear button with a Body-size label (no chevron: it overlapped the word), the side its open document is on ("L", "R") and an unread dot; a menu with nothing introduced today shows no title (Records from day 5, Books from the Seal Register's day; the gating is track A's `Introductions`, read where the rows are made). |
| Drop-down | one at a time, under its title, a white list framed by a strong hairline, 44-unit rows; the open title sits on a warm plate with a bar of the primary colour (the step pills' look). Clicking the title again, a press elsewhere, Escape or a choice closes it. |
| Papers | the papers handed over (dimmed until scanned), the transcript, then "Not handed over" and the papers to flag (§2). |
| Records, Rules, Books | Citizen records; today's rules; each reference book. |
| Calendar | the Calendar view, and "Today: 14 Mar 2150 · Hold to compare" (holds today on the workbench). |
| Right side | who is at the desk on one line (the name in bold, then the counters, muted) and Search (Ctrl K), moved from the status line, which now spans the main column. |

Decisions: a one-entry menu (Records, Rules) still drops down (one pattern, and room for what later days add); the books are rows of the Books menu, not a scrolling strip; the traveller's face left the PC (the person is at the desk; Look > Their face holds them for a photo).

## 4. The date in the taskbar

The tray (widened from 760 to 1000 units so a culture's long currency word, the stability and the date never overlap) reads Credits · Stability · **14 MAR 2150 · Day 1** · 09:00 (`tray.date`; the agency's calendar, `AgencyCalendar.Today`, in capitals as the desk calendar prints it). The date is a clear button (underlined, hover hint "Compare today's date"): a click opens the Investigation app and holds today on the workbench (`InvestigationApp.HoldToday` → `MatchBoard.PickToday`), so the next click on an expiry or a ticket's date judges it ("Expired", "Not today", logged). With no readable first date the tray falls back to "Day 1".

## 5. Clean-up asked by track A

- The Deviation Report's search entries are gone (`IndexEntries.Deviation` removed; the report's form stays for the code that still documents deviations).
- The steps' pills and foot are no longer built; `GuideBar` keeps only the headless logic (a new case's first pair, Ctrl+1…5, the decision view) and the checklist engine; the `guide.*` strings are removed from the content.
- Left for later (not track C's to decide alone): the Settings pair and Ctrl+Shift+S still toggle the step hints, which show nothing now.
- **The PC's decision step stays, headless** (decided after track B landed its physical stamps): no menu, button or foot reaches it; only Ctrl+5 / Ctrl+Tab do, and the audit play still exercises its evidence gate on even slots. The verdict is the stamp on the passport. Removing it (`DecisionView`, the façade's Accept/Deny wiring, `GuideStage.Decision`, the keys' decision region, the audit play's PC path: 13 files) is the recommended follow-up so the PC only investigates; it was not done at the end of this night to keep the merge safe.

## 6. Verification

compile_check 0 errors · the offline runner 0 failures · EditMode (TimeDeskEditMode) · Generate World twice (the second run changes nothing) and the validator · Build Office UI twice (equal by semantic dump) · the audit play days 1-3 · screenshots at 1080p and 720p, neutral and Arabic, in `SCRATCH/deskfirst/C/shots`: the minimal wheel, a finding unlocking a question, a paper flagged missing unlocking "Hand me your …", the menu bar with a drop-down open, two documents at full height, the taskbar's date.
