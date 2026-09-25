# Code audit and overhaul — approach, success criteria, guarantees

Requested by Saleh on 2026-09-25: after every roadmap piece is finished, audit the code
"as an expert Unity game developer and software engineer": review the repo class by class,
refactor spaghetti and redundant code, remove hard-coded values and garbage code, fix bugs,
rewrite for efficiency, eliminate dependencies, write tests and make sure the code passes
them. This document is the plan, written BEFORE execution.

**Runs after:** pieces 7 (physical desk), 8 (traveller wheel content: piece 7 ships the basic wheel), 4 (characters), 5 (history),
6 (UI reacts), 9 (translation) are merged to main. The ChatGPT/art work (art brief, adopting
Codex's hybrid office) stays last, after this overhaul, unless Saleh reorders.

## 1. Scope

- **In:** all project C#: `Assets/Scripts/**` (Domain, Visuals, Core, Timeline, Cases, UI,
  Office, Shift, DevTools...), `Assets/Editor/**` (builders, generator, validator),
  `Assets/Tests/**`, the asmdefs, `Packages/manifest.json`, `Assets/Data/World/world_source.json`
  and the generated content pipeline, `docs/FEATURES.md`.
- **Light touch:** Codex's scripts (e.g. `OfficeTrafficVehicle.cs`) — bugs and warnings only.
- **Out:** third-party code (`Packages/*`, TextMesh Pro, UnitySkills, the art asset packs such
  as Creepy_Cat / 80s_Office), art assets, Codex's hybrid scene.

## 2. Approach (phases)

