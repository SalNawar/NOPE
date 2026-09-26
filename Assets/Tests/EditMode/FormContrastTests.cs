using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The form style's contrast pairs (PC spec FO7, §6.4), checked once by Build
/// Office UI because forms are never themed: ink and labels on the paper and
/// on a box, the section heads on their band, the fine print on the paper,
/// the rules and the stamp area's dash as outlines (3:1), and the ink on each
/// fill laid over a box (the hover tint, every theme's pick highlight).
/// </summary>
public class FormContrastTests
{
    private static Rgba Hex(string hex)
    {
        Assert.IsTrue(Rgba.TryParseHex(hex, out Rgba c), hex);
        return c;
    }

    /// <summary>The spec's starting colours (§6.4) and the band under a section head.</summary>
    private static FormPalette Spec() => new FormPalette
    {
        Paper = Hex("#F4F0E4"),
        Ink = Hex("#1A1714"),
        LabelInk = Hex("#3B342B"),
        FinePrintInk = Hex("#4A443A"),
        Rule = Hex("#5B5347"),
        BoxFill = Hex("#FBF8F0"),
        Band = Hex("#E2DACA"),
        StampDash = Hex("#6B6358")
    };

    [Test]
    public void TheSpecColours_Pass()
    {
        var overlays = new List<(string, Rgba)> { ("the hover tint", new Rgba(0f, 0f, 0f, 0.06f)), ("a pick", new Rgba(1f, 0.92f, 0.35f, 0.7f)) };
        CollectionAssert.IsEmpty(FormContrast.Problems(Spec(), overlays, new ContrastRules()));
    }

    [Test]
    public void ALightLabelInk_IsReported_OnThePaperAndOnABox()
    {
        FormPalette p = Spec();
        p.LabelInk = Hex("#B8B0A0");
        List<string> problems = FormContrast.Problems(p, new List<(string, Rgba)>(), new ContrastRules());
        Assert.AreEqual(2, problems.Count(m => m.Contains("label")), string.Join("\n", problems));
    }

    [Test]
    public void AFaintRule_NeedsTheOutlineMinimum()
    {
        FormPalette p = Spec();
        p.Rule = Hex("#D8D2C4");
        List<string> problems = FormContrast.Problems(p, new List<(string, Rgba)>(), new ContrastRules());
        Assert.IsTrue(problems.Any(m => m.Contains("rule") && m.Contains("3.0")), string.Join("\n", problems));
    }

    [Test]
    public void ADarkPickFill_IsReported_ByItsName()
    {
        var overlays = new List<(string, Rgba)> { ("the 'egypt' pick highlight", new Rgba(0.1f, 0.1f, 0.3f, 0.9f)) };
        List<string> problems = FormContrast.Problems(Spec(), overlays, new ContrastRules());
        Assert.IsTrue(problems.Single().Contains("egypt"), string.Join("\n", problems));
    }
}
