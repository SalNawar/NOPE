using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// DESK-PLAN's decor check on given results (pure; the QA editor runner):
/// the night of 2026-10-28, seam C: on the painted desk desk_plan_decor
/// stopped at its first drop ("No object 'Decor_form_sorter' is loaded"),
/// since the decorations are cut there (DESK-V3). The scenario's
/// deskhomeshown step now writes the "not applicable" line and passes the
/// drags over; the check reads the drops when any was measured at HEAD,
/// else that line. The code review's RG-03 (2026-10-28): that line is red,
/// not green (the decorations are cut, a feature Saleh asked for); the
/// known-red list spares only that line (BUG-23a, DESK-V3).
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
    public void OnThePaintedDesk_TheNotApplicableLine_IsRed_TheCutIsKnownRedUnderDeskV3()
    {
        GateResult r = DeskPlanChecks.DecorVerdict(new[] { Result(DeskPlanChecks.DecorSpotsId, true, "not applicable on the painted desk") }, Head);
        Assert.IsFalse(r.Pass, "a cut feature is not green");
        StringAssert.Contains("not applicable", r.Measured);
        StringAssert.Contains("cut", r.Detail);
        StringAssert.Contains("BUG-23a", r.Detail);
        CollectionAssert.AreEqual(new[] { DeskPlanChecks.DecorSpotsId }, r.Unmet, "it names the line, so the known-red entry spares that line only");

        string text = System.IO.File.ReadAllText(System.IO.Path.Combine(SoFieldReadersTests.ProjectRoot(), "tools", "qa", "gate_known_red.txt"));
        Dictionary<string, string> known = DemoGate.ParseKnownRed(text);
        Dictionary<string, List<string>> own = DemoGate.KnownRedPatternsByCheck(text);
        StringAssert.Contains("BUG-23a", known["desk.play.decor"]);
        StringAssert.Contains("DESK-V3", known["desk.play.decor"]);
        Assert.IsFalse(DemoGate.Evaluate(GateLevel.Merge, new[] { r }, 1, known, own).Refused, "the merge gate lists the cut");
        Assert.IsTrue(DemoGate.Evaluate(GateLevel.Demo, new[] { r }, 1, known, own).Refused, "the demo gate refuses it");
        GateResult drop = DeskPlanChecks.DecorVerdict(new[] { Result("desk_plan_decor.drop.Slow", false, "12 px") }, Head);
        Assert.IsTrue(DemoGate.Evaluate(GateLevel.Merge, new[] { drop }, 1, known, own).Refused, "a red drop on the art desk is not spared with the cut");
    }

    [Test]
    public void TheLineWrittenAsNotApplicable_IsStillRed_ForTheCutFeature()
    {
        // Since GATE-12 the scenario writes the line as n/a (neutral); the decor check still turns the cut red (RG-03).
        QaGateResult na = Result(DeskPlanChecks.DecorSpotsId, true, "not applicable on the painted desk");
        na.na = true;
        GateResult r = DeskPlanChecks.DecorVerdict(new[] { na }, Head);
        Assert.IsFalse(r.Pass);
        Assert.IsFalse(r.NotApplicable);
        CollectionAssert.AreEqual(new[] { DeskPlanChecks.DecorSpotsId }, r.Unmet);
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
        Assert.IsFalse(r.Pass, "the cut is red (RG-03)");
        CollectionAssert.AreEqual(new[] { DeskPlanChecks.DecorSpotsId }, r.Unmet, "the line decides, not the stale drop");
    }
}
