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
| **The scanner** | `Scanner`: nothing yet. The gameplay layer shows a stand-in flatbed (0.40 × 0.32 m) at the default pose (1.02, 1.06, −0.46), with a code-drawn feeder tray and analysis lamp on it while the Auto-Feed and Analysis Scanner upgrades are owned (the PC redesign SC6). | **missing**: an `Anchor_Scanner` with the model's renderers under it, about 0.40 × 0.32 m, square to the desk, a flat top, nothing sticking out of its footprint (UI_ART_RULES, "3D office props"); with it, the two upgrades' parts (a sheet feeder tray, a lamp bar), Tier 2, text-free |
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
| The anime hall's departure board: its display stays blank; the game draws the day's portal rows on it (the portals spec v3 BD1-BD5) | `DepartureBoard`: `GameplayAnchors/Anchor_DepartureBoard` over the hall's `16 Departure board blank display` | done: keep the display text-free |
| The anime hall's five portal rings: the game dims a ring under maintenance and draws the glow and the Return Gate's spiral inside it, under the bay, the ring and the glass (orders: the bay's − 1; VX1-VX6) | the layers `38`-`47` and `53`-`57` (`DeskConfigSO.hallPortalLayers`) | done; keep each ring's centre free of other layers at the bay's order − 1 |

## 2. The 2D layers over the office

Drawn on the office overlay canvas (1920 × 1080 reference; it scales with the screen).

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| PC frame bezel | Clicking the PC opens a front-facing monitor left of centre, 1240 × 1060 at 1080p; the desktop shows through its 4:3 glass | `Assets/Art/Office/Placeholder/pc_frame.png` | builder placeholder, 620 × 530 (the builder keeps an existing file: checked 2026-09-26) | **1860 × 1590**, the glass a transparent hole at x 90–1770, y 90–1350 from the top left; best rendered from the Blender CRT (UI_ART_RULES, "The PC close-up frame") | 1 | placeholder |
| Close X | the frame's top-right corner, 64 × 64 | `Assets/Art/Office/Placeholder/pc_close.png` | builder placeholder, 48 × 48 | 96 × 96 | 1 | placeholder |
| Power button | the frame's chin, 64 × 64 | `Assets/Art/Office/Placeholder/crt_power.png` | builder placeholder, 28 × 28 | 96 × 96 | 1 | placeholder |
| Power LED | the frame's chin, 18 × 18 | `Assets/Art/Office/Placeholder/crt_led.png` | a white disc, 8 × 8, tinted on and off by the game | keep | 1 | done |
| Brand plate | the frame's chin | none: the game prints "CHRONODESK 2150" | – | keep that part of the chin blank | – | – |
| Speech bubble | superseded (run 7): the UI kit draws it (`Assets/Art/UI/Kit`, docs/UI_KIT.md); the slot is retired | - | - | - | - | retired |
| Speech bubble tail | superseded (run 7): the UI kit draws it (`Assets/Art/UI/Kit`, docs/UI_KIT.md); the slot is retired | - | - | - | - | retired |
| Wheel choices and the "< Back" centre | on an ellipse around the traveller, 240 × 44 and 150 × 44 | the UI kit's `ui_button.png` (section 3) | code-drawn themed buttons | from the kit | 2 | code-drawn, later |
| Desk tooltip | above a clicked prop (credits, day, stability, time), 360 × 60 | the UI kit's `tooltip.png` (section 3) | code-drawn yellow panel | from the kit | 2 | code-drawn, later |
| Portal glow | inside an open departure portal's ring in the anime hall (the portals spec v3 VX1), about 150 px across at 1080p for the front ring; tinted pale cyan and drawn as a glow (unlit) by the game, slowly turning | `Assets/Art/UI/Resources/Office/portal_glow.png` | a code-drawn radial glow (`PortalGlowPlaceholder`) | 512 × 512, a soft swirl of light on transparent, greyscale, clear at the rim | 1 | missing |
| Return Gate spiral | inside the Return Gate's ring (portal 03) while it is in service (VX5); tinted amber and drawn as a glow (unlit) | `Assets/Art/UI/Resources/Office/portal_return_glow.png` | a code-drawn inward spiral (`PortalGlowPlaceholder`) | the same format: an inward spiral | 1 | missing |

- The wheel's icons are in section 8.
- **Drawn by code, no art:** the fallback HUD (it shows only when the art office lacks a readout) and the floating hints.
- **Piece 10 (desk examination, being built now)** adds the claim tag (1100 × 64) and an office compare strip (1200 × 56) at the top of the overlay, the stamp tray (Accept and Deny above the 3D stamp), and moves the verdict line and the citation slip onto the overlay. All are code-drawn themed panels with no art slot planned; the citation slip's art (section 5) follows it there.

### The hall's swappable slots (2026-10-07; `ArtDeliverables/TimeDesk/HallSlots/HALL_SLOTS_ART_REQUEST.md`)

The anime hall's parts that change with the run (the leading culture, the Helix River's tier, the debt crisis's phase, today's special, the famous travellers let through): 15 slots, 104 variant files, each a painting on the hall's whole 2172 x 724 transparent canvas registered to the live warm-stone painting (`Completion/WarmStone/HallWarmStone.png`), delivered to `Assets/Art/UI/Resources/Hall/Slots/<slot>/<variant>.png`. A missing file keeps the hall as painted, so none is needed to play. Templates: `ArtDeliverables/TimeDesk/HallSlots/templates/`. Priorities: 1 culture (banners, the board plate, the door signs, the floor medallion: 56), 2 stability (anomalies, cracked glass: 6), 3 phase posters and exhibits (37), 4 event checkpoints and queue clutter (5). Not counted in the totals below (their own request).

## 3. The PC desktop

