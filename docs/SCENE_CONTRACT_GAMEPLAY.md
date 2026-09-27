# Office scene contract (art office + gameplay layer)

The office is two scenes loaded together:

| Scene | Owner | What it holds |
|---|---|---|
| the **art office**: `Assets/Art/Office/AnimeHallLayers/AnimeHall.unity` (the anime terminal hall, live since art c75e1fe) or `Assets/Scenes/OfficeScene.unity` (the 3D room) | the **art side** (Blender/Codex/ChatGPT) | the room or hall, the desk, the PC, the props, the lights, the cameras. The **active** scene (its lighting and skybox). |
| `Assets/Scenes/OfficeGameplay.unity` | the **gameplay side**, built only by `Tools > TimeDesk > Build Office UI (HUD + Panels)` | the game: GameManager, the PC desktop and its cameras, the PC frame, the papers, the scanner stand-in, the traveller, the wheel, the click boxes, the office binder. |

**Which art office plays is a knob**: `officeSceneName` on
`Assets/Resources/RunConfig.asset` (`AnimeHall` today; `OfficeScene` for the
3D room). The game loads the art office by that scene name, so the scene may
live anywhere under `Assets` as long as it is in the build list: Build Office
UI keeps the named one enabled right after the title and lists every other
art scene after Home, disabled (`BuildScenes.Order`); the editor tools below
(`Check Office Scene Contract`, `Add Gameplay Anchors`) open the named scene
(`ArtOfficeScene`). Switching the knob and running Build Office UI is the whole
change.

Loading the art office by any path (Title → New Run/Continue, Home → Sleep, or
pressing Play on the art scene) loads the gameplay layer additively on top
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

The fallbacks below are the 3D room's (art ea62550, the desk layout of 633e2e5).
The anime hall's are in the next section.

