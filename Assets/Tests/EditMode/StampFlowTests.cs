using NUnit.Framework;

/// <summary>
/// The stamps, Papers, Please's way (Saleh 2026-10-06): the bar slides out
/// and back; a stamp presses what lies under it; only the passport takes it
/// (anywhere on it, Saleh 2026-10-06), once: the second stamp is refused, so approve and deny
/// together is impossible; other papers never take a verdict stamp; the
/// papers go back only with a verdict on the passport.
/// </summary>
public class StampFlowTests
{
    [Test]
    public void TheBar_SlidesOutAndBack_AndStows()
    {
        var flow = new StampFlow();
        Assert.IsFalse(flow.BarOut);
        Assert.IsTrue(flow.ToggleBar());
        Assert.IsFalse(flow.ToggleBar());
        Assert.IsFalse(flow.StowBar(), "in already");
        flow.ToggleBar();
        Assert.IsTrue(flow.StowBar());
        Assert.IsFalse(flow.BarOut);
    }

    [Test]
    public void AStamp_PressesOnlyWhileTheBarIsOut()
    {
        var flow = new StampFlow();
        Assert.AreEqual(StampPress.Nothing, flow.Press(DeskStamp.Approved, true, true));
        Assert.AreEqual(DeskStamp.None, flow.Verdict);
    }

    [Test]
    public void ThePassport_AnywhereOnIt_TakesTheStamp_AndThatIsTheVerdict()
    {
        var flow = new StampFlow();
        flow.ToggleBar();
        Assert.AreEqual(StampPress.Stamped, flow.Press(DeskStamp.Denied, true, true));
        Assert.AreEqual(DeskStamp.Denied, flow.Verdict);
        Assert.IsTrue(flow.CanHandBack);
    }

    [Test]
    public void ApproveAndDeny_OnOnePassport_IsImpossible()
    {
        var flow = new StampFlow();
        flow.ToggleBar();
        Assert.AreEqual(StampPress.Stamped, flow.Press(DeskStamp.Approved, true, true));
        Assert.AreEqual(StampPress.AlreadyStamped, flow.Press(DeskStamp.Denied, true, true), "the other stamp is refused");
        Assert.AreEqual(StampPress.AlreadyStamped, flow.Press(DeskStamp.Approved, true, true), "the same stamp again too");
        Assert.AreEqual(DeskStamp.Approved, flow.Verdict, "the first verdict stands");
    }

    [Test]
    public void AnotherPaper_NeverTakesAVerdictStamp()
    {
        var flow = new StampFlow();
        flow.ToggleBar();
        Assert.AreEqual(StampPress.NotPassport, flow.Press(DeskStamp.Approved, true, false));
        Assert.AreEqual(StampPress.NotPassport, flow.Press(DeskStamp.Denied, true, false));
        Assert.AreEqual(DeskStamp.None, flow.Verdict);
        Assert.IsFalse(flow.CanHandBack);
    }

    [Test]
    public void NothingUnderTheStamp_IsNothing()
    {
        var flow = new StampFlow();
        flow.ToggleBar();
        Assert.AreEqual(StampPress.Nothing, flow.Press(DeskStamp.Approved, false, false));
        Assert.AreEqual(StampPress.Nothing, flow.Press(DeskStamp.None, true, true));
    }

    [Test]
    public void ANewCase_ClearsTheVerdict_AndKeepsTheBar()
    {
        var flow = new StampFlow();
        flow.ToggleBar();
        flow.Press(DeskStamp.Approved, true, true);
        flow.BeginCase();
        Assert.AreEqual(DeskStamp.None, flow.Verdict);
        Assert.IsTrue(flow.BarOut);
        Assert.AreEqual(StampPress.Stamped, flow.Press(DeskStamp.Denied, true, true), "the next passport takes its own verdict");
    }
}
