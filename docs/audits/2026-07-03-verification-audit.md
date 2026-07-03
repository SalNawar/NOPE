# Verification-Logic Audit — 2026-07-03

Scope: the full accept/deny verification chain — `DiscrepancyLog`,
`CompareController`, `InvestigationUIController`, `ShiftScoring`,
`CaseFactory`, `GameManager`, `TravelRuleSO`/`DayPlanSO`, window chrome.
Trigger: player reports penalties despite (subjectively) correct decisions;
screenshot showing a MATCH-proof comparison that registered nothing.

## Critical bugs (fixed)

1. **Match-proof comparisons registered nothing.** The scanner only accepted
   *mismatch against the claimed era's entry*. Matching a forged value against
   the entry it actually belongs to (Aqueduct == Latia/Rome while claiming
   Norvik/Medieval) showed MATCH and registered nothing — so a player who
   correctly proved foreign provenance still got an "unproven denial" citation.
   → `DiscrepancyLog` now registers both proof modalities (mismatch proof and
   origin/match proof). Covered by `DiscrepancyLogTests`.
2. **Unprovable forgeries.** `CaseFactory` could forge a field whose truth is
   not authored in any reference book — the lie was undetectable and every deny
   became "unproven". → Forging now only targets provable fields (book must
   contain the claim's truth + an alternative value); logs a content warning
   otherwise. (Fixed earlier this session, recorded here.)
3. **Window chrome dead on document/reference windows.** Their min/max/X were
   decorative panels (no Button, no OSWindowChrome) from an early build.
   → All windows now share one `BuildWinControls` (real buttons + chrome).
4. **Builder idempotency traps.** `MakeButton`/`Text` returned early on a
   same-named object of the wrong type and then *fell through to create a
   duplicate sibling*; `Panel` never re-applied geometry, so scenes kept stale
   layouts forever. → Same-named wrong-type leftovers are replaced; `Panel`
   re-applies geometry every build (authoritative builder).

## Spaghetti / design-debt (addressed)

- **Static mutable state in rule code:** `ShiftScoring._lib` was set per call
  and read by helpers — removed; the library is passed explicitly.
- **Domain logic trapped in Assembly-CSharp:** untestable (this is exactly why
  the old test suite was quarantined). → New `TimeDesk.Domain` assembly holds
  `DiscrepancyLog`, `ReadyGate`, `ShiftLedger`, `DocumentField`,
  `ClueCategory`; `CompareEvidence` now uses string ids instead of
  ScriptableObject references. Test assembly references it.

## Not bugs (worth knowing)

- "Penalty even though the visitor was lying" is the **evidence-gate by
  design**: a deny needs at least one *registered* discrepancy. With the
  match-proof fix, both intuitive proof styles now count.
- Day 2+ adds directive rules; accepting a directive-violating (otherwise
  genuine) traveler is a citation — the directive window is easy to ignore.
  UX follow-up: surface active directives more prominently.
- Stability loss accompanies every citation, including free warnings.

## Known debt (deferred, tracked)

- `ShiftScoring`, `CaseFactory`, `WorldState` still live in Assembly-CSharp —
  the decision-table tests for scoring need them moved into the domain
  assembly (next extraction candidate).
- `ClickableTests` / `OfficeViewControllerTests` remain quarantined until
  `Clickable`/`OfficeViewController` move into an asmdef.
- `OfficeUIController` still carries the legacy era-pick UI path (dead when
  the investigation desk is wired).
- Reference books currently have one entry per nation+era; more variety would
  make match-proofs richer.
