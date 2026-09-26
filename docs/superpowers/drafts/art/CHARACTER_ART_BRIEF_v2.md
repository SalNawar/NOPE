# Time Sorter: Character Art Brief v2.3 (for ChatGPT)

*2026-09-24, revised the same day after the review of v2 (Appendix E), on 2026-09-25 for the 3D office (v2.1: see "What changed in v2.1"), again on 2026-09-25 for the ReStory style (v2.2: see "What changed in v2.2"), and on 2026-09-26 for the 2150 clothes and accessory kit (v2.3). Replaces `docs/CHARACTER_ART_BRIEF.md` (v1). Follows the piece-4 characters design (layers, file names, canvas), its Amendment A1 (premade cast about half women), piece 5 (Future outfits), the office move (the game in the art side's 3D office) and Saleh's style direction: like ReStory's cute 2D anime-style customers, for both kinds of character, with flat cel colours, simple textures and neutral even lighting.*

**The contract this brief follows.** This brief implements `docs/CHARACTER_ART_CONTRACT.md`, the tracked character-art contract of the piece-4 design (its W1 and R26). The older character contracts are retired: `ART_ASSET_LIST.md` section D (the 240 x 440 visitor trios and legendary pairs), its Tier-1 `traveller.png`, and the character direction in `PRODUCTION_PLAN.md`. No character art is delivered to them, and none goes to `Assets/Art/Office/Placeholder/traveller.png`: the game no longer uses that file. It stays on disk only because the art scene's leftover 2D booth (`OfficeRoot`, switched off when the office loads) and the art side's recovery scenes still reference it, and it goes when the art side deletes those leftovers (`docs/SCENE_CONTRACT_GAMEPLAY.md`).

Everything ChatGPT needs to draw every character in the game. "Time Sorter" is set in a 3D office; the travellers are flat 2D figures, built from layers, standing behind its desk. Work through the brief batch by batch, and **send each place's files to Claude as soon as that place is done**: Claude cuts the images out, lines them up, bakes the hair colours, names them for the game and tests them in Unity, in the game's office. A mistake that repeats (square images, a drifting mannequin, a colour the cut-out eats) then costs a few images instead of a whole batch. The game already runs on coloured placeholder shapes, and every finished file replaces its placeholder the moment it lands, so each place makes the game look better straight away.

Files that go with this brief (all in this folder, except the office screenshot):

- `office_style_reference.png`: a screenshot of the game's office that Saleh takes once (Unity's Game view at 1920 x 1080, the office with no traveller at the desk, saved as a PNG). It is a **style reference only**: ChatGPT matches its colour range and contrast so the figures sit in that room, and never draws the room. Attach it where a prompt's "Attached:" line names it.
- `character_guide_v2_1024x1536.png`: the figure guide. Attach it in Batch 1.
- `mannequin_m.png` and `mannequin_f.png`: Claude makes these from your approved Batch 1 figures.
- `style_card.png`: Claude makes it from the approved pilot (Batch 1, Step 4). Every garment prompt and the premade prompt (9.10) attach it from then on as a style reference.
- `premadebase_m_skin[N].png` and `premadebase_f_skin[N].png`: Claude makes these after Batch 2 (the approved body and head of each skin tone, lined up). The premade prompts attach them.
- `CHATGPT_MESSAGE.md`: the short message that starts the work.
- `UI_ART_RULES.md`: the rules for the desktop wallpapers, the 2D layers over the office (the PC frame, the speech bubble, the paper faces) and other UI art (a separate track).
- `coverage.json`: every file name the game needs, so the delivery can be checked by a script.

Attach whatever each prompt's "Attached:" line names.

## What changed in v2.3 (2026-09-26): 2150 clothes and the accessory kit

Every traveller must be dressed for the time they are going to, or they would cause a panic there (Saleh). A citizen of 2150 travelling into the past can get it wrong: another place's item, their own 2150 clothes, or one 2150 accessory over an otherwise right costume. Two things change for the art:

1. **The neutral Future outfits become game files** (`outfit_m_neutral_future`, `outfit_f_neutral_future`): they are the present's clothes, which the game draws on a 2150 citizen who forgot their costume. They were working references only; draw them exactly as Batch 11 says.
2. **New: the 2150 accessory kit** (Batch 12, section 24): four small items per gender (smart lenses, comm earpiece, transit badge, shoulder display), each readable at a glance above the desk and worn over any period costume. They are filed under the art nation `neutral` with their own name at the end (`accessory_m_neutral_future_lenses`).

Nothing else changes.

## What changed in v2.2 (2026-09-25): the ReStory style

v2.1 named the 2D customers of *ReStory: Chill Electronics Repairs* as its reference, then steered away from them: realistic proportions, a ban on large heads and eyes, and a ban on anime styling in Showa Tokyo's DO NOT DRAW. ReStory's customers are cute 2D anime-style characters with a soft, rounded, illustrative look, standing in a detailed 3D shop. That is this game's own set-up (flat 2D travellers in a 3D office), and the art side now cel-shades the office's desk props (`NOPE/Desk Anime`). Saleh asked for that style for both kinds of character: the generated travellers and the premades. Every change:

