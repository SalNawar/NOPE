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
