# FLOW: smooth transitions, real pause, fast loads, endings that land

Branch `fix/a-flow`, cut from main `9b8ab5f` (P0: carve, cuts, QA-RIG) and **rebased onto origin/main `4290bd6`** (RUN-SAFE merged) in verifier round 2. Sources: FIX_PLAN.md §3 "FLOW" and FIX_PLAN_v2.md "FLOW: delta (D20, D12)" (v2 wins), BUGLIST.md, DECISIONS_1019a.md (D12, D20, R3/R4), GAME_FEEL_BIBLE.md rows F3, G1, G3, G4, J5, J6, and R7\PHASEB\LEASES.md (the GameManager carve, the scoped pause, the Esc seam).

**Bugs closed here (code; each still needs its Unity recipe, §3):** 28b (CloseNow honours the ending), 32a, 34a, 38b, 56a, 65k, 72, 73c (MotionTuning_Default), 73f (the clock warning); 34b **in the office** (R3 included).

**Verifier round 2 (this commit):** rebased onto `4290bd6` (one conflict, the FEATURES.md endings line: both insertions kept, RUN-SAFE's night check first); the statements that RUN-SAFE had not yet adopted `SceneTransit.Load` corrected everywhere; the [BP] load line now measured in real time (`FlowLoadWatch`, `flow_load` / `flow_load_seq`, §1 BUG-31a, recipe 9); `Transit.cs`'s summaries say shutter, not fade; the known-red owners and the right-click question listed for INTEGRATION and Saleh.

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

### Carve (LEASES.md proposal, done as a pure move): commit `b8c6e1b`
`GameManager.cs` is split so each Phase B track holds its own partial instead of chaining on the root:

| Partial | What moved | Phase B holder |
|---|---|---|
| `GameManager.Booth.cs` | slot arming, the AVAILABLE sign's wiring, calling and presenting the traveller, `React` | BELL-SIGN |
| `GameManager.CaseEnd.cs` | `HandleDecision`, `ShowVerdictThen`, `HandleEndingReached` | TIMELINE-FAIL (if needed), then LANGUAGE |
| `GameManager.News.cs` | Start's archive call, extracted as `RecordTodaysPaper` | NEWS-WEB |
| `GameManager.Departures.cs` | `Departed`, `AnnounceDeparture` | PORTALS |

No serialized field moved. `carve_check.py --base origin/main --head b8c6e1b`: 14 MOVED, 60 SAME. Its two reports are the extract itself: `Start` lost the archive lines, and `RecordTodaysPaper` is the new member holding them, unchanged. Both were checked by hand. LEASES.md's "if FLOW doesn't carve" chain on `GameManager.cs` no longer applies, and INTEGRATION updates the register. The shutter's call in `OpenShift` (`GameManager.Booth.cs`, this branch) passes to BELL-SIGN with the partial.

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
  - It blocks clicks while shown. A second load while one runs is ignored with a warning. Outside play mode, or for a scene that cannot load, it is `SceneManager.LoadScene`.
  - It runs on real time (`UiMotion.Delta`), so the light catches up after a long frame. **It does not hide one:** a long frame of main-thread work (a scene's activation) holds the screen, shutter and all, for its whole length. Only a real-time measurement sees that (recipe 9, below).
  - **QA marks (new in round 2).** `TransitRunner` raises `QaSignals.Event("transit", ...)` at `start:<scene>` (the load accepted, the click's frame), `rise:<scene>` (the shutter starting up over the settled scene) and `up:<scene>` (the shutter off). They cost nothing when nothing listens, and they land in a recording's `events.tsv`.
- **`Office/OfficeScenes.cs`: the gameplay layer loads asynchronously.** Behind the shutter (`SceneTransit.Loading`), the office's gameplay layer now comes in with `LoadSceneAsync` (additive), so the shutter keeps moving while it streams. Before, it was a synchronous `SceneManager.LoadScene`.
  - The runner's settle wait (every scene in `SceneManager` loaded) covers it.
  - Without the shutter (Play in the editor, a scene SceneTransit cannot stream), it still loads on the next frame as before, so the art office never shows alone for longer than today.
- **`GameManager.cs`: theme once.** `CultureThemeService` already themes each scene as it loads. GameManager's second `RefreshActive()` now runs only when it made the run itself (an office opened on its own in the editor).
- **Live in play (round 2, after the rebase).** RUN-SAFE's `RunManager.LoadOfficeScene/LoadHomeScene/LoadTitleScene` call `SceneTransit.Load` on main, so the shutter and the async load run in every scene change in play. FEATURES.md, `Transit.cs`, `OfficeScenes.cs`, `FlowOps.cs` and `flow_shutter.json` now say so.
  - 31b (the ~650 MB of hall layers) is the art-side request LIGHT files.
- **The [BP] pass is still at risk: `GameManager.Start`'s case generation runs in one activation frame.** A player sees that frame held, shutter and all, for as long as it takes. Round 1 called it "one long frame, not a held one", which was wrong. A fixed-step capture cannot see it either: `flow_load` recorded at `Time.captureFramerate = 30`, and the shutter's light steps on `UiMotion.Delta`, so `seqtools frozen` reads a multi-second activation frame as one moving frame and passes whatever the hitch.
  - **Round 2 measures it in real time.** `FlowLoadWatch` (new, rig only) times every real frame (`Time.realtimeSinceStartupAsDouble`, which `captureFramerate` does not touch) from the `start` mark to the `up` mark, with nothing recording. The ops `flowLoadClock` / `flowLoadTime` assert `load.<name>.frames` (no real frame over 250 ms) and `load.<name>.time` (shown within 1.0 s of the click) for title → office (the session's cold load), office → Home and Home → office (`flow_load.json`). The old captures moved to `flow_load_seq.json` for `frozen` and `cuts`. `flow.bp.load` needs both halves.
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

### Gate checks and QA recipes (FLOW's)
- **`Assets/Editor/QA/Checks/FlowChecks.cs`:**
  - **static:**
    - `flow.runInBackground`: ProjectSettings has it at 0.
    - `flow.motionTuning.live`: no stale knob in MotionTuning_Default.
    - **`flow.clock.tooltip`** (new): Reaction_Clock has no tooltip key. Red until DESK-PLAN.
  - **play:** `flow.esc.office`, **`flow.esc.pc`** (new: Esc with the PC open keeps it open and Settings showing, R3), `flow.esc.home`, `flow.esc.title`, `flow.focus`, `flow.payoff`, `flow.transitions.cuts`, **`flow.transit.shutter`** (new: a scene change through SceneTransit brings the shutter down, loads behind it and lifts it), `flow.endings`, `flow.night.close`, `flow.night.bell`;
  - **built player (demo):** `flow.bp.load`: both `bp.flow_load.load.*` (real time: no frame over 250 ms, the office shown within 1.0 s) and `bp.flow_load_seq.seq.*.frozen` (the captures; round 2).
- **Recipes** (`tools/qa/scenarios/`): `flow_pause.json` (now with a second Esc in the PC: `settings.pcEsc`), `flow_transitions.json`, **`flow_shutter.json`** (new: office → Title and Title → office through `SceneTransit.Load`, recorded: the shutter measured on its own, apart from a run's loads), `flow_endings.json`, `flow_night.json`, and for [BP] **`flow_load.json`** (round 2: the loads timed in real time, nothing recorded) and **`flow_load_seq.json`** (round 2: round 1's captured `flow_load`, renamed).
- **Rig ops** (`Assets/Tests/PlayMode/FLOW/Rig/FlowOps.cs`): `flowSettings`, `flowFocus`, `flowPayoff`, `flowEdition`, `flowClosed`, `flowEarlyFinish`, **`flowTransit`** (new: a scene by name, or `office` for RunConfig's art office, loaded through `SceneTransit.Load`; asserts `transit.<name>`), and in round 2 **`flowLoadClock`** (name: starts `FlowLoadWatch` before the step that starts a load) and **`flowLoadTime`** (name, n frame budget in ms (250), f visible budget in s (0 reports only): asserts `load.<name>.frames` and `load.<name>.time`).
  - **The red python test.** Before this round, all six went through a `TryRegister` wrapper, so `tools/qa/test_seqlib.py`'s scan for literal `QaRunner.RegisterOp("name"` calls saw none of them. Every `flow_*.json` failed `test_scenarios_use_known_ops` ('flowpayoff' not found, flow_endings step 12): 35 tests, 1 failure, though the first round reported it OK.
  - **The fix:** each is now a literal `QaRunner.RegisterOp("flowX", ...)` call, still registered once (`flowTransit` too).
- **UTF play tests** (`Assets/Tests/PlayMode/FLOW/Editor/FlowPlayTests.cs`, Explicit, category QaPlay): one per recipe but `flow_load` and `flow_load_seq` (QA player only; `Shutter_ASceneChangeLoadsBehindTheShutter` is new).
- **For INTEGRATION's known-red list** (`tools/qa/gate_known_red.txt`, not FLOW's file):
  - `flow.esc.home  flow_pause.settings.home  # BUG-34b, HOME (#14): Home has no Settings window until HOME builds one`;
  - `flow.esc.title  flow_pause.settings.title  # BUG-34b, HOME (#14): the Title has no Settings window until HOME builds one`;
  - `flow.clock.tooltip  # BUG-70b, DESK-PLAN (#5): Reaction_Clock's tooltip copies the readout until its key is blank (asset + builder)`.
  - Each leaves once its owner has merged. None of the three owners is confirmed in any register yet (round 2 checked main `4290bd6`): INTEGRATION confirms them.
  - The feel rows G3, G4 and J5 are red for the same kind of reason (the `BoothShutter` art object; the `booth_shutter_open/close`, `day_start`, `available_on` and `amb_hall_day` clips). INTEGRATION carries them in the feel index as red, with those blockers.

## 2. Tests (outside Unity)

| Check | Result |
|---|---|
| `compile_check_any.py E:\unity\NOPE-docs` | 0 errors in every project (161 warnings, the baseline); round 2, rebased: the same |
| Offline runner (`cc_docs`) | passed 3759, failed 0 (3747 before this round, +12); round 2, rebased on `4290bd6` (RUN-SAFE's tests included): passed 3788, failed 0 |
| QA editor runner (`tools/qa/editor_runner`, `QA_PROJECT` set) | passed 69, failed 0, 21 NeedsEditor skipped (the two new dater tests are NeedsEditor); round 2, rebased (RUN-SAFE's and QA-RIG's on main included): passed 81, failed 0, 41 NeedsEditor skipped |
| `python -m unittest discover -s tools/qa -p "test_*.py"` | 35 tests, **OK** (it was 1 failure: `test_scenarios_use_known_ops`, fixed above); round 2: 35 tests, OK (`flow_load` and `flow_load_seq` use known ops, `flowLoadClock` and `flowLoadTime` included) |

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
  - `ShiftEnd` closing, payoff wait and ending routing;
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
  - **(new) the dater's cache hits:** the second print of the same texts reuses its layers (no repaint), and a new date reuses the word's outline.

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
   - `python tools/qa/seqtools.py cuts <run>/seq/{dayend,gohome,sleep}`: no single-frame full-screen change;
   - `frozen` on `gohome` and `sleep`: ≤0.25 s (the loads go through the shutter on this tree);
   - by eye on `seq/dayend`: the ribbon, reaction and walk-off are fully visible before the report's first frame.
5. **The shutter veil** (`flow_shutter.json`, or `FlowPlayTests.Shutter_ASceneChangeLoadsBehindTheShutter`; `SceneTransit.Load` directly, the path RunManager's loads take). **Pass:**
   - `flow_shutter.transit.TitleScene` and `flow_shutter.transit.office` ok: "shutter seen down True, '<scene>' loaded True, shutter up and off True, still loading False";
   - `python tools/qa/seqtools.py frozen <run>/seq/office_title` and `.../title_office`: longest still run ≤0.25 s; `seqtools.py cuts` on both: none;
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
   - the bell heard on an audio capture of the `early` sequence.
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
     - `python tools/qa/seqtools.py frozen <run>/seq/title_office --result-dir Library/QA/results --bp --owner FLOW --commit <c>`, and the same for `home_office`. **Pass:** "longest still run" ≤0.25 s on both (`bp.flow_load_seq.seq.*.frozen`);
     - `python tools/qa/seqtools.py cuts <run>/seq/title_office`, and `home_office`: none.
   - **c. The gate:** `flow.bp.load` green ("real time: ... 0 fail ...; captures: 2 result(s): 2 pass ...").
   - 31b's hall layers are listed as a known art fallback if not yet landed.
10. **Booth shutter (G3, G4)** once the art's `BoothShutter` object exists. **Pass:**
    - the first AVAILABLE press starts the clock on that frame and the shutter rolls up over 1.2 s (36 frames, fast then settling);
    - at the day's end, after the walk-off, it rolls down over 1.2 s (slow then landing) with `booth_shutter_close` on the landing frame, and the report's first frame comes after it;
    - without the object, the report follows the payoff with no extra wait (today).
11. **Clean console** (one played day, `QaJobs.DaySequence` to day 2). **Pass:** 0 stack-traced `[TimelineService]` logs; no "no ShiftClockDriver wired" warning (the office has a clock).
12. **Clock tooltip** (after DESK-PLAN's two edits). **Pass:**
    - `flow.clock.tooltip` green;
    - by hand, a click on the desk clock shows no tooltip.

## 4. What other tracks must know

| Track | What |
|---|---|
| RUN-SAFE | Done on main: `RunManager.LoadOfficeScene/LoadHomeScene/LoadTitleScene` call `SceneTransit.Load(scene)`, so the shutter and the async load are live in play (FLOW is rebased on it). The cheat End Shift already works with FLOW's once-a-day closing: its second `HandleShiftClosed` call is a no-op. Still open: decide what Settings' Quit to Title does to a mid-shift run. |
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
| INTEGRATION | **Confirm the owners nobody has confirmed yet:** BUG-70b to DESK-PLAN; Settings at Home and on the Title (34b) to HOME. Add the three known-red lines (§1: `flow.clock.tooltip`, `flow.esc.home`, `flow.esc.title`) and carry G3/G4/J5 as red in the feel index with their blockers (the `BoothShutter` object; the shutter, `day_start`, `available_on` and `amb_hall_day` clips). Put §5.6's right-click question to Saleh. LEASES.md's `GameManager.cs` rows now name the four partials. Fold the feel cards `R7\FIX\FEEL\G3/G4/J5/J6.md` into the index. The FEATURES lines are updated in this branch. |

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
