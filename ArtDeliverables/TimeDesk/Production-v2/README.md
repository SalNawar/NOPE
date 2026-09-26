# Time Sorter — non-character art pack, 24 September 2026

Open gallery.html to browse the individual PNGs. manifest.json lists every planned asset and its dimensions; validation.json records file-size and transparency checks.

## Scope

291 planned PNG deliverables, including one booth composition reference. 264 assets also have editable SVG sources. Illustrated environments and physical props were created with the built-in image-generation tool; interface graphics, geometric emblems, document templates and typographic posters were drawn as native vector artwork and exported to PNG. generated-sources.json preserves the final image prompts and source paths; Sources/ preserves the selected full-resolution generated images.

Countries: Egypt, Greece, China, Germany, Japan, Britain, Iraq/Mesopotamia, Italy. Each has Ancient, Old, Modern and Future document kits: passport stock, permit stock, seal and border (128 files total). The country labels are agency navigation categories, not assertions that identical modern nation-states existed in antiquity. Emblems are original secular game graphics, not authentic historical government seals. Documents are time-transit agency templates, not replicas of historical passports.

Characters, portraits, family faces and all queue sprites are excluded. The booth conceals the waiting area behind frosted glass. The desk photograph contains a landscape. Mysticism/religious icons and the old fictional six-nation kits are superseded by the user's newer instructions.

## Main artwork

- Concepts/booth-master.png: composition reference, not a flattened gameplay replacement.
- Office/Booth/desk_deep.png: full-frame 1920x1080 tabletop layer. Preserve its full canvas registration.
- Office/Placeholder/backwall.png: opaque rear environment.
- Office/Placeholder/partition.png: single independent partition; mirror at layout time if needed.
- Physical props: separate alpha PNGs with blank runtime displays.
- Desktop/xp_bliss.png: original coastal OS wallpaper. Legacy filename retained for mapping.
- UI/: desktop chrome and button states, investigation forms, country/era document kits, shared icons.
- Office/Placeholder/Posters/: 24 country/attribute prints plus three event prints.
- Home/: apartment environment, expenses/shop UI, slot-machine body, separate lever and symbols.
- Title/: title environment, logo, menu controls, panel and six environmental ending illustrations.

The shallow 1600x360 Office/Placeholder/desk.png is a compatibility export. The deeper composition requires desk_deep.png and appropriate scene layout; the old shallow strip alone cannot reproduce it.

## Runtime text and UI

Identity values, dates, money, percentages, document claims and changing story text remain blank. Fixed labels such as READY, DAY, Start and the newsletter titles are baked where appropriate.

Window/frame interiors are transparent. Suggested nine-slice borders: window_frame 10 pixels on each side; titlebar/taskbar 8 left/right and 6 top/bottom; menu panel 12 left/right/bottom and 48 top. Final sprite borders should be checked against the consuming UI layout. Use center pivots, Full Rect, Sprite Single; use alpha for transparency and disable lossy compression for small text/UI.

SVG sources use Segoe UI text; PNGs have text already rasterized. Re-export requires that font to preserve exact typography.

Slot lever size was unspecified in the supplied list; this pack uses 200x600. Only the lever silhouette belongs on the lever sprite; the machine has no baked lever.

## Integration status

This is an art delivery in ArtDeliverables, not a Unity scene/code change. No existing Assets files, .meta GUIDs, scenes or game scripts were replaced.

Intended destination mapping:
- Office/... -> Assets/Art/Office/...
- UI/... -> Assets/Art/UI/...
- Home/... -> Assets/Art/Home/...
- Title/... -> Assets/Art/Title/...
- Desktop/xp_bliss.png -> Assets/Art/Generated/xp_bliss.png
- Concepts/... -> reference only, not a runtime loader path.

When replacing existing Tier 1 PNGs, preserve their .png.meta GUIDs. The original 4x office replacements use PPU 400. New country/era kits and Tier 2 graphics need matching loader references, as noted in the supplied asset list. Real object rotation and camera movement require scene/interaction support; PNGs provide fixed-view art.

## Validation

Dimensions and alpha are checked across the manifest; SVG sources are parsed as XML. Generated scenes and props have been visually reviewed, with representative native UI/document/poster checks. This does not substitute for checking scale, readability, hit areas and alignment inside the actual Unity scenes. Characters and case-specific runtime content are deliberately absent.

build_native_art.py and pack_art.py were removed on 2026-09-26 (their outputs are in Assets/Art and the SVG sources stay). build_native_art.py recreated the native artwork. pack_art.py copies source images, applies the crop/resize packaging requested in the original asset brief, updates validation.json and creates gallery.html. Run the packer after the native exporter if re-exporting both.

