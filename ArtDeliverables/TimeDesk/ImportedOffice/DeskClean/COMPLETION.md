# Completed desk art pass

> Historical PBR checkpoint. The user subsequently rejected the primitive phone and requested an actual anime shader. The current phone, connected mouse cable, removed floppy disks and 2D paper trial are documented in `ANIME_SHADER.md`; `anime_validation.json` and capture `65_anime_desk.png` supersede the corresponding claims below.

Final scene: `Assets/Scenes/OfficeScene.unity`. Final Game view: `../../DeskFinish/Iterations/60_desk_complete.png`.

The user approved the softer, clean direction and requested completion of all desk props, with particular attention to the undersized phone and pen cup. The pass covers **20 placements**: the rebuilt CRT plus 19 other placements using 18 distinct models. A buried duplicate newspaper was deactivated.

## Art and placement

| Object | Finish and placement decision |
| --- | --- |
| CRT | Upright rounded monitor, thin bezel, curved blank glass, swivel base, lower case and small controls. Left of the writing area. |
| Keyboard | Rounded low housing and keycaps, real readable mesh legends, restrained teal modifiers/orange accents. Angled with the CRT; moved fully into the camera frame. |
| Mouse and mouse pad | Smoothed shell; plain rounded pad. Immediately right of keyboard, with separation from the main blotter. |
| Phone | Reconstructed soft rotary phone, dial legends and coiled cable. Enlarged to a roughly 0.42 m body width in scene units; beside the cashbox and within reach. |
| Pen cup | Clean hollow cup about 0.144 m wide and 0.216 m tall in scene units; larger pencils. Back right, clear of the writing surface. |
| Calculator | Rounded wedge, LCD insert and labelled keys. Right of the writing pad, angled toward the player. |
| Banker lamp | Curved enamel shade and smooth support/base. Back right, behind the tools. |
| Output tray | Soft rolled rim and matte enamel. Moved inward so the complete tray is in frame. |
| Output forms | Thin sheets with curl and binder clip; placed on the tray insert, not on the desk below it. |
| Spare forms | Matching clean paper bundle behind the tray. |
| Stapler | Soft ABS cap, metal channel and rubber sole; alongside the tray. |
| Stamp and ink pad | Rounded grip/base, clean case and cushion. Together at the front right; the stamp fits the ink surface. |
| File sorter | Softened dividers and clean folder/paper materials. Behind the pen cup near the right-side paperwork. |
| Till | Clean enamel, appropriate metal/trim and softer edges; live credits display retained. |
| NEXT sign | Soft casing and clean materials; existing live label and interaction retained. |
| Binder | Clean rounded cover/spine and paper block. Stored behind the left computer area. |
| Keys | Small rounded metal parts, relocated beside the till. |
| Data disks | Clean shell/shutter/labels, beside the PC and clear of the mouse pad. |
| Office blotter | Actual rounded outline, plain warm-neutral matte face and restrained dark border. Clear central writing area; no repair grid or ruler. |

These dimensions describe the stylized scene, not a claim of exact real-world or ReStory measurements. Relative scale was judged together in the game camera. Geometry carries large transitions; broad materials have no grunge or micro-normal noise. Functional legends remain precise. Sunlight/contact shadows still come from the preserved scene lighting.

## Sources and production

`author_desk_clean.py` creates separate, editable Blender collections and exports project-owned FBX files. Mouse, till, ink pad, file sorter, data disks, tray, paper bundles, stamp and NEXT build on editable project Blender sources, with clean material remapping, removed wear patches and adjusted rounding. Keyboard, phone, calculator, banker lamp, pen cup, stapler, binder, keys and blotter were reconstructed. Vendor assets were not overwritten.

`DeskClean.blend` contains editable source; font sources are retained alongside evaluated glyph meshes. `DeskClean_manifest.json` records every export, material and triangle count. The 18 unique desktop meshes total 108,780 triangles; the CRT is separate. Simple URP Lit materials supply consistent shadow/depth passes and distinct ABS, enamel, rubber, glass, paper, wood and metal response. No gameplay code or project-wide shader/lighting settings changed.

## Verification

- `placement_audit.json`: actual scene bounds and positions for all 20 placements.
- `runtime_validation.json`: all 19 new desktop installations present; superseded renderers disabled; no missing/error materials; all prop bounds inside the desk; actual camera rays hit CRT and NEXT.
- `event_validation.json`: CRT → monitor, Back → office, NEXT → monitor, Back → office; zero Unity console errors and zero URP Lit shader errors.
- `scene_scope_audit.json`: 36 existing serialized documents changed from `23aa6e1`, all desktop props or CRT interaction/focus anchors; zero original documents removed. Room, floor, ceiling, original desk, boards, lighting and exterior remain unchanged.
- Final Game-view inspection: keyboard and output tray fully in frame, larger phone/cup, clear mouse movement and writing surfaces, grouped paperwork tools.

The scene is saved in Edit mode. This completes the requested desk pass. It does not assert exact ReStory parity or a full gameplay-day test.
