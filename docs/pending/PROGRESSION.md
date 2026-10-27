# PROGRESSION: three weeks, a new feature every two days

*2026-10-28. The run is days 1 to 21. Branch `feat/progression-21`, from `integrate/cam` (main 6c5536e plus CAM). Spec: `E:\unity\NOPE-tools\briefs\PROGRESSION_21.md` (the night session's research spec; its section 5 is the table below). Status: data, code, tests, scenarios and docs are in; the Unity steps of section 5 are not run yet.*

## 1. The decision

Saleh, 2026-10-28, verbatim: **"I also want to extend progression to 3 weeks instead of two with a new feature every 2 to 3 days."**

The rulings applied (the coordinator, 2026-10-28, on the spec):

1. The spec's table is the source of truth: features on days 1, 3, 5, 7, 9, 11, 13, 15, 17, 19 and 21; the ten even days are mastery days. The day map, old to new: 1, 3, 3, 7, 5, 7, 9, 11, 13, 15, 17, 18, 21, 19, 21. The earlier ruling "week 1 keeps one new thing a day" is withdrawn.
2. The endings move to day 21 (`Ending_WorldReport.asset`, `threshold` 21).
3. Nothing is invented: the same 15 features, re-paced. The spec's three merges: the Entry Ticket with the Departure Board (day 3), the seals with the Transponder Card (day 7), the range limit with the Return Order (day 21). The ban is an event on a mastery day (20; the spec's 18, see the divergences).
4. The Currency and Device questions stay cut: `fromDay` 22, past the run.

## 2. The 21 days

F = feature day (a new paper, check or tool; its guide page; a bulletin that opens "NEW:"). M = mastery day (no feature; a story beat, a famous traveller, a new lie for a tool the player holds, a harder mix or an event; a notice; no guide page). Two famous travellers are forced a day from day 6, every one of the 32 once.

