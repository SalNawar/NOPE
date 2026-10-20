using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The base of every track's play tests (P0-B: "UTF [UnityTest]
/// PlayMode-in-editor tests in an Editor-side assembly that can see
/// Assembly-CSharp types"). A track writes its tests in
/// Assets/Tests/PlayMode/&lt;Track&gt;/Editor/ (Assembly-CSharp-Editor, which
/// sees the game's types; a PlayMode asmdef cannot reference
/// Assembly-CSharp), derives from this class and plays a scenario of
/// tools/qa/scenarios through the same harness as the job runner:
/// <code>
/// [UnityTest, Explicit, Category("QaPlay")]
/// public IEnumerator Drops()
/// {
///     Assert.IsTrue(Prepare("doc_drops.json"));
///     yield return new EnterPlayMode();
///     yield return Play();
///     yield return new ExitPlayMode();
///     AssertRun(Finish());
/// }
/// </code>
/// Every step between the yields keeps its state in SessionState and the
/// run's folder, so the domain reload of entering play mode loses nothing.
/// The run's assertions become gate results as with the job runner.
/// </summary>
public abstract class QaPlayTest
{
    /// <summary>The path of scenario <paramref name="file"/> in tools/qa/scenarios.</summary>
    protected static string Scenario(string file) => Path.Combine(GateResults.ProjectRoot, "tools", "qa", "scenarios", file);

    /// <summary>In edit mode: prepares scenario <paramref name="file"/> from <paramref name="startScene"/> (empty: an empty scene). False when it refused.</summary>
    protected static bool Prepare(string file, string startScene = QaPlayHarness.TitleScene, float maxMinutes = 30f) =>
        QaPlayHarness.Prepare(Scenario(file), startScene, maxMinutes) != null;

    /// <summary>In play mode: plays the prepared scenario to its end (or its deadline) and imports its results.</summary>
    protected static IEnumerator Play()
    {
        QaRunner runner = QaPlayHarness.BeginRun();
        while (!runner.Done && !QaPlayHarness.PastDeadline)
            yield return null;
        QaPlayHarness.Collect();
    }

    /// <summary>Back in edit mode: the run's report (read from its folder) after the editor is restored and the sequences are analysed.</summary>
    protected static QaRunReport Finish()
    {
        QaRunReport report = QaPlayHarness.ReadReport();
        QaPlayHarness.Restore();
        QaPlayHarness.Analyze();
        return report;
    }

    /// <summary>
    /// Fails with every unexpected failure unless the run finished and every
    /// red assertion is on the known-red list (tools/qa/gate_known_red.txt's
    /// run patterns: an open BUG part a later track owns, e.g. the camera
    /// until CAM merges). Known red ones are logged, not failed, so the test
    /// turns red only on a regression, as the merge gate does.
    /// </summary>
    protected static void AssertRun(QaRunReport report)
    {
        Assert.IsNotNull(report, $"The run did not finish (see {QaPlayHarness.OutDir}).");
        RunVerdict verdict = DemoGate.JudgeRun(report, DemoGate.KnownRedRunPatterns());
        if (verdict.KnownRed.Count > 0)
            Debug.Log($"[QA] {report.scenario}: {verdict.KnownRed.Count} known red (tools/qa/gate_known_red.txt):\n{string.Join("\n", verdict.KnownRed)}");
        Assert.IsTrue(verdict.Ok, $"{report.scenario} failed ({QaPlayHarness.OutDir}):\n{string.Join("\n", verdict.Unexpected)}");
    }
}
