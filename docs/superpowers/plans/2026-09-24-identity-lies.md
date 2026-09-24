# Identity & Lies (piece 2) Implementation Plan

> **For agentic workers:** execute task-by-task (REQUIRED SUB-SKILL: superpowers:subagent-driven-development or superpowers:executing-plans). Steps use checkbox (`- [ ]`) syntax. Spec: `docs/superpowers/specs/2026-09-24-identity-lies-design.md`. House rules: `C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\HOUSE_RULES.md` (read it before Task 0).

**Goal:** Every traveller gets a claimed home and, if they lie, a true home among today's other places whose values leak onto their papers as provable tells, replacing piece 1's random forged field.

**Architecture:** The rules live in the pure `TimeDesk.Domain` assembly and are tested first: the lie stream seed (`Seeds.ForLies`), birth-date tell years (`BirthDates`), the provable-tell rule (`Forgery.IsProvableTell`), lie planning (`Lies`/`LiePlan`/`HomeCandidate`), the verdict table (`VerdictRules`) and gender derivation (`TravellerGenders`, `NameRoster.BaseName`). `CaseFactory` (Assembly-CSharp) becomes thin glue: it fills papers from the claim, projects today's places into `HomeCandidate`s, calls `Lies.Plan` on the traveller's own lie stream and applies the plan; `ShiftScoring` and `CaseInstance` call `VerdictRules`. The per-day tell count is authored in `world_source.json` and written by `Tools > TimeDesk > Generate World`.

**Tech Stack:** Unity 6000.4.11f1, C# 9, NUnit EditMode tests (`TimeDeskEditMode` references only `TimeDesk.Domain` and `TimeDesk.Visuals`), Python 3 helper scripts in the scratchpad (`subs.py`, `make_meta.py`, `compile_check.py`, the reflection test runner).

---

## Conventions used by every task

- **Worktree:** `E:\unity\NOPE-feat-clock`, branch `feat/identity-lies`. Never touch `E:\unity\NOPE`. Never push, rebase or amend.
- **SCRATCH** below means `C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad`.
- **Line endings.** CRLF: `ShiftLedger.cs`, `DocumentField.cs`, `DiscrepancyLog.cs`, `CitizenRegistry.cs`, every Assembly-CSharp script touched here (`CaseFactory.cs`, `CaseInstance.cs`, `DayPlanSO.cs`, `CaseBlueprintSO.cs`, `ShiftScoring.cs`, `GameManager.cs`, `InvestigationUIController.cs`, `CitizenRecordsWindowController.cs`, `GameConfigSO.cs`, `WorldState.cs`, `EffectSO.cs`, `ContentLibrarySO.cs`, `DocumentTemplateSO.cs`, `TimelineService.cs`), `ContentLibraryValidator.cs`, `DiscrepancyLogTests.cs` and `docs/FEATURES.md`. LF: `Seeds.cs`, `BirthDates.cs`, `Forgery.cs`, `FactTable.cs`, `NameRoster.cs`, `WorldContentGenerator.cs`, `world_source.json` and every other test file touched here. Edit existing files only through the task's Python script (it uses `SCRATCH/subs.py` and `SCRATCH/cut.py`, which preserve each file's EOL, and in Task 12 a counted byte-level rename, which cannot touch line endings). New files are written whole with the Write tool (LF).
- **Every new `.cs` under `Assets` gets a `.meta`** in the same commit: `python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/make_meta.py" <paths>`.
- **Compile check** (run from Bash):
  `python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/compile_check.py" 'E:\unity\NOPE-feat-clock'`
  Pass = both `== Assembly-CSharp-Editor.csproj: exit 0` and `== TimeDeskEditMode.csproj: exit 0` with `0 Error(s)`.
- **Test run** (after a passing compile check; the optional last argument filters by `Class.Method` substring):
  `"C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/runner/bin/Debug/net10.0/runner.exe" 'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\cc\Temp\Bin\Debug'`
  The branch base gives `passed 156, failed 0`. Each task states the new total.
- **Commits:** `cd /e/unity/NOPE-feat-clock && git add <exact paths> && git commit -F - <<'EOF' … EOF`. Every message ends with a blank line and `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Never stage `NOPE-feat-clock.sln`, `TimeDesk.Visuals.csproj` or `_TimeDesk*` files.
- Every task leaves both projects compiling and the suite green.
- **Pre-verified:** this plan was replayed against a copy of the worktree at `14bd22e` (`SCRATCH/plancheck/dryrun.py`, 2026-09-24, and again after the plan review, output `SCRATCH/plancheck/dryrun_rev2.txt`): every scripted edit matched exactly once, CRLF/LF were preserved, every task compiled (including the Task 0 and Task 13 temporary automation), the stated test totals were observed after each task, and the Task 9 and Task 12 grep gates came back empty. `build_world_source.py` reproduces the committed `world_source.json` byte for byte. If a script now reports `expected 1 match, found 0`, the file changed after `14bd22e`: re-read it and adapt that one pair.

## File map

| File | Responsibility | Task |
|---|---|---|
| `Assets/Tests/EditMode/ScriptedRandom.cs` (+ .meta) | new: the suite's one scripted `IRandomSource` (`ScriptStep`, `ScriptedRandom`: ordered Value/Range answers, fails on a wrong-kind or extra draw, `Draws`, `Done`) | 1 |
| `Assets/Tests/EditMode/ScriptedRandomTests.cs` (+ .meta) | new: pins the helper's contract (wrong-kind and extra draws fail, clamped Range answers, `Draws`/`Done`) | 1 |
| `Assets/Tests/EditMode/WeightedRandomTests.cs` | `FixedRolls` replaced by `ScriptedRandom` | 1 |
| `Assets/Scripts/Domain/Seeds.cs` | `LieSalt`, `ForLies`; `ForClues` doc | 2 |
| `Assets/Tests/EditMode/SeedsTests.cs` | lie-stream distinctness test | 2 |
| `Assets/Scripts/Domain/VerdictRules.cs` (+ .meta) | new: `ShouldAccept`, `IsUnprovenDenial` | 3 |
| `Assets/Tests/EditMode/VerdictRulesTests.cs` (+ .meta) | new: decision tables | 3 |
| `Assets/Scripts/Domain/NameRoster.cs` | `BaseName` (owner of the numeral-suffix format) | 4 |
| `Assets/Tests/EditMode/NameRosterTests.cs` | `BaseName` cases, round trip over `Take` | 4 |
| `Assets/Scripts/Domain/TravellerGender.cs` (+ .meta) | new: `TravellerGender` enum, `TravellerGenders.FromNameLists` | 5 |
| `Assets/Tests/EditMode/TravellerGendersTests.cs` (+ .meta) | new | 5 |
| `Assets/Scripts/Domain/BirthDates.cs` | + `HasOtherYear`, `PickOtherYear` (Task 6); − `Forge`, `AddYears`, class doc (Task 9) | 6, 9 |
| `Assets/Tests/EditMode/BirthDatesTests.cs` | + tell-year cases (Task 6); − `Forge_*` cases (Task 9) | 6, 9 |
| `Assets/Scripts/Domain/Lies.cs` (+ .meta) | new: `HomeCandidate` (Task 7); `LieOutcome`, `LiePlan`, `Lies` (Task 8) | 7, 8 |
| `Assets/Scripts/Domain/Forgery.cs` | + `IsProvableTell` (Task 7); − `IsProvable`, class doc (Task 9) | 7, 9 |
| `Assets/Tests/EditMode/ForgeryTests.cs` | + tell tests (Task 7); rewritten to the tell tests only (Task 9) | 7, 9 |
| `Assets/Tests/EditMode/LiesTests.cs` (+ .meta) | new: lie planning, golden draw order, round trip through `DiscrepancyLog` | 8 |
| `Assets/Scripts/Domain/FactTable.cs` | − `HasOtherValue`, `PickOtherValue`, `OtherValues`; class doc | 9 |
| `Assets/Tests/EditMode/FactTableTests.cs` | − `FixedIndex`/`First`, `PickOtherValue`/`HasOtherValue` tests; origin proof asserts the label | 9 |
| `Assets/Scripts/Domain/ShiftLedger.cs` | `CaseVerdict.wasForged` → `wasLiar` + `trueHomeLabel`; docs | 9 |
| `Assets/Scripts/Domain/DiscrepancyLog.cs` | `ValuesMatch` doc (Task 9); remaining docs (Task 12) | 9, 12 |
| `Assets/Scripts/CaseInstance.cs` | claim docs; − `isForged`; + `trueHome`, `trueHomeLabel`, `IsLiar`, `HomeLabel`, `gender`; `ShouldAccept` via `VerdictRules` | 9 |
| `Assets/Scripts/CaseFactory.cs` | lie stream, `PlaceLabel`, gender, `PopulateDocumentFields` returns fields, `Disguise`, `LiarChance`; forge code removed; docs | 9 |
| `Assets/Scripts/DayPlanSO.cs` | + `tellCount`/`TellCount`; "claimed (home) era" docs | 9 |
| `Assets/Scripts/CaseBlueprintSO.cs` | − `forgedBirthYearShift`; `contradictionChance` doc | 9 |
| `Assets/Scripts/Shift/ShiftScoring.cs` | `VerdictRules` gate, `wasLiar`/`trueHomeLabel`, R12 citation text, docs, log | 9 |
| `Assets/Scripts/GameManager.cs` | case-start and `[Result]` logs | 9 |
| `docs/FEATURES.md` | behaviour contract (:11, :36, :43, :44 + gender bullet, :55–57, :83, :85, :87 in Task 9; :36 Records wiring warning and :62 in Task 10; :94 in Task 11) | 9, 10, 11 |
| `Assets/Scripts/UI/InvestigationUIController.cs` | registry field, fallback agency-record block, Records wiring warning, R12 idle hint, doc | 10 |
| `Assets/Editor/WorldContentGenerator.cs` | `DayData.tells`, `tells < 1` check, `MakeDay` writes `tellCount` | 11 |
| `Assets/Data/World/world_source.json` | `"tells": 1` on each day | 11 |
| `SCRATCH/build_world_source.py` (not committed) | same key, so a rebuild keeps it | 11 |
| `DocumentField.cs`, `CitizenRegistry.cs`, `GameConfigSO.cs`, `WorldState.cs`, `EffectSO.cs`, `ContentLibrarySO.cs`, `DocumentTemplateSO.cs`, `TimelineService.cs`, `CitizenRecordsWindowController.cs`, `ContentLibraryValidator.cs` | doc comments / validator message only | 12 |
| `Assets/Tests/EditMode/DiscrepancyLogTests.cs` | tell vocabulary only: `TellDocField`, `TellIdentityField`, `BirthDateTell_VsRecord_…`, class doc and one comment (assertions unchanged) | 12 |
| `Assets/Data/Investigation/DayPlan_Inv_Day1..3.asset`, `CaseBlueprint_Investigation.asset` | regenerated by Generate World in Unity (`tellCount: 1`; stale `forgedBirthYearShift` dropped) | 13 |
| `docs/superpowers/specs/2026-09-24-identity-lies-design.md` | verification record appended | 13 |
| `SCRATCH/cut.py` (not committed) | EOL-preserving "replace from marker to marker" helper | 0 |
| `Assets/Editor/_TimeDeskLies*.cs` (never committed) | temporary Unity automation | 0, 13 |

---

### Task 0: Setup and the pre-change baseline (Unity)

The spec's verification step 1 needs a dump of who travellers are **before any code change** (branch base `14bd22e`), so Task 13 can prove the stream contract line for line.

**Files:**
- Create (scratch, not committed): `SCRATCH/cut.py`
- Create (temporary, never committed): `Assets/Editor/_TimeDeskLiesBaseline.cs`
- Output: `SCRATCH/lies_baseline.txt`

- [ ] **Step 1: Confirm the starting point**

Run: `cd /e/unity/NOPE-feat-clock && git branch --show-current && git log --oneline -3 && git status --short`
Expected: `feat/identity-lies`; the last three commits are `docs(plan): identity & lies implementation plan`, `docs(spec): identity & lies (piece 2) design` and `14bd22e docs(spec): world-model verification record` (the two docs commits change no code, so the code is still the branch base `14bd22e`); untracked only `NOPE-feat-clock.sln` and `TimeDesk.Visuals.csproj`.

- [ ] **Step 2: Create the cut helper** (`SCRATCH/cut.py`, Write tool)

```python
"""EOL-preserving marker cut: cut(path, start, end, insert='') replaces the text from
`start` (inclusive, must match exactly once) through the first `end` after it
(inclusive) with `insert`. CRLF files are normalised to LF for matching and restored."""
import pathlib, sys

def cut(path, start, end, insert=''):
    p = pathlib.Path(path); b = p.read_bytes(); crlf = b'\r\n' in b
    t = b.decode('utf-8').replace('\r\n', '\n')
    if t.count(start) != 1:
        sys.exit(f"{p}: start marker must match once, found {t.count(start)}: {start[:100]!r}")
    i = t.index(start)
    j = t.find(end, i)
    if j < 0:
        sys.exit(f"{p}: end marker not found after start: {end[:100]!r}")
    t = t[:i] + insert + t[j + len(end):]
    if crlf:
        t = t.replace('\n', '\r\n')
    p.write_bytes(t.encode('utf-8'))
    print('cut', p, 'crlf' if crlf else 'lf')
```

- [ ] **Step 3: Check no Unity editor is open on the worktree** (PowerShell)

Run: `Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Select-Object ProcessId, CommandLine`
Expected: no process whose command line contains `NOPE-feat-clock` (one on `E:\unity\NOPE` is Codex's; leave it alone).

- [ ] **Step 4: Write the baseline dumper** (`Assets/Editor/_TimeDeskLiesBaseline.cs`, Write tool)

```csharp
// TEMPORARY (piece 2 baseline; never committed): dumps who each traveller is
// before any piece-2 change, one line per slot, for the stream-contract check.
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;

public static class TimeDeskLiesBaseline
{
    private const string OutPath = @"C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\lies_baseline.txt";

    public static void Run()
    {
        try
        {
            var lib = AssetDatabase.LoadAssetAtPath<ContentLibrarySO>("Assets/Data/Content Library/ContentLibrary_Main.asset");
            var lines = new List<string>();
            foreach (int seed in new[] { 12345, 999 })
            {
                for (int day = 1; day <= 3; day++)
                {
                    DayPlanSO plan = lib.GetDayPlan(day);
                    var factory = new CaseFactory(lib, lib.BuildFactTable(plan));
                    List<CaseInstance> cases = factory.GenerateDayCases(plan, new WorldState { day = day }, Seeds.Day(seed, day));
                    var violators = (Dictionary<int, NationEraProfileSO>)typeof(CaseFactory)
                        .GetField("_violators", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(factory);
                    for (int i = 0; i < cases.Count; i++)
                        lines.Add(Line(seed, day, i + 1, cases[i], violators.ContainsKey(i + 1)));
                }
            }

            File.WriteAllLines(OutPath, lines);
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            File.WriteAllText(OutPath + ".error", e.ToString());
            EditorApplication.Exit(2);
        }
    }

    /// <summary>One slot. Task 13's world check prints exactly this format.</summary>
    private static string Line(int seed, int day, int slot, CaseInstance c, bool violator) =>
        $"seed={seed} day={day} slot={slot} claim='{c.originLabel}' name='{c.visitorGivenName}' born='{c.trueBirthDate}' role='{(c.archetype != null ? c.archetype.displayName : "")}' allowed={c.claimAllowedByRules} violator={violator}";
}
```

- [ ] **Step 5: Run it in the worktree's own Unity** (PowerShell, timeout 600000 ms)

```powershell
Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe' -ArgumentList @('-projectPath','E:\unity\NOPE-feat-clock','-executeMethod','TimeDeskLiesBaseline.Run','-logFile','C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\unity_lies_baseline.log') -PassThru -Wait
```
Expected: exit code 0; `SCRATCH/lies_baseline.txt` has 60 lines (2 seeds × (8 + 10 + 12) slots), each like `seed=12345 day=1 slot=1 claim='…' name='…' born='…' role='…' allowed=True violator=False`; no `lies_baseline.txt.error`. Check with `wc -l "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/lies_baseline.txt"`.

- [ ] **Step 6: Clean up**

```bash
cd /e/unity/NOPE-feat-clock && rm -f Assets/Editor/_TimeDeskLiesBaseline.cs Assets/Editor/_TimeDeskLiesBaseline.cs.meta && git checkout -- Assembly-CSharp.csproj Assembly-CSharp-Editor.csproj TimeDesk.Domain.csproj TimeDeskEditMode.csproj NOPE.sln ProjectSettings "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset" Assets/Scenes/OfficeScene.unity && git status --short
```
Expected: only `?? NOPE-feat-clock.sln` and `?? TimeDesk.Visuals.csproj`. No commit in this task.

---

### Task 1: One shared scripted random source for tests

The golden-order tests of Task 8 pin the lie stream's draw order only because `ScriptedRandom` fails a test on a draw of the wrong kind or past the end of its script. `ScriptedRandomTests` pins that behaviour, so a regression in the helper (for example clamping instead of failing) fails loudly instead of silently un-pinning every golden-order test.

**Files:**
- Create: `Assets/Tests/EditMode/ScriptedRandom.cs` (+ `.meta`)
- Test: `Assets/Tests/EditMode/ScriptedRandomTests.cs` (+ `.meta`)
- Modify: `Assets/Tests/EditMode/WeightedRandomTests.cs` (whole file)

- [ ] **Step 1: Write the failing tests: the helper's own contract, and WeightedRandomTests pointed at it**

Write `Assets/Tests/EditMode/ScriptedRandomTests.cs` (Write tool, LF), then make its meta: `python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/make_meta.py" 'E:\unity\NOPE-feat-clock\Assets\Tests\EditMode\ScriptedRandomTests.cs'`

```csharp
using NUnit.Framework;

/// <summary>
/// The contract the golden-order tests rely on: a draw of the wrong kind or
/// past the end of the script fails the test, Range answers are offsets from
/// minInclusive clamped into the range, and Draws/Done track the script.
/// </summary>
public class ScriptedRandomTests
{
    [Test]
    public void ADrawOfTheWrongKind_FailsTheTest()
    {
        var onValue = new ScriptedRandom(ScriptStep.Value(0.5f));
        Assert.Throws<AssertionException>(() => onValue.Range(0, 3));

        var onRange = new ScriptedRandom(ScriptStep.Range(0));
        Assert.Throws<AssertionException>(() => onRange.Value());
    }

    [Test]
    public void ADrawPastTheEndOfTheScript_FailsTheTest()
    {
        var rng = new ScriptedRandom(ScriptStep.Range(0));
        rng.Range(0, 3);
        Assert.Throws<AssertionException>(() => rng.Range(0, 3));
        Assert.Throws<AssertionException>(() => rng.Value());
        Assert.Throws<AssertionException>(() => new ScriptedRandom().Value());
    }

    [Test]
    public void ARangeAnswer_IsAnOffsetFromMin_ClampedIntoTheRange()
    {
        var rng = new ScriptedRandom(ScriptStep.Range(1), ScriptStep.Range(5), ScriptStep.Range(-3));
        Assert.AreEqual(11, rng.Range(10, 13));
        Assert.AreEqual(12, rng.Range(10, 13), "clamped to maxExclusive - 1");
        Assert.AreEqual(10, rng.Range(10, 13), "clamped to minInclusive");
    }

    [Test]
    public void DrawsAndDone_TrackTheScript()
    {
        var rng = new ScriptedRandom(ScriptStep.Value(0.25f), ScriptStep.Range(0));
        Assert.AreEqual(0, rng.Draws);
        Assert.IsFalse(rng.Done);

        Assert.AreEqual(0.25f, rng.Value());
        Assert.AreEqual(1, rng.Draws);
        Assert.IsFalse(rng.Done);

        Assert.AreEqual(4, rng.Range(4, 9));
        Assert.AreEqual(2, rng.Draws);
        Assert.IsTrue(rng.Done);
    }
}
```

Then replace the whole of `Assets/Tests/EditMode/WeightedRandomTests.cs` (Write tool, LF):

```csharp
using NUnit.Framework;

public class WeightedRandomTests
{
    private sealed class Item
    {
        public string name;
        public float weight;
    }

    [Test]
    public void ZeroWeightItem_IsNeverPicked_EvenOnARollOfZero()
    {
        var items = new[] { new Item { name = "a", weight = 0f }, new Item { name = "b", weight = 1f } };
        Assert.AreEqual("b", WeightedRandom.Pick(items, i => i.weight, new ScriptedRandom(ScriptStep.Value(0f))).name);
    }

    [Test]
    public void AllZeroOrEmpty_ReturnsDefault()
    {
        var items = new[] { new Item { name = "a", weight = 0f } };
        Assert.IsNull(WeightedRandom.Pick(items, i => i.weight, new ScriptedRandom(ScriptStep.Value(0.5f))));
        Assert.IsNull(WeightedRandom.Pick(new Item[0], i => i.weight, new ScriptedRandom(ScriptStep.Value(0.5f))));
        Assert.IsNull(WeightedRandom.Pick<Item>(null, i => i.weight, new ScriptedRandom(ScriptStep.Value(0.5f))));
    }

    [Test]
    public void SingleItem_IsAlwaysPicked()
    {
        var items = new[] { new Item { name = "only", weight = 2f } };
        Assert.AreEqual("only", WeightedRandom.Pick(items, i => i.weight, new ScriptedRandom(ScriptStep.Value(0.999f))).name);
    }

    [TestCase(0.0f, "a")]
    [TestCase(0.24f, "a")]
    [TestCase(0.25f, "b")]
    [TestCase(0.99f, "b")]
    public void Roll_MapsOntoCumulativeWeights(float roll, string expected)
    {
        var items = new[] { new Item { name = "a", weight = 1f }, new Item { name = "b", weight = 3f } };
        Assert.AreEqual(expected, WeightedRandom.Pick(items, i => i.weight, new ScriptedRandom(ScriptStep.Value(roll))).name);
    }

    [Test]
    public void Proportions_FollowWeights()
    {
        var items = new[] { new Item { name = "a", weight = 1f }, new Item { name = "b", weight = 3f } };
        var rng = new SeededRandom(2026);
        int b = 0;
        for (int i = 0; i < 4000; i++)
            if (WeightedRandom.Pick(items, it => it.weight, rng).name == "b")
                b++;
        Assert.That(b / 4000f, Is.InRange(0.72f, 0.78f));
    }
}
```

- [ ] **Step 2: Run the compile check to verify it fails**

Expected: `== TimeDeskEditMode.csproj: exit 1` with `error CS0246: The type or namespace name 'ScriptedRandom' could not be found` (and `error CS0103: The name 'ScriptStep' does not exist in the current context`), from both test files.

- [ ] **Step 3: Write the shared source** (`Assets/Tests/EditMode/ScriptedRandom.cs`, Write tool), then make its meta: `python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/make_meta.py" 'E:\unity\NOPE-feat-clock\Assets\Tests\EditMode\ScriptedRandom.cs'`

```csharp
using NUnit.Framework;

/// <summary>One scripted answer for <see cref="ScriptedRandom"/>: a Value() roll or a Range() offset.</summary>
public readonly struct ScriptStep
{
    /// <summary>True for a Value() answer, false for a Range() answer.</summary>
    public readonly bool IsValue;

    /// <summary>The Value() answer.</summary>
    public readonly float Roll;

    /// <summary>The Range() answer, as an offset from minInclusive (clamped into the range).</summary>
    public readonly int Offset;

    private ScriptStep(bool isValue, float roll, int offset)
    {
        IsValue = isValue;
        Roll = roll;
        Offset = offset;
    }

    /// <summary>Answers the next draw, which must be a Value() draw, with <paramref name="roll"/>.</summary>
    public static ScriptStep Value(float roll) => new ScriptStep(true, roll, 0);

    /// <summary>Answers the next draw, which must be a Range() draw, with minInclusive + <paramref name="offset"/> (clamped).</summary>
    public static ScriptStep Range(int offset) => new ScriptStep(false, 0f, offset);
}

/// <summary>
/// The suite's one scripted random source: answers draws from an ordered
/// script, so tests can pin the exact draw order. Each draw takes the next
/// step; a draw of the other kind than the next step, or a draw past the end
/// of the script, fails the test.
/// </summary>
public sealed class ScriptedRandom : IRandomSource
{
    /// <summary>The answers, in draw order.</summary>
    private readonly ScriptStep[] _script;

    /// <summary>Creates a source that answers with <paramref name="script"/>, in order.</summary>
    public ScriptedRandom(params ScriptStep[] script)
    {
        _script = script ?? new ScriptStep[0];
    }

    /// <summary>Number of draws made so far.</summary>
    public int Draws { get; private set; }

    /// <summary>True when every scripted step has been drawn.</summary>
    public bool Done => Draws == _script.Length;

    /// <inheritdoc />
    public int Range(int minInclusive, int maxExclusive)
    {
        ScriptStep step = Take(false);
        if (maxExclusive <= minInclusive)
            return minInclusive;

        long answer = (long)minInclusive + step.Offset;
        return answer < minInclusive ? minInclusive : answer >= maxExclusive ? maxExclusive - 1 : (int)answer;
    }

    /// <inheritdoc />
    public float Value() => Take(true).Roll;

    /// <summary>The next step; fails the test on a draw of the wrong kind or past the end.</summary>
    private ScriptStep Take(bool wantValue)
    {
        string kind = wantValue ? "Value" : "Range";
        if (Draws >= _script.Length)
            Assert.Fail($"Draw {Draws + 1} ({kind}) is past the end of the {_script.Length}-step script.");

        ScriptStep step = _script[Draws];
        if (step.IsValue != wantValue)
            Assert.Fail($"Draw {Draws + 1} is a {kind} draw, but the script expects a {(step.IsValue ? "Value" : "Range")} draw.");

        Draws++;
        return step;
    }
}
```

- [ ] **Step 4: Compile check, then run the tests**

Expected: compile check exit 0 for both projects; runner `passed 160, failed 0` (filter `WeightedRandomTests` shows 8 passing cases, filter `ScriptedRandomTests` 4).

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Tests/EditMode/ScriptedRandom.cs Assets/Tests/EditMode/ScriptedRandom.cs.meta Assets/Tests/EditMode/ScriptedRandomTests.cs Assets/Tests/EditMode/ScriptedRandomTests.cs.meta Assets/Tests/EditMode/WeightedRandomTests.cs && git commit -F - <<'EOF'
test: one shared scripted random source (ScriptedRandom)

Answers draws from an ordered Value/Range script and fails a test on a
draw of the wrong kind or past the end, so later tests can pin draw order;
ScriptedRandomTests pins that contract. Replaces WeightedRandomTests.FixedRolls.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 2: The per-traveller lie stream (`Seeds.ForLies`)

**Files:**
- Modify: `Assets/Scripts/Domain/Seeds.cs`
- Test: `Assets/Tests/EditMode/SeedsTests.cs`

- [ ] **Step 1: Write the failing test** — save as `SCRATCH/p2_t2_test.py` and run `python` on it:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Tests\EditMode\SeedsTests.cs', [
(r"""    [Test]
    public void Mix_IsDeterministic_AndSaltSensitive()""",
r"""    [Test]
    public void LieStream_IsDistinctFromCaseClueAndViolatorStreams()
    {
        int daySeed = Seeds.Day(12345, 2);
        var cases = new HashSet<int>(Enumerable.Range(1, 20).Select(c => Seeds.ForCase(daySeed, c)));
        var lies = new HashSet<int>();
        for (int c = 1; c <= 20; c++)
        {
            int caseSeed = Seeds.ForCase(daySeed, c);
            int lie = Seeds.ForLies(caseSeed);
            Assert.IsFalse(cases.Contains(lie), $"case {c}: the lie seed is a case seed");
            Assert.AreNotEqual(Seeds.ForClues(caseSeed), lie, $"case {c}: lie seed equals the clue seed");
            Assert.AreNotEqual(Seeds.ForViolators(daySeed), lie, $"case {c}: lie seed equals the violator seed");
            Assert.AreEqual(lie, Seeds.ForLies(caseSeed), $"case {c}: not deterministic");
            lies.Add(lie);
        }
        Assert.AreEqual(20, lies.Count, "lie seeds repeat across cases");
    }

    [Test]
    public void Mix_IsDeterministic_AndSaltSensitive()"""),
])
```

- [ ] **Step 2: Compile check — expect failure**

Expected: `== TimeDeskEditMode.csproj: exit 1`, `error CS0117: 'Seeds' does not contain a definition for 'ForLies'`.

- [ ] **Step 3: Implement** — save as `SCRATCH/p2_t2.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Scripts\Domain\Seeds.cs', [
(r"""    public const int ClueSalt = 0x434C5545;""",
r"""    public const int ClueSalt = 0x434C5545;

    /// <summary>Salt for a traveller's lie stream ("LIES").</summary>
    public const int LieSalt = 0x4C494553;"""),
