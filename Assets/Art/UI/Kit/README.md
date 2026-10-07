# The cel UI kit

Label-free sprites for every Time Sorter UI element, in the 80s/90s anime cel style Saleh approved on 2026-10-07 (sheets 01-05, see `docs/UI_KIT.md`). Exported at 2x design size by the kit exporter (`E:\unity\NOPE-tools\art-gen\export_kit.py`, from the same code that drew the approved sheets).

- `kit_manifest.json`: every sprite's size, 9-slice border (L, B, R, T px), pivot, pixels per unit and intended use. `Assets/Editor/UiKitImporter.cs` applies it on import.
- Naming: `<component>_<variant>_<state>`; button states rest / hover / pressed / locked map to Selectable SpriteSwap normal / highlighted / pressed / disabled.
- Text is never baked (the Translation Lens and Settings language must reach every label), except `logo_time_sorter`.
- Fonts (OFL) in `Assets/Fonts/Kit`: BigShoulders Bold (labels, headings), Outfit (speech), GeistMono Bold (phosphor readouts), UnifrakturMaguntia (newspaper masthead).
