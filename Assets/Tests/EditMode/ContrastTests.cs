using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>WCAG contrast maths and the theme contrast rules every culture must pass (piece 6 U10, R7).</summary>
public class ContrastTests
{
    private static Rgba Hex(string hex)
    {
        Assert.IsTrue(Rgba.TryParseHex(hex, out Rgba c), hex);
        return c;
    }

    private static readonly Rgba Black = new Rgba(0f, 0f, 0f);
    private static readonly Rgba White = new Rgba(1f, 1f, 1f);
    private static readonly ContrastRules Rules = new ContrastRules();

    private static List<string> Check(params ContrastPair[] pairs) =>
        Contrast.Problems(pairs, Hex("#092E70"), Hex("#FFD76E"), Rules);

    [Test]
    public void RelativeLuminance_OfBlackWhiteAndGrey()
    {
        Assert.AreEqual(0d, Contrast.RelativeLuminance(Black), 1e-9);
        Assert.AreEqual(1d, Contrast.RelativeLuminance(White), 1e-9);
        Assert.AreEqual(0.1845d, Contrast.RelativeLuminance(Hex("#777777")), 0.0005);
    }

    [Test]
    public void Ratio_IsSymmetric_AndMatchesKnownValues()
    {
        Assert.AreEqual(21d, Contrast.Ratio(White, Black), 0.01);
        Assert.AreEqual(Contrast.Ratio(White, Black), Contrast.Ratio(Black, White), 1e-9);
        Assert.AreEqual(1d, Contrast.Ratio(Hex("#3D993B"), Hex("#3D993B")), 1e-9);
        Assert.AreEqual(6.085d, Contrast.Ratio(White, Hex("#2157DB")), 0.005);
        Assert.AreEqual(3.609d, Contrast.Ratio(White, Hex("#3D993B")), 0.005);
    }

    [Test]
    public void Over_CompositesByAlpha()
    {
        Rgba red = new Rgba(1f, 0f, 0f);
        Assert.AreEqual(1f, Contrast.Over(red.WithAlpha(1f), Black).R, 1e-6);
        Assert.AreEqual(0f, Contrast.Over(red.WithAlpha(0f), Black).R, 1e-6);
        Rgba half = Contrast.Over(red.WithAlpha(0.5f), White);
        Assert.AreEqual(1f, half.R, 1e-6);
        Assert.AreEqual(0.5f, half.G, 1e-6);
        Assert.AreEqual(1f, half.A, 1e-6);
    }

    [Test]
    public void Problems_ReportsTextBelowItsClass_WithNameAndNumbers()
    {
        // #777777 on white is 4.48:1.
        List<string> problems = Check(new ContrastPair("RoleA", Hex("#777777"), White, ContrastClass.Text));
        Assert.AreEqual(1, problems.Count);
        StringAssert.Contains("RoleA", problems[0]);
        StringAssert.Contains("4.5", problems[0]);
        StringAssert.Contains("4.4", problems[0]);
    }

    [Test]
    public void Problems_LargeTextGlyphAndHint_NeedThree_NoneIsNeverReported()
    {
        Assert.IsEmpty(Check(new ContrastPair("Big", Hex("#777777"), White, ContrastClass.LargeText)));
        Assert.IsEmpty(Check(new ContrastPair("Mark", Hex("#777777"), White, ContrastClass.Glyph)));
        Assert.IsEmpty(Check(new ContrastPair("Hint", Hex("#777777"), White, ContrastClass.Hint)));
        Assert.IsEmpty(Check(new ContrastPair("Flat", White, White, ContrastClass.None)));
        Assert.AreEqual(1, Check(new ContrastPair("Pale", Hex("#AAAAAA"), White, ContrastClass.LargeText)).Count);
    }

    [Test]
    public void Problems_ReportsATranslucentTextFill()
    {
        List<string> problems = Check(new ContrastPair("Glass", White, Black.WithAlpha(0.6f), ContrastClass.Text));
        Assert.AreEqual(1, problems.Count);
        StringAssert.Contains("Glass", problems[0]);
    }

    [Test]
    public void Problems_CompositesATranslucentInkOverItsFill()
    {
        // Black at 20% over white is a pale grey: far below 3:1, although opaque black would pass.
        Assert.AreEqual(1, Check(new ContrastPair("Faint", Black.WithAlpha(0.2f), White, ContrastClass.Hint)).Count);
        Assert.IsEmpty(Check(new ContrastPair("Solid", Black, White, ContrastClass.Hint)));
    }

    [Test]
    public void Problems_ReportsRingsLessThanNineApart()
    {
        List<string> problems = Contrast.Problems(new ContrastPair[0], Hex("#0B3A8C"), Hex("#FFD76E"), Rules);
        Assert.AreEqual(1, problems.Count);
        StringAssert.Contains("7.6", problems[0]);
        StringAssert.Contains("9", problems[0]);
        Assert.IsEmpty(Contrast.Problems(new ContrastPair[0], Hex("#092E70"), Hex("#FFD76E"), Rules));
    }

    [TestCase("#000000")]
    [TestCase("#FFFFFF")]
    [TestCase("#777777")]
    [TestCase("#3B73BD")]
    [TestCase("#F2E6C9")]
    [TestCase("#123456")]
    public void RingsNineApart_OneReachesThreeOnAnyBackground(string background)
    {
        Rgba dark = Hex("#092E70"), light = Hex("#FFD76E"), bg = Hex(background);
        Assert.GreaterOrEqual(Contrast.Ratio(dark, light), 9d);
        Assert.GreaterOrEqual(Math.Max(Contrast.Ratio(dark, bg), Contrast.Ratio(light, bg)), 3d);
    }

    [Test]
    public void RingsNineApart_ReachThreeOnTheWorstGrey()
    {
        // The worst background sits at the geometric mean of the rings' luminances (+0.05):
        // there both rings are equally far, sqrt(b/a) >= 3 when b/a >= 9.
        Rgba dark = Hex("#092E70"), light = Hex("#FFD76E");
        double a = Contrast.RelativeLuminance(dark) + 0.05, b = Contrast.RelativeLuminance(light) + 0.05;
        double s = Math.Sqrt(a * b) - 0.05;
        float g = (float)(s <= 0.0031308 ? 12.92 * s : 1.055 * Math.Pow(s, 1 / 2.4) - 0.055);
        var grey = new Rgba(g, g, g);
        Assert.AreEqual(s, Contrast.RelativeLuminance(grey), 1e-4);
        Assert.GreaterOrEqual(Math.Max(Contrast.Ratio(dark, grey), Contrast.Ratio(light, grey)), 3d);
    }

    [Test]
    public void ContrastRules_MinPerClass()
    {
        Assert.AreEqual(4.5f, Rules.Min(ContrastClass.Text));
        Assert.AreEqual(3f, Rules.Min(ContrastClass.LargeText));
        Assert.AreEqual(3f, Rules.Min(ContrastClass.Glyph));
        Assert.AreEqual(3f, Rules.Min(ContrastClass.Hint));
        Assert.AreEqual(0f, Rules.Min(ContrastClass.None));
    }
}