(r"""    /// stream so clue settings never change who is a forger.""",
r"""    /// stream so clue settings never change who lies."""),
(r"""    public static int ForClues(int caseSeed) => Mix(caseSeed, ClueSalt);""",
r"""    public static int ForClues(int caseSeed) => Mix(caseSeed, ClueSalt);

    /// <summary>
    /// Seed for one traveller's lie stream: the liar roll, the true-home pick
    /// and the tell picks. Kept apart from the case stream so lie tuning never
    /// changes who travellers are.
    /// </summary>
    public static int ForLies(int caseSeed) => Mix(caseSeed, LieSalt);"""),
])
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 161, failed 0`.

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Domain/Seeds.cs Assets/Tests/EditMode/SeedsTests.cs && git commit -F - <<'EOF'
feat(domain): per-traveller lie stream (Seeds.ForLies)

A salted stream for the liar roll, true-home pick and tell picks, apart
from the case stream so lie tuning never changes who travellers are.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 3: The verdict table in Domain (`VerdictRules`)

**Files:**
- Create: `Assets/Scripts/Domain/VerdictRules.cs` (+ `.meta`)
- Test: `Assets/Tests/EditMode/VerdictRulesTests.cs` (+ `.meta`)

- [ ] **Step 1: Write the failing test** (`Assets/Tests/EditMode/VerdictRulesTests.cs`, Write tool; then `make_meta.py` on it)

```csharp
using NUnit.Framework;

public class VerdictRulesTests
{
    [TestCase(false, true, true)]
    [TestCase(false, false, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, false)]
    public void ShouldAccept_OnlyAnHonestTravellerWithAnAllowedClaim(bool isLiar, bool claimAllowed, bool expected)
    {
        Assert.AreEqual(expected, VerdictRules.ShouldAccept(isLiar, claimAllowed));
    }

    // Row 1 is the one unproven denial; every other row flips one input of it.
    [TestCase(true, 0, false, true, true, true)]
    [TestCase(false, 0, false, true, true, false)]
    [TestCase(true, -1, false, true, true, false)]
    [TestCase(true, 1, false, true, true, false)]
    [TestCase(true, 0, true, true, true, false)]
    [TestCase(true, 0, false, false, true, false)]
    [TestCase(true, 0, false, true, false, false)]
    public void IsUnprovenDenial_OnlyAnUnevidencedDenialOfAnAllowedLiar(bool requireEvidence, int evidenceCount, bool accepted, bool isLiar, bool claimAllowed, bool expected)
    {
        Assert.AreEqual(expected, VerdictRules.IsUnprovenDenial(requireEvidence, evidenceCount, accepted, isLiar, claimAllowed));
    }
}
```

- [ ] **Step 2: Compile check — expect failure**

Expected: `== TimeDeskEditMode.csproj: exit 1`, `error CS0103: The name 'VerdictRules' does not exist in the current context`.

- [ ] **Step 3: Implement** (`Assets/Scripts/Domain/VerdictRules.cs`, Write tool; then `make_meta.py` on it)

```csharp
/// <summary>
/// The accept/deny decision table: who should be accepted, and when a right
/// denial still earns a citation because nothing was documented. Pure, so
/// every row is tested headless.
/// </summary>
public static class VerdictRules
{
    /// <summary>Accept only an honest traveller whose claimed destination today's rules allow.</summary>
    public static bool ShouldAccept(bool isLiar, bool claimAllowed) => !isLiar && claimAllowed;

    /// <summary>
    /// True when a denial is right but unproven: the evidence gate is on, the
    /// evidence system is active and logged nothing (<paramref name="evidenceCount"/>
    /// is -1 when the system is inactive), and the denied traveller is a liar
    /// whose claim is allowed (directive denials never need evidence).
    /// </summary>
    public static bool IsUnprovenDenial(bool requireEvidence, int evidenceCount, bool accepted, bool isLiar, bool claimAllowed) =>
        requireEvidence && evidenceCount == 0 && !accepted && isLiar && claimAllowed;
}
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 172, failed 0`.

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Domain/VerdictRules.cs Assets/Scripts/Domain/VerdictRules.cs.meta Assets/Tests/EditMode/VerdictRulesTests.cs Assets/Tests/EditMode/VerdictRulesTests.cs.meta && git commit -F - <<'EOF'
feat(domain): verdict rules in Domain (ShouldAccept, unproven denial)

The accept/deny decision and the evidence gate as a tested decision table;
CaseInstance and ShiftScoring call it once liars exist.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 4: `NameRoster.BaseName` owns the numeral-suffix format

**Files:**
- Modify: `Assets/Scripts/Domain/NameRoster.cs`
- Test: `Assets/Tests/EditMode/NameRosterTests.cs`

- [ ] **Step 1: Write the failing tests** — save as `SCRATCH/p2_t4_test.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Tests\EditMode\NameRosterTests.cs', [
(r"""        Assert.AreEqual(expected, NameRoster.Roman(number));
    }
}""",
r"""        Assert.AreEqual(expected, NameRoster.Roman(number));
    }

    [TestCase("Marcus II", "Marcus")]
    [TestCase("Marcus XIV", "Marcus")]
    [TestCase(" Marcus II ", "Marcus")]
    [TestCase("Marcus", "Marcus")]
    [TestCase("Anna Maria", "Anna Maria")]
    [TestCase("Mary Ann", "Mary Ann")]
    [TestCase("Sitt al-Wuzara", "Sitt al-Wuzara")]
    [TestCase("Marcus I", "Marcus I")]
    [TestCase("Marcus iv", "Marcus iv")]
    public void BaseName_StripsOnlyTheSuffixTakeAdds(string name, string expected)
    {
        Assert.AreEqual(expected, NameRoster.BaseName(name));
    }

    [Test]
    public void BaseName_MapsEveryNameTakeHandsOut_BackToItsPoolName()
    {
        var roster = new NameRoster();
        string[] pool = { "Marcus", "Anna Maria" };
        for (int i = 0; i < 40; i++)
        {
            int pick = i;
            string name = roster.Take(pool, n => pick % n);
            CollectionAssert.Contains(pool, NameRoster.BaseName(name), name);
        }
    }
}"""),
])
```

- [ ] **Step 2: Compile check — expect failure**

Expected: `== TimeDeskEditMode.csproj: exit 1`, `error CS0117: 'NameRoster' does not contain a definition for 'BaseName'`.

- [ ] **Step 3: Implement** — save as `SCRATCH/p2_t4.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Scripts\Domain\NameRoster.cs', [
(r"""        return sb.ToString();
    }
""",
r"""        return sb.ToString();
    }

    /// <summary>
    /// The pool name behind a name <see cref="Take"/> handed out: the trimmed
    /// name without its " {numeral}" suffix ("Marcus II" gives "Marcus"). The
    /// last word counts as a suffix only when it is exactly <see cref="Roman"/>(n)
    /// for some n of at least 2, so "Anna Maria", "Marcus I" and "Marcus iv"
    /// stay whole. Null stays null.
    /// </summary>
    public static string BaseName(string name)
    {
        if (name == null)
            return null;

        string trimmed = name.Trim();
        int space = trimmed.LastIndexOf(' ');
        if (space <= 0)
            return trimmed;

        string last = trimmed.Substring(space + 1);
        int value = ParseRoman(last);
        return value >= 2 && Roman(value) == last ? trimmed.Substring(0, space).TrimEnd() : trimmed;
    }

    /// <summary>Value of an upper-case Roman numeral; 0 when it holds another character or exceeds 3999.</summary>
    private static int ParseRoman(string numeral)
    {
        int total = 0;
        for (int i = 0; i < numeral.Length; i++)
        {
            int digit = RomanDigit(numeral[i]);
            if (digit == 0)
                return 0;

            int next = i + 1 < numeral.Length ? RomanDigit(numeral[i + 1]) : 0;
            total += digit < next ? -digit : digit;
        }

        return total <= 3999 ? total : 0;
    }

    /// <summary>Value of one upper-case Roman digit (0 for any other character).</summary>
    private static int RomanDigit(char c)
    {
        switch (c)
        {
            case 'I': return 1;
            case 'V': return 5;
            case 'X': return 10;
            case 'L': return 50;
            case 'C': return 100;
            case 'D': return 500;
            case 'M': return 1000;
            default: return 0;
        }
    }
"""),
])
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 182, failed 0`.

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Domain/NameRoster.cs Assets/Tests/EditMode/NameRosterTests.cs && git commit -F - <<'EOF'
feat(domain): NameRoster.BaseName owns the numeral-suffix format

Maps "Marcus II" back to its pool name; only an exact Roman numeral of 2 or
more counts as a suffix, so multi-word names stay whole.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 5: Traveller gender from the claimed place's name lists

**Files:**
- Create: `Assets/Scripts/Domain/TravellerGender.cs` (+ `.meta`)
- Test: `Assets/Tests/EditMode/TravellerGendersTests.cs` (+ `.meta`)

- [ ] **Step 1: Write the failing test** (`Assets/Tests/EditMode/TravellerGendersTests.cs`, Write tool; then `make_meta.py`)

```csharp
using NUnit.Framework;

/// <summary>
/// Gender from the claimed place's name lists. Male: Marcus, Gaius, Sasha.
/// Female: Lucia, Anna Maria, Sasha (Sasha is on both).
/// </summary>
public class TravellerGendersTests
{
    private static readonly string[] Male = { "Marcus", "Gaius", "Sasha" };
    private static readonly string[] Female = { "Lucia", "Anna Maria", "Sasha" };

    private static TravellerGender Of(string name) => TravellerGenders.FromNameLists(name, Male, Female);

    [Test]
    public void NameOnTheMaleList_IsMale()
    {
        Assert.AreEqual(TravellerGender.Male, Of("Gaius"));
    }

    [Test]
    public void NameOnTheFemaleList_IsFemale()
    {
        Assert.AreEqual(TravellerGender.Female, Of("Lucia"));
    }

    [Test]
    public void SuffixedName_CountsAsItsPoolName()
    {
        Assert.AreEqual(TravellerGender.Male, Of("Marcus II"));
        Assert.AreEqual(TravellerGender.Female, Of("Lucia XIV"));
    }

    [Test]
    public void MultiWordName_IsMatchedWhole()
    {
        Assert.AreEqual(TravellerGender.Female, Of("Anna Maria"));
    }

    [Test]
    public void Comparison_IgnoresCaseAndSurroundingSpaces()
    {
        Assert.AreEqual(TravellerGender.Male, Of(" marcus "));
    }

    [Test]
    public void NameOnBothLists_IsUnknown()
    {
        Assert.AreEqual(TravellerGender.Unknown, Of("Sasha"));
    }

    [Test]
    public void SubjectFallback_IsUnknown()
    {
        Assert.AreEqual(TravellerGender.Unknown, Of("Subject #3"));
    }

    [Test]
    public void NullOrBlankName_IsUnknown()
    {
        Assert.AreEqual(TravellerGender.Unknown, Of(null));
        Assert.AreEqual(TravellerGender.Unknown, Of("  "));
    }

    [Test]
    public void ANullListCountsAsEmpty()
    {
        Assert.AreEqual(TravellerGender.Unknown, TravellerGenders.FromNameLists("Marcus", null, Female));
        Assert.AreEqual(TravellerGender.Female, TravellerGenders.FromNameLists("Lucia", null, Female));
    }
}
```

- [ ] **Step 2: Compile check — expect failure**

Expected: `== TimeDeskEditMode.csproj: exit 1`, `error CS0246: The type or namespace name 'TravellerGender' could not be found`.

- [ ] **Step 3: Implement** (`Assets/Scripts/Domain/TravellerGender.cs`, Write tool; then `make_meta.py`)

```csharp
using System.Collections.Generic;

/// <summary>A traveller's gender, as far as the agency can tell from their name.</summary>
public enum TravellerGender
{
    /// <summary>Not derivable: legendaries, "Subject #n", or a name on both lists or neither.</summary>
    Unknown,

    /// <summary>The name is on the claimed place's male list.</summary>
    Male,

    /// <summary>The name is on the claimed place's female list.</summary>
    Female
}

/// <summary>
/// Derives a traveller's gender from which of the claimed place's name lists
/// their given name came from, so it costs no draw. Pure, so it is tested headless.
/// </summary>
public static class TravellerGenders
{
    /// <summary>
    /// Male or Female when <paramref name="givenName"/> is on exactly one list:
    /// first as written, then without the numeral suffix NameRoster adds
    /// ("Marcus II" counts as Marcus). Both passes use the scanner comparison
    /// (trimmed, case-insensitive). A null list counts as empty; a null or blank
    /// name, or a name on both lists or on neither, is Unknown.
    /// </summary>
    public static TravellerGender FromNameLists(string givenName, IReadOnlyList<string> maleNames, IReadOnlyList<string> femaleNames)
    {
        if (string.IsNullOrWhiteSpace(givenName))
            return TravellerGender.Unknown;

        TravellerGender exact = Classify(givenName, maleNames, femaleNames, out bool found);
        if (found)
            return exact;

        return Classify(NameRoster.BaseName(givenName), maleNames, femaleNames, out _);
    }

    /// <summary>Gender by list membership; <paramref name="found"/> is true when the name is on at least one list.</summary>
    private static TravellerGender Classify(string name, IReadOnlyList<string> maleNames, IReadOnlyList<string> femaleNames, out bool found)
    {
        bool male = Contains(maleNames, name);
        bool female = Contains(femaleNames, name);
        found = male || female;
        return male == female ? TravellerGender.Unknown : male ? TravellerGender.Male : TravellerGender.Female;
    }

