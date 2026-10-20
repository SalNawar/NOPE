# RUN-SAFE: cheats and saves you can trust

Branch `fix/a-run-safe`, cut from `fix/p0-cuts` (5aecb60). Merge #3 of Phase A.

Sources: FIX_PLAN.md §0 and §3 "RUN-SAFE", FIX_PLAN_v2.md "RUN-SAFE: delta" (v2 wins), BUGLIST.md (BUG-25, 27, 28, 35, 56, 65, 68), DECISIONS_1019a.md, GAME_FEEL_BIBLE.md row G3, PHASEB/LEASES.md, docs/pending/CARVE.md and CUTS.md.

**Goal (v1):** a day skip, jump or restart lands at the true start of the day, with the hall matching the clock, nothing auto-playing, and money and state as in a played run. Quitting never loses the run. Nothing is deleted without asking. The cheat overlay blocks input behind it.

**Saleh's words this track builds (quoted):**
- D18, "Cheats: visible, as now". The panel ships visible, on F9 or ~ as today. v1's "behind a key combo" is dropped. The input block stays, because BUG-68a is a bug. Skinning the panel stays a proposal (S3) and is not done here.
- Note 22 (1019a): "Whe i skkip days with cheats I dont go to the begining of the day".
- Standing rule (memory), "Everything diegetic": the save notice is a screen strip, not a desk object. It reports a fault and asks for no action, so it is not an action surface. Framing it as the Bureau's "RECORDS" desk is **proposal (ours)**.

Code-only run: Saleh was on the PC. Every check that needs Unity is listed under RESULT as "deferred: Unity", with its recipe.

## What changed, per BUG part