1. **The style (section 2, Block A, Block P).** The shared STYLE text of both blocks is rewritten: cute, soft 2D anime-style characters (expressive anime eyes, larger than realistic, with one simple highlight; a small, simple nose and mouth; clean rounded face shapes; hair in clean stylised shapes and locks), flat cel colours with one hard-edged shade tone, the same dark-brown outline and the same neutral, even front lighting. Adults who look their age, never chibi. No manga symbols (sweat drops, anger marks, blush lines); section 10 says so too.
2. **One cast.** The style applies the same way to both kinds of character, and section 2 and both blocks say so: the same eyes, line, shading and colour treatment, so a premade never stands out from a generated traveller at the desk. The premade prompt (9.10) now attaches the style card too, and Claude compares each premade with the pilot's Athens travellers (section 11).
3. **The proportions do not change.** The head size and body proportions stay the figure guide's and `LookCanvas`'s (the mannequins, every layer and the passport crop depend on them): the style changes the face and the rendering only. Batch 1, Step 1 now rejects a base figure whose head is too big for its body.
4. **Faces stay distinct.** Section 7 and prompts 9.1 and 9.3 say how faces a to d differ (cheeks and chin, eye shape, brows, nose, the lines of age) inside face a's head outline, which Batch 2 still requires. The skin tones stay exactly their swatches (the style never lightens a darker skin), and every culture gets the same style.
5. **Premade expressions** (prompt 9.11, Block P): anime-expressive but dignified, because they are real people; each premade keeps the features its description gives (Socrates' snub nose and full lips, for example).
6. **Showa Tokyo.** Its DO NOT DRAW bans "cosplay, idol-costume or anime-costume cliches" in place of the old anime ban, which would now ban the style itself, and adds "a school-uniform look": in this style its office outfit (a blouse with a bow at the collar and a pleated skirt) could drift into one. Every other DO NOT DRAW and review line was checked against the new style; none contradicts it.
7. **`CHATGPT_MESSAGE.md` and `README.md`** carry the new style rules; the first image is still the bald base man, now in this style.

Nothing else changes: the layers, canvas and landmarks, the green background and colour rules, the leak items and MUST READ lines, section 1's desk view and sizes, the file names, the batch order and files, and every key in `coverage.json`.

## What changed in v2.1 (2026-09-25): revised for the 3D office

The v2 brief was written for the old flat 2D booth. The game now runs in the art side's Blender-made 3D office, with the travellers as flat 2D layered figures standing behind its desk. Every change, decided by Claude under Saleh's "go with all pieces, don't stop" (open to his review):

1. **The framing (D1).** Block A, Block P, `CHATGPT_MESSAGE.md` and the `UI_ART_RULES.md` set-up say it: "Time Sorter" is set in a 3D office and the travellers are flat 2D figures standing at the desk. A screenshot of the office, `office_style_reference.png`, is attached as a **style reference only** (its colour range and contrast, so the figures sit in the room; the room is never drawn). The flat style, the outlines, the neutral even lighting and the green background are unchanged. "Unity adds time-of-day light" is replaced by what the game does: it stands the flat, unlit figure in the lit room and tones it to the room's light.
2. **Where the player sees a traveller (D2).** Section 1's "shown three ways" and the Visitor window (270 x 406 px) are replaced by a table of the three places a traveller appears: the office view (head to waist, measured sizes), the passport photo, and the Look menu (names only). Nothing shows the whole figure. The layers stay whole figures on the same 1024 x 1536 canvas (the contract is unchanged; a walk-in or a moved anchor may show more later), but nothing that identifies a look relies on the lower body, and every identifying item must read at the measured head size. Section 3's canvas table, section 6's accessory row, prompts 9.4 and 9.8 and the checklist say so.
3. **Leak items the desk can see (D3).** Every leak item now sits on the head, face, neck, shoulders or upper chest. Five sat below the desk; each was replaced or moved (details and sources in Appendix F):
   - Ottoman Ioannina, woman: the **pafti buckle** (waist) becomes **silver chest chains** (the pafti stays drawn, closing the outfit's belt);
   - Tokugawa Edo, woman: the **Nagoya-obi** (hips) becomes the **kazuki veil**, a kosode worn over the head (the Nagoya-obi stays drawn, in the outfit);
   - Beijing 1972, woman: the **khaki satchel** (hip) becomes the **navy cap**, the same cap as the men's (the satchel stays drawn, not leakable);
   - Metapolitefsi Athens, man and woman: the **tagari bag** stays the leak item but is drawn high against the side of the chest on a short strap;
   - Eastern Han Luoyang, woman: the **bi-disc pendant** (mid-chest, borderline) moves up to the upper chest.
   Every MUST READ line now names only what shows above the desk (21 rewritten; the lower body stays in the outfit lines, because it is still drawn), and `tools/data_v2.py` fails on a leak accessory or a MUST READ line that names a lower-body feature.
4. **The game matches (D4).** The same leak-item change is in `Assets/Data/World/world_source.json` (the piece-4 wardrobe that `worldSourceWardrobe` mirrors): the three changed signatures, their labels and `leakable` flags, the Costume Guide rows (Culture values) they give, one new hand-authored confusable pair and 4 generated ones (Appendix D).
5. **UI art (D5).** `UI_ART_RULES.md`: the 3D-office framing; wallpapers become 4:3 (1440 x 1080, from ChatGPT's 1536 x 1024); posters leave the ChatGPT prompts and the delivery list (the office has no poster); a new section on the 2D layers over the office (the PC frame, rendered from the Blender CRT by preference, the speech bubble, the paper faces); the 3D props the Blender side makes (the scanner).
6. **The 2D booth is gone from the text (D6).** Pixels-per-unit and world-unit instructions, "the booth rework", the `traveller.png` sentence, "Claude checks the booth", the Visitor window and Appendix E's poster question are removed or replaced. `coverage.json` `officeArt` lists the 4:3 wallpapers and marks the posters deferred; its canvas gains the measured desk view.

**Open questions for Saleh (v2.1)**

1. **The desk hides less than thought.** Measured at the default traveller anchor, the NEXT sign's top edge cuts the figure at the **waist** (canvas y 760), not mid-chest: the whole chest shows. The brief still keeps identity above mid-chest (y 630), because papers held up to read (piece 10) cover the figure's sides from there down and the Look menu's "< Back" button sits over the neck while it is open. Keep that margin?
2. **The tagari.** Worn high on the chest it is less typical than at the hip, where the research and v2 put it. Accept, or pick another Greek 1975 item (none passed the checks: Appendix F)?
3. **The kazuki and the women's navy cap** add headwear to two looks that had none (both historically attested: Appendix F). Accept?

## What changed since v1

- **Style.** Cute, soft 2D anime-style characters (since v2.2) with flat cel colours and very little texture, the same for travellers and premades, and **neutral, even lighting** (the game stands the flat figure in its 3D office and tones it to the room's light). v1 asked for painterly shading lit from the top-left.
- **File names follow the game's key grammar** (section 8). Hair and facial hair come out in five baked colours (Claude makes them from your medium-brown drawing). The Future uses the same pattern as every other era (`outfit_m_china_future`), and all Future travellers share one neutral hairstyle.
- **A hair-back layer.** Hair that shows beside the neck behind the shoulders is split off by Claude and drawn behind the body, so it never covers a collar or shawl.
- **All 68 data fixes and all 22 practicality fixes from the v1 review are in** (Appendices A to C say how, and the few that were adapted and why).
- **Leak items.** Every look now names, per gender, the one item a liar from that place can "leak" onto a disguise. Draw those extra clearly.
- **Batches follow the game's days**, so the places that appear first get art first. The pilot is Periclean Athens (day 1) instead of Victorian London.
- **Premade characters are named**: ten real, long-dead people who are not rulers, five women and five men (section 11), made in their own ChatGPT Project.
- **After the v2 review** (Appendix E): every leak item stands on its own; purses, fans and cases that hung from an outfit's belt are now part of that outfit; every prompt states the size; a style card keeps the look steady; each DO NOT DRAW line holds only likely confusions (caricature and hate-symbol checks moved to a review line outside the block); the premades wear their place's Costume Guide item.

## 1. How characters work in the game

- **Two kinds of character.** *Generated* travellers are built from layers, so the game can make thousands of different people. *Premade* characters (named historical people) are drawn whole, with four expressions. Both kinds share one style (section 2), so they look like one cast.
- **The layers, bottom first:** hair back, body, outfit, head, facial hair, hair, headwear, accessory. You draw seven kinds of image; Claude makes the hair-back layer from your hair drawing.
- **Every layer is a full 1024 x 1536 image with the figure in exactly the same spot**, so the game stacks them with no adjusting.
- **Every country and era has its own look.** Honest travellers wear only their claimed place's look. A liar's disguise leaks exactly **one** item from their real home, like a Victorian top hat on someone claiming to be from Edo Japan. The player picks that garment by name in the traveller wheel's Look menu and compares it with a reference book on the office PC, the **Costume Guide**, which lists one "leak item" per gender for each place. That is why every look has LEAK ITEMS: they must be recognisable at the desk's size (below), different from every other place's, and make sense on their own over any other place's clothes.
- **A worn item can hide another.** A turban, hood or veil that hides all the hair (its PAIR line says so) means the game draws no hair under it, so its edge must come down to the hairline.
- **Skin and hair colour never give anyone away.** The game picks them from the place the traveller *claims* to be from (and turns hair grey from age 60). Only culture (clothes, hairstyle, headwear, accessories) can be a clue.
- **The traveller's job never changes the look.** Soldiers, scientists and merchants all wear civilian dress.

### Where the player sees a traveller

The game is set in the art side's 3D office. A traveller is the stack of flat 2D layers, standing behind the desk as a billboard 1.8 m tall (soles to the top of the head), turned to face the camera and toned to the room's light. Sizes are measured at the default traveller anchor, at 1920 x 1080 (and 1280 x 720; they scale with the screen height only).

| Where | What the player sees | Size at 1920 x 1080 (1280 x 720) |
|---|---|---|
| **The office view**, from the moment the traveller is called until the decision | The figure from the headroom (tall hats) down to the **waist**: the NEXT sign and the desk hide everything below (canvas y 760). Papers held up to read (from piece 10) cover the figure's sides from about mid-chest (canvas y 630) down, and while the wheel's Look menu (or another sub-menu) is open, its "< Back" button sits over the neck and collar. | The head (top of the head to the chin) about **58 px** tall and 44 px wide (39 x 30); top of the head to the shoulders 85 px (57); the shoulders 121 px wide (80). One canvas pixel is about 0.36 screen pixels (0.24): the 2-3 px outline shows as about 1 px, and a detail thinner than a finger (about 12 canvas px) is lost. |
| **The passport photo**: on the passport paper on the desk, on its scanned copy on the PC, and (from piece 10) on the paper held up close to read | The head-and-shoulders crop (362, 215) to (662, 590) of the same stack: head, neck, shoulders and upper chest. | On the desk paper about 48 x 25 px, lying flat (not for reading); on the scanned copy in the open PC frame about 60 x 75 px (40 x 50); on a held paper about 95 x 119 px (63 x 79), and up to 134 x 167 px beside the PC frame. |
| **The Look menu** on the traveller wheel | No picture: every worn garment is listed by its label ("sakkos snood", "petasos hat"); choosing one puts it into the PC's compare bar, to compare with the Costume Guide. | – |

**Nothing shows the whole figure.** The Visitor window of the first design was dropped (the piece-4 amendments, K1). Still draw every layer as a whole figure, down to the feet, on the same 1024 x 1536 canvas: the art contract is unchanged, and a walk-in or a moved traveller anchor may show more later. But **nothing that identifies a look may rely on the lower body**: every LEAK ITEM and everything a MUST READ line names sits on the head, face, neck, shoulders or upper chest, in bold shapes that read when the head is only 58 px tall (a leak item is at least a palm across).

## 2. The look

The reference is the 2D customers who walk up to the counter in *ReStory: Chill Electronics Repairs*: cute, soft 2D anime-style characters with a rounded, illustrative look, standing in a detailed 3D shop. That is this game's set-up too (flat 2D travellers in a 3D office), and the art side already shades the office's desk props with an anime cel shader (`NOPE/Desk Anime`: `ArtDeliverables/TimeDesk/ImportedOffice/DeskClean/ANIME_SHADER.md`). So the characters follow ReStory's customers: anime faces, flat cel colours, almost no texture. Never name any game in a prompt; Block A describes the look in words, and a named game pulls ChatGPT towards that game's look.

- **One cast, two kinds.** The style is the same for both kinds of character: the generated travellers, built from layers (Block A), and the premades, drawn whole (Block P, section 11). Both blocks carry the same STYLE text, written once by `tools/build_v2.py`, and both say the two kinds must look like one cast: the same eyes, line, shading and colour treatment, so a premade never stands out from a generated traveller at the desk.
- **Faces and hair:** cute, soft and anime-style: expressive anime eyes (larger than realistic, with one simple highlight), a small, simple nose and mouth, clean rounded face shapes, and hair in clean stylised shapes and locks. Adults who look their age, never chibi or childlike.
- **Proportions: the guide's, exactly.** The head size and body proportions are fixed by the figure guide and the game's `LookCanvas` (section 3; the figure is about seven and a half heads tall): the mannequins, every layer, the premades and the passport crop depend on them. The style changes the face and the rendering, never the head size or the body.
- **Reads small:** every look stays recognisable when the head is 58 px tall (section 1). The anime face helps: big, clear eyes and simple, bold shapes survive at that size, where fine realistic features blur.
- **Respect:** never a caricature of any people. The five skin tones stay exactly their swatches (the style never lightens a darker skin tone), and the four faces a to d stay clearly different in cheeks and chin, eye shape, brows and nose (section 7), because anime styling tends to give everyone the same face. Every culture gets this same style: no exoticised styling, and no cosplay or anime-costume cliches.
- **Textures:** flat cel colours: each colour area has one hard-edged shade tone (the same hue, about 20% darker, with a crisp edge, never blended) and at most one small highlight. No fabric grain, brush or paper texture, noise or photo detail. Patterns (stripes, checks, borders, embroidery) are clean, bold, flat shapes that still read when the head is 58 px tall (section 1).
- **Outline:** a clean, even dark-brown line (#3B2A20, 2 to 3 px) around every piece and its main folds.
- **Lighting: neutral and even.** Plain white light from the front. Shading only shows form (under the chin, inside folds, under a brim), the same on both sides. No light direction, rim light, glow, cast or ground shadow, and no warm or cool tint. The game stands the flat, unlit figure in its lit 3D office and tones it to the room's light (a warm grey tint today), so any light baked into the drawing would fight the room's.
- **The room:** `office_style_reference.png` shows the office the figures stand in. ChatGPT matches its colour range and contrast (so a figure never looks pasted in), never its lighting, and never draws the room.
- **Colour:** gently muted, natural period dyes. Nothing neon.
- **Pose and expression:** front view, standing straight, facing the viewer, arms relaxed slightly away from the body, hands open and empty, neutral expression, eyes looking at the viewer. The premades' other three expressions (prompt 9.11) are anime-expressive but dignified, because they are real historical people. No manga symbols on any face (sweat drops, anger marks, blush lines, sparkles, tears, speed lines).
- **One style across about 40 chats.** After the pilot is approved, Claude makes `style_card.png`: the approved Athens outfits in greyscale on the mannequin, plus a small swatch of folds showing their one hard-edged shade tone, a pattern band and the outline weight. Every garment prompt attaches it as a style reference only, and so does the premade prompt (9.10), so line weight, shading depth and pattern scale stay the same in every chat and in both Projects.

## 3. Canvas, pivot and the figure guide

Every layer (and every premade image) uses the same canvas and the same figure position. These numbers are the game's (`LookCanvas` in the code), so they never change.

| What | Value |
|---|---|
| Canvas | 1024 x 1536 px, portrait (ChatGPT's tall size) |
| Centre line | x = 512 |
| Landmarks (from the top) | top of head 260, chin 424, shoulders 500, waist 760, hips 900, knees 1170, soles of the feet 1490 |
| Safe area | x 120 to 904; nothing may cross it. Tall hats and hair may rise into the headroom above y = 260 |
| Pivot | the feet: x = 512, y = 1490 (the game's pivot is 3% up from the bottom edge) |
| Passport photo | crop (362, 215) to (662, 590), 4:5, head and shoulders; a tall hat is cut by the frame, which is fine |
| Office view (section 1) | the figure from the headroom to the waist (y 760); the head about 58 px tall at 1920 x 1080 (39 px at 1280 x 720), one canvas pixel about 0.36 screen pixels |
| Read zone | everything that identifies a look sits above mid-chest (y 630): the head, face, neck, shoulders and upper chest |
| Delivered file | RGBA PNG, 1024 x 1536, **untrimmed** (Claude never crops layers; the stack depends on it) |

ChatGPT can't measure pixels, so the prompts refer to the guide's lines instead. Claude re-centres and rescales every image onto these landmarks before cutting it out, so a figure that comes out slightly big or off-centre is not a reason to reject it.

## 4. Why green and magenta

ChatGPT can't line up separate transparent images reliably. So every piece is drawn **on top of a flat magenta mannequin, on a flat green background**. Claude's script then lines each image up with the mannequin (ChatGPT always redraws the whole picture, so everything shifts a little), removes the green and magenta, and leaves just the piece in the right place. That's why characters must never use bright green, magenta, pink or purple, and why nothing may be shaded onto the magenta: a shadow painted on the mannequin turns into purple-grey pixels that either vanish (the hat looks pasted on) or stay as a dark smear that travels with every leaked hat. Claude adds any contact shade in processing.

## 5. Setup in ChatGPT (once)

1. Create a ChatGPT **Project** called "Time Sorter Characters".
2. Paste **Block A** (below) into the Project's instructions.
3. Take `office_style_reference.png` once: in Unity, open the game's office, set the Game view to 1920 x 1080, and save a screenshot of the office with no traveller at the desk. It is a style reference only (section 2).
4. Keep it, the figure guide, and later the two mannequins and the style card, in a folder on your PC. In every message, use the paperclip to attach exactly the file(s) named on the prompt's "Attached:" line, even if they are also in the Project files. ChatGPT's drawing tool only reliably uses images attached to the message. Don't add finished pieces to the Project files, because ChatGPT copies details from them into other looks.
5. Every prompt below ends with a size line ("Portrait, 1024 x 1536, the same framing as the attached image."). Keep it: without it ChatGPT often answers with a square image, which the importer rejects and which costs a regeneration.
6. Start a new chat for each PAIR block (each country and era). Long chats drift in style and mix up looks.
7. Never ask ChatGPT to fix or tweak an image it made. If something is wrong, press Regenerate, or send the same prompt again with the original mannequin attached. Every edit redraws the whole picture, and the figure drifts further each time.
8. Save every image with ChatGPT's own download button on a computer, so the file is a 1024 x 1536 PNG. Never screenshot, and never save from the phone app: JPG files blur the green and magenta edges Claude removes.
9. **Send each place to Claude as soon as it is done** (its 5 to 9 files), even in the middle of a batch, and wait for Claude's go-ahead on the first place of every batch. The batches are only the planning unit.
10. Premade characters are real people, so they are made in a **second Project**, "Time Sorter Premades", with its own instructions, Block P (section 11).

### Block A: paste into the Project instructions

```
You are drawing characters for "Time Sorter", a game set in a 3D office in which the player is a clerk at a time-travel border desk, checking the papers of travellers from many countries and eras. The travellers are flat 2D figures standing at the desk. Every character is built from separate layers (body, head, outfit, facial hair, hair, headwear, accessory) that the game stacks on top of each other, so every layer must line up with the same figure.

IN THE GAME
- The characters are flat 2D figures that stand behind the clerk's desk in the 3D office, facing the player; the game places them in the room and tones them to its light, so draw them flat and evenly lit.
- The desk hides every figure below the waist, and the head shows small on screen. So everything that tells where a character comes from (headwear, hair, face, beard, collar, necklace, shoulders and upper chest) must be bold and clear. Still draw the whole figure, down to the feet.
- When I attach office_style_reference.png, it is a STYLE REFERENCE ONLY: a screenshot of the game's office. Match its colour range and contrast so the character sits in that room. Never draw the room, the desk, any furniture or the room's lighting: the background stays flat green.

STYLE
- Cute, soft 2D anime-style characters, like the customers of a cozy shop-counter game: expressive anime eyes (larger than realistic, with one simple highlight), a small, simple nose and mouth, clean rounded face shapes, and hair drawn in clean stylised shapes and locks. Every character is an adult who looks their age: never chibi, never childlike.
- The head size and body proportions are fixed by the attached guide, mannequin or base figure (adult proportions, about seven and a half heads tall; see CANVAS). The style changes only the face and the rendering: never enlarge the head, and never shorten or lengthen the body.
- Faces stay individual: give each face the face shape, eye shape, brows and nose its prompt describes, never one anime face for everyone. Paint every skin tone exactly as given, never lighter. Every culture is drawn in this same style: never a caricature, never an exoticised version of a people.
- One cast: the game's layered travellers and its named historical characters are drawn in two separate Projects but must look like one cast, with the same eyes, line, shading and colour treatment, so neither kind ever stands out from the other at the desk.
- Everything reads small: in the game the head is only about 58 px tall on screen, so keep the eyes, brows, mouth, hair shapes and headwear simple, bold and clear.
- Flat cel colours: each colour area has ONE hard-edged shade tone (the same hue, about 20% darker, with a crisp edge and no soft blending) and at most one small highlight. No fabric grain, brush or paper texture, noise, gradient or photographic detail. Draw patterns (stripes, checks, borders, embroidery) as clean, bold, flat shapes.
- A clean, even dark-brown outline (#3B2A20, 2-3 px) around every piece and its main folds.
- Neutral, even lighting: plain white light from the front. Shade only to show form (under the chin, inside folds, under a brim), the same on both sides. No light direction, no rim light, no glow, no cast shadow, no ground shadow, no warm or cool tint.
- Gently muted, natural period colours. Nothing neon.
- No manga symbols: no sweat drops, anger marks, blush lines, sparkles, tears or speed lines. A face shows feeling only through its eyes, brows and mouth.
- Front view: standing straight, facing the viewer, arms relaxed slightly away from the body, hands open and empty, neutral expression, eyes looking at the viewer.
- When I attach a STYLE REFERENCE image, match its line weight, shading and pattern scale, and never copy its clothing.
- Respectful and historically grounded. No caricature or stereotype of any people or culture, and no likeness of any real person.
- Civilian clothing only. No military uniforms or armour, weapons, flags, insignia, badges, royal regalia, political, regime or hate symbols, and no religious vestments or holy symbols.

CANVAS (every image)
- Portrait, 1024 x 1536 px. The figure stays exactly where the attached guide, mannequin or base figure puts it: same top of head, chin, shoulders, waist, hips, knees and feet, same centre line. Never move, resize, turn or re-pose it.
- Background: one flat pure green #00FF00 everywhere outside the figure. No gradient, texture, floor, shadow, vignette or frame. Never draw a checkerboard or "transparent" pattern.
- Leave green on all four sides: nothing touches an edge. Tall hats and hair may rise into the space above the head.
- Never use bright green, lime, magenta, pink, purple or violet anywhere in the clothing, hair or items, not even muted. Where a look asks for purple, paint deep wine red. Where it asks for pink or rose, paint dusty coral or salmon. Where it asks for green, paint dark olive, moss or bottle green. Paint jade as a dark, dull grey-green stone with a strong dark outline. Natural skin and lip colours on faces and bodies are fine.
- Paint every fabric fully opaque, even veils, gauze, muslin and stockings, and never cut holes through it. Magenta, green or skin must never show through anything. Where I say "light" or "fine" fabric, paint a thin, light, solid fabric.
- No text, letters, numbers, logos, watermarks or signatures anywhere. Bands called "tiraz" or "script" are abstract embroidered patterns, never real or fake letters. Coins are plain discs with no face, letters or marks.

MANNEQUIN RULE
- When I attach a magenta mannequin, keep it as it is: flat magenta, same shape, same position, same darker-magenta face marks. Draw ONLY the item I ask for, on top of it, fitted to it. Do not draw skin, a face, hair or anything else I did not ask for.
- Never shade, darken or cast a shadow onto the magenta mannequin. Shade only inside the item you draw.

HAIR COLOUR
- Draw all natural hair and facial hair in one medium brown; the other colours are made later. A wig is costume: draw a wig in the colour I describe.
- Anything in the hair that is not hair (ties, cords, ribbons, pins, combs, nets) must be clearly a different colour from brown: white, silver or blue, or black where the look says so. Never red. Draw gold ornaments bright yellow with a dark outline.

When I send a PAIR block, reply only READY and draw nothing. After that, one image per reply.
```

## 6. The layers

| Layer | What it contains | What it must NOT contain | How many |
|---|---|---|---|
| Body | The figure without the head: neck up to the chin line, arms, open hands, legs, bare feet, plain light-grey undergarment (a strapless bandeau for women and mid-thigh shorts) | Head, hair, real clothes | 2 genders x 5 skin tones (tones 2 to 5 recoloured from tone 1 by Claude) |
| Head | Bald head with ears and face, in the body's skin tone, no makeup | Hair, beard, anything below the chin | 2 genders x 5 skin tones x 4 faces |
| Outfit | All clothing and footwear for one place, with the fastenings and the purses, cases, fans and cords that hang from its own belt or sash | Face, hair, headwear, the accessory, jewellery, badges (plain buttons, clasps and pins are fine); nothing above the chin line | 1 per gender per place |
| Facial hair | Beard, moustache or whiskers only | Face, hair | Men only, when the look has one |
| Hair | The hairstyle only, as seen from the front with any hat or veil taken off, fitted to the bald head (shaved parts stay magenta) | Face, headwear; a plait, braid or tail that hangs down the back (the body hides it) | 1 per gender per place |
| Hair back | Made by Claude: the part of the hair that shows beside the neck behind the shoulders, moved behind the body | (you never draw it separately) | Only for loose long hair, a wide wig or a spread curtain of plaits |
| Headwear | Hat, cap, veil, hood or headdress, sized to sit over a full head of hair; eyes, nose and mouth stay visible. One that hides all the hair (its PAIR line says so) comes down to the hairline, because the game draws no hair under it | Hair, face | When the look has one |
| Accessory | One worn item that stands on its own on any outfit and hairstyle and reads at the desk's head size (section 1): a necklace, collar or pendant on its own chain or cord, a belt, sash or girdle and what hangs from it, a strap bag, a shawl or scarf, glasses, headphones, large earrings | Anything held in the hands; anything that hangs from, is tucked into, pins, closes or sits under another layer's item (a sash, belt, cloak, collar, pocket, hairstyle); rings, single or small earrings, small pins or badges | When the look has one |

A liar's leaked item is drawn over the claimed look, so every item must make sense on its own, on anyone. An item is never an absence ("clean-shaven", "bareheaded"): only real items can leak. For a leak item this is a hard rule: a brooch that pins "the cloak" or a pendant hanging from "the sash" would float in the air over another place's clothes. A leak item also sits where the desk never hides it, on the head, face, neck, shoulders or upper chest (section 1): a belt, a sash round the waist or a bag at the hip can be an ordinary accessory, never a leak item.

## 7. Bodies, heads, skin tones, faces and hair colours

**Skin tones** (5): 1 very light `#F1D3C0`, 2 light `#E0B394`, 3 medium olive `#C39A6B`, 4 brown `#94653F`, 5 deep brown `#5C3A24`. Every culture uses several tones; the game picks them by weights per place, so a skin tone never points to a country. ChatGPT gets the swatches in prompt 9.2, and Claude recolours bodies 2 to 5 from the approved tone-1 body to the same swatches, so heads and bodies agree.

**Faces** (4), chosen by the traveller's age. In the anime style (section 2) faces easily come out alike, so each face has its own cheeks and chin, eye shape, brows and nose. All four keep face a's head outline and feature positions (within a few pixels: Batch 2), because the beards, caps and glasses are fitted to them, so they differ inside that outline:

| Face | Age | Drawn as |
|---|---|---|
| a | 18 to 34 | a young adult in their 20s: soft, round cheeks and chin; large, round eyes; gently curved brows; a small, short nose |
| b | 18 to 34 | a different young adult in their 20s: slimmer cheeks and a more defined chin; narrower, longer eyes; straight, thicker brows; a longer, straighter nose |
| c | 35 to 59 | middle-aged, 40s to 50s: fuller cheeks and a firmer, squarer chin; steady eyes with a small line at each outer corner; heavier, lower brows; a broader nose; a few lines on the forehead and beside the mouth |
| d | 60 and over | elderly, 60s and up: softer, slightly hollow cheeks and a softer jawline; smaller eyes under heavier lids, with wrinkles at the corners; thin, lighter eyebrows; a longer nose; wrinkles on the forehead and cheeks (the game also turns the hair grey) |

**Hair colours** (5, baked by Claude, never drawn by ChatGPT): black, brown, blond, red, grey. You draw every natural hairstyle and beard once, in medium brown; Claude masks any ornaments and bakes the five colours into five files. Wigs are costume and keep the colour you draw (one file, no variants).

For Saleh only, never for ChatGPT (the game's starting weights, to review; they are never a clue): Egypt and Iraq mostly tones 3 to 4 and black hair; Greece and Italy tones 2 to 3, black or brown hair; China and Japan tones 2 to 3, black hair; Britain and Germany tones 1 to 2, brown or blond hair, some red.

## 8. File names, delivery and where files go

**You save** each image with exactly the listed name into `ArtDeliverables/TimeDesk/Characters/Raw/<batch folder>/`, as the green-and-magenta original. **Claude makes** the cut-out, game-ready files from it at `Assets/Art/Characters/Resources/Characters/<key>.png`, and the game loads them by name. A finished file replaces its placeholder with no code change.

| You draw (raw file) | Claude delivers (game keys) |
|---|---|
| `base_[m/f]_skin1_facea.png` (whole figure) | `body_[m/f]_skin1` to `body_[m/f]_skin5` (skin 1 cut from it; skins 2 to 5 recoloured from it to the section-7 swatches) and `head_[m/f]_skin1_facea` |
| `base_[m/f]_skin[2-5]_facea.png` (whole figure, new skin tone) | `head_[m/f]_skin[N]_facea` (Claude keeps only the head) |
| `head_[m/f]_skin[1-5]_face[b/c/d].png` (whole figure, new face) | `head_[m/f]_skin[N]_face[b/c/d]` |
| `outfit_[m/f]_[country]_[era].png` | `outfit_[m/f]_[country]_[era]` |
| `hair_[m/f]_[country]_[era].png` | `hair_[m/f]_[country]_[era]_[black/brown/blond/red/grey]` (5 files; a wig: 1 file with no colour), plus `hairback_...` in the same colours when the hair shows behind the shoulders |
| `facialhair_m_[country]_[era].png` | `facialhair_m_[country]_[era]_[colour]` (5 files) |
| `headwear_[m/f]_[country]_[era].png` | `headwear_[m/f]_[country]_[era]` |
| `accessory_[m/f]_[country]_[era].png` | `accessory_[m/f]_[country]_[era]` |
| `outfit_[m/f]_[country]_future.png` | `outfit_[m/f]_[country]_future` |
| `hair_[m/f]_neutral_future.png`, `facialhair_m_neutral_future.png` | `hair_[m/f]_neutral_future_[colour]`, `facialhair_m_neutral_future_[colour]` (shared by every Future place) |
| `outfit_[m/f]_neutral_future.png` | `outfit_[m/f]_neutral_future` (the present's clothes, since v2.3) |
| `accessory_[m/f]_neutral_future_[item].png` | `accessory_[m/f]_neutral_future_[item]` (the 2150 accessory kit; items: `lenses earpiece badge display`) |
| `premade_[id]_[expression].png` | `premade_[id]_[neutral/happy/angry/worried]` |

- Countries: `egypt iraq greece italy china japan britain germany`. Eras: `ancient medieval earlymodern industrial modern future`. Gender: `m` or `f`. Lower case, no spaces, underscores only.
- Only what the look has: no file for "none" (no facial hair, no headwear, no accessory).
- `outfit_[m/f]_neutral_future.png` is the base of the Future batch and, since v2.3, a game file: the present's clothes. Claude's working files are not game keys: `mannequin_[m/f].png`, `style_card.png`, `premadebase_[m/f]_skin[N].png`.
- The full list of game keys, with the raw file each comes from, is in `coverage.json`.

## 9. The prompts

Replace the parts in [square brackets]. For each place, start a new chat, send its PAIR block first (they're in the batch sections), then use prompts 9.4 to 9.8 for each file in its list, one message per file. Every prompt ends with the size line; keep it.

### 9.0 Starting a place

```
Attached: office_style_reference.png (STYLE REFERENCE ONLY: the game's office; match its colour range and contrast, never draw the room).
[paste the PAIR block]
Read this and reply only READY. Do not draw anything yet.
```

Attach the office screenshot here once per chat; ChatGPT keeps it in mind for the rest of that place's images.

### 9.1 Base figure (Batch 1)

```
Attached: character_guide_v2_1024x1536.png (the Time Sorter figure guide) and office_style_reference.png (STYLE REFERENCE ONLY: the game's office; match its colour range and contrast, never draw the room).
Draw a BASE FIGURE: a [man / woman] in their 20s, skin tone [1 very light], bald (no hair at all, smooth scalp), ears visible, no makeup, neutral expression, looking at the viewer.
Face: soft, round cheeks and chin; large, round eyes; gently curved brows; a small, short nose.
Follow the guide's pose, proportions and landmark lines: the top of the head on the TOP OF HEAD line, the chin on the CHIN line, the soles of the feet on the bottom line, centred on the dashed centre line.
Clothing: only [plain light-grey fitted shorts ending mid-thigh / a plain light-grey strapless bandeau covering only the bust, and plain light-grey fitted shorts ending mid-thigh]. Bare feet.
Remove all guide lines, labels and the grey silhouette. Flat pure green #00FF00 background.
Portrait, 1024 x 1536, the same framing as the attached guide.
```

### 9.2 Skin tone variant (Batch 2)

```
Attached: my approved base figure base_[m / f]_skin1_facea.png.
Keep everything exactly the same (pose, face, proportions, position, clothing, background) and change ONLY the skin tone to [2 light, #E0B394 / 3 medium olive, #C39A6B / 4 brown, #94653F / 5 deep brown, #5C3A24].
Portrait, 1024 x 1536, the same framing as the attached image.
```

Claude keeps only the head from these; the bodies for tones 2 to 5 are recoloured from your approved tone-1 body to the same swatches, so they match the mannequin exactly.

### 9.3 Extra face (Batch 2)

```
Attached: my approved base figure base_[m / f]_skin[N]_facea.png.
Keep everything exactly the same (pose, body, skin tone, grey clothing, position, size, background) and change ONLY the face to: [b: a different young adult in their 20s: slimmer cheeks and a more defined chin; narrower, longer eyes; straight, thicker brows; a longer, straighter nose / c: middle-aged, 40s to 50s: fuller cheeks and a firmer, squarer chin; steady eyes with a small line at each outer corner; heavier, lower brows; a broader nose; a few lines on the forehead and beside the mouth / d: elderly, 60s and up: softer, slightly hollow cheeks and a softer jawline; smaller eyes under heavier lids, with wrinkles at the corners; thin, lighter eyebrows; a longer nose; wrinkles on the forehead and cheeks]. Still bald, ears visible, no makeup, neutral expression, looking at the viewer. Do not move, turn or resize the head, and keep its outline: only the cheeks and chin may be a little rounder or more defined, as described.
Portrait, 1024 x 1536, the same framing as the attached image.
```

Each image shows the whole figure; Claude cuts the head out, lines it up on the face-a marks and matches its colour to the body (Batch 2 says how).

### 9.4 Outfit

```
Attached: mannequin_[m / f].png (draw on this) and style_card.png (STYLE REFERENCE ONLY: match its line weight and shading; do not copy any clothing).
From the PAIR block, draw ONLY the [man's / woman's] OUTFIT (all clothing and footwear) on the magenta mannequin, fitted to its body.
Do not draw the face, hair, facial hair, headwear, accessory or any jewellery. The head stays magenta and uncovered: keep every collar and garment below the chin line, because anything drawn over the head is hidden by the head layer. Any part of the body the outfit does not cover stays magenta.
Draw the whole outfit down to the shoes. Make the collar, shoulders and chest especially clear: in the game the desk hides the figure below the waist.
Portrait, 1024 x 1536, the same framing as the attached image.
```

### 9.5 Hair

```
Attached: mannequin_[m / f].png (draw on this) and style_card.png (STYLE REFERENCE ONLY: match its line weight and shading; do not copy any clothing).
From the PAIR block, draw ONLY the [man's / woman's] HAIR on the mannequin's bald head, in medium brown (unless the PAIR block says it is a wig). Leave any shaved parts of the scalp magenta.
Draw the hair as it looks from the front with any hat or veil taken off. If the PAIR block says the hair is hidden under headwear or pinned up, draw it short or pinned up close to the head (a neat low bun, or braids pinned up), never hanging below the jaw, and do not draw the headwear. A plait, braid or tail that hangs down the back is hidden by the body: leave it out.
Hair must never cover the eyes, nose or mouth. Draw nothing else.
Portrait, 1024 x 1536, the same framing as the attached image.
```

### 9.6 Facial hair

```
Attached: mannequin_m.png (draw on this) and style_card.png (STYLE REFERENCE ONLY: match its line weight and shading; do not copy any clothing).
From the PAIR block, draw ONLY the man's FACIAL HAIR on the mannequin's face, using the darker magenta face marks to place it, in medium brown. Leave the rest of the face magenta. Draw nothing else.
Portrait, 1024 x 1536, the same framing as the attached image.
```

### 9.7 Headwear

```
Attached: mannequin_[m / f].png (draw on this) and style_card.png (STYLE REFERENCE ONLY: match its line weight and shading; do not copy any clothing).
From the PAIR block, draw ONLY the [man's / woman's] HEADWEAR on the mannequin's head, sized to sit over a full head of hair: a little larger all round than the bald head, never tight to the scalp. If the PAIR block says it hides all the hair, bring its front edge right down to the hairline so no bald scalp would show. Headwear that covers the head (a hood, veil or head-shawl) continues down the sides of the neck onto the shoulders. The eyes, nose and mouth stay fully visible. Draw only what is seen in front of the body, and leave out anything that would hang down the back. Draw nothing else.
Portrait, 1024 x 1536, the same framing as the attached image.
```

### 9.8 Accessory

```
Attached: mannequin_[m / f].png (draw on this) and style_card.png (STYLE REFERENCE ONLY: match its line weight and shading; do not copy any clothing).
From the PAIR block, draw ONLY the [man's / woman's] ACCESSORY in its natural worn position on the body. It is worn, never held, and it stands on its own: do not draw a belt, sash, cloak or collar for it to hang from unless the accessory line names one as part of it. Make it big and bold enough to recognise when the head is shown only about 40 to 60 px tall: a clear shape at least a palm across, with no detail that matters thinner than a finger. Draw only what is seen in front of the body, and leave out anything that would hang down the back. Draw nothing else.
Portrait, 1024 x 1536, the same framing as the attached image.
```

### 9.9 Future culture outfit (Batch 11)

```
Attached: my approved outfit_[m / f]_neutral_future.png.
Keep the magenta mannequin, the green background and the outfit's cut, fit and outline exactly. Restyle ONLY the [man's / woman's] outfit's fabric, colours, seams, trims and patterns with the motifs in the PAIR block. Where a motif describes a shape (sleeves, drapes, capes, wraps, collars), suggest it with panels, seams and trim lines on the same cut. Clothing and shoes only: no hat, cap, headband, hood, glasses, visor, jewellery, neck ring or bag, nothing above the chin, nothing that glows, nothing see-through, no holes cut through the fabric, no letters.
Portrait, 1024 x 1536, the same framing as the attached image.
```

### 9.10 Premade character (Batches 3, 6 and 8)

```
Attached: premadebase_[m / f]_skin[N].png (the lined-up base figure), style_card.png (STYLE REFERENCE ONLY: match its line weight and shading; do not copy any clothing) and office_style_reference.png (STYLE REFERENCE ONLY: the game's office; match its colour range and contrast, never draw the room).
Dress this exact figure as a complete PREMADE CHARACTER, keeping its pose, size and position, with a new face, hair, clothing, headwear and accessories: [the character's description from its batch section]. Hands stay open and empty. Neutral expression, looking at the viewer. Flat pure green #00FF00 background.
Do not draw: [the character's "do not draw" line].
Portrait, 1024 x 1536, the same framing as the attached image.
```

### 9.11 Premade expression

```
Attached: my approved premade_[id]_neutral.png.
Keep everything exactly the same (pose, clothing, position, background) and change ONLY the facial expression to [happy: a warm, open smile with softly curved, smiling eyes / angry: lowered, frowning brows, narrowed eyes and pressed lips / worried: raised inner brows, wide, uncertain eyes and a small, tight mouth]. Make it clear and expressive in the anime style, but dignified: no comic distortion of the face and no manga symbols (sweat drops, anger marks, blush lines, tears).
Portrait, 1024 x 1536, the same framing as the attached image.
```

Claude takes only the face from each expression image and pastes it onto your approved neutral image. Don't reject an expression because its clothes changed; reject it only if the head moved or changed shape.

## 10. What NOT to draw

These hold for every image, on top of each PAIR block's own DO NOT DRAW list.

- **No text of any kind:** no letters, numbers, logos, brand names, signatures, watermarks, labels, and no fake writing. "Tiraz" and "script" bands are abstract patterns. Coins are plain discs.
- **No insignia or regime symbols:** no flags, national or party emblems, stars on caps, eagles, rising-sun rays, the imperial chrysanthemum, five-claw dragons, rank badges, medals, company badges, crests that read as a warrior's retainer, and never any hate symbol.
- **No royal regalia or religious vestments:** no crowns, cobras, false beards, sceptres; no priest's, monk's or nun's garments, no holy symbols or deity motifs.
- **No uniforms, armour or weapons.** Every traveller is a civilian.
- **No skin-colour tells:** never tie a skin tone or hair colour to a culture, never tint or shade skin inside an outfit, hair or headwear layer, never draw skin anywhere except on the base figures and heads, and never draw makeup on the heads.
- **No held props:** hands stay open and empty.
- **No caricature:** no stereotyped features, no "Hollywood" versions of a culture, no ragged or comic poverty.
- **No manga symbols:** no sweat drops, anger marks, blush lines, sparkles, tears or speed lines on any face.
- **No colour the cut-out eats:** no bright green, lime, magenta, pink, purple or violet; nothing see-through; no holes cut through fabric (lattice, perforation and laser-cut motifs are printed or stitched on solid cloth); no checkerboard.
- **No lighting effects:** no glow, rim light, cast or ground shadow, or coloured mood light, and no shade painted onto the magenta mannequin.
- **No room:** nothing from the office screenshot (no walls, desk, furniture, floor, window or its lighting); the background is always flat green.
- **Nothing hanging behind the body** on hair, headwear and accessory layers (it can't be seen from the front and it lands in front of the outfit).

## 11. Premade characters (set-up and cast)

Premade characters are drawn **whole**: one finished image per expression (neutral, happy, angry, worried), on the lined-up base figure `premadebase_[m/f]_skin[N].png` that Claude makes after Batch 2 (the approved recoloured body with the approved head, aligned to the landmarks). Claude registers every premade image to the same landmarks as the layers before delivering it, so its place behind the desk and the passport crop match the generated travellers. The game swaps the four images as they talk.

**A second Project.** Block A forbids any likeness of a real person, draws every hair in medium brown and works on a magenta mannequin; premades are real people, drawn whole, in their real hair colour. So they get their own Project:

1. Create a second ChatGPT Project called **"Time Sorter Premades"**.
2. Paste **Block P** (below) into its instructions.
3. Start one new chat per character inside it. Every chat inherits Block P, so nothing is pasted by hand.

All ten lived and died centuries ago, none was a ruler, and no photograph of any of them exists.

### Block P: paste into the premades Project's instructions

```
You are drawing the named historical characters of "Time Sorter", a game set in a 3D office in which the player is a clerk at a time-travel border desk. The travellers are flat 2D figures standing at the desk. Each character is ONE whole image: a respectful portrait based on period descriptions, not a photo likeness. Every one of them lived and died centuries ago, and none was a ruler.

IN THE GAME
- The characters are flat 2D figures that stand behind the clerk's desk in the 3D office, facing the player; the game places them in the room and tones them to its light, so draw them flat and evenly lit.
- The desk hides every figure below the waist, and the head shows small on screen. So everything that tells where a character comes from (headwear, hair, face, beard, collar, necklace, shoulders and upper chest) must be bold and clear. Still draw the whole figure, down to the feet.
- When I attach office_style_reference.png, it is a STYLE REFERENCE ONLY: a screenshot of the game's office. Match its colour range and contrast so the character sits in that room. Never draw the room, the desk, any furniture or the room's lighting: the background stays flat green.

STYLE
- Cute, soft 2D anime-style characters, like the customers of a cozy shop-counter game: expressive anime eyes (larger than realistic, with one simple highlight), a small, simple nose and mouth, clean rounded face shapes, and hair drawn in clean stylised shapes and locks. Every character is an adult who looks their age: never chibi, never childlike.
- The head size and body proportions are fixed by the attached guide, mannequin or base figure (adult proportions, about seven and a half heads tall; see CANVAS). The style changes only the face and the rendering: never enlarge the head, and never shorten or lengthen the body.
- Faces stay individual: give each face the face shape, eye shape, brows and nose its prompt describes, never one anime face for everyone. Paint every skin tone exactly as given, never lighter. Every culture is drawn in this same style: never a caricature, never an exoticised version of a people.
- One cast: the game's layered travellers and its named historical characters are drawn in two separate Projects but must look like one cast, with the same eyes, line, shading and colour treatment, so neither kind ever stands out from the other at the desk.
- Everything reads small: in the game the head is only about 58 px tall on screen, so keep the eyes, brows, mouth, hair shapes and headwear simple, bold and clear.
- Flat cel colours: each colour area has ONE hard-edged shade tone (the same hue, about 20% darker, with a crisp edge and no soft blending) and at most one small highlight. No fabric grain, brush or paper texture, noise, gradient or photographic detail. Draw patterns (stripes, checks, borders, embroidery) as clean, bold, flat shapes.
- A clean, even dark-brown outline (#3B2A20, 2-3 px) around every piece and its main folds.
- Neutral, even lighting: plain white light from the front. Shade only to show form (under the chin, inside folds, under a brim), the same on both sides. No light direction, no rim light, no glow, no cast shadow, no ground shadow, no warm or cool tint.
- Gently muted, natural period colours. Nothing neon.
- No manga symbols: no sweat drops, anger marks, blush lines, sparkles, tears or speed lines. A face shows feeling only through its eyes, brows and mouth.
- Front view: standing straight, facing the viewer, arms relaxed slightly away from the body, hands open and empty, neutral expression, eyes looking at the viewer.
- When I attach a STYLE REFERENCE image, match its line weight, shading and pattern scale, and never copy its clothing.
- Respectful and historically grounded. No caricature or stereotype of any people or culture.
- Civilian clothing only. No military uniforms or armour, weapons, flags, insignia, badges, royal regalia, political, regime or hate symbols, and no religious vestments or holy symbols.

CANVAS (every image)
- Portrait, 1024 x 1536 px. The figure stays exactly where the attached guide, mannequin or base figure puts it: same top of head, chin, shoulders, waist, hips, knees and feet, same centre line. Never move, resize, turn or re-pose it.
- Background: one flat pure green #00FF00 everywhere outside the figure. No gradient, texture, floor, shadow, vignette or frame. Never draw a checkerboard or "transparent" pattern.
- Leave green on all four sides: nothing touches an edge. Tall hats and hair may rise into the space above the head.
- Never use bright green, lime, magenta, pink, purple or violet anywhere in the clothing, hair or items, not even muted. Where a look asks for purple, paint deep wine red. Where it asks for pink or rose, paint dusty coral or salmon. Where it asks for green, paint dark olive, moss or bottle green. Paint jade as a dark, dull grey-green stone with a strong dark outline. Natural skin and lip colours on faces and bodies are fine.
- Paint every fabric fully opaque, even veils, gauze, muslin and stockings, and never cut holes through it. Magenta, green or skin must never show through anything. Where I say "light" or "fine" fabric, paint a thin, light, solid fabric.
- No text, letters, numbers, logos, watermarks or signatures anywhere. Bands called "tiraz" or "script" are abstract embroidered patterns, never real or fake letters. Coins are plain discs with no face, letters or marks.

CHARACTERS
- Draw each character on the attached base figure, keeping its pose, size and position, with a new face, hair, clothing, headwear and accessories.
- Draw hair, beards and wigs in the colour the description gives (grey for an elderly character).
- Dress each character exactly as described: the civilian dress of their own place and time.
- Draw every face in the shared style, but keep the features its description gives (such as a snub nose, full lips, a firm jaw or the lines of age): they make each person recognisable.
- Each character's first image has a neutral expression. When I ask for happy, angry or worried, change only the expression: anime-expressive but dignified, because these are real people. Show feeling through the eyes, brows and mouth only; no comic distortion and no manga symbols.
```

**Rules for every premade:** the same style as the generated travellers (section 2: one cast, with the same eyes, line, shading and colour treatment; Claude compares each premade with the pilot's Athens travellers at the desk, and one that stands out is redrawn), and the same canvas, neutral lighting, green background and "do not draw" rules as everyone else; period-accurate civilian dress of their claimed place and moment, **including that place's Costume Guide item for their gender** (the Look menu lists a premade's whole picture as one garment, "Period dress", valued with the claim's Costume Guide entry, and from day 3 the player compares dress with the Guide, so an honest premade must look like its row, and the item must show above the desk); anime-expressive but dignified expressions (prompt 9.11); the age shown below; hands open and empty. Every description was checked against its place's ChatGPT DO NOT DRAW line and contradicts nothing in it; paste the premade's own "Do not draw" line with prompt 9.10.

**The cast** (Amendment A1: about half women, at most ten). The drawing descriptions are in Batches 3, 6 and 8 (sections 15, 18 and 20).

| id | Name | Gender | Claims | Wears (Costume Guide item) | Age | Born (for the game) | In the game |
|---|---|---|---|---|---|---|---|
| `senenmut` | Senenmut | man | New Kingdom Egypt | wesekh collar (accessory) | 35 | 14 Mar 1505 BCE | forced: day 1, slot 3 (story) |
| `socrates` | Socrates | man | Periclean Athens | petasos hat (headwear) | 40 | 6 Jun 470 BCE | forced: day 2, slot 6 (story); pooled day 3; impostor from italy_ancient |
| `aspasia` | Aspasia | woman | Periclean Athens | sakkos snood (headwear) | 40 | 12 Sep 470 BCE | pooled days 2-3 |
| `banzhao` | Ban Zhao | woman | Eastern Han Luoyang | bi-disc pendant (accessory) | 60 | 3 Feb 45 | pooled days 2-3 |
| `arib` | Arib al-Ma'muniyya | woman | Abbasid Baghdad | veil with isaba (headwear) | 33 | 20 May 797 | pooled days 2-3 |
| `khwarizmi` | Muhammad al-Khwarizmi | man | Abbasid Baghdad | qalansuwa (headwear) | 50 | 3 Apr 780 | pooled days 2-3 |
| `gutenberg` | Johannes Gutenberg | man | Holy Roman Empire (Rhineland) | Gugel hood (headwear) | 40 | 24 Jun 1400 | pooled day 3 |
| `leonardo` | Leonardo da Vinci | man | Sforza Milan | red berretta (headwear) | 43 | 15 Apr 1452 | pooled day 3 |
| `lanyer` | Aemilia Lanyer | woman | Elizabethan England | hat over coif (headwear) | 31 | 27 Jan 1569 | pooled day 3 |
| `gallerani` | Cecilia Gallerani | woman | Sforza Milan | lenza (headwear) | 22 | 3 Mar 1473 | pooled day 3 |

Alternates if Saleh wants to swap someone: **Alessandra Macinghi Strozzi** (Florentine Republic, born 1407), who was in the cast until the v2 review: in 1439 she was a widow, and a widow's veil and dress contradict both Florence's Costume Guide item (the ghirlanda) and its DO NOT DRAW line (no women's veils), so her art would look disguised. She can come back if that mismatch is accepted knowingly (Appendix E). Also Cai Lun (Eastern Han Luoyang, born c. 62) and Zeami Motokiyo (Muromachi Kyoto, born 1363; the only way to give Japan a premade before day 4). Caritas Pirckheimer (Renaissance Nuremberg) was left out: she was an abbess, and the rules forbid religious vestments.

## 12. Batch order

The game runs on placeholder shapes until art arrives, and every file replaces its placeholder by name. So the order below puts first what the player sees first: the pilot proves the method, bases give every traveller a real body, then each game day in turn. Dress tells start on day 3, so the leak items of days 1 to 3 matter most.

| Batch | What | Game day | Why now |
|---|---|---|---|
| 1 | Pilot: base figures + Periclean Athens + stress test | day 1 | proves the stack, the key and the style before anything else |
| 2 | Skin tones and faces | every day | every traveller has a real body and face |
| 3 | Story premades: Senenmut, Socrates | days 1-2 | the two scripted story visitors |
| 4 | Day 1 places (Ancient Egypt, Iraq, Italy) | day 1 | day 1 fully drawn |
| 5 | Day 2 places (Ancient China and Britain; Medieval Egypt, Iraq, Greece, Italy, China, Britain) | day 2 | day 2 fully drawn |
| 6 | Day 2-3 premades: Aspasia, Ban Zhao, Arib al-Ma'muniyya, Muhammad al-Khwarizmi | days 2-3 | the random premade pool of day 2 |
| 7 | Day 3 places (Ancient and Medieval Japan and Germany; all Early modern) | day 3 | day 3, the first day with dress tells |
| 8 | Day 3 premades: Johannes Gutenberg, Leonardo da Vinci, Aemilia Lanyer, Cecilia Gallerani | day 3 | the rest of the premade pool |
| 9 | Industrial era | day 4 |  |
| 10 | Modern era | day 5 |  |
| 11 | The Future | day 6 | only appears once a country leads history |
| 12 | The 2150 accessory kit | day 2 | a 2150 citizen's costume error: one kit item over a right costume (the neutral Future outfits of Batch 11 are the other, 2150 clothes) |

Batch 1 has to come first and Batch 2 second (premades and everything else need the approved base figures). After that the order is a recommendation: skipping ahead never breaks anything. Within a batch, send each place to Claude as soon as it is done. The UI art in `UI_ART_RULES.md` is a separate track in its own ChatGPT Project; start it any time after Batch 1.

## 13. Batch 1: pilot (15 images)

This small batch proves the pieces stack, the cut-out works and the style is right before anyone makes hundreds of images. Folder: `Raw/batch01-pilot/`.

**Step 1.** Use prompt 9.1 twice, for skin tone 1, face a:

- `base_m_skin1_facea.png`
- `base_f_skin1_facea.png`

**Send to Claude.** Claude resizes and re-centres both figures onto the guide's landmarks (top of head y=260, chin y=424, soles y=1490) before making the mannequins, so don't reject a figure just because it is a little big or off-centre. Do reject one whose head is too big for its body (easy in this style: with the top of the head and the soles on their lines, the chin sits clearly below the CHIN line), because the head size is fixed. Claude splits each figure into a body and a head, recolours the approved body to the four other skin swatches (section 7), and sends back `mannequin_m.png` and `mannequin_f.png`. Keep them in your art folder.

**Step 2.** In a new chat, send this PAIR block with prompt 9.0, then use prompts 9.4 to 9.8 for each file below it. The style card does not exist yet, so for this pilot only, attach the mannequin alone and leave the style card out of each prompt's "Attached:" line.

#### Greece: Periclean Athens

Costume Guide entry (the Culture value): **petasos hat / sakkos snood**

```
PAIR: greece_ancient (Periclean Athens, 430 BCE)
Place and time: Athens in the age of Pericles and Socrates, c. 430 BCE

MAN
- Outfit: Short, knee-length wool chiton in undyed cream or saffron, under a large rectangular himation (wool mantle) in a colour such as madder red, blue-grey or saffron. The himation wraps round the body and is thrown over the left shoulder, leaving the right arm and shoulder free (the chiton covers the chest). Its hem has a woven meander (Greek key) border in dark red or black. Simple strapped leather sandals or bare feet.
- Hair: Classical Athenian crop: short-to-medium thick curls brushed forward, half covering the ears
- Facial hair: Full, rounded, neatly trimmed beard with moustache (the norm for an adult Athenian citizen)
- Headwear: Petasos: a flat, wide-brimmed traveller's hat of undyed tan felt with a low crown, worn tilted back on the head with the brim turned up at the front so the face is fully clear, tied under the chin with a thin cord
- Accessory: Pera: a hand-sized soft leather traveller's pouch on a thin strap slung diagonally across the body from the left shoulder

WOMAN
- Outfit: Ankle-length Ionic chiton of fine crinkled linen, with short sleeves formed by a row of small pins along the upper arms, tied at the natural waist with a narrow woven girdle and bloused over it (kolpos). A himation in saffron, madder red or indigo is wrapped diagonally over one shoulder, with a meander or wave-pattern band at its hem.
- Hair: Centre-parted wavy hair drawn back to a bun at the nape, with a few curls escaping at the temples
- Headwear: Sakkos: a patterned cloth snood that covers the crown and back of the head and wraps the bun, leaving the forehead and face fully visible
- Accessory: none

MUST READ AT A GLANCE: The coloured himation with a woven meander (Greek key) border, draped over one shoulder with no curved toga edge; men add a beard and the flat petasos traveller's hat, and women add the patterned sakkos snood
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (petasos hat); woman's headwear (sakkos snood)
DO NOT DRAW: Roman toga with a curved, rounded edge or a U-shaped front fold, or a tunic with purple stripes (reads as Rome); clean-shaven adult men (reads as Roman); laurel or olive victory wreaths; Corinthian helmet from Pericles' busts, hoplite armour, Spartan red cloak (military); short chlamys cloak pinned at the shoulder with petasos (the ephebe/cadet and hunting costume); all-white marble-statue clothing (real Greek dress was coloured); 'toga party' bedsheet look; girdle high under the bust (a Hellenistic fashion, a century later); Greek national flag colours or modern symbols; priestess or temple ritual dress (sacred)
```

Files (8): `outfit_m_greece_ancient.png`, `hair_m_greece_ancient.png`, `facialhair_m_greece_ancient.png`, `headwear_m_greece_ancient.png`, `accessory_m_greece_ancient.png`, `outfit_f_greece_ancient.png`, `hair_f_greece_ancient.png`, `headwear_f_greece_ancient.png`

**Step 3 (stress test).** These try the risky cases (a hood over the head that hides all the hair, hair under a veil, a large dark-green area, a wig hanging behind the shoulders). In a new chat for each PAIR block (they are in Batches 4, 5 and 7): paste the Egypt medieval PAIR block and make `outfit_f_egypt_medieval.png` (add to the prompt: "Make the qamis deep bottle green.", so the green case is really tested), `hair_f_egypt_medieval.png`, `headwear_f_egypt_medieval.png`; paste the Japan ancient PAIR block and make `accessory_m_japan_ancient.png`; paste the Egypt ancient PAIR block and make `hair_f_egypt_ancient.png`. They count toward their own batches, which skip them.

**Send to Claude.** Claude stacks everything in Unity: the figure behind the desk in the game's office, the passport photo, and the whole canvas. If the pieces line up and the style is right, go on. If not, we fix the prompts before making more.

**Step 4 (style card).** Once the pilot is approved, Claude makes `style_card.png`: the approved Athens outfits in greyscale on the mannequin, with a small swatch of folds showing their one hard-edged shade tone, a pattern band and the outline weight, all on the green background. From now on every garment prompt (9.4 to 9.8) attaches it next to the mannequin, and every premade prompt (9.10) next to the base figure, as a style reference only.

## 14. Batch 2: skin tones and faces (38 images)

Folder: `Raw/batch02-base/`. One chat is fine here (no costumes to mix up).

- **8 base figures** with prompt 9.2: `base_[m/f]_skin[2-5]_facea.png`. Claude keeps only the head from these; the bodies for skins 2 to 5 were already recoloured from your approved skin-1 body to the same swatches, so they match the mannequin exactly.
- **30 heads** with prompt 9.3, faces b, c and d for every skin tone and both genders: `head_[m/f]_skin[1-5]_face[b/c/d].png`. Each image shows the whole figure.

**Send to Claude** after the first skin tone's faces (so a drifting head is caught early), then the rest.

How Claude processes and accepts the heads (every garment is fitted to the face-a marks, so a head that drifts would misalign beards, glasses and close caps):

1. Claude cuts each head out and warps it so its eyes, nose, mouth, ears and skull outline land on the face-a mannequin's marks, then matches its colour to the body.
2. A head whose skull outline or jaw is more than a few pixels off the face-a outline after that is rejected: press Regenerate for it.
3. **Stacking check** before Batch 2 is done: Claude stacks the pilot's beard and petasos and the stress test's hood on every new head, with face d as the hardest case. It repeats the check when the first glasses (Batch 10: Iraq and Germany Modern) and the first close caps (the guapimao in Batch 9, the cloche in Batch 10) arrive.
4. Claude then makes the premade base figures `premadebase_[m/f]_skin[1-5].png` (the recoloured body with the approved face-a head, aligned to the landmarks) for Batches 3, 6 and 8.

From here on every traveller in the game has a real body and face.

## 15. Batch 3: premade characters: Senenmut, Socrates (8 images)

The two story visitors: Senenmut is always the third traveller of day 1, and "Socrates" the sixth of day 2. Make them in the **Time Sorter Premades** Project (section 11), one new chat per character. Use prompt 9.10 for the neutral image, then 9.11 three times. Folder: `Raw/batch03-premades/`. Send each character's four images to Claude as soon as they are done.

#### Senenmut (`senenmut`)

- Claims: New Kingdom Egypt (egypt_ancient), 1470 BCE. Age at the desk: about 35 (face band c). Base figure to attach: `premadebase_m_skin4.png`.
- Wears the claimed place's Costume Guide item: wesekh collar (accessory).
- In the game: forced: day 1, slot 3 (story).

```
Senenmut, steward of the royal household and architect of the queen's temple at Deir el-Bahari. A lean man of about 35 with a calm, clever face, clean-shaven. A shoulder-length black bobbed wig with a short straight fringe. A calf-length wrap-around kilt of fine white linen, knotted at the waist, with a finely pleated front panel; bare chest; a broad wesekh collar of blue-leaning turquoise, lapis-blue, carnelian-red and gold beads with teardrop pendants, covering the upper chest and shoulders; plain gold bands on the upper arms; papyrus sandals.
```

Do not draw: nemes headcloth, cobra (uraeus), any crown, false beard, crook and flail; a long pleated over-robe; anything held in the hands.

Files: `premade_senenmut_neutral.png`, `premade_senenmut_happy.png`, `premade_senenmut_angry.png`, `premade_senenmut_worried.png`

#### Socrates (`socrates`)

- Claims: Periclean Athens (greece_ancient), 430 BCE. Age at the desk: about 40 (face band c). Base figure to attach: `premadebase_m_skin3.png`.
- Wears the claimed place's Costume Guide item: petasos hat (headwear).
- In the game: forced: day 2, slot 6 (story); pooled day 3; an authored liar, really from italy_ancient (an impostor).

```
Socrates of Athens as his friends described him, about 40: stocky, with a snub nose, wide-set prominent eyes, full lips, a high balding forehead with curly dark-brown hair at the sides and back, and a full, bushy dark-brown beard. He wears only one plain, well-worn himation of coarse undyed brown-grey wool (a tribon) wrapped round the body and over the left shoulder, leaving the right shoulder and arm bare, with no tunic under it. On his head, a petasos: a flat, wide-brimmed traveller's hat of undyed tan felt with a low crown, tilted back with the brim turned up at the front so the face is fully clear. Barefoot. Warm, alert and dignified, never a caricature.
```

Do not draw: a Roman toga or any Roman item (in the game he is an impostor, but his art shows the Socrates he claims to be); a short chlamys cloak; philosopher props, scrolls, anything held.

Files: `premade_socrates_neutral.png`, `premade_socrates_happy.png`, `premade_socrates_angry.png`, `premade_socrates_worried.png`

**Send to Claude.**

## 16. Batch 4: Day 1 places (19 images)

Day 1 sends travellers from Ancient Egypt, Iraq, Greece and Italy. Greece was the pilot. Folder: `Raw/batch04-day-1-places/`. Start a new chat for each place, send its PAIR block with prompt 9.0, then make each file in its list. **Send each place's files to Claude as soon as that place is done**, and wait for the go-ahead on the first place of the batch.

### Egypt: New Kingdom Egypt

Costume Guide entry (the Culture value): **wesekh collar**

```
PAIR: egypt_ancient (New Kingdom Egypt, 1470 BCE)
Place and time: Thebes during the reign of Hatshepsut, c. 1470 BCE (her architect Senenmut; 'scribes of the fields' re-measuring land with knotted ropes after each Nile flood)

MAN
- Outfit: Calf-length wrap-around kilt of fine bleached white linen, knotted at the waist, with the overlapping front edge falling in a slanting, finely pleated panel. Bare torso; plain papyrus or leather sandals. Everything is white or ecru linen.
- Hair: Official's bobbed wig: straight black wig cut bluntly at shoulder length with a short straight fringe, falling over the ears or tucked behind them (head shaved underneath) (this is a WIG: draw it in the colour described)
- Facial hair: none (clean-shaven; the false beard was royal and must not be used)
- Headwear: none
- Accessory: Wesekh broad collar: a flat semicircular collar of many rows of faience beads (blue-leaning turquoise, lapis blue, carnelian red, gold) with a row of teardrop pendants on the outer edge, covering the upper chest and shoulders

WOMAN
- Outfit: Ankle-length, close-fitting sheath dress of fine white linen that covers the bust and is held up by two broad shoulder straps; papyrus sandals or barefoot. No overgarment.
- Hair: Tripartite wig: a long, heavy, straight black wig parted in the centre, with two thick lappets falling in front of the shoulders to the chest and the rest hanging down the back (this is a WIG: draw it in the colour described)
- Headwear: Thin floral fillet: a narrow band of blue-lotus petals tied round the wig, with a single lotus bud over the forehead
- Accessory: Wesekh broad collar of rows of blue-leaning turquoise, lapis, carnelian and gold faience beads with teardrop pendants

MUST READ AT A GLANCE: The broad beaded wesekh collar worn over crisp white linen, with a heavy wig
LEAK ITEMS (draw these extra clear and true to the text): man's accessory (wesekh collar); woman's accessory (wesekh collar)
DO NOT DRAW: nemes striped headcloth, uraeus cobra, vulture crown, blue khepresh crown (all royal or divine); false beard, crook and flail; Hollywood 'Cleopatra' styling or gold lame; sheer pleated over-robes, knotted shawls and layered 'duplex' wigs (later 18th-dynasty and Amarna fashion); perfume/wax cones on the head (banquet-only, and they read as a hat); Greek draped chiton/himation and fibula brooches (reads as Greece-ancient; Ptolemaic Alexandria was largely Greek-dressed); coloured or fringed wool wraps and long beards (read as Mesopotamia); ankh or god emblems as ornaments; mummy wrappings, pyramids or sphinx props
```

Files (6): `outfit_m_egypt_ancient.png`, `hair_m_egypt_ancient.png`, `accessory_m_egypt_ancient.png`, `outfit_f_egypt_ancient.png`, `headwear_f_egypt_ancient.png`, `accessory_f_egypt_ancient.png` (already made in Batch 1: `hair_f_egypt_ancient.png`)

Notes for Claude's processing: the man's hair is a wig (one colour, no variants); the woman's hair is a wig (one colour, no variants); the woman's hair hangs behind the shoulders (Claude splits off a hair-back layer).

**Send this place's files to Claude** before you start the next place.

### Iraq / Mesopotamia: Babylonia

Costume Guide entry (the Culture value): **curled beard / gold fillet**

```
PAIR: iraq_ancient (Babylonia, 1760 BCE)
Place and time: Babylon under Hammurabi, c. 1760 BCE (the law code, clay contracts sealed with cylinder seals)

MAN
- Outfit: Ankle-length wrap of heavy dyed wool (madder red, blue or yellow) wound round the body and thrown over the left shoulder, leaving the right shoulder and arm bare. Every edge has a thick rolled fringe or tufted border, and a wide cloth belt holds it at the waist. Plain leather sandals.
- Hair: Long hair drawn back into a low, rounded chignon (bun) at the nape
- Facial hair: Full, long beard reaching the upper chest, squared at the bottom, combed into long wavy strands that end in neat rows of small curls, with a moustache
- Headwear: Narrow twisted-wool headband (fillet) circling the head and holding the hair
- Accessory: Lapis-lazuli cylinder seal: a finger-length carved stone cylinder hung on a cord around the neck. It was the owner's signature, rolled onto clay contracts.

WOMAN
- Outfit: Ankle-length shawl-dress of dyed wool (saffron-yellow, rust or blue), wound round the body with the end thrown over the left shoulder so the right shoulder is bare, and it is fastened at the chest with a pair of long bronze toggle pins. A thick contrasting fringe runs along its edges and spirals down the body in one or two bands. It is a fringed wrap, not a tiered flounced robe.
- Hair: Centre-parted hair braided into one thick plait wound round the head like a crown
- Headwear: A wide gold-thread ribbon wound over the coiled braid, hung with small gold ring pendants
- Accessory: none

MUST READ AT A GLANCE: The heavily fringed, coloured wool wrap draped over the left shoulder with the right shoulder bare; men also have the long chest-length curled beard
LEAK ITEMS (draw these extra clear and true to the text): man's facial hair (curled beard); woman's headwear (gold fillet)
DO NOT DRAW: horned crowns (divine) and Hammurabi's royal brimmed cap; tiered flounced robes (in Old Babylonian art these are goddess dress, e.g. the Lama goddess); Assyrian winged bulls (lamassu), Ishtar Gate dragons and other religious motifs; Sumerian sheepskin kaunakes skirts (an earlier period); plain, unfringed Greek-style himation with short hair and a short beard (reads as Greece-ancient); white linen, broad bead collars, wigs or clean-shaven faces (read as Egypt); maces, weapons, or clay tablets held in the hand
```

Files (8): `outfit_m_iraq_ancient.png`, `hair_m_iraq_ancient.png`, `facialhair_m_iraq_ancient.png`, `headwear_m_iraq_ancient.png`, `accessory_m_iraq_ancient.png`, `outfit_f_iraq_ancient.png`, `hair_f_iraq_ancient.png`, `headwear_f_iraq_ancient.png`

**Send this place's files to Claude** before you start the next place.

### Italy: Republican Rome

Costume Guide entry (the Culture value): **Caesar crop / nodus roll**

```
PAIR: italy_ancient (Republican Rome, 50 BCE)
Place and time: Rome in the last years of the Republic, as Caesar rose to power, c. 50 BCE

MAN
- Outfit: Knee-length white wool tunica with two narrow deep wine-red vertical stripes (angusticlavus, associated with the equestrian business class), under a long, ample, plain off-white wool toga. The toga is draped the late-Republican way: over the left shoulder, round the back, under the right arm and back up across the front to the left shoulder, forming a firm diagonal band across the chest. Its distinctive rounded (curved) lower edge hangs to the ankles and dips lower at the sides. The right-hand clavus stripe shows at the right side of the chest. Closed black leather calcei ankle boots with laced straps, not sandals.
- Hair: Late-Republican short crop, cut close and combed forward onto the brow (Caesar style)
- Facial hair: none (clean-shaven, the Roman norm of the period)
- Headwear: none
- Accessory: none

WOMAN
- Outfit: A long-sleeved tunica with a sleeveless, floor-length stola over it on narrow shoulder straps (the dress of a respectable married woman). A large plain rectangular palla (mantle) with no patterned border is draped over the left shoulder and around the body. Ochre, dusty coral, slate blue or blue wool.
- Hair: Nodus hairstyle: a raised roll of hair pushed up and pinned above the forehead, the sides waved back, the rest braided into a small bun at the nape
- Headwear: none
- Accessory: Monile: a short necklace of large pearls on gold links, worn high on the chest

MUST READ AT A GLANCE: Men: a plain white toga with a curved, rounded edge draped over the left shoulder, over a tunic with a narrow deep wine-red clavus stripe running down from the shoulder. Women: the nodus roll of hair above the forehead, with the stola's straps on the shoulders.
LEAK ITEMS (draw these extra clear and true to the text): man's hair (Caesar crop); woman's hair (nodus roll)
DO NOT DRAW: deep knee-length sinus fold and chest pouch (umbo): these are Augustan (umbo from c. 10 BCE), a generation too late; laurel wreath (triumphal or imperial); toga praetexta or all-purple toga (magistrate or emperor); broad senatorial stripe (latus clavus); legionary armour, red military cloak, gladiator gear; beards on men (reads as Greek or later imperial); Greek-key or other patterned borders (keep those for Greece); sandals worn with the toga; Vestal or priestly veiling, toga drawn over the head (sacred); arm held in a toga sling (a statue pose that fights the layered body rig)
```

Files (5): `outfit_m_italy_ancient.png`, `hair_m_italy_ancient.png`, `outfit_f_italy_ancient.png`, `hair_f_italy_ancient.png`, `accessory_f_italy_ancient.png`

**Send this place's files to Claude** before you start the next place.

When the last place is in, Claude runs the batch check in Unity.

## 17. Batch 5: Day 2 places (60 images)

Day 2 adds the Medieval era and China and Britain. Folder: `Raw/batch05-day-2-places/`. Start a new chat for each place, send its PAIR block with prompt 9.0, then make each file in its list. **Send each place's files to Claude as soon as that place is done**, and wait for the go-ahead on the first place of the batch.

### China: Eastern Han Luoyang

Costume Guide entry (the Culture value): **jieze cap / bi-disc pendant**

```
PAIR: china_ancient (Eastern Han Luoyang, 105 CE)
Place and time: Luoyang, the Eastern Han capital, c. 105 CE, when Cai Lun presented his improved paper to the court

MAN
- Outfit: Ankle-length, straight-hemmed wrap robe (zhiju paofu) closed with the wearer's left panel over the right, making a 'y' at the throat, with two or three layered collars (the innermost white). The sleeves are huge, bagging out below the arm and narrowing at the wrist. The ground is black, deep red or brown silk with broad contrasting patterned borders at collar, cuffs and hem, tied at the waist with a cloth sash on a bronze belt hook, with a hand-sized embroidered silk pouch hanging from the sash on a red cord. Black cloth shoes with upturned square toes peek from the hem.
- Hair: Han topknot (fa ji): all hair combed smoothly up from forehead and nape into a single knot on the crown, no fringe
- Facial hair: Thin moustache with slightly upturned tips and a short pointed chin beard
- Headwear: Jieze: a close-fitting black cloth cap with a stiff band across the forehead, a raised roof-shaped ridge on top covering the topknot, and two short upright flaps ('ears') standing at the back. No chin ties. A slim bamboo writing brush (zanbi) is tucked upright into its side.
- Accessory: none

WOMAN
- Outfit: Floor-length quju shenyi: a wrap robe whose long curved front panel spirals round the body, making layered diagonal bands on the skirt. Wide drooping sleeves narrow at the wrist, and two or three cross-collars layer at the neck, the wearer's left over right. Deep red, black or ochre silk with checked or cloud-scroll borders, cinched with a sash. The silhouette is narrow and trails at the hem.
- Hair: Chuishao ji: centre-parted hair drawn smoothly back into a low looped bun at the nape, with a short tail of hair hanging below it
- Headwear: none
- Accessory: Jade bi-disc pendant: a flat round disc of dark, dull grey-green jade with a hole in the centre, as wide as the palm, hanging high on the chest (its centre about a hand's width below the collarbones) on its own red silk cord round the neck, with a short red tassel below it

MUST READ AT A GLANCE: The layered cross-collars (a 'y' at the throat) of a long wrap robe with broad dark borders and huge bag-shaped 'ox-dewlap' sleeves; men add the roof-ridged black jieze cap, women the palm-wide dark jade bi-disc pendant high on the chest
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (jieze cap); woman's accessory (bi-disc pendant)
DO NOT DRAW: Emperor's mianguan crown with bead curtains, dragon robes or imperial yellow; jinxian guan ridged official hat or coloured seal-ribbons (shou), which mark rank; armour, swords, crossbows; Qing items (queue braid, skullcap, horse-hoof cuffs) or Tang/Ming items; Japanese elements: mizura side loops, magatama beads, short jacket with trousers tied at the knee; conical straw hat (douli); robe closed with the right panel over the left (reversed); chin ties on the jieze (ties belong to the formal guan hat worn on top of it)
```

Files (7): `outfit_m_china_ancient.png`, `hair_m_china_ancient.png`, `facialhair_m_china_ancient.png`, `headwear_m_china_ancient.png`, `outfit_f_china_ancient.png`, `hair_f_china_ancient.png`, `accessory_f_china_ancient.png`

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): 'coolie' hat stereotype, Fu Manchu moustache, slanted-eye caricature.

**Send this place's files to Claude** before you start the next place.

### Britain: Iron Age Britain

Costume Guide entry (the Culture value): **torc**

```
PAIR: britain_ancient (Iron Age Britain, 20 CE)
Place and time: Camulodunum (Colchester), the trading capital of the Catuvellauni under Cunobelinus, c. 20 CE: shortly before the Roman conquest, while wine and luxury goods were arriving from Roman Gaul

MAN
- Outfit: Knee-length belted wool tunic over close-fitting trousers (bracae), both woven in bold multicoloured checks of red, mustard yellow and dark olive green. A thick, short rectangular cloak in a contrasting check is pinned at the right shoulder. Soft leather ankle shoes.
- Hair: Long swept-back mane: shoulder-length hair brushed straight back off the forehead, thick and full
- Facial hair: Long drooping moustache that hangs past the corners of the mouth; chin and cheeks clean-shaven
- Headwear: none
- Accessory: Twisted gold torc: a thick, rope-twisted open neck ring with rounded knob ends meeting at the throat

WOMAN
- Outfit: Ankle-length, long-sleeved tunic-dress of bright checked wool in red, yellow and dark olive green, cinched with a woven belt. Over it hangs a thick, plain dark-red or ochre wool mantle, fastened at the chest with a plain bronze pin.
- Hair: Very long centre-parted hair in two thick braids falling forward over the shoulders to the waist
- Headwear: none
- Accessory: Twisted gold or bronze torc at the neck

MUST READ AT A GLANCE: The rope-twisted gold torc at the throat, worn with bold multicoloured checked wool
LEAK ITEMS (draw these extra clear and true to the text): man's accessory (torc); woman's accessory (torc)
DO NOT DRAW: blue woad body paint or tattoos (skin must never carry the signal); horned or winged helmets; Roman toga, tunic-with-toga or Roman armour; swords, shields, spears, chariot-warrior gear; Scottish kilts or clan tartans (anachronistic by 1,500+ years); Suebian side-knot hairstyle or full beard (that is Germany-ancient); plain undyed or single-colour cloth with amber beads (reads as Germany-ancient); fur loincloths or pelts; druid robes or any religious costume
```

Files (7): `outfit_m_britain_ancient.png`, `hair_m_britain_ancient.png`, `facialhair_m_britain_ancient.png`, `accessory_m_britain_ancient.png`, `outfit_f_britain_ancient.png`, `hair_f_britain_ancient.png`, `accessory_f_britain_ancient.png`

Notes for Claude's processing: the man's hair hangs behind the shoulders (Claude splits off a hair-back layer).

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): 'savage barbarian' caricature.

**Send this place's files to Claude** before you start the next place.

### Egypt: Mamluk Egypt

Costume Guide entry (the Culture value): **imama turban / izar hood**

```
PAIR: egypt_medieval (Mamluk Egypt, 1340)
Place and time: Mamluk Cairo under al-Nasir Muhammad, c. 1340 CE (the al-Khalij al-Nasiri canal was dug in 1325 and the Nile flood was read at the Roda Nilometer)

MAN
- Outfit: Ankle-length pale linen qamis under an open, front-opening farajiyya, the long outer robe of Mamluk scholars, judges and scribes. The farajiyya is bottle green, brown or dark blue wool (never black), with very wide, long sleeves that fall well past the fingertips because ample sleeves marked civilian status. No embroidered armbands. Soft leather boots.
- Hair: Short-cropped
- Facial hair: Full, rounded, neatly trimmed beard with a moustache
- Headwear: Large, broad, rounded white muslin turban ('imama) wound low and full around a small hidden cap, its lower edge just above the eyebrows, with no cap showing above it. One loose end (the 'adhaba tail) hangs down in front of one shoulder. (This headwear hides all the hair, so no hair is drawn under it: bring its front edge right down to the hairline so no bald scalp shows.)
- Accessory: Broad woven-silk girdle wound at the waist, with long knotted fringe ends

WOMAN
- Outfit: Ankle-length silk qamis in saffron, crimson or deep bottle green with small woven motifs. It has very wide sleeves that hang well below the hands and is worn over full trousers (sirwal) gathered at the ankle. Over it, a large plain WHITE linen izar falls from the shoulders down to the ankles, hanging behind the arms and open at the front, so the bright qamis shows down the middle and its big sleeves show at the sides.
- Hair: Long hair braided and pinned up close to the head, with a thin smooth edge of hair at the brow
- Headwear: The top of the white izar drawn over the head as a soft hood that frames the fully visible face and falls down both sides of the neck onto the shoulders, where it meets the izar. No face veil. (This headwear hides all the hair, so no hair is drawn under it: bring its front edge right down to the hairline so no bald scalp shows.)
- Accessory: Broad gold filigree collar necklace with a row of hanging pendants

MUST READ AT A GLANCE: Men: a big, low, round white turban, worn with very long, wide sleeves. Women: a full-length white izar hooded over the head and hanging open over a brightly coloured gown.
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (imama turban); woman's headwear (izar hood)
DO NOT DRAW: Mamluk military dress: kallawta cap, heraldic blazons or roundels, metal-plaque belts, swords, bows; Abbasid tall black qalansuwa cap, black robes, tiraz script armbands, or taylasan/tarha shawl over the shoulders (read as Iraq-medieval); Blue, yellow or red izars, or any colour-coded turban: these colours were imposed on Christian, Jewish and Samaritan subjects; face-covering veils (burqu'); Ottoman tall kavuk turbans; the tall tartur/taqiyya women's caps (15th century and later, and banned in 1426/7); belly-dance costume; Crusader imagery
```

Files (6): `outfit_m_egypt_medieval.png`, `hair_m_egypt_medieval.png`, `facialhair_m_egypt_medieval.png`, `headwear_m_egypt_medieval.png`, `accessory_m_egypt_medieval.png`, `accessory_f_egypt_medieval.png` (already made in Batch 1: `outfit_f_egypt_medieval.png`, `hair_f_egypt_medieval.png`, `headwear_f_egypt_medieval.png`)

Notes for Claude's processing: the man's headwear covers the hair (check on the bald head that no scalp shows under it); the woman's headwear covers the hair (check on the bald head that no scalp shows under it).

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): harem cliches.

**Send this place's files to Claude** before you start the next place.

### Iraq / Mesopotamia: Abbasid Baghdad

Costume Guide entry (the Culture value): **qalansuwa / veil with isaba**

```
PAIR: iraq_medieval (Abbasid Baghdad, 830 CE)
Place and time: Baghdad under the Abbasids, c. 830 CE (al-Khwarizmi at the House of Wisdom; translation and algebra)

MAN
- Outfit: Ankle-length pale linen qamis and sirwal under a front-opening, calf-length durra'a robe of black wool or silk. Broad bands of gold embroidery (tiraz) circle both upper sleeves, patterned with abstract strokes and knots, not letters. The sleeves are moderately wide and end at the wrist. Soft leather khuff boots.
- Hair: Hair to the nape, combed smooth and close to the head
- Facial hair: Full beard, neatly trimmed and rounded, with a moustache
- Headwear: Qalansuwa tawila: a tall, stiff, smooth black cap (a cone or high cylinder about one and a half times the height of the head) with a white turban wound round its base. The tall black top rises clearly above the turban and stays upright, not soft or crumpled.
- Accessory: Taylasan: a dark shawl draped over both shoulders, its ends falling down the front of the chest, the mark of a scholar. It stays on top of the shoulders, clear of the tiraz armbands on the upper sleeves.

WOMAN
- Outfit: Long-sleeved, ankle-length qamis in a rich colour (dusty coral, saffron or olive) with gold-embroidered tiraz bands (abstract pattern, no letters) at the upper arms, over full sirwal gathered at the ankle. A light mantle (rida') lies over the shoulders only, not over the head.
- Hair: Long hair pinned up close to the head, with two glossy S-shaped side-curls (sudgh) combed forward onto the cheeks at the temples
- Headwear: Light khimar veil over the hair, drawn opaque (not sheer or see-through), bound at the brow by an 'isaba: a narrow jewelled and embroidered headband embroidered with a line of small abstract gold patterns (no letters). The veil's sides fall behind the temples so the cheeks and the S-shaped side-curls stay uncovered
- Accessory: Choker of large pearls

MUST READ AT A GLANCE: Men: the tall, stiff black qalansuwa cap rising out of a white turban, with tiraz armbands. Women: the jewelled 'isaba band across the brow, with S-shaped side-curls on the cheeks.
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (qalansuwa); woman's headwear (veil with isaba)
DO NOT DRAW: curled-toe slippers, balloon 'genie' trousers, onion domes; face-covering veils; any Arabic letters or writing, real or fake, on clothing; keffiyeh with agal (a later style); Ottoman bulbous turbans; Mamluk very long hanging sleeves, a big round all-white turban, or a full-length white izar wrap (read as Egypt-medieval); soft, crumpled or forward-leaning cap (reads as the Japanese eboshi); Sasanian royal crowns, black battle banners, weapons
```

Files (9): `outfit_m_iraq_medieval.png`, `hair_m_iraq_medieval.png`, `facialhair_m_iraq_medieval.png`, `headwear_m_iraq_medieval.png`, `accessory_m_iraq_medieval.png`, `outfit_f_iraq_medieval.png`, `hair_f_iraq_medieval.png`, `headwear_f_iraq_medieval.png`, `accessory_f_iraq_medieval.png`

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): 'Arabian Nights' or Aladdin cliches; harem stereotypes.

**Send this place's files to Claude** before you start the next place.

### Greece: Byzantine Mystras

Costume Guide entry (the Culture value): **Palaiologan hat / fakiolion**

```
PAIR: greece_medieval (Byzantine Mystras, 1430)
Place and time: Mystras, capital of the Byzantine Despotate of the Morea, in the circle of the philosopher Gemistos Plethon, c. 1430

MAN
- Outfit: Ankle-length kabbadion: a caftan-like robe closed down the centre front with a long row of small round buttons, with long narrow sleeves, in bottle green, wine or blue silk with a small woven pattern. No overcoat, so the button row stays fully visible. Soft dark boots.
- Hair: Long hair, centre-parted, falling to the shoulders
- Facial hair: Long full beard with moustache (the Byzantine lay norm, noted by Italian observers in 1438)
- Headwear: Tall felt hat with a high rounded crown and a brim turned sharply up all round against it, with a light crown and a darker brim, as Pisanello sketched among the Byzantine delegation.
- Accessory: Belt with gilt metal mounts, worn at the waist

WOMAN
- Outfit: A fitted, sleeveless over-dress of patterned silk damask (for example wine red with a small floral pattern), shaped close at the bust and flaring from just below it to the floor, with braided gold trim at the neckline and armholes. Under it is a long T-shaped under-tunic of lighter patterned silk whose long, fitted sleeves show full-length on the arms.
- Hair: Hair pinned up close to the head, with a smooth edge at the forehead
- Headwear: Turban-style headdress (fakiolion): a long strip of cream silk woven with bands of gold and red, wound high and rounded around the head so it covers all the hair. The neck, ears and face stay open, with no cloth wrapped under the chin. (This headwear hides all the hair, so no hair is drawn under it: bring its front edge right down to the hairline so no bald scalp shows.)
- Accessory: Large gold crescent-shaped (lunate) earrings edged with pearls, hanging clearly below the headdress

MUST READ AT A GLANCE: Men: a tall felt hat with a high rounded crown and turned-up brim, a long beard and a button-front robe. Women: a compact, patterned, wound turban-headdress with big crescent earrings, over a fitted sleeveless damask over-dress.
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (Palaiologan hat); woman's headwear (fakiolion)
DO NOT DRAW: imperial regalia: loros, crowns, purple silk, double-headed eagle; emperor's pointed-visor hat (royalty) and the skaranikon official cap bearing the emperor's portrait (regime insignia); Orthodox clerical or monastic robes, black kalimavkion, crosses or icons (sacred); Virgin Mary (Theotokos) style dark star-marked maphorion veil (sacred); plain white headcloth wrapped round the neck and chin (reads as the Britain headrail) or a full-length white wrap (reads Egypt); wide bell-sleeved overgown (reads Britain); the Byzantine 'head donut' padded ring (unattested, and reads as Italy's ghirlanda); Ottoman white turban on men (anachronistic here; reads Egypt or Iraq); Italian cappuccio hood-hat, or clean-shaven men (reads Italy); armour or weapons, including the sabre in Pisanello's sketches
```

Files (9): `outfit_m_greece_medieval.png`, `hair_m_greece_medieval.png`, `facialhair_m_greece_medieval.png`, `headwear_m_greece_medieval.png`, `accessory_m_greece_medieval.png`, `outfit_f_greece_medieval.png`, `hair_f_greece_medieval.png`, `headwear_f_greece_medieval.png`, `accessory_f_greece_medieval.png`

Notes for Claude's processing: the man's hair hangs behind the shoulders (Claude splits off a hair-back layer); the woman's headwear covers the hair (check on the bald head that no scalp shows under it).

**Send this place's files to Claude** before you start the next place.

### Italy: Florentine Republic

Costume Guide entry (the Culture value): **mazzocchio hat / ghirlanda**

```
PAIR: italy_medieval (Florentine Republic, 1439)
Place and time: Florence of the Medici bank, during the Council of Florence, 1439

MAN
- Outfit: Ankle-length lucco, the Florentine citizen's gown, in deep red (rosato or cremisi) wool. It is closed high at the neck with a small standing collar, falls in straight organ-pipe pleats, is fur-lined, has wide open sleeves and is belted with a narrow leather belt, with a leather merchant's purse (scarsella) hanging from it at the hip. A dark doublet and hose are worn beneath, with soft black shoes.
- Hair: Early-15th-century Florentine crop: short, rounded, cut above the ears
- Facial hair: none (clean-shaven, the Florentine norm)
- Headwear: Cappuccio a mazzocchio: a padded ring sitting on top of the head like a hat, with draped cloth (foggia) falling to one side and a long band (becchetto) hanging over one shoulder, in red or black. Hair shows below the ring, and it does not enclose the neck like a hood.
- Accessory: none

WOMAN
- Outfit: Fitted gamurra gown in plain silk with long, close sleeves, under a giornea: an open-sided, sleeveless overgown of crimson or bottle-green silk velvet with the Italian pomegranate pattern, falling from the shoulders to the floor with a short train, edged with fur or brocade at the neck. No trailing sleeves.
- Hair: Fashionably plucked high forehead; hair drawn back smoothly and gathered into a coil at the back of the head, partly visible
- Headwear: Ghirlanda: a thick padded roll, like a fat wreath, covered in crimson silk and studded with pearls, set around the crown of the head. No veil, so the drawn-back hair and the high bare forehead show.
- Accessory: A large gold pendant jewel (fermaglio) with a pearl cluster, as wide as the palm, hanging at the base of the throat on its own short gold chain

MUST READ AT A GLANCE: Men: the cappuccio a mazzocchio (a padded ring-hat with a draped side and a long hanging becchetto) over a red lucco gown. Women: the pearl-studded padded ghirlanda roll over bare, drawn-back hair and a plucked high forehead.
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (mazzocchio hat); woman's headwear (ghirlanda)
DO NOT DRAW: beards on men (reads Byzantine Greek); tall brimmed Byzantine hat; women's veils, wimples or wound headcloths (read Byzantine, Egypt, Britain or Germany); floor-trailing hanging sleeves (reads Egypt-medieval); a hood enclosing the head and neck with a scalloped shoulder cape (reads Germany's Gugel); cardinal's red galero hat, clerical or papal robes (sacred); laurel wreath and red-cap 'Dante' costume cliches; the later balzo headdress (1450s-80s, Ferrara and Milan); condottiere armour or weapons; slashed Renaissance sleeves (too late; reserve for early modern); Medici family crest or balls emblem
```

Files (7): `outfit_m_italy_medieval.png`, `hair_m_italy_medieval.png`, `headwear_m_italy_medieval.png`, `outfit_f_italy_medieval.png`, `hair_f_italy_medieval.png`, `headwear_f_italy_medieval.png`, `accessory_f_italy_medieval.png`

**Send this place's files to Claude** before you start the next place.

### China: Northern Song Kaifeng

Costume Guide entry (the Culture value): **winged futou / crescent comb**

```
PAIR: china_medieval (Northern Song Kaifeng, 1075)
Place and time: Kaifeng, the Northern Song capital, c. 1075. This is the era of Shen Kuo, scholar-official and envoy to the Liao, and of the magnetic compass and movable type.

MAN
- Outfit: Ankle-length round-collared robe (yuanling pao) fastened at the right shoulder, with a white inner collar edge at the throat, moderately wide sleeves and a horizontal seam band (lan) across the knees. Plain, unpatterned silk in muted dark olive green or blue-grey, the colours of middle-ranking civil officials, not the bright colours of high ranks. Black cloth boots; a long, straight, sober silhouette.
- Hair: Topknot on the crown; neat hairline
- Facial hair: Long thin moustache and a tapered chin beard, the groomed Song scholar look
- Headwear: Zhijiao futou: a black lacquered-gauze cap with two stiff, straight, flat wings sticking out horizontally from the back. From wing tip to wing tip the cap is about one and a half times the shoulder width, inside the safe area, so the silhouette reads as a 'T'.
- Accessory: Black leather belt worn loose at the hips, set with plain square plaques of black horn or silver

WOMAN
- Outfit: Beizi: a long, straight, open-front coat to mid-calf. Its parallel front edges do not overlap and are edged with a narrow contrasting band; it has high side slits and narrow sleeves. It is worn over a moxiong chest wrap and a long pleated skirt, in pale, understated Song colours: dull grey-green celadon, ivory, pale coral or light blue.
- Hair: Gaoji: a tall bun piled high on the crown in one or two upright loops
- Headwear: Guanshu: a large arched crescent comb of ivory or lacquer, set upright across the front of the high bun
- Accessory: none

MUST READ AT A GLANCE: Men: the black futou cap with long, stiff, horizontal wings. Women: the tall bun crowned by a big upright crescent comb.
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (winged futou); woman's headwear (crescent comb)
DO NOT DRAW: Ming or Qing items: queue, skullcap, standing collars with metal buttons; official fish pouch (yudai), purple or vermilion high-rank robes, and jade rank belts; emperor's yellow robe, dragons; armour or weapons; Mongol/Yuan-style dress; Japanese eboshi or hitatare; conical straw hat (douli); collar closed right-over-left (zuoren); futou wings so long they break the sprite frame
```

Files (8): `outfit_m_china_medieval.png`, `hair_m_china_medieval.png`, `facialhair_m_china_medieval.png`, `headwear_m_china_medieval.png`, `accessory_m_china_medieval.png`, `outfit_f_china_medieval.png`, `hair_f_china_medieval.png`, `headwear_f_china_medieval.png`

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): 'coolie' stereotype or any facial caricature.

**Send this place's files to Claude** before you start the next place.

### Britain: Anglo-Saxon England

Costume Guide entry (the Culture value): **disc brooch / headrail**

```
PAIR: britain_medieval (Anglo-Saxon England, 1050)
Place and time: London under Edward the Confessor, c. 1050: a North Sea port trading wool and silver, where laws of the time mention merchants from Flanders, Normandy and the German Empire

MAN
- Outfit: Knee-length wool tunic (cyrtel) in madder red or woad blue, loosely bloused over a belt, with contrasting embroidered or tablet-woven bands at the neckline, cuffs and hem. Loose trousers are wrapped from ankle to knee with linen leg bindings (winingas). A plain rectangular wool cloak is draped over the left shoulder and fastened at the right shoulder with a small plain pin.
- Hair: Neck-length straight hair, centre-parted and cut evenly at the collar
- Facial hair: Long full moustache with a clean-shaven chin (the English look on the Bayeux Tapestry)
- Headwear: none
- Accessory: Large round silver disc brooch with interlace and niello decoration, about as wide as the face, worn high on the right shoulder. Draw the brooch alone, with no cloak behind it

WOMAN
- Outfit: Ankle-length undergown with tight long sleeves, under a slightly shorter overgown with wide bell sleeves, in soft red, blue or dark olive-green wool with embroidered cuff bands. A plain mantle hangs from the shoulders, fastened at the upper chest with a plain round gilt clasp.
- Hair: Long hair coiled low and close at the nape
- Headwear: Headrail: a long rectangular veil of COLOURED wool or linen (blue, brown-red or ochre, contrasting with the gown) laid over the head, wrapped once around the throat and draped down over the shoulders and back. It frames the face but never covers it. (This headwear hides all the hair, so no hair is drawn under it: bring its front edge right down to the hairline so no bald scalp shows.)
- Accessory: none

MUST READ AT A GLANCE: Men: a big round silver disc brooch high on the right shoulder and the long English moustache, with a cloak over a tunic. Women: a coloured headrail veil wrapped over the head and round the throat, above a bell-sleeved overgown.
LEAK ITEMS (draw these extra clear and true to the text): man's accessory (disc brooch); woman's headwear (headrail)
DO NOT DRAW: horned or winged helmets, a Viking-raider look; chainmail, Norman kite shields, any armour or weapons; crowns or royal regalia; monks' habits, nuns' wimples or any liturgical vestment; white headrail (reads Egypt's white izar and Germany's Kruseler at thumbnail size); a compact wound turban-headdress (reads Greece); hood with a long tail (Gugel) or long pointed shoes (Germany-medieval); frilled Kruseler veil (Germany-medieval); tall conical hennin or later Gothic headdresses; Robin Hood-style feathered cap and Lincoln green
```

Files (7): `outfit_m_britain_medieval.png`, `hair_m_britain_medieval.png`, `facialhair_m_britain_medieval.png`, `accessory_m_britain_medieval.png`, `outfit_f_britain_medieval.png`, `hair_f_britain_medieval.png`, `headwear_f_britain_medieval.png`

Notes for Claude's processing: the woman's headwear covers the hair (check on the bald head that no scalp shows under it).

**Send this place's files to Claude** before you start the next place.

When the last place is in, Claude runs the batch check in Unity.

## 18. Batch 6: premade characters: Aspasia, Ban Zhao, Arib al-Ma'muniyya, Muhammad al-Khwarizmi (16 images)

The random premade pool of days 2 and 3 (each ordinary slot has a small chance of one of them). Make them in the **Time Sorter Premades** Project (section 11), one new chat per character. Use prompt 9.10 for the neutral image, then 9.11 three times. Folder: `Raw/batch06-premades/`. Send each character's four images to Claude as soon as they are done.

#### Aspasia (`aspasia`)

- Claims: Periclean Athens (greece_ancient), 430 BCE. Age at the desk: about 40 (face band c). Base figure to attach: `premadebase_f_skin3.png`.
- Wears the claimed place's Costume Guide item: sakkos snood (headwear).
- In the game: pooled days 2-3.

```
Aspasia of Miletus, a learned woman of Periclean Athens, about 40, with an intelligent, composed face. Centre-parted wavy dark hair gathered into a patterned cloth sakkos snood that covers the crown and back of the head, a few curls at the temples. An ankle-length Ionic chiton of fine crinkled linen with short sleeves pinned along the upper arms, girdled and bloused at the waist; over it a saffron himation with a woven meander border wrapped diagonally over the left shoulder. Large gold disc earrings and a simple gold necklace. Plain strapped sandals.
```

Do not draw: seductive or 'courtesan' styling, goddess styling, a Roman stola.

Files: `premade_aspasia_neutral.png`, `premade_aspasia_happy.png`, `premade_aspasia_angry.png`, `premade_aspasia_worried.png`

#### Ban Zhao (`banzhao`)

- Claims: Eastern Han Luoyang (china_ancient), 105 CE. Age at the desk: about 60 (face band d). Base figure to attach: `premadebase_f_skin2.png`.
- Wears the claimed place's Costume Guide item: bi-disc pendant (accessory).
- In the game: pooled days 2-3.

```
Ban Zhao, historian and teacher at the Eastern Han court, about 60: a dignified elderly scholar with a kind, firm face and grey hair drawn smoothly back into a low looped bun at the nape, held with one plain silver hairpin. A floor-length quju wrap robe closed with the wearer's left panel over the right (a 'y' at the throat), layered collars with the innermost white, in sober deep brown or dark red silk with broad black cloud-scroll borders; huge sleeves that bag below the arm and narrow at the wrist; a dark cloth sash. A jade bi-disc pendant: a flat round disc of dark, dull grey-green jade with a hole in the centre, as wide as the palm, hanging high on the chest (its centre about a hand's width below the collarbones) on its own red silk cord round the neck.
```

Do not draw: court rank insignia, phoenix crowns, books or brushes in the hands.

Files: `premade_banzhao_neutral.png`, `premade_banzhao_happy.png`, `premade_banzhao_angry.png`, `premade_banzhao_worried.png`

#### Arib al-Ma'muniyya (`arib`)

- Claims: Abbasid Baghdad (iraq_medieval), 830 CE. Age at the desk: about 33 (face band b). Base figure to attach: `premadebase_f_skin3.png`.
- Wears the claimed place's Costume Guide item: veil with isaba (headwear).
- In the game: pooled days 2-3.

```
Arib al-Ma'muniyya, celebrated singer, poet and composer of Abbasid Baghdad, early 30s, confident and elegant. A long-sleeved, ankle-length saffron silk qamis with gold-embroidered tiraz bands (abstract pattern, no letters) at the upper arms, full sirwal trousers gathered at the ankle, and a light dusty-coral mantle over the shoulders only. An opaque light khimar veil over the black hair, bound at the brow by an 'isaba headband of small abstract gold patterns (no letters), with two glossy black S-shaped side-curls on the cheeks. Gold earrings and a choker of large pearls.
```

Do not draw: a face veil, real or fake Arabic letters, a musical instrument in the hands.

Review only (Saleh and Claude; never paste into ChatGPT): Orientalist or 'harem' styling.

Files: `premade_arib_neutral.png`, `premade_arib_happy.png`, `premade_arib_angry.png`, `premade_arib_worried.png`

#### Muhammad al-Khwarizmi (`khwarizmi`)

- Claims: Abbasid Baghdad (iraq_medieval), 830 CE. Age at the desk: about 50 (face band c). Base figure to attach: `premadebase_m_skin3.png`.
- Wears the claimed place's Costume Guide item: qalansuwa (headwear).
- In the game: pooled days 2-3.

```
Muhammad ibn Musa al-Khwarizmi, scholar of the House of Wisdom in Baghdad, about 50, with a thoughtful face and a full, neatly rounded dark beard and moustache. A tall, stiff black qalansuwa cap wound at its base with a white turban; a front-opening, calf-length black durra'a robe over a pale linen qamis, with broad gold tiraz bands (abstract strokes and knots, no letters) around the upper sleeves; a dark taylasan scholar's shawl over both shoulders with its ends falling down the front; soft leather boots.
```

Do not draw: astrolabes, books or scrolls in the hands, real or fake Arabic letters.

Files: `premade_khwarizmi_neutral.png`, `premade_khwarizmi_happy.png`, `premade_khwarizmi_angry.png`, `premade_khwarizmi_worried.png`

**Send to Claude.**

## 19. Batch 7: Day 3 places (88 images)

Day 3 adds the Early modern era, Japan and Germany, and it is the first day on which a liar's dress can leak. (Tokugawa Edo is closed by a rule on day 3, but rule-breakers still claim it, so it is needed.) Folder: `Raw/batch07-day-3-places/`. Start a new chat for each place, send its PAIR block with prompt 9.0, then make each file in its list. **Send each place's files to Claude as soon as that place is done**, and wait for the go-ahead on the first place of the batch.

### Japan: Kofun Yamato

Costume Guide entry (the Culture value): **mizura / magatama beads**

```
PAIR: japan_ancient (Kofun Yamato, 450 CE)
Place and time: Yamato (the Nara–Osaka plain) in the Kofun period, c. 450 CE, the age of the great keyhole tombs and haniwa clay figures

MAN
- Outfit: Kinu: a hip-length jacket of undyed white hemp with narrow tube sleeves, fastened at the chest with two small cloth bows and belted at the waist. Loose white hakama trousers are gathered and tied just below each knee with cords (ashi-yui), so they balloon above. Bare feet or simple straw sandals; optional simple red triangle or dot trim.
- Hair: Mizura: hair parted in the centre, with each side looped into a figure-eight bundle tied beside the ears and hanging to the jaw
- Facial hair: none
- Headwear: none
- Accessory: Necklace of dark, dull grey-green jade magatama (comma-shaped beads) strung with cylindrical kudatama beads

WOMAN
- Outfit: White hemp kinu jacket with narrow sleeves, fastened with small cloth bows and belted at the waist, over a mo: a long wrapped skirt to the ankle, sometimes pleated or striped in red and white.
- Hair: Flat 'board' chignon: hair gathered on top of the head into a long, flat bun lying front to back, tied at its middle, with a small comb at the front
- Headwear: none
- Accessory: Multi-strand necklace of dark grey-green magatama and round blue glass beads

MUST READ AT A GLANCE: Men: mizura hair loops beside the ears (the clearest silhouette cue) and a necklace of dark grey-green comma-shaped magatama beads over a short belted jacket. Women: the flat board chignon and a necklace of magatama beads over the jacket.
LEAK ITEMS (draw these extra clear and true to the text): man's hair (mizura); woman's accessory (magatama beads)
DO NOT DRAW: keiko armour, swords, helmets (the famous haniwa warrior); shrine-maiden ritual sash (osuhi), bronze mirrors, anything liturgical; a single oversized jewel styled as the imperial regalia; red face paint (seen on haniwa figures); later kimono with a wide obi, or Heian court robes; Chinese long robes with huge sleeves, or topknot caps
```

Files (5): `outfit_m_japan_ancient.png`, `hair_m_japan_ancient.png`, `outfit_f_japan_ancient.png`, `hair_f_japan_ancient.png`, `accessory_f_japan_ancient.png` (already made in Batch 1: `accessory_m_japan_ancient.png`)

Notes for Claude's processing: mask the woman's hair ornament before recolouring: small comb.

**Send this place's files to Claude** before you start the next place.

### Germany: Germania

Costume Guide entry (the Culture value): **Suebian knot / amber beads**

```
PAIR: germany_ancient (Germania, 98 CE)
Place and time: Free Germania east of the Rhine and north of the Danube, c. 98 CE, as described in Tacitus' Germania

MAN
- Outfit: Tight-fitting, long-sleeved, knee-length wool tunic and close-fitting long trousers, as in the Thorsberg bog finds, in undyed brown and deep woad blue with a diamond-twill texture. A short, dark rectangular wool cloak with a fringed, tablet-woven border is pinned at the right shoulder with a bronze bow brooch (fibula). Leather wrap shoes.
- Hair: Suebian knot: hair combed back and to the side and twisted into a tight knot on the right side of the head, above the temple
- Facial hair: Full short beard
- Headwear: none
- Accessory: none

WOMAN
- Outfit: Loose, sleeveless, ankle-length linen tube dress (peplos type) pinned at each shoulder, leaving the arms bare, with deep wine-red bands at the neck and hem as Tacitus notes. A plain undyed wool shawl drapes round the shoulders, and the waist is tied with a woven band. Draw it modestly: fuller and draped, not a fitted sheath.
- Hair: Long hair gathered into a low knot at the nape, bound with a narrow woven wool hair-band wound round it
- Headwear: none
- Accessory: Necklace of chunky orange Baltic amber and glass beads

MUST READ AT A GLANCE: For men, the Suebian side-knot hairstyle with a full beard. For women, a chunky Baltic amber necklace over a linen dress with deep wine-red trim.
LEAK ITEMS (draw these extra clear and true to the text): man's hair (Suebian knot); woman's accessory (amber beads)
DO NOT DRAW: horned or winged helmets; Viking-age or opera-costume looks; fur trims, pelts or fur loincloths; spears, shields or any weapons; Roman armour; gold torc or bright multicoloured checks (that is Britain-ancient); fitted white strapped sheath dress or broad bead collar (reads as Egypt-ancient); snood or hairnet over the bun (too close to the Greek sakkos); Viking-age oval brooches with bead strings between them (the wrong period); Roman toga or Roman-style jewellery
```

Files (6): `outfit_m_germany_ancient.png`, `hair_m_germany_ancient.png`, `facialhair_m_germany_ancient.png`, `outfit_f_germany_ancient.png`, `hair_f_germany_ancient.png`, `accessory_f_germany_ancient.png`

Notes for Claude's processing: mask the woman's hair ornament before recolouring: woven wool hair-band.

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): Viking or Wagnerian-opera stereotypes; barbarian caricature.

**Send this place's files to Claude** before you start the next place.

### Japan: Muromachi Kyoto

Costume Guide entry (the Culture value): **eboshi cap / ichime-gasa**

```
PAIR: japan_medieval (Muromachi Kyoto, 1400)
Place and time: Kyoto under the Ashikaga shogunate, c. 1400: Kitayama culture, Noh theatre, and Japanese folding fans exported to Ming China

MAN
- Outfit: Hitatare of hemp: a cross-collared jacket, closed left-over-right, with very wide square sleeves whose cuffs are gathered by a threaded cord. It is tucked into matching ankle-length pleated hakama, with small decorative cord tufts (kikutoji) on the seams and chest ties. Persimmon brown, indigo or olive, with no family crests. A folding fan (sensu) with black lacquered ribs is tucked, closed, into the waist ties at the front.
- Hair: Motodori topknot at the crown over a full head of hair (no shaved pate)
- Facial hair: Short moustache and a small chin tuft
- Headwear: Momi-eboshi: a tall, soft cap of black, lightly lacquered gauze or paper, crumpled and leaning slightly forward
- Accessory: none

WOMAN
- Outfit: Tsubo-shozoku travel dress: an ankle-length patterned kosode, closed left-over-right, under an outer robe hitched up at the hips so its hem ends at mid-calf, tied with a narrow cloth sash, with a small tie-dyed drawstring purse (kinchaku) hanging from it. Muted tie-dyed or small-motif patterns in indigo, rust and cream.
- Hair: Long centre-parted hair worn down, drawn smoothly back behind the ears and tied once loosely at the nape; the long tail falls straight down the back, hidden from the front
- Headwear: Ichime-gasa: a wide-brimmed lacquered sedge hat with a tall knob crown, worn WITHOUT its veil so the face is fully visible
- Accessory: none

MUST READ AT A GLANCE: Men: a tall, soft, crumpled black eboshi cap with a wide-sleeved hitatare. Women: the wide, knob-crowned ichime-gasa travel hat.
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (eboshi cap); woman's headwear (ichime-gasa)
DO NOT DRAW: armour, katana, samurai helmets, ninja imagery; Heian junihitoe or court robes (aristocratic and the wrong period); Buddhist monk kesa or yamabushi robes (liturgical); amulet pendants (kake-mamori), which are religious; family crests (kamon) on the hitatare, which can read as a warrior retainer; wide Edo obi or geisha look (anachronistic); Chinese futou with horizontal wings; a stiff, smooth, upright cap with a turban at its base (reads Iraq's qalansuwa); collar closed right-over-left (the burial style); the hanging veil (mushi-no-tareginu) that hides the face
```

Files (7): `outfit_m_japan_medieval.png`, `hair_m_japan_medieval.png`, `facialhair_m_japan_medieval.png`, `headwear_m_japan_medieval.png`, `outfit_f_japan_medieval.png`, `hair_f_japan_medieval.png`, `headwear_f_japan_medieval.png`

**Send this place's files to Claude** before you start the next place.

### Germany: Holy Roman Empire (Rhineland)

Costume Guide entry (the Culture value): **Gugel hood / Kruseler veil**

```
PAIR: germany_medieval (Holy Roman Empire (Rhineland), 1440)
Place and time: The Rhine cities of Strasbourg and Mainz, c. 1440, when Johannes Gutenberg was running his first experiments with movable metal type

MAN
- Outfit: Calf-length gown (Tappert) in deep blue or bottle-green wool with regular organ-pipe pleats, a high standing collar and wide, fur-trimmed sleeves, belted low on the hips with a leather belt with a leather purse hanging from it, worn over tight hose. Moderately long pointed shoes (Schnabelschuhe).
- Hair: Chin-length straight hair with a full fringe
- Facial hair: none (clean-shaven, the 15th-century norm)
- Headwear: Gugel: a close hood enclosing the head and neck, with the face opening fully clear. It has a short shoulder cape cut into scalloped leaf-shaped points (dags). (This headwear hides all the hair, so no hair is drawn under it: bring its front edge right down to the hairline so no bald scalp shows.)
- Accessory: none

WOMAN
- Outfit: Ankle-length, high-waisted wool gown in deep blue or bottle green with long sleeves fitted tight to the wrist, belted just under the bust with a wide girdle, the skirt trailing on the ground. A dark mantle hangs from the shoulders, closed at the chest by a plain cord (not held in the hands). The neckline is closed with a round gold clasp.
- Hair: Hair plaited and pinned up close to the head
- Headwear: Kruseler: a white linen veil edged with several rows of tightly ruffled frills that frame the forehead and cheeks like a halo, with no chin band (This headwear hides all the hair, so no hair is drawn under it: bring its front edge right down to the hairline so no bald scalp shows.)
- Accessory: none

MUST READ AT A GLANCE: Men: the Gugel hood with its scalloped shoulder cape, the face fully clear. Women: the Kruseler veil with its many-layered frilled edge.
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (Gugel hood); woman's headwear (Kruseler veil)
DO NOT DRAW: armour, crowns or noble regalia; clerical or monastic robes; a pointed or funnel-shaped hat, or any marker or badge on the clothing; Anglo-Saxon disc-brooch cloak, long moustache or headrail veil (Britain-medieval); tall conical hennin (Burgundian); Italian padded ring-hat (mazzocchio) or the women's ghirlanda roll (Italy-medieval); an ankle-length red citizen's gown (reads as the Florentine lucco); Crusader tabards or crosses
```

Files (6): `outfit_m_germany_medieval.png`, `hair_m_germany_medieval.png`, `headwear_m_germany_medieval.png`, `outfit_f_germany_medieval.png`, `hair_f_germany_medieval.png`, `headwear_f_germany_medieval.png`

Notes for Claude's processing: the man's headwear covers the hair (check on the bald head that no scalp shows under it); the woman's headwear covers the hair (check on the bald head that no scalp shows under it).

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): the 'Judenhut' or any discriminatory marker.

**Send this place's files to Claude** before you start the next place.

### Egypt: Ottoman Egypt

Costume Guide entry (the Culture value): **red-crown turban / tartur**

```
PAIR: egypt_earlymodern (Ottoman Egypt, 1700)
Place and time: Ottoman Cairo, c. 1700 (Cairo as hub of the Red Sea coffee trade; the yearly cutting of the Khalij canal dam at the Nile flood)

MAN
- Outfit: Ankle-length quftan (kaftan) of saffron or dusty-coral silk with small woven stripes or motifs, crossed over the chest. Over it a long, wide-sleeved gibba coat of plain dark broadcloth (bottle green or deep blue), open at the front. Wide trousers and pointed red leather slippers.
- Hair: Short-cropped
- Facial hair: Full beard, trimmed short and rounded, with a moustache
- Headwear: Crisp white muslin turban wound in smooth, rounded folds around a red felt cap, with the domed red crown of the cap rising clearly above the top of the turban so it shows from the front. The turban cloth is pure white. (This headwear hides all the hair, so no hair is drawn under it: bring its front edge right down to the hairline so no bald scalp shows.)
- Accessory: Patterned Kashmir or Indian wool shawl wound as a broad sash (hizam) at the waist

WOMAN
- Outfit: Floor-length entari robe of brocaded silk (for example gold on deep coral). Its long sleeves are slit from the elbow to hang open. It is worn over a thin, opaque white chemise and wide shintiyan trousers and belted at the hips with a jewelled belt.
- Hair: Long hair in many thin plaits falling behind the shoulders, spread wide so they show on both sides of the neck
- Headwear: Tartur: a tall headdress that is wider at the top than at the base, wrapped in black or brown cloth woven with gold stripes. The face is fully uncovered.
- Accessory: Long gold earrings, each ending in a cluster of plain gold discs with raised rims (no face, letters or marks)

MUST READ AT A GLANCE: A pure-white muslin turban wound around a red cap, with the red crown showing (women: the tall, gold-striped tartur headdress)
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (red-crown turban); woman's headwear (tartur)
DO NOT DRAW: Istanbul court kavuks and giant ceremonial turbans; janissary bork caps or any Ottoman military dress; Mamluk-bey armour and weapons; fez worn alone without a turban (a 19th-century look); coloured or checked turban cloth, striped camel-hair 'aba cloaks, keffiyeh/agal (read as Iraq); a small red cap wrapped in a patterned kerchief for the woman (reads as Greece-earlymodern); face veils that hide the face; Mamluk floor-length sleeves (read as Egypt-medieval)
```

Files (9): `outfit_m_egypt_earlymodern.png`, `hair_m_egypt_earlymodern.png`, `facialhair_m_egypt_earlymodern.png`, `headwear_m_egypt_earlymodern.png`, `accessory_m_egypt_earlymodern.png`, `outfit_f_egypt_earlymodern.png`, `hair_f_egypt_earlymodern.png`, `headwear_f_egypt_earlymodern.png`, `accessory_f_egypt_earlymodern.png`

Notes for Claude's processing: the man's headwear covers the hair (check on the bald head that no scalp shows under it); the woman's hair hangs behind the shoulders (Claude splits off a hair-back layer).

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): odalisque or harem imagery.

**Send this place's files to Claude** before you start the next place.

### Iraq / Mesopotamia: Ottoman Baghdad

Costume Guide entry (the Culture value): **checked turban / futa shawl**

```
PAIR: iraq_earlymodern (Ottoman Baghdad, 1720)
Place and time: Baghdad under the governor Hasan Pasha (1704-1723), whose household grew into the Mamluk regime, c. 1720 (notaries recording contracts for the qadi court registers)

MAN
- Outfit: Ankle-length zaboun robe of striped cotton-silk (cream with narrow crimson stripes), wrapped across the chest and belted, with a long brass pen case (qalamdan) tucked upright into the belt. Over it is an 'aba: a wide, square-shouldered cloak of camel-hair wool with arm slits, woven in broad vertical stripes of dark brown and cream, falling open at the front to mid-calf.
- Hair: Short-cropped
- Facial hair: Full beard with a moustache
- Headwear: Loosely wound turban of clearly coloured, patterned cloth (cream with bold red-brown checks), with a fringed end tucked in at the side. It is never plain white and never shows a red crown. (This headwear hides all the hair, so no hair is drawn under it: bring its front edge right down to the hairline so no bald scalp shows.)
- Accessory: none

WOMAN
- Outfit: Ankle-length, long-sleeved dress of striped silk-cotton (indigo and dusty coral) held by a silver-link belt, over trousers. A dark, sleeveless 'aba cloak is worn open over the shoulders.
- Hair: Long hair in two braids pinned up close to the head
- Headwear: Black crepe head-shawl (futa) wrapped over the hair and round the neck, bound at the brow by a narrow black 'asaba band. The face is uncovered. (This headwear hides all the hair, so no hair is drawn under it: bring its front edge right down to the hairline so no bald scalp shows.)
- Accessory: Gold qilada necklace hung with a row of plain gold discs with raised rims (no face, letters or marks) and teardrop pendants

MUST READ AT A GLANCE: The square-shouldered camel-hair 'aba cloak with broad brown-and-cream vertical stripes (women: the black crepe head-shawl wrapped over the hair and round the neck, with a dark 'aba worn open over the shoulders)
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (checked turban); woman's headwear (futa shawl)
DO NOT DRAW: Cairo's pure-white turban over a red cap with a kaftan and gibba (reads as Egypt-earlymodern); fez or tarboush; the Safavid Qizilbash red-staved taj (a sectarian and military emblem); janissary or other Ottoman military dress; face veils that hide the face; khanjar daggers or any weapons; Persian royal court dress; crescent-and-star motifs in jewellery (can read as a later Ottoman state emblem)
```

Files (8): `outfit_m_iraq_earlymodern.png`, `hair_m_iraq_earlymodern.png`, `facialhair_m_iraq_earlymodern.png`, `headwear_m_iraq_earlymodern.png`, `outfit_f_iraq_earlymodern.png`, `hair_f_iraq_earlymodern.png`, `headwear_f_iraq_earlymodern.png`, `accessory_f_iraq_earlymodern.png`

Notes for Claude's processing: the man's headwear covers the hair (check on the bald head that no scalp shows under it); the woman's headwear covers the hair (check on the bald head that no scalp shows under it).

**Send this place's files to Claude** before you start the next place.

### Greece: Ottoman Ioannina

Costume Guide entry (the Culture value): **kalpak / silver chest chains**

```
PAIR: greece_earlymodern (Ottoman Ioannina, 1700)
Place and time: Greek Orthodox merchant community of Ioannina (Epirus) under Ottoman rule, c. 1700

MAN
- Outfit: Ankle-length anteri (caftan) of striped silk-cotton (alaca) wrapped across the chest. Over it a long dark-blue or black wool overcoat (kavadi/gouna) with loose sleeves, lined and edged with fox or lamb fur at the collar and all down the front. Black leather shoes, the colour prescribed for Christians.
- Hair: Cropped short
- Facial hair: Full beard with a moustache
- Headwear: Tall black lambskin kalpak: a brimless, clearly fur-textured cap with a softly rounded top, worn instead of a turban as the typical headgear of Greek Christian merchants
- Accessory: Wide striped silk-cotton sash (zonari) wound several times around the waist

WOMAN
- Outfit: Long open-fronted anteri of striped silk (alatzas) over a white chemise with wide sleeves and a full skirt. Over it a short, sleeveless, dark velvet waistcoat (zipouni) embroidered with gold cord. A belt at the waist is closed with a pafti: a large, ornate double-plate silver-filigree buckle (Ioannina silverwork).
- Hair: Long hair plaited into two braids that fall forward over the shoulders onto the chest
- Headwear: Small red cap wrapped with a patterned silk headscarf (tsemberi) tied at the back. The braids and face stay visible.
- Accessory: Silver chest chains: four or five rows of fine silver chains hanging in festoons across the upper chest from two round silver-filigree rosettes on the collarbones, the rosettes joined by a silver chain round the back of the neck; the chains are strung with round silver filigree beads (Ioannina silverwork)

MUST READ AT A GLANCE: Men: the tall, rounded, black lambskin kalpak with a full beard, over a long dark coat edged with fur at the collar and down the front. Women: rows of fine silver chains festooned across the upper chest over a dark gold-corded velvet waistcoat, with a red cap wrapped in a patterned headscarf.
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (kalpak); woman's accessory (silver chest chains)
DO NOT DRAW: white turban or green garments (reserved for Muslims under Ottoman sumptuary law; also reads as Ottoman Egypt or Iraq); red kalpak (forbidden to Christians by a 1662 regulation) and sable fur (restricted for non-Muslims); mitre-shaped or two-pointed kalpaks (a mitre shape reads as clergy); a square, stiff-gauze tall cap or a pale wide-sleeved robe (reads as Ming China); fustanella kilt or tasselled fez (19th century; reserve for industrial); Janissary or Ottoman military uniforms, yataghan or pistols in the sash; Orthodox clergy robes, priest's hat, crosses (sacred); Venetian or Italian Renaissance dress (would read as Venetian Crete or Italy); sultan or court dress (royalty)
```

Files (9): `outfit_m_greece_earlymodern.png`, `hair_m_greece_earlymodern.png`, `facialhair_m_greece_earlymodern.png`, `headwear_m_greece_earlymodern.png`, `accessory_m_greece_earlymodern.png`, `outfit_f_greece_earlymodern.png`, `hair_f_greece_earlymodern.png`, `headwear_f_greece_earlymodern.png`, `accessory_f_greece_earlymodern.png`

**Send this place's files to Claude** before you start the next place.

### Italy: Sforza Milan

Costume Guide entry (the Culture value): **red berretta / lenza**

```
PAIR: italy_earlymodern (Sforza Milan, 1495)
Place and time: Milan under Ludovico Sforza, while Leonardo da Vinci painted the Last Supper and designed machines, c. 1495

MAN
- Outfit: Knee-length dusty-coral wool pitocco, a short, belted overgown with a full pleated skirt, worn when long gowns were the norm. At the neck the doublet collar shows a finely gathered white linen shirt. A small leather-bound notebook hangs from the belt on a cord. Close-fitting dark hose and soft round-toed shoes.
- Hair: Zazzera: straight, chin-to-shoulder-length bob with a blunt fringe across the brow
- Facial hair: none (clean-shaven, the Italian norm c. 1495)
- Headwear: Small, soft, round red berretta cap worn straight on the head, with no brim and no feather
- Accessory: none

WOMAN
- Outfit: Square-necked gown (gamurra) in deep blue or crimson. Its contrasting sleeves are laced on at the shoulder and elbow, so puffs of the white camicia show through the gaps. A sbernia mantle is draped over one shoulder.
- Hair: Centre-parted, smooth and flat over the ears, with the rest bound into one very long braid down the back (coazzone), hidden from the front. No hat.
- Headwear: Lenza: a finger-wide dark velvet band worn straight across the forehead, with a large dark-red stone set in gold, as big as the whole eye, at the centre of the brow
- Accessory: Long necklace of dark jet beads looped twice around the neck

MUST READ AT A GLANCE: Lenza: a finger-wide dark brow band with a large dark-red jewel at the centre, worn straight across the forehead over smooth, hatless hair (men: a small soft red berretta over a shoulder-length bob, with a dusty-coral pitocco tunic)
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (red berretta); woman's headwear (lenza)
DO NOT DRAW: Leonardo's old long white 'wizard' beard (a later image; also blurs the Greek beard tell); any beard on the man, a wide flat tilted Barett, a fur-collared Schaube or gold chains (read as Germany-earlymodern); any hat on the woman (the German woman wears the Barett); Galileo-era black Spanish dress with ruff or falling collar (reads northern Europe); Tudor-style flat cap with brim and feather (reads Britain); a cloak slung over one shoulder on the man (duplicates Britain); Landsknecht slashing and puffing, armour, weapons; Venetian carnival masks; clerical or cardinal robes (sacred); Sforza or ducal regalia (royalty)
```

Files (7): `outfit_m_italy_earlymodern.png`, `hair_m_italy_earlymodern.png`, `headwear_m_italy_earlymodern.png`, `outfit_f_italy_earlymodern.png`, `hair_f_italy_earlymodern.png`, `headwear_f_italy_earlymodern.png`, `accessory_f_italy_earlymodern.png`

**Send this place's files to Claude** before you start the next place.

### China: Late Ming Suzhou

Costume Guide entry (the Culture value): **square gauze cap / baotou**

```
PAIR: china_earlymodern (Late Ming Suzhou, 1635)
Place and time: Suzhou and Jiangnan in the late Ming, c. 1635, during the printing and publishing boom that produced Song Yingxing's illustrated technology encyclopedia Tiangong Kaiwu (1637)

MAN
- Outfit: Daopao: ankle-length cross-collared robe closed left panel over right, with very wide sleeves and a broad white protective strip (huling) along the collar edge. Plain pale blue or light grey silk or cotton; black cloth shoes. A lay scholar's robe, not a priest's vestment.
- Hair: Topknot on the crown, with a black horsehair net headband (wangjin) across the forehead
- Facial hair: Neatly trimmed moustache and a short-to-medium, neat chin beard, as in Ming gentry portraits
- Headwear: Sifang pingding jin: a tall, square, box-like, flat-topped cap of stiff black gauze with visible corners, the Ming scholar's and commoner's 'four-cornered' cap
- Accessory: Sitao: a blue or black silk-cord sash tied at the waist, with long tasselled ends hanging to the knee

WOMAN
- Outfit: Long ao jacket reaching the knee, with a standing collar and a centre-front opening fastened at the collar by gold interlocking zimu buttons. Its pipa-shaped sleeves narrow at the cuff. It is worn over a mamianqun, a pleated 'horse-face' skirt with flat front and back panels. Pale blue or apricot jacket; the skirt is plain white or very pale, the Chongzhen-era fashion, with a narrow woven band near the hem.
- Hair: Rounded bun set high and slightly flattened on the crown, held with a few gold hairpins
- Headwear: Baotou (tougu): a broad black satin band worn low across the brow, with a small jade or pearl ornament at the centre
- Accessory: none

MUST READ AT A GLANCE: Tall, square, black gauze scholar's cap (women: the broad black satin baotou band worn low across the brow, over a jacket with a standing collar and gold buttons)
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (square gauze cap); woman's headwear (baotou)
DO NOT DRAW: Qing queue, skullcap or horse-hoof cuffs (wrong dynasty); Mandarin rank squares (buzi), python or dragon robes, the official winged wusha cap; Daoist priest vestments. Despite its name the daopao here is a lay robe, so no bagua, taiji or cranes.; long drooping moustaches; white robes for the man (read as mourning); rounded fur-textured caps or dark fur-edged coats (read as Greece-earlymodern); jinbu waist pendants hanging to the knee (too close to Japan's tasselled cord belt); Japanese chonmage topknot or kamishimo; conical straw hat (douli); Collar closed right-over-left; Qipao (a 20th-century garment)
```

Files (8): `outfit_m_china_earlymodern.png`, `hair_m_china_earlymodern.png`, `facialhair_m_china_earlymodern.png`, `headwear_m_china_earlymodern.png`, `accessory_m_china_earlymodern.png`, `outfit_f_china_earlymodern.png`, `hair_f_china_earlymodern.png`, `headwear_f_china_earlymodern.png`

Notes for Claude's processing: mask the man's hair ornament before recolouring: black horsehair net headband (wangjin); mask the woman's hair ornament before recolouring: gold hairpins.

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): Fu Manchu caricature or caricatured features; 'coolie' stereotype.

**Send this place's files to Claude** before you start the next place.

### Japan: Tokugawa Edo

Costume Guide entry (the Culture value): **chasen-mage / kazuki veil**

```
PAIR: japan_earlymodern (Tokugawa Edo, 1610)
Place and time: Edo and Sunpu at the founding of the Tokugawa shogunate, c. 1610, in Tokugawa Ieyasu's time. He received a Spanish mechanical clock in 1611.

MAN
- Outfit: Kataginu and hakama over a kosode. The kataginu is a sleeveless over-vest with straight, squared shoulders that extend a little past the wearer's own. They lie flat, not yet the stiff, flaring wings of later Edo. The matching pleated hakama are in indigo, grey or brown hemp. Under them is a kosode with small sleeve openings and a fine repeating pattern, collar crossing left over right. No crests. An inro (a small stacked lacquered case) hangs at the hip on a silk cord from the hakama waist ties, held by a carved toggle.
- Hair: Chasen-mage: shaved pate (sakayaki), with the remaining hair gathered at the crown into a short upright topknot bound with white cord like a tea whisk
- Facial hair: Thin moustache and a small chin beard
- Headwear: none
- Accessory: none

WOMAN
- Outfit: Keicho-style kosode: an ankle-length straight robe with small wrist openings and rounded sleeve bottoms. Dense small motifs in divided zones (tie-dye, embroidery, gold leaf) sit on a dark ground of black, deep red or brown. Collar left over right; slim silhouette with no wide obi. Round the hips, a Nagoya-obi: a braided silk cord belt wound several times and tied in front, with long tassels hanging to the knee.
- Hair: Long centre-parted hair drawn to the nape and looped once into a soft tamamusubi fold, tied with white paper cord, with the ends hanging down the back (hidden from the front)
- Headwear: Kazuki: a second kosode worn over the head as a veil. Its collar edge lies across the top of the forehead, it frames the fully visible face and the hair at the sides, and the robe falls over both shoulders to the upper arms. Dark ground (black, deep red or brown) with dense small motifs in divided zones, in colours different from the kosode worn on the body. The face is fully uncovered.
- Accessory: none

MUST READ AT A GLANCE: Men: the bare, shaved-pate tea-whisk topknot (chasen-mage) with a sleeveless kataginu vest with flat, squared shoulders over a kosode. Women: the kazuki, a densely patterned kosode worn over the head and shoulders as a veil, framing the face.
LEAK ITEMS (draw these extra clear and true to the text): man's hair (chasen-mage); woman's headwear (kazuki veil)
DO NOT DRAW: stiff, whalebone-stiffened winged kataginu shoulders (a mid-Edo, Genroku-era development); Katana, wakizashi or any sword (samurai wore two; omit them); Armour or kabuto helmets; Tokugawa hollyhock (aoi) crest or any clan crest; Wide obi with a large back bow, or geisha/oiran white makeup and hairpins (later Edo); Chinese square cap or cross-collar daopao with huge sleeves; Collar closed right-over-left; Late-Edo folded-forward chonmage (use the upright chasen-mage)
```

Files (6): `outfit_m_japan_earlymodern.png`, `hair_m_japan_earlymodern.png`, `facialhair_m_japan_earlymodern.png`, `outfit_f_japan_earlymodern.png`, `hair_f_japan_earlymodern.png`, `headwear_f_japan_earlymodern.png`

Notes for Claude's processing: mask the man's hair ornament before recolouring: white cord; mask the woman's hair ornament before recolouring: white paper cord.

**Send this place's files to Claude** before you start the next place.

### Britain: Elizabethan England

Costume Guide entry (the Culture value): **capotain hat / hat over coif**

```
PAIR: britain_earlymodern (Elizabethan England, 1600)
Place and time: The Royal Exchange in London, c. 1600, the year the East India Company received its charter

MAN
- Outfit: Close-fitting black doublet with small shoulder wings, a short tabbed skirt and a long row of small buttons. It is worn with padded knee breeches (Venetians), dark stockings and flat shoes, and a short black cloak slung over one shoulder. At the neck is a large starched white cartwheel ruff.
- Hair: Short hair brushed up and back from the forehead
- Facial hair: Pointed 'pick-a-devant' beard with an upturned moustache
- Headwear: Tall-crowned black felt capotain hat with a narrow brim and a plain hatband
- Accessory: none

WOMAN
- Outfit: Black or russet gown with a stiff, long, pointed bodice and a full skirt over a modest hip roll, not a court wheel farthingale. A wide starched white lace-edged ruff sits at the neck, with matching white linen cuffs.
- Hair: Hair drawn up and back, pinned close to the head, with a smooth front edge at the hairline
- Headwear: Tall-crowned black felt hat with a narrow brim, worn over a close white linen coif whose edge frames the face (the London citizen's wife's hat) (This headwear hides all the hair, so no hair is drawn under it: bring its front edge right down to the hairline so no bald scalp shows.)
- Accessory: A slim silver-link girdle worn at the waist, with a silver pomander ball as big as a fist hanging from it on a chain to the knee

MUST READ AT A GLANCE: A large starched white cartwheel ruff under a tall-crowned, narrow-brimmed black capotain hat
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (capotain hat); woman's headwear (hat over coif)
DO NOT DRAW: royal or court regalia: crowns, jewel-encrusted Elizabeth I gowns, huge wheel farthingales; Henry VIII-style flat cap and wide fur-collared gown (reads as Germany c. 1525); slashed-and-puffed Landsknecht sleeves, a wide flat tilted Barett or heavy gold chains (Germany earlymodern); rapiers, swords, armour; a buckle on the hatband (buckled hats are a later myth); Tudor rose badges or royal livery
```

Files (8): `outfit_m_britain_earlymodern.png`, `hair_m_britain_earlymodern.png`, `facialhair_m_britain_earlymodern.png`, `headwear_m_britain_earlymodern.png`, `outfit_f_britain_earlymodern.png`, `hair_f_britain_earlymodern.png`, `headwear_f_britain_earlymodern.png`, `accessory_f_britain_earlymodern.png`

Notes for Claude's processing: the woman's headwear covers the hair (check on the bald head that no scalp shows under it).

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): 'Puritan' caricature.

**Send this place's files to Claude** before you start the next place.

### Germany: Renaissance Nuremberg

Costume Guide entry (the Culture value): **Barett**

```
PAIR: germany_earlymodern (Renaissance Nuremberg, 1525)
Place and time: Nuremberg c. 1525: Duerer's city and Europe's hub of printing and precision instruments (Henlein's early portable spring-driven clocks, Behaim's globe), the year the city council adopted the Reformation as Luther's printed pamphlets spread

MAN
- Outfit: Knee-length Schaube: a wide, open, pleated overcoat with a broad fur or velvet shawl collar, worn over a doublet whose sleeves are slashed and puffed to show a contrasting lining. Pleated white shirt with an embroidered stand-up neckband. Hose, and wide square-toed 'cow-mouth' shoes (Kuhmaulschuhe).
- Hair: Shortened Kolbe of the 1520s: hair cut short, level with the ear-lobes, with a short straight fringe
- Facial hair: Full, rounded beard with a moustache, trimmed fairly short (the new German fashion of the 1520s)
- Headwear: Wide, flat black velvet Barett with a slashed brim, worn tilted to one side
- Accessory: Heavy gold link chain draped across the shoulders and chest

WOMAN
- Outfit: Red or black wool gown with a laced bodice, a full pleated skirt and sleeves banded with horizontal puffs and slashes. It is worn over a high-necked pleated white linen shirt with a gold-embroidered collar band, and a short black velvet shoulder cape (Goller) covers the shoulders.
- Hair: Hair plaited and gathered into a gold-thread hairnet (Haarhaube) at the back of the head
- Headwear: Wide, flat Barett tilted to one side over the hairnet, trimmed with a small white feather
- Accessory: Several layered gold chains across the chest

MUST READ AT A GLANCE: The wide, flat, slashed Barett worn tilted (men: with a broad fur-collared Schaube and a full beard; women: with the black Goller shoulder cape)
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (Barett); woman's headwear (Barett)
DO NOT DRAW: a clean-shaven man with a shoulder-length bob under a small round cap, or a pink knee-length gown (reads as Italy-earlymodern); Landsknecht mercenary gear: armour, halberds, two-handed swords, exaggerated codpieces, huge feather plumes; Luther's black preaching gown or any clerical vestment; starched cartwheel ruffs or tall capotain hats (Britain earlymodern); princely or court regalia; printing tools, globes or other held props (the theme lives in the notes, not in the hands)
```

Files (9): `outfit_m_germany_earlymodern.png`, `hair_m_germany_earlymodern.png`, `facialhair_m_germany_earlymodern.png`, `headwear_m_germany_earlymodern.png`, `accessory_m_germany_earlymodern.png`, `outfit_f_germany_earlymodern.png`, `hair_f_germany_earlymodern.png`, `headwear_f_germany_earlymodern.png`, `accessory_f_germany_earlymodern.png`

Notes for Claude's processing: mask the woman's hair ornament before recolouring: gold-thread hairnet.

**Send this place's files to Claude** before you start the next place.

When the last place is in, Claude runs the batch check in Unity.

## 20. Batch 8: premade characters: Johannes Gutenberg, Leonardo da Vinci, Aemilia Lanyer, Cecilia Gallerani (16 images)

The rest of the day-3 premade pool. Make them in the **Time Sorter Premades** Project (section 11), one new chat per character. Use prompt 9.10 for the neutral image, then 9.11 three times. Folder: `Raw/batch08-premades/`. Send each character's four images to Claude as soon as they are done.

#### Johannes Gutenberg (`gutenberg`)

- Claims: Holy Roman Empire (Rhineland) (germany_medieval), 1440. Age at the desk: about 40 (face band c). Base figure to attach: `premadebase_m_skin2.png`.
- Wears the claimed place's Costume Guide item: Gugel hood (headwear).
- In the game: pooled day 3.

```
Johannes Gutenberg, goldsmith and inventor of printing with movable metal type, about 40. No true likeness survives (the forked beard of his traditional image was drawn a century after his death), so draw him as a clean-shaven Rhineland burgher of the 1440s with a shrewd, patient face: a Gugel hood of deep brown wool enclosing the head and neck with the face fully clear, its short shoulder cape cut into scalloped leaf-shaped points; a calf-length Tappert gown of dark blue wool with regular organ-pipe pleats, a high standing collar and wide fur-trimmed sleeves, belted low on the hips with a leather belt and a leather purse; dark hose; moderately pointed shoes.
```

Do not draw: a beard, a fur hat, a printing press, type or books in the hands.

Files: `premade_gutenberg_neutral.png`, `premade_gutenberg_happy.png`, `premade_gutenberg_angry.png`, `premade_gutenberg_worried.png`

#### Leonardo da Vinci (`leonardo`)

- Claims: Sforza Milan (italy_earlymodern), 1495. Age at the desk: about 43 (face band c). Base figure to attach: `premadebase_m_skin2.png`.
- Wears the claimed place's Costume Guide item: red berretta (headwear).
- In the game: pooled day 3.

```
Leonardo da Vinci, painter and engineer at the Sforza court of Milan, about 43, handsome and well groomed as his contemporaries described him, and clean-shaven like the Milanese men of the 1490s: shoulder-length, softly curled, well-kept brown hair. A small, soft, round red berretta cap worn straight on the head, with no brim and no feather. A knee-length, belted dusty-coral wool tunic (pitocco), short when long gowns were the fashion, over a white linen shirt gathered at the neck; close-fitting dark hose; soft round-toed shoes.
```

Do not draw: any beard (the long white beard belongs to his old age), paintings, notebooks or machines in the hands.

Files: `premade_leonardo_neutral.png`, `premade_leonardo_happy.png`, `premade_leonardo_angry.png`, `premade_leonardo_worried.png`

#### Aemilia Lanyer (`lanyer`)

- Claims: Elizabethan England (britain_earlymodern), 1600. Age at the desk: about 31 (face band b). Base figure to attach: `premadebase_f_skin2.png`.
- Wears the claimed place's Costume Guide item: hat over coif (headwear).
- In the game: pooled day 3.

```
Aemilia Lanyer (born Bassano), London poet, about 31, bright and self-possessed. A tall-crowned black felt hat with a narrow brim, worn over a close white linen coif whose lace edge frames the face (the London citizen's wife's hat), with dark hair drawn up under the coif. A gentlewoman's black or russet silk gown with a long, stiff, pointed bodice and a full skirt over a modest hip roll; a wide, starched, white lace-edged ruff and white lace cuffs; one long strand of pearls.
```

Do not draw: royal regalia, a court wheel farthingale, an Elizabeth I look (no crown, no jewelled wig).

Files: `premade_lanyer_neutral.png`, `premade_lanyer_happy.png`, `premade_lanyer_angry.png`, `premade_lanyer_worried.png`

#### Cecilia Gallerani (`gallerani`)

- Claims: Sforza Milan (italy_earlymodern), 1495. Age at the desk: about 22 (face band b). Base figure to attach: `premadebase_f_skin2.png`.
- Wears the claimed place's Costume Guide item: lenza (headwear).
- In the game: pooled day 3.

```
Cecilia Gallerani, poet and learned lady of the Sforza court of Milan, about 22, with a composed, intelligent face. Dark hair centre-parted and smoothed flat over the ears, the rest bound into one long braid down the back (hidden from the front). A lenza: a finger-wide dark velvet band worn straight across the forehead, with a large dark-red stone set in gold, as big as the whole eye, at the centre of the brow. A square-necked deep-crimson gamurra gown with contrasting deep-blue sleeves laced on at the shoulder and elbow, so puffs of the white linen camicia show through the gaps; a deep-blue sbernia mantle draped over the left shoulder; a long necklace of black jet beads looped twice round the neck.
```

Do not draw: an animal in her arms, a see-through veil, any hat, ducal jewels or regalia.

Files: `premade_gallerani_neutral.png`, `premade_gallerani_happy.png`, `premade_gallerani_angry.png`, `premade_gallerani_worried.png`

**Send to Claude.**

## 21. Batch 9: Industrial era (64 images)

Day 4 adds the Industrial era (1750 to 1900). Folder: `Raw/batch09-industrial-era/`. Start a new chat for each place, send its PAIR block with prompt 9.0, then make each file in its list. **Send each place's files to Claude as soon as that place is done**, and wait for the go-ahead on the first place of the batch.

### Egypt: Khedivate of Egypt

Costume Guide entry (the Culture value): **tarboush / tarha veil**

```
PAIR: egypt_industrial (Khedivate of Egypt, 1869)
Place and time: Cairo in 1869, the year Khedive Isma'il opened the Suez Canal, during the building of the new irrigation canal network

MAN
- Outfit: Stambouline (istanbuli): a black or dark-navy knee-length frock coat, single-breasted and buttoned right up to a small standing collar, with matching narrow trousers and polished black shoes. A white shirt collar just shows at the neck.
- Hair: Short and neatly trimmed
- Facial hair: Full, neatly groomed moustache with a clean-shaven chin
- Headwear: Tarboush: a stiff, upright, flat-topped cap of deep-crimson felt, shaped like a truncated cone and set straight and level on the head. A short black silk tassel falls from the centre of the crown toward the back. It is never floppy or tilted.
- Accessory: Gold watch-chain hanging in a clear loop across the front of the chest

WOMAN
- Outfit: Yelek: an ankle-length, fitted, long-sleeved robe of striped silk (cream with dusty-coral and gold stripes), buttoned from bust to hips and slit open at the sides below. It is worn over an opaque white chemise closed at the neck and full shintiyan trousers gathered at the ankle, and girdled at the hips with a folded Kashmir shawl.
- Hair: Centre-parted, with many long thin plaits falling behind the shoulders, spread wide so they show on both sides of the neck, each plait ending in small gold safa ornaments
- Headwear: Tarha: a long white muslin head veil with its ends embroidered in coloured silk and gold thread. It rests on the head and falls over both shoulders and down the sides of the body to below the knee, and the face is fully uncovered.
- Accessory: Kirdan: a gold choker-collar necklace hung with a row of teardrop and disc pendants

MUST READ AT A GLANCE: Men: the stiff, upright crimson tarboush with a black tassel, over a black stambouline buttoned to the collar. Women: the long white embroidered tarha veil falling from the head over both shoulders, over a striped yelek robe.
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (tarboush); woman's headwear (tarha veil)
DO NOT DRAW: Ottoman or Egyptian military uniforms, epaulettes, medals, the Khedive's orders and sashes; European top hats; a soft, drooping, tilted fez with a very long tassel (reads as Greece-industrial); a small tilted red cap with a gold tassel on a woman (reads as Greece-industrial); keffiyeh with 'igal, 'aba cloaks, or a black 'abaya over the head (read as Iraq-industrial); the black habara wrap with burqu' face veil (hides the face); pyramid or sphinx props
```

Files (9): `outfit_m_egypt_industrial.png`, `hair_m_egypt_industrial.png`, `facialhair_m_egypt_industrial.png`, `headwear_m_egypt_industrial.png`, `accessory_m_egypt_industrial.png`, `outfit_f_egypt_industrial.png`, `hair_f_egypt_industrial.png`, `headwear_f_egypt_industrial.png`, `accessory_f_egypt_industrial.png`

Notes for Claude's processing: the woman's hair hangs behind the shoulders (Claude splits off a hair-back layer); mask the woman's hair ornament before recolouring: gold safa ornaments.

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): Orientalist harem imagery.

**Send this place's files to Claude** before you start the next place.

### Iraq / Mesopotamia: Ottoman Iraq (Baghdad Vilayet)

Costume Guide entry (the Culture value): **chfiyya / abaya veil**

```
PAIR: iraq_industrial (Ottoman Iraq (Baghdad Vilayet), 1870)
Place and time: Baghdad under the reforming governor Midhat Pasha, c. 1870 (Iraq's first printing press and first newspaper, al-Zawra, 1869)

MAN
- Outfit: Ankle-length zaboun robe of white cotton with thin blue-black stripes, crossed over at the chest and girdled with a folded patterned shawl, with a slim brass scribe's pen case (dawat) tucked upright into the front of it. Over it is an 'aba: a wide, sleeveless, open-fronted cloak of fine wool in dark camel-brown, with a band of gold-thread embroidery round the neck and down the front edges.
- Hair: Short and close-cropped
- Facial hair: Heavy moustache with a short trimmed beard
- Headwear: Chfiyya (Iraqi keffiyeh): a square headcloth of plain white cotton, folded into a triangle and draped over the head to the shoulders, held in place by a doubled black camel-hair 'igal cord ring. The face is fully visible. (This headwear hides all the hair, so no hair is drawn under it: bring its front edge right down to the hairline so no bald scalp shows.)
- Accessory: none

WOMAN
- Outfit: Ankle-length, long-sleeved dress of brocaded silk (gold on crimson or bottle green) with a silver-plaque belt. Over everything is a black silk 'abaya, hanging from the shoulders to the ankles and falling open at the front to show the dress.
- Hair: Long hair in two braids falling down the back, hidden from the front
- Headwear: The top of the black silk 'abaya drawn over a black head-kerchief (futa), from the crown down both sides of the head and neck to the shoulders. The face is uncovered. (This headwear hides all the hair, so no hair is drawn under it: bring its front edge right down to the hairline so no bald scalp shows.)
- Accessory: Heavy gold filigree bangles stacked on both wrists

MUST READ AT A GLANCE: Men: the white chfiyya headcloth held by a black 'igal cord, worn with a gold-trimmed camel 'aba. Women: the black silk 'abaya drawn over the head and falling open over a brocade dress.
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (chfiyya); woman's headwear (abaya veil)
DO NOT DRAW: a stiff crimson fez or tarboush with a stambouline frock coat (reads as Egypt-industrial, even though Ottoman officials in Baghdad also wore it); a white veil trailing down the back (reads as Egypt women); black-and-white or other chequered keffiyeh patterns tied to modern political movements, and any slogans; Ottoman military uniforms; the black horsehair face veil (pushi) or anything else that hides the face; daggers, rifles or other weapons; Gulf-style all-white thobe and ghutra (a modern look)
```

Files (8): `outfit_m_iraq_industrial.png`, `hair_m_iraq_industrial.png`, `facialhair_m_iraq_industrial.png`, `headwear_m_iraq_industrial.png`, `outfit_f_iraq_industrial.png`, `hair_f_iraq_industrial.png`, `headwear_f_iraq_industrial.png`, `accessory_f_iraq_industrial.png`

Notes for Claude's processing: the man's headwear covers the hair (check on the bald head that no scalp shows under it); the woman's headwear covers the hair (check on the bald head that no scalp shows under it).

**Send this place's files to Claude** before you start the next place.

### Greece: Athens, Kingdom of Greece

Costume Guide entry (the Culture value): **fesi**

```
PAIR: greece_industrial (Athens, Kingdom of Greece, 1860)
Place and time: Athens, new capital of the independent Greek kingdom, c. 1860

MAN
- Outfit: Civilian fustanella: a white, many-pleated linen kilt ending just above the knee, a full-sleeved white shirt, and a dark-crimson sleeveless jacket (fermeli) trimmed with gold braid and worn open over an embroidered waistcoat. Below are knitted white stockings with tasselled garters and plain black leather shoes.
- Hair: Short on top, slightly longer at the nape
- Facial hair: Thick moustache with slightly upturned ends
- Headwear: Fesi: a soft Greek fez of red felt whose limp crown droops to one side, worn tilted, with a very long dark-blue silk tassel falling to the chest. It is clearly floppy, unlike Egypt's stiff, upright tarboush.
- Accessory: Wide red silk sash (zonari) wound around the waist

WOMAN
- Outfit: Amalia dress: a floor-length full skirt of pale ivory or dusty-coral silk, a white long-sleeved blouse with a lace chemisette front, and a short, fitted, open bolero jacket (kontogouni) of deep-burgundy velvet heavily embroidered with gold braid.
- Hair: Long hair plaited into two braids and wound around the crown of the head
- Headwear: Small red fesi pinned tilted on the crown, with a long tassel of braided gold thread falling to the shoulder. The face and hairline are fully visible.
- Accessory: Necklace of plain gold discs with raised rims on a chain (the dowry flouria, drawn with no face, letters or marks)

MUST READ AT A GLANCE: Men: the small red fesi with a long tassel, over a full-sleeved white shirt and a dark-crimson gold-braided jacket. Women: the gold-embroidered velvet kontogouni bolero with a small tilted red fesi and a gold tassel.
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (fesi); woman's headwear (fesi)
DO NOT DRAW: Evzone guard look: pom-pom tsarouchia shoes, guard ranks, ceremonial drill pose (military); silahlik weapon belt, pistols, yataghan, rifles, cartridge belts; stiff, tall, upright Egyptian or Ottoman tarboosh (reads as Egypt); a long white veil trailing down the back (reads as Egypt women); Queen Amalia's crown or court jewels (royalty); a bright-blue-and-white colour scheme (the Greek flag); Orthodox clergy dress (sacred); Italian tabarro cloak or broad-brimmed felt hat (reads as Italy)
```

Files (9): `outfit_m_greece_industrial.png`, `hair_m_greece_industrial.png`, `facialhair_m_greece_industrial.png`, `headwear_m_greece_industrial.png`, `accessory_m_greece_industrial.png`, `outfit_f_greece_industrial.png`, `hair_f_greece_industrial.png`, `headwear_f_greece_industrial.png`, `accessory_f_greece_industrial.png`

**Send this place's files to Claude** before you start the next place.

### Italy: Bologna, Kingdom of Italy

Costume Guide entry (the Culture value): **silk scarf / coral beads**

```
PAIR: italy_industrial (Bologna, Kingdom of Italy, 1895)
Place and time: Bologna and Pontecchio, where young Guglielmo Marconi ran his first radio experiments, 1895

MAN
- Outfit: Dark wool three-piece suit with a white shirt, stiff standing collar and dark cravat. Over it is a full, calf-length circular tabarro cloak in black or midnight-blue wool, with a velvet collar and a metal chain clasp at the throat. One side is flung back over the opposite shoulder, showing the suit.
- Hair: Short, side-parted and pomaded
- Facial hair: Full, thick moustache brushed outward in the 'Umbertine' fashion, not waxed to points
- Headwear: Wide-brimmed soft black felt hat with a low rounded crown and a broad, slightly drooping brim, set level and high enough that the eyes stay clearly visible (the hat worn with the tabarro in the Po Valley)
- Accessory: Long white silk scarf hanging loose around the neck, both ends falling straight down the front of the chest, in the style of Verdi

WOMAN
- Outfit: Mid-1890s dress in dark wool: a fitted bodice with a high collar, puffed leg-of-mutton sleeves and a smooth, bell-shaped, floor-length skirt. A large black silk scialle (shawl) with a long knotted fringe is draped over the shoulders and crossed at the chest.
- Hair: Swept up into a high chignon with a soft curled fringe
- Headwear: none
- Accessory: Multi-strand red coral bead necklace worn over the high collar

MUST READ AT A GLANCE: Men: the tabarro, a full dark circular cloak with a velvet collar and one side flung over the opposite shoulder, worn with a broad-brimmed black felt hat. Women: the black fringed silk scialle crossed over the chest, with a red coral necklace.
LEAK ITEMS (draw these extra clear and true to the text): man's accessory (silk scarf); woman's accessory (coral beads)
DO NOT DRAW: Garibaldi red shirt (volunteer military uniform); Bersaglieri feathered hat, carabinieri, any uniform; King Umberto I style regalia (royalty); a sinister, cloaked-villain look; Greek fustanella or fez (reads as Greece); Homburg or dented-crown hat with a narrow curled brim, or pince-nez (reads as Germany-industrial); top hat and frock coat without the cloak, or a poke bonnet with a Paisley shawl (reads as Britain-industrial)
```

Files (8): `outfit_m_italy_industrial.png`, `hair_m_italy_industrial.png`, `facialhair_m_italy_industrial.png`, `headwear_m_italy_industrial.png`, `accessory_m_italy_industrial.png`, `outfit_f_italy_industrial.png`, `hair_f_italy_industrial.png`, `accessory_f_italy_industrial.png`

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): mafia or gangster caricature; organ-grinder or peasant caricature.

**Send this place's files to Claude** before you start the next place.

### China: Qing Shanghai

Costume Guide entry (the Culture value): **guapimao cap / meile band**

```
PAIR: china_industrial (Qing Shanghai, 1880)
Place and time: Shanghai in the late Qing, c. 1880: the Shenbao newspaper, the Jiangnan Arsenal translation bureau and the first telegraph lines

MAN
- Outfit: Changshan: an ankle-length gown in dark-blue or grey silk with a small standing collar and a curved diagonal (dajin) opening fastened by cloth knot buttons down the right side. Over it is a magua: a short, hip-length black jacket with a straight centre opening, knot buttons and roomy sleeves. A small embroidered silk purse (hebao) on a knotted cord with a tassel hangs at the right hip below the magua. Black cloth shoes with white soles.
- Hair: Queue: the front of the scalp shaved back to the crown and the rest braided into one long, neat plait down the back (hidden from the front). Draw it tidily and with dignity, never as a caricature.
- Facial hair: none (clean-shaven; by Qing custom moustaches were usually grown only in middle age)
- Headwear: Guapimao: a rounded black satin skullcap of six segments with a red knotted-silk button on top and no official finial
- Accessory: none

WOMAN
- Outfit: Ao: a loose, knee-length jacket with wide flared sleeves and a curved right-side opening with knot buttons. Its collar edge, front curve, cuffs and hem are covered in several broad bands of contrasting embroidered trim (xianggun). The jacket is pale coral or pale blue with black trim, worn over a black pleated mamian skirt. Flat black cloth shoes.
- Hair: Sleek and centre-parted, combed smoothly back into a flat low bun at the nape and pinned with a silver hairpin
- Headwear: Meile (brow band): a narrow band of black satin worn across the forehead just above the brows and tied at the back. It is embroidered with small flowers and set with a small pearl or jade ornament at centre front, and leaves the face fully visible.
- Accessory: Thick pale white-jade (mutton-fat jade) bangle

MUST READ AT A GLANCE: Men: the black six-segment guapimao skullcap with a red knot button, worn with a short black magua jacket over a long changshan gown. Women: the broad-trimmed ao jacket with a black embroidered meile band across the forehead.
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (guapimao cap); woman's headwear (meile band)
DO NOT DRAW: long drooping moustache; Qing official hat with rank finial or peacock feather, mandarin squares, court beads (chaozhu); Boxer, Taiping or military imagery; Manchu court headdress (liangbatou) or platform shoes on a Han civilian woman; conical straw hat (douli); 1920s qipao (anachronistic); bowler hat, haori or hakama (reads as Japan-industrial)
```

Files (7): `outfit_m_china_industrial.png`, `hair_m_china_industrial.png`, `headwear_m_china_industrial.png`, `outfit_f_china_industrial.png`, `hair_f_china_industrial.png`, `headwear_f_china_industrial.png`, `accessory_f_china_industrial.png`

Notes for Claude's processing: mask the woman's hair ornament before recolouring: silver hairpin.

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): 'pigtail' cartoons, Fu Manchu, slanted-eye features; opium pipes or opium imagery; any emphasis on bound feet (the outfit names flat cloth shoes); 'coolie' stereotype.

