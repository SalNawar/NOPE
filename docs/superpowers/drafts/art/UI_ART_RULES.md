# Time Sorter: UI Art Rules (for ChatGPT)

*2026-09-24, revised the same day after the review of the art brief (flag colours, no "travel poster", sizes in every prompt), and on 2026-09-25 for the 3D office (v2.1: 4:3 wallpapers, posters deferred, the 2D layers over the office, the 3D props the Blender side makes). The piece-6 UI rules ("the PC reacts to history", `docs/UI_ART_CONTRACT.md`) condensed for ChatGPT. The characters have their own brief: `CHARACTER_ART_BRIEF_v2.md`.*

## What this is for

"Time Sorter" is set in the art side's 3D office, made in Blender: a clerk's desk with a beige CRT computer, papers, a stamp and the travellers, who are flat 2D figures standing behind the desk. The UI is 2D and sits on top of that room: the PC's desktop, the PC close-up frame, the speech bubble, the papers' faces.

Each night the game works out which country dominates history. From the next morning the office PC's desktop takes on that country's culture: the **desktop wallpaper**, the window and taskbar colours, the fonts, the colour and language of Accept and Deny. The colours, fonts and words are done in code. The art needed from ChatGPT is:

- **8 culture wallpapers** (one per country), shown behind the desktop windows;
- later, optionally: the Costume Guide book cover, grey window-chrome pieces, and a fallback for the PC close-up frame (section "2D layers over the 3D office").

Before any country leads, the desk is "neutral": today's XP-style blue look with its existing wallpaper (`xp_bliss.png`, 1920 x 1080; the 4:3 desktop shows its middle 1440 x 1080). It needs no new art.

**Not from ChatGPT:** the office itself and its props (desk, PC, signs, scanner) are the art side's 3D models in Blender (section "3D office props"), and the PC close-up frame is best rendered from the Blender CRT.

## The hard rules (every UI image)

1. **No baked text, ever.** No letters, numbers, words, captions, signage, logos, brand names, watermarks, signatures, clock digits, and no fake writing, calligraphy, or hieroglyphs or cuneiform arranged like writing. The language on screen changes with history (and a player can switch to English), so any word painted into art would be wrong for someone. Where a real object would carry writing (a sign, a board, a banner, a screen, a book cover, a label, a brand plate), leave it **blank**; the game prints the text.
2. **No flags or symbols of power.** No national flags or their colour stripes, coats of arms, eagles, rising-sun rays, the imperial chrysanthemum, five-claw dragons, stars on caps, party or regime emblems, military insignia, or any hate symbol. Never put a country's flag colours side by side as bands or stripes: Italy no green, white and red; Germany no black-white-red and no black-red-gold; Japan no red sun or red disc on a pale ground.
3. **No religious buildings, symbols or text.** No mosques, churches, temples, shrines or pagodas as a subject, no crosses, crescents, deities, holy writing or ritual objects.
4. **No real people, no real brands.** Crowds, if any, are tiny, faceless and civilian.
5. **Chrome is greyscale.** Anything that is part of the PC's interface (window frames, title bars, taskbar, buttons, icon backgrounds) is drawn in **neutral grey only**, from white to mid grey, with no colour at all: the game tints it with the culture's colours. Keep a flat middle and even borders so it can stretch (9-slice). No text or letter-shaped icons.
6. **Documents are not themed.** Passports, permits, reference books, Citizen Records and the passport photo frame keep their own look whatever culture leads. Any field where the game prints a value stays empty.
7. **Style.** The same cel look as the characters and the office's cel-shaded desk: flat colour areas, each with one hard-edged shade tone, clean outlines, very little texture. Wallpapers may use soft gradients for the sky. Calm and low in contrast: windows and text sit on top.
8. **Lighting.** Soft, even daylight. No lens flare, no strong sun, no dramatic sunset or night scene (the office has its own lighting).

## Wallpapers (4:3)

