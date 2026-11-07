using System.Linq;
using NUnit.Framework;

/// <summary>
/// harness.txt's first line tells the truth about the frames too (GATE-07 of
/// 2026-10-28: the line was written before the frame analysis ran and said
/// "ok" over red flicker and latency lines). The verdict is red when any
/// analysis line is red and not on the known-red list, when an analysis could
/// not run, and when the run itself failed. Pure; runs outside Unity.
/// </summary>
public class HarnessVerdictTests
{
    private const string Run = "cam_views_20261028_193253";
    private const string Seq = @"E:\unity\NOPE-t6\Library\QA\runs\cam_views_20261028_193253\seq\d12_desk_work";

    private static SeqAnalysis Analysis(string output, bool ran = true, bool exitOk = true) =>
        new SeqAnalysis { Seq = Seq, Ran = ran, ExitOk = exitOk, Output = output };

    [Test]
    public void ARedLatencyLine_TurnsAnOkRunRed_AndIsNamedById()
    {
        string output = $"PASS flash {Seq}: 2 flashes/s at worst (max 3); 0 red full-field\n" +
                        $"FAIL latency {Seq}: 22 presses; worst 2 frames to the first change (max 1); 0 with no change\n" +
                        $"  press f25754 left: visual 2, cue None\n";
        HarnessVerdict v = HarnessVerdict.Judge("ok", true, Run, new[] { Analysis(output, exitOk: false) }, new string[0]);
        Assert.IsFalse(v.Ok);
        StringAssert.StartsWith("failed", v.Line);
        StringAssert.Contains("cam_views.seq.d12_desk_work.latency", v.Line);
        StringAssert.Contains("worst 2 frames", v.Line);
        Assert.AreEqual(1, v.Unexpected.Count);
    }

    [Test]
    public void AKnownRedAnalysisLine_KeepsTheRunOk_AndIsListed()
    {
        string output = $"FAIL flicker {Seq}: 381519 px toggling > 2x in 10 s (worst 48)\n";
        HarnessVerdict v = HarnessVerdict.Judge("ok", true, Run, new[] { Analysis(output, exitOk: false) }, new[] { "*.seq.*.flicker" });
        Assert.IsTrue(v.Ok);
        StringAssert.StartsWith("ok", v.Line);
        StringAssert.Contains("frame analysis: 1 known red", v.Line);
        StringAssert.Contains("cam_views.seq.d12_desk_work.flicker", v.Line);
        Assert.AreEqual(1, v.KnownRed.Count);
    }

    [Test]
    public void GreenAnalysis_LeavesTheRunsLineAsItIs()
    {
        string output = $"PASS flicker {Seq}: 0 px\nPASS cuts {Seq}: 0 single-frame jumps\nSKIP latency {Seq}: no press in events.tsv\n";
        HarnessVerdict v = HarnessVerdict.Judge("ok (1 known red, tools/qa/gate_known_red.txt: x)", true, Run, new[] { Analysis(output) }, new string[0]);
        Assert.IsTrue(v.Ok);
        Assert.AreEqual("ok (1 known red, tools/qa/gate_known_red.txt: x)", v.Line);
        Assert.AreEqual("ok", HarnessVerdict.Judge("ok", true, Run, Enumerable.Empty<SeqAnalysis>(), new string[0]).Line, "no sequence recorded: nothing to add");
    }

    [Test]
    public void AnAnalysisThatCouldNotRunOrCrashed_IsRed()
    {
        HarnessVerdict notRun = HarnessVerdict.Judge("ok", true, Run, new[] { Analysis("could not run python (file not found)", ran: false, exitOk: false) }, new string[0]);
        Assert.IsFalse(notRun.Ok);
        StringAssert.Contains("did not run", notRun.Line);
        HarnessVerdict crashed = HarnessVerdict.Judge("ok", true, Run, new[] { Analysis("Traceback (most recent call last):\n  ValueError", exitOk: false) }, new string[0]);
        Assert.IsFalse(crashed.Ok);
        StringAssert.Contains("ended with an error", crashed.Line);
    }

    [Test]
    public void AFailedRun_StaysFailed_AndKeepsItsFirstWord_WithTheAnalysisAdded()
    {
        string output = $"FAIL cuts {Seq}: 1 single-frame jumps in mean luma over 15 in 17 frames\n";
        HarnessVerdict v = HarnessVerdict.Judge("timeout: the run did not finish before its deadline", false, Run, new[] { Analysis(output, exitOk: false) }, new string[0]);
        Assert.IsFalse(v.Ok);
        StringAssert.StartsWith("timeout:", v.Line);
        StringAssert.Contains("cam_views.seq.d12_desk_work.cuts", v.Line);
        Assert.IsFalse(HarnessVerdict.Judge(string.Empty, false, Run, Enumerable.Empty<SeqAnalysis>(), new string[0]).Ok, "no run line: never ok");
    }

    [Test]
    public void TheScenario_IsTheRunFolderLessItsTime()
    {
        Assert.AreEqual("cam_views", HarnessVerdict.ScenarioOf("cam_views_20261028_193253"));
        Assert.AreEqual("qa_rig_truth", HarnessVerdict.ScenarioOf(@"E:\x\runs\qa_rig_truth_20261107_101500"));
        Assert.AreEqual("selftest", HarnessVerdict.ScenarioOf("selftest"));
    }
}
