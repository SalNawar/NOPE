# RUN-SAFE: cheats and saves you can trust

Branch `fix/a-run-safe`, cut from `fix/p0-cuts` (5aecb60). Merge #3 of Phase A.

Sources: FIX_PLAN.md §0 and §3 "RUN-SAFE", FIX_PLAN_v2.md "RUN-SAFE: delta" (v2 wins), BUGLIST.md (BUG-25, 27, 28, 35, 56, 65, 68), DECISIONS_1019a.md, GAME_FEEL_BIBLE.md row G3, PHASEB/LEASES.md, docs/pending/CARVE.md and CUTS.md.

**Goal (v1):** a day skip, jump or restart lands at the true start of the day, with the hall matching the clock, nothing auto-playing, and money and state as in a played run. Quitting never loses the run. Nothing is deleted without asking. The cheat overlay blocks input behind it.

**Saleh's words this track builds (quoted):**
- D18, "Cheats: visible, as now". The panel ships visible, on F9 or ~ as today. v1's "behind a key combo" is dropped. The input block stays, because BUG-68a is a bug. Skinning the panel stays a proposal (S3) and is not done here.
- Note 22 (1019a): "Whe i skkip days with cheats I dont go to the begining of the day".
- Standing rule (memory), "Everything diegetic": the save notice is a screen strip, not a desk object. It reports a fault and asks for no action, so it is not an action surface. Framing it as the Bureau's "RECORDS" desk is **proposal (ours)**.

Code-only run: Saleh was on the PC. Every check that needs Unity is listed under RESULT as "deferred: Unity", with its recipe.

Three commits: the Domain rules (1530848), the fix (3904133) with this doc (b2fa9d2), then the verifier's findings (the commit after b2fa9d2; see "Verifier findings, round 1" below).

## What changed, per BUG part

