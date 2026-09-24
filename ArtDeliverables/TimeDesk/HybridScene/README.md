# Time Sorter — separated asset pack v1

Open `Generated/gallery.html` to browse the generated image assets and mesh files. The complete pack is `TimeSorter-separated-assets-v1.zip`.

This is an asset authoring pass, not a completed or installed Unity scene. No Unity tests, Play mode, captures or scene changes were performed in this pass.

## Delivered

- **18 PNG images:** sky, far megacity, near megablock appearance study, hover car, six paintings, three sculptures, four invention/artifact exhibits and one discarded form. The sky is opaque; the other files contain alpha channels. The original alpha and RGB data are preserved without postprocessing.
- **22 individual OBJ base meshes**, plus **one assembled OBJ** with the hall/booth placement proposal. Separate hall floor/structure, banner, departure board, portal ring/opening/cabinet, bench, plinth, frame, desk, short partition, CRT, keyboard, till, NEXT casing, digital clock, stability device, calendar, folded newspaper, hover vehicle and near megablock.
- **One animated GLB:** a simple 3D hover vehicle with a 24-second straight lane traversal. The clip is authored, not wired into Unity; repeating the traversal requires loop control and offscreen wrap.
- Shared matte color materials, local planar UV coordinates, named object parts, camera/placement metadata, mesh/image inventories and the image prompts.
- Editable offline mesh authoring source. These scripts generate files in this folder only; they are not Unity editor installers.

## How the layers correspond

| Layer | Delivered representation |
|---|---|
| Sky | Independent opaque sky PNG |
| Far buildings | Separate skyline PNG with alpha |
| Near buildings | Separate appearance-study PNG and modular base mesh |
| Traffic | Separate car PNG, simple vehicle mesh and animated GLB |
| Hall | Separate floor and structure meshes with open windows on three walls |
| Hall furniture | Bench, plinth, frame, four banner instances, blank departures housing |
| Hall art | Thirteen individually named 2D exhibit images; proposed locations in the inventory |
| Hall litter | Discarded blank form PNG |
| Portal | Separate ring, opening plane and control cabinet meshes |
| Desk | Desk and short partition meshes |
| Devices | CRT with 4:3 screen, keyboard, till, NEXT casing, digital clock and stability casing |
| Mounted paper | Calendar and folded newspaper base meshes; separate blank content surfaces |

## Limits of this pass

The OBJ models are simple base geometry with a palette material, not polished final prop models. The assembled OBJ does not include the raster exhibits, a sky material, window glass or a final lighting setup. It records the placement proposal; the camera has not been approved in Unity. Proposed exhibit positions are not installed placements.

The sculpture and invention PNGs depict dimensional objects but **are not 3D models**. Use them as image assets or modeling references. A camera move around a sculpture needs a sculpted mesh. Paintings have stylized reinterpretations and fictional wear rather than conservation-accurate reproductions. The composition after Mondrian is an original arrangement in that visual language.

Raster assets contain illustrated form shading and cannot fully relight like geometry. The near-building PNG is an appearance study, not an unlit material map. The actual hall/desk/device base meshes have no baked lights or shadows. Runtime lighting, portal effects, window emission, history-driven asset variants, banner motion and gameplay interactions remain to be connected. No alternate-history variant set is included yet.

All device screens and sign faces are unlettered. Unity supplies the agency identity, departures, calendar/day, clock digits, stability, credits, desktop and NEXT caption. The four banner instances are separate from future graphics. No characters are included.

## File handling

Keep `palette.mtl` beside the OBJ files. Units are metres, Y is up and the proposed office view points toward +Z. Named flat screen/picture surfaces have local 0–1 planar UVs; this is not a packed texture atlas. OBJ has no gameplay, animation or collider data. GLB support depends on the import tools selected for the Unity project.

Files live outside `Assets` to avoid silently changing the active project. The existing 2D renderer, click/raycast system and Office/Monitor camera transition need deliberate hybrid-scene integration, as described in `SCENE_CONTRACT.md`.

The raster files were made with the built-in image generation tool. `Generated/image_manifest.json` stores each selected file's final prompt and dimensions. `Generated/image_sources.json` records the original generation paths. No API/CLI generation was used.
