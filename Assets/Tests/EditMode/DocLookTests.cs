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

    // ---------------- BookletFlip (D3, BUG-11b) ----------------

    [Test]
    public void BookletFlip_ShutOpenShut_ThroughItsSwing()
    {
        var flip = new BookletFlip();
        Assert.AreEqual(CoverState.Closed, flip.State, "a passport arrives shut");
        Assert.AreEqual(0f, flip.Angle);
        flip.Open();
        Assert.AreEqual(CoverState.Opening, flip.State);
        Assert.IsTrue(flip.IsOpenOrOpening);
        flip.Tick(BookletFlip.LiftSeconds);
        Assert.AreEqual(BookletFlip.LiftDegrees, flip.Angle, 1e-2f, "the lift first: 15 degrees in 40 ms");
        flip.Tick(BookletFlip.SwingSeconds + 0.001f);
        Assert.AreEqual(CoverState.Open, flip.State);
        Assert.AreEqual(180f, flip.Angle);
        flip.Close();
        Assert.AreEqual(CoverState.Closing, flip.State);
        flip.Tick(1f);
        Assert.AreEqual(CoverState.Closed, flip.State);
        Assert.AreEqual(0f, flip.Angle);
    }

    [Test]
    public void BookletFlip_OvershootsOpen_ThenSettles()
    {
        float most = 0f;
        for (int i = 0; i <= 100; i++)
            most = Math.Max(most, BookletFlip.Swing(i / 100f));
        Assert.Greater(most, 180f, "the OutBack swing passes open before it settles");
        Assert.AreEqual(180f, BookletFlip.Swing(1f), 1e-3f);
        Assert.AreEqual(0f, BookletFlip.Swing(0f), 1e-3f);
    }

    [Test]
    public void BookletFlip_ReducedMotion_JumpsAtOnce_AndRepeatsAreIgnored()
    {
        var flip = new BookletFlip();
        flip.Open(instant: true);
        Assert.AreEqual(CoverState.Open, flip.State);
        flip.Open();
        Assert.AreEqual(CoverState.Open, flip.State, "opening an open booklet does nothing");
        Assert.IsFalse(flip.Tick(0.1f));
        flip.Close(instant: true);
        Assert.AreEqual(CoverState.Closed, flip.State);
        flip.Shut();
        Assert.AreEqual(0f, flip.Progress);
    }

    [Test]
    public void BookletFlip_TurningBackMidSwing_StartsFromWhereTheCoverIs()
    {
        var flip = new BookletFlip();
        flip.Open();
        flip.Tick((BookletFlip.LiftSeconds + BookletFlip.SwingSeconds) * 0.75f);
        flip.Close();
        Assert.AreEqual(CoverState.Closing, flip.State);
        Assert.AreEqual(0.25f, flip.Progress, 1e-3f);
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