    /// <summary>True when the list holds the name under the scanner comparison (a null list holds nothing).</summary>
    private static bool Contains(IReadOnlyList<string> names, string name)
    {
        if (names == null)
            return false;

        foreach (string candidate in names)
            if (DiscrepancyLog.ValuesMatch(candidate, name))
                return true;

        return false;
    }
}
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 191, failed 0`.

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Domain/TravellerGender.cs Assets/Scripts/Domain/TravellerGender.cs.meta Assets/Tests/EditMode/TravellerGendersTests.cs Assets/Tests/EditMode/TravellerGendersTests.cs.meta && git commit -F - <<'EOF'
feat(domain): traveller gender from the claimed place's name lists

Exact match first, then the name without its numeral suffix; both use the
scanner comparison. No draw, so no stream moves.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 6: Birth-date tell years (`HasOtherYear`, `PickOtherYear`)

`BirthDates.Forge` stays until Task 9 (the piece-1 `CaseFactory` still calls it).

**Files:**
- Modify: `Assets/Scripts/Domain/BirthDates.cs`
- Test: `Assets/Tests/EditMode/BirthDatesTests.cs`

- [ ] **Step 1: Write the failing tests** — save as `SCRATCH/p2_t6_test.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Tests\EditMode\BirthDatesTests.cs', [
(r"""        Assert.AreEqual("sometime (?)", BirthDates.Forge("sometime", 2, 24, 0, 0, new SeededRandom(1)));
    }
}""",
r"""        Assert.AreEqual("sometime (?)", BirthDates.Forge("sometime", 2, 24, 0, 0, new SeededRandom(1)));
    }

    [Test]
    public void PickOtherYear_KeepsDayAndMonth_AndStaysInTheRange()
    {
        var rng = new SeededRandom(5);
        for (int i = 0; i < 500; i++)
        {
            string date = BirthDates.PickOtherYear("9 Apr 1843", 1780, 1900, rng);
            Assert.IsTrue(BirthDates.TryParse(date, out int d, out int m, out int y), date);
            Assert.AreEqual(9, d);
            Assert.AreEqual(3, m);
            Assert.That(y, Is.InRange(1780, 1900));
        }
    }

    [Test]
    public void PickOtherYear_NeverTakesTheCoverYear_WhenTheRangesOverlap()
    {
        // Early-modern Egypt and Greece both span 1630..1682.
        var rng = new SeededRandom(11);
        var years = new System.Collections.Generic.HashSet<int>();
        for (int i = 0; i < 2000; i++)
        {
            Assert.IsTrue(BirthDates.TryParse(BirthDates.PickOtherYear("5 May 1650", 1630, 1682, rng), out _, out _, out int y));
            Assert.AreNotEqual(1650, y);
            years.Add(y);
        }
        Assert.Greater(years.Count, 40);
    }

    [Test]
    public void PickOtherYear_AcrossTheBceCeBoundary_NeverWritesYearZero()
    {
        var rng = new SeededRandom(8);
        for (int i = 0; i < 500; i++)
        {
            Assert.IsTrue(BirthDates.TryParse(BirthDates.PickOtherYear("1 Jan 3", -30, 30, rng), out _, out _, out int y));
            Assert.AreNotEqual(0, y);
            Assert.AreNotEqual(3, y);
        }
    }

    [Test]
    public void PickOtherYear_WithOneCandidateLeft_AlwaysTakesIt()
    {
        var rng = new SeededRandom(3);
        for (int i = 0; i < 50; i++)
            Assert.AreEqual("5 May 811", BirthDates.PickOtherYear("5 May 812", 811, 812, rng));
    }

    [Test]
    public void PickOtherYear_ReversedBounds_AreTolerated()
    {
        Assert.IsTrue(BirthDates.TryParse(BirthDates.PickOtherYear("5 May 850", 900, 800, new SeededRandom(1)), out _, out _, out int y));
        Assert.That(y, Is.InRange(800, 900));
        Assert.AreNotEqual(850, y);
    }

    [Test]
    public void PickOtherYear_UsesOneDraw_IndexingTheEligibleYearsInAscendingOrder()
    {
        // 1630..1634 without the cover year 1632: 1630, 1631, 1633, 1634.
        var rng = new ScriptedRandom(ScriptStep.Range(2));
        Assert.AreEqual("5 May 1633", BirthDates.PickOtherYear("5 May 1632", 1630, 1634, rng));
        Assert.IsTrue(rng.Done);

        // 2 BCE..2 CE without year 0 and the cover year 1 CE: 2 BCE, 1 BCE, 2 CE.
        var zero = new ScriptedRandom(ScriptStep.Range(2));
        Assert.AreEqual("1 Jan 2", BirthDates.PickOtherYear("1 Jan 1", -2, 2, zero));
        Assert.IsTrue(zero.Done);
    }

    [Test]
    public void PickOtherYear_WithNoOtherYear_ReturnsNull_WithoutDrawing()
    {
        var rng = new ScriptedRandom();
        Assert.IsNull(BirthDates.PickOtherYear("Unknown", 1630, 1682, rng));
        Assert.IsNull(BirthDates.PickOtherYear("5 May 1650", 0, 0, rng));
        Assert.IsNull(BirthDates.PickOtherYear("5 May 1650", 1650, 1650, rng));
        Assert.AreEqual(0, rng.Draws);
    }

    [TestCase("Unknown", 1630, 1682, false)]
    [TestCase("5 May 1650", 0, 0, false)]
    [TestCase("5 May 1650", 1650, 1650, false)]
    [TestCase("1 Jan 1", 0, 1, false)]
    [TestCase("5 May 1650", 1630, 1682, true)]
    [TestCase("3 Jun 1450 BCE", -1460, -1440, true)]
    public void HasOtherYear_DecisionTable(string cover, int yearMin, int yearMax, bool expected)
    {
        Assert.AreEqual(expected, BirthDates.HasOtherYear(cover, yearMin, yearMax));
    }
}"""),
])
```

- [ ] **Step 2: Compile check — expect failure**

Expected: `== TimeDeskEditMode.csproj: exit 1`, `error CS0117: 'BirthDates' does not contain a definition for 'PickOtherYear'` (and `HasOtherYear`).

- [ ] **Step 3: Implement** — save as `SCRATCH/p2_t6.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Scripts\Domain\BirthDates.cs', [
(r"""        return Format(day, month, year);
    }
""",
r"""        return Format(day, month, year);
    }

    /// <summary>
    /// True when <paramref name="coverDate"/> is readable and [yearMin, yearMax]
    /// (bounds may be reversed) holds a year that is neither 0 nor the cover
    /// date's year, so a birth-date tell can be drawn. Draws nothing.
    /// </summary>
    public static bool HasOtherYear(string coverDate, int yearMin, int yearMax) =>
        TryParse(coverDate, out _, out _, out int coverYear) && OtherYearCount(coverYear, yearMin, yearMax) > 0;

    /// <summary>
    /// A birth-date tell: the cover date's day and month with a year drawn
    /// uniformly from [yearMin, yearMax] (bounds may be reversed), never year 0
    /// and never the cover year. Exactly one Range draw: an index into those
    /// years in ascending order. Null, with no draw, when <see cref="HasOtherYear"/>
    /// is false or <paramref name="rng"/> is null.
    /// </summary>
    public static string PickOtherYear(string coverDate, int yearMin, int yearMax, IRandomSource rng)
    {
        if (rng == null || !TryParse(coverDate, out int day, out int month, out int coverYear))
            return null;

        int count = OtherYearCount(coverYear, yearMin, yearMax);
        if (count <= 0)
            return null;

        if (yearMax < yearMin)
            (yearMin, yearMax) = (yearMax, yearMin);

        // Step over the skipped years (0 and the cover year), smallest first.
        long year = (long)yearMin + rng.Range(0, count);
        foreach (int skipped in coverYear < 0 ? new[] { coverYear, 0 } : new[] { 0, coverYear })
            if (skipped >= yearMin && skipped <= year)
                year++;

        return Format(day, month, (int)year);
    }

    /// <summary>How many years of [yearMin, yearMax] (either order) are neither 0 nor <paramref name="coverYear"/>.</summary>
    private static int OtherYearCount(int coverYear, int yearMin, int yearMax)
    {
        if (yearMax < yearMin)
            (yearMin, yearMax) = (yearMax, yearMin);

        long count = (long)yearMax - yearMin + 1;
        if (yearMin <= 0 && 0 <= yearMax)
            count--;
        if (coverYear != 0 && yearMin <= coverYear && coverYear <= yearMax)
            count--;

        return count > int.MaxValue ? int.MaxValue : (int)count;
    }
"""),
])
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 204, failed 0`.

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Domain/BirthDates.cs Assets/Tests/EditMode/BirthDatesTests.cs && git commit -F - <<'EOF'
feat(domain): birth-date tell years (HasOtherYear, PickOtherYear)

One uniform draw among the home range's years, skipping year 0 and the
cover year by construction; no draw when no such year exists.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 7: The provable-tell rule (`HomeCandidate`, `Forgery.IsProvableTell`)

`Lies.cs` starts with `HomeCandidate` because the tell rule takes one; Task 8 adds the planner. `Forgery.IsProvable` and its tests stay until Task 9.

**Files:**
- Create: `Assets/Scripts/Domain/Lies.cs` (+ `.meta`)
- Modify: `Assets/Scripts/Domain/Forgery.cs`
- Test: `Assets/Tests/EditMode/ForgeryTests.cs`

- [ ] **Step 1: Write the failing tests** — save as `SCRATCH/p2_t7_test.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Tests\EditMode\ForgeryTests.cs', [
(r"""        Assert.IsFalse(Forgery.IsProvable(ClueCategory.Currency, "egypt", "ancient", Today(), null, "1 Jan 5"));
    }
}""",
r"""        Assert.IsFalse(Forgery.IsProvable(ClueCategory.Currency, "egypt", "ancient", Today(), null, "1 Jan 5"));
    }

    /// <summary>Books for the tell tests: Currency and Language (no Geography book).</summary>
    private static readonly HashSet<ClueCategory> BookCategories = new HashSet<ClueCategory> { ClueCategory.Currency, ClueCategory.Language };

    /// <summary>The home under test: Babylonia, born 1810..1790 BCE.</summary>
    private static readonly HomeCandidate IraqHome = new HomeCandidate("iraq", "ancient", -1810, -1790);

    /// <summary>
    /// Today for the tell tests: the claim Egypt, the home Iraq, and Greece,
    /// whose Language equals Iraq's under the scanner comparison.
    /// </summary>
    private static FactTable World()
    {
        var t = new FactTable();
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Currency, "Deben");
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Language, "Middle Egyptian");
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Geography, "Thebes");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Currency, "Silver shekel");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Language, "Old Babylonian");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Geography, "Babylon");
        t.Add("greece", "ancient", "Periclean Athens (Ancient)", ClueCategory.Currency, "Silver drachma");
        t.Add("greece", "ancient", "Periclean Athens (Ancient)", ClueCategory.Language, " old babylonian ");
        return t;
    }

    private static bool IsTell(ClueCategory category, HomeCandidate home, string cover = "3 Jun 1450 BCE", FactTable facts = null) =>
        Forgery.IsProvableTell(category, "egypt", "ancient", cover, home, facts ?? World(), BookCategories);

    [Test]
    public void PlaceFact_WithABook_AndAHomeValueOnlyTheHomeHas_IsATell()
    {
        // Iraq's own Currency row matches the value: the home's own row is never a collision.
        Assert.IsTrue(IsTell(ClueCategory.Currency, IraqHome));
    }

    [Test]
    public void PlaceFact_WithoutABook_IsNotATell()
    {
        Assert.IsFalse(IsTell(ClueCategory.Geography, IraqHome));
    }

    [Test]
    public void PlaceFact_EqualToTheClaimUnderTheScannerComparison_IsNotATell()
    {
        FactTable t = World();
        t.Add("italy", "ancient", "Republican Rome (Ancient)", ClueCategory.Currency, " DEBEN ");
        Assert.IsFalse(IsTell(ClueCategory.Currency, new HomeCandidate("italy", "ancient", 0, 0), facts: t));
    }

    [Test]
    public void PlaceFact_TheHomeOrTheClaimLacksToday_IsNotATell()
    {
        Assert.IsFalse(IsTell(ClueCategory.Currency, new HomeCandidate("china", "ancient", 0, 0)));
        Assert.IsFalse(Forgery.IsProvableTell(ClueCategory.Currency, "china", "ancient", "1 Jan 5", IraqHome, World(), BookCategories));
    }

    [Test]
    public void PlaceFact_SharedWithAThirdPlaceToday_IsNotATell()
    {
        // Greece's " old babylonian " equals Iraq's Language under the scanner comparison.
        Assert.IsFalse(IsTell(ClueCategory.Language, IraqHome));
    }

    [Test]
    public void BirthDate_IsATell_OnlyWhenTheHomeRangeHoldsAnotherYear()
    {
        Assert.IsTrue(IsTell(ClueCategory.BirthDate, IraqHome));
        Assert.IsFalse(IsTell(ClueCategory.BirthDate, IraqHome, cover: "Unknown"));
        Assert.IsFalse(IsTell(ClueCategory.BirthDate, new HomeCandidate("greece", "ancient", 0, 0)));
        Assert.IsFalse(IsTell(ClueCategory.BirthDate, new HomeCandidate("italy", "ancient", -1450, -1450)));
    }

    [Test]
    public void Name_IsNeverATell()
    {
        Assert.IsFalse(IsTell(ClueCategory.Name, IraqHome));
    }

    [Test]
    public void MissingTableOrBooks_IsNotATell()
    {
        Assert.IsFalse(Forgery.IsProvableTell(ClueCategory.Currency, "egypt", "ancient", "1 Jan 5", IraqHome, null, BookCategories));
        Assert.IsFalse(Forgery.IsProvableTell(ClueCategory.Currency, "egypt", "ancient", "1 Jan 5", IraqHome, World(), null));
    }
}"""),
])
```

- [ ] **Step 2: Compile check — expect failure**

Expected: `== TimeDeskEditMode.csproj: exit 1`, `error CS0246: The type or namespace name 'HomeCandidate' could not be found` and `error CS0117: 'Forgery' does not contain a definition for 'IsProvableTell'`.

- [ ] **Step 3: Create `Lies.cs` with `HomeCandidate`** (`Assets/Scripts/Domain/Lies.cs`, Write tool; then `make_meta.py`)

```csharp
/// <summary>One of today's places as the lie rules see it: its ids and birth years.</summary>
public readonly struct HomeCandidate
{
    /// <summary>Nation id of the place (matches NationSO.id).</summary>
    public readonly string NationId;

    /// <summary>Era id of the place (matches EraSO.id).</summary>
    public readonly string EraId;

    /// <summary>Earliest birth year of a traveller from here (negative = BCE; 0..0 = none authored).</summary>
    public readonly int BirthYearMin;

    /// <summary>Latest birth year of a traveller from here (negative = BCE).</summary>
    public readonly int BirthYearMax;

    /// <summary>Creates a candidate.</summary>
    public HomeCandidate(string nationId, string eraId, int birthYearMin, int birthYearMax)
    {
        NationId = nationId;
        EraId = eraId;
        BirthYearMin = birthYearMin;
        BirthYearMax = birthYearMax;
    }
}
```

- [ ] **Step 4: Add `IsProvableTell`** — save as `SCRATCH/p2_t7.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Scripts\Domain\Forgery.cs', [
(r"""        return !string.IsNullOrEmpty(truth) && facts.HasOtherValue(category, truth);
    }
}""",
r"""        return !string.IsNullOrEmpty(truth) && facts.HasOtherValue(category, truth);
    }

    /// <summary>
    /// Whether a liar claiming (<paramref name="claimNationId"/>, <paramref name="claimEraId"/>)
    /// whose true home is <paramref name="home"/> can leak a provable tell in
    /// <paramref name="category"/>: names never; a birth date when the cover date
    /// is readable and the home's birth years hold another year
    /// (BirthDates.HasOtherYear); a place fact when a reference book covers the
    /// category, today's table holds both the claim's and the home's value, they
    /// differ under the scanner comparison, and no other place today shares the
    /// home's value (so the origin proof can only name the home).
    /// </summary>
    public static bool IsProvableTell(ClueCategory category, string claimNationId, string claimEraId,
                                      string coverBirthDate, HomeCandidate home, FactTable facts,
                                      ICollection<ClueCategory> bookCategories)
    {
        switch (category)
        {
            case ClueCategory.Name:
                return false;
            case ClueCategory.BirthDate:
                return BirthDates.HasOtherYear(coverBirthDate, home.BirthYearMin, home.BirthYearMax);
        }

        if (facts == null || bookCategories == null || !bookCategories.Contains(category))
            return false;

        string claimValue = facts.Get(claimNationId, claimEraId, category);
        string homeValue = facts.Get(home.NationId, home.EraId, category);
        if (string.IsNullOrEmpty(claimValue) || string.IsNullOrEmpty(homeValue) || DiscrepancyLog.ValuesMatch(claimValue, homeValue))
            return false;

        foreach (FactRow row in facts.Rows(category))
        {
            bool homesOwnRow = row.NationId == home.NationId && row.EraId == home.EraId;
            if (!homesOwnRow && DiscrepancyLog.ValuesMatch(row.Value, homeValue))
                return false;
        }

        return true;
    }
}"""),
])
```

- [ ] **Step 5: Compile check and run**

Expected: exit 0 / exit 0; `passed 212, failed 0`.

- [ ] **Step 6: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Domain/Lies.cs Assets/Scripts/Domain/Lies.cs.meta Assets/Scripts/Domain/Forgery.cs Assets/Tests/EditMode/ForgeryTests.cs && git commit -F - <<'EOF'
feat(domain): provable tell rule (Forgery.IsProvableTell, HomeCandidate)

A place fact is a tell only with a book, a claim value and a home value
that differ, and a home value no other of today's places shares (so the
origin proof names the home); a birth date only when the home range holds
another year. The piece-1 IsProvable goes with its caller in the switch.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 8: Lie planning (`Lies.Plan`, `LiePlan`)

**Files:**
- Modify: `Assets/Scripts/Domain/Lies.cs` (whole file)
- Test: `Assets/Tests/EditMode/LiesTests.cs` (+ `.meta`)

- [ ] **Step 1: Write the failing test** (`Assets/Tests/EditMode/LiesTests.cs`, Write tool; then `make_meta.py`)

```csharp
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Lie planning. Today, in order: the claim New Kingdom Egypt; a twin (Greece)
/// whose values equal Egypt's under the scanner comparison and who has no
/// birth years; Babylonia (Iraq), different in every book category and born
/// 1460..1440 BCE (around the cover year 1450 BCE); Republican Rome (Italy),
/// different only in Currency and born exactly in the cover year. Iraq's
/// values and Italy's Currency appear nowhere else. The papers follow the real
/// templates (passport: Name, BirthDate, Currency, Language; permit:
/// Technology, Currency), so Iraq's eligible tells are BirthDate, Currency,
/// Language and Technology, in that order.
/// </summary>
public class LiesTests
{
    private const string Cover = "3 Jun 1450 BCE";
    private const int CoverYear = -1450;

    private static readonly HashSet<ClueCategory> Books = new HashSet<ClueCategory> { ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology };

    private static readonly HomeCandidate Egypt = new HomeCandidate("egypt", "ancient", -1520, -1452);
    private static readonly HomeCandidate Twin = new HomeCandidate("greece", "ancient", 0, 0);
    private static readonly HomeCandidate Iraq = new HomeCandidate("iraq", "ancient", -1460, -1440);
    private static readonly HomeCandidate Italy = new HomeCandidate("italy", "ancient", -1450, -1450);
    private static readonly HomeCandidate[] Today4 = { Egypt, Twin, Iraq, Italy };

    private static FactTable Facts()
    {
        var t = new FactTable();
        Add(t, Egypt, "New Kingdom Egypt (Ancient)", "Deben", "Middle Egyptian", "Papyrus");
        Add(t, Twin, "Periclean Athens (Ancient)", " deben ", "MIDDLE EGYPTIAN", "papyrus");
        Add(t, Iraq, "Babylonia (Ancient)", "Silver shekel", "Old Babylonian", "Cylinder seal");
        Add(t, Italy, "Republican Rome (Ancient)", "Denarius", "Middle Egyptian", "Papyrus");
        return t;
    }

    private static void Add(FactTable t, HomeCandidate p, string label, string currency, string language, string technology)
    {
        t.Add(p.NationId, p.EraId, label, ClueCategory.Currency, currency);
        t.Add(p.NationId, p.EraId, label, ClueCategory.Language, language);
        t.Add(p.NationId, p.EraId, label, ClueCategory.Technology, technology);
    }

    private static DocumentField Field(ClueCategory category, string label, string value, int page) =>
        new DocumentField { category = category, label = label, value = value, page = page };

    /// <summary>Egypt's honest papers: passport, then (optionally) the permit.</summary>
    private static List<DocumentField> Papers(bool withPermit = true)
    {
        var papers = new List<DocumentField>
        {
            Field(ClueCategory.Name, "Full Name", "Nebamun", 0),
            Field(ClueCategory.BirthDate, "Date of Birth", Cover, 0),
            Field(ClueCategory.Currency, "Coin of Issue", "Deben", 0),
            Field(ClueCategory.Language, "Native Tongue", "Middle Egyptian", 0)
        };

        if (withPermit)
        {
            papers.Add(Field(ClueCategory.Technology, "Declared Device", "Papyrus", 0));
            papers.Add(Field(ClueCategory.Currency, "Bond Currency", "Deben", 1));
        }

        return papers;
    }

    private static LiePlan Plan(IRandomSource rng, int tellCount = 1, IReadOnlyList<HomeCandidate> todays = null,
                                List<DocumentField> papers = null, float chance = 0.5f, FactTable facts = null) =>
        Lies.Plan(chance, tellCount, "egypt", "ancient", Cover, todays ?? Today4, papers ?? Papers(), facts ?? Facts(), Books, rng);

    private static ScriptedRandom Script(params ScriptStep[] steps) => new ScriptedRandom(steps);
    private static ScriptStep V(float roll) => ScriptStep.Value(roll);
    private static ScriptStep R(int offset) => ScriptStep.Range(offset);

    [Test]
    public void MayLie_OnlyNonLegendaryTravellersWithAnAllowedClaimAndPapers()
    {
        Assert.IsTrue(Lies.MayLie(false, true, Papers()));
        Assert.IsFalse(Lies.MayLie(true, true, Papers()), "legendary");
        Assert.IsFalse(Lies.MayLie(false, false, Papers()), "forbidden claim");
        Assert.IsFalse(Lies.MayLie(false, true, null), "no papers");
        Assert.IsFalse(Lies.MayLie(false, true, new List<DocumentField>()), "empty papers");
    }

    [Test]
    public void ARollAtOrAboveTheChance_IsHonest_AfterExactlyOneDraw()
    {
        ScriptedRandom rng = Script(V(0.5f));
        LiePlan plan = Plan(rng, chance: 0.5f);
        Assert.AreEqual(LieOutcome.Honest, plan.Outcome);
        Assert.AreEqual(-1, plan.HomeIndex);
        CollectionAssert.IsEmpty(plan.Tells);
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void ChanceZero_NeverLies_ChanceOne_AlwaysLies()
    {
        for (int seed = 0; seed < 300; seed++)
        {
            Assert.AreEqual(LieOutcome.Honest, Plan(new SeededRandom(seed), chance: 0f).Outcome, $"seed {seed}");
            Assert.AreEqual(LieOutcome.Liar, Plan(new SeededRandom(seed), chance: 1f).Outcome, $"seed {seed}");
        }
    }

    [Test]
    public void GoldenOrder_PlaceFact_RollThenHomeThenTell()
    {
        ScriptedRandom rng = Script(V(0f), R(0), R(1));
        List<DocumentField> papers = Papers();
        LiePlan plan = Plan(rng, papers: papers);

        Assert.AreEqual(LieOutcome.Liar, plan.Outcome);
        Assert.AreEqual(2, plan.HomeIndex, "an index into todays, not into the filtered candidates (the twin was dropped)");
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency }, plan.Tells);
        Assert.IsTrue(rng.Done, "exactly three draws");

