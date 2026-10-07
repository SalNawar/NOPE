# Notes for the character remake (2026-10-07)

Everything in `docs/CHARACTER_ART_CONTRACT.md` and `ArtDeliverables/TimeDesk/Characters/StyleReference/CHARACTER_STYLE_80S.md` still holds:
- the 1024×1536 canvas;
- the landmarks;
- the layer order;
- the file keys;
- the four expressions;
- the 1980s anime look.

Only the drawing style changes. Below are the new requests.

## 1. Passport photos show 2150 clothes, never the costume (new rule from Saleh)

Saleh: "If someone is in disguise, their passport pic shouldn't have them in old costumes." The ID photo was taken in 2150, so every traveller's photo shows them in plain 2150 civilian dress. The game builds the photo from these layers:
- the traveller's own body, head and facial hair;
- a **2150 hairstyle**;
- a **2150 outfit**;
- no headwear and no accessory.

The photo is the crop **(362, 215) to (662, 590)**, a 4:5 frame, so only the head, the neck and the top of the shoulders and chest show. Please draw:

| What | Keys | Notes |
|---|---|---|
| 2150 photo outfits | `outfit_m_civil_2150_v1..v3`, `outfit_f_civil_2150_v1..v3` | Full canvas on the base body, registered like every outfit. The neckline and shoulders carry the look. **v1** tidy: plain collared jacket, a middle-income tourist. **v2** labourer: work coverall or high-collar utility top. **v3** worn: layered hoodie and scarf, a displaced person. Muted 2150 debt-dystopia civilian wear; no text, logos or insignia. |
| 2150 hair | `hair_m_civil_2150_<colour>`, `hair_f_civil_2150_<colour>` | One neutral, ordinary 2150 cut per gender. Deliver it in one colour; `tools/characters` bakes the other four colours (black, brown, blond, red, grey). Add a hair-back layer if the women's cut needs one. |

## 2. Premades need a passport photo image

The ten premade people are single whole images, so the game can't swap their clothes. Each premade needs one more image besides its four expressions:
- **`premade_{id}_photo`**: the same person on the same canvas and pose, **neutral expression**, wearing a 2150 civil outfit like the ones above (any of v1-v3, chosen to suit them), with a 2150 or neat neutral hairstyle and **no era headwear or props**.
- Only the photo crop matters, but draw the whole figure so it registers like the other images.

## 3. Keep layers clean so the costume can be swapped

- Outfits, headwear and accessories must be separate layers that never paint over the head, hair or face area: "outfits are complete on their own and hold nothing above the chin" (contract section 6).
- Heads stay make-up free.
- Era hair stays on the hair layer, never baked into the head.
- Without this, the game can't rebuild a disguised traveller in 2150 clothes.

## 4. Order of work

1. The 2150 outfits and hair (6 outfits, 2 hairstyles). Small, and every traveller's photo uses them.
2. `premade_{id}_photo` for each premade, alongside its four expressions.
3. The remake batches in the existing priority order.