- **Where they show.** The desktop is a 4:3 canvas of 1440 x 1080. The player sees it in the **PC close-up frame** (clicking the office PC opens a front-facing monitor frame left of centre, its glass 1120 x 840 at 1920 x 1080), and the same desktop is cloned, small, onto the **office CRT** in the room (a 1024 x 768 texture, letterboxed, about 175 x 130 px on a 1080p screen). The wallpaper is an image that **envelopes** the 4:3 desktop (the overflow is clipped), so a 16:9 wallpaper loses its sides.
- **Ask ChatGPT for the wide size, 1536 x 1024.** Claude crops the middle **1365 x 1024** (4:3; about 85 px come off each side, so keep anything important inside the middle) and scales it to the final **1440 x 1080**. Nothing is cut at the top or bottom.
- **Composition:** like the classic "rolling hills and sky" desktop: a wide, calm view with a big sky and the subject far away, and no single focal point (windows cover the middle). Keep quiet:
  - the **left fifth** (the desktop icons sit there, in two columns);
  - the **top strip**, the top eighth (a claim banner spans the top 12% while a traveller is at the desk);
  - the **bottom strip** (the taskbar runs along the bottom).
  In the ChatGPT image that is: nothing busy left of about x = 360, above about y = 135, or below about y = 985.
- **Calm enough to read small:** the office CRT shows the whole desktop at about an eighth of its size, so use a few broad, calm shapes and colour areas; fine detail turns to noise there.
- **Subject:** the country's Future place in 2150, seen at peace from a distance (table below). It is the present day of the player's world, shaped by that culture.
- **Colours:** built around the culture's wallpaper colour and palette below, so the themed windows sit well on it.
- **The placeholders.** The eight text-free placeholders at `Assets/Art/Culture/[country]/wallpaper.png` are 960 x 540 (16:9). Generate World never writes over an existing file, so they are not regenerated: until the art lands, the 4:3 desktop shows their middle (an eighth of the width is cut on each side), which is harmless for a sky-and-hills gradient. The final art replaces each file in place.

## Posters: later

The 3D office has no poster. Piece 6 deferred the culture posters to the office move (its S2 and F13), and the move added no poster frame, so posters are not in the ChatGPT prompts or the delivery list, and no `poster.png` exists. When the art side adds a poster frame to the office (with an anchor the game can find), the poster becomes a texture in that frame, sized by it; its rules (text-free, an illustrated wall print with no title area) will be written then.

## The eight cultures

| Country (file id) | Future place (2150) | Wallpaper colour | Palette (chrome / accent / paper / highlight) | Motifs to draw on | Avoid |
|---|---|---|---|---|---|
| Egypt (`egypt`) | Nile Arcology | `#C9A45C` | `#1C4E80` / `#C8962E` / `#F2E6C9` / `#E9C46A` | terraced white arcologies along a wide blue Nile, date palms, solar desalination towers far off, lotus-and-papyrus column shapes, turquoise and lapis bead-row bands | pyramids-and-sphinx cliches, gods, cobras, crowns |
| Iraq (`iraq`) | Baghdad Garden City | `#2E6F95` | `#1B4F72` / `#D4A017` / `#EFE6D2` / `#F0C75E` | the Tigris lined with vertical date-palm farms, circular Round-City gardens, glazed lapis-blue and ochre brick patterns, flowing curved modern buildings | religious buildings or text, deity motifs, winged bulls |
| Greece (`greece`) | Aegean Commonwealth | `#6FA8DC` | `#0D5EAF` / `#1F6F8B` / `#FAFAF7` / `#FFE08A` | white island terraces over a deep blue sea, wave-power buoys as small floats, olive trees, meander (Greek key) borders | the national flag's blue-and-white stripes, temples as a subject |
| Italy (`italy`) | Mediterranean Union Rome | `#C98B5A` | `#7A2E1F` / `#2E6B3F` / `#F5EBDD` / `#F4D58D` | terracotta rooftops and arcades, umbrella pines, printed-stone vaulted halls, Vitruvian circle-and-square geometry | the tricolour: never green, white and red side by side as bands or stripes; churches and domes as a subject |
| China (`china`) | Shanghai Megacity | `#8E1B1B` | `#A31F1F` / `#D4AF37` / `#FBF3E4` / `#F5D76E` | a river bend with a far skyline of layered towers, a thin maglev arc, misty ink-painting hills, cloud-scroll and key-fret borders | dragons, stars, flags, political imagery, pagodas |
| Japan (`japan`) | Neo-Tokyo Bay | `#E9DFC8` | `#1F2F4A` / `#B23A1A` / `#F7F3EA` / `#F3D9A0` | a calm bay and far skyline, one soft mountain silhouette, wave (seigaiha) and hemp-leaf patterns, indigo and washi-cream | rising-sun rays, a red sun or any red disc, the chrysanthemum, shrines and torii |
| Britain (`britain`) | Thames Barrier London | `#5B7F95` | `#1F2A44` / `#00563F` / `#F4EFE3` / `#EED9A0` | the Thames with the barrier's curved steel shells, green parkland, a soft grey-blue sky, tweed-check and paisley border bands | the Union Jack, crowns, red phone-box and bus cliches |
| Germany (`germany`) | Rhine-Ruhr Metropole | `#9EA3A8` | `#2B2B2B` / `#E0B000` / `#F2F2F0` / `#FFE066` | the Rhine between vineyard hills, Bauhaus-geometric buildings, a hydrogen hyperloop line, circle-square-triangle shapes in primary colours on graphite | eagles, crosses, black-white-red or black-red-gold side by side as bands or stripes (the primary-colour shapes stay separate, on graphite), blackletter |

