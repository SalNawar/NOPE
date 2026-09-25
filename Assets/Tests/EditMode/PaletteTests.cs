using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>A culture's seeds through the shared role map (piece 6 R3), diegetic roles kept out of cultures (Z4).</summary>
public class PaletteTests
{
    private const float Tolerance = 0.0001f;

    private static readonly Dictionary<string, Rgba> BaseSeeds = new Dictionary<string, Rgba>
    {
        { "bar", new Rgba(0.1f, 0.2f, 0.3f) },
        { "text", new Rgba(1f, 1f, 1f) },
        { "shade", new Rgba(0f, 0f, 0f, 0.5f) }
    };

    private static readonly Dictionary<string, Rgba> Culture = new Dictionary<string, Rgba>
    {
        { "bar", new Rgba(0.9f, 0.1f, 0.1f) }
    };

    private static PaletteRule Rule(string role, string fill, string ink, float alpha = 1f, ContrastClass cls = ContrastClass.Text) =>
        new PaletteRule { role = role, fill = fill, ink = ink, alpha = alpha, textClass = cls };

    private static List<ResolvedRole> Resolve(PaletteRule[] map, PaletteOverride[] overrides, bool allowDiegetic, List<string> problems) =>
        Palette.Resolve(map, BaseSeeds, Culture, overrides, allowDiegetic, problems);

    [Test]
    public void ARole_TakesItsSeeds_TheCultureFirstThenTheBase()
    {
        var problems = new List<string>();
        ResolvedRole r = Resolve(new[] { Rule("Taskbar", "bar", "text") }, null, false, problems).Single();
        Assert.IsEmpty(problems);
        Assert.AreEqual(ThemeRoleId.Taskbar, r.Role);
        Assert.AreEqual(0.9f, r.Fill.Value.R, Tolerance, "the culture's bar");
        Assert.AreEqual(1f, r.Ink.Value.G, Tolerance, "the base's text");
        Assert.AreEqual(ContrastClass.Text, r.TextClass);
    }

    [Test]
    public void TheRulesAlpha_MultipliesTheFill()
    {
        var problems = new List<string>();
        ResolvedRole r = Resolve(new[] { Rule("DeskDim", "shade", "", 0.5f, ContrastClass.None) }, null, false, problems).Single();
        Assert.AreEqual(0.25f, r.Fill.Value.A, Tolerance);
        Assert.IsNull(r.Ink);
    }

    [Test]
    public void AnOverride_ReplacesTheFillTheInkOrBoth()
    {
        var problems = new List<string>();
        List<ResolvedRole> roles = Resolve(
            new[] { Rule("Taskbar", "bar", "text"), Rule("Tray", "bar", "text"), Rule("TitleBar", "bar", "text") },
            new[]
            {
                new PaletteOverride { role = "Taskbar", fill = "#FF000080" },
                new PaletteOverride { role = "Tray", ink = "#00FF00" },
                new PaletteOverride { role = "TitleBar", fill = "#0000FF", ink = "#000000" }
            }, false, problems);
        Assert.IsEmpty(problems);
        ResolvedRole bar = roles.Single(r => r.Role == ThemeRoleId.Taskbar);
        Assert.AreEqual(1f, bar.Fill.Value.R, Tolerance);
        Assert.AreEqual(128f / 255f, bar.Fill.Value.A, Tolerance, "the override's alpha as written");
        Assert.AreEqual(1f, bar.Ink.Value.R, Tolerance, "the ink kept");
        ResolvedRole tray = roles.Single(r => r.Role == ThemeRoleId.Tray);
        Assert.AreEqual(0.9f, tray.Fill.Value.R, Tolerance, "the fill kept");
        Assert.AreEqual(1f, tray.Ink.Value.G, Tolerance);
        Assert.AreEqual(0f, tray.Ink.Value.R, Tolerance);
        ResolvedRole title = roles.Single(r => r.Role == ThemeRoleId.TitleBar);
        Assert.AreEqual(1f, title.Fill.Value.B, Tolerance);
        Assert.AreEqual(0f, title.Ink.Value.R, Tolerance);
    }