### Phase 0 — Freeze and baseline (the safety net, before any change)
1. Branch `overhaul/audit` from main; tag `audit-baseline`.
2. **Inventory:** every class/struct/enum with assembly, file, lines, public surface, callers
   (a Roslyn-free script over the sources + the compiler's symbol output from `compile_check`).
3. **Metrics baseline** (scripted, repeatable): lines per class/method, cyclomatic complexity
   (the .NET SDK's built-in analyzers — no downloads), compiler warnings in our code, unused
   members (IDE0051/IDE0052), duplicate-code blocks (a token-window hash script), magic numbers
   and string literals in rule code, `Find*`/`GetComponent` calls in per-frame paths, LINQ and
   allocations in `Update`/`LateUpdate`, static mutable state, singleton access sites.
4. **Behaviour baseline — golden masters** (Unity automation, saved as files):
   - case generation dump for days 1-6 × 20 seeds (every traveller, papers, tells, answers,
     looks, premades, violators) — must stay byte-identical unless a change is an intended fix;
   - world check / validator / Generate World idempotency output;
   - semantic dumps of every built scene (Title, Home, Office);
   - scripted play-through transcripts (days 1-6: verdicts, evidence, ledger, news, saves);
   - a profiled play-through (frame time, GC alloc per frame, scene load time).
5. **Characterization tests:** where behaviour is only covered by automation today, add
   permanent tests first (see Phase 3 PlayMode suite), so refactors are pinned by tests.

### Phase 1 — Audit (read-only, class by class)
- Parallel expert reviewers, one per subsystem (Domain; Visuals; world/content pipeline;
  cases/lies/interview; timeline/history; shift/scoring/save; office/desk/camera; desktop UI;
  editor builders; tests). Each class gets a card: responsibility, smells, bugs, dead code,
  duplication, hard-coded values, coupling, performance, Unity pitfalls (fake-null `?.`/`??`
  on UnityEngine.Object, execution order, serialized-field fragility, `OnValidate` side
  effects, coroutine lifetime, event leaks), testability, and a proposed change with risk.
- Every finding is **adversarially verified** by two independent skeptics against the code;
  only confirmed findings enter the backlog. Findings are deduplicated across reviewers.
- Output: `docs/reviews/2026-xx-code-audit.md` (per-class cards + prioritized backlog).

### Phase 2 — Plan the refactor as small slices
Group the backlog into behaviour-preserving slices, each independently verifiable, ordered:
1. Garbage and dead code removal (unused members, unreachable branches, stale comments).
2. Bug fixes — each with a failing regression test first.
3. Hard-coded values → named knobs in ScriptableObjects (tunable) or named constants
   (structural), never literals in rule code.
4. Duplicated logic → one shared helper (zero re-implementation rule).
5. God-class decomposition (e.g. `GameManager`, `InvestigationUIController`,
   `OfficeSceneUIBuilder`, `CaseFactory`) into single-responsibility types.
6. Logic out of MonoBehaviours into pure Domain code with tests.
7. **Dependency reduction:** split `Assembly-CSharp` into asmdefs (e.g. `TimeDesk.Game`,
   `TimeDesk.UI`, `TimeDesk.Office`, `TimeDesk.Editor`) with one-way references so gameplay
   logic becomes testable and compile-isolated (script GUIDs stay, scenes keep references);
   replace singleton reach-ins (`RunManager.Instance`, `Find*`) with injected references;
   remove unused packages from `manifest.json` after proving nothing references them.
8. Performance: only measured hot paths (per-frame allocations, repeated lookups, LINQ in
   loops, string building in UI updates), each change backed by the profiled baseline.
The slice plan is written as a normal implementation plan (tests first, exact edits) and
reviewed before execution.

### Phase 3 — Tests
- A permanent **PlayMode test assembly** replacing today's temporary automation: day flow,
  READY/NEXT and closing, liar caught on papers and on answers, desk scanner → PC, PC
  focus/power, traveller wheel, save/continue, determinism across two seeds.
- EditMode: every Domain class has decision-table tests; each fixed bug has a regression test;
  editor tools (generator/validator/builder) get tests where they hold rules.

### Phase 4 — Execute slice by slice
For every slice: characterization/regression tests first → change → compile (0 errors, no new
warnings) → offline tests → golden-master diff (identical, or the diff is exactly the
documented intended fix) → commit. Every few slices: full Unity EditMode + PlayMode suite and
scene/builder idempotency. Each slice is reviewed by independent lenses with adversarial
verification before the next begins. Any red gate stops the line until fixed.

### Phase 5 — Final verification and merge
Full suites, all golden masters, builders and generator idempotent, validator clean, a
scripted play-through of days 1-6 with screenshots, the profiled play-through compared with
the baseline, and a final independent multi-lens review of the whole diff. Then a gate record
in `docs/reviews/`, fast-forward main, push.

## 3. What success means (measurable)
| Area | Criterion |
|---|---|
| Behaviour | Every `docs/FEATURES.md` item verified; golden masters identical to baseline except documented intended fixes |
| Tests | All EditMode + PlayMode tests pass (only the known third-party UnitySkills failure excepted); every Domain class tested; every fixed bug has a regression test; test count up vs baseline |
| Bugs | Every confirmed audit bug fixed or explicitly deferred with a reason |
| Garbage/dead code | Zero unused members/types in our assemblies (analyzer-clean), zero stale temporary code |
| Duplication | No duplicated logic blocks above the threshold (token-window script) |
| Hard-coding | No magic numbers/strings in rule code; tunables in SOs; paths/ids in one place |
| Structure | No class over ~400 lines or method over ~60 lines / complexity 15 without a written justification (builders may be split by area instead) |
| Dependencies | Gameplay code in asmdefs with one-way references; no singleton reach-ins in logic; no unused packages; no new third-party dependency |
| Warnings | Zero compiler warnings in our code |
| Performance | No regression; zero steady-state GC alloc per frame in the office view and desktop; scene load time not worse |
| Process | Every change reviewed + adversarially verified; each slice green before the next |

## 4. How it is guaranteed
- **Pinned behaviour before refactoring:** golden masters + characterization tests are
  captured first; a refactor that changes any output fails the gate.
- **Small reversible slices** on `overhaul/audit`, one commit per slice, baseline tag for
  rollback; nothing reaches main until Phase 5 passes.
- **Automated gates** (compile, offline tests, Unity suites, golden-master diff, idempotency,
  validator, metrics) run for every slice — not by eye.
- **Independent review:** separate reviewer agents per lens, each finding challenged by two
  skeptics; a final whole-diff review by agents that did not write the code.
- **Evidence in writing:** the audit report, slice plan, metrics before/after, and the gate
  record are committed, so every claim can be checked.
- **Tool note:** `/code-review ultra` could not run on the repo (the art branch diff is ~1M
  lines, limit 8k). The audit therefore runs as the in-repo multi-agent review above.
