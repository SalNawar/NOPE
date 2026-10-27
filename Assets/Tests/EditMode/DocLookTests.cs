using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// DOC-LOOK's pure rules (FIX_PLAN DOC-LOOK, BUG-9, 10c, 11b, 12a, 14c, 40c;
/// Saleh's D3 and D15): no word breaks across lines, a ruled page's lines,
/// the cap height on screen, the passport's cover (its swing, its bow, the
/// box a shut booklet or folder keeps and the marks that ride the bow), a
/// card that never bends, print that rides the curl and the sheet's lifted
/// corner, the contact shadow by stack rank, and its rebuild in place with no
/// allocation.
/// </summary>
public class DocLookTests
{
    private const float Eps = 1e-4f;

    /// <summary>A monospace measure: <paramref name="charWidth"/> per character.</summary>
    private static Func<string, float> Mono(float charWidth) => s => (s ?? string.Empty).Length * charWidth;

    // ---------------- TextFit (BUG-9, 37c) ----------------

    [Test]
    public void WordShare_FitsTheWidestWord_NeverUnderTheWordLeast()
    {
        Assert.AreEqual(1f, TextFit.WordShare(9f, 10f), Eps);
        Assert.AreEqual(10f / 11f * TextFit.WidthSlack, TextFit.WordShare(11f, 10f), Eps);
        Assert.AreEqual(TextFit.WordLeast, TextFit.WordShare(100f, 10f), Eps);
    }

    [Test]
    public void Unbroken_AWordTooWideIsCutWithAStop_NoWordBreaksAcrossLines()
    {
        Func<string, float> measure = Mono(1f);
        string laid = TextFit.Unbroken("KONSTANTINOPOULOS Anna", 10f, 1f, measure);
        Assert.AreEqual("KONSTANTI. Anna", laid);
        foreach (string word in TextFit.Words(laid))
            Assert.LessOrEqual(measure(word), 10f, word);
        Assert.AreEqual("short words stay", TextFit.Unbroken("short words stay", 10f, 1f, measure));
        Assert.AreEqual("AB CDEF.", TextFit.Unbroken("AB CDEFGHIJ", 5f, 1f, measure), "the room is the box over the share: 5 at full size");
        Assert.AreEqual("a <b>LONGERTHANTEN</b>", TextFit.Unbroken("a <b>LONGERTHANTEN</b>", 10f, 1f, measure), "a word holding a rich-text tag is never cut");
    }

    [Test]
    public void WrapWords_LinesNoWiderThanTheBox_WordsWhole_ParagraphsKept()
    {
        List<string> lines = TextFit.WrapWords("one two three four\nfive", 9f, Mono(1f));
        CollectionAssert.AreEqual(new[] { "one two", "three", "four", "five" }, lines);
        Assert.IsTrue(lines.All(l => l.Length <= 9 || !l.Contains(' ')));
    }

    [Test]
    public void IsLogicalArabic_UnshapedLettersOnly()
    {
        Assert.IsTrue(TextFit.IsLogicalArabic("بغداد"));
        Assert.IsFalse(TextFit.IsLogicalArabic("Baghdad"));
        Assert.IsFalse(TextFit.IsLogicalArabic("ﺍﺑ"), "presentation forms: already shaped, printed as they are");
    }

    [Test]
    public void Shrunk_UnderTheLegibilityFloor_AndLeastOfAGrownText()
    {
        Assert.AreEqual(0.85f, TextFit.Floor, Eps, "FIX_PLAN DOC-LOOK: the floor rises from 0.5 to 0.85");
        Assert.IsTrue(TextFit.Shrunk(0.8f));
        Assert.IsFalse(TextFit.Shrunk(0.85f));
        Assert.AreEqual(0.25f, TextFit.LeastOf(0.5f, 2f), Eps);
        Assert.AreEqual(0.5f, TextFit.LeastOf(0.5f, 0.5f), Eps, "never more room than the art's own");
    }

    // ---------------- RuledLines (BUG-10c) ----------------

    [Test]
    public void RuledLines_AWrappedLineStepsDownOneRule_OrWholeRules()
    {
        // an em of 10 mm with a 1.2 em line under a 15 mm rule: 3 mm more, 30 hundredths of the em
        Assert.AreEqual(30f, RuledLines.Spacing(0.015f, 0.01f, 1.2f), 1e-3f);
        // a line taller than a rule takes two
        Assert.AreEqual((0.02f - 0.012f) / 0.01f * 100f, RuledLines.Spacing(0.01f, 0.01f, 1.2f), 1e-3f);
        Assert.AreEqual(0f, RuledLines.Spacing(0f, 0.01f, 1.2f));
    }

    [Test]
    public void RuledLines_ARowGrowsByWholeRules()
    {
        Assert.AreEqual(0f, RuledLines.Snap(0f, 0.02f), Eps);
        Assert.AreEqual(0f, RuledLines.Snap(0.0009f, 0.02f), Eps, "a hair is none");
        Assert.AreEqual(0.02f, RuledLines.Snap(0.012f, 0.02f), Eps);
        Assert.AreEqual(0.04f, RuledLines.Snap(0.025f, 0.02f), Eps);
        Assert.AreEqual(0.013f, RuledLines.Snap(0.013f, 0f), Eps, "no rules: as it is");
    }

