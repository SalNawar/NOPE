using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

/// <summary>
/// FLOW's play tests (docs/pending/FLOW.md): each plays one of FLOW's QA
/// recipes (tools/qa/scenarios/flow_*.json, with FlowOps) through QA-RIG's
/// harness and asserts its run. Explicit (each enters play mode for
/// minutes): run them by name, or by the job runner's QaJobs.Scenario with
/// Library/QA/job_scenario.txt naming the recipe, which plays the same
/// scenario. The built-player load gate (flow_load and flow_load_seq on the
/// QA player) runs through tools/qa/run_player.py instead.
/// </summary>
public class FlowPlayTests : QaPlayTest
{
    /// <summary>Esc reaches Settings in the office, at Home and on the title, the clock holds while it shows, and the focus lost holds the clock and the audio (BUG-34).</summary>
    [UnityTest, Explicit, Category("QaPlay")]
    public IEnumerator Pause_EscReachesSettings_AndTheClockHolds()
    {
        Assert.IsTrue(Prepare("flow_pause.json"));
        yield return new EnterPlayMode();
        yield return Play();
        yield return new ExitPlayMode();
        AssertRun(Finish());
    }

    /// <summary>The last traveller's payoff plays before the report; the day's end, Go Home and Sleep are recorded for the cuts and frozen-frame analyses (BUG-32a, BUG-72).</summary>
    [UnityTest, Explicit, Category("QaPlay")]
    public IEnumerator Transitions_ThePayoffBeforeTheReport_AndNoHardCut()
    {
        Assert.IsTrue(Prepare("flow_transitions.json"));
        yield return new EnterPlayMode();
        yield return Play();
        yield return new ExitPlayMode();
        AssertRun(Finish());
    }

    /// <summary>Day 12's night shift closes at 24:00, and an early finish raises Closed once, so the closing bell rings (BUG-72). About ten minutes: the shift runs to closing.</summary>
    [UnityTest, Explicit, Category("QaPlay")]
    public IEnumerator Night_ClosesAt2400_AndAnEarlyFinishRingsTheBell()
    {
        Assert.IsTrue(Prepare("flow_night.json", QaPlayHarness.TitleScene, 45f));
        yield return new EnterPlayMode();
        yield return Play();
        yield return new ExitPlayMode();
        AssertRun(Finish());
    }

    /// <summary>A scene change through SceneTransit (office to Title, Title to office): the booth's shutter comes down, the scenes load behind it and it goes up; recorded for the frozen and cuts analyses (BUG-31a, the bible's J5).</summary>
    [UnityTest, Explicit, Category("QaPlay")]
    public IEnumerator Shutter_ASceneChangeLoadsBehindTheShutter()
    {
        Assert.IsTrue(Prepare("flow_shutter.json"));
        yield return new EnterPlayMode();
        yield return Play();
        yield return new ExitPlayMode();
        AssertRun(Finish());
    }

    /// <summary>A verdict that ends the run closes the shift; the report, then the evening edition with Title and Quit (BUG-56a, BUG-28b).</summary>
    [UnityTest, Explicit, Category("QaPlay")]
    public IEnumerator Endings_TheReportThenTheEveningEdition()
    {
        Assert.IsTrue(Prepare("flow_endings.json"));
        yield return new EnterPlayMode();
        yield return Play();
        yield return new ExitPlayMode();
        AssertRun(Finish());
    }
}
