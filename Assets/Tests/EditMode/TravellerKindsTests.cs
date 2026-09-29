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
        Assert.AreEqual(2f, TravellerKinds.PickWeight(TravellerKind.RichTourist, 2f, null));
        Assert.AreEqual(1f, TravellerKinds.PickWeight(TravellerKind.Displaced, 1f, null));
        Assert.AreEqual(0f, TravellerKinds.PickWeight(TravellerKind.Displaced, -1f, null));
    }

    /// <summary>The displaced case, today's (a famous premade's slot; a return-home liar's): every weight as before, so days 1-6 draw the same.</summary>
    [Test]
    public void PickWeight_TheDisplacedCaseIsTodays()
    {
        Assert.AreEqual(0f, TravellerKinds.PickWeight(TravellerKind.RichTourist, 5f, TravellerKind.Displaced), "the famous are displaced premades (K1); a return-home liar is displaced");
        Assert.AreEqual(0f, TravellerKinds.PickWeight(TravellerKind.Labourer, 5f, TravellerKind.Displaced));
        Assert.AreEqual(3f, TravellerKinds.PickWeight(TravellerKind.Displaced, 3f, TravellerKind.Displaced));
    }

    /// <summary>A citizen premade's slot (days 7-15 B2) keeps its one weighted draw, restricted to the premade's kind.</summary>
    [Test]
    public void PickWeight_OnlyThePremadesKindWeighs()
    {
        Assert.AreEqual(2f, TravellerKinds.PickWeight(TravellerKind.PoorTourist, 2f, TravellerKind.PoorTourist));
        Assert.AreEqual(0f, TravellerKinds.PickWeight(TravellerKind.RichTourist, 2f, TravellerKind.PoorTourist));
        Assert.AreEqual(0f, TravellerKinds.PickWeight(TravellerKind.Displaced, 2f, TravellerKind.Labourer));
        Assert.AreEqual(0f, TravellerKinds.PickWeight(TravellerKind.Labourer, 0f, TravellerKind.Labourer), "an unweighted kind stays unweighted: the checks refuse such a day");
    }
}
