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
   and rotation are the place; an empty RectTransform's rect is also its size:
   `Anchor_DepartureBoard`). Put them under one top-level `GameplayAnchors`
   root, never under `ImportedOfficeDress` (its builders rebuild that tree).
2. **Fallbacks**: existing art objects, by root path or bare name (inactive ones
   are skipped). Where a fallback has renderers, their bounds are the place; a
   sprite's are its opaque pixels' (its physics shape, which Unity generates
   from the alpha at import: the hall's layers are whole-canvas sprites, so a
   layer's own bounds would be the whole hall).
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
| `NextSign` | the AVAILABLE sign (the art's NEXT sign): the click box that turns the desk available or pauses it | fallback `HybridOffice/Booth/Blender_Next` (beside the mat's back edge since 633e2e5); without it, **default** (0.13, 1.06, 0.16), where the art's sign stands, with the gameplay's READY placeholder | — |
| `Intercom` | opens the wheel | fallback `ImportedOfficeDress/Desk/Clerk hotline`, the phone at the desk's right rear since 633e2e5 (then `HybridOffice/Booth/Finish_Intercom`) | — |
| `Stamp`, `Till`, `StabilityMonitor`, `Calendar`, `Clock` | reacting props with tooltips | fallbacks under `HybridOffice/Booth/` | — |
| `Calculator`, `PenPot`, `Stapler` | flavour props (react on click) | fallbacks under `ImportedOfficeDress/Desk/` | — |
| `ReadoutDay`, `ReadoutStability`, `ReadoutCredits`, `ReadoutClock` | the art's TMP texts the game writes (day, stability %, credits, digital shift clock) | bare names `DayNumber`, `StabilityPercent`, `CreditsNumber`, `ShiftClockDisplay` | keep these names on the texts (or add anchors on them). Without them the game shows a small fallback HUD. |
| `ReadoutNext` | the NEXT sign's caption (the game writes the `desk.readyCaption` UI string, "AVAILABLE", in the label's own ink while the desk is available and in `DeskConfigSO.readyPausedInk` while paused) | bare name `NextLabel` | — |
| `OfficeCamera` | the camera the player sees through (the 3D raycaster goes on it) | fallback `Main Camera` | — |
| `OfficeVCam` | the Cinemachine camera that frames the office (the game raises its priority) | fallback `Cameras/OfficeVCam` | — |
| `DepartureBoard` | the Departure Board (the portals spec v3 BD1-BD5): the day's portal rows, a gameplay text fitted inside it, and its click box with the portals' tooltip | none in the room: the board is skipped there (no default pose) | `Anchor_DepartureBoard` (a sized empty RectTransform) over a board's display |

