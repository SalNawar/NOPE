# Joined panorama preview

Open panorama-preview.html or serve this directory on localhost and open that file. City, Join and Hall show each full-width panel and the transition; slider moves continuously. Compare join toggles the repair overlay. Seam guide marks the original panel boundary.

Two original neutral panels remain unchanged at2172x724 each. Total master extent4344x724. A2172x724 registered repaint atx1086 repairs the central discontinuity; edges blend only inside its overlap margins. Exact placement is in registration.json. This is a non-destructive image assembly preview, not a flattened replacement or Unity asset.

## Inspection
- Inspected city endpoint, central join and hall endpoint in browser.
- Central handrail and stone cap are now continuous rather than jumping vertically at original boundary.
- Hall portal layout and far-left skyline remain available at their original scale.
- Sky and city retain painted haze/form definition and some water detail. They are not measured physical albedo.
- Correction overlay's overlap margins still warrant a close artist review before export. Generated content can drift in small architectural details.
- A16:9 endpoint crop does NOT contain the entire3:1 hall panel. Final game camera framing is unresolved; do not claim this full-panel preview verifies it.
- No runtime lighting, layer extraction, occlusion/normals or scene modification this checkpoint.

## Next production work
Use this registered composite as the extraction reference, preserving all component images. Flatten/export paired corrected masters through an image editor before cutting; preserve the global coordinate manifest. Separate city/sky, glazing, architecture/floor and near furnishings, with clean hidden-area paint and common registration. Follow LIGHTING_PREP.md and verify one lighting sample before expanding setup.

## Generation provenance
Built-in image generation was used for the joint repaint. The input was a native-scale browser capture of the two neutral panels at their join, not a newly compressed hall image.
Prompt:
Precise seamless panoramic JOIN REPAIR. Input shows two adjacent panels with a vertical discontinuity EXACTLY down center. Keep output same3:1 crop, same camera and EVERYTHING in outer left25% and right25% pixel-aligned and unchanged. Only repaint the central transition to make this one continuous scene. The brass handrail currently abruptly drops at center: repair to ONE continuous straight/slightly panoramic handrail with no kink or break, smoothly rejoining unmodified ends. Same for stone wall cap and locker rows beneath. Preserve all window mullions; arrange consistent perspective spacing with no doubled seam. City river shoreline/buildings currently jump at center: repaint a coherent continuous river and neighborhood across the center without mirrored/repeated towers. Match sky hue/horizon on both sides. No new structural pillar or other object to cover the join. Geometry must be genuinely continuous. Keep exact right stair/door/portal positions and leftmost city. Preserve neutral diffuse unlit material appearance, no new shadows or highlights, remove any central water sparkle. NO text, people or desk. This image is a correction patch inside a wider master, so do not crop, zoom, shift, reframe or change outer margins.

