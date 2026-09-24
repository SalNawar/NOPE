# Foreground desk art — 24 September 2026

Installed and saved in `Assets/Scenes/OfficeScene.unity`.

The active replacement assets are under `Assets/Art/Office/DeskFinish`: 19 separately reusable Blender models, their material-linked prefabs, and three generated albedo textures. `DeskFinish.blend` retains editable mesh parts and modifiers. `author_desk_finish.py`, `hardware_models.py` and `booth_detail_models.py` are offline Blender authoring sources, outside Unity's Assets directory. The scene is authoritative for final placement; `hardware_scene_placement.json` supersedes the corresponding entries in the earlier `placements.json`.

## Current PC, till and camera refinement

The latest user-provided reference is saved as `ReStory_current_reference.png` (original clipboard filename `codex-clipboard-b79cb0f5-6ff7-451e-b56f-8db496cc82b8.png`). It supersedes the earlier orange-monitor reference for these objects and the camera composition.

- **PC:** a newly modeled tapered CRT shell with recessed cooling openings, a continuous molded bezel, convex blank 4:3 glass, 8-degree tube tilt, swivel disc and forked support. The horizontal desktop case is thicker, with real drive-bay recesses, distinct inset drive fronts, a grille, feet and separate folded cover. Grey-beige plastic, metal, rubber and glass have separate materials. No branding or fixed-language legends were added.
- **Keyboard:** a low wedge, shallow sculpted keycaps, separate function/navigation/number groups, differentiated modifier keys and a flexible cable. After the composition iterations, PC scene scale is 0.59; keyboard scale is 0.57. Both are authored at their local origins, without presentation offsets.
- **Till:** the tall checkout register is replaced by a low steel cashbox with a drawer reveal, recessed pull, lock and small raised credit display. The original `CreditsNumber` object remains attached to gameplay and is fitted to the new screen. Cashbox scene scale is 0.70.
- **Camera:** vertical FOV changed from 58 to 55 degrees, pitch from 5 to 10 degrees, and position from (0, 2.15, -3) to (0, 2.16, -2.62), on both the office Cinemachine camera and Main Camera. This is framing inferred from the reference, not a claim to know its exact camera settings. The PC is on the left, yawed -56 degrees toward the player; its tube retains its separate 8-degree back tilt. The keyboard follows at -44 degrees, with the mouse to its right. The till and intercom sit behind this group. The lamp, tray, stamp, inkpad, stationery and folded newspaper form a group on the right. The narrower mat remains clear for case work.
- **Interaction:** the original PC click target and monitor zoom camera were moved to the new tilted glass. `hardware_anchors.json` supplies exact model-space attachment points.

`Hardware_authoring.png` is an offline Blender view of the meshes, not an in-game screenshot. The user subsequently authorized small changes followed by tests and reference comparisons, superseding their earlier request not to test. Actual Game View captures are now under `Iterations/`; `20_lamp_reflector.png` is the latest reviewed wide view.

## Booth and finish passes 11–20

- Added a slim open booth crown, side supports, blank agency plate and shallow side shelves. The hall remains visible through the opening.
- Added a three-slot form sorter, clipped forms in the document tray, two floppy disks and a sleeve next to the computer. Forms and labels remain blank for changing languages.
- Added small Rocket and Nefertiti reproductions to the shelves, plus Starry Night and Mondrian postcards and pinned blank slips to the instrument boards. Their identities are recorded in `../HybridScene/EXHIBIT_INVENTORY.md`.
- Made the mat bluer, separated the cool city palette from the warm desk, and added a real-time reflection probe. No daylight was baked into textures.
- Replaced the lamp's incorrectly facing reflector disk with an inward-facing reflector and visible bulb. Its subtle emission supports the existing real-time task spotlight.
- Configured a separate editable `OfficeContactShading.asset` SSAO feature (intensity 1.7, radius 0.16, direct-light contribution 0.6), high-quality SMAA and `OfficeFinishProfile.asset`: Neutral tonemapping, exposure 0.12, contrast 10, saturation 8, vignette 0.13. Linked the URP package's postprocessing data through the Editor API; the first grading attempt had no effect because that reference was absent.

