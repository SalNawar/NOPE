# Character art pilot (batch 1): processing record

- **Branch:** `feat/character-pilot`, from `art` at `8446fb2` (the commit that holds the raw pilot).
- **Requested by:** Saleh, 2026-09-26: "lookup the character art done by it as well and hook it up" (the pilot ChatGPT generated).
- **Recorded:** 2026-09-26 by Claude. Claude wrote the tool and ran the QA, so this is the author's record, not a third-party review.
- **Unity:** not run. The persistent editor was busy with another branch. The Unity checks still needed are listed at the end.
- **Status:** the 42 game keys of the pilot are installed and load by name. The pilot is **not yet approved**: the brief's step 4 (the style card) and Batch 2 wait for Saleh's approval after the Unity check.

## What was processed

There were 15 selected raw sources in `ArtDeliverables/TimeDesk/Characters/Raw/batch01-pilot/`: the two base figures, the eight Athens pieces and the five stress cases. The `*_initial` and `*_rejected*` files were not used. The tool is `tools/characters/process_pilot.py` with its library `tools/characters/charkit/`, and every decision is written up in `tools/characters/README.md`. It keys out the green and the magenta with despill, registers every layer to its base figure and the bases to `LookCanvas`, splits the head from the body, bakes the variants the brief asks Claude for, and writes the keys with their metas. The output is deterministic: two runs gave byte-identical files.

**42 keys installed** in `Assets/Art/Characters/Resources/Characters/` (the folder `CharacterArt` loads with `Resources.Load<Sprite>("Characters/{key}")`). Each key is a 1024 × 1536 RGBA PNG, untrimmed, and has a hand-written `.meta` with a fresh GUID that matches `CharacterArtImporter`'s settings. The three new folders have metas too. Every name was checked against `coverage.json`'s LookKeys-derived list.

| Source | Keys |
|---|---|
| `base_m_skin1_facea` | `head_m_skin1_facea`, `body_m_skin1` … `body_m_skin5` |
| `base_f_skin1_facea` | `head_f_skin1_facea`, `body_f_skin1` … `body_f_skin5` |
| `outfit_m_greece_ancient` | `outfit_m_greece_ancient` |
| `hair_m_greece_ancient` | `hair_m_greece_ancient_{black,brown,blond,red,grey}` |
| `facialhair_m_greece_ancient` | `facialhair_m_greece_ancient_{black,brown,blond,red,grey}` |
| `headwear_m_greece_ancient` | `headwear_m_greece_ancient` |
| `accessory_m_greece_ancient` | `accessory_m_greece_ancient` |
| `outfit_f_greece_ancient` | `outfit_f_greece_ancient` |
| `hair_f_greece_ancient` | `hair_f_greece_ancient_{black,brown,blond,red,grey}` |
| `headwear_f_greece_ancient` | `headwear_f_greece_ancient` |
| `outfit_f_egypt_medieval` | `outfit_f_egypt_medieval` |
| `hair_f_egypt_medieval` | `hair_f_egypt_medieval_{black,brown,blond,red,grey}` |
| `headwear_f_egypt_medieval` | `headwear_f_egypt_medieval` |
| `hair_f_egypt_ancient` (a wig: one colour) | `hair_f_egypt_ancient`, `hairback_f_egypt_ancient` |
| `accessory_m_japan_ancient` | `accessory_m_japan_ancient` |

Variants: bodies 2 to 5 are recoloured from skin 1 to the section-7 swatches, and skin 1 is normalised to its own swatch, head and body alike. Natural hair and the beard are baked into the five `LookKeys` colours, using `PlaceholderPalette.Hair`. The wig keeps its black. The hair-back layer is split for the one item flagged `back` (the tripartite wig).

Not made, because they need later approved batches: heads for skins 2 to 5 and faces b, c and d (Batch 2 generates them), the mannequins, the style card and the premade bases (after the pilot is approved). No contact shade was added under the hats.

## QA numbers

