# UI-PC: a clean desktop with a real taskbar clock

Branch `fix/a-ui-pc`, cut from main `9b8ab5f` (P0: CARVE, CUTS, QA-RIG). Two commits: `60fe324` (the track) and the fix round below (the review of `60fe324`).

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
| 7 | **Owned pass criteria unmet.** | Stated under **Open: the track is not done** and **Needs a ruling**. Three items are closed in this round: CODE-157 (the overlay pair is kept by key, tested), CODE-156's Shift+F10 anchoring and pinned-row refocus, and CODE-154's stale toast. |
| 8 | **The standing harness.** | `ui_pc_session.json` now plays the plan's whole session by player input (Input System events, never a handler called directly): `uipc.windows`, `uipc.enter`, `uipc.zoom`, `uipc.balance`, `uipc.room`, `uipc.escdark`, plus the earlier five. `uipc.windows` opens 8+ windows from Start and walks the chevron. `uipc.restore` scrolls with the wheel, minimises with the title bar's button and restores with the taskbar button. The `uipc.order` hint now says `cheat AddMoney`. Each op has a gate check in `UiPcChecks`. |
| 9 | **Tests.** The tests covered only pure rules, and they were not seen red first. | The rules behind the integration paths are now pure and tested: the screen-off back-out, the press cue, `OrderPick` (what the detail card shows versus what the action acts on), the chevron label, `OverlayPick`, and the CRT and flash knobs. All 15 new tests were written first and seen red (CS0722, CS0246 and CS0103: the types did not exist) before the code. The Unity-side wiring is the harness's (item 8). |
| 10 | **Minor.** CrtPower's, FlashBudget's and QuitGuard's numbers were constants. Mail kept a dead Times path. Hover-select moved the Orders action. | **Knobs:** DesktopConfigSO now holds `crtOpenSeconds`, `crtCloseSeconds`, `crtFadeSeconds`, `crtLineHeight`, `crtHumLeadSeconds`, `crtAfterglowFrames`, `flashesPerSecond`, `flashTintSeconds` and `quitWarningSeconds`. PcFrame and DesktopShell read them; `FlashPolicy.Configure` takes the flash pair. **Mail:** the dead Times path is gone from UI-PC's files: MailFeed's News source, Mail's Times wording and its News link. Mailbox.cs still builds the item (it is unlisted, so it is on the cleanup list). **Orders:** the pointer over a card only previews it. Only a click or the arrows choose the card that the action and Enter act on (`OrderPick`). The action button shows only while the detail card shows the chosen card. |

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
- **Not done in code (visual work for the Unity pass):** Citizen Account's empty third, Notes' empty columns, and the per-app default window sizes and empty states. See **Open**.

