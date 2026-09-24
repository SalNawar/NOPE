# Time Sorter: Art & UI Asset List for ChatGPT

Every visual the game needs, written so it can be pasted into ChatGPT section by section. Each item says what to draw, the size and format, and exactly where the file goes in `E:\unity\NOPE` so the game picks it up.

Based on the repo state on 23 Sep 2026 (ALPHA\_ROADMAP.md, docs/FEATURES.md, the office scene spec, `OfficeSceneUIBuilder.cs`, and the content in `Assets/Data`).

## How to read this page

- **Tier 1: Drop-in.** The code already loads these files by path. Replace the PNG and the game uses it. No code changes.  
- **Tier 2: Needs a hook.** The game currently draws these with flat colored Unity UI or has no slot for them yet. Generate them into the listed folder now; a small loader (`ArtLibrary`, loads by name from these paths) has to be added before they show up.  
- Items marked ⚠️ need a decision before generating.

## Paste this first into ChatGPT (style brief)

You are producing 2D game art for "Time Sorter", a Papers, Please-style game.

The player is a clerk at a time-travel border desk who inspects travellers'

documents and accepts or denies their trip to another era.

Style rules for EVERY asset:

\- 2D painted illustration, slightly muted palette, retro 1990s bureaucratic

  office mixed with light sci-fi (beige CRT tech, stamped paperwork, dim booth).

\- Consistent light source from top-left.

\- Props and characters: transparent PNG background, object fully inside the

  canvas, no drop shadow outside the object, no cropping.

\- Backgrounds: opaque PNG.

\- NEVER bake numbers or changing text into art. Where the game prints text

  (day number, money, percentages, names, fields), leave a clean blank area.

\- Exact pixel sizes are given per asset. Keep the aspect ratio exactly.

\- Deliver one file per asset, named exactly as specified.

Style anchors already in the repo (attach to ChatGPT as reference): `ArtDeliverables/TimeDesk/Concepts/office-direction-v1.png` and `desktop-direction-v1.png`.

---

# TIER 1: Drop-in assets (work automatically)

## Import rule for every Tier 1 file

1. Overwrite the existing PNG **with the same file name**. Do NOT delete the `.png.meta` next to it (it keeps the GUID, so every scene reference survives).  
2. In Unity, select the file: Texture Type \= Sprite (2D and UI), Sprite Mode \= Single, Mesh Type \= Full Rect, Pivot \= Center.  
3. **Pixels Per Unit** \= 100 x (new width / placeholder width). World size comes from pixel size at PPU 100\. Easiest: ask ChatGPT for exactly 4x the placeholder size and set PPU \= **400** on all of them.

## Office booth: `Assets/Art/Office/Placeholder/`

1. **backwall.png**: placeholder 400x240, deliver **1600x960**, opaque. The wall behind the counter, seen from the clerk's seat: waiting hall of a time-travel customs office, rope barriers, departure board, faint era clocks. Keep the lower centre calm (the traveller stands there).  
2. **partition.png**: placeholder 120x240, deliver **480x960**, transparent. One tall privacy-booth partition angled toward the viewer. The SAME file is used left and right, so make it symmetric or set Flip X on `RightPartition`.  
3. **desk.png**: placeholder 400x90, deliver **1600x360**, transparent. Clerk's desk front edge, worn laminate, memorabilia only at the far edges (centre and right stay clear for the sign and CRT).  
4. **traveller.png**: placeholder 60x110, deliver **240x440**, transparent. Default generic traveller at the counter, neutral pose.  
5. **crt.png**: placeholder 150x130, deliver **600x520**, transparent. Beige 90s CRT on a swivel base, tube depth on the RIGHT, glass turned LEFT toward the player, dim screen glow. Clickable, so clear silhouette.  
6. **sign.png**: placeholder 96x50, deliver **384x200**, transparent. Standing desk placard "READY / NEXT" (this text may be baked). Clickable.  
7. **calendar.png**: placeholder 80x100, deliver **320x400**, transparent. Wall tear-off calendar, "DAY" header strip, big BLANK middle for the day number.  
8. **stabilitymonitor.png**: placeholder 110x80, deliver **440x320**, transparent. Timeline stability wall device. The game TINTS the whole sprite green/amber/red, so draw it in light greys and white. Blank display for the percentage.  
9. **till.png**: placeholder 110x80, deliver **440x320**, transparent. Retro cash register / credits till with a blank display window.  
10. **poster.png**: placeholder 80x110, deliver **320x440**. Default neutral agency poster ("Keep the Timeline Clean" style, no real logos).

## Desktop wallpaper: `Assets/Art/Generated/`

