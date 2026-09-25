using NUnit.Framework;

/// <summary>The rule that keeps theming off evidence: exactly the diegetic roles are skipped.</summary>
public class ThemeRolesTests
{
    [TestCase(ThemeRoleId.Desktop)]
    [TestCase(ThemeRoleId.AcceptButton)]
    [TestCase(ThemeRoleId.WindowBody)]
    [TestCase(ThemeRoleId.Tooltip)]
    [TestCase(ThemeRoleId.ClickCatcher, Description = "the last chrome role")]
    public void ChromeRoles_AreNotDiegetic(ThemeRoleId role)
    {
        Assert.IsFalse(ThemeRoles.IsDiegetic(role));
    }

    [TestCase(ThemeRoleId.DiegeticPaper, Description = "the first diegetic role")]
    [TestCase(ThemeRoleId.DiegeticPhoto)]
    [TestCase(ThemeRoleId.DiegeticRow)]
    [TestCase(ThemeRoleId.DiegeticLabel)]
    [TestCase(ThemeRoleId.DiegeticNote)]
    [TestCase(ThemeRoleId.DiegeticBacking)]
    [TestCase(ThemeRoleId.DiegeticBookRow)]
    [TestCase(ThemeRoleId.DiegeticBubble)]
    [TestCase(ThemeRoleId.DiegeticDevice)]
    public void EvidenceRoles_AreDiegetic(ThemeRoleId role)
    {
        Assert.IsTrue(ThemeRoles.IsDiegetic(role));
    }

    [Test]
    public void EveryRoleBeforeTheFirstDiegeticOne_IsChrome()
    {
        for (int i = 0; i < (int)ThemeRoleId.DiegeticPaper; i++)
            Assert.IsFalse(ThemeRoles.IsDiegetic((ThemeRoleId)i), ((ThemeRoleId)i).ToString());
    }
}
