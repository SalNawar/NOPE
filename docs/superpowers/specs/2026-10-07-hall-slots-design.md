# The hall's swappable slots (Track H, 2026-10-07)

Saleh: "We have all these variables and layers - we need assets that you can easily swap as the variables
shift, to create unique combinations", then "the hall is split into layers at the moment for this reason" and
"I need the GPT document to start generating inner hall assets". The anime hall reacted only to the time of
day, the weather, the portals, the departure board and the crowds; now its banners, signs, posters, exhibits,
floor, glass and floor clutter follow the run.

## What the hall reacts to (`HallState`, `Assets/Scripts/Domain/HallSlots.cs`)

| Variable | Values | Rule |
|---|---|---|
| culture | neutral or a nation id | `CultureThemeService.ActiveCultureId`: the timeline leader's culture, the one the PC theme and the Translation Lens follow |
| tier | steady < strained < breaching < collapsing | `HelixRiver.Tier`: Collapsing in the critical band (the river flickers), Breaching from the warning line (its glitch starts), Strained when the calm falls under `HelixRiverKnobs.oxbowsFrom` (its oxbows pinch off: stability 90 by default), else Steady. One rule in the river's file, on the river's knobs and `GameConfigSO`'s margins, so the hall and the river always agree |
| phase | normal < extended < nights | `HallStates.PhaseOf(today's hours, the standard day)` from the day's desk hours (Track N's night shifts, `days[].shiftStart` / `shiftEnd`): normal when the day closes at the standard hour, nights at midnight, extended between |
| event | none, recall, ban, return | `HallStates.EventOf(today's rule types, yesterday's)`: return with a ReturnHome rule (days 14-15), else ban with a NationForbidden or NationEraForbidden rule (day 12), else recall on the first day of a TransponderRecall rule (day 11), else none. Read from the day plans, not from day numbers |
| exhibits | the famous travellers let through, by nation; the strongest, the latest | `HallStates.ExhibitsOf` over the run's flags in order (`premade:{id}:accepted`, the latest verdict wins), each premade counted for `LegendarySO.nation`; strongest first, a tie by the latest acceptance |