| Id | Used for | Found today (art ea62550, the desk layout of 633e2e5) | The art side may add |
|---|---|---|---|
| `PCScreen` | the PC: its click box opens the PC frame; the desktop is cloned onto its glass | fallback `ImportedOfficeDress/Desk/Retro CRT` | nothing needed. The glass is the renderer, or the submesh whose material, is named `Glass` or `Screen` (case-insensitive); keep that naming on any new PC. |
| `PCPower` | the PC's power knob (click box) | fallback `ImportedOfficeDress/Desk/Retro CRT/Rebuilt CRT/CRT2_Orange` (the rebuilt CRT's orange power buttons, the monitor's and the system unit's, one mesh: one box over both, outlined on hover); without it, **default**: derived from the glass (bottom right of the bezel, measured on the older CRT study) | `Anchor_PCPower` on the monitor's knob |
| `DeskSurface` | the desk plane the papers lie and slide on (height = the art's top) | fallback `HybridOffice/Booth/Finish_Mat` | `Anchor_DeskSurface` (or keep the mat) |
| `Scanner` | the scanner (drop bed, scan pulse) | **default** pose (1.02, 1.06, −0.46): right of the mat (its left edge on the mat's border), between the calculator and the ink pad, where it touches no prop and its click box covers no other prop's box as the office camera sees it; the gameplay layer shows a placeholder flatbed there (with its feeder tray and analysis lamp while those shop upgrades are owned) | a scanner model with `Anchor_Scanner` (the placeholder hides when an art scanner is found) |
| `Traveller` | where the traveller's feet stand (the figure turns to the camera) | **default** (0, 0, 1.6) | `Anchor_Traveller` behind the desk |
| `HandOver` | where handed-over papers slide in from and back to | **default** (0.05, 1.07, 0.45): right behind the NEXT sign, which hides a paper there from the office camera, so papers slide out from under the sign and back under it. Since the 633e2e5 layout the sign stands against the mat's back edge and the lamp behind it: every path from the far side of the desk to the mat crosses the sign's base, and the old default (0.45, 1.07, 0.95) lay on the lamp's cord, so papers came out of the lamp. This spot touches no prop, and no path from it to the mat or the scanner crosses the lamp | `Anchor_HandOver` at the traveller's edge of the desk, with a clear run to the mat |
| `NextSign` | READY / NEXT: the click box | fallback `HybridOffice/Booth/Blender_Next` (beside the mat's back edge since 633e2e5); without it, **default** (0.13, 1.06, 0.16), where the art's sign stands, with the gameplay's READY placeholder | — |
| `Intercom` | opens the wheel | fallback `ImportedOfficeDress/Desk/Clerk hotline`, the phone at the desk's right rear since 633e2e5 (then `HybridOffice/Booth/Finish_Intercom`) | — |
| `Stamp`, `Till`, `StabilityMonitor`, `Calendar`, `Clock` | reacting props with tooltips | fallbacks under `HybridOffice/Booth/` | — |
| `Calculator`, `PenPot`, `Stapler` | flavour props (react on click) | fallbacks under `ImportedOfficeDress/Desk/` | — |
| `ReadoutDay`, `ReadoutStability`, `ReadoutCredits`, `ReadoutClock` | the art's TMP texts the game writes (day, stability %, credits, digital shift clock) | bare names `DayNumber`, `StabilityPercent`, `CreditsNumber`, `ShiftClockDisplay` | keep these names on the texts (or add anchors on them). Without them the game shows a small fallback HUD. |
| `ReadoutNext` | the NEXT sign's caption (the game writes the `desk.readyCaption` UI string) | bare name `NextLabel` | — |
| `OfficeCamera` | the camera the player sees through (the 3D raycaster goes on it) | fallback `Main Camera` | — |
| `OfficeVCam` | the Cinemachine camera that frames the office (the game raises its priority) | fallback `Cameras/OfficeVCam` | — |

**For the art side:** `Tools > TimeDesk > Add Gameplay Anchors (art office)`,
run with the knob's art scene open, adds an empty `GameplayAnchors/Anchor_{id}` at
the current default pose for every place still on a default (today: `Scanner`,
`Traveller`, `HandOver`). Move them where the art wants them and save.
The gameplay side does not run this tool on the art scene.

## The anime hall (`AnimeHall.unity`, art c75e1fe)

The hall is the art side's registered 2D layers (`Registered hall layers`, an
`AnimeHallPresentation` with 58 sprite layers, a directional daylight and a
point light, all on layer 29) behind the **preserved 3D desk** (`Approved 3D
desk — preserved art`, the 633e2e5 layout as a flat list of `Prop__Material`
mesh parts) and one camera, `Anime hall player preview`. What the contract
finds there, by bare name (one part per prop: the part whose bounds stand for
it; a click box, its hover outline and its reaction cover that part):

| Id | Found | Note |
|---|---|---|
| `PCScreen` | `CRT2_Glass` | the glass itself, so the desktop's clone binds; the click box is the screen face |
| `PCPower` | `CRT2_Orange` | the same knob mesh as the room's |
| `DeskSurface` | `Clean_Blotter__DeskClean_Pad` | the blotter's top (1.069 m) is the paper plane; 1.45 × 1.02 m |
| `NextSign` | `Clean_Next__DeskClean_ABS` | the sign's body |
| `Intercom` | `Clean_Phone__DeskClean_PhoneBody` | |
| `Stamp`, `Till`, `StabilityMonitor`, `Calendar`, `Clock` | `Clean_Stamp__DeskClean_Wood`, `Clean_Till__DeskClean_Green`, `Office_Stability__Plastic_WarmGrey`, `Office_Calendar__Office_TealDark`, `Office_Clock__Plastic_WarmGrey` | |
| `Calculator`, `PenPot`, `Stapler` | `Clean_Calculator__DeskClean_Case`, `Clean_PenPot__DeskClean_ABS`, `Clean_Stapler__DeskClean_Case` | |
| `ReadoutDay`, `ReadoutStability`, `ReadoutCredits`, `ReadoutClock`, `ReadoutNext` | `DayNumber`, `StabilityPercent`, `CreditsNumber`, `ShiftClockDisplay`, `NextLabel` | found, **but they are empty meshes without a TextMeshPro component**, so the game shows its fallback HUD and the boards keep the static `Preview display — 09:00 / 01 / 100% / NEXT` TextMeshes |
| `Scanner`, `Traveller`, `HandOver` | **default** poses | the same desk layout as the room, so the room's defaults hold |
| `OfficeCamera` | `Anime hall player preview` | untagged, culled to layer 29, no AudioListener, no Cinemachine brain: the binder adds the gameplay's layers to its culling mask (`OfficeLayers.GameplayMask`) and an AudioListener when no scene has one |
| `OfficeVCam` | **missing** | the desk view stays off (a warning at load) |

What the hall must carry for the game to be whole (the art side, in
`AnimeHall.unity`; nothing else reads these names):

- the readouts as **TextMeshPro** texts named `DayNumber`, `StabilityPercent`,
  `CreditsNumber`, `ShiftClockDisplay`, `NextLabel` (then delete the four
  `Preview display — …` TextMeshes, which would show stale values under them);
- a Cinemachine camera named `OfficeVCam` under a `Cameras` root (or
  `Anchor_OfficeVCam`) with a `CinemachineBrain` on the player camera, for the
  desk view;
- `Anchor_Scanner`, `Anchor_Traveller`, `Anchor_HandOver` (`Add Gameplay
  Anchors`) where the hall wants them; a scanner model under `Anchor_Scanner`
  hides the placeholder;
- optional: group each prop's parts under one parent (`Retro CRT` with the
  glass among its children, `Clerk hotline`, `Blender_Stamp`, …) or put an
  `Anchor_{id}` over it, so the whole prop takes the click, the outline and the
  reaction instead of one part; the daylight's culling mask may include
  Default and Interactable so the papers and the traveller are lit by it;
  the camera may be tagged `MainCamera`.

## Hooks the gameplay layer offers the art side

Art-side runtime code never reads gameplay objects directly (and never edits
gameplay code or assets); it reads a read-only hook the gameplay side owns. Ask
the gameplay side for a new hook when the art needs one.

| Hook | What it gives | Used by |
|---|---|---|
| `ShiftClockDriver.Live` (`IShiftProgress`) | today's shift progress, `Progress01`: 0 at opening, 1 at closing (`ShiftClock.Progress01`); null when no gameplay layer is loaded (the art office on its own, edit mode) | `OfficeHallCrowdPalette` (the crowds' morning to evening colours); `AnimeHallShiftLink` (the anime hall's daylight and ambient) |

The anime hall's `AnimeHallPresentation` offers `SetTime(normalizedEvening)` and
`SetPan(normalizedPan)` ("gameplay supplies time and pan"). The gameplay layer
drives **time** only: at load the binder adds an `AnimeHallShiftLink` to its own
object when the art office carries a presentation; each frame it reads the hook
above and calls `SetTime` with the crowds' curve (`CrowdPaletteBlend.Evening`,
`DeskConfigSO.hallEveningStartsAt` 0.5, `hallEveningFullAt` 0.9: morning until
13:00, full evening from 16:12), writing only when the value changes. Without a
gameplay clock the hall keeps the time its art authored. Pan is left to the art.

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

No art tool writes these leftovers any more (2026-09-26: `ConnectMonitor` went
with ImportedOfficeBuilder, and Apply Rebuilt CRT Study checks the PC's glass
instead of aligning the old click proxy and push-in camera), so they can go.

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
- One sorting layer is the gameplay layer's: `Gameplay`, after `Default`, for
  its world sprites and notes (the traveller's figure, the day-1 desk notes).
  Transparent objects sort by sorting layer and order before depth, so the art
  may use any orders on `Default` (the hall's registered layers use 0..57) and
  the traveller still draws in front of them. Art sprites stay on `Default`.
- The office camera is the art's, but at load the binder adds the gameplay's
  layers (`Default`, `Interactable`) to its culling mask, removes `PCDesktop`,
  and adds an `AudioListener` when no loaded scene has one.
- Nothing from the gameplay layer is parented into the art scene; the binder
  places gameplay objects by world position.
- The art's own colliders are ignored by input (they are not on `Interactable`).

## Verifying

- `Tools > TimeDesk > Check Office Scene Contract` lists each anchor, its
  source and where it resolved, then the counts.
- Play `OfficeScene.unity`: the console shows the binder's two lines (fallbacks,
  defaults). A changed art scene that loses a fallback shows up there first.
