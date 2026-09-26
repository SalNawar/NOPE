# Time Sorter: Art & UI Asset List for ChatGPT

*Rewritten 2026-09-25 for the 3D office; replaces the 2026-09-23 list. Checked against `main` at `da3ab90`: every path, placeholder and size below was read from the code, the builders, the scenes and the files. A copy lives in Saleh's Google Drive; `ArtDeliverables/TimeDesk/Reference/ART_ASSET_LIST.md` is the old 2026-09-23 export and is superseded by this file.*

*Tiers, statuses and paths updated 2026-09-26 for the art hooks (redesign phase 27): the Tier-2 images now have by-name slots, so most of them are drop-in.*

Every piece of art the game needs today: what it is, where the player sees it, the file, the size to deliver, and whether the game already loads it.

## How to use this list

- Find the item, check its tier and status, and draw it at the **Deliver** size.
- **Tier 1: drop-in.** The game already loads this exact file. No code change and no rebuild:
  - a file that exists: replace the PNG in place and keep its `.meta` (it holds the import settings and every reference);
  - a **by-name slot** (a path under `Assets/Art/UI/Resources/`, redesign phase 27): put the PNG there under the exact name given; Unity makes its `.meta` and `ArtSlotImporter` sets the import (a sprite; 9-slice borders where the item says 9-slice). Until the file exists the game keeps the look in the **Now** column. The rules: `docs/UI_ART_CONTRACT.md`, "By-name art slots".