**For the art side:** `Tools > TimeDesk > Add Gameplay Anchors (art office)`,
run with the knob's art scene open, adds an empty `GameplayAnchors/Anchor_{id}` at
the current default pose for every place still on a default (the room today:
`Scanner`, `Traveller`, `HandOver`). Move them where the art wants them and save.
The gameplay side does not run this tool on the art scene (the one exception is
the hall's, below).

**For the anime hall:** `Tools > TimeDesk > Add Anime Hall Hooks (art office)`
(`AnimeHallHooks`), run with the hall open as the knob's art office, adds what
the hall lacked for the game to be whole (the list at the end of the next
section), leaves alone whatever it already has (a second run changes nothing)
and marks the scene dirty for saving. The gameplay side ran it once, with
Saleh's OK (2026-09-29). A rebuild of the hall from scratch (its first
installer, `AnimeHallLayerInstaller`, was retired on 2026-10-07) would lose these
hooks: run it again afterwards.

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
| `ReadoutDay`, `ReadoutStability`, `ReadoutCredits`, `ReadoutClock`, `ReadoutNext` | `GameplayAnchors/DayNumber`, `StabilityPercent`, `CreditsNumber`, `ShiftClockDisplay`, `NextLabel` | world-space TextMeshPro texts on the boards' display faces (the calendar's paper, the stability monitor's and the clock's glass, the till's display, the NEXT sign's glass), where the static `Preview display — 01 / 100% / 09:00 / NEXT` TextMeshes stood (deleted): dark ink (the art's `211F26`) on the calendar's paper, light digits (its `FFF2D9` ivory) on the dark glasses. `GameplayAnchors` is the scene's **first root**, so these are found before the preserved desk's empty meshes of the same names (copies of the room's texts without their TextMeshPro component), which stay; deleting those would drop the reliance on root order. Since the hall-art pass (2026-10-07) the till's `CreditsNumber` lives inside `Anchor_Till` (it pulses with the till), so `ReadoutCredits` first names its full root path (`OfficeSceneContractSO.TillReadoutPath`): the bare name would find the desk's empty mesh |
| `Scanner`, `Traveller`, `HandOver` | `GameplayAnchors/Anchor_Scanner`, `Anchor_Traveller`, `Anchor_HandOver` | at the room's defaults (the same desk layout), for the art side to move |
| `OfficeCamera` | `Anime hall player preview` | tagged `MainCamera`, with a `CinemachineBrain` (so it follows `OfficeVCam`: move that to move the view), depth 100, culled to layer 29, no AudioListener: the binder orders the PC frame's camera after it and the clone's before it, and adds the gameplay's layers to its culling mask (`OfficeLayers.GameplayMask`) and an AudioListener when no scene has one |
| `OfficeVCam` | `Cameras/OfficeVCam` | a CinemachineCamera at the camera's pose, its lens copied (55°, 0.05 to 400 m), as the room's: the desk view works |
| `DepartureBoard` | `GameplayAnchors/Anchor_DepartureBoard` (an empty RectTransform over the display's opaque pixels, added by Add Anime Hall Hooks); else the fallback `16 Departure board blank display` (its opaque pixels) | the rows sit inside it, inset by `DeskConfigSO.hallBoardInset` (8 % a side) so they stay below the claim strip; the tooltip opens under it |

What the hall carries for the game to be whole since 2026-09-29 (`Add Anime
Hall Hooks`; nothing else reads these names): the five readouts above, the
four static previews gone; `Cameras/OfficeVCam` and the brain on the player
camera, tagged `MainCamera`, for the desk view; `Anchor_Scanner`,
`Anchor_Traveller`, `Anchor_HandOver` (a scanner model under `Anchor_Scanner`
hides the placeholder); `Anchor_DepartureBoard` (since 2026-09-30, Saleh's OK,
the portals spec v3); the daylight's culling mask includes Default and
Interactable, so the papers on the desk are lit by it (and dim with it in the
evening); and, since 2026-09-30 (Saleh: lights, shadows, dust and a full
day-night cycle; `docs/HALL_LIGHTING.md`), the hall's lights: its 58 painted
layers on the `HallBackdrop` layer with URP's `Sprite-Lit-Default` material
(the exterior layer `03` on the `HallSky` sorting layer and the board display
`16` on `HallDisplays`, both listed before Default: the layers are mutually
exclusive masks, so they draw where they did), and a
`HallLighting` root with the `HallBackdrop` (the painted layers drawn through
a 2D Renderer, `Assets/Settings/HallRenderer2D.asset`, by a camera made at runtime, shown behind
everything by the office camera, which never draws that layer itself), the
`HallLightingRig` and its lights (`Plane`, following the presentation: a global
and a sky light, the window shafts, the ceiling fixtures, the screens, the door
signs, a light per portal ring, the piers' shadow casters, the dust; `Desk`: the
green lamp's spot and the PC screen's glow, 3D lights on the art's layer).

Still optional for the art side: group each prop's parts under one parent
(`Retro CRT` with the glass among its children, `Clerk hotline`,
`Blender_Stamp`, …) or put an `Anchor_{id}` over it, so the whole prop takes the
click, the outline and the reaction instead of one part.

## Hooks the gameplay layer offers the art side

Art-side runtime code never reads gameplay objects directly (and never edits
gameplay code or assets); it reads a read-only hook the gameplay side owns. Ask
the gameplay side for a new hook when the art needs one.

| Hook | What it gives | Used by |
|---|---|---|
| `ShiftClockDriver.Live` (`IShiftProgress`) | where the clock's hour stands on the standard day, `StandardProgress01`: 0 at or before GameConfigSO's standard opening (09:00), 1 at or after its closing (17:00), whatever today's own hours (night shifts, `ShiftHours.StandardProgress`: a late shift opens in the evening), and the clock's `MinuteOfDay` (540 is 09:00; a night shift ends at 1440, midnight); null when no gameplay layer is loaded (the art office on its own, edit mode) | `OfficeHallCrowdPalette` (the crowds' morning to evening colours); `AnimeHallShiftLink` (the anime hall's daylight and ambient, and its lights' hour) |

The anime hall's `AnimeHallPresentation` offers `SetTime(normalizedEvening)` and
`SetPan(normalizedPan)` ("gameplay supplies time and pan"). The gameplay layer
drives **time** only: at load the binder adds an `AnimeHallShiftLink` to its own
object when the art office carries a presentation; each frame it reads the hook
above and calls `SetTime` with the hall lights' evening, or without them the
crowds' curve (`CrowdPaletteBlend.Evening` of `StandardProgress01`,
`DeskConfigSO.hallEveningStartsAt` 0.5, `hallEveningFullAt` 0.9: morning until
13:00, full evening from 16:12, on any day), writing only when the value changes. With it
the link sets the colour of the calendar's readout (`ReadoutDay`, the art's
text on the art's paper, which the evening dims to nearly black):
`DeskConfigSO.hallCalendarDayInk` (black) by day and `hallCalendarEveningInk`
(white) from `hallCalendarEveningInkFrom` (0.555) of the evening, so the date
reads 4.5:1 or better all day as drawn; the text's own authored colour is not
used during a shift. Without a gameplay clock the hall keeps the time and the
ink its art authored. Pan is left to the art.

The gameplay layer also drives **the portal rings** (the portals spec v3
VX1-VX7): at load the binder adds an `AnimeHallPortalLink` beside the shift
link; at the day's start it draws a `PortalEffect` inside each ring (the glow
of an open departure portal, the Return Gate's amber spiral, or nothing for a
closed portal: CLOSED or under maintenance; a departure flares it). It never
tints a ring (the art's `SetLayerTint` is not used): each ring's registered
layer also carries the wall, pillar and bay pixels around and below its frame,
so a tint greys that whole disc; every ring keeps the art's colour. The effects are the one
gameplay drawing on the art's **Default** sorting layer: sorting order = the
ring's secure-bay order − 1 (read from the bay's renderer, `FindLayer`), so they
draw under the bay's front fence and panels, the metal ring (the gate frame) and
the painted glass, over the floor and walls seen through the ring; each is
placed on its ring's opaque pixels (its diameter `hallPortalGlowSize` × the
ring's width) and follows them if the art pans. A renamed or renumbered layer
is a `DeskConfigSO.hallPortalLayers` edit; the art side keeps each ring's
centre free of other layers at the bay's order − 1 (at art c75e1fe the orders
the effects share, the stone bust's 37 and each lower ring's order, have no
pixel inside any ring's centre). With the hall's lights the effects move to the
`HallBackdrop` layer, so the 2D pass that draws the painted layers draws them in
that order, and each ring's state also sets its light (on while it shows a
glow or the spiral, off while closed).

The gameplay layer also drives **the hall's lights** (`docs/HALL_LIGHTING.md`):
the shift link hands the hall's `HallLightingRig` the clock's minute each
frame; the rig's day-night cycle (`HallDayCycle`, its knobs
`Assets/Data/Config/HallLighting_Default.asset`) then gives the presentation's
`SetTime` its evening (1 − the daylight) instead of the crowds' curve, and the
traveller's tint its shade. The rig and its lights are hall objects (Add Anime
Hall Hooks places them); the art side may move, retune or duplicate a light
(its `HallLight` says what drives it).

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
- One more layer and one more sorting layer come with the anime hall's lights
  (Add Anime Hall Hooks adds them): the layer `HallBackdrop` (the hall's painted
  layers, the portal rings' effects, the dust and the Light2Ds: only the hall's
  2D backdrop camera draws it, never the office camera) and the sorting layers
  `HallSky` and `HallDisplays`, before `Default` (the hall's exterior, which only
  the sky light reaches; the Departure Board's display, which only the global
  light and the board's own light reach).
- One sorting layer is the gameplay layer's: `Gameplay`, after `Default`, for
  its world sprites and notes (the traveller's figure, the day-1 desk notes).
  Transparent objects sort by sorting layer and order before depth, so the art
  may use any orders on `Default` (the hall's registered layers use 0..57) and
  the traveller still draws in front of them. Art sprites stay on `Default`.
  The one exception: the portal rings' effects draw on `Default` at each ring's
  secure-bay order − 1 (above), under the ring's frame. The Departure Board's
  rows are on `Gameplay`.
- The office camera is the art's, but at load the binder adds the gameplay's
  layers (`Default`, `Interactable`) to its culling mask, removes `PCDesktop`,
  and adds an `AudioListener` when no loaded scene has one. It also orders the
  desktop's two cameras around it: the PC frame's camera right after it (depth
  + 1, so the office's clear never covers the desktop in the frame's glass) and
  the clone camera right before it (depth − 1), so the art may give its camera
  any depth (the hall's is 100, the room's −1).
- Nothing from the gameplay layer is parented into the art scene; the binder
  places gameplay objects by world position.
- The art's own colliders are ignored by input (they are not on `Interactable`).

## Verifying

- `Tools > TimeDesk > Check Office Scene Contract` lists each anchor, its
  source and where it resolved, then the counts.
- Play `OfficeScene.unity`: the console shows the binder's two lines (fallbacks,
  defaults). A changed art scene that loses a fallback shows up there first.
