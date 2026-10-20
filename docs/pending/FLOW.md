# FLOW: smooth transitions, real pause, fast loads, endings that land

Branch `fix/a-flow`, cut from main `9b8ab5f` (P0: carve, cuts, QA-RIG). Sources: FIX_PLAN.md §3 "FLOW" and FIX_PLAN_v2.md "FLOW: delta (D20, D12)" (v2 wins), BUGLIST.md, DECISIONS_1019a.md (D12, D20, R3/R4), GAME_FEEL_BIBLE.md rows F3, G1, G3, G4, J5, J6, and R7\PHASEB\LEASES.md (the GameManager carve, the scoped pause, the Esc seam).

**Bugs closed here:** 28b (CloseNow honours the ending), 31a, 32a, 34a, 34b, 38b, 56a, 65k, 72, 73c (MotionTuning_Default), 73f (the clock warning). **Not closed here:** 70b (see its row: the fix is one line in DESK-PLAN's builder).

**Saleh's calls built:**
- **D20:** "if you press escape you should auto open the pc setting menu"; pausing is the AVAILABLE sign. There is **no pause card**. FLOW provides the scoped pause (`PauseState`) for BELL-SIGN's closed booth; it does not wire the sign (BELL-SIGN P2, and its question 2 is still open with Saleh).
- **R3/R4:** Esc opens the same Settings window everywhere: the PC's Settings app in the office; the scene's own Settings window at Home and on the Title.
- **D12:** "you are fired if you destroy the timeline". The run ends at the day's end, never the instant a verdict lands. The threshold value is CONTENT's (see 56a).

---

## 1. Changes by BUG id

### Carve (LEASES.md proposal, done as a pure move): commit `b8c6e1b`
`GameManager.cs` is split so each Phase B track holds its own partial instead of chaining on the root:

| Partial | What moved | Phase B holder |
|---|---|---|
| `GameManager.Booth.cs` | slot arming, the AVAILABLE sign's wiring, calling and presenting the traveller, `React` | BELL-SIGN |
| `GameManager.CaseEnd.cs` | `HandleDecision`, `ShowVerdictThen`, `HandleEndingReached` | TIMELINE-FAIL (if needed), then LANGUAGE |
| `GameManager.News.cs` | Start's archive call, extracted as `RecordTodaysPaper` | NEWS-WEB |
| `GameManager.Departures.cs` | `Departed`, `AnnounceDeparture` | PORTALS |

No serialized field moved. `carve_check.py --base origin/main --head b8c6e1b`: 14 MOVED, 60 SAME; its two reports are the extract itself (`Start` lost the archive lines, `RecordTodaysPaper` is the new member holding them, unchanged), checked by hand. LEASES.md's "if FLOW doesn't carve" chain on `GameManager.cs` no longer applies: INTEGRATION updates the register.

### BUG-34a: the clock and audio pause on focus loss and while Settings is open
- **`Domain/PauseState.cs` (new, pure).** The game's pause is a set of named sources. `Request(source, scope)` / `Release(source)`, with `PauseScope.Clock` (the shift clock only: the booth closed, a citation slip) and `PauseScope.Full` (the clock, audio and input: Settings, focus lost). `ClockHeld` is true while any source holds; `FullHeld` while a Full one does. One source's release never restarts a clock another holds, in any order (BELL-SIGN P2's requirement). Sources: `Settings`, `Focus`, `Citation`; BELL-SIGN adds its own (`"booth"`, Clock).
- **`Shift/ShiftClockDriver.cs`.** Each frame it follows `PauseState.Shared.ClockHeld` with one `ShiftClock.Pause` / `Resume` pair (so the clock's own nesting stays balanced). Its public `Pause`/`Resume` are gone: every pause is a PauseState source.
- **`GameManager.CaseEnd.cs`.** A citation slip holds the clock as the `Citation` source (was a direct `Pause`/`Resume`). `GameManager.OnDestroy` releases it, so a slip still up as the office goes (a jump, quitting) holds nothing in the next scene.
- **`UI/PauseMenu.cs` (new).** Made once at startup (`RuntimeInitializeOnLoadMethod`), kept across scenes, hidden. Holds `Settings` (Full) while Settings shows, and `Focus` (Full) while the window has lost the focus and the game does not run in the background. Audio: the focus lost pauses everything (`AudioListener.pause`); Settings pauses the looping beds (room tone, music) and lets one-shot cues finish, so Settings' own clicks sound. The focus lost also cancels the desk's hold (`HoldRegistry.Desk.OnFocusLost`).
- **`ProjectSettings.asset`: `runInBackground: 0`.** The player now pauses when it loses the focus. The QA rig and the editor's job runner set `Application.runInBackground` on for their own runs (unchanged), so a run in an unfocused editor keeps playing.

### BUG-34b: Esc opens Settings (no pause card)
- **`UI/PauseMenu.cs`.** On Esc: nothing while a scene loads (`SceneTransit.Loading`) or the cheat overlay holds the input (`InputGate.Blocked`). Otherwise it runs `EscapeChain.Shared.LetGo()`; when nothing let go, it opens Settings:
  - **Office:** the PC opens by Q's own rules (`OfficeControls.Press(ControlKey.Pc)`, and only when `PcFrame.CanOpen`, so never under the briefing or the report), with the Settings app's window in front.
  - **Home and Title:** the scene's `SettingsWindowController`, when it has one. Neither scene has one yet: HOME builds them (FIX_PLAN: HOME consumes "the PauseMenu self-install"); HOME-3D may later move Home's to the IOUPAD.
- **`Domain/EscapeChain.cs` (new, pure): the "let go first" seam (LEASES.md proposal).** Steps run lowest order first, ties in registration order; the first that lets go ends the press. FLOW's own steps:
  1. `hold` (0): the desk's hold is cancelled first (DOC-HANDLE's v2 rule).
  2. `desktop` (100): the PC's own Escape chain (a menu, the card, the search, a field: `DesktopKeyboard.EscapeTakenFrame`).
  3. `office` (150): the office's own back-out on this press (`OfficeControls` left a mode). `UI/EscapeWatch.cs` (new, execution order -1100) records the controls state before the office's pollers act, so PauseMenu (order 1000) can tell. **This step exists only while `ControlRules` still backs out on Esc** (UI-PC #13: "Esc no longer leaves the PC"; DOC-HANDLE v2: Esc "falls through to FLOW's Esc→Settings"). Once both have merged, the step never fires: INTEGRATION removes it and EscapeWatch then.
  4. Phase B tracks register their own at `EscapeChain.TrackOrder` (200): DIALOGUE's open ring, PORTALS' held cable. No lease on FLOW's handler is needed.
- Esc never changes the AVAILABLE sign (BELL-SIGN P7).

### BUG-31a: seals and daters painted once; async load behind black; each scene themed once
- **`Visuals/SealOutlines.cs`.** `Resolve(name)` looks the outline up once and returns an `Outline` that tests points without a dictionary lookup (the load spent 64% of its time on 2.7 M `OrdinalIgnoreCase` lookups). `Inked(name, x, y)` stays, through it.
- **`UI/Forms/SealArt.cs`.** Paints from the resolved outline. Its textures were already cached per shape for the session; now the one paint is cheap too.
- **`UI/Forms/DaterImpressionArt.cs`.** The word's outline layer (the costly one) is cached per word and font for the session (`Words`), so a new date or BY line repaints only those layers. `WordLayer` measures only the letters that can reach a pixel (`LetterReach`), instead of every letter for every pixel.
- **`UI/Motion/Transit.cs`, `SceneTransit.Load`.** The P0 stub is filled: the screen fades to black over the kit's length, the scene loads with `LoadSceneAsync` behind it, the black lifts after every loaded scene (the gameplay layer comes on top of the art office) has loaded and run two frames, and clicks are blocked while it is up. A second load while one runs is ignored with a warning. Outside play mode, or for a scene that cannot load, it is `SceneManager.LoadScene`.
- **`GameManager.cs`: theme once.** `CultureThemeService` already themes each scene as it loads; GameManager's second `RefreshActive()` now runs only when it made the run itself (an office opened on its own in the editor).
- **Not FLOW's:** `RunManager.LoadOfficeScene/LoadHomeScene/LoadTitleScene` still call `SceneManager.LoadScene`. RUN-SAFE (#3) routes them through `SceneTransit.Load` (CARVE's seam table). Until then the fade and the async load do not run in play. 31b (the ~650 MB of hall layers) is the art-side request LIGHT files.

### BUG-32a: the transition kit; the last traveller's payoff before the report
- **`Domain/TransitCurve.cs` (new, pure).** The kit's ease (a cubic smoothstep: it leaves and lands at rest) and its length, clamped to 0.2-0.3 s.
- **`UI/Motion/Transit.cs`.** `Fade`, `Slide` and `Wait` play on real time (`UiMotion.Delta`, so hit-stop never stretches them and a recording steps them at 30 fps) over `Transit.Seconds` (`MotionTuningSO.transitSeconds`, 0.25 s) or the given length. A new transition of the same target replaces the running one; a destroyed target ends its transition without `done`. Reduced Motion cuts a slide; fades and waits keep their length. Outside play mode they cut at once (the tests, the builders).
- **Adopters (not FLOW's files):** UI-PC (PC open and close, 32c), HOME (panels and Sleep, 32d), CAM (32b), CONTENT (DayFlowUIController's report, briefing and Start Shift, 32e).
- **`GameManager.cs`: the payoff first.** `HandleDayCompleted` does the day's bookkeeping and the save at once, then `AfterThePayoff` waits until the last traveller has left (`DeskAvailability.IsDeparting`, at most `MotionTuningSO.payoffWaitSeconds`, 8 s of real time: `ShiftEnd.PayoffDone`) before it ends their linger and bubble and shows the report. Before, the report cut in over the ribbon, reaction and walk-off.

### BUG-28b (CloseNow) and BUG-56a: failure at the day's end; the evening edition
- **`Domain/ShiftEnd.cs` (new, pure).** `OnClosing(travellerAtDesk, verdictShowing)`: closing finishes the slot while a traveller stands at the desk **or a decided one's verdict (a citation slip) still shows**, so the report is never drawn over the slip. `PayoffDone(...)`. `EndsTheRun(endingId)`.
- **`GameManager.CloseNow(string endingId)` (public; TIMELINE-FAIL's Q4 = A).** Sets the ending (blank keeps the one set), and while the shift runs closes it now: the clock finishes (the bell rings, `Closed`), the sign goes off, nobody else is called, a traveller at the desk or a showing verdict is finished first; then the payoff, the report and the ending. Before the shift starts, the ending waits for the day's end.
- **`GameManager.CaseEnd.cs`.** A verdict that meets an ending no longer loads the title as its slip closes: it sets and saves the ending, shows the verdict, and calls `CloseNow(ending.id)`.
- **`GameManager.cs`.** `HandleDayCompleted` routes by `ShiftEnd.EndsTheRun(world.endingId)`: an ending set during the shift (a verdict, a cheat) or at its close goes to the ending after the report, never Home. Closing is handled once a day (`_closingHandled`), whichever comes first (closing time, End Shift, CloseNow, the queue run out).
- **`Domain/EveningEdition.cs` (new, pure) and `Endings/EveningEditionView.cs` (new).** When the Title loads after a run ended in this session, the run's last paper is laid over it: the morning paper's masthead, "Edition of day N", the ending's name as the headline over its picture and story (the pet's name put in, the closing card set apart), and the pet's fate in Home's own words (the Welfare Office's line when it was taken, else its four needs; never a number). MENU fades it away onto the Title's own ending page; QUIT GAME quits. Built at runtime in the Title's own faces, from existing strings (`briefing.masthead`, `book.edition`, `taskbar.start`, `startmenu.quit`), so neither the Title scene nor the string table changes. A Title opened at boot over an old ending shows its own page only.
- **D12's threshold is CONTENT's** (`GameConfig_Default.firedAtStability`, 60 today; v2 CONTENT delta: "the threshold sits at the destruction point", proposal stability 0). FLOW's code reads it as is. "No firing at 70% corruption" passes only once CONTENT's value lands (#17). The warning near the destruction point is HALL-FX's fallback (`SetWarning`), then TIMELINE-FAIL's.
- **Not FLOW's:** the night's ending check after `AdvanceNight` (56b, RUN-SAFE, `RunManager.cs`) and the check at Home open (56c, HOME).
- **Art request:** `Ending_PetTaken` and `Ending_WorldReport` have no picture (Fired and Bankrupt do). The edition lays out without one.

### BUG-72: "24:00" and the bell on an early finish
- **`Domain/ShiftClock.cs`.** `Format(1440)` is "24:00" (a later minute still wraps). `Stop()` is replaced by `Finish()`: the shift ends before closing time, the clock stops at its minute, closes and raises `Closed` once, so `FeelDirector`'s closing bell rings on an early finish too. Nothing happens if the shift never started or already closed.
- **`Shift/ShiftClockDriver.cs`.** `StopShift()` is `Clock.Finish()`. **`Domain/ShiftHours.cs`:** the comment says "24:00".

### BUG-38b: every dater impression carries its verdict word
- **`Domain/DaterRubber.cs` (new, pure).** The rubber is bureau equipment, cut in the bureau's English: it prints the culture's verdict word when every letter can print (the rubber's own slab capitals, or the date font's glyphs), and otherwise APPROVED or DENIED. Greek, Arabic, Japanese and Chinese eras printed a blank word before.
- **`UI/Forms/DaterImpressionArt.cs`.** Asks `DaterRubber.Word` before painting.

### BUG-70b: the clock tooltip (not closed here)
The desk clock's tooltip copies the readout's text when clicked and stays up while the readout moves on (17:28 against 17:29). FLOW's `ShiftClockReadouts` is the one source of the time and is unchanged. The fix the BUGLIST names, "drop the clock tooltip (it duplicates the readout)", is one argument in DESK-PLAN's `OfficeSceneUIBuilder.Desk.cs` (`Reaction_Clock`'s tooltip key `"tooltip.value"` → `""`) and a Build Office UI. **Handed to DESK-PLAN** (or INTEGRATION). FEEDBACK's 70a (tooltips follow hover) shortens the window but does not remove it.

### BUG-65k, 73c, 73f: hygiene
- **65k, `Timeline/TimelineService.cs`.** The nightly resolve's ~80 trace lines go through `Trace`, `[Conditional("TIMEDESK_TRACE_TIMELINE")]`: compiled out with their arguments unless that symbol is defined. Errors still log.
- **73c, `MotionTuning_Default.asset`.** The two knobs MotionKnobs no longer has (`deskStretchPerSpeed`, `deskSquashPerAccel`) are removed; the two new live ones (`transitSeconds`, `payoffWaitSeconds`) are added. DOC-HANDLE keeps its own 73c part (the duplicate `paperSlideSeconds`, MotionKnobs' dead knobs).
- **73f, `GameManager.cs`.** "no ShiftClockDriver wired" was logged when the investigation UI was missing; it now logs when the clock is missing.

### Gate checks and QA recipes (FLOW's)
- **`Assets/Editor/QA/Checks/FlowChecks.cs`:**
  - static: `flow.runInBackground` (ProjectSettings has it at 0), `flow.motionTuning.live` (no stale knob in MotionTuning_Default);
  - play: `flow.esc.office`, `flow.esc.home`, `flow.esc.title`, `flow.focus`, `flow.payoff`, `flow.transitions.cuts`, `flow.endings`, `flow.night.close`, `flow.night.bell`;
  - built player (demo): `flow.bp.load`.
- **Recipes** (`tools/qa/scenarios/`): `flow_pause.json`, `flow_transitions.json`, `flow_endings.json`, `flow_night.json`, `flow_load.json` ([BP]).
- **Rig ops** (`Assets/Tests/PlayMode/FLOW/Rig/FlowOps.cs`): `flowSettings`, `flowFocus`, `flowPayoff`, `flowEdition`, `flowClosed`, `flowEarlyFinish`.
- **UTF play tests** (`Assets/Tests/PlayMode/FLOW/Editor/FlowPlayTests.cs`, Explicit, category QaPlay): one per recipe but `flow_load`.
- **For INTEGRATION's known-red list** (`tools/qa/gate_known_red.txt`, not FLOW's file):
  - `flow.esc.home flow_pause.settings.home  # BUG-34b: Home has no Settings window until HOME (#14)`;
  - `flow.esc.title flow_pause.settings.title  # BUG-34b: the Title has no Settings window until HOME (#14)`.
  - Both leave once HOME has merged.

## 2. Tests (outside Unity)

| Check | Result |
|---|---|
| `compile_check_any.py E:\unity\NOPE-docs` | 0 errors in every project |
| Offline runner (`cc_docs`) | passed 3747, failed 0 |
| QA editor runner (`tools/qa/editor_runner`) | passed 69, failed 0, 19 NeedsEditor skipped |
| `python -m unittest discover -s tools/qa -p "test_*.py"` | OK |

New and changed EditMode tests:
- `PauseStateTests` (new): the scopes; booth, Settings, focus and a slip released in every order leave no hold (0 leaks, BELL-SIGN's pass line); a repeated request is one hold; a request again takes the newer scope; `Changed` only on a real change; `Clear`; focus loss pauses only without runInBackground.
- `FlowRulesTests` (new): every seal shape resolves and inks exactly as by name, an unknown name inks nothing; `TransitCurve` eases from rest to rest, never goes back, cuts only outside play or for no length, and keeps the kit's range; `EscapeChain` order, ties and re-registration; `ShiftEnd` closing, payoff wait and ending routing; `DaterRubber` prints a printable word as given and falls back to the English word.
- `EveningEditionTests` (new): the page's headline, story, card and fallback; the pet's fate in Home's words, the Welfare line, nothing without a pet.
- `ShiftClockTests`: `Finish` halts and closes once (the bell), and raises nothing before the start or after closing; `Format(1440)` is "24:00". `ShiftHoursTests`: the night shift closes at "24:00".
- `FlowSeamTests` (Editor side; NeedsEditor ones run only in Unity): Transit outside play cuts and waits no time, the kit's length is in range, `SceneTransit` loads as `SceneManager.LoadScene` does, a seal's second request is the same texture (no repaint), a Greek and an Arabic word print as APPROVED and DENIED.

## 3. RESULT

**Code done; deferred: Unity** (FLOW runs code-only; another agent holds the Unity lane). No scene, generated asset or builder changed, so no Build Office UI or Generate World is needed for FLOW. Every recipe below runs in this worktree's editor by job (`Library/ClaudeJobs/<name>.job`), never while Saleh is on the PC.

1. **EditMode suite in Unity** (TestRunnerApi job). **Pass:** green but the known UnitySkills failure, including `FlowSeamTests`' five NeedsEditor tests and `PauseStateTests`, `FlowRulesTests`, `EveningEditionTests`.
2. **Smoke** (`QaJobs.Smoke`). **Pass:** `harness.txt` first line `ok` or `ok (N known red: ...)` with nothing but the existing known red; 0 console errors; no `[TimelineService]` line in the run's console.
3. **Pause** (`QaJobs.Scenario`, `job_scenario.txt` = `flow_pause.json`; or `FlowPlayTests.Pause_EscReachesSettings_AndTheClockHolds`). **Pass lines in `run.json`:**
   - `flow_pause.settings.office` ok: "Settings shown True, clock X -> X over 3 s";
   - `flow_pause.focus` ok: "held True, audio paused True, clock X -> X, released True";
   - `flow_pause.settings.home` and `.title`: red until HOME (known red); green after HOME;
   - stills `office_settings`, `home_settings`, `title_settings`: Settings in front, read by eye.
4. **Transitions** (`flow_transitions.json`). **Pass:**
   - `flow_transitions.payoff` ok: "report shown True, ... report over a leaving traveller False";
   - `python tools/qa/seqtools.py cuts <run>/seq/{dayend,gohome,sleep}`: no single-frame full-screen change;
   - `frozen` on `gohome` and `sleep`: ≤0.25 s, **after RUN-SAFE routes RunManager's loads through SceneTransit** (until then, listed as RUN-SAFE's adoption).
   - By eye on `seq/dayend`: the ribbon, reaction and walk-off are fully visible before the report's first frame.
5. **Endings** (`flow_endings.json`). **Pass:**
   - `flow_endings.payoff` ok (the fired verdict's payoff before the report);
   - `flow_endings.edition` ok: "edition True, Title True, Quit True";
   - by eye on `stills/fired_report` and `stills/edition`: the report (never Home) then the evening edition with the Fired picture, the pet line, MENU and QUIT GAME.
   - Then by hand (cheat panel): SetStability 25, decide a traveller wrongly. **Pass: no firing while CONTENT's value is the destruction point** (re-run at CONTENT #17; at today's 60 it fires, which is CONTENT's value, not FLOW's code).
   - Each other ending by cheat (SetStability for Fired; AddMoney -1000 then Sleep for Bankrupt; JumpToDay 15 and play to its end for the world report): the edition, then MENU lands on the Title's ending page.
6. **Night shift** (`flow_night.json`, about 10 minutes). **Pass:**
   - `flow_night.closed.midnight` ok: "closed True, the clock reads 24:00";
   - `flow_night.bell.early` ok: "Closed raised 1 time(s) ... before closing True";
   - the bell heard on an audio capture of the `early` sequence.
7. **Dater words** (cheat JumpToDay to an el, ar, ja and zh era day and en; stamp APPROVED and DENIED on a passport each). **Pass:** every impression shows its verdict word (APPROVED/DENIED where the culture's word cannot print), ink in the word band ≥80% of the en baseline, read by eye on stills.
8. **Load [BP]** (after `QaJobs.BuildQaPlayer`; `python tools/qa/run_player.py tools/qa/scenarios/flow_load.json`, five times). **Pass:** `bp.flow_load.seq.*.frozen` ok (no frame held over 0.25 s); the office visible within 1.0 s of the click, median of 5 (read from `frames.tsv`). Needs RUN-SAFE's adoption of `SceneTransit.Load`; 31b's hall layers are listed as a known art fallback if not yet landed.
9. **Clean console** (one played day, `QaJobs.DaySequence` to day 2). **Pass:** 0 stack-traced `[TimelineService]` logs; no "no ShiftClockDriver wired" warning (the office has a clock).

## 4. What other tracks must know

| Track | What |
|---|---|
| RUN-SAFE | Route `RunManager.LoadOfficeScene/LoadHomeScene/LoadTitleScene` through `SceneTransit.Load(scene)`. The cheat End Shift already works with FLOW's once-a-day closing (its second `HandleShiftClosed` call is a no-op). |
| UI-PC, HOME, CAM, CONTENT | Screen changes go through `Transit.Fade/Slide/Wait` (0.2-0.3 s; `Transit.Seconds`). |
| HOME | Build a `SettingsWindowController` into the Title and Home scenes; PauseMenu finds and opens it on Esc (no code needed). `flow.esc.home` / `flow.esc.title` turn green then. |
| UI-PC, DOC-HANDLE | When Esc no longer backs out of the PC and the views, tell INTEGRATION, which removes EscapeChain's `office` step and `EscapeWatch`. |
| BELL-SIGN | The closed booth: `PauseState.Shared.Request("booth", PauseScope.Clock)` / `Release("booth")`, in `GameManager.Booth.cs`. |
| DIALOGUE, PORTALS | Let go first: `EscapeChain.Shared.Register("<name>", EscapeChain.TrackOrder, () => /* let go, true */)`; unregister on destroy. |
| TIMELINE-FAIL | `GameManager.CloseNow(endingId)` ends the run at the day's end (its Q4 = A). It holds `GameManager.CaseEnd.cs`, `Endings/**`, `TimelineService.cs` and `ShiftStrandings.cs` after FLOW. |
| CONTENT | D12: `firedAtStability` at the destruction point (FLOW's no-firing-at-70% pass re-runs at #17). |
| DESK-PLAN | BUG-70b: drop `Reaction_Clock`'s tooltip key in `OfficeSceneUIBuilder.Desk.cs`. |
| INTEGRATION | The two known-red lines above; LEASES.md's `GameManager.cs` rows now name the four partials; FEATURES lines are updated in this branch. |

## 5. Decisions taken in FLOW (ours, not Saleh's)

1. **Settings pauses the loops, not the one-shots.** Settings holds the clock and pauses the looping beds; one-shot cues finish, so Settings' own clicks sound. The focus lost pauses all audio.
2. **The dater rubber prints the culture's word when it can, English otherwise** (BUGLIST's "fixed English, or covered for every culture"). Latin-script cultures keep their own word; the rest print APPROVED or DENIED. This matches D7's "bureau equipment is immune" reading for the PC.
3. **A run-ending verdict closes the shift at once** (`CloseNow`): the day's end comes forward (report, then the ending), instead of the clerk working on to closing time on a run that is already over.
4. **The payoff wait is capped at 8 s** (`payoffWaitSeconds`), so the report can never be stuck behind a figure that does not leave.
5. **The evening edition is built at runtime** in the Title's own faces from existing strings, so no scene or string table changes with it. Its fallback headline for an ending without a name is "The End".
