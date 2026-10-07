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

## Station-life silhouettes — October 7

- [x] Generate 32 different human activity / station-life compositions; preserve simple alpha-only white style.
- [x] Cover cleaners, luggage, police, monks, families, students, commuters, railway staff, couriers, tourists, accessibility, workers and solo waiting poses.
- [x] Expand placement to 37 groups, including 15 bridge groups and extra distant floor figures.
- [x] Inspect new shapes and every foot placement in native front and left captures.
- [x] Verify fades, light palette, stationary transforms and clean Unity compilation.
- [x] Save and inspect live Play before completion.

Station-life review: all-group front and left screenshots inspected; seven lighting-hour frames and population samples captured. Minor adjacent-cell atlas fragment found and excluded by native component UV bounds. Distant groups receive longer staggered holds, keeping the rear populated. Scene saved with synchronized normal-view light plane; zero Unity errors before Play.

Live Play at 31 seconds inspected: new roles, background and bridge populations are visible; native errors zero. Final sampled population 14–33 groups, alpha 0–0.46, unchanged transforms. All 32 new composition indices are represented in the 37 placements.

## Rear density follow-up — October 7

- [x] Add thirteen extra rear-floor compositions: center lane, space below bridge, and clear lanes immediately in front of rear bays.
- [x] Longer staggered rear holds; 50 total placements, 35 floor / 15 bridge.
- [x] Inspect feet, wall clearance and portal clearance with every new placement visible.
- [x] Verify actual live rear density, independent fades and lighting.
- [x] Save and show current view.

User refinement: fewer in front, more behind. Removed three broad near groups and moved five foreground compositions toward the rear bays. Only two floor placements remain beyond source foot Y=500; retained all 32 new station-life types. Current total: 47 placements, 32 floor / 15 bridge. Review not closed until updated captures inspected.

Latest image clarification: concentrate behind the two rear portals, reduce the front-portal area. Revised to 45 placements: 24 behind the rear bays, 15 bridge, four middle-floor activities, two near-floor groups. Rear hoops occlude far crowd rims; rear glass attenuates figures behind it. Revised distribution awaits native visual inspection.

Final distribution inspected in all-group normal/left-pan frames and night capture: 24 rear-bay groups, 15 bridge, four middle floor, two near-floor. Source-registered hoop occlusion and glass attenuation preserve the rear portals in front of the crowd. 601 sampled seconds show 25–37 active groups, alpha 0–0.46 and unchanged transforms.

Live 30-second Play capture inspected: crowd concentration is behind the rear bays; near/front portal walking lanes are sparse. Zero Unity errors. Saved main camera and lighting plane remain unchanged. All 32 station-life types still referenced.