**Send this place's files to Claude** before you start the next place.

### Japan: Meiji Nagoya

Costume Guide entry (the Culture value): **bowler hat / sokuhatsu**

```
PAIR: japan_industrial (Meiji Nagoya, 1899)
Place and time: Nagoya in the Meiji period, c. 1899, the time of Sakichi Toyoda's steam-powered wooden loom (1896) and Japan's rapid industrialisation

MAN
- Outfit: Dark kimono under a knee-length black haori with five small plain white circles (blank crest roundels with no design inside), closed at the chest by a thick white braided silk cord (haori-himo) tied in a flat knot with two tassels, over grey-and-black striped pleated hakama. White tabi socks and zori sandals. The kimono collar crosses left over right (the wearer's left panel on top, so from the front the neckline forms a lowercase 'y').
- Hair: Zangiri: a short, Western-style, side-parted crop (topknots were abandoned after 1871)
- Facial hair: Neat, trimmed moustache, straight and not waxed to points
- Headwear: Black Western bowler hat (yamataka-bo) with a rigid round dome and a narrow brim
- Accessory: none

WOMAN
- Outfit: Jogakusei (girl student) look: a small-patterned kimono, e.g. with indigo-and-white yagasuri arrow-feather stripes, under a maroon (ebicha) pleated hakama tied high above the waist. Tabi with zori or low boots. The kimono collar crosses left over right (the wearer's left panel on top, so from the front the neckline forms a lowercase 'y').
- Hair: Sokuhatsu: hair swept up and back into a puffed pompadour roll with a bun at the back of the head (Western-inspired, promoted from 1885), with a large white silk ribbon bow tied high at the back, its loops standing out on both sides of the head in front view
- Headwear: none
- Accessory: none

MUST READ AT A GLANCE: Men: a black bowler hat worn with a black haori closed by a white himo cord, a Western hat over Japanese dress. Women: the sokuhatsu hairstyle with a large white ribbon bow, over a yagasuri kimono.
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (bowler hat); woman's hair (sokuhatsu)
DO NOT DRAW: Imperial Army or Navy uniforms, the Rising Sun flag, the sixteen-petal imperial chrysanthemum; Swords (banned in 1876); Samurai topknot (anachronistic by now); Geisha makeup or kimono; Chinese queue, skullcap or changshan; Collar closed right-over-left (the dressing used for the dead); a bowler worn with a Western frock coat or suit (loses the Japanese tell; reads as Germany or Britain)
```

