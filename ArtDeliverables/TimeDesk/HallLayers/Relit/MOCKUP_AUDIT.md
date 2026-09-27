# Mockup comparison and correction plan

Reference: `../Architecture/RevC/terminal-player-view-mockup.png`. Compare to the same forward camera, not a free Scene view. The original desk meshes, materials and transforms are preserved; the reference's live PC content is not part of this art-only scene.

## Structural mistakes found

| Element | Problem in the initial relit scene | Correction and check |
|---|---|---|
| Main piers | Too narrow and too far apart. Right pier masked Gate 03. | Set the front structural piers to approximately one-third/two-thirds of the image, with larger cross-sections. Review camera projection, not just world positions. |
| Sky bridge | Deck at 14.5 m, gallery at 8 m; crossed the entire hall towards glazing. | One shared upper-floor height. Bridge meets the structural pier line and gallery; the west continuation is an interior corridor, separated from glazing. Assert equal deck heights and touching bounds. |
| Left landing | Shallow shelf, unlike the broad waiting landing in the reference. | A 14 × 18 m raised landing, with a 5 m wide supported stair. Check stair risers, platform edge and room clearance. |
| Upper portals | Faced the player directly rather than their gallery. | Existing portal meshes rotated towards the gallery; inspect both visible aperture and physical clearance. |
| Portal model | Simplified new torus replaced the detailed existing asset. | All four gates instantiate the existing Hall_Portal prefab geometry. Original prefab/material assets remain untouched. |
| Luggage | Free-standing small bank; no architectural storage area. | Numbered three-row compartments in an alcove below the stair, travel cases and a grounded luggage cart. |
| Partition | Missing clear barrier across the player's raised landing. | Four glass panes, slim continuous top rail and grounded posts. |
| Floor | Generic stone squares and straight side stripes. | Cream approach, burgundy grid bands, brass borders and concentric inlay around Gate 01. No shadow imagery used. |

## Material and composition gaps

- The mockup has a vaulted, detailed overhead space and visible warm pendant fixtures. Replace the flat grid ceiling with a ribbed barrel vault and suspended lanterns.
- The mockup has a dense, varied city silhouette. Add layered towers, stepped crowns and spires instead of sparse uniform blocks.
- The departures board was too low and its sparse text did not fill the display. Raise it and add aligned gate/time columns.
- Large stone surfaces were flat and muddy. Keep neutral albedo, restrained surface wear and real-time lighting, with warmer ambient fill and depth haze.
- The polished floor needs live response. A planar reflection is rendered from the current scene, not a baked floor image; keep it subtle so it does not become a mirror.
- Decorative banners were narrow because transparent atlas padding was included. Use tight UV regions and correct cloth proportions.

## Completion evidence

Inspect the new morning, evening and opposite-light captures after these corrections. `measured-anchors.md` records actual geometry bounds, deck heights and camera-projected centres. `validation.txt` checks no architecture backdrop, no baked lightmaps and shader compilation. Do not claim pixel-perfect equivalence: geometry, perspective and illustration contain different degrees of detail, and artistic review remains necessary.

### Verified revision

Latest captures: 27 September, 15:18. Front pier centres project at X=0.323/0.677; bridge and gallery tops both equal Y=11. Four existing portal instances and four partition panes are present, bench feet are grounded, and validation reports zero baked lightmaps and zero Terminal Relit shader errors. The bridge now terminates visually at the front piers and the partition no longer clouds the floor.

Remaining visual differences: the city silhouette and ceiling ornament are simpler than the reference; the luggage alcove is less prominent; the stair occupies more of the central view; the departure board typography needs further polish. Evening lighting is intentionally much darker than the daytime reference. No claim of pixel-perfect equivalence or runtime performance validation is made.