- **Tier 2: needs a hook.** The game draws a flat panel, or nothing, there today, and has no slot yet. The art can be made now; Claude adds the slot when it lands. Save the ChatGPT original in `ArtDeliverables/TimeDesk/UI/Raw/` under the file name given (for every item, keep the ChatGPT original there too).
- **Tier 3: Blender.** A 3D model from the art side, not a ChatGPT image. Listed so the list is complete.
- **Status:** *placeholder* (the game's generated stand-in), *interim* (older art from the 2026-09-24 batch: the old painted style or baked text; not final), *missing* (no file), *code-drawn* (no file; the game draws a flat themed panel), *done*.
- Sizes are pixels. "At 1080p" means the 1920 × 1080 reference canvas the office overlay is laid out on.
- Every decision the first version marked ⚠️ is made (Saleh, 2026-09-25: "yes to all"); see "Decisions".

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
| PC frame bezel | Clicking the PC opens a front-facing monitor left of centre, 1240 × 1060 at 1080p; the desktop shows through its 4:3 glass | `Assets/Art/Office/Placeholder/pc_frame.png` | builder placeholder, 620 × 530 (the builder keeps an existing file: checked 2026-09-26) | **1860 × 1590**, the glass a transparent hole at x 90–1770, y 90–1350 from the top left; best rendered from the Blender CRT (UI_ART_RULES, "The PC close-up frame") | 1 | placeholder |
| Close X | the frame's top-right corner, 64 × 64 | `Assets/Art/Office/Placeholder/pc_close.png` | builder placeholder, 48 × 48 | 96 × 96 | 1 | placeholder |
| Power button | the frame's chin, 64 × 64 | `Assets/Art/Office/Placeholder/crt_power.png` | builder placeholder, 28 × 28 | 96 × 96 | 1 | placeholder |
| Power LED | the frame's chin, 18 × 18 | `Assets/Art/Office/Placeholder/crt_led.png` | a white disc, 8 × 8, tinted on and off by the game | keep | 1 | done |
| Brand plate | the frame's chin | none: the game prints "CHRONODESK 2150" | – | keep that part of the chin blank | – | – |
| Speech bubble | above the traveller's head, 420 × 110 | `Assets/Art/UI/Resources/Office/speech_bubble.png` | code-drawn cream panel | a 9-slice body: white or light grey (the game tints it cream), rounded corners inside the outer quarter of each side (the importer slices at a quarter of the shorter side), a clean 2 to 3 px dark outline, no shadow, no margin round the edge. Ask ChatGPT for 1024 × 1024; Claude scales it to 128 × 128 | 1 | code-drawn |
| Speech bubble tail | under the bubble's bottom centre, its top on the bubble's bottom edge | `Assets/Art/UI/Resources/Office/speech_bubble_tail.png` | none (the bubble has no tail until it lands) | about 32 × 24 on screen, pointing down, white or light grey with the bubble's outline; deliver 64 × 48 | 1 | missing |
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
| Desktop icons × 6 | the six free-placed desktop icons (plan phase 17): a 72 × 72 glyph over its label in a 120 × 132 cell, tinted by the theme | `Assets/Art/UI/Resources/Desktop/icon_<id>.png` (ids below) | code-drawn placeholder glyphs (`DesktopIconPlaceholder`); the old interim `icon_internet`, `icon_notes` and `icon_settings` in `UI/Desktop/` (cream plates) are not used | 128 × 128, a bold greyscale glyph on transparent (it shows about 56 px tall at 1080p), no plate | 1 | placeholder |
| Cursors: arrow, hand | everywhere: the game's cursor, the hand over anything clickable | `Assets/Art/UI/Desktop/cursor_arrow.png`, `cursor_hand.png` (found by name, set in `InteractionFeedback_Default`) | interim 32 × 32 | 32 × 32; the tip (arrow) and the fingertip (hand) are the click point: the stored points are (3, 2) and (13, 3), so Claude re-measures them when new art lands | 1 | interim |
| Cursors: grab, grabbing | over a desk paper and while dragging it | `cursor_grab.png`, `cursor_grabbing.png` | none (the hand shows) | 32 × 32 | 2 | missing, later |
| UI kit × 9 | every window, button, bar and menu (table below) | `Assets/Art/UI/Desktop/` | code-drawn themed panels | greyscale 9-slice pieces | 2 | later |

**Desktop icon ids** (the PC redesign DK1; `DesktopAppIds`): `investigation`, `internet`, `mail`, `citizen_account`, `notes`, `settings`, and nothing else. The old tiles' interim glyphs (`directives`, `scanner`, `citizen_records`, `cluelog`, the books) move to the Investigation app's tab glyphs with the app (plan phase 16); `lexicon`, `dialect` and `material` are retired with their placeholder apps. The slot is wired (plan phase 27): each icon shows its file when it exists, else the placeholder glyph for its id.

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

A traveller hands over Temporal Customs forms (the TC forms of the redesign). Each lies on the desk as a paper (0.26 × 0.34 m); a scan opens its copy in a window on the PC; a click lifts a paper up close to read. The game prints every field, box and line and places the photo, so the faces are text-free (UI_ART_RULES, "The document paper faces").

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| Paper face | every paper on the desk; from piece 10 held up to read (up to 670 px tall at 1080p) | `Assets/Art/Office/Placeholder/paper.png` (the `_BaseMap` of `Assets/Art/Office/Gameplay/Materials/Paper.mat`) | builder placeholder, 150 × 200, plain cream | **1024 × 1339** (at least 784 × 1024): a paper tone and a printed border, perhaps a guilloche band behind the title; no labels, lines, boxes, emblems or seals. Claude turns mipmaps on when it lands | 1 | placeholder |
| A face per document kind × 10 | every desk paper of that kind (it replaces the paper face above for that kind) | `Assets/Art/UI/Resources/Forms/paper_<form number>.png`, the number lower case without its dash: `paper_tc101`, `tc230`, `tc310`, `tc415`, `tc416`, `tc417`, `tc520`, `tc610`, `tc620`, `tc630` (the PC spec's FO8) | the plain agency face below, else the paper face above | as the paper face: a paper tone, a printed border, a guilloche band behind the header, perhaps the kind's tint; **no boxes or lines** (the code draws them) | 1 | missing |
| Plain agency face | a desk paper whose kind has no face of its own; the PC's pages (plan phase 5) | `Assets/Art/UI/Resources/Forms/paper_agency.png` | the paper face above | as the paper face, plain agency paper | 1 | missing |
| Agency seal | printed at 10 % behind every form's header | `Assets/Art/UI/Resources/Forms/agency_seal.png` | the builder's code-drawn ring (`Assets/Art/Office/Placeholder/form_seal.png`) | 512 × 512, greyscale, Temporal Customs' own abstract mark (for example an hourglass in a ring); not a flag, crest or nation's emblem; no letters | 1 | code-drawn |
| Photo frame | the photo's 4:5 window on the desk paper: drawn over the photo | `Assets/Art/UI/Resources/Forms/photo_frame.png` | interim 240 × 300 (a grey border with a transparent window), wired; the grey frame behind it stays | 4:5, for example 480 × 600 with a transparent window | 1 | interim |
| The scanned copy | the document window on the PC: a page on a dark backing | reuses the faces above (plan phase 5's form view calls the same slots) | code-drawn | no separate art | 2 | code-drawn, waits on phase 5 |
| Reference book covers × 6 | on the book's tile (left of its name) and at the top of its window; the Investigation app can show them (`SlotArt.CoverFor`) | `Assets/Art/UI/Resources/Investigation/refbook_cover_<id>.png` for `currency`, `language`, `technology`, `capital`, `ruler`, `culture` | 3 interim (400 × 560, English titles baked), wired; 3 missing (the tile and the window show no cover) | 400 × 560, a closed cover with a simple motif, no title | 1 | interim / missing |
| Verdict ink marks | on every paper as it leaves after the verdict, in its stamp area (fitted at its own aspect) | `Assets/Art/UI/Resources/Forms/stamp_accept.png`, `stamp_deny.png` | none | 400 × 200, transparent, a text-free tick mark and cross mark in their own ink colours | 1 | missing |

- **The stamp itself** is the Blender prop (section 1). The PC's Accept and Deny buttons are code-drawn, and so is piece 10's stamp tray.
- **Drawn by code, no art:** the reference book pages, Citizen Records, the Deviation Report, the Directives sticky note and the transcript. All are windows, so they take the UI kit.

## 5. The day flow

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| Morning briefing sheet | "The Temporal Times", over the office at the start of each day, 700 × 780 | `Assets/Art/UI/Resources/DayFlow/temporal_times_paper.png` | interim 1200 × 1600, plain, wired; tinted by the newsletter's colour (the flat panel's) | 1400 × 1560, a light, nearly neutral newsprint sheet (the game tints it per culture), text-free (the game prints the masthead, the title and the news) | 1 | interim |
| Shift ledger sheet | "Shift Ledger: Evening Edition", over the office at the end of the shift, 700 × 780 | `Assets/Art/UI/Resources/DayFlow/shift_ledger_paper.png` | interim 1200 × 1600, ruled, wired; tinted like the briefing | 1400 × 1560, light and nearly neutral, text-free | 1 | interim |
| Citation slip | "Timeline Deviation Notice" after a wrong verdict, on the office overlay, centre, 560 × 320 | `Assets/Art/UI/Resources/DayFlow/citation_slip.png` | interim 600 × 800 (portrait, ruled), wired: stretched onto the landscape slip and tinted the alert red (the white text stays readable) | 1120 × 640, a light paper slip with a printed border, text-free; the game tints it the alert colour | 1 | interim |

- The two sheets' "Start shift" and "Go home" buttons take the UI kit's button.
- **Drawn by code, no art:** the verdict line and the idle line.

## 6. Home

`Assets/Scenes/HomeScene.unity`. The Tier 1 art here is wired in the scene by hand (the Home builder makes only the panels), so a file replaced in place keeps working.

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| Home background | behind every Home panel, full screen | `Assets/Art/Home/home_bg.png` (`Canvas/ArtBackground`) | interim 1920 × 1080, painted, warm lamp light | 1920 × 1080, opaque, redrawn in the cel style | 1 | interim |
| Expenses panel | the day's bills and the family rows, 760 × 600 | `Assets/Art/Home/panel_expenses.png` (`ExpensesPanel`) | interim 800 × 1000, stretched to 760 × 600 | 1520 × 1200, a text-free paper panel | 1 | interim |
| Shop panel | the upgrade shop, 760 × 600 | `Assets/Art/Home/panel_shop.png` (`ShopPanel`) | interim 800 × 1000, stretched | 1520 × 1200 | 1 | interim |
| Slot panel | the slot machine's panel, 560 × 360 | `Assets/Art/Home/panel_slot.png` | code-drawn (dark, with white text: a light panel needs the texts recoloured, so there is no slot yet) | 1120 × 720 | 2 | code-drawn |
| Sleep panel | "Turn in", 560 × 320 | `Assets/Art/Home/panel_sleep.png` | code-drawn (as the slot panel) | 1120 × 640 | 2 | code-drawn |
| Slot machine | standing on the slot panel's top edge, in a 375 × 240 box at its own aspect (the panel's white texts stay on the dark panel) | `Assets/Art/UI/Resources/Home/slot_machine.png` | interim 800 × 1000, portrait, wired (it shows small in the landscape box) | a landscape machine, 1000 × 640, the reels in the middle, no lever in the picture | 1 | interim |
| Slot lever | at the machine box's right edge, 80 × 240 | `Assets/Art/UI/Resources/Home/slot_lever.png` | interim 200 × 600, wired | 160 × 480, standing | 1 | interim |
| Slot outcome symbols × 5 | the result of a spin | `Assets/Art/Home/slot_<outcome id>.png` for `small_win`, `jackpot_cash`, `busted_machine`, `forgery_warning`, `legendary_omen` | 5 interim at 256 × 256, not wired (a spin shows text only); two still carry older names (`slot_jackpot`, `slot_busted`) | 256 × 256 | 2 | interim |
| Upgrade icons × 8 | at the left of each shop row, 44 × 44 | `Assets/Art/UI/Resources/Home/upgrade_<id>.png` (ids below) | 3 interim at 256 × 256, wired (`upgrade_adv_scanner`, `upgrade_archive_access`, `upgrade_diplo_contacts`); 5 missing (their rows are text only) | 256 × 256 | 1 | interim / missing |
| Family portraits × 6 | at the left of the member's family row, 44 × 44: the Partner and the Kid (`RunConfig.startingFamilyMembers`), whose condition runs from 0 to 10 | `Assets/Art/UI/Resources/Home/family_<member>_<band>.png`: members `partner`, `kid`; bands `well` (condition 0–3), `ill` (4–7), `grave` (8–10) | none: the rows are text only | 512 × 512, in the character style; 2 members × 3 condition bands | 1 | missing |

**Upgrade ids (8).** `adv_scanner` (Advanced Scanner), `archive_access` (Archive Access), `diplo_contacts` (Diplomatic Contacts), `interview_protocols` (Interview Protocols), and the four Speech translators (papers are always English, so the Papers translators retired): `tr_near_east_spoken` (Near East Translator: Speech), `tr_mediterranean_spoken`, `tr_east_asia_spoken`, `tr_north_europe_spoken`.

- **Drawn by code, no art:** the HUD line and the rows' text. The buttons take the UI kit's button.

## 7. Title and endings

`Assets/Scenes/TitleScene.unity`. The Tier 1 art is wired in the scene by hand.

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| Title background | the first screen, full screen | `Assets/Art/Title/title_bg.png` (`Canvas/ArtBackground`) | interim 1920 × 1080, painted (an older office, warm light) | 1920 × 1080, opaque, redrawn in the cel style showing the 3D office (from a render the art side makes) | 1 | interim |
| Logo | the title panel's top (the printed title is switched off; the logo is the title) | `Assets/Art/Title/logo_time_sorter.png` (`TitlePanel/ArtLogo`, keeps its aspect) | interim 1200 × 400, "TIME SORTER" lettered | 1800 × 600, the lettered wordmark "TIME SORTER" (the one place lettering is allowed) in the cel style | 1 | interim |
| Title button face | Continue and New Run on the title, New Run on the ending panel: one text-free face for all three, sliced, their labels on; the hover face swaps in under the pointer | `Assets/Art/UI/Resources/Title/title_button.png`, `title_button_hover.png` | the interim `Assets/Art/Title/btn_continue_*` and `btn_new_run_*` (400 × 100, English baked, labels off) until the face lands | a text-free 9-slice face, 512 × 128, normal and hover (the importer slices at 32 px: keep the rounded corners inside it) | 1 | missing (interim fallback) |
| Ending panel | the run's ending, 760 × 520 | `Assets/Art/Title/ending_panel.png` | interim 1200 × 800, not wired (the panel's white text needs a dark panel) | 1520 × 1040, text-free | 2 | interim |
| Ending illustrations × 6 | full screen behind the ending panel (`EndingSO.picture`) | `Assets/Art/Title/ending_<id>.png` for `fired`, `bankrupt`, `retirement`, `scientific_age`, `democracy_triumphant`, `artistic_golden_age` | interim 1600 × 900, painted, wired | 1920 × 1080, opaque, redrawn in the cel style; replace in place | 1 | interim |

