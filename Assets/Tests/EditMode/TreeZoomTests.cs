using NUnit.Framework;

/// <summary>The Orders tree's zoom and pan (TreeZoom): the readable levels, the reset level, the zoom about the pointer, the scroll kept inside the tree and a node brought into view.</summary>
public class TreeZoomTests
{
    private static readonly int[] Levels = { 50, 75, 100, 125, 150, 200 };

    [Test]
    public void ReadableLevels_DropTheLevelsThatPutTheSmallestTextUnderTheFloor()
    {
        // The node's state line at its 16 u floor: nothing below 100 % reads.
        CollectionAssert.AreEqual(new[] { 100, 125, 150, 200 }, TreeZoom.ReadableLevels(Levels, 16f, 16f));
        // A 24 u line could zoom out to two thirds: 75 % keeps it at 18 u.
        CollectionAssert.AreEqual(new[] { 75, 100, 125, 150, 200 }, TreeZoom.ReadableLevels(Levels, 24f, 16f));
    }

    [Test]
    public void ReadableLevels_NoneReadable_TheLevelWhereItJustReads()
    {
        CollectionAssert.AreEqual(new[] { 160 }, TreeZoom.ReadableLevels(new[] { 100, 125, 150 }, 10f, 16f));
        CollectionAssert.AreEqual(new[] { 100 }, TreeZoom.ReadableLevels(null, 16f, 16f));
    }

    [Test]
    public void LeastReadable_AFloorMetExactly_IsNotRoundedUp()
    {
        Assert.AreEqual(100, TreeZoom.LeastReadable(16f, 16f));
        Assert.AreEqual(67, TreeZoom.LeastReadable(24f, 16f));
        Assert.AreEqual(100, TreeZoom.LeastReadable(0f, 16f), "no sizes: 100 %");
    }

    [Test]
    public void ResetLevel_OneHundredWhenReadable_ElseTheLowest()
    {
        Assert.AreEqual(100, TreeZoom.ResetLevel(new[] { 75, 100, 150 }));
        Assert.AreEqual(125, TreeZoom.ResetLevel(new[] { 150, 125, 200 }));
        Assert.AreEqual(100, TreeZoom.ResetLevel(new int[0]));
    }

    [Test]
    public void ZoomAbout_KeepsTheSpotUnderThePointer()
    {
        // Scrolled 100 in, pointer 200 into the view: the tree's spot 300 (at 1x) sits under it.
        float scroll = TreeZoom.ZoomAbout(100f, 200f, 1f, 1.5f);
        Assert.AreEqual(250f, scroll, 1e-4f);
        Assert.AreEqual(300f * 1.5f, scroll + 200f, 1e-4f, "the same spot, zoomed, is still under the pointer");
        // And back out again returns to the start.
        Assert.AreEqual(100f, TreeZoom.ZoomAbout(scroll, 200f, 1.5f, 1f), 1e-4f);
    }

    [Test]
    public void ClampScroll_StaysInsideTheTree()
    {
        Assert.AreEqual(0f, TreeZoom.ClampScroll(-40f, 1000f, 600f));
        Assert.AreEqual(400f, TreeZoom.ClampScroll(900f, 1000f, 600f));
        Assert.AreEqual(250f, TreeZoom.ClampScroll(250f, 1000f, 600f));
        Assert.AreEqual(0f, TreeZoom.ClampScroll(250f, 500f, 600f), "a tree that fits does not scroll");
    }

    [Test]
    public void Reveal_TheLeastScrollThatShowsTheNode()
    {
        Assert.AreEqual(100f, TreeZoom.Reveal(100f, 200f, 300f, 600f, 10f), "already in view: no scroll");
        Assert.AreEqual(210f, TreeZoom.Reveal(100f, 600f, 800f, 600f, 10f), "below: its end comes into view");
        Assert.AreEqual(40f, TreeZoom.Reveal(100f, 50f, 150f, 600f, 10f), "above: its start comes into view");
        Assert.AreEqual(490f, TreeZoom.Reveal(0f, 500f, 1200f, 600f, 10f), "longer than the view: its start");
    }
}
