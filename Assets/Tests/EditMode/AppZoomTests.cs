using NUnit.Framework;

/// <summary>The panes' zoom (the PC redesign KB5): the steps between the levels, clamped at both ends, and the saved default read back.</summary>
public class AppZoomTests
{
    private static readonly int[] Levels = { 100, 125, 150 };

    [Test]
    public void Step_UpAndDown()
    {
        Assert.AreEqual(125, AppZoom.Step(Levels, 100, 1));
        Assert.AreEqual(150, AppZoom.Step(Levels, 125, 1));
        Assert.AreEqual(125, AppZoom.Step(Levels, 150, -1));
        Assert.AreEqual(100, AppZoom.Step(Levels, 125, -1));
    }

    [Test]
    public void Step_StopsAtBothEnds()
    {
        Assert.AreEqual(150, AppZoom.Step(Levels, 150, 1));
        Assert.AreEqual(100, AppZoom.Step(Levels, 100, -1));
    }

    [Test]
    public void Step_FromALevelNotListed_GoesToTheNearestLevelThatWay()
    {
        Assert.AreEqual(125, AppZoom.Step(Levels, 110, 1));
        Assert.AreEqual(100, AppZoom.Step(Levels, 110, -1));
        Assert.AreEqual(150, AppZoom.Step(Levels, 200, 1));
    }

    [Test]
    public void Parse_ASavedLevel_ElseTheFirst()
    {
        Assert.AreEqual(125, AppZoom.Parse("125", Levels));
        Assert.AreEqual(150, AppZoom.Parse(" 150 ", Levels));
        Assert.AreEqual(100, AppZoom.Parse("110", Levels));
        Assert.AreEqual(100, AppZoom.Parse("big", Levels));
        Assert.AreEqual(100, AppZoom.Parse(null, Levels));
    }

    [Test]
    public void NoLevels_AlwaysOneHundred()
    {
        Assert.AreEqual(100, AppZoom.Step(new int[0], 125, 1));
        Assert.AreEqual(100, AppZoom.Parse("125", null));
    }

    [Test]
    public void Scale_IsTheLevelAsAFactor() => Assert.AreEqual(1.25f, AppZoom.Scale(125), 1e-6f);
}