        plan.ApplyTo(papers);
        HomeCandidate home = Today4[plan.HomeIndex];
        foreach (DocumentField f in papers.Where(f => f.category == ClueCategory.Currency))
            Assert.AreEqual(Facts().Get(home.NationId, home.EraId, ClueCategory.Currency), f.value);
    }

    [Test]
    public void GoldenOrder_BirthDate_RollThenHomeThenTellThenYear()
    {
        ScriptedRandom rng = Script(V(0f), R(0), R(0), R(2));
        List<DocumentField> papers = Papers();
        LiePlan plan = Plan(rng, papers: papers);

        CollectionAssert.AreEqual(new[] { ClueCategory.BirthDate }, plan.Tells);
        Assert.IsTrue(rng.Done, "exactly four draws");

        // Iraq's years without the cover year, ascending: 1460..1451 BCE, 1449..1440 BCE; index 2 is 1458 BCE.
        plan.ApplyTo(papers);
        Assert.AreEqual("3 Jun 1458 BCE", papers.Single(f => f.category == ClueCategory.BirthDate).value);
    }

    [Test]
    public void TellPicks_FollowFirstAppearanceOrder_WithCurrencyCountedOnce()
    {
        ClueCategory TellAt(int index)
        {
            ScriptedRandom rng = Script(V(0f), R(0), R(index));
            LiePlan plan = Plan(rng);
            Assert.IsTrue(rng.Done);
            return plan.Tells.Single();
        }

        Assert.AreEqual(ClueCategory.Currency, TellAt(1));
        Assert.AreEqual(ClueCategory.Language, TellAt(2));
        Assert.AreEqual(ClueCategory.Technology, TellAt(3));
    }

    [Test]
    public void TellCount_IsCappedAtTheHomesEligibleCategories_WithoutRepeats()
    {
        ScriptedRandom rng = Script(V(0f), R(0), R(0), R(0), R(0), R(0), R(0));
        LiePlan plan = Plan(rng, tellCount: 9);
        Assert.AreEqual(4, plan.Tells.Count);
        Assert.AreEqual(4, plan.Tells.Distinct().Count());
        Assert.AreEqual(1, plan.Tells.Count(t => t == ClueCategory.Currency));
        Assert.IsTrue(rng.Done, "roll, home, four tells, birth year");
    }

    [Test]
    public void TellCountBelowOne_StillGivesOneTell()
    {
        ScriptedRandom rng = Script(V(0f), R(0), R(1));
        LiePlan plan = Plan(rng, tellCount: 0);
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency }, plan.Tells);
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void AnUnprintedCategory_IsNeverATell()
    {
        for (int seed = 0; seed < 300; seed++)
        {
            LiePlan plan = Plan(new SeededRandom(seed), tellCount: 9, papers: Papers(withPermit: false), chance: 1f);
            CollectionAssert.DoesNotContain(plan.Tells, ClueCategory.Technology, $"seed {seed}");
        }
    }

    [Test]
    public void TheTrueHome_IsNeverTheClaimOrTheTwin()
    {
        for (int seed = 0; seed < 500; seed++)
        {
            LiePlan plan = Plan(new SeededRandom(seed), chance: 1f);
            Assert.AreEqual(LieOutcome.Liar, plan.Outcome, $"seed {seed}");
            Assert.That(plan.HomeIndex, Is.EqualTo(2).Or.EqualTo(3), $"seed {seed}");
        }
    }

    [Test]
    public void OnlyTheClaimAndTheTwinToday_IsNoPossibleLie_AfterOneDraw()
    {
        ScriptedRandom rng = Script(V(0f));
        LiePlan plan = Plan(rng, todays: new[] { Egypt, Twin });
        Assert.AreEqual(LieOutcome.NoPossibleLie, plan.Outcome);
        Assert.AreEqual(-1, plan.HomeIndex);
        CollectionAssert.IsEmpty(plan.Tells);
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void EligibilityIsPerHome_ItalyOnlyGivesCurrency()
    {
        ScriptedRandom rng = Script(V(0f), R(0), R(0));
        LiePlan plan = Plan(rng, tellCount: 9, todays: new[] { Egypt, Italy });
        Assert.AreEqual(1, plan.HomeIndex);
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency }, plan.Tells);
        Assert.IsTrue(rng.Done, "no birth-year draw: Italy's only year is the cover year");
    }

    [Test]
    public void AHomeValueSharedWithAnotherPlace_IsNeverATell()
    {
        var claim = new HomeCandidate("egypt", "ancient", 0, 0);
        var iraq = new HomeCandidate("iraq", "ancient", 0, 0);
        var greece = new HomeCandidate("greece", "ancient", 0, 0);
        var facts = new FactTable();
        facts.Add("egypt", "ancient", "Egypt", ClueCategory.Currency, "Deben");
        facts.Add("iraq", "ancient", "Iraq", ClueCategory.Currency, "Silver shekel");
        facts.Add("greece", "ancient", "Greece", ClueCategory.Currency, "silver shekel");
        var papers = new List<DocumentField> { Field(ClueCategory.Currency, "Coin of Issue", "Deben", 0) };

        ScriptedRandom rng = Script(V(0f));
        LiePlan plan = Lies.Plan(1f, 1, "egypt", "ancient", Cover, new[] { claim, iraq, greece }, papers, facts, Books, rng);
        Assert.AreEqual(LieOutcome.NoPossibleLie, plan.Outcome);
        Assert.IsTrue(rng.Done);

        var italy = new HomeCandidate("italy", "ancient", 0, 0);
        facts.Add("italy", "ancient", "Italy", ClueCategory.Currency, "Denarius");
        for (int seed = 0; seed < 100; seed++)
            Assert.AreEqual(3, Lies.Plan(1f, 1, "egypt", "ancient", Cover, new[] { claim, iraq, greece, italy }, papers, facts, Books, new SeededRandom(seed)).HomeIndex, $"seed {seed}");
    }

    [Test]
    public void TheSameSeed_GivesTheSamePlan()
    {
        LiePlan x = Plan(new SeededRandom(7), tellCount: 2, chance: 1f);
        LiePlan y = Plan(new SeededRandom(7), tellCount: 2, chance: 1f);
        Assert.AreEqual(x.Outcome, y.Outcome);
        Assert.AreEqual(x.HomeIndex, y.HomeIndex);
        CollectionAssert.AreEqual(x.Tells, y.Tells);

        List<DocumentField> px = Papers(), py = Papers();
        x.ApplyTo(px);
        y.ApplyTo(py);
        CollectionAssert.AreEqual(px.Select(f => f.value).ToList(), py.Select(f => f.value).ToList());
    }

    [Test]
    public void ApplyTo_RewritesAndFlagsEveryFieldOfTheTellCategory_AndNothingElse()
    {
        List<DocumentField> papers = Papers();
        Plan(Script(V(0f), R(0), R(1))).ApplyTo(papers);

        List<DocumentField> honest = Papers();
        for (int i = 0; i < papers.Count; i++)
        {
            if (papers[i].category == ClueCategory.Currency)
            {
                Assert.AreEqual("Silver shekel", papers[i].value, papers[i].label);
                Assert.IsTrue(papers[i].isAnachronism, papers[i].label);
            }
            else
            {
                Assert.AreEqual(honest[i].value, papers[i].value, papers[i].label);
                Assert.IsFalse(papers[i].isAnachronism, papers[i].label);
            }
        }
    }

    [Test]
    public void ApplyTo_ABirthDateTell_KeepsDayAndMonth_TakesAHomeYear_NeverTheRecordYear()
    {
        for (int index = 0; index < 20; index++)
        {
            List<DocumentField> papers = Papers();
            Plan(Script(V(0f), R(0), R(0), R(index))).ApplyTo(papers);
            DocumentField born = papers.Single(f => f.category == ClueCategory.BirthDate);
            Assert.IsTrue(born.isAnachronism);
            Assert.IsTrue(BirthDates.TryParse(born.value, out int d, out int m, out int y), born.value);
            Assert.AreEqual(3, d);
            Assert.AreEqual(5, m);
            Assert.That(y, Is.InRange(-1460, -1440));
            Assert.AreNotEqual(CoverYear, y);
        }
    }

    [Test]
    public void ApplyTo_AnHonestOrImpossiblePlan_ChangesNothing()
    {
        List<DocumentField> papers = Papers();
        Plan(Script(V(0.9f))).ApplyTo(papers);
        Plan(Script(V(0f)), todays: new[] { Egypt, Twin }).ApplyTo(papers);

        List<DocumentField> honest = Papers();
        for (int i = 0; i < papers.Count; i++)
        {
            Assert.AreEqual(honest[i].value, papers[i].value);
            Assert.IsFalse(papers[i].isAnachronism);
        }
    }

    [Test]
    public void EveryTell_RegistersAgainstTheClaimTheHomeAndTheRecord_AndNothingElse()
    {
        FactTable facts = Facts();
        List<DocumentField> papers = Papers();
        Plan(Script(V(0f), R(0), R(0), R(0), R(0), R(0), R(0)), tellCount: 9, papers: papers, facts: facts).ApplyTo(papers);

        foreach (DocumentField f in papers.Where(f => f.isAnachronism))
        {
            CompareEvidence doc = CompareEvidence.FromDocumentField(f);
            if (f.category == ClueCategory.BirthDate)
            {
                Discrepancy record = new DiscrepancyLog().TryRegister(doc, CompareEvidence.ForRecordField(ClueCategory.BirthDate, Cover), "egypt", "ancient");
                Assert.AreEqual(DiscrepancyProof.RecordMismatch, record?.provedBy, f.label);
                continue;
            }

            foreach (FactRow row in facts.Rows(f.category))
            {
                Discrepancy d = new DiscrepancyLog().TryRegister(doc, row.ToEvidence(), "egypt", "ancient");
                string where = $"{f.label} vs {row.OriginLabel}";
                if (row.NationId == "egypt")
                {
                    Assert.AreEqual(DiscrepancyProof.ClaimMismatch, d?.provedBy, where);
                }
                else if (row.NationId == "iraq")
                {
                    Assert.AreEqual(DiscrepancyProof.ForeignOrigin, d?.provedBy, where);
                    Assert.AreEqual("Babylonia (Ancient)", d.actualOrigin, where);
                }
                else
                {
                    Assert.IsNull(d, where);
                }
            }
        }
    }
}
```

- [ ] **Step 2: Compile check — expect failure**

Expected: `== TimeDeskEditMode.csproj: exit 1`, `error CS0246: The type or namespace name 'LiePlan' could not be found` (also `LieOutcome`, `Lies`).

- [ ] **Step 3: Implement** — replace the whole of `Assets/Scripts/Domain/Lies.cs` (Write tool, LF):

```csharp
using System.Collections.Generic;

/// <summary>One of today's places as the lie rules see it: its ids and birth years.</summary>
public readonly struct HomeCandidate
{
    /// <summary>Nation id of the place (matches NationSO.id).</summary>
    public readonly string NationId;

    /// <summary>Era id of the place (matches EraSO.id).</summary>
    public readonly string EraId;

    /// <summary>Earliest birth year of a traveller from here (negative = BCE; 0..0 = none authored).</summary>
    public readonly int BirthYearMin;

    /// <summary>Latest birth year of a traveller from here (negative = BCE).</summary>
    public readonly int BirthYearMax;

    /// <summary>Creates a candidate.</summary>
    public HomeCandidate(string nationId, string eraId, int birthYearMin, int birthYearMax)
    {
        NationId = nationId;
        EraId = eraId;
        BirthYearMin = birthYearMin;
        BirthYearMax = birthYearMax;
    }
}

/// <summary>What one traveller's lie roll decided.</summary>
public enum LieOutcome
{
    /// <summary>The traveller really comes from the place they claim.</summary>
    Honest,

    /// <summary>The traveller comes from another of today's places; their papers leak tells.</summary>
    Liar,

    /// <summary>The roll said liar, but no other place today could give a tell; the traveller stays honest.</summary>
    NoPossibleLie
}

/// <summary>The outcome of one traveller's lie roll: the true home and the tells their papers leak.</summary>
public sealed class LiePlan
{
    /// <summary>Shared empty tell list.</summary>
    private static readonly ClueCategory[] NoTells = new ClueCategory[0];

    /// <summary>The value each tell category prints (the home's fact, or the tell birth date).</summary>
    private readonly Dictionary<ClueCategory, string> _values;

    /// <summary>Creates a plan (Lies.Plan only).</summary>
    internal LiePlan(LieOutcome outcome, int homeIndex, IReadOnlyList<ClueCategory> tells, Dictionary<ClueCategory, string> values)
    {
        Outcome = outcome;
        HomeIndex = homeIndex;
        Tells = tells;
        _values = values;
    }

    /// <summary>A plan with no home and no tells.</summary>
    internal static LiePlan Without(LieOutcome outcome) =>
        new LiePlan(outcome, -1, NoTells, new Dictionary<ClueCategory, string>());

    /// <summary>What the roll decided.</summary>
    public LieOutcome Outcome { get; }

    /// <summary>Index of the true home in the (unfiltered) list Lies.Plan was given; -1 unless <see cref="LieOutcome.Liar"/>.</summary>
    public int HomeIndex { get; }

    /// <summary>The tell categories, in pick order; empty unless <see cref="LieOutcome.Liar"/>.</summary>
    public IReadOnlyList<ClueCategory> Tells { get; }

    /// <summary>
    /// Rewrites every field whose category is a tell with the tell's value and
    /// flags it as an anachronism. Changes nothing for an Honest or
    /// NoPossibleLie plan.
    /// </summary>
    public void ApplyTo(IEnumerable<DocumentField> fields)
    {
        if (fields == null)
            return;

        foreach (DocumentField field in fields)
        {
            if (field != null && _values.TryGetValue(field.category, out string value))
            {
                field.value = value;
                field.isAnachronism = true;
            }
        }
    }
}

/// <summary>
/// Who lies about their home, where they really come from, and which tells
/// their papers leak. Pure and seeded, so every rule and the draw order are
/// tested headless. Draws on the traveller's lie stream (Seeds.ForLies), in
/// this order: the roll; for a liar, the home; one pick per tell category;
/// then the birth year when BirthDate is a tell.
/// </summary>
public static class Lies
{
    /// <summary>
    /// Whether a traveller may lie at all: not a legendary, a claim today's
    /// rules allow, and papers to leak tells on. Exempt travellers make no draw.
    /// </summary>
    public static bool MayLie(bool isLegendary, bool claimAllowed, IReadOnlyList<DocumentField> papers) =>
        !isLegendary && claimAllowed && papers != null && papers.Count > 0;

    /// <summary>
    /// Rolls one traveller's lie. With probability <paramref name="liarChance"/>
    /// the traveller lies: the true home is picked uniformly among today's other
    /// places that could give at least one tell, and
    /// max(1, min(<paramref name="tellCount"/>, that home's eligible categories))
    /// tell categories are picked uniformly without replacement. A category is
    /// eligible when the papers print it and Forgery.IsProvableTell holds; the
    /// candidates keep the papers' first-appearance order. A liar for whom no
    /// place qualifies is NoPossibleLie. A null <paramref name="rng"/> is Honest
    /// with no draw.
    /// </summary>
    public static LiePlan Plan(float liarChance, int tellCount,
                               string claimNationId, string claimEraId, string coverBirthDate,
                               IReadOnlyList<HomeCandidate> todays, IReadOnlyList<DocumentField> papers,
                               FactTable facts, ICollection<ClueCategory> bookCategories, IRandomSource rng)
    {
        if (rng == null || !(rng.Value() < liarChance))
            return LiePlan.Without(LieOutcome.Honest);

        List<ClueCategory> printed = PrintedCategories(papers);
        var candidates = new List<int>();
        var eligibleByCandidate = new List<List<ClueCategory>>();

        if (todays != null)
        {
            for (int i = 0; i < todays.Count; i++)
            {
                HomeCandidate place = todays[i];
                if (place.NationId == claimNationId && place.EraId == claimEraId)
                    continue;

                var eligible = new List<ClueCategory>();
                foreach (ClueCategory category in printed)
                    if (Forgery.IsProvableTell(category, claimNationId, claimEraId, coverBirthDate, place, facts, bookCategories))
                        eligible.Add(category);

                if (eligible.Count > 0)
                {
                    candidates.Add(i);
                    eligibleByCandidate.Add(eligible);
                }
            }
        }

        if (candidates.Count == 0)
            return LiePlan.Without(LieOutcome.NoPossibleLie);

        int pick = rng.Range(0, candidates.Count);
        int homeIndex = candidates[pick];
        HomeCandidate home = todays[homeIndex];
        List<ClueCategory> pool = eligibleByCandidate[pick];

        int count = tellCount < 1 ? 1 : tellCount > pool.Count ? pool.Count : tellCount;
        var tells = new List<ClueCategory>(count);
        for (int k = 0; k < count; k++)
        {
            int index = rng.Range(0, pool.Count);
            tells.Add(pool[index]);
            pool.RemoveAt(index);
        }

        var values = new Dictionary<ClueCategory, string>();
        foreach (ClueCategory category in tells)
            if (category != ClueCategory.BirthDate)
                values[category] = facts.Get(home.NationId, home.EraId, category);

        if (tells.Contains(ClueCategory.BirthDate))
            values[ClueCategory.BirthDate] = BirthDates.PickOtherYear(coverBirthDate, home.BirthYearMin, home.BirthYearMax, rng);

        return new LiePlan(LieOutcome.Liar, homeIndex, tells.AsReadOnly(), values);
    }

    /// <summary>The distinct categories the papers print, in first-appearance order.</summary>
    private static List<ClueCategory> PrintedCategories(IReadOnlyList<DocumentField> papers)
    {
        var printed = new List<ClueCategory>();
        if (papers == null)
            return printed;

        foreach (DocumentField field in papers)
            if (field != null && !printed.Contains(field.category))
                printed.Add(field.category);

        return printed;
    }
}
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 230, failed 0` (filter `LiesTests`: 18 passing).

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Domain/Lies.cs Assets/Tests/EditMode/LiesTests.cs Assets/Tests/EditMode/LiesTests.cs.meta && git commit -F - <<'EOF'
feat(domain): lie planning (Lies.Plan, LiePlan)

One roll on the lie stream; a liar gets a true home among today's other
places that can give a tell and the day's tell count of distinct tell
categories, each carrying the home's value. Golden-order tests pin the
stream contract; a round trip proves every tell registers.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 9: Liars travel under a cover identity; random forgery retired (wiring)

No new EditMode test: Assembly-CSharp is not reachable from the test assembly (spec §2.9). Every decision `CaseFactory`, `CaseInstance` and `ShiftScoring` make is a call into Domain code tested in Tasks 2–8; Task 13 proves the whole-day behaviour. This task also removes piece 1's forgery code and its tests (spec §3.1), which could not go earlier without breaking the build.

**Files:**
- Modify: `Assets/Scripts/Domain/FactTable.cs`, `Assets/Scripts/Domain/BirthDates.cs`, `Assets/Scripts/Domain/Forgery.cs` (whole file), `Assets/Scripts/Domain/ShiftLedger.cs`, `Assets/Scripts/Domain/DiscrepancyLog.cs` (line 261 doc)
- Modify: `Assets/Scripts/CaseInstance.cs`, `Assets/Scripts/CaseFactory.cs`, `Assets/Scripts/DayPlanSO.cs`, `Assets/Scripts/CaseBlueprintSO.cs`, `Assets/Scripts/Shift/ShiftScoring.cs`, `Assets/Scripts/GameManager.cs`
- Modify: `docs/FEATURES.md`
- Test: `Assets/Tests/EditMode/FactTableTests.cs` (whole file), `Assets/Tests/EditMode/ForgeryTests.cs` (whole file), `Assets/Tests/EditMode/BirthDatesTests.cs` (cut)

- [ ] **Step 1: Retire the piece-1 forgery tests**

Replace the whole of `Assets/Tests/EditMode/FactTableTests.cs` (Write tool, LF):

```csharp
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Today's facts: two Ancient places. Egypt uses "Deben", Babylonia "Silver shekel";
/// both list their languages. The claim under test is Egypt / Ancient.
/// </summary>
public class FactTableTests
{
    private static FactTable Today()
    {
        var t = new FactTable();
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Currency, "Deben");
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Language, "Middle Egyptian");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Currency, "Silver shekel");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Language, "Old Babylonian");
        return t;
    }

    [Test]
    public void Get_ReturnsTheFact_OrNullWhenMissing()
    {
        FactTable t = Today();
        Assert.AreEqual("Deben", t.Get("egypt", "ancient", ClueCategory.Currency));
        Assert.IsNull(t.Get("china", "ancient", ClueCategory.Currency));
        Assert.IsNull(t.Get("egypt", "ancient", ClueCategory.Technology));
    }

    [Test]
    public void Rows_KeepInsertionOrder_AndCarryOriginLabels()
    {
        var rows = Today().Rows(ClueCategory.Currency);
        CollectionAssert.AreEqual(new[] { "Deben", "Silver shekel" }, rows.Select(r => r.Value).ToArray());
        Assert.AreEqual("Babylonia (Ancient)", rows[1].OriginLabel);
        Assert.AreEqual("iraq", rows[1].NationId);
        Assert.AreEqual("ancient", rows[1].EraId);
        Assert.AreEqual(0, Today().Rows(ClueCategory.Politics).Count);
    }

    [Test]
    public void OriginLabel_IsPerPlace()
    {
        Assert.AreEqual("New Kingdom Egypt (Ancient)", Today().OriginLabel("egypt", "ancient"));
        Assert.IsNull(Today().OriginLabel("egypt", "modern"));
    }

    [Test]
    public void Add_IgnoresDuplicatesAndBlankValues()
    {
        FactTable t = Today();
        Assert.IsFalse(t.Add("egypt", "ancient", "x", ClueCategory.Currency, "Other"));
        Assert.IsFalse(t.Add("egypt", "ancient", "x", ClueCategory.Technology, "  "));
        Assert.AreEqual("Deben", t.Get("egypt", "ancient", ClueCategory.Currency));
        Assert.AreEqual(2, t.Rows(ClueCategory.Currency).Count);
    }

    [Test]
    public void ATellFromAnotherPlace_RegistersAsOriginProof_NamingThatPlace()
    {
        FactTable t = Today();
        var tell = new CompareEvidence { kind = EvidenceKind.DocumentField, category = ClueCategory.Currency, value = "Silver shekel", isAnachronism = true };
        FactRow babylon = t.Rows(ClueCategory.Currency)[1];
        Discrepancy d = new DiscrepancyLog().TryRegister(tell, babylon.ToEvidence(), "egypt", "ancient");
        Assert.NotNull(d);
        Assert.AreEqual(DiscrepancyProof.ForeignOrigin, d.provedBy);
        Assert.AreEqual("Babylonia (Ancient)", d.actualOrigin);
    }

    [Test]
    public void ATell_RegistersAsMismatch_AgainstTheClaimedPlacesRow()
    {
        FactTable t = Today();
        var tell = new CompareEvidence { kind = EvidenceKind.DocumentField, category = ClueCategory.Currency, value = "Silver shekel", isAnachronism = true };
        FactRow egypt = t.Rows(ClueCategory.Currency)[0];
        Discrepancy d = new DiscrepancyLog().TryRegister(tell, egypt.ToEvidence(), "egypt", "ancient");
        Assert.NotNull(d);
        Assert.AreEqual(DiscrepancyProof.ClaimMismatch, d.provedBy);
    }
}
```

Replace the whole of `Assets/Tests/EditMode/ForgeryTests.cs` (Write tool, LF):

```csharp
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Which fields can carry a liar's tell: only those the player can disprove.
/// Today holds the claim Egypt, the home Iraq, and Greece (whose Language
/// equals Iraq's under the scanner comparison); books exist for Currency and
/// Language only.
/// </summary>
public class ForgeryTests
{
    /// <summary>Books for the tell tests: Currency and Language (no Geography book).</summary>
    private static readonly HashSet<ClueCategory> BookCategories = new HashSet<ClueCategory> { ClueCategory.Currency, ClueCategory.Language };

    /// <summary>The home under test: Babylonia, born 1810..1790 BCE.</summary>
    private static readonly HomeCandidate IraqHome = new HomeCandidate("iraq", "ancient", -1810, -1790);

    /// <summary>
    /// Today for the tell tests: the claim Egypt, the home Iraq, and Greece,
    /// whose Language equals Iraq's under the scanner comparison.
    /// </summary>
    private static FactTable World()
    {
        var t = new FactTable();
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Currency, "Deben");
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Language, "Middle Egyptian");
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Geography, "Thebes");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Currency, "Silver shekel");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Language, "Old Babylonian");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Geography, "Babylon");
        t.Add("greece", "ancient", "Periclean Athens (Ancient)", ClueCategory.Currency, "Silver drachma");
        t.Add("greece", "ancient", "Periclean Athens (Ancient)", ClueCategory.Language, " old babylonian ");
        return t;
    }

    private static bool IsTell(ClueCategory category, HomeCandidate home, string cover = "3 Jun 1450 BCE", FactTable facts = null) =>
        Forgery.IsProvableTell(category, "egypt", "ancient", cover, home, facts ?? World(), BookCategories);

    [Test]
    public void PlaceFact_WithABook_AndAHomeValueOnlyTheHomeHas_IsATell()
    {
        // Iraq's own Currency row matches the value: the home's own row is never a collision.
        Assert.IsTrue(IsTell(ClueCategory.Currency, IraqHome));
    }

    [Test]
    public void PlaceFact_WithoutABook_IsNotATell()
    {
        Assert.IsFalse(IsTell(ClueCategory.Geography, IraqHome));
    }

    [Test]
    public void PlaceFact_EqualToTheClaimUnderTheScannerComparison_IsNotATell()
    {
        FactTable t = World();
        t.Add("italy", "ancient", "Republican Rome (Ancient)", ClueCategory.Currency, " DEBEN ");
        Assert.IsFalse(IsTell(ClueCategory.Currency, new HomeCandidate("italy", "ancient", 0, 0), facts: t));
    }

    [Test]
    public void PlaceFact_TheHomeOrTheClaimLacksToday_IsNotATell()
    {
        Assert.IsFalse(IsTell(ClueCategory.Currency, new HomeCandidate("china", "ancient", 0, 0)));
        Assert.IsFalse(Forgery.IsProvableTell(ClueCategory.Currency, "china", "ancient", "1 Jan 5", IraqHome, World(), BookCategories));
    }

    [Test]
    public void PlaceFact_SharedWithAThirdPlaceToday_IsNotATell()
    {
        // Greece's " old babylonian " equals Iraq's Language under the scanner comparison.
        Assert.IsFalse(IsTell(ClueCategory.Language, IraqHome));
    }

    [Test]
    public void BirthDate_IsATell_OnlyWhenTheHomeRangeHoldsAnotherYear()
    {
        Assert.IsTrue(IsTell(ClueCategory.BirthDate, IraqHome));
        Assert.IsFalse(IsTell(ClueCategory.BirthDate, IraqHome, cover: "Unknown"));
        Assert.IsFalse(IsTell(ClueCategory.BirthDate, new HomeCandidate("greece", "ancient", 0, 0)));
        Assert.IsFalse(IsTell(ClueCategory.BirthDate, new HomeCandidate("italy", "ancient", -1450, -1450)));
    }

    [Test]
    public void Name_IsNeverATell()
    {
        Assert.IsFalse(IsTell(ClueCategory.Name, IraqHome));
    }

    [Test]
    public void MissingTableOrBooks_IsNotATell()
    {
        Assert.IsFalse(Forgery.IsProvableTell(ClueCategory.Currency, "egypt", "ancient", "1 Jan 5", IraqHome, null, BookCategories));
        Assert.IsFalse(Forgery.IsProvableTell(ClueCategory.Currency, "egypt", "ancient", "1 Jan 5", IraqHome, World(), null));
    }
}
```

Then cut the six `Forge_*` tests — save as `SCRATCH/p2_t9_tests.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from cut import cut
W = r'E:\unity\NOPE-feat-clock'

cut(W + r'\Assets\Tests\EditMode\BirthDatesTests.cs',
    "\n    [Test]\n    public void Forge_ShiftsTheYearWithinTheShiftRange_KeepingDayAndMonth()",
    'BirthDates.Forge("sometime", 2, 24, 0, 0, new SeededRandom(1)));\n    }\n')
```

- [ ] **Step 2: Compile check and run — the old APIs are now unreferenced by tests**

Expected: exit 0 / exit 0; `passed 213, failed 0` (−4 FactTable, −7 old Forgery, −6 Forge cases).

- [ ] **Step 3: Remove the piece-1 forgery API and update Domain docs** — replace the whole of `Assets/Scripts/Domain/Forgery.cs` (Write tool, LF):

```csharp
using System.Collections.Generic;