    // ---------------- CapHeight (BUG-9) ----------------

    [Test]
    public void CapHeight_Pixels_FromTheSizeThePageAndThePose()
    {
        // an em of 2% of a 0.4 m page (8 mm), a cap of 0.7 em, at 1000 px per metre: 5.6 px at 1080 lines, 3.73 at 720
        Assert.AreEqual(5.6f, CapHeight.Pixels(2f, 100f, 0.4f, 0.7f, 1000f, 1080), 1e-3f);
        Assert.AreEqual(5.6f * 720f / 1080f, CapHeight.Pixels(2f, 100f, 0.4f, 0.7f, 1000f, 720), 1e-3f);
        Assert.AreEqual(0f, CapHeight.Pixels(2f, 0f, 0.4f, 0.7f, 1000f, 1080));
        Assert.IsTrue(CapHeight.DataReads(10f, 7f));
        Assert.IsFalse(CapHeight.DataReads(10f, 6.9f));
        Assert.IsFalse(CapHeight.DataReads(9.9f, 8f));
    }

    [Test]
    public void CapHeight_Needed_HowMuchLargerTheDataMustPrintToRead()
    {
        Assert.AreEqual(1f, CapHeight.Needed(12f, 8f), Eps, "it reads");
        Assert.AreEqual(2f, CapHeight.Needed(5f, 3.6f), Eps, "the A1 floor needs twice");
        Assert.AreEqual(7f / 3f, CapHeight.Needed(5f, 3f), Eps, "the A2 floor needs more");
        Assert.IsTrue(float.IsPositiveInfinity(CapHeight.Needed(0f, 3f)), "no data measured");
    }

    // ---------------- BookletFlip (D3, BUG-11b) ----------------

    /// <summary>Every angle a flip shows while it swings to rest, sampled every <paramref name="step"/> seconds (its angle now first).</summary>
    private static List<float> Swing(BookletFlip flip, float step = 0.002f)
    {
        var angles = new List<float> { flip.Angle };
        for (int i = 0; i < 1000 && flip.Moving; i++)
        {
            flip.Tick(step);
            angles.Add(flip.Angle);
        }
        return angles;
    }

    /// <summary>The most a swing from rest turns in one frame at 120 frames a second (degrees).</summary>
    private static float FastestStep()
    {
        var flip = new BookletFlip();
        flip.Open();
        List<float> angles = Swing(flip, 1f / 120f);
        float most = 0f;
        for (int i = 1; i < angles.Count; i++)
            most = Math.Max(most, Math.Abs(angles[i] - angles[i - 1]));
        return most;
    }

    [Test]
    public void BookletFlip_ShutOpenShut_ThroughItsSwing()
    {
        var flip = new BookletFlip();
        Assert.AreEqual(CoverState.Closed, flip.State, "a passport arrives shut");
        Assert.AreEqual(0f, flip.Angle);
        Assert.IsTrue(flip.Open());
        Assert.AreEqual(CoverState.Opening, flip.State);
        Assert.IsTrue(flip.IsOpenOrOpening);
        flip.Tick(BookletFlip.LiftSeconds);
        Assert.AreEqual(BookletFlip.LiftDegrees, flip.Angle, 1e-2f, "the lift first: 15 degrees in 40 ms");
        flip.Tick(BookletFlip.SwingSeconds + 0.001f);
        Assert.AreEqual(CoverState.Open, flip.State);
        Assert.AreEqual(180f, flip.Angle);
        Assert.IsTrue(flip.Close());
        Assert.AreEqual(CoverState.Closing, flip.State);
        flip.Tick(BookletFlip.LiftSeconds);
        Assert.AreEqual(180f - BookletFlip.LiftDegrees, flip.Angle, 1e-2f, "shutting lifts off the open side first");
        flip.Tick(1f);
        Assert.AreEqual(CoverState.Closed, flip.State);
        Assert.AreEqual(0f, flip.Angle);
    }

    [Test]
    public void BookletFlip_NeverPassesThroughTheSurface_ItReboundsOffIt()
    {
        var flip = new BookletFlip();
        flip.Open();
        List<float> opening = Swing(flip);
        flip.Close();
        List<float> closing = Swing(flip);
        foreach (float a in opening.Concat(closing))
        {
            Assert.GreaterOrEqual(a, 0f, "below 0 the cover would go through the data page and the desk");
            Assert.LessOrEqual(a, 180f, "past 180 the cover would go through the desk beside the booklet");
        }
        Assert.Greater(Rebound(opening), 5f, "it reaches the desk and rebounds off it toward the viewer (the OutBack overshoot, folded)");
        Assert.Greater(opening.Max(), 175f);
        Assert.AreEqual(180f, opening.Last(), 1e-3f, "before it settles open");
        Assert.Greater(Rebound(closing.ConvertAll(a => 180f - a)), 5f, "shutting rebounds off the page the same way");
        Assert.Less(closing.Min(), 5f);
        Assert.AreEqual(0f, closing.Last(), 1e-3f);
    }

