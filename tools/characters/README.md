# Character art processing (`tools/characters`)

Turns ChatGPT's raw green-and-magenta character sources into the game-ready
layers the game loads by name. First used for the pilot (batch 1,
2026-09-26); the steps are the brief's "Claude processes" steps
(`docs/superpowers/drafts/art/CHARACTER_ART_BRIEF_v2.md` v2.2, sections 3, 4,
6, 7, 8 and 13) under the contract `docs/CHARACTER_ART_CONTRACT.md`.

```
python tools/characters/process_pilot.py                       # install into Assets/Art/Characters/Resources/Characters
python tools/characters/process_pilot.py --qa <dir>            # ... and write the QA composites + report.json into <dir>
python tools/characters/process_pilot.py --out <dir> --no-meta # try it somewhere else, no Unity metas
python tools/characters/process_pilot.py --only hair_f_egypt_ancient --out <dir> --no-meta   # one source (+ the bases)
```

Needs Python 3 with numpy and Pillow only. A full run takes about 5 minutes
(7 with QA). The output is deterministic for the same inputs.

## Files

| File | What it does |
|---|---|
| `process_pilot.py` | The pilot's source table (raw file, layer, gender, place, how it is registered) and the run: bases first, then each layer; writes the keys, their metas and the report. |
| `charkit/contract.py` | Reads the game's numbers from the game: `LookCanvas.cs` (landmarks, photo rect), `PlaceholderPalette.cs` (skin swatches, hair colours), `LookKeys.cs` (hair-colour tokens), `CharacterArt.cs` (the Resources folder), `world_source.json` (wig / back / covers flags). Builds key names with the LookKeys grammar and checks every name against `coverage.json` (the brief's LookKeys-derived list): a name the game would not load stops the run. |
| `charkit/keying.py` | The keyer (green and magenta removal, despill). |
| `charkit/landmarks.py`, `charkit/measure.py` | Landmarks: silhouette top and bottom, centre line, eyes, nose, mouth, chin, contours. |
| `charkit/register.py` | Similarity transforms, least-squares point fits, contour (chamfer + ICP) registration. |
| `charkit/warp.py` | Premultiplied resampling onto the canvas. |
| `charkit/layers.py` | Head/body split, skin and hair colour baking, the hair-back split, sealing mannequin gaps. |
| `charkit/unitymeta.py` | Hand-written Unity `.meta` files (fresh GUIDs). |
| `charkit/qa.py` | QA composites and the alpha-edge fringe check. |
| `charkit/imgops.py` | numpy morphology, labelling, colour bleeding. |

## Decisions

### Canvas and output

- Every output is a 1024 x 1536 RGBA PNG, **untrimmed**, straight (not
  premultiplied) alpha, the figure on `LookCanvas`'s landmarks (contract 2).
- Fully transparent pixels carry the nearest item colour (bled 6 px), so
  bilinear filtering and Unity's alpha-is-transparency dilation never pull a
  dark or green halo in.
- Names: `body_{g}_skin{N}`, `head_{g}_skin{N}_face{v}`,
  `{layer}_{g}_{nation}_{era}[_{colour}]` exactly as `LookKeys` builds them;
  files go to `CharacterArt.AssetFolder`
  (`Assets/Art/Characters/Resources/Characters/{key}.png`), which is where
  `CharacterArt` calls `Resources.Load<Sprite>("Characters/{key}")`.
- Metas: written by hand with fresh GUIDs, following the project's existing
  sprite metas (TextureImporter serializedVersion 13, like the retired
  `Assets/Art/Office/Placeholder/traveller.png.meta`) with exactly what
  `CharacterArtImporter` enforces on import: Sprite, Single, FullRect mesh,
  custom pivot (0.5, 46/1536 = the soles), pixels per unit 1536 (one unit
  tall), readable, no mipmaps, no crunch, max size 2048, alpha is
  transparency. The three new folders get folder metas. An existing meta is
  never replaced (re-runs keep GUIDs).

### Keying (`keying.py`)

The raw sources are opaque: the item on a flat magenta mannequin on flat
#00FF00 (or, for a base, the figure on green).

- **Green** is keyed by its colour difference gd = G - max(R, B), divided by
  the measured background's (the median of a 20 px border; about (4, 249, 7)).
  A pixel is background when gd >= 0.3 of the background's (about 73). The
  review's materials sit far below: bottle green (24, 64, 40) has gd 24,
  olive about 0, grey-green jade (64, 88, 72) about 16. So **bright chroma green
  and dark or desaturated greens are separated by the difference key, not by
  hue**, and the bottle-green qamis and the jade survive whole.
