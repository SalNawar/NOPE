> This first study predates the two detailed user references. It remains comparison evidence. The later reconstruction and its separate Blender source are described in [REVISION2_REVIEW.md](REVISION2_REVIEW.md); neither study is user-approved final art.

# CRT style study provenance

- Style source: user-selected `ReStory_current_reference.png`; see `../RESTORY_STYLE_GUIDE.md`.
- Mesh source: 80s Office pack `SM_Monitor.FBX`, used by `Computer.prefab`. Vendor files remain unchanged.
- Editable Blender source: `CRT_StyleStudy.blend`, containing before and after collections plus the comparison camera and lights. `style_crt.py`, which built it with Blender 5.2, was removed on 2026-09-26 (this ivory study was rejected). Keep the .blend: `rebuild_crt.py` loads it for its before/after comparison.
- New export: `Assets/Art/Office/ImportedOffice/Models/CRT_Painted.fbx`.
- Texture: `Assets/Art/Office/ImportedOffice/Textures/computer_ivory_painted.png`, copied unchanged from the built-in image-generation output `exec-64bc0115-2fe4-44ae-80ff-3a29d8ea4abe.png`.
- Image-generation inputs: original `T_Computer_BaseColor.PNG` atlas (edit target) and the selected ReStory screenshot (style reference).
- Prompt: Produce a square replacement albedo atlas; preserve every UV island outline, position, orientation, gutter, vent, control and fastener. Recolour broad computer casing and monitor bezel into coherent warm ivory ABS, retaining dark charcoal recesses and screen border. Replace muddy grey discolouration with quiet broad colour variations and sparse tiny corner wear. No new text, logos, repacking, directional sunlight, cast shadows or specular streaks. Output only the texture atlas.
- Geometry: original screen vertices and UVs retained; selective 1.2 mm casing bevels, original split normals retained. No new screen content.
- Unity: two dedicated URP Lit materials, ivory satin plastic and restrained blank glass. Stock URP passes avoid the previous custom shader's mismatched shadow/depth material layout. No global shader or lighting change.
- Blender comparisons use identical camera, lights, exposure and ground plane. The Unity comparisons use the same scene camera and lighting. Comparison sheets are resized/cropped screenshots, not production texture edits.
