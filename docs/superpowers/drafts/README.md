# Parked drafts

Saved 2026-09-25 so nothing lives only in a temp folder. These were REVIEWED DRAFTS,
not built features; each became a real spec under `docs/superpowers/specs/` when its
piece was built (the build re-checked it against the code first). Pieces 4 to 6 have
since been built (`2026-09-24-characters-design.md`, `2026-09-24-history-facts-design.md`,
`2026-09-24-ui-reacts-design.md`), and the office move put the game in the art side's 3D
office (`2026-09-25-office-move-design.md`), so the drafts below are the reviewed record,
except `art/`, which is still the working brief.

- `specs/` — design specs for roadmap pieces 4 (characters), 5 (history changes facts)
  and 6 (the PC/UI reacts to history). Each was reviewed by two independent lenses.
- `decisions/` — the decision records behind them (made by Claude under Saleh's
  "go with all pieces, don't stop", open to his review), including the art-sync plan
  (`artsync_decisions.md`: adopting Codex's hybrid 3D office, since done by the office move).
- `art/` — the ChatGPT character/UI art brief, v2.1 (revised 2026-09-25 for the 3D office:
  the travellers are flat 2D figures standing at the desk), the ready-to-paste ChatGPT
  message, the UI art rules, `coverage.json` (every required art file), the figure guide,
  and the scripts that rebuild them. Piece 4 is built on it: the game's wardrobes in
  `world_source.json` mirror `tools/wardrobe_v2.json`, and its 880 art keys are
  `coverage.json`'s. Generated (edit the scripts, never the outputs; a second run changes
  nothing): `CHARACTER_ART_BRIEF_v2.md` and `coverage.json` by `tools/build_v2.py`;
  `tools/wardrobe_v2.json` and `tools/confusable_candidates.txt` by `tools/data_v2.py`
  (run it first); the guide PNG by `tools/make_guide_v2.py`. `tools/verify_coverage.py`
  checks a delivery. Hand-written: `CHATGPT_MESSAGE.md` and `UI_ART_RULES.md`.
- `piece6-support/` — the contrast checker, Arabic shaper and draft culture/string
  tables used to validate the piece-6 spec.