| Part | What the player or tester gets now | Where |
|---|---|---|
| **27a** day-scoped switches | Every morning clears the day's switches: the natural one after Sleep, and the landing of every skip, jump and restart (`DevToolsState.ResetDay`). That covers the forced hour (the hall shows the clock's hour again), Auto-decide (no new day plays itself), the forced leader (through TEXT-LANG's `CultureThemeService.ResetForNewDay`, P0 stub) and the hall overrides (`HallSlotsDev`, stand-ins included). The run's switches (Unlock everything, forced strandings and fate, a forced personality) and the next generation's (premade next, costume error next, lie next, appearance next) stay until a new or continued run. They are named loudly on the badge: "CHEATS ON: Unlock everything · Lie next SwappedPhoto". A jump holds the next generation's switches aside while it plays the days it passes over and puts them back before the landing (`DevToolsState.HoldNextGeneration`), so the forced appearance, lie, premade or costume error reaches the day the jump lands on. The cheat menu's language lock (`CultureThemeService.LockOverride`, TEXT-LANG's, set by `TranslationLensCheats`) is run-scoped from here: named on the badge ("Language locked" or "Language unlocked") and cleared by a new or continued run (`DevToolsState.Elsewhere`). Every switch has exactly one scope (`DayScoped`, `RunScoped`, `NextGeneration`; a reflection test and a gate check fail on a new switch with no scope). A cheat that opens a morning or ends the run closes the panel, so the day starts at its briefing. A jump back adopts the pet afresh (same kind, name and coat), never a later day's pet. | DevToolsState.cs, RunManager.cs, DebugPanelController.cs, CheatBadge.cs (new), CheatRoute.cs (new) |
| **28a** cheat routing, restart snapshot | Skip, jump and restart run play's own day steps (`RunRoutes` in Domain, played by `CheatRoute.Play`):<br>• **Skip** closes today's shift as played. The live shift closes on its own ledger (`GameManager.CloseShiftForCheat`: strandings, instalment, dialog outcomes, statement row, Home as the resume point, tomorrow modifiers reset; `DayCycle.EndShift`), followed by play's ending check. Then Home's night on the default care (`DayCycle.HomeNight`: break-in, fixed bill, HOME's `NightRules.DefaultNight`, the night's statement row), Sleep, and the next morning.<br>• **Jump forward** does the same for today. Every day before the target is then **played with its whole queue decided correctly**, as the cheat menu's Auto-decide plays it (`DayCycle.PlayShiftCorrectly`), with its night. So the run earns its pay and pays its rent, and it reaches day 15 as a played run would.<br>• **Jump back** builds a new world on the run's seed and pet, played the same way.<br>• **Restart** restores today's **morning snapshot**. RunManager keeps one every morning, in memory and in `nope_morning.json`, so it survives Continue. Today's decisions, pay, citations, premades met, pet changes and cheat changes are undone.<br>• A route from Home (or from the end-of-shift report) starts its night from the end-of-shift save, so a Home arrival already charged in memory is never charged twice. Every cheat change made since that save is replayed on it first (`RunManager.CheatEdits`: stability, money, flags, Unlock and Lock, Give all items, Force leader, other tracks' cheats), so none is lost. What Home itself did before the skip (bills paid, a purchase, the pet's care chosen) is replaced by the night on the default care, as a skip always does.<br>• Give all items and Law-breaker next restart **with** their change (`RestartShift(reapply)`).<br>• **Rewinds warn first.** A restart, a jump back, an appearance on an earlier day (or today in a running shift), and Unlock everything, Give all items or Law-breaker next in a running shift each show what is lost (`DevCheats.Warning`, `RewindWarnings`) and act only on **Confirm**. Cancel changes nothing. Scripts calling `DevCheats.Run` directly (QA-RIG) count as confirmed. | RunRoutes.cs (new, Domain), CheatRoute.cs (new), CheatEdits.cs (new), DayCycle.cs, RunManager.cs, DevCheats.cs, DevToolsState.cs, GameManager.Cheats.cs, LawBreakerCheats.cs, DebugPanelController.cs |
| **28b** End Shift over a citation (the cheat side) | The cheat's End Shift never draws the report over an open citation slip: it waits for Acknowledge, then closes. A run that has ended (fired, bankrupt) goes to its ending, never Home: after the slip's Acknowledge (its own way there), otherwise at once. The decision is a Domain rule (`EndShiftRule.Decide`), tested. A slip counts as open only where the office shows slips (`EndShiftRule.SlipOpens`): with no office UI the verdict's continuation runs at once and no Acknowledge comes, so End Shift never waits forever. The title loads once: when the cheat's End Shift and an ending's own continuation (the verdict's, the report's) both ask, `RunManager.LoadTitleScene` ignores the second request until the scene changes. FLOW's CloseNow (#15) honours the ending in play. | Domain/RunRoutes.cs (`EndShiftRule`), GameManager.Cheats.cs, RunManager.cs |
| **28c** a cheat grant re-delivers the pending order | Give all items and Unlock (More tab) cancel and refund the item's pending order first, through UI-PC's `OrderBook.CancelPendingFor` (P0 stub). The next delivery never brings it again (`DevCheats.GrantAll`). DESK-PLAN's Deliver skipping owned items is DESK-PLAN's part. | DevCheats.cs |
| **25b** the cheat's Dusk | Time: Dusk sets the hour to `HallDayCycle.DuskHour`, LIGHT's one day cycle (P0 stub 18.5), instead of a hard-coded 18.5 (`DevCheats.HourOf`, `TimeChoice`). | DevCheats.cs, DebugPanelController.cs |
| **56b** the ending check after the night | Sleep runs `DayCycle.Sleep` (`NightOrder` in Domain) in this order:<br>1. The day-boundary check: failures, and the run's last day, which still gets its own night resolve.<br>2. The night's resolve.<br>3. The failures again, on the world the night left.<br>4. Only then does the day turn.<br>So a night story event that pushes stability past the line fires the clerk that night, never at the next morning's first correct stamp. | Domain/RunRoutes.cs (`NightOrder`), DayCycle.cs, RunManager.cs |
| **35a** save logic and Continue | **A save every morning, day 1 included.** New Run saves day 1's morning at once, so a quit on day 1 can be continued.<br>**Nothing in the slot is ever deleted.** New Run sets the old run's files aside as `nope_save.replaced-<time>.json` and so on, never deleting them. A load sets an unreadable save, or one from before the format, aside as `nope_save.unreadable-<time>.json` or `nope_save.old-format-<time>.json`, so no later save writes over it. A run recovered from the backup keeps the damaged save aside. The boot no longer starts a New Run that wiped the slot: with no save to continue, it starts a run in memory and writes nothing.<br>**Saves are flushed** to the disk (`FileStream.Flush(true)`).<br>**Failed saves are visible.** A failed write, a file set aside and a recovered run are reported (`SaveSystem.Problem`, `ProblemChanged`) and shown for 9 s by `SaveNotice`, a strip at the top of every screen. A later good save clears a failed write.<br>**Continue reloads from disk** (`RunManager.ContinueRun`), never the run in memory. The NEW RUN confirm (35b, HOME) asks `SaveSystem.HasSave()`. The mid-shift forfeit warning (35c) is UI-PC's. | SaveSystem.cs, SaveNotice.cs (new), Domain/SaveSafety.cs (new), RunManager.cs, TitleSceneController.cs |
| **68a** the overlay and InputGate | While the panel is open it holds the `InputGate` (Hold or Release by name, replacing the P0 stub). OfficeControls, DeskInspect and DesktopWindowManager therefore take no input, through the checks the P0 carve inserted. CheatBadge's full-screen uGUI blocker takes every click the EventSystem routes behind the panel (the uGUI screens of the desk, the PC, Home and the Title). The desk's world-space objects that physics raycasts reach (`DeskController`, `DeskStampTray`) are driven through the gated consumers; recipe 6 checks the desk objects, the PC, Home and the Title by hand. D18: the panel stays visible. | Domain/InputGate.cs, DebugPanelController.cs, CheatBadge.cs (new) |
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
- `DevTools/CheatEdits.cs`
- `Core/SaveNotice.cs`
- `DevTools/CheatBadge.cs`
- `Domain/RunRoutes.cs`: `RunStep`, `RunRoutes`, `NightOrder`, `Rewind`, `RewindWarnings`, `EndShiftAction`, `EndShiftRule`
- `Domain/SaveSafety.cs`: `SaveProblem`, `SaveSafety`
- The tests below

**Not touched:** RunConfigSO.cs, PlayerPrefKeys.cs, HallSlotsDev.cs, FeelCheats.cs, HallSlotsCheats.cs, DebugInspector.cs. Nothing outside RUN-SAFE's list was edited: no scene, asset, string table or other track's file.

**Staged, for after QA-RIG merges** (this branch did not carry QA-RIG's registry):
- The gate checks: installed at the integration merge as `Assets/Editor/QA/Checks/RunSafeChecks.cs`. Its play checks (`runsafe.play.dayJumps`, `runsafe.play.overlayBlock`) read RUN-SAFE's own results of each run: console, presses, quality, every probe's missing scripts and materials, and one probe per landing (`day_jumps.missingScripts.<landing>` for all eight, `overlay_block.missingScripts.after`), so a run that stops early fails them. The probes' LiberationSans results and the run's camera are TEXT-LANG's and CAM's (`qa.play.liberationSans`, `qa.play.camera`, both known red); with `day_jumps.*` the check refused every merge until TEXT-LANG lands (found at the integration's first gate run).
- The overlay scenario: installed at the integration merge as `tools/qa/scenarios/overlay_block.json`.
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
- `RunManager.SkipDay()`, `JumpToDay(int)` (now returns bool; QA-RIG's call still compiles), `RestartShift(reapply)`, `RestartWarning()`, `JumpWarning(day)`, `Mornings`, `CheatEdits` and `RunOn(world, cheat)`.
- `DevToolsState.ResetDay()`, the scope lists (`DayScoped`, `RunScoped`, `NextGeneration`, `Elsewhere`), `HoldNextGeneration`, `RestoreNextGeneration`, `StickyLine` and `StickyKey`.
- `EndShiftRule.Decide` and `SlipOpens` (Domain): the cheat's End Shift decision.

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
  - `DayCycle.PlayShiftCorrectly` repeats `BalanceSimulation`'s generate, present and decide loop. One of them should call the other once BalanceSimulation has an owner.
  - The QA harness (`QaPlayHarness`) backs up `SaveSystem.Files`, which now include `nope_morning.json`. Each scripted New Run leaves `nope_save.replaced-*.json` files in `Library/EditorSaves`; the harness may delete those in its own scratch slot.
- **TEXT-LANG / TOOLS.**
  - `TranslationLensCheats` (Lens: Word, Sentence, Object) grants Translation Lens levels. If those grants own Orders items, they should cancel the pending order the same way (`OrderBook.CancelPendingFor`, see `DevCheats.GrantAll`).
  - The forced leader is now day-scoped: cleared each morning by `ResetForNewDay`. The panel says so ("Kept through the next night").
  - **The language lock (#12, gap found by the verifier).** BUG-27's symptom names "the language lock" and its root cause names `CultureThemeService` statics. `CultureThemeService.LockOverride` (set by `TranslationLensCheats`' "Language: lock now / unlock / follow the ramp") was neither cleared nor shown. RUN-SAFE now names it on the badge and clears it in `DevToolsState.ResetAll` (a new or continued run), without editing CultureThemeService.cs: it is treated as run-scoped. If TEXT-LANG wants it day-scoped instead, clear it in `ResetForNewDay` and move its entry from `DevToolsState.Elsewhere` into the day's doc line; the badge then stops naming it at the next morning on its own.
  - `TranslationLensCheats` registers without `restartsShift`, so its cheats are noted as world changes (`RunManager.CheatEdits`) and replayed on the end-of-shift save by a skip or jump from Home (through `RunManager.RunOn`, which makes the save the run's `World` while the cheat runs). A cheat registered with `restartsShift: true` is not replayed: its change comes with its own restart.
- **Residual input consumers.** These poll the keyboard and are not gated: `DesktopKeyboard`, `TreeScrollRect`, `AppPane`, `SearchResultsView` and `MatchLines` (UI-PC), `SlotMachineView` (HOME), and `DeskView` and `CityView` (CAM). The blocker stops their clicks, but not their keys while the panel is open. Each owner adds `if (InputGate.Blocked) return;` if it matters. The P0 list of three (68b) is gated.
- **Leases (LEASES.md).**
  - `GameManager.Cheats.cs` passes to BELL-SIGN unless RUN-SAFE ships a `CallNextForCheat()` seam. It does not: nothing in this track needed one.
  - `DevCheats.cs` passes to TIMELINE-FAIL and `Core/DayCycle.cs` to DETAIN, after this merge.

## Tests

**Offline (TimeDeskEditMode, the NOPE-tools runner):** passed 3739, failed 0 (baseline 3710: −1 old stub test, +26 in the fix, +4 in the verifier round).
- `InputGateTests` (5, replacing the stub's 1): blocked exactly while held; a hold taken twice is released once; one overlay never frees another's hold; unnamed holds and unheld releases change nothing.
- `CheatRoutingTests` (20):
  - skip from the office and from Home;
  - a jump forward closes today as played and plays the days between; a jump from Home; a jump back; today's jump is a restart;
  - the restart's steps;
  - every one of the 15×15×2 routes ends at the morning, and no night runs before its shift closes;
  - only today is closed as played, every other day is played;
  - the target is clamped;
  - Sleep checks the endings after the night (fires that night), turns the day without an ending, and ends on a boundary ending without turning;
  - the restart's and the jump back's warnings, and which change is a rewind;
  - End Shift (`EndShiftRule`): a set ending routes to the ending whatever the shift's state (after an open slip's Acknowledge), an open slip makes closing time wait, no shift ends nothing, and a slip is open only where slips are shown.
- `SaveSafetyTests` (5): set-aside names (reason and time, a second one in the same second), what is set aside, the reason by problem, the worst problem told, a good save clearing only a failed write.

**Editor-side, `Assets/Editor/RunSafeTests/` (Assembly-CSharp-Editor; in Unity's EditMode suite).** Offline, with the scratch runner, `NeedsEditor` skipped: RunSafeTests plus the 8 SeamTests classes, passed 21, failed 0, skipped 32.
- `DevToolsStateTests` (11). Nine run outside Unity too (a scratch variant of the runner):
  - every switch has one scope;
  - a morning keeps the run's and the next generation's switches;
  - the badge's line and its key;
  - the cheat's Dusk is `DuskHour`;
  - the time choice follows the forced hour;
  - the language lock is listed in `Elsewhere` and named on the badge, and its key changes with it;
  - holding the next generation turns its switches off and restoring puts the same ones back (the run's untouched); restoring nothing changes nothing.
  - Two are `NeedsEditor` (clearing logs, and the log needs the engine): a morning clears the day's switches and the hall overrides; a new run clears the language lock.
- `CheatEditsTests` (3, pure, run outside Unity too): the changes are replayed on the save in the order they were made; a save forgets them; a null edit or no save changes nothing.
- `SaveSystemTests` (7, `NeedsEditor`, JsonUtility; on a scratch slot):
  - read back, with a backup kept;
  - an unreadable save is set aside, kept and reported;
  - an old-format save is set aside and never written over;
  - a backup recovery keeps the damaged save;
  - New Run sets every file aside and deletes nothing;
  - a failed write is reported, the last good save stands, and a later good save clears the report;
  - the morning snapshot is kept apart from the slot.
- `CheatLedgerTests` (11, `NeedsEditor`, the run's library). Field-by-field diff of the whole saved world:
  - skip equals the shift's close, Home's night on the default care and Sleep, and rent is charged;
  - a jump to 6 equals day 1 closed and days 2-5 played;
  - **a jump to the last day lands there without an ending** (QA reaches days 8, 12 and 15 by jumps);
  - a jump back to 3 equals a new run played to 3, with the same pet;
  - a restart equals the morning (money, citations, premades, pet, phase, ending);
  - a restart with no snapshot keeps the world and lifts the ending;
  - a skip from Home starts from the end-of-shift save, never charging the arrival twice;
  - the live shift closes once, on its own ledger;
  - a cheat grant cancels the pending order, and it is never delivered again;
  - **a skip from Home keeps the cheat changes made at Home** (stability, money, a flag): equal to those changes on the end-of-shift save, then the night;
  - **a jump keeps the next generation's switches for the day it lands on:** Force appearance (a once-per-run premade) and Law-breaker next on day 2, then Jump to 8. The switches are still set at day 8, the premade's met flag equals a jump with no switch set, the whole world equals that jump field by field, and day 8's generation then takes the appearance (the premade met, the switch used up).
- What "play" means in the ledger tests: `DayCycle`'s steps, the ones `GameManager.HandleDayCompleted` and `HomeManager` (Start's arrival, HandlePay, HandleSleep) run between their screens. The tests prove the route's orchestration over those steps. They do not run the scenes: that equivalence is recipe 12.

TDD: every test was red first (a compile error on the missing API), then green.

## Decisions taken here (ours, not Saleh's)

1. **A skip closes today as played.** The rest of today's queue is not decided for the player, the same as End Shift. **Days a jump passes over are played with every traveller decided correctly** (the whole queue, the Auto-decide cheat's player). Without that, a jump would either charge nothing (BUG-28) or bankrupt a run that never earns, and QA could not reach day 15 (starting money 50, rent 30 a night). Under a harsh economy (D9), even an all-correct run might not survive to day 15. If so, `CheatLedgerTests.AJumpToTheLastDay_LandsThere_AsAPlayedRunWould` fails in Unity. That is a balance finding for CONTENT, not a cheat bug.
   - **What a day played headless does not do.** It runs `DayCycle`'s steps, not the office scene's `GameManager.Start`. So for the days a jump passes over: the morning paper is not recorded into the News site's back issues (`NewsArchive.Record`), so those days have no back issues; the desk guide's day is not begun (`guide.BeginShift`: its FTUE and guided pages are not marked for those days); the desk upgrades' day is not begun (`deskUpgrades.BeginDay`, the scene's deliveries on the desk); and the travellers are generated with the interview and dress tells always on, where the office turns them off when no interview or appearance check is reachable. Money, citations, the pet, orders, premades met, the statement and the history are a played run's.
   - **The forced leader inside a jump.** A day switch is cleared at the landing, not at the mornings inside the jump, so a forced leader holds through every night a jump passes over.
2. **A jump forward from a run that has ended is refused,** with a line in the panel. A restart or a jump back still works. The same goes for Skip.
3. **The forced leader is a day switch.** It holds through the next night, then the morning clears it, through TEXT-LANG's reset.
4. **The confirm is a row in the panel itself** (IMGUI): the warning line, Confirm, Cancel. Scripts that call `DevCheats.Run` are not asked.
5. **The badge moved to uGUI and says more:** the run's and the next generation's switches, by name.
6. **Set-aside files are kept forever.** They are small, and New Run is rare. A cleanup belongs in Settings later, behind a confirm.
7. **The save notice is a plain strip** with English fallbacks until the string tables carry its keys. Skinning it with the UI kit is UI-PC's (S3).
8. **A skip or jump from Home replays the cheat changes on the end-of-shift save** (verifier round 1, finding 2, option a). Warning or refusing was the other choice. Replaying keeps a tester's stability, money, flag, item, unlock and leader cheats at Home, and keeps the arrival charged once. Home's own choices before the skip are still replaced by the default care: a skip plays the night for you.
9. **The language lock is run-scoped** (verifier round 1, finding 3): named on the badge, cleared by a new or continued run. Day-scoped is TEXT-LANG's call (see the notes). **At TEXT-LANG's integration (#12)** it became day-scoped: `CultureThemeService.ResetForNewDay` clears it and `DevToolsState.ResetDay` calls that every morning; `Elsewhere` keeps its entry (where the switch lives), and the badge, `runsafe.switch.elsewhere` and the tests are unchanged.

## FEATURES and CHEATS (for INTEGRATION)

**Folded** into `docs/FEATURES.md` and `docs/CHEATS.md` by INTEGRATION on integrate/a (2026-10-20), as written below; the More tab's "the badge alone while a run is cheated" became "no OnGUI runs; the CHEATS ON badge is uGUI".

**`docs/FEATURES.md`, the Title line (line 9), the "+ SaveSystem persistence" clause.** Replace "New Run deletes all three" with:

> nothing in the slot is ever deleted: New Run sets the old run's files aside (`nope_save.replaced-<time>.json`, ...) and saves day 1's morning at once; a load sets an unreadable save, or one from before the save format, aside (`nope_save.unreadable-<time>.json`, `nope_save.old-format-<time>.json`) and a run recovered from the backup keeps the damaged save aside (tested: `SaveSafetyTests`, `SaveSystemTests`); every morning is saved and kept as the day's morning snapshot (`nope_morning.json`); a save is flushed to the disk; a failed write, a file set aside and a recovered run show a notice strip for 9 s (`SaveNotice`, `save.notice.*`) and a later good save clears a failed write; Continue reads the save from disk again.

**`docs/FEATURES.md`, the cheat menu line (line 255).** Rewrite these clauses:
- The jump clause:

  > jump to the start of any day's shift, 1 to the run's last day (`RunManager.JumpToDay`: forward, today closes as played and has Home's night on the default care, then every day before the target is played with its whole queue decided correctly and its night, so the run lands with a played run's money, citations, pet and premades (not the News site's back issues or the desk guide's days for the days passed over); the next generation's switches wait for the day it lands on; back, a new run on the same seed and pet played the same way; tested: `CheatRoutingTests`, `CheatLedgerTests`)

- The restart clause:

  > restart today's shift (`RunManager.RestartShift`: today's morning snapshot comes back, so the day's decisions, pay, citations, premades met and cheat changes are undone, an ending lifted)

- The skip clause, "skip the day":

  > (`RunManager.SkipDay`: today closes as played, Home's night on the default care, Sleep; from Home, from the end-of-shift save with every cheat change made since replayed on it, what Home itself did replaced by the default care; tested: `CheatLedgerTests`, `CheatEditsTests`)

- The end-shift clause, add:

  > it waits for an open citation slip to be acknowledged, and a run that has ended goes to its ending, never Home (`EndShiftRule`; tested: `CheatRoutingTests`)

- The give-all clause, add:

  > a pending order for an item given is cancelled and refunded

- The time-of-day clause: "Dusk 18:30" becomes:

  > Dusk at the day cycle's dusk (`HallDayCycle.DuskHour`)

- Add:

  > every morning (and every cheat's landing) clears the day's switches (the forced hour, Auto-decide, the forced leader, the hall overrides; tested: `DevToolsStateTests`); a restart, a jump back, an appearance on an earlier day and a switch that restarts a running shift show what they lose and act only on Confirm; the open panel blocks all game input (`InputGate`, a uGUI blocker); a cheat that opens a morning closes the panel

- Replace "a cheated run shows a small grey 'CHEATS ON'" with:

  > a cheated run shows "CHEATS ON" at the screen's bottom left with every run switch, waiting next-generation switch and the language lock named (uGUI, `CheatBadge`); with the panel closed no OnGUI runs

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

  > Closes today as played, Home's night on the default care (rent, bills, the pet's night), Sleep, the next morning. From Home (or over the shift report) it starts from the end-of-shift save: every cheat change made since is kept (replayed on it), but what you did at Home (bills paid, purchases, the care chosen) is replaced by the default care. To play Home's night your way, Sleep at Home instead.

- **Unlock everything**, **Give all items** and **First traveller**: add "Asks first when it restarts a running shift or goes back a day". **Give all items** also gets "a pending order for an item is cancelled and refunded".
- **Time:** "the day cycle's dusk (18:30 until LIGHT's cycle lands) … until the next morning".
- **The More tab's Skip Day** is relabelled "Skip day (its close, Home's night on the default care, Sleep)". **Force leader:** "Kept through the next night (the morning clears it)".
- **Language: lock now / unlock** (TEXT-LANG's lines): add "Shown on the CHEATS badge; a new or continued run follows the ramp again".
- **Saves section:**

  > A cheated run shows CHEATS ON and every sticky switch (Unlock everything, forced strandings, a forced personality, the language lock, anything waiting for the next generation) at the bottom left. A jump keeps what waits for the next generation for the day it lands on. Every morning clears the day's switches (the forced hour, Auto-decide, the forced leader, the hall overrides). While the menu is open the game takes no input. Every morning is saved and kept as the restart's snapshot.

- **Adding a cheat:** document `DevCheats.Register(label, run, restartsShift: true)` for a cheat that restarts the shift.

**Save format:** `WorldState` is unchanged, so there is no version bump. The morning snapshot is a second file with the same wrapper and version. The coordinated bump stays INTEGRATION's (LEASES).

## RESULT

**Done outside Unity:**

| Check | Before (5aecb60) | After |
|---|---|---|
| `compile_check_any.py E:\unity\NOPE-t5` | 0 errors, 161 warnings | 0 errors, 161 warnings |
| Offline runner (TimeDeskEditMode) | passed 3710, failed 0 | passed 3735, failed 0 (fix); passed 3739, failed 0 (verifier round 1) |
| Editor-side tests offline (scratch runner, `NeedsEditor` skipped): RunSafeTests plus the 8 SeamTests classes | - | passed 15, failed 0, skipped 29 (fix); passed 21, failed 0, skipped 32 (verifier round 1) |

**deferred: Unity.** Run these as one batch in the worktree's editor, on fix/a-run-safe merged after QA-RIG (#2). Use `QaJobs` scenarios where named, and A1 (1920x1080) unless stated. Before the batch, copy the three staged files (above) into place.

1. **deferred: Unity. Import and compile.**
   - **Recipe:** open the project and let it import. Then:
     - Build Office UI, and compare OfficeGameplay by semantic dump with its pre-merge dump (this track changes no builder or scene, so it must be identical);
     - smoke play: Title → New Run → day 1 → one traveller decided.
   - **Pass:** 0 compile errors, 0 console errors, dump identical.
2. **deferred: Unity. EditMode suite.**
   - **Recipe:** run the EditMode suite, at least these test classes:
     - `InputGateTests`, `CheatRoutingTests`, `SaveSafetyTests`;
     - `DevToolsStateTests`, `CheatEditsTests`, `SaveSystemTests`, `CheatLedgerTests`;
     - the SeamTests (`TestRunnerApi`, `Filter { testMode = EditMode, groupNames = { "^InputGateTests", "^CheatRoutingTests", "^SaveSafetyTests", "^DevToolsStateTests", "^CheatEditsTests", "^SaveSystemTests", "^CheatLedgerTests", "SeamTests$" } }`, report to SCRATCH).
   - **Pass:** all green (the known third-party UnitySkills failure aside).
   - **If `AJumpToTheLastDay` fails** on a bankrupt or welfare ending: record the ending and day, and file it to CONTENT as a balance finding (decision 1).
   - **If `AJump_KeepsTheNextGenerationsSwitches` fails** on "a once-per-run premade in the library": the library has none; pick a story beat instead (`DevCheats.Appearance(lib, "beat:8:0")`) and say so in the report.
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
   - **Recipe:** `overlay_block.json` (staged), then by hand, each with F9 open and a click and a key press behind the panel:
     1. **The desk.** Click and drag a paper, click the stamp tray and a stamp, click a drawer and a desk object the physics raycasts reach (`DeskController` and `DeskStampTray` cast against the interactable and paper layers; they are not among the three gated consumers, so the blocker is what stops them).
     2. **The PC.** Open the PC, then F9, then click its icons and type.
     3. **Home.** At Home: click the bills toggles, Pay, the pet, the slot machine and Sleep.
     4. **The Title.** On the Title: click Continue, New Run and Settings.
   - **Pass:**
     - between the marks `panel_open` and `panel_closed`: 0 log lines from OfficeControls, DeskInspect or DesktopWindowManager acting; the probe shows the same view, no wheel, the PC still closed;
     - no paper moves, no stamp lifts, no drawer opens behind the panel;
     - after closing, Q opens the PC;
     - over the PC, no app opens behind the panel;
     - at Home, no bill toggles, nothing is paid, the slot machine does not spin and Sleep does not run (`[HomeManager]` logs nothing);
     - on the Title, no run starts or continues and Settings does not open;
     - `runsafe.play.overlayBlock` is green.
   - Keys behind the panel at Home and on the Title are not gated (the residual consumers above); note any that act, for their owners.
7. **deferred: Unity. Fired run, End Shift (BUG-28b, CODE-108).**
   - **Recipe:**
     1. Day 1: cheat Stab. 70, then approve faulty travellers until the run is fired (the slip opens).
     2. Cheat End shift while the slip is open.
     3. Second case: a citation with no ending, then End shift while that slip is open.
   - **Pass:**
     - Fired run: the line reads "acknowledge the slip and the ending follows"; no report is drawn; after Acknowledge, the ending screen shows (not Home).
     - No ending: the line reads "Closing time once the citation slip is acknowledged"; the report comes only after Acknowledge.
     - In both fired cases, `[RunManager] LoadTitleScene ->` is logged once. If a second request comes (End Shift on a fired run while the verdict or the report is still up, then its own continuation), it logs "the title is already on its way" and the title is not loaded again.
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
     3. Run it three ways, from the same save each time: (a) Sleep at Home; (b) Skip day from the office mid-shift; (c) at Home, make the two cheats there, then Skip day.
   - **Pass:**
     - in each, the run ends that night: the Title shows the fired ending, logged as `[EndingService] Immediate` from `DayCycle.Sleep`;
     - the next morning never starts;
     - in (c), the log shows `[RunManager] Replaying 2 cheat change(s) made since the save` naming the stability and the flag. Before verifier round 1, a skip from Home dropped both, and the check ran on the wrong world.
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
    - **Against play's real path (scenes, not `DayCycle` alone; verifier round 1, finding 5):**
      1. A second New Run (seed 12345, same pet). Play day 1 by hand the same way (the same 2 travellers, the same stamps), then End shift.
      2. At Home, Pay with the bills as offered (the default care), and Sleep.
      3. Diff day 2's `nope_save.json` against the save after step 3 above (the Skip).
      - **Pass:** equal apart from `cheated`. A difference names a step `HomeManager` or `GameManager.HandleDayCompleted` runs that `DayCycle.HomeNight` or `EndShift` does not: file it to RUN-SAFE.
13. **deferred: Unity. Cheat grant.**
    - **Recipe:** order an item in Orders, then Give all items (Confirm).
    - **Pass:**
      - the order log has no pending entry, and the price is refunded;
      - the next morning brings no delivery memo for it.
14. **deferred: Unity. Note 22 re-check (before and after).**
    - **Recipe:** Saleh's path: Time: Night, then Skip day; then Jump to 8 and Restart.
    - **Pass:** each lands at the beginning of the day (the briefing, the clock's light). Capture before (5aecb60) and after.
15. **deferred: Unity. A forced appearance across a jump (verifier round 1, finding 1).**
    - **Recipe:**
      1. On day 2, cheat Force appearance with a day-8 story beat (`beat:8:0`), Confirm if asked. The cheat jumps to 8.
      2. Again from a day-6 save: Force appearance with a day-3 beat (a jump back), Confirm.
      3. On day 2: Law-breaker next, then Jump to 5.
    - **Pass:**
      - the beat stands first in day 8's queue (and in day 3's for step 2);
      - the log's `[CaseFactory] Cheat: ... stands in slot 1 of day N` names the landing day, never a day passed over;
      - in step 3, day 5's first traveller who can carry it brings someone else's photo; the badge shows "Lie next" until then.
16. **deferred: Unity. The language lock on the badge (verifier round 1, finding 3).**
    - **Recipe:** More tab: "Language: lock now". Then New Run.
    - **Pass:** the badge reads "... Language locked" while set; after New Run it is gone and the office's language follows the ramp (no lock on day 1).

## Verifier findings, round 1 (the commit after b2fa9d2)

| # | Finding | What changed | Tests |
|---|---|---|---|
| 1 | [S2, BUG-28a/27a] Days a jump plays headless used up the next-generation switches (forced appearance, Law-breaker next, Premade next, costume error) on the wrong day, and a once-per-run premade was marked met there. | `CheatRoute.Play` holds the next generation's switches aside around every `PlayShift` step and puts them back after it (`DevToolsState.HoldNextGeneration`, `RestoreNextGeneration`, by name over `NextGeneration`). The landing's generation takes them. | `CheatLedgerTests.AJump_KeepsTheNextGenerationsSwitches_ForTheDayItLandsOn`; `DevToolsStateTests.HoldingTheNextGeneration_...`, `RestoringNothingHeld_...`; recipe 15 |
| 2 | [S2, BUG-28a] A skip or jump from Home threw away every change since the end-of-shift save, cheats included. | Option (a): `DevCheats.Run` notes every world change a cheat makes (`Edit`, into `RunManager.CheatEdits`). The route's end-of-shift save gets them replayed, in order (`RunManager.EndOfShiftSave`; other tracks' cheats through `RunManager.RunOn`). The list is cleared at every good save and every new or continued run. Doc, CHEATS text and recipe 9 updated. Home's own choices are still replaced by the default care (decision 8). | `CheatEditsTests` (3); `CheatLedgerTests.ASkipFromHome_KeepsTheCheatChangesMadeAtHome`; recipe 9 (c) |
| 3 | [S3, BUG-27a gap] The language lock (`CultureThemeService.LockOverride`) was neither cleared nor shown. | On the badge (`StickyLine`, `StickyKey`), cleared by `ResetAll`, listed in `DevToolsState.Elsewhere`. TEXT-LANG's file is untouched; a note for TEXT-LANG (#12) is above. The staged gate checks gain `runsafe.switch.elsewhere`. | `DevToolsStateTests.TheLanguageLock_...`, `ANewRun_ClearsTheLanguageLock`; gate check; recipe 16 |
| 4 | [test gap, BUG-28b] No test for End Shift's logic; `_slipOpen` stuck with no office UI; the title could load twice. | The decision is `EndShiftRule.Decide` (Domain), used by `GameManager.CheatEndShift`. A slip opens only where slips are shown (`EndShiftRule.SlipOpens(citation, officeUI != null)`). `RunManager.LoadTitleScene` ignores a second request until the scene changes. | `CheatRoutingTests` +4 (`EndShift_WithASetEnding_RoutesToTheEnding_NeverHome` and the rest); recipe 7 |
| 5 | [S4] The ledger tests compare against `DayCycle`'s steps, not the scenes; headless days skip what `GameManager.Start` does. | The doc names the exceptions (decision 1: News back issues, the desk guide's day, the desk upgrades' day, the tells' reachability). The FEATURES jump clause names them too. Recipe 12 gains a scene-level comparison. The `BalanceSimulation` duplication is noted for INTEGRATION. | recipe 12 |
| 6 | [S4] `RewindWarnings.JumpBack`'s `target >= today` branch was unreachable. | Removed (a jump to today takes Restart's warning through `JumpWarning`); the test's same-day assert removed. | `CheatRoutingTests.AJumpBacksWarning_NamesTheDaysLost` |
| 7 | [doc] Recipe 6 did not check Home, the Title or the desk's physics raycasts; recipe 9 relied on the Home skip. | Recipe 6 adds the desk objects, Home and the Title. Recipe 9 runs Sleep at Home, Skip mid-shift, and Skip from Home after cheats there. | - |

**For the adversarial reviewer:**
- The quoted rule (D18) is honoured: the panel stays visible, and only the input block and the badge changed.
- No design rule was invented: the decisions above are marked as ours.
- One open question for Saleh, if he wants it: should days a jump passes over be played by an all-correct clerk (as built), or should a jump only charge the nights?
