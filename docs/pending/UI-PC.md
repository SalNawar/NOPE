# UI-PC: a clean desktop with a real taskbar clock

Branch `fix/a-ui-pc`, cut from main `9b8ab5f` (P0: CARVE, CUTS, QA-RIG), then rebased onto main `4290bd6` (RUN-SAFE merged; no file in common). Commits: the track (`60fe324` before the rebase), fix round 1 (`d6fd65b`, `3996e95`: the review of `60fe324`), fix round 2 (`4e2445a`: the review of `d6fd65b`) and fix round 3 (`e85d9bd`: the reverify of `4e2445a`, WAVE2_OPEN "UI-PC", below).

**Sources:**
- FIX_PLAN.md §3 "UI-PC" and FIX_PLAN_v2.md "UI-PC: delta" (v2 wins);
- BUGLIST.md;
- DECISIONS_1019a.md (Saleh's calls; later entries override earlier ones). Quoted:
  - R3/R4 (2026-10-20): "Esc opens the same Settings window everywhere (PC, Home, Title). To leave the PC, press Q or click the bezel."
  - D20: "if you press escape you should auto open the pc setting menu."
  - D7: the PC stays in the player's language. D19: News merges into the Internet (NEWS-WEB's).
- GAME_FEEL_BIBLE rows A12, F1-F4, G1 and gap 2 (FlashPolicy);
- PHASEB/LEASES.md and docs/pending/CARVE.md, CUTS.md, QA-RIG.md.

