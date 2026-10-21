# FLOW: smooth transitions, real pause, fast loads, endings that land

Branch `fix/a-flow`, cut from main `9b8ab5f` (P0: carve, cuts, QA-RIG) and **rebased onto origin/main `4290bd6`** (RUN-SAFE merged) in verifier round 2. Sources: FIX_PLAN.md §3 "FLOW" and FIX_PLAN_v2.md "FLOW: delta (D20, D12)" (v2 wins), BUGLIST.md, DECISIONS_1019a.md (D12, D20, R3/R4), GAME_FEEL_BIBLE.md rows F3, G1, G3, G4, J5, J6, and R7\PHASEB\LEASES.md (the GameManager carve, the scoped pause, the Esc seam).

**Bugs closed here (code; each still needs its Unity recipe, §3):** 28b (CloseNow honours the ending), 32a, 34a, 38b, 56a, 65k, 72, 73c (MotionTuning_Default), 73f (the clock warning); 34b **in the office** (R3 included).

**Verifier round 7 (this commit):**
- **S4, the gates' ids are tested** (§1, Gate checks; WAVE2_OPEN2 FLOW item 1): no test failed if a check went back to a pattern or lost an id (only `GateResults.Judge` was tested, on its own). `FlowChecks.All` is split: the pure **`FlowChecks.Verdict(id, builtPlayer, results, head, ids)`** (each id on `GateResults.Judge`; an id with a `*` is refused, never read as a pattern) judges what **`FlowChecks.Measured`** gives (`GateResults.ReadAll()` and `GitHead.Read`). Every FLOW check that reads results goes through it, the nine that read one result too (they called `GateResults.Require` directly). **`FlowSeamTests`: 7 new tests, not NeedsEditor, so the offline editor runner runs them.** They run the registered checks themselves on given results (`Measured` swapped for the call), against a list of each check's exact results:
  - each check passes on its results, and ignores a failing result beside them (one a pattern over its ids would read);
  - each check is red with any one of its results missing ("not measured");
  - `flow.skip.midshift` is red with only the midshift pair and green with all four;
  - `flow.bp.load` is red when any one of its results was measured in the editor ("NOT-BP");
  - `Verdict` refuses a pattern and a check that names no result, and a result from another commit is stale;
  - every listed check is registered with its kind, and every registered FLOW check that reads results is listed;
  - **every result id is produced by a step of FLOW's recipes** (`flow_*.json`, read with `ContentJson`): each FLOW op's assertion as FlowOps names it (`flowRunState`'s `same.<name>`, `flowDayEnds`, `flowTransit`, `flowQueue`, `flowLoadTime` and the others), and each recorded sequence's seqtools results, every `frozen` one from a load recorded alone (`flowRecLoad`, below).
  - **Mutants** (each applied, compiled and run, then restored): `SkipMidShift` back to `GateResults.Require(..., "flow_skip.same.*")`: 3 failures (outside Unity it also throws, reading the results folder itself); `All` given `flow_skip.same.*` and `flow_skip.dayEnds.*`: 3; `flow.bp.load` without `.office_home.frozen`: 2; `flow.transit.shutter` on `flow_shutter.transit.*`: 2; `flow.transitions.cuts` without the sleep sequence: 1; `flow_load_seq`'s `home_office` recorded with `recStart`: 1 (the recipes' test).
- **Frozen reads the load alone** (WAVE2_OPEN2 FLOW item 2; §1 BUG-31a; recipes 4, 5 and 9; §5 decision 13): seqtools' `frozen` reads the whole recording, and `flow_load_seq`'s and `flow_shutter`'s recordings began before the load (the Title's two `newRun` presses, each holding the pointer still for 9 frames) and ended on the settled scene (`flow_load_seq`: `waitOffice`'s 30 frames, then `wait 1`; `flow_shutter`: `wait 0.5`). Either end may hold still for more than 0.25 s, so `flow.bp.load` and the frozen lines of recipes 4, 5 and 9b could go red whatever the load did.
  - New rig op **`flowRecLoad`** (name): its sequence starts on the load's start mark (the click's frame) and stops on its up mark (the shutter off). It asserts nothing: a load that never went through the shutter leaves no sequence, so its results are missing and the check is "not measured".
  - `flow_load_seq` and `flow_shutter` record each load with it. In `flow_load_seq`, `flowSettled` follows the step that starts the load (`flowTransit` waits for the shutter itself), and `flow_shutter` waits for its run's first load (`flowSettled` after `newRun`) before it records, so each recorded change is `flowTransit`'s own. `flow_load_seq` captures office → Home too, so `flow.bp.load` reads every load's frozen: **eight results** (`bp.flow_load_seq.seq.office_home.frozen` is new).
  - `flow_transitions`' `gohome` and `sleep` keep their lead and tail for `cuts`: a cut on the click's own frame, or as the new scene's panels come in, needs them. Recipe 4 no longer reads `frozen` on them.

**Verifier round 6 (`d57c057`):**
- **S4, each gate names its results** (§1, Gate checks): a check that read a pattern (`flow_skip.same.*`) passed on whichever results existed, and `GateResults.Judge` passes when at least one result matches and all matches pass; nothing marks a run complete. So a `flow_skip` run that stopped in the double skip (`flowSettled` or `waitOffice 3` timing out, the way a deadlocked queue or a broken double skip shows) passed `flow.skip.midshift` on the midshift leg alone, and a `flow_shutter` run stopped before `flowQueue` passed `flow.transit.shutter`. `FlowChecks.All` now requires each result by its exact id, as `RunSafeChecks.All` does: `flow.skip.midshift` its four, `flow.transit.shutter` its three, `flow.bp.load` its seven, and (the same hole) `flow.transitions.cuts` its three sequences. `flow.transit.shutter` no longer claims seqtools' frozen and cuts analyses: recipe 5 reads their output.
- **The shutter under the save notice is pinned:** `FlowSeamTests.TheSceneChangeShutter_SortsUnderTheSaveNotice` reads `TransitRunner.VeilOrder` and `SaveNotice.SortingOrder` (both private constants) and fails if the shutter does not sort under the notice. It is not NeedsEditor, so the offline editor runner runs it; with `VeilOrder` set back to 32000 it fails ("Expected: less than 32000, But was: 32000").
- **Docs:** recipe 4 writes its cuts results (`--result-dir`, one sequence per call), so `flow.transitions.cuts` can be measured at all; End Shift's close is described in the code's order (§1 round 4, §4 RUN-SAFE row); recipe 13's signals before the round 4 fix are the ones the midshift leg can show (no instalment: nobody is decided in it); the cheat menu under the shutter is listed in full (§1 round 5, §4 RUN-SAFE row).

**Verifier round 5 (`8b055be`):**
- **S3, the office being replaced is not the live shift** (§1, round 5): round 4 stopped the office left behind the shutter from closing its own day again, but a second Skip day or Jump asked for under the shutter still found that office as `GameManager.Current` (set in `Start`, cleared only in `OnDestroy`), and `RunManager.Run` closed its ledger again on the landing day. `SceneTransit.Load` now raises `SceneTransit.Asked` as each load is asked for, and the office stops being `Current` then; an office that opens while a newer load waits never becomes it. FLOW's files only (`Transit.cs`, `GameManager.cs`); RunManager is unchanged. New `flow_skip` leg: a double skip against a skip, the shutter off, a skip (`flow_skip.same.doubleskip`, `flow_skip.dayEnds.doubleskip`; recipe 13).
- **S4, the queue tested in play** (§1, round 5): new rig op `flowQueue` and a `flow_shutter` leg: the Title asked for, the office while the shutter comes down, the Title again while it rises (`flow_shutter.transit.queue.titleOfficeTitle`; recipe 5).
- **S4, the docs:** the FEATURES line "A day is closed once" (§6) and recipe 13 now say the same save apart from what presenting a traveller marks (and, for the double skip, the back issue of the day no office opened). §1's queue line, §4's RUN-SAFE row and §5's decision 10 no longer claim that queueing alone kept RunManager's routes right.

**Verifier round 4 (`62c1597`):**
- **Ownership:** `docs/FEATURES.md` is back to origin/main `4290bd6` (FIX_PLAN §0 rule 2: only INTEGRATION writes it). FLOW's lines are in §6, "FEATURES lines (for INTEGRATION)", each with the line it replaces.
- **S2, a cheat's close is the day's only close** (§1, round 4): a Skip day or a forward jump from the office mid-shift no longer ends the day a second time on the landing morning. `GameManager` ignores its clock's Closed and its day loop's end once its shift is closed elsewhere or a scene change is replacing the office (`ShiftEnd.LeftBehind`, tested). New QA recipe `flow_skip` (recipe 13) and gate check `flow.skip.midshift`.
- **S3, the rig waits for the shutter** (§1, round 4): `SceneTransit.Changing` holds from the accepted load until the shutter is off; FLOW's op `flowSettled` waits on it after a reloading cheat (`flow_night`, `flow_skip`); QA-RIG's `waitOffice`/`waitScene` line is handed to INTEGRATION (§4). A load asked for during another is queued, the latest winning (`SceneChangeQueue`, tested), never dropped.
- **Minor:** §1 Carve's `carve_check` command has a pinned base (`cb1846a~1`); the shutter sorts at 31999, one under RUN-SAFE's `SaveNotice` (32000).

**Verifier round 3 (`d8bc05b`, docs only):** §1 Carve cites the rebased carve `cb1846a` (14 MOVED, 68 SAME) instead of the pre-rebase `b8c6e1b`; `EveningEditionView.SortingOrder`'s summary says the scene change's shutter (`TransitShutter`), not a black veil.

**Verifier round 2 (`e1218f9`):** rebased onto `4290bd6` (one conflict, the FEATURES.md endings line: both insertions kept, RUN-SAFE's night check first); the statements that RUN-SAFE had not yet adopted `SceneTransit.Load` corrected everywhere; the [BP] load line now measured in real time (`FlowLoadWatch`, `flow_load` / `flow_load_seq`, §1 BUG-31a, recipe 9); `Transit.cs`'s summaries say shutter, not fade; the known-red owners and the right-click question listed for INTEGRATION and Saleh.

**Live in play since the rebase: 31a's shutter and async load.** RUN-SAFE's `RunManager.LoadOfficeScene/LoadHomeScene/LoadTitleScene` (`RunManager.cs`, on main) already call `SceneTransit.Load`, so on this branch every scene change in play brings the shutter down and loads the scene (and the office's gameplay layer) asynchronously. FIX_PLAN names the risk: "Async loading reorders Awake/Start: smoke play and full sequence at every step". **Every recipe in §3 runs on this rebased tree**, and recipe 2 (smoke) and recipe 11 (a full played day) come first.

**Not closed here, with their owners. No register assigns any of them yet: INTEGRATION must carry them as known red and confirm each owner.**
- **34b at Home and on the Title:** no Settings window exists there. FLOW proposes HOME for both (§1, BUG-34b); the Title is in FIX_PLAN's HOME section, Home is only FLOW's proposal. `flow.esc.home` and `flow.esc.title` are red until then.
- **70b:** the fix is DESK-PLAN's builder line and asset; neither is changed on main (`Reaction_Clock.asset:19` `tooltipKey: tooltip.value`, `OfficeSceneUIBuilder.Desk.cs:381` passes `"tooltip.value"`). FLOW adds the gate check `flow.clock.tooltip`, red until then.
- **G3, G4 and J5 (feel rows):** red until the art's `BoothShutter` object and the clips `booth_shutter_open`, `booth_shutter_close`, `day_start`, `available_on` and `amb_hall_day` exist (DESK-PLAN's art pass, FEEDBACK's clips).

**For Saleh to confirm (FLOW's reading, not his words):** a right-click still backs out of the PC. R3 says "To leave the PC, press Q or click the bezel"; FLOW keeps the right-click back-out (Papers, Please's way) because `DesktopKeyboard` routes a right-click the PC's own chain does not take as its back-out, `EscapeChain.KeepsTheOfficeOut` returns false for it, and `OfficeControls` then runs `ClosePc`. If Saleh wants Q and the bezel only, the change is one line in `KeepsTheOfficeOut` (true for a right-click in the PC) and its test (§5.6).

**Saleh's calls built:**
- **D20:** "if you press escape you should auto open the pc setting menu"; pausing is the AVAILABLE sign. There is **no pause card**. FLOW provides the scoped pause (`PauseState`) for BELL-SIGN's closed booth; it does not wire the sign (BELL-SIGN P2, and its question 2 is still open with Saleh).
- **R3/R4:** "Esc opens the same Settings window everywhere (PC, Home, Title). To leave the PC, press Q or click the bezel." In the office, Esc opens the PC's Settings app and **never closes the PC**. Home and Title wait for HOME.
- **D12:** "you are fired if you destroy the timeline". The run ends at the day's end, never the instant a verdict lands. The threshold value is CONTENT's (see 56a).

---

## 1. Changes by BUG id

### Carve (LEASES.md proposal, done as a pure move): commit `cb1846a`
`GameManager.cs` is split so each Phase B track holds its own partial instead of chaining on the root:

| Partial | What moved | Phase B holder |
|---|---|---|
| `GameManager.Booth.cs` | slot arming, the AVAILABLE sign's wiring, calling and presenting the traveller, `React` | BELL-SIGN |
| `GameManager.CaseEnd.cs` | `HandleDecision`, `ShowVerdictThen`, `HandleEndingReached` | TIMELINE-FAIL (if needed), then LANGUAGE |
| `GameManager.News.cs` | Start's archive call, extracted as `RecordTodaysPaper` | NEWS-WEB |
| `GameManager.Departures.cs` | `Departed`, `AnnounceDeparture` | PORTALS |

No serialized field moved. `python E:\unity\NOPE-tools\scripts\carve_check.py --base cb1846a~1 --head cb1846a E:\unity\NOPE-docs`: 14 MOVED, 68 SAME. The base is pinned to the carve's own parent (`cb1846a~1` is `4290bd6`), so the result holds after main moves; a base of `origin/main` breaks as soon as main moves (with main at HALL-FX's integration it prints "FAIL (231)"). (`cb1846a` is the carve after the round 2 rebase onto `4290bd6`; the pre-rebase carve `b8c6e1b` is no longer on this branch. **After any later rebase, update the hash here.**) Its two reports are the extract itself: `Start` lost the archive lines, and `RecordTodaysPaper` is the new member holding them, unchanged. Both were checked by hand. LEASES.md's "if FLOW doesn't carve" chain on `GameManager.cs` no longer applies, and INTEGRATION updates the register. The shutter's call in `OpenShift` (`GameManager.Booth.cs`, this branch) passes to BELL-SIGN with the partial.

### BUG-34a: the clock and audio pause on focus loss and while Settings is open
- **`Domain/PauseState.cs` (new, pure).** The game's pause is a set of named sources: `Request(source, scope)` and `Release(source)`.
  - `PauseScope.Clock` holds the shift clock only (the booth closed, a citation slip).
  - `PauseScope.Full` holds the clock and the audio (Settings, focus lost).
  - `ClockHeld` is true while any source holds; `FullHeld` while a Full one does.
  - One source's release never restarts a clock another holds, in any order (BELL-SIGN P2's requirement).
  - Sources: `Settings`, `Focus`, `Citation`. BELL-SIGN adds its own (`"booth"`, Clock).
