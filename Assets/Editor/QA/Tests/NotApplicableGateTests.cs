using System.Linq;
using NUnit.Framework;

/// <summary>
/// A result that did not apply is neutral (GATE-12 of 2026-10-28: 32 "not
/// applicable on the painted desk" lines passed as green): the gate lists it
/// and counts it in neither pass nor fail; a check whose every result is n/a
/// is n/a itself, never a pass and never a refusal; a red, stale or dirty
/// result beside n/a ones still makes the check red. Pure; runs outside Unity.
/// </summary>
public class NotApplicableGateTests
{
    private const string Head = "abcdef0123456789abcdef0123456789abcdef01";

    private static QaGateResult Result(string id, bool pass, bool na = false, string commit = Head) =>
        new QaGateResult { id = id, owner = "DESK-PLAN", pass = pass, na = na, measured = na ? "not applicable on the painted desk" : "96 %", detail = na ? "the panel shows nothing" : "d", commit = commit };

    [Test]
    public void NaResults_CountInNeitherPassNorFail_AndAreListed()
    {
        GateResult r = GateResults.Judge("desk.play.visible", "DESK-PLAN", "desk_plan_homes.visible.*",
            new[] { Result("desk_plan_homes.visible.mat", true), Result("desk_plan_homes.visible.ring", true, na: true) }, Head, false);
        Assert.IsTrue(r.Pass);
        Assert.IsFalse(r.NotApplicable, "one result applied and passed");
        StringAssert.Contains("1 pass, 0 fail, 1 n/a", r.Measured);
        StringAssert.Contains("N/A desk_plan_homes.visible.ring not applicable on the painted desk: the panel shows nothing", r.Detail);
    }

    [Test]
    public void ACheckWhoseEveryResultIsNa_IsNa_NeverAPass()
    {
        GateResult r = GateResults.Judge("desk.play.visible", "DESK-PLAN", "desk_plan_homes.visible.*",
            new[] { Result("desk_plan_homes.visible.ring", true, na: true), Result("desk_plan_homes.visible.rack", true, na: true) }, Head, false);
        Assert.IsTrue(r.NotApplicable);
        Assert.IsFalse(r.Green);
        StringAssert.Contains("0 pass, 0 fail, 2 n/a", r.Measured);
        StringAssert.StartsWith("N/A desk.play.visible", r.ToString());
        DemoGateReport report = DemoGate.Evaluate(GateLevel.Demo, new[] { r, GateResult.Ok("a.one", "QA-RIG", "1") }, 2);
        Assert.IsFalse(report.Refused, "n/a never refuses");
        Assert.AreEqual("1 pass, 0 fail, 1 n/a", report.Counts);
        string md = report.ToMarkdown();
        StringAssert.Contains("## Not applicable", md);
        StringAssert.Contains("- N/A `desk.play.visible`", md);
    }

    [Test]
    public void ARedOrStaleResult_BesideNaOnes_StillMakesTheCheckRed()
    {
        GateResult red = GateResults.Judge("desk.play.visible", "DESK-PLAN", "desk_plan_homes.visible.*",
            new[] { Result("desk_plan_homes.visible.mat", false), Result("desk_plan_homes.visible.ring", true, na: true) }, Head, false);
        Assert.IsFalse(red.Pass);
        CollectionAssert.AreEqual(new[] { "desk_plan_homes.visible.mat" }, red.Unmet, "the n/a line is no unmet result");
        GateResult stale = GateResults.Judge("desk.play.visible", "DESK-PLAN", "desk_plan_homes.visible.*",
            new[] { Result("desk_plan_homes.visible.ring", true, na: true, commit: "0000000000000000000000000000000000000000") }, Head, false);
        Assert.IsFalse(stale.Pass, "an n/a line from an older commit is stale, as any result");
        Assert.IsFalse(stale.NotApplicable);
    }

    [Test]
    public void ARunsNaAssertion_BecomesAnNaResult_AndNeverFailsTheRun()
    {
        var report = new QaRunReport
        {
            scenario = "desk_plan_homes", ok = true,
            assertions = new[] { new QaAssertion { id = "desk_plan_homes.visible.ring", owner = "DESK-PLAN", pass = true, na = true, measured = "not applicable on the painted desk" } },
        };
        QaGateResult r = GateResults.FromRun(report, "run", Head, false).Single();
        Assert.IsTrue(r.na);
        Assert.IsTrue(r.pass);
        Assert.IsTrue(DemoGate.JudgeRun(report, new string[0]).Ok);
    }
}
