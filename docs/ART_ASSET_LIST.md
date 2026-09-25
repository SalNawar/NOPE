# Time Sorter: Art & UI Asset List for ChatGPT

Every visual the game needs, written so it can be pasted into ChatGPT section by section. Each item says what to draw, the size and format, and exactly where the file goes in `E:\unity\NOPE` so the game picks it up.

Based on the repo state on 23 Sep 2026 (ALPHA_ROADMAP.md, docs/FEATURES.md, the office scene spec, `OfficeSceneUIBuilder.cs`, and the content in `Assets/Data`).

## How to read this page

- **Tier 1: Drop-in.** The code already loads these files by path. Replace the PNG and the game uses it. No code changes.
- **Tier 2: Needs a hook.** The game currently draws these with flat colored Unity UI or has no slot for them yet. Generate them into the listed folder now; a small loader (`ArtLibrary`, loads by name from these paths) has to be added before they show up.
- Items marked ⚠️ need a decision before generating.

## Paste this first into ChatGPT (style brief)

```
You are producing 2D game art for "Time Sorter", a Papers, Please-style game.
The player is a clerk at a time-travel border desk who inspects travellers'
documents and accepts or denies their trip to another era.

Style rules for EVERY asset:
- 2D painted illustration, slightly muted palette, retro 1990s bureaucratic
  office mixed with light sci-fi (beige CRT tech, stamped paperwork, dim booth).
- Consistent light source from top-left.
- Props and characters: transparent PNG background, object fully inside the
  canvas, no drop shadow outside the object, no cropping.
- Backgrounds: opaque PNG.
- NEVER bake numbers or changing text into art. Where the game prints text
  (day number, money, percentages, names, fields), leave a clean blank area.
- Exact pixel sizes are given per asset. Keep the aspect ratio exactly.
- Deliver one file per asset, named exactly as specified.
```

Style anchors already in the repo (attach to ChatGPT as reference): `ArtDeliverables/TimeDesk/Concepts/office-direction-v1.png` and `desktop-direction-v1.png`.

---

# TIER 1: Drop-in assets (work automatically)

## Import rule for every Tier 1 file

1. Overwrite the existing PNG **with the same file name**. Do NOT delete the `.png.meta` next to it (it keeps the GUID, so every scene reference survives).
2. In Unity, select the file: Texture Type = Sprite (2D and UI), Sprite Mode = Single, Mesh Type = Full Rect, Pivot = Center.
3. **Pixels Per Unit** = 100 x (new width / placeholder width). World size comes from pixel size at PPU 100. Easiest: ask ChatGPT for exactly 4x the placeholder size and set PPU = **400** on all of them.

## Office booth: `Assets/Art/Office/Placeholder/`

