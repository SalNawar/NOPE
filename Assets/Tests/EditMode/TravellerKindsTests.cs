using NUnit.Framework;

/// <summary>
/// The kinds' rules (traveller types K1, K2): who is a 2150 citizen and which
/// kind a premade can be drawn as (which lies fit a kind: LieKindsTests).
/// </summary>
public class TravellerKindsTests
{
    [TestCase(TravellerKind.RichTourist, true)]
    [TestCase(TravellerKind.PoorTourist, true)]
    [TestCase(TravellerKind.Labourer, true)]
    [TestCase(TravellerKind.Displaced, false)]
    public void IsCitizen_EveryKindButTheDisplaced(TravellerKind kind, bool citizen)
    {
        Assert.AreEqual(citizen, TravellerKinds.IsCitizen(kind));
    }

    [Test]
    public void PickWeight_IsTheDaysWeight_NeverBelowZero()
    {
        Assert.AreEqual(2f, TravellerKinds.PickWeight(TravellerKind.RichTourist, 2f, false));
        Assert.AreEqual(1f, TravellerKinds.PickWeight(TravellerKind.Displaced, 1f, false));
        Assert.AreEqual(0f, TravellerKinds.PickWeight(TravellerKind.Displaced, -1f, false));
    }

    [Test]
    public void PickWeight_APremadeIsAlwaysDisplaced()
    {
        Assert.AreEqual(0f, TravellerKinds.PickWeight(TravellerKind.RichTourist, 5f, true), "the famous are displaced premades (K1)");
        Assert.AreEqual(0f, TravellerKinds.PickWeight(TravellerKind.Labourer, 5f, true));
        Assert.AreEqual(3f, TravellerKinds.PickWeight(TravellerKind.Displaced, 3f, true));
    }
}