A 4:3 canvas of 1440 × 1080, seen in the PC frame (its glass is 1120 × 840 at 1080p) and cloned small onto the office PC's screen. The culture theme recolours and refonts everything here except the documents.

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| Neutral wallpaper | behind the desktop until a country leads history | `Assets/Art/Generated/xp_bliss.png` (via `Theme_neutral`) | 1920 × 1080; the 4:3 desktop shows its middle 1440 × 1080 | keep | 1 | done |
| Culture wallpapers × 8 | behind the desktop from the morning after a country leads | `Assets/Art/Culture/<id>/wallpaper.png` for `egypt`, `iraq`, `greece`, `italy`, `china`, `japan`, `britain`, `germany` (via `Theme_<id>`) | Generate World placeholders, 960 × 540 | **1440 × 1080**: ask ChatGPT for 1536 × 1024 and Claude crops the middle (UI_ART_RULES, "Wallpapers" and "The eight cultures") | 1 | placeholder |
| Desktop icons × 8 | the eight free-placed desktop icons (plan phase 17; Orders added 2026-09-29; Portals, a portal ring, the portals spec v3 PA1): a 72 × 72 glyph over its label in a 120 × 132 cell, tinted by the theme | `Assets/Art/UI/Resources/Desktop/icon_<id>.png` (ids below) | code-drawn placeholder glyphs (`DesktopIconPlaceholder`); the old interim `icon_internet`, `icon_notes` and `icon_settings` in `UI/Desktop/` (cream plates) are not used | 128 × 128, a bold greyscale glyph on transparent (it shows about 56 px tall at 1080p), no plate | 1 | placeholder |
| Orders branch glyphs × 4 | the Orders tree's band heads, 34 × 34 (a node without its own icon shows its band's glyph too, 48 × 48) | `Assets/Art/UI/Resources/Orders/branch_<id>.png` for `desk`, `interview`, `portals`, `contacts` | code-drawn placeholders (`DesktopIconPlaceholder.OrdersGlyphs`: a scanner, a speech bubble, a portal ring, two linked rings) | 128 × 128, a bold greyscale glyph on transparent, no plate | 1 | placeholder |
| Upgrade icons × 12 | at the left of each Orders node, 48 × 48 (moved from Home's shop, 2026-09-29) | `Assets/Art/UI/Resources/Orders/upgrade_<id>.png` (ids below) | 2 interim at 256 × 256, wired (`upgrade_adv_scanner`, `upgrade_diplo_contacts`, moved here with their metas); 10 missing (their band's glyph shows) | 256 × 256 | 1 | interim / missing |
| Cursors: arrow, hand | everywhere: the game's cursor, the hand over anything clickable | `Assets/Art/UI/Desktop/cursor_arrow.png`, `cursor_hand.png` (found by name, set in `InteractionFeedback_Default`) | interim 32 × 32 | 32 × 32; the tip (arrow) and the fingertip (hand) are the click point: the stored points are (3, 2) and (13, 3), so Claude re-measures them when new art lands | 1 | interim |
| Cursors: grab, grabbing | over a desk paper and while dragging it | `cursor_grab.png`, `cursor_grabbing.png` | none (the hand shows) | 32 × 32 | 2 | missing, later |
| UI kit × 9 | every window, button, bar and menu (table below) | `Assets/Art/UI/Desktop/` | code-drawn themed panels | greyscale 9-slice pieces | 2 | later |

**Desktop icon ids** (the PC redesign DK1; `DesktopAppIds`): `investigation`, `portals` (a portal ring on its base), `internet`, `mail`, `citizen_account`, `orders` (a parcel or a crate with a tick), `notes`, `settings`, and nothing else.

