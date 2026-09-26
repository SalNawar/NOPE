# Hall architecture study — revision A

2026-09-26. The user requested a top view and side view, with a sanity check, before further hall art. These are measured design drawings, not changes to Unity. They supersede the latest screen mockup as the spatial planning reference; the proposed architecture still needs the user's review.

## Drawings

- **A01_top_plan.png / .svg:** circulation, three counter bays, left public arrival core, left luggage lockers, right recessed departmental corridor, existing portal, staff circulation and display locations.
- **A02_side_section.png / .svg:** source hall height, desk and human scale, source camera height, portal, proposed building level and a front elevation of station-style luggage compartments.

The SVGs are editable vectors. The Python source creates original line drawings; Sharp renders those drawings to PNG. No generative image edit, Unity scene modification or Blender source edit was used for this study.

## Confirmed requirements

- **Traveller route confirmed by the user:** enter from the building, desk check, then depart through the portal.
- The hall must visibly connect to the larger building; placing arbitrary doors on a flat wall is insufficient.
- Lockers should resemble train-station luggage storage, with mixed bag/suitcase compartments, rather than tall wardrobe lockers.
- One painting is enough. Other artifacts from around the world should be deliberately displayed, with neglected mounts, cases, labels or lighting.
- Retain the original floor, preserve the PC and keep the portal clear. Gameplay remains owned by Claude.

## Proposed building logic

**Level 02**, one floor above a street admissions lobby, is a design assumption, not established story canon. A public stair and lift on the left deliver travellers into the hall. The original 28 × 30 m footprint becomes a shared departures concourse.

There are **three check desks on the same front counter line**. The middle station is the player. The other two are outside the main camera view and do not introduce side booths. A restricted gallery connects the backs of the desks, with secured access from the right service corridor. Openings are shown behind each bay so the clerk areas do not become sealed pockets.

Travellers enter at the back-left, deposit belongings at the locker bank, wait on the left and approach a called desk. After clearance, they turn back into the hall toward the portal. A separate referral path reaches the right departmental corridor. MEDBAY and JAIL occupy branches off that corridor; C-SUITES is reached through its secure lift/stair connection. This is architectural context only, not new gameplay routing.

The right corridor has a **forward-facing opening and an L-shaped recess**. Its floor continues through the opening and turns toward the departments. The room therefore reads as connected space, rather than a box with a door pasted onto its side wall.

The locker bank faces toward the player from the left side of the hall. This matters because a bank placed directly against the near left wall would be hidden behind the corkboard. Its two 2.8 m modules are 0.8 m deep and about 1.95 m tall, with a 1.8 m standing apron.

P / A / B are display anchors for one framed painting, a ceramic vessel and a stone relief. Specific pieces and countries are not final. They are proposed as a small neglected institutional collection, not loose objects scattered around the hall.

## Measured source and scale findings

Source: `../../PaletteExploration/Applied/LayoutWear/after.json`, captured from the approved scene earlier in this task. `measured_scene.json` records the bounds used. The scene camera's serialized vertical field of view is 55 degrees. Unity units are treated as metres for this design study.

| Item | Recorded source | Design implication |
|---|---|---|
| Hall floor | 28 × 30 m | Plausible as a shared civic concourse; needs circulation and multiple stations to explain the volume. |
| Main desk footprint | 5.8 × 2.53 m | Oversized for a single clerk under the metre assumption. Study a 3.2 × 1.10 m station before committing to art. |
| Desk surface height | About 1.06 m | Compatible with a standing service counter; proposed height stays approximately 1.05 m. |
| Camera | Y 2.16 m, 10 degrees downward | Above the 1.62 m standing-eye reference; improves tabletop visibility but requires a deliberate framing decision. |
| Portal ring | About 5.94 m diameter / 6.02 m top height | Retained as major civic infrastructure; not scaled to match furniture. |
| Portal base | 6.65 × 1.55 m | Retained with a clear approach. |
| Main hall structural envelope | Approximately 14.35 m high | A very tall civic volume. This is a source measurement, not a proposal to add a ceiling. |

**The smaller desk is a planning envelope only.** It would require checking the current props and interaction areas in a separate blockout. Do not scale the PC, compact the approved desktop automatically, move gameplay anchors or alter the camera as part of applying these drawings.

## Sanity check results

`sanity_check.json` contains the reproducible calculations.

- All three proposed travel paths clear the listed furniture footprints for a 0.6 m diameter person in the plan. Sampling interval is at most 0.05 m. This is a design-space furniture check, not a Unity NavMesh, building-code or complete wall/door check.
- The central departure approach reserves 4.8 m clear width, with furniture and exhibit positions outside it.
- The public entrance is 4 m wide. The forward-facing right passage is 4 m wide and connects to a 5 m cross-corridor.
- The five sampled target centers are inside the source camera frame and outside conservative projected corkboard rectangles. The left entrance has only a small visibility margin; the next camera blockout must confirm that enough of the opening is visible.
- The originally considered near-left wall locker bank and flat right-sidewall opening fail the corkboard overlap check. That finding caused the locker orientation and the right corridor geometry to change.
- A target-center check does **not** establish full visibility. Entire openings, occlusion by desks/props/people, the portal's clearance and the two gameplay camera states still require a shared-camera blockout.

## Next step

Review the floor level, three-counter arrangement and desk scale. Then make one untextured perspective blockout from this geometry, using the game's actual camera. Only after the layout works should the hall furnishers and three sprite depth layers be authored from that shared camera.

This checkpoint does not modify OfficeScene, OfficeGameplay, gameplay assets, characters, existing editor menus/scripts, protected shaders/materials or any of Claude's listed files. It does not run Unity rebuild tools.