11. **xp\_bliss.png**: placeholder 960x540, deliver **1920x1080**, opaque. Wallpaper of the in-game OS on the CRT. Calm landscape with a subtle sci-fi element (candidate exists: `ArtDeliverables/TimeDesk/Desktop/wallpaper-orbit-coast.png`). Keep top-left clear for icons.

## Timeline poster variants (semi-automatic)

The wall poster (`ReactivePoster`, component `TimelineReactiveSprite`) swaps sprite when a Visuals cue fires. Generate into `Assets/Art/Office/Placeholder/Posters/`, 320x440 each, then add each to the component's **Mappings** list once in the Inspector (cueId \= file name without `poster_` and `.png`). No cue strings are authored in the Effect assets yet, so a `Cue` op must be added to each matching Effect.

- Dominant attribute per nation (15): `poster_dom_greece_democracy`, `poster_dom_greece_science`, `poster_dom_greece_art`, same three for `germany`, `japan`, `egypt`, `china`.  
- Triggers (3): `poster_science_boom`, `poster_democracy_collapse`, `poster_art_renaissance`.  
- ⚠️ The "NGermany" era must not use real Nazi-era symbols or insignia. Invented iconography only.

## Existing art in `ArtDeliverables/TimeDesk/`

These are **full 1672x941 scene layers**, not cropped props, so they will not line up if dropped in directly. Crop each to its object and resize to the Tier 1 size, or use them as ChatGPT reference. Mapping: 01-waiting-hall → backwall, 03-partition-frame → partition, 05-desk → desk, 06-crt → crt, 07-credits-till → till, 09-ready-sign → sign, 10-calendar → calendar, 11-stability-monitor → stabilitymonitor, Timeline/poster-neutral → poster, Characters/traveller-explorer (1024x1536) → traveller. Not used anywhere yet: 02-queue, 08-intercom, Desktop/icons-4x4, Desktop/window-frame.

---

# TIER 2: Assets that need a loader hook

## A. Office extras: `Assets/Art/Office/Booth/`

- **queue\_bg.png** 1600x600 transparent: line of waiting silhouettes (toga, armour, robe, futuristic suit).  
- **queue\_silhouette\_01..06.png** 200x400 transparent.  
- **intercom.png** 320x240 transparent: desk intercom box (clicked to request documents).  
- **scanner\_tray.png** 600x200 transparent: document slot on the desk.  
- **desk\_mug.png, desk\_photo.png, desk\_plant.png, desk\_stamp.png** 200x200 transparent.  
- **lighting\_overlay\_warm.png / \_cold.png / \_alarm.png** 1920x1080 transparent, low opacity mood tints.

## B. Fake OS desktop UI: `Assets/Art/UI/Desktop/`

Transparent PNG. Icons 128x128, readable at 48x48.

- icon\_currency.png (Currency Ledger)  
- icon\_language.png (Tongues & Scripts)  
- icon\_technology.png (Index of Devices)  
- icon\_directives.png (sticky-note travel rules)  
- icon\_citizen\_records.png  
- icon\_scanner.png (Deviation Report)  
- icon\_compare.png  
- icon\_internet.png  
- icon\_lexicon.png, icon\_dialect.png, icon\_material.png (upgrade-gated; code dims them)  
- icon\_cluelog.png, icon\_notes.png  
- icon\_settings.png, icon\_power.png

Window and shell chrome (9-slice friendly: flat stretchable middle, detail only at corners/edges):

- window\_frame.png 256x256, window\_titlebar.png 256x32  
- btn\_min / btn\_max / btn\_close in `_normal`, `_hover`, `_pressed` (24x24, 9 files)  
- taskbar.png 256x40, start\_button\_normal / \_pressed.png 120x40 ("Start" baked)  
- start\_menu\_panel.png 256x384  
- tray\_bg.png 256x40, tray\_day / tray\_credits / tray\_stability.png 24x24  
- cursor\_arrow.png, cursor\_hand.png 32x32  
- btn\_back\_to\_office.png 200x48 ("\< Office")

## C. Investigation UI: `Assets/Art/UI/Investigation/`

- **scanner\_backing.png** 1024x1024 opaque dark scanner glass.  
- **page\_blank.png** 800x1100 white scanned page with paper grain.  
- **photo\_frame.png** 240x300.  
- **claim\_banner.png** 1200x96 (blank).  
- **intercom\_panel.png** 600x400, **intercom\_button\_normal/\_hover.png** 400x64.  
- **compare\_bar\_neutral / \_match / \_mismatch / \_deviation.png** 1200x64 (grey, green, amber, red, no text).  
- **btn\_accept\_normal/\_hover/\_pressed.png**, **btn\_deny\_normal/\_hover/\_pressed.png** 300x96, or stamps **stamp\_approved.png**, **stamp\_denied.png** 400x200.  
- **refbook\_page.png** 800x1100, **refbook\_cover\_currency / \_language / \_technology.png** 400x560.  
- **citizen\_records\_frame.png** 1000x700, **agency\_logo.png** 256x256 (invented seal).  
- **deviation\_report\_form.png** 800x1100.  
- **directives\_sticky.png** 400x400.  
- **citation\_slip.png** 600x800.

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