**Orders upgrade ids (12).** `scanner_autofeed` (Auto-Feed Scanner), `adv_scanner` (Analysis Scanner), `interview_protocols` (Interview Protocols), the four Speech translators `tr_near_east_spoken`, `tr_mediterranean_spoken`, `tr_east_asia_spoken`, `tr_north_europe_spoken`, `diplo_contacts` (Diplomatic Contacts), and the portal repairs `repair_portal_02`, `repair_return_gate`, `repair_portal_04`, `repair_portal_05`. The state badges (padlock, clock, tick) are code-drawn and need no art. The old tiles' interim glyphs (`directives`, `scanner`, `citizen_records`, `cluelog`, the books) move to the Investigation app's tab glyphs with the app (plan phase 16); `lexicon`, `dialect` and `material` are retired with their placeholder apps. The slot is wired (plan phase 27): each icon shows its file when it exists, else the placeholder glyph for its id.

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
| Reference book covers × 6 | at the top of the book's register page in the Investigation app's Reference tab, right of its title (the tab's book chips can show them too: `SlotArt.CoverFor`) | `Assets/Art/UI/Resources/Investigation/refbook_cover_<id>.png` for `currency`, `language`, `technology`, `capital`, `ruler`, `culture` | 3 interim (400 × 560, English titles baked), wired; 3 missing (the page shows no cover) | 400 × 560, a closed cover with a simple motif, no title | 1 | interim / missing |
| Verdict ink marks | on every paper as it leaves after the verdict, in its stamp area (fitted at its own aspect) | `Assets/Art/UI/Resources/Forms/stamp_accept.png`, `stamp_deny.png` | none | 400 × 200, transparent, a text-free tick mark and cross mark in their own ink colours | 1 | missing |

- **The desk stamp prop** is the Blender prop (section 1): a click slides the stamp bar out ("The desk" below). The PC has no Accept or Deny since the PC clean-up.

### Travel documents (desk-first, 2026-10-05; `docs/superpowers/specs/2026-10-05-travel-documents-design.md`)

The papers now look like real travel documents, each its own shape (the aspect is width over height; the faces above are re-sized to it): the **passport** TC-101 (an open booklet, 0.66: the data page above the spine, the visa page below), the **entry ticket** TC-230 (a landscape stub, 1.15), the **waiver** TC-310 (a long legal sheet, 0.56), the **work permit** TC-520 (a letterhead, 0.765), the **travel permit** TC-610 (a folded square card, 1.0) and the **transponder card** TC-240 (a plastic ID-1 card, 1.586, new). Until the art lands the code draws every stand-in: the cover's edge in the nation's colour, the spine, the crease, the chip, the rounded corners, the emblems and the stamps. Every face stays text-free and box-free (the code prints them).

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| Passport emblems × 8 | at the passport header's left (about 50 px at 1080p held) and as the visa page's large faint watermark, tinted by the nation's cover colour | `Assets/Art/UI/Resources/Forms/emblem_<emblem>.png`: `emblem_wingedsun` (Egypt), `emblem_octastar` (Iraq), `emblem_laurel` (Greece), `emblem_star` (Italy), `emblem_fivestars` (China), `emblem_chrysanthemum` (Japan), `emblem_crown` (Britain), `emblem_eagle` (Germany) | code-drawn geometric stand-ins (`EmblemShapes`) | 512 × 512, **white on transparent** (one ink; the game tints it), a 2150 national emblem built on the motif named, no letters | 1 | code-drawn |
| Passport page per nation × 8 | the passport's paper (data page and visa page) for a holder of that nation; tried before `paper_tc101` | `Assets/Art/UI/Resources/Forms/paper_tc101_<nation>.png`: `egypt`, `iraq`, `greece`, `italy`, `china`, `japan`, `britain`, `germany` | the ivory tint | 1024 × 1552 (0.66): security paper with a fine guilloche in the nation's tint, the emblem as a faint repeat pattern; **no cover edge and no spine** (the code draws them in the cover colour), no boxes | 1 | missing |
| Transponder card face | the transponder card TC-240 | `Assets/Art/UI/Resources/Forms/paper_tc240.png` | the pearl tint, rounded corners and a sheen band drawn by code | 1024 × 646 (1.586), pale pearl plastic with a holographic sheen down the left, corners rounded at 3.5 % of the width and transparent outside them; no chip (the code draws it) | 1 | missing |
| The six documents' faces at their new shapes | replaces the per-kind face sizes above for `paper_tc101` (1024 × 1552), `paper_tc230` (1024 × 890, ticket card stock, a torn-off stub edge at the left), `paper_tc310` (1024 × 1829, long legal paper), `paper_tc520` (1024 × 1339, letterhead stock with an engraved band at the head and foot), `paper_tc610` (1024 × 1024, card stock with a soft fold down the middle) | as the per-kind faces | as the per-kind faces | as listed; the code keeps drawing the bands, the crease and the perforation over them | 1 | missing |
| APPROVED and DENIED stamps | the stamp's mark: on the passport's visa page (and each other paper's stamp area) where the stamp is pressed | `Assets/Art/UI/Resources/Forms/stamp_accept.png`, `stamp_deny.png` (the same files as the verdict ink marks above, now the stamps' impressions) | code-drawn: a double frame and the word APPROVED (green) or DENIED (red), tilted | 560 × 200, transparent, a rubber-stamp impression with the word APPROVED (green ink) or DENIED (red ink) in a double frame, slightly worn | 1 | code-drawn |
| Closed passport covers × 8 | (a later step: the closed booklet before it is opened; not drawn yet) | `Assets/Art/UI/Resources/Forms/passport_cover_<nation>.png` | none | 704 × 1000, the closed booklet in the nation's cover colour (`world_source.json` `countries[].passport.cover`), the emblem in gold foil, PASSPORT in English and the nation's script | 2 | future |
### The Canva documents (run 7, 2026-10-07: Saleh approved, "yes, do all documents in this style")

Saleh designed the eight traveller documents in Canva (exported at 2000-2400 px, final). Unlike the faces above they carry their **labels, frames, crests and glyph bands** (English, like every paper): the game prints only the values, the photo, the seal, the stamps and a few prints (the travel documents spec, TD6). The shipped faces are processed copies of the originals (at most 2048 px): the sample portraits painted out of the photo windows, the labels of art rows the paper has no field for and the labels the game prints itself painted out, round corners clear, and for the passport an open booklet (a visa page made from the data page's paper above the data page, a cover margin the code paints in the holder's nation's colour). Each has a blank face (every field label painted out) the code patches a field's label from until the field is introduced. The places of each value are measured on the art into the template's `FormLook.art` (shares of the face): a new export must keep them, or be re-measured.

| Item | Where it shows | File | Status |
|---|---|---|---|
| Passport TC-101 (booklet 0.65) | the passport on the desk and on the PC | `Forms/paper_tc101.png`, `blank_tc101.png` | Saleh's art, done |
| Entry Ticket TC-230 (1.29) | the ticket | `Forms/paper_tc230.png`, `blank_tc230.png` | done (four rows relabelled: Transponder, Currency Carried, Declared Effects, Transponder Class) |
| Transponder Card TC-240 (1.75) | the card | `Forms/paper_tc240.png`, `blank_tc240.png` | done (HOLDER and VALID UNTIL painted out: no such field; TRAVEL CLASS relabelled Transponder Class) |
| Stranding Waiver TC-310 (0.75) | the waiver | `Forms/paper_tc310.png`, `blank_tc310.png` | done (two rows relabelled: Transponder, Debt Passed to Kin; DATE painted out; the art's three conditions replace the template's printed paragraphs) |
| Work Permit TC-520 (0.77) | the permit | `Forms/paper_tc520.png`, `blank_tc520.png` | done (DEBT CREDITED painted out; the photo sits right of AUTHORISED BY: the art has no window) |
| Travel Permit TC-610 (1.08) | the travel permit | `Forms/paper_tc610.png`, `blank_tc610.png` | done (DESTINATION relabelled Date of Birth; the glyph band painted out by Saleh, none drawn) |
| Intake Declaration TC-620 (0.75) | the declaration | `Forms/paper_tc620.png`, `blank_tc620.png` | done (RETURNING FROM (ERA) painted out; the seal in the checkboxes' empty half) |
| Return Order TC-630 (0.77) | the order | `Forms/paper_tc630.png`, `blank_tc630.png` | done (CURRENT ERA relabelled Return To, RETURN GATE relabelled Incident; ORDER No. prints the serial) |
| Photo laminate | over the photo on these papers | `Forms/photo_holo.png` | code-generated, done |

**ID photos (2150 dress).** A paper's photo shows the holder in 2150 civilian dress (`Looks.PhotoLook`). Requested from GPT (the character remake), no files yet: `Characters/outfit_{m|f}_civil_2150_v1` (tidy: tourists), `_v2` (labourer), `_v3` (worn: the displaced); `Characters/hair_{m|f}_civil_2150_{colour}` (each hair colour); `Characters/premade_{id}_photo` (a premade's ID photo on the character canvas). Until they land `CharacterArtFallback` draws today's plainest 2150 outfit and the neutral 2150 hair, and a premade's neutral picture cropped to the head and neck; the delivered files take over with no code change.

The proofs of means (TC-415 to 417) keep the code-drawn forms; the rows above for `paper_tc101` ... `paper_tc610` and the transponder card face are superseded.

### The desk (desk-first, 2026-10-05: the city view, the stamps, the rulebook)

Saleh 2026-10-05: the player can look left at the 2150 city (item 6) and the day's rules lie on the desk (item 11). Since Papers, Please's controls (2026-10-06) the stamps are a bar sliding out over the desk from its right, with the 3D stamps back on it (Saleh, same day: "I want the 3D stamp"); the ink pad is gone (Papers, Please's stamps have no ink). The code draws every stand-in until the art lands.

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| **City view painting** | the whole screen when the player looks left (A / the "◀ City" button): after the hall turns toward its window wall (the art's left pan), the whole panorama fades in, fitted inside the screen (Saleh 2026-10-07) | the hall's living city, `Assets/Art/Office/AnimeHallLayers/Completion/City/TimeWeather/` (`City{Morning,Noon,Evening,Night}{Clear,Rain}.png`, `CityDepth.png`, `CityAtmosphereAtlas.png`, drawn by `LivingLeft.mat` / "NOPE/Hall Living City"; `CityView` draws a copy of the windows' material unmasked) | the art's eight 2172 × 724 time and weather paintings with their moving atmosphere, blended at the hall's hour and rain (ChatGPT's hall-art branch, merged 2026-10-07) | the art side owns them (see `README_CLAUDE_HANDOVER.md`) | 1 | done |
| **Stamp bar** | Papers, Please's stamp bar with the 3D stamps (2026-10-06): slides out over the desk from its right (the grey tab on the screen's edge, TAB); a rail in front of the DENIED and APPROVED stamps, their words on the rail's top | `Assets/Art/Office/DeskClean/Models/` (a dedicated `Clean_StampBar.fbx` and `Clean_StampApproved.fbx` / `Clean_StampDenied.fbx` would replace the builder's; not wired yet: `BuildStampTray` builds the rack) | the art's desk stamp `Clean_Stamp` at half size in the DeskClean materials with a code-placed red / green cap and words; the rail and arms are primitives in DeskClean green-dark, wood and brass | two hand stamps about 8 × 5 cm, 8 cm tall (the word APPROVED / DENIED on the block's front and reversed on the rubber, a green / red knob), and a rail about 28 cm long with brackets, in the DeskClean palette and the hall's clean anime shading; the grey tab stays code-drawn | 1 | code-built |
| **Inspect button** | the red button at the screen's bottom right (a magnifying glass over SPACE), lit with a yellow ring in inspect mode | `Assets/Art/UI/Resources/Desk/inspect_button.png`, `inspect_button_on.png` (not wired yet) | code-drawn: a red square with a white magnifying glass (`MagnifierGlyph`) over SPACE | 112 × 112 px, a red enamel desk button with a white magnifying glass, room under it for the key the game prints; the lit state with a yellow glow | 2 | code-drawn |
| Rulebook booklet | the booklet left of the mat (draggable), two tabs on its top edge (RULES, PAPERS) | `Assets/Art/UI/Resources/Forms/paper_rulebook.png` (not wired yet) | a cream quad and two tab quads, the code prints the rows | 1024 × 1180 (the page) and two tabs, a laminated agency rule booklet, text-free | 2 | code-drawn |

- **Drawn by code, no art:** the reference book pages, Citizen Records, the Deviation Report, the Directives sticky note and the transcript. All are windows, so they take the UI kit.

## 5. The day flow

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| Morning briefing sheet | superseded (run 7): the UI kit draws it (`Assets/Art/UI/Kit`, docs/UI_KIT.md); the slot is retired | - | - | - | - | retired |
| Shift ledger sheet | superseded (run 7): the UI kit draws it (`Assets/Art/UI/Kit`, docs/UI_KIT.md); the slot is retired | - | - | - | - | retired |
| The Citation (TC-900) | after a wrong verdict it flies onto the desk and stays there (replaced the citation slip pop-up, 2026-10-07) | `Assets/Art/UI/Resources/Forms/paper_tc900.png` (`CitationForm_TC900` holds its places) | Saleh's Canva red carbon ticket (1200 × 2545), processed: the row numbers painted out (the game numbers rows across continuation sheets) and a worn dark navy CITED stamp added across the rows | done | 1 | done |

- The two sheets' "Start shift" and "Go home" buttons take the UI kit's button.
- **Drawn by code, no art:** the verdict line and the idle line.

## 6. Home

`Assets/Scenes/HomeScene.unity`. The Tier 1 art here is wired in the scene by hand (the Home builder makes only the panels), so a file replaced in place keeps working.

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| Home background | behind every Home panel, full screen | `Assets/Art/Home/home_bg.png` (`Canvas/ArtBackground`) | interim 1920 × 1080, painted, warm lamp light | 1920 × 1080, opaque, redrawn in the cel style | 1 | interim |
| Expenses panel | the bills step: the fixed costs, the pet's needs and the five bills' rows, 1000 × 880 | `Assets/Art/Home/panel_expenses.png` (`ExpensesPanel`) | interim 800 × 1000, stretched to 900 × 780 | 1520 × 1200, a text-free paper panel | 1 | interim |
| House panel (the old shop panel) | the House tree (the Home upgrades spec §6), 1840 × 920 | `Assets/Art/Home/panel_shop.png` (`ShopPanel`) | interim 800 × 1000, stretched | 1840 × 920 or larger at that aspect, a text-free paper panel | 1 | interim |
| Slot panel | the slot machine's panel, 560 × 360 | `Assets/Art/Home/panel_slot.png` | code-drawn (dark, with white text: a light panel needs the texts recoloured, so there is no slot yet) | 1120 × 720 | 2 | code-drawn |
| Sleep panel | "Turn in", 560 × 320 | `Assets/Art/Home/panel_sleep.png` | code-drawn (as the slot panel) | 1120 × 640 | 2 | code-drawn |
| Slot machine | replaced by the cel UI kit (2026-10-07: docs/UI_KIT.md; the slot panel's `slot_reel`, `dome_red_*` and `slot_lever`, the Title's `plate_ox` / `plate_slate`) | — | — | — | — | retired |
| Slot lever | replaced by the cel UI kit (2026-10-07: docs/UI_KIT.md; the slot panel's `slot_reel`, `dome_red_*` and `slot_lever`, the Title's `plate_ox` / `plate_slate`) | — | — | — | — | retired |
| Slot outcome symbols × 5 | the result of a spin | `Assets/Art/Home/slot_<outcome id>.png` for `small_win`, `jackpot_cash`, `busted_machine`, `forgery_warning`, `legendary_omen` | 5 interim at 256 × 256, not wired (a spin shows text only); two still carry older names (`slot_jackpot`, `slot_busted`) | 256 × 256 | 2 | interim |
| House upgrade icons × 18 | at the left of each House card, 56 × 56 | `Assets/Art/UI/Resources/Home/upgrade_<id>.png` (ids below) | none yet (the cards are text only); the office's upgrades moved to the PC's Orders app (section 3) | 256 × 256 | 1 | missing |
| Pet × 8 | the pet's corner (left half of the 1180 × 800 panel) and the Title's adoption preview: a dog and a cat, four states each (the Home pet spec PS7) | `Assets/Art/UI/Resources/Home/pet_<kind>_<state>.png`: kinds `dog`, `cat`; states `idle`, `happy`, `sad`, `sick` | code-drawn stand-in (`PetStandIn`) | 1024 × 1024, transparent, the same pose and scale in a kind's four states (the brief: `docs/PET_ART_REQUEST.md`) | 1 | missing |
| Pet corner | behind the pet in its corner | `Assets/Art/UI/Resources/Home/pet_corner.png` | a plain cream plate | 1100 × 1100, opaque, the middle third plain | 1 | missing |
| Toys × 4 (pictures) | the pet's corner | `Assets/Art/UI/Resources/Home/toy_<toy id>.png` for `toy_ball`, `toy_rope`, `toy_feather`, `toy_squeaky` | none (the rows are text) | 256 × 256 | 1 | missing |
| Toys × 4 (Orders icons) and the Toys band glyph | the Orders tree's Toys band | `Assets/Art/UI/Resources/Orders/upgrade_<toy id>.png`, `Orders/branch_toys.png` | the band's code-drawn ball | 256 × 256 and 128 × 128, white on transparent | 1 | missing |

**House upgrade ids (18).** `house_rations_b`, `house_veg_box`, `house_hen_share`, `house_draught_seals`, `house_insulation`, `house_floor_up`, `house_corner_flat`, `house_second_lock`, `house_strongbox`, `house_alarm`, `house_air_filter`, `house_water_purifier`, `house_medicine_cabinet`, `house_clinic`, `house_plant`, `house_photo`, `house_radio`, `house_better_bed`.

- **Drawn by code, no art:** the HUD line and the rows' text. The buttons take the UI kit's button.

## 7. Title and endings

`Assets/Scenes/TitleScene.unity`. The Tier 1 art is wired in the scene by hand.

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| Title background | the first screen, full screen | `Assets/Art/Title/title_bg.png` (`Canvas/ArtBackground`) | interim 1920 × 1080, painted (an older office, warm light) | 1920 × 1080, opaque, redrawn in the cel style showing the 3D office (from a render the art side makes) | 1 | interim |
| Logo | the title block's top, on its navy plate (2026-09-30: the interim logo is switched off and the name is printed, cream on navy, because the logo's dark teal ink was drawn for a light ground and read at 1.6:1 over the painting; Build Title UI turns `TitlePanel/ArtLogo` off) | `Assets/Art/Title/logo_time_sorter.png` (`TitlePanel/ArtLogo`, keeps its aspect) | interim 1200 × 400, "TIME SORTER" lettered in dark ink for a light ground (off) | 1800 × 600, the lettered wordmark "TIME SORTER" (the one place lettering is allowed) in the cel style | 1 | interim |
| Title button face | replaced by the cel UI kit (2026-10-07: docs/UI_KIT.md; the slot panel's `slot_reel`, `dome_red_*` and `slot_lever`, the Title's `plate_ox` / `plate_slate`) | — | — | — | — | retired |
| Ending panel | the run's ending, 760 × 520 | `Assets/Art/Title/ending_panel.png` | interim 1200 × 800, not wired (the panel's white text needs a dark panel) | 1520 × 1040, text-free | 2 | interim |
| Ending illustrations × 2 | full screen behind the ending panel (`EndingSO.picture`) | `Assets/Art/Title/ending_<id>.png` for `fired`, `bankrupt` (the day-15 ending, `world_report`, has no picture: the Title's background stays behind the world's outcomes; `retirement`, `scientific_age`, `democracy_triumphant` and `artistic_golden_age` are unused since those endings were retired on 2026-09-29) | interim 1600 × 900, painted, wired | 1920 × 1080, opaque, redrawn in the cel style; replace in place | 1 | interim |