Files (6): `outfit_m_japan_industrial.png`, `hair_m_japan_industrial.png`, `facialhair_m_japan_industrial.png`, `headwear_m_japan_industrial.png`, `outfit_f_japan_industrial.png`, `hair_f_japan_industrial.png`

Notes for Claude's processing: mask the woman's hair ornament before recolouring: white ribbon bow.

**Send this place's files to Claude** before you start the next place.

### Britain: Victorian Britain

Costume Guide entry (the Culture value): **top hat / poke bonnet**

```
PAIR: britain_industrial (Victorian Britain, 1843)
Place and time: London in 1843, when Ada Lovelace published her notes on Babbage's Analytical Engine, as the Cooke-Wheatstone electric telegraph spread and just before Railway Mania (1845-47)

MAN
- Outfit: Knee-length black or bottle-green wool frock coat with a nipped waist and full skirt, over a patterned silk waistcoat and narrow fawn or checked trousers. A white shirt with collar points kept below the chin, wrapped with a wide black silk neck-stock or cravat.
- Hair: Side-parted hair, full and curled over the ears
- Facial hair: Mutton-chop side-whiskers running down the jaw, with the upper lip and chin clean-shaven
- Headwear: Tall black silk top hat (stovepipe) with a slightly curled brim
- Accessory: Gold pocket-watch chain hanging in a clear loop across the front of the chest

WOMAN
- Outfit: Day dress with a tight bodice that comes to a point at the waist, low sloping shoulders, narrow long sleeves and a full bell-shaped skirt over layered petticoats. It is made in dark plaid silk or printed cotton, with a small white lace collar.
- Hair: Early-Victorian centre parting: hair smoothed flat over the crown, with clusters of ringlets over each ear and a bun at the back
- Headwear: Deep straw poke ('coal-scuttle') bonnet that frames the face, tied under the chin with a wide ribbon. The face stays fully visible.
- Accessory: Large Paisley shawl, woven in Paisley, Scotland, with the teardrop 'boteh' pattern, draped around the shoulders

MUST READ AT A GLANCE: Men: a tall black silk top hat with mutton-chop whiskers and a clean upper lip. Women: a coal-scuttle bonnet with a Paisley shawl.
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (top hat); woman's headwear (poke bonnet)
DO NOT DRAW: redcoat, police, guards or any uniform; Union Jack or royal regalia; steampunk goggles, gears or other anachronistic gadgets; Homburg hat or upturned waxed moustache (Germany-industrial); loose uncorseted Reformkleid or Jugendstil ornament (Germany-industrial); plain black fringed shawl with a bare chignon and coral beads (Italy-industrial); bustle silhouette (1870s-80s, the wrong decade); a monocle
```