    [Test]
    public void UnknownSeedsRolesAndBadHex_EachAddOneProblem()
    {
        var problems = new List<string>();
        Resolve(new[] { Rule("Taskbar", "nope", "text"), Rule("NoSuchRole", "bar", "text"), Rule("7", "bar", "text") },
                new[] { new PaletteOverride { role = "Ghost", fill = "#000000" }, new PaletteOverride { role = "Taskbar", fill = "#GG0000" } },
                false, problems);
        Assert.AreEqual(5, problems.Count, string.Join(" | ", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("'nope'")));
        Assert.IsTrue(problems.Any(p => p.Contains("'NoSuchRole'")));
        Assert.IsTrue(problems.Any(p => p.Contains("'7'")));
        Assert.IsTrue(problems.Any(p => p.Contains("'Ghost'")));
        Assert.IsTrue(problems.Any(p => p.Contains("#GG0000")));
    }

    [Test]
    public void AMapRuleForADiegeticRole_IsAProblem()
    {
        var problems = new List<string>();
        List<ResolvedRole> roles = Resolve(new[] { Rule("DiegeticRow", "bar", "text") }, null, true, problems);
        Assert.AreEqual(1, problems.Count);
        Assert.IsEmpty(roles);
    }

    [Test]
    public void AnOverrideOfADiegeticRole_OnlyForTheNeutralTheme()
    {
        var o = new[] { new PaletteOverride { role = "DiegeticPaper", fill = "#F7F5EB" } };
        var culture = new List<string>();
        Assert.IsEmpty(Resolve(new PaletteRule[0], o, false, culture));
        Assert.AreEqual(1, culture.Count);

        var neutral = new List<string>();
        ResolvedRole paper = Resolve(new PaletteRule[0], o, true, neutral).Single();
        Assert.IsEmpty(neutral);
        Assert.AreEqual(ThemeRoleId.DiegeticPaper, paper.Role);
        Assert.AreEqual(ContrastClass.None, paper.TextClass);
    }

    [Test]
    public void Missing_ListsChromeRolesWithoutColours_DiegeticOnesOnlyForNeutral()
    {
        var roles = new List<ResolvedRole> { new ResolvedRole { Role = ThemeRoleId.Desktop } };
        List<ThemeRoleId> culture = Palette.Missing(roles, false);
        Assert.IsFalse(culture.Contains(ThemeRoleId.Desktop));
        Assert.IsTrue(culture.Contains(ThemeRoleId.Taskbar));
        Assert.IsFalse(culture.Any(ThemeRoles.IsDiegetic));
        Assert.IsTrue(Palette.Missing(roles, true).Contains(ThemeRoleId.DiegeticBubble));
    }

    [Test]
    public void Pairs_SkipRolesWithoutAClassOrWithoutBothColours()
    {
        var roles = new List<ResolvedRole>
        {
            new ResolvedRole { Role = ThemeRoleId.Tray, Fill = new Rgba(0f, 0f, 0f), Ink = new Rgba(1f, 1f, 1f), TextClass = ContrastClass.Text },
            new ResolvedRole { Role = ThemeRoleId.Taskbar, Fill = new Rgba(0f, 0f, 0f), TextClass = ContrastClass.Text },
            new ResolvedRole { Role = ThemeRoleId.TaskbarGloss, Fill = new Rgba(0f, 0f, 0f), Ink = new Rgba(1f, 1f, 1f), TextClass = ContrastClass.None }
        };
        ContrastPair pair = Palette.Pairs(roles).Single();
        Assert.AreEqual("Tray", pair.Name);
        Assert.AreEqual(ContrastClass.Text, pair.Class);
    }
}
