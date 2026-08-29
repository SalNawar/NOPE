# NOPE — Main Branch Merge Criteria

*Established 2026-08-29 by Saleh (review lead). Derived from the Engineering
Manifesto (`docs/ENGINEERING_MANIFESTO.md`, Notion: "Engineering Manifesto —
Decoupled, Reusable, Configurable") and the Testing Strategy. **Nothing merges
to `main` unless every Gate below passes.** Gates are pass/fail; Quality Bars
are scored and can justify rejection when materially violated.*

`main` is the playable branch. A broken `main` blocks everyone; the burden of
proof is on the PR, not the reviewer.

## Gates (hard pass/fail)

| # | Gate | What is checked |
|---|------|-----------------|
| G1 | **Builds & boots** | Project compiles with no errors; game boots to Title and a day can be played end-to-end. No missing scripts/`meta` files, no broken scene references. |
| G2 | **Tests pass & gate the merge** | Full EditMode suite green (pre-existing unrelated failures must be called out). New rule logic ships with tests that encode the decision table, not just the happy path. |
| G3 | **Feature complete vs. its spec** | The PR implements its approved spec/design doc. Half-done features stay on the branch. Placeholder shells are fine only when the spec says so. |
| G4 | **No silent contract changes** | `docs/FEATURES.md` updated in the same PR for any added/removed/altered behaviour. |
| G5 | **Mergeable & current** | No conflicts with `main`; branch is rebased/merged up to a recent `main`. PRs based on stale history that clobber newer settings are rejected until refreshed. |
| G6 | **Asset hygiene** | Every new asset has its `.meta`; binaries (PNG etc.) tracked per `.gitattributes` (LFS); no stray generated churn (`.sln`/`.csproj`/ProjectSettings) unless intentional and explained. |

## Quality Bars (scored 1–5, reject if any ≤2 without written waiver)

- **Configurability** — designer-tunable numbers live in ScriptableObjects /
  serialized fields, never code constants. A magic number in a rule path is a
  defect even when correct.
- **Modularity / decoupling** — rules in `TimeDesk.Domain` (headless,
  unit-testable, no UnityEngine scene deps); MonoBehaviours orchestrate and
  render, they do not decide. Typed boundaries (structs/events/interfaces);
  no static mutable state in rule code; null-safe optional wiring (missing
  panel skips, never throws).
- **Scalability** — content added as data (SOs registered in ContentLibrary),
  not hard-coded branches; mechanisms generic enough for the next feature
  (reuse named at review time before a new mechanism is added).
- **Optimization** — no per-frame allocations or Find/GetComponent in hot
  paths; scene/asset diffs proportionate to the change; editor tooling stays
  idempotent so scenes converge, not accumulate.
- **Code quality** — matches surrounding style; fails loudly in the log,
  never in the frame; warnings state the fix; no dead code or commented-out
  blocks; commit messages describe the change honestly.

## Intent review (mandatory second pass)

Every PR gets an intent audit against the systems that already exist, before
the verdict is final. Two questions, answered with code evidence:

1. **Does it redo or override existing code/decisions?** If yes: why, was it
   necessary, and is it demonstrably better? Overriding a deliberate
   configuration or replacing a working mechanism without a named, argued
   reason is grounds for rejection regardless of gate results.
2. **Does it re-code something the existing architecture already supports
   without code?** Before any new mechanism lands, the review verifies the
   no-code path (authoring assets/config on existing systems) and the
   minimal-extension path (small change inside the owning system) were
   genuinely insufficient. A parallel mechanism that half-duplicates an
   existing one — two sources of truth that can disagree — is a bug that
   hasn't happened yet, and is rejected even when well-built.

The burden of proof is on the PR: the plan/spec must name the existing
mechanism that was tried and why it didn't fit (manifesto, Law 2).

## Process

1. Reviewer records verdict (ACCEPT / ACCEPT WITH CONDITIONS / REJECT) in
   `docs/reviews/` with per-gate results and per-bar scores.
2. REJECT feedback must be actionable: file, line, what to change, which
   gate/bar it restores.
3. Stacked PRs are reviewed base-first; a stacked PR cannot merge before its
   base.
4. Verification claims in the PR body are re-run, not trusted.
