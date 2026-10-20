using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// DOC-LOOK's pure rules (FIX_PLAN DOC-LOOK, BUG-9, 10c, 11b, 12a, 14c, 40c;
/// Saleh's D3 and D15): no word breaks across lines, a ruled page's lines,
/// the cap height on screen, the passport's cover, a card that never bends,
/// print that rides the curl and the sheet's lifted corner, and the contact
/// shadow by stack rank.
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
}