QA ran without Unity. The composites and `report.json` are in the session scratchpad, `C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\charpilot\` (not committed). `python tools/characters/process_pilot.py --qa <dir>` rebuilds them anywhere.

### Base figures on LookCanvas

The scalp and soles are placed on their lines. The chin is not fitted, so its residual is the brief's head-size test.

| Base | Scale | Scalp | Soles | Centre line | **Chin** | Head height (guide 164) | Skin measured, then normalised to #F1D3C0 |
|---|---|---|---|---|---|---|---|
| man | 0.987 | 260 (0) | 1490 (0) | 512 (0) | 422.4 (**−1.6**) | 162 px | (243, 201, 178) |
| woman | 0.975 | 260 (0) | 1490 (0) | 512 (0) | 437.0 (**+13.0**) | 177 px (+8%) | (250, 208, 182) |

### Layers against their base (measured error, px, after registration)

The **contour** columns give the mean and 95th percentile distance of the visible mannequin outline to the base's silhouette. The **face** column gives the largest distance of the layer's own face marks from the base's face points. For head-only sources it covers the eyes, nose, mouth and chin, and those five points are the fit. For mannequin sources it covers the eyes and nose, which are measured independently for outfits and accessories and are part of the fit for head items. **Rescale** is the scale the source needed.

| Layer | Registered by | Rescale | Contour mean / p95 | Face max (px) | Holes sealed (px) |
|---|---|---|---|---|---|
| outfit_m_greece_ancient | body contour | 0.999 | 0.76 / 2.63 | 0.5 | 1 |
| hair_m_greece_ancient | head contour + eyes, nose | 0.978 | 0.83 / 2.30 | 0.5 | 0 |
| facialhair_m_greece_ancient | head contour + eyes, nose | 0.993 | 0.63 / 1.06 | 1.1 | 0 |
| headwear_m_greece_ancient | head contour + eyes, nose | 0.975 | 0.85 / 2.71 | 0.9 | 0 |
| accessory_m_greece_ancient | body contour | 0.998 | 0.50 / 0.89 | 1.3 | 0 |
| outfit_f_greece_ancient | body contour | 0.996 | 0.74 / 2.01 | 3.0 (left eye) | 15 |
| outfit_f_egypt_medieval | body contour | 0.996 | 0.76 / 2.37 | 2.8 (left eye) | 0 |
| accessory_m_japan_ancient | body contour | 1.000 | 0.51 / 0.66 | 1.8 | 0 |
| hair_f_greece_ancient | face (5 points) | **0.768** | – | 2.1 | 10 |
| headwear_f_greece_ancient | face (5 points) | **0.827** | – | 2.0 | 0 |
| hair_f_egypt_medieval | face (5 points) | 0.994 | – | 1.2 | 2 |
| headwear_f_egypt_medieval | face (5 points) | **0.867** | – | 1.5 | 9 |
| hair_f_egypt_ancient | face (5 points) | **0.797** | – | 1.5 | 25 |

- **The petasos source's head was drawn about 11 px higher than its body.** Registered by the whole body, the hat sat 11 px too high and left a band of bare scalp under the brim, which the gap check caught as 1,091 px. The head items drawn on the full mannequin are now registered by the head and neck: the head fit moved the petasos 11.1 px down at the eyes (12.9 px at the scalp), the man's hair 1.3 px and the beard 0.8 px.
- Covers check for the izar hood, which hides the hair: the head pixels the Mamluk braids would cover but the hood leaves bare come to **1 px**. The same count for the Athens hair is 459 px, from the curls hanging beside the ears, which lie outside any hood's reach. This matters only if the hood leaks onto an Athens claim, and it is not scalp.
- The hair-back split of the tripartite wig moved **1,029 px** (the rear section visible outside the lappets at the lower corners) behind the body. The lappets stay in front of the shoulders.

### Keying and edges

- **The review's green cases are preserved.** 200,618 bottle-green qamis pixels: every interior pixel stays fully opaque, and 99.95% of all of them are kept. 1,472 grey-green jade pixels: every interior pixel stays opaque, and 99.93% are kept.
- **Alpha-edge check** over 208,325 visible edge pixels of all 42 keys (at least 25% coverage next to transparency):
  - Green fringe: **4 px**, all on the jade (gd at most 15 above the jade's own green).
  - Magenta-hued fringe: **57 px**, mostly dark purplish anti-aliasing (min(R,B) − G at most 17.5).
  - Pixels still close to a key colour (gd or ms over 100, coverage at least 10%): **0**. No technical mask colour reaches Unity.
- The office view at 1920 × 1080 (head about 58 px) and at 1280 × 720 (39 px): every leak item reads, namely the petasos, the sakkos, the izar hood, the wig and the magatama necklace. See `desk_1080.png`, `desk_720.png` and their `_x3` enlargements.
- The passport crop (362, 215)–(662, 590) at 48×60, 60×75, 95×119 and 134×167: `passport.png`. The petasos brim is cut by the frame, which the contract allows.

Composites: `stack_*_full.png` (12 stacks: both Athens looks with and without headwear; the Mamluk woman with the hood and with the braids; the sakkos leaked onto the braids; the hood leaked onto the Athens woman; the wig with its hair back on the base and over a garment; the magatama on the bare man and over the Athens outfit), `stacks_sheet.png`, `desk_*.png`, `passport.png`, `keying_sheet.png` (raw | keyed | on dark | alpha | pixel classes), `edges_worst.png` (each layer's three worst edge windows at 4× over black and white), `hair_colours_and_skins.png`.

## Known issues for ChatGPT's next generations

1. **The woman base's head is 8% too big** (the chin lands 13 px below the CHIN line with the scalp and soles on theirs). The brief says to reject such a figure. Every female layer was drawn around this base, and the stack is consistent, so the pilot works. **Saleh should decide before Batch 2**: accept this base, or regenerate `base_f_skin1_facea` and redo the female mannequin and layers. The man is within 2 px.
2. **The head-only female sources come out 0–30% too big and at different sizes** (rescales of 0.77, 0.83, 0.87, 0.80 and 0.99). Registration fixes the placement (face residuals ≤ 2.1 px). The downscale thins their outline to about 1.5–2 px against the 2–3 px of the other layers, and the sizes do not agree with each other. Recommendation: draw female hair and headwear on the full `mannequin_f` like the men's, or at least ask for the head at the mannequin's exact size.
3. **Head items drift on the full mannequin too**: the petasos source's head sat 11 px higher than its body. The tool now registers head items by the head. Still, "do not move the head" should be repeated in the prompt, because ChatGPT redraws the whole figure.
4. **The sakkos is smaller than the Mamluk braid crown.** When the sakkos leaks onto a Mamluk claim, the braids show above it (`stack_mamluk_f_sakkos_full.png`). The brief wants headwear sized over a full head of hair: the sakkos should be a little larger, or be checked against the biggest pinned-up styles of each batch.
5. **The tripartite wig's lappets are very wide.** They cover most of the shoulders and the upper chest down to y 565, and the rear section shows only as thin outer strips, which is all the hair-back layer holds. It reads well, but it will hide much of the linen sheath dress's shoulders and straps. The wesekh collar, an accessory drawn above the hair, will cover the lappets' lower part. Check the Egypt-ancient stack when the dress and the collar arrive.
6. **The drawn hair browns vary**: the man measures (128, 88, 49), the Athens woman (152, 86, 64) and the Mamluk braids (106, 49, 40), against the brief's #78513A = (120, 81, 58). Baking normalises all five colours, so this is not visible in the game, but a much redder brown would bake less cleanly.
7. **The drawn skin is lighter and warmer than the swatch**: the man measures (243, 201, 178) and the woman (250, 208, 182), against #F1D3C0 = (241, 211, 192). It is normalised in processing.
8. Small leftovers: the man's curl tips over the magenta forehead lose a few pixels to the magenta and show a slight stair-step. A few dark sideburn fragments from the mannequin outline survive next to the hair. Both are invisible at the desk's size.

## What ChatGPT's `Production/STATUS.md` should record (not edited here)

- Batch 1 was processed on 2026-09-26 by `tools/characters/process_pilot.py` on branch `feat/character-pilot`. **42 game keys are installed** in `Assets/Art/Characters/Resources/Characters/` (the list above), so the pilot's placeholders are replaced in the game.
- Bodies 2 to 5 are recoloured, and skin 1 is normalised to #F1D3C0. The hair and beard colours are baked. The wig has one colour and a hair-back layer.
- Still to do: the Unity check in the art office, then Saleh's approval of the pilot, then the style card and the Batch 2 heads. The open decision is known issue 1 (the woman base's head size).
- Prompt changes to make before more garments: known issues 2 and 3 (female head items on the full mannequin; keep the head where it is) and 4 (headwear sized over the largest hairstyles).

## Unity verification still needed

Run it as a job in the art-branch office, never in the old 2D booth.

1. **Import:** the 42 PNGs import as sprites through `CharacterArtImporter` with no 2:3 warning. Record whether Unity rewrites the hand-written metas; if it does, commit Unity's version. The PNGs are Git LFS objects, so check they arrive as images and not as pointers.
2. **Loading:** `Resources.Load<Sprite>("Characters/<key>")` returns every key, and Validate Content Library's art count goes up by the new distinct keys.
3. **The office view:** force a traveller of each pilot look with **skin 1 and face a** (the only real heads so far) and screenshot the stack behind the desk at 1920 × 1080 and 1280 × 720. Check:
   - the layer order and the pivot at the soles;
   - the room tint (`SetTint`);
   - that the leak items read;
   - that the outlines hold up under compression.
   The importer turns mipmaps off, and the sprite is drawn at about 0.36 scale, so watch for aliasing and shimmer on the thin outlines. If it shows, the importer's mipmap setting needs a decision, which is a code change.
4. **The passport photo:** `GetPhoto` crops on the paper, on the scanned copy and on the held paper.
5. **A traveller with another skin or face:** the real body with a placeholder head. That is expected until Batch 2; confirm that it looks acceptable, or decide to hold bodies 2 to 5 back until their heads arrive.
6. **Leaks in play:** a Mamluk claim with the sakkos leaked (the braids over the hood-less head), and the hood leaked onto another claim (no hair drawn under it).
