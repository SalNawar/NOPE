using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The rule that keeps theming off evidence (piece 6 Z4; the PC redesign
/// TH2): the diegetic roles are an explicit list, checked for every enum
/// value, so an appended chrome role never becomes diegetic by its number;
/// and the two retired roles.
/// </summary>
public class ThemeRolesTests
{
    /// <summary>The diegetic roles, and only these.</summary>
    private static readonly ThemeRoleId[] Diegetic =
    {
        ThemeRoleId.DiegeticPaper, ThemeRoleId.DiegeticPhoto, ThemeRoleId.DiegeticRow, ThemeRoleId.DiegeticLabel,
        ThemeRoleId.DiegeticNote, ThemeRoleId.DiegeticBacking, ThemeRoleId.DiegeticBookRow, ThemeRoleId.DiegeticBubble,
        ThemeRoleId.DiegeticDevice, ThemeRoleId.DiegeticForm, ThemeRoleId.SiteContent
    };

    private static IEnumerable<ThemeRoleId> All => ((ThemeRoleId[])Enum.GetValues(typeof(ThemeRoleId))).AsEnumerable();

    [Test]
    public void EveryRole_IsDiegetic_ExactlyWhenListed()
    {
        foreach (ThemeRoleId role in All)
            Assert.AreEqual(Diegetic.Contains(role), ThemeRoles.IsDiegetic(role), role.ToString());
    }

    [TestCase(ThemeRoleId.TabStrip)]
    [TestCase(ThemeRoleId.Tab)]
    [TestCase(ThemeRoleId.TabActive)]
    [TestCase(ThemeRoleId.Sidebar)]
    [TestCase(ThemeRoleId.SearchResults)]
    [TestCase(ThemeRoleId.Badge)]
    [TestCase(ThemeRoleId.Toast)]
    [TestCase(ThemeRoleId.FocusRing)]
    [TestCase(ThemeRoleId.IconSelection, Description = "appended after the diegetic roles, still chrome")]
    public void TheAppendedChromeRoles_AreNotDiegetic(ThemeRoleId role)
    {
        Assert.Greater((int)role, (int)ThemeRoleId.DiegeticDevice);
        Assert.IsFalse(ThemeRoles.IsDiegetic(role));
    }

    [Test]
    public void TheRolesKeepTheirNumbers()
    {
        // Serialized by ThemeTag as an int: the first diegetic role and the appended ones never move.
        Assert.AreEqual(37, (int)ThemeRoleId.DiegeticPaper);
        Assert.AreEqual(45, (int)ThemeRoleId.DiegeticDevice);
        Assert.AreEqual(46, (int)ThemeRoleId.TabStrip);
        Assert.AreEqual(54, (int)ThemeRoleId.IconSelection);
        Assert.AreEqual(56, (int)ThemeRoleId.SiteContent);
    }

    [Test]
    public void OnlyDeskDimAndStickyNote_AreRetired()
    {
        CollectionAssert.AreEquivalent(new[] { ThemeRoleId.StickyNote, ThemeRoleId.DeskDim }, All.Where(ThemeRoles.IsRetired).ToArray());
    }
}
