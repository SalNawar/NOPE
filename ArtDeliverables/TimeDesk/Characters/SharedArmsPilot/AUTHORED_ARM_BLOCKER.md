# One authored thinking arm — concrete registration approval request

The single generated male thinking_a arm is at sources/arm_m_skin1__thinking_a.png, preserved byte-for-byte. `authored-proof.cjs` assembles it against the actual existing body/head/outfit/hair/accessory references. `authored-comparison.svg` embeds every image; authored-comparison.png is the exact librsvg render. The arm is truly authored continuous artwork, not neutral arm pieces rotated at an elbow. The female source was not generated against an unresolved pipeline.

Inspection: rounded elbow and coherent hand eliminate the previous articulated-cut joints. Direct placement still FAILS registration: upper arm/forearm too long, fingers miss chin, generated alpha includes a faint glow. This is not evidence that authored shared arms fail. It establishes that raw generation alone did not obey the registration contract.

## Exact next operation requiring approval

On THIS source only, create NEW pilot outputs, preserving all existing art and raw bytes:

1. Remove the generated glow by isolating the opaque arm silhouette, preserving its antialiased outline (an alpha/colour segmentation mask with a bounded edge band; not repainting).
2. Apply a measured similarity registration (uniform scale, rotation and translation) to fit the shoulder attachment and chin-contact hand anchors on the 1024x1536 canvas. No deformation of the existing torso/head/clothing. A provisional starting fit is about 0.65–0.75 scale and a modest clockwise rotation around the generated shoulder, followed by manual anchor QA; these numbers are not an accepted transform.
3. Extract only the chin-contact fingers/hand pixels into hands_m_skin1__thinking_a above head/hair/accessory; retain the rest as the reusable arm layer. Do not include wrist/sleeve in the foreground Hands layer.
4. Render the actual registered reusable layers against the existing pilot, inspect shoulder seam/chin contact and repeat with accessories off/on before deciding whether a female authored arm should follow.

The task's image-editing restrictions require the built-in tool unless another method is explicitly authorized. The parent asked for a ready source and precise needed operation if deterministic extraction/alignment becomes necessary. This request is that gate. No Python/.NET/sharp raster editing or cleanup was done; sharp only rendered the SVG composition.

Approval needed: allow deterministic silhouette extraction, similarity registration and foreground hand-mask extraction on this one new generated source into new pilot files, using the project's existing image-processing tools or a narrowly scoped equivalent. No full redraw, additional eras/roster or existing asset replacement is requested.
