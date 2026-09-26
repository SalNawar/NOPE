# Revision C — grand time-travel terminal

2026-09-26. Current planning reference. This supersedes the tilted desk line in revision B. The user asked to see plans for a grand train-station-like terminal at the crown of a government megatower, where travellers use teleporters instead of trains.

## Confirmed requirements

- Massive, grand dystopian terminal overlooking a fictional city from the top of the tower.
- A straight-facing desk in a private inspection booth. No visible coworkers or other desks.
- Multiple platforms or floors, with circulation that explains where people come from and go.
- Building arrival → desk check → departure through a teleporter.
- Left window pan, station-style luggage lockers on the left, one painting and a few deliberately displayed world artifacts with institutional neglect.

## Drawings

- **C01_terminal_plan.png / .svg:** inspection concourse with lower and upper platform footprints, circulation, private booth, tower core and window relationship.
- **C02_terminal_section.png / .svg:** three levels, the booth's view toward lower and upper platforms, stair connection and a larger booth privacy detail.

Four platforms and the dimensions are **proposals**. This is a spatial planning study, not a Unity implementation or an approved final building layout.

## Three-level organization

The existing desk floor remains the zero datum in the study:

| Level | Role |
|---|---|
| −6 m | Lower departure floor; Platforms 01 and 02, each with its own boarding area and teleporter. |
| 0 m | Inspection concourse; public tower arrival, waiting/storage, the player's private booth and controlled platform access. |
| +6 m | Upper gallery and Platforms 03 and 04; supported structure, a high crossing and a link back to the platform lift. |

The study envelope is approximately 60 × 81 m, with the high volume reaching +24 m above the inspection floor: 30 m overall from the lower floor. The tower's absolute height and story number remain unspecified.

Public arrivals reach the right-hand core, then waiting/storage and the inspection desk. After clearance, travellers use the controlled stair or platform lift. The lift connects all three levels; an upper link connects it to the gallery. MEDBAY, JAIL and C-SUITES remain destinations beyond the right corridor. The small number of visible platform gates communicates a larger transport system without filling the scene with office counters.

## Player booth and privacy

The desk faces straight forward; there is no rotated or fan-shaped arrangement. Its recorded footprint is a placeholder so the study does not silently resize the PC or rework the desktop.

An opaque right return and enclosed rear/service accommodation hide other work areas. The left side has a clear window bay, through which the player can look toward the west exterior glazing. The right/rear rooms are context only; no extra visible clerks or desks are proposed.

The intended main view looks past one visitor into the large terminal, toward the near lower platform and more distant upper platforms. The left pan reveals the city. Every part of that pan still needs a real 3D blockout check against the existing PC, corkboards and partitions.

## Sanity check

`checks.json` records narrow analytic checks, not a claim of full scene validation:

- Desk yaw is zero. The candidate left pan is 60 degrees.
- The inspection floor and source camera height of 2.16 m are retained as study references.
- The ray to the foot of Platform 01's portal clears the proposed concourse edge by approximately **0.263 m**. This requires the edge to be relatively close to the booth; moving it farther into the hall could hide the lower platform.
- A solid 1.1 m guard would obstruct that ray. The plan therefore calls for clear glass with sparse structural supports, to be checked in perspective.
- The center left-pan ray intersects the proposed booth's clear side-window span. This does not prove that the complete window view is free of the PC, boards or other props.
- The stair is a schematic two-flight run with a landing; detailed stair, lift, gate and access geometry remains a later design step.
- The section projects side galleries into the drawing; it is not asserting that all four portals stand on a single physical slice through the building.

## Art and integration boundaries

The atmosphere comes from transport infrastructure: numbered platforms, departure boards, controlled boarding, stacked galleries, long sightlines and human-scale luggage facilities beneath monumental architecture. One painting and two artifact anchors remain peripheral details, not a gallery filling the hall.

The next useful visual is a single untextured spatial blockout seen from the booth and through the left pan. Near architecture needs real depth; distant city, framed images and distant anonymous crowd groups may still use sprites where suitable.

No Unity scene, floor mesh, PC, existing editor script, shader/material, character asset or gameplay file was changed. Original floor appearance and the near-desk floor remain preservation requirements; the proposed void and new levels would require a separate reviewed geometry pass. Claude owns camera controls, visitor alignment and interaction hooks.

The diagrams are original SVG drawings created by `draw_terminal.py` using the earlier vector helpers, with PNGs rendered by Sharp. Prior revisions are preserved as historical work, not overwritten.
