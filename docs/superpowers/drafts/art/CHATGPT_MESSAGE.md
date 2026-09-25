# Starting the character art with ChatGPT

Four steps. Everything referred to here is in `CHARACTER_ART_BRIEF_v2.md` (the brief, v2.2: the ReStory style, for both kinds of character).

1. **Once: take a screenshot of your office.** In Unity, open the game's office (the art scene with the gameplay layer, as the game loads it), set the Game view to **1920 x 1080**, and save a screenshot of the office with **no traveller at the desk** (before you press READY) as `office_style_reference.png`. It is a style reference only: ChatGPT uses it to match the room's colour range and contrast, so the figures sit in the room. It never draws the room.
2. **Once:** create a ChatGPT Project called "Time Sorter Characters" and paste **Block A** (brief, section 5) into its instructions. Keep `character_guide_v2_1024x1536.png` and `office_style_reference.png` in a folder on your PC.
3. **Start a new chat in that Project**, attach `character_guide_v2_1024x1536.png` and `office_style_reference.png` with the paperclip, and send the message below.
4. When the man is done, send the woman prompt underneath it (same chat, both files attached again). Save both with the download button as `base_m_skin1_facea.png` and `base_f_skin1_facea.png`, then send them to Claude. Claude returns the two mannequins, and you continue with Batch 1, Step 2 (Periclean Athens) in a new chat.

## The message (copy everything in the box)

```
Hi! You'll be drawing all the character art for "Time Sorter", a game set in a 3D office: the player is a clerk at a time-travel border desk, checking the papers of travellers from 8 countries and 6 eras (Ancient to a Future in 2150). The travellers are flat 2D figures standing behind the desk in that office. I'll send you prompts from our art brief one at a time.

What we're making:
- Every traveller is built from stacked layers: body, head, outfit, facial hair, hair, headwear and accessory. Each layer is its own full 1024 x 1536 portrait image with the figure in exactly the same place, so the game can stack them.
- 40 places, each with a man's and a woman's look, plus the Future, plus 10 historical characters drawn whole later, in the same style, so everyone looks like one cast.
- A liar's disguise can "leak" one item from their real home, so every item must be clear and distinct on its own.
- In the game the desk hides every figure below the waist and the head shows small on screen, so the headwear, hair, face, collar, shoulders and upper chest must be bold and clear. Still draw every figure whole, down to the feet.

The rules (they're also in the Project instructions):
- Cute, soft 2D anime-style characters, like the customers of a cozy shop-counter game: expressive anime eyes (larger than realistic, with one simple highlight), a small, simple nose and mouth, clean rounded face shapes, and hair in clean stylised shapes and locks. Adults who look their age, never chibi.
- Keep the guide's head size and body proportions exactly: the style changes the face and the rendering, never the size of the head or the body. Faces stay individual, every skin tone stays exactly as given, and every culture gets this same style, never a caricature.
- Flat cel colours, each with one hard-edged shade tone (the same hue, a little darker), almost no texture, clean dark-brown outlines. No manga symbols (sweat drops, anger marks, blush lines).
- Neutral, even front lighting: no light direction, no glow, no shadows on the ground. The game places each flat figure in the office and tones it to the room's light.
- Flat pure green #00FF00 background. Never use bright green or lime (dark bottle or olive green is fine), magenta, pink, purple or violet in the clothing, hair or items, and paint every fabric opaque.
- No text, letters, numbers, logos, flags, insignia, uniforms, weapons, royal regalia or religious items. Civilian clothes only, respectful and never a caricature.
- Draw only what I ask for, exactly where the attached guide or mannequin puts the figure. One image per reply.

Attached: the Time Sorter figure guide, and office_style_reference.png, a screenshot of the game's office. The office screenshot is a STYLE REFERENCE ONLY: match its colour range and contrast so the figure sits in that room. Never draw the room, the desk, any furniture or the room's lighting: the background stays flat green.

First image.
Draw a BASE FIGURE: a man in his 20s, skin tone 1 (very light), bald (no hair at all, smooth scalp), ears visible, no makeup, neutral expression, looking at the viewer.
Face: soft, round cheeks and chin; large, round eyes; gently curved brows; a small, short nose.
Follow the guide's pose, proportions and landmark lines: the top of the head on the TOP OF HEAD line, the chin on the CHIN line, the soles of the feet on the bottom line, centred on the dashed centre line.
Clothing: only plain light-grey fitted shorts ending mid-thigh. Bare feet.
Remove all guide lines, labels and the grey silhouette. Flat pure green #00FF00 background.
Portrait, 1024 x 1536, the same framing as the attached guide.
```

## Then the woman (copy the box)

```
Attached: the Time Sorter figure guide and office_style_reference.png (STYLE REFERENCE ONLY: match its colour range and contrast, never draw the room).
Draw a BASE FIGURE: a woman in her 20s, skin tone 1 (very light), bald (no hair at all, smooth scalp), ears visible, no makeup, neutral expression, looking at the viewer.
Face: soft, round cheeks and chin; large, round eyes; gently curved brows; a small, short nose.
Follow the guide's pose, proportions and landmark lines: the top of the head on the TOP OF HEAD line, the chin on the CHIN line, the soles of the feet on the bottom line, centred on the dashed centre line.
Clothing: only a plain light-grey strapless bandeau covering only the bust, and plain light-grey fitted shorts ending mid-thigh. Bare feet.
Remove all guide lines, labels and the grey silhouette. Flat pure green #00FF00 background.
Portrait, 1024 x 1536, the same framing as the attached guide.
```

## Reminders

- If an image is wrong, press **Regenerate** or resend the same prompt. Never ask ChatGPT to tweak its own image: every edit redraws everything and the figure drifts.
- If the head comes out too big for the body (with the top of the head and the soles on their lines, the chin sits clearly below the CHIN line), press **Regenerate**: the head size is fixed, whatever the style.
- If an image shows any part of the office (a wall, the desk, a floor or the room's warm light), press Regenerate: the office screenshot is only a colour and contrast reference.
- Every prompt in the brief ends with a size line ("Portrait, 1024 x 1536, ..."). Keep it, or ChatGPT may answer with a square image.
- One new chat per place (per PAIR block); send the PAIR block first, with `office_style_reference.png` attached (prompt 9.0), and wait for READY. Send each place's files to Claude as soon as that place is done.
- Premade characters (Senenmut, Socrates and the others) are made in a second Project, "Time Sorter Premades", with its own instructions (Block P), because they are real people drawn whole in their real hair colours (brief, section 11). They use the same style as the travellers, so the two kinds look like one cast.
- Desktop wallpapers and the other UI art have their own rules and their own Project: `UI_ART_RULES.md`.
