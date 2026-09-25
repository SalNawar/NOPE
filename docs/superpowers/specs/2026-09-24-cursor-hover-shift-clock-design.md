# Cursor, hover highlight and shift clock: design

*2026-09-24 · approved by Saleh in chat ("yes go ahead") · branch `feat/cursor-hover-shift-clock`*

Two small office-feel features, built before the world-model work: a game cursor with a hover highlight on everything clickable, and a Papers, Please-style real-time shift clock that ends the day at closing time. The clock is also the future driver of time-of-day lighting.

## 1. Cursor and hover highlight

**Behaviour**

- The game shows its own arrow cursor in every scene (Title, Office, Home). Over anything clickable it shows a hand.
- A booth object under the cursor (CRT, READY sign, and any future `Clickable`) gets a white outline hugging its silhouette, like the till in the reference game.
- UI buttons and icons get an XP-style amber outline while hovered (white vanishes on the light windows).
- Non-interactable things (a `Clickable` with `Interactable = false`, a disabled `Selectable`) get the arrow and no outline.

**Design**

- **One persistent `HoverHighlighter`** does both jobs. `InteractionFeedbackBootstrap` creates it at startup from `RunConfig.interactionFeedback` (the same Resources config `RunManager` boots from), so every scene is covered with no per-scene wiring and the cursor never flips back to the OS arrow between scenes. In `LateUpdate` it reads the Input System UI module's own raycast for the pointer (falling back to an `EventSystem` raycast for other modules), and walks the hierarchy for the nearest `Clickable` or `Selectable` only when the hit object changes. When the target changes, it moves the highlight and switches the cursor. Clickable objects need no per-object setup, and runtime-spawned buttons (intercom actions, shelf buttons) are covered automatically.
- **World outline.** For a hovered `Clickable` with a `SpriteRenderer`, the highlighter lazily builds an outline sprite and shows it on a child renderer (`HoverOutline`, sorting order +1, unlit material from settings, tinted with the outline colour). The outline sprite is made by reading the sprite's pixels (a blit to a temporary RenderTexture, so the texture doesn't need Read/Write) and running a distance transform on the alpha. It's cached per renderer and rebuilt when the renderer's sprite changes (for example, the timeline poster swapping art). Generated textures are destroyed with the highlighter. This needs no shader authoring and no transparent padding in the art, and it ignores 2D lighting, so it keeps working once day lighting lands.
- **UI outline.** The highlighter adds a dedicated `HoverUIOutline : UnityEngine.UI.Outline` to the `Selectable`'s target graphic on first hover and toggles it. A dedicated subclass means authored `Outline` components are never hijacked.
- **Cursor.** `Cursor.SetCursor` is called only when the hovered state changes. The textures come from settings. If they're missing, the OS cursor stays and the highlight still works. Whenever the builder swaps a cursor texture, it re-derives the click point from the image's alpha (`CursorHotspot`, pure, tested).
- **Pure math in a testable assembly.** The ring-from-alpha algorithm (`OutlineMask`: chamfer distance transform, anti-aliased ring, margin and pivot maths) lives in a new pure assembly, `TimeDesk.Visuals` (`noEngineReferences`). It isn't game rules, so it doesn't belong in `TimeDesk.Domain`, but it's pure and gets unit tests.
- **Settings: `InteractionFeedbackSO`** (`Assets/Data/Config/InteractionFeedback_Default.asset`, referenced by `RunConfig`) holds the arrow and hand textures with hotspots, the world and UI outline colours, world outline width (world units), UI outline distance, and the unlit outline material.
- **Constraint:** outlines read a rectangular region of the sprite's texture. Clickable sprites therefore need Rectangle atlas packing with no rotation; tight or rotated packing logs a warning and shows no outline.

## 2. Shift clock

**Behaviour**

- When the player presses **Start Shift**, the clock starts at **09:00** and runs in real time to **17:00** over **8 real minutes** (defaults in `GameConfigSO`, "Shift clock" header).
- The day plan's visitor count becomes the **queue size**. Live day plans ramp **8 / 10 / 12** over days 1–3, in the assets and in the content generator, so the clock normally ends the day. The ramp keeps day 1 short of bankruptcy: the worst case is 50 − (5 + 10 + 20×5) = −65 against the −100 threshold, where a flat 12 would allow −105. The shadowed legacy plans `DayPlan_1..3` are unchanged.
- **At closing time:**
  - A traveller already presented (READY pressed) can be finished. After their verdict the day ends.
  - If the next traveller hasn't been called in (READY still armed), the booth closes at once and that traveller is never presented.
  - A slot that starts after closing (for example, a closing that lands during before-case events) closes at once.