    /// <summary>The most a rising swing falls back after a high it reached (degrees): its rebound.</summary>
    private static float Rebound(List<float> angles)
    {
        float high = float.MinValue, most = 0f;
        foreach (float a in angles)
        {
            high = Math.Max(high, a);
            most = Math.Max(most, high - a);
        }
        return most;
    }

    [Test]
    public void BookletFlip_ReducedMotion_JumpsAtOnce_AndRepeatsAreIgnored()
    {
        var flip = new BookletFlip();
        Assert.IsTrue(flip.Open(instant: true));
        Assert.AreEqual(CoverState.Open, flip.State);
        Assert.IsFalse(flip.Open(), "opening an open booklet does nothing");
        Assert.AreEqual(CoverState.Open, flip.State);
        Assert.IsFalse(flip.Tick(0.1f));
        flip.Close(instant: true);
        Assert.AreEqual(CoverState.Closed, flip.State);
        flip.Shut();
        Assert.AreEqual(0f, flip.Progress);
    }

    [Test]
    public void BookletFlip_TurningBackMidSwing_KeepsTheAngle_AndSwingsBackSmoothly()
    {
        foreach (float share in new[] { 0.1f, 0.3f, 0.5f, 0.75f, 0.9f })
        {
            var flip = new BookletFlip();
            flip.Open();
            flip.Tick((BookletFlip.LiftSeconds + BookletFlip.SwingSeconds) * share);
            float before = flip.Angle;
            Assert.IsTrue(flip.Close());
            Assert.AreEqual(CoverState.Closing, flip.State);
            Assert.AreEqual(before, flip.Angle, 1e-3f, $"at {share} of the opening the cover turns back from where it is");
            List<float> back = Swing(flip, 1f / 120f);
            for (int i = 1; i < back.Count; i++)
                Assert.LessOrEqual(Math.Abs(back[i] - back[i - 1]), FastestStep() * 1.15f, $"at {share}, frame {i}: about as fast as a swing from rest, never a jump");
            Assert.IsTrue(back.All(a => a >= 0f && a <= 180f));
            Assert.AreEqual(0f, back.Last(), 1e-3f);

            // and the other way: shutting from open, turned back to open
            var shut = new BookletFlip();
            shut.Open(instant: true);
            shut.Close();
            shut.Tick((BookletFlip.LiftSeconds + BookletFlip.SwingSeconds) * share);
            float closing = shut.Angle;
            shut.Open();
            Assert.AreEqual(closing, shut.Angle, 1e-3f, $"at {share} of the closing");
        }
    }

    [Test]
    public void BookletFlip_Flex_TheFreeEdgeTrailsTheSwing_FlatAtRest()
    {
        var flip = new BookletFlip();
        Assert.AreEqual(0f, flip.Flex, "shut: flat");
        flip.Open();
        flip.Tick(BookletFlip.LiftSeconds + BookletFlip.SwingSeconds * 0.2f);
        Assert.Greater(flip.Flex, 0.3f, "opening mid-swing: it bows one way");
        flip.Tick(1f);
        Assert.AreEqual(0f, flip.Flex, "open: flat");
        flip.Close();
        flip.Tick(BookletFlip.LiftSeconds + BookletFlip.SwingSeconds * 0.2f);
        Assert.Less(flip.Flex, -0.3f, "shutting: the other way");
        Assert.LessOrEqual(Math.Abs(flip.Flex), 1f);
    }

    [Test]
    public void BookletFlip_ATurnBack_IsTimedByTheWayLeft()
    {
        var flip = new BookletFlip();
        flip.Open();
        flip.Tick(BookletFlip.LiftSeconds); // 15 degrees up
        flip.Close();
        int steps = Swing(flip, 0.001f).Count - 1;
        Assert.LessOrEqual(steps * 0.001f, BookletFlip.SwingSeconds * BookletFlip.LeastShare + 0.002f, "15 degrees back takes the least share of a swing, not a whole one");
    }

