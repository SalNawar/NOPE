# Illustrated desk shader

The custom Unity shader is `NOPE/Desk Anime`, in `Assets/Art/Office/DeskClean/DeskAnime.shader`. It replaces the PBR lighting equation with a three-tone diffuse palette. It is reusable on other opaque or alpha-cutout game props; the current installation is deliberately restricted to the desktop and CRT materials.

## Visual rules

- Preserve broad, clean colour fields. Shape changes come from normals and controlled shading bands, not surface noise.
- Shadows are cooler and lower in value; lit planes are slightly warm. The material colour remains recognizable in both.
- Use a short softened transition between the shadow, midtone and light regions. Close objects retain rounded geometry without looking airbrushed.
- Highlights are small graphic shapes. Paper has none; rubber and dark glass have very little; metal fittings have stronger, broader accents.
- Contours gently darken grazing surfaces. This is a surface contour treatment, not a screen-space outline or a filter applied over the UI.
- Actual scene shadows, depth, occlusion and fog remain supported. No lights, global render settings, floor or room materials are changed by the desk installer.

## Material controls

| Control | Desk starting point | Purpose |
| --- | --- | --- |
| Shadow value / midtone value | .48 / .76 | Three clear values, with a readable dark side |
| Shadow / light boundary | .02 / .58 | Where surface orientation switches tone |
| Band transition | .065 | Short transition, widened by pixel derivatives to reduce aliasing |
| Cool shadow multiplier | .72, .81, .94 | Colour separation without grey mud |
| Warm light multiplier | 1.06, 1.015, .92 | Restrained warm key |
| Painted highlight | .055 plastic, .13 metal, 0 paper | Distinct materials without PBR sparkle |
| Subtle contour | .10, 0 paper | Mild shape definition |
| Cast shadow strength | 1 normally; .25 CRT fascia | Avoid amplifying tiny vent shadows into a second striped graphic |

Use `Tools > Office Art > Apply Desk Anime Shading` to restore these material presets. The geometry/layout revision uses `Apply Desk Anime Revision`. Both are editor-only authoring tools; no runtime object generation is added.

The shader includes its own forward, shadow caster, depth and depth-normal passes with consistent alpha clipping and material buffers. It supports the project's realtime main light and additional local lights, fog, instancing and screen-space ambient occlusion. It is an opaque/cutout art shader, not a replacement for transparent glass, particles, UI, or a baked-lightmap workflow. Blender renders are source/shape checks; Unity Game view is the shading authority.

## Related corrections

- Phone: actual licensed `SM_Landline_Phone.fbx` silhouette, original curved receiver and dial UVs, clean sage material, retained authored normals. Source pack files are untouched. `refine_phone.py` is the reproducible refinement.
- Phone placement: root (-.43, 1.06, .20), scale 1.85. Phone and cord clear the till by approximately 4.7 cm in scene units.
- Mouse: cable now continues from the shell into the computer housing. The endpoint is inside the CRT system-unit bounds; the route is checked in Game view.
- Floppy disks: removed from the scene and from the current installer bindings.
- 2D trial: the spare paper/clip at the back right is a two-triangle alpha-cutout illustration, rendered from our own Blender paper and clip using the existing palette. It sits just above the desk and uses the same anime shader. The output-tray papers remain dimensional.

`revise_desktop.py` exports only the phone, mouse and paper trial. `author_desk_clean.py` also invokes it after a full rebuild so the old phone or disconnected cable is not restored. Editable source is `DeskClean.blend`; the 2D texture is `Assets/Art/Office/DeskClean/Textures/PaperDetail2D.png`.

Current checks are in `anime_validation.json` and `runtime_validation.json`. Earlier capture 60 and the original completion report describe the previous PBR checkpoint, which the user subsequently rejected for the phone and insufficient anime shading. This pass is a new visual iteration, not a claim of exact ReStory parity or user acceptance.
