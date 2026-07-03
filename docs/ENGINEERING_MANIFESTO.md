# NOPE Engineering Manifesto

*Author: Saleh · Maintained with every feature. If code violates this document,
either the code or the document must change — never silently.*

## The three laws

**1. Decoupled.** Every piece of logic has one home and one reason to change.
Game rules never live inside UI components; UI never computes rules. A class
should be understandable — and replaceable — without reading its neighbors.
The acid test: *can this logic run headless, in a unit test, with no scene
loaded?* If a rule can't, it is coupled, and it will eventually break silently.

**2. Reusable.** Code is written for the next feature, not just this one.
Prefer a small generic mechanism (a gate, a log, a window chrome, a compare
bar) over a bespoke one-off. If two systems half-duplicate each other, one of
them is a bug that hasn't happened yet. Reuse is enforced at review time:
before adding a new mechanism, name the existing one you tried to reuse and
why it didn't fit.

**3. Configurable.** Behaviour that a designer might tune lives in data
(ScriptableObjects), never in code constants. Every gameplay number — pay,
penalties, chances, thresholds, gating toggles — must be reachable in the
Inspector without a recompile. A hard-coded magic number in a rule path is a
defect, even when its value is correct.

## Working rules derived from the laws

- **Domain vs. presentation.** Pure simulation logic (scoring, gates, logs,
  ledgers) lives in `TimeDesk.Domain` — an assembly with no UI and no scene
  dependencies, covered by EditMode tests. MonoBehaviours orchestrate and
  render; they do not decide.
- **Boundaries are typed.** Systems talk through small structs, events, and
  interfaces (`ICameraRig`, `CompareEvidence`, `ReadyGate`), not by reaching
  into each other's hierarchies or statics. Static mutable state is forbidden
  in rule code.
- **Null-safe, optional wiring.** UI controllers degrade gracefully when a
  reference is unwired; a missing panel skips, it never throws. Scoring skips
  a gate the scene can't support (e.g. evidence gating without the rich desk).
- **Editor tooling is idempotent and authoritative.** Builders re-apply the
  layout they own on every run, so a scene built last month converges to
  today's design. "Finds existing and leaves it alone" is how stale bugs hide.
- **Fail loudly in the log, never in the frame.** Content gaps and wiring gaps
  produce a `LogWarning` with the fix spelled out, and the game keeps running.
- **Every feature is inventoried.** `docs/FEATURES.md` lists what the game
  does. A PR that removes or changes a listed behaviour must say so.
- **Tests gate the merge.** New rule logic ships with tests that encode the
  decision table, not just the happy path. A bug fixed twice is a missing
  test.

## Definition of done

A change is done when: the rule logic is in the domain assembly (or has a
written reason why not), its knobs are in a ScriptableObject, tests cover the
decision table, `FEATURES.md` is updated, and the editor builder can rebuild
the scene from scratch to a working state.
