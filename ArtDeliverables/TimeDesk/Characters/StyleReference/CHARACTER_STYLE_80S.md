# Character style: 1980s anime (Saleh, 2026-10-07)

Saleh, pointing at `style_80s_anime_portrait.png`: "that's the type of art I wanted for our characters - 80s anime." The full passport this crop came from is `style_80s_anime_passport_context.png`.

This replaces the current "clean 2D anime" look for every new character image: generated traveller layers, heads, hair, outfits and the premade whole figures. The art already installed stays in the game until its replacement lands.

## What makes the look

The target is a late-1980s TV and OVA mecha-era cel look: Gundam, Macross, City Hunter, Patlabor.

- **Proportions:** realistic, adult proportions. Long faces, defined cheekbones and a firm jaw and chin; no chibi, no moe, no huge round eyes.
- **Eyes:**
  - narrower and almond-shaped, with heavy upper lids drawn as a thick dark line;
  - small irises with one or two sharp white highlights;
  - thin, angled brows.
- **Line:** confident ink outlines of varying weight. Thicker on the silhouette and under the jaw, fine inside. Dark brown-black ink, never pure grey.
- **Hair:** chunky, layered, spiky strands with clear clumps. Two-tone cel shading plus a crisp highlight band (blue-black hair shows blue highlights).
- **Shading:**
  - hard two-tone cel shadows: warm skin shadow tones, cooler, slightly purple shadows on cloth;
  - a single soft airbrushed touch on the cheek or collar at most.
- **Colour:** slightly muted, warm retro palette, as if painted on cels and filmed. A faint film grain is fine; no neon gradients and no modern glossy rendering.
- **Clothing:** solid, believable tailoring with clear folds drawn as ink lines plus one shadow tone. Era costumes keep their historical accuracy, from the character brief.
- **Pose and expression:**
  - three-quarter view, calm and readable;
  - expressions (neutral, happy, angry, worried) acted through brows, lids and mouth, the way 80s anime does it: subtle, never exaggerated.

## Avoid
- modern anime features (large shiny eyes, tiny noses with no bridge, pastel moe palettes, painterly soft shading);
- 3D-render or semi-realistic painting;
- chibi proportions;
- thin uniform vector lines;
- heavy bloom.

## Keep from the existing contract
The character pipeline's technical rules are unchanged: sizes, flat background, registration of layers, layer order, file keys, the expression set, and Block P rules for famous people (`docs/CHARACTER_ART_CONTRACT.md`, `CHARACTER_ART_BRIEF_v2.md`, `FAMOUS_PREMADES_REQUEST.md`). Only the drawing style changes.

## Suggested process
1. Restyle the pilot first: one base body and head per gender, and one premade (four expressions).
2. Place them in the game next to the current art and send Saleh the comparison.
3. On his OK, carry the style through the remaining batches in the existing priority order.
