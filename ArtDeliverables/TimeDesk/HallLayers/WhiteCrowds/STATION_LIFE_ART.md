# Station-life silhouette art

Generated with the built-in image-generation tool. Alpha-only runtime recoloring preserves the approved translucent white silhouette style. Source PNGs remain unchanged. The authoring tool uses connected-component bounds to exclude minor neighboring-cell fragments from the native mesh UVs.

## Assets / final prompt briefs

`Assets/Art/Office/AnimeHallLayers/Completion/WhiteCrowds/WhiteCrowdActivities.png` — 4×4 transparent full-body silhouette atlas: cleaner with mop, janitor with trolley, suitcase traveler, luggage pair, police pair, lone officer, three monks, monk with staff, parent with children, crate worker, hand-truck porter, window observers, technician with clipboard, wheelchair user with attendant, elder with companion, kneeling mechanic with colleague.

`Assets/Art/Office/AnimeHallLayers/Completion/WhiteCrowds/WhiteCrowdStationLife.png` — 4×4 transparent full-body silhouette atlas: phone commuter, students with backpacks and suitcase seating, business traveler, map-reading tourists, railway conductor, attendant guiding traveler, parcel couriers, musician with cello case, family with stroller, elderly couple with shopping trolley, construction workers, photographer, food vendor and customer, suitcase sitter and stretching companion, healthcare workers, traveler with folded bicycle.

Shared prompt constraints: separate equal cells with transparent margins; human-only, solid blue opaque source silhouettes for alpha extraction; no faces, interior clothing details, outlines, text, shadows or background scenery. Varied body types, ages, coats, props, negative space and static poses. Runtime ignores source RGB and tints silhouettes according to hall lighting.

## Scene

45 placements: 24 behind the rear portal bays, 15 on the bridge, four middle-floor activities and two near-floor groups. All 32 new activity types are represented, alongside retained original/simple compositions. Rear-floor groups use independently phased 84-second cycles with 56-second holds to keep the rear hall populated. Bridge groups hold longer than foreground groups. Fades remain four seconds and all transforms remain stationary.

Review evidence: RearPortalPopulationReview-2026-10-07.
