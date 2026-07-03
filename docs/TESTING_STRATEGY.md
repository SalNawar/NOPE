# NOPE — Multi-Tier Testing Strategy

Goal: every push proves three things — **code is clean**, **code stays
maintainable**, **no feature regressed** — plus a performance budget so the
game stays optimized. Tiers are ordered by speed; a push must pass every tier
that exists for the code it touches.

## Tier 0 — Static & compile gates (seconds)

- Zero compile errors, zero *new* warnings in `Assembly-CSharp*` and
  `TimeDesk.*` assemblies.
- Manifesto conformance at review time: rule logic in the domain assembly,
  tunables in ScriptableObjects, no static mutable state in rule code, no
  `??`/`?.` on UnityEngine.Object.
- `FEATURES.md` updated in the same commit as any behaviour change.

## Tier 1 — Domain unit tests (EditMode, < 1 min) — LIVE

- Assembly: `TimeDeskEditMode` → tests `TimeDesk.Domain` (pure C#, no scenes).
- Style: decision tables, not happy paths — every rule branch has a test
  (see `DiscrepancyLogTests`: 3 proof modalities × junk rejections × dedupe).
- Current suites: `DiscrepancyLogTests`, `CitizenRegistryTests`,
  `ReadyGateTests`, `ShiftLedgerTests`.
- Run: Unity Test Runner (EditMode) or UnitySkills `test_run`.
- Rule: new domain code ships with its decision table in the same push.

## Tier 2 — Integration & scene tests (PlayMode, minutes) — NEXT

Blocked on finishing the asmdef migration (gameplay code still lives in the
predefined Assembly-CSharp, which test assemblies cannot reference).
Planned coverage once `TimeDesk.Gameplay` + `TimeDesk.EditorTools` exist:

- **Builder smoke**: run `OfficeSceneUIBuilder.Build()` on a blank scene;
  assert every controller reference is wired non-null (catches the class of
  bug where a rebuilt window loses its wiring).
- **Day-loop smoke**: scripted play through one day (start shift → READY →
  request documents → decide × N → shift report) using simulated clicks.
- **Scoring integration**: `ShiftScoring` decision matrix against real
  `GameConfigSO` assets.

## Tier 3 — Feature-contract tests (per release)

- `FEATURES.md` is the contract; each line gains a `(tested: X)` tag as
  coverage arrives. A release checklist walks every untagged line manually.
- A PR that deletes/alters a listed feature must say so in its description —
  reviewers diff the feature file first.

## Tier 4 — Performance budgets (nightly / on demand)

- Package: `com.unity.test-framework.performance` (already resolved).
- Budgets to encode as performance tests once Tier 2 lands:
  - Case generation: ≤ 2 ms and ≤ 16 KB GC alloc per case.
  - Compare/registration: zero GC alloc per click after warm-up.
  - Office scene: ≥ 60 fps on the dev machine, no per-frame allocations in
    `Update` paths (currently only `OfficeViewController.Update` runs per frame).
- Until then: Profiler spot-checks per push on the office scene; any new
  `Update`/per-frame allocation needs a written justification.

## CI (GitHub Actions) — SETUP PENDING

- game-ci `unity-test-runner` on `workflow_dispatch` first, then `pull_request`
  once stable: EditMode + (later) PlayMode on every PR.
- Requires repo secrets `UNITY_LICENSE` / `UNITY_EMAIL` / `UNITY_PASSWORD`
  (personal license activation via game-ci docs) — owner action.
- Local gate meanwhile: run Tier 1 via Test Runner before every push.

## Cadence summary

| When | What runs |
|---|---|
| Every compile | Tier 0 |
| Every push | Tier 1 (+ Tier 0), FEATURES.md diff |
| Every PR | Tier 1 in CI (once secrets land), reviewer manifesto check |
| Every release | Tier 3 walkthrough |
| Nightly / on demand | Tier 4 budgets |
