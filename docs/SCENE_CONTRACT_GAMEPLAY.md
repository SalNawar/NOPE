# Office scene contract (art office + gameplay layer)

The office is two scenes loaded together:

| Scene | Owner | What it holds |
|---|---|---|
| `Assets/Scenes/OfficeScene.unity` | the **art side** (Blender/Codex) | the room, the desk, the PC, the props, the lights, the cameras. The **active** scene (its lighting and skybox). |
| `Assets/Scenes/OfficeGameplay.unity` | the **gameplay side**, built only by `Tools > TimeDesk > Build Office UI (HUD + Panels)` | the game: GameManager, the PC desktop and its cameras, the PC frame, the papers, the scanner stand-in, the traveller, the wheel, the click boxes, the office binder. |

Loading the art office by any path (Title → New Run/Continue, Home → Sleep, or
pressing Play on `OfficeScene.unity`) loads the gameplay layer additively on top
(`OfficeScenes`). The builder never opens or writes the art scene, and the game
never edits it: at load, `OfficeSceneBinder` finds the art's places through this
contract and puts the gameplay pieces there (click boxes, papers, the traveller),
at runtime only.

## How a place is found

For each anchor id the binder tries, in order:

1. **`Anchor_{id}`** anywhere in the art scene (an empty GameObject; its position
   and rotation are the place). Put them under one top-level `GameplayAnchors`
   root, never under `ImportedOfficeDress` (its builders rebuild that tree).
2. **Fallbacks**: existing art objects, by root path or bare name (inactive ones
   are skipped). Where a fallback has renderers, their bounds are the place.
3. **A default pose** written in the contract (`Assets/Data/Config/OfficeSceneContract.asset`), for places the art has no object for yet.
4. **Missing**: the piece is hidden or, where the game needs it, a working
   placeholder is used.

Every office load logs one line listing the places found by fallback, and one
warning listing places on a default pose or missing (naming the tool below). The
report of the current art scene: `Tools > TimeDesk > Check Office Scene Contract`.

## The anchors

| Id | Used for | Found today (art 23aa6e1) | The art side may add |
|---|---|---|---|
| `PCScreen` | the PC: its click box opens the PC frame; the desktop is cloned onto its glass | fallback `ImportedOfficeDress/Desk/Retro CRT` | nothing needed. The glass is the renderer, or the submesh whose material, is named `Glass` or `Screen` (case-insensitive); keep that naming on any new PC. |
| `PCPower` | the PC's power knob (click box) | **default**: derived from the glass (bottom right of the bezel) | `Anchor_PCPower` on the real knob |
| `DeskSurface` | the desk plane the papers lie and slide on (height = the art's top) | fallback `HybridOffice/Booth/Finish_Mat` | `Anchor_DeskSurface` (or keep the mat) |
| `Scanner` | the scanner (drop bed, scan pulse) | **default** pose (1.08, 1.06, −0.33); the gameplay layer shows a placeholder flatbed there | a scanner model with `Anchor_Scanner` (the placeholder hides when an art scanner is found) |
| `Traveller` | where the traveller's feet stand (the figure turns to the camera) | **default** (0, 0, 1.6) | `Anchor_Traveller` behind the desk |
| `HandOver` | where handed-over papers slide in from and back to | **default** (0.45, 1.07, 0.95) | `Anchor_HandOver` at the traveller's edge of the desk |
| `NextSign` | READY / NEXT: the click box | fallback `HybridOffice/Booth/Blender_Next` | — |
| `Intercom` | opens the wheel | fallback `ImportedOfficeDress/Desk/Clerk hotline` (then `HybridOffice/Booth/Finish_Intercom`) | — |
| `Stamp`, `Till`, `StabilityMonitor`, `Calendar`, `Clock` | reacting props with tooltips | fallbacks under `HybridOffice/Booth/` | — |
| `Calculator`, `PenPot`, `Stapler` | flavour props (react on click) | fallbacks under `ImportedOfficeDress/Desk/` | — |
| `ReadoutDay`, `ReadoutStability`, `ReadoutCredits`, `ReadoutClock` | the art's TMP texts the game writes (day, stability %, credits, digital shift clock) | bare names `DayNumber`, `StabilityPercent`, `CreditsNumber`, `ShiftClockDisplay` | keep these names on the texts (or add anchors on them). Without them the game shows a small fallback HUD. |
| `ReadoutNext` | the NEXT sign's caption (the game writes the `desk.readyCaption` UI string) | bare name `NextLabel` | — |
| `OfficeCamera` | the camera the player sees through (the 3D raycaster goes on it) | fallback `Main Camera` | — |
| `OfficeVCam` | the Cinemachine camera that frames the office (the game raises its priority) | fallback `Cameras/OfficeVCam` | — |

**For the art side:** `Tools > TimeDesk > Add Gameplay Anchors (art office)`,
run with `OfficeScene.unity` open, adds an empty `GameplayAnchors/Anchor_{id}` at
the current default pose for every place still on a default (today: `PCPower`,
`Scanner`, `Traveller`, `HandOver`). Move them where the art wants them and save.
The gameplay side does not run this tool on the art scene.

## What the art scene must not do (and what the game does about leftovers)

The art scene still carries **leftover gameplay objects** from before the move.
The game switches them off the moment the art office loads (after their `Awake`,
before any `Start`), and `GameManager`/`InvestigationUIController` copies living
in the art scene do nothing. The art side should **delete** them:

- `GameManager`, `DaySystem`, `EventSystem`, `Canvas`, `OfficeOverlayCanvas`
  (the old game, desktop, overlay and input)
- `OfficeRoot` (the old 2D booth; it also carries a `CinemachineCameraRig`
  component, now an empty stub kept only so this object loads without a
  missing-script warning; the stub goes when `OfficeRoot` does)
- `Cameras/MonitorVCam` (the retired push-in camera)
- the `Physics2DRaycaster` on `Main Camera` (the game disables it and adds a
  `PhysicsRaycaster` at runtime)

The Codex tools that write into `OfficeRoot/CRTMonitor` and `MonitorVCam`
(`ConnectMonitor`, `ApplyRebuiltCrt`) still work on those leftovers; once they
are deleted, point those tools at the PC under `ImportedOfficeDress/Desk`.

Other art-side fixes found by the move:

- `Assets/80s_Office/Scripts/ReplaceGameObjects.cs` used `UnityEditor` in a
  runtime folder, which broke player builds. It is now wrapped in
  `#if UNITY_EDITOR` (an art-owned file; keep the guard if it is replaced).
- `Assets/_Recovery/0 (1).unity` is a Unity crash-recovery copy; it is not in
  the build settings. Delete it.

## Rules for the gameplay side

- Build Office UI builds only `OfficeGameplay.unity`, refuses in play mode or
  with unsaved scenes, and lists it right after `OfficeScene` in the build
  settings. It never opens the art scene.
- Two layers are the gameplay layer's: `Interactable` (every click box; the
  office camera's `PhysicsRaycaster` sees only it) and `PCDesktop` (the desktop
  canvas and its two cameras, far below the office; the office camera never draws it).
- Nothing from the gameplay layer is parented into the art scene; the binder
  places gameplay objects by world position.
- The art's own colliders are ignored by input (they are not on `Interactable`).

## Verifying

- `Tools > TimeDesk > Check Office Scene Contract` lists each anchor, its
  source and where it resolved, then the counts.
- Play `OfficeScene.unity`: the console shows the binder's two lines (fallbacks,
  defaults). A changed art scene that loses a fallback shows up there first.
