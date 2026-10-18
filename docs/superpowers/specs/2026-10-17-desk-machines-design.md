# Desk machines: the chip reader, the UV torch, and a desk object for every upgrade (2026-10-17)

**Saleh, 2026-10-17 (his words):**
- "use blender and canva to make other machiences the player can unlock. for example the chip reader should be an actual machine that reads the chip when you scan it. uv light should be an actual ub flashlight you can use to see the hidden watermarks and symbols"
- "every upgrade should have a visual representation on the desk"
- "and a satysfing interaction"

Standing rules that also bind this design: everything is diegetic, Papers, Please controls, no extra confirm steps, 80s/90s anime cel style.

Everything marked **Decision** is mine (track MC) and is written down so Saleh can overrule it. Nothing else here is a rule of his.

This spec replaces part of the desk-machine spec (`2026-10-07-desk-machine-design.md` §5) and of the scanner-app spec's §2.4: the chip read and the hidden layer move off the scanner.

## 0. Where things stand today (facts, not decisions)
- The cream scanner (Track BR) scans a paper dropped on it. The copy goes to the PC (Investigation > Papers), and the copy has PRINT / UV / CHIP tabs.
- The UV and CHIP data are still a stub (`StubScanLayers`):
  - every paper shows an intact watermark;
  - every chip stores exactly what is printed.
- So no case today has a fault that only UV or the chip could show. That makes the move safe: nothing a player needs is lost when the scanner stops showing UV and CHIP.
- The machines are only worth building together with the real hidden-layer and chip data (§3).

## 1. The two machines

### Chip reader
A chunky 80s card terminal in gunmetal and cream. It has:
- a card slot on the front;
- an RF pad on top for passports;
- a green VFD line;
- three LEDs: POWER, READ and FAULT;
- a coiled cable to the desk;
- a "TCB" Bureau plate.

**Use.** Drag a paper onto the reader:
- A card (the Transponder Card, an ID) goes into the slot.
- A passport booklet is laid on the pad.
- That is the whole action. The read starts by itself (drop and go, like the scanner).

**What it reads.** The read puts the paper's chip record into the case: the same data the CHIP tab shows today.
- The VFD scrolls the result: `CHIP OK`, or each mismatched field (`TRANSP 4471-K != PRINT`), or `NO CHIP`.
- The FAULT LED lights on a mismatch.
- **Decision (Papers, Please-style evidence):** a read logs nothing by itself.
  - Each mismatched line on the VFD is an inspectable box: in inspect mode, click the line, then the printed box, and the discrepancy is logged.
  - The PC's CHIP tab shows the same lines, glowing, and one click logs one.
- Then the paper ejects and slides back to where it was picked up, as after a scan.
- The clock keeps running. One paper at a time; a second dropped paper waits by the reader.

### UV torch
A handheld blacklight torch: a ribbed rubber grip, a violet lens with a chrome bezel, a thumb slide switch and a lanyard loop. It rests in a cradle at the desk's front edge.

**Use (drag, as Papers, Please moves everything):**
- Press on the torch: it lifts from the cradle and clicks on.
- Drag it over the papers: a violet cone (an ellipse on the desk) follows the pointer.
- Inside the cone only, a paper shows its hidden layer: the watermark, the ghost of an erased value, the seal's microprint, symbols.
- Release: the torch clicks off and springs back to its cradle.
- No toggle and no second step.
- **Decision:** while you hold the torch, you can't drag papers with that same press. Papers, Please has one hand too.

**How the reveal is drawn.** A light mask in the papers' shader:
- a global cone (its position, radius and softness) that the torch sets each frame;
- the papers' hidden-layer texture, drawn only where the mask is above 0.
- Outside the cone, papers look exactly as they do today.

**Finding a mark.**
- A fault mark that is at least 60 % inside the cone for 0.25 s counts as *seen*.
- It blinks once, with the typewriter-bell ding.
- In inspect mode, click a lit mark, then its field, to log it.
- The PC's UV tab then lists the marks you have seen on that paper.

**The cone in Domain.** Which marks the cone covers, and when one counts as *seen*, is a pure rule in Domain (`UvCone`), with tests. The shader only draws it.

### What the scanner keeps doing
The scanner keeps everything except the hidden layer and the chip:
- sending copies to the PC (PRINT);
- record lookup, the rules check, the cross-check, overlays and history;
- Auto-Feed and the Analysis flag.