    [Test]
    public void BookletFlip_Flex_NeverJumps_WhenTheCoverTurnsBack()
    {
        float biggestTurned = 0f;
        foreach (float fps in new[] { 60f, 120f })
            foreach (float share in new[] { 0.1f, 0.3f, 0.5f, 0.75f, 0.9f })
                foreach (bool opening in new[] { true, false })
                {
                    string at = $"{fps} fps, {share} of the {(opening ? "opening" : "shutting")}";
                    var flip = new BookletFlip();
                    if (!opening)
                        flip.Open(instant: true);
                    if (opening)
                        flip.Open();
                    else
                        flip.Close();
                    float step = 1f / fps, last = flip.Flex, most = 0f;
                    int frames = Math.Max(1, (int)Math.Round((BookletFlip.LiftSeconds + BookletFlip.SwingSeconds) * share * fps));
                    for (int i = 0; i < frames; i++)
                    {
                        flip.Tick(step);
                        most = Math.Max(most, Math.Abs(flip.Flex - last));
                        last = flip.Flex;
                    }
                    float turned = flip.Flex;
                    biggestTurned = Math.Max(biggestTurned, Math.Abs(turned));
                    if (opening)
                        flip.Close();
                    else
                        flip.Open();
                    Assert.AreEqual(turned, flip.Flex, $"{at}: turning back alone leaves the bow where it is");
                    for (int i = 0; i < 400 && (flip.Moving || flip.Flex != 0f); i++)
                    {
                        flip.Tick(step);
                        most = Math.Max(most, Math.Abs(flip.Flex - last));
                        last = flip.Flex;
                    }
                    Assert.LessOrEqual(most, BookletFlip.FlexRate * step + 1e-4f, $"{at}: the bow eases through flat, never a pop (the old sign flip was 2 in one frame)");
                    Assert.AreEqual(0f, flip.Flex, $"{at}: flat once the cover rests");
                    Assert.IsFalse(flip.Moving);
                }
        Assert.Greater(biggestTurned, 0.5f, "some turn back happened under a full bow (at about 90 degrees)");
        Assert.LessOrEqual(BookletFlip.FlexRate / 60f, 0.5f, "at 60 frames a second the passport's free edge (3 mm at the most) moves at most 1.5 mm a frame");
    }

    [Test]
    public void BookletFlip_Flex_TrailsTheSwingAndItsRebound_AndNoneUnderReducedMotion()
    {
        var flip = new BookletFlip();
        flip.Open();
        float least = 0f, most = 0f;
        for (int i = 0; i < 1000 && (flip.Moving || flip.Flex != 0f); i++)
        {
            flip.Tick(1f / 240f);
            least = Math.Min(least, flip.Flex);
            most = Math.Max(most, flip.Flex);
        }
        Assert.Greater(most, 0.6f, "the swing bows its free edge behind it (GAME_FEEL_BIBLE A1: 2 to 4 mm on the passport)");
        Assert.Less(least, -0.03f, "rebounding off the desk the cover turns back toward the viewer, and its edge trails that way too");
        Assert.AreEqual(0f, flip.Flex);
        Assert.IsFalse(flip.Tick(1f / 240f), "at rest and flat, a tick does nothing");

        var still = new BookletFlip();
        still.Open();
        still.Tick(BookletFlip.LiftSeconds + BookletFlip.SwingSeconds * 0.1f);
        Assert.AreNotEqual(0f, still.Flex);
        still.Close(instant: true);
        Assert.AreEqual(0f, still.Flex, "Reduced Motion: shut at once, unbowed");
        still.Open();
        still.Tick(0.05f);
        still.Shut();
        Assert.AreEqual(0f, still.Flex, "a booklet arriving shut lies flat");
    }

    [Test]
    public void ShutBox_KeepsOnlyTheSideOfTheSpineThatShows()
    {
        // a passport 0.125 m tall, its spine 5 mm under its middle: shut, its box keeps the data page under the spine
        (float centre, float size) = ShutBox.Keep(0f, 0.125f, -0.005f, true);
        Assert.AreEqual(-0.0625f, centre - size / 2f, Eps, "from the data page's foot");
        Assert.AreEqual(-0.005f, centre + size / 2f, Eps, "to the spine: nothing over the hidden visa page");
        // the folder's box (0.4 m wide, centred 1 cm right) keeps its board's side, right of a spine at 1.44 cm
        (centre, size) = ShutBox.Keep(0.01f, 0.4f, 0.0144f, false);
        Assert.AreEqual(0.0144f, centre - size / 2f, Eps);
        Assert.AreEqual(0.21f, centre + size / 2f, Eps, "to the folder's right edge");
        // a spine past the box keeps all of it on its own side and none on the other
        (centre, size) = ShutBox.Keep(0f, 0.2f, 0.5f, true);
        Assert.AreEqual(0f, centre, Eps);
        Assert.AreEqual(0.2f, size, Eps);
        Assert.AreEqual(0f, ShutBox.Keep(0f, 0.2f, 0.5f, false).size, Eps);
    }

    // ---------------- CoverBow (wave 2, point 8: the cover's marks ride its bow) ----------------

    /// <summary>
    /// The folder's front board as OfficeSceneUIBuilder.BuildFolderCover lays
    /// it, in metres (the re-verify of ce69276, point 3: the builder's own
    /// numbers, copied here because Assembly-CSharp is out of this assembly's
    /// reach, so a change to them needs these changed too). The folder's face
    /// is RulebookFolderSize, 0.46 by 0.25875; the board runs from the spine
    /// (FolderCover.spine, 0.011 of the face's width right of its middle) to
    /// the body's left edge (DeskRulebook.BodyCentreX 0.003 less half its
    /// BodyWidth 0.957), 0.224 wide, and is BodyHeight (0.917) of the face
    /// tall, 0.237, centred BodyCentreY (-0.0275) of it up. In the hinge's
    /// space it lies centred half a board right of the hinge, its free edge to
    /// its right; it bows FolderCover.FlipBow (1 mm), and the seal, a square
    /// 0.42 of the board wide, lies 0.05 mm over it.
    /// </summary>
    private const float BoardWidth = (0.011f - (0.003f - 0.957f / 2f)) * 0.46f, BoardHeight = 0.917f * 0.25875f, BoardMiddle = -0.0275f * 0.25875f;
    private const float BoardBow = 0.001f, SealGap = 0.00005f, SealHalf = 0.42f * BoardWidth / 2f;

