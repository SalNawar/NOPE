# Physical desk and traveller wheel (piece 7) Implementation Plan

> **For agentic workers:** execute task-by-task (REQUIRED SUB-SKILL: superpowers:subagent-driven-development or superpowers:executing-plans). Steps use checkbox (`- [ ]`) syntax. Spec: `docs/superpowers/specs/2026-09-25-physical-desk-design.md`. Binding decisions: `SCRATCH/piece7_decisions.md`. House rules: `C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\HOUSE_RULES.md` (read it before Task 0).

**Goal:** The booth becomes a desk you work at. The PC's desktop is drawn live on the CRT's glass and the camera pushes in on it when you click the CRT; the screen has a power button; documents are physical papers the traveller hands over (the passport when they step up, the permit when you ask), which you drag around the desk and drop on the desk scanner to open their scanned copies on the PC; you talk to the traveller through a wheel of choices that opens around them, and their replies show in a speech bubble as well as in the PC transcript; the other desk objects react to clicks. Seams are laid for decoration (item 7) and translation (item 8).

**Architecture:** Every rule is Domain or Visuals and tested first: the booth's input table (`BoothRules`), screen power with its wake and citation-hold rules (`PcScreen`), one case's papers, drops, scans and the day-1 notes (`DeskPapers`, `DeskHints`), the desk and window geometry, paper stacking and sorting bands (`DeskGeometry`), the wheel's ring and fit (`RadialLayout`), the monitor framing (`MonitorFraming`), the props' reaction poses (`ReactionCurve`), the display-text seam (`DisplayText`), and the interview's hand-over requests, Back kind and the traveller's reply (`Dialog.cs`, `InterviewScript`). Assembly-CSharp is glue: `MonitorScreen` (the desktop canvas on the glass, power), `OfficeViewController` and `CinemachineCameraRig` (focus, settling, framing), the desk components (`DeskSurface`, `DeskDraggable`, `DeskDocument`, `DeskScanner`, `DeskController`, `DeskReaction`, `DeskSlot`, `DeskItem`), `TravellerView`, the overlay UI (`TravellerWheel`, `RadialLayoutGroup`, `OverlayCallout`, `OverlayProjection`), and `BoothCoordinator`, which applies `BoothRules` to all of them; `GameManager` sets the booth's phase through one presence transition and holds the screen for a citation slip; `InvestigationUIController` hands documents over to the desk (or straight to their windows where no desk is wired) and plays the interview on the wheel. Knobs live in two new ScriptableObjects (`DeskConfigSO`, `DeskReactionSO`) that the builder creates. The scene is rebuilt by `Tools > TimeDesk > Build Office UI` (its booth and desk parts in the new partial `OfficeSceneUIBuilder.Desk.cs`); `world_source.json` and the generated content do not change.

**Tech Stack:** Unity 6000.4.11f1, C# 9, NUnit EditMode tests (`TimeDeskEditMode` references only `TimeDesk.Domain` and `TimeDesk.Visuals`), uGUI + TextMeshPro, Cinemachine 3, the Input System (`InputSystemUIInputModule`), Python 3 helper scripts in the scratchpad (`subs.py`, `cut.py`, `subsn.py`, the new `rewrite.py`, `make_meta.py`, `compile_check.py`, the reflection test runner).

---

## Conventions used by every task

- **Worktree:** `E:\unity\NOPE-feat-clock`, branch `feat/physical-desk` (checked out; never switch branches). Never touch `E:\unity\NOPE` (Codex works there on `art` with a Unity editor open). Never edit `Assets/Scenes/OfficeScene_HybridArt.unity` (it is opened and played in Task 20, never saved). Never push, rebase or amend.
- **SCRATCH** below means `C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad`.
- **Line endings.** CRLF (working tree): `OfficeViewController.cs`, `ICameraRig.cs`, `CinemachineCameraRig.cs`, `InvestigationUIController.cs`, `InteractionPanelController.cs`, `DraggableWindow.cs`, `DocumentWindowController.cs`, `DesktopShell.cs`, `CompareController.cs`, `DayFlowUIController.cs`, `GameManager.cs`, `DocumentTemplateSO.cs`, `DiscrepancyLog.cs`, `ContentLibraryValidator.cs`, `DocTemplate_Passport.asset`, `docs/FEATURES.md`, `docs/ART_ASSET_LIST.md`, `ArtDeliverables/TimeDesk/OFFICE_RESET_BRIEF.md`, `ArtDeliverables/TimeDesk/HybridScene/SCENE_CONTRACT.md` and `docs/superpowers/plans/2026-09-25-code-audit-overhaul-plan.md`. LF: `HoverHighlighter.cs`, `TranscriptWindowController.cs`, `OfficeSceneUIBuilder.cs`, `WorldContentGenerator.cs`, `Dialog.cs`, `InterviewScript.cs`, `InterviewContent.cs`, `InterviewScriptTests.cs`, `InterviewDayTests.cs`, the two draft specs and the physical-desk spec. Existing files are edited only through the task's Python script: `SCRATCH/subs.py` (`apply`: exact pairs, each old text must match exactly once) or `SCRATCH/rewrite.py` (`rewrite`: a whole-file replacement, created in Task 0, that first checks the file's current text against a hash so a file changed since this plan was written fails loudly). Both normalise CRLF to LF for matching and restore the file's own ending, so line endings never change. A file replaced whole is first written with the Write tool to the staging folder `SCRATCH/p7stage/` (never into the worktree), then `rewrite` copies it in. New files are written whole with the Write tool (LF).
- **Every new `.cs` under `Assets` and every new folder gets a `.meta`** in the same commit: `python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/make_meta.py" <paths>` (run from the worktree root, as each task's step shows). Unity writes the metas of generated assets itself (Task 19).
- **Compile check** (run from Bash):
  `python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/compile_check.py" 'E:\unity\NOPE-feat-clock'`
  Pass = both `== Assembly-CSharp-Editor.csproj: exit 0` and `== TimeDeskEditMode.csproj: exit 0` with `0 Error(s)`.
- **Test run** (after a passing compile check; an optional last argument filters by `Class.Method` substring):
  `"C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/runner/bin/Debug/net10.0/runner.exe" 'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\cc\Temp\Bin\Debug'`
  The branch base gives `passed 337, failed 0`. Each task states the new total.
- **Running a task script:** save the block to the named file with the Write tool, then `python "<that file>"`. Every script prints `ok <path> crlf|lf` or `rewrote <path> crlf|lf` per file it touched and exits non-zero on the first text that does not match; then nothing after it ran. If a script reports `expected 1 match, found 0` or `expected text sha1 …`, the file changed after this plan was written: re-read it and adapt that one edit (the code is the truth).
- **This plan is committed alone before Task 0** (`docs(plan): the physical desk and basic traveller wheel implementation plan`, made when its review was accepted), so the tree is clean when Task 0 starts and every "`git status --short` shows only the two untracked files" expectation below holds. Task 0 Step 1 checks that commit.
- **Commits:** `cd /e/unity/NOPE-feat-clock && git add <exact paths> && git commit -F - <<'EOF' … EOF`. Every message ends with a blank line and `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Never stage `NOPE-feat-clock.sln`, `TimeDesk.Visuals.csproj` or `_TimeDesk*` files. Never stage files you did not create or change in the task.
- **Assembly-CSharp is not unit-testable** (the test assembly sees only Domain and Visuals). Glue tasks therefore have no failing-test step: their rules are the Domain and Visuals calls tested earlier, and their behaviour is checked in Unity in Tasks 19 and 20.
- **The scene is rebuilt once, in Task 19.** Tasks 15–17 change the builder in three commits; until Task 19 the committed `OfficeScene.unity` is the old scene, which the code of every commit keeps playable (every new reference is optional). Players get the builder's behaviour only with the rebuilt scene, so the builder-driven behaviour lines of `docs/FEATURES.md` change in Task 19's scene commit (`feat(scene): …`), never in a builder commit; behaviour that code changes by itself (one-shot requests, the passport on arrival, the window clamp, the warnings, the results copy) is recorded in its own commit. Any `fix:` that changes behaviour or a documented default updates FEATURES in that same commit.
- Every task leaves both projects compiling and the suite green.
- **Pre-verified** (and again after the plan review's revisions): every script and whole file of this plan was replayed in order against a copy of the worktree at `25a2c50` (`SCRATCH/p7/dry.py`, output `SCRATCH/p7/dry_out.txt`): every scripted edit matched exactly once, CRLF/LF were preserved, each "expect failure" step failed with the stated errors, each task compiled, the stated test totals were observed, each task's commit stages exactly the files it changed (the Unity tasks' generated files aside), and the temporary Unity automation of Tasks 0, 19 and 20 compiled against the code it runs on.

## File map

| File | Responsibility | Task |
|---|---|---|
| `SCRATCH/rewrite.py`, `SCRATCH/p7stage/` (not committed) | whole-file, EOL-preserving, hash-checked replacement; the staging folder | 0 |
| `Assets/Editor/_TimeDeskP7Baseline.cs` (never committed) | the pre-change generation dump and the base scene's null references | 0 |
| `Assets/Scripts/Domain/BoothRules.cs` (+ .meta) | new: `BoothPhase`, `BoothContext`, `BoothInput`, `BoothRules.Evaluate` | 1 |
| `Assets/Tests/EditMode/BoothRulesTests.cs` (+ .meta) | new: the input table, one test per output | 1 |
| `Assets/Scripts/Domain/PcScreen.cs` (+ .meta) | new: `WakeReason`, `PcWakeRules`, `PcScreen` (toggle, off, wake, hold) | 2 |
| `Assets/Tests/EditMode/PcScreenTests.cs` (+ .meta) | new | 2 |
| `Assets/Scripts/Domain/DeskPapers.cs` (+ .meta) | new: `DocumentHandOver`, `DocumentHandOvers`, `CaseDocument`, `CaseDocuments`, `DropOutcome`, `DeskPapers`, `DeskHints` | 3 |
| `Assets/Tests/EditMode/DeskPapersTests.cs` (+ .meta) | new: states, the drop decision table, scans, the notes | 3 |
| `Assets/Scripts/Visuals/DeskGeometry.cs` (+ .meta) | new: `DeskRect`, `RectClamp`, `PaperStack`, `SortingBands` | 4 |
| `Assets/Tests/EditMode/DeskGeometryTests.cs` (+ .meta) | new | 4 |
| `Assets/Scripts/Visuals/RadialLayout.cs` (+ .meta) | new: `Point`, `MaxFit`, `Extent` | 5 |
| `Assets/Tests/EditMode/RadialLayoutTests.cs` (+ .meta) | new | 5 |
| `Assets/Scripts/Visuals/MonitorFraming.cs`, `ReactionCurve.cs` (+ .metas) | new: the focus framing; `ReactionKind`, `ReactionPose`, `ReactionCurve` | 6 |
| `Assets/Tests/EditMode/MonitorFramingTests.cs`, `ReactionCurveTests.cs` (+ .metas) | new | 6 |
| `Assets/Scripts/Visuals/DisplayText.cs` (+ .meta) | new: `TextMedium`, `DisplayText.For` | 7 |
| `Assets/Tests/EditMode/DisplayTextTests.cs` (+ .meta) | new | 7 |
| `Assets/Scripts/UI/TranscriptWindowController.cs`, `DocumentWindowController.cs` | display text through the seam; the "Traveller · …" compare label; docs | 7 |
| `Assets/Scripts/UI/CompareController.cs`, `Assets/Scripts/Domain/DiscrepancyLog.cs` | docs: compared values are canonical | 7 |
| `Assets/Scripts/Domain/Dialog.cs`, `InterviewScript.cs`, `InterviewContent.cs` | `HandOverDocument`, `DialogChoiceKind`/`Kind`, `documents`, one-shot requests, `SpokenSince`, "traveller wheel" wording | 8 |
| `Assets/Tests/EditMode/InterviewScriptTests.cs`, `InterviewDayTests.cs` | the hand-over, one-shot, Back kind, `SpokenSince` and wording tests | 8 |
| `Assets/Scripts/DocumentTemplateSO.cs` | `handOver` | 8 |
| `Assets/Scripts/UI/InvestigationUIController.cs` | the no-desk hand-over path, document icons, `_currentCase` reset (Task 8); the desk, the wheel, the idle line, the window layout (Task 14) | 8, 14 |
| `Assets/Data/Investigation/DocTemplate_Passport.asset` | `handOver: 1` | 9 |
| `Assets/Editor/ContentLibraryValidator.cs`, `WorldContentGenerator.cs` | `MaxRequestedDocuments`, public `TravellerBlueprints`, docs | 9 |
| `Assets/Scripts/UI/DraggableWindow.cs` | parent-space drag, the clamp inside the screen | 10 |
| `Assets/Scripts/UI/HoverHighlighter.cs` | `PurgeDeadOutlines` | 10 |
| `Assets/Scripts/UI/InteractionPanelController.cs` | the centre slot; `Clear` deactivates first | 10 |
| `Assets/Scripts/UI/RadialLayoutGroup.cs`, `OverlayProjection.cs`, `OverlayCallout.cs` (+ .metas) | new: the ring layout, overlay placement (the canvas rect resolved once by each caller), the timed label | 10 |
| `Assets/Scripts/Office/ICameraRig.cs`, `CinemachineCameraRig.cs`, `OfficeViewController.cs` | settling, the brain, the framing, the legacy desktop path | 11 |
| `Assets/Scripts/Office/DeskConfigSO.cs`, `MonitorScreen.cs` (+ .metas) | new: the desk knobs; the live screen | 11 |
| `Assets/Scripts/UI/DesktopShell.cs` | `quitButton`, `screenOffButton`, `TurnOffScreen` | 11 |
| `Assets/Scripts/Office/Desk/` (+ folder .meta) with `DeskSurface`, `DeskDraggable`, `DeskDocument`, `DeskScanner`, `DeskController`, `DeskReaction`, `DeskReactionSO`, `DeskSlot`, `DeskItem` (+ .metas) | new: the desk | 12 |
| `Assets/Scripts/Characters/` (+ folder .meta), `TravellerView.cs` (+ .meta) | new: the booth figure seam | 13 |
| `Assets/Scripts/UI/TravellerWheel.cs`, `Assets/Scripts/Office/BoothCoordinator.cs` (+ .metas) | new: the wheel; the booth's rules applied | 13 |
| `Assets/Scripts/GameManager.cs` | the presence transition, the booth's phases and day, the citation hold | 13 |
| `Assets/Scripts/UI/DayFlowUIController.cs` | "(log a deviation before denying)" | 14 |
| `Assets/Editor/OfficeSceneUIBuilder.cs` (partial) | the World Space desktop, the monitor wiring, READY cleared, the taskbar and Start menu, `WirePersistentVoid` clearing through `ClearPersistentCalls` (Task 15); the 4:3 desktop, the wheel and callouts, the idle line, renames (Task 16); the desk's call sites, the wiring, 16 raycast hits (Task 17) | 15, 16, 17 |
| `Assets/Editor/OfficeSceneUIBuilder.Desk.cs` (+ .meta) | new partial: the monitor screen and hit zones, `ClearPersistentCalls` (Task 15); the wheel and callouts (Task 16); the desk, props, slots, traveller and coordinator (Task 17) | 15, 16, 17 |
| `docs/FEATURES.md` | the behaviour contract, in the commit of each behaviour (the builder's lines with the rebuilt scene) | 8, 9, 10, 14, 19 |
| `docs/superpowers/drafts/specs/2026-09-24-characters-design.md`, `2026-09-24-ui-reacts-design.md` | amendment blocks | 18 |
| `docs/superpowers/plans/2026-09-25-code-audit-overhaul-plan.md`, `ArtDeliverables/TimeDesk/OFFICE_RESET_BRIEF.md`, `ArtDeliverables/TimeDesk/HybridScene/SCENE_CONTRACT.md`, `docs/ART_ASSET_LIST.md` | the piece-8 line; appended art amendments | 18 |
| `Assets/Scenes/OfficeScene.unity` (committed with its FEATURES lines), `Assets/Data/Config/Desk_Default.asset`, `Assets/Data/Config/DeskReactions/*` (+ folder meta), `Assets/Art/Office/Placeholder/{paper,crt_power,crt_led}.png` (+ metas), `Assets/Art/Generated/xp_bliss.png.meta` | rebuilt / created by Build Office UI in Unity | 19 |
| `Assets/Editor/_TimeDeskP7Build.cs`, `_TimeDeskP7Automation.cs`, `_TimeDeskP7PlaySmoke.cs`, `_TimeDeskP7Scenes.cs` (never committed) | temporary Unity automation | 19, 20 |
| `docs/superpowers/specs/2026-09-25-physical-desk-design.md` | aligned with this plan's departures; the verification record (§9) | 21 |

---

### Task 0: Setup and the pre-change baseline (Unity)

Spec §6 step 0 needs, **before any piece-7 code**, a dump of case generation (who each traveller is, their lie with its channels, every paper value and every answer) made exactly as `GameManager.Start` makes it, so Task 20 can prove the generation is byte-identical (V7); and the base scene's `InterviewReachable` ("spoken"). Task 19's no-missed-wiring check also needs the null references the changed components already had in the base scene (its allow-list, §6 step 4).

**Files:**
- Create (scratch, not committed): `SCRATCH/rewrite.py`, the folder `SCRATCH/p7stage/`
- Create (temporary, never committed): `Assets/Editor/_TimeDeskP7Baseline.cs`
- Output: `SCRATCH/p7_baseline_cases.txt`, `SCRATCH/p7_baseline_scene.txt`

- [ ] **Step 1: Confirm the starting point**

Run: `cd /e/unity/NOPE-feat-clock && git branch --show-current && git log --oneline -4 && git status --short`
Expected: `feat/physical-desk`; the four newest commits are this plan's own commit `docs(plan): the physical desk and basic traveller wheel implementation plan` (it adds only this file), `25a2c50 docs(spec): the physical desk and basic traveller wheel (piece 7) design`, `6477d8f docs(plan): code audit and overhaul — approach, success criteria, guarantees` and `6dfd14d docs: park reviewed drafts for pieces 4-6 and the ChatGPT art brief v2`. Untracked only `NOPE-feat-clock.sln` and `TimeDesk.Visuals.csproj` (if this plan shows as untracked, it was not committed: stop and commit it alone first, as the Conventions say). Run the compile check and the test run: `passed 337, failed 0`.

- [ ] **Step 2: Create the whole-file helper and the staging folder** (`SCRATCH/rewrite.py`, Write tool; skip if it already exists with this content)

```python
"""EOL-preserving whole-file rewrite: rewrite(path, old_sha1, text) replaces a file's
text after checking that its current text, normalised to LF, hashes to old_sha1
(the first 12 hex digits of its SHA-1), so a file that changed since the plan was
written fails loudly instead of being overwritten. The file keeps its CRLF or LF
line endings; `text` is written with the file's own ending."""
import hashlib, pathlib, sys

def sha(path):
    """The first 12 hex digits of the SHA-1 of a file's LF-normalised text."""
    t = pathlib.Path(path).read_bytes().decode('utf-8').replace('\r\n', '\n')
    return hashlib.sha1(t.encode('utf-8')).hexdigest()[:12]

def rewrite(path, old_sha1, text):
    p = pathlib.Path(path); b = p.read_bytes(); crlf = b'\r\n' in b
    got = sha(p)
    if got != old_sha1:
        sys.exit(f"{p}: expected text sha1 {old_sha1}, found {got}: the file changed since the plan was written; re-read it and adapt")
    text = text.replace('\r\n', '\n')
    if crlf:
        text = text.replace('\n', '\r\n')
    p.write_bytes(text.encode('utf-8'))
    print('rewrote', p, 'crlf' if crlf else 'lf')

if __name__ == '__main__':
    for arg in sys.argv[1:]:
        print(sha(arg), arg)
```

Then: `mkdir -p "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/p7stage"`. Check the helper against the six files later tasks replace whole: `cd /e/unity/NOPE-feat-clock && python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/rewrite.py" Assets/Scripts/UI/DraggableWindow.cs Assets/Scripts/UI/InteractionPanelController.cs Assets/Scripts/Office/ICameraRig.cs Assets/Scripts/Office/CinemachineCameraRig.cs Assets/Scripts/Office/OfficeViewController.cs Assets/Scripts/UI/DesktopShell.cs`
Expected, one line each: `f7aed008a90c …DraggableWindow.cs`, `7b2bdfc6cd07 …InteractionPanelController.cs`, `716d826a3b01 …ICameraRig.cs`, `310233d2e634 …CinemachineCameraRig.cs`, `60d3dcfebddc …OfficeViewController.cs`, `0015ce3662f6 …DesktopShell.cs`.

- [ ] **Step 3: Check no Unity editor is open on the worktree** (PowerShell)

Run: `Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Select-Object ProcessId, CommandLine`
Expected: no process whose command line contains `NOPE-feat-clock` (one on `E:\unity\NOPE` is Codex's; leave it alone).

- [ ] **Step 4: Write the baseline dumper** (`Assets/Editor/_TimeDeskP7Baseline.cs`, Write tool)

```csharp
// TEMPORARY (piece 7 baseline; never committed): before any piece-7 code, dumps
// case generation (who each traveller is, their lie with channels, every paper
// value, every answer) through GameManager's own calls, the base scene's
// interview reachability (GameManager's "spoken"), and the null references of
// the scene components piece 7 changes, for the verification task (spec
// section 6 step 0).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class TimeDeskP7Baseline
{
    private const string Scratch = @"C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad";
    private const string LibraryPath = "Assets/Data/Content Library/ContentLibrary_Main.asset";

    /// <summary>The scene components piece 7 changes; their null references are allowed to stay null after the rebuild.</summary>
    private static readonly Type[] Changed =
    {
        typeof(InvestigationUIController), typeof(InteractionPanelController), typeof(DesktopShell),
        typeof(CinemachineCameraRig), typeof(OfficeViewController), typeof(GameManager)
    };

    public static void Run()
    {
        try
        {
            var lib = AssetDatabase.LoadAssetAtPath<ContentLibrarySO>(LibraryPath);
            File.WriteAllLines(Path.Combine(Scratch, "p7_baseline_cases.txt"), CaseLines(lib));

            EditorSceneManager.OpenScene("Assets/Scenes/OfficeScene.unity", OpenSceneMode.Single);
            InvestigationUIController invest = Object.FindObjectsByType<InvestigationUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
            var scene = new List<string> { $"spoken={invest.InterviewReachable}" };
            scene.AddRange(NullReferences());
            File.WriteAllLines(Path.Combine(Scratch, "p7_baseline_scene.txt"), scene);
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            File.WriteAllText(Path.Combine(Scratch, "p7_baseline.error"), e.ToString());
            EditorApplication.Exit(2);
        }
    }

    /// <summary>
    /// Seeds 12345 and 999 x days 1-3, generated exactly as GameManager.Start
    /// does with the interview reachable: the day-start interview
    /// (TimelineService.BuildInterviewDay) gives the askable and spoken-tell
    /// categories. One line per slot; the verification task prints exactly this.
    /// </summary>
    private static List<string> CaseLines(ContentLibrarySO lib)
    {
        var lines = new List<string>();
        foreach (int seed in new[] { 12345, 999 })
        {
            for (int day = 1; day <= 3; day++)
            {
                DayPlanSO plan = lib.GetDayPlan(day);
                var world = new WorldState { day = day };
                InterviewDay interview = TimelineService.BuildInterviewDay(lib, world, new ShiftLedger());
                var factory = new CaseFactory(lib, lib.BuildFactTable(plan));
                List<CaseInstance> cases = factory.GenerateDayCases(plan, world, Seeds.Day(seed, day), interview.AskableCategories, interview.AnswerTellCategories);
                var violators = (Dictionary<int, NationEraProfileSO>)typeof(CaseFactory)
                    .GetField("_violators", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(factory);
                for (int i = 0; i < cases.Count; i++)
                    lines.Add(Line(seed, day, i + 1, cases[i], violators.ContainsKey(i + 1)));
            }
        }
        return lines;
    }

    private static string Line(int seed, int day, int slot, CaseInstance c, bool violator)
    {
        List<DocumentField> fields = c.documents.SelectMany(d => d.fields).ToList();
        IEnumerable<string> tells = fields.Where(f => f.isAnachronism).Select(f => $"{f.category}/Papers").Distinct()
            .Concat(c.answers.Where(a => a.isTell).Select(a => $"{a.category}/Answer"));
        string papers = string.Join(";", c.documents.Select(d => $"{(d.template != null ? d.template.displayName : "?")}:" +
                                                                string.Join(",", d.fields.Select(f => $"{f.label}={f.value}"))));
        string answers = string.Join(";", c.answers.Select(a => $"{a.category}={a.value}{(a.isTell ? "[TELL]" : "")}"));
        return $"seed={seed} day={day} slot={slot} claim='{c.originLabel}' name='{c.visitorGivenName}' born='{c.trueBirthDate}' role='{(c.archetype != null ? c.archetype.displayName : "")}' allowed={c.claimAllowedByRules} violator={violator}" +
               $" | liar={c.IsLiar} home='{c.HomeLabel}' tells=[{string.Join(",", tells)}] | papers={papers} | answers={answers}";
    }

    /// <summary>"Type.propertyPath" for every null object reference of the changed components in the open scene.</summary>
    private static IEnumerable<string> NullReferences()
    {
        foreach (Type type in Changed)
        {
            foreach (Object component in Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SerializedProperty p = new SerializedObject(component).GetIterator();
                bool enter = true;
                while (p.NextVisible(enter))
                {
                    enter = p.propertyType == SerializedPropertyType.Generic;
                    if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == null && p.name != "m_Script")
                        yield return $"null {type.Name}.{p.propertyPath}";
                }
            }
        }
    }
}
```

- [ ] **Step 5: Run it in the worktree's own Unity** (PowerShell, timeout 600000 ms)

```powershell
Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe' -ArgumentList @('-projectPath','E:\unity\NOPE-feat-clock','-executeMethod','TimeDeskP7Baseline.Run','-logFile','C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\unity_p7_baseline.log') -PassThru -Wait
```
Expected: exit code 0 and no `SCRATCH/p7_baseline.error`. Check with `wc -l "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/p7_baseline_cases.txt" && head -3 "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/p7_baseline_scene.txt"`: 60 case lines (2 seeds × (8 + 10 + 12) slots), each like `seed=12345 day=1 slot=1 claim='…' name='…' born='…' role='…' allowed=True violator=False | liar=False home='…' tells=[] | papers=Travel Passport:Full Name=…,… | answers=Currency=…;…`, day-1 lines with only `/Papers` tells; and the scene file starting `spoken=True`, followed by one `null Type.property` line for each object reference of the six changed components that is already empty in the base scene (possibly none).

- [ ] **Step 6: Clean up**

```bash
cd /e/unity/NOPE-feat-clock && rm -f Assets/Editor/_TimeDeskP7Baseline.cs Assets/Editor/_TimeDeskP7Baseline.cs.meta && git checkout -- Assembly-CSharp.csproj Assembly-CSharp-Editor.csproj TimeDesk.Domain.csproj TimeDeskEditMode.csproj NOPE.sln ProjectSettings "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset" Assets/Scenes/OfficeScene.unity && git status --short
```
Expected: only `?? NOPE-feat-clock.sln` and `?? TimeDesk.Visuals.csproj`. (If `git checkout` reports a path that is not modified or not tracked, that is fine.) No commit in this task.

---

### Task 1: The booth's input table (`BoothRules`)

Spec §1.9, §2.2, V5, R31: which of the desktop, the CRT, the power button, the focus exit and glass zones, the props, the papers, the wheel and the traveller take input, from the view, its settling, the screen's power, the shift's phase, the wheel and a pending citation slip. Nothing calls it yet; `BoothCoordinator` applies it (Task 13).

**Files:**
- Create: `Assets/Scripts/Domain/BoothRules.cs` (+ `.meta`)
- Test: `Assets/Tests/EditMode/BoothRulesTests.cs` (+ `.meta`)

- [ ] **Step 1: Write the failing test** (`Assets/Tests/EditMode/BoothRulesTests.cs`, Write tool)

```csharp
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The booth's input table (spec section 1.9): one test per output, each over
/// every row; the wheel, blend and citation details; the default context.
/// Expected outputs are written in this order: Desktop, CRT, Power, eXit,
/// pRops, pApers, Wheel allowed, Traveller live ('1' = true).
/// </summary>
public class BoothRulesTests
{
    private sealed class Row
    {
        public string Name;
        public BoothContext Context;
        public string Expected;
    }

    private static Row R(string name, bool focused, bool settled, bool screenOn, BoothPhase phase, bool wheelOpen, string expected) =>
        new Row { Name = name, Context = new BoothContext(focused, settled, screenOn, phase, wheelOpen, false), Expected = expected };

    /// <summary>The rows of spec section 1.9 (the citation row is tested separately).</summary>
    private static readonly Row[] Rows =
    {
        //                                         focused settled screen phase                      wheel   DCPXRAWT
        R("newsletter, booth view",                false,  true,   true,  BoothPhase.Newsletter,      false, "00000000"),
        R("newsletter, monitor view",              true,   true,   true,  BoothPhase.Newsletter,      false, "00010000"),
        R("booth, settled, no traveller",          false,  true,   true,  BoothPhase.NoTraveller,     false, "01101000"),
        R("booth, settled, traveller at the desk", false,  true,   true,  BoothPhase.TravellerAtDesk, false, "01101111"),
        R("wheel open",                            false,  true,   true,  BoothPhase.TravellerAtDesk, true,  "00000010"),
        R("blending to the booth",                 false,  false,  true,  BoothPhase.TravellerAtDesk, false, "01000000"),
        R("blending to the monitor",               true,   false,  true,  BoothPhase.TravellerAtDesk, false, "00000000"),
        R("focused, settled, screen on",           true,   true,   true,  BoothPhase.TravellerAtDesk, false, "10110000"),
        R("focused, settled, screen off",          true,   true,   false, BoothPhase.TravellerAtDesk, false, "00110000"),
    };

    /// <summary>The outputs in the order of the expected strings.</summary>
    private static bool[] Outputs(BoothInput o) => new[]
    {
        o.DesktopInteractive, o.CrtFocusable, o.PowerButtonLive, o.FocusExitLive,
        o.PropsLive, o.PapersLive, o.WheelAllowed, o.TravellerLive
    };

    private static void CheckColumn(int column, string output)
    {
        var wrong = new List<string>();
        foreach (Row row in Rows)
        {
            bool expected = row.Expected[column] == '1';
            if (Outputs(BoothRules.Evaluate(row.Context))[column] != expected)
                wrong.Add($"{row.Name}: expected {expected}");
        }

        Assert.IsEmpty(wrong, $"{output}: {string.Join("; ", wrong)}");
    }

    [Test]
    public void DesktopInteractive_OnlyFocusedSettledAndLit_NeverUnderANewsletter() => CheckColumn(0, "DesktopInteractive");

    [Test]
    public void CrtFocusable_InTheBoothWithoutANewsletterOrAnOpenWheel() => CheckColumn(1, "CrtFocusable");

    [Test]
    public void PowerButtonLive_WhenSettled_WithoutANewsletterOrAnOpenWheel() => CheckColumn(2, "PowerButtonLive");

    [Test]
    public void FocusExitLive_OnlyFocusedAndSettled() => CheckColumn(3, "FocusExitLive");

    [Test]
    public void PropsLive_InTheSettledBooth_WithoutANewsletterOrAnOpenWheel() => CheckColumn(4, "PropsLive");

    [Test]
    public void PapersLive_LikeProps_WhileATravellerIsAtTheDesk() => CheckColumn(5, "PapersLive");

    [Test]
    public void WheelAllowed_InTheSettledBooth_WhileATravellerIsAtTheDesk() => CheckColumn(6, "WheelAllowed");

    [Test]
    public void TravellerLive_WhenTheWheelIsAllowedButClosed() => CheckColumn(7, "TravellerLive");

    [Test]
    public void AnOpenWheel_StaysAllowed_SoOpeningItNeverClosesIt_ButTheTravellerGoesInert()
    {
        BoothInput open = BoothRules.Evaluate(new BoothContext(false, true, true, BoothPhase.TravellerAtDesk, true, false));
        Assert.IsTrue(open.WheelAllowed);
        Assert.IsFalse(open.TravellerLive);
    }

    [Test]
    public void TheCrt_CanBeClickedWhileTheCameraBlendsBackToTheBooth_NotWhileItPushesIn()
    {
        Assert.IsTrue(BoothRules.Evaluate(new BoothContext(false, false, true, BoothPhase.NoTraveller, false, false)).CrtFocusable);
        Assert.IsFalse(BoothRules.Evaluate(new BoothContext(true, false, true, BoothPhase.NoTraveller, false, false)).CrtFocusable);
    }

    [Test]
    public void APendingCitation_OnlyMakesThePowerButtonInert_InEveryRow()
    {
        foreach (Row row in Rows)
        {
            BoothContext c = row.Context;
            BoothInput plain = BoothRules.Evaluate(c);
            BoothInput held = BoothRules.Evaluate(new BoothContext(c.Focused, c.Settled, c.ScreenOn, c.Phase, c.WheelOpen, true));

            Assert.IsFalse(held.PowerButtonLive, row.Name);
            bool[] a = Outputs(plain), b = Outputs(held);
            for (int i = 0; i < a.Length; i++)
                if (i != 2)
                    Assert.AreEqual(a[i], b[i], $"{row.Name}: output {i} must not depend on the citation");
        }
    }

    [Test]
    public void TheDefaultContext_OnlyLetsTheCrtBeClicked()
    {
        BoothContext c = default;
        Assert.AreEqual(BoothPhase.NoTraveller, c.Phase, "NoTraveller is the first phase, the default before Start");
        CollectionAssert.AreEqual(new[] { false, true, false, false, false, false, false, false }, Outputs(BoothRules.Evaluate(c)));
    }
}
```

- [ ] **Step 2: Run the compile check — expect failure**

Expected: `== Assembly-CSharp-Editor.csproj: exit 0` and `== TimeDeskEditMode.csproj: exit 1` with exactly three distinct errors: `error CS0246: The type or namespace name 'BoothContext' could not be found`, the same for `'BoothInput'` and `'BoothPhase'` (the compiler stops at the unresolved types).

- [ ] **Step 3: Implement** — write `Assets/Scripts/Domain/BoothRules.cs` (Write tool):

```csharp
/// <summary>Where the shift is, as the booth's input rules need it. Set by GameManager.</summary>
public enum BoothPhase
{
    /// <summary>No traveller at the desk: before the shift, behind READY or between travellers (the default before Start).</summary>
    NoTraveller,

    /// <summary>A traveller is at the desk, from presentation until the decision.</summary>
    TravellerAtDesk,

    /// <summary>The morning briefing or the shift report is up.</summary>
    Newsletter
}

/// <summary>What the booth's input rules read.</summary>
public readonly struct BoothContext
{
    /// <summary>The view is the monitor (MonitorFocus).</summary>
    public readonly bool Focused;

    /// <summary>The camera rig shows that view and no blend is running.</summary>
    public readonly bool Settled;

    /// <summary>The PC's screen is on.</summary>
    public readonly bool ScreenOn;

    /// <summary>Where the shift is.</summary>
    public readonly BoothPhase Phase;

    /// <summary>The traveller wheel is open.</summary>
    public readonly bool WheelOpen;

    /// <summary>A citation slip waits for Acknowledge (the traveller has left; the slip holds the clock and the screen).</summary>
    public readonly bool CitationPending;

    /// <summary>Creates a context.</summary>
    public BoothContext(bool focused, bool settled, bool screenOn, BoothPhase phase, bool wheelOpen, bool citationPending)
    {
        Focused = focused;
        Settled = settled;
        ScreenOn = screenOn;
        Phase = phase;
        WheelOpen = wheelOpen;
        CitationPending = citationPending;
    }
}

/// <summary>Which of the booth's inputs are live (BoothRules.Evaluate).</summary>
public readonly struct BoothInput
{
    /// <summary>The desktop canvas takes clicks: focused, settled, the screen on, no newsletter.</summary>
    public readonly bool DesktopInteractive;

    /// <summary>Clicking the CRT focuses it: in the booth (settled or blending back), no newsletter, the wheel closed.</summary>
    public readonly bool CrtFocusable;

    /// <summary>The bezel power button toggles the screen: settled in either view, no newsletter, the wheel closed, no pending citation slip.</summary>
    public readonly bool PowerButtonLive;

    /// <summary>The focus exit zone and the glass zone are up: focused and settled.</summary>
    public readonly bool FocusExitLive;

    /// <summary>The desk props react to clicks: the settled booth, no newsletter, the wheel closed.</summary>
    public readonly bool PropsLive;

    /// <summary>The papers can be dragged and clicked: as the props, while a traveller is at the desk.</summary>
    public readonly bool PapersLive;

    /// <summary>The wheel may be open: the settled booth while a traveller is at the desk (false closes an open wheel).</summary>
    public readonly bool WheelAllowed;

    /// <summary>The traveller hit zone opens the wheel: the wheel is allowed and closed.</summary>
    public readonly bool TravellerLive;

    /// <summary>Creates an output set.</summary>
    public BoothInput(bool desktopInteractive, bool crtFocusable, bool powerButtonLive, bool focusExitLive,
                      bool propsLive, bool papersLive, bool wheelAllowed, bool travellerLive)
    {
        DesktopInteractive = desktopInteractive;
        CrtFocusable = crtFocusable;
        PowerButtonLive = powerButtonLive;
        FocusExitLive = focusExitLive;
        PropsLive = propsLive;
        PapersLive = papersLive;
        WheelAllowed = wheelAllowed;
        TravellerLive = travellerLive;
    }
}

/// <summary>
/// The booth's input table (the physical-desk spec, section 1.9): from the
/// view, its settling, the screen's power, the shift's phase, the wheel and a
/// pending citation slip, which of the desktop, the CRT, the bezel power
/// button, the focus exit, the props, the papers, the wheel and the traveller
/// take input. Pure, so every row is tested headless; BoothCoordinator applies it.
/// </summary>
public static class BoothRules
{
    /// <summary>Evaluates the table for one context.</summary>
    public static BoothInput Evaluate(BoothContext c)
    {
        bool newsletter = c.Phase == BoothPhase.Newsletter;
        bool props = !c.Focused && c.Settled && !newsletter && !c.WheelOpen;
        bool wheel = !c.Focused && c.Settled && c.Phase == BoothPhase.TravellerAtDesk;

        return new BoothInput(
            desktopInteractive: c.Focused && c.Settled && c.ScreenOn && !newsletter,
            crtFocusable: !c.Focused && !newsletter && !c.WheelOpen,
            powerButtonLive: c.Settled && !newsletter && !c.WheelOpen && !c.CitationPending,
            focusExitLive: c.Focused && c.Settled,
            propsLive: props,
            papersLive: props && c.Phase == BoothPhase.TravellerAtDesk,
            wheelAllowed: wheel,
            travellerLive: wheel && !c.WheelOpen);
    }
}
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 349, failed 0` (337 + 12).

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/make_meta.py" Assets/Scripts/Domain/BoothRules.cs Assets/Tests/EditMode/BoothRulesTests.cs && git add Assets/Scripts/Domain/BoothRules.cs Assets/Scripts/Domain/BoothRules.cs.meta Assets/Tests/EditMode/BoothRulesTests.cs Assets/Tests/EditMode/BoothRulesTests.cs.meta && git commit -F - <<'EOF'
feat(domain): the booth's input table (BoothRules)

One decision table for who takes input in the booth: the desktop only when
focused, settled and lit; the CRT, the props, the papers and the traveller
in the settled booth; nothing under a newsletter; only the wheel while it
is open; the power button inert while a citation slip waits.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 2: Screen power (`PcScreen`)

Spec §1.3, §2.3, K7, R31: toggling and turning off (refused while a citation slip holds the screen), waking on a presented traveller or a finished scan (each a knob), and the hold. `MonitorScreen` owns one (Task 11); `PcWakeRules` is held by `DeskConfigSO`.

**Files:**
- Create: `Assets/Scripts/Domain/PcScreen.cs` (+ `.meta`)
- Test: `Assets/Tests/EditMode/PcScreenTests.cs` (+ `.meta`)

- [ ] **Step 1: Write the failing test** (`Assets/Tests/EditMode/PcScreenTests.cs`, Write tool)

```csharp
using NUnit.Framework;

/// <summary>Screen power: the start state, toggling, turning off, the wake rules and the citation hold.</summary>
public class PcScreenTests
{
    private static PcWakeRules Rules(bool onTraveller = true, bool onScan = true) =>
        new PcWakeRules { onTravellerPresented = onTraveller, onScanFinished = onScan };

    private static (PcScreen screen, int[] changes) Watch(bool startsOn, PcWakeRules rules)
    {
        var screen = new PcScreen(startsOn, rules);
        var changes = new int[1];
        screen.Changed += () => changes[0]++;
        return (screen, changes);
    }

    [Test]
    public void TheStartState_FollowsStartsOn()
    {
        Assert.IsTrue(new PcScreen(true, Rules()).IsOn);
        Assert.IsFalse(new PcScreen(false, Rules()).IsOn);
    }

    [Test]
    public void TheWakeRules_WakeOnBothByDefault()
    {
        var rules = new PcWakeRules();
        Assert.IsTrue(rules.onTravellerPresented);
        Assert.IsTrue(rules.onScanFinished);
    }

    [Test]
    public void ToggleTwice_ReturnsToTheStart_TrueEachTime_RaisingChangedTwice()
    {
        (PcScreen screen, int[] changes) = Watch(true, Rules());
        Assert.IsTrue(screen.Toggle());
        Assert.IsFalse(screen.IsOn);
        Assert.IsTrue(screen.Toggle());
        Assert.IsTrue(screen.IsOn);
        Assert.AreEqual(2, changes[0]);
    }

    [Test]
    public void TurnOff_TurnsALitScreenOff_AndADarkOneNot()
    {
        (PcScreen screen, int[] changes) = Watch(true, Rules());
        Assert.IsTrue(screen.TurnOff());
        Assert.IsFalse(screen.IsOn);
        Assert.AreEqual(1, changes[0]);

        Assert.IsFalse(screen.TurnOff(), "already off");
        Assert.AreEqual(1, changes[0], "nothing changed, nothing raised");
    }

    [TestCase(WakeReason.TravellerPresented, true, false, true)]
    [TestCase(WakeReason.TravellerPresented, false, false, false)]
    [TestCase(WakeReason.TravellerPresented, true, true, false)]
    [TestCase(WakeReason.TravellerPresented, false, true, false)]
    [TestCase(WakeReason.ScanFinished, true, false, true)]
    [TestCase(WakeReason.ScanFinished, false, false, false)]
    [TestCase(WakeReason.ScanFinished, true, true, false)]
    [TestCase(WakeReason.ScanFinished, false, true, false)]
    public void Wake_TurnsOnOnlyADarkScreen_WhenTheReasonsOwnRuleIsOn(WakeReason reason, bool ruleOn, bool startsOn, bool wakes)
    {
        // The other reason's rule is always the opposite, so only the reason's own rule can decide.
        bool traveller = reason == WakeReason.TravellerPresented ? ruleOn : !ruleOn;
        (PcScreen screen, int[] changes) = Watch(startsOn, Rules(traveller, !traveller));
        Assert.AreEqual(wakes, screen.Wake(reason));
        Assert.AreEqual(startsOn || wakes, screen.IsOn);
        Assert.AreEqual(wakes ? 1 : 0, changes[0]);
    }

    [Test]
    public void NullRules_NeverWake()
    {
        (PcScreen screen, int[] changes) = Watch(false, null);
        Assert.IsFalse(screen.Wake(WakeReason.TravellerPresented));
        Assert.IsFalse(screen.Wake(WakeReason.ScanFinished));
        Assert.IsFalse(screen.IsOn);
        Assert.AreEqual(0, changes[0]);
    }

    [Test]
    public void AnUndefinedReason_NeverWakes()
    {
        (PcScreen screen, int[] changes) = Watch(false, Rules());
        Assert.IsFalse(screen.Wake((WakeReason)99));
        Assert.IsFalse(screen.IsOn);
        Assert.AreEqual(0, changes[0]);
    }

    [Test]
    public void Holding_TurnsADarkScreenOn_RaisingChangedOnce()
    {
        (PcScreen screen, int[] changes) = Watch(false, null);
        screen.SetHeld(true);
        Assert.IsTrue(screen.IsOn);
        Assert.AreEqual(1, changes[0]);
    }

    [Test]
    public void Holding_ALitScreen_RaisesNothing()
    {
        (PcScreen screen, int[] changes) = Watch(true, Rules());
        screen.SetHeld(true);
        Assert.IsTrue(screen.IsOn);
        Assert.AreEqual(0, changes[0]);
    }

    [Test]
    public void AHeldScreen_RefusesToggleAndTurnOff_StayingOnAndRaisingNothing()
    {
        (PcScreen screen, int[] changes) = Watch(true, Rules());
        screen.SetHeld(true);
        Assert.IsFalse(screen.Toggle());
        Assert.IsFalse(screen.TurnOff());
        Assert.IsTrue(screen.IsOn);
        Assert.AreEqual(0, changes[0]);
    }

    [Test]
    public void Releasing_ChangesNothing_ThenToggleAndTurnOffWorkAgain()
    {
        (PcScreen screen, int[] changes) = Watch(false, null);
        screen.SetHeld(true);
        screen.SetHeld(false);
        Assert.IsTrue(screen.IsOn, "releasing leaves the screen on");
        Assert.AreEqual(1, changes[0], "only the hold's own wake was raised");

        Assert.IsTrue(screen.TurnOff());
        Assert.IsFalse(screen.IsOn);
        Assert.IsTrue(screen.Toggle());
        Assert.IsTrue(screen.IsOn);
        Assert.AreEqual(3, changes[0]);
    }
}
```

- [ ] **Step 2: Run the compile check — expect failure**

Expected: `== Assembly-CSharp-Editor.csproj: exit 0` and `== TimeDeskEditMode.csproj: exit 1` with `error CS0246: The type or namespace name 'PcWakeRules' could not be found`, the same for `'PcScreen'` and `'WakeReason'`, and `error CS0103: The name 'WakeReason' does not exist in the current context`.

- [ ] **Step 3: Implement** — write `Assets/Scripts/Domain/PcScreen.cs` (Write tool):

```csharp
using System;

/// <summary>Why a dark screen may wake itself.</summary>
public enum WakeReason
{
    /// <summary>A traveller was presented at the desk.</summary>
    TravellerPresented,

    /// <summary>A desk scan finished (its scanned window has just opened on the PC).</summary>
    ScanFinished
}

/// <summary>Which events turn a dark screen on (a knob group held by DeskConfigSO).</summary>
[Serializable]
public sealed class PcWakeRules
{
    /// <summary>A newly presented traveller turns a dark screen on.</summary>
    public bool onTravellerPresented = true;

    /// <summary>A finished desk scan turns a dark screen on.</summary>
    public bool onScanFinished = true;
}

/// <summary>
/// The PC's screen power: only the display and the desktop's input go dark,
/// the PC keeps running. The screen can be toggled (the bezel button) or
/// turned off (Start > Turn off screen), wakes itself on the enabled reasons,
/// and a held screen is always on (a pending citation slip): holding lights
/// it, and while held nothing turns it off. Pure, so every rule is tested headless.
/// </summary>
public sealed class PcScreen
{
    /// <summary>The wake rules (null wakes on nothing).</summary>
    private readonly PcWakeRules _rules;

    /// <summary>True while a citation slip holds the screen on.</summary>
    private bool _held;

    /// <summary>Creates a screen that starts on or off; a null <paramref name="rules"/> never wakes.</summary>
    public PcScreen(bool startsOn, PcWakeRules rules)
    {
        IsOn = startsOn;
        _rules = rules;
    }

    /// <summary>True while the screen is on.</summary>
    public bool IsOn { get; private set; }

    /// <summary>Raised after the screen turns on or off (only on a real change).</summary>
    public event Action Changed;

    /// <summary>Flips the screen and returns true; while held it changes nothing and returns false.</summary>
    public bool Toggle()
    {
        if (_held)
            return false;

        Set(!IsOn);
        return true;
    }

    /// <summary>Turns the screen off; returns true when it did (false when it was off, or held).</summary>
    public bool TurnOff()
    {
        if (_held || !IsOn)
            return false;

        Set(false);
        return true;
    }

    /// <summary>Turns a dark screen on when the reason's rule is on; returns true when it did. An unknown reason never wakes.</summary>
    public bool Wake(WakeReason reason)
    {
        if (IsOn || !RuleOn(reason))
            return false;

        Set(true);
        return true;
    }

    /// <summary>A held screen is always on (a pending citation slip): holding turns a dark screen on; while held, Toggle and TurnOff refuse; releasing changes nothing else.</summary>
    public void SetHeld(bool held)
    {
        _held = held;
        if (held && !IsOn)
            Set(true);
    }

    /// <summary>True when the wake rules let this reason wake the screen.</summary>
    private bool RuleOn(WakeReason reason)
    {
        if (_rules == null)
            return false;

        switch (reason)
        {
            case WakeReason.TravellerPresented: return _rules.onTravellerPresented;
            case WakeReason.ScanFinished: return _rules.onScanFinished;
            default: return false;
        }
    }

    /// <summary>Changes the state and raises Changed (callers only pass a real change).</summary>
    private void Set(bool on)
    {
        IsOn = on;
        Changed?.Invoke();
    }
}
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 367, failed 0` (349 + 18: 10 tests and 8 wake cases).

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/make_meta.py" Assets/Scripts/Domain/PcScreen.cs Assets/Tests/EditMode/PcScreenTests.cs && git add Assets/Scripts/Domain/PcScreen.cs Assets/Scripts/Domain/PcScreen.cs.meta Assets/Tests/EditMode/PcScreenTests.cs Assets/Tests/EditMode/PcScreenTests.cs.meta && git commit -F - <<'EOF'
feat(domain): screen power with wake rules and the citation hold (PcScreen)

The PC's screen can be toggled or turned off, wakes itself on a presented
traveller or a finished scan (each a knob), and a held screen (a pending
citation slip) is always on and refuses both.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 3: One case's papers and the day-1 notes (`DeskPapers`, `DeskHints`)

Spec §2.4, K5, K6, R36, R16, R32: the hand-over knob's enum (`DocumentHandOver`, `OnRequest` = 0 so existing templates read as on request), a case's documents (`CaseDocument`; `DocumentHandOvers.IsRequested` is the one home of "waits for a request", behind `CaseDocument.Requested` and, in Task 9, the validator; `CaseDocuments.ArrivalIndices` is the one home of "handed over on arrival", used by `DeskPapers` and, in Task 8, the no-desk path), the papers' states behind `HandOver`, `CanDrag`, `Drop` (the drop outcome is decided here, R36), `Tick` and `ReturnAll`, and when the two day-1 notes show.

**Files:**
- Create: `Assets/Scripts/Domain/DeskPapers.cs` (+ `.meta`)
- Test: `Assets/Tests/EditMode/DeskPapersTests.cs` (+ `.meta`)

- [ ] **Step 1: Write the failing test** (`Assets/Tests/EditMode/DeskPapersTests.cs`, Write tool)

```csharp
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// One case's papers on the desk and the day-1 desk notes. A paper's state is
/// private, so each test observes it through CanDrag, ScannerBusy,
/// OnDeskCount, HandOver, Drop and Tick. Case under test: a passport handed
/// over on arrival, a permit on request and a letter on arrival, scans of 1.5 s.
/// </summary>
public class DeskPapersTests
{
    private const float Scan = 1.5f;

    private static CaseDocument Doc(string name, DocumentHandOver handOver) =>
        new CaseDocument { name = name, holder = "Nefertari", handOver = handOver };

    private static DeskPapers Papers(float scanSeconds = Scan) => new DeskPapers(new[]
    {
        Doc("Travel Passport", DocumentHandOver.OnArrival),
        Doc("Transit Permit", DocumentHandOver.OnRequest),
        Doc("Letter", DocumentHandOver.OnArrival)
    }, scanSeconds);

    /// <summary>Papers with every paper on the desk.</summary>
    private static DeskPapers AllOnDesk()
    {
        DeskPapers p = Papers();
        for (int i = 0; i < p.Count; i++)
            Assert.IsTrue(p.HandOver(i));
        return p;
    }

    /// <summary>The four states a paper can be in, reached through the public members (public: test-case arguments).</summary>
    public enum State { WithTraveller, OnDesk, Scanning, Returned }

    /// <summary>Papers whose paper 0 is in <paramref name="state"/> and whose scanner is busy or idle (busy with paper 1 unless paper 0 scans).</summary>
    private static DeskPapers InState(State state, bool busy)
    {
        DeskPapers p = Papers();
        if (state != State.WithTraveller)
            Assert.IsTrue(p.HandOver(0));
        Assert.IsTrue(p.HandOver(1));
        if (state == State.Scanning)
            Assert.AreEqual(DropOutcome.Scanning, p.Drop(0, true));
        if (busy && state != State.Scanning)
            Assert.AreEqual(DropOutcome.Scanning, p.Drop(1, true));
        if (state == State.Returned)
            p.ReturnAll();
        return p;
    }

    [Test]
    public void EveryPaper_StartsWithTheTraveller()
    {
        DeskPapers p = Papers();
        Assert.AreEqual(3, p.Count);
        Assert.AreEqual(0, p.OnDeskCount);
        Assert.IsFalse(p.ScannerBusy);
        for (int i = 0; i < p.Count; i++)
            Assert.IsFalse(p.CanDrag(i));
    }

    [Test]
    public void ArrivalIndices_AreTheOnArrivalPapers_InPaperOrder()
    {
        CollectionAssert.AreEqual(new[] { 0, 2 }, Papers().ArrivalIndices);
        var withANull = new[] { null, Doc("Travel Passport", DocumentHandOver.OnArrival), Doc("Transit Permit", DocumentHandOver.OnRequest) };
        CollectionAssert.AreEqual(new[] { 1 }, CaseDocuments.ArrivalIndices(withANull), "the rule both paths use: a null entry is skipped");
        CollectionAssert.IsEmpty(CaseDocuments.ArrivalIndices(null));
        Assert.IsTrue(DocumentHandOvers.IsRequested(DocumentHandOver.OnRequest));
        Assert.IsFalse(DocumentHandOvers.IsRequested(DocumentHandOver.OnArrival));
        Assert.IsTrue(Doc("x", DocumentHandOver.OnRequest).Requested);
        Assert.IsFalse(Doc("x", DocumentHandOver.OnArrival).Requested);
        Assert.AreEqual(0, (int)DocumentHandOver.OnRequest, "OnRequest is 0, so existing templates read as OnRequest");
        Assert.AreEqual(1, (int)DocumentHandOver.OnArrival);
    }

    [Test]
    public void HandOver_OnlyFromTheTraveller_Once()
    {
        DeskPapers p = Papers();
        Assert.IsTrue(p.HandOver(1));
        Assert.IsTrue(p.CanDrag(1));
        Assert.AreEqual(1, p.OnDeskCount);
        Assert.IsFalse(p.HandOver(1), "a second hand-over fails");

        Assert.AreEqual(DropOutcome.Scanning, p.Drop(1, true));
        Assert.IsFalse(p.HandOver(1), "not while scanning");

        p.ReturnAll();
        Assert.IsFalse(p.HandOver(0), "not after the papers went back");
        Assert.IsFalse(p.HandOver(1));
    }

    [Test]
    public void OutOfRangeIndices_GiveFalse_AndRefused()
    {
        DeskPapers p = AllOnDesk();
        foreach (int i in new[] { -1, 3, 99 })
        {
            Assert.IsFalse(p.HandOver(i));
            Assert.IsFalse(p.CanDrag(i));
            Assert.AreEqual(DropOutcome.Refused, p.Drop(i, false));
            Assert.AreEqual(DropOutcome.Refused, p.Drop(i, true));
        }
        Assert.AreEqual(3, p.OnDeskCount);
    }

    [Test]
    public void CanDrag_OnlyOnTheDesk()
    {
        Assert.IsFalse(InState(State.WithTraveller, false).CanDrag(0));
        Assert.IsTrue(InState(State.OnDesk, false).CanDrag(0));
        Assert.IsFalse(InState(State.Scanning, false).CanDrag(0));
        Assert.IsFalse(InState(State.Returned, false).CanDrag(0));
    }

    [TestCase(State.WithTraveller, false, false, DropOutcome.Refused)]
    [TestCase(State.WithTraveller, false, true, DropOutcome.Refused)]
    [TestCase(State.WithTraveller, true, false, DropOutcome.Refused)]
    [TestCase(State.WithTraveller, true, true, DropOutcome.Refused)]
    [TestCase(State.OnDesk, false, false, DropOutcome.Stays)]
    [TestCase(State.OnDesk, false, true, DropOutcome.Stays)]
    [TestCase(State.OnDesk, true, false, DropOutcome.Scanning)]
    [TestCase(State.OnDesk, true, true, DropOutcome.Refused)]
    [TestCase(State.Scanning, false, true, DropOutcome.Refused)]
    [TestCase(State.Scanning, true, true, DropOutcome.Refused)]
    [TestCase(State.Returned, false, false, DropOutcome.Refused)]
    [TestCase(State.Returned, true, false, DropOutcome.Refused)]
    public void Drop_DecisionTable(State state, bool overScanner, bool busy, DropOutcome expected)
    {
        DeskPapers p = InState(state, busy);
        bool couldDrag = p.CanDrag(0);
        bool wasBusy = p.ScannerBusy;
        int onDesk = p.OnDeskCount;

        Assert.AreEqual(expected, p.Drop(0, overScanner));

        if (expected == DropOutcome.Scanning)
        {
            Assert.IsTrue(p.ScannerBusy);
            Assert.IsFalse(p.CanDrag(0), "a scanning paper cannot be dragged");
            Assert.AreEqual(onDesk, p.OnDeskCount, "a scanning paper still counts as on the desk");
        }
        else
        {
            Assert.AreEqual(couldDrag, p.CanDrag(0), "nothing changes for the dropped paper");
            Assert.AreEqual(wasBusy, p.ScannerBusy, "nothing changes for the scanner");
            Assert.AreEqual(onDesk, p.OnDeskCount);
        }
    }

    [Test]
    public void ABusyScanner_KeepsScanningItsPaper_WhileTheRefusedOneStaysDraggable()
    {
        DeskPapers p = AllOnDesk();
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(0, true));
        Assert.AreEqual(DropOutcome.Refused, p.Drop(1, true));
        Assert.IsTrue(p.CanDrag(1));
        Assert.AreEqual(-1, p.Tick(1f));
        Assert.AreEqual(0, p.Tick(0.5f), "the first paper's scan still finishes");
    }

    [Test]
    public void AScannedPaper_CanBeScannedAgain()
    {
        DeskPapers p = AllOnDesk();
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(2, true));
        Assert.AreEqual(2, p.Tick(Scan));
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(2, true));
        Assert.AreEqual(2, p.Tick(Scan));
    }

    [Test]
    public void Tick_FinishesAtTheDuration_AndReturnsThePaper()
    {
        DeskPapers p = AllOnDesk();
        Assert.AreEqual(-1, p.Tick(1f), "no scan running");
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(1, true));

        Assert.AreEqual(-1, p.Tick(1f));
        Assert.AreEqual(-1, p.Tick(0f), "zero is ignored");
        Assert.AreEqual(-1, p.Tick(-5f), "negative is ignored");
        Assert.AreEqual(1, p.Tick(0.5f), "exactly at the duration");
        Assert.IsFalse(p.ScannerBusy);
        Assert.IsTrue(p.CanDrag(1), "draggable again once scanned");
        Assert.AreEqual(-1, p.Tick(10f), "no scan running any more");

        Assert.AreEqual(DropOutcome.Scanning, p.Drop(0, true));
        Assert.AreEqual(0, p.Tick(9f), "an overshoot finishes too");
    }

    [Test]
    public void ReturnAll_MidScan_CancelsTheScanForever()
    {
        DeskPapers p = AllOnDesk();
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(0, true));
        p.ReturnAll();

        Assert.IsFalse(p.ScannerBusy);
        Assert.AreEqual(0, p.OnDeskCount);
        for (int i = 0; i < p.Count; i++)
            Assert.IsFalse(p.CanDrag(i));
        Assert.AreEqual(-1, p.Tick(Scan));
        Assert.AreEqual(-1, p.Tick(100f));
    }

    [Test]
    public void OnDeskCount_CountsScanningPapers()
    {
        DeskPapers p = Papers();
        Assert.IsTrue(p.HandOver(0));
        Assert.IsTrue(p.HandOver(2));
        Assert.AreEqual(2, p.OnDeskCount);
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(0, true));
        Assert.AreEqual(2, p.OnDeskCount);
    }

    [Test]
    public void AScanTimeOfZero_IsRaisedToOneHundredthOfASecond()
    {
        DeskPapers p = Papers(0f);
        Assert.IsTrue(p.HandOver(0));
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(0, true));
        Assert.AreEqual(-1, p.Tick(0.005f), "half of the raised minimum");
        Assert.AreEqual(0, p.Tick(0.005f), "the raised minimum");
    }

    [Test]
    public void ANullDocumentList_MeansNoPapers()
    {
        var p = new DeskPapers(null, Scan);
        Assert.AreEqual(0, p.Count);
        CollectionAssert.IsEmpty(p.ArrivalIndices);
        Assert.IsFalse(p.HandOver(0));
        Assert.AreEqual(-1, p.Tick(1f));
    }

    // -----------------------------
    // DeskHints
    // -----------------------------

    private const string ScanHint = "Drag papers onto the scanner to read them on the PC.";
    private const string WheelHint = "Click the traveller to talk and ask for papers.";

    [Test]
    public void TheScanHint_ShowsOnDayOne_WhileAPaperIsOnTheDesk_UntilTheFirstScan()
    {
        Assert.IsTrue(DeskHints.ScanHintVisible(ScanHint, 1, 1, 0, true));
        Assert.IsFalse(DeskHints.ScanHintVisible(" ", 1, 1, 0, true), "a blank hint");
        Assert.IsFalse(DeskHints.ScanHintVisible(null, 1, 1, 0, true), "no hint");
        Assert.IsFalse(DeskHints.ScanHintVisible(ScanHint, 2, 1, 0, true), "past the last day");
        Assert.IsFalse(DeskHints.ScanHintVisible(ScanHint, 1, 1, 1, true), "a scan today");
        Assert.IsFalse(DeskHints.ScanHintVisible(ScanHint, 1, 1, 0, false), "no paper on the desk");
        Assert.IsFalse(DeskHints.ScanHintVisible(ScanHint, 1, 0, 0, true), "last day 0: never");
    }

    [Test]
    public void TheWheelHint_ShowsOnDayOne_WhileATravellerIsAtTheDesk_UntilTheWheelFirstOpens()
    {
        Assert.IsTrue(DeskHints.WheelHintVisible(WheelHint, 1, 1, false, true));
        Assert.IsFalse(DeskHints.WheelHintVisible("", 1, 1, false, true), "a blank hint");
        Assert.IsFalse(DeskHints.WheelHintVisible(WheelHint, 2, 1, false, true), "past the last day");
        Assert.IsFalse(DeskHints.WheelHintVisible(WheelHint, 1, 1, true, true), "the wheel was opened today");
        Assert.IsFalse(DeskHints.WheelHintVisible(WheelHint, 1, 1, false, false), "no traveller at the desk");
        Assert.IsFalse(DeskHints.WheelHintVisible(WheelHint, 1, 0, false, true), "last day 0: never");
    }

    [Test]
    public void TheCaseUnderTest_HandsOverTwoPapersOnArrival()
    {
        var arrived = new List<int>();
        DeskPapers p = Papers();
        foreach (int i in p.ArrivalIndices)
            if (p.HandOver(i))
                arrived.Add(i);
        CollectionAssert.AreEqual(new[] { 0, 2 }, arrived);
        Assert.AreEqual(2, p.OnDeskCount);
        Assert.IsFalse(p.CanDrag(1), "the permit waits for its request");
    }
}
```

- [ ] **Step 2: Run the compile check — expect failure**

Expected: `== Assembly-CSharp-Editor.csproj: exit 0` and `== TimeDeskEditMode.csproj: exit 1` with `error CS0246: The type or namespace name 'CaseDocument' could not be found`, the same for `'DeskPapers'`, `'DocumentHandOver'` and `'DropOutcome'`, and `error CS0103: The name 'DropOutcome' does not exist in the current context`.

- [ ] **Step 3: Implement** — write `Assets/Scripts/Domain/DeskPapers.cs` (Write tool):

```csharp
using System.Collections.Generic;

/// <summary>When the traveller hands a document over. Serialized on DocumentTemplateSO: append only.</summary>
public enum DocumentHandOver
{
    /// <summary>When the desk asks for it (a "Request" choice on the traveller wheel). 0, so existing templates read as OnRequest.</summary>
    OnRequest,

    /// <summary>When the traveller steps up to the desk.</summary>
    OnArrival
}

/// <summary>The one rule over DocumentHandOver.</summary>
public static class DocumentHandOvers
{
    /// <summary>True when a document with this hand-over waits for the desk to ask (it then gets a hub request and counts toward the hub's size).</summary>
    public static bool IsRequested(DocumentHandOver handOver) => handOver == DocumentHandOver.OnRequest;
}

/// <summary>One of the current traveller's documents, as the desk and the interview see it.</summary>
public sealed class CaseDocument
{
    /// <summary>The document's display name ("Travel Passport").</summary>
    public string name;

    /// <summary>The name the paper shows: the traveller's registered given name.</summary>
    public string holder;

    /// <summary>When the traveller hands it over.</summary>
    public DocumentHandOver handOver;

    /// <summary>True when the document is handed over only on request (it then gets a hub request).</summary>
    public bool Requested => DocumentHandOvers.IsRequested(handOver);
}

/// <summary>Rules over one traveller's documents.</summary>
public static class CaseDocuments
{
    /// <summary>The documents handed over on arrival, in paper order: the papers the desk receives at presentation, or the windows that open then where no desk is wired. Null entries are skipped; a null list gives none.</summary>
    public static IReadOnlyList<int> ArrivalIndices(IReadOnlyList<CaseDocument> documents)
    {
        var arrivals = new List<int>();
        int count = documents != null ? documents.Count : 0;
        for (int i = 0; i < count; i++)
            if (documents[i] != null && !documents[i].Requested)
                arrivals.Add(i);
        return arrivals;
    }
}

/// <summary>What happens to a paper released on the desk: it stays where it was dropped, it starts scanning, or it goes back to where it was picked up.</summary>
public enum DropOutcome
{
    /// <summary>It stays where it was dropped (not over the scanner).</summary>
    Stays,

    /// <summary>It starts scanning (over the idle scanner).</summary>
    Scanning,

    /// <summary>It goes back to where it was picked up (the scanner is busy, or the paper could not be dropped).</summary>
    Refused
}

/// <summary>
/// One case's papers: each is handed over once (from the traveller onto the
/// desk), can be dragged while on the desk, and is scanned by dropping it on
/// the scanner, one scan at a time, for a fixed duration; at the decision
/// every paper goes back and a running scan is cancelled. Pure, so every
/// state and outcome is tested headless; DeskController animates them.
/// </summary>
public sealed class DeskPapers
{
    /// <summary>The shortest scan (a shorter duration is raised to it).</summary>
    private const float MinScanSeconds = 0.01f;

    /// <summary>Where a paper is.</summary>
    private enum PaperState
    {
        /// <summary>Not handed over yet.</summary>
        WithTraveller,

        /// <summary>On the desk, draggable.</summary>
        OnDesk,

        /// <summary>On the scanner's bed, being scanned.</summary>
        Scanning,

        /// <summary>Given back at the decision.</summary>
        Returned
    }

    private readonly PaperState[] _states;
    private readonly float _scanSeconds;

    /// <summary>The paper being scanned, or -1.</summary>
    private int _scanning = -1;

    /// <summary>Seconds the running scan has taken.</summary>
    private float _elapsed;

    /// <summary>Every paper starts with the traveller; a null list means no papers; a scan shorter than 0.01 s is raised to it.</summary>
    public DeskPapers(IReadOnlyList<CaseDocument> documents, float scanSeconds)
    {
        _states = new PaperState[documents != null ? documents.Count : 0];
        ArrivalIndices = CaseDocuments.ArrivalIndices(documents);
        _scanSeconds = scanSeconds > MinScanSeconds ? scanSeconds : MinScanSeconds;
    }

    /// <summary>How many papers the case has.</summary>
    public int Count => _states.Length;

    /// <summary>True while a scan runs.</summary>
    public bool ScannerBusy => _scanning >= 0;

    /// <summary>The papers handed over on arrival, in paper order (CaseDocuments.ArrivalIndices).</summary>
    public IReadOnlyList<int> ArrivalIndices { get; }

    /// <summary>Papers on the desk, the one being scanned included.</summary>
    public int OnDeskCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < _states.Length; i++)
                if (StateOf(i) == PaperState.OnDesk || StateOf(i) == PaperState.Scanning)
                    n++;
            return n;
        }
    }

    /// <summary>Hands a paper over from the traveller onto the desk; false for any other state or an index out of range.</summary>
    public bool HandOver(int i)
    {
        if (!InRange(i) || StateOf(i) != PaperState.WithTraveller)
            return false;

        _states[i] = PaperState.OnDesk;
        return true;
    }

    /// <summary>True while the paper is on the desk (false out of range).</summary>
    public bool CanDrag(int i) => InRange(i) && StateOf(i) == PaperState.OnDesk;

    /// <summary>
    /// Decides a released paper: a paper that is not on the desk (or an index
    /// out of range) is Refused and nothing changes; a paper on the desk stays
    /// where it was dropped unless it is over the scanner, where it starts
    /// scanning while the scanner is idle and is Refused while it is busy. A
    /// scanned paper may be scanned again.
    /// </summary>
    public DropOutcome Drop(int i, bool overScanner)
    {
        if (!CanDrag(i))
            return DropOutcome.Refused;
        if (!overScanner)
            return DropOutcome.Stays;
        if (ScannerBusy)
            return DropOutcome.Refused;

        BeginScan(i);
        return DropOutcome.Scanning;
    }

    /// <summary>
    /// Advances a running scan by a positive amount (0 and negative amounts
    /// are ignored). When it reaches the scan duration the paper goes back on
    /// the desk and its index is returned; otherwise, or with no scan
    /// running, -1.
    /// </summary>
    public int Tick(float seconds)
    {
        if (!ScannerBusy || !(seconds > 0f))
            return -1;

        _elapsed += seconds;
        if (_elapsed < _scanSeconds)
            return -1;

        int done = _scanning;
        _states[done] = PaperState.OnDesk;
        _scanning = -1;
        _elapsed = 0f;
        return done;
    }

    /// <summary>Every paper goes back to the traveller's side for good; a running scan is cancelled and never finishes.</summary>
    public void ReturnAll()
    {
        for (int i = 0; i < _states.Length; i++)
            _states[i] = PaperState.Returned;
        _scanning = -1;
        _elapsed = 0f;
    }

    /// <summary>Puts a paper on the scanner's bed and starts the timer (Drop is the only caller).</summary>
    private void BeginScan(int i)
    {
        _states[i] = PaperState.Scanning;
        _scanning = i;
        _elapsed = 0f;
    }

    /// <summary>A paper's state (callers check the range).</summary>
    private PaperState StateOf(int i) => _states[i];

    /// <summary>True for a valid paper index.</summary>
    private bool InRange(int i) => i >= 0 && i < _states.Length;
}

/// <summary>When the day-1 desk notes show (their text and last day are DeskConfigSO knobs).</summary>
public static class DeskHints
{
    /// <summary>The scanner note: a non-blank hint, on or before its last day, while a paper is on the desk and nothing was scanned yet today.</summary>
    public static bool ScanHintVisible(string hint, int day, int untilDay, int scansToday, bool paperOnDesk) =>
        !string.IsNullOrWhiteSpace(hint) && day <= untilDay && scansToday == 0 && paperOnDesk;

    /// <summary>The wheel note: a non-blank hint, on or before its last day, while a traveller is at the desk and the wheel was not opened yet today.</summary>
    public static bool WheelHintVisible(string hint, int day, int untilDay, bool wheelOpenedToday, bool travellerAtDesk) =>
        !string.IsNullOrWhiteSpace(hint) && day <= untilDay && !wheelOpenedToday && travellerAtDesk;
}
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 394, failed 0` (367 + 27: 15 tests and the 12-row drop table).

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/make_meta.py" Assets/Scripts/Domain/DeskPapers.cs Assets/Tests/EditMode/DeskPapersTests.cs && git add Assets/Scripts/Domain/DeskPapers.cs Assets/Scripts/Domain/DeskPapers.cs.meta Assets/Tests/EditMode/DeskPapersTests.cs Assets/Tests/EditMode/DeskPapersTests.cs.meta && git commit -F - <<'EOF'
feat(domain): papers on the desk, drops and scans (DeskPapers, DeskHints)

A case's papers are handed over once, dragged while on the desk and
scanned one at a time for a fixed duration; the drop outcome (stays,
scanning, refused) is decided in Domain; the decision returns every paper
and cancels a running scan. DeskHints says when the day-1 scanner and wheel
notes show. DocumentHandOver is the template knob's enum (OnRequest = 0).

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 4: Desk and window geometry (`DeskGeometry`)

Spec §2.6: the desk rectangle (containment, clamping, points across it), the per-axis clamp windows and callouts use (a window taller than the screen keeps its title bar visible), the paper stack's order, and the sorting-band check the builder runs.

**Files:**
- Create: `Assets/Scripts/Visuals/DeskGeometry.cs` (+ `.meta`)
- Test: `Assets/Tests/EditMode/DeskGeometryTests.cs` (+ `.meta`)

- [ ] **Step 1: Write the failing test** (`Assets/Tests/EditMode/DeskGeometryTests.cs`, Write tool)

```csharp
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The desk's geometry: the rectangle papers stay in (a 4 x 2 rectangle
/// centred on (1, -1): x -1..3, y -2..0), the window clamp,
/// the paper stack and the sorting bands.
/// </summary>
public class DeskGeometryTests
{
    private static readonly DeskRect Desk = new DeskRect(1f, -1f, 4f, 2f);

    // -----------------------------
    // DeskRect
    // -----------------------------

    [Test]
    public void Contains_IncludesTheEdges_AndNothingOutside()
    {
        Assert.IsTrue(Desk.Contains(1f, -1f), "centre");
        Assert.IsTrue(Desk.Contains(-1f, -2f), "bottom-left corner");
        Assert.IsTrue(Desk.Contains(3f, 0f), "top-right corner");
        Assert.IsFalse(Desk.Contains(-1.01f, -1f), "left");
        Assert.IsFalse(Desk.Contains(3.01f, -1f), "right");
        Assert.IsFalse(Desk.Contains(1f, -2.01f), "below");
        Assert.IsFalse(Desk.Contains(1f, 0.01f), "above");
    }

    [Test]
    public void Clamp_MovesEachSideIn_AndLeavesAPointInside()
    {
        Assert.AreEqual((-1f, -1f), Desk.Clamp(-5f, -1f), "left");
        Assert.AreEqual((3f, -1f), Desk.Clamp(9f, -1f), "right");
        Assert.AreEqual((1f, -2f), Desk.Clamp(1f, -7f), "below");
        Assert.AreEqual((1f, 0f), Desk.Clamp(1f, 4f), "above");
        Assert.AreEqual((3f, 0f), Desk.Clamp(8f, 8f), "a corner");
        Assert.AreEqual((0.5f, -1.5f), Desk.Clamp(0.5f, -1.5f), "inside");
    }

    [Test]
    public void PointAt_MapsZeroToOne_FromTheBottomLeft_ClampingUV()
    {
        Assert.AreEqual((-1f, -2f), Desk.PointAt(0f, 0f));
        Assert.AreEqual((3f, 0f), Desk.PointAt(1f, 1f));
        Assert.AreEqual((1f, -1f), Desk.PointAt(0.5f, 0.5f));
        Assert.AreEqual((0f, -1.5f), Desk.PointAt(0.25f, 0.25f));
        Assert.AreEqual((-1f, 0f), Desk.PointAt(-3f, 7f), "u and v are clamped to 0..1");
    }

    [Test]
    public void NegativeSizes_CountAsZero()
    {
        var point = new DeskRect(2f, 3f, -4f, -1f);
        Assert.IsTrue(point.Contains(2f, 3f));
        Assert.IsFalse(point.Contains(2.5f, 3f));
        Assert.AreEqual((2f, 3f), point.Clamp(-10f, 10f));
        Assert.AreEqual((2f, 3f), point.PointAt(1f, 0f));
    }

    // -----------------------------
    // RectClamp
    // -----------------------------

    [Test]
    public void Shift_IsZeroInside_AndMovesASpanBackOnEachSide()
    {
        Assert.AreEqual(0f, RectClamp.Shift(-2f, 2f, -10f, 10f, false), "inside");
        Assert.AreEqual(0f, RectClamp.Shift(-10f, 10f, -10f, 10f, true), "exactly fitting");
        Assert.AreEqual(3f, RectClamp.Shift(-13f, -9f, -10f, 10f, false), "past the low side");
        Assert.AreEqual(-4f, RectClamp.Shift(8f, 14f, -10f, 10f, false), "past the high side");
        Assert.AreEqual(5f, RectClamp.Shift(-15f, 0f, -10f, 10f, true), "below, keepMax only matters when oversized");
        Assert.AreEqual(-2f, RectClamp.Shift(0f, 12f, -10f, 10f, true), "above");
    }

    [Test]
    public void Shift_AnOversizedSpan_AlignsItsMaxWithKeepMax_ElseItsMin()
    {
        Assert.AreEqual(5f, RectClamp.Shift(-20f, 5f, -10f, 10f, true), "keepMax: max to hi (a window's title bar stays visible)");
        Assert.AreEqual(10f, RectClamp.Shift(-20f, 5f, -10f, 10f, false), "otherwise: min to lo");
        Assert.AreEqual(-30f, RectClamp.Shift(10f, 40f, -10f, 10f, true));
        Assert.AreEqual(-20f, RectClamp.Shift(10f, 40f, -10f, 10f, false));
    }

    // -----------------------------
    // PaperStack
    // -----------------------------

    [Test]
    public void AddPutsOnTop_ReAddMoves_BringToFrontMoves_ClearEmpties()
    {
        var stack = new PaperStack();
        stack.Add(4);
        stack.Add(1);
        stack.Add(7);
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, new[] { stack.IndexOf(4), stack.IndexOf(1), stack.IndexOf(7) });

        stack.Add(4);
        CollectionAssert.AreEqual(new[] { 2, 0, 1 }, new[] { stack.IndexOf(4), stack.IndexOf(1), stack.IndexOf(7) }, "re-adding moves it to the top");

        stack.BringToFront(1);
        CollectionAssert.AreEqual(new[] { 1, 2, 0 }, new[] { stack.IndexOf(4), stack.IndexOf(1), stack.IndexOf(7) });

        stack.BringToFront(9);
        Assert.AreEqual(-1, stack.IndexOf(9), "bringing an absent paper forward adds nothing");

        stack.Clear();
        Assert.AreEqual(-1, stack.IndexOf(4));
        Assert.AreEqual(-1, stack.IndexOf(1));
    }

    // -----------------------------
    // SortingBands
    // -----------------------------

    /// <summary>The default bands: props up to 6, exit 10, glass 11, bezel 12, screen 20, papers from 30 (2 of them), held 60.</summary>
    private static List<string> Bands(int props = 6, int exit = 10, int glass = 11, int bezel = 12, int screen = 20, int paper = 30, int papers = 2, int held = 60) =>
        SortingBands.Problems(props, exit, glass, bezel, screen, paper, papers, held);

    private static void OneProblemNaming(List<string> problems, string upper, string lower, int value)
    {
        Assert.AreEqual(1, problems.Count, string.Join(" | ", problems));
        StringAssert.Contains(upper, problems[0]);
        StringAssert.Contains(lower, problems[0]);
        StringAssert.Contains(value.ToString(), problems[0]);
    }

    [Test]
    public void TheDefaultBands_HaveNoProblem()
    {
        CollectionAssert.IsEmpty(Bands());
    }

    [Test]
    public void EachBandEqualToTheOneBelow_IsOneProblemNamingBoth()
    {
        OneProblemNaming(Bands(exit: 6), "focus exit zone", "highest prop", 6);
        OneProblemNaming(Bands(glass: 10), "glass zone", "focus exit zone", 10);
        OneProblemNaming(Bands(bezel: 11), "bezel", "glass zone", 11);
        OneProblemNaming(Bands(screen: 12), "screen canvas", "bezel", 12);
        OneProblemNaming(Bands(paper: 20), "paper base", "screen canvas", 20);
    }

    [Test]
    public void TheHeldPaper_MustSitAboveEveryStackedPaper()
    {
        CollectionAssert.IsEmpty(Bands(paper: 30, papers: 4, held: 34));
        OneProblemNaming(Bands(paper: 30, papers: 4, held: 33), "held paper", "top paper", 33);
        OneProblemNaming(Bands(paper: 30, papers: 2, held: 30), "held paper", "top paper", 31);
    }
}
```

- [ ] **Step 2: Run the compile check — expect failure**

Expected: `== Assembly-CSharp-Editor.csproj: exit 0` and `== TimeDeskEditMode.csproj: exit 1` with one distinct error, `error CS0246: The type or namespace name 'DeskRect' could not be found` (the compiler stops at the unresolved parameter type before reporting the other missing names).

- [ ] **Step 3: Implement** — write `Assets/Scripts/Visuals/DeskGeometry.cs` (Write tool):

```csharp
using System;
using System.Collections.Generic;

/// <summary>
/// An axis-aligned rectangle in a desk's local plane (the rectangle paper
/// centres stay in, a scanner's drop area). Negative sizes count as 0.
/// </summary>
public readonly struct DeskRect
{
    private readonly float _centreX;
    private readonly float _centreY;
    private readonly float _halfWidth;
    private readonly float _halfHeight;

    /// <summary>Creates a rectangle from its centre and size.</summary>
    public DeskRect(float centreX, float centreY, float width, float height)
    {
        _centreX = centreX;
        _centreY = centreY;
        _halfWidth = Math.Max(width, 0f) / 2f;
        _halfHeight = Math.Max(height, 0f) / 2f;
    }

    /// <summary>True when the point lies inside or on an edge.</summary>
    public bool Contains(float x, float y) =>
        x >= _centreX - _halfWidth && x <= _centreX + _halfWidth &&
        y >= _centreY - _halfHeight && y <= _centreY + _halfHeight;

    /// <summary>The nearest point inside the rectangle.</summary>
    public (float x, float y) Clamp(float x, float y) =>
        (Math.Min(Math.Max(x, _centreX - _halfWidth), _centreX + _halfWidth),
         Math.Min(Math.Max(y, _centreY - _halfHeight), _centreY + _halfHeight));

    /// <summary>The point at (u, v) across the rectangle, each clamped to 0..1; (0, 0) is the bottom left.</summary>
    public (float x, float y) PointAt(float u, float v) =>
        (_centreX - _halfWidth + 2f * _halfWidth * Math.Min(Math.Max(u, 0f), 1f),
         _centreY - _halfHeight + 2f * _halfHeight * Math.Min(Math.Max(v, 0f), 1f));
}

/// <summary>Keeps a span inside a range, one axis at a time (desktop windows, overlay callouts).</summary>
public static class RectClamp
{
    /// <summary>
    /// The offset that moves the span [min, max] inside [lo, hi]: 0 when it
    /// already is inside; for a span longer than the range, the offset that
    /// aligns max with hi when <paramref name="keepMax"/> (a window's title bar
    /// stays visible), else min with lo.
    /// </summary>
    public static float Shift(float min, float max, float lo, float hi, bool keepMax)
    {
        if (max - min > hi - lo)
            return keepMax ? hi - max : lo - min;
        if (min < lo)
            return lo - min;
        if (max > hi)
            return hi - max;
        return 0f;
    }
}

/// <summary>
/// The z order of the papers on the desk by paper id, bottom first. Papers
/// never leave the stack one by one; the case's end clears it.
/// </summary>
public sealed class PaperStack
{
    private readonly List<int> _order = new List<int>();

    /// <summary>Puts the paper on top (re-adding moves it there).</summary>
    public void Add(int id)
    {
        _order.Remove(id);
        _order.Add(id);
    }

    /// <summary>Moves a stacked paper to the top (an absent one stays absent).</summary>
    public void BringToFront(int id)
    {
        if (_order.Remove(id))
            _order.Add(id);
    }

    /// <summary>Empties the stack.</summary>
    public void Clear() => _order.Clear();

    /// <summary>The paper's place from the bottom (0), or -1 when it is not stacked.</summary>
    public int IndexOf(int id) => _order.IndexOf(id);
}

/// <summary>The booth's sorting bands on the Default layer, checked by the builder so input and drawing order agree.</summary>
public static class SortingBands
{
    /// <summary>
    /// Every band that does not sit strictly above the one below it: props &lt;
    /// focus exit zone &lt; glass zone &lt; bezel &lt; screen canvas &lt; paper
    /// base, and the held paper above the top stacked paper (paper base +
    /// <paramref name="maxPapers"/> - 1). Each problem names both bands and
    /// their values; empty when the bands are sound.
    /// </summary>
    public static List<string> Problems(int maxPropOrder, int focusExitOrder, int glassOrder, int bezelOrder,
                                        int screenCanvasOrder, int paperBaseOrder, int maxPapers, int heldPaperOrder)
    {
        var problems = new List<string>();
        void Above(string upper, int upperOrder, string lower, int lowerOrder)
        {
            if (upperOrder <= lowerOrder)
                problems.Add($"the {upper} ({upperOrder}) must sit above the {lower} ({lowerOrder})");
        }

        Above("focus exit zone", focusExitOrder, "highest prop order", maxPropOrder);
        Above("glass zone", glassOrder, "focus exit zone", focusExitOrder);
        Above("bezel", bezelOrder, "glass zone", glassOrder);
        Above("screen canvas", screenCanvasOrder, "bezel", bezelOrder);
        Above("paper base", paperBaseOrder, "screen canvas", screenCanvasOrder);
        Above("held paper", heldPaperOrder, "top paper order", paperBaseOrder + maxPapers - 1);
        return problems;
    }
}
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 404, failed 0` (394 + 10).

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/make_meta.py" Assets/Scripts/Visuals/DeskGeometry.cs Assets/Tests/EditMode/DeskGeometryTests.cs && git add Assets/Scripts/Visuals/DeskGeometry.cs Assets/Scripts/Visuals/DeskGeometry.cs.meta Assets/Tests/EditMode/DeskGeometryTests.cs Assets/Tests/EditMode/DeskGeometryTests.cs.meta && git commit -F - <<'EOF'
feat(visuals): desk geometry, window clamp, paper stack and sorting bands

DeskRect keeps paper centres on the desk and places spawn slots; RectClamp
keeps a span inside a range (a window taller than the screen keeps its top);
PaperStack orders papers; SortingBands reports overlapping booth bands.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 5: The wheel's ring (`RadialLayout`)

Spec §2.6, R5: items on an ellipse (item 0 at the top, clockwise), how many choices fit without overlapping each other or the centre ("< Back"), and the ring's extent (the box the projection keeps on screen). The defaults fit 8, the interview's menu capacity. The spec's worked note that a circle of radius 210 "gives 6" is corrected here: five items already collide on that circle (items 2 and 3 sit level, 246.9 < 248 apart), so its `MaxFit` is 4 (Notes for the reviewer, 1).

**Files:**
- Create: `Assets/Scripts/Visuals/RadialLayout.cs` (+ `.meta`)
- Test: `Assets/Tests/EditMode/RadialLayoutTests.cs` (+ `.meta`)

- [ ] **Step 1: Write the failing test** (`Assets/Tests/EditMode/RadialLayoutTests.cs`, Write tool)

```csharp
using System;
using NUnit.Framework;

/// <summary>
/// The traveller wheel's ring: where each item sits, how many choices fit,
/// and the ring's extent. The wheel's defaults: radii 300 x 200, items
/// 240 x 44, the centre ("&lt; Back") 150 x 44, gap 8 (reference px).
/// </summary>
public class RadialLayoutTests
{
    private const float Tolerance = 0.001f;

    private static int FitOfTheDefaults(int limit) => RadialLayout.MaxFit(300f, 200f, 240f, 44f, 150f, 44f, 8f, limit);

    [Test]
    public void Point_ItemZeroIsAtTheTop()
    {
        (float x, float y) = RadialLayout.Point(0, 8, 300f, 200f);
        Assert.AreEqual(0f, x, Tolerance);
        Assert.AreEqual(200f, y, Tolerance);
    }

    [Test]
    public void Point_GoesClockwise()
    {
        (float x, float y) = RadialLayout.Point(1, 8, 300f, 200f);
        Assert.Greater(x, 0f, "item 1 is right of the top");
        Assert.AreEqual(300f * Math.Cos(Math.PI / 4), x, Tolerance);
        Assert.AreEqual(200f * Math.Sin(Math.PI / 4), y, Tolerance);
    }

    [Test]
    public void Point_FourItemsSitOnTheAxes()
    {
        var expected = new[] { (0f, 200f), (300f, 0f), (0f, -200f), (-300f, 0f) };
        for (int i = 0; i < 4; i++)
        {
            (float x, float y) = RadialLayout.Point(i, 4, 300f, 200f);
            Assert.AreEqual(expected[i].Item1, x, Tolerance, $"item {i} x");
            Assert.AreEqual(expected[i].Item2, y, Tolerance, $"item {i} y");
        }
    }

    [Test]
    public void Point_NoItems_IsTheCentre()
    {
        Assert.AreEqual((0f, 0f), RadialLayout.Point(0, 0, 300f, 200f));
        Assert.AreEqual((0f, 0f), RadialLayout.Point(2, -1, 300f, 200f));
    }

    [Test]
    public void MaxFit_TheDefaultsFitEight_TheInterviewsMenuCapacity()
    {
        // n = 7: the lowest pair sits at (+-130.1, -180.2), 260.2 >= 248 apart;
        // n = 8: top neighbours (0, 200) and (212.1, 141.4) are 58.6 >= 52 apart vertically;
        // n = 9: neighbours (0, 200) and (192.8, 153.2) are 46.8 < 52 apart vertically.
        Assert.AreEqual(8, FitOfTheDefaults(16));
    }

    [Test]
    public void MaxFit_ACircleOfRadius210_FitsFour()
    {
        // n = 5 already fails: items 2 and 3 sit level at (+-123.4, -169.9), 246.9 < 248 apart.
        Assert.AreEqual(4, RadialLayout.MaxFit(210f, 210f, 240f, 44f, 150f, 44f, 8f, 16));
    }

    [Test]
    public void MaxFit_ACentreAsLargeAsTheRing_FitsNothing()
    {
        Assert.AreEqual(0, RadialLayout.MaxFit(300f, 200f, 240f, 44f, 900f, 500f, 8f, 16));
    }

    [Test]
    public void MaxFit_RespectsTheLimit()
    {
        Assert.AreEqual(5, FitOfTheDefaults(5));
        Assert.AreEqual(0, FitOfTheDefaults(0));
    }

    [Test]
    public void Extent_IsTheBoxAroundEveryPossibleItem()
    {
        Assert.AreEqual((840f, 444f), RadialLayout.Extent(300f, 200f, 240f, 44f));
    }
}
```

- [ ] **Step 2: Run the compile check — expect failure**

Expected: `== Assembly-CSharp-Editor.csproj: exit 0` and `== TimeDeskEditMode.csproj: exit 1` with one distinct error, `error CS0103: The name 'RadialLayout' does not exist in the current context`.

- [ ] **Step 3: Implement** — write `Assets/Scripts/Visuals/RadialLayout.cs` (Write tool):

```csharp
using System;

/// <summary>
/// The traveller wheel's ring: items on an ellipse, item 0 at the top, then
/// clockwise; how many items of a size fit without overlapping each other or
/// the centre box; and the box the ring can cover. Engine-free, so the layout
/// RadialLayoutGroup applies and the fit the builder checks are tested headless.
/// </summary>
public static class RadialLayout
{
    /// <summary>Where item <paramref name="i"/> of <paramref name="n"/> sits, relative to the ring's centre (angle 90 - i * 360 / n degrees on the ellipse); (0, 0) when n is 0 or less.</summary>
    public static (float x, float y) Point(int i, int n, float radiusX, float radiusY)
    {
        if (n <= 0)
            return (0f, 0f);

        double angle = (90.0 - i * 360.0 / n) * Math.PI / 180.0;
        return ((float)(radiusX * Math.Cos(angle)), (float)(radiusY * Math.Sin(angle)));
    }

    /// <summary>
    /// The largest m up to <paramref name="limit"/> such that every count from
    /// 1 to m fits (the wheel may show any number of choices up to its
    /// capacity); 0 when even one item does not fit.
    /// </summary>
    public static int MaxFit(float radiusX, float radiusY, float itemW, float itemH, float centreW, float centreH, float gap, int limit)
    {
        int fit = 0;
        for (int n = 1; n <= limit; n++)
        {
            if (!Fits(n, radiusX, radiusY, itemW, itemH, centreW, centreH, gap))
                break;
            fit = n;
        }

        return fit;
    }

    /// <summary>The size of the box around every item the ring can place: the ellipse plus one item each way.</summary>
    public static (float width, float height) Extent(float radiusX, float radiusY, float itemW, float itemH) =>
        (2f * radiusX + itemW, 2f * radiusY + itemH);

    /// <summary>True when n items overlap neither each other nor the centre box, at least <paramref name="gap"/> apart.</summary>
    private static bool Fits(int n, float radiusX, float radiusY, float itemW, float itemH, float centreW, float centreH, float gap)
    {
        var points = new (float x, float y)[n];
        for (int i = 0; i < n; i++)
            points[i] = Point(i, n, radiusX, radiusY);

        for (int i = 0; i < n; i++)
        {
            if (!Apart(points[i].x, points[i].y, itemW, itemH, 0f, 0f, centreW, centreH, gap))
                return false;

            for (int j = i + 1; j < n; j++)
                if (!Apart(points[i].x, points[i].y, itemW, itemH, points[j].x, points[j].y, itemW, itemH, gap))
                    return false;
        }

        return true;
    }

    /// <summary>Two centred boxes are apart when they are separated by the gap on at least one axis.</summary>
    private static bool Apart(float ax, float ay, float aw, float ah, float bx, float by, float bw, float bh, float gap) =>
        Math.Abs(ax - bx) >= (aw + bw) / 2f + gap || Math.Abs(ay - by) >= (ah + bh) / 2f + gap;
}
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 413, failed 0` (404 + 9).

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/make_meta.py" Assets/Scripts/Visuals/RadialLayout.cs Assets/Tests/EditMode/RadialLayoutTests.cs && git add Assets/Scripts/Visuals/RadialLayout.cs Assets/Scripts/Visuals/RadialLayout.cs.meta Assets/Tests/EditMode/RadialLayoutTests.cs Assets/Tests/EditMode/RadialLayoutTests.cs.meta && git commit -F - <<'EOF'
feat(visuals): the traveller wheel's ring layout and fit (RadialLayout)

Items sit on an ellipse, item 0 at the top then clockwise; MaxFit is the
most choices that fit (every count up to it) without overlapping each other
or the centre slot: 8 for the default 300 x 200 ring, the interview's menu
capacity; Extent is the box around every item.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 6: The focus framing and the props' reaction poses (`MonitorFraming`, `ReactionCurve`)

Spec §2.6, R12, K14: the orthographic size at which the CRT's glass fills a fraction of the view (height-limited, or width-limited on narrow screens), and the click reactions' poses (squash, wobble, nudge, pulse; every kind at rest at both ends). `ReactionKind` is serialized on `DeskReactionSO`, so its ints are pinned.

**Files:**
- Create: `Assets/Scripts/Visuals/MonitorFraming.cs`, `Assets/Scripts/Visuals/ReactionCurve.cs` (+ `.meta`s)
- Test: `Assets/Tests/EditMode/MonitorFramingTests.cs`, `Assets/Tests/EditMode/ReactionCurveTests.cs` (+ `.meta`s)

- [ ] **Step 1: Write the failing tests** — `Assets/Tests/EditMode/MonitorFramingTests.cs` (Write tool):

```csharp
using NUnit.Framework;

/// <summary>The monitor camera's framing: the glass fills a fraction of the view's height, or of its width on narrow screens.</summary>
public class MonitorFramingTests
{
    private const float Tolerance = 0.001f;

    [Test]
    public void TheCrtGlass_AtSixteenByNine_IsHeightLimited()
    {
        // The glass is 2.343 x 1.758 world units; at fill 0.85 it is 85% of the view's height.
        Assert.AreEqual(1.034f, MonitorFraming.OrthoSize(2.343f, 1.758f, 16f / 9f, 0.85f), Tolerance);
    }

    [Test]
    public void AWideGlass_IsWidthLimited_AtFourByThreeAndAtOneByOne()
    {
        Assert.AreEqual(3f, MonitorFraming.OrthoSize(4f, 1f, 4f / 3f, 0.5f), Tolerance, "width 4 at 4:3 needs a view 3 high");
        Assert.AreEqual(2f, MonitorFraming.OrthoSize(2f, 1f, 1f, 0.5f), Tolerance, "width 2 at 1:1 needs a view 2 high");
    }

    [Test]
    public void TheFill_IsClampedToFivePercentAndOne()
    {
        Assert.AreEqual(10f, MonitorFraming.OrthoSize(1f, 1f, 1f, 0f), Tolerance, "0 counts as 0.05");
        Assert.AreEqual(0.5f, MonitorFraming.OrthoSize(1f, 1f, 1f, 2f), Tolerance, "2 counts as 1");
    }

    [Test]
    public void AnAspectOfZeroOrLess_CountsAsOne()
    {
        Assert.AreEqual(2f, MonitorFraming.OrthoSize(2f, 1f, 0f, 0.5f), Tolerance);
        Assert.AreEqual(2f, MonitorFraming.OrthoSize(2f, 1f, -3f, 0.5f), Tolerance);
    }
}
```

and `Assets/Tests/EditMode/ReactionCurveTests.cs` (Write tool):

```csharp
using System;
using NUnit.Framework;

/// <summary>The desk props' click reactions: every pose starts and ends at rest, and the peaks.</summary>
public class ReactionCurveTests
{
    private const float Tolerance = 0.0001f;
    private const float A = 0.12f;

    private static void AssertPose(ReactionPose expected, ReactionPose actual, string what)
    {
        Assert.AreEqual(expected.ScaleX, actual.ScaleX, Tolerance, what + " scale x");
        Assert.AreEqual(expected.ScaleY, actual.ScaleY, Tolerance, what + " scale y");
        Assert.AreEqual(expected.AngleDeg, actual.AngleDeg, Tolerance, what + " angle");
        Assert.AreEqual(expected.OffsetY, actual.OffsetY, Tolerance, what + " offset");
    }

    [Test]
    public void EveryKind_IsAtRestAtTheStartAndTheEnd()
    {
        foreach (ReactionKind kind in Enum.GetValues(typeof(ReactionKind)))
        {
            AssertPose(ReactionPose.Identity, ReactionCurve.Evaluate(kind, 0f, A), $"{kind} at 0");
            AssertPose(ReactionPose.Identity, ReactionCurve.Evaluate(kind, 1f, A), $"{kind} at 1");
        }
    }

    [Test]
    public void SquashPulseAndNudge_PeakHalfway()
    {
        AssertPose(new ReactionPose(1f + A, 1f - A, 0f, 0f), ReactionCurve.Evaluate(ReactionKind.Squash, 0.5f, A), "squash");
        AssertPose(new ReactionPose(1f + A, 1f + A, 0f, 0f), ReactionCurve.Evaluate(ReactionKind.Pulse, 0.5f, A), "pulse");
        AssertPose(new ReactionPose(1f, 1f, 0f, -A), ReactionCurve.Evaluate(ReactionKind.Nudge, 0.5f, A), "nudge");
    }

    [Test]
    public void Wobble_AtOneSixth_TurnsTwentyFiveTimesTheAmplitude()
    {
        // a * 30 * sin(pi / 2) * (1 - 1/6) = 25a degrees.
        AssertPose(new ReactionPose(1f, 1f, 25f * A, 0f), ReactionCurve.Evaluate(ReactionKind.Wobble, 1f / 6f, A), "wobble");
    }

    [Test]
    public void TimeIsClamped()
    {
        AssertPose(ReactionPose.Identity, ReactionCurve.Evaluate(ReactionKind.Squash, -1f, A), "before");
        AssertPose(ReactionPose.Identity, ReactionCurve.Evaluate(ReactionKind.Pulse, 3f, A), "after");
    }

    [Test]
    public void None_IsAlwaysAtRest()
    {
        foreach (float t in new[] { 0f, 0.25f, 0.5f, 0.9f, 1f })
            AssertPose(ReactionPose.Identity, ReactionCurve.Evaluate(ReactionKind.None, t, A), $"none at {t}");
    }

    [Test]
    public void TheKindInts_ArePinned_TheyAreSerializedOnDeskReactionSO()
    {
        Assert.AreEqual(0, (int)ReactionKind.None);
        Assert.AreEqual(1, (int)ReactionKind.Squash);
        Assert.AreEqual(2, (int)ReactionKind.Wobble);
        Assert.AreEqual(3, (int)ReactionKind.Nudge);
        Assert.AreEqual(4, (int)ReactionKind.Pulse);
    }
}
```

- [ ] **Step 2: Run the compile check — expect failure**

Expected: `== Assembly-CSharp-Editor.csproj: exit 0` and `== TimeDeskEditMode.csproj: exit 1` with one distinct error, `error CS0246: The type or namespace name 'ReactionPose' could not be found` (the compiler stops at the unresolved type before reporting the other missing names).

- [ ] **Step 3: Implement** — write `Assets/Scripts/Visuals/MonitorFraming.cs` (Write tool):

```csharp
using System;

/// <summary>How close the monitor camera frames the CRT's glass (the ReStory-style push-in).</summary>
public static class MonitorFraming
{
    /// <summary>
    /// The orthographic half-height at which a glass of <paramref name="width"/>
    /// x <paramref name="height"/> world units fills <paramref name="fill"/> of the
    /// view's height, or of its width when the screen is narrower than the
    /// glass: max(height, width / aspect) / (2 * fill). The fill is clamped to
    /// [0.05, 1]; an aspect of 0 or less counts as 1.
    /// </summary>
    public static float OrthoSize(float width, float height, float aspect, float fill)
    {
        float f = Math.Min(Math.Max(fill, 0.05f), 1f);
        float a = aspect > 0f ? aspect : 1f;
        return Math.Max(height, width / a) / (2f * f);
    }
}
```

and `Assets/Scripts/Visuals/ReactionCurve.cs` (Write tool):

```csharp
using System;

/// <summary>How a desk prop reacts to a click. Serialized on DeskReactionSO: append only.</summary>
public enum ReactionKind
{
    /// <summary>No movement (a tooltip-only prop).</summary>
    None,

    /// <summary>Wider and flatter, then back (a stamp's thunk).</summary>
    Squash,

    /// <summary>A damped rock from side to side.</summary>
    Wobble,

    /// <summary>A small dip down, then back.</summary>
    Nudge,

    /// <summary>Grows evenly, then back.</summary>
    Pulse
}

/// <summary>A prop's pose relative to its rest pose: scale factors, a turn and a vertical offset (local units).</summary>
public readonly struct ReactionPose
{
    /// <summary>Horizontal scale factor.</summary>
    public readonly float ScaleX;

    /// <summary>Vertical scale factor.</summary>
    public readonly float ScaleY;

    /// <summary>Turn about the view axis, degrees.</summary>
    public readonly float AngleDeg;

    /// <summary>Vertical offset, local units.</summary>
    public readonly float OffsetY;

    /// <summary>Creates a pose.</summary>
    public ReactionPose(float scaleX, float scaleY, float angleDeg, float offsetY)
    {
        ScaleX = scaleX;
        ScaleY = scaleY;
        AngleDeg = angleDeg;
        OffsetY = offsetY;
    }

    /// <summary>The rest pose.</summary>
    public static ReactionPose Identity => new ReactionPose(1f, 1f, 0f, 0f);
}

/// <summary>The click reactions' curves: every kind starts and ends at rest (DeskReaction plays them).</summary>
public static class ReactionCurve
{
    /// <summary>
    /// The pose at time <paramref name="t"/> (clamped to 0..1) with amplitude a,
    /// where s = sin(pi t): Squash (1 + a s, 1 - a s); Pulse (1 + a s, 1 + a s);
    /// Nudge an offset of -a s; Wobble a turn of a * 30 * sin(3 pi t) * (1 - t)
    /// degrees; None the rest pose.
    /// </summary>
    public static ReactionPose Evaluate(ReactionKind kind, float t, float amplitude)
    {
        double c = t < 0f ? 0.0 : t > 1f ? 1.0 : t;
        float s = (float)Math.Sin(Math.PI * c);

        switch (kind)
        {
            case ReactionKind.Squash:
                return new ReactionPose(1f + amplitude * s, 1f - amplitude * s, 0f, 0f);
            case ReactionKind.Pulse:
                return new ReactionPose(1f + amplitude * s, 1f + amplitude * s, 0f, 0f);
            case ReactionKind.Nudge:
                return new ReactionPose(1f, 1f, 0f, -amplitude * s);
            case ReactionKind.Wobble:
                return new ReactionPose(1f, 1f, (float)(amplitude * 30.0 * Math.Sin(3.0 * Math.PI * c) * (1.0 - c)), 0f);
            default:
                return ReactionPose.Identity;
        }
    }
}
```

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 423, failed 0` (413 + 4 + 6).

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/make_meta.py" Assets/Scripts/Visuals/MonitorFraming.cs Assets/Scripts/Visuals/ReactionCurve.cs Assets/Tests/EditMode/MonitorFramingTests.cs Assets/Tests/EditMode/ReactionCurveTests.cs && git add Assets/Scripts/Visuals/MonitorFraming.cs Assets/Scripts/Visuals/MonitorFraming.cs.meta Assets/Scripts/Visuals/ReactionCurve.cs Assets/Scripts/Visuals/ReactionCurve.cs.meta Assets/Tests/EditMode/MonitorFramingTests.cs Assets/Tests/EditMode/MonitorFramingTests.cs.meta Assets/Tests/EditMode/ReactionCurveTests.cs Assets/Tests/EditMode/ReactionCurveTests.cs.meta && git commit -F - <<'EOF'
feat(visuals): the monitor push-in framing and the props' reaction poses

MonitorFraming sizes the monitor camera so the CRT's glass fills a knob's
fraction of the view at any aspect; ReactionCurve gives the squash, wobble,
nudge and pulse poses, each at rest when it starts and ends.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 7: The display-text seam (`DisplayText`) and its callers

Spec §2.6, §2.10, K19, R19, R25: the one place displayed text may differ from its canonical value (item 8's translation reveals, piece 9). The scanned page shows each field value through it (Written) and the transcript each sentence (Spoken); every compare call keeps the canonical value, and the docs of `CompareController.Select` and `CompareEvidence.value` say so. The transcript's compare label becomes "Traveller · {category}". (The third caller, the traveller's reply in the wheel's bubble, arrives with the wheel in Task 14.)

**Files:**
- Create: `Assets/Scripts/Visuals/DisplayText.cs` (+ `.meta`)
- Test: `Assets/Tests/EditMode/DisplayTextTests.cs` (+ `.meta`)
- Modify: `Assets/Scripts/UI/TranscriptWindowController.cs`, `Assets/Scripts/UI/DocumentWindowController.cs`, `Assets/Scripts/UI/CompareController.cs`, `Assets/Scripts/Domain/DiscrepancyLog.cs`

- [ ] **Step 1: Write the failing test** (`Assets/Tests/EditMode/DisplayTextTests.cs`, Write tool)

```csharp
using NUnit.Framework;

/// <summary>The display-text seam (item 8's translation reveals, piece 9): today it shows the canonical text as it is.</summary>
public class DisplayTextTests
{
    [Test]
    public void BothMedia_ShowTheCanonicalText()
    {
        Assert.AreEqual("Deben", DisplayText.For("Deben", TextMedium.Written));
        Assert.AreEqual("We trade with Deben.", DisplayText.For("We trade with Deben.", TextMedium.Spoken));
    }

    [Test]
    public void Null_ShowsNothing()
    {
        Assert.AreEqual(string.Empty, DisplayText.For(null, TextMedium.Written));
        Assert.AreEqual(string.Empty, DisplayText.For(null, TextMedium.Spoken));
    }
}
```

- [ ] **Step 2: Run the compile check — expect failure**

Expected: `== Assembly-CSharp-Editor.csproj: exit 0` and `== TimeDeskEditMode.csproj: exit 1` with `error CS0103: The name 'DisplayText' does not exist in the current context` and `error CS0103: The name 'TextMedium' does not exist in the current context`.

- [ ] **Step 3: Implement** — write `Assets/Scripts/Visuals/DisplayText.cs` (Write tool):

```csharp
/// <summary>How a text reaches the player: printed on a document, or said aloud.</summary>
public enum TextMedium
{
    /// <summary>Printed (a document value).</summary>
    Written,

    /// <summary>Said (a transcript sentence, the traveller's reply).</summary>
    Spoken
}

/// <summary>
/// The one place displayed text may differ from its canonical value (item 8's
/// translation reveals, piece 9): the scanned document window's values, the
/// transcript's sentences and the traveller's reply in the speech bubble go
/// through it. Never pass its result to CompareController or CompareEvidence:
/// evidence stays canonical.
/// </summary>
public static class DisplayText
{
    /// <summary>The text to show for a canonical string; today the canonical string itself ("" for null).</summary>
    public static string For(string canonical, TextMedium medium) => canonical ?? string.Empty;
}
```

Then save as `SCRATCH/p7_t07.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

# The transcript shows each sentence through the display seam; the compare
# bar reads the canonical value and names the traveller (R19, R25).
apply(W + r'\Assets\Scripts\UI\TranscriptWindowController.cs', [
("""/// showing the newest page. Only answer rows are clickable: a click puts the
/// answer's canonical fact value into the compare bar as the traveller's
/// statement. Rows are named Line_{id}. No coroutines: the desktop canvas is
/// switched off outside monitor focus.
/// </summary>""",
"""/// showing the newest page. Only answer rows are clickable: a click puts the
/// answer's canonical fact value into the compare bar as the traveller's
/// statement. Sentences are shown through DisplayText (the spoken reveal
/// point); the compared value stays canonical. Rows are named Line_{id}.
/// </summary>"""),
("""            texts[1].text = line.Text;""",
"""            texts[1].text = DisplayText.For(line.Text, TextMedium.Spoken);"""),
("""        string label = $"Intercom · {ClueLabels.Report(line.Category)}";""",
"""        string label = $"Traveller · {ClueLabels.Report(line.Category)}";"""),
])

# A scanned page shows each value through the display seam; the compare call
# keeps the canonical value.
apply(W + r'\Assets\Scripts\UI\DocumentWindowController.cs', [
("""/// Renders one visitor document as a flippable, multi-page window. Each field
/// is a clickable row (label + value) that registers with the CompareController.""",
"""/// Renders one visitor document as a flippable, multi-page window. Each field
/// is a clickable row (label + value) that registers with the CompareController;
/// the value is shown through DisplayText and compared as its canonical text."""),
("""            if (texts.Length > 1 && texts[1] != null)
                texts[1].text = f.value;""",
"""            if (texts.Length > 1 && texts[1] != null)
                texts[1].text = DisplayText.For(f.value, TextMedium.Written);"""),
])

apply(W + r'\Assets\Scripts\UI\CompareController.cs', [
("""    /// <summary>Registers a clicked value for comparison (no typed evidence).</summary>""",
"""    /// <summary>Registers a clicked value for comparison (no typed evidence); value is the canonical value (it drives MATCH/MISMATCH; never display text).</summary>"""),
("""    /// <summary>Registers a clicked value for comparison, with typed evidence.</summary>""",
"""    /// <summary>Registers a clicked value for comparison, with typed evidence; value is the canonical value (it drives MATCH/MISMATCH; never display text).</summary>"""),
])

apply(W + r'\Assets\Scripts\Domain\DiscrepancyLog.cs', [
("""    /// <summary>The displayed value.</summary>
    public string value;""",
"""    /// <summary>The canonical value (never display text: see DisplayText).</summary>
    public string value;"""),
])
```

Expected: `ok …TranscriptWindowController.cs lf`, `ok …DocumentWindowController.cs crlf`, `ok …CompareController.cs crlf`, `ok …DiscrepancyLog.cs crlf`.

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 425, failed 0` (423 + 2).

- [ ] **Step 5: Commit**

```bash
cd /e/unity/NOPE-feat-clock && python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/make_meta.py" Assets/Scripts/Visuals/DisplayText.cs Assets/Tests/EditMode/DisplayTextTests.cs && git add Assets/Scripts/Visuals/DisplayText.cs Assets/Scripts/Visuals/DisplayText.cs.meta Assets/Tests/EditMode/DisplayTextTests.cs Assets/Tests/EditMode/DisplayTextTests.cs.meta Assets/Scripts/UI/TranscriptWindowController.cs Assets/Scripts/UI/DocumentWindowController.cs Assets/Scripts/UI/CompareController.cs Assets/Scripts/Domain/DiscrepancyLog.cs && git commit -F - <<'EOF'
feat(visuals): one display-text seam for documents and the transcript

DisplayText.For is the one place displayed text may differ from its
canonical value (item 8's translation, piece 9); the scanned page and the
transcript show text through it while every compare call keeps the
canonical value. The transcript's compare label reads "Traveller · …".

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 8: Documents handed over, one-shot requests and the traveller's reply

Spec §2.5, §2.11 (the no-desk path), §2.13, K5, K6, R2, R3, R25, R26: the interview's requests become hand-overs (`DialogAction.HandOverDocument`), one-shot, offered only for documents handed over on request (`CaseDocument.Requested`) while keeping the paper's index; "< Back" is the one choice of kind `Back` (the wheel's centre, Task 14); `InterviewScript.SpokenSince` gives the traveller's reply to a choice (the bubble text, Task 14); the menu-capacity wording names the traveller wheel, and `MenuProblems` counts requested documents only. `DocumentTemplateSO` gains the `handOver` knob (default `OnRequest`, so every existing template keeps today's behaviour until Task 9). `InvestigationUIController` builds the case's `CaseDocument`s (holder = the traveller's given name, which every Name field prints, §2.11) and, with no desk yet, hands every document straight to its window: documents handed over on arrival (`CaseDocuments.ArrivalIndices`, Task 3, the rule the desk's `DeskPapers` uses) open when the traveller is presented, a request opens its window, and each document's window gets a desktop icon the first time it opens (R3). `Decide` clears `_currentCase` before the decision callback, so a new case presented from that callback is not cleared afterwards (Notes for the reviewer, 2).

**Files:**
- Modify: `Assets/Tests/EditMode/InterviewScriptTests.cs`, `Assets/Tests/EditMode/InterviewDayTests.cs`
- Modify: `Assets/Scripts/Domain/Dialog.cs`, `Assets/Scripts/Domain/InterviewScript.cs`, `Assets/Scripts/Domain/InterviewContent.cs`, `Assets/Scripts/DocumentTemplateSO.cs`, `Assets/Scripts/UI/InvestigationUIController.cs`, `docs/FEATURES.md`

- [ ] **Step 1: Write the failing tests** — save as `SCRATCH/p7_t08_test.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'
T = W + r'\Assets\Tests\EditMode\InterviewScriptTests.cs'

apply(T, [
# --- the fixture: documents with their hand-over, both on request by default ---
("""/// capacity rules. Traveller under test: claims an Ancient place, carries a
/// Travel Passport and a Transit Permit, answers Currency honestly ("Deben")""",
"""/// capacity rules. Traveller under test: claims an Ancient place, carries a
/// Travel Passport and a Transit Permit (both handed over on request unless a
/// test says otherwise), answers Currency honestly ("Deben")"""),
("""    private static InterviewCase Case(bool smallTalk = true, string intro = "Next! Step forward, sir.") => new InterviewCase
    {
        introLine = intro,
        claimLine = "I request passage home to New Kingdom Egypt (Ancient).",
        claimedEraId = "ancient",
        documentNames = new[] { "Travel Passport", "Transit Permit" },""",
"""    private static CaseDocument Doc(string name, DocumentHandOver handOver = DocumentHandOver.OnRequest) =>
        new CaseDocument { name = name, holder = "Nefertari", handOver = handOver };

    private static InterviewCase Case(bool smallTalk = true, string intro = "Next! Step forward, sir.", CaseDocument[] documents = null) => new InterviewCase
    {
        introLine = intro,
        claimLine = "I request passage home to New Kingdom Egypt (Ancient).",
        claimedEraId = "ancient",
        documents = documents ?? new[] { Doc("Travel Passport"), Doc("Transit Permit") },"""),
# --- a request hands its document over, once ---
("""    [Test]
    public void Request_SpeaksPromptAndReply_OpensItsDocument_AndIsRepeatable()
    {
        DialogChoice permit = Build().Node(InterviewScript.HubNodeId).Choices[1];
        Assert.AreEqual(DialogAction.OpenDocument, permit.Action);
        Assert.AreEqual(1, permit.DocumentIndex);
        Assert.IsFalse(permit.OneShot);""",
"""    [Test]
    public void Request_SpeaksPromptAndReply_HandsItsDocumentOver_AndIsOneShot()
    {
        DialogChoice permit = Build().Node(InterviewScript.HubNodeId).Choices[1];
        Assert.AreEqual(DialogAction.HandOverDocument, permit.Action);
        Assert.AreEqual(1, permit.DocumentIndex);
        Assert.IsTrue(permit.OneShot, "a paper once handed over never goes back mid-case");"""),
("""    [Test]
    public void Hub_HasNoAskEntry_WithoutQuestionsOrSmallTalk()""",
"""    [Test]
    public void ARequest_LeavesTheHubOnceChosen()
    {
        var runner = new DialogRunner(Build(), InterviewScript.Opening(Case()));
        Assert.IsNotNull(runner.Choose("request:1"));
        CollectionAssert.AreEqual(new[] { "request:0", "ask", "dlg:dlg_rumour" }, Ids(runner.Choices));
        Assert.IsNull(runner.Choose("request:1"), "it cannot be chosen twice");
    }

    [Test]
    public void Hub_OnlyDocumentsHandedOverOnRequestGetARequest_KeepingThePaperIndex()
    {
        InterviewCase c = Case(documents: new[] { Doc("Travel Passport", DocumentHandOver.OnArrival), Doc("Transit Permit") });
        DialogNode hub = Build(c).Node(InterviewScript.HubNodeId);
        CollectionAssert.AreEqual(new[] { "request:1", "ask", "dlg:dlg_rumour" }, Ids(hub.Choices));
        Assert.AreEqual("Request Transit Permit", hub.Choices[0].Label);
        Assert.AreEqual(1, hub.Choices[0].DocumentIndex, "the index stays the paper's place in the case");
    }

    [Test]
    public void Hub_HasNoRequest_WhenEveryDocumentIsHandedOverOnArrival()
    {
        InterviewCase c = Case(documents: new[] { Doc("Travel Passport", DocumentHandOver.OnArrival), Doc("Transit Permit", DocumentHandOver.OnArrival) });
        CollectionAssert.AreEqual(new[] { "ask", "dlg:dlg_rumour" }, Ids(Build(c).Node(InterviewScript.HubNodeId).Choices));
    }

    [Test]
    public void Back_IsTheOnlyChoiceOfKindBack()
    {
        DialogGraph graph = Build();
        foreach (string node in new[] { InterviewScript.HubNodeId, InterviewScript.AskNodeId, "dlg_rumour/start", "dlg_rumour/detail" })
            foreach (DialogChoice choice in graph.Node(node).Choices)
                Assert.AreEqual(choice.Id == "back" ? DialogChoiceKind.Back : DialogChoiceKind.Normal, choice.Kind, $"{node}: {choice.Id}");
    }

    [Test]
    public void Hub_HasNoAskEntry_WithoutQuestionsOrSmallTalk()"""),
# --- wording: the traveller wheel ---
("""    public void Problems_ANodeWithMoreChoicesThanTheIntercomShows_UnlessTheCapacityIsZero()
    {
        AuthoredDialog d = Rumour();
        Only(DialogChecks.Problems(d, 1), "node 'start' offers 2 choices; the intercom shows at most 1");""",
"""    public void Problems_ANodeWithMoreChoicesThanTheWheelShows_UnlessTheCapacityIsZero()
    {
        AuthoredDialog d = Rumour();
        Only(DialogChecks.Problems(d, 1), "node 'start' offers 2 choices; the traveller wheel shows at most 1");"""),
("""        StringAssert.Contains("The ask menu holds 9 choices", Only(DialogChecks.MenuProblems(7, true, 2, 2, 8), "the intercom shows at most 8"));""",
"""        StringAssert.Contains("The ask menu holds 9 choices", Only(DialogChecks.MenuProblems(7, true, 2, 2, 8), "the traveller wheel shows at most 8"));"""),
("""    public void MenuProblems_TheHub_DocumentsPlusAskPlusDialogs()
    {
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(1, false, 2, 5, 8), "2 requests + ask + 5 dialogs = 8");
        StringAssert.Contains("The hub holds 9 choices", Only(DialogChecks.MenuProblems(1, false, 2, 6, 8), "the intercom shows at most 8"));""",
"""    public void MenuProblems_TheHub_RequestedDocumentsPlusAskPlusDialogs()
    {
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(1, false, maxRequestedDocuments: 2, dialogs: 5, maxChoices: 8), "2 requests + ask + 5 dialogs = 8");
        StringAssert.Contains("The hub holds 9 choices", Only(DialogChecks.MenuProblems(1, false, maxRequestedDocuments: 2, dialogs: 6, maxChoices: 8), "the traveller wheel shows at most 8"));"""),
])

# --- SpokenSince: the traveller's reply to a choice ---
apply(T, [
("""    // -----------------------------
    // Authored dialogs
    // -----------------------------
""",
"""    // -----------------------------
    // The traveller's reply (SpokenSince)
    // -----------------------------

    private static readonly DialogLine[] Said3 =
    {
        new DialogLine("case.intro", DialogSpeaker.Desk, "Next!"),
        new DialogLine("case.claim", DialogSpeaker.Traveller, "I request passage home."),
        new DialogLine("q.prompt", DialogSpeaker.Desk, "What do you trade with?"),
        new DialogLine("q.answer", DialogSpeaker.Traveller, "We trade with Deben."),
        new DialogLine("dlg.more", DialogSpeaker.Desk, "Tell me more."),
        new DialogLine("dlg.1", DialogSpeaker.Traveller, "Only a rumour."),
        new DialogLine("dlg.2", DialogSpeaker.Traveller, "A courier carries them.")
    };

    [Test]
    public void SpokenSince_JoinsTheTravellersLines_InOrder_SkippingTheDesk()
    {
        Assert.AreEqual("Only a rumour.\\nA courier carries them.", InterviewScript.SpokenSince(Said3, 4));
        Assert.AreEqual("We trade with Deben.\\nOnly a rumour.\\nA courier carries them.", InterviewScript.SpokenSince(Said3, 2));
    }

    [Test]
    public void SpokenSince_LeavesOutLinesBeforeFrom_AndCountsANegativeFromAsZero()
    {
        Assert.AreEqual("A courier carries them.", InterviewScript.SpokenSince(Said3, 6));
        Assert.AreEqual("I request passage home.\\nWe trade with Deben.\\nOnly a rumour.\\nA courier carries them.", InterviewScript.SpokenSince(Said3, -3));
    }

    [Test]
    public void SpokenSince_IsEmpty_AtOrPastTheEnd_ForDeskLinesOnly_AndForNoTranscript()
    {
        Assert.AreEqual(string.Empty, InterviewScript.SpokenSince(Said3, 7));
        Assert.AreEqual(string.Empty, InterviewScript.SpokenSince(Said3, 99));
        Assert.AreEqual(string.Empty, InterviewScript.SpokenSince(new[] { Said3[0], Said3[2] }, 0));
        Assert.AreEqual(string.Empty, InterviewScript.SpokenSince(null, 0));
    }

    [Test]
    public void SpokenSince_AfterARequest_IsTheTravellersReply()
    {
        var runner = new DialogRunner(Build(), InterviewScript.Opening(Case()));
        int before = runner.Transcript.Count;
        runner.Choose("request:0");
        Assert.AreEqual("Here you are.", InterviewScript.SpokenSince(runner.Transcript, before));
    }

    // -----------------------------
    // Authored dialogs
    // -----------------------------
"""),
])

apply(W + r'\Assets\Tests\EditMode\InterviewDayTests.cs', [
("""        // A rumour-shaped start node with two choices: too many for an intercom of one, fine with no capacity set.""",
"""        // A rumour-shaped start node with two choices: too many for a traveller wheel of one, fine with no capacity set."""),
("""        CollectionAssert.IsEmpty(Offered(one), "two choices never fit an intercom of one");""",
"""        CollectionAssert.IsEmpty(Offered(one), "two choices never fit a traveller wheel of one");"""),
("""        StringAssert.Contains("node 'start' offers 2 choices; the intercom shows at most 1", one.ContentProblems[0]);""",
"""        StringAssert.Contains("node 'start' offers 2 choices; the traveller wheel shows at most 1", one.ContentProblems[0]);"""),
])
```

Expected: `ok …InterviewScriptTests.cs lf`, `ok …InterviewScriptTests.cs lf`, `ok …InterviewDayTests.cs lf`.

- [ ] **Step 2: Run the compile check — expect failure**

Expected: `== Assembly-CSharp-Editor.csproj: exit 0` and `== TimeDeskEditMode.csproj: exit 1` with these six distinct errors: `error CS0103: The name 'DialogChoiceKind' does not exist in the current context`, `error CS0117: 'DialogAction' does not contain a definition for 'HandOverDocument'`, `error CS0117: 'InterviewCase' does not contain a definition for 'documents'`, `error CS0117: 'InterviewScript' does not contain a definition for 'SpokenSince'`, `error CS1061: 'DialogChoice' does not contain a definition for 'Kind' …` and `error CS1739: The best overload for 'MenuProblems' does not have a parameter named 'maxRequestedDocuments'`.

- [ ] **Step 3: Implement** — save as `SCRATCH/p7_t08.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

# --- Domain: the hand-over action, the Back kind ---
apply(W + r'\Assets\Scripts\Domain\Dialog.cs', [
("""    /// <summary>The traveller hands over a document: its window opens (DialogChoice.DocumentIndex).</summary>
    OpenDocument,""",
"""    /// <summary>The traveller hands a document over (DialogChoice.DocumentIndex): onto the desk, or straight to its window where no desk is wired.</summary>
    HandOverDocument,"""),
("""/// <summary>One transcript line. Immutable; an answer line also carries the fact it states.</summary>""",
"""/// <summary>What a choice is, for renderers: the wheel puts Back in its centre. Piece 8 appends kinds.</summary>
public enum DialogChoiceKind
{
    /// <summary>An ordinary choice (a ring item).</summary>
    Normal,

    /// <summary>The way back to the hub (the wheel's centre).</summary>
    Back
}

/// <summary>One transcript line. Immutable; an answer line also carries the fact it states.</summary>"""),
("""    /// <summary>The intercom button label.</summary>
    public string Label;""",
"""    /// <summary>The choice's label on the traveller wheel.</summary>
    public string Label;

    /// <summary>What the choice is, for renderers (Normal unless set).</summary>
    public DialogChoiceKind Kind;"""),
("""    /// <summary>OpenDocument: index of the document in paper order.</summary>""",
"""    /// <summary>HandOverDocument: index of the document in paper order.</summary>"""),
])

# --- Domain: requests only for documents handed over on request, one-shot; the reply ---
apply(W + r'\Assets\Scripts\Domain\InterviewScript.cs', [
("""    /// <summary>The traveller's documents' names, in paper order (one request each).</summary>
    public IReadOnlyList<string> documentNames;""",
"""    /// <summary>The traveller's documents in paper order; only those handed over on request get a hub request.</summary>
    public IReadOnlyList<CaseDocument> documents;"""),
("""    /// <summary>
    /// The traveller's graph. Hub: "request:{i}" per document (repeatable,
    /// opens document i), then "ask" when the ask menu has a question or""",
"""    /// <summary>
    /// What the traveller said since transcript line <paramref name="from"/>:
    /// the texts of the Traveller lines at or after it, in order, joined with a
    /// new line; empty when there are none. A null transcript gives ""; a from
    /// below 0 counts as 0. (The reply the traveller wheel's bubble shows.)
    /// </summary>
    public static string SpokenSince(IReadOnlyList<DialogLine> transcript, int from)
    {
        if (transcript == null)
            return string.Empty;

        var said = new List<string>();
        for (int i = from < 0 ? 0 : from; i < transcript.Count; i++)
        {
            DialogLine line = transcript[i];
            if (line != null && line.Speaker == DialogSpeaker.Traveller)
                said.Add(line.Text);
        }

        return string.Join("\\n", said);
    }

    /// <summary>
    /// The traveller's graph. Hub: "request:{i}" per document handed over on
    /// request (one-shot, hands document i over), then "ask" when the ask menu has a question or"""),
("""        IReadOnlyList<string> documents = c != null && c.documentNames != null ? c.documentNames : new string[0];
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

        ask.Choices.Add(new DialogChoice { Id = "back", Label = lines.backLabel, Next = HubNodeId });""",
"""        IReadOnlyList<CaseDocument> documents = c != null && c.documents != null ? c.documents : new CaseDocument[0];
        for (int i = 0; i < documents.Count; i++)
        {
            CaseDocument doc = documents[i];
            if (doc == null || !doc.Requested)
                continue;

            hub.Choices.Add(new DialogChoice
            {
                Id = $"request:{i}",
                Label = Interview.Fill(lines.requestLabel, Interview.DocumentToken, doc.name),
                Lines =
                {
                    new DialogLine(Id(lines.requestPrompt), DialogSpeaker.Desk, Interview.Fill(Text(lines.requestPrompt), Interview.DocumentToken, doc.name)),
                    new DialogLine(Id(lines.requestReply), DialogSpeaker.Traveller, Text(lines.requestReply))
                },
                Action = DialogAction.HandOverDocument,
                DocumentIndex = i,
                OneShot = true
            });
        }

        ask.Choices.Add(new DialogChoice { Id = "back", Label = lines.backLabel, Next = HubNodeId, Kind = DialogChoiceKind.Back });"""),
("""                problems.Add($"node '{node.id}' offers {Choices(node).Count} choices; the intercom shows at most {maxChoices}");""",
"""                problems.Add($"node '{node.id}' offers {Choices(node).Count} choices; the traveller wheel shows at most {maxChoices}");"""),
("""    /// <summary>
    /// Menus larger than the intercom shows: the ask menu (1 back + the
    /// questions + 1 when there is small talk) or the hub (the most documents
    /// of any traveller + 1 ask entry + every dialog, counted as offered at
    /// once). Skipped when <paramref name="maxChoices"/> is 0 or less.
    /// </summary>
    public static List<string> MenuProblems(int questions, bool smallTalk, int maxDocuments, int dialogs, int maxChoices)""",
"""    /// <summary>
    /// Menus larger than the traveller wheel shows: the ask menu (1 back + the
    /// questions + 1 when there is small talk) or the hub (the most documents
    /// one traveller hands over on request + 1 ask entry + every dialog,
    /// counted as offered at once). Skipped when <paramref name="maxChoices"/>
    /// is 0 or less.
    /// </summary>
    public static List<string> MenuProblems(int questions, bool smallTalk, int maxRequestedDocuments, int dialogs, int maxChoices)"""),
("""            problems.Add($"The ask menu holds {ask} choices (< Back, {questions} question(s){(smallTalk ? ", small talk" : string.Empty)}); the intercom shows at most {maxChoices}.");

        int hub = maxDocuments + 1 + dialogs;
        if (hub > maxChoices)
            problems.Add($"The hub holds {hub} choices ({maxDocuments} document request(s), the ask entry, {dialogs} dialog(s)); the intercom shows at most {maxChoices}.");""",
"""            problems.Add($"The ask menu holds {ask} choices (< Back, {questions} question(s){(smallTalk ? ", small talk" : string.Empty)}); the traveller wheel shows at most {maxChoices}.");

        int hub = maxRequestedDocuments + 1 + dialogs;
        if (hub > maxChoices)
            problems.Add($"The hub holds {hub} choices ({maxRequestedDocuments} document request(s), the ask entry, {dialogs} dialog(s)); the traveller wheel shows at most {maxChoices}.");"""),
])

apply(W + r'\Assets\Scripts\Domain\InterviewContent.cs', [
("""/// against at run time and by the content validator: the most choices the
/// intercom shows at once. (The longest line a transcript row holds is a""",
"""/// against at run time and by the content validator: the most choices the
/// traveller wheel shows at once. (The longest line a transcript row holds is a"""),
("""    /// <summary>The most choices the intercom shows at once (content never offers more).</summary>""",
"""    /// <summary>The most choices the traveller wheel shows at once (content never offers more).</summary>"""),
])

# --- The template knob ---
apply(W + r'\Assets\Scripts\DocumentTemplateSO.cs', [
("""    [Header("Investigation fields")]
    public DocumentFieldSpec[] fieldSpecs;""",
"""    [Header("Investigation fields")]
    public DocumentFieldSpec[] fieldSpecs;

    /// <summary>When the traveller hands this document over: when they step up (OnArrival) or when asked (OnRequest).</summary>
    [Header("Desk")]
    public DocumentHandOver handOver = DocumentHandOver.OnRequest;"""),
])

# --- The desk's no-desk path: requests hand documents over to their windows, with icons ---
apply(W + r'\Assets\Scripts\UI\InvestigationUIController.cs', [
("""    private Action<bool> _onDecision;
    private readonly List<GameObject> _docWindows = new();
    private readonly List<GameObject> _docIcons = new();""",
"""    private Action<bool> _onDecision;
    private readonly List<GameObject> _docWindows = new();
    private readonly List<GameObject> _docIcons = new();

    /// <summary>The current traveller's documents in paper order (name, holder, hand-over).</summary>
    private readonly List<CaseDocument> _caseDocuments = new();

    /// <summary>Papers whose window already has a desktop icon this case.</summary>
    private readonly HashSet<int> _iconedDocuments = new();"""),
("""        foreach (GameObject ic in _docIcons)
            if (ic != null)
                Destroy(ic);
        _docIcons.Clear();

        // Documents are handed over through the interview ("Request Travel
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

        StartInterview(inst, documentNames);""",
"""        foreach (GameObject ic in _docIcons)
            if (ic != null)
                Destroy(ic);
        _docIcons.Clear();
        _iconedDocuments.Clear();
        _caseDocuments.Clear();

        // Documents are handed over, never taken: those marked "on arrival" when
        // the traveller steps up, the others through an interview request. Each
        // window spawns hidden and opens when its document is handed over.
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
                _caseDocuments.Add(new CaseDocument
                {
                    name = doc != null && doc.template != null ? doc.template.displayName : "Document",
                    holder = inst.visitorGivenName,
                    handOver = doc != null && doc.template != null ? doc.template.handOver : DocumentHandOver.OnRequest
                });
                i++;
            }
        }

        foreach (int i in CaseDocuments.ArrivalIndices(_caseDocuments))
            OpenDocumentWindow(i);

        StartInterview(inst, _caseDocuments);"""),
("""    /// <summary>
    /// Starts the traveller's interview: the hub with a request per document,
    /// and, when the interview is reachable, today's questions, small talk and""",
"""    /// <summary>
    /// Starts the traveller's interview: the hub with a request per document
    /// handed over on request, and, when the interview is reachable, today's questions, small talk and"""),
("""    private void StartInterview(CaseInstance inst, IReadOnlyList<string> documentNames)""",
"""    private void StartInterview(CaseInstance inst, IReadOnlyList<CaseDocument> documents)"""),
("""            documentNames = documentNames,""",
"""            documents = documents,"""),
("""    /// <summary>
    /// Plays one interview choice: the transcript shows its lines; a document
    /// request opens and raises that document's window, any other choice opens
    /// the transcript; a finished dialog is recorded for the end of the shift.
    /// </summary>""",
"""    /// <summary>
    /// Plays one interview choice: the transcript shows its lines; a document
    /// request hands that document over (its window opens and is raised), any
    /// other choice opens the transcript; a finished dialog is recorded for the
    /// end of the shift.
    /// </summary>"""),
("""        if (choice.Action == DialogAction.OpenDocument)
        {
            GameObject window = choice.DocumentIndex >= 0 && choice.DocumentIndex < _docWindows.Count ? _docWindows[choice.DocumentIndex] : null;
            if (window != null)
            {
                window.SetActive(true);
                window.transform.SetAsLastSibling();
            }
        }
        else if (transcriptChrome != null)""",
"""        if (choice.Action == DialogAction.HandOverDocument)
            OpenDocumentWindow(choice.DocumentIndex);
        else if (transcriptChrome != null)"""),
("""    /// <summary>
    /// Closes every window on the window layer (templates are already
    /// inactive; per-case document clones are destroyed separately).
    /// </summary>""",
"""    /// <summary>
    /// Opens a paper's scanned window and raises it. The first time it opens
    /// this case, the paper also gets a desktop icon at the top of the grid,
    /// which reopens the window after it is closed.
    /// </summary>
    private void OpenDocumentWindow(int index)
    {
        GameObject window = index >= 0 && index < _docWindows.Count ? _docWindows[index] : null;
        if (window == null)
            return;

        window.SetActive(true);
        window.transform.SetAsLastSibling();
        if (_iconedDocuments.Add(index))
            AddDesktopIcon(_caseDocuments[index].name, window, true);
    }

    /// <summary>
    /// Closes every window on the window layer (templates are already
    /// inactive; per-case document clones are destroyed separately).
    /// </summary>"""),
("""    private void Decide(bool accepted)
    {
        Hide();
        Action<bool> cb = _onDecision;""",
"""    private void Decide(bool accepted)
    {
        Hide();

        // No case is on the desk from here: cleared before the callback, which
        // may present the next traveller at once (no READY sign wired).
        _currentCase = null;
        Action<bool> cb = _onDecision;"""),
])
```

Expected: `ok …Dialog.cs lf`, `ok …InterviewScript.cs lf`, `ok …InterviewContent.cs lf`, `ok …DocumentTemplateSO.cs crlf`, `ok …InvestigationUIController.cs crlf`.

- [ ] **Step 4: Compile check and run**

Expected: exit 0 / exit 0; `passed 433, failed 0` (425 + 8: the four `SpokenSince` tests, `ARequest_LeavesTheHubOnceChosen`, the two hand-over hub tests and `Back_IsTheOnlyChoiceOfKindBack`; the request test is renamed, not added).

- [ ] **Step 5: FEATURES** — save as `SCRATCH/p7_t08_features.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\docs\FEATURES.md', [
("""- [ ] Desktop icon grid top-left; reference book icons + app icons (documents come via the intercom, not icons)""",
"""- [ ] Desktop icon grid top-left; reference book icons + app icons (a document's window gets an icon at the top of the grid the first time it opens)"""),
("""- [ ] Every new case closes all open windows (pin system planned to override)""",
"""- [ ] Every new case closes all open windows (pin system planned to override) (scanned-document icons are cleared too)"""),
("""Hub: "Request <document>" (repeatable; the traveller hands it over and the window opens), "Ask about home >\"""",
"""Hub: "Request <document>" for each document handed over on request (one-shot; the traveller hands it over and its window opens), "Ask about home >\""""),
("""content never offers more choices than the intercom shows (8), and the way back and the requests come first (hub and menus tested: `InterviewScriptTests`, `DialogRunnerTests`;""",
"""content never offers more choices than the intercom shows (8), and the way back and the requests come first (hub, menus, one-shot requests and which documents get one tested: `InterviewScriptTests`, `DialogRunnerTests`;"""),
])
```

Expected: `ok …docs\FEATURES.md crlf`.

- [ ] **Step 6: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/Domain/Dialog.cs Assets/Scripts/Domain/InterviewScript.cs Assets/Scripts/Domain/InterviewContent.cs Assets/Scripts/DocumentTemplateSO.cs Assets/Scripts/UI/InvestigationUIController.cs Assets/Tests/EditMode/InterviewScriptTests.cs Assets/Tests/EditMode/InterviewDayTests.cs docs/FEATURES.md && git commit -F - <<'EOF'
feat(interview): documents are handed over, requests are one-shot

A request hands its document over (HandOverDocument) and leaves the hub;
only documents handed over on request get one, keeping the paper's index
(DocumentTemplateSO.handOver, OnRequest by default). "< Back" is the one
choice of kind Back; SpokenSince gives the traveller's reply to a choice;
menu checks count requested documents and name the traveller wheel.
Without a desk, documents go straight to their windows (arrivals at
presentation, requests when chosen), each with an icon the first time;
Decide clears the case before its callback.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 9: The passport is handed over on arrival; the checks count requested documents

Spec §2.13, §2.14, K5: the passport template's `handOver` becomes `OnArrival` (the traveller hands it over when stepping up, "documents submitted"); the permit keeps `OnRequest`. `ContentLibraryValidator.MaxRequestedDocuments` counts the documents a traveller hands over on request (`DocumentHandOvers.IsRequested`, Task 3, the predicate behind `CaseDocument.Requested`), and both the validator and Generate World check the hub's capacity with it; `MaxDocuments` (every paper, the desk's spawn slots in Task 17) is documented as such, and `TravellerBlueprints` becomes public so the builder counts the same blueprints (Task 16). Editor code and a data asset: there is no EditMode test (the counts are checked in Unity, Task 19: `MaxRequestedDocuments` 1, `MaxDocuments` 2).

**Files:**
- Modify: `Assets/Data/Investigation/DocTemplate_Passport.asset`, `Assets/Editor/ContentLibraryValidator.cs`, `Assets/Editor/WorldContentGenerator.cs`, `docs/FEATURES.md`

- [ ] **Step 1: Apply** — save as `SCRATCH/p7_t09.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

# The passport is handed over when the traveller steps up ("documents submitted");
# the permit keeps the default, on request.
apply(W + r'\Assets\Data\Investigation\DocTemplate_Passport.asset', [
("""  - category: 0
    label: Native Tongue
    page: 0
""",
"""  - category: 0
    label: Native Tongue
    page: 0
  handOver: 1
"""),
])

# The validator and Generate World count only documents handed over on request
# in the hub; the builder counts every paper (desk spawn slots) from the same blueprints.
apply(W + r'\Assets\Editor\ContentLibraryValidator.cs', [
("""    /// op that acts while active (and a warning for a permanent one with a
    /// briefing or news line); menus fuller than the intercom shows.""",
"""    /// op that acts while active (and a warning for a permanent one with a
    /// briefing or news line); menus fuller than the traveller wheel shows."""),
("""        foreach (string problem in DialogChecks.MenuProblems(lib.Questions.Count(q => q != null), smallTalk, MaxDocuments(TravellerBlueprints(lib)),""",
"""        foreach (string problem in DialogChecks.MenuProblems(lib.Questions.Count(q => q != null), smallTalk, MaxRequestedDocuments(TravellerBlueprints(lib)),"""),
("""    /// <summary>
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
    private static IEnumerable<CaseBlueprintSO> TravellerBlueprints(ContentLibrarySO lib)""",
"""    /// <summary>
    /// The most papers one traveller carries among these blueprints (null
    /// blueprints and templates are skipped). The office builder checks the
    /// desk's paper spawn slots against it.
    /// </summary>
    public static int MaxDocuments(IEnumerable<CaseBlueprintSO> blueprints) =>
        blueprints.Where(b => b != null && b.DocumentTemplates != null)
                  .Select(b => b.DocumentTemplates.Count(t => t != null))
                  .DefaultIfEmpty(0)
                  .Max();

    /// <summary>
    /// The most documents one traveller hands over on request among these
    /// blueprints (a hub request each; templates handed over on arrival, null
    /// blueprints and null templates are skipped). Generate World counts its
    /// source's blueprints with the same rule.
    /// </summary>
    public static int MaxRequestedDocuments(IEnumerable<CaseBlueprintSO> blueprints) =>
        blueprints.Where(b => b != null && b.DocumentTemplates != null)
                  .Select(b => b.DocumentTemplates.Count(t => t != null && DocumentHandOvers.IsRequested(t.handOver)))
                  .DefaultIfEmpty(0)
                  .Max();

    /// <summary>Every blueprint a traveller can come from: the day plans' possible and forced ones and the legendaries' overrides (nulls included). The office builder counts the same blueprints.</summary>
    public static IEnumerable<CaseBlueprintSO> TravellerBlueprints(ContentLibrarySO lib)"""),
])

apply(W + r'\Assets\Editor\WorldContentGenerator.cs', [
("""        // --- Menus: the intercom must show every choice ---""",
"""        // --- Menus: the traveller wheel must show every choice ---"""),
("""        foreach (string problem in DialogChecks.MenuProblems(questions.Length, anySmallTalk, ContentLibraryValidator.MaxDocuments(Blueprints(authored)), dialogs.Length, iv.menuCapacity))""",
"""        foreach (string problem in DialogChecks.MenuProblems(questions.Length, anySmallTalk, ContentLibraryValidator.MaxRequestedDocuments(Blueprints(authored)), dialogs.Length, iv.menuCapacity))"""),
])

apply(W + r'\docs\FEATURES.md', [
("""Hub: "Request <document>" for each document handed over on request (one-shot; the traveller hands it over and its window opens), "Ask about home >\"""",
"""Hub: "Request <document>" for each document handed over on request (one-shot; the traveller hands it over and its window opens; a document handed over on arrival, the passport, opens when the traveller is presented; `DocumentTemplateSO` hand-over), "Ask about home >\""""),
("""no line is longer than the transcript holds, no menu is fuller than the intercom shows and no dialog effect carries a timed modifier;""",
"""no line is longer than the transcript holds, no menu is fuller than the intercom shows (requests count only documents handed over on request) and no dialog effect carries a timed modifier;"""),
])
```

Expected: `ok …DocTemplate_Passport.asset crlf`, `ok …ContentLibraryValidator.cs crlf`, `ok …WorldContentGenerator.cs lf`, `ok …docs\FEATURES.md crlf`.

- [ ] **Step 2: Compile check and run**

Expected: exit 0 / exit 0; `passed 433, failed 0`.

- [ ] **Step 3: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Data/Investigation/DocTemplate_Passport.asset Assets/Editor/ContentLibraryValidator.cs Assets/Editor/WorldContentGenerator.cs docs/FEATURES.md && git commit -F - <<'EOF'
content(desk): the passport is handed over on arrival

The traveller hands the passport over when stepping up and the permit
when asked. The validator and Generate World size the hub with
MaxRequestedDocuments (documents handed over on request); MaxDocuments
counts every paper, and TravellerBlueprints is public for the builder.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 10: Windows on a World Space screen, the wheel's ring and overlay callouts

Spec §2.10, V6, R4, R5, R9, R33, R34: glue for the canvas re-architecture and the wheel's parts. `DraggableWindow` drags in its parent's space through the press camera (so it works on the World Space desktop, where the old division by the canvas scale was wrong) and keeps the window inside the screen with `RectClamp` (a window taller than the screen keeps its title bar visible); its unused `_canvas` goes. `InteractionPanelController` gains a centre slot (`InteractionAction.centre`, `centreSlot`: the wheel puts "< Back" there, Task 14) and deactivates each old button before destroying it, so the ring is never laid out with dying buttons. New: `RadialLayoutGroup` (lays its active children on the `RadialLayout` ellipse, skipping `ignoreLayout` children), `OverlayProjection` (`TryPlace`: an overlay element over a world point, clamped inside the canvas rect its caller resolved once with `CanvasRectOf`, so nothing is looked up per frame) and `OverlayCallout` (a timed, unclickable label that follows a transform: the speech bubble and the desk tooltip, R34). `HoverHighlighter` purges outlines of destroyed renderers whenever it creates one (per-case papers are destroyed at every decision, R9). Nothing uses the new pieces until Task 13; the committed scene keeps working (`centreSlot` is optional).

**Files:**
- Modify (whole file): `Assets/Scripts/UI/DraggableWindow.cs`, `Assets/Scripts/UI/InteractionPanelController.cs`
- Modify: `Assets/Scripts/UI/HoverHighlighter.cs`, `docs/FEATURES.md`
- Create: `Assets/Scripts/UI/RadialLayoutGroup.cs`, `Assets/Scripts/UI/OverlayProjection.cs`, `Assets/Scripts/UI/OverlayCallout.cs` (+ `.meta`s)

- [ ] **Step 1: Stage the new `DraggableWindow`** — write `SCRATCH/p7stage/DraggableWindow.cs` (Write tool; it replaces `Assets/Scripts/UI/DraggableWindow.cs` in Step 5):

```csharp
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Makes a UI window draggable by a header bar, brings it to the front when
/// touched, and supports stow/restore. Attach to the HEADER object (which needs
/// a raycast-target Graphic, e.g. an Image) and point <see cref="windowRoot"/>
/// at the window panel that should move. The pointer is read in the window's
/// parent space (RectTransformUtility with the press camera), so a drag is
/// right on an overlay canvas and on the CRT's world-space desktop alike, and
/// the window is kept inside its parent while dragged (one taller than the
/// parent keeps its title bar visible). Investigation documents and reference
/// books use this so the player can arrange papers like a Papers, Please booth.
/// </summary>
public sealed class DraggableWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
{
    /// <summary>The panel that actually moves / is brought to front.</summary>
    [SerializeField] private RectTransform windowRoot;

    private RectTransform _self;
    private Vector2 _pointerStart;
    private Vector2 _windowStart;

    /// <summary>The moved panel (defaults to this object's parent).</summary>
    public RectTransform WindowRoot => windowRoot;

    private void Awake()
    {
        _self = (RectTransform)transform;

        if (windowRoot == null)
            windowRoot = _self.parent as RectTransform ?? _self;
    }

    /// <summary>Brings the window above its siblings.</summary>
    public void BringToFront()
    {
        if (windowRoot != null)
            windowRoot.SetAsLastSibling();
    }

    /// <summary>A press anywhere on the header brings the window to the front.</summary>
    public void OnPointerDown(PointerEventData eventData) => BringToFront();

    /// <summary>Starts a drag: remembers where the window and the pointer were.</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        BringToFront();

        if (windowRoot == null)
            return;

        _windowStart = windowRoot.anchoredPosition;
        TryPointer(eventData, out _pointerStart);
    }

    /// <summary>Moves the window with the pointer, kept inside its parent.</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (windowRoot == null || !TryPointer(eventData, out Vector2 pointer))
            return;

        windowRoot.anchoredPosition = _windowStart + (pointer - _pointerStart);
        KeepInsideParent();
    }

    /// <summary>Shows the window and brings it forward.</summary>
    public void Open()
    {
        if (windowRoot == null)
            return;

        windowRoot.gameObject.SetActive(true);
        BringToFront();
    }

    /// <summary>Hides the window (stow to the shelf/tray).</summary>
    public void Stow()
    {
        if (windowRoot != null)
            windowRoot.gameObject.SetActive(false);
    }

    /// <summary>Toggles between open and stowed.</summary>
    public void Toggle()
    {
        if (windowRoot == null)
            return;

        if (windowRoot.gameObject.activeSelf)
            Stow();
        else
            Open();
    }

    /// <summary>The pointer in the window's parent space (the press camera: null on an overlay canvas, the world camera on the CRT).</summary>
    private bool TryPointer(PointerEventData eventData, out Vector2 local)
    {
        local = Vector2.zero;
        return windowRoot.parent is RectTransform parent &&
               RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out local);
    }

    /// <summary>Moves the window back inside its parent's rect, per axis (RectClamp): a window taller than the parent keeps its top, the title bar, inside.</summary>
    private void KeepInsideParent()
    {
        if (!(windowRoot.parent is RectTransform parent))
            return;

        Rect bounds = parent.rect;
        Rect own = windowRoot.rect;
        Vector3 at = windowRoot.localPosition;
        float dx = RectClamp.Shift(at.x + own.xMin, at.x + own.xMax, bounds.xMin, bounds.xMax, false);
        float dy = RectClamp.Shift(at.y + own.yMin, at.y + own.yMax, bounds.yMin, bounds.yMax, true);
        windowRoot.anchoredPosition += new Vector2(dx, dy);
    }
}
```

- [ ] **Step 2: Stage the new `InteractionPanelController`** — write `SCRATCH/p7stage/InteractionPanelController.cs` (Write tool; it replaces `Assets/Scripts/UI/InteractionPanelController.cs` in Step 5):

```csharp
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One action the player can issue to the traveller ("Request Passport").</summary>
public struct InteractionAction
{
    /// <summary>Button label.</summary>
    public string label;

    /// <summary>Shown in the panel's centre slot when it has one (the wheel's "&lt; Back"); otherwise listed like any action.</summary>
    public bool centre;

    /// <summary>Invoked when the player issues the action.</summary>
    public Action execute;
}

/// <summary>
/// A list of the interview's current choices: the traveller wheel's ring
/// (laid out by RadialLayoutGroup), or a plain vertical list (the hybrid
/// scene's intercom). Actions are the current interview node's choices
/// (requests, questions, dialog replies), supplied per step by the
/// investigation controller; the panel only renders buttons.
/// </summary>
public sealed class InteractionPanelController : MonoBehaviour
{
    /// <summary>Container the action buttons are spawned under.</summary>
    [SerializeField] private Transform actionsRoot;

    /// <summary>Disabled template button cloned per action.</summary>
    [SerializeField] private Button actionButtonTemplate;

    /// <summary>Optional: where centre actions go (the traveller wheel); none keeps every action in the list.</summary>
    [SerializeField] private Transform centreSlot;

    private readonly List<GameObject> _spawned = new();

    private void Awake()
    {
        if (actionButtonTemplate != null)
            actionButtonTemplate.gameObject.SetActive(false);
    }

    /// <summary>Replaces the visible actions with the given list.</summary>
    public void SetActions(IReadOnlyList<InteractionAction> actions)
    {
        Clear();

        if (actions == null || actionsRoot == null || actionButtonTemplate == null)
            return;

        foreach (InteractionAction action in actions)
        {
            Transform parent = action.centre && centreSlot != null ? centreSlot : actionsRoot;
            Button btn = Instantiate(actionButtonTemplate, parent);
            btn.gameObject.SetActive(true);
            _spawned.Add(btn.gameObject);

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = action.label;

            Action execute = action.execute;
            btn.onClick.AddListener(() => execute?.Invoke());
        }
    }

    /// <summary>
    /// Removes all spawned action buttons. Each is deactivated first: Destroy
    /// waits for the end of the frame, and a layout group must never count a
    /// dying button with the new ones.
    /// </summary>
    public void Clear()
    {
        foreach (GameObject go in _spawned)
        {
            if (go == null)
                continue;

            go.SetActive(false);
            Destroy(go);
        }
        _spawned.Clear();
    }
}
```

- [ ] **Step 3: The ring layout** — write `Assets/Scripts/UI/RadialLayoutGroup.cs` (Write tool):

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lays its active children out on an ellipse (RadialLayout.Point: item 0 at
/// the top, then clockwise), each at <see cref="ItemSize"/>, centred on this
/// rect's pivot. A child with LayoutElement.ignoreLayout (the traveller wheel's
/// centre slot) is left alone. Reports no preferred size.
/// </summary>
public sealed class RadialLayoutGroup : LayoutGroup
{
    /// <summary>The ellipse's horizontal and vertical radii (reference px).</summary>
    [SerializeField] private Vector2 radii = new Vector2(300f, 200f);

    /// <summary>Every item's size (reference px).</summary>
    [SerializeField] private Vector2 itemSize = new Vector2(240f, 44f);

    /// <summary>The ellipse's radii; setting it re-lays the ring.</summary>
    public Vector2 Radii
    {
        get => radii;
        set
        {
            radii = value;
            SetDirty();
        }
    }

    /// <summary>Every item's size; setting it re-lays the ring.</summary>
    public Vector2 ItemSize
    {
        get => itemSize;
        set
        {
            itemSize = value;
            SetDirty();
        }
    }

    /// <summary>Collects the laid-out children (the base class) and reports no preferred width.</summary>
    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        SetLayoutInputForAxis(0f, 0f, -1f, 0);
    }

    /// <summary>Reports no preferred height.</summary>
    public override void CalculateLayoutInputVertical() => SetLayoutInputForAxis(0f, 0f, -1f, 1);

    /// <summary>Places the items (both axes at once).</summary>
    public override void SetLayoutHorizontal() => Place();

    /// <summary>Places the items (both axes at once).</summary>
    public override void SetLayoutVertical() => Place();

    /// <summary>Puts child i of n at RadialLayout.Point(i, n) around the pivot, at the item size.</summary>
    private void Place()
    {
        int n = rectChildren.Count;
        var middle = new Vector2(0.5f, 0.5f);
        for (int i = 0; i < n; i++)
        {
            RectTransform child = rectChildren[i];
            m_Tracker.Add(this, child, DrivenTransformProperties.Anchors | DrivenTransformProperties.Pivot |
                                       DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.SizeDelta);
            (float x, float y) = RadialLayout.Point(i, n, radii.x, radii.y);
            child.anchorMin = middle;
            child.anchorMax = middle;
            child.pivot = middle;
            child.sizeDelta = itemSize;
            child.anchoredPosition = new Vector2(x, y);
        }
    }
}
```

- [ ] **Step 4: Overlay placement and the callout** — write `Assets/Scripts/UI/OverlayProjection.cs` (Write tool):

```csharp
using UnityEngine;

/// <summary>
/// Places an element of a screen-space overlay canvas over a world point: the
/// point through the given camera to the screen, then into the canvas, plus an
/// offset (canvas reference px), clamped inside the canvas. The target needs
/// anchors and pivot (0.5, 0.5) under a full-screen parent (the builder sets
/// both). Used by the traveller wheel and the overlay callouts, which resolve
/// their canvas once (CanvasRectOf) and their camera at Awake, so the per-frame
/// placement looks nothing up.
/// </summary>
public static class OverlayProjection
{
    /// <summary>The rect of the root canvas above <paramref name="host"/> (resolved once, at a caller's Awake), or null when it has none.</summary>
    public static RectTransform CanvasRectOf(Component host)
    {
        Canvas canvas = host != null ? host.GetComponentInParent<Canvas>() : null;
        return canvas != null ? (RectTransform)canvas.rootCanvas.transform : null;
    }

    /// <summary>Places <paramref name="target"/> inside <paramref name="canvasRect"/> (its root canvas's rect); false (the target unmoved) when a reference is missing or the point is behind the camera or outside the viewport.</summary>
    public static bool TryPlace(RectTransform target, RectTransform canvasRect, Camera camera, Vector3 world, Vector2 offset)
    {
        if (target == null || canvasRect == null || camera == null)
            return false;

        Vector3 viewport = camera.WorldToViewportPoint(world);
        if (viewport.z < 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
            return false;

        Vector2 screen = camera.ViewportToScreenPoint(viewport);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local))
            return false;

        Vector2 at = local + offset;
        Rect bounds = canvasRect.rect;
        Vector2 half = target.rect.size * 0.5f;
        at.x += RectClamp.Shift(at.x - half.x, at.x + half.x, bounds.xMin, bounds.xMax, false);
        at.y += RectClamp.Shift(at.y - half.y, at.y + half.y, bounds.yMin, bounds.yMax, false);
        target.anchoredPosition = at;
        return true;
    }
}
```

and `Assets/Scripts/UI/OverlayCallout.cs` (Write tool):

```csharp
using TMPro;
using UnityEngine;

/// <summary>
/// A timed label on the office overlay canvas that takes no clicks, projected
/// at a followed transform plus an offset (OverlayProjection): the traveller's
/// speech bubble (TravellerWheel.Say) and the desk props' tooltips
/// (DeskReaction). The host stays active; its Panel child is shown and hidden.
/// It hides when its time is up, when the followed object is destroyed, or when
/// that object leaves the view. Callers apply DisplayText.
/// </summary>
public sealed class OverlayCallout : MonoBehaviour
{
    /// <summary>The shown box (anchors and pivot (0.5, 0.5) under this full-screen host; raycast targets off).</summary>
    [SerializeField] private RectTransform panel;

    /// <summary>The box's text.</summary>
    [SerializeField] private TMP_Text label;

    private Camera _camera;
    private RectTransform _canvasRect;
    private Transform _follow;
    private Vector2 _offset;
    private float _remaining;

    /// <summary>Resolves the camera and the overlay canvas once, and hides the box (never this host).</summary>
    private void Awake()
    {
        _camera = Camera.main;
        _canvasRect = OverlayProjection.CanvasRectOf(this);
        Hide();
    }

    /// <summary>Shows <paramref name="text"/> (as given) over <paramref name="follow"/> plus <paramref name="offset"/> for <paramref name="seconds"/>, restarting the timer; a blank text or a null follow hides it.</summary>
    public void Show(string text, Transform follow, Vector2 offset, float seconds)
    {
        if (string.IsNullOrWhiteSpace(text) || follow == null || panel == null)
        {
            Hide();
            return;
        }

        if (label != null)
            label.text = text;
        _follow = follow;
        _offset = offset;
        _remaining = seconds;
        panel.gameObject.SetActive(true);
        if (!OverlayProjection.TryPlace(panel, _canvasRect, _camera, follow.position, offset))
            Hide();
    }

    /// <summary>Hides the box.</summary>
    public void Hide()
    {
        _follow = null;
        if (panel != null)
            panel.gameObject.SetActive(false);
    }

    /// <summary>Only while shown: counts down and follows the object.</summary>
    private void LateUpdate()
    {
        if (panel == null || !panel.gameObject.activeSelf)
            return;

        _remaining -= Time.deltaTime;
        if (_remaining <= 0f || _follow == null || !OverlayProjection.TryPlace(panel, _canvasRect, _camera, _follow.position, _offset))
            Hide();
    }
}
```

- [ ] **Step 5: Replace the two files, purge dead outlines, FEATURES** — save as `SCRATCH/p7_t10.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
from rewrite import rewrite
W = r'E:\unity\NOPE-feat-clock'
STAGE = S + r'\p7stage'

def staged(name):
    return open(STAGE + '\\' + name, encoding='utf-8').read()

# Whole-file rewrites (both files are CRLF; rewrite keeps that).
rewrite(W + r'\Assets\Scripts\UI\DraggableWindow.cs', 'f7aed008a90c', staged('DraggableWindow.cs'))
rewrite(W + r'\Assets\Scripts\UI\InteractionPanelController.cs', '7b2bdfc6cd07', staged('InteractionPanelController.cs'))

# Per-case papers are destroyed at every decision: their outlines (and generated
# textures) are purged whenever a new outline is created, not only when a scene unloads (R9).
apply(W + r'\Assets\Scripts\UI\HoverHighlighter.cs', [
("""        if (!_worldOutlines.TryGetValue(sr, out WorldOutline o))
        {
            o = new WorldOutline();
            _worldOutlines[sr] = o;
        }""",
"""        if (!_worldOutlines.TryGetValue(sr, out WorldOutline o))
        {
            PurgeDeadOutlines();
            o = new WorldOutline();
            _worldOutlines[sr] = o;
        }"""),
("""    /// <summary>Drops outlines whose sprites went away with an unloaded scene.</summary>
    private void HandleSceneUnloaded(Scene scene)
    {
        var dead = new List<SpriteRenderer>();
        foreach (KeyValuePair<SpriteRenderer, WorldOutline> pair in _worldOutlines)
        {
            if (pair.Key == null)
                dead.Add(pair.Key);
        }

        foreach (SpriteRenderer key in dead)
        {
            DestroyOutlineSprite(_worldOutlines[key]);
            _worldOutlines.Remove(key);
        }

        _lastHitObject = null;""",
"""    /// <summary>Drops outlines whose sprites went away with an unloaded scene.</summary>
    private void HandleSceneUnloaded(Scene scene)
    {
        PurgeDeadOutlines();

        _lastHitObject = null;"""),
("""    /// <summary>Destroys a generated outline sprite and its texture.</summary>""",
"""    /// <summary>
    /// Drops the outlines of destroyed renderers (an unloaded scene's sprites,
    /// the papers of a finished case) with their generated sprites and textures.
    /// Runs when a scene unloads and before a new outline is created, so the
    /// cache holds at most the last case's dead papers.
    /// </summary>
    private void PurgeDeadOutlines()
    {
        var dead = new List<SpriteRenderer>();
        foreach (KeyValuePair<SpriteRenderer, WorldOutline> pair in _worldOutlines)
        {
            if (pair.Key == null)
                dead.Add(pair.Key);
        }

        foreach (SpriteRenderer key in dead)
        {
            DestroyOutlineSprite(_worldOutlines[key]);
            _worldOutlines.Remove(key);
        }
    }

    /// <summary>Destroys a generated outline sprite and its texture.</summary>"""),
])

apply(W + r'\docs\FEATURES.md', [
("""- [ ] Draggable windows; min/max/close chrome on ALL windows (app, document, reference)""",
"""- [ ] Draggable windows; min/max/close chrome on ALL windows (app, document, reference); windows stay inside the screen while dragged (a window taller than the screen keeps its title bar visible) (clamp tested: `DeskGeometryTests`)"""),
])
```

Expected: `rewrote …DraggableWindow.cs crlf`, `rewrote …InteractionPanelController.cs crlf`, `ok …HoverHighlighter.cs lf`, `ok …docs\FEATURES.md crlf`.

- [ ] **Step 6: Compile check and run**

Expected: exit 0 / exit 0; `passed 433, failed 0`.

- [ ] **Step 7: Commit**

```bash
cd /e/unity/NOPE-feat-clock && python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/make_meta.py" Assets/Scripts/UI/RadialLayoutGroup.cs Assets/Scripts/UI/OverlayProjection.cs Assets/Scripts/UI/OverlayCallout.cs && git add Assets/Scripts/UI/DraggableWindow.cs Assets/Scripts/UI/InteractionPanelController.cs Assets/Scripts/UI/HoverHighlighter.cs Assets/Scripts/UI/RadialLayoutGroup.cs Assets/Scripts/UI/RadialLayoutGroup.cs.meta Assets/Scripts/UI/OverlayProjection.cs Assets/Scripts/UI/OverlayProjection.cs.meta Assets/Scripts/UI/OverlayCallout.cs Assets/Scripts/UI/OverlayCallout.cs.meta docs/FEATURES.md && git commit -F - <<'EOF'
feat(ui): windows clamp on any canvas; the ring layout and overlay callouts

DraggableWindow drags in its parent's space through the press camera and
stays inside the screen (a window taller than it keeps its title bar).
InteractionPanelController gains a centre slot and deactivates old buttons
before destroying them. New: RadialLayoutGroup (the wheel's ring),
OverlayProjection and OverlayCallout (the speech bubble and the desk
tooltip). HoverHighlighter purges outlines of destroyed renderers.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 11: The live screen, its power and the settled push-in

Spec §1.2, §1.3, §2.7, §2.8, K1, K2, K7, K8, K15, R11, R12, R13, R14, R15, R35: `DeskConfigSO` holds every desk knob (framing, power, scanner, papers, sorting bands, the wheel and its bubble, the day-1 notes; created by the builder in Task 15). `MonitorScreen` sits on `CRTMonitor`, with its `ScreenAnchor` child as the glass: it owns a `PcScreen` (toggle, wake, hold), enables the World Space desktop canvas and tints the bezel LED, and knows the glass's centre and size (scene data, R13). `ICameraRig` gains `IsSettled`; `CinemachineCameraRig` uses a serialized `brain` (else a lookup in `Start` with one warning, R35), applies the configured blend, frames the monitor with `MonitorFraming` from the glass, and is settled when no blend runs. `OfficeViewController` gains `monitorScreen`, `IsSettled` and a `Settled` event (a settle poll runs in either view, then the focus-only Escape check); the desktop's `SetActive` toggling survives only where no `MonitorScreen` is wired (the hybrid scene's legacy path, R4); the unused `InitForTest`/`Toggle` go. `DesktopShell`'s saved Power entry becomes `quitButton` (`[FormerlySerializedAs("powerButton")]`, so the hybrid scene still quits) and a new `screenOffButton` calls `TurnOffScreen`. Every new reference is optional, so the committed scene still plays.

**Files:**
- Modify (whole file): `Assets/Scripts/Office/ICameraRig.cs`, `Assets/Scripts/Office/CinemachineCameraRig.cs`, `Assets/Scripts/Office/OfficeViewController.cs`, `Assets/Scripts/UI/DesktopShell.cs`
- Create: `Assets/Scripts/Office/DeskConfigSO.cs`, `Assets/Scripts/Office/MonitorScreen.cs` (+ `.meta`s)

- [ ] **Step 1: The knobs** — write `Assets/Scripts/Office/DeskConfigSO.cs` (Write tool):

```csharp
using UnityEngine;

/// <summary>
/// Tuning for the physical desk (piece 7): the monitor push-in, screen power,
/// the scanner, the papers, the sorting bands, the traveller wheel and its
/// reply bubble, and the day-1 desk notes. Geometry that belongs to the art
/// (the glass rectangle, the desk rectangle, the scanner's drop area, the
/// anchors) stays in scene components, so another office supplies its own.
/// Created and assigned by Tools > TimeDesk > Build Office UI
/// (Assets/Data/Config/Desk_Default.asset).
/// </summary>
[CreateAssetMenu(fileName = "Desk_Default", menuName = "TimeDesk/Office/Desk Config")]
public sealed class DeskConfigSO : ScriptableObject
{
    [Header("Monitor focus")]
    /// <summary>How much of the view's height (or width, on narrow screens) the CRT's glass fills when focused.</summary>
    [Range(0.5f, 1f)] public float monitorFill = 0.85f;

    /// <summary>Seconds the camera takes to push in or pull back (the brain's default blend).</summary>
    [Min(0f)] public float focusBlendSeconds = 0.6f;

    [Header("Screen power")]
    /// <summary>True when the screen is on at the start of a shift.</summary>
    public bool screenStartsOn = true;

    /// <summary>Which events turn a dark screen on.</summary>
    public PcWakeRules wake = new PcWakeRules();

    /// <summary>The bezel LED's colour while the screen is on.</summary>
    public Color ledOnColor = new Color(0.35f, 0.95f, 0.45f, 1f);

    /// <summary>The bezel LED's colour while the screen is off.</summary>
    public Color ledOffColor = new Color(0.15f, 0.17f, 0.15f, 1f);

    [Header("Scanner")]
    /// <summary>Seconds a desk scan takes (the shift clock keeps running).</summary>
    [Min(0.1f)] public float scanSeconds = 1.5f;

    /// <summary>The day-1 note on the scanner tray (ASCII; empty = no note).</summary>
    public string scanHint = "Drag papers onto the scanner to read them on the PC.";

    /// <summary>The last day the scanner note shows (0 = never).</summary>
    [Min(0)] public int scanHintUntilDay = 1;

    [Header("Papers")]
    /// <summary>Where handed-over papers land, 0..1 across the desk rectangle (reused in order when a traveller has more papers).</summary>
    public Vector2[] paperSpawnSlots =
    {
        new Vector2(0.58f, 0.71f), new Vector2(0.76f, 0.66f), new Vector2(0.62f, 0.29f), new Vector2(0.84f, 0.26f)
    };

    /// <summary>Seconds a paper takes to slide (hand-over, back from the scanner, away at the decision).</summary>
    [Min(0f)] public float paperSlideSeconds = 0.25f;

    [Header("Sorting bands (Default layer)")]
    /// <summary>The focus exit zone's order: above every desk prop.</summary>
    public int focusExitOrder = 10;

    /// <summary>The glass zone's order: above the exit zone, below the bezel.</summary>
    public int glassOrder = 11;

    /// <summary>The CRT bezel's power button and LED.</summary>
    public int bezelOrder = 12;

    /// <summary>The desktop canvas on the glass.</summary>
    public int screenCanvasOrder = 20;

    /// <summary>The bottom paper of the stack (each paper above adds 1).</summary>
    public int paperBaseOrder = 30;

    /// <summary>The paper being dragged: above every stacked paper.</summary>
    public int heldPaperOrder = 60;

    [Header("Traveller wheel (overlay reference px)")]
    /// <summary>The ring's horizontal and vertical radii.</summary>
    public Vector2 wheelRadii = new Vector2(300f, 200f);

    /// <summary>Every ring item's size.</summary>
    public Vector2 wheelItemSize = new Vector2(240f, 44f);

    /// <summary>The centre slot's size ("&lt; Back").</summary>
    public Vector2 wheelCentreSize = new Vector2(150f, 44f);

    /// <summary>The least gap between two ring items, or an item and the centre (the builder's fit check).</summary>
    [Min(0f)] public float wheelItemGap = 8f;

    /// <summary>The day-1 note above the traveller (ASCII; empty = no note).</summary>
    public string wheelHint = "Click the traveller to talk and ask for papers.";

    /// <summary>The last day the wheel note shows (0 = never).</summary>
    [Min(0)] public int wheelHintUntilDay = 1;

    [Header("Reply bubble")]
    /// <summary>Seconds the traveller's reply stays up.</summary>
    [Min(0.1f)] public float bubbleSeconds = 4f;

    /// <summary>Where the bubble sits from the traveller's anchor (overlay reference px).</summary>
    public Vector2 bubbleOffset = new Vector2(650f, 100f);
}
```

- [ ] **Step 2: The live screen** — write `Assets/Scripts/Office/MonitorScreen.cs` (Write tool):

```csharp
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The PC's screen on the CRT: the desktop canvas drawn live on the glass, its
/// power (PcScreen: the display and the desktop's input go dark, the PC keeps
/// running) and the bezel LED. The glass rectangle is scene data, so another
/// monitor only changes numbers. BoothCoordinator gates the desktop's input
/// (SetInteractive) and holds the screen on while a citation slip waits.
/// </summary>
public sealed class MonitorScreen : MonoBehaviour
{
    /// <summary>The World Space desktop canvas on the glass.</summary>
    [SerializeField] private Canvas desktopCanvas;

    /// <summary>The desktop canvas's raycaster (off unless the desktop may take input).</summary>
    [SerializeField] private GraphicRaycaster desktopRaycaster;

    /// <summary>The ScreenAnchor at the glass's centre; its local XY is the screen plane.</summary>
    [SerializeField] private Transform glass;

    /// <summary>The glass rectangle in the anchor's local units (the desktop canvas fills it).</summary>
    [SerializeField] private Vector2 glassSize;

    /// <summary>The bezel LED, tinted with the power state.</summary>
    [SerializeField] private SpriteRenderer powerLed;

    /// <summary>The desk tuning (the start state, the wake rules, the LED colours).</summary>
    [SerializeField] private DeskConfigSO config;

    private PcScreen _screen;

    /// <summary>True while the screen is on.</summary>
    public bool IsOn => _screen.IsOn;

    /// <summary>Raised after the screen turns on or off.</summary>
    public event Action PowerChanged;

    /// <summary>The glass's centre in world space.</summary>
    public Vector3 GlassCentre => glass != null ? glass.position : transform.position;

    /// <summary>The glass rectangle's size in world units.</summary>
    public Vector2 GlassWorldSize
    {
        get
        {
            Vector3 scale = glass != null ? glass.lossyScale : transform.lossyScale;
            return new Vector2(glassSize.x * Mathf.Abs(scale.x), glassSize.y * Mathf.Abs(scale.y));
        }
    }

    private void Awake()
    {
        if (config != null)
        {
            _screen = new PcScreen(config.screenStartsOn, config.wake);
        }
        else
        {
            _screen = new PcScreen(true, null);
            Debug.LogWarning("[MonitorScreen] No DeskConfigSO wired: the screen starts on and never wakes itself. Run Tools > TimeDesk > Build Office UI.", this);
        }

        _screen.Changed += HandleChanged;
        Apply();
        SetInteractive(false);
    }

    private void OnDestroy()
    {
        _screen.Changed -= HandleChanged;
    }

    /// <summary>Turns the screen on or off (the bezel button's persistent call); a held screen stays on.</summary>
    public void TogglePower() => _screen.Toggle();

    /// <summary>Turns the screen off (Start > Turn off screen); a held screen stays on.</summary>
    public void TurnOff() => _screen.TurnOff();

    /// <summary>Turns a dark screen on when the reason's wake rule is on.</summary>
    public void Wake(WakeReason reason) => _screen.Wake(reason);

    /// <summary>Holds the screen on (a pending citation slip) or releases it.</summary>
    public void SetHeld(bool held) => _screen.SetHeld(held);

    /// <summary>
    /// Lets the desktop take input or not (its raycaster). Turning it off also
    /// clears the EventSystem's selection when it is on the desktop, so a focused
    /// field (the Records search) stops taking keys.
    /// </summary>
    public void SetInteractive(bool on)
    {
        if (desktopRaycaster != null)
            desktopRaycaster.enabled = on;

        if (on || desktopCanvas == null)
            return;

        EventSystem events = EventSystem.current;
        GameObject selected = events != null ? events.currentSelectedGameObject : null;
        if (selected != null && selected.transform.IsChildOf(desktopCanvas.transform))
            events.SetSelectedGameObject(null);
    }

    private void HandleChanged()
    {
        Apply();
        PowerChanged?.Invoke();
    }

    /// <summary>Shows or hides the desktop (the painted glass shows when off) and tints the LED.</summary>
    private void Apply()
    {
        bool on = _screen.IsOn;
        if (desktopCanvas != null)
            desktopCanvas.enabled = on;
        if (powerLed != null && config != null)
            powerLed.color = on ? config.ledOnColor : config.ledOffColor;
    }
}
```

- [ ] **Step 3: Stage the new `ICameraRig`** — write `SCRATCH/p7stage/ICameraRig.cs` (Write tool; it replaces `Assets/Scripts/Office/ICameraRig.cs` in Step 7):

```csharp
/// <summary>
/// Abstraction over the camera so view-state logic is decoupled from
/// Cinemachine. Implemented for real by CinemachineCameraRig.
/// </summary>
public interface ICameraRig
{
    /// <summary>Frames the wide office/booth view.</summary>
    void ShowOffice();

    /// <summary>Frames the close-up monitor view.</summary>
    void ShowMonitor();

    /// <summary>True when the rig shows that view's camera and no blend is running.</summary>
    bool IsSettled(OfficeView view);
}
```

- [ ] **Step 4: Stage the new `CinemachineCameraRig`** — write `SCRATCH/p7stage/CinemachineCameraRig.cs` (Write tool; it replaces `Assets/Scripts/Office/CinemachineCameraRig.cs`):

```csharp
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Real <see cref="ICameraRig"/> backed by two Cinemachine cameras. Raises the
/// active camera's priority so the CinemachineBrain blends the push-in. With a
/// live monitor and the desk tuning wired, an orthographic monitor camera is
/// framed on the CRT's glass (MonitorFraming, the fill knob) and the blend
/// takes the knob's time. Every new reference is optional: without a brain
/// (the hybrid scene does not wire one) it is looked up in Start, and a rig
/// with no brain counts as settled at once so the booth never waits for it.
/// </summary>
public sealed class CinemachineCameraRig : MonoBehaviour, ICameraRig
{
    /// <summary>Wide booth camera.</summary>
    [SerializeField] private CinemachineCamera officeCam;

    /// <summary>Close-up monitor camera.</summary>
    [SerializeField] private CinemachineCamera monitorCam;

    /// <summary>The brain on the camera these cameras drive; set by the builder. Unset (the hybrid scene), it is looked up in Start.</summary>
    [SerializeField] private CinemachineBrain brain;

    /// <summary>Optional: the live monitor, whose glass the monitor camera frames.</summary>
    [SerializeField] private MonitorScreen monitorScreen;

    /// <summary>Optional: the desk tuning (the glass's fill of the view, the blend time).</summary>
    [SerializeField] private DeskConfigSO config;

    private const int Active = 20;
    private const int Idle = 10;

    /// <summary>After every OnEnable (brains register there): finds the brain when unset, and sets its blend from the knob.</summary>
    private void Start()
    {
        if (brain == null)
        {
            brain = CinemachineCore.FindPotentialTargetBrain(officeCam);
            if (brain == null)
                Debug.LogWarning("[CinemachineCameraRig] No CinemachineBrain drives the office cameras: views count as settled at once. Run Tools > TimeDesk > Build Office UI.", this);
        }

        if (brain != null && config != null)
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, config.focusBlendSeconds);
    }

    /// <summary>Raises the office camera above the monitor camera.</summary>
    public void ShowOffice()
    {
        if (officeCam != null) officeCam.Priority = Active;
        if (monitorCam != null) monitorCam.Priority = Idle;
    }

    /// <summary>Frames the glass (orthographic cameras with a live monitor), then raises the monitor camera above the office camera.</summary>
    public void ShowMonitor()
    {
        FrameMonitor();
        if (officeCam != null) officeCam.Priority = Idle;
        if (monitorCam != null) monitorCam.Priority = Active;
    }

    /// <summary>True when the brain shows that view's camera with no blend running; true with no brain (the camera never moves, so nothing waits for it).</summary>
    public bool IsSettled(OfficeView view)
    {
        if (brain == null)
            return true;

        CinemachineCamera cam = view == OfficeView.MonitorFocus ? monitorCam : officeCam;
        return !brain.IsBlending && brain.ActiveVirtualCamera == (ICinemachineCamera)cam;
    }

    /// <summary>
    /// Centres the monitor camera on the glass (keeping its z) and sizes it so
    /// the glass fills the knob's fraction of the view at the output camera's
    /// aspect. Only for an orthographic output camera with the brain, the
    /// monitor screen and the config wired: a perspective camera (the hybrid
    /// scene) is left alone. (The camera's own flag is read, not
    /// Lens.Orthographic, which Cinemachine fills only when it pulls the camera state.)
    /// </summary>
    private void FrameMonitor()
    {
        if (monitorCam == null || brain == null || brain.OutputCamera == null || monitorScreen == null || config == null ||
            !brain.OutputCamera.orthographic)
            return;

        Vector3 glass = monitorScreen.GlassCentre;
        Transform t = monitorCam.transform;
        t.position = new Vector3(glass.x, glass.y, t.position.z);

        Vector2 size = monitorScreen.GlassWorldSize;
        monitorCam.Lens.OrthographicSize = MonitorFraming.OrthoSize(size.x, size.y, brain.OutputCamera.aspect, config.monitorFill);
    }
}
```

- [ ] **Step 5: Stage the new `OfficeViewController`** — write `SCRATCH/p7stage/OfficeViewController.cs` (Write tool; it replaces `Assets/Scripts/Office/OfficeViewController.cs`):

```csharp
using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>The two camera states of the office scene.</summary>
public enum OfficeView
{
    /// <summary>Wide booth view from the agent's POV (default).</summary>
    OfficeFocus,

    /// <summary>Close-up on the CRT; the desktop takes input once the push-in settles.</summary>
    MonitorFocus
}

/// <summary>
/// Drives the office's two camera states through <see cref="ICameraRig"/> and
/// reports when the rig has settled on a view (the desktop's input waits for
/// it). With a live monitor wired (MonitorScreen) the desktop canvas stays
/// active on the CRT and BoothCoordinator gates its input; without one (the
/// hybrid scene) the desktop is shown only in MonitorFocus. Clicking the CRT
/// calls FocusMonitor; Escape, a click outside the screen and the desktop's
/// "&lt; Desk" button call FocusOffice.
/// </summary>
public sealed class OfficeViewController : MonoBehaviour
{
    /// <summary>Cinemachine rig (a CinemachineCameraRig MonoBehaviour).</summary>
    [SerializeField] private MonoBehaviour cameraRigBehaviour;

    /// <summary>The desktop canvas's object: shown only in MonitorFocus when no live monitor is wired (the hybrid scene's full-screen desktop); left active otherwise.</summary>
    [SerializeField] private GameObject desktopRoot;

    /// <summary>
    /// Optional: the live desktop on the CRT. When set, the desktop canvas
    /// stays active (the booth coordinator gates its input); when not (the
    /// hybrid scene), it is shown only in MonitorFocus.
    /// </summary>
    [SerializeField] private MonitorScreen monitorScreen;

    private ICameraRig _rig;

    /// <summary>The current view state.</summary>
    public OfficeView Current { get; private set; } = OfficeView.OfficeFocus;

    /// <summary>True once the rig shows the current view with no blend running (at once with no rig).</summary>
    public bool IsSettled { get; private set; }

    /// <summary>Raised after the view changes to the given state.</summary>
    public event Action<OfficeView> ViewChanged;

    /// <summary>Raised once per view change, when the rig first shows that view with no blend running.</summary>
    public event Action<OfficeView> Settled;

    private void Awake()
    {
        _rig = cameraRigBehaviour as ICameraRig;
        ApplyState();
    }

    /// <summary>
    /// The settle poll first, in either view and only while unsettled (no
    /// allocation); then Escape leaves the monitor, like the desktop's
    /// "&lt; Desk" button and a click outside the screen.
    /// </summary>
    private void Update()
    {
        if (!IsSettled && _rig != null && _rig.IsSettled(Current))
        {
            IsSettled = true;
            Settled?.Invoke(Current);
        }

        if (Current != OfficeView.MonitorFocus)
            return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            FocusOffice();
    }

    /// <summary>Pushes in to the monitor (no-op if already there).</summary>
    public void FocusMonitor() => SetView(OfficeView.MonitorFocus);

    /// <summary>Pulls back to the booth (no-op if already there).</summary>
    public void FocusOffice() => SetView(OfficeView.OfficeFocus);

    private void SetView(OfficeView view)
    {
        if (view == Current)
            return;

        Current = view;
        ApplyState();
        ViewChanged?.Invoke(Current);
    }

    /// <summary>Shows the view on the rig (unsettled until the poll sees it); with no rig the view is settled at once.</summary>
    private void ApplyState()
    {
        bool monitor = Current == OfficeView.MonitorFocus;

        if (monitorScreen == null && desktopRoot != null)
            desktopRoot.SetActive(monitor);

        if (_rig == null)
        {
            IsSettled = true;
            Settled?.Invoke(Current);
            return;
        }

        IsSettled = false;
        if (monitor)
            _rig.ShowMonitor();
        else
            _rig.ShowOffice();
    }
}
```

- [ ] **Step 6: Stage the new `DesktopShell`** — write `SCRATCH/p7stage/DesktopShell.cs` (Write tool; it replaces `Assets/Scripts/UI/DesktopShell.cs`):

```csharp
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Fake-OS desktop shell: the Start button toggles a small menu offering
/// Settings (opens an empty stub window), Turn off screen (darkens the live
/// monitor; only where one is wired) and Quit game. Wire from the editor
/// builder. All fields are optional / null-safe.
/// </summary>
public sealed class DesktopShell : MonoBehaviour
{
    /// <summary>Taskbar Start button.</summary>
    [SerializeField] private Button startButton;

    /// <summary>The pop-up Start menu panel (hidden on start).</summary>
    [SerializeField] private GameObject startMenu;

    /// <summary>Start-menu "Settings" entry.</summary>
    [SerializeField] private Button settingsButton;

    /// <summary>Start-menu "Quit game" entry; quits. (Formerly "Power", which quit too, so every scene's saved entry keeps quitting.)</summary>
    [FormerlySerializedAs("powerButton")]
    [SerializeField] private Button quitButton;

    /// <summary>Start-menu "Turn off screen" entry; optional, with the monitor screen.</summary>
    [SerializeField] private Button screenOffButton;

    /// <summary>The live monitor the "Turn off screen" entry darkens (wired with the entry).</summary>
    [SerializeField] private MonitorScreen monitorScreen;

    /// <summary>Settings window opened by the Settings entry (empty stub).</summary>
    [SerializeField] private OSWindowChrome settingsWindow;

    private void Start()
    {
        if (startMenu != null)
            startMenu.SetActive(false);

        if (startButton != null)
            startButton.onClick.AddListener(ToggleStartMenu);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null)
            quitButton.onClick.AddListener(Quit);
        if (screenOffButton != null)
            screenOffButton.onClick.AddListener(TurnOffScreen);
    }

    /// <summary>Shows/hides the Start menu.</summary>
    public void ToggleStartMenu()
    {
        if (startMenu != null)
            startMenu.SetActive(!startMenu.activeSelf);
    }

    /// <summary>Opens the Settings window and closes the Start menu.</summary>
    public void OpenSettings()
    {
        if (settingsWindow != null)
            settingsWindow.Open();
        if (startMenu != null)
            startMenu.SetActive(false);
    }

    /// <summary>Turns the monitor's screen off (a screen held on by a pending citation slip stays on) and closes the Start menu.</summary>
    public void TurnOffScreen()
    {
        if (monitorScreen != null)
            monitorScreen.TurnOff();
        if (startMenu != null)
            startMenu.SetActive(false);
    }

    /// <summary>Quits the game (exits play mode in the editor).</summary>
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
```

- [ ] **Step 7: Replace the four files** — save as `SCRATCH/p7_t11.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from rewrite import rewrite
W = r'E:\unity\NOPE-feat-clock'
STAGE = S + r'\p7stage'

def staged(name):
    return open(STAGE + '\\' + name, encoding='utf-8').read()

# Whole-file rewrites; all four files are CRLF and stay so.
rewrite(W + r'\Assets\Scripts\Office\ICameraRig.cs', '716d826a3b01', staged('ICameraRig.cs'))
rewrite(W + r'\Assets\Scripts\Office\CinemachineCameraRig.cs', '310233d2e634', staged('CinemachineCameraRig.cs'))
rewrite(W + r'\Assets\Scripts\Office\OfficeViewController.cs', '60d3dcfebddc', staged('OfficeViewController.cs'))
rewrite(W + r'\Assets\Scripts\UI\DesktopShell.cs', '0015ce3662f6', staged('DesktopShell.cs'))
```

Expected: `rewrote …ICameraRig.cs crlf`, `rewrote …CinemachineCameraRig.cs crlf`, `rewrote …OfficeViewController.cs crlf`, `rewrote …DesktopShell.cs crlf`.

- [ ] **Step 8: Compile check and run**

Expected: exit 0 / exit 0; `passed 433, failed 0`.

- [ ] **Step 9: Commit**

```bash
cd /e/unity/NOPE-feat-clock && python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/make_meta.py" Assets/Scripts/Office/DeskConfigSO.cs Assets/Scripts/Office/MonitorScreen.cs && git add Assets/Scripts/Office/DeskConfigSO.cs Assets/Scripts/Office/DeskConfigSO.cs.meta Assets/Scripts/Office/MonitorScreen.cs Assets/Scripts/Office/MonitorScreen.cs.meta Assets/Scripts/Office/ICameraRig.cs Assets/Scripts/Office/CinemachineCameraRig.cs Assets/Scripts/Office/OfficeViewController.cs Assets/Scripts/UI/DesktopShell.cs && git commit -F - <<'EOF'
feat(office): the live screen on the CRT, its power and a settled push-in

MonitorScreen draws the desktop live on the CRT's glass, owns its power
(PcScreen) and tints the bezel LED. The camera rig takes its brain from the
scene (else finds it in Start), frames the glass with MonitorFraming and
reports IsSettled; the view exposes Settled and keeps the old show/hide
path only where no MonitorScreen is wired. DeskConfigSO holds the desk's
knobs. The desktop's Power entry quits (quitButton, the saved data kept);
Turn off screen is a new entry.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 12: The desk: surface, papers, scanner, props and slots

Spec §1.5, §1.6, §1.8, §2.9, K4, K5, K11, K12, K14, K16, V3, V4, R1, R7, R8, R20, R21, R36: the desk components under `Assets/Scripts/Office/Desk/`. `DeskSurface` projects the pointer onto the desk plane and clamps with `DeskRect`; `DeskDraggable` drags a collider proxy with the EventSystem (the order and collider change while held, R7/R8); `DeskDocument` is one paper (a `SortingGroup` with its title and holder, the photo slot hook, its click and drag, and a slide that makes it inert until it lands, R36); `DeskScanner` holds the drop area and the glass bed and pulses; `DeskController` runs one case's papers through `DeskPapers` (hand-over from the traveller's side to the spawn slots, drops, scans, the stack, returning papers at the decision) and raises `ScanFinished`; `DeskReaction` plays a `DeskReactionSO` (a `ReactionCurve` pose, an optional sound, an optional tooltip through the `OverlayCallout`); `DeskSlot` and `DeskItem` are the decoration hooks (item 7). Nothing references them until Tasks 13–17.

**Files:**
- Create: the folder `Assets/Scripts/Office/Desk/` (+ `Desk.meta`) with `DeskSurface.cs`, `DeskDraggable.cs`, `DeskDocument.cs`, `DeskScanner.cs`, `DeskController.cs`, `DeskReaction.cs`, `DeskReactionSO.cs`, `DeskSlot.cs`, `DeskItem.cs` (+ `.meta`s)

- [ ] **Step 1: The surface and the drag** — write `Assets/Scripts/Office/Desk/DeskSurface.cs` (Write tool):

```csharp
using UnityEngine;

/// <summary>
/// The desk top papers lie on: a rectangle of <see cref="size"/> centred on
/// this transform, in its plane (position and forward). The pointer is
/// projected onto the plane by a camera ray, so the orthographic booth and a
/// perspective 3D desk work alike, and paper centres are kept inside the rectangle.
/// </summary>
public sealed class DeskSurface : MonoBehaviour
{
    /// <summary>The rectangle paper centres stay in, in local XY, centred on the transform.</summary>
    [SerializeField] private Vector2 size;

    /// <summary>Where the camera ray through a screen point meets the desk plane; false when the ray is parallel to it or the plane is behind the camera.</summary>
    public bool TryProject(Camera cam, Vector2 screenPoint, out Vector3 world)
    {
        world = Vector3.zero;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(screenPoint);
        var plane = new Plane(transform.forward, transform.position);
        if (!plane.Raycast(ray, out float enter))
            return false;

        world = ray.GetPoint(enter);
        return true;
    }

    /// <summary>The nearest point inside the rectangle, on the plane.</summary>
    public Vector3 Clamp(Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        (float x, float y) = new DeskRect(0f, 0f, size.x, size.y).Clamp(local.x, local.y);
        return transform.TransformPoint(new Vector3(x, y, 0f));
    }

    /// <summary>The point at (u, v) across the rectangle (0..1 each; (0, 0) is the bottom left).</summary>
    public Vector3 PointAt(Vector2 uv)
    {
        (float x, float y) = new DeskRect(0f, 0f, size.x, size.y).PointAt(uv.x, uv.y);
        return transform.TransformPoint(new Vector3(x, y, 0f));
    }
}
```

and `Assets/Scripts/Office/Desk/DeskDraggable.cs` (Write tool):

```csharp
using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drags a desk object across a DeskSurface with the EventSystem (its
/// Collider2D proxy is found by the Physics2DRaycaster): the pointer is
/// projected onto the desk plane and the object's centre stays inside the desk
/// rectangle. The proxy is off during the drag, so the release is never over
/// the object itself (no click follows a drag) and the drop is decided by the
/// projected pointer (DragEnded). While disabled by its owner the EventSystem
/// sends it nothing. Generic: papers use it now, decoration later.
/// </summary>
public sealed class DeskDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>The collider the raycaster finds (off while dragged).</summary>
    [SerializeField] private Collider2D proxy;

    private DeskSurface _surface;
    private Vector3 _grabOffset;

    /// <summary>Raised when a drag starts.</summary>
    public event Action<DeskDraggable> DragBegan;

    /// <summary>Raised when a drag ends, with the pointer projected onto the desk (or the object's position when the projection fails).</summary>
    public event Action<DeskDraggable, Vector3> DragEnded;

    /// <summary>Where the object was when the last drag started.</summary>
    public Vector3 PickUpPosition { get; private set; }

    /// <summary>Sets the desk this object moves on.</summary>
    public void Init(DeskSurface surface) => _surface = surface;

    /// <summary>Records the pick-up position and the grab offset, and turns the proxy off.</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        PickUpPosition = transform.position;
        _grabOffset = Vector3.zero;
        if (_surface != null && _surface.TryProject(eventData.pressEventCamera, eventData.position, out Vector3 point))
            _grabOffset = transform.position - point;

        if (proxy != null)
            proxy.enabled = false;
        DragBegan?.Invoke(this);
    }

    /// <summary>Follows the projected pointer, the centre clamped to the desk.</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_surface != null && _surface.TryProject(eventData.pressEventCamera, eventData.position, out Vector3 point))
            transform.position = _surface.Clamp(point + _grabOffset);
    }

    /// <summary>Turns the proxy back on and reports where the pointer was released.</summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (proxy != null)
            proxy.enabled = true;

        Vector3 released = transform.position;
        if (_surface != null && _surface.TryProject(eventData.pressEventCamera, eventData.position, out Vector3 point))
            released = point;
        DragEnded?.Invoke(this, released);
    }
}
```

- [ ] **Step 2: A paper and the scanner** — write `Assets/Scripts/Office/Desk/DeskDocument.cs` (Write tool):

```csharp
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A physical paper on the desk: a SortingGroup root carrying its sprite, its
/// collider, a Clickable and a DeskDraggable (so the hover outline follows the
/// paper and a raycast reports the group's order). It shows the document's
/// title, the holder's name and a reserved photo slot; every field is read on
/// the scanned PC copy. The texts stay in their source script (piece 9
/// translates the scanned copy, not the paper). Slides are linear moves in
/// Update, only while sliding.
/// </summary>
public sealed class DeskDocument : MonoBehaviour
{
    /// <summary>The document's title ("Travel Passport").</summary>
    [SerializeField] private TextMeshPro title;

    /// <summary>The holder's name (the traveller's registered given name).</summary>
    [SerializeField] private TextMeshPro holder;

    /// <summary>Reserved for piece 4's passport photo (inactive until then).</summary>
    [SerializeField] private GameObject photoSlot;

    /// <summary>The paper's click (brings it to the front).</summary>
    [SerializeField] private Clickable click;

    /// <summary>The paper's drag.</summary>
    [SerializeField] private DeskDraggable drag;

    /// <summary>The paper's sorting group (its order is the paper's place in the stack).</summary>
    [SerializeField] private SortingGroup group;

    private Vector3 _slideFrom;
    private Vector3 _slideTo;
    private float _slideSeconds;
    private float _slideElapsed;
    private Action _slideDone;

    /// <summary>The paper's index in the case (DeskController maps a drag or a click to it).</summary>
    public int Index { get; private set; }

    /// <summary>True while the paper slides (it takes no input then).</summary>
    public bool IsSliding { get; private set; }

    /// <summary>Only while sliding: moves along the slide and calls its done callback on landing.</summary>
    private void Update()
    {
        if (!IsSliding)
            return;

        _slideElapsed += Time.deltaTime;
        float t = _slideSeconds > 0f ? Mathf.Clamp01(_slideElapsed / _slideSeconds) : 1f;
        transform.position = Vector3.Lerp(_slideFrom, _slideTo, t);
        if (t < 1f)
            return;

        IsSliding = false;
        Action done = _slideDone;
        _slideDone = null;
        done?.Invoke();
    }

    /// <summary>Shows a document: its index, canonical title and holder (the paper keeps its source text).</summary>
    public void Bind(int index, CaseDocument doc)
    {
        Index = index;
        if (title != null)
            title.text = doc != null ? doc.name : string.Empty;
        if (holder != null)
            holder.text = doc != null ? doc.holder : string.Empty;
    }

    /// <summary>Sets the paper's sorting order (its place in the stack, or the held order).</summary>
    public void SetOrder(int order)
    {
        if (group != null)
            group.sortingOrder = order;
    }

    /// <summary>Lets the paper be dragged and clicked, or not.</summary>
    public void SetLive(bool live)
    {
        if (drag != null)
            drag.enabled = live;
        if (click != null)
            click.Interactable = live;
    }

    /// <summary>Slides the paper to a world point in <paramref name="seconds"/> (a linear move), then calls <paramref name="done"/>.</summary>
    public void SlideTo(Vector3 target, float seconds, Action done)
    {
        _slideFrom = transform.position;
        _slideTo = target;
        _slideSeconds = seconds;
        _slideElapsed = 0f;
        _slideDone = done;
        IsSliding = true;
    }
}
```

and `Assets/Scripts/Office/Desk/DeskScanner.cs` (Write tool):

```csharp
using UnityEngine;

/// <summary>
/// The desk scanner: a paper released with the pointer inside its drop area
/// scans on its glass bed (DeskController, DeskPapers.Drop), and the scanner
/// pulses when a scan finishes.
/// </summary>
public sealed class DeskScanner : MonoBehaviour
{
    /// <summary>The drop area, in local units, centred on the transform.</summary>
    [SerializeField] private Vector2 dropSize;

    /// <summary>Where a scanning paper lies, in local units.</summary>
    [SerializeField] private Vector2 bedCentre;

    /// <summary>The scanner's click reaction, played when a scan finishes (optional).</summary>
    [SerializeField] private DeskReaction reaction;

    /// <summary>True when a world point lies inside the drop area.</summary>
    public bool Contains(Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        return new DeskRect(0f, 0f, dropSize.x, dropSize.y).Contains(local.x, local.y);
    }

    /// <summary>The glass bed's centre in world space.</summary>
    public Vector3 BedPoint => transform.TransformPoint(bedCentre);

    /// <summary>Plays the scanner's reaction (a finished scan).</summary>
    public void Pulse()
    {
        if (reaction != null)
            reaction.Play();
    }
}
```

- [ ] **Step 3: One case's papers** — write `Assets/Scripts/Office/Desk/DeskController.cs` (Write tool):

```csharp
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// One case's papers on the desk: hands papers over from the traveller's side
/// to the spawn slots, lets the player drag them (DeskDraggable), decides each
/// drop through DeskPapers (it stays, it scans on the scanner, or it slides back
/// to where it was picked up), runs the scan timer and raises ScanFinished,
/// stacks papers by sorting order, returns them at the decision, and shows the
/// day-1 scan note. A paper takes input only while BoothCoordinator allows
/// papers, DeskPapers lets it be dragged and it is not sliding.
/// </summary>
public sealed class DeskController : MonoBehaviour
{
    /// <summary>The desk the papers lie on.</summary>
    [SerializeField] private DeskSurface surface;

    /// <summary>The desk scanner.</summary>
    [SerializeField] private DeskScanner scanner;

    /// <summary>The inactive paper cloned per handed-over document.</summary>
    [SerializeField] private DeskDocument paperTemplate;

    /// <summary>Where paper clones live.</summary>
    [SerializeField] private Transform paperRoot;

    /// <summary>Where papers slide in from and back to: the traveller's side of the desk.</summary>
    [SerializeField] private Transform handOverPoint;

    /// <summary>The day-1 note on the scanner tray (its text comes from the config).</summary>
    [SerializeField] private TMP_Text scanHint;

    /// <summary>The desk tuning (scan time, spawn slots, slide time, paper orders, the note).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The case's papers by index (null until handed over).</summary>
    private readonly List<DeskDocument> _papers = new List<DeskDocument>();

    private readonly PaperStack _stack = new PaperStack();
    private IReadOnlyList<CaseDocument> _documents = Array.Empty<CaseDocument>();
    private DeskPapers _state;
    private bool _live;
    private int _day;
    private int _scansToday;
    private int _handedOver;

    /// <summary>The paper being dragged, or -1.</summary>
    private int _held = -1;

    /// <summary>True when the desk and all its parts are wired; otherwise documents go straight to their windows (InvestigationUIController).</summary>
    public bool IsReachable =>
        surface != null && scanner != null && paperTemplate != null && paperRoot != null && handOverPoint != null && config != null;

    /// <summary>Raised when a scan finishes, with the paper's index (its window opens).</summary>
    public event Action<int> ScanFinished;

    private void Awake()
    {
        if (scanHint != null && config != null)
            scanHint.text = config.scanHint;
        RefreshHint();
    }

    /// <summary>Only while a scan runs: advances it; a finished paper slides back to where it was picked up.</summary>
    private void Update()
    {
        if (_state == null || !_state.ScannerBusy)
            return;

        int done = _state.Tick(Time.deltaTime);
        if (done < 0)
            return;

        DeskDocument paper = _papers[done];
        Slide(paper, paper.GetComponent<DeskDraggable>().PickUpPosition);
        scanner.Pulse();
        _scansToday++;
        RefreshHint();
        ScanFinished?.Invoke(done);
    }

    /// <summary>Starts a day: nothing scanned yet (the scan note may show again on its days).</summary>
    public void BeginDay(int day)
    {
        _day = day;
        _scansToday = 0;
        RefreshHint();
    }

    /// <summary>Starts a case's papers; the documents handed over on arrival slide onto the desk.</summary>
    public void BeginCase(IReadOnlyList<CaseDocument> docs)
    {
        _documents = docs ?? Array.Empty<CaseDocument>();
        _state = new DeskPapers(_documents, config.scanSeconds);
        _papers.Clear();
        for (int i = 0; i < _state.Count; i++)
            _papers.Add(null);
        _stack.Clear();
        _handedOver = 0;
        _held = -1;

        foreach (int i in _state.ArrivalIndices)
            HandOver(i);
        RefreshHint();
    }

    /// <summary>
    /// Hands paper <paramref name="i"/> over: a clone of the template appears at
    /// the hand-over point, on top of the stack, and slides to the next spawn
    /// slot (slots are reused in order). Nothing happens unless DeskPapers
    /// agrees (a paper is handed over once).
    /// </summary>
    public void HandOver(int i)
    {
        if (_state == null || !_state.HandOver(i))
            return;

        DeskDocument paper = Instantiate(paperTemplate, paperRoot);
        paper.transform.position = handOverPoint.position;
        paper.gameObject.SetActive(true);
        paper.Bind(i, _documents[i]);

        DeskDraggable drag = paper.GetComponent<DeskDraggable>();
        drag.Init(surface);
        drag.DragBegan += HandleDragBegan;
        drag.DragEnded += HandleDragEnded;
        paper.GetComponent<Clickable>().onClick.AddListener(() => BringToFront(paper));

        _papers[i] = paper;
        _stack.Add(i);
        ApplyOrders();

        Vector2[] slots = config.paperSpawnSlots;
        Vector3 target = slots != null && slots.Length > 0 ? surface.PointAt(slots[_handedOver % slots.Length]) : surface.transform.position;
        _handedOver++;
        Slide(paper, target);
        RefreshHint();
    }

    /// <summary>The decision: every paper goes back (a running scan is cancelled), slides inert to the traveller's side and is destroyed.</summary>
    public void EndCase()
    {
        if (_state == null)
            return;

        _state.ReturnAll();
        foreach (DeskDocument paper in _papers)
        {
            if (paper == null)
                continue;

            DeskDocument leaving = paper;
            leaving.SetLive(false);
            leaving.SlideTo(handOverPoint.position, config.paperSlideSeconds, () => Destroy(leaving.gameObject));
        }

        _papers.Clear();
        _stack.Clear();
        _held = -1;
        RefreshHint();
    }

    /// <summary>Allows the papers input or not (BoothCoordinator); remembered for papers handed over later.</summary>
    public void SetPapersLive(bool live)
    {
        _live = live;
        foreach (DeskDocument paper in _papers)
            if (paper != null)
                ApplyLive(paper);
    }

    private void HandleDragBegan(DeskDraggable drag)
    {
        DeskDocument paper = drag.GetComponent<DeskDocument>();
        _held = paper.Index;
        paper.SetOrder(config.heldPaperOrder);
    }

    /// <summary>DeskPapers decides the drop; the paper slides to the bed (scanning), back to its pick-up point (refused), or stays; it goes on top either way.</summary>
    private void HandleDragEnded(DeskDraggable drag, Vector3 released)
    {
        DeskDocument paper = drag.GetComponent<DeskDocument>();
        _held = -1;

        switch (_state.Drop(paper.Index, scanner.Contains(released)))
        {
            case DropOutcome.Scanning:
                Slide(paper, scanner.BedPoint);
                break;
            case DropOutcome.Refused:
                Slide(paper, drag.PickUpPosition);
                break;
        }

        _stack.BringToFront(paper.Index);
        ApplyOrders();
        RefreshHint();
    }

    /// <summary>A click without a drag brings the paper to the top.</summary>
    private void BringToFront(DeskDocument paper)
    {
        _stack.BringToFront(paper.Index);
        ApplyOrders();
    }

    /// <summary>Slides a paper; it is inert while sliding, and its liveness is re-applied when it lands.</summary>
    private void Slide(DeskDocument paper, Vector3 target)
    {
        paper.SlideTo(target, config.paperSlideSeconds, () => ApplyLive(paper));
        ApplyLive(paper);
    }

    /// <summary>A paper takes input while papers are allowed, DeskPapers lets it be dragged and it is not sliding.</summary>
    private void ApplyLive(DeskDocument paper) =>
        paper.SetLive(_live && _state != null && _state.CanDrag(paper.Index) && !paper.IsSliding);

    /// <summary>Stack order: the base order plus the paper's place; the held paper above them all.</summary>
    private void ApplyOrders()
    {
        foreach (DeskDocument paper in _papers)
            if (paper != null)
                paper.SetOrder(paper.Index == _held ? config.heldPaperOrder : config.paperBaseOrder + _stack.IndexOf(paper.Index));
    }

    /// <summary>The day-1 scan note (DeskHints): re-evaluated at every hand-over, drop, finished scan and case end.</summary>
    private void RefreshHint()
    {
        if (scanHint == null)
            return;

        bool paperOnDesk = _state != null && _state.OnDeskCount > 0;
        scanHint.gameObject.SetActive(config != null &&
                                      DeskHints.ScanHintVisible(config.scanHint, _day, config.scanHintUntilDay, _scansToday, paperOnDesk));
    }
}
```

- [ ] **Step 4: Props, reactions and hooks** — write `Assets/Scripts/Office/Desk/DeskReactionSO.cs` (Write tool):

```csharp
using UnityEngine;

/// <summary>
/// One desk prop's click reaction (DeskReaction): its animation, an optional
/// sound and an optional tooltip with its placement and duration. The builder
/// creates the booth's reactions under Assets/Data/Config/DeskReactions and
/// keeps a designer's edits.
/// </summary>
[CreateAssetMenu(fileName = "Reaction_", menuName = "TimeDesk/Office/Desk Reaction")]
public sealed class DeskReactionSO : ScriptableObject
{
    /// <summary>The animation (None: tooltip only).</summary>
    public ReactionKind kind;

    /// <summary>Seconds the animation takes.</summary>
    [Min(0.05f)] public float seconds = 0.35f;

    /// <summary>How strong the animation is (scale fraction, offset in local units, 30 degrees per unit of wobble).</summary>
    public float amplitude = 0.12f;

    /// <summary>Optional: played through the prop's AudioSource (the project has no clips yet).</summary>
    public AudioClip clip;

    /// <summary>The tooltip ("{value}" is the prop's readout text); empty = no tooltip.</summary>
    [TextArea] public string tooltip;

    /// <summary>Seconds the tooltip stays up.</summary>
    [Min(0.1f)] public float tooltipSeconds = 2.5f;

    /// <summary>Where the tooltip sits from the prop (overlay reference px; up is positive).</summary>
    public Vector2 tooltipOffset = new Vector2(0f, 60f);
}
```

`Assets/Scripts/Office/Desk/DeskReaction.cs` (Write tool):

```csharp
using TMPro;
using UnityEngine;

/// <summary>
/// A desk prop's click reaction, tuned by a DeskReactionSO: a short animation
/// (ReactionCurve, relative to the rest pose captured at Awake), an optional
/// sound (when the reaction has a clip and the prop an AudioSource) and an
/// optional tooltip that shows a live readout's text (the calendar's day, the
/// till's credits...) through the overlay tooltip callout.
/// </summary>
[RequireComponent(typeof(Clickable))]
public sealed class DeskReaction : MonoBehaviour
{
    /// <summary>The reaction's tuning.</summary>
    [SerializeField] private DeskReactionSO reaction;

    /// <summary>Optional: the live text the tooltip's {value} shows.</summary>
    [SerializeField] private TMP_Text readout;

    /// <summary>The overlay tooltip (shared by every prop).</summary>
    [SerializeField] private OverlayCallout tooltip;

    /// <summary>Optional: plays the reaction's clip.</summary>
    [SerializeField] private AudioSource audioSource;

    private Vector3 _restPosition;
    private Quaternion _restRotation;
    private Vector3 _restScale;
    private float _elapsed;
    private bool _animating;

    private void Awake()
    {
        _restPosition = transform.localPosition;
        _restRotation = transform.localRotation;
        _restScale = transform.localScale;
        GetComponent<Clickable>().onClick.AddListener(Play);
    }

    /// <summary>Plays the reaction: the animation, the clip when there is one and a source, and the tooltip when its template is not blank.</summary>
    public void Play()
    {
        if (reaction == null)
            return;

        _elapsed = 0f;
        _animating = reaction.kind != ReactionKind.None;

        if (reaction.clip != null && audioSource != null)
            audioSource.PlayOneShot(reaction.clip);

        if (tooltip != null && !string.IsNullOrWhiteSpace(reaction.tooltip))
            tooltip.Show(Interview.Fill(reaction.tooltip, Interview.ValueToken, readout != null ? readout.text : string.Empty),
                         transform, reaction.tooltipOffset, reaction.tooltipSeconds);
    }

    /// <summary>Only while animating: the pose relative to the rest pose; the rest pose again at the end.</summary>
    private void Update()
    {
        if (!_animating)
            return;

        _elapsed += Time.deltaTime;
        float t = _elapsed / reaction.seconds;
        if (t >= 1f)
        {
            transform.localPosition = _restPosition;
            transform.localRotation = _restRotation;
            transform.localScale = _restScale;
            _animating = false;
            return;
        }

        ReactionPose pose = ReactionCurve.Evaluate(reaction.kind, t, reaction.amplitude);
        transform.localPosition = _restPosition + new Vector3(0f, pose.OffsetY, 0f);
        transform.localRotation = _restRotation * Quaternion.Euler(0f, 0f, pose.AngleDeg);
        transform.localScale = new Vector3(_restScale.x * pose.ScaleX, _restScale.y * pose.ScaleY, _restScale.z);
    }
}
```

`Assets/Scripts/Office/Desk/DeskSlot.cs` (Write tool):

```csharp
using UnityEngine;

/// <summary>What a desk slot may hold.</summary>
public enum DeskSlotKind
{
    /// <summary>A decoration's home spot (the plant, the mug, the photo).</summary>
    Decoration,

    /// <summary>A free spot for anything the player places.</summary>
    Free
}

/// <summary>
/// A named spot on the desk. Decoration hook (item 7): read by the builder now
/// (props stand at their slot), by the decoration piece later. No public
/// members until then.
/// </summary>
public sealed class DeskSlot : MonoBehaviour
{
    /// <summary>The slot's id ("plant", "mug", "photo", "free_1"...).</summary>
    [SerializeField] private string slotId;

    /// <summary>What the slot may hold.</summary>
    [SerializeField] private DeskSlotKind kind;
}
```

and `Assets/Scripts/Office/Desk/DeskItem.cs` (Write tool):

```csharp
using UnityEngine;

/// <summary>
/// A decoration prop's id. Decoration hook (item 7): read by the builder now,
/// by the decoration piece later. No public members until then.
/// </summary>
public sealed class DeskItem : MonoBehaviour
{
    /// <summary>The item's id ("stamp", "mug", "plant", "poster").</summary>
    [SerializeField] private string itemId;
}
```

- [ ] **Step 5: Compile check and run**

Expected: exit 0 / exit 0; `passed 433, failed 0`.

- [ ] **Step 6: Commit**

```bash
cd /e/unity/NOPE-feat-clock && python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/make_meta.py" Assets/Scripts/Office/Desk Assets/Scripts/Office/Desk/DeskSurface.cs Assets/Scripts/Office/Desk/DeskDraggable.cs Assets/Scripts/Office/Desk/DeskDocument.cs Assets/Scripts/Office/Desk/DeskScanner.cs Assets/Scripts/Office/Desk/DeskController.cs Assets/Scripts/Office/Desk/DeskReaction.cs Assets/Scripts/Office/Desk/DeskReactionSO.cs Assets/Scripts/Office/Desk/DeskSlot.cs Assets/Scripts/Office/Desk/DeskItem.cs && git add Assets/Scripts/Office/Desk Assets/Scripts/Office/Desk.meta && git commit -F - <<'EOF'
feat(desk): papers you drag and scan, reacting props and decoration slots

DeskController hands a case's papers over to the desk's spawn slots, lets
the player drag them (DeskDraggable over DeskSurface), decides each drop
through DeskPapers (stays, scans on the DeskScanner, or slides back) and
reports finished scans; a sliding paper is inert until it lands. Props
react through DeskReaction and DeskReactionSO; DeskSlot and DeskItem are
the decoration hooks.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

`git status --short` then shows only the two untracked files.

---

### Task 13: The traveller at the desk, the wheel and the booth's rules applied

Spec §1.4, §1.7, §1.9, §2.8, §2.10, §2.12, K20, V1, V5, R10, R31, R32, R33, R34: `TravellerView` shows the booth figure from presentation to the decision and gives the wheel and the bubble their anchor. `TravellerWheel` sits on an always-active host and toggles its `Catcher` (R33): it opens around the traveller (its ring sized from the knobs with `RadialLayout.Extent`, placed with `OverlayProjection` in the overlay canvas rect it resolves at `Awake`), closes on Escape or a click outside the ring, and `Say` shows the traveller's reply in the bubble. `BoothCoordinator` applies `BoothRules` and the wake rules to everything that takes input in the booth (the desktop's raycaster, the CRT, the power button, the exit and glass zones, the props, the papers, the wheel, the traveller) whenever the view, its settling, the screen, the phase, the wheel or a pending citation changes, and owns the day-1 wheel note. `GameManager` gains `travellerView` and `booth`: `SetTravellerAtDesk` is the one presence transition (the closing rule, the figure, the booth phase), the newsletters set `Newsletter`, `BeginShift` sets `NoTraveller`, the booth learns the day after READY is wired, and `ShowVerdictThen` holds the screen while a citation slip waits (R31).

**Files:**
- Create: the folder `Assets/Scripts/Characters/` (+ `Characters.meta`) with `TravellerView.cs`; `Assets/Scripts/UI/TravellerWheel.cs`; `Assets/Scripts/Office/BoothCoordinator.cs` (+ `.meta`s)
- Modify: `Assets/Scripts/GameManager.cs`

- [ ] **Step 1: The booth figure** — write `Assets/Scripts/Characters/TravellerView.cs` (Write tool):

```csharp
using UnityEngine;

/// <summary>
/// The traveller in the booth: shown from presentation until the decision
/// (GameManager.SetTravellerAtDesk), hidden otherwise. Its anchor (the
/// traveller's chest) is the one point the traveller wheel's ring and the
/// reply bubble centre on. Piece 4's layered figure extends this component.
/// </summary>
public sealed class TravellerView : MonoBehaviour
{
    /// <summary>What shows while the traveller is at the desk (the placeholder sprite now; piece 4's layers).</summary>
    [SerializeField] private Renderer[] figure;

    /// <summary>Where the wheel and the bubble centre (the traveller's chest).</summary>
    [SerializeField] private Transform anchor;

    /// <summary>Where the wheel and the bubble centre.</summary>
    public Transform Anchor => anchor;

    private void Awake() => Clear();

    /// <summary>Shows the traveller (presentation).</summary>
    public void Show() => SetFigureVisible(true);

    /// <summary>Hides the traveller (the decision, or no traveller yet).</summary>
    public void Clear() => SetFigureVisible(false);

    private void SetFigureVisible(bool visible)
    {
        if (figure == null)
            return;

        foreach (Renderer part in figure)
            if (part != null)
                part.enabled = visible;
    }
}
```

- [ ] **Step 2: The wheel** — write `Assets/Scripts/UI/TravellerWheel.cs` (Write tool):

```csharp
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// The traveller wheel: the interview's choices on an elliptical ring around
/// the traveller (the ring's InteractionPanelController lays them out with
/// RadialLayoutGroup; "&lt; Back" sits in the centre slot), opened by clicking
/// the traveller or the desk intercom while BoothCoordinator allows it, and
/// the traveller's replies in a speech bubble beside them. This host is always
/// active (so it wakes at load); its Catcher child, a full-screen
/// click-to-close area holding the ring, is shown only while the wheel is
/// open. Escape or a click outside the ring closes it.
/// </summary>
public sealed class TravellerWheel : MonoBehaviour, IPointerClickHandler
{
    /// <summary>The full-screen transparent click catcher, active only while open; the ring sits under it.</summary>
    [SerializeField] private GameObject catcher;

    /// <summary>The ring's root (anchors and pivot (0.5, 0.5)), placed over the traveller.</summary>
    [SerializeField] private RectTransform ring;

    /// <summary>The ring's layout.</summary>
    [SerializeField] private RadialLayoutGroup layout;

    /// <summary>The ring's centre slot ("&lt; Back").</summary>
    [SerializeField] private RectTransform centreSlot;

    /// <summary>The traveller: its anchor places the ring and the reply bubble.</summary>
    [SerializeField] private TravellerView traveller;

    /// <summary>The speech bubble (an overlay callout).</summary>
    [SerializeField] private OverlayCallout bubble;

    /// <summary>The wheel's layout and bubble knobs.</summary>
    [SerializeField] private DeskConfigSO config;

    private bool _canOpen;
    private Camera _camera;
    private RectTransform _canvasRect;

    /// <summary>True while the wheel is open.</summary>
    public bool IsOpen => catcher != null && catcher.activeSelf;

    /// <summary>Raised when the wheel opens or closes.</summary>
    public event Action OpenChanged;

    /// <summary>
    /// Runs at load (the host is active): applies the knobs to the ring and its
    /// centre, sizes the ring to the box around every item (so the projection
    /// keeps it on screen), caches the camera and the overlay canvas's rect
    /// (the per-frame placement looks nothing up) and closes the wheel (never
    /// deactivating this host).
    /// </summary>
    private void Awake()
    {
        if (config != null)
        {
            if (layout != null)
            {
                layout.Radii = config.wheelRadii;
                layout.ItemSize = config.wheelItemSize;
            }

            if (ring != null)
            {
                (float width, float height) = RadialLayout.Extent(config.wheelRadii.x, config.wheelRadii.y, config.wheelItemSize.x, config.wheelItemSize.y);
                ring.sizeDelta = new Vector2(width, height);
            }

            if (centreSlot != null)
                centreSlot.sizeDelta = config.wheelCentreSize;
        }

        _camera = Camera.main;
        _canvasRect = OverlayProjection.CanvasRectOf(this);
        if (catcher != null)
            catcher.SetActive(false);
    }

    /// <summary>Only while open: follows the traveller; Escape closes.</summary>
    private void LateUpdate()
    {
        if (!IsOpen)
            return;

        Place();
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            Close();
    }

    /// <summary>Opens the wheel over the traveller, unless it may not open or is open (the traveller hit zone's and the desk intercom's persistent call).</summary>
    public void Open()
    {
        if (!_canOpen || IsOpen || catcher == null)
            return;

        catcher.SetActive(true);
        Place();
        OpenChanged?.Invoke();
    }

    /// <summary>Closes the ring (a reply bubble stays until its time is up).</summary>
    public void Close()
    {
        if (!IsOpen)
            return;

        catcher.SetActive(false);
        OpenChanged?.Invoke();
    }

    /// <summary>Whether the wheel may be open (BoothRules.WheelAllowed); false closes it and hides the reply bubble (the view left the booth or the traveller left).</summary>
    public void SetCanOpen(bool can)
    {
        _canOpen = can;
        if (can)
            return;

        Close();
        if (bubble != null)
            bubble.Hide();
    }

    /// <summary>Shows the traveller's reply beside them (the caller has applied DisplayText); a blank reply hides the bubble.</summary>
    public void Say(string text)
    {
        if (bubble != null && config != null)
            bubble.Show(text, traveller != null ? traveller.Anchor : null, config.bubbleOffset, config.bubbleSeconds);
    }

    /// <summary>A click on the catcher (outside the ring's buttons) closes the wheel.</summary>
    public void OnPointerClick(PointerEventData eventData) => Close();

    /// <summary>Centres the ring on the traveller's anchor (with no traveller or anchor it stays centred on the screen).</summary>
    private void Place()
    {
        if (ring != null && traveller != null && traveller.Anchor != null)
            OverlayProjection.TryPlace(ring, _canvasRect, _camera, traveller.Anchor.position, Vector2.zero);
    }
}
```

- [ ] **Step 3: The booth's rules applied** — write `Assets/Scripts/Office/BoothCoordinator.cs` (Write tool):

```csharp
using TMPro;
using UnityEngine;

/// <summary>
/// Applies BoothRules and the wake rules to the booth: from the view, its
/// settling, the screen's power, the shift's phase (set by GameManager), the
/// wheel and a pending citation slip, it decides which of the desktop, the
/// CRT, the bezel power button, the focus exit and glass zones, the desk
/// props, the papers, the traveller and the wheel take input; it wakes the
/// screen for a presented traveller and a finished scan, holds it on for a
/// citation slip, and shows the day-1 wheel note. Every reference is optional:
/// a missing view counts as the booth, settled; a missing screen counts as on.
/// Event-driven (no per-frame code).
/// </summary>
public sealed class BoothCoordinator : MonoBehaviour
{
    /// <summary>The office view (focus, settling).</summary>
    [SerializeField] private OfficeViewController view;

    /// <summary>The live monitor (power, desktop input).</summary>
    [SerializeField] private MonitorScreen screen;

    /// <summary>The papers and the scanner.</summary>
    [SerializeField] private DeskController desk;

    /// <summary>The traveller wheel.</summary>
    [SerializeField] private TravellerWheel wheel;

    /// <summary>The CRT's click (focus).</summary>
    [SerializeField] private Clickable crt;

    /// <summary>The bezel power button.</summary>
    [SerializeField] private Clickable powerButton;

    /// <summary>The zone around the screen that leaves focus.</summary>
    [SerializeField] private Clickable focusExit;

    /// <summary>The inert zone over the glass that keeps a click on a dark screen inside focus.</summary>
    [SerializeField] private Clickable glassZone;

    /// <summary>The traveller's hit zone (opens the wheel).</summary>
    [SerializeField] private Clickable travellerHitZone;

    /// <summary>The desk props (stamp, mug, plant, poster, intercom, scanner tray, till, stability monitor, wall clock, calendar).</summary>
    [SerializeField] private Clickable[] props;

    /// <summary>The day-1 note above the traveller.</summary>
    [SerializeField] private TMP_Text wheelHint;

    /// <summary>The desk tuning (the wheel note's text and last day).</summary>
    [SerializeField] private DeskConfigSO config;

    private BoothPhase _phase = BoothPhase.NoTraveller;
    private int _day;
    private bool _citationPending;
    private bool _wheelOpenedToday;

    private void Awake()
    {
        if (wheelHint != null && config != null)
            wheelHint.text = config.wheelHint;
    }

    private void OnEnable()
    {
        if (view != null)
        {
            view.ViewChanged += HandleView;
            view.Settled += HandleView;
        }
        if (screen != null)
            screen.PowerChanged += Apply;
        if (wheel != null)
            wheel.OpenChanged += HandleWheel;
        if (desk != null)
            desk.ScanFinished += HandleScanFinished;
    }

    private void OnDisable()
    {
        if (view != null)
        {
            view.ViewChanged -= HandleView;
            view.Settled -= HandleView;
        }
        if (screen != null)
            screen.PowerChanged -= Apply;
        if (wheel != null)
            wheel.OpenChanged -= HandleWheel;
        if (desk != null)
            desk.ScanFinished -= HandleScanFinished;
    }

    /// <summary>The first application, once every component has woken (Awake runs before any Start).</summary>
    private void Start() => Apply();

    /// <summary>Where the shift is (GameManager); presenting a traveller also wakes the screen.</summary>
    public void SetPhase(BoothPhase phase)
    {
        _phase = phase;
        if (phase == BoothPhase.TravellerAtDesk && screen != null)
            screen.Wake(WakeReason.TravellerPresented);
        Apply();
    }

    /// <summary>Starts a day: the day-1 notes may show again on their days.</summary>
    public void BeginDay(int day)
    {
        _day = day;
        _wheelOpenedToday = false;
        if (desk != null)
            desk.BeginDay(day);
        Apply();
    }

    /// <summary>A citation slip waits for Acknowledge (or no longer does): it holds the screen on and makes the power button inert.</summary>
    public void SetCitationPending(bool pending)
    {
        _citationPending = pending;
        if (screen != null)
            screen.SetHeld(pending);
        Apply();
    }

    private void HandleView(OfficeView _) => Apply();

    private void HandleWheel()
    {
        if (wheel.IsOpen)
            _wheelOpenedToday = true;
        Apply();
    }

    private void HandleScanFinished(int _)
    {
        if (screen != null)
            screen.Wake(WakeReason.ScanFinished);
        Apply();
    }

    /// <summary>The rules' input for the booth as it is now.</summary>
    private BoothContext Context() => new BoothContext(
        view != null && view.Current == OfficeView.MonitorFocus,
        view == null || view.IsSettled,
        screen == null || screen.IsOn,
        _phase,
        wheel != null && wheel.IsOpen,
        _citationPending);

    /// <summary>Applies the rules. The wheel first: closing it changes the context the rest reads (its OpenChanged re-applies too, harmlessly).</summary>
    private void Apply()
    {
        if (wheel != null)
            wheel.SetCanOpen(BoothRules.Evaluate(Context()).WheelAllowed);

        BoothInput input = BoothRules.Evaluate(Context());
        if (screen != null)
            screen.SetInteractive(input.DesktopInteractive);
        if (crt != null)
            crt.Interactable = input.CrtFocusable;
        if (powerButton != null)
            powerButton.Interactable = input.PowerButtonLive;
        if (focusExit != null)
            focusExit.gameObject.SetActive(input.FocusExitLive);
        if (glassZone != null)
            glassZone.gameObject.SetActive(input.FocusExitLive);
        if (props != null)
            foreach (Clickable prop in props)
                if (prop != null)
                    prop.Interactable = input.PropsLive;
        if (desk != null)
            desk.SetPapersLive(input.PapersLive);
        if (travellerHitZone != null)
            travellerHitZone.Interactable = input.TravellerLive;
        if (wheelHint != null)
            wheelHint.gameObject.SetActive(config != null &&
                DeskHints.WheelHintVisible(config.wheelHint, _day, config.wheelHintUntilDay, _wheelOpenedToday, _phase == BoothPhase.TravellerAtDesk));
    }
}
```

- [ ] **Step 4: GameManager** — save as `SCRATCH/p7_t13.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

# GameManager: one presence transition feeds the closing rule, the booth figure
# and the booth's input phase; the newsletters and a pending citation slip are
# phases/flags of the booth too (R10, R31).
apply(W + r'\Assets\Scripts\GameManager.cs', [
("""    /// <summary>Scene clock for today's shift (optional: without it the day ends only when the queue is empty).</summary>
    [SerializeField] private ShiftClockDriver shiftClock;""",
"""    /// <summary>Scene clock for today's shift (optional: without it the day ends only when the queue is empty).</summary>
    [SerializeField] private ShiftClockDriver shiftClock;

    /// <summary>Optional: the booth figure, from presentation until the decision.</summary>
    [SerializeField] private TravellerView travellerView;

    /// <summary>Optional: the booth's input and wake rules.</summary>
    [SerializeField] private BoothCoordinator booth;"""),
("""        // READY sign releases the per-case gate (only meaningful when wired).
        if (readySign != null)
            readySign.onClick.AddListener(() => _readyGate.Release());
""",
"""        // READY sign releases the per-case gate (only meaningful when wired).
        if (readySign != null)
            readySign.onClick.AddListener(() => _readyGate.Release());

        // The booth's day (its day-1 notes) and phase: the briefing comes first.
        if (booth != null)
            booth.BeginDay(_worldState.day);
"""),
("""            Debug.Log("[GameManager] <<< Exiting Start (showing morning briefing before day loop).");
            dayFlowUI.ShowBriefing(_worldState, () => BeginShift(planToRun, seedToUse));""",
"""            Debug.Log("[GameManager] <<< Exiting Start (showing morning briefing before day loop).");
            if (booth != null)
                booth.SetPhase(BoothPhase.Newsletter);
            dayFlowUI.ShowBriefing(_worldState, () => BeginShift(planToRun, seedToUse));"""),
("""            Debug.Log($"[GameManager] <<< Exiting HandleDayCompleted (showing results panel, then {(ending != null ? "the title scene" : "Home")}).");
            dayFlowUI.ShowResults(_worldState, _ledger, next);""",
"""            Debug.Log($"[GameManager] <<< Exiting HandleDayCompleted (showing results panel, then {(ending != null ? "the title scene" : "Home")}).");
            if (booth != null)
                booth.SetPhase(BoothPhase.Newsletter);
            dayFlowUI.ShowResults(_worldState, _ledger, next);"""),
("""    /// <summary>Starts the day loop and the shift clock together (after the briefing).</summary>
    private void BeginShift(DayPlanSO plan, int daySeed)
    {
        orchestrator.StartDay(_worldState, plan, daySeed);""",
"""    /// <summary>Starts the day loop and the shift clock together (after the briefing).</summary>
    private void BeginShift(DayPlanSO plan, int daySeed)
    {
        if (booth != null)
            booth.SetPhase(BoothPhase.NoTraveller);

        orchestrator.StartDay(_worldState, plan, daySeed);"""),
("""    /// <summary>Presents a case via the investigation UI (or legacy era UI).</summary>
    private void ShowActiveCase(CaseInstance inst)
    {
        _travellerAtDesk = true;
""",
"""    /// <summary>
    /// The one presence transition: whether a traveller is at the desk feeds
    /// the closing-time rule, the booth figure and the booth's input phase.
    /// </summary>
    private void SetTravellerAtDesk(bool at)
    {
        _travellerAtDesk = at;

        if (travellerView != null)
        {
            if (at)
                travellerView.Show();
            else
                travellerView.Clear();
        }

        if (booth != null)
            booth.SetPhase(at ? BoothPhase.TravellerAtDesk : BoothPhase.NoTraveller);
    }

    /// <summary>Presents a case via the investigation UI (or legacy era UI).</summary>
    private void ShowActiveCase(CaseInstance inst)
    {
        SetTravellerAtDesk(true);
"""),
("""        Debug.Log($"[GameManager] >>> Entering HandlePlayerChoseEra (slot {_activeCaseIndex1Based}, chosenEra='{chosenEra?.id}').");
        _travellerAtDesk = false;""",
"""        Debug.Log($"[GameManager] >>> Entering HandlePlayerChoseEra (slot {_activeCaseIndex1Based}, chosenEra='{chosenEra?.id}').");
        SetTravellerAtDesk(false);"""),
("""        Debug.Log($"[GameManager] >>> Entering HandleDecision (slot {_activeCaseIndex1Based}, accepted={accepted}).");
        _travellerAtDesk = false;""",
"""        Debug.Log($"[GameManager] >>> Entering HandleDecision (slot {_activeCaseIndex1Based}, accepted={accepted}).");
        SetTravellerAtDesk(false);"""),
("""    /// <summary>
    /// Shows the verdict slip if a UI is wired (pausing the shift clock while a
    /// citation slip is up), then runs the continuation.
    /// </summary>
    private void ShowVerdictThen(CaseVerdict verdict, System.Action onContinue)
    {
        if (officeUI == null)
        {
            onContinue?.Invoke();
            return;
        }

        // A citation slip holds the day, and the shift clock, until acknowledged.
        bool holdsClock = shiftClock != null && verdict != null && verdict.citationIssued;
        if (holdsClock)
            shiftClock.Pause();

        officeUI.ShowVerdict(verdict, () =>
        {
            if (holdsClock)
                shiftClock.Resume();
            onContinue?.Invoke();
        });
    }""",
"""    /// <summary>
    /// Shows the verdict slip if a UI is wired (pausing the shift clock while a
    /// citation slip is up, and holding the PC screen on so the slip can never
    /// sit on a dark screen), then runs the continuation.
    /// </summary>
    private void ShowVerdictThen(CaseVerdict verdict, System.Action onContinue)
    {
        if (officeUI == null)
        {
            onContinue?.Invoke();
            return;
        }

        // A citation slip holds the day, the shift clock and the screen until acknowledged.
        bool citation = verdict != null && verdict.citationIssued;
        bool holdsClock = shiftClock != null && citation;
        if (holdsClock)
            shiftClock.Pause();
        if (citation && booth != null)
            booth.SetCitationPending(true);

        officeUI.ShowVerdict(verdict, () =>
        {
            if (citation && booth != null)
                booth.SetCitationPending(false);
            if (holdsClock)
                shiftClock.Resume();
            onContinue?.Invoke();
        });
    }"""),
])
```

Expected: `ok …GameManager.cs crlf`.

- [ ] **Step 5: Compile check and run**

Expected: exit 0 / exit 0; `passed 433, failed 0`.

- [ ] **Step 6: Commit**

```bash
cd /e/unity/NOPE-feat-clock && python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/make_meta.py" Assets/Scripts/Characters Assets/Scripts/Characters/TravellerView.cs Assets/Scripts/UI/TravellerWheel.cs Assets/Scripts/Office/BoothCoordinator.cs && git add Assets/Scripts/Characters Assets/Scripts/Characters.meta Assets/Scripts/UI/TravellerWheel.cs Assets/Scripts/UI/TravellerWheel.cs.meta Assets/Scripts/Office/BoothCoordinator.cs Assets/Scripts/Office/BoothCoordinator.cs.meta Assets/Scripts/GameManager.cs && git commit -F - <<'EOF'
feat(booth): the traveller at the desk, the traveller wheel, one input table

TravellerView shows the booth figure while a traveller is at the desk.
TravellerWheel opens the interview's choices around them and shows their
replies in a speech bubble. BoothCoordinator applies BoothRules to the
desktop, the CRT and its power button, the focus zones, the props, the
papers, the wheel and the traveller, and owns the day-1 wheel note.
GameManager sets the booth's phase through one presence transition and
holds the screen on while a citation slip waits.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 14: The investigation plays on the desk and the wheel

Spec §2.11, §1.10, K5, K6, K19, K20, R2, R3, R17, R23, R25, R27: `InvestigationUIController` gains `desk`, `wheel`, `idleScreen` and the window-layout fields (initialised to today's constants, so the unrebuilt hybrid scene keeps its layout, R23; the builder writes the 4:3 values in Task 16). `DeskReachable` (the desk and its scanner wired) decides every branch (R3): a new case goes to `desk.BeginCase` (arrivals slide to the desk) or, without a desk, straight to the windows (Task 8's path); a hand-over request calls `desk.HandOver` or opens the window; a finished scan opens the paper's window. `Choose` closes the wheel after a hand-over and says the traveller's reply (`DisplayText.For(InterviewScript.SpokenSince(…), Spoken)`) in the bubble when the choice added one ("Ask about home >" and "< Back" add none, so the last reply stays up, spec §1.7); the no-desk arrivals come from `CaseDocuments.ArrivalIndices`, the rule `DeskPapers` uses; `RefreshChoices` puts "< Back" (kind `Back`) in the wheel's centre slot; `Hide` shows the idle line ("Waiting for the next traveller") and `ShowRich` hides it; `Decide` returns the papers with `desk.EndCase`. One warning at start names an unwired desk scanner (K5, R3). The results line reads "(log a deviation before denying)" (`DayFlowUIController`).

**Files:**
- Modify: `Assets/Scripts/UI/InvestigationUIController.cs`, `Assets/Scripts/UI/DayFlowUIController.cs`, `docs/FEATURES.md`

- [ ] **Step 1: Apply** — save as `SCRATCH/p7_t14.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

apply(W + r'\Assets\Scripts\UI\InvestigationUIController.cs', [
# --- class doc ---
("""/// <summary>
/// Orchestrates the office investigation: shows the visitor's travel claim and
/// today's directives, runs the interview on the intercom (document requests,
/// today's questions and narrative dialogs, with the transcript window), spawns
/// a draggable window per document, builds a shelf of reference books the
/// player can open/stow, and offers the binary Accept/Deny.
///""",
"""/// <summary>
/// Orchestrates the office investigation: shows the visitor's travel claim and
/// today's directives, runs the interview on the traveller wheel (document
/// requests, today's questions and narrative dialogs, with the transcript
/// window and the traveller's replies in the wheel's bubble), hands each
/// document over as a physical paper on the desk (whose scan opens its
/// window) or, where no desk is wired, straight to its draggable window,
/// builds a shelf of reference books the player can open/stow, and offers the
/// binary Accept/Deny.
///"""),
# --- fields ---
("""    [Header("Interaction / records")]
    /// <summary>The intercom: shows the current interview node's choices (requests, questions, dialog replies).</summary>
    [SerializeField] private InteractionPanelController interactionPanel;""",
"""    [Header("Interaction / records")]
    /// <summary>The traveller wheel's ring: shows the current interview node's choices (requests, questions, dialog replies).</summary>
    [SerializeField] private InteractionPanelController interactionPanel;"""),
("""    /// <summary>The transcript window's chrome; every interview choice but a document request opens it.</summary>
    [SerializeField] private OSWindowChrome transcriptChrome;
""",
"""    /// <summary>The transcript window's chrome; every interview choice but a document request opens it.</summary>
    [SerializeField] private OSWindowChrome transcriptChrome;

    [Header("Desk")]
    /// <summary>The physical papers and the scanner (optional: without it documents open on request, straight to their windows).</summary>
    [SerializeField] private DeskController desk;

    /// <summary>The traveller wheel: closed after a hand-over, and it shows the traveller's replies.</summary>
    [SerializeField] private TravellerWheel wheel;

    /// <summary>Shown on the desktop between travellers.</summary>
    [SerializeField] private GameObject idleScreen;

    [Header("Window layout")]
    /// <summary>Where the first document window opens (desktop units from the centre). The builder writes the 4:3 layout; this default is the 16:9 one.</summary>
    [SerializeField] private Vector2 documentWindowOrigin = new Vector2(-330f, 140f);

    /// <summary>Offset from one document window to the next.</summary>
    [SerializeField] private Vector2 documentWindowStep = new Vector2(620f, 0f);

    /// <summary>Where the first book window opens.</summary>
    [SerializeField] private Vector2 bookWindowOrigin = new Vector2(-380f, -150f);

    /// <summary>Horizontal step between the three book windows of a row.</summary>
    [SerializeField] private float bookWindowColumnStep = 320f;

    /// <summary>Offset from one row of book windows to the next.</summary>
    [SerializeField] private Vector2 bookWindowRowStep = new Vector2(40f, 40f);
"""),
# --- InterviewReachable doc ---
("""    /// True when a traveller's answers can be read: always in the text
    /// fallback; in the rich desk only when the intercom, the transcript window
    /// and its chrome are wired.""",
"""    /// True when a traveller's answers can be read: always in the text
    /// fallback; in the rich desk only when the wheel's ring, the transcript
    /// window and its chrome are wired."""),
("""    private bool RichMode =>
        documentWindowTemplate != null && windowLayer != null &&
        acceptButton != null && denyButton != null;
""",
"""    private bool RichMode =>
        documentWindowTemplate != null && windowLayer != null &&
        acceptButton != null && denyButton != null;

    /// <summary>True when documents become physical papers: the rich desk with the desk and all its parts wired (a partly wired desk takes the window path, so papers always reach the PC).</summary>
    private bool DeskReachable => RichMode && desk != null && desk.IsReachable;
"""),
# --- Awake / OnDestroy ---
("""        // Without the transcript nothing a traveller says could be read, so the day speaks no tell.
        if (RichMode && !InterviewReachable)
            Debug.LogWarning("[InvestigationUIController] Intercom or interview transcript not wired: questions are hidden and no tell is spoken today. Run Tools > TimeDesk > Build Office UI.", this);
    }

    private void OnDestroy()
    {
        if (compareController != null)
            compareController.PairCompared -= HandlePairCompared;
    }""",
"""        // Without the transcript nothing a traveller says could be read, so the day speaks no tell.
        if (RichMode && !InterviewReachable)
            Debug.LogWarning("[InvestigationUIController] Traveller wheel or interview transcript not wired: questions are hidden and no tell is spoken today. Run Tools > TimeDesk > Build Office UI.", this);

        // Without the desk every document still reaches the PC, as its window.
        if (RichMode && !DeskReachable)
            Debug.LogWarning("[InvestigationUIController] Desk scanner not wired: documents open on the PC when handed over (no physical papers). Run Tools > TimeDesk > Build Office UI.", this);

        if (DeskReachable)
            desk.ScanFinished += OpenDocumentWindow;

        if (idleScreen != null)
            idleScreen.SetActive(true);
    }

    private void OnDestroy()
    {
        if (compareController != null)
            compareController.PairCompared -= HandlePairCompared;

        if (DeskReachable)
            desk.ScanFinished -= OpenDocumentWindow;
    }"""),
# --- Hide shows the idle line ---
("""    /// <summary>Hides the investigation overlay (between cases).</summary>
    public void Hide()
    {
        if (root != null) root.SetActive(false);
        if (_fallbackPanel != null) _fallbackPanel.SetActive(false);
    }""",
"""    /// <summary>Hides the investigation overlay (between cases); the desktop shows its idle line.</summary>
    public void Hide()
    {
        if (root != null) root.SetActive(false);
        if (_fallbackPanel != null) _fallbackPanel.SetActive(false);
        if (idleScreen != null) idleScreen.SetActive(true);
    }"""),
# --- ShowRich ---
("""    private void ShowRich(CaseInstance inst, ContentLibrarySO lib)
    {
        if (root != null) root.SetActive(true);
""",
"""    private void ShowRich(CaseInstance inst, ContentLibrarySO lib)
    {
        if (root != null) root.SetActive(true);
        if (idleScreen != null) idleScreen.SetActive(false);
"""),
("""        // Documents are handed over, never taken: those marked "on arrival" when
        // the traveller steps up, the others through an interview request. Each
        // window spawns hidden and opens when its document is handed over.""",
"""        // Documents are handed over, never taken: those marked "on arrival" when
        // the traveller steps up, the others through the traveller wheel. With
        // the desk, each becomes a paper whose scan opens its window; without
        // it, the window opens at the hand-over. Windows spawn hidden."""),
("""                    rt.anchoredPosition = new Vector2(-330f + i * 620f, 140f);""",
"""                    rt.anchoredPosition = documentWindowOrigin + i * documentWindowStep;"""),
("""        foreach (int i in CaseDocuments.ArrivalIndices(_caseDocuments))
            OpenDocumentWindow(i);
""",
"""        if (DeskReachable)
        {
            desk.BeginCase(_caseDocuments);
        }
        else
        {
            foreach (int i in CaseDocuments.ArrivalIndices(_caseDocuments))
                OpenDocumentWindow(i);
        }
"""),
# --- the wheel: Back in the centre ---
("""    /// <summary>Shows the current interview node's choices on the intercom.</summary>""",
"""    /// <summary>Shows the current interview node's choices on the traveller wheel ("&lt; Back" in its centre).</summary>"""),
("""            actions.Add(new InteractionAction { label = choice.Label, execute = () => Choose(id) });""",
"""            actions.Add(new InteractionAction { label = choice.Label, centre = choice.Kind == DialogChoiceKind.Back, execute = () => Choose(id) });"""),
("""            Debug.LogError("[InvestigationUIController] No interview day was injected (GameManager.SetInterviewDay), so the intercom is empty.", this);""",
"""            Debug.LogError("[InvestigationUIController] No interview day was injected (GameManager.SetInterviewDay), so the traveller wheel is empty.", this);"""),
# --- Choose ---
("""    /// <summary>
    /// Plays one interview choice: the transcript shows its lines; a document
    /// request hands that document over (its window opens and is raised), any
    /// other choice opens the transcript; a finished dialog is recorded for the
    /// end of the shift.
    /// </summary>
    private void Choose(string choiceId)
    {
        DialogChoice choice = _runner != null ? _runner.Choose(choiceId) : null;
        if (choice == null)
            return;

        if (transcriptWindow != null)
            transcriptWindow.Refresh();

        if (choice.Action == DialogAction.HandOverDocument)
            OpenDocumentWindow(choice.DocumentIndex);
        else if (transcriptChrome != null)
        {
            transcriptChrome.Open();
        }
""",
"""    /// <summary>
    /// Plays one interview choice: the transcript shows its lines; a document
    /// request hands that document over (a paper onto the desk, or straight to
    /// its window where no desk is wired) and closes the wheel so the player can
    /// take it; any other choice opens the transcript; the traveller's reply, when
    /// the choice adds one, goes to the wheel's bubble (the spoken reveal point;
    /// a choice without one, such as "Ask about home >" or "&lt; Back", leaves the
    /// last reply up); a finished dialog is recorded for the end of the shift.
    /// </summary>
    private void Choose(string choiceId)
    {
        if (_runner == null)
            return;

        int before = _runner.Transcript.Count;
        DialogChoice choice = _runner.Choose(choiceId);
        if (choice == null)
            return;

        if (transcriptWindow != null)
            transcriptWindow.Refresh();

        if (choice.Action == DialogAction.HandOverDocument)
        {
            if (DeskReachable)
                desk.HandOver(choice.DocumentIndex);
            else
                OpenDocumentWindow(choice.DocumentIndex);

            if (wheel != null)
                wheel.Close();
        }
        else if (transcriptChrome != null)
        {
            transcriptChrome.Open();
        }

        string reply = InterviewScript.SpokenSince(_runner.Transcript, before);
        if (wheel != null && reply.Length > 0)
            wheel.Say(DisplayText.For(reply, TextMedium.Spoken));
"""),
# --- OpenDocumentWindow doc: the scan reveal point ---
("""    /// <summary>
    /// Opens a paper's scanned window and raises it. The first time it opens
    /// this case, the paper also gets a desktop icon at the top of the grid,
    /// which reopens the window after it is closed.
    /// </summary>""",
"""    /// <summary>
    /// Opens a paper's scanned window and raises it (the desk's ScanFinished,
    /// or a hand-over where no desk is wired: the written reveal point). The
    /// first time it opens this case, the paper also gets a desktop icon at
    /// the top of the grid, which reopens the window after it is closed.
    /// </summary>"""),
# --- books ---
("""                rt.anchoredPosition = new Vector2(-380f + (i % 3) * 320f + (i / 3) * 40f, -150f + (i / 3) * 40f);""",
"""                rt.anchoredPosition = bookWindowOrigin + new Vector2((i % 3) * bookWindowColumnStep, 0f) + (i / 3) * bookWindowRowStep;"""),
# --- Decide: the papers leave with the traveller ---
("""    private void Decide(bool accepted)
    {
        Hide();
""",
"""    private void Decide(bool accepted)
    {
        if (DeskReachable)
            desk.EndCase();
        Hide();
"""),
])

apply(W + r'\Assets\Scripts\UI\DayFlowUIController.cs', [
("""                sb.AppendLine($"Undocumented denials: {ledger.UnprovenDenialCount} (scan the evidence before denying)");""",
"""                sb.AppendLine($"Undocumented denials: {ledger.UnprovenDenialCount} (log a deviation before denying)");"""),
])

apply(W + r'\docs\FEATURES.md', [
("""- [ ] End-of-day "SHIFT LEDGER" newsletter over the booth (Go Home), incl. undocumented-denials line""",
"""- [ ] End-of-day "SHIFT LEDGER" newsletter over the booth (Go Home), incl. the undocumented-denials line ("log a deviation before denying")"""),
("""likewise the rich desk warns once at start when the intercom, the interview transcript or its window chrome is not wired: that day questions are hidden, no answer is computed and no tell is spoken, and the intercom offers only document requests""",
"""likewise the rich desk warns once at start when the traveller wheel, the interview transcript or its window chrome is not wired: that day questions are hidden, no answer is computed and no tell is spoken, and the wheel offers only document requests; likewise it warns once when the desk scanner is not wired: documents then open on the PC when handed over"""),
])
```

Expected: `ok …InvestigationUIController.cs crlf`, `ok …DayFlowUIController.cs crlf`, `ok …docs\FEATURES.md crlf`.

- [ ] **Step 2: Compile check and run**

Expected: exit 0 / exit 0; `passed 433, failed 0`.

- [ ] **Step 3: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scripts/UI/InvestigationUIController.cs Assets/Scripts/UI/DayFlowUIController.cs docs/FEATURES.md && git commit -F - <<'EOF'
feat(investigation): papers go to the desk, the interview to the wheel

With a desk wired, documents are handed over as papers (arrivals when the
traveller is presented, requests when chosen) and a finished scan opens
the paper's window; without one they open on the PC directly. The wheel
closes after a hand-over, shows the traveller's reply in the bubble and
holds "< Back" in its centre. An idle line shows between travellers; the
window layout is data; the papers leave at the decision; an unwired desk
scanner is reported once. The results line asks to log a deviation.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 15: The builder: the desktop on the CRT's glass, the push-in and screen power

Spec §2.15 items 1–6 and 11, §1.2, §1.3, §1.11, K1, K2, K7, K8, K9, K15, R12, R13, R22, R28, R29, R35, R37: `OfficeSceneUIBuilder` becomes a partial class; the booth and desk parts go to the new `OfficeSceneUIBuilder.Desk.cs`. `Build()` creates `Desk_Default` (`EnsureDeskConfig`) after the newsletters and passes it to `BuildBooth`, which now builds the camera first, then the live monitor (`BuildMonitorScreen`: `CRTMonitor/ScreenAnchor` at the glass's centre, `MonitorScreen` on `CRTMonitor`, the bezel's power button and LED, the inactive `GlassZone`, R37), frames the monitor camera from the glass (`MonitorFraming`, replacing the old `MonitorOrthoSize` constant), wires the rig's `brain`, `monitorScreen` and config (R35) and the view's `monitorScreen`, gives `FocusExitZone` its `FocusOffice` call, clears READY's persistent calls (READY only calls the next traveller, K9; `ClearPersistentCalls` is the one home of that clearing, which `WirePersistentVoid` now goes through too) and removes `BackToOfficeButton`. The desktop `Canvas` becomes a World Space canvas under the screen anchor, found by name, with a `RectMask2D`. The taskbar gains "< Desk" (the way back), and the Start menu holds Settings, Turn off screen and Quit game (`DesktopShell.screenOffButton` and `quitButton`, wired with the screen). The committed scene is not rebuilt until Task 19.

**Files:**
- Create: `Assets/Editor/OfficeSceneUIBuilder.Desk.cs` (+ `.meta`)
- Modify: `Assets/Editor/OfficeSceneUIBuilder.cs`

- [ ] **Step 1: The new partial** — write `Assets/Editor/OfficeSceneUIBuilder.Desk.cs` (Write tool):

```csharp
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office builder's booth and desk parts (the physical desk, piece 7):
/// the live monitor on the CRT, the desk tuning asset and the shared hit-zone
/// helpers. Part of <see cref="OfficeSceneUIBuilder"/>; Build() calls these in
/// its order (geometry before the objects that wire to it).
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The desk tuning asset, created by the builder when missing (a designer's edits are kept).</summary>
    private const string DeskConfigPath = "Assets/Data/Config/Desk_Default.asset";

    /// <summary>The desktop canvas in canvas units: the old full-screen desktop's 1080-unit height, at the glass's 4:3.</summary>
    private static readonly Vector2 DesktopSize = new Vector2(1440f, 1080f);

    /// <summary>Size of the largest 4:3 rectangle inside the CRT's glass, in the crt sprite's own units (measured from crt.png with a colour mask): the desktop canvas fills it.</summary>
    private static readonly Vector2 CrtGlassSize = new Vector2(0.676f, 0.507f);

    /// <summary>The power button on the CRT's right bezel, beside the glass's lower right corner (crt sprite units).</summary>
    private static readonly Vector2 CrtPowerButtonCentre = new Vector2(0.285f, -0.19f);

    /// <summary>The power LED, right of the button (crt sprite units).</summary>
    private static readonly Vector2 CrtPowerLedCentre = new Vector2(0.33f, -0.19f);

    /// <summary>The focus exit zone around the screen (crt sprite units): larger than any focused view, so a click anywhere outside the screen leaves focus.</summary>
    private static readonly Vector2 FocusExitSize = new Vector2(8f, 6f);

    /// <summary>Returns Desk_Default, creating it with the spec's defaults when missing (a designer's edits are kept).</summary>
    private static DeskConfigSO EnsureDeskConfig()
    {
        DeskConfigSO config = AssetDatabase.LoadAssetAtPath<DeskConfigSO>(DeskConfigPath);
        if (config != null)
            return config;

        EnsureFolderTree("Assets/Data/Config");
        config = ScriptableObject.CreateInstance<DeskConfigSO>();
        AssetDatabase.CreateAsset(config, DeskConfigPath);
        AssetDatabase.SaveAssets();
        return config;
    }

    /// <summary>
    /// The live monitor on the CRT: a ScreenAnchor at the glass's centre holding
    /// the World Space desktop canvas (1440 x 1080 units scaled into the glass's
    /// 4:3 rectangle, drawn by the main camera just above the CRT), the bezel's
    /// power button (persistent MonitorScreen.TogglePower) and LED, the focus
    /// exit zone around the screen (BuildBooth wires it to FocusOffice once the
    /// view exists) and the inert glass zone, both inactive until focused; and
    /// the MonitorScreen that owns them. Idempotent.
    /// </summary>
    private static MonitorScreen BuildMonitorScreen(SpriteRenderer crt, Canvas desktopCanvas, Camera main, DeskConfigSO config)
    {
        Transform anchor = EnsureChild(crt.transform, "ScreenAnchor");
        anchor.localPosition = new Vector3(CrtGlassCentre.x, CrtGlassCentre.y, 0f);
        anchor.localRotation = Quaternion.identity;
        anchor.localScale = Vector3.one;

        Transform canvasTransform = desktopCanvas.transform;
        canvasTransform.SetParent(anchor, false);
        canvasTransform.localPosition = Vector3.zero;
        canvasTransform.localRotation = Quaternion.identity;
        canvasTransform.localScale = Vector3.one * (CrtGlassSize.y / DesktopSize.y);
        desktopCanvas.worldCamera = main;
        desktopCanvas.sortingLayerID = SortingLayer.NameToID("Default");
        desktopCanvas.sortingOrder = config.screenCanvasOrder;

        SpriteRenderer power = PlaceSprite(crt.transform, "PowerButton", EnsureOfficeShape("crt_power", 28, 28, Center, PowerButtonPixel),
            new Vector3(CrtPowerButtonCentre.x, CrtPowerButtonCentre.y, 0f), 0.06f, config.bezelOrder);
        Clickable powerClick = EnsureClickable(power);
        SpriteRenderer led = PlaceSprite(crt.transform, "PowerLed", EnsureOfficeShape("crt_led", 8, 8, Center, LedPixel),
            new Vector3(CrtPowerLedCentre.x, CrtPowerLedCentre.y, 0f), 0.02f, config.bezelOrder);

        Clickable exit = EnsureHitZone(crt.transform, "FocusExitZone", Vector3.zero, FocusExitSize, config.focusExitOrder);
        exit.gameObject.SetActive(false);

        // A click on the screen never leaves focus, even while it is dark (its raycaster is then off).
        Clickable glass = EnsureHitZone(anchor, "GlassZone", Vector3.zero, CrtGlassSize, config.glassOrder);
        glass.Interactable = false;
        ClearPersistentCalls(glass, "onClick");
        glass.gameObject.SetActive(false);

        MonitorScreen screen = crt.GetComponent<MonitorScreen>();
        if (screen == null)
            screen = crt.gameObject.AddComponent<MonitorScreen>();
        var so = new SerializedObject(screen);
        SetRef(so, "desktopCanvas", desktopCanvas);
        SetRef(so, "desktopRaycaster", desktopCanvas.GetComponent<GraphicRaycaster>());
        SetRef(so, "glass", anchor);
        so.FindProperty("glassSize").vector2Value = CrtGlassSize;
        SetRef(so, "powerLed", led);
        SetRef(so, "config", config);
        so.ApplyModifiedProperties();

        WirePersistentVoid(powerClick, "onClick", screen, nameof(MonitorScreen.TogglePower));
        return screen;
    }

    /// <summary>
    /// A hit zone over other art: a Clickable on a box of <paramref name="size"/>
    /// (the zone's local units) with a hidden SpriteRenderer that carries only
    /// the sorting order the raycast ranks it by (a collider with no renderer
    /// reports order 0). The hover shows the hand cursor and no outline. Idempotent.
    /// </summary>
    private static Clickable EnsureHitZone(Transform parent, string name, Vector3 localPosition, Vector2 size, int sortingOrder)
    {
        Transform t = EnsureChild(parent, name);
        t.localPosition = localPosition;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;

        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = t.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = null;
        sr.enabled = false;
        sr.sortingOrder = sortingOrder;

        BoxCollider2D box = t.GetComponent<BoxCollider2D>();
        if (box == null)
            box = t.gameObject.AddComponent<BoxCollider2D>();
        box.offset = Vector2.zero;
        box.size = size;

        Clickable click = t.GetComponent<Clickable>();
        if (click == null)
            click = t.gameObject.AddComponent<Clickable>();
        return click;
    }

    /// <summary>Finds or creates a plain child object under a parent.</summary>
    private static Transform EnsureChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    /// <summary>Placeholder power button: a cream round face on a dark rim, with a power glyph.</summary>
    private static Color32 PowerButtonPixel(int x, int y)
    {
        float dx = x - 13.5f;
        float dy = y - 13.5f;
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        if (d > 13.5f)
            return new Color32(0, 0, 0, 0);
        if (d > 11.5f)
            return new Color32(74, 70, 62, 255);

        var glyph = new Color32(90, 86, 78, 255);
        bool bar = Mathf.Abs(dx) < 1.3f && dy > 0f && dy < 7.5f;
        bool ring = d > 5f && d < 7f && !(dy > 0f && Mathf.Abs(dx) < 3f);
        return bar || ring ? glyph : new Color32(214, 208, 190, 255);
    }

    /// <summary>Placeholder LED: a white disc, tinted on and off by MonitorScreen.</summary>
    private static Color32 LedPixel(int x, int y)
    {
        float dx = x - 3.5f;
        float dy = y - 3.5f;
        return dx * dx + dy * dy <= 14.5f ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
    }

    /// <summary>Empties a UnityEvent's persistent calls on <paramref name="host"/> (a call saved in the scene would otherwise survive the build).</summary>
    private static void ClearPersistentCalls(Object host, string eventProp)
    {
        var so = new SerializedObject(host);
        if (ClearPersistentCalls(so, eventProp) != null)
            so.ApplyModifiedProperties();
    }

    /// <summary>
    /// Empties a UnityEvent's persistent calls in <paramref name="so"/> and
    /// returns them (null when the object has no such event); the caller
    /// applies <paramref name="so"/>. WirePersistentVoid adds its call to them.
    /// </summary>
    private static SerializedProperty ClearPersistentCalls(SerializedObject so, string eventProp)
    {
        SerializedProperty calls = so.FindProperty(eventProp + ".m_PersistentCalls.m_Calls");
        if (calls != null)
            calls.ClearArray();
        return calls;
    }
}
```

- [ ] **Step 2: The builder's own file** — save as `SCRATCH/p7_t15.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'
B = W + r'\Assets\Editor\OfficeSceneUIBuilder.cs'

apply(B, [
# --- the class: partial (the booth and desk parts live in OfficeSceneUIBuilder.Desk.cs, R22) ---
("""/// One-click builder for the full Office investigation scene.
/// Creates and wires everything needed for a playable shift:
/// - Canvas + EventSystem (new Input System)""",
"""/// One-click builder for the full Office investigation scene.
/// Creates and wires everything needed for a playable shift:
/// - The PC desktop: a World Space canvas drawn live on the CRT's glass
///   (1440 x 1080 units at 4:3), with screen power and a focus push-in
///   [MonitorScreen, OfficeViewController, CinemachineCameraRig]; EventSystem
///   (new Input System)"""),
("""/// Safe to re-run: finds existing pieces by name and only fills gaps. It builds
/// only Assets/Scenes/OfficeScene.unity and refuses any other active scene.
/// </summary>
public static class OfficeSceneUIBuilder
{""",
"""/// Safe to re-run: finds existing pieces by name and only fills gaps. It builds
/// only Assets/Scenes/OfficeScene.unity and refuses any other active scene.
/// The booth and desk parts are in OfficeSceneUIBuilder.Desk.cs.
/// </summary>
public static partial class OfficeSceneUIBuilder
{"""),
("""    /// <summary>
    /// The reference resolution of both canvas scalers, which never scale a
    /// canvas below it (ConfigureScaler); every layout is authored against it,
    /// so what fits at this size (the intercom's fit reads its height) fits on
    /// every screen.
    /// </summary>""",
"""    /// <summary>
    /// The reference resolution of the office overlay canvas's scaler, which
    /// never scales the canvas below it (ConfigureScaler); every overlay layout
    /// is authored against it, so what fits at this size fits on every screen.
    /// (The desktop is a World Space canvas of DesktopSize units with no scaler.)
    /// </summary>"""),
# --- Build(): the desk tuning, then the booth with its live monitor ---
("""        // Briefing + Results — newsletter panels on the office overlay canvas so
        // they read in the booth view, not inside the PC desktop (which is hidden
        // outside MonitorFocus).
        Canvas officeCanvas = EnsureOfficeOverlayCanvas();""",
"""        // Briefing + Results: newsletter panels on the office overlay canvas, so
        // they read over the booth view, not on the small live desktop.
        Canvas officeCanvas = EnsureOfficeOverlayCanvas();"""),
("""        Transform results = BuildNewsletter(officeCanvas.transform, "ResultsPanel", "SHIFT LEDGER — EVENING EDITION",
            "GO HOME", out TMP_Text resultsTitle, out TMP_Text resultsBody, out Button goHome);
""",
"""        Transform results = BuildNewsletter(officeCanvas.transform, "ResultsPanel", "SHIFT LEDGER — EVENING EDITION",
            "GO HOME", out TMP_Text resultsTitle, out TMP_Text resultsBody, out Button goHome);

        // The desk tuning (created once): the monitor push-in, screen power, sorting bands.
        DeskConfigSO deskConfig = EnsureDeskConfig();
"""),
("""        // Booth + cameras + view controller (new). The existing Canvas becomes
        // the Monitor-Focus desktop, hidden until the CRT is focused.
        OfficeViewController officeView = BuildBooth(canvas);

        // Fake-OS desktop shell: NEW apps only (existing document/reference/compare
        // windows are launched by the investigation icon grid), plus a Start menu.
        BuildDesktopShell(canvas, bookShelf, windowLayer, library);""",
"""        // Booth + cameras + view controller. The desktop canvas is drawn live on
        // the CRT's glass (BuildMonitorScreen), always active; its input is gated.
        OfficeViewController officeView = BuildBooth(canvas, deskConfig, out MonitorScreen monitorScreen);

        // Fake-OS desktop shell: NEW apps only (existing document/reference/compare
        // windows are launched by the investigation icon grid), plus a Start menu
        // and the taskbar's way back to the desk.
        BuildDesktopShell(canvas, bookShelf, windowLayer, library, officeView, monitorScreen);"""),
("""        Debug.Log("[TimeDesk] Office investigation desk built and wired (HUD, citation, briefing/results, claim, document + book windows, intercom + interview transcript, compare, Accept/Deny, GameManager, DaySystem). Save the scene.");""",
"""        Debug.Log("[TimeDesk] Office investigation desk built and wired (live monitor on the CRT with screen power, HUD, citation, briefing/results, claim, document + book windows, intercom + interview transcript, compare, Accept/Deny, GameManager, DaySystem). Save the scene.");"""),
# --- the desktop canvas: World Space on the CRT's glass ---
("""    private static Canvas EnsureCanvas()
    {
        // The desktop canvas — never the office overlay canvas, which hosts the
        // briefing/results newsletters and stays visible outside MonitorFocus.
        Canvas canvas = null;
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.gameObject.name == "OfficeOverlayCanvas")
                continue;
            canvas = c;
            break;
        }
        if (canvas == null)
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            canvas = go.AddComponent<Canvas>();
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ConfigureScaler(canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>());
        if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    /// <summary>
    /// Scales a canvas with the screen from the reference resolution, and
    /// never below it: Expand keeps the canvas at least 1920x1080 in both
    /// dimensions (a screen wider than 16:9 gets more width, a narrower one
    /// more height), so a layout that fits at the reference size, such as the
    /// intercom's choices, fits on every screen.
    /// </summary>""",
"""    /// <summary>The desktop canvas's object name (the office overlay canvas is another Canvas).</summary>
    private const string DesktopCanvasName = "Canvas";

    /// <summary>
    /// The desktop canvas, found by its name: a World Space canvas of
    /// <see cref="DesktopSize"/> units (BuildMonitorScreen puts it on the CRT's
    /// glass), masked to its rect (dragged windows never draw over the bezel).
    /// A canvas scaler only serves a screen-space canvas, so it has none.
    /// </summary>
    private static Canvas EnsureCanvas()
    {
        Canvas canvas = null;
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c.gameObject.name == DesktopCanvasName)
            {
                canvas = c;
                break;
            }
        }
        if (canvas == null)
        {
            var go = new GameObject(DesktopCanvasName, typeof(RectTransform));
            canvas = go.AddComponent<Canvas>();
            go.AddComponent<GraphicRaycaster>();
        }
        canvas.renderMode = RenderMode.WorldSpace;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
            Object.DestroyImmediate(scaler);
        if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
        if (canvas.GetComponent<RectMask2D>() == null) canvas.gameObject.AddComponent<RectMask2D>();

        var rt = (RectTransform)canvas.transform;
        rt.anchorMin = Center;
        rt.anchorMax = Center;
        rt.pivot = Center;
        rt.sizeDelta = DesktopSize;
        return canvas;
    }

    /// <summary>
    /// Scales the office overlay canvas (the newsletters, and the booth's
    /// overlay UI) with the screen from the reference resolution, and never
    /// below it: Expand keeps the canvas at least 1920x1080 in both dimensions
    /// (a screen wider than 16:9 gets more width, a narrower one more height),
    /// so a layout that fits at the reference size fits on every screen.
    /// </summary>"""),
("""    /// <summary>
    /// Overlay canvas for office-view UI (the briefing/results newsletters).
    /// Separate from the desktop canvas, which OfficeViewController hides
    /// outside MonitorFocus.
    /// </summary>""",
"""    /// <summary>
    /// Overlay canvas for booth-view UI (the briefing/results newsletters),
    /// drawn over the booth and the live desktop, which is a World Space canvas
    /// on the CRT's glass.
    /// </summary>"""),
# --- the camera constants: the glass from its measured 4:3 rectangle; the framing is computed ---
("""    /// <summary>Orthographic half-height of the monitor close-up, which frames the CRT's glass.</summary>
    private const float MonitorOrthoSize = 1.4f;

    /// <summary>Centre of the CRT's glass in the crt sprite's own units (the monitor camera aims at it).</summary>
    private static readonly Vector2 CrtGlassCentre = new Vector2(-0.15f, 0.05f);""",
"""    /// <summary>Centre of the largest 4:3 rectangle inside the CRT's glass, in the crt sprite's own units (the screen anchor and the monitor camera sit on it).</summary>
    private static readonly Vector2 CrtGlassCentre = new Vector2(-0.144f, 0.054f);"""),
# --- BuildBooth: the live monitor, the framing, READY cleared ---
("""    /// <summary>
    /// Builds the world-space booth, two Cinemachine cameras, a Physics2DRaycaster,
    /// and wires OfficeViewController + the CRT/READY clickables. The painted art
    /// is laid out as in the art pass's 2D composition: the back wall and the deep
    /// desk cover the whole office view, partitions frame it, and the props sit on
    /// the desk and partitions. Idempotent.
    /// </summary>
    private static OfficeViewController BuildBooth(Canvas desktopCanvas)
    {""",
"""    /// <summary>
    /// Builds the world-space booth, two Cinemachine cameras, a Physics2DRaycaster
    /// and the live monitor (BuildMonitorScreen), and wires OfficeViewController,
    /// the camera rig, the CRT's click (focus) and the focus exit zone; READY's
    /// saved focus call is cleared (READY only releases GameManager's gate). The
    /// painted art is laid out as in the art pass's 2D composition: the back
    /// wall and the deep desk cover the whole office view, partitions frame it,
    /// and the props sit on the desk and partitions. Idempotent.
    /// </summary>
    private static OfficeViewController BuildBooth(Canvas desktopCanvas, DeskConfigSO config, out MonitorScreen screen)
    {"""),
("""        SpriteRenderer sign = PlaceSprite(booth, "ReadySign", EnsureOfficeSprite("sign", new Color(0.79f, 0.76f, 0.58f), 96, 50), new Vector3(0f, -2f, 0f), 2.3f, 2);
        Clickable signClick = EnsureClickable(sign);

        // Cameras: the booth view, and a close-up on the CRT's glass.
        GameObject camsRoot = GameObject.Find("Cameras") ?? new GameObject("Cameras");
        CinemachineCamera officeCam = EnsureVcam(camsRoot.transform, "OfficeVCam", OfficeCamPosition, OfficeOrthoSize);
        Vector3 glass = crt.transform.TransformPoint(CrtGlassCentre);
        CinemachineCamera monitorCam = EnsureVcam(camsRoot.transform, "MonitorVCam", new Vector3(glass.x, glass.y, OfficeCamPosition.z), MonitorOrthoSize);

        // Brain + 2D raycaster on the Main Camera.
        Camera main = Camera.main;
        if (main == null)
        {
            var mc = new GameObject("Main Camera", typeof(Camera));
            mc.tag = "MainCamera";
            main = mc.GetComponent<Camera>();
        }
        main.orthographic = true;
        if (main.GetComponent<CinemachineBrain>() == null) main.gameObject.AddComponent<CinemachineBrain>();
        Physics2DRaycaster raycaster = main.GetComponent<Physics2DRaycaster>();
        if (raycaster == null)
            raycaster = main.gameObject.AddComponent<Physics2DRaycaster>();

        // A fixed hit buffer keeps the UI module's per-frame booth raycast allocation-free.
        raycaster.maxRayIntersections = BoothRaycastHits;

        // Camera rig + view controller on OfficeRoot.
        CinemachineCameraRig rig = root.GetComponent<CinemachineCameraRig>();
        if (rig == null)
            rig = root.AddComponent<CinemachineCameraRig>();
        var soRig = new SerializedObject(rig);
        SetRef(soRig, "officeCam", officeCam);
        SetRef(soRig, "monitorCam", monitorCam);
        soRig.ApplyModifiedProperties();

        OfficeViewController view = root.GetComponent<OfficeViewController>();
        if (view == null)
            view = root.AddComponent<OfficeViewController>();
        var soView = new SerializedObject(view);
        SetRef(soView, "cameraRigBehaviour", rig);
        SetRef(soView, "desktopRoot", desktopCanvas.gameObject);
        soView.ApplyModifiedProperties();

        // CRT click -> focus monitor; READY click is wired to GameManager's gate,
        // but also focuses the monitor so the player lands on the desktop.
        WireClickToFocusMonitor(crtClick, view);
        WireClickToFocusMonitor(signClick, view);

        // Diegetic readouts + a timeline-reactive poster + the desktop Back button.
        BuildReadouts(booth, calendarPartition.transform);
        BuildReactiveProp(booth);
        BuildBackToOfficeButton(desktopCanvas, view);

        return view;
    }""",
"""        SpriteRenderer sign = PlaceSprite(booth, "ReadySign", EnsureOfficeSprite("sign", new Color(0.79f, 0.76f, 0.58f), 96, 50), new Vector3(0f, -2f, 0f), 2.3f, 2);
        Clickable signClick = EnsureClickable(sign);

        // Brain + 2D raycaster on the Main Camera.
        Camera main = Camera.main;
        if (main == null)
        {
            var mc = new GameObject("Main Camera", typeof(Camera));
            mc.tag = "MainCamera";
            main = mc.GetComponent<Camera>();
        }
        main.orthographic = true;
        if (main.GetComponent<CinemachineBrain>() == null) main.gameObject.AddComponent<CinemachineBrain>();
        Physics2DRaycaster raycaster = main.GetComponent<Physics2DRaycaster>();
        if (raycaster == null)
            raycaster = main.gameObject.AddComponent<Physics2DRaycaster>();

        // A fixed hit buffer keeps the UI module's per-frame booth raycast allocation-free.
        raycaster.maxRayIntersections = BoothRaycastHits;

        // The live monitor: the desktop canvas on the glass, its power button and LED, the focus zones.
        screen = BuildMonitorScreen(crt, desktopCanvas, main, config);

        // Cameras: the booth view, and a push-in on the glass. The monitor camera
        // starts framed for the reference aspect; the rig reframes it for the
        // screen's aspect on every push-in (MonitorFraming, the fill knob).
        GameObject camsRoot = GameObject.Find("Cameras") ?? new GameObject("Cameras");
        CinemachineCamera officeCam = EnsureVcam(camsRoot.transform, "OfficeVCam", OfficeCamPosition, OfficeOrthoSize);
        Vector3 glass = screen.GlassCentre;
        Vector2 glassSize = screen.GlassWorldSize;
        float monitorOrtho = MonitorFraming.OrthoSize(glassSize.x, glassSize.y, ReferenceResolution.x / ReferenceResolution.y, config.monitorFill);
        CinemachineCamera monitorCam = EnsureVcam(camsRoot.transform, "MonitorVCam", new Vector3(glass.x, glass.y, OfficeCamPosition.z), monitorOrtho);

        // Camera rig + view controller on OfficeRoot.
        CinemachineCameraRig rig = root.GetComponent<CinemachineCameraRig>();
        if (rig == null)
            rig = root.AddComponent<CinemachineCameraRig>();
        var soRig = new SerializedObject(rig);
        SetRef(soRig, "officeCam", officeCam);
        SetRef(soRig, "monitorCam", monitorCam);
        SetRef(soRig, "brain", main.GetComponent<CinemachineBrain>());
        SetRef(soRig, "monitorScreen", screen);
        SetRef(soRig, "config", config);
        soRig.ApplyModifiedProperties();

        OfficeViewController view = root.GetComponent<OfficeViewController>();
        if (view == null)
            view = root.AddComponent<OfficeViewController>();
        var soView = new SerializedObject(view);
        SetRef(soView, "cameraRigBehaviour", rig);
        SetRef(soView, "desktopRoot", desktopCanvas.gameObject);
        SetRef(soView, "monitorScreen", screen);
        soView.ApplyModifiedProperties();

        // The CRT's click pushes in; a click outside the screen pulls back.
        // READY only releases GameManager's gate (no zoom): its saved focus call goes.
        WireClickToFocusMonitor(crtClick, view);
        WirePersistentVoid(crt.transform.Find("FocusExitZone").GetComponent<Clickable>(), "onClick", view, nameof(OfficeViewController.FocusOffice));
        ClearPersistentCalls(signClick, "onClick");

        // Diegetic readouts + a timeline-reactive poster. The desktop's old
        // "< Office" button is replaced by the taskbar's "< Desk" (BuildDesktopShell).
        BuildReadouts(booth, calendarPartition.transform);
        BuildReactiveProp(booth);
        DestroyChildIfPresent(desktopCanvas.transform, "BackToOfficeButton");

        return view;
    }"""),
# --- the taskbar's way back and the Start menu: Settings, Turn off screen, Quit game ---
("""    /// <summary>Adds a visible "Back to Office" button to the desktop canvas.</summary>
    private static void BuildBackToOfficeButton(Canvas desktopCanvas, OfficeViewController view)
    {
        Button back = MakeButton(desktopCanvas.transform, "BackToOfficeButton", "< Office",
            new Vector2(0.005f, 0.93f), new Vector2(0.105f, 0.99f), new Color(0.2f, 0.3f, 0.5f, 0.95f));
        WirePersistentVoid(back, "m_OnClick", view, nameof(OfficeViewController.FocusOffice));
    }

    /// <summary>
    /// Builds the fake-OS desktop shell: a left column of icons (some unlock-gated
    /// by a library upgrade id, reported when the library does not know it) that
    /// open placeholder windows with min/max/close chrome, plus a Start menu
    /// (Settings + Power) wired to a DesktopShell on the canvas. Idempotent.
    /// </summary>
    private static void BuildDesktopShell(Canvas canvas, Transform iconGrid, Transform windowLayer, ContentLibrarySO library)
    {""",
"""    /// <summary>
    /// Builds the fake-OS desktop shell: a left column of icons (some unlock-gated
    /// by a library upgrade id, reported when the library does not know it) that
    /// open placeholder windows with min/max/close chrome, a Start menu
    /// (Settings, Turn off screen and Quit game) wired to a DesktopShell on the
    /// canvas, and the taskbar's "&lt; Desk" button (FocusOffice; built here, after
    /// the view exists). Idempotent.
    /// </summary>
    private static void BuildDesktopShell(Canvas canvas, Transform iconGrid, Transform windowLayer, ContentLibrarySO library,
                                          OfficeViewController view, MonitorScreen screen)
    {"""),
("""        // Rebuilt from scratch each run: the entries need fixed LayoutElement
        // heights or the vertical layout collapses them on top of each other.
        DestroyChildIfPresent(root, "StartMenu");
        Transform startMenu = Panel(root, "StartMenu", new Vector2(0f, 0f), new Vector2(0.14f, 0f), new Vector2(0f, 96f), new Vector2(0f, 112f), new Color(0.1f, 0.12f, 0.18f, 0.97f));
        AddVLayout(startMenu, 4f);
        Button settingsEntry = MakeButton(startMenu, "SettingsEntry", "Settings", Vector2.zero, Vector2.one, new Color(0.2f, 0.25f, 0.35f, 1f));
        SetLayoutHeight(settingsEntry, 46f);
        Button powerEntry = MakeButton(startMenu, "PowerEntry", "Power", Vector2.zero, Vector2.one, new Color(0.5f, 0.2f, 0.2f, 1f));
        SetLayoutHeight(powerEntry, 46f);
        startMenu.gameObject.SetActive(false);
""",
"""        // Rebuilt from scratch each run: the entries need fixed LayoutElement
        // heights or the vertical layout collapses them on top of each other
        // (three 46-px entries, 4 px apart, 6 px padding: 158 px).
        DestroyChildIfPresent(root, "StartMenu");
        Transform startMenu = Panel(root, "StartMenu", new Vector2(0f, 0f), new Vector2(0.2f, 0f), new Vector2(0f, 119f), new Vector2(0f, 158f), new Color(0.1f, 0.12f, 0.18f, 0.97f));
        AddVLayout(startMenu, 4f);
        Button settingsEntry = MakeButton(startMenu, "SettingsEntry", "Settings", Vector2.zero, Vector2.one, new Color(0.2f, 0.25f, 0.35f, 1f));
        SetLayoutHeight(settingsEntry, 46f);
        Button screenOffEntry = MakeButton(startMenu, "ScreenOffEntry", "Turn off screen", Vector2.zero, Vector2.one, new Color(0.2f, 0.25f, 0.35f, 1f));
        SetLayoutHeight(screenOffEntry, 46f);
        Button quitEntry = MakeButton(startMenu, "QuitEntry", "Quit game", Vector2.zero, Vector2.one, new Color(0.5f, 0.2f, 0.2f, 1f));
        SetLayoutHeight(quitEntry, 46f);
        startMenu.gameObject.SetActive(false);

        // The taskbar's way back to the booth, next to Start.
        Transform taskbar = root.Find("Taskbar");
        Button deskButton = MakeButton(taskbar, "DeskButton", "< Desk", new Vector2(0.125f, 0.1f), new Vector2(0.245f, 0.9f), new Color(0.2f, 0.3f, 0.5f, 0.95f));
        SetAnchors(deskButton.transform, new Vector2(0.125f, 0.1f), new Vector2(0.245f, 0.9f));
        WirePersistentVoid(deskButton, "m_OnClick", view, nameof(OfficeViewController.FocusOffice));
"""),
("""        SetRef(soShell, "settingsButton", settingsEntry);
        SetRef(soShell, "powerButton", powerEntry);
        SetRef(soShell, "settingsWindow", settings);""",
"""        SetRef(soShell, "settingsButton", settingsEntry);
        SetRef(soShell, "quitButton", quitEntry);
        SetRef(soShell, "screenOffButton", screenOffEntry);
        SetRef(soShell, "monitorScreen", screen);
        SetRef(soShell, "settingsWindow", settings);"""),
# --- one home for emptying a UnityEvent's persistent calls (ClearPersistentCalls, the Desk partial) ---
("""    /// <summary>
    /// Wires a single persistent, parameterless (Void) call on a UnityEvent
    /// serialized property (e.g. Clickable "onClick" or Button "m_OnClick").
    /// </summary>
    private static void WirePersistentVoid(Object host, string eventProp, Object target, string method)
    {
        var so = new SerializedObject(host);
        SerializedProperty calls = so.FindProperty(eventProp + ".m_PersistentCalls.m_Calls");
        if (calls == null)
            return;

        calls.ClearArray();
        calls.InsertArrayElementAtIndex(0);""",
"""    /// <summary>
    /// Wires a single persistent, parameterless (Void) call on a UnityEvent
    /// serialized property (e.g. Clickable "onClick" or Button "m_OnClick"),
    /// replacing the calls ClearPersistentCalls empties.
    /// </summary>
    private static void WirePersistentVoid(Object host, string eventProp, Object target, string method)
    {
        var so = new SerializedObject(host);
        SerializedProperty calls = ClearPersistentCalls(so, eventProp);
        if (calls == null)
            return;

        calls.InsertArrayElementAtIndex(0);"""),
])
```

Expected: `ok …OfficeSceneUIBuilder.cs lf`.

- [ ] **Step 3: Compile check and run**

Expected: exit 0 / exit 0; `passed 433, failed 0`.

- [ ] **Step 4: Commit**

```bash
cd /e/unity/NOPE-feat-clock && python "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad/make_meta.py" Assets/Editor/OfficeSceneUIBuilder.Desk.cs && git add Assets/Editor/OfficeSceneUIBuilder.Desk.cs Assets/Editor/OfficeSceneUIBuilder.Desk.cs.meta Assets/Editor/OfficeSceneUIBuilder.cs && git commit -F - <<'EOF'
feat(builder): the desktop on the CRT's glass, the push-in and screen power

Build Office UI puts the desktop canvas on the CRT's glass (World Space,
clipped), adds the bezel's power button, LED and glass zone, frames the
monitor camera from the glass and wires the camera rig's brain. READY only
calls the next traveller; the taskbar's "< Desk" and the Start menu's Turn
off screen and Quit game replace the Back button and Power. The booth and
desk parts live in the new OfficeSceneUIBuilder.Desk.cs.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 16: The builder: the 4:3 desktop, the traveller wheel and the callouts

Spec §2.15 items 7–9, §1.10, §2.17, K2, K3, K10, K17, K18, R4, R5, R17, R18, R23, R29, R33, R34: the desktop is laid out for 1440×1080 (the CRT's 4:3 glass): the wallpaper fills it without squeezing (`AspectRatioFitter`, mipmaps on), the transcript moves to the freed right side, and the controller's window-layout fields get the 4:3 values. The pre-investigation leftovers a live monitor would show go (`VisitorText`, `Doc1Text`, `Doc2Text`, `ResultText`, `EraButtonsRoot`, R18), and an idle line ("Waiting for the next traveller") fills the screen between travellers. The overlay gains the traveller wheel (an always-active host with a `Catcher`, a `Ring` laid out by `RadialLayoutGroup` whose `Centre` ignores the layout, R33) and two `OverlayCallout`s (`SpeechBubble`, `DeskTooltip`, R34). The intercom panel is retired: the ring is the interaction panel. The builder reports a wheel that fits fewer choices than the content's menu capacity. The Scanner app becomes "Deviation Report" and Material "Material Analysis"; icon labels are set on every build.

**Files:**
- Modify: `Assets/Editor/OfficeSceneUIBuilder.cs`, `Assets/Editor/OfficeSceneUIBuilder.Desk.cs`

- [ ] **Step 1: Apply** — save as `SCRATCH/p7_t16.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'
B = W + r'\Assets\Editor\OfficeSceneUIBuilder.cs'

apply(W + r'\Assets\Editor\OfficeSceneUIBuilder.Desk.cs', [
("""using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
""",
"""using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
"""),
])

apply(B, [
# --- class summary: the interview on the wheel ---
("""///   windows, a reference-book shelf with openable book windows, the intercom
///   and the interview transcript, a visual compare bar, and Accept/Deny
///   buttons  [InvestigationUIController + CompareController]""",
"""///   windows, a reference-book shelf with openable book windows, the interview
///   transcript, a visual compare bar, and Accept/Deny buttons, laid out for
///   the 4:3 desktop  [InvestigationUIController + CompareController]
/// - The traveller wheel (the interview's choices around the traveller), the
///   speech bubble and the desk tooltip on the office overlay canvas
///   [TravellerWheel, OverlayCallout]"""),
# --- the leftovers of the pre-investigation UI, which a live monitor would show (R18) ---
("""        // XP desktop wallpaper (behind everything) + taskbar with system-tray HUD.
        BuildDesktop(root);""",
"""        // Leftovers of the pre-investigation era UI under the OfficeUIController's
        // object: a live monitor would show them. Its HUD and citation wiring stay
        // (Test_DayLoop runs the legacy fields from its own scene).
        foreach (string leftover in new[] { "VisitorText", "Doc1Text", "Doc2Text", "ResultText", "EraButtonsRoot" })
            DestroyChildIfPresent(officeUI.transform, leftover);

        // XP desktop wallpaper (behind everything), the idle line between
        // travellers + taskbar with system-tray HUD.
        GameObject idleScreen = BuildDesktop(root);"""),
# --- (a) the overlay parts, right after the newsletters and the desk tuning ---
("""        // The desk tuning (created once): the monitor push-in, screen power, sorting bands.
        DeskConfigSO deskConfig = EnsureDeskConfig();
""",
"""        // The desk tuning (created once): the monitor push-in, screen power, sorting bands, the wheel.
        DeskConfigSO deskConfig = EnsureDeskConfig();

        // Booth-view overlays, rebuilt each run with always-active hosts: the
        // traveller's speech bubble, the desk props' tooltip, and the traveller
        // wheel (the interview's choices; its ring replaces the retired intercom).
        OverlayCallout speechBubble = BuildOverlayCallout(officeCanvas.transform, "SpeechBubble", new Vector2(420f, 110f), new Color(0.98f, 0.97f, 0.93f, 0.97f));
        OverlayCallout deskTooltip = BuildOverlayCallout(officeCanvas.transform, "DeskTooltip", new Vector2(360f, 60f), Tooltip);
        TravellerWheel wheel = BuildTravellerWheel(officeCanvas.transform, deskConfig, speechBubble);
        InteractionPanelController interaction = wheel.transform.Find("Catcher/Ring").GetComponent<InteractionPanelController>();
"""),
# --- the Deviation Report (the desk device is the scanner, K17) ---
("""        // Scanner is the deviation report: its body lists the discrepancies the
        // player has documented for the current case (drives deny gating).
        DestroyChildIfPresent(windowLayer, "IconScannerWindow");
        OSWindowChrome scannerWindow = BuildOSWindow(windowLayer, "IconScannerWindow", "Scanner — Deviation Report",
            "No deviations documented.", new Vector2(560f, 420f));
        TMP_Text scannerText = scannerWindow.transform.Find("Body").GetComponent<TMP_Text>();
        BuildDesktopIcon(bookShelf, "IconScanner", "Scanner", scannerWindow, "");""",
"""        // The Deviation Report: its body lists the discrepancies the player has
        // documented for the current case (drives deny gating). Its object names
        // keep "Scanner"; the desk device is the scanner.
        DestroyChildIfPresent(windowLayer, "IconScannerWindow");
        OSWindowChrome scannerWindow = BuildOSWindow(windowLayer, "IconScannerWindow", "Deviation Report",
            "No deviations documented.", new Vector2(560f, 420f));
        TMP_Text scannerText = scannerWindow.transform.Find("Body").GetComponent<TMP_Text>();
        BuildDesktopIcon(bookShelf, "IconScanner", "Deviation Report", scannerWindow, "");"""),
# --- the intercom panel is retired: the wheel's ring shows the interview (K3) ---
("""        // Intercom: the interview's choices (document requests, questions,
        // dialog replies), provided at runtime by InvestigationUIController.
        // The layout numbers also give how many choices it shows at once: at
        // the reference height, the least height the canvas ever has.
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
        int intercomFit = Mathf.FloorToInt((actionsHeight - 2 * VLayoutPadding + actionSpacing) / (actionHeight + actionSpacing));
        actionTemplate.gameObject.SetActive(false);
        InteractionPanelController interaction = intercom.GetComponent<InteractionPanelController>();
        if (interaction == null)
            interaction = intercom.gameObject.AddComponent<InteractionPanelController>();
        var soInteract = new SerializedObject(interaction);
        SetRef(soInteract, "actionsRoot", intercomActions);
        SetRef(soInteract, "actionButtonTemplate", actionTemplate);
        soInteract.ApplyModifiedProperties();
""",
"""        // The intercom panel is retired: the traveller wheel's ring shows the interview.
        DestroyChildIfPresent(investRoot, "IntercomPanel");
"""),
# --- the transcript takes the freed right side, clear of the compare bar ---
("""        Transform transcriptWin = Panel(windowLayer, "TranscriptWindow", Center, Center, new Vector2(220f, 70f), new Vector2(620f, 460f), Paper);""",
"""        Transform transcriptWin = Panel(windowLayer, "TranscriptWindow", Center, Center, new Vector2(395f, 60f), new Vector2(620f, 460f), Paper);"""),
# --- the wheel's fit, against the content's menu capacity ---
("""        if (library != null && library.Interview != null && library.Interview.menuCapacity > intercomFit)
            Debug.LogError($"[TimeDesk] The intercom fits {intercomFit} choices, but the content library's interview menu capacity is {library.Interview.menuCapacity}; lower interview.menuCapacity in world_source.json or enlarge the intercom.");""",
"""        if (library != null && library.Interview != null)
        {
            int wheelFit = RadialLayout.MaxFit(deskConfig.wheelRadii.x, deskConfig.wheelRadii.y, deskConfig.wheelItemSize.x, deskConfig.wheelItemSize.y,
                                               deskConfig.wheelCentreSize.x, deskConfig.wheelCentreSize.y, deskConfig.wheelItemGap, library.Interview.menuCapacity);
            if (wheelFit < library.Interview.menuCapacity)
                Debug.LogError($"[TimeDesk] The traveller wheel fits {wheelFit} choices, but the content library's interview menu capacity is {library.Interview.menuCapacity}; lower interview.menuCapacity in world_source.json or enlarge the wheel (Desk_Default: wheelRadii, wheelItemSize).");
        }"""),
# --- wiring: the wheel's ring, the wheel, the idle line, the 4:3 window layout ---
("""        SetRef(soInvest, "transcriptWindow", transcript);
        SetRef(soInvest, "transcriptChrome", transcriptChrome);
        soInvest.ApplyModifiedProperties();""",
"""        SetRef(soInvest, "transcriptWindow", transcript);
        SetRef(soInvest, "transcriptChrome", transcriptChrome);
        SetRef(soInvest, "wheel", wheel);
        SetRef(soInvest, "idleScreen", idleScreen);
        // The 4:3 desktop: documents cascade on the left, clear of the icon
        // column; books open in two staggered rows (the fields' defaults are
        // the 16:9 layout a scene that is not rebuilt keeps).
        soInvest.FindProperty("documentWindowOrigin").vector2Value = new Vector2(-195f, 150f);
        soInvest.FindProperty("documentWindowStep").vector2Value = new Vector2(40f, -40f);
        soInvest.FindProperty("bookWindowOrigin").vector2Value = new Vector2(-180f, -150f);
        soInvest.FindProperty("bookWindowColumnStep").floatValue = 300f;
        soInvest.FindProperty("bookWindowRowStep").vector2Value = new Vector2(40f, 40f);
        soInvest.ApplyModifiedProperties();"""),
("""        Debug.Log("[TimeDesk] Office investigation desk built and wired (live monitor on the CRT with screen power, HUD, citation, briefing/results, claim, document + book windows, intercom + interview transcript, compare, Accept/Deny, GameManager, DaySystem). Save the scene.");""",
"""        Debug.Log("[TimeDesk] Office investigation desk built and wired (live monitor on the CRT with screen power, HUD, citation, briefing/results, claim, document + book windows, traveller wheel + interview transcript, speech bubble, compare, Accept/Deny, GameManager, DaySystem). Save the scene.");"""),
# --- the wallpaper fills the 4:3 screen without squeezing; the idle line ---
("""    private static void BuildDesktop(Transform root)
    {
        Transform desk = Panel(root, "Desktop", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.23f, 0.45f, 0.74f, 1f));
        Image img = desk.GetComponent<Image>();
        Sprite wall = EnsureWallpaper();
        if (img != null && wall != null)
        {
            img.sprite = wall;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.color = Color.white; // don't tint the wallpaper with the fallback color
        }
        desk.SetAsFirstSibling();
    }""",
"""    /// <summary>
    /// The wallpaper (behind everything; it envelopes the 4:3 desktop at its own
    /// aspect, the overflow clipped by the canvas's mask) and, right above it,
    /// the idle line shown between travellers (inactive; the investigation
    /// controller shows it). Returns the idle line's object.
    /// </summary>
    private static GameObject BuildDesktop(Transform root)
    {
        Transform desk = Panel(root, "Desktop", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.23f, 0.45f, 0.74f, 1f));
        Image img = desk.GetComponent<Image>();
        Sprite wall = EnsureWallpaper();
        if (img != null && wall != null)
        {
            img.sprite = wall;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.color = Color.white; // don't tint the wallpaper with the fallback color
        }
        AspectRatioFitter fitter = desk.GetComponent<AspectRatioFitter>();
        if (fitter == null)
            fitter = desk.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = wall != null ? wall.rect.width / wall.rect.height : ReferenceResolution.x / ReferenceResolution.y;
        desk.SetAsFirstSibling();

        // Between travellers the live monitor reads this, large enough for the booth view.
        TMP_Text idle = Text(root, "IdleText", "Waiting for the next traveller", 80, TextAlignmentOptions.Center, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.62f), Color.white);
        idle.fontStyle = FontStyles.Bold;
        idle.raycastTarget = false;
        idle.transform.SetSiblingIndex(1);
        idle.gameObject.SetActive(false);
        return idle.gameObject;
    }"""),
("""    private static Sprite EnsureWallpaper()
    {
        const string assetPath = "Assets/Art/Generated/xp_bliss.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (existing != null)
            return existing;

        EnsureFolderTree("Assets/Art/Generated");
""",
"""    /// <summary>
    /// The wallpaper sprite (a generated placeholder when the art is missing),
    /// with mipmaps on: the live monitor shows it small in the booth view, where
    /// a texture without mipmaps shimmers (K18).
    /// </summary>
    private static Sprite EnsureWallpaper()
    {
        const string assetPath = "Assets/Art/Generated/xp_bliss.png";
        if (AssetDatabase.LoadAssetAtPath<Sprite>(assetPath) == null)
            GenerateWallpaper(assetPath);

        if (AssetImporter.GetAtPath(assetPath) is TextureImporter imp && !imp.mipmapEnabled)
        {
            imp.mipmapEnabled = true;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    /// <summary>Writes the placeholder wallpaper (an XP "Bliss"-like hill and sky) as a sprite.</summary>
    private static void GenerateWallpaper(string assetPath)
    {
        EnsureFolderTree("Assets/Art/Generated");
"""),
("""        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        if (AssetImporter.GetAtPath(assetPath) is TextureImporter imp)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }""",
"""        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        if (AssetImporter.GetAtPath(assetPath) is TextureImporter imp)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.SaveAndReimport();
        }
    }"""),
# --- icon labels follow the builder; Material Analysis ---
("""            ("IconMaterial", "Material", "Material Scanner", "Flags tech/materials beyond the claimed era. (upgrade)", "adv_scanner"),""",
"""            ("IconMaterial", "Material", "Material Analysis", "Flags tech/materials beyond the claimed era. (upgrade)", "adv_scanner"),"""),
("""        // BuildOSWindow keeps an existing window's texts, so the renamed Dialect window is rebuilt.
        DestroyChildIfPresent(windowLayer, "IconDialectWindow");""",
"""        // BuildOSWindow keeps an existing window's texts, so the renamed Dialect and Material windows are rebuilt.
        DestroyChildIfPresent(windowLayer, "IconDialectWindow");
        DestroyChildIfPresent(windowLayer, "IconMaterialWindow");"""),
("""        Button btn = MakeButton(grid, name, label, Vector2.zero, Vector2.one, new Color(0.2f, 0.3f, 0.45f, 0.85f));
        FitIconLabel(btn);""",
"""        Button btn = MakeButton(grid, name, label, Vector2.zero, Vector2.one, new Color(0.2f, 0.3f, 0.45f, 0.85f));
        Transform labelObject = btn.transform.Find("Label");
        TMP_Text labelText = labelObject != null ? labelObject.GetComponent<TMP_Text>() : null;
        if (labelText != null)
            labelText.text = label; // an existing icon keeps its place in the grid; its label follows the builder
        FitIconLabel(btn);"""),
("""    /// Lets a desktop icon's label shrink to fit its tile, wrapping only between
    /// words: auto-sizing shrinks a word that does not fit the tile's width
    /// instead of breaking it. Re-applied on every build (MakeButton keeps an
    /// existing label as it is).""",
"""    /// Lets a desktop icon's label shrink to fit its tile, wrapping only between
    /// words: auto-sizing shrinks a word that does not fit the tile's width
    /// instead of breaking it. Re-applied on every build (MakeButton keeps an
    /// existing label as it is; the label text is set by BuildDesktopIcon)."""),
])
```

Expected: `ok …OfficeSceneUIBuilder.Desk.cs lf`, `ok …OfficeSceneUIBuilder.cs lf`.

- [ ] **Step 2: The wheel and callout builders** — save as `SCRATCH/p7_t16_desk.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'
D = W + r'\Assets\Editor\OfficeSceneUIBuilder.Desk.cs'

# The overlay parts of the booth: the traveller wheel and the two callouts (R33, R34).
apply(D, [
("""/// The office builder's booth and desk parts (the physical desk, piece 7):
/// the live monitor on the CRT, the desk tuning asset and the shared hit-zone
/// helpers.""",
"""/// The office builder's booth and desk parts (the physical desk, piece 7):
/// the live monitor on the CRT, the traveller wheel and the overlay callouts,
/// the desk tuning asset and the shared hit-zone helpers."""),
("""            calls.ClearArray();
        return calls;
    }
}
""",
"""            calls.ClearArray();
        return calls;
    }

    /// <summary>
    /// An overlay callout (a timed label that takes no clicks) under the office
    /// overlay canvas, rebuilt each run: an always-active full-screen host with
    /// no graphic, and its Panel child (anchors and pivot (0.5, 0.5), raycast
    /// targets off, inactive) holding an auto-sized label.
    /// </summary>
    private static OverlayCallout BuildOverlayCallout(Transform overlay, string name, Vector2 size, Color background)
    {
        DestroyChildIfPresent(overlay, name);
        Transform host = Panel(overlay, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        Transform panel = Panel(host, "Panel", Center, Center, Vector2.zero, size, background);
        ((RectTransform)panel).pivot = Center;
        panel.GetComponent<Image>().raycastTarget = false;

        TMP_Text label = Text(panel, "Label", "", 24, TextAlignmentOptions.Center, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), Ink);
        label.enableAutoSizing = true;
        label.fontSizeMin = 14f;
        label.fontSizeMax = 24f;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;

        OverlayCallout callout = host.gameObject.AddComponent<OverlayCallout>();
        var so = new SerializedObject(callout);
        SetRef(so, "panel", panel);
        SetRef(so, "label", label);
        so.ApplyModifiedProperties();

        panel.gameObject.SetActive(false);
        return callout;
    }

    /// <summary>
    /// The traveller wheel under the office overlay canvas, rebuilt each run: an
    /// always-active full-screen host (TravellerWheel, no graphic); its Catcher,
    /// a full-screen transparent click-to-close area, inactive; the Ring under it
    /// (anchors and pivot (0.5, 0.5), placed by projection) with the choice
    /// renderer (InteractionPanelController, its template stretched so a centre
    /// clone fills the centre slot), the RadialLayoutGroup and the Centre slot
    /// ("&lt; Back", ignored by the layout). The wheel's traveller is set by
    /// BuildDeskInteraction, once the traveller view exists.
    /// </summary>
    private static TravellerWheel BuildTravellerWheel(Transform overlay, DeskConfigSO config, OverlayCallout bubble)
    {
        DestroyChildIfPresent(overlay, "TravellerWheel");
        Transform host = Panel(overlay, "TravellerWheel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
        Transform catcher = Panel(host, "Catcher", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));

        Transform ring = Panel(catcher, "Ring", Center, Center, Vector2.zero, Vector2.zero, null);
        ((RectTransform)ring).pivot = Center;
        RadialLayoutGroup layout = ring.gameObject.AddComponent<RadialLayoutGroup>();
        layout.Radii = config.wheelRadii;
        layout.ItemSize = config.wheelItemSize;

        Transform centre = Panel(ring, "Centre", Center, Center, Vector2.zero, config.wheelCentreSize, null);
        ((RectTransform)centre).pivot = Center;
        centre.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        Button template = MakeButton(ring, "ActionButtonTemplate", "Choice", Vector2.zero, Vector2.one, new Color(0.16f, 0.28f, 0.42f, 0.95f));
        TMP_Text choice = template.transform.Find("Label").GetComponent<TMP_Text>();
        choice.enableAutoSizing = true;
        choice.fontSizeMin = 12f;
        choice.fontSizeMax = 20f;
        choice.textWrappingMode = TextWrappingModes.Normal;
        template.gameObject.SetActive(false);

        InteractionPanelController panel = ring.gameObject.AddComponent<InteractionPanelController>();
        var soPanel = new SerializedObject(panel);
        SetRef(soPanel, "actionsRoot", ring);
        SetRef(soPanel, "actionButtonTemplate", template);
        SetRef(soPanel, "centreSlot", centre);
        soPanel.ApplyModifiedProperties();

        TravellerWheel wheel = host.gameObject.AddComponent<TravellerWheel>();
        var so = new SerializedObject(wheel);
        SetRef(so, "catcher", catcher.gameObject);
        SetRef(so, "ring", ring);
        SetRef(so, "layout", layout);
        SetRef(so, "centreSlot", centre);
        SetRef(so, "bubble", bubble);
        SetRef(so, "config", config);
        so.ApplyModifiedProperties();

        catcher.gameObject.SetActive(false);
        return wheel;
    }
}
"""),
])
```

Expected: `ok …OfficeSceneUIBuilder.Desk.cs lf`.

- [ ] **Step 3: Compile check and run**

Expected: exit 0 / exit 0; `passed 433, failed 0`.

- [ ] **Step 4: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Editor/OfficeSceneUIBuilder.cs Assets/Editor/OfficeSceneUIBuilder.Desk.cs && git commit -F - <<'EOF'
feat(builder): a 4:3 desktop, the traveller wheel and the overlay callouts

The desktop is laid out for the CRT's 4:3 glass, with an idle line between
travellers and without the pre-investigation leftovers. The overlay gains
the traveller wheel (its ring is the interview's panel, "< Back" in the
centre), the speech bubble and the desk tooltip; the intercom panel is
retired and the wheel's fit is checked against the menu capacity. The
Scanner reads "Deviation Report" and Material "Material Analysis".

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 17: The builder: the desk, its props and the booth's wiring

Spec §2.15 items 10 and 12, §2.16, K4, K11, K12, K13, K14, K16, K20, V8, R6, R16, R20, R21, R24, R32: `BuildBooth` adds the traveller's view (anchor and `TravellerHitZone`), the desk slots (plant, mug and photo decorations, two free spots; the plant and mug move onto theirs) and the desk (`DeskSurface`, the scanner's drop area and bed on the tray, the paper template, `DeskController`). After the wall clock exists, the new `BuildDeskInteraction` gives the ten props their reactions (the stamp, mug, plant, poster, intercom and tray animate; the till, calendar, stability monitor and clock show tooltips; `DeskItem` ids on the four decorations), a calendar hit zone, the traveller's hit zone and the intercom calling `TravellerWheel.Open`, the scan and wheel notes, and wires `BoothCoordinator`, the controller's desk, `GameManager`'s traveller view and booth. The builder reports overlapping sorting bands (`SortingBands.Problems`) and fewer spawn slots than a traveller's papers (`ContentLibraryValidator.MaxDocuments` over `TravellerBlueprints`, the validator's own blueprints), and the booth's raycaster holds 16 hits (R24). `docs/FEATURES.md` does not change here: players get the builder's behaviour only when the rebuilt `OfficeScene` is committed, so its lines ship with that commit (Task 19 Steps 7–8).

**Files:**
- Modify: `Assets/Editor/OfficeSceneUIBuilder.cs`, `Assets/Editor/OfficeSceneUIBuilder.Desk.cs`

- [ ] **Step 1: The call sites and the wiring** — save as `SCRATCH/p7_t17.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'
B = W + r'\Assets\Editor\OfficeSceneUIBuilder.cs'

apply(B, [
# --- class summary ---
("""/// - The traveller wheel (the interview's choices around the traveller), the
///   speech bubble and the desk tooltip on the office overlay canvas
///   [TravellerWheel, OverlayCallout]""",
"""/// - The traveller wheel (the interview's choices around the traveller), the
///   speech bubble and the desk tooltip on the office overlay canvas
///   [TravellerWheel, OverlayCallout]
/// - The physical desk: papers and the desk scanner, reacting props,
///   decoration slots, the traveller (shown while at the desk) and its hit
///   zone, and the booth's input rules  [DeskController, DeskReaction,
///   TravellerView, BoothCoordinator]"""),
# --- (d) the desk's clicks, after the wall clock exists; (e) the wiring ---
("""        BuildShiftClockReadouts(shiftClock, trayClockText);
""",
"""        BuildShiftClockReadouts(shiftClock, trayClockText);

        // The booth's clicks, once every prop exists (the wall clock above):
        // reactions, hit zones, the wheel's openers and notes, the coordinator.
        BoothCoordinator booth = BuildDeskInteraction(officeView, monitorScreen, deskConfig, wheel, deskTooltip, trayClockText, library);
"""),
("""        SetRef(soInvest, "wheel", wheel);
        SetRef(soInvest, "idleScreen", idleScreen);""",
"""        SetRef(soInvest, "desk", officeView.transform.Find("DeskSurface").GetComponent<DeskController>());
        SetRef(soInvest, "wheel", wheel);
        SetRef(soInvest, "idleScreen", idleScreen);"""),
("""        SetRef(soGm, "shiftClock", shiftClock);
        soGm.ApplyModifiedProperties();""",
"""        SetRef(soGm, "shiftClock", shiftClock);
        SetRef(soGm, "travellerView", officeView.transform.Find("Traveller").GetComponent<TravellerView>());
        SetRef(soGm, "booth", booth);
        soGm.ApplyModifiedProperties();"""),
("""        Debug.Log("[TimeDesk] Office investigation desk built and wired (live monitor on the CRT with screen power, HUD, citation, briefing/results, claim, document + book windows, traveller wheel + interview transcript, speech bubble, compare, Accept/Deny, GameManager, DaySystem). Save the scene.");""",
"""        Debug.Log("[TimeDesk] Office investigation desk built and wired (live monitor on the CRT with screen power, the desk with papers, scanner and reacting props, traveller + wheel + speech bubble, booth input rules, HUD, citation, briefing/results, claim, document + book windows, interview transcript, compare, Accept/Deny, GameManager, DaySystem). Save the scene.");"""),
# --- (b) the booth's geometry: the traveller's view, the slots, the desk ---
("""    /// Builds the world-space booth, two Cinemachine cameras, a Physics2DRaycaster
    /// and the live monitor (BuildMonitorScreen), and wires OfficeViewController,""",
"""    /// Builds the world-space booth, two Cinemachine cameras, a Physics2DRaycaster,
    /// the live monitor (BuildMonitorScreen), the traveller's view, the desk
    /// slots and the desk (BuildDesk: its surface, the paper template, the
    /// scanner and its note), and wires OfficeViewController,"""),
("""        // The traveller stands behind the desk, whose far edge hides the placeholder's lower part.
        PlaceSprite(booth, "Traveller", EnsureOfficeSprite("traveller", new Color(0.49f, 0.42f, 0.86f), 60, 110), new Vector3(0f, 0.5f, 2f), 2f, -20);

        // Desk props (decoration only).
        PlaceSprite(booth, "DeskIntercom", BoothArt("intercom"), new Vector3(-6.3f, -1.8f, 0f), 2.1f, 1);
        PlaceSprite(booth, "ScannerTray", BoothArt("scanner_tray"), new Vector3(-4.6f, -3.1f, 0f), 3.8f, 2);
        PlaceSprite(booth, "DeskPlant", BoothArt("desk_plant"), new Vector3(-7.5f, -1.15f, 0f), 1.8f, 3);
        PlaceSprite(booth, "DeskMug", BoothArt("desk_mug"), new Vector3(-6.5f, -4.8f, 0f), 0.85f, 5);
        PlaceSprite(booth, "DeskStamp", BoothArt("desk_stamp"), new Vector3(-2.2f, -4.3f, 0f), 0.8f, 5);""",
"""        // The traveller stands behind the desk, whose far edge hides the
        // placeholder's lower part; shown from presentation until the decision.
        SpriteRenderer traveller = PlaceSprite(booth, "Traveller", EnsureOfficeSprite("traveller", new Color(0.49f, 0.42f, 0.86f), 60, 110), new Vector3(0f, 0.5f, 2f), 2f, -20);
        BuildTravellerView(traveller);

        // Desk props (their clicks and reactions: BuildDeskInteraction). The plant
        // and the mug stand at their named slots (decoration hooks).
        Transform slots = BuildDeskSlots(booth);
        PlaceSprite(booth, "DeskIntercom", BoothArt("intercom"), new Vector3(-6.3f, -1.8f, 0f), 2.1f, 1);
        SpriteRenderer tray = PlaceSprite(booth, "ScannerTray", BoothArt("scanner_tray"), new Vector3(-4.6f, -3.1f, 0f), 3.8f, 2);
        PlaceSprite(booth, "DeskPlant", BoothArt("desk_plant"), booth.InverseTransformPoint(slots.Find("plant").position), 1.8f, 3);
        PlaceSprite(booth, "DeskMug", BoothArt("desk_mug"), booth.InverseTransformPoint(slots.Find("mug").position), 0.85f, 5);
        PlaceSprite(booth, "DeskStamp", BoothArt("desk_stamp"), new Vector3(-2.2f, -4.3f, 0f), 0.8f, 5);"""),
("""        // Diegetic readouts + a timeline-reactive poster. The desktop's old
        // "< Office" button is replaced by the taskbar's "< Desk" (BuildDesktopShell).
        BuildReadouts(booth, calendarPartition.transform);""",
"""        // The desk: its surface, the paper template, the scanner and its note.
        BuildDesk(booth, tray, config);

        // Diegetic readouts + a timeline-reactive poster. The desktop's old
        // "< Office" button is replaced by the taskbar's "< Desk" (BuildDesktopShell).
        BuildReadouts(booth, calendarPartition.transform);"""),
# --- the raycast buffer: up to 8 stacked papers plus the scanner, a prop and the zones under one point ---
("""    /// <summary>Hit buffer size for the booth's Physics2DRaycaster (non-allocating raycasts).</summary>
    private const int BoothRaycastHits = 8;""",
"""    /// <summary>
    /// Hit buffer size for the booth's Physics2DRaycaster (non-allocating
    /// raycasts): one point can cross up to 8 stacked papers plus the scanner,
    /// a prop, the traveller zone and the exit zone, and hits are cut before
    /// they are sorted.
    /// </summary>
    private const int BoothRaycastHits = 16;"""),
])
```

Expected: `ok …OfficeSceneUIBuilder.cs lf`.

- [ ] **Step 2: The desk's builders** — save as `SCRATCH/p7_t17_desk.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'
D = W + r'\Assets\Editor\OfficeSceneUIBuilder.Desk.cs'

apply(D, [
("""using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
""",
"""using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
"""),
("""/// The office builder's booth and desk parts (the physical desk, piece 7):
/// the live monitor on the CRT, the traveller wheel and the overlay callouts,
/// the desk tuning asset and the shared hit-zone helpers.""",
"""/// The office builder's booth and desk parts (the physical desk, piece 7):
/// the live monitor on the CRT, the traveller wheel and the overlay callouts,
/// the desk (its surface, the paper template, the scanner and the day-1
/// notes), the reacting props, the decoration slots, the traveller's view and
/// hit zone, the booth coordinator, the desk tuning assets and the shared
/// hit-zone helpers."""),
("""    /// <summary>The focus exit zone around the screen (crt sprite units): larger than any focused view, so a click anywhere outside the screen leaves focus.</summary>
    private static readonly Vector2 FocusExitSize = new Vector2(8f, 6f);
""",
"""    /// <summary>The focus exit zone around the screen (crt sprite units): larger than any focused view, so a click anywhere outside the screen leaves focus.</summary>
    private static readonly Vector2 FocusExitSize = new Vector2(8f, 6f);

    /// <summary>Where the desk reactions live (created by the builder when missing; a designer's edits are kept).</summary>
    private const string DeskReactionFolder = "Assets/Data/Config/DeskReactions";

    /// <summary>The highest sorting order of a desk prop (the till's number): the focus exit zone must sit above it.</summary>
    private const int HighestPropOrder = 6;

    /// <summary>The desk rectangle paper centres stay in (world centre): left of the CRT, reaching the scanner.</summary>
    private static readonly Vector3 DeskSurfaceCentre = new Vector3(-2.25f, -4.15f, 0f);

    /// <summary>The desk rectangle's size (world units): x -7.4..2.9 keeps paper centres left of the CRT, y -5.9..-2.4.</summary>
    private static readonly Vector2 DeskSurfaceSize = new Vector2(10.3f, 3.5f);

    /// <summary>Where papers slide in from and back to (world units): just past the desk rectangle's far edge, below the traveller.</summary>
    private static readonly Vector3 PaperHandOverPoint = new Vector3(0f, -2.3f, 0f);

    /// <summary>The placeholder paper's colour.</summary>
    private static readonly Color PaperCream = new Color(0.95f, 0.92f, 0.82f, 1f);

    /// <summary>The paper's reserved photo box (hidden until piece 4 fills it).</summary>
    private static readonly Color PhotoGrey = new Color(0.55f, 0.56f, 0.58f, 1f);

    /// <summary>The scanner's glass bed centre, in the tray sprite's own units.</summary>
    private static readonly Vector2 ScannerBedCentre = new Vector2(0.1f, 0.3f);

    /// <summary>The day-1 scan note just below the tray (tray sprite units: centre and box).</summary>
    private static readonly Vector2 ScanHintCentre = new Vector2(0f, -1.35f);

    /// <summary>The scan note's box (tray sprite units).</summary>
    private static readonly Vector2 ScanHintSize = new Vector2(6.6f, 0.6f);

    /// <summary>The scan note's order: above the tray, under the papers.</summary>
    private const int ScanHintOrder = 4;

    /// <summary>The day-1 wheel note above the traveller's head (world centre).</summary>
    private static readonly Vector2 WheelHintCentre = new Vector2(0f, 2.75f);

    /// <summary>The wheel note's box (world units).</summary>
    private static readonly Vector2 WheelHintSize = new Vector2(4.4f, 0.5f);

    /// <summary>The wheel note's order: over the back wall and the desk art, under every prop.</summary>
    private const int WheelHintOrder = -9;

    /// <summary>The desk notes' ink.</summary>
    private static readonly Color NoteInk = new Color(0.12f, 0.14f, 0.18f, 1f);

    /// <summary>Where the wheel and the bubble centre on the placeholder traveller (its chest), world units.</summary>
    private static readonly Vector3 TravellerAnchorWorld = new Vector3(0f, 1f, 2f);

    /// <summary>The desk art's far edge in world units (desk_deep.png's first opaque row): the traveller is hidden below it.</summary>
    private const float DeskArtFarEdgeY = -0.84f;

    /// <summary>The traveller hit zone's order: just above the traveller (-20).</summary>
    private const int TravellerZoneOrder = -19;

    /// <summary>The calendar hit zone over the sheet painted on the left partition (partition sprite units: centre and size).</summary>
    private static readonly Vector2 CalendarZoneCentre = new Vector2(0.35f, 3.1f);

    /// <summary>The calendar hit zone's size (partition sprite units).</summary>
    private static readonly Vector2 CalendarZoneSize = new Vector2(2.4f, 3.6f);

    /// <summary>The calendar hit zone's order: just above the partition (-50).</summary>
    private const int CalendarZoneOrder = -49;

    /// <summary>The desk's named spots (decoration hooks, item 7): the plant and the mug stand at theirs; the rest are empty for now.</summary>
    private static readonly (string id, DeskSlotKind kind, Vector3 position)[] DeskSlots =
    {
        ("plant", DeskSlotKind.Decoration, new Vector3(-7.5f, -1.15f, 0f)),
        ("mug", DeskSlotKind.Decoration, new Vector3(-6.5f, -4.8f, 0f)),
        ("photo", DeskSlotKind.Decoration, new Vector3(2.4f, -1.6f, 0f)),
        ("free_1", DeskSlotKind.Free, new Vector3(-6.8f, -5.6f, 0f)),
        ("free_2", DeskSlotKind.Free, new Vector3(2.5f, -5.6f, 0f)),
    };
"""),
("""    /// <summary>Returns Desk_Default, creating it with the spec's defaults when missing (a designer's edits are kept).</summary>""",
"""    /// <summary>Returns a desk reaction asset, creating it with a kind and a tooltip when missing (a designer's edits are kept).</summary>
    private static DeskReactionSO EnsureDeskReaction(string name, ReactionKind kind, string tooltip)
    {
        string path = $"{DeskReactionFolder}/{name}.asset";
        DeskReactionSO reaction = AssetDatabase.LoadAssetAtPath<DeskReactionSO>(path);
        if (reaction != null)
            return reaction;

        EnsureFolderTree(DeskReactionFolder);
        reaction = ScriptableObject.CreateInstance<DeskReactionSO>();
        reaction.kind = kind;
        reaction.tooltip = tooltip;
        AssetDatabase.CreateAsset(reaction, path);
        return reaction;
    }

    /// <summary>Returns Desk_Default, creating it with the spec's defaults when missing (a designer's edits are kept).</summary>"""),
("""            calls.ClearArray();
        return calls;
    }

    /// <summary>
    /// An overlay callout (a timed label that takes no clicks) under the office""",
"""            calls.ClearArray();
        return calls;
    }

    /// <summary>The named desk spots under OfficeRoot/DeskSlots, each with its DeskSlot id and kind. Idempotent.</summary>
    private static Transform BuildDeskSlots(Transform booth)
    {
        Transform root = EnsureChild(booth, "DeskSlots");
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;

        foreach ((string id, DeskSlotKind kind, Vector3 position) in DeskSlots)
        {
            Transform slot = EnsureChild(root, id);
            slot.localPosition = position;
            DeskSlot component = slot.GetComponent<DeskSlot>();
            if (component == null)
                component = slot.gameObject.AddComponent<DeskSlot>();
            var so = new SerializedObject(component);
            so.FindProperty("slotId").stringValue = id;
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.ApplyModifiedProperties();
        }

        return root;
    }

    /// <summary>The traveller's view on the placeholder: its figure (hidden until presented) and the anchor the wheel and the bubble centre on. Idempotent.</summary>
    private static void BuildTravellerView(SpriteRenderer traveller)
    {
        Transform anchor = EnsureChild(traveller.transform, "Anchor");
        anchor.position = TravellerAnchorWorld;
        anchor.localRotation = Quaternion.identity;
        anchor.localScale = Vector3.one;

        TravellerView view = traveller.GetComponent<TravellerView>();
        if (view == null)
            view = traveller.gameObject.AddComponent<TravellerView>();
        var so = new SerializedObject(view);
        SerializedProperty figure = so.FindProperty("figure");
        figure.arraySize = 1;
        figure.GetArrayElementAtIndex(0).objectReferenceValue = traveller;
        SetRef(so, "anchor", anchor);
        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// The desk: the scanner on the tray (its sprite's size is the drop area)
    /// with the day-1 scan note below it; OfficeRoot/DeskSurface (the rectangle
    /// paper centres stay in) with its Papers root, the HandOver point on the
    /// traveller's side and the inactive PaperTemplate; and the DeskController
    /// wired to them. Idempotent.
    /// </summary>
    private static void BuildDesk(Transform booth, SpriteRenderer tray, DeskConfigSO config)
    {
        DeskScanner scanner = tray.GetComponent<DeskScanner>();
        if (scanner == null)
            scanner = tray.gameObject.AddComponent<DeskScanner>();
        var soScanner = new SerializedObject(scanner);
        soScanner.FindProperty("dropSize").vector2Value = tray.sprite != null ? (Vector2)tray.sprite.bounds.size : Vector2.zero;
        soScanner.FindProperty("bedCentre").vector2Value = ScannerBedCentre;
        soScanner.ApplyModifiedProperties();

        TextMeshPro scanHint = WorldText(tray.transform, "ScanHint", "", NoteInk, ScanHintCentre, ScanHintSize, 0f, 5f, ScanHintOrder);
        scanHint.fontStyle = FontStyles.Bold;
        scanHint.gameObject.SetActive(false);

        Transform surfaceTransform = EnsureChild(booth, "DeskSurface");
        surfaceTransform.position = DeskSurfaceCentre;
        surfaceTransform.localRotation = Quaternion.identity;
        surfaceTransform.localScale = Vector3.one;
        DeskSurface surface = surfaceTransform.GetComponent<DeskSurface>();
        if (surface == null)
            surface = surfaceTransform.gameObject.AddComponent<DeskSurface>();
        var soSurface = new SerializedObject(surface);
        soSurface.FindProperty("size").vector2Value = DeskSurfaceSize;
        soSurface.ApplyModifiedProperties();

        Transform papers = EnsureChild(surfaceTransform, "Papers");
        papers.localPosition = Vector3.zero;
        Transform handOver = EnsureChild(surfaceTransform, "HandOver");
        handOver.position = PaperHandOverPoint;
        DeskDocument template = BuildPaperTemplate(surfaceTransform, config);

        DeskController desk = surfaceTransform.GetComponent<DeskController>();
        if (desk == null)
            desk = surfaceTransform.gameObject.AddComponent<DeskController>();
        var so = new SerializedObject(desk);
        SetRef(so, "surface", surface);
        SetRef(so, "scanner", scanner);
        SetRef(so, "paperTemplate", template);
        SetRef(so, "paperRoot", papers);
        SetRef(so, "handOverPoint", handOver);
        SetRef(so, "scanHint", scanHint);
        SetRef(so, "config", config);
        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// The inactive paper every handed-over document clones: a SortingGroup root
    /// carrying the paper sprite (1.5 units wide), a click box fitted to it, a
    /// Clickable, a DeskDraggable (the box is its proxy) and the DeskDocument;
    /// the title and holder texts inside the group; the hidden photo slot. Idempotent.
    /// </summary>
    private static DeskDocument BuildPaperTemplate(Transform surface, DeskConfigSO config)
    {
        Sprite paperSprite = EnsureOfficeSprite("paper", PaperCream, 150, 200);
        SpriteRenderer paper = PlaceSprite(surface, "PaperTemplate", paperSprite, Vector3.zero, 1.5f, 0);

        SortingGroup group = paper.GetComponent<SortingGroup>();
        if (group == null)
            group = paper.gameObject.AddComponent<SortingGroup>();
        group.sortingLayerID = SortingLayer.NameToID("Default");
        group.sortingOrder = config.paperBaseOrder;

        Clickable click = EnsureClickable(paper);
        DeskDraggable drag = paper.GetComponent<DeskDraggable>();
        if (drag == null)
            drag = paper.gameObject.AddComponent<DeskDraggable>();
        var soDrag = new SerializedObject(drag);
        SetRef(soDrag, "proxy", paper.GetComponent<BoxCollider2D>());
        soDrag.ApplyModifiedProperties();

        TextMeshPro title = WorldText(paper.transform, "Title", "Document", Ink, new Vector2(0f, 0.72f), new Vector2(1.3f, 0.28f), 0f, 3f, 2);
        TextMeshPro holder = WorldText(paper.transform, "Holder", "Name", Ink, new Vector2(0f, 0.42f), new Vector2(1.3f, 0.24f), 0f, 2.4f, 3);
        SpriteRenderer photo = PlaceSprite(paper.transform, "PhotoSlot", paperSprite, new Vector3(0f, -0.25f, 0f), 0.6f, 1);
        photo.color = PhotoGrey;
        photo.gameObject.SetActive(false);

        DeskDocument doc = paper.GetComponent<DeskDocument>();
        if (doc == null)
            doc = paper.gameObject.AddComponent<DeskDocument>();
        var so = new SerializedObject(doc);
        SetRef(so, "title", title);
        SetRef(so, "holder", holder);
        SetRef(so, "photoSlot", photo.gameObject);
        SetRef(so, "click", click);
        SetRef(so, "drag", drag);
        SetRef(so, "group", group);
        so.ApplyModifiedProperties();

        paper.gameObject.SetActive(false);
        return doc;
    }

    /// <summary>
    /// The booth's clicks, after every prop exists (the wall clock is built by
    /// BuildShiftClockReadouts): each prop's click box and reaction (the reaction
    /// assets are created once), the calendar and traveller hit zones, the
    /// wheel's openers (the traveller and the desk intercom) and its traveller,
    /// the decor props' ids, the day-1 wheel note, and the BoothCoordinator wired
    /// to all of it; then the desk's checks (sorting bands, paper spawn slots).
    /// Idempotent.
    /// </summary>
    private static BoothCoordinator BuildDeskInteraction(OfficeViewController view, MonitorScreen screen, DeskConfigSO config, TravellerWheel wheel,
                                                         OverlayCallout tooltip, TMP_Text trayClockText, ContentLibrarySO library)
    {
        Transform booth = view.transform;
        Transform trayTransform = booth.Find("ScannerTray");
        Transform tillTransform = booth.Find("CreditsTill");
        Transform stabilityTransform = booth.Find("StabilityMonitor");
        Transform partition = booth.Find("LeftPartition");

        Clickable stamp = BuildProp(booth.Find("DeskStamp"), EnsureDeskReaction("Reaction_Stamp", ReactionKind.Squash, ""), tooltip, null, null);
        Clickable mug = BuildProp(booth.Find("DeskMug"), EnsureDeskReaction("Reaction_Mug", ReactionKind.Wobble, ""), tooltip, null, null);
        Clickable plant = BuildProp(booth.Find("DeskPlant"), EnsureDeskReaction("Reaction_Plant", ReactionKind.Wobble, ""), tooltip, null, null);
        Clickable poster = BuildProp(booth.Find("ReactivePoster"), EnsureDeskReaction("Reaction_Poster", ReactionKind.Wobble, ""), tooltip, null, null);
        Clickable intercom = BuildProp(booth.Find("DeskIntercom"), EnsureDeskReaction("Reaction_Intercom", ReactionKind.Squash, ""), tooltip, null, null);
        Clickable tray = BuildProp(trayTransform, EnsureDeskReaction("Reaction_Scanner", ReactionKind.Pulse, ""), tooltip, null, null);
        Clickable till = BuildProp(tillTransform, EnsureDeskReaction("Reaction_Till", ReactionKind.Nudge, "Credits: {value}"), tooltip,
                                   ReadoutText(tillTransform, "CreditsNumber"), tillTransform.GetComponent<AudioSource>());
        Clickable stability = BuildProp(stabilityTransform, EnsureDeskReaction("Reaction_Stability", ReactionKind.None, "Timeline stability: {value}"), tooltip,
                                        ReadoutText(stabilityTransform, "StabilityPercent"), null);
        Clickable clock = BuildProp(booth.Find("WallClock"), EnsureDeskReaction("Reaction_Clock", ReactionKind.None, "{value}"), tooltip, trayClockText, null);

        // The calendar is painted on the left partition: a hit zone over its sheet.
        Clickable calendar = EnsureHitZone(partition, "CalendarZone", CalendarZoneCentre, CalendarZoneSize, CalendarZoneOrder);
        WireReaction(calendar, EnsureDeskReaction("Reaction_Calendar", ReactionKind.None, "Day {value}"), tooltip, ReadoutText(partition, "DayNumber"), null);
        AssetDatabase.SaveAssets();

        // A finished scan pulses the scanner.
        var soScanner = new SerializedObject(trayTransform.GetComponent<DeskScanner>());
        SetRef(soScanner, "reaction", trayTransform.GetComponent<DeskReaction>());
        soScanner.ApplyModifiedProperties();

        // Decoration hooks: the decor props' ids.
        SetDeskItem(booth.Find("DeskStamp"), "stamp");
        SetDeskItem(booth.Find("DeskMug"), "mug");
        SetDeskItem(booth.Find("DeskPlant"), "plant");
        SetDeskItem(booth.Find("ReactivePoster"), "poster");

        // The traveller: a hit zone over the part above the desk art's far edge,
        // a child with its own hidden renderer (piece 4's layered root has none).
        // It and the desk intercom open the wheel, which centres on its anchor.
        Transform traveller = booth.Find("Traveller");
        Bounds art = traveller.GetComponent<SpriteRenderer>().sprite.bounds;
        float bottom = Mathf.Max(traveller.InverseTransformPoint(new Vector3(0f, DeskArtFarEdgeY, 0f)).y, art.min.y);
        Clickable travellerZone = EnsureHitZone(traveller, "TravellerHitZone", new Vector3(art.center.x, (bottom + art.max.y) / 2f, 0f),
                                                new Vector2(art.size.x, art.max.y - bottom), TravellerZoneOrder);
        WirePersistentVoid(travellerZone, "onClick", wheel, nameof(TravellerWheel.Open));
        WirePersistentVoid(intercom, "onClick", wheel, nameof(TravellerWheel.Open));
        var soWheel = new SerializedObject(wheel);
        SetRef(soWheel, "traveller", traveller.GetComponent<TravellerView>());
        soWheel.ApplyModifiedProperties();

        // The day-1 wheel note above the traveller's head (the coordinator sets its text and shows it).
        TextMeshPro wheelHint = WorldText(booth, "WheelHint", "", NoteInk, WheelHintCentre, WheelHintSize, 0f, 5f, WheelHintOrder);
        wheelHint.fontStyle = FontStyles.Bold;
        wheelHint.gameObject.SetActive(false);

        // The booth coordinator applies the input rules to all of it.
        Transform crt = booth.Find("CRTMonitor");
        BoothCoordinator coordinator = booth.GetComponent<BoothCoordinator>();
        if (coordinator == null)
            coordinator = booth.gameObject.AddComponent<BoothCoordinator>();
        var so = new SerializedObject(coordinator);
        SetRef(so, "view", view);
        SetRef(so, "screen", screen);
        SetRef(so, "desk", booth.Find("DeskSurface").GetComponent<DeskController>());
        SetRef(so, "wheel", wheel);
        SetRef(so, "crt", crt.GetComponent<Clickable>());
        SetRef(so, "powerButton", crt.Find("PowerButton").GetComponent<Clickable>());
        SetRef(so, "focusExit", crt.Find("FocusExitZone").GetComponent<Clickable>());
        SetRef(so, "glassZone", crt.Find("ScreenAnchor/GlassZone").GetComponent<Clickable>());
        SetRef(so, "travellerHitZone", travellerZone);
        Clickable[] props = { stamp, mug, plant, poster, intercom, tray, till, stability, clock, calendar };
        SerializedProperty propList = so.FindProperty("props");
        propList.arraySize = props.Length;
        for (int i = 0; i < props.Length; i++)
            propList.GetArrayElementAtIndex(i).objectReferenceValue = props[i];
        SetRef(so, "wheelHint", wheelHint);
        SetRef(so, "config", config);
        so.ApplyModifiedProperties();

        // Checks: input and drawing order agree; every paper a traveller carries has a spawn slot.
        int maxPapers = library != null ? ContentLibraryValidator.MaxDocuments(ContentLibraryValidator.TravellerBlueprints(library)) : 0;
        foreach (string problem in SortingBands.Problems(HighestPropOrder, config.focusExitOrder, config.glassOrder, config.bezelOrder,
                                                         config.screenCanvasOrder, config.paperBaseOrder, maxPapers, config.heldPaperOrder))
            Debug.LogError($"[TimeDesk] The desk's sorting bands overlap: {problem}; fix the orders in Desk_Default.");
        int slots = config.paperSpawnSlots != null ? config.paperSpawnSlots.Length : 0;
        if (slots < maxPapers)
            Debug.LogError($"[TimeDesk] The desk has {slots} paper spawn slots but a traveller can carry {maxPapers} papers; add slots in Desk_Default.");

        return coordinator;
    }

    /// <summary>Makes a booth sprite a reacting prop: a click box fitted to its art and a DeskReaction.</summary>
    private static Clickable BuildProp(Transform prop, DeskReactionSO reaction, OverlayCallout tooltip, TMP_Text readout, AudioSource audioSource)
    {
        Clickable click = EnsureClickable(prop.GetComponent<SpriteRenderer>());
        WireReaction(click, reaction, tooltip, readout, audioSource);
        return click;
    }

    /// <summary>Adds or rewires a clickable's DeskReaction (its readout and audio source are optional).</summary>
    private static void WireReaction(Clickable click, DeskReactionSO reaction, OverlayCallout tooltip, TMP_Text readout, AudioSource audioSource)
    {
        DeskReaction component = click.GetComponent<DeskReaction>();
        if (component == null)
            component = click.gameObject.AddComponent<DeskReaction>();
        var so = new SerializedObject(component);
        SetRef(so, "reaction", reaction);
        SetRef(so, "readout", readout);
        SetRef(so, "tooltip", tooltip);
        SetRef(so, "audioSource", audioSource);
        so.ApplyModifiedProperties();
    }

    /// <summary>A readout's text under a prop, or null.</summary>
    private static TMP_Text ReadoutText(Transform prop, string name)
    {
        Transform t = prop.Find(name);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    /// <summary>Gives a decor prop its DeskItem id.</summary>
    private static void SetDeskItem(Transform prop, string id)
    {
        DeskItem item = prop.GetComponent<DeskItem>();
        if (item == null)
            item = prop.gameObject.AddComponent<DeskItem>();
        var so = new SerializedObject(item);
        so.FindProperty("itemId").stringValue = id;
        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// An overlay callout (a timed label that takes no clicks) under the office"""),
])
```

Expected: `ok …OfficeSceneUIBuilder.Desk.cs lf`.

- [ ] **Step 3: Compile check and run**

Expected: exit 0 / exit 0; `passed 433, failed 0`.

- [ ] **Step 4: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Editor/OfficeSceneUIBuilder.cs Assets/Editor/OfficeSceneUIBuilder.Desk.cs && git commit -F - <<'EOF'
feat(builder): the desk, its props, the traveller and the booth's wiring

Build Office UI adds the traveller's view and hit zone, the desk slots,
the desk with its scanner and paper template, the props' reactions and
tooltips, the scan and wheel notes, and wires BoothCoordinator, the desk
and the wheel. It reports overlapping sorting bands and too few spawn
slots; the booth's raycaster holds 16 hits. The committed scene is
rebuilt, with its FEATURES lines, in a later commit.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 18: Drafts, the audit plan and the art contracts amended

Spec §3.2, §3.3, R30: pieces 4 and 6 are drafts built on premises this piece removes, so each gets an "Amendments from piece 7, read first" block (their own lines are not edited); the audit plan names piece 8 as the wheel's content; Codex's two art contracts get appended amendments (their text kept); the art asset list records the CRT request, the new placeholders and the grab cursors.

**Files:**
- Modify: `docs/superpowers/drafts/specs/2026-09-24-characters-design.md`, `docs/superpowers/drafts/specs/2026-09-24-ui-reacts-design.md`, `docs/superpowers/plans/2026-09-25-code-audit-overhaul-plan.md`, `ArtDeliverables/TimeDesk/OFFICE_RESET_BRIEF.md`, `ArtDeliverables/TimeDesk/HybridScene/SCENE_CONTRACT.md`, `docs/ART_ASSET_LIST.md`

- [ ] **Step 1: Apply** — save as `SCRATCH/p7_t18.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

# --- The piece-4 draft: an amendment block, read first (spec section 3.2) ---
apply(W + r'\docs\superpowers\drafts\specs\2026-09-24-characters-design.md', [
("""Line numbers refer to `efe385d` on `feat/identity-lies` (piece 2, Tasks 1–12 committed).""",
"""## Amendments from piece 7 (2026-09-25), read first

Piece 7 (the physical desk and the basic traveller wheel, `docs/superpowers/specs/2026-09-25-physical-desk-design.md`) lands before this piece and removes premises this draft relies on. Piece 4 re-decides the rows below before its plan; this draft's own lines are otherwise not edited.

- **The booth figure is clickable; garments are inspected through the traveller wheel** (C4 :27, §1.2 :100-106, R12 :67, R14 :69, C8 :31 (its Visitor-window use), C18 :41 ("In the Visitor window a premade is one clickable region"), §2.9 `TravellerPortraitView` as the Visitor window :427-437, §2.11 the Visitor fields and `ShowRich` opening :469-481, §2.12 the Visitor window block :500-509, §3.3 the FEATURES :35/:827 lines :819 and :827, §6 :926-927, §7 UI crowding :946):
  - C4's premise ("the desktop canvas and the booth are never visible together") is false: the desktop is drawn live on the CRT in the booth view, and clicking the booth figure opens the traveller wheel (piece-7 K20).
  - Garments are inspected through the wheel ("Look >", piece-8 content once this piece exists): each visible garment is a choice, and its observation becomes a compare-clickable transcript row carrying `EvidenceKind.Appearance` (the item's label as text, the Culture value as evidence).
  - The Visitor window, its auto-open (R12) and `VisitorReachable` (R14) are dropped; Appearance tells are gated by `InterviewReachable`.
  - `TravellerPortraitView` stays only for the passport photo.
- **`TravellerView` exists** (C19 :42, §2.9 `TravellerView` :421-425, §2.10 :441 and :457-460): piece 7 created `Assets/Scripts/Characters/TravellerView.cs` with `Show()`/`Clear()`, `figure` and `Anchor`; this piece extends it (`Show(look, art)`, the layer renderers) instead of creating it. `GameManager.travellerView` exists; its `Show`/`Clear` calls sit in `GameManager.SetTravellerAtDesk`.
- **The builder keeps the anchor and the hit zone** (R11 :66, §2.12 `BuildTraveller` :493-499): when it replaces the placeholder with layer children it must keep `Traveller/Anchor` and `Traveller/TravellerHitZone`, and refit the hit zone to the figure (a child with its own hidden renderer: a collider on the `SortingGroup` root would report order 0).
- **Speech bubbles** (W12 :50, §4 :845): delivered, minimal, by piece 7 (one `OverlayCallout`, shown by `TravellerWheel.Say`); polish goes to piece 8; walk-in and walk-out stay with the booth rework.
- **"Clickable booth figure"** (§4 :849, not scheduled): delivered by piece 7.
- **The passport photo** (C20 and R13, :43 and :68) also fills the physical paper's reserved `DeskDocument.photoSlot`.
- **Both pieces edit `DocumentTemplateSO`** (`handOver` in piece 7, `showsPhoto` here), and **`ContentLibraryValidator.TravellerBlueprints` is now public** (piece 7), which R15 (`blueprintOverride` removed) edits: the plan re-reads both.
- **Line numbers and paths:** the builder anchors this draft cites are stale again (piece 7 adds `Assets/Editor/OfficeSceneUIBuilder.Desk.cs`), as are the `DocumentWindowController` photo layout (the window stays) and the menu-capacity wording ("intercom" is now "the traveller wheel").

Line numbers refer to `efe385d` on `feat/identity-lies` (piece 2, Tasks 1–12 committed)."""),
])

# --- The piece-6 draft: the same kind of block ---
apply(W + r'\docs\superpowers\drafts\specs\2026-09-24-ui-reacts-design.md', [
("""Line numbers refer to `efe385d` on `feat/identity-lies` (piece 2 implemented).""",
"""## Amendments from piece 7 (2026-09-25), read first

Piece 7 (the physical desk and the basic traveller wheel, `docs/superpowers/specs/2026-09-25-physical-desk-design.md`) lands before this piece and changes the surfaces this draft themes. Piece 6 re-decides the rows below before its plan; this draft's own lines are otherwise not edited.

- **Surfaces** (U11 :27, the surfaces table :94-97): the intercom panel row goes. The traveller wheel (its ring buttons and centre slot), the speech bubble and the desk tooltip (one `OverlayCallout` component, two instances) are new overlay surfaces; themed or neutral is piece 6's decision (piece 7 builds them neutral).
- **Theme roles 13, 17 and 18** (:297, :301-302): the intercom's `ActionButtonTemplate`, `IntercomPanel` and its title are gone. The wheel's ring buttons replace role 13; roles 17 and 18 have no target.
- **Keys** (:492-494, :506, :508, :511, :528, :543, :743, :748, :756, :759); every player-visible string piece 7 adds or changes:
  - `intercom.title` and `intercom.actionSample` go (with the flavour row at :748);
  - `window.scanner` and `icon.scanner` read "Deviation Report" (the flavour rows :756 and :759 are translated again);
  - `window.material` reads "Material Analysis";
  - `compare.intercomLabel` becomes a traveller label, "Traveller · {0}";
  - `desktop.back` ("< Office", :506) goes with the desktop's Back button; the taskbar's "< Desk" is a new key;
  - `startmenu.power` ("Power", :511, flavour row :743) reads "Quit game"; "Turn off screen" is a new key;
  - `results.unproven` (:528) ends "(log a deviation before denying)";
  - new keys: the idle line "Waiting for the next traveller", the scan note "Drag papers onto the scanner to read them on the PC.", the wheel note "Click the traveller to talk and ask for papers.", and the four tooltip templates "Credits: {0}", "Day {0}", "Timeline stability: {0}", "{0}".
- **Copy held in ScriptableObjects** (`DeskConfigSO.scanHint` and `wheelHint`, the `DeskReactionSO.tooltip` templates) is UI copy, not content and not diegetic evidence (Z4 covers documents, books and records), so it becomes keys under this draft's R2: piece 6 decides whether the fields then hold a key or move into the table. Its key walk must include these fields, which a walk of the builder and the code would not find.
- **The apply walk** (:423) **and the completeness check** (:599): the desktop canvas is a World Space canvas, always active, under `OfficeRoot/CRTMonitor/ScreenAnchor`; the overlay canvas gains `TravellerWheel` and two `OverlayCallout`s (`SpeechBubble`, `DeskTooltip`); the world gains the scan and wheel notes (world TMP texts).
- **Verification** (:1020): "READY, focus the monitor": READY no longer focuses; click the CRT.

Line numbers refer to `efe385d` on `feat/identity-lies` (piece 2 implemented)."""),
])

# --- The audit plan: piece 8 is the wheel's content (spec section 3.3) ---
apply(W + r'\docs\superpowers\plans\2026-09-25-code-audit-overhaul-plan.md', [
("""**Runs after:** pieces 7 (physical desk), 8 (traveller wheel), 4 (characters), 5 (history),""",
"""**Runs after:** pieces 7 (physical desk), 8 (traveller wheel content: piece 7 ships the basic wheel), 4 (characters), 5 (history),"""),
])

# --- Codex's art contracts: appended amendments, their text kept (R30) ---
apply(W + r'\ArtDeliverables\TimeDesk\OFFICE_RESET_BRIEF.md', [
("""See ../TimeDesk/HybridScene/SCENE_CONTRACT.md for the current layer/model/animation separation and source-code integration notes. Image mockups are concept references, not 3D assets. Megabuildings must be locked independently; cars must never be baked into buildings or hall. No removed installers are to be recreated.
""",
"""See ../TimeDesk/HybridScene/SCENE_CONTRACT.md for the current layer/model/animation separation and source-code integration notes. Image mockups are concept references, not 3D assets. Megabuildings must be locked independently; cars must never be baked into buildings or hall. No removed installers are to be recreated.

## Amendment (piece 7, 2026-09-25)
Source: docs/superpowers/specs/2026-09-25-physical-desk-design.md (the physical desk and the traveller wheel).
- Supersedes line 10 ("do not invent physical scanning mechanics") and the deferred extras' "Physical intercom and document slot ... The PC currently hosts intercom requests and scanned-document investigation": the desk has physical papers the player drags, a working scanner tray (a paper dropped on its glass bed opens its scanned copy on the PC) and an intercom speaker that opens the traveller wheel.
- Supersedes the READY/NEXT line's "focus binding": READY only calls the next traveller; clicking the CRT focuses the PC.
- The CRT needs a power button and an LED on its bezel, and a blank glass whose inscribed 4:3 rectangle the game fills with the live desktop.
"""),
])

apply(W + r'\ArtDeliverables\TimeDesk\HybridScene\SCENE_CONTRACT.md', [
("""7. User tests in Unity. Codex does not run tests, Play mode or UI automation.
""",
"""7. User tests in Unity. Codex does not run tests, Play mode or UI automation.

## Amendment (piece 7, 2026-09-25): new code anchors
Source: docs/superpowers/specs/2026-09-25-physical-desk-design.md. Added to "Existing code anchors (read, not changed)"; each is a scene component whose geometry is data, so the 3D props supply their own numbers at adoption:
- `MonitorScreen` on the PC's glass: the screen plane (a `ScreenAnchor` at the glass's centre) and the glass rectangle's size, screen power and the bezel LED; the desktop is a World Space canvas on that plane.
- `DeskSurface`: the desk plane and the rectangle papers stay in.
- `DeskScanner`: the scanner's drop area and glass bed.
- `DeskSlot` anchors (plant, mug, photo, free spots) and `DeskItem` ids on decor props.
- `TravellerView.Anchor` (where the traveller wheel and the speech bubble centre) and the `TravellerHitZone` child.
- `BoothCoordinator`, which applies the booth's input rules.
- The focus framing is computed from the glass (orthographic now; a perspective adapter at adoption).
The 4:3 CRT requirement above ("Match the CRT front to a 4:3 screen") is confirmed: the desktop is 1440 x 1080 units. `Clickable` still needs a `Collider2D`, so 3D papers and props need proxies at adoption.
"""),
])

apply(W + r'\docs\ART_ASSET_LIST.md', [
("""5. **crt.png**: placeholder 150x130, deliver **600x520**, transparent. Beige 90s CRT on a swivel base, tube depth on the RIGHT, glass turned LEFT toward the player, dim screen glow. Clickable, so clear silhouette.""",
"""5. **crt.png**: placeholder 150x130, deliver **600x520**, transparent. Beige 90s CRT on a swivel base, tube depth on the RIGHT, glass turned LEFT toward the player, dim screen glow. Clickable, so clear silhouette. **Art request (piece 7):** a front-facing CRT whose glass is a large, blank 4:3 rectangle (the game draws the live desktop in the glass's inscribed 4:3 rectangle), with a power button and an LED on its bezel."""),
("""- **intercom.png** 320x240 transparent: desk intercom box (clicked to request documents).
- **scanner_tray.png** 600x200 transparent: document slot on the desk.""",
"""- **intercom.png** 320x240 transparent: desk intercom box (clicked to open the traveller wheel).
- **scanner_tray.png** 600x200 transparent: desk scanner: drop papers on its glass bed.
- **paper.png** 150x200 (in `Assets/Art/Office/Placeholder/`, piece 7): a blank paper sheet the desk's papers use; later one paper stock per document kind.
- **crt_power.png** 28x28 and **crt_led.png** 8x8 (in `Assets/Art/Office/Placeholder/`, piece 7): the CRT bezel's power button and LED (draw the LED white: the game tints it on and off)."""),
("""- cursor_arrow.png, cursor_hand.png 32x32""",
"""- cursor_arrow.png, cursor_hand.png 32x32
- cursor_grab.png, cursor_grabbing.png 32x32 (over and while dragging a desk paper; piece 7 shows the hand for now)"""),
])
```

Expected: `ok …2026-09-24-characters-design.md lf`, `ok …2026-09-24-ui-reacts-design.md lf`, `ok …2026-09-25-code-audit-overhaul-plan.md crlf`, `ok …OFFICE_RESET_BRIEF.md crlf`, `ok …SCENE_CONTRACT.md crlf`, `ok …ART_ASSET_LIST.md crlf`.

- [ ] **Step 2: Commit**

```bash
cd /e/unity/NOPE-feat-clock && git add docs/superpowers/drafts/specs/2026-09-24-characters-design.md docs/superpowers/drafts/specs/2026-09-24-ui-reacts-design.md docs/superpowers/plans/2026-09-25-code-audit-overhaul-plan.md ArtDeliverables/TimeDesk/OFFICE_RESET_BRIEF.md ArtDeliverables/TimeDesk/HybridScene/SCENE_CONTRACT.md docs/ART_ASSET_LIST.md && git commit -F - <<'EOF'
docs: amend the piece 4 and 6 drafts, the audit plan and the art contracts

The characters and UI-reacts drafts get an amendment block, read first:
the booth figure is clickable, garments go through the traveller wheel,
TravellerView and the speech bubble exist, and the surfaces and keys
piece 7 adds or retires. The audit plan names piece 8 as the wheel's
content. The office brief and the scene contract get appended amendments
(the desk's papers, scanner and intercom; READY without focus; the CRT's
power button and 4:3 glass; the new code anchors); the art asset list
records the CRT request, the paper and bezel placeholders and grab cursors.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

---

### Task 19: Generate World, rebuild OfficeScene and create the desk's assets (Unity)

Spec §6 steps 2 and 4, §3.4, and HOUSE_RULES ("if you change the builder, the scene must be rebuilt in Unity and committed"; `docs/FEATURES.md` changes in the same commit as the behaviour). One `-executeMethod` session runs Generate World twice (nothing may differ from the committed data: `world_source.json` and the generator's output do not change in this piece), Validate Content Library and the hand-over counts (`MaxRequestedDocuments` 1, `MaxDocuments` 2); then opens `OfficeScene`, runs Build Office UI twice and saves after each build, dumping the scene semantically after each (paths, active flags, component types, transforms, rects, TMP settings, renderers, sorting groups, colliders, canvases, persistent calls and every serialized value of the project's components, references as target paths). The two dumps must be equal. It checks the rebuilt scene (spec §6 step 4: the World Space desktop, the removed objects, every persistent call, the overlay hosts and their inactive children, the renames, the 16 raycast hits, the assets) and walks every object reference of the new and changed components (the allow-list: a prop's `readout`/`audioSource` where item 10 gives none, and Task 0's baseline nulls). A third build, never saved, runs with three in-memory edits (the library's menu capacity 9, `Desk_Default` with one spawn slot and the glass zone at the exit zone's order), so the builder's three reports are seen to fire; every edit is undone, the scene is reopened, and the scene, library and config files must be unchanged on disk. The YAML is not compared: the builder destroys and recreates several objects on every run (window chrome, the wheel's ring buttons, the Start menu), so their fileIDs change. Once every check passes, `docs/FEATURES.md` gets the builder-driven behaviour lines (the booth, the desk, the scanner, the wheel, screen power, the exits, the Start menu, the renames) and is committed with the rebuilt scene: that commit is the one that gives players the behaviour, so the scene and its contract ship together (Tasks 15–17 change only the builder).

**When a check fails**, first decide where the defect is:
- **In the temporary `_TimeDeskP7Build.cs`** (the automation itself is wrong): fix that script, commit nothing, re-run Step 4.
- **In the product:** revert what Unity wrote (`git checkout -- Assets/Scenes/OfficeScene.unity Assets/Art/Generated/xp_bliss.png.meta && rm -rf Assets/Data/Config/Desk_Default.asset Assets/Data/Config/Desk_Default.asset.meta Assets/Data/Config/DeskReactions Assets/Data/Config/DeskReactions.meta Assets/Art/Office/Placeholder/paper.png Assets/Art/Office/Placeholder/paper.png.meta Assets/Art/Office/Placeholder/crt_power.png Assets/Art/Office/Placeholder/crt_power.png.meta Assets/Art/Office/Placeholder/crt_led.png Assets/Art/Office/Placeholder/crt_led.png.meta`; the builder keeps an existing `Desk_Default` and reaction assets, so a changed default only reaches them when they are recreated), fix the cause at its source (Domain or Visuals first, with a failing EditMode test, when the rule is theirs), pass the offline gate, commit it as `fix: …`, then re-run Step 4. If the fix changes a value that Step 7's FEATURES lines state (a `DeskConfigSO` default such as the fill 0.85 or the 1.5 s scan), adapt that line in the Step 7 script before it runs, and correct the spec's figures for it in the `fix:` commit.

**Files:**
- Create (temporary, never committed): `Assets/Editor/_TimeDeskP7Build.cs`
- Commit: `Assets/Data/Config/Desk_Default.asset`, `Assets/Data/Config/DeskReactions/` (ten `Reaction_*.asset`) and their metas, `Assets/Art/Office/Placeholder/paper.png`, `crt_power.png`, `crt_led.png` and their metas, `Assets/Art/Generated/xp_bliss.png.meta` (mipmaps on), the rebuilt `Assets/Scenes/OfficeScene.unity`
- Modify: `docs/FEATURES.md` (CRLF; Step 7, committed with the scene in Step 8)
- Output: `SCRATCH/p7_build_report.txt`, `SCRATCH/p7_scene_dump_1.txt`, `SCRATCH/p7_scene_dump_2.txt` (Task 20 compares against the second)

- [ ] **Step 1: Offline gate**

Run the compile check and the test run. Expected: exit 0 / exit 0; `passed 433, failed 0`. `git status --short` shows only the two untracked files. `SCRATCH/p7_baseline_scene.txt` and `SCRATCH/p7_baseline_cases.txt` exist (Task 0).

- [ ] **Step 2: Check no Unity editor is open on the worktree** (PowerShell)

Run: `Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Select-Object ProcessId, CommandLine`
Expected: none on `NOPE-feat-clock`.

- [ ] **Step 3: Write the build automation** (`Assets/Editor/_TimeDeskP7Build.cs`, Write tool)

```csharp
// TEMPORARY automation for piece 7 (the physical desk); never committed.
// Generate World twice (idempotent, nothing differs from the committed data),
// Validate Content Library and the hand-over counts (spec section 6 step 2),
// then Build Office UI twice on OfficeScene, saving after each build, with a
// semantic scene dump after each (they must be equal) and the scene checks of
// step 4; then an unsaved third build with three in-memory edits, whose three
// reports must fire while no file changes. Reports go to the scratchpad.
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
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class TimeDeskP7Build
{
    private const string Scratch = @"C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad";
    private static readonly string ReportPath = Path.Combine(Scratch, "p7_build_report.txt");
    private const string LibraryPath = "Assets/Data/Content Library/ContentLibrary_Main.asset";
    private const string ScenePath = "Assets/Scenes/OfficeScene.unity";
    private const string DeskConfigPath = "Assets/Data/Config/Desk_Default.asset";
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
            // --- Content (step 2) ---
            Dictionary<string, string> committed = HashDataFolder();
            RunMenu("Tools/TimeDesk/Generate World");
            Dictionary<string, string> first = HashDataFolder();
            RunMenu("Tools/TimeDesk/Generate World");
            Dictionary<string, string> second = HashDataFolder();
            Check(Changed(committed, first).Count == 0, $"Generate World changes no committed data file: [{string.Join(", ", Changed(committed, first))}]");
            Check(Changed(first, second).Count == 0, $"a second Generate World changes nothing: [{string.Join(", ", Changed(first, second))}]");

            List<string> validator = RunMenu("Tools/TimeDesk/Validate Content Library");
            Check(validator.Any(l => l.Contains("no issues found")) && !validator.Any(l => l.StartsWith("[Error]") || l.StartsWith("[Warning]")),
                  "Validate Content Library reports no issues");

            var lib = AssetDatabase.LoadAssetAtPath<ContentLibrarySO>(LibraryPath);
            List<CaseBlueprintSO> blueprints = ContentLibraryValidator.TravellerBlueprints(lib).ToList();
            Check(ContentLibraryValidator.MaxRequestedDocuments(blueprints) == 1 && ContentLibraryValidator.MaxDocuments(blueprints) == 2,
                  $"a traveller hands over at most {ContentLibraryValidator.MaxRequestedDocuments(blueprints)} document on request (1) and carries at most {ContentLibraryValidator.MaxDocuments(blueprints)} papers (2)");

            // --- The builder, twice, saving after each build (step 4) ---
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            List<string> build1 = RunMenu(BuildMenu);
            Check(!build1.Any(l => l.StartsWith("[Error]") || l.StartsWith("[Exception]")),
                  "the first Build Office UI logs no error (the wheel fits 8, 4 slots >= 2 papers, the bands are clean)");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            List<string> dump1 = DumpScene();
            File.WriteAllLines(Path.Combine(Scratch, "p7_scene_dump_1.txt"), dump1);
            CheckScene();

            List<string> build2 = RunMenu(BuildMenu);
            Check(!build2.Any(l => l.StartsWith("[Error]") || l.StartsWith("[Exception]")), "the second Build Office UI logs no error");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            List<string> dump2 = DumpScene();
            File.WriteAllLines(Path.Combine(Scratch, "p7_scene_dump_2.txt"), dump2);
            Check(dump1.SequenceEqual(dump2),
                  $"the two builds dump the same scene ({dump1.Count} lines): first-only [{string.Join(" || ", dump1.Except(dump2).Take(20))}] second-only [{string.Join(" || ", dump2.Except(dump1).Take(20))}]");

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

    private static List<string> Changed(Dictionary<string, string> a, Dictionary<string, string> b) =>
        a.Keys.Union(b.Keys).Where(k => !a.TryGetValue(k, out string x) || !b.TryGetValue(k, out string y) || x != y).OrderBy(k => k).ToList();

    // -----------------------------
    // The rebuilt scene (step 4)
    // -----------------------------

    private static Transform Find(string path)
    {
        string[] parts = path.Split('/');
        GameObject root = EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == parts[0]);
        if (root == null)
            return null;
        return parts.Length == 1 ? root.transform : root.transform.Find(string.Join("/", parts.Skip(1)));
    }

    private static int PersistentCalls(Object host, string eventProp, out string methods)
    {
        SerializedProperty calls = new SerializedObject(host).FindProperty(eventProp + ".m_PersistentCalls.m_Calls");
        var names = new List<string>();
        for (int i = 0; calls != null && i < calls.arraySize; i++)
        {
            SerializedProperty call = calls.GetArrayElementAtIndex(i);
            Object target = call.FindPropertyRelative("m_Target").objectReferenceValue;
            names.Add($"{(target != null ? target.GetType().Name : "null")}.{call.FindPropertyRelative("m_MethodName").stringValue}");
        }
        methods = string.Join(", ", names);
        return names.Count;
    }

    private static void CheckScene()
    {
        const string Crt = "OfficeRoot/CRTMonitor";
        const string Screen = Crt + "/ScreenAnchor/Canvas";
        Transform canvasT = Find(Screen);
        Canvas canvas = canvasT != null ? canvasT.GetComponent<Canvas>() : null;
        Check(canvas != null && canvas.renderMode == RenderMode.WorldSpace && canvas.sortingOrder == 20 && canvas.GetComponent<CanvasScaler>() == null &&
              canvas.GetComponent<RectMask2D>() != null && Find("Canvas") == null && ((RectTransform)canvasT).sizeDelta == new Vector2(1440f, 1080f),
              "the desktop Canvas sits under CRTMonitor/ScreenAnchor: World Space, order 20, 1440x1080, no CanvasScaler, a RectMask2D");

        Transform invest = Find(Screen + "/InvestigationUI/InvestigationRoot");
        Transform officeUi = canvasT.GetComponentInChildren<OfficeUIController>(true).transform;
        Check(invest.Find("IntercomPanel") == null && canvasT.Find("BackToOfficeButton") == null &&
              new[] { "VisitorText", "Doc1Text", "Doc2Text", "ResultText", "EraButtonsRoot" }.All(n => officeUi.Find(n) == null),
              "no IntercomPanel, BackToOfficeButton, VisitorText, Doc1Text, Doc2Text, ResultText or EraButtonsRoot");

        Check(PersistentCalls(Find("OfficeRoot/ReadySign").GetComponent<Clickable>(), "onClick", out _) == 0, "ReadySign has no persistent call");
        Check(PersistentCalls(Find(Crt).GetComponent<Clickable>(), "onClick", out string crtCalls) == 1 && crtCalls == "OfficeViewController.FocusMonitor", $"CRTMonitor: [{crtCalls}]");
        Check(PersistentCalls(Find(Crt + "/PowerButton").GetComponent<Clickable>(), "onClick", out string power) == 1 && power == "MonitorScreen.TogglePower", $"PowerButton: [{power}]");
        Transform exit = Find(Crt + "/FocusExitZone");
        int exitCount = PersistentCalls(exit.GetComponent<Clickable>(), "onClick", out string exitCalls);
        Check(!exit.gameObject.activeSelf && exitCount == 1 && exitCalls == "OfficeViewController.FocusOffice",
              $"FocusExitZone is inactive with FocusOffice [{exitCalls}]");
        Transform glass = Find(Crt + "/ScreenAnchor/GlassZone");
        var config = AssetDatabase.LoadAssetAtPath<DeskConfigSO>(DeskConfigPath);
        Check(!glass.gameObject.activeSelf && PersistentCalls(glass.GetComponent<Clickable>(), "onClick", out _) == 0 && !glass.GetComponent<Clickable>().Interactable &&
              glass.GetComponent<SpriteRenderer>().sortingOrder == config.glassOrder && !glass.GetComponent<SpriteRenderer>().enabled,
              "GlassZone is inactive, has no persistent call, a non-interactable Clickable and a hidden renderer at glassOrder");
        Check(PersistentCalls(Find("OfficeRoot/Traveller/TravellerHitZone").GetComponent<Clickable>(), "onClick", out string zone) == 1 && zone == "TravellerWheel.Open" &&
              PersistentCalls(Find("OfficeRoot/DeskIntercom").GetComponent<Clickable>(), "onClick", out string intercom) == 1 && intercom == "TravellerWheel.Open",
              "TravellerHitZone and DeskIntercom call TravellerWheel.Open");

        const string Overlay = "OfficeOverlayCanvas";
        Check(Find(Overlay + "/TravellerWheel").gameObject.activeSelf && Find(Overlay + "/SpeechBubble").gameObject.activeSelf && Find(Overlay + "/DeskTooltip").gameObject.activeSelf &&
              !Find(Overlay + "/TravellerWheel/Catcher").gameObject.activeSelf && !Find(Overlay + "/SpeechBubble/Panel").gameObject.activeSelf &&
              !Find(Overlay + "/DeskTooltip/Panel").gameObject.activeSelf,
              "the overlay hosts TravellerWheel, SpeechBubble and DeskTooltip are active; Catcher and both Panels are inactive");
        var ring = (RectTransform)Find(Overlay + "/TravellerWheel/Catcher/Ring");
        Transform centre = ring.Find("Centre");
        bool centred(RectTransform rt) => rt.anchorMin == new Vector2(0.5f, 0.5f) && rt.anchorMax == new Vector2(0.5f, 0.5f) && rt.pivot == new Vector2(0.5f, 0.5f);
        Check(centre.GetComponent<LayoutElement>().ignoreLayout && centred(ring) && centred((RectTransform)Find(Overlay + "/SpeechBubble/Panel")) &&
              centred((RectTransform)Find(Overlay + "/DeskTooltip/Panel")),
              "Ring/Centre ignores the layout; the Ring and both panels have anchors and pivot (0.5, 0.5)");

        Transform icon = invest.Find("BookShelf/IconScanner/Label");
        Transform report = invest.Find("WindowLayer/IconScannerWindow/Header/TitleText");
        Transform material = invest.Find("WindowLayer/IconMaterialWindow/Header/TitleText");
        Check(icon.GetComponent<TMP_Text>().text == "Deviation Report" && report.GetComponent<TMP_Text>().text == "Deviation Report" &&
              material.GetComponent<TMP_Text>().text == "Material Analysis",
              $"the IconScanner label reads '{icon.GetComponent<TMP_Text>().text}', its window '{report.GetComponent<TMP_Text>().text}', Material '{material.GetComponent<TMP_Text>().text}'");

        Check(Camera.main.GetComponent<Physics2DRaycaster>().maxRayIntersections == 16, "the booth raycaster holds 16 hits");
        string[] reactions = { "Stamp", "Mug", "Plant", "Poster", "Intercom", "Scanner", "Till", "Calendar", "Stability", "Clock" };
        Check(config != null && reactions.All(r => AssetDatabase.LoadAssetAtPath<DeskReactionSO>($"Assets/Data/Config/DeskReactions/Reaction_{r}.asset") != null) &&
              reactions.All(r => AssetDatabase.LoadAssetAtPath<DeskReactionSO>($"Assets/Data/Config/DeskReactions/Reaction_{r}.asset").clip == null),
              "Desk_Default and the ten reaction assets exist (no clips)");

        MissedWiring();
    }

    /// <summary>
    /// Every object reference (arrays included) of every new component in the
    /// scene and of every changed one is set, except the allow-list: a
    /// DeskReaction's readout and audio source where the prop has none, and a
    /// changed component's field that was already null in the baseline scene.
    /// </summary>
    private static void MissedWiring()
    {
        var baseline = new HashSet<string>(File.ReadAllLines(Path.Combine(Scratch, "p7_baseline_scene.txt"))
            .Where(l => l.StartsWith("null ")).Select(l => l.Substring(5)));
        Type[] checkedTypes =
        {
            typeof(BoothCoordinator), typeof(MonitorScreen), typeof(DeskController), typeof(DeskScanner), typeof(DeskDocument), typeof(DeskDraggable),
            typeof(DeskReaction), typeof(TravellerView), typeof(TravellerWheel), typeof(OverlayCallout),
            typeof(InvestigationUIController), typeof(InteractionPanelController), typeof(DesktopShell), typeof(CinemachineCameraRig),
            typeof(OfficeViewController), typeof(GameManager)
        };
        var readoutless = new HashSet<string> { "DeskStamp", "DeskMug", "DeskPlant", "ReactivePoster", "DeskIntercom", "ScannerTray" };

        var missing = new List<string>();
        var allowed = new List<string>();
        int checkedRefs = 0;
        foreach (Type type in checkedTypes)
        {
            Object[] found = Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (found.Length == 0)
                missing.Add($"no {type.Name} in the scene");
            foreach (Object component in found)
            {
                var c = (Component)component;
                SerializedProperty p = new SerializedObject(component).GetIterator();
                bool enter = true;
                while (p.NextVisible(enter))
                {
                    enter = p.propertyType == SerializedPropertyType.Generic;
                    if (p.propertyType != SerializedPropertyType.ObjectReference || p.name == "m_Script")
                        continue;
                    checkedRefs++;
                    if (p.objectReferenceValue != null)
                        continue;

                    string key = $"{type.Name}.{p.propertyPath}";
                    bool reactionAllowance = type == typeof(DeskReaction) &&
                        (p.name == "audioSource" && c.name != "CreditsTill" || p.name == "readout" && readoutless.Contains(c.name));
                    if (reactionAllowance || baseline.Contains(key))
                        allowed.Add($"{c.name}:{key}");
                    else
                        missing.Add($"{c.name}:{key}");
                }
            }
        }

        Report($"allowed null references: [{string.Join(", ", allowed)}]");
        Check(missing.Count == 0, $"no missed wiring ({checkedRefs} references walked): missing [{string.Join(", ", missing)}]");
    }

    /// <summary>
    /// An unsaved third build with the library's menu capacity 9, Desk_Default
    /// holding one spawn slot and a glass zone order equal to the exit zone's:
    /// the builder must report exactly those three problems; every edit is
    /// undone, the saved scene is reopened, and no file on disk may change.
    /// </summary>
    private static void BuilderReports()
    {
        string[] files = { ScenePath, LibraryPath, DeskConfigPath };
        string[] before = files.Select(FileHash).ToArray();

        var lib = AssetDatabase.LoadAssetAtPath<ContentLibrarySO>(LibraryPath);
        var config = AssetDatabase.LoadAssetAtPath<DeskConfigSO>(DeskConfigPath);
        int capacity = lib.Interview.menuCapacity;
        Vector2[] slots = config.paperSpawnSlots;
        int glass = config.glassOrder;
        List<string> build3;
        try
        {
            lib.Interview.menuCapacity = 9;
            config.paperSpawnSlots = new[] { new Vector2(0.5f, 0.5f) };
            config.glassOrder = config.focusExitOrder;
            build3 = RunMenu(BuildMenu);
        }
        finally
        {
            lib.Interview.menuCapacity = capacity;
            config.paperSpawnSlots = slots;
            config.glassOrder = glass;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); // drops the unsaved third build

        string[] errors = build3.Where(l => l.StartsWith("[Error]")).OrderBy(l => l, StringComparer.Ordinal).ToArray();
        string[] expected = new[]
        {
            "[Error] [TimeDesk] The traveller wheel fits 8 choices, but the content library's interview menu capacity is 9; lower interview.menuCapacity in world_source.json or enlarge the wheel (Desk_Default: wheelRadii, wheelItemSize).",
            "[Error] [TimeDesk] The desk has 1 paper spawn slots but a traveller can carry 2 papers; add slots in Desk_Default.",
            "[Error] [TimeDesk] The desk's sorting bands overlap: the glass zone (10) must sit above the focus exit zone (10); fix the orders in Desk_Default."
        }.OrderBy(l => l, StringComparer.Ordinal).ToArray();
        Check(errors.SequenceEqual(expected), $"an unsaved build with capacity 9, one spawn slot and the glass zone at the exit zone's order reports exactly those three: [{string.Join(" | ", errors)}]");
        Check(files.Select(FileHash).SequenceEqual(before), $"the unsaved build changed no file on disk ({string.Join(", ", files)})");
    }

    // -----------------------------
    // The semantic dump
    // -----------------------------

    /// <summary>
    /// The scene as the builder shapes it: every object's path, active flag and
    /// component types; transforms (0.001) and rect geometry (0.1); TMP text
    /// settings; sprite renderers, sorting groups, colliders and canvases; every
    /// Button's persistent calls; and every serialized value of the project's own
    /// components (references as target paths, persistent calls included).
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
        lines.Add($"{path} transform pos={V3(t.localPosition)} rot={V3(t.localEulerAngles)} scale={V3(t.localScale)}");

        if (t is RectTransform rt)
            lines.Add($"{path} rect min={V(rt.anchorMin)} max={V(rt.anchorMax)} pivot={V(rt.pivot)} pos={V(rt.anchoredPosition)} size={V(rt.sizeDelta)}");

        foreach (TMP_Text text in t.GetComponents<TMP_Text>())
            lines.Add($"{path} text='{text.text}' size={(text.enableAutoSizing ? $"auto {text.fontSizeMin:0.#}-{text.fontSizeMax:0.#}" : text.fontSize.ToString("0.#"))} wrap={text.textWrappingMode} overflow={text.overflowMode} style={text.fontStyle} raycast={text.raycastTarget}");
        foreach (SpriteRenderer sr in t.GetComponents<SpriteRenderer>())
            lines.Add($"{path} sprite={(sr.sprite != null ? sr.sprite.name : "none")} enabled={sr.enabled} order={sr.sortingOrder} color={sr.color}");
        foreach (SortingGroup g in t.GetComponents<SortingGroup>())
            lines.Add($"{path} sortingGroup order={g.sortingOrder}");
        foreach (BoxCollider2D box in t.GetComponents<BoxCollider2D>())
            lines.Add($"{path} box offset={V(box.offset)} size={V(box.size)} enabled={box.enabled}");
        foreach (Canvas canvas in t.GetComponents<Canvas>())
            lines.Add($"{path} canvas mode={canvas.renderMode} order={canvas.sortingOrder} camera={(canvas.worldCamera != null ? canvas.worldCamera.name : "none")}");
        foreach (Button button in t.GetComponents<Button>())
            lines.Add($"{path} button calls={PersistentCallsText(button.onClick)}");

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
                    SerializedPropertyType.Vector2 => V(p.vector2Value),
                    SerializedPropertyType.Vector3 => V3(p.vector3Value),
                    _ => null
                };
                if (value != null)
                    lines.Add($"{path} {mb.GetType().Name}.{p.propertyPath}={value}");
            }
        }

        for (int i = 0; i < t.childCount; i++)
            Dump(t.GetChild(i), lines);
    }

    private static string PersistentCallsText(UnityEventBase e)
    {
        var calls = new List<string>();
        for (int i = 0; i < e.GetPersistentEventCount(); i++)
        {
            Object target = e.GetPersistentTarget(i);
            calls.Add($"{Target(target)}.{e.GetPersistentMethodName(i)}");
        }
        return "[" + string.Join(", ", calls) + "]";
    }

    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

    private static string V(Vector2 v) => $"({v.x:0.0},{v.y:0.0})";

    private static string V3(Vector3 v) => $"({v.x:0.000},{v.y:0.000},{v.z:0.000})";

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
Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe' -ArgumentList @('-projectPath','E:\unity\NOPE-feat-clock','-executeMethod','TimeDeskP7Build.Run','-logFile','C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\unity_p7_build.log') -PassThru -Wait
```
Expected: exit code 0 and `SCRATCH/p7_build_report.txt` ending `fails=0`, with no `FAIL` line. In order it holds: both Generate World runs `ran=True`, each logging `World generated: …`; `PASS Generate World changes no committed data file: []`; `PASS a second Generate World changes nothing: []`; `PASS Validate Content Library reports no issues`; `PASS a traveller hands over at most 1 document on request (1) and carries at most 2 papers (2)`; the first build `ran=True` and `PASS the first Build Office UI logs no error (…)`; the scene checks: `PASS the desktop Canvas sits under CRTMonitor/ScreenAnchor: …`, `PASS no IntercomPanel, BackToOfficeButton, …`, `PASS ReadySign has no persistent call`, `PASS CRTMonitor: [OfficeViewController.FocusMonitor]`, `PASS PowerButton: [MonitorScreen.TogglePower]`, `PASS FocusExitZone is inactive with FocusOffice [OfficeViewController.FocusOffice]`, `PASS GlassZone is inactive, …`, `PASS TravellerHitZone and DeskIntercom call TravellerWheel.Open`, `PASS the overlay hosts …`, `PASS Ring/Centre ignores the layout; …`, `PASS the IconScanner label reads 'Deviation Report', its window 'Deviation Report', Material 'Material Analysis'`, `PASS the booth raycaster holds 16 hits`, `PASS Desk_Default and the ten reaction assets exist (no clips)`; `allowed null references: [...]` (the props' `DeskReaction.readout` on DeskStamp, DeskMug, DeskPlant, ReactivePoster, DeskIntercom and ScannerTray, every prop's `DeskReaction.audioSource` but CreditsTill's, and any Task 0 baseline null, by name) and `PASS no missed wiring (… references walked): missing []`; the second build `ran=True` and `PASS the second Build Office UI logs no error`; `PASS the two builds dump the same scene (… lines): first-only [] second-only []`; the third build `ran=True` with its three errors, then `PASS an unsaved build with capacity 9, one spawn slot and the glass zone at the exit zone's order reports exactly those three: [...]` ("The traveller wheel fits 8 choices, but the content library's interview menu capacity is 9; …", "The desk has 1 paper spawn slots but a traveller can carry 2 papers; …" and "The desk's sorting bands overlap: the glass zone (10) must sit above the focus exit zone (10); …") and `PASS the unsaved build changed no file on disk (…)`.

- [ ] **Step 5: Hygiene**

```bash
cd /e/unity/NOPE-feat-clock && rm -f Assets/Editor/_TimeDeskP7Build.cs Assets/Editor/_TimeDeskP7Build.cs.meta && git checkout -- Assembly-CSharp.csproj Assembly-CSharp-Editor.csproj TimeDesk.Domain.csproj TimeDeskEditMode.csproj NOPE.sln ProjectSettings "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset" && git status --short
```
Expected: modified `Assets/Scenes/OfficeScene.unity` and `Assets/Art/Generated/xp_bliss.png.meta`; untracked `Assets/Art/Office/Placeholder/paper.png`, `crt_power.png`, `crt_led.png` (each with its `.meta`), `Assets/Data/Config/Desk_Default.asset` (+ `.meta`), `Assets/Data/Config/DeskReactions/` and `Assets/Data/Config/DeskReactions.meta`, plus `NOPE-feat-clock.sln` and `TimeDesk.Visuals.csproj`. Spot-check `git diff Assets/Art/Generated/xp_bliss.png.meta` (only the mipmap settings change: `enableMipMap: 1`) and `ls Assets/Data/Config/DeskReactions` (the ten `Reaction_*.asset`: Calendar, Clock, Intercom, Mug, Plant, Poster, Scanner, Stability, Stamp, Till, each with a `.meta`). Unity may re-save another existing asset in its own YAML shape (`git diff` shows only formatting); revert such a file with `git checkout -- <path>`. Any other changed file means the builder touched something unexpected: stop and investigate.

- [ ] **Step 6: Commit the desk's assets**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Data/Config/Desk_Default.asset Assets/Data/Config/Desk_Default.asset.meta Assets/Data/Config/DeskReactions Assets/Data/Config/DeskReactions.meta Assets/Art/Office/Placeholder/paper.png Assets/Art/Office/Placeholder/paper.png.meta Assets/Art/Office/Placeholder/crt_power.png Assets/Art/Office/Placeholder/crt_power.png.meta Assets/Art/Office/Placeholder/crt_led.png Assets/Art/Office/Placeholder/crt_led.png.meta Assets/Art/Generated/xp_bliss.png.meta && git commit -F - <<'EOF'
content(desk): the desk's tuning, the props' reactions and placeholders

Build Office UI creates Desk_Default (the desk's knobs at the spec's
defaults) and the ten props' reactions, and draws the paper, the CRT's
power button and its LED as placeholders; the wallpaper gains mipmaps
(the live monitor shows it small).

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

- [ ] **Step 7: FEATURES** — save as `SCRATCH/p7_t19_features.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

# The behaviour the rebuilt scene gets (spec section 3.4): the live monitor,
# screen power, the desk, the wheel and the bubble. Committed with the rebuilt
# OfficeScene (Step 8), the commit that gives players this behaviour.
apply(W + r'\docs\FEATURES.md', [
("""whole-day determinism is checked in Unity, not by the EditMode suite)""",
"""whole-day determinism is checked in Unity, not by the EditMode suite); desk interactions (papers, scans, the wheel) draw no random numbers and never change generation"""),
("""## Office scene — two-state booth

- [ ] World-space booth (back wall, partitions, desk, traveller, CRT, READY sign) with Cinemachine office/monitor cameras
- [ ] Click CRT or READY sign → zoom into monitor (desktop UI); Escape or "< Office" button → pull back
- [ ] Diegetic readouts: wall calendar (day), stability monitor (percent + lamp tint), credits till (with ding)""",
"""## Office scene — two-state booth

- [ ] World-space booth (back wall, partitions, desk, CRT with its live desktop and power button, READY sign, desk scanner and props; the traveller appears when READY is tapped and leaves at the decision) with Cinemachine office/monitor cameras
- [ ] Click the CRT → the camera pushes in until the screen fills most of the view (`DeskConfigSO`: fill 0.85, 0.6 s); the desktop takes input only once the push-in settles; while focused only the desktop and the bezel power button take input; leave with Escape, a click outside the screen or the taskbar "< Desk" button (input table tested: `BoothRulesTests`; framing tested: `MonitorFramingTests`)
- [ ] Live monitor: the PC desktop is drawn on the CRT glass in the booth view (4:3, 1440×1080 units, masked to the screen), and keeps running while you work at the desk; between travellers it reads "Waiting for the next traveller"
- [ ] Screen power: the bezel button (both views) or Start ▸ Turn off screen darkens the screen; the PC keeps running (scans open windows, the clock ticks); a new traveller or a finished scan wakes it (`DeskConfigSO` toggles); while a citation slip waits for Acknowledge the screen stays on and cannot be turned off (power, wake and hold rules tested: `PcScreenTests`; the power button's citation row tested: `BoothRulesTests`)
- [ ] Diegetic readouts: wall calendar (day), stability monitor (percent + lamp tint), credits till (with ding); clicking the calendar, stability monitor, till or wall clock shows its value as a tooltip"""),
("""- [ ] Morning briefing as "THE TEMPORAL TIMES" newsletter over the booth (Start Shift)
- [ ] End-of-day "SHIFT LEDGER" newsletter over the booth (Go Home), incl. the undocumented-denials line ("log a deviation before denying")
- [ ] Per-case READY gate: visitor is presented only after the player taps READY (tested: `ReadyGateTests`)
- [ ] Analog wall clock (placeholder face + hands) driven by the shift clock
""",
"""- [ ] Morning briefing as "THE TEMPORAL TIMES" newsletter over the booth (Start Shift) (the booth ignores clicks while a newsletter is up; tested: `BoothRulesTests`)
- [ ] End-of-day "SHIFT LEDGER" newsletter over the booth (Go Home), incl. the undocumented-denials line ("log a deviation before denying") (the booth ignores clicks while a newsletter is up; tested: `BoothRulesTests`)
- [ ] Per-case READY gate: visitor is presented only after the player taps READY (READY no longer zooms in) (tested: `ReadyGateTests`)
- [ ] Analog wall clock (placeholder face + hands) driven by the shift clock

## Desk

- [ ] Papers: a traveller hands each document over once, the passport when they step up and the permit when requested (`DocumentTemplateSO` hand-over); papers show the title and the traveller's name (a hidden photo slot is reserved for piece 4); drag them anywhere on the desk (they stay where dropped, on top); a sliding paper cannot be picked up (hand-over and which states can be dragged tested: `DeskPapersTests`; clamping and stacking tested: `DeskGeometryTests`)
- [ ] Desk scanner: drop a paper on it to scan (1.5 s, one at a time, the clock keeps running); the paper returns to where it was picked up and its scanned window opens on the PC with a desktop icon; a paper dropped while the scanner is busy slides back (scan states, drop outcomes and one-at-a-time tested: `DeskPapersTests`)
- [ ] Day 1 desk notes: by the scanner until the first scan, and above the traveller until the wheel is first opened that day (when each shows tested: `DeskPapersTests`)
- [ ] Traveller wheel: click the traveller or the desk intercom while they are at the desk; the interview's choices on an elliptical ring (up to 8), sub-menus in place with "< Back" in the centre; Escape or a click outside closes it; a request closes it (layout and fit tested: `RadialLayoutTests`)
- [ ] Speech bubble: the traveller's reply to a wheel choice shows beside them for 4 s; a choice without a reply ("Ask about home >", "< Back") leaves the last reply up (read-only; the PC transcript is the evidence; which lines form the reply tested: `InterviewScriptTests`)
- [ ] Desk objects react to clicks (squash, wobble, nudge, pulse; `DeskReactionSO`) (poses tested: `ReactionCurveTests`); plant and mug stand at named desk slots for decoration later
"""),
("""- [ ] XP-style wallpaper, taskbar with Start button and system tray (Day / Credits / Stability / Clock)
- [ ] Start menu: Settings (stub window) + Power (quit); fixed-height entries""",
"""- [ ] XP-style wallpaper, taskbar with Start button and system tray (Day / Credits / Stability / Clock) and a "< Desk" button
- [ ] Start menu: Settings (stub window), Turn off screen, Quit game; fixed-height entries"""),
("""- [ ] Placeholder apps: Internet, Lexicon, Dialect, Material, Notes""",
"""- [ ] Placeholder apps: Internet, Lexicon, Dialect, Material (its window titled "Material Analysis"), Notes"""),
("""answer rows are compare-clickable; every intercom choice except a document request opens it""",
"""answer rows are compare-clickable; every wheel choice except a document request opens it"""),
("""- [ ] Intercom = the interview: every entry is a dialog choice. Hub: "Request <document>" for each document handed over on request (one-shot; the traveller hands it over and its window opens; a document handed over on arrival, the passport, opens when the traveller is presented; `DocumentTemplateSO` hand-over), "Ask about home >" ("< Back" first, then today's questions, one-shot per traveller, and small talk) and today's narrative dialogs; content never offers more choices than the intercom shows (8), and the way back and the requests come first""",
"""- [ ] Traveller wheel = the interview: every entry is a dialog choice. Hub: "Request <document>" for each document handed over on request (one-shot; the traveller hands it over onto the desk, or straight to its window where no desk is wired; a document handed over on arrival, the passport, lands on the desk, or opens, when the traveller is presented; `DocumentTemplateSO` hand-over), "Ask about home >" ("< Back" first, in the wheel's centre, then today's questions, one-shot per traveller, and small talk) and today's narrative dialogs; content never offers more choices than the wheel shows (8), and the way back and the requests come first"""),
("""- [ ] Documents render as SCANNED pages (white page + photo placeholder on dark scanner backing), multi-page, structured fields""",
"""- [ ] A paper's scanned copy renders as a SCANNED page (white page + photo placeholder on dark scanner backing), multi-page, structured fields"""),
("""- [ ] Scanner = Deviation Report: true contradictions auto-register""",
"""- [ ] Deviation Report (the desktop app formerly titled "Scanner"): true contradictions auto-register"""),
("""- [ ] The clock pauses only while a citation slip is shown (the interview takes real time and costs nothing else)""",
"""- [ ] The clock pauses only while a citation slip is shown, and the slip keeps the PC screen on until acknowledged (the interview, scans and camera moves take real time and cost nothing else)"""),
("""- [ ] Hover highlight: white outline on visible booth clickables (a clickable whose sprite is hidden, a hit zone over other art, gets the hand cursor only)""",
"""- [ ] Hover highlight: white outline on visible booth clickables (CRT, READY, desk props, papers; a clickable whose sprite is hidden, a hit zone over other art such as the traveller, the calendar or the focus exit zone, gets the hand cursor only; the glass zone, which only keeps a click on the screen from leaving focus, shows the arrow)"""),
("""no menu is fuller than the intercom shows (requests count only documents handed over on request)""",
"""no menu is fuller than the traveller wheel shows (requests count only documents handed over on request)"""),
("""it reports an intercom that fits fewer choices than the content's menu capacity; the desktop and office-overlay canvases scale from 1920×1080 and never below it (a screen wider than 16:9 gets extra width, a narrower one extra height), so the intercom shows every choice it fits at the reference size on any screen""",
"""it reports a traveller wheel that fits fewer choices than the content's menu capacity, a desk with fewer paper slots than a traveller's papers, and overlapping sorting bands (bands tested: `DeskGeometryTests`); the desktop is a 1440×1080 World Space canvas on the CRT glass; the office-overlay canvas scales from 1920×1080 and never below it (a screen wider than 16:9 gets extra width, a narrower one extra height), so the wheel shows every choice it fits at the reference size on any screen"""),
])
```

Expected: `ok …docs\FEATURES.md crlf`. Read the diff once (`git diff docs/FEATURES.md`): the "two-state booth" heading and its lines describe the live monitor and the desk, and no line still says the intercom lists requests. This step runs once: when Task 19 is re-run after a `fix:` (Task 20), skip it (FEATURES is committed, and the `fix:` commit carries any FEATURES change of its own).

- [ ] **Step 8: Commit the rebuilt scene with its FEATURES lines**

```bash
cd /e/unity/NOPE-feat-clock && git add Assets/Scenes/OfficeScene.unity docs/FEATURES.md && git commit -F - <<'EOF'
feat(scene): rebuild OfficeScene with the physical desk

Build Office UI run twice (the semantic scene dumps are equal): the
desktop is drawn live on the CRT's glass, the desk holds papers, the
scanner and reacting props, the traveller wheel and the callouts are on
the overlay, and BoothCoordinator applies the booth's input rules.
FEATURES describes the booth, the desk, the scanner, the wheel, screen
power and the renames, shipping with the scene that plays them.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```
On a re-run after a `fix:` (Task 20), stage only the scene and name the fix: `chore(scene): rebuild OfficeScene after <the fix>`. `git status --short`: only `?? NOPE-feat-clock.sln` and `?? TimeDesk.Visuals.csproj`.

---

### Task 20: Unity verification

Proves spec §6 steps 1, 2, 3, 4 (idempotence of the committed scene), 5, 6 and 7 in the branch's own Unity 6000.4.11f1 through three temporary `-executeMethod` sessions (steps 0 and 4's first half were proven in Tasks 0 and 19). Session A is an editor script that also runs the EditMode suite through `TestRunnerApi` (`Assembly-CSharp-Editor.csproj` already references `UnityEditor.TestRunner`). Session B plays the rebuilt `OfficeScene` for days 1–3 of a new run and takes the screenshots. Session C plays `Test_DayLoop` and Codex's `OfficeScene_HybridArt` (opened and played, never saved). Then every screenshot is read and judged.

**When a check fails**, first decide where the defect is:
- **In a temporary `_TimeDesk*` script** (the automation is wrong, for example a TMP sub-mesh object that exists only in a loaded scene and not in a built one, or a wait that is too short): fix that script, commit nothing, re-run only the affected session.
- **In the product** (including a screenshot judged wrong in Step 9): fix it at its source (Domain or Visuals first, with a failing EditMode test, when the rule is theirs; the builder's constants or `Desk_Default`'s defaults for placement), pass the offline gate (Step 1), commit it as `fix: …`, re-run Task 19 Steps 3–6 and 8 (the scene and, for a changed default, the recreated `Desk_Default`; Step 7's FEATURES script has already run, so skip it) and then this task from Step 3. A fix that changes behaviour or a documented default (a `DeskConfigSO` or `DeskReactionSO` default, a builder constant the player sees) updates `docs/FEATURES.md` in the same `fix:` commit (for example the fill in the "Click the CRT" line), together with the spec's figures for that value. Record each such fix for the verification record (Task 21).

**Files:**
- Create (temporary, never committed): `Assets/Editor/_TimeDeskP7Automation.cs`, `Assets/Editor/_TimeDeskP7PlaySmoke.cs`, `Assets/Editor/_TimeDeskP7Scenes.cs`
- Output: `SCRATCH/p7_automation_report.txt`, `SCRATCH/p7_cases_now.txt`, `SCRATCH/p7_playsmoke_report.txt`, `SCRATCH/p7_scenes_report.txt`, the twelve `SCRATCH/p7_*.png` screenshots

What the automation must prove (spec §6):
- **Session A:** case generation for seeds 12345 and 999 × days 1–3 is byte-identical to Task 0's dump (V7); Generate World twice changes no file; Validate Content Library reports nothing; `InterviewReachable` ("spoken") is true in the rebuilt scene as it was before; two more builds of the loaded committed scene dump exactly Task 19's second dump, the committed scene as loaded dumps the same, and the scene file is unchanged; the EditMode suite passes except the known third-party `UnitySkills.Tests.Core.PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly`.
- **Session B** (a run seed from 12345 up, picked with the same `CaseFactory` call, whose day 1 opens with a liar with a passport Currency or Language tell, then an honest traveller, then a non-legendary traveller, and whose day 2 opens with a liar whose only tell is the spoken capital; the player's save moved aside and restored): the play-through of spec §6 step 5, steps 5.1–5.16, every press target resolved through `EventSystem.RaycastAll` first (the desktop's passport field row and transcript answer row included, on the focused desktop; book rows, on pages a book shows only while searched, are invoked directly), one drag-to-scan (5.6) and one plain paper click (5.7) through a virtual mouse and `InputSystemUIInputModule`, and the twelve screenshots.
- **Session C:** `Test_DayLoop` plays one case through its era buttons with no error and no warning naming a piece-7 part; `OfficeScene_HybridArt` plays one full case on the no-desk path (the passport's window and icon at presentation, the one-shot permit request from its intercom list, a compared passport field, Deny), with exactly one "Desk scanner not wired" warning, nothing about the brain, and no error. Its log also carries the scene's existing "Traveller wheel or interview transcript not wired" warning: the hybrid scene wires no transcript, so `InterviewReachable` is false there and its days generate with `spoken=false`. READY is pressed only once the day loop has armed it.

- [ ] **Step 1: Offline gate**

Run the compile check and the test run. Expected: exit 0 / exit 0; `passed 433, failed 0`. `git status --short` shows only the two untracked files. `SCRATCH/p7_scene_dump_2.txt` exists (Task 19).

- [ ] **Step 2: Check no Unity editor is open on the worktree** (PowerShell)

Run: `Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Select-Object ProcessId, CommandLine`
Expected: none on `NOPE-feat-clock`.

- [ ] **Step 3: Write session A** (`Assets/Editor/_TimeDeskP7Automation.cs`, Write tool)

```csharp
// TEMPORARY automation for piece 7 (the physical desk); never committed.
// Session A of the verification: case generation unchanged (spec section 6
// step 3), Generate World idempotent and the validator clean (step 2), the
// committed scene equal to the builder's output twice over (step 4), then the
// EditMode suite (step 1). Reports go to the scratchpad.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class TimeDeskP7Automation
{
    private const string Scratch = @"C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad";
    private static readonly string ReportPath = Path.Combine(Scratch, "p7_automation_report.txt");
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
            var lib = AssetDatabase.LoadAssetAtPath<ContentLibrarySO>(LibraryPath);

            // --- Generation unchanged (step 3) ---
            string[] baseline = File.ReadAllLines(Path.Combine(Scratch, "p7_baseline_cases.txt"));
            List<string> now = CaseLines(lib);
            File.WriteAllLines(Path.Combine(Scratch, "p7_cases_now.txt"), now);
            Check(baseline.SequenceEqual(now),
                  $"case generation is byte-identical to the baseline ({now.Count} slots): first difference [{string.Join(" || ", baseline.Except(now).Take(2))}] vs [{string.Join(" || ", now.Except(baseline).Take(2))}]");

            // --- Content (step 2) ---
            Dictionary<string, string> before = HashDataFolder();
            RunMenu("Tools/TimeDesk/Generate World");
            Dictionary<string, string> first = HashDataFolder();
            RunMenu("Tools/TimeDesk/Generate World");
            Dictionary<string, string> second = HashDataFolder();
            Check(Changed(before, first).Count == 0 && Changed(first, second).Count == 0,
                  $"Generate World twice changes no file: [{string.Join(", ", Changed(before, first).Concat(Changed(first, second)))}]");
            List<string> validator = RunMenu("Tools/TimeDesk/Validate Content Library");
            Check(validator.Any(l => l.Contains("no issues found")) && !validator.Any(l => l.StartsWith("[Error]") || l.StartsWith("[Warning]")),
                  "Validate Content Library reports no issues");

            // --- The committed scene is the builder's output (step 4), and spoken (V7) ---
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            string sceneHash = FileHash(ScenePath);
            InvestigationUIController invest = Object.FindObjectsByType<InvestigationUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
            bool baselineSpoken = File.ReadAllLines(Path.Combine(Scratch, "p7_baseline_scene.txt")).Contains("spoken=True");
            Check(invest.InterviewReachable && baselineSpoken, $"spoken (InterviewReachable) is {invest.InterviewReachable} in the rebuilt scene and was {baselineSpoken} before piece 7");

            List<string> committed = DumpScene();
            List<string> build1 = RunMenu(BuildMenu);
            List<string> dump1 = DumpScene();
            List<string> build2 = RunMenu(BuildMenu);
            List<string> dump2 = DumpScene();
            string[] recorded = File.ReadAllLines(Path.Combine(Scratch, "p7_scene_dump_2.txt"));
            Check(!build1.Concat(build2).Any(l => l.StartsWith("[Error]") || l.StartsWith("[Exception]")), "two more builds log no error");
            Check(dump1.SequenceEqual(recorded) && dump2.SequenceEqual(recorded),
                  $"two more builds dump exactly Task 19's second build ({recorded.Length} lines): differences [{string.Join(" || ", dump1.Except(recorded).Concat(dump2.Except(recorded)).Take(20))}]");
            Check(committed.SequenceEqual(recorded),
                  $"the committed scene, as loaded, dumps the same ({committed.Count} lines): loaded-only [{string.Join(" || ", committed.Except(recorded).Take(20))}] recorded-only [{string.Join(" || ", recorded.Except(committed).Take(20))}]");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); // drops the unsaved builds
            Check(FileHash(ScenePath) == sceneHash, "the scene file is unchanged");
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

    private static List<string> Changed(Dictionary<string, string> a, Dictionary<string, string> b) =>
        a.Keys.Union(b.Keys).Where(k => !a.TryGetValue(k, out string x) || !b.TryGetValue(k, out string y) || x != y).OrderBy(k => k).ToList();

    /// <summary>Exactly Task 0's baseline (_TimeDeskP7Baseline.CaseLines).</summary>
    private static List<string> CaseLines(ContentLibrarySO lib)
    {
        var lines = new List<string>();
        foreach (int seed in new[] { 12345, 999 })
        {
            for (int day = 1; day <= 3; day++)
            {
                DayPlanSO plan = lib.GetDayPlan(day);
                var world = new WorldState { day = day };
                InterviewDay interview = TimelineService.BuildInterviewDay(lib, world, new ShiftLedger());
                var factory = new CaseFactory(lib, lib.BuildFactTable(plan));
                List<CaseInstance> cases = factory.GenerateDayCases(plan, world, Seeds.Day(seed, day), interview.AskableCategories, interview.AnswerTellCategories);
                var violators = (Dictionary<int, NationEraProfileSO>)typeof(CaseFactory)
                    .GetField("_violators", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(factory);
                for (int i = 0; i < cases.Count; i++)
                    lines.Add(Line(seed, day, i + 1, cases[i], violators.ContainsKey(i + 1)));
            }
        }
        return lines;
    }

    private static string Line(int seed, int day, int slot, CaseInstance c, bool violator)
    {
        List<DocumentField> fields = c.documents.SelectMany(d => d.fields).ToList();
        IEnumerable<string> tells = fields.Where(f => f.isAnachronism).Select(f => $"{f.category}/Papers").Distinct()
            .Concat(c.answers.Where(a => a.isTell).Select(a => $"{a.category}/Answer"));
        string papers = string.Join(";", c.documents.Select(d => $"{(d.template != null ? d.template.displayName : "?")}:" +
                                                                string.Join(",", d.fields.Select(f => $"{f.label}={f.value}"))));
        string answers = string.Join(";", c.answers.Select(a => $"{a.category}={a.value}{(a.isTell ? "[TELL]" : "")}"));
        return $"seed={seed} day={day} slot={slot} claim='{c.originLabel}' name='{c.visitorGivenName}' born='{c.trueBirthDate}' role='{(c.archetype != null ? c.archetype.displayName : "")}' allowed={c.claimAllowedByRules} violator={violator}" +
               $" | liar={c.IsLiar} home='{c.HomeLabel}' tells=[{string.Join(",", tells)}] | papers={papers} | answers={answers}";
    }

    // -----------------------------
    // The semantic dump (exactly Task 19's)
    // -----------------------------

    private static List<string> DumpScene()
    {
        Canvas.ForceUpdateCanvases();
        var lines = new List<string>();
        foreach (GameObject go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            Dump(go.transform, lines);
        return lines;
    }

    private static void Dump(Transform t, List<string> lines)
    {
        string path = PathOf(t);
        lines.Add($"{path} active={t.gameObject.activeSelf} components=[{string.Join(",", t.GetComponents<Component>().Select(c => c != null ? c.GetType().Name : "Missing"))}]");
        lines.Add($"{path} transform pos={V3(t.localPosition)} rot={V3(t.localEulerAngles)} scale={V3(t.localScale)}");

        if (t is RectTransform rt)
            lines.Add($"{path} rect min={V(rt.anchorMin)} max={V(rt.anchorMax)} pivot={V(rt.pivot)} pos={V(rt.anchoredPosition)} size={V(rt.sizeDelta)}");

        foreach (TMP_Text text in t.GetComponents<TMP_Text>())
            lines.Add($"{path} text='{text.text}' size={(text.enableAutoSizing ? $"auto {text.fontSizeMin:0.#}-{text.fontSizeMax:0.#}" : text.fontSize.ToString("0.#"))} wrap={text.textWrappingMode} overflow={text.overflowMode} style={text.fontStyle} raycast={text.raycastTarget}");
        foreach (SpriteRenderer sr in t.GetComponents<SpriteRenderer>())
            lines.Add($"{path} sprite={(sr.sprite != null ? sr.sprite.name : "none")} enabled={sr.enabled} order={sr.sortingOrder} color={sr.color}");
        foreach (SortingGroup g in t.GetComponents<SortingGroup>())
            lines.Add($"{path} sortingGroup order={g.sortingOrder}");
        foreach (BoxCollider2D box in t.GetComponents<BoxCollider2D>())
            lines.Add($"{path} box offset={V(box.offset)} size={V(box.size)} enabled={box.enabled}");
        foreach (Canvas canvas in t.GetComponents<Canvas>())
            lines.Add($"{path} canvas mode={canvas.renderMode} order={canvas.sortingOrder} camera={(canvas.worldCamera != null ? canvas.worldCamera.name : "none")}");
        foreach (Button button in t.GetComponents<Button>())
            lines.Add($"{path} button calls={PersistentCallsText(button.onClick)}");

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
                    SerializedPropertyType.Vector2 => V(p.vector2Value),
                    SerializedPropertyType.Vector3 => V3(p.vector3Value),
                    _ => null
                };
                if (value != null)
                    lines.Add($"{path} {mb.GetType().Name}.{p.propertyPath}={value}");
            }
        }

        for (int i = 0; i < t.childCount; i++)
            Dump(t.GetChild(i), lines);
    }

    private static string PersistentCallsText(UnityEventBase e)
    {
        var calls = new List<string>();
        for (int i = 0; i < e.GetPersistentEventCount(); i++)
            calls.Add($"{Target(e.GetPersistentTarget(i))}.{e.GetPersistentMethodName(i)}");
        return "[" + string.Join(", ", calls) + "]";
    }

    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

    private static string V(Vector2 v) => $"({v.x:0.0},{v.y:0.0})";

    private static string V3(Vector3 v) => $"({v.x:0.000},{v.y:0.000},{v.z:0.000})";

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

    private sealed class Callbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            Report($"tests passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount} state={result.ResultState}");
            Report($"fails={_fails}");
            EditorApplication.Exit(_fails > 0 ? 4 : result.FailCount == 0 ? 0 : 3);
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
Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe' -ArgumentList @('-projectPath','E:\unity\NOPE-feat-clock','-executeMethod','TimeDeskP7Automation.Run','-logFile','C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\unity_p7_automation.log') -PassThru -Wait
```
Expected in `SCRATCH/p7_automation_report.txt`: only `PASS` lines before the summary: `PASS case generation is byte-identical to the baseline (60 slots): …`, `PASS Generate World twice changes no file: []`, `PASS Validate Content Library reports no issues`, `PASS spoken (InterviewReachable) is True in the rebuilt scene and was True before piece 7`, `PASS two more builds log no error`, `PASS two more builds dump exactly Task 19's second build (… lines): differences []`, `PASS the committed scene, as loaded, dumps the same (… lines): …`, `PASS the scene file is unchanged`; then `tests passed=562 failed=1 …` (433 Domain and Visuals tests + the 129 other EditMode tests counted since piece 2) with the only `FAIL` line being `UnitySkills.Tests.Core.PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly`, and `fails=0`. Exit code 3 comes from that known failure.

- [ ] **Step 5: Write session B** (`Assets/Editor/_TimeDeskP7PlaySmoke.cs`, Write tool)

```csharp
// TEMPORARY play-mode verification for piece 7 (the physical desk); never
// committed. Picks a run seed (from 12345 up) whose day 1 opens with a liar
// with a passport Currency/Language tell, then an honest traveller, and whose
// day 2 opens with a liar whose only tell is the spoken capital; plays days
// 1-3 of a new run on the rebuilt OfficeScene (spec section 6 step 5): papers,
// the wheel and the bubble, drag-to-scan with a virtual mouse, props and
// tooltips, focus and the 720p framing, screen power, the exits, the citation
// hold, closing with a scan running and with READY armed, the outline purge,
// the day-2 spoken tell; screenshots go to the scratchpad. The play-through
// is one coroutine driven from EditorApplication.update (play mode has one
// domain reload, at entry; scene loads keep it); SessionState carries the
// setup across the entry and exit reloads.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class TimeDeskP7PlaySmoke
{
    private const string Scratch = @"C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad";
    private static readonly string ReportPath = Path.Combine(Scratch, "p7_playsmoke_report.txt");
    private static readonly string SaveBackup = Path.Combine(Scratch, "nope_save_backup_p7.json");
    private const string LibraryPath = "Assets/Data/Content Library/ContentLibrary_Main.asset";
    private const string ViewName = "TimeDeskP7";

    private const string StepKey = "P7Smoke.Step";
    private const string SeedKey = "P7Smoke.Seed";
    private const string NamesKey = "P7Smoke.Names";
    private const string FailsKey = "P7Smoke.Fails";
    private const string LogKey = "P7Smoke.Log";
    private const string HadSaveKey = "P7Smoke.HadSave";
    private const string ViewWidthKey = "P7Smoke.ViewWidth";
    private const string ViewHeightKey = "P7Smoke.ViewHeight";

    private const int Playing = 1;
    private const int Finishing = 2;

    /// <summary>The play-through, driven one step per editor update while playing (nested enumerators run first).</summary>
    private static readonly Stack<IEnumerator> Script = new Stack<IEnumerator>();

    private static Mouse _mouse;
    private static Keyboard _keyboard;
    private static InputSettings.EditorInputBehaviorInPlayMode _inputBehaviour;

    static TimeDeskP7PlaySmoke()
    {
        if (SessionState.GetInt(StepKey, 0) == 0)
            return;
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
    }

    public static void Run()
    {
        File.WriteAllText(ReportPath, "start\n");
        var lib = AssetDatabase.LoadAssetAtPath<ContentLibrarySO>(LibraryPath);

        int seed = 0;
        string[] names = null;
        for (int s = 12345; s <= 17345 && names == null; s++)
        {
            names = PickRoles(lib, s);
            seed = s;
        }
        if (names == null)
        {
            Report("FAIL no run seed in 12345..17345 opens day 1 with a passport-tell liar and an honest traveller and day 2 with a spoken-capital liar");
            EditorApplication.Exit(5);
            return;
        }
        Report($"seed={seed} day-1 slots 1-3 and day-2 slot 1: {string.Join(" | ", names)}");

        // A fresh run on that seed: the player's save is moved aside (restored in
        // Finish; the path is reported for a manual restore if Unity dies first)
        // and RunConfig is pinned (saved, so it survives the play-mode domain
        // reload; the hygiene step reverts the asset).
        bool hadSave = File.Exists(SaveSystem.SavePath);
        Report($"save path={SaveSystem.SavePath} hadSave={hadSave}");
        if (hadSave)
        {
            File.Copy(SaveSystem.SavePath, SaveBackup, true);
            File.Delete(SaveSystem.SavePath);
        }
        var runConfig = new SerializedObject(Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath));
        runConfig.FindProperty("fixedRunSeed").intValue = seed;
        runConfig.FindProperty("startingDay").intValue = 1;
        runConfig.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();

        PlayModeWindow.GetRenderingResolution(out uint width, out uint height);
        SessionState.SetInt(ViewWidthKey, (int)width);
        SessionState.SetInt(ViewHeightKey, (int)height);
        PlayModeWindow.SetCustomRenderingResolution(1920, 1080, ViewName);

        SessionState.SetInt(SeedKey, seed);
        SessionState.SetString(NamesKey, string.Join("|", names));
        SessionState.SetInt(HadSaveKey, hadSave ? 1 : 0);
        SessionState.SetInt(FailsKey, 0);
        SessionState.SetString(LogKey, string.Empty);
        SessionState.SetInt(StepKey, Playing);

        EditorSceneManager.OpenScene("Assets/Scenes/OfficeScene.unity", OpenSceneMode.Single);
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
        EditorApplication.EnterPlaymode();
    }

    /// <summary>The names of day 1's slots 1-3 and day 2's slot 1 when the seed gives the roles the play-through needs, else null.</summary>
    private static string[] PickRoles(ContentLibrarySO lib, int seed)
    {
        List<CaseInstance> day1 = Generate(lib, seed, 1);
        List<CaseInstance> day2 = Generate(lib, seed, 2);
        if (day1.Count < 4 || day2.Count < 2)
            return null;

        bool passportLiar = !day1[0].isLegendary && day1[0].IsLiar && PassportTell(day1[0]) != null;
        bool honest = !day1[1].isLegendary && !day1[1].IsLiar && day1[1].claimAllowedByRules;
        List<InterviewAnswer> spoken = day2[0].answers.Where(a => a.isTell).ToList();
        bool capital = !day2[0].isLegendary && day2[0].IsLiar && spoken.Count == 1 && spoken[0].category == ClueCategory.Geography;
        return passportLiar && honest && capital && !day1[2].isLegendary
            ? new[] { day1[0].visitorGivenName, day1[1].visitorGivenName, day1[2].visitorGivenName, day2[0].visitorGivenName }
            : null;
    }

    /// <summary>A day's travellers exactly as GameManager generates them for a new run (no effects, no modifiers).</summary>
    private static List<CaseInstance> Generate(ContentLibrarySO lib, int seed, int day)
    {
        DayPlanSO plan = lib.GetDayPlan(day);
        var world = new WorldState { day = day };
        InterviewDay interview = TimelineService.BuildInterviewDay(lib, world, new ShiftLedger());
        return new CaseFactory(lib, lib.BuildFactTable(plan)).GenerateDayCases(plan, world, Seeds.Day(seed, day), interview.AskableCategories, interview.AnswerTellCategories);
    }

    /// <summary>The passport's Currency or Language tell field (proven against a book), or null.</summary>
    private static DocumentField PassportTell(CaseInstance c) =>
        c.documents.Where(d => d.template != null && d.template.displayName == "Travel Passport")
                   .SelectMany(d => d.fields)
                   .FirstOrDefault(f => f.isAnachronism && f.page == 0 && (f.category == ClueCategory.Currency || f.category == ClueCategory.Language));

    private static void Tick()
    {
        int step = SessionState.GetInt(StepKey, 0);
        if (step == Finishing)
        {
            if (!EditorApplication.isPlaying)
                Finish();
            return;
        }

        if (step != Playing || !EditorApplication.isPlaying)
            return;

        try
        {
            if (Script.Count == 0)
                Script.Push(Play());

            while (Script.Count > 0)
            {
                IEnumerator top = Script.Peek();
                if (!top.MoveNext())
                {
                    Script.Pop();
                    continue;
                }

                if (top.Current is IEnumerator nested)
                {
                    Script.Push(nested);
                    continue;
                }

                return; // yielded: resume on the next update
            }

            EndPlay();
        }
        catch (Exception e)
        {
            Check(false, $"exception: {e}");
            Script.Clear();
            EndPlay();
        }
    }

    private static void EndPlay()
    {
        RestoreInput();
        SessionState.SetInt(StepKey, Finishing);
        EditorApplication.ExitPlaymode();
    }

    // -----------------------------
    // The play-through
    // -----------------------------

    private static IEnumerator Play()
    {
        yield return Until(() => RunManager.HasInstance && Object.FindFirstObjectByType<GameManager>() != null, 30f, "the office loads");
        SetUpInput();
        string[] names = SessionState.GetString(NamesKey, string.Empty).Split('|');
        int seed = SessionState.GetInt(SeedKey, 0);
        Check(RunManager.Instance.World.runSeed == seed && RunManager.Instance.World.day == 1, $"a new run on seed {seed}, day 1 (got seed {RunManager.Instance.World.runSeed}, day {RunManager.Instance.World.day})");

        yield return Day1(names);
        yield return Day2(names[3]);
        yield return Day3();
    }

    private static IEnumerator Day1(string[] names)
    {
        var s = new Booth();

        // 5.1 The briefing: the booth takes no clicks; no day-1 note.
        yield return Until(() => s.Briefing.activeInHierarchy, 20f, "the morning briefing");
        yield return Frames(2);
        Check(!s.Crt.Interactable && !s.Power.Interactable && s.Props.All(p => !p.Interactable), "5.1 during the briefing the CRT, the power button and every prop are inert");
        Check(!s.ScanHint.activeSelf && !s.WheelHint.activeSelf, "5.1 neither day-1 note shows during the briefing");
        ClickUi(s.StartShift, "5.1 START SHIFT");

        // 5.2 READY armed.
        yield return Until(() => s.Ready.Interactable && s.View.IsSettled, 20f, "READY arms");
        Check(!s.Figure.enabled && s.Idle.activeInHierarchy && s.Papers().Count == 0, "5.2 before READY the traveller is hidden, the idle line shows and no paper exists");
        yield return Shot("p7_booth_idle_1080.png");

        // 5.3 READY: the traveller, the passport sliding in (inert until it lands), the notes, the hub.
        ClickWorld(s.Ready, "5.3 READY");
        DeskDocument passport = s.Paper(0);
        Check(passport != null && passport.IsSliding && !passport.GetComponent<DeskDraggable>().enabled && !passport.GetComponent<Clickable>().Interactable,
              "5.3 the passport slides in and is not live until it lands");
        yield return Until(() => !passport.IsSliding, 3f, "the passport lands");
        yield return Frames(2);
        Check(s.CurrentName() == names[0], $"5.3 slot 1 is the precomputed traveller '{names[0]}' (got '{s.CurrentName()}')");
        Check(s.Figure.enabled && Near(passport.transform.position, s.Slot(0)) && s.Papers().Count == 1 && s.Live(passport),
              "5.3 the traveller shows; the passport lies live at slot 0; the permit is absent");
        Check(s.ScanHint.activeSelf && s.WheelHint.activeSelf, "5.3 both day-1 notes show (scan and wheel)");
        Check(s.Labels().SequenceEqual(new[] { "Request Transit Permit", "Ask about home >" }), $"5.3 the hub is [{string.Join(", ", s.Labels())}]");
        s.Hover(passport);
        yield return Shot("p7_booth_papers_1080.png");

        // 5.4 The wheel: the first click opens it; the ask menu in place; a reply in the bubble, kept through "< Back"; the ways to close it.
        ClickWorld(s.TravellerZone, "5.4 the traveller");
        Check(s.Wheel.IsOpen, "5.4 the first click on the traveller opens the wheel");
        yield return Frames(3);
        Check(!s.WheelHint.activeSelf, "5.4 the wheel note hides once the wheel has opened");
        yield return Shot("p7_wheel_hub_1080.png");
        ClickUi(s.Choice("Ask about home >"), "5.4 Ask about home >");
        yield return Frames(3);
        Button back = s.Choice("< Back");
        Check(back.transform.parent == s.Centre, "5.4 '< Back' sits in the wheel's centre");
        Check(s.RingItems().Count == s.Labels().Count - 1 && s.RingItems().All(b => s.Spawned().Contains(b.gameObject)),
              $"5.4 the ring holds exactly the new choices ({s.RingItems().Count} + the centre; no dying button laid out)");
        yield return Shot("p7_wheel_ask_1080.png");
        int before = s.Transcript().Count;
        ClickUi(s.Choice("Currency"), "5.4 Currency");
        yield return Frames(2);
        string reply = InterviewScript.SpokenSince(s.Transcript(), before);
        Check(s.BubbleShown && s.BubbleText == reply && reply.Length > 0, $"5.4 the bubble shows the answer '{s.BubbleText}'");
        Check(s.TranscriptWindow.gameObject.activeInHierarchy, "5.4 the transcript opens on the live monitor");
        yield return Shot("p7_bubble_1080.png");
        ClickUi(s.Choice("< Back"), "5.4 < Back after the reply");
        yield return Frames(2);
        Check(s.BubbleShown && s.BubbleText == reply && s.Labels().Contains("Request Transit Permit"),
              $"5.4 '< Back' returns to the hub and leaves the reply up ('{s.BubbleText}')");
        yield return Escape();
        Check(!s.Wheel.IsOpen, "5.4 Escape closes the wheel");
        ClickWorld(s.Intercom, "5.4 the desk intercom");
        Check(s.Wheel.IsOpen, "5.4 the intercom opens the wheel");
        yield return Frames(2);
        ClickAt(new Vector2(40f, 40f), s.Wheel.gameObject, "5.4 a click outside the ring");
        Check(!s.Wheel.IsOpen, "5.4 a click outside the ring closes it");

        // 5.5 Request the permit: the wheel closes, the reply stays, the permit lands at slot 1, the request is gone.
        ClickWorld(s.TravellerZone, "5.5 the traveller");
        yield return Frames(2);
        ClickUi(s.Choice("Request Transit Permit"), "5.5 Request Transit Permit");
        Check(!s.Wheel.IsOpen && s.BubbleShown && s.BubbleText == "Here you are.", $"5.5 the wheel closes and the bubble keeps the reply ('{s.BubbleText}')");
        DeskDocument permit = s.Paper(1);
        yield return Until(() => permit != null && !permit.IsSliding, 3f, "the permit lands");
        yield return Frames(2);
        Check(Near(permit.transform.position, s.Slot(1)) && !s.Labels().Contains("Request Transit Permit"), "5.5 the permit lies at slot 1 and its request left the hub");
        s.Hover(permit);

        // 5.6 Drag-to-scan through the real input stack (a virtual mouse).
        int trayClicks = 0, paperClicks = 0;
        s.TrayClick.onClick.AddListener(() => trayClicks++);
        passport.GetComponent<Clickable>().onClick.AddListener(() => paperClicks++);
        Vector2 from = ScreenOf(passport.GetComponent<Collider2D>());
        Check(TopHandler<IDragHandler>(from) == passport.gameObject, "5.6 the press resolves to the passport");
        Vector3 pickUp = passport.transform.position;
        yield return MouseDown(from);
        yield return MouseMove(from + new Vector2(24f, 0f));
        Check(s.Order(passport) == s.Config.heldPaperOrder && !passport.GetComponent<Collider2D>().enabled, "5.6 past 10 px the drag starts: the passport is held (order 60) and its collider is off");
        yield return MouseMove(ScreenOf(s.Tray));
        yield return MouseUp(ScreenOf(s.Tray));
        Check(s.ScannerBusy(), "5.6 the release over the tray starts the scan");
        float scanStartMinute = s.Clock.CurrentMinute, scanStartReal = Time.realtimeSinceStartup;

        // While it scans: the permit dropped on the busy scanner slides back, and is not live while it does.
        Vector3 permitPickUp = permit.transform.position;
        DragTo(permit, s.Tray.transform.position, "5.6 the permit onto the busy scanner");
        Check(permit.IsSliding && !s.Live(permit), "5.6 the refused permit slides back, inert");
        Vector2 onPermit = ScreenOf(permit.GetComponent<Collider2D>());
        Check(PointerAt(onPermit).pointerCurrentRaycast.gameObject == permit.gameObject && TopHandler<IBeginDragHandler>(onPermit) != permit.gameObject,
              "5.6 a press on the sliding permit hits it but reaches no drag handler (its DeskDraggable is off)");
        yield return Frames(3);
        yield return Shot("p7_scanning_1080.png");
        yield return Until(() => !s.ScannerBusy(), 4f, "the scan finishes");
        float realSeconds = Time.realtimeSinceStartup - scanStartReal;
        float minutes = s.Clock.CurrentMinute - scanStartMinute;
        float rate = (s.Clock.EndMinute - s.Clock.StartMinute) / s.Clock.RealSecondsPerShift;
        Check(realSeconds >= s.Config.scanSeconds - 0.05f && minutes >= 0.9f * s.Config.scanSeconds * rate,
              $"5.6 the scan took {realSeconds:0.00} s while the clock ran {minutes:0.00} min (at least {s.Config.scanSeconds} s)");
        yield return Until(() => !passport.IsSliding && !permit.IsSliding, 3f, "the papers settle");
        Check(Near(passport.transform.position, pickUp) && Near(permit.transform.position, permitPickUp), "5.6 both papers are back where they were picked up");
        Check(s.DocWindow(0).activeInHierarchy && s.FirstIconLabel() == "Travel Passport", $"5.6 the passport's window is open and its icon is first in the grid ('{s.FirstIconLabel()}')");
        Check(!s.ScanHint.activeSelf && s.View.Current == OfficeView.OfficeFocus, "5.6 the scan note hides and the camera did not move");
        Check(trayClicks == 0 && paperClicks == 0, $"5.6 the virtual-mouse drag clicked neither the tray ({trayClicks}) nor the paper ({paperClicks})");
        DragTo(permit, s.Tray.transform.position, "5.6 the permit onto the idle scanner");
        yield return Until(() => !s.ScannerBusy() && !permit.IsSliding, 5f, "the permit's scan");
        Check(s.DocWindow(1).activeInHierarchy, "5.6 the permit's window opens");

        // 5.7 Papers stay where dropped, clamped to the desk; a plain click brings one forward; a held paper.
        Vector3 spot = s.Surface.PointAt(new Vector2(0.3f, 0.5f));
        DragTo(permit, spot, "5.7 the permit elsewhere on the desk");
        Check(Near(permit.transform.position, spot), "5.7 the permit stays where it was dropped");
        DragTo(permit, new Vector3(-30f, -30f, 0f), "5.7 the permit off the desk");
        Check(s.OnDesk(permit.transform.position), $"5.7 its centre is clamped to the desk ({permit.transform.position})");
        Vector2 click = ScreenOf(passport.GetComponent<Collider2D>());
        yield return MouseDown(click);
        yield return MouseMove(click + new Vector2(3f, 0f));
        yield return MouseUp(click + new Vector2(3f, 0f));
        Check(paperClicks == 1 && s.Order(passport) > s.Order(permit), $"5.7 a plain click under 10 px brings the passport forward (orders {s.Order(passport)} over {s.Order(permit)})");
        BeginDrag(permit);
        Check(s.Order(permit) == s.Config.heldPaperOrder && !permit.GetComponent<Collider2D>().enabled, "5.7 during a drag the paper's order is 60 and its collider is off");
        EndDrag(permit, permit.transform.position);

        // 5.8 Props and tooltips.
        yield return Props(s);

        // 5.9 Focus: input once settled; 1080p and 720p; the proof (rows through the raycast); the window clamp; Deny.
        ClickWorld(s.Crt, "5.9 the CRT");
        Check(!s.DesktopRaycaster.enabled, "5.9 the desktop takes no input while the camera pushes in");
        yield return Until(() => s.View.IsSettled, 5f, "the push-in settles");
        yield return Frames(2);
        Check(s.DesktopRaycaster.enabled, "5.9 the desktop takes input once settled");
        yield return Shot("p7_focus_1080.png");
        yield return Resolution(1280, 720);
        yield return Shot("p7_focus_720.png");
        yield return Resolution(1920, 1080);
        CaseInstance liar = s.Case();
        DocumentField tell = PassportTell(liar);
        yield return ClickDocumentRow(s, 0, tell.label);
        ClickBookRow(tell.category, liar.originLabel);
        Check(s.Ui.EvidenceCount == 1 && s.CompareText().Contains("DEVIATION LOGGED"), $"5.9 the passport's {tell.label} against its book row logs the deviation ({s.CompareText()})");
        yield return WindowClamp(s);
        ClickUi(s.Deny, "5.9 DENY");
        Check(!s.Figure.enabled && !s.Wheel.IsOpen && !s.BubbleShown && s.Field("_currentCase", s.Ui) == null, "5.9 the traveller, the wheel and the bubble are cleared; no case is on the desk");
        yield return Until(() => s.Papers().Count == 0, 3f, "the papers slide back and are destroyed");

        // 5.10 Screen power and the exits (slot 2).
        yield return Until(() => s.Ready.Interactable && s.View.Current == OfficeView.OfficeFocus && s.View.IsSettled, 10f, "back in the booth, READY armed");
        ClickWorld(s.Power, "5.10 the power button");
        Check(!s.Screen.IsOn && !s.DesktopCanvas.enabled && s.Led.color == s.Config.ledOffColor, "5.10 the power button darkens the screen and the LED");
        yield return Shot("p7_screen_off_booth_1080.png");
        ClickWorld(s.Ready, "5.10 READY");
        Check(s.Screen.IsOn, "5.10 presenting a traveller wakes the screen");
        yield return Until(() => s.Paper(0) != null && !s.Paper(0).IsSliding, 3f, "slot 2's passport lands");
        Check(s.CurrentName() == names[1], $"5.10 slot 2 is the precomputed traveller '{names[1]}'");
        s.Hover(s.Paper(0));
        yield return Focus(s);
        ClickUi(s.StartButton, "5.10 Start");
        ClickUi(s.ScreenOffEntry, "5.10 Start > Turn off screen");
        Check(!s.Screen.IsOn && !s.DesktopRaycaster.enabled && !s.DesktopCanvas.enabled, "5.10 Turn off screen darkens it and the desktop takes no input");
        ClickWorld(s.GlassZone, "5.10 the dark glass");
        Check(s.View.Current == OfficeView.MonitorFocus, "5.10 a click on the dark glass keeps the focus");
        yield return Shot("p7_screen_off_focus_1080.png");
        ClickWorld(s.Power, "5.10 the power button, in frame");
        Check(s.Screen.IsOn, "5.10 the power button turns the screen back on");
        ClickAt(Camera.main.WorldToScreenPoint(s.Crt.transform.TransformPoint(new Vector3(-0.6f, 0f, 0f))), s.FocusExit.gameObject, "5.10 the bezel outside the glass");
        Check(s.View.Current == OfficeView.OfficeFocus, "5.10 a click outside the screen leaves");
        yield return Focus(s);
        ClickUi(s.DeskButton, "5.10 < Desk");
        Check(s.View.Current == OfficeView.OfficeFocus, "5.10 '< Desk' leaves");
        yield return Focus(s);
        s.RecordsChrome.Open();
        EventSystem.current.SetSelectedGameObject(s.RecordsSearch.gameObject);
        yield return Frames(2);
        Check(EventSystem.current.currentSelectedGameObject == s.RecordsSearch.gameObject, "5.10 the Records search field has the keyboard");
        yield return Escape();
        Check(s.View.Current == OfficeView.OfficeFocus && EventSystem.current.currentSelectedGameObject == null, "5.10 Escape leaves and the search field loses the keyboard");
        yield return Until(() => s.View.IsSettled, 5f, "back in the booth");

        // 5.11 A dark screen wakes when a scan finishes.
        ClickWorld(s.Power, "5.11 the power button");
        Check(!s.Screen.IsOn, "5.11 the screen is off");
        DragTo(s.Paper(0), s.Tray.transform.position, "5.11 the passport onto the scanner");
        yield return Until(() => !s.ScannerBusy(), 4f, "the scan finishes");
        Check(s.Screen.IsOn && s.DocWindow(0).activeInHierarchy, "5.11 the finished scan wakes the screen and opens the window");

        // 5.12 The citation hold: deny the honest traveller.
        yield return Focus(s);
        ClickUi(s.Deny, "5.12 DENY (an honest traveller)");
        Check(s.CitationPanel.activeInHierarchy && s.Screen.IsOn, "5.12 a citation slip opens on a lit screen");
        ClickUi(s.StartButton, "5.12 Start");
        ClickUi(s.ScreenOffEntry, "5.12 Start > Turn off screen");
        Check(s.Screen.IsOn, "5.12 Turn off screen does nothing while the slip waits");
        yield return Escape();
        yield return Until(() => s.View.IsSettled, 5f, "back in the booth");
        float pausedAt = s.Clock.CurrentMinute;
        Check(!s.Power.Interactable && s.Screen.IsOn && s.DesktopCanvas.enabled && s.CitationPanel.activeInHierarchy, "5.12 the power button is inert; the slip stays visible on the live monitor");
        yield return Seconds(1f);
        Check(s.Clock.IsPaused && s.Clock.CurrentMinute == pausedAt && !s.Ready.Interactable, "5.12 the clock is paused and READY is not armed");
        yield return Focus(s);
        ClickUi(s.Acknowledge, "5.12 Acknowledge");
        Check(!s.Clock.IsPaused, "5.12 Acknowledge resumes the clock");
        yield return Until(() => s.View.Current == OfficeView.OfficeFocus && s.Ready.Interactable, 10f, "the booth, READY armed");

        // 5.15 The outline cache: at most the last case's dead papers; the next hover purges them.
        Check(s.DeadOutlines() <= 1, $"5.15 after two decided travellers the outline cache holds {s.DeadOutlines()} dead paper(s), at most the last case's one");

        // 5.13 Closing time with the traveller at the desk while a paper scans.
        ClickWorld(s.Ready, "5.13 READY");
        yield return Until(() => s.Paper(0) != null && !s.Paper(0).IsSliding, 3f, "slot 3's passport lands");
        Check(s.CurrentName() == names[2], $"5.13 slot 3 is the precomputed traveller '{names[2]}'");
        s.Hover(s.Paper(0));
        Check(s.DeadOutlines() == 0, "5.15 hovering the next traveller's passport purges the dead outlines");
        yield return Resolution(1280, 720);
        yield return Shot("p7_booth_720.png");
        yield return Resolution(1920, 1080);
        DragTo(s.Paper(0), s.Tray.transform.position, "5.13 the passport onto the scanner");
        Check(s.ScannerBusy(), "5.13 a scan runs");
        s.Clock.Tick(100000f);
        yield return Until(() => !s.ScannerBusy(), 4f, "the scan finishes after closing time");
        Check(s.DocWindow(0).activeInHierarchy && s.Papers().Count == 1, "5.13 after closing time the scan still finishes and opens its window; the paper stays");
        ClickWorld(s.TravellerZone, "5.13 the traveller");
        Check(s.Wheel.IsOpen, "5.13 the wheel still opens until the decision");
        yield return Escape();
        yield return Focus(s);
        ClickUi(s.Deny, "5.13 DENY");
        yield return Frames(2);
        if (s.CitationPanel.activeInHierarchy)
            ClickUi(s.Acknowledge, "5.13 Acknowledge");
        yield return Until(() => s.Results.activeInHierarchy, 10f, "the shift report");
        yield return Until(() => s.Papers().Count == 0, 3f, "the papers leave");
        Check(!s.Figure.enabled, "5.13 the day ends after the decision; papers and traveller are cleared");
        RunManager.Instance.AdvanceToNextDay();
    }

    private static IEnumerator Day2(string capitalLiar)
    {
        yield return Until(() => RunManager.Instance.World.day == 2 && Object.FindFirstObjectByType<GameManager>() != null && Briefing() != null && Briefing().activeInHierarchy, 30f, "day 2's briefing");
        var s = new Booth();
        yield return Frames(2);

        // 5.14 No day-1 note on day 2; the spoken capital proven from the transcript.
        ClickUi(s.StartShift, "5.14 START SHIFT");
        yield return Until(() => s.Ready.Interactable && s.View.IsSettled, 20f, "READY arms");
        ClickWorld(s.Ready, "5.14 READY");
        yield return Until(() => s.Paper(0) != null && !s.Paper(0).IsSliding, 3f, "the passport lands");
        Check(!s.ScanHint.activeSelf && !s.WheelHint.activeSelf, "5.14 neither day-1 note shows on day 2");
        Check(s.CurrentName() == capitalLiar, $"5.14 day 2's slot 1 is the precomputed spoken-capital liar '{capitalLiar}' (got '{s.CurrentName()}')");
        ClickWorld(s.TravellerZone, "5.14 the traveller");
        yield return Frames(2);
        ClickUi(s.Choice("Ask about home >"), "5.14 Ask about home >");
        yield return Frames(2);
        int before = s.Transcript().Count;
        ClickUi(s.Choice("Capital"), "5.14 Capital");
        yield return Frames(2);
        CaseInstance liar = s.Case();
        InterviewAnswer said = liar.answers.First(a => a.category == ClueCategory.Geography);
        Check(s.BubbleShown && s.BubbleText == InterviewScript.SpokenSince(s.Transcript(), before) && s.BubbleText.Contains(said.value),
              $"5.14 the bubble says the capital ('{s.BubbleText}')");
        yield return Escape();
        yield return Focus(s);
        yield return ClickTranscriptRow(s, "Line_q_capital.answer");
        Check(s.CompareText().Contains("Traveller · CAPITAL"), $"5.14 the answer row reads 'Traveller · CAPITAL' in the compare bar ({s.CompareText()})");
        ClickBookRow(ClueCategory.Geography, liar.originLabel);
        Check(s.Ui.EvidenceCount == 1 && s.CompareText().Contains("DEVIATION LOGGED"), $"5.14 against the Capitals Gazetteer's claim row it is logged ({s.CompareText()})");
        ClickUi(s.Deny, "5.14 DENY");
        yield return Frames(2);
        CaseVerdict verdict = s.Game.Ledger.verdicts.Last();
        Check(verdict.correct && !verdict.citationIssued, "5.14 the denial is correct, with no citation");

        // 5.13 (second half) Closing time with READY armed: nothing is handed over; the ledger shows.
        yield return Until(() => s.Ready.Interactable && s.View.IsSettled, 10f, "READY arms for slot 2");
        s.Clock.Tick(100000f);
        yield return Until(() => s.Results.activeInHierarchy, 10f, "the shift report");
        Check(s.Papers().Count == 0 && !s.Ready.Interactable && !s.Figure.enabled, "5.13 closing behind READY hands nothing over; the ledger shows");
        RunManager.Instance.AdvanceToNextDay();
    }

    private static IEnumerator Day3()
    {
        yield return Until(() => RunManager.Instance.World.day == 3 && Object.FindFirstObjectByType<GameManager>() != null && Briefing() != null && Briefing().activeInHierarchy, 30f, "day 3's briefing");
        var s = new Booth();
        yield return Frames(2);

        // 5.14 Day 3 played to closing: one traveller accepted, then closing time.
        ClickUi(s.StartShift, "5.14 day 3 START SHIFT");
        yield return Until(() => s.Ready.Interactable && s.View.IsSettled, 20f, "READY arms");
        ClickWorld(s.Ready, "5.14 day 3 READY");
        yield return Until(() => s.Paper(0) != null && !s.Paper(0).IsSliding, 3f, "the passport lands");
        yield return Focus(s);
        ClickUi(s.Accept, "5.14 day 3 ACCEPT");
        yield return Frames(2);
        if (s.CitationPanel.activeInHierarchy)
            ClickUi(s.Acknowledge, "5.14 day 3 Acknowledge");
        yield return Until(() => s.Ready.Interactable && s.View.IsSettled, 10f, "READY arms for slot 2");
        s.Clock.Tick(100000f);
        yield return Until(() => s.Results.activeInHierarchy, 10f, "day 3's shift report");
        Check(true, "5.14 day 3 played to closing");
    }

    /// <summary>5.8: each prop reacts (and returns to rest); the four readouts show as tooltips.</summary>
    private static IEnumerator Props(Booth s)
    {
        foreach (Clickable prop in s.Props)
        {
            var reaction = prop.GetComponent<DeskReaction>();
            var so = (DeskReactionSO)s.Field("reaction", reaction);
            var readout = (TMP_Text)s.Field("readout", reaction);
            Transform t = prop.transform;
            Vector3 position = t.localPosition, scale = t.localScale;
            Quaternion rotation = t.localRotation;

            ClickWorld(prop, $"5.8 {prop.name}");
            yield return Frames(3);
            if (so.kind != ReactionKind.None)
                Check(t.localPosition != position || t.localScale != scale || t.localRotation != rotation, $"5.8 {prop.name} animates ({so.kind})");
            if (!string.IsNullOrWhiteSpace(so.tooltip))
            {
                string expected = Interview.Fill(so.tooltip, Interview.ValueToken, readout != null ? readout.text : string.Empty);
                Check(s.TooltipShown && s.TooltipText == expected, $"5.8 {prop.name} shows '{s.TooltipText}' (expected '{expected}')");
            }
            if (prop.name == "CalendarZone")
                Check(s.TooltipText == "Day 01", $"5.8 the calendar reads '{s.TooltipText}'");
            if (prop.name == "CreditsTill")
            {
                Check(s.TooltipText.StartsWith("Credits: "), $"5.8 the till reads '{s.TooltipText}'");
                yield return Shot("p7_tooltip_1080.png");
            }
            if (prop.name == "StabilityMonitor")
                Check(s.TooltipText.StartsWith("Timeline stability: ") && s.TooltipText.EndsWith("%"), $"5.8 the stability monitor reads '{s.TooltipText}'");
            if (prop.name == "DeskIntercom")
            {
                Check(s.Wheel.IsOpen, "5.8 the intercom opened the wheel");
                yield return Escape();
            }

            yield return Seconds(0.6f);
            Check(t.localPosition == position && t.localScale == scale && t.localRotation == rotation, $"5.8 {prop.name} is back at its rest pose");
        }
    }

    /// <summary>5.9: a document window dragged far off the screen stays inside it (and is clipped by the screen's mask).</summary>
    private static IEnumerator WindowClamp(Booth s)
    {
        GameObject window = s.DocWindow(0);
        window.GetComponent<OSWindowChrome>().Open();
        yield return Frames(2);
        DraggableWindow header = window.GetComponentInChildren<DraggableWindow>();
        Vector2 at = ScreenOf(header.GetComponent<RectTransform>());
        GameObject handler = TopHandler<IDragHandler>(at);
        Check(handler == header.gameObject, $"5.9 the press on the window header resolves to it (got {(handler != null ? handler.name : "nothing")})");
        PointerEventData data = PointerAt(at);
        ExecuteEvents.Execute(header.gameObject, data, ExecuteEvents.beginDragHandler);
        data.position = at + new Vector2(2000f, -2000f);
        ExecuteEvents.Execute(header.gameObject, data, ExecuteEvents.dragHandler);
        ExecuteEvents.Execute(header.gameObject, data, ExecuteEvents.endDragHandler);
        var rt = (RectTransform)window.transform;
        Rect parent = ((RectTransform)rt.parent).rect;
        Vector3 p = rt.localPosition;
        Check(p.x + rt.rect.xMax <= parent.xMax + 0.5f && p.y + rt.rect.yMin >= parent.yMin - 0.5f && s.DesktopCanvas.GetComponent<RectMask2D>() != null,
              "5.9 a window dragged off the screen is clamped inside it (the screen's mask clips the rest)");
    }

    /// <summary>Focuses the monitor through the CRT's click and waits for the push-in to settle.</summary>
    private static IEnumerator Focus(Booth s)
    {
        ClickWorld(s.Crt, "focus the CRT");
        yield return Until(() => s.View.Current == OfficeView.MonitorFocus && s.View.IsSettled, 5f, "the push-in settles");
        yield return Frames(2);
    }

    // -----------------------------
    // The booth as the play-through sees it (re-found after each scene load)
    // -----------------------------

    private sealed class Booth
    {
        public readonly GameManager Game = Object.FindFirstObjectByType<GameManager>();
        public readonly InvestigationUIController Ui = Object.FindObjectsByType<InvestigationUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        public readonly OfficeViewController View = Object.FindFirstObjectByType<OfficeViewController>();
        public readonly BoothCoordinator Coordinator = Object.FindFirstObjectByType<BoothCoordinator>();
        public readonly MonitorScreen Screen = Object.FindFirstObjectByType<MonitorScreen>();
        public readonly DeskController Desk = Object.FindFirstObjectByType<DeskController>();
        public readonly TravellerWheel Wheel = Object.FindFirstObjectByType<TravellerWheel>();
        public readonly ShiftClock Clock = Object.FindFirstObjectByType<ShiftClockDriver>().Clock;

        public DeskConfigSO Config => (DeskConfigSO)Field("config", Desk);
        public DeskSurface Surface => (DeskSurface)Field("surface", Desk);
        public DeskScanner Tray => (DeskScanner)Field("scanner", Desk);
        public Clickable TrayClick => Tray.GetComponent<Clickable>();
        public GameObject ScanHint => ((TMP_Text)Field("scanHint", Desk)).gameObject;
        public GameObject WheelHint => ((TMP_Text)Field("wheelHint", Coordinator)).gameObject;
        public Clickable Ready => (Clickable)Field("readySign", Game);
        public Clickable Crt => (Clickable)Field("crt", Coordinator);
        public Clickable Power => (Clickable)Field("powerButton", Coordinator);
        public Clickable FocusExit => (Clickable)Field("focusExit", Coordinator);
        public Clickable GlassZone => (Clickable)Field("glassZone", Coordinator);
        public Clickable TravellerZone => (Clickable)Field("travellerHitZone", Coordinator);
        public Clickable[] Props => (Clickable[])Field("props", Coordinator);
        public Clickable Intercom => Props.First(p => p.name == "DeskIntercom");
        public Renderer Figure => ((Renderer[])Field("figure", Object.FindFirstObjectByType<TravellerView>()))[0];
        public GameObject Idle => (GameObject)Field("idleScreen", Ui);
        public Canvas DesktopCanvas => (Canvas)Field("desktopCanvas", Screen);
        public GraphicRaycaster DesktopRaycaster => (GraphicRaycaster)Field("desktopRaycaster", Screen);
        public SpriteRenderer Led => (SpriteRenderer)Field("powerLed", Screen);
        public Transform Centre => (Transform)Field("centreSlot", Field("interactionPanel", Ui));
        public TranscriptWindowController TranscriptWindow => (TranscriptWindowController)Field("transcriptWindow", Ui);
        public GameObject Briefing => (GameObject)Field("briefingPanel", DayFlow());
        public GameObject Results => (GameObject)Field("resultsPanel", DayFlow());
        public Button StartShift => (Button)Field("startShiftButton", DayFlow());
        public Button Deny => (Button)Field("denyButton", Ui);
        public Button Accept => (Button)Field("acceptButton", Ui);
        public GameObject CitationPanel => (GameObject)Field("citationPanel", Object.FindFirstObjectByType<OfficeUIController>());
        public Button Acknowledge => (Button)Field("citationContinueButton", Object.FindFirstObjectByType<OfficeUIController>());
        public Button StartButton => (Button)Field("startButton", Shell());
        public Button ScreenOffEntry => (Button)Field("screenOffButton", Shell());
        public Button DeskButton => DesktopCanvas.transform.Find("Taskbar/DeskButton").GetComponent<Button>();
        public OSWindowChrome RecordsChrome => ((CitizenRecordsWindowController)Field("recordsWindow", Ui)).GetComponent<OSWindowChrome>();
        public TMP_InputField RecordsSearch => (TMP_InputField)Field("searchInput", Field("recordsWindow", Ui));

        private static DayFlowUIController DayFlow() => Object.FindObjectsByType<DayFlowUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        private DesktopShell Shell() => DesktopCanvas.GetComponent<DesktopShell>();

        public object Field(string name, object target) => TimeDeskP7PlaySmoke.Field(target, name);

        public List<DeskDocument> Papers() =>
            ((Transform)Field("paperRoot", Desk)).GetComponentsInChildren<DeskDocument>().ToList();

        public DeskDocument Paper(int index) => Papers().FirstOrDefault(p => p.Index == index);

        public Vector3 Slot(int k) => Surface.PointAt(Config.paperSpawnSlots[k]);

        public bool OnDesk(Vector3 world)
        {
            Vector3 local = Surface.transform.InverseTransformPoint(world);
            Vector2 size = (Vector2)Field("size", Surface);
            return new DeskRect(0f, 0f, size.x + 0.001f, size.y + 0.001f).Contains(local.x, local.y);
        }

        public bool Live(DeskDocument paper) => paper.GetComponent<DeskDraggable>().enabled && paper.GetComponent<Clickable>().Interactable;

        public int Order(DeskDocument paper) => paper.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder;

        public bool ScannerBusy()
        {
            var state = (DeskPapers)Field("_state", Desk);
            return state != null && state.ScannerBusy;
        }

        public CaseInstance Case() => (CaseInstance)Field("_currentCase", Ui);

        public string CurrentName() => Case() != null ? Case().visitorGivenName : "none";

        public IReadOnlyList<DialogLine> Transcript() => ((DialogRunner)Field("_runner", Ui)).Transcript;

        public List<GameObject> Spawned() => (List<GameObject>)Field("_spawned", Field("interactionPanel", Ui));

        public List<string> Labels() => Spawned().Select(b => b.GetComponentInChildren<TMP_Text>(true).text).ToList();

        public Button Choice(string label)
        {
            GameObject go = Spawned().FirstOrDefault(b => b.GetComponentInChildren<TMP_Text>(true).text == label);
            if (go == null)
                throw new InvalidOperationException($"no wheel choice '{label}' in [{string.Join(", ", Labels())}]");
            return go.GetComponent<Button>();
        }

        public List<Button> RingItems()
        {
            var ring = (Transform)Field("ring", Wheel);
            return ring.Cast<Transform>().Where(t => t.gameObject.activeSelf && t != Centre).Select(t => t.GetComponent<Button>()).Where(b => b != null).ToList();
        }

        private OverlayCallout Bubble => (OverlayCallout)Field("bubble", Wheel);
        private OverlayCallout Tooltip => (OverlayCallout)Field("tooltip", Props[0].GetComponent<DeskReaction>());
        public bool BubbleShown => ((RectTransform)Field("panel", Bubble)).gameObject.activeSelf;
        public string BubbleText => ((TMP_Text)Field("label", Bubble)).text;
        public bool TooltipShown => ((RectTransform)Field("panel", Tooltip)).gameObject.activeSelf;
        public string TooltipText => ((TMP_Text)Field("label", Tooltip)).text;

        public GameObject DocWindow(int index) => ((List<GameObject>)Field("_docWindows", Ui))[index];

        public string FirstIconLabel()
        {
            var shelf = (Transform)Field("bookShelfRoot", Ui);
            Transform first = shelf.Cast<Transform>().First(t => t.gameObject.activeSelf);
            return first.GetComponentInChildren<TMP_Text>(true).text;
        }

        public string CompareText() => ((TMP_Text)Field("compareText", Field("compareController", Ui))).text;

        /// <summary>A hover over a paper, as HoverHighlighter does it (its outline is generated), then off.</summary>
        public void Hover(DeskDocument paper)
        {
            HoverHighlighter highlighter = Object.FindFirstObjectByType<HoverHighlighter>();
            MethodInfo set = typeof(HoverHighlighter).GetMethod("SetHighlighted", BindingFlags.NonPublic | BindingFlags.Instance);
            set.Invoke(highlighter, new object[] { paper.GetComponent<Clickable>(), true });
            set.Invoke(highlighter, new object[] { paper.GetComponent<Clickable>(), false });
        }

        /// <summary>Outline cache entries whose renderer was destroyed.</summary>
        public int DeadOutlines()
        {
            var cache = (System.Collections.IDictionary)Field("_worldOutlines", Object.FindFirstObjectByType<HoverHighlighter>());
            return cache.Keys.Cast<Object>().Count(k => k == null);
        }
    }

    private static GameObject Briefing()
    {
        DayFlowUIController flow = Object.FindObjectsByType<DayFlowUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        return flow != null ? (GameObject)Field(flow, "briefingPanel") : null;
    }

    // -----------------------------
    // Input: the real raycast, handler-level events, a virtual mouse and keyboard
    // -----------------------------

    /// <summary>A screen point on a component: a collider's centre through the main camera, or a rect's centre on its canvas.</summary>
    private static Vector2 ScreenOf(Component c)
    {
        if (c.transform is RectTransform rt)
        {
            Canvas canvas = rt.GetComponentInParent<Canvas>().rootCanvas;
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return RectTransformUtility.WorldToScreenPoint(cam, rt.TransformPoint(rt.rect.center));
        }

        Collider2D col = c.GetComponent<Collider2D>();
        Vector3 world = col != null ? col.bounds.center : c.transform.position;
        return Camera.main.WorldToScreenPoint(world);
    }

    private static PointerEventData PointerAt(Vector2 screen)
    {
        var results = new List<RaycastResult>();
        var data = new PointerEventData(EventSystem.current) { position = screen, button = PointerEventData.InputButton.Left };
        EventSystem.current.RaycastAll(data, results);
        if (results.Count == 0)
            throw new InvalidOperationException($"nothing under {screen}");
        data.pointerCurrentRaycast = results[0];
        data.pointerPressRaycast = results[0];
        data.pressPosition = screen;
        data.rawPointerPress = results[0].gameObject;
        data.eligibleForClick = true;
        data.clickCount = 1;
        return data;
    }

    /// <summary>The object that would handle <typeparamref name="T"/> for the top raycast hit at a screen point (the real sorting).</summary>
    private static GameObject TopHandler<T>(Vector2 screen) where T : IEventSystemHandler =>
        ExecuteEvents.GetEventHandler<T>(PointerAt(screen).pointerCurrentRaycast.gameObject);

    /// <summary>A click at a screen point, after checking that the top hit resolves to the expected handler.</summary>
    private static void ClickAt(Vector2 screen, GameObject expected, string what)
    {
        PointerEventData data = PointerAt(screen);
        GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(data.pointerCurrentRaycast.gameObject);
        if (handler != expected)
            throw new InvalidOperationException($"{what}: the top hit is {data.pointerCurrentRaycast.gameObject.name} (handler {(handler != null ? handler.name : "none")}), not {expected.name}");
        data.pointerPress = handler;
        ExecuteEvents.Execute(handler, data, ExecuteEvents.pointerClickHandler);
    }

    private static void ClickWorld(Component target, string what) => ClickAt(ScreenOf(target), target.gameObject, what);

    private static void ClickUi(Button button, string what) => ClickAt(ScreenOf(button), button.gameObject, what);

    /// <summary>Handler-level drag of a paper, pressed through the real raycast, to a world point on the desk.</summary>
    private static void DragTo(DeskDocument paper, Vector3 world, string what)
    {
        PointerEventData data = BeginDrag(paper);
        data.position = Camera.main.WorldToScreenPoint(world);
        ExecuteEvents.Execute(paper.gameObject, data, ExecuteEvents.dragHandler);
        ExecuteEvents.Execute(paper.gameObject, data, ExecuteEvents.endDragHandler);
    }

    private static PointerEventData BeginDrag(DeskDocument paper)
    {
        Vector2 at = ScreenOf(paper.GetComponent<Collider2D>());
        PointerEventData data = PointerAt(at);
        GameObject handler = ExecuteEvents.GetEventHandler<IDragHandler>(data.pointerCurrentRaycast.gameObject);
        if (handler != paper.gameObject)
            throw new InvalidOperationException($"the press at paper {paper.Index} resolves to {(handler != null ? handler.name : "nothing")}");
        ExecuteEvents.Execute(paper.gameObject, data, ExecuteEvents.beginDragHandler);
        _held = data;
        return data;
    }

    private static PointerEventData _held;

    private static void EndDrag(DeskDocument paper, Vector3 world)
    {
        _held.position = Camera.main.WorldToScreenPoint(world);
        ExecuteEvents.Execute(paper.gameObject, _held, ExecuteEvents.endDragHandler);
    }

    private static void SetUpInput()
    {
        _inputBehaviour = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        _mouse = InputSystem.AddDevice<Mouse>("P7Mouse");
        _keyboard = InputSystem.AddDevice<Keyboard>("P7Keyboard");
    }

    private static void RestoreInput()
    {
        if (_mouse != null)
            InputSystem.RemoveDevice(_mouse);
        if (_keyboard != null)
            InputSystem.RemoveDevice(_keyboard);
        _mouse = null;
        _keyboard = null;
        InputSystem.settings.editorInputBehaviorInPlayMode = _inputBehaviour;
    }

    private static IEnumerator MouseDown(Vector2 at)
    {
        InputSystem.QueueStateEvent(_mouse, new MouseState { position = at });
        yield return Frames(2);
        InputSystem.QueueStateEvent(_mouse, new MouseState { position = at }.WithButton(MouseButton.Left));
        yield return Frames(2);
    }

    private static IEnumerator MouseMove(Vector2 to)
    {
        Vector2 from = _mouse.position.ReadValue();
        for (int i = 1; i <= 6; i++)
        {
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = Vector2.Lerp(from, to, i / 6f) }.WithButton(MouseButton.Left));
            yield return Frames(1);
        }
        yield return Frames(1);
    }

    private static IEnumerator MouseUp(Vector2 at)
    {
        InputSystem.QueueStateEvent(_mouse, new MouseState { position = at });
        yield return Frames(3);
    }

    private static IEnumerator Escape()
    {
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape));
        yield return Frames(2);
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
        yield return Frames(2);
    }

    // -----------------------------
    // Desktop rows: a document's field row and a transcript row are pressed
    // through the real raycast on the focused, settled desktop (so neither the
    // glass zone nor the exit zone may take a click meant for a row); book rows
    // sit on pages a book shows only while searched, so they are invoked
    // directly, like the piece-3 play-through.
    // -----------------------------

    private static IEnumerator ClickDocumentRow(Booth s, int documentIndex, string label)
    {
        GameObject window = s.DocWindow(documentIndex);
        window.GetComponent<OSWindowChrome>().Open(); // raised above the other windows, as its icon would
        yield return Frames(2);
        foreach (GameObject row in (List<GameObject>)Field(window.GetComponent<DocumentWindowController>(), "_rows"))
        {
            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0 && texts[0].text == label)
            {
                ClickAt(ScreenOf(row.transform), row, $"the '{label}' row of document {documentIndex}");
                yield break;
            }
        }
        throw new InvalidOperationException($"no row '{label}' in document {documentIndex}");
    }

    private static void ClickBookRow(ClueCategory category, string originLabel)
    {
        foreach (ReferenceBookWindowController w in Object.FindObjectsByType<ReferenceBookWindowController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!(Field(w, "_book") is ReferenceBookSO book) || book.category != category)
                continue;

            for (int page = 0; page < 10; page++)
            {
                w.ShowPage(page);
                GameObject row = ((List<GameObject>)Field(w, "_rows")).FirstOrDefault(r => r.GetComponentsInChildren<TMP_Text>(true)[0].text == originLabel);
                if (row != null)
                {
                    row.GetComponent<Button>().onClick.Invoke();
                    return;
                }
            }
        }
        throw new InvalidOperationException($"no {category} book row '{originLabel}'");
    }

    private static IEnumerator ClickTranscriptRow(Booth s, string rowName)
    {
        TranscriptWindowController transcript = s.TranscriptWindow;
        ((OSWindowChrome)s.Field("transcriptChrome", s.Ui)).Open(); // raised above the other windows
        for (int page = 0; page < 20; page++)
        {
            yield return Frames(2); // a new page's rows are laid out
            GameObject row = ((List<GameObject>)Field(transcript, "_rows")).FirstOrDefault(r => r.name == rowName);
            if (row != null)
            {
                ClickAt(ScreenOf(row.transform), row, $"the transcript row {rowName}");
                yield break;
            }
            transcript.ShowPage(page);
        }
        throw new InvalidOperationException($"no transcript row '{rowName}'");
    }

    // -----------------------------
    // Waiting, screenshots, the game view
    // -----------------------------

    private static IEnumerator Frames(int n)
    {
        int target = Time.frameCount + n;
        while (Time.frameCount < target)
            yield return null;
    }

    private static IEnumerator Seconds(float seconds)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end)
            yield return null;
    }

    private static IEnumerator Until(Func<bool> condition, float timeout, string what)
    {
        float end = Time.realtimeSinceStartup + timeout;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > end)
                throw new TimeoutException($"waited {timeout} s for {what}");
            yield return null;
        }
    }

    private static IEnumerator Shot(string file)
    {
        string path = Path.Combine(Scratch, file);
        if (File.Exists(path))
            File.Delete(path);
        yield return Frames(2);
        ScreenCapture.CaptureScreenshot(path);
        yield return Until(() => File.Exists(path), 10f, "the screenshot " + file);
        Report($"screenshot {file} (screen {UnityEngine.Screen.width}x{UnityEngine.Screen.height})");
    }

    private static IEnumerator Resolution(int width, int height)
    {
        PlayModeWindow.SetCustomRenderingResolution((uint)width, (uint)height, ViewName);
        yield return Until(() => UnityEngine.Screen.width == width && UnityEngine.Screen.height == height, 10f, $"a {width}x{height} game view");
        yield return Frames(5);
    }

    private static bool Near(Vector3 a, Vector3 b) => Vector2.Distance(a, b) < 0.02f;

    // -----------------------------
    // Plumbing
    // -----------------------------

    private static void Finish()
    {
        PlayModeWindow.SetCustomRenderingResolution((uint)SessionState.GetInt(ViewWidthKey, 1920), (uint)SessionState.GetInt(ViewHeightKey, 1080), ViewName);

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
        Check(log.Length == 0, log.Length == 0 ? "5.16 warnings/errors: none" : "5.16 warnings/errors:\n" + log);
        int fails = SessionState.GetInt(FailsKey, 0);
        Report($"fails={fails}");

        SessionState.SetInt(StepKey, 0);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        EditorApplication.Exit(fails == 0 ? 0 : 6);
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Log)
            SessionState.SetString(LogKey, SessionState.GetString(LogKey, string.Empty) + $"[{type}] {message}\n");
    }

    /// <summary>A field of the object's type or any base type.</summary>
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
Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe' -ArgumentList @('-projectPath','E:\unity\NOPE-feat-clock','-executeMethod','TimeDeskP7PlaySmoke.Run','-logFile','C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\unity_p7_playsmoke.log') -PassThru -Wait
```
Expected: exit code 0; `SCRATCH/p7_playsmoke_report.txt` holds `seed=… day-1 slots 1-3 and day-2 slot 1: …` and `save path=… hadSave=…`, then only `PASS` lines (the new run's seed and day, then steps 5.1–5.16 in play order: day 1's 5.1–5.13 and 5.15, day 2's 5.14 and the second half of 5.13, day 3's 5.14, and `PASS 5.16 warnings/errors: none`), ten `screenshot p7_… (screen 1920x1080)` lines and two `(screen 1280x720)` lines (`p7_focus_720.png`, `p7_booth_720.png`), and `fails=0`. `SCRATCH/nope_save_backup_p7.json` must not remain.

**Recovery if Unity died before `Finish()`** (tool timeout, crash, a hang in play mode): the player's real save may still be in the scratchpad. The save path is shared with the main `E:\unity\NOPE` project (same company and product name), so restore it before anything else (PowerShell):

```powershell
$s = 'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
$m = Select-String -Path "$s\p7_playsmoke_report.txt" -Pattern '^save path=(.+) hadSave=(True|False)$' | Select-Object -First 1
$path = $m.Matches[0].Groups[1].Value; $hadSave = $m.Matches[0].Groups[2].Value -eq 'True'
if (Test-Path "$s\nope_save_backup_p7.json") { Copy-Item "$s\nope_save_backup_p7.json" $path -Force; Remove-Item "$s\nope_save_backup_p7.json" }
elseif (-not $hadSave -and (Test-Path $path)) { Remove-Item $path }
```

Then revert `Assets/Resources/RunConfig.asset` and `ProjectSettings` (Step 11's checkout), fix the cause and re-run Step 6. (The game view keeps the custom "TimeDeskP7" size if Unity died; it is a per-user editor setting and harmless.)

- [ ] **Step 7: Write session C** (`Assets/Editor/_TimeDeskP7Scenes.cs`, Write tool)

```csharp
// TEMPORARY play-mode verification for piece 7 (the physical desk); never
// committed. The scenes without the new pieces (spec section 6 step 6):
// Test_DayLoop plays one case through its era buttons; OfficeScene_HybridArt
// (Codex's scene, opened and played, never saved) plays one full case on the
// no-desk path: the passport window opens with its icon at presentation, the
// permit is requested once from the intercom list, a field is compared, and
// the traveller is denied. SessionState carries the steps across the play-mode
// domain reloads.
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
public static class TimeDeskP7Scenes
{
    private const string Scratch = @"C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad";
    private static readonly string ReportPath = Path.Combine(Scratch, "p7_scenes_report.txt");
    private static readonly string SaveBackup = Path.Combine(Scratch, "nope_save_backup_p7_scenes.json");

    private const string StepKey = "P7Scenes.Step";
    private const string TimeKey = "P7Scenes.Time";
    private const string FailsKey = "P7Scenes.Fails";
    private const string LogKey = "P7Scenes.Log";
    private const string HadSaveKey = "P7Scenes.HadSave";

    static TimeDeskP7Scenes()
    {
        if (SessionState.GetInt(StepKey, 0) == 0)
            return;
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
    }

    public static void Run()
    {
        File.WriteAllText(ReportPath, "start\n");
        bool hadSave = File.Exists(SaveSystem.SavePath);
        Report($"save path={SaveSystem.SavePath} hadSave={hadSave}");
        if (hadSave)
        {
            File.Copy(SaveSystem.SavePath, SaveBackup, true);
            File.Delete(SaveSystem.SavePath);
        }
        SessionState.SetInt(HadSaveKey, hadSave ? 1 : 0);
        SessionState.SetInt(FailsKey, 0);
        SessionState.SetString(LogKey, string.Empty);

        EditorSceneManager.OpenScene("Assets/Scenes/Test_DayLoop.unity", OpenSceneMode.Single);
        Next(1);
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        int step = SessionState.GetInt(StepKey, 0);
        try
        {
            if (step == 1 && EditorApplication.isPlaying && Elapsed > 4f) DayLoopCase();
            else if (step == 2 && !EditorApplication.isPlaying && Elapsed > 1f) OpenHybrid();
            else if (step == 3 && EditorApplication.isPlaying && Elapsed > 5f) HybridStart();
            else if (step == 31 && EditorApplication.isPlaying) HybridReady();
            else if (step == 4 && Elapsed > 1f) HybridCase();
            else if (step == 5 && !EditorApplication.isPlaying && Elapsed > 1f) Finish();
        }
        catch (Exception e)
        {
            Check(false, $"exception at step {step}: {e}");
            if (EditorApplication.isPlaying)
            {
                Next(step < 3 ? 2 : 5);
                EditorApplication.ExitPlaymode();
            }
            else
            {
                Finish();
            }
        }
    }

    /// <summary>Test_DayLoop: the legacy era-pick path plays one case (every new GameManager reference is null there).</summary>
    private static void DayLoopCase()
    {
        var office = Object.FindFirstObjectByType<OfficeUIController>();
        var buttons = (List<Button>)Field(office, "_spawnedButtons");
        Check(buttons.Count > 0, $"Test_DayLoop shows a case with {buttons.Count} era buttons");
        buttons[0].onClick.Invoke();
        string log = SessionState.GetString(LogKey, string.Empty);
        Check(!log.Contains("[Error]") && !log.Contains("[Exception]"), "Test_DayLoop: a case played through its era buttons with no error or exception");
        string[] ours = { "DeskController", "Desk scanner", "TravellerWheel", "TravellerView", "MonitorScreen", "BoothCoordinator", "OverlayCallout", "CinemachineBrain" };
        Check(!ours.Any(w => log.Contains(w)), $"Test_DayLoop: no warning names a piece-7 part ({log.Replace('\n', '/')})");
        Report("Test_DayLoop log: " + log.Replace('\n', '/'));
        SessionState.SetString(LogKey, string.Empty);
        Next(2);
        EditorApplication.ExitPlaymode();
    }

    private static void OpenHybrid()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/OfficeScene_HybridArt.unity", OpenSceneMode.Single);
        Next(3);
        EditorApplication.EnterPlaymode();
    }

    /// <summary>OfficeScene_HybridArt: the briefing's START SHIFT (READY waits for the day loop to arm it).</summary>
    private static void HybridStart()
    {
        var flow = Object.FindObjectsByType<DayFlowUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        ((Button)Field(flow, "startShiftButton")).onClick.Invoke();
        Next(31);
    }

    /// <summary>
    /// OfficeScene_HybridArt: READY, once the day loop has armed its gate (the
    /// slot's start may come frames after START SHIFT; a READY pressed before
    /// is a silent no-op). Fails the check after 20 s.
    /// </summary>
    private static void HybridReady()
    {
        var ready = (Clickable)Field(Object.FindFirstObjectByType<GameManager>(), "readySign");
        if (!ready.Interactable)
        {
            if (Elapsed <= 20f)
                return;
            Check(false, "READY arms in the hybrid scene within 20 s of START SHIFT");
            Next(5);
            EditorApplication.ExitPlaymode();
            return;
        }

        Report($"READY armed {Elapsed:0.0} s after START SHIFT");
        ready.onClick.Invoke();
        Next(4);
    }

    /// <summary>OfficeScene_HybridArt: one full case on the no-desk path.</summary>
    private static void HybridCase()
    {
        var ui = Object.FindObjectsByType<InvestigationUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        var inst = (CaseInstance)Field(ui, "_currentCase");
        Check(inst != null, "the hybrid scene presents a traveller after READY");
        var windows = (List<GameObject>)Field(ui, "_docWindows");
        Check(windows[0].activeSelf && FirstIconLabel(ui) == "Travel Passport", $"the passport's window opens at presentation with its icon ('{FirstIconLabel(ui)}')");

        List<Button> intercom = Buttons(ui);
        Check(intercom.Select(Label).SequenceEqual(new[] { "Request Transit Permit" }), $"the intercom lists [{string.Join(", ", intercom.Select(Label))}]");
        intercom[0].onClick.Invoke();
        Check(windows[1].activeSelf && !Buttons(ui).Select(Label).Contains("Request Transit Permit"), "the request opens the permit's window and is one-shot");
        var shelf = (Transform)Field(ui, "bookShelfRoot");
        Check(shelf.Cast<Transform>().Count(t => t.gameObject.activeSelf && t.GetComponentInChildren<TMP_Text>(true).text == "Transit Permit") == 1, "the permit's window got one icon");

        DocumentInstance passport = inst.documents[0];
        DocumentField field = passport.fields.First(f => f.category == ClueCategory.Currency);
        foreach (GameObject row in (List<GameObject>)Field(windows[0].GetComponent<DocumentWindowController>(), "_rows"))
            if (row.GetComponentsInChildren<TMP_Text>(true)[0].text == field.label)
                row.GetComponent<Button>().onClick.Invoke();
        foreach (ReferenceBookWindowController w in Object.FindObjectsByType<ReferenceBookWindowController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!(Field(w, "_book") is ReferenceBookSO book) || book.category != ClueCategory.Currency)
                continue;
            for (int page = 0; page < 10; page++)
            {
                w.ShowPage(page);
                GameObject row = ((List<GameObject>)Field(w, "_rows")).FirstOrDefault(r => r.GetComponentsInChildren<TMP_Text>(true)[0].text == inst.originLabel);
                if (row == null)
                    continue;
                row.GetComponent<Button>().onClick.Invoke();
                break;
            }
        }
        string compare = ((TMP_Text)Field(Field(ui, "compareController"), "compareText")).text;
        Check(compare.StartsWith("MATCH") || compare.StartsWith("MISMATCH") || compare.Contains("DEVIATION LOGGED"), $"a passport field compares with its book row ({compare})");

        ((Button)Field(ui, "denyButton")).onClick.Invoke();
        Check(Field(ui, "_currentCase") == null, "Deny ends the case");

        string log = SessionState.GetString(LogKey, string.Empty);
        Report("hybrid log: " + log.Replace('\n', '/'));
        int deskWarnings = log.Split('\n').Count(l => l.Contains("Desk scanner not wired"));
        Check(deskWarnings == 1 && !log.Contains("CinemachineBrain") && !log.Contains("[Error]") && !log.Contains("[Exception]"),
              $"the hybrid scene warns once that the desk scanner is not wired, says nothing about the brain, and logs no error ({deskWarnings} desk warning(s))");
        SessionState.SetString(LogKey, string.Empty);
        Next(5);
        EditorApplication.ExitPlaymode();
    }

    private static List<Button> Buttons(InvestigationUIController ui) =>
        ((List<GameObject>)Field(Field(ui, "interactionPanel"), "_spawned")).Where(g => g != null && g.activeSelf).Select(g => g.GetComponent<Button>()).ToList();

    private static string Label(Button b) => b.GetComponentInChildren<TMP_Text>(true).text;

    private static string FirstIconLabel(InvestigationUIController ui)
    {
        var shelf = (Transform)Field(ui, "bookShelfRoot");
        Transform first = shelf.Cast<Transform>().First(t => t.gameObject.activeSelf);
        return first.GetComponentInChildren<TMP_Text>(true).text;
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

        int fails = SessionState.GetInt(FailsKey, 0);
        Report($"fails={fails}");
        SessionState.SetInt(StepKey, 0);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        EditorApplication.Exit(fails == 0 ? 0 : 6);
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Log)
            SessionState.SetString(LogKey, SessionState.GetString(LogKey, string.Empty) + $"[{type}] {message}\n");
    }

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

- [ ] **Step 8: Run session C** (PowerShell, timeout 600000 ms), right after session B and before the hygiene step, so `RunConfig` is still pinned to session B's seed: the hybrid scene plays a day-1 traveller of that seed, generated with `spoken=false` (the scene wires no transcript, so `InterviewReachable` is false), not necessarily session B's slot 1; day 1 has no legendaries and one blueprint, so the passport and the permit are guaranteed

```powershell
Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe' -ArgumentList @('-projectPath','E:\unity\NOPE-feat-clock','-executeMethod','TimeDeskP7Scenes.Run','-logFile','C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad\unity_p7_scenes.log') -PassThru -Wait
```
Expected: exit code 0; `SCRATCH/p7_scenes_report.txt` holds `save path=… hadSave=…`, then `PASS Test_DayLoop shows a case with … era buttons`, `PASS Test_DayLoop: a case played through its era buttons with no error or exception`, `PASS Test_DayLoop: no warning names a piece-7 part (…)`, the `Test_DayLoop log:` line, `READY armed … s after START SHIFT`, `PASS the hybrid scene presents a traveller after READY`, `PASS the passport's window opens at presentation with its icon ('Travel Passport')`, `PASS the intercom lists [Request Transit Permit]`, `PASS the request opens the permit's window and is one-shot`, `PASS the permit's window got one icon`, `PASS a passport field compares with its book row (…)`, `PASS Deny ends the case`, the `hybrid log:` line (expected to carry the scene's `Traveller wheel or interview transcript not wired` warning and the one `Desk scanner not wired` warning), `PASS the hybrid scene warns once that the desk scanner is not wired, says nothing about the brain, and logs no error (1 desk warning(s))`, and `fails=0`. `SCRATCH/nope_save_backup_p7_scenes.json` must not remain (if it does, restore it with Step 6's recovery block, reading `p7_scenes_report.txt` and `nope_save_backup_p7_scenes.json`). Then `git status --short Assets/Scenes/OfficeScene_HybridArt.unity` prints nothing: Codex's scene was never saved.

- [ ] **Step 9: Read and judge every screenshot** (Read tool, each against its checklist; spec §6 step 7)

Open each file in SCRATCH with the Read tool and write down a verdict per item (pass, or what is wrong) for the record:
- `p7_booth_idle_1080.png`: the desktop inside the glass, with teal margins on all four sides and nothing drawn over the bezel; the idle line legible at a glance; no "Visitor"/"Document 1/2" leftovers; the power LED lit and the button on the bezel, not on the glass; no shimmer or broken text on the miniature.
- `p7_booth_papers_1080.png`: the traveller visible; the passport lying on the blotter, its title and name readable; the scan note on the tray readable and not covered; the wheel note above the traveller readable, clear of the clock and the poster; no paper over the CRT or the READY sign.
- `p7_wheel_hub_1080.png` and `p7_wheel_ask_1080.png`: the ring centred on the traveller and fully on screen; no two items overlapping; labels unclipped; "< Back" centred (in the ask menu), touching no ring item.
- `p7_bubble_1080.png`: the bubble beside the ring, not over it, fully on screen, its text unclipped; the transcript window visible on the live monitor.
- `p7_scanning_1080.png`: the paper on the scanner bed, above the tray; the refused paper back on the desk.
- `p7_tooltip_1080.png`: the tooltip above the till, fully on screen, its text unclipped, hiding nothing that matters (the CRT's glass).
- `p7_focus_1080.png`: the glass fills about 85% of the height with a bezel visible; the power button visible; the desktop legible (claim banner, window rows, taskbar); no window over the icon column; the transcript clear of the compare bar; "< Desk" in the taskbar.
- `p7_focus_720.png`: the same, legible at 720p (window rows readable). If it is not, that is a product fix ("When a check fails"): raise the `monitorFill` default in `DeskConfigSO` (so the recreated `Desk_Default` carries it) in a `fix:` commit that also updates the fill in FEATURES' "Click the CRT" line and the spec's fill figures (§1.2, §2.7, R13's 918/612 px and §7 "Readability"; or, where a figure is left, says so in the §9 record), re-shoot, re-judge, and record the fill used with the CRT art request (spec §7 "Readability").
- `p7_screen_off_booth_1080.png` and `p7_screen_off_focus_1080.png`: the glass shows the painted teal with no desktop; the LED dark.
- `p7_booth_720.png`: the props, the papers and the monitor miniature read correctly at 720p.

Any failed item is a product defect: fix it and re-verify as "When a check fails" says (Task 19 Steps 3–6 and 8, then this task from Step 3: session A proves the rebuilt scene again before sessions B and C re-shoot), then re-judge.

- [ ] **Step 10: Offline re-check**

Run the compile check and the test run. Expected: exit 0 / exit 0; `passed 433, failed 0` (more if a `fix:` commit added tests).

- [ ] **Step 11: Hygiene**

```bash
cd /e/unity/NOPE-feat-clock && rm -f Assets/Editor/_TimeDeskP7Automation.cs Assets/Editor/_TimeDeskP7Automation.cs.meta Assets/Editor/_TimeDeskP7PlaySmoke.cs Assets/Editor/_TimeDeskP7PlaySmoke.cs.meta Assets/Editor/_TimeDeskP7Scenes.cs Assets/Editor/_TimeDeskP7Scenes.cs.meta && git checkout -- Assembly-CSharp.csproj Assembly-CSharp-Editor.csproj TimeDesk.Domain.csproj TimeDeskEditMode.csproj NOPE.sln ProjectSettings "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset" Assets/Scenes/OfficeScene.unity Assets/Resources/RunConfig.asset && git status --short
```
Expected: only `?? NOPE-feat-clock.sln` and `?? TimeDesk.Visuals.csproj` (the verification changes no committed file; the play-through's glyphs only touch the reverted TMP fallback atlas, and the input setting it changed is restored and in `ProjectSettings` at most). No commit in this task unless a `fix:` was needed.

---

### Task 21: Cleanup and the verification record

Spec §6 step 8: the branch is left clean; the spec is aligned with what this plan built where it departs from it (Notes for the reviewer 1–5, and the plan review's findings 3, 5 and 7); and the verification record, with the screenshot verdicts, is appended to the spec as §9.

**Files:**
- Modify: `docs/superpowers/specs/2026-09-25-physical-desk-design.md` (LF; Step 2's script, then the Edit tool for the record)

- [ ] **Step 1: Final checks**

Run the compile check and the test run: exit 0 / exit 0; `passed 433, failed 0` (or the higher total after a `fix:` with tests). Then:

```bash
cd /e/unity/NOPE-feat-clock && git status --short && ls Assets/Editor | grep -c "_TimeDesk" ; git diff --stat 25a2c50 HEAD -- Assets/Scenes/OfficeScene_HybridArt.unity Assets/Resources/RunConfig.asset ProjectSettings && git ls-files --eol $(git diff --name-only 25a2c50 HEAD) | grep -c mixed ; ls "C:/Users/Saleh/AppData/Local/Temp/claude/E--unity-NOPE/06be6de7-86f0-489b-bc3c-afd3817f5196/scratchpad" | grep -c "nope_save_backup_p7"
```
Expected: `git status` shows only the two untracked files; `0` `_TimeDesk` files; no diff stat for the hybrid scene, `RunConfig` or `ProjectSettings`; `0` files with mixed line endings; `0` save backups. `git log --oneline 25a2c50..HEAD` lists the plan commit and this plan's commits in task order (Tasks 1–19, plus any `fix:`).

- [ ] **Step 2: Align the spec with this plan** — save as `SCRATCH/p7_t21_spec.py` and run:

```python
import sys
S = r'C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad'
sys.path.insert(0, S)
from subs import apply
W = r'E:\unity\NOPE-feat-clock'

# The spec says what the branch built: every place this plan departs from it
# (the plan's "Notes for the reviewer" and its review) is corrected in place,
# and a short list under Review notes names each one.
apply(W + r'\docs\superpowers\specs\2026-09-25-physical-desk-design.md', [
# --- R5: the radius-210 worked number ---
("""A circle of radius 210 fails at 7 items (the two lowest items sit level, 182 px apart, under the 248 px they need); the ellipse fits every count from 1 to 8 (worked in §2.6).""",
"""A circle of radius 210 fits only 4 (at 5 items the two lowest already sit level, 246.9 px apart, under the 248 px they need); the ellipse fits every count from 1 to 8 (worked in §2.6)."""),
# --- R26: cleared before the callback ---
("""| R26 | **`InvestigationUIController._currentCase` is reset at the decision.** |""",
"""| R26 | **`InvestigationUIController._currentCase` is reset at the decision, before the decision callback runs** (the callback may present the next traveller at once where no READY sign is wired, and that case must not be cleared after it). |"""),
# --- 2.4: one home for "waits for a request" and for the arrival documents ---
("""  - `bool Requested => handOver == DocumentHandOver.OnRequest`.
""",
"""  - `bool Requested => DocumentHandOvers.IsRequested(handOver)`.
- `public static class DocumentHandOvers`: `bool IsRequested(DocumentHandOver handOver)` = `handOver == OnRequest`, the one home of "waits for a request" (callers: `CaseDocument.Requested` and `ContentLibraryValidator.MaxRequestedDocuments`).
- `public static class CaseDocuments`: `IReadOnlyList<int> ArrivalIndices(IReadOnlyList<CaseDocument> documents)`, the documents handed over on arrival in paper order (null entries skipped, a null list gives none). Callers: the `DeskPapers` constructor and `InvestigationUIController.ShowRich`'s no-desk path.
"""),
("""`IReadOnlyList<int> ArrivalIndices` (the OnArrival papers in paper order);""",
"""`IReadOnlyList<int> ArrivalIndices` (the OnArrival papers in paper order, from `CaseDocuments.ArrivalIndices`);"""),
# --- 2.6: the radius-210 worked number; Extent ---
("""  - so `MaxFit(limit 16) == 8`. A circle of radius 210 fails at n = 7 (182.2 < 248), so its `MaxFit` is 6.
""",
"""  - so `MaxFit(limit 16) == 8`. A circle of radius 210 fits 4: n = 5 already collides (items 2 and 3 sit level at (±123.4, −169.9), 246.9 < 248 apart).
- `(float width, float height) Extent(float radiusX, float radiusY, float itemW, float itemH)` = (2 · radiusX + itemW, 2 · radiusY + itemH), the box around every item the ring can place ((840, 444) for the defaults). Caller: `TravellerWheel.Awake` sizes the ring's rect from it, so `OverlayProjection` keeps the whole ring on screen, not only its centre.
"""),
# --- 2.9: DeskDocument without a paper field; HandOver returns nothing ---
("""- **Fields:** `SpriteRenderer paper`, `TextMeshPro title`, `TextMeshPro holder`,""",
"""- **Fields** (the paper's `SpriteRenderer` sits on the root itself, R8, and no code reads it, so there is no field for it): `TextMeshPro title`, `TextMeshPro holder`,"""),
("""- **`bool HandOver(int i)`:** calls `DeskPapers.HandOver(i)`; when true, it clones""",
"""- **`void HandOver(int i)`** (its one caller, `InvestigationUIController.Choose`, needs no result; a paper that cannot be handed over is `DeskPapers.HandOver`'s tested "no change"): calls `DeskPapers.HandOver(i)`; when that succeeds, it clones"""),
# --- 2.10: the wheel without a panel field; the projection's canvas resolved once ---
("""`RectTransform ring` (the ring's root), `InteractionPanelController panel`, `RadialLayoutGroup layout`,""",
"""`RectTransform ring` (the ring's root; its `InteractionPanelController` is driven through `InvestigationUIController.interactionPanel`, R4, so the wheel holds no reference to it), `RadialLayoutGroup layout`,"""),
("""applies the config's radii and item size to the layout and its centre size to the centre slot, caches `Camera.main`, and deactivates the catcher""",
"""applies the config's radii and item size to the layout and its centre size to the centre slot, sizes the ring's rect to `RadialLayout.Extent`, caches `Camera.main` and its overlay canvas's rect (`OverlayProjection.CanvasRectOf`), and deactivates the catcher"""),
("""`OverlayProjection.TryPlace(ring, camera, traveller.Anchor.position, Vector2.zero)`""",
"""`OverlayProjection.TryPlace(ring, canvasRect, camera, traveller.Anchor.position, Vector2.zero)`"""),
("""- **Awake:** caches `Camera.main` and hides the panel (never its own GameObject).""",
"""- **Awake:** caches `Camera.main` and its overlay canvas's rect (`OverlayProjection.CanvasRectOf`), and hides the panel (never its own GameObject)."""),
("""`OverlayProjection.TryPlace(panel, camera, follow.position, offset)`""",
"""`OverlayProjection.TryPlace(panel, canvasRect, camera, follow.position, offset)`"""),
("""**`OverlayProjection`** (new, static): `bool TryPlace(RectTransform target, Camera camera, Vector3 world, Vector2 offset)`:
- uses the target's root canvas and the given camera (each caller caches `Camera.main` at `Awake`);""",
"""**`OverlayProjection`** (new, static): `RectTransform CanvasRectOf(Component host)` (the rect of the root canvas above the host, or null) and `bool TryPlace(RectTransform target, RectTransform canvasRect, Camera camera, Vector3 world, Vector2 offset)`:
- uses the given canvas rect and camera, which each caller resolves once at `Awake` (`CanvasRectOf`, `Camera.main`), so the per-frame call looks nothing up (MERGE_CRITERIA: no `GetComponent` in hot paths); false when either is missing;"""),
# --- 2.11: the no-desk arrivals, the reply only when there is one, the reset before the callback ---
("""  - when `DeskReachable`, `desk.BeginCase(docs)`; otherwise `OpenDocumentWindow(i)` for each arrival index;""",
"""  - when `DeskReachable`, `desk.BeginCase(docs)`; otherwise `OpenDocumentWindow(i)` for each of `CaseDocuments.ArrivalIndices(docs)` (the rule `DeskPapers` uses);"""),
("""  5. when a wheel is wired, `wheel.Say(DisplayText.For(InterviewScript.SpokenSince(_runner.Transcript, before), TextMedium.Spoken))`: the traveller's reply, the spoken reveal point (R19, R34);""",
"""  5. `reply = InterviewScript.SpokenSince(_runner.Transcript, before)`; when a wheel is wired and the reply is not empty, `wheel.Say(DisplayText.For(reply, TextMedium.Spoken))`: the traveller's reply, the spoken reveal point (R19, R34). A choice that adds no traveller line ("Ask about home >", "< Back") leaves the last reply up (§1.7);"""),
("""- **`Decide`:** `desk.EndCase()` when `DeskReachable`, `Hide()`, then the callback, then `_currentCase = null` (R26).""",
"""- **`Decide`:** `desk.EndCase()` when `DeskReachable`, `Hide()`, `_currentCase = null` (R26), then the callback (which may present the next traveller at once)."""),
# --- 2.14: the validator reuses the predicate ---
("""the most templates with `handOver == OnRequest` in one blueprint.""",
"""the most templates with `DocumentHandOvers.IsRequested(handOver)` in one blueprint."""),
# --- 2.15: one home for clearing persistent calls; the dropped fields ---
("""is replaced by `ClearPersistentCalls(signClick, "onClick")`, a new helper that empties `m_PersistentCalls.m_Calls`.""",
"""is replaced by `ClearPersistentCalls(signClick, "onClick")`, a new helper that empties `m_PersistentCalls.m_Calls` (`WirePersistentVoid` empties them through it before adding its call, so the clearing has one home)."""),
("""   - the wheel's fields: `catcher`, `ring`, `panel`, `layout`,""",
"""   - the wheel's fields: `catcher`, `ring`, `layout`,"""),
("""`DeskDocument` (`paper`, `title`, `holder`,""",
"""`DeskDocument` (`title`, `holder`,"""),
("""    - `TravellerWheel`: `catcher`, `ring`, `panel`, `layout`,""",
"""    - `TravellerWheel`: `catcher`, `ring`, `layout`,"""),
# --- 5: the tests as written ---
("""  - `ArrivalIndices` keep paper order;""",
"""  - `ArrivalIndices` keep paper order; `CaseDocuments.ArrivalIndices` skips a null entry and gives none for a null list; `DocumentHandOvers.IsRequested` holds only for `OnRequest`;"""),
("""a circle of radius 210 gives 6;""",
"""a circle of radius 210 gives 4 (n = 5 already collides);"""),
("""`limit` is respected (`limit 5` → 5).
""",
"""`limit` is respected (`limit 5` → 5);
  - `Extent`: (840, 444) for the defaults.
"""),
# --- Review notes: V1's number, and the list of departures ---
("""(a radius-210 circle gives 6)""",
"""(a radius-210 circle gives 4; the first draft said 6, corrected by the implementation plan's review)"""),
("""`BuildTaskbar` runs before the view exists.
""",
"""`BuildTaskbar` runs before the view exists.

### Implementation plan departures (2026-09-25)

The implementation plan (`docs/superpowers/plans/2026-09-25-physical-desk.md`) settled these while it was written and reviewed; the sections above now say the same:
- **The radius-210 worked number:** such a circle fits 4, not 6 (R5, §2.6, §5, V1); the shipped ellipse fits 8, as before.
- **`RadialLayout.Extent`** (tested) sizes the wheel's ring, so the projection keeps all of it on screen (§2.6, §2.10).
- **Two fields dropped as dead** (the F10/F19 rule): `TravellerWheel.panel` (§2.10, §2.15 items 7 and 12) and `DeskDocument.paper` (§2.9, §2.15 item 10).
- **`DeskController.HandOver` returns nothing** (§2.9).
- **`_currentCase` is cleared before the decision callback** (R26, §2.11).
- **One home per rule:** `CaseDocuments.ArrivalIndices` (the desk and the no-desk path) and `DocumentHandOvers.IsRequested` (`CaseDocument.Requested` and the validator) (§2.4, §2.11, §2.14, §5); `WirePersistentVoid` clears through `ClearPersistentCalls` (§2.15 item 4).
- **`OverlayProjection` takes the canvas rect** each caller resolves once at `Awake` (`CanvasRectOf`), so nothing is looked up per frame (§2.10).
- **The reply bubble** changes only for a choice that adds a traveller line; "Ask about home >" and "< Back" leave the last reply up (§1.7, §2.11).
"""),
])
```

Expected: `ok …2026-09-25-physical-desk-design.md lf`. The script corrects the radius-210 worked number (R5, §2.6, §5, V1: a circle of radius 210 fits 4), adds `RadialLayout.Extent` (§2.6, §5), drops `TravellerWheel.panel` and `DeskDocument.paper` (§2.9, §2.10, §2.15 items 7, 10 and 12), makes `DeskController.HandOver` return nothing (§2.9), clears `_currentCase` before the decision callback (R26, §2.11), names the one-home helpers (`CaseDocuments.ArrivalIndices`, `DocumentHandOvers.IsRequested`, `ClearPersistentCalls` inside `WirePersistentVoid`; §2.4, §2.11, §2.14, §2.15 item 4, §5), gives `OverlayProjection` its cached canvas rect (§2.10), says a choice without a reply leaves the bubble up (§2.11), and lists all of this under Review notes as "Implementation plan departures". If a `fix:` changed any of these places, adapt that pair (the code is the truth). Then commit:

```bash
cd /e/unity/NOPE-feat-clock && git add docs/superpowers/specs/2026-09-25-physical-desk-design.md && git commit -F - <<'EOF'
docs(spec): align the physical desk spec with its implementation plan

The radius-210 worked number is 4 (not 6); RadialLayout.Extent,
CaseDocuments.ArrivalIndices, DocumentHandOvers.IsRequested and
OverlayProjection.CanvasRectOf are named; TravellerWheel.panel and
DeskDocument.paper are gone; DeskController.HandOver returns nothing;
_currentCase is cleared before the decision callback; a choice without a
reply leaves the bubble up. Review notes list each departure.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```

- [ ] **Step 3: Append the verification record to the spec**

Append this section to the end of `docs/superpowers/specs/2026-09-25-physical-desk-design.md` (after "Review notes"), filling every `{…}` from the named report line or Task 20 Step 9's verdicts:

```markdown
## 9. Verification ({date of the run})

Run in the branch's own Unity 6000.4.11f1 editor through temporary `-executeMethod` scripts (not committed), at `{short hash of HEAD}`:

- Baseline before any piece-7 code (Task 0): seeds 12345 and 999 × days 1–3, 60 slots dumped (claim, name, date, role, verdict, lie, tells with channels, every paper value and answer); `spoken` true in the base scene; {the number of baseline null references, from p7_baseline_scene.txt} null references in the changed components.
- EditMode suite (step 1): {passed} passed; the one failure is the known third-party `UnitySkills.Tests.Core.PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly`. Offline Domain and Visuals run: {offline total}/{offline total}.
- Content (step 2): Generate World ran twice (Task 19) and twice again (Task 20) and changed no file; Validate Content Library: no issues; a traveller hands over at most 1 document on request and carries at most 2 papers.
- Generation unchanged (step 3): the 60 slots are byte-identical to the baseline; `spoken` is true in the rebuilt scene.
- Builder (step 4): Build Office UI ran twice on OfficeScene with no error, and the two semantic dumps ({lines} lines) are equal; two more builds of the committed scene (Task 20) dump the same, as does the committed scene as loaded. The desktop `Canvas` is World Space under `CRTMonitor/ScreenAnchor` (order 20, 1440×1080, `RectMask2D`, no scaler); the retired objects are gone; READY has no persistent call, the CRT focuses, the power button toggles, the exit zone leaves focus and the glass zone is inert; the traveller's zone and the intercom open the wheel; the overlay hosts are active with their `Catcher` and panels inactive; "Deviation Report" and "Material Analysis"; 16 raycast hits; `Desk_Default` and ten reactions. No missed wiring ({references walked} references; allowed nulls: {the allow-list line}). An unsaved third build reported exactly the wheel-capacity, spawn-slot and sorting-band errors and changed no file.
- Play-through (step 5; run seed {seed}, a new run on day 1; travellers {names}): {one sentence per step group from the report: 5.1–5.3 the briefing, READY and the notes; 5.4–5.5 the wheel, the bubble (kept through "< Back") and the one-shot request; 5.6–5.7 the virtual-mouse drag-to-scan, the refused paper, the clamp and the bring-to-front click; 5.8 the ten props and their tooltips; 5.9 the settled focus at 1080p and 720p and the logged deviation; 5.10–5.11 screen power, the glass zone, the three exits and the scan wake; 5.12 the citation hold; 5.13 closing with a scan running and behind READY; 5.14 day 2's spoken capital ("Traveller · CAPITAL", logged) and day 3; 5.15 the outline purge}. No warnings or errors.
- Other scenes (step 6): Test_DayLoop played one case with no error and no piece-7 warning; OfficeScene_HybridArt played one full case on the no-desk path (the passport's window and icon at presentation, the one-shot permit request, {the compare text}, Deny) with one "Desk scanner not wired" warning, the scene's existing "Traveller wheel or interview transcript not wired" warning (it wires no transcript, so its day generated with `spoken=false`) and nothing about the brain; the hybrid scene is unchanged.
- Screenshots (step 7), each read and judged against §6 step 7's checklist: `p7_booth_idle_1080` {verdict}; `p7_booth_papers_1080` {verdict}; `p7_wheel_hub_1080` {verdict}; `p7_wheel_ask_1080` {verdict}; `p7_bubble_1080` {verdict}; `p7_scanning_1080` {verdict}; `p7_tooltip_1080` {verdict}; `p7_focus_1080` {verdict}; `p7_focus_720` {verdict, with the fill used}; `p7_screen_off_booth_1080` {verdict}; `p7_screen_off_focus_1080` {verdict}; `p7_booth_720` {verdict}.
- Fixes made during verification: {each `fix:` commit with the check that found it, or "none"}.
- Balance (§7): the scripted play-through forces closing time, so it measures no throughput; scans take {scanSeconds} s of shift time each, and a queue or shift-length retune stays a later decision (V9).
```

- [ ] **Step 4: Commit the record**

```bash
cd /e/unity/NOPE-feat-clock && git add docs/superpowers/specs/2026-09-25-physical-desk-design.md && git commit -F - <<'EOF'
docs(spec): physical desk verification record

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
EOF
```
Final `git status --short`: only `?? NOPE-feat-clock.sln` and `?? TimeDesk.Visuals.csproj`.

---

## Merging this branch (spec §7 "Scene merge")

`art` (`0db426f`, Codex's checkout at `E:\unity\NOPE`) diverges heavily from `main` in `Assets/Scenes/OfficeScene.unity` and in the builder, and this branch commits a rebuilt `OfficeScene.unity` (Task 19) and a builder split into two files (Tasks 15–17). A textual merge of the scene YAML is impractical (every builder run rewrites thousands of lines with new fileIDs). When this branch is merged:
1. Coordinate with the Codex scene work first (ideally Codex commits its remaining scene changes). `OfficeScene_HybridArt.unity` is not touched by this branch.
2. On a conflict in `OfficeScene.unity`, never hand-merge it: take the other side's file, finish the merge of everything else (the builder conflicts by hand, keeping this branch's partial split), then run `Tools > TimeDesk > Build Office UI (HUD + Panels)` on the merged tree in Unity and save the scene. Builder-owned placement must then respect art-sync D3 ("existing-wins") and D4 (Codex's UI skins), spec §7.
3. Repeat Task 19 Steps 3–6 on the merged tree (two builds, equal semantic dumps, the scene checks, the missed-wiring walk; its `allow-list` baseline is Task 0's `p7_baseline_scene.txt`; Step 7's FEATURES lines are already merged) and Task 20 Steps 5–9 (the play-through, the other scenes and the screenshots), then commit the rebuilt scene alone.

---

## Spec coverage

| Spec | Task |
|---|---|
| K1, R29 (one World Space desktop canvas on the glass, no scaler, masked, the wallpaper enveloped) | 11, 15, 16, 19 |
| K2, R12, R13, R28 (4:3 desktop in the glass's inscribed rectangle, framing from the glass and a fill knob, the power button on the right bezel) | 6, 11, 15, 16 |
| K3, K10, R4, R5, R33, R34 (the traveller wheel on the overlay, the ring reusing `InteractionPanelController`, "< Back" in the centre, the intercom panel retired, always-active hosts, one callout for the bubble and tooltips) | 5, 8, 10, 13, 14, 16 |
| K4, R1, R7, R8 (EventSystem drags on collider proxies, the desk plane, drops by the projected pointer, papers as sorting groups) | 4, 12 |
| K5, V3, V4, R36 (scan = copy, one at a time, the drop outcome in Domain, no camera move, a dropped paper stays clamped) | 3, 12, 14 |
| K6, R3 (the `handOver` knob; arrival documents on both paths) | 3, 8, 9, 14 |
| K7, R31 (screen-only power, the bezel button and LED, Turn off screen and Quit game, the wake rules, the citation hold) | 1, 2, 11, 13, 15 |
| K8, R37 (focused input: the desktop and the power button; three exits; the glass zone) | 1, 4, 11, 15, 17 |
| K9 (READY without focus) | 15 |
| K11 (the verdict stays on the PC; the stamp reacts) | 17 |
| K12 (the paper's title, holder and photo slot) | 8 (holder), 12, 17 |
| K13, R24 (sorting bands as knobs, checked by the builder; 16 raycast hits) | 4, 11, 17 |
| K14, R20 (one `DeskReaction` with `DeskReactionSO`; tooltips from the live readouts) | 6, 12, 17 |
| K15 (one `DeskConfigSO`, created by the builder) | 11, 15, 19 |
| K16, R21 (decoration hooks: `DeskSlot`, `DeskItem`, `DeskDraggable`) | 12, 17 |
| K17, V2 (Deviation Report, Material Analysis) | 16 |
| K18 (a mipmapped wallpaper) | 16, 19 |
| K19, R19 (the display-text seam and its callers; the paper keeps its source text) | 7, 14 |
| K20 (the clickable booth traveller) | 13, 17 |
| K21, R14, R35 (built in `OfficeScene` only; every new reference optional; the hybrid scene's legacy path and brain lookup) | 11, 13, 14, 15–17, 19, 20 (session C) |
| V1 (one traveller-presence seam) | 13 |
| V5, R10 (one booth input table; phases set by `GameManager`) | 1, 13 |
| V6 (masked desktop, clamped windows) | 4, 10, 15 |
| V7 (`spoken` and case generation unchanged) | 0, 14, 20 (session A) |
| V8, R16, R32 (the two day-1 notes) | 3, 13, 17 |
| V9 (the clock keeps running during scans and blends) | 12 (unchanged clock), 20 (step 5.6) |
| R2 (one-shot requests; an icon the first time a window opens) | 8, 14 |
| R6 (catchers only while needed) | 13, 15, 16 |
| R9 (outline purge) | 10, 20 (step 5.15) |
| R11 (settled = the brain shows the view's camera and is not blending) | 11, 13 |
| R15 (`InitForTest`, `Toggle` removed) | 11 |
| R17 (the idle line) | 14, 16 |
| R18 (the five leftovers destroyed) | 16 |
| R22 (the partial builder file) | 15 |
| R23 (window layout as data, 4:3 values from the builder) | 14, 16 |
| R25 (copy: "Traveller · …", "(log a deviation before denying)", "the traveller wheel") | 7, 8, 14, 16, 17 |
| R26 (`_currentCase` reset at the decision, before the callback) | 8, 21 (the spec says so) |
| R27 (papers never outlive their case) | 12, 14, 20 (step 5.13) |
| R30 (art contracts amended by appended sections) | 18 |
| §2.18 every file that changes | File map above |
| §3.1 code removed (`MonitorOrthoSize`, `BuildBackToOfficeButton`, the intercom panel, `InitForTest`/`Toggle`, `DraggableWindow._canvas`, the Power entry's quit-only role, the leftovers) | 10, 11, 15, 16 |
| §3.2 earlier spec lines superseded (records kept; the piece-4 and piece-6 drafts amended) | 18 |
| §3.3 plan and art documents amended | 18 |
| §3.4 `docs/FEATURES.md` | 8, 9, 10, 14, 19 (with the rebuilt scene) |
| §5 tests | 1–8 |
| §6 verification (baseline, suite, content, generation, builder, play-through, other scenes, screenshots, hygiene, record) | 0, 19, 20, 21 |

---

## Notes for the reviewer (where this plan settles what the spec left open, or departs from it)

1. **The spec's worked radial-fit number is wrong.** §2.6 (:335), §5 (:1107) and review note V1 (:1305) say a radius-210 circle with 240 × 44 items "fails at n = 7 … so its `MaxFit` is 6". Five items on that circle already collide (items 2 and 3 sit level, 246.9 apart, less than 240 + 8; by the spec's own chord measure, n = 5 and n = 6 give 246.9 and 210, both below 248), so `MaxFit` is 4, and `RadialLayoutTests` pins 4. The shipped ellipse (300 × 200) fits 8 as R5 says, so nothing else changes. The verification record (§9) does not restate the number; Task 21 Step 2 corrects R5, §2.6, §5 and V1 in the spec (its own `docs(spec)` commit, since this plan's commit carries the plan alone).
2. **`_currentCase` is cleared before the decision callback** (R26 says "at the decision"): `Decide` sets it to null, then invokes `_onDecision`, so a case presented from inside that callback is never cleared after it. Task 21 Step 2 writes this into R26 and §2.11.
3. **Two serialized fields the spec lists are dropped as dead**: `TravellerWheel.panel` (§2.15 items 7 and 12, :701 and :732; the wheel reaches its ring through `InvestigationUIController.interactionPanel`, R4) and `DeskDocument.paper` (:715; the renderer sits on the paper's own root, R8). No code would read either, which the spec's own F10/F19 rule forbids. Task 21 Step 2 drops both from §2.9, §2.10 and §2.15.
4. **`DeskController.HandOver` returns nothing**: its one caller (`InvestigationUIController.Choose`) has no use for a result; a paper that cannot be handed over is `DeskPapers.HandOver`'s tested "no change". Task 21 Step 2 corrects §2.9.
5. **`RadialLayout.Extent` is added** (tested): the wheel sizes its ring's rect from it, so `OverlayProjection` clamps the whole ring on screen, not only its centre. Task 21 Step 2 adds it to §2.6 and §5.
6. **The ring's action template is stretched** (anchors 0–1), so a clone parented to the centre slot fills it while ring clones take `RadialLayoutGroup`'s item size: one template serves both.
7. **`BoothCoordinator` subscribes in `OnEnable`/`OnDisable` and applies the table first in `Start`**, after every scene component's `Awake`, so the first evaluation sees the wired references. `Apply` calls `wheel.SetCanOpen` before the rest, because closing the wheel changes an input of the same table.
8. **FEATURES:** the builder-driven lines change in Task 19's scene commit (`feat(scene): rebuild OfficeScene with the physical desk`), the commit that gives players that behaviour, so FEATURES never describes a committed scene that does not play it; the builder commits (Tasks 15–17) leave FEATURES alone, and lines whose behaviour code changes by itself change in that code's commit (Conventions). A `fix:` that changes behaviour or a documented default updates FEATURES in that commit.
9. **The outline-purge check (step 5.15) runs within day 1**, after two decided travellers and on the third's passport, instead of "after three travellers": the next day's scene load purges the cache by itself (`HandleSceneUnloaded`), which would make the check vacuous.
10. **The run seed is searched from 12345 upward** (spec: "seed 12345"): the play-through needs a passport-tell liar, an honest traveller and a non-legendary third traveller on day 1 and a spoken-capital liar on day 2, which one fixed seed need not give. The search uses the shipped `CaseFactory` call.
11. **No `GameView` reflection fallback is written**: `PlayModeWindow.SetCustomRenderingResolution` and `GetRenderingResolution` exist in 6000.4.11f1.
12. **Placement the spec left open is pinned as builder constants** (`OfficeSceneUIBuilder.Desk.cs`, data, no code change to move): the scan note at (0, −1.35) under the tray (6.6 × 0.6, order 4), the wheel note at (0, 2.75) above the traveller (4.4 × 0.5, order −9), the traveller zone at order −19, a calendar hit zone (0.35, 3.1; 2.4 × 3.6; order −49), the scanner bed at (0.1, 0.3) on the tray, the traveller's anchor at (0, 1, 2), and the desk art's far edge at y −0.84 (its first opaque row), below which the traveller is hidden.
13. **Session C runs right after session B**, while `RunConfig` is still pinned to B's seed, so the hybrid scene plays a day-1 traveller of that seed (spec §6 step 6 plays "one full case" without naming one). It is not necessarily session B's slot 1: the hybrid scene wires no transcript, so `InterviewReachable` is false and its day generates with `spoken=false`, and its log carries the existing "Traveller wheel or interview transcript not wired" warning. Day 1 has no legendaries and one blueprint, so the traveller is non-legendary and carries the passport and the permit either way. READY is pressed only once the day loop has armed it (step 31 of `_TimeDeskP7Scenes.cs`, as session B waits for `Ready.Interactable`).
14. **The Unity suite total** is the offline 433 plus the 129 other EditMode tests counted since piece 2: `passed=562 failed=1` (the known third-party failure).
15. **Balance (spec §7):** the scripted play-through forces closing time, so it cannot measure how many travellers a day handles; the record says so instead of reporting a number.
16. **Not dry-runnable offline:** the four Unity sessions (Tasks 0, 19, 20) and the screenshot judgements. Their scripts compile against the code they run on (checked), but their behaviour, URP's world-canvas rendering and the brain's blends are proven only in Unity, in Tasks 19–20.
17. **A choice without a reply leaves the bubble up.** `Choose` calls `wheel.Say` only when `SpokenSince` is non-empty, so "Ask about home >" and "< Back" (no lines, `InterviewScript.cs:115, 152`) no longer hide a reply the player is reading (spec §1.7: the bubble clears after 4 s, on focus, or when the traveller leaves). Step 5.4 presses "< Back" after the Currency answer and checks the bubble; that press also returns the wheel to the hub, which step 5.5 needs: the wheel reopens where the interview was (§1.7), so without it 5.5 would have looked for "Request Transit Permit" in the ask menu.
18. **Presses go through the raycast wherever the target is on screen.** The passport's field row (5.9, on the raised passport window, before the window-clamp drag moves it into the corner) and the transcript's answer row (5.14) are clicked through `ClickAt`, and the sliding permit's press (5.6) is resolved through the raycast too (it must hit the permit and reach no drag handler on it). Book rows, on pages a book shows only while searched, are still invoked directly.

---

## Plan review (2026-09-25)

Ten findings, each checked against the code at `25a2c50`, the spec and this plan's own scripts before it was applied. All ten are applied; none is rejected. The suite totals do not change (the new helpers' assertions sit in an existing `DeskPapersTests` test), and the dry run was repeated on the revised plan (Conventions, "Pre-verified").

| # | Finding | Verdict | Where |
|---|---|---|---|
| 1 | The plan departs from the spec (a radius-210 circle fits 4, not 6; `TravellerWheel.panel` and `DeskDocument.paper` dropped; `HandOver` returns nothing; `_currentCase` cleared before the callback; `Extent` added) and no task corrects the spec | applied | Task 21 Step 2 (`p7_t21_spec.py`, its own `docs(spec)` commit before the record): R5, R26, §2.4, §2.6, §2.9, §2.10, §2.11, §2.14, §2.15 items 4, 7, 10 and 12, §5 and V1 corrected, and an "Implementation plan departures" list under Review notes; Notes 1–5 |
| 2 | FEATURES' builder-driven lines landed in Task 17, two commits before the rebuilt scene that plays them, and Tasks 15–16 changed behaviour without FEATURES | applied, the first fix | The script moves to Task 19 Step 7 and ships with the scene in Step 8's `feat(scene): rebuild OfficeScene with the physical desk`; Task 17 no longer touches FEATURES; Conventions, file map, Note 8, coverage §3.4 |
| 3 | `Choose` called `Say` after every choice, so "< Back" (no lines) hid a reply | applied | Task 14 (`Say` only for a non-empty reply; the `Choose` doc); FEATURES' "Speech bubble" line (Task 19); step 5.4 checks that "< Back" keeps the reply; spec §2.11 (Task 21); Note 17 |
| 4 | A fix raising `monitorFill` would leave FEATURES' "fill 0.85" and the spec's fill figures stale | applied | Task 20 "When a check fails" (a fix that changes behaviour or a documented default updates FEATURES and the spec's figures in the same commit) and Step 9's `p7_focus_720` bullet; Conventions |
| 5 | `OverlayProjection.TryPlace` looked the canvas up with `GetComponentInParent` every frame | applied | Task 10 (`CanvasRectOf`; `TryPlace` takes the canvas rect; `OverlayCallout` caches it at `Awake`), Task 13 (`TravellerWheel` caches it); spec §2.10 (Task 21) |
| 6 | Session C pressed READY in START SHIFT's frame, before the day loop may have armed it | applied | Task 20 Step 7: step 3 presses START SHIFT; the new step 31 waits up to 20 s for `readySign.Interactable` (a failed check otherwise), then presses READY; Step 8's expected report |
| 7 | Re-implementation: the arrival documents computed twice, the on-request predicate stated twice, the clearing half of `WirePersistentVoid` repeated | applied | Task 3 (`CaseDocuments.ArrivalIndices`, `DocumentHandOvers.IsRequested`, tested in `DeskPapersTests`), Tasks 8 and 14 (the no-desk path), Task 9 (the validator), Task 15 (`ClearPersistentCalls(SerializedObject, …)` empties and returns the calls; `WirePersistentVoid` adds its call to them) |
| 8 | Desktop row clicks and the sliding-paper press skipped the raycast | applied | Task 20 Step 5: the passport field row (5.9, before the clamp drag) and the transcript answer row (5.14) through `ClickAt`; the sliding permit's press through the raycast (`TryBeginDrag` removed); book rows stay direct (Note 18) |
| 9 | Two FEATURES lines overstated behaviour (the glass zone's cursor, the photo slot) | applied | Task 19 Step 7's script |
| 10 | Prose disagreed with the code or the procedure (a–e) | applied | (a) Tasks 11 and 15: `MonitorScreen` on `CRTMonitor`, its `ScreenAnchor` child the glass; (b) Task 14 cites K5, R3; (c) Task 20 Step 8, Note 13 and the §9 template: a day-1 traveller of the seed with `spoken=false`, the transcript warning expected; (d) Step 9 points at "When a check fails"; (e) Task 19 no longer lists the paper template as recreated |

Found while verifying, and fixed here:
- **W1.** Step 5.5 looked for "Request Transit Permit" while the wheel was still on the ask menu (the wheel reopens where the interview was, spec §1.7), so the play-through would have stopped there; finding 3's "< Back" press returns it to the hub first (Note 17).
- **W2.** A re-run of Task 19 after a Task 20 `fix:` skips the FEATURES step (it runs once) and commits the scene alone; the merge procedure does the same.

Rejected: none.
