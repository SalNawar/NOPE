using NUnit.Framework;

/// <summary>
/// The machine close-up (the desk layout spec §4): on with the setting, never
/// during a drag, held while the machine works, given back by any input or
/// when the hold runs out, taken over by the next machine.
/// </summary>
public class CloseUpTimelineTests
{
    [Test]
    public void AMachineUsed_IsShownClose_ForItsHold()
    {
        var t = new CloseUpTimeline();
        Assert.IsTrue(t.Start("scanner", true, false, 10f, 1.5f));
        Assert.AreEqual("scanner", t.Machine);
        Assert.IsFalse(t.Tick(11f));
        Assert.IsTrue(t.Tick(11.5f), "the hold ran out");
        Assert.IsFalse(t.Active);
        Assert.IsFalse(t.Tick(12f), "it ends once");
    }

    [Test]
    public void TheSettingOff_OrADrag_ShowsNothing()
    {
        var t = new CloseUpTimeline();
        Assert.IsFalse(t.Start("scanner", false, false, 0f, 1f));
        Assert.IsFalse(t.Start("scanner", true, true, 0f, 1f));
        Assert.IsFalse(t.Start(null, true, false, 0f, 1f));
        Assert.IsFalse(t.Active);
    }

    [Test]
    public void AnyInput_GivesTheViewBack_AtOnce()
    {
        var t = new CloseUpTimeline();
        t.Start("chip_reader", true, false, 0f, 5f);
        Assert.IsTrue(t.Input());
        Assert.IsFalse(t.Active);
        Assert.IsFalse(t.Input(), "nothing left to end");
    }

    [Test]
    public void AMachineStillWorking_KeepsItsCloseUp_AnotherDoesNot()
    {
        var t = new CloseUpTimeline();
        t.Start("chip_reader", true, false, 0f, 1f);
        t.Extend("scanner", 0.5f, 10f);
        t.Extend("chip_reader", 0.5f, 2f);
        Assert.IsFalse(t.Tick(2f));
        Assert.IsTrue(t.Tick(2.5f));
    }

    [Test]
    public void TheNextMachine_TakesItOver()
    {
        var t = new CloseUpTimeline();
        t.Start("scanner", true, false, 0f, 5f);
        Assert.IsTrue(t.Start("rolodex", true, false, 1f, 1f));
        Assert.AreEqual("rolodex", t.Machine);
        Assert.IsTrue(t.Tick(2f), "the new hold counts");
    }
}