    /// <summary>
    /// The passport's front as PaperCover.Arrive lays it on TC-101 (copied as
    /// the board's are): the booklet is Desk_Default's paperSize.y (0.34)
    /// times TC-101's look scale (0.64) tall, 0.2176, and its aspect (0.65435)
    /// of that wide, 0.142 (DeskDocument.Bind); its spine is at the art's
    /// middle, so the front covers the data page, the lower half, 0.109 tall,
    /// centred half a page under the spine, its free edge at its foot. It bows
    /// PaperCover.FlipBow (1.5 mm), and the emblem, a square 0.42 of its width,
    /// lies PaperLayers.Seal (0.1 mm) over it.
    /// </summary>
    private const float FrontWidth = 0.34f * 0.64f * 0.65435f, FrontHeight = 0.34f * 0.64f / 2f, FrontBow = 0.0015f, EmblemHalf = 0.42f * FrontWidth / 2f;

    /// <summary>The two covers' sheets and marks in their hinge's space, as built (BoardWidth, FrontWidth): each mark a square 0.42 of its sheet's width, centred on it.</summary>
    private static readonly (string name, float w, float h, float sheetX, float sheetY, float freeX, float freeY, float bow, float gap)[] CoverSheets =
    {
        ("the folder's seal", BoardWidth, BoardHeight, BoardWidth / 2f, BoardMiddle, 1f, 0f, BoardBow, SealGap),
        ("the passport's emblem", FrontWidth, FrontHeight, 0f, -FrontHeight / 2f, 0f, -1f, FrontBow, PaperLayers.Seal)
    };

    /// <summary>
    /// The least gap (metres, toward the viewer: -z) between a mark laid by
    /// CoverBow.Ride on a bent cover sheet and the sheet's face under it, over
    /// a 9 by 9 grid of the mark (its corners and edges included). The flat
    /// face lies at z 0 and the mark rests <paramref name="gap"/> over it; the
    /// ride moves the mark by its depth and turns it as Unity's
    /// Quaternion.Euler(pitch, yaw, 0) does (the pitch about x, then the yaw
    /// about y), and each point of the turned mark is set against the face
    /// where that point now lies.
    /// </summary>
    private static float LeastGap(float markX, float markY, float half, float gap, float sheetX, float sheetY, float w, float h, PaperBend bend)
    {
        (float depth, float pitch, float yaw) = CoverBow.Ride(markX, markY, half, half, sheetX, sheetY, w, h, bend);
        double a = pitch * Math.PI / 180.0, b = yaw * Math.PI / 180.0;
        float least = float.MaxValue;
        for (int i = -4; i <= 4; i++)
            for (int j = -4; j <= 4; j++)
            {
                double ox = half * i / 4.0, oy = half * j / 4.0;
                double ry = oy * Math.Cos(a), rz = oy * Math.Sin(a); // the pitch tips the mark's +y toward +z
                double rx = ox * Math.Cos(b) + rz * Math.Sin(b);
                double z = -ox * Math.Sin(b) + rz * Math.Cos(b); // the yaw turns its +x toward -z
                float face = bend.Depth((markX + (float)rx - sheetX) * 2f / w, (markY + (float)ry - sheetY) * 2f / h, w, h);
                least = Math.Min(least, face - (-gap + depth + (float)z));
            }
        return least;
    }

    [Test]
    public void CoverBow_Ride_UnderTheBow_EveryMarkClearsItsBoardByItsRestGap()
    {
        foreach (var c in CoverSheets)
        {
            float half = 0.42f * c.w / 2f;
            // shutting, the free edge curls toward the viewer, up through a mark left lying at rest
            PaperBend shutting = CoverBow.Of(c.freeX, c.freeY, -c.bow);
            float rise = 0f;
            for (int i = -4; i <= 4; i++)
                for (int j = -4; j <= 4; j++)
                    rise = Math.Max(rise, -shutting.Depth(half * i / 4f * 2f / c.w, half * j / 4f * 2f / c.h, c.w, c.h));
            Assert.Greater(rise, c.gap, $"{c.name}: unridden, the shutting board comes up past the mark's gap");

            // the mark centred as built, moved a quarter of the way toward the free edge and toward the hinge, and askew
            float alongX = 0.25f * c.w / 2f * c.freeX, alongY = 0.25f * c.h / 2f * c.freeY;
            foreach ((float ox, float oy) in new[] { (0f, 0f), (alongX, alongY), (-alongX, -alongY), (0.1f * c.w, 0.1f * c.h) })
                foreach (float flex in new[] { -1f, -0.5f, 0.5f, 1f })
                {
                    float least = LeastGap(c.sheetX + ox, c.sheetY + oy, half, c.gap, c.sheetX, c.sheetY, c.w, c.h, CoverBow.Of(c.freeX, c.freeY, c.bow * flex));
                    Assert.GreaterOrEqual(least, c.gap - 1e-7f, $"{c.name} moved x {ox * 1000f:0.0} mm, y {oy * 1000f:0.0} mm off centre, at flex {flex}: the ridden mark stays its rest gap over the board everywhere under it");
                }
        }
    }

