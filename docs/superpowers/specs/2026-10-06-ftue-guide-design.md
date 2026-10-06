# The FTUE and the daily guide: design

*2026-10-06 · `feat/ftue-guide` · decided and built by Claude for Saleh's review (no questions asked: every choice below is the default, his to overturn) · the code wins over this text; the behaviour contract is `docs/FEATURES.md` ("The desk's guide")*

Saleh, verbatim: "we need to create an FTUE and a help guide that gets expanded every day like Papers, Please that explains the new rule, not just have it in a document, at least for the first week until we teach the player to keep checking new rules."

## 1. What the player sees

| Day | At the shift's start | During the shift | The rulebook's GUIDE tab |
|---|---|---|---|
| 1 | The FTUE's first step ("TUTORIAL 1/8": click the AVAILABLE sign) with an arrow at the sign | One step at a time, each done by doing it; then "TUTORIAL DONE" and a closing line | BASICS + the Passport, Today's destination, Valid Until (NEW) |
| 2-7 | The rulebook opens itself on the day's new page in the reading view; the prompt "NEW TODAY" names it and points where to check it (Got it) | The first traveller who carries it gets the page's one-step practice ("PRACTICE") | + the day's page(s), NEW |
| 8-15 | Nothing | Nothing | + the day's page(s), NEW; a red NEW badge over the GUIDE tab until opened |

The FTUE's eight steps (`world_source.json` guide.ftue): call a traveller (Call), drag the Passport onto the desk (OnDesk), inspect with SPACE or the magnifier (Inspect), the Passport's Destination against today's rule (Compare: Destination), its Valid Until against the calendar (Compare: Expiry), the stamps out with TAB or the grey tab (StampsOut), the stamp on the ENTRY VISA box (Stamp), the stamped Passport handed back on the counter (HandBack).

The practices (guide.pages[].practice): day 2 the Entry Ticket's Citizen ID against the Passport's (Compare: CitizenId); day 3 a Destination against today's rule, the board showing the same two routes (Compare: Destination); day 4 a seal against the Seal Register on the PC (Compare: Seal); day 5 a scan (Scan); day 6 a class against today's class rule (Compare: AccountStatus or TransponderClass); day 7 the waiver's signature against today's waiver rule (Compare: Signature or WaiverNo).

## 2. The parts

