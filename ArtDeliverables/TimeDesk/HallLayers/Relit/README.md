# Dynamically lit terminal

Open `Assets/Art/Office/TerminalRelit/DynamicTerminal.unity`, or use **Tools > Terminal Art > Open Dynamically Lit Terminal**. This supersedes the painted-background preview for lighting evaluation. The earlier preview is retained as a composition reference.

The hall is now mesh architecture: floor, window mullions and transparent glazing, piers, ceiling coffers, stairs, bridge, upper platform, portal housings, luggage compartments, grounded benches and artifact plinths. The approved desk is cloned with its original meshes, materials and transforms. OfficeScene and gameplay assets are not modified.

## Live lighting

Select **Dynamically lit grand terminal**. Its **Terminal Lighting Rig** component has an **Evening** slider, from 0 (morning) to 1 (evening), and context-menu presets. Sun direction, intensity, colour, ambient illumination and local lamps change together. You can also rotate **Realtime window sun** directly to inspect moving shadows.

- The architecture image is absent from this scene and prefab.
- No lightmaps, baked shadow textures or lightmapping-static objects are authored.
- `NOPE/Terminal Relit` shades both geometry and alpha-clipped decorative cards, receives main/additional light shadows, supports screen-space contact shading and has alpha-aware depth/shadow passes.
- Banners, one displayed painting and two artifacts use a new neutral decorative atlas. Some illustrated form detail remains in the artifact artwork; full view-dependent form shading would need modelled artifacts or authored normal maps. Hidden volume proxies provide their cast shadows.
- Only luminous lamp panes, portal energy and sign lettering are intentionally emissive/unlit.
- The preview has its own URP asset for long-distance shadows. Its component temporarily selects that asset and restores the prior pipeline when disabled/closed. Do not commit a changed project quality setting from an open preview.

## Verification captures

`morning.png`, `evening.png` and `opposite-light-test.png` are actual Unity renders from the same camera and geometry. The opposite-light capture changes only the directional light rotation from the morning setup; it is a diagnostic, not an art preset. `left-window.png` verifies real geometry from a second camera angle. `validation.txt` records the no-backdrop/no-lightmap and shader checks.

## Integration boundary

`DynamicTerminalArt.prefab` contains hall art only. The scene includes the desk, preview camera and lighting rig. This is an art scene; it does not replace OfficeScene, install gameplay routes, connect the time-of-day system, or provide input for camera panning. Claude can drive `TerminalLightingRig.SetTime(0..1)` through an agreed gameplay hook. Renderer index 1 is the project's existing forward renderer.

Regenerate with `TerminalRelitProduction.Build` (editor menu or Unity batchmode; omit `-quit`, as capture and exit are handled by the builder). Never use the removed legacy art rebuild menus. Neutral atlas generation used the built-in image tool; the exact prompt is in `neutral-decor.prompt.md`.

## Mockup alignment revision — 27 September

The front piers now project at 32.3% and 67.7% of image width. The 36 m bridge joins their line at Z=50; its deck and the right gallery both finish at Y=11. The piers include passages at this level. The west landing measures 14 x 18 m with a supported stair. Existing Hall_Portal prefab geometry replaces all four simplified rings, with the two upper gates angled toward their gallery.

Added burgundy/ivory/brass floor inlays, a transparent four-pane desk partition, a luggage alcove and cart, a deeper signed right passage, vaulted ribs, pendant lights and denser city geometry. Floor reflections render the live scene. Morning, evening and reversed-sun captures retain zero baked lightmaps. The measured bounds and comparison are recorded in `measured-anchors.md` and `MOCKUP_AUDIT.md`.

The composition is closer, but the city and ceiling remain simpler than the illustrated reference, the luggage display is less prominent, and floor reflections are subtler. These are remaining art-quality gaps, not evidence of an exact match. Runtime performance has not been profiled.

The subsequent stair correction replaces the oversized flight with a 3 m wide main/return stair and intermediate landing, reduces the west landing to 14 x 12 m, and brings luggage storage into view beneath it. The audit records the final dimensions; earlier 14 x 18 m dimensions are superseded. Upper lamps, pendants and displayed artifacts were repositioned after camera-view inspection.
