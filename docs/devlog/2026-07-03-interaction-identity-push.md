# Devlog — 2026-07-03 — Interaction & Identity push

Everything shipped today across three waves, plus the roadmap items we
explicitly parked. Mirrored to Notion (NOPE — Code Documentation).

## Wave 1 — Office UI fixes

Start menu overlap fixed; desktop icon grid pinned top-left and in bounds;
directives moved from an always-open note into a sticky-note window with an
icon; morning briefing / shift report moved out of the PC onto an office
overlay canvas styled as newsletters (THE TEMPORAL TIMES / SHIFT LEDGER);
scene builder made authoritative (re-applies geometry each run).

## Wave 2 — Evidence-gated denials + audit

Scanner became the Deviation Report: comparing evidence auto-registers true
contradictions; denying a forger without documented evidence = citation +
salary deduction (`requireEvidenceToDeny`); directive violations exempt; only
provable forgeries generated. Full code audit: match-proof modality added
(match against a foreign era's entry proves origin), window chrome made real
on all windows, `TimeDesk.Domain` assembly extracted, EditMode tests revived
(quarantine emptied), manifesto + feature inventory + audit report written.

## Wave 3 — Interaction & identity (this push)

- **Compare bar honesty**: when evidence registers, the bar flips to a red
  "● DEVIATION LOGGED — <summary>" verdict (an origin-proof no longer reads
  as a green MATCH). Text auto-sizes; window bodies wrap and clip.
- **Intercom (interaction system v1)**: per-case action list issued to the
  traveller — "Request Travel Passport", "Request Transit Permit". Documents
  no longer sit in the icon grid; they arrive when requested. The action list
  is data-driven, ready for interrogation actions.
- **Clean desk rule**: every new case closes all open windows.
- **Scanned documents**: passport/permit render as white scanned pages with a
  photo placeholder on a dark scanner backing — visually distinct from OS
  windows.
- **Identity**: passport now carries Full Name + Date of Birth. Visitors get
  era-plausible birth dates; forgeries can now alter the birth date.
- **Citizen Records app**: type a name → the agency's record (name, born,
  origin, clerk note). Name/Born rows are compare-clickable; a forged birth
  date against the record registers a RECORD-MISMATCH deviation. Registry is
  rebuilt per day from the day's visitors; clerk notes are the future
  easter-egg hook.
- **Tests**: +11 domain tests (record-proof decision table, registry lookup).
- **Docs**: `TESTING_STRATEGY.md` (multi-tier plan), feature inventory update.

## Parked (explicitly, so nothing is lost)

1. **Pin system** — player pins windows to survive the new-case clean desk.
2. **Photo compare** — passport photo vs a captured photo of the traveller
   uploaded to the system.
3. **Missing/corrupted records** — sometimes the record is absent (corruption
   or "funny reasons"); the player verifies identity indirectly via family
   history (father/sister records). Records app already returns misses;
   name forging stays OFF until this lands so lookups always work.
4. **More intercom actions** — interrogation questions, requests beyond the
   two documents.
5. **Easter eggs in citizen records** — clerk-note field is the hook.
6. **Asmdef migration phase 2** — move `ShiftScoring`/`CaseFactory` into the
   domain so the scoring decision matrix gets Tier-1 tests; unlocks Tier-2
   scene/builder smoke tests.
7. **CI** — game-ci workflow once Unity license secrets are added.
