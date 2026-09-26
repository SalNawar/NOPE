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

## By-name art slots (redesign phase 27)

The UI's Tier-2 images use the character art's convention: loaded by name at runtime, so a file that does not exist yet can be delivered with no code change and no rebuild.

- **Where.** `Assets/Art/UI/Resources/<area>/<name>.png` (`ArtSlots.AssetRoot`); the slot is the path under the Resources folder (`Office/speech_bubble`, `Desktop/icon_<app id>`, `Investigation/refbook_cover_<id>`, `Forms/paper_<form number>`, `Forms/paper_agency`, `Forms/photo_frame`, `Forms/agency_seal`, `Forms/stamp_accept`, `Forms/stamp_deny`, `DayFlow/temporal_times_paper`, `DayFlow/shift_ledger_paper`, `DayFlow/citation_slip`, `Home/upgrade_<id>`, `Home/family_<member>_<band>`, `Home/slot_machine`, `Home/slot_lever`, `Title/title_button`, `Title/title_button_hover`). `ArtSlots` names every slot from the game's ids; docs/ART_ASSET_LIST.md lists them.
- **Import.** `ArtSlotImporter` imports every texture there on every import: a single sprite, transparent, clamped, at most 2048 px; the desk papers' art (`Forms/`) with mipmaps; the speech bubble and the Title face 9-sliced at a quarter of their shorter side. Drop a PNG with the slot's exact name; no `.meta` needs editing.
- **Fallback.** A missing file keeps the slot's code-drawn look (`ArtSlots.First`: the first candidate found, else none), so the game never waits on art. A paper's face tries its form's own face, then the agency's plain face.
- **Tint.** Light art on a panel (the bubble, the newsletters' sheets, the citation slip) takes the panel's colour as its tint, and the theme's where the panel is themed, as the flat panel did: draw it light and nearly neutral. A picture (a cover, a portrait, the slot machine, the Title face) shows untinted. The desktop icons take the theme's `DesktopIcon` ink: greyscale.
- **The endings' pictures** are the office convention instead: `EndingSO.picture` references `Assets/Art/Title/ending_<id>.png`, replaced in place.
