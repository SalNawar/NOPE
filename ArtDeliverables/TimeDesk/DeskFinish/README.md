# Foreground desk art — 24 September 2026

Installed and saved in `Assets/Scenes/OfficeScene.unity`.

Latest user-directed change: all 13 historical exhibits/keepsakes and 7 display supports have been removed from the scene. Functional office props, floor paperwork, portal and city remain. Source assets are retained. Current Game View: `Iterations/37_historical_clutter_removed.png`. The historical-art descriptions below document earlier passes.

The active replacement assets are under `Assets/Art/Office/DeskFinish`: 18 separately reusable Blender models, their material-linked prefabs, and four generated albedo textures. `DeskFinish.blend` retains editable mesh parts and modifiers. `author_desk_finish.py`, `hardware_models.py` and `booth_detail_models.py` are offline Blender authoring sources, outside Unity's Assets directory. The scene is authoritative for final placement; `hardware_scene_placement.json` supersedes the corresponding entries in the earlier `placements.json`.

## Current PC, till and camera refinement

The latest user-provided reference is saved as `ReStory_current_reference.png` (original clipboard filename `codex-clipboard-b79cb0f5-6ff7-451e-b56f-8db496cc82b8.png`). It supersedes the earlier orange-monitor reference for these objects and the camera composition.

- **PC:** a newly modeled tapered CRT shell with recessed cooling openings, a continuous molded bezel, convex blank 4:3 glass, 8-degree tube tilt, swivel disc and forked support. The horizontal desktop case is thicker, with real drive-bay recesses, distinct inset drive fronts, a grille, feet and separate folded cover. Grey-beige plastic, metal, rubber and glass have separate materials. No branding or fixed-language legends were added.
- **Keyboard:** a low wedge, shallow sculpted keycaps, separate function/navigation/number groups, differentiated modifier keys and a flexible cable. After the composition iterations, PC scene scale is 0.59; keyboard scale is 0.57. Both are authored at their local origins, without presentation offsets.
- **Till:** the tall checkout register is replaced by a low steel cashbox with a drawer reveal, recessed pull, lock and small raised credit display. The original `CreditsNumber` object remains attached to gameplay and is fitted to the new screen. Cashbox scene scale is 0.70.
- **Camera:** vertical FOV changed from 58 to 55 degrees, pitch from 5 to 10 degrees, and position from (0, 2.15, -3) to (0, 2.16, -2.62), on both the office Cinemachine camera and Main Camera. This is framing inferred from the reference, not a claim to know its exact camera settings. The PC is on the left, yawed -56 degrees toward the player; its tube retains its separate 8-degree back tilt. The keyboard follows at -44 degrees, with the mouse to its right. The till and intercom sit behind this group. The lamp, tray, stamp, inkpad, stationery and folded newspaper form a group on the right. The narrower mat remains clear for case work.
- **Interaction:** the original PC click target and monitor zoom camera were moved to the new tilted glass. `hardware_anchors.json` supplies exact model-space attachment points.

`Hardware_authoring.png` is an offline Blender view of the meshes, not an in-game screenshot. The user subsequently authorized small changes followed by tests and reference comparisons, superseding their earlier request not to test. Actual Game View captures are under `Iterations/`; `36_iteration_3_final.png` is the latest reviewed wide view.

## Latest three authorized art iterations

Completed and visually reviewed all three passes from `docs/THREE_ART_ITERATIONS.md`, starting at art checkpoint 8928865:

1. Material and lighting separation: cooler PC plastic/keys and metal, walnut tint, painted unlit stone with per-slab UVs, darker glass without the pale reflection veil, and real-time ceiling fixtures.
2. Environment construction: three different facade treatments at unchanged placements, fluted civic piers, window joint covers, glass behind the frames, portal seals/emitter ribs/bearing fixings, stepped pedestal mouldings, and animated liquid folds in the portal shader.
3. Final corrections: more daylight and foreground fill, selected illuminated city windows, and intercom/inkpad seams, feet, fixings and hinges. No camera or object layout changes.

Final actual capture: `Iterations/36_iteration_3_final.png` (5120x2880), with a resized inspection preview. Verification is recorded in `Iterations/verification_36.json`: all three glass panes at 0/100/25, original monitor event, zero console errors and zero portal shader errors. Transparency restored and saved at 25. No new gameplay C# in these three passes. This is a visible improvement, but ReStory quality parity has not been reached.

Final lighting: daylight 1.7, cool interior fill 0.32, neutral foreground bounce 2.4; lamp intensity 4.5 with outer/inner cones 85/45 degrees; two ceiling spots 65 each. Exposure +0.10, contrast 12, saturation 13, vignette 0.20. All lighting remains real-time. Glass tint is (0.16, 0.23, 0.27) at the existing 25% transparency setting. Current values supersede the historical pass notes below.

## Current finish pass: iterations 22-32

Camera and object composition are frozen after pass 26 at the user's request. Hall proportion/layout changes are deferred. This section records the earlier pass ending at `Iterations/32_final_art_pass.png`.

