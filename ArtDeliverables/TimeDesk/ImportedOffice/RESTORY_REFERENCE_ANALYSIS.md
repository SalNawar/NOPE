# ReStory reference analysis — 25 September 2026

## Evidence and scope

The user supplied two references:

1. [CRT desk crop](References/ReStory_CRT_closeup.png), 430 × 453 pixels: the computer's proportions, construction, colour relationships and personal details.
2. [Laptop inspection view](References/ReStory_closeup_cleanliness.png): close-range surface cleanliness, readable detail, shading and material separation.

These are rendered images, not source assets. The exact shaders, light count, roughness, texture resolution, polygon count and division between painted and computed shading cannot be recovered from them. Observations below describe visible appearance; proposed implementation settings are our starting points, not claims about ReStory's pipeline.

The user's additional instructions are authoritative: **all close-view objects must be much softer**, and the desk must use **an office writing pad, not a blue gridded repair/cutting mat**. The reference supplies visual treatment, not permission to copy a repair workshop's contents.

## 1. Overall visual language

Both images use controlled detail. Large areas read as stable colours and simple volumes. Detail concentrates where it explains construction or use: key legends, drives, fasteners, hinges, labels and circuit components. It does not dissolve into surface noise.

Broad planes remain legible, rounded transitions carry gentle gradients, and narrow seams create separation. Neither image requires a hard toon-lighting ramp or a thick black outline around every object. Most dark borders are explainable as gaps, gaskets, recesses, edges or contact shadows.

The laptop close-up is decisive: the dark keyboard deck and screen are quiet, while key legends and exposed electronics remain crisp. The detached bezel has visible mottling, but the wear is localized to that repair part. It does not justify grime over every casing.

**Design consequence:** clean surfaces are the default. Add functional detail deliberately. Add aging only after the clean object works.

## 2. CRT construction and silhouette

The monitor reads as an upright, approximately square front in this perspective. The rear casing extends backward and tapers, with a distinctly shaded left side. A slim frame surrounds a large visible screen. The lower bezel holds small controls and marks; the picture remains the primary face.

A visible circular/elliptical swivel base separates the monitor from the system unit. The system unit is low and horizontal, wider than the monitor above it. Its front is asymmetric: relatively quiet panels on the left, stacked drive hardware toward the right, and a small warm-coloured button near the upper-right corner.

The monitor's controls are small accents around the frame. There is no dominant radio-style tuning dial occupying a wide side panel. The stand and local contact shadow make the monitor and system unit read as assembled separate objects.

Perspective prevents reliable recovery of exact dimensions. Useful constraints are relational: upright screen, thin frame, tapered depth, separate stand and low horizontal case. Additional views would be needed for precise reconstruction.

**Our mismatch:** the pack CRT is wide and TV-like, with a thick rounded surround, a broad dark inset and a large side knob. Repainting it ivory did not correct those proportions. The first study is a material experiment, not an approved geometry target.

## 3. Softness at our closer camera

ReStory keeps box construction and clear planes. Softness comes from rounded edge transitions, smooth curved parts, restrained highlights and limited texture contrast. It does not require swollen forms or blurry textures.

The user wants stronger softness because our camera is closer. Our adaptation should therefore make edge rounding more visible on housings, keycaps, buttons, tray corners and the writing pad. Retain flatter central faces so the object remains manufactured.

The first study's 1.2 mm selective bevel was too small to be the main softness strategy, and it retained the source silhouette. The next pass should address main corner radii and the frame profile first, then secondary bevels and normals.

**Design consequence:** model transitions that remain visible at gameplay distance. Do not use global blur, heavy mip bias, bloom or reduced resolution as substitutes.

## 4. Value and colour hierarchy

The CRT crop is predominantly warm brown, tan and subdued green. The computer is not uniformly bright cream. The lit bezel strip is relatively bright; the lower bezel, rear shell and system-unit front occupy darker warm values. The darker side plane gives depth without relying on heavy texture.

The screen is the major cool, saturated area: blue sky, green fields and a light mountain. Small teal and orange control accents repeat these relationships at a lower intensity. The green till, dark teal mousepad, brown desk and cardboard box support the focal object.

The laptop view confirms that materials can be dark and remain clear. Its screen, casing, key tops and key wells are separate nearby values. The exposed green board and tiny warm pointing control supply compact accents. Cleanliness does not mean making everything pale or increasing exposure.

### Screenshot samples

These are median **displayed colours**, including illumination, grading and image processing. They are useful for checking the rendered result, not for direct copying into albedo.

| Sampled region | Display colour | Reading |
| --- | --- | --- |
| Upper CRT bezel strip | `#B58D5D` | Warm restrained highlight |
| Lower bezel face | `#7C5F44` | Mid-value tan/brown |
| Rear casing in shadow | `#362217` | Deep warm side plane |
| System-unit front sample | `#47382B` | Subdued body value |
| Desk wood | `#4A2E19` | Warm dark support |
| Till front | `#273120` | Muted olive green |
| Mousepad | `#222C2D` | Dark cool counterpoint |
| Screen sky | `#3B69AD` | Saturated focal colour |