## 8. Icons

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| Wheel icons × 6 | at the left of each traveller-wheel choice, 28 px at 1080p, on a dark blue button | `Assets/Art/UI/Resources/WheelIcons/wheel_<kind>.png` for `request` (a sheet of paper or an open hand), `question` (a speech balloon), `look` (an eye), `dialog` (two balloons), `back` (an arrow pointing left), `normal` (a small dot) | white glyphs of the same shapes drawn at run time, 32 × 32, never on disk; the folder does not exist yet (a file dropped there shows: checked 2026-09-26) | 64 × 64, white on transparent (the importer makes it a sprite) | 1 | missing |

- The desktop icons are in section 3, the upgrade icons in section 6.
- **Every icon:** a bold, simple silhouette that reads at 24 px; no letters, question marks or exclamation marks; a white or grey glyph on transparent (the game may tint it); no plate behind it unless the item says so.

## Characters (pointer)

About 880 files (50 bases, 759 garments, 31 Future outfits, 40 premade expressions) from about 406 ChatGPT images, at `Assets/Art/Characters/Resources/Characters/<key>.png`, 1024 × 1536. Tier 1: the game loads each by key at run time and draws a 256 × 384 placeholder until it lands. Status: missing (the folder does not exist yet). Everything else is in the character brief and `coverage.json`.