| Part | What the player or tester gets now | Where |
|---|---|---|
| **27a** day-scoped switches | Every morning clears the day's switches: the natural one after Sleep, and the landing of every skip, jump and restart (`DevToolsState.ResetDay`). That covers the forced hour (the hall shows the clock's hour again), Auto-decide (no new day plays itself), the forced leader (through TEXT-LANG's `CultureThemeService.ResetForNewDay`, P0 stub) and the hall overrides (`HallSlotsDev`, stand-ins included). The run's switches (Unlock everything, forced strandings and fate, a forced personality) and the next generation's (premade next, costume error next, lie next, appearance next) stay until a new or continued run. They are named loudly on the badge: "CHEATS ON: Unlock everything · Lie next SwappedPhoto". Every switch has exactly one scope (`DayScoped`, `RunScoped`, `NextGeneration`; a reflection test and a gate check fail on a new switch with no scope). A cheat that opens a morning or ends the run closes the panel, so the day starts at its briefing. A jump back adopts the pet afresh (same kind, name and coat), never a later day's pet. | DevToolsState.cs, RunManager.cs, DebugPanelController.cs, CheatBadge.cs (new), CheatRoute.cs (new) |
| **28a** cheat routing, restart snapshot | Skip, jump and restart run play's own day steps (`RunRoutes` in Domain, played by `CheatRoute.Play`):<br>• **Skip** closes today's shift as played. The live shift closes on its own ledger (`GameManager.CloseShiftForCheat`: strandings, instalment, dialog outcomes, statement row, Home as the resume point, tomorrow modifiers reset; `DayCycle.EndShift`), followed by play's ending check. Then Home's night on the default care (`DayCycle.HomeNight`: break-in, fixed bill, HOME's `NightRules.DefaultNight`, the night's statement row), Sleep, and the next morning.<br>• **Jump forward** does the same for today. Every day before the target is then **played with its whole queue decided correctly**, as the cheat menu's Auto-decide plays it (`DayCycle.PlayShiftCorrectly`), with its night. So the run earns its pay and pays its rent, and it reaches day 15 as a played run would.<br>• **Jump back** builds a new world on the run's seed and pet, played the same way.<br>• **Restart** restores today's **morning snapshot**. RunManager keeps one every morning, in memory and in `nope_morning.json`, so it survives Continue. Today's decisions, pay, citations, premades met, pet changes and cheat changes are undone.<br>• A route from Home starts its night from the end-of-shift save, so a Home arrival already charged in memory is never charged twice.<br>• Give all items and Law-breaker next restart **with** their change (`RestartShift(reapply)`).<br>• **Rewinds warn first.** A restart, a jump back, an appearance on an earlier day (or today in a running shift), and Unlock everything, Give all items or Law-breaker next in a running shift each show what is lost (`DevCheats.Warning`, `RewindWarnings`) and act only on **Confirm**. Cancel changes nothing. Scripts calling `DevCheats.Run` directly (QA-RIG) count as confirmed. | RunRoutes.cs (new, Domain), CheatRoute.cs (new), DayCycle.cs, RunManager.cs, DevCheats.cs, GameManager.Cheats.cs, LawBreakerCheats.cs, DebugPanelController.cs |
| **28b** End Shift over a citation (the cheat side) | The cheat's End Shift never draws the report over an open citation slip: it waits for Acknowledge, then closes. A run that has ended (fired, bankrupt) goes to its ending, never Home: after the slip's Acknowledge (its own way there), otherwise at once. FLOW's CloseNow (#15) honours the ending in play. | GameManager.Cheats.cs |
| **28c** a cheat grant re-delivers the pending order | Give all items and Unlock (More tab) cancel and refund the item's pending order first, through UI-PC's `OrderBook.CancelPendingFor` (P0 stub). The next delivery never brings it again (`DevCheats.GrantAll`). DESK-PLAN's Deliver skipping owned items is DESK-PLAN's part. | DevCheats.cs |
| **25b** the cheat's Dusk | Time: Dusk sets the hour to `HallDayCycle.DuskHour`, LIGHT's one day cycle (P0 stub 18.5), instead of a hard-coded 18.5 (`DevCheats.HourOf`, `TimeChoice`). | DevCheats.cs, DebugPanelController.cs |
| **56b** the ending check after the night | Sleep runs `DayCycle.Sleep` (`NightOrder` in Domain) in this order:<br>1. The day-boundary check: failures, and the run's last day, which still gets its own night resolve.<br>2. The night's resolve.<br>3. The failures again, on the world the night left.<br>4. Only then does the day turn.<br>So a night story event that pushes stability past the line fires the clerk that night, never at the next morning's first correct stamp. | Domain/RunRoutes.cs (`NightOrder`), DayCycle.cs, RunManager.cs |
| **35a** save logic and Continue | **A save every morning, day 1 included.** New Run saves day 1's morning at once, so a quit on day 1 can be continued.<br>**Nothing in the slot is ever deleted.** New Run sets the old run's files aside as `nope_save.replaced-<time>.json` and so on, never deleting them. A load sets an unreadable save, or one from before the format, aside as `nope_save.unreadable-<time>.json` or `nope_save.old-format-<time>.json`, so no later save writes over it. A run recovered from the backup keeps the damaged save aside. The boot no longer starts a New Run that wiped the slot: with no save to continue, it starts a run in memory and writes nothing.<br>**Saves are flushed** to the disk (`FileStream.Flush(true)`).<br>**Failed saves are visible.** A failed write, a file set aside and a recovered run are reported (`SaveSystem.Problem`, `ProblemChanged`) and shown for 9 s by `SaveNotice`, a strip at the top of every screen. A later good save clears a failed write.<br>**Continue reloads from disk** (`RunManager.ContinueRun`), never the run in memory. The NEW RUN confirm (35b, HOME) asks `SaveSystem.HasSave()`. The mid-shift forfeit warning (35c) is UI-PC's. | SaveSystem.cs, SaveNotice.cs (new), Domain/SaveSafety.cs (new), RunManager.cs, TitleSceneController.cs |
| **68a** the overlay and InputGate | While the panel is open it holds the `InputGate` (Hold or Release by name, replacing the P0 stub). OfficeControls, DeskInspect and DesktopWindowManager therefore take no input, through the checks the P0 carve inserted. CheatBadge's full-screen uGUI blocker takes every click behind the panel, so nothing on the desk, the PC, Home or the Title is clicked through it. D18: the panel stays visible. | Domain/InputGate.cs, DebugPanelController.cs, CheatBadge.cs (new) |
| **65b** IMGUI allocations | The "CHEATS ON" badge moved from IMGUI to uGUI (`CheatBadge`). Its line is rebuilt only when what it shows changes (`DevToolsState.StickyKey`). The panel's component is enabled only while the panel is open, so with the panel closed, cheated run or not, no OnGUI runs. | DebugPanelController.cs, CheatBadge.cs, DevToolsState.cs |

### Files

**Changed (all RUN-SAFE's):**
- `Assets/Scripts/Core/RunManager.cs`
- `Core/SaveSystem.cs`
- `Core/DayCycle.cs`
- `DevTools/DevToolsState.cs`
- `DevTools/DevCheats.cs`
- `DevTools/DebugPanelController.cs`
- `DevTools/LawBreakerCheats.cs`
- `GameManager.Cheats.cs`
- `TitleSceneController.cs`
- `Domain/InputGate.cs`

**New (RUN-SAFE's):**
- `Core/CheatRoute.cs`
- `Core/SaveNotice.cs`
- `DevTools/CheatBadge.cs`
- `Domain/RunRoutes.cs`: `RunStep`, `RunRoutes`, `NightOrder`, `Rewind`, `RewindWarnings`
- `Domain/SaveSafety.cs`: `SaveProblem`, `SaveSafety`
- The tests below

**Not touched:** RunConfigSO.cs, PlayerPrefKeys.cs, HallSlotsDev.cs, FeelCheats.cs, HallSlotsCheats.cs, DebugInspector.cs. Nothing outside RUN-SAFE's list was edited: no scene, asset, string table or other track's file.

**Staged, for after QA-RIG merges** (this branch does not carry QA-RIG's registry):
- `docs/pending/data/RUN-SAFE/RunSafeChecks.cs.txt`: the gate checks. Copy it to `Assets/Editor/QA/Checks/RunSafeChecks.cs` and make its meta.
- `docs/pending/data/RUN-SAFE/overlay_block.json`: the overlay scenario. Copy it to `tools/qa/scenarios/`.
- `docs/pending/data/RUN-SAFE/ui_strings.md`: the four `save.notice.*` strings, for the string tables' owner.

## Seams

**Consumed (all P0 stubs, as planned):**

| Seam | Owner | Used for |
|---|---|---|
| `CultureThemeService.ResetForNewDay` | TEXT-LANG | The culture's day reset in `DevToolsState.ResetDay` |
| `SceneTransit.Load` | FLOW | Every scene load of RunManager |
| `OrderBook.CancelPendingFor` | UI-PC | The grant's cancel and refund |
| `NightRules.DefaultNight` | HOME | Inside `DayCycle.HomeNight` |
| `HallDayCycle.DuskHour` | LIGHT | The cheat's Dusk |

**Provided:**
- `InputGate.Hold`, `Release` and `Blocked`.
- `SaveSystem.HasSave()`: the save-exists query for HOME's NEW RUN confirm.
- `SaveSystem.Problem`, `ProblemDetail` and `ProblemChanged`: any screen may show the save problem in its own skin.
- `SaveSystem.FolderOverride`: tests and tools point the slot at a scratch folder.
- `DayCycle.EndShift`, `HomeNight`, `PlayShiftCorrectly` and `Sleep`.
- `GameManager.CloseShiftForCheat()`.
- `DevCheats.Register(label, run, restartsShift)`: an optional third argument, so existing calls compile unchanged.
- `DevCheats.Warning`, `HourOf`, `TimeChoice`, `TimeNames` and `GrantAll`.
- `RunManager.SkipDay()`, `JumpToDay(int)` (now returns bool; QA-RIG's call still compiles), `RestartShift(reapply)`, `RestartWarning()`, `JumpWarning(day)` and `Mornings`.
- `DevToolsState.ResetDay()`, the scope lists, `StickyLine` and `StickyKey`.

**Removed:**
- `SaveSystem.Delete`: no caller. New Run sets the old run's files aside instead.
- `DevCheats.TimesOfDay`, replaced by `TimeNames` and `HourOf`. Its only readers were RUN-SAFE's, plus a comment in LIGHT's HallDayCycle.cs, which LIGHT may update.

## Notes for other owners (nothing of theirs was edited)

- **FLOW (#15).**
  - `GameManager.HandleDayCompleted` can call `DayCycle.EndShift`. Today it repeats the same four steps: close, statement row, Home phase, modifiers reset. Switching makes play and the cheats share one call.
  - CloseNow honouring a set `endingId` is FLOW's half of 28b.
  - FLOW's new Esc → Settings handler must check `InputGate.Blocked`, as OfficeControls does.
- **INTEGRATION.**
  - `Assets/Editor/BalanceSimulation.cs` is in no track's list. It still checks the day-boundary ending before `AdvanceNight`, and pays Home's night inline. For the balance numbers to match the game's flow again, its night should become `DayCycle.HomeNight` + `DayCycle.Sleep`, the same order as BUG-56b. `AdvanceNight` is unchanged, so it still compiles and runs.
  - The QA harness (`QaPlayHarness`) backs up `SaveSystem.Files`, which now include `nope_morning.json`. Each scripted New Run leaves `nope_save.replaced-*.json` files in `Library/EditorSaves`; the harness may delete those in its own scratch slot.
- **TEXT-LANG / TOOLS.**
  - `TranslationLensCheats` (Lens: Word, Sentence, Object) grants Translation Lens levels. If those grants own Orders items, they should cancel the pending order the same way (`OrderBook.CancelPendingFor`, see `DevCheats.GrantAll`).
  - The forced leader is now day-scoped: cleared each morning by `ResetForNewDay`. The panel says so ("Kept through the next night").
- **Residual input consumers.** These poll the keyboard and are not gated: `DesktopKeyboard`, `TreeScrollRect`, `AppPane`, `SearchResultsView` and `MatchLines` (UI-PC), `SlotMachineView` (HOME), and `DeskView` and `CityView` (CAM). The blocker stops their clicks, but not their keys while the panel is open. Each owner adds `if (InputGate.Blocked) return;` if it matters. The P0 list of three (68b) is gated.
- **Leases (LEASES.md).**
  - `GameManager.Cheats.cs` passes to BELL-SIGN unless RUN-SAFE ships a `CallNextForCheat()` seam. It does not: nothing in this track needed one.
  - `DevCheats.cs` passes to TIMELINE-FAIL and `Core/DayCycle.cs` to DETAIN, after this merge.

## Tests

**Offline (TimeDeskEditMode, the NOPE-tools runner):** passed 3735, failed 0 (baseline 3710: −1 old stub test, +26).
- `InputGateTests` (5, replacing the stub's 1): blocked exactly while held; a hold taken twice is released once; one overlay never frees another's hold; unnamed holds and unheld releases change nothing.
- `CheatRoutingTests` (16):
  - skip from the office and from Home;
  - a jump forward closes today as played and plays the days between; a jump from Home; a jump back; today's jump is a restart;
  - the restart's steps;
  - every one of the 15×15×2 routes ends at the morning, and no night runs before its shift closes;
  - only today is closed as played, every other day is played;
  - the target is clamped;
  - Sleep checks the endings after the night (fires that night), turns the day without an ending, and ends on a boundary ending without turning;
  - the restart's and the jump back's warnings, and which change is a rewind.
- `SaveSafetyTests` (5): set-aside names (reason and time, a second one in the same second), what is set aside, the reason by problem, the worst problem told, a good save clearing only a failed write.

**Editor-side, `Assets/Editor/RunSafeTests/` (Assembly-CSharp-Editor; in Unity's EditMode suite):**
- `DevToolsStateTests` (7). Six run outside Unity too (a scratch variant of the runner: 6 passed, 0 failed):
  - every switch has one scope;
  - a morning keeps the run's and the next generation's switches;
  - the badge's line and its key;
  - the cheat's Dusk is `DuskHour`;
  - the time choice follows the forced hour.
  - One is `NeedsEditor`: a morning clears the day's switches and the hall overrides. Clearing logs, and the log needs the engine.
- `SaveSystemTests` (7, `NeedsEditor`, JsonUtility; on a scratch slot):
  - read back, with a backup kept;
  - an unreadable save is set aside, kept and reported;
  - an old-format save is set aside and never written over;
  - a backup recovery keeps the damaged save;
  - New Run sets every file aside and deletes nothing;
  - a failed write is reported, the last good save stands, and a later good save clears the report;
  - the morning snapshot is kept apart from the slot.
- `CheatLedgerTests` (9, `NeedsEditor`, the run's library). Field-by-field diff of the whole saved world:
  - skip equals the shift's close, Home's night on the default care and Sleep, and rent is charged;
  - a jump to 6 equals day 1 closed and days 2-5 played;
  - **a jump to the last day lands there without an ending** (QA reaches days 8, 12 and 15 by jumps);
  - a jump back to 3 equals a new run played to 3, with the same pet;
  - a restart equals the morning (money, citations, premades, pet, phase, ending);
  - a restart with no snapshot keeps the world and lifts the ending;
  - a skip from Home starts from the end-of-shift save, never charging the arrival twice;
  - the live shift closes once, on its own ledger;
  - a cheat grant cancels the pending order, and it is never delivered again.

TDD: every test was red first (a compile error on the missing API), then green.

## Decisions taken here (ours, not Saleh's)

1. **A skip closes today as played.** The rest of today's queue is not decided for the player, the same as End Shift. **Days a jump passes over are played with every traveller decided correctly** (the whole queue, the Auto-decide cheat's player). Without that, a jump would either charge nothing (BUG-28) or bankrupt a run that never earns, and QA could not reach day 15 (starting money 50, rent 30 a night). Under a harsh economy (D9), even an all-correct run might not survive to day 15. If so, `CheatLedgerTests.AJumpToTheLastDay_LandsThere_AsAPlayedRunWould` fails in Unity. That is a balance finding for CONTENT, not a cheat bug.
2. **A jump forward from a run that has ended is refused,** with a line in the panel. A restart or a jump back still works. The same goes for Skip.
3. **The forced leader is a day switch.** It holds through the next night, then the morning clears it, through TEXT-LANG's reset.
4. **The confirm is a row in the panel itself** (IMGUI): the warning line, Confirm, Cancel. Scripts that call `DevCheats.Run` are not asked.
5. **The badge moved to uGUI and says more:** the run's and the next generation's switches, by name.
6. **Set-aside files are kept forever.** They are small, and New Run is rare. A cleanup belongs in Settings later, behind a confirm.
7. **The save notice is a plain strip** with English fallbacks until the string tables carry its keys. Skinning it with the UI kit is UI-PC's (S3).

## FEATURES and CHEATS (for INTEGRATION)

**`docs/FEATURES.md`, the Title line (line 9), the "+ SaveSystem persistence" clause.** Replace "New Run deletes all three" with:

> nothing in the slot is ever deleted: New Run sets the old run's files aside (`nope_save.replaced-<time>.json`, ...) and saves day 1's morning at once; a load sets an unreadable save, or one from before the save format, aside (`nope_save.unreadable-<time>.json`, `nope_save.old-format-<time>.json`) and a run recovered from the backup keeps the damaged save aside (tested: `SaveSafetyTests`, `SaveSystemTests`); every morning is saved and kept as the day's morning snapshot (`nope_morning.json`); a save is flushed to the disk; a failed write, a file set aside and a recovered run show a notice strip for 9 s (`SaveNotice`, `save.notice.*`) and a later good save clears a failed write; Continue reads the save from disk again.

**`docs/FEATURES.md`, the cheat menu line (line 255).** Rewrite these clauses:
- The jump clause:

  > jump to the start of any day's shift, 1 to the run's last day (`RunManager.JumpToDay`: forward, today closes as played and has Home's night on the default care, then every day before the target is played with its whole queue decided correctly and its night, so the run lands with a played run's money, citations, pet and premades; back, a new run on the same seed and pet played the same way; tested: `CheatRoutingTests`, `CheatLedgerTests`)

- The restart clause:

  > restart today's shift (`RunManager.RestartShift`: today's morning snapshot comes back, so the day's decisions, pay, citations, premades met and cheat changes are undone, an ending lifted)

- The skip clause, "skip the day":

  > (`RunManager.SkipDay`: today closes as played, Home's night on the default care, Sleep)

- The end-shift clause, add:

  > it waits for an open citation slip to be acknowledged, and a run that has ended goes to its ending, never Home

- The give-all clause, add:

  > a pending order for an item given is cancelled and refunded

- The time-of-day clause: "Dusk 18:30" becomes:

  > Dusk at the day cycle's dusk (`HallDayCycle.DuskHour`)

- Add:

  > every morning (and every cheat's landing) clears the day's switches (the forced hour, Auto-decide, the forced leader, the hall overrides; tested: `DevToolsStateTests`); a restart, a jump back, an appearance on an earlier day and a switch that restarts a running shift show what they lose and act only on Confirm; the open panel blocks all game input (`InputGate`, a uGUI blocker); a cheat that opens a morning closes the panel

- Replace "a cheated run shows a small grey 'CHEATS ON'" with:

  > a cheated run shows "CHEATS ON" at the screen's bottom left with every run switch and waiting next-generation switch named (uGUI, `CheatBadge`); with the panel closed no OnGUI runs

**`docs/FEATURES.md`, the Endings line (line 14).** Add:

> Sleep checks the failures again after the night's resolve, so a night event that crosses a line ends the run that night (`DayCycle.Sleep`, `NightOrder`; tested: `CheatRoutingTests`)

**`docs/CHEATS.md`.**
- **1 … 15:**

  > Jump to the start of that day's shift (the briefing). Forward: today closes as played and has its night; every day between is played with every traveller decided correctly, each night on the default care, so money, pet and story are a played run's. Back: the run replays from day 1 with its seed and pet the same way (asks first). The same day: restart (asks first). A run that has ended can only jump back or restart.

- **Restart shift:**

  > Today again from its morning (the morning snapshot): today's decisions, pay, citations and cheat changes are undone. Asks first.

- **End shift**, add:

  > With a citation slip open it waits for Acknowledge; a run that has ended goes to its ending.

- **Skip day** (was "Skip day (sleep)"):

  > Closes today as played, Home's night on the default care (rent, bills, the pet's night), Sleep, the next morning.

- **Unlock everything**, **Give all items** and **First traveller**: add "Asks first when it restarts a running shift or goes back a day". **Give all items** also gets "a pending order for an item is cancelled and refunded".
- **Time:** "the day cycle's dusk (18:30 until LIGHT's cycle lands) … until the next morning".
- **The More tab's Skip Day** is relabelled "Skip day (its close, Home's night on the default care, Sleep)". **Force leader:** "Kept through the next night (the morning clears it)".
- **Saves section:**

  > A cheated run shows CHEATS ON and every sticky switch (Unlock everything, forced strandings, a forced personality, anything waiting for the next generation) at the bottom left. Every morning clears the day's switches (the forced hour, Auto-decide, the forced leader, the hall overrides). While the menu is open the game takes no input. Every morning is saved and kept as the restart's snapshot.

- **Adding a cheat:** document `DevCheats.Register(label, run, restartsShift: true)` for a cheat that restarts the shift.

**Save format:** `WorldState` is unchanged, so there is no version bump. The morning snapshot is a second file with the same wrapper and version. The coordinated bump stays INTEGRATION's (LEASES).

## RESULT

**Done outside Unity:**

| Check | Before (5aecb60) | After |
|---|---|---|
| `compile_check_any.py E:\unity\NOPE-t5` | 0 errors, 161 warnings | 0 errors, 161 warnings |
| Offline runner (TimeDeskEditMode) | passed 3710, failed 0 | passed 3735, failed 0 |
| Editor-side tests offline (scratch runner, `NeedsEditor` skipped): RunSafeTests plus the 8 SeamTests classes | - | passed 15, failed 0, skipped 29 |

**deferred: Unity.** Run these as one batch in the worktree's editor, on fix/a-run-safe merged after QA-RIG (#2). Use `QaJobs` scenarios where named, and A1 (1920x1080) unless stated. Before the batch, copy the three staged files (above) into place.

1. **deferred: Unity. Import and compile.**
   - **Recipe:** open the project and let it import. Then:
     - Build Office UI, and compare OfficeGameplay by semantic dump with its pre-merge dump (this track changes no builder or scene, so it must be identical);
     - smoke play: Title → New Run → day 1 → one traveller decided.
   - **Pass:** 0 compile errors, 0 console errors, dump identical.
2. **deferred: Unity. EditMode suite.**
   - **Recipe:** run the EditMode suite, at least these test classes:
     - `InputGateTests`, `CheatRoutingTests`, `SaveSafetyTests`;
     - `DevToolsStateTests`, `SaveSystemTests`, `CheatLedgerTests`;
     - the SeamTests (`TestRunnerApi`, `Filter { testMode = EditMode, groupNames = { "^InputGateTests", "^CheatRoutingTests", "^SaveSafetyTests", "^DevToolsStateTests", "^SaveSystemTests", "^CheatLedgerTests", "SeamTests$" } }`, report to SCRATCH).
   - **Pass:** all green (the known third-party UnitySkills failure aside).
   - **If `AJumpToTheLastDay` fails** on a bankrupt or welfare ending: record the ending and day, and file it to CONTENT as a balance finding (decision 1).
3. **deferred: Unity. Video: Night, Skip, Jump to 8, Restart.**
   - **Recipe:**
     1. Golden seed 12345: New Run → day 1 briefing → start the shift.
     2. Cheat: Time: Night, and Auto-decide ON.
     3. Record SEQ 30 fps through: Skip day (lands on day 2) → Jump to 8 → Restart (Confirm) → wait 30 s at the briefing.
   - **Pass, at every landing:**
     - the briefing shows, and the panel is closed;
     - the frame-0 hall hour equals the clock's (`HallLightingRig.Hour` logged = the shift clock's hour);
     - Auto-decide is off (no `CheatDecide` log within 30 s);
     - the badge shows "CHEATS ON" with no day switch named.
4. **deferred: Unity. `day_jumps` (QA-RIG's scenario, owner RUN-SAFE)**, plus restarts from days 1, 7 and 12.
   - **Recipe:** `QaJobs.DayJumps`, then the gate.
   - **Pass:**
     - each step reaches `waitOffice` at the right day;
     - `day_jumps.console` is clean;
     - `runsafe.play.dayJumps` is green.
   - Expect the jumps to take longer than before: each day passed over is generated and decided headless (`DayCycle.PlayShiftCorrectly`). The load's frozen frame is FLOW's at #15.
5. **deferred: Unity. Day-1 quit, New Run and a read-only folder.**
   - **Recipe and pass:**
     - **Day-1 quit.** New Run → day 1 office → stop play mode mid-shift → play from the Title. **Pass:** Continue is offered and resumes day 1's morning (the briefing, the day-1 money).
     - **New Run with a save.** **Pass:** `Library/EditorSaves` gains `nope_save.replaced-*.json` (and `.bak`/morning companions), and nothing is deleted. Re-run at HOME #14 for the confirm.
     - **Failed write.** Create a folder named `nope_save.json.tmp` in `Library/EditorSaves`, then end a shift. **Pass:** the RECORDS strip shows for about 9 s, and the old save stands. Remove the folder and save again: no strip.
     - **Unreadable save.** Write `garbage` into `nope_save.json` and play the Title. **Pass:** the "damaged save" strip shows, `nope_save.unreadable-*.json` exists with "garbage" in it, and the Title offers New Run only.
6. **deferred: Unity. Overlay block.**
   - **Recipe:** `overlay_block.json` (staged), then by hand: open the PC, then F9, then click its icons and type.
   - **Pass:**
     - between the marks `panel_open` and `panel_closed`: 0 log lines from OfficeControls, DeskInspect or DesktopWindowManager acting; the probe shows the same view, no wheel, the PC still closed;
     - after closing, Q opens the PC;
     - over the PC, no app opens behind the panel;
     - `runsafe.play.overlayBlock` is green.
7. **deferred: Unity. Fired run, End Shift (BUG-28b, CODE-108).**
   - **Recipe:**
     1. Day 1: cheat Stab. 70, then approve faulty travellers until the run is fired (the slip opens).
     2. Cheat End shift while the slip is open.
     3. Second case: a citation with no ending, then End shift while that slip is open.
   - **Pass:**
     - Fired run: the line reads "acknowledge the slip and the ending follows"; no report is drawn; after Acknowledge, the ending screen shows (not Home).
     - No ending: the line reads "Closing time once the citation slip is acknowledged"; the report comes only after Acknowledge.
   - Re-run at FLOW #15.
8. **deferred: Unity. Rewinds warn.**
   - **Recipe:** on day 6, each of these:
     - Jump to 3; Restart shift;
     - First traveller set to a day-2 beat;
     - Unlock everything; Give all items; Law-breaker next.
   - **Pass:**
     - each shows the warning row (naming the days, or today's decisions, money and citations) with Confirm and Cancel;
     - Cancel changes nothing (day, money; `nope_save.json`'s write time unchanged);
     - Confirm acts;
     - 0 rewinds happen without a Confirm.
9. **deferred: Unity. Night event and firing (BUG-56b).**
   - **Recipe:**
     1. Cheat stability to 60.5 (More tab: Set 100, then −10 steps, or a scripted `DevCheats.Run(SetStability, amount: 60.5f)`).
     2. Trigger the Rook night event (−3%; set its story flag in the More tab's Flags if its day has not come).
     3. Sleep (Home) or Skip day.
   - **Pass:**
     - the run ends that night: the Title shows the fired ending, logged as `[EndingService] Immediate` from `DayCycle.Sleep`;
     - the next morning never starts.
10. **deferred: Unity. Cheat Dusk.**
    - **Recipe:** Time: Dusk.
    - **Pass:** `DevToolsState.ForcedHour == HallDayCycle.DuskHour`. The by-eye match with a natural 18:30 is LIGHT's, at #11.
11. **deferred: Unity, [BP]. Allocations.**
    - **Recipe:** in the QA player, run `bp_smoke` with a cheated run (the badge showing) and the panel closed, then `QaJobs.GcSites`.
    - **Pass:** 0 B/frame from DebugPanelController, IMGUI and CheatBadge.
12. **deferred: Unity. The plan's ledger in play.**
    - **Recipe:**
      1. Play day 1 by hand (2 travellers decided), then Restart (Confirm).
      2. Diff `nope_save.json` against `nope_morning.json`.
      3. Skip day.
      4. Jump to 6.
      5. Jump back to 3 (Confirm).
    - **Pass:**
      - after Restart, the two saved worlds are equal apart from `cheated`;
      - after the Skip, day 2's money = day 1's end money − rent − the night's bills (the statement shows both rows);
      - the jump back to 3 matches a fresh New Run (seed 12345, same pet) jumped to 3, field by field.
13. **deferred: Unity. Cheat grant.**
    - **Recipe:** order an item in Orders, then Give all items (Confirm).
    - **Pass:**
      - the order log has no pending entry, and the price is refunded;
      - the next morning brings no delivery memo for it.
14. **deferred: Unity. Note 22 re-check (before and after).**
    - **Recipe:** Saleh's path: Time: Night, then Skip day; then Jump to 8 and Restart.
    - **Pass:** each lands at the beginning of the day (the briefing, the clock's light). Capture before (5aecb60) and after.

**For the adversarial reviewer:**
- The quoted rule (D18) is honoured: the panel stays visible, and only the input block and the badge changed.
- No design rule was invented: the decisions above are marked as ours.
- One open question for Saleh, if he wants it: should days a jump passes over be played by an all-correct clerk (as built), or should a jump only charge the nights?