/// <summary>
/// Which fields can carry a liar's tell: only fields the player can disprove;
/// a place-fact tell's origin proof can only name the true home. Pure, so the
/// decision table is tested headless.
/// </summary>
public static class Forgery
{
    /// <summary>
    /// Whether a liar claiming (<paramref name="claimNationId"/>, <paramref name="claimEraId"/>)
    /// whose true home is <paramref name="home"/> can leak a provable tell in
    /// <paramref name="category"/>: names never; a birth date when the cover date
    /// is readable and the home's birth years hold another year
    /// (BirthDates.HasOtherYear); a place fact when a reference book covers the
    /// category, today's table holds both the claim's and the home's value, they
    /// differ under the scanner comparison, and no other place today shares the
    /// home's value (so the origin proof can only name the home).
    /// </summary>
    public static bool IsProvableTell(ClueCategory category, string claimNationId, string claimEraId,
                                      string coverBirthDate, HomeCandidate home, FactTable facts,
                                      ICollection<ClueCategory> bookCategories)
    {
        switch (category)
        {
            case ClueCategory.Name:
                return false;
            case ClueCategory.BirthDate:
                return BirthDates.HasOtherYear(coverBirthDate, home.BirthYearMin, home.BirthYearMax);
        }

        if (facts == null || bookCategories == null || !bookCategories.Contains(category))
            return false;

        string claimValue = facts.Get(claimNationId, claimEraId, category);
        string homeValue = facts.Get(home.NationId, home.EraId, category);
        if (string.IsNullOrEmpty(claimValue) || string.IsNullOrEmpty(homeValue) || DiscrepancyLog.ValuesMatch(claimValue, homeValue))
            return false;

        foreach (FactRow row in facts.Rows(category))
        {
            bool homesOwnRow = row.NationId == home.NationId && row.EraId == home.EraId;
            if (!homesOwnRow && DiscrepancyLog.ValuesMatch(row.Value, homeValue))
                return false;
        }

        return true;
    }
}
```

Then save as `SCRATCH/p2_t9_domain.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
from cut import cut
W = r'E:\unity\NOPE-feat-clock'
D = W + r'\Assets\Scripts\Domain'

# FactTable: drop HasOtherValue, PickOtherValue, OtherValues; class doc.
cut(D + r'\FactTable.cs',
    "\n    /// <summary>\n    /// Whether today holds a value of this category",
    "        return candidates;\n    }\n")
apply(D + r'\FactTable.cs', [
(r"""/// lookup that case generation (field values, forgeries, the claim's origin
/// label) and the reference books read. Built once per day (a snapshot), so
/// papers and books always agree; Citizen Records carry the origin label the
/// case took from here.""",
r"""/// lookup that case generation (field values, tells, the claim's origin
/// label) and the reference books read. Built once per day (a snapshot), so
/// papers and books always agree; Citizen Records carry the claimed place's
/// label the case took from here."""),
])

# BirthDates: drop Forge and AddYears (and the using they needed); class doc.
cut(D + r'\BirthDates.cs',
    "\n    /// <summary>\n    /// A plausible but wrong date",
    "        return result;\n    }\n")
apply(D + r'\BirthDates.cs', [
("using System;\nusing System.Collections.Generic;\n", "using System;\n"),
(r"""/// Pure, so generation and forgery are seeded and tested headless.""",
r"""/// Pure, so generation and birth-date tells are seeded and tested headless."""),
])

# DiscrepancyLog: ValuesMatch is no longer shared with FactTable.
apply(D + r'\DiscrepancyLog.cs', [
(r"""    /// <summary>Case-insensitive, trimmed equality (mirrors the compare bar; shared with FactTable).</summary>""",
r"""    /// <summary>Case-insensitive, trimmed equality (mirrors the compare bar; shared with Forgery.IsProvableTell and TravellerGenders.FromNameLists).</summary>"""),
])

# CaseVerdict: wasForged -> wasLiar + trueHomeLabel; docs.
apply(D + r'\ShiftLedger.cs', [
(r"""    /// <summary>Denials of real forgers made without documented evidence.</summary>""",
r"""    /// <summary>Denials of liars made without documented evidence.</summary>"""),
(r"""    /// <summary>True if accepting was the correct call (genuine + allowed).</summary>""",
r"""    /// <summary>True if accepting was the correct call (honest + allowed).</summary>"""),
(r"""    /// <summary>True if the case's documents were forged (had an anachronism).</summary>
    public bool wasForged;""",
r"""    /// <summary>True if the traveller lied about their home.</summary>
    public bool wasLiar;

    /// <summary>Where the traveller really comes from: the claimed place for an honest traveller.</summary>
    public string trueHomeLabel = string.Empty;"""),
(r"""    /// <summary>True if a real forger was denied without documented evidence.</summary>""",
r"""    /// <summary>True if a liar was denied without documented evidence.</summary>"""),
])
```

- [ ] **Step 4: Wire the lie into case generation and scoring** — save as `SCRATCH/p2_t9_cs.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
from cut import cut
W = r'E:\unity\NOPE-feat-clock'
A = W + r'\Assets\Scripts'

# ---------------- CaseInstance ----------------
apply(A + r'\CaseInstance.cs', [
(r"""    /// <summary>The correct destination era.</summary>""",
r"""    /// <summary>
    /// The era the traveller claims as home and is sent to (the correct era on
    /// the legacy era-pick path). A liar's real era is trueHome.era.
    /// </summary>"""),
(r"""    /// <summary>Destination nation within the era (timeline impacts land here).</summary>""",
r"""    /// <summary>The nation the traveller claims as home (the destination; timeline impacts land here).</summary>"""),
(r"""    /// <summary>Human label of the true place, "Abbasid Baghdad (Medieval)" (claims and Citizen Records).</summary>""",
r"""    /// <summary>Label of the claimed place, "Abbasid Baghdad (Medieval)" (claim line and Citizen Records).</summary>"""),
(r"""    /// <summary>The visitor's true given name (citizen-records lookup key).</summary>""",
r"""    /// <summary>The registered given name, from the claimed place's names (a liar's cover name; citizen-records lookup key).</summary>"""),
(r"""    /// <summary>The visitor's TRUE date of birth (what the agency has on file).</summary>
    public string trueBirthDate;""",
r"""    /// <summary>
    /// The registered date of birth, from the claimed place's birth years (what
    /// the agency has on file). A birth-date tell prints a different year on the papers.
    /// </summary>
    public string trueBirthDate;

    /// <summary>Gender from the claimed place's name list the given name came from (Unknown for legendaries and "Subject #n").</summary>
    public TravellerGender gender;"""),
(r"""    /// <summary>True if any document field is an anachronism for the claim.</summary>
    public bool isForged;""",
r"""    /// <summary>
    /// Where the traveller really comes from: another of today's places for a
    /// liar, null for an honest traveller (whose home is the claim).
    /// </summary>
    public NationEraProfileSO trueHome;

    /// <summary>The true home's label, from today's FactTable like <see cref="originLabel"/> (empty for an honest traveller).</summary>
    public string trueHomeLabel = string.Empty;

    /// <summary>True when the traveller lied about their home (their papers leak tells).</summary>
    public bool IsLiar => trueHome != null;

    /// <summary>Where the traveller really comes from, as a label (verdict and logs).</summary>
    public string HomeLabel => IsLiar ? trueHomeLabel : originLabel;"""),
(r"""    /// The correct decision: accept only a genuine traveler whose destination
    /// is permitted today. Deny if forged OR the claim breaks a daily rule.
    /// </summary>
    public bool ShouldAccept => !isForged && claimAllowedByRules;""",
r"""    /// The correct decision: accept only an honest traveller whose destination
    /// is permitted today; deny a liar or a rule-breaking destination.
    /// </summary>
    public bool ShouldAccept => VerdictRules.ShouldAccept(IsLiar, claimAllowedByRules);"""),
])

# ---------------- DayPlanSO ----------------
apply(A + r'\DayPlanSO.cs', [
(r"""    /// <summary>Weighted set of eras to pick the TRUE era from (optional).</summary>""",
r"""    /// <summary>Weighted set of eras to pick the claimed (home) era from (optional).</summary>"""),
(r"""    [SerializeField] private LegendarySO[] availableLegendaries;""",
r"""    [SerializeField] private LegendarySO[] availableLegendaries;

    /// <summary>
    /// How many tells each liar's disguise leaks today (at least 1; capped per
    /// liar at the categories that can carry a tell). Written by
    /// Tools > TimeDesk > Generate World from world_source.json.
    /// </summary>
    [SerializeField, Min(1)] private int tellCount = 1;"""),
(r"""    public IReadOnlyList<LegendarySO> AvailableLegendaries => availableLegendaries;""",
r"""    public IReadOnlyList<LegendarySO> AvailableLegendaries => availableLegendaries;

    /// <summary>Tells each liar leaks today (at least 1).</summary>
    public int TellCount => tellCount;"""),
(r"""/// Weighted era entry used by DayPlanSO to pick the TRUE era.""",
r"""/// Weighted era entry used by DayPlanSO to pick the claimed (home) era."""),
])

# ---------------- CaseBlueprintSO ----------------
apply(A + r'\CaseBlueprintSO.cs', [
(r"""    /// <summary>Chance per clue line to be a contradiction (lie/anachronism).</summary>""",
r"""    /// <summary>
    /// Chance per traveller to be a liar (plus the WorldState and effect
    /// modifiers). The legacy clue path also reads it as the chance per clue
    /// line to be a contradiction.
    /// </summary>"""),
(r"""    [SerializeField, Range(0f, 1f)] private float redHerringChance = 0.10f;

    /// <summary>
    /// How far a forged birth date moves the year (x = min, y = max years,
    /// either way). The forged year stays inside the place's birth years when it can.
    /// </summary>
    [SerializeField] private Vector2Int forgedBirthYearShift = new Vector2Int(2, 24);""",
r"""    [SerializeField, Range(0f, 1f)] private float redHerringChance = 0.10f;"""),
(r"""    public float RedHerringChance => redHerringChance;

    /// <summary>Public read-only forged birth-year shift (x = min, y = max years).</summary>
    public Vector2Int ForgedBirthYearShift => forgedBirthYearShift;""",
r"""    public float RedHerringChance => redHerringChance;"""),
(r"""            totalCluesMax = totalCluesMin;

        forgedBirthYearShift.x = Mathf.Max(1, forgedBirthYearShift.x);
        forgedBirthYearShift.y = Mathf.Max(forgedBirthYearShift.x, forgedBirthYearShift.y);
    }""",
r"""            totalCluesMax = totalCluesMin;
    }"""),
])

# ---------------- CaseFactory ----------------
CF = A + r'\CaseFactory.cs'

# PopulateDocumentFields tail (forge roll .. end of method) -> return fields + new helpers.
cut(CF,
    "        if (allFields.Count == 0)\n            return;\n\n        // Chance for this case to carry a forged field",
    "            inst.isForged = true;\n        }\n    }\n",
    r"""        return allFields;
    }

    /// <summary>A place's label as today's FactTable (and so the scanner) prints it; the profile's own label when the place is not in today's table.</summary>
    private string PlaceLabel(NationEraProfileSO p) => _facts.OriginLabel(p.nation.id, p.era.id) ?? p.OriginLabel;

    /// <summary>
    /// Rolls the traveller's lie on their lie stream and applies it (Lies.Plan):
    /// a liar gets a true home among today's other places, and every field of
    /// each tell category is rewritten with that home's value. Exempt
    /// travellers (legendaries, a claim a rule forbids, no papers) draw
    /// nothing. Returns the plan, or null when the traveller is exempt.
    /// </summary>
    private LiePlan Disguise(CaseInstance inst, List<DocumentField> fields, DayPlanSO plan, CaseBlueprintSO blueprint, WorldState state, int caseIndex1Based)
    {
        if (!Lies.MayLie(inst.isLegendary, inst.claimAllowedByRules, fields))
            return null;

        // Today's places as the lie rules see them, in _todays order (HomeIndex indexes both).
        var todays = new List<HomeCandidate>(_todays.Count);
        foreach (NationEraProfileSO p in _todays)
            todays.Add(new HomeCandidate(p.nation.id, p.era.id, p.birthYearMin, p.birthYearMax));

        LiePlan lie = Lies.Plan(
            LiarChance(blueprint, state),
            plan.TellCount,
            inst.claimedNation != null ? inst.claimedNation.id : null,
            inst.claimedEra != null ? inst.claimedEra.id : null,
            inst.trueBirthDate,
            todays,
            fields,
            _facts,
            _bookCategories,
            _lieRng);

        if (lie.Outcome == LieOutcome.NoPossibleLie)
        {
            Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: rolled a liar, but no other place today differs from '{inst.originLabel}' in a printed, book-covered fact or birth year, so the traveller stays honest. Widen the day's eras or countries, or add a reference book for a printed category.");
        }
        else if (lie.Outcome == LieOutcome.Liar)
        {
            inst.trueHome = _todays[lie.HomeIndex];
            inst.trueHomeLabel = PlaceLabel(inst.trueHome);
            lie.ApplyTo(fields);
        }

        return lie;
    }

    /// <summary>
    /// The chance a traveller lies: the blueprint's contradiction chance plus
    /// tomorrow's slot modifier and active ForgeryChanceBonus effects, clamped
    /// to 0..1. The legacy clue path reads the same knob as its per-clue
    /// contradiction chance.
    /// </summary>
    private float LiarChance(CaseBlueprintSO blueprint, WorldState state) =>
        Mathf.Clamp01(
            blueprint.ContradictionChance +
            (state != null ? state.forgeryChanceModifier : 0f) +
            TimelineEffects.SumFloat(state, _lib, EffectOpType.ForgeryChanceBonus));
""")

apply(CF, [
# Class summary.
(r"""/// DayPlanSO -> picks the traveller's place (era by weight, nation among today's
/// places) -> identity (names and birth years of that place) -> documents whose
/// fields come from today's FactTable (one field may be forged with another
/// place's value). Every draw comes from seeded streams (one per traveller,
/// plus the day's rule-violator stream), so the same run and day always
/// produce the same travellers.""",
r"""/// DayPlanSO -> picks the place the traveller CLAIMS as home (era by weight,
/// nation among today's places) -> the registered identity every traveller
/// carries (names and birth years of the claimed place) -> documents whose
/// fields come from today's FactTable for the claim -> maybe a lie: a liar
/// really comes from another of today's places and their papers leak tells
/// carrying that true home's values (Lies). Every draw comes from seeded
/// streams (per traveller: the case, legacy clue and lie streams; plus the
/// day's rule-violator stream), so the same run and day always produce the
/// same travellers."""),
# Streams.
(r"""    /// <summary>The current traveller's legacy clue stream, apart from <see cref="_rng"/> so clue settings never change who forges.</summary>
    private IRandomSource _clueRng = new SeededRandom(0);

    /// <summary>Categories with a reference book (only these can prove a forged place fact).</summary>""",
r"""    /// <summary>The current traveller's legacy clue stream, apart from <see cref="_rng"/> so clue settings never change who lies.</summary>
    private IRandomSource _clueRng = new SeededRandom(0);

    /// <summary>The current traveller's lie stream (Seeds.ForLies), apart from <see cref="_rng"/> so lie tuning never changes who travellers are.</summary>
    private IRandomSource _lieRng = new SeededRandom(0);

    /// <summary>Categories with a reference book (only these can carry a place-fact tell).</summary>"""),
(r"""            _clueRng = new SeededRandom(Seeds.ForClues(caseSeed));""",
r"""            _clueRng = new SeededRandom(Seeds.ForClues(caseSeed));
            _lieRng = new SeededRandom(Seeds.ForLies(caseSeed));"""),
# GenerateSingleCase summary and comments.
(r"""    /// - Otherwise pick the true era from day weights and a place in it
    /// - If blueprint not forced, pick from possibleBlueprints
    /// - Build documents, then fill and maybe forge their fields""",
r"""    /// - Otherwise pick the claimed era from day weights and a place in it
    /// - If blueprint not forced, pick from possibleBlueprints
    /// - Build documents, fill their fields from the claim, then maybe disguise a liar"""),
(r"""        // 3) Decide true era.""",
r"""        // 3) Decide the claimed era (the traveller's stated home and destination)."""),
# Claim label through PlaceLabel; gender.
(r"""        string originLabel = place != null
            ? _facts.OriginLabel(place.nation.id, place.era.id) ?? place.OriginLabel
            : FallbackOriginLabel(nation, trueEra);
        string givenName = ResolveGivenName(legendary, place, caseIndex1Based);""",
r"""        string originLabel = place != null ? PlaceLabel(place) : FallbackOriginLabel(nation, trueEra);
        string givenName = ResolveGivenName(legendary, place, caseIndex1Based);
        TravellerGender gender = legendary != null || place == null
            ? TravellerGender.Unknown
            : TravellerGenders.FromNameLists(givenName, place.maleNames, place.femaleNames);"""),
(r"""            trueBirthDate = birthDate,
            introLine = intro""",
r"""            trueBirthDate = birthDate,
            gender = gender,
            introLine = intro"""),
# Step 7 and the case log.
(r"""        // 7) Investigation layer: stated claim, structured fields + forgery, daily rules.""",
r"""        // 7) Investigation layer: stated claim, structured fields, the lie (if any), daily rules."""),
(r"""        PopulateDocumentFields(inst, place, blueprint, state);

        Debug.Log($"[CaseFactory] Case {caseIndex1Based}: blueprint='{blueprint.name}', place='{originLabel}', archetype='{archetype?.displayName}', legendary={legendary != null}, visitor='{visitorName}', born='{birthDate}', forged={inst.isForged}, claimAllowed={inst.claimAllowedByRules}, shouldAccept={inst.ShouldAccept}.");""",
r"""        List<DocumentField> fields = PopulateDocumentFields(inst);
        LiePlan lie = Disguise(inst, fields, plan, blueprint, state, caseIndex1Based);

        string archetypeName = archetype != null ? archetype.displayName : string.Empty;
        string tells = lie != null ? string.Join(", ", lie.Tells) : string.Empty;
        Debug.Log($"[CaseFactory] Case {caseIndex1Based}: blueprint='{blueprint.name}', place='{originLabel}', archetype='{archetypeName}', legendary={legendary != null}, visitor='{visitorName}', born='{birthDate}', liar={inst.IsLiar}, home='{inst.HomeLabel}', tells=[{tells}], gender={inst.gender}, claimAllowed={inst.claimAllowedByRules}, shouldAccept={inst.ShouldAccept}.");"""),
# PopulateDocumentFields head.
(r"""    /// Fills each document's structured fields from today's facts for the
    /// case's claimed place, then (with the blueprint's contradiction chance)
    /// forges exactly one provable field with another place's value (or a
    /// shifted birth year inside the place's birth years), flagging the case.
    /// </summary>
    private void PopulateDocumentFields(CaseInstance inst, NationEraProfileSO place, CaseBlueprintSO blueprint, WorldState state)
    {
        if (inst == null || _lib == null)
            return;

        var allFields = new List<DocumentField>();""",
r"""    /// Fills each document's structured fields from today's facts for the
    /// case's claimed place (identity fields from the registered identity) and
    /// returns them in paper order. Never null: empty when there are no documents.
    /// </summary>
    private List<DocumentField> PopulateDocumentFields(CaseInstance inst)
    {
        var allFields = new List<DocumentField>();

        if (inst == null || _lib == null)
            return allFields;"""),
# Doc fixes.
(r"""        // consistent (never a forgery) and the gap visible.""",
r"""        // consistent (never a tell: a tell needs the claim's fact) and the gap visible."""),
(r"""    /// uniform pick among today's places in the true era > null (no place).""",
r"""    /// uniform pick among today's places in the claimed era > null (no place)."""),
(r"""    /// Builds the agency's citizen master record for a day's visitors. Records
    /// always carry the TRUE identity, so forged papers can be caught against
    /// them. (Future: deliberately missing/corrupted records + family history.)""",
r"""    /// Builds the agency's citizen master record for a day's visitors. Records
    /// carry the registered identity: an honest traveller's, or a liar's cover
    /// (claimed origin). They never reveal a true home. (Future: deliberately
    /// missing/corrupted records + family history.)"""),
(r"""    /// Picks the true era by the DayPlan weights.""",
r"""    /// Picks the claimed era by the DayPlan weights."""),
# Legacy clue path: same knob through LiarChance.
(r"""        // Effective contradiction chance: blueprint base + tomorrow modifier
        // (slot machine) + stacked ForgeryChanceBonus effects.
        float effectiveContradictionChance = Mathf.Clamp01(
            blueprint.ContradictionChance +
            state.forgeryChanceModifier +
            TimelineEffects.SumFloat(state, _lib, EffectOpType.ForgeryChanceBonus));""",
r"""        // Effective contradiction chance: the same knob as the liar chance
        // (blueprint base + tomorrow modifier + stacked ForgeryChanceBonus effects).
        float effectiveContradictionChance = LiarChance(blueprint, state);"""),
])

# ---------------- ShiftScoring ----------------
apply(A + r'\Shift\ShiftScoring.cs', [
(r"""    /// matches CaseInstance.ShouldAccept (accept a genuine, permitted traveler;
    /// deny a forgery or a rule-breaking destination).""",
r"""    /// matches CaseInstance.ShouldAccept (accept an honest, permitted traveller;
    /// deny a liar or a rule-breaking destination)."""),
("forged={inst?.isForged}, ", "liar={inst?.IsLiar}, "),
(r"""            wasForged = inst != null && inst.isForged,""",
r"""            wasLiar = inst != null && inst.IsLiar,
            trueHomeLabel = inst != null ? inst.HomeLabel : string.Empty,"""),
(r"""        // Evidence gate: denying a forger must be backed by documented scanner""",
r"""        // Evidence gate: denying a liar must be backed by documented scanner"""),
(r"""        if (config.requireEvidenceToDeny && evidenceCount == 0 &&
            !accepted && inst != null && inst.isForged && inst.claimAllowedByRules)""",
r"""        if (inst != null && VerdictRules.IsUnprovenDenial(config.requireEvidenceToDeny, evidenceCount, accepted, inst.IsLiar, inst.claimAllowedByRules))"""),
('"Approved travel on forged or forbidden papers."',
 '"Approved a disguised traveller or a forbidden destination."'),
])

# ---------------- GameManager ----------------
apply(A + r'\GameManager.cs', [
(r"""        Debug.Log($"[GameManager] Case {caseIndex1Based}: visitor='{inst.visitorDisplayName}', trueEra='{inst.trueEra?.id}', archetype='{inst.archetype?.displayName}', nation='{inst.nation?.displayName}', legendary={inst.isLegendary}, documents={inst.documents.Count}, clues={inst.usedClues.Count}.");""",
r"""        string claimedEraId = inst.trueEra != null ? inst.trueEra.id : string.Empty;
        string archetypeName = inst.archetype != null ? inst.archetype.displayName : string.Empty;
        string nationName = inst.nation != null ? inst.nation.displayName : string.Empty;
        Debug.Log($"[GameManager] Case {caseIndex1Based}: visitor='{inst.visitorDisplayName}', claim='{inst.originLabel}', claimedEra='{claimedEraId}', archetype='{archetypeName}', nation='{nationName}', legendary={inst.isLegendary}, liar={inst.IsLiar}, home='{inst.HomeLabel}', gender={inst.gender}, documents={inst.documents.Count}, clues={inst.usedClues.Count}.");"""),
("forged={inst.isForged}, ", "liar={verdict.wasLiar}, home='{verdict.trueHomeLabel}', "),
])
```

- [ ] **Step 5: Compile check and run**

Expected: exit 0 / exit 0; `passed 213, failed 0`.

- [ ] **Step 6: Prove nothing of the retired API is left**

Run: `cd /e/unity/NOPE-feat-clock && grep -rnE "isForged|wasForged|IsProvable\(|PickOtherValue|HasOtherValue|OtherValues|BirthDates\.Forge|AddYears|ForgedBirthYearShift|forgedBirthYearShift|Approved travel on forged" Assets --include=*.cs`
Expected: no output. (`forgedBirthYearShift` still appears in `CaseBlueprint_Investigation.asset`; Generate World drops it in Task 13.)

- [ ] **Step 7: Update the behaviour contract** — save as `SCRATCH/p2_t9_features.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\docs\FEATURES.md', [
("plus a day stream for rule violators and a separate stream for legacy clues;",
 "plus a day stream for rule violators, a separate stream for legacy clues and a per-traveller lie stream (`Seeds.ForLies`);"),
