# UI-PC: a clean desktop with a real taskbar clock

Branch `fix/a-ui-pc`, cut from main `9b8ab5f` (P0: CARVE, CUTS, QA-RIG). Sources: FIX_PLAN.md §3 "UI-PC" and FIX_PLAN_v2.md "UI-PC: delta" (v2 wins), BUGLIST.md, DECISIONS_1019a.md (Saleh's calls; R3/R4: "Esc opens the same Settings window everywhere ... To leave the PC, press Q or click the bezel"; D7: the PC stays in the player's language; D19: News merges into the Internet, NEWS-WEB's), GAME_FEEL_BIBLE rows A12, F1-F4, G1 and gap 2 (FlashPolicy).

**Code-only.** Saleh was on the PC and another agent ran Unity, so nothing here was played. OfficeGameplay.unity is not rebuilt or committed (INTEGRATION's). Every Unity check is in the RESULT section as a recipe on the QA rig.

## Saleh's decided behaviour, as built

- **The PC is always English** (D7 and 2026-10-18). Every PC string goes through `UiText.Pc` (the reading language); the new strings are `Full` tier (English in every culture, no translations). No PC script reads a culture string.
- **Esc opens Settings, never leaves the PC** (R3/R4). On the PC, Esc first runs the desktop's own chain (a menu, the card, the search, a field, the Start menu, a drag, a held value). With nothing left to close it brings Settings to the front (`PcBackOut`). The desktop takes every back-out press, so OfficeControls' back-out never closes the PC.
- **Q or the bezel leave the PC** (R3/R4). The bezel is now a click target with a "Back to desk" hint. Q stays as it is (OfficeControls, DOC-HANDLE's).
- **The Internet waits for NEWS-WEB** (D19, BUG-50b). The Internet app is unchanged. The fallback stays v1's: the designed minimal page, or the app hidden with Saleh's OK at candidate time.

## Changes per BUG id

### BUG-20a: no clock box; a Windows tray clock
- The desk CRT's clock plate is retired. `CrtClock` is an empty marker that never shows. `PcScreenClone` no longer places or shows it, and `BuildCrtClock` builds only an inactive host with two empty texts. Those exist because DESK-PLAN's `.Desk.cs` and the root still look them up (cleanup list below).
- The tray is Windows' clock: the time over today's date (`"09:07"` over `"14 MAR 2150"`). It is the bar's one small glass readout. The time is ShiftClockReadouts' (the shift clock's own format), and the date is `TrayClock.Date` (new, Domain).
- `DesktopConfigSO.taskbarHeight` is 60 → **72 u** (code default and `Desktop_Default.asset`), so the two lines fit.
- **Decision (ours):** v1's pass line "≥9 px cap on the desk CRT clone at A1" contradicts Saleh's note 10 ("when you go inside on the start toolbar, not a massive box"). A Windows-size tray clock cannot reach 9 px cap on the CRT seen from the chair. We built his intent. From the chair, the office's own clock (the art's digital clock, written by ShiftClockReadouts) tells the time.

### BUG-21a: taskbar skin, Start, Start menu, window buttons, dead affordances
- **Tray.** Credits and the Helix River left the tray. The river hangs on the wall monitor; the wallet is the desk till's and the Citizen Account's. The tray holds only the clock.
- **Bar.** The light kit bar now reads as one piece: Start, the window buttons, and the clock glass. Back to desk is gone (BUG-50a).
- **Start label.** "Start" reads Start: `taskbar.start` English text "Menu" → "Start". The culture tables' translations are never shown on the PC.
- **Start menu.**
  - Each app entry has the app's kit pictogram tile.
  - The menu fits the entries a day shows (`DesktopShell.FitMenu`: its layout's preferred height, bottom edge kept above the taskbar), so days 1-4 have no blank slab.
  - "Turn off screen" is gone.
- **Window buttons.** Each shows its glyph and its title (ellipsis, never mid-letter). They shrink together from 220 u to their least, 120 u. Past that, the rest wait behind a chevron (`» 3`) whose click brings the next one forward. The focused window's button always shows.
  - New pure rule: `TaskbarStrip.Fit`.
  - New knobs: `taskbarButtonLeast` and `taskbarChevronWidth`. `taskbarButtonWidth` is 56 → 220.
- **The date affordance** has no underline. The clock button is live only while the Investigation app is on the PC (`InvestigationApp.OnPc`), so it is never a dead control on days 1-4.

### BUG-21b: culture colours vs FEATURES
The PC's colours are replaced by PC-LANG-FX in Phase B, so UI-PC changes no colour. **Fallback (v1 default):** FEATURES lines 78 and 91 are corrected below. The taskbar and title bars keep the kit's deliberate look and take no culture colours.

### BUG-21c: one press, one click sound
On the PC, a press plays only the desktop's `mouse_click` (DesktopWindowManager):
- UiJuice plays no `ui_press`, `ui_release` or `ui_toggle` for a control on the PC desktop's layer. Its hover and its refusal `ui_error` stay. Outside the PC nothing changes.
- ScanLayerSwitch's extra `ui_press` and `ui_tab` on a click are gone.

### BUG-50a: apps and exits
- **Exits.** Q and the bezel leave the PC; Esc brings Settings. The former ways out:
  - The bezel's red X stays: it sits on the bezel and leaves, as the bezel does.
  - A window's X closes only its window (unchanged).
  - The DESK [Q] tab is CAM's, so it is not changed here (request below).
  - Back to desk is removed. `BuildWindowManager` sweeps the `DeskButton` the root still builds.
  - The bezel's power button only powers the screen (it never leaves the PC).
  - Start's "Turn off screen" is removed.
  - A right-click with nothing to close does nothing.
  - **Decision (ours):** a click on the room beside the frame still closes the PC and reaches what it lands on (the traveller, the intercom: the frame's exit catcher). Saleh asked for this pass-through on 2026-09-30, and it is the desk, not a PC control.
- **Mail.** The auto copies are gone (`MailDigest`): the newspaper is not mailed, and only today's directive memo stays. A message opens with one draw, not two.
- **Shortcut card.** It is 960 × 860 → 980 u tall (UI-16).
- **Orders.**
  - Ordering is one click: the pointer over a card shows it in the detail card, and a click on an orderable card orders it.
  - The zoom bar moved from the tree's corner into the header row, right of the wallet line (UI-25).
  - An upgrade's own icon art is no longer overwritten by its band's kit tile (CODE-158).
- **Investigation.**
  - The pane tags are reworded: "Left" / "Right" / "Opens here" → "Left page" / "Right page" / "Links open here" (English only; the PC never shows the translations).
  - The Reference tab opens on a book that is on today's shelf (BUG-47b).
- **Not done in code (visual work for the Unity pass):** Citizen Account's empty third, Notes' empty columns, the per-app default window sizes and empty states. They need eyes on the A1/A2 captures. The recipe is below.

### BUG-58a: app state
- **Wallet signal.** `WalletEvents.Changed` has its first listener: the Citizen Account redraws on every wallet write while it shows, keeping its scroll. Orders already redraws on its signature. The tray has no wallet any more (CODE-121).
- **Orders** (CODE-114, CODE-120):
  - It lays its tree out once, not on every show, so a restored window keeps its scroll, zoom and selection.
  - Its action acts once per input (`ActionDebounce`, the desktop's double-click time). A double-click never orders and then cancels.
- **Notes** keeps the page the clerk was on across a restore on the same day (CODE-160).
- **Enter and navigation (CODE-117).** While the desktop takes input, the EventSystem sends no navigation events. Enter is the desktop's keys only, never also a Submit on the last button clicked, and no automatic navigation runs over the desktop's Selectables.
- **Fake-null (CODE-116).** `?? AddComponent` is replaced with TryGetComponent in ScanLayerSwitch and SiteRenderer.
- **The strip note (CODE-110).** "DEVIATION LOGGED" and "ALREADY DOCUMENTED" stay on the office strip until the next pick: `CompareController.Release` lets the pair go without redrawing the strip.
- **The case board (CODE-112)** keeps one rest position and returns to it when hidden mid-slide.
- **The scan wipe (CODE-153)** hides when the view hides mid-wipe.
- **Hold line on the CRT (CODE-113).** A held value's line follows the pointer only while the desktop takes input.
- **Ctrl+K (CODE-156)** is polled (the card has always listed it).
- **Deferred, needs Unity to check:**
  - CODE-111, text zoom on scanned pages. FitToWidth cancels the pane zoom. The fix needs horizontal scrolling in the page's scroll, which is a builder and layout change best made with the captures open.
  - CODE-154, the scan toast inside the minimised app.
  - CODE-157, case board overlay indices.
  - CODE-156's Shift+F10 menu anchoring and the pinned-row refocus.
  - CODE-160 for the Browser (NEWS-WEB replaces it) and Portals (PORTALS replaces it).

### BUG-58b: PC legibility
The bezel grows to the screen's height on screens taller than 16:9 (`PcFrame.FitBezel`): up to 80 % of the width, never under its designed size. At A4 (5:4) and A3 (4:3) the PC's text is about 16-24 % larger.
- **Not done:** the ≥16 px target at A2 (1280×720, 16:9) needs a larger PC type scale. `PcType.Caption` 24 and `Body` 26 give about 12.5-13.5 px there.
- Raising them reflows every PC layout, so it is not safe without the A2 captures.
- **Proposal for the Unity pass:** Caption 28, Body 30, Title 32. Check the legibility recipe and the TMP overflow scan, and revert any app that overflows.

### BUG-32c: PC open and close
The picture powers on as a CRT (`CrtPower`, GAME_FEEL_BIBLE F1):
- **Open.** It grows from a bright line to full height over 150 ms OutCubic, with a one-frame glow on the glass through `FlashPolicy`.
- **Close.** It squeezes to a line, then a dot, over 120 ms, and only then does the frame hide.
- **Reduced Motion.** The frame fades over 120 ms instead.

Input follows the view on the press frame, and frame 0 is a line, not the full picture. **Decision (ours):** this is the device's own effect, so it does not go through FLOW's Transit (a cut until FLOW). FLOW's screen transitions stay FLOW's.

### BUG-35c: the mid-shift quit warning
During a shift (started, not closed; a pause counts), Quit game first turns into "Quit? Today's shift is forfeit. Click again", with the error cue. A second press within 4 s quits (`QuitGuard`). Closing the menu drops the warning. Outside a shift, one press quits.

### BUG-47b: proof and hint colours; the Reference tab
- **Only proofs are red** (`FindingShades`). A proof is a proved deviation (DiscrepancyLog) or a directive fault's evidence (a broken rule, NotToday, Expired, a missing paper). Any other logged difference is an amber hint:
  - "Data differs" with no proof;
  - NO RECORD;
  - a seal mismatch with no proof.
- **How a hint looks:**
  - on the workbench, a dashed amber line;
  - on the status line, the note's plate;
  - in the findings column, an amber-tinted plate with a headline that is not bold.
- The classification is CONTENT's (FindingRules), unchanged.
- **The Reference tab** opens on the first book on today's shelf.
- **Not mine:** DeskInspect's desk line reads `MatchBoard.Line`'s FindingLook, which is unchanged. CONTENT can show hints there too by reading `FindingShades`.

### BUG-28c: CancelPendingFor
The seam is final, not a stub. A pending order is always today's (an order arrives the next morning), so OrderBook's own same-day cancel and refund is the whole job. The UiPcSeamTests are renamed to match.

### BUG-28d: last-day orders
- `OrderBook.Order` refuses on a day `Orders.CanOrder(day)` is false.
- The Orders window greys the action and the card click.
- An orderable node's state line reads "Orders are closed today: nothing would arrive".
- `CanOrder` is CONTENT's (still the P0 stub, true), so this is measured fully at CONTENT's merge.

### BUG-65i: UI kit memory
UiKitImporter now compresses the kit: CompressedHQ, BC7 on Standalone, mips kept. Its `GetVersion` is 2, so the kit reimports. Every kit `.meta` changes at that reimport, and INTEGRATION commits them with the rebuild. A sprite whose size is not a multiple of 4 stays uncompressed, and Unity's import warning names it for the kit exporter.

**Not done:** atlasing the kit into a SpriteAtlas. That is an asset made in Unity, and BC7 alone is the bulk of the saving.

### BUG-65j: change-driven redraws
- **MatchLines** redraws only on a frame where a box, the pointer (while holding) or a visibility changed. The label's widest word is measured once per label. ShowLink and ShowHold with nothing new do nothing.
- **MatchBoard** keeps the two rows it found and looks again only when one goes or changes key (it used to call FindRow twice every frame).
- **Mail** draws once per open.

### BUG-68b
DesktopWindowManager's `InputGate.Blocked` check (inserted by the carve) is kept.

### BUG-2b (v2: the scanner's microprint view)
**Waits on TOOLS #10.** `ScanLayer.Micro` exists, but TOOLS' microprint content and readability rule for the scan view are not on main. ScanLayerSwitch is ready for a Micro tab once TOOLS provides the line and its size. The recipe is below.

### GAME_FEEL gap 2: FlashPolicy and Reduce flashing
- **`FlashBudget` / `FlashPolicy`** (Domain): at most 3 flashes in any second; with Reduce flashing, every flash is a static tint held 200 ms. Every flash asks the one static budget.
- **`FlashPreference`** keeps the choice in PlayerPrefs and hands it to the policy at start-up.
- **Settings** has a new Reduce flashing Off / On row (`FlashSettingRow`) in the room the cut close-ups row left.
- The first consumer is the CRT's power-on glow. HALL-FX, LIGHT and DETAIN route their flashes through `FlashPolicy.Request(now)`.

## Seams provided and consumed

- **Provided:**
  - `PcFrame.CanOpen`: now real, true while the screen is on (MonitorScreen tells it through `PcFrame.ScreenPowered`). DOC-HANDLE's F1 guard never opens a dark frame.
  - `OrderBook.CancelPendingFor`: final.
  - `WalletEvents.Changed`: has a listener.
  - `FindingShades`: the proof and hint colours.
  - `FlashPolicy`: for every flash's owner.
- **Consumed:**
  - `Orders.CanOrder` (CONTENT);
  - `InputGate` (RUN-SAFE);
  - `MotionPreference.Reduced`;
  - UiText.Pc.
- **Not consumed:** Transit (decision above).
- **For FLOW (Esc → Settings at the desk, 34b):** `OfficeViewController.FocusMonitor()`, then `DesktopApps.OpenApp(DesktopAppIds.Settings)`. That is the same Settings window Esc brings forward on the PC.

## Requests to other owners (not edited here)

- **CAM:** hide the DESK [Q] tab while the PC is open (BUG-50a: one way out), or keep it labelled as Q.
  - `OfficeViewController.FocusDesk` has no caller once INTEGRATION drops the root's DeskButton lines.
- **DOC-HANDLE:**
  - `ControlRules.BackOut`'s `PcOpen → ClosePc` no longer happens while the desktop takes input, because the desktop stamps every back-out. It still runs when the screen is off: Esc then closes the PC, since there is no desktop to show Settings on.
  - Delete `DeskConfigSO.crtClockSize` (BUG-20b; added to `tools/qa/so_reader_allowlist.txt` with that owner).
- **HALL-FX:** `BuildTrayRiver` and `TrayRiverWidth` (`.HelixRiver.cs`) have no caller now that the river left the tray.
- **QA-RIG / INTEGRATION (the root builder):**
  - drop the DeskButton block and `DeskButtonWidth`;
  - drop the `screenOffButton` and `monitorScreen` wiring in BuildDesktopShell;
  - drop the `crtDateText` lookup;
  - drop the `.Desk.cs` CrtClock lines (DESK-PLAN's);
  - then delete `CrtClock.cs`, `BuildCrtClock`, `Assets/Art/Office/Gameplay/Materials/CrtClock_Plate.mat`, DesktopShell's two retired fields, `PcScreenClone.clock` and `OfficeUIController.crtDateText` (FEEDBACK's file);
  - drop BuildWindowManager's DeskButton sweep once the root no longer builds it.
- **FEEDBACK:** the cue for the quit warning reuses `ui_error`. A dedicated `pc_warn` cue is a sound-bank request.
- **CONTENT:** show hint versus proof on the desk line too (DeskInspect reads FindingLook).
- **TEXT-LANG:** the tray's text sizing in `.Kit.cs` (ApplyKitByRole, Tray) reads the tray as the row for both stacked lines. If the Unity pass shows the two lines too large for the 72 u bar, the Readout share for the tray needs a stacked case.

## Data (applied in this commit; the owner of world_source.json, please check)

**Two files outside UI-PC's list are touched, on purpose; the owners should review them:**
- `Assets/Data/World/world_source.json` (CONTENT's in Phase A) carries the string rows below. They can't wait for a fragment: `UiStringsTests.EveryLiteralKeyTheScriptsLookUp_IsInTheReadingTable` fails the offline suite on any literal key that the reading table lacks. We checked this: with the rows reverted, the suite is `passed 3734, failed 1`.
  - The rows are additions plus three value edits, all inside `ui.strings`.
  - TEXT-LANG's branch also edits `ui.strings`, so INTEGRATION may have to resolve a conflict next to `taskbar.start` at merge. Keep both sides' rows.
- `tools/qa/so_reader_allowlist.txt` (INTEGRATION-kept) has one line added, `DeskConfigSO.crtClockSize`. Retiring the clock plate leaves that field with no reader, and `SoFieldReadersTests.Project_EveryUnreadSOField_IsOnTheCleanupAllowlist` says to list it there. Deleting the field is DOC-HANDLE's job (BUG-20b).

All of these are `Full` tier (English in every culture) under `ui.strings`. They are entries of an existing list, so ContentSheetMap is unchanged. **Generate World must run before Build Office UI**: CheckLabelKeysAndRoles errors on keys the generated reading table lacks.

| Key | English | Note |
|---|---|---|
| `taskbar.start` | Menu → **Start** | value only (Flavour; translations untouched, never shown on the PC) |
| `taskbar.more` | `» {0}` | new: the chevron's count |
| `taskbar.moreHint` | More windows | new: the chevron's hint |
| `startmenu.quitForfeit` | Quit? Today's shift is forfeit. Click again | new (BUG-35c) |
| `app.orders.closed` | Orders are closed today: nothing would arrive | new (BUG-28d) |
| `settings.reduceFlashing` | Reduce flashing | new (G3) |
| `app.side.left` / `app.side.right` / `app.pane.shelfHere` | Left page / Right page / Links open here | values only (UI-24) |

`Desktop_Default.asset`: `taskbarHeight` 72, `taskbarButtonWidth` 220.

## FEATURES.md (for INTEGRATION to fold in)

- **Line 24:** drop the desk CRT's clock plate sentence ("from the office view the desk CRT's glass carries a clock plate ...").
- **Line 26:** "the PC's power knob or the frame's power button darkens the screen". Start ▸ Turn off screen is gone.
- **Line 64 (controls):**
  - On the PC, Esc and a right-click run the desktop's chain, then Esc brings Settings to the front and a right-click does nothing.
  - The PC is left with Q or a click on the bezel (or on the room beside it).
- **Line 78 (the desktop):**
  - A 72-unit light kit taskbar: Start, then one button per open window, then the tray clock.
  - **Window buttons:** the app's glyph and title; 220 u, shrinking to 120 u, then a chevron "» N" whose click brings the next waiting window forward. The focused window's button always shows.
  - **Tray clock:** the time over today's date, one glass readout. A click holds today on the workbench once the Investigation app is on the PC. No credits, no Helix River.
  - **Colours:** the taskbar and title bars keep the kit's look and take no culture colours (21b; the colour work is PC-LANG-FX's).
- **Line 79 (the menu):**
  - "Start" with each app's pictogram tile, then Arrange icons and Quit game.
  - The menu fits the entries a day shows.
  - During a shift, Quit game first warns that the day is forfeit, and a second press within 4 s quits.
- **Line 85 (keys):**
  - Ctrl+K is polled.
  - The EventSystem sends no navigation events while the desktop takes input, so Enter acts once.
  - Esc with nothing to close opens Settings.
- **Line 91 (windows):** as line 78 (no culture colours).
- **Line 15 (Orders):**
  - The pointer over a card shows it in the detail card, and a click on an orderable card orders it.
  - Any action acts once per input, within the double-click time.
  - The zoom bar sits in the header row.
  - The tree keeps its scroll, zoom and selection across minimise and restore.
  - On a day `Orders.CanOrder` is false, ordering is refused and greyed: "Orders are closed today: nothing would arrive".
- **Mail:** the newspaper is not mailed, and only today's directive memo stays (`MailDigest`, tested: `PcDesktopRulesTests`).
- **Investigation:**
  - A logged difference that proves nothing is an amber hint (dashed line, tinted plate); only proofs are red (`FindingShades`, tested: `PcDesktopRulesTests`).
  - The Reference tab opens on a book on today's shelf.
- **New, the PC frame:**
  - The picture powers on from a line (150 ms) and collapses to a line and a dot (120 ms); with Reduced Motion it fades (`CrtPower`, tested).
  - On screens taller than 16:9 the bezel grows to the screen's height.
  - F1 at the desk does nothing while the screen is off (`PcFrame.CanOpen`, tested: UiPcSeamTests).
- **New, Settings:** Reduce flashing Off / On. Every flash goes through `FlashPolicy`: at most 3 per second, a static tint with Reduce flashing (tested: `PcDesktopRulesTests`).
- **UI kit:** compressed BC7 with mips (UiKitImporter v2; `uipc.kit.compressed`).

## Tests

| Check | Result |
|---|---|
| `compile_check_any.py E:\unity\NOPE-gpt` | 0 errors (Assembly-CSharp, Assembly-CSharp-Editor, TimeDeskEditMode) |
| House runner | passed 3735, failed 0 (3710 before + 25 new) |
| `tools/qa/editor_runner` (QA_PROJECT set) | passed 69, failed 0, 16 NeedsEditor skipped (UiPcSeamTests' CanOpen test is new and pure) |

**New EditMode tests:**
- `PcDesktopRulesTests` (21):
  - the back-out (3);
  - the debounce (3);
  - the quit guard (3);
  - the inbox digest (2);
  - the flash budget (2);
  - the CRT power line (3);
  - proof and hint shades (4);
  - the tray date and midnight (1).
- `DesktopLayoutTests`, the taskbar overflow (4).
- They cover the plan's test list ("tray clock formatting (24:00 and dates); an Order debounce rule; window-button overflow layout") and every new pure rule. Each was written with its rule and run against it. They were not written first and seen red.

**Play (in Unity, deferred):**
- `tools/qa/scenarios/ui_pc_session.json` with the rig ops in `Assets/Tests/PlayMode/UiPc/Rig/UiPcRigOps.cs`: `uipc.esc`, `uipc.bezel`, `uipc.order`, `uipc.restore`, `uipc.tray`.
- Gate checks in `Assets/Editor/QA/Checks/UiPcChecks.cs`: `uipc.kit.compressed` (static) and `uipc.esc`, `uipc.bezel`, `uipc.tray`, `uipc.order`, `uipc.restore`, `uipc.transitions` (play).

## RESULT

**deferred: Unity.** Every recipe runs on the worktree's persistent editor by job file, with Saleh away from the PC. Put captures in `R7\FIX\UI-PC\` with a RESULT.md of the numbers.

**Before any recipe**, after the merge:
1. Generate World.
2. Build Office UI. Check the log: no `CheckLabelKeysAndRoles` error and no integrity error.
3. Let the UI kit reimport (UiKitImporter v2).
4. Commit the kit `.meta` files and OfficeGameplay (INTEGRATION).

| # | Recipe | Pass line |
|---|---|---|
| 1 | **Kit.** `QaJobs.Gate` (static checks) | `uipc.kit.compressed` green. In the Memory Profiler (play, office, PC open), UI kit textures total ≤25 MB, down from ~92 MB. A 10 s PC `record` with `seqtools flicker` shows no new shimmer |
| 2 | **PC session.** `Library/QA/job_scenario.txt` = `tools/qa/scenarios/ui_pc_session.json`, job `QaJobs.Scenario` | `harness.txt` ok (CAM's camera known red allowed). `ui_pc_session.esc`, `.bezel`, `.tray`, `.order`, `.restore` pass. `presses` passes with no `direct` step |
| 3 | **Open and close (BUG-32c).** `python tools/qa/seqtools.py cuts Library/QA/runs/<run>/seq/pc_open --result-dir Library/QA/results`, the same for `pc_close`, and `frozen` on both | No single-frame full-screen jump (`ui_pc_session.seq.pc_*.cuts`, so `uipc.transitions` is green). Frame 0 of `pc_open` shows a thin bright line, not the full desktop. `flash` on `pc_open` ≤3 flashes/s |
| 4 | **Single exit (UI-26)** in the session's day 5 office: Esc ×3 with the Start menu open, then with nothing open; a right-click on the empty desktop; a window's X; the bezel's red X; a click on the bezel band; the power button; Q | Only Q, the bezel (band or X) and a click on the room beside the frame leave. Esc closes the menu, then brings Settings forward. A right-click and a window's X never leave. The power button darkens the screen and the frame stays. There is no Back to desk and no Turn off screen |
| 5 | **Taskbar (BUG-20a, 21a).** Stills of the PC on days 1, 5 and 12 (`jumpDay`) at A1 1920×1080 and A2 1280×720, placed beside `docs/ui-kit/sheet02-pc.png`. Open 8 windows by DesktopApps (`call`), then still. Hover and click the clock on day 1 and day 5 | No box clock on the desk CRT. The tray clock is time over date, within 1 minute of the art office's clock. Start reads Start. The Start menu has icons and no blank slab on day 1. 8 windows give labelled buttons plus "» N", nothing over the tray, and the chevron brings each window forward in turn. The clock is not clickable on day 1 and holds today on day 5. 0 truncations in the TMP overflow scan, 0 LiberationSans (`probe`) |
| 6 | **One click, one sound (BUG-21c).** FEEDBACK's audio events (`events.tsv`, channel `audio`) over the session's clicks | Each press on the PC has exactly one click cue (`mouse_click`). A refused (disabled) control adds `ui_error`. Hover cues are allowed |
| 7 | **Orders (BUG-58a, 28d).** In the session, after `uipc.order`: press Enter twice (`key Enter` ×2), then Cancel on the detail button by a double-click (`button OrdersWindow.actionButton` ×2) | One order, cancelled once, refunded once: `world.orders` count and money as before the order. When CONTENT lands `CanOrder`, day 15 (`jumpDay 15`) shows the Order action greyed, the state line "Orders are closed today", and no charge after a card click |
| 8 | **Wallet live.** With Citizen Account open and scrolled down, `cheat AddMoney 50` | The statement redraws without losing its scroll, and its balance equals the Orders wallet line |
| 9 | **Proofs and hints (BUG-47b)** on a seed with a "Data differs" pair that proves nothing and a proved deviation (day 5 golden seed; use `cheat ForceAppearance` if needed) | The hint's line is dashed amber, its column plate amber and not bold, and its status line is the note plate. The proof is red and bold. On day 1 the Reference tab shows an introduced book |
| 10 | **Legibility (BUG-58b)** at A2 1280×720, A3 1440×1080 and A4 1280×1024: stills of Mail, Investigation (Documents, Records), Orders, Settings and Notes | A3/A4: the bezel fills the height with the room visible to its right. Smallest body text: measure TMP rect × canvas scale and record it. The ≥16 px target at A2 needs the type-scale proposal; try Caption 28 / Body 30 / Title 32 and keep it only if recipe 5's overflow scan stays at 0 |
| 11 | **Quit warning (BUG-35c).** Mid-shift, Start ▸ Quit game once, wait 5 s, click it twice | The first click shows the forfeit line (and `ui_error`). It reverts after 4 s. Two clicks quit (play mode ends). Before Start Shift, one click quits |
| 12 | **Reduce flashing.** Settings ▸ Reduce flashing On, then close and reopen the PC with `record` | The power-on shows a soft tint for about 200 ms, not a flash. `seqtools flash` stays ≤3/s with no full-field flash |
| 13 | **Microprint (BUG-2b)**, after TOOLS #10 lands: the scanner's hidden-layer view of a ReprintedPaper (days 9-15, `cheat ForceAppearance`) at A2 | Microprint cap ≥12 px at 720p, read by eye from the still. Blocked until TOOLS provides the content |

**Known risks for the Unity pass:**
1. **The tray's two lines in 72 u.** The kit Readout's 0.5 share is capped at 26. If they crowd, ask TEXT-LANG for a stacked-tray case.
2. **Start menu icons in the kit pass.** The tile is tagged Kit, so the theme leaves it.
3. **The amber hint plate.** It is a CanvasRenderer multiplier on the theme's warn plate. Check it reads amber in every culture theme.
4. **The chevron's label.** It is `» {0}` and needs the string table generated.
