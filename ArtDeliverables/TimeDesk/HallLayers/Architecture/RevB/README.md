# Revision B — government megatower crown

**Superseded layout.** [Revision C](../RevC/README.md) responds to the user's grand-terminal direction: multiple platforms/levels, a straight-facing private booth and no visible other desks or coworkers. The angled five-station line below is rejected; retain only the tower-crown and city-view context.

2026-09-26. This revision supersedes revision A's low-floor setting and its proposal to retain the original hall envelope. It is an architectural study, not an applied scene change.

## Confirmed by the user

- The hall is **massive and grand**, at the top of a mega government tower overlooking a fictional city.
- The player will be able to **pan left to look through the window**.
- Travellers enter from the building, pass the desk check and depart through the portal.
- Earlier requirements remain: station-style luggage lockers on the left, one painting plus other world artifacts displayed with neglect, believable circulation, original floor appearance preserved, PC unchanged, clear portal and no side booths.

## Drawings

- `B01_tower_crown_plan.png` and `.svg`: a proposed 60 × 80 m hall; five front service stations with the player at the window end; public/department core on the right; forward and left camera directions.
- `B02_tower_crown_section.png` and `.svg`: a proposed 24 m high civic volume, human-scale reference, and a local section through the player and left glazing, with the city below.

The dimensions and five-station count are proposals. The tower's absolute height and floor number are intentionally unspecified. The desk footprint is shown at its recorded size as a placeholder, rather than automatically shrinking it as in revision A. Its final ergonomic scale still needs the camera blockout.

## Architectural logic

The immense hall is the public crown of the tower. Tall repeated piers, deep window reveals, a long ceremonial view and large areas of glazing establish its scale. People, luggage lockers and ordinary furniture stay at human scale. Visible lower rooftops and layered city massing establish the elevation.

The player's station is at the **left/window end** of a front counter line. The other stations extend to the right. This gives the player a nearby view through the left facade without looking through another clerk's workstation. For this revision, the public lifts/stair and departmental circulation move to the right; revision A's left arrival core is superseded.

The right core brings people up from admissions far below. A recessed continuation serves MEDBAY, JAIL and C-SUITES; a secured connection serves the staff gallery behind the desks. The route across the hall remains entry → waiting/check → portal. The plan is an adjacency and massing study; detailed queue and door geometry remains a blockout task.

Grandeur and neglect should coexist. The structure and materials express state power; wear is concentrated at handled surfaces, waiting furniture, institutional displays and maintenance patches. Use one painting and a few deliberately mounted artifacts. Do not return to scattered art, cramped booths or a monochrome finish across the building.

## Pan and art representation

The new view requires a coherent spatial model. Nearby walls, window reveals, piers, desk and props must hold up while the viewpoint rotates. Do not use a single flattened hall paint-over or one-camera sprite arrangement as the final room.

The original three layers can remain **depth/organization groups**, but their assets must share one spatial model and camera convention. Distant city masses, mounted pictures and suitably distant crowd groups can still use sprites where the changing view does not expose their flatness. Near architecture needs actual geometry. Foreground props and UI remain independently placed.

**No camera controls or gameplay changes were made.** Claude owns the eventual pan input, camera constraints, traveller alignment, desktop interaction and PC-focus integration. These drawings describe the desired viewing relationship only.

## Sanity checks

Calculated values are in `study_checks.json`:

- Proposed floor area is 4,800 m², about **5.7 times** the recorded 840 m² source floor.
- Study eye is about **6.94 m** from the left glazing at its closest point.
- A straight sideways look would demand about **126°** of rotation from the proposed forward heading. That is unnecessarily large for this study.
- The revised target looks **obliquely** through the left facade, about 17.85 m from the study eye, requiring approximately **59°** of left rotation. This is a candidate framing, not a committed gameplay setting.
- The portal is approximately **39 m** from the study eye. At its recorded 6 m height, its approximate angular size falls to **8.8°**. The camera blockout must check whether it remains sufficiently prominent before committing to placement or scale.
- The diagram separates an approved departure route from the waiting benches. It is not a complete navigation, occlusion or access-control validation.

Next inspect forward and left views from one untextured model, including the whole pan between them. Check the existing corkboards, left partition, PC and props for occlusion. Confirm that the room still feels grand in the forward view and the window view shows city below, not only sky.

## Preservation and reproduction

Unity has not changed. The existing floor must not be stretched to fit the proposed larger footprint: keep its approved appearance and near-field scale, then design compatible extensions. Do not resize the PC or mutate gameplay anchors to fit the drawing.

`draw_tower_crown.py` reuses only the vector drawing primitives at the start of revision A's `draw_hall_study.py`; it does not execute or rewrite the revision A drawing outputs. It creates original SVGs and the JSON measurements. PNGs are rendered from those SVGs using the bundled Sharp library.

All work in this checkpoint is confined to HallLayers design deliverables. No protected editor code, materials, shaders, character pilot, Blender scripts or gameplay files are edited.
