# Current direction — awaiting mockup review

The user corrected the first pack-placement pass on 2026-09-24. That pass's
middle-ground administration and waiting-area furniture is **not the accepted
direction**. Do not rerun its `layout.json` as the final design.

## Revised scope

- Show a mockup before continuing the rebuild.
- Use the actual imported packs' object shapes, with a mix of retro office and
  industrial science-fiction equipment.
- Rebuild the **player desk, portal, hall architecture, ceiling and floor**.
- Remove the newly added middle-ground furniture/clutter. Do not repopulate the
  hall with desks, shelves or seating as a substitute for rebuilding the desk.
- Match the supplied ReStory reference's simple painted surfaces and considered
  material/light response. A generated concept is not evidence of achieved Unity
  rendering quality, nor a guarantee of exact model geometry.

## Hybrid rendering, explicitly requested by the user

Not everything is 3D. Use 2D sprites across the desk, middle ground and background.

- 3D: structural hall, floor and ceiling, desk body, substantial hardware,
  portal housing and other objects needing depth or real-time light interaction.
- 2D: paper/document faces, notices and selected environmental details, plus
  layered painted backgrounds. Separate animated traffic and portal effects.
- Decide sprite placement from the fixed game camera; preserve occlusion and
  contact with surfaces. Do not make every small detail another 3D prop.
- Sprite and mesh colours, edges and texture detail must share one art direction.

## Continuing constraints

PC left, tilted inward; mouse immediately to its right. Central work mat clear.
Digital clock and changing text remain runtime elements. CRT blank in office
view. Four ceiling banners. Three large windowed walls and a grounded megacity
beyond. No recognisable waiting queue, historical art clutter, cups at edges,
hearts or lunar decorations. Final lighting remains dynamic.

## References

- ReStory_current_reference.png in ../DeskFinish is the selected reference.
- Catalog/mockup_pack_references.jpg contains actual imported prefab previews.
- The initial 388-prefab catalogue and file inventory remain useful, but the
  original selection/placement decisions were superseded by this scope change.

The first mockup is a built-in image-generation concept based on pack previews,
the ReStory reference and the empty-hall Game view. It is not an in-engine render.