Files (9): `outfit_m_britain_industrial.png`, `hair_m_britain_industrial.png`, `facialhair_m_britain_industrial.png`, `headwear_m_britain_industrial.png`, `accessory_m_britain_industrial.png`, `outfit_f_britain_industrial.png`, `hair_f_britain_industrial.png`, `headwear_f_britain_industrial.png`, `accessory_f_britain_industrial.png`

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): Dickensian ragged-urchin or chimney-sweep caricature; 'toff' caricature.

**Send this place's files to Claude** before you start the next place.

### Germany: Wilhelmine Germany

Costume Guide entry (the Culture value): **Homburg / Jugendstil pendant**

```
PAIR: germany_industrial (Wilhelmine Germany, 1899)
Place and time: Berlin c. 1899, during the boom in German electrical engineering (Siemens, AEG) and chemistry (Bayer's aspirin, synthetic dyes), when Berlin's permanent dress-reform exhibition opened

MAN
- Outfit: Dark charcoal wool frock coat (Gehrock) buttoned high over a matching waistcoat, with narrow dark striped trousers. A very tall, stiff white stand-up collar (Stehkragen) with a narrow dark tie.
- Hair: Short brush cut (Buerstenschnitt): hair cropped close and standing upright
- Facial hair: Moustache with the ends twisted up in the Wilhelmine 'Es ist erreicht' fashion, kept moderate rather than pointed up to the cheekbones
- Headwear: Dark felt Homburg with a single dent down the centre of the crown and a stiff, curled-up, ribbon-bound brim (named after Bad Homburg in Hesse)
- Accessory: Pince-nez (Kneifer) clipped to the nose, on a thin black cord

WOMAN
- Outfit: Reformkleid: a loose, uncorseted, floor-length dress hanging straight from a fitted shoulder yoke, with soft full sleeves, in moss-green, cream or dove-grey wool. The yoke and hem are embroidered with flowing Jugendstil plant curves.
- Hair: Soft, loose chignon: hair pinned up low and full at the back, with natural waves at the temples
- Headwear: none
- Accessory: Jugendstil pendant: a large silver plaque with dark-blue enamel and a curving 'whiplash' leaf motif in the Pforzheim style, about as wide as the face, hanging at the base of the throat on its own short silver chain

MUST READ AT A GLANCE: Men: a Homburg hat with pince-nez and an upturned moustache. Women: a large Jugendstil pendant at the throat, over the embroidered yoke of a loose Reformkleid.
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (Homburg); woman's accessory (Jugendstil pendant)
DO NOT DRAW: Pickelhaube or any military or police uniform; Iron Cross, eagle emblems, black-white-red imperial colours; student-fraternity caps, sashes or duelling scars; a monocle; a moustache waxed into long needle points reaching the cheekbones; lederhosen, dirndl or Tyrolean hats; top hat with mutton-chop whiskers, or a poke bonnet with Paisley shawl (Britain-industrial); broad-brimmed soft felt hat with a full circular cloak (Italy-industrial); a round-domed bowler (the Japan-industrial hat); tight wasp-waist corset with a bustle
```

