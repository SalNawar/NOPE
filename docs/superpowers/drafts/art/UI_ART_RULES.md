# Time Sorter: UI Art Rules (for ChatGPT)

*2026-09-24, revised the same day after the review of the art brief (poster size and import, flag colours, no "travel poster", sizes in every prompt). The piece-6 UI rules ("the PC reacts to history", its planned `docs/UI_ART_CONTRACT.md`) condensed for ChatGPT. The characters have their own brief: `CHARACTER_ART_BRIEF_v2.md`.*

## What this is for

Each night the game works out which country dominates history. From the next morning the office PC takes on that country's culture: the **desktop wallpaper**, the **booth poster**, the window and taskbar colours, the fonts, the colour and language of Accept and Deny. The colours, fonts and words are done in code. The art needed is:

- **8 culture wallpapers** (one per country), shown behind the desktop windows;
- **8 culture posters** (one per country), the poster on the booth wall;
- later, optionally: grey window-chrome pieces, and the Costume Guide book cover.

Before any country leads, the desk is "neutral": today's XP-style blue look with its existing wallpaper (`xp_bliss.png`). It needs no new art.

## The hard rules (every UI image)

1. **No baked text, ever.** No letters, numbers, words, captions, signage, logos, brand names, watermarks, signatures, clock digits, and no fake writing, calligraphy, or hieroglyphs or cuneiform arranged like writing. The language on screen changes with history (and a player can switch to English), so any word painted into art would be wrong for someone. Where a real object would carry writing (a sign, a board, a banner, a screen, a book cover, a label), leave it **blank**; the game prints the text.
2. **No flags or symbols of power.** No national flags or their colour stripes, coats of arms, eagles, rising-sun rays, the imperial chrysanthemum, five-claw dragons, stars on caps, party or regime emblems, military insignia, or any hate symbol. Never put a country's flag colours side by side as bands or stripes: Italy no green, white and red; Germany no black-white-red and no black-red-gold; Japan no red sun or red disc on a pale ground.
3. **No religious buildings, symbols or text.** No mosques, churches, temples, shrines or pagodas as a subject, no crosses, crescents, deities, holy writing or ritual objects.
4. **No real people, no real brands.** Crowds, if any, are tiny, faceless and civilian.
5. **Chrome is greyscale.** Anything that is part of the PC's interface (window frames, title bars, taskbar, buttons, icon backgrounds) is drawn in **neutral grey only**, from white to mid grey, with no colour at all: the game tints it with the culture's colours. Keep a flat middle and even borders so it can stretch (9-slice). No text or letter-shaped icons.
6. **Documents are not themed.** Passports, permits, reference books, Citizen Records and the passport photo frame keep their own look whatever culture leads. They are a separate brief; any field where the game prints a value stays empty.
7. **Style.** The same clean, simple look as the characters: flat colour areas, one soft shade tone, clean outlines, very little texture. Wallpapers may use soft gradients for the sky. Calm and low in contrast: windows and text sit on top.
8. **Lighting.** Soft, even daylight. No lens flare, no strong sun, no dramatic sunset or night scene (the office has its own lighting).

## Wallpapers

- **Ask ChatGPT for the wide size, 1536 x 1024.** Claude crops it to 16:9 (the middle 1536 x 864, so keep anything important away from the top and bottom 80 px) and scales it to the final **1920 x 1080** (the placeholder it replaces is 960 x 540; the desktop wallpaper is a stretched UI image, so only the 16:9 shape matters).
- **Composition:** like the classic "rolling hills and sky" desktop: a wide, calm view with a big sky and the subject far away. No single focal point (windows will cover the middle). Keep the **left fifth** quiet (the desktop icons sit there) and the **bottom strip** quiet (the taskbar).
- **Subject:** the country's Future place in 2150, seen at peace from a distance (table below). It is the present day of the player's world, shaped by that culture.
- **Colours:** built around the culture's wallpaper colour and palette below, so the themed windows sit well on it.

## Posters

- **Ask ChatGPT for the tall size, 1024 x 1536.** The game's poster is 8:11, so Claude trims 64 px from the top and bottom (keep the design inside the middle 1024 x 1408) and scales the result to the final **320 x 440 px**.
- **The final size is fixed by the booth.** The poster hangs on a back wall only 4 x 2.4 world units, at 0.8 x 1.1 units, like the existing reactive posters (`Assets/Art/Office/Placeholder/Posters/*`: 320 x 440 at 400 pixels per unit). The generated placeholder is 80 x 110 at 100 pixels per unit (also 0.8 x 1.1 units). So when Claude drops in the 320 x 440 art, it also sets **Pixels Per Unit to 400** in that poster's `.meta`. Dropped in at 1024 x 1408 with the placeholder's PPU of 100, the poster would be 10.24 x 14.08 units, about 13 times too big.
- **An illustrated wall print in a flat vintage-poster style, with no title area, no banner and no lettering of any kind:** bold flat shapes, 3 to 5 colours from the culture's palette, one big simple motif of the Future place, a plain border if you like. Readable when small (it hangs on the booth wall).

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