### BUG-58a: app state
- **Wallet signal.** `WalletEvents.Changed` has its first listener: the Citizen Account redraws on every wallet write while it shows, and keeps its scroll. OrderBook's order and cancel raise it. Orders redraws on its signature. The tray has no wallet any more (CODE-121).
- **Orders** (CODE-114, CODE-120):
  - It lays its tree out once, so a restored window keeps its scroll, zoom and chosen card.
  - Its action acts once per input (`ActionDebounce`, the desktop's double-click time).
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
- **Deferred, needs Unity:**
  - CODE-111 (text zoom on scanned pages): FitToWidth cancels the pane zoom. The fix needs horizontal scrolling in the page's scroll, which is a layout change best made with the captures open.
  - CODE-160 for the Browser (NEWS-WEB replaces it) and Portals (PORTALS replaces it).

### BUG-58b: PC legibility
The bezel grows to the screen's height on screens taller than 16:9 (`PcFrame.FitBezel`): up to 80 % of the width, never under its designed size. At A4 and A3 the PC's text is about 16-24 % larger.

**Not met: the ≥16 px target at A2.** See **Open**.

### BUG-32c: PC open and close (GAME_FEEL_BIBLE F1)
The picture powers on as a CRT (`CrtPower`, with DesktopConfigSO's knobs):
- **Open.**
  - On the press frame, the frame shows with its glass dark while `pc_on` (the degauss hum) starts.
  - After `crtHumLeadSeconds` (80 ms), the picture grows from a bright full-width line (2 %) to full height over 150 ms OutCubic.
  - At the line's start, a one-frame glow plays on the glass through `FlashPolicy`.
- **Close.** The picture squeezes to a line, then a dot, over 120 ms. The dot glows out over 2 frames (the phosphor afterglow), and only then does the frame hide.
- **How the line is drawn.** The frame camera keeps the glass's whole viewport, and its projection is scaled about the centre. The black clear is the tube around the line.
- **Reduced Motion.** The two-half fade (fix round, item 3). Nothing moves, and the picture never cuts.
- **Input** follows the view on the press frame.
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
UiKitImporter compresses the kit: CompressedHQ, BC7 on Standalone, mips kept. Its `GetVersion` is 2, so the kit reimports, and INTEGRATION commits the changed kit `.meta` files with the rebuild.

**Not met yet:** see **Open**.

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
| **F1** | **Built:** opens on the press frame; Q and the bezel leave; Esc = Settings (also with the screen off); the power-on line (150 ms OutCubic) and the collapse to a dot (120 ms); the degauss hum 80 ms before the picture; the 1-frame glow local to the glass; the 2-frame afterglow; no frame-0 pop. **Others' cells:** the camera's Heavy push is CAM's. `pc_on` and `pc_off` exist; a dedicated degauss take is a FEEDBACK sound-bank request. |
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
| `ui_strings.json` | `world_source.json` → `ui.strings` (CONTENT's) | **Add** 4 `Full` rows after `taskbar.start`: `taskbar.moreHint`, `startmenu.quitForfeit`, `app.orders.closed`, `settings.reduceFlashing`. **Change** 5 English values: `taskbar.start` (Start), `app.side.left` / `app.side.right` / `app.pane.shelfHere` (UI-24), and `keys.escape` (the F1 card's Esc line, R3/R4). **Remove** 6 keys that no longer have a reader, from the reading table and all 6 culture tables: `mail.from.times`, `mail.subject.times`, `mail.body.times`, `mail.body.timesArchived`, `mail.link.news`, `startmenu.screenOff`. One **dependent** change, `keys.desk.back`, goes in with DOC-HANDLE's ControlRules change. The rows are entries of an existing list, so ContentSheetMap is unchanged. TEXT-LANG's branch also edits `ui.strings` next to `taskbar.start`: keep both sides' rows. |
| `Desktop_Default.json` | `Assets/Data/Config/Desktop_Default.asset` (listed under no track) | `taskbarHeight` 60 → 72, and `taskbarButtonWidth` 56 → 220. The new DesktopConfigSO fields need no edit: Unity loads them at their code defaults. |
| `so_reader_allowlist.txt` | `tools/qa/so_reader_allowlist.txt` (INTEGRATION-kept) | One line, `DeskConfigSO.crtClockSize`: the retired plate's knob, which DOC-HANDLE deletes (BUG-20b). |

**Until the fragments land, on this branch alone:**
- Start reads "Menu", and the pane tags read the old words.
- The taskbar is 60 u and its buttons 56 u.
- The Quit warning, the Orders closed note and the Reduce flashing label draw their keys, and Build Office UI logs CheckLabelKeysAndRoles errors for those 4 keys.
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
4. **The room beside the frame (Saleh).** R3/R4 makes a click on the room do nothing, so the room is visible but inert while the PC is open. **Ask:** confirm this, or say if a click there should pass through to the room without leaving.

## Open: the track is not done (owned pass criteria still unmet)

| Item | State | What closes it |
|---|---|---|
| **BUG-58b, legibility ≥16 px at A2** | Not met. `PcType.Caption` 24 and `Body` 26 are 12.5 and 13.5 px at 720p (1 u = 0.519 px). 16 px needs Body ≥31 u. | Raising the scale reflows every PC layout, so it needs the A2 captures and the TMP overflow scan. Unity-pass proposal: Caption 28, Body 31, Title 33. Keep it only if recipe 5's overflow scan stays at 0, and revert per app otherwise. |
| **BUG-65i, UI kit ≤25 MB** | BC7 with mips is in. It is unmeasured, and there is no SpriteAtlas. Estimate: BC7 is 1 byte/px against RGBA32's 4, and mips add a third, so ~92 MB becomes ~31 MB. That would still miss 25 MB. | Measure (recipe 1). If it is over, the Unity pass atlases the kit (a SpriteAtlas is an asset made in Unity) or drops the mips on the largest panels that never minify. |
| **BUG-50a, per-app empty states and default sizes** (Citizen Account's empty third, Notes' empty columns) | Not done. They are layout and size choices that need the A1/A2 captures, and the sizes live in the asset (a fragment). | The Unity pass: captures of each app on days 1, 5 and 12, then the size values as a `Desktop_Default.json` fragment update and the empty-state layout in `.Apps.cs`. |
| **CODE-111** (text zoom on scanned pages) | Deferred. | The layout change with the captures open (horizontal scroll in the page's scroll). |
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
  - The action and Enter act on the chosen card only, once per input within the double-click time.
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
  - The press frame shows the frame with a dark glass while the CRT hums. After 80 ms, the picture grows from a line (150 ms). It closes to a line and a dot (120 ms), and the dot glows out over 2 frames.
  - With Reduced Motion, the frame and its dark glass fade in, then the picture fades up (`CrtPower`, tested).
  - The knobs are DesktopConfigSO's.
  - On screens taller than 16:9 the bezel grows to the screen's height.
  - F1 at the desk does nothing while the screen is off (`PcFrame.CanOpen`, tested: UiPcSeamTests).
- **New, Settings:**
  - Reduce flashing Off / On.
  - Every flash goes through `FlashPolicy`: at most `flashesPerSecond` (3) a second, a static tint with Reduce flashing (tested: `PcDesktopRulesTests`).
  - A press on a Settings switch plays the toggle cue, any other PC press the mouse click, one sound per press (`PcPressCue`, tested).
- **UI kit:** compressed BC7 with mips (UiKitImporter v2; `uipc.kit.compressed`).

## Tests

| Check | Result |
|---|---|
| `compile_check_any.py E:\unity\NOPE-gpt` | 0 errors (Assembly-CSharp, Assembly-CSharp-Editor, TimeDeskEditMode) |
| House runner (`NOPE-tools\runner`) | passed 3745, failed 0. That is 3710 on main + 35 new: 30 in `PcDesktopRulesTests` and 5 in `DesktopLayoutTests`. |
| `tools/qa/editor_runner` (QA_PROJECT set) | passed 68, failed 1, 16 NeedsEditor skipped. The one failure is `SoFieldReadersTests.Project_EveryUnreadSOField_IsOnTheCleanupAllowlist` on `DeskConfigSO.crtClockSize`, which the `so_reader_allowlist.txt` fragment closes at merge. |

**New EditMode tests, 35 in all:**
- `PcDesktopRulesTests` (30):
  - the back-out (3) and the screen-off back-out (2);
  - the debounce (3);
  - the quit guard (3);
  - the inbox digest (2);
  - the flash budget and its knobs (3);
  - the CRT power move: the lead, the line, the collapse, the afterglow, the Reduced Motion fade with no cut, and the knobs (4);
  - the press cue (1);
  - `OrderPick` (2);
  - `OverlayPick` (2);
  - proof and hint shades (4);
  - the tray date and midnight (1).
- `DesktopLayoutTests` (5): the taskbar overflow (4) and the chevron label (1).

**TDD:** the 15 tests added or rewritten in this round were written before their code and seen red: the flash knobs, CrtPower's new shape, the screen-off back-out, the press cue, OrderPick, OverlayPick and the chevron label. They failed to compile with CS0722, CS0246 and CS0103, because the types and members did not exist. The 20 earlier tests from 60fe324 were written with their rules, not first.

**Play (in Unity, deferred):** `tools/qa/scenarios/ui_pc_session.json`, with its ops in `Assets/Tests/PlayMode/UiPc/Rig/UiPcRigOps.cs`:
- `uipc.tray`, `uipc.room`, `uipc.esc`, `uipc.escdark`, `uipc.windows`, `uipc.order`, `uipc.enter`, `uipc.zoom`, `uipc.restore`, `uipc.balance`, `uipc.bezel`.
- Every press, key, chord and wheel notch is an Input System event on the rig's virtual devices. The one exception is `uipc.balance`'s money, which arrives by the AddMoney cheat (the "money arrives" event itself, not a UI path).
- Gate checks in `Assets/Editor/QA/Checks/UiPcChecks.cs`: `uipc.kit.compressed` (static), and `uipc.esc`, `.escdark`, `.room`, `.bezel`, `.tray`, `.windows`, `.order`, `.enter`, `.zoom`, `.restore`, `.balance`, `.transitions` (play).

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
| 1 | **Kit.** `QaJobs.Gate` (static checks), then the Memory Profiler in play (office, PC open) | `uipc.kit.compressed` green. UI kit textures total ≤25 MB, down from ~92 MB. A 10 s PC `record` with `seqtools flicker` shows no new shimmer. |
| 2 | **PC session.** `Library/QA/job_scenario.txt` = `tools/qa/scenarios/ui_pc_session.json`, job `QaJobs.Scenario` | `harness.txt` ok (CAM's camera known red allowed). All eleven `ui_pc_session.<op>` assertions pass: `tray`, `room`, `esc`, `escdark`, `windows`, `order`, `enter`, `zoom`, `restore`, `balance`, `bezel`. `presses` passes. |
| 3 | **Open and close (BUG-32c, F1).** `python tools/qa/seqtools.py cuts Library/QA/runs/<run>/seq/pc_open --result-dir Library/QA/results`, the same for `pc_close`, and `frozen` on both. Then set Settings ▸ Reduced Motion and repeat the session's open and close with `record`. | No single-frame full-screen jump (`ui_pc_session.seq.pc_*.cuts`, so `uipc.transitions` is green). **pc_open:** frame 0 shows the bezel with a dark glass. The frames from about 80 ms show a thin bright full-width line growing to the whole picture by about 230 ms. No frame shows the whole desktop shrunk inside a strip. **pc_close:** a line, then a dot, then 2 frames of a fading dot, then the room. `flash` on pc_open ≤3 flashes/s. **Reduced Motion:** no squeeze. The bezel and the dark glass fade in over the room, then the picture fades up from black. Closing is the reverse. `cuts` finds no jump inside the glass. |
| 4 | **Single exit (UI-26)** in day 5's office: Esc ×3 with the Start menu open, then with nothing open; a right-click on the empty desktop; a window's X; a click on the room right of the frame; the power button, then Esc and a right-click with the screen off; the bezel's red X; a click on the bezel band; Q | Only Q and the bezel (band or X) leave. Esc closes the menu, then brings Settings forward, and with the screen off it turns the screen on and brings Settings. A right-click, a window's X and a click on the room never leave. The power button darkens the screen and the frame stays. There is no Back to desk and no Turn off screen. |
| 5 | **Taskbar (BUG-20a, 21a).** Stills of the PC on days 1, 5 and 12 (`jumpDay`) at A1 1920×1080 and A2 1280×720, beside `docs/ui-kit/sheet02-pc.png`; the session's `d5_windows` still. Hover and click the clock on day 1 and day 5. | No box clock on the desk CRT. The tray clock is time over date, within 1 minute of the art office's clock. Start reads Start. The Start menu has icons and no blank slab on day 1. `d5_windows` shows labelled buttons plus "» N" and nothing over the tray (`uipc.windows`). The clock is not clickable on day 1 and holds today on day 5. 0 truncations in the TMP overflow scan, 0 LiberationSans (`probe`). |
| 6 | **One press, one sound (BUG-21c, F3).** FEEDBACK's audio events (`events.tsv`, channel `audio`) over the session's clicks, plus five clicks on Settings' switches | Each press on the PC has exactly one press cue: `ui_toggle` on a Settings switch, `mouse_click` anywhere else. A refused (disabled) control adds `ui_error`. Hover cues are allowed. |
| 7 | **Orders on the last day (BUG-28d)**, after CONTENT lands `CanOrder`: `jumpDay 15`, open Orders, click an orderable card | The Order action is greyed, the header reads "Orders are closed today: nothing would arrive", and nothing is charged. |
| 8 | **Orders preview (UI-25)**: choose card A by a click, move the pointer across cards B and C to the action button, click it | The detail card follows the pointer over B and C and shows A again over the button. The click acts on A. |
| 9 | **Proofs and hints (BUG-47b)** on a seed with a "Data differs" pair that proves nothing and a proved deviation (day 5 golden seed; use `cheat ForceAppearance` if needed) | The hint's line is dashed amber, its column plate amber and not bold, and its status line is the note plate. The proof is red and bold. On day 1 the Reference tab shows an introduced book. |
| 10 | **Legibility (BUG-58b)** at A2 1280×720, A3 1440×1080 and A4 1280×1024: stills of Mail, Investigation (Documents, Records), Orders, Settings and Notes | A3/A4: the bezel fills the height, with the room visible to its right. Smallest body text: measure TMP rect × canvas scale and record it. The ≥16 px target at A2 needs the type-scale proposal (**Open**): try it and keep it only if recipe 5's overflow scan stays at 0. |
| 11 | **Quit warning (BUG-35c).** Mid-shift, Start ▸ Quit game once, wait 5 s, then click it twice; then close and reopen the theme (a culture change) while the warning shows | The first click shows the Warning label (and `ui_error`). It reverts after 4 s. Two clicks quit (play mode ends). A theme refresh while the warning shows keeps the warning's words. Before Start Shift, one click quits. |
| 12 | **Reduce flashing.** Settings ▸ Reduce flashing On, then close and reopen the PC with `record` | The power-on shows a soft tint for about 200 ms, not a flash. `seqtools flash` stays ≤3/s with no full-field flash. |
| 13 | **Microprint (BUG-2b)**, after TOOLS #10 lands: the scanner's hidden-layer view of a ReprintedPaper (days 9-15, `cheat ForceAppearance`) at A2 | Microprint cap ≥12 px at 720p, read by eye from the still. Blocked until TOOLS provides the content. |
| 14 | **Chevron label (21a).** With 9 windows open (`uipc.windows`), switch the culture theme (Settings ▸ language, or a history jump that changes the leading culture) | The chevron still reads "» N" with the right count, never `» {0}`. |
| 15 | **Stale toast (CODE-154).** Scan a paper with the Investigation app minimised, wait past `toastSeconds`, restore the app with `record` | No frame of the restored app shows the toast. |

**Known risks for the Unity pass:**
1. **The tray's two lines in 72 u.** The kit Readout's 0.5 share is capped at 26. If the lines crowd, ask TEXT-LANG for a stacked-tray case.
2. **The squeezed projection and the event camera.** The frame camera is the desktop canvas's event camera. During the 150-230 ms power-on, raycasts map through the squeezed projection, so a click there lands off target. The projection is reset at the end of the move. Recipe 2's `presses` check catches a blocked press.
3. **The amber hint plate.** It is a CanvasRenderer multiplier on the theme's warn plate. Check it reads amber in every culture theme.
4. **The Quit entry's Warning label** sits on the same row as its label and is sized FitNoSmaller(Body). Check it fits the menu's width at A2.