**Code-only.** Saleh was on the PC and another agent ran Unity, so nothing here was played. OfficeGameplay.unity is not rebuilt or committed (INTEGRATION's). Every Unity check is in the RESULT section as a recipe on the QA rig.

## Fix round: the review of 60fe324

| # | Review item | Resolution |
|---|---|---|
| 1 | **Ownership.** The commit edited `world_source.json` (CONTENT's), `tools/qa/so_reader_allowlist.txt` (INTEGRATION-kept) and `Desktop_Default.asset` (not listed under any track). | All three are back to main's bytes. Their changes are fragments under `docs/pending/data/UI-PC/` (FIX_PLAN v2 rule 11's route), which INTEGRATION applies in UI-PC's merge commit: `ui_strings.json`, `Desktop_Default.json` and `so_reader_allowlist.txt`. See **Data fragments** below. The code no longer needs any of them to pass the offline suite: no script looks up a new key by a literal. |
| 2 | **Exits.** The full-screen exit catcher still closed the PC on a click on the room. Esc with the screen off still closed the PC through OfficeControls. | **The room:** the catcher is now `RoomCatcher`, a transparent plate that takes the click and does nothing. R3/R4 (2026-10-20) overrides the 2026-09-30 pass-through. **Screen off:** DesktopKeyboard takes the back-out itself while the PC is open with its screen off (`PcBackOut.ScreenOff`). Esc turns the screen on (`MonitorScreen.PowerOn`) and brings Settings forward. A right-click does nothing. Both are stamped, so OfficeControls never closes the PC. This no longer waits on DOC-HANDLE. |
| 3 | **F1 effect.** Shrinking the frame camera's viewport drew the whole desktop scaled down in a strip. With Reduced Motion, the picture (its own camera) hard-cut. | **The line:** the camera keeps the glass's whole viewport, and its projection is squeezed about the centre (`Matrix4x4.Scale × projection`). The picture becomes a full-width line inside the black tube (the camera's clear). It is reset to the camera's own projection when the move ends. **Reduced Motion:** a two-half fade. First the bezel and a dark glass fade in over the room, with the camera off. The camera comes on under the fully dark glass, then the glass clears off the picture. Closing is the reverse. The picture never cuts in or out where it can be seen. `CrtPower` returns this as a `CrtFrame` (X, Y, Bezel, Veil, Picture). Tested. |
| 4 | **Labels written over by the theme.** The chevron was keyed `taskbar.more`, so each theme refresh wrote `» {0}` over the count. The Quit entry's warning was written into its keyed label. | **The chevron** has no label key. Its label is `TaskbarStrip.ChevronLabel(n)`, a glyph and a number, with nothing to translate (tested). **Quit game** has a second, hidden label, `Warning`, keyed `startmenu.quitForfeit` (built by `.Desktop.cs`). DesktopShell swaps which label shows, so a refresh re-applies each label to its own text. |
| 5 | **GAME_FEEL rows.** F1's degauss lead and afterglow were missing. F3's ui_toggle was suppressed with no statement of the conflict. F4 was never mentioned. | **F1:** pc_on starts on the press frame. The glass stays dark for `crtHumLeadSeconds` (80 ms), then the line grows. On closing, the dot glows out over `crtAfterglowFrames` (2 frames). Both are tested. **F3:** a press on a Settings switch plays `ui_toggle` instead of the mouse click (`PcPressCue`; switches carry `PcSwitch`). That gives one press one sound, and the switch's sound is the toggle, so the conflict is resolved, not traded. **F4:** see **GAME_FEEL rows** (deferred to PORTALS, with the reason). |
| 6 | **BUG-50 placeholder art.** No art request or fallback was filed. | See **Art request (GPT mailbox)**, with a fallback per item. |
| 7 | **Owned pass criteria unmet.** | Stated under **Open** and **Needs a ruling**. Three items are closed in this round: CODE-157 (the overlay pair is kept by key, tested), CODE-156's Shift+F10 anchoring and pinned-row refocus, and CODE-154's stale toast. |
| 8 | **The standing harness.** | `ui_pc_session.json` now plays the plan's whole session by player input (Input System events, never a handler called directly): `uipc.windows`, `uipc.enter`, `uipc.zoom`, `uipc.balance`, `uipc.room`, `uipc.escdark`, plus the earlier five. `uipc.windows` opens 8+ windows from Start and walks the chevron. `uipc.restore` scrolls with the wheel, minimises with the title bar's button and restores with the taskbar button. The `uipc.order` hint now says `cheat AddMoney`. Each op has a gate check in `UiPcChecks`. |
| 9 | **Tests.** The tests covered only pure rules, and they were not seen red first. | The rules behind the integration paths are now pure and tested: the screen-off back-out, the press cue, `OrderPick` (what the detail card shows versus what the action acts on), the chevron label, `OverlayPick`, and the CRT and flash knobs. All 15 new tests were written first and seen red (CS0722, CS0246 and CS0103: the types did not exist) before the code. The Unity-side wiring is the harness's (item 8). |
| 10 | **Minor.** CrtPower's, FlashBudget's and QuitGuard's numbers were constants. Mail kept a dead Times path. Hover-select moved the Orders action. | **Knobs:** DesktopConfigSO now holds `crtOpenSeconds`, `crtCloseSeconds`, `crtFadeSeconds`, `crtLineHeight`, `crtHumLeadSeconds`, `crtAfterglowFrames`, `flashesPerSecond`, `flashTintSeconds` and `quitWarningSeconds`. PcFrame and DesktopShell read them; `FlashPolicy.Configure` takes the flash pair. **Mail:** the dead Times path is gone from UI-PC's files: MailFeed's News source, Mail's Times wording and its News link. Mailbox.cs still builds the item (it is unlisted, so it is on the cleanup list). **Orders:** the pointer over a card only previews it. Only a click or the arrows choose the card that the action and Enter act on (`OrderPick`). The action button shows only while the detail card shows the chosen card. |

## Fix round 2: the review of d6fd65b

| # | Review item | Resolution |
|---|---|---|
| 1 | **Not done by the track's own admission:** BUG-58b (16 px at A2), BUG-65i (25 MB), BUG-50a's empty states and default sizes (Citizen Account, Notes), CODE-111. | All four are now built in code. What is left of each is the Unity measurement (FIX_PLAN rule 5, items 3-6), listed under **Open: what stands between the track and done**. **58b:** the PC type scale is the legible one (Body 31 u = 16.1 px at 720p; `PcType.Legible`, a one-line revert). **65i:** the old ~31 MB estimate was wrong (it counted the mips twice). From the 272 PNG headers the kit is 92.1 MB as RGBA32 with mips (the audit's ~92 MB) and **23.2 MB in BC7 with mips**, under 25 MB with no atlas. The new static gate `uipc.kit.memory` measures it as imported. **50a:** the Citizen Account's pages sit on the form style's dark backing (no blank margin), Notes' Days column has its own empty state, and the reading apps share one restored size. **CODE-111:** `PageFit` keeps the pane's text zoom on scanned pages, and a page wider than its pane scrolls sideways instead of being cut (tested). |
| 2 | **Rulings still open** (Transit, the 9 px CRT clause, Desktop_Default.asset's owner, the inert room). | Unchanged: they are asks under **Needs a ruling**, not decisions. Until they are ruled, the BUG-20 and BUG-32c pass lines are not met as written. Item 4 now also names what ClickCatcher becomes under each answer (fix round 2, item 6). |
| 3 | **`uipc.enter` likely red:** its first Enter came ~17 frames after `uipc.order`'s last click, inside OrdersWindow's shared debounce at an uncapped frame rate. | The op now waits `DoubleClickSeconds + 0.15 s` before its first Enter, as its later steps do. The shared clock behind it is gone too (item 8), so a card click no longer refuses the next Enter at all. |
| 4 | **F1 frame-0 pop:** the bezel (63 % of the screen at A1, more at A3/A4) showed whole on the press frame and hid in one frame after the afterglow, so `seqtools cuts` would flag pc_open's frame 0 and pc_close's last frame. | **The frame fades in and out** (`crtFrameSeconds` 150 ms). On the press frame it shows at `CrtPower.FrameFirstStep` (0.2), and each 30 fps capture step is ≤0.18 of the frame's alpha. The test measures every captured step against a cut's 0.25. The picture's camera stays off until the frame is whole. Turned on under a half-faded glass, its black clear would have cut in. **The hum** (`pc_on`) now plays `crtHumLeadSeconds` (80 ms) before the picture, so it starts 70 ms after the press. **Closing:** the glass is dark as the picture goes (it showed the room through the hole for the afterglow). The dot glows out over it, then the frame fades out over 150 ms and only then hides. **Reduced Motion:** the fade is 300 ms (two 150 ms halves, so each half is ≥4 capture frames), and the frame's last closing step is ≤0.22. |
| 5 | **`uipc.balance` vacuous** when OfficeReadouts' credits text is unbound. | The HUD comparison now fails with "no readout bound" when there is no credits text. The plan compares Account, Orders and the HUD, so a missing readout is red, not skipped. |
| 6 | **ClickCatcher's pass-through is unreachable**, and its doc still says a click on the room closes the PC. ClickCatcher.cs is unlisted. | Filed under **Requests** for the orchestrator to assign: drop `passThrough` and `PassOn`, and correct the class doc. If Saleh rules (Needs a ruling, item 4) that a click on the room should reach the room, then reuse it instead: RoomCatcher gets a ClickCatcher with `passThrough` and no `onClick`. |
| 7 | **Misplaced doc comment** in DesktopWindowManager (`TopHit`'s summary sat above `IsSwitch`). | Moved back onto `TopHit`. Each member has its own summary. |
| 8 | **Squeezed projection and the event camera:** a click during the power-on mapped through the 2 % line projection. | **Mitigated:** `PcFrame.PictureMoving` is true from the press until the picture is whole, and from the close until the frame is gone. MonitorScreen holds the desktop's raycaster off meanwhile, and DesktopWindowManager keeps the EventSystem's navigation off. The bezel, its X and power button stay live from the press frame. A click on the desktop lands on nothing while the player cannot see it, never on a hidden element. (This round also claimed the keys stayed live. They did not: fix round 3, item 1.) |
| 9 | **Feel: one shared debounce** silently refused any Enter or button press within 0.4 s of a card click that ordered. | **`OrderInputs`** (Domain, tested). Each input has its own clock. The button's double-click acts once, and so do two quick Enters, but a card click never refuses the Enter or the button that follow it. A card click needs no clock: it acts only on an orderable card, so its second click finds the card ordered and does nothing. **A refused press plays `ui_error`**, so it is never silent. |
| 10 | **Merge dependency:** the fragments must land in UI-PC's merge commit, before Build Office UI. | Unchanged, and the order is restated under **Data fragments**. There is now one more Full key (`notes.daysNone`, Notes' empty Days column), so until the fragments land **five** builder labels draw raw keys. |

## Fix round 3: the reverify of 4e2445a (WAVE2_OPEN, "UI-PC")

| # | Finding | Resolution |
|---|---|---|
| 1 | **S2 regression: Esc and a right-click during the power-on closed the PC** (R3/R4 "Esc = Settings everywhere"; GAME_FEEL F1 [S]). DesktopKeyboard gated its keys on the desktop's raycaster, and round 2 holds that raycaster off for the whole power-on (`PcFrame.PictureMoving`, about 300 ms with full motion and with Reduced Motion). So no key was taken and no Esc was stamped, and OfficeControls' back-out closed the PC. | **DesktopKeyboard now gates on `MonitorScreen.IsInteractive`** (BoothRules.DesktopInteractive: the frame open, the screen on), which is true from the press frame. Its `raycaster` field is gone, and so is the builder's wiring of it (`.Keys.cs`). Only the desktop's clicks still wait for the whole picture. The keys and the back-out never do: during the power-on, Esc runs the desktop's chain and then brings Settings forward, a right-click is stamped and does nothing, and both are stamped, so the office never closes the PC. **The session step** `uipcPowerOn` (after `uipcEscDark`) closes the PC with Q, opens it with Q, right-clicks over the glass 2 frames later and presses Esc 4 frames later, with full motion and then with Reduced Motion. It asserts that both presses landed while the picture moved, that the PC stayed open and that Settings came forward (asserts `poweron.full` and `poweron.reduced`; gate check `uipc.poweron`). Reduced Motion is set for the step and put back after: it is a setting, not the input under test. |
| 2 | **The standing QA python test was red:** `ScenarioTests.test_scenarios_use_known_ops` collects track ops with `RegisterOp\(\s*"([A-Za-z]+)"`, so the dotted `uipc.*` names were unknown offline. | **The 11 ops are renamed without dots,** as QA-RIG's test reads them: `uipcTray`, `uipcRoom`, `uipcEsc`, `uipcEscDark`, `uipcWindows`, `uipcOrder`, `uipcEnter`, `uipcZoom`, `uipcRestore`, `uipcBalance`, `uipcBezel`, plus the new `uipcPowerOn` (UiPcRigOps' registrations and `ui_pc_session.json`). The assert ids (`ui_pc_session.esc` ...) and the gate check ids (`uipc.esc` ...) are unchanged. `python -m unittest discover -s tools/qa -p "test_*.py"`: Ran 35, OK. It is now in the **Tests** table. |
| 3 | **F1 pop on a reversed move:** `SetOpen(false)` during the power-on restarted the collapse at the whole picture (the bezel jumped to 1, the desktop appeared in one frame, then squeezed). `SetOpen(true)` during the collapse, the afterglow or the frame's fade out dropped the bezel to 0.2 and replayed from the start. | **A reversed move picks up where the glass is** (`CrtPower.Resume`, pure and tested). PcFrame keeps the frame it last drew, and the new move starts at the time whose frame is that one. Q before any picture showed: nothing to collapse, so the frame fades out from its alpha. Q while the line grows: the collapse starts at the same height. A click on the PC during the collapse: the power-on goes on from the same height, with the hum and no second glow. During the frame's fade out: it fades back in from its alpha. With Reduced Motion: the fade turns back from where it is. Three moments have no twin on the other move and pick up at its nearest: the afterglow's whole frame on a dark glass powers on at once from the line, with the hum; the collapse's dot opens from the full-width line, a change inside 2 % of the glass; a frame fainter than the first step (0.2) opens from that step, the press frame's. **The close takes no click:** from the Q, the fading frame's CanvasGroup stops blocking raycasts, so its full-screen RoomCatcher, its X and its power button no longer swallow the first desk click after Q or toggle the screen (WAVE2's "Close window input" check, fixed before it is measured). |
| 4 | **Doc out of date:** a stale CODE-111 "deferred" line, Orders' `ActionDebounce`, and `crtFrameSeconds` missing from the Desktop_Default fragment. | The CODE-111 line is gone (it is built: `PageFit`). BUG-58a's Orders line names `OrderInputs`. `Desktop_Default.json` lists `crtFrameSeconds`. The keys' claims (BUG-32c's Input line, the F1 row, the FEATURES fragment) are true now (item 1). |
| 5 | **House rule, a public member with no runtime caller:** `OrderInputs.Seconds` was read only by a test. | Dropped. The test asserts the behaviour instead: the button's own clock still refuses at 0.39 s and acts again at 0.41 s. |
| 6 | **Open rulings** (the 9 px CRT clause, Transit, Desktop_Default.asset's owner, the inert room). | Unchanged and not the track's to make: see **Needs a ruling**. Until rulings 1 and 2 are made, the BUG-20 and BUG-32c pass lines are not met as written. |

## Saleh's decided behaviour, as built

- **The PC is always English** (D7 and 2026-10-18).
  - Every PC string goes through `UiText.Pc` (the reading language).
  - The new strings are `Full` tier: English in every culture, with no translations.
  - No PC script reads a culture string.
- **Esc opens Settings, and never leaves the PC** (R3/R4).
  - On the PC, Esc first runs the desktop's own chain: a menu, the card, the search, a field, the Start menu, a drag, a held value. With nothing left to close, it brings Settings to the front (`PcBackOut`).
  - With the screen off, Esc turns the screen on and brings Settings forward (`PcBackOut.ScreenOff`).
  - The desktop stamps every back-out press, so OfficeControls' back-out never closes the PC.
- **Only Q and the bezel leave the PC** (R3/R4). Each former way out now does this:

  | Former way out | Now |
  |---|---|
  | A click on the bezel band, or its red X | Leaves (the X sits on the bezel). Q stays as it is (OfficeControls, DOC-HANDLE's). |
  | A click on the room beside the frame | Does nothing. |
  | A right-click with nothing to close | Does nothing. |
  | A window's X | Closes only its window. |
  | The bezel's power button | Only powers the screen. |
  | Back to desk, Start's "Turn off screen" | Gone. |
  | The DESK [Q] tab | CAM's (request below). |
- **The Internet waits for NEWS-WEB** (D19, BUG-50b). The Internet app is unchanged. The fallback stays v1's: the designed minimal page, or the app hidden with Saleh's OK at candidate time.

## Changes per BUG id

### BUG-20a: no clock box; a Windows tray clock
- **The desk CRT's clock plate is retired.**
  - `CrtClock` is an empty marker that never shows, and `PcScreenClone` no longer places or shows it.
  - `BuildCrtClock` builds only an inactive host with two empty texts. They exist because DESK-PLAN's `.Desk.cs` and the root still look them up (cleanup list below).
- **The tray is Windows' clock:** the time over today's date (`"09:07"` over `"14 MAR 2150"`). It is the bar's one small glass readout.
  - The time is ShiftClockReadouts' (the shift clock's own format).
  - The date is `TrayClock.Date` (Domain).
- **`DesktopConfigSO.taskbarHeight` is 72 u** in code. The asset value is a fragment (`Desktop_Default.json`; main's asset holds 60).
- **Needs a ruling:** v1's pass line asks for a "≥9 px cap on the desk CRT clone at A1". It is not met by design (see **Needs a ruling**, item 1).

### BUG-21a: taskbar skin, Start, Start menu, window buttons, dead affordances
- **Tray.** Credits and the Helix River left the tray. The river hangs on the wall monitor; the wallet is the desk till's and the Citizen Account's. The tray holds only the clock.
- **Bar.** The light kit bar reads as one piece: Start, the window buttons, the clock glass. Back to desk is gone.
- **Start label.** "Start" reads Start through the `taskbar.start` value change (fragment). Until INTEGRATION applies it, the label reads "Menu".
- **Start menu.**
  - Each app entry has the app's kit pictogram tile.
  - The menu fits the entries a day shows (`DesktopShell.FitMenu`), so days 1-4 have no blank slab.
  - "Turn off screen" is gone.
- **Window buttons.**
  - Each shows its glyph and its title (ellipsis, never mid-letter).
  - They shrink together from `taskbarButtonWidth` (220 u in code; the asset value is in the fragment) to their least, 120 u.
  - Past that, the rest wait behind a chevron (`» 3`, `TaskbarStrip.ChevronLabel`, no key), whose click brings the next one forward. The focused window's button always shows.
  - Pure rule: `TaskbarStrip.Fit`. Knobs: `taskbarButtonLeast` and `taskbarChevronWidth`.
- **The date affordance** has no underline. The clock button is live only while the Investigation app is on the PC (`InvestigationApp.OnPc`).

### BUG-21b: culture colours vs FEATURES
The PC's colours are replaced by PC-LANG-FX in Phase B, so UI-PC changes no colour.

**Fallback (v1 default):** FEATURES lines 78 and 91 are corrected below. The taskbar and title bars keep the kit's deliberate look and take no culture colours.

### BUG-21c: one press, one sound
A press on the PC plays exactly one cue, chosen by the desktop (`DesktopWindowManager.Press`, `PcPressCue`):
- on a switch (a Settings choice marked `PcSwitch`, or a Toggle), `ui_toggle` (F3);
- anywhere else, `mouse_click`.

UiJuice plays no `ui_press`, `ui_release` or `ui_toggle` for a control on the PC desktop's layer. Its hover and a refusal's `ui_error` stay. ScanLayerSwitch's extra `ui_press` and `ui_tab` are gone. Outside the PC nothing changes.

### BUG-50a: apps and exits
- **Exits:** as in **Saleh's decided behaviour** above.
- **Mail.**
  - The auto copies are gone (`MailDigest`): the newspaper is not mailed, and only today's directive memo stays.
  - A message opens with one draw, not two.
  - The dead Times path is gone from Mail: MailFeed's News source, Mail's Times sender, subject, reference and body, and its News link with the two fields it used. `BuildMailWindow` keeps its two parameters only until INTEGRATION drops the root's arguments.
- **Shortcut card.** It is 960 × 860 → 980 u tall (UI-16; a code default the asset does not hold).
- **Orders.**
  - Ordering is one click. The pointer over a card previews it in the detail card. A click chooses a card, and orders it when it can be ordered.
  - The action button and Enter act on the chosen card only (`OrderPick`). The button shows only while the detail card shows the chosen card.
  - The zoom bar sits in the header row, right of the wallet line (UI-25).
  - An upgrade's own icon art is no longer overwritten by its band's kit tile (CODE-158).
- **Investigation.**
  - The pane tags are reworded "Left page" / "Right page" / "Links open here" (UI-24; value changes in the fragment). They read the old words until INTEGRATION applies it.
  - The Reference tab opens on a book that is on today's shelf (BUG-47b).
- **Citizen Account (UI-24: "a third of the window is an empty blue-edged margin").** The Record Extract is a 520 u portrait page, and the Statement below it is a landscape page across the window. On the window's own colour, the room beside the portrait page read as blank paper. The pages now sit on the form style's dark backing (`DiegeticBacking`), as the Documents tab lays its scanned pages, so the margin reads as a document viewer's surround. The wheel also scrolls over it now: the scroll has a graphic there.
- **Notes (UI-24: "two empty columns").** The Days column shows its own empty state under today's row while no earlier day has a page: "Pages from earlier days show here." (`notes.daysNone`, a Full key in the fragment; `NotesWindow.daysHint`). The Clippings area already had its hint.
- **Default sizes (UI-24: "window sizes are inconsistent").** The reading apps (Mail, the Citizen Account, Notes) share one restored size, 1060 × 800 u. The work apps (Investigation, Orders) open maximised. Portals stays 880 × 600 (its spec). Settings is 1000 u tall, which fits above the 72 u taskbar. The sizes are DesktopConfigSO code defaults: Desktop_Default.asset holds none of them, so no fragment is needed.

### BUG-58a: app state
- **Wallet signal.** `WalletEvents.Changed` has its first listener: the Citizen Account redraws on every wallet write while it shows, and keeps its scroll. OrderBook's order and cancel raise it. Orders redraws on its signature. The tray has no wallet any more (CODE-121).
- **Orders** (CODE-114, CODE-120):
  - It lays its tree out once, so a restored window keeps its scroll, zoom and chosen card.
  - Each of its inputs (the action button, Enter) acts once within the desktop's double-click time, on its own clock (`OrderInputs`): a card click never refuses the Enter or the button after it.
  - The pointer passing over other cards never changes what the action acts on (`OrderPick`).
- **Notes** keeps the page the clerk was on across a restore on the same day (CODE-160).
- **Enter and navigation (CODE-117).** While the desktop takes input, the EventSystem sends no navigation events. Enter is the desktop's keys only.
- **Fake-null (CODE-116).** `?? AddComponent` is replaced with TryGetComponent in ScanLayerSwitch and SiteRenderer.
- **The strip note (CODE-110)** stays on the office strip until the next pick (`CompareController.Release`).
- **The case board (CODE-112)** keeps one rest position, and returns to it when hidden mid-slide.
- **The case board overlay (CODE-157):** the chosen pair is kept by title and occurrence across a new scan (`OverlayPick`, tested).
- **The scan wipe (CODE-153)** hides when the view hides mid-wipe.
- **The scan toast (CODE-154):** a toast whose time ran out while the app was minimised is gone on the frame the app shows again, so it never flashes stale. The Papers dot still marks the paper.
- **Hold line on the CRT (CODE-113).** A held value's line follows the pointer only while the desktop takes input.
- **Keys (CODE-156):**
  - Ctrl+K is polled.
  - Shift+F10 with nothing selected opens the desktop's menu at the icon area's top left (it opened past the right edge).
  - Enter on a pinned or recent row keeps the ring on that row (no refocus to row 0).
- **CODE-111** (text zoom on scanned pages) is built: `PageFit` keeps the pane's zoom, and a page wider than its pane scrolls sideways (see **Open**, recipe 16).
- **Deferred to their replacements:** CODE-160 for the Browser (NEWS-WEB replaces it) and Portals (PORTALS replaces it).

### BUG-58b: PC legibility
- **The type scale is the legible one** (`OfficeSceneUIBuilder.Tokens.cs`, `PcType.Legible = true`):
  - Caption 24 → 28 u (14.5 px at 720p);
  - Body 26 → 31 u (16.1 px at 720p: FIX_PLAN's "smallest body text ≥16 px" at A2);
  - Title 28 → 33 u, Heading 34 → 38 u, to keep the hierarchy.
- **The rows keep their heights.** A Body line (31 u, about 38 u with its line spacing) fits the 44 u control and the 48 u row.
- **The token is the PC's one scale, so the change reaches its other readers too:** TEXT-LANG's `.Kit.cs` floor for kit-fitted text on the desktop (`PcType.Caption`), and the root's generic window body and input field (`PcType.Body`). That is intended: they are PC text.
- **The revert is one line.** If recipe 5's TMP overflow scan finds a truncation the layouts cannot take, set `PcType.Legible = false` and rebuild Office UI. That restores the old scale exactly.
- **The bezel** grows to the screen's height on screens taller than 16:9 (`PcFrame.FitBezel`): up to 80 % of the width, never under its designed size. At A4 and A3 the PC's text is about 16-24 % larger again.
- **Not in the scale:** the window title bars (DesktopConfigSO `titleFontSize` 26, an asset value), the forms' own type (a document's design, D2) and the Orders tree's node text (its zoom levels and `ordersTextFloor`). The 16 px pass line is about body text. Recipe 10 records the smallest of each.

### BUG-32c: PC open and close (GAME_FEEL_BIBLE F1)
The picture powers on as a CRT (`CrtPower`, with DesktopConfigSO's knobs):
- **Open.**
  - From the press frame, the frame and its dark glass fade in over `crtFrameSeconds` (150 ms). It starts at 0.2 alpha (`CrtPower.FrameFirstStep`), so the press is answered at once and no frame is a cut (F1: "no frame-0 UI pop").
  - `pc_on` (the degauss hum) plays `crtHumLeadSeconds` (80 ms) before the picture.
  - Once the frame is whole, the picture grows from a bright full-width line (2 %) to full height over 150 ms OutCubic. Its camera is off until then.
  - At the line's start, a one-frame glow plays on the glass through `FlashPolicy`.
- **Close.** The picture squeezes to a line, then a dot, over 120 ms, and the glass is dark as it goes. The dot glows out over 2 frames (the phosphor afterglow). Then the frame fades out over 150 ms, and only then hides.
- **How the line is drawn.** The frame camera keeps the glass's whole viewport, and its projection is scaled about the centre. The black clear is the tube around the line.
- **Reduced Motion.** The two-half fade (fix round 1, item 3), now 300 ms (`crtFadeSeconds`: two halves of 150 ms). Nothing moves, and neither the frame nor the picture cuts.
- **Input.** The bezel, its X, its power button and the keys take input from the press frame: DesktopKeyboard reads `MonitorScreen.IsInteractive`, so Esc during the power-on brings Settings forward and a right-click does nothing (fix round 3, item 1). The desktop takes clicks once the picture is whole (`PcFrame.PictureMoving`; fix round 2, item 8). Closing, the fading frame takes no click (its CanvasGroup stops blocking raycasts at the Q).
- **A reversed move** (Q or the bezel during the power-on; a click on the PC during the collapse, the afterglow or the frame's fade out) picks up where the glass is (`CrtPower.Resume`, tested), so it never pops (fix round 3, item 3).
- **Not consumed: Transit** (see **Needs a ruling**, item 2).

### BUG-35c: the mid-shift quit warning
During a shift (started and not closed; a pause counts), Quit game first shows its Warning label "Quit? Today's shift is forfeit. Click again", with the error cue. A second press within `quitWarningSeconds` (4 s) quits (`QuitGuard`).

Closing the menu drops the warning. Outside a shift, one press quits. The label's key is in the fragment, so INTEGRATION's Generate World must run before Build Office UI.

### BUG-47b: proof and hint colours; the Reference tab
- **Only proofs are red** (`FindingShades`):
  - A proof is a proved deviation (DiscrepancyLog), or a directive fault's evidence (a broken rule, NotToday, Expired, a missing paper).
  - Any other logged difference is an amber hint: "Data differs" with no proof, NO RECORD, or a seal mismatch with no proof.
- **How a hint looks:**
  - on the workbench, a dashed amber line;
  - on the status line, the note's plate;
  - in the findings column, an amber-tinted plate whose headline is not bold.
- The classification is CONTENT's (FindingRules), unchanged.
- **The Reference tab** opens on the first book on today's shelf.
- **Not mine:** DeskInspect's desk line reads `MatchBoard.Line`'s FindingLook. CONTENT can show hints there by reading `FindingShades`.

### BUG-28c and BUG-28d: orders
- **CancelPendingFor (28c)** is final. A pending order is always today's, so OrderBook's own same-day cancel and refund is the whole job.
- **Last-day orders (28d):**
  - `OrderBook.Order` refuses on a day `Orders.CanOrder(day)` is false.
  - The Orders window greys the action and the card click, and shows its header note "Orders are closed today: nothing would arrive" (a keyed label, `ClosedText`).
  - `CanOrder` is CONTENT's (still the P0 stub, true), so this is measured fully at CONTENT's merge.

### BUG-65i: UI kit memory
- **UiKitImporter compresses the kit:** CompressedHQ, BC7 on Standalone, mips kept. Its `GetVersion` is 2, so the kit reimports, and INTEGRATION commits the changed kit `.meta` files with the rebuild.
- **The budget is met by the compression alone.** From the 272 PNG headers (width × height, every mip level, 4×4 blocks):
  - RGBA32 with mips: 92.1 MB (the audit's ~92 MB);
  - BC7 with mips: **23.2 MB**, under FIX_PLAN's 25 MB.
  - One sprite is not a multiple of 4 and stays uncompressed (0.1 MB).
  - The previous estimate (~31 MB) counted the mips twice.
- **No SpriteAtlas.** The plan's approach line names one, but the pass line is the memory, and an atlas adds padding without saving memory. Batching is not a pass line. The deviation is stated here for the reviewer.
- **Static gate `uipc.kit.memory`:** it loads every kit texture as imported and sums each mip's GPU size in its imported format (`GraphicsFormatUtility.ComputeMipmapSize`). It must be ≤25 MB (decimal, the stricter reading), and it names the five largest. The memory profile in play (recipe 1) confirms it in the running game.

### BUG-65j: change-driven redraws
- **MatchLines** redraws only when a box, the pointer (while holding) or a visibility changes.
- **MatchBoard** keeps the two rows it found.
- **Mail** draws once per open.

### BUG-68b
DesktopWindowManager's `InputGate.Blocked` check (inserted by the carve) is kept. DesktopKeyboard's screen-off back-out also respects it.

### BUG-2b (v2: the scanner's microprint view)
**Waits on TOOLS #10.** ScanLayerSwitch is ready for a Micro tab once TOOLS provides the line and its size.

### GAME_FEEL gap 2: FlashPolicy and Reduce flashing
- **`FlashBudget` / `FlashPolicy`** (Domain): at most `flashesPerSecond` (3) flashes in any second. With Reduce flashing, every flash is a static tint held `flashTintSeconds` (200 ms). Every flash asks the one static budget, which PcFrame configures from DesktopConfigSO at load.
- **`FlashPreference`** keeps the choice in PlayerPrefs.
- **Settings** has a Reduce flashing Off / On row (`FlashSettingRow`, both choices `PcSwitch`).
- **Consumers.** The first is the CRT's power-on glow. HALL-FX, LIGHT and DETAIN route their flashes through `FlashPolicy.Request(now)`.

## GAME_FEEL rows (UI-PC's: A12, F1-F4, G1)

| Row | Status |
|---|---|
| **F1** | **Built:** opens on the press frame (the frame's first fade step); Q and the bezel leave; Esc = Settings (also with the screen off); the power-on line (150 ms OutCubic) and the collapse to a dot (120 ms); the degauss hum 80 ms before the picture; the 1-frame glow local to the glass; the 2-frame afterglow on a dark glass; no frame-0 pop and no last-frame pop (the frame fades in and out, every 30 fps step under a cut's, tested), also on a reversed move (`CrtPower.Resume`, tested); Esc and the keys from the press frame, also during the power-on (session step `uipcPowerOn`). **Others' cells:** the camera's Heavy push is CAM's. `pc_on` and `pc_off` exist; a dedicated degauss take is a FEEDBACK sound-bank request. |
| **F2** | **Built:** one click, one action and one sound (BUG-21c, BUG-58). The window scale and the hover lift are main's, unchanged. |
| **F3** | **Built:** Esc on the press frame cancels a hold first, else opens Settings; `ui_toggle` on each switch; Settings has Reduce flashing. **Others' cells:** "the clock pauses" with Settings open is FLOW's pause (34). Volume and the other settings rows are BUG-62's. |
| **F4** | **Deferred to PORTALS (Phase B, B12).** Its cells (a countdown matching the domain ±1 s, the rolling cost per row, the red flash at the grace's end, `pc_notify` on a new waiter) all read the switchboard, calibration and waiting-list domain from Saleh's 2026-10-20 Portals design. None of it exists in Phase A. PortalsWindow is leased to PORTALS after UI-PC merges (LEASES). Phase A's part is UI-PC's: the read-only schedule window keeps the kit window and redraws on show. |
| **G1** | **Built:** a small tray clock, not a box (BUG-20). `clock_tick` is an AU cell (FEEDBACK's). |
| **A12** | UI-PC's part is BUG-47b: hints amber and proofs red on the workbench. The desk object, the sparks and the sting are CAM's, CONTENT's and FEEDBACK's. |

## Art request (GPT mailbox; for the orchestrator to post to TO_GPT.md)

FIX_PLAN's art dependencies list "PC placeholder art (UI-PC, BUG-50)". BUGLIST names UI-PC's three placeholders: the bezel, the seal ring and the Orders icons. Each has a fallback, so none holds the demo.

| Item | What to draw | Where it goes | Fallback if it has not arrived at candidate time |
|---|---|---|---|
| **PC bezel** | A front-on CRT monitor bezel, 1240 × 1060 px (2× of the 620 × 530 sprite), in warm ivory plastic. The 4:3 glass hole is fully transparent at `FrameGlass`'s rect. A darker chin carries room for the brand plate, the power button at bottom right and the LED. The style is the kit's 80s/90s cel look. | `pc_frame` (OfficeSceneUIBuilder.PcFrame's `EnsureOfficeShape`), with the red close X (48 px) and the round power button (28 px) | The current code-drawn ivory bezel. It is drawn in the kit's tones with no text, and the adversarial reviewer judges it at candidate time. |
| **Agency seal (the seal ring)** | The agency's round seal, 256 px, transparent, one ink colour | `ArtSlots.AgencySeal` (FormView already reads the slot first) | The code-drawn ring (FormView's built seal) |
| **Orders icons** | One 128 px pictogram per upgrade (`ArtSlots.OrderIcon(id)`) and per branch (`ArtSlots.OrderBranch(branch)`), in the kit's pictogram style | The art slots OrdersWindow reads first | The branch's kit pictogram tile (`UiKitNames.UpgradeTile`), already shown whenever the kit is on. The code-drawn glyph is drawn only without the kit. |

## Data fragments (docs/pending/data/UI-PC/; INTEGRATION applies them in UI-PC's merge commit)

UI-PC edits no file it does not own (FIX_PLAN v1 rule 1). The data this track needs goes through the steward's route (v2 rule 11).

**Order at merge:**
1. Apply the three fragments.
2. Generate World.
3. Run the content validator.
4. Build Office UI. CheckLabelKeysAndRoles must report no missing key.

| Fragment | Target | What |
|---|---|---|
| `ui_strings.json` | `world_source.json` → `ui.strings` (CONTENT's) | **Add** 5 `Full` rows after `taskbar.start`: `taskbar.moreHint`, `startmenu.quitForfeit`, `app.orders.closed`, `settings.reduceFlashing`, `notes.daysNone`. **Change** 5 English values: `taskbar.start` (Start), `app.side.left` / `app.side.right` / `app.pane.shelfHere` (UI-24), and `keys.escape` (the F1 card's Esc line, R3/R4). **Remove** 6 keys that no longer have a reader, from the reading table and all 6 culture tables: `mail.from.times`, `mail.subject.times`, `mail.body.times`, `mail.body.timesArchived`, `mail.link.news`, `startmenu.screenOff`. One **dependent** change, `keys.desk.back`, goes in with DOC-HANDLE's ControlRules change. The rows are entries of an existing list, so ContentSheetMap is unchanged. TEXT-LANG's branch also edits `ui.strings` next to `taskbar.start`: keep both sides' rows. |
| `Desktop_Default.json` | `Assets/Data/Config/Desktop_Default.asset` (listed under no track) | `taskbarHeight` 60 → 72, and `taskbarButtonWidth` 56 → 220. The new DesktopConfigSO fields (`crtFrameSeconds` among them) and the changed window sizes need no edit: the asset holds none of them, so Unity loads them at their code defaults. |
| `so_reader_allowlist.txt` | `tools/qa/so_reader_allowlist.txt` (INTEGRATION-kept) | One line, `DeskConfigSO.crtClockSize`: the retired plate's knob, which DOC-HANDLE deletes (BUG-20b). |

**Until the fragments land, on this branch alone:**
- Start reads "Menu", and the pane tags read the old words.
- The taskbar is 60 u and its buttons 56 u.
- The Quit warning, the Orders closed note, the Reduce flashing label and Notes' Days empty state draw their keys, and Build Office UI logs CheckLabelKeysAndRoles errors for those keys (5 with `taskbar.moreHint`).
- `tools/qa/editor_runner` fails one test, `SoFieldReadersTests.Project_EveryUnreadSOField_IsOnTheCleanupAllowlist`, on `DeskConfigSO.crtClockSize`.

The house runner is green either way.

## Seams provided and consumed

- **Provided:**
  - `PcFrame.CanOpen`: real, true while the screen is on (DOC-HANDLE's F1 guard).
  - `OrderBook.CancelPendingFor`: final.
  - `WalletEvents.Changed`: it has a listener.
  - `FindingShades`: the proof and hint colours.
  - `FlashPolicy` (with `Configure`): for every flash's owner.
  - `MonitorScreen.PowerOn`.
  - `PcFrame.PictureMoving` (and `PictureMovingChanged`): true while the frame's picture moves. MonitorScreen and DesktopWindowManager read it; any later owner of desktop input should too.
- **Consumed:**
  - `Orders.CanOrder` (CONTENT);
  - `InputGate` (RUN-SAFE);
  - `MotionPreference.Reduced`;
  - `UiText.Pc`.
- **Not consumed:** Transit (see **Needs a ruling**).
- **For FLOW (Esc → Settings at the desk, 34b):** `OfficeViewController.FocusMonitor()`, then `DesktopApps.OpenApp(DesktopAppIds.Settings)`. That is the same Settings window Esc brings forward on the PC.

## Requests to other owners (not edited here)

- **CAM:** hide the DESK [Q] tab while the PC is open (BUG-50a: one way out), or keep it labelled as Q.
- **DOC-HANDLE:**
  - `ControlRules.BackOut`'s `PcOpen → ClosePc` no longer runs while the PC is open: the desktop stamps every back-out with the screen on (`BoothRules.DesktopInteractive`) and with it off (`PcBackOut.ScreenOff`), and under a newsletter the rules already return None. Drop the step, and apply the `keys.desk.back` dependent row with it.
  - Delete `DeskConfigSO.crtClockSize` (BUG-20b).
- **HALL-FX:** `BuildTrayRiver` and `TrayRiverWidth` (`.HelixRiver.cs`) have no caller now that the river left the tray.
- **QA-RIG / INTEGRATION (the root builder and the cleanup list):**
  - drop the DeskButton block and `DeskButtonWidth`;
  - drop the `screenOffButton` and `monitorScreen` wiring in BuildDesktopShell;
  - drop the `crtDateText` lookup;
  - drop the `.Desk.cs` CrtClock lines (DESK-PLAN's);
  - then delete `CrtClock.cs`, `BuildCrtClock`, `CrtClock_Plate.mat`, DesktopShell's two retired fields, `PcScreenClone.clock` and `OfficeUIController.crtDateText`;
  - drop BuildWindowManager's DeskButton sweep;
  - drop the root's `apps` and `browser` arguments to `BuildMailWindow`, then its two parameters.
- **ClickCatcher.cs (unlisted; the orchestrator assigns it):** the PC frame no longer uses `passThrough`, and the only other ClickCatcher (`.Desk.cs`'s viewCatcher) never sets it, so `PassOn` is unreachable. Its class doc still says a click on the room beside the frame closes it, which R3/R4 overrode. If Saleh confirms the inert room (Needs a ruling, item 4), drop `passThrough` and `PassOn` and correct the doc. If he wants a click there to reach the room, keep both: RoomCatcher then gets a ClickCatcher with `passThrough` and no `onClick` (a UI-PC builder change).
- **Mailbox.cs (unlisted Domain file; the orchestrator assigns it):** `Mailbox.ForDays` still builds a `TimesIssue` item that MailDigest always drops. Stop building it, then drop `MailKind.TimesIssue`, `MailLink.News` and `MailSources.News`, with MailboxTests' Times cases. That makes MailDigest's TimesIssue branch dead, so delete it too.
- **NEWS-WEB (MailFeed's next holder, LEASES):** the Times mail and Mail's News link are gone (BUG-50a: the paper is not mailed). If the merged Internet wants a link from Mail, it comes back as NEWS-WEB's own message kind.
- **FEEDBACK:** a dedicated `pc_degauss` take for F1's hum (pc_on plays now), and a `pc_warn` cue for the quit warning (it reuses `ui_error`).
- **CONTENT:** show hint versus proof on the desk line too (DeskInspect reads FindingLook).
- **TEXT-LANG:** the tray's text sizing in `.Kit.cs` reads the tray as one row. If the 72 u bar crowds the two lines, the Readout share needs a stacked case.

## Needs a ruling (orchestrator, or Saleh where noted)

1. **BUG-20, the ≥9 px cap on the desk CRT clone at A1 (v1 pass line).**
   - **Saleh's note 10** asks for the date "in the start toolbar, not a massive box". A Windows-size tray clock is about 4 px cap on the CRT seen from the chair, so a 9 px cap needs a ~45 u clock, which is the box he rejected.
   - **We built his note.** From the chair, the office's own digital clock (written by ShiftClockReadouts) tells the time.
   - **Ask:** accept "the tray clock reads within 1 minute of the shift clock" as the pass line, and drop the 9 px clause.
2. **BUG-32c, Transit (the plan's Consumes).**
   - **Why the PC's open and close do not go through Transit:** Transit offers a CanvasGroup fade and a RectTransform slide, and its P0 stub cuts instantly. Neither can draw a CRT line, nor fade a camera-drawn picture. Consuming it would put the hard cut back until FLOW lands.
   - F1 specifies the device's own effect, and the Reduced Motion fade above covers the no-motion case.
   - **Ask:** accept the PC frame as its own transition, with FLOW's screen changes staying FLOW's.
3. **Desktop_Default.asset's owner.** UI-PC owns `DesktopConfigSO.cs`, and DOC-HANDLE and CONTENT each own their SO and its asset as a pair (rule 2). **Ask:** assign the asset to UI-PC (it is a fragment today).
4. **The room beside the frame (Saleh).** R3/R4 makes a click on the room do nothing, so the room is visible but inert while the PC is open. **Ask:** confirm this, or say if a click there should pass through to the room without leaving. The answer also decides ClickCatcher's pass-through (see **Requests**).

Until items 1 and 2 are ruled, the BUG-20 and BUG-32c pass lines are **not met as written**.

## Open: what stands between the track and done

**Every owned pass criterion is now built in code.** The track is still **not done** under FIX_PLAN rule 5: no recipe has run in Unity (items 3-6), and two pass lines wait on rulings. Each line below names what closes it.

| Item | State | What closes it |
|---|---|---|
| **BUG-58b, legibility ≥16 px at A2** | Built: Body 31 u = 16.1 px at 720p (`PcType.Legible`). Unmeasured, and the larger type may overflow fixed chrome labels. | Recipe 10's measurement, and recipe 5's TMP overflow scan at 0. On a truncation the layouts cannot take: `PcType.Legible = false`, rebuild, and file the per-app fix. |
| **BUG-65i, UI kit ≤25 MB** | Built: BC7 with mips, computed 23.2 MB from the PNG headers. Gate `uipc.kit.memory` added. | `uipc.kit.memory` green after the reimport, and the play memory profile (recipe 1) at ≤25 MB. |
| **BUG-50a, empty states and default sizes** | Built: the Account's backing, Notes' Days empty state, one size for the reading apps. | Recipe 17's captures on days 1, 5 and 12 at A1 and A2, beside the kit sheet, judged by the adversarial reviewer. |
| **CODE-111** (text zoom on scanned pages) | Built: `PageFit` (tested) in `FitToWidth`; a page wider than its pane scrolls sideways. | Recipe 16. |
| **BUG-20, BUG-32c pass lines as written** | Wait on rulings 1 and 2. | The rulings (**Needs a ruling**). |
| **BUG-2b** (microprint in the scan view) | Waits on TOOLS #10. | Recipe 13. |
| **F4** (Portals countdown and waiting list) | PORTALS' (Phase B). | PORTALS' spec. |

## FEATURES.md (for INTEGRATION to fold in)

- **Line 24:** drop the desk CRT's clock plate sentence.
- **Line 26:** "the PC's power knob or the frame's power button darkens the screen". Start ▸ Turn off screen is gone. With the screen off, Esc turns it on and opens Settings.
- **Line 64 (controls):**
  - On the PC, Esc and a right-click run the desktop's chain. Then Esc brings Settings to the front, and a right-click does nothing.
  - The PC is left only with Q or a click on the bezel. A click on the room beside it does nothing.
- **Line 78 (the desktop):**
  - A 72-unit light kit taskbar: Start, one button per open window, then the tray clock.
  - **Window buttons:** the app's glyph and title, 220 u, shrinking to 120 u, then a chevron "» N" (a glyph and a count) whose click brings the next waiting window forward.
  - **Tray clock:** the time over today's date. A click holds today on the workbench once the Investigation app is on the PC. No credits, no Helix River.
  - **Colours:** the kit's look, with no culture colours (21b).
- **Line 79 (the menu):**
  - "Start" with each app's pictogram tile, then Arrange icons and Quit game. The menu fits the entries a day shows.
  - During a shift, Quit game first shows its warning that the day is forfeit, and a second press within 4 s (`quitWarningSeconds`) quits.
- **Line 85 (keys):**
  - Ctrl+K is polled.
  - The EventSystem sends no navigation events while the desktop takes input, so Enter acts once.
  - Esc with nothing to close opens Settings.
  - Shift+F10 with nothing selected opens the desktop menu at the icon area's top left.
  - Enter on a pinned row keeps the ring there.
- **Line 91 (windows):** as line 78 (no culture colours).
- **Line 15 (Orders):**
  - The pointer over a card previews it in the detail card. A click chooses a card, and orders it when it can be ordered (tested: `PcDesktopRulesTests` OrderPick).
  - The action and Enter act on the chosen card only. Each acts once within the double-click time, on its own clock: a card click never refuses the Enter or the button after it. A refused press plays the error cue (`OrderInputs`, tested: `PcDesktopRulesTests`).
  - The zoom bar sits in the header row.
  - The tree keeps its scroll, zoom and chosen card across minimise and restore.
  - On a day `Orders.CanOrder` is false, ordering is refused and greyed, and the header says "Orders are closed today: nothing would arrive".
- **Mail:** the newspaper is not mailed, and only today's directive memo stays (`MailDigest`, tested: `PcDesktopRulesTests`). No message links to the News site.
- **Investigation:**
  - A logged difference that proves nothing is an amber hint; only proofs are red (`FindingShades`, tested: `PcDesktopRulesTests`).
  - The Reference tab opens on a book on today's shelf.
  - The case board's overlay keeps the chosen pair across a new scan (`OverlayPick`, tested).
  - A scan toast that ran out while the app was minimised does not show on restore.
- **New, the PC frame:**
  - From the press frame, the frame and its dark glass fade in (150 ms). The CRT hums 80 ms before the picture, then the picture grows from a line (150 ms). It closes to a line and a dot (120 ms) on a dark glass, the dot glows out over 2 frames, and the frame fades out (150 ms). No step of the frame is a cut (`CrtPower`, tested).
  - With Reduced Motion, the frame and its dark glass fade in, then the picture fades up, 300 ms in all (`CrtPower`, tested).
  - The desktop takes clicks once the picture is whole. The bezel and the keys work from the press frame: Esc during the power-on opens Settings, and a right-click then does nothing.
  - Reversed while it moves (Q during the power-on, a click on the PC while it closes), the frame turns back from where it is, with no jump (`CrtPower.Resume`, tested: `PcDesktopRulesTests`). The closing frame takes no click.
  - The knobs are DesktopConfigSO's.
  - On screens taller than 16:9 the bezel grows to the screen's height.
  - F1 at the desk does nothing while the screen is off (`PcFrame.CanOpen`, tested: UiPcSeamTests).
- **New, Settings:**
  - Reduce flashing Off / On.
  - Every flash goes through `FlashPolicy`: at most `flashesPerSecond` (3) a second, a static tint with Reduce flashing (tested: `PcDesktopRulesTests`).
  - A press on a Settings switch plays the toggle cue, any other PC press the mouse click, one sound per press (`PcPressCue`, tested).
- **UI kit:** compressed BC7 with mips, about 23 MB (UiKitImporter v2; `uipc.kit.compressed`, `uipc.kit.memory`).
- **PC type:** the PC's type scale is sized for 720p: body text 31 u (16 px at 720p), captions 28 u, titles 33 u, headings 38 u.
- **Windows:** Mail, the Citizen Account and Notes open at one size (1060 × 800). Investigation and Orders open maximised. Settings fits above the taskbar.
- **Citizen Account:** its pages sit on the dark document backing.
- **Notes:** while today is the only day, the Days column says "Pages from earlier days show here."
- **Investigation, scanned pages:** Ctrl+= and Settings' Text size grow a scanned page as they grow other text. A page wider than its pane scrolls sideways from its left edge and is never cut (`PageFit`, tested: `PcDesktopRulesTests`).

## Tests

| Check | Result |
|---|---|
| `compile_check_any.py E:\unity\NOPE-gpt` | 0 errors (Assembly-CSharp, Assembly-CSharp-Editor, TimeDeskEditMode) |
| House runner (`NOPE-tools\runner`) | passed 3784, failed 0, on main `4290bd6`. That is 3739 on main + 45 new: 40 in `PcDesktopRulesTests` and 5 in `DesktopLayoutTests`. |
| `python -m unittest discover -s tools/qa -p "test_*.py"` | Ran 35, OK. `ScenarioTests.test_scenarios_use_known_ops` reads `ui_pc_session.json`'s ops from UiPcRigOps' `RegisterOp` names (fix round 3, item 2: it was red on the dotted names). |
| `tools/qa/editor_runner` (QA_PROJECT set) | passed 80, failed 1, 36 NeedsEditor skipped (RUN-SAFE's tests included). The one failure is `SoFieldReadersTests.Project_EveryUnreadSOField_IsOnTheCleanupAllowlist` on `DeskConfigSO.crtClockSize`, which the `so_reader_allowlist.txt` fragment closes at merge. |

**New EditMode tests, 45 in all:**
- `PcDesktopRulesTests` (40):
  - the back-out (3) and the screen-off back-out (2);
  - the debounce (3);
  - the quit guard (3);
  - the inbox digest (2);
  - the flash budget and its knobs (3);
  - the CRT power move: the frame's fade in from its first step, the hum 80 ms ahead of the picture (also when the frame is quicker than the lead), the line, the collapse on a dark glass, the afterglow, the frame's fade out, every 30 fps capture step under a cut's, the Reduced Motion fade with no cut, and the knobs (6);
  - a reversed move picks up where the glass is: Q during the power-on, a press during the collapse, the afterglow and the frame's fade out, and Reduced Motion both ways (3);
  - `PageFit` (CODE-111): the fit at 100 %, the zoom kept, sideways scrolling under the floor (3);
  - `OrderInputs`: a card click never blocks the button or Enter, and each input acts once per double-click time, on its own clock (2);
  - the press cue (1);
  - `OrderPick` (2);
  - `OverlayPick` (2);
  - proof and hint shades (4);
  - the tray date and midnight (1).
- `DesktopLayoutTests` (5): the taskbar overflow (4) and the chevron label (1).

**TDD:** the 15 tests added or rewritten in fix round 1 were written before their code and seen red: the flash knobs, CrtPower's new shape, the screen-off back-out, the press cue, OrderPick, OverlayPick and the chevron label. They failed to compile with CS0722, CS0246 and CS0103, because the types and members did not exist. In fix round 2, the 11 CRT, PageFit and OrderInputs tests were written first and seen red (CS1729, CS0117 and CS1061: no 7-argument CrtPower, no `FrameFirstStep`, `HumStarted`, `Leaving` or `LeaveSeconds`, no `PageFit` or `OrderInputs`). The 20 earlier tests from 60fe324 were written with their rules, not first. In fix round 3, the 3 reversed-move tests were written first and seen red (CS1061: no `CrtPower.Resume`); the OrderInputs test now asserts the button's clock instead of reading `Seconds`.

**Play (in Unity, deferred):** `tools/qa/scenarios/ui_pc_session.json`, with its ops in `Assets/Tests/PlayMode/UiPc/Rig/UiPcRigOps.cs`:
- `uipcTray`, `uipcRoom`, `uipcEsc`, `uipcEscDark`, `uipcPowerOn`, `uipcWindows`, `uipcOrder`, `uipcEnter`, `uipcZoom`, `uipcRestore`, `uipcBalance`, `uipcBezel` (no dots: QA-RIG's offline test reads names matching `RegisterOp("[A-Za-z]+"`).
- Every press, key, chord and wheel notch is an Input System event on the rig's virtual devices. The exceptions are setup, not the input under test: `uipcBalance`'s money arrives by the AddMoney cheat (the "money arrives" event itself, not a UI path), and `uipcPowerOn` sets Reduced Motion for its second pass and puts the player's choice back after.
- Gate checks in `Assets/Editor/QA/Checks/UiPcChecks.cs`: `uipc.kit.compressed` and `uipc.kit.memory` (static), and `uipc.esc`, `.escdark`, `.poweron`, `.room`, `.bezel`, `.tray`, `.windows`, `.order`, `.enter`, `.zoom`, `.restore`, `.balance`, `.transitions` (play). The gate check ids keep their dots; only the op names lost them.

## RESULT

**deferred: Unity.** Every recipe runs on the worktree's persistent editor by job file, with Saleh away from the PC. Put captures in `R7\FIX\UI-PC\` with a RESULT.md of the numbers.

**Before any recipe**, after the merge:
1. Apply the three fragments (**Data fragments**).
2. Generate World.
3. Build Office UI. Check the log: no `CheckLabelKeysAndRoles` error and no integrity error.
4. Let the UI kit reimport (UiKitImporter v2).
5. Commit the kit `.meta` files and OfficeGameplay (INTEGRATION).

| # | Recipe | Pass line |
|---|---|---|
| 1 | **Kit.** `QaJobs.Gate` (static checks), then the Memory Profiler in play (office, PC open) | `uipc.kit.compressed` and `uipc.kit.memory` green (about 23.2 MB as imported). The profile's UI kit textures total ≤25 MB, down from ~92 MB. A 10 s PC `record` with `seqtools flicker` shows no new shimmer. |
| 2 | **PC session.** `Library/QA/job_scenario.txt` = `tools/qa/scenarios/ui_pc_session.json`, job `QaJobs.Scenario` | `harness.txt` ok (CAM's camera known red allowed). All thirteen of UI-PC's `ui_pc_session.*` assertions pass: `tray`, `room`, `esc`, `escdark`, `poweron.full`, `poweron.reduced`, `windows`, `order`, `enter`, `zoom`, `restore`, `balance`, `bezel`. Each `poweron.*` line must read "right-click during the power-on" and "Esc during it": a press that landed after the move measured nothing. `presses` passes. `balance` names the office's credits text; "no readout bound" is a fail to fix in the binder's wiring, not a pass. |
| 3 | **Open and close (BUG-32c, F1).** `python tools/qa/seqtools.py cuts Library/QA/runs/<run>/seq/pc_open --result-dir Library/QA/results`, the same for `pc_close`, and `frozen` on both. Then set Settings ▸ Reduced Motion and repeat the session's open and close with `record`. Run the open at A1 and at A4 (the frame is largest there). | No single-frame full-screen jump (`ui_pc_session.seq.pc_*.cuts`, so `uipc.transitions` is green). **pc_open:** frame 0 shows the bezel and its dark glass faint (about 0.2) over the room. It is whole by about 150 ms, with the hum (`pc_on` in `events.tsv`) about 70 ms after the press. From about 150 ms a thin bright full-width line grows to the whole picture by about 300 ms. No frame shows the whole desktop shrunk inside a strip, and the glass never shows the room through it. **pc_close:** a line, then a dot on a dark glass, then 2 frames of a fading dot, then the frame fading out over the room for about 150 ms. `flash` on pc_open ≤3 flashes/s. **Reduced Motion:** no squeeze. The bezel and the dark glass fade in over the room (150 ms), then the picture fades up from black (150 ms). Closing is the reverse. `cuts` finds no jump. |
| 4 | **Single exit (UI-26)** in day 5's office: Esc ×3 with the Start menu open, then with nothing open; a right-click on the empty desktop; a window's X; a click on the room right of the frame; the power button, then Esc and a right-click with the screen off; Q twice, then Esc and a right-click 2-5 frames after the second Q (full motion and Reduced Motion); Q, then a desk or paper click within 300 ms, and a click on the fading power button; Q during the power-on, and a click on the PC during the collapse, with `record`; the bezel's red X; a click on the bezel band; Q | Only Q and the bezel (band or X) leave. Esc closes the menu, then brings Settings forward, and with the screen off it turns the screen on and brings Settings. A right-click, a window's X and a click on the room never leave. The power button darkens the screen and the frame stays. There is no Back to desk and no Turn off screen. **During the power-on** (both motion settings), Esc brings Settings forward and a right-click does nothing; the PC stays open, and a desktop click lands on nothing while the bezel's X and power button work. **During the close**, the first desk or paper click reaches the room and the fading power button does not toggle the screen. **A reversed move** turns back from where the glass is: `seqtools cuts` finds no jump in either recording, and no frame shows the whole desktop appear at once. |
| 5 | **Taskbar (BUG-20a, 21a).** Stills of the PC on days 1, 5 and 12 (`jumpDay`) at A1 1920×1080 and A2 1280×720, beside `docs/ui-kit/sheet02-pc.png`; the session's `d5_windows` still. Hover and click the clock on day 1 and day 5. | No box clock on the desk CRT. The tray clock is time over date, within 1 minute of the art office's clock. Start reads Start. The Start menu has icons and no blank slab on day 1. `d5_windows` shows labelled buttons plus "» N" and nothing over the tray (`uipc.windows`). The clock is not clickable on day 1 and holds today on day 5. 0 truncations in the TMP overflow scan, 0 LiberationSans (`probe`). |
| 6 | **One press, one sound (BUG-21c, F3).** FEEDBACK's audio events (`events.tsv`, channel `audio`) over the session's clicks, plus five clicks on Settings' switches | Each press on the PC has exactly one press cue: `ui_toggle` on a Settings switch, `mouse_click` anywhere else. A refused (disabled) control adds `ui_error`. Hover cues are allowed. |
| 7 | **Orders on the last day (BUG-28d)**, after CONTENT lands `CanOrder`: `jumpDay 15`, open Orders, click an orderable card | The Order action is greyed, the header reads "Orders are closed today: nothing would arrive", and nothing is charged. |
| 8 | **Orders preview (UI-25)**: choose card A by a click, move the pointer across cards B and C to the action button, click it | The detail card follows the pointer over B and C and shows A again over the button. The click acts on A. |
| 9 | **Proofs and hints (BUG-47b)** on a seed with a "Data differs" pair that proves nothing and a proved deviation (day 5 golden seed; use `cheat ForceAppearance` if needed) | The hint's line is dashed amber, its column plate amber and not bold, and its status line is the note plate. The proof is red and bold. On day 1 the Reference tab shows an introduced book. |
| 10 | **Legibility (BUG-58b)** at A2 1280×720, A3 1440×1080 and A4 1280×1024: stills of Mail, Investigation (Documents, Records), Orders, Settings and Notes | A3/A4: the bezel fills the height, with the room visible to its right. **Smallest body text ≥16 px at A2** (TMP font size × canvas scale; Body 31 u is 16.1 px at 720p). Record the title bars, the forms and the Orders tree's smallest separately. Recipe 5's overflow scan stays at 0 with the legible scale; otherwise set `PcType.Legible = false`, rebuild, and list the overflowing labels. The adversarial reviewer reads a Mail body and an Investigation row aloud from the A2 still. |
| 11 | **Quit warning (BUG-35c).** Mid-shift, Start ▸ Quit game once, wait 5 s, then click it twice; then close and reopen the theme (a culture change) while the warning shows | The first click shows the Warning label (and `ui_error`). It reverts after 4 s. Two clicks quit (play mode ends). A theme refresh while the warning shows keeps the warning's words. Before Start Shift, one click quits. |
| 12 | **Reduce flashing.** Settings ▸ Reduce flashing On, then close and reopen the PC with `record` | The power-on shows a soft tint for about 200 ms, not a flash. `seqtools flash` stays ≤3/s with no full-field flash. |
| 13 | **Microprint (BUG-2b)**, after TOOLS #10 lands: the scanner's hidden-layer view of a ReprintedPaper (days 9-15, `cheat ForceAppearance`) at A2 | Microprint cap ≥12 px at 720p, read by eye from the still. Blocked until TOOLS provides the content. |
| 14 | **Chevron label (21a).** With 9 windows open (the op `uipcWindows`), switch the culture theme (Settings ▸ language, or a history jump that changes the leading culture) | The chevron still reads "» N" with the right count, never `» {0}`. |
| 15 | **Stale toast (CODE-154).** Scan a paper with the Investigation app minimised, wait past `toastSeconds`, restore the app with `record` | No frame of the restored app shows the toast. |
| 16 | **Text zoom on scanned pages (CODE-111).** Day 5, a traveller's passport scanned, Investigation in two panes (the passport left). Stills at Text size 100, 125 and 150 % (Ctrl+= and Settings), and with the findings column open (the narrowest pane) | The passport's words are larger at each step: their TMP size × scale grows by the zoom's ratio (±2 %). No edge of the page is cut by the pane: past the pane's width, a drag moves it sideways from its left edge, and every box is reachable. At 100 % the page fits its pane as before. |
| 17 | **Apps' empty states and sizes (BUG-50a).** Stills of the Citizen Account, Notes and Mail, each from Start, on days 1, 5 and 12 at A1 and A2, beside `docs/ui-kit/sheet02-pc.png` | Mail, the Account and Notes open at the same size. The Account's portrait page sits on the dark backing (no blank paper margin), and the wheel scrolls over the backing. Notes' Days column shows "Pages from earlier days show here." on day 1 and hides it once an earlier day has a page. The reviewer finds no blank slab in any app. |

**Known risks for the Unity pass:**
1. **The tray's two lines in 72 u.** The kit Readout's 0.5 share is capped at 26. If the lines crowd, ask TEXT-LANG for a stacked-tray case.
2. **A click during the power move.** The desktop takes no click while the picture moves: about 300 ms on opening, about 350 ms on closing (`PcFrame.PictureMoving`). A click sent in that time is dropped, never mis-aimed. The keys and the back-out are not: they are the PC's from the press frame (`uipcPowerOn`). The closing frame takes no click either, so a click then reaches the room. The session's click ops wait the move out, and any later op must too.
3. **The amber hint plate.** It is a CanvasRenderer multiplier on the theme's warn plate. Check it reads amber in every culture theme.
4. **The Quit entry's Warning label** sits on the same row as its label and is sized FitNoSmaller(Body). Check it fits the menu's width at A2.
5. **The legible type scale** may overflow fixed-width chrome labels (Settings' rows, the navigator, the taskbar's tray). Recipe 5's overflow scan decides, and `PcType.Legible` is the revert.
6. **A scanned page's sideways scroll** has no horizontal scrollbar: it moves by drag, and from its left edge on each zoom step. Check in recipe 16 that a drag on the page does not pick a box.