The hex colours are the game's own theme colours (piece 6), so the art matches the windows drawn over it.

## 2D layers over the 3D office

The office is 3D; these are the flat layers the game draws over it. Sizes are in reference pixels of the 1920 x 1080 overlay canvas, which scales with the screen (never below 1920 x 1080 in either dimension).

### The PC close-up frame

Clicking the office PC opens a front-facing monitor frame on the overlay, left of centre, with the room visible on its right (like ReStory's close-up). The game draws the live desktop into its glass. The pieces (the builder's `BuildPcFrame`, the `PcFrame` component):

| Piece | File (replaced in place, keep its `.meta`) | Size and place in the frame |
|---|---|---|
| Bezel | `Assets/Art/Office/Placeholder/pc_frame.png` (placeholder 620 x 530) | the frame is **1240 x 1060**, 20 px from the screen's left edge, centred vertically. The **glass is a transparent 4:3 hole** at x 60 to 1180, y 60 to 900 from the frame's top-left (1120 x 840): a 60 px bezel at the left, right and top, and a 160 px chin below. Transparent outside the rounded outer corners too. |
| Close X | `pc_close.png` (placeholder 48 x 48) | 64 x 64, its top-right corner 14 px in from the frame's top-right corner (a red button with a white cross, like the placeholder). |
| Power button | `crt_power.png` (placeholder 28 x 28) | 64 x 64, its bottom-right corner 70 px from the frame's right edge and 46 px up from its bottom. |
| Power LED | `crt_led.png` (8 x 8) | 18 x 18, centred 170 px from the right edge and 78 px up from the bottom: a plain white disc the game tints on and off. It stays as it is. |
| Brand plate | none (the game prints it) | the game prints "CHRONODESK 2150" in a 496 x 60 area centred 78 px up from the bottom (x 372 to 868 of the frame): keep that part of the chin **blank**, a plain plate at most. |

- **Resolution.** The frame's image has a fixed size on screen, so any resolution with the frame's aspect (1240 : 1060) works. Deliver the bezel at **1860 x 1590** (1.5x: crisp up to a 1620-line screen and under the 2048 import cap), with the glass hole at x 90 to 1770, y 90 to 1350; the close X and the power button at 96 x 96.
- **Render it from the Blender CRT (recommended).** The office CRT is the art side's Blender model (`ImportedOfficeDress/Desk/Retro CRT` in the art scene). A front orthographic render of it, square to the screen, with a transparent background and the screen cut out, gives a frame that matches the monitor in the room exactly. Fit it so the CRT's glass fills the 1120 x 840 hole; if the model's proportions differ from the frame's (its chin, its side bezels), tell Claude, and the builder's frame constants follow the art rather than the art being stretched. Light it softly and evenly from the front (the frame sits in front of the room, not in it).
- **ChatGPT is only the fallback.** If no render is possible: "PC FRAME: a front view of a beige 1980s CRT computer monitor bezel, square to the viewer, filling the image height, with rounded corners, a slightly darker chin, a blank brand plate and no buttons drawn (the game adds the close button, the power button and the LED). The screen area is one flat pure green #00FF00 rectangle in 4:3 proportion. No text, logos or letters. Flat, even light. Wide, 1536 x 1024." Claude crops the middle 1198 x 1024, fits the green screen onto the glass hole, cuts it out and scales the result to 1860 x 1590.

### The speech bubble