## 8. Icons

| Item | Where it shows | File | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| Wheel icons × 6 | at the left of each traveller-wheel choice, 28 px at 1080p, on a dark blue button | `Assets/Art/UI/Resources/WheelIcons/wheel_<kind>.png` for `request` (a sheet of paper or an open hand), `question` (a speech balloon), `look` (an eye), `dialog` (two balloons), `back` (an arrow pointing left), `normal` (a small dot) | white glyphs of the same shapes drawn at run time, 32 × 32, never on disk; the folder does not exist yet (a file dropped there shows: checked 2026-09-26) | 64 × 64, white on transparent (the importer makes it a sprite) | 1 | missing |

- The desktop icons, the Orders branch glyphs and the upgrade icons are in section 3; the house upgrade icons' slot in section 6.
- **Every icon:** a bold, simple silhouette that reads at 24 px; no letters, question marks or exclamation marks; a white or grey glyph on transparent (the game may tint it); no plate behind it unless the item says so.

## Characters (pointer)

About 880 files (50 bases, 759 garments, 31 Future outfits, 40 premade expressions) from about 406 ChatGPT images, at `Assets/Art/Characters/Resources/Characters/<key>.png`, 1024 × 1536. Tier 1: the game loads each by key at run time. Status: **50 files in** (2026-09-30): the pilot's 42 keys (batch 1: both base figures, the Athens man and woman, the Mamluk woman's outfit, braids and hood, the tripartite wig, the magatama) and 8 interim heads (skins 2-5, face a, recoloured from the pilot's skin-1 heads until Batch 2 draws its own). Everything else is in the character brief and `coverage.json`.

**Travellers use only the ChatGPT art** (Saleh, 2026-09-30). A key with no art yet is drawn with its nearest key that has art, by the table in `Assets/Resources/CharacterArtFallback.asset` (`CharacterArtFallbackSO`; the order is in `CHARACTER_ART_CONTRACT.md` section 9): the same nation in the nearest era, then the same era of a neighbouring nation, then (outfit, hair, beard, headwear) the nearest place that has one; an accessory or a hair-back with no art is simply not drawn. The procedural placeholder figure is retired. So every new file replaces a stand-in the moment it lands, with no code change.

A premade (a character drawn whole) shows its picture once its neutral image is in the folder; until then the game draws it as a layered traveller in its claimed place's costume (from the same ChatGPT layers and stand-ins), the same face at every appearance (days 7-15 B4).

### Coverage today (what each place's travellers are drawn with)

`**own**` = the place's own ChatGPT art; a place name = the stand-in drawn instead (hair and beards in the traveller's baked colour); `none` = not drawn (no stand-in); blank = the place has no item there. Bodies: all 10 are ChatGPT art. Heads: face a in all five skins (skin 1 drawn, 2-5 interim recolours); faces b, c and d show face a of the same skin. Premades: no whole picture yet; each is drawn as the layered stand-in of its claim (Socrates, Aspasia and Pell wear their own Athens art). The per-key table is regenerated from the game's own fallback code (`LookArtFallback`) with each art drop.

