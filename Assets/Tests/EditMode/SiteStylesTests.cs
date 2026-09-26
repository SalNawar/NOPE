using System.Collections.Generic;
using NUnit.Framework;

/// <summary>The sites' fixed page styles: every text colour reads on the page and on a box at the body-text minimum (P spec IN6, checked once for every site).</summary>
public class SiteStylesTests
{
    [Test]
    public void EveryStyle_PassesTheBodyTextContrast()
    {
        var rules = new ContrastRules();
        foreach ((string name, SiteStyle style) in SiteStyles.All)
        {
            List<string> problems = Contrast.Problems(SiteStyles.Pairs(name, style), new Rgba(0f, 0f, 0f), new Rgba(1f, 1f, 1f), rules);
            Assert.IsEmpty(problems, name + ": " + string.Join(" | ", problems));
        }
    }

    [Test]
    public void For_TheKindsOwnStyle_ElseTheStartPages()
    {
        Assert.AreEqual(SiteStyles.All[0].Style.Paper, SiteStyles.For("News").Paper);
        Assert.AreEqual(SiteStyles.All[1].Style.Accent, SiteStyles.For("History").Accent);
        Assert.AreEqual(SiteStyles.All[2].Style.Box, SiteStyles.For("Ancestry").Box);
        Assert.AreEqual(SiteStyles.All[3].Style.Link, SiteStyles.For("Static").Link);
        Assert.AreEqual(SiteStyles.All[3].Style.Ink, SiteStyles.For(null).Ink);
    }
}
