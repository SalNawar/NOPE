# The desk's plan (desk layout v2)

Date: 2026-10-18. Track LAY. Branch `feat/desk-layout`.

## Why

From Saleh's playtest of demo 1018a:

- Note 8: "we need a better location for the stamp drawer it is in the way".
- Note 12: "you should be able to orgnize your desk by moving stuff around".
- Note 13: "you cant see what each machiene does the camera should be better and the layout of stuff on the desk. every machine should have a space pre planed for it use canva to plan it the re ofgnize the items. the big machience and upgrades the player cant move but small decorations he can".
- Later notes: "reorgnize the desk its so messy. look at restory and learn from it".
- And: "when reorgnizing the desk look at player UX how he transitions between tasks or job it to have least friction and make sure everything is pleasing and satisfying".

The plan was made in Canva (design DAHYVEWVFes, pages 3 and 4: plan v2 and the flow). It drew on what ReStory does: every object has a home, things are grouped by job, the surface is calm, and the tools stay in reach. Saleh approved it on 2026-10-18 ("go with plan v2").

## Saleh's calls

- Every machine and upgrade has a planned space. The player cannot move the machines or the upgrades.
- The player can move the small decorations. The existing art props are used for them for now ("use existing for now this is low prio").
- The office camera: 58 degrees, looking 5 degrees further down ("Yes").
- Machine close-ups: a Settings toggle, on by default.

## Decisions that are mine (LAY)

- The stamp drawer becomes a fixed rack beside the counter. Nothing slides; TAB brings up the reading view over the rack.
- The rack's daters stand up as each case begins and fold down as it ends. This reuses the drawer's cradle motion; the slide is not used.
- The FTUE's "open the stamps" step is removed. The stamp step points at the rack. `GuideAction.StampsOut` is retired: no step uses it, and its number is kept.
- An intercom on the counter strip opens the questions where the player is. The art's phone moves to the comms shelf and still opens them too.
- The shelves and the intercom are built at run time from boxes, in the art's own materials (`DeskClean_Wood`, `DeskClean_PhoneBody`, `DeskClean_Dark`, `DeskClean_Orange`), until the art provides its own models.
- The decor zones are the back left corner, and the right side between the machines and the hall shelf.
- The close-up is triggered by:
  - the scanner reading a paper;
  - the chip reader reading a chip;
  - a click on an upgrade's object.

  It holds while the machine works. Any click or key gives the view back at once. It never starts during a drag.

## §1 The frame

Every bay is a `DeskBay`: a centre, a footprint, a yaw, and whether it stands on a shelf. A bay is given in the desk's own frame (`DeskFrame`):

- the origin is the mat's centre on the desk's top;
- x runs right and y runs away from the chair, along the office view's level axes.

The office binder builds the frame at load and places everything through `DeskFrame.Place`. A camera change therefore never moves a machine. All the numbers are in `DeskConfigSO.layout` (`DeskLayoutSettings`).

## §2 The bays (metres; x right, y away)

| Bay | Centre | Footprint | Yaw | Notes |
|---|---|---|---|---|
| scanner | 0.74, 0.13 | 0.56 x 0.36 | 90 | the near end of its bed is inside the reading view's right edge |
| chipReader | 0.60, -0.155 | 0.21 x 0.21 | 90 | under the scanner; scale 0.9 |
| stampRack | 0.28, 0.24 | 0.32 x 0.12 | 0 | beside the counter, at the papers' top right |
| intercom | 0.50, 0.37 | 0.12 x 0.08 | 0 | on the counter strip's right |
| uvTorch | 0.18, -0.22 | 0.24 x 0.10 | -90 | in the tool pouch under the papers |
| lensKit | -0.06, -0.19 | 0.20 x 0.15 | 0 | in the tool pouch, beside the torch |
| detain | -0.51, 0.74 | 0.26 x 0.28 | 0 | beside the AVAILABLE sign |
| crate | 1.04, -0.52 | 0.48 x 0.34 | 8 | off the mat's front right, delivery mornings only |
| commsShelf | -1.35, 1.29 | 1.90 x 0.34 | | a 5 cm riser holding the phone, BABEL, the Rolodex and the protocol cards |
| hallShelf | 1.57, 1.21 | 1.58 x 0.50 | | holds the RING SERVICE panel; the booth camera and the X-ray bays are kept empty |
| tuner, thumbprint | 1.22, 0.06 / 1.10, -0.12 | | | the later machines' bays, kept empty |