| Place | ♂ outfit | ♂ hair | ♂ beard | ♂ headwear | ♂ accessory | ♀ outfit | ♀ hair | ♀ headwear | ♀ accessory |
|---|---|---|---|---|---|---|---|---|---|
| egypt ancient | greece ancient | none |  |  | greece ancient | egypt medieval | **own** | egypt medieval | none |
| egypt medieval | greece ancient | greece ancient | greece ancient | greece ancient | none | **own** | **own** | **own** | none |
| egypt earlymodern | greece ancient | greece ancient | greece ancient | greece ancient | none | egypt medieval | egypt medieval | egypt medieval | none |
| egypt industrial | greece ancient | greece ancient | greece ancient | greece ancient | none | egypt medieval | egypt medieval | egypt medieval | none |
| egypt modern | greece ancient | greece ancient | greece ancient |  | none | egypt medieval | egypt medieval | egypt medieval | none |
| iraq ancient | greece ancient | greece ancient | greece ancient | greece ancient | greece ancient | greece ancient | greece ancient | greece ancient |  |
| iraq medieval | greece ancient | greece ancient | greece ancient | greece ancient | none | egypt medieval | egypt medieval | egypt medieval | none |
| iraq earlymodern | greece ancient | greece ancient | greece ancient | greece ancient |  | egypt medieval | egypt medieval | egypt medieval | none |
| iraq industrial | greece ancient | greece ancient | greece ancient | greece ancient |  | egypt medieval | egypt medieval | egypt medieval | none |
| iraq modern | greece ancient | greece ancient | greece ancient | greece ancient | none | egypt medieval | egypt medieval | egypt medieval | none |
| greece ancient | **own** | **own** | **own** | **own** | **own** | **own** | **own** | **own** |  |
| greece medieval | greece ancient | greece ancient | greece ancient | greece ancient | greece ancient | greece ancient | greece ancient | greece ancient | none |
| greece earlymodern | greece ancient | greece ancient | greece ancient | greece ancient | greece ancient | greece ancient | greece ancient | greece ancient | none |
| greece industrial | greece ancient | greece ancient | greece ancient | greece ancient | greece ancient | greece ancient | greece ancient | greece ancient | none |
| greece modern | greece ancient | greece ancient | greece ancient |  | greece ancient | greece ancient | greece ancient |  | none |
| italy ancient | greece ancient | greece ancient |  |  |  | greece ancient | greece ancient |  | none |
| italy medieval | greece ancient | greece ancient |  | greece ancient |  | egypt medieval | egypt medieval | egypt medieval | none |
| italy earlymodern | greece ancient | greece ancient |  | greece ancient |  | egypt medieval | egypt medieval | egypt medieval | none |
| italy industrial | greece ancient | greece ancient | greece ancient | greece ancient | none | egypt medieval | egypt medieval |  | none |
| italy modern | greece ancient | greece ancient |  |  | none | egypt medieval | egypt medieval |  | none |
| china ancient | greece ancient | greece ancient | greece ancient | greece ancient |  | greece ancient | greece ancient |  | none |
| china medieval | greece ancient | greece ancient | greece ancient | greece ancient | none | egypt medieval | egypt medieval | egypt medieval |  |
| china earlymodern | greece ancient | greece ancient | greece ancient | greece ancient | none | egypt medieval | egypt medieval | egypt medieval |  |
| china industrial | greece ancient | greece ancient |  | greece ancient |  | egypt medieval | egypt medieval | egypt medieval | none |
| china modern | greece ancient | greece ancient |  | greece ancient | none | egypt medieval | egypt medieval | egypt medieval | none |
| japan ancient | greece ancient | greece ancient |  |  | **own** | greece ancient | greece ancient |  | none |
| japan medieval | greece ancient | greece ancient | greece ancient | greece ancient |  | egypt medieval | egypt medieval | egypt medieval |  |
| japan earlymodern | greece ancient | greece ancient | greece ancient |  |  | egypt medieval | egypt medieval | egypt medieval |  |
| japan industrial | greece ancient | greece ancient | greece ancient | greece ancient |  | egypt medieval | egypt medieval |  |  |
| japan modern | greece ancient | greece ancient |  |  | japan ancient | egypt medieval | egypt medieval |  | none |
| britain ancient | greece ancient | greece ancient | greece ancient |  | none | greece ancient | greece ancient |  | none |
| britain medieval | greece ancient | greece ancient | greece ancient |  | none | egypt medieval | egypt medieval | egypt medieval |  |
| britain earlymodern | greece ancient | greece ancient | greece ancient | greece ancient |  | egypt medieval | egypt medieval | egypt medieval | none |
| britain industrial | greece ancient | greece ancient | greece ancient | greece ancient | none | egypt medieval | egypt medieval | egypt medieval | none |
| britain modern | greece ancient | greece ancient |  | greece ancient | none | egypt medieval | egypt medieval | egypt medieval | none |
| germany ancient | greece ancient | greece ancient | greece ancient |  |  | greece ancient | greece ancient |  | none |
| germany medieval | greece ancient | greece ancient |  | greece ancient |  | egypt medieval | egypt medieval | egypt medieval |  |
| germany earlymodern | greece ancient | greece ancient | greece ancient | greece ancient | none | egypt medieval | egypt medieval | egypt medieval | none |
| germany industrial | greece ancient | greece ancient | greece ancient | greece ancient | none | egypt medieval | egypt medieval |  | none |
| germany modern | greece ancient | greece ancient | greece ancient | greece ancient | none | egypt medieval | egypt medieval | egypt medieval | none |
| egypt future | greece ancient | greece ancient | greece ancient |  |  | egypt medieval | egypt medieval |  |  |
| iraq future | greece ancient | greece ancient | greece ancient |  |  | egypt medieval | egypt medieval |  |  |
| greece future | greece ancient | greece ancient | greece ancient |  |  | greece ancient | egypt medieval |  |  |
| italy future | greece ancient | greece ancient | greece ancient |  |  | egypt medieval | egypt medieval |  |  |
| china future | greece ancient | greece ancient | greece ancient |  |  | egypt medieval | egypt medieval |  |  |
| japan future | greece ancient | greece ancient | greece ancient |  |  | egypt medieval | egypt medieval |  |  |
| britain future | greece ancient | greece ancient | greece ancient |  |  | egypt medieval | egypt medieval |  |  |
| germany future | greece ancient | greece ancient | greece ancient |  |  | egypt medieval | egypt medieval |  |  |
| present (2150) | greece ancient | greece ancient |  |  |  | egypt medieval | egypt medieval |  |  |

