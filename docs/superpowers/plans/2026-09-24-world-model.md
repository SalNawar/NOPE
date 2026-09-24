# World Model (piece 1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. Steps use checkbox syntax. Spec: `docs/superpowers/specs/2026-09-24-world-model-design.md`.

**Goal:** 8 countries × 5 eras of researched places, all facts read through one per-day `FactTable`, deterministic per-traveller randomness, and the made-up world retired.

**Architecture:** Pure pieces (`FactTable`, `IRandomSource`/`SeededRandom`/`Seeds`, `WeightedRandom`, `BirthDates`) live in `TimeDesk.Domain` with EditMode tests. `NationEraProfileSO` carries each place's facts. `ContentLibrarySO.BuildFactTable(plan)` is the single adapter. `CaseFactory`, the book windows and GameManager consume the table. One editor generator (`WorldContentGenerator`) authors all content from a committed JSON file.

**Environment:** worktree `E:\unity\NOPE-feat-clock`, branch `feat/world-model`. Offline checks as before (`compile_check.py`, reflection runner, `make_meta.py`). Unity verification via `-executeMethod` in the worktree's own editor. Some `.asset`/`.cs` files are CRLF: edit them byte-preserving.

---

### Task 1: Randomness in Domain
**Files:** create `Domain/IRandomSource.cs`, `Domain/SeededRandom.cs`, `Domain/Seeds.cs`; move `Scripts/WeightedRandom.cs` → `Domain/WeightedRandom.cs` (git mv keeps the GUID); create `Core/UnityRandomSource.cs`; modify `Home/HomeManager.cs` (slot pick passes `new UnityRandomSource()`), `Core/RunManager.cs` (`GetDaySeed` → `Seeds.Day`). **Tests:** `SeededRandomTests`, `SeedsTests`, `WeightedRandomTests`.
- `IRandomSource { int Range(int minInclusive, int maxExclusive); float Value(); }` (max ≤ min returns min; Value in [0,1))
- `SeededRandom(ulong seed)`: SplitMix64.
- `Seeds.Day(int runSeed, int day) = unchecked(runSeed * 397 ^ day * 7919)` (golden-tested); `Mix(int seed, int salt)`; `ForCase(int daySeed, int caseIndex1Based)`.
- `WeightedRandom.Pick<T>(IReadOnlyList<T>, Func<T,float>, IRandomSource)`: skips w ≤ 0, roll in [0,total), `roll < acc`.

### Task 2: BirthDates
**Files:** `Domain/BirthDates.cs`. **Tests:** `BirthDatesTests`.
- `Generate(int yearMin, int yearMax, IRandomSource)` → "12 Mar 830" / "3 Jun 1450 BCE" (never year 0).
- `Forge(string date, IRandomSource)` shifts the year ±2..24, skips 0, keeps the format; unparseable input gets "(?)".
- `Format(day, month, year)` and `TryParse(string, out day, out month, out year)`.

### Task 3: FactTable
**Files:** `Domain/FactTable.cs`; `Domain/DiscrepancyLog.cs` (`ValuesMatch` private → internal). **Tests:** `FactTableTests` (includes the DiscrepancyLog round trip).

### Task 4: Data types
**Files:** `Timeline/NationEraProfileSO.cs` (moment, year, birthYearMin/Max, facts, maleNames, femaleNames, `GetFact`, `OriginLabel`, `AllNames`), `EraSO.cs` (order), `DayPlanSO.cs` (allowedNations, `AllowsNation`), `Investigation/ReferenceBookSO.cs` (cover only), `ContentLibrarySO.cs` (`BuildFactTable(plan)`, `TodaysProfiles(plan)`, remove `GetReferenceBook`), `CaseInstance.cs` (originLabel).

### Task 5: CaseFactory
**Files:** `CaseFactory.cs`. Constructor `(lib, facts)`; `GenerateDayCases(plan, state, daySeed)`; a per-case `_rng`; the place pick from `TodaysProfiles`; names from the place pools; birth dates via `BirthDates`; fields, provable values and forgeries via `FactTable`; claim line and origin label from the profile; every `UnityEngine.Random` replaced.

### Task 6: UI + GameManager
**Files:** `UI/ReferenceBookWindowController.cs` (`SetBook(book, facts, compare)`), `UI/InvestigationUIController.cs` (`SetFacts`, shelf, fallback body), `GameManager.cs` (build the table per day, pass the seed).

### Task 7: Timeline + saves
**Files:** `Timeline/TimelineService.cs` (silent first ranking), `Core/SaveSystem.cs` (v2 gate).

### Task 8: Content
**Files:** `Assets/Data/World/world_source.json` (built from the verified research), `Editor/WorldContentGenerator.cs` (menu `Tools/TimeDesk/Generate World`), delete `Editor/InvestigationContentGenerator.cs` and `Editor/Phase7ContentGenerator.cs`, update `Editor/ContentLibraryValidator.cs`, repoint `Scenes/Test_DayLoop.unity`.

### Task 9: Docs
`docs/FEATURES.md` (world, today's books, random forgeries, determinism ticked with its tests, day ramp, BCE birth dates, silent first ranking, save v2).

### Task 10: Independent review (multi-lens + adversarial verify), then fixes.

### Task 11: Unity
Run Generate World, run Build Office UI, run the Test Runner, and do a scripted play-through of day 1 (places, books, forgery, closing). Commit the generated assets and the scene.
