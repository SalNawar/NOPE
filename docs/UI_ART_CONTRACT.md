# UI art contract (the PC reacts to history, piece 6)

How art reaches the office PC's themed UI, so final art can replace placeholders without code changes.

## Rules

- **Text-free.** No word, number or letter is ever painted into UI or wallpaper art: labels change language with the present culture and are drawn by the game (`world_source.json` `ui.strings` / `ui.languages`). A baked word would defeat the language switch.
- **Evidence is never skinned by the PC theme.** Documents, reference-book rows, Citizen Records rows, the interview transcript and the speech bubble are diegetic (`ThemeRoles.IsDiegetic`); the culture theme never recolours or refonts them. Skinning documents by the traveller's own place is a separate, later art task.
- **Chrome art is neutral.** The theme tints UI images by role (`ThemeTag`), so a chrome sprite drawn for the UI must be neutral greyscale (white where the theme colour should show); a coloured sprite would be double-tinted.

## Where culture art lives (one path convention)

| Art | Path | Size | Placeholder |
|---|---|---|---|
| Neutral desktop wallpaper | `Assets/Art/Generated/xp_bliss.png` | 960 × 540 | created by Generate World only when missing |
| Culture desktop wallpaper | `Assets/Art/Culture/{countryId}/wallpaper.png` (`egypt`, `iraq`, `greece`, `italy`, `china`, `japan`, `britain`, `germany`) | 960 × 540 | a text-free painted hill and sky in the culture's colours, created by Generate World only when missing |

- The path of each wallpaper is authored in `world_source.json` (`ui.neutral.wallpaper`, `countries[].culture.wallpaper`); Generate World writes it into the theme (`Assets/Data/World/Culture/Theme_{id}.asset`).
- **Replace a placeholder in place**: overwrite the PNG at the same path and keep its `.meta` (the theme's sprite reference stays valid; no rebuild needed). Generate World never overwrites an existing file; it keeps the import a single sprite with mipmaps (the live monitor shows it small).
- Booth props (the reactive poster and other booth art) are not themed yet; that waits for the move into the new office.
- Character art (piece 4) is the project's other convention: layers loaded by key at runtime, documented with the characters.
