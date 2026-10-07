# Character art contract

*Piece 4 (characters), 2026-09-25. The binding technical contract between the game and the character art. The detailed ChatGPT brief (`docs/superpowers/drafts/art/CHARACTER_ART_BRIEF_v2.md`, with `coverage.json` and `character_guide_v2_1024x1536.png`) follows this file; where they disagree, this file and the code win.*

It supersedes the older character contracts: the 2026-09-23 `ART_ASSET_LIST.md` §D (visitor trios, legendary pairs) and its Tier-1 `traveller.png` (the list was rewritten for the 3D office on 2026-09-25 and points here), and `PRODUCTION_PLAN.md`'s character direction. `Assets/Art/Office/Placeholder/traveller.png` is retired and unused by the game: no new art goes there. Its only references are the art scene's leftover 2D booth (`OfficeRoot/Traveller` in `OfficeScene.unity`, switched off when the office loads) and the recovery scenes (`Assets/_Recovery/`); it goes when the art side deletes them (`docs/SCENE_CONTRACT_GAMEPLAY.md`).

## 1. Style

Simpler, ReStory-like textures, neutral even lighting, front view, the civilian dress of the claimed place and moment. No text, insignia, regalia or religious vestments. Skin and hair colour are never evidence of where someone is from; the only check that reads them is a paper's photo against the person at the desk (someone else's photo shows another skin tone and hair colour from these same layers; the document design spec, D8).

## 2. Canvas (`LookCanvas`)

- Every layer and every premade image is **1024 × 1536 px** (2:3), RGBA PNG, untrimmed, the figure in the same place on every file.
- Landmarks (px from the top): head top 260, chin 424, shoulders 500, waist 760, hips 900, knees 1170, soles 1490. Safe area x 120..904, y 10..1490. Headroom above 260 is for hats and buns.
- The sprite pivot is the soles (x 512, y 1490): the importer makes each image one unit tall with that pivot, so every layer registers with every other whatever its resolution.
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

Put each file at `Assets/Art/Characters/Resources/Characters/{key}.png`. Import is automatic (`CharacterArtImporter`: sprite, full rect, pivot at the soles, one unit tall, readable, mipmaps with trilinear filtering, high-quality BC7 compression, no crunch, at most 2048 px; a non-2:3 file is reported). A key without a file is drawn with its nearest delivered stand-in (section 9), so art can arrive in any order and replaces its stand-in the moment it lands. Tools > TimeDesk > Validate Content Library logs how many keys have art and the first missing names. Raw ChatGPT sources are processed into keys by `tools/characters/process_pilot.py` (keying, registration, the variants; `tools/characters/README.md`); rejected candidates under `revisions/` are never processed.

## 6. Drawing rules that keep dress tells fair