- If the queue runs out before closing, the day ends early and the clock stops.
- Scheduled events and forced cases in slots that closing time cut off are listed in a warning for designers.
- **Pauses:** only while a citation slip is on screen. The existing contract, "citation slip pauses the day until acknowledged", now covers the clock too.
- **Displays:** a clock in the monitor's taskbar tray (`HH:MM`, 24-hour), and an analog wall clock in the booth (face, hour hand and minute hand as separate sprites, placeholders generated until art arrives: `clock_face`, `clock_hand_hour`, `clock_hand_minute`).
- **Unique names per day.** With up to 12 travellers and 5–6 names per nation, duplicate names become likely, and Citizen Records returns the first match. Names are therefore unique within a day: an unused pool name is preferred, once the pool is exhausted a suffix is added ("Marcus II"), and a legendary whose name is already taken that day is not rolled.

**Design**

- **`ShiftClock` (TimeDesk.Domain, pure).** It takes start and end minute-of-day plus the real seconds per shift. It has `Start()`, `Tick(realSeconds)`, `Pause()`/`Resume()` (counted, so nested pauses are safe), `Stop()` (halts without closing), `IsClosed`, the `Closed` event (fires exactly once when it reaches the end), `CurrentMinute`, `Progress01` (for lighting), and static helpers `Format(minute)` → `"09:05"` and `HandAngles(minute)` → hour and minute hand degrees (clockwise, 0 = 12 o'clock). The constructor rejects end ≤ start and non-positive duration.
- **`ShiftFlow` (TimeDesk.Domain, pure).** A decision table for closing: `OnClosing(travellerAtDesk)` → `FinishCurrent | CloseNow`. Tests encode the table.
- **`DaySlotSequencer` (TimeDesk.Domain, pure).** The day loop's per-slot state: `CanStartSlot`, `BeginWaiting`, `MarkResolved`, `Advance`, `CloseAfterCurrentSlot`, `CloseNow` (releases a pending wait and flags the slot as abandoned). `DayOrchestrator` keeps the coroutine and events and delegates the state to it. Tests cover closing during before-case events, behind READY, between slots, and with a traveller at the desk.
- **`ReadyGate.Disarm()`.** New. It cancels a pending READY without firing `Released`.
- **`NameRoster` (TimeDesk.Domain, pure).** `Reserve(name)`, `IsTaken(name)` and `Take(pool, randomIndex)` → an unused name, or a suffixed variant ("II", "III", …) when the pool is exhausted. `CaseFactory` gets a fresh roster per `GenerateDayCases`, reserves legendary names, and filters the legendary roll with `IsTaken`.
- **`ShiftClockDriver` (MonoBehaviour).** It owns a `ShiftClock`, is configured from `GameConfigSO` (falling back to defaults with a warning when the config is missing or invalid), ticks with scaled `Time.deltaTime` in `Update`, and re-exposes `Closed`.
- **`DayOrchestrator`** gets `CloseAfterCurrentSlot()` (the loop stops before the next slot) and `CloseNow()` (it also releases the current wait and skips that slot's end and after-case events), both backed by `DaySlotSequencer`. It stays ignorant of the clock.
- **`GameManager`** starts the clock with the day loop (after the briefing). On `Closed`, it applies the `ShiftFlow` decision. With a traveller at the desk (presented, no decision yet), it calls `CloseAfterCurrentSlot()`. Otherwise it disarms the READY gate if it's armed, disables the sign and calls `CloseNow()`. All verdict paths go through `ShowVerdictThen`, which pauses the clock while a citation slip is shown. `HandleDayCompleted` stops the clock.
- **`ShiftClockReadouts` (MonoBehaviour, on OfficeRoot)** polls the driver: it writes the tray text and rotates the wall-clock hands. All references are optional and null-safe.
- **Builder** (`OfficeSceneUIBuilder`, authoritative): it ensures the settings asset (finding `cursor_arrow` and `cursor_hand` textures by name, generating placeholders under `Assets/Art/Generated/Cursors/` if missing, setting cursor import settings and click points) and assigns it to `RunConfig`. It also ensures the `ShiftClockDriver` wired to `GameManager`, the tray clock (the tray re-anchored to four slots), the wall clock with its readouts, and a fixed hit buffer on the booth's `Physics2DRaycaster`. One placeholder-PNG helper serves every generated texture. The built `OfficeScene` and the generated assets ship on the branch, so the feature is never half-live after a merge.

## 3. Out of scope

Time-of-day lighting (it will read `Progress01`), clock art, a pause menu, and changing shift length per day.

## 4. Tests (EditMode, `Assets/Tests/EditMode`)

- `ShiftClockTests`: start, tick and clamp; closes once at the end; pause, nested pause and resume; stop; ticks ignored before start and after close; format; hand angles; constructor guards.
- `ShiftFlowTests`: the closing decision table.
- `ReadyGateTests`: `Disarm` cancels without firing.
- `NameRosterTests`: unique picks, exhaustion suffixes, reserve, `IsTaken`.
- `DaySlotSequencerTests`: the day loop's closing table.
- `CursorHotspotTests`: tip, fingertip, faint anti-aliasing ignored, empty image.
- `OutlineMaskTests`: ring placement, no ring inside the opaque area, margin sizing, pivot shift, anti-alias falloff.

Before merge, tests also run outside Unity (compiled with `dotnet`, reflection runner), then in the Unity Test Runner.

## 5. FEATURES.md changes

- Add: game cursor and hover highlight; shift clock with closing rules; tray and wall clock.
- Change: day length (a fixed visitor count becomes a queue with a closing time).
- Add: unique visitor names per day.

## 6. Revisions after the independent review (2026-09-24)

A four-lens review (logic, Unity runtime, intent audit, regressions), with an adversarial verifier per lens, confirmed 12 distinct issues. All are fixed on this branch:

1. The built `OfficeScene` and the generated assets were missing, so a merge would have shipped longer days with no clock. The scene is now built and committed on the branch.
2. A flat queue of 12 made day-1 bankruptcy possible. The queue now ramps 8 / 10 / 12, and the legacy plans are reverted.
3. The content generator would have reverted the queue sizes. It now writes 8 / 10 / 12.
4. The closing state machine was untested. It moved to `DaySlotSequencer`, with tests.
5. Legendaries could repeat a name within a day. The roll now filters with `NameRoster.IsTaken`, and a clash is logged as an error.
6. Cursor click points ignored the delivered art. They're now derived from the image (`CursorHotspot`).
7. The cursor only worked in the Office scene. It's now a persistent highlighter via `RunConfig`.
8. A second raycast and `GetComponent` calls ran every frame. The highlighter now reuses the module's raycast and caches the hierarchy walk.
9. The UI outline was invisible on light rows. It now uses a separate amber colour with `useGraphicAlpha` off.
10. Tight-packed sprites read the wrong region silently. They now raise an explicit error, which is logged.
11. The builder duplicated its helpers (placeholder PNG writer, texture lookup). They're consolidated.
12. A missing clock or unreached content failed silently. Both now log warnings.

This supersedes the original note that `ShiftFlow` alone covers closing (`ShouldStartSlot` was dropped in favour of the orchestrator's loop, now `DaySlotSequencer`).

## 7. Verification record (2026-09-24, in the branch's own Unity instance)

- **Compile + EditMode suite outside Unity:** 86/86 TimeDesk tests pass.
- **Unity Test Runner (EditMode):** every TimeDesk test passes. The only failure in the run is the third-party UnitySkills `PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly`, which assumes an empty open scene; the automation had OfficeScene open.
- **Builder on OfficeScene:** it adds WallClock, HourHand, MinuteHand and ClockText plus the ShiftClockDriver, ShiftClockReadouts and TMP components, and removes nothing. GameManager.shiftClock, ShiftClockReadouts (driver, tray, hands), RunConfig.interactionFeedback and the raycaster hit buffer (8) are all wired.
- **Play-mode smoke, scripted:**
  - The bootstrap creates the persistent HoverHighlighter.
  - The clock shows 09:00 during the briefing and is not running; the tray shows `09:00` and the hour hand sits at +90°.
  - The GPU readback orientation is correct on DX12.
  - The CRT outline is 150×130 plus a 6 px margin, with its pivot shifted by exactly the margin.
  - Start Shift starts the clock (`09:02` after about 2 s) and arms READY.
  - Closing with the traveller behind READY closes the booth at once and abandons the slot; READY is disarmed and the Shift Ledger shows.
  - Closing with the traveller at the desk finishes the current slot: the day stays open, the decision is scored (+10), then the day ends and the Shift Ledger shows.
  - The only error logged is Unity's own `UnityEditor.Search` indexing exception at editor start.
- **Not yet verified by eye:** how the outline and the cursor swap look under a real mouse, and the cursor in the Title and Home scenes. Both are on the Task 10 checklist for Saleh.
