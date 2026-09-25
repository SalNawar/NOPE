# Office art style guide — clean surfaces, soft forms, crisp detail

Revision 2, 25 September 2026. Supersedes the initial ivory-CRT material study as the style target.

The user subsequently approved continuing this direction across every desktop object and stopped requesting before/after sheets. The completed desktop pass is recorded in [DeskClean/COMPLETION.md](DeskClean/COMPLETION.md).

## References and authority

- [CRT crop](References/ReStory_CRT_closeup.png): computer shape, construction, proportion, colour hierarchy and small graphic accents.
- [Laptop close-up](References/ReStory_closeup_cleanliness.png): clean close-range surfaces, restrained shading and readable functional detail.
- [Full analysis](RESTORY_REFERENCE_ANALYSIS.md): observations, limitations, screenshot samples and differences from our first study.
- The user's direction: all near-view objects must be **much softer**; the central desk pad must be **a plain office writing pad**, not a blue checked repair mat.

These images guide visual treatment. They do not turn the office into an electronics workshop. The older generated hybrid mockup is not material or shader evidence.

## Core target

Clean, softly shaped retro office equipment. Calm colour areas, smooth manufactured edges, gentle highlights, readable seams and precise controls. Broad surfaces must hold up close without grain, mottling, smeared marks or bright painted borders.

**Soft forms + clean surfaces + crisp functional graphics.** This applies to every object viewed close. Commercial pack assets receive the same review as original models.

## 1. Shape and softness

- Keep recognizable manufactured construction: flat central faces, consistent thickness, deliberate joints and separate assembled parts.
- Make major corner radii visibly softer for our closer camera. Tiny edge bevels alone are insufficient.
- Give buttons, keycaps, mouse bodies, tray corners and pad edges smooth transitions. Avoid razor edges, faceted curves and black seam artifacts.
- Retain large planar masses. Do not inflate housings or make every material look like soft rubber.
- Preserve clear silhouettes and control lettering. Global blur, heavy positive mip bias, bloom and depth of field are not substitutes for soft geometry.

### Starting modelling ranges

These are proposed values for our metre-scaled assets, not measurements of ReStory. Tune at the closest supported camera distance.

| Part | Starting treatment |
| --- | --- |
| Large ABS housing | Main corner radii about 5–12 mm; smaller secondary breaks; broad flat faces |
| Bezel/frame | Gently rounded or chamfered lip; avoid a swollen tube-like perimeter |
| Typical small keycaps | About 0.6–1.2 mm edge rounding, smooth tops, narrow gaps |
| Buttons | Soft cap/lip transitions, enough segments to avoid visible facets |
| Tray/body corners | Broad rounded corners; folded or rolled edge where appropriate |
| Office blotter | Low-profile rounded slab, soft outline, subtle dark edge |
| Paper/thin metal | Appropriate thinness; soften corners and shading without inflating thickness |

Evaluate projected appearance rather than applying one bevel width everywhere. Add geometry where the silhouette requires it and inspect normals after export.

## 2. Clean surfaces first

- Start without a grunge layer. The clean object must already communicate its shape.
- Keep fine colour noise and micro-normal detail minimal on broad plastic, rubber, glass and painted panels.
- Use gentle large-scale variation only where it helps. Reject cloudy dirt over every face.
- Put wear at a few plausible handled corners, joins or labels. Wear is optional, not mandatory on every prop.
- Keep key legends, drive slots, seams, fasteners and useful marks crisp at intended display size.
- Give functional graphics enough UV space. Remove noisy detail selectively rather than blur the whole atlas.
- Never paint fixed sunlight, cast shadows, specular streaks or continuous bright edge bands into albedo.

The laptop close-up is the cleanliness benchmark. Wear on its detached bezel is a particular material story, not a universal recipe.

## 3. Colour and value hierarchy

- Use warm aged neutral plastics with controlled differences between parts. Avoid making the entire machine bright yellow cream.
- Keep the frame relatively light, the rear shell and system unit more subdued, and gaps darker without thick black borders.
- Use desaturated teal/green and muted orange selectively on controls or small labels. Do not recolour every object green.
- Let quiet warm wood support the scene; its grain should not compete with props and papers.
- Preserve warm/cool contrast without increasing saturation everywhere.
- Judge colour under project lighting. Screenshot samples contain light and grading; do not copy them directly into material albedo.

The reference screen supplies a large blue/green accent. Our blank office-view screen intentionally lacks that colour contribution.

## 4. Material and shader treatment

