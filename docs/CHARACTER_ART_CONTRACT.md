# Character art contract

*Piece 4 (characters), 2026-09-25. The binding technical contract between the game and the character art. The detailed ChatGPT brief (`docs/superpowers/drafts/art/CHARACTER_ART_BRIEF_v2.md`, with `coverage.json` and `character_guide_v2_1024x1536.png`) follows this file; where they disagree, this file and the code win.*

It supersedes the older character contracts: the 2026-09-23 `ART_ASSET_LIST.md` §D (visitor trios, legendary pairs) and its Tier-1 `traveller.png` (the list was rewritten for the 3D office on 2026-09-25 and points here), and `PRODUCTION_PLAN.md`'s character direction. `Assets/Art/Office/Placeholder/traveller.png` is retired and unused by the game: no new art goes there. Its only references are the art scene's leftover 2D booth (`OfficeRoot/Traveller` in `OfficeScene.unity`, switched off when the office loads) and the recovery scenes (`Assets/_Recovery/`); it goes when the art side deletes them (`docs/SCENE_CONTRACT_GAMEPLAY.md`).

## 1. Style

Simpler, ReStory-like textures, neutral even lighting, front view, the civilian dress of the claimed place and moment. No text, insignia, regalia or religious vestments. Skin and hair colour are never evidence.

## 2. Canvas (`LookCanvas`)

- Every layer and every premade image is **1024 × 1536 px** (2:3), RGBA PNG, untrimmed, the figure in the same place on every file.
- Landmarks (px from the top): head top 260, chin 424, shoulders 500, waist 760, hips 900, knees 1170, soles 1490. Safe area x 120..904, y 10..1490. Headroom above 260 is for hats and buns.
- The sprite pivot is the soles (x 512, y 1490): the importer makes each image one unit tall with that pivot, so final art and the 256 × 384 runtime placeholders are interchangeable.
- The passport photo is the crop (362, 215)–(662, 590), 4:5; tall headwear is cut by it.

## 3. Layers and stack order (`LookLayer`)

Bottom first: hair back, body, outfit, head, facial hair, hair, headwear, accessory; a premade is one whole image instead. The hair back belongs to the hair item (only hair flagged `back`) and is drawn behind the body. Facial hair, headwear and accessory are optional per place and gender. An item can hide another (`covers`): a hidden layer is not drawn.

## 4. File names (`LookKeys`)

| Layer | Key |
|---|---|
| Body | `body_{g}_skin{N}` (g = m/f, N = 1..5) |
| Head | `head_{g}_skin{N}_face{v}` (v = a, b for 18–34; c for 35–59; d for 60+) |
| Garment | `{layer}_{g}_{nation}_{era}` (layer = outfit, headwear, accessory) |
| Hair, hair back | `{layer}_{g}_{nation}_{era}_{colour}` (black, brown, blond, red, grey), or no colour for a wig |
| Facial hair | `facialhair_{g}_{nation}_{era}_{colour}`, always coloured (a wig never strips the beard's colour) |
| Premade | `premade_{id}_{expression}` (neutral, happy, angry, worried) |

Nation, era and premade ids are lowercase letters and digits. A Future place (piece 5) uses the ordinary grammar with era `future`. An item may be filed under a shared art nation instead of its place's nation (`artNation` in `world_source.json`, a key token), so places of one era share one drawing: the eight Future places draw only their own culture-shaped outfits (`outfit_{g}_{country}_future`) and share one neutral hair per gender and one men's beard (`hair_{g}_neutral_future_{colour}`, `facialhair_m_neutral_future_{colour}`). The suffix `_v{N}` is reserved for outfit variants. Colour variants are baked files (the game never tints).

## 5. Delivery

Put each file at `Assets/Art/Characters/Resources/Characters/{key}.png`. Import is automatic (`CharacterArtImporter`: sprite, full rect, pivot at the soles, one unit tall, readable, no mipmaps, no crunch, at most 2048 px; a non-2:3 file is reported). Every key without a file is drawn as a placeholder at runtime, so art can arrive in any order. Tools > TimeDesk > Validate Content Library logs how many keys have art and the first missing names.

## 6. Drawing rules that keep dress tells fair

- A signature item is always an item, never an absence ("clean-shaven", "bareheaded").
- A leakable item reads on its own at the size the office shows a traveller (behind the desk, from the head to the waist: the head about 58 px tall at 1920 × 1080 and 39 px at 1280 × 720, one canvas pixel about 0.36 and 0.24 screen pixels; the brief's "Where the player sees a traveller"), front-visible, on the head, face, neck, shoulders or upper chest, not hanging off another layer; outfits are complete on their own and hold nothing above the chin.
- Headwear is sized over full hair; heads carry no make-up; skins 2–5 are recoloured from skin 1 with ornaments masked first.
- Items named alike in the Costume Guide must not look alike: the confusable pairs in `world_source.json` (`looks.confusable`) keep such pairs from leaking, but the art should keep them apart too.

## 7. Premades

Ten real, long-dead people who are not rulers, five women and five men (the cast is in the characters spec §2.14). Four aligned expression images each, drawn on the approved base figure, wearing their claimed place's Costume Guide item. Real people are drawn in a separate chat without the no-likeness clause.

## 8. Other art

`refbook_cover_culture.png` (the Costume Guide's cover) is still to come. The passport photo frame on the paper and on the scanned page is a builder placeholder; final document art keeps a 4:5 photo window.