    [Test]
    public void CoverBow_Ride_Flat_TheMarkLiesAtRest_AndTheBowCurlsOnlyTheFreeEdge()
    {
        foreach (PaperBend flat in new[] { PaperBend.Flat, CoverBow.Of(1f, 0f, 0f), CoverBow.Of(0f, -1f, 0f) })
        {
            (float depth, float pitch, float yaw) = CoverBow.Ride(BoardWidth / 2f + 0.015f, BoardMiddle + 0.01f, SealHalf, SealHalf, BoardWidth / 2f, BoardMiddle, BoardWidth, BoardHeight, flat);
            Assert.AreEqual(0f, depth, 0f, "flat, an off-centre mark stays at rest");
            Assert.AreEqual(0f, pitch, 0f);
            Assert.AreEqual(0f, yaw, 0f);
        }
        // the one bow the shader draws (PaperCover.Bow) and the marks ride: the hinge end flat, the free edge curled twice the bow
        PaperBend folder = CoverBow.Of(1f, 0f, BoardBow);
        Assert.AreEqual(0f, folder.Depth(-1f, 0.3f, BoardWidth, BoardHeight), 1e-7f, "the folder board's hinge (its left) stays flat");
        Assert.AreEqual(0.002f, folder.Depth(1f, 0.3f, BoardWidth, BoardHeight), 1e-7f, "its free edge (its right) curls 2 mm");
        PaperBend passport = CoverBow.Of(0f, -1f, FrontBow);
        Assert.AreEqual(0f, passport.Depth(0.4f, 1f, FrontWidth, FrontHeight), 1e-7f, "the passport front's spine (its top) stays flat");
        Assert.AreEqual(0.003f, passport.Depth(0.4f, -1f, FrontWidth, FrontHeight), 1e-7f, "its free edge (its foot) curls 3 mm");
    }

    [Test]
    public void CoverBow_Ride_AMarkOffCentre_TakesTheSlopeAndDepthOfTheFaceUnderIt()
    {
        // the folder's seal a quarter of the way from its board's centre toward the free edge (28 mm), then toward the hinge, the board shutting at its full bow
        float w = BoardWidth, h = BoardHeight, cx = BoardWidth / 2f, cy = BoardMiddle;
        PaperBend shutting = CoverBow.Of(1f, 0f, -BoardBow);
        foreach ((float x, string way) in new[] { (0.25f, "toward the free edge"), (-0.25f, "toward the hinge") })
        {
            (float depth, float pitch, float yaw) = CoverBow.Ride(cx + x * w / 2f, cy, SealHalf, SealHalf, cx, cy, w, h, shutting);
            (float dx, float dy) = shutting.Slope(x, 0f, w, h);
            Assert.AreEqual(0f, dy, 1e-9f);
            Assert.AreEqual(0f, pitch, 1e-6f, way + ": the bow runs across the board, so the seal does not tip up the page");
            Assert.AreEqual(-Math.Atan(dx) * 180.0 / Math.PI, yaw, 1e-4, way + ": turned to the face's slope under the seal's own centre (flat under the board's)");
            Assert.Greater(Math.Abs(yaw), 0.1f, way + ": turned, not flat (about 0.35 and 0.16 degrees)");
            Assert.AreEqual(shutting.Depth(x, 0f, w, h) - shutting.Bulge(x, 0f, SealHalf, SealHalf, w, h), depth, 1e-9f, way + ": on the face under its centre, lifted by how far the curl rises past it");
        }
        Assert.Greater(CoverBow.Ride(cx + 0.015f, cy, SealHalf, SealHalf, cx, cy, w, h, shutting).yaw, 0f, "toward the free edge the curl rises toward the viewer: the seal's edge on that side comes toward the viewer with it");
        Assert.Less(CoverBow.Ride(cx - 0.015f, cy, SealHalf, SealHalf, cx, cy, w, h, shutting).yaw, 0f, "toward the hinge the bow's hump lies on the hinge's side of the seal: it turns the other way");

        // the passport's emblem a quarter of the way from its front's centre toward its foot (the free edge, 13.6 mm): the bow runs up the page, so it tips about its x
        PaperBend passport = CoverBow.Of(0f, -1f, -FrontBow);
        float py = -FrontHeight / 2f;
        var (_, tip, turn) = CoverBow.Ride(0f, py - 0.25f * FrontHeight / 2f, EmblemHalf, EmblemHalf, 0f, py, FrontWidth, FrontHeight, passport);
        var (_, up) = passport.Slope(0f, -0.25f, FrontWidth, FrontHeight);
        Assert.AreEqual(Math.Atan(up) * 180.0 / Math.PI, tip, 1e-4, "tipped to the slope up the page");
        Assert.Greater(tip, 0.5f, "its top tips away as its foot comes toward the viewer with the curling edge (about 1.1 degrees)");
        Assert.AreEqual(0f, turn, 1e-6f);
    }

