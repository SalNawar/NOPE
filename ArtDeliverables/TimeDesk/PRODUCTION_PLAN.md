# Time Sorter: corrected art production brief

Source of truth: Reference/ART_ASSET_LIST.md, exported from the user's Google Doc on 2026-09-23. The user's lighthearted anime direction takes priority over the document's darker mood references. Scope is artwork; game code is already completed by the user.

## Existing drafts

The existing Concepts, Office, Characters, Desktop and Timeline images are preliminary drafts, not an approved production set. The explorer in modern overalls is NOT a historically suitable Greek traveller. The desktop concept's Helios/Ancient Greece pairing is incorrect. Existing dimensions, atlases, framing and colored stability monitor do not satisfy all new delivery contracts. Preserve these drafts as references only.

## Visual identity

The office and fictional OS use friendly late-1990s/early-2000s design: cream plastic, soft blues, readable beveled controls and original branding. Characters share clean anime linework, expressive faces and soft shading, while their garments, grooming and possessions follow their own time and place. No Microsoft artwork or logos are needed.

## Canonical era mapping

| Investigation nations | Era |
|---|---|
| Aegyptus, Latia | Ancient Rome |
| Albion, Norvik | Medieval |
| Helios, Solaris | Future |

The separate historical timeline roster uses Greece, N.Germany, Japan, Egypt and China. Preserve the current roster rather than merging data for an art task. Repository profiles specify Classical Greece, Nazi Germany, Imperial Japan, Ancient Egypt and Imperial China. Egypt's dynasty, China's dynasty and Japan's precise period remain unspecified; establish these before producing those historical costumes. Use invented insignia for Germany as requested in the asset list.

## Character research and consistency

Before generating each character, record period/location, occupation/status, garment construction and fastening, hairstyle/head covering, footwear, materials, and carried objects. Consult museum objects or historical references. Check legendary characters against their actual asset IDs and trueEra; wanderers must not automatically receive arbitrary fantasy costumes.

For Classical Greece, use referenced chiton or peplos construction and a suitable himation where appropriate, with period-appropriate hair arrangement and accessories. Choose garments according to the person and role; do not make every traveller a warrior or add a laurel crown by default. Exclude modern overalls, zippers, trainers and backpacks unless the game explicitly calls for an anachronistic clue. Document any intentional anomaly separately.

References consulted:
- https://www.metmuseum.org/ko/essays/ancient-greek-dress
- https://www.ysma.gr/en/educational-actions/educational-resources/museum-kits/ancient-greek-dress/
- https://parthenonfrieze.gr/en/explore-the-frieze/east-frieze/

Generate each body first, then derive idle2 and portrait from that same design. Keep face, clothing, palette and accessories consistent. Standard visitors use the requested 240x440 body and 240x300 portrait contracts. Do not substitute a flattened contact sheet for separate deliverables.

## Production sequence

1. Establish historical costume references and resolve the unspecified historical periods.
2. Correct a Classical Greek traveller as a visual benchmark before expanding the cast.
3. Produce the 11 Tier 1 drop-in files and 18 timeline posters using exact source filenames and dimensions.
4. Produce the 24 investigation visitor trios, 21 legendary body/portrait pairs and the existing historical cast as specified; retain one identity across variants.
5. Produce individual desktop, investigation and nation document assets.
6. Produce booth extras, day-flow, home, title, ending and shared icon assets.

## Delivery checks

- Follow every exact path, filename, size and aspect ratio in the source list.
- Props/characters: actual alpha, complete object within frame, no external drop shadow; consistent top-left lighting.
- Backgrounds: opaque. Keep gameplay focal areas uncluttered.
- Leave runtime text, numbers, document claims and variable displays blank. Only explicitly allowed static labels may be baked in.
- Stability monitor: white/light gray grayscale because the game tints the entire sprite.
- Partition: one reusable partition asset, not a pair baked into a widescreen frame.
- UI controls: distinct state files and usable nine-slice edges where requested.
- Preserve existing .png.meta GUIDs for Tier 1 replacement. Use Sprite Single, FullRect, center pivot and source-specified PPU scaling (400 for 4x replacements).
- Visually inspect historical coherence, hands, silhouettes, alpha edges, readability and consistent variants before calling an asset complete.
- Tier 2 art still needs loader hooks according to the source document; generating files alone does not integrate it. No code changes are included here.

## Outstanding specifics and current blocker

Family defaults in source are Partner and Kid; appearance and named condition variants are not defined by the asset list. Slot lever dimensions are unspecified. Resolve those before their production batch.

Image generation returned usage_limit_reached. The reported reset is 2026-09-23 at 22:04 Asia/Riyadh. No corrected historical assets have been generated yet, and no automatic retry is scheduled.