Body (240x440) \+ portrait (240x300) each, more ornate, subtle gold rim. Names follow asset ids (e.g. `Legendary_Greece_Philosopher_body.png`).

- Greece: Calliope Demarch (orator), Theron the Stargazer (philosopher), Phidian Marcos (sculptor)  
- N. Germany: Doctor Ilse Vahn (engineer), Colonel Reinhardt Voss (officer), Greta Brandt (painter). ⚠️ No real insignia.  
- Japan: Renji Tachibana (illustrator), Hideo Kanzaki (inventor), Sachiko Imari (reformer)  
- Egypt: Senmet Khare (architect), Amara Sett (painter), Nefru Tahan (vizier)  
- China: Wen Zhirong (inventor), Magistrate Lian Bo (official), Mei Qiulan (painter)  
- Wanderers: The Alchemist, The Iconoclast, The Idealist, The Muse, The Tyrant, The Visionary

21 legendaries, 42 files.

## E. Newsletters and day flow: `Assets/Art/UI/DayFlow/`

- **temporal\_times\_paper.png** 1200x1600, **temporal\_times\_masthead.png** 1200x240 ("THE TEMPORAL TIMES" baked)  
- **shift\_ledger\_paper.png** 1200x1600, **shift\_ledger\_header.png** 1200x200 ("SHIFT LEDGER")  
- **btn\_start\_shift.png**, **btn\_go\_home.png** 360x96, normal \+ hover  
- **stamp\_citation.png** 300x300

## F. Home scene: `Assets/Art/Home/`

- **home\_bg.png** 1920x1080 opaque: cramped apartment at night, bills on the table, slot machine in the corner, bed doorway.  
- **panel\_expenses.png** 800x1000.  
- **panel\_shop.png** 800x1000 \+ 256x256 icons: **upgrade\_advanced\_scanner.png**, **upgrade\_archive\_access.png**, **upgrade\_diplomatic\_contacts.png**.  
- **slot\_machine.png** 800x1000, **slot\_lever.png**, reel symbols 256x256: **slot\_small\_win.png**, **slot\_jackpot.png**, **slot\_busted.png**, **slot\_forgery\_warning.png**, **slot\_legendary\_omen.png**.  
- **btn\_sleep.png** 360x96.  
- Family: ⚠️ confirm member count/roles from `RunConfig.startingFamilyMembers`. Per member `family_<role>_portrait.png` 256x256 plus condition variants ⚠️ (confirm condition names in `HomeEconomy`).

## G. Title and endings: `Assets/Art/Title/`

- **title\_bg.png** 1920x1080 opaque.  
- **logo\_time\_sorter.png** 1200x400 transparent.  
- **btn\_continue.png**, **btn\_new\_run.png** 400x100, normal \+ hover.  
- **ending\_panel.png** 1200x800.  
- Endings 1600x900: **ending\_fired.png**, **ending\_bankrupt.png**, **ending\_retirement.png**, **ending\_scientific\_age.png**, **ending\_democracy\_triumphant.png**, **ending\_artistic\_golden\_age.png**.

## H. Shared icons: `Assets/Art/UI/Icons/`

256x256 transparent.

- Eras: **era\_rome.png**, **era\_medieval.png**, **era\_future.png** (+ Greece, NGermany, Japan, Egypt, China if the timeline cast stays).  
- Attributes: **attr\_democracy.png**, **attr\_science.png**, **attr\_art.png**, **attr\_industry.png**, **attr\_militarism.png**, **attr\_mysticism.png**, **attr\_philosophy.png**.  
- Nation emblems (reuse `<Nation>_seal.png` for the fictional six).  
- HUD: **icon\_money.png**, **icon\_stability.png**, **icon\_day.png**, **icon\_warning.png**.

---

# Totals and suggested order

1. Tier 1 booth props \+ wallpaper: 11 files. Immediate payoff, zero code.  
2. Desktop UI (B) \+ Investigation UI (C): about 60 files.  
3. Visitors (D): 72 investigation files, 42 legendary files.  
4. Day flow (E), Home (F), Title/endings (G), icons (H): about 50 files.  
5. Timeline poster variants: 18 files.

# Hand-off checklist per batch

- File names match exactly.  
- Sizes match exactly; transparent where stated.  
- No numbers or changing text baked in.  
- Tier 1: overwrite in place, keep `.meta`, set PPU.  
- Tier 2: drop into the folder; unused until the `ArtLibrary` loader is added.  
- Commit PNG **and** `.png.meta` together (PR \#2 was rejected for missing metas).