using NUnit.Framework;

/// <summary>Showing a list a page at a time (the paged windows and the Home shop, piece 9 T9).</summary>
public class PagingTests
{
    [TestCase(0, 1)]
    [TestCase(1, 1)]
    [TestCase(6, 1)]
    [TestCase(7, 2)]
    [TestCase(12, 2)]
    [TestCase(13, 3)]
    public void PageCount_AtSixPerPage_IsAtLeastOne(int count, int pages)
    {
        Assert.AreEqual(pages, Paging.PageCount(count, 6));
    }

    [Test]
    public void APerPageBelowOne_CountsAsOne()
    {
        Assert.AreEqual(3, Paging.PageCount(3, 0));
        Assert.AreEqual(3, Paging.PageCount(3, -2));
        Assert.AreEqual(2, Paging.First(2, 3, 0));
    }

    [Test]
    public void Clamp_KeepsThePageInRange()
    {
        Assert.AreEqual(0, Paging.Clamp(-1, 13, 6));
        Assert.AreEqual(1, Paging.Clamp(1, 13, 6));
        Assert.AreEqual(2, Paging.Clamp(7, 13, 6));
        Assert.AreEqual(0, Paging.Clamp(3, 0, 6), "an empty list has one page");
    }

    [Test]
    public void FirstAndEnd_CoverThePage_TheLastOnePartly()
    {
        Assert.AreEqual(0, Paging.First(0, 13, 6));
        Assert.AreEqual(6, Paging.End(0, 13, 6));
        Assert.AreEqual(6, Paging.First(1, 13, 6));
        Assert.AreEqual(12, Paging.End(1, 13, 6));
        Assert.AreEqual(12, Paging.First(2, 13, 6));
        Assert.AreEqual(13, Paging.End(2, 13, 6));
        Assert.AreEqual(12, Paging.First(9, 13, 6), "the page is clamped first");
        Assert.AreEqual(0, Paging.End(0, 0, 6), "an empty list shows nothing");
    }

    [Test]
    public void PageOf_ThePageAnItemIsOn()
    {
        Assert.AreEqual(0, Paging.PageOf(0, 6));
        Assert.AreEqual(0, Paging.PageOf(5, 6));
        Assert.AreEqual(1, Paging.PageOf(6, 6));
        Assert.AreEqual(2, Paging.PageOf(12, 6));
        Assert.AreEqual(0, Paging.PageOf(-3, 6), "before the first item: the first page");
        Assert.AreEqual(4, Paging.PageOf(4, 0), "a per-page count below 1 counts as 1");
    }
}