### Commission next (the most visible gaps first)

1. **Batch 2 base figures:** `base_{m,f}_skin[2-5]_facea` and the faces b, c, d (`head_{g}_skin{N}_face{b,c,d}`): every traveller shows a head, and today all ages share face a and skins 2-5 are recolours.
2. **Day 1 (ancient Egypt, Iraq, Greece, Italy):** the men's Egyptian kilt, bobbed wig (drawn bald today) and wesekh collar; the women's linen sheath dress and lotus fillet; Iraq's and Italy's ancient outfits, hair and headwear for both genders (both draw the Athens art today).
3. **Day 2 (adds medieval, China, Britain):** a man's medieval outfit and turban (every man's hat is the petasos today), the women's medieval outfits of Iraq, Italy, China and Britain (they wear the Mamluk qamis and hood today), China's and Britain's ancient looks.
4. **Days 3-6 and the premades' places:** early-modern, industrial and modern outfits for both genders (every modern traveller wears an ancient or medieval stand-in), then the accessories (none but the Athens pouch and the magatama exist, so most travellers show none), the Future outfits and the present's kit.
5. **The premades' whole pictures** (`premade_<id>_<expression>`, the table below): each replaces its layered stand-in at once.

### Premades of days 7-15 (by-name slots)

Sixteen new whole-figure characters, four expressions each (`neutral`, `happy`, `angry`, `worried`): 64 files, Tier 1 drop-in at `Assets/Art/Characters/Resources/Characters/premade_<id>_<expression>.png`. Each is drawn once, in the costume of the one place they claim; the character brief's batch 9 has the prompt. The six story characters come first (they stand in the second week's beats); the ten famous are pooled from day 7.