- Removed clipping booth crown/shelves, duplicate miniatures and painting postcards. Removed the unused frame source function, FBX and prefab. Side boards now carry pinned administrative slips with empty form fields.
- Four complete blank banners hang from the ceiling, with turned hems and suspension tabs. Sign/agency/banner faces remain available for localization and history changes.
- Six unique paintings use two physically supported picture rails. Five frames have continuous mitred moulding profiles; the damaged Mondrian frame retains its broken corner. Unique subjects are listed in `../HybridScene/EXHIBIT_INVENTORY.md`. One Voyager cover keepsake remains on the desk.
- Sculptures use gentler scan smoothing and about 24,000 triangles each to retain anatomy. Flat bronze/stone/crown materials remain, without scanned textures. Credits are in `../HybridScene/BlenderOffice/SOURCES.md`.
- Replaced the stretched low-resolution far-city plane with six mesh megablocks. Near buildings now have window reveals and projecting sills. Traffic remains animated and separate.
- Cashbox lid hinges, lock key and small handle wear; pinned forms with blank low-contrast rules. Paper, graphics and pins rotate together.
- Replaced the desk's inherited cube UV channel with one continuous walnut map. Neutral material tint, smoothness 0.26; dynamic lighting.
- Extended the lamp arm and aligned shade and spotlight with the mat centre. Final spotlight: intensity 4.2, outer cone 68 degrees, inner cone 38 degrees, colour (1, 0.82, 0.61). A restrained real-time room-bounce fill improves the shaded computer face. The mat's grid remains visible.
- Removed 15 superseded scene objects. Earlier source assets remain where still reused.

## Window transparency control

Select `HybridOffice/Hall/WindowGlass`. Since 2026-09-26 the glass material (`Hall_ClearGlass`, from the palette pass) owns the panes' alpha. To override it, tick **Office Window Glass > Override Material Alpha**: the **Glass Transparency** slider then controls all three window walls (zero is opaque, 100 disables the panes; the saved value is 25). Untick it to hand the alpha back to the material. Prefab: `Assets/Art/Office/Hybrid/BlenderOffice/Prefabs/Hall_WindowGlass.prefab`.

`Assets/Scripts/Office/OfficeWindowGlass.cs` is the only new runtime component in this corrective pass, explicitly requested for the slider. It updates material property blocks only when needed, without material clones or gameplay changes. This supersedes the earlier no-new-C# note for this visual control only.

## Verification and visual limits

Each pass has an actual Play mode capture. Final checks exercised transparency 0/100/25 with expected pane visibility, restored saved value 25, and invoked the original CRT `Clickable.onClick`, which entered `MonitorFocus`. Fresh console: zero errors. These are event-path and visual checks, not a physical pointer test or full gameplay day. An earlier UnitySkills request-abort error during transition was archived before the fresh check.

The scene still differs visibly from ReStory in architectural detail, background treatment, exhibit integration and overall light/material richness. No exact match is claimed. The large civic hall and megacity remain this game's environment; no characters or recognizable queue were added. Further hall composition changes are deferred.

All illumination is separate from albedo. Existing live clock, credits, stability, NEXT and desktop remain game-controlled; screen and history-dependent labels are not painted into art. Rendering settings are retained in `OfficeContactShading.asset`, `OfficeFinishProfile.asset` and the placement snapshot.

## Generated texture briefs and provenance

Created with the built-in image-generation tool; original outputs were copied without raster post-processing.

| Unity texture | Generation brief | Original output |
|---|---|---|
| `Textures/ivory_plastic.png` | Flat warm ivory ABS albedo for a stylized late-1990s office computer; quiet centre, broad subtle discoloration, restrained edge scratches and a faint removed adhesive patch. No lettering, logos, perspective, objects, specular highlights or baked directional lighting. | `exec-0cd455a3-0456-4cca-b8f1-acd0c23c6416.png` |
| `Textures/petrol_enamel.png` | Flat muted blue-green painted metal albedo; simple colour areas, broad faded patches and sparse pale corner chips. No lettering, objects, perspective or baked lighting. | `exec-fbd1a612-dd91-4cc6-83ea-4c01b5a3c293.png` |
| `Textures/inspection_mat.png` | Orthographic blue-green inspection mat with a wide pale grid, border and ticks, restrained stylized wear, no numbers or letters, no props and no directional lighting. | `exec-f26b481a-155a-4bea-8f64-93d82a1a20cc.png` |
| `Textures/walnut_veneer.png` | Flat walnut veneer with broad horizontal painted grain, sparse scratches, even illumination and no objects, lettering or baked light. Full prompt in `walnut_texture_provenance.json`. | `exec-d0faf65b-733e-47bb-aa52-46ee19ec0915.png` |

Original outputs are in `C:/Users/Saleh/.codex/generated_images/01a0ce95-032b-7493-bef2-461e92d9f386/`.

The later hall stone albedo is `Assets/Art/Office/Hybrid/BlenderOffice/Textures/civic_stone.png`, an unchanged 1254x1254 generated output. Brief and source filename are recorded in `../HybridScene/BlenderOffice/civic_stone_provenance.json`. It contains surface colour only; lighting and shadows remain in the scene.

The earlier walnut albedo remains in the previous asset library; the active desk uses `Textures/walnut_veneer.png`. Images were copied into the project without raster post-processing. Manufactured parts map their textures individually; small controls sample quiet texture areas.

## Handoff

The scene is authoritative. `hardware_scene_placement.json` records 78 current camera, prop, exhibit, light and environment transforms plus glass/rendering settings. It supersedes old placement snapshots. `hardware_anchors.json` retains monitor attachment points. `Iterations/REVIEW.md` records accepted/rejected passes; `Iterations/verification_36.json` records the latest targeted checks.

Authoring scripts and .blend files stay outside Assets; Unity consumes FBX models and linked prefabs. Keep sculpture credits with redistributed derivatives. The art branch continues from pushed main checkpoint b7f8671.
