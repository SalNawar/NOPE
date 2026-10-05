using NUnit.Framework;

/// <summary>
/// Papers never end up hidden behind the scanner (the desk-first redesign,
/// item 4): the blocked area is the scanner's footprint and the shadow its
/// body casts away from the camera; a paper reaching into it moves the
/// shortest way out to the left, right or front, kept on the desk, never
/// further back; the eject spot is in front of the scanner, else beside it.
/// </summary>
public class ScannerClearanceTests
{
    private const float Eps = 1e-4f;

    // The desk's clamp area: 2 m wide, 1 m deep (y away from the camera), centred on the origin.
    private static readonly DeskRect Desk = new DeskRect(0f, 0f, 2f, 1f);

    // The scanner on the desk's right, 0.4 x 0.3.
    private static readonly DeskRect Scanner = new DeskRect(0.6f, 0f, 0.4f, 0.3f);

    [Test]
    public void Shadow_IsTheBodysHeightOverTheElevationsTangent_Capped()
    {
        Assert.AreEqual(0.1f, ScannerClearance.Shadow(0.1f, 45f, 1f), Eps);
        Assert.AreEqual(0f, ScannerClearance.Shadow(0.1f, 90f, 1f), Eps, "straight down hides nothing behind");
        Assert.AreEqual(0.5f, ScannerClearance.Shadow(0.1f, 0f, 0.5f), Eps, "near level: the cap");
        Assert.AreEqual(0f, ScannerClearance.Shadow(0f, 30f, 1f), Eps);
    }

    [Test]
    public void APaperClearOfTheScanner_StaysWhereItIs()
    {
        var paper = new DeskRect(-0.3f, 0f, 0.26f, 0.34f);
        Assert.AreEqual((-0.3f, 0f), ScannerClearance.Clear(paper, Scanner, 0.2f, Desk));
    }

    [Test]
    public void APaperInTheShadowBehindTheScanner_IsMovedOut_NeverFurtherBack()
    {
        // Behind the scanner (y beyond its far edge 0.15), inside the 0.2 shadow.
        var paper = new DeskRect(0.6f, 0.3f, 0.26f, 0.2f);
        (float x, float y) = ScannerClearance.Clear(paper, Scanner, 0.2f, Desk);
        Assert.IsFalse(ScannerClearance.Overlaps(new DeskRect(x, y, 0.26f, 0.2f), ScannerClearance.Blocked(Scanner, 0.2f)));
        Assert.LessOrEqual(y, 0.3f + Eps, "never pushed further back");
        Assert.AreEqual(0.6f - 0.2f - ScannerClearance.Gap - 0.13f, x, Eps, "the left is the shortest way out on this desk");
    }

    [Test]
    public void APaperHalfOnTheScannersFront_GoesToTheFront()
    {
        var paper = new DeskRect(0.6f, -0.2f, 0.26f, 0.2f);
        (float x, float y) = ScannerClearance.Clear(paper, Scanner, 0.2f, Desk);
        Assert.AreEqual(0.6f, x, Eps);
        Assert.AreEqual(-0.15f - ScannerClearance.Gap - 0.1f, y, Eps);
    }

    [Test]
    public void TheMove_KeepsThePaperOnTheDesk()
    {
        // The scanner at the desk's right edge: the right is off the desk, so the paper goes left or front.
        var edge = new DeskRect(0.8f, 0f, 0.4f, 0.3f);
        var paper = new DeskRect(0.9f, 0.05f, 0.26f, 0.2f);
        (float x, float y) = ScannerClearance.Clear(paper, edge, 0.1f, Desk);
        Assert.IsTrue(Desk.Contains(x, y));
        Assert.IsFalse(ScannerClearance.Overlaps(new DeskRect(x, y, 0.26f, 0.2f), ScannerClearance.Blocked(edge, 0.1f)));
    }

    [Test]
    public void TheEjectSpot_IsInFrontOfTheScanner_ElseBesideIt()
    {
        (float x, float y) = ScannerClearance.Eject(0.26f, 0.2f, Scanner, Desk);
        Assert.AreEqual(0.6f, x, Eps);
        Assert.AreEqual(-0.15f - ScannerClearance.Gap - 0.1f, y, Eps);

        // A scanner at the desk's near edge: no room in front, so it ejects to its left.
        var near = new DeskRect(0.6f, -0.35f, 0.4f, 0.3f);
        (x, y) = ScannerClearance.Eject(0.26f, 0.2f, near, Desk);
        Assert.IsFalse(ScannerClearance.Overlaps(new DeskRect(x, y, 0.26f, 0.2f), near));
        Assert.Less(x, 0.4f);
    }
}
