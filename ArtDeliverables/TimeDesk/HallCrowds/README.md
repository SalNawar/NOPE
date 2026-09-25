# Hall crowd groups

25 September 2026, branch `art`. Background art added after the user accepted the anime desk direction. The user requested a busy building filled with groups, pale by morning and dark by evening. Their corrections take priority: merged anonymous shapes, no recognizable personal characteristics, but visible legs and feet.

The scene contains 19 complete illustrated groups at different depths, using six authored compositions. Upper bodies merge into opaque, flat masses. Head rhythms and group widths vary; simple leg gaps and feet anchor them on the original floor. The user found the initial scale too small against the room; groups are enlarged by 30% (tallest points 2.24–2.42 world units) for this oversized hall. No faces, hair detail, costumes, luggage, outlines, individual NPCs, colliders, or animation. Each group has a faint ground contact and casts no 3D shadow. The front desk and portal remain readable.

## Assets and reproduction

- Final image: `Assets/Art/Office/HallCrowds/Textures/CrowdGroups_Atlas.png` — generated and revised with the built-in image tool; RGBA, 1536 × 1024. The shader uses alpha only, ignoring the image's RGB/glow. Prompt history: `GENERATION_PROMPT.md`.
- `NOPE/Hall Crowd Silhouette`: opaque alpha-cutout, flat tint, depth-writing and fog. Six meshes sample tight atlas UV rectangles without altering the source image.
- Morning/evening material pairs have three restrained depth colours. `NOPE/Hall Crowd Contact` draws the faint group contacts.
- `Assets/Editor/OfficeArt/OfficeHallCrowds.cs`: **Tools → Office Art → Build Hall Crowd Groups**, in OfficeScene/Edit mode. Rebuilds only `OfficeHallCrowds`. Placements, import settings, meshes, materials, and bindings are reproducible here.
- `Assets/Scripts/Office/OfficeHallCrowdPalette.cs`: presentation component with Automatic, Morning, and Evening preview modes. **Tools → Office Art → Hall Crowds** contains palette previews and validation. Leave **Follow Shift** selected for normal play.

Automatic colour follows visitor-slot progression from `DayOrchestrator.ShiftProgress`. It remains pale during the first half of the shift, then blends to the dark version by 90%. This is shift progression, not the static 08:00 clock display. Room lighting is not changed by this component. Morning and evening screenshots compare the crowd palettes under the same existing hall lighting.

## Verification

- Final Game-view captures: `../DeskFinish/Iterations/70_hall_crowds_morning_scale.png` and `../DeskFinish/Iterations/71_hall_crowds_evening_scale.png`.
- `validation_morning.json`, `validation_evening.json`, `validation_automatic.json`: 19 groups, six compositions, all 19 in the gameplay camera frustum, no colliders, all material bindings and floor contact checks pass.
- Both new shaders compile without errors; Unity console has no errors after both previews.
- `audit_scene_scope.py` compares the saved scene with `d99d585`: only new crowd documents and the scene-root reference may change. Floor, ceiling, lights, camera, portal, and desk are preserved.
- Runtime palette previews and automatic opening-state binding were checked. A complete day was not played through.

The initial detailed atlas was rejected as too recognizable. The first simplified version removed too much of the lower bodies; the final revision restores legs and feet. Screenshots 66–69 are superseded by 70/71, which include the user's scale correction.
