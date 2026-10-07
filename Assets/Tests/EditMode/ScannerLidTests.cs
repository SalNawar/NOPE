using NUnit.Framework;

/// <summary>The cream scanner's lid (ScannerLid, Track BR): when it stands open, what counts as near, the readout's count; and the scanner's prop contract.</summary>
public sealed class ScannerLidTests
{
    private const float Idle = 2.5f;

    [Test]
    public void TheLid_OpensForAPaperDraggedNear_AndShutsWhenItLeaves()
    {
        Assert.IsFalse(ScannerLid.Open(false, false, float.PositiveInfinity, Idle), "at rest, no scan yet: shut");
        Assert.IsTrue(ScannerLid.Open(false, true, float.PositiveInfinity, Idle), "a paper dragged near: open");
        Assert.IsFalse(ScannerLid.Open(false, false, float.PositiveInfinity, Idle), "it leaves without dropping: shut again");
    }

    [Test]
    public void TheLid_ShutsForTheScan_OpensAgainAfter_AndShutsByItselfWhenIdle()
    {
        Assert.IsFalse(ScannerLid.Open(true, true, float.PositiveInfinity, Idle), "dropped: it shuts for the scan, even with the pointer still near");
        Assert.IsTrue(ScannerLid.Open(false, false, 0f, Idle), "the scan ends: it opens again");
        Assert.IsTrue(ScannerLid.Open(false, false, Idle - 0.01f, Idle), "and stays open a while");
        Assert.IsFalse(ScannerLid.Open(false, false, Idle, Idle), "then shuts by itself");
        Assert.IsTrue(ScannerLid.Open(false, true, Idle * 3f, Idle), "a new paper dragged near opens it again");
    }

    [Test]
    public void Near_IsTheDropAreaGrownByTheMargin()
    {
        Assert.IsTrue(ScannerLid.Near(0f, 0f, 0.4f, 0.32f, 0.08f));
        Assert.IsTrue(ScannerLid.Near(0.27f, 0.23f, 0.4f, 0.32f, 0.08f), "within the margin past the corner");
        Assert.IsFalse(ScannerLid.Near(0.29f, 0f, 0.4f, 0.32f, 0.08f), "past the margin on the side");
        Assert.IsFalse(ScannerLid.Near(0f, -0.25f, 0.4f, 0.32f, 0.08f), "past the margin in front");
    }

    [Test]
    public void TheReadout_CountsFrom000To100()
    {
        Assert.AreEqual(0, ScannerLid.Count(-1f));
        Assert.AreEqual(0, ScannerLid.Count(0f));
        Assert.AreEqual(42, ScannerLid.Count(0.425f));
        Assert.AreEqual(99, ScannerLid.Count(0.999f));
        Assert.AreEqual(100, ScannerLid.Count(1f));
        Assert.AreEqual(100, ScannerLid.Count(1.3f), "never past 100");
    }

    [Test]
    public void TheScannerArt_IsUsedOnlyWithEveryPartOfItsContract()
    {
        var all = new System.Collections.Generic.HashSet<string>(PropArt.Scanner);
        Assert.IsTrue(PropArt.UseArt(true, PropArt.Scanner, all.Contains));
        all.Remove("Lid");
        Assert.IsFalse(PropArt.UseArt(true, PropArt.Scanner, all.Contains), "no lid: the built stand-in");
        CollectionAssert.AreEqual(new[] { "Lid" }, PropArt.Missing(PropArt.Scanner, all.Contains));
    }
}