    // ---------------- PaperBend (D3, BUG-12a) ----------------

    [Test]
    public void FlexOf_ACardNeverBends_WhateverTheKnobs()
    {
        Assert.AreEqual(0f, PaperBend.FlexOf(PaperKind.Card, 1f, 1f));
        Assert.AreEqual(0.55f, PaperBend.FlexOf(PaperKind.Booklet, 1f, 0.55f), Eps);
        Assert.AreEqual(1f, PaperBend.FlexOf(PaperKind.Sheet, 1f, 0.55f), Eps);
    }

    [Test]
    public void Bulge_NoneFlat_AndAPartOverACurlRisesClearOfIt()
    {
        Assert.AreEqual(0f, PaperBend.Flat.Bulge(0f, 0f, 0.05f, 0.01f, 0.2f, 0.3f));
        // a landing curl: the ends curl up toward the viewer (negative bow)
        var curl = new PaperBend(1f, 0f, -0.004f, 0f, 0f, 0f, 0f, 0f);
        float bulge = curl.Bulge(0.5f, 0f, 0.04f, 0.005f, 0.2f, 0.3f);
        Assert.Greater(bulge, 0f, "a flat line near the curling end would dip under the face");
        // the part lifted by its bulge is over the face everywhere under it
        float centre = curl.Depth(0.5f, 0f, 0.2f, 0.3f);
        (float dx, float dy) = curl.Slope(0.5f, 0f, 0.2f, 0.3f);
        for (int i = -4; i <= 4; i++)
        {
            float ox = 0.04f * i / 4f;
            float plane = centre + dx * ox - bulge;
            Assert.LessOrEqual(plane, curl.Depth(0.5f + ox * 2f / 0.2f, 0f, 0.2f, 0.3f) + 1e-6f);
        }
    }

    [Test]
    public void PrintCells_CutsAQuadIntoCellsNoWiderThanTheShare()
    {
        Assert.AreEqual(24, PaperBend.PrintCells(0.2f, 0.2f), "a full-width rule");
        Assert.AreEqual(1, PaperBend.PrintCells(0.0004f, 0.26f), "a rule's thickness stays one cell");
        Assert.AreEqual(1, PaperBend.PrintCells(0f, 0.26f));
        Assert.AreEqual(12, PaperBend.PrintCells(0.1f, 0.2f));
    }

    [Test]
    public void PrintCells_TheBentChordNeverSinksUnderTheFace_AtTheKnobsGreatestBend()
    {
        // MotionKnobs' most: paperBowMax 14 mm, the trailing end drooping up to twice (lag 0.5, and 1 to be safe),
        // paperTwistMax 6 mm, paperFlutterMax 5 mm in paperFlutterWaves 1.5; a sheet, a wide ticket and a card's size
        const float printLift = 0.0003f; // DeskDocument.BentPrintLift: how far the bent print rides over the face
        var knobs = new MotionKnobs();
        foreach ((float w, float h) in new[] { (0.17f, 0.26f), (0.26f, 0.17f), (0.12f, 0.08f) })
        {
            int nx = PaperBend.PrintCells(w, w), ny = PaperBend.PrintCells(h, h);
            float most = 0f;
            for (int angle = 0; angle < 360; angle += 45)
                foreach (float lag in new[] { knobs.paperBowLag, 1f })
                    foreach (float twist in new[] { knobs.paperTwistMax, -knobs.paperTwistMax })
                        for (int phase = 0; phase < 4; phase++)
                        {
                            float a = angle * (float)Math.PI / 180f;
                            var bend = new PaperBend((float)Math.Cos(a), (float)Math.Sin(a), knobs.paperBowMax, lag, twist, knobs.paperFlutterMax, phase * (float)Math.PI / 2f, knobs.paperFlutterWaves);
                            most = Math.Max(most, ChordGap(bend, nx, ny, w, h));
                        }
            Assert.LessOrEqual(most, printLift, $"a {w} by {h} m page: the chord sinks {most * 1000f:0.000} mm under the face");
        }
    }