1. **backwall.png**: placeholder 400x240, deliver **1600x960**, opaque. The wall behind the counter, seen from the clerk's seat: waiting hall of a time-travel customs office, rope barriers, departure board, faint era clocks. Keep the lower centre calm (the traveller stands there).
2. **partition.png**: placeholder 120x240, deliver **480x960**, transparent. One tall privacy-booth partition angled toward the viewer. The SAME file is used left and right, so make it symmetric or set Flip X on `RightPartition`.
3. **desk.png**: placeholder 400x90, deliver **1600x360**, transparent. Clerk's desk front edge, worn laminate, memorabilia only at the far edges (centre and right stay clear for the sign and CRT).
4. **traveller.png**: placeholder 60x110, deliver **240x440**, transparent. Default generic traveller at the counter, neutral pose.
5. **crt.png**: placeholder 150x130, deliver **600x520**, transparent. Beige 90s CRT on a swivel base, tube depth on the RIGHT, glass turned LEFT toward the player, dim screen glow. Clickable, so clear silhouette. **Art request (piece 7):** a front-facing CRT whose glass is a large, blank 4:3 rectangle (the game draws the live desktop in the glass's inscribed 4:3 rectangle), with a power button and an LED on its bezel.
6. **sign.png**: placeholder 96x50, deliver **384x200**, transparent. Standing desk placard "READY / NEXT" (this text may be baked). Clickable.
7. **calendar.png**: placeholder 80x100, deliver **320x400**, transparent. Wall tear-off calendar, "DAY" header strip, big BLANK middle for the day number.
8. **stabilitymonitor.png**: placeholder 110x80, deliver **440x320**, transparent. Timeline stability wall device. The game TINTS the whole sprite green/amber/red, so draw it in light greys and white. Blank display for the percentage.
9. **till.png**: placeholder 110x80, deliver **440x320**, transparent. Retro cash register / credits till with a blank display window.
10. **poster.png**: placeholder 80x110, deliver **320x440**. Default neutral agency poster ("Keep the Timeline Clean" style, no real logos).

## Desktop wallpaper: `Assets/Art/Generated/`

11. **xp_bliss.png**: placeholder 960x540, deliver **1920x1080**, opaque. Wallpaper of the in-game OS on the CRT. Calm landscape with a subtle sci-fi element (candidate exists: `ArtDeliverables/TimeDesk/Desktop/wallpaper-orbit-coast.png`). Keep top-left clear for icons.

## Timeline poster variants (semi-automatic)

The wall poster (`ReactivePoster`, component `TimelineReactiveSprite`) swaps sprite when a Visuals cue fires. Generate into `Assets/Art/Office/Placeholder/Posters/`, 320x440 each, then add each to the component's **Mappings** list once in the Inspector (cueId = file name without `poster_` and `.png`). No cue strings are authored in the Effect assets yet, so a `Cue` op must be added to each matching Effect.

- Dominant attribute per nation (15): `poster_dom_greece_democracy`, `poster_dom_greece_science`, `poster_dom_greece_art`, same three for `germany`, `japan`, `egypt`, `china`.
- Triggers (3): `poster_science_boom`, `poster_democracy_collapse`, `poster_art_renaissance`.
- ⚠️ The "NGermany" era must not use real Nazi-era symbols or insignia. Invented iconography only.

## Existing art in `ArtDeliverables/TimeDesk/`

These are **full 1672x941 scene layers**, not cropped props, so they will not line up if dropped in directly. Crop each to its object and resize to the Tier 1 size, or use them as ChatGPT reference. Mapping: 01-waiting-hall → backwall, 03-partition-frame → partition, 05-desk → desk, 06-crt → crt, 07-credits-till → till, 09-ready-sign → sign, 10-calendar → calendar, 11-stability-monitor → stabilitymonitor, Timeline/poster-neutral → poster, Characters/traveller-explorer (1024x1536) → traveller. Not used anywhere yet: 02-queue, 08-intercom, Desktop/icons-4x4, Desktop/window-frame.

---

# TIER 2: Assets that need a loader hook

## A. Office extras: `Assets/Art/Office/Booth/`

- **queue_bg.png** 1600x600 transparent: line of waiting silhouettes (toga, armour, robe, futuristic suit).
- **queue_silhouette_01..06.png** 200x400 transparent.
- **intercom.png** 320x240 transparent: desk intercom box (clicked to open the traveller wheel).
- **scanner_tray.png** 600x200 transparent: desk scanner: drop papers on its glass bed.
- **paper.png** 150x200 (in `Assets/Art/Office/Placeholder/`, piece 7): a blank paper sheet the desk's papers use; later one paper stock per document kind.
- **crt_power.png** 28x28 and **crt_led.png** 8x8 (in `Assets/Art/Office/Placeholder/`, piece 7): the CRT bezel's power button and LED (draw the LED white: the game tints it on and off).
- **desk_mug.png, desk_photo.png, desk_plant.png, desk_stamp.png** 200x200 transparent.
- **lighting_overlay_warm.png / _cold.png / _alarm.png** 1920x1080 transparent, low opacity mood tints.

## B. Fake OS desktop UI: `Assets/Art/UI/Desktop/`

Transparent PNG. Icons 128x128, readable at 48x48.

- icon_currency.png (Currency Ledger)
- icon_language.png (Tongues & Scripts)
- icon_technology.png (Index of Devices)
- icon_directives.png (sticky-note travel rules)
- icon_citizen_records.png
- icon_scanner.png (Deviation Report)
- icon_compare.png
- icon_internet.png
- icon_lexicon.png, icon_dialect.png, icon_material.png (upgrade-gated; code dims them)
- icon_cluelog.png, icon_notes.png
- icon_settings.png, icon_power.png

Window and shell chrome (9-slice friendly: flat stretchable middle, detail only at corners/edges):

- window_frame.png 256x256, window_titlebar.png 256x32
- btn_min / btn_max / btn_close in `_normal`, `_hover`, `_pressed` (24x24, 9 files)
- taskbar.png 256x40, start_button_normal / _pressed.png 120x40 ("Start" baked)
- start_menu_panel.png 256x384
- tray_bg.png 256x40, tray_day / tray_credits / tray_stability.png 24x24
- cursor_arrow.png, cursor_hand.png 32x32
- cursor_grab.png, cursor_grabbing.png 32x32 (over and while dragging a desk paper; piece 7 shows the hand for now)
- btn_back_to_office.png 200x48 ("< Office")

## C. Investigation UI: `Assets/Art/UI/Investigation/`

- **scanner_backing.png** 1024x1024 opaque dark scanner glass.
- **page_blank.png** 800x1100 white scanned page with paper grain.
- **photo_frame.png** 240x300.
- **claim_banner.png** 1200x96 (blank).
- **intercom_panel.png** 600x400, **intercom_button_normal/_hover.png** 400x64.
- **compare_bar_neutral / _match / _mismatch / _deviation.png** 1200x64 (grey, green, amber, red, no text).
- **btn_accept_normal/_hover/_pressed.png**, **btn_deny_normal/_hover/_pressed.png** 300x96, or stamps **stamp_approved.png**, **stamp_denied.png** 400x200.
- **refbook_page.png** 800x1100, **refbook_cover_currency / _language / _technology.png** 400x560.
- **citizen_records_frame.png** 1000x700, **agency_logo.png** 256x256 (invented seal).
- **deviation_report_form.png** 800x1100.
- **directives_sticky.png** 400x400.
- **citation_slip.png** 600x800.

### Per-nation document kits: `Assets/Art/UI/Documents/<Nation>/`

For each of **Aegyptus, Albion, Helios, Latia, Norvik, Solaris**:

- `<Nation>_passport_stock.png` 800x1100 (fields blank: Full Name, Date of Birth, Coin of Issue, Native Tongue)
- `<Nation>_permit_stock.png` 800x1100 (fields blank: Declared Device, Bond Currency)
- `<Nation>_seal.png` 256x256
- `<Nation>_border.png` 800x1100 transparent overlay

24 files. Era flavour: Rome (Aegyptus, Latia), Medieval (Albion, Norvik), Future (Helios, Solaris).

## D. Characters: `Assets/Art/Characters/`

### Visitors (investigation cast)

Nations: Aegyptus, Albion, Helios, Latia, Norvik, Solaris. Archetypes: Diplomat, Merchant, Mystic, Warrior. **24 visitors**, each:

- `Visitors/<Nation>/<Nation>_<Archetype>_body.png` 240x440 transparent
- `Visitors/<Nation>/<Nation>_<Archetype>_idle2.png` 240x440 (second idle frame)
- `Visitors/<Nation>/<Nation>_<Archetype>_portrait.png` 240x300 (passport photo, flat light background)

⚠️ Alternative: layered parts per nation (backdrop, body, head, hair, outfit, accessory) to mix faces. Pick one approach.

### Timeline cast (Phase 7 content)

Archetypes Artist, Diplomat, Merchant, Scientist, Soldier, Wanderer across Greece, N. Germany, Japan, Egypt, China. ⚠️ Decide whether these real-world nations stay or merge into the fictional six. If they stay: same body/idle2/portrait trio into `Visitors/<Nation>/`.

### Legendary visitors: `Assets/Art/Characters/Legendaries/`

Body (240x440) + portrait (240x300) each, more ornate, subtle gold rim. Names follow asset ids (e.g. `Legendary_Greece_Philosopher_body.png`).

- Greece: Calliope Demarch (orator), Theron the Stargazer (philosopher), Phidian Marcos (sculptor)
- N. Germany: Doctor Ilse Vahn (engineer), Colonel Reinhardt Voss (officer), Greta Brandt (painter). ⚠️ No real insignia.
- Japan: Renji Tachibana (illustrator), Hideo Kanzaki (inventor), Sachiko Imari (reformer)
- Egypt: Senmet Khare (architect), Amara Sett (painter), Nefru Tahan (vizier)
- China: Wen Zhirong (inventor), Magistrate Lian Bo (official), Mei Qiulan (painter)
- Wanderers: The Alchemist, The Iconoclast, The Idealist, The Muse, The Tyrant, The Visionary

21 legendaries, 42 files.

## E. Newsletters and day flow: `Assets/Art/UI/DayFlow/`

- **temporal_times_paper.png** 1200x1600, **temporal_times_masthead.png** 1200x240 ("THE TEMPORAL TIMES" baked)
- **shift_ledger_paper.png** 1200x1600, **shift_ledger_header.png** 1200x200 ("SHIFT LEDGER")
- **btn_start_shift.png**, **btn_go_home.png** 360x96, normal + hover
- **stamp_citation.png** 300x300

## F. Home scene: `Assets/Art/Home/`

- **home_bg.png** 1920x1080 opaque: cramped apartment at night, bills on the table, slot machine in the corner, bed doorway.
- **panel_expenses.png** 800x1000.
- **panel_shop.png** 800x1000 + 256x256 icons: **upgrade_advanced_scanner.png**, **upgrade_archive_access.png**, **upgrade_diplomatic_contacts.png**.
- **slot_machine.png** 800x1000, **slot_lever.png**, reel symbols 256x256: **slot_small_win.png**, **slot_jackpot.png**, **slot_busted.png**, **slot_forgery_warning.png**, **slot_legendary_omen.png**.
- **btn_sleep.png** 360x96.
- Family: ⚠️ confirm member count/roles from `RunConfig.startingFamilyMembers`. Per member `family_<role>_portrait.png` 256x256 plus condition variants ⚠️ (confirm condition names in `HomeEconomy`).

## G. Title and endings: `Assets/Art/Title/`

- **title_bg.png** 1920x1080 opaque.
- **logo_time_sorter.png** 1200x400 transparent.
- **btn_continue.png**, **btn_new_run.png** 400x100, normal + hover.
- **ending_panel.png** 1200x800.
- Endings 1600x900: **ending_fired.png**, **ending_bankrupt.png**, **ending_retirement.png**, **ending_scientific_age.png**, **ending_democracy_triumphant.png**, **ending_artistic_golden_age.png**.

## H. Shared icons: `Assets/Art/UI/Icons/`

256x256 transparent.

- Eras: **era_rome.png**, **era_medieval.png**, **era_future.png** (+ Greece, NGermany, Japan, Egypt, China if the timeline cast stays).
- Attributes: **attr_democracy.png**, **attr_science.png**, **attr_art.png**, **attr_industry.png**, **attr_militarism.png**, **attr_mysticism.png**, **attr_philosophy.png**.
- Nation emblems (reuse `<Nation>_seal.png` for the fictional six).
- HUD: **icon_money.png**, **icon_stability.png**, **icon_day.png**, **icon_warning.png**.

---

# Totals and suggested order

1. Tier 1 booth props + wallpaper: 11 files. Immediate payoff, zero code.
2. Desktop UI (B) + Investigation UI (C): about 60 files.
3. Visitors (D): 72 investigation files, 42 legendary files.
4. Day flow (E), Home (F), Title/endings (G), icons (H): about 50 files.
5. Timeline poster variants: 18 files.

# Hand-off checklist per batch

- File names match exactly.
- Sizes match exactly; transparent where stated.
- No numbers or changing text baked in.
- Tier 1: overwrite in place, keep `.meta`, set PPU.
- Tier 2: drop into the folder; unused until the `ArtLibrary` loader is added.
- Commit PNG **and** `.png.meta` together (PR #2 was rejected for missing metas).
