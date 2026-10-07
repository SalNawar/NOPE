# Warm stone and polished floor

- [x] Edit existing hall painting using user reference for materials only.
- [x] Inspect generated layout, warm walls, glossy floor, preserved portal/rail/window positions.
- [x] Install as a new asset; preserve current scene registrations and active systems.
- [x] Check window masks, crowd/portal registration and floor extension in Unity.
- [x] Check morning/noon/evening/night and left pan.
- [x] Check Unity errors and saved changes.

Generated with built-in image_gen; original output exec-c5b9763e-915b-40c0-85b0-5a1eaf5d00ac.png. Painted reflections are part of the hall background and receive the existing time-of-day shading.

Verification: native front captures at 08:00, 12:00, 16:30, 22:00 and left-pan noon inspected. Live Play capture included. Window apertures/rail/portal registration retained. No existing scene transforms changed; cashier pulse config unchanged. Zero Unity errors. Reflections are painted architectural reflections, not live reflections of moving characters.

Crowd colour follow-up: neutral white/grey requested. Removed warm hue from day-cycle palette, retaining brightness changes, opacity and fades. Live capture inspected; added Crowd opacity (%) slider in Hall lighting, 65% default. Verified 0% hides, 100% reaches opaque, 65% increases visibility while retaining group variation; neutral RGB and stationary transforms confirmed.
- [x] Black crowd follow-up: set silhouette tint to black; retained opacity slider, placement and fades. Inspected live Unity capture and checked console (zero errors).