| Character (`id`) | Place and look | Files | Now | Deliver | Tier | Status |
|---|---|---|---|---|---|---|
| `pell`: Pell Quimby (2150 story character, days 7, 10, 15) | Periclean Athens, 430 BCE: a cheerful woman of 26 in a cheap tourist's copy of an Athenian chiton and himation, a straw sun hat, a battered transponder on her wrist | `premade_pell_neutral.png`, `_happy`, `_angry`, `_worried` | a generated look in the place's costume | 1024 × 1536, whole figure | 1 | missing |
| `ines`: Ines Varga (2150 story character, day 8) | Victorian Britain, 1843: a tired woman of 51 in a Victorian mill worker's dress and apron, a Temporal Customs lanyard tucked into her collar | `premade_ines_neutral.png`, `_happy`, `_angry`, `_worried` | a generated look in the place's costume | 1024 × 1536, whole figure | 1 | missing |
| `rook`: Rook Danner (2150 story character, day 11) | Victorian Britain, 1843: a grinning man of 33 in a too-clean Victorian workman's jacket and flat cap | `premade_rook_neutral.png`, `_happy`, `_angry`, `_worried` | a generated look in the place's costume | 1024 × 1536, whole figure | 1 | missing |
| `ada`: "Ada Lovelace" (a 2150 enthusiast posing as her, day 12) | Victorian Britain, 1843: a woman of about 28 in a slightly-too-new Victorian day dress and bonnet, a little too pleased with it | `premade_ada_neutral.png`, `_happy`, `_angry`, `_worried` | a generated look in the place's costume | 1024 × 1536, whole figure | 1 | missing |
| `hollis`: Marek Hollis (2150 story character, day 13) | Wilhelmine Germany, 1899: a gaunt man of 32 in a Wilhelmine collier's jacket and cap | `premade_hollis_neutral.png`, `_happy`, `_angry`, `_worried` | a generated look in the place's costume | 1024 × 1536, whole figure | 1 | missing |
| `auditor`: Quill Ferreira, the Directorate's auditor (day 14) | Republican Rome, 50 BCE: a sharp man of 48 in a Roman toga worn over very good 2150 shoes | `premade_auditor_neutral.png`, `_happy`, `_angry`, `_worried` | a generated look in the place's costume | 1024 × 1536, whole figure | 1 | missing |
| `tahtawi`: Rifa'a al-Tahtawi (famous, pooled) | Khedivate of Egypt, 1869: an Egyptian scholar of 67 in a turban and a scholar's robe over a kaftan | `premade_tahtawi_neutral.png`, `_happy`, `_angry`, `_worried` | a generated look in the place's costume | 1024 × 1536, whole figure | 1 | missing |
| `kulthum`: Umm Kulthum (famous, pooled) | Nasser's Egypt, 1962: an Egyptian singer of 58 in an elegant long evening dress, dark glasses, a handkerchief in her hand | `premade_kulthum_neutral.png`, `_happy`, `_angry`, `_worried` | a generated look in the place's costume | 1024 × 1536, whole figure | 1 | missing |
| `malaika`: Nazik al-Mala'ika (famous, pooled) | Kingdom of Iraq, 1956: an Iraqi poet of 33 in a 1950s skirt suit, a book under her arm | `premade_malaika_neutral.png`, `_happy`, `_angry`, `_worried` | a generated look in the place's costume | 1024 × 1536, whole figure | 1 | missing |
| `sayyab`: Badr Shakir al-Sayyab (famous, pooled) | Kingdom of Iraq, 1956: a slight Iraqi poet of 29 in a 1950s suit, a notebook in his hand | `premade_sayyab_neutral.png`, `_happy`, `_angry`, `_worried` | a generated look in the place's costume | 1024 × 1536, whole figure | 1 | missing |
| `elytis`: Odysseas Elytis (famous, pooled) | Metapolitefsi Athens, 1975: a Greek poet of 63 in a jacket and an open-collared shirt, glasses | `premade_elytis_neutral.png`, `_happy`, `_angry`, `_worried` | a generated look in the place's costume | 1024 × 1536, whole figure | 1 | missing |
| `fellini`: Federico Fellini (famous, pooled) | Dolce Vita Rome, 1960: an Italian film director of 40 in a dark suit and a hat, a scarf | `premade_fellini_neutral.png`, `_happy`, `_angry`, `_worried` | a generated look in the place's costume | 1024 × 1536, whole figure | 1 | missing |
| `hezehui`: He Zehui (famous, pooled) | Beijing, People's Republic, 1972: a Chinese physicist of 58 in a plain Zhongshan-style jacket, short hair, glasses | `premade_hezehui_neutral.png`, `_happy`, `_angry`, `_worried` | a generated look in the place's costume | 1024 × 1536, whole figure | 1 | missing |
| `toyoda`: Sakichi Toyoda (famous, pooled) | Meiji Nagoya, 1899: a Japanese inventor of 32 in a Meiji kimono with a work apron over it | `premade_toyoda_neutral.png`, `_happy`, `_angry`, `_worried` | a generated look in the place's costume | 1024 × 1536, whole figure | 1 | missing |
| `turing`: Alan Turing (famous, pooled) | Post-war Britain, 1950: a British mathematician of 38 in a tweed jacket and a crooked tie, a runner's build | `premade_turing_neutral.png`, `_happy`, `_angry`, `_worried` | a generated look in the place's costume | 1024 × 1536, whole figure | 1 | missing |
| `meitner`: Lise Meitner (famous, pooled) | Weimar Berlin, 1926: a physicist of 48 in a dark 1920s dress and jacket, her hair in a bun | `premade_meitner_neutral.png`, `_happy`, `_angry`, `_worried` | a generated look in the place's costume | 1024 × 1536, whole figure | 1 | missing |