- A signature item is always an item, never an absence ("clean-shaven", "bareheaded").
- A leakable item reads on its own at the size the office shows a traveller (behind the desk, from the head to the waist: the head about 58 px tall at 1920 × 1080 and 39 px at 1280 × 720, one canvas pixel about 0.36 and 0.24 screen pixels; the brief's "Where the player sees a traveller"), front-visible, on the head, face, neck, shoulders or upper chest, not hanging off another layer; outfits are complete on their own and hold nothing above the chin.
- Headwear is sized over full hair; heads carry no make-up; skins 2–5 are recoloured from skin 1 with ornaments masked first.
- Items named alike in the Costume Guide must not look alike: the confusable pairs in `world_source.json` (`looks.confusable`) keep such pairs from leaking, but the art should keep them apart too.

## 7. Premades

Ten real, long-dead people who are not rulers, five women and five men (the cast is in the characters spec §2.14). Four aligned expression images each, drawn on the approved base figure, wearing their claimed place's Costume Guide item. Real people are drawn in a separate chat without the no-likeness clause.

## 8. Other art

`refbook_cover_culture.png` (the Costume Guide's cover) is still to come. The passport photo frame on the paper and on the scanned page is a builder placeholder; final document art keeps a 4:5 photo window.

## 9. Only ChatGPT art, and the fallback (2026-09-30)

Saleh: "characters should only use new assets by chat gpt". The game draws no character art of its own: every layer is a ChatGPT file (or a variant baked from one by `tools/characters`: the five baked hair colours, bodies 2-5 and the interim heads 2-5 recoloured from skin 1 per section 6). A key with no file is drawn with its **nearest key that has a file**, found by `LookArtFallback` from the table in `Assets/Resources/CharacterArtFallback.asset` (`CharacterArtFallbackSO`, edited in the inspector; Generate World never writes it). A stand-in keeps the layer, the gender, the hair colour (a wig's uncoloured name only meets wigs) and the art variant; its steps, tried in order after the key itself:

| Layer | Steps |
|---|---|
| Body | another skin tone, the nearest first (the lighter on a tie) |
| Head | another face of the same skin (a, b, c, d), then the nearest skin |
| Outfit, hair, facial hair, headwear | the same nation in the nearest era (the earlier on a tie); the same era of each neighbouring nation; then any place, the nearest era first and the neighbours before the other nations |
| Hair back, accessory | the same nation in the nearest era; the same era of each neighbouring nation (no further: a missing collar or back of hair reads better than a wrong one) |
| Premade whole picture | the same premade's neutral picture |

Neighbours (the nearest first): Egypt: Iraq, Greece, Italy. Iraq: Egypt, Greece, Italy. Greece: Italy, Egypt, Iraq. Italy: Greece, Germany, Britain. China: Japan, Iraq. Japan: China. Britain: Germany, Italy. Germany: Britain, Italy. The shared `neutral` nation has none (it reaches art through "any place").

A layer whose steps find nothing is not drawn (the pilot has no man's wig, so an Egyptian man of the Old Kingdom shows his shaved head). Development builds log each stand-in once (`[CharacterArt] No ChatGPT art for '<key>' yet; drawing '<stand-in>' in its place`). What a stand-in shows is only art: the garments the player looks at, their labels and the Costume Guide rows never change with it. `docs/ART_ASSET_LIST.md` ("Coverage today") lists what each place is drawn with and what to commission next.

## 10. The 1980s set and pose frames (2026-10-07)

Saleh chose the 1980s anime style (`CHARACTER_STYLE_80S.md`). GPT's raw sources (`ArtDeliverables/TimeDesk/Characters/RetroRegeneration/Raw`) become keys of a second set, `Resources/Characters/80s/{key}.png`, by `tools/characters/process_retro.py`; the classic set stays until each replacement lands. A traveller is drawn from the 80s set only when every key of their look is in it, else wholly from the classic set: one traveller never mixes styles (`LookArtSets`, `CharacterArt.PresentTraveller`).

- **Pose frames** are instant swaps of still frames on dialogue beats: neutral (arrival, idle, after a response), explaining (answers), thinking (the wheel open), objecting (a challenge, a reaction to a denial) (`TravellerPose`). A frame's key is the neutral key plus `__{pose}`, the pose `{category}_{a|b|c}` (GPT's pose families); a premade's frame is `premade_{id}__{pose}`.
- **The moving set** is the body, the outfit and the accessory, plus `hands_{g}_skin{N}__{pose}` (drawn over everything) for thinking_a and thinking_b. Each outfit is drawn in neutral plus one assigned variant per category; the frame takes the variant the outfit is drawn in. When any layer of the set has no drawing, the whole set stays neutral. A frame never has a stand-in.
- **The ID photo** shows 2150 civilian dress (`Looks.PhotoLook`): `outfit_{g}_civil_2150_v1..v3`, `hair_{g}_civil_2150_{colour}` (baked from GPT's brown), and `premade_{id}_photo`.
- **Registration**: every source of a gender sits on GPT's fitting figure; one similarity per gender maps it to the landmarks (scalp 260, soles 1490), after the source's visible fitting figure is checked against the base (more than 6 px or 1.5% off is rejected, never stretched). Premades follow GPT's drawing convention (eyes at raw y 340, soles at 1473) onto the fitting figure's eye line and Feet.
- **Skin**: heads keep GPT's drawing per skin and face (faces b-d moved onto their skin's face-a tone); bodies 2-5 are recoloured from body skin 1 to GPT's own tones (GPT's skin 2-5 bases wear calibration clothes).
