# Office Scene — Two States + Diegetic Booth — Design

**Date:** 2026-06-21
**Status:** Approved (design); pending implementation plan
**Scene:** `Assets/Scenes/OfficeScene.unity`
**Related:** ALPHA_ROADMAP.md (Phases 1–3), existing investigation/window scripts

---

## 1. Goal & Scope

Reframe `OfficeScene` into a clickable, Papers-Please-style **booth from the agent's POV** with two
camera states, diegetic (in-world) readouts, fully swappable placeholder art, and props that react to
the timeline system.

This work is **presentation + a desktop shell** around the *existing* investigation/verdict logic. Case
generation, scoring, citations, and day flow (`GameManager`, `CaseFactory`, `ShiftScoring`,
`DayFlowUIController`) are **unchanged** — they are re-hosted, not rewritten.

### In scope (MVP)
- Two camera states: **Office Focus** (default) and **Monitor Focus**.
- Booth set dressing as swappable sprites: back wall, angled left/right partition walls (privacy booth),
  desk, decor, queue + animated traveller (placeholder).
- Two world-space interactables: **CRT monitor** (→ Monitor Focus) and **Ready sign** (→ request next case).
- Diegetic readouts replacing the overlay HUD: **Day** (wall calendar), **Stability** (TVA-style timeline
  monitor), **Credits** (cash-register till + "ding" on increase).
- A fake-OS **desktop shell** (screen-space) inside Monitor Focus: desktop icons, draggable windows with
  minimize/maximize/close, a taskbar with **Start** → **Settings** (empty stub) + **Power** (quit).
- Timeline-reactive props that swap sprites based on the current timeline (`TimelineCueReceiver`, `Visuals`).
- Editor tooling to build/wire it idempotently.

### Out of scope (now)
- Final art (everything ships as placeholder swappable sprites).
- Real Settings content; audio beyond the till ding.
- Curved-glass CRT shader; full traveller animation set (placeholder static / 2-frame is fine).

---

## 2. Visual Reference (agreed in brainstorming)

- **State A — Office Focus (default):** agent POV. Angled left + right **partition walls** form a privacy
  booth around the desk. Background queue of people; an **animated 2D traveller sprite** at the stand
  beyond the counter. Desk holds memorabilia. A **beige CRT monitor** (ViewVision-style, on a swivel base)
  sits on the **right** of the desk, **tube on the right edge, glass turned left toward the player**. A
  **standing office placard sign** ("READY / NEXT") is centered on the desk.
- **State B — Monitor Focus:** camera pushes into the CRT; a crisp full-screen desktop UI appears.

---

## 3. Scene Structure

World-space 2D (URP 2D). Hierarchy under a single `OfficeRoot`:

```
OfficeRoot
├─ Booth/            backWall, leftPartition, rightPartition, desk   (SpriteRenderer prefabs)
├─ Decor/            memorabilia, mug, pinned posters                (some are TimelineReactiveSprite)
├─ Queue/            backgroundLine, TravellerSprite
├─ Interactables/    CRTMonitor (Clickable), ReadySign (Clickable)
├─ Readouts/         DayCalendar, StabilityMonitor, CreditsTill
└─ Cameras/          OfficeVCam, MonitorVCam            (Cinemachine 3.1.x)

Main Camera          + CinemachineBrain + Physics2DRaycaster
EventSystem          (existing — InputSystemUIInputModule)
DesktopCanvas        Screen Space – Overlay, disabled by default — OS shell + windows
```

Every visual element is a **prefab** under `Assets/Prefabs/Office/` whose `SpriteRenderer` references a
placeholder sprite under `Assets/Art/Office/Placeholder/`.

---

## 4. Camera & View State Machine

`OfficeViewController` (MonoBehaviour) with `enum OfficeView { OfficeFocus, MonitorFocus }`.

- **Cinemachine 3.1.x**: `OfficeVCam` and `MonitorVCam`; `CinemachineBrain` on Main Camera blends on
  priority change.
- Tap `CRTMonitor` → `SetView(MonitorFocus)`: raise `MonitorVCam` priority; on blend complete, enable
  `DesktopCanvas` and route input to it.
- Desktop "back" / click-out / minimize-all → `SetView(OfficeFocus)`: disable `DesktopCanvas`, lower
  priority.
- While in `MonitorFocus`, office-only interactables (e.g. `ReadySign`) ignore clicks.
- Exposes `CurrentView` and a `ViewChanged` event so readouts / audio can respond.

---

## 5. Desktop OS Shell (screen-space)

`DesktopController` on `DesktopCanvas`. Responsibilities: desktop icons, taskbar, window lifecycle.

- **Taskbar**: `Start` button → start menu with **Settings** (opens an empty stub window) and **Power**
  (`Application.Quit()`; logs a no-op note in the editor). Optional clock label (cosmetic).
- **Window chrome**: reusable `OSWindowChrome` prefab (title bar + minimize / maximize / close buttons)
  wrapping the existing `DraggableWindow` (drag/bring-to-front/stow). Close hides/returns the window to
  its icon; minimize stows to taskbar; maximize toggles size.
