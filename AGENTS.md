# NOPE (Time Sorter) — Agent Instructions

Papers, Please-style time-investigator game. Unity 6000.4.11f1, URP 2D, new
Input System, Cinemachine 3.1.7. Read these before touching anything:

- `docs/ENGINEERING_MANIFESTO.md` — the three laws (Decoupled, Reusable,
  Configurable). Non-negotiable; if code violates them, fix code or doc,
  never silently.
- `docs/FEATURES.md` — the behavior contract. Any change that alters listed
  behavior updates this file in the same change.
- `docs/TESTING_STRATEGY.md` — the 5-tier strategy; new rule logic ships
  with decision-table tests in `Assets/Tests/EditMode`.

## Operating rules (from the manifesto — violations are defects)

- Every tunable number lives in a ScriptableObject (`GameConfig_Default`,
  `RunConfig`, `ContentLibrary_Main`), never a code constant.
- Pure rule logic belongs in the `TimeDesk.Domain` asmdef, UI-free,
  EditMode-testable. MonoBehaviours orchestrate; they don't decide.
- **Zero prefabs.** Scenes are built/wired by `Tools → TimeDesk` editor
  builders (`Assets/Editor/*Builder.cs`, idempotent, authoritative) or
  generated at runtime in code. Content = ScriptableObject assets.
- No scene contains `RunManager`; `RunManager.GetOrCreate()` spawns it.
  `RunConfig.asset` must stay in `Assets/Resources/`.
- Content/wiring gaps fail loudly via `LogWarning` with the fix spelled out.
- Run `ContentLibraryValidator` after content changes (it caught the
  duplicate-day-number bug class before anyone ran it).

## Driving Unity from the command line

Editor 6000.4.11f1 is at
`C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe`.
Batch mode works only when the editor does NOT hold the project lock
(`Temp/UnityLockfile`). Pattern:

```
Unity.exe -batchmode -projectPath <repo> -executeMethod <MenuItem method> -quit -logFile <log>
```

Use it for: `ContentLibraryValidator`, the `Tools → TimeDesk` builders,
and EditMode tests (`-runTests -testPlatform EditMode -testResults results.xml`).

## Design docs (approved, live)

- `docs/superpowers/specs/2026-08-22-day-one-design.md` — Day 1 design
  (scripted spine V1/V3/V6, pools A/B/C, mercy wall-trace, evening economy).
- `docs/superpowers/plans/2026-08-22-day-one-implementation.md` — its
  implementation plan; Task 0 (vocabulary inventory) and Task 1 (duplicate
  day-number fix) come first.

## Known traps

- A successful `asset_refresh` + menu execution does NOT prove the new code compiled: Unity defers script recompiles until the editor is focused, and menu items keep running the last-good assembly. After editing scripts via the bridge, verify compilation explicitly (bridge restart after domain reload, or check for CS errors on a cleared console) before trusting any tool run or telling the director it works.
- Scene builders (Tools → TimeDesk) mark scenes dirty but NEVER save them. After driving a builder through the bridge, explicitly `scene_save` the target scene path — otherwise the build lives only in the editor's memory and silently vanishes from disk.
- ~~`DayPlan_1/2/3` (fictional nations) share day numbers with
  `DayPlan_Inv_Day1/2/3` (real-world)~~ — FIXED 2026-08-22: fictional set
  renumbered to days 4/5/6 (`DayPlan_4/5/6`); day 1 resolves to the
  investigation plan.
- ~~Duplicate archetype ids (merchant/diplomat) between the main and
  investigation sets~~ — FIXED 2026-08-22: investigation variants renamed to
  `trader`/`envoy`. The 6 "legendary has no nation" warnings for the Wanderer
  set are ACCEPTED design (wanderers are unaffiliated), not debt.
- `ShiftScoring`/`CaseFactory`/`WorldState` are still in
  `Assembly-CSharp` (extraction debt) — check assembly location before
  assuming testability. Content/factory rules are therefore validated by
  `Tools → TimeDesk → Validate Day One Spine` (editor script) instead of NUnit.
- The Unity-Skills bridge REST server may restart on ports 8090–8100 after
  domain reloads, and its mode can come back as `auto` (test_run then returns
  MODE_FORBIDDEN until the panel is set back to Bypass).
- The Notion mirror of the code docs lives one hop away in the agent's
  workspace memory; the repo's `docs/` is the source of truth.
