using NUnit.Framework;

/// <summary>
/// The physical stamps (the desk-first redesign, item 12): the tray slides
/// out, a stamp is picked up, inked on the pad and pressed on a paper; an
/// inked press on the passport sets the verdict (the last one counts), a dry
/// press leaves a faint mark that counts for nothing, and the papers go back
/// only with a verdict on the passport.
/// </summary>
public class StampFlowTests
{
    [Test]
    public void AStamp_IsPickedUp_OnlyFromTheTrayOut()
    {
        var flow = new StampFlow(1);
        Assert.IsFalse(flow.PickUp(DeskStamp.Approved), "the tray is in");
        Assert.IsTrue(flow.OpenTray());
        Assert.IsFalse(flow.OpenTray(), "out already");
        Assert.IsTrue(flow.PickUp(DeskStamp.Approved));
        Assert.AreEqual(DeskStamp.Approved, flow.Held);
        Assert.IsTrue(flow.PickUp(DeskStamp.Denied), "the other stamp swaps in");
        Assert.AreEqual(DeskStamp.Denied, flow.Held);
        Assert.IsTrue(flow.CloseTray());
        Assert.AreEqual(DeskStamp.None, flow.Held, "closing the tray puts the stamp back");
    }

    [Test]
    public void ADryPress_LeavesAFaintMark_AndNoVerdict()
    {
        var flow = new StampFlow(1);
        flow.OpenTray();
        flow.PickUp(DeskStamp.Approved);
        Assert.AreEqual(StampMark.Faint, flow.Press(true));
        Assert.AreEqual(DeskStamp.None, flow.Verdict);
        Assert.IsFalse(flow.CanHandBack);
    }

    [Test]
    public void AnInkedPressOnThePassport_IsTheVerdict_AndUsesTheInk()
    {
        var flow = new StampFlow(1);
        flow.OpenTray();
        flow.PickUp(DeskStamp.Denied);
        Assert.IsTrue(flow.Ink());
        Assert.IsTrue(flow.HeldInked);
        Assert.AreEqual(StampMark.Inked, flow.Press(true));
        Assert.AreEqual(DeskStamp.Denied, flow.Verdict);
        Assert.IsTrue(flow.CanHandBack);
        Assert.IsFalse(flow.HeldInked, "one press per inking");
        Assert.AreEqual(StampMark.Faint, flow.Press(true), "dry again");
        Assert.AreEqual(DeskStamp.Denied, flow.Verdict, "a dry press changes nothing");
    }

    [Test]
    public void AnInkedPressOnAnotherPaper_LeavesAMark_ButNoVerdict()
    {
        var flow = new StampFlow(2);
        flow.OpenTray();
        flow.PickUp(DeskStamp.Approved);
        flow.Ink();
        Assert.AreEqual(StampMark.Inked, flow.Press(false));
        Assert.AreEqual(DeskStamp.None, flow.Verdict);
        Assert.AreEqual(1, flow.InkOf(DeskStamp.Approved), "two presses per inking: one left");
    }

    [Test]
    public void TheLastInkedPressOnThePassport_Counts()
    {
        var flow = new StampFlow(1);
        flow.OpenTray();
        flow.PickUp(DeskStamp.Approved);
        flow.Ink();
        flow.Press(true);
        flow.PickUp(DeskStamp.Denied);
        flow.Ink();
        flow.Press(true);
        Assert.AreEqual(DeskStamp.Denied, flow.Verdict);
    }

    [Test]
    public void EachStamp_KeepsItsOwnInk_OnTheTray()
    {
        var flow = new StampFlow(1);
        flow.OpenTray();
        flow.PickUp(DeskStamp.Approved);
        flow.Ink();
        Assert.IsTrue(flow.PutDown());
        Assert.IsFalse(flow.PutDown(), "nothing held");
        Assert.IsFalse(flow.Ink(), "nothing held to ink");
        flow.PickUp(DeskStamp.Denied);
        Assert.IsFalse(flow.HeldInked, "the other stamp is dry");
        flow.PickUp(DeskStamp.Approved);
        Assert.IsTrue(flow.HeldInked, "picked up again, still inked");
    }

    [Test]
    public void ANewCase_ClearsTheVerdictAndTheHand_NotTheTrayOrTheInk()
    {
        var flow = new StampFlow(3);
        flow.OpenTray();
        flow.PickUp(DeskStamp.Approved);
        flow.Ink();
        flow.Press(true);
        flow.BeginCase();
        Assert.AreEqual(DeskStamp.None, flow.Verdict);
        Assert.AreEqual(DeskStamp.None, flow.Held);
        Assert.IsTrue(flow.TrayOut);
        Assert.AreEqual(2, flow.InkOf(DeskStamp.Approved));
        Assert.AreEqual(StampMark.None, flow.Press(true), "nothing held");
    }

    [Test]
    public void PressesPerInking_IsAtLeastOne()
    {
        var flow = new StampFlow(0);
        flow.OpenTray();
        flow.PickUp(DeskStamp.Approved);
        flow.Ink();
        Assert.AreEqual(1, flow.InkOf(DeskStamp.Approved));
    }
}