[Sampling rectangles and RGB values](References/reference_colour_samples.json) are retained for reproducibility. Sampling the small crop cannot reveal unlit base colours.

## 5. Texture scale and cleanliness

The texture hierarchy has three useful scales:

- **Large:** calm colour areas and gentle variation establish a material without mottling every plane.
- **Medium:** seams, drive faces, labels, stickers, keyboard groups, panel borders and selected wear explain the object.
- **Small:** key legends, screws and electronics remain precise, selective and organized.

The laptop's broad black surfaces have little distracting grain. Its key legends have sharp edges. The mouse is a smooth dome with a clean colour division. The mat grid is regular information, not random noise; its repair-work purpose still makes it wrong for our office blotter.

The CRT crop's low resolution softens tiny details. That is not evidence that its source textures are blurry. The larger laptop image establishes the desired close-range clarity.

**Our mismatch:** the generated atlas added cloudy variation and emphasized edges. In Unity the bright cream rim dominates. The next revision should quiet those artifacts, retain functional detail and let geometry/light describe the edges. Clean colour blocks are preferable to a conspicuous painted-noise layer.

## 6. Material response

| Family | Visible evidence | Implementation consequence |
| --- | --- | --- |
| Plastic | Quiet planes, gentle gradients, little glare | Nonmetal; clean normals; broad restrained specular response |
| Screen | Image-led CRT; calm dark laptop display | Separate display/glass treatment; control glare and retain legibility |
| Keycaps | Raised tops, dark gaps, crisp legends | Rounded geometry and precise graphics |
| Painted equipment | Muted green panels and local seams | Coloured coating, not blanket metallic shading or chips |
| Wood | Warm broad shading and subordinate grain | Slow colour variation; no competing high-contrast streaks |
| Rubber/pad | Dark simple shape, subdued surface | Matte, soft corners, minimal micro-normal detail |
| Exposed metal/electronics | Selected small highlights | Confine sharper response to relevant small parts |

The screenshots do not prove numerical roughness values. Tune coherent material families under fixed conditions and inspect them in Unity. Increasing smoothness everywhere would move away from these images.

## 7. Light and contact

The CRT's upper/front edge is warm and bright while its side and lower case are darker. The stand contact, keyboard gaps and desk contact anchor the assembly. The laptop also retains a definite cast shadow: objects do not float in uniform ambient light.

Softness does not mean removing shadows. Keep contact tight where parts meet, without oversized black halos or jagged long shadows. Broad gradients should reveal planes and curvature. Separate surface colour from warm illumination so day progression remains possible.

The images cannot establish whether the original uses baked lighting, dynamic lights, a particular AO method or painted shading. Our project must retain dynamic lighting. Do not bake a fixed light direction into new albedo maps.

Compare assets with the same camera, lights, exposure and placement. Evaluate any later lighting pass separately. This analysis does not authorize changes to the restored room lighting.

## 8. Graphics and signs of use

The CRT has small colourful marks, a yellow label, an angled green patch on the case, tiny control accents and a few personal graphics. These vary in scale and placement. They add history without covering the large surfaces.

The laptop stresses functional precision: key legends, panel hardware and board parts remain organized. A removed part can be worn while the assembly stays visually clean.

For our office, use fictional, understated service labels where useful. Do not copy brands, illegible marks as invented text, or the reference's cash total. Clock, day, credits, stability, NEXT and focused desktop content remain dynamic.

## 9. Transfer the rendering, not the workshop

Transfer clean material hierarchy, soft forms, selective accents, warm/cool relationships and grounded shading. The office's own props and clear writing area remain authoritative.

Soldering equipment, loose electronics, repair trays, repair hints and a gridded cutting mat belong to different gameplay. The user explicitly rejected the blue checked mat. Our replacement is a plain warm-neutral office blotter with a matte face and restrained edge.

The reference's CRT shows a colourful image, but our physical screen remains blank in office view. That removes a major colour focal point. Record that difference honestly rather than copying wallpaper or claiming an exact match.

## 10. Corrections to the first guide

| Earlier direction/result | Revised requirement |
| --- | --- |
| Preserve source proportions unconditionally | Rebuild the wrong silhouette, then deliberately align gameplay anchors |
| Bright cream surround as the main improvement | Thinner softer frame, restrained values, clear construction |
| Existing UVs are inviolable | Keep useful UVs; unwrap rebuilt parts properly |
| Wear proves stylization | Clean default; purposeful localized wear only |
| Tiny bevels supply softness | Shape major corners, maintain smooth normals, tune at close distance |
| Darken every recess strongly | Use narrow physical gaps and controlled local contrast |
| Borrow the repair mat | Use an office writing blotter appropriate to this game |

## Next one-object pass

Keep the CRT as the review object. First revise silhouette, stand and frame in Blender with clean materials. Confirm close-view softness. Then add restrained colours and precise small details. Present identical-camera before/after renders in Blender and Unity. Retain the first study as evidence, not the approved standard.

Apply these rules to all later near-view objects, while keeping the user's one-object-at-a-time workflow. The separately requested pad correction is authorized now. The analysis does not claim that the other props have already been rebuilt.
