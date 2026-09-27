# Neutral master pass and lighting preparation

Built-in imagegen edits saved as right-hall-v2-neutral.png and left-city-v2-neutral.png. Originals preserved. No scene edits or layer cutting performed.

## Review
Hall floor reflections and visible lamp/elevator glow removed. City now has subdued sky and less directional contrast. Form shading and city water highlights still remain; these are neutralized painted candidates, NOT verified pure albedo. The city panel's railing now slopes consistently toward the hall but its endpoint still does not exactly meet the hall edge. Skyline continuity also needs a registered seam pass. Do not label these seamless or start final extraction yet.

## Production layer contract
Use one master pixel coordinate system across both panels; retain common height, pixels-per-unit, pivot convention and overlap metadata. Do not independently trim/rescale layers. Fill hidden pixels for the allowed camera pan.
- Separate sky and distant city from all glazing/mullions; skyline haze is controlled separately.
- Separate glass, architectural shell, floor, bridge, rails, fixtures, portal frames, lockers/displays, flags and editable signage.
- Keep fixture emission and portal VFX out of base colors.
- Supply normal maps with consistent orientation, occlusion and shadow geometry/masks rather than baking time-dependent shadows into diffuse artwork.
- Gameplay text and flag marks remain overlays. No crowds or desk in these assets.
- Test transparent edges for fringes and order each layer explicitly.

## Runtime lighting gate
The linked OB Game Dev tutorial uses two rotating Light2D sources and time-evaluated color gradients. Its transcript does not establish an occlusion/normal workflow.
Existing custom forward-rendered hall material is not automatically compatible with Light2D. Verify the actual rendering path before implementing: use an isolated compatible 2D rendering setup or deliberate custom forward lighting; do not change project renderer blindly.
Claude owns time/gameplay. Art consumes a normalized time value through an agreed hook, no new authoritative clock.
Test one floor/window/pier patch at neutral noon, morning, evening, night, with lamps off/on and moving light from both sides. Check no fixed highlights/shadows remain, no double illumination, correct shadow direction, no bright cutout edges.
Test the complete left pan and seam at all these states before accepting layers.
Do not claim that simple color tinting removes baked lighting.

## Prompts
### Hall
Lighting removal pass ONLY on attached stylized game hall. Preserve exact camera, 3:1 framing, perspective, contours, composition, object placement, floor pattern and all geometry. Produce a BASE COLOR / DIFFUSE ALBEDO-style painted master for dynamic runtime lighting. Remove ALL floor reflections, reflected window stripes, polished specular glare, light pools, highlights from lamps and elevator, baked sunlight, cast shadow gradients. Floor must be MATTE evenly colored worn stone, retain tile seams, burgundy bands, chips and stains. Turn every lamp and elevator light OFF: diffusers neutral dull grey-white. Door hardware matte bronze, no luminous gold. Remove direction-dependent bright sides/dark sides from columns and city buildings as far as possible while preserving thin outlines and restrained form cues. Recesses remain readable but not black pools. Sky neutral desaturated overcast grey-blue, no sun or bright silver cloud edges. City river matte, no sun glitter. This is not night or recoloring: preserve true grey stone, burgundy cloth, ochre lockers, charcoal mechanisms. NO bloom, color grading, vignette, ambient spotlights or new text. NO changed architecture, no people, no desk, no new foreground rail. Portal openings remain empty, exactly three lower and two upper. Do not make a final lit illustration; make a flat diffuse-color paint pass with clean stylized edges for later normals and dynamic Unity lights.
### City
Two inputs: image1 is edit target city panel. Image2 is neutral hall reference, to sit immediately to RIGHT of target. Output ONLY edited city panel, same3:1 size. This is the LEFT EXTENSION OF SAME SHOT, not a different camera.
Match image2 neutral diffuse base-color treatment: remove bright blue sunny sky, directional building highlights, river glitter, glass reflections, floor gloss and any lamp glow. Soft desaturated grey-blue overcast sky with no bright cloud rims. Retain varied material colors and visible worn city geometry, not grayscale. Same restrained flat stylized painted appearance as neutral hall. No people/text.
Correct target window/railing perspective to extend hall LEFTWARD: at target RIGHT edge, top brass handrail meets at73% image height and the pale stone wall cap beneath meets at81% height, EXACTLY continuing the left edge of image2. Both lines slope DOWN as they extend toward LEFT (continuing existing hall's perspective), going out of bottom edge before reaching left edge; do NOT draw a level horizontal rail all across picture. Below these lines at the bottom-right draw continuation of ochre/graphite locker wall like reference; most remaining panel is transparent glazing with city behind. Slim vertical mullions, match thickness and tone, no thick new pillar. City horizon at about30% image height, consistent with right reference. City landscape features at RIGHT boundary continue the river/towers from LEFT boundary of image2. This panel should show a huge city view, not another hall. Keep original city tower language and broadly same districts while resolving right-edge matching. No portal, desk, furniture or foreground barrier. No signage/emblems. No new cinematic lighting. Geometry is a continuous panoramic projection, not two shots.
### City join correction
Precise localized edit ONLY of railing/locker wall geometry. Preserve all city, sky, window mullions and overall framing unchanged. This panel will join a hall at its RIGHT edge. Currently top railing hits right edge around y420 in a724px high image, which is too high. LOWER the railing, stone cap, and locker wall together: top brass rail must intersect right boundary at y530 (73.2% from top). Stone wall cap at right boundary y587 (81.1%). Preserve rail slope rising toward right: at x1500 toprail y700; at x2172 toprail y530. Thus railing appears only in LOWER RIGHT portion; the left two-thirds of image has no rail. Fill vacated upper area with uninterrupted city river/buildings behind transparent glass. Don't move city or windows; do not crop or rescale. No new people, text, reflections or lighting. This is exact endpoint alignment for a panoramic join. Output same3:1 image.