Files (8): `outfit_m_germany_industrial.png`, `hair_m_germany_industrial.png`, `facialhair_m_germany_industrial.png`, `headwear_m_germany_industrial.png`, `accessory_m_germany_industrial.png`, `outfit_f_germany_industrial.png`, `hair_f_germany_industrial.png`, `accessory_f_germany_industrial.png`

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): Prussian-officer caricature; Kaiser caricature.

**Send this place's files to Claude** before you start the next place.

When the last place is in, Claude runs the batch check in Unity.

## 22. Batch 10: Modern era (61 images)

Day 5 adds the Modern era (1900 to 2000). Folder: `Raw/batch10-modern-era/`. Start a new chat for each place, send its PAIR block with prompt 9.0, then make each file in its list. **Send each place's files to Claude as soon as that place is done**, and wait for the go-ahead on the first place of the batch.

### Egypt: Nasser's Egypt

Costume Guide entry (the Culture value): **fringed shal / mandil**

```
PAIR: egypt_modern (Nasser's Egypt, 1962)
Place and time: Egypt under Gamal Abdel Nasser, Cairo and Aswan, c. 1962 (Aswan High Dam under construction 1960-70; Suez Canal nationalised 1956)

MAN
- Outfit: Ankle-length galabiya of fine pale-grey or finely striped cotton (loose and collarless, with a deep slit neckline and wide sleeves), worn with a tailored dark Western suit jacket open over it; leather shoes.
- Hair: Short hair combed straight back from a high forehead
- Facial hair: Thick, neat moustache, otherwise clean-shaven
- Headwear: none (bareheaded; the tarboush was abandoned after the 1952 revolution)
- Accessory: Shal: a long, plain dark wool scarf (brown, charcoal or camel) with a knotted fringe, draped round the neck so both ends hang straight down the chest outside the jacket. No stripes.

WOMAN
- Outfit: Village or provincial galabiya: an ankle-length, loose, long-sleeved dress in brightly printed cotton (small flowers on a saturated red, bottle-green or blue ground), with a round neck and a gathered yoke, often finished with a gathered flounce at the hem. Flat slippers.
- Hair: Long hair, centre-parted and smoothed flat to the head, in one thick plait hanging down the back, hidden from the front
- Headwear: Mandil abu oya: a coloured triangular cotton headscarf tied at the back of the head (knot at the nape, point covering the back of the head), its front edge trimmed with a fringe of tiny crocheted flowers or beads that frames the forehead. Face, chin and neck fully visible.
- Accessory: Ghawayesh: a stack of thick gold bangles on each wrist

MUST READ AT A GLANCE: Men: a long, dark, fringed shal round the neck, its ends down the chest, over a tailored Western jacket worn on a galabiya. Women: a flower-edged mandil tied at the nape, with a bright printed galabiya.
LEAK ITEMS (draw these extra clear and true to the text): man's accessory (fringed shal); woman's headwear (mandil)
DO NOT DRAW: tarboush or fez (reads as Egypt-industrial); military uniforms or Free Officers' khaki; revolutionary or United Arab Republic emblems, the eagle, flags; keffiyeh with agal, or the sidara cap (read as Iraq); black melaya laff wrap over the head (reads as Iraq's 'abaya); face veils or the burqu'; belly-dance costume or coin hip-scarves; bouffant hair, sheath dress and big sunglasses (read as 1960 Rome); striped scarf (reads as Britain's college scarf); pharaonic costume
```

Files (8): `outfit_m_egypt_modern.png`, `hair_m_egypt_modern.png`, `facialhair_m_egypt_modern.png`, `accessory_m_egypt_modern.png`, `outfit_f_egypt_modern.png`, `hair_f_egypt_modern.png`, `headwear_f_egypt_modern.png`, `accessory_f_egypt_modern.png`

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): a likeness of Nasser or Umm Kulthum; 'harem' caricature.

**Send this place's files to Claude** before you start the next place.

### Iraq / Mesopotamia: Kingdom of Iraq

Costume Guide entry (the Culture value): **sidara / gold coin pendant**

```
PAIR: iraq_modern (Kingdom of Iraq, 1956)
Place and time: Baghdad c. 1956, the Development Board's modernist building boom (commissions for Gropius, Le Corbusier and Wright) in the city where Zaha Hadid grew up

MAN
- Outfit: Light-grey or tan wool two-piece suit with wide lapels, white shirt, dark tie and polished shoes.
- Hair: Short, neatly combed and side-parted, lying close to the head
- Facial hair: Neatly trimmed moustache
- Headwear: Sidara (al-Faisaliyya): a soft black felt or velvet civilian cap that folds flat; it is boat-shaped, with a raised point at front and back and a lengthwise crease along the crown, worn straight on the head with no badge
- Accessory: Thick black horn-rimmed glasses with clear lenses

WOMAN
- Outfit: Tailored 1950s dress (fitted bodice, belted waist, full mid-calf skirt) in ruby red, sapphire blue or amber, with a black silk 'abaya hanging from the shoulders and falling open at the front to the ankles so the dress shows; low-heeled pumps.
- Hair: Short, softly permed curls, side-parted, with the front curls framing the forehead
- Headwear: The top of the black 'abaya drawn over the head, its edge resting on the crown and the cloth falling to the shoulders, leaving the face and front hair visible
- Accessory: Gold coin pendant: a single large plain gold disc with a raised rim (a gold lira, drawn with no face, letters or marks), about as wide as three fingers, on its own short gold chain at the base of the throat

MUST READ AT A GLANCE: The sidara, Iraq's folding boat-shaped national cap, on men. The black 'abaya over the head, worn open over a bright 1950s dress, on women.
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (sidara); woman's accessory (gold coin pendant)
DO NOT DRAW: military berets, garrison-cap insignia or any badge on the sidara; flags or eagle emblems; fez or tarboush (Ottoman and Egypt); keffiyeh with agal (reserved for Iraq-industrial); Gulf-style white thobe and ghutra; face veils; pearl strand (Britain modern's 'twinset and pearls')
```

Files (9): `outfit_m_iraq_modern.png`, `hair_m_iraq_modern.png`, `facialhair_m_iraq_modern.png`, `headwear_m_iraq_modern.png`, `accessory_m_iraq_modern.png`, `outfit_f_iraq_modern.png`, `hair_f_iraq_modern.png`, `headwear_f_iraq_modern.png`, `accessory_f_iraq_modern.png`

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): Ba'ath-era imagery; a likeness of King Faisal, Zaha Hadid or any real person.

**Send this place's files to Claude** before you start the next place.

### Greece: Metapolitefsi Athens

Costume Guide entry (the Culture value): **tagari bag**

```
PAIR: greece_modern (Metapolitefsi Athens, 1975)
Place and time: Athens after the fall of the junta and the restoration of democracy, c. 1975

MAN
- Outfit: Brown corduroy jacket over a thick hand-knit wool sweater or an open-collared patterned shirt, with flared jeans and suede desert boots.
- Hair: Collar-length, slightly shaggy 1970s cut
- Facial hair: Thick full moustache
- Headwear: none
- Accessory: Tagari: a hand-woven wool shoulder bag in bold horizontal stripes with a fringed bottom, about as tall as the head, fringe included. It is worn crossbody on a short, broad strap woven in the same stripes: the strap runs from the right shoulder across the chest, and the bag rides high against the left side of the chest, its top level with the armpit

WOMAN
- Outfit: White cotton folk-revival blouse with red and black cross-stitch embroidery at the neckline and cuffs, under a loose chunky knit cardigan. Below are a long midi skirt or flared jeans and flat handmade leather strap sandals.
- Hair: Long, straight, centre-parted and loose
- Headwear: none
- Accessory: Tagari: a hand-woven wool shoulder bag in bold horizontal stripes with a fringed bottom, about as tall as the head, fringe included. It is worn crossbody on a short, broad strap woven in the same stripes: the strap runs from the right shoulder across the chest, and the bag rides high against the left side of the chest, its top level with the armpit

MUST READ AT A GLANCE: The tagari: a hand-woven wool bag in bold stripes with a fringed bottom, worn high on the left side of the chest on a broad striped strap that crosses the chest (men with a thick moustache and a shaggy 1970s cut; women with a cross-stitched folk blouse and long loose hair)
LEAK ITEMS (draw these extra clear and true to the text): man's accessory (tagari bag); woman's accessory (tagari bag)
DO NOT DRAW: any uniform, insignia or phoenix emblem; any military or police uniform; Greek flag as clothing; ancient chiton or Evzone costume on a modern traveller; Orthodox clergy dress (sacred); komboloi worry beads (held prop, not worn); fisherman's or other soft peaked cap (collides with Beijing 1972's cap); plain khaki canvas satchel (reads as Beijing 1972); slim dark suit and dark sunglasses (reads 1960 Italy)
```

Files (7): `outfit_m_greece_modern.png`, `hair_m_greece_modern.png`, `facialhair_m_greece_modern.png`, `accessory_m_greece_modern.png`, `outfit_f_greece_modern.png`, `hair_f_greece_modern.png`, `accessory_f_greece_modern.png`

Notes for Claude's processing: the woman's hair hangs behind the shoulders (Claude splits off a hair-back layer).

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): junta-era (1967-74) symbols; 'Zorba' or taverna-dancer caricature.

**Send this place's files to Claude** before you start the next place.

### Italy: Dolce Vita Rome

Costume Guide entry (the Culture value): **dark shades / cat-eye shades**

```
PAIR: italy_modern (Dolce Vita Rome, 1960)
Place and time: Rome during the economic miracle, the year of 'La Dolce Vita', 1960

MAN
- Outfit: Slim, sharply tailored single-breasted suit in charcoal or midnight mohair-wool, with a short jacket, narrow lapels and narrow cropped trousers. Worn with a crisp white shirt and thin dark tie (or a fine-knit black turtleneck, the 'dolcevita') and polished slip-on loafers.
- Hair: Short, neatly slicked back with pomade
- Facial hair: none
- Headwear: none
- Accessory: Dark sunglasses with solid black lenses and heavy black frames

WOMAN
- Outfit: Fitted knee-length sleeveless sheath dress with a narrow waist in a bold, swirling kaleidoscopic silk print (burnt orange, ochre, teal-blue and black) in the 1960s Florentine style, with pointed low-heeled pumps.
- Hair: Short, voluminous backcombed bouffant, left uncovered
- Headwear: none
- Accessory: Cat-eye sunglasses with dark lenses

MUST READ AT A GLANCE: Dark sunglasses: men wear them with a razor-slim tailored suit; women wear cat-eye sunglasses with a swirling kaleidoscopic silk-print sheath and an uncovered bouffant
LEAK ITEMS (draw these extra clear and true to the text): man's accessory (dark shades); woman's accessory (cat-eye shades)
DO NOT DRAW: a black shirt worn as a uniform, or any insignia; military or carabinieri uniforms; pinstripes, a fedora or a violin case; a striped gondolier shirt or a straw boater; Vespa or other held or ridden props; priest's collar or clerical dress (sacred); headscarf knotted under the chin (now Britain modern's tell); woven crossbody bag or fisherman's cap (reads 1970s Greece); orange-padded headphones or lapel badge (reads Japan 1980)
```

Files (6): `outfit_m_italy_modern.png`, `hair_m_italy_modern.png`, `accessory_m_italy_modern.png`, `outfit_f_italy_modern.png`, `hair_f_italy_modern.png`, `accessory_f_italy_modern.png`

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): Fascist-era imagery (black shirts, fasces); mafia or gangster caricature; pizza-chef or gondolier stereotypes.

**Send this place's files to Claude** before you start the next place.

### China: Beijing, People's Republic

Costume Guide entry (the Culture value): **navy cap**

```
PAIR: china_modern (Beijing, People's Republic, 1972)
Place and time: Beijing, c. 1972, when Tu Youyou's Project 523 team isolated artemisinin at the Academy of Traditional Chinese Medicine

MAN
- Outfit: Zhongshan suit: loose, boxy grey or navy-blue cotton-wool jacket with a closed turn-down collar, five front buttons and four patch pockets with buttoned flaps, worn with matching straight, roomy trousers and black cloth slip-on shoes.
- Hair: Short, neat side-parted crop
- Facial hair: none
- Headwear: Soft navy cotton cap with a tall, rounded, slightly stiffened crown, a cloth band and a short stiff peak. It sits high on the head, unlike a low flat cap. Plain, with no badge or star.
- Accessory: Plain faded-khaki canvas satchel on a cross-body strap, with no star, slogan, print or stripes

WOMAN
- Outfit: Women's version of the same Zhongshan jacket in pale blue or grey cotton, with turn-down collar and four patch pockets and a white shirt collar folded out over it. Straight dark trousers and black cloth strap shoes; practical and unornamented.
- Hair: Short, straight, chin-length bob with a side part, held back on one side by a plain black hair clip
- Headwear: The same soft navy cotton cap as the men's: a tall, rounded, slightly stiffened crown, a cloth band and a short stiff peak, sitting high on the head over the bob, unlike a low flat cap. Plain, with no badge or star.
- Accessory: Plain faded-khaki canvas satchel on a cross-body strap, with no star, slogan, print or stripes

MUST READ AT A GLANCE: The soft navy cap with a tall, rounded crown and a short peak, over a blue-grey Zhongshan (Mao) jacket with a turn-down collar and buttoned patch pockets, worn by men and women alike
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (navy cap); woman's headwear (navy cap)
DO NOT DRAW: any badge or pin on the jacket; a star or badge on the cap, a green army-style uniform, armbands; a book in the hands, slogans or any text; 1930s qipao or Shanghai glamour (wrong decade); White lab coat as the main garment (not culturally distinctive); conical straw hat (douli); Japanese salaryman suit and tie; low flat tweed cap (Britain modern); striped or fringed woven bag (Greece modern)
```

Files (8): `outfit_m_china_modern.png`, `hair_m_china_modern.png`, `headwear_m_china_modern.png`, `accessory_m_china_modern.png`, `outfit_f_china_modern.png`, `hair_f_china_modern.png`, `headwear_f_china_modern.png`, `accessory_f_china_modern.png`

Notes for Claude's processing: mask the woman's hair ornament before recolouring: black hair clip.

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): Mao badges or portrait pins; PLA or Red Guard imagery; Little Red Book or propaganda; 'coolie' stereotype or any caricature.

**Send this place's files to Claude** before you start the next place.

### Japan: Showa Tokyo

Costume Guide entry (the Culture value): **orange headphones**

```
PAIR: japan_modern (Showa Tokyo, 1980)
Place and time: Tokyo during the Walkman boom, c. 1980, as Akio Morita's Sony took Japanese electronics global

MAN
- Outfit: Salaryman suit: dark navy or charcoal single-breasted two-piece with a white shirt and a narrow dark tie; black leather shoes.
- Hair: Short hair neatly combed back with a side part
- Facial hair: none
- Headwear: none
- Accessory: Lightweight silver-and-black headband headphones with round bright-orange foam ear pads, resting around the neck with the ear pads on the collarbones either side of the throat; no cord, no player, no brand logo

WOMAN
- Outfit: Office outfit: a fitted navy or burgundy waistcoat over a white blouse with a soft ribbon bow at the collar, a matching knee-length pleated skirt, bare legs (no stockings drawn: leave the legs magenta) and low pumps.
- Hair: Seiko-chan cut: layered, shoulder-length feathered bob with soft inward flicks at the sides and wispy bangs (the c. 1980 pop-idol trend)
- Headwear: none
- Accessory: The same silver-and-black headband headphones with round bright-orange foam ear pads, resting around the neck; no cord, no player, no brand logo

MUST READ AT A GLANCE: Orange-foam headphones resting around the neck (men with a dark salaryman suit, women with a fitted office waistcoat over a blouse with a bow at the collar)
LEAK ITEMS (draw these extra clear and true to the text): man's accessory (orange headphones); woman's accessory (orange headphones)
DO NOT DRAW: Rising Sun flag or rays, or a hachimaki headband with the hinomaru; WWII military uniforms or imagery; Sony or any real brand logo; cosplay, idol-costume or anime-costume cliches; a school-uniform look; samurai or geisha looks; Kimono (not everyday wear at this moment); Chinese Zhongshan suit; dark sunglasses (reads as 1960 Rome, whose slim suit is close to the salaryman's)
```

Files (6): `outfit_m_japan_modern.png`, `hair_m_japan_modern.png`, `accessory_m_japan_modern.png`, `outfit_f_japan_modern.png`, `hair_f_japan_modern.png`, `accessory_f_japan_modern.png`

**Send this place's files to Claude** before you start the next place.

### Britain: Post-war Britain

Costume Guide entry (the Culture value): **flat cap / headscarf**

```
PAIR: britain_modern (Post-war Britain, 1950)
Place and time: Manchester, c. 1950, where Alan Turing was working on the university's pioneering stored-program computer (the Manchester Mark 1, built commercially by Ferranti)

MAN
- Outfit: Brown herringbone tweed sports jacket with leather elbow patches over a Fair Isle-patterned sleeveless knitted pullover, a white shirt and a knitted wool tie. Baggy grey flannel trousers and brown brogues.
- Hair: Short back and sides: neatly cut, side-parted and slicked with hair cream
- Facial hair: none
- Headwear: Tweed flat cap: low, flat crown sloping forward onto a short peak
- Accessory: Long college scarf in broad block stripes of two contrasting colours, wound once round the neck with the ends hanging loose

WOMAN
- Outfit: Soft wool twinset (short-sleeved jumper with a matching cardigan) in a pastel or heather tone, tucked into a below-the-knee A-line tweed skirt. Seamed stockings and low-heeled court shoes.
- Hair: Early-1950s 'set': collar-length hair in soft rolled pin-curls framing the face
- Headwear: Printed silk square headscarf folded into a triangle and knotted under the chin, covering the ears, its front edge set back from the forehead; face fully visible
- Accessory: A single strand of pearls (the 'twinset and pearls')

MUST READ AT A GLANCE: Men: a flat cap, with a herringbone tweed jacket over a Fair Isle pullover. Women: an under-the-chin headscarf, with a twinset and pearls.
LEAK ITEMS (draw these extra clear and true to the text): man's headwear (flat cap); woman's headwear (headscarf)
DO NOT DRAW: military, Home Guard, ARP or any uniform; Union Jack, royal regalia, guards' bearskins, police helmets; 1960s Mod or 1970s punk looks (the wrong moment); cloche hat, Bubikopf bob or Bauhaus colour-block prints (Germany modern); broad-brimmed soft felt hat with round wire spectacles and a walrus moustache (Germany modern); tall-crowned navy peaked cap (China modern); kerchief knotted at the back of the head with a flower-trimmed edge (Egypt modern); Sherlock-style deerstalker cliche
```

Files (8): `outfit_m_britain_modern.png`, `hair_m_britain_modern.png`, `headwear_m_britain_modern.png`, `accessory_m_britain_modern.png`, `outfit_f_britain_modern.png`, `hair_f_britain_modern.png`, `headwear_f_britain_modern.png`, `accessory_f_britain_modern.png`

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): a likeness of Alan Turing.

**Send this place's files to Claude** before you start the next place.

### Germany: Weimar Berlin

Costume Guide entry (the Culture value): **walrus moustache / cloche**

```
PAIR: germany_modern (Weimar Berlin, 1926)
Place and time: Berlin c. 1926, the Weimar Republic's golden age of science and design: Einstein directs the Kaiser Wilhelm Institute for Physics, and the Bauhaus has just moved to Dessau

MAN
- Outfit: Loose, slightly rumpled charcoal three-piece wool suit with wide trousers, a white shirt with a soft turn-down collar and a dark knitted tie. A long dark wool overcoat is worn open. Black leather lace-up shoes.
- Hair: Collar-length hair brushed back from the forehead, a little untidy (not a wild halo)
- Facial hair: Very bushy walrus moustache that covers the whole upper lip and droops past the corners of the mouth, wider than the mouth
- Headwear: Broad-brimmed soft black felt hat with a pinched crown
- Accessory: Round wire-rimmed spectacles

WOMAN
- Outfit: Straight, loose, knee-length drop-waist dress with a Bauhaus-style geometric pattern of colour blocks (red, ochre, blue and black squares, bars and circles), worn with flat bar-strap shoes.
- Hair: Bubikopf: a sleek chin-length bob with a blunt fringe
- Headwear: Close-fitting felt cloche hat pulled down to just above the eyebrows, with a small brim so the eyes and face stay clear
- Accessory: Geometric chrome-and-brass necklace of circles and bars in the Bauhaus metal-workshop style

MUST READ AT A GLANCE: For women, the close felt cloche over a Bubikopf bob, with Bauhaus colour-block geometry on the dress. For men, a broad-brimmed soft felt hat, a walrus moustache and round wire spectacles.
LEAK ITEMS (draw these extra clear and true to the text): man's facial hair (walrus moustache); woman's headwear (cloche)
DO NOT DRAW: any uniform, armband or insignia; brown or black shirts, jackboots, leather trench coats; Iron Cross, eagles, black-white-red imperial colours; toothbrush moustache; monocle or Pickelhaube; lederhosen or dirndl; fishnets or lingerie; wild white hair or a famous-portrait pose; tweed flat cap, Fair Isle knitwear or twinset and pearls (Britain modern)
```

Files (9): `outfit_m_germany_modern.png`, `hair_m_germany_modern.png`, `facialhair_m_germany_modern.png`, `headwear_m_germany_modern.png`, `accessory_m_germany_modern.png`, `outfit_f_germany_modern.png`, `hair_f_germany_modern.png`, `headwear_f_germany_modern.png`, `accessory_f_germany_modern.png`

Review only (Saleh and Claude check the images for these; never paste them into ChatGPT, whose Block A already bans caricature, hate symbols and likenesses in general terms): swastika or any other regime symbol; secret-police stereotype; beer-hall caricature; 'Cabaret' decadence caricature; a likeness of Einstein.

**Send this place's files to Claude** before you start the next place.

When the last place is in, Claude runs the batch check in Unity.

## 23. Batch 11: the Future (21 images)

The Future (2150) is the player's own time. Its look changes with history: whichever country dominates the past, its Future place becomes a destination, and its travellers dress in that culture's Future style. There is no Future traveller before a country leads, so this batch comes last. Folder: `Raw/batch11-future/`.

Make the neutral Future first with prompts 9.4 to 9.6 (it sets the cut and the shared hairstyle and beard). Send the neutral Future to Claude and wait for the go-ahead. Then make each culture Future from the approved neutral outfit with prompt 9.9, and send each one to Claude as soon as it is done. Culture Futures are outfits only: they reuse the neutral hair and beard, and have no headwear or accessory. A motif that describes a shape (cape-sleeves, one-shoulder drapes, wraps) is suggested with panels, seams and trims on the neutral cut, never a new silhouette (issue 10).

### Neutral Future (the shared base)

```
PAIR: neutral_future (the Future, the player's own time, 2150)

MAN
- Outfit: clean, practical civilian clothes in soft technical fabrics: a hip-length jacket with a soft stand collar kept below the chin and simple geometric seams, over a plain fitted top, slim straight trousers, and flat low-profile shoes. Muted slate grey, sand and slate teal-blue.
- Hair: short, neat, textured crop.
- Facial hair: a short, neatly trimmed beard and moustache.

WOMAN
- Outfit: the same practical future style: a long panelled coat-dress with a soft stand collar kept below the chin and simple geometric seams, over slim trousers, with flat low-profile shoes. Muted slate grey, sand and slate teal-blue.
- Hair: sleek, smooth hair drawn back into a low bun at the nape.

MUST READ AT A GLANCE: the soft stand collar and clean geometric seams in slate grey, sand and slate teal-blue.
DO NOT DRAW: armour, helmets, visors, glowing parts, robot parts, weapons, logos, letters or readable text.
```

