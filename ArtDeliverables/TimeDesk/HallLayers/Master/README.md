# Hall master: composition before separation

Latest composition: scifi-worn-composition-v2.png. This is a generated composition study, NOT a Unity render and NOT a finished albedo asset. The foreground desk is contextual; do not replace gameplay/PC from this image.

User requires genuine runtime relighting, not just morning/evening tinting. Lighting reference supplied by user: https://www.youtube.com/watch?v=1h-hSlffawM (EVERYTHING you need to know to LIGHT your 2D Game). Only page title could be retrieved in the current research pass; no claim of watching its content.

Production requirements:
- One coherent composition establishes perspective, support surfaces, scale and material style before extraction.
- Matching base-colour layers must remove sunlight, directional shading, cast shadows, reflections and luminous portal effects. The composition still contains painted shading and cannot be used as lighting-neutral albedo as-is.
- Separate city, architectural shell, floor, fixtures/railings, crowds, flag cloth/emblems, portal frames and energy. All text is an editable overlay.
- Registered surface normals and light-response masks provide runtime response. Shadow/occlusion geometry or masks must reflect architectural depth; a single image cannot supply correct 3D shadows automatically.
- Lamp/portal emission is separate and runtime controlled.
- Verify a moving local light across a wall/floor/prop sample before processing the whole room. Test lights off, moved, recoloured and evening. No residual painted light should remain.
- Choose renderer/compositing deliberately: existing preview uses a 3D forward renderer with a 3D desk. URP Light2D does not automatically affect that custom forward shader. Do not silently change global render settings or gameplay.

Retain grand tower terminal layout, left panorama, platforms and circulation. More visible functional sci-fi retrofit, localized neglect, modern recessed station storage, one displayed painting and an artifact niche. Repeated wall fixtures attach to matched structural bays at consistent real-world heights. No scattered lantern poles or floating props. Current master is still subject to visual review before layer extraction.

Built-in image generation was used. Prompt: preserve camera/desk/layout; replace hall lanterns with matching bay-mounted recessed light panels; add utility trunking, service hatches, vents, portal couplings, floor access strips and recessed lockers; add chipped bases, patched plaster, water streaks, repaired tiles and faded flags; request neutral diffuse base colours with no directional light or glow. Output remains a composition study until baked-light removal is verified.

Latest master: scifi-worn-composition-v3-no-portal-effects.png. All four blue portal effects removed by built-in image editing; empty rings show reconstructed background. Portal energy/VFX is deferred. Prompt requested only energy/glow removal with composition preserved. Still a composition study, not verified neutral albedo.

## Day/night reference verified from tutorial transcript

User reference: https://www.youtube.com/watch?v=F9NFSTOGYXg&t=28s — How To Make Simple 2D Day-Night Cycle, OB Game Dev. Transcript reviewed on 2026-09-27.

The demonstrated technique uses Sprite-Lit-Default materials and two 2D spot lights for day and night. A common parent rotates both lights. A normalised 0–1 time value evaluates separate colour gradients: warm sunrise/sunset/day, cool night; the inactive light fades to black. A UI slider demonstrates the time input. This is runtime light movement/colour control; it does not itself demonstrate normal maps, architectural occlusion, physically correct window shadows or baked-light removal.

Use this as the base day/night control pattern for the hall. Keep the artwork neutral and portal VFX deferred. Extend independently for interior fixtures and proper window/architecture occlusion. Claude supplies authoritative gameplay time; the art controller receives it. Do not put a second independent gameplay clock in art.