## Totals

New 2D files (the Blender scanner and the characters are counted apart):

| Section | Tier 1 | Tier 2 | Files |
|---|---|---|---|
| 1. The office (Blender) | – | – | the scanner model (Tier 3) |
| 2. 2D layers over the office | 5 | 0 | 5 |
| 3. PC desktop | 16 | 11 | 27 |
| 4. Documents | 22 | 0 | 22 |
| 5. Day flow | 3 | 0 | 3 |
| 6. Home | 19 | 7 | 26 |
| 7. Title and endings | 10 | 1 | 11 |
| 8. Icons | 6 | 0 | 6 |
| **Total** | **81** | **19** | **100** |

- Every decision was answered yes, so all are wanted. The count moved from 107 to 100 with the redesign: six desktop icons instead of seventeen, one Title face (normal and hover) instead of four baked buttons, eight upgrade icons, and ten per-kind faces, the plain agency face and the seal instead of the passport and permit faces.
- Tier 2 now: the UI kit (9) and the grab cursors (2), the slot and sleep panels and the ending panel (their white text needs a dark panel), the slot outcome symbols (5); the scanned copy waits on plan phase 5 and reuses the faces.
- Also: the scanner (1 Blender model) and about 880 character files (the brief).

## Suggested order

What the player sees first comes first. Within a step, the Tier 1 files come first: they pay off with no code.

