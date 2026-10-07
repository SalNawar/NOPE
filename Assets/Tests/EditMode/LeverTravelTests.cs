using NUnit.Framework;

/// <summary>
/// The gate lever's travel (the desk machine spec §2): dragged down against
/// resistance (it needs a longer pull the further it goes), a ratchet click
/// every 15 degrees, home only at the full pull.
/// </summary>
public class LeverTravelTests
{
    private const float Travel = 60f, Step = 15f, Resistance = 0.6f;

    [TestCase(0f, 0)]
    [TestCase(14.9f, 0)]
    [TestCase(15f, 1)]
    [TestCase(44f, 2)]
    [TestCase(60f, 4)]
    [TestCase(-3f, 0)]
    public void ANotch_Every15Degrees(float angle, int notch)
    {
        Assert.AreEqual(notch, LeverTravel.Notch(angle, Step));
    }

    [Test]
    public void Crossings_AreCountedOncePerNotch_EitherWay()
    {
        Assert.AreEqual(1, LeverTravel.Crossed(10f, 16f, Step));
        Assert.AreEqual(0, LeverTravel.Crossed(16f, 29f, Step));
        Assert.AreEqual(3, LeverTravel.Crossed(0f, 46f, Step));
        Assert.AreEqual(-2, LeverTravel.Crossed(46f, 20f, Step), "springing back passes them going up");
        Assert.AreEqual(0, LeverTravel.Crossed(5f, 5f, 0f), "no step, no clicks");
    }

    [Test]
    public void Home_OnlyAtTheFullPull()
    {
        Assert.IsFalse(LeverTravel.Home(59f, Travel));
        Assert.IsTrue(LeverTravel.Home(Travel, Travel));
        Assert.IsTrue(LeverTravel.Home(Travel + 2f, Travel));
    }

    [Test]
    public void Resistance_TheArmLagsThePull_MoreTheFurtherItGoes()
    {
        Assert.AreEqual(0f, LeverTravel.Resisted(0f, Travel, Resistance), 1e-5f);
        float a = LeverTravel.Resisted(20f, Travel, Resistance), b = LeverTravel.Resisted(40f, Travel, Resistance);
        Assert.Less(a, 20f);
        Assert.Less(b - a, a, "the second 20 degrees of pull move the arm less than the first");
        Assert.AreEqual(Travel, LeverTravel.Resisted(Travel * (1f + Resistance), Travel, Resistance), 1e-3f, "a full pull reaches home");
        Assert.AreEqual(Travel, LeverTravel.Resisted(500f, Travel, Resistance), 1e-5f, "never past home");
        Assert.AreEqual(0f, LeverTravel.Resisted(-30f, Travel, Resistance), 1e-5f, "never above rest");
    }

    [Test]
    public void NoResistance_FollowsThePull()
    {
        Assert.AreEqual(30f, LeverTravel.Resisted(30f, Travel, 0f), 1e-5f);
    }
}
