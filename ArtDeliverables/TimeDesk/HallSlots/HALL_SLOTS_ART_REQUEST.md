# The hall's swappable slots: art request (Track H, 2026-10-07)

Saleh (2026-10-07): "We have all these variables and layers - we need assets that you can easily swap as the variables shift, to create unique combinations", then "the hall is split into layers at the moment for this reason" and "I need the GPT document to start generating inner hall assets". This is that document: what to paint for each swappable part of the anime terminal hall (`Assets/Art/Office/AnimeHallLayers/AnimeHall.unity`), at what size, where, and when the game shows it. **No art is required to play**: a missing file keeps the hall exactly as painted today.

Status: this request and its templates land first (docs only); the runtime that shows the files (the slots asset, the gameplay link, the cheat buttons and the capture tool named below) follows on branch `feat/hall-slots`. Files delivered before it lands simply wait in their folders.

## Read this first: which picture you paint against

- The hall's registered layers (`Assets/Art/Office/AnimeHallLayers/Textures/00-…57-*.png`, listed in `layers.json`, 2172 x 724 each) were cut from the earlier v8 painting. **In the live scene all 58 are disabled**: what the player sees is one composite painting, `Assets/Art/Office/AnimeHallLayers/Completion/WarmStone/HallWarmStone.png` (the scene's layer `62 Approved deeper architecture`, sorting order 58, material `WarmStoneArchitecture` / shader `NOPE/Hall Deep Layout`). The warm-stone repaint moved things: the old layer PNGs no longer sit where the objects are drawn now (the banners moved about 65 px right, the door signs about 60 px, the framed artwork about 55 px). **Paint every variant against `HallWarmStone.png`**, never against the old layer PNG. The old layer file is named below only as the slot's name and as a reference for what the object is.
- Same canvas as every layer: **2172 x 724 px, RGBA, transparent background**, registered pixel for pixel to `HallWarmStone.png` (its (0,0) is your (0,0); no offset, no scaling, no crop). The game places the file over the painting exactly as the layers were placed.
- A variant is drawn **over** the composite painting (it is one picture, so nothing of the painting can be "behind" your art). Therefore: (1) a variant that replaces an object must cover the old object completely where it shows (opaque over the old banner, sign, plate...), so nothing of the old one peeks out; (2) anything that stands **in front** of your object in the painting (a railing, a post, a rod) must be left **out** of your art (transparent there), so it still reads in front. The templates show both.
- Paint only inside the slot's box (the magenta box of its template). Pixels outside it are ignored by the review and may be cleared.
- If your pipeline prefers it, first re-cut the warm-stone painting into new registered layers for these objects (same canvas, same naming `NN-name.png`), then paint the variants from those: the game only needs the variant files below.

## Files and naming