1. **The title screen:** background, logo, the button face and its hover face (4, Tier 1).
2. **The morning briefing** sheet (1).
3. **The office:** the scanner (Blender) and the travellers (the character brief's batches, its own track).
4. **At the desk:** the wheel icons (6), the paper face (1), the speech bubble and its tail (2), all Tier 1.
5. **The PC:** the frame, close X and power button (3); the culture wallpapers (8; the first one shows from day 2 at the earliest); the desktop icons (6), all Tier 1.
6. **The end of the shift:** the citation slip and the shift ledger (2).
7. **Home:** the background and the two panels (3); the upgrade icons (8); the slot machine and lever (2); the family (6), all Tier 1; the outcome symbols (5) and the slot and sleep panels (2), Tier 2.
8. **The endings:** the six illustrations (6, Tier 1) and the panel (1, Tier 2).
9. **Polish:** the document faces (10 kinds and the agency's), the seal and the photo frame (2), the book covers (6), the ink marks (2), all Tier 1; the UI kit (9) and the cursors (2 Tier 1 and 2 Tier 2).

## Decisions (Saleh, 2026-09-25: "yes to all")

1. **Title background:** redrawn in the cel style, showing the 3D office (from a render the art side makes).
2. **Logo:** the lettered "TIME SORTER" wordmark, the one image allowed to carry lettering.
3. **Title buttons:** text-free 9-slice faces with the game's labels on (one button look everywhere).
4. **Home background and ending illustrations:** redrawn in the cel style.
5. **Desktop icons:** superseded by the PC redesign's DK1: six icons, greyscale and tinted by the culture (rule 5), each a glyph over its label; the player places them.
6. **Paper faces:** one face per document kind (passport, permit), with the photo frame; drawn from the faces' own brief after piece 10.
7. **Reference book covers:** shown on the book's tile and at the top of its window.
8. **Verdict ink mark:** a text-free tick or cross mark lands on the papers after the verdict.
9. **Translator icons:** four, one per region (speech).
10. **Family:** the Partner and the Kid are drawn in the character style, three condition bands each.
11. **Slot machine:** "yes" did not pick between the two options here, so Claude chose a **landscape machine** drawn for the 560 × 360 panel: the Home screen's 800 × 600 layout has no room for a taller panel. (The art hooks stand it on the panel's top edge in a 375 × 240 box instead of behind the panel's white texts, which would not read on it.)

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
| `icon_power`, `tray_day`, `tray_credits`, `tray_stability` | `UI/Desktop/` | The Start menu and the tray print text. (`icon_settings` came back as one of the six desktop icons.) |
| `icon_lexicon`, `icon_dialect`, `icon_material` | `UI/Desktop/` | Their placeholder apps are retired (the PC redesign DK7). |
| `scanner_backing`, `page_blank`, `refbook_page`, `claim_banner`, `compare_bar_neutral`/`_match`/`_mismatch`/`_deviation`, `citizen_records_frame`, `deviation_report_form`, `directives_sticky` | `UI/Investigation/` | Code-drawn themed windows and strips; the scanned copy reuses the paper face. |
| `btn_accept`, `btn_deny` (normal, hover, pressed) | `UI/Investigation/` | Baked words. The decision buttons are themed per culture with fixed code glyphs (piece 6). |
| `stamp_approved`, `stamp_denied`, `stamp_citation` | `UI/Investigation/`, `UI/DayFlow/` | Baked words, which break the language switch; the text-free ink marks (section 4) replace them. |
| `agency_logo`, the nation seals | `UI/Investigation/` | Paper faces carry no emblems or seals. |
| Per-nation document kits (Aegyptus, Albion, Helios, Latia, Norvik, Solaris; 24 files then), and the 128 kit files now on disk | `UI/Documents/<Country>/<Ancient/Old/Modern/Future>/` | The invented nations are gone; the game has six eras, not these four; the stocks bake text and field lines; the paper face is text-free with a code layout (section 4). |
| Visitor trios, 240 × 440 (72 files), and the timeline cast | – | Characters are layered 1024 × 1536 figures (the character brief). |
| 21 invented legendaries (42 files) | – | The ten premades, real people drawn whole, replace them (the brief, section 11). |
| `temporal_times_masthead`, `shift_ledger_header` | `UI/DayFlow/` | The game prints the mastheads. |
| `btn_start_shift`, `btn_go_home`, `btn_sleep` | `UI/DayFlow/`, `Home/` | Baked words; the kit's button replaces them. |
| Era icons (`era_rome`, `era_medieval`, `era_future` then; `era_ancient`, `_old`, `_modern`, `_future` on disk) | `UI/Icons/` | No screen shows eras as icons, and the game has six eras. |
| Attribute icons (7 then; 6 on disk) | `UI/Icons/` | No screen shows attributes as icons; the game has three (democracy, science, art). |
| `icon_money`, `icon_stability`, `icon_day`, `icon_warning` | `UI/Icons/` | The office readouts are the art side's 3D displays; the tray prints text. |
| The `ArtLibrary` loader | – | Replaced by the by-name art slots (plan phase 27: `ArtSlots`, `SlotArt`, `Assets/Art/UI/Resources/`), the character art's convention; each builder gives its images their slots. |

## Delivery checklist

- File names exactly as listed; sizes as listed; transparent unless the item says opaque.
- No text, letters or numbers in any image, except the logo's "TIME SORTER" wordmark.
- Tier 1, a file that exists: overwrite it in place and keep its `.meta`.
- Tier 1, a by-name slot with no file yet: put the PNG at its path under `Assets/Art/UI/Resources/` with the exact name; nothing else.
- Tier 2: save the original in `ArtDeliverables/TimeDesk/UI/Raw/`; Claude wires it.
- Keep every ChatGPT original in `ArtDeliverables/TimeDesk/UI/Raw/`.
- Commit each PNG with its `.meta`.