The traveller's lines appear in a bubble above their head (its centre 290 px above the shoulders, clear of the wheel's top item). Today it is a plain panel, 420 x 110, cream (#FAF7ED at 97%), with dark text inset 4% from the sides and 8% from the top and bottom; it looks the same in every culture (piece 6's diegetic roles).

- **The art: a 9-slice bubble body.** A rounded rectangle drawn **white or very light grey** (the game tints it cream), a flat middle, even edges, a soft 2 to 3 px dark outline, no shadow and no text. Master 128 x 128 with the rounded corners inside the outer 32 px on each side (the 9-slice borders), so it stretches to any size; at 420 x 110 the corners must stay within 16 px, where the text's inset begins.
- **The tail (optional)** is a separate small sprite, about 32 x 24, pointing down, which the game places under the bubble's bottom centre (a tail cannot sit inside a 9-slice that stretches).
- **Not wired yet:** the panel has no sprite today, so Claude adds the sprite (and the tail) to the builder when the art arrives. Put the files in `ArtDeliverables/TimeDesk/UI/Raw/` as `speech_bubble.png` and `speech_bubble_tail.png`. Ask ChatGPT for the square 1024 x 1024 and Claude scales it.

### The document paper faces

The papers the traveller hands over lie on the desk as lit quads 0.26 x 0.34 m (the paper's aspect is 0.765), and from piece 10 (desk examination, being built now) a click lifts a paper up close to read. **The faces are text-free: the game prints every field** (the title, every row's label and value, in the traveller's script where piece 9 says so) **and places the photo** in its frame, all laid out by code (piece 10's `PaperFace`: a title band, rows of a label over a value, the photo at the top right). So a face carries no labels, lines, boxes or blanks where the rows go (they move with the layout): a paper tone, a printed border, perhaps a guilloche band behind the title, all without words, numbers, emblems or seals.

- **Resolution.** A held paper is 0.44 screen heights tall in the office (475 px at 1080p, 317 px at 720p) and up to 0.62 beside the PC frame (670 px at 1080p, 893 px at 1440p, 1339 px at 2160p). So a face needs at least **784 x 1024** (one texel per pixel up to 1440p); **1024 x 1339**, ChatGPT's tall 1024 x 1536 cropped to the paper's aspect, covers 4K.
- **Today** every document uses one placeholder texture, `Assets/Art/Office/Placeholder/paper.png` (150 x 200, cream). **The documents' own brief follows piece 10**, which fixes the face layout and whether each document (passport, permit, the others) gets its own face.

## 3D office props (the Blender side, not ChatGPT)

The room and its props are the art side's models. The game finds them through the named-anchor contract (`docs/SCENE_CONTRACT_GAMEPLAY.md`) and never edits the art scene.

- **The scanner.** Until the art has one, the gameplay layer shows a stand-in flatbed (0.40 x 0.32 m) at the default pose (1.08, 1.06, -0.33). An art scanner needs:
  - an empty `Anchor_Scanner` (under the `GameplayAnchors` root) with the scanner model's renderers under it; when the anchor has renderers, the stand-in hides;
  - its footprint becomes the **drop area**: the world-aligned bounds of every active renderer under the anchor, so keep the model square to the desk and keep cables and stands out of it (they would enlarge the area), about 0.40 x 0.32 m like the stand-in (a paper is 0.26 x 0.34 m);
  - a **flat top**: the top of those bounds (plus 2 mm) is where a scanned paper lies, so a lid is closed flat or left off (a raised lid would float the paper at its height);
  - a spot on the desk the office camera sees, clear of the papers held up to read (they cover the lower middle of the screen); the default spot is right of the mat.
  The game adds the rest: the click box, the hover outline on the model, the scan reaction and the hint above the bed.
- **The office's own signs and screens** (the NEXT sign, the departures board, the day, stability, credits and shift-clock readouts) are the art side's 3D models. The no-baked-text rule still holds: their faces stay blank, and the game writes the NEXT caption and the readouts into the art's own text objects, found by name (`NextLabel`, `DayNumber`, `StabilityPercent`, `CreditsNumber`, `ShiftClockDisplay`).

## Other UI art (later, optional)

- **Costume Guide cover** (`Assets/Art/UI/Investigation/refbook_cover_culture.png`, **400 x 560**, next to and like `refbook_cover_currency.png` and the other reference-book covers): a closed book cover with a simple garment motif (a hat and a collar), **no title**. The game shows the book's name. Ask ChatGPT for the tall size and Claude trims and scales it.
- **Window chrome** (taskbar, title bar, window frame, button): greyscale only, flat middle, even borders (rule 5). Not needed until the game adds theme slots for them.
- **Traveller wheel icons** (piece 8; unchanged; `Assets/Art/UI/Resources/WheelIcons/wheel_<kind>.png`, **64 x 64**, imported as Sprite (2D and UI); the game loads them by name, so no code change): one per kind of wheel choice, shown at 28 px at the left of each choice on a dark blue button. `wheel_request` (a sheet of paper or an open hand: the desk asks for something), `wheel_question` (a speech balloon), `wheel_look` (an eye), `wheel_dialog` (two speech balloons), `wheel_back` (an arrow pointing left), `wheel_normal` (a small dot: a reply inside a conversation). Bold, simple silhouettes, **white on a transparent background** (the game may tint them, rule 5), readable when tiny. No letters and no character shapes: no question mark, no exclamation mark. Until they exist the game draws white placeholder glyphs of the same shapes at runtime. Ask ChatGPT for a square image and Claude scales it.

## Setting up ChatGPT for UI art

Use a separate Project, "Time Sorter UI Art", so the character rules don't mix in. Paste this into its instructions:

```
You are drawing UI art for "Time Sorter", a game set in a 3D office: the player is a clerk at a time-travel border desk, and the desktop of the office computer changes with whichever culture dominates history. You draw the flat 2D pictures that the computer's desktop and the game's interface show.

RULES FOR EVERY IMAGE
- Absolutely no text: no letters, numbers, words, captions, signage, logos, watermarks, signatures and no fake writing or calligraphy. Anything that would carry writing (signs, screens, book covers, banners, brand plates) is left blank.
- No flags or flag colour stripes, coats of arms, eagles, rising-sun rays, a red sun disc, chrysanthemum crests, dragons, stars, party, regime or military symbols, and no hate symbols. Never put green, white and red, or black-red-gold, or black-white-red side by side as bands or stripes.
- No religious buildings, symbols or text. No real people, no real brands; any people are tiny, faceless civilians.
- Style: clean 2D cel-style illustration with flat colour areas, each with one hard-edged shade tone, clean outlines and very little texture. Soft gradients are fine for skies. Calm and low in contrast.
- Lighting: soft, even daylight. No lens flare, no dramatic sunset, no night scene.
- Use the colours I give you as the main palette.

WALLPAPERS: wide 1536 x 1024. A calm, distant view with a big sky and no single focal point. The picture is cropped to 4:3 (a strip at the left and right edges is cut), so keep everything important inside the middle. Keep the left fifth, the top eighth and the bottom strip quiet. Use a few broad, calm shapes: the picture is also shown very small.
One image per reply.
```

Then, per culture (new chat each):

```
WALLPAPER for [country]: [Future place], the year 2150, seen at peace from a distance.
Draw on: [motifs from the table].
Main colours: [wallpaper colour] with [palette].
Avoid: [avoid list from the table].
Wide, 1536 x 1024.
```

## Delivery

- Save with ChatGPT's download button as PNG (never a screenshot). Name them `wallpaper_[country].png` and put them in `ArtDeliverables/TimeDesk/Culture/Raw/`.
- Claude crops the **middle 1365 x 1024** of each, scales it to **1440 x 1080** and puts it at `Assets/Art/Culture/[country]/wallpaper.png`, replacing the game's text-free placeholder at the same path and **keeping its `.meta`** (Sprite, max size 2048, mipmaps on). That stays valid: the wallpaper's fitter takes the image's own aspect, and pixels per unit do not matter for it. Claude then checks each wallpaper in the game, in the art office: in the open PC frame and on the office CRT.
- The ChatGPT originals stay in `ArtDeliverables/TimeDesk/Culture/Raw/`.
- The PC frame, the speech bubble and the paper faces are delivered as their sections say; the scanner is the art side's.
- (Characters work differently: they are loaded by name at run time, see the character brief.)
- `coverage.json` (section `officeArt`) lists the 8 wallpapers with their final size and import, marks the posters deferred, and names the 2D layers and the scanner.

## Checklist

- No text, letters or numbers anywhere, not even tiny or fake.
- No flags, crests, emblems or religious symbols.
- Wallpaper: calm and distant; everything important inside the middle (the left and right edges are cut); quiet on the left, at the top and at the bottom; broad shapes that still read very small.
- No flag colours side by side as bands or stripes (Italy green-white-red; Germany black-white-red or black-red-gold; Japan a red disc on a pale ground).
- Every file came back at the size asked for (wide 1536 x 1024 for a wallpaper), not square.
- Colours match the culture's row in the table.