The user explicitly requested a custom anime shader after judging the PBR pass insufficient. Use `NOPE/Desk Anime` for the current desk: three controlled diffuse tones, cool shadows, warm light, selective graphic highlights and restrained contours. Keep consistent shadow, depth and normal passes. See `DeskClean/ANIME_SHADER.md` for the actual material controls and reproduction. The roughness table below remains useful for Blender source previews; the Unity anime shader uses graphic highlight controls instead of a PBR specular lobe.

| Surface | Proposed starting response | Visual check |
| --- | --- | --- |
| Aged ABS | Nonmetal; roughness about .68–.82 | Broad faint highlight; no wet glare |
| Painted enamel | Nonmetal coating; roughness about .48–.68 | Slightly tighter than ABS, not shiny all over |
| Rubber/blotter | Nonmetal; roughness about .82–.94 | Calm matte face and soft edge |
| Blank glass | Separate material; roughness initially .35–.50 | Restrained reflection, no dominant white hotspot |
| Exposed metal | Confined to actual fittings; tune separately | Selected small highlights |
| Paper | Nearly matte; minimal normal detail | Clean tone and readable print |

These are our starting points, not recovered ReStory settings. Roughness maps inversely to Unity smoothness. Match appearance between Blender and Unity rather than assuming equal numbers produce equal results.

Use weak or absent micro-normal maps on clean surfaces. Geometry establishes large transitions and recesses. Keep ambient/contact shading local; reject wide black halos around every join.

## 5. Light and comparison

- Retain contact between desk and props, and between assembled parts. Softness does not mean removing grounding shadows.
- Use broad transitions and controlled highlights while keeping shadowed planes readable.
- Keep lighting separate from albedo for dynamic day progression.
- Compare assets with identical camera, lights, exposure and placement. Review future lighting changes separately.
- Leave the restored room, floor, ceiling and global lighting untouched during a one-object desk study.

## 6. CRT reconstruction brief

The current pack CRT has the wrong TV-like proportions. The first repaint is an experiment, not the approved target.

1. Build a more upright monitor face with a larger screen-to-front relationship and a thinner, gently rounded frame.
2. Retain a deep tapered rear shell with readable planes. Do not merely stretch the entire current model taller.
3. Use a distinct swivel stand and visible separation from the low horizontal system unit.
4. Replace the dominant side tuning knob with appropriate small office-monitor controls.
5. Organize the case into quiet panels and meaningful drive/control groups, with selective decoration.
6. Resolve silhouette, softness and shading using clean materials before adding wear.
7. Keep useful UVs, but unwrap rebuilt parts correctly. UV preservation is not a reason to keep the wrong shape.
8. Preserve the desk footprint and deliberately re-align the screen and gameplay anchors after model changes. Verify the alignment.
9. Keep the physical screen blank in office view. The focused desktop remains dynamic; do not copy the reference wallpaper.

## 7. Later object passes

| Object | Treatment |
| --- | --- |
| Keyboard | Soft caps, clear wells, crisp legends, restrained neutral/teal/orange grouping |
| Mouse | Smooth dome, soft button division, restrained trim and plausible cable |
| Lamp | Rounded shade/support transitions, quiet enamel, limited wear |
| Phone/calculator | Smooth housings, readable controls, clean faces and selective labels |
| Tray/stationery | Soft corners, appropriate thickness, purposeful seams |
| Writing pad | Plain matte warm-neutral office blotter; no blue face, grid, ruler or repair markings |

Apply this treatment individually to each object within the user's now-authorized complete desk pass. Check the assembled desk in Unity; additional before/after sheets are no longer required. Judge relative scale together, especially phone and pen cup versus CRT and keyboard.

## 8. Context and content

- Preserve the clear central writing area and restored architecture.
- Do not import soldering gear, loose electronics, repair trays or workshop hints merely because they occur in ReStory.
- Use fictional understated labels only where useful; avoid copied brands and recognizable reference graphics.
- Keep clock, day, credits, stability, NEXT, language and focused desktop content game-controlled.
- Preserve interactions. Do not add runtime workaround code merely to place art.

## 9. Review gates

1. **Shape:** the clean model reads correctly and has noticeably soft edges at the close camera.
2. **Cleanliness:** broad surfaces have no distracting grime, mottling, faceting, seams or painted highlight rims.
3. **Detail:** useful graphics, controls and part boundaries remain crisp.
4. **Material:** plastic, rubber, glass, coatings and metal are distinct without extreme gloss.
5. **Integration:** inspect the current Unity Game view, relative scale, placement, screen alignment and relevant events. Comparison sheets are optional historical evidence, not a review gate.
6. **Scope:** no unrelated room changes or unrequested prop replacements.

Save editable Blender source and project-owned exports. Preserve vendor assets. Show actual engine evidence, record remaining differences, and do not label a colour edit a completed ReStory match.
