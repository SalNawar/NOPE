# Time Sorter: Art & UI Asset List for ChatGPT

*Rewritten 2026-09-25 for the 3D office; replaces the 2026-09-23 list. Checked against `main` at `da3ab90`: every path, placeholder and size below was read from the code, the builders, the scenes and the files. A copy lives in Saleh's Google Drive; `ArtDeliverables/TimeDesk/Reference/ART_ASSET_LIST.md` is the old 2026-09-23 export and is superseded by this file.*

Every piece of art the game needs today: what it is, where the player sees it, the file, the size to deliver, and whether the game already loads it.

## How to use this list

- Find the item, check its tier and status, and draw it at the **Deliver** size.
- **Tier 1: drop-in.** The game already loads this exact file. Replace the PNG in place and keep its `.meta` (it holds the import settings and every reference). No code change.
- **Tier 2: needs a hook.** The game draws a flat panel, or nothing, there today. The art can be made now; Claude adds the slot to the builder when it lands. Save the ChatGPT original in `ArtDeliverables/TimeDesk/UI/Raw/` under the file name given.
- **Tier 3: Blender.** A 3D model from the art side, not a ChatGPT image. Listed so the list is complete.
- **Status:** *placeholder* (the game's generated stand-in), *interim* (older art from the 2026-09-24 batch: the old painted style or baked text; not final), *missing* (no file), *code-drawn* (no file; the game draws a flat themed panel), *done*.
- Sizes are pixels. "At 1080p" means the 1920 × 1080 reference canvas the office overlay is laid out on.
- ⚠️ marks an item that waits on a decision from Saleh (all listed in "Decisions for Saleh").

## How the art docs fit together

| Art | Where its rules live |
|---|---|
| Characters: the layered travellers and the ten premades | [CHARACTER_ART_BRIEF_v2.md](https://github.com/SalNawar/NOPE/blob/main/docs/superpowers/drafts/art/CHARACTER_ART_BRIEF_v2.md), with [CHARACTER_ART_CONTRACT.md](https://github.com/SalNawar/NOPE/blob/main/docs/CHARACTER_ART_CONTRACT.md) and [coverage.json](https://github.com/SalNawar/NOPE/blob/main/docs/superpowers/drafts/art/coverage.json). Not repeated here. |
| The rules for all UI art: the culture wallpapers, the wheel icons, the 2D layers over the office (PC close-up frame, speech bubble, paper faces), posters (deferred) | [UI_ART_RULES.md](https://github.com/SalNawar/NOPE/blob/main/docs/superpowers/drafts/art/UI_ART_RULES.md), with the piece-6 [UI_ART_CONTRACT.md](https://github.com/SalNawar/NOPE/blob/main/docs/UI_ART_CONTRACT.md) |
| The office and its 3D props | The Blender side (art branch), placed through [SCENE_CONTRACT_GAMEPLAY.md](https://github.com/SalNawar/NOPE/blob/main/docs/SCENE_CONTRACT_GAMEPLAY.md) |
| Everything else | This list |
| The start page for ChatGPT | [README.md](https://github.com/SalNawar/NOPE/blob/main/docs/superpowers/drafts/art/README.md) |

Where this list and the code disagree, the code wins.

## Style for all 2D UI and illustration art

The same cel look as the characters and the office's cel-shaded "Desk Anime" desk: flat colour areas, each with **one hard-edged shade tone**, clean outlines, very little texture, and neutral, even lighting (no light direction, glow or cast shadow). The hard rules of UI_ART_RULES apply to every image: no baked text, no flags or symbols of power, no religious symbols, no real people or brands, interface chrome in greyscale (the game tints it), and documents never themed. Transparent PNG unless an item says opaque.

## 1. The office (Blender side, Tier 3)

The room and every prop in it are the art side's 3D models in `Assets/Scenes/OfficeScene.unity`. The gameplay layer finds them by name through the scene contract and never edits the art scene.

| Prop | Anchor, and what the game finds today (art `58bda15`) | Status |
|---|---|---|
| The desk and its mat (the papers lie on it) | `DeskSurface`: `HybridOffice/Booth/Finish_Mat` | done |
| The PC (its glass renderer or material named `Glass` or `Screen`) | `PCScreen`: `ImportedOfficeDress/Desk/Retro CRT` | done |
| The PC's power knob | `PCPower`: `.../Rebuilt CRT/CRT2_Orange` | done |
| **The scanner** | `Scanner`: nothing yet. The gameplay layer shows a stand-in flatbed (0.40 × 0.32 m) at the default pose (1.02, 1.06, −0.46). | **missing**: an `Anchor_Scanner` with the model's renderers under it, about 0.40 × 0.32 m, square to the desk, a flat top, nothing sticking out of its footprint (UI_ART_RULES, "3D office props") |
| The NEXT sign and its caption text | `NextSign`: `HybridOffice/Booth/Blender_Next`; caption `NextLabel` | done |
| The intercom (opens the traveller wheel) | `Intercom`: `ImportedOfficeDress/Desk/Clerk hotline` | done |
| Stamp, till, stability monitor, calendar, clock | `HybridOffice/Booth/Blender_Stamp`, `Finish_Till`, `Blender_Stability`, `Blender_DayCalendar`, `Blender_Clock` | done |
| Calculator, pen pot, stapler | `ImportedOfficeDress/Desk/Desk calculator`, `Pen pot`, `Forms stapler` | done |
| The readouts (day, stability, credits, shift clock) | text objects `DayNumber`, `StabilityPercent`, `CreditsNumber`, `ShiftClockDisplay` | done: faces stay blank, the game writes the text |
| Where the traveller stands, where papers are handed over | `Traveller`, `HandOver`: default poses | optional: `Tools > TimeDesk > Add Gameplay Anchors (art office)` adds the anchors to move |
| The hall crowds behind the glass | `OfficeHallCrowds` (atlas `Assets/Art/Office/HallCrowds/Textures/CrowdGroups_Atlas.png`) | done (art `5eee9bd`) |
| The wall exhibits and the city outside | `HybridOffice/Exhibits`, `HybridOffice/Exterior` | done |
| A poster frame | none | deferred: the culture posters return when the art side adds a frame with an anchor |
| Desk decoration spots | the gameplay layer's empty slots `photo`, `free_1`, `free_2` | no art planned; any decoration would be a 3D model |

## 2. The 2D layers over the office

Drawn on the office overlay canvas (1920 × 1080 reference; it scales with the screen).

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| PC frame bezel | Clicking the PC opens a front-facing monitor left of centre, 1240 × 1060 at 1080p; the desktop shows through its 4:3 glass | `Assets/Art/Office/Placeholder/pc_frame.png` | builder placeholder, 620 × 530 | **1860 × 1590**, the glass a transparent hole at x 90–1770, y 90–1350 from the top left; best rendered from the Blender CRT (UI_ART_RULES, "The PC close-up frame") | 1 | placeholder |
| Close X | the frame's top-right corner, 64 × 64 | `Assets/Art/Office/Placeholder/pc_close.png` | builder placeholder, 48 × 48 | 96 × 96 | 1 | placeholder |
| Power button | the frame's chin, 64 × 64 | `Assets/Art/Office/Placeholder/crt_power.png` | builder placeholder, 28 × 28 | 96 × 96 | 1 | placeholder |
| Power LED | the frame's chin, 18 × 18 | `Assets/Art/Office/Placeholder/crt_led.png` | a white disc, 8 × 8, tinted on and off by the game | keep | 1 | done |
| Brand plate | the frame's chin | none: the game prints "CHRONODESK 2150" | – | keep that part of the chin blank | – | – |
| Speech bubble | above the traveller's head, 420 × 110 | `ArtDeliverables/TimeDesk/UI/Raw/speech_bubble.png` | code-drawn cream panel | a 9-slice body: white or light grey (the game tints it cream), rounded corners inside the outer 32 px, a clean 2 to 3 px dark outline, no shadow. Ask ChatGPT for 1024 × 1024; Claude scales it to 128 × 128 | 2 | code-drawn |
| Speech bubble tail | under the bubble's bottom centre | `ArtDeliverables/TimeDesk/UI/Raw/speech_bubble_tail.png` | none | about 32 × 24 on screen, pointing down; deliver 64 × 48 | 2 | missing |
| Wheel choices and the "< Back" centre | on an ellipse around the traveller, 240 × 44 and 150 × 44 | the UI kit's `ui_button.png` (section 3) | code-drawn themed buttons | from the kit | 2 | code-drawn, later |
| Desk tooltip | above a clicked prop (credits, day, stability, time), 360 × 60 | the UI kit's `tooltip.png` (section 3) | code-drawn yellow panel | from the kit | 2 | code-drawn, later |

- The wheel's icons are in section 8.
- **Drawn by code, no art:** the fallback HUD (it shows only when the art office lacks a readout) and the floating hints.
- **Piece 10 (desk examination, being built now)** adds the claim tag (1100 × 64) and an office compare strip (1200 × 56) at the top of the overlay, the stamp tray (Accept and Deny above the 3D stamp), and moves the verdict line and the citation slip onto the overlay. All are code-drawn themed panels with no art slot planned; the citation slip's art (section 5) follows it there.

## 3. The PC desktop

A 4:3 canvas of 1440 × 1080, seen in the PC frame (its glass is 1120 × 840 at 1080p) and cloned small onto the office PC's screen. The culture theme recolours and refonts everything here except the documents.

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| Neutral wallpaper | behind the desktop until a country leads history | `Assets/Art/Generated/xp_bliss.png` (via `Theme_neutral`) | 1920 × 1080; the 4:3 desktop shows its middle 1440 × 1080 | keep | 1 | done |
| Culture wallpapers × 8 | behind the desktop from the morning after a country leads | `Assets/Art/Culture/<id>/wallpaper.png` for `egypt`, `iraq`, `greece`, `italy`, `china`, `japan`, `britain`, `germany` (via `Theme_<id>`) | Generate World placeholders, 960 × 540 | **1440 × 1080**: ask ChatGPT for 1536 × 1024 and Claude crops the middle (UI_ART_RULES, "Wallpapers" and "The eight cultures") | 1 | placeholder |
| Desktop icons × 17 ⚠️ | the icon column at the left: two columns of 82 × 60 tiles, text-only today | `Assets/Art/UI/Desktop/icon_<id>.png` (ids below) | 12 interim (128 × 128, a cream plate with a blue glyph), 5 missing | 128 × 128, a bold greyscale glyph on transparent (in an 82 × 60 tile it would show about 25 px tall in the frame), no plate | 2 | interim / missing |
| Cursors: arrow, hand | everywhere: the game's cursor, the hand over anything clickable | `Assets/Art/UI/Desktop/cursor_arrow.png`, `cursor_hand.png` (found by name, set in `InteractionFeedback_Default`) | interim 32 × 32 | 32 × 32; the tip (arrow) and the fingertip (hand) are the click point: the stored points are (3, 2) and (13, 3), so Claude re-measures them when new art lands | 1 | interim |
| Cursors: grab, grabbing | over a desk paper and while dragging it | `cursor_grab.png`, `cursor_grabbing.png` | none (the hand shows) | 32 × 32 | 2 | missing, later |
| UI kit × 9 | every window, button, bar and menu (table below) | `Assets/Art/UI/Desktop/` | code-drawn themed panels | greyscale 9-slice pieces | 2 | later |

**Desktop icon ids.** Apps: `directives` (Directives), `scanner` (Deviation Report), `citizen_records` (Records), `cluelog` (Clue Log, the interview transcript), `internet`, `lexicon`, `dialect`, `material`, `notes`. Reference books: `currency` (Currency Ledger), `language` (Tongues & Scripts), `technology` (Index of Devices), `capital` (Capitals Gazetteer), `ruler` (Rulers & Regents), `culture` (Costume Guide). A traveller's documents: `passport` (Travel Passport), `permit` (Transit Permit). Interim art exists for the nine apps and the first three books; `capital`, `ruler`, `culture`, `passport` and `permit` are missing.

**The UI kit (later).** UI_ART_RULES rule 5: greyscale only (white to mid grey), a flat middle and even borders so each piece stretches, no text or letter-shaped glyphs. The culture theme tints every piece. Not needed until Claude adds the theme slots.

| Piece | Used by | Now | Deliver |
|---|---|---|---|
| `window_frame.png` | every window's body and border | interim 256 × 256 (coloured) | 128 × 128, 9-slice |
| `window_titlebar.png` | the 30-unit title bars | interim 256 × 32 (coloured) | 128 × 32 |
| `ui_button.png` | every code button: minimise, maximise, close, Prev and Next, Search, the Settings choices, the Start menu entries, "< Desk", Accept and Deny, the wheel, the newsletter and Home buttons | interim `btn_min`, `btn_max`, `btn_close` (normal, hover, pressed; 24 × 24, glyphs baked) | 128 × 48, text-free (hover and pressed come from the tint) |
| `taskbar.png` | the taskbar, 36 units high | interim 256 × 40 (coloured) | 128 × 40 |
| `start_button.png` | the Start button (the game prints "start") | interim `start_button_normal`/`_pressed` 120 × 40, "Start" baked | 128 × 40, text-free |
| `tray_bg.png` | the tray (day, credits, stability, clock) | interim 256 × 40 | 128 × 32 |
| `start_menu_panel.png` | the Start menu | interim 256 × 384 | 128 × 128 |
| `input_field.png` | the Records search box | none | 128 × 48 |
| `tooltip.png` | the desk tooltip (section 2) | none | 128 × 64 |

**Drawn by code, no art:** the idle line between travellers, the claim banner, the verdict line, the compare bar, the list rows, the Settings window's contents, and the Accept and Deny glyphs (a fixed tick and cross, piece 6).

## 4. Documents

A traveller hands over a **Travel Passport** (with the photo) and a **Transit Permit**. Each lies on the desk as a paper (0.26 × 0.34 m); a scan opens its copy in a window on the PC. From piece 10 a click lifts a paper up close to read. The game prints every field and places the photo, so the faces are text-free (UI_ART_RULES, "The document paper faces").

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| Paper face | every paper on the desk; from piece 10 held up to read (up to 670 px tall at 1080p) | `Assets/Art/Office/Placeholder/paper.png` (the `_BaseMap` of `Assets/Art/Office/Gameplay/Materials/Paper.mat`) | builder placeholder, 150 × 200, plain cream | **1024 × 1339** (at least 784 × 1024): a paper tone and a printed border, perhaps a guilloche band behind the title; no labels, lines, boxes, emblems or seals. Claude turns mipmaps on when it lands | 1 | placeholder |
| Passport face, permit face ⚠️ | one face per document kind instead of one for all | set by the paper faces' own brief, which follows piece 10 | none | as the paper face | 2 | waits on piece 10 |
| Photo frame ⚠️ | the passport photo's 4:5 window: on the desk paper and on the scanned copy | with the faces' brief (it may be printed on the passport face instead) | code-drawn grey box | 4:5, for example 480 × 600 with a transparent window | 2 | code-drawn |
| The scanned copy | the document window on the PC: a page on a dark backing | the hook reuses the paper face | code-drawn | no separate art | 2 | code-drawn |
| Reference book covers × 6 ⚠️ | nowhere today: a book is a text tile and a window | `Assets/Art/UI/Investigation/refbook_cover_<id>.png` for `currency`, `language`, `technology`, `capital`, `ruler`, `culture` | 3 interim (400 × 560, titles baked), 3 missing | 400 × 560, a closed cover with a simple motif, no title | 2 | interim / missing |
| Verdict ink marks ⚠️ | on a paper after the verdict | `stamp_accept.png`, `stamp_deny.png` (new names) | none | 400 × 200, transparent, a text-free tick mark and cross mark | 2 | missing |

- **The stamp itself** is the Blender prop (section 1). The PC's Accept and Deny buttons are code-drawn, and so is piece 10's stamp tray.
- **Drawn by code, no art:** the reference book pages, Citizen Records, the Deviation Report, the Directives sticky note and the transcript. All are windows, so they take the UI kit.

## 5. The day flow

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| Morning briefing sheet | "The Temporal Times", over the office at the start of each day, 700 × 780 | `Assets/Art/UI/DayFlow/temporal_times_paper.png` | interim 1200 × 1600, ruled; the live panel is code-drawn | 1400 × 1560, a newsprint sheet, text-free (the game prints the masthead, the title and the news) | 2 | interim |
| Shift ledger sheet | "Shift Ledger: Evening Edition", over the office at the end of the shift, 700 × 780 | `Assets/Art/UI/DayFlow/shift_ledger_paper.png` | interim 1200 × 1600, ruled | 1400 × 1560, text-free | 2 | interim |
| Citation slip | "Timeline Deviation Notice" after a wrong verdict, centre, 560 × 320 (on the desktop today; on the office overlay from piece 10) | `Assets/Art/UI/Investigation/citation_slip.png` | interim 600 × 800 (portrait, ruled); the live panel is a code-drawn red panel | 1120 × 640, a light paper slip with a printed border, text-free | 2 | interim |

- The two sheets' "Start shift" and "Go home" buttons take the UI kit's button.
- **Drawn by code, no art:** the verdict line and the idle line.

## 6. Home

`Assets/Scenes/HomeScene.unity`. The Tier 1 art here is wired in the scene by hand (the Home builder makes only the panels), so a file replaced in place keeps working.

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| Home background ⚠️ | behind every Home panel, full screen | `Assets/Art/Home/home_bg.png` (`Canvas/ArtBackground`) | interim 1920 × 1080, painted, warm lamp light | 1920 × 1080, opaque | 1 | interim |
| Expenses panel | the day's bills and the family rows, 760 × 600 | `Assets/Art/Home/panel_expenses.png` (`ExpensesPanel`) | interim 800 × 1000, stretched to 760 × 600 | 1520 × 1200, a text-free paper panel | 1 | interim |
| Shop panel | the upgrade shop, 760 × 600 | `Assets/Art/Home/panel_shop.png` (`ShopPanel`) | interim 800 × 1000, stretched | 1520 × 1200 | 1 | interim |
| Slot panel ⚠️ | the slot machine, 560 × 360 | `Assets/Art/Home/panel_slot.png` | code-drawn | 1120 × 720 | 2 | code-drawn |
| Sleep panel | "Turn in", 560 × 320 | `Assets/Art/Home/panel_sleep.png` | code-drawn | 1120 × 640 | 2 | code-drawn |
| Slot machine ⚠️ | in the slot panel | `Assets/Art/Home/slot_machine.png` | interim 800 × 1000, portrait: it does not fit the landscape panel | set with the panel's layout | 2 | interim |
| Slot lever | beside the machine | `Assets/Art/Home/slot_lever.png` | interim 200 × 600 | with the layout | 2 | interim |
| Slot outcome symbols × 5 | the result of a spin | `Assets/Art/Home/slot_<outcome id>.png` for `small_win`, `jackpot_cash`, `busted_machine`, `forgery_warning`, `legendary_omen` | 5 interim at 256 × 256; two still carry older names (`slot_jackpot`, `slot_busted`) | 256 × 256 | 2 | interim |
| Upgrade icons × 12 ⚠️ | one per shop row (the rows are text today) | `Assets/Art/Home/upgrade_<id>.png` (ids below) | 3 interim at 256 × 256 (`upgrade_archive_access`, and `upgrade_advanced_scanner` and `upgrade_diplomatic_contacts` under older names), 9 missing | 256 × 256, for about 48 px in a shop row | 2 | interim / missing |
| Family portraits ⚠️ | the family rows: the Partner and the Kid (`RunConfig.startingFamilyMembers`), whose condition runs from 0 to 10 | `Assets/Art/Home/family_<member>_<band>.png` | none: the rows are text | 512 × 512, in the character style; 2 members × 3 condition bands | 2 | missing |

**Upgrade ids (12).** `adv_scanner` (Advanced Scanner), `archive_access` (Archive Access), `diplo_contacts` (Diplomatic Contacts), `interview_protocols` (Interview Protocols), and the eight translators: `tr_near_east_written` and `tr_near_east_spoken` (Near East Translator: Papers, Speech), `tr_mediterranean_written`, `tr_mediterranean_spoken`, `tr_east_asia_written`, `tr_east_asia_spoken`, `tr_north_europe_written`, `tr_north_europe_spoken`.

- **Drawn by code, no art:** the HUD line and the rows. The buttons take the UI kit's button.

## 7. Title and endings

`Assets/Scenes/TitleScene.unity`. The Tier 1 art is wired in the scene by hand.

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| Title background ⚠️ | the first screen, full screen | `Assets/Art/Title/title_bg.png` (`Canvas/ArtBackground`) | interim 1920 × 1080, painted (an older office, warm light) | 1920 × 1080, opaque | 1 | interim |
| Logo ⚠️ | the title panel's top (the printed title is switched off; the logo is the title) | `Assets/Art/Title/logo_time_sorter.png` (`TitlePanel/ArtLogo`, keeps its aspect) | interim 1200 × 400, "TIME SORTER" lettered | 1800 × 600 | 1 | interim |
| Title buttons ⚠️ | Continue and New Run on the title, New Run on the ending panel; the sprite swaps on hover; their labels are off | `Assets/Art/Title/btn_continue_normal.png`, `btn_continue_hover.png`, `btn_new_run_normal.png`, `btn_new_run_hover.png` | interim 400 × 100, English baked, stretched to 1152 × 162 (title) and 304 × 73 (ending panel) | a text-free 9-slice face, 512 × 128, normal and hover; Claude then switches the three buttons to sliced with their labels on | 1 | interim |
| Ending panel | the run's ending, 760 × 520 | `Assets/Art/Title/ending_panel.png` | interim 1200 × 800, not wired | 1520 × 1040, text-free | 2 | interim |
| Ending illustrations × 6 ⚠️ | behind the ending text | `Assets/Art/Title/ending_<id>.png` for `fired`, `bankrupt`, `retirement`, `scientific_age`, `democracy_triumphant`, `artistic_golden_age` | interim 1600 × 900, painted, not wired (an ending has no picture field yet) | 1920 × 1080, opaque | 2 | interim |

## 8. Icons

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| Wheel icons × 6 | at the left of each traveller-wheel choice, 28 px at 1080p, on a dark blue button | `Assets/Art/UI/Resources/WheelIcons/wheel_<kind>.png` for `request` (a sheet of paper or an open hand), `question` (a speech balloon), `look` (an eye), `dialog` (two balloons), `back` (an arrow pointing left), `normal` (a small dot) | white glyphs of the same shapes drawn at run time, 32 × 32, never on disk; the folder does not exist yet | 64 × 64, white on transparent, imported as Sprite (2D and UI) | 1 | missing |

- The desktop icons are in section 3, the upgrade icons in section 6.
- **Every icon:** a bold, simple silhouette that reads at 24 px; no letters, question marks or exclamation marks; a white or grey glyph on transparent (the game may tint it); no plate behind it unless the item says so.

## Characters (pointer)

About 880 files (50 bases, 759 garments, 31 Future outfits, 40 premade expressions) from about 406 ChatGPT images, at `Assets/Art/Characters/Resources/Characters/<key>.png`, 1024 × 1536. Tier 1: the game loads each by key at run time and draws a 256 × 384 placeholder until it lands. Status: missing (the folder does not exist yet). Everything else is in the character brief and `coverage.json`.

## Totals

New 2D files (the Blender scanner and the characters are counted apart):

| Section | Tier 1 | Tier 2 | Files |
|---|---|---|---|
| 1. The office (Blender) | – | – | the scanner model (Tier 3) |
| 2. 2D layers over the office | 3 | 2 | 5 |
| 3. PC desktop | 10 | 28 | 38 |
| 4. Documents | 1 | 11 | 12 |
| 5. Day flow | 0 | 3 | 3 |
| 6. Home | 3 | 27 | 30 |
| 7. Title and endings | 6 | 7 | 13 |
| 8. Icons | 6 | 0 | 6 |
| **Total** | **29** | **78** | **107** |

- 107 if every ⚠️ question is answered yes. A no to decisions 3, 5, 6, 7, 8 and 10 (the title buttons, the desktop icons, the per-document faces with the photo frame, the book covers, the ink marks, the family) and two shared translator icons (decision 9) leave 63.
- Also: the scanner (1 Blender model) and about 880 character files (the brief).

## Suggested order

What the player sees first comes first. Within a step, the Tier 1 files come first: they pay off with no code.

1. **The title screen:** background, logo, buttons (6, Tier 1), once decisions 1 to 3 are made.
2. **The morning briefing** sheet (1).
3. **The office:** the scanner (Blender) and the travellers (the character brief's batches, its own track).
4. **At the desk:** the wheel icons (6, Tier 1), the paper face (1, Tier 1), the speech bubble and its tail (2).
5. **The PC:** the frame, close X and power button (3, Tier 1); the culture wallpapers (8, Tier 1; the first one shows from day 2 at the earliest); the desktop icons (17).
6. **The end of the shift:** the citation slip and the shift ledger (2).
7. **Home:** the background and the two panels (3, Tier 1); the upgrade icons (12); the slot machine, lever and symbols (7) with the slot and sleep panels (2); the family (6).
8. **The endings:** the panel and the six illustrations (7).
9. **Polish:** the UI kit (9), the cursors (2 Tier 1 and 2 Tier 2), the document faces and photo frame after piece 10 (3), the book covers (6), the ink marks (2).

## Decisions for Saleh ⚠️

1. **Title background:** keep the interim painting (an older office in warm light) or redraw it in the cel style to show the 3D office (for example from a render the art side makes)?
2. **Logo:** keep a lettered "Time Sorter" wordmark (the one place lettering would be allowed) or a text-free emblem with the title printed by the game?
3. **Title buttons:** text-free faces with the labels on (one button look everywhere), or keep the baked English?
4. **Home background and ending illustrations:** keep the interim painted style or redraw them in the cel style?
5. **Desktop icons:** give the 17 icon tiles pictures (a small tile layout change), greyscale and tinted by the culture per rule 5?
6. **Paper faces:** one face for every document, or one per document kind (passport, permit)? Decided with the faces' brief after piece 10, together with the photo frame.
7. **Reference book covers:** show them somewhere (the book window or its tile), or drop them?
8. **Verdict ink mark:** should a verdict leave a text-free tick or cross mark on the paper?
9. **Translator icons:** eight (four regions × papers and speech) or two (a papers glyph and a speech glyph, shared by the regions)?
10. **Family:** draw the Partner and the Kid (in the character style, three condition bands each), or keep the rows text-only?
11. **Slot machine:** the panel is landscape (560 × 360) and the interim machine is portrait. Enlarge the panel, or draw a landscape machine?

## Retired (don't make these)

Many retired files still sit in `Assets/Art` from the 2026-09-24 batch. Several are referenced only by the art scene's leftover 2D booth and old canvases (switched off when the office loads) and by the recovery scenes, so they stay on disk until the art side deletes those leftovers ([SCENE_CONTRACT_GAMEPLAY.md](https://github.com/SalNawar/NOPE/blob/main/docs/SCENE_CONTRACT_GAMEPLAY.md), "What the art scene must not do").

| Old item (2026-09-23 list) | Files on disk | Why it is retired |
|---|---|---|
| The style brief: "2D painted illustration", light from the top left; the pixels-per-unit import rule (PPU 400) | – | Replaced by the cel style above. The office is 3D, so no sprite is sized in world units. |
| Booth props: `backwall`, `partition`, `desk`, `crt` (and the front-facing CRT request), `sign`, `calendar`, `stabilitymonitor`, `till`, `poster` | `Assets/Art/Office/Placeholder/`; also `Office/Booth/desk_deep.png`, `partition_calendar.png` | The office and its props are the art side's 3D models (section 1). |
| `traveller.png` (240 × 440) | `Assets/Art/Office/Placeholder/traveller.png` (60 × 110) | Unused by the game: travellers are layered figures (the character brief). |
| A new 1920 × 1080 desktop wallpaper | – | The desktop is 4:3. `xp_bliss.png` stays as the neutral wallpaper; the eight culture wallpapers are the new art. |
| Timeline poster variants (18 then; 27 files now) | `Assets/Art/Office/Placeholder/Posters/` | The 3D office has no poster frame; posters are deferred (UI_ART_RULES, "Posters: later"). |
| Crops of the existing `ArtDeliverables` scene layers (01-waiting-hall to 11-stability-monitor, 02-queue, 08-intercom, Desktop/icons-4x4, window-frame, traveller-explorer) | `ArtDeliverables/TimeDesk/` | Layers of the 2D booth; nothing to crop now. |
| `queue_bg`, `queue_silhouette_01` to `06` | – | The art side's 3D hall crowds. |
| `intercom.png`, `scanner_tray.png`, `desk_mug`, `desk_photo`, `desk_plant`, `desk_stamp` | `Assets/Art/Office/Booth/` | 3D props: the Clerk hotline, the scanner model (section 1), the art side's desk. |
| `lighting_overlay_warm`, `_cold`, `_alarm` | `Assets/Art/Office/Booth/` | The 3D office lights itself; there is no overlay slot. |
| `btn_back_to_office`, `intercom_panel`, `intercom_button_normal`/`_hover` | `UI/Desktop/`, `UI/Investigation/` | Retired in piece 7: the taskbar's "< Desk" and the traveller wheel replace them. |
| `btn_desk_normal`/`_hover`/`_pressed`, `wheel_item_*`, `wheel_centre_*`, `desk_tooltip`, `speech_bubble` (420 × 110) | – | Replaced by the UI kit's button and tooltip (section 3) and the 9-slice speech bubble (section 2). |
| `btn_min`/`btn_max`/`btn_close` (9 states), `start_button_normal`/`_pressed` | `UI/Desktop/` | Replaced by the kit's text-free `ui_button.png` and `start_button.png`. |
| `icon_compare` | `UI/Desktop/` | There is no Compare app: comparing is the compare bar. |
| `icon_settings`, `icon_power`, `tray_day`, `tray_credits`, `tray_stability` | `UI/Desktop/` | The Start menu and the tray print text. |
| `scanner_backing`, `page_blank`, `refbook_page`, `claim_banner`, `compare_bar_neutral`/`_match`/`_mismatch`/`_deviation`, `citizen_records_frame`, `deviation_report_form`, `directives_sticky` | `UI/Investigation/` | Code-drawn themed windows and strips; the scanned copy reuses the paper face. |
| `btn_accept`, `btn_deny` (normal, hover, pressed) | `UI/Investigation/` | Baked words. The decision buttons are themed per culture with fixed code glyphs (piece 6). |
| `stamp_approved`, `stamp_denied`, `stamp_citation` | `UI/Investigation/`, `UI/DayFlow/` | Baked words, which break the language switch; see decision 8. |
| `agency_logo`, the nation seals | `UI/Investigation/` | Paper faces carry no emblems or seals. |
| Per-nation document kits (Aegyptus, Albion, Helios, Latia, Norvik, Solaris; 24 files then), and the 128 kit files now on disk | `UI/Documents/<Country>/<Ancient/Old/Modern/Future>/` | The invented nations are gone; the game has six eras, not these four; the stocks bake text and field lines; the paper face is text-free with a code layout (section 4). |
| Visitor trios, 240 × 440 (72 files), and the timeline cast | – | Characters are layered 1024 × 1536 figures (the character brief). |
| 21 invented legendaries (42 files) | – | The ten premades, real people drawn whole, replace them (the brief, section 11). |
| `temporal_times_masthead`, `shift_ledger_header` | `UI/DayFlow/` | The game prints the mastheads. |
| `btn_start_shift`, `btn_go_home`, `btn_sleep` | `UI/DayFlow/`, `Home/` | Baked words; the kit's button replaces them. |
| Era icons (`era_rome`, `era_medieval`, `era_future` then; `era_ancient`, `_old`, `_modern`, `_future` on disk) | `UI/Icons/` | No screen shows eras as icons, and the game has six eras. |
| Attribute icons (7 then; 6 on disk) | `UI/Icons/` | No screen shows attributes as icons; the game has three (democracy, science, art). |
| `icon_money`, `icon_stability`, `icon_day`, `icon_warning` | `UI/Icons/` | The office readouts are the art side's 3D displays; the tray prints text. |
| The `ArtLibrary` loader | – | Each hook goes into the builder that owns its screen. |

## Delivery checklist

- File names exactly as listed; sizes as listed; transparent unless the item says opaque.
- No text, letters or numbers in any image (the logo waits on decision 2).
- Tier 1: overwrite the file in place and keep its `.meta`.
- Tier 2: save the original in `ArtDeliverables/TimeDesk/UI/Raw/`; Claude wires it.
- Commit each PNG with its `.meta`.