- **Full and input (v2 said "clock, audio, input").** Full blocks no input itself, and the doc comment now says so; before, it claimed input stopped. Input is already covered by context:
  - Settings in the office is the PC's window, and the open PC takes the office's input from the desk (BoothCoordinator).
  - The focus lost delivers no input and cancels the desk's hold (`HoldRegistry.OnFocusLost`).
  - HOME's Settings windows must be modal themselves (§4).
  - FLOW does not route Full through `InputGate`. RUN-SAFE's gate also blocks the PC (`DesktopWindowManager`), so it would block Settings itself.
- **`Shift/ShiftClockDriver.cs`.** Each frame it follows `PauseState.Shared.ClockHeld` with one `ShiftClock.Pause` / `Resume` pair, so the clock's own nesting stays balanced. Its public `Pause`/`Resume` are gone: every pause is a PauseState source.
- **`GameManager.CaseEnd.cs`.** A citation slip holds the clock as the `Citation` source (it was a direct `Pause`/`Resume`). `GameManager.OnDestroy` releases it, so a slip still up as the office goes (a jump, quitting) holds nothing in the next scene.
- **`UI/PauseMenu.cs` (new).** Made once at startup (`RuntimeInitializeOnLoadMethod`), kept across scenes, hidden.
  - It holds `Settings` (Full) while Settings shows, and `Focus` (Full) while the window has lost the focus and the game does not run in the background.
  - Audio: the focus lost pauses everything (`AudioListener.pause`). Settings pauses the looping beds (room tone, music) and lets one-shot cues finish, so Settings' own clicks sound.
  - The focus lost also cancels the desk's hold (`HoldRegistry.Desk.OnFocusLost`).
- **`ProjectSettings.asset`: `runInBackground: 0`.** The player now pauses when it loses the focus. The QA rig and the editor's job runner set `Application.runInBackground` on for their own runs (unchanged), so a run in an unfocused editor keeps playing.

### BUG-34b: Esc opens Settings (no pause card); R3 in effect
- **`UI/PauseMenu.cs`.** On Esc it does nothing while a scene loads (`SceneTransit.Loading`) or the cheat overlay holds the input (`InputGate.Blocked`). Otherwise it runs `EscapeChain.Shared.LetGo()`, and when nothing let go, it opens Settings:
  - **Office:** the PC opens by Q's own rules (`OfficeControls.Press(ControlKey.Pc)`, only when `PcFrame.CanOpen`, so never under the briefing or the report), with the Settings app's window in front.
  - **Home and Title:** the scene's `SettingsWindowController`, when it has one. Neither scene has one (see the hand-off below).
- **R3: Esc never closes the PC, and never leaves a mode.** This is new in this round. Before it, the office's own back-out (`OfficeControls` → `ControlRules.BackOut`, DOC-HANDLE's) still closed the PC on Esc.
  - **`Domain/EscapeChain.cs`: `KeepsTheOfficeOut(state, desktopTookIt, rightClick)` (pure, tested).** It is true when the office's back-out would leave a mode on this Esc press: the PC, the wheel, the value held, inspect mode, the city, the reading view.
    - A drag is a hold, so the back-out still cancels it first (v2's precedence: "cancel a hold first, then open Settings").
    - A right-click still backs out, Papers, Please's way, **including out of the PC** (`DesktopKeyboard` routes an untaken right-click as the back-out; `KeepsTheOfficeOut` returns false for it; `OfficeControls` runs `ClosePc`). R3 names only Q and the bezel, so this is FLOW's decision for Saleh to confirm (§5.6).
    - The PC's own chain still goes first: a menu, the card, the search, a field (`DesktopKeyboard.EscapeTakenFrame`).
  - **`UI/EscapeWatch.cs`** now runs at -1085, between the PC's poller (`DesktopKeyboard`, -1090) and the office's (`OfficeControls`, -1080). On such a press it keeps `OfficeControls` out of that one frame (`enabled = false`), and `PauseMenu.Update` turns it back on in the same frame.
    - So the PC stays open and Settings comes to the front. Only Q or the bezel leave the PC.
    - `OfficeControls` has no OnEnable/OnDisable, so the toggle skips exactly one `Update` and nothing else. No other code toggles it.
  - **The "office" chain step (order 150)** now only reports a drag the back-out cancelled.
  - **Removal:** once DOC-HANDLE's `OfficeControls` stops backing out on Esc (v2: Esc "falls through to FLOW's Esc→Settings"), the guard never fires. INTEGRATION then removes `KeepsTheOfficeOut`, EscapeWatch's guard and the office step, in one change with DOC-HANDLE's.
- **`Domain/EscapeChain.cs` (new, pure): the "let go first" seam (LEASES.md proposal).** Steps run lowest order first, ties in registration order; the first that lets go ends the press. FLOW's own steps:
  1. `hold` (0): the desk's hold is cancelled first (DOC-HANDLE's v2 rule).
  2. `desktop` (100): the PC's own Escape chain.
  3. `office` (150): a drag the office's back-out cancelled.
  4. Phase B tracks register their own at `EscapeChain.TrackOrder` (200): DIALOGUE's open ring, PORTALS' held cable. No lease on FLOW's handler is needed.