- **Domain** (`Assets/Scripts/Domain/Guide.cs`, tested: `GuideTests`): `GuideContent` (the words and steps, with `Problems` over the ramp), `GuidePage`, `GuideStep` (`Matches` a `GuideEvent`: the action, the finding's detail, the paper's form), `GuideTargets` (where an arrow may point), `GuideText.Keys` ({inspect}, {stamps}, {pc}, {back} become `ControlRules`' keys), `GuideState` (the run's progress, saved: `WorldState.guide`, additive) and `Guide` (the pages on a day, by `Introductions.FirstDay`; the new ones; the day's `GuideMoment`; the practice; the badge; the FTUE's current step, `Record`, `Skip`, `Replay`, `CloseShift`).
- **Content**: `world_source.json` "guide" through Generate World into `ContentLibrarySO.Guide` (`WorldContentGenerator.Guide.cs`; the validator's `CheckGuide`); content sheets `guide`, `guideBasics`, `guidePages`, `guideFtue` (`ContentSheetMap.Guide`); the narrative workbook's "The desk's guide" block (`NarrativeWorkbook.AddGuide`); ui strings `guide.*`, `desk.guide.*`, `desk.rulebook.tabGuide`, `settings.replayTutorial`, `keys.replayTutorial`.
- **Office**: `GuideDirector` (Office/Guide) hears the desk (`DeskController.PaperExamined`, `ScanFinished`), inspect mode (`DeskInspect.Changed`), the stamp bar (`DeskStampTray.Changed`: `BarOut`, `HasVerdict`), the workbench (`MatchBoard.Logged`: the finding's `Category`) and GameManager (`BeginShift` after the briefing, `CaseShown`, `CaseDecided`, `EndShift` before the save); `GuidePrompt` draws the plate and the arrow on the office overlay; `DeskRulebook` gained the GUIDE page (`SetGuide`, `OpenGuide`, `SetGuideBadge`, `GuideRead`). Build Office UI builds all of it (`OfficeSceneUIBuilder.Guide.cs`, the booklet's third tab in `BuildRulebook`) and wires the two "Replay the desk tutorial" buttons (Settings' Keyboard row, the F1 card) to `GuideDirector.Replay`.

## 3. Decisions made on Saleh's behalf

1. **The GUIDE is the rulebook's third tab**, not a tab per page: by day 15 there are 18 pages and the booklet is 26 cm wide. Inside it BASICS comes first, then the pages by the day they arrive, turned with < PREV / NEXT > and numbered ("2 / 6"); today's are marked NEW.
2. **One page per introduced paper, tool or rule, for all 15 days** (18 pages: day 1 three, day 5 two, every other day one), each keyed by its `Introductions` key, so a page appears the day the ramp introduces its feature and the guide can never disagree with the ramp. Generate World refuses a day after the first with no page.
3. **The FTUE completes steps out of order**: a later step done early stays done; the prompt always asks the first open one (the player is never told to redo what they did). It ends with the shift (`Guide.CloseShift`), so it never follows the player into day 2; an older save reaching day 2+ with no guide progress never sees it.
4. **Never blocking**: only the prompt's two buttons take clicks. On the PC the prompt shows only when it points at the PC (the seal practice), so it never covers the desktop. An arrow whose target is off the screen hides rather than pinning to the edge (it pointed into the plate). A paper target not on the desk yet points at the rulebook's tabs (its PAPERS tab lists the papers to ask for).
5. **The new rule's moment uses the reading view**: the rulebook lies beside the mat, below the normal view, so the moment tilts in to show the open page; Got it returns. Calling a traveller also ends it. The moment's line is the page's title and "check" only (the page itself shows the rest).
6. **The practice** shows on the day's first traveller who carries the page's feature (its paper for a paper's page; anyone for a tool or a rule), is done by doing it, and is dismissed for good with Got it; one not done waits for the next carrier the same day. Only the day's first new page with a practice gets one (day 5 has two pages; the scanner's is practised).
7. **The badge** marks a page added today until it is opened, on every day (on days 1-7 the moment opens it at once); from day 8 it is all there is.
8. **The last guided day is content** (`guide.guidedThroughDay` 7), not a ScriptableObject knob: it sits with the pages in the content sheet Saleh edits (design rule 4).
9. **Replay** is in Settings (Keyboard row, beside Show shortcuts) and on the F1 card (under the list); it closes the PC and runs the steps from 1 on any day (a traveller already at the desk counts as called); its Skip ends it again.
10. **The audit play** (`tools/audit/unity/_TimeDeskAuditPlay.cs.txt`) checks the FTUE starts at step 1 and the call completes it, then skips it (its own clicks need not follow the steps: deterministic), and on each day checks the GUIDE's sheets, the moment (then Got it) and that the practice showed.
11. **The stamps track** (draggable 3D stamps, the counter strip, the hand-back guard, the PC app until day 5) is untouched: the guide only listens to `DeskStampTray.Changed` and GameManager's decision.

## 4. Open points for Saleh

- **The bulletins of days 9-14 do not match the ramp**: day 9's says "debt standing" (the day introduces dress), day 10's the recall (debt standing), day 11's "no 2150 goods" (cut; the day introduces the recall), day 14's "paper dates" (the return home). The guide's pages follow the ramp (`Introductions`); the bulletins are content to fix.
- The other languages' string tables have no guide strings yet (English shows).
- Sound: no cue on a step done (no sound asset).

## 5. Verification

Reports in `SCRATCH/ftue`: compile 0 errors; the offline runner and EditMode (TimeDeskEditMode) 0 failures (`GuideTests`); Generate World twice (the second run changes nothing) and the validator; Build Office UI idempotent by semantic dump (the office scene rebuilt and committed: the builder changed); the audit play days 1-3; the FTUE probe (`probe/probe_report.txt`, screenshots at 1920x1080 and 1280x720: each FTUE step, the done line, the GUIDE's BASICS and a page, the replay, the day 2 and day 3 moments and practices).