- Deliver each variant to `Assets/Art/UI/Resources/Hall/Slots/<slot>/<variant>.png` (the slot is the folder; the import settings are automatic: a sprite, at most 4096 px, no mipmaps). Example: `Assets/Art/UI/Resources/Hall/Slots/13-flag-left-cloth/egypt.png`. A slot of a registered layer keeps that layer's file stem as its folder name; a new overlay is named `new-…`.
- **Alternates** (optional, any variant): to give a variant several interchangeable paintings, deliver `<variant>_1.png`, `<variant>_2.png`, … and tell the gameplay side the count (one field, `alternates`, in `Assets/Data/Config/HallSlots_Default.asset`); the game picks one per run, so runs differ but a replay shows the same hall.
- The game picks a slot's variant from the hall's variables (below); the highest priority whose condition holds shows; with none, the painting stays as it is (or the slot's fallback shows). The rules live in `Assets/Data/Config/HallSlots_Default.asset` (`HallSlotsSO`), editable in the Inspector; the conditions' grammar is in `docs/superpowers/specs/2026-10-07-hall-slots-design.md`.
- Templates: `ArtDeliverables/TimeDesk/HallSlots/templates/<slot>.png` (the hall around the slot at daylight and at night, the paintable box in magenta, window glass hatched red), `_canvas_guide.png` (every slot on the whole canvas) and `_blank_canvas_2172x724.png` (a transparent canvas to paint on).

## Style and light (every slot)

- Match `HallWarmStone.png` exactly: clean 2D anime cel painting, the same dark brown-purple ink outline weight (about 2 px at canvas scale), two-tone cel shading, the same restrained palette (cream stone, oxblood, slate, brass, graphite), the same one-point perspective (follow the template's floor and wall lines; the hall's vanishing point is near canvas x 1085, y 300). Objects keep the scale of their neighbours.
- **Paint at neutral daylight** (as the hall looks in `HallWarmStone.png`). Do not paint night, lamp glows or cast light: the game tints your art with the hall (morning, noon, evening, night), adds the floor's cast shadows on the floor, and cuts out the window glass (the red hatch: nothing is ever drawn there). Avoid pure white in the ceiling band (top 180 px, right of x 782): the night lighting makes the ceiling's brightest neutral pixels glow.
- **No legible words or numbers** in any variant (the Translation Lens cannot read painted text): emblems, pictograms, colours and patterns carry the meaning.
- Culture variants use each nation's passport emblem and the game's culture theme colours (the PC theme and the passports already use them):

| Nation | Folder name | Passport emblem | Passport cover | Theme chrome / accent | Motifs |
|---|---|---|---|---|---|
| Egypt | `egypt` | WingedSun | #1F4A2E | #1C4E80 / #C8962E | winged sun disc, lotus and papyrus borders, lapis blue and gold |
| Iraq | `iraq` | Octastar | #1E1E22 | #1B4F72 / #D4A017 | eight-pointed star of Ishtar, glazed-brick blue with gold lions-gate stepped border |
| Greece | `greece` | Laurel | #1E2F5C | #0D5EAF / #1F6F8B | laurel wreath, meander (Greek key) border, white and Aegean blue |
| Italy | `italy` | Star | #6B1F2A | #7A2E1F / #2E6B3F | five-pointed star in a laurel ring, Roman civic arches, terracotta and olive green |
| China | `china` | FiveStars | #8A1C1C | #A31F1F / #D4AF37 | five gold stars, cloud-scroll border, vermilion and gold |
| Japan | `japan` | Chrysanthemum | #4B2C5E | #1F2F4A / #B23A1A | sixteen-petal chrysanthemum, seigaiha wave border, indigo and vermilion |
| Britain | `britain` | Crown | #1B4B54 | #1F2A44 / #00563F | crown over a plain shield, Victorian railway-livery lining, navy and racing green |
| Germany | `germany` | Eagle | #5A3A1E | #2B2B2B / #E0B000 | plain heraldic eagle, Bauhaus bar border, charcoal and signal yellow |

Neutral (no culture leads, days before the first leader and whenever none does) needs no file: the hall stays as painted.

## The hall's variables

| Variable | Values | Read from |
|---|---|---|
| `culture` | neutral, or one of the eight nations | the timeline's leader (the same culture the PC theme and the Translation Lens follow) |
| `tier` | steady < strained < breaching < collapsing | the Helix River's own thresholds (oxbows from calm 0.75 = stability 90; the glitch from the warning line 70; the flicker in the critical band 63 and under) |
| `phase` | normal (days 1-7) < extended (8-11) < nights (12-15) | the day (the shift hours' ramp) |
| `event` | none, recall (day 11), ban (day 12), return (days 14-15) | the day plan's rules |
| `exhibit` / `recent` | the nation with the most famous travellers let through / of the latest one; none | the run's verdicts on the famous travellers |

## Inventory: every registered layer, and what varies

The 58 registered layers (`layers.json`) and their role here. "Slot" = varied by this request; "no" = stays as painted (with the reason).

| Layer | File | Visible bounds (v8) | Slot? |
|---|---|---|---|
| 00 Structural masonry | `00-structural-masonry.png` | 0,136 - 2172,724 | no: structure |
| 01 Ceiling and overhead services | `01-ceiling-and-overhead-services.png` | 694,0 - 2172,136 | no: structure; the ceiling band glows at night |
| 02 Window frames | `02-window-frames.png` | 0,0 - 1348,572 | no: window frames (glass is cut out) |
| 03 Exterior placeholder single layer | `03-exterior-placeholder-single-layer.png` | 0,0 - 1322,571 | no: the living city (its own system: time, weather) |
| 04 Upper right service walls | `04-upper-right-service-walls.png` | 1475,88 - 2172,262 | no: structure |
| 05 Lower right department walls | `05-lower-right-department-walls.png` | 1341,307 - 2172,723 | no: wall (the new wall poster sits on it) |
| 06 Concourse terracotta floor | `06-concourse-terracotta-floor.png` | 4,336 - 2169,724 | **slot `06-concourse-terracotta-floor`** (8 variants) |
| 07 Upper gallery floor | `07-upper-gallery-floor.png` | 1475,262 - 2172,300 | no: structure |
| 08 Bridge fascia | `08-bridge-fascia.png` | 533,264 - 1349,308 | **slot `08-bridge-fascia`** (2 variants) |
| 09 Left elevator pier | `09-left-elevator-pier.png` | 550,0 - 694,418 | no: structure (shadow caster) |
| 10 Right load bearing pier | `10-right-load-bearing-pier.png` | 1333,0 - 1475,472 | no: structure (shadow caster) |
| 11 Left stair structure | `11-left-stair-structure.png` | 411,404 - 688,617 | no: structure |
| 12 Foreground platform | `12-foreground-platform.png` | 0,676 - 1655,724 | no: foreground floor (its own registered system) |
| 13 Flag left cloth | `13-flag-left-cloth.png` | 722,0 - 784,198 | **slot `13-flag-left-cloth`** (8 variants) |
| 14 Flag right cloth | `14-flag-right-cloth.png` | 1299,0 - 1355,197 | **slot `14-flag-right-cloth`** (8 variants) |
| 15 Departure board frame | `15-departure-board-frame.png` | 842,12 - 1152,183 | **slot `15-departure-board-frame`** (8 variants) |
| 16 Departure board blank display | `16-departure-board-blank-display.png` | 853,74 - 1142,173 | no: the board's display: the game draws the departure rows on it |
| 17 Detention door | `17-detention-door.png` | 1391,400 - 1453,516 | no: door |
| 18 Medbay door | `18-medbay-door.png` | 1482,440 - 1558,551 | no: door |
| 19 Executive suites door | `19-executive-suites-door.png` | 1595,486 - 1683,607 | no: door |
| 20 Detention pictogram | `20-detention-pictogram.png` | 1387,351 - 1458,414 | **slot `20-detention-pictogram`** (8 variants) |
| 21 Medbay pictogram | `21-medbay-pictogram.png` | 1478,374 - 1561,456 | **slot `21-medbay-pictogram`** (8 variants) |
| 22 Executive suites pictogram | `22-executive-suites-pictogram.png` | 1592,402 - 1685,504 | **slot `22-executive-suites-pictogram`** (8 variants) |
| 23 Lower elevator doors | `23-lower-elevator-doors.png` | 579,303 - 648,427 | no: elevator |
| 24 Upper side elevator door | `24-upper-side-elevator-door.png` | 671,157 - 698,264 | no: elevator |
| 25 Left parcel lockers | `25-left-parcel-lockers.png` | 0,443 - 475,687 | no: lockers (the queue clutter stands in front of them) |
| 26 Rear baggage scanner | `26-rear-baggage-scanner.png` | 722,314 - 821,403 | no: scanner (the anomalies corner is beside it) |
| 27 Rear waiting seat | `27-rear-waiting-seat.png` | 951,328 - 1056,361 | no: removed in the repaint (inactive) |
| 28 Rear water dispenser | `28-rear-water-dispenser.png` | 1057,316 - 1072,353 | no: removed in the repaint (inactive) |
| 29 Rear mixed parcel cabinets | `29-rear-mixed-parcel-cabinets.png` | 1214,308 - 1280,367 | no: cargo (the anomalies overlay covers this corner) |
| 30 Rear maintenance rover | `30-rear-maintenance-rover.png` | 1275,310 - 1331,382 | no: rover |
| 31 Right vending machine | `31-right-vending-machine.png` | 1716,470 - 1812,633 | no: vending machine |
| 32 Right waiting seats | `32-right-waiting-seats.png` | 1770,544 - 1940,674 | no: seats |
| 33 Right waste bin | `33-right-waste-bin.png` | 1921,586 - 1969,679 | no: bin |
| 34 Left waste bin | `34-left-waste-bin.png` | 592,546 - 633,614 | no: bin |
| 35 Framed historical artwork | `35-framed-historical-artwork.png` | 1863,414 - 1932,522 | **slot `35-framed-historical-artwork`** (8 variants) |
| 36 Armillary display | `36-armillary-display.png` | 1954,455 - 2012,533 | **slot `36-armillary-display`** (8 variants) |
| 37 Stone bust display | `37-stone-bust-display.png` | 2057,457 - 2124,555 | no: cut by the canvas edge (half visible) |
| 38 Portal 01 front secure bay | `38-portal-01-front-secure-bay.png` | 804,475 - 1238,639 | no: portal bay (the portal effects and the glass cracks overlay) |
| 39 Portal 01 front metal ring | `39-portal-01-front-metal-ring.png` | 938,405 - 1117,586 | no: portal ring (the game's portal glow) |
| 40 Portal 02 rear left secure bay | `40-portal-02-rear-left-secure-bay.png` | 801,362 - 985,430 | no: portal bay |
| 41 Portal 02 rear left metal ring | `41-portal-02-rear-left-metal-ring.png` | 863,328 - 946,407 | no: portal ring |
| 42 Portal 03 rear right secure bay | `42-portal-03-rear-right-secure-bay.png` | 1065,363 - 1242,432 | no: portal bay |
| 43 Portal 03 rear right metal ring | `43-portal-03-rear-right-metal-ring.png` | 1098,329 - 1181,406 | no: portal ring |
| 44 Portal 04 upper left secure bay | `44-portal-04-upper-left-secure-bay.png` | 1563,202 - 1789,267 | no: upper portal bay |
| 45 Portal 04 upper left metal ring | `45-portal-04-upper-left-metal-ring.png` | 1647,153 - 1754,264 | no: upper portal ring |
| 46 Portal 05 upper right secure bay | `46-portal-05-upper-right-secure-bay.png` | 1798,202 - 2082,269 | no: upper portal bay |
| 47 Portal 05 upper right metal ring | `47-portal-05-upper-right-metal-ring.png` | 1877,135 - 2022,271 | no: upper portal ring |
| 48 Foreground tall platform railing | `48-foreground-tall-platform-railing.png` | 0,531 - 2172,724 | no: railing (in front of everything: leave it out of your art) |
| 49 Bridge and upper gallery railing | `49-bridge-and-upper-gallery-railing.png` | 530,243 - 2172,282 | no: railing |
| 50 Left observation and stair railing | `50-left-observation-and-stair-railing.png` | 0,369 - 683,595 | no: railing |
| 51 Fixture diffusers | `51-fixture-diffusers.png` | 783,0 - 1995,169 | no: fixtures (the lighting system) |
| 52 Fire safety cabinets | `52-fire-safety-cabinets.png` | 542,185 - 1478,441 | no: fire cabinets |
| 53 Portal 01 front painted glass | `53-portal-01-front-painted-glass.png` | 806,475 - 1230,639 | no: portal glass (the glass cracks overlay) |
| 54 Portal 02 rear left painted glass | `54-portal-02-rear-left-painted-glass.png` | 804,366 - 983,430 | no: portal glass |
| 55 Portal 03 rear right painted glass | `55-portal-03-rear-right-painted-glass.png` | 1068,366 - 1235,427 | no: portal glass |
| 56 Portal 04 upper left painted glass | `56-portal-04-upper-left-painted-glass.png` | 1574,203 - 1788,263 | no: upper portal glass |
| 57 Portal 05 upper right painted glass | `57-portal-05-upper-right-painted-glass.png` | 1802,202 - 2074,267 | no: upper portal glass |

New overlay layers (no registered layer shows these things; each is a 2172 x 724 canvas like the layers, drawn over the painting): `new-wall-poster`, `new-anomalies`, `new-glass-cracks`, `new-checkpoint`, `new-queue-props`.

## The slots, in priority order

### Priority 1: Culture: banners, signs, the board plate, the floor medallion (56 files)

Why first: the most visible change: who leads history repaints the hall.

#### `13-flag-left-cloth`

- What it is today: the left red hanging banner of the hall's ceiling (painted red cloth, no emblem).
- Slot: varies the registered layer `13 Flag left cloth` (old file `Textures/13-flag-left-cloth.png`, for reference only).
- Paintable box: **70 x 204 px at canvas (786, 0)** (top-left origin) on the 2172 x 724 canvas; template `templates/13-flag-left-cloth.png`.
- Draw order: over the painting (58) and the portal glows (59), under the crowds (61-62) and the gallery fixtures (99): sorting order 60.
- With no variant: the painting as it is.

| File | Shows when | Priority | What to paint |
|---|---|---|---|
| `13-flag-left-cloth/egypt.png` | `culture=egypt` | 10 | The left hanging banner in the leading culture's colours with its emblem centred in the upper third for Egypt: winged sun disc, lotus and papyrus borders, lapis blue and gold; emblem WingedSun, colours #1C4E80 / #C8962E |
| `13-flag-left-cloth/iraq.png` | `culture=iraq` | 10 | The left hanging banner in the leading culture's colours with its emblem centred in the upper third for Iraq: eight-pointed star of Ishtar, glazed-brick blue with gold lions-gate stepped border; emblem Octastar, colours #1B4F72 / #D4A017 |
| `13-flag-left-cloth/greece.png` | `culture=greece` | 10 | The left hanging banner in the leading culture's colours with its emblem centred in the upper third for Greece: laurel wreath, meander (Greek key) border, white and Aegean blue; emblem Laurel, colours #0D5EAF / #1F6F8B |
| `13-flag-left-cloth/italy.png` | `culture=italy` | 10 | The left hanging banner in the leading culture's colours with its emblem centred in the upper third for Italy: five-pointed star in a laurel ring, Roman civic arches, terracotta and olive green; emblem Star, colours #7A2E1F / #2E6B3F |
| `13-flag-left-cloth/china.png` | `culture=china` | 10 | The left hanging banner in the leading culture's colours with its emblem centred in the upper third for China: five gold stars, cloud-scroll border, vermilion and gold; emblem FiveStars, colours #A31F1F / #D4AF37 |
| `13-flag-left-cloth/japan.png` | `culture=japan` | 10 | The left hanging banner in the leading culture's colours with its emblem centred in the upper third for Japan: sixteen-petal chrysanthemum, seigaiha wave border, indigo and vermilion; emblem Chrysanthemum, colours #1F2F4A / #B23A1A |
| `13-flag-left-cloth/britain.png` | `culture=britain` | 10 | The left hanging banner in the leading culture's colours with its emblem centred in the upper third for Britain: crown over a plain shield, Victorian railway-livery lining, navy and racing green; emblem Crown, colours #1F2A44 / #00563F |
| `13-flag-left-cloth/germany.png` | `culture=germany` | 10 | The left hanging banner in the leading culture's colours with its emblem centred in the upper third for Germany: plain heraldic eagle, Bauhaus bar border, charcoal and signal yellow; emblem Eagle, colours #2B2B2B / #E0B000 |

#### `14-flag-right-cloth`

- What it is today: the right red hanging banner.
- Slot: varies the registered layer `14 Flag right cloth` (old file `Textures/14-flag-right-cloth.png`, for reference only).
- Paintable box: **70 x 200 px at canvas (1360, 0)** (top-left origin) on the 2172 x 724 canvas; template `templates/14-flag-right-cloth.png`.
- Draw order: over the painting (58) and the portal glows (59), under the crowds (61-62) and the gallery fixtures (99): sorting order 60.
- With no variant: the painting as it is.

| File | Shows when | Priority | What to paint |
|---|---|---|---|
| `14-flag-right-cloth/egypt.png` | `culture=egypt` | 10 | The right hanging banner, the twin of the left one (same design, mirrored folds) for Egypt: winged sun disc, lotus and papyrus borders, lapis blue and gold; emblem WingedSun, colours #1C4E80 / #C8962E |
| `14-flag-right-cloth/iraq.png` | `culture=iraq` | 10 | The right hanging banner, the twin of the left one (same design, mirrored folds) for Iraq: eight-pointed star of Ishtar, glazed-brick blue with gold lions-gate stepped border; emblem Octastar, colours #1B4F72 / #D4A017 |
| `14-flag-right-cloth/greece.png` | `culture=greece` | 10 | The right hanging banner, the twin of the left one (same design, mirrored folds) for Greece: laurel wreath, meander (Greek key) border, white and Aegean blue; emblem Laurel, colours #0D5EAF / #1F6F8B |
| `14-flag-right-cloth/italy.png` | `culture=italy` | 10 | The right hanging banner, the twin of the left one (same design, mirrored folds) for Italy: five-pointed star in a laurel ring, Roman civic arches, terracotta and olive green; emblem Star, colours #7A2E1F / #2E6B3F |
| `14-flag-right-cloth/china.png` | `culture=china` | 10 | The right hanging banner, the twin of the left one (same design, mirrored folds) for China: five gold stars, cloud-scroll border, vermilion and gold; emblem FiveStars, colours #A31F1F / #D4AF37 |
| `14-flag-right-cloth/japan.png` | `culture=japan` | 10 | The right hanging banner, the twin of the left one (same design, mirrored folds) for Japan: sixteen-petal chrysanthemum, seigaiha wave border, indigo and vermilion; emblem Chrysanthemum, colours #1F2F4A / #B23A1A |
| `14-flag-right-cloth/britain.png` | `culture=britain` | 10 | The right hanging banner, the twin of the left one (same design, mirrored folds) for Britain: crown over a plain shield, Victorian railway-livery lining, navy and racing green; emblem Crown, colours #1F2A44 / #00563F |
| `14-flag-right-cloth/germany.png` | `culture=germany` | 10 | The right hanging banner, the twin of the left one (same design, mirrored folds) for Germany: plain heraldic eagle, Bauhaus bar border, charcoal and signal yellow; emblem Eagle, colours #2B2B2B / #E0B000 |

#### `15-departure-board-frame`

- What it is today: the band above the departure board between its two hanging rods (empty today): a header plate hangs here.
- Slot: varies the registered layer `15 Departure board frame` (old file `Textures/15-departure-board-frame.png`, for reference only).
- Paintable box: **226 x 40 px at canvas (950, 36)** (top-left origin) on the 2172 x 724 canvas; template `templates/15-departure-board-frame.png`.
- Draw order: over the painting (58) and the portal glows (59), under the crowds (61-62) and the gallery fixtures (99): sorting order 60.
- With no variant: the painting as it is.

| File | Shows when | Priority | What to paint |
|---|---|---|---|
| `15-departure-board-frame/egypt.png` | `culture=egypt` | 10 | A short header plate hung between the board's two rods, the leading culture's emblem on a plate in its colours (no letters) for Egypt: winged sun disc, lotus and papyrus borders, lapis blue and gold; emblem WingedSun, colours #1C4E80 / #C8962E |
| `15-departure-board-frame/iraq.png` | `culture=iraq` | 10 | A short header plate hung between the board's two rods, the leading culture's emblem on a plate in its colours (no letters) for Iraq: eight-pointed star of Ishtar, glazed-brick blue with gold lions-gate stepped border; emblem Octastar, colours #1B4F72 / #D4A017 |
| `15-departure-board-frame/greece.png` | `culture=greece` | 10 | A short header plate hung between the board's two rods, the leading culture's emblem on a plate in its colours (no letters) for Greece: laurel wreath, meander (Greek key) border, white and Aegean blue; emblem Laurel, colours #0D5EAF / #1F6F8B |
| `15-departure-board-frame/italy.png` | `culture=italy` | 10 | A short header plate hung between the board's two rods, the leading culture's emblem on a plate in its colours (no letters) for Italy: five-pointed star in a laurel ring, Roman civic arches, terracotta and olive green; emblem Star, colours #7A2E1F / #2E6B3F |
| `15-departure-board-frame/china.png` | `culture=china` | 10 | A short header plate hung between the board's two rods, the leading culture's emblem on a plate in its colours (no letters) for China: five gold stars, cloud-scroll border, vermilion and gold; emblem FiveStars, colours #A31F1F / #D4AF37 |
| `15-departure-board-frame/japan.png` | `culture=japan` | 10 | A short header plate hung between the board's two rods, the leading culture's emblem on a plate in its colours (no letters) for Japan: sixteen-petal chrysanthemum, seigaiha wave border, indigo and vermilion; emblem Chrysanthemum, colours #1F2F4A / #B23A1A |
| `15-departure-board-frame/britain.png` | `culture=britain` | 10 | A short header plate hung between the board's two rods, the leading culture's emblem on a plate in its colours (no letters) for Britain: crown over a plain shield, Victorian railway-livery lining, navy and racing green; emblem Crown, colours #1F2A44 / #00563F |
| `15-departure-board-frame/germany.png` | `culture=germany` | 10 | A short header plate hung between the board's two rods, the leading culture's emblem on a plate in its colours (no letters) for Germany: plain heraldic eagle, Bauhaus bar border, charcoal and signal yellow; emblem Eagle, colours #2B2B2B / #E0B000 |

#### `20-detention-pictogram`

- What it is today: the Detention door's window sign (navy prison-bars pictogram on white).
- Slot: varies the registered layer `20 Detention pictogram` (old file `Textures/20-detention-pictogram.png`, for reference only).
- Paintable box: **76 x 66 px at canvas (1448, 345)** (top-left origin) on the 2172 x 724 canvas; template `templates/20-detention-pictogram.png`.
- Draw order: over the painting (58) and the portal glows (59), under the crowds (61-62) and the gallery fixtures (99): sorting order 60.
- With no variant: the painting as it is.

| File | Shows when | Priority | What to paint |
|---|---|---|---|
| `20-detention-pictogram/egypt.png` | `culture=egypt` | 10 | The Detention sign restyled in the culture's sign language: same bars-and-lock idea, its colours and border motif for Egypt: winged sun disc, lotus and papyrus borders, lapis blue and gold; emblem WingedSun, colours #1C4E80 / #C8962E |
| `20-detention-pictogram/iraq.png` | `culture=iraq` | 10 | The Detention sign restyled in the culture's sign language: same bars-and-lock idea, its colours and border motif for Iraq: eight-pointed star of Ishtar, glazed-brick blue with gold lions-gate stepped border; emblem Octastar, colours #1B4F72 / #D4A017 |
| `20-detention-pictogram/greece.png` | `culture=greece` | 10 | The Detention sign restyled in the culture's sign language: same bars-and-lock idea, its colours and border motif for Greece: laurel wreath, meander (Greek key) border, white and Aegean blue; emblem Laurel, colours #0D5EAF / #1F6F8B |
| `20-detention-pictogram/italy.png` | `culture=italy` | 10 | The Detention sign restyled in the culture's sign language: same bars-and-lock idea, its colours and border motif for Italy: five-pointed star in a laurel ring, Roman civic arches, terracotta and olive green; emblem Star, colours #7A2E1F / #2E6B3F |
| `20-detention-pictogram/china.png` | `culture=china` | 10 | The Detention sign restyled in the culture's sign language: same bars-and-lock idea, its colours and border motif for China: five gold stars, cloud-scroll border, vermilion and gold; emblem FiveStars, colours #A31F1F / #D4AF37 |
| `20-detention-pictogram/japan.png` | `culture=japan` | 10 | The Detention sign restyled in the culture's sign language: same bars-and-lock idea, its colours and border motif for Japan: sixteen-petal chrysanthemum, seigaiha wave border, indigo and vermilion; emblem Chrysanthemum, colours #1F2F4A / #B23A1A |
| `20-detention-pictogram/britain.png` | `culture=britain` | 10 | The Detention sign restyled in the culture's sign language: same bars-and-lock idea, its colours and border motif for Britain: crown over a plain shield, Victorian railway-livery lining, navy and racing green; emblem Crown, colours #1F2A44 / #00563F |
| `20-detention-pictogram/germany.png` | `culture=germany` | 10 | The Detention sign restyled in the culture's sign language: same bars-and-lock idea, its colours and border motif for Germany: plain heraldic eagle, Bauhaus bar border, charcoal and signal yellow; emblem Eagle, colours #2B2B2B / #E0B000 |

#### `21-medbay-pictogram`

- What it is today: the Medbay door's window sign (red heart with a pulse line on white).
- Slot: varies the registered layer `21 Medbay pictogram` (old file `Textures/21-medbay-pictogram.png`, for reference only).
- Paintable box: **88 x 77 px at canvas (1538, 359)** (top-left origin) on the 2172 x 724 canvas; template `templates/21-medbay-pictogram.png`.
- Draw order: over the painting (58) and the portal glows (59), under the crowds (61-62) and the gallery fixtures (99): sorting order 60.
- With no variant: the painting as it is.

| File | Shows when | Priority | What to paint |
|---|---|---|---|
| `21-medbay-pictogram/egypt.png` | `culture=egypt` | 10 | The Medbay sign restyled in the culture's sign language: same heart-and-pulse idea, its colours and border motif for Egypt: winged sun disc, lotus and papyrus borders, lapis blue and gold; emblem WingedSun, colours #1C4E80 / #C8962E |
| `21-medbay-pictogram/iraq.png` | `culture=iraq` | 10 | The Medbay sign restyled in the culture's sign language: same heart-and-pulse idea, its colours and border motif for Iraq: eight-pointed star of Ishtar, glazed-brick blue with gold lions-gate stepped border; emblem Octastar, colours #1B4F72 / #D4A017 |
| `21-medbay-pictogram/greece.png` | `culture=greece` | 10 | The Medbay sign restyled in the culture's sign language: same heart-and-pulse idea, its colours and border motif for Greece: laurel wreath, meander (Greek key) border, white and Aegean blue; emblem Laurel, colours #0D5EAF / #1F6F8B |
| `21-medbay-pictogram/italy.png` | `culture=italy` | 10 | The Medbay sign restyled in the culture's sign language: same heart-and-pulse idea, its colours and border motif for Italy: five-pointed star in a laurel ring, Roman civic arches, terracotta and olive green; emblem Star, colours #7A2E1F / #2E6B3F |
| `21-medbay-pictogram/china.png` | `culture=china` | 10 | The Medbay sign restyled in the culture's sign language: same heart-and-pulse idea, its colours and border motif for China: five gold stars, cloud-scroll border, vermilion and gold; emblem FiveStars, colours #A31F1F / #D4AF37 |
| `21-medbay-pictogram/japan.png` | `culture=japan` | 10 | The Medbay sign restyled in the culture's sign language: same heart-and-pulse idea, its colours and border motif for Japan: sixteen-petal chrysanthemum, seigaiha wave border, indigo and vermilion; emblem Chrysanthemum, colours #1F2F4A / #B23A1A |
| `21-medbay-pictogram/britain.png` | `culture=britain` | 10 | The Medbay sign restyled in the culture's sign language: same heart-and-pulse idea, its colours and border motif for Britain: crown over a plain shield, Victorian railway-livery lining, navy and racing green; emblem Crown, colours #1F2A44 / #00563F |
| `21-medbay-pictogram/germany.png` | `culture=germany` | 10 | The Medbay sign restyled in the culture's sign language: same heart-and-pulse idea, its colours and border motif for Germany: plain heraldic eagle, Bauhaus bar border, charcoal and signal yellow; emblem Eagle, colours #2B2B2B / #E0B000 |

#### `22-executive-suites-pictogram`

- What it is today: the Executive Suites door's window sign (navy office chair with a crown on white).
- Slot: varies the registered layer `22 Executive suites pictogram` (old file `Textures/22-executive-suites-pictogram.png`, for reference only).
- Paintable box: **94 x 74 px at canvas (1654, 377)** (top-left origin) on the 2172 x 724 canvas; template `templates/22-executive-suites-pictogram.png`.
- Draw order: over the painting (58) and the portal glows (59), under the crowds (61-62) and the gallery fixtures (99): sorting order 60.
- With no variant: the painting as it is.

| File | Shows when | Priority | What to paint |
|---|---|---|---|
| `22-executive-suites-pictogram/egypt.png` | `culture=egypt` | 10 | The Executive Suites sign restyled in the culture's sign language: same crowned-chair idea, its colours and border motif for Egypt: winged sun disc, lotus and papyrus borders, lapis blue and gold; emblem WingedSun, colours #1C4E80 / #C8962E |
| `22-executive-suites-pictogram/iraq.png` | `culture=iraq` | 10 | The Executive Suites sign restyled in the culture's sign language: same crowned-chair idea, its colours and border motif for Iraq: eight-pointed star of Ishtar, glazed-brick blue with gold lions-gate stepped border; emblem Octastar, colours #1B4F72 / #D4A017 |
| `22-executive-suites-pictogram/greece.png` | `culture=greece` | 10 | The Executive Suites sign restyled in the culture's sign language: same crowned-chair idea, its colours and border motif for Greece: laurel wreath, meander (Greek key) border, white and Aegean blue; emblem Laurel, colours #0D5EAF / #1F6F8B |
| `22-executive-suites-pictogram/italy.png` | `culture=italy` | 10 | The Executive Suites sign restyled in the culture's sign language: same crowned-chair idea, its colours and border motif for Italy: five-pointed star in a laurel ring, Roman civic arches, terracotta and olive green; emblem Star, colours #7A2E1F / #2E6B3F |
| `22-executive-suites-pictogram/china.png` | `culture=china` | 10 | The Executive Suites sign restyled in the culture's sign language: same crowned-chair idea, its colours and border motif for China: five gold stars, cloud-scroll border, vermilion and gold; emblem FiveStars, colours #A31F1F / #D4AF37 |
| `22-executive-suites-pictogram/japan.png` | `culture=japan` | 10 | The Executive Suites sign restyled in the culture's sign language: same crowned-chair idea, its colours and border motif for Japan: sixteen-petal chrysanthemum, seigaiha wave border, indigo and vermilion; emblem Chrysanthemum, colours #1F2F4A / #B23A1A |
| `22-executive-suites-pictogram/britain.png` | `culture=britain` | 10 | The Executive Suites sign restyled in the culture's sign language: same crowned-chair idea, its colours and border motif for Britain: crown over a plain shield, Victorian railway-livery lining, navy and racing green; emblem Crown, colours #1F2A44 / #00563F |
| `22-executive-suites-pictogram/germany.png` | `culture=germany` | 10 | The Executive Suites sign restyled in the culture's sign language: same crowned-chair idea, its colours and border motif for Germany: plain heraldic eagle, Bauhaus bar border, charcoal and signal yellow; emblem Eagle, colours #2B2B2B / #E0B000 |

#### `06-concourse-terracotta-floor`

- What it is today: the open floor between the two rear portal bays, crossed by the burgundy inlay (plain today): a floor medallion lies here.
- Slot: varies the registered layer `06 Concourse terracotta floor` (old file `Textures/06-concourse-terracotta-floor.png`, for reference only).
- Paintable box: **108 x 34 px at canvas (1036, 376)** (top-left origin) on the 2172 x 724 canvas; template `templates/06-concourse-terracotta-floor.png`.
- Draw order: over the painting (58) and the portal glows (59), under the crowds (61-62) and the gallery fixtures (99): sorting order 60.
- With no variant: the painting as it is.

| File | Shows when | Priority | What to paint |
|---|---|---|---|
| `06-concourse-terracotta-floor/egypt.png` | `culture=egypt` | 10 | A flat inlaid floor medallion in perspective (an ellipse about 100 x 30 px), the culture's emblem in stone inlay, worn and polished like the floor for Egypt: winged sun disc, lotus and papyrus borders, lapis blue and gold; emblem WingedSun, colours #1C4E80 / #C8962E |
| `06-concourse-terracotta-floor/iraq.png` | `culture=iraq` | 10 | A flat inlaid floor medallion in perspective (an ellipse about 100 x 30 px), the culture's emblem in stone inlay, worn and polished like the floor for Iraq: eight-pointed star of Ishtar, glazed-brick blue with gold lions-gate stepped border; emblem Octastar, colours #1B4F72 / #D4A017 |
| `06-concourse-terracotta-floor/greece.png` | `culture=greece` | 10 | A flat inlaid floor medallion in perspective (an ellipse about 100 x 30 px), the culture's emblem in stone inlay, worn and polished like the floor for Greece: laurel wreath, meander (Greek key) border, white and Aegean blue; emblem Laurel, colours #0D5EAF / #1F6F8B |
| `06-concourse-terracotta-floor/italy.png` | `culture=italy` | 10 | A flat inlaid floor medallion in perspective (an ellipse about 100 x 30 px), the culture's emblem in stone inlay, worn and polished like the floor for Italy: five-pointed star in a laurel ring, Roman civic arches, terracotta and olive green; emblem Star, colours #7A2E1F / #2E6B3F |
| `06-concourse-terracotta-floor/china.png` | `culture=china` | 10 | A flat inlaid floor medallion in perspective (an ellipse about 100 x 30 px), the culture's emblem in stone inlay, worn and polished like the floor for China: five gold stars, cloud-scroll border, vermilion and gold; emblem FiveStars, colours #A31F1F / #D4AF37 |
| `06-concourse-terracotta-floor/japan.png` | `culture=japan` | 10 | A flat inlaid floor medallion in perspective (an ellipse about 100 x 30 px), the culture's emblem in stone inlay, worn and polished like the floor for Japan: sixteen-petal chrysanthemum, seigaiha wave border, indigo and vermilion; emblem Chrysanthemum, colours #1F2F4A / #B23A1A |
| `06-concourse-terracotta-floor/britain.png` | `culture=britain` | 10 | A flat inlaid floor medallion in perspective (an ellipse about 100 x 30 px), the culture's emblem in stone inlay, worn and polished like the floor for Britain: crown over a plain shield, Victorian railway-livery lining, navy and racing green; emblem Crown, colours #1F2A44 / #00563F |
| `06-concourse-terracotta-floor/germany.png` | `culture=germany` | 10 | A flat inlaid floor medallion in perspective (an ellipse about 100 x 30 px), the culture's emblem in stone inlay, worn and polished like the floor for Germany: plain heraldic eagle, Bauhaus bar border, charcoal and signal yellow; emblem Eagle, colours #2B2B2B / #E0B000 |

### Priority 2: Stability: anomalies and cracked glass (6 files)

Why: the Helix River's tier made physical.

#### `new-anomalies`

- What it is today: the rear-right cargo corner and the floor right of the front portal (era-bleed objects appear here).
- Slot: a **new overlay layer** (nothing registered shows this).
- Paintable box: **184 x 246 px at canvas (1258, 300)** (top-left origin) on the 2172 x 724 canvas; template `templates/new-anomalies.png`.
- Draw order: over the painting (58) and the portal glows (59), under the crowds (61-62) and the gallery fixtures (99): sorting order 60.
- With no variant: the painting as it is.

| File | Shows when | Priority | What to paint |
|---|---|---|---|
| `new-anomalies/strained.png` | `tier=strained` | 10 | One or two small era-bleed objects that should not be here: an amphora among the cargo, a medieval lantern on the floor; faint cyan time-shimmer outline |
| `new-anomalies/breaching.png` | `tier=breaching` | 20 | More objects bleeding in: a Roman column drum, a samurai helmet on a crate, a gramophone; cyan shimmer, one object half-transparent |
| `new-anomalies/collapsing.png` | `tier=collapsing` | 30 | The corner overrun: a pharaoh statue fragment, a Victorian lamp-post, a tilted Bauhaus chair, small floating debris with cyan rifts |

#### `new-glass-cracks`

- What it is today: the blue glass fences of the three portal bays (front bay above the brass railing, both rear bays).
- Slot: a **new overlay layer** (nothing registered shows this).
- Paintable box: **442 x 190 px at canvas (864, 358)** (top-left origin) on the 2172 x 724 canvas; template `templates/new-glass-cracks.png`.
- Draw order: over the painting (58) and the portal glows (59), under the crowds (61-62) and the gallery fixtures (99): sorting order 60.
- With no variant: the painting as it is.

| File | Shows when | Priority | What to paint |
|---|---|---|---|
| `new-glass-cracks/strained.png` | `tier=strained` | 10 | A couple of small star chips on the front bay's glass |
| `new-glass-cracks/breaching.png` | `tier=breaching` | 20 | Long cracks across the front bay's glass and chips on both rear bays; tape patches on one pane |
| `new-glass-cracks/collapsing.png` | `tier=collapsing` | 30 | Shattered panes with spider-web cracks on every bay, a glowing cyan time rift along one crack |

### Priority 3: Phase posters and exhibits (37 files)

Why: the debt crisis hardens the propaganda; the famous travellers let through end up on the walls.

#### `new-wall-poster`

- What it is today: the blank grey wall panel between the vending machine and the framed artwork (right wall).
- Slot: a **new overlay layer** (nothing registered shows this).
- Paintable box: **62 x 100 px at canvas (1842, 414)** (top-left origin) on the 2172 x 724 canvas; template `templates/new-wall-poster.png`.
- Draw order: over the painting (58) and the portal glows (59), under the crowds (61-62) and the gallery fixtures (99): sorting order 60.
- Fallback (shows when no other variant does): `bureau`. This slot is blank in the painting, so its fallback is needed first.

| File | Shows when | Priority | What to paint |
|---|---|---|---|
| `new-wall-poster/bureau.png` | `(always: the fallback)` | 0 | A calm Bureau of Temporal Customs notice poster: the hourglass seal, a queue pictogram, hall palette (oxblood, slate, brass, bone) |
| `new-wall-poster/bureau_extended.png` | `phase=extended` | 11 | The Bureau's debt-relief propaganda: a smiling clerk pictogram, a coin falling into an hourglass, harder reds |
| `new-wall-poster/bureau_nights.png` | `phase=nights` | 12 | Night-shift propaganda: a clock past midnight, a stern eye, the coin chain; torn corner, pasted over the older poster |
| `new-wall-poster/egypt.png` | `culture=egypt` | 20 | A civic poster of the leading culture Egypt: winged sun disc, lotus and papyrus borders, lapis blue and gold; emblem WingedSun |
| `new-wall-poster/iraq.png` | `culture=iraq` | 20 | A civic poster of the leading culture Iraq: eight-pointed star of Ishtar, glazed-brick blue with gold lions-gate stepped border; emblem Octastar |
| `new-wall-poster/greece.png` | `culture=greece` | 20 | A civic poster of the leading culture Greece: laurel wreath, meander (Greek key) border, white and Aegean blue; emblem Laurel |
| `new-wall-poster/italy.png` | `culture=italy` | 20 | A civic poster of the leading culture Italy: five-pointed star in a laurel ring, Roman civic arches, terracotta and olive green; emblem Star |
| `new-wall-poster/china.png` | `culture=china` | 20 | A civic poster of the leading culture China: five gold stars, cloud-scroll border, vermilion and gold; emblem FiveStars |
| `new-wall-poster/japan.png` | `culture=japan` | 20 | A civic poster of the leading culture Japan: sixteen-petal chrysanthemum, seigaiha wave border, indigo and vermilion; emblem Chrysanthemum |
| `new-wall-poster/britain.png` | `culture=britain` | 20 | A civic poster of the leading culture Britain: crown over a plain shield, Victorian railway-livery lining, navy and racing green; emblem Crown |
| `new-wall-poster/germany.png` | `culture=germany` | 20 | A civic poster of the leading culture Germany: plain heraldic eagle, Bauhaus bar border, charcoal and signal yellow; emblem Eagle |
| `new-wall-poster/egypt_crisis.png` | `culture=egypt & phase>=extended` | 30 | Egypt's poster hardened by the debt crisis: the same design with a debt chain or a red stamp across it, pasted-over edges |
| `new-wall-poster/iraq_crisis.png` | `culture=iraq & phase>=extended` | 30 | Iraq's poster hardened by the debt crisis: the same design with a debt chain or a red stamp across it, pasted-over edges |
| `new-wall-poster/greece_crisis.png` | `culture=greece & phase>=extended` | 30 | Greece's poster hardened by the debt crisis: the same design with a debt chain or a red stamp across it, pasted-over edges |
| `new-wall-poster/italy_crisis.png` | `culture=italy & phase>=extended` | 30 | Italy's poster hardened by the debt crisis: the same design with a debt chain or a red stamp across it, pasted-over edges |
| `new-wall-poster/china_crisis.png` | `culture=china & phase>=extended` | 30 | China's poster hardened by the debt crisis: the same design with a debt chain or a red stamp across it, pasted-over edges |
| `new-wall-poster/japan_crisis.png` | `culture=japan & phase>=extended` | 30 | Japan's poster hardened by the debt crisis: the same design with a debt chain or a red stamp across it, pasted-over edges |
| `new-wall-poster/britain_crisis.png` | `culture=britain & phase>=extended` | 30 | Britain's poster hardened by the debt crisis: the same design with a debt chain or a red stamp across it, pasted-over edges |
| `new-wall-poster/germany_crisis.png` | `culture=germany & phase>=extended` | 30 | Germany's poster hardened by the debt crisis: the same design with a debt chain or a red stamp across it, pasted-over edges |

#### `08-bridge-fascia`

- What it is today: the dark fascia band under the bridge between its two small displays.
- Slot: varies the registered layer `08 Bridge fascia` (old file `Textures/08-bridge-fascia.png`, for reference only).
- Paintable box: **242 x 30 px at canvas (964, 270)** (top-left origin) on the 2172 x 724 canvas; template `templates/08-bridge-fascia.png`.
- Draw order: over the painting (58) and the portal glows (59), under the crowds (61-62) and the gallery fixtures (99): sorting order 60.
- With no variant: the painting as it is.

| File | Shows when | Priority | What to paint |
|---|---|---|---|
| `08-bridge-fascia/extended.png` | `phase=extended` | 10 | A long thin Bureau banner strip on the fascia: oxblood cloth with brass rings, a coin-and-hourglass icon repeated (no words) |
| `08-bridge-fascia/nights.png` | `phase=nights` | 20 | The strip replaced by a night-shift banner: slate with a moon-and-clock icon, a red warning band; slightly sagging |

#### `35-framed-historical-artwork`

- What it is today: the gold-framed blue artwork on the right wall (a white plane-like sculpture today).
- Slot: varies the registered layer `35 Framed historical artwork` (old file `Textures/35-framed-historical-artwork.png`, for reference only).
- Paintable box: **92 x 135 px at canvas (1918, 390)** (top-left origin) on the 2172 x 724 canvas; template `templates/35-framed-historical-artwork.png`.
- Draw order: over the painting (58) and the portal glows (59), under the crowds (61-62) and the gallery fixtures (99): sorting order 60.
- With no variant: the painting as it is.

| File | Shows when | Priority | What to paint |
|---|---|---|---|
| `35-framed-historical-artwork/egypt.png` | `recent=egypt` | 10 | The frame keeps its gold border; inside, a museum portrait-poster of Egypt's famous traveller let through most recently: a silhouette bust in Egypt's colours with WingedSun motif (no likeness, no text) |
| `35-framed-historical-artwork/iraq.png` | `recent=iraq` | 10 | The frame keeps its gold border; inside, a museum portrait-poster of Iraq's famous traveller let through most recently: a silhouette bust in Iraq's colours with Octastar motif (no likeness, no text) |
| `35-framed-historical-artwork/greece.png` | `recent=greece` | 10 | The frame keeps its gold border; inside, a museum portrait-poster of Greece's famous traveller let through most recently: a silhouette bust in Greece's colours with Laurel motif (no likeness, no text) |
| `35-framed-historical-artwork/italy.png` | `recent=italy` | 10 | The frame keeps its gold border; inside, a museum portrait-poster of Italy's famous traveller let through most recently: a silhouette bust in Italy's colours with Star motif (no likeness, no text) |
| `35-framed-historical-artwork/china.png` | `recent=china` | 10 | The frame keeps its gold border; inside, a museum portrait-poster of China's famous traveller let through most recently: a silhouette bust in China's colours with FiveStars motif (no likeness, no text) |
| `35-framed-historical-artwork/japan.png` | `recent=japan` | 10 | The frame keeps its gold border; inside, a museum portrait-poster of Japan's famous traveller let through most recently: a silhouette bust in Japan's colours with Chrysanthemum motif (no likeness, no text) |
| `35-framed-historical-artwork/britain.png` | `recent=britain` | 10 | The frame keeps its gold border; inside, a museum portrait-poster of Britain's famous traveller let through most recently: a silhouette bust in Britain's colours with Crown motif (no likeness, no text) |
| `35-framed-historical-artwork/germany.png` | `recent=germany` | 10 | The frame keeps its gold border; inside, a museum portrait-poster of Germany's famous traveller let through most recently: a silhouette bust in Germany's colours with Eagle motif (no likeness, no text) |

#### `36-armillary-display`

- What it is today: the armillary sphere on its stand in the gold alcove (right wall, far right).
- Slot: varies the registered layer `36 Armillary display` (old file `Textures/36-armillary-display.png`, for reference only).
- Paintable box: **74 x 96 px at canvas (2056, 440)** (top-left origin) on the 2172 x 724 canvas; template `templates/36-armillary-display.png`.
- Draw order: over the painting (58) and the portal glows (59), under the crowds (61-62) and the gallery fixtures (99): sorting order 60.
- With no variant: the painting as it is.

| File | Shows when | Priority | What to paint |
|---|---|---|---|
| `36-armillary-display/egypt.png` | `exhibit=egypt` | 10 | The alcove's exhibit replaced by an artefact of Egypt, the nation with the most famous travellers let through: winged sun disc, lotus and papyrus borders, lapis blue and gold; a museum object on the same stand |
| `36-armillary-display/iraq.png` | `exhibit=iraq` | 10 | The alcove's exhibit replaced by an artefact of Iraq, the nation with the most famous travellers let through: eight-pointed star of Ishtar, glazed-brick blue with gold lions-gate stepped border; a museum object on the same stand |
| `36-armillary-display/greece.png` | `exhibit=greece` | 10 | The alcove's exhibit replaced by an artefact of Greece, the nation with the most famous travellers let through: laurel wreath, meander (Greek key) border, white and Aegean blue; a museum object on the same stand |
| `36-armillary-display/italy.png` | `exhibit=italy` | 10 | The alcove's exhibit replaced by an artefact of Italy, the nation with the most famous travellers let through: five-pointed star in a laurel ring, Roman civic arches, terracotta and olive green; a museum object on the same stand |
| `36-armillary-display/china.png` | `exhibit=china` | 10 | The alcove's exhibit replaced by an artefact of China, the nation with the most famous travellers let through: five gold stars, cloud-scroll border, vermilion and gold; a museum object on the same stand |
| `36-armillary-display/japan.png` | `exhibit=japan` | 10 | The alcove's exhibit replaced by an artefact of Japan, the nation with the most famous travellers let through: sixteen-petal chrysanthemum, seigaiha wave border, indigo and vermilion; a museum object on the same stand |
| `36-armillary-display/britain.png` | `exhibit=britain` | 10 | The alcove's exhibit replaced by an artefact of Britain, the nation with the most famous travellers let through: crown over a plain shield, Victorian railway-livery lining, navy and racing green; a museum object on the same stand |
| `36-armillary-display/germany.png` | `exhibit=germany` | 10 | The alcove's exhibit replaced by an artefact of Germany, the nation with the most famous travellers let through: plain heraldic eagle, Bauhaus bar border, charcoal and signal yellow; a museum object on the same stand |

### Priority 4: Event checkpoints and queue clutter (5 files)

Why: today's recall, border closure or return, and the Debt Relief queue.

#### `new-checkpoint`

- What it is today: the open concourse floor between the stair foot and the front portal bay (left of centre).
- Slot: a **new overlay layer** (nothing registered shows this).
- Paintable box: **182 x 110 px at canvas (690, 436)** (top-left origin) on the 2172 x 724 canvas; template `templates/new-checkpoint.png`.
- Draw order: over the painting (58) and the portal glows (59), under the crowds (61-62) and the gallery fixtures (99): sorting order 60.
- With no variant: the painting as it is.

| File | Shows when | Priority | What to paint |
|---|---|---|---|
| `new-checkpoint/recall.png` | `event=recall` | 10 | A recall bin: a wheeled cage half full of confiscated Driftbox transponders with a striped RECALL-coloured barrier (no words, a crossed-out transponder icon) |
| `new-checkpoint/ban.png` | `event=ban` | 20 | A border-closure checkpoint: two striped barriers, a stanchion rope and one Bureau guard silhouette in cel shading standing at attention |
| `new-checkpoint/return.png` | `event=return` | 30 | A Return Gate queue lane: amber stanchions, a floor arrow, a small cart of returned luggage with old-era tags |

#### `new-queue-props`

- What it is today: the walkway in front of the left parcel lockers, above the front brass railing.
- Slot: a **new overlay layer** (nothing registered shows this).
- Paintable box: **306 x 96 px at canvas (296, 452)** (top-left origin) on the 2172 x 724 canvas; template `templates/new-queue-props.png`.
- Draw order: over the painting (58) and the portal glows (59), under the crowds (61-62) and the gallery fixtures (99): sorting order 60.
- With no variant: the painting as it is.

| File | Shows when | Priority | What to paint |
|---|---|---|---|
| `new-queue-props/extended.png` | `phase=extended` | 10 | Debt Relief queue clutter: a few bundles, a tool bag, a folded bedroll, one rope stanchion |
| `new-queue-props/nights.png` | `phase=nights` | 20 | The queue camps overnight: more bundles and tool bags, a thermos, blankets, a rope line along the lockers |

## Totals

| Priority | Files |
|---|---|
| 1. Culture: banners, signs, the board plate, the floor medallion | 56 |
| 2. Stability: anomalies and cracked glass | 6 |
| 3. Phase posters and exhibits | 37 |
| 4. Event checkpoints and queue clutter | 5 |
| **All** | **104** (15 slots: 9 on registered layers, 6 new overlays) |

Suggested order inside priority 1: the two banners (16 files), then the board plate (8), the three door signs (24), the floor medallion (8). A first useful drop is just `13-flag-left-cloth` and `14-flag-right-cloth` for two or three nations: they show on the first day a culture leads.

## How to check a delivery

- Drop the PNGs into `Assets/Art/UI/Resources/Hall/Slots/<slot>/`; no code or scene change is needed. In play mode, the cheat menu (F9 or the backtick key, Play tab, "More (other tracks)") has `Hall:` buttons that set each variable and cycle the combinations; `Tools > Terminal Art > Hall Slots > Capture Combinations` renders the representative combinations.
- Check: the object sits exactly on the painted one (toggle the cheat between neutral and your culture: nothing should jump), the railing and posts still read in front, the art darkens with the hall at night (Time cheat: Night), no pixel lands on the window glass.