- **Magenta** is keyed as a hue family: ms = min(R, B) - G >= 25 with R and B
  balanced (the smaller at least half the larger). That covers the fill, the
  darker outline, the face marks and any shading painted on the mannequin
  (the review's "flatten unintended mannequin shading": it is all removed).
  Wine reds (R much larger than B) are outside the family.
- **Dark pixels are ambiguous**: the mannequin's dark outline and an item's
  outline have the same near-black colours. Dark pixels with a clear magenta
  tint (the mannequin's marks) join the magenta family outright. Any other
  dark pixel (tested after unmixing any green blended into it) is **mask**
  only when magenta is within 3 px, no item fill is within 2 px, no thick
  dark area (at least 7 px across: a black wig, a dark gown panel) is within
  3 px, and it is either not warm or sits on the mannequin's silhouette
  (touching both green and magenta). Warm dark pixels (blue the lowest
  channel: the dark-brown #3B2A20 outline, a leather cord) off the
  silhouette are item, so thin cords and straps drawn over the magenta
  survive (the petasos's chin cord was cut into dashes before this rule).
- Where magenta meets green with no outline, the one-pixel blend of the two
  (a grey) is mask when it lies on such a boundary.
- Specks: item components under 12 px, and under 40 px that lie more than
  8 px from any real part, are dropped (edge noise of the mask colours).
- **Soft alpha** only in a 2 px band where the item meets a key colour:
  alpha = (k_key - k_pixel) / (k_key - k_item) along that key's own
  difference axis (gd for green, ms for magenta), k_item being the most
  key-free item colour within 3 px. Item pixels in the band keep at least
  0.15.
- **Despill / edge decontamination**: every visible pixel within 6 px of the
  green has its green clamped to max(R, B) + the local legitimate green (the
  highest gd of deep item pixels within 6 px: 0 next to skin, cream or red,
  about 25 next to the bottle-green gown); within 6 px of magenta the
  magenta excess min(R, B) - G is removed from R and B (no item is
  legitimately magenta). Band pixels then take the colour of the nearest
  solid item pixel (blended with their own despilled colour above 50%
  coverage). Algebraic un-mixing was tried first and rejected: it amplifies
  noise into purple and green outlines.

### Registration

Every transform is a similarity (one uniform scale + translation; no
rotation: the figures are upright, and a rotated source would show up as
large residuals).

- **Bases -> LookCanvas** (brief 13, step 1): the scalp (first silhouette
  row) goes to y 260, the soles (last row) to y 1490, the skull's centre line
  to x 512. The chin is not fitted: its residual is the brief's head-size test
  ("with the top of the head and the soles on their lines, the chin sits
  clearly below the CHIN line" = head too big). The eyes, nose, mouth and
  chin are measured for the layers that register to the face.
- **Full-body mannequin sources -> their base**: the visible mannequin
  contour (figure edge next to magenta: parts the item does not cover) is
  matched to the base's silhouette contour by a grid search on a distance
  map then ICP (points over 6 px off are ignored). Composed with the base's
  transform. Outfits and accessories use the whole body.
- **Head items drawn on the full mannequin** (the man's hair, beard and
  petasos) use only the **head-and-neck contour** (down to 70 px below the
  chin) **plus the eye and nose marks** (equal weight): ChatGPT redraws the
  head a few pixels off the body (the petasos source's head sits 11 px higher
  than its body), and a head item must fit the head.
- **Head-only sources** (the review's registration exception: the five female
  hair / headwear images) are fitted by least squares on five face points,
  eyes, nose, mouth and chin, to the base woman's same points. Their heads
  were drawn 0-30% too big; the fit rescales them.
- Face marks: eyes are the largest blobs left after opening the dark marks
  with a 4 px disk (thin brows, nose, mouth and outlines disappear); nose,
  mouth and chin are the successive dark runs down the centre line (the
  mouth at least 10 px below the nose, the chin line at least 12 px below the
  mouth). On a mannequin only the eyes and nose are used (its mouth and chin
  marks are too faint to find reliably).
- Resampling: colours premultiplied, Pillow's antialiased bicubic resize
  over the exact fractional source box; after un-premultiplying, pixels
  under 60% coverage take the nearest solid colour (the filter's overshoot
  otherwise tints faint edge pixels).

### Mannequin gaps

Where a source shows magenta that, after registration, falls outside the
base figure, and that region is enclosed by the item (not touching the
source's green), the room would show through the item. Such holes are filled
with the item's nearest colour. The count is in the report (`sealed_gap_px`);
large counts mean a misregistration, and were how the petasos's shifted head
was found.

### Base figures: head / body split and skin tones

- Head = everything above 10 px under the chin outside the neck's columns
  (skull, ears, jaw corners), and over the neck everything down to the jaw
  outline's last dark row (the jaw line is the lowest short dark run in each
  column; the neck's own side outlines are skipped; a median over 7 columns
  removes outliers). No lighter row is left under the jaw: on a darker body
  it would show as a line.
- Body = the rest. Over the neck it is continued upwards behind the face to
  40 px above the chin by repeating the neck row just under the jaw (hidden
  by the head), so a head of another face or skin tone never leaves a gap at
  the chin (brief 7: faces keep face a's outline "within a few pixels").
- The brief (sections 6, 7, 8; coverage.json) has Claude make
  `body_{g}_skin1`..`skin5` from the approved skin-1 base: skin 1 is cut
  from it and **bodies 2-5 are recoloured** to the section-7 swatches
  (from `PlaceholderPalette.SkinSwatches`). Skin pixels (within about 25
  degrees of the measured skin hue, not grey, not outline) are moved from the
  measured skin fill to the swatch per channel in linear light, so the one
  hard shade keeps its ratio. The grey undergarment and the outlines keep
  their colours.
- **Skin 1 is normalised to its swatch too** (#F1D3C0; the drawings measured
  (243, 201, 178) and (250, 208, 182)), head and body with the same mapping,
  so they agree. The review asked for the skin to be normalised/validated.
- Heads for skins 2-5 and faces b, c, d are **not made**: the brief takes
  them from Batch 2 generations (`base_{g}_skin[2-5]_facea`,
  `head_{g}_skin{N}_face[b/c/d]`), not from a recolour. Until then those keys
  stay runtime placeholders.

### Hair colours (brief 7, 8)

- Natural hair and every beard are baked into the five `LookKeys.HairColours`
  (black, brown, blond, red, grey) using the game's own colours from
  `PlaceholderPalette.Hair`, so a baked file and its placeholder agree. Brown
  is re-baked too, so every place's brown is the same brown.
- A hair pixel (hue within about 30 degrees of the drawing's measured fill,
  saturated) becomes the target colour scaled by the pixel's luminance
  relative to the fill (linear light): shade and highlight keep their
  brightness ratios and take the target's hue exactly. Scaling each channel
  separately was tried first and rejected: brown's weak blue channel blew up
  into purple fringes on grey and blond.
- The dark outline keeps the brief's dark brown for every colour except
  black, where it is moved too so it stays darker than the fill.
- Ornaments are masked by hue (anything far from the brown: white, silver,
  blue, gold). The pilot has no hair ornaments (the brief lists none for its
  places).
- Wigs (`wig: true` in world_source.json: the Egyptian tripartite wig) keep
  their drawn colour: one file, no colour suffix.

### Hair back (contract 3; brief 6)

Only hair flagged `back: true` gets a `hairback_` key (in the pilot: the
tripartite wig, `hair_f_egypt_ancient`). The contract's hair back is "the
part of the hair that shows beside the neck behind the shoulders". In front
view the rear hair shows only where it hangs outside, and shorter than, the
front locks: from each outer edge inwards, the columns whose hair ends at
least 20 px above the front locks' lowest point are rear hair, and their
pixels below the chin move to the hair-back layer (drawn behind the body).
The lappets stay in front of the shoulders, as the review requires. As drawn,
the wig's rear section is only the thin outer strip at the bottom corners
(about 1000 px); it now sits behind the shoulders.

### Covers check

For headwear that hides the hair (`covers: ["Hair"]`: the izar hood) the
report counts, per processed hairstyle of that gender, the head pixels that
hair would cover but the headwear leaves bare (`hair_left_uncovered_px`):
bald scalp that would show when the game drops the hair.

### Not done here (needs later approved batches)

- `mannequin_{m,f}.png`, `style_card.png` and `premadebase_*` are made after
  the pilot is approved (brief 13 step 4, 14); the working mannequins in
  `References/` are ChatGPT's.
- No contact shade is added under hats (brief 4 allows it; the pilot's
  headwear reads without it).

## QA (`--qa`)

`stack_*_full.png` (full stacks in LookLayer order), `stacks_sheet.png`,
`desk_1080.png` / `desk_720.png` (the office view: the head 58 / 39 px tall,
headroom to waist, antialiased downscale) and their `_x3` enlargements,
`passport.png` (the LookCanvas photo rect at 48x60, 60x75, 95x119, 134x167
and full size), `keying_sheet.png` (raw | keyed on a checker | on dark |
alpha | pixel classes: green, magenta, blue = dark mask, black = dark item,
white = item fill, orange = dropped speck), `edges_worst.png` (each layer's three worst edge windows at 4x over
black and white), `hair_colours_and_skins.png`, and `report.json`.

The fringe check samples every visible edge pixel (coverage >= 0.25 next to
transparency, or partial coverage): a **green fringe** pixel has
gd more than 12 above the most-green solid item colour within 12 px (so the
bottle green and the jade are not fringe); a **magenta fringe** pixel has
min(R, B) - G > 12 with R and B balanced (the smaller at least 60% of the
larger: a madder or wine red is not magenta). `key_colour_px` counts any pixel of at least 10%
coverage still close to a key colour (gd or ms over 100): it must be 0.
