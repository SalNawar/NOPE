# Cursor, hover highlight and shift clock: design

*2026-09-24 · approved by Saleh in chat ("yes go ahead") · branch `feat/cursor-hover-shift-clock`*

Two small office-feel features, built before the world-model work: a game cursor with a hover highlight on everything clickable, and a Papers, Please-style real-time shift clock that ends the day at closing time. The clock is also the future driver of time-of-day lighting.

## 1. Cursor and hover highlight

**Behaviour**

- The game shows its own arrow cursor. Over anything clickable it shows a hand.
- A booth object under the cursor (CRT, READY sign, and any future `Clickable`) gets a white outline hugging its silhouette, like the till in the reference game.
- UI buttons and icons on the monitor desktop get a matching outline while hovered.
- Non-interactable things (a `Clickable` with `Interactable = false`, a disabled `Selectable`) get the arrow and no outline.

**Design**

- **One scene component, `HoverHighlighter`**, does both jobs. Each frame it raycasts the pointer through the `EventSystem` (the same raycasters the clicks use), takes the topmost hit, and walks up the hierarchy for an interactable `Clickable` or `Selectable`. When the target changes, it moves the highlight and switches the cursor. Clickable objects need no per-object setup, and runtime-spawned buttons (intercom actions, shelf buttons) are covered automatically.
- **World outline.** For a hovered `Clickable` with a `SpriteRenderer`, the highlighter lazily builds an outline sprite and shows it on a child renderer (`HoverOutline`, sorting order +1, unlit material from settings, tinted with the outline colour). The outline sprite is made by reading the sprite's pixels (a blit to a temporary RenderTexture, so the texture doesn't need Read/Write) and running a distance transform on the alpha. It's cached per renderer and rebuilt when the renderer's sprite changes (for example, the timeline poster swapping art). Generated textures are destroyed with the highlighter. This needs no shader authoring and no transparent padding in the art, and it ignores 2D lighting, so it keeps working once day lighting lands.
- **UI outline.** The highlighter adds a dedicated `HoverUIOutline : UnityEngine.UI.Outline` to the `Selectable`'s target graphic on first hover and toggles it. A dedicated subclass means authored `Outline` components are never hijacked.
- **Cursor.** `Cursor.SetCursor` is called only when the hovered state changes. The textures come from settings. If they're missing, the OS cursor stays and the highlight still works.
- **Pure math in a testable assembly.** The ring-from-alpha algorithm (`OutlineMask`: chamfer distance transform, anti-aliased ring, margin and pivot maths) lives in a new pure assembly, `TimeDesk.Visuals` (`noEngineReferences`). It isn't game rules, so it doesn't belong in `TimeDesk.Domain`, but it's pure and gets unit tests.
- **Settings: `InteractionFeedbackSO`** (`Assets/Data/Config/InteractionFeedback_Default.asset`) holds the arrow and hand textures with hotspots, outline colour, world outline width (world units), UI outline distance, and the unlit outline material.

## 2. Shift clock

**Behaviour**

- When the player presses **Start Shift**, the clock starts at **09:00** and runs in real time to **17:00** over **8 real minutes** (defaults in `GameConfigSO`, "Shift clock" header).
- The day plan's visitor count becomes the **queue size**. Live day plans go to 12, so the clock normally ends the day, not an empty queue.
- **At closing time:**
  - A traveller already presented (READY pressed) can be finished. After their verdict the day ends.
  - If the next traveller hasn't been called in (READY still armed), the booth closes at once and that traveller is never presented.
  - A slot that starts after closing (for example, a closing that lands during before-case events) closes at once.
- If the queue runs out before closing, the day ends early and the clock stops.
- **Pauses:** only while a citation slip is on screen. The existing contract, "citation slip pauses the day until acknowledged", now covers the clock too.
- **Displays:** a clock in the monitor's taskbar tray (`HH:MM`, 24-hour), and an analog wall clock in the booth (face, hour hand and minute hand as separate sprites, placeholders generated until art arrives: `clock_face`, `clock_hand_hour`, `clock_hand_minute`).
- **Unique names per day.** With 12 travellers and 5–6 names per nation, duplicate names become likely, and Citizen Records returns the first match. Names are therefore unique within a day: an unused pool name is preferred, and once the pool is exhausted a suffix is added ("Marcus II").

