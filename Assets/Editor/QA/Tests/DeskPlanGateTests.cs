using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// DESK-PLAN's decor check on given results (pure; the QA editor runner):
/// the night of 2026-10-28, seam C: on the painted desk desk_plan_decor
/// stopped at its first drop ("No object 'Decor_form_sorter' is loaded"),
/// since the decorations are cut there (DESK-V3). The scenario's
/// deskhomeshown step now writes the "not applicable" line and passes the
/// drags over; the check reads the drops when any was measured at HEAD,
/// else that line, and only when it says not applicable.
/// </summary>
public class DeskPlanGateTests
{
    private const string Head = "abcdef0123456789abcdef0123456789abcdef01";

    private static QaGateResult Result(string id, bool pass, string measured, string commit = Head, bool dirty = false) =>
        new QaGateResult { id = id, owner = "DESK-PLAN", pass = pass, measured = measured, detail = "d", commit = commit, dirty = dirty };

    [Test]
    public void TheDrops_AtHead_AreJudged_AsBefore()
    {
        GateResult green = DeskPlanChecks.DecorVerdict(new[] { Result("desk_plan_decor.drop.Fast", true, "2.1 px"), Result("desk_plan_decor.drop.Slow", true, "1.0 px") }, Head);
        Assert.IsTrue(green.Pass, green.Detail);
        GateResult red = DeskPlanChecks.DecorVerdict(new[] { Result("desk_plan_decor.drop.Fast", true, "2.1 px"), Result("desk_plan_decor.drop.Slow", false, "12 px") }, Head);
        Assert.IsFalse(red.Pass);
        StringAssert.Contains("desk_plan_decor.drop.Slow", red.Detail);
    }

    [Test]
    public void OnThePaintedDesk_TheNotApplicableLine_PassesTheCheck_AndSaysWhy()
    {
        GateResult r = DeskPlanChecks.DecorVerdict(new[] { Result(DeskPlanChecks.DecorSpotsId, true, "not applicable on the painted desk") }, Head);
        Assert.IsTrue(r.Pass, r.Detail);
        StringAssert.Contains("not applicable", r.Measured);
    }

    [Test]
    public void ADeskWithSpots_AndNoDropMeasured_IsRed()
    {
        GateResult r = DeskPlanChecks.DecorVerdict(new[] { Result(DeskPlanChecks.DecorSpotsId, true, "in play") }, Head);
        Assert.IsFalse(r.Pass);
        StringAssert.Contains("no drop measured at HEAD", r.Detail);
    }

    [Test]
    public void NothingMeasured_OrTheLineStaleOrDirty_IsRed()
    {
        Assert.IsFalse(DeskPlanChecks.DecorVerdict(new QaGateResult[0], Head).Pass);
        Assert.IsFalse(DeskPlanChecks.DecorVerdict(new[] { Result(DeskPlanChecks.DecorSpotsId, true, "not applicable on the painted desk", "0000000000000000000000000000000000000000") }, Head).Pass, "stale");
        Assert.IsFalse(DeskPlanChecks.DecorVerdict(new[] { Result(DeskPlanChecks.DecorSpotsId, true, "not applicable on the painted desk", Head, true) }, Head).Pass, "dirty");
    }

    [Test]
    public void StaleDrops_DoNotMaskThePaintedDesksLine()
    {
        // A drop measured at an older commit (the art desk) beside the painted desk's line at HEAD: the line decides.
        GateResult r = DeskPlanChecks.DecorVerdict(new[]
        {
            Result("desk_plan_decor.drop.Fast", false, "12 px", "0000000000000000000000000000000000000000"),
            Result(DeskPlanChecks.DecorSpotsId, true, "not applicable on the painted desk"),
        }, Head);
        Assert.IsTrue(r.Pass, r.Detail);
    }
}
