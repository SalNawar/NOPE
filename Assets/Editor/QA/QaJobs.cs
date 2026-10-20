using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The QA rig's entry points for the persistent editors' job runner (a job
/// file holding "QaJobs.&lt;Method&gt;"; E:\unity\NOPE-tools\unity\_ClaudeJobRunner)
/// and the Time Sorter > QA menu. Play jobs return at once and finish on
/// their own: watch Library/QA/last_run.txt's folder for harness.txt.
/// docs/pending/QA-RIG.md has each recipe and its pass line.
/// </summary>
public static class QaJobs
{
    /// <summary>The scenarios' folder: &lt;project&gt;/tools/qa/scenarios.</summary>
    public static string Scenarios => Path.Combine(GateResults.ProjectRoot, "tools", "qa", "scenarios");

    /// <summary>The file naming an arbitrary scenario for <see cref="Scenario"/> (one line: a path, absolute or project-relative).</summary>
    public static string ScenarioPointer => Path.Combine(GateResults.ProjectRoot, "Library", "QA", "job_scenario.txt");

    /// <summary>The merge gate (INTEGRATION after every merge): report in Library/QA/DemoGate/report.md.</summary>
    [MenuItem("Time Sorter/QA/Jobs/Merge Gate")]
    public static void Gate() => DemoGate.Run(GateLevel.Merge);

    /// <summary>The demo gate (what DemoBuild runs first): report in Library/QA/DemoGate/report_demo.md.</summary>
    public static void GateDemo() => DemoGate.Run(GateLevel.Demo);

    /// <summary>The rig's self-test on an empty scene: every P-* path through the real event system, 1 px.</summary>
    [MenuItem("Time Sorter/QA/Jobs/Self-test")]
    public static void SelfTest() => QaPlayHarness.Start(Path.Combine(Scenarios, "selftest.json"), string.Empty, 5f);

    /// <summary>The smoke day: Title, New Run, day 1, one traveller decided, the probes, a 10 s sequence.</summary>
    [MenuItem("Time Sorter/QA/Jobs/Smoke Day")]
    public static void Smoke() => QaPlayHarness.Start(Path.Combine(Scenarios, "smoke_day1.json"));

    /// <summary>The office at A1-A4 and H1-H5: a still and the probes for each.</summary>
    [MenuItem("Time Sorter/QA/Jobs/Aspect Sweep")]
    public static void AspectSweep() => QaPlayHarness.Start(Path.Combine(Scenarios, "aspect_sweep.json"));

    /// <summary>Days 1, 5, 8, 12, 15 by each cheat path (Jump, Skip, Restart).</summary>
    [MenuItem("Time Sorter/QA/Jobs/Day Jumps")]
    public static void DayJumps() => QaPlayHarness.Start(Path.Combine(Scenarios, "day_jumps.json"), QaPlayHarness.TitleScene, 45f);

    /// <summary>Days 1 to 15 played in sequence (auto-decided shifts, Home, Sleep).</summary>
    [MenuItem("Time Sorter/QA/Jobs/Day Sequence")]
    public static void DaySequence() => QaPlayHarness.Start(Path.Combine(Scenarios, "day_sequence.json"), QaPlayHarness.TitleScene, 90f);

    /// <summary>BUG-65a: GC per call site on day 12 with every item (the editor profiler; perf.gcsites).</summary>
    [MenuItem("Time Sorter/QA/Jobs/GC Sites")]
    public static void GcSites() => QaPlayHarness.Start(Path.Combine(Scenarios, "perf_day12.json"));

    /// <summary>Any scenario: the path in Library/QA/job_scenario.txt (a track's recipe).</summary>
    public static void Scenario()
    {
        if (!File.Exists(ScenarioPointer))
        {
            Debug.LogError($"[QA] Write the scenario's path into {ScenarioPointer} first.");
            return;
        }
        string path = File.ReadAllText(ScenarioPointer).Trim();
        QaPlayHarness.Start(Path.IsPathRooted(path) ? path : Path.Combine(GateResults.ProjectRoot, path));
    }

    /// <summary>Unsticks the harness after a run that died (restores the saves, view, input and quality; call it again once out of play mode).</summary>
    [MenuItem("Time Sorter/QA/Jobs/Reset Harness")]
    public static void Reset() => QaPlayHarness.Reset();

    /// <summary>Builds the QA player (BuiltPlayerCapture): the demo's scenes with the rig compiled in, outside the project.</summary>
    [MenuItem("Time Sorter/QA/Jobs/Build QA Player")]
    public static void BuildQaPlayer() => BuiltPlayerCapture.Build();
}
