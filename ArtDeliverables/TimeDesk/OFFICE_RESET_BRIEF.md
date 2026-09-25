# Office art reset — requirements and composition

Status: composition brief only. Previous generated booth mockups are rejected as layout authority. No new generation or Unity testing in this reset pass. Pending automatic desk application cancelled.

## Authority
- User constraints: seated clerk POV, deep physical desk, lighthearted anime 1990s/early-2000s style, original XP-inspired OS, no religious imagery, no new character art, no identifiable next case/queue, no testing by Codex. Side partitions must not extend to the distant rear wall. No arbitrary corner trays, books, cards or furnishings.
- docs/ART_ASSET_LIST.md: Tier 1 office sprites and Tier 2 extras; preserve GUIDs, clean separate layers, blank dynamic text fields, common top-left lighting.
- docs/superpowers/specs/2026-06-21-office-scene-two-states-design.md: Office Focus and Monitor Focus, CRT and Ready controls, live readouts and reactive poster.
- Assets/Editor/OfficeSceneUIBuilder.cs BuildBooth/BuildReadouts/BuildReactiveProp and Assets/Scripts/GameManager.cs: current wiring takes precedence over outdated feature checkboxes.
- docs/superpowers/specs/2026-07-03-scanner-evidence-design.md: Scanner app is the discrepancy report; do not invent physical scanning mechanics.

## Required objects and proposed locations
- Rear customs hall: visually subordinate, separated from booth by visible floor/space. No domestic garden-window composition. No visible next traveller.
- Current traveller slot: central opening across counter; empty before READY, no character generation in art task.
- Two short freestanding privacy partitions: near desk sides, visible terminal edges at visitor end; angled in same perspective, never continuous walls reaching rear backdrop.
- Left partition: separate Day calendar above separate reactive agency poster. Both retain dynamic behavior and follow mounting plane.
- Right partition: separate stability device with blank numeric screen, neutral casing compatible with current whole-sprite tint. Do not invent a second functional display just to fill space.
- Desk: continuous surface across foreground with credible top depth and front edge. Uncluttered center, not a miniature distant desk. No giant mat required by source; any surface inset must remain subordinate.
- READY/NEXT: clear center-reach standing placard, separate clickable silhouette, unobstructed. Preserve current next-case gate and focus binding.
- CRT: substantial right-rear desk footprint, tube extends right, screen faces left toward clerk; below/right of traveller sightline. Separate sprite, existing click and camera target preserved.
- Credits till: left desk working zone, separate from READY; number readable and blank in art. Existing live credits behavior preserved.

## Deferred extras
Physical intercom and document slot are Tier 2 extras, not verified physical interactions. Exclude from core composition until their hooks are intentionally implemented. The PC currently hosts intercom requests and scanned-document investigation. No physical reference books, loose cards, ornamental trays, lamps, stationery, mug, plant or stamp in core pass. Optional brief-listed decor can be considered later without blocking controls.

## Production contract
1. Set one 16:9 camera/crop, horizon and perspective guide before producing assets; all tabletop footprints and partition geometry share it.
2. Establish object bounds in a simple composition layout with only the required objects. Proposed placement above is design judgment, not a claim that source prescribes exact dimensions.
3. Produce matching layers: rear hall, left partition, right partition, desk, CRT, READY, calendar, stability device, till, reactive poster. Keep dynamic props out of background; preserve live TMP numbers and sprite swaps.
4. Judge the assembled composition, never a partial background, when communicating what the final scene will look like.
5. Integrate with existing bindings; report separately whether files were produced, scene was saved, and user testing is pending. Do not run Play mode, control Unity or test.

## Latest user revisions — supersede earlier decoration exclusions
- Booth should be cluttered and lived-in, with pinned notes, newspaper, pictures and memorabilia, while controls and visitor sightline remain clear.
- Humor: dry, absurd time-travel bureaucracy; remove hearts, cute mascots and inspirational/cutesy slogans. Use keepsakes and pictures from different eras.
- All text, agency names and logos are dynamic/localizable and must remain separate from painted art. Leave blank faces on departure board, banners, signs, posters, notes, labels and newspaper text areas. Departure board can display actual daily destinations when wired by gameplay code.
- Center control runtime label is NEXT only; no baked lettering.
- CRT art has a blank screen. Desktop rendered by game.
- Clock is DIGITAL (latest user instruction overrides proposed analog face/hands). Separate casing and blank display, runtime shift-time digits.
- Neutral diffuse base colors and simple textures. No baked directional sunlight, time-of-day tint or long shadows. Lighting changes morning to evening in Unity.
- Current work is mockup exploration only. No new Unity integration or testing authorized by the mockup request.

## Hybrid production supersedes earlier flat-scene approach
See ../TimeDesk/HybridScene/SCENE_CONTRACT.md for the current layer/model/animation separation and source-code integration notes. Image mockups are concept references, not 3D assets. Megabuildings must be locked independently; cars must never be baked into buildings or hall. No removed installers are to be recreated.

## Amendment (piece 7, 2026-09-25)
Source: docs/superpowers/specs/2026-09-25-physical-desk-design.md (the physical desk and the traveller wheel).
- Supersedes line 10 ("do not invent physical scanning mechanics") and the deferred extras' "Physical intercom and document slot ... The PC currently hosts intercom requests and scanned-document investigation": the desk has physical papers the player drags, a working scanner tray (a paper dropped on its glass bed opens its scanned copy on the PC) and an intercom speaker that opens the traveller wheel.
- Supersedes the READY/NEXT line's "focus binding": READY only calls the next traveller; clicking the CRT focuses the PC.
- The CRT needs a power button and an LED on its bezel, and a blank glass whose inscribed 4:3 rectangle the game fills with the live desktop.
