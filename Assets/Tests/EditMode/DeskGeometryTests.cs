using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The desk's geometry: the rectangle papers stay in (a 4 x 2 rectangle
/// centred on (1, -1): x -1..3, y -2..0), the window clamp,
/// the paper stack and the sorting bands.
/// </summary>
public class DeskGeometryTests
{
    private static readonly DeskRect Desk = new DeskRect(1f, -1f, 4f, 2f);

    // -----------------------------
    // DeskRect
    // -----------------------------

    [Test]
    public void Contains_IncludesTheEdges_AndNothingOutside()
    {
        Assert.IsTrue(Desk.Contains(1f, -1f), "centre");
        Assert.IsTrue(Desk.Contains(-1f, -2f), "bottom-left corner");
        Assert.IsTrue(Desk.Contains(3f, 0f), "top-right corner");
        Assert.IsFalse(Desk.Contains(-1.01f, -1f), "left");
        Assert.IsFalse(Desk.Contains(3.01f, -1f), "right");
        Assert.IsFalse(Desk.Contains(1f, -2.01f), "below");
        Assert.IsFalse(Desk.Contains(1f, 0.01f), "above");
    }

    [Test]
    public void Clamp_MovesEachSideIn_AndLeavesAPointInside()
    {
        Assert.AreEqual((-1f, -1f), Desk.Clamp(-5f, -1f), "left");
        Assert.AreEqual((3f, -1f), Desk.Clamp(9f, -1f), "right");
        Assert.AreEqual((1f, -2f), Desk.Clamp(1f, -7f), "below");
        Assert.AreEqual((1f, 0f), Desk.Clamp(1f, 4f), "above");
        Assert.AreEqual((3f, 0f), Desk.Clamp(8f, 8f), "a corner");
        Assert.AreEqual((0.5f, -1.5f), Desk.Clamp(0.5f, -1.5f), "inside");
    }

    [Test]
    public void PointAt_MapsZeroToOne_FromTheBottomLeft_ClampingUV()
    {
        Assert.AreEqual((-1f, -2f), Desk.PointAt(0f, 0f));
        Assert.AreEqual((3f, 0f), Desk.PointAt(1f, 1f));
        Assert.AreEqual((1f, -1f), Desk.PointAt(0.5f, 0.5f));
        Assert.AreEqual((0f, -1.5f), Desk.PointAt(0.25f, 0.25f));
        Assert.AreEqual((-1f, 0f), Desk.PointAt(-3f, 7f), "u and v are clamped to 0..1");
    }

    [Test]
    public void NegativeSizes_CountAsZero()
    {
        var point = new DeskRect(2f, 3f, -4f, -1f);
        Assert.IsTrue(point.Contains(2f, 3f));
        Assert.IsFalse(point.Contains(2.5f, 3f));
        Assert.AreEqual((2f, 3f), point.Clamp(-10f, 10f));
        Assert.AreEqual((2f, 3f), point.PointAt(1f, 0f));
    }

    // -----------------------------
    // RectClamp
    // -----------------------------

    [Test]
    public void Shift_IsZeroInside_AndMovesASpanBackOnEachSide()
    {
        Assert.AreEqual(0f, RectClamp.Shift(-2f, 2f, -10f, 10f, false), "inside");
        Assert.AreEqual(0f, RectClamp.Shift(-10f, 10f, -10f, 10f, true), "exactly fitting");
        Assert.AreEqual(3f, RectClamp.Shift(-13f, -9f, -10f, 10f, false), "past the low side");
        Assert.AreEqual(-4f, RectClamp.Shift(8f, 14f, -10f, 10f, false), "past the high side");
        Assert.AreEqual(5f, RectClamp.Shift(-15f, 0f, -10f, 10f, true), "below, keepMax only matters when oversized");
        Assert.AreEqual(-2f, RectClamp.Shift(0f, 12f, -10f, 10f, true), "above");
    }

    [Test]
    public void Shift_AnOversizedSpan_AlignsItsMaxWithKeepMax_ElseItsMin()
    {
        Assert.AreEqual(5f, RectClamp.Shift(-20f, 5f, -10f, 10f, true), "keepMax: max to hi (a window's title bar stays visible)");
        Assert.AreEqual(10f, RectClamp.Shift(-20f, 5f, -10f, 10f, false), "otherwise: min to lo");
        Assert.AreEqual(-30f, RectClamp.Shift(10f, 40f, -10f, 10f, true));
        Assert.AreEqual(-20f, RectClamp.Shift(10f, 40f, -10f, 10f, false));
    }

    // -----------------------------
    // PaperStack
    // -----------------------------

    [Test]
    public void AddPutsOnTop_ReAddMoves_BringToFrontMoves_ClearEmpties()
    {
        var stack = new PaperStack();
        stack.Add(4);
        stack.Add(1);
        stack.Add(7);
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, new[] { stack.IndexOf(4), stack.IndexOf(1), stack.IndexOf(7) });

        stack.Add(4);
        CollectionAssert.AreEqual(new[] { 2, 0, 1 }, new[] { stack.IndexOf(4), stack.IndexOf(1), stack.IndexOf(7) }, "re-adding moves it to the top");

        stack.BringToFront(1);
        CollectionAssert.AreEqual(new[] { 1, 2, 0 }, new[] { stack.IndexOf(4), stack.IndexOf(1), stack.IndexOf(7) });

        stack.BringToFront(9);
        Assert.AreEqual(-1, stack.IndexOf(9), "bringing an absent paper forward adds nothing");

        stack.Clear();
        Assert.AreEqual(-1, stack.IndexOf(4));
        Assert.AreEqual(-1, stack.IndexOf(1));
    }

    // -----------------------------
    // SortingBands
    // -----------------------------

    /// <summary>The default bands: props up to 6, exit 10, glass 11, bezel 12, screen 20, papers from 30 (2 of them), held 60.</summary>
    private static List<string> Bands(int props = 6, int exit = 10, int glass = 11, int bezel = 12, int screen = 20, int paper = 30, int papers = 2, int held = 60) =>
        SortingBands.Problems(props, exit, glass, bezel, screen, paper, papers, held);

    private static void OneProblemNaming(List<string> problems, string upper, string lower, int value)
    {
        Assert.AreEqual(1, problems.Count, string.Join(" | ", problems));
        StringAssert.Contains(upper, problems[0]);
        StringAssert.Contains(lower, problems[0]);
        StringAssert.Contains(value.ToString(), problems[0]);
    }

    [Test]
    public void TheDefaultBands_HaveNoProblem()
    {
        CollectionAssert.IsEmpty(Bands());
    }

    [Test]
    public void EachBandEqualToTheOneBelow_IsOneProblemNamingBoth()
    {
        OneProblemNaming(Bands(exit: 6), "focus exit zone", "highest prop", 6);
        OneProblemNaming(Bands(glass: 10), "glass zone", "focus exit zone", 10);
        OneProblemNaming(Bands(bezel: 11), "bezel", "glass zone", 11);
        OneProblemNaming(Bands(screen: 12), "screen canvas", "bezel", 12);
        OneProblemNaming(Bands(paper: 20), "paper base", "screen canvas", 20);
    }

    [Test]
    public void TheHeldPaper_MustSitAboveEveryStackedPaper()
    {
        CollectionAssert.IsEmpty(Bands(paper: 30, papers: 4, held: 34));
        OneProblemNaming(Bands(paper: 30, papers: 4, held: 33), "held paper", "top paper", 33);
        OneProblemNaming(Bands(paper: 30, papers: 2, held: 30), "held paper", "top paper", 31);
    }
}