## Other UI art (later, optional)

- **Costume Guide cover** (`Assets/Art/UI/Investigation/refbook_cover_culture.png`, **400 x 560**, next to and like `refbook_cover_currency.png` and the other reference-book covers): a closed book cover with a simple garment motif (a hat and a collar), **no title**. The game shows the book's name. Ask ChatGPT for the tall size and Claude trims and scales it.
- **Window chrome** (taskbar, title bar, window frame, button): greyscale only, flat middle, even borders (rule 5). Not needed until the game adds theme slots for them.
- **Booth signs and screens** (READY and NEXT signs, departures board, banners, the monitor screen, the stability label): always drawn blank.

## Setting up ChatGPT for UI art

Use a separate Project, "Time Sorter UI Art", so the character rules don't mix in. Paste this into its instructions:

```
You are drawing UI art for "Time Sorter", a 2D game about a clerk at a time-travel border desk. The office computer's wallpaper and the booth poster change with whichever culture dominates history.

RULES FOR EVERY IMAGE
- Absolutely no text: no letters, numbers, words, captions, signage, logos, watermarks, signatures and no fake writing or calligraphy. Anything that would carry writing (signs, screens, book covers, banners) is left blank.
- No flags or flag colour stripes, coats of arms, eagles, rising-sun rays, a red sun disc, chrysanthemum crests, dragons, stars, party, regime or military symbols, and no hate symbols. Never put green, white and red, or black-red-gold, or black-white-red side by side as bands or stripes.
- No religious buildings, symbols or text. No real people, no real brands; any people are tiny, faceless civilians.
- Style: clean, simple 2D illustration with flat colour areas, one soft shade tone, clean outlines and very little texture. Soft gradients are fine for skies. Calm and low in contrast.
- Lighting: soft, even daylight. No lens flare, no dramatic sunset, no night scene.
- Use the colours I give you as the main palette.

WALLPAPERS: wide 1536 x 1024. A calm, distant view with a big sky and no single focal point. Keep the left fifth and the bottom strip quiet, and nothing important in the top and bottom 80 px.
POSTERS: tall 1024 x 1536. An illustrated wall print in a flat vintage-poster style, with no title area, no banner and no lettering of any kind: bold flat shapes, 3 to 5 colours, one big simple motif, everything important inside the middle 1024 x 1408.
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

```
POSTER for [country]: an illustrated wall print of [Future place], 2150, in a flat vintage-poster style, with no title area, no banner and no lettering of any kind.
One big motif: [pick one motif from the table].
Colours: [3 to 5 colours from the palette].
Avoid: [avoid list from the table].
Tall, 1024 x 1536.
```

## Delivery

- Save with ChatGPT's download button as PNG (never a screenshot). Name them `wallpaper_[country].png` and `poster_[country].png` and put them in `ArtDeliverables/TimeDesk/Culture/Raw/`.
- Claude crops and scales them and puts them at `Assets/Art/Culture/[country]/wallpaper.png` and `Assets/Art/Culture/[country]/poster.png`, replacing the game's text-free placeholder at the same path and keeping its `.meta` (so every sprite reference stays valid):
  - **wallpaper:** the middle 1536 x 864 of the ChatGPT image, scaled to **1920 x 1080**. Its `.meta` stays as it is (a stretched UI image).
  - **poster:** the middle 1024 x 1408, scaled to **320 x 440**, and in the same step the poster's `.meta` **Pixels Per Unit goes from 100 to 400**, so it keeps the placeholder's world size of 0.8 x 1.1 units. Claude checks the booth in Unity after the first poster.
- The ChatGPT originals stay in `ArtDeliverables/TimeDesk/Culture/Raw/` (a larger poster can be re-exported from them if the booth rework shows posters bigger).
- (Characters work differently: they are loaded by name at run time, see the character brief.)
- `coverage.json` (section `officeArt`) lists these 16 files with their final sizes and the poster's import setting.

## Checklist

- No text, letters or numbers anywhere, not even tiny or fake.
- No flags, crests, emblems or religious symbols.
- Wallpaper: calm, distant, quiet on the left and at the bottom, nothing important near the top or bottom edge.
- Poster: readable when small, design inside the middle 1024 x 1408, no title band, banner or lettering.
- No flag colours side by side as bands or stripes (Italy green-white-red; Germany black-white-red or black-red-gold; Japan a red disc on a pale ground).
- Every file came back at the size asked for (wide 1536 x 1024 or tall 1024 x 1536), not square.
- Colours match the culture's row in the table.