    /// <summary>The most a mesh of <paramref name="nx"/> by <paramref name="ny"/> cells (each two triangles, as DeskDocument cuts them), bent at its vertices, lies behind the bent face (metres).</summary>
    private static float ChordGap(PaperBend bend, int nx, int ny, float w, float h)
    {
        float most = 0f;
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < ny; j++)
            {
                float x0 = -1f + 2f * i / nx, x1 = -1f + 2f * (i + 1) / nx, y0 = -1f + 2f * j / ny, y1 = -1f + 2f * (j + 1) / ny;
                var cells = new[]
                {
                    new[] { (x0, y0), (x0, y1), (x1, y1) },
                    new[] { (x0, y0), (x1, y1), (x1, y0) }
                };
                foreach ((float x, float y)[] tri in cells)
                {
                    float d0 = bend.Depth(tri[0].x, tri[0].y, w, h), d1 = bend.Depth(tri[1].x, tri[1].y, w, h), d2 = bend.Depth(tri[2].x, tri[2].y, w, h);
                    for (int u = 0; u <= 4; u++)
                        for (int v = 0; u + v <= 4; v++)
                        {
                            float a = u / 4f, b = v / 4f, c = 1f - a - b;
                            float px = a * tri[0].x + b * tri[1].x + c * tri[2].x, py = a * tri[0].y + b * tri[1].y + c * tri[2].y;
                            most = Math.Max(most, a * d0 + b * d1 + c * d2 - bend.Depth(px, py, w, h));
                        }
                }
            }
        return most;
    }

    // ---------------- PaperDepth (BUG-12a, 40c) ----------------

    [Test]
    public void SheetRelief_TheLiftedCornerAndTheEdgeCurl_AsTheModelBuildsThem()
    {
        Assert.AreEqual(PaperDepth.CornerLift + PaperDepth.EdgeCurl, PaperDepth.SheetRelief(1f, 0f), 1e-6f, "the bottom right corner, at the right edge");
        Assert.AreEqual(0f, PaperDepth.SheetRelief(0.5f, 0.5f), 1e-7f, "flat in the middle");
        Assert.AreEqual(0f, PaperDepth.SheetRelief(0.5f, 1f), 1e-7f);
        float point = PaperDepth.SheetRelief(0.9f, 0.1f);
        Assert.GreaterOrEqual(PaperDepth.ReliefOver(0.85f, 0.15f, 0.05f, 0.05f), point - 1e-7f, "a part's lift covers every point under it");
        Assert.AreEqual(0f, PaperDepth.ReliefOver(0.3f, 0.7f, 0.05f, 0.05f), 1e-7f);
    }

    [Test]
    public void RankStrength_DarkerUpTheStack_Capped()
    {
        Assert.AreEqual(1f, PaperDepth.RankStrength(0), Eps);
        Assert.Greater(PaperDepth.RankStrength(1), PaperDepth.RankStrength(0));
        Assert.Greater(PaperDepth.RankStrength(2), PaperDepth.RankStrength(1));
        Assert.AreEqual(PaperDepth.MostRankStrength, PaperDepth.RankStrength(50), Eps);
        Assert.AreEqual(1f, PaperDepth.RankStrength(-3), Eps);
    }

    /// <summary>An allocation the counter must see (a static field, so no compiler can keep it off the heap).</summary>
    private static object _countedProbe;

    [Test]
    public void Shadow_ARebuildIntoTheSameMesh_AllocatesNothing_AndMatchesAFreshOne()
    {
        // BUG-65g (the re-verify of cbbe2e3): DeskPaperDepth rebuilds its shadow every frame a hold or a hover eases, into the one DepthMesh it keeps
        long probe = GC.GetAllocatedBytesForCurrentThread();
        _countedProbe = new byte[256];
        if (GC.GetAllocatedBytesForCurrentThread() - probe <= 0)
            Assert.Ignore("This runtime counts no allocations (the counter read 0 over a 256-byte array): the offline runner (CoreCLR) measures it.");
        List<(float x, float y)> rounded = PaperSilhouette.Outline(0.14f, 0.22f, 0.003f).ConvertAll(q => (q.x / 0.14f, q.y / 0.22f));
        foreach (List<(float x, float y)> outline in new[] { rounded, PaperEdge.Rectangle(1f, 1f) })
        {
            var mesh = new DepthMesh();
            PaperDepth.Shadow(outline, 0.14f, 0.22f, 0.001f, 0.008f, 0.002f, -0.003f, 0.5f, mesh); // lying: the first build gives the lists their room
            long before = GC.GetAllocatedBytesForCurrentThread();
            DepthMesh again = PaperDepth.Shadow(outline, 0.14f, 0.22f, 0.001f, 0.026f, 0.01f, -0.017f, 0.3f, mesh); // a later frame of the lift
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.AreEqual(0L, allocated, $"a rebuild of a {outline.Count}-point outline into the same mesh allocated {allocated} B");
            Assert.AreSame(mesh, again, "built into the mesh given");
            DepthMesh fresh = PaperDepth.Shadow(outline, 0.14f, 0.22f, 0.001f, 0.026f, 0.01f, -0.017f, 0.3f);
            CollectionAssert.AreEqual(fresh.Vertices, mesh.Vertices, "emptied first: the corners a fresh mesh gets, none left from the first build");
            CollectionAssert.AreEqual(fresh.Colours, mesh.Colours);
            CollectionAssert.AreEqual(fresh.Triangles, mesh.Triangles);
        }
        Assert.Greater(rounded.Count, 4, "a rounded card's outline, as DeskDocument hands a card's");
    }
}
