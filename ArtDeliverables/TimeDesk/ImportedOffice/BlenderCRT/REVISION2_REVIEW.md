# CRT reconstruction — one-object review

25 September 2026. Historical one-object checkpoint. The user subsequently said the work was on the right track, stopped requesting comparisons and authorized the full desk pass. See ../DeskClean/COMPLETION.md for the completed result.

The latest user CRT crop and laptop close-up informed the revised [style guide](../RESTORY_STYLE_GUIDE.md) and [reference analysis](../RESTORY_REFERENCE_ANALYSIS.md).

## Changes

- Reconstructed the wide TV-like source study as an upright office CRT: slimmer rounded bezel, gently convex blank glass, tapered rear housing, visible swivel pedestal and a low horizontal system unit.
- Softer silhouette corners (up to 18 mm), stable planar shading, small monitor controls, restrained orange/teal details and crisp fictional legends.
- Clean, untextured colour materials for this geometry review: no grunge, blurry atlas or painted highlights. URP Lit has dedicated plastic, trim and glass response. These settings are our choices, not recovered ReStory settings.
- Original vendor files and earlier study remain available; only `Rebuilt CRT` is active in the desk pivot.
- Nine exported material groups, 26,384 triangles. Editable parts, comparison model and studio lights are retained in `CRT_Rebuilt_Study.blend`. Reproduce using `rebuild_crt.py` in Blender 5.2.

## Same-camera evidence

| Earlier ivory study | Reconstructed study |
| --- | --- |
| ![Before Blender](revision2_before.png) | ![After Blender](revision2_after.png) |

These are real Blender renders under identical studio conditions. They are not the in-game lighting.

| Unity before | Unity after |
| --- | --- |
| ![Before Unity](../../DeskFinish/Iterations/55_crt_before_rebuild.png) | ![After Unity](../../DeskFinish/Iterations/56_crt_rebuilt.png) |

Both Unity captures use the same office camera and lighting. Animated traffic and portal content may differ between frames. The scene's harder sunlight remains visible; studio softness does not establish final engine parity with ReStory.

## Integration and checks

- Desktop footprint centre and desk contact retained. Overall bounds changed from extents `(0.46,0.37,0.40)` to `(0.45,0.33,0.43)` metres; this is a reconstruction, not an identical silhouette.
- Screen and click target share centre `(-1.543,1.506,-0.396)`. Focus camera repositioned to the new screen. See `revision2_unity_alignment.txt`.
- Existing CRT, Back, NEXT, Back events produced `MonitorFocus`, `OfficeFocus`, `MonitorFocus`, `OfficeFocus`. No runtime gameplay code changed. No physical pointer test or full gameplay day was performed.
- Unity console: zero errors; URP Lit: zero shader errors. See `revision2_validation.json`.
- `revision2_scene_diff.json` records existing scene documents changed from checkpoint `23aa6e1`. Changes are confined to the CRT hierarchy, its click/focus anchors and the separately requested office-pad material overrides.

The room, original floor, ceiling and lights remain preserved. The pad is a plain warm-neutral office blotter. The keyboard and other desk assets still await individual passes; do not describe them as softened or complete.

The later whole-desk instruction supersedes this checkpoint's original one-object review gate.
