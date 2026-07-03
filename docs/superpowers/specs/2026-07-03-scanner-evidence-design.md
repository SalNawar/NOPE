# Scanner Evidence System ("Deviation Report") — Design

Date: 2026-07-03 · Status: approved by Saleh

## Goal

Papers, Please-style evidence gating for denials. Spotting a lie is not enough —
the player must *document* it by scanning (comparing) the two things that prove
it. Denying a forger without documented evidence earns a citation and a salary
deduction, even though the visitor really was lying.

## Rules

1. **Registering evidence.** Comparing a document field against the
   reference-book entry of the *same category* that applies to the *claimed*
   nation+era registers a discrepancy IF the values differ AND the field is
   genuinely anachronistic (`DocumentField.isAnachronism`). One discrepancy per
   category per case. Junk comparisons (different categories, wrong-era book
   entries, non-forged mismatches) show MISMATCH as before but register nothing.
2. **Scanner window = Deviation Report.** The Scanner desktop app lists the
   registered discrepancies for the current case ("LANGUAGE INCORRECT — papers:
   X / expected: Y"). Cleared per case. Opens automatically on first find; the
   compare bar appends "DEVIATION LOGGED".
3. **Scoring.**
   - DENY forger, ≥1 discrepancy → correct (salary credit as today).
   - DENY forger, 0 discrepancies → **unproven denial**: citation + deduction
     via the existing citation ladder (free warnings apply), distinct slip text.
   - DENY directive-violator → always justified; no scanned evidence needed
     (the daily directive is the evidence). v1 scope decision.
   - DENY genuine traveler → wrong, as today (no valid evidence can exist).
   - ACCEPT → unchanged; timeline impacts still apply only on accept.
4. **Config.** `GameConfigSO.requireEvidenceToDeny` (default on). Penalties
   reuse `citationPenalties` + `freeWarningsPerDay`.

## Components

- `DiscrepancyLog` (new, pure C#): `TryRegister(evidenceA, evidenceB,
  claimedNation, claimedEra)` validates true contradictions; holds the
  per-case list. `CompareEvidence` struct carries typed row metadata
  (kind, category, value, isAnachronism / entry nation+era).
- `CompareController`: `Select` gains an evidence-carrying overload; raises
  `PairCompared(a, b)` when the second slot fills. Visual behavior unchanged.
- `DocumentWindowController` / `ReferenceBookWindowController`: pass typed
  evidence with each row click.
- `InvestigationUIController`: owns the log, tracks the current case, renders
  the Scanner window text, exposes `EvidenceCount` / `EvidenceSystemActive`.
- `ShiftScoring.ResolveDecision`: new `evidenceCount` parameter (−1 = system
  inactive, e.g. fallback UI → gate skipped); unproven-denial branch flips
  `correct` to false with citation text "Deviation denied without documented
  evidence."
- `CaseVerdict`: `evidenceCount`, `unprovenDenial`. `ShiftLedger`:
  `UnprovenDenialCount`. Shift report shows "Undocumented denials: N".
- `OfficeSceneUIBuilder`: Scanner window rebuilt as the Deviation Report and
  wired into `InvestigationUIController` (like the Directives window).

## Out of scope (follow-ups)

- Clickable directive rows to "prove" destination violations by scan.
- Highlighting registered discrepancies on the documents themselves.