## Totals

New 2D files (the Blender scanner and the characters are counted apart):

| Section | Tier 1 | Tier 2 | Files |
|---|---|---|---|
| 1. The office (Blender) | – | – | the scanner model (Tier 3) |
| 2. 2D layers over the office | 7 | 0 | 7 |
| 3. PC desktop | 34 | 11 | 45 |
| 4. Documents | 39 | 8 | 47 |
| 5. Day flow | 3 | 0 | 3 |
| 6. Home | 23 | 7 | 30 |
| 7. Title and endings | 10 | 1 | 11 |
| 8. Icons | 6 | 0 | 6 |
| **Total** | **122** | **27** | **149** |

- Every decision was answered yes, so all are wanted. The Orders app (2026-09-29) added the Orders icon, four branch glyphs and four portal-repair icons, and moved the eight upgrade icons from Home to it (109); the portals (the portals spec v3) added the Portals icon, the portal glow and the Return Gate's spiral (112). The count moved from 107 to 100 with the redesign: six desktop icons instead of seventeen, one Title face (normal and hover) instead of four baked buttons, eight upgrade icons, and ten per-kind faces, the plain agency face and the seal instead of the passport and permit faces.
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
7. **Home:** the background and the two panels (3); the upgrade icons (8); the slot machine and lever (2); the pet, its corner and its toys (18, `docs/PET_ART_REQUEST.md`), all Tier 1; the outcome symbols (5) and the slot and sleep panels (2), Tier 2.
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
10. **Family:** retired with the Home pet spec (2026-10-05: "pet only for now"); the pet replaces it (section 6).
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