("(Name/Born rows are compare-clickable; origin + clerk note)",
 "(Name/Born rows are compare-clickable; origin + clerk note); a liar's record is their cover identity (claimed origin), so records never reveal a true home"),
("names come only from the place's period names",
 "names come only from the claimed place's period names (a liar's name is part of their cover)"),
("- [ ] Every traveller asks to go home to the place they claim; a forger's papers carry values from another of today's places (full disguises come with the identity & lies piece)",
 "- [ ] Every traveller asks to go home to the place they claim, and their name, birth date, papers and Citizen Record all come from that claim. An honest traveller really comes from there: every field agrees with the books and the record. A liar really comes from another of today's places (any country, any of today's eras) and travels under the claimed place's cover identity, but the papers leak tells carrying the true home's value: a Currency, Language or Technology tell rewrites every field of that category; a birth-date tell keeps the record's day and month with a year from the true home's birth years. Each liar leaks the day's tell count (`DayPlanSO` \"tell count\", 1 on days 1–3), capped by the categories that can carry a tell for them. The liar chance is the blueprint's contradiction chance plus the slot-machine modifier and forgery-risk effects; rule violators and legendaries never lie; a rolled liar with no possible tell stays honest and a warning is logged (tell selection, eligibility and the may-lie exemptions tested: `LiesTests`, `ForgeryTests`; the liar chance, cover records and whole-day behaviour are checked in Unity, not by the EditMode suite)\n"
 "- [ ] Traveller gender is recorded from the claimed place's name lists (\"Marcus II\" counts as Marcus; legendaries and \"Subject #n\" are unknown); it is not shown yet (tested: `TravellerGendersTests`, `NameRosterTests`)"),
("  - [ ] Mismatch proof: forged field ≠ claimed-era reference entry",
 "  - [ ] Mismatch proof: a liar's tell ≠ claimed-era reference entry"),
("  - [ ] Match proof: forged field = a *different* era/nation's entry (origin proof)",
 "  - [ ] Match proof: a liar's tell = a *different* era/nation's entry (origin proof); it names the traveller's true home"),
("  - [ ] Record proof: forged identity field ≠ agency citizen record (tested)",
 "  - [ ] Record proof: a birth-date tell ≠ agency citizen record (tested)"),
("denying a forger with **zero** documented discrepancies = citation + deduction even though the visitor lied (`requireEvidenceToDeny` toggle)",
 "denying a liar with **zero** documented discrepancies = citation + deduction even though the visitor lied (`requireEvidenceToDeny` toggle) (tested: `VerdictRulesTests`)"),
("- [ ] Only provable forgeries are generated (tested: `ForgeryTests`): a place fact is forged only when a reference book covers it, taking another of today's places' value (so the books prove it); birth dates are shifted 2–24 years (blueprint knob) but stay inside the place's birth years, provable via citizen records (tested: `BirthDatesTests`); names never forged until the missing-record mechanic lands",
 "- [ ] Only provable tells are generated (tested: `ForgeryTests`, `BirthDatesTests`): a place fact is a tell only when a reference book covers it and the true home's value differs from the claim's and belongs to no other of today's places (so the books prove it and the origin proof names the home); a birth-date tell keeps day and month and takes a year from the true home's birth years, never the record's (provable via citizen records); names, capitals and rulers are never tells"),
("- [ ] Timeline impacts apply only on ACCEPT; sends tracked per era",
 "- [ ] Timeline impacts apply only on ACCEPT and land on the claimed place (where the traveller is sent), liar or not; sends tracked per era"),
])
```

- [ ] **Step 8: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Domain/FactTable.cs Assets/Scripts/Domain/BirthDates.cs Assets/Scripts/Domain/Forgery.cs Assets/Scripts/Domain/ShiftLedger.cs Assets/Scripts/Domain/DiscrepancyLog.cs Assets/Scripts/CaseInstance.cs Assets/Scripts/CaseFactory.cs Assets/Scripts/DayPlanSO.cs Assets/Scripts/CaseBlueprintSO.cs Assets/Scripts/Shift/ShiftScoring.cs Assets/Scripts/GameManager.cs Assets/Tests/EditMode/FactTableTests.cs Assets/Tests/EditMode/ForgeryTests.cs Assets/Tests/EditMode/BirthDatesTests.cs docs/FEATURES.md && git commit -F - <<'EOF'
feat(cases): liars travel under a cover identity; retire random forgery

Today's place pick is the claim and drives the whole registered identity.
CaseFactory fills the papers from the claim, then rolls the lie on the
traveller's own stream (Seeds.ForLies): a liar gets a true home among
today's other places and leaks the day's tell count of tells carrying that
home's values. Violators, forbidden claims and legendaries never lie.
ShouldAccept and the evidence gate call VerdictRules; the verdict records
wasLiar and the true home's label. Gender is derived from the claimed
place's name lists. Removed: the forge roll, Forgery.IsProvable,
FactTable.PickOtherValue/HasOtherValue, BirthDates.Forge and the
forgedBirthYearShift knob, with their tests.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 10: Fallback prints the agency record; Records wiring warning; scanner hint

UI glue over `CitizenRegistry.Find` (tested by `CitizenRegistryTests`); no new EditMode test is possible (spec §2.9).

**Files:**
- Modify: `Assets/Scripts/UI/InvestigationUIController.cs`, `docs/FEATURES.md`

- [ ] **Step 1: Implement** — save as `SCRATCH/p2_t10.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Scripts\UI\InvestigationUIController.cs', [
(r"""    private FactTable _facts;
""",
r"""    private FactTable _facts;

    /// <summary>Today's citizen registry (set by GameManager; the text fallback prints the current traveller's record).</summary>
    private CitizenRegistry _registry;
"""),
(r"""        if (compareController != null)
            compareController.PairCompared += HandlePairCompared;
    }""",
r"""        if (compareController != null)
            compareController.PairCompared += HandlePairCompared;

        // Birth-date tells are proven only against Citizen Records (RecordMismatch).
        if (EvidenceSystemActive && recordsWindow == null)
            Debug.LogWarning("[InvestigationUIController] Citizen Records not wired: birth-date tells cannot be proven. Run Tools > TimeDesk > Build Office UI.", this);
    }"""),
(r"""    /// Auto-registers a true contradiction when the player compares a forged
    /// document field against the reference entry that disproves it.""",
r"""    /// Auto-registers a true contradiction when the player compares a liar's
    /// tell against the reference entry or record that disproves it."""),
(r"""    /// <summary>Injects the day's citizen registry into the Records app.</summary>
    public void SetCitizenRegistry(CitizenRegistry registry)
    {
        if (recordsWindow != null)""",
r"""    /// <summary>Injects the day's citizen registry into the Records app and the text fallback.</summary>
    public void SetCitizenRegistry(CitizenRegistry registry)
    {
        _registry = registry;
        if (recordsWindow != null)"""),
(r"""                "Compare a document field against the matching reference entry " +
                "for the claimed era to log evidence.";""",
r"""                "Compare a document field against the claimed place's reference entry, " +
                "the entry it really belongs to, or the Citizen Record to log evidence.";"""),
(r"""            _fallbackBody.text = BuildFallbackBody(inst, lib, _facts);
    }

    private static string BuildFallbackBody(CaseInstance inst, ContentLibrarySO lib, FactTable facts)
    {""",
r"""            _fallbackBody.text = BuildFallbackBody(inst, lib, _facts, _registry);
    }

    /// <summary>
    /// The text fallback's body: the papers, the traveller's agency record (so
    /// a birth-date tell can be spotted without the Records app) and today's books.
    /// </summary>
    private static string BuildFallbackBody(CaseInstance inst, ContentLibrarySO lib, FactTable facts, CitizenRegistry registry)
    {"""),
(r"""                foreach (DocumentField f in doc.fields)
                    sb.AppendLine($"    {f.label}: {f.value}");
            }
            sb.AppendLine();
        }""",
r"""                foreach (DocumentField f in doc.fields)
                    sb.AppendLine($"    {f.label}: {f.value}");
            }
            sb.AppendLine();

            sb.AppendLine("— AGENCY RECORD —");
            CitizenRecord record = registry != null ? registry.Find(inst.visitorGivenName) : null;
            if (record == null)
            {
                sb.AppendLine("    No record on file.");
            }
            else
            {
                sb.AppendLine($"    Name: {record.fullName}");
                sb.AppendLine($"    Born: {record.birthDate}");
                sb.AppendLine($"    Origin: {record.origin}");
            }
            sb.AppendLine();
        }"""),
])

apply(W + r'\docs\FEATURES.md', [
("so records never reveal a true home",
 "so records never reveal a true home; the rich desk warns once at start when the evidence system is active but Citizen Records is not wired (birth-date tells need it)"),
("- [ ] Fallback text-mode investigation when the rich desk isn't built",
 "- [ ] Fallback text-mode investigation when the rich desk isn't built (papers, the traveller's agency record and today's books)"),
])
```

The first pair extends the Citizen Records line (:36) that Task 9 rewrote, so the new start-up warning enters the behaviour contract in the same commit as its code.

- [ ] **Step 2: Compile check and run**

Expected: exit 0 / exit 0; `passed 213, failed 0`.

- [ ] **Step 3: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/UI/InvestigationUIController.cs docs/FEATURES.md && git commit -F - <<'EOF'
feat(ui): fallback prints the agency record; warn when Records is not wired

A birth-date tell is provable only against the record, so the text
fallback now prints the traveller's record and the rich desk warns once
when the evidence system is active without Citizen Records. The scanner
idle hint names all three proofs.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 11: The per-day tell count is authored in `world_source.json`

**Files:**
- Modify: `Assets/Editor/WorldContentGenerator.cs`, `Assets/Data/World/world_source.json`, `docs/FEATURES.md`
- Modify (scratch, not committed): `SCRATCH/build_world_source.py`

- [ ] **Step 1: Implement** — save as `SCRATCH/p2_t11.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Editor\WorldContentGenerator.cs', [
(r"""            foreach (string r in d.rules ?? Array.Empty<string>())
                if (!ruleIds.Contains(r))
                    errors.Add($"Day '{d.asset}' uses unknown rule '{r}'.");""",
r"""            foreach (string r in d.rules ?? Array.Empty<string>())
                if (!ruleIds.Contains(r))
                    errors.Add($"Day '{d.asset}' uses unknown rule '{r}'.");
            if (d.tells < 1)
                errors.Add($"Day '{d.asset}' needs \"tells\" of at least 1.");"""),
(r"""    /// Writes the day's queue, eras, countries and rules. Legendary settings are
    /// left to their authors (missing legendaries are dropped).""",
r"""    /// Writes the day's queue, tell count, eras, countries and rules. Legendary
    /// settings are left to their authors (missing legendaries are dropped)."""),
(r"""        so.FindProperty("visitorsCount").intValue = d.queue;""",
r"""        so.FindProperty("visitorsCount").intValue = d.queue;
        so.FindProperty("tellCount").intValue = d.tells;"""),
(r"""        public int queue;
        public EraWeightData[] eras;""",
r"""        public int queue;
        /// <summary>Tells each liar leaks this day (at least 1).</summary>
        public int tells;
        public EraWeightData[] eras;"""),
])

apply(W + r'\Assets\Data\World\world_source.json', [
('      "queue": 8,\n', '      "queue": 8,\n      "tells": 1,\n'),
('      "queue": 10,\n', '      "queue": 10,\n      "tells": 1,\n'),
('      "queue": 12,\n', '      "queue": 12,\n      "tells": 1,\n'),
])

apply(S + r'\build_world_source.py', [
("'queue': 8, ", "'queue': 8, 'tells': 1, "),
("'queue': 10, ", "'queue': 10, 'tells': 1, "),
("'queue': 12,", "'queue': 12, 'tells': 1,"),
])

apply(W + r'\docs\FEATURES.md', [
("(eras, countries, 40 places, rules, day plans)",
 "(eras, countries, 40 places, rules, day plans with each day's tell count)"),
])
```

- [ ] **Step 2: Prove the build script reproduces the edited source**

Run: `cd /e/unity/NOPE-feat-clock && python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/build_world_source.py" && git diff --stat -- Assets/Data/World/world_source.json && git diff -- Assets/Data/World/world_source.json | grep '^[-+] '`
Expected: `places 40 …`; `1 file changed, 3 insertions(+)`; exactly three lines `+      "tells": 1,`. If the diff shows anything else, the research inputs drifted since piece 1: run `git checkout -- Assets/Data/World/world_source.json`, re-apply only the `world_source.json` pairs of Step 1, and record the drift in the Task 13 verification record.

- [ ] **Step 3: Compile check and run**

Expected: exit 0 / exit 0; `passed 213, failed 0`.

- [ ] **Step 4: Commit** (the generated day plans are regenerated and committed in Task 13; `DayPlanSO.tellCount` defaults to 1 until then)

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Editor/WorldContentGenerator.cs Assets/Data/World/world_source.json docs/FEATURES.md && git commit -F - <<'EOF'
content(world): per-day tell count authored in world_source.json

Generate World writes days[].tells into DayPlanSO.tellCount and rejects a
day without a tell count of at least 1. Days 1-3 use 1.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 12: Lie wording in doc comments, the validator message and test vocabulary

Doc-only (plus one editor log message and test-helper renames); no behaviour changes, so `FEATURES.md` is untouched. `DiscrepancyLogTests` keeps every assertion and its test count; only its "forged" helper names, one test name, the class doc and one comment move to tell vocabulary (spec §2.8), so the Step 2 gate can cover `Assets/Tests` too.

**Files:**
- Modify: `Assets/Scripts/Domain/DocumentField.cs`, `Assets/Scripts/Domain/DiscrepancyLog.cs`, `Assets/Scripts/Domain/CitizenRegistry.cs`, `Assets/Scripts/Core/GameConfigSO.cs`, `Assets/Scripts/WorldState.cs`, `Assets/Scripts/EffectSO.cs`, `Assets/Scripts/ContentLibrarySO.cs`, `Assets/Scripts/DocumentTemplateSO.cs`, `Assets/Scripts/Timeline/TimelineService.cs`, `Assets/Scripts/UI/CitizenRecordsWindowController.cs`, `Assets/Editor/ContentLibraryValidator.cs`
- Test: `Assets/Tests/EditMode/DiscrepancyLogTests.cs` (CRLF; renames only)

- [ ] **Step 1: Apply** — save as `SCRATCH/p2_t12.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'
A = W + r'\Assets\Scripts'

apply(A + r'\Domain\DocumentField.cs', [
(r"""/// papers show, the value printed, and whether that value is an anachronism
/// (inconsistent with the claimed nation+era per the reference books).
/// Reference type so generation can flag forgeries after the list is built.""",
r"""/// papers show, the value printed, and whether that value is a liar's tell
/// (anachronistic for the claimed nation+era per the reference books or the
/// agency record). Reference type so generation can apply a lie's tells after
/// the list is built."""),
(r"""    /// <summary>True if this value is forged/anachronistic for the claim.</summary>""",
r"""    /// <summary>True if this value is a liar's tell (anachronistic for the claim).</summary>"""),
])

apply(A + r'\Domain\DiscrepancyLog.cs', [
(r"""    /// <summary>Document side: true if the value is forged for the claim.</summary>""",
r"""    /// <summary>Document side: true if the value is a liar's tell (anachronistic for the claim).</summary>"""),
(r"""    /// <summary>Forged value printed on the visitor's papers.</summary>""",
r"""    /// <summary>The tell's value printed on the visitor's papers.</summary>"""),
(r"""/// - MISMATCH proof: a forged document field differs from the reference entry""",
r"""/// - MISMATCH proof: a liar's tell differs from the reference entry"""),
(r"""/// - MATCH proof: a forged document field equals a reference entry that does""",
r"""/// - MATCH proof: a liar's tell equals a reference entry that does"""),
(r"""        // Only a genuinely forged field is a contradiction; a coincidental""",
r"""        // Only a liar's tell is a contradiction; a coincidental"""),
])

apply(A + r'\Domain\CitizenRegistry.cs', [
(r"""    /// <summary>Where/when this citizen belongs ("Latia — Ancient Rome").</summary>""",
r"""    /// <summary>The registered origin label (a liar's is their claimed cover origin; records never show a true home).</summary>"""),
])

apply(A + r'\Core\GameConfigSO.cs', [
(r"""    /// When true, denying a forger without documented scanner evidence earns a""",
r"""    /// When true, denying a liar without documented scanner evidence earns a"""),
])

apply(A + r'\WorldState.cs', [
(r"""    /// <summary>Additive modifier to contradiction/forgery chance for generated cases.</summary>""",
r"""    /// <summary>Additive modifier to the liar chance (and the legacy per-clue contradiction chance).</summary>"""),
])

apply(A + r'\EffectSO.cs', [
(r"""    ForgeryChanceBonus,      // floatParam = +contradiction chance (0..1)""",
r"""    ForgeryChanceBonus,      // floatParam = +liar chance (0..1)"""),
])

apply(A + r'\ContentLibrarySO.cs', [
(r"""    /// <summary>Categories that have a reference book (only these can prove a forged place fact).</summary>""",
r"""    /// <summary>Categories that have a reference book (only these can carry a place-fact tell).</summary>"""),
])

apply(A + r'\DocumentTemplateSO.cs', [
(r"""    /// claimed nation+era; a forged case flips one to an anachronism.""",
r"""    /// claimed nation+era; a liar's tells rewrite every field of a tell category."""),
])

apply(A + r'\Timeline\TimelineService.cs', [
(r"""    /// to the CHOSEN era, so impacts land on (case nation, chosen era) — authored
    /// profile if one exists, ad-hoc score keys otherwise.""",
r"""    /// to the CHOSEN era, so impacts land on (claimed nation, chosen era): where
    /// the traveller is sent, liar or not — authored profile if one exists,
    /// ad-hoc score keys otherwise."""),
])

apply(A + r'\UI\CitizenRecordsWindowController.cs', [
(r"""/// disprove forged identity papers (RecordMismatch evidence).""",
r"""/// disprove a liar's birth-date tell (RecordMismatch evidence)."""),
])

apply(W + r'\Assets\Editor\ContentLibraryValidator.cs', [
("and their birth dates are never forged.", "and their birth dates can never carry a birth-date tell."),
])

# DiscrepancyLogTests: tell vocabulary. The helpers are used many times, so this
# is a counted byte-level rename (subs.apply wants one match per pair); it never
# touches line endings. Assertions and the test count are unchanged.
import pathlib
p = pathlib.Path(W + r'\Assets\Tests\EditMode\DiscrepancyLogTests.cs')
b = p.read_bytes()
for old, new, n in [
    (b'ForgedBirthDate_VsRecord_Registers_AsRecordMismatch', b'BirthDateTell_VsRecord_Registers_AsRecordMismatch', 1),
    (b'ForgedIdentityField', b'TellIdentityField', 3),
    (b'ForgedDocField', b'TellDocField', 17),
    (b'/// The forged papers print "Aqueduct"', b'/// A liar\'s papers print "Aqueduct"', 1),
    (b'// Forged value equals', b'// The tell equals', 1),
]:
    if b.count(old) != n:
        sys.exit(f'{p}: expected {n} x {old!r}, found {b.count(old)}')
    b = b.replace(old, new)
p.write_bytes(b)
print('renamed', p)
```

- [ ] **Step 2: Check no stale "forg" wording is left outside the kept flavour and names**

Run: `cd /e/unity/NOPE-feat-clock && grep -rni "forg" Assets/Scripts Assets/Editor Assets/Tests --include=*.cs | grep -viE "forgeryChanceModifier|ForgeryChanceBonus|class Forgery|Forgery\.|legendary/forgery/pay|forget|forgiven"`
Expected: no output. (Kept by the spec §2.7: the `Forgery` class name (and so `ForgeryTests`), `forgeryChanceModifier`/`ForgeryChanceBonus` names and the logs that print them, `SlotOutcomeSO`'s "legendary/forgery/pay"; the English word "forget" is not about forgery.)

- [ ] **Step 3: Compile check and run**

Expected: exit 0 / exit 0; `passed 213, failed 0` (filter `DiscrepancyLogTests` shows the same cases as before, one renamed to `BirthDateTell_VsRecord_Registers_AsRecordMismatch`).

- [ ] **Step 4: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Domain/DocumentField.cs Assets/Scripts/Domain/DiscrepancyLog.cs Assets/Scripts/Domain/CitizenRegistry.cs Assets/Scripts/Core/GameConfigSO.cs Assets/Scripts/WorldState.cs Assets/Scripts/EffectSO.cs Assets/Scripts/ContentLibrarySO.cs Assets/Scripts/DocumentTemplateSO.cs Assets/Scripts/Timeline/TimelineService.cs Assets/Scripts/UI/CitizenRecordsWindowController.cs Assets/Editor/ContentLibraryValidator.cs Assets/Tests/EditMode/DiscrepancyLogTests.cs && git commit -F - <<'EOF'
docs: lie wording in doc comments, the validator message and tests

Anachronisms are now a liar's tells, records carry the cover origin and
the forgery knobs are the liar chance. DiscrepancyLogTests' helpers use
tell vocabulary (assertions unchanged). Flavour names stay.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 13: Unity verification

Proves spec §6 in the branch's own Unity 6000.4.11f1 through two temporary `-executeMethod` sessions (HOUSE_RULES "Verifying in Unity"). Reports go to SCRATCH. The builder is unchanged, so OfficeScene is **not** rebuilt.

Session A stays in `Assets/Editor` on purpose, although it calls `TestRunnerApi` (HOUSE_RULES puts TestRunnerApi automation in `Assets/Tests/EditMode`): the world check needs Assembly-CSharp types (`CaseFactory`, `ContentLibrarySO`), which `TimeDeskEditMode` cannot see, and the generated `Assembly-CSharp-Editor.csproj` already references `UnityEditor.TestRunner`, so one editor script runs both in one Unity session. Piece 1 split the same work into `Assets/Editor/_TimeDeskWorldCheck.cs` and `Assets/Tests/EditMode/_TimeDeskAutomation.cs` (its Unity logs, `SCRATCH/unity_worldcheck2.log` and `SCRATCH/unity_world.log`), at the cost of a second Unity run; both layouts are temporary and never committed.

**When a check fails**, first decide where the defect is:
- **In a temporary `_TimeDesk*` script** (the automation itself is wrong): fix that script, commit nothing, and re-run only the affected session (Step 4 for session A, Step 6 for session B).
- **In the product:** fix it at its source, Domain first with a failing EditMode test, pass the offline gate (Step 1), commit it as `fix: …`, then re-run both sessions (Step 4, then Step 6).

**Files:**
- Create (temporary, never committed): `Assets/Editor/_TimeDeskLiesAutomation.cs`, `Assets/Editor/_TimeDeskLiesPlaySmoke.cs`
- Commit: `Assets/Data/Investigation/DayPlan_Inv_Day1.asset`, `…Day2.asset`, `…Day3.asset`, `Assets/Data/Investigation/CaseBlueprint_Investigation.asset`
- Modify: `docs/superpowers/specs/2026-09-24-identity-lies-design.md` (verification record)

What the automation must prove (spec §6):
- **Content:** Generate World twice, the second run changes no file; the day plans gain `tellCount: 1`; the blueprint loses `forgedBirthYearShift`; Validate Content Library reports no issues.
- **World check** on seeds 12345 and 999 × days 1–3, plus a 200-seed sweep, with cases from `new CaseFactory(lib, lib.BuildFactTable(plan)).GenerateDayCases(plan, new WorldState { day = d }, Seeds.Day(seed, d))`: (1) determinism (same seed identical dumps incl. liar flags, homes, tells and every paper value; a different seed differs); (2) stream contract (claims, names, registered dates, roles, allowed flags and violator slots equal `lies_baseline.txt` line for line); (3) liars: true home in today's places, not the claim, at least one flagged field, tell categories = min(tellCount, eligible), every field of a tell category rewritten; (4) place-fact tells equal `facts.Get(trueHome, category)`, birth-date tells keep the record's day/month and take a home-range year that is not the record's; (5) registrable: claim row → `ClaimMismatch`, home row → `ForeignOrigin` with `actualOrigin == trueHomeLabel`, every other row of the category → nothing, record → `RecordMismatch`; (6) honest travellers fully consistent, nothing flagged, `ShouldAccept == claimAllowedByRules`; (7) planned violators, forbidden claims and legendaries are honest; (8) `BuildRegistry(cases).Find(name)` shows the cover (name, registered date, claim label, never the true home's label); (9) gender is Male/Female and matches the claimed place's list holding the suffix-stripped name, Unknown 0 times; (10) sweep: liar rate close to 0.5 of eligible travellers, all four tell kinds occur, cross-era liars occur on days 2–3, no NoPossibleLie warning.
- **EditMode suite** through `TestRunnerApi`: all pass except the known third-party `UnitySkills.Tests.Core.PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly` (report it, ignore it).
- **Day-1 play-through** on the committed OfficeScene: honest → Accept → correct, no citation; place-fact liar → compare the tell with the **true home's** book row → "… which belongs to <true home>" → Deny → correct, no citation; birth-date liar → open the Citizen Records app (it is built closed, and opening it is what wires its Name/Born rows) → search for the cover name shows the claimed origin and the cover date → the Born row fills the compare bar → compare it with Date of Birth → "BIRTH DATE INCORRECT" → Deny → correct; one liar denied without evidence → unproven-denial citation; forced closing → results show "Undocumented denials: 1"; no warnings or errors (so the Records wiring warning stays silent).

