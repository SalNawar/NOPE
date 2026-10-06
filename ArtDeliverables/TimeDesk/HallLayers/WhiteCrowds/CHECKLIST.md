# White crowd variations â€” task checklist

User direction: reuse the approved ReStory-style white silhouette crowd assets, make them more transparent, add interesting compositions and scatter them. Earlier request: stationary groups with independent fades and color matching the hall lighting.

- [x] Locate approved original six crowd meshes/atlas; leave them unchanged.
- [x] Remove the rejected detailed commuter additions and restore the scene.
- [x] Generate complementary faceless white silhouettes: singles, pairs, staggered groups and open asymmetric clusters.
- [x] Combine original and new compositions with believable perspective scale and lower opacity.
- [x] Keep the focal traveler area, stairs and door entrances readable.
- [x] Match all four hall lighting states and intermediate slider values.
- [x] Implement staggered fades while all group transforms remain fixed.
- [x] Inspect actual scene at multiple population times and both pan endpoints.
- [x] Verify no movement, alpha range, palette response, asset references and Unity errors.

Do not mark visual checks complete merely because the install/compile succeeded.

Review evidence: 601 sampled seconds, 4â€“13 visible groups, alpha 0â€“0.46, all positions/rotations/scales unchanged. Seven lighting-hour captures; six population captures; pan endpoints/midpoint inspected. Live Play at 23.47s and 55.47s shows population changing from 9 to 10 groups. Native compile/console error count was zero after install and Play captures. Original crowd source assets unchanged. The main traveler focus, stairs, elevator and door signs remain readable through the transparent crowds.


## More groups / wall placement correction — October 7

User rejected the previous wall-side anchors. Previous visual sign-off was insufficient.

- [x] Expand to 25 placements using approved silhouette compositions.
- [x] Move the right-door groups inward into walking lanes.
- [x] Inspect every group together, including its entire foot span, in normal and left views.
- [x] Verify fades, fixed transforms, lighting response and Unity errors after placement changes.
- [x] Save and show the checked scene.

Placement review evidence: PlacementReview-2026-10-07-Final/all-groups.png and all-groups-left.png show every anchor simultaneously. Corrected a remaining broad wall-side group and a portal overlap found during inspection. 25 placements, 16 main-floor / 9 balcony. Sampled population 8–22 groups; alpha 0–0.46; fixed transforms. Night and normal population captures inspected; Unity error count zero after final install and save.

Live Play frame 25s inspected: crowd feet remain on floor, rail occlusion works, and the central traveler area stays clear.