Files: `outfit_m_neutral_future.png`, `outfit_f_neutral_future.png` (the base of every culture Future and, since v2.3, the present's clothes: import labels "tech jacket" / "coat-dress"), `hair_m_neutral_future.png`, `hair_f_neutral_future.png`, `facialhair_m_neutral_future.png`

### Future shaped by Egypt: Nile Arcology

Costume Guide entry (proposed): **bead-row yoke linen** (the whole outfit; a Future look never leaks)

```
PAIR: egypt_future (Nile Arcology, 2150: the Future after Egypt came to dominate history)
Restyle the approved neutral Future outfits (same cut, same fit) for both the man and the woman with these motifs:
Crisp white and ecru knife-pleated linen; concentric broad-collar necklines reworked as layered circular yokes in bead-row colours (blue-leaning Nile turquoise, lapis blue, carnelian red, gold); kohl-wing graphic lines as piping along seams; vertical fluting taken from lotus and papyrus columns; Mamluk oversized hanging sleeves as dramatic cape-sleeves; a printed or embroidered mashrabiya lattice pattern (not cut-through holes) and irrigation-canal grid quilting. Avoid god emblems, royal crowns and cobra motifs.
Keep it wearable civilian clothing, clearly of 2150 yet clearly Egypt-inspired. No flags, emblems, insignia, letters or readable text; nothing that glows; nothing see-through; no holes cut through the fabric (lattice, perforation and laser-cut motifs are printed or stitched on solid cloth).
```

Files: `outfit_m_egypt_future.png`, `outfit_f_egypt_future.png` (prompt 9.9). Hair and facial hair: the shared neutral Future items. Import labels (Appendix D): outfit "bead-row yoke linen", hair "short textured crop" / "sleek low bun", facial hair "short trimmed beard", all with `artNation: "neutral"` on the hair and beard.

### Future shaped by Iraq / Mesopotamia: Baghdad Garden City

Costume Guide entry (proposed): **fringed lapis coat** (the whole outfit; a Future look never leaks)

```
PAIR: iraq_future (Baghdad Garden City, 2150: the Future after Iraq / Mesopotamia came to dominate history)
Restyle the approved neutral Future outfits (same cut, same fit) for both the man and the woman with these motifs:
Tiered Babylonian fringe as kinetic hem and shoulder fringing; one-shoulder spiral draping; embossed wedge-shaped texture and roll-print bands of abstract geometric pattern (no figures, no writing); glazed-brick lapis blue with ochre, date-palm brown, cream and gold; the taylasan shoulder shawl as a draped shoulder panel; tiraz armbands of abstract embroidered bands (no letters); square-shouldered open 'aba cloaks with gold-thread yoke edging; the concentric circles of Baghdad's Round City and muqarnas honeycomb geometry; flowing parametric curves recalling Iraqi-born Zaha Hadid's architecture. Avoid religious text and deity motifs.
Keep it wearable civilian clothing, clearly of 2150 yet clearly Iraq-inspired. No flags, emblems, insignia, letters or readable text; nothing that glows; nothing see-through; no holes cut through the fabric (lattice, perforation and laser-cut motifs are printed or stitched on solid cloth).
```

Files: `outfit_m_iraq_future.png`, `outfit_f_iraq_future.png` (prompt 9.9). Hair and facial hair: the shared neutral Future items. Import labels (Appendix D): outfit "fringed lapis coat", hair "short textured crop" / "sleek low bun", facial hair "short trimmed beard", all with `artNation: "neutral"` on the hair and beard.

### Future shaped by Greece: Aegean Commonwealth

Costume Guide entry (proposed): **meander-trim drape** (the whole outfit; a Future look never leaks)

```
PAIR: greece_future (Aegean Commonwealth, 2150: the Future after Greece came to dominate history)
Restyle the approved neutral Future outfits (same cut, same fit) for both the man and the woman with these motifs:
Meander (Greek key) and wave-scroll borders as fine metallic trim lines on hems and cuffs. Asymmetric one-shoulder drapes from the himation and chiton, and crisp accordion pleating from the fustanella. Round fibula-style shoulder clasps, and Byzantine gold roundel (segmenta) inserts as circular panels. Palette of chalk white, Aegean blue, terracotta and black from red-figure pottery, with fine olive-leaf line embroidery. Striped hand-woven panels echoing the tagari.
Keep it wearable civilian clothing, clearly of 2150 yet clearly Greece-inspired. No flags, emblems, insignia, letters or readable text; nothing that glows; nothing see-through; no holes cut through the fabric (lattice, perforation and laser-cut motifs are printed or stitched on solid cloth).
```

Files: `outfit_m_greece_future.png`, `outfit_f_greece_future.png` (prompt 9.9). Hair and facial hair: the shared neutral Future items. Import labels (Appendix D): outfit "meander-trim drape", hair "short textured crop" / "sleek low bun", facial hair "short trimmed beard", all with `artNation: "neutral"` on the hair and beard.

### Future shaped by Italy: Mediterranean Union Rome

Costume Guide entry (proposed): **clavus-stripe wrap** (the whole outfit; a Future look never leaks)

```
PAIR: italy_future (Mediterranean Union Rome, 2150: the Future after Italy came to dominate history)
Restyle the approved neutral Future outfits (same cut, same fit) for both the man and the woman with these motifs:
Sweeping curved toga-like wraps with slim vertical clavi-stripe accents. Renaissance slashed-and-puffed sleeves with contrasting linings, and pomegranate damask/velvet patterns rendered as cut or sheen textures. Palette of Florentine crimson, ochre, terracotta, travertine and Carrara-marble veining. Circular tabarro capes with velvet collars, razor-slim 1960s Roman tailoring, and kaleidoscopic swirl prints. Vitruvian circle-and-square geometry, arcade arches and Marconi-style concentric radio-wave rings as graphic motifs.
Keep it wearable civilian clothing, clearly of 2150 yet clearly Italy-inspired. No flags, emblems, insignia, letters or readable text; nothing that glows; nothing see-through; no holes cut through the fabric (lattice, perforation and laser-cut motifs are printed or stitched on solid cloth).
```

Files: `outfit_m_italy_future.png`, `outfit_f_italy_future.png` (prompt 9.9). Hair and facial hair: the shared neutral Future items. Import labels (Appendix D): outfit "clavus-stripe wrap", hair "short textured crop" / "sleek low bun", facial hair "short trimmed beard", all with `artNation: "neutral"` on the hair and beard.

### Future shaped by China: Shanghai Megacity

Costume Guide entry (proposed): **pankou cross-collar coat** (the whole outfit; a Future look never leaks)

```
PAIR: china_future (Shanghai Megacity, 2150: the Future after China came to dominate history)
Restyle the approved neutral Future outfits (same cut, same fit) for both the man and the woman with these motifs:
Diagonal cross-collar (jiaoling) wrap closures and high standing collars fastened with sculpted pankou knot buttons. Deep, flowing sleeves cut as layered opaque panels. Horse-face (mamian) pleated side panels on long coats and skirts. Cloud-scroll (xiangyun) and key-fret (huiwen) borders reworked as fine metallic circuit-trace trim. Paper-fold pleating and layered pale panels like xuan paper pages. A fine movable-type grid texture (blank squares, no characters) and star-chart or compass-rose line engraving for the navigation and knowledge theme. Dark jade-like stone fastenings and lacquer finishes. Palette: ink black, cinnabar lacquer red, dull grey-green celadon, indigo, gold-thread accents. No imperial five-claw dragons, rank squares, flags or political badges.
Keep it wearable civilian clothing, clearly of 2150 yet clearly China-inspired. No flags, emblems, insignia, letters or readable text; nothing that glows; nothing see-through; no holes cut through the fabric (lattice, perforation and laser-cut motifs are printed or stitched on solid cloth).
```

Files: `outfit_m_china_future.png`, `outfit_f_china_future.png` (prompt 9.9). Hair and facial hair: the shared neutral Future items. Import labels (Appendix D): outfit "pankou cross-collar coat", hair "short textured crop" / "sleek low bun", facial hair "short trimmed beard", all with `artNation: "neutral"` on the hair and beard.

### Future shaped by Japan: Neo-Tokyo Bay

Costume Guide entry (proposed): **kasane-collar layers** (the whole outfit; a Future look never leaks)

```
PAIR: japan_future (Neo-Tokyo Bay, 2150: the Future after Japan came to dominate history)
Restyle the approved neutral Future outfits (same cut, same fit) for both the man and the woman with these motifs:
Kimono collar layering (kasane colour stacking) as a signature neckline. Wide, structured obi-style waist cinchers with modular clasps and obijime cord detail. Clean architectural shoulder lines inspired by the haori and kataginu. Hakama pleat geometry in wide trousers and skirts. Precise geometric patterns (asanoha hemp leaf, seigaiha waves, shippo, kikko, yagasuri) rendered as laser-cut-look stitched line work (the fabric stays solid). Sashiko grid stitching and origami-fold panels. Karakuri-inspired hinged seam details (fabric, not machinery) and split-toe tabi footwear. Palette: aizome indigo, sumi black, washi white, vermilion lacquer, Hokusai Prussian blue, pale coral accents. No rising-sun rays, imperial chrysanthemum, flags or military insignia.
Keep it wearable civilian clothing, clearly of 2150 yet clearly Japan-inspired. No flags, emblems, insignia, letters or readable text; nothing that glows; nothing see-through; no holes cut through the fabric (lattice, perforation and laser-cut motifs are printed or stitched on solid cloth).
```

Files: `outfit_m_japan_future.png`, `outfit_f_japan_future.png` (prompt 9.9). Hair and facial hair: the shared neutral Future items. Import labels (Appendix D): outfit "kasane-collar layers", hair "short textured crop" / "sleek low bun", facial hair "short trimmed beard", all with `artNation: "neutral"` on the hair and beard.

### Future shaped by Britain: Thames Barrier London

Costume Guide entry (proposed): **tweed-grid frock coat** (the whole outfit; a Future look never leaks)

```
PAIR: britain_future (Thames Barrier London, 2150: the Future after Britain came to dominate history)
Restyle the approved neutral Future outfits (same cut, same fit) for both the man and the woman with these motifs:
Sharp Savile Row-style tailoring stretched into long frock-coat lines with high stand collars. Herringbone tweed, Fair Isle knit bands and bold Iron Age checks re-rendered as fine woven or circuit-trace micro-patterns. A torc-inspired twisted collar edge sewn into the neckline. Pleated opaque white collar rings, kept below the chin, that echo the Elizabethan ruff. Paisley teardrop scrollwork etched into panels. Brass rivets, railway-line pinstripes and printed punch-card dot grids (not cut-through holes) that recall steam, the telegraph and the computing of Lovelace and Turing. Palette: dark bottle green, oxblood, slate grey, heather and navy, with brass and copper accents.
Keep it wearable civilian clothing, clearly of 2150 yet clearly Britain-inspired. No flags, emblems, insignia, letters or readable text; nothing that glows; nothing see-through; no holes cut through the fabric (lattice, perforation and laser-cut motifs are printed or stitched on solid cloth).
```

Files: `outfit_m_britain_future.png`, `outfit_f_britain_future.png` (prompt 9.9). Hair and facial hair: the shared neutral Future items. Import labels (Appendix D): outfit "tweed-grid frock coat", hair "short textured crop" / "sleek low bun", facial hair "short trimmed beard", all with `artNation: "neutral"` on the hair and beard.

### Future shaped by Germany: Rhine-Ruhr Metropole

Costume Guide entry (proposed): **Bauhaus colour panels** (the whole outfit; a Future look never leaks)

```
PAIR: germany_future (Rhine-Ruhr Metropole, 2150: the Future after Germany came to dominate history)
Restyle the approved neutral Future outfits (same cut, same fit) for both the man and the woman with these motifs:
Bauhaus geometry (circle, square, triangle) in red, yellow, blue and black on graphite. Clean, precision-engineered seams and round lens-like buttons and fastenings that recall Jena optics. Jugendstil 'whiplash' curves as embroidered or laser-cut-look stitched trim (the fabric stays solid). Renaissance slashing and puffing reinterpreted as vented panels with contrasting linings. Frilled, Kruseler-like pleated collars kept below the chin. A fine movable-type grid texture (no letters). Baltic amber and linen-white accents, with the white kept away from red-and-black areas. Avoid black-white-red combinations, eagles, crosses and blackletter lettering.
Keep it wearable civilian clothing, clearly of 2150 yet clearly Germany-inspired. No flags, emblems, insignia, letters or readable text; nothing that glows; nothing see-through; no holes cut through the fabric (lattice, perforation and laser-cut motifs are printed or stitched on solid cloth).
```

Files: `outfit_m_germany_future.png`, `outfit_f_germany_future.png` (prompt 9.9). Hair and facial hair: the shared neutral Future items. Import labels (Appendix D): outfit "Bauhaus colour panels", hair "short textured crop" / "sleek low bun", facial hair "short trimmed beard", all with `artNation: "neutral"` on the hair and beard.

**Send to Claude.**

## 24. Batch 12: the 2150 accessory kit (8 images)

The travellers of 2150 must dress for the time they are going to. One who slips wears a single item of their own time over an otherwise right costume, and the player catches it by comparing it with the Costume Guide. These are those items: one small set, drawn for the man and for the woman. Folder: `Raw/batch12-2150-kit/`.

```
PAIR: neutral_future_kit (the 2150 accessory kit, the player's own time)
Both: small, clean, practical 2150 gadgets in slate grey, sand and slate teal-blue, like the neutral Future outfits. Each stands on its own, over any period costume, and reads at a glance above the desk.
- SMART LENSES: a pair of slim rimless smart lenses: two small rounded-rectangle lenses of flat, opaque pale blue-grey joined by a thin slate-grey bridge, sitting on the nose in front of the eyes.
- COMM EARPIECE: a sleek slate teal-blue comm earpiece hooked over the ear on the viewer's left, with a short slim boom reaching forward along the cheek.
- TRANSIT BADGE: a large rounded-rectangle transit badge in sand with slate teal-blue geometric panels, clipped to the upper chest on the viewer's right (no text, numbers or symbols).
- SHOULDER DISPLAY: a curved slate-grey display panel strapped over the shoulder on the viewer's left, its face flat matte teal-blue (not lit, no text).
MUST READ AT A GLANCE: each item's clean geometric shape in slate grey and teal-blue, unlike any period accessory.
DO NOT DRAW: glowing parts, screens with pictures, letters, numbers or readable text, logos, cables, anything held in the hand.
```

For each item, send the PAIR block once, then prompt 9.8 (Accessory) with the item's name, once for the man and once for the woman.

Files: `accessory_m_neutral_future_lenses.png`, `accessory_m_neutral_future_earpiece.png`, `accessory_m_neutral_future_badge.png`, `accessory_m_neutral_future_display.png`, `accessory_f_neutral_future_lenses.png`, `accessory_f_neutral_future_earpiece.png`, `accessory_f_neutral_future_badge.png`, `accessory_f_neutral_future_display.png`. Import labels (world_source.json `present.kit`): "smart lenses" (`lenses`); "comm earpiece" (`earpiece`); "transit badge" (`badge`); "shoulder display" (`display`).

**Send to Claude.**

## Checklist before you send a place to Claude

- Every file is a 1024 x 1536 portrait PNG (not square) saved with the download button, named exactly as listed.
- The background is one flat green (no checkerboard, floor or shadow), and the magenta figure is still there at the same size and in the same place as in the mannequin file. Open both and flip between them: nothing should jump.
- Nothing touches or runs off any edge of the image: there is green on all four sides, including above the tallest hat.
- No pink or green tint inside white or pale fabric; nothing see-through; no holes through the fabric.
- No shade or shadow painted onto the magenta mannequin.
- Headwear that hides all the hair comes down to the hairline (no bald scalp showing under it).
- Each accessory stands on its own (no belt, sash or cloak drawn for it to hang from).
- Lighting is flat and even: no bright side, no glow, no shadow on the ground.
- No text, logos, flags, badges or insignia anywhere; coins are plain discs.
- The LEAK ITEMS are clearly visible, match the description and sit on the head, face, neck, shoulders or upper chest; the collar, shoulders and chest read clearly (the desk hides the rest in the game).

## Totals

| Batch | Images you make | Game files Claude makes from them |
|---|---|---|
| Batch 1: pilot | 15 | 42 |
| Batch 2: skin tones and faces | 38 | 38 |
| Batch 3: premades (Senenmut, Socrates) | 8 | 8 |
| Batch 4: Day 1 places | 19 | 39 |
| Batch 5: Day 2 places | 60 | 158 |
| Batch 6: premades (Aspasia, Ban Zhao, Arib al-Ma'muniyya, Muhammad al-Khwarizmi) | 16 | 16 |
| Batch 7: Day 3 places | 88 | 225 |
| Batch 8: premades (Johannes Gutenberg, Leonardo da Vinci, Aemilia Lanyer, Cecilia Gallerani) | 16 | 16 |
| Batch 9: Industrial era | 64 | 161 |
| Batch 10: Modern era | 61 | 146 |
| Batch 11: the Future | 21 | 33 |
| Batch 12: the 2150 accessory kit | 8 | 8 |
| **Total** | **414** | **890** |

The game files include the five baked hair colours, the hair-back layers and the four premade expressions. `coverage.json` lists every game file.

## Appendix A. The 68 data fixes (brief_review.json `fixes`)

All 68 are in the PAIR blocks above. "Applied" means the reviewer's text is still used exactly as written (`tools/data_v2.py` compares every field with the reviewer's text). "Applied, then amended" means the fix went in and a later rule changed its text; the note names that rule: a v1 practicality issue (the piece-4 spec's rule R23: where a data fix and a later practicality issue conflict, the issue wins) or a finding of the v2 review (Appendix E). The other statuses say where an issue replaced or merged the fix. Totals: 33 applied, then amended, 28 applied, 5 superseded, 1 applied, then reworded, 1 merged.

| # | Place | Field | Status | Note |
|---|---|---|---|---|
| 1 | egypt_ancient | male.outfit | applied |  |
| 2 | egypt_ancient | signature | applied |  |
| 3 | iraq_ancient | signature | applied |  |
| 4 | greece_ancient | female.outfit | applied |  |
| 5 | greece_ancient | female.accessory | applied |  |
| 6 | greece_ancient | female.headwear | applied |  |
| 7 | italy_ancient | signature | applied, then amended | issue 4; v2.1 (3D office): MUST READ names only what shows above the desk |
| 8 | china_ancient | signature | applied, then amended | v2.1 (3D office): MUST READ names only what shows above the desk |
| 9 | japan_ancient | signature | applied, then amended | issue 4 (on fix 9's wording); v2.1 (3D office): MUST READ names only what shows above the desk |
| 10 | britain_ancient | female.outfit | applied, then amended | issue 21 (on fix 10); issue 4 |
| 11 | germany_ancient | female.outfit | applied, then amended | issue 4 |
| 12 | germany_ancient | signature | applied, then amended | issue 4 |
| 13 | egypt_medieval | female.outfit | applied, then amended | issue 4 |
| 14 | egypt_medieval | female.headwear | applied, then reworded | issue 6's wording (down both sides of the neck) replaces it; same intent |
| 15 | egypt_medieval | male.headwear | applied, then amended | issue 8 |
| 16 | egypt_medieval | signature | applied |  |
| 17 | iraq_medieval | male.accessory | merged | issue 8 moves the ends to the front; fix 17's 'clear of the tiraz armbands' is kept |
| 18 | iraq_medieval | female.headwear | applied, then amended | issue 13 |
| 19 | greece_medieval | male.headwear | applied |  |
| 20 | china_medieval | female.hair | applied |  |
| 21 | japan_medieval | avoid | applied |  |
| 22 | britain_medieval | signature | applied, then amended | review 2 finding 3 (the brooch no longer pins the cloak); v2.1 (3D office): MUST READ names only what shows above the desk |
| 23 | britain_medieval | female.accessory | applied, then amended | review 2 finding 15 (clasp moved into the outfit) |
| 24 | germany_medieval | male.outfit | applied, then amended | issue 4 rule, extended; review 2 finding 15 |
| 25 | germany_medieval | male.accessory | applied, then amended | review 2 finding 15 (purse moved into the outfit) |
| 26 | germany_medieval | female.outfit | applied, then amended | issue 4 rule, extended; review 2 finding 15: the Fuerspan clasp closes the neckline |
| 27 | germany_medieval | signature | applied, then amended | v2.1 (3D office): MUST READ names only what shows above the desk |
| 28 | egypt_earlymodern | male.headwear | applied |  |
| 29 | iraq_earlymodern | signature | applied |  |
| 30 | greece_earlymodern | female.accessory | applied, then amended | v2.1 (3D office): the pafti buckle at the waist is hidden by the desk; chest silverwork is the visible Ioannina item |
| 31 | italy_earlymodern | female.hair | applied, then amended | issue 5 deletes the sheer trinzale net that fix 31 kept |
| 32 | italy_earlymodern | signature | applied, then amended | issue 11 (large brow jewel) and issue 4 (pink -> dusty coral) |
| 33 | japan_earlymodern | signature | applied, then amended | v2.1 (3D office): MUST READ names only what shows above the desk |
| 34 | japan_earlymodern | male.accessory | applied, then amended | review 2 finding 15 (inro moved into the outfit) |
| 35 | britain_earlymodern | female.hair | applied |  |
| 36 | britain_earlymodern | female.headwear | applied |  |
| 37 | germany_earlymodern | female.hair | applied |  |
| 38 | egypt_industrial | male.hair | applied |  |
| 39 | egypt_industrial | female.outfit | applied, then amended | issue 4 rule, extended |
| 40 | egypt_industrial | female.hair | applied, then amended | review 2 finding 8 |
| 41 | egypt_industrial | female.headwear | superseded | spec R23 / issue 8: the tarha falls over both shoulders and down the sides (the kerchief cap stays dropped) |
| 42 | egypt_industrial | signature | superseded | spec R23 / issue 8's signature wording |
| 43 | iraq_industrial | male.hair | applied |  |
| 44 | iraq_industrial | female.outfit | superseded | spec R23 / issue 6: the abaya body stays in the outfit, from the shoulders |
| 45 | iraq_industrial | female.hair | applied, then amended | review 2 finding 8 |
| 46 | iraq_industrial | female.headwear | superseded | spec R23 / issue 6: headwear holds only the head part of the abaya |
| 47 | greece_industrial | female.hair | applied |  |
| 48 | italy_industrial | male.headwear | applied |  |
| 49 | italy_industrial | male.accessory | applied |  |
| 50 | china_industrial | male.accessory | applied, then amended | review 2 finding 15 (purse moved into the outfit) |
| 51 | china_industrial | signature | applied |  |
| 52 | japan_industrial | male.outfit | applied, then amended | review 2 findings 15/35: the himo closes the haori; crests become blank roundels |
| 53 | japan_industrial | female.outfit | applied, then amended | issue 4; review 2 finding 37: contrast with the maroon hakama |
| 54 | japan_industrial | female.accessory | superseded | issue 11 moves the ribbon bow into the hair layer (a bow tied to a hairstyle cannot leak onto another hairstyle); accessory is none |
| 55 | egypt_modern | female.hair | applied, then amended | review 2 finding 8 |
| 56 | iraq_modern | male.hair | applied |  |
| 57 | iraq_modern | female.outfit | applied, then amended | review 2 finding 33 |
| 58 | iraq_modern | female.headwear | applied |  |
| 59 | iraq_modern | female.hair | applied |  |
| 60 | greece_modern | male.accessory | applied, then amended | v2.1 (3D office): the bag rode at the hip, behind the desk; drawn high on the chest |
| 61 | greece_modern | female.accessory | applied, then amended | v2.1 (3D office): the bag rode at the hip, behind the desk; drawn high on the chest |
| 62 | china_modern | female.outfit | applied |  |
| 63 | japan_modern | female.accessory | applied, then amended | review 2 finding 3 (same item as the man's) |
| 64 | japan_modern | signature | applied, then amended | review 2 finding 3; v2.1 (3D office): MUST READ names only what shows above the desk |
| 65 | britain_modern | signature | applied, then amended | v2.1 (3D office): MUST READ names only what shows above the desk |
| 66 | britain_modern | female.headwear | applied |  |
| 67 | germany_modern | male.outfit | applied |  |
| 68 | germany_modern | female.headwear | applied |  |

## Appendix B. The 22 practicality issues (brief_review.json `issues`)

| # | Problem | How v2 handles it |
|---|---|---|
| 1 | ChatGPT redraws the whole image; pixel landmarks | Applied: section 4 text, setup step 6 (never tweak, Regenerate), Batch 1 Step 1 note (Claude re-centres), checklist line 2 (flip between files). Prompt 9.1 refers to the guide's lines, not pixel values. |
| 2 | Images in Project files are not used; finished pieces get copied | Applied: setup step 3 and the files list (attach what each prompt's Attached line names). |
| 3 | PAIR block alone makes ChatGPT draw; one chat per era drifts | Applied: setup step 5 (new chat per PAIR), prompt 9.0 (reply only READY), Block A's last line, every batch intro. |
| 4 | Purple, pink and green get eaten by the key | Applied: Block A colour rule and every listed line; the same rule was also applied to every other pink, rose, purple and light-green word in the looks (Appendix C). The v2 review added turquoise and teal (Appendix E, finding 33). |
| 5 | See-through fabric keys badly | Applied: Block A opacity rule; iraq_medieval veil (fix 18), sheer chemise (fix 39), trinzale deleted, sheer stockings to bare legs, Future China and Britain edits; the Ottoman Egypt tartur's gauze veil was also removed (Appendix C); checklist line 4. The v2 review removed 'chiffon' and the Future motifs that cut holes through fabric (findings 29 and 33). |
| 6 | Garments over the head placed in the outfit | Applied: Mamluk izar (fixes 13-14, issue wording), Ottoman Iraq 'abaya split into outfit (from the shoulders) and headwear (head part) per R23, Kingdom of Iraq (fixes 57-58), prompt 9.4 (collars below the chin), 1843 collar points. |
| 7 | Hair 'hidden under' headwear | Applied: prompt 9.5 as written. Every remaining hair line that named a headwear item was rewritten without it (Appendix C), the same fix the reviewer made in fixes 38, 43, 45, 55, 56 and 59. Headwear that hides all the hair now carries `covers` (Appendix D). |
| 8 | Things that hang behind the body land in front of the outfit | Applied: the hair-back layer (section 1, section 6; Claude splits it), prompts 9.7 and 9.8, the 'adhaba tail in front, the taylasan ends down the front (merged with fix 17), the tarha over both shoulders (R23), the new Ottoman Egypt women's MUST READ. The v2 review set one test for the hair-back flag (section 6, Appendix E finding 8). |
| 9 | Skin variants redrawn; heads pasted across images | Applied: prompts 9.2 and 9.3, Batch 2 text, section 6 body row. The v2 review added skin swatches (section 7) and the head alignment and stacking check (Batch 2). |
| 10 | Future motifs break the layer rules | Applied: prompt 9.9, the Batch 11 intro, and every motif edit (plus: Britain's 'racing green' to bottle green, China's celadon to dull grey-green, Iraq's cuneiform texture made abstract). Future hair is shared (piece 5 R18). |
| 11 | Accessories tied to another layer or too small | Applied to every line the issue listed, and after the v2 review to every other accessory line: about 19 more hung from, tucked into, pinned or closed an outfit item, two of them leak items (Appendix E, findings 3, 15 and 16). Purses, cases, fans and cords that hang from an outfit's own belt are now part of that outfit, and `tools/data_v2.py` checks every accessory line. Where the research has no larger authentic item, the accessory is none (Appendix C). |
| 12 | Headwear drawn tight to the bald scalp | Applied: prompt 9.7 as written. |
| 13 | Tiraz 'pseudo-script' vs the no-text rule | Applied: Block A text rule and the three tiraz/'isaba lines. The Abbasid DO NOT DRAW still asked for pseudo-script until the v2 review fixed it (Appendix E, finding 19). |
| 14 | Kohl on the face layer | Applied: fix 2's MUST READ; heads have no makeup (section 6, prompt 9.1, section 10). |
| 15 | Grey undershirt shows under low necklines | Applied: prompt 9.1 (bandeau and mid-thigh shorts), section 6 body row. |
| 16 | Gold ornaments recoloured with the hair | Applied: Block A hair-ornament rule (after the v2 review: never red); the ornaments to mask are listed per place ('Notes for Claude's processing') and in coverage.json. |
| 17 | The pilot tests none of the risky cases | Applied: Batch 1 Step 3 (the same five files, with the qamis forced to bottle green). The pilot pair itself moved from Victorian Britain to Periclean Athens so it counts toward day 1; v1's batch counts no longer apply. |
| 18 | Premades vs 'no likeness'; expressions drift | Applied: premades have their own Project with Block P (section 11, after the v2 review), prompts 9.10 and 9.11, the note after 9.11. |
| 19 | File names for Future, bases, heads, premades | Adapted: the game's key grammar is now the single naming rule (section 8). Future files follow the same order as every era (`outfit_m_egypt_future`), not v1's flipped order (Appendix C). |
| 20 | Unusable safe-box check; checkerboard | Applied: checklist line 3, Block A (no checkerboard). |
| 21 | Jewellery and badges in outfits | Applied: section 6 outfit row, the La Tene brooch now a plain bronze pin, the shasho badge deleted. |
| 22 | JPG and screenshots | Applied: setup step 7, checklist line 1. |

## Appendix C. Rejected or adapted, with reasons (v1 review)

Nothing was rejected outright. These parts were adapted:

1. **Issue 19's Future file order** (`outfit_m_future_egypt`) is not used. The game's key grammar is `{layer}_{gender}_{country}_{era}` for every place, the Future included (piece-4 R27), and the shared Future hair uses the country token `neutral` (piece-5 R18): `hair_m_neutral_future_brown`. One rule is simpler than two.
2. **Issue 17's pilot pair.** The stress test is kept as written, but the pilot pair is Periclean Athens (day 1) instead of Victorian Britain (day 4), so the pilot's images count toward the first day the player plays. Athens still tests a hat over hair, a beard, a snood and a strap bag.
3. **Issue 11, accessories with no larger researched item.** The reviewer asked to replace small or dependent accessories "with a larger item from costume_research.json". The research offers one for three of them and none for four, so, as fix 5 did for the Athenian woman, no item was invented:
   - italy_ancient man: the signet ring is now **none**;
   - italy_ancient woman: pearl earrings became a **short necklace of large pearls** (the research notes pearls as the Roman status craze);
   - china_medieval woman: pearl earrings are now **none**;
   - china_earlymodern woman: earrings are now **none** (the research's jinbu pendant was dropped there because it looks like Japan's tasselled Nagoya-obi);
   - britain_earlymodern man: the single earring is now **none** (a merchant's gold chain would look like the German early-modern men's chain);
   - china_modern man: the pen in a pocket became the **plain khaki satchel** the research calls "the ubiquitous plain canvas bag of the time";
   - china_ancient man: the issue's embroidered silk pouch hangs from the outfit's sash, so after the v2 review it is part of the outfit and the accessory is **none** (Appendix E, finding 15).
4. **Fix 54 vs issue 11** (Meiji women's ribbon bow): issue 11 wins (R23 rule). The bow is now part of the hairstyle, masked before hair recolouring; the accessory is none. A bow tied to one hairstyle could never leak onto another. After the v2 review the bow is white, not deep red (red is too close to brown hair for the mask).
5. **Fixes 44 and 46 vs issue 6** (Ottoman Iraq 'abaya) and **fixes 41 and 42 vs issue 8** (Khedivate tarha): the issues win, as the piece-4 spec R23 already decided.
6. **Fix 17 vs issue 8** (taylasan): merged. The ends fall down the front (issue 8); it stays clear of the tiraz armbands (fix 17).
7. **Fix 31 vs issue 5** (trinzale net): issue 5 wins; the sheer net is deleted.
8. **Fix 14 vs issue 6** (Mamluk izar hood): same intent; the issue's wording (down both sides of the neck) is used.
9. **Extensions of the reviewer's own rules** (same reasons, lines the review did not list): every remaining pink, rose, purple or light-green colour word became dusty coral, deep wine red or a dark green (19 edits in 17 item texts; "not the purple of high ranks" became "not the bright colours of high ranks"); the Ottoman Egypt chemise and the Ottoman Egypt tartur's hanging gauze veil (sheer, and hanging down the back); the Gugel's long tail down the back (fix 27 had already dropped it from the MUST READ); the New Kingdom sheath's note about sheer over-robes (it named what must not be drawn); 15 hair lines that still named the headwear on top of them; the Ottoman Baghdad men's turban now has one colourway so its leak label names one item.
10. **v1's "the game recolours hair"** is replaced by baked colour variants made by Claude (piece-4 C13; art contract item 9).

## Appendix D. For the piece-4 content import (not for ChatGPT)

The piece-4 plan authors each place's `wardrobe` block in `world_source.json` by hand from the corrected research (its R23). These are the proposals this brief is drawn to, for Saleh's review. The full data is `tools/wardrobe_v2.json`: the corrected text of every item, and each place's `worldSourceWardrobe` already in the spec's format (section 2.13: `{ signature, outfit: {label}, hair: {label, wig?, back?}, facialHair, headwear: {label, covers?}, accessory }`, with `leakable: true` on each signature item). Items marked "none" in a PAIR block are absent (no key).

Checked by `tools/data_v2.py`: every item label is at most 24 characters, ASCII and without "/"; every Culture value is at most 28 characters and unique; the full `Looks.LabelProblems` rule per gender (no two places share a signature label, and no item of any place, in any slot, carries another place's signature label); every accessory stands on its own (no "tucked", "hanging from the sash/belt", "pinning", "closing", "small").

**Every item, per place and gender** (bold = the signature item, which is `leakable`; every signature passes spec C7: an item, readable at the desk's head size (section 1), standing on its own; since v2.1 every leak item sits on the head, face, neck, shoulders or upper chest; flags in brackets):

| Place | g | Signature slot | Outfit | Hair | Facial hair | Headwear | Accessory | Culture value |
|---|---|---|---|---|---|---|---|---|
| egypt_ancient | m | Accessory | pleated linen kilt | bobbed wig [wig] |  |  | **wesekh collar** (leakable) | wesekh collar |
| egypt_ancient | f | Accessory | linen sheath dress | tripartite wig [wig, back] |  | lotus fillet | **wesekh collar** (leakable) |  |
| iraq_ancient | m | FacialHair | fringed wool wrap | low chignon | **curled beard** (leakable) | wool fillet | cylinder seal | curled beard / gold fillet |
| iraq_ancient | f | Headwear | fringed shawl-dress | braid crown |  | **gold fillet** (leakable) |  |  |
| greece_ancient | m | Headwear | chiton and himation | forward curls | rounded beard | **petasos hat** (leakable) [hides top] | pera pouch | petasos hat / sakkos snood |
| greece_ancient | f | Headwear | Ionic chiton | wavy low bun |  | **sakkos snood** (leakable) [hides top] |  |  |
| italy_ancient | m | Hair | toga and tunica | **Caesar crop** (leakable) |  |  |  | Caesar crop / nodus roll |
| italy_ancient | f | Hair | stola and palla | **nodus roll** (leakable) |  |  | pearl monile |  |
| china_ancient | m | Headwear | zhiju wrap robe | Han topknot | moustache and goatee | **jieze cap** (leakable) [hides top] |  | jieze cap / bi-disc pendant |
| china_ancient | f | Accessory | quju wrap robe | low looped bun |  |  | **bi-disc pendant** (leakable) |  |
| japan_ancient | m | Hair | kinu and tied hakama | **mizura** (leakable) |  |  | magatama necklace | mizura / magatama beads |
| japan_ancient | f | Accessory | kinu and mo skirt | board chignon |  |  | **magatama beads** (leakable) |  |
| britain_ancient | m | Accessory | checked tunic and bracae | swept-back mane [back] | long drooping moustache |  | **torc** (leakable) | torc |
| britain_ancient | f | Accessory | checked wool dress | two thick braids |  |  | **torc** (leakable) |  |
| germany_ancient | m | Hair | twill tunic and trousers | **Suebian knot** (leakable) | full short beard |  |  | Suebian knot / amber beads |
| germany_ancient | f | Accessory | linen peplos dress | low bound knot |  |  | **amber beads** (leakable) |  |
| egypt_medieval | m | Headwear | qamis and farajiyya | short crop | rounded beard | **imama turban** (leakable) [covers Hair] | fringed silk girdle | imama turban / izar hood |
| egypt_medieval | f | Headwear | qamis and white izar | pinned braids |  | **izar hood** (leakable) [covers Hair] | filigree collar |  |
| iraq_medieval | m | Headwear | black durra'a robe | smooth nape-length hair | rounded beard | **qalansuwa** (leakable) [hides top] | taylasan shawl | qalansuwa / veil with isaba |
| iraq_medieval | f | Headwear | qamis with tiraz bands | S-curl side locks |  | **veil with isaba** (leakable) [hides top] | pearl choker |  |
| greece_medieval | m | Headwear | buttoned kabbadion | long centre-parted hair [back] | long full beard | **Palaiologan hat** (leakable) [hides top] | gilt-mounted belt | Palaiologan hat / fakiolion |
| greece_medieval | f | Headwear | damask over-dress | pinned-up hair |  | **fakiolion** (leakable) [covers Hair] | lunate earrings |  |
| italy_medieval | m | Headwear | red lucco gown | rounded short crop |  | **mazzocchio hat** (leakable) [hides top] |  | mazzocchio hat / ghirlanda |
| italy_medieval | f | Headwear | gamurra and giornea | drawn-back coil |  | **ghirlanda** (leakable) [hides top] | pearl pendant jewel |  |
| china_medieval | m | Headwear | round-collared robe | crown topknot | thin moustache and beard | **winged futou** (leakable) [hides top] | plaque belt | winged futou / crescent comb |
| china_medieval | f | Headwear | beizi coat and skirt | tall gaoji bun |  | **crescent comb** (leakable) |  |  |
| japan_medieval | m | Headwear | hitatare and hakama | motodori topknot | moustache and chin tuft | **eboshi cap** (leakable) [hides top] |  | eboshi cap / ichime-gasa |
| japan_medieval | f | Headwear | tsubo-shozoku robes | long tied-back hair |  | **ichime-gasa** (leakable) [hides top] |  |  |
| britain_medieval | m | Accessory | cyrtel and leg bindings | collar-length hair | long English moustache |  | **disc brooch** (leakable) | disc brooch / headrail |
| britain_medieval | f | Headwear | bell-sleeved overgown | low nape coil |  | **headrail** (leakable) [covers Hair] |  |  |
| germany_medieval | m | Headwear | pleated Tappert gown | fringed chin-length hair |  | **Gugel hood** (leakable) [covers Hair] |  | Gugel hood / Kruseler veil |
| germany_medieval | f | Headwear | high-waisted gown | pinned plaits |  | **Kruseler veil** (leakable) [covers Hair] |  |  |
| egypt_earlymodern | m | Headwear | quftan and gibba | short crop | short rounded beard | **red-crown turban** (leakable) [covers Hair] | shawl sash | red-crown turban / tartur |
| egypt_earlymodern | f | Headwear | brocade entari | curtain of thin plaits [back] |  | **tartur** (leakable) [hides top] | disc-drop earrings |  |
| iraq_earlymodern | m | Headwear | zaboun and striped aba | short crop | full beard | **checked turban** (leakable) [covers Hair] |  | checked turban / futa shawl |
| iraq_earlymodern | f | Headwear | striped dress and aba | pinned braids |  | **futa shawl** (leakable) [covers Hair] | qilada necklace |  |
| greece_earlymodern | m | Headwear | anteri and fur coat | short crop | full beard | **kalpak** (leakable) [hides top] | striped sash | kalpak / silver chest chains |
| greece_earlymodern | f | Accessory | anteri and zipouni | two front braids |  | cap and tsemberi [hides top] | **silver chest chains** (leakable) |  |
| italy_earlymodern | m | Headwear | coral pitocco | zazzera bob |  | **red berretta** (leakable) [hides top] |  | red berretta / lenza |
| italy_earlymodern | f | Headwear | laced-sleeve gamurra | smooth centre parting |  | **lenza** (leakable) | jet bead necklace |  |
| china_earlymodern | m | Headwear | pale daopao robe | topknot with wangjin | neat moustache and beard | **square gauze cap** (leakable) [hides top] | tasselled sitao sash | square gauze cap / baotou |
| china_earlymodern | f | Headwear | ao and horse-face skirt | high flat bun |  | **baotou** (leakable) |  |  |
| japan_earlymodern | m | Hair | kataginu and hakama | **chasen-mage** (leakable) | thin moustache and beard |  |  | chasen-mage / kazuki veil |
| japan_earlymodern | f | Headwear | Keicho kosode | tamamusubi loop |  | **kazuki veil** (leakable) [hides top] |  |  |
| britain_earlymodern | m | Headwear | doublet and ruff | brushed-up short hair | pick-a-devant beard | **capotain hat** (leakable) [hides top] |  | capotain hat / hat over coif |
| britain_earlymodern | f | Headwear | gown and lace ruff | pinned-up hair |  | **hat over coif** (leakable) [covers Hair] | pomander girdle |  |
| germany_earlymodern | m | Headwear | fur-collared Schaube | short Kolbe | short rounded beard | **Barett** (leakable) [hides top] | gold chain | Barett |
| germany_earlymodern | f | Headwear | slashed gown and Goller | plaits in gold net |  | **Barett** (leakable) [hides top] | layered gold chains |  |
| egypt_industrial | m | Headwear | black stambouline | short trimmed hair | groomed moustache | **tarboush** (leakable) [hides top] | gold watch chain | tarboush / tarha veil |
| egypt_industrial | f | Headwear | striped yelek robe | curtain of thin plaits [back] |  | **tarha veil** (leakable) [hides top] | kirdan choker |  |
| iraq_industrial | m | Headwear | zaboun and camel aba | close crop | moustache, short beard | **chfiyya** (leakable) [covers Hair] |  | chfiyya / abaya veil |
| iraq_industrial | f | Headwear | brocade dress and abaya | long braids |  | **abaya veil** (leakable) [covers Hair] | filigree bangles |  |
| greece_industrial | m | Headwear | fustanella | short tapered hair | upturned moustache | **fesi** (leakable) [hides top] | red silk sash | fesi |
| greece_industrial | f | Headwear | Amalia dress | braids round the crown |  | **fesi** (leakable) | gold disc necklace |  |
| italy_industrial | m | Accessory | suit and tabarro | pomaded side parting | Umbertine moustache | wide black felt hat [hides top] | **silk scarf** (leakable) | silk scarf / coral beads |
| italy_industrial | f | Accessory | dress and black scialle | high curled chignon |  |  | **coral beads** (leakable) |  |
| china_industrial | m | Headwear | changshan and magua | queue |  | **guapimao cap** (leakable) [hides top] |  | guapimao cap / meile band |
| china_industrial | f | Headwear | trimmed ao and skirt | low flat bun |  | **meile band** (leakable) | white jade bangle |  |
| japan_industrial | m | Headwear | haori and hakama | zangiri crop | trimmed moustache | **bowler hat** (leakable) [hides top] |  | bowler hat / sokuhatsu |
| japan_industrial | f | Hair | yagasuri and hakama | **sokuhatsu** (leakable) |  |  |  |  |
| britain_industrial | m | Headwear | frock coat and stock | curled side parting | mutton-chop whiskers | **top hat** (leakable) [hides top] | watch chain | top hat / poke bonnet |
| britain_industrial | f | Headwear | plaid day dress | ringlets and bun |  | **poke bonnet** (leakable) [hides top] | Paisley shawl |  |
| germany_industrial | m | Headwear | Gehrock and Stehkragen | brush cut | turned-up moustache | **Homburg** (leakable) [hides top] | pince-nez | Homburg / Jugendstil pendant |
| germany_industrial | f | Accessory | Reformkleid | soft low chignon |  |  | **Jugendstil pendant** (leakable) |  |
| egypt_modern | m | Accessory | galabiya and jacket | combed-back hair | thick moustache |  | **fringed shal** (leakable) | fringed shal / mandil |
| egypt_modern | f | Headwear | printed galabiya | smooth plaited hair |  | **mandil** (leakable) [hides top] | gold bangles |  |
| iraq_modern | m | Headwear | light wool suit | side-parted short hair | trimmed moustache | **sidara** (leakable) [hides top] | horn-rimmed glasses | sidara / gold coin pendant |
| iraq_modern | f | Accessory | 1950s dress and abaya | permed curls |  | abaya over the head [hides top] | **gold coin pendant** (leakable) |  |
| greece_modern | m | Accessory | corduroy and flares | shaggy 1970s cut | thick full moustache |  | **tagari bag** (leakable) | tagari bag |
| greece_modern | f | Accessory | embroidered blouse | long loose hair [back] |  |  | **tagari bag** (leakable) |  |
| italy_modern | m | Accessory | slim mohair suit | slicked-back hair |  |  | **dark shades** (leakable) | dark shades / cat-eye shades |
| italy_modern | f | Accessory | print sheath dress | bouffant |  |  | **cat-eye shades** (leakable) |  |
| china_modern | m | Headwear | Zhongshan suit | side-parted crop |  | **navy cap** (leakable) [hides top] | khaki satchel | navy cap |
| china_modern | f | Headwear | Zhongshan jacket | clipped bob |  | **navy cap** (leakable) [hides top] | khaki satchel |  |
| japan_modern | m | Accessory | salaryman suit | combed side parting |  |  | **orange headphones** (leakable) | orange headphones |
| japan_modern | f | Accessory | office waistcoat set | Seiko-chan cut |  |  | **orange headphones** (leakable) |  |
| britain_modern | m | Headwear | tweed and Fair Isle | short back and sides |  | **flat cap** (leakable) [hides top] | college scarf | flat cap / headscarf |
| britain_modern | f | Headwear | twinset and tweed skirt | pin-curl set |  | **headscarf** (leakable) [hides top] | pearl strand |  |
| germany_modern | m | FacialHair | rumpled suit and coat | brushed-back hair | **walrus moustache** (leakable) | broad soft felt hat [hides top] | round spectacles | walrus moustache / cloche |
| germany_modern | f | Headwear | Bauhaus drop-waist dress | Bubikopf bob |  | **cloche** (leakable) [hides top] | Bauhaus necklace |  |

**`covers`.** Headwear that hides all the hair carries `covers: ["Hair"]`, so `Looks.Compose` draws no hair under it and `CanLeak` row 5 refuses a hair leak onto that claim: egypt_medieval m (imama turban); egypt_medieval f (izar hood); greece_medieval f (fakiolion); britain_medieval f (headrail); germany_medieval m (Gugel hood); germany_medieval f (Kruseler veil); egypt_earlymodern m (red-crown turban); iraq_earlymodern m (checked turban); iraq_earlymodern f (futa shawl); britain_earlymodern f (hat over coif); iraq_industrial m (chfiyya); iraq_industrial f (abaya veil). The PAIR block tells ChatGPT to bring such headwear down to the hairline. Headwear that hides only the top of the head ("hides top": most hats and caps, and the veils and scarves that show the front hair, such as Abbasid Baghdad's veil with its side-curls and post-war Britain's headscarf) has no `covers`, or the honest hair around it would vanish.

**Confusable pairs to author** (`looks.confusable`; a leak between these places in this slot would be hard to see, so `Looks.CanLeak` refuses it). Every signature item was checked against every other place's same-gender item in its slot; `tools/confusable_candidates.txt` prints that whole grid for Saleh's review.

| Place A | Place B | Slot | Gender | Why |
|---|---|---|---|---|
| greece_ancient | italy_ancient | Hair | m | short forward-brushed crops look alike (spec 2.14) |
| egypt_industrial | greece_industrial | Headwear | m | tarboush vs fesi: both red fez-type caps (research note 15) |
| iraq_medieval | japan_medieval | Headwear | m | two tall black caps (research note 6) |
| china_earlymodern | greece_earlymodern | Headwear | m | tall black caps; check at thumbnail (research note 11) |
| china_modern | britain_modern | Headwear | m | soft peaked caps (research note 20) |
| britain_earlymodern | britain_industrial | Headwear | m | capotain vs top hat: two tall black narrow-brimmed hats |
| egypt_medieval | egypt_earlymodern | Headwear | m | two big white turbans; only the red crown tells them apart |
| germany_industrial | germany_modern | Headwear | m | Homburg vs the pinched soft felt hat: two dark dented felt hats |
| iraq_ancient | greece_medieval | FacialHair | m | chest-length curled beard vs the Byzantine long full beard |
| iraq_ancient | germany_medieval | FacialHair | m | the Gugel's neck and cape hide most of a chest-length beard |
| germany_modern | greece_modern | FacialHair | m | walrus vs thick full moustache (same day-5 world) |
| germany_modern | greece_industrial | FacialHair | m | walrus vs thick upturned moustache |
| germany_modern | italy_industrial | FacialHair | m | walrus vs full, thick Umbertine moustache (research note 19) |
| germany_modern | egypt_modern | FacialHair | m | walrus vs thick, neat moustache |
| germany_modern | egypt_industrial | FacialHair | m | walrus vs full, groomed moustache |
| germany_modern | iraq_industrial | FacialHair | m | walrus vs heavy moustache (with a short beard) |
| germany_modern | britain_ancient | FacialHair | m | walrus vs long drooping moustache past the mouth corners |
| germany_modern | britain_medieval | FacialHair | m | walrus vs long full English moustache |
| egypt_modern | iraq_medieval | Accessory | m | long dark fringed shal vs dark taylasan, both ends down the chest |
| iraq_industrial | iraq_modern | Headwear | f | both are a black 'abaya over the head |
| iraq_earlymodern | iraq_industrial | Headwear | f | black futa head-shawl vs black 'abaya over a black futa |
| iraq_earlymodern | iraq_modern | Headwear | f | black futa head-shawl vs black 'abaya over the head |
| egypt_industrial | egypt_medieval | Headwear | f | white tarha veil over both shoulders vs white izar hood |
| iraq_medieval | egypt_medieval | Headwear | f | light khimar veil vs white izar hood (only the 'isaba band differs) |
| iraq_medieval | egypt_industrial | Headwear | f | light khimar veil vs white tarha veil (only the 'isaba band differs) |
| china_earlymodern | china_industrial | Headwear | f | two black brow bands |
| italy_earlymodern | china_earlymodern | Headwear | f | dark brow bands with a central jewel (research note 14) |
| italy_earlymodern | china_industrial | Headwear | f | dark brow bands with a central jewel |
| egypt_modern | britain_modern | Headwear | f | two triangular headscarves (research note 22) |
| egypt_modern | greece_earlymodern | Headwear | f | coloured headscarves tied at the back |
| greece_ancient | egypt_modern | Headwear | f | patterned sakkos vs mandil: cloth covering the back of the head |
| greece_ancient | greece_earlymodern | Headwear | f | patterned sakkos vs patterned tsemberi |
| greece_industrial | greece_earlymodern | Headwear | f | two small red caps on the crown |
| germany_ancient | italy_industrial | Accessory | f | chunky orange amber vs red coral beads |
| iraq_modern | greece_industrial | Accessory | f | plain gold discs at the neck (coin pendant vs disc necklace) |
| iraq_modern | iraq_earlymodern | Accessory | f | plain gold discs at the neck (coin pendant vs qilada) |
| greece_earlymodern | germany_earlymodern | Accessory | f | festoons of chains on the chest (silver vs layered gold) |

**Generated pairs: a hair signature hidden under a hat** (98 entries in `wardrobe_v2.json` `confusableHidden`, slot `Hair`). The telling part of these hairstyles sits on the top or upper side of the head, so under a claim's hat or cap ("hides top") the leak would be invisible although the proof would land. Mizura (loops at ear level) stays visible under a hat and needs none.

- Suebian knot (germany_ancient, man) is refused under the headwear of: britain_earlymodern, britain_industrial, britain_modern, china_ancient, china_earlymodern, china_industrial, china_medieval, china_modern, egypt_industrial, germany_earlymodern, germany_industrial, germany_modern, greece_ancient, greece_earlymodern, greece_industrial, greece_medieval, iraq_medieval, iraq_modern, italy_earlymodern, italy_industrial, italy_medieval, japan_industrial, japan_medieval.
- nodus roll (italy_ancient, woman) is refused under the headwear of: britain_industrial, britain_modern, china_modern, egypt_earlymodern, egypt_industrial, egypt_modern, germany_earlymodern, germany_modern, greece_ancient, greece_earlymodern, iraq_medieval, iraq_modern, italy_medieval, japan_earlymodern, japan_medieval.
- Caesar crop (italy_ancient, man) is refused under the headwear of: britain_earlymodern, britain_industrial, britain_modern, china_ancient, china_earlymodern, china_industrial, china_medieval, china_modern, egypt_industrial, germany_earlymodern, germany_industrial, germany_modern, greece_earlymodern, greece_industrial, greece_medieval, iraq_medieval, iraq_modern, italy_earlymodern, italy_industrial, italy_medieval, japan_industrial, japan_medieval.
- chasen-mage (japan_earlymodern, man) is refused under the headwear of: britain_earlymodern, britain_industrial, britain_modern, china_ancient, china_earlymodern, china_industrial, china_medieval, china_modern, egypt_industrial, germany_earlymodern, germany_industrial, germany_modern, greece_ancient, greece_earlymodern, greece_industrial, greece_medieval, iraq_medieval, iraq_modern, italy_earlymodern, italy_industrial, italy_medieval, japan_industrial, japan_medieval.
- sokuhatsu (japan_industrial, woman) is refused under the headwear of: britain_industrial, britain_modern, china_modern, egypt_earlymodern, egypt_industrial, egypt_modern, germany_earlymodern, germany_modern, greece_ancient, greece_earlymodern, iraq_medieval, iraq_modern, italy_medieval, japan_earlymodern, japan_medieval.

These generated pairs work with the spec as written. A simpler alternative for Saleh (open question 2 in Appendix E): one extra `CanLeak` row, "a Hair signature is refused when the claim wears headwear that hides the top of the head", with a `hidesTop` flag on those headwear items instead of 98 pairs.

Not authored, with the reason: the spec's section 2.14 asks for "greece/italy/germany Industrial men's facial hair (#19) at least". No Industrial man's signature is facial hair (theirs are the fesi, the silk scarf and the Homburg), so a facial-hair leak can never happen between those three places; the pairs would be unreachable. The Modern walrus moustache, which can leak onto them, has its own pairs above.

**Future places** (piece 5, section 2.14 and R18): signature slot `Outfit` for both genders (a Future home never leaks), hair and facial hair with `artNation: "neutral"`, no headwear or accessory. Each is in `wardrobe_v2.json` `future[].worldSourceWardrobe` and in `coverage.json` `wardrobeFlags`.

| Place | Culture value = outfit label (both genders) | Hair (m / f) | Facial hair (m) |
|---|---|---|---|
| egypt_future | bead-row yoke linen | short textured crop / sleek low bun (`artNation: neutral`) | short trimmed beard (`artNation: neutral`) |
| iraq_future | fringed lapis coat | short textured crop / sleek low bun (`artNation: neutral`) | short trimmed beard (`artNation: neutral`) |
| greece_future | meander-trim drape | short textured crop / sleek low bun (`artNation: neutral`) | short trimmed beard (`artNation: neutral`) |
| italy_future | clavus-stripe wrap | short textured crop / sleek low bun (`artNation: neutral`) | short trimmed beard (`artNation: neutral`) |
| china_future | pankou cross-collar coat | short textured crop / sleek low bun (`artNation: neutral`) | short trimmed beard (`artNation: neutral`) |
| japan_future | kasane-collar layers | short textured crop / sleek low bun (`artNation: neutral`) | short trimmed beard (`artNation: neutral`) |
| britain_future | tweed-grid frock coat | short textured crop / sleek low bun (`artNation: neutral`) | short trimmed beard (`artNation: neutral`) |
| germany_future | Bauhaus colour panels | short textured crop / sleek low bun (`artNation: neutral`) | short trimmed beard (`artNation: neutral`) |

**Premades** (the spec's section 2.14 cast table, rebalanced per Amendment A1; birth dates are inside each place's birth years, invented where history gives only a year; record notes are ASCII and under 120 characters):

| id | name | gender | place | truePlace | birthDate | archetype | recordNote | schedule |
|---|---|---|---|---|---|---|---|---|
| senenmut | Senenmut | m | egypt_ancient |  | 14 Mar 1505 BCE | artist | Steward of the Pharaoh's household and architect of her temple. | forced: day 1, slot 3 (story) |
| socrates | Socrates | m | greece_ancient | italy_ancient | 6 Jun 470 BCE | wanderer | Philosopher. Known to question officials at length. | forced: day 2, slot 6 (story); pooled day 3 |
| aspasia | Aspasia | f | greece_ancient |  | 12 Sep 470 BCE | diplomat | Teacher of rhetoric from Miletus. Keeps the company of Pericles. | pooled days 2-3 |
| banzhao | Ban Zhao | f | china_ancient |  | 3 Feb 45 | scientist | Historian of the Han court. Completed the Book of Han. | pooled days 2-3 |
| arib | Arib al-Ma'muniyya | f | iraq_medieval |  | 20 May 797 | artist | Singer, poet and composer of the caliph's court. | pooled days 2-3 |
| khwarizmi | Muhammad al-Khwarizmi | m | iraq_medieval |  | 3 Apr 780 | scientist | Scholar of the House of Wisdom. Writes on calculation. | pooled days 2-3 |
| gutenberg | Johannes Gutenberg | m | germany_medieval |  | 24 Jun 1400 | scientist | Goldsmith of Mainz. Business in Strasbourg undisclosed. | pooled day 3 |
| leonardo | Leonardo da Vinci | m | italy_earlymodern |  | 15 Apr 1452 | artist | Painter and engineer in the service of the Duke of Milan. | pooled day 3 |
| lanyer | Aemilia Lanyer | f | britain_earlymodern |  | 27 Jan 1569 | artist | Poet of London. Her book is not yet printed. | pooled day 3 |
| gallerani | Cecilia Gallerani | f | italy_earlymodern |  | 3 Mar 1473 | artist | Poet and letter writer of the Sforza court in Milan. | pooled day 3 |

Day pools this implies: day 1 forced `senenmut` (slot 3); day 2 forced `socrates` (slot 6), pool `aspasia, banzhao, arib, khwarizmi`; day 3 pool `socrates, aspasia, banzhao, arib, khwarizmi, gutenberg, leonardo, lanyer, gallerani` (9 ids). Every claim and true place is in its day's world, and no day-3 rule forbids a claim (none claims Northern Song Kaifeng or Tokugawa Edo). Cecilia Gallerani's birth year, 1473, lies in Sforza Milan's range (1425 to 1477, Amendment A1). The Senenmut dialog and the Socrates intro stay as the spec has them.

**Knock-on edits in the other documents** (they still describe the old cast; to make when the spec is amended):

1. Piece-4 spec section 1.4 ("Who"): replace the list of eight men with the ten above.
2. Piece-4 spec section 2.14: the premade table (all ten, with genders; Cai Lun, Zeami and Shakespeare leave the cast), the days (day 2 pool `aspasia, banzhao, arib, khwarizmi`; day 3 pool as above), the notes (ten premades; longest name still "Muhammad al-Khwarizmi", 21; every note under 120 characters) and the remark that the set "is all male", which goes. Its confusable sentence gets the reason above for not authoring pair #19.
3. Piece-4 spec C14 and R19 ("a starter set of eight", "Eight premades"): ten.
4. Piece-4 spec section 2.17: "8 `Premade_*.asset`" becomes 10.
5. Piece-5 spec R22, its section 1.7 note and its section 2.14 `days[]` line: days 4 to 6 carry day 3's pool of 9 ids, not seven.
6. `docs/CHARACTER_ART_CONTRACT.md` item 7 (premades, when it is written): premades are made in their own Project with Block P and drawn on Claude's `premadebase` figures, not "in a separate chat without the no-likeness clause".

## Appendix E. The review of v2: applied, adapted and rejected

The v2 brief was reviewed on 2026-09-24 (43 findings). Nothing was rejected. Two findings were applied in an adapted form, and each says why. Findings are numbered in the review's order.

| # | Severity | Finding | Status | What changed |
|---|---|---|---|---|
| 1 | high | Poster delivered at 1024 x 1408 with the placeholder's PPU 100 renders about 13 times too big | Applied, then superseded (v2.1) | UI_ART_RULES Posters and Delivery, coverage.json officeArt: the poster was scaled to 320 x 440 with its .meta at 400 pixels per unit (0.8 x 1.1 world units, the old booth's posters); the wallpaper's target was 1920 x 1080. v2.1: the 3D office has no poster (deferred) and the desktop wallpaper is 4:3, 1440 x 1080 (UI_ART_RULES). |
| 2 | medium | No `leakable` or `covers`; non-signature items have no labels | Applied, adapted | Every item has a label and every signature is `leakable` (Appendix D); `data_v2.py` runs the full per-gender LabelProblems rule. `covers: [Hair]` on the 12 headwear items that hide all the hair. Adapted: post-war Britain's headscarf and Abbasid Baghdad's veil show the front hair and side-curls, so they get no `covers` (the honest hair would vanish); instead, hair signatures read on the top of the head get generated confusable pairs under every such headwear. |
| 3 | medium | Five signature items hang off another layer or are tiny | Applied | Bi-disc on its own cord round the neck, palm-wide; the Anglo-Saxon brooch face-wide at the shoulder with the cloak fastening itself; headphones round the neck with no cord or player; the Jugendstil brooch became a face-wide pendant on its own chain (label `Jugendstil pendant`); the lenza a finger-wide band with an eye-sized stone. The non-signature lines: finding 15. |
| 4 | medium | Confusable list incomplete | Applied | Every signature checked against every same-gender item in its slot (`tools/confusable_candidates.txt`); 27 hand pairs added, including all seven the finding lists (the Caesar-crop ones through `covers` and the generated 'hidden under a hat' set, because every one of those claims wears a turban or a hat), plus 94 generated pairs. Pair #19 is unreachable and not authored (reason in Appendix D). Two items were made clearer instead of paired: the petasos is undyed tan felt (apart from the black wide-brimmed hats of Bologna and Weimar) and the Ottoman Baghdad turban has bold checks. |
| 5 | medium | Premade descriptions break their place's DO NOT DRAW | Applied | Leonardo clean-shaven (the Milanese fashion of the 1490s; his look at 43 is unknown), Gutenberg clean-shaven in the Gugel (no likeness survives), Senenmut's pleated over-kilt removed; Strozzi replaced (finding 6). Section 11 keeps 'the place's list applies' and says every description was checked against it. |
| 6 | medium | Honest premades don't wear their place's Costume Guide item | Applied | Leonardo red berretta, Gutenberg Gugel, Lanyer hat over coif, Ban Zhao bi-disc, Socrates petasos worn on the head (slung would hang behind him, unseen). Strozzi is swapped for Cecilia Gallerani (Sforza Milan, born 1473, an A1 candidate), whose portrait shows exactly the lenza; Strozzi is listed as an alternate. The cast table names each premade's item. |
| 7 | low | The premade chat inherits Block A's hair colour and mannequin rules | Applied | Block P (section 11) has no mannequin, medium-brown or READY rule, and says to draw hair in the colour described. |
| 8 | low | Hair-back flag set inconsistently | Applied | One test (section 6): back only for loose long hair, a wide wig or a spread curtain of plaits. 6 hair items are back; single plaits, pairs of braids and tails are hidden from the front and say so; the Ioannina and Iron Age braids now fall in front of the shoulders (the Ioannina headwear line already said the braids stay visible). |
| 9 | low | coverage.json does not match the brief | Applied | Future wardrobe entries added; one ornament key (`ornamentsToMask`); bodies 2 to 5 are produced by `base_[m/f]_skin1_facea` (recoloured to the section-7 swatches); the Costume Guide cover has its path and size (`Assets/Art/UI/Investigation/refbook_cover_culture.png`, 400 x 560). |
| 10 | low | Other documents still describe the old cast | Applied | Appendix D lists the knock-on edits (piece-4 sections 1.4, 2.14, C14, R19, 2.17; piece-5 R22: nine ids). |
| 11 | low | The brief never names docs/CHARACTER_ART_CONTRACT.md | Applied | Precedence paragraph at the top: the brief implements the contract; ART_ASSET_LIST.md section D, PRODUCTION_PLAN's character direction and `traveller.png` are retired. |
| 12 | low | The guide invites crowns | Applied | The headroom label reads 'tall hats, headdresses and hair buns may extend up here' (finding 40 offered a shorter wording; this one keeps headdresses such as the tartur). |
| 13 | info | Check of the brief_review.json fixes | No action | Confirmed; the remaining gaps are the other findings. |
| 14 | high | Block A and the first message name Papers, Please | Applied | Both describe the gameplay only; section 2 says never to name a game. |
| 15 | high | About 19 accessory lines still hang from, tuck into, pin or close an outfit item | Applied | Purses, pen cases, fan, inro, notebook, hebao, toggle pins, fibula, clasps and the haori-himo moved into their outfits (the accessory is none); the pomander brings its own girdle; the Florentine brooch became a pendant on its own chain; the two signature items as in finding 3. `data_v2.py` fails on any accessory text matching the finding's pattern (plus 'at the collar' and 'where the'). |
| 16 | high | Two signature brooches with no size | Applied | Finding 3; the women's Anglo-Saxon 'small' brooch is now a plain clasp in the outfit. |
| 17 | medium | Block A for premades pasted by hand ten times | Applied | Block P lives in a second Project, 'Time Sorter Premades'; each character's chat inherits it. |
| 18 | medium | Premades contradict the DO NOT DRAW lists; Leonardo's beard | Applied | Finding 5. |
| 19 | medium | Abbasid pseudo-script and China Future's movable type | Applied | 'any Arabic letters or writing, real or fake, on clothing'; 'a fine movable-type grid texture (blank squares, no characters)'. |
| 20 | medium | Long DO NOT DRAW lists name slurs and hate symbols | Applied, adapted | Each list is split: ChatGPT gets the confusions and anachronisms; caricature, slur, hate-symbol and likeness items sit on a review line outside the PAIR block. 'Coolie hat' became 'conical straw hat (douli)'. Adapted: a few neutral guard items that ChatGPT is likely to add stay in its list in plain words (a badge on the jacket, a star on the cap, a monocle). |
| 21 | medium | Heads b to d drift; beards, glasses and caps misalign | Applied | Batch 2: Claude warps each head onto the face-a marks, rejects heads that stay a few pixels off, and runs a stacking check (face d hardest), repeated when the first glasses and close caps arrive. |
| 22 | medium | No style anchor after Batch 1; unmeasurable style words | Applied | `style_card.png` after the pilot (greyscale Athens outfits plus a swatch), attached as 'STYLE REFERENCE ONLY' to every garment prompt; Block A: outline #3B2A20, shade the same hue about 20% darker, at most one small highlight. |
| 23 | medium | Shade under brims lands on the magenta | Applied | MANNEQUIN RULE: never shade onto the mannequin; section 4 says why; Claude adds contact shade. |
| 24 | medium | Errors surface only after a whole batch | Applied | Send each place (and each premade) as soon as it is done; wait for the go-ahead on the first place of every batch. |
| 25 | medium | Walrus and curled-beard look-alikes | Applied | Eight walrus pairs and two curled-beard pairs (Byzantine beard; the Gugel hides most of a chest-length beard), and the walrus text made extreme. |
| 26 | medium | Culture palettes invite flags | Applied | UI_ART_RULES avoid lists: Italy no green, white and red side by side; Germany no black-red-gold (and still no black-white-red); Japan no red sun or red disc. |
| 27 | medium | Poster PPU (same as finding 1) | Applied, then superseded (v2.1) | Finding 1. |
| 28 | medium | 'Travel poster' primes lettering | Applied | 'an illustrated wall print in a flat vintage-poster style, with no title area, no banner and no lettering of any kind'; 'no title band' in the checklist. |
| 29 | medium | Future motifs cut holes through fabric | Applied | Printed or embroidered lattice and punch-card patterns, 'laser-cut-look stitched' line work on solid fabric; section 10 and prompt 9.9 ban holes. |
| 30 | medium | Premades attach drifting skin 2 to 5 figures | Applied | Claude makes `premadebase_[m/f]_skin[N].png` after Batch 2; prompt 9.10 attaches it; Claude registers every premade image to the landmarks. |
| 31 | low | Red hair ornaments defeat the mask | Applied | Ornaments are white, silver or blue (black where the look says so), never red; the Meiji bow is white. |
| 32 | low | The first message bans all green | Applied | 'never bright green or lime (dark bottle or olive green is fine)'. |
| 33 | low | Colour words that invite banned colours | Applied | 'ruby red, sapphire blue or amber'; 'cotton'; 'blue-leaning turquoise'; 'slate teal-blue'; the stress test forces the qamis to deep bottle green. |
| 34 | low | Coins come back with faces or lettering | Applied | Every coin is a plain gold disc with a raised rim and no face, letters or marks; Block A and section 10 say so too. |
| 35 | low | Meiji 'generic family crests' | Applied | 'five small plain white circles (blank crest roundels with no design inside)'. |
| 36 | low | Crescent pendants | Applied | 'a row of teardrop and disc pendants'. |
| 37 | low | Two near-identical reds in the Meiji girl's dress | Applied | Indigo-and-white yagasuri. |
| 38 | low | Futou wing width ambiguous | Applied | 'from wing tip to wing tip about one and a half times the shoulder width, inside the safe area'. |
| 39 | low | 'Office lady company uniform' | Applied | 'Office outfit'. |
| 40 | low | Guide: no feet; 'crowns' in the headroom label | Applied | Simple front-view feet end on y = 1490; label as in finding 12. |
| 41 | low | Skin tones only in words; 'never pink' may grey the lips | Applied | Five hex swatches (section 7, prompt 9.2, coverage.json); Block A allows natural skin and lip colours on faces and bodies. |
| 42 | low | Appendix A misreports statuses | Applied | `data_v2.py` compares every fix with the reviewer's text; changed rows read 'applied, then amended' with the rule that changed them. |
| 43 | low | Wording and accuracy details | Applied | '(reserved for Muslims under Ottoman sumptuary law)'; 'as Caesar rose to power, c. 50 BCE'; Iraq Future 'roll-print bands of abstract geometric pattern (no figures, no writing)'; Germany Future keeps white away from red-and-black areas. |

Also changed while applying them (same reasons): the Florentine women's brooch became a pendant on its own chain (finding 15); the Khedivate and Ottoman Egypt plait curtains are described as showing beside the neck, so their hair-back flag passes the one test (finding 8); the Qing woman's outfit names flat cloth shoes, so the 'bound feet' warning can leave ChatGPT's list (finding 20); the watch chains no longer name a waistcoat (finding 15).

**Open questions for Saleh**

1. **Cast swap.** Strozzi out, Cecilia Gallerani in (finding 6). Keep, or bring Strozzi back and accept that her widow's veil looks unlike Florence's Costume Guide row?
2. **Hair under hats.** 98 generated confusable pairs keep the hair signatures (Caesar crop, Suebian knot, chasen-mage, nodus roll, sokuhatsu) from leaking invisibly under a claim's hat. Keep them as data, or add one `CanLeak` row and a `hidesTop` headwear flag to the piece-4 spec instead?
3. **Headphones round the neck.** Showa Tokyo's men wore them on the head with a Walkman on the belt; the leak item now rests round the neck for both genders so it stands alone. Acceptable?
4. **Leonardo and Gutenberg without beards.** Chosen to fit their places' rules and the historical record; their famous later images have beards, so they are harder to recognise. Acceptable, given that the name appears on their papers?
5. **Poster size.** Closed in v2.1: the 3D office has no poster. When the art side adds a poster frame to the office, the poster becomes a texture in it, sized by that frame (UI_ART_RULES, "Posters: later").

## Appendix F. v2.1: the desk view and the leak items it can see

**How the desk view was measured.** In the art office on `main` (d5844d0: the art scene of art 23aa6e1 with the gameplay layer), the office camera sits at (0, 2.16, -2.62), pitched 10 degrees down, with a vertical field of view of 55 degrees; the traveller stands at the default `Anchor_Traveller` (0, 0, 1.6), 1.8 m from the soles to the top of the head (`DeskConfigSO.travellerHeight`, `TravellerView.Stand`), a yaw-only billboard tinted (0.9, 0.88, 0.84). Projecting the `LookCanvas` landmarks through that camera gives the placeholder's hat top at y 407, the chin at 505 and the sleeve ends at 613 on a 1920 x 1080 screen; the layered placeholder in `p9_day2_bubble_hieroglyphs.png` (a screenshot of piece 9's play-through) shows them at 406.5, 505 and 609 to 613, so the projection holds. The head (the top of the head, y 260, to the chin, y 424) is 58 px tall; the "about 85 px" read off that screenshot earlier was the top of the head to the shoulders (85 px). The NEXT sign's top edge meets the figure at y 622 to 628 on screen, the waist line (canvas y 760 projects to 622), and the sign is as wide as the figure's hands. The field of view is vertical, so every size scales with the screen height (39 px at 720 lines).

**The rule.** A leak item sits on the head, face, neck, shoulders or upper chest (above y 630), is leakable over any other place's look, differs from every other place's signature (the candidate grid in `tools/confusable_candidates.txt`; `Looks.LabelProblems` in `tools/data_v2.py`), is respectful, and breaks no line of its place's DO NOT DRAW list. An item the look already has (its headwear or hair) was preferred when it passed those checks.

| Place | Gender | v2 leak item (where) | v2.1 leak item | Why | Source |
|---|---|---|---|---|---|
| greece_earlymodern (Ottoman Ioannina) | woman | pafti buckle (Accessory, the waist: behind the NEXT sign) | **silver chest chains** (Accessory) | The look's headwear (cap and tsemberi) is already confusable with three other women's headwear (the sakkos, the mandil, the fesi), and its two front braids look like Iron Age Britain's; Epirote silverwork keeps Ioannina's identity where the desk shows it. The pafti stays drawn, as the buckle that closes the outfit's belt. A confusable pair with Renaissance Nuremberg's layered gold chains (the one similar item) keeps a hard-to-see leak out. | Greek women's chest ornaments of silver chains (for example the Benaki Museum's chest ornament of chains; the kioustekia of Greek costume) and Ioannina's filigree workshops ("Wearing Silver", The Athenian, 1990); back-projected to c. 1700 like the pafti itself, as the research's own note says. Drawn without crosses, saints' plaques or double-headed eagles (the place's DO NOT DRAW list bans crosses; Block A bans eagles). |
| japan_earlymodern (Tokugawa Edo) | woman | Nagoya-obi (Accessory, the hips) | **kazuki veil** (Headwear, hides the top) | The look had no headwear, and its tamamusubi hair reads like Muromachi Kyoto's long tied-back hair from the front (its loop sits at the nape, out of view). Japanese women of 1610 wore no necklaces or earrings, and hairpins belong to later Edo (the DO NOT DRAW list). The kazuki, a patterned kosode draped over the head, is a big, dark, densely patterned shape unlike every other veil (all plain white, black or one colour). The Nagoya-obi stays drawn, in the outfit. | Women going out in the Momoyama and early Edo periods wore a kosode over the head as a veil (kazuki), as the genre screens the research already cites show; the Edo kazuki was worn with its collar pulled forward over the forehead (for example the Kyoto Prefectural Library and Archives, "The Costume of Edo-Period Japanese Women"). |
| china_modern (Beijing 1972) | woman | khaki satchel (Accessory, the hip) | **navy cap** (Headwear, hides the top) | The women's look had no headwear, and its clipped bob reads like Weimar Berlin's Bubikopf; the plain cloth peaked cap of the men's look was worn by men and women alike with the Zhongshan suit. Among the women's headwear it has no look-alike (the Weimar cloche is a low bell hat without a peak). The satchel stays drawn as the women's accessory (not leakable). The Culture value becomes one item for both genders: "navy cap". | The research's own sources (1970s documentary photographs of Beijing; the suit "worn by men and women alike") and "Dress in Communist China" (Fashion, Costume, and Culture: men and women wore the same garments, with cloth peaked caps). Plain, with no star or badge (DO NOT DRAW). |
| greece_modern (Metapolitefsi Athens) | man and woman | tagari bag (Accessory, the front of the hip) | **tagari bag**, drawn high: a short, broad striped strap across the chest and the bag against the left side of the chest, its top level with the armpit | No head or neck item of Athens 1975 passes the checks: the thick moustache and the shaggy cut look like other places' moustaches and cuts, the fisherman's cap is Beijing's and London's cap (the research dropped it for that), and worry beads are held. The tagari is the research's single Greek item for both genders, so it stays and moves up. | The research: the tagari was so emblematic of 1970s Athenian students that it named them. Worn higher than it usually was (open question 2 of v2.1). |
| china_ancient (Eastern Han Luoyang) | woman | bi-disc pendant (Accessory, mid-chest: borderline) | **bi-disc pendant**, raised to the upper chest (its centre about a hand below the collarbones) | It shows above the desk at mid-chest, but papers held up to read (piece 10) reach mid-chest, so it moves up to clear them. The label and the Culture value are unchanged ("jieze cap / bi-disc pendant"), so the game data does not change; Ban Zhao's description moves it up too. | Unchanged (it hangs on its own cord round the neck since the v2 review, finding 3). |

**The Culture values (Costume Guide rows) that change:** Ottoman Ioannina "kalpak / silver chest chains"; Tokugawa Edo "chasen-mage / kazuki veil"; Beijing, People's Republic "navy cap". Metapolitefsi Athens and Eastern Han Luoyang keep theirs.

**MUST READ lines.** 21 lines named or led with a feature below the desk (such as calcei boots, knee-tied trousers, hakama, the fustanella, Schnabelschuhe, leg bindings, knee- and ankle-length garments, skirts, the pafti and the Nagoya-obi, or a long garment's silhouette), or did not name the leak item that replaced one; each now names only what shows above the desk. The lower-body description stays in the outfit lines, because every layer is still drawn whole.
