using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// DESK-PLAN's clock reaction under FLOW's rule (BUG-70b, flow.clock.tooltip;
/// the night of 2026-10-28, seam H): the desk clock shows no tooltip that
/// copies its readout. Reaction_Clock.asset's tooltipKey is blank, and the
/// builder makes the asset with a blank key, so a rebuild on a fresh
/// checkout does not bring the tooltip back. Reads the project (QA_PROJECT
/// outside Unity).
/// </summary>
public class DeskReactionTests
{
    [Test]
    public void TheClockReaction_HasNoTooltipKey()
    {
        string path = Path.Combine(SoFieldReadersTests.ProjectRoot(), "Assets", "Data", "Config", "DeskReactions", "Reaction_Clock.asset");
        Assert.IsTrue(File.Exists(path), path);
        Match m = Regex.Match(File.ReadAllText(path), @"^\s*tooltipKey:[ \t]*(.*?)\s*$", RegexOptions.Multiline);
        Assert.IsTrue(m.Success, "the asset carries a tooltipKey field");
        Assert.AreEqual(string.Empty, m.Groups[1].Value, "BUG-70b: the clock's tooltip copied the readout and went stale as the minute moved on");
    }

    [Test]
    public void TheBuilder_MakesTheClockReaction_WithABlankKey()
    {
        string path = Path.Combine(SoFieldReadersTests.ProjectRoot(), "Assets", "Editor", "OfficeSceneUIBuilder.Desk.cs");
        string source = File.ReadAllText(path);
        Match m = Regex.Match(source, @"EnsureDeskReaction\(""Reaction_Clock"",\s*ReactionKind\.None,\s*""(.*?)""\)");
        Assert.IsTrue(m.Success, "the builder makes Reaction_Clock through EnsureDeskReaction");
        Assert.AreEqual(string.Empty, m.Groups[1].Value, "a fresh build must not put the tooltip back");
    }
}