**Decision:** the scan's UV tab shows only once the torch exists, and only the marks you have seen. The CHIP tab shows only once the reader exists, and only after a read. Before the machines exist, neither tab shows: their data is a stub now anyway. This is a change to FEATURES.

## 2. How they are unlocked
**Decision (recommended, option A):** the Bureau **issues** each machine on a ramp day, as it issued the scanner (day 5) and the Word lens (day 8).
- Each machine is a `days[].introduces` key: `tool:chip_reader` and `tool:uv_torch`.
- The day's bulletin and a GUIDE page name it, and a desk note sits by it on its first day.
- **UV torch: day 7.** The Stranding Waiver arrives that day. Genuine waivers and passports carry the Bureau watermark, and doctored values leave ghosts.
- **Chip reader: day 11.** The Driftbox 3 recall arrives that day. A relabelled card prints another model, but its chip says Driftbox 3.
- Both days are a JSON value, so Saleh can move them.

**Content follows the machines** (the lean in the brief):
- Torch-only and chip-only faults are never generated before their machine's day. Their gate is `Introductions.Has(day, …)`, never a day number in code.
- From the machine's day: at most one such fault per day for its first two days, then the day plan's own chance.
- These faults are forgeries, so DETAIN may be used for them (`Law.Breaks`).

**Option B (not recommended):** sell the machines in Orders. A player who skips the purchase would meet faults they cannot see. The only fix is to gate the content on ownership, which rewards not buying.

**Later, if Saleh wants:** Orders upgrades of each machine, as variants of their props:
- *Batch Reader*: reads every chipped paper in one drop;
- *Wide Lens*: a bigger cone.

## 3. What building them needs (step 3, after the OK)
- **The real `hidden` / `chip` data** that replaces `StubScanLayers`:
  - per-paper marks and chip records, from the case and its forgery;
  - generated on their own salted `Seeds` stream, so existing draws never shift;
  - the variants are a ghost value, a missing watermark, a missing microprint on a forged seal, and a chip mismatch.
