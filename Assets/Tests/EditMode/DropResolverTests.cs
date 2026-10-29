using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Where a dropped paper rests (DOC-HANDLE, BUG-6 and BUG-8; Saleh's note 25;
/// FOLD-01, Saleh's playtest of 2026-10-28, point 4): DropResolver keeps it
/// exactly where it was released unless part of it would lie out of view,
/// off the desk or over a machine or an object standing on the desk; then it
/// moves the least it can, clear of every keep-out at once; where nothing is
/// clear, the nearest place that overlaps less, never the least overlap far
/// away; a refusal rests just beside what refused it; the folder obeys the
/// same rule; the footprint, not the pointer, decides the dock; and the final
/// size is what is judged. The FOLD-01 cases use the desk the fold_rest
/// scenario measured.
/// </summary>
public class DropResolverTests
{
    private const float Eps = 1e-5f;

    /// <summary>The desk the reading view shows: 1.2 m wide, 0.8 m deep, centred on the desk.</summary>
    private static readonly DeskRect Area = new DeskRect(0f, 0f, 1.2f, 0.8f);

    [Test]
    public void ADrop_RestsExactlyWhereItWasReleased()
    {
        var paper = new DeskRect(0.13f, -0.07f, 0.26f, 0.34f);
        var keepOut = new List<DeskRect> { new DeskRect(0.45f, 0.2f, 0.2f, 0.2f) };
        Assert.AreEqual((0.13f, -0.07f), DropResolver.Rest(paper, Area, keepOut), "nothing in the way: not a millimetre of move");
        Assert.AreEqual((0.13f, -0.07f), DropResolver.Rest(paper, Area, null), "with no keep-outs at all");
    }

    [Test]
    public void ADropPartlyOutOfView_RestsWhollyInside_TheLeastMove()
    {
        var paper = new DeskRect(0.55f, -0.3f, 0.26f, 0.34f); // its right edge at 0.68 and its bottom at -0.47: past the area's 0.6 and -0.4
        (float x, float y) = DropResolver.Rest(paper, Area, null);
        Assert.AreEqual(0.6f - 0.13f, x, Eps, "its right edge on the area's");
        Assert.AreEqual(-0.4f + 0.17f, y, Eps, "its bottom edge on the area's");
        (float wx, _) = DropResolver.Inside(new DeskRect(0.3f, 0f, 2f, 0.3f), Area);
        Assert.AreEqual(0f, wx, Eps, "a paper wider than the area is centred on it");
    }

    [Test]
    public void ADropOverAProp_MovesTheLeastWayClearOfIt()
    {
        var prop = new DeskRect(0.3f, 0f, 0.2f, 0.2f); // x 0.2..0.4
        var paper = new DeskRect(0.12f, 0f, 0.2f, 0.2f); // x 0.02..0.22: 2 cm over the prop's left edge
        (float x, float y) = DropResolver.Rest(paper, Area, new List<DeskRect> { prop });
        Assert.AreEqual(0.2f - DropResolver.Gap - 0.1f, x, Eps, "just left of it: the shortest way out");
        Assert.AreEqual(0f, y, Eps, "and no other move");
        Assert.AreEqual(0f, DropResolver.Overlap(new DeskRect(x, y, 0.2f, 0.2f), new List<DeskRect> { prop }), "never under a prop");
    }

    [Test]
    public void TheKeepOuts_AreSolvedTogether_NeverOnePushIntoAnother()
    {
        // Pushed out of the right prop the short way (left), a one-at-a-time push landed in the left prop (BUG-6).
        var left = new DeskRect(-0.05f, 0f, 0.2f, 0.4f);
        var right = new DeskRect(0.3f, 0f, 0.2f, 0.4f);
        var paper = new DeskRect(0.16f, 0f, 0.2f, 0.2f);
        var keepOut = new List<DeskRect> { left, right };
        (float x, float y) = DropResolver.Rest(paper, Area, keepOut);
        var rest = new DeskRect(x, y, 0.2f, 0.2f);
        Assert.AreEqual(0f, DropResolver.Overlap(rest, keepOut), "clear of both at once");
        Assert.LessOrEqual(System.Math.Abs(x - 0.16f) + System.Math.Abs(y), 0.35f, "and still the nearest clear place");
    }

    [Test]
    public void ARefusalOnTheCounter_RestsJustOffIt_NeverFlyingBack()
    {
        // A desk paper dropped on the counter before the verdict: the counter (y from 0.3 on) is a keep-out; it rests just nearer.
        var counter = new DeskRect(0f, 0.3f + 50f, 100f, 100f);
        var paper = new DeskRect(-0.2f, 0.25f, 0.26f, 0.34f); // released with its top 12 cm over the counter
        (float x, float y) = DropResolver.Rest(paper, Area, new List<DeskRect> { counter });
        Assert.AreEqual(-0.2f, x, Eps, "in place across");
        Assert.AreEqual(0.3f - DropResolver.Gap - 0.17f, y, Eps, "just off the counter's near edge: about 12 cm, not back to where it was picked up");
    }

    [Test]
    public void TheFolder_ObeysTheSameRule()
    {
        // The rulebook folder dropped over the scanner's casing rests just beside it, as a paper would.
        var scanner = new DeskRect(0.4f, 0.1f, 0.3f, 0.25f);
        var folder = new DeskRect(0.3f, 0.1f, 0.36f, 0.27f);
        (float x, float y) = DropResolver.Rest(folder, Area, new List<DeskRect> { scanner });
        Assert.AreEqual(0f, DropResolver.Overlap(new DeskRect(x, y, 0.36f, 0.27f), new List<DeskRect> { scanner }));
        var inView = new DeskRect(x, y, 0.36f, 0.27f);
        Assert.AreEqual((x, y), DropResolver.Inside(inView, Area), "and wholly in view");
    }

    [Test]
    public void TheFootprint_NotThePointer_DecidesTheDock()
    {
        var reader = new DeskRect(0.3f, 0f, 0.26f, 0.26f);
        var scanner = new DeskRect(-0.3f, 0f, 0.3f, 0.3f);
        var docks = new List<DeskRect> { reader, scanner };
        // DeskController.DockAt calls DockOf with the paper's footprint (DeskDocument.RestCorners at its root) and each machine's own drop-area test.
        int Dock(DeskRect paper) => DropResolver.DockOf(paper, docks, (d, x, y) => d.Contains(x, y));
        // Grabbed by its left edge, the pointer at x 0.1 is off the reader, but the paper's centre at x 0.25 lies on it.
        Assert.AreEqual(0, Dock(new DeskRect(0.25f, 0f, 0.26f, 0.34f)), "the paper's centre lies on the reader");
        // Grabbed by its right edge over the reader, the paper itself hangs off it: no dock.
        Assert.AreEqual(-1, Dock(new DeskRect(0.05f, 0.3f, 0.26f, 0.34f)));
        Assert.AreEqual(1, Dock(new DeskRect(-0.32f, 0.02f, 0.1f, 0.1f)));
        Assert.AreEqual(-1, DropResolver.DockOf(new DeskRect(0f, 0f, 1f, 1f), (List<DeskRect>)null, (d, x, y) => true));
        Assert.AreEqual(-1, DropResolver.DockOf(new DeskRect(0f, 0f, 1f, 1f), docks, null));
    }

    [Test]
    public void WhereTwoDropAreasMeet_TheFirstListed_TakesIt()
    {
        // DeskController.DockAt lists the chip reader first: where its drop area meets the scanner's, the reader takes the paper.
        var reader = new DeskRect(0.1f, 0f, 0.3f, 0.3f);
        var scanner = new DeskRect(-0.1f, 0f, 0.3f, 0.3f);
        var tried = new List<int>();
        int dock = DropResolver.DockOf(new DeskRect(0f, 0f, 0.2f, 0.2f), new List<DeskRect> { reader, scanner }, (d, x, y) =>
        {
            tried.Add(tried.Count);
            return d.Contains(x, y);
        });
        Assert.AreEqual(0, dock, "both hold the centre: the first listed wins");
        Assert.AreEqual(1, tried.Count, "and the rest are not tried");
        Assert.AreEqual(1, DropResolver.DockOf(new DeskRect(0f, 0f, 0.2f, 0.2f), new List<DeskRect> { new DeskRect(5f, 5f, 1f, 1f), scanner }, (d, x, y) => d.Contains(x, y)));
    }

    [Test]
    public void TheFinalSize_IsWhatIsJudged()
    {
        // A counter paper (0.16 by 0.2) dropped on the desk grows to its desk size (0.26 by 0.34) under the cursor first:
        // at its counter size it would clear the prop; at the size it rests at it does not, so it is judged at that size.
        var prop = new DeskRect(0.3f, 0f, 0.2f, 0.2f);
        var keepOut = new List<DeskRect> { prop };
        Assert.AreEqual(0f, DropResolver.Overlap(new DeskRect(0.1f, 0f, 0.16f, 0.2f), keepOut), "the counter size clears it");
        (float x, _) = DropResolver.Rest(new DeskRect(0.1f, 0f, 0.26f, 0.34f), Area, keepOut);
        Assert.AreEqual(0.2f - DropResolver.Gap - 0.13f, x, Eps, "the desk size does not: it rests clear at that size");
    }

    [Test]
    public void WhereNothingIsClear_ItRestsWhereItOverlapsLeast()
    {
        var everything = new DeskRect(0f, 0f, 1.2f, 0.8f);
        var paper = new DeskRect(0.1f, 0.1f, 0.26f, 0.34f);
        (float x, float y) = DropResolver.Rest(paper, Area, new List<DeskRect> { everything });
        Assert.AreEqual((x, y), DropResolver.Inside(new DeskRect(x, y, 0.26f, 0.34f), Area), "still wholly in view");
    }

    // ---------------- FOLD-01 (Saleh's playtest of 2026-10-28, point 4: "You move the folder and it snaps somewhere else") ----------------
    // The desk measured by the fold_rest scenario (run fold_rest_20261029_000835, rests.tsv), day 1, 1920x1080, in DeskController's
    // rest frame (the desk's own axes, metres from the desk surface's centre: x right, y away from the chair). The shut folder's footprint
    // is 0.2249 by 0.2588 m.

    private const float FolderW = 0.2249f, FolderH = 0.2588f;

    /// <summary>The reading view's corners on the desk (near left, near right, far left, far right), taken in the frame the view is posed on.</summary>
    private static readonly (float x, float y)[] ViewCorners = { (-1.449f, -0.592f), (0.921f, -0.592f), (-1.639f, 0.840f), (1.112f, 0.840f) };

    /// <summary>The same view's corners as the old code took them: in the frame of the office camera's line to the scanner, 27 degrees off (run fold_probe_20261028_233226).</summary>
    private static readonly (float x, float y)[] TurnedCorners = { (-1.017f, -1.189f), (1.090f, -0.105f), (-1.842f, -0.003f), (0.604f, 1.256f) };

    /// <summary>Where the back step begins (DeskPaintedMap.BackStep): the desk ends there.</summary>
    private const float BackStep = DeskPaintedMap.BackStep;

    /// <summary>The desk a folder's centre may reach (the surface's clamp area grown by the folder).</summary>
    private static DeskRect FolderDesk => new DeskRect(0f, 0f, 1.9925f + FolderW, 1.015f + FolderH);

    /// <summary>The desk's frame (DeskLayout.Frame, the bake's) to the rest's frame: the desk surface's centre lies 0.27375 m right of the art mat's centre (the stamp rack at 0.9625 in the bake, 0.6888 here).</summary>
    private const float FrameToRest = -0.27375f;

    /// <summary>What stands on the desk on day 1 (DeskLayout.Standing: each object at its own footprint, the painted trays of the machine rail, what the shell draws beside the mat).</summary>
    private static List<DeskRect> Day1Standing()
    {
        var standing = new List<DeskRect>
        {
            new DeskRect(-0.6188f, 0.6900f, 0.2300f, 0.1800f), // the AVAILABLE sign
            new DeskRect(-1.7988f, 0.1500f, 0.6500f, 0.5400f), // the PC
            new DeskRect(-1.8038f, -0.4000f, 0.6200f, 0.2400f), // the keyboard
            new DeskRect(-1.4992f, -0.2766f, 0.1938f, 0.4569f), // the PC's mouse
            new DeskRect(0.6888f, 0.4375f, 0.2950f, 0.2150f), // the stamp rack
            new DeskRect(-0.8538f, 0.7300f, 0.2000f, 0.2600f), // DETAIN
            new DeskRect(-0.2738f, 0.9000f, 0.4200f, 0.6000f), // the deal tray
            new DeskRect(-1.2188f, 0.3500f, 0.2900f, 0.2200f), // the intercom
            new DeskRect(-1.1138f, 0.7400f, 0.2400f, 0.2400f), // the glasses
            new DeskRect(0.0763f, 0.7400f, 0.2000f, 0.2000f), // the bell
            new DeskRect(-1.2188f, -0.1950f, 0.3100f, 0.2100f), // the red book's pocket
            new DeskRect(0.6963f, -0.4000f, 0.2600f, 0.2600f), // the chip reader's tray
            new DeskRect(0.7963f, -0.0200f, 0.4600f, 0.2400f), // the scanner's tray
        };
        standing.AddRange(Shell());
        return standing;
    }

    /// <summary>What the shell draws beside the mat (DeskPaintedMap.ShellStanding: the machine rail, the tool pods), in the rest's frame.</summary>
    private static List<DeskRect> Shell()
    {
        var shell = new List<DeskRect>();
        foreach (var (_, x0, y0, x1, y1, _) in DeskPaintedMap.ShellStanding)
            shell.Add(new DeskRect((x0 + x1) / 2f + FrameToRest, (y0 + y1) / 2f, x1 - x0, y1 - y0));
        return shell;
    }

    /// <summary>Where the folder let go with its centre at (<paramref name="x"/>, <paramref name="y"/>) rests on day 1's desk.</summary>
    private static (float x, float y) Day1Rest(float x, float y) =>
        DropResolver.Rest(new DeskRect(x, y, FolderW, FolderH), DropResolver.Area(FolderDesk, BackStep, ViewCorners), Day1Standing());

    [Test]
    public void TheReadingViewsCorners_InTheFrameItIsPosedOn_LeaveTheWholeView_InATurnedFrame_ABandShallowerThanTheFolder()
    {
        // The cause of FOLD-01: the rest was judged along the office camera's line to the scanner. The view's corners in that frame
        // left a band 10 cm deep, shallower than the folder, so every rest's depth went to the band's middle (Axis).
        DeskRect turned = DropResolver.Area(FolderDesk, float.PositiveInfinity, TurnedCorners);
        Assert.Less(turned.Height, FolderH, "in the turned frame the area is a band shallower than the folder");
        Assert.AreEqual(-0.054f, DropResolver.Rest(new DeskRect(-0.7509f, -0.2283f, 0.3184f, 0.3330f), turned, null).y, 1e-3f,
                        "the audit's drag 2 then rested at the band's middle (its measured rest, 125 px from the release)");
        DeskRect area = DropResolver.Area(FolderDesk, BackStep, ViewCorners);
        Assert.AreEqual(-0.592f, area.CentreY - area.Height / 2f, 1e-4f, "in the desk's frame: from the view's near edge");
        Assert.AreEqual(BackStep, area.CentreY + area.Height / 2f, 1e-4f, "to the back step's front");
        Assert.AreEqual(-(1.9925f + FolderW) / 2f, area.CentreX - area.Width / 2f, 1e-4f, "from the drag's clamp on the left (the view reaches further)");
        Assert.AreEqual(0.921f, area.CentreX + area.Width / 2f, 1e-4f, "to the view's right edge");
        Assert.AreEqual(FolderDesk.Width, DropResolver.Area(FolderDesk, 99f, new[] { (5f, 5f), (6f, 5f), (5f, 6f), (6f, 6f) }).Width, 1e-5f,
                        "a view off the desk leaves nothing: the desk alone");
    }

    [Test]
    public void TheMatsFreeMiddle_RestsWhereLetGo()
    {
        Assert.AreEqual((-0.1568f, -0.0001f), Day1Rest(-0.1568f, -0.0001f), "case (a)");
    }

    [Test]
    public void TheMatsLeftPart_ByTheRedBook_RestsWhereLetGo()
    {
        Assert.AreEqual((-0.8486f, -0.1987f), Day1Rest(-0.8486f, -0.1987f), "case (b): clear of the red book's pocket and the intercom");
    }

    [Test]
    public void TheAuditsDragTwo_RestsWhereLetGo()
    {
        // Let go at (576,486) top-left at 1920x1080: the folder rested 125 px away, on the mat's top rail and slot.
        Assert.AreEqual((-0.7790f, 0.1341f), Day1Rest(-0.7790f, 0.1341f), "case (c)");
    }

    [Test]
    public void LetGoOverDetain_ItTakesTheLeastMoveOntoTheDesk_ClearOfDetainAndTheBackStep()
    {
        // Case (d), Saleh point 2b ("folder clipping detain button"): its top over DETAIN and the sign on the back step.
        (float x, float y) = Day1Rest(-0.8301f, 0.5005f);
        Assert.AreEqual(-0.8301f, x, Eps, "no move across");
        Assert.AreEqual(BackStep - FolderH / 2f, y, Eps, "just onto the desk: its top on the back step's front, the least move (8 cm, about 55 px)");
        Assert.AreEqual(0f, DropResolver.Overlap(new DeskRect(x, y, FolderW, FolderH), Day1Standing()), "clear of DETAIN, the sign and the rest");
    }

    [Test]
    public void LetGoPartlyOffTheViewsEdge_ItTakesTheLeastMoveInward()
    {
        // Case (e): the drag holds the centre at the clamp's near edge (-0.5075); its bottom edge lies past the view's rest margin.
        (float x, float y) = Day1Rest(0.0225f, -0.5075f);
        Assert.AreEqual(0.0225f, x, Eps);
        Assert.AreEqual(-0.592f + FolderH / 2f, y, Eps, "its bottom edge on the view's near edge: the least move inward (4.5 cm, 37 px)");
    }

    [Test]
    public void LetGoInAMachinesSlideStrip_ItRestsWhereLetGo_TheSliderNotTheFolderGivesWay()
    {
        // Day 12 (fold_rest's case i): the folder let go beside the chip reader, 2.7 cm from it, inside the 4.3 cm strip the reader
        // slides into. The reader's registered obstacle is its swept box; the rest keeps clear of its own footprint
        // (DeskSlides.Standing) and the slider stops short instead (SlideRoom, MachineSlideIn.Room).
        DeskSlides slides = DeskSlides.Approved();
        var reader = new DeskHome(DeskHomeMap.ChipReader, 0.6963f, -0.40f, 0.26f, 0.26f, 0f, 0.147f, HomeShow.WhenOwned, HomeSlide.Left);
        DeskRect swept = slides.Obstacle(reader, 0.147f).footprint;
        DeskRect standing = slides.Standing(reader, swept);
        var casing = new DeskRect(0.6963f, -0.4010f, 0.2600f, 0.2620f);
        var folder = new DeskRect(0.4273f, -0.3885f, FolderW, FolderH);
        DeskRect area = DropResolver.Area(FolderDesk, BackStep, ViewCorners);
        Assert.AreEqual(reader.Footprint.CentreX, standing.CentreX, Eps, "the reader stands on its own footprint");
        Assert.AreEqual(reader.Footprint.Width, standing.Width, Eps);
        Assert.AreEqual((0.4273f, -0.3885f), DropResolver.Rest(folder, area, new List<DeskRect> { casing, standing }), "rests where let go");
        (float sx, _) = DropResolver.Rest(folder, area, new List<DeskRect> { casing, swept });
        Assert.Less(sx, 0.4273f - 0.015f, "the swept box (the old keep-out) pushed it 1.7 cm toward the mat");
        var registered = new DeskRect(9f, 9f, 1f, 1f);
        DeskHome detain = DeskHomeMap.Find(DeskHomeMap.Approved(), DeskHomeMap.Detain);
        Assert.AreEqual(9f, slides.Standing(detain, registered).CentreX, Eps, "a home where nothing slides keeps what it registered");
        Assert.AreEqual(9f, slides.Standing(null, registered).CentreX, Eps, "and none keeps it too");
    }

    [Test]
    public void WhereNothingIsClear_ItTakesTheNearestPlaceThatOverlapsLess_NeverTheLeastOverlapFarAway()
    {
        // "When nothing clear is near, prefer the nearest place, not the least total overlap anywhere" (FOLD-01): the old rule took a
        // thing across the desk to the place of least overlap (here the far right corner's gap, 0.9 m away).
        var floorLeft = new DeskRect(-0.075f, 0f, 1.05f, 0.8f); // x -0.6..0.45, all y
        var floorRight = new DeskRect(0.525f, -0.075f, 0.15f, 0.65f); // x 0.45..0.6, y -0.4..0.25: a gap at the far right corner
        var prop = new DeskRect(-0.2f, 0f, 0.2f, 0.2f); // x -0.3..-0.1
        var keepOut = new List<DeskRect> { floorLeft, floorRight, prop };
        var paper = new DeskRect(-0.3f, 0f, 0.2f, 0.2f); // half over the prop
        (float x, float y) = DropResolver.Rest(paper, Area, keepOut);
        Assert.AreEqual(-0.3f - 0.1f - DropResolver.Gap, x, Eps, "the nearest place that overlaps less: just left of the prop");
        Assert.AreEqual(0f, y, Eps);
        Assert.Less(DropResolver.Overlap(new DeskRect(x, y, 0.2f, 0.2f), keepOut), DropResolver.Overlap(paper, keepOut), "it overlaps less than where it was let go");
        // With nothing better anywhere it stays where it was let go (moved inside the area only).
        var everything = new List<DeskRect> { new DeskRect(0f, 0f, 1.2f, 0.8f) };
        Assert.AreEqual((-0.3f, 0f), DropResolver.Rest(paper, Area, everything));
    }

    // ---------------- FOLD-01 round 2 (the verifier's F1): what the shell draws beside the mat ----------------

    [Test]
    public void LetGoHalfOverTheScannersTray_ItRestsClearOfTheWholeRail_NotUnderTheTraysRim()
    {
        // The verifier's a15 (1920x1080, day 1): the folder half over the scanner's tray. Kept clear of the tray's home alone, it
        // lay 5 to 6 cm under the tray's rim (its track cut toward the mat) and beside a lamp's bezel that drew over it.
        DeskRect rail = Shell()[0];
        float railLeft = rail.CentreX - rail.Width / 2f;
        (float x, float y) = Day1Rest(0.50f, -0.02f);
        Assert.AreEqual(railLeft - DropResolver.Gap - FolderW / 2f, x, Eps, "just left of the rail: the least move clear of its rims, lamps and bolts");
        Assert.AreEqual(-0.02f, y, Eps, "no move along the rail");
        List<DeskRect> homesOnly = Day1Standing().GetRange(0, 13);
        (float ox, _) = DropResolver.Rest(new DeskRect(0.50f, -0.02f, FolderW, FolderH), DropResolver.Area(FolderDesk, BackStep, ViewCorners), homesOnly);
        Assert.Greater(ox + FolderW / 2f, railLeft + 0.05f, "kept clear of the tray's home alone, its right edge lay over the rail: under the rim");
    }

    [Test]
    public void APaperLetGoOverTheIntercomsTrackFrame_RestsClearOfTheToolPods()
    {
        // The verifier's b05 and e21: a passport let go over the intercom rested with its left part under the intercom's track frame
        // (the intercom's footprint ends 6 cm left of the frame the shell draws round it).
        const float w = 0.26f, h = 0.18f;
        DeskRect area = DropResolver.Area(new DeskRect(0f, 0f, 1.9925f + w, 1.015f + h), BackStep, ViewCorners);
        var paper = new DeskRect(-0.99f, 0.35f, w, h);
        DeskRect pods = Shell()[1];
        float podsRight = pods.CentreX + pods.Width / 2f;
        (float x, float y) = DropResolver.Rest(paper, area, Day1Standing());
        Assert.AreEqual(podsRight + DropResolver.Gap + w / 2f, x, Eps, "just right of the pods' frames");
        Assert.AreEqual(0.35f, y, Eps);
        (float ox, _) = DropResolver.Rest(paper, area, Day1Standing().GetRange(0, 13));
        Assert.Less(ox - w / 2f, podsRight - 0.05f, "kept clear of the intercom's home alone, its left part lay under the frame");
    }
}