- **Icons** (each opens/focuses a window; document windows reuse `DocumentWindowController`,
  reference windows reuse `ReferenceBookWindowController`, comparison uses `CompareController`):

  | Icon | Opens | Source |
  |---|---|---|
  | Passport | Document window | visitor's passport doc |
  | Permit | Document window | visitor's permit doc |
  | Currency | Reference window | `RefBook_Currency` (ReferenceBookSO) |
  | Language | Reference window | `RefBook_Language` |
  | Technology | Reference window | `RefBook_Technology` |
  | Travel Rules | Rules window | day's active `TravelRuleSO`s |
  | Compare | Compare tool window | `CompareController` |
  | Scanner | Scanned-document window | the document(s) submitted by the current traveller |
  | Internet | News window (placeholder) | news lines via `TimelineEffects.GetLines(..., NewsLine)` |

  > The single "Reference Book" is **split by function** into Currency / Language / Technology icons.
  > Internet/News is a placeholder shell now; content wires to the existing newsletter/news lines.

The desktop **re-hosts** the existing `InvestigationUIController` window set; it does not replace the
verdict flow. Era-choice / verdict submission remains driven by the existing controllers.

---

## 6. Diegetic Readouts

`OfficeReadouts` MonoBehaviour, fed from `WorldState` (the same fields `OfficeUIController.UpdateHud`
consumes — money, timelineStability, day). Whatever updates the HUD today also calls `OfficeReadouts`.

- **Day → `DayCalendar`**: TMP number over a calendar sprite.
- **Stability → `StabilityMonitor`**: TMP percent + sprite-state swap (green / amber / red thresholds)
  on the TVA-style device.
- **Credits → `CreditsTill`**: TMP value on a register sprite; an `AudioSource` plays a **ding** when the
  displayed value increases (tracks last value).

The overlay HUD fields on `OfficeUIController` become unused for display but remain (null-safe) so nothing
breaks; the diegetic objects are the wired targets.

---

## 7. Swappable-Sprite Convention

- Visual element = prefab under `Assets/Prefabs/Office/`; `SpriteRenderer` → placeholder sprite under
  `Assets/Art/Office/Placeholder/`.
- Final art = replace the placeholder sprite asset (same name) or reassign the prefab field. **No code or
  scene-graph changes required.**
- Interactables carry `BoxCollider2D` + `Clickable` (a serialized `UnityEvent onClick`) so click targets
  are freely re-wired in the inspector.

---

## 8. Timeline-Reactive Props

`TimelineReactiveSprite : TimelineCueReceiver` (channel = `Visuals`).

- Inspector: a list of `{ cueId : string → Sprite }` plus an optional default sprite and an optional
  "active toggle" / tint mode.
- `OnCuesChanged(IReadOnlyList<string> cues)` selects the first mapping whose `cueId` is present and
  assigns it to the `SpriteRenderer`; falls back to default when none match.
- Reads only `WorldState` + `TimelineEffects` (via the base class `Refresh()`), per locked conventions.
  Refreshes on day load (`Start`) and on demand (debug tools).
- Drop on "x elements": wall posters, queue silhouettes, decor, an ambient lighting/tint object. Designers
  author which props react and to which cues; cue strings are emitted by `EffectSO` `Cue` ops.

---

## 9. Input

- Add `Physics2DRaycaster` to the office camera.
- `Clickable : MonoBehaviour, IPointerClickHandler` raises its `UnityEvent`. Reuses the existing
  `EventSystem` + `InputSystemUIInputModule` (new Input System), consistent with current UI.

---

## 10. Editor Tooling

Extend `Tools > TimeDesk > Build Office UI` (`Assets/Editor/OfficeSceneUIBuilder.cs`) to generate and wire:
booth + decor + queue prefabs, the two interactables, the two Cinemachine vcams + brain + raycaster, the
three readout objects, and the `DesktopCanvas` shell (icons, taskbar, start menu, window-chrome template).
Idempotent — finds existing pieces by name; safe to re-run.

---

## 11. New / Changed Files (anticipated)

**New runtime scripts** (`Assets/Scripts/Office/` unless noted):
- `OfficeViewController.cs` — two-state camera machine.
- `OfficeReadouts.cs` — diegetic Day/Stability/Credits.
- `Clickable.cs` — world-sprite click → UnityEvent (`Assets/Scripts/UI/` or `Office/`).
- `DesktopController.cs` — OS shell (icons, taskbar, start menu).
- `OSWindowChrome.cs` — min/max/close around `DraggableWindow`.
- `TimelineReactiveSprite.cs` — `Assets/Scripts/Timeline/` (subclasses `TimelineCueReceiver`).

**Changed:**
- `OfficeSceneUIBuilder.cs` — extend the editor builder.
- Light touch to whatever calls `OfficeUIController.UpdateHud` so it also updates `OfficeReadouts`.

**New assets:** placeholder sprites under `Assets/Art/Office/Placeholder/`; prefabs under
`Assets/Prefabs/Office/`; a till "ding" placeholder `AudioClip`.

**Package:** `com.unity.cinemachine` 3.1.7 (already added).

---

## 12. Open Items / Future
- Era-specific reference content and news wiring are placeholders now (Internet icon shows news lines only).
- Traveller animation is placeholder; full sprite animation is a later art pass.
- Settings window content is deferred.
