# Time Sorter — hybrid scene contract

Status: production design, not an implemented scene. This replaces the earlier flat-sprite composition approach. No Unity integration, preview capture, Play mode or tests are part of this pass. Do not recreate the removed art installers or automatic scene mutation scripts.

## Locked art direction
- The desired appearance is simple-material stylized 3D like the supplied ReStory screenshot: solid volumes, coherent perspective, restrained texture and readable broad wear. Paintings must be reinterpreted in that same visual language, not pasted realistic reproductions. Flat material detail does not mean flattening every object into vector art.
- Exterior is a dense oppressive retro-1990s megacity: enormous wide stepped concrete megablocks, inhabited structural decks, deep overlapping urban canyons and blank large sign panels. No thin generic skyscrapers, spires, utopian space palaces or incoherent elevated roads.
- Main public hall, not a storeroom: huge window panels on rear/left/right walls with real structural piers and low solid wall sections; open circulation, public seats, neglected significant exhibits, paperwork and rubbish. No storage shelves/box stacks used as hall filler.
- Exactly four banners suspended from ceiling beams, separate from agency artwork/text. Central ring portal and hanging departure board. Booth remains a later independent foreground pass.
- No religious content. No generated characters. No identifiable next traveller or visible character queue.
- Neutral base materials; no permanent sun beams, cast shadows, time-of-day tint or glowing windows in albedo.
- Every changing text, number, agency name and logo is separate runtime content. Blank CRT; NEXT label supplied by game; digital clock, not analog.

## Layer groups and representations
Layers are organizational/depth groups; they are not necessarily fullscreen PNGs.

| Group | Asset representation | Dynamic behavior |
|---|---|---|
| 00 Sky | Separate sky material/backplate | Shift-time sky color; optional moving clouds/haze |
| 01 FarMegablocks | Separate alpha skyline cards; no cars, sky, lit windows or interior | History-driven skyline variants; restrained parallax |
| 02 NearMegablocks | Simple modular 3D building masses with flat materials; distant detail via cards | Facade/technology variants, separate window emission |
| 03 TrafficFar | Small individually cut vehicle sprites on exterior paths | Looping traffic, separate speeds/densities; variants from history if authored |
| 04 TrafficNear | Low-poly 3D hover vehicles where camera movement exposes depth | Dedicated paths, occlusion and exterior depth; no static road decks |
| 05 HallShell | 3D floor, structural piers, window frames, low walls, ceiling, conduits | Runtime lighting, masks and shadows; separate dirt decals |
| 06 HallFurnishings | Individual simple 3D seats, vending machine, bins | Only explicitly authored state changes |
| 07 HallExhibits | Separate framed picture planes and 3D sculptures/inventions | Individual intact/damaged/alternate-history assets at stable display anchors |
| 08 FloorClutter | Separate paper cards/meshes, crumpled paper and small debris | Optional authored movement/state; not fused to paintings or floor |
| 09 Portal | 3D ring/base/cables, separate animated opening and controllable emission | Activation and portal animation; no portal glow painted into room |
| 10 BannersSigns | Four separate banner meshes, board housings and screen planes | Gentle banner motion; independent text/agency branding/history changes |
| 11 BoothDesk | 3D desk, left/right short partitions | Runtime light/shadows; consistent geometry, not independent perspective paintings |
| 12 DeskProps | Separate 3D CRT/keyboard/till/NEXT casing/clock/stability unit, selected keepsakes | Existing interactions/readouts preserved; individual state variants |
| 13 MountedPaper | Separate calendar/posters/photos/notes/newspaper planes and frames | Localization/history changes, reusable folded newspaper stock and obscure photo |
| 14 VisitorSlot | Existing character integration retained; future body art can remain billboard/sprite | Current visitor only; occlusion by desk via explicit depth plan |
| 15 RuntimePresentation | Text, screens, lighting, FX as independent scene objects | Clock, numbers, language, agency identity and screen content |

## Camera and spatial contract
- Establish one geometry blockout and shared 16:9 Office camera before rendering final layer plates. Derive all projections from that camera, not from independent image prompts.
- Preserve two view states: Office and Monitor; current Cinemachine rig needs deliberate camera/projection alignment for a hybrid scene. Do not change projection silently.
- Match the CRT front to a 4:3 screen with correct casing depth and a separate keyboard. Never stretch a rendered sprite to fit a rectangle.
- Exterior exists behind the real window openings. Cars pass behind piers, frames and buildings through actual depth/masks, not across an overlay. Near/far car routes have defined endpoints, direction, scale and speed.
- No roads in this exterior design; separated aerial lanes remove the earlier impossible-road topology.
- Leave room for visitor and booth in final composition. Do not judge only a background crop, and do not bake foreground into hall layers.

