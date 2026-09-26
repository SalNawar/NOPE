using NUnit.Framework;

/// <summary>
/// The current traveller's papers as the Investigation app counts them (the PC
/// redesign AP1's counters, AP6's chips): each is not handed over, on the
/// desk, or scanned; each step happens once; a paper that reaches the PC
/// without a desk (scanned at once) counts as received.
/// </summary>
public class CasePapersTests
{
    [Test]
    public void ANewCase_HasEveryPaperNotHandedOver()
    {
        var papers = new CasePapers(3);
        Assert.AreEqual(3, papers.Count);
        for (int i = 0; i < 3; i++)
            Assert.AreEqual(PaperState.NotHandedOver, papers.State(i));
        Assert.AreEqual(0, papers.Received);
        Assert.AreEqual(0, papers.Scanned);
    }

    [Test]
    public void HandOver_PutsThePaperOnTheDesk_Once()
    {
        var papers = new CasePapers(2);
        Assert.IsTrue(papers.HandOver(1));
        Assert.AreEqual(PaperState.OnDesk, papers.State(1));
        Assert.IsFalse(papers.HandOver(1));
        Assert.AreEqual(1, papers.Received);
        Assert.AreEqual(0, papers.Scanned);
    }

    [Test]
    public void Scan_OfAPaperOnTheDesk_MakesItScanned_Once()
    {
        var papers = new CasePapers(2);
        papers.HandOver(0);
        Assert.IsTrue(papers.Scan(0));
        Assert.AreEqual(PaperState.Scanned, papers.State(0));
        Assert.IsFalse(papers.Scan(0));
        Assert.AreEqual(1, papers.Received);
        Assert.AreEqual(1, papers.Scanned);
    }

    [Test]
    public void Scan_WithoutADesk_CountsTheHandOverToo()
    {
        var papers = new CasePapers(2);
        Assert.IsTrue(papers.Scan(1));
        Assert.AreEqual(PaperState.Scanned, papers.State(1));
        Assert.AreEqual(1, papers.Received);
        Assert.IsFalse(papers.HandOver(1), "a scanned paper is never handed over again");
        Assert.AreEqual(PaperState.Scanned, papers.State(1));
    }

    [TestCase(-1)]
    [TestCase(2)]
    public void AnIndexOutsideTheCase_ChangesNothing(int index)
    {
        var papers = new CasePapers(2);
        Assert.IsFalse(papers.HandOver(index));
        Assert.IsFalse(papers.Scan(index));
        Assert.AreEqual(PaperState.NotHandedOver, papers.State(index));
        Assert.AreEqual(0, papers.Received);
    }

    [Test]
    public void ACaseWithoutPapers_CountsNothing()
    {
        var papers = new CasePapers(0);
        Assert.AreEqual(0, papers.Count);
        Assert.AreEqual(0, papers.Received);
        Assert.AreEqual(0, new CasePapers(-3).Count);
    }
}
