# Blender PC art pass — 24 September 2026

Saved in `Assets/Scenes/OfficeScene.unity`. The subsequent object pass is documented
in `../BlenderOffice/README.md`; visual approval remains with the user.
The source is the user's ReStory screenshot, copied as `ReStory_reference.png`.
The PC is an original unbranded interpretation of its proportions and material style.

## Deliverables

- `TimeDesk_PC.blend`: editable Blender 5.2.2 source, assembled on its desk with
  an authoring camera and lights. Individual parts, curves, bevels and weighted
  normals are retained. These authoring lights are never exported to Unity.
- `author_pc.py`: explicit offline Blender authoring source. It creates editable
  objects and exports evaluated copies. It never edits a Unity scene or runs in-game.
- `asset_manifest.json`: material palette and exported object/material mappings.
- `PC_authoring_view.png`: earlier orange-palette Blender presentation, now superseded by the neutral casing revision; not a Unity screenshot.
- `BeforeIntegration/OfficeScene.unity`: scene backup from before this pass.
- `Assets/Art/Office/Hybrid/BlenderPC/Models`: three FBX assets.
- `Assets/Art/Office/Hybrid/BlenderPC/Materials`: URP materials, matte on casings.
- `Assets/Art/Office/Hybrid/BlenderPC/Prefabs`: reusable material-bound art prefabs.
- `Assets/Art/Office/Hybrid/BlenderPC/Textures/desk_walnut_albedo.png`: neutral
  stylized walnut albedo, generated with the built-in image-generation tool.

## Scene integration

`HybridOffice/Booth/PC_CRT_Desktop` contains the tube monitor and its horizontal
desktop case: molded front, tapered back, physical vent recesses, drive faces,
switches, cables and sparse wear. The glass is a separate shallow convex 4:3 mesh
with normalized screen UVs. Its surface is blank. Branding inserts, keyboard keys
and labels have no baked words. The original screen-space desktop UI is unchanged.

`PC_Keyboard` has staggered rows, shaped concave key tops, a separate navigation
cluster and keypad, and a cable routed alongside the desktop case. It shares the
PC's yaw and coordinate system.

`PC_Desk` replaces the existing tabletop at the same 1.060 m surface height and
5.8 × 2.52 m footprint, with a timber edge and matte walnut surface. Existing desk
props and gameplay placements remain.

The old `Hybrid_CRT`, `Hybrid_Keyboard` and `Hybrid_Desk` art instances were removed
from the scene after the new objects and materials were installed. Their source
assets remain available for recovery. `OfficeRoot/CRTMonitor`, its Clickable,
serialized events and Collider2D remain; only the click bounds/position and
MonitorVCam placement were aligned with the new screen. No gameplay C# or automatic
scene installer was added. One-shot integration helpers were removed afterward.

Only authoring renders and static import/hierarchy/material reads were performed.
Codex did not enter Play mode, run tests or capture the Unity Game view. The user
performs the in-game review. Other office objects now use the BlenderOffice library
described in `../BlenderOffice/README.md`.

## Editor crash during this pass

Unity's crash log reported `d3d12: Unrecoverable GPU device error` / device-removed
error `887a0005`, rather than identifying an FBX import error. Unity was reopened
with `-force-d3d11` for this editor session. Project build graphics settings were
not changed. A stale licensing client from the failed startup was restarted;
the editor and local REST service then reconnected normally.

## Texture provenance

Built-in image generation, reference: user-supplied ReStory screenshot.
Generated source: `exec-8077458f-ddc1-4085-b908-a64b209a6214.png`.

Prompt:

> Create a production-ready BASE COLOR ALBEDO TEXTURE, a single square 1024x1024
> seamless texture for a stylized 3D game wooden repair-shop desk, inspired by the
> simple painted surfaces in the supplied ReStory screenshot. Texture only,
> perfectly orthographic, evenly lit with NO shading: no perspective, no objects,
> no cast shadows, no highlights, no ambient occlusion, no vignette. Medium muted
> walnut brown, flat broad brown regions, long loose hand-painted horizontal
> woodgrain strokes, only 8 to 14 long grain strokes across the entire image. Very
> low contrast, broad simple graphic wear, occasional tiny muted worn streaks.
> Absolutely no photographic wood, no fine grain, no pores, no noise, no planks or
> panel borders, no words. The tone is an old practical workshop tabletop. All
> four edges tile seamlessly. Keep the surface quiet so 3D props remain the focus.

The generated file was copied unchanged into the project. Its output resolution
is retained; Blender maps it to the tabletop. Texture tiling was not visually
tested in Unity.