- Esc never changes the AVAILABLE sign (BELL-SIGN P7).
- **Hand-off: Settings at Home and on the Title. INTEGRATION assigns both to HOME (#14).**
  - **Title:** FIX_PLAN's HOME section already promises "a Title screen ... with Settings, Quit and Credits".
  - **Home:** no section names it. FLOW proposes HOME, since HOME owns the Home scene and its builders: build a `SettingsWindowController` into `HomeScene` too. HOME-3D may later move it to the tablet; PauseMenu finds whatever window the scene has, so no FLOW change follows.
  - **Both:** PauseMenu needs no code change. The window must be modal (Full blocks no input). `flow.esc.home` and `flow.esc.title` stay known red until HOME merges.
- **Hand-off: Quit to Title and Quit from Settings (v2's proposal; not built, and not FLOW's files).** Owners:
  - **UI-PC** (`OfficeSceneUIBuilder.Apps.cs`, the Settings app's window): two rows at the window's foot.
  - **TEXT-LANG** (`SettingsWindowController.cs`): their buttons, and the strings `settings.quitToTitle` and `settings.quit` in all 7 tables.
  - **Behind them:** Quit is `Application.Quit()`, as `DesktopShell`'s Start-menu "Quit game" does today. Quit to Title is `RunManager.LoadTitleScene()`. RUN-SAFE decides what a mid-shift Quit to Title does to the run (save, or drop the day); `LoadTitleScene` already goes through `SceneTransit.Load`.
  - **HOME's** Title and Home windows carry the same two rows; Quit to Title is hidden on the Title itself.
  - FLOW's `flowSettings` op is extended for them when they land.

### BUG-31a: seals and daters painted once; async load behind an in-world transition; each scene themed once
- **`Visuals/SealOutlines.cs`.** `Resolve(name)` looks the outline up once and returns an `Outline` that tests points without a dictionary lookup. The load spent 64% of its time on 2.7 M `OrdinalIgnoreCase` lookups. `Inked(name, x, y)` stays, through it.
- **`UI/Forms/SealArt.cs`.** Paints from the resolved outline. Its textures were already cached per shape for the session; now the one paint is cheap too.
- **`UI/Forms/DaterImpressionArt.cs`.**
  - The word's outline layer (the costly one) is cached per word and font for the session (`Words`), so a new date or BY line repaints only those layers.
  - The slab-letter outline itself moved to **`Visuals/DaterWordOutline.cs` (new, pure)**, which measures at each pixel only the letters that can reach it (`DefaultReach`), instead of every letter for every pixel.
  - With a reach of infinity it measures them all. The offline test pins the two **pixel-identical**, for APPROVED, DENIED, every drawn letter and three sizes. A second test pins the reach's own claim: past it, every letter's distance exceeds the stroke.
- **`UI/Motion/Transit.cs`, `SceneTransit.Load`, and `UI/Motion/TransitShutter.cs` (new): the in-world transition.**
  - The black veil is replaced by **the booth's roll shutter**, drawn at runtime in the cel kit: painted steel slats with inked edges, a brass bottom bar and handle.
  - It rolls down in the kit's length (`Transit.Seconds`, 0.25 s), the scene loads with `LoadSceneAsync` behind it, and it rolls up once every loaded scene has loaded and run two frames.
  - **While it is down, the hall's warm light moves through its slat gaps and glints on its faces** (`ShutterRoll.Sweep`, tested: it moves at least 1% of a screen height every 30 fps frame, under Reduced Motion too). So no frame is held without motion, which a static black hold during a multi-second load could not pass ([BP]: no frame repeated without motion over 0.25 s).
  - Under Reduced Motion it fades in and out where it hangs instead of rolling.
  - It blocks clicks while shown. Outside play mode, or for a scene that cannot load, it is `SceneManager.LoadScene`.
  - **A load asked for during another is never dropped (round 4).** It waits, a later one replacing it (`Domain/SceneChangeQueue.cs`, new, pure, tested), and loads behind the same shutter; one asked for while the shutter rises brings it down again. Before, it was ignored with a warning, while RunManager had already moved its world for it (a second cheat while the first one's office loaded), so the world and the loaded scene agreed only by luck. Queueing keeps the loaded scene in step with RunManager's world. It did not, alone, keep RunManager's routes off the office being replaced, which stayed `GameManager.Current` under the shutter: round 5's `SceneTransit.Asked` does (§1, round 5, S3). Tested in play since round 5 (`flowQueue`, `flow_shutter`).
  - **`SceneTransit.Changing` (round 4)** holds from the accepted load until the shutter is off. `SceneTransit.Loading` clears before the rise, as before (PauseMenu's Esc guard and GameManager read it).
  - **Sorting order 31999 (round 4)**, one under RUN-SAFE's `SaveNotice` (32000, "above every screen of the game"). At the same order the two root overlay canvases fell back to hierarchy order, so a failed-save strip raised just before a load (GoHomeOrAdvance's `SaveNow` then `LoadHomeScene`, OpenMorning's `SaveMorning`, `EndRunAt`) could sit under the shutter for the load and lose part of its 9 s (RUN-SAFE recipe 5). Pinned since round 6: `FlowSeamTests.TheSceneChangeShutter_SortsUnderTheSaveNotice` fails if the shutter does not sort under the notice.
  - It runs on real time (`UiMotion.Delta`), so the light catches up after a long frame. **It does not hide one:** a long frame of main-thread work (a scene's activation) holds the screen, shutter and all, for its whole length. Only a real-time measurement sees that (recipe 9, below).
  - **QA marks (new in round 2).** `TransitRunner` raises `QaSignals.Event("transit", ...)` at `start:<scene>` (the load accepted, the click's frame), `rise:<scene>` (the shutter starting up over the settled scene) and `up:<scene>` (the shutter off). They cost nothing when nothing listens, and they land in a recording's `events.tsv`.
- **`Office/OfficeScenes.cs`: the gameplay layer loads asynchronously.** Behind the shutter (`SceneTransit.Loading`), the office's gameplay layer now comes in with `LoadSceneAsync` (additive), so the shutter keeps moving while it streams. Before, it was a synchronous `SceneManager.LoadScene`.
  - The runner's settle wait (every scene in `SceneManager` loaded) covers it.
  - Without the shutter (Play in the editor, a scene SceneTransit cannot stream), it still loads on the next frame as before, so the art office never shows alone for longer than today.
- **`GameManager.cs`: theme once.** `CultureThemeService` already themes each scene as it loads. GameManager's second `RefreshActive()` now runs only when it made the run itself (an office opened on its own in the editor).
- **Live in play (round 2, after the rebase).** RUN-SAFE's `RunManager.LoadOfficeScene/LoadHomeScene/LoadTitleScene` call `SceneTransit.Load` on main, so the shutter and the async load run in every scene change in play. `Transit.cs`, `OfficeScenes.cs`, `FlowOps.cs` and `flow_shutter.json` now say so, and so do the FEATURES lines in §6.
  - 31b (the ~650 MB of hall layers) is the art-side request LIGHT files.
- **The [BP] pass is still at risk: `GameManager.Start`'s case generation runs in one activation frame.** A player sees that frame held, shutter and all, for as long as it takes. Round 1 called it "one long frame, not a held one", which was wrong. A fixed-step capture cannot see it either: `flow_load` recorded at `Time.captureFramerate = 30`, and the shutter's light steps on `UiMotion.Delta`, so `seqtools frozen` reads a multi-second activation frame as one moving frame and passes whatever the hitch.
  - **Round 2 measures it in real time.** `FlowLoadWatch` (new, rig only) times every real frame (`Time.realtimeSinceStartupAsDouble`, which `captureFramerate` does not touch) from the `start` mark to the `up` mark, with nothing recording. The ops `flowLoadClock` / `flowLoadTime` assert `load.<name>.frames` (no real frame over 250 ms) and `load.<name>.time` (shown within 1.0 s of the click) for title → office (the session's cold load), office → Home and Home → office (`flow_load.json`). The old captures moved to `flow_load_seq.json` for `frozen` and `cuts`; since round 7 each capture is its load alone (`flowRecLoad`: from the click's frame to the shutter off), and office → Home is captured too. `flow.bp.load` needs both halves.
  - **The activation work is not split here.** Splitting `GameManager.Start` across frames reorders what other components' `Start` read from it, which is the FIX_PLAN risk above. If `load.*.frames` is red on the QA player, the follow-up is to split it (the day's case generation over frames behind the shutter, with the shutter's settle wait extended to cover it), and `flow.bp.load` stays red until then.

### BUG-32a: the transition kit; the last traveller's payoff before the report
- **`Domain/TransitCurve.cs` (new, pure).** The kit's ease (a cubic smoothstep: it leaves and lands at rest) and its length, clamped to 0.2-0.3 s.
- **`UI/Motion/Transit.cs`.** `Fade`, `Slide` and `Wait`:
  - They play on real time (`UiMotion.Delta`, so hit-stop never stretches them and a recording steps them at 30 fps) over `Transit.Seconds` (`MotionTuningSO.transitSeconds`, 0.25 s) or the given length.
  - A new transition of the same target replaces the running one; a destroyed target ends its transition without `done`.
  - Reduced Motion cuts a slide; fades and waits keep their length. Outside play mode they cut at once (the tests, the builders).
- **Adopters (not FLOW's files):** UI-PC (PC open and close, 32c), HOME (panels and Sleep, 32d), CAM (32b), CONTENT (DayFlowUIController's report, briefing and Start Shift, 32e).
- **`GameManager.cs`: the payoff first.** `HandleDayCompleted` does the day's bookkeeping and the save at once. Then `AfterThePayoff` waits until the last traveller has left (`DeskAvailability.IsDeparting`, at most `MotionTuningSO.payoffWaitSeconds`, 8 s of real time: `ShiftEnd.PayoffDone`). It then ends their linger and bubble, rolls the booth shutter down (G4, below) and shows the report. Before, the report cut in over the ribbon, reaction and walk-off.

### The bible's G3, G4, J5 and J6 (FLOW's feel rows): new in this round
The feel cards are written, planned with their evidence recipes: `R7\FIX\FEEL\G3.md`, `G4.md`, `J5.md`, `J6.md` (bible §5; INTEGRATION owns the index). Each is expected **red** today. The blockers are named in each card and in §4.
- **G3 / G4, the booth's roll shutter.**
  - **`Domain/ShutterRoll.cs` (new, pure, tested):** 1.2 s, up OutCubic, down InCubic; the cue ids `booth_shutter_open` and `booth_shutter_close` (both already in SoundBank_Default; FEEDBACK may fold them into `SoundCues`).
  - **`Office/BoothShutter.cs` (new):** attached by GameManager. It rolls the office's object named `BoothShutter` up as the shift opens (`OpenShift`, the first AVAILABLE press: G3) and down once the last payoff has played, ending on the clang before the report (`AfterThePayoff`: G4).
  - **`MotionTuningSO.shutterSeconds`** (1.2) is in `MotionTuning_Default`.
  - **The office has no shutter object yet**, so today nothing rolls or sounds, and the report follows the payoff at once.
  - **Art request** (the desk set, DESK-PLAN's Blender pass): a roll shutter over the glass divider, named `BoothShutter`, placed shut, rolling up along its parent's up by its height, behind a header or drum that hides it.
- **J5 (the day transition).**
  - The shutter veil above, with the light that never stops.
  - The `day_start` sting already plays as the morning paper drops (DayFlowUIController, CONTENT's), but the bank has no clip for it.
  - The row says "a fade through black, 250 ms". FLOW proposes the shutter in its place (§5); Saleh approves or not.
- **J6 (endings).** The evening edition with Title and Quit (56a, below). The destruction moment (rumble, 120 ms hit-stop, the warning) is TIMELINE-FAIL's and HALL-FX's. `mus_title` has no player and no clip.
- **G4's other halves:** the bell on every finish (72, below), the last-hour alarm (`ShiftBells`). The report's printed-sheet arrival is CONTENT's (32e).

### BUG-28b (CloseNow) and BUG-56a: failure at the day's end; the evening edition
- **`Domain/ShiftEnd.cs` (new, pure).**
  - `OnClosing(travellerAtDesk, verdictShowing)`: closing finishes the slot while a traveller stands at the desk **or a decided one's verdict (a citation slip) still shows**, so the report is never drawn over the slip.
  - `PayoffDone(...)` and `EndsTheRun(endingId)`.
- **`GameManager.CloseNow(string endingId)` (public; TIMELINE-FAIL's Q4 = A).**
  - It sets the ending (blank keeps the one set) and, while the shift runs, closes it now: the clock finishes (the bell rings, `Closed`), the sign goes off, and nobody else is called.
  - A traveller at the desk or a showing verdict is finished first; then the payoff, the report and the ending.
  - Before the shift starts, the ending waits for the day's end.
- **`GameManager.CaseEnd.cs`.** A verdict that meets an ending no longer loads the title as its slip closes. It sets and saves the ending, shows the verdict, and calls `CloseNow(ending.id)`.
- **`GameManager.cs`.** `HandleDayCompleted` routes by `ShiftEnd.EndsTheRun(world.endingId)`: an ending set during the shift (a verdict, a cheat) or at its close goes to the ending after the report, never Home. Closing is handled once a day (`_closingHandled`), whichever comes first: closing time, End Shift, CloseNow, or the queue run out.
- **`Domain/EveningEdition.cs` (new, pure) and `Endings/EveningEditionView.cs` (new).** When the Title loads after a run ended in this session, the run's last paper is laid over it:
  - the morning paper's masthead and "Edition of day N";
  - the ending's name as the headline over its picture and story (the pet's name put in, the closing card set apart);
  - the pet's fate in Home's own words: the Welfare Office's line when it was taken, else its four needs; never a number;
  - MENU fades it away onto the Title's own ending page; QUIT GAME quits.
  - It is built at runtime in the Title's own faces, from existing strings (`briefing.masthead`, `book.edition`, `taskbar.start`, `startmenu.quit`), so neither the Title scene nor the string table changes. A Title opened at boot over an old ending shows its own page only.
- **D12's threshold is CONTENT's** (`GameConfig_Default.firedAtStability`, 60 today; v2 CONTENT delta: "the threshold sits at the destruction point", proposal stability 0). FLOW's code reads it as is. "No firing at 70% corruption" passes only once CONTENT's value lands (#17). The warning near the destruction point is HALL-FX's fallback (`SetWarning`), then TIMELINE-FAIL's.
- **Not FLOW's:** the night's ending check after `AdvanceNight` (56b, RUN-SAFE, `RunManager.cs`) and the check at Home open (56c, HOME).
- **Art request:** `Ending_PetTaken` and `Ending_WorldReport` have no picture (Fired and Bankrupt do). The edition lays out without one.

### BUG-72: "24:00" and the bell on an early finish
- **`Domain/ShiftClock.cs`.** `Format(1440)` is "24:00" (a later minute still wraps). `Stop()` is replaced by `Finish()`: when the shift ends before closing time, the clock stops at its minute, closes and raises `Closed` once, so `FeelDirector`'s closing bell rings on an early finish too. Nothing happens if the shift never started or already closed.
- **`Shift/ShiftClockDriver.cs`.** `StopShift()` is `Clock.Finish()`. **`Domain/ShiftHours.cs`:** the comment says "24:00".

### BUG-38b: every dater impression carries its verdict word
- **`Domain/DaterRubber.cs` (new, pure).** The rubber is bureau equipment, cut in the bureau's English. It prints the culture's verdict word when every letter can print (the rubber's own slab capitals, or the date font's glyphs), and otherwise APPROVED or DENIED. Greek, Arabic, Japanese and Chinese eras printed a blank word before.
- **`UI/Forms/DaterImpressionArt.cs`: the verdict is passed in, never read back from the ink** (new in this round). Before, the fallback word was chosen by comparing the ink's RGB to `ApprovedInk`, so a tinted or culture ink would have printed DENIED for an approval.
  - **The new overload `Paint(word, approved, wordInk, ...)`** takes the verdict, and the verdict alone picks the English word.
  - **The colour-only overload `Paint(word, wordInk, ...)`** stays for DeskStampTray's calls (STAMP's file). It maps the two verdict inks exactly and **refuses any other ink** with an `ArgumentException`, never guessing.
  - **Hand-off to STAMP:** `DaterImpressionArt.Paint(Word(approved), approved, Ink(approved), ...)` at DeskStampTray's three calls; INTEGRATION folds it with STAMP's own DeskStampTray changes. INTEGRATION removes the colour-only overload once nothing calls it.

### BUG-70b: the clock tooltip (not closed here; INTEGRATION must assign it)
- **The bug.** The desk clock's tooltip copies the readout's text when clicked (`DeskReaction`: `tooltip.value` = "{0}" of the readout at the click) and stays up 2.5 s while the readout moves on (17:28 against 17:29).
- **Why FLOW does not fix it.** FLOW's `ShiftClockReadouts` is the one source of the time and is unchanged. Nothing in FLOW's files can refresh a shown tooltip: the callout is FEEDBACK's `OverlayCallout`, and the reaction is DESK-PLAN's builder data.
- **The fix the BUGLIST names:** "drop the clock tooltip (it duplicates the readout)". It is DESK-PLAN's and takes two edits, **both needed**:
  1. `OfficeSceneUIBuilder.Desk.cs`: `EnsureDeskReaction("Reaction_Clock", ReactionKind.None, "tooltip.value")` → `""`;
  2. `Assets/Data/Config/DeskReactions/Reaction_Clock.asset`: `tooltipKey: tooltip.value` → `tooltipKey: `.

  `EnsureDeskReaction` returns an existing asset untouched, so the builder line plus a Build Office UI alone would **not** change the shipped asset. The DESK-PLAN branch (`fix/a-desk-plan`) has neither edit.
- **FLOW's part:** the static gate check **`flow.clock.tooltip`** (FlowChecks) reads `Reaction_Clock.asset`. It is green when the tooltip key is blank or the asset is gone, and red today.
- **INTEGRATION's part:** assign 70b to DESK-PLAN in the register, and carry `flow.clock.tooltip` as known red until then (§1, the known-red list).
- FEEDBACK's 70a (tooltips follow hover) shortens the window but does not remove it.

### BUG-65k, 73c, 73f: hygiene
- **65k, `Timeline/TimelineService.cs`.** The nightly resolve's ~80 trace lines go through `Trace`, `[Conditional("TIMEDESK_TRACE_TIMELINE")]`: compiled out with their arguments unless that symbol is defined. Errors still log.
- **73c, `MotionTuning_Default.asset`.** The two knobs MotionKnobs no longer has (`deskStretchPerSpeed`, `deskSquashPerAccel`) are removed. The live ones are added: `transitSeconds`, `payoffWaitSeconds`, and in this round `shutterSeconds`. DOC-HANDLE keeps its own 73c part (the duplicate `paperSlideSeconds`, MotionKnobs' dead knobs).
- **73f, `GameManager.cs`.** "no ShiftClockDriver wired" was logged when the investigation UI was missing; it now logs when the clock is missing.

### Round 4, S2: a cheat's close is the day's only close
- **The bug (came with the round 2 rebase).** RUN-SAFE's `GameManager.CloseShiftForCheat` (`GameManager.Cheats.cs`) is the first route step (`RunStep.CloseShift`) of `RunManager.SkipDay` and of a forward `JumpToDay`. It clears `_travellerAtDesk` and `_shiftRunning`, then calls `shiftClock.StopShift()`. On main that was the silent `Clock.Stop()`; FLOW made it `ShiftClock.Finish()` (BUG-72), which raises `Closed` once the clock has started (after the first AVAILABLE press). So:
  1. `HandleShiftClosed` ran (`_closingHandled` was false), and `ShiftEnd.OnClosing` picked `orchestrator.CloseNow()`, which released the day loop's wait.
  2. The old office, still alive behind the shutter (the load is async), raised `OnDayCompleted` on its next check.
  3. `HandleDayCompleted` ran on `_worldState`, the same object as `RunManager.World` (an office route changes that world in place), which the route had already closed, turned to the landing morning and saved.
  4. It repeated `DayCycle.CloseShift` (no once-only guard: strandings, the instalment charged again to `money` and `clerkDebtPaid`, the dialogs' outcomes), wrote a second statement row under the landing day, set `phase = Home`, reset the landing day's tomorrow modifiers and overwrote the morning save. A Continue then went to Home and skipped the landing day's shift.
  - The same happened a moment later with a verdict ribbon showing (`FinishCurrent`), and on the last slot even without `Closed` (the ribbon's end resolved the slot and the queue ran out).
- **The fix (FLOW's files only, no change to RUN-SAFE's).**
  - **`Domain/ShiftEnd.cs`: `LeftBehind(shiftRunning, sceneChanging)`** (pure, tested: `FlowRulesTests`): an office's shift is no longer its own to close when it is not running (a cheat's route closed it, or End Shift showed its ending at once) or a scene change is replacing the office.
  - **`GameManager.cs`: `ShiftLeftBehind`** (`ShiftEnd.LeftBehind(_shiftRunning, SceneTransit.Loading)`). `HandleShiftClosed` and `HandleDayCompleted` return at once when it is true, each with one log line. `HandleDayCompleted` checks it before its "Entering HandleDayCompleted" line, so the line counts the day ends really handled.
  - The scene-change half also covers a **restart or a jump back** with a verdict ribbon on the last slot: the old office's day loop ended under the shutter and its `ResetTomorrowModifiers` and `SaveNow` hit `RunManager.World`, the restored morning.
  - **`BeginShift`** sets `_shiftRunning` before `orchestrator.StartDay`, so a day loop that ends at once (a slot resolved inside `OnCaseSlotStarted`) is never taken for a shift left behind.
  - Unchanged: `CheatEndShift` keeps `_shiftRunning` true, so its close is never taken for a shift left behind, and `CloseNow` requires it. Both close in the same order (`GameManager.Cheats.cs:109-112`, `GameManager.cs` `CloseNow`): `shiftClock.StopShift()` first. Once AVAILABLE has started the clock, `ShiftClock.Finish` raises `Closed`, and that `HandleShiftClosed` is the day's close; the explicit `HandleShiftClosed()` call that follows is then the no-op (`_closingHandled`). The explicit call closes the day only when the clock never started (`Finish` returns at once).
- **Still true, for RUN-SAFE and Saleh:** a skip or a forward jump from a started shift rings the closing bell as the shutter comes down (`Finish` raises `Closed`, and FeelDirector rings on it). FLOW keeps it: the shift does end. If it is unwanted, `CloseShiftForCheat` needs a silent halt (a RUN-SAFE line), and FLOW would add one to `ShiftClockDriver`.
- **QA:** `flow_skip.json` (recipe 13): two fresh runs of the golden seed, a Skip day from day 1's briefing, then one mid-shift (AVAILABLE pressed, a traveller at the desk). The landing day's day, phase, money, `clerkDebtPaid`, statement rows and save must match (`flow_skip.same.midshift`; the saves compared without what presenting a traveller may mark: a premade met, a returner, the guide), and until the landing day's own report exactly one day end is handled, day 2's (`flow_skip.dayEnds.midshift`). Gate check **`flow.skip.midshift`** (FlowChecks, Play) needs both, and round 5's two (below), each by its exact id (round 6). `day_jumps` and `flow_night` jump at the briefing, where `Finish` returns early, so nothing covered this before.

### Round 4, S3: the QA rig waits for the shutter
- **The race (new with the async load going live).** A cheat changes `RunManager.World` at once; `TransitRunner` then lowers the shutter for `Transit.Seconds` before `LoadSceneAsync`, so the old office, its briefing still up, stays loaded for 0.25 s plus the load. QA-RIG's `waitoffice` (`QaRunner.cs:278`) checks `QaGame.OfficeLoaded()` (any GameManager), `QaGame.Day()` (the run's world, already the new day) and `BriefingShown()` (the old briefing), so it returned at once.
  - `flow_night` recipe 7 pressed the old office's Start Shift under the shutter, then waited for a traveller in an office about to be replaced.
  - RUN-SAFE's `day_jumps.json` (on main) takes its stills and probes right after `waitOffice` at all 8 landings, so they could capture the shutter or the old office.
- **FLOW's part (done).** `SceneTransit.Changing` (above, BUG-31a). FLOW's op **`flowSettled`** (f timeout, 120 s) waits until it is false; `flow_night` has it after `jumpDay 12` and after `RestartShift`, and `flow_skip` after each scene change (except, on purpose, between the double skip's two skips: round 5).
- **QA-RIG's part (handed to INTEGRATION, §4):** the same condition in `waitoffice` and `waitscene`, so every scenario (`day_jumps` included) waits for the shutter without a FLOW op.

### Round 5, S3: the office being replaced is not the live shift
- **The bug (round 4's fix left this path open).** The async load keeps the office being left alive behind the shutter, and `GameManager.Current` (set in `Start`, cleared only in `OnDestroy`) still named it until the new office activated: the 0.25 s lowering plus the load, seconds for the office.
  - `RunManager.Run` (`RunManager.cs:434`, RUN-SAFE's) takes `live = GameManager.Current` when `Current.World == World`, which still held after a skip (an office route changes the world in place).
  - A second Skip day (or a forward Jump) in that window ran the route's `CloseShift` step through the stale office's `CloseShiftForCheat` (`GameManager.Cheats.cs`), which has no once-only guard. `DayCycle.EndShift` then ran on day N+1's world with day N's `_ledger`, `_dayCases` and `_today`: day N's strandings, the instalment out of day N's pay and the dialogs' outcomes again, and a statement row with day N's verdicts under day N+1. A skip from day N+1's briefing closes on an empty ledger instead.
  - Round 4's `ShiftLeftBehind` guard does not cover it: it stops the office's own handlers, not RunManager's call into the office.
- **The fix (FLOW's files only, no change to RUN-SAFE's).**
  - **`UI/Motion/Transit.cs`.** `SceneTransit.Asked` (event): `SceneTransit.Load` raises it in play before anything loads, for a load that starts a scene change and for one queued behind another alike. `SceneTransit.NextWaiting` (`TransitRunner.SceneQueued`): a load asked for during the change waits to follow it, so the scene loading now is to be replaced as well. The event's listeners are forgotten as play starts (an editor without a domain reload).
  - **`GameManager.cs`.** `Start` takes `Current` only when no newer load waits (else one log line: this office is being replaced), and listens to `Asked`. `LeaveCurrent` clears `Current` when it is this office (one log line). `OnDestroy` stops listening.
  - So under the shutter `RunManager.Run` finds no live shift (or, once the latest load's office has opened, that office at its briefing, on an empty ledger), and the route's `CloseShift` step closes the landing day on an empty ledger, as from its briefing. RunManager needs no change.
  - **The cheat menu under the shutter** (from the ask until the latest load's office opens, `GameManager.Current` is null) acts as outside the office (`DevCheats.Run` and `Warning`, RUN-SAFE's):
    - Decide correctly, End Shift, Reveal faults and Tutorial answer "Not in the office.";
    - Auto-decide only sets `DevToolsState.AutoDecide` ("on, from the next shift");
    - Unlock everything only sets its switch, Give all items edits the run's world (`Edit`, "in force from the next shift"), and a Force appearance on today only sets it ("in the next shift"): none of them restarts the landing shift (no `RestartShift`);
    - Add money, Add stability and Set stability change the world but refresh no office HUD;
    - `DevCheats.Warning` sees no shift (`inShift` false), so Unlock everything, Give all items, a Force appearance on today and an Extra that restarts the shift show no restart warning; and a restart's own warning (`RunManager.RestartWarning`) counts no decisions for the office that is leaving.
  - **Not a Domain rule:** it is a static notice wired to a MonoBehaviour's static. The part that decides which load waits is `SceneChangeQueue` (pure, tested: `FlowRulesTests`); the play leg below covers the wiring.
- **QA:** `flow_skip.json` gains two fresh runs of the golden seed. In both, one traveller is decided on day 1 (`DecideCorrectly`). The record: Skip day, `flowSettled`, Skip day (`flowRunState settled`). The double skip: the second Skip day five frames after the first (the cheat step's own wait), with no `flowSettled` between (`flowRunState doubleskip`, n 1).
  - `flow_skip.same.doubleskip` compares day 3's day, phase, money, `clerkDebtPaid`, statement rows and save. The saves are also compared without the News site's back issues: each office records its morning paper as it opens, and the double skip never opens day 2's office.
  - `flow_skip.dayEnds.doubleskip`: 0 day ends handled from the first skip to day 3's briefing.
  - Expected before the fix: day 2's statement row carries day 1's verdict and pay, the money and `clerkDebtPaid` differ by day 1's instalment once the clerk's debt is known, and the saves differ.
  - `flow.skip.midshift` requires all four results by their exact ids (round 6): `flow_skip.same.midshift`, `flow_skip.dayEnds.midshift`, `flow_skip.same.doubleskip` and `flow_skip.dayEnds.doubleskip`. A run that stops in the double skip (a `flowSettled` or `waitOffice 3` timeout) leaves the last two missing, and the gate is red.

### Round 5, S4: TransitRunner's queue tested in play
- **The gap.** Only the pure `SceneChangeQueue` was tested. Nothing asked for a load while the shutter came down, while the scene loaded or while it rose, so a `LoadRoutine` that never took the queued scene (round 3's drop) or never lowered the shutter again after the rise passed every check.
- **The rig op `flowQueue`** (FlowOps; name, text three scenes, f timeout) and **a leg at the end of `flow_shutter.json`** (from the office: `TitleScene,office,TitleScene`).
  - The first scene is asked for through `SceneTransit.Load`. A frame later, while the shutter comes down, the second; as soon as the shutter starts up over the second, the third.
  - `flow_shutter.transit.queue.titleOfficeTitle` asserts: both asks came at those moments (the shutter's cover under 1 with only the first start marked; the rise marked and no up yet); the first scene never loaded (the second replaced it before any load); the shutter came all the way down again between the second scene's up and the third's rise; the third scene is loaded; `SceneTransit.Changing` and `Loading` ended false; the veil is off; and the transit marks are exactly `start:TitleScene, start:<office>, rise:<office>, up:<office>, start:TitleScene, rise:TitleScene, up:TitleScene` (`<office>` is RunConfig's art office).
  - `flow.transit.shutter` requires it with the leg's other two results, each by its exact id (round 6): `flow_shutter.transit.TitleScene`, `flow_shutter.transit.office` and `flow_shutter.transit.queue.titleOfficeTitle`; a run stopped before `flowQueue` is red. `FlowPlayTests.Shutter_ASceneChangeLoadsBehindTheShutter` plays it with the rest of `flow_shutter`.

### Gate checks and QA recipes (FLOW's)
- **`Assets/Editor/QA/Checks/FlowChecks.cs`:**
  - **static:**
    - `flow.runInBackground`: ProjectSettings has it at 0.
    - `flow.motionTuning.live`: no stale knob in MotionTuning_Default.
    - **`flow.clock.tooltip`** (new): Reaction_Clock has no tooltip key. Red until DESK-PLAN.
  - **play:** `flow.esc.office`, **`flow.esc.pc`** (new: Esc with the PC open keeps it open and Settings showing, R3), `flow.esc.home`, `flow.esc.title`, `flow.focus`, `flow.payoff`, `flow.transitions.cuts` (round 6: `flow_transitions.seq.dayend.cuts`, `.gohome.cuts` and `.sleep.cuts`, written by recipe 4), **`flow.transit.shutter`** (new: a scene change through SceneTransit brings the shutter down, loads behind it and lifts it; round 5: and the queue in play; round 6: `flow_shutter.transit.TitleScene`, `.transit.office` and `.transit.queue.titleOfficeTitle`), `flow.endings`, `flow.night.close`, `flow.night.bell`, **`flow.skip.midshift`** (round 4: `flow_skip.same.midshift` and `flow_skip.dayEnds.midshift`; round 5: and `flow_skip.same.doubleskip` and `flow_skip.dayEnds.doubleskip`);
  - **built player (demo):** `flow.bp.load`: both halves (round 2): real time, `bp.flow_load.load.<load>.frames` for `title_office`, `office_home` and `home_office` (no frame over 250 ms) and `.time` for `title_office` and `home_office` (shown within 1.0 s; `office_home`'s time is reported only), and the captures, `bp.flow_load_seq.seq.title_office.frozen`, `.office_home.frozen` (round 7) and `.home_office.frozen`, each capture its load alone (round 7, `flowRecLoad`).
  - **Every check that reads results names each one by its exact id (round 6, `FlowChecks.All`),** so a run that stopped part-way, its later results missing, is red. A pattern passed on whichever results existed (`GateResults.Judge`: at least one match, all matches pass). **Round 7:** the verdict is the pure `FlowChecks.Verdict` (an id with a `*` is refused) on what `FlowChecks.Measured` gives (the results folder and HEAD), for the single-result checks too, and `FlowSeamTests` runs every registered check on given results and checks that FLOW's recipes produce each id (§2).
- **Recipes** (`tools/qa/scenarios/`): `flow_pause.json` (now with a second Esc in the PC: `settings.pcEsc`), `flow_transitions.json` (its sequences run from before the click to the settled scene, for `cuts`), **`flow_shutter.json`** (new: office → Title and Title → office through `SceneTransit.Load`, recorded: the shutter measured on its own, apart from a run's loads; round 5: then the queue leg, `flowQueue`; round 7: each load recorded alone, `flowRecLoad`), `flow_endings.json`, `flow_night.json`, **`flow_skip.json`** (round 4: a mid-shift Skip day against one from the briefing; round 5: a double skip under the shutter against a skip, the shutter off, a skip), and for [BP] **`flow_load.json`** (round 2: the loads timed in real time, nothing recorded) and **`flow_load_seq.json`** (round 2: round 1's captured `flow_load`, renamed; round 7: each load captured alone, `flowRecLoad`, office → Home included).
- **Rig ops** (`Assets/Tests/PlayMode/FLOW/Rig/FlowOps.cs`): `flowSettings`, `flowFocus`, `flowPayoff`, `flowEdition`, `flowClosed`, `flowEarlyFinish`, **`flowTransit`** (new: a scene by name, or `office` for RunConfig's art office, loaded through `SceneTransit.Load`; asserts `transit.<name>`), and in round 2 **`flowLoadClock`** (name: starts `FlowLoadWatch` before the step that starts a load) and **`flowLoadTime`** (name, n frame budget in ms (250), f visible budget in s (0 reports only): asserts `load.<name>.frames` and `load.<name>.time`)), and in round 4 **`flowSettled`** (f timeout: waits until `SceneTransit.Changing` is false, after a reloading cheat), **`flowRunState`** (name, text: records the run's day, phase, money, `clerkDebtPaid`, statement rows and save; with text, asserts `same.<name>` against that record, the saves compared without what presenting a traveller may mark; round 5: with n 1, without the News site's back issues too) and **`flowWatchDayEnds`** / **`flowDayEnds`** (name, n, text: counts the "Entering HandleDayCompleted" lines and asserts `dayEnds.<name>`: exactly n, on the days in text), and in round 5 **`flowQueue`** (name, text three scenes, f timeout: asserts `transit.queue.<name>`; §1, round 5, S4), and in round 7 **`flowRecLoad`** (name: records the next scene change alone, its sequence `seq/<name>` from the load's start mark, the click's frame, to its up mark, the shutter off; asserts nothing).
  - **The red python test.** Before this round, all six went through a `TryRegister` wrapper, so `tools/qa/test_seqlib.py`'s scan for literal `QaRunner.RegisterOp("name"` calls saw none of them. Every `flow_*.json` failed `test_scenarios_use_known_ops` ('flowpayoff' not found, flow_endings step 12): 35 tests, 1 failure, though the first round reported it OK.
  - **The fix:** each is now a literal `QaRunner.RegisterOp("flowX", ...)` call, still registered once (`flowTransit` too).
- **UTF play tests** (`Assets/Tests/PlayMode/FLOW/Editor/FlowPlayTests.cs`, Explicit, category QaPlay): one per recipe but `flow_load` and `flow_load_seq` (QA player only; `Shutter_ASceneChangeLoadsBehindTheShutter` is new; round 4: `Skip_MidShiftLandsAsASkipFromTheBriefing`; round 5: both play their recipe's new leg, their summaries say so).
- **For INTEGRATION's known-red list** (`tools/qa/gate_known_red.txt`, not FLOW's file):
  - `flow.esc.home  flow_pause.settings.home  # BUG-34b, HOME (#14): Home has no Settings window until HOME builds one`;
  - `flow.esc.title  flow_pause.settings.title  # BUG-34b, HOME (#14): the Title has no Settings window until HOME builds one`;
  - `flow.clock.tooltip  # BUG-70b, DESK-PLAN (#5): Reaction_Clock's tooltip copies the readout until its key is blank (asset + builder)`.
  - Each leaves once its owner has merged. None of the three owners is confirmed in any register yet (round 2 checked main `4290bd6`): INTEGRATION confirms them.
  - The feel rows G3, G4 and J5 are red for the same kind of reason (the `BoothShutter` art object; the `booth_shutter_open/close`, `day_start`, `available_on` and `amb_hall_day` clips). INTEGRATION carries them in the feel index as red, with those blockers.

## 2. Tests (outside Unity)

| Check | Result |
|---|---|
| `compile_check_any.py E:\unity\NOPE-docs` | 0 errors in every project (161 warnings, the baseline); round 2, rebased: the same; round 4: the same; round 5: the same; round 6: the same; round 7: the same |
| Offline runner (`cc_docs`) | passed 3759, failed 0 (3747 before this round, +12); round 2, rebased on `4290bd6` (RUN-SAFE's tests included): passed 3788, failed 0; round 4: passed 3791, failed 0 (+3: `ShiftEnd.LeftBehind`, `SceneChangeQueue` twice); round 5: passed 3791, failed 0 (no new pure test: the round's change is wiring, §1 round 5); round 6: passed 3791, failed 0 (the new test is an Editor-side one, below); round 7: passed 3791, failed 0 (the round's tests are Editor-side ones, below) |
| QA editor runner (`tools/qa/editor_runner`, `QA_PROJECT` set) | passed 69, failed 0, 21 NeedsEditor skipped (the two new dater tests are NeedsEditor); round 2, rebased (RUN-SAFE's and QA-RIG's on main included): passed 81, failed 0, 41 NeedsEditor skipped; round 4: passed 81, failed 0, 41 NeedsEditor skipped; round 5: the same; round 6: passed 82, failed 0, 41 NeedsEditor skipped (+1: the shutter sorts under the save notice; with `VeilOrder` at 32000 it fails); round 7: passed 89, failed 0, 41 NeedsEditor skipped (+7: FLOW's checks that read results, run on given results, and the recipes' ids; each mutant in the round 7 list above turns them red) |
| `python -m unittest discover -s tools/qa -p "test_*.py"` | 35 tests, **OK** (it was 1 failure: `test_scenarios_use_known_ops`, fixed above); round 2: 35 tests, OK (`flow_load` and `flow_load_seq` use known ops, `flowLoadClock` and `flowLoadTime` included); round 4: 35 tests, OK (`flow_skip` and the `flowSettled` lines use known ops); round 5: 35 tests, OK (`flowQueue` in `flow_shutter` is a known op); round 6: 35 tests, OK; round 7: 35 tests, OK (`flowRecLoad` in `flow_shutter` and `flow_load_seq` is a known op) |

New and changed EditMode tests:
- **`PauseStateTests` (new):**
  - the scopes;
  - booth, Settings, focus and a slip released in every order leave no hold (0 leaks, BELL-SIGN's pass line);
  - a repeated request is one hold, and a request again takes the newer scope;
  - `Changed` only on a real change; `Clear`;
  - focus loss pauses only without runInBackground.
- **`FlowRulesTests` (new):**
  - every seal shape resolves and inks exactly as by name, and an unknown name inks nothing;
  - `TransitCurve` eases from rest to rest, never goes back, cuts only outside play or for no length, and keeps the kit's range;
  - `EscapeChain` order, ties and re-registration, and **(new) `KeepsTheOfficeOut`**: Esc keeps every mode (the PC, the wheel, the value, inspect, the city, the reading view, and combinations); a right-click, or the PC's own chain taking the press, does not; a drag is cancelled first; nothing under a newsletter;
  - `ShiftEnd` closing, payoff wait and ending routing, and **(round 4) `LeftBehind`**: a running shift closes its own day; one closed elsewhere, or an office a scene change is replacing, closes nothing again;
  - **(round 4) `SceneChangeQueue`:** a request during a change waits and the latest wins; taking it keeps the change going; nothing waiting keeps the scene; after `Finish` a request starts at once;
  - `DaterRubber` prints a printable word as given and falls back to the English word;
  - **(new) `ShutterRoll`:** 1.2 s, OutCubic up and InCubic down, never goes back, a roll from part-way covers the rest; the veil's light moves at least 1% of a screen every 30 fps frame for 300 frames, also under Reduced Motion, and wraps;
  - **(new) `DaterWordOutline`:** the reaching-letters outline is pixel-identical to the every-letter one (7 words × 3 sizes); the reach is past every letter's farthest serif; a word with an undrawn letter is not drawn.
- **`EveningEditionTests` (new):** the page's headline, story, card and fallback; the pet's fate in Home's words, the Welfare line, nothing without a pet.
- **`ShiftClockTests`:** `Finish` halts and closes once (the bell), and raises nothing before the start or after closing; `Format(1440)` is "24:00". **`ShiftHoursTests`:** the night shift closes at "24:00".
- **`FlowSeamTests`** (Editor side; the NeedsEditor ones run only in Unity):
  - Transit outside play cuts and waits no time, and the kit's length is in range;
  - `SceneTransit` loads as `SceneManager.LoadScene` does;
  - a seal's second request is the same texture (no repaint);
  - a Greek and an Arabic word print as APPROVED and DENIED;
  - **(new)** a tinted ink with the verdict passed prints that verdict's English outline, and an ink that is neither verdict's is refused;
  - **(new) the dater's cache hits:** the second print of the same texts reuses its layers (no repaint), and a new date reuses the word's outline;
  - **(round 6) the scene change's shutter sorts under the save notice:** `TransitRunner.VeilOrder` is less than `SaveNotice.SortingOrder` (both private constants, read by reflection). Not NeedsEditor: the offline editor runner runs it.
  - **(round 7) FLOW's checks that read results,** run as registered on given results (`FlowChecks.Measured` swapped for the call), against the test's list of each check's exact results (not NeedsEditor): each passes on its results and ignores a failing result beside them; each is red with any one missing; `flow.skip.midshift` with only the midshift pair is "not measured", with all four green; `flow.bp.load` is red when one result was measured in the editor; `Verdict` refuses a pattern and a nameless check and judges by commit; the list and the registry agree; and every listed result id is produced by a step of FLOW's recipes, each `frozen` one by a `flowRecLoad` sequence (reads `tools/qa/scenarios/flow_*.json`: `QA_PROJECT` outside Unity).

## 3. RESULT

**Code done; deferred: Unity.** FLOW runs code-only, and another agent holds the Unity lane.
- No scene, generated asset or builder changed, so no Build Office UI or Generate World is needed for FLOW. `MotionTuning_Default.asset` gains `shutterSeconds: 1.2`, an Inspector knob.
- Every recipe below runs in this worktree's editor by job (`Library/ClaudeJobs/<name>.job`), never while Saleh is on the PC.
- **They run on the rebased tree (main `4290bd6` + FLOW).** RUN-SAFE's loads go through the shutter there, so async loading is live in play and can reorder Awake/Start (FIX_PLAN's risk). Run recipe 2 (smoke) and recipe 11 (a full played day) first; any new console error or missing reference after a load is FLOW's to fix before the rest.

1. **EditMode suite in Unity** (TestRunnerApi job). **Pass:** green except the known UnitySkills failure, including `FlowSeamTests`' seven NeedsEditor tests and `PauseStateTests`, `FlowRulesTests` and `EveningEditionTests`.
2. **Smoke** (`QaJobs.Smoke`). **Pass:**
   - `harness.txt`'s first line is `ok` or `ok (N known red: ...)`, with nothing but the existing known red plus FLOW's three proposed lines;
   - 0 console errors;
   - no `[TimelineService]` line in the run's console.
3. **Pause and Esc** (`QaJobs.Scenario`, `job_scenario.txt` = `flow_pause.json`; or `FlowPlayTests.Pause_EscReachesSettings_AndTheClockHolds`). **Pass lines in `run.json`:**
   - `flow_pause.settings.office` ok: "Settings shown True, clock X -> X over 3 s";
   - **`flow_pause.settings.pcEsc` ok** (R3): "Settings shown True, clock X -> X over 1 s" after a second Esc with the PC open (the PC did not close);
   - `flow_pause.focus` ok: "held True, audio paused True, clock X -> X, released True";
   - `flow_pause.settings.home` and `.title`: red until HOME (known red), green after HOME;
   - stills `office_settings`, `home_settings`, `title_settings`: Settings in front, read by eye.
   - **By hand (R3), the office:**
     - Esc with the wheel open, in inspect mode, in the city view and in the reading view: each time the PC opens with Settings in front, and the mode is not left first.
     - A right-click in each mode backs out of it.
     - Esc while dragging a document cancels the drag (no Settings on that press).
     - Esc with the PC's Start menu open closes the menu only.
     - **Pass:** all as listed, 0 console errors.
4. **Transitions** (`flow_transitions.json`). **Pass:**
   - `flow_transitions.payoff` ok: "report shown True, ... report over a leaving traveller False";
   - `python tools/qa/seqtools.py cuts <run>/seq/dayend --result-dir Library/QA/results --owner FLOW --commit <c>`, and the same for `gohome` and `sleep` (seqtools takes one sequence per call), with `<c>` the commit the run measured (the `commit` of its imported results, such as `Library/QA/results/flow_transitions.payoff.json`; add `--dirty` if those are dirty). The harness's own analysis runs only `all` (flicker, flash, latency), so without these three calls `flow.transitions.cuts` stays "not measured". **Pass:** no single-frame full-screen change in any of the three (`flow_transitions.seq.dayend.cuts`, `.gohome.cuts`, `.sleep.cuts`), and `flow.transitions.cuts` green;
   - `frozen` is not read on `gohome` and `sleep` (round 7): each starts before the click and ends on the settled scene, for `cuts`, and the report before the click and the settled scene after the shutter may hold still. The loads' frozen is read over each load alone: recipe 5 (`flow_shutter`) in the editor, recipe 9b (`flow_load_seq`, Go Home's and Sleep's loads included) on the QA player;
   - by eye on `seq/dayend`: the ribbon, reaction and walk-off are fully visible before the report's first frame.
5. **The shutter veil** (`flow_shutter.json`, or `FlowPlayTests.Shutter_ASceneChangeLoadsBehindTheShutter`; `SceneTransit.Load` directly, the path RunManager's loads take). **Pass:**
   - `flow_shutter.transit.TitleScene` and `flow_shutter.transit.office` ok: "shutter seen down True, '<scene>' loaded True, shutter up and off True, still loading False";
   - **(round 5)** `flow_shutter.transit.queue.titleOfficeTitle` ok: "'<office>' asked for while the shutter came down True (cover 0.xx), 'TitleScene' never loaded True; 'TitleScene' asked for while it rose True, the shutter down again True; 'TitleScene' loaded True; change over True, veil off True; marks [start:TitleScene, start:<office>, rise:<office>, up:<office>, start:TitleScene, rise:TitleScene, up:TitleScene]" (`<office>`: RunConfig's art office). A False on either "asked for" means a frame ran longer than the shutter's 0.25 s roll, so the moment was missed: re-run before judging the queue;
   - `flow.transit.shutter` green: all three results above (round 6);
   - `python tools/qa/seqtools.py frozen <run>/seq/office_title` and `.../title_office`: longest still run ≤0.25 s (round 7: each sequence is its load alone, from the start mark to the shutter off, `flowRecLoad`, so a still run in it is the load's); `seqtools.py cuts` on both: none. Read from seqtools' output: no gate reads these two analyses (the [BP] frozen line is `flow.bp.load`'s);
   - by eye on the sequences: the shutter rolls down in about 8 frames, holds with the light moving through its slats, and rolls up over the new scene;
   - the stills read as the booth's shutter (painted slats, inked edges, brass bar), not a placeholder: the director's eye, for Saleh's call on J5 (§5).
   - Repeat with Reduced Motion on: a fade where it hangs, the light moving slower, still no frozen run.
6. **Endings** (`flow_endings.json`). **Pass:**
   - `flow_endings.payoff` ok (the fired verdict's payoff before the report);
   - `flow_endings.edition` ok: "edition True, Title True, Quit True";
   - by eye on `stills/fired_report` and `stills/edition`: the report (never Home), then the evening edition with the Fired picture, the pet line, MENU and QUIT GAME;
   - then by hand (cheat panel): SetStability 25, decide a traveller wrongly. **Pass: no firing while CONTENT's value is the destruction point** (re-run at CONTENT #17; at today's 60 it fires, which is CONTENT's value, not FLOW's code);
   - each other ending by cheat (SetStability for Fired; AddMoney -1000 then Sleep for Bankrupt; JumpToDay 15 and play to its end for the world report): the edition, then MENU lands on the Title's ending page.
7. **Night shift** (`flow_night.json`, about 10 minutes). **Pass:**
   - `flow_night.closed.midnight` ok: "closed True, the clock reads 24:00";
   - `flow_night.bell.early` ok: "Closed raised 1 time(s) ... before closing True";
   - the bell heard on an audio capture of the `early` sequence;
   - **the presses reached the new office (round 4):** `flow_night.presses` ok with 0 blocked. The recipe waits for the shutter (`flowSettled`) after `jumpDay 12` and after `RestartShift`; without it, `waitOffice` passed on the old office still loaded under the shutter, and `startShift` pressed its Start Shift directly (S3).
   - **RUN-SAFE's `day_jumps.json` needs the same wait** at all 8 landings: it comes with QA-RIG's `waitoffice` line (§4, the QA-RIG row). INTEGRATION re-runs `day_jumps` at FLOW's merge only after that line lands, else its stills and probes may catch the shutter or the old office.
8. **Dater words** (cheat JumpToDay to an el, ar, ja and zh era day and en; stamp APPROVED and DENIED on a passport each). **Pass:**
   - every impression shows its verdict word (APPROVED/DENIED where the culture's word cannot print);
   - ink in the word band ≥80% of the en baseline;
   - read by eye on stills;
   - the APPROVED and DENIED outlines look as before this round (DaterWordOutline is pixel-identical offline; this is the in-game confirmation).
9. **Load [BP]** (after `QaJobs.BuildQaPlayer` on the rebased tree, committed, so the results are not dirty). Two halves; `flow.bp.load` needs both.
   - **a. Real time, the line a capture cannot see.** `python tools/qa/run_player.py tools/qa/scenarios/flow_load.json`, five times (a fresh player each time, so title → office is the cold load). Nothing records during the loads. **Pass lines in each run's `run.json` (imported as `bp.flow_load.load.*`):**
     - `flow_load.load.title_office.frames`, `.office_home.frames` and `.home_office.frames` ok: "worst real frame N ms of M frames from the click to the shutter off, 0 over 250 ms; shutter off True", on every one of the five runs;
     - `flow_load.load.title_office.time` and `.home_office.time` ok: "shown X s after the click (the shutter starting up) ... budget 1.0 s", on every run (FIX_PLAN's median of 5 then holds too; write the five X values and their median into the result note);
     - `.office_home.time` is reported only (FIX_PLAN gives no budget for it).
     - A red `.frames` is the activation frame (§1, BUG-31a): the follow-up is splitting `GameManager.Start`'s case generation across frames, not a looser line.
   - **b. The captures.** `python tools/qa/run_player.py tools/qa/scenarios/flow_load_seq.json` once, then on its run folder `<run>`, with `<c>` the QA player's commit from `qa_build.json`:
     - `python tools/qa/seqtools.py frozen <run>/seq/title_office --result-dir Library/QA/results --bp --owner FLOW --commit <c>`, and the same for `office_home` and `home_office`. **Pass:** "longest still run" ≤0.25 s on all three (`bp.flow_load_seq.seq.title_office.frozen`, `.office_home.frozen`, `.home_office.frozen`). Each sequence is its load alone (round 7, `flowRecLoad`): it starts on the click's frame and ends as the shutter goes off, so neither the Title before the click nor the settled scene after the shutter is read;
     - `python tools/qa/seqtools.py cuts <run>/seq/title_office`, `office_home` and `home_office`: none (from the click's frame on; the frame before the Go Home and Sleep clicks is in recipe 4's `gohome` and `sleep`).
   - **c. The gate:** `flow.bp.load` green: each of its eight ids (round 7: `.office_home.frozen` added) reads "1 result(s): 1 pass, 0 fail, 0 stale, 0 dirty, 0 not on a built player" (round 6: a run that stopped after `title_office` is red, its later loads missing).
   - 31b's hall layers are listed as a known art fallback if not yet landed.
10. **Booth shutter (G3, G4)** once the art's `BoothShutter` object exists. **Pass:**
    - the first AVAILABLE press starts the clock on that frame and the shutter rolls up over 1.2 s (36 frames, fast then settling);
    - at the day's end, after the walk-off, it rolls down over 1.2 s (slow then landing) with `booth_shutter_close` on the landing frame, and the report's first frame comes after it;
    - without the object, the report follows the payoff with no extra wait (today).
11. **Clean console** (one played day, `QaJobs.DaySequence` to day 2). **Pass:** 0 stack-traced `[TimelineService]` logs; no "no ShiftClockDriver wired" warning (the office has a clock).
12. **Clock tooltip** (after DESK-PLAN's two edits). **Pass:**
    - `flow.clock.tooltip` green;
    - by hand, a click on the desk clock shows no tooltip.
13. **A day is closed once** (`flow_skip.json`, or `FlowPlayTests.Skip_MidShiftLandsAsASkipFromTheBriefing`; round 4, S2, and round 5, S3). **Pass:**
    - `flow_skip.same.midshift` ok: "midshift: day 2, phase Office, money M, clerkDebtPaid C, statement rows R, ...; briefing: the same ...; saves match". The same save apart from what presenting a traveller marks: the saves are compared without a premade met, a returner come back and the guide's progress (`flowRunState`). Before the round 4 fix: phase Home, one statement row more and the landing morning's save overwritten, so the saves differ. No instalment shows in this leg: nobody is decided in it, so `ClerkAccountSource.TakeInstalment` works from a ledger pay of 0 (and on days 1-2 the clerk's debt may not be known yet);
    - `flow_skip.dayEnds.midshift` ok: "1 day end(s) handled (expected 1), on day(s) [2]" (the landing day's own End Shift). Before the round 4 fix: 2, on [2, 2] (the office left behind closed the landing day's world again);
    - `flow.skip.midshift` green (its four results, the round 5 leg's included, round 6); 0 console errors; the console shows "Closing time of day 1 ignored" once, as the mid-shift skip's clock closes;
    - the still `d2_after_midshift_skip`: day 2's briefing, the shutter off;
    - **(round 5)** `flow_skip.same.doubleskip` ok: "doubleskip: day 3, phase Office, money M, clerkDebtPaid C, statement rows R, ...; settled: the same ...; saves match (back issues aside)", the saves compared without what presenting a traveller marks and without the News site's back issues (the double skip never opens day 2's office, so day 2's paper is never recorded). Expected before the round 5 fix: day 2's statement row carries day 1's verdict and pay, the money and `clerkDebtPaid` differ by day 1's instalment once the clerk's debt is known, and the saves differ;
    - **(round 5)** `flow_skip.dayEnds.doubleskip` ok: "0 day end(s) handled (expected 0)";
    - **(round 5)** the console shows "A scene change replaces day 1's office: it is no longer the live shift" as each run's first skip asks for its load, and the still `d3_after_double_skip` shows day 3's briefing, the shutter off.
    - Then by hand: the same with a verdict ribbon showing on the last traveller (DecideCorrectly, then Skip day at once), and with Restart shift in the same moment: no second "Entering HandleDayCompleted", and Continue from the Title lands on the landing day's briefing.

## 4. What other tracks must know

| Track | What |
|---|---|
| RUN-SAFE | Done on main: `RunManager.LoadOfficeScene/LoadHomeScene/LoadTitleScene` call `SceneTransit.Load(scene)`, so the shutter and the async load are live in play (FLOW is rebased on it). **The cheats and FLOW's closing (round 4):** (1) End Shift (`CheatEndShift`, `GameManager.Cheats.cs:109-112`) calls `shiftClock.StopShift()` first: once AVAILABLE has started the clock, `ShiftClock.Finish` raises `Closed`, and that `HandleShiftClosed` is the day's close; End Shift's own `HandleShiftClosed()` call that follows is the no-op (`_closingHandled`). Its own call closes the day only when the clock never started. (2) A skip or a forward jump: `CloseShiftForCheat` clears `_shiftRunning`, then `StopShift()` is FLOW's `ShiftClock.Finish()`, which raises `Closed` once the clock has started. FLOW's `HandleShiftClosed` and `HandleDayCompleted` ignore a shift that is no longer running or an office a scene change is replacing (`ShiftEnd.LeftBehind`), so the route's `DayCycle.EndShift` stays the day's only close and the landing morning's save is never overwritten (§1, round 4, S2; `flow_skip`). (3) A restart or a jump back: the old office's day loop, if it ends under the shutter, closes nothing (`SceneTransit.Loading`). (4) **A second day-changing cheat under the shutter (round 5):** `SceneTransit.Load` raises `SceneTransit.Asked` as each load is asked for, and the office being replaced stops being `GameManager.Current` then (an office that opens while a newer load waits never becomes it), so `RunManager.Run`'s `live` is null under the shutter (or the latest load's office at its briefing) and the route closes the landing day on an empty ledger, as from its briefing, never the replaced office's ledger again (§1, round 5, S3; `flow_skip`'s double skip). So, under the shutter, the cheat menu acts as outside the office (§1, round 5, S3, the full list): Decide correctly, End Shift, Reveal faults and Tutorial answer "Not in the office."; Auto-decide only sets `DevToolsState.AutoDecide`; Unlock everything only sets its switch, Give all items edits the run's world ("in force from the next shift") and a Force appearance on today only sets it, none restarting the landing shift; Add money and the stability cheats refresh no office HUD; and `DevCheats.Warning` shows no restart warning for them (`inShift` false), nor counts a leaving office's decisions in a restart's. With that and the queue (a second `SceneTransit.Load` during a scene change loads next, the latest winning, never dropped), RunManager needs no change. Doc only, for RUN-SAFE or INTEGRATION: `GameManager.Current`'s summary (`GameManager.Cheats.cs:28`) can add "and null for an office a scene change is replacing". The closing bell rings on a skip from a started shift; a silent halt in `CloseShiftForCheat` would be RUN-SAFE's line if Saleh does not want it. **`day_jumps.json`** needs QA-RIG's `waitoffice` line below before its stills and probes are trustworthy (S3). Still open: decide what Settings' Quit to Title does to a mid-shift run. |
| QA-RIG | **Round 4, S3 (INTEGRATION applies it at FLOW's merge, or QA-RIG on its next change):** the rig's waits must not pass while a scene change runs. `Assets/Tests/PlayMode/Rig/QaRunner.cs:278` (`waitoffice`): `yield return QaGame.Until(() => !SceneTransit.Changing && QaGame.OfficeLoaded() && (s.n <= 0 \|\| QaGame.Day() == s.n) && QaGame.BriefingShown(),`; and `:345` (`waitscene`): `yield return QaGame.Until(() => !SceneTransit.Changing && SceneManager.GetSceneByName(s.name).isLoaded, Timeout(s, 60f), $"the scene {s.name}");`. Then `flowSettled` in FLOW's recipes is redundant but harmless. Without it, `waitoffice` passes on the office a cheat is replacing (its briefing still up under the shutter, the run's world already on the new day), which affects RUN-SAFE's `day_jumps` at all 8 landings. **Round 6, for QA-RIG's next change (not FLOW's file):** `FlowChecks.Verdict` (each result by its exact id, pure since round 7) mirrors `RunSafeChecks.All`; one shared `GateResults` helper could serve both. `QaRigChecks`' own patterns (`smoke_day1.seq.*.flicker`, `.flash`, `.latency` and the `bp_smoke` ones) have the same hole: they pass on whichever sequences were analysed. |
| UI-PC, HOME, CAM, CONTENT | Screen changes go through `Transit.Fade/Slide/Wait` (0.2-0.3 s; `Transit.Seconds`). |
| UI-PC, TEXT-LANG | Quit to Title and Quit rows in the Settings window (§1, BUG-34b): UI-PC's `.Apps.cs` builds them, TEXT-LANG's `SettingsWindowController` wires them and adds `settings.quitToTitle` and `settings.quit` to all 7 tables. |
| HOME | Build a modal `SettingsWindowController` into the Title **and** Home scenes (Home is FLOW's proposed assignment; INTEGRATION confirms). PauseMenu finds and opens it on Esc, with no code needed, and `flow.esc.home` / `flow.esc.title` turn green then. Add `mus_title` on the Title (J6). |
| DOC-HANDLE | When `OfficeControls` stops backing out on Esc (v2: Esc falls through to FLOW's Esc→Settings; right-click keeps backing out), tell INTEGRATION. It then removes `EscapeChain.KeepsTheOfficeOut`, EscapeWatch's guard and the `office` chain step. Until then, FLOW's guard keeps your component out of an Esc frame that would leave a mode. |
| DESK-PLAN | **BUG-70b (assigned by INTEGRATION):** blank `Reaction_Clock`'s tooltip key in both `OfficeSceneUIBuilder.Desk.cs` and `Reaction_Clock.asset`. **Art:** a `BoothShutter` object in the desk set (G3/G4; §1). |
| STAMP | Call `DaterImpressionArt.Paint(word, approved, ink, ...)` at DeskStampTray's three print sites. The colour-only overload refuses any ink but the two verdict inks. |
| FEEDBACK | Clips for `booth_shutter_open`, `booth_shutter_close`, `available_on`, `amb_hall_day`, `day_start`, `helix_breach`, `mus_title` (the bank has the ids, no clips); a real `shift_end_bell`. Optionally move ShutterRoll's two cue ids into `SoundCues`. |
| BELL-SIGN | The closed booth: `PauseState.Shared.Request("booth", PauseScope.Clock)` / `Release("booth")`, in `GameManager.Booth.cs`. `OpenShift` there now also rolls the booth shutter up (keep the call). |
| DIALOGUE, PORTALS | Let go first: `EscapeChain.Shared.Register("<name>", EscapeChain.TrackOrder, () => /* let go, true */)`; unregister on destroy. |
| TIMELINE-FAIL | `GameManager.CloseNow(endingId)` ends the run at the day's end (its Q4 = A). It holds `GameManager.CaseEnd.cs`, `Endings/**`, `TimelineService.cs` and `ShiftStrandings.cs` after FLOW. J6's destruction moment (rumble, 120 ms hit-stop, warning) is its own. |
| CONTENT | D12: `firedAtStability` at the destruction point (FLOW's no-firing-at-70% pass re-runs at #17). G4's printed-sheet report (32e). |
| INTEGRATION | **Confirm the owners nobody has confirmed yet:** BUG-70b to DESK-PLAN; Settings at Home and on the Title (34b) to HOME. Add the three known-red lines (§1: `flow.clock.tooltip`, `flow.esc.home`, `flow.esc.title`) and carry G3/G4/J5 as red in the feel index with their blockers (the `BoothShutter` object; the shutter, `day_start`, `available_on` and `amb_hall_day` clips). Put §5.6's right-click question to Saleh. LEASES.md's `GameManager.cs` rows now name the four partials. Fold the feel cards `R7\FIX\FEEL\G3/G4/J5/J6.md` into the index. **Fold §6's FEATURES lines into `docs/FEATURES.md`** in the merge commit (this branch no longer edits it). Apply the QA-RIG row's two `QaRunner.cs` lines (S3) before re-running `day_jumps` as regression. |

## 5. Decisions taken in FLOW (ours, not Saleh's)

1. **Settings pauses the loops, not the one-shots.** Settings holds the clock and pauses the looping beds; one-shot cues finish, so Settings' own clicks sound. The focus lost pauses all audio.
2. **The dater rubber prints the culture's word when it can, English otherwise** (BUGLIST's "fixed English, or covered for every culture"). Latin-script cultures keep their own word; the rest print APPROVED or DENIED. This matches D7's "bureau equipment is immune" reading for the PC.
3. **A run-ending verdict closes the shift at once** (`CloseNow`): the day's end comes forward (the report, then the ending), instead of the clerk working on to closing time on a run that is already over.
4. **The payoff wait is capped at 8 s** (`payoffWaitSeconds`), so the report can never be stuck behind a figure that does not leave.
5. **The evening edition is built at runtime** in the Title's own faces from existing strings, so no scene or string table changes with it. Its fallback headline for an ending without a name is "The End".
6. **Esc never leaves a mode, only Settings opens** (R3 read with D20 and v2's DOC-HANDLE line): not the PC (Saleh's words), and not the wheel, inspect mode, the city or the reading view either. A right-click backs out of them, as in Papers, Please. A drag is a hold and is still cancelled first. **For Saleh to confirm:** the right-click also closes the PC (an untaken right-click is `DesktopKeyboard`'s back-out, `KeepsTheOfficeOut` is false for it, `OfficeControls` runs `ClosePc`), though R3 names only "Q or click the bezel". This is FLOW's call, not his. If he wants Q and the bezel only, `KeepsTheOfficeOut` returns true for a right-click with the PC open (one line and its test).
7. **The scene-change veil is the booth's roll shutter, not a black fade** (J5 says "a fade through black, 250 ms"; FIX_PLAN v1 says "an in-world transition"). A static black hold cannot pass "no frame repeated without motion > 0.25 s" during a multi-second load; the shutter's moving light can, between frames (a single long frame is held whatever the veil: recipe 9a measures it). It keeps J5's 250 ms for the roll. **For Saleh's approval** with the stills of recipe 5. If he prefers black, the light can move on black instead (a code change in `TransitShutter` only).
8. **PauseScope.Full blocks no input itself.** The context does it (§1, BUG-34a), so FLOW adds no second input gate beside RUN-SAFE's.
9. **The colour-only dater `Paint` refuses an unknown ink** rather than guessing a verdict: a loud error in a test beats a silent DENIED on an approval.
10. **A load asked for during a scene change is queued, the latest winning (round 4),** not refused. RunManager assumes every `SceneTransit.Load` happens and moves its world first, so refusing would need a RUN-SAFE change and still leave the world and the scene apart; queueing in FLOW's runner keeps the loaded scene in step with the world. Queueing alone did not keep RunManager's routes off the office being replaced, which stayed `GameManager.Current` under the shutter: decision 12 does.
11. **A cheat's close of a started shift still rings the closing bell (round 4).** The guard is in FLOW's handlers (`ShiftEnd.LeftBehind`), not a silent halt in RUN-SAFE's `CloseShiftForCheat`, so the fix needs no other track's file. The bell sounds as the shutter comes down on a skip; if Saleh finds it wrong there, the halt is RUN-SAFE's one line.
12. **The office being replaced stops being `GameManager.Current` as its replacement is asked for (round 5),** FLOW-side (`SceneTransit.Asked`, answered in `GameManager.cs`), rather than RunManager taking the live office only while `!SceneTransit.Changing` (a line in RUN-SAFE's `RunManager.cs`). Both close the window (under the shutter the latest load's office, once open, is at its briefing on an empty ledger, so either way the route closes the landing day as from its briefing). FLOW's keeps the change in FLOW's files and also keeps the cheat menu's office cheats (Decide correctly, End Shift, Reveal faults) off an office that is leaving.
13. **Frozen is measured over the load alone (round 7),** never over a whole recording: a `flowRecLoad` sequence starts on the load's start mark and stops on its up mark. The other choice (saying in this file that the Title and the settled screens may hold still, and dropping `frozen` from `flow.bp.load` as round 6 did for `flow.transit.shutter`) would leave the [BP] line "no frame repeated without motion over 0.25 s" ungated on the captures. The `cuts` recordings (`flow_transitions`) keep a lead and a tail, so a cut on the click's frame or after the shutter is still seen; `frozen` is not read on them. `flow.bp.load` now reads office → Home's frozen too, so its captures cover every load its real-time half times.
14. **Every FLOW check that reads results goes through `FlowChecks.Verdict` (round 7),** the single-result ones too, so one list in `FlowSeamTests` pins every check's results and an id is never read as a pattern.

## 6. FEATURES lines (for INTEGRATION)

`docs/FEATURES.md` is **not** edited in this branch (FIX_PLAN §0 rule 2: INTEGRATION writes it; round 4 restored it to origin/main `4290bd6`). INTEGRATION folds these into it in FLOW's merge commit. Line numbers are origin/main `4290bd6`'s. Each change is an exact replacement: find the old text on that line, put the new text in its place.

**Line 14** (Endings, `EndingService`: BUG-56a, BUG-28b, the evening edition):
- Old:
```text
`NightOrder`; tested: `CheatRoutingTests`). No ending judges
```
- New:
```text
`NightOrder`; tested: `CheatRoutingTests`). A failure plays out at the day's end, never the instant a verdict lands (BUG-56a; TIMELINE-FAIL's Q4 = A): the verdict that meets it shows (its slip, if any), the shift closes at once (`GameManager.CloseNow(endingId)`: the AVAILABLE sign goes off, nobody else is called, the closing bell rings), the last traveller's reaction and walk-off play, then the shift report, and its button leads to the ending, never Home; End Shift over an open citation slip finishes that traveller's slot first, so the report never covers the slip, and a run whose ending was set during the shift ends in it (BUG-28b; `ShiftEnd`, tested: `FlowRulesTests`). After an ending reached in the session, the Title opens on the evening edition (`EveningEditionView`, built at runtime in the Title's own faces): the morning paper's masthead, "Edition of day N", the ending's name as the headline over its picture and story (the pet's name put in), and the pet's fate in Home's own words (the Welfare Office's line when it was taken, else its four needs; never a number; `EveningEdition`, tested: `EveningEditionTests`), with Menu (the paper is laid down onto the Title's own ending page) and Quit game; a Title opened at boot over an old ending shows its own page only. No ending judges
```

**Line 48** (Stamps on the papers: BUG-38b, the dater's word):
- Old:
```text
a letter they lack, the font's outline), under it today
```
- New:
```text
a letter they lack, the font's outline; the rubber is the bureau's English: a word it cannot print, a Greek, Arabic, Japanese or Chinese one, prints as APPROVED or DENIED by the verdict the stamp passes, never read back from the ink's colour (an ink that is neither verdict's is refused), BUG-38b, `DaterRubber`, tested: `FlowRulesTests`; the word's hollow outline, `DaterWordOutline`, measures at each pixel only the letters that can reach it and paints exactly what measuring every letter paints, tested: `FlowRulesTests`), under it today
```

**Line 64** (Controls, Papers, Please's one input model: R3, Esc no longer backs out):
- Old:
```text
A right-click and Esc always back out, of the innermost mode
```
- New:
```text
A right-click always backs out, of the innermost mode
```
- Old:
```text
in one order everywhere (`ControlRules.BackOut`): a drag
```
- New:
```text
in one order everywhere (`ControlRules.BackOut`); Esc only cancels a drag and otherwise opens Settings, never leaving a mode (Saleh's R3: Q or the bezel leave the PC; see Pause and Esc): a drag
```

**Line 172** (The hours grow: BUG-72, 24:00):
- Old:
```text
close at midnight, shown 00:00 (each span
```
- New:
```text
close at midnight, shown 24:00 (BUG-72; `ShiftClock.Format`; each span
```
- Old:
```text
"Desk hours: 16:00 to 00:00"
```
- New:
```text
"Desk hours: 16:00 to 24:00"
```

**Line 176** (the clock's pauses: BUG-34a) is replaced by six lines, in this order: the first replaces it, the other five are new.
- Old (the whole line):
```text
- [ ] The clock pauses only while a citation slip is shown, and the slip keeps the PC screen on until acknowledged (the slip is back since Saleh's 1007b demo; the interview, scans and camera moves take real time and cost nothing else)
```
- New:
```text
- [ ] The clock pauses while a citation slip is shown (the slip keeps the PC screen on until acknowledged; the slip is back since Saleh's 1007b demo), while Settings is open and while the game's window has lost the focus (see Pause and Esc); every pause is a named source of `PauseState` (a Clock source holds the clock only, a Full one the clock and the audio), and the clock runs only while none holds it, so one source's release never restarts a clock another holds (tested: `PauseStateTests`, in every order of booth, Settings, focus and slip). The interview, scans and camera moves take real time and cost nothing else
- [ ] Pause and Esc (BUG-34; Saleh's D20, R3/R4; `PauseMenu`, made at startup and kept across scenes; no pause card: pausing the queue is the AVAILABLE sign): Esc lets go of one thing first, if anything (`EscapeChain`, tested: `FlowRulesTests`: the desk's hold, then the PC's own Escape chain, then a drag the office's back-out cancels, then a step a later track registers), and otherwise opens Settings, never leaving a mode (R3: with the PC open, Esc brings its Settings app to the front and the PC stays open, only Q or the bezel leave it; the wheel, inspect mode, the city and the reading view stay too, a right-click backs out of them; `EscapeChain.KeepsTheOfficeOut`, tested: `FlowRulesTests`; `EscapeWatch` keeps the office's back-out off such a press): in the office the PC (by Q's rules, so not under the briefing or the report) with its Settings app in front; in a scene with a Settings window of its own (none yet on the Title and at Home: HOME's), that window; nothing while a scene loads or the cheat overlay holds the input. While Settings is the PC's front window with the PC open, the shift clock holds and the looping beds pause (one-shot cues finish). The player no longer runs in the background (`ProjectSettings` runInBackground off): losing the window's focus holds the clock and pauses all audio until it comes back, and cancels the desk's hold (`HoldRegistry`); the QA rig and the editor's job runner turn runInBackground on for their runs (the rule tested: `PauseStateTests`; the presses, the clock and the audio checked in Unity: FLOW's `flow_pause` recipe)
- [ ] The day's end waits for the last traveller's payoff (BUG-32a): their reaction and walk-off play before the shift report (up to `MotionTuningSO.payoffWaitSeconds`, 8 s), then their bubble goes; the day's bookkeeping and the save happen at once (`ShiftEnd.PayoffDone`, tested: `FlowRulesTests`; the order checked in Unity: `flow_transitions`)
- [ ] A day is closed once (FLOW rounds 4 and 5): a skip or a forward jump from the office closes today's shift through the cheat's route (`GameManager.CloseShiftForCheat`, then the route's night), and the office it leaves behind the shutter then handles neither its clock's closing nor its day loop's end; nor does an office that a restart or a jump back is replacing (`ShiftEnd.LeftBehind`, tested: `FlowRulesTests`). An office stops being the cheat menu's live shift (`GameManager.Current`, which the routes close) as soon as a scene change that replaces it is asked for (`SceneTransit.Asked`), and one that opens while a newer load waits never becomes it, so a second skip or jump asked for under the shutter closes the landing day as from its briefing, never the replaced office's day again. So a mid-shift skip lands as a skip of the same day from the briefing, and a second skip under the shutter as one after it lifted: the same money, debt paid, statement rows and phase, and the same save apart from what presenting a traveller marks (a premade met, a returning traveller come back, the desk guide's progress) and, after a second skip under the shutter, the News site's back issue of the day it passed over, whose office never opened (its Unity recipe: FLOW's `flow_skip`)
- [ ] The booth's roll shutter (the bible's G3 and G4; `BoothShutter`): shut at the day's start, it rolls up as the shift opens (the first AVAILABLE press: `MotionTuningSO.shutterSeconds`, 1.2 s, eased out, cue booth_shutter_open) and rolls down once the last traveller's payoff has played, before the report (1.2 s, eased in, ending on booth_shutter_close; `ShutterRoll`, tested: `FlowRulesTests`). It is the office's object named BoothShutter, placed shut, rolling up by its own height; the office has none yet (the desk set's shutter is an art request), so today nothing rolls or sounds and the report follows the payoff at once
- [ ] Screen transitions (BUG-32a, the bible's J5: one shared kit, `Transit`): a fade, a slide and a wait, eased in and out on real time over `MotionTuningSO.transitSeconds` (0.25 s, kept within 0.2-0.3 s; `TransitCurve`, tested: `FlowRulesTests`; a slide cuts under Reduced Motion); a scene change through `SceneTransit.Load` (every RunManager load: the office, Home and the Title) brings the booth's roll shutter down over the screen in the kit's length (`TransitShutter`, drawn at runtime in the cel kit: painted slats, inked edges, a brass bar; a fade where it hangs under Reduced Motion), loads the scene asynchronously behind it (the office's gameplay layer too, `OfficeScenes`; without the shutter it loads on the next frame as before), keeps the hall's light moving through its slats while it is down, so no frame is held without motion (`ShutterRoll.Sweep`, tested: `FlowRulesTests`; a long frame of work on the main thread, such as the office's activation, still holds the screen for that frame: the QA player's real-time load watch measures each load's worst frame against 250 ms, `flow_load`), and rolls it up once every scene it brings has loaded and run its first frames, so a load never shows a frozen frame of the scene left nor a half-built one; clicks are blocked meanwhile, and a scene asked for during a change is never dropped: it loads next behind the same shutter, the latest request winning, and one asked for while the shutter rises brings it down again (`SceneChangeQueue`, tested: `FlowRulesTests`; in play: FLOW's `flow_shutter`); the shutter sorts just under the save notice, so a failed save's strip stays readable over it; outside play mode both cut at once (tested: `FlowSeamTests`). The office loads faster (BUG-31a): each seal outline is painted once a session from its outline resolved once (`SealOutlines.Resolve`, tested: `FlowRulesTests`), each dater word's outline once a session (`DaterImpressionArt`), and each scene is themed once as it loads (GameManager themes again only an office opened on its own)
```

**Line 191** (Cues the game plays: BUG-72, the bell on an early finish), at the line's end:
- Old:
```text
and shift_end_bell at closing time
```
- New:
```text
and shift_end_bell when the shift ends: at closing time, and on an early finish (the queue ran out, End Shift, a run that ended, a skip or a jump from a started shift), once (BUG-72; `ShiftClock.Finish`, tested: `ShiftClockTests`)
```

Round 4 changed three of these against round 3's branch text: the transitions line names the queued load and the shutter's order under the save notice; the line "A day is closed once" is new; the bell line names a skip or a jump from a started shift. Round 5 changed two against round 4's: "A day is closed once" names the office that stops being the live shift and the save's exceptions (what presenting a traveller marks, a passed-over day's back issue); the transitions line says a load asked for during the rise brings the shutter down again and names `flow_shutter`.