| Day | Week | F/M | Queue | Hours | What the day brings |
|---|---|---|---|---|---|
| 1 | 1 | F | 8 | 09:00-17:00 | As before: the Passport, the open destination, the dates, the calendar, the rulebook, Mail, Notes, Settings, the FTUE. |
| 2 | 1 | M | 10 | standard | A harder mix: Medieval, China and Britain. Violations start (0.05). A notice. |
| 3 | 1 | F | 12 | standard | The Entry Ticket and the Departure Board (two routes, portal 02). Internet, Portals, Orders. Lie DoctoredIdentity. Guided (two pages). |
| 4 | 1 | M | 12 | standard | Industrial joins. The first returning denied traveller (`returns` 1 from day 3). |
| 5 | 1 | F | 13 | standard | The scanner and the records, Investigation and the Citizen Account, the displaced and the Travel Permit, `q_born`. Guided (two pages). |
| 6 | 1 | M | 14 | standard | The famous travellers arrive: Caesar and Ramesses forced, the rest pooled at 0.05. |
| 7 | 1 | F | 14 | standard | The Transponder Card and travel class; the issuing seals and the Seal Register. Lies PoorPosingAsRich, ForgedSeal. Guided (two pages). Kurosawa, Saladin. |
| 8 | 2 | M | 14 | 13:00-21:00 | Week 2 opens: the hours (`hours_extended`, `drive_begins` print this morning). Industrial 2. Cleopatra, Beethoven. |
| 9 | 2 | F | 14 | 13:00-21:00 | The Stranding Waiver and the UV torch. Lie FakeWaiver. The last guided day (`guidedThroughDay` 9). Nobunaga, Alexander. |
| 10 | 2 | M | 16 | 13:00-21:00 | Pell 1 (WaiverUnsigned). Lie WashedPaper. `desk4_closes`, `rift_storm` print. Displaced 4. Darwin, Sun Tzu. |
| 11 | 2 | F | 16 | 13:00-21:00 | Debt Relief labourers and the Work Permit; the Translation Lens, the culture day (`translation.fromDay` 11). Lie ForgedContract. Break-ins from tonight (`breakInFromDay` 11, unchanged). Einstein, Marx. |
| 12 | 2 | M | 16 | 13:00-21:00 | Ines. Lie ReprintedPaper. Luther, Newton. |
| 13 | 2 | F | 16 | 13:00-21:00 | Dress for the destination, the Look menu, the Costume Guide. Costume errors 0.05, violations 0.06. Michelangelo, Turing. |
| 14 | 2 | M | 17 | 13:00-21:00 | Gutenberg (`dlg_gutenberg`). Confucius. |
| 15 | 3 | F | 17 | 16:00-24:00 | The debt standing (`pc:standing`, Debt). Lie DebtorPosingAsTourist. Week 3 opens: the night hours (`night_shifts` prints). Industrial 2, Modern 2. Aristotle, Hokusai. |
| 16 | 3 | M | 17 | 16:00-24:00 | Hollis (Frozen). Labourers 4. Genghis, Qin Shi Huang. |
| 17 | 3 | F | 18 | 16:00-24:00 | The Driftbox 3 recall and the chip reader. `driftbox_recall` prints. Violations 0.08. Kulthum, Leonardo. |
| 18 | 3 | M | 18 | 16:00-24:00 | Pell 2 (Recalled) and Rook (the bribe), on the board's routes (Periclean Athens, Victorian Britain). Lie ClonedChip. `audit_week` prints. Displaced 3. Elizabeth, Khwarizmi. |
| 19 | 3 | F | 18 | 16:00-24:00 | The Intake Declaration and the return home; Tongues, Capitals, Rulers; the Language, Capital and Ruler questions (`fromDay` 19). Lies FalseOrigin, FakeDisplaced. The Answer channel. Shakespeare, Marco Polo. |
| 20 | 3 | M | 18 | 16:00-24:00 | The auditor (`auditor_found` after Rook's bribe), bound for Republican Rome on portal 01; "Ada Lovelace", "Socrates". The ban: `Rule_NoModernGermany`, Weimar Berlin CLOSED on portal 02. Galileo. |
| 21 | 3 | F | 18 | 16:00-24:00 | The Return Order and the Economy range limit. `range_limit`, `audit_rook`, `drive_last_day` print. Pell 3. Ancient 2. Archimedes, Hammurabi. The run ends at this night. |

Divergences from the spec, each with its reason:

- Violations 0.08 from day 17 (the spec's row 17), not from 18 (its knob table 5.1): the recall is a closure with a guaranteed violator, so the harder rate lands with it, as the range limit did before.
- Day 21's era weights are the old day 15's (Ancient 2, the rest 1), so the range limit's guaranteed violator finds Ancient routes; the spec's "Industrial 2, Modern 2" stops at day 20.
- Day 14 carries Gutenberg and Confucius (the spec's "two a day" with Gutenberg as a dialog beat); day 20 carries Galileo and Socrates.
- The day plans' `premades` pools are "every famous minus the day's forced ones" (FEATURES' rule); before, the pools were hand-cut lists of 12 to 14.
- The ban stands on day 20, not the spec's 18. Generate World refuses an authored fault on a traveller whose place the day closes (`Premades.ForcedProblems`: the closure is the fault already). Day 18's two beats are bound for Periclean Athens (Pell) and Victorian Britain (Rook), so both board routes are theirs and no route is left for the ban; day 18 takes the old day 11's routes. Day 20's board is Republican Rome (the auditor's place, portal 01) and Weimar Berlin on portal 02, CLOSED by the ban; the auditors' day keeps "every check, every time". The rhythm holds: a closure for every kind is no feature (`DayPacing.RuleKey`).
- The guide's registry (`ContentLibrarySO.Ramp`, Generate World's `CheckGuide`) leaves the open-destination rules out. The spec asked only for the `Guide.cs` rule; without this the day's own `Rule_Open_Dnn` key made every mastery day "introduce" a check. `DayPacing` already counted them as no check.

## 3. What changed

Data (`content(progression)`): `Assets/Data/World/world_source.json`: `days[]` 21 plans (the generator script of the session, from the 15 old records: queues, hours, bulletins, papers, introduces, lies, kinds, eras, rules, forced, pools, portals); `rules[]` `Rule_Open_D16` to `D21` (each a copy of the day whose routes it reuses: 13, 11, 11, 14, 14 with Weimar Berlin on portal 02, 15); `questions[]` fromDay 19 (`q_language`, `q_capital`, `q_ruler`) and 22 (`q_currency`, `q_device`); `translation.fromDay` 11 and the two lens level descriptions; `guide.guidedThroughDay` 9; `pc.steps` fromDay by the day map (the displaced "facts" to 19); `history.rules` thresholds (section 5.2 of the spec) and `audit_rook`'s "on day 18".

Code: `Assets/Scripts/Domain/Guide.cs` (`GuideContent.NeedsPage`: a lie or a question needs no page, a mastery day passes), `Assets/Scripts/ContentLibrarySO.cs` and `Assets/Editor/WorldContentGenerator.Guide.cs` (the registry without the open destinations), `Assets/Data/Endings/Ending_WorldReport.asset` (21), `Assets/Scripts/DevTools/DebugPanelController.cs` (the fallback 21), `Assets/Editor/BalanceSimulation.cs` (`Days` read from the library's `LastDay` at Run and Observe; `DefaultDays` 21), `Assets/Editor/QA/Checks/RunSafeChecks.cs` (landings d18, d21), `Assets/Editor/QA/QaJobs.cs` (budgets 60 s and 130 s), comment-only pins in `HallSlots.cs`, `DayPlans.cs`, `BalanceSimSettingsSO.cs`.

Tests: new `Assets/Tests/EditMode/ProgressionRhythmTests.cs` (the 21 plans, the ending's threshold, the feature days 1, 3, ..., 21, never consecutive, no gap over 3, mastery days a lie at most with a notice, the queues and the hours by week, every day gate on a feature day or past the run); re-pinned `GuideTests` (the quiet-day case), `DayPlansTests`, `StepSetsTests`, `NarrativeWorkbookTests` (21 rows, Pell on day 10), `EndingRulesTests`, `AgencyCalendarTests` (day 21 is 3 Apr 2150), `CheatRoutingTests` (`LastDay` 21), `CheatLedgerTests` (text), `IntroductionsTests`, `TranslationLensTests` (the lens on 11), `ShiftHoursTests`, `HallSlotsTests`, `OrdersTests`, `SeedsTests`.

QA scenarios (`tools/qa/scenarios/`): `day_sequence.json` 21 blocks, 1000 cr added on day 1 (one auto-decided traveller a day earns less than the harsh economy's nights cost, and the bankrupt ending ended the first run at night 7) and day 21's night played to the title's ending panel; `day_jumps.json` adds Jump d18 and d21; `tools_torch` day 9, `tools_lens` day 11, `tools_clicks` and `tools_microprint` day 18; `textlang_takeover` and `textlang_takeover_history` days 10, 11, 12 (day 10 has no NEW TODAY prompt step); `cam_views`, `cam_chrome`, `cam_desk`, `cam_gc`, `stamp_aim`, `perf_day12`, `desk_plan_homes`, `desk_plan_decor` keep day 12 and add the Unlock everything cheat after Give all items (the chip reader comes on day 17 now; the `d12` leg names and `cam.play.day12` stay).

Docs: `FEATURES.md` (the ramp, the day plans, the hours, the queue, the endings, the lens, the guide, the balance, the rules; a new "Progression: three weeks" line), `CHEATS.md`, `pending/RUN-SAFE.md`, `pending/QA-RIG.md`, `pending/DESK-PLAN.md`, `pending/TOOLS.md`, `pending/TEXT-LANG.md`, `pending/LIGHT.md`, `ART_ASSET_LIST.md`, `CHARACTER_ART_BRIEF.md`, `CONTENT_SHEETS.md`, `narrative/README.md`, `ArtDeliverables/TimeDesk/HallSlots/HALL_SLOTS_ART_REQUEST.md`.

Not changed: `breakInFromDay` 11 (mid second week, as Saleh asked); `GameConfig_Default` and the prices (section 5); `RunRoutes`, `DevCheats`, `RunManager`, the save format (a 15-day save continues into the new plans; a save that ended shows its ending); the memory note `design-1019a-decisions.md` ("days 1-15, always"), which is the coordinator's to update.

## 4. Offline checks (all green at the head)

- `compile_check_p5.py`: 0 errors (Assembly-CSharp-Editor and TimeDeskEditMode).
- The house runner (`E:\unity\NOPE-tools\runner`): passed 3946, failed 0 (3940 before; the rhythm tests and the quiet-day case added).
- `tools/qa/editor_runner` with `QA_PROJECT`: passed 104, failed 0, 55 NeedsEditor skipped.
- `python -m unittest` over `tools/qa` (35) and `tools/hall` (11): OK.
- An offline pass over the source mirroring Generate World's day checks (forced slots within the queue, every famous forced once, each page's word in its day's bulletin, each lie fitting a kind and after its machine, every route in the day's world, portal 01 open, one new paper and one new check a day at most): no problems.

## 5. What Unity must verify

1. `Tools > TimeDesk > Generate World`: writes `DayPlan_Inv_Day16` to `Day21`, `Rule_Open_D16` to `D21`, the Days sheet; 0 errors, no unplanned-tail warning. Then `Validate Content Library`: 0 issues.
2. The EditMode suite in the editor (the NeedsEditor tests: `CheatLedgerTests.AJumpToTheLastDay` lands on 21 with no ending).
3. `QaJobs.DaySequence` (days 1 to 21; day 21's night shows the ending) and `QaJobs.DayJumps` (d15, d18, d21 land at their briefings; `runsafe.play.dayJumps` green).
4. The guide: days 1 to 3 as before; days 9, 10 and 11: the NEW TODAY moment opens on 9 (guided) and the badge on 11, nothing on 10.
5. The measured day 12 scenarios with Unlock everything: every machine seen (cam_desk, desk_plan_homes), the stills re-based.
6. The Statement (`AccountWindow`, one row a day): 21 rows must fit the page at 720p.
7. `Tools > TimeDesk > Balance > Run 50-Run Simulation` (it now plays 21 days): see section 6.

## 6. The open balance finding

A wrong call takes 2.5 % of the current stability (`stabilityLossPerWrong` 5 at 0.5 % a point). Imperfect play (one wrong call a day) ends at 100 x 0.975^15 = 68 over 15 days and at 100 x 0.975^21 = 59 over 21, under the firing line of 60. The harsh economy also carries six more nights of rent (30 a night) against queues of 17 and 18.

Nothing was changed here: the knobs are Saleh's (rule 4 of 2026-09-29) and the simulation is the judge. The spec's recommendations, for the simulation's run: `stabilityLossPerWrong` 5 to 3.5 (imperfect play ends near 70, careless play still fires); `topTierPrice` 150 to about 210 and the late upgrade and Orders prices scaled by 21/15, so Q6 ("buy one or two if you take bribes") holds at day 21. The simulation has no offline entry (`BalanceSimulation.Run` needs the editor's asset database), so it is a Unity step: run it, then retune, then re-run.