**Design**

- **`ShiftClock` (TimeDesk.Domain, pure).** It takes start and end minute-of-day plus the real seconds per shift. It has `Start()`, `Tick(realSeconds)`, `Pause()`/`Resume()` (counted, so nested pauses are safe), `Stop()` (halts without closing), `IsClosed`, the `Closed` event (fires exactly once when it reaches the end), `CurrentMinute`, `Progress01` (for lighting), and static helpers `Format(minute)` → `"09:05"` and `HandAngles(minute)` → hour and minute hand degrees (clockwise, 0 = 12 o'clock). The constructor rejects end ≤ start and non-positive duration.
- **`ShiftFlow` (TimeDesk.Domain, pure).** A decision table for closing: `OnClosing(visitorPresented)` → `FinishCurrent | CloseNow`, and `ShouldStartSlot(clockClosed)`. Tests encode the table.
- **`ReadyGate.Disarm()`.** New. It cancels a pending READY without firing `Released`.
- **`NameRoster` (TimeDesk.Domain, pure).** `Reserve(name)` and `Take(pool, randomIndex)` → an unused name, or a suffixed variant ("II", "III", …) when the pool is exhausted. `CaseFactory` gets a fresh roster per `GenerateDayCases`. Legendary names are reserved.
- **`ShiftClockDriver` (MonoBehaviour).** It owns a `ShiftClock`, is configured from `GameConfigSO` (falling back to defaults with a warning when the config is missing or invalid), ticks with scaled `Time.deltaTime` in `Update`, and re-exposes `Closed`.
- **`DayOrchestrator`** gets `CloseAfterCurrentSlot()` (the loop stops before the next slot) and `CloseNow()` (it also releases the current wait and skips that slot's end and after-case events). It stays ignorant of the clock.
- **`GameManager`** starts the clock with the day loop (after the briefing). On `Closed`, it applies the `ShiftFlow` decision: if READY is armed, it disarms the gate, disables the sign and calls `CloseNow()`; otherwise it calls `CloseAfterCurrentSlot()`. A slot that starts while closed also calls `CloseNow()`. All verdict paths go through `ShowVerdictThen`, which pauses the clock while a citation slip is shown. `HandleDayCompleted` stops the clock.
- **`ShiftClockReadouts` (MonoBehaviour, on OfficeRoot)** polls the driver: it writes the tray text and rotates the wall-clock hands. All references are optional and null-safe.
- **Builder** (`OfficeSceneUIBuilder`, authoritative): it ensures the settings asset (finding `cursor_arrow` and `cursor_hand` textures by name, generating placeholders under `Assets/Art/Generated/Cursors/` if missing, and setting cursor import settings), the `HoverHighlighter`, the `ShiftClockDriver` wired to `GameManager`, the tray clock (the tray re-anchored to four slots), and the wall clock with its readouts.

## 3. Out of scope

Time-of-day lighting (it will read `Progress01`), clock art, a pause menu, and changing shift length per day.

## 4. Tests (EditMode, `Assets/Tests/EditMode`)

- `ShiftClockTests`: start, tick and clamp; closes once at the end; pause, nested pause and resume; stop; ticks ignored before start and after close; format; hand angles; constructor guards.
- `ShiftFlowTests`: the closing decision table.
- `ReadyGateTests`: `Disarm` cancels without firing.
- `NameRosterTests`: unique picks, exhaustion suffixes, reserve.
- `OutlineMaskTests`: ring placement, no ring inside the opaque area, margin sizing, pivot shift, anti-alias falloff.

Before merge, tests also run outside Unity (compiled with `dotnet`, reflection runner), then in the Unity Test Runner.

## 5. FEATURES.md changes

- Add: game cursor and hover highlight; shift clock with closing rules; tray and wall clock.
- Change: day length (a fixed visitor count becomes a queue with a closing time).
- Add: unique visitor names per day.