- [ ] **Step 1: Offline gate**

Run the compile check and the test run. Expected: exit 0 / exit 0; `passed 213, failed 0`. `git status --short` shows only the two untracked files.

- [ ] **Step 2: Check no Unity editor is open on the worktree** (PowerShell)

Run: `Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Select-Object ProcessId, CommandLine`
Expected: none on `NOPE-feat-clock`.

- [ ] **Step 3: Write session A** (`Assets/Editor/_TimeDeskLiesAutomation.cs`, Write tool)

```csharp
// TEMPORARY automation for piece 2 (identity & lies); never committed.
// Generate World twice (idempotency), Validate Content Library, the world
// check (spec §6 step 3), then the EditMode suite. Reports go to the scratchpad.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class TimeDeskLiesAutomation
{
    private const string Scratch = @"C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad";
    private static readonly string ReportPath = Path.Combine(Scratch, "lies_automation_report.txt");
    private static readonly string WorldPath = Path.Combine(Scratch, "lies_world_check.txt");
    private static readonly string BaselinePath = Path.Combine(Scratch, "lies_baseline.txt");
    private const string LibraryPath = "Assets/Data/Content Library/ContentLibrary_Main.asset";

    private static int _problems;

    private static void Report(string line) => File.AppendAllText(ReportPath, line + "\n");
    private static void World(string line) => File.AppendAllText(WorldPath, line + "\n");

    private static void Problem(string line)
    {
        _problems++;
        World("PROBLEM " + line);
    }

    public static void Run()
    {
        File.WriteAllText(ReportPath, "start\n");
        File.WriteAllText(WorldPath, string.Empty);
        try
        {
            RunMenu("Tools/TimeDesk/Generate World");
            Dictionary<string, string> before = HashDataFolder();
            RunMenu("Tools/TimeDesk/Generate World");
            Dictionary<string, string> after = HashDataFolder();
            List<string> changed = before.Keys.Union(after.Keys)
                .Where(k => !before.TryGetValue(k, out string a) || !after.TryGetValue(k, out string b) || a != b)
                .ToList();
            Report($"idempotent second run: changedFiles={changed.Count} {string.Join(", ", changed)}");

            for (int day = 1; day <= 3; day++)
                Report($"DayPlan_Inv_Day{day} has 'tellCount: 1' = {File.ReadAllText($"Assets/Data/Investigation/DayPlan_Inv_Day{day}.asset").Contains("tellCount: 1")}");
            Report($"blueprint still has forgedBirthYearShift = {File.ReadAllText("Assets/Data/Investigation/CaseBlueprint_Investigation.asset").Contains("forgedBirthYearShift")}");

            RunMenu("Tools/TimeDesk/Validate Content Library");
            WorldCheck();
            Report($"world check problems={_problems} (details: {WorldPath})");
        }
        catch (Exception e)
        {
            Report("AUTOMATION FAILED " + e);
            EditorApplication.Exit(2);
            return;
        }

        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Callbacks());
        api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
    }

    private static void RunMenu(string menu)
    {
        var lines = new List<string>();
        Application.LogCallback capture = (message, stack, type) => lines.Add($"[{type}] {message}");
        Application.logMessageReceived += capture;
        bool ran = EditorApplication.ExecuteMenuItem(menu);
        Application.logMessageReceived -= capture;
        Report($"menu '{menu}' ran={ran}");
        foreach (string l in lines.Where(l => !l.StartsWith("[Log]") || l.Contains("[WorldContentGenerator]") || l.Contains("[ContentLibraryValidator]")))
            Report("  " + l);
    }

    private static Dictionary<string, string> HashDataFolder()
    {
        using var sha = SHA1.Create();
        return Directory.GetFiles("Assets/Data", "*", SearchOption.AllDirectories)
            .ToDictionary(p => p.Replace('\\', '/'), p => Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(p))));
    }

    private sealed class Coverage
    {
        public int eligible, liars, crossEraLiars, legendaries;
        public readonly Dictionary<ClueCategory, int> tellKinds = new Dictionary<ClueCategory, int>();
    }

    private static void WorldCheck()
    {
        var lib = AssetDatabase.LoadAssetAtPath<ContentLibrarySO>(LibraryPath);
        string[] baseline = File.Exists(BaselinePath) ? File.ReadAllLines(BaselinePath) : Array.Empty<string>();
        if (baseline.Length == 0)
            Problem($"no baseline at {BaselinePath} (Task 0 not run)");

        int noLieWarnings = 0;
        Application.LogCallback watch = (message, stack, type) =>
        {
            if (type == LogType.Warning && message.Contains("rolled a liar, but no other place"))
                noLieWarnings++;
        };
        Application.logMessageReceived += watch;
        try
        {
            var named = new Coverage();
            foreach (int seed in new[] { 12345, 999 })
            {
                for (int day = 1; day <= 3; day++)
                {
                    List<string> lines = CheckDay(lib, seed, day, named, dump: true);
                    string prefix = $"seed={seed} day={day} ";
                    List<string> expected = baseline.Where(l => l.StartsWith(prefix)).ToList();
                    if (!expected.SequenceEqual(lines))
                        Problem($"stream contract, seed {seed} day {day}:\n  baseline:\n    {string.Join("\n    ", expected)}\n  now:\n    {string.Join("\n    ", lines)}");
                }
            }

            var sweep = new Coverage();
            for (int seed = 1; seed <= 200; seed++)
                for (int day = 1; day <= 3; day++)
                    CheckDay(lib, seed, day, sweep, dump: false);

            float rate = sweep.eligible == 0 ? 0f : sweep.liars / (float)sweep.eligible;
            World($"coverage (200 seeds x days 1-3): eligible={sweep.eligible} liars={sweep.liars} rate={rate:0.000} " +
                  $"tells=[{string.Join(", ", sweep.tellKinds.Select(k => $"{k.Key}={k.Value}"))}] crossEraLiars(days 2-3)={sweep.crossEraLiars} " +
                  $"legendaries={sweep.legendaries + named.legendaries}");
            if (rate < 0.45f || rate > 0.55f)
                Problem($"liar rate {rate:0.000} is not close to 0.5");
            foreach (ClueCategory kind in new[] { ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology, ClueCategory.BirthDate })
                if (!sweep.tellKinds.ContainsKey(kind))
                    Problem($"no {kind} tell in the sweep");
            if (sweep.crossEraLiars == 0)
                Problem("no cross-era liar on days 2-3");
        }
        finally
        {
            Application.logMessageReceived -= watch;
        }

        World($"NoPossibleLie warnings: {noLieWarnings}");
        if (noLieWarnings > 0)
            Problem($"{noLieWarnings} NoPossibleLie warning(s)");
    }

    private static List<string> CheckDay(ContentLibrarySO lib, int seed, int day, Coverage cov, bool dump)
    {
        DayPlanSO plan = lib.GetDayPlan(day);
        FactTable facts = lib.BuildFactTable(plan);
        List<NationEraProfileSO> todays = lib.TodaysProfiles(plan);
        HashSet<ClueCategory> books = lib.ReferenceBookCategories();

        List<CaseInstance> cases = Generate(lib, plan, seed, day, out Dictionary<int, NationEraProfileSO> violators);
        CitizenRegistry registry = CaseFactory.BuildRegistry(cases);

        if (dump)
        {
            string first = Dump(cases);
            if (first != Dump(Generate(lib, plan, seed, day, out _)))
                Problem($"seed {seed} day {day}: the same seed gave different travellers");
            if (first == Dump(Generate(lib, plan, seed + 1, day, out _)))
                Problem($"seed {seed} day {day}: a different seed gave the same travellers");
            World($"=== seed {seed} day {day} ({plan.name}, tellCount={plan.TellCount}, places={todays.Count})");
            World(first);
        }

        var lines = new List<string>();
        for (int i = 0; i < cases.Count; i++)
        {
            bool violator = violators.ContainsKey(i + 1);
            lines.Add(BaselineLine(seed, day, i + 1, cases[i], violator));
            CheckCase(lib, plan, facts, todays, books, registry, cases[i], $"seed {seed} day {day} slot {i + 1}", violator, day, cov);
        }

        return lines;
    }

    private static List<CaseInstance> Generate(ContentLibrarySO lib, DayPlanSO plan, int seed, int day, out Dictionary<int, NationEraProfileSO> violators)
    {
        var factory = new CaseFactory(lib, lib.BuildFactTable(plan));
        List<CaseInstance> cases = factory.GenerateDayCases(plan, new WorldState { day = day }, Seeds.Day(seed, day));
        violators = (Dictionary<int, NationEraProfileSO>)typeof(CaseFactory)
            .GetField("_violators", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(factory);
        return cases;
    }

    /// <summary>Exactly Task 0's baseline format.</summary>
    private static string BaselineLine(int seed, int day, int slot, CaseInstance c, bool violator) =>
        $"seed={seed} day={day} slot={slot} claim='{c.originLabel}' name='{c.visitorGivenName}' born='{c.trueBirthDate}' role='{(c.archetype != null ? c.archetype.displayName : "")}' allowed={c.claimAllowedByRules} violator={violator}";

    private static string Dump(List<CaseInstance> cases) => string.Join("\n", cases.Select(c =>
        $"  {c.visitorDisplayName} | {c.claimLine} | born {c.trueBirthDate} | liar={c.IsLiar} home='{c.HomeLabel}' gender={c.gender} allowed={c.claimAllowedByRules} accept={c.ShouldAccept}\n      " +
        string.Join("; ", c.documents.SelectMany(d => d.fields).Select(f => $"{f.label}={f.value}{(f.isAnachronism ? " [TELL]" : "")}"))));

    private static void CheckCase(ContentLibrarySO lib, DayPlanSO plan, FactTable facts, List<NationEraProfileSO> todays,
                                  HashSet<ClueCategory> books, CitizenRegistry registry, CaseInstance c, string where,
                                  bool violator, int day, Coverage cov)
    {
        List<DocumentField> fields = c.documents.SelectMany(d => d.fields).ToList();
        string cn = c.claimedNation != null ? c.claimedNation.id : null;
        string ce = c.claimedEra != null ? c.claimedEra.id : null;

        if (c.isLegendary)
            cov.legendaries++;

        // (7) Violators, forbidden claims and legendaries are honest.
        if (c.IsLiar && (violator || !c.claimAllowedByRules || c.isLegendary))
            Problem($"{where}: a violator, forbidden claim or legendary lies");

        if (Lies.MayLie(c.isLegendary, c.claimAllowedByRules, fields))
        {
            cov.eligible++;
            if (c.IsLiar)
                cov.liars++;
        }

        // (8) Records show the cover.
        CitizenRecord record = registry.Find(c.visitorGivenName);
        if (record == null || record.fullName != c.visitorGivenName || record.birthDate != c.trueBirthDate || record.origin != c.originLabel)
            Problem($"{where}: the record is not the registered cover identity");
        else if (c.IsLiar && record.origin == c.trueHomeLabel)
            Problem($"{where}: the record reveals the true home");

        // (9) Gender.
        TravellerGender expectedGender = ExpectedGender(lib.GetProfile(c.claimedNation, c.claimedEra), c.visitorGivenName);
        if (c.gender == TravellerGender.Unknown || c.gender != expectedGender)
            Problem($"{where}: gender {c.gender}, expected {expectedGender} for '{c.visitorGivenName}'");

        if (!c.IsLiar)
        {
            // (6) Honest travellers are fully consistent.
            foreach (DocumentField f in fields)
            {
                string want = f.category == ClueCategory.Name ? c.visitorGivenName
                    : f.category == ClueCategory.BirthDate ? c.trueBirthDate
                    : facts.Get(cn, ce, f.category);
                if (f.isAnachronism || f.value != want)
                    Problem($"{where}: honest field {f.label}='{f.value}' (expected '{want}', flagged={f.isAnachronism})");
            }

            if (c.ShouldAccept != c.claimAllowedByRules)
                Problem($"{where}: honest ShouldAccept={c.ShouldAccept} but claimAllowed={c.claimAllowedByRules}");
            return;
        }

        // (3) Liars.
        NationEraProfileSO home = c.trueHome;
        if (!todays.Contains(home))
            Problem($"{where}: true home '{c.trueHomeLabel}' is not one of today's places");
        if (home.nation == c.claimedNation && home.era == c.claimedEra)
            Problem($"{where}: the true home is the claim");
        if (c.trueHomeLabel != facts.OriginLabel(home.nation.id, home.era.id))
            Problem($"{where}: trueHomeLabel '{c.trueHomeLabel}' is not today's FactTable label");
        if (c.ShouldAccept)
            Problem($"{where}: a liar should be accepted");
        if (day >= 2 && home.era != c.claimedEra)
            cov.crossEraLiars++;

        List<ClueCategory> tellKinds = fields.Where(f => f.isAnachronism).Select(f => f.category).Distinct().ToList();
        if (tellKinds.Count == 0)
            Problem($"{where}: a liar without a tell");
        foreach (ClueCategory kind in tellKinds)
            cov.tellKinds[kind] = (cov.tellKinds.TryGetValue(kind, out int n) ? n : 0) + 1;

        var candidate = new HomeCandidate(home.nation.id, home.era.id, home.birthYearMin, home.birthYearMax);
        int eligible = fields.Select(f => f.category).Distinct()
            .Count(cat => Forgery.IsProvableTell(cat, cn, ce, c.trueBirthDate, candidate, facts, books));
        if (tellKinds.Count != Math.Min(plan.TellCount, eligible))
            Problem($"{where}: {tellKinds.Count} tell categories, expected min({plan.TellCount}, {eligible})");

        bool registrable = false;
        foreach (DocumentField f in fields.Where(f => tellKinds.Contains(f.category)))
        {
            if (!f.isAnachronism)
            {
                Problem($"{where}: {f.label} is in a tell category but was not rewritten");
                continue;
            }

            CompareEvidence doc = CompareEvidence.FromDocumentField(f);

            if (f.category == ClueCategory.BirthDate)
            {
                // (4) Record's day and month, a home year that is not the record's.
                bool parsed = BirthDates.TryParse(f.value, out int d, out int m, out int y) &
                              BirthDates.TryParse(c.trueBirthDate, out int rd, out int rm, out int ry);
                int lo = Math.Min(home.birthYearMin, home.birthYearMax);
                int hi = Math.Max(home.birthYearMin, home.birthYearMax);
                if (!parsed || d != rd || m != rm || y < lo || y > hi || y == ry)
                    Problem($"{where}: birth-date tell '{f.value}' vs record '{c.trueBirthDate}', home years {lo}..{hi}");

                // (5) Record proof.
                Discrepancy r = new DiscrepancyLog().TryRegister(doc, CompareEvidence.ForRecordField(ClueCategory.BirthDate, record != null ? record.birthDate : null), cn, ce);
                if (r == null || r.provedBy != DiscrepancyProof.RecordMismatch)
                    Problem($"{where}: the birth-date tell does not register against the record");
                else
                    registrable = true;
                continue;
            }

            // (4) A place-fact tell carries the home's value.
            string homeValue = facts.Get(home.nation.id, home.era.id, f.category);
            if (f.value != homeValue)
                Problem($"{where}: {f.label}='{f.value}' is not the home's '{homeValue}'");

            // (5) Mismatch on the claim's row, origin proof naming the home on its row, nothing elsewhere.
            foreach (FactRow row in facts.Rows(f.category))
            {
                Discrepancy found = new DiscrepancyLog().TryRegister(doc, row.ToEvidence(), cn, ce);
                bool isClaim = row.NationId == cn && row.EraId == ce;
                bool isHome = row.NationId == home.nation.id && row.EraId == home.era.id;
                if (isClaim && (found == null || found.provedBy != DiscrepancyProof.ClaimMismatch))
                    Problem($"{where}: {f.label} does not register a mismatch against the claim's row");
                else if (isHome && (found == null || found.provedBy != DiscrepancyProof.ForeignOrigin || found.actualOrigin != c.trueHomeLabel))
                    Problem($"{where}: {f.label} does not name the true home against its row (got '{(found != null ? found.actualOrigin : "nothing")}')");
                else if (!isClaim && !isHome && found != null)
                    Problem($"{where}: {f.label} registers against a third place '{row.OriginLabel}'");
                else if (isClaim || isHome)
                    registrable = true;
            }
        }

        if (!registrable)
            Problem($"{where}: the liar has no registrable tell");
    }

    private static TravellerGender ExpectedGender(NationEraProfileSO place, string givenName)
    {
        if (place == null)
            return TravellerGender.Unknown;

        string name = NameRoster.BaseName(givenName);
        bool male = (place.maleNames ?? Array.Empty<string>()).Any(n => string.Equals(n.Trim(), name, StringComparison.OrdinalIgnoreCase));
        bool female = (place.femaleNames ?? Array.Empty<string>()).Any(n => string.Equals(n.Trim(), name, StringComparison.OrdinalIgnoreCase));
        return male == female ? TravellerGender.Unknown : male ? TravellerGender.Male : TravellerGender.Female;
    }

    private sealed class Callbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            Report($"tests passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount} state={result.ResultState}");
            EditorApplication.Exit(result.FailCount == 0 ? 0 : 3);
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (!result.HasChildren && result.TestStatus == TestStatus.Failed)
                Report($"FAIL {result.FullName}: {result.Message}");
        }
    }
}
```

- [ ] **Step 4: Run session A** (PowerShell, timeout 600000 ms)

```powershell
Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe' -ArgumentList @('-projectPath','E:\unity\NOPE-feat-clock','-executeMethod','TimeDeskLiesAutomation.Run','-logFile','C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\unity_lies_automation.log') -PassThru -Wait
```
Expected in `SCRATCH/lies_automation_report.txt`: both Generate World runs `ran=True` with `World generated: 6 eras, 8 nations, 40 places, 3 rules, 3 day plans`; `idempotent second run: changedFiles=0`; three `has 'tellCount: 1' = True`; `blueprint still has forgedBirthYearShift = False`; the validator logs `no issues found`; `world check problems=0`; `tests passed=342 failed=1` (285 − 156 + 213) with the only `FAIL` line being `UnitySkills.Tests.Core.PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly`. Exit code 3 comes from that known failure. In `SCRATCH/lies_world_check.txt`: no `PROBLEM` line, a coverage line with `rate` between 0.45 and 0.55, all four tell kinds, `crossEraLiars` above 0, and `NoPossibleLie warnings: 0`.

- [ ] **Step 5: Write session B** (`Assets/Editor/_TimeDeskLiesPlaySmoke.cs`, Write tool)

