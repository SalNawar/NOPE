# Dialog & Questions (piece 3) Implementation Plan

> **For agentic workers:** execute task-by-task (REQUIRED SUB-SKILL: superpowers:subagent-driven-development or superpowers:executing-plans). Steps use checkbox (`- [ ]`) syntax. Spec: `docs/superpowers/specs/2026-09-24-dialog-questions-design.md`. House rules: `C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\HOUSE_RULES.md` (read it before Task 0).

**Goal:** The intercom becomes an interview: travellers answer questions that open up over the first days, a liar can slip in speech as well as on paper, a spoken slip is proven like a printed one, and small authored dialogs carry narrative consequences that apply at the end of the shift.

**Architecture:** Every rule lives in the pure `TimeDesk.Domain` assembly and is tested first: the gate evaluator shared by triggers and questions (`Gates`, `GateSnapshot`, `FlagKeys`), the effect-op rule (`EffectOps`), tells on two channels (`Lies`, `TellChannel`, `LiePlan.ChannelOf/TellValue`), the answer and wording rules (`Interview`), the dialog graph and runner (`Dialog.cs`), the per-traveller interview graph and the structure and capacity rules (`InterviewScript`, `DialogChecks`), the day's availability and the end-of-shift plan (`InterviewDay`, `DialogOutcomes`), and the statement-agnostic proof (`DiscrepancyLog.Prove/Add`, `ClueLabels`). Assembly-CSharp is glue: `TimelineService` projects conditions, snapshots the world and builds the day's `InterviewDay` from them (`BuildInterviewDay`, the one call the game and the Unity verification make), `CaseFactory` computes each traveller's answers from the same values as the papers, `GameManager` injects the day's `InterviewDay` and applies dialog outcomes at the end of the shift, and the UI plays the graph on the intercom with a transcript window (`TranscriptWindowController` over the shared `PagedRowsWindow`). Content (questions, dialogs, interview wording, small talk, tell channels) is authored in `world_source.json` and written by `Tools > TimeDesk > Generate World`; the scene is rebuilt by `Tools > TimeDesk > Build Office UI`.

**Tech Stack:** Unity 6000.4.11f1, C# 9, NUnit EditMode tests (`TimeDeskEditMode` references only `TimeDesk.Domain` and `TimeDesk.Visuals`), TextMeshPro (`com.unity.ugui` 2.0.0), Python 3 helper scripts in the scratchpad (`subs.py`, `cut.py`, `subsn.py`, `make_meta.py`, `compile_check.py`, the reflection test runner).

---

## Conventions used by every task

- **Worktree:** `E:\unity\NOPE-feat-clock`, branch `feat/dialog-questions` (checked out; never switch branches). Never touch `E:\unity\NOPE`. Never push, rebase or amend.
- **SCRATCH** below means `C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad`.
- **Line endings.** CRLF: `DiscrepancyLog.cs`, `ShiftLedger.cs`, `EffectSO.cs`, `TimelineTriggerSO.cs`, `TimelineService.cs`, `ContentLibrarySO.cs`, `DayPlanSO.cs`, `EraSO.cs`, `NationEraProfileSO.cs`, `CaseInstance.cs`, `CaseFactory.cs`, `GameManager.cs`, `ShiftScoring.cs`, `InvestigationUIController.cs`, `CompareController.cs`, `InteractionPanelController.cs`, `UpgradeSO.cs`, `WorldState.cs`, `ContentLibraryValidator.cs`, `DiscrepancyLogTests.cs` and `docs/FEATURES.md`. LF: `Lies.cs`, `Seeds.cs`, `Forgery.cs`, `ReferenceBookWindowController.cs`, `OfficeSceneUIBuilder.cs`, `WorldContentGenerator.cs`, `world_source.json`, `ContentLibrary_Main.asset`, `LiesTests.cs`, `SeedsTests.cs`, `ForgeryTests.cs`, `FactTableTests.cs`. Existing files are edited only through the task's Python script: `SCRATCH/subs.py` (`apply`: exact pairs, each old text must match exactly once), `SCRATCH/cut.py` (`cut`: replace from a unique start marker through the first end marker) and `SCRATCH/subsn.py` (`replace_n`: a counted replacement, created in Task 0). All three normalise CRLF to LF for matching and restore it, so line endings never change. New files are written whole with the Write tool (LF); `Forgery.cs`, `Lies.cs` and `ReferenceBookWindowController.cs` are LF and are replaced whole the same way.
- **Every new `.cs` under `Assets` and every new folder gets a `.meta`** in the same commit: `python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/make_meta.py" <paths>`. Hand-authored `.asset` files get their `.meta` from the task's script (fixed GUIDs, so the library can reference them). Unity writes the metas of generated assets itself.
- **Compile check** (run from Bash):
  `python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/compile_check.py" 'E:\unity\NOPE-feat-clock'`
  Pass = both `== Assembly-CSharp-Editor.csproj: exit 0` and `== TimeDeskEditMode.csproj: exit 0` with `0 Error(s)`.
- **Test run** (after a passing compile check; an optional last argument filters by `Class.Method` substring):
  `"C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/runner/bin/Debug/net10.0/runner.exe" 'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\cc\Temp\Bin\Debug'`
  The branch base gives `passed 216, failed 0`. Each task states the new total.
- **Running a task script:** save the block to the named file with the Write tool, then `python "<that file>"`. Every script prints `ok <path> crlf|lf` (or `cut …` / `replaced …` / `wrote …`) per file it touched and exits non-zero on the first text that does not match; then nothing after it ran. If a script reports `expected 1 match, found 0`, the file changed after this plan was written: re-read it and adapt that one pair (the code is the truth).
- **This plan is committed alone before Task 0** (`docs(plan): dialog + questions implementation plan`, made when its review was accepted), so the tree is clean when Task 0 starts and every "`git status --short` shows only the two untracked files" expectation below holds. Task 0 Step 1 checks that commit.
- **Commits:** `cd /e/unity/NOPE-feat-clock && git add <exact paths> && git commit -F - <<'EOF' … EOF`. Every message ends with a blank line and `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Never stage `NOPE-feat-clock.sln`, `TimeDesk.Visuals.csproj` or `_TimeDesk*` files. Never stage files you did not create or change in the task.
- **Assembly-CSharp is not unit-testable** (the test assembly sees only Domain and Visuals). Glue tasks therefore have no failing-test step: their rules are the Domain calls tested earlier, and their behaviour is checked in Unity in Tasks 17 and 18.
- Every task leaves both projects compiling and the suite green.
- **Pre-verified:** every script and whole file of this plan was replayed in order against a copy of the worktree at `fbf4905` (`SCRATCH/p3/dry.py`, output `SCRATCH/p3/dry_out.txt`; re-run after the review revision at the end of this plan): every scripted edit matched exactly once, CRLF/LF were preserved, each "expect failure" step failed with the stated errors, each task compiled, the stated test totals were observed, and the temporary Unity automation of Tasks 0, 17 and 18 compiled against the code it runs on.

## File map

| File | Responsibility | Task |
|---|---|---|
| `Assets/Scripts/Domain/Gates.cs` (+ .meta) | new: `TriggerConditionType` (moved from `TimelineTriggerSO.cs`, + `UpgradeOwned`), `GateCondition`, `GateSnapshot`, `Gates` (`Passes`, `AllPass`, `DayOnly`, `UnlockNight`), `Gated<T>`, `FlagKeys` | 1 |
| `Assets/Tests/EditMode/GatesTests.cs` (+ .meta) | new: decision table, snapshot copy, the two DayAtLeast readings, `DayOnly`, flag grammar, pinned ints | 1 |
| `Assets/Scripts/Timeline/TimelineTriggerSO.cs` | enum moved out; `FiredFlag` via `FlagKeys`; `key` doc (Task 1); class doc (Task 13) | 1, 13 |
| `Assets/Scripts/Domain/EffectOps.cs` (+ .meta) | new: `EffectOpType` (moved from `EffectSO.cs`), `EffectOps.ActsWhileActive` | 2 |
| `Assets/Tests/EditMode/EffectOpsTests.cs` (+ .meta) | new | 2 |
| `Assets/Scripts/EffectSO.cs` | `EffectOpType` moved out | 2 |
| `Assets/Scripts/Domain/Forgery.cs` | `IsProvableCategory` extracted, called by `IsProvableTell`; class doc | 3 |
| `Assets/Tests/EditMode/ForgeryTests.cs` | `IsProvableCategory` cases | 3 |
| `Assets/Scripts/Domain/Seeds.cs` | `DialogSalt`, `ForDialog` | 4 |
| `Assets/Tests/EditMode/SeedsTests.cs` | dialog-stream distinctness | 4 |
| `Assets/Scripts/Domain/ClueLabels.cs` (+ .meta) | new: `Report` | 5 |
| `Assets/Scripts/Domain/DiscrepancyLog.cs` | `EvidenceKind.Answer`, `ForAnswer`, `Discrepancy.source`, `Summary`, `Prove`/`Add` (`TryRegister` and `CategoryLabel` removed), docs | 5 |
| `Assets/Tests/EditMode/DiscrepancyLogTests.cs` | `Register` helper over `Prove`+`Add`; answer, source, `Add` and label cases | 5 |
| `Assets/Tests/EditMode/FactTableTests.cs` | two calls move to `Prove` | 5 |
| `Assets/Scripts/UI/InvestigationUIController.cs` | `HandlePairCompared` via `Prove`/`Add` (Task 5); book placement (Task 13); interview wiring, fallback, idle hint, docs (Task 14) | 5, 13, 14 |
| `Assets/Scripts/UI/CompareController.cs` | `ShowAlreadyDocumented` (Task 5); class doc (Task 14) | 5, 14 |
| `Assets/Scripts/Domain/Lies.cs` | `TellChannel`, (category, channel) options, `ChannelOf`/`TellValue`, `ApplyTo` Papers only, docs | 6 |
| `Assets/Tests/EditMode/LiesTests.cs` | two calls move to `Prove` (Task 5); channel arguments, rename, answer fixture and tests (Task 6) | 5, 6 |
| `Assets/Scripts/DayPlanSO.cs` | `tellChannels`/`TellChannels` (Task 6); `ForcedBlueprints` (Task 12); doc names the generator (Task 13) | 6, 12, 13 |
| `Assets/Scripts/CaseFactory.cs` | `Lies.Plan` call (Task 6); answers, opener, claim, small talk, dialog stream, docs, warnings, log (Task 14) | 6, 14 |
| `Assets/Scripts/Domain/InterviewContent.cs` (+ .meta) | new: `LineText`, `ScriptLine`, `ScriptChoice`, `ScriptNode`, `AuthoredDialog`, `WordingOverride`, `InterviewQuestion`, `InterviewLines` | 7 |
| `Assets/Scripts/Domain/Interview.cs` (+ .meta) | new: `InterviewAnswer`, `Interview` (`Answer`, `Opener`, `Claim`, `PickSmallTalk`, `WorstCaseLength`, `Placeholder`, `HoldsToken`, `Fill`) | 7 |
| `Assets/Scripts/Domain/Dialog.cs` (+ .meta) | new: `DialogSpeaker`, `DialogAction`, `DialogLine`, `DialogChoice`, `DialogNode`, `DialogGraph`, `DialogRunner` | 7 |
| `Assets/Tests/EditMode/InterviewTests.cs`, `DialogRunnerTests.cs` (+ .metas) | new | 7 |
| `Assets/Scripts/Domain/InterviewScript.cs` (+ .meta) | new: `InterviewCase`, `InterviewScript`, `DialogChecks` | 8 |
| `Assets/Tests/EditMode/InterviewScriptTests.cs` (+ .meta) | new | 8 |
| `Assets/Scripts/Domain/ShiftLedger.cs` | `DialogOutcome`, `dialogOutcomes` | 9 |
| `Assets/Scripts/Domain/InterviewDay.cs` (+ .meta) | new: `InterviewDay`, `DialogOutcomes` | 9 |
| `Assets/Tests/EditMode/InterviewDayTests.cs` (+ .meta) | new | 9 |
| `Assets/Scripts/Timeline/TimelineService.cs` | private `ToGate`, `ToGates`, `Snapshot`; `AllConditionsPass` via `Gates` (Task 10); `BuildInterviewDay`, the one projection GameManager and the Unity verification call (Task 14) | 10, 14 |
| `Assets/Scripts/UI/PagedRowsWindow.cs` (+ .meta) | new: shared paging and row cloning | 11 |
| `Assets/Scripts/UI/ReferenceBookWindowController.cs` | derives from `PagedRowsWindow` | 11 |
| `Assets/Scripts/Dialog.meta`, `Assets/Scripts/Dialog/QuestionSO.cs`, `DialogSO.cs` (+ .metas) | new: generated content assets | 12 |
| `Assets/Scripts/ContentLibrarySO.cs` | `interview`, `questions`, `dialogs`; doc | 12 |
| `Assets/Scripts/EraSO.cs`, `Assets/Scripts/Timeline/NationEraProfileSO.cs` | `smallTalk` | 12 |
| `Assets/Scripts/UpgradeSO.cs`, `Assets/Scripts/WorldState.cs` | docs | 12 |
| `Assets/Editor/ContentLibraryValidator.cs` | interview, upgrade-id, tell-channel and small-talk checks; `DialogEffectOpError` and `MaxDocuments` (both shared with the generator); `RequiredFacts` doc and `CheckPlaces` message | 12 |
| `Assets/Data/Investigation/RefBook_Capital.asset`, `RefBook_Ruler.asset`, `Assets/Data/Upgrades/Upgrade_InterviewProtocols.asset`, `Assets/Data/Effects/Effect_Dialog_RumourHeard.asset` (+ .metas) | new hand-authored content | 12 |
| `Assets/Data/Content Library/ContentLibrary_Main.asset` | + upgrade and effect by hand (Task 12); the rest by Generate World (Task 17) | 12, 17 |
| `Assets/Editor/WorldContentGenerator.cs` | interview source shape, checks (`CheckInterview`), builders, `WireLibrary`, `Interview` folder, `BirthYears` (shared by `MakePlace` and the line-length check) | 13 |
| `Assets/Data/World/world_source.json` | books, small talk, channels, `interview`, `questions`, `dialogs` | 13 |
| `Assets/Scripts/CaseInstance.cs` | `answers`, `smallTalk`; docs | 14 |
| `Assets/Scripts/GameManager.cs` | `BuildInterviewDay` (calls `TimelineService.BuildInterviewDay`, logs content problems), the spoken gate, injection (Task 14); `ApplyDialogOutcomes`, end of shift (Task 15) | 14, 15 |
| `Assets/Scripts/UI/TranscriptWindowController.cs` (+ .meta) | new: the transcript window | 14 |
| `Assets/Scripts/UI/InteractionPanelController.cs`, `Assets/Scripts/Shift/ShiftScoring.cs` | doc; citation copy | 14 |
| `Assets/Editor/OfficeSceneUIBuilder.cs` | intercom mask and capacity, transcript window, icon ids, Dialect copy, layout constants | 16 |
| `docs/FEATURES.md` | behaviour contract, in the commit of each behaviour | 5, 12, 13, 14, 15, 16 |
| `Assets/Data/World/Interview/*`, `Eras/*`, `Places/*`, `Assets/Data/Investigation/DayPlan_Inv_Day1..3.asset` | generated by Generate World in Unity | 17 |
| `Assets/Scenes/OfficeScene.unity` | rebuilt by Build Office UI in Unity | 17 |
| `docs/superpowers/specs/2026-09-24-dialog-questions-design.md` | verification record appended | 18 |
| `SCRATCH/subsn.py` (not committed) | counted, EOL-preserving replacement helper | 0 |
| `Assets/Editor/_TimeDeskP3*.cs` (never committed) | temporary Unity automation | 0, 17, 18 |

---

### Task 0: Setup and the pre-change baseline (Unity)

Spec §6 step 0 needs, **before any piece-3 code**, a dump of who travellers are and what they lie with (so Task 18 can prove the stream contract line for line) and the decisions of the authored timeline triggers on crafted worlds (so Task 18 can prove the gate evaluator changed nothing).

**Files:**
- Create (scratch, not committed): `SCRATCH/subsn.py`
- Create (temporary, never committed): `Assets/Editor/_TimeDeskP3Baseline.cs`
- Output: `SCRATCH/p3_baseline_cases.txt`, `SCRATCH/p3_baseline_triggers.txt`

- [ ] **Step 1: Confirm the starting point**

Run: `cd /e/unity/NOPE-feat-clock && git branch --show-current && git log --oneline -4 && git status --short`
Expected: `feat/dialog-questions`; the four newest commits are this plan's own commit `docs(plan): dialog + questions implementation plan` (it adds only this file), `fbf4905 docs(spec): align dialog + questions spec with the code as implemented`, `8395470 docs(spec): dialog + questions (piece 3) design` and `dc676cd docs(spec): identity & lies verification record`; the code is still piece 2's final code (`efe385d`). Untracked only `NOPE-feat-clock.sln` and `TimeDesk.Visuals.csproj` (if this plan shows as untracked, it was not committed: stop and commit it alone first, as the Conventions say). Run the compile check and the test run: `passed 216, failed 0`.

- [ ] **Step 2: Create the counted-replacement helper** (`SCRATCH/subsn.py`, Write tool; skip if it already exists with this content)

```python
"""EOL-preserving counted replacement: replace_n(path, old, new, n) replaces every
occurrence of `old` with `new` and fails unless there are exactly `n` of them.
CRLF files are normalised to LF for matching and restored."""
import pathlib, sys

def replace_n(path, old, new, n):
    p = pathlib.Path(path); b = p.read_bytes(); crlf = b'\r\n' in b
    t = b.decode('utf-8').replace('\r\n', '\n')
    found = t.count(old)
    if found != n:
        sys.exit(f"{p}: expected {n} matches, found {found}: {old[:100]!r}")
    t = t.replace(old, new)
    if crlf:
        t = t.replace('\n', '\r\n')
    p.write_bytes(t.encode('utf-8'))
    print('replaced', n, 'x in', p, 'crlf' if crlf else 'lf')
```

- [ ] **Step 3: Check no Unity editor is open on the worktree** (PowerShell)

Run: `Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Select-Object ProcessId, CommandLine`
Expected: no process whose command line contains `NOPE-feat-clock` (one on `E:\unity\NOPE` is Codex's; leave it alone).

- [ ] **Step 4: Write the baseline dumper** (`Assets/Editor/_TimeDeskP3Baseline.cs`, Write tool)

```csharp
// TEMPORARY (piece 3 baseline; never committed): dumps who each traveller is,
// their lie and their papers before any piece-3 change, and what every
// authored trigger decides on crafted worlds, for the verification task.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;

public static class TimeDeskP3Baseline
{
    private const string Scratch = @"C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad";
    private static readonly string CasesPath = Path.Combine(Scratch, "p3_baseline_cases.txt");
    private static readonly string TriggersPath = Path.Combine(Scratch, "p3_baseline_triggers.txt");

    public static void Run()
    {
        try
        {
            var lib = AssetDatabase.LoadAssetAtPath<ContentLibrarySO>("Assets/Data/Content Library/ContentLibrary_Main.asset");

            var cases = new List<string>();
            foreach (int seed in new[] { 12345, 999 })
            {
                for (int day = 1; day <= 3; day++)
                {
                    DayPlanSO plan = lib.GetDayPlan(day);
                    var factory = new CaseFactory(lib, lib.BuildFactTable(plan));
                    List<CaseInstance> generated = factory.GenerateDayCases(plan, new WorldState { day = day }, Seeds.Day(seed, day));
                    var violators = (Dictionary<int, NationEraProfileSO>)typeof(CaseFactory)
                        .GetField("_violators", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(factory);
                    for (int i = 0; i < generated.Count; i++)
                        cases.Add(Line(seed, day, i + 1, generated[i], violators.ContainsKey(i + 1)));
                }
            }
            File.WriteAllLines(CasesPath, cases);

            MethodInfo allPass = typeof(TimelineService).GetMethod("AllConditionsPass", BindingFlags.NonPublic | BindingFlags.Static);
            var triggers = new List<string>();
            List<WorldState> worlds = CraftedWorlds();
            foreach (TimelineTriggerSO trigger in lib.Triggers)
                for (int i = 0; i < worlds.Count; i++)
                    triggers.Add($"trigger={trigger.id} state={i} pass={allPass.Invoke(null, new object[] { trigger, worlds[i] })}");
            File.WriteAllLines(TriggersPath, triggers);

            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            File.WriteAllText(CasesPath + ".error", e.ToString());
            EditorApplication.Exit(2);
        }
    }

    /// <summary>
    /// One slot: identity | lie | papers. The verification task prints exactly
    /// this format (tells = the flagged paper categories, in paper order).
    /// </summary>
    private static string Line(int seed, int day, int slot, CaseInstance c, bool violator)
    {
        List<DocumentField> fields = c.documents.SelectMany(d => d.fields).ToList();
        string tells = string.Join(",", fields.Where(f => f.isAnachronism).Select(f => f.category).Distinct());
        string papers = string.Join(";", fields.Select(f => $"{f.label}={f.value}"));
        return $"seed={seed} day={day} slot={slot} claim='{c.originLabel}' name='{c.visitorGivenName}' born='{c.trueBirthDate}' role='{(c.archetype != null ? c.archetype.displayName : "")}' allowed={c.claimAllowedByRules} violator={violator}" +
               $" | liar={c.IsLiar} home='{c.HomeLabel}' tells=[{tells}] | papers={papers}";
    }

    /// <summary>Twenty crafted worlds (days 1-5, flags, counters, stability, scores, tiers, upgrades); the verification task builds the same list.</summary>
    private static List<WorldState> CraftedWorlds()
    {
        var worlds = new List<WorldState>();
        for (int day = 1; day <= 5; day++)
            worlds.Add(new WorldState { day = day });

        WorldState w;
        w = new WorldState { day = 2 }; w.SetFlag("rumour_calculators"); worlds.Add(w);
        w = new WorldState { day = 3 }; w.SetFlag("met_tesla"); w.SetFlag("trig:art_renaissance:fired"); worlds.Add(w);
        w = new WorldState { day = 2 }; w.AddCounter("sent:tag:scientist", 7); worlds.Add(w);
        w = new WorldState { day = 4 }; w.AddCounter("sent:tag:soldier", 30); w.AddCounter("sent:era:ancient", 12); worlds.Add(w);
        w = new WorldState { day = 1, timelineStability = 5f }; worlds.Add(w);
        w = new WorldState { day = 3, timelineStability = 50f }; worlds.Add(w);
        w = new WorldState { day = 2 }; w.timeline.AddScore("attrTotal:art", 500f); w.timeline.AddScore("attrTotal:science", 500f); worlds.Add(w);
        w = new WorldState { day = 2 }; w.timeline.AddScore("attrTotal:democracy", -50f); worlds.Add(w);
        w = new WorldState { day = 3 }; w.timeline.AddScore("attr:egypt_ancient:art", 400f); w.timeline.AddScore("attr:italy_ancient:science", 400f); worlds.Add(w);
        w = new WorldState { day = 2 }; w.timeline.AddScore("nation:egypt", 90f); worlds.Add(w);
        w = new WorldState { day = 3 }; w.timeline.dominantKeys.Add("egypt_ancient:art"); w.timeline.supportingKeys.Add("italy_ancient:science"); worlds.Add(w);
        w = new WorldState { day = 1 }; w.UnlockUpgrade("archive_access"); worlds.Add(w);
        w = new WorldState { day = 5 }; w.UnlockUpgrade("adv_scanner"); w.UnlockUpgrade("diplo_contacts"); worlds.Add(w);
        w = new WorldState { day = 4, timelineStability = 0f }; w.SetFlag("Tyrant_Rises"); w.AddCounter("sent:tag:artist", 2); worlds.Add(w);
        w = new WorldState { day = 5, timelineStability = 20f }; w.SetFlag("upgrade:archive_access"); worlds.Add(w);
        return worlds;
    }
}
```

- [ ] **Step 5: Run it in the worktree's own Unity** (PowerShell, timeout 600000 ms)

```powershell
Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe' -ArgumentList @('-projectPath','E:\unity\NOPE-feat-clock','-executeMethod','TimeDeskP3Baseline.Run','-logFile','C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\unity_p3_baseline.log') -PassThru -Wait
```
Expected: exit code 0 and no `SCRATCH/p3_baseline_cases.txt.error`. Check with `wc -l "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/p3_baseline_cases.txt" "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/p3_baseline_triggers.txt"`: 60 case lines (2 seeds × (8 + 10 + 12) slots), each like `seed=12345 day=1 slot=1 claim='…' name='…' born='…' role='…' allowed=True violator=False | liar=False home='…' tells=[] | papers=Full Name=…;…`, and 60 trigger lines (3 authored triggers × 20 crafted worlds), all ending `pass=False` (the three authored triggers need a profile they do not have, spec R22).

- [ ] **Step 6: Clean up**

```bash
cd /e/unity/NOPE-feat-clock && rm -f Assets/Editor/_TimeDeskP3Baseline.cs Assets/Editor/_TimeDeskP3Baseline.cs.meta && git checkout -- Assembly-CSharp.csproj Assembly-CSharp-Editor.csproj TimeDesk.Domain.csproj TimeDeskEditMode.csproj NOPE.sln ProjectSettings "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset" Assets/Scenes/OfficeScene.unity && git status --short
```
Expected: only `?? NOPE-feat-clock.sln` and `?? TimeDesk.Visuals.csproj`. (If `git checkout` reports a path that is not modified, that is fine.) No commit in this task.

---

### Task 1: One gate evaluator in Domain (`Gates`, `GateSnapshot`, `FlagKeys`)

`TriggerConditionType` moves from `TimelineTriggerSO.cs` into Domain with its ints unchanged, gains `UpgradeOwned`, and gets a pure evaluator over a frozen snapshot (spec §2.2, Q6/X3, R4). The fired-flag format moves to `FlagKeys`. `TimelineService` keeps its own switch until Task 10 (an `UpgradeOwned` condition falls to its `false` default until then; no content uses one before Task 13).

**Files:**
- Create: `Assets/Scripts/Domain/Gates.cs` (+ `.meta`)
- Test: `Assets/Tests/EditMode/GatesTests.cs` (+ `.meta`)
- Modify: `Assets/Scripts/Timeline/TimelineTriggerSO.cs` (enum removed, `FiredFlag`, `key` doc)

- [ ] **Step 1: Write the failing test** (`Assets/Tests/EditMode/GatesTests.cs`, Write tool; then `make_meta.py` on it)

```csharp
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The gate decision table (timeline triggers, interview questions, dialogs),
/// the snapshot it reads, the two readings of "DayAtLeast", and the run-flag
/// grammar. Snapshot under test: day 3, stability 40, flag "met_tesla",
/// upgrade "adv_scanner", counter "sent:tag:scientist" = 5, scores
/// "attr:egypt_ancient:science" = 12 and "nation:egypt" = 7, dominant
/// "egypt_ancient:science", supporting "egypt_ancient:art".
/// </summary>
public class GatesTests
{
    private static GateSnapshot Snap(int day = 3, float stability = 40f) => new GateSnapshot(
        day, stability,
        new[] { "met_tesla" },
        new[] { "adv_scanner" },
        new[] { new KeyValuePair<string, int>("sent:tag:scientist", 5) },
        new[] { new KeyValuePair<string, float>("attr:egypt_ancient:science", 12f), new KeyValuePair<string, float>("nation:egypt", 7f) },
        new[] { "egypt_ancient:science" },
        new[] { "egypt_ancient:art" });

    private static bool Passes(TriggerConditionType type, string key, float threshold, GateSnapshot s = null) =>
        Gates.Passes(new GateCondition(type, key, threshold), s ?? Snap());

    [Test]
    public void CounterAtLeast_ComparesTheCounter_ANullKeyReadsZero()
    {
        Assert.IsTrue(Passes(TriggerConditionType.CounterAtLeast, "sent:tag:scientist", 5f));
        Assert.IsFalse(Passes(TriggerConditionType.CounterAtLeast, "sent:tag:scientist", 6f));
        Assert.IsFalse(Passes(TriggerConditionType.CounterAtLeast, "sent:tag:artist", 1f), "a missing counter reads 0");
        Assert.IsTrue(Passes(TriggerConditionType.CounterAtLeast, null, 0f), "a null key reads 0, which passes threshold 0 (as today)");
        Assert.IsFalse(Passes(TriggerConditionType.CounterAtLeast, null, 1f));
    }

    [Test]
    public void FlagSet_And_FlagNotSet_ReadTheFlags()
    {
        Assert.IsTrue(Passes(TriggerConditionType.FlagSet, "met_tesla", 0f));
        Assert.IsFalse(Passes(TriggerConditionType.FlagSet, "met_edison", 0f));
        Assert.IsFalse(Passes(TriggerConditionType.FlagNotSet, "met_tesla", 0f));
        Assert.IsTrue(Passes(TriggerConditionType.FlagNotSet, "met_edison", 0f));
        Assert.IsFalse(Passes(TriggerConditionType.FlagSet, null, 0f), "a null key is never set");
        Assert.IsTrue(Passes(TriggerConditionType.FlagNotSet, null, 0f), "a null key is never set, so FlagNotSet passes (as today)");
    }

    [Test]
    public void AttributeScores_CompareTheScore_AndNeedAKey()
    {
        Assert.IsTrue(Passes(TriggerConditionType.AttributeScoreAtLeast, "attr:egypt_ancient:science", 12f));
        Assert.IsFalse(Passes(TriggerConditionType.AttributeScoreAtLeast, "attr:egypt_ancient:science", 12.5f));
        Assert.IsTrue(Passes(TriggerConditionType.AttributeScoreAtMost, "attr:egypt_ancient:science", 12f));
        Assert.IsFalse(Passes(TriggerConditionType.AttributeScoreAtMost, "attr:egypt_ancient:science", 11f));
        Assert.IsTrue(Passes(TriggerConditionType.AttributeScoreAtMost, "attr:egypt_ancient:art", 0f), "a missing score reads 0");
        Assert.IsFalse(Passes(TriggerConditionType.AttributeScoreAtLeast, null, -100f), "an unresolved reference never passes");
        Assert.IsFalse(Passes(TriggerConditionType.AttributeScoreAtMost, null, 100f), "an unresolved reference never passes");
    }

    [Test]
    public void DominanceTiers_ReadTheTierKeys_AndNeedAKey()
    {
        Assert.IsTrue(Passes(TriggerConditionType.AttributeIsDominant, "egypt_ancient:science", 0f));
        Assert.IsFalse(Passes(TriggerConditionType.AttributeIsDominant, "egypt_ancient:art", 0f));
        Assert.IsTrue(Passes(TriggerConditionType.AttributeIsSupporting, "egypt_ancient:art", 0f));
        Assert.IsFalse(Passes(TriggerConditionType.AttributeIsSupporting, "egypt_ancient:science", 0f));
        Assert.IsFalse(Passes(TriggerConditionType.AttributeIsDominant, null, 0f));
        Assert.IsFalse(Passes(TriggerConditionType.AttributeIsSupporting, null, 0f));
    }

    [Test]
    public void NationScoreAtLeast_ComparesTheNationScore_AndNeedsAKey()
    {
        Assert.IsTrue(Passes(TriggerConditionType.NationScoreAtLeast, "nation:egypt", 7f));
        Assert.IsFalse(Passes(TriggerConditionType.NationScoreAtLeast, "nation:egypt", 8f));
        Assert.IsFalse(Passes(TriggerConditionType.NationScoreAtLeast, null, -100f));
    }

    [Test]
    public void DayAtLeast_And_StabilityAtMost_ReadTheSnapshot()
    {
        Assert.IsTrue(Passes(TriggerConditionType.DayAtLeast, null, 3f));
        Assert.IsFalse(Passes(TriggerConditionType.DayAtLeast, null, 4f));
        Assert.IsTrue(Passes(TriggerConditionType.StabilityAtMost, null, 40f));
        Assert.IsFalse(Passes(TriggerConditionType.StabilityAtMost, null, 39.9f));
    }

    [Test]
    public void UpgradeOwned_ReadsUpgrades_NeverTheUnlockFlag()
    {
        Assert.IsTrue(Passes(TriggerConditionType.UpgradeOwned, "adv_scanner", 0f));
        Assert.IsFalse(Passes(TriggerConditionType.UpgradeOwned, "archive_access", 0f));
        Assert.IsFalse(Passes(TriggerConditionType.UpgradeOwned, null, 0f));

        var flagOnly = new GateSnapshot(1, 100f, new[] { "upgrade:archive_access" }, null, null, null, null, null);
        Assert.IsFalse(Passes(TriggerConditionType.UpgradeOwned, "archive_access", 0f, flagOnly), "the 'upgrade:x' flag alone is not ownership");
    }

    [Test]
    public void AnUnknownConditionType_NeverPasses()
    {
        Assert.IsFalse(Passes((TriggerConditionType)99, "met_tesla", 0f));
    }

    [Test]
    public void ANullSnapshot_PassesNothing()
    {
        Assert.IsFalse(Gates.Passes(new GateCondition(TriggerConditionType.FlagNotSet, "x", 0f), null));
        Assert.IsFalse(Gates.AllPass(new[] { new GateCondition(TriggerConditionType.DayAtLeast, null, 0f) }, null));
    }

    [Test]
    public void AllPass_TrueForNoConditions_FalseAsSoonAsOneFails()
    {
        Assert.IsTrue(Gates.AllPass(null, Snap()));
        Assert.IsTrue(Gates.AllPass(new GateCondition[0], Snap()));

        var dayAndFlag = new[]
        {
            new GateCondition(TriggerConditionType.DayAtLeast, null, 2f),
            new GateCondition(TriggerConditionType.FlagSet, "met_tesla", 0f)
        };
        Assert.IsTrue(Gates.AllPass(dayAndFlag, Snap()));
        Assert.IsFalse(Gates.AllPass(dayAndFlag, Snap(day: 1)));

        var oneFails = new[]
        {
            new GateCondition(TriggerConditionType.DayAtLeast, null, 2f),
            new GateCondition(TriggerConditionType.FlagSet, "met_edison", 0f)
        };
        Assert.IsFalse(Gates.AllPass(oneFails, Snap()));
    }

    [Test]
    public void GateSnapshot_CopiesItsInputs()
    {
        var flags = new List<string> { "a" };
        var upgrades = new List<string> { "u" };
        var counters = new List<KeyValuePair<string, int>> { new KeyValuePair<string, int>("c", 1) };
        var scores = new List<KeyValuePair<string, float>> { new KeyValuePair<string, float>("s", 1f) };
        var dominant = new List<string> { "d" };
        var supporting = new List<string> { "p" };
        var s = new GateSnapshot(2, 50f, flags, upgrades, counters, scores, dominant, supporting);

        flags.Clear(); flags.Add("b");
        upgrades.Clear();
        counters.Clear(); counters.Add(new KeyValuePair<string, int>("c", 9));
        scores.Clear();
        dominant.Clear();
        supporting.Clear();

        Assert.AreEqual(2, s.Day);
        Assert.AreEqual(50f, s.Stability);
        Assert.IsTrue(s.HasFlag("a"));
        Assert.IsFalse(s.HasFlag("b"));
        Assert.IsTrue(s.HasUpgrade("u"));
        Assert.AreEqual(1, s.Counter("c"));
        Assert.AreEqual(1f, s.Score("s"));
        Assert.IsTrue(s.IsDominant("d"));
        Assert.IsTrue(s.IsSupporting("p"));
    }

    [Test]
    public void GateSnapshot_NullOrBlankArguments_ReadFalseOrZero()
    {
        var s = new GateSnapshot(1, 100f, null, null, null, null, null, null);
        Assert.IsFalse(s.HasFlag(null));
        Assert.IsFalse(s.HasFlag(""));
        Assert.IsFalse(s.HasUpgrade(" "));
        Assert.AreEqual(0, s.Counter(null));
        Assert.AreEqual(0f, s.Score(""));
        Assert.IsFalse(s.IsDominant(null));
        Assert.IsFalse(s.IsSupporting(null));
    }

    [Test]
    public void DayAtLeast_TheNightlyResolveAndTheDayStartReadDifferentDays()
    {
        // A trigger announcing a day-3 question fires the night of day 2
        // (the nightly resolve snapshots before day++)...
        var unlockTrigger = new GateCondition(TriggerConditionType.DayAtLeast, null, Gates.UnlockNight(3));
        Assert.IsTrue(Gates.Passes(unlockTrigger, Snap(day: 2)));
        Assert.IsFalse(Gates.Passes(unlockTrigger, Snap(day: 1)));

        // ...while the question itself is askable from the start of day 3.
        var question = new GateCondition(TriggerConditionType.DayAtLeast, null, 3f);
        Assert.IsFalse(Gates.Passes(question, Snap(day: 2)));
        Assert.IsTrue(Gates.Passes(question, Snap(day: 3)));

        Assert.AreEqual(1, Gates.UnlockNight(2));
    }

    [Test]
    public void DayOnly_TrueOnlyWhenEveryConditionIsADayGate()
    {
        Assert.IsTrue(Gates.DayOnly(null));
        Assert.IsTrue(Gates.DayOnly(new GateCondition[0]));
        Assert.IsTrue(Gates.DayOnly(new[] { new GateCondition(TriggerConditionType.DayAtLeast, null, 2f) }));
        Assert.IsFalse(Gates.DayOnly(new[]
        {
            new GateCondition(TriggerConditionType.DayAtLeast, null, 2f),
            new GateCondition(TriggerConditionType.UpgradeOwned, "interview_protocols", 0f)
        }));
        Assert.IsFalse(Gates.DayOnly(new[] { new GateCondition(TriggerConditionType.FlagSet, "rumour_calculators", 0f) }));
    }

    [Test]
    public void FlagKeys_KeepTheFormatSavesHold()
    {
        Assert.AreEqual("trig:x:fired", FlagKeys.TriggerFired("x"));
        Assert.AreEqual("dlg:x:done", FlagKeys.DialogDone("x"));
    }

    [Test]
    public void TriggerConditionType_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)TriggerConditionType.CounterAtLeast);
        Assert.AreEqual(1, (int)TriggerConditionType.FlagSet);
        Assert.AreEqual(2, (int)TriggerConditionType.FlagNotSet);
        Assert.AreEqual(3, (int)TriggerConditionType.AttributeScoreAtLeast);
        Assert.AreEqual(4, (int)TriggerConditionType.AttributeScoreAtMost);
        Assert.AreEqual(5, (int)TriggerConditionType.AttributeIsDominant);
        Assert.AreEqual(6, (int)TriggerConditionType.AttributeIsSupporting);
        Assert.AreEqual(7, (int)TriggerConditionType.NationScoreAtLeast);
        Assert.AreEqual(8, (int)TriggerConditionType.DayAtLeast);
        Assert.AreEqual(9, (int)TriggerConditionType.StabilityAtMost);
        Assert.AreEqual(10, (int)TriggerConditionType.UpgradeOwned);
    }
}
```

- [ ] **Step 2: Run the compile check — expect failure**

Expected: `== TimeDeskEditMode.csproj: exit 1` with `error CS0246: The type or namespace name 'GateSnapshot' could not be found` and `… 'TriggerConditionType' could not be found`.

- [ ] **Step 3: Implement** — write `Assets/Scripts/Domain/Gates.cs` (Write tool; then `make_meta.py` on it):

```csharp
using System.Collections.Generic;

/// <summary>
/// Condition type for gates (timeline triggers, interview questions, dialogs).
/// Serialized as ints: append only.
/// </summary>
public enum TriggerConditionType
{
    /// <summary>WorldState counter >= threshold. key = counter key (e.g., "sent:tag:greek-warrior:greece480").</summary>
    CounterAtLeast,

    /// <summary>Flag is set. key = flag name.</summary>
    FlagSet,

    /// <summary>Flag is NOT set. key = flag name.</summary>
    FlagNotSet,

    /// <summary>Attribute score in a profile >= threshold (baseline + accumulated).</summary>
    AttributeScoreAtLeast,

    /// <summary>Attribute score in a profile <= threshold.</summary>
    AttributeScoreAtMost,

    /// <summary>Attribute is currently DOMINANT in the profile.</summary>
    AttributeIsDominant,

    /// <summary>Attribute is currently SUPPORTING in the profile.</summary>
    AttributeIsSupporting,

    /// <summary>Nation global score >= threshold.</summary>
    NationScoreAtLeast,

    /// <summary>
    /// Current day >= threshold, read from the snapshot's day. The nightly
    /// resolve snapshots before day++ (the day just played), so a trigger
    /// gated "DayAtLeast N" fires the night of day N and its effects start on
    /// day N+1; the office's day-start snapshot holds the day being played, so
    /// a question gated "DayAtLeast N" is askable from day N
    /// (Gates.UnlockNight converts one reading into the other).
    /// </summary>
    DayAtLeast,

    /// <summary>Timeline stability <= threshold.</summary>
    StabilityAtMost,

    /// <summary>The upgrade is owned (WorldState.HasUpgrade). key = UpgradeSO.id; never the 'upgrade:x' flag an unlock effect may set.</summary>
    UpgradeOwned
}

/// <summary>One gate condition as the rules read it: its type and the plain key and threshold it compares.</summary>
public readonly struct GateCondition
{
    /// <summary>What the condition tests.</summary>
    public readonly TriggerConditionType Type;

    /// <summary>The counter, flag, upgrade, score or dominance key; null means an unresolved reference.</summary>
    public readonly string Key;

    /// <summary>Numeric threshold (counters, scores, day, stability).</summary>
    public readonly float Threshold;

    /// <summary>Creates a condition.</summary>
    public GateCondition(TriggerConditionType type, string key, float threshold)
    {
        Type = type;
        Key = key;
        Threshold = threshold;
    }
}

/// <summary>
/// A frozen copy of what gates read: day, stability, flags, owned upgrades,
/// counters, scores and dominance tiers. The constructor copies every
/// collection, so later changes to the world never change an evaluation. A
/// null or blank argument reads false or 0, and a missing counter or score
/// reads 0 (like WorldState.HasFlag/GetCounter and TimelineStateData.GetScore).
/// </summary>
public sealed class GateSnapshot
{
    /// <summary>Set flags.</summary>
    private readonly HashSet<string> _flags;

    /// <summary>Owned upgrade ids.</summary>
    private readonly HashSet<string> _upgrades;

    /// <summary>Dominant tier keys ("profileId:attrId").</summary>
    private readonly HashSet<string> _dominant;

    /// <summary>Supporting tier keys ("profileId:attrId").</summary>
    private readonly HashSet<string> _supporting;

    /// <summary>Counter values by key (the first entry of a key wins, like WorldState.GetCounter).</summary>
    private readonly Dictionary<string, int> _counters = new Dictionary<string, int>();

    /// <summary>Score values by key (the first entry of a key wins, like TimelineStateData.GetScore).</summary>
    private readonly Dictionary<string, float> _scores = new Dictionary<string, float>();

    /// <summary>Copies the given state. Null collections count as empty.</summary>
    public GateSnapshot(int day, float stability,
                        IEnumerable<string> flags, IEnumerable<string> upgradeIds,
                        IEnumerable<KeyValuePair<string, int>> counters,
                        IEnumerable<KeyValuePair<string, float>> scores,
                        IEnumerable<string> dominantKeys, IEnumerable<string> supportingKeys)
    {
        Day = day;
        Stability = stability;
        _flags = Copy(flags);
        _upgrades = Copy(upgradeIds);
        _dominant = Copy(dominantKeys);
        _supporting = Copy(supportingKeys);

        if (counters != null)
            foreach (KeyValuePair<string, int> c in counters)
                if (!string.IsNullOrEmpty(c.Key) && !_counters.ContainsKey(c.Key))
                    _counters.Add(c.Key, c.Value);

        if (scores != null)
            foreach (KeyValuePair<string, float> s in scores)
                if (!string.IsNullOrEmpty(s.Key) && !_scores.ContainsKey(s.Key))
                    _scores.Add(s.Key, s.Value);
    }

    /// <summary>The day the snapshot was taken on.</summary>
    public int Day { get; }

    /// <summary>Timeline stability (0..100).</summary>
    public float Stability { get; }

    /// <summary>True when the flag is set.</summary>
    public bool HasFlag(string flag) => !string.IsNullOrEmpty(flag) && _flags.Contains(flag);

    /// <summary>True when the upgrade is owned.</summary>
    public bool HasUpgrade(string upgradeId) => !string.IsNullOrEmpty(upgradeId) && _upgrades.Contains(upgradeId);

    /// <summary>A counter's value (0 when missing).</summary>
    public int Counter(string key) => !string.IsNullOrEmpty(key) && _counters.TryGetValue(key, out int v) ? v : 0;

    /// <summary>A score's value (0 when missing).</summary>
    public float Score(string key) => !string.IsNullOrEmpty(key) && _scores.TryGetValue(key, out float v) ? v : 0f;

    /// <summary>True when the tier key is currently DOMINANT.</summary>
    public bool IsDominant(string key) => !string.IsNullOrEmpty(key) && _dominant.Contains(key);

    /// <summary>True when the tier key is currently SUPPORTING.</summary>
    public bool IsSupporting(string key) => !string.IsNullOrEmpty(key) && _supporting.Contains(key);

    /// <summary>A set of the non-blank items (empty for null).</summary>
    private static HashSet<string> Copy(IEnumerable<string> items)
    {
        var set = new HashSet<string>();
        if (items != null)
            foreach (string item in items)
                if (!string.IsNullOrEmpty(item))
                    set.Add(item);
        return set;
    }
}

/// <summary>
/// Evaluates gate conditions over a snapshot. Timeline triggers (at the
/// nightly resolve) and the office's day-start interview (questions,
/// dialogs) share this one evaluator, so every rule is tested headless.
/// </summary>
public static class Gates
{
    /// <summary>Whether one condition passes on the snapshot (a null snapshot passes nothing; an unknown type never passes).</summary>
    public static bool Passes(GateCondition c, GateSnapshot s)
    {
        if (s == null)
            return false;

        switch (c.Type)
        {
            case TriggerConditionType.CounterAtLeast: return s.Counter(c.Key) >= c.Threshold;
            case TriggerConditionType.FlagSet: return s.HasFlag(c.Key);
            case TriggerConditionType.FlagNotSet: return !s.HasFlag(c.Key);
            case TriggerConditionType.AttributeScoreAtLeast: return c.Key != null && s.Score(c.Key) >= c.Threshold;
            case TriggerConditionType.AttributeScoreAtMost: return c.Key != null && s.Score(c.Key) <= c.Threshold;
            case TriggerConditionType.AttributeIsDominant: return c.Key != null && s.IsDominant(c.Key);
            case TriggerConditionType.AttributeIsSupporting: return c.Key != null && s.IsSupporting(c.Key);
            case TriggerConditionType.NationScoreAtLeast: return c.Key != null && s.Score(c.Key) >= c.Threshold;
            case TriggerConditionType.DayAtLeast: return s.Day >= c.Threshold;
            case TriggerConditionType.StabilityAtMost: return s.Stability <= c.Threshold;
            case TriggerConditionType.UpgradeOwned: return s.HasUpgrade(c.Key);
            default: return false;
        }
    }

    /// <summary>True when every condition passes (a null or empty list passes); false as soon as one fails.</summary>
    public static bool AllPass(IEnumerable<GateCondition> conditions, GateSnapshot s)
    {
        if (conditions == null)
            return true;

        foreach (GateCondition c in conditions)
            if (!Passes(c, s))
                return false;

        return true;
    }

    /// <summary>True when every condition is a DayAtLeast gate (a null or empty list counts): only such questions carry spoken tells.</summary>
    public static bool DayOnly(IEnumerable<GateCondition> conditions)
    {
        if (conditions == null)
            return true;

        foreach (GateCondition c in conditions)
            if (c.Type != TriggerConditionType.DayAtLeast)
                return false;

        return true;
    }

    /// <summary>
    /// The nightly-resolve day on which to announce something askable from
    /// <paramref name="fromDay"/>: the night before, since the resolve reads
    /// the day just played (see TriggerConditionType.DayAtLeast).
    /// </summary>
    public static int UnlockNight(int fromDay) => fromDay - 1;
}

/// <summary>Pairs content with its projected gate, so availability is decided in Domain.</summary>
public readonly struct Gated<T>
{
    /// <summary>The gated content (a question, a dialog).</summary>
    public readonly T Item;

    /// <summary>Its conditions (null counts as none).</summary>
    public readonly IReadOnlyList<GateCondition> Conditions;

    /// <summary>Creates a gated item.</summary>
    public Gated(T item, IReadOnlyList<GateCondition> conditions)
    {
        Item = item;
        Conditions = conditions;
    }
}

/// <summary>Flag names the run writes into WorldState.flags; one home for the format.</summary>
public static class FlagKeys
{
    /// <summary>Set after a one-shot timeline trigger fires ("trig:{id}:fired", the format saves already hold).</summary>
    public static string TriggerFired(string triggerId) => $"trig:{triggerId}:fired";

    /// <summary>Set at the end of the shift that completed a one-shot narrative dialog ("dlg:{id}:done").</summary>
    public static string DialogDone(string dialogId) => $"dlg:{dialogId}:done";
}
```

Then move the enum out of `TimelineTriggerSO.cs` — save as `SCRATCH/p3_t01.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
from cut import cut
W = r'E:\unity\NOPE-feat-clock'
T = W + r'\Assets\Scripts\Timeline\TimelineTriggerSO.cs'

# The enum moves to Domain Gates.cs (values and order unchanged).
cut(T, """/// <summary>
/// Condition type for timeline triggers. All conditions on a trigger must pass.
/// </summary>
public enum TriggerConditionType
{""", """    /// <summary>Timeline stability <= threshold.</summary>
    StabilityAtMost
}

""")

apply(T, [
("""    /// <summary>Flag set after a one-shot trigger fires.</summary>
    public string FiredFlag => $"trig:{id}:fired";""",
"""    /// <summary>Flag set after a one-shot trigger fires (FlagKeys.TriggerFired).</summary>
    public string FiredFlag => FlagKeys.TriggerFired(id);"""),
("""    /// <summary>Counter key or flag name.</summary>
    public string key;""",
"""    /// <summary>Counter key, flag name, or upgrade id (UpgradeOwned).</summary>
    public string key;"""),
])
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 232, failed 0` (216 + 16).

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Domain/Gates.cs Assets/Scripts/Domain/Gates.cs.meta Assets/Tests/EditMode/GatesTests.cs Assets/Tests/EditMode/GatesTests.cs.meta Assets/Scripts/Timeline/TimelineTriggerSO.cs && git commit -F - <<'EOF'
feat(domain): one gate evaluator for triggers and questions (Gates)

TriggerConditionType moves to Domain with its ints unchanged and gains
UpgradeOwned; Gates evaluates conditions over a frozen GateSnapshot, with
DayOnly and UnlockNight for the two readings of DayAtLeast. The fired-flag
format moves to FlagKeys, next to the dialog done flag.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 2: The effect-op rule in Domain (`EffectOps`)

`EffectOpType` moves from `EffectSO.cs` into Domain (ints unchanged; the trailing comments become docs) with `ActsWhileActive`, the rule a dialog's effect is checked against (spec R24).

**Files:**
- Create: `Assets/Scripts/Domain/EffectOps.cs` (+ `.meta`)
- Test: `Assets/Tests/EditMode/EffectOpsTests.cs` (+ `.meta`)
- Modify: `Assets/Scripts/EffectSO.cs` (enum removed)

- [ ] **Step 1: Write the failing test** (`Assets/Tests/EditMode/EffectOpsTests.cs`, Write tool; then `make_meta.py`)

```csharp
using NUnit.Framework;

/// <summary>
/// Which effect ops change play for as long as their effect is active (a
/// dialog's effect may hold none of them), and the ints assets store.
/// </summary>
public class EffectOpsTests
{
    [TestCase(EffectOpType.SetFlag, false)]
    [TestCase(EffectOpType.ClearFlag, false)]
    [TestCase(EffectOpType.AddCounter, false)]
    [TestCase(EffectOpType.AddMoney, false)]
    [TestCase(EffectOpType.AddStability, false)]
    [TestCase(EffectOpType.UnlockUpgrade, false)]
    [TestCase(EffectOpType.AddAttributeScore, false)]
    [TestCase(EffectOpType.AddNationScore, false)]
    [TestCase(EffectOpType.LegendaryChanceBonus, true)]
    [TestCase(EffectOpType.ForgeryChanceBonus, true)]
    [TestCase(EffectOpType.PayRateBonus, true)]
    [TestCase(EffectOpType.VisitorTagWeight, true)]
    [TestCase(EffectOpType.ShopDiscountPercent, true)]
    [TestCase(EffectOpType.CaseBlueprintWeight, true)]
    [TestCase(EffectOpType.Cue, true)]
    [TestCase(EffectOpType.BriefingLine, false)]
    [TestCase(EffectOpType.NewsLine, false)]
    public void ActsWhileActive_OnlyTheContinuousModifiersAndCues(EffectOpType type, bool expected)
    {
        Assert.AreEqual(expected, EffectOps.ActsWhileActive(type));
    }

    [Test]
    public void EffectOpType_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)EffectOpType.SetFlag);
        Assert.AreEqual(1, (int)EffectOpType.ClearFlag);
        Assert.AreEqual(2, (int)EffectOpType.AddCounter);
        Assert.AreEqual(3, (int)EffectOpType.AddMoney);
        Assert.AreEqual(4, (int)EffectOpType.AddStability);
        Assert.AreEqual(5, (int)EffectOpType.UnlockUpgrade);
        Assert.AreEqual(6, (int)EffectOpType.AddAttributeScore);
        Assert.AreEqual(7, (int)EffectOpType.AddNationScore);
        Assert.AreEqual(8, (int)EffectOpType.LegendaryChanceBonus);
        Assert.AreEqual(9, (int)EffectOpType.ForgeryChanceBonus);
        Assert.AreEqual(10, (int)EffectOpType.PayRateBonus);
        Assert.AreEqual(11, (int)EffectOpType.VisitorTagWeight);
        Assert.AreEqual(12, (int)EffectOpType.ShopDiscountPercent);
        Assert.AreEqual(13, (int)EffectOpType.CaseBlueprintWeight);
        Assert.AreEqual(14, (int)EffectOpType.Cue);
        Assert.AreEqual(15, (int)EffectOpType.BriefingLine);
        Assert.AreEqual(16, (int)EffectOpType.NewsLine);
    }
}
```

- [ ] **Step 2: Run the compile check — expect failure**

Expected: `== TimeDeskEditMode.csproj: exit 1` with `error CS0103: The name 'EffectOpType' does not exist in the current context` and `error CS0246: The type or namespace name 'EffectOpType' could not be found`.

- [ ] **Step 3: Implement** — write `Assets/Scripts/Domain/EffectOps.cs` (Write tool; then `make_meta.py`):

```csharp
/// <summary>
/// What an effect op does. Serialized as ints: append only.
/// INSTANT ops run once when the effect activates.
/// CONTINUOUS ops are aggregated by TimelineEffects queries while the effect is active.
/// </summary>
public enum EffectOpType
{
    // ---- Instant (applied once on activation) ----

    /// <summary>stringParam = flag</summary>
    SetFlag,

    /// <summary>stringParam = flag</summary>
    ClearFlag,

    /// <summary>stringParam = counter key, floatParam = amount</summary>
    AddCounter,

    /// <summary>floatParam = credits (can be negative)</summary>
    AddMoney,

    /// <summary>floatParam = stability delta</summary>
    AddStability,

    /// <summary>stringParam = upgrade id</summary>
    UnlockUpgrade,

    /// <summary>profile + attribute + floatParam</summary>
    AddAttributeScore,

    /// <summary>nation + floatParam</summary>
    AddNationScore,

    // ---- Continuous (queried while active) ----

    /// <summary>floatParam = +chance (0..1)</summary>
    LegendaryChanceBonus,

    /// <summary>floatParam = +liar chance (0..1)</summary>
    ForgeryChanceBonus,

    /// <summary>floatParam = +multiplier (0.25 = +25% pay)</summary>
    PayRateBonus,

    /// <summary>stringParam = archetype tag, floatParam = weight multiplier</summary>
    VisitorTagWeight,

    /// <summary>stringParam = upgrade id ("" = all), floatParam = percent off</summary>
    ShopDiscountPercent,

    /// <summary>stringParam = blueprint name, floatParam = weight multiplier</summary>
    CaseBlueprintWeight,

    /// <summary>stringParam = cue id, consumed by receivers of this effect's channel</summary>
    Cue,

    /// <summary>stringParam = line added to tomorrow's briefing</summary>
    BriefingLine,

    /// <summary>stringParam = line added to tomorrow's newsletter</summary>
    NewsLine
}

/// <summary>Rules over effect ops, pure so they are tested headless.</summary>
public static class EffectOps
{
    /// <summary>
    /// True for the continuous ops that change play for as long as the effect
    /// is active (legendary, liar and pay bonuses, visitor and blueprint
    /// weights, shop discounts, cues); false for the instant ops and for
    /// BriefingLine/NewsLine, which act once or only through the next
    /// morning's paper. A narrative dialog's effect may hold only the latter.
    /// </summary>
    public static bool ActsWhileActive(EffectOpType type)
    {
        switch (type)
        {
            case EffectOpType.LegendaryChanceBonus:
            case EffectOpType.ForgeryChanceBonus:
            case EffectOpType.PayRateBonus:
            case EffectOpType.VisitorTagWeight:
            case EffectOpType.ShopDiscountPercent:
            case EffectOpType.CaseBlueprintWeight:
            case EffectOpType.Cue:
                return true;
            default:
                return false;
        }
    }
}
```

Then remove the enum from `EffectSO.cs` — save as `SCRATCH/p3_t02.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from cut import cut
W = r'E:\unity\NOPE-feat-clock'

# The op enum moves to Domain EffectOps.cs (values and order unchanged).
cut(W + r'\Assets\Scripts\EffectSO.cs', """/// <summary>
/// What a single effect operation does.
/// INSTANT ops run once when the effect activates.""", """    NewsLine                 // stringParam = line added to tomorrow's newsletter
}

""")
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 250, failed 0` (+ 17 cases + 1).

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Domain/EffectOps.cs Assets/Scripts/Domain/EffectOps.cs.meta Assets/Tests/EditMode/EffectOpsTests.cs Assets/Tests/EditMode/EffectOpsTests.cs.meta Assets/Scripts/EffectSO.cs && git commit -F - <<'EOF'
feat(domain): effect ops in Domain with the acts-while-active rule

EffectOpType moves to Domain with its ints unchanged; EffectOps.ActsWhileActive
names the continuous ops a narrative dialog's effect may not hold.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 3: The provable-category rule (`Forgery.IsProvableCategory`)

The category switch at the top of `IsProvableTell` becomes `IsProvableCategory`, the one rule questions and tells share (spec R20). `IsProvableTell` calls it; its results are unchanged, which the existing `ForgeryTests` keep proving.

**Files:**
- Modify: `Assets/Scripts/Domain/Forgery.cs` (whole file)
- Test: `Assets/Tests/EditMode/ForgeryTests.cs`

- [ ] **Step 1: Write the failing tests** — save as `SCRATCH/p3_t03_test.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Tests\EditMode\ForgeryTests.cs', [
("""    [Test]
    public void MissingTableOrBooks_IsNotATell()
    {
        Assert.IsFalse(Forgery.IsProvableTell(ClueCategory.Currency, "egypt", "ancient", "1 Jan 5", IraqHome, null, BookCategories));
        Assert.IsFalse(Forgery.IsProvableTell(ClueCategory.Currency, "egypt", "ancient", "1 Jan 5", IraqHome, World(), null));
    }
}""",
"""    [Test]
    public void MissingTableOrBooks_IsNotATell()
    {
        Assert.IsFalse(Forgery.IsProvableTell(ClueCategory.Currency, "egypt", "ancient", "1 Jan 5", IraqHome, null, BookCategories));
        Assert.IsFalse(Forgery.IsProvableTell(ClueCategory.Currency, "egypt", "ancient", "1 Jan 5", IraqHome, World(), null));
    }

    // -----------------------------
    // IsProvableCategory: the one rule questions and tells share
    // -----------------------------

    /// <summary>Every category, as a book set covering all of them.</summary>
    private static HashSet<ClueCategory> EveryBook() =>
        new HashSet<ClueCategory>((ClueCategory[])System.Enum.GetValues(typeof(ClueCategory)));

    [Test]
    public void IsProvableCategory_Name_IsNever_EvenWithEveryBook()
    {
        Assert.IsFalse(Forgery.IsProvableCategory(ClueCategory.Name, EveryBook()));
    }

    [Test]
    public void IsProvableCategory_BirthDate_IsAlways_TheRecordProvesIt()
    {
        Assert.IsTrue(Forgery.IsProvableCategory(ClueCategory.BirthDate, new HashSet<ClueCategory>()));
        Assert.IsTrue(Forgery.IsProvableCategory(ClueCategory.BirthDate, null));
    }

    [Test]
    public void IsProvableCategory_APlaceFact_ExactlyWhenABookCoversIt()
    {
        Assert.IsTrue(Forgery.IsProvableCategory(ClueCategory.Currency, BookCategories));
        Assert.IsFalse(Forgery.IsProvableCategory(ClueCategory.Geography, BookCategories));
        Assert.IsTrue(Forgery.IsProvableCategory(ClueCategory.Geography, EveryBook()));
    }

    [Test]
    public void IsProvableCategory_ANullBookSet_ProvesNoPlaceFact()
    {
        foreach (ClueCategory category in new[] { ClueCategory.Language, ClueCategory.Material, ClueCategory.Politics, ClueCategory.Technology, ClueCategory.Currency, ClueCategory.Geography, ClueCategory.Culture })
            Assert.IsFalse(Forgery.IsProvableCategory(category, null), category.ToString());
    }
}"""),
])
```

- [ ] **Step 2: Run the compile check — expect failure**

Expected: `== TimeDeskEditMode.csproj: exit 1`, `error CS0117: 'Forgery' does not contain a definition for 'IsProvableCategory'`.

- [ ] **Step 3: Implement** — replace the whole of `Assets/Scripts/Domain/Forgery.cs` (Write tool, LF):

```csharp
using System.Collections.Generic;

/// <summary>
/// Which categories can carry a liar's tell, on the papers or in an answer:
/// only those the player can disprove; a place-fact tell's origin proof can
/// only name the true home. Pure, so the decision table is tested headless.
/// </summary>
public static class Forgery
{
    /// <summary>
    /// Whether any tell in this category could ever be proven: the one rule
    /// interview questions and tells share. Never a name; always a birth date
    /// (the Citizen Record proves it); a place fact exactly when
    /// <paramref name="bookCategories"/> is non-null and holds the category.
    /// </summary>
    public static bool IsProvableCategory(ClueCategory category, ICollection<ClueCategory> bookCategories)
    {
        switch (category)
        {
            case ClueCategory.Name:
                return false;
            case ClueCategory.BirthDate:
                return true;
            default:
                return bookCategories != null && bookCategories.Contains(category);
        }
    }

    /// <summary>
    /// Whether a liar claiming (<paramref name="claimNationId"/>, <paramref name="claimEraId"/>)
    /// whose true home is <paramref name="home"/> can leak a provable tell in
    /// <paramref name="category"/>: never outside <see cref="IsProvableCategory"/>;
    /// a birth date when the cover date is readable and the home's birth years
    /// hold another year (BirthDates.HasOtherYear); a place fact when today's
    /// table holds both the claim's and the home's value, they differ under the
    /// scanner comparison, and no other place today shares the home's value (so
    /// the origin proof can only name the home).
    /// </summary>
    public static bool IsProvableTell(ClueCategory category, string claimNationId, string claimEraId,
                                      string coverBirthDate, HomeCandidate home, FactTable facts,
                                      ICollection<ClueCategory> bookCategories)
    {
        if (!IsProvableCategory(category, bookCategories))
            return false;

        if (category == ClueCategory.BirthDate)
            return BirthDates.HasOtherYear(coverBirthDate, home.BirthYearMin, home.BirthYearMax);

        if (facts == null)
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

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 254, failed 0`.

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Domain/Forgery.cs Assets/Tests/EditMode/ForgeryTests.cs && git commit -F - <<'EOF'
refactor(domain): extract Forgery.IsProvableCategory

The category rule at the top of IsProvableTell (never a name, always a
birth date, a place fact only with a book) becomes the one rule questions
and tells share. IsProvableTell's results are unchanged.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 4: The per-traveller dialog stream (`Seeds.ForDialog`)

**Files:**
- Modify: `Assets/Scripts/Domain/Seeds.cs`
- Test: `Assets/Tests/EditMode/SeedsTests.cs`

- [ ] **Step 1: Write the failing test** — save as `SCRATCH/p3_t04_test.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Tests\EditMode\SeedsTests.cs', [
("""    [Test]
    public void Mix_IsDeterministic_AndSaltSensitive()""",
"""    [Test]
    public void DialogStream_IsDistinctFromCaseClueLieAndViolatorStreams()
    {
        int daySeed = Seeds.Day(12345, 2);
        var cases = new HashSet<int>(Enumerable.Range(1, 20).Select(c => Seeds.ForCase(daySeed, c)));
        var dialogs = new HashSet<int>();
        for (int c = 1; c <= 20; c++)
        {
            int caseSeed = Seeds.ForCase(daySeed, c);
            int dialog = Seeds.ForDialog(caseSeed);
            Assert.IsFalse(cases.Contains(dialog), $"case {c}: the dialog seed is a case seed");
            Assert.AreNotEqual(Seeds.ForClues(caseSeed), dialog, $"case {c}: dialog seed equals the clue seed");
            Assert.AreNotEqual(Seeds.ForLies(caseSeed), dialog, $"case {c}: dialog seed equals the lie seed");
            Assert.AreNotEqual(Seeds.ForViolators(daySeed), dialog, $"case {c}: dialog seed equals the violator seed");
            Assert.AreEqual(dialog, Seeds.ForDialog(caseSeed), $"case {c}: not deterministic");
            dialogs.Add(dialog);
        }
        Assert.AreEqual(20, dialogs.Count, "dialog seeds repeat across cases");
    }

    [Test]
    public void Mix_IsDeterministic_AndSaltSensitive()"""),
])
```

- [ ] **Step 2: Run the compile check — expect failure**

Expected: `== TimeDeskEditMode.csproj: exit 1`, `error CS0117: 'Seeds' does not contain a definition for 'ForDialog'`.

- [ ] **Step 3: Implement** — save as `SCRATCH/p3_t04.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Scripts\Domain\Seeds.cs', [
("""    public const int LieSalt = 0x4C494553;""",
"""    public const int LieSalt = 0x4C494553;

    /// <summary>Salt for a traveller's dialog stream ("DIAG").</summary>
    public const int DialogSalt = 0x44494147;"""),
("""    public static int ForLies(int caseSeed) => Mix(caseSeed, LieSalt);""",
"""    public static int ForLies(int caseSeed) => Mix(caseSeed, LieSalt);

    /// <summary>
    /// Seed for one traveller's dialog variant picks (small talk), apart from
    /// the case and lie streams so content never changes who travellers are
    /// or who lies.
    /// </summary>
    public static int ForDialog(int caseSeed) => Mix(caseSeed, DialogSalt);"""),
])
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 255, failed 0`.

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Domain/Seeds.cs Assets/Tests/EditMode/SeedsTests.cs && git commit -F - <<'EOF'
feat(domain): per-traveller dialog stream (Seeds.ForDialog)

A salted stream for dialog variant picks (small talk), apart from the case
and lie streams so content never changes who travellers are or who lies.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 5: Proof from any statement (`DiscrepancyLog.Prove`/`Add`, `ClueLabels`)

`TryRegister` splits into the pure `Prove` (a statement, document field **or answer**, against exactly one truth source) and `Add` (one discrepancy per category), so the desk can tell "proves nothing" from "already documented" (spec R17, Q3, Q12, R13). Its only production caller, `HandlePairCompared`, moves in the same commit and shows the new "ALREADY DOCUMENTED" note. Reports gain the source and the report labels (Technology now reads DEVICE). No answer row exists yet (Task 14), so the FEATURES change here covers the note and the labels only.

**Files:**
- Create: `Assets/Scripts/Domain/ClueLabels.cs` (+ `.meta`)
- Modify: `Assets/Scripts/Domain/DiscrepancyLog.cs`, `Assets/Scripts/UI/InvestigationUIController.cs`, `Assets/Scripts/UI/CompareController.cs`, `docs/FEATURES.md`
- Test: `Assets/Tests/EditMode/DiscrepancyLogTests.cs`, `Assets/Tests/EditMode/FactTableTests.cs`, `Assets/Tests/EditMode/LiesTests.cs`

- [ ] **Step 1: Write the failing tests** — save as `SCRATCH/p3_t05_test.py` and run (the 22 `TryRegister` calls of `DiscrepancyLogTests` go through a test-local `Register` helper, so every existing assertion stays as it is):

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
from subsn import replace_n
W = r'E:\unity\NOPE-feat-clock'
T = W + r'\Assets\Tests\EditMode'

# Every existing TryRegister call goes through a test-local helper over Prove + Add,
# so every existing assertion stays as it is.
replace_n(T + r'\DiscrepancyLogTests.cs', 'log.TryRegister(', 'Register(log, ', 22)

apply(T + r'\DiscrepancyLogTests.cs', [
("""/// <summary>
/// Decision table for DiscrepancyLog.TryRegister — the core verification rule.""",
"""/// <summary>
/// Decision table for DiscrepancyLog.Prove and Add — the core verification rule."""),
("""    private static CompareEvidence TellIdentityField(ClueCategory category, string value) => new CompareEvidence
    {
        kind = EvidenceKind.DocumentField,
        category = category,
        value = value,
        isAnachronism = true
    };
""",
"""    private static CompareEvidence TellIdentityField(ClueCategory category, string value) => new CompareEvidence
    {
        kind = EvidenceKind.DocumentField,
        category = category,
        value = value,
        isAnachronism = true
    };

    /// <summary>A spoken Technology answer: a liar's Answer tell by default.</summary>
    private static CompareEvidence SaidDevice(string value = "Aqueduct", bool isTell = true) =>
        CompareEvidence.ForAnswer(ClueCategory.Technology, value, isTell);

    /// <summary>Proves the pair and documents the proof; the proof when the log accepts it, else null.</summary>
    private static Discrepancy Register(DiscrepancyLog log, CompareEvidence a, CompareEvidence b, string claimedNationId, string claimedEraId)
    {
        Discrepancy proof = DiscrepancyLog.Prove(a, b, claimedNationId, claimedEraId);
        return log.Add(proof) ? proof : null;
    }
"""),
("""    [Test]
    public void Clear_EmptiesTheLog()
    {
        var log = new DiscrepancyLog();
        Register(log, TellDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra);
        log.Clear();

        Assert.AreEqual(0, log.Count);
    }
}""",
"""    [Test]
    public void Clear_EmptiesTheLog()
    {
        var log = new DiscrepancyLog();
        Register(log, TellDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra);
        log.Clear();

        Assert.AreEqual(0, log.Count);
    }

    // -----------------------------
    // Spoken answers (the interview)
    // -----------------------------

    [Test]
    public void AnswerTell_VsTheClaimsRow_ProvesAMismatch_SaidByTheTraveller()
    {
        Discrepancy d = DiscrepancyLog.Prove(SaidDevice(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra);

        Assert.NotNull(d);
        Assert.AreEqual(DiscrepancyProof.ClaimMismatch, d.provedBy);
        Assert.AreEqual(EvidenceKind.Answer, d.source);
        StringAssert.Contains("traveller said: \\"Aqueduct\\"", d.Summary);
        StringAssert.Contains("expected: \\"Longship\\"", d.Summary);
    }

    [Test]
    public void AnswerTell_VsAForeignRow_ProvesTheOrigin_SaidByTheTraveller()
    {
        Discrepancy d = DiscrepancyLog.Prove(Entry("latia", "rome", "Aqueduct"), SaidDevice(), ClaimNation, ClaimEra);

        Assert.NotNull(d);
        Assert.AreEqual(DiscrepancyProof.ForeignOrigin, d.provedBy);
        Assert.AreEqual("latia — rome", d.actualOrigin);
        StringAssert.Contains("traveller said \\"Aqueduct\\", which belongs to latia — rome", d.Summary);
    }

    [Test]
    public void AnswerTell_VsTheRecord_ProvesARecordMismatch()
    {
        Discrepancy d = DiscrepancyLog.Prove(
            CompareEvidence.ForAnswer(ClueCategory.BirthDate, "3 Jun 1801 BCE", true),
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 Jun 1510 BCE"),
            ClaimNation, ClaimEra);

        Assert.NotNull(d);
        Assert.AreEqual(DiscrepancyProof.RecordMismatch, d.provedBy);
        Assert.AreEqual("BIRTH DATE INCORRECT — traveller said: \\"3 Jun 1801 BCE\\"  /  agency records: \\"3 Jun 1510 BCE\\"", d.Summary);
    }

    [Test]
    public void HonestAnswer_NeverRegisters_EitherWayOrOrder()
    {
        Assert.IsNull(DiscrepancyLog.Prove(SaidDevice("Longship", false), Entry("norvik", "medieval", "Waterwheel"), ClaimNation, ClaimEra));
        Assert.IsNull(DiscrepancyLog.Prove(Entry("latia", "rome", "Longship"), SaidDevice("Longship", false), ClaimNation, ClaimEra));
        Assert.IsNull(DiscrepancyLog.Prove(
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 May 1131"),
            CompareEvidence.ForAnswer(ClueCategory.BirthDate, "3 May 1101", false),
            ClaimNation, ClaimEra));
    }

    [Test]
    public void AnswerVsPapers_AndAnswerVsAnswer_ProveNothing()
    {
        Assert.IsNull(DiscrepancyLog.Prove(SaidDevice(), HonestDocField(), ClaimNation, ClaimEra));
        Assert.IsNull(DiscrepancyLog.Prove(TellDocField(), SaidDevice("Longship", false), ClaimNation, ClaimEra));
        Assert.IsNull(DiscrepancyLog.Prove(SaidDevice(), SaidDevice("Longship"), ClaimNation, ClaimEra));
    }

    [Test]
    public void PaperSummaries_StillSayPapers_AndUseTheReportLabel()
    {
        Discrepancy mismatch = DiscrepancyLog.Prove(TellDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra);
        Assert.AreEqual(EvidenceKind.DocumentField, mismatch.source);
        Assert.AreEqual("DEVICE INCORRECT — papers: \\"Aqueduct\\"  /  expected: \\"Longship\\"", mismatch.Summary);

        Discrepancy origin = DiscrepancyLog.Prove(TellDocField(), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra);
        Assert.AreEqual("DEVICE INCORRECT — papers show \\"Aqueduct\\", which belongs to latia — rome", origin.Summary);
    }

    [Test]
    public void Add_RefusesASecondProofOfADocumentedCategory_FromEitherSource_AndNull()
    {
        var log = new DiscrepancyLog();
        Assert.IsTrue(log.Add(DiscrepancyLog.Prove(TellDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra)));
        Assert.IsFalse(log.Add(DiscrepancyLog.Prove(SaidDevice(), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra)), "same category, spoken");
        Assert.IsFalse(log.Add(null));
        Assert.AreEqual(1, log.Count);
        Assert.AreEqual(EvidenceKind.DocumentField, log.Items[0].source);
    }

    [Test]
    public void Prove_IsPure_ADocumentedCategoryStillProves_TheSameWayTwice()
    {
        var log = new DiscrepancyLog();
        Assert.IsTrue(log.Add(DiscrepancyLog.Prove(TellDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra)));

        // Technology is documented now; Prove never reads the log, so the spoken pair still proves.
        Discrepancy x = DiscrepancyLog.Prove(SaidDevice(), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra);
        Discrepancy y = DiscrepancyLog.Prove(SaidDevice(), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra);

        Assert.NotNull(x, "an already documented category still proves; only Add refuses it");
        Assert.AreEqual(DiscrepancyProof.ForeignOrigin, x.provedBy);
        Assert.AreNotSame(x, y);
        Assert.AreEqual(x.Summary, y.Summary);
        Assert.AreEqual(x.provedBy, y.provedBy);
        Assert.AreEqual(x.source, y.source);
        Assert.AreEqual(1, log.Count, "proving documents nothing");
        Assert.AreEqual(EvidenceKind.DocumentField, log.Items[0].source);
    }

    [TestCase(ClueCategory.Language, "LANGUAGE")]
    [TestCase(ClueCategory.Material, "MATERIAL")]
    [TestCase(ClueCategory.Politics, "RULER")]
    [TestCase(ClueCategory.Technology, "DEVICE")]
    [TestCase(ClueCategory.Currency, "CURRENCY")]
    [TestCase(ClueCategory.Geography, "CAPITAL")]
    [TestCase(ClueCategory.Culture, "CULTURE")]
    [TestCase(ClueCategory.Name, "NAME")]
    [TestCase(ClueCategory.BirthDate, "BIRTH DATE")]
    public void ClueLabels_Report_OneLabelPerCategory(ClueCategory category, string expected)
    {
        Assert.AreEqual(expected, ClueLabels.Report(category));
    }
}"""),
])

apply(T + r'\FactTableTests.cs', [
("""        Discrepancy d = new DiscrepancyLog().TryRegister(tell, babylon.ToEvidence(), "egypt", "ancient");""",
"""        Discrepancy d = DiscrepancyLog.Prove(tell, babylon.ToEvidence(), "egypt", "ancient");"""),
("""        Discrepancy d = new DiscrepancyLog().TryRegister(tell, egypt.ToEvidence(), "egypt", "ancient");""",
"""        Discrepancy d = DiscrepancyLog.Prove(tell, egypt.ToEvidence(), "egypt", "ancient");"""),
])

apply(T + r'\LiesTests.cs', [
("""                Discrepancy record = new DiscrepancyLog().TryRegister(doc, CompareEvidence.ForRecordField(ClueCategory.BirthDate, Cover), "egypt", "ancient");""",
"""                Discrepancy record = DiscrepancyLog.Prove(doc, CompareEvidence.ForRecordField(ClueCategory.BirthDate, Cover), "egypt", "ancient");"""),
("""                Discrepancy d = new DiscrepancyLog().TryRegister(doc, row.ToEvidence(), "egypt", "ancient");""",
"""                Discrepancy d = DiscrepancyLog.Prove(doc, row.ToEvidence(), "egypt", "ancient");"""),
])
```

- [ ] **Step 2: Run the compile check — expect failure**

Expected: `== TimeDeskEditMode.csproj: exit 1` with `error CS0117: 'DiscrepancyLog' does not contain a definition for 'Prove'`, `'CompareEvidence' does not contain a definition for 'ForAnswer'`, `'EvidenceKind' does not contain a definition for 'Answer'`, `'Discrepancy' does not contain a definition for 'source'` and `'DiscrepancyLog' does not contain a definition for 'Add'`.

- [ ] **Step 3: Implement** — write `Assets/Scripts/Domain/ClueLabels.cs` (Write tool; then `make_meta.py`):

```csharp
/// <summary>
/// The one set of upper-case category labels the player reads in reports:
/// the Deviation Report, the compare bar's notes and interview answer rows.
/// </summary>
public static class ClueLabels
{
    /// <summary>
    /// Report label of a category: Geography is CAPITAL, Politics RULER,
    /// Technology DEVICE, BirthDate BIRTH DATE, and every other category its
    /// upper-case name.
    /// </summary>
    public static string Report(ClueCategory category)
    {
        switch (category)
        {
            case ClueCategory.Geography: return "CAPITAL";
            case ClueCategory.Politics: return "RULER";
            case ClueCategory.Technology: return "DEVICE";
            case ClueCategory.BirthDate: return "BIRTH DATE";
            default: return category.ToString().ToUpperInvariant();
        }
    }
}
```

Then save as `SCRATCH/p3_t05.py` and run (the Domain change, its production caller, the compare-bar note and the behaviour contract):

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
from cut import cut
W = r'E:\unity\NOPE-feat-clock'
D = W + r'\Assets\Scripts\Domain\DiscrepancyLog.cs'

apply(D, [
("""    /// <summary>A field from the agency's citizen records.</summary>
    RecordField
}""",
"""    /// <summary>A field from the agency's citizen records.</summary>
    RecordField,

    /// <summary>A traveller's spoken answer in the interview transcript.</summary>
    Answer
}"""),
("""    /// <summary>Papers differ from the claimed era's reference entry.</summary>
    ClaimMismatch,

    /// <summary>Papers match a reference entry that belongs to a different origin.</summary>
    ForeignOrigin,

    /// <summary>Papers differ from the agency's citizen record.</summary>
    RecordMismatch""",
"""    /// <summary>The statement (papers or answer) differs from the claimed place's reference entry.</summary>
    ClaimMismatch,

    /// <summary>The statement matches a reference entry that belongs to a different origin.</summary>
    ForeignOrigin,

    /// <summary>The statement differs from the agency's citizen record.</summary>
    RecordMismatch"""),
("""    /// <summary>Document side: true if the value is a liar's tell (anachronistic for the claim).</summary>
    public bool isAnachronism;""",
"""    /// <summary>Statement side (document field or answer): true if the value is a liar's tell.</summary>
    public bool isAnachronism;"""),
("""    /// <summary>Evidence for a clicked citizen-record field.</summary>
    public static CompareEvidence ForRecordField(ClueCategory category, string value) => new CompareEvidence
    {
        kind = EvidenceKind.RecordField,
        category = category,
        value = value,
        entryOriginLabel = "agency records"
    };
}""",
"""    /// <summary>Evidence for a clicked citizen-record field.</summary>
    public static CompareEvidence ForRecordField(ClueCategory category, string value) => new CompareEvidence
    {
        kind = EvidenceKind.RecordField,
        category = category,
        value = value,
        entryOriginLabel = "agency records"
    };

    /// <summary>Evidence for a clicked interview answer row: the canonical value the traveller said, a tell when <paramref name="isTell"/>.</summary>
    public static CompareEvidence ForAnswer(ClueCategory category, string value, bool isTell) => new CompareEvidence
    {
        kind = EvidenceKind.Answer,
        category = category,
        value = value,
        isAnachronism = isTell
    };
}"""),
("""    /// <summary>The tell's value printed on the visitor's papers.</summary>
    public string documentValue;""",
"""    /// <summary>The tell's value, as printed or as spoken.</summary>
    public string documentValue;"""),
("""    /// <summary>
    /// Match proof: where the printed value actually belongs (a different
    /// nation/era than claimed). Null when proved by mismatch.
    /// </summary>
    public string actualOrigin;""",
"""    /// <summary>
    /// Match proof: where the printed or spoken value actually belongs (a
    /// different nation/era than claimed). Null when proved by mismatch.
    /// </summary>
    public string actualOrigin;"""),
])

# Discrepancy.source + Summary (report label and where the tell was stated); CategoryLabel goes.
cut(D, """    /// <summary>How this contradiction was proved.</summary>
    public DiscrepancyProof provedBy;
""", """        c == ClueCategory.BirthDate ? "BIRTH DATE" : c.ToString().ToUpperInvariant();
}
""", """    /// <summary>How this contradiction was proved.</summary>
    public DiscrepancyProof provedBy;

    /// <summary>Where the tell was stated: DocumentField (papers) or Answer (the traveller said it).</summary>
    public EvidenceKind source;

    /// <summary>Player-facing report line ("CAPITAL INCORRECT — traveller said: ..."), naming where the tell was stated.</summary>
    public string Summary
    {
        get
        {
            string what = ClueLabels.Report(category);
            bool said = source == EvidenceKind.Answer;
            switch (provedBy)
            {
                case DiscrepancyProof.ForeignOrigin:
                    return said
                        ? $"{what} INCORRECT — traveller said \\"{documentValue}\\", which belongs to {actualOrigin}"
                        : $"{what} INCORRECT — papers show \\"{documentValue}\\", which belongs to {actualOrigin}";
                case DiscrepancyProof.RecordMismatch:
                    return $"{what} INCORRECT — {Stated(said)}: \\"{documentValue}\\"  /  agency records: \\"{expectedValue}\\"";
                default:
                    return $"{what} INCORRECT — {Stated(said)}: \\"{documentValue}\\"  /  expected: \\"{expectedValue}\\"";
            }
        }
    }

    /// <summary>Who stated the value, as a mismatch line names it.</summary>
    private static string Stated(bool said) => said ? "traveller said" : "papers";
}
""")

# The class doc and TryRegister -> the pure Prove plus Add.
cut(D, """/// <summary>
/// Per-case list of documented contradictions (the "Deviation Report").""", """public sealed class DiscrepancyLog
{""", """/// <summary>
/// Per-case list of documented contradictions (the Deviation Report). Pure C#
/// so the rules are unit-testable. <see cref="Prove"/> decides whether a
/// compared pair is a true contradiction: a statement (document field or
/// answer) against one truth source (a reference entry or the citizen
/// record), proved either way:
/// - MISMATCH proof: a liar's tell differs from the reference entry that
///   applies to the CLAIMED nation+era, or from the agency's record.
/// - MATCH proof: a liar's tell equals a reference entry that does
///   NOT apply to the claim — the value provably belongs somewhere else
///   (e.g. papers claim Medieval but the declared device matches Ancient Rome).
/// <see cref="Add"/> documents it once per category.
/// </summary>
public sealed class DiscrepancyLog
{""")

cut(D, """    /// <summary>
    /// Validates a compared pair against the current claim and registers it if""", """        _items.Add(found);
        return found;
    }
""", """    /// <summary>
    /// Whether a compared pair proves a contradiction of the current claim:
    /// the proof, or null when it proves nothing. The statement side (a
    /// document field or an answer) must be a liar's tell and face exactly one
    /// truth source (a reference entry or a record field) of the same
    /// category; two statements or two truths prove nothing. Pure: no log changes.
    /// </summary>
    public static Discrepancy Prove(CompareEvidence a, CompareEvidence b, string claimedNationId, string claimedEraId)
    {
        CompareEvidence statement, truth;
        if (IsStatement(a.kind))
        {
            statement = a;
            truth = b;
        }
        else
        {
            statement = b;
            truth = a;
        }

        // Must be one statement (papers or answer) against one truth source (book or records).
        if (!IsStatement(statement.kind))
            return null;
        if (truth.kind != EvidenceKind.ReferenceEntry && truth.kind != EvidenceKind.RecordField)
            return null;

        // Same category — comparing Language against Currency proves nothing.
        if (statement.category != truth.category)
            return null;

        // Only a liar's tell is a contradiction; a coincidental
        // mismatch/match on an honest statement proves nothing.
        if (!statement.isAnachronism)
            return null;

        if (truth.kind == EvidenceKind.RecordField)
        {
            // Record proof: the statement disagrees with the agency's own
            // records about who this person is. No era claim involved.
            if (ValuesMatch(statement.value, truth.value))
                return null;

            return new Discrepancy
            {
                category = statement.category,
                documentValue = statement.value,
                expectedValue = truth.value,
                provedBy = DiscrepancyProof.RecordMismatch,
                source = statement.kind
            };
        }

        // Without a claim there is nothing to contradict.
        if (string.IsNullOrEmpty(claimedEraId))
            return null;

        bool entryAppliesToClaim =
            !string.IsNullOrEmpty(truth.entryEraId) && truth.entryEraId == claimedEraId &&
            (string.IsNullOrEmpty(truth.entryNationId) || truth.entryNationId == claimedNationId);

        bool valuesMatch = ValuesMatch(statement.value, truth.value);

        if (entryAppliesToClaim && !valuesMatch)
        {
            // Mismatch proof: the claim's reference disagrees with the statement.
            return new Discrepancy
            {
                category = statement.category,
                documentValue = statement.value,
                expectedValue = truth.value,
                provedBy = DiscrepancyProof.ClaimMismatch,
                source = statement.kind
            };
        }

        if (!entryAppliesToClaim && valuesMatch)
        {
            // Match proof: the stated value belongs to a different origin.
            return new Discrepancy
            {
                category = statement.category,
                documentValue = statement.value,
                actualOrigin = string.IsNullOrEmpty(truth.entryOriginLabel) ? "a different era" : truth.entryOriginLabel,
                provedBy = DiscrepancyProof.ForeignOrigin,
                source = statement.kind
            };
        }

        return null;
    }

    /// <summary>
    /// Documents a proof. False, with nothing added, when the proof is null or
    /// its category is already documented: one discrepancy per category is
    /// enough evidence, whatever its source.
    /// </summary>
    public bool Add(Discrepancy proof)
    {
        if (proof == null)
            return false;

        foreach (Discrepancy existing in _items)
            if (existing.category == proof.category)
                return false;

        _items.Add(proof);
        return true;
    }

    /// <summary>True for a statement row: a document field or a spoken answer.</summary>
    private static bool IsStatement(EvidenceKind kind) =>
        kind == EvidenceKind.DocumentField || kind == EvidenceKind.Answer;
""")

# The Scanner's only production caller: prove, then document once per category.
apply(W + r'\Assets\Scripts\UI\InvestigationUIController.cs', [
("""    /// <summary>
    /// Auto-registers a true contradiction when the player compares a liar's
    /// tell against the reference entry or record that disproves it.
    /// </summary>
    private void HandlePairCompared(CompareEvidence a, CompareEvidence b)
    {
        if (_currentCase == null)
            return;

        Discrepancy found = _discrepancies.TryRegister(a, b,
            _currentCase.claimedNation != null ? _currentCase.claimedNation.id : null,
            _currentCase.claimedEra != null ? _currentCase.claimedEra.id : null);
        if (found == null)
            return;

        RefreshScannerText();

        if (compareController != null)
            compareController.ShowDeviation(found.Summary);
""",
"""    /// <summary>
    /// Documents a true contradiction when the player compares a liar's tell
    /// against the reference entry or record that disproves it; proving an
    /// already documented category again only says so in the compare bar.
    /// </summary>
    private void HandlePairCompared(CompareEvidence a, CompareEvidence b)
    {
        if (_currentCase == null)
            return;

        Discrepancy proof = DiscrepancyLog.Prove(a, b,
            _currentCase.claimedNation != null ? _currentCase.claimedNation.id : null,
            _currentCase.claimedEra != null ? _currentCase.claimedEra.id : null);
        if (proof == null)
            return;

        if (!_discrepancies.Add(proof))
        {
            if (compareController != null)
                compareController.ShowAlreadyDocumented(ClueLabels.Report(proof.category));
            return;
        }

        RefreshScannerText();

        if (compareController != null)
            compareController.ShowDeviation(proof.Summary);
"""),
])

apply(W + r'\Assets\Scripts\UI\CompareController.cs', [
("""        compareText.color = mismatchColor;
        compareText.text = $"●  DEVIATION LOGGED — {summary}";
    }
""",
"""        compareText.color = mismatchColor;
        compareText.text = $"●  DEVIATION LOGGED — {summary}";
    }

    /// <summary>
    /// Replaces the compare bar verdict when a pair proves a category that is
    /// already in the Deviation Report, so a second proof visibly adds nothing.
    /// </summary>
    public void ShowAlreadyDocumented(string categoryLabel)
    {
        if (compareText == null)
            return;

        compareText.color = neutralColor;
        compareText.text = $"●  ALREADY DOCUMENTED — {categoryLabel} is in the Deviation Report";
    }
"""),
])

apply(W + r'\docs\FEATURES.md', [
("""  - [ ] One discrepancy per category; cleared per case; window auto-opens on first find
  - [ ] Compare bar flips to a red "DEVIATION LOGGED — …" verdict when evidence registers (never a green MATCH)""",
"""  - [ ] One discrepancy per category; a second proof of a documented category shows "ALREADY DOCUMENTED" in the compare bar and adds nothing; cleared per case; window auto-opens on first find
  - [ ] Compare bar flips to a red "DEVIATION LOGGED — …" verdict when evidence registers (never a green MATCH)
  - [ ] Reports label Geography CAPITAL, Politics RULER, Technology DEVICE and birth dates BIRTH DATE (tested: `DiscrepancyLogTests`)"""),
])
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 272, failed 0` (+ 8 tests + 9 label cases).

- [ ] **Step 5: Prove nothing calls the removed API**

Run: `cd /e/unity/NOPE-feat-clock && grep -rn "TryRegister\|CategoryLabel(" Assets --include=*.cs`
Expected: no output.

- [ ] **Step 6: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Domain/ClueLabels.cs Assets/Scripts/Domain/ClueLabels.cs.meta Assets/Scripts/Domain/DiscrepancyLog.cs Assets/Scripts/UI/InvestigationUIController.cs Assets/Scripts/UI/CompareController.cs docs/FEATURES.md Assets/Tests/EditMode/DiscrepancyLogTests.cs Assets/Tests/EditMode/FactTableTests.cs Assets/Tests/EditMode/LiesTests.cs && git commit -F - <<'EOF'
feat(domain): prove a contradiction from any statement (Prove/Add)

DiscrepancyLog.TryRegister splits into the pure Prove (a document field or
a spoken answer against exactly one truth source) and Add (one discrepancy
per category, from any source). Reports name where the tell was stated and
use one set of labels (ClueLabels: CAPITAL, RULER, DEVICE, BIRTH DATE). The
desk says ALREADY DOCUMENTED when a second proof of a category is dropped.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 6: Tells on two channels (`Lies.Plan`, `TellChannel`)

A tell becomes a (category, channel) option: Papers for a printed category, Answer for one of today's tell-carrying question categories, each open only when the day's channels allow it (spec X1, R1, R2, §2.3). One draw per tell over the open options; a pick closes the other channel of its category. `ApplyTo` rewrites Papers tells only. The day plan gains its channel knob (default Papers). `CaseFactory` passes no tell-carrying question yet, so every day stays exactly piece 2's until Task 14 wires the interview.

**Files:**
- Modify: `Assets/Scripts/Domain/Lies.cs` (whole file), `Assets/Scripts/DayPlanSO.cs`, `Assets/Scripts/CaseFactory.cs`
- Test: `Assets/Tests/EditMode/LiesTests.cs`

- [ ] **Step 1: Write the failing tests** — save as `SCRATCH/p3_t06_test.py` and run (the helper and the two direct calls pass no tell-carrying question and Papers only, so every existing test keeps its draws and assertions):

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Tests\EditMode\LiesTests.cs', [
("""/// Technology, Currency), so Iraq's eligible tells are BirthDate, Currency,
/// Language and Technology, in that order.
/// </summary>""",
"""/// Technology, Currency), so Iraq's eligible tells are BirthDate, Currency,
/// Language and Technology, in that order. The answer tests add capitals
/// (Egypt "Thebes", the twin " thebes ", Iraq "Babylon", Italy "Rome") and a
/// Geography book, and ask about Currency and Geography.
/// </summary>"""),
("""    private static readonly HashSet<ClueCategory> Books = new HashSet<ClueCategory> { ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology };
""",
"""    private static readonly HashSet<ClueCategory> Books = new HashSet<ClueCategory> { ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology };

    /// <summary>No question may carry an Answer tell.</summary>
    private static readonly ClueCategory[] None = new ClueCategory[0];

    /// <summary>Piece 2's channel set: tells only on the papers.</summary>
    private static readonly TellChannel[] PapersOnly = { TellChannel.Papers };

    /// <summary>Tells only in answers.</summary>
    private static readonly TellChannel[] AnswerOnly = { TellChannel.Answer };

    /// <summary>Days 2-3: tells on the papers or in answers.</summary>
    private static readonly TellChannel[] Both = { TellChannel.Papers, TellChannel.Answer };
"""),
("""        Lies.Plan(chance, tellCount, "egypt", "ancient", Cover, todays ?? Today4, papers ?? Papers(), facts ?? Facts(), Books, rng);""",
"""        Lies.Plan(chance, tellCount, "egypt", "ancient", Cover, todays ?? Today4, papers ?? Papers(), None, PapersOnly, facts ?? Facts(), Books, rng);"""),
("""    public void AnUnprintedCategory_IsNeverATell()""",
"""    public void AnUnprintedCategory_IsNeverAPapersTell()"""),
("""        LiePlan plan = Lies.Plan(1f, 1, "egypt", "ancient", Cover, new[] { claim, iraq, greece }, papers, facts, Books, rng);""",
"""        LiePlan plan = Lies.Plan(1f, 1, "egypt", "ancient", Cover, new[] { claim, iraq, greece }, papers, None, PapersOnly, facts, Books, rng);"""),
("""            Assert.AreEqual(3, Lies.Plan(1f, 1, "egypt", "ancient", Cover, new[] { claim, iraq, greece, italy }, papers, facts, Books, new SeededRandom(seed)).HomeIndex, $"seed {seed}");""",
"""            Assert.AreEqual(3, Lies.Plan(1f, 1, "egypt", "ancient", Cover, new[] { claim, iraq, greece, italy }, papers, None, PapersOnly, facts, Books, new SeededRandom(seed)).HomeIndex, $"seed {seed}");"""),
("""                else
                {
                    Assert.IsNull(d, where);
                }
            }
        }
    }
}""",
"""                else
                {
                    Assert.IsNull(d, where);
                }
            }
        }
    }

    // -----------------------------
    // Two channels: papers and answers
    // -----------------------------

    /// <summary>Books plus a Geography book (the Capitals Gazetteer).</summary>
    private static readonly HashSet<ClueCategory> AnswerBooks = new HashSet<ClueCategory> { ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology, ClueCategory.Geography };

    /// <summary>Facts() plus capitals: Egypt "Thebes", the twin " thebes ", Iraq "Babylon", Italy "Rome".</summary>
    private static FactTable AnswerFacts()
    {
        FactTable t = Facts();
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Geography, "Thebes");
        t.Add("greece", "ancient", "Periclean Athens (Ancient)", ClueCategory.Geography, " thebes ");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Geography, "Babylon");
        t.Add("italy", "ancient", "Republican Rome (Ancient)", ClueCategory.Geography, "Rome");
        return t;
    }

    private static LiePlan PlanSpoken(IRandomSource rng, IReadOnlyList<ClueCategory> answerTellCategories, IReadOnlyList<TellChannel> channels,
                                      int tellCount = 1, IReadOnlyList<HomeCandidate> todays = null, List<DocumentField> papers = null,
                                      FactTable facts = null) =>
        Lies.Plan(0.5f, tellCount, "egypt", "ancient", Cover, todays ?? Today4, papers ?? Papers(), answerTellCategories, channels,
                  facts ?? AnswerFacts(), AnswerBooks, rng);

    /// <summary>Every field still shows Egypt's honest value, unflagged.</summary>
    private static void AssertPapersAreTheCover(List<DocumentField> papers)
    {
        List<DocumentField> honest = Papers();
        for (int i = 0; i < papers.Count; i++)
        {
            Assert.AreEqual(honest[i].value, papers[i].value, papers[i].label);
            Assert.IsFalse(papers[i].isAnachronism, papers[i].label);
        }
    }

    [Test]
    public void GoldenOrder_Answer_TheOptionsArePapersThenAnswers()
    {
        // Iraq's options: P:BirthDate, P:Currency, P:Language, P:Technology, A:Currency, A:Geography.
        ScriptedRandom rng = Script(V(0f), R(0), R(5));
        List<DocumentField> papers = Papers();
        LiePlan plan = PlanSpoken(rng, new[] { ClueCategory.Currency, ClueCategory.Geography }, Both, papers: papers);

        Assert.AreEqual(LieOutcome.Liar, plan.Outcome);
        Assert.AreEqual(2, plan.HomeIndex);
        CollectionAssert.AreEqual(new[] { ClueCategory.Geography }, plan.Tells);
        Assert.AreEqual(TellChannel.Answer, plan.ChannelOf(ClueCategory.Geography));
        Assert.AreEqual("Babylon", plan.TellValue(ClueCategory.Geography));
        Assert.IsNull(plan.ChannelOf(ClueCategory.Currency), "not a tell");
        Assert.IsNull(plan.TellValue(ClueCategory.Currency), "not a tell");
        Assert.IsTrue(rng.Done, "exactly three draws");

        plan.ApplyTo(papers);
        AssertPapersAreTheCover(papers);
    }

    [Test]
    public void AnAnswerTell_InAPrintedCategory_LeavesThePapersOnTheCover()
    {
        ScriptedRandom rng = Script(V(0f), R(0), R(4));
        List<DocumentField> papers = Papers();
        LiePlan plan = PlanSpoken(rng, new[] { ClueCategory.Currency, ClueCategory.Geography }, Both, papers: papers);

        CollectionAssert.AreEqual(new[] { ClueCategory.Currency }, plan.Tells);
        Assert.AreEqual(TellChannel.Answer, plan.ChannelOf(ClueCategory.Currency));
        Assert.AreEqual("Silver shekel", plan.TellValue(ClueCategory.Currency));
        Assert.IsTrue(rng.Done);

        plan.ApplyTo(papers);
        AssertPapersAreTheCover(papers);
    }

    [Test]
    public void ACategoryLeaksOnOneChannelOnly()
    {
        ScriptedRandom rng = Script(V(0f), R(0), R(0), R(0), R(0), R(0), R(0), R(0));
        LiePlan plan = PlanSpoken(rng, new[] { ClueCategory.Currency, ClueCategory.Geography }, Both, tellCount: 9);

        CollectionAssert.AreEqual(new[] { ClueCategory.BirthDate, ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology, ClueCategory.Geography }, plan.Tells);
        CollectionAssert.AreEqual(
            new TellChannel?[] { TellChannel.Papers, TellChannel.Papers, TellChannel.Papers, TellChannel.Papers, TellChannel.Answer },
            plan.Tells.Select(t => plan.ChannelOf(t)).ToArray());
        Assert.IsTrue(rng.Done, "roll, home, five tells, then the birth year: 8 draws");
    }

    [Test]
    public void PapersOnly_ReproducesPiece2_WhateverQuestionsAreAsked()
    {
        for (int seed = 0; seed < 300; seed++)
        {
            List<DocumentField> asked = Papers(), unasked = Papers();
            LiePlan x = PlanSpoken(new SeededRandom(seed), new[] { ClueCategory.Currency, ClueCategory.Geography }, PapersOnly, papers: asked);
            LiePlan y = PlanSpoken(new SeededRandom(seed), None, PapersOnly, papers: unasked);

            Assert.AreEqual(y.Outcome, x.Outcome, $"seed {seed}");
            Assert.AreEqual(y.HomeIndex, x.HomeIndex, $"seed {seed}");
            CollectionAssert.AreEqual(y.Tells, x.Tells, $"seed {seed}");

            x.ApplyTo(asked);
            y.ApplyTo(unasked);
            CollectionAssert.AreEqual(unasked.Select(f => f.value).ToList(), asked.Select(f => f.value).ToList(), $"seed {seed}");
        }
    }

    [Test]
    public void AnswerOnly_WithNoTellCarryingQuestion_IsNoPossibleLie_AfterOneDraw()
    {
        ScriptedRandom rng = Script(V(0f));
        Assert.AreEqual(LieOutcome.NoPossibleLie, PlanSpoken(rng, None, AnswerOnly).Outcome);
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void AHomeEligibleOnlyThroughAnAnswer_IsACandidate()
    {
        // Ugarit matches the claim in everything printed and has no birth years; only its capital differs.
        var syria = new HomeCandidate("syria", "ancient", 0, 0);
        FactTable facts = AnswerFacts();
        facts.Add("syria", "ancient", "Ugarit (Ancient)", ClueCategory.Currency, "DEBEN");
        facts.Add("syria", "ancient", "Ugarit (Ancient)", ClueCategory.Language, "middle egyptian");
        facts.Add("syria", "ancient", "Ugarit (Ancient)", ClueCategory.Technology, "Papyrus");
        facts.Add("syria", "ancient", "Ugarit (Ancient)", ClueCategory.Geography, "Ugarit");
        var todays = new[] { Egypt, syria };

        ScriptedRandom spoken = Script(V(0f), R(0), R(0));
        LiePlan plan = PlanSpoken(spoken, new[] { ClueCategory.Geography }, Both, todays: todays, facts: facts);
        Assert.AreEqual(LieOutcome.Liar, plan.Outcome);
        Assert.AreEqual(1, plan.HomeIndex);
        Assert.AreEqual(TellChannel.Answer, plan.ChannelOf(ClueCategory.Geography));
        Assert.AreEqual("Ugarit", plan.TellValue(ClueCategory.Geography));
        Assert.IsTrue(spoken.Done);

        ScriptedRandom printed = Script(V(0f));
        Assert.AreEqual(LieOutcome.NoPossibleLie, PlanSpoken(printed, new[] { ClueCategory.Geography }, PapersOnly, todays: todays, facts: facts).Outcome);
        Assert.IsTrue(printed.Done);
    }

    [Test]
    public void ABirthDateAnswerTell_KeepsThePapersOnTheCover_AndDrawsTheYearLast()
    {
        // Only Iraq has a birth year other than the cover's.
        ScriptedRandom rng = Script(V(0f), R(0), R(0), R(2));
        List<DocumentField> papers = Papers();
        LiePlan plan = PlanSpoken(rng, new[] { ClueCategory.BirthDate }, AnswerOnly, papers: papers);

        Assert.AreEqual(2, plan.HomeIndex);
        CollectionAssert.AreEqual(new[] { ClueCategory.BirthDate }, plan.Tells);
        Assert.AreEqual(TellChannel.Answer, plan.ChannelOf(ClueCategory.BirthDate));
        Assert.AreEqual("3 Jun 1458 BCE", plan.TellValue(ClueCategory.BirthDate));
        Assert.IsTrue(rng.Done, "roll, home, tell, year");

        plan.ApplyTo(papers);
        AssertPapersAreTheCover(papers);
    }

    [Test]
    public void AValueTheHomeSharesWithTheClaim_IsNeverAnAnswerTell()
    {
        FactTable facts = AnswerFacts();
        facts.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Politics, "Sultan Mustafa II");
        facts.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Politics, "sultan mustafa ii");
        var books = new HashSet<ClueCategory>(AnswerBooks) { ClueCategory.Politics };

        ScriptedRandom rng = Script(V(0f));
        LiePlan plan = Lies.Plan(1f, 1, "egypt", "ancient", Cover, new[] { Egypt, Iraq }, Papers(), new[] { ClueCategory.Politics }, AnswerOnly, facts, books, rng);
        Assert.AreEqual(LieOutcome.NoPossibleLie, plan.Outcome);
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void EveryAnswerTell_ProvesAgainstTheClaimAndTheHome_AndNothingElse()
    {
        FactTable facts = AnswerFacts();
        LiePlan plan = PlanSpoken(Script(V(0f), R(0), R(0), R(0)), new[] { ClueCategory.Currency, ClueCategory.Geography }, AnswerOnly, tellCount: 9, facts: facts);
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Geography }, plan.Tells);

        foreach (ClueCategory category in plan.Tells)
        {
            CompareEvidence said = CompareEvidence.ForAnswer(category, plan.TellValue(category), true);
            foreach (FactRow row in facts.Rows(category))
            {
                Discrepancy d = DiscrepancyLog.Prove(said, row.ToEvidence(), "egypt", "ancient");
                string where = $"{category} vs {row.OriginLabel}";
                if (row.NationId == "egypt")
                {
                    Assert.AreEqual(DiscrepancyProof.ClaimMismatch, d?.provedBy, where);
                    Assert.AreEqual(EvidenceKind.Answer, d.source, where);
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

        LiePlan born = PlanSpoken(Script(V(0f), R(0), R(0), R(2)), new[] { ClueCategory.BirthDate }, AnswerOnly);
        Discrepancy record = DiscrepancyLog.Prove(
            CompareEvidence.ForAnswer(ClueCategory.BirthDate, born.TellValue(ClueCategory.BirthDate), true),
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, Cover), "egypt", "ancient");
        Assert.AreEqual(DiscrepancyProof.RecordMismatch, record?.provedBy);
    }
}"""),
])
```

- [ ] **Step 2: Run the compile check — expect failure**

Expected: `== TimeDeskEditMode.csproj: exit 1`, `error CS0246: The type or namespace name 'TellChannel' could not be found`.

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

    /// <summary>The traveller comes from another of today's places; their papers or answers leak tells.</summary>
    Liar,

    /// <summary>The roll said liar, but no other place today could give a tell; the traveller stays honest.</summary>
    NoPossibleLie
}

/// <summary>Where a liar's tell shows; serialized in DayPlanSO, append only.</summary>
public enum TellChannel
{
    /// <summary>Every paper field of the tell's category shows the true home's value.</summary>
    Papers,

    /// <summary>The spoken answer to the category's question gives the true home's value; the papers show the cover.</summary>
    Answer
}

/// <summary>The outcome of one traveller's lie roll: the true home and the tells the liar leaks, on the papers or in speech.</summary>
public sealed class LiePlan
{
    /// <summary>Shared empty tell list.</summary>
    private static readonly ClueCategory[] NoTells = new ClueCategory[0];

    /// <summary>The value of each tell category, printed or spoken (the home's fact, or the tell birth date).</summary>
    private readonly Dictionary<ClueCategory, string> _values;

    /// <summary>The channel each tell category leaks on.</summary>
    private readonly Dictionary<ClueCategory, TellChannel> _channels;

    /// <summary>Creates a plan (Lies.Plan only).</summary>
    internal LiePlan(LieOutcome outcome, int homeIndex, IReadOnlyList<ClueCategory> tells,
                     Dictionary<ClueCategory, string> values, Dictionary<ClueCategory, TellChannel> channels)
    {
        Outcome = outcome;
        HomeIndex = homeIndex;
        Tells = tells;
        _values = values;
        _channels = channels;
    }

    /// <summary>A plan with no home and no tells.</summary>
    internal static LiePlan Without(LieOutcome outcome) =>
        new LiePlan(outcome, -1, NoTells, new Dictionary<ClueCategory, string>(), new Dictionary<ClueCategory, TellChannel>());

    /// <summary>What the roll decided.</summary>
    public LieOutcome Outcome { get; }

    /// <summary>Index of the true home in the (unfiltered) list Lies.Plan was given; -1 unless <see cref="LieOutcome.Liar"/>.</summary>
    public int HomeIndex { get; }

    /// <summary>The tell categories, in pick order; empty unless <see cref="LieOutcome.Liar"/>.</summary>
    public IReadOnlyList<ClueCategory> Tells { get; }

    /// <summary>The channel a tell category leaks on, or null when the category is not a tell.</summary>
    public TellChannel? ChannelOf(ClueCategory category) =>
        _channels.TryGetValue(category, out TellChannel channel) ? channel : (TellChannel?)null;

    /// <summary>A tell's value (the true home's fact, or the tell birth date), or null when the category is not a tell.</summary>
    public string TellValue(ClueCategory category) =>
        _values.TryGetValue(category, out string value) ? value : null;

    /// <summary>
    /// Rewrites every field whose category is a Papers tell with the tell's
    /// value and flags it as an anachronism. An Answer tell leaves the papers
    /// on the cover. Changes nothing for an Honest or NoPossibleLie plan.
    /// </summary>
    public void ApplyTo(IEnumerable<DocumentField> fields)
    {
        if (fields == null)
            return;

        foreach (DocumentField field in fields)
        {
            if (field != null && ChannelOf(field.category) == TellChannel.Papers)
            {
                field.value = _values[field.category];
                field.isAnachronism = true;
            }
        }
    }
}

/// <summary>
/// Who lies about their home, where they really come from, and which tells
/// they leak on their papers or in their answers. Pure and seeded, so every
/// rule and the draw order are tested headless. Draws on the traveller's lie
/// stream (Seeds.ForLies), in this order: the roll; for a liar, the home; one
/// pick per tell (a category/channel option); then the birth year when
/// BirthDate is a tell on either channel.
/// </summary>
public static class Lies
{
    /// <summary>One way a home can leak a category: on the papers or in an answer.</summary>
    private readonly struct TellOption
    {
        /// <summary>The leaked category.</summary>
        public readonly ClueCategory Category;

        /// <summary>Where it leaks.</summary>
        public readonly TellChannel Channel;

        /// <summary>Creates an option.</summary>
        public TellOption(ClueCategory category, TellChannel channel)
        {
            Category = category;
            Channel = channel;
        }
    }

    /// <summary>
    /// Whether a traveller may lie at all: not a legendary, a claim today's
    /// rules allow, and papers to leak tells on. Exempt travellers make no draw.
    /// </summary>
    public static bool MayLie(bool isLegendary, bool claimAllowed, IReadOnlyList<DocumentField> papers) =>
        !isLegendary && claimAllowed && papers != null && papers.Count > 0;

    /// <summary>
    /// Rolls one traveller's lie. With probability <paramref name="liarChance"/>
    /// the traveller lies. A (category, channel) option of another of today's
    /// places is open when <paramref name="channels"/> allows the channel and
    /// Forgery.IsProvableTell holds: Papers for a category the papers print
    /// (first-appearance order), then Answer for one of
    /// <paramref name="answerTellCategories"/> (today's question categories that
    /// may carry an Answer tell, in question order; InterviewDay.AnswerTellCategories).
    /// The true home is picked uniformly among the places with at least one
    /// open option, and max(1, min(<paramref name="tellCount"/>, the distinct
    /// categories among the home's options)) tells are picked one uniform
    /// draw each over the options still open; a pick closes the other option
    /// of its category, so a category leaks on one channel only. A liar for
    /// whom no place qualifies is NoPossibleLie. A null <paramref name="rng"/>
    /// is Honest with no draw; a null list counts as empty.
    /// </summary>
    public static LiePlan Plan(float liarChance, int tellCount,
                               string claimNationId, string claimEraId, string coverBirthDate,
                               IReadOnlyList<HomeCandidate> todays, IReadOnlyList<DocumentField> papers,
                               IReadOnlyList<ClueCategory> answerTellCategories, IReadOnlyList<TellChannel> channels,
                               FactTable facts, ICollection<ClueCategory> bookCategories, IRandomSource rng)
    {
        if (rng == null || !(rng.Value() < liarChance))
            return LiePlan.Without(LieOutcome.Honest);

        bool papersOpen = Allows(channels, TellChannel.Papers);
        bool answersOpen = Allows(channels, TellChannel.Answer);
        List<ClueCategory> printed = papersOpen ? PrintedCategories(papers) : new List<ClueCategory>();
        List<ClueCategory> asked = answersOpen ? Distinct(answerTellCategories) : new List<ClueCategory>();

        var candidates = new List<int>();
        var optionsByCandidate = new List<List<TellOption>>();

        if (todays != null)
        {
            for (int i = 0; i < todays.Count; i++)
            {
                HomeCandidate place = todays[i];
                if (place.NationId == claimNationId && place.EraId == claimEraId)
                    continue;

                var options = new List<TellOption>();
                foreach (ClueCategory category in printed)
                    if (Forgery.IsProvableTell(category, claimNationId, claimEraId, coverBirthDate, place, facts, bookCategories))
                        options.Add(new TellOption(category, TellChannel.Papers));
                foreach (ClueCategory category in asked)
                    if (Forgery.IsProvableTell(category, claimNationId, claimEraId, coverBirthDate, place, facts, bookCategories))
                        options.Add(new TellOption(category, TellChannel.Answer));

                if (options.Count > 0)
                {
                    candidates.Add(i);
                    optionsByCandidate.Add(options);
                }
            }
        }

        if (candidates.Count == 0)
            return LiePlan.Without(LieOutcome.NoPossibleLie);

        int pick = rng.Range(0, candidates.Count);
        int homeIndex = candidates[pick];
        HomeCandidate home = todays[homeIndex];
        List<TellOption> pool = optionsByCandidate[pick];

        int categories = CategoryCount(pool);
        int count = tellCount < 1 ? 1 : tellCount > categories ? categories : tellCount;
        var tells = new List<ClueCategory>(count);
        var channelOf = new Dictionary<ClueCategory, TellChannel>();
        for (int k = 0; k < count; k++)
        {
            TellOption option = pool[rng.Range(0, pool.Count)];
            tells.Add(option.Category);
            channelOf[option.Category] = option.Channel;
            pool.RemoveAll(o => o.Category == option.Category);
        }

        var values = new Dictionary<ClueCategory, string>();
        foreach (ClueCategory category in tells)
            if (category != ClueCategory.BirthDate)
                values[category] = facts.Get(home.NationId, home.EraId, category);

        if (tells.Contains(ClueCategory.BirthDate))
            values[ClueCategory.BirthDate] = BirthDates.PickOtherYear(coverBirthDate, home.BirthYearMin, home.BirthYearMax, rng);

        return new LiePlan(LieOutcome.Liar, homeIndex, tells.AsReadOnly(), values, channelOf);
    }

    /// <summary>True when the channel list holds the channel (a null list holds none).</summary>
    private static bool Allows(IReadOnlyList<TellChannel> channels, TellChannel channel)
    {
        if (channels == null)
            return false;

        foreach (TellChannel c in channels)
            if (c == channel)
                return true;

        return false;
    }

    /// <summary>The distinct categories of a list, in order (empty for null).</summary>
    private static List<ClueCategory> Distinct(IReadOnlyList<ClueCategory> categories)
    {
        var distinct = new List<ClueCategory>();
        if (categories == null)
            return distinct;

        foreach (ClueCategory category in categories)
            if (!distinct.Contains(category))
                distinct.Add(category);

        return distinct;
    }

    /// <summary>How many distinct categories the options cover.</summary>
    private static int CategoryCount(List<TellOption> options)
    {
        var seen = new List<ClueCategory>();
        foreach (TellOption option in options)
            if (!seen.Contains(option.Category))
                seen.Add(option.Category);
        return seen.Count;
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

Then save as `SCRATCH/p3_t06.py` and run (the day plan's knob and the one caller):

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Scripts\DayPlanSO.cs', [
("""/// - Procedural generation knobs (blueprints, eras, legendary chance)
""",
"""/// - Procedural generation knobs (blueprints, eras, legendary chance)
/// - Where today's liars may leak tells (tell count and tell channels)
"""),
("""    [SerializeField, Min(1)] private int tellCount = 1;
""",
"""    [SerializeField, Min(1)] private int tellCount = 1;

    /// <summary>
    /// Where today's liars may leak tells: Papers (their documents) and/or
    /// Answer (their answers to today's questions). The default keeps a day
    /// plan that does not set it on papers-only tells.
    /// </summary>
    [SerializeField] private TellChannel[] tellChannels = { TellChannel.Papers };
"""),
("""    /// <summary>Tells each liar leaks today (at least 1).</summary>
    public int TellCount => tellCount;
""",
"""    /// <summary>Tells each liar leaks today (at least 1).</summary>
    public int TellCount => tellCount;

    /// <summary>Where today's liars may leak tells (empty when unset).</summary>
    public IReadOnlyList<TellChannel> TellChannels => tellChannels ?? Array.Empty<TellChannel>();
"""),
])

# Lies.Plan takes the answer-tell categories and the day's channels. No question
# is asked yet, so no Answer option opens and every day stays papers-only.
apply(W + r'\Assets\Scripts\CaseFactory.cs', [
("""            todays,
            fields,
            _facts,
            _bookCategories,
            _lieRng);""",
"""            todays,
            fields,
            System.Array.Empty<ClueCategory>(),
            plan.TellChannels,
            _facts,
            _bookCategories,
            _lieRng);"""),
])
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 281, failed 0` (+ 9).

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Domain/Lies.cs Assets/Scripts/DayPlanSO.cs Assets/Scripts/CaseFactory.cs Assets/Tests/EditMode/LiesTests.cs && git commit -F - <<'EOF'
feat(domain): tells on two channels, papers or answers (Lies.Plan)

A tell is a (category, channel) option: Papers for a printed category,
Answer for a tell-carrying question's category, open when the day's
channels allow it. One draw per tell; a category leaks on one channel
only; ApplyTo rewrites Papers tells only. With Papers alone the draws are
piece 2's. DayPlanSO gains tellChannels (default Papers); no question
carries a tell until the interview is wired.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 7: Interview content, answers and the dialog runner

The serializable interview content (spec §2.4 table), the answer and wording rules (`Interview`), and the runtime graph and runner (`Dialog.cs`, §2.5). The three files reference each other (`ScriptLine` uses `DialogSpeaker`, `DialogLine.Answer` takes an `InterviewAnswer`, `Interview` fills `InterviewLines`), so they land together.

**Files:**
- Create: `Assets/Scripts/Domain/InterviewContent.cs`, `Assets/Scripts/Domain/Interview.cs`, `Assets/Scripts/Domain/Dialog.cs` (+ `.meta`s)
- Test: `Assets/Tests/EditMode/InterviewTests.cs`, `Assets/Tests/EditMode/DialogRunnerTests.cs` (+ `.meta`s)

- [ ] **Step 1: Write the failing tests** — write `Assets/Tests/EditMode/InterviewTests.cs` (Write tool; then `make_meta.py`):

```csharp
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// What travellers say and how the interview's wording is filled. The lie
/// fixture: the claim Egypt (Currency "Deben", capital "Thebes") and Iraq
/// (Currency "Silver shekel", capital "Babylon"); books for both categories.
/// </summary>
public class InterviewTests
{
    private const string Cover = "3 Jun 1450 BCE";

    private static readonly HomeCandidate Egypt = new HomeCandidate("egypt", "ancient", -1470, -1452);
    private static readonly HomeCandidate Iraq = new HomeCandidate("iraq", "ancient", 0, 0);
    private static readonly HashSet<ClueCategory> Books = new HashSet<ClueCategory> { ClueCategory.Currency, ClueCategory.Geography };

    private static FactTable Facts()
    {
        var t = new FactTable();
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Currency, "Deben");
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Geography, "Thebes");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Currency, "Silver shekel");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Geography, "Babylon");
        return t;
    }

    private static List<DocumentField> Papers() => new List<DocumentField>
    {
        new DocumentField { category = ClueCategory.Currency, label = "Coin of Issue", value = "Deben", page = 0 }
    };

    /// <summary>A lie plan over today = [Egypt, Iraq], asking about the capital.</summary>
    private static LiePlan Plan(float chance, TellChannel channel, params ScriptStep[] script) =>
        Lies.Plan(chance, 1, "egypt", "ancient", Cover, new[] { Egypt, Iraq }, Papers(),
                  new[] { ClueCategory.Geography }, new[] { channel }, Facts(), Books, new ScriptedRandom(script));

    private static InterviewLines Lines() => new InterviewLines
    {
        opener = new LineText("interview.opener", "Next! Step forward, {honorific}."),
        openerLegendary = new LineText("interview.openerLegendary", "Priority arrival: {name}."),
        claim = new LineText("interview.claim", "I request passage home to {place}."),
        honorificMale = "sir",
        honorificFemale = "madam",
        honorificUnknown = "traveller"
    };

    // -----------------------------
    // Answers
    // -----------------------------

    [Test]
    public void Answer_WithoutASpokenTell_IsTheCover()
    {
        LiePlan honest = Plan(0f, TellChannel.Answer, ScriptStep.Value(0.5f));
        LiePlan noLie = Lies.Plan(1f, 1, "egypt", "ancient", Cover, new[] { Egypt }, Papers(), new[] { ClueCategory.Geography },
                                  new[] { TellChannel.Answer }, Facts(), Books, new ScriptedRandom(ScriptStep.Value(0f)));
        Assert.AreEqual(LieOutcome.Honest, honest.Outcome);
        Assert.AreEqual(LieOutcome.NoPossibleLie, noLie.Outcome);

        foreach (LiePlan plan in new[] { null, honest, noLie })
        {
            InterviewAnswer a = Interview.Answer(ClueCategory.Geography, "Thebes", plan);
            Assert.AreEqual(ClueCategory.Geography, a.category);
            Assert.AreEqual("Thebes", a.value);
            Assert.IsFalse(a.isTell);
        }
    }

    [Test]
    public void Answer_AnAnswerTell_GivesTheTrueHomesValue_AndIsATell()
    {
        LiePlan liar = Plan(1f, TellChannel.Answer, ScriptStep.Value(0f), ScriptStep.Range(0), ScriptStep.Range(0));
        Assert.AreEqual(TellChannel.Answer, liar.ChannelOf(ClueCategory.Geography));

        InterviewAnswer a = Interview.Answer(ClueCategory.Geography, "Thebes", liar);
        Assert.AreEqual("Babylon", a.value);
        Assert.IsTrue(a.isTell);

        InterviewAnswer other = Interview.Answer(ClueCategory.Currency, "Deben", liar);
        Assert.AreEqual("Deben", other.value, "a category that is not a tell answers with the cover");
        Assert.IsFalse(other.isTell);
    }

    [Test]
    public void Answer_APapersTell_AnswersWithTheCover()
    {
        LiePlan liar = Plan(1f, TellChannel.Papers, ScriptStep.Value(0f), ScriptStep.Range(0), ScriptStep.Range(0));
        Assert.AreEqual(TellChannel.Papers, liar.ChannelOf(ClueCategory.Currency));

        InterviewAnswer a = Interview.Answer(ClueCategory.Currency, "Deben", liar);
        Assert.AreEqual("Deben", a.value, "a Papers tell leaks on the papers only");
        Assert.IsFalse(a.isTell);
    }

    // -----------------------------
    // Opener and claim
    // -----------------------------

    [TestCase(TravellerGender.Male, "Next! Step forward, sir.")]
    [TestCase(TravellerGender.Female, "Next! Step forward, madam.")]
    [TestCase(TravellerGender.Unknown, "Next! Step forward, traveller.")]
    public void Opener_UsesTheHonorificOfTheRecordedGender(TravellerGender gender, string expected)
    {
        Assert.AreEqual(expected, Interview.Opener(Lines(), gender, null));
    }

    [Test]
    public void Opener_ALegendary_UsesTheLegendaryTemplate()
    {
        Assert.AreEqual("Priority arrival: Nikola Tesla.", Interview.Opener(Lines(), TravellerGender.Unknown, "Nikola Tesla"));
        Assert.AreEqual("Next! Step forward, sir.", Interview.Opener(Lines(), TravellerGender.Male, "  "), "a blank name is no legendary");
    }

    [Test]
    public void Opener_NullLines_GiveEmpty()
    {
        Assert.AreEqual(string.Empty, Interview.Opener(null, TravellerGender.Male, null));
    }

    [Test]
    public void Claim_FillsThePlace_OrGivesTheBareLabelWhenTheTemplateIsBlank()
    {
        Assert.AreEqual("I request passage home to Babylonia (Ancient).", Interview.Claim(Lines(), "Babylonia (Ancient)"));
        Assert.AreEqual("Babylonia (Ancient)", Interview.Claim(new InterviewLines(), "Babylonia (Ancient)"));
        Assert.AreEqual("Babylonia (Ancient)", Interview.Claim(null, "Babylonia (Ancient)"));
    }

    // -----------------------------
    // Small talk
    // -----------------------------

    private static readonly LineText[] PlaceLines = { new LineText("egypt_ancient.smalltalk.1", "The Nile rose."), new LineText("egypt_ancient.smalltalk.2", "The fields are black.") };
    private static readonly LineText[] EraLines = { new LineText("ancient.smalltalk.1", "The harvest was good.") };

    [Test]
    public void PickSmallTalk_ThePlacesLinesWin_OneDraw()
    {
        var rng = new ScriptedRandom(ScriptStep.Range(1));
        Assert.AreEqual("egypt_ancient.smalltalk.2", Interview.PickSmallTalk(PlaceLines, EraLines, rng).id);
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void PickSmallTalk_TheErasLines_WhenThePlaceHasNone()
    {
        var rng = new ScriptedRandom(ScriptStep.Range(0));
        Assert.AreEqual("ancient.smalltalk.1", Interview.PickSmallTalk(new LineText[0], EraLines, rng).id);
        Assert.IsTrue(rng.Done);

        var fromNull = new ScriptedRandom(ScriptStep.Range(0));
        Assert.AreEqual("ancient.smalltalk.1", Interview.PickSmallTalk(null, EraLines, fromNull).id);
        Assert.IsTrue(fromNull.Done);
    }

    [Test]
    public void PickSmallTalk_NoLines_IsNull_WithNoDraw()
    {
        var rng = new ScriptedRandom();
        Assert.IsNull(Interview.PickSmallTalk(new LineText[0], null, rng));
        Assert.AreEqual(0, rng.Draws);
        Assert.IsNull(Interview.PickSmallTalk(PlaceLines, EraLines, null), "no stream, no pick");
    }

    // -----------------------------
    // Token fills
    // -----------------------------

    [Test]
    public void WorstCaseLength_FillsEveryOccurrenceOfTheToken()
    {
        Assert.AreEqual("At home we speak .".Length + 28, Interview.WorstCaseLength("At home we speak {value}.", Interview.ValueToken, 28));
        Assert.AreEqual(", ".Length + 2 * 10, Interview.WorstCaseLength("{value}, {value}", Interview.ValueToken, 10));
        Assert.AreEqual("Here you are.".Length, Interview.WorstCaseLength("Here you are.", Interview.ValueToken, 50));
        Assert.AreEqual(0, Interview.WorstCaseLength(null, Interview.ValueToken, 50));
    }

    [Test]
    public void Fill_ReplacesEveryOccurrence_AndLeavesOtherTokens()
    {
        Assert.AreEqual("Rome and Rome", Interview.Fill("{place} and {place}", Interview.PlaceToken, "Rome"));
        Assert.AreEqual(string.Empty, Interview.Fill(null, Interview.PlaceToken, "Rome"));
        Assert.AreEqual("home to .", Interview.Fill("home to {place}.", Interview.PlaceToken, null));
        Assert.AreEqual("{name} sent Rome", Interview.Fill("{name} sent {place}", Interview.PlaceToken, "Rome"));
        Assert.AreEqual("{value}", Interview.Placeholder(Interview.ValueToken));
    }

    [Test]
    public void HoldsToken_OnlyTheTokensPlaceholderCounts()
    {
        Assert.IsTrue(Interview.HoldsToken("At home we speak {value}.", Interview.ValueToken));
        Assert.IsTrue(Interview.HoldsToken("{place}", Interview.PlaceToken));
        Assert.IsFalse(Interview.HoldsToken("At home we speak value.", Interview.ValueToken), "the bare word is no token");
        Assert.IsFalse(Interview.HoldsToken("I request passage home to {place}.", Interview.ValueToken), "another token");
        Assert.IsFalse(Interview.HoldsToken("{Value}", Interview.ValueToken), "tokens are case-sensitive, as Fill is");
        Assert.IsFalse(Interview.HoldsToken(null, Interview.ValueToken));
    }
}
```

and `Assets/Tests/EditMode/DialogRunnerTests.cs` (Write tool; then `make_meta.py`):

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The dialog runner. Graph under test: node "a" (line "a.enter") offers
/// "go" (to "b"), "stay" (one-shot, no Next), "again" (repeatable, empty
/// Next) and "lost" (Next names no node); node "b" (line "b.enter") offers
/// "back" (to "a"). Each choice speaks one line, "{choice}.said".
/// </summary>
public class DialogRunnerTests
{
    private static DialogLine Line(string id) => new DialogLine(id, DialogSpeaker.Traveller, id);

    private static DialogChoice Choice(string id, string next, bool oneShot = false) => new DialogChoice
    {
        Id = id,
        Label = id,
        Next = next,
        OneShot = oneShot,
        Lines = new List<DialogLine> { Line(id + ".said") }
    };

    private static DialogGraph Graph()
    {
        var graph = new DialogGraph("a");
        graph.Add(new DialogNode
        {
            Id = "a",
            Lines = new List<DialogLine> { Line("a.enter") },
            Choices = new List<DialogChoice> { Choice("go", "b"), Choice("stay", null, oneShot: true), Choice("again", ""), Choice("lost", "nowhere") }
        });
        graph.Add(new DialogNode
        {
            Id = "b",
            Lines = new List<DialogLine> { Line("b.enter") },
            Choices = new List<DialogChoice> { Choice("back", "a") }
        });
        return graph;
    }

    private static DialogRunner Runner() => new DialogRunner(Graph(), new[] { Line("open.1"), Line("open.2") });

    private static string[] Ids(IEnumerable<DialogLine> lines) => lines.Select(l => l.Id).ToArray();

    private static string[] ChoiceIds(DialogRunner r) => r.Choices.Select(c => c.Id).ToArray();

    [Test]
    public void TheOpeningComesFirst_ThenTheStartNodesLines()
    {
        DialogRunner r = Runner();
        CollectionAssert.AreEqual(new[] { "open.1", "open.2", "a.enter" }, Ids(r.Transcript));
        CollectionAssert.AreEqual(new[] { "go", "stay", "again", "lost" }, ChoiceIds(r));
    }

    [Test]
    public void Choose_AppendsTheChoicesLines_ThenEntersNext_OnEveryEntry()
    {
        DialogRunner r = Runner();
        Assert.AreEqual("go", r.Choose("go").Id);
        CollectionAssert.AreEqual(new[] { "open.1", "open.2", "a.enter", "go.said", "b.enter" }, Ids(r.Transcript));
        CollectionAssert.AreEqual(new[] { "back" }, ChoiceIds(r));

        r.Choose("back");
        CollectionAssert.AreEqual(new[] { "open.1", "open.2", "a.enter", "go.said", "b.enter", "back.said", "a.enter" }, Ids(r.Transcript),
                                  "re-entering a node appends its lines again");
        CollectionAssert.AreEqual(new[] { "go", "stay", "again", "lost" }, ChoiceIds(r));
    }

    [Test]
    public void ANullEmptyOrUnknownNext_StaysOnTheNode()
    {
        DialogRunner r = Runner();
        r.Choose("stay");
        r.Choose("again");
        r.Choose("lost");
        CollectionAssert.AreEqual(new[] { "open.1", "open.2", "a.enter", "stay.said", "again.said", "lost.said" }, Ids(r.Transcript));
        CollectionAssert.AreEqual(new[] { "go", "again", "lost" }, ChoiceIds(r), "still on node a");
    }

    [Test]
    public void AOneShotChoice_LeavesTheMenu_ARepeatableOneStays()
    {
        DialogRunner r = Runner();
        r.Choose("stay");
        r.Choose("again");
        CollectionAssert.DoesNotContain(ChoiceIds(r), "stay");
        CollectionAssert.Contains(ChoiceIds(r), "again");

        Assert.IsNotNull(r.Choose("again"), "a repeatable choice can be picked again");
        CollectionAssert.Contains(ChoiceIds(r), "again");
    }

    [Test]
    public void AnIdNotOffered_ReturnsNull_AndChangesNothing()
    {
        DialogRunner r = Runner();
        r.Choose("stay");
        string[] before = Ids(r.Transcript);

        Assert.IsNull(r.Choose("back"), "a choice of another node");
        Assert.IsNull(r.Choose("stay"), "a used one-shot choice");
        Assert.IsNull(r.Choose("missing"));
        Assert.IsNull(r.Choose(null));

        CollectionAssert.AreEqual(before, Ids(r.Transcript));
        CollectionAssert.AreEqual(new[] { "go", "again", "lost" }, ChoiceIds(r));
    }

    [Test]
    public void Choices_IsAFreshListEachTime()
    {
        DialogRunner r = Runner();
        Assert.AreNotSame(r.Choices, r.Choices);
        ((List<DialogChoice>)r.Choices).Clear();
        Assert.AreEqual(4, r.Choices.Count);
    }

    [Test]
    public void TheGraph_RefusesADuplicateNodeId_AndFindsNodesById()
    {
        DialogGraph graph = Graph();
        Assert.Throws<ArgumentException>(() => graph.Add(new DialogNode { Id = "a" }));
        Assert.AreEqual("a", graph.StartNodeId);
        Assert.AreEqual("b", graph.Node("b").Id);
        Assert.IsNull(graph.Node("nowhere"));
        Assert.IsNull(graph.Node(null));
    }

    [Test]
    public void AnswerLines_CarryTheirFact_SpokenLinesDoNot()
    {
        var a = new InterviewAnswer { category = ClueCategory.Geography, value = "Babylon", isTell = true };
        DialogLine answer = DialogLine.Answer("q_capital.answer", "Our capital is Babylon.", a);
        Assert.AreEqual(DialogSpeaker.Traveller, answer.Speaker);
        Assert.IsTrue(answer.IsAnswer);
        Assert.AreEqual(ClueCategory.Geography, answer.Category);
        Assert.AreEqual("Babylon", answer.Value);
        Assert.IsTrue(answer.IsTell);

        var spoken = new DialogLine("case.intro", DialogSpeaker.Desk, "Next!");
        Assert.IsFalse(spoken.IsAnswer);
        Assert.IsFalse(spoken.IsTell);
        Assert.IsNull(spoken.Value);
    }
}
```

- [ ] **Step 2: Run the compile check — expect failure**

Expected: `== TimeDeskEditMode.csproj: exit 1` with `error CS0246` for `LineText`, `InterviewLines`, `DialogLine`, `DialogChoice`, `DialogGraph` and `DialogRunner`.

- [ ] **Step 3: Implement** — write `Assets/Scripts/Domain/InterviewContent.cs` (Write tool; then `make_meta.py`):

```csharp
using System;
using System.Collections.Generic;

// Serializable interview content. The generated ScriptableObjects
// (QuestionSO, DialogSO, ContentLibrarySO.interview) hold these directly, so
// the rules read them with no projection. Ids follow the generator's grammar
// (Tools > TimeDesk > Generate World) and key piece 6's string tables.

/// <summary>A line with a stable id.</summary>
[Serializable]
public sealed class LineText
{
    /// <summary>Stable id ("q_capital.answer", "interview.opener", "egypt_ancient.smalltalk.1", ...).</summary>
    public string id;

    /// <summary>The wording, possibly with {tokens}.</summary>
    public string text;

    /// <summary>An empty line (for serialization).</summary>
    public LineText()
    {
    }

    /// <summary>A line with its id and wording.</summary>
    public LineText(string id, string text)
    {
        this.id = id;
        this.text = text;
    }
}

/// <summary>One authored spoken line of a narrative dialog.</summary>
[Serializable]
public sealed class ScriptLine
{
    /// <summary>Stable id; starts with the dialog's id and a dot.</summary>
    public string id;

    /// <summary>Who says it.</summary>
    public DialogSpeaker speaker;

    /// <summary>The wording.</summary>
    public string text;
}

/// <summary>One authored reply the player can pick in a narrative dialog.</summary>
[Serializable]
public sealed class ScriptChoice
{
    /// <summary>Id within the dialog (the choice's line id is "{dialogId}.{id}").</summary>
    public string id;

    /// <summary>The menu entry, which the desk also speaks into the transcript.</summary>
    public string label;

    /// <summary>Lines spoken after the desk's label.</summary>
    public List<ScriptLine> lines = new();

    /// <summary>The node to go to; empty ends the dialog and returns to the hub.</summary>
    public string next;

    /// <summary>EffectSO asset name applied at the end of the shift (ending choices only); empty = none.</summary>
    public string effect;
}

/// <summary>One node of a narrative dialog.</summary>
[Serializable]
public sealed class ScriptNode
{
    /// <summary>Id within the dialog.</summary>
    public string id;

    /// <summary>Lines spoken on entering the node.</summary>
    public List<ScriptLine> lines = new();

    /// <summary>The replies offered at this node.</summary>
    public List<ScriptChoice> choices = new();
}

/// <summary>
/// An authored branching conversation, the data contract a runner consumes
/// (a future Yarn or Ink importer would produce this, too).
/// </summary>
[Serializable]
public sealed class AuthoredDialog
{
    /// <summary>Stable id (run memory: FlagKeys.DialogDone).</summary>
    public string id;

    /// <summary>The hub entry ("Any news from home? >").</summary>
    public string label;

    /// <summary>True when the dialog is offered at most once per run (the generator writes !repeatable).</summary>
    public bool oneShot = true;

    /// <summary>The nodes; nodes[0] is the start.</summary>
    public List<ScriptNode> nodes = new();
}

/// <summary>Per-era wording of a question: chosen by the traveller's claimed era.</summary>
[Serializable]
public sealed class WordingOverride
{
    /// <summary>The claimed era this wording is for (EraSO.id).</summary>
    public string eraId;

    /// <summary>The desk's question.</summary>
    public LineText prompt = new();

    /// <summary>The traveller's answer template ({value}).</summary>
    public LineText answer = new();
}

/// <summary>One interview question: a fact category, its menu label and wording.</summary>
[Serializable]
public sealed class InterviewQuestion
{
    /// <summary>Stable id ("q_capital").</summary>
    public string id;

    /// <summary>The fact the answer gives (always provable: a book or the Citizen Record).</summary>
    public ClueCategory category;

    /// <summary>The ask-menu entry ("Capital").</summary>
    public string label;

    /// <summary>The desk's question.</summary>
    public LineText prompt = new();

    /// <summary>The traveller's answer template; {value} is the canonical fact value.</summary>
    public LineText answer = new();

    /// <summary>Wording per claimed era (only the sentence changes, never the value).</summary>
    public List<WordingOverride> overrides = new();

    /// <summary>The question as asked of a traveller claiming <paramref name="eraId"/>: that era's override, else the default.</summary>
    public LineText PromptFor(string eraId)
    {
        WordingOverride o = OverrideFor(eraId);
        return o != null ? o.prompt : prompt;
    }

    /// <summary>The answer template for a traveller claiming <paramref name="eraId"/>: that era's override, else the default.</summary>
    public LineText AnswerFor(string eraId)
    {
        WordingOverride o = OverrideFor(eraId);
        return o != null ? o.answer : answer;
    }

    /// <summary>The override for an era, or null.</summary>
    private WordingOverride OverrideFor(string eraId)
    {
        if (string.IsNullOrEmpty(eraId) || overrides == null)
            return null;

        foreach (WordingOverride o in overrides)
            if (o != null && o.eraId == eraId)
                return o;

        return null;
    }
}

/// <summary>
/// The interview's fixed wording, plus the two layout limits content is
/// checked against: the most choices the intercom shows at once and the
/// longest line a transcript row holds.
/// </summary>
[Serializable]
public sealed class InterviewLines
{
    /// <summary>The desk's speaker label in the transcript ("DESK").</summary>
    public string deskName;

    /// <summary>The desk's opener ({honorific}).</summary>
    public LineText opener = new();

    /// <summary>The desk's opener for a legendary ({name}).</summary>
    public LineText openerLegendary = new();

    /// <summary>The traveller's claim ({place}); also the banner and the shift summary.</summary>
    public LineText claim = new();

    /// <summary>Honorific for a traveller recorded as male ("sir").</summary>
    public string honorificMale;

    /// <summary>Honorific for a traveller recorded as female ("madam").</summary>
    public string honorificFemale;

    /// <summary>Honorific when the gender is unknown ("traveller").</summary>
    public string honorificUnknown;

    /// <summary>Hub entry per document ({document}).</summary>
    public string requestLabel;

    /// <summary>The desk's request ({document}).</summary>
    public LineText requestPrompt = new();

    /// <summary>The traveller's reply as they hand the document over.</summary>
    public LineText requestReply = new();

    /// <summary>Hub entry that opens the questions sub-menu.</summary>
    public string askLabel;

    /// <summary>The ask menu's way back to the hub (always its first entry).</summary>
    public string backLabel;

    /// <summary>Ask-menu entry for small talk.</summary>
    public string smallTalkLabel;

    /// <summary>The desk's small-talk question.</summary>
    public LineText smallTalkPrompt = new();

    /// <summary>The most choices the intercom shows at once (content never offers more).</summary>
    public int menuCapacity;

    /// <summary>The longest line a transcript row holds, worst case after filling every token.</summary>
    public int maxLineChars;
}
```

`Assets/Scripts/Domain/Interview.cs` (Write tool; then `make_meta.py`):

```csharp
using System.Collections.Generic;

/// <summary>One spoken answer, computed at case generation from the same values as the papers.</summary>
public sealed class InterviewAnswer
{
    /// <summary>The fact the answer gives.</summary>
    public ClueCategory category;

    /// <summary>The canonical value: a FactTable value, a placeholder, or a date.</summary>
    public string value;

    /// <summary>True only for an Answer-channel tell (the true home's value while the papers show the cover).</summary>
    public bool isTell;
}

/// <summary>
/// The interview's wording and answer rules, pure so they are tested
/// headless: what a traveller answers, the desk's opener, the claim sentence,
/// the small-talk pick, and the token fills and checks (with the worst-case
/// length the generator checks lines against).
/// </summary>
public static class Interview
{
    /// <summary>The canonical fact value in an answer template.</summary>
    public const string ValueToken = "value";

    /// <summary>The traveller's honorific in the opener.</summary>
    public const string HonorificToken = "honorific";

    /// <summary>A legendary's name in the legendary opener.</summary>
    public const string NameToken = "name";

    /// <summary>A document's name in a request.</summary>
    public const string DocumentToken = "document";

    /// <summary>The claimed place's label in the claim.</summary>
    public const string PlaceToken = "place";

    /// <summary>
    /// What a traveller answers about <paramref name="category"/>: the tell's
    /// value when <paramref name="lie"/> leaks this category on the Answer
    /// channel (a tell); otherwise the cover value, the same one the papers
    /// print (honest travellers, NoPossibleLie, other categories, Papers tells).
    /// </summary>
    public static InterviewAnswer Answer(ClueCategory category, string coverValue, LiePlan lie)
    {
        if (lie != null && lie.ChannelOf(category) == TellChannel.Answer)
            return new InterviewAnswer { category = category, value = lie.TellValue(category), isTell = true };

        return new InterviewAnswer { category = category, value = coverValue, isTell = false };
    }

    /// <summary>
    /// The desk's opener: the legendary template with the name for a
    /// legendary (a non-blank <paramref name="legendaryName"/>), otherwise the
    /// opener with the honorific of <paramref name="gender"/>. "" for null lines.
    /// </summary>
    public static string Opener(InterviewLines lines, TravellerGender gender, string legendaryName)
    {
        if (lines == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(legendaryName))
            return Fill(lines.openerLegendary != null ? lines.openerLegendary.text : null, NameToken, legendaryName);

        return Fill(lines.opener != null ? lines.opener.text : null, HonorificToken, Honorific(gender, lines));
    }

    /// <summary>The traveller's claim sentence for a place label; the bare label when the template is blank (the banner never goes empty).</summary>
    public static string Claim(InterviewLines lines, string placeLabel)
    {
        string template = lines != null && lines.claim != null ? lines.claim.text : null;
        return string.IsNullOrWhiteSpace(template) ? placeLabel ?? string.Empty : Fill(template, PlaceToken, placeLabel);
    }

    /// <summary>
    /// A traveller's small talk: from their claimed place's lines, or its
    /// era's when the place has none. One Range draw when there is a line;
    /// null, with no draw, when there is none or <paramref name="rng"/> is null.
    /// </summary>
    public static LineText PickSmallTalk(IReadOnlyList<LineText> placeLines, IReadOnlyList<LineText> eraLines, IRandomSource rng)
    {
        IReadOnlyList<LineText> pool = placeLines != null && placeLines.Count > 0 ? placeLines : eraLines;
        if (rng == null || pool == null || pool.Count == 0)
            return null;

        return pool[rng.Range(0, pool.Count)];
    }

    /// <summary>The template's length with every {token} filled by a value <paramref name="longestValue"/> characters long (0 for null).</summary>
    public static int WorstCaseLength(string template, string token, int longestValue)
    {
        if (template == null)
            return 0;

        string placeholder = Placeholder(token);
        int occurrences = 0;
        for (int i = template.IndexOf(placeholder, System.StringComparison.Ordinal); i >= 0;
             i = template.IndexOf(placeholder, i + placeholder.Length, System.StringComparison.Ordinal))
            occurrences++;

        return template.Length + occurrences * (longestValue - placeholder.Length);
    }

    /// <summary>A token as templates write it ("{value}").</summary>
    public static string Placeholder(string token) => "{" + token + "}";

    /// <summary>True when the template holds the {token} placeholder (false for null); Generate World and the content validator both check templates with it.</summary>
    public static bool HoldsToken(string template, string token) =>
        template != null && template.IndexOf(Placeholder(token), System.StringComparison.Ordinal) >= 0;

    /// <summary>Replaces every {token} in the template; null template gives "", a null value inserts "", other tokens stay.</summary>
    public static string Fill(string template, string token, string value)
    {
        if (template == null)
            return string.Empty;

        return template.Replace(Placeholder(token), value ?? string.Empty);
    }

    /// <summary>The honorific for a gender.</summary>
    private static string Honorific(TravellerGender gender, InterviewLines lines)
    {
        switch (gender)
        {
            case TravellerGender.Male: return lines.honorificMale;
            case TravellerGender.Female: return lines.honorificFemale;
            default: return lines.honorificUnknown;
        }
    }
}
```

and `Assets/Scripts/Domain/Dialog.cs` (Write tool; then `make_meta.py`):

```csharp
using System;
using System.Collections.Generic;

/// <summary>Who speaks a line in the interview transcript.</summary>
public enum DialogSpeaker
{
    /// <summary>The player's desk.</summary>
    Desk,

    /// <summary>The traveller.</summary>
    Traveller
}

/// <summary>What choosing a dialog choice does beyond appending lines and moving on.</summary>
public enum DialogAction
{
    /// <summary>Nothing else.</summary>
    None,

    /// <summary>The traveller hands over a document: its window opens (DialogChoice.DocumentIndex).</summary>
    OpenDocument,

    /// <summary>A narrative dialog ends: it is recorded for the end of the shift (DialogChoice.DialogId, EffectName).</summary>
    CompleteDialog
}

/// <summary>One transcript line. Immutable; an answer line also carries the fact it states.</summary>
public sealed class DialogLine
{
    /// <summary>A spoken line.</summary>
    public DialogLine(string id, DialogSpeaker speaker, string text)
        : this(id, speaker, text, false, default, null, false)
    {
    }

    private DialogLine(string id, DialogSpeaker speaker, string text, bool isAnswer, ClueCategory category, string value, bool isTell)
    {
        Id = id;
        Speaker = speaker;
        Text = text;
        IsAnswer = isAnswer;
        Category = category;
        Value = value;
        IsTell = isTell;
    }

    /// <summary>A traveller's answer line: its sentence, plus the answer's category, canonical value and tell flag.</summary>
    public static DialogLine Answer(string id, string text, InterviewAnswer a) =>
        new DialogLine(id, DialogSpeaker.Traveller, text, true, a != null ? a.category : default, a != null ? a.value : null, a != null && a.isTell);

    /// <summary>Stable id (the transcript names its row Line_{Id}).</summary>
    public string Id { get; }

    /// <summary>Who says it.</summary>
    public DialogSpeaker Speaker { get; }

    /// <summary>The sentence.</summary>
    public string Text { get; }

    /// <summary>True for an answer to a question (the only compare-clickable rows).</summary>
    public bool IsAnswer { get; }

    /// <summary>The answered category (answers only).</summary>
    public ClueCategory Category { get; }

    /// <summary>The canonical value the sentence contains verbatim (answers only).</summary>
    public string Value { get; }

    /// <summary>True when the answer is a liar's Answer tell.</summary>
    public bool IsTell { get; }
}

/// <summary>One choice the player can pick at a node.</summary>
public sealed class DialogChoice
{
    /// <summary>Id, unique within the graph.</summary>
    public string Id;

    /// <summary>The intercom button label.</summary>
    public string Label;

    /// <summary>Lines appended to the transcript when chosen.</summary>
    public List<DialogLine> Lines = new List<DialogLine>();

    /// <summary>Node to move to; null or empty stays on the current node.</summary>
    public string Next;

    /// <summary>True when the choice leaves the menu once picked (per traveller).</summary>
    public bool OneShot;

    /// <summary>What else choosing it does.</summary>
    public DialogAction Action;

    /// <summary>OpenDocument: index of the document in paper order.</summary>
    public int DocumentIndex = -1;

    /// <summary>CompleteDialog: the finished dialog's id.</summary>
    public string DialogId;

    /// <summary>CompleteDialog: the EffectSO asset name to apply at the end of the shift (empty = none).</summary>
    public string EffectName;
}

/// <summary>One node of the runtime graph.</summary>
public sealed class DialogNode
{
    /// <summary>Id, unique within the graph.</summary>
    public string Id;

    /// <summary>Lines appended every time the node is entered.</summary>
    public List<DialogLine> Lines = new List<DialogLine>();

    /// <summary>The choices offered here.</summary>
    public List<DialogChoice> Choices = new List<DialogChoice>();
}

/// <summary>A traveller's interview as a graph of nodes, built per traveller (InterviewScript.Build).</summary>
public sealed class DialogGraph
{
    /// <summary>Nodes by id.</summary>
    private readonly Dictionary<string, DialogNode> _nodes = new Dictionary<string, DialogNode>();

    /// <summary>Creates a graph that starts at <paramref name="startNodeId"/>.</summary>
    public DialogGraph(string startNodeId)
    {
        StartNodeId = startNodeId;
    }

    /// <summary>Where a runner starts.</summary>
    public string StartNodeId { get; }

    /// <summary>Adds a node.</summary>
    /// <exception cref="ArgumentException">A node with the same id exists.</exception>
    public void Add(DialogNode node)
    {
        if (node == null || node.Id == null)
            throw new ArgumentException("A dialog node needs an id.");
        if (_nodes.ContainsKey(node.Id))
            throw new ArgumentException($"Dialog node '{node.Id}' is added twice.");
        _nodes.Add(node.Id, node);
    }

    /// <summary>The node with this id, or null.</summary>
    public DialogNode Node(string id) =>
        id != null && _nodes.TryGetValue(id, out DialogNode node) ? node : null;
}

/// <summary>
/// Plays a dialog graph: an append-only transcript and the choices of the
/// current node. Pure, so every step is tested headless.
/// </summary>
public sealed class DialogRunner
{
    private readonly DialogGraph _graph;
    private readonly List<DialogLine> _transcript = new List<DialogLine>();
    private readonly HashSet<string> _used = new HashSet<string>();
    private DialogNode _current;

    /// <summary>Starts with the opening lines, then enters the start node (appending its lines).</summary>
    public DialogRunner(DialogGraph graph, IEnumerable<DialogLine> opening)
    {
        _graph = graph;
        if (opening != null)
            foreach (DialogLine line in opening)
                if (line != null)
                    _transcript.Add(line);

        Enter(graph != null ? graph.Node(graph.StartNodeId) : null);
    }

    /// <summary>Every line so far, in order (append-only; a live view).</summary>
    public IReadOnlyList<DialogLine> Transcript => _transcript;

    /// <summary>The current node's choices minus the used one-shot ones, as a fresh list.</summary>
    public IReadOnlyList<DialogChoice> Choices
    {
        get
        {
            var choices = new List<DialogChoice>();
            if (_current != null)
                foreach (DialogChoice c in _current.Choices)
                    if (c != null && !(c.OneShot && _used.Contains(c.Id)))
                        choices.Add(c);
            return choices;
        }
    }

    /// <summary>
    /// Picks a currently offered choice: appends its lines, marks it used if
    /// one-shot, and moves to its Next node (a null, empty or unknown Next
    /// stays). Returns the choice, or null (changing nothing) when the id is
    /// not among <see cref="Choices"/>.
    /// </summary>
    public DialogChoice Choose(string choiceId)
    {
        DialogChoice choice = null;
        foreach (DialogChoice c in Choices)
        {
            if (c.Id == choiceId)
            {
                choice = c;
                break;
            }
        }

        if (choice == null)
            return null;

        foreach (DialogLine line in choice.Lines)
            if (line != null)
                _transcript.Add(line);

        if (choice.OneShot)
            _used.Add(choice.Id);

        DialogNode next = string.IsNullOrEmpty(choice.Next) ? null : _graph.Node(choice.Next);
        if (next != null)
            Enter(next);

        return choice;
    }

    /// <summary>Makes a node current and appends its lines.</summary>
    private void Enter(DialogNode node)
    {
        if (node == null)
            return;

        _current = node;
        foreach (DialogLine line in node.Lines)
            if (line != null)
                _transcript.Add(line);
    }
}
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 304, failed 0` (+ 15 `InterviewTests` + 8 `DialogRunnerTests`).

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Domain/InterviewContent.cs Assets/Scripts/Domain/InterviewContent.cs.meta Assets/Scripts/Domain/Interview.cs Assets/Scripts/Domain/Interview.cs.meta Assets/Scripts/Domain/Dialog.cs Assets/Scripts/Domain/Dialog.cs.meta Assets/Tests/EditMode/InterviewTests.cs Assets/Tests/EditMode/InterviewTests.cs.meta Assets/Tests/EditMode/DialogRunnerTests.cs Assets/Tests/EditMode/DialogRunnerTests.cs.meta && git commit -F - <<'EOF'
feat(domain): interview content, answer rules and the dialog runner

Serializable interview content (lines with stable ids, questions with
per-era wording, authored dialogs, the interview's wording and layout
limits); what a traveller answers (the cover, or an Answer tell's value),
the opener with the gender's honorific, the claim sentence, the small-talk
pick and token fills; and a pure dialog graph and runner.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 8: The interview graph and its structure rules (`InterviewScript`, `DialogChecks`)

Builds each traveller's graph: the hub (requests in paper order, "Ask about home >", one entry per offered dialog), the ask menu ("< Back" **first**, then the answered questions and small talk) and the authored dialogs' namespaced nodes (spec §2.5, Q4, Q11, R25). `DialogChecks` holds the structure rules (including "cannot reach an ending" and "effect on a non-ending choice") and the menu capacity rules shared by the generator, the validator and the day.

**Files:**
- Create: `Assets/Scripts/Domain/InterviewScript.cs` (+ `.meta`)
- Test: `Assets/Tests/EditMode/InterviewScriptTests.cs` (+ `.meta`)

- [ ] **Step 1: Write the failing test** (`Assets/Tests/EditMode/InterviewScriptTests.cs`, Write tool; then `make_meta.py`)

```csharp
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The interview graph (hub, ask menu, merged dialogs) and the structure and
/// capacity rules. Traveller under test: claims an Ancient place, carries a
/// Travel Passport and a Transit Permit, answers Currency honestly ("Deben")
/// and Geography with an Answer tell ("Babylon"), has no Politics answer, and
/// has a small-talk line. The rumour dialog is shaped like dlg_rumour.
/// </summary>
public class InterviewScriptTests
{
    private static InterviewLines Lines() => new InterviewLines
    {
        deskName = "DESK",
        requestLabel = "Request {document}",
        requestPrompt = new LineText("interview.requestPrompt", "Your {document}, please."),
        requestReply = new LineText("interview.requestReply", "Here you are."),
        askLabel = "Ask about home >",
        backLabel = "< Back",
        smallTalkLabel = "Small talk",
        smallTalkPrompt = new LineText("interview.smallTalkPrompt", "How is life back home?")
    };

    private static InterviewQuestion Question(string id, ClueCategory category, string label, string answer) => new InterviewQuestion
    {
        id = id,
        category = category,
        label = label,
        prompt = new LineText(id + ".prompt", "About " + label + "?"),
        answer = new LineText(id + ".answer", answer)
    };

    private static List<InterviewQuestion> Questions()
    {
        InterviewQuestion currency = Question("q_currency", ClueCategory.Currency, "Currency", "We pay in {value}.");
        currency.overrides.Add(new WordingOverride
        {
            eraId = "ancient",
            prompt = new LineText("q_currency.ancient.prompt", "What do you trade with at home?"),
            answer = new LineText("q_currency.ancient.answer", "We trade with {value}.")
        });

        return new List<InterviewQuestion>
        {
            currency,
            Question("q_capital", ClueCategory.Geography, "Capital", "Our capital is {value}."),
            Question("q_ruler", ClueCategory.Politics, "Ruler", "We are ruled by {value}.")
        };
    }

    private static InterviewCase Case(bool smallTalk = true, string intro = "Next! Step forward, sir.") => new InterviewCase
    {
        introLine = intro,
        claimLine = "I request passage home to New Kingdom Egypt (Ancient).",
        claimedEraId = "ancient",
        documentNames = new[] { "Travel Passport", "Transit Permit" },
        answers = new[]
        {
            new InterviewAnswer { category = ClueCategory.Currency, value = "Deben", isTell = false },
            new InterviewAnswer { category = ClueCategory.Geography, value = "Babylon", isTell = true }
        },
        smallTalk = smallTalk ? new LineText("egypt_ancient.smalltalk.1", "The Nile rose right on time.") : null
    };

    private static ScriptLine Said(string id, string text) => new ScriptLine { id = id, speaker = DialogSpeaker.Traveller, text = text };

    private static ScriptChoice Reply(string id, string label, string next = "", string effect = "") =>
        new ScriptChoice { id = id, label = label, next = next, effect = effect };

    /// <summary>start: "more" (to detail) or "ignore" (ends); detail: "noted" (ends, with the rumour effect).</summary>
    private static AuthoredDialog Rumour() => new AuthoredDialog
    {
        id = "dlg_rumour",
        label = "Any news from home? >",
        oneShot = true,
        nodes =
        {
            new ScriptNode
            {
                id = "start",
                lines = { Said("dlg_rumour.start.1", "News? Only a rumour.") },
                choices = { Reply("more", "Tell me more.", "detail"), Reply("ignore", "Not my business.") }
            },
            new ScriptNode
            {
                id = "detail",
                lines = { Said("dlg_rumour.detail.1", "They say a courier carries them.") },
                choices = { Reply("noted", "Noted. Thank you.", effect: "Effect_Dialog_RumourHeard") }
            }
        }
    };

    private static DialogGraph Build(InterviewCase c = null, IReadOnlyList<InterviewQuestion> questions = null, IReadOnlyList<AuthoredDialog> dialogs = null) =>
        InterviewScript.Build(Lines(), questions ?? Questions(), dialogs ?? new[] { Rumour() }, c ?? Case());

    private static string[] Ids(IEnumerable<DialogChoice> choices) => choices.Select(c => c.Id).ToArray();

    private static string[] LineIds(IEnumerable<DialogLine> lines) => lines.Select(l => l.Id).ToArray();

    // -----------------------------
    // Hub and ask menu
    // -----------------------------

    [Test]
    public void Hub_RequestsInPaperOrder_ThenAsk_ThenOneEntryPerDialog()
    {
        DialogNode hub = Build().Node(InterviewScript.HubNodeId);
        CollectionAssert.AreEqual(new[] { "request:0", "request:1", "ask", "dlg:dlg_rumour" }, Ids(hub.Choices));
        CollectionAssert.AreEqual(new[] { "Request Travel Passport", "Request Transit Permit", "Ask about home >", "Any news from home? >" },
                                  hub.Choices.Select(c => c.Label).ToArray());

        DialogChoice ask = hub.Choices[2];
        Assert.AreEqual(InterviewScript.AskNodeId, ask.Next);
        CollectionAssert.IsEmpty(ask.Lines);

        DialogChoice dialog = hub.Choices[3];
        Assert.AreEqual("dlg_rumour/start", dialog.Next);
        Assert.IsTrue(dialog.OneShot);
    }

    [Test]
    public void Request_SpeaksPromptAndReply_OpensItsDocument_AndIsRepeatable()
    {
        DialogChoice permit = Build().Node(InterviewScript.HubNodeId).Choices[1];
        Assert.AreEqual(DialogAction.OpenDocument, permit.Action);
        Assert.AreEqual(1, permit.DocumentIndex);
        Assert.IsFalse(permit.OneShot);
        Assert.IsTrue(string.IsNullOrEmpty(permit.Next), "stays on the hub");
        CollectionAssert.AreEqual(new[] { "interview.requestPrompt", "interview.requestReply" }, LineIds(permit.Lines));
        Assert.AreEqual("Your Transit Permit, please.", permit.Lines[0].Text);
        Assert.AreEqual(DialogSpeaker.Desk, permit.Lines[0].Speaker);
        Assert.AreEqual("Here you are.", permit.Lines[1].Text);
        Assert.AreEqual(DialogSpeaker.Traveller, permit.Lines[1].Speaker);
    }

    [Test]
    public void Hub_HasNoAskEntry_WithoutQuestionsOrSmallTalk()
    {
        DialogNode hub = Build(Case(smallTalk: false), new InterviewQuestion[0], new AuthoredDialog[0]).Node(InterviewScript.HubNodeId);
        CollectionAssert.AreEqual(new[] { "request:0", "request:1" }, Ids(hub.Choices));
    }

    [Test]
    public void Ask_BackFirst_ThenAnsweredQuestionsInOrder_ThenSmallTalk()
    {
        DialogNode ask = Build().Node(InterviewScript.AskNodeId);
        CollectionAssert.AreEqual(new[] { "back", "q:q_currency", "q:q_capital", "smalltalk" }, Ids(ask.Choices), "q_ruler has no answer, so it is skipped");
        CollectionAssert.AreEqual(new[] { "< Back", "Currency", "Capital", "Small talk" }, ask.Choices.Select(c => c.Label).ToArray());

        Assert.AreEqual(InterviewScript.HubNodeId, ask.Choices[0].Next);
        Assert.IsFalse(ask.Choices[0].OneShot);
        Assert.IsTrue(ask.Choices.Skip(1).All(c => c.OneShot && string.IsNullOrEmpty(c.Next)));

        DialogChoice smallTalk = ask.Choices[3];
        CollectionAssert.AreEqual(new[] { "interview.smallTalkPrompt", "egypt_ancient.smalltalk.1" }, LineIds(smallTalk.Lines));
        Assert.AreEqual(DialogSpeaker.Traveller, smallTalk.Lines[1].Speaker);
        Assert.IsFalse(smallTalk.Lines[1].IsAnswer, "small talk is never evidence");
    }

    [Test]
    public void Ask_OffersNoSmallTalk_WithoutALine()
    {
        DialogNode ask = Build(Case(smallTalk: false)).Node(InterviewScript.AskNodeId);
        CollectionAssert.AreEqual(new[] { "back", "q:q_currency", "q:q_capital" }, Ids(ask.Choices));
    }

    [Test]
    public void AnswerLines_CarryTheFact_AndTheClaimedErasWordingWins()
    {
        DialogNode ask = Build().Node(InterviewScript.AskNodeId);

        DialogChoice currency = ask.Choices[1];
        CollectionAssert.AreEqual(new[] { "q_currency.ancient.prompt", "q_currency.ancient.answer" }, LineIds(currency.Lines));
        Assert.AreEqual("What do you trade with at home?", currency.Lines[0].Text);
        Assert.AreEqual(DialogSpeaker.Desk, currency.Lines[0].Speaker);
        Assert.AreEqual("We trade with Deben.", currency.Lines[1].Text);
        Assert.IsTrue(currency.Lines[1].IsAnswer);
        Assert.AreEqual(ClueCategory.Currency, currency.Lines[1].Category);
        Assert.AreEqual("Deben", currency.Lines[1].Value);
        Assert.IsFalse(currency.Lines[1].IsTell);

        DialogLine capital = ask.Choices[2].Lines[1];
        Assert.AreEqual("q_capital.answer", capital.Id);
        Assert.AreEqual("Our capital is Babylon.", capital.Text);
        Assert.AreEqual("Babylon", capital.Value);
        Assert.IsTrue(capital.IsTell);

        InterviewCase medieval = Case();
        medieval.claimedEraId = "medieval";
        Assert.AreEqual("We pay in Deben.", Build(medieval).Node(InterviewScript.AskNodeId).Choices[1].Lines[1].Text, "no override for this era");
    }

    [Test]
    public void Opening_IsTheIntroThenTheClaim_OrTheClaimAloneWhenTheIntroIsBlank()
    {
        IReadOnlyList<DialogLine> both = InterviewScript.Opening(Case());
        CollectionAssert.AreEqual(new[] { "case.intro", "case.claim" }, LineIds(both));
        Assert.AreEqual(DialogSpeaker.Desk, both[0].Speaker);
        Assert.AreEqual(DialogSpeaker.Traveller, both[1].Speaker);
        Assert.AreEqual("I request passage home to New Kingdom Egypt (Ancient).", both[1].Text);

        CollectionAssert.AreEqual(new[] { "case.claim" }, LineIds(InterviewScript.Opening(Case(intro: " "))));
    }

    // -----------------------------
    // Authored dialogs
    // -----------------------------

    [Test]
    public void AnAuthoredDialog_IsMerged_WithNamespacedIds_AndTheDeskSpeakingLabels()
    {
        DialogGraph graph = Build();
        DialogNode start = graph.Node("dlg_rumour/start");
        CollectionAssert.AreEqual(new[] { "dlg_rumour.start.1" }, LineIds(start.Lines));
        CollectionAssert.AreEqual(new[] { "dlg_rumour.more", "dlg_rumour.ignore" }, Ids(start.Choices));

        DialogChoice more = start.Choices[0];
        Assert.AreEqual("dlg_rumour/detail", more.Next);
        Assert.AreEqual(DialogAction.None, more.Action);
        Assert.AreEqual("dlg_rumour.more", more.Lines[0].Id);
        Assert.AreEqual("Tell me more.", more.Lines[0].Text);
        Assert.AreEqual(DialogSpeaker.Desk, more.Lines[0].Speaker);

        DialogChoice ignore = start.Choices[1];
        Assert.AreEqual(InterviewScript.HubNodeId, ignore.Next);
        Assert.AreEqual(DialogAction.CompleteDialog, ignore.Action);
        Assert.AreEqual("dlg_rumour", ignore.DialogId);
        Assert.IsTrue(string.IsNullOrEmpty(ignore.EffectName));

        DialogChoice noted = graph.Node("dlg_rumour/detail").Choices[0];
        Assert.AreEqual(DialogAction.CompleteDialog, noted.Action);
        Assert.AreEqual("Effect_Dialog_RumourHeard", noted.EffectName);
    }

    [Test]
    public void AFullWalkThroughTheRumour_EndsBackAtTheHub()
    {
        var runner = new DialogRunner(Build(), InterviewScript.Opening(Case()));
        Assert.IsNotNull(runner.Choose("dlg:dlg_rumour"));
        Assert.IsNotNull(runner.Choose("dlg_rumour.more"));
        DialogChoice last = runner.Choose("dlg_rumour.noted");

        Assert.AreEqual(DialogAction.CompleteDialog, last.Action);
        CollectionAssert.AreEqual(new[] { "request:0", "request:1", "ask" }, Ids(runner.Choices), "back at the hub, the dialog entry used");
        CollectionAssert.AreEqual(
            new[] { "case.intro", "case.claim", "dlg_rumour.start.1", "dlg_rumour.more", "dlg_rumour.detail.1", "dlg_rumour.noted" },
            LineIds(runner.Transcript));
    }

    // -----------------------------
    // DialogChecks.Problems
    // -----------------------------

    private static string Only(List<string> problems, string expected)
    {
        Assert.AreEqual(1, problems.Count, string.Join(" | ", problems));
        StringAssert.Contains(expected, problems[0]);
        return problems[0];
    }

    [Test]
    public void Problems_ASoundDialogHasNone()
    {
        CollectionAssert.IsEmpty(DialogChecks.Problems(Rumour(), 8));
    }

    [Test]
    public void Problems_NoNodes()
    {
        Only(DialogChecks.Problems(new AuthoredDialog { id = "x" }, 8), "no nodes");
        Only(DialogChecks.Problems(null, 8), "no nodes");
    }

    [Test]
    public void Problems_DuplicateNodeAndChoiceIds()
    {
        AuthoredDialog d = Rumour();
        d.nodes.Add(new ScriptNode { id = "detail", choices = { Reply("bye", "Bye.") } });
        Only(DialogChecks.Problems(d, 8), "node 'detail' is listed twice");

        AuthoredDialog c = Rumour();
        c.nodes[1].choices.Add(Reply("more", "More?"));
        Only(DialogChecks.Problems(c, 8), "choice 'more' is listed twice");
    }

    [Test]
    public void Problems_ANextThatNamesNoNode()
    {
        AuthoredDialog d = Rumour();
        d.nodes[0].choices[0].next = "detial";
        List<string> problems = DialogChecks.Problems(d, 8);
        Assert.IsTrue(problems.Any(p => p.Contains("choice 'more' leads to unknown node 'detial'")), string.Join(" | ", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("node 'detail' cannot be reached")), string.Join(" | ", problems));
    }

    [Test]
    public void Problems_ANodeUnreachableFromTheStart()
    {
        AuthoredDialog d = Rumour();
        d.nodes.Add(new ScriptNode { id = "orphan", choices = { Reply("bye", "Bye.") } });
        Only(DialogChecks.Problems(d, 8), "node 'orphan' cannot be reached from 'start'");
    }

    [Test]
    public void Problems_ANodeWithoutChoices()
    {
        AuthoredDialog d = Rumour();
        d.nodes[1].choices.Clear();
        List<string> problems = DialogChecks.Problems(d, 8);
        Assert.IsTrue(problems.Any(p => p.Contains("node 'detail' has no choices")), string.Join(" | ", problems));
    }

    [Test]
    public void Problems_NoChoiceEndsTheDialog()
    {
        var d = new AuthoredDialog
        {
            id = "loop",
            nodes =
            {
                new ScriptNode { id = "a", choices = { Reply("to_b", "B", "b") } },
                new ScriptNode { id = "b", choices = { Reply("to_a", "A", "a") } }
            }
        };
        List<string> problems = DialogChecks.Problems(d, 8);
        Assert.IsTrue(problems.Contains("no choice ends the dialog"), string.Join(" | ", problems));
    }

    [Test]
    public void Problems_ALoopThatLeftTheOnlyEndingBehind_CannotReachAnEnding()
    {
        // start -> a -> b -> a; the only ending sits in "exit", a branch the player has left.
        var d = new AuthoredDialog
        {
            id = "trap",
            nodes =
            {
                new ScriptNode { id = "start", choices = { Reply("left", "Left.", "a"), Reply("right", "Right.", "exit") } },
                new ScriptNode { id = "a", choices = { Reply("to_b", "On.", "b") } },
                new ScriptNode { id = "b", choices = { Reply("to_a", "Back.", "a") } },
                new ScriptNode { id = "exit", choices = { Reply("bye", "Bye.") } }
            }
        };
        List<string> problems = DialogChecks.Problems(d, 8);
        CollectionAssert.AreEquivalent(new[] { "node 'a' cannot reach an ending", "node 'b' cannot reach an ending" }, problems);
    }

    [Test]
    public void Problems_AnEffectOnAChoiceThatDoesNotEndTheDialog()
    {
        AuthoredDialog d = Rumour();
        d.nodes[0].choices[0].effect = "Effect_Dialog_RumourHeard";
        Only(DialogChecks.Problems(d, 8), "choice 'more' has an effect but does not end the dialog");
    }

    [Test]
    public void Problems_AnEffectOnARepeatableDialog()
    {
        AuthoredDialog d = Rumour();
        d.oneShot = false;
        Only(DialogChecks.Problems(d, 8), "choice 'noted' has an effect, but the dialog is repeatable");
    }

    [Test]
    public void Problems_ANodeWithMoreChoicesThanTheIntercomShows_UnlessTheCapacityIsZero()
    {
        AuthoredDialog d = Rumour();
        Only(DialogChecks.Problems(d, 1), "node 'start' offers 2 choices; the intercom shows at most 1");
        CollectionAssert.IsEmpty(DialogChecks.Problems(d, 0));
    }

    // -----------------------------
    // DialogChecks.MenuProblems
    // -----------------------------

    [Test]
    public void MenuProblems_TheAskMenu_BackPlusQuestionsPlusSmallTalk()
    {
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(6, true, 2, 2, 8), "< Back + 6 questions + small talk = 8");
        StringAssert.Contains("The ask menu holds 9 choices", Only(DialogChecks.MenuProblems(7, true, 2, 2, 8), "the intercom shows at most 8"));
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(7, false, 2, 2, 8), "without small talk, 7 questions fit");
    }

    [Test]
    public void MenuProblems_TheHub_DocumentsPlusAskPlusDialogs()
    {
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(1, false, 2, 5, 8), "2 requests + ask + 5 dialogs = 8");
        StringAssert.Contains("The hub holds 9 choices", Only(DialogChecks.MenuProblems(1, false, 2, 6, 8), "the intercom shows at most 8"));
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(99, true, 99, 99, 0), "no capacity, no check");
    }
}
```

- [ ] **Step 2: Run the compile check — expect failure**

Expected: `== TimeDeskEditMode.csproj: exit 1`, `error CS0246: The type or namespace name 'InterviewCase' could not be found`.

- [ ] **Step 3: Implement** (`Assets/Scripts/Domain/InterviewScript.cs`, Write tool; then `make_meta.py`)

```csharp
using System.Collections.Generic;

/// <summary>One traveller's interview input, from their case.</summary>
public sealed class InterviewCase
{
    /// <summary>The desk's opener (CaseInstance.introLine); skipped when blank.</summary>
    public string introLine;

    /// <summary>The traveller's claim sentence (CaseInstance.claimLine).</summary>
    public string claimLine;

    /// <summary>The claimed era's id: picks each question's wording override.</summary>
    public string claimedEraId;

    /// <summary>The traveller's documents' names, in paper order (one request each).</summary>
    public IReadOnlyList<string> documentNames;

    /// <summary>The traveller's answers to today's askable questions (CaseInstance.answers).</summary>
    public IReadOnlyList<InterviewAnswer> answers;

    /// <summary>The traveller's small-talk line; null means no small talk.</summary>
    public LineText smallTalk;
}

/// <summary>
/// Builds a traveller's interview graph: the hub (document requests, "Ask
/// about home >", today's narrative dialogs), the ask menu ("&lt; Back"
/// first, then the questions and small talk) and every authored dialog's
/// nodes. Pure, so every menu and line is tested headless.
/// </summary>
public static class InterviewScript
{
    /// <summary>The hub node's id.</summary>
    public const string HubNodeId = "hub";

    /// <summary>The ask menu's id.</summary>
    public const string AskNodeId = "ask";

    /// <summary>The transcript's first lines: the desk's opener ("case.intro", skipped when blank), then the traveller's claim ("case.claim").</summary>
    public static IReadOnlyList<DialogLine> Opening(InterviewCase c)
    {
        var lines = new List<DialogLine>();
        if (c == null)
            return lines;

        if (!string.IsNullOrWhiteSpace(c.introLine))
            lines.Add(new DialogLine("case.intro", DialogSpeaker.Desk, c.introLine));
        lines.Add(new DialogLine("case.claim", DialogSpeaker.Traveller, c.claimLine));
        return lines;
    }

    /// <summary>The desk asking <paramref name="q"/> of a traveller claiming <paramref name="eraId"/>.</summary>
    public static DialogLine PromptLine(InterviewQuestion q, string eraId)
    {
        LineText prompt = q.PromptFor(eraId);
        return new DialogLine(prompt.id, DialogSpeaker.Desk, prompt.text);
    }

    /// <summary>The traveller's answer line: the (era's) template with the canonical value, carrying the answer's fact.</summary>
    public static DialogLine AnswerLine(InterviewQuestion q, string eraId, InterviewAnswer a)
    {
        LineText answer = q.AnswerFor(eraId);
        return DialogLine.Answer(answer.id, Interview.Fill(answer.text, Interview.ValueToken, a != null ? a.value : null), a);
    }

    /// <summary>
    /// The traveller's graph. Hub: "request:{i}" per document (repeatable,
    /// opens document i), then "ask" when the ask menu has a question or
    /// small talk, then "dlg:{id}" per dialog (one-shot). Ask: "back" first
    /// (so an overlong menu can never hide the way back), then "q:{id}" per
    /// question the traveller has an answer for (one-shot), then "smalltalk".
    /// Authored nodes become "{dialogId}/{nodeId}" and their choices
    /// "{dialogId}.{choiceId}" (the desk speaks the label); an ending choice
    /// returns to the hub and completes the dialog with its effect.
    /// </summary>
    public static DialogGraph Build(InterviewLines lines, IReadOnlyList<InterviewQuestion> questions,
                                    IReadOnlyList<AuthoredDialog> dialogs, InterviewCase c)
    {
        lines = lines ?? new InterviewLines();
        var graph = new DialogGraph(HubNodeId);
        var hub = new DialogNode { Id = HubNodeId };
        var ask = new DialogNode { Id = AskNodeId };

        IReadOnlyList<string> documents = c != null && c.documentNames != null ? c.documentNames : new string[0];
        for (int i = 0; i < documents.Count; i++)
        {
            string name = documents[i];
            hub.Choices.Add(new DialogChoice
            {
                Id = $"request:{i}",
                Label = Interview.Fill(lines.requestLabel, Interview.DocumentToken, name),
                Lines =
                {
                    new DialogLine(Id(lines.requestPrompt), DialogSpeaker.Desk, Interview.Fill(Text(lines.requestPrompt), Interview.DocumentToken, name)),
                    new DialogLine(Id(lines.requestReply), DialogSpeaker.Traveller, Text(lines.requestReply))
                },
                Action = DialogAction.OpenDocument,
                DocumentIndex = i
            });
        }

        ask.Choices.Add(new DialogChoice { Id = "back", Label = lines.backLabel, Next = HubNodeId });

        string eraId = c != null ? c.claimedEraId : null;
        if (questions != null)
        {
            foreach (InterviewQuestion q in questions)
            {
                InterviewAnswer a = q != null ? AnswerFor(c, q.category) : null;
                if (a == null)
                    continue;

                ask.Choices.Add(new DialogChoice
                {
                    Id = $"q:{q.id}",
                    Label = q.label,
                    Lines = { PromptLine(q, eraId), AnswerLine(q, eraId, a) },
                    OneShot = true
                });
            }
        }

        if (c != null && c.smallTalk != null)
        {
            ask.Choices.Add(new DialogChoice
            {
                Id = "smalltalk",
                Label = lines.smallTalkLabel,
                Lines =
                {
                    new DialogLine(Id(lines.smallTalkPrompt), DialogSpeaker.Desk, Text(lines.smallTalkPrompt)),
                    new DialogLine(c.smallTalk.id, DialogSpeaker.Traveller, c.smallTalk.text)
                },
                OneShot = true
            });
        }

        if (ask.Choices.Count > 1)
            hub.Choices.Add(new DialogChoice { Id = "ask", Label = lines.askLabel, Next = AskNodeId });

        if (dialogs != null)
        {
            foreach (AuthoredDialog d in dialogs)
            {
                if (d == null || d.nodes == null || d.nodes.Count == 0 || d.nodes[0] == null)
                    continue;

                hub.Choices.Add(new DialogChoice { Id = $"dlg:{d.id}", Label = d.label, Next = NodeId(d, d.nodes[0].id), OneShot = true });
                foreach (ScriptNode node in d.nodes)
                    if (node != null)
                        graph.Add(BuildNode(d, node));
            }
        }

        graph.Add(hub);
        graph.Add(ask);
        return graph;
    }

    /// <summary>An authored node as a runtime node: namespaced ids, the desk speaking each choice's label.</summary>
    private static DialogNode BuildNode(AuthoredDialog d, ScriptNode node)
    {
        var built = new DialogNode { Id = NodeId(d, node.id) };
        if (node.lines != null)
            foreach (ScriptLine line in node.lines)
                if (line != null)
                    built.Lines.Add(new DialogLine(line.id, line.speaker, line.text));

        if (node.choices == null)
            return built;

        foreach (ScriptChoice choice in node.choices)
        {
            if (choice == null)
                continue;

            string id = $"{d.id}.{choice.id}";
            var runtime = new DialogChoice { Id = id, Label = choice.label };
            runtime.Lines.Add(new DialogLine(id, DialogSpeaker.Desk, choice.label));
            if (choice.lines != null)
                foreach (ScriptLine line in choice.lines)
                    if (line != null)
                        runtime.Lines.Add(new DialogLine(line.id, line.speaker, line.text));

            if (!string.IsNullOrEmpty(choice.next))
            {
                runtime.Next = NodeId(d, choice.next);
            }
            else
            {
                runtime.Next = HubNodeId;
                runtime.Action = DialogAction.CompleteDialog;
                runtime.DialogId = d.id;
                runtime.EffectName = choice.effect;
            }

            built.Choices.Add(runtime);
        }

        return built;
    }

    /// <summary>A dialog node's graph id.</summary>
    private static string NodeId(AuthoredDialog d, string nodeId) => $"{d.id}/{nodeId}";

    /// <summary>The traveller's answer about a category, or null.</summary>
    private static InterviewAnswer AnswerFor(InterviewCase c, ClueCategory category)
    {
        if (c == null || c.answers == null)
            return null;

        foreach (InterviewAnswer a in c.answers)
            if (a != null && a.category == category)
                return a;

        return null;
    }

    private static string Id(LineText line) => line != null ? line.id : null;

    private static string Text(LineText line) => line != null ? line.text : null;
}

/// <summary>
/// Structure and capacity rules for dialogs and menus, pure so the generator
/// (before writing), the validator (on assets) and the day-start interview
/// (InterviewDay) share one set of rules.
/// </summary>
public static class DialogChecks
{
    /// <summary>
    /// Every structural problem of a dialog, each naming its node or choice:
    /// no nodes; duplicate node or choice ids; a next naming no node; a node
    /// unreachable from the start; a node without choices; no ending choice;
    /// a reachable node from which no ending can be reached; an effect on a
    /// choice that does not end the dialog, or on a dialog that is not
    /// one-shot; a node offering more than <paramref name="maxChoices"/>
    /// choices (skipped when it is 0 or less). Empty for a sound dialog.
    /// </summary>
    public static List<string> Problems(AuthoredDialog d, int maxChoices)
    {
        var problems = new List<string>();
        if (d == null || d.nodes == null || d.nodes.Count == 0 || d.nodes[0] == null)
        {
            problems.Add("the dialog has no nodes");
            return problems;
        }

        var nodes = new Dictionary<string, ScriptNode>();
        var choiceIds = new HashSet<string>();
        bool anyEnding = false;

        foreach (ScriptNode node in d.nodes)
        {
            if (node == null)
            {
                problems.Add("a node is empty");
                continue;
            }

            if (nodes.ContainsKey(node.id ?? string.Empty))
                problems.Add($"node '{node.id}' is listed twice");
            else
                nodes.Add(node.id ?? string.Empty, node);

            foreach (ScriptChoice choice in Choices(node))
            {
                if (!choiceIds.Add(choice.id ?? string.Empty))
                    problems.Add($"choice '{choice.id}' is listed twice");
                if (string.IsNullOrEmpty(choice.next))
                    anyEnding = true;
                else if (!string.IsNullOrEmpty(choice.effect))
                    problems.Add($"choice '{choice.id}' has an effect but does not end the dialog");
                if (!string.IsNullOrEmpty(choice.effect) && !d.oneShot)
                    problems.Add($"choice '{choice.id}' has an effect, but the dialog is repeatable (a dialog with a consequence must be one-shot)");
            }

            if (Choices(node).Count == 0)
                problems.Add($"node '{node.id}' has no choices");
            else if (maxChoices > 0 && Choices(node).Count > maxChoices)
                problems.Add($"node '{node.id}' offers {Choices(node).Count} choices; the intercom shows at most {maxChoices}");
        }

        foreach (ScriptNode node in nodes.Values)
            foreach (ScriptChoice choice in Choices(node))
                if (!string.IsNullOrEmpty(choice.next) && !nodes.ContainsKey(choice.next))
                    problems.Add($"choice '{choice.id}' leads to unknown node '{choice.next}'");

        if (!anyEnding)
            problems.Add("no choice ends the dialog");

        // Forwards: what the player can reach from the start.
        string start = d.nodes[0].id ?? string.Empty;
        var reachable = new HashSet<string> { start };
        var queue = new Queue<string>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            foreach (ScriptChoice choice in Choices(nodes[queue.Dequeue()]))
                if (!string.IsNullOrEmpty(choice.next) && nodes.ContainsKey(choice.next) && reachable.Add(choice.next))
                    queue.Enqueue(choice.next);
        }

        // Backwards: every node from which some path ends the dialog.
        var ending = new HashSet<string>();
        foreach (ScriptNode node in nodes.Values)
            foreach (ScriptChoice choice in Choices(node))
                if (string.IsNullOrEmpty(choice.next))
                    ending.Add(node.id ?? string.Empty);

        bool grew = true;
        while (grew)
        {
            grew = false;
            foreach (ScriptNode node in nodes.Values)
            {
                string id = node.id ?? string.Empty;
                if (ending.Contains(id))
                    continue;

                foreach (ScriptChoice choice in Choices(node))
                {
                    if (!string.IsNullOrEmpty(choice.next) && ending.Contains(choice.next))
                    {
                        ending.Add(id);
                        grew = true;
                        break;
                    }
                }
            }
        }

        foreach (string id in nodes.Keys)
        {
            if (!reachable.Contains(id))
                problems.Add($"node '{id}' cannot be reached from '{start}'");
            else if (anyEnding && !ending.Contains(id))
                problems.Add($"node '{id}' cannot reach an ending");
        }

        return problems;
    }

    /// <summary>
    /// Menus larger than the intercom shows: the ask menu (1 back + the
    /// questions + 1 when there is small talk) or the hub (the most documents
    /// of any traveller + 1 ask entry + every dialog, counted as offered at
    /// once). Skipped when <paramref name="maxChoices"/> is 0 or less.
    /// </summary>
    public static List<string> MenuProblems(int questions, bool smallTalk, int maxDocuments, int dialogs, int maxChoices)
    {
        var problems = new List<string>();
        if (maxChoices <= 0)
            return problems;

        int ask = 1 + questions + (smallTalk ? 1 : 0);
        if (ask > maxChoices)
            problems.Add($"The ask menu holds {ask} choices (< Back, {questions} question(s){(smallTalk ? ", small talk" : string.Empty)}); the intercom shows at most {maxChoices}.");

        int hub = maxDocuments + 1 + dialogs;
        if (hub > maxChoices)
            problems.Add($"The hub holds {hub} choices ({maxDocuments} document request(s), the ask entry, {dialogs} dialog(s)); the intercom shows at most {maxChoices}.");

        return problems;
    }

    /// <summary>A node's non-null choices (empty for none).</summary>
    private static List<ScriptChoice> Choices(ScriptNode node)
    {
        var choices = new List<ScriptChoice>();
        if (node != null && node.choices != null)
            foreach (ScriptChoice c in node.choices)
                if (c != null)
                    choices.Add(c);
        return choices;
    }
}
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 326, failed 0` (+ 22).

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Domain/InterviewScript.cs Assets/Scripts/Domain/InterviewScript.cs.meta Assets/Tests/EditMode/InterviewScriptTests.cs Assets/Tests/EditMode/InterviewScriptTests.cs.meta && git commit -F - <<'EOF'
feat(domain): the interview graph and dialog structure rules

InterviewScript builds a traveller's hub (document requests first, the ask
entry, today's dialogs), the ask menu ("< Back" first, then the answered
questions and small talk) and the authored dialogs' namespaced nodes.
DialogChecks reports broken dialogs (unreachable nodes, nodes that cannot
reach an ending, misplaced effects, overfull nodes) and overfull menus.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 9: The day's interview and the end-of-shift plan (`InterviewDay`, `DialogOutcomes`)

Every availability rule in one tested place (spec R7, R23, §2.5): the askable questions, the ones that may carry a spoken tell (day-gated only), the dialogs offered at day start (conditions pass, structure sound, a one-shot dialog not done), completion recorded in the shift ledger, and what the end of the shift applies once.

**Files:**
- Create: `Assets/Scripts/Domain/InterviewDay.cs` (+ `.meta`)
- Modify: `Assets/Scripts/Domain/ShiftLedger.cs`
- Test: `Assets/Tests/EditMode/InterviewDayTests.cs` (+ `.meta`)

- [ ] **Step 1: Write the failing test** (`Assets/Tests/EditMode/InterviewDayTests.cs`, Write tool; then `make_meta.py`)

```csharp
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// What the office offers on a day. Questions: Currency (ungated), Capital
/// (DayAtLeast 2), Ruler (DayAtLeast 3), Date of birth (UpgradeOwned
/// interview_protocols) and a Language question gated by the flag
/// "met_tesla". Dialogs: the rumour (DayAtLeast 2, one-shot), its follow-up
/// (FlagSet rumour_calculators, one-shot), a repeatable chat (ungated) and a
/// broken dialog with no nodes (DayAtLeast 99).
/// </summary>
public class InterviewDayTests
{
    private static GateCondition Day(int n) => new GateCondition(TriggerConditionType.DayAtLeast, null, n);

    private static GateSnapshot Snap(int day, string[] flags = null, string[] upgrades = null) =>
        new GateSnapshot(day, 100f, flags, upgrades, null, null, null, null);

    private static Gated<InterviewQuestion> Question(string id, ClueCategory category, params GateCondition[] conditions) =>
        new Gated<InterviewQuestion>(new InterviewQuestion { id = id, category = category, label = id }, conditions);

    private static List<Gated<InterviewQuestion>> Questions() => new List<Gated<InterviewQuestion>>
    {
        Question("q_currency", ClueCategory.Currency),
        Question("q_capital", ClueCategory.Geography, Day(2)),
        Question("q_ruler", ClueCategory.Politics, Day(3)),
        Question("q_born", ClueCategory.BirthDate, new GateCondition(TriggerConditionType.UpgradeOwned, "interview_protocols", 0f)),
        Question("q_tongue", ClueCategory.Language, new GateCondition(TriggerConditionType.FlagSet, "met_tesla", 0f))
    };

    /// <summary>A sound one-node dialog: one ending choice, with an effect for one-shot dialogs.</summary>
    private static AuthoredDialog Dialog(string id, bool oneShot) => new AuthoredDialog
    {
        id = id,
        label = id,
        oneShot = oneShot,
        nodes =
        {
            new ScriptNode
            {
                id = "start",
                choices = { new ScriptChoice { id = "bye", label = "Bye.", next = "", effect = oneShot ? "Effect_" + id : "" } }
            }
        }
    };

    private static List<Gated<AuthoredDialog>> Dialogs() => new List<Gated<AuthoredDialog>>
    {
        new Gated<AuthoredDialog>(Dialog("dlg_rumour", true), new[] { Day(2) }),
        new Gated<AuthoredDialog>(Dialog("dlg_rumour_followup", true), new[] { new GateCondition(TriggerConditionType.FlagSet, "rumour_calculators", 0f) }),
        new Gated<AuthoredDialog>(Dialog("dlg_chat", false), null),
        new Gated<AuthoredDialog>(new AuthoredDialog { id = "dlg_broken", label = "Broken" }, new[] { Day(99) })
    };

    private static InterviewDay DayOf(GateSnapshot snapshot, ShiftLedger ledger = null) =>
        new InterviewDay(new InterviewLines { menuCapacity = 8 }, Questions(), Dialogs(), snapshot, ledger ?? new ShiftLedger());

    private static string[] Offered(InterviewDay day) => day.OfferedDialogs().Select(d => d.id).ToArray();

    [Test]
    public void Questions_AreThoseWhoseConditionsPass_InLibraryOrder()
    {
        InterviewDay day1 = DayOf(Snap(1));
        CollectionAssert.AreEqual(new[] { "q_currency" }, day1.Questions.Select(q => q.id).ToArray());
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency }, day1.AskableCategories);

        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Geography }, DayOf(Snap(2)).AskableCategories);
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Geography, ClueCategory.Politics }, DayOf(Snap(3)).AskableCategories);

        InterviewDay everything = DayOf(Snap(3, new[] { "met_tesla" }, new[] { "interview_protocols" }));
        CollectionAssert.AreEqual(new[] { "q_currency", "q_capital", "q_ruler", "q_born", "q_tongue" }, everything.Questions.Select(q => q.id).ToArray());
    }

    [Test]
    public void AnswerTellCategories_AreTheDayGatedQuestionsOnly()
    {
        InterviewDay day = DayOf(Snap(3, new[] { "met_tesla" }, new[] { "interview_protocols" }));
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Geography, ClueCategory.Politics, ClueCategory.BirthDate, ClueCategory.Language }, day.AskableCategories);
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Geography, ClueCategory.Politics }, day.AnswerTellCategories,
                                  "upgrade- and flag-gated questions are hint-only");

        CollectionAssert.AreEqual(DayOf(Snap(3)).AnswerTellCategories, day.AnswerTellCategories, "a purchase or a flag never changes the tell categories");
    }

    [Test]
    public void Dialogs_WhoseConditionsFail_AreNotOffered()
    {
        CollectionAssert.AreEqual(new[] { "dlg_chat" }, Offered(DayOf(Snap(1))));
        CollectionAssert.AreEqual(new[] { "dlg_rumour", "dlg_chat" }, Offered(DayOf(Snap(2))));
        CollectionAssert.AreEqual(new[] { "dlg_rumour", "dlg_rumour_followup", "dlg_chat" }, Offered(DayOf(Snap(3, new[] { "rumour_calculators" }))));
    }

    [Test]
    public void AOneShotDialogAlreadyDone_IsNotOffered_ARepeatableOneStillIs()
    {
        InterviewDay day = DayOf(Snap(3, new[] { FlagKeys.DialogDone("dlg_rumour"), FlagKeys.DialogDone("dlg_chat") }));
        CollectionAssert.AreEqual(new[] { "dlg_chat" }, Offered(day));
    }

    [Test]
    public void ABrokenDialog_IsNeverOffered_AndContentProblemsNameIt_EvenWhenItsConditionsFail()
    {
        InterviewDay day = DayOf(Snap(1));
        CollectionAssert.DoesNotContain(Offered(day), "dlg_broken");
        Assert.AreEqual(1, day.ContentProblems.Count, string.Join(" | ", day.ContentProblems));
        StringAssert.StartsWith("Dialog 'dlg_broken' is not offered: ", day.ContentProblems[0]);
        StringAssert.Contains("no nodes", day.ContentProblems[0]);

        // A rumour-shaped start node with two choices: too many for an intercom of one, fine with no capacity set.
        var twoChoices = new List<Gated<AuthoredDialog>> { new Gated<AuthoredDialog>(new AuthoredDialog
        {
            id = "dlg_two",
            label = "Two",
            oneShot = true,
            nodes =
            {
                new ScriptNode
                {
                    id = "start",
                    choices =
                    {
                        new ScriptChoice { id = "more", label = "Tell me more.", next = "" },
                        new ScriptChoice { id = "noted", label = "Noted.", next = "", effect = "Effect_dlg_two" }
                    }
                }
            }
        }, null) };

        var one = new InterviewDay(new InterviewLines { menuCapacity = 1 }, null, twoChoices, Snap(1), new ShiftLedger());
        CollectionAssert.IsEmpty(Offered(one), "two choices never fit an intercom of one");
        Assert.AreEqual(1, one.ContentProblems.Count, string.Join(" | ", one.ContentProblems));
        StringAssert.StartsWith("Dialog 'dlg_two' is not offered: ", one.ContentProblems[0]);
        StringAssert.Contains("node 'start' offers 2 choices; the intercom shows at most 1", one.ContentProblems[0]);

        var unlimited = new InterviewDay(new InterviewLines { menuCapacity = 0 }, null, twoChoices, Snap(1), new ShiftLedger());
        CollectionAssert.AreEqual(new[] { "dlg_two" }, Offered(unlimited), "no capacity, no capacity problem");
        CollectionAssert.IsEmpty(unlimited.ContentProblems);
    }

    [Test]
    public void OfferedDialogs_DropsADialogCompletedThisShift()
    {
        var ledger = new ShiftLedger();
        InterviewDay day = DayOf(Snap(2), ledger);
        Assert.IsTrue(day.Complete("dlg_chat", ""));
        CollectionAssert.AreEqual(new[] { "dlg_rumour" }, Offered(day), "any completed dialog leaves the hub for the rest of the shift");
    }

    [Test]
    public void Complete_RecordsTheOutcome_AndRefusesAnUnknownOrRepeatedId()
    {
        var ledger = new ShiftLedger();
        InterviewDay day = DayOf(Snap(2), ledger);

        Assert.IsTrue(day.Complete("dlg_rumour", "Effect_dlg_rumour"));
        Assert.IsTrue(day.Complete("dlg_chat", ""));
        Assert.IsFalse(day.Complete("dlg_rumour", "Effect_dlg_rumour"), "already completed this shift");
        Assert.IsFalse(day.Complete("dlg_rumour_followup", ""), "not offered today");
        Assert.IsFalse(day.Complete("missing", ""));

        Assert.AreEqual(2, ledger.dialogOutcomes.Count);
        Assert.AreEqual("dlg_rumour", ledger.dialogOutcomes[0].dialogId);
        Assert.AreEqual("Effect_dlg_rumour", ledger.dialogOutcomes[0].effectName);
        Assert.IsTrue(ledger.dialogOutcomes[0].oneShot);
        Assert.IsFalse(ledger.dialogOutcomes[1].oneShot);
    }

    [Test]
    public void FlagsToSet_OneDoneFlagPerOneShotDialog_InLedgerOrder()
    {
        var outcomes = new List<DialogOutcome>
        {
            new DialogOutcome { dialogId = "b", effectName = "", oneShot = true },
            new DialogOutcome { dialogId = "chat", effectName = "", oneShot = false },
            new DialogOutcome { dialogId = "a", effectName = "Effect_A", oneShot = true },
            new DialogOutcome { dialogId = "b", effectName = "Effect_B", oneShot = true }
        };
        CollectionAssert.AreEqual(new[] { "dlg:b:done", "dlg:a:done" }, DialogOutcomes.FlagsToSet(outcomes));
    }

    [Test]
    public void EffectsToApply_EachOutcomeWithAnEffect_OncePerDialog_InLedgerOrder()
    {
        var outcomes = new List<DialogOutcome>
        {
            new DialogOutcome { dialogId = "b", effectName = "", oneShot = true },
            new DialogOutcome { dialogId = "a", effectName = "Effect_A", oneShot = true },
            new DialogOutcome { dialogId = "b", effectName = "Effect_B", oneShot = true },
            new DialogOutcome { dialogId = "a", effectName = "Effect_A2", oneShot = true }
        };
        CollectionAssert.AreEqual(new[] { "Effect_A", "Effect_B" }, DialogOutcomes.EffectsToApply(outcomes).Select(o => o.effectName).ToArray());
    }

    [Test]
    public void NullInputs_AreEmpty()
    {
        CollectionAssert.IsEmpty(DialogOutcomes.FlagsToSet(null));
        CollectionAssert.IsEmpty(DialogOutcomes.EffectsToApply(null));

        var day = new InterviewDay(null, null, null, null, null);
        CollectionAssert.IsEmpty(day.Questions);
        CollectionAssert.IsEmpty(day.OfferedDialogs());
        Assert.IsNotNull(day.Lines);
        Assert.IsFalse(day.Complete("x", ""));
    }
}
```

- [ ] **Step 2: Run the compile check — expect failure**

Expected: `== TimeDeskEditMode.csproj: exit 1`, `error CS0246: The type or namespace name 'InterviewDay' could not be found`.

- [ ] **Step 3: Implement** — save as `SCRATCH/p3_t09.py` and run (the ledger records dialog outcomes):

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Scripts\Domain\ShiftLedger.cs', [
("""/// <summary>
/// Records every verdict of the current shift for the end-of-day report.
/// Pure data — no Unity dependencies. Rebuilt fresh each day.
/// </summary>
public sealed class ShiftLedger
{
    /// <summary>All verdicts issued this shift, in order.</summary>
    public readonly List<CaseVerdict> verdicts = new();
""",
"""/// <summary>
/// Records every verdict of the current shift for the end-of-day report, and
/// every narrative dialog completed this shift. Pure data — no Unity
/// dependencies. Rebuilt fresh each day.
/// </summary>
public sealed class ShiftLedger
{
    /// <summary>All verdicts issued this shift, in order.</summary>
    public readonly List<CaseVerdict> verdicts = new();

    /// <summary>Narrative dialogs completed this shift, in order (applied at the end of the shift; DialogOutcomes).</summary>
    public readonly List<DialogOutcome> dialogOutcomes = new();
"""),
("""/// <summary>
/// The outcome of a single case decision, fully resolved.
/// </summary>""",
"""/// <summary>
/// A narrative dialog completed this shift; its effect is applied at the end
/// of the shift (DialogOutcomes).
/// </summary>
[Serializable]
public sealed class DialogOutcome
{
    /// <summary>The completed dialog's id.</summary>
    public string dialogId;

    /// <summary>EffectSO asset name the ending choice named (empty = none).</summary>
    public string effectName;

    /// <summary>True when the dialog is one-shot per run (the end of the shift sets its done flag).</summary>
    public bool oneShot;
}

/// <summary>
/// The outcome of a single case decision, fully resolved.
/// </summary>"""),
])
```

Then write `Assets/Scripts/Domain/InterviewDay.cs` (Write tool; then `make_meta.py`):

```csharp
using System.Collections.Generic;

/// <summary>
/// The day's interview, fixed at day start: which questions are askable,
/// which of them may carry a spoken tell, and which narrative dialogs are
/// offered (their conditions pass on the day-start snapshot, their structure
/// is sound, and a one-shot dialog is not done yet). Every availability rule
/// lives here, so the office decides nothing on its own.
/// </summary>
public sealed class InterviewDay
{
    private readonly List<InterviewQuestion> _questions = new List<InterviewQuestion>();
    private readonly List<ClueCategory> _askable = new List<ClueCategory>();
    private readonly List<ClueCategory> _answerTell = new List<ClueCategory>();
    private readonly List<AuthoredDialog> _dayStartDialogs = new List<AuthoredDialog>();
    private readonly List<string> _problems = new List<string>();
    private readonly ShiftLedger _ledger;

    /// <summary>Decides today's interview once, from the day-start snapshot.</summary>
    public InterviewDay(InterviewLines lines, IReadOnlyList<Gated<InterviewQuestion>> questions,
                        IReadOnlyList<Gated<AuthoredDialog>> dialogs, GateSnapshot snapshot, ShiftLedger ledger)
    {
        Lines = lines ?? new InterviewLines();
        _ledger = ledger ?? new ShiftLedger();

        if (questions != null)
        {
            foreach (Gated<InterviewQuestion> q in questions)
            {
                if (q.Item == null || !Gates.AllPass(q.Conditions, snapshot))
                    continue;

                _questions.Add(q.Item);
                _askable.Add(q.Item.category);
                if (Gates.DayOnly(q.Conditions))
                    _answerTell.Add(q.Item.category);
            }
        }

        if (dialogs != null)
        {
            foreach (Gated<AuthoredDialog> d in dialogs)
            {
                if (d.Item == null)
                    continue;

                List<string> problems = DialogChecks.Problems(d.Item, Lines.menuCapacity);
                foreach (string problem in problems)
                    _problems.Add($"Dialog '{d.Item.id}' is not offered: {problem}");

                bool done = d.Item.oneShot && snapshot != null && snapshot.HasFlag(FlagKeys.DialogDone(d.Item.id));
                if (problems.Count == 0 && !done && Gates.AllPass(d.Conditions, snapshot))
                    _dayStartDialogs.Add(d.Item);
            }
        }
    }

    /// <summary>The interview's fixed wording and layout limits.</summary>
    public InterviewLines Lines { get; }

    /// <summary>Today's askable questions, in library order.</summary>
    public IReadOnlyList<InterviewQuestion> Questions => _questions;

    /// <summary>The askable questions' categories, in the same order: every traveller answers each.</summary>
    public IReadOnlyList<ClueCategory> AskableCategories => _askable;

    /// <summary>
    /// The categories of the askable questions gated by day alone, in the same
    /// order: only these may carry an Answer tell (a question gated by an
    /// upgrade, a flag, a counter or stability is hint-only).
    /// </summary>
    public IReadOnlyList<ClueCategory> AnswerTellCategories => _answerTell;

    /// <summary>Every structural problem of every dialog, whether or not its conditions pass ("Dialog 'x' is not offered: ...").</summary>
    public IReadOnlyList<string> ContentProblems => _problems;

    /// <summary>The dialogs offered at day start, minus those completed this shift.</summary>
    public IReadOnlyList<AuthoredDialog> OfferedDialogs()
    {
        var offered = new List<AuthoredDialog>();
        foreach (AuthoredDialog d in _dayStartDialogs)
            if (!CompletedThisShift(d.id))
                offered.Add(d);
        return offered;
    }

    /// <summary>
    /// Records a finished dialog in the shift ledger (its effect is applied at
    /// the end of the shift; DialogOutcomes). False, with nothing recorded,
    /// for a dialog not offered today or already completed this shift.
    /// </summary>
    public bool Complete(string dialogId, string effectName)
    {
        AuthoredDialog dialog = null;
        foreach (AuthoredDialog d in _dayStartDialogs)
        {
            if (d.id == dialogId)
            {
                dialog = d;
                break;
            }
        }

        if (dialog == null || CompletedThisShift(dialogId))
            return false;

        _ledger.dialogOutcomes.Add(new DialogOutcome { dialogId = dialogId, effectName = effectName, oneShot = dialog.oneShot });
        return true;
    }

    /// <summary>True when the ledger already holds this dialog.</summary>
    private bool CompletedThisShift(string dialogId)
    {
        foreach (DialogOutcome o in _ledger.dialogOutcomes)
            if (o != null && o.dialogId == dialogId)
                return true;
        return false;
    }
}

/// <summary>
/// What the end of the shift applies for the dialogs completed during it:
/// the run-level done flags and the effects, each once. Pure, so the one-shot
/// memory and the apply-once rule are tested headless.
/// </summary>
public static class DialogOutcomes
{
    /// <summary>FlagKeys.DialogDone for every one-shot outcome, once per dialog id, in ledger order (empty for null).</summary>
    public static IReadOnlyList<string> FlagsToSet(IReadOnlyList<DialogOutcome> outcomes)
    {
        var flags = new List<string>();
        if (outcomes == null)
            return flags;

        foreach (DialogOutcome o in outcomes)
        {
            if (o == null || !o.oneShot)
                continue;

            string flag = FlagKeys.DialogDone(o.dialogId);
            if (!flags.Contains(flag))
                flags.Add(flag);
        }

        return flags;
    }

    /// <summary>The outcomes that name an effect, the first per dialog id, in ledger order (empty for null).</summary>
    public static IReadOnlyList<DialogOutcome> EffectsToApply(IReadOnlyList<DialogOutcome> outcomes)
    {
        var effects = new List<DialogOutcome>();
        if (outcomes == null)
            return effects;

        var seen = new HashSet<string>();
        foreach (DialogOutcome o in outcomes)
        {
            if (o == null || string.IsNullOrWhiteSpace(o.effectName) || !seen.Add(o.dialogId ?? string.Empty))
                continue;

            effects.Add(o);
        }

        return effects;
    }
}
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 336, failed 0` (+ 10). This is the offline total for the rest of the plan.

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Domain/InterviewDay.cs Assets/Scripts/Domain/InterviewDay.cs.meta Assets/Scripts/Domain/ShiftLedger.cs Assets/Tests/EditMode/InterviewDayTests.cs Assets/Tests/EditMode/InterviewDayTests.cs.meta && git commit -F - <<'EOF'
feat(domain): the day's interview and the end-of-shift plan

InterviewDay decides at day start which questions are askable, which of
them may carry a spoken tell (day-gated only, so purchases and flags never
change the lie draws) and which dialogs are offered; completions go to the
shift ledger. DialogOutcomes plans the done flags and effects the end of
the shift applies, each once.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 10: Triggers evaluate through the Domain gates (`TimelineService`)

Glue (spec §2.8, R6, R22): `ToGate` projects a `TriggerCondition` to a plain `GateCondition` (a missing profile, attribute or nation gives a null key, which never passes, as today), `ToGates` projects a list, `Snapshot` copies the world (scores under the keys the conditions read, with `GetProfileAttributeScore` as the one baseline formula), all three private to `TimelineService` (their other caller, `BuildInterviewDay`, joins them in Task 14), and the private `AllConditionsPass` keeps its name and signature (Task 18 calls it by reflection against Task 0's baseline) but becomes one `Gates.AllPass` call. No behaviour change, so no FEATURES change.

**Files:**
- Modify: `Assets/Scripts/Timeline/TimelineService.cs`

- [ ] **Step 1: Implement** — save as `SCRATCH/p3_t10.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
from cut import cut
W = r'E:\unity\NOPE-feat-clock'
T = W + r'\Assets\Scripts\Timeline\TimelineService.cs'

# Triggers evaluate through the Domain gate evaluator: the conditions are
# projected to plain gates and the world is copied into a snapshot.
cut(T, """    /// <summary>Returns true if every condition on the trigger passes.</summary>
    private static bool AllConditionsPass(TimelineTriggerSO trigger, WorldState world)
    {""", """        return true;
    }
""", """    /// <summary>
    /// Returns true if every condition on the trigger passes (Gates.AllPass):
    /// one snapshot per trigger, so a trigger sees the flags that earlier
    /// triggers set tonight.
    /// </summary>
    private static bool AllConditionsPass(TimelineTriggerSO trigger, WorldState world) =>
        Gates.AllPass(ToGates(trigger.conditions), Snapshot(world, trigger.conditions));

    /// <summary>
    /// A condition as the Domain gates read it: its type, threshold and plain
    /// key (the counter, flag or upgrade key; the profile-attribute score key,
    /// the dominance key or the nation score key for the reference types; null
    /// when a needed reference is missing, which never passes).
    /// </summary>
    private static GateCondition ToGate(TriggerCondition c)
    {
        string key;
        switch (c.type)
        {
            case TriggerConditionType.CounterAtLeast:
            case TriggerConditionType.FlagSet:
            case TriggerConditionType.FlagNotSet:
            case TriggerConditionType.UpgradeOwned:
                key = c.key;
                break;
            case TriggerConditionType.AttributeScoreAtLeast:
            case TriggerConditionType.AttributeScoreAtMost:
                key = c.profile != null && c.attribute != null ? TimelineKeys.ProfileAttr(c.profile, c.attribute) : null;
                break;
            case TriggerConditionType.AttributeIsDominant:
            case TriggerConditionType.AttributeIsSupporting:
                key = c.profile != null && c.attribute != null ? TimelineKeys.Dominance(c.profile, c.attribute) : null;
                break;
            case TriggerConditionType.NationScoreAtLeast:
                key = c.nation != null ? TimelineKeys.Nation(c.nation) : null;
                break;
            default:
                key = null;
                break;
        }

        return new GateCondition(c.type, key, c.threshold);
    }

    /// <summary>The non-null conditions projected with <see cref="ToGate"/>, in order (empty for null).</summary>
    private static List<GateCondition> ToGates(IEnumerable<TriggerCondition> conditions)
    {
        var gates = new List<GateCondition>();
        if (conditions != null)
            foreach (TriggerCondition c in conditions)
                if (c != null)
                    gates.Add(ToGate(c));
        return gates;
    }

    /// <summary>
    /// Copies what gates read from the world: day, stability, flags, owned
    /// upgrades, counters and dominance tiers, plus the scores the given
    /// conditions read, each under its gate key with the value it has now
    /// (GetProfileAttributeScore for profile scores, so the baseline formula
    /// keeps one home; the timeline score for nations).
    /// </summary>
    private static GateSnapshot Snapshot(WorldState world, IEnumerable<TriggerCondition> conditions)
    {
        var scores = new List<KeyValuePair<string, float>>();
        if (conditions != null)
        {
            foreach (TriggerCondition c in conditions)
            {
                if (c == null)
                    continue;

                if ((c.type == TriggerConditionType.AttributeScoreAtLeast || c.type == TriggerConditionType.AttributeScoreAtMost) &&
                    c.profile != null && c.attribute != null)
                    scores.Add(new KeyValuePair<string, float>(TimelineKeys.ProfileAttr(c.profile, c.attribute), GetProfileAttributeScore(world, c.profile, c.attribute)));
                else if (c.type == TriggerConditionType.NationScoreAtLeast && c.nation != null)
                    scores.Add(new KeyValuePair<string, float>(TimelineKeys.Nation(c.nation), world.timeline.GetScore(TimelineKeys.Nation(c.nation))));
            }
        }

        return new GateSnapshot(
            world.day,
            world.timelineStability,
            world.flags,
            world.unlockedUpgradeIds,
            world.counters.Where(e => e != null).Select(e => new KeyValuePair<string, int>(e.key, e.value)),
            scores,
            world.timeline.dominantKeys,
            world.timeline.supportingKeys);
    }
""")
```

- [ ] **Step 2: Compile check and run**

Expected: exit 0 / exit 0; `passed 336, failed 0`.

- [ ] **Step 3: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Timeline/TimelineService.cs && git commit -F - <<'EOF'
refactor(timeline): triggers evaluate through the Domain gates

TimelineService projects conditions to GateConditions (ToGate, ToGates),
copies the world into a GateSnapshot (Snapshot) and evaluates triggers with
Gates.AllPass, the evaluator the day's interview will use. One snapshot per
trigger, so a trigger still sees flags set earlier the same night.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 11: Shared paging for book and transcript windows (`PagedRowsWindow`)

The book window's paging and row cloning move into an abstract base with the **same serialized field names**, so the existing book template keeps its references (spec §2.11, R10). Pure refactor: no behaviour change.

**Files:**
- Create: `Assets/Scripts/UI/PagedRowsWindow.cs` (+ `.meta`)
- Modify: `Assets/Scripts/UI/ReferenceBookWindowController.cs` (whole file)

- [ ] **Step 1: Implement** — write `Assets/Scripts/UI/PagedRowsWindow.cs` (Write tool; then `make_meta.py`):

```csharp
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A window that lists rows a page at a time (reference books, the interview
/// transcript): a footer with prev/next and "Page n/m", and rows cloned from
/// <see cref="entryRowTemplate"/> (a disabled row with two TMP texts, an Image
/// background and a Button). Subclasses say how many rows there are and fill
/// each one. The serialized field names are the ones the builder wires.
/// </summary>
public abstract class PagedRowsWindow : MonoBehaviour
{
    /// <summary>Title in the window's header.</summary>
    [SerializeField] private TMP_Text titleText;

    /// <summary>Footer "Page n/m" text.</summary>
    [SerializeField] private TMP_Text pageText;

    /// <summary>Footer button to the previous page.</summary>
    [SerializeField] private Button prevButton;

    /// <summary>Footer button to the next page.</summary>
    [SerializeField] private Button nextButton;

    /// <summary>Container the page's rows are cloned under.</summary>
    [SerializeField] private Transform entryRowsRoot;

    /// <summary>Disabled row cloned per shown row.</summary>
    [SerializeField] private GameObject entryRowTemplate;

    /// <summary>Rows per page.</summary>
    [SerializeField, Min(1)] private int entriesPerPage = 6;

    /// <summary>The page shown (0-based).</summary>
    private int _page;

    /// <summary>The current page's row clones.</summary>
    private readonly List<GameObject> _rows = new();

    /// <summary>Wires the footer buttons and hides the template.</summary>
    protected virtual void Awake()
    {
        if (prevButton != null)
            prevButton.onClick.AddListener(() => ShowPage(_page - 1));

        if (nextButton != null)
            nextButton.onClick.AddListener(() => ShowPage(_page + 1));

        if (entryRowTemplate != null)
            entryRowTemplate.SetActive(false);
    }

    /// <summary>How many rows the window lists in all.</summary>
    protected abstract int RowCount { get; }

    /// <summary>Fills one cloned row (texts in child order: [0] heading/speaker, [1] value/sentence).</summary>
    protected abstract void FillRow(int index, GameObject row, TMP_Text[] texts, Image background, Button button);

    /// <summary>Sets the header title.</summary>
    protected void SetTitle(string title)
    {
        if (titleText != null)
            titleText.text = title;
    }

    /// <summary>Switches to a page (clamped), updates the footer and rebuilds its rows.</summary>
    public void ShowPage(int page)
    {
        int pages = PageCount();
        _page = Mathf.Clamp(page, 0, pages - 1);

        if (pageText != null)
            pageText.text = $"Page {_page + 1}/{pages}";

        if (prevButton != null)
            prevButton.interactable = _page > 0;

        if (nextButton != null)
            nextButton.interactable = _page < pages - 1;

        Rebuild();
    }

    /// <summary>Shows the newest page.</summary>
    public void ShowLastPage() => ShowPage(PageCount() - 1);

    /// <summary>Pages needed for every row (at least 1).</summary>
    private int PageCount()
    {
        int count = RowCount;
        return count == 0 ? 1 : Mathf.Max(1, Mathf.CeilToInt(count / (float)Mathf.Max(1, entriesPerPage)));
    }

    /// <summary>Replaces the row clones with the current page's rows.</summary>
    private void Rebuild()
    {
        foreach (GameObject r in _rows)
            if (r != null)
                Destroy(r);

        _rows.Clear();

        int count = RowCount;
        if (count == 0 || entryRowsRoot == null || entryRowTemplate == null)
            return;

        int per = Mathf.Max(1, entriesPerPage);
        int start = _page * per;
        int end = Mathf.Min(start + per, count);

        for (int i = start; i < end; i++)
        {
            GameObject row = Instantiate(entryRowTemplate, entryRowsRoot);
            row.SetActive(true);
            _rows.Add(row);
            FillRow(i, row, row.GetComponentsInChildren<TMP_Text>(true), row.GetComponent<Image>(), row.GetComponent<Button>());
        }
    }
}
```

Then replace the whole of `Assets/Scripts/UI/ReferenceBookWindowController.cs` (Write tool, LF):

```csharp
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders a reference book as a flippable window. Its rows are today's facts
/// for the book's category (FactTable.Rows), each a clickable "place : value"
/// row the player can compare against a traveller's statement. Paging and
/// row cloning are PagedRowsWindow's.
/// </summary>
public sealed class ReferenceBookWindowController : PagedRowsWindow
{
    private ReferenceBookSO _book;
    private FactTable _facts;
    private CompareController _compare;

    /// <summary>Binds a book cover to today's facts and renders its first page.</summary>
    public void SetBook(ReferenceBookSO book, FactTable facts, CompareController compare)
    {
        _book = book;
        _facts = facts;
        _compare = compare;

        SetTitle(book != null ? book.displayName : "Reference");
        ShowPage(0);
    }

    /// <summary>Today's rows for this book (empty when unbound).</summary>
    private IReadOnlyList<FactRow> Rows() =>
        _book != null && _facts != null ? _facts.Rows(_book.category) : System.Array.Empty<FactRow>();

    /// <inheritdoc />
    protected override int RowCount => Rows().Count;

    /// <summary>Shows a place and its value; a click puts the entry into the compare bar as a truth source.</summary>
    protected override void FillRow(int index, GameObject row, TMP_Text[] texts, Image background, Button button)
    {
        FactRow fact = Rows()[index];

        if (texts.Length > 0 && texts[0] != null)
            texts[0].text = fact.OriginLabel;
        if (texts.Length > 1 && texts[1] != null)
            texts[1].text = fact.Value;

        string label = $"{(_book != null ? _book.displayName : "Reference")}: {fact.OriginLabel}";
        string value = fact.Value;
        CompareEvidence evidence = fact.ToEvidence();

        if (button != null && _compare != null)
            button.onClick.AddListener(() => _compare.Select(label, value, background, evidence));
    }
}
```

- [ ] **Step 2: Compile check and run**

Expected: exit 0 / exit 0; `passed 336, failed 0`.

- [ ] **Step 3: Check the serialized names survive**

Run: `cd /e/unity/NOPE-feat-clock && grep -c "\[SerializeField" Assets/Scripts/UI/PagedRowsWindow.cs Assets/Scripts/UI/ReferenceBookWindowController.cs && grep -c "entryRowTemplate:" Assets/Scenes/OfficeScene.unity`
Expected: `PagedRowsWindow.cs:7`, `ReferenceBookWindowController.cs:0`, and `1` for the scene: the book template's serialized values (`titleText` … `entriesPerPage`) keep their names, and Unity maps a base class's private serialized fields by name, so the committed scene needs no change.

- [ ] **Step 4: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/UI/PagedRowsWindow.cs Assets/Scripts/UI/PagedRowsWindow.cs.meta Assets/Scripts/UI/ReferenceBookWindowController.cs && git commit -F - <<'EOF'
refactor(ui): shared paging for row windows (PagedRowsWindow)

The book window's footer paging and row cloning move into an abstract
base with the same serialized field names, so the book template keeps its
references and the interview transcript can page with the same code.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 12: Interview content assets and their validation

The content types the generator writes and the office reads (spec §2.7), the four hand-authored assets (§2.14: two books, the Interview Protocols upgrade, the rumour effect, with fixed GUIDs so the library can list the upgrade and the effect), and the validator's checks (§2.13): questions, dialogs, capacity, upgrade ids, tell channels, small talk. The validator owns the dialog-effect error text (`DialogEffectOpError`), which the generator reports verbatim in Task 13, and the most-documents rule (`MaxDocuments` over a blueprint list), which the generator calls with its own blueprints; the template-token rule is Domain's `Interview.HoldsToken` (Task 7), and the forced cases' blueprints come from the new read-only `DayPlanSO.ForcedBlueprints`, not from the day plan's serialized layout.

**Files:**
- Create: `Assets/Scripts/Dialog/QuestionSO.cs`, `Assets/Scripts/Dialog/DialogSO.cs` (+ `.meta`s, + `Assets/Scripts/Dialog.meta`)
- Create: `Assets/Data/Investigation/RefBook_Capital.asset`, `RefBook_Ruler.asset`, `Assets/Data/Upgrades/Upgrade_InterviewProtocols.asset`, `Assets/Data/Effects/Effect_Dialog_RumourHeard.asset` (+ `.meta`s, from the script)
- Modify: `Assets/Scripts/ContentLibrarySO.cs`, `Assets/Scripts/EraSO.cs`, `Assets/Scripts/Timeline/NationEraProfileSO.cs`, `Assets/Scripts/UpgradeSO.cs`, `Assets/Scripts/WorldState.cs`, `Assets/Scripts/DayPlanSO.cs` (`ForcedBlueprints`), `Assets/Editor/ContentLibraryValidator.cs`, `Assets/Data/Content Library/ContentLibrary_Main.asset`, `docs/FEATURES.md`

- [ ] **Step 1: The content asset types** — create the folder and write `Assets/Scripts/Dialog/QuestionSO.cs` (Write tool):

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One interview question, generated by Tools > TimeDesk > Generate World from
/// world_source.json "questions" (never edited by hand). Askable on a day when
/// all its conditions pass on the day-start snapshot (InterviewDay).
/// </summary>
public sealed class QuestionSO : ScriptableObject
{
    /// <summary>The question: its category, ask-menu label and wording, with stable line ids.</summary>
    public InterviewQuestion question = new();

    /// <summary>When it is askable: DayAtLeast fromDay (when fromDay &gt; 1) plus the authored conditions.</summary>
    public List<TriggerCondition> conditions = new();
}
```

and `Assets/Scripts/Dialog/DialogSO.cs` (Write tool):

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One narrative dialog, generated by Tools > TimeDesk > Generate World from
/// world_source.json "dialogs" (never edited by hand). Offered in the
/// interview hub on a day when its conditions pass on the day-start snapshot,
/// its structure is sound and, if one-shot, it is not done yet (InterviewDay).
/// </summary>
public sealed class DialogSO : ScriptableObject
{
    /// <summary>The dialog: its hub label, nodes, lines and choices, with stable ids.</summary>
    public AuthoredDialog dialog = new();

    /// <summary>When it is offered (the authored conditions).</summary>
    public List<TriggerCondition> conditions = new();
}
```

then make the metas: `python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/make_meta.py" 'E:\unity\NOPE-feat-clock\Assets\Scripts\Dialog' 'E:\unity\NOPE-feat-clock\Assets\Scripts\Dialog\QuestionSO.cs' 'E:\unity\NOPE-feat-clock\Assets\Scripts\Dialog\DialogSO.cs'`

Then save as `SCRATCH/p3_t12_types.py` and run (library fields, small talk, docs):

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'
A = W + r'\Assets\Scripts'

apply(A + r'\ContentLibrarySO.cs', [
("""    /// <summary>Reference books the player consults (one per clue category).</summary>
    [SerializeField] private ReferenceBookSO[] referenceBooks;
""",
"""    /// <summary>Reference books the player consults (one per clue category).</summary>
    [SerializeField] private ReferenceBookSO[] referenceBooks;

    [Header("Interview")]
    /// <summary>The interview's fixed wording and layout limits (written by Generate World from world_source.json "interview").</summary>
    [SerializeField] private InterviewLines interview = new();

    /// <summary>Interview questions, in ask-menu order (generated from world_source.json "questions").</summary>
    [SerializeField] private QuestionSO[] questions;

    /// <summary>Narrative dialogs (generated from world_source.json "dialogs").</summary>
    [SerializeField] private DialogSO[] dialogs;
"""),
("""    /// <summary>Categories that have a reference book (only these can carry a place-fact tell).</summary>""",
"""    /// <summary>Categories that have a reference book (only these can carry a place-fact tell, on papers or in an answer).</summary>"""),
("""    /// <summary>Public read-only access to timeline triggers.</summary>
    public IReadOnlyList<TimelineTriggerSO> Triggers => timelineTriggers ?? System.Array.Empty<TimelineTriggerSO>();
""",
"""    /// <summary>Public read-only access to timeline triggers.</summary>
    public IReadOnlyList<TimelineTriggerSO> Triggers => timelineTriggers ?? System.Array.Empty<TimelineTriggerSO>();

    /// <summary>The interview's fixed wording and layout limits.</summary>
    public InterviewLines Interview => interview;

    /// <summary>Public read-only access to interview questions (ask-menu order).</summary>
    public IReadOnlyList<QuestionSO> Questions => questions ?? System.Array.Empty<QuestionSO>();

    /// <summary>Public read-only access to narrative dialogs.</summary>
    public IReadOnlyList<DialogSO> Dialogs => dialogs ?? System.Array.Empty<DialogSO>();
"""),
])

apply(A + r'\EraSO.cs', [
("""// ReSharper disable InconsistentNaming
using UnityEngine;""",
"""// ReSharper disable InconsistentNaming
using System.Collections.Generic;
using UnityEngine;"""),
("""    /// <summary>Chronological position (0 = oldest); orders places in the books.</summary>
    public int order;""",
"""    /// <summary>Chronological position (0 = oldest); orders places in the books.</summary>
    public int order;

    /// <summary>Small-talk lines of travellers claiming this era (used when their place has none).</summary>
    public List<LineText> smallTalk = new();"""),
])

apply(A + r'\Timeline\NationEraProfileSO.cs', [
("""    /// <summary>Period-appropriate female given names.</summary>
    public string[] femaleNames;
""",
"""    /// <summary>Period-appropriate female given names.</summary>
    public string[] femaleNames;

    /// <summary>Small-talk lines of travellers claiming this place (flavour, never evidence).</summary>
    public List<LineText> smallTalk = new();
"""),
])

apply(A + r'\UpgradeSO.cs', [
("""/// Upgrade definition used for gating clue generation and for the Home shop (Phase 4).""",
"""/// Upgrade definition used for gating clue generation, interview questions
/// (UpgradeOwned) and the Home shop (Phase 4)."""),
])

apply(A + r'\WorldState.cs', [
("""    /// <summary>Unlocked upgrade IDs for gating clue generation and shop state.</summary>""",
"""    /// <summary>Unlocked upgrade IDs for gating clue generation, interview questions and shop state.</summary>"""),
])
```

- [ ] **Step 2: The validator** — save as `SCRATCH/p3_t12_validator.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Editor\ContentLibraryValidator.cs', [
("""/// Phase 6 editor tool: scans every ContentLibrarySO asset in the project and
/// reports data issues to the console — null array entries, duplicate or
/// missing IDs, duplicate day numbers, and dangling cross-references.""",
"""/// Phase 6 editor tool: scans every ContentLibrarySO asset in the project and
/// reports data issues to the console — null array entries, duplicate or
/// missing IDs, duplicate day numbers, dangling cross-references, and
/// interview content the office could not use (questions, dialogs, menus)."""),
("""        issues += CheckNullEntries(lib.Endings, "Endings", lib);
""",
"""        issues += CheckNullEntries(lib.Endings, "Endings", lib);
        issues += CheckNullEntries(lib.Questions, "Questions", lib);
        issues += CheckNullEntries(lib.Dialogs, "Dialogs", lib);
"""),
("""        issues += CheckDuplicateIds(Ids(lib.Profiles, p => p.id), "Profiles", lib);
""",
"""        issues += CheckDuplicateIds(Ids(lib.Profiles, p => p.id), "Profiles", lib);
        issues += CheckDuplicateIds(Ids(lib.Questions, q => q.question != null ? q.question.id : null), "Questions", lib);
        issues += CheckDuplicateIds(Ids(lib.Dialogs, d => d.dialog != null ? d.dialog.id : null), "Dialogs", lib);
"""),
("""        issues += CheckPlaces(lib);
        issues += CheckDayPlanPlaces(lib);

        return issues;
    }

    /// <summary>Fact categories every place must have (papers + books + planned questions).</summary>""",
"""        issues += CheckPlaces(lib);
        issues += CheckDayPlanPlaces(lib);

        // --- Interview (wording, questions, dialogs, menus), upgrade ids, tell channels, small talk ---
        issues += CheckInterview(lib);
        issues += CheckUpgradeIds(lib);
        issues += CheckTellChannels(lib);
        issues += CheckSmallTalk(lib);

        return issues;
    }

    /// <summary>The error for a dialog effect op that would act while active (Generate World reports the same text).</summary>
    public static string DialogEffectOpError(string dialogId, string choiceId, string effectName, EffectOpType op) =>
        $"Dialog '{dialogId}' choice '{choiceId}' names effect '{effectName}', whose {op} op would already act this evening and in a replay of the day; " +
        "dialog effects may only hold instant ops and briefing/news lines (timed modifiers from dialogs are piece 4/5 work)";

    /// <summary>
    /// Reports interview content the office could not use: blank wording or
    /// layout limits; a question whose answers could never be proven, a second
    /// question for one category, an answer template without {value}; a
    /// structurally broken dialog; a dialog effect that is missing or holds an
    /// op that acts while active (and a warning for a permanent one with a
    /// briefing or news line); menus fuller than the intercom shows.
    /// </summary>
    private static int CheckInterview(ContentLibrarySO lib)
    {
        int issues = 0;
        void Error(string message, UnityEngine.Object context)
        {
            Debug.LogError($"[ContentLibraryValidator] {message} ('{lib.name}')", context);
            issues++;
        }

        InterviewLines lines = lib.Interview ?? new InterviewLines();
        var wording = new (string field, string text)[]
        {
            ("deskName", lines.deskName), ("opener", lines.opener?.text), ("openerLegendary", lines.openerLegendary?.text),
            ("claim", lines.claim?.text), ("honorificMale", lines.honorificMale), ("honorificFemale", lines.honorificFemale),
            ("honorificUnknown", lines.honorificUnknown), ("requestLabel", lines.requestLabel), ("requestPrompt", lines.requestPrompt?.text),
            ("requestReply", lines.requestReply?.text), ("askLabel", lines.askLabel), ("backLabel", lines.backLabel),
            ("smallTalkLabel", lines.smallTalkLabel), ("smallTalkPrompt", lines.smallTalkPrompt?.text)
        };
        foreach ((string field, string text) in wording)
            if (string.IsNullOrWhiteSpace(text))
                Error($"Interview line '{field}' is blank (run Tools > TimeDesk > Generate World).", lib);

        if (lines.menuCapacity < 1)
            Error("Interview menu capacity is below 1 (run Tools > TimeDesk > Generate World).", lib);
        if (lines.maxLineChars < 1)
            Error("Interview longest line (maxLineChars) is below 1 (run Tools > TimeDesk > Generate World).", lib);

        HashSet<ClueCategory> books = lib.ReferenceBookCategories();
        var asked = new HashSet<ClueCategory>();
        foreach (QuestionSO q in lib.Questions)
        {
            if (q == null || q.question == null)
                continue;

            InterviewQuestion question = q.question;
            if (!Forgery.IsProvableCategory(question.category, books))
                Error($"Question '{question.id}' asks about {question.category}: answers in this category can never be proven (no reference book covers it, and it is not a birth date).", q);
            if (!asked.Add(question.category))
                Error($"Question '{question.id}' asks about {question.category} again (one question per category).", q);
            if (!Interview.HoldsToken(question.answer?.text, Interview.ValueToken))
                Error($"Question '{question.id}': its answer template must hold {{value}}.", q);
            foreach (WordingOverride o in question.overrides ?? new List<WordingOverride>())
                if (o != null && !Interview.HoldsToken(o.answer?.text, Interview.ValueToken))
                    Error($"Question '{question.id}' override '{o.eraId}': its answer template must hold {{value}}.", q);
        }

        foreach (DialogSO d in lib.Dialogs)
        {
            if (d == null || d.dialog == null)
                continue;

            AuthoredDialog dialog = d.dialog;
            foreach (string problem in DialogChecks.Problems(dialog, lines.menuCapacity))
                Error($"Dialog '{dialog.id}': {problem}.", d);

            foreach (ScriptNode node in dialog.nodes ?? new List<ScriptNode>())
            {
                foreach (ScriptChoice choice in node?.choices ?? new List<ScriptChoice>())
                {
                    if (choice == null || string.IsNullOrWhiteSpace(choice.effect))
                        continue;

                    EffectSO fx = lib.GetEffectByAssetName(choice.effect);
                    if (fx == null)
                    {
                        Error($"Dialog '{dialog.id}' choice '{choice.id}' names effect '{choice.effect}', which the library does not list; add it to the library's effects.", d);
                        continue;
                    }

                    foreach (EffectOp op in fx.ops)
                        if (op != null && EffectOps.ActsWhileActive(op.type))
                            Error(DialogEffectOpError(dialog.id, choice.id, choice.effect, op.type) + ".", d);

                    if (fx.defaultDurationDays < 0 && fx.ops.Any(op => op != null && (op.type == EffectOpType.BriefingLine || op.type == EffectOpType.NewsLine)))
                    {
                        Debug.LogWarning($"[ContentLibraryValidator] Dialog '{dialog.id}' choice '{choice.id}' names effect '{choice.effect}', which is permanent and carries a briefing or news line: the line would repeat every morning ('{lib.name}').", d);
                        issues++;
                    }
                }
            }
        }

        bool smallTalk = (lib.Eras ?? Array.Empty<EraSO>()).Any(e => e != null && e.smallTalk != null && e.smallTalk.Count > 0) ||
                         lib.Profiles.Any(p => p != null && p.smallTalk != null && p.smallTalk.Count > 0);
        foreach (string problem in DialogChecks.MenuProblems(lib.Questions.Count(q => q != null), smallTalk, MaxDocuments(TravellerBlueprints(lib)),
                                                             lib.Dialogs.Count(d => d != null), lines.menuCapacity))
            Error(problem, lib);

        return issues;
    }

    /// <summary>
    /// The most documents one traveller carries among these blueprints (a
    /// request each; null blueprints and templates are skipped). Generate
    /// World counts its source's blueprints with the same rule.
    /// </summary>
    public static int MaxDocuments(IEnumerable<CaseBlueprintSO> blueprints) =>
        blueprints.Where(b => b != null && b.DocumentTemplates != null)
                  .Select(b => b.DocumentTemplates.Count(t => t != null))
                  .DefaultIfEmpty(0)
                  .Max();

    /// <summary>Every blueprint a traveller can come from: the day plans' possible and forced ones and the legendaries' overrides (nulls included).</summary>
    private static IEnumerable<CaseBlueprintSO> TravellerBlueprints(ContentLibrarySO lib)
    {
        foreach (DayPlanSO plan in lib.DayPlans)
        {
            if (plan == null)
                continue;

            if (plan.PossibleBlueprints != null)
                foreach (CaseBlueprintSO blueprint in plan.PossibleBlueprints)
                    yield return blueprint;

            foreach (CaseBlueprintSO blueprint in plan.ForcedBlueprints)
                yield return blueprint;
        }

        foreach (LegendarySO legend in lib.Legendaries)
            if (legend != null)
                yield return legend.blueprintOverride;
    }

    /// <summary>
    /// Reports every upgrade id that names no library upgrade: UpgradeOwned
    /// keys in questions, dialogs and triggers, and UnlockUpgrade or
    /// (non-empty) ShopDiscountPercent parameters in effects.
    /// </summary>
    private static int CheckUpgradeIds(ContentLibrarySO lib)
    {
        int issues = 0;
        void Check(string upgradeId, string owner, UnityEngine.Object context)
        {
            if (lib.GetUpgradeById(upgradeId) != null)
                return;

            Debug.LogError($"[ContentLibraryValidator] {owner} names unknown upgrade '{upgradeId}' (not in '{lib.name}' upgrades).", context);
            issues++;
        }

        void CheckConditions(IEnumerable<TriggerCondition> conditions, string owner, UnityEngine.Object context)
        {
            foreach (TriggerCondition c in conditions ?? Array.Empty<TriggerCondition>())
                if (c != null && c.type == TriggerConditionType.UpgradeOwned)
                    Check(c.key, owner, context);
        }

        foreach (QuestionSO q in lib.Questions)
            if (q != null)
                CheckConditions(q.conditions, $"Question '{q.name}'", q);
        foreach (DialogSO d in lib.Dialogs)
            if (d != null)
                CheckConditions(d.conditions, $"Dialog '{d.name}'", d);
        foreach (TimelineTriggerSO t in lib.Triggers)
            if (t != null)
                CheckConditions(t.conditions, $"Trigger '{t.name}'", t);

        foreach (EffectSO fx in lib.Effects)
        {
            if (fx == null)
                continue;

            foreach (EffectOp op in fx.ops)
                if (op != null && (op.type == EffectOpType.UnlockUpgrade || (op.type == EffectOpType.ShopDiscountPercent && !string.IsNullOrEmpty(op.stringParam))))
                    Check(op.stringParam, $"Effect '{fx.name}' ({op.type})", fx);
        }

        return issues;
    }

    /// <summary>Reports day plans with no tell channel (their liars could leak nothing).</summary>
    private static int CheckTellChannels(ContentLibrarySO lib)
    {
        int issues = 0;
        foreach (DayPlanSO plan in lib.DayPlans)
        {
            if (plan != null && plan.TellChannels.Count == 0)
            {
                Debug.LogError($"[ContentLibraryValidator] Day plan '{plan.name}' has no tell channel, so its liars can leak no tell (world_source.json days[].channels) in '{lib.name}'.", plan);
                issues++;
            }
        }

        return issues;
    }

    /// <summary>Warns about an era a day plan uses that has no small talk while some of its places that day have none either.</summary>
    private static int CheckSmallTalk(ContentLibrarySO lib)
    {
        int issues = 0;
        var warned = new HashSet<EraSO>();
        foreach (DayPlanSO plan in lib.DayPlans)
        {
            if (plan == null)
                continue;

            List<NationEraProfileSO> today = lib.TodaysProfiles(plan);
            foreach (EraWeight w in plan.EraWeights ?? Array.Empty<EraWeight>())
            {
                if (w.era == null || w.weight <= 0f || (w.era.smallTalk != null && w.era.smallTalk.Count > 0) || warned.Contains(w.era))
                    continue;

                if (today.Any(p => p.era == w.era && (p.smallTalk == null || p.smallTalk.Count == 0)))
                {
                    Debug.LogWarning($"[ContentLibraryValidator] Era '{w.era.id}' (day plan '{plan.name}') has no small talk, and some of its places have none either; their travellers have nothing to say when asked in '{lib.name}'.", w.era);
                    warned.Add(w.era);
                    issues++;
                }
            }
        }

        return issues;
    }

    /// <summary>Fact categories every place must have (papers + books + questions).</summary>"""),
("""                    Debug.LogError($"[ContentLibraryValidator] Place '{place.name}' has no {category} fact in '{lib.name}' (papers would print a placeholder).", place);""",
"""                    Debug.LogError($"[ContentLibraryValidator] Place '{place.name}' has no {category} fact in '{lib.name}' (papers and answers would use a placeholder).", place);"""),
])

# The forced cases' blueprints, read through DayPlanSO itself (not its serialized layout).
apply(W + r'\Assets\Scripts\DayPlanSO.cs', [
("""    /// <summary>Whether each active rule is guaranteed a violator in the first half of the queue.</summary>
    public bool GuaranteeRuleViolators => guaranteeRuleViolators;
""",
"""    /// <summary>Whether each active rule is guaranteed a violator in the first half of the queue.</summary>
    public bool GuaranteeRuleViolators => guaranteeRuleViolators;

    /// <summary>Every forced case's blueprint (set slots only, in authored order); the content validator counts their documents.</summary>
    public IEnumerable<CaseBlueprintSO> ForcedBlueprints
    {
        get
        {
            foreach (ForcedCaseSlot slot in forcedCases)
                if (slot != null && slot.caseBlueprint != null)
                    yield return slot.caseBlueprint;
        }
    }
"""),
])
```

- [ ] **Step 3: The hand-authored assets** — first check the four GUIDs are free: `cd /e/unity/NOPE-feat-clock && grep -rl "f4a8944939c8454f90f50df9a7898f2c\|2fb3a1caac37490aac9f1df4ff62901a\|1c15365f5ee847309cf3bf46489df055\|8400fe10969c4ecc8639da5d1a9e8484" Assets | head` (expected: no output). Then save as `SCRATCH/p3_t12_assets.py` and run:

```python
import pathlib, sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'
D = pathlib.Path(W) / 'Assets' / 'Data'

HEADER = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
"""

# Unity's canonical meta (as make_meta.py writes it): "userData: " and the two
# bundle keys keep their trailing space, so Unity has nothing to re-save.
META = ("fileFormatVersion: 2\n"
        "guid: {guid}\n"
        "NativeFormatImporter:\n"
        "  externalObjects: {{}}\n"
        "  mainObjectFileID: 11400000\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n")

def write(rel, guid, body):
    path = D / rel
    if path.exists() or pathlib.Path(str(path) + '.meta').exists():
        sys.exit(f'{path} already exists')
    path.write_text(HEADER + body, encoding='utf-8', newline='\n')
    pathlib.Path(str(path) + '.meta').write_text(META.format(guid=guid), encoding='utf-8', newline='\n')
    print('wrote', path)

# Script guids: ReferenceBookSO a409d798..., UpgradeSO 633ce146..., EffectSO 5c0cc60a...
write('Investigation/RefBook_Capital.asset', 'f4a8944939c8454f90f50df9a7898f2c', """  m_Script: {fileID: 11500000, guid: a409d79898b69804089f9044ace25331, type: 3}
  m_Name: RefBook_Capital
  m_EditorClassIdentifier: Assembly-CSharp::ReferenceBookSO
  displayName: Capitals Gazetteer
  category: 5
""")

write('Investigation/RefBook_Ruler.asset', '2fb3a1caac37490aac9f1df4ff62901a', """  m_Script: {fileID: 11500000, guid: a409d79898b69804089f9044ace25331, type: 3}
  m_Name: RefBook_Ruler
  m_EditorClassIdentifier: Assembly-CSharp::ReferenceBookSO
  displayName: Rulers & Regents
  category: 2
""")

write('Upgrades/Upgrade_InterviewProtocols.asset', '1c15365f5ee847309cf3bf46489df055', """  m_Script: {fileID: 11500000, guid: 633ce1469e2ac064aa273d138f70aebc, type: 3}
  m_Name: Upgrade_InterviewProtocols
  m_EditorClassIdentifier: Assembly-CSharp::UpgradeSO
  id: interview_protocols
  displayName: Interview Protocols
  description: Clearance to ask travellers when they were born.
  cost: 120
  unlockEffect: {fileID: 0}
""")

write('Effects/Effect_Dialog_RumourHeard.asset', '8400fe10969c4ecc8639da5d1a9e8484', """  m_Script: {fileID: 11500000, guid: 5c0cc60a29baede43870d511f5fc52e4, type: 3}
  m_Name: Effect_Dialog_RumourHeard
  m_EditorClassIdentifier: Assembly-CSharp::EffectSO
  displayName: 'Rumour: calculators'
  channel: 0
  defaultDurationDays: 1
  ops:
  - type: 0
    stringParam: rumour_calculators
    floatParam: 0
    attribute: {fileID: 0}
    nation: {fileID: 0}
    profile: {fileID: 0}
  - type: 15
    stringParam: 'A traveller''s tip: someone is smuggling pocket calculators into the past. Keep your eyes open.'
    floatParam: 0
    attribute: {fileID: 0}
    nation: {fileID: 0}
    profile: {fileID: 0}
""")

# The library lists the new upgrade and the dialog's effect (Generate World writes the rest).
apply(str(D / 'Content Library' / 'ContentLibrary_Main.asset'), [
("""  - {fileID: 11400000, guid: d84dddd24193a8849840cf0d983a4c4f, type: 2}
  upgrades:""",
"""  - {fileID: 11400000, guid: d84dddd24193a8849840cf0d983a4c4f, type: 2}
  - {fileID: 11400000, guid: 8400fe10969c4ecc8639da5d1a9e8484, type: 2}
  upgrades:"""),
("""  - {fileID: 11400000, guid: f163b89b65df0ff4bb129a518b6ee741, type: 2}
  attributes:""",
"""  - {fileID: 11400000, guid: f163b89b65df0ff4bb129a518b6ee741, type: 2}
  - {fileID: 11400000, guid: 1c15365f5ee847309cf3bf46489df055, type: 2}
  attributes:"""),
])
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 336, failed 0`.

- [ ] **Step 5: Update the behaviour contract** — save as `SCRATCH/p3_t12_features.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\docs\FEATURES.md', [
("""- [ ] Content validator checks every place (five facts, names, birth years set) and every day plan (today has places; every weighted era has one; every rule can be broken; listed legendaries come from today's places)""",
"""- [ ] Content validator checks every place (five facts, names, birth years set) and every day plan (today has places; every weighted era has one; every rule can be broken; listed legendaries come from today's places; at least one tell channel); questions (a book or the Citizen Record proves every question's category; one question per category; answer templates hold `{value}`), dialogs (structure, reachable endings, effects and their op types, one-shot), the interview's wording and menu capacity, every upgrade id, and small talk for the eras day plans use"""),
])
```

- [ ] **Step 6: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Dialog.meta Assets/Scripts/Dialog/QuestionSO.cs Assets/Scripts/Dialog/QuestionSO.cs.meta Assets/Scripts/Dialog/DialogSO.cs Assets/Scripts/Dialog/DialogSO.cs.meta Assets/Scripts/ContentLibrarySO.cs Assets/Scripts/EraSO.cs Assets/Scripts/Timeline/NationEraProfileSO.cs Assets/Scripts/UpgradeSO.cs Assets/Scripts/WorldState.cs Assets/Scripts/DayPlanSO.cs Assets/Editor/ContentLibraryValidator.cs Assets/Data/Investigation/RefBook_Capital.asset Assets/Data/Investigation/RefBook_Capital.asset.meta Assets/Data/Investigation/RefBook_Ruler.asset Assets/Data/Investigation/RefBook_Ruler.asset.meta Assets/Data/Upgrades/Upgrade_InterviewProtocols.asset Assets/Data/Upgrades/Upgrade_InterviewProtocols.asset.meta Assets/Data/Effects/Effect_Dialog_RumourHeard.asset Assets/Data/Effects/Effect_Dialog_RumourHeard.asset.meta "Assets/Data/Content Library/ContentLibrary_Main.asset" docs/FEATURES.md && git commit -F - <<'EOF'
feat(content): interview content assets and their validation

QuestionSO and DialogSO hold generated interview content; the library
gains the interview's wording, questions and dialogs; eras and places gain
small talk. New hand-authored content: the Capitals Gazetteer and Rulers &
Regents books, the Interview Protocols upgrade and the rumour effect. The
validator checks questions (provable, one per category), dialogs
(structure, effects, one-shot), menu capacity, upgrade ids, tell channels
and small talk.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 13: Generate World writes the interview

The source gains the interview sections, the two books, small talk and each day's tell channels (spec §2.13–2.14); the generator checks them before writing anything (`CheckInterview`: tokens, provable categories, gates and announcements, dialog structure and effects, one global id set, ASCII, worst-case line lengths, menus) and writes questions, dialogs and one unlock-announcement trigger per gated question into the owned `Assets/Data/World/Interview` folder, rewiring the library's triggers (hand-authored ones kept). Five books need a second row of book windows (R21), placed here. Generated assets are produced in Unity in Task 17.

**Files:**
- Modify: `Assets/Editor/WorldContentGenerator.cs`, `Assets/Data/World/world_source.json`, `Assets/Scripts/DayPlanSO.cs` (doc), `Assets/Scripts/Timeline/TimelineTriggerSO.cs` (doc), `Assets/Scripts/UI/InvestigationUIController.cs` (book placement), `docs/FEATURES.md`

- [ ] **Step 1: The generator** — save as `SCRATCH/p3_t13_generator.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Editor\WorldContentGenerator.cs', [
# --- class doc ---
("""/// <summary>
/// Tools > TimeDesk > Generate World. The one authoritative world generator:
/// reads the researched world (Assets/Data/World/world_source.json) and
/// creates or updates the eras, nations, places (NationEraProfileSO with facts,
/// names and birth years), travel rules and day plans, points the case
/// blueprint at the listed archetypes, then sets every world array of the
/// content library explicitly. Idempotent: re-running converges to the source
/// file. It owns the Eras/Nations/Places/Rules folders under Assets/Data/World
/// (assets there that the source no longer lists go to the OS trash) and only
/// drops missing references elsewhere, so hand-authored content (legendaries,
/// effects, triggers) survives a re-run. The authored assets the source points
/// at (library, blueprint, attributes, archetypes, books) must already exist;
/// every reference is checked before anything is written.
/// </summary>""",
"""/// <summary>
/// Tools > TimeDesk > Generate World. The one authoritative world generator:
/// reads the hand-maintained world source (Assets/Data/World/world_source.json)
/// and creates or updates the eras, nations, places (NationEraProfileSO with
/// facts, names, birth years and small talk), travel rules and day plans (tell
/// count and tell channels), the interview (its wording and layout limits,
/// questions, narrative dialogs, and a one-shot unlock-announcement trigger
/// for every gated question), points the case blueprint at the listed
/// archetypes, then sets every world array of the content library
/// explicitly. Idempotent: re-running converges to the source file. It owns
/// the Eras/Nations/Places/Rules/Interview folders under Assets/Data/World
/// (assets there that the source no longer lists go to the OS trash) and only
/// drops missing references elsewhere, so hand-authored content (legendaries,
/// effects, triggers) survives a re-run. The authored assets the source points
/// at (library, blueprint, attributes, archetypes, books) must already exist;
/// every reference, id and line is checked before anything is written.
/// </summary>"""),
("""    private static readonly string[] OwnedFolders = { "Eras", "Nations", "Places", "Rules" };""",
"""    private static readonly string[] OwnedFolders = { "Eras", "Nations", "Places", "Rules", "Interview" };

    /// <summary>Folder of the generated interview assets (questions, dialogs, unlock triggers).</summary>
    private const string InterviewFolder = WorldRoot + "/Interview";"""),
# --- Generate ---
("""        CheckReferences(src, authored, errors);
        if (errors.Count > 0)""",
"""        CheckReferences(src, authored, errors);
        CheckInterview(src, authored, errors);
        if (errors.Count > 0)"""),
("""        DayPlanSO[] days = src.days.Select(d => MakeDay(d, src.content.dayPlanFolder, authored.blueprint, eras, nations, rules)).ToArray();
""",
"""        DayPlanSO[] days = src.days.Select(d => MakeDay(d, src.content.dayPlanFolder, authored.blueprint, eras, nations, rules)).ToArray();

        // --- Interview: questions, dialogs, unlock announcements ---
        QuestionData[] questionData = src.questions ?? Array.Empty<QuestionData>();
        QuestionSO[] questions = questionData.Select(q => MakeQuestion(q, written)).ToArray();
        DialogSO[] dialogs = (src.dialogs ?? Array.Empty<DialogData>()).Select(d => MakeDialog(d, written)).ToArray();
        TimelineTriggerSO[] unlocks = questionData.Where(IsGated).Select(q => MakeUnlockTrigger(q, written)).ToArray();
"""),
("""        WireLibrary(authored.library, days, src.eras.Select(e => eras[e.id]).ToArray(), src.countries.Select(c => nations[c.id]).ToArray(),
                    places, authored.archetypes, src.content.attributes.Select(a => authored.attributes[a.id]).ToArray(), authored.books);""",
"""        WireLibrary(authored.library, days, src.eras.Select(e => eras[e.id]).ToArray(), src.countries.Select(c => nations[c.id]).ToArray(),
                    places, authored.archetypes, src.content.attributes.Select(a => authored.attributes[a.id]).ToArray(), authored.books,
                    BuildLines(src.interview), questions, dialogs, unlocks);"""),
("""        Debug.Log($"[WorldContentGenerator] World generated: {eras.Count} eras, {nations.Count} nations, {places.Length} places, {rules.Count} rules, {days.Length} day plans; {pruned} unlisted generated asset(s) moved to the trash.");""",
"""        Debug.Log($"[WorldContentGenerator] World generated: {eras.Count} eras, {nations.Count} nations, {places.Length} places, {rules.Count} rules, {days.Length} day plans, {questions.Length} questions, {dialogs.Length} dialogs, {unlocks.Length} unlock triggers; {pruned} unlisted generated asset(s) moved to the trash.");"""),
# --- builders: small talk, channels ---
("""        era.order = e.order;
        EditorUtility.SetDirty(era);""",
"""        era.order = e.order;
        era.smallTalk = SmallTalk(e.id, e.smallTalk);
        EditorUtility.SetDirty(era);"""),
("""        place.birthYearMin = p.year - ageMax;
        place.birthYearMax = p.year - ageMin;
""",
"""        (place.birthYearMin, place.birthYearMax) = BirthYears(p, ageMin, ageMax);
"""),
("""        place.femaleNames = p.femaleNames ?? Array.Empty<string>();
""",
"""        place.femaleNames = p.femaleNames ?? Array.Empty<string>();
        place.smallTalk = SmallTalk(place.id, p.smallTalk);
"""),
("""    /// <summary>
    /// Writes the day's queue, tell count, eras, countries and rules. Legendary
    /// settings are left to their authors (missing legendaries are dropped).
    /// </summary>""",
"""    /// <summary>
    /// Writes the day's queue, tell count, tell channels, eras, countries and
    /// rules. Legendary settings are left to their authors (missing legendaries
    /// are dropped).
    /// </summary>"""),
("""        so.FindProperty("tellCount").intValue = d.tells;
""",
"""        so.FindProperty("tellCount").intValue = d.tells;
        string[] channels = d.channels ?? Array.Empty<string>();
        SerializedProperty tellChannels = so.FindProperty("tellChannels");
        tellChannels.arraySize = channels.Length;
        for (int i = 0; i < channels.Length; i++)
            tellChannels.GetArrayElementAtIndex(i).enumValueIndex = (int)(TellChannel)Enum.Parse(typeof(TellChannel), channels[i]);
"""),
# --- WireLibrary ---
("""    /// <summary>Sets every world array of the library (authoritative) and drops missing references from the rest.</summary>
    private static void WireLibrary(ContentLibrarySO lib, DayPlanSO[] days, EraSO[] eras, NationSO[] nations, NationEraProfileSO[] places,
                                    ArchetypeSO[] archetypes, AttributeSO[] attributes, ReferenceBookSO[] books)
    {""",
"""    /// <summary>
    /// Sets every world array of the library and the interview (authoritative),
    /// rewires the triggers (the hand-authored ones kept in order, then the
    /// generated unlock triggers) and drops missing references from the rest.
    /// </summary>
    private static void WireLibrary(ContentLibrarySO lib, DayPlanSO[] days, EraSO[] eras, NationSO[] nations, NationEraProfileSO[] places,
                                    ArchetypeSO[] archetypes, AttributeSO[] attributes, ReferenceBookSO[] books,
                                    InterviewLines interview, QuestionSO[] questions, DialogSO[] dialogs, TimelineTriggerSO[] unlocks)
    {"""),
("""        SetArray(so, "referenceBooks", books);
        DropMissing(so, "clues");
        DropMissing(so, "legendaries");
        DropMissing(so, "effects");
        DropMissing(so, "timelineTriggers");""",
"""        SetArray(so, "referenceBooks", books);
        so.FindProperty("interview").boxedValue = interview;
        SetArray(so, "questions", questions);
        SetArray(so, "dialogs", dialogs);
        SetArray(so, "timelineTriggers", AuthoredTriggers(so).Concat(unlocks).ToArray());
        DropMissing(so, "clues");
        DropMissing(so, "legendaries");
        DropMissing(so, "effects");"""),
# --- helpers + source shape ---
("""    private static void EnsureFolder(string path)
    {""",
"""    /// <summary>The library's current triggers that are not generated here (non-null, outside the Interview folder), in order.</summary>
    private static List<Object> AuthoredTriggers(SerializedObject so)
    {
        var kept = new List<Object>();
        SerializedProperty p = so.FindProperty("timelineTriggers");
        for (int i = 0; i < p.arraySize; i++)
        {
            Object o = p.GetArrayElementAtIndex(i).objectReferenceValue;
            if (o != null && !AssetDatabase.GetAssetPath(o).StartsWith(InterviewFolder + "/"))
                kept.Add(o);
        }
        return kept;
    }

    private static void EnsureFolder(string path)
    {"""),
("""    [Serializable] private sealed class WorldSource
    {
        public int travellerAgeMin = 18;
        public int travellerAgeMax = 70;
        public ContentData content;
        public EraData[] eras;
        public CountryData[] countries;
        public PlaceData[] places;
        public RuleData[] rules;
        public DayData[] days;
    }""",
"""    [Serializable] private sealed class WorldSource
    {
        public int travellerAgeMin = 18;
        public int travellerAgeMax = 70;
        public ContentData content;
        public EraData[] eras;
        public CountryData[] countries;
        public PlaceData[] places;
        public RuleData[] rules;
        public DayData[] days;
        public InterviewData interview;
        public QuestionData[] questions;
        public DialogData[] dialogs;
    }"""),
("""    [Serializable] private sealed class EraData { public string id; public string displayName; public int order; }""",
"""    [Serializable] private sealed class EraData { public string id; public string displayName; public int order; public string[] smallTalk; }"""),
("""        public string[] maleNames;
        public string[] femaleNames;
    }""",
"""        public string[] maleNames;
        public string[] femaleNames;
        public string[] smallTalk;
    }"""),
("""        /// <summary>Tells each liar leaks this day (at least 1).</summary>
        public int tells;
        public EraWeightData[] eras;
        public string[] countries;
        public string[] rules;
    }
}""",
"""        /// <summary>Tells each liar leaks this day (at least 1).</summary>
        public int tells;
        /// <summary>Where this day's tells may show ("Papers", "Answer").</summary>
        public string[] channels;
        public EraWeightData[] eras;
        public string[] countries;
        public string[] rules;
    }

    /// <summary>The interview's wording (plain strings; ids are generated) and its two layout limits.</summary>
    [Serializable] private sealed class InterviewData
    {
        public string deskName;
        public string opener;
        public string openerLegendary;
        public string claim;
        public string honorificMale;
        public string honorificFemale;
        public string honorificUnknown;
        public string requestLabel;
        public string requestPrompt;
        public string requestReply;
        public string askLabel;
        public string backLabel;
        public string smallTalkLabel;
        public string smallTalkPrompt;
        public int menuCapacity;
        public int maxLineChars;
    }

    /// <summary>A question; fromDay is required (0 = missing), announce is required exactly when the question is gated.</summary>
    [Serializable] private sealed class QuestionData
    {
        public string id;
        public string category;
        public string label;
        public string prompt;
        public string answer;
        public int fromDay;
        public string announce;
        public ConditionData[] conditions;
        public OverrideData[] overrides;
    }

    [Serializable] private sealed class OverrideData { public string era; public string prompt; public string answer; }

    [Serializable] private sealed class ConditionData { public string type; public string key; public float threshold; }

    /// <summary>A narrative dialog; one-shot unless "repeatable" is true.</summary>
    [Serializable] private sealed class DialogData
    {
        public string id;
        public string label;
        public bool repeatable;
        public ConditionData[] conditions;
        public NodeData[] nodes;
    }

    [Serializable] private sealed class NodeData { public string id; public LineData[] lines; public ChoiceData[] choices; }

    [Serializable] private sealed class LineData { public string id; public string speaker; public string text; }

    [Serializable] private sealed class ChoiceData { public string id; public string label; public LineData[] lines; public string next; public string effect; }
}"""),
])

# The interview builders and checks sit between the day-plan builder and WireLibrary.
apply(W + r'\Assets\Editor\WorldContentGenerator.cs', [
("""    /// <summary>
    /// Sets every world array of the library and the interview (authoritative),""",
"""    /// <summary>Small-talk lines with their generated ids ("{ownerId}.smalltalk.{n}", 1-based).</summary>
    private static List<LineText> SmallTalk(string ownerId, string[] lines) =>
        (lines ?? Array.Empty<string>()).Select((text, i) => new LineText(SmallTalkId(ownerId, i), text)).ToList();

    /// <summary>The id of an era's or place's small-talk line.</summary>
    private static string SmallTalkId(string ownerId, int index) => $"{ownerId}.smalltalk.{index + 1}";

    /// <summary>The id of an interview line ("interview.opener", ...).</summary>
    private static string InterviewLineId(string field) => $"interview.{field}";

    /// <summary>The interview's wording with generated line ids, and its layout limits.</summary>
    private static InterviewLines BuildLines(InterviewData i) => new InterviewLines
    {
        deskName = i.deskName,
        opener = new LineText(InterviewLineId("opener"), i.opener),
        openerLegendary = new LineText(InterviewLineId("openerLegendary"), i.openerLegendary),
        claim = new LineText(InterviewLineId("claim"), i.claim),
        honorificMale = i.honorificMale,
        honorificFemale = i.honorificFemale,
        honorificUnknown = i.honorificUnknown,
        requestLabel = i.requestLabel,
        requestPrompt = new LineText(InterviewLineId("requestPrompt"), i.requestPrompt),
        requestReply = new LineText(InterviewLineId("requestReply"), i.requestReply),
        askLabel = i.askLabel,
        backLabel = i.backLabel,
        smallTalkLabel = i.smallTalkLabel,
        smallTalkPrompt = new LineText(InterviewLineId("smallTalkPrompt"), i.smallTalkPrompt),
        menuCapacity = i.menuCapacity,
        maxLineChars = i.maxLineChars
    };

    /// <summary>A question with generated line ids ("{id}.prompt", "{id}.{era}.answer", ...).</summary>
    private static InterviewQuestion BuildQuestion(QuestionData q) => new InterviewQuestion
    {
        id = q.id,
        category = (ClueCategory)Enum.Parse(typeof(ClueCategory), q.category),
        label = q.label,
        prompt = new LineText($"{q.id}.prompt", q.prompt),
        answer = new LineText($"{q.id}.answer", q.answer),
        overrides = (q.overrides ?? Array.Empty<OverrideData>()).Select(o => new WordingOverride
        {
            eraId = o.era,
            prompt = new LineText($"{q.id}.{o.era}.prompt", o.prompt),
            answer = new LineText($"{q.id}.{o.era}.answer", o.answer)
        }).ToList()
    };

    /// <summary>A dialog as the runner's data contract; one-shot unless repeatable. Never throws (an unknown speaker reads Traveller; the checks report it).</summary>
    private static AuthoredDialog BuildDialog(DialogData d) => new AuthoredDialog
    {
        id = d.id,
        label = d.label,
        oneShot = !d.repeatable,
        nodes = (d.nodes ?? Array.Empty<NodeData>()).Select(n => new ScriptNode
        {
            id = n.id,
            lines = ScriptLines(n.lines),
            choices = (n.choices ?? Array.Empty<ChoiceData>()).Select(c => new ScriptChoice
            {
                id = c.id,
                label = c.label,
                lines = ScriptLines(c.lines),
                next = c.next ?? string.Empty,
                effect = c.effect ?? string.Empty
            }).ToList()
        }).ToList()
    };

    private static List<ScriptLine> ScriptLines(LineData[] lines) =>
        (lines ?? Array.Empty<LineData>()).Select(l => new ScriptLine
        {
            id = l.id,
            speaker = ParseEnum(l.speaker, out DialogSpeaker speaker) ? speaker : DialogSpeaker.Traveller,
            text = l.text
        }).ToList();

    /// <summary>A day gate at <paramref name="threshold"/> when the question starts after day 1, else nothing.</summary>
    private static IEnumerable<TriggerCondition> DayGate(int fromDay, int threshold) =>
        fromDay > 1
            ? new[] { new TriggerCondition { type = TriggerConditionType.DayAtLeast, threshold = threshold } }
            : Array.Empty<TriggerCondition>();

    /// <summary>The authored conditions as trigger conditions.</summary>
    private static IEnumerable<TriggerCondition> Conditions(ConditionData[] conditions) =>
        (conditions ?? Array.Empty<ConditionData>()).Select(c => new TriggerCondition
        {
            type = (TriggerConditionType)Enum.Parse(typeof(TriggerConditionType), c.type),
            key = c.key,
            threshold = c.threshold
        });

    /// <summary>True when a question is gated (fromDay above 1 or any authored condition), so an unlock trigger announces it.</summary>
    private static bool IsGated(QuestionData q) => q.fromDay > 1 || (q.conditions != null && q.conditions.Length > 0);

    /// <summary>Writes Interview/Question_{id}.asset: the question and its day-start conditions (DayAtLeast fromDay when fromDay > 1, plus the authored ones).</summary>
    private static QuestionSO MakeQuestion(QuestionData q, HashSet<string> written)
    {
        QuestionSO so = LoadOrCreate<QuestionSO>($"{InterviewFolder}/Question_{q.id}.asset", written);
        so.question = BuildQuestion(q);
        so.conditions = DayGate(q.fromDay, q.fromDay).Concat(Conditions(q.conditions)).ToList();
        EditorUtility.SetDirty(so);
        return so;
    }

    /// <summary>Writes Interview/Dialog_{id}.asset: the dialog and its conditions.</summary>
    private static DialogSO MakeDialog(DialogData d, HashSet<string> written)
    {
        DialogSO so = LoadOrCreate<DialogSO>($"{InterviewFolder}/Dialog_{d.id}.asset", written);
        so.dialog = BuildDialog(d);
        so.conditions = Conditions(d.conditions).ToList();
        EditorUtility.SetDirty(so);
        return so;
    }

    /// <summary>
    /// Writes Interview/Trigger_Unlock_{questionId}.asset for a gated question:
    /// a one-shot trigger whose news line is the question's announcement. It
    /// fires the night before the first day the question is askable
    /// (DayAtLeast Gates.UnlockNight(fromDay) when fromDay > 1) and when the
    /// question's own conditions hold.
    /// </summary>
    private static TimelineTriggerSO MakeUnlockTrigger(QuestionData q, HashSet<string> written)
    {
        TimelineTriggerSO t = LoadOrCreate<TimelineTriggerSO>($"{InterviewFolder}/Trigger_Unlock_{q.id}.asset", written);
        t.id = $"unlock_{q.id}";
        t.displayName = $"Unlock: {q.label}";
        t.description = $"Generated by Generate World: announces the {q.label} question in the morning paper of the first day it can be asked.";
        t.oneShot = true;
        t.newsLineOnFire = q.announce;
        t.conditions = DayGate(q.fromDay, Gates.UnlockNight(q.fromDay)).Concat(Conditions(q.conditions)).ToList();
        t.outcomes = new List<TriggerOutcome>();
        EditorUtility.SetDirty(t);
        return t;
    }

    /// <summary>An enum value by name that the enum defines (plain Enum.TryParse also accepts any number).</summary>
    private static bool ParseEnum<T>(string text, out T value) where T : struct =>
        Enum.TryParse(text, out value) && Enum.IsDefined(typeof(T), value);

    /// <summary>
    /// Sets every world array of the library and the interview (authoritative),"""),
])

# CheckInterview sits with the other checks, after CheckReferences.
apply(W + r'\Assets\Editor\WorldContentGenerator.cs', [
("""            if (d.tells < 1)
                errors.Add($"Day '{d.asset}' needs \\"tells\\" of at least 1.");
        }
    }
""",
"""            if (d.tells < 1)
                errors.Add($"Day '{d.asset}' needs \\"tells\\" of at least 1.");
        }
    }

    /// <summary>
    /// Checks each day's tell channels and the interview sections (wording,
    /// questions, dialogs, small talk) before anything is written: tokens,
    /// provable categories, gates and announcements, dialog structure and
    /// effects, one set of unique line ids, ASCII text, the worst-case length
    /// of every line the transcript can show, and menu sizes.
    /// </summary>
    private static void CheckInterview(WorldSource src, Authored authored, List<string> errors)
    {
        foreach (DayData d in src.days)
        {
            if (d.channels == null || d.channels.Length == 0)
                errors.Add($"Day '{d.asset}' needs \\"channels\\" (Papers and/or Answer).");
            else
                foreach (string c in d.channels)
                    if (!ParseEnum(c, out TellChannel _))
                        errors.Add($"Day '{d.asset}' has unknown tell channel '{c}' (Papers or Answer).");
        }

        InterviewData iv = src.interview;
        if (iv == null)
        {
            errors.Add($"'{SourcePath}' has no \\"interview\\" section (the interview's wording, menuCapacity and maxLineChars).");
            return;
        }

        // One id set for every line, generated or authored; it starts with the runtime ids.
        var ids = new Dictionary<string, string> { ["case.intro"] = "the desk's opener at runtime", ["case.claim"] = "the traveller's claim at runtime" };
        void Id(string id, string owner)
        {
            if (string.IsNullOrWhiteSpace(id))
                errors.Add($"A line of {owner} has a blank id.");
            else if (ids.TryGetValue(id, out string first))
                errors.Add($"Line id '{id}' is used by {first} and by {owner}.");
            else
                ids.Add(id, owner);
        }

        void Ascii(string id, string text)
        {
            char bad = (text ?? string.Empty).FirstOrDefault(ch => ch > 127);
            if (bad != default)
                errors.Add($"'{id}' holds the non-ASCII character '{bad}'; authored interview text must be ASCII (new glyphs dirty the TMP fallback atlas).");
        }

        // --- The interview's wording and limits ---
        var wording = new (string field, string text)[]
        {
            ("deskName", iv.deskName), ("opener", iv.opener), ("openerLegendary", iv.openerLegendary), ("claim", iv.claim),
            ("honorificMale", iv.honorificMale), ("honorificFemale", iv.honorificFemale), ("honorificUnknown", iv.honorificUnknown),
            ("requestLabel", iv.requestLabel), ("requestPrompt", iv.requestPrompt), ("requestReply", iv.requestReply),
            ("askLabel", iv.askLabel), ("backLabel", iv.backLabel), ("smallTalkLabel", iv.smallTalkLabel), ("smallTalkPrompt", iv.smallTalkPrompt)
        };
        foreach ((string field, string text) in wording)
        {
            if (string.IsNullOrWhiteSpace(text))
                errors.Add($"interview.{field} is blank.");
            Ascii(InterviewLineId(field), text);
        }

        foreach ((string field, string text, string token) in new[]
                 {
                     ("opener", iv.opener, Interview.HonorificToken), ("openerLegendary", iv.openerLegendary, Interview.NameToken),
                     ("claim", iv.claim, Interview.PlaceToken), ("requestLabel", iv.requestLabel, Interview.DocumentToken),
                     ("requestPrompt", iv.requestPrompt, Interview.DocumentToken)
                 })
            if (!string.IsNullOrWhiteSpace(text) && !Interview.HoldsToken(text, token))
                errors.Add($"interview.{field} must hold {Interview.Placeholder(token)}.");

        if (iv.menuCapacity < 1)
            errors.Add("interview.menuCapacity must be at least 1 (a missing value reads 0).");
        if (iv.maxLineChars < 1)
            errors.Add("interview.maxLineChars must be at least 1 (a missing value reads 0).");

        foreach (string field in new[] { "opener", "openerLegendary", "claim", "requestPrompt", "requestReply", "smallTalkPrompt" })
            Id(InterviewLineId(field), $"interview.{field}");

        // --- Questions ---
        var bookCategories = new HashSet<ClueCategory>((authored.books ?? Array.Empty<ReferenceBookSO>()).Select(b => b.category));
        var eraIds = new HashSet<string>(src.eras.Select(e => e.id));
        var questionIds = new HashSet<string>();
        var askedCategories = new HashSet<ClueCategory>();
        QuestionData[] questions = src.questions ?? Array.Empty<QuestionData>();
        foreach (QuestionData q in questions)
        {
            string owner = $"Question '{q.id}'";
            if (string.IsNullOrWhiteSpace(q.id))
                errors.Add("A question has a blank id.");
            else if (!questionIds.Add(q.id))
                errors.Add($"Question id '{q.id}' is listed twice.");

            if (!ParseEnum(q.category, out ClueCategory category))
            {
                errors.Add($"{owner} has unknown category '{q.category}'.");
            }
            else
            {
                if (!Forgery.IsProvableCategory(category, bookCategories))
                    errors.Add($"{owner} asks about {category}, which no reference book (or, for a birth date, the Citizen Record) can prove.");
                if (!askedCategories.Add(category))
                    errors.Add($"{owner} asks about {category} again (one question per category).");
            }

            if (string.IsNullOrWhiteSpace(q.label))
                errors.Add($"{owner} has a blank label.");
            if (string.IsNullOrWhiteSpace(q.prompt))
                errors.Add($"{owner} has a blank prompt.");
            if (!Interview.HoldsToken(q.answer, Interview.ValueToken))
                errors.Add($"{owner}: its answer must hold {Interview.Placeholder(Interview.ValueToken)}.");
            if (q.fromDay < 1)
                errors.Add($"{owner} needs \\"fromDay\\" of at least 1 (a missing fromDay reads 0).");

            bool gated = IsGated(q);
            if (gated && string.IsNullOrWhiteSpace(q.announce))
                errors.Add($"{owner} is gated (from day {q.fromDay}, {(q.conditions != null ? q.conditions.Length : 0)} condition(s)) and needs an \\"announce\\" line for the morning paper.");
            else if (!gated && !string.IsNullOrWhiteSpace(q.announce))
                errors.Add($"{owner} is askable from day 1 without conditions, so nothing announces it; drop its \\"announce\\" line.");

            CheckConditions(q.conditions, owner, true, authored, errors);

            Ascii($"{q.id}.label", q.label);
            Ascii($"{q.id}.prompt", q.prompt);
            Ascii($"{q.id}.answer", q.answer);
            Ascii($"{q.id}.announce", q.announce);
            Id($"{q.id}.prompt", $"question '{q.id}'");
            Id($"{q.id}.answer", $"question '{q.id}'");

            var overridden = new HashSet<string>();
            foreach (OverrideData o in q.overrides ?? Array.Empty<OverrideData>())
            {
                string oOwner = $"{owner} override '{o.era}'";
                if (!eraIds.Contains(o.era ?? string.Empty))
                    errors.Add($"{oOwner} names an unknown era.");
                else if (!overridden.Add(o.era))
                    errors.Add($"{owner} overrides era '{o.era}' twice.");
                if (string.IsNullOrWhiteSpace(o.prompt))
                    errors.Add($"{oOwner} has a blank prompt.");
                if (!Interview.HoldsToken(o.answer, Interview.ValueToken))
                    errors.Add($"{oOwner}: its answer must hold {Interview.Placeholder(Interview.ValueToken)}.");
                Ascii($"{q.id}.{o.era}.prompt", o.prompt);
                Ascii($"{q.id}.{o.era}.answer", o.answer);
                Id($"{q.id}.{o.era}.prompt", $"question '{q.id}' override '{o.era}'");
                Id($"{q.id}.{o.era}.answer", $"question '{q.id}' override '{o.era}'");
            }
        }

        // --- Dialogs ---
        var dialogIds = new HashSet<string>();
        DialogData[] dialogs = src.dialogs ?? Array.Empty<DialogData>();
        foreach (DialogData d in dialogs)
        {
            string owner = $"Dialog '{d.id}'";
            if (string.IsNullOrWhiteSpace(d.id))
                errors.Add("A dialog has a blank id.");
            else if (!dialogIds.Add(d.id))
                errors.Add($"Dialog id '{d.id}' is listed twice.");
            if (string.IsNullOrWhiteSpace(d.label))
                errors.Add($"{owner} has a blank label.");
            Ascii($"{d.id}.label", d.label);
            CheckConditions(d.conditions, owner, false, authored, errors);

            void Lines(LineData[] lines, string lineOwner)
            {
                foreach (LineData line in lines ?? Array.Empty<LineData>())
                {
                    if (!ParseEnum(line.speaker, out DialogSpeaker _))
                        errors.Add($"{owner} line '{line.id}' has unknown speaker '{line.speaker}' (Desk or Traveller).");
                    if (line.id == null || !line.id.StartsWith(d.id + "."))
                        errors.Add($"{owner} line id '{line.id}' must start with '{d.id}.'.");
                    if (string.IsNullOrWhiteSpace(line.text))
                        errors.Add($"{owner} line '{line.id}' is blank.");
                    Ascii(line.id, line.text);
                    Id(line.id, lineOwner);
                }
            }

            foreach (NodeData n in d.nodes ?? Array.Empty<NodeData>())
            {
                Lines(n.lines, $"dialog '{d.id}' node '{n.id}'");
                foreach (ChoiceData c in n.choices ?? Array.Empty<ChoiceData>())
                {
                    Id($"{d.id}.{c.id}", $"choice '{c.id}' of dialog '{d.id}'");
                    Ascii($"{d.id}.{c.id}", c.label);
                    Lines(c.lines, $"a line of choice '{c.id}' of dialog '{d.id}'");

                    if (string.IsNullOrWhiteSpace(c.effect))
                        continue;

                    EffectSO fx = authored.library != null ? authored.library.GetEffectByAssetName(c.effect) : null;
                    if (fx == null)
                    {
                        errors.Add($"{owner} choice '{c.id}' names effect '{c.effect}', which ContentLibrary_Main does not list; add it to the library's effects.");
                        continue;
                    }

                    foreach (EffectOp op in fx.ops)
                        if (op != null && EffectOps.ActsWhileActive(op.type))
                            errors.Add(ContentLibraryValidator.DialogEffectOpError(d.id, c.id, c.effect, op.type) + ".");
                }
            }

            foreach (string problem in DialogChecks.Problems(BuildDialog(d), iv.menuCapacity))
                errors.Add($"{owner}: {problem}.");
        }

        // --- Small talk ---
        void SmallTalkLines(string ownerId, string[] lines, string owner)
        {
            for (int i = 0; lines != null && i < lines.Length; i++)
            {
                string id = SmallTalkId(ownerId, i);
                if (string.IsNullOrWhiteSpace(lines[i]))
                    errors.Add($"{owner} has a blank small-talk line ('{id}').");
                Ascii(id, lines[i]);
                Id(id, $"the small talk of {owner}");
            }
        }

        foreach (EraData e in src.eras)
            SmallTalkLines(e.id, e.smallTalk, $"era '{e.id}'");
        foreach (PlaceData p in src.places)
            SmallTalkLines($"{p.country}_{p.era}", p.smallTalk, $"place '{p.country}_{p.era}'");

        // --- Menus: the intercom must show every choice ---
        bool anySmallTalk = src.eras.Any(e => e.smallTalk != null && e.smallTalk.Length > 0) ||
                            src.places.Any(p => p.smallTalk != null && p.smallTalk.Length > 0);
        foreach (string problem in DialogChecks.MenuProblems(questions.Length, anySmallTalk, ContentLibraryValidator.MaxDocuments(Blueprints(authored)), dialogs.Length, iv.menuCapacity))
            errors.Add(problem);

        // --- Line length: every line the transcript can show fits two lines of a row ---
        int max = iv.maxLineChars;
        if (max < 1)
            return;

        void Fits(string id, string template, string token, int longestValue)
        {
            int length = Interview.WorstCaseLength(template, token, longestValue);
            if (length > max)
                errors.Add($"Line '{id}' can render {length} characters; the transcript holds at most {max} (interview.maxLineChars).");
        }

        int longestHonorific = new[] { iv.honorificMale, iv.honorificFemale, iv.honorificUnknown }.Max(h => (h ?? string.Empty).Length);
        int longestName = authored.library != null ? authored.library.Legendaries.Where(l => l != null).Select(l => (l.displayName ?? string.Empty).Length).DefaultIfEmpty(0).Max() : 0;
        int longestDocument = DocumentTemplates(authored).Select(t => (t.displayName ?? string.Empty).Length).DefaultIfEmpty(0).Max();
        var eraNames = src.eras.ToDictionary(e => e.id, e => e.displayName);
        int longestPlace = src.places.Select(p => OriginLabels.Format(p.displayName, eraNames.TryGetValue(p.era ?? string.Empty, out string era) ? era : null).Length)
                              .DefaultIfEmpty(0).Max();

        Fits(InterviewLineId("opener"), iv.opener, Interview.HonorificToken, longestHonorific);
        Fits(InterviewLineId("openerLegendary"), iv.openerLegendary, Interview.NameToken, longestName);
        Fits(InterviewLineId("claim"), iv.claim, Interview.PlaceToken, longestPlace);
        Fits(InterviewLineId("requestPrompt"), iv.requestPrompt, Interview.DocumentToken, longestDocument);
        Fits(InterviewLineId("requestReply"), iv.requestReply, Interview.ValueToken, 0);
        Fits(InterviewLineId("smallTalkPrompt"), iv.smallTalkPrompt, Interview.ValueToken, 0);

        foreach (QuestionData q in questions)
        {
            int longestValue = ParseEnum(q.category, out ClueCategory category) ? LongestValue(src, category) : 0;
            Fits($"{q.id}.prompt", q.prompt, Interview.ValueToken, 0);
            Fits($"{q.id}.answer", q.answer, Interview.ValueToken, longestValue);
            foreach (OverrideData o in q.overrides ?? Array.Empty<OverrideData>())
            {
                Fits($"{q.id}.{o.era}.prompt", o.prompt, Interview.ValueToken, 0);
                Fits($"{q.id}.{o.era}.answer", o.answer, Interview.ValueToken, longestValue);
            }
        }

        foreach (DialogData d in dialogs)
        {
            foreach (NodeData n in d.nodes ?? Array.Empty<NodeData>())
            {
                foreach (LineData line in n.lines ?? Array.Empty<LineData>())
                    Fits(line.id, line.text, Interview.ValueToken, 0);
                foreach (ChoiceData c in n.choices ?? Array.Empty<ChoiceData>())
                {
                    Fits($"{d.id}.{c.id}", c.label, Interview.ValueToken, 0);
                    foreach (LineData line in c.lines ?? Array.Empty<LineData>())
                        Fits(line.id, line.text, Interview.ValueToken, 0);
                }
            }
        }

        foreach (EraData e in src.eras)
            for (int i = 0; e.smallTalk != null && i < e.smallTalk.Length; i++)
                Fits(SmallTalkId(e.id, i), e.smallTalk[i], Interview.ValueToken, 0);
        foreach (PlaceData p in src.places)
            for (int i = 0; p.smallTalk != null && i < p.smallTalk.Length; i++)
                Fits(SmallTalkId($"{p.country}_{p.era}", i), p.smallTalk[i], Interview.ValueToken, 0);
    }

    /// <summary>
    /// A condition list's problems: an unknown type; a type that needs a
    /// profile, attribute or nation reference (not nameable in the source yet);
    /// DayAtLeast inside a question (its day is "fromDay"); an unknown upgrade;
    /// a blank flag or counter key.
    /// </summary>
    private static void CheckConditions(ConditionData[] conditions, string owner, bool isQuestion, Authored authored, List<string> errors)
    {
        foreach (ConditionData c in conditions ?? Array.Empty<ConditionData>())
        {
            if (!ParseEnum(c.type, out TriggerConditionType type))
            {
                errors.Add($"{owner} has unknown condition type '{c.type}'.");
                continue;
            }

            switch (type)
            {
                case TriggerConditionType.AttributeScoreAtLeast:
                case TriggerConditionType.AttributeScoreAtMost:
                case TriggerConditionType.AttributeIsDominant:
                case TriggerConditionType.AttributeIsSupporting:
                case TriggerConditionType.NationScoreAtLeast:
                    errors.Add($"{owner} uses a {type} condition, which needs a profile, attribute or nation reference; world_source.json cannot name those until piece 5 (history) adds id resolution.");
                    break;
                case TriggerConditionType.DayAtLeast:
                    if (isQuestion)
                        errors.Add($"{owner} gates on DayAtLeast in \\"conditions\\"; use \\"fromDay\\" (one day value per question).");
                    break;
                case TriggerConditionType.UpgradeOwned:
                    if (authored.library == null || authored.library.GetUpgradeById(c.key) == null)
                        errors.Add($"{owner} requires unknown upgrade '{c.key}' (not in the content library's upgrades).");
                    break;
                case TriggerConditionType.FlagSet:
                case TriggerConditionType.FlagNotSet:
                case TriggerConditionType.CounterAtLeast:
                    if (string.IsNullOrWhiteSpace(c.key))
                        errors.Add($"{owner} has a {type} condition with a blank key.");
                    break;
            }
        }
    }

    /// <summary>The document templates a traveller can carry: the wired blueprint's and every listed legendary's override's.</summary>
    private static List<DocumentTemplateSO> DocumentTemplates(Authored authored)
    {
        var templates = new List<DocumentTemplateSO>();
        foreach (CaseBlueprintSO blueprint in Blueprints(authored))
            if (blueprint.DocumentTemplates != null)
                templates.AddRange(blueprint.DocumentTemplates.Where(t => t != null));
        return templates;
    }

    /// <summary>The wired blueprint and the listed legendaries' overrides (non-null).</summary>
    private static List<CaseBlueprintSO> Blueprints(Authored authored)
    {
        var blueprints = new List<CaseBlueprintSO>();
        if (authored.blueprint != null)
            blueprints.Add(authored.blueprint);
        if (authored.library != null)
            foreach (LegendarySO legend in authored.library.Legendaries)
                if (legend != null && legend.blueprintOverride != null)
                    blueprints.Add(legend.blueprintOverride);
        return blueprints;
    }

    /// <summary>
    /// The longest value a {value} of this category can take: the longest fact
    /// of any place, or for a birth date the longest registered date the
    /// places' birth years give.
    /// </summary>
    private static int LongestValue(WorldSource src, ClueCategory category)
    {
        int longest = 0;
        foreach (PlaceData p in src.places)
        {
            if (category == ClueCategory.BirthDate)
            {
                (int min, int max) = BirthYears(p, src.travellerAgeMin, src.travellerAgeMax);
                for (int year = min; year <= max; year++)
                {
                    // Only a date BirthDates can read back is ever printed (it owns "no year 0").
                    string date = BirthDates.Format(28, 0, year);
                    if (BirthDates.TryParse(date, out _, out _, out _))
                        longest = Math.Max(longest, date.Length);
                }
                continue;
            }

            foreach (FactData f in p.facts ?? Array.Empty<FactData>())
                if (f.category == category.ToString())
                    longest = Math.Max(longest, (f.value ?? string.Empty).Length);
        }

        return longest;
    }

    /// <summary>A place's birth-year range: its year minus the oldest and the youngest traveller age (MakePlace writes it, LongestValue measures it).</summary>
    private static (int min, int max) BirthYears(PlaceData p, int ageMin, int ageMax) => (p.year - ageMax, p.year - ageMin);
"""),
])
```

- [ ] **Step 2: The source** — save as `SCRATCH/p3_t13_json.py` and run (it keeps the file's exact `json.dumps(indent=2)` shape and refuses a file already edited):

```python
import json, pathlib, sys
W = r'E:\unity\NOPE-feat-clock'
P = pathlib.Path(W) / 'Assets' / 'Data' / 'World' / 'world_source.json'

text = P.read_text(encoding='utf-8')
src = json.loads(text)
# The file is exactly json.dumps(indent=2, ensure_ascii=False) + "\n"; keep that shape.
if json.dumps(src, indent=2, ensure_ascii=False) + '\n' != text:
    sys.exit('world_source.json is not in its json.dumps(indent=2) shape; edit it by hand instead')
if 'interview' in src or 'questions' in src or 'dialogs' in src:
    sys.exit('world_source.json already has interview sections')

# Two new books: the capital and ruler questions' proof.
src['content']['books'] += [
    'Assets/Data/Investigation/RefBook_Capital.asset',
    'Assets/Data/Investigation/RefBook_Ruler.asset',
]

# Small talk: two lines per era with travellers, one own line per day-1 place.
era_talk = {
    'ancient': ['The harvest was good this year, thanks be to the gods.', 'The roads are safe, as long as you travel by day.'],
    'medieval': ['The bells rang all week for the feast.', 'Taxes again. Always taxes.'],
    'earlymodern': ['Every week a new pamphlet, every month a new war.', 'The post rides faster than ever.'],
    'industrial': ['The smoke never lifts over the mills.', 'Everyone talks about the railway.'],
    'modern': ['The news never stops, day or night.', 'Everything is on the radio now.'],
}
for era in src['eras']:
    if era['id'] in era_talk:
        era['smallTalk'] = era_talk[era['id']]

place_talk = {
    ('egypt', 'ancient'): ['The Nile rose right on time; the fields are black and rich.'],
    ('iraq', 'ancient'): ['The scribes are busy; every jar of barley gets its tablet.'],
    ('greece', 'ancient'): ['Everyone argues in the agora, and nobody agrees.'],
    ('italy', 'ancient'): ['The Senate talks, the legions march.'],
}
for place in src['places']:
    key = (place['country'], place['era'])
    if key in place_talk:
        place['smallTalk'] = place_talk.pop(key)
if place_talk:
    sys.exit(f'places not found: {sorted(place_talk)}')

# Tell channels per day, right after the tell count.
channels = {1: ['Papers'], 2: ['Papers', 'Answer'], 3: ['Papers', 'Answer']}
for i, day in enumerate(src['days']):
    ordered = {}
    for k, v in day.items():
        ordered[k] = v
        if k == 'tells':
            ordered['channels'] = channels[day['day']]
    src['days'][i] = ordered

src['interview'] = {
    'deskName': 'DESK',
    'opener': 'Next! Step forward, {honorific}.',
    'openerLegendary': 'Priority arrival: {name}.',
    'claim': 'I request passage home to {place}.',
    'honorificMale': 'sir',
    'honorificFemale': 'madam',
    'honorificUnknown': 'traveller',
    'requestLabel': 'Request {document}',
    'requestPrompt': 'Your {document}, please.',
    'requestReply': 'Here you are.',
    'askLabel': 'Ask about home >',
    'backLabel': '< Back',
    'smallTalkLabel': 'Small talk',
    'smallTalkPrompt': 'How is life back home?',
    'menuCapacity': 8,
    'maxLineChars': 100,
}

def question(qid, category, label, prompt, answer, from_day, announce='', conditions=None, overrides=None):
    return {
        'id': qid,
        'category': category,
        'label': label,
        'prompt': prompt,
        'answer': answer,
        'fromDay': from_day,
        'announce': announce,
        'conditions': conditions or [],
        'overrides': overrides or [],
    }

src['questions'] = [
    question('q_currency', 'Currency', 'Currency', 'What money do you pay with at home?', 'We pay in {value}.', 1,
             overrides=[{'era': 'ancient', 'prompt': 'What do you trade with at home?', 'answer': 'We trade with {value}.'}]),
    question('q_language', 'Language', 'Language', 'What language do you speak at home?', 'At home we speak {value}.', 1),
    question('q_device', 'Technology', 'Device', 'What tool do you use every day?', 'Every day I use {value}.', 1),
    question('q_capital', 'Geography', 'Capital', 'What is the capital of your land?', 'Our capital is {value}.', 2,
             announce='BUREAU NOTICE: desk officers may now ask travellers for their capital. Check answers against the Capitals Gazetteer.'),
    question('q_ruler', 'Politics', 'Ruler', 'Who rules your land?', 'We are ruled by {value}.', 3,
             announce='BUREAU NOTICE: desk officers may now ask travellers who rules them. Check answers against Rulers & Regents.'),
    question('q_born', 'BirthDate', 'Date of birth', 'When were you born?', 'I was born on {value}.', 1,
             announce='BUREAU NOTICE: with Interview Protocols, desk officers may now ask travellers when they were born. Check answers against Citizen Records.',
             conditions=[{'type': 'UpgradeOwned', 'key': 'interview_protocols', 'threshold': 0}]),
]

def line(lid, text, speaker='Traveller'):
    return {'id': lid, 'speaker': speaker, 'text': text}

def choice(cid, label, next_node='', effect=''):
    return {'id': cid, 'label': label, 'lines': [], 'next': next_node, 'effect': effect}

src['dialogs'] = [
    {
        'id': 'dlg_rumour',
        'label': 'Any news from home? >',
        'conditions': [{'type': 'DayAtLeast', 'key': '', 'threshold': 2}],
        'nodes': [
            {
                'id': 'start',
                'lines': [line('dlg_rumour.start.1', 'News? Only a rumour: someone sells pocket calculators to the ancients.')],
                'choices': [choice('more', 'Tell me more.', 'detail'), choice('ignore', 'Not my business.')],
            },
            {
                'id': 'detail',
                'lines': [line('dlg_rumour.detail.1', 'They say a courier from your own century carries them. Watch for anyone who counts too fast.')],
                'choices': [choice('noted', 'Noted. Thank you.', effect='Effect_Dialog_RumourHeard')],
            },
        ],
    },
    {
        'id': 'dlg_rumour_followup',
        'label': 'About those calculators... >',
        'conditions': [{'type': 'FlagSet', 'key': 'rumour_calculators', 'threshold': 0}],
        'nodes': [
            {
                'id': 'start',
                'lines': [line('dlg_rumour_followup.start.1', 'Calculators? I saw nothing. Nothing at all.')],
                'choices': [choice('bye', 'Of course. Next!')],
            },
        ],
    },
]

P.write_text(json.dumps(src, indent=2, ensure_ascii=False) + '\n', encoding='utf-8', newline='\n')
print('wrote', P)
```

- [ ] **Step 3: Check the source's worst-case line lengths** (the generator's rule, run here in Python on the real facts; Unity repeats it in Task 17) — save as `SCRATCH/p3_t13_lengths.py` and run:

```python
import json, pathlib
W = r'E:\unity\NOPE-feat-clock'
src = json.loads((pathlib.Path(W) / 'Assets' / 'Data' / 'World' / 'world_source.json').read_text(encoding='utf-8'))
iv = src['interview']
eras = {e['id']: e['displayName'] for e in src['eras']}

def longest_value(category):
    if category == 'BirthDate':
        years = [y for p in src['places'] for y in range(p['year'] - src['travellerAgeMax'], p['year'] - src['travellerAgeMin'] + 1) if y != 0]
        return max(len(f'28 Jan {-y} BCE' if y < 0 else f'28 Jan {y}') for y in years)
    return max(len(f['value']) for p in src['places'] for f in p['facts'] if f['category'] == category)

def worst(template, token, longest):
    return len(template) + template.count('{' + token + '}') * (longest - len(token) - 2)

lengths = [
    ('interview.opener', worst(iv['opener'], 'honorific', max(len(iv[h]) for h in ('honorificMale', 'honorificFemale', 'honorificUnknown')))),
    ('claim', worst(iv['claim'], 'place', max(len(f"{p['displayName']} ({eras[p['era']]})") for p in src['places']))),
    ('interview.requestPrompt', worst(iv['requestPrompt'], 'document', len('Travel Passport'))),
]
for q in src['questions']:
    lengths.append((q['id'] + '.prompt', len(q['prompt'])))
    lengths.append((q['id'] + '.answer', worst(q['answer'], 'value', longest_value(q['category']))))
    for o in q['overrides']:
        lengths.append((f"{q['id']}.{o['era']}.answer", worst(o['answer'], 'value', longest_value(q['category']))))
for d in src['dialogs']:
    for n in d['nodes']:
        lengths += [(l['id'], len(l['text'])) for l in n['lines']]
        lengths += [(f"{d['id']}.{c['id']}", len(c['label'])) for c in n['choices']]
for owner in src['eras'] + src['places']:
    lengths += [('smalltalk', len(t)) for t in owner.get('smallTalk', [])]

ranked = sorted(lengths, key=lambda x: -x[1])
print('longest', *ranked[0])
print(*[l for l in ranked if l[0] == 'claim'][0])
print('over', [l for l in lengths if l[1] > iv['maxLineChars']])
```

Expected output: `longest dlg_rumour.detail.1 92`, `claim 70`, `over []`.

- [ ] **Step 4: Docs and the book placement** — save as `SCRATCH/p3_t13_docs.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Scripts\DayPlanSO.cs', [
("""    /// <summary>
    /// Where today's liars may leak tells: Papers (their documents) and/or
    /// Answer (their answers to today's questions). The default keeps a day
    /// plan that does not set it on papers-only tells.
    /// </summary>""",
"""    /// <summary>
    /// Where today's liars may leak tells: Papers (their documents) and/or
    /// Answer (their answers to today's questions). Written by
    /// Tools > TimeDesk > Generate World from world_source.json days[].channels;
    /// the default keeps a day plan that does not set it on papers-only tells.
    /// </summary>"""),
])

apply(W + r'\Assets\Scripts\Timeline\TimelineTriggerSO.cs', [
("""/// A special condition of the timeline ("5+ greek warriors sent to the battle era",
/// "Tesla delivered to Germany") evaluated during the nightly resolve. When all
/// conditions pass, its outcome effects activate — e.g., "+scientist visitors for
/// 3 days" or "-30% on electric tech upgrades for 2 days".""",
"""/// A special condition of the timeline ("5+ greek warriors sent to the battle era",
/// "Tesla delivered to Germany") evaluated during the nightly resolve. When all
/// conditions pass, its outcome effects activate — e.g., "+scientist visitors for
/// 3 days" or "-30% on electric tech upgrades for 2 days". Generate World also
/// writes one per gated interview question, whose news line announces it."""),
])

# Five book windows: wrap after three, the second row offset by 40 px.
apply(W + r'\Assets\Scripts\UI\InvestigationUIController.cs', [
("""            if (win.transform is RectTransform rt)
                rt.anchoredPosition = new Vector2(-380f + i * 320f, -150f);""",
"""            if (win.transform is RectTransform rt)
                rt.anchoredPosition = new Vector2(-380f + (i % 3) * 320f + (i / 3) * 40f, -150f + (i / 3) * 40f);"""),
])

apply(W + r'\docs\FEATURES.md', [
("""- [ ] `Tools > TimeDesk > Generate World` — builds the real world from `Assets/Data/World/world_source.json` (eras, countries, 40 places, rules, day plans with each day's tell count) and wires `ContentLibrary_Main`, the case blueprint and the day plans named in its `content` section; checks every reference before writing anything; idempotent; owns only `Assets/Data/World/{Eras,Nations,Places,Rules}` (unlisted assets there go to the trash) and never removes hand-authored legendaries, effects or triggers""",
"""- [ ] `Tools > TimeDesk > Generate World` — builds the real world from the hand-maintained `Assets/Data/World/world_source.json` (eras, countries, 40 places, rules, day plans with each day's tell count and tell channels, small talk, the interview's wording and its two layout limits (menu capacity, longest line), questions with an unlock-announcement trigger for every gated question, and dialogs) and wires `ContentLibrary_Main`, the case blueprint and the day plans named in its `content` section; checks every reference before writing anything, and that every line id is unique, authored interview text is ASCII, no line is longer than the transcript holds, no menu is fuller than the intercom shows and no dialog effect carries a timed modifier; idempotent; owns only `Assets/Data/World/{Eras,Nations,Places,Rules,Interview}` (unlisted assets there go to the trash) and never removes hand-authored legendaries, effects or triggers (the library's triggers are rewired: hand-authored ones kept in order, generated unlock triggers appended); the scratchpad script that once wrote the source is retired"""),
("""- [ ] Reference book windows list TODAY's places only (country then era, paged), read from the day's `FactTable` snapshot — the same values printed on papers (table tested: `FactTableTests`; the day's place filter is covered by the content validator and the Unity world check)""",
"""- [ ] Reference book windows list TODAY's places only (country then era, paged), read from the day's `FactTable` snapshot — the same values printed on papers (table tested: `FactTableTests`; the day's place filter is covered by the content validator and the Unity world check)
- [ ] Reference books: Currency Ledger, Tongues & Scripts, Index of Devices, Capitals Gazetteer, Rulers & Regents (all on the desktop from day 1; five book windows open in two staggered rows)"""),
])
```

- [ ] **Step 5: Compile check and run**

Expected: exit 0 / exit 0; `passed 336, failed 0`.

- [ ] **Step 6: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Editor/WorldContentGenerator.cs Assets/Data/World/world_source.json Assets/Scripts/DayPlanSO.cs Assets/Scripts/Timeline/TimelineTriggerSO.cs Assets/Scripts/UI/InvestigationUIController.cs docs/FEATURES.md && git commit -F - <<'EOF'
feat(content): Generate World writes the interview

world_source.json gains the interview's wording and limits, six questions,
two dialogs, small talk, the capital and ruler books and each day's tell
channels. Generate World checks them before writing (tokens, provable
categories, gates and announcements, dialog structure and effects, unique
line ids, ASCII, worst-case line lengths, menu sizes) and writes questions,
dialogs and an unlock-announcement trigger per gated question into the
owned Interview folder. Five book windows wrap into two rows.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 14: The intercom becomes an interview (wiring)

Glue for spec §1.1–1.5, §1.9, §2.9–2.11, X6/X7/X9, R8, R11, R12, R26: every traveller answers today's askable questions from the same values as the papers (`AddAnswers`, placeholders included), a liar's Answer tell is spoken, small talk is picked on the dialog stream, and the opener and claim are content (a day whose library lacks their wording logs one warning per missing piece, not one per traveller). `GameManager` builds the day's `InterviewDay` from the day-start world with `TimelineService.BuildInterviewDay` (the one projection; the Unity verification calls it too) and injects it; where the transcript is not reachable nothing spoken is computed (R12). The desk plays the interview graph on the intercom, opens the transcript for every choice but a request, and records finished dialogs. The fallback lists the interview and only the claimed place's book entries. Copy changes (citation, scanner hint) and every FEATURES line of the interview land here.

**Between this task and Task 17 the interview wording is empty.** The library gets its `interview` section only when Task 17 runs Generate World, so until then every day logs the two wording warnings once (opener, claim line), transcripts start with the claim, and the claim banner shows the bare place label: FEATURES' "Claim banner (visitor name + "I request passage home to <place> (<era>)")" is briefly untrue and holds again once Task 17 writes `interview.claim`. Nothing runs the game in between (Tasks 15 and 16 are offline), so no play is affected.

**Files:**
- Create: `Assets/Scripts/UI/TranscriptWindowController.cs` (+ `.meta`)
- Modify: `Assets/Scripts/CaseInstance.cs`, `Assets/Scripts/CaseFactory.cs`, `Assets/Scripts/UI/InvestigationUIController.cs`, `Assets/Scripts/UI/CompareController.cs`, `Assets/Scripts/UI/InteractionPanelController.cs`, `Assets/Scripts/Shift/ShiftScoring.cs`, `Assets/Scripts/GameManager.cs`, `Assets/Scripts/Timeline/TimelineService.cs`, `docs/FEATURES.md`

- [ ] **Step 1: The transcript window** — write `Assets/Scripts/UI/TranscriptWindowController.cs` (Write tool; then `make_meta.py`):

```csharp
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Case Notes: Interview — the current traveller's transcript: one row per
/// line (speaker, sentence), paged like the reference books and always
/// showing the newest page. Only answer rows are clickable: a click puts the
/// answer's canonical fact value into the compare bar as the traveller's
/// statement. Rows are named Line_{id}. No coroutines: the desktop canvas is
/// switched off outside monitor focus.
/// </summary>
public sealed class TranscriptWindowController : PagedRowsWindow
{
    private IReadOnlyList<DialogLine> _lines = System.Array.Empty<DialogLine>();
    private string _deskName = string.Empty;
    private string _travellerName = string.Empty;
    private CompareController _compare;

    /// <summary>Shows a traveller's transcript (the runner's live, append-only list) on its newest page.</summary>
    public void Bind(IReadOnlyList<DialogLine> transcript, string deskName, string travellerName, CompareController compare)
    {
        _lines = transcript ?? System.Array.Empty<DialogLine>();
        _deskName = deskName ?? string.Empty;
        _travellerName = travellerName ?? string.Empty;
        _compare = compare;
        ShowLastPage();
    }

    /// <summary>Shows the newest page (call after lines were appended).</summary>
    public void Refresh() => ShowLastPage();

    /// <inheritdoc />
    protected override int RowCount => _lines.Count;

    /// <summary>Speaker and sentence; an answer row puts its fact into the compare bar, other rows are inert.</summary>
    protected override void FillRow(int index, GameObject row, TMP_Text[] texts, Image background, Button button)
    {
        DialogLine line = _lines[index];
        row.name = $"Line_{line.Id}";

        if (texts.Length > 0 && texts[0] != null)
            texts[0].text = line.Speaker == DialogSpeaker.Desk ? _deskName : _travellerName;
        if (texts.Length > 1 && texts[1] != null)
            texts[1].text = line.Text;

        if (button == null)
            return;

        // A disabled button is neither clickable nor hover-highlighted.
        button.enabled = line.IsAnswer;
        if (!line.IsAnswer || _compare == null)
            return;

        string label = $"Intercom · {ClueLabels.Report(line.Category)}";
        string value = line.Value;
        CompareEvidence evidence = CompareEvidence.ForAnswer(line.Category, line.Value, line.IsTell);
        button.onClick.AddListener(() => _compare.Select(label, value, background, evidence));
    }
}
```

- [ ] **Step 2: Case generation speaks** — save as `SCRATCH/p3_t14_cases.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'
A = W + r'\Assets\Scripts'

apply(A + r'\CaseInstance.cs', [
("""    /// <summary>Short intro line for the case.</summary>
    public string introLine;""",
"""    /// <summary>The desk's opener for this traveller (interview lines, with the traveller's honorific); the transcript's first line.</summary>
    public string introLine;"""),
("""    /// <summary>True when the traveller lied about their home (their papers leak tells).</summary>""",
"""    /// <summary>True when the traveller lied about their home (their papers or answers leak tells).</summary>"""),
("""    /// <summary>The visitor's stated travel claim line, for the UI banner.</summary>
    public string claimLine;""",
"""    /// <summary>The traveller's claim sentence (interview.claim with the claimed place's label); the banner, the shift summary and the transcript's second line.</summary>
    public string claimLine;

    /// <summary>The traveller's answer to each question askable today, in question order (computed at generation from the same values as the papers).</summary>
    public readonly List<InterviewAnswer> answers = new();

    /// <summary>What the traveller says when asked small talk (their claimed place's or era's flavour; null when none is authored).</summary>
    public LineText smallTalk;"""),
])

C = A + r'\CaseFactory.cs'
apply(C, [
("""/// DayPlanSO -> picks the place the traveller CLAIMS as home (era by weight,
/// nation among today's places) -> the registered identity every traveller
/// carries (names and birth years of the claimed place) -> documents whose
/// fields come from today's FactTable for the claim -> maybe a lie: a liar
/// really comes from another of today's places and their papers leak tells
/// carrying that true home's values (Lies). Every draw comes from seeded
/// streams (per traveller: the case, legacy clue and lie streams; plus the
/// day's rule-violator stream), so the same run and day always produce the
/// same travellers.""",
"""/// DayPlanSO -> picks the place the traveller CLAIMS as home (era by weight,
/// nation among today's places) -> the registered identity every traveller
/// carries (names and birth years of the claimed place) -> documents whose
/// fields come from today's FactTable for the claim -> maybe a lie: a liar
/// really comes from another of today's places and leaks tells carrying that
/// true home's values, on the papers or in their answers (Lies) -> the
/// traveller's answers to today's questions, the desk's opener and the claim
/// sentence (the content library's interview wording) and a small-talk line.
/// Every draw comes from seeded streams (per traveller: the case, legacy
/// clue, lie and dialog streams; plus the day's rule-violator stream), so the
/// same run and day always produce the same travellers."""),
("""    private IRandomSource _lieRng = new SeededRandom(0);
""",
"""    private IRandomSource _lieRng = new SeededRandom(0);

    /// <summary>The current traveller's dialog stream (Seeds.ForDialog): the small-talk pick, apart from the case and lie streams.</summary>
    private IRandomSource _dialogRng = new SeededRandom(0);

    /// <summary>Today's askable question categories; every traveller answers each (InterviewDay.AskableCategories).</summary>
    private IReadOnlyList<ClueCategory> _askable = System.Array.Empty<ClueCategory>();

    /// <summary>Today's question categories that may carry an Answer tell (day-gated questions only; InterviewDay.AnswerTellCategories).</summary>
    private IReadOnlyList<ClueCategory> _answerTellCategories = System.Array.Empty<ClueCategory>();
"""),
("""    /// <summary>
    /// Generates the full list of cases for a day, based on the DayPlan.
    /// Forced cases override procedural blueprint selection per slot. Each slot
    /// draws from its own stream (Seeds.ForCase), so one traveller's draws never
    /// shift the next one's.
    /// </summary>
    public List<CaseInstance> GenerateDayCases(DayPlanSO plan, WorldState state, int daySeed)
    {""",
"""    /// <summary>
    /// Generates the full list of cases for a day, based on the DayPlan.
    /// Forced cases override procedural blueprint selection per slot. Each slot
    /// draws from its own stream (Seeds.ForCase), so one traveller's draws never
    /// shift the next one's. Every traveller answers each of
    /// <paramref name="askable"/> (InterviewDay.AskableCategories); only
    /// <paramref name="answerTellCategories"/> (InterviewDay.AnswerTellCategories)
    /// may carry a spoken tell. Null lists count as empty.
    /// </summary>
    public List<CaseInstance> GenerateDayCases(DayPlanSO plan, WorldState state, int daySeed,
                                               IReadOnlyList<ClueCategory> askable, IReadOnlyList<ClueCategory> answerTellCategories)
    {"""),
("""        int total = Mathf.Max(1, plan.VisitorsCount);

        // Fresh roster: names are unique within the day (records use first match).""",
"""        int total = Mathf.Max(1, plan.VisitorsCount);
        _askable = askable ?? System.Array.Empty<ClueCategory>();
        _answerTellCategories = answerTellCategories ?? System.Array.Empty<ClueCategory>();

        // The opener and the claim are content: one warning a day when Generate World has not written them.
        InterviewLines wording = _lib.Interview;
        if (wording == null || string.IsNullOrWhiteSpace(wording.opener?.text) || string.IsNullOrWhiteSpace(wording.openerLegendary?.text))
            Debug.LogWarning($"[CaseFactory] Day {plan.DayNumber}: the content library's interview opener or legendary opener is blank, so a transcript may start with the claim. Run Tools > TimeDesk > Generate World.");
        if (wording == null || string.IsNullOrWhiteSpace(wording.claim?.text))
            Debug.LogWarning($"[CaseFactory] Day {plan.DayNumber}: the content library has no interview claim line, so the banner shows the bare place label. Run Tools > TimeDesk > Generate World.");

        // Fresh roster: names are unique within the day (records use first match)."""),
("""            _lieRng = new SeededRandom(Seeds.ForLies(caseSeed));
            results.Add(GenerateSingleCase(plan, state, i, caseIndex1Based));""",
"""            _lieRng = new SeededRandom(Seeds.ForLies(caseSeed));
            _dialogRng = new SeededRandom(Seeds.ForDialog(caseSeed));
            results.Add(GenerateSingleCase(plan, state, i, caseIndex1Based));"""),
("""        string intro = legendary != null ? $"Priority arrival: {legendary.displayName}." : "Next subject for reassignment.";
""",
"""        string intro = Interview.Opener(_lib.Interview, gender, legendary != null ? legendary.displayName : null);
"""),
("""        inst.claimLine = $"I request passage home to {originLabel}.";""",
"""        inst.claimLine = Interview.Claim(_lib.Interview, originLabel);"""),
("""        LiePlan lie = Disguise(inst, fields, plan, blueprint, state, caseIndex1Based);

        string archetypeName = archetype != null ? archetype.displayName : string.Empty;
        string tells = lie != null ? string.Join(", ", lie.Tells) : string.Empty;
        Debug.Log($"[CaseFactory] Case {caseIndex1Based}: blueprint='{blueprint.name}', place='{originLabel}', archetype='{archetypeName}', legendary={legendary != null}, visitor='{visitorName}', born='{birthDate}', liar={inst.IsLiar}, home='{inst.HomeLabel}', tells=[{tells}], gender={inst.gender}, claimAllowed={inst.claimAllowedByRules}, shouldAccept={inst.ShouldAccept}.");""",
"""        LiePlan lie = Disguise(inst, fields, plan, blueprint, state, caseIndex1Based);
        AddAnswers(inst, lie);

        // Small talk: the claimed place's lines, else its era's (glue: only resolves the two lists).
        EraSO talkEra = place != null ? place.era : trueEra;
        inst.smallTalk = Interview.PickSmallTalk(place != null ? place.smallTalk : null, talkEra != null ? talkEra.smallTalk : null, _dialogRng);

        string archetypeName = archetype != null ? archetype.displayName : string.Empty;
        string tells = lie != null ? string.Join(", ", lie.Tells.Select(t => $"{t}/{lie.ChannelOf(t)}")) : string.Empty;
        Debug.Log($"[CaseFactory] Case {caseIndex1Based}: blueprint='{blueprint.name}', place='{originLabel}', archetype='{archetypeName}', legendary={legendary != null}, visitor='{visitorName}', born='{birthDate}', liar={inst.IsLiar}, home='{inst.HomeLabel}', tells=[{tells}], answers={inst.answers.Count}, gender={inst.gender}, claimAllowed={inst.claimAllowedByRules}, shouldAccept={inst.ShouldAccept}.");"""),
("""    /// <summary>
    /// Rolls the traveller's lie on their lie stream and applies it (Lies.Plan):
    /// a liar gets a true home among today's other places, and every field of
    /// each tell category is rewritten with that home's value. Exempt
    /// travellers (legendaries, a claim a rule forbids, no papers) draw
    /// nothing. Returns the plan, or null when the traveller is exempt.
    /// </summary>""",
"""    /// <summary>
    /// Rolls the traveller's lie on their lie stream and applies it (Lies.Plan):
    /// a liar gets a true home among today's other places; every field of each
    /// Papers-tell category is rewritten with that home's value, and an Answer
    /// tell leaves the papers on the cover (AddAnswers speaks it). Exempt
    /// travellers (legendaries, a claim a rule forbids, no papers) draw
    /// nothing. Returns the plan, or null when the traveller is exempt.
    /// </summary>"""),
("""            System.Array.Empty<ClueCategory>(),
            plan.TellChannels,""",
"""            _answerTellCategories,
            plan.TellChannels,"""),
("""            Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: rolled a liar, but no other place today can carry a provable tell against '{inst.originLabel}' (no printed, book-covered fact that differs from the claim's and belongs to that place alone, and no birth year other than the record's), so the traveller stays honest. Widen the day's eras or countries, add a reference book for a printed category, or give places that share a fact value distinct values.");""",
"""            Debug.LogWarning($"[CaseFactory] Case {caseIndex1Based}: rolled a liar, but no other place today can carry a provable tell against '{inst.originLabel}' (no book-covered fact that its papers print or today's day-gated questions ask, that differs from the claim's and belongs to that place alone, and no birth year other than the record's), so the traveller stays honest. Widen the day's eras or countries, add a reference book, allow more tell channels, or give places that share a fact value distinct values.");"""),
("""    /// <summary>
    /// The chance a traveller lies: the blueprint's contradiction chance plus""",
"""    /// <summary>
    /// The traveller's answer to each of today's askable questions, in
    /// question order: the cover value ResolveFieldValue gives the papers (the
    /// registered birth date, the claim's fact or its placeholder), or an
    /// Answer tell's true-home value (Interview.Answer). Reads the claim, never
    /// the fields, so it does not matter that Disguise already applied the plan.
    /// </summary>
    private void AddAnswers(CaseInstance inst, LiePlan lie)
    {
        foreach (ClueCategory category in _askable)
            inst.answers.Add(Interview.Answer(category, ResolveFieldValue(category, inst), lie));
    }

    /// <summary>
    /// The chance a traveller lies: the blueprint's contradiction chance plus"""),
("""    /// facts for the claimed place, with a readable placeholder (and a warning)
    /// when content is missing. A liar's tells overwrite these values afterwards
    /// (Disguise).
    /// </summary>""",
"""    /// facts for the claimed place, with a readable placeholder (and a warning)
    /// when content is missing; also each spoken answer's cover value
    /// (AddAnswers). A liar's Papers tells overwrite the printed values
    /// afterwards (Disguise); an Answer tell replaces only the spoken value
    /// (Interview.Answer).
    /// </summary>"""),
("""        Debug.LogWarning($"[CaseFactory] '{inst.originLabel}' has no {category} fact today; printing a placeholder. Check the place's facts (Tools > TimeDesk > Validate Content Library).");""",
"""        Debug.LogWarning($"[CaseFactory] '{inst.originLabel}' has no {category} fact today; using a placeholder on the papers and in answers. Check the place's facts (Tools > TimeDesk > Validate Content Library).");"""),
])
```

- [ ] **Step 3: The desk plays the interview** — save as `SCRATCH/p3_t14_ui.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
from cut import cut
W = r'E:\unity\NOPE-feat-clock'
U = W + r'\Assets\Scripts\UI\InvestigationUIController.cs'

apply(U, [
("""/// <summary>
/// Orchestrates the office investigation: shows the visitor's travel claim and
/// today's directives, spawns a draggable window per document, builds a shelf of
/// reference books the player can open/stow, and offers the binary Accept/Deny.
///""",
"""/// <summary>
/// Orchestrates the office investigation: shows the visitor's travel claim and
/// today's directives, runs the interview on the intercom (document requests,
/// today's questions and narrative dialogs, with the transcript window), spawns
/// a draggable window per document, builds a shelf of reference books the
/// player can open/stow, and offers the binary Accept/Deny.
///"""),
("""    /// <summary>Intercom panel listing per-case traveller actions.</summary>
    [SerializeField] private InteractionPanelController interactionPanel;""",
"""    /// <summary>The intercom: shows the current interview node's choices (requests, questions, dialog replies).</summary>
    [SerializeField] private InteractionPanelController interactionPanel;"""),
("""    /// <summary>Citizen Records app (registry injected per day).</summary>
    [SerializeField] private CitizenRecordsWindowController recordsWindow;
""",
"""    /// <summary>Citizen Records app (registry injected per day).</summary>
    [SerializeField] private CitizenRecordsWindowController recordsWindow;

    [Header("Interview")]
    /// <summary>Case Notes: Interview, the current traveller's transcript (answer rows are compare-clickable).</summary>
    [SerializeField] private TranscriptWindowController transcriptWindow;

    /// <summary>The transcript window's chrome; every interview choice but a document request opens it.</summary>
    [SerializeField] private OSWindowChrome transcriptChrome;
"""),
("""    /// <summary>The case currently on the desk (null between cases).</summary>
    private CaseInstance _currentCase;
""",
"""    /// <summary>The case currently on the desk (null between cases).</summary>
    private CaseInstance _currentCase;

    /// <summary>Today's interview (set by GameManager): askable questions, offered dialogs, wording and the shift's dialog outcomes.</summary>
    private InterviewDay _day;

    /// <summary>The current traveller's interview (null before the first case).</summary>
    private DialogRunner _runner;
"""),
("""    public bool EvidenceSystemActive => RichMode && compareController != null;
""",
"""    public bool EvidenceSystemActive => RichMode && compareController != null;

    /// <summary>
    /// True when a traveller's answers can be read: always in the text
    /// fallback; in the rich desk only when the intercom, the transcript window
    /// and its chrome are wired. When false, GameManager computes no answers
    /// and generates no spoken tell that day. (Serialized references are
    /// compared with != null: an unassigned one is Unity's fake null.)
    /// </summary>
    public bool InterviewReachable =>
        !RichMode || (interactionPanel != null && transcriptWindow != null && transcriptChrome != null);
"""),
("""        if (EvidenceSystemActive && recordsWindow == null)
            Debug.LogWarning("[InvestigationUIController] Citizen Records not wired: birth-date tells cannot be proven. Run Tools > TimeDesk > Build Office UI.", this);
    }""",
"""        if (EvidenceSystemActive && recordsWindow == null)
            Debug.LogWarning("[InvestigationUIController] Citizen Records not wired: birth-date tells cannot be proven. Run Tools > TimeDesk > Build Office UI.", this);

        // Without the transcript nothing a traveller says could be read, so the day speaks no tell.
        if (RichMode && !InterviewReachable)
            Debug.LogWarning("[InvestigationUIController] Intercom or interview transcript not wired: questions are hidden and no tell is spoken today. Run Tools > TimeDesk > Build Office UI.", this);
    }"""),
("""    /// <summary>Injects today's facts (the reference books render these rows).</summary>
    public void SetFacts(FactTable facts)
    {
        _facts = facts;
    }
""",
"""    /// <summary>Injects today's facts (the reference books render these rows).</summary>
    public void SetFacts(FactTable facts)
    {
        _facts = facts;
    }

    /// <summary>Injects today's interview (questions, dialogs and wording, fixed at day start).</summary>
    public void SetInterviewDay(InterviewDay day)
    {
        _day = day;
    }
"""),
("""                "Compare a document field against the claimed place's reference entry, " +
                "the entry it really belongs to, or the Citizen Record to log evidence.";""",
"""                "Compare a document field or a traveller's answer against the claimed place's reference entry, " +
                "the entry it really belongs to, or the Citizen Record to log evidence.";"""),
])

# The per-document request actions become the interview hub.
cut(U, """        // Documents are handed over via intercom actions ("Request Passport"),
        // not desktop icons: the windows spawn hidden and open on request.
        var actions = new List<InteractionAction>();
""", """        if (interactionPanel != null)
            interactionPanel.SetActions(actions);
""", """        // Documents are handed over through the interview ("Request Travel
        // Passport"), not desktop icons: the windows spawn hidden and open on request.
        var documentNames = new List<string>();

        if (inst != null)
        {
            int i = 0;
            foreach (DocumentInstance doc in inst.documents)
            {
                DocumentWindowController clone = Instantiate(documentWindowTemplate, windowLayer);
                clone.gameObject.SetActive(false);
                if (clone.transform is RectTransform rt)
                    rt.anchoredPosition = new Vector2(-330f + i * 620f, 140f);
                clone.SetDocument(doc, compareController);
                _docWindows.Add(clone.gameObject);
                documentNames.Add(doc != null && doc.template != null ? doc.template.displayName : "Document");
                i++;
            }
        }

        StartInterview(inst, documentNames);
""")

apply(U, [
("""    /// <summary>
    /// Closes every window on the window layer (templates are already""",
"""    /// <summary>
    /// Starts the traveller's interview: the hub with a request per document,
    /// and, when the interview is reachable, today's questions, small talk and
    /// offered dialogs (without a wired transcript nothing spoken could be read,
    /// so only the requests remain). The transcript starts with the opener and
    /// the claim.
    /// </summary>
    private void StartInterview(CaseInstance inst, IReadOnlyList<string> documentNames)
    {
        _runner = null;
        if (_day == null)
        {
            Debug.LogError("[InvestigationUIController] No interview day was injected (GameManager.SetInterviewDay), so the intercom is empty.", this);
            if (interactionPanel != null)
                interactionPanel.Clear();
            return;
        }

        bool reachable = InterviewReachable;
        var interviewCase = new InterviewCase
        {
            introLine = inst != null ? inst.introLine : null,
            claimLine = inst != null ? inst.claimLine : null,
            claimedEraId = inst != null && inst.claimedEra != null ? inst.claimedEra.id : null,
            documentNames = documentNames,
            answers = inst != null ? inst.answers : null,
            smallTalk = reachable && inst != null ? inst.smallTalk : null
        };

        DialogGraph graph = InterviewScript.Build(_day.Lines,
            reachable ? _day.Questions : Array.Empty<InterviewQuestion>(),
            reachable ? _day.OfferedDialogs() : Array.Empty<AuthoredDialog>(),
            interviewCase);
        _runner = new DialogRunner(graph, InterviewScript.Opening(interviewCase));

        if (transcriptWindow != null)
            transcriptWindow.Bind(_runner.Transcript, _day.Lines.deskName, inst != null ? inst.visitorGivenName : string.Empty, compareController);

        RefreshChoices();
    }

    /// <summary>Shows the current interview node's choices on the intercom.</summary>
    private void RefreshChoices()
    {
        if (interactionPanel == null || _runner == null)
            return;

        var actions = new List<InteractionAction>();
        foreach (DialogChoice choice in _runner.Choices)
        {
            string id = choice.Id;
            actions.Add(new InteractionAction { label = choice.Label, execute = () => Choose(id) });
        }

        interactionPanel.SetActions(actions);
    }

    /// <summary>
    /// Plays one interview choice: the transcript shows its lines; a document
    /// request opens and raises that document's window, any other choice opens
    /// the transcript; a finished dialog is recorded for the end of the shift.
    /// </summary>
    private void Choose(string choiceId)
    {
        DialogChoice choice = _runner != null ? _runner.Choose(choiceId) : null;
        if (choice == null)
            return;

        if (transcriptWindow != null)
            transcriptWindow.Refresh();

        if (choice.Action == DialogAction.OpenDocument)
        {
            GameObject window = choice.DocumentIndex >= 0 && choice.DocumentIndex < _docWindows.Count ? _docWindows[choice.DocumentIndex] : null;
            if (window != null)
            {
                window.SetActive(true);
                window.transform.SetAsLastSibling();
            }
        }
        else if (transcriptChrome != null)
        {
            transcriptChrome.Open();
        }

        if (choice.Action == DialogAction.CompleteDialog)
            _day.Complete(choice.DialogId, choice.EffectName);

        RefreshChoices();
    }

    /// <summary>
    /// Closes every window on the window layer (templates are already"""),
("""        if (_fallbackBody != null)
            _fallbackBody.text = BuildFallbackBody(inst, lib, _facts, _registry);
    }

    /// <summary>
    /// The text fallback's body: the papers, the traveller's agency record (so
    /// a birth-date tell can be spotted without the Records app) and today's books.
    /// </summary>
    private static string BuildFallbackBody(CaseInstance inst, ContentLibrarySO lib, FactTable facts, CitizenRegistry registry)
    {""",
"""        if (_fallbackBody != null)
            _fallbackBody.text = BuildFallbackBody(inst, lib, _facts, _registry, _day);
    }

    /// <summary>
    /// The text fallback's body: the papers, the traveller's agency record (so
    /// a birth-date tell can be spotted without the Records app), their answers
    /// to today's questions, and the claimed place's entry in each book.
    /// </summary>
    private static string BuildFallbackBody(CaseInstance inst, ContentLibrarySO lib, FactTable facts, CitizenRegistry registry, InterviewDay day)
    {"""),
("""                sb.AppendLine($"    Origin: {record.origin}");
            }
            sb.AppendLine();
        }

        if (lib != null && lib.ReferenceBooks.Count > 0)
        {
            sb.AppendLine("— REFERENCE BOOKS (cross-check) —");
            foreach (ReferenceBookSO book in lib.ReferenceBooks)
            {
                if (book == null)
                    continue;
                sb.AppendLine($"[{book.displayName}]");
                if (facts != null)
                    foreach (FactRow row in facts.Rows(book.category))
                        sb.AppendLine($"    {row.OriginLabel}: {row.Value}");
            }
        }""",
"""                sb.AppendLine($"    Origin: {record.origin}");
            }
            sb.AppendLine();

            if (day != null)
            {
                sb.AppendLine("— INTERVIEW —");
                string eraId = inst.claimedEra != null ? inst.claimedEra.id : null;
                foreach (InterviewQuestion q in day.Questions)
                {
                    InterviewAnswer answer = inst.answers.Find(a => a.category == q.category);
                    if (answer == null)
                        continue;
                    sb.AppendLine(InterviewScript.PromptLine(q, eraId).Text);
                    sb.AppendLine($"    {inst.visitorGivenName}: {InterviewScript.AnswerLine(q, eraId, answer).Text}");
                }
                sb.AppendLine();
            }
        }

        if (lib != null && lib.ReferenceBooks.Count > 0)
        {
            sb.AppendLine("— REFERENCE (claimed place) —");
            string nationId = inst != null && inst.claimedNation != null ? inst.claimedNation.id : null;
            string eraId = inst != null && inst.claimedEra != null ? inst.claimedEra.id : null;
            foreach (ReferenceBookSO book in lib.ReferenceBooks)
                if (book != null)
                    sb.AppendLine($"{book.displayName}: {(facts != null ? facts.Get(nationId, eraId, book.category) : null) ?? "(no entry)"}");
        }"""),
])

apply(W + r'\Assets\Scripts\UI\CompareController.cs', [
("""/// the player can spot a mismatch themselves — no automatic verdict. A third
/// click starts a new comparison. Document field rows and reference-book entry
/// rows call <see cref="Select"/>.""",
"""/// the player can spot a mismatch themselves — no automatic verdict. A third
/// click starts a new comparison. Document field rows, reference-book entry
/// rows, Citizen Records rows and interview transcript answer rows call
/// <see cref="Select"/>."""),
])

apply(W + r'\Assets\Scripts\UI\InteractionPanelController.cs', [
("""/// <summary>
/// The intercom: a vertical list of actions the player can issue to the
/// traveller. Actions are provided per case by the investigation controller;
/// the panel only renders buttons (future actions — interrogation questions,
/// photo capture — plug in as more list entries).
/// </summary>""",
"""/// <summary>
/// The intercom: a vertical list of actions the player can issue to the
/// traveller. Actions are the current interview node's choices (requests,
/// questions, dialog replies), supplied per step by the investigation
/// controller; the panel only renders buttons.
/// </summary>"""),
])

apply(W + r'\Assets\Scripts\Shift\ShiftScoring.cs', [
("""            ? "Deviation denied without documented evidence. Scan the papers next time."
""",
"""            ? "Deviation denied without documented evidence. Log a deviation from the papers or the traveller's answers before denying."
"""),
])
```

- [ ] **Step 4: GameManager builds and injects the day's interview** — save as `SCRATCH/p3_t14_game.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Scripts\GameManager.cs', [
("""        // Create the case factory from the content library and today's facts.
        _caseFactory = new CaseFactory(contentLibrary, facts);

        // Generate all cases up-front (seeded: same run + same day = same travellers).
        _dayCases = _caseFactory.GenerateDayCases(dayPlan, _worldState, seed);

        // Investigation: surface today's travel directives (rules to deny), the
        // agency's citizen records for today's visitors, and today's facts.
        if (investigationUI != null)
        {
            investigationUI.SetDirectives(dayPlan.ActiveTravelRules);
            investigationUI.SetCitizenRegistry(CaseFactory.BuildRegistry(_dayCases));
            investigationUI.SetFacts(facts);
        }""",
"""        // Create the case factory from the content library and today's facts.
        _caseFactory = new CaseFactory(contentLibrary, facts);

        // Today's interview, fixed at day start: the askable questions, which of
        // them may carry a spoken tell, and the offered dialogs.
        InterviewDay interview = BuildInterviewDay();

        // Generate all cases up-front (seeded: same run + same day + same
        // interview wiring = same travellers). Where nothing spoken can be read,
        // no answer is computed and no tell is spoken.
        bool spoken = investigationUI != null && investigationUI.InterviewReachable;
        _dayCases = _caseFactory.GenerateDayCases(dayPlan, _worldState, seed,
            spoken ? interview.AskableCategories : System.Array.Empty<ClueCategory>(),
            spoken ? interview.AnswerTellCategories : System.Array.Empty<ClueCategory>());
        Debug.Log($"[GameManager] Interview: spoken={spoken}, askable=[{string.Join(", ", interview.AskableCategories)}], spoken tells may come from [{string.Join(", ", interview.AnswerTellCategories)}], dialogs offered={interview.OfferedDialogs().Count}.");

        // Investigation: surface today's travel directives (rules to deny), the
        // agency's citizen records for today's visitors, today's facts and interview.
        if (investigationUI != null)
        {
            investigationUI.SetDirectives(dayPlan.ActiveTravelRules);
            investigationUI.SetCitizenRegistry(CaseFactory.BuildRegistry(_dayCases));
            investigationUI.SetFacts(facts);
            investigationUI.SetInterviewDay(interview);
        }"""),
("""    /// <summary>
    /// Unsubscribes to prevent event leaks on scene unload / play mode exit.
    /// </summary>""",
"""    /// <summary>
    /// Today's interview from the content library and the day-start world
    /// (TimelineService.BuildInterviewDay; InterviewDay decides what is askable
    /// and offered). Logs every structurally broken dialog as an error.
    /// </summary>
    private InterviewDay BuildInterviewDay()
    {
        InterviewDay interview = TimelineService.BuildInterviewDay(contentLibrary, _worldState, _ledger);
        foreach (string problem in interview.ContentProblems)
            Debug.LogError($"[GameManager] {problem} Run Tools > TimeDesk > Generate World, then Validate Content Library.");
        return interview;
    }

    /// <summary>
    /// Unsubscribes to prevent event leaks on scene unload / play mode exit.
    /// </summary>"""),
])

# The one projection of the library's gates onto the day-start world; GameManager
# and the Unity verification both build the day's interview with it.
apply(W + r'\Assets\Scripts\Timeline\TimelineService.cs', [
("""/// - NightlyResolve: recompute dominance tiers, fire triggers, expire effects,
///   build the deterministic "tomorrow package" (run at sleep, before day++).
""",
"""/// - NightlyResolve: recompute dominance tiers, fire triggers, expire effects,
///   build the deterministic "tomorrow package" (run at sleep, before day++).
/// - BuildInterviewDay: the day's interview, its questions and dialogs gated
///   on a snapshot of the day-start world (run at day start).
"""),
("""    /// <summary>
    /// Returns true if every condition on the trigger passes (Gates.AllPass):""",
"""    /// <summary>
    /// The day's interview from the library's questions and dialogs and the
    /// day-start world (glue only; InterviewDay decides what is askable and
    /// offered): each item's conditions projected with ToGates, and one
    /// snapshot of the world holding the scores every question's and dialog's
    /// conditions read. Null library entries are skipped.
    /// </summary>
    public static InterviewDay BuildInterviewDay(ContentLibrarySO lib, WorldState world, ShiftLedger ledger)
    {
        var conditions = new List<TriggerCondition>();
        var questions = new List<Gated<InterviewQuestion>>();
        foreach (QuestionSO q in lib.Questions)
        {
            if (q == null)
                continue;
            if (q.conditions != null)
                conditions.AddRange(q.conditions);
            questions.Add(new Gated<InterviewQuestion>(q.question, ToGates(q.conditions)));
        }

        var dialogs = new List<Gated<AuthoredDialog>>();
        foreach (DialogSO d in lib.Dialogs)
        {
            if (d == null)
                continue;
            if (d.conditions != null)
                conditions.AddRange(d.conditions);
            dialogs.Add(new Gated<AuthoredDialog>(d.dialog, ToGates(d.conditions)));
        }

        return new InterviewDay(lib.Interview, questions, dialogs, Snapshot(world, conditions), ledger);
    }

    /// <summary>
    /// Returns true if every condition on the trigger passes (Gates.AllPass):"""),
])
```

- [ ] **Step 5: Compile check and run**

Expected: exit 0 / exit 0; `passed 336, failed 0`.

- [ ] **Step 6: Check the retired wording is gone**

Run: `cd /e/unity/NOPE-feat-clock && grep -rn "Next subject for reassignment\|I request passage home to {originLabel}\|Scan the papers next time\|var actions = new List<InteractionAction>();" Assets --include=*.cs`
Expected: exactly one hit, `InvestigationUIController.cs` inside `RefreshChoices` (the interview's own action list); none of the three retired strings.

- [ ] **Step 7: Update the behaviour contract** — save as `SCRATCH/p3_t14_features.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\docs\FEATURES.md', [
("""a separate stream for legacy clues and a per-traveller lie stream (`Seeds.ForLies`); day seed formula unchanged (seeding tested: `SeedsTests`, `SeededRandomTests`, `WeightedRandomTests`; whole-day determinism is checked in Unity, not by the EditMode suite)""",
"""a separate stream for legacy clues, a per-traveller lie stream (`Seeds.ForLies`) and a per-traveller dialog stream (`Seeds.ForDialog`, small-talk pick); the day's lie draws depend only on the run and the day (and the interview wiring), never on purchases or flags; day seed formula unchanged (seeding tested: `SeedsTests`, `SeededRandomTests`, `WeightedRandomTests`; tell eligibility tested: `InterviewDayTests`; whole-day determinism is checked in Unity, not by the EditMode suite)"""),
("""so records never reveal a true home; the rich desk warns once at start when the evidence system is active but Citizen Records is not wired (birth-date tells need it)""",
"""so records never reveal a true home; the rich desk warns once at start when the evidence system is active but Citizen Records is not wired (birth-date tells need it); likewise the rich desk warns once at start when the intercom, the interview transcript or its window chrome is not wired: that day questions are hidden, no answer is computed and no tell is spoken, and the intercom offers only document requests"""),
("""A liar really comes from another of today's places (any country, any of today's eras) and travels under the claimed place's cover identity, but the papers leak tells carrying the true home's value: a Currency, Language or Technology tell rewrites every field of that category; a birth-date tell keeps the record's day and month with a year from the true home's birth years. Each liar leaks the day's tell count (`DayPlanSO` "tell count", 1 on days 1–3), capped by the categories that can carry a tell for them. The liar chance is the blueprint's contradiction chance plus the slot-machine modifier and forgery-risk effects; rule violators and legendaries never lie; a rolled liar with no possible tell stays honest and a warning is logged (tell selection, eligibility and the may-lie exemptions tested: `LiesTests`, `ForgeryTests`; the liar chance, cover records and whole-day behaviour are checked in Unity, not by the EditMode suite)""",
"""A liar really comes from another of today's places (any country, any of today's eras) and travels under the claimed place's cover identity, but the disguise leaks tells carrying the true home's value, on the papers (a Currency, Language or Technology tell rewrites every field of that category; a birth-date tell keeps the record's day and month with a year from the true home's birth years) or in speech (their answer to one of today's day-gated questions gives the true home's value while the papers show the cover). A category leaks on one channel only. The day plan's tell channels set where tells may appear (day 1 papers only; days 2–3 papers and answers); a spoken tell needs its question askable that day, gated by day alone, and a book (or the Citizen Record for a birth date). Each liar leaks the day's tell count (`DayPlanSO` "tell count", 1 on days 1–3), capped by the categories that can carry a tell for them. The liar chance is the blueprint's contradiction chance plus the slot-machine modifier and forgery-risk effects; rule violators and legendaries never lie; a rolled liar with no possible tell stays honest and a warning is logged (tell selection, channels, eligibility and the may-lie exemptions tested: `LiesTests`, `ForgeryTests`, `InterviewDayTests`; answers tested: `InterviewTests`; the liar chance, cover records and whole-day behaviour are checked in Unity, not by the EditMode suite)"""),
("""- [ ] Traveller gender is recorded from the claimed place's name lists ("Marcus II" counts as Marcus; legendaries and "Subject #n" are unknown); it is not shown yet (tested: `TravellerGendersTests`, `NameRosterTests`)""",
"""- [ ] Traveller gender is recorded from the claimed place's name lists ("Marcus II" counts as Marcus; legendaries and "Subject #n" are unknown); the desk's opener uses it for the honorific (sir/madam/traveller) (tested: `TravellerGendersTests`, `NameRosterTests`, `InterviewTests`)"""),
("""- [ ] Intercom interaction panel: per-case traveller actions — "Request Travel Passport", "Request Transit Permit" (more actions planned: interrogation, photo capture)""",
"""- [ ] Intercom = the interview: every entry is a dialog choice. Hub: "Request <document>" (repeatable; the traveller hands it over and the window opens), "Ask about home >" ("< Back" first, then today's questions, one-shot per traveller, and small talk) and today's narrative dialogs; content never offers more choices than the intercom shows (8), and the way back and the requests come first (hub and menus tested: `InterviewScriptTests`, `DialogRunnerTests`; locked questions hidden and dialogs offered tested: `InterviewDayTests`; capacity rules tested: `InterviewScriptTests`)
- [ ] Questions: Currency, Language, Device from day 1; Capital from day 2; Ruler from day 3 (each announced in that morning's paper); Date of birth once Interview Protocols is owned (announced the next morning). Unlocks are fixed at the start of the day. Only day-gated questions can carry a spoken tell: the birth-date question is always answered with the registered date, a hint against a passport birth-date tell (gates tested: `GatesTests`; availability and tell eligibility tested: `InterviewDayTests`)
- [ ] Honest answers equal the claimed place's book entries and the Citizen Record; a liar answers with the cover except for a spoken tell; small talk comes from the claimed place (or its era) and is never evidence (answers tested: `InterviewTests`)"""),
("""- [ ] Scanner = Deviation Report: true contradictions auto-register (tested: `DiscrepancyLogTests`)
  - [ ] Mismatch proof: a liar's tell ≠ claimed-era reference entry
  - [ ] Match proof: a liar's tell = a *different* era/nation's entry (origin proof); it names the traveller's true home
  - [ ] Record proof: a birth-date tell ≠ agency citizen record (tested)
  - [ ] Junk comparisons never register (wrong category, foreign-era mismatch, honest fields)
  - [ ] One discrepancy per category; a second proof of a documented category shows "ALREADY DOCUMENTED" in the compare bar and adds nothing; cleared per case; window auto-opens on first find
  - [ ] Compare bar flips to a red "DEVIATION LOGGED — …" verdict when evidence registers (never a green MATCH)
  - [ ] Reports label Geography CAPITAL, Politics RULER, Technology DEVICE and birth dates BIRTH DATE (tested: `DiscrepancyLogTests`)""",
"""- [ ] Scanner = Deviation Report: true contradictions auto-register from a document field or a traveller's answer (tested: `DiscrepancyLogTests`)
  - [ ] Mismatch proof: a liar's tell (printed or spoken) ≠ claimed-era reference entry
  - [ ] Match proof: a liar's tell (printed or spoken) = a *different* era/nation's entry (origin proof); it names the traveller's true home
  - [ ] Record proof: a birth-date tell (printed or spoken) ≠ agency citizen record (tested)
  - [ ] Junk comparisons never register (wrong category, foreign-era mismatch, honest fields and answers, answer vs papers, answer vs answer)
  - [ ] One discrepancy per category from any source; a second proof of a documented category shows "ALREADY DOCUMENTED" in the compare bar and adds nothing; cleared per case; window auto-opens on first find
  - [ ] Compare bar flips to a red "DEVIATION LOGGED — …" verdict when evidence registers (never a green MATCH)
  - [ ] Reports say where the tell was ("papers show …" / "traveller said …") and label Geography CAPITAL, Politics RULER, Technology DEVICE and birth dates BIRTH DATE (tested: `DiscrepancyLogTests`)"""),
("""- [ ] Fallback text-mode investigation when the rich desk isn't built (papers, the traveller's agency record and today's books)""",
"""- [ ] Fallback text-mode investigation when the rich desk isn't built (papers, the traveller's agency record, their answers to today's questions, and the claimed place's entry in each book)"""),
("""- [ ] The clock pauses only while a citation slip is shown""",
"""- [ ] The clock pauses only while a citation slip is shown (the interview takes real time and costs nothing else)"""),
("""- [ ] Only provable tells are generated (tested: `ForgeryTests`, `BirthDatesTests`): a place fact is a tell only when a reference book covers it and the true home's value differs from the claim's and belongs to no other of today's places (so the books prove it and the origin proof names the home); a birth-date tell keeps day and month and takes a year from the true home's birth years, never the record's (provable via citizen records); names, capitals and rulers are never tells""",
"""- [ ] Only provable tells are generated (tested: `ForgeryTests`, `LiesTests`, `BirthDatesTests`, `InterviewDayTests`): a place fact is a tell only when a reference book covers it and the true home's value differs from the claim's and belongs to no other of today's places (so the books prove it and the origin proof names the home); it shows on the papers only when a paper prints it, in speech only when its question is askable that day and gated by day alone; a birth-date tell keeps day and month and takes a year from the true home's birth years, never the record's (provable via citizen records); names are never tells"""),
])
```

- [ ] **Step 8: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/UI/TranscriptWindowController.cs Assets/Scripts/UI/TranscriptWindowController.cs.meta Assets/Scripts/CaseInstance.cs Assets/Scripts/CaseFactory.cs Assets/Scripts/UI/InvestigationUIController.cs Assets/Scripts/UI/CompareController.cs Assets/Scripts/UI/InteractionPanelController.cs Assets/Scripts/Shift/ShiftScoring.cs Assets/Scripts/GameManager.cs Assets/Scripts/Timeline/TimelineService.cs docs/FEATURES.md && git commit -F - <<'EOF'
feat(interview): the intercom becomes an interview

Every traveller answers today's askable questions from the same values as
their papers; a liar may leak a tell in an answer to a day-gated question
while the papers show the cover. The desk plays the interview graph on the
intercom (requests, the ask menu with "< Back" first, today's dialogs) with
the Case Notes transcript, whose answer rows are compare-clickable. The
opener greets by gender and the claim sentence is content. GameManager
injects the day's InterviewDay; without a wired transcript no answer is
computed and no tell is spoken. The fallback lists the interview and the
claimed place's book entries; the citation and scanner hint cover answers.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 15: Dialog consequences at the end of the shift

Glue for spec X5, R24, §1.6, §2.10: at the end of the shift, before the save, `ApplyDialogOutcomes` sets each one-shot dialog's done flag and activates each named effect once (instant ops now, start day tomorrow so its lines reach the next paper); when an effect was applied, the HUD refreshes and endings are checked, and a matched ending leads to the title scene instead of Home.

**Files:**
- Modify: `Assets/Scripts/GameManager.cs`, `docs/FEATURES.md`

- [ ] **Step 1: Implement** — save as `SCRATCH/p3_t15.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Scripts\GameManager.cs', [
("""    /// <summary>
    /// Called when the last case of the day resolves. Saves the run.
    /// Phase 3 replaces the log with the results screen; Phase 4 leads into the Home scene.
    /// </summary>""",
"""    /// <summary>
    /// Called when the last case of the day resolves: applies the narrative
    /// dialogs' consequences (then refreshes the HUD and checks endings), saves
    /// the run, shows the shift report and leads into the Home scene (or the
    /// title scene when an ending was reached).
    /// </summary>"""),
("""        if (RunManager.HasInstance)
        {
            // Yesterday's slot modifiers were consumed by today's shift.
            RunManager.Instance.ResetTomorrowModifiers();
            RunManager.Instance.SaveNow();
        }

        // Show the shift report, then hand off to the home phase. The report is
        // read in the booth (like the morning briefing), so pull back first.
        if (dayFlowUI != null)
        {
            if (officeView != null)
                officeView.FocusOffice();

            Debug.Log("[GameManager] <<< Exiting HandleDayCompleted (showing results panel, then Home).");
            dayFlowUI.ShowResults(_worldState, _ledger, HandleGoHome);
        }
        else
        {
            if (officeUI != null)
                officeUI.SetResultText($"Day {_worldState.day} complete.");

            Debug.Log("[GameManager] <<< Exiting HandleDayCompleted (no results panel, going Home directly).");
            HandleGoHome();
        }
    }""",
"""        // Narrative dialogs' consequences apply now, before the save, so a
        // Continue replay of this day can never apply them twice.
        EndingSO ending = null;
        if (ApplyDialogOutcomes())
        {
            if (officeUI != null)
                officeUI.UpdateHud(_worldState);

            if (_gameConfig != null)
            {
                ending = EndingService.Evaluate(_worldState, contentLibrary, _gameConfig);
                if (ending != null)
                {
                    Debug.Log($"[GameManager] Ending check after dialog consequences: matched '{ending.id}' ({ending.displayName}).");
                    _worldState.endingId = ending.id;
                }
            }
        }

        if (RunManager.HasInstance)
        {
            // Yesterday's slot modifiers were consumed by today's shift.
            RunManager.Instance.ResetTomorrowModifiers();
            RunManager.Instance.SaveNow();
        }

        System.Action next = ending != null ? new System.Action(HandleEndingReached) : HandleGoHome;

        // Show the shift report, then hand off to the home phase (or the title
        // scene after an ending). The report is read in the booth (like the
        // morning briefing), so pull back first.
        if (dayFlowUI != null)
        {
            if (officeView != null)
                officeView.FocusOffice();

            Debug.Log($"[GameManager] <<< Exiting HandleDayCompleted (showing results panel, then {(ending != null ? "the title scene" : "Home")}).");
            dayFlowUI.ShowResults(_worldState, _ledger, next);
        }
        else
        {
            if (officeUI != null)
                officeUI.SetResultText($"Day {_worldState.day} complete.");

            Debug.Log($"[GameManager] <<< Exiting HandleDayCompleted (no results panel, going to {(ending != null ? "the title scene" : "Home")} directly).");
            next();
        }
    }

    /// <summary>
    /// Applies what the shift's completed dialogs decided (DialogOutcomes):
    /// sets each one-shot dialog's done flag, and activates each named effect
    /// once, with its instant ops now and a start day of tomorrow (so its
    /// briefing and news lines reach the next morning's paper). Returns true
    /// when any effect was applied.
    /// </summary>
    private bool ApplyDialogOutcomes()
    {
        if (_ledger == null || _worldState == null)
            return false;

        foreach (string flag in DialogOutcomes.FlagsToSet(_ledger.dialogOutcomes))
            _worldState.SetFlag(flag);

        bool applied = false;
        foreach (DialogOutcome outcome in DialogOutcomes.EffectsToApply(_ledger.dialogOutcomes))
        {
            EffectSO fx = contentLibrary.GetEffectByAssetName(outcome.effectName);
            if (fx == null)
            {
                Debug.LogWarning($"[GameManager] Dialog '{outcome.dialogId}' names effect '{outcome.effectName}', which ContentLibrary_Main does not list; add it to the library's effects.");
                continue;
            }

            TimelineService.ActivateEffect(_worldState, fx, $"Dialog: {outcome.dialogId}", _worldState.day + 1, fx.defaultDurationDays, applyInstantOps: true);
            applied = true;
        }

        return applied;
    }"""),
])

apply(W + r'\docs\FEATURES.md', [
("""- [ ] Endings evaluated after every verdict (`EndingService`); firing threshold on stability; bankruptcy threshold""",
"""- [ ] Endings evaluated after every verdict, and at the end of a shift that applied dialog consequences (`EndingService`); firing threshold on stability; bankruptcy threshold"""),
("""- [ ] Shift ledger (tested: `ShiftLedgerTests`); citation slip pauses the day (and the shift clock) until acknowledged""",
"""- [ ] Shift ledger (tested: `ShiftLedgerTests`); citation slip pauses the day (and the shift clock) until acknowledged
- [ ] Narrative dialogs may carry a consequence: an effect limited to instant ops (flags, counters, money, stability, upgrades, scores) and briefing/news lines; the generator and validator reject timed modifiers (pay, liar, legendary, shop, visitor and blueprint bonuses, cues) in a dialog's effect. A dialog with a consequence is one-shot per run. Consequences apply at the end of the shift, before the save: instant ops at once (the HUD refreshes and endings are checked), briefing and news lines in the next morning's paper. A dialog asset whose structure is broken (a node that cannot reach an ending, an effect on a non-ending choice, too many choices) is never offered and an error is logged (recording, one-shot memory and apply-once tested: `InterviewDayTests`; structure tested: `InterviewScriptTests`; the op rule tested: `EffectOpsTests`; the effect application is Assembly-CSharp and checked in Unity)"""),
])
```

- [ ] **Step 2: Compile check and run**

Expected: exit 0 / exit 0; `passed 336, failed 0`.

- [ ] **Step 3: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/GameManager.cs docs/FEATURES.md && git commit -F - <<'EOF'
feat(interview): dialog consequences apply at the end of the shift

Before the end-of-shift save, the shift's completed dialogs set their done
flags (one-shot dialogs) and activate their effects once, with instant ops
now and a start day of tomorrow, so a Continue replay can never apply them
twice. The HUD refreshes and endings are checked when an effect applied.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 16: The builder builds the interview desk

Spec §2.12, Q9/X4, Q18, R9, R10, R25: the intercom's `Actions` gets a `RectMask2D` and its layout numbers become named constants that also compute how many choices it shows (8, from the height of the one `ReferenceResolution` both canvas scalers now read), checked against the content's `interview.menuCapacity`; the transcript window ("Case Notes: Interview", 620×460 at (220, 70), 8 rows per page, the speaker in a 150 px column and the sentence wrapping 12–18 pt) takes the retired Clue Log slot and is wired into the investigation controller; desktop icons get their real upgrade ids (reported when unknown) and Dialect becomes an honest placeholder. The transcript window is destroyed and rebuilt on every run, like Records: its row template always gets the transcript layout, and the window layer's order is the same after every build (the semantic idempotence check of Task 17 compares it). The scene is rebuilt and committed in Task 17.

**Files:**
- Modify: `Assets/Editor/OfficeSceneUIBuilder.cs`, `docs/FEATURES.md`

- [ ] **Step 1: Implement** — save as `SCRATCH/p3_t16.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
from subsn import replace_n
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Editor\OfficeSceneUIBuilder.cs', [
("""/// - Investigation desk: claim banner, directives, draggable/multi-page document
///   windows, a reference-book shelf with openable book windows, a visual
///   compare bar, and Accept/Deny buttons  [InvestigationUIController + CompareController]""",
"""/// - Investigation desk: claim banner, directives, draggable/multi-page document
///   windows, a reference-book shelf with openable book windows, the intercom
///   and the interview transcript, a visual compare bar, and Accept/Deny
///   buttons  [InvestigationUIController + CompareController]"""),
("""    private static readonly Color Ink = new Color(0.1f, 0.09f, 0.08f, 1f);
""",
"""    private static readonly Color Ink = new Color(0.1f, 0.09f, 0.08f, 1f);

    /// <summary>Padding on every side of a vertical list (AddVLayout).</summary>
    private const int VLayoutPadding = 6;

    /// <summary>The reference resolution of both canvas scalers; every layout is authored against it (the intercom's fit reads its height).</summary>
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    /// <summary>Transcript rows per page: the book row height (34 px) and spacing fit 8 in the window's row area.</summary>
    private const int TranscriptRowsPerPage = 8;
"""),
# --- intercom: named layout numbers, a mask, and the capacity they give ---
("""        // Intercom: per-case traveller actions ("Request Passport", ...); the
        // action list is provided at runtime by InvestigationUIController.
        Transform intercom = Panel(investRoot, "IntercomPanel", new Vector2(0.79f, 0.36f), new Vector2(0.995f, 0.85f), Vector2.zero, Vector2.zero, new Color(0.07f, 0.1f, 0.16f, 0.92f));
        TMP_Text intercomTitle = Text(intercom, "Title", "INTERCOM", 20, TextAlignmentOptions.Center, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.99f), new Color(0.7f, 0.85f, 1f, 1f));
        intercomTitle.fontStyle = FontStyles.Bold;
        Transform intercomActions = Panel(intercom, "Actions", new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.86f), Vector2.zero, Vector2.zero, null);
        AddVLayout(intercomActions, 6f);
        Button actionTemplate = MakeButton(intercomActions, "ActionButtonTemplate", "Request", Vector2.zero, Vector2.one, new Color(0.16f, 0.28f, 0.42f, 1f));
        SetLayoutHeight(actionTemplate, 44f);""",
"""        // Intercom: the interview's choices (document requests, questions,
        // dialog replies), provided at runtime by InvestigationUIController.
        // The layout numbers also give how many choices it shows at once.
        const float intercomMinY = 0.36f, intercomMaxY = 0.85f;
        const float actionsMinY = 0.02f, actionsMaxY = 0.86f;
        const float actionSpacing = 6f, actionHeight = 44f;
        Transform intercom = Panel(investRoot, "IntercomPanel", new Vector2(0.79f, intercomMinY), new Vector2(0.995f, intercomMaxY), Vector2.zero, Vector2.zero, new Color(0.07f, 0.1f, 0.16f, 0.92f));
        TMP_Text intercomTitle = Text(intercom, "Title", "INTERCOM", 20, TextAlignmentOptions.Center, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.99f), new Color(0.7f, 0.85f, 1f, 1f));
        intercomTitle.fontStyle = FontStyles.Bold;
        Transform intercomActions = Panel(intercom, "Actions", new Vector2(0.04f, actionsMinY), new Vector2(0.96f, actionsMaxY), Vector2.zero, Vector2.zero, null);
        AddVLayout(intercomActions, actionSpacing);
        if (intercomActions.GetComponent<RectMask2D>() == null)
            intercomActions.gameObject.AddComponent<RectMask2D>(); // an overflow is clipped, never drawn over the desk
        Button actionTemplate = MakeButton(intercomActions, "ActionButtonTemplate", "Request", Vector2.zero, Vector2.one, new Color(0.16f, 0.28f, 0.42f, 1f));
        SetLayoutHeight(actionTemplate, actionHeight);
        float actionsHeight = (intercomMaxY - intercomMinY) * ReferenceResolution.y * (actionsMaxY - actionsMinY);
        int intercomFit = Mathf.FloorToInt((actionsHeight - 2 * VLayoutPadding + actionSpacing) / (actionHeight + actionSpacing));"""),
# --- the transcript window, in the retired Clue Log slot ---
("""        soRecords.ApplyModifiedProperties();
        BuildDesktopIcon(bookShelf, "IconRecords", "Records", recordsChrome, "");
""",
"""        soRecords.ApplyModifiedProperties();
        BuildDesktopIcon(bookShelf, "IconRecords", "Records", recordsChrome, "");

        // Case Notes: Interview — the current traveller's transcript, in the
        // retired Clue Log placeholder's slot. Rebuilt fresh each run (like
        // Records), so its row template always has the transcript layout and
        // the window layer keeps a stable order. Opened by every interview
        // choice but a document request; answer rows are compare-clickable.
        DestroyChildIfPresent(windowLayer, "IconClueLogWindow");
        DestroyChildIfPresent(windowLayer, "TranscriptWindow");
        Transform transcriptWin = Panel(windowLayer, "TranscriptWindow", Center, Center, new Vector2(220f, 70f), new Vector2(620f, 460f), Paper);
        WindowShell transcriptShell = BuildWindowShell(transcriptWin, "Case Notes: Interview");
        TranscriptWindowController transcript = transcriptWin.gameObject.AddComponent<TranscriptWindowController>();
        var soTranscript = new SerializedObject(transcript);
        SetRef(soTranscript, "titleText", transcriptShell.title);
        SetRef(soTranscript, "pageText", transcriptShell.page);
        SetRef(soTranscript, "prevButton", transcriptShell.prev);
        SetRef(soTranscript, "nextButton", transcriptShell.next);
        SetRef(soTranscript, "entryRowsRoot", transcriptShell.rowsRoot);
        SetRef(soTranscript, "entryRowTemplate", transcriptShell.rowTemplate);
        soTranscript.FindProperty("entriesPerPage").intValue = TranscriptRowsPerPage;
        soTranscript.ApplyModifiedProperties();
        ApplyTranscriptRowLayout(transcriptShell.rowTemplate);
        OSWindowChrome transcriptChrome = transcriptWin.GetComponent<OSWindowChrome>();
        transcriptWin.gameObject.SetActive(false);
        BuildDesktopIcon(bookShelf, "IconClueLog", "Clue Log", transcriptChrome, "");
"""),
# --- capacity check against the content's knob ---
("""        if (library == null) Debug.LogWarning("[TimeDesk] No ContentLibrarySO found — assign GameManager.contentLibrary manually.");
""",
"""        if (library == null) Debug.LogWarning("[TimeDesk] No ContentLibrarySO found — assign GameManager.contentLibrary manually.");
        if (library != null && library.Interview != null && library.Interview.menuCapacity > intercomFit)
            Debug.LogError($"[TimeDesk] The intercom fits {intercomFit} choices, but the content library's interview menu capacity is {library.Interview.menuCapacity}; lower interview.menuCapacity in world_source.json or enlarge the intercom.");
"""),
("""        BuildDesktopShell(canvas, bookShelf, windowLayer);""",
"""        BuildDesktopShell(canvas, bookShelf, windowLayer, library);"""),
("""        SetRef(soInvest, "recordsWindow", records);
        soInvest.ApplyModifiedProperties();""",
"""        SetRef(soInvest, "recordsWindow", records);
        SetRef(soInvest, "transcriptWindow", transcript);
        SetRef(soInvest, "transcriptChrome", transcriptChrome);
        soInvest.ApplyModifiedProperties();"""),
("""        Debug.Log("[TimeDesk] Office investigation desk built and wired (HUD, citation, briefing/results, claim, document + book windows, compare, Accept/Deny, GameManager, DaySystem). Save the scene.");""",
"""        Debug.Log("[TimeDesk] Office investigation desk built and wired (HUD, citation, briefing/results, claim, document + book windows, intercom + interview transcript, compare, Accept/Deny, GameManager, DaySystem). Save the scene.");"""),
("""    private static GameObject BuildRowTemplate(Transform parent)
    {""",
"""    /// <summary>
    /// The transcript's row layout, re-applied on every build: the speaker in a
    /// fixed 150 px column that ellipsizes, the sentence in the rest, wrapping
    /// onto a second line and auto-sizing 12-18 pt inside the fixed 34 px row
    /// (its width never follows its text).
    /// </summary>
    private static void ApplyTranscriptRowLayout(GameObject row)
    {
        HorizontalLayoutGroup h = row.GetComponent<HorizontalLayoutGroup>();
        if (h != null)
            h.childForceExpandWidth = false;

        ConfigureTranscriptText(row.transform.Find("Label"), 150f, 0f, TextWrappingModes.NoWrap, TextOverflowModes.Ellipsis);
        ConfigureTranscriptText(row.transform.Find("Value"), 0f, 1f, TextWrappingModes.Normal, TextOverflowModes.Overflow);
    }

    /// <summary>Sizes one transcript row text: layout width, auto-size range, wrapping and overflow.</summary>
    private static void ConfigureTranscriptText(Transform t, float width, float flexibleWidth, TextWrappingModes wrapping, TextOverflowModes overflow)
    {
        if (t == null)
            return;

        LayoutElement le = t.GetComponent<LayoutElement>();
        if (le == null)
            le = t.gameObject.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.flexibleWidth = flexibleWidth;

        TMP_Text text = t.GetComponent<TMP_Text>();
        if (text == null)
            return;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = 18f;
        text.textWrappingMode = wrapping;
        text.overflowMode = overflow;
    }

    private static GameObject BuildRowTemplate(Transform parent)
    {"""),
("""        l.spacing = spacing;
        l.padding = new RectOffset(6, 6, 6, 6);
        l.childAlignment = TextAnchor.UpperCenter;""",
"""        l.spacing = spacing;
        l.padding = new RectOffset(VLayoutPadding, VLayoutPadding, VLayoutPadding, VLayoutPadding);
        l.childAlignment = TextAnchor.UpperCenter;"""),
# --- the desktop shell: real upgrade ids, an honest Dialect, no Clue Log placeholder ---
("""    /// <summary>
    /// Builds the fake-OS desktop shell: a left column of icons (some unlock-gated)
    /// that open placeholder windows with min/max/close chrome, plus a Start menu
    /// (Settings + Power) wired to a DesktopShell on the canvas. Idempotent.
    /// </summary>
    private static void BuildDesktopShell(Canvas canvas, Transform iconGrid, Transform windowLayer)
    {""",
"""    /// <summary>
    /// Builds the fake-OS desktop shell: a left column of icons (some unlock-gated
    /// by a library upgrade id, reported when the library does not know it) that
    /// open placeholder windows with min/max/close chrome, plus a Start menu
    /// (Settings + Power) wired to a DesktopShell on the canvas. Idempotent.
    /// </summary>
    private static void BuildDesktopShell(Canvas canvas, Transform iconGrid, Transform windowLayer, ContentLibrarySO library)
    {"""),
("""        // Only genuinely-new apps get placeholder windows; existing
        // Passport/Permit documents, reference books, and Compare are launched by
        // the investigation icon grid (real windows with real data). Scanner and
        // Directives are built separately in Build() and wired to controllers.
        var apps = new (string name, string label, string title, string body, string upgrade)[]
        {
            ("IconInternet", "Internet", "Internet - News",  "Today's news feed. (placeholder)", ""),
            ("IconLexicon",  "Lexicon",  "Lexicon",          "Wikipedia-style era glossary. (placeholder)", "ArchiveAccess"),
            ("IconDialect",  "Dialect",  "Dialect Filter",   "Highlights anachronistic phrases. (upgrade)", "DialectFilter"),
            ("IconMaterial", "Material", "Material Scanner", "Flags tech/materials beyond the claimed era. (upgrade)", "AdvancedScanner"),
            ("IconClueLog",  "Clue Log", "Case Notes",       "Clues & contradictions for the current case. (placeholder)", ""),
            ("IconNotes",    "Notes",    "Sticky Notes",     "Your notes. (placeholder)", ""),
        };

        foreach (var a in apps)
        {
            OSWindowChrome w = BuildOSWindow(windowLayer, a.name + "Window", a.title, a.body);
            BuildDesktopIcon(iconGrid, a.name, a.label, w, a.upgrade);
        }""",
"""        // Only genuinely-new apps get placeholder windows; existing
        // Passport/Permit documents, reference books, and Compare are launched by
        // the investigation icon grid (real windows with real data). Scanner,
        // Directives, Records and the interview transcript (Clue Log) are built
        // separately in Build() and wired to controllers. Upgrade ids are
        // UpgradeSO.id values.
        var apps = new (string name, string label, string title, string body, string upgrade)[]
        {
            ("IconInternet", "Internet", "Internet - News",  "Today's news feed. (placeholder)", ""),
            ("IconLexicon",  "Lexicon",  "Lexicon",          "Wikipedia-style era glossary. (placeholder)", "archive_access"),
            ("IconDialect",  "Dialect",  "Dialect",          "Notes on accents and phrasing. (placeholder)", ""),
            ("IconMaterial", "Material", "Material Scanner", "Flags tech/materials beyond the claimed era. (upgrade)", "adv_scanner"),
            ("IconNotes",    "Notes",    "Sticky Notes",     "Your notes. (placeholder)", ""),
        };

        // BuildOSWindow keeps an existing window's texts, so the renamed Dialect window is rebuilt.
        DestroyChildIfPresent(windowLayer, "IconDialectWindow");

        foreach (var a in apps)
        {
            if (library != null && !string.IsNullOrEmpty(a.upgrade) && library.GetUpgradeById(a.upgrade) == null)
                Debug.LogError($"[TimeDesk] Desktop icon '{a.name}' requires unknown upgrade '{a.upgrade}' (not in the content library's upgrades); it could never unlock.");

            OSWindowChrome w = BuildOSWindow(windowLayer, a.name + "Window", a.title, a.body);
            BuildDesktopIcon(iconGrid, a.name, a.label, w, a.upgrade);
        }"""),
])

# Both canvas scalers read the one reference resolution the intercom's fit uses.
replace_n(W + r'\Assets\Editor\OfficeSceneUIBuilder.cs',
          "        scaler.referenceResolution = new Vector2(1920f, 1080f);",
          "        scaler.referenceResolution = ReferenceResolution;", 2)
```

- [ ] **Step 2: Compile check and run**

Expected: exit 0 / exit 0; `passed 336, failed 0`.

- [ ] **Step 3: Check the old icon ids and the literal resolutions are gone**

Run: `cd /e/unity/NOPE-feat-clock && grep -n "ArchiveAccess\|DialectFilter\|AdvancedScanner\|Dialect Filter\|IconClueLogWindow\|1920f, 1080f" Assets/Editor/OfficeSceneUIBuilder.cs`
Expected: two lines: the `ReferenceResolution` constant (the one `new Vector2(1920f, 1080f)`) and the `DestroyChildIfPresent(windowLayer, "IconClueLogWindow");` that removes the retired placeholder.

- [ ] **Step 4: Update the behaviour contract** — save as `SCRATCH/p3_t16_features.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\docs\FEATURES.md', [
("""- [ ] Upgrade-gated icons (Lexicon/Dialect/Material) dim until the upgrade is owned""",
"""- [ ] Upgrade-gated icons (Lexicon needs Archive Access, Material needs Advanced Scanner) dim until the upgrade is owned; the builder reports icon upgrade ids the library does not know"""),
("""- [ ] Placeholder apps: Internet, Lexicon, Dialect, Material, Clue Log, Notes""",
"""- [ ] Placeholder apps: Internet, Lexicon, Dialect, Material, Notes
- [ ] Clue Log = Case Notes: Interview, the current traveller's transcript (speaker + line, 8 per page, long lines wrap onto two, jumps to the newest page); answer rows are compare-clickable; every intercom choice except a document request opens it (questions, small talk, dialog replies, "Ask about home >" and "< Back"); it closes with every new case"""),
("""- [ ] `Tools > TimeDesk > Build Office UI` — idempotent, authoritative scene builder""",
"""- [ ] `Tools > TimeDesk > Build Office UI` — idempotent, authoritative scene builder; it reports an intercom that fits fewer choices than the content's menu capacity"""),
])
```

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Editor/OfficeSceneUIBuilder.cs docs/FEATURES.md && git commit -F - <<'EOF'
feat(builder): the interview transcript, intercom capacity and real icon ids

Build Office UI adds the Case Notes: Interview window in the Clue Log slot
(8 rows per page, wrapping sentences) and wires it; masks the intercom and
reports when it fits fewer choices than the content's menu capacity; gives
Lexicon and Material their real upgrade ids, reports unknown ones, and
turns Dialect into an honest placeholder.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 17: Generate the interview content and rebuild OfficeScene (Unity)

Spec §6 steps 1 and 5, and HOUSE_RULES ("if you change the builder, the scene must be rebuilt in Unity and committed"). One `-executeMethod` session runs Generate World twice (the second run must change nothing), checks the generated content, runs Validate Content Library, runs the generator's own checks on five edited in-memory copies of the source (nothing is written), then opens OfficeScene, runs Build Office UI twice and saves after each build, dumping the scene semantically each time (paths, active flags, component types, rects, TMP settings and every serialized value of the project's components, references as target paths). The two dumps must be equal. A third build, never saved, runs with two in-memory edits (the library's menu capacity 9, Archive Access under another id) so the builder's two reports are seen to fire (FEATURES :31 and :97); both edits are undone, the scene is reopened, and the scene, library and upgrade files must be unchanged on disk. The YAML is not compared: the builder destroys and recreates several objects on every run (window controls, the document template, newsletters, Scanner, Records, the Start menu, `IconDialectWindow` and now `TranscriptWindow`), so their fileIDs change.

**When a check fails**, first decide where the defect is:
- **In the temporary `_TimeDesk*` script** (the automation itself is wrong): fix that script, commit nothing, re-run Step 4.
- **In the product:** revert what Unity wrote (`git checkout -- Assets/Data Assets/Scenes/OfficeScene.unity`, then `git clean -fd Assets/Data/World/Interview && rm -f Assets/Data/World/Interview.meta` for the new folder and the meta Unity made beside it), fix the cause at its source (Domain first with a failing EditMode test when the rule is Domain's), pass the offline gate, commit it as `fix: …`, then re-run Step 4.

**Files:**
- Create (temporary, never committed): `Assets/Editor/_TimeDeskP3Content.cs`
- Commit: the generated content (`Assets/Data/World/Interview/*` and `Interview.meta`, `Assets/Data/World/Eras/*`, `Assets/Data/World/Places/*`, `Assets/Data/Investigation/DayPlan_Inv_Day1..3.asset`, `Assets/Data/Content Library/ContentLibrary_Main.asset`) and the rebuilt `Assets/Scenes/OfficeScene.unity`
- Output: `SCRATCH/p3_content_report.txt`, `SCRATCH/p3_scene_dump_1.txt`, `SCRATCH/p3_scene_dump_2.txt`

- [ ] **Step 1: Offline gate**

Run the compile check and the test run. Expected: exit 0 / exit 0; `passed 336, failed 0`. `git status --short` shows only the two untracked files.

- [ ] **Step 2: Check no Unity editor is open on the worktree** (PowerShell)

Run: `Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Select-Object ProcessId, CommandLine`
Expected: none on `NOPE-feat-clock`.

- [ ] **Step 3: Write the content and scene automation** (`Assets/Editor/_TimeDeskP3Content.cs`, Write tool)

```csharp
// TEMPORARY automation for piece 3 (dialog + questions); never committed.
// Generate World twice (idempotency), the generated content, Validate Content
// Library, the generator's negative checks, then Build Office UI twice on
// OfficeScene with a semantic scene dump after each build (spec section 6
// steps 1 and 5). Reports go to the scratchpad.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class TimeDeskP3Content
{
    private const string Scratch = @"C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad";
    private static readonly string ReportPath = Path.Combine(Scratch, "p3_content_report.txt");
    private const string LibraryPath = "Assets/Data/Content Library/ContentLibrary_Main.asset";
    private const string ScenePath = "Assets/Scenes/OfficeScene.unity";
    private const string BuildMenu = "Tools/TimeDesk/Build Office UI (HUD + Panels)";

    private static int _fails;

    private static void Report(string line) => File.AppendAllText(ReportPath, line + "\n");

    private static void Check(bool ok, string what)
    {
        Report((ok ? "PASS " : "FAIL ") + what);
        if (!ok)
            _fails++;
    }

    public static void Run()
    {
        File.WriteAllText(ReportPath, "start\n");
        try
        {
            // --- Step 1: content ---
            RunMenu("Tools/TimeDesk/Generate World");
            Dictionary<string, string> before = HashDataFolder();
            RunMenu("Tools/TimeDesk/Generate World");
            Dictionary<string, string> after = HashDataFolder();
            List<string> changed = before.Keys.Union(after.Keys)
                .Where(k => !before.TryGetValue(k, out string a) || !after.TryGetValue(k, out string b) || a != b)
                .ToList();
            Check(changed.Count == 0, $"idempotent second Generate World: changedFiles={changed.Count} {string.Join(", ", changed)}");

            CheckContent();

            List<string> validator = RunMenu("Tools/TimeDesk/Validate Content Library");
            Check(validator.Any(l => l.Contains("no issues found")) && !validator.Any(l => l.StartsWith("[Error]") || l.StartsWith("[Warning]")),
                  "Validate Content Library reports no issues");

            NegativeChecks();

            // --- Step 5: the builder, twice, with a semantic dump after each build ---
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            List<string> build1 = RunMenu(BuildMenu);
            Check(!build1.Any(l => l.StartsWith("[Error]") || l.StartsWith("[Exception]")), "first Build Office UI logs no error (the intercom fits the menu capacity; every icon upgrade id is known)");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            List<string> dump1 = DumpScene();
            File.WriteAllLines(Path.Combine(Scratch, "p3_scene_dump_1.txt"), dump1);
            CheckScene();

            List<string> build2 = RunMenu(BuildMenu);
            Check(!build2.Any(l => l.StartsWith("[Error]") || l.StartsWith("[Exception]")), "second Build Office UI logs no error");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            List<string> dump2 = DumpScene();
            File.WriteAllLines(Path.Combine(Scratch, "p3_scene_dump_2.txt"), dump2);

            List<string> onlyFirst = dump1.Except(dump2).Take(20).ToList();
            List<string> onlySecond = dump2.Except(dump1).Take(20).ToList();
            Check(dump1.SequenceEqual(dump2),
                  $"the two builds dump the same scene ({dump1.Count} lines): first-only [{string.Join(" || ", onlyFirst)}] second-only [{string.Join(" || ", onlySecond)}]");

            // --- Step 5: the builder's two reports, on a third build that is never saved ---
            BuilderReports();
        }
        catch (Exception e)
        {
            Report("AUTOMATION FAILED " + e);
            EditorApplication.Exit(2);
            return;
        }

        Report($"fails={_fails}");
        EditorApplication.Exit(_fails == 0 ? 0 : 3);
    }

    // -----------------------------
    // Step 1: generated content
    // -----------------------------

    private static void CheckContent()
    {
        var lib = AssetDatabase.LoadAssetAtPath<ContentLibrarySO>(LibraryPath);

        string[] interviewFiles = Directory.GetFiles("Assets/Data/World/Interview", "*.asset").Select(f => Path.GetFileName(f)).OrderBy(n => n).ToArray();
        Report("Interview folder: " + string.Join(", ", interviewFiles));
        Check(interviewFiles.Count(n => n.StartsWith("Question_")) == 6 && interviewFiles.Count(n => n.StartsWith("Dialog_")) == 2 &&
              interviewFiles.Count(n => n.StartsWith("Trigger_Unlock_")) == 3 && interviewFiles.Length == 11,
              "Assets/Data/World/Interview holds 6 questions, 2 dialogs and 3 unlock triggers");

        Check(lib.ReferenceBooks.Count == 5, $"the library holds 5 books ({string.Join(", ", lib.ReferenceBooks.Select(b => b.displayName))})");
        Check(lib.Questions.Count == 6, $"the library holds 6 questions ({string.Join(", ", lib.Questions.Select(q => q.question.id))})");
        Check(lib.Dialogs.Count == 2, $"the library holds 2 dialogs ({string.Join(", ", lib.Dialogs.Select(d => d.dialog.id))})");
        string[] triggerIds = lib.Triggers.Select(t => t.id).ToArray();
        Check(triggerIds.Length == 6 &&
              triggerIds.Take(3).OrderBy(i => i).SequenceEqual(new[] { "art_renaissance", "democracy_collapse", "science_boom" }) &&
              triggerIds.Skip(3).SequenceEqual(new[] { "unlock_q_capital", "unlock_q_ruler", "unlock_q_born" }),
              $"the library's triggers are the 3 authored ones, then the 3 unlock triggers ({string.Join(", ", triggerIds)})");
        Check(lib.Upgrades.Count == 4 && lib.GetUpgradeById("interview_protocols") != null, "the library holds 4 upgrades, Interview Protocols among them");
        Check(lib.GetEffectByAssetName("Effect_Dialog_RumourHeard") != null, "the library lists Effect_Dialog_RumourHeard");
        Check(lib.Interview.menuCapacity == 8 && lib.Interview.maxLineChars == 100,
              $"interview.menuCapacity {lib.Interview.menuCapacity} (8), maxLineChars {lib.Interview.maxLineChars} (100)");
        Check(lib.Interview.opener.id == "interview.opener" && lib.Interview.claim.text == "I request passage home to {place}.", "the interview wording carries its generated ids");

        Check(lib.Dialogs.All(d => d.dialog.oneShot), "both dialogs are one-shot (no \"repeatable\" key in the source)");

        string Gate(IEnumerable<TriggerCondition> conditions) =>
            string.Join(" + ", conditions.Select(c => $"{c.type}{(string.IsNullOrEmpty(c.key) ? "" : " " + c.key)}{(c.type == TriggerConditionType.DayAtLeast ? " " + c.threshold : "")}"));

        var expectedQuestions = new Dictionary<string, string>
        {
            ["q_currency"] = "", ["q_language"] = "", ["q_device"] = "", ["q_capital"] = "DayAtLeast 2", ["q_ruler"] = "DayAtLeast 3",
            ["q_born"] = "UpgradeOwned interview_protocols"
        };
        foreach (QuestionSO q in lib.Questions)
            Check(expectedQuestions.TryGetValue(q.question.id, out string want) && Gate(q.conditions) == want,
                  $"question {q.question.id} ({q.question.category}, '{q.question.label}') conditions: [{Gate(q.conditions)}]");

        var expectedTriggers = new Dictionary<string, string>
        {
            ["unlock_q_capital"] = "DayAtLeast 1", ["unlock_q_ruler"] = "DayAtLeast 2", ["unlock_q_born"] = "UpgradeOwned interview_protocols"
        };
        foreach (TimelineTriggerSO t in lib.Triggers.Where(t => t.id.StartsWith("unlock_")))
            Check(expectedTriggers.TryGetValue(t.id, out string want) && Gate(t.conditions) == want && t.oneShot && t.newsLineOnFire.StartsWith("BUREAU NOTICE:"),
                  $"trigger {t.id}: one-shot, conditions [{Gate(t.conditions)}], news '{t.newsLineOnFire}'");

        for (int day = 1; day <= 3; day++)
        {
            string channels = string.Join(",", lib.GetDayPlan(day).TellChannels);
            Check(channels == (day == 1 ? "Papers" : "Papers,Answer"), $"DayPlan_Inv_Day{day} tell channels [{channels}]");
        }

        EraSO ancient = lib.GetEraById("ancient");
        Check(ancient.smallTalk.Count == 2 && ancient.smallTalk[0].id == "ancient.smalltalk.1", "Era_ancient holds its two small-talk lines with generated ids");
        NationEraProfileSO egypt = lib.Profiles.First(p => p.id == "egypt_ancient");
        Check(egypt.smallTalk.Count == 1 && egypt.smallTalk[0].id == "egypt_ancient.smalltalk.1", "Place_egypt_ancient holds its small-talk line");
    }

    // -----------------------------
    // Step 1: the generator's negative checks (nothing is written)
    // -----------------------------

    private static void NegativeChecks()
    {
        string json = File.ReadAllText("Assets/Data/World/world_source.json");
        Check(CheckSource(json).Count == 0, $"the committed source passes CheckInterview ({string.Join(" | ", CheckSource(json))})");

        string noFromDay = RemoveAfter(json, "\"id\": \"q_language\"", "      \"fromDay\": 1,\n");
        Expect(noFromDay, "a question without fromDay", "Question 'q_language' needs \"fromDay\" of at least 1 (a missing fromDay reads 0).");

        string seventh = Replace(json, "\n  ],\n  \"dialogs\": [",
            ",\n    {\"id\": \"q_extra\", \"category\": \"Technology\", \"label\": \"Extra\", \"prompt\": \"What else?\", \"answer\": \"This: {value}.\", \"fromDay\": 1, \"announce\": \"\", \"conditions\": [], \"overrides\": []}\n  ],\n  \"dialogs\": [");
        Expect(seventh, "a seventh question (its category necessarily repeats one)",
               "Question 'q_extra' asks about Technology again (one question per category).",
               "The ask menu holds 9 choices (< Back, 7 question(s), small talk); the intercom shows at most 8.");

        string sameId = Replace(json, "\"id\": \"dlg_rumour.start.1\"", "\"id\": \"dlg_rumour.more\"");
        Expect(sameId, "an authored line id equal to a choice-label id",
               "Line id 'dlg_rumour.more' is used by dialog 'dlg_rumour' node 'start' and by choice 'more' of dialog 'dlg_rumour'.");

        const string longLine = "Please stamp my papers quickly, because the last ferry home leaves before the evening bells ring out.";
        Check(longLine.Length == 101, "the long test line has 101 characters");
        string tooLong = Replace(json, "Calculators? I saw nothing. Nothing at all.", longLine);
        Expect(tooLong, "a line of 101 characters",
               "Line 'dlg_rumour_followup.start.1' can render 101 characters; the transcript holds at most 100 (interview.maxLineChars).");

        string timed = Replace(json, "\"effect\": \"Effect_Dialog_RumourHeard\"", "\"effect\": \"Effect_Upgrade_PayBoost\"");
        Expect(timed, "a dialog naming an effect with a PayRateBonus op",
               ContentLibraryValidator.DialogEffectOpError("dlg_rumour", "noted", "Effect_Upgrade_PayBoost", EffectOpType.PayRateBonus) + ".");
    }

    private static void Expect(string json, string what, params string[] expected)
    {
        List<string> errors = CheckSource(json);
        Check(errors.OrderBy(e => e).SequenceEqual(expected.OrderBy(e => e)), $"negative check, {what}: got [{string.Join(" | ", errors)}]");
    }

    /// <summary>The generator's own checks (CheckReferences + CheckInterview) on a source text, by reflection; nothing is written.</summary>
    private static List<string> CheckSource(string json)
    {
        Type generator = typeof(WorldContentGenerator);
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Static;
        Type sourceType = generator.GetNestedType("WorldSource", BindingFlags.NonPublic);
        object src = JsonUtility.FromJson(json, sourceType);
        var errors = new List<string>();
        object authored = generator.GetMethod("LoadAuthored", Private).Invoke(null, new[] { sourceType.GetField("content").GetValue(src), errors });
        generator.GetMethod("CheckReferences", Private).Invoke(null, new[] { src, authored, errors });
        generator.GetMethod("CheckInterview", Private).Invoke(null, new[] { src, authored, errors });
        return errors;
    }

    private static string Replace(string text, string old, string replacement)
    {
        int i = text.IndexOf(old, StringComparison.Ordinal);
        if (i < 0 || text.IndexOf(old, i + 1, StringComparison.Ordinal) >= 0)
            throw new InvalidOperationException($"'{old}' must occur exactly once in the source");
        return text.Substring(0, i) + replacement + text.Substring(i + old.Length);
    }

    private static string RemoveAfter(string text, string anchor, string old)
    {
        int a = text.IndexOf(anchor, StringComparison.Ordinal);
        int i = a < 0 ? -1 : text.IndexOf(old, a, StringComparison.Ordinal);
        if (i < 0)
            throw new InvalidOperationException($"'{old}' not found after '{anchor}'");
        return text.Substring(0, i) + text.Substring(i + old.Length);
    }

    // -----------------------------
    // Step 5: the rebuilt scene
    // -----------------------------

    private static void CheckScene()
    {
        InvestigationUIController invest = Object.FindObjectsByType<InvestigationUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        Transform root = invest.transform;
        Transform window = root.Find("InvestigationRoot/WindowLayer/TranscriptWindow");
        Check(window != null && root.Find("InvestigationRoot/WindowLayer/IconClueLogWindow") == null, "TranscriptWindow exists and IconClueLogWindow is gone");

        var controller = window.GetComponent<TranscriptWindowController>();
        var chrome = window.GetComponent<OSWindowChrome>();
        var so = new SerializedObject(invest);
        Check(controller != null && chrome != null &&
              so.FindProperty("transcriptWindow").objectReferenceValue == controller &&
              so.FindProperty("transcriptChrome").objectReferenceValue == chrome,
              "the investigation controller is wired to the transcript window and its chrome");

        var soTranscript = new SerializedObject(controller);
        Check(soTranscript.FindProperty("entriesPerPage").intValue == 8 &&
              new[] { "titleText", "pageText", "prevButton", "nextButton", "entryRowsRoot", "entryRowTemplate" }.All(p => soTranscript.FindProperty(p).objectReferenceValue != null),
              "the transcript controller has 8 rows per page and every paging reference");
        Check(!window.gameObject.activeSelf, "the transcript window is built closed");

        Transform icon = root.Find("InvestigationRoot/BookShelf/IconClueLog");
        Check(icon != null && new SerializedObject(icon.GetComponent<DesktopIcon>()).FindProperty("targetWindow").objectReferenceValue == chrome,
              "the Clue Log icon opens the transcript");

        Check(root.Find("InvestigationRoot/IntercomPanel/Actions").GetComponent<RectMask2D>() != null, "IntercomPanel/Actions has a RectMask2D");

        foreach ((string name, string id) in new[] { ("IconLexicon", "archive_access"), ("IconDialect", ""), ("IconMaterial", "adv_scanner") })
        {
            Transform t = root.Find("InvestigationRoot/BookShelf/" + name);
            string actual = t != null ? new SerializedObject(t.GetComponent<DesktopIcon>()).FindProperty("requiredUpgradeId").stringValue : "(missing)";
            Check(actual == id, $"{name} requires '{actual}' (expected '{id}')");
        }

        Transform dialect = root.Find("InvestigationRoot/WindowLayer/IconDialectWindow");
        string title = dialect.Find("Header/TitleText").GetComponent<TMP_Text>().text;
        string body = dialect.Find("Body").GetComponent<TMP_Text>().text;
        Check(title == "Dialect" && body == "Notes on accents and phrasing. (placeholder)", $"the Dialect window reads '{title}' / '{body}'");
    }

    /// <summary>
    /// Runs Build Office UI a third time with two in-memory edits (the
    /// library's menu capacity 9, Archive Access under another id): the
    /// builder must report exactly the intercom that fits 8 choices and
    /// Lexicon's unknown upgrade. Both edits are undone, the unsaved build is
    /// dropped by reopening the saved scene, and no file on disk may change.
    /// </summary>
    private static void BuilderReports()
    {
        string[] files = { ScenePath, LibraryPath, "Assets/Data/Upgrades/Upgrade_ArchiveAccess.asset" };
        string[] before = files.Select(FileHash).ToArray();

        var lib = AssetDatabase.LoadAssetAtPath<ContentLibrarySO>(LibraryPath);
        UpgradeSO archive = lib.GetUpgradeById("archive_access");
        int capacity = lib.Interview.menuCapacity;
        List<string> build3;
        try
        {
            lib.Interview.menuCapacity = 9;
            archive.id = "archive_access_hidden";
            ResetLookups(lib);
            build3 = RunMenu(BuildMenu);
        }
        finally
        {
            lib.Interview.menuCapacity = capacity;
            archive.id = "archive_access";
            ResetLookups(lib);
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); // drops the unsaved third build

        string[] errors = build3.Where(l => l.StartsWith("[Error]")).OrderBy(l => l, StringComparer.Ordinal).ToArray();
        string[] expected = new[]
        {
            "[Error] [TimeDesk] The intercom fits 8 choices, but the content library's interview menu capacity is 9; lower interview.menuCapacity in world_source.json or enlarge the intercom.",
            "[Error] [TimeDesk] Desktop icon 'IconLexicon' requires unknown upgrade 'archive_access' (not in the content library's upgrades); it could never unlock."
        }.OrderBy(l => l, StringComparer.Ordinal).ToArray();
        Check(errors.SequenceEqual(expected), $"an unsaved build with menu capacity 9 and Archive Access renamed reports exactly the two builder errors: [{string.Join(" | ", errors)}]");
        Check(files.Select(FileHash).SequenceEqual(before), $"the unsaved build changed no file on disk ({string.Join(", ", files)})");
    }

    /// <summary>Clears the library's id lookups through its own reset (the private OnEnable), so the next lookup sees an edited id.</summary>
    private static void ResetLookups(ContentLibrarySO lib) =>
        typeof(ContentLibrarySO).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(lib, null);

    /// <summary>
    /// The scene as the builder shapes it: every object's path, active flag and
    /// component types; rect geometry; TMP text settings (the size range of an
    /// auto-sized text, never the size it last computed); and every serialized
    /// value of the project's own components (references as target paths).
    /// </summary>
    private static List<string> DumpScene()
    {
        Canvas.ForceUpdateCanvases(); // layout-driven rects are compared after a full layout pass
        var lines = new List<string>();
        foreach (GameObject go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            Dump(go.transform, lines);
        return lines;
    }

    private static void Dump(Transform t, List<string> lines)
    {
        string path = PathOf(t);
        lines.Add($"{path} active={t.gameObject.activeSelf} components=[{string.Join(",", t.GetComponents<Component>().Select(c => c != null ? c.GetType().Name : "Missing"))}]");

        if (t is RectTransform rt)
            lines.Add($"{path} rect min={V(rt.anchorMin)} max={V(rt.anchorMax)} pivot={V(rt.pivot)} pos={V(rt.anchoredPosition)} size={V(rt.sizeDelta)}");

        foreach (TMP_Text text in t.GetComponents<TMP_Text>())
            lines.Add($"{path} text='{text.text}' size={(text.enableAutoSizing ? $"auto {text.fontSizeMin:0.#}-{text.fontSizeMax:0.#}" : text.fontSize.ToString("0.#"))} wrap={text.textWrappingMode} overflow={text.overflowMode}");

        foreach (MonoBehaviour mb in t.GetComponents<MonoBehaviour>())
        {
            if (mb == null || mb.GetType().Assembly.GetName().Name != "Assembly-CSharp")
                continue;

            SerializedProperty p = new SerializedObject(mb).GetIterator();
            bool enter = true;
            while (p.NextVisible(enter))
            {
                enter = p.propertyType == SerializedPropertyType.Generic;
                if (p.name == "m_Script")
                    continue;
                string value = p.propertyType switch
                {
                    SerializedPropertyType.ObjectReference => Target(p.objectReferenceValue),
                    SerializedPropertyType.String => $"'{p.stringValue}'",
                    SerializedPropertyType.Integer => p.intValue.ToString(),
                    SerializedPropertyType.Boolean => p.boolValue.ToString(),
                    SerializedPropertyType.Float => p.floatValue.ToString("0.###"),
                    SerializedPropertyType.Enum => p.enumValueIndex.ToString(),
                    SerializedPropertyType.Color => p.colorValue.ToString(),
                    _ => null
                };
                if (value != null)
                    lines.Add($"{path} {mb.GetType().Name}.{p.propertyPath}={value}");
            }
        }

        for (int i = 0; i < t.childCount; i++)
            Dump(t.GetChild(i), lines);
    }

    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

    private static string V(Vector2 v) => $"({v.x:0.0},{v.y:0.0})";

    private static string Target(Object o)
    {
        if (o == null)
            return "null";
        if (o is Component c)
            return PathOf(c.transform) + ":" + c.GetType().Name;
        if (o is GameObject g)
            return PathOf(g.transform);
        return AssetDatabase.GetAssetPath(o) + ":" + o.name;
    }

    // -----------------------------
    // Helpers
    // -----------------------------

    /// <summary>Runs a menu item, reports it and every non-Log line it logged (plus the tools' own Log lines), and returns what it logged.</summary>
    private static List<string> RunMenu(string menu)
    {
        var lines = new List<string>();
        Application.LogCallback capture = (message, stack, type) => lines.Add($"[{type}] {message}");
        Application.logMessageReceived += capture;
        bool ran = EditorApplication.ExecuteMenuItem(menu);
        Application.logMessageReceived -= capture;
        Report($"menu '{menu}' ran={ran}");
        foreach (string l in lines.Where(l => !l.StartsWith("[Log]") || l.Contains("[WorldContentGenerator]") || l.Contains("[ContentLibraryValidator] <<<") || l.Contains("[TimeDesk]")))
            Report("  " + l);
        return lines;
    }

    private static Dictionary<string, string> HashDataFolder() =>
        Directory.GetFiles("Assets/Data", "*", SearchOption.AllDirectories).ToDictionary(p => p.Replace('\\', '/'), FileHash);

    private static string FileHash(string path)
    {
        using var sha = SHA1.Create();
        return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
    }
}
```

- [ ] **Step 4: Run it** (PowerShell, timeout 600000 ms)

```powershell
Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe' -ArgumentList @('-projectPath','E:\unity\NOPE-feat-clock','-executeMethod','TimeDeskP3Content.Run','-logFile','C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\unity_p3_content.log') -PassThru -Wait
```
Expected: exit code 0 and `SCRATCH/p3_content_report.txt` ending `fails=0`, with no `FAIL` line. In order it holds: both Generate World runs `ran=True`, each logging `World generated: 6 eras, 8 nations, 40 places, 3 rules, 3 day plans, 6 questions, 2 dialogs, 3 unlock triggers`; `PASS idempotent second Generate World: changedFiles=0`; the Interview folder listing (`Dialog_dlg_rumour.asset, Dialog_dlg_rumour_followup.asset, Question_q_born.asset, Question_q_capital.asset, Question_q_currency.asset, Question_q_device.asset, Question_q_language.asset, Question_q_ruler.asset, Trigger_Unlock_q_born.asset, Trigger_Unlock_q_capital.asset, Trigger_Unlock_q_ruler.asset`); PASS lines for 5 books, 6 questions, 2 dialogs, the triggers (3 authored, then `unlock_q_capital, unlock_q_ruler, unlock_q_born`), 4 upgrades, the rumour effect, menu capacity 8 and 100 characters, the generated ids, one-shot dialogs, each question's conditions (`q_currency`, `q_language`, `q_device`: none; `q_capital`: `DayAtLeast 2`; `q_ruler`: `DayAtLeast 3`; `q_born`: `UpgradeOwned interview_protocols`), each unlock trigger's conditions (`DayAtLeast 1`, `DayAtLeast 2`, `UpgradeOwned interview_protocols`), the three days' channels (`Papers`; `Papers,Answer`; `Papers,Answer`) and the small talk; `PASS Validate Content Library reports no issues`; `PASS the committed source passes CheckInterview ()`; five `PASS negative check, …` lines, each showing exactly its expected message(s); both Build Office UI runs `ran=True` with no error; the scene checks (`TranscriptWindow` wired with 8 rows per page and built closed, `IconClueLog` targets it, `IconClueLogWindow` gone, the intercom mask, the icon ids `archive_access` / `''` / `adv_scanner`, the Dialect window's "Dialect" / "Notes on accents and phrasing. (placeholder)"); `PASS the two builds dump the same scene (… lines)`; the third build `ran=True` with its two errors reported, then `PASS an unsaved build with menu capacity 9 and Archive Access renamed reports exactly the two builder errors: [...]` ("The intercom fits 8 choices, but the content library's interview menu capacity is 9; …" and "Desktop icon 'IconLexicon' requires unknown upgrade 'archive_access' …") and `PASS the unsaved build changed no file on disk (…)`.

- [ ] **Step 5: Hygiene**

```bash
cd /e/unity/NOPE-feat-clock && rm -f Assets/Editor/_TimeDeskP3Content.cs Assets/Editor/_TimeDeskP3Content.cs.meta && git checkout -- Assembly-CSharp.csproj Assembly-CSharp-Editor.csproj TimeDesk.Domain.csproj TimeDeskEditMode.csproj NOPE.sln ProjectSettings "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset" && git status --short
```
Expected: modified `Assets/Data/Content Library/ContentLibrary_Main.asset`, `Assets/Data/Investigation/DayPlan_Inv_Day1.asset`, `DayPlan_Inv_Day2.asset`, `DayPlan_Inv_Day3.asset`, the six `Assets/Data/World/Eras/Era_*.asset` (each gains `smallTalk`), the forty `Assets/Data/World/Places/Place_*.asset` (each gains `smallTalk`) and `Assets/Scenes/OfficeScene.unity`; untracked `Assets/Data/World/Interview/` and `Assets/Data/World/Interview.meta`, plus `NOPE-feat-clock.sln` and `TimeDesk.Visuals.csproj`. Unity may also re-save one of the four hand-authored assets of Task 12 in its own YAML shape (`git diff` shows only formatting); commit such a file with the content. Any other changed file means Generate World or the builder touched something unexpected: stop and investigate. Spot-check `git diff Assets/Data/Investigation/DayPlan_Inv_Day2.asset` (gains `tellChannels:` with `- 0` and `- 1`) and `git diff "Assets/Data/Content Library/ContentLibrary_Main.asset"` (gains the two books, `interview:`, `questions:`, `dialogs:` and three trigger references).

- [ ] **Step 6: Commit the generated content**

```bash
cd /e/unity/NOPE-feat-clock && git add "Assets/Data/Content Library/ContentLibrary_Main.asset" Assets/Data/Investigation/DayPlan_Inv_Day1.asset Assets/Data/Investigation/DayPlan_Inv_Day2.asset Assets/Data/Investigation/DayPlan_Inv_Day3.asset Assets/Data/World/Eras Assets/Data/World/Places Assets/Data/World/Interview Assets/Data/World/Interview.meta && git commit -F - <<'EOF'
content(world): generate the interview (questions, dialogs, unlock triggers)

Generate World writes six questions, two dialogs and three unlock
triggers into Assets/Data/World/Interview, small talk into the eras and
places, tell channels into the day plans, and the interview's wording,
questions, dialogs, five books and six triggers into ContentLibrary_Main.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```
(Add any hand-authored asset Unity re-saved, per Step 5.)

- [ ] **Step 7: Commit the rebuilt scene**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scenes/OfficeScene.unity && git commit -F - <<'EOF'
chore(scene): rebuild OfficeScene with the interview transcript

Build Office UI run twice (the semantic scene dumps are equal): the Case
Notes: Interview window replaces the Clue Log placeholder, the intercom is
masked, the desktop icons carry real upgrade ids and Dialect is an honest
placeholder.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```
`git status --short`: only `?? NOPE-feat-clock.sln` and `?? TimeDesk.Visuals.csproj`.

---

### Task 18: Unity verification

Proves spec §6 steps 2, 3, 4 and 6 in the branch's own Unity 6000.4.11f1 through two temporary `-executeMethod` sessions (steps 0, 1 and 5 were proven in Tasks 0 and 17), then appends the verification record (step 7). Session A is an editor script that also runs the EditMode suite through `TestRunnerApi` (it needs Assembly-CSharp types, which `TimeDeskEditMode` cannot see; `Assembly-CSharp-Editor.csproj` already references `UnityEditor.TestRunner`, as in piece 2's Task 13). Session B plays the rebuilt OfficeScene.

**When a check fails**, first decide where the defect is:
- **In a temporary `_TimeDesk*` script**: fix that script, commit nothing, re-run only the affected session (Step 4 for A, Step 6 for B).
- **In the product:** fix it at its source, Domain first with a failing EditMode test, pass the offline gate (Step 1), commit it as `fix: …`, then re-run both sessions. A fix to the builder or the source also re-runs Task 17's Steps 4–7.

**Files:**
- Create (temporary, never committed): `Assets/Editor/_TimeDeskP3Automation.cs`, `Assets/Editor/_TimeDeskP3PlaySmoke.cs`
- Modify: `docs/superpowers/specs/2026-09-24-dialog-questions-design.md` (verification record)
- Output: `SCRATCH/p3_automation_report.txt`, `SCRATCH/p3_world_check.txt`, `SCRATCH/p3_playsmoke_report.txt`

What the automation must prove (spec §6):
- **Gates and triggers (step 2):** the three authored triggers give Task 0's 60 results exactly (none fires); `NightlyResolve` on a day-1 world fires `unlock_q_capital` (its news line is in `tomorrow.newsLines`) and not `unlock_q_ruler` or `unlock_q_born`, and a second night does not repeat it; on a day-2 world it fires `unlock_q_ruler`; owning `interview_protocols` fires `unlock_q_born`, and never without it; the day-start `InterviewDay` gives askable [Currency, Language, Technology] on day 1, + Geography on day 2, + Politics on day 3, + BirthDate only with the upgrade, and `AnswerTellCategories` equal the askable ones without BirthDate on every day, with or without the upgrade; no dialog content problem.
- **World check (step 3)** on seeds 12345 and 999 × days 1–3, each with and without Interview Protocols, plus a 200-seed sweep × days 1–3 × both, with cases from `CaseFactory.GenerateDayCases(plan, world, Seeds.Day(seed, d), askable, answerTellCategories)` and both lists from the day-start `InterviewDay`: (1) determinism (same seed identical dumps including answers and small talk; a different seed differs); (2) stream contract (identities equal Task 0's lines on every day, with and without the upgrade; day 1's lines equal Task 0's in full — liars, homes, tells and paper values; on every day the lie — home, tells with channels, paper values — is identical with and without the upgrade, and the answers only gain the birth date); (3) every liar has a tell, each on a channel the day allows, a spoken tell only in a day-gated question's category, no category with two tells; (4) a Papers tell rewrites every field of its category and its answer is the cover; a spoken tell leaves every paper field on the cover and says the true home's value (or a date with the record's day and month and a home-range year other than the record's); (5) every non-tell answer is `ResolveFieldValue` of the claim, answers follow the askable questions, and the birth-date answer is always the registered date; (6) every spoken tell proves `ClaimMismatch` (source Answer) on the claim's row, `ForeignOrigin` naming the true home on its row, and nothing on any other row; (7) honest answers never prove anything, against any row or the record; (8) no Politics tell between Egypt and Greece (Early modern); (9) sweep: the spoken-tell share is 0 on day 1 and within 0.08 of 0.50 (day 2) and 0.55 (day 3), identical with the upgrade; every allowed (category, channel) occurs; BirthDate never on the Answer channel; no `NoPossibleLie` warning; (10) every opener greets by the recorded gender, every claim is `interview.claim` with the origin label, and small talk comes from the claimed place's lines or, when it has none, its era's.
- **EditMode suite (step 4)** through `TestRunnerApi`: everything passes except the known third-party `UnitySkills.Tests.Core.PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly` (report it, ignore it).
- **Play-through (step 6)** on the rebuilt OfficeScene, a new run pinned to a seed and to day 2 whose slots 1–3 hold an honest traveller, a liar whose only tell is the spoken capital, and a liar with a first-page Papers tell in an asked category (the seed is picked with the same `CaseFactory` call): slot 1's hub lists the two requests, "Ask about home >" and "Any news from home? >"; "Request Travel Passport" opens the passport and adds the prompt and reply while the transcript stays closed; the ask menu lists "< Back", Currency, Language, Device, Capital, Small talk (no Ruler, no Date of birth); the capital liar's answer row (`Line_q_capital.answer`, clickable) against the Gazetteer's claim row logs `CAPITAL INCORRECT — traveller said: "…"`; then the true home's row, clicked first, against the same answer row shows `ALREADY DOCUMENTED` (clicking the answer row again right after a full pair would only clear the comparison: `CompareController.Select` treats a click on a selected row as "clear"); the honest traveller's four answers each MATCH their claim row and log nothing; the Papers-tell liar answers with the cover (MATCH, nothing logged) while the paper field against the book logs; every transcript row shown, and a row filled with the 70-, 92- and 100-character worst cases, reports `isTextOverflowing == false`; Escape (FocusOffice) and back keeps the transcript and its page; "Any news from home? >" → "Tell me more." → "Noted. Thank you." is recorded and the entry is gone from slot 2's hub; each verdict is correct with no citation; after forced closing the save holds `dlg:dlg_rumour:done`, `rumour_calculators` and one `Effect_Dialog_RumourHeard` entry with `startDay = 3`; `AdvanceToNextDay` shows the rumour tip and the ruler notice in day 3's paper, the follow-up dialog is offered and the rumour is not, and Ruler is askable; no warning or error (so the interview wiring warning stays silent).

- [ ] **Step 1: Offline gate**

Run the compile check and the test run. Expected: exit 0 / exit 0; `passed 336, failed 0`. `git status --short` shows only the two untracked files.

- [ ] **Step 2: Check no Unity editor is open on the worktree** (PowerShell)

Run: `Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Select-Object ProcessId, CommandLine`
Expected: none on `NOPE-feat-clock`.

- [ ] **Step 3: Write session A** (`Assets/Editor/_TimeDeskP3Automation.cs`, Write tool)

```csharp
// TEMPORARY automation for piece 3 (dialog + questions); never committed.
// Gates and triggers (spec section 6 step 2), the world check (step 3), then
// the EditMode suite (step 4). Reports go to the scratchpad.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class TimeDeskP3Automation
{
    private const string Scratch = @"C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad";
    private static readonly string ReportPath = Path.Combine(Scratch, "p3_automation_report.txt");
    private static readonly string WorldPath = Path.Combine(Scratch, "p3_world_check.txt");
    private const string LibraryPath = "Assets/Data/Content Library/ContentLibrary_Main.asset";
    private const string Protocols = "interview_protocols";

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
            var lib = AssetDatabase.LoadAssetAtPath<ContentLibrarySO>(LibraryPath);
            GatesAndTriggers(lib);
            WorldCheck(lib);
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

    // -----------------------------
    // Step 2: gates and triggers
    // -----------------------------

    private static void GatesAndTriggers(ContentLibrarySO lib)
    {
        // The authored triggers decide exactly as before piece 3 (by reflection on the same private method).
        MethodInfo allPass = typeof(TimelineService).GetMethod("AllConditionsPass", BindingFlags.NonPublic | BindingFlags.Static);
        List<WorldState> worlds = CraftedWorlds();
        var now = new List<string>();
        foreach (TimelineTriggerSO trigger in lib.Triggers.Where(t => !t.id.StartsWith("unlock_")))
            for (int i = 0; i < worlds.Count; i++)
                now.Add($"trigger={trigger.id} state={i} pass={allPass.Invoke(null, new object[] { trigger, worlds[i] })}");
        string[] baseline = File.ReadAllLines(Path.Combine(Scratch, "p3_baseline_triggers.txt"));
        Report($"{(baseline.OrderBy(l => l).SequenceEqual(now.OrderBy(l => l)) ? "PASS" : "FAIL")} authored triggers: {now.Count} results equal the baseline ({now.Count(l => l.EndsWith("pass=True"))} pass)");
        if (!baseline.OrderBy(l => l).SequenceEqual(now.OrderBy(l => l)))
            Problem("authored trigger results differ from p3_baseline_triggers.txt");

        // Unlock announcements: the night before the first askable day, once.
        GameConfigSO config = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath).gameConfig;
        string capital = lib.Triggers.First(t => t.id == "unlock_q_capital").newsLineOnFire;
        string ruler = lib.Triggers.First(t => t.id == "unlock_q_ruler").newsLineOnFire;
        string born = lib.Triggers.First(t => t.id == "unlock_q_born").newsLineOnFire;

        var day1 = new WorldState { day = 1 };
        TimelineService.NightlyResolve(day1, lib, config);
        Expect(day1.tomorrow.newsLines.Contains(capital) && day1.HasFlag(FlagKeys.TriggerFired("unlock_q_capital")) &&
               !day1.tomorrow.newsLines.Contains(ruler) && !day1.HasFlag(FlagKeys.TriggerFired("unlock_q_born")),
               "night of day 1: the capital notice, not the ruler or birth-date notice");
        TimelineService.NightlyResolve(day1, lib, config);
        Expect(!day1.tomorrow.newsLines.Contains(capital), "a second night: the capital notice is not repeated (one-shot)");

        var day2 = new WorldState { day = 2 };
        TimelineService.NightlyResolve(day2, lib, config);
        Expect(day2.tomorrow.newsLines.Contains(ruler) && day2.HasFlag(FlagKeys.TriggerFired("unlock_q_ruler")), "night of day 2: the ruler notice");

        var bought = new WorldState { day = 1 };
        bought.UnlockUpgrade(Protocols);
        TimelineService.NightlyResolve(bought, lib, config);
        Expect(bought.tomorrow.newsLines.Contains(born), "night after buying Interview Protocols: the birth-date notice");
        Expect(!day1.HasFlag(FlagKeys.TriggerFired("unlock_q_born")) && !day2.HasFlag(FlagKeys.TriggerFired("unlock_q_born")), "never the birth-date notice without the upgrade");

        // The day-start interview.
        var askable = new[] { ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology, ClueCategory.Geography, ClueCategory.Politics };
        for (int day = 1; day <= 3; day++)
        {
            foreach (bool upgrade in new[] { false, true })
            {
                InterviewDay interview = BuildInterviewDay(lib, DayWorld(day, upgrade));
                List<ClueCategory> expected = askable.Take(day + 2).ToList();
                if (upgrade)
                    expected.Add(ClueCategory.BirthDate);
                Expect(interview.AskableCategories.SequenceEqual(expected),
                       $"day {day}{(upgrade ? " with the upgrade" : "")}: askable [{string.Join(", ", interview.AskableCategories)}]");
                Expect(interview.AnswerTellCategories.SequenceEqual(askable.Take(day + 2)),
                       $"day {day}{(upgrade ? " with the upgrade" : "")}: spoken tells may come from [{string.Join(", ", interview.AnswerTellCategories)}]");
                Expect(interview.ContentProblems.Count == 0, $"day {day}: no dialog content problem");
            }
        }
    }

    private static void Expect(bool ok, string what)
    {
        Report((ok ? "PASS " : "FAIL ") + what);
        if (!ok)
            Problem(what);
    }

    /// <summary>A fresh world on a day, owning Interview Protocols when asked.</summary>
    private static WorldState DayWorld(int day, bool upgrade)
    {
        var world = new WorldState { day = day };
        if (upgrade)
            world.UnlockUpgrade(Protocols);
        return world;
    }

    /// <summary>The day-start interview, built by the same call GameManager makes (TimelineService.BuildInterviewDay), with an empty ledger.</summary>
    private static InterviewDay BuildInterviewDay(ContentLibrarySO lib, WorldState world) =>
        TimelineService.BuildInterviewDay(lib, world, new ShiftLedger());

    // -----------------------------
    // Step 3: the world check
    // -----------------------------

    private sealed class Coverage
    {
        public readonly int[] liars = new int[4];
        public readonly int[] spoken = new int[4];
        public readonly HashSet<string> options = new HashSet<string>();
    }

    private static void WorldCheck(ContentLibrarySO lib)
    {
        string[] baseline = File.ReadAllLines(Path.Combine(Scratch, "p3_baseline_cases.txt"));
        int noLieWarnings = 0;
        Application.LogCallback watch = (message, stack, type) =>
        {
            if (type == LogType.Warning && message.Contains("rolled a liar, but no other place"))
                noLieWarnings++;
        };
        Application.logMessageReceived += watch;
        try
        {
            foreach (int seed in new[] { 12345, 999 })
            {
                for (int day = 1; day <= 3; day++)
                {
                    DayRun plain = Generate(lib, seed, day, false);
                    DayRun bought = Generate(lib, seed, day, true);
                    string prefix = $"seed={seed} day={day} ";
                    List<string> expected = baseline.Where(l => l.StartsWith(prefix)).ToList();

                    // (1) Determinism.
                    if (Dump(plain) != Dump(Generate(lib, seed, day, false)))
                        Problem($"seed {seed} day {day}: the same seed gave different travellers");
                    if (Dump(plain) == Dump(Generate(lib, seed + 1, day, false)))
                        Problem($"seed {seed} day {day}: a different seed gave the same travellers");
                    World($"=== seed {seed} day {day}");
                    World(Dump(plain));

                    // (2) Stream contract.
                    List<string> lines = plain.cases.Select((c, i) => BaselineLine(seed, day, i + 1, c, plain.violators.ContainsKey(i + 1))).ToList();
                    List<string> boughtLines = bought.cases.Select((c, i) => BaselineLine(seed, day, i + 1, c, bought.violators.ContainsKey(i + 1))).ToList();
                    if (!expected.Select(Identity).SequenceEqual(lines.Select(Identity)) || !expected.Select(Identity).SequenceEqual(boughtLines.Select(Identity)))
                        Problem($"stream contract, seed {seed} day {day}: claims, names, dates, roles or violator slots differ from the baseline");
                    if (day == 1 && !expected.SequenceEqual(lines))
                        Problem($"stream contract, seed {seed} day 1: liars, homes, tells or paper values differ from the baseline:\n  baseline:\n    {string.Join("\n    ", expected)}\n  now:\n    {string.Join("\n    ", lines)}");
                    for (int i = 0; i < plain.cases.Count; i++)
                    {
                        CaseInstance a = plain.cases[i], b = bought.cases[i];
                        if (LieKey(a) != LieKey(b))
                            Problem($"seed {seed} day {day} slot {i + 1}: the upgrade changed the lie ({LieKey(a)} vs {LieKey(b)})");
                        if (!b.answers.Take(a.answers.Count).Select(x => x.value).SequenceEqual(a.answers.Select(x => x.value)) ||
                            b.answers.Count != a.answers.Count + 1 || b.answers.Last().category != ClueCategory.BirthDate)
                            Problem($"seed {seed} day {day} slot {i + 1}: with the upgrade the answers should only gain the birth date");
                    }

                    foreach (DayRun run in new[] { plain, bought })
                        for (int i = 0; i < run.cases.Count; i++)
                            CheckCase(lib, run, run.cases[i], $"seed {seed} day {day}{(run.upgrade ? " +upgrade" : "")} slot {i + 1}", null);
                }
            }

            // (9) The sweep.
            var plainCoverage = new Coverage();
            var boughtCoverage = new Coverage();
            for (int seed = 1; seed <= 200; seed++)
            {
                for (int day = 1; day <= 3; day++)
                {
                    foreach (bool upgrade in new[] { false, true })
                    {
                        DayRun run = Generate(lib, seed, day, upgrade);
                        foreach (CaseInstance c in run.cases)
                            CheckCase(lib, run, c, $"sweep seed {seed} day {day}{(upgrade ? " +upgrade" : "")}", upgrade ? boughtCoverage : plainCoverage);
                    }
                }
            }

            foreach ((Coverage cov, string name) in new[] { (plainCoverage, "without the upgrade"), (boughtCoverage, "with the upgrade") })
            {
                var shares = new List<string>();
                for (int day = 1; day <= 3; day++)
                {
                    float share = cov.liars[day] == 0 ? 0f : cov.spoken[day] / (float)cov.liars[day];
                    shares.Add($"day {day} {share:0.000} ({cov.spoken[day]}/{cov.liars[day]})");
                    float target = day == 1 ? 0f : day == 2 ? 0.50f : 0.55f;
                    if (day == 1 ? cov.spoken[day] != 0 : Math.Abs(share - target) > 0.08f)
                        Problem($"spoken-tell share {name}, day {day}: {share:0.000}, expected about {target:0.00}");
                }
                World($"coverage (200 seeds x days 1-3, {name}): spoken shares {string.Join("; ", shares)}; options seen: {string.Join(", ", cov.options.OrderBy(o => o))}");
            }

            if (plainCoverage.spoken.Sum() != boughtCoverage.spoken.Sum() || plainCoverage.liars.Sum() != boughtCoverage.liars.Sum())
                Problem("the sweep's liars or spoken tells differ with the upgrade");

            foreach (string option in ExpectedOptions())
                if (!plainCoverage.options.Contains(option))
                    Problem($"option {option} never occurs in the sweep");
            if (plainCoverage.options.Concat(boughtCoverage.options).Any(o => o.EndsWith("BirthDate/Answer")))
                Problem("a BirthDate Answer tell occurred (the birth-date question is hint-only)");
        }
        finally
        {
            Application.logMessageReceived -= watch;
        }

        World($"NoPossibleLie warnings: {noLieWarnings}");
        if (noLieWarnings > 0)
            Problem($"{noLieWarnings} NoPossibleLie warning(s)");
    }

    /// <summary>Every (day, category, channel) the day allows: papers on days 1-3, answers for the day-gated questions on days 2-3.</summary>
    private static IEnumerable<string> ExpectedOptions()
    {
        foreach (int day in new[] { 1, 2, 3 })
        {
            foreach (string c in new[] { "BirthDate", "Currency", "Language", "Technology" })
                yield return $"day{day} {c}/Papers";
            if (day == 1)
                continue;
            foreach (string c in new[] { "Currency", "Language", "Technology", "Geography" })
                yield return $"day{day} {c}/Answer";
            if (day == 3)
                yield return "day3 Politics/Answer";
        }
    }

    private sealed class DayRun
    {
        public int day;
        public bool upgrade;
        public DayPlanSO plan;
        public FactTable facts;
        public InterviewDay interview;
        public CaseFactory factory;
        public List<CaseInstance> cases;
        public Dictionary<int, NationEraProfileSO> violators;
    }

    private static DayRun Generate(ContentLibrarySO lib, int seed, int day, bool upgrade)
    {
        WorldState world = DayWorld(day, upgrade);
        var run = new DayRun { day = day, upgrade = upgrade, plan = lib.GetDayPlan(day), interview = BuildInterviewDay(lib, world) };
        run.facts = lib.BuildFactTable(run.plan);
        run.factory = new CaseFactory(lib, run.facts);
        run.cases = run.factory.GenerateDayCases(run.plan, world, Seeds.Day(seed, day), run.interview.AskableCategories, run.interview.AnswerTellCategories);
        run.violators = (Dictionary<int, NationEraProfileSO>)typeof(CaseFactory)
            .GetField("_violators", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(run.factory);
        return run;
    }

    /// <summary>Exactly Task 0's baseline format (tells = the flagged paper categories, in paper order).</summary>
    private static string BaselineLine(int seed, int day, int slot, CaseInstance c, bool violator)
    {
        List<DocumentField> fields = c.documents.SelectMany(d => d.fields).ToList();
        string tells = string.Join(",", fields.Where(f => f.isAnachronism).Select(f => f.category).Distinct());
        string papers = string.Join(";", fields.Select(f => $"{f.label}={f.value}"));
        return $"seed={seed} day={day} slot={slot} claim='{c.originLabel}' name='{c.visitorGivenName}' born='{c.trueBirthDate}' role='{(c.archetype != null ? c.archetype.displayName : "")}' allowed={c.claimAllowedByRules} violator={violator}" +
               $" | liar={c.IsLiar} home='{c.HomeLabel}' tells=[{tells}] | papers={papers}";
    }

    private static string Identity(string line) => line.Substring(0, line.IndexOf(" | ", StringComparison.Ordinal));

    /// <summary>A traveller's lie: home, tells with channels, and every paper value.</summary>
    private static string LieKey(CaseInstance c) =>
        $"liar={c.IsLiar} home='{c.HomeLabel}' tells=[{string.Join(",", Tells(c).Select(t => $"{t.Key}/{t.Value}"))}] papers={string.Join(";", c.documents.SelectMany(d => d.fields).Select(f => f.value))}";

    /// <summary>The traveller's tells with their channels: flagged paper categories, then spoken tells.</summary>
    private static List<KeyValuePair<ClueCategory, TellChannel>> Tells(CaseInstance c)
    {
        var tells = c.documents.SelectMany(d => d.fields).Where(f => f.isAnachronism).Select(f => f.category).Distinct()
            .Select(cat => new KeyValuePair<ClueCategory, TellChannel>(cat, TellChannel.Papers)).ToList();
        tells.AddRange(c.answers.Where(a => a.isTell).Select(a => new KeyValuePair<ClueCategory, TellChannel>(a.category, TellChannel.Answer)));
        return tells;
    }

    private static string Dump(DayRun run) => string.Join("\n", run.cases.Select(c =>
        $"  {c.visitorDisplayName} | {c.claimLine} | {c.introLine} | born {c.trueBirthDate} | {LieKey(c)} | small talk '{(c.smallTalk != null ? c.smallTalk.id : "none")}'\n      " +
        string.Join("; ", c.answers.Select(a => $"{a.category}={a.value}{(a.isTell ? " [TELL]" : "")}"))));

    private static void CheckCase(ContentLibrarySO lib, DayRun run, CaseInstance c, string where, Coverage cov)
    {
        List<DocumentField> fields = c.documents.SelectMany(d => d.fields).ToList();
        string cn = c.claimedNation != null ? c.claimedNation.id : null;
        string ce = c.claimedEra != null ? c.claimedEra.id : null;
        MethodInfo resolve = typeof(CaseFactory).GetMethod("ResolveFieldValue", BindingFlags.NonPublic | BindingFlags.Instance);
        string Cover(ClueCategory category) => (string)resolve.Invoke(run.factory, new object[] { category, c });

        // (10) Opener, claim and small talk.
        string honorific = c.gender == TravellerGender.Male ? lib.Interview.honorificMale
            : c.gender == TravellerGender.Female ? lib.Interview.honorificFemale : lib.Interview.honorificUnknown;
        if (!c.isLegendary && c.introLine != Interview.Fill(lib.Interview.opener.text, Interview.HonorificToken, honorific))
            Problem($"{where}: opener '{c.introLine}' does not greet a {c.gender} traveller");
        if (c.claimLine != Interview.Claim(lib.Interview, c.originLabel))
            Problem($"{where}: claim '{c.claimLine}' is not interview.claim with '{c.originLabel}'");
        NationEraProfileSO claimed = lib.GetProfile(c.claimedNation, c.claimedEra);
        List<LineText> placeLines = claimed != null ? claimed.smallTalk : new List<LineText>();
        List<LineText> pool = placeLines.Count > 0 ? placeLines : c.claimedEra != null ? c.claimedEra.smallTalk : new List<LineText>();
        if (pool.Count > 0 ? c.smallTalk == null || !pool.Any(l => l.id == c.smallTalk.id) : c.smallTalk != null)
            Problem($"{where}: small talk '{(c.smallTalk != null ? c.smallTalk.id : "none")}' is not from the claimed place or its era");

        // (5) Answers: one per askable question, in order; a non-tell answer is the cover.
        if (!c.answers.Select(a => a.category).SequenceEqual(run.interview.AskableCategories))
            Problem($"{where}: answers [{string.Join(", ", c.answers.Select(a => a.category))}] do not follow the askable questions");
        foreach (InterviewAnswer a in c.answers.Where(a => !a.isTell))
            if (a.value != Cover(a.category))
                Problem($"{where}: the {a.category} answer '{a.value}' is not the cover '{Cover(a.category)}'");
        InterviewAnswer bornAnswer = c.answers.FirstOrDefault(a => a.category == ClueCategory.BirthDate);
        if (bornAnswer != null && (bornAnswer.isTell || bornAnswer.value != c.trueBirthDate))
            Problem($"{where}: the birth-date answer '{bornAnswer.value}' is not the registered date");

        if (!c.IsLiar)
        {
            // (7) Honest answers never prove anything.
            if (c.answers.Any(a => a.isTell))
                Problem($"{where}: an honest traveller speaks a tell");
            foreach (InterviewAnswer a in c.answers)
            {
                CompareEvidence said = CompareEvidence.ForAnswer(a.category, a.value, a.isTell);
                if (run.facts.Rows(a.category).Any(row => DiscrepancyLog.Prove(said, row.ToEvidence(), cn, ce) != null) ||
                    DiscrepancyLog.Prove(said, CompareEvidence.ForRecordField(a.category, c.trueBirthDate), cn, ce) != null)
                    Problem($"{where}: an honest {a.category} answer proves something");
            }
            return;
        }

        // (3) Tells.
        List<KeyValuePair<ClueCategory, TellChannel>> tells = Tells(c);
        if (tells.Count == 0)
            Problem($"{where}: a liar without a tell");
        if (tells.Select(t => t.Key).Distinct().Count() != tells.Count)
            Problem($"{where}: a category carries two tells ({string.Join(", ", tells.Select(t => $"{t.Key}/{t.Value}"))})");
        foreach (KeyValuePair<ClueCategory, TellChannel> t in tells)
        {
            if (!run.plan.TellChannels.Contains(t.Value))
                Problem($"{where}: a {t.Value} tell on a day without that channel");
            if (t.Value == TellChannel.Answer && !run.interview.AnswerTellCategories.Contains(t.Key))
                Problem($"{where}: a spoken {t.Key} tell, but {t.Key} is not a day-gated question today");
            if (cov != null)
                cov.options.Add($"day{run.day} {t.Key}/{t.Value}");
        }
        if (cov != null)
        {
            cov.liars[run.day]++;
            if (tells.Any(t => t.Value == TellChannel.Answer))
                cov.spoken[run.day]++;
        }

        NationEraProfileSO home = c.trueHome;
        foreach (KeyValuePair<ClueCategory, TellChannel> t in tells)
        {
            ClueCategory category = t.Key;
            List<DocumentField> ofCategory = fields.Where(f => f.category == category).ToList();

            // (4) Papers vs answers.
            string value;
            if (t.Value == TellChannel.Papers)
            {
                if (ofCategory.Any(f => !f.isAnachronism))
                    Problem($"{where}: a {category} paper tell left a field unrewritten");
                value = ofCategory[0].value;
                InterviewAnswer spoken = c.answers.FirstOrDefault(a => a.category == category);
                if (spoken != null && spoken.value != Cover(category))
                    Problem($"{where}: the answer in the Papers-tell category {category} is not the cover");
            }
            else
            {
                if (ofCategory.Any(f => f.isAnachronism || f.value != Cover(category)))
                    Problem($"{where}: a spoken {category} tell changed the papers");
                value = c.answers.First(a => a.category == category).value;
            }

            if (category == ClueCategory.BirthDate)
            {
                bool parsed = BirthDates.TryParse(value, out int d, out int m, out int y) & BirthDates.TryParse(c.trueBirthDate, out int rd, out int rm, out int ry);
                int lo = Math.Min(home.birthYearMin, home.birthYearMax), hi = Math.Max(home.birthYearMin, home.birthYearMax);
                if (!parsed || d != rd || m != rm || y < lo || y > hi || y == ry)
                    Problem($"{where}: birth-date tell '{value}' vs record '{c.trueBirthDate}', home years {lo}..{hi}");
            }
            else if (value != run.facts.Get(home.nation.id, home.era.id, category))
            {
                Problem($"{where}: the {category} tell '{value}' is not the home's value");
            }

            // (6) Every spoken tell proves: mismatch on the claim's row, the home named on its row, nothing elsewhere.
            if (t.Value != TellChannel.Answer)
                continue;
            CompareEvidence said = CompareEvidence.ForAnswer(category, value, true);
            if (category == ClueCategory.BirthDate)
            {
                Discrepancy record = DiscrepancyLog.Prove(said, CompareEvidence.ForRecordField(category, c.trueBirthDate), cn, ce);
                if (record == null || record.provedBy != DiscrepancyProof.RecordMismatch)
                    Problem($"{where}: the spoken birth-date tell does not prove against the record");
                continue;
            }

            foreach (FactRow row in run.facts.Rows(category))
            {
                Discrepancy found = DiscrepancyLog.Prove(said, row.ToEvidence(), cn, ce);
                bool isClaim = row.NationId == cn && row.EraId == ce;
                bool isHome = row.NationId == home.nation.id && row.EraId == home.era.id;
                if (isClaim && (found == null || found.provedBy != DiscrepancyProof.ClaimMismatch || found.source != EvidenceKind.Answer))
                    Problem($"{where}: the spoken {category} tell does not prove a mismatch against the claim's row");
                else if (isHome && (found == null || found.provedBy != DiscrepancyProof.ForeignOrigin || found.actualOrigin != c.trueHomeLabel))
                    Problem($"{where}: the spoken {category} tell does not name the true home against its row");
                else if (!isClaim && !isHome && found != null)
                    Problem($"{where}: the spoken {category} tell proves against a third place '{row.OriginLabel}'");
            }

            // (8) Shared values: "Sultan Mustafa II" (Egypt and Greece, Early modern) is never a Politics tell between them.
            if (category == ClueCategory.Politics && ce == "earlymodern" && home.era.id == "earlymodern" &&
                new[] { cn, home.nation.id }.OrderBy(n => n).SequenceEqual(new[] { "egypt", "greece" }))
                Problem($"{where}: a Politics tell between Egypt and Greece (Early modern), who share their ruler");
        }
    }

    /// <summary>The same twenty crafted worlds as Task 0's baseline.</summary>
    private static List<WorldState> CraftedWorlds()
    {
        var worlds = new List<WorldState>();
        for (int day = 1; day <= 5; day++)
            worlds.Add(new WorldState { day = day });

        WorldState w;
        w = new WorldState { day = 2 }; w.SetFlag("rumour_calculators"); worlds.Add(w);
        w = new WorldState { day = 3 }; w.SetFlag("met_tesla"); w.SetFlag("trig:art_renaissance:fired"); worlds.Add(w);
        w = new WorldState { day = 2 }; w.AddCounter("sent:tag:scientist", 7); worlds.Add(w);
        w = new WorldState { day = 4 }; w.AddCounter("sent:tag:soldier", 30); w.AddCounter("sent:era:ancient", 12); worlds.Add(w);
        w = new WorldState { day = 1, timelineStability = 5f }; worlds.Add(w);
        w = new WorldState { day = 3, timelineStability = 50f }; worlds.Add(w);
        w = new WorldState { day = 2 }; w.timeline.AddScore("attrTotal:art", 500f); w.timeline.AddScore("attrTotal:science", 500f); worlds.Add(w);
        w = new WorldState { day = 2 }; w.timeline.AddScore("attrTotal:democracy", -50f); worlds.Add(w);
        w = new WorldState { day = 3 }; w.timeline.AddScore("attr:egypt_ancient:art", 400f); w.timeline.AddScore("attr:italy_ancient:science", 400f); worlds.Add(w);
        w = new WorldState { day = 2 }; w.timeline.AddScore("nation:egypt", 90f); worlds.Add(w);
        w = new WorldState { day = 3 }; w.timeline.dominantKeys.Add("egypt_ancient:art"); w.timeline.supportingKeys.Add("italy_ancient:science"); worlds.Add(w);
        w = new WorldState { day = 1 }; w.UnlockUpgrade("archive_access"); worlds.Add(w);
        w = new WorldState { day = 5 }; w.UnlockUpgrade("adv_scanner"); w.UnlockUpgrade("diplo_contacts"); worlds.Add(w);
        w = new WorldState { day = 4, timelineStability = 0f }; w.SetFlag("Tyrant_Rises"); w.AddCounter("sent:tag:artist", 2); worlds.Add(w);
        w = new WorldState { day = 5, timelineStability = 20f }; w.SetFlag("upgrade:archive_access"); worlds.Add(w);
        return worlds;
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
Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe' -ArgumentList @('-projectPath','E:\unity\NOPE-feat-clock','-executeMethod','TimeDeskP3Automation.Run','-logFile','C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\unity_p3_automation.log') -PassThru -Wait
```
Expected in `SCRATCH/p3_automation_report.txt`: only `PASS` lines before the summary (the authored triggers `60 results equal the baseline (0 pass)`, the four notice checks, and per day with and without the upgrade the askable and spoken-tell categories); `world check problems=0`; `tests passed=465 failed=1` (336 Domain tests + the 129 other EditMode tests piece 2 counted: 345 − 216) with the only `FAIL` line being `UnitySkills.Tests.Core.PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly`. Exit code 3 comes from that known failure. In `SCRATCH/p3_world_check.txt`: no `PROBLEM` line; two coverage lines (without and with the upgrade) with spoken shares `day 1 0.000`, day 2 near 0.50 and day 3 near 0.55, identical counts in both lines, and options covering `day1 BirthDate/Papers` … `day3 Politics/Answer`; `NoPossibleLie warnings: 0`.

- [ ] **Step 5: Write session B** (`Assets/Editor/_TimeDeskP3PlaySmoke.cs`, Write tool)

```csharp
// TEMPORARY play-mode smoke for piece 3 (dialog + questions); never committed.
// Picks a day-2 seed whose slots 1-3 hold an honest traveller, a liar whose
// only tell is a spoken capital, and a liar with a first-page Papers tell in
// an asked category; plays them through the real OfficeScene interview (spec
// section 6 step 6), completes the rumour dialog, forces closing time, checks
// the save, advances to day 3 and checks its paper and hub. Survives
// play-mode domain reloads and the day-3 scene load via SessionState.
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
public static class TimeDeskP3PlaySmoke
{
    private const string Scratch = @"C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad";
    private static readonly string ReportPath = Path.Combine(Scratch, "p3_playsmoke_report.txt");
    private static readonly string SaveBackup = Path.Combine(Scratch, "nope_save_backup_p3.json");

    private const string StepKey = "P3Smoke.Step";
    private const string TimeKey = "P3Smoke.Time";
    private const string SlotKey = "P3Smoke.Slot";
    private const string SeedKey = "P3Smoke.Seed";
    private const string RolesKey = "P3Smoke.Roles";
    private const string NamesKey = "P3Smoke.Names";
    private const string FailsKey = "P3Smoke.Fails";
    private const string LogKey = "P3Smoke.Log";
    private const string HadSaveKey = "P3Smoke.HadSave";

    private const string RumourEntry = "Any news from home? >";
    private const string FollowUpEntry = "About those calculators... >";
    private const string RumourTip = "A traveller's tip: someone is smuggling pocket calculators into the past.";
    private const string RulerNotice = "BUREAU NOTICE: desk officers may now ask travellers who rules them.";

    static TimeDeskP3PlaySmoke()
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
        DayPlanSO day2 = lib.GetDayPlan(2);

        int seed = 0;
        string[] roles = null;
        List<CaseInstance> queue = null;
        for (int s = 1; s <= 5000 && roles == null; s++)
        {
            var world = new WorldState { day = 2 };
            InterviewDay interview = BuildInterviewDay(lib, world);
            queue = new CaseFactory(lib, lib.BuildFactTable(day2)).GenerateDayCases(day2, world, Seeds.Day(s, 2), interview.AskableCategories, interview.AnswerTellCategories);
            roles = AssignRoles(queue);
            seed = s;
        }

        if (roles == null)
        {
            Report("FAIL no seed in 1..5000 has an honest traveller, a spoken-capital liar and a first-page Papers-tell liar in slots 1-3");
            EditorApplication.Exit(5);
            return;
        }

        string names = string.Join("|", queue.Take(3).Select(c => c.visitorGivenName));
        Report($"seed={seed} roles={string.Join(",", roles)} names={names}");

        // A fresh run on that seed, starting on day 2: move the player's save aside
        // (restored in Finish; the path is reported for a manual restore if Unity
        // dies first) and pin the seed and start day in RunConfig.asset, saved to
        // disk so they survive the play-mode domain reload (hygiene reverts the asset).
        bool hadSave = File.Exists(SaveSystem.SavePath);
        Report($"save path={SaveSystem.SavePath} hadSave={hadSave}");
        if (hadSave)
        {
            File.Copy(SaveSystem.SavePath, SaveBackup, true);
            File.Delete(SaveSystem.SavePath);
        }
        var runConfig = new SerializedObject(Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath));
        runConfig.FindProperty("fixedRunSeed").intValue = seed;
        runConfig.FindProperty("startingDay").intValue = 2;
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

    /// <summary>The day-start interview, built by the same call GameManager makes (TimelineService.BuildInterviewDay), with an empty ledger.</summary>
    private static InterviewDay BuildInterviewDay(ContentLibrarySO lib, WorldState world) =>
        TimelineService.BuildInterviewDay(lib, world, new ShiftLedger());

    /// <summary>Roles for slots 1-3 ("honest", "capital", "papers"), each exactly once, or null.</summary>
    private static string[] AssignRoles(List<CaseInstance> queue)
    {
        if (queue.Count < 4)
            return null;

        string[] roles = queue.Take(3).Select(Role).ToArray();
        return roles.OrderBy(r => r).SequenceEqual(new[] { "capital", "honest", "papers" }) ? roles : null;
    }

    private static string Role(CaseInstance c)
    {
        if (c.isLegendary)
            return "other";
        if (!c.IsLiar)
            return c.claimAllowedByRules ? "honest" : "other";
        List<InterviewAnswer> spoken = c.answers.Where(a => a.isTell).ToList();
        if (spoken.Count == 1 && spoken[0].category == ClueCategory.Geography)
            return "capital";
        DocumentField tell = FirstPagePlaceTell(c);
        return spoken.Count == 0 && tell != null && c.answers.Any(a => a.category == tell.category) ? "papers" : "other";
    }

    private static DocumentField FirstPagePlaceTell(CaseInstance c) =>
        c.documents.SelectMany(d => d.fields).FirstOrDefault(f => f.isAnachronism && f.category != ClueCategory.BirthDate && f.page == 0);

    private static void Tick()
    {
        int step = SessionState.GetInt(StepKey, 0);
        try
        {
            if (step == 1 && EditorApplication.isPlaying && Elapsed > 4f) StartShift(2);
            else if (step == 2 && Elapsed > 1f) CallTraveller(3);
            else if (step == 3 && Elapsed > 1f) Interview();
            else if (step == 4 && Elapsed > 1f) CheckVerdict();
            else if (step == 5 && Elapsed > 1f) ForceClosing();
            else if (step == 6 && Elapsed > 2f) CheckResultsAndAdvance();
            else if (step == 7 && Elapsed > 4f) Day3Briefing();
            else if (step == 8 && Elapsed > 1f) CallTraveller(9);
            else if (step == 9 && Elapsed > 1f) Day3Hub();
            else if (step == 10 && !EditorApplication.isPlaying && Elapsed > 1f) Finish();
        }
        catch (Exception e)
        {
            Check(false, $"exception at step {step}: {e}");
            if (EditorApplication.isPlaying)
            {
                Next(10);
                EditorApplication.ExitPlaymode();
            }
            else
            {
                Finish();
            }
        }
    }

    // -----------------------------
    // Day 2
    // -----------------------------

    private static void StartShift(int next)
    {
        RunManager run = RunManager.Instance;
        int seed = SessionState.GetInt(SeedKey, 0);
        string got = run != null ? $"seed {run.World.runSeed}, day {run.World.day}" : "no RunManager";
        Check(run != null && run.World.runSeed == seed && run.World.day == 2, $"run pinned to seed {seed}, day 2 (got {got})");
        ClickBriefingStart();
        Next(next);
    }

    private static void ClickBriefingStart()
    {
        GameObject briefing = GameObject.Find("BriefingPanel");
        Transform start = briefing != null ? briefing.transform.Find("Paper/ActionButton") : null;
        if (start == null)
            throw new InvalidOperationException("no BriefingPanel/Paper/ActionButton");
        start.GetComponent<Button>().onClick.Invoke();
    }

    private static void CallTraveller(int next)
    {
        var ready = (Clickable)Field(Object.FindFirstObjectByType<GameManager>(), "readySign");
        if (!ready.Interactable)
        {
            if (Elapsed > 20f)
                throw new InvalidOperationException("READY never armed");
            return;
        }

        ready.onClick.Invoke();
        Next(next);
    }

    private static void Interview()
    {
        int slot = SessionState.GetInt(SlotKey, 1);
        string role = SessionState.GetString(RolesKey, string.Empty).Split(',')[slot - 1];
        string expectedName = SessionState.GetString(NamesKey, string.Empty).Split('|')[slot - 1];
        var ui = Object.FindFirstObjectByType<InvestigationUIController>();
        var inst = (CaseInstance)Field(ui, "_currentCase");
        Check(inst != null && inst.visitorGivenName == expectedName,
              $"slot {slot}: the desk shows the precomputed traveller '{expectedName}' (got '{(inst != null ? inst.visitorGivenName : "none")}')");

        var transcript = (TranscriptWindowController)Field(ui, "transcriptWindow");
        var runner = (DialogRunner)Field(ui, "_runner");

        if (slot == 1)
        {
            Check(Labels(ui).SequenceEqual(new[] { "Request Travel Passport", "Request Transit Permit", "Ask about home >", RumourEntry }),
                  $"slot 1: the hub lists [{string.Join(", ", Labels(ui))}]");

            // A request opens the document and speaks, without opening the transcript.
            int lines = runner.Transcript.Count;
            Click(ui, "Request Travel Passport");
            DocumentInstance passport = inst.documents.First(d => d.template.displayName == "Travel Passport");
            Check(DocumentWindow(passport).gameObject.activeSelf && runner.Transcript.Count == lines + 2 && !transcript.gameObject.activeSelf,
                  "slot 1: Request Travel Passport opens the passport and adds two lines, the transcript stays closed");

            Click(ui, "Ask about home >");
            Check(Labels(ui).SequenceEqual(new[] { "< Back", "Currency", "Language", "Device", "Capital", "Small talk" }),
                  $"slot 1: the ask menu lists [{string.Join(", ", Labels(ui))}]");
            Click(ui, "< Back");
        }
        else if (slot == 2)
        {
            Check(!Labels(ui).Contains(RumourEntry), $"slot 2: the completed rumour is not offered again [{string.Join(", ", Labels(ui))}]");
        }

        if (role == "capital")
        {
            if (Labels(ui).Contains("Ask about home >"))
                Click(ui, "Ask about home >");
            ((OSWindowChrome)Field(ui, "transcriptChrome")).Close();
            Click(ui, "Capital");
            Check(transcript.gameObject.activeSelf, $"slot {slot}: asking the capital opens the transcript");
            CheckRows(transcript, slot);

            InterviewAnswer said = inst.answers.First(a => a.category == ClueCategory.Geography);
            ClickTranscriptRow(transcript, "Line_q_capital.answer");
            ClickBookRow(ClueCategory.Geography, inst.originLabel);
            Check(ui.EvidenceCount == 1 && CompareText(ui).Contains("CAPITAL INCORRECT — traveller said: \"" + said.value + "\""),
                  $"slot {slot}: the spoken capital against the claim's Gazetteer row logs the deviation ({CompareText(ui)})");

            // The true home's row first: ShowPage rebuilt the book rows, so it is a new
            // pick, which clears the full pair and fills slot A. (Clicking the answer row
            // again first would clear the comparison instead: CompareController.Select
            // treats a click on a selected row as "clear", and nothing rebuilt the
            // transcript since.) The same answer row then forms the second pair.
            ClickBookRow(ClueCategory.Geography, inst.trueHomeLabel);
            ClickTranscriptRow(transcript, "Line_q_capital.answer");
            Check(ui.EvidenceCount == 1 && CompareText(ui).Contains("ALREADY DOCUMENTED — CAPITAL is in the Deviation Report"),
                  $"slot {slot}: the true home's row against the same answer says ALREADY DOCUMENTED ({CompareText(ui)})");
        }
        else if (role == "honest")
        {
            foreach ((string label, ClueCategory category, string row) in new[]
                     {
                         ("Currency", ClueCategory.Currency, "q_currency"), ("Language", ClueCategory.Language, "q_language"),
                         ("Device", ClueCategory.Technology, "q_device"), ("Capital", ClueCategory.Geography, "q_capital")
                     })
            {
                Ask(ui, label);
                ClickTranscriptRow(transcript, AnswerRowName(inst, row));
                ClickBookRow(category, inst.originLabel);
                Check(CompareText(ui).StartsWith("MATCH ") && ui.EvidenceCount == 0,
                      $"slot {slot}: the honest {label} answer matches the claim's row and logs nothing ({CompareText(ui)})");
            }
            CheckRows(transcript, slot);
        }
        else
        {
            DocumentField tell = FirstPagePlaceTell(inst);
            string label = tell.category == ClueCategory.Technology ? "Device" : tell.category.ToString();
            string question = tell.category == ClueCategory.Technology ? "q_device" : "q_" + tell.category.ToString().ToLowerInvariant();
            Ask(ui, label);
            ClickTranscriptRow(transcript, AnswerRowName(inst, question));
            ClickBookRow(tell.category, inst.originLabel);
            Check(CompareText(ui).StartsWith("MATCH ") && ui.EvidenceCount == 0,
                  $"slot {slot}: the Papers-tell liar answers {label} with the cover, which logs nothing ({CompareText(ui)})");
            CheckRows(transcript, slot);

            DocumentInstance doc = inst.documents.First(d => d.fields.Contains(tell));
            Click(ui, "< Back");
            Click(ui, $"Request {doc.template.displayName}");
            ClickDocumentRow(doc, tell.label);
            ClickBookRow(tell.category, inst.originLabel);
            Check(ui.EvidenceCount == 1, $"slot {slot}: the {tell.label} field against the claim's row logs the deviation ({CompareText(ui)})");
        }

        if (slot == 1)
        {
            // Escape and back: the transcript and its page are unchanged.
            if (!transcript.gameObject.activeSelf)
                ((OSWindowChrome)Field(ui, "transcriptChrome")).Open();
            string before = TranscriptState(transcript);
            var view = Object.FindFirstObjectByType<OfficeViewController>();
            view.FocusOffice();
            view.FocusMonitor();
            Check(TranscriptState(transcript) == before && transcript.gameObject.activeInHierarchy, "slot 1: leaving the monitor and coming back keeps the transcript and its page");

            WorstCaseRows(transcript);

            // The rumour: ask, hear more, note it; it ends back at the hub.
            if (Labels(ui).Contains("< Back"))
                Click(ui, "< Back");
            Click(ui, RumourEntry);
            Click(ui, "Tell me more.");
            Click(ui, "Noted. Thank you.");
            CheckRows(transcript, slot);
            List<DialogOutcome> outcomes = Object.FindFirstObjectByType<GameManager>().Ledger.dialogOutcomes;
            Check(outcomes.Count == 1 && outcomes[0].dialogId == "dlg_rumour" && outcomes[0].effectName == "Effect_Dialog_RumourHeard" &&
                  Labels(ui).Contains("Request Travel Passport") && !Labels(ui).Contains(RumourEntry),
                  $"slot 1: the rumour is completed and recorded, back at the hub [{string.Join(", ", Labels(ui))}]");
        }

        ((Button)Field(ui, role == "honest" ? "acceptButton" : "denyButton")).onClick.Invoke();
        Next(4);
    }

    private static void CheckVerdict()
    {
        int slot = SessionState.GetInt(SlotKey, 1);
        string role = SessionState.GetString(RolesKey, string.Empty).Split(',')[slot - 1];
        CaseVerdict v = Object.FindFirstObjectByType<GameManager>().Ledger.verdicts.Last();

        if (role == "honest")
            Check(v.accepted && v.correct && !v.citationIssued && !v.wasLiar, $"slot {slot}: honest traveller accepted, correct, no citation");
        else
            Check(!v.accepted && v.correct && !v.citationIssued && v.wasLiar, $"slot {slot}: {role} liar denied with evidence, correct, no citation");

        if (v.citationIssued)
            ((Button)Field(Object.FindFirstObjectByType<OfficeUIController>(), "citationContinueButton")).onClick.Invoke();

        SessionState.SetInt(SlotKey, slot + 1);
        Next(slot < 3 ? 2 : 5);
    }

    private static void ForceClosing()
    {
        Object.FindFirstObjectByType<ShiftClockDriver>().Clock.Tick(100000f);
        Next(6);
    }

    private static void CheckResultsAndAdvance()
    {
        var flow = Object.FindObjectsByType<DayFlowUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        Check(((GameObject)Field(flow, "resultsPanel")).activeInHierarchy, "the shift report is shown");

        WorldState saved = SaveSystem.Load();
        ActiveEffectEntry[] rumour = saved.timeline.activeEffects.Where(e => e.effectId == "Effect_Dialog_RumourHeard").ToArray();
        Check(saved.day == 2 && saved.HasFlag("dlg:dlg_rumour:done") && saved.HasFlag("rumour_calculators") &&
              rumour.Length == 1 && rumour[0].startDay == 3 && rumour[0].durationDays == 1,
              $"the end-of-shift save holds the done flag, the rumour flag and one rumour effect starting day 3 (flags [{string.Join(", ", saved.flags)}], effects {rumour.Length})");

        RunManager.Instance.AdvanceToNextDay();
        Next(7);
    }

    // -----------------------------
    // Day 3
    // -----------------------------

    private static void Day3Briefing()
    {
        var flow = Object.FindObjectsByType<DayFlowUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        string body = ((TMP_Text)Field(flow, "briefingBodyText")).text;
        Check(RunManager.Instance.World.day == 3 && body.Contains(RumourTip) && body.Contains(RulerNotice),
              $"day 3: the morning paper carries the rumour tip and the ruler notice ({body.Replace('\n', '/')})");
        ClickBriefingStart();
        Next(8);
    }

    private static void Day3Hub()
    {
        var ui = Object.FindFirstObjectByType<InvestigationUIController>();
        List<string> hub = Labels(ui);
        Check(hub.Contains(FollowUpEntry) && !hub.Contains(RumourEntry), $"day 3: the follow-up is offered and the rumour is not [{string.Join(", ", hub)}]");
        Click(ui, "Ask about home >");
        Check(Labels(ui).Contains("Ruler"), $"day 3: Ruler is askable [{string.Join(", ", Labels(ui))}]");
        Next(10);
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

    // -----------------------------
    // The interview through the real UI
    // -----------------------------

    /// <summary>The intercom's current buttons (the panel's live list; destroyed buttons are already out of it).</summary>
    private static List<Button> Buttons(InvestigationUIController ui) =>
        ((List<GameObject>)Field(Field(ui, "interactionPanel"), "_spawned")).Select(go => go.GetComponent<Button>()).ToList();

    private static List<string> Labels(InvestigationUIController ui) =>
        Buttons(ui).Select(b => b.GetComponentInChildren<TMP_Text>(true).text).ToList();

    private static void Click(InvestigationUIController ui, string label)
    {
        Button button = Buttons(ui).FirstOrDefault(b => b.GetComponentInChildren<TMP_Text>(true).text == label);
        if (button == null)
            throw new InvalidOperationException($"no intercom choice '{label}' in [{string.Join(", ", Labels(ui))}]");
        button.onClick.Invoke();
    }

    /// <summary>Asks a question from wherever the intercom is (the hub or the ask menu).</summary>
    private static void Ask(InvestigationUIController ui, string label)
    {
        if (Labels(ui).Contains("Ask about home >"))
            Click(ui, "Ask about home >");
        Click(ui, label);
    }

    /// <summary>The answer row's name: the claimed era's override id when it has one, else the default.</summary>
    private static string AnswerRowName(CaseInstance inst, string questionId)
    {
        var lib = AssetDatabase.LoadAssetAtPath<ContentLibrarySO>("Assets/Data/Content Library/ContentLibrary_Main.asset");
        InterviewQuestion q = lib.Questions.First(x => x.question.id == questionId).question;
        return "Line_" + q.AnswerFor(inst.claimedEra != null ? inst.claimedEra.id : null).id;
    }

    private static List<GameObject> Rows(PagedRowsWindow window) => (List<GameObject>)Field(window, "_rows");

    /// <summary>Clicks a transcript row by name: on the page shown when it is there, else on the first page that has it.</summary>
    private static void ClickTranscriptRow(TranscriptWindowController transcript, string rowName)
    {
        GameObject row = Rows(transcript).FirstOrDefault(r => r.name == rowName);
        for (int page = 0; row == null && page < 20; page++)
        {
            transcript.ShowPage(page);
            row = Rows(transcript).FirstOrDefault(r => r.name == rowName);
        }

        if (row == null)
            throw new InvalidOperationException($"no transcript row '{rowName}'");

        Button button = row.GetComponent<Button>();
        Check(button.enabled, $"{rowName} is a clickable answer row");
        button.onClick.Invoke();
    }

    private static void ClickBookRow(ClueCategory category, string originLabel)
    {
        foreach (ReferenceBookWindowController w in Object.FindObjectsByType<ReferenceBookWindowController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!(Field(w, "_book") is ReferenceBookSO book) || book.category != category)
                continue;

            foreach (int page in Enumerable.Range(0, 10))
            {
                w.ShowPage(page);
                GameObject row = Rows(w).FirstOrDefault(r => r.GetComponentsInChildren<TMP_Text>(true)[0].text == originLabel);
                if (row != null)
                {
                    row.GetComponent<Button>().onClick.Invoke();
                    return;
                }
            }
        }

        throw new InvalidOperationException($"no {category} book row '{originLabel}'");
    }

    private static DocumentWindowController DocumentWindow(DocumentInstance doc)
    {
        foreach (DocumentWindowController w in Object.FindObjectsByType<DocumentWindowController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (ReferenceEquals(Field(w, "_doc"), doc))
                return w;
        throw new InvalidOperationException($"no window for '{doc.template.displayName}'");
    }

    private static void ClickDocumentRow(DocumentInstance doc, string label)
    {
        foreach (GameObject row in (List<GameObject>)Field(DocumentWindow(doc), "_rows"))
        {
            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0 && texts[0].text == label)
            {
                row.GetComponent<Button>().onClick.Invoke();
                return;
            }
        }

        throw new InvalidOperationException($"no row '{label}' in {doc.template.displayName}");
    }

    private static string CompareText(InvestigationUIController ui) =>
        ((TMP_Text)Field(Field(ui, "compareController"), "compareText")).text;

    private static string TranscriptState(TranscriptWindowController transcript) =>
        ((TMP_Text)Field(transcript, "pageText")).text + " | " + string.Join(",", Rows(transcript).Select(r => r.name));

    /// <summary>Every row the transcript shows fits: no text overflows after a layout and mesh update.</summary>
    private static void CheckRows(TranscriptWindowController transcript, int slot)
    {
        var root = (RectTransform)Field(transcript, "entryRowsRoot");
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        foreach (GameObject row in Rows(transcript))
        {
            foreach (TMP_Text text in row.GetComponentsInChildren<TMP_Text>(true))
            {
                text.ForceMeshUpdate();
                Check(!text.isTextOverflowing, $"slot {slot}: row {row.name} shows '{text.text}' without overflow (size {text.fontSize:0.#})");
            }
        }
    }

    /// <summary>The worst-case lines (the 70-character claim, the 92-character rumour line, a 100-character sentence) fit a row.</summary>
    private static void WorstCaseRows(TranscriptWindowController transcript)
    {
        var root = (RectTransform)Field(transcript, "entryRowsRoot");
        var template = (GameObject)Field(transcript, "entryRowTemplate");
        foreach (string sentence in new[]
                 {
                     "I request passage home to Ottoman Iraq (Baghdad Vilayet) (Industrial).",
                     "They say a courier from your own century carries them. Watch for anyone who counts too fast.",
                     "Please stamp my papers quickly, because the ferry home leaves before the evening bells ring out now."
                 })
        {
            GameObject row = Object.Instantiate(template, root);
            row.SetActive(true);
            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            texts[0].text = "Hatshepsut";
            texts[1].text = sentence;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            texts[0].ForceMeshUpdate();
            texts[1].ForceMeshUpdate();
            Check(!texts[0].isTextOverflowing && !texts[1].isTextOverflowing,
                  $"a {sentence.Length}-character line fits a transcript row (size {texts[1].fontSize:0.#}, {texts[1].textInfo.lineCount} line(s))");
            Object.DestroyImmediate(row);
        }
    }

    // -----------------------------
    // Plumbing
    // -----------------------------

    private static void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Log)
            SessionState.SetString(LogKey, SessionState.GetString(LogKey, string.Empty) + $"[{type}] {message}\n");
    }

    /// <summary>A field of the object's type or any base type (PagedRowsWindow keeps its rows private).</summary>
    private static object Field(object target, string name)
    {
        for (Type t = target.GetType(); t != null; t = t.BaseType)
        {
            FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null)
                return f.GetValue(target);
        }

        throw new MissingFieldException(target.GetType().Name, name);
    }

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
Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe' -ArgumentList @('-projectPath','E:\unity\NOPE-feat-clock','-executeMethod','TimeDeskP3PlaySmoke.Run','-logFile','C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\unity_p3_playsmoke.log') -PassThru -Wait
```
Expected: exit code 0; `SCRATCH/p3_playsmoke_report.txt` holds `seed=… roles=… names=…` and `save path=… hadSave=…`, then only `PASS` lines (every check listed above, in play order) and `fails=0`. `SCRATCH/nope_save_backup_p3.json` must not remain.

**Recovery if Unity died before `Finish()`** (tool timeout, crash, a hang in play mode): the player's real save may still be in the scratchpad. The save path is shared with the main `E:\unity\NOPE` project (same company and product name), so restore it before anything else (PowerShell):

```powershell
$s = 'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
$m = Select-String -Path "$s\p3_playsmoke_report.txt" -Pattern '^save path=(.+) hadSave=(True|False)$' | Select-Object -First 1
$path = $m.Matches[0].Groups[1].Value; $hadSave = $m.Matches[0].Groups[2].Value -eq 'True'
if (Test-Path "$s\nope_save_backup_p3.json") { Copy-Item "$s\nope_save_backup_p3.json" $path -Force; Remove-Item "$s\nope_save_backup_p3.json" }
elseif (-not $hadSave -and (Test-Path $path)) { Remove-Item $path }
```

Then revert `Assets/Resources/RunConfig.asset` (Step 8), fix the cause and re-run Step 6.

- [ ] **Step 7: Offline re-check**

Run the compile check and the test run. Expected: exit 0 / exit 0; `passed 336, failed 0`.

- [ ] **Step 8: Hygiene**

```bash
cd /e/unity/NOPE-feat-clock && rm -f Assets/Editor/_TimeDeskP3Automation.cs Assets/Editor/_TimeDeskP3Automation.cs.meta Assets/Editor/_TimeDeskP3PlaySmoke.cs Assets/Editor/_TimeDeskP3PlaySmoke.cs.meta && git checkout -- Assembly-CSharp.csproj Assembly-CSharp-Editor.csproj TimeDesk.Domain.csproj TimeDeskEditMode.csproj NOPE.sln ProjectSettings "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset" Assets/Scenes/OfficeScene.unity Assets/Resources/RunConfig.asset && git status --short
```
Expected: only `?? NOPE-feat-clock.sln` and `?? TimeDesk.Visuals.csproj` (the verification changes no committed file; the play-through's glyphs only touch the reverted TMP fallback atlas).

- [ ] **Step 9: Append the verification record to the spec**

Append this section to the end of `docs/superpowers/specs/2026-09-24-dialog-questions-design.md` (after "Review notes"), filling every `{…}` from the named report line (the spec file is LF; use the Edit tool):

```markdown
## 8. Verification ({date of the run})

Run in the branch's own Unity 6000.4.11f1 editor through temporary `-executeMethod` scripts (not committed), at `{short hash of HEAD}`:

- Baseline before any piece-3 code (Task 0): seeds 12345 and 999 × days 1–3, 60 slots dumped with their lies and papers; the three authored triggers' results on 20 crafted worlds (60, none passing).
- Content (step 1): Generate World ran twice and the second run changed no file; `Assets/Data/World/Interview` holds 6 questions, 2 dialogs and 3 unlock triggers; the library holds 5 books, 6 questions, 2 dialogs, the 3 authored triggers then the 3 unlock triggers, 4 upgrades and the rumour effect, with menu capacity 8 and 100-character lines; both dialogs are one-shot; every question's and unlock trigger's conditions are as §2.14 says; the day plans hold their channels. Validate Content Library: no issues. The generator's checks on five edited copies of the source each gave exactly the expected error(s) (a missing `fromDay`; a seventh question, whose repeated category adds the one-per-category error; a line id equal to a choice-label id; a 101-character line; a dialog effect with a PayRateBonus op).
- Gates and triggers (step 2): the authored triggers' 60 results equal the baseline. Unlock notices fire the night before the first askable day (capital on the night of day 1, ruler on the night of day 2, birth date the night Interview Protocols is owned, never without it), once each. The day-start interview asks {askable per day from the report} and lets spoken tells come only from the day-gated questions, with or without the upgrade.
- World check (step 3; seeds 12345 and 999 × days 1–3 with and without Interview Protocols, plus 200 seeds × days 1–3 × both): {problems from "world check problems="} problems. Deterministic; identities equal the baseline on every day, and day 1's lies and papers equal it in full; the upgrade changes no lie and only adds the birth-date answer. Every spoken tell proves against the claim's row and names the true home on its row, and nothing else; honest answers prove nothing; no Politics tell between Egypt and Greece (Early modern). Sweep: spoken-tell share {the "spoken shares" of the coverage line}; options {the "options seen" list}; no BirthDate Answer tell; no NoPossibleLie warning.
- EditMode suite (step 4): {passed} passed. The one failure is the known third-party `UnitySkills.Tests.Core.PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly`. Offline Domain run: 336/336.
- Builder (step 5): Build Office UI ran twice on OfficeScene with no error; the two semantic scene dumps ({lines} lines) are equal. `TranscriptWindow` is wired (8 rows per page, built closed), `IconClueLog` opens it, `IconClueLogWindow` is gone, the intercom's `Actions` is masked, the icons require `archive_access` / nothing / `adv_scanner`, and the Dialect window reads "Dialect" / "Notes on accents and phrasing. (placeholder)". A third, unsaved build with an in-memory menu capacity of 9 and Archive Access under another id reported exactly the builder's two errors (the intercom fits 8 choices; Lexicon's upgrade is unknown) and changed no file. The rebuilt scene is committed.
- Play-through (step 6; run seed {seed}, a new run started on day 2): {one sentence per role, from the report: the hub and ask menu contents, the request, the capital liar's "CAPITAL INCORRECT — traveller said: …" and "ALREADY DOCUMENTED", the honest MATCHes, the Papers-tell liar's cover answer and paper proof}. No transcript row overflowed, including the 70-, 92- and 100-character worst cases. Escape and back kept the transcript. The rumour dialog was completed and left the next traveller's hub. After forced closing the save held the done flag, `rumour_calculators` and the rumour effect starting on day 3; day 3's paper carried the rumour tip and the ruler notice, offered the follow-up dialog (not the rumour) and asked about the ruler. No warnings or errors.
```

- [ ] **Step 10: Commit the record**

```bash
cd /e/unity/NOPE-feat-clock && git add docs/superpowers/specs/2026-09-24-dialog-questions-design.md && git commit -F - <<'EOF'
docs(spec): dialog + questions verification record

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```
Final `git status --short`: only `?? NOPE-feat-clock.sln` and `?? TimeDesk.Visuals.csproj`.

---

## Merging this branch (spec §7 "Scene merge")

`main` has moved to `b7f8671`, whose Codex art checkpoint changed `Assets/Scenes/OfficeScene.unity`, and the main checkout still has uncommitted scene work; this branch commits a rebuilt `OfficeScene.unity` (Task 17). A textual merge of the scene YAML is impractical (every builder run rewrites thousands of lines with new fileIDs). When this branch is merged:
1. Coordinate with the Codex scene work first (ideally Codex commits its remaining scene changes).
2. On a conflict in `OfficeScene.unity`, never hand-merge it: take the other side's file (`git checkout --theirs Assets/Scenes/OfficeScene.unity` when merging `main` into this branch), finish the merge of everything else, then run `Tools > TimeDesk > Build Office UI (HUD + Panels)` on the merged tree in Unity and save the scene.
3. Repeat Task 17's scene half on the merged tree (two builds, the semantic dumps equal, the scene checks) and Task 18's Steps 5–6 (the play-through), then commit the rebuilt scene.

---

## Spec coverage

| Spec | Task |
|---|---|
| Q1, R19 (custom Domain dialog graph and runner; `AuthoredDialog` as the seam) | 7, 8 |
| X1, R1, R2, R3, R23 (tells as (category, channel) options; one channel per category; day-gated questions only; shared values) | 6, 9, 14 |
| Q3, Q12, R13, R17 (answers are compare-clickable evidence; `Prove`/`Add`; one per category from any source; ALREADY DOCUMENTED; `ClueLabels`) | 5, 14 |
| Q4, Q11, R25 (hub, ask sub-menu with "< Back" first, one-shot questions, repeatable requests, menu capacity) | 8, 12, 13, 14, 16, 17 |
| Q5, R15, X7, X8, R16 (systemic questions carry proof; small talk on `Seeds.ForDialog`, never evidence; no Chatter content) | 4, 7, 13, 14 |
| Q6, X3, R4, R6, R22 (`TriggerConditionType` in Domain + `UpgradeOwned`; `Gates` over `GateSnapshot`; projection; DayAtLeast readings; authored triggers unchanged) | 1, 10, 18 |
| Q7, R21 (Capitals Gazetteer, Rulers & Regents; five book windows) | 12, 13 |
| Q8, X10, R14, R18, R20, R27 (world_source sections, generator checks and ids, ASCII, provable categories, required `fromDay`, `repeatable`) | 13, 17 |
| Q9, X4, Q10, R10 (transcript window in the Clue Log slot, paging, wrapping rows, row names) | 11, 14, 16 |
| Q13 (no tell when values match) | 3, 6 (unchanged rule, pinned by `ForgeryTests`/`LiesTests`) |
| Q14, X5, R24 (dialog effects at the end of the shift; instant ops and lines only; one-shot memory via `FlagKeys.DialogDone`) | 1, 2, 9, 12, 13, 15 |
| Q15 (no time cost) | 14 (FEATURES :71) |
| Q16 (stable ids) | 7, 13 |
| Q17, R5, X2 (unlock schedule; Interview Protocols; unlock-announcement triggers) | 12, 13, 17, 18 |
| Q18, R9 (real icon upgrade ids, reported when unknown; honest Dialect) | 16, 17 |
| X6, R7 (`InterviewDay` injected; UI never touches `WorldState`) | 9, 14 |
| X9, R8 (honorific in the opener; bare given name as speaker) | 7, 14 |
| F1, R11 (fallback prints the interview and the claimed place's entries) | 14 |
| C1 (citation and scanner hint cover answers) | 14 |
| R12 (interview wiring gates the Answer channel; warning) | 14 |
| R26 (claim line is content) | 7, 13, 14 |
| §2.17 every file that changes | File map above |
| §3.1 code removed (`TryRegister`, `CategoryLabel`, enum copies, the action loop, the Clue Log placeholder, the book listing, the intro and claim literals) | 1, 2, 5, 14, 16 |
| §3.3 FEATURES lines | 5 (:60 note, labels), 12 (:96), 13 (:95, books), 14 (:11, :36, :44, :45, :50 and the question/answer bullets, :55–61, :63, :71, :86), 15 (:12, scoring bullet), 16 (:31, :33, Clue Log bullet, :97) |
| §5 tests | 1–9 |
| §6 verification (baseline, content, gates, world check, suite, builder, play-through, hygiene) | 0, 17, 18 |

---

## Notes for the reviewer (where this plan settles what the spec left open)

1. **The transcript window is rebuilt on every Build Office UI run** (`DestroyChildIfPresent(windowLayer, "TranscriptWindow")` before `Panel`, like Records), instead of being found and re-laid-out. Found-or-created, it would sit before the Scanner/Records/document-template windows after the second build but after them after the first (those three are destroyed and appended on every run), so the window layer's order, and with it the order-sensitive semantic dump of spec §6 step 5, would differ between the two builds. Rebuilt, every build gives the same order, and the row template always has the transcript layout without a separate "re-apply" path.
2. **The seventh-question negative check yields two errors.** A seventh question must repeat a category (six are all the provable ones), so besides the capacity error ("The ask menu holds 9 choices …") the generator also reports "asks about Technology again (one question per category)". Task 17 expects exactly those two messages.
3. **Id-collision wording.** The spec's example owner "dialog 'dlg_rumour' line 'start'" is written as "dialog 'dlg_rumour' node 'start'" (the line sits in node `start`); the message is otherwise the spec's: "Line id 'dlg_rumour.more' is used by dialog 'dlg_rumour' node 'start' and by choice 'more' of dialog 'dlg_rumour'."
4. **One home for the dialog-effect error text:** `ContentLibraryValidator.DialogEffectOpError` (public), which the generator calls, so "the same message as the generator's" (§2.13) cannot drift.
5. **Enum parsing in the new checks** uses `Enum.TryParse` plus `Enum.IsDefined` (`WorldContentGenerator.ParseEnum`), so a number such as `"7"` is not accepted as a channel, category, speaker or condition type.
6. **Checks slightly beyond the spec's list, in the spirit of R27** (a missing value must fail loudly): an override's prompt must be non-blank, an authored dialog line must be non-blank, and a counter condition's key must be non-blank (the spec names the flag key only).
7. **Book placement (R21) lands with the books (Task 13)**, not with the interview wiring, so five books never open off-screen in any commit.
8. **Between Task 6 and Task 14** the day plans (not yet regenerated) keep the default Papers channel and no question carries a tell, so every intermediate commit plays exactly like piece 2; Task 17 regenerates the day plans.
9. **`TimelineService.BuildInterviewDay` is the one projection** of the library's questions and dialogs onto the day-start world (Task 14). `GameManager.BuildInterviewDay` calls it and only logs `ContentProblems`; both Unity sessions of Task 18 call it too, so the world check and the seed pick run the shipped code. Its helpers `ToGate`, `ToGates` and `Snapshot` are therefore private (Task 10): spec R6 and §2.8 make them public with `GameManager.BuildInterviewDay` as their second caller, and that caller now sits inside `TimelineService`.
10. **Missing interview wording warns once a day** (Task 14): spec §2.9 says a blank opener or claim "logs one warning"; `GenerateDayCases` checks the library's wording once per day instead of once per traveller.
11. **Rules the generator and the validator share have one home each:** "the template holds {token}" is Domain's `Interview.HoldsToken` (Task 7, tested); "the most documents one traveller carries" is `ContentLibraryValidator.MaxDocuments(IEnumerable<CaseBlueprintSO>)`, which each side calls with its own blueprints (the generator's source blueprints; the validator's day-plan, forced and legendary blueprints); the forced blueprints come from `DayPlanSO.ForcedBlueprints`, not from the serialized field names; a place's birth-year range is `WorldContentGenerator.BirthYears`, which `MakePlace` writes and the line-length check measures (keeping only dates `BirthDates.TryParse` reads back, so "no year 0" stays `BirthDates`' rule).

---

## Review of 2026-09-24: findings not taken, or taken differently

All twelve findings were verified against the code and the plan, and applied: (1) the ALREADY DOCUMENTED click order in Task 18, (2) the plan's own commit before Task 0, (3) canonical `.meta` files in Task 12, (4) one `ReferenceResolution` in the builder, (5) one birth-year range in the generator, (6) one home for each rule the generator and the validator share, (7) one interview projection, `TimelineService.BuildInterviewDay`, (8) the two vacuous assertions in Tasks 5 and 9, (9) one wording warning a day in Task 14, (10) the intercom field's doc and the Clue Log bullet, (11) the builder's two reports exercised in Task 17, and (12) the folder meta in Task 17's recovery. The parts below were not taken, or were taken in another form:

1. **Finding 6, "any era or place has small talk" (not taken).** The generator's `anySmallTalk` and the validator's `smallTalk` stay separate one-line predicates. They read different data at different times: the generator checks the source's `string[]` lines before anything is written, and the validator checks the generated assets' `List<LineText>` afterwards. What they share is only "is any list non-empty". The rule that uses the answer, what small talk adds to the ask menu, already has one home: `DialogChecks.MenuProblems`. A shared helper would be a generic "any non-empty list" wrapper with no game rule in it. The finding's own fix list does not include this item either.
2. **Finding 2 (taken differently).** The plan was committed alone (`docs(plan): dialog + questions implementation plan`) as part of accepting this review. So there is no executor step that commits it. A Conventions line records the commit, and Task 0 Step 1 now expects it unconditionally (with a stop-and-commit instruction if the plan is ever found untracked).
3. **Finding 7 (taken as proposed, one detail).** Each automation session keeps a one-line local `BuildInterviewDay(lib, world)` that calls `TimelineService.BuildInterviewDay(lib, world, new ShiftLedger())`. It is a call to the shipped method, not a copy of it, and it keeps the three call sites unchanged.
4. **Finding 11 (the first option, widened).** The unsaved third build exercises both reporting branches, the menu capacity and the unknown icon upgrade id, not just the capacity. Instead of "restore without saving", it checks that the scene, library and upgrade files on disk are byte-identical afterwards. The builder calls `AssetDatabase.SaveAssets`, so that check is the proof that nothing was saved.
