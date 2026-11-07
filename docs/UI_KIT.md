# UI kit: the cel look (approved 2026-10-07)

Saleh's direction: every screen in one 80s/90s anime cel family at ReStory-level finish. That means:

- brown-purple ink outlines;
- hard two-tone cel shading with purple-tinted shadows;
- an airbrushed sheen;
- a star glint on hover;
- screentone dots on locked or disabled elements;
- condensed BigShoulders labels;
- an optional small katakana line under primary labels;
- the hall's palette: oxblood #8A2F3B, slate #4A5872, signal red #C23A2E, brass #D4A055, manila #E2C994, bone #EEE5D0, lavender #9886A8, ink #2B1C24, card #FAF2E1.

## Reference sheets (`docs/ui-kit/`)

| Sheet | Contents |
|---|---|
| `sheet01-controls.png` | primary and secondary plates, printed card, pull tabs with keycaps, inspect button, folder tabs, icon keys |
| `sheet02-pc.png` | CHRONODESK OS: desktop icons, windows, list rows, menu bar and drop-down, start menu, taskbar, fields, toggles, chips, toast, tooltip, readouts |
| `sheet03-upgrades.png` | the Orders and House upgrade card in 4 states, links, actions |
| `sheet04-home.png` | title, pet adoption, bills, pet corner, slots, sleep, endings |
| `sheet05-desk.png` | dialogue wheel, speech bubble, tutorial, counter strip, citation slip, verdict ribbons, compare tags, pager |

Also in `docs/ui-kit/`:
- `mock_office.png`, `mock_briefing.png`: the kit in the game;
- `newspaper.png`: the morning paper (one key story, one titled story, the rest dummy type);
- `helix-river-states.png`: the stability display (no number).

## Rules
- Sprites live in `Assets/Art/UI/Kit` and are label-free. Text stays live TMP so the Translation Lens and language settings keep working. `kit_manifest.json` plus `Assets/Editor/UiKitImporter.cs` set each sprite's 9-slice border.
- Button states rest / hover / pressed / locked map to SpriteSwap normal / highlighted / pressed / disabled.
- Stability is never shown to the player as a number; the Helix River shows it.
- New pieces are rendered with the kit exporter (`E:\unity\NOPE-tools\art-gen\`) in the same style. They are never drawn ad hoc.

## The PC's cyberpunk look (D2, 2026-11-07)

Saleh dropped the XP direction for the PC: "I'm going full cyberpunk futristic 80s retro scifi". The PC keeps this kit's sprite names, fonts and type scale, and wears another look over them at run time:

- One theme asset, `PcThemeSO` (`Assets/Data/UI/PcTheme_Cyberpunk.asset`, named by `DesktopConfigSO.theme`). `PcTheme_Cel.asset` keeps this kit's look.
- Tokens: glass in near-black blue, phosphor cyan (the primary), magenta (the accent, tinted by the leading culture), amber (warnings and money), the alert red, light ink.
- The chrome is drawn in code from the tokens (`PcKit`, `PcChromeArt`): each piece keeps its kit sprite's name, pad, 9-slice border and size. The kit's colour words map to tones (`PcSkinRules`): oxblood to the accent, red to the alert, green to phosphor, brass to amber, bone, slate and lavender to the quiet edge.
- The glyphs, tiles, the power glyph and the skyline wallpaper are PNGs in `Assets/Art/UI/PcCyber`, made with PIL (`E:/unity/NOPE-tools/art-gen/pc_cyber/glyphs.py`, `wallpaper.py`): white strokes the game tints.
- The office, the desk, Home and the Title keep this kit's cel look.