The papers' reading area (`DeskConfigSO.readingArea`, (0.02, 0.17, 0.78, 0.63) of the reading view) lies between the counter strip and the tool pouch, clear of the machines' beds. The rack stands over its top right, so the spread keeps papers off the rack where it can: lying under the rack costs four times as much as covering a paper (`DeskController.Spread`).

Build Office UI checks the plan. Every bay must lie on the art desk's top (`deskTop`), and no two bays may overlap. Any problem is logged as an error (`DeskArrangement.Problems`).

## §3 Decorations

`DeskLayoutSettings.decor` lists the art's small props: the form sorter, the tray, the calculator, the binder, the pen pot, the stapler, the ink pad and the old stamp.

- **Gathering.** `DeskDecor` gathers each prop's parts under one root at run time.
- **Starting place.** Each prop stands in a decor zone. That is where the player last put it, or failing that, the spot nearest the art's place in its own zone.
- **Dragging.** Dragging a prop (`DecorHandle`) lifts it 3 cm and it follows the pointer.
- **Dropping.** On release it settles on the nearest free spot of any zone, on a 2 cm grid (`DeskArrangement.TrySettle`). If no zone has room, it goes back where it was.
- **Saving.** Places are saved per player in PlayerPrefs `TimeDesk.DeskDecor`.
- **Clicks.** A prop that has a click reaction keeps it: its click box travels with it.

## §4 The camera

**Office view.** The office view uses the art's pose, looking `officeExtraPitch` (5) degrees further down, through `officeFieldOfView` (58) degrees. It is applied in `OfficeSceneBinder.ReadyCamera`, and `LensFit` still widens it for narrower screens.

**Close-ups.** The close-up is `MachineCloseUp`, with its timing in `CloseUpTimeline`. It is a Cinemachine camera two priorities above the office camera. It is placed as follows:

- `closeUpDistance` (0.75 m) from the machine's middle, on the chair's side;
- `closeUpHeight` (0.8) of that distance above it;
- a field of view of `closeUpFieldOfView` (38) degrees.

It blends over `closeUpBlendSeconds` (0.35 s), and cuts instead under Reduced Motion. Settings' Machine close-ups turns it off (`MotionPreference.MachineCloseUps`).

## §5 The flow (plan v2's model, `flow_model.py`, one day-12 case)

| | Today | Plan v2 |
|---|---|---|
| View switches | 8 (6 by the player) | 4 (2 by the player) |
| Pointer travel at 1920x1080 | 17,247 px | 14,264 px |
| Stamp and hand-back | 1,212 px, plus TAB and a view switch | 897 px |

The flow is measured in Unity by the probe (R7/LAY/UNITY_TODO.md).

## §6 Code

New code:

- **Pure, with tests:**
  - `DeskArrangement` (Visuals): bays, settling, saved places;
  - `CloseUpTimeline` (Domain).
- **Office:**
  - `DeskLayoutSettings`, `DeskBay`, `DeskDecorItem`, `DeskFrame` (Office/DeskLayout);
  - `DeskFurniture`, `DeskDecor`, `DecorHandle`, `MachineCloseUp` (Office/Desk).

Removed:

- the viewport placements:
  - `DeskLay`;
  - `DeskConfigSO`: `detainView`, `chipReaderView`, `uvTorchAt`, `machineScale`, every upgrade `*View`/`*Yaw`, `stampBarView`, `stampBarTravel` and `stampSpotView`;
- the drawer's summon, tether and slide:
  - `StampFlow.BarOut`, `ToggleBar` and `StowBar`;
  - `ControlState.StampsOut`;
  - `DeskStampTray.Tether`, `PlaceTab`, `PlaceHint` and `TryStampSpot`;
  - `DeskView.TryLiveViewPoint` and `TryLiveViewport`;
- the passport sliding under the drawer (`DeskController.PassportUnderDaters`, `DeskDocument.TryStampAreaOffset`);
- the binder's clear-of-scanner pass for viewport-laid props.