Every visual pass was captured in Play mode. Pass 20 returned zero console errors. These are incremental improvements, not a claim that the environment matches ReStory's finish. Source meshes and material metadata are retained alongside the saved Unity assets. Review captures live outside Assets; byte-identical imported screenshot copies were removed.

## Changes

- Built the PC, keyboard and cashbox described above, with painted surface materials and separate controls. Preserved the existing gameplay click target, monitor camera and actual CreditsNumber text object.
- Replaced the thin frame tray with a continuous rounded metal pan; rebuilt the intercom with speaker openings and a cable.
- Added a wired mouse and pad, open stamp inkpad, articulated task lamp and pencil rack. These are functional desk objects, not invented historical artifacts.
- Replaced the plain mat with a subtly worn inspection grid. Kept the middle clear for case work. Grouped the stamp/inkpad/tray to the right and computer controls to the left, following the latest user instruction.
- Added wood-framed painted instrument boards and short rear counter rails. Raised the calendar, clock and stability display; kept the existing live text objects.
- Retained the folded side newspaper, Voyager record-cover keepsake and historical postcards. No characters, cups, hearts, moon decorations or queue were added.
- Applied a warm side daylight key and lower cooler fill. The task lamp has a real-time spotlight aimed at the document area. Additional-light shadows and URP soft shadows are enabled, with a 4096 main-light shadow map. No directional lighting or shadows are painted into the new albedo textures.
- Deleted the nine superseded foreground visual instances from the scene. No gameplay or Unity editor C# was added. Temporary import and placement scripts were removed after installation.

The hall, portal, traffic, four ceiling banners and existing artwork remain separate from these foreground assets. The monitor screen and changing sign faces remain free of painted text. Existing contact shading is still enabled on OfficeForwardRenderer.

## Generated texture briefs and provenance

Created with the built-in image-generation tool; original outputs were copied without raster post-processing.

| Unity texture | Generation brief | Original output |
|---|---|---|
| `Textures/ivory_plastic.png` | Flat warm ivory ABS albedo for a stylized late-1990s office computer; quiet centre, broad subtle discoloration, restrained edge scratches and a faint removed adhesive patch. No lettering, logos, perspective, objects, specular highlights or baked directional lighting. | `exec-0cd455a3-0456-4cca-b8f1-acd0c23c6416.png` |
| `Textures/petrol_enamel.png` | Flat muted blue-green painted metal albedo; simple colour areas, broad faded patches and sparse pale corner chips. No lettering, objects, perspective or baked lighting. | `exec-fbd1a612-dd91-4cc6-83ea-4c01b5a3c293.png` |
| `Textures/inspection_mat.png` | Orthographic blue-green inspection mat with a wide pale grid, border and ticks, restrained stylized wear, no numbers or letters, no props and no directional lighting. | `exec-f26b481a-155a-4bea-8f64-93d82a1a20cc.png` |

Original outputs are in `C:/Users/Saleh/.codex/generated_images/01a0ce95-032b-7493-bef2-461e92d9f386/`.

The existing walnut albedo is reused from `Assets/Art/Office/Hybrid/BlenderPC/Textures/desk_walnut_albedo.png`, with a neutral material tint to avoid multiplying brown into it twice. Manufactured mesh parts now map the surface texture individually; tiny controls use the quiet middle of the texture.

## Handoff

The current PC and keyboard are built directly at their local asset origins by `hardware_models.py`; they no longer import the old arranged Blender collections. Editor mesh bounds place the equipment feet at desk height. Exact current glass and camera positions are recorded in `hardware_scene_placement.json`.

The composition was reviewed through successive Play mode captures after dismissing the briefing through its normal button. Invoking the original monitor `Clickable.onClick` event still enters `MonitorFocus` and shows the desktop. This verifies the event path, not a physical pointer-raycast test. The Unity console reported zero errors after the wide-view and monitor checks. The scene was saved in edit mode. `Before.unity` is the earlier scene backup.

The layout now follows the reference's left computer group, right document group and clear central working surface. The overall environment, prop density and material finish still differ visibly from ReStory; these captures are iteration evidence, not a claim that the whole scene has reached final reference quality. See `Iterations/REVIEW.md` for the accepted and rejected passes.
