# Piece 6 — The PC / UI reacts to history: decisions (made by Claude under Saleh's "go with all pieces dont stop", 2026-09-24)

Source analysis: SCRATCH/piece6_map.json (`synthesis.decisions` = U1..U15; `critique.extraDecisions` = Z1..Z9; handle or explicitly defer every `critique.missing` item). Stacked on pieces 2-5 (branch feat/ui-reacts); use their FINAL implemented names (read the code).

Saleh: "everything the bg of your pc the font color and style even approve and reject changes color and language everything".

- U1 = A. The present culture = piece 5's Future leader country id (plus the saved ordered ranking for later blends). Below the leader floor / before any leader, the look stays neutral (today's XP/English).
- U2 = C. The saved history snapshot is the source of truth; it reaches the scene through the existing cue pipeline (a "culture:<id>" UI-channel cue; TimelineCueReceiver subclasses do the visual work). Z2 = C: apply on scene load (sceneLoaded, before Start) so the briefing never flashes the neutral look.
- U3 = A. Evidence stays canonical Latin: document values, names, birth dates, OriginLabels, FactTable strings. Only chrome, labels and prose templates change. Field labels / document / book names may be keyed (display-only).
- U4 = C. Flavour tier: a curated key set (~30-40: ACCEPT/DENY, start, window titles, mastheads, tray labels, INTERCOM, SEARCH, START SHIFT/GO HOME, MATCH/MISMATCH, READY...) translated into the culture's language, with small English glosses on gameplay-critical controls; every UI string goes through keys so full translation later is data-only.
- U5 = A. Culture content authored in world_source.json (countries[].culture: locale, script, rtl, font candidates, palette roles, wallpaper path, labels) — the generator writes Theme/Effect/string-table assets; sprites by path convention with a warning when art is missing.
- U6 = A. The builder stamps ThemeRole / ThemeLabel components (Panel/Text/MakeButton) and becomes authoritative for text and buttons; the applier walks them. Theme only at scene load, never mid-comparison. Z4 = A: traveller documents, reference books and Records are diegetic → a "traveller document" role that PC theming skips.
- U7 = A. Small custom string lookup (culture → reading language → key, args, warning on miss) in TimeDesk.Visuals (engine-free, tested); Domain returns structured data, not sentences.
- U8 = A. Non-Latin fonts at runtime from installed OS fonts (TMP_FontAsset.CreateFontAsset(family, style)) with ordered per-script candidate lists (separate Chinese and Japanese chains), prewarm + cache; Greek uses LiberationSans; never download or copy fonts into the project. Missing font → fall back to English labels + warning.
- U9 = C (+A for runtime templates), subject to an editor spike: Arabic labels pre-shaped (pure presentation-forms shaper + RTL run handling in TimeDesk.Visuals, tested) at generate time; runtime templates reuse the shaper. No layout mirroring in v1. If the spike fails, fall back to U9-D for Arabic (palette/wallpaper/numerals only, Latin transliteration) and document it.
- U10 = C. Per-culture Accept/Deny and MATCH/MISMATCH colours, but positions never swap, each button keeps a shape glyph (✓/✗), and a tested contrast validator (text >= 4.5:1, large/UI >= 3:1) runs in the generator and fails loudly; the hover outline colour is part of the theme and validated.
- U11: The monitor desktop canvas + the newsletter overlay (briefing/ledger) + booth sprite props via the existing TimelineReactiveSprite (fix the blank-poster bug). Home scene only if the role helpers make it cheap; Title out.
- U12 = B. Settings toggle "UI language: follow history / always English" (per-player preference, not in the run save; hosted in the existing empty Settings window); colours/fonts/wallpaper still follow history.
- U13 = A. Placeholder text-free wallpapers per culture generated from the palette (like EnsureWallpaper), replaceable by real art later (add to the art list; text-free art is a hard rule).
- U14 = A. The theme changes only at the day boundary, announced by a briefing news line (piece 5 already announces leader changes — reuse, don't duplicate).
- U15 = B. Verify: read back colours/fonts/labels/HasCharacters in a GUI-mode play smoke per forced culture (debug force-culture/leader cheat), plus screenshots saved to the scratchpad; Domain/Visuals decision-table tests offline first.
- Z1 = A: pieces 5/6 own the argmax and cue emission (note for Saleh to tell Marwan re PR #3).
- Z3 = B-ish: themes tint/restyle UI chrome and swap wallpaper; booth art is not re-tinted (Codex's delivered art stays as is); the builder stops re-applying hard-coded tints only on themed UI elements.
- Z5 = B: old saves get the culture step idempotently on Continue.
- Z6 = B: western digits and 24h clock stay (no mixed digit systems next to canonical values).
- Z7 = A: endings stay attribute-total based; write down that the present culture and the ending can differ.
- Z8: sprite resolution by path convention, one mechanism (note it for the art pipeline).
- Wallet: relabel "Credits" with the present culture's Future currency name (from the resolved Future place fact, piece 5 H9).
- Also in piece 6 (moved here by earlier pieces): book-row "changed" markers for history edits (piece 5 H14) if cheap; per-traveller report lines in the shift ledger (piece 2 parked) if cheap — otherwise list as follow-ups.

## AMENDMENT A1 (Claude, 2026-09-24) — Codex's reskinned UI and hybrid office
After the art sync (SCRATCH/artsync_decisions.md), the desktop/newsletter UI carries Codex's sprite skins. Follow artsync D4 = C: text-free skins get a "skinned" ThemeRole that the applier tints white (keeps the art), baked-word sprites (Accept/Deny/Start) are replaced by builder labels over text-free backgrounds so colour + language follow history; the builder is existing-wins (never destroys/recreates skinned objects). The booth sign caption "NEXT" (Codex's NextLabel) is a localized flavour key (artsync D14). The booth's 3D world, lights and poster stay Codex's (artsync D13: ReactivePoster inactive — U11's booth-prop theming is deferred).