- **Domain:**
  - `UvCone` (the cone's coverage and when a mark is *seen*);
  - `ChipReaderCycle` (Idle, Insert, Reading, Result, Eject, with its durations in `DeskConfigSO`);
  - the content gates.
- **Tests** for all of the Domain rules.
- **Placement:** the chip reader goes left of the mat on the chair side; the torch cradle goes at the front-left edge. Both go through new contract anchors (`Anchor_ChipReader`, `Anchor_UvTorch`) with default poses and the scanner's clearance rule, so no paper gets stuck behind them.
- **Overlap to note:**
  - fix/desk-1017 owns scanner snapping and paper drop sizing; the reader's drop target reuses that code, so it merges after it;
  - feat/docs-3d-models (the papers' meshes) must keep the hidden-layer UV channel the torch shader reads.

## 4. Every work upgrade gets a desk object (Saleh's rule) and a satisfying interaction (Saleh's rule)
All of the objects and interactions below are **Decisions**, for Saleh to pick from.

Sound names in `code` exist in `SOUND_LIST.md`. Those marked **new** need adding to the list; until the files exist, the game plays its generated placeholders.

Every object moves on Track J's springs. Reduced Motion turns the springs into snaps.

**Shared arrival (Decision):** on a delivery morning, a TCB supply crate sits on the desk.
- Click its tape: it tears, the flaps pop, and the item springs to its place.
- Untouched, it unpacks by itself when AVAILABLE is pressed, so it is never a required step.
- Sounds: **new** `crate_tear`, **new** `crate_pop`.

| Upgrade (id · how you get it) | Desk object · where | What changes on the desk | Interaction and feel: action · beats · sound · feedback | Effort |
|---|---|---|---|---|
| **Chip reader** (`tool:chip_reader` · issued day 11) | 80s card terminal · left of the mat | the machine appears | drop a card into the slot or a passport on the pad · the card clunks in, the VFD scrolls `READING....`, three ticks, the result scrolls, the card ejects with a spring · **new** `chip_insert`, **new** `chip_read` (VFD ticks), **new** `chip_ok` (two-tone beep), **new** `chip_fault` (low buzz, or `scanner_flag`), **new** `chip_eject` · a small camera nudge on the clunk; the READ LED amber, then green or red; the VFD lines | Blender model (made) |
| **UV torch** (`tool:uv_torch` · issued day 7) | blacklight torch in a cradle · front-left edge | torch and cradle appear | press to lift and switch on, drag over papers, release · the thumb switch clicks, the cone flickers on in about 80 ms, a slight flicker while held, a mark blinks when seen, the torch springs back to the cradle · **new** `uv_on` (switch click), **new** `uv_hum` (soft loop while on), `inspect_match` when a fault mark is seen, **new** `uv_off` · a violet cone light on the desk, the lens glowing; the cradle clacks on return | Blender model (made) |
| Scanner (`tool:scanner` · issued day 5) | the cream scanner · right of the mat | **already has a prop** | the scanner as today | — |
| Auto-Feed Scanner (`scanner_autofeed` · Orders 200) | a sheet-feeder tray with pinch rollers on the scanner's side | the tray bolts on while installed; while not installed, it sits in its crate under the desk's right edge | a handed-over paper zips through the rollers by itself; click the tray for a roller spin · the guides spring and the rollers whirr · **new** `adf_feed` (a roller zip), `scanner_scan` · the tray's green FEED pilot lights | variant of the scanner (add parts to `scanner_model.py`) |
| Analysis Scanner (`adv_scanner` · Orders 300, one install at a time) | an amber "ANALYSIS" beacon pod on the scanner's back corner, with an extra readout row | the pod is bolted on while installed, else crated | on a flagged pair the beacon pops up and spins; click it to push it down · `scanner_flag`, **new** `beacon_pop` · an amber rotating light on the desk; the readout reads `ANALYSED 1!` | variant of the scanner |
| Interview Protocols (`interview_protocols` · Orders 120) | laminated question cards on a ring, hung on the counter microphone's stem | the card ring appears; the birth-date card is on top | click to flick through the cards (one per cleared question); the card for the question just asked flips forward by itself · `ui_tab` (card flick) ×3 · a small swing of the ring | decal + a small model (the ring) |
| 4 Speech translators (`tr_*_spoken` · Orders 80 each, after Protocols) | a "BABEL" translator rack with four cartridge bays · on the counter, by the microphone | each pack is a coloured cartridge in its bay, with a region decal | the cartridge clunks into its bay on delivery; while a covered tongue is spoken, its LED blinks and the VU needle dances with the speech; click the rack for a test chirp and a needle swing · **new** `cartridge_in`, **new** `translator_chirp` · the cartridge LED and the needle | Blender model (rack) + 4 decal variants |
| Translation Lens: Word (`lens_word` · issued day 8) | a brass jeweller's loupe on a small stand by the monitor | the loupe appears | the lens stays a hover ability, with no new step; each word that flips glints the loupe's glass and ticks its ring one notch; click to spin it on its swivel · **new** `loupe_tick` · a star glint on the glass | Blender model |
| Translation Lens: Sentences (`lens_sentence` · Orders 60) | a bar magnifier (a line reader) beside the loupe | the bar joins the stand | as for the loupe; a sentence flip slides a highlight down the bar · `loupe_tick` · the bar's red reading line glows | variant of the loupe |
| Translation Lens: Objects (`lens_object` · Orders 120) | a full-page Fresnel sheet on a hinged arm | the arm folds out over the stand | an object's cascade sweeps a sheen across the sheet; click to fold or unfold the arm (it springs) · `loupe_tick`, **new** `arm_fold` · a sheen across the sheet | variant of the loupe |
| Portal 02, 04, 05, Return Gate repairs (`repair_*` · Orders) | a "RING SERVICE" relay panel with a pilot lamp and a breaker per ring · the counter's left end | **the hall already shows each repair** (the ring in service); on the panel, a repaired ring's lamp is lit and its module carries a SERVICED sticker | click a lit lamp: the relay clacks and that ring's glow in the hall pulses once; on an approval through a ring, its lamp flashes · **new** `relay_clack`, `portal_through` · the lamp, and the ring's pulse in the hall | Blender model (panel) + a decal per repair |
| Diplomatic Contacts (`diplo_contacts` · Orders 350, pay) | a Rolodex · beside the phone | the Rolodex appears, a gold-edged card on top | drag to spin it: the cards flick past with a ratchet and settle on a spring; it stops on a contact card (a lore line) · **new** `rolodex_flick` ×3 · the cards fan, and the gold card glints on payday | Blender model |
| Pet toys: ball, rope, feather wand, Squeaky Auditor (`toy_*` · Orders, Toys band) | the toy sits by the photo frame on the day it arrives, then goes Home | the toy appears for that day | click it: the ball bounces, the auditor squeaks, the feather sways, the rope swings · **new** `toy_squeak`, `prop_click` · a small hop | small Blender models · **overlap: fix/pets-1017 owns the toys at Home** |

**Home upgrades** (Home venue: food, flat, security, health, comfort) are not work upgrades. They show at Home, which is the Home and pets track's area. They are not in this table.

## 5. Other machines Saleh could unlock (candidates only; not built)
1. **Transponder tuner.** A dial box that reads the date a traveller's transponder is set to return them to. Saleh's idea: "transponder set to return them to the wrong date" (desk-machine spec §7). You turn the dial until the display locks, with a ratcheting click.
2. **Thumbprint pad.** A glass pad that slides out through the counter hatch. The traveller presses a thumb, and a print comes back to compare with the record. It catches false identity, a crime under `Law.Breaks`.
3. **Booth camera.** A Polaroid-style camera on an arm. It snaps the traveller and prints a photo to lay next to the passport photo: a physical overlay compare.
4. **Luggage scale and X-ray belt.** A declared weight against the measured one, for contraband, as in Papers, Please.

## 6. Art (made, step 2)
- **Models.** `tools/props/machines/chip_reader_model.py` and `uv_torch_model.py`, run with Blender 5.2 in background mode, using the shared `machine_kit.py`.
- **Textures.** `machine_textures.py` paints them procedurally with Pillow: the kit palette, BigShoulders, and a katakana line.
  - **Canva was not used.** Getting a Canva design into the repo means downloading an export, and that needs Saleh's say-so.
- **Output.** `Assets/Art/Office/Gameplay/Machines/`: `ChipReader.fbx`, `UvTorch.fbx` and their PNGs.
- **Both models are low-poly and UV-mapped:**
  - the chip reader is about 4.4k triangles, 0.23 by 0.23 m and 0.074 m tall;
  - the torch is 0.21 m long, and with its cradle about 3.7k triangles.
- **The prop contracts (object names) are in each script's header.** Unity's front is local -Z.
  - **Chip reader:** `Body`, `Pad`, `SlotFlap`, `Readout`, `LED_Power`, `LED_Read`, `LED_Fault`, `Keys`, `Badge`, `SlotLabel`, `Cable`, and the empties `CardSlot` and `PadCentre`.
  - **Torch:** the `UvTorch` root, at the grip, with the children `Switch` (10 mm of travel), `Lens` (where the cone starts), `Label` and `BeamFX`.
  - **Cradle:** `Cradle`, with its child `CradleLabel`.

## 7. Saleh's answers (2026-10-17, relayed by the orchestrator)
1. **Unlock:** option A. The Bureau issues the UV torch on day 7 and the chip reader on day 11. The days stay in the JSON.
2. **Build every desk object in §4 now:**
   - the supply crate;
   - the scanner's Auto-Feed tray and Analysis beacon;
   - the BABEL rack;
   - the loupe, the bar magnifier and the Fresnel arm;
   - the Rolodex;
   - the Interview Protocols card ring;
   - the RING SERVICE panel.

   The pet toys stay with the pets track: they are Home items now.
3. **The supply crate:** yes.
4. **The four candidate machines:** all four are picked, for design only (§8). None is built yet.

## 8. The four later machines (design only; not built)
Every line below is a **Decision**, except the picks themselves, which are Saleh's.

Each machine follows the same pattern as the two machines above:
- the Bureau issues it on the ramp day its rule arrives, as a `tool:*` key in `days[].introduces`, with a bulletin, a GUIDE page and a desk note on its first day;
- the faults only it can catch are generated only from that day, gated through `Introductions`;
- its use is one physical action, with no confirm step;
- what it finds is logged the Papers, Please way: in inspect mode, click its readout line, then the paper's box.

### 8.1 Transponder tuner
- **Saleh's idea:** "transponder set to return them to the wrong date".
- **Object:** a chunky dial box, slate and brass, with a big knurled knob and a split-flap date window. It sits at the counter's left end beside the relay panel.
- **Use:**
  - Drop the Transponder Card into its tray.
  - Turn the knob by dragging it in a circle. The flaps clatter through dates; the knob clicks every notch and gets heavier near the stored date.
  - When the flaps reach the card's return date, the knob locks with a thunk, a green lamp lights, and the window shows the date the transponder will actually return to.
- **Rule:** the return date must equal the Entry Ticket's departure plus the trip. A transponder set to another date is a forgery, so DETAIN may be used.
- **Unlock:** day 12, with bans. The new fault is "returns to the wrong date".
- **Feel:**
  - sounds: **new** `tuner_notch` ×3, **new** `tuner_lock`, and `board_flip` for the flaps;
  - a camera nudge on the lock.

### 8.2 Thumbprint pad
- **Object:** a cream glass pad on a drawer that slides out through the counter hatch toward the traveller. A print card ejects onto the counter.
- **Use:**
  - Click the hatch handle: the drawer slides out and the traveller presses a thumb (their arm animates).
  - The drawer slides back and a small print card comes out of the slot: the thumbprint plus its ridge code.
  - Lay the card next to the record's print on the PC, or drop it on the scanner for an overlay. A false identity shows as different ridges.
- **Rule:** the person is not the holder (a false identity), which is a crime under `Law.Breaks`, so DETAIN applies.
- **Unlock:** day 13. It also lets SwappedPhoto cases be proven by print, not only by eye.
- **Feel:**
  - sounds: **new** `hatch_slide`, **new** `pad_press` (a squeak on glass), **new** `card_eject`;
  - the pad's backlight glows while the thumb is on it.

### 8.3 Booth camera
- **Object:** a Polaroid-style camera on a jointed arm clamped to the counter edge, aimed at the traveller.
- **Use:**
  - Click the camera: a flash, a whirr, and a photo slides out.
  - The photo develops from blank to the image over about 2 s as it is dragged.
  - Lay it on the passport and the overlay compare shimmers wherever the faces differ. This is the physical twin of the PC's overlay compare (scanner-app spec §2.5).
- **Rule:** no new fault. It makes SwappedPhoto and false-identity cases provable at the desk.
- **Unlock:** day 9, with the clothes and the look menu, as the second look tool.
- **Feel:**
  - sounds: **new** `camera_flash`, **new** `photo_eject`;
  - a white flash over the booth, and a small camera nudge.

### 8.4 Luggage scale and X-ray belt
- **Object:** a floor scale on the traveller's side, whose dial shows on the counter, and a short X-ray belt with a green monitor at the desk's right end.
- **Use:**
  - Click the belt's switch: the bag rides through and the monitor scans line by line.
  - Contraband shows as a bright shape. The scale's needle settles on the measured weight, to compare with the declared weight on the Entry Ticket.
- **Rule:** contraband, the third crime under `Law.Breaks`, which the game doesn't have yet. It needs its own content: what each era forbids, and declared weights.
- **Unlock:** a later week. This is the largest of the four, because it adds a new content type.
- **Feel:**
  - sounds: **new** `belt_run` (a loop), **new** `xray_scan`, **new** `scale_settle` (a needle wobble);
  - the monitor's scanline sweeps.

## 9. As built (2026-10-17)
What landed on `feat/desk-machines`, and where the build departs from §1 and §4. Every departure is **my decision**, not Saleh's; each can be turned back.

### 9.1 The two machines
- **Chip reader** (`ChipReader`, `DeskHardware`): a paper is dropped on its pad (a passport or a card alike; no separate card slot). It slides in, READING ticks, then CHIP OK, NO CHIP or MISMATCH. Decision: the VFD is one line, so after MISMATCH it cycles each differing line ("CITIZEN ID QRT-301"). In inspect mode a click on the readout logs the shown line against its box.
- **UV torch** (`UvTorch`): Decision: the cradle sits on the mat's front-right corner, inside the reading view, not the front-left edge (from the left it was out of the reading view, and the held torch covered the papers). The torch is held as a right hand holds it, so its body never hides what its cone shows. A fault mark seen in the cone leaves a violet tick on its box; in inspect mode a click on the tick logs it.
- **Hidden lies join the day after their machine** (Decision): `WashedPaper` days 8-15, `ClonedChip` days 12-15. Generate World refuses a hidden lie listed before its machine (`HiddenLies.Problems`).
- **One-click logging** (Decision): a machine's reading is logged as a pair (the reading and its box) from the desk (tick, readout) or from the scanned copy's UV and CHIP tabs.

### 9.2 The upgrades' desk objects
All placed by `DeskConfigSO` knobs (a viewport point of the office view at 16:9 and a yaw each; `upgradeScale` 1.25). Built by Build Office UI from the props' models (`tools/props/machines/upgrade_props_model.py`); a model missing a part of its contract (`PropArt`) leaves its object out with a warning. What each shows is pure Domain (`UpgradeObjects`, tested).

| Object | Shown when | Where (Decision) | Interaction as built | Cues |
|---|---|---|---|---|
| Supply crate (`SupplyCrate`) | a morning with deliveries (`Orders.DeliveredOn`) | on the mat, right of the rulebook; it shows once the briefing is closed | click: the tape tears, the four flaps pop one by one, a tag with the delivered upgrades' names springs up; the day's new objects pop onto the desk and a new cartridge drops into the rack; the crate sinks away. Left shut, it opens itself when AVAILABLE is pressed | `crate_tear`, `crate_pop` |
| Auto-Feed tray (`ScannerAddOns`) | the Auto-Feed Scanner installed | on the scanner's left side (its own model, in the scanner's frame) | a paper it takes in zips through the rollers and blinks the FEED lamp; the rollers turn while a scan runs. Decision: no click, and no crate under the desk when it is owned but not installed (nothing shows) | `adf_feed` |
| Analysis beacon (`ScannerAddOns`) | the Analysis Scanner installed | on the scanner's back right corner | every analysis pass pops the pod: lit amber and spinning for 2.6 s when it marks a pair, half way and unlit when it marks none. Decision: no click to push it down, no extra readout row | `beacon_pop`, `scanner_flag` |
| Protocol card ring (`ProtocolRing`) | `interview_protocols` owned | Decision: on its own weighted stand at the desk's left, by the cash register (the art office has no microphone stem) | click: the four cards flick over the ring one after another; a new traveller flicks the front card | `ui_tab` |
| BABEL rack (`BabelRack`) | any Speech translator owned | behind the DETAIN button, between the cash register and the AVAILABLE sign | a cartridge per owned pack (packs' order); one delivered today drops into its bay when the crate opens; while a translated line goes up its lamp glows and the VU needle dances (`TravellerWheel.TongueTranslated`); click: test chirp, the needle swings to the red, the lamps blink | `cartridge_in`, `translator_chirp` |
| Lens stand (`LensKit`) | the Translation Lens's reach (`TranslationLens.Reach`) | the desk's front left, between the PC and the mat | the loupe (Word), the bar magnifier (Sentence), the Fresnel arm (Object); every word the lens turns ticks the loupe a notch (`TranslationLensPresenter.WordTurned`); click: the arm folds down over the desk or back up (without the arm, the loupe spins). Decision: no glint or sheen yet | `loupe_tick`, `arm_fold` |
| Rolodex (`Rolodex`) | `diplo_contacts` owned | right of the chip reader, behind the calculator | drag it up or down and the drum turns with the pointer, spins on and settles on a card; a click flicks it on a few cards. Decision: no lore line on the card yet | `rolodex_flick` |
| RING SERVICE panel (`RingPanel`) | any ring's repair owned | the desk's right, toward the portals | module n shows portal n: lamp lit while in service today, breaker up, a SERVICED sticker once its repair is owned; click a breaker: the relay clacks, the breaker snaps and its lamp blinks; a traveller going through a portal flashes its lamp (`GameManager.Departed`). Decision: no pulse of the ring in the hall | `relay_clack` |

The pet toys stay with the pets track (Saleh's answer 2).

### 9.3 Sounds
The cues above are `SoundCues` and rows 89-106 of `SOUND_LIST.md` (`ui_tab` and `scanner_flag` already existed). Until their files land, each plays a placeholder made in code (`CueSounds`).

### 9.4 Overlaps
- `fix/desk-1017` (scanner snapping, drops): `DeskController.FeedScanner` and `DeskScanner` gained one call each (the Auto-Feed's `Fed`, the beacon's `Flag`), and the builder's scanner art wires the add-ons.
- `fix/travellers-1017` (dialogue): `TravellerWheel.Speak` raises `TongueTranslated`.
- `feat/docs-3d-models` (papers): the UV layer is a child of each desk paper (`UvPaperLayer`), drawn over the paper's own quads.

## 10. Every object has a function (2026-10-18, track FUN)
**Saleh, demo 1018a (his words):**
- note 10: "when you hold the UV the mouse should be on the base not the light"
- note 14: "every object the playyer unlocks should be useable not just there like it should have a function"

The orchestrator's ideas are in the brief; every line below marked **Decision** is mine (track FUN), not Saleh's, and can be turned back. Each function is one diegetic action, with no confirm step. Where it needs data, the data is in `world_source.json` (Generate World), seeded, with Domain rules and tests.

| Object | Its function now | Domain (tested) |
|---|---|---|
| UV torch (note 10) | The pointer holds the **grip**, `uvHoldHeight` (0.14 m) over the desk; the lens points away from the chair `uvHoldTilt` (40°) down; the cone, and the reveal, land **ahead** of the pointer where the torch's axis meets the desk. | `UvCone.Ahead` |
| Protocol card ring | Its cards are the questions the traveller at the desk can be asked **now** (the ask menu's open questions, a label printed on each card). A **click asks the front card's question**, exactly as the wheel would, and the card swings over to the next; a **drag flicks** to the next without asking; nothing to ask: the ring only sways. **Decision:** the ring follows the wheel's unlocks (a question opens once its detail is logged), so it never asks what the wheel could not. | `ProtocolDeck`, `DialogRunner.Offered`, `ChooseFrom` |
| BABEL rack | While a traveller whose tongue is foreign today is at the desk, the **cartridge that covers their tongue keeps a pilot glow** (none glows: nothing understands them, so their answers prove nothing). A **click replays the traveller's last line** in the bubble, flipping again where a cartridge covers it. **Decision:** no cartridge swapping (it would make owned translators a chore). | `DialogLines.LastBy` |
| Lens stand | Each part is a **magnifier**: the loupe (round, 2.5×) and the bar (a strip, 1.8×) are held like the torch; the **Fresnel** (1.4×) magnifies hands-free while its arm is folded down (a click on the base). Their glass shows a second camera's view through it. They read the papers' **microprint**: a hairline along the foot of every paper of a microprint form (TC-101, TC-230, TC-310, TC-520) repeating the Citizen ID its office issued. Read (one repeat shown for a beat), it gets a tick; in inspect mode a click picks it against the Citizen ID box. **Decision:** a new hidden lie, **ReprintedPaper** (another citizen's genuine paper reprinted with the traveller's own true values; its microprint still repeats the first holder's ID), days 9-15, the day after the lens; a doctored Citizen ID shows there too (the issued value). **Decision:** the stand moves onto the mat's left (`lensKitAt`), inside the reading view, as the torch's cradle did; LAY owns its final place. | `MicroLens`, `HiddenLies.PlanReprint`, `MicroLine`, `DeskReadings.ReadMicro` |
| RING SERVICE panel | The desk's own **departure board**: each module's lamp shows its ring today (green open, red CLOSED today, amber no route, blue the Return Gate, dark under maintenance); a click on a breaker **reads the ring out** in the overlay's tooltip (the board's own line). **Decision:** no re-routing (a traveller's ring follows from the rules; a CLOSED destination is denied, never re-routed). | `UpgradeObjects.LampOf`, `PortalText.TooltipLine` |
| Auto-Feed tray | A **click on the tray feeds the stack in one go**: every paper not scanned yet this case, in hand-over order, each back where it lay. **Decision:** nothing feeds itself at the hand-over any more (the tray is the trigger; the upgrade's blurb says so). | `DeskPapers.FeedStack` |
| Analysis beacon | A **click jumps to the flagged pair**: the two papers of the pair the last analysis marked come to the top of the stack, both boxes tinted amber for the case. **Decision:** nothing is picked or logged (the PC redesign's SC5: the clerk still compares the two). | — |
| Rolodex | Each card is one of eight **Diplomatic Contacts** (`agency.contacts`: a name, a post, the faults they watch, a tip and a quiet line). The card the drum settles on reads out what that contact hears **today**: a tip naming the claimed place of a traveller carrying a fault they watch, or their quiet line; the gold card on top is the day's lead. **Decision:** a tip names a place, never a traveller; the draws are on the day's own stream (`Seeds.ForContacts`, "CNTC"). | `ContactCards` |
| Supply crate | Already functional (it brings the day's deliveries). | — |

Overlaps: LAY (NOPE-feat-clock) owns where the objects sit (this track moved only the lens stand onto the mat, by a knob); DOC2 (NOPE-t5) owns papers moving into machines (the Auto-Feed's stack reuses the existing feed).