## Two separate kinds of time
1. Shift time: morning-to-evening sky, light direction/intensity, fixture emission, digital clock. All consumers share one clock value; do not independently advance each material/object.
2. Changed history: silhouettes/material variants, artworks, devices, language, agency name/logo, destinations. These respond to authored game state/cues, not to minutes passing.
3. Ambient animation: traffic, banner motion and portal FX. Authored pause behavior is required; random motion must not change gameplay state.

## Existing code anchors (read, not changed)
- OfficeViewController and CinemachineCameraRig provide Office/Monitor switching.
- GameManager owns the next-case gate and existing ReadySign reference; keep object reference while runtime caption becomes NEXT.
- OfficeReadouts supplies day, stability and credits to TMP, currently tints an entire stability SpriteRenderer. For 3D casing, preserve casing material and target a separate lamp/screen indicator with a deliberate adapter.
- TimelineReactiveSprite swaps sprites from Visuals cues. It does not swap 3D prefabs/materials. TimelineCueReceiver is the available base for later explicit adapters.
- TimelineCueReceiver refreshes at Start/on explicit Refresh; do not assume immediate automatic mid-case visual updates.
- A shared continuous shift-clock component was not identified in the inspected script-name/text search. Confirm intended source with the gameplay implementation before binding dynamic day lighting.
- Current Clickable uses Collider2D/Physics2DRaycaster. Moving to mesh props requires a planned raycast/collider adapter or retained proxy. Do not discard serialized actions.
- Current setup uses a 2D Unity scene. Lit meshes, depth occlusion and sprite participation must be checked against the existing renderer configuration during integration; mixing PNGs and meshes alone is not sufficient.

## Export contract
- Raster source art: neutral materials; true alpha for cutouts; empty display/text surfaces; no baked characters, cars or cast lighting.
- Geometry: editable source plus importable mesh, materials and texture maps, consistent physical scale, useful pivots, named screen surfaces. Generated PNGs are not mesh deliverables.
- Each exhibit gets its own stable ID, recognizability reference and display anchor. Broken state must be a defined variant, not random distortion.
- Keep base material, damage/decal layer, light/emission, animated part and text independent wherever they can change.
- A skyline concept establishes massing only; final far/near split and three-window coverage must follow scene geometry. Do not promise a single flattened skyline provides proper multi-view parallax.

## Work sequence
1. Correct isolated megacity massing concept (no traffic or hall).
2. Lock geometry/camera blockout and near/far exterior split.
3. Sky/building/traffic assets independently.
4. Hall geometry, then independently modeled/stylized exhibits and debris.
5. Booth, 3D functional props and display planes.
6. Explicit Inspector wiring to gameplay/time/history systems; no catch-all installers.
7. User tests in Unity. Codex does not run tests, Play mode or UI automation.

## Amendment (piece 7, 2026-09-25): new code anchors
Source: docs/superpowers/specs/2026-09-25-physical-desk-design.md. Added to "Existing code anchors (read, not changed)"; each is a scene component whose geometry is data, so the 3D props supply their own numbers at adoption:
- `MonitorScreen` on the PC's glass: the screen plane (a `ScreenAnchor` at the glass's centre) and the glass rectangle's size, screen power and the bezel LED; the desktop is a World Space canvas on that plane.
- `DeskSurface`: the desk plane and the rectangle papers stay in.
- `DeskScanner`: the scanner's drop area and glass bed.
- `DeskSlot` anchors (plant, mug, photo, free spots) and `DeskItem` ids on decor props.
- `TravellerView.Anchor` (where the traveller wheel and the speech bubble centre) and the `TravellerHitZone` child.
- `BoothCoordinator`, which applies the booth's input rules.
- The focus framing is computed from the glass (orthographic now; a perspective adapter at adoption).
The 4:3 CRT requirement above ("Match the CRT front to a 4:3 screen") is confirmed: the desktop is 1440 x 1080 units. `Clickable` still needs a `Collider2D`, so 3D papers and props need proxies at adoption.