```csharp
// TEMPORARY play-mode smoke for piece 2 (identity & lies); never committed.
// Picks a day-1 seed whose slots 1-4 hold one honest traveller and three
// liars (one with a first-page place-fact tell, one with only a birth-date
// tell), plays them through the real OfficeScene UI, forces closing time and
// checks the report. Survives play-mode domain reloads via SessionState.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class TimeDeskLiesPlaySmoke
{
    private const string Scratch = @"C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad";
    private static readonly string ReportPath = Path.Combine(Scratch, "lies_playsmoke_report.txt");
    private static readonly string SaveBackup = Path.Combine(Scratch, "nope_save_backup.json");

    private const string StepKey = "LiesSmoke.Step";
    private const string TimeKey = "LiesSmoke.Time";
    private const string SlotKey = "LiesSmoke.Slot";
    private const string SeedKey = "LiesSmoke.Seed";
    private const string RolesKey = "LiesSmoke.Roles";
    private const string NamesKey = "LiesSmoke.Names";
    private const string HomeKey = "LiesSmoke.Home";
    private const string FailsKey = "LiesSmoke.Fails";
    private const string LogKey = "LiesSmoke.Log";
    private const string HadSaveKey = "LiesSmoke.HadSave";

    static TimeDeskLiesPlaySmoke()
    {
        if (SessionState.GetInt(StepKey, 0) == 0)
            return;
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
    }

    public static void Run()
    {
        File.WriteAllText(ReportPath, "start\n");
        var lib = AssetDatabase.LoadAssetAtPath<ContentLibrarySO>("Assets/Data/Content Library/ContentLibrary_Main.asset");
        DayPlanSO day1 = lib.GetDayPlan(1);

        int seed = 0;
        string[] roles = null;
        List<CaseInstance> queue = null;
        for (int s = 1; s <= 5000 && roles == null; s++)
        {
            queue = new CaseFactory(lib, lib.BuildFactTable(day1)).GenerateDayCases(day1, new WorldState { day = 1 }, Seeds.Day(s, 1));
            roles = AssignRoles(queue);
            seed = s;
        }

        if (roles == null)
        {
            Report("FAIL no seed in 1..5000 has one honest traveller, a place-fact liar and a birth-date liar in slots 1-4");
            EditorApplication.Exit(5);
            return;
        }

        string names = string.Join("|", queue.Take(4).Select(c => c.visitorGivenName));
        Report($"seed={seed} roles={string.Join(",", roles)} names={names}");

        // A fresh run on that seed: move the player's save aside (restored in Finish; the
        // path is reported so Step 6 can restore it by hand if Unity dies first) and pin
        // the seed in RunConfig.asset, saved to disk so it survives the scene load and the
        // play-mode domain reload (Step 8 reverts the asset).
        bool hadSave = File.Exists(SaveSystem.SavePath);
        Report($"save path={SaveSystem.SavePath} hadSave={hadSave}");
        if (hadSave)
        {
            File.Copy(SaveSystem.SavePath, SaveBackup, true);
            File.Delete(SaveSystem.SavePath);
        }
        var runConfig = new SerializedObject(Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath));
        runConfig.FindProperty("fixedRunSeed").intValue = seed;
        runConfig.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();

        SessionState.SetInt(SeedKey, seed);
        SessionState.SetString(RolesKey, string.Join(",", roles));
        SessionState.SetString(NamesKey, names);
        SessionState.SetInt(HadSaveKey, hadSave ? 1 : 0);
        SessionState.SetInt(SlotKey, 1);
        SessionState.SetInt(FailsKey, 0);
        SessionState.SetString(LogKey, string.Empty);

        EditorSceneManager.OpenScene("Assets/Scenes/OfficeScene.unity", OpenSceneMode.Single);
        Next(1);
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
        EditorApplication.EnterPlaymode();
    }

    /// <summary>Roles for slots 1-4 ("accept", "origin", "record", "unproven"), or null when the queue does not fit.</summary>
    private static string[] AssignRoles(List<CaseInstance> queue)
    {
        if (queue.Count < 5)
            return null;

        List<CaseInstance> first = queue.Take(4).ToList();
        if (first.Count(c => !c.IsLiar) != 1)
            return null;

        int record = first.FindIndex(c => c.IsLiar && TellKinds(c).SequenceEqual(new[] { ClueCategory.BirthDate }));
        int origin = first.FindIndex(c => c.IsLiar && FirstPagePlaceTell(c) != null);
        if (record < 0 || origin < 0)
            return null;

        return first.Select((c, i) => !c.IsLiar ? "accept" : i == record ? "record" : i == origin ? "origin" : "unproven").ToArray();
    }

    private static List<ClueCategory> TellKinds(CaseInstance c) =>
        c.documents.SelectMany(d => d.fields).Where(f => f.isAnachronism).Select(f => f.category).Distinct().ToList();

    private static DocumentField FirstPagePlaceTell(CaseInstance c) =>
        c.documents.SelectMany(d => d.fields).FirstOrDefault(f => f.isAnachronism && f.category != ClueCategory.BirthDate && f.page == 0);

    private static void Tick()
    {
        int step = SessionState.GetInt(StepKey, 0);
        try
        {
            if (step == 1 && EditorApplication.isPlaying && Elapsed > 4f) StartShift();
            else if (step == 2 && Elapsed > 1f) CallTraveller();
            else if (step == 3 && Elapsed > 1f) Decide();
            else if (step == 4 && Elapsed > 1f) CheckVerdict();
            else if (step == 5 && Elapsed > 1f) ForceClosing();
            else if (step == 6 && Elapsed > 2f) CheckResults();
            else if (step == 7 && !EditorApplication.isPlaying && Elapsed > 1f) Finish();
        }
        catch (Exception e)
        {
            Check(false, $"exception at step {step}: {e}");
            if (EditorApplication.isPlaying)
            {
                Next(7);
                EditorApplication.ExitPlaymode();
            }
            else
            {
                Finish();
            }
        }
    }

    private static void StartShift()
    {
        RunManager run = RunManager.Instance;
        int seed = SessionState.GetInt(SeedKey, 0);
        string got = run != null ? $"seed {run.World.runSeed}, day {run.World.day}" : "no RunManager";
        Check(run != null && run.World.runSeed == seed && run.World.day == 1, $"run pinned to seed {seed}, day 1 (got {got})");

        GameObject briefing = GameObject.Find("BriefingPanel");
        Transform start = briefing != null ? briefing.transform.Find("Paper/ActionButton") : null;
        if (start == null)
            throw new InvalidOperationException("no BriefingPanel/Paper/ActionButton");
        start.GetComponent<Button>().onClick.Invoke();
        Next(2);
    }

    private static void CallTraveller()
    {
        var ready = (Clickable)Field(Object.FindFirstObjectByType<GameManager>(), "readySign");
        if (!ready.Interactable)
        {
            if (Elapsed > 20f)
                throw new InvalidOperationException($"READY never armed for slot {SessionState.GetInt(SlotKey, 1)}");
            return;
        }

        ready.onClick.Invoke();
        Next(3);
    }

    private static void Decide()
    {
        int slot = SessionState.GetInt(SlotKey, 1);
        string role = SessionState.GetString(RolesKey, string.Empty).Split(',')[slot - 1];
        string expectedName = SessionState.GetString(NamesKey, string.Empty).Split('|')[slot - 1];
        var ui = Object.FindFirstObjectByType<InvestigationUIController>();
        var inst = (CaseInstance)Field(ui, "_currentCase");
        Check(inst != null && inst.visitorGivenName == expectedName,
              $"slot {slot}: the desk shows the precomputed traveller '{expectedName}' (got '{(inst != null ? inst.visitorGivenName : "none")}')");
        SessionState.SetString(HomeKey, inst.HomeLabel);

        if (role == "origin")
        {
            DocumentField tell = FirstPagePlaceTell(inst);
            DocumentInstance doc = inst.documents.First(d => d.fields.Contains(tell));
            RequestDocument(ui, doc);
            ClickRow(DocumentRows(doc), tell.label, doc.template.displayName);
            ClickRow(BookRows(tell.category), inst.trueHomeLabel, $"the {tell.category} book");
            string scanner = ((TMP_Text)Field(ui, "scannerText")).text;
            Check(ui.EvidenceCount == 1 && scanner.Contains($"which belongs to {inst.trueHomeLabel}"),
                  $"slot {slot}: {tell.label} vs the true home's row logs the origin proof ({scanner.Split('\n')[0]})");
        }
        else if (role == "record")
        {
            // The builder makes RecordsWindow inactive ("opened by its icon"), so the
            // controller's Awake, which wires the Name/Born rows, has not run yet.
            // Open the app first, as the player would; Awake then clears the search
            // box (ShowIdle), so type the name only afterwards.
            var records = (CitizenRecordsWindowController)Field(ui, "recordsWindow");
            var chrome = records.GetComponent<OSWindowChrome>();
            if (chrome != null)
                chrome.Open();
            else
                records.gameObject.SetActive(true);
            Check(records.gameObject.activeInHierarchy, $"slot {slot}: the Citizen Records app is open");

            ((TMP_InputField)Field(records, "searchInput")).text = inst.visitorGivenName;
            records.Search();
            string origin = ((TMP_Text)Field(records, "originText")).text;
            string born = ((TMP_Text)Field(records, "bornValueText")).text;
            Check(origin.Contains(inst.originLabel) && !origin.Contains(inst.trueHomeLabel) && born == inst.trueBirthDate,
                  $"slot {slot}: the record shows the cover (origin '{origin}', born '{born}')");

            // The Born row must feed the compare bar (slot A), or the record proof below can never register.
            ((GameObject)Field(records, "bornRow")).GetComponent<Button>().onClick.Invoke();
            object slotA = Field(Field(records, "compareController"), "_a");
            var picked = (CompareEvidence)Field(slotA, "evidence");
            Check((bool)Field(slotA, "set") && picked.kind == EvidenceKind.RecordField && picked.category == ClueCategory.BirthDate,
                  $"slot {slot}: clicking Born puts the record's birth date in the compare bar (got {picked.kind} {picked.category})");

            DocumentField dob = inst.documents.SelectMany(d => d.fields).First(f => f.category == ClueCategory.BirthDate);
            DocumentInstance passport = inst.documents.First(d => d.fields.Contains(dob));
            RequestDocument(ui, passport);
            ClickRow(DocumentRows(passport), dob.label, passport.template.displayName);
            string scanner = ((TMP_Text)Field(ui, "scannerText")).text;
            Check(ui.EvidenceCount == 1 && scanner.Contains("BIRTH DATE INCORRECT"),
                  $"slot {slot}: Born vs {dob.label} logs the record proof ({scanner.Split('\n')[0]})");
        }

        ((Button)Field(ui, role == "accept" ? "acceptButton" : "denyButton")).onClick.Invoke();
        Next(4);
    }

    private static void CheckVerdict()
    {
        int slot = SessionState.GetInt(SlotKey, 1);
        string role = SessionState.GetString(RolesKey, string.Empty).Split(',')[slot - 1];
        string home = SessionState.GetString(HomeKey, string.Empty);
        CaseVerdict v = Object.FindFirstObjectByType<GameManager>().Ledger.verdicts.Last();

        if (role == "accept")
            Check(v.accepted && v.correct && !v.citationIssued && !v.wasLiar, $"slot {slot}: honest traveller accepted, correct, no citation");
        else if (role == "unproven")
            Check(!v.accepted && !v.correct && v.unprovenDenial && v.wasLiar && v.citationText.Contains("Deviation denied without documented evidence"),
                  $"slot {slot}: a liar denied without evidence gets the unproven-denial citation");
        else
            Check(!v.accepted && v.correct && !v.citationIssued && v.wasLiar && v.trueHomeLabel == home,
                  $"slot {slot}: {role} liar denied with evidence, correct, no citation, true home '{v.trueHomeLabel}'");

        if (v.citationIssued)
            ((Button)Field(Object.FindFirstObjectByType<OfficeUIController>(), "citationContinueButton")).onClick.Invoke();

        SessionState.SetInt(SlotKey, slot + 1);
        Next(slot < 4 ? 2 : 5);
    }

    private static void ForceClosing()
    {
        Object.FindFirstObjectByType<ShiftClockDriver>().Clock.Tick(100000f);
        Next(6);
    }

    private static void CheckResults()
    {
        var flow = Object.FindFirstObjectByType<DayFlowUIController>();
        var panel = (GameObject)Field(flow, "resultsPanel");
        string body = ((TMP_Text)Field(flow, "resultsBodyText")).text;
        Check(panel.activeInHierarchy && body.Contains("Undocumented denials: 1"),
              $"results panel shows 'Undocumented denials: 1' ({body.Replace('\n', '/')})");
        Next(7);
        EditorApplication.ExitPlaymode();
    }

    private static void Finish()
    {
        bool hadSave = SessionState.GetInt(HadSaveKey, 0) == 1;
        if (hadSave && File.Exists(SaveBackup))
        {
            File.Copy(SaveBackup, SaveSystem.SavePath, true);
            File.Delete(SaveBackup);
        }
        else if (!hadSave && File.Exists(SaveSystem.SavePath))
        {
            File.Delete(SaveSystem.SavePath);
        }

        string log = SessionState.GetString(LogKey, string.Empty);
        Check(log.Length == 0, log.Length == 0 ? "warnings/errors: none" : "warnings/errors:\n" + log);
        int fails = SessionState.GetInt(FailsKey, 0);
        Report($"fails={fails}");

        SessionState.SetInt(StepKey, 0);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        EditorApplication.Exit(fails == 0 ? 0 : 6);
    }

    private static void RequestDocument(InvestigationUIController ui, DocumentInstance doc)
    {
        string label = $"Request {doc.template.displayName}";
        var panel = (InteractionPanelController)Field(ui, "interactionPanel");
        foreach (Button b in panel.GetComponentsInChildren<Button>(true))
        {
            TMP_Text t = b.GetComponentInChildren<TMP_Text>(true);
            if (b.gameObject.activeSelf && t != null && t.text == label)
            {
                b.onClick.Invoke();
                return;
            }
        }

        throw new InvalidOperationException($"no intercom action '{label}'");
    }

    private static List<GameObject> DocumentRows(DocumentInstance doc)
    {
        foreach (DocumentWindowController w in Object.FindObjectsByType<DocumentWindowController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (ReferenceEquals(Field(w, "_doc"), doc))
                return (List<GameObject>)Field(w, "_rows");
        throw new InvalidOperationException($"no window for '{doc.template.displayName}'");
    }

    private static List<GameObject> BookRows(ClueCategory category)
    {
        foreach (ReferenceBookWindowController w in Object.FindObjectsByType<ReferenceBookWindowController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (Field(w, "_book") is ReferenceBookSO book && book.category == category)
                return (List<GameObject>)Field(w, "_rows");
        throw new InvalidOperationException($"no {category} book window");
    }

    private static void ClickRow(List<GameObject> rows, string heading, string where)
    {
        foreach (GameObject row in rows)
        {
            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0 && texts[0].text == heading)
            {
                row.GetComponent<Button>().onClick.Invoke();
                return;
            }
        }

        throw new InvalidOperationException($"no row '{heading}' in {where}");
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Log)
            SessionState.SetString(LogKey, SessionState.GetString(LogKey, string.Empty) + $"[{type}] {message}\n");
    }

    private static object Field(object target, string name) =>
        target.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);

    private static float Elapsed => (float)EditorApplication.timeSinceStartup - SessionState.GetFloat(TimeKey, 0f);

    private static void Next(int step)
    {
        SessionState.SetInt(StepKey, step);
        SessionState.SetFloat(TimeKey, (float)EditorApplication.timeSinceStartup);
    }

    private static void Report(string line) => File.AppendAllText(ReportPath, line + "\n");

    private static void Check(bool ok, string what)
    {
        Report((ok ? "PASS " : "FAIL ") + what);
        if (!ok)
            SessionState.SetInt(FailsKey, SessionState.GetInt(FailsKey, 0) + 1);
    }
}
```

- [ ] **Step 6: Run session B** (PowerShell, timeout 600000 ms)

```powershell
Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe' -ArgumentList @('-projectPath','E:\unity\NOPE-feat-clock','-executeMethod','TimeDeskLiesPlaySmoke.Run','-logFile','C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\unity_lies_playsmoke.log') -PassThru -Wait
```
Expected: exit code 0; `SCRATCH/lies_playsmoke_report.txt` holds the chosen `seed=… roles=… names=…` and `save path=… hadSave=…`, only `PASS` lines (run pinned; per slot: the precomputed traveller, the origin proof naming the true home, the Records app open, the record showing the cover, the Born row in the compare bar and the `BIRTH DATE INCORRECT` proof, each verdict as its role requires; `Undocumented denials: 1`; `warnings/errors: none`), and `fails=0`. `SCRATCH/nope_save_backup.json` must not remain.

**Recovery if Unity died before `Finish()`** (tool timeout, crash, a hang in play mode): the player's real save may still be in the scratchpad. The save path is shared with the main `E:\unity\NOPE` project (same company and product name), so restore it before anything else (PowerShell):

```powershell
$s = 'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
$m = Select-String -Path "$s\lies_playsmoke_report.txt" -Pattern '^save path=(.+) hadSave=(True|False)$' | Select-Object -First 1
$path = $m.Matches[0].Groups[1].Value; $hadSave = $m.Matches[0].Groups[2].Value -eq 'True'
if (Test-Path "$s\nope_save_backup.json") { Copy-Item "$s\nope_save_backup.json" $path -Force; Remove-Item "$s\nope_save_backup.json" }
elseif (-not $hadSave -and (Test-Path $path)) { Remove-Item $path }
```

The first branch puts the player's save back and deletes the backup; the second removes a save the smoke run itself created when there was none before (what `Finish()` would have done). Then fix the cause and re-run Step 6.

- [ ] **Step 7: Offline re-check**

Run the compile check and the test run. Expected: exit 0 / exit 0; `passed 213, failed 0`.

- [ ] **Step 8: Hygiene**

```bash
cd /e/unity/NOPE-feat-clock && rm -f Assets/Editor/_TimeDeskLiesAutomation.cs Assets/Editor/_TimeDeskLiesAutomation.cs.meta Assets/Editor/_TimeDeskLiesPlaySmoke.cs Assets/Editor/_TimeDeskLiesPlaySmoke.cs.meta && git checkout -- Assembly-CSharp.csproj Assembly-CSharp-Editor.csproj TimeDesk.Domain.csproj TimeDeskEditMode.csproj NOPE.sln ProjectSettings "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset" Assets/Scenes/OfficeScene.unity Assets/Resources/RunConfig.asset && git status --short && git diff --stat
```
Expected: modified only `Assets/Data/Investigation/DayPlan_Inv_Day1.asset`, `DayPlan_Inv_Day2.asset`, `DayPlan_Inv_Day3.asset` (each `+  tellCount: 1`) and `Assets/Data/Investigation/CaseBlueprint_Investigation.asset` (`-  forgedBirthYearShift: {x: 2, y: 24}`); untracked only `NOPE-feat-clock.sln` and `TimeDesk.Visuals.csproj`. Any other changed file under `Assets/Data` means Generate World is not idempotent: stop and investigate.

- [ ] **Step 9: Commit the regenerated content**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Data/Investigation/DayPlan_Inv_Day1.asset Assets/Data/Investigation/DayPlan_Inv_Day2.asset Assets/Data/Investigation/DayPlan_Inv_Day3.asset Assets/Data/Investigation/CaseBlueprint_Investigation.asset && git commit -F - <<'EOF'
content(world): regenerate day plans (tell count 1) and re-save the blueprint

Generate World writes tellCount: 1 to DayPlan_Inv_Day1..3 and the re-saved
CaseBlueprint_Investigation drops the retired forgedBirthYearShift key.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

- [ ] **Step 10: Append the verification record to the spec**

Append this section to the end of `docs/superpowers/specs/2026-09-24-identity-lies-design.md` (after "Review notes"), filling every `{…}` from the named report line (the spec file is LF; use the Edit tool):

```markdown
## 8. Verification ({date of the run})

Run in the branch's own Unity 6000.4.11f1 editor through temporary `-executeMethod` scripts (not committed):

- Baseline at `14bd22e` (before any code change): seeds 12345 and 999, days 1–3, 60 slots dumped.
- Generate World ran twice; the second run changed no file. The day plans gained `tellCount: 1` and the blueprint lost `forgedBirthYearShift`. Validate Content Library: no issues.
- World check (seeds 12345 and 999 × days 1–3, plus 200 seeds × days 1–3): {problems from "world check problems="} problems. The same seed gives the same travellers, liars, homes, tells and paper values; a different seed differs. Claims, names, registered dates, roles and violator slots equal the baseline line for line. Every liar's true home is another of today's places; each tell carries the home's value and registers (mismatch on the claim's row, origin proof naming the home on its row, record proof for birth dates), and no third place's row registers anything. Honest travellers are fully consistent; violators are honest; records show the cover; every traveller is Male or Female. Sweep: liar rate {rate} of {eligible} eligible travellers; tells {the "tells=[…]" list}; {crossEraLiars} cross-era liars on days 2–3; no NoPossibleLie warning.
- EditMode suite: {passed} passed. The one failure is the known third-party `UnitySkills.Tests.Core.PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly` (scene-dependent, unrelated). Offline Domain run: 213/213.
- Scripted day-1 play-through on the committed OfficeScene (run seed {seed}; the builder is unchanged, so the scene was not rebuilt): the honest traveller was accepted without a citation; comparing a liar's {category} tell with the true home's book row logged "… which belongs to {true home}" and the denial was correct; the birth-date liar's Citizen Record showed the claimed origin and the cover date, comparing Born with Date of Birth logged "BIRTH DATE INCORRECT" and the denial was correct; denying a liar without evidence issued the unproven-denial citation; forcing closing time showed "Undocumented denials: 1". No warnings or errors; the Records wiring warning stayed silent.
```

- [ ] **Step 11: Commit the record**

```bash
cd /e/unity/NOPE-feat-clock && git add docs/superpowers/specs/2026-09-24-identity-lies-design.md && git commit -F - <<'EOF'
docs(spec): identity & lies verification record

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```
Final `git status --short`: only `?? NOPE-feat-clock.sln` and `?? TimeDesk.Visuals.csproj`.

---

## Spec coverage

| Spec | Task |
|---|---|
| D1, R4, R10, R14 (claim fields, `trueHome`, `IsLiar`, `HomeLabel`, `PlaceLabel`, verdict label) | 9 |
| D2, D6, D9, E2, R1, R3, R6, R7, R11, R13 (lie model, exemptions, eligibility, draw order, collisions) | 7, 8, 9 |
| D3 (records show the cover) | 9 (docs), 12 (docs), 13 (checked) |
| D4, R2, R5 (tell kinds, birth-date years, forgery retirement) | 6, 7, 9 |
| E1 (a tell rewrites every field of its category) | 8 |
| D5 (home among `_todays`, no new index) | 9 |
| D7 (`Seeds.ForLies`) | 2, 9 |
| D8, E3 (`VerdictRules`, evidence gate) | 3, 9 |
| D10, R8 (gender) | 4, 5, 9 |
| E4, R12 (copy) | 9 (citation), 10 (idle hint), 12 (docs, `DiscrepancyLogTests` vocabulary) |
| R9 (`LiarChance` shared with the legacy clue path) | 9 |
| R15 (fallback record, wiring warning and its FEATURES line) | 10 |
| §2.6 knobs (`tellCount`, generator, `world_source.json`, build script) | 9, 11, 13 |
| §3.1 code and tests removed | 9 |
| §3.3 FEATURES lines | 9, 10, 11 |
| §5 tests (`ScriptedRandom`, `ScriptedRandomTests`, `LiesTests`, `ForgeryTests`, `BirthDatesTests`, `FactTableTests`, `SeedsTests`, `VerdictRulesTests`, `TravellerGendersTests`, `NameRosterTests`, `WeightedRandomTests`) | 1–9 |
| §2.8 / §5 `DiscrepancyLogTests` tell vocabulary | 12 |
| §6 verification (baseline, content, world check, suite, play-through, hygiene) | 0, 13 |

---

## Review notes

An independent review of this plan (2026-09-24) raised nine findings. Each was checked against the code at `14bd22e`, and all were applied; none was rejected. Two parts of suggested fixes were not taken as written (the `GetPersistentEventCount` check in 1 and the "piece-1 precedent" wording in 4), and where a finding offered a choice (3, 8, 9) the note says which was taken.

1. **Major: the record proof could never log (Task 13 Step 5).** Confirmed: `OfficeSceneUIBuilder.BuildOSWindow` ends with `SetActive(false)` ("opened by its icon"), OfficeScene saves `RecordsWindow` with `m_IsActive: 0`, and nothing but its desktop icon opens it, so `CitizenRecordsWindowController.Awake` (which wires the Born row) never ran. Applied: the record branch opens the app through its `OSWindowChrome` (whose `window` field the builder serializes) before typing, because `Awake` clears the search box. Refinement: the suggested `onClick.GetPersistentEventCount` check was not used, because it counts only persistent listeners and `WireRow` adds a runtime one, so it would read 0 even when the wiring works. The smoke instead checks, right after the Born click, that `CompareController._a` holds a `RecordField` / `BirthDate` evidence, so a wiring failure is reported where it happens.
2. **Failure policy (Task 13 intro).** Applied: a defect in a `_TimeDesk*` script is fixed there without a commit and only the affected session re-runs; a product defect is fixed Domain-first with a failing test, committed as `fix: …`, and both sessions re-run.
3. **FEATURES for the Records wiring warning (Task 10, spec §3.3).** Applied to the Citizen Records line (:36), which Task 9 already rewrites, in the Task 10 commit that adds the warning; spec §3.3 lists it too.
4. **Session A in `Assets/Editor` (Task 13).** Applied, with a correction: the finding called this the piece-1 precedent, but the piece-1 Unity logs show piece 1 split the work (`Assets/Editor/_TimeDeskWorldCheck.cs` for the world check, `Assets/Tests/EditMode/_TimeDeskAutomation.cs` for the suite). The Task 13 note states the real reason for one editor file and names the piece-1 layout.
5. **`ScriptedRandomTests` (Task 1, spec R11/§5).** Applied test-first in Task 1 (four tests), and every running total moved by 4 (160 after Task 1, 213 after Task 9, 342 in Unity). The Unity NUnit is 3.5 (`com.unity.ext.nunit` 2.0.5), where `Assert.Fail` only throws `AssertionException`, so `Assert.Throws<AssertionException>` observes the helper's failures without failing the test itself.
6. **Seed pin (Task 13 Step 5).** Applied: the pin is written with `SerializedObject` and saved from the start (`RunConfig.asset` is reverted in Step 8); the conditional fallback note is gone.
7. **Save recovery (Task 13 Step 6).** Applied: `Run()` reports `save path=… hadSave=…`, and Step 6 has a PowerShell recovery that restores the backup (or removes a save the smoke created) if Unity dies before `Finish()`.
8. **Spec EOL notes.** Applied in the spec by correcting the notes rather than dropping them: `DiscrepancyLog.cs`, `CitizenRegistry.cs` and `InvestigationUIController.cs` are CRLF at `14bd22e`, as this plan's Conventions already said. Every other per-file EOL note in the spec was re-checked against the bytes and is right.
9. **"forg" in `DiscrepancyLogTests`.** Took option (b): Task 12 renames the helpers to `TellDocField`/`TellIdentityField`, the test to `BirthDateTell_VsRecord_Registers_AsRecordMismatch`, the class doc to "A liar's papers print…" and one comment, and the Step 2 gate now also covers `Assets/Tests`. The test count is unchanged. Spec §2.8 and §5 list the file; `DiscrepancyLogTests.cs` is CRLF, so it joins the Conventions CRLF list.

Task 0 no longer commits the spec and this plan: they were committed on `feat/identity-lies` as `docs(spec): identity & lies (piece 2) design` and `docs(plan): identity & lies implementation plan` before execution starts.
