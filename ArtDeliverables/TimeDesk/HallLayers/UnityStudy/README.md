# Terminal Unity art study

This is a first playable-camera **art/blockout checkpoint**, not the finished mockup reproduction and not installed in OfficeScene.

Open `Assets/Art/Office/TerminalStudy/TerminalHallStudy.unity` to inspect the real Unity scene. `forward.png` and `left-pan.png` are Unity camera renders, not generated concept images. The study camera uses the office's forward renderer (index 1); the project's default renderer is 2D and is unsuitable for this scene.

## What exists

- Straight-facing visual copies of the approved desk meshes/materials at their original transforms. No gameplay scripts are copied. Runtime display contents are consequently absent in this study; the actual PC and UI are unchanged.
- Approximately 60 m wide, 30 m tall terminal: inspection concourse at zero, departure floor at −6 m, supported upper gallery and crossing at +6 m.
- Four gates reuse the existing portal art without editing its shader or materials.
- Broad controlled stair, schematic lift core/link, right arrival passage, departure signage, mixed suitcase compartments and a small peripheral display placeholder.
- Rounded architectural blocks, existing anime shader on new hall materials, restrained plaster wear, clear glazing, original floor material on newly authored surfaces, six anonymous crowd groups and a left city study.
- `TerminalHallArt.prefab` contains the hall art only. It excludes the copied desk, camera, light and all gameplay components.

## Still required

This is **not final production art**. The lighting/composition, city detail, custom artifacts, final floor treatment and environmental storytelling still need refinement against the mockup. The current painting is a placeholder, lift core is schematic, and the departure circulation has not been navigation-tested. Banners are thin geometry in this blockout; final flags must be 2D sprites. No live left-pan input is implemented. The prefab is intentionally not dropped into the live scene alongside the old hall.

The approved desktop and original OfficeScene floor assets have not been edited. Replacing the old hall with this lowered floor changes the visitor-to-portal relationship, so live installation must use Claude's gameplay hook rather than silently shifting gameplay anchors.

## Concrete integration contract for Claude

Coordinates are in the existing desk's world space, desk yaw zero:

| Location | Position / orientation |
|---|---|
| Player camera | (0, 2.16, −2.62), pitch 10°, yaw 0°, vertical FOV 55° |
| Left inspection view | Same position, pitch 12°, yaw −60°; screenshot sample only |
| Visitor at desk | Proposed (0, 0, 2.1), must be checked with the actual character |
| Arrival passage | (41, 0, 8) |
| Stair top / bottom | (8.5, 0, 4.5) / (8.5, −6, 17.3) |
| Gate 01 / 02 | (2, −6, 27) / (21, −6, 47) |
| Gate 03 / 04 | (27, 6, 40) / (33, 6, 63) |

Claude needs to own traveller routing, gate destination selection, camera panning and the switch from the existing hall to this art root. These coordinates are proposed art anchors, not changes made to gameplay. No claim of cleanup-on-main was received, so the protected cleanup files and main merge remain untouched.

## Regeneration

Editor menu: `Tools > Terminal Art > Create Isolated Hall Study`.

For an unattended run with this project closed: Unity `-batchmode -projectPath E:\unity\NOPE -executeMethod TerminalHallStudy.Build -force-d3d11 -logFile <temporary path>`. Do not pass `-quit`: the builder waits for rendering initialization, captures both views and then exits batch mode itself. The builder saves only its own generated assets and scene; it does not save OfficeScene. Existing study outputs are regenerated.

The old art rebuild menus are not used. Character pilot, gameplay assets, protected shaders and Claude's cleanup scripts are not modified.