The state is read when an input changes (the day, the stability's hundredths, the flags' count, the culture,
the dev overrides' version), so a shift's verdicts move the tier live; a frame reads five values and
allocates nothing.

## Slots (`HallSlotsSO`, `Assets/Data/Config/HallSlots_Default.asset`)

An Inspector asset the art side edits (Generate World never writes it), named by `DeskConfigSO.hallSlots`.
Each slot: an id (its art folder: a registered layer's file stem, or `new-…`), the registered layer it varies
(empty for a new overlay), what it shows, its region on the 2172 x 724 source canvas, a sorting order, its
variants (an id = the file name, a condition, a priority, alternates, what to paint) and a fallback.

Conditions: clauses joined by `&`; `culture=egypt` / `culture!=neutral`, `tier=`, `tier>=`, `tier<=`,
`phase=`, `phase>=`, `phase<=`, `event=`, `exhibit=` (strongest), `recent=` (latest), `exhibit:egypt` (at
least one). A clause that does not parse never matches and is reported (`HallSlotPick.Problems`, run at load
and by `Tools > Terminal Art > Hall Slots > Validate`).

Pick (`HallSlotPick.Pick`): the highest priority whose condition holds, the first listed among equals; else
the fallback; else nothing (the painting as it is). A variant with N alternates shows `id_k`, k drawn once
per run from `Seeds.ForHallSlot(runSeed, "slot/variant")` (a new salted stream "HALL" with its distinctness
test): runs differ, a replay matches, no traveller's draw moves.

## The finding that shaped the slots

The hall's 58 registered layers (`Textures/`, `layers.json`) are **disabled** in the live scene. What the
player sees is one composite, `Completion/WarmStone/HallWarmStone.png` (layer `62 Approved deeper
architecture`, order 58, `NOPE/Hall Deep Layout`), a repaint whose objects moved 55-65 px from the old layer
cuts. So a slot cannot swap a layer's texture and be seen: every variant is drawn **over** the composite, on
the same 2172 x 724 canvas, registered pixel for pixel, painted against `HallWarmStone.png` (the old layer
names stay as the slots' names, as Saleh's layer split intended). Consequences written into the art request:
a variant covers the object it replaces and leaves out what stands in front of it (the railings), and nothing
can be drawn on the window glass (the material cuts it out).

## Runtime (`HallSlotsLink`)

Added by `OfficeSceneBinder.BindHall` beside the shift and portal links. Two sprite renderers per slot (the
crossfade's) under the painting's parent with the painting's transform, layer, sorting layer and material:
the hall's pan carries them and the 2D backdrop camera draws them in order. Each frame (after the art's
`HallBakedLighting`, execution order 210) the painting's lighting values (`_StateWeights`,
`_HallShadowRay`, `_LightingAmount`, ...) are copied onto them, only those the painting holds. Art loads by
name through `SlotArt` (`Assets/Art/UI/Resources/Hall/Slots/<slot>/<file>.png`, `ArtSlotImporter` imports it
as a sprite up to 4096 px) and is placed by its bounds over the whole canvas, so a half-size delivery still
registers. Crossfade 0.6 s; Reduced Motion cuts.

Missing art: a registered layer's slot keeps the painting; a new overlay's draws a stand-in in the editor and
development builds (`Debug.isDebugBuild`): a two-tone cel plate in the hall palette on the slot's region with
"slot / variant" on it (a world TextMeshPro on the Default layer, drawn by the office camera); release builds
draw nothing. The cheat "Hall: stand-ins" shows every slot's stand-in.

## Tools

- `Tools > Terminal Art > Hall Slots > Export Templates`: the templates of the art request.
- `Validate`: the asset's problems and the art delivered.
- `Capture Combinations`: in play, twelve representative combinations (`HallSlotsTools.Combos`) to
  `Logs/HallSlotsCombos`, every stand-in shown, the hour forced per combination.
- The cheat menu's `Hall:` buttons (`HallSlotsCheats`): step each variable, back to the run's, stand-ins.
- `tools/hall/fit_slot_art.py`: fits a flat, front-view design (GPT's image tool cannot paint a registered
  canvas) onto the painting: the banners' cloth (stretched onto the painted cloth, its folds kept) and the
  board's header plate (contained between the rods, standing on the board); its docstring is the contract.

## Delivered art (2026-10-07)

Both banners and the board's header plate for the eight cultures: GPT's flat designs
(`ArtDeliverables/TimeDesk/HallSlots/sources/banner|board-plate/`) fitted by the tool, 24 files in
`Assets/Art/UI/Resources/Hall/Slots/`. Slot art is imported with the high-quality compression (BC7 on
PC: the default block compression mottled the dark fields over the uncompressed painting), and never takes
the ceiling diffusers' night glow (the link points the material's fixture reference at black for the slots).

## Decisions taken on Saleh's behalf

1. The slots live in an Inspector asset, not in `world_source.json`: they are art registration data the art
   side edits, not content; the rules and the validator are Domain and tested.
2. Fifteen slots, all in the part of the hall the desk shows (the screen covers canvas x 165-2015 and the
   desk's corkboards hide x 1770 and right, and x 400 and left below y 370): 8 on registered layers (both
   banners, the board's header band, the three door signs, the floor between the rear bays, the bridge
   fascia), 7 new overlays (a wall poster on the dark left pier, a framed portrait on the right pillar, a
   vitrine on the upper gallery, anomalies in the rear-right cargo corner, cracks on the portal bays' glass, a
   checkpoint on the left concourse, the Debt Relief queue's clutter by the lockers). Dropped: the right
   wall's framed artwork, armillary and stone bust (behind the right corkboard: the first stand-in captures
   showed them hidden), the doors themselves, the lockers (the clutter stands before them).
3. The framed portrait follows the latest famous nation (`recent=`), the vitrine the strongest
   (`exhibit=`).
4. No painted words in any variant (the Translation Lens cannot read painted text).
5. All slots draw at order 60 (over the painting and the portal glows, under the crowds and the fixtures).
6. Neutral culture needs no file: the hall stays as painted until a culture leads.
