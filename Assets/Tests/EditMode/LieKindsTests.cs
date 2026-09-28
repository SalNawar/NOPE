using System.Collections.Generic;
using NUnit.Framework;

/// <summary>Which lies fit which kinds of traveller (traveller types §6.1), and which lies are record lies.</summary>
public class LieKindsTests
{
    [TestCase(LieKind.FalseOrigin, TravellerKind.Displaced, true)]
    [TestCase(LieKind.FalseOrigin, TravellerKind.RichTourist, false)]
    [TestCase(LieKind.FalseOrigin, TravellerKind.PoorTourist, false)]
    [TestCase(LieKind.FalseOrigin, TravellerKind.Labourer, false)]
    [TestCase(LieKind.PoorPosingAsRich, TravellerKind.RichTourist, true)]
    [TestCase(LieKind.PoorPosingAsRich, TravellerKind.PoorTourist, false)]
    [TestCase(LieKind.PoorPosingAsRich, TravellerKind.Labourer, false)]
    [TestCase(LieKind.PoorPosingAsRich, TravellerKind.Displaced, false)]
    [TestCase(LieKind.DoctoredIdentity, TravellerKind.RichTourist, true)]
    [TestCase(LieKind.DoctoredIdentity, TravellerKind.PoorTourist, true)]
    [TestCase(LieKind.DoctoredIdentity, TravellerKind.Labourer, false)]
    [TestCase(LieKind.DoctoredIdentity, TravellerKind.Displaced, false)]
    [TestCase(LieKind.FakeDisplaced, TravellerKind.Displaced, true)]
    [TestCase(LieKind.FakeDisplaced, TravellerKind.RichTourist, false)]
    [TestCase(LieKind.FakeDisplaced, TravellerKind.PoorTourist, false)]
    [TestCase(LieKind.FakeDisplaced, TravellerKind.Labourer, false)]
    [TestCase(LieKind.DebtorPosingAsTourist, TravellerKind.RichTourist, true)]
    [TestCase(LieKind.DebtorPosingAsTourist, TravellerKind.PoorTourist, true)]
    [TestCase(LieKind.DebtorPosingAsTourist, TravellerKind.Labourer, false)]
    [TestCase(LieKind.DebtorPosingAsTourist, TravellerKind.Displaced, false)]
    [TestCase(LieKind.ForgedContract, TravellerKind.Labourer, true)]
    [TestCase(LieKind.ForgedContract, TravellerKind.RichTourist, false)]
    [TestCase(LieKind.ForgedContract, TravellerKind.PoorTourist, false)]
    [TestCase(LieKind.ForgedContract, TravellerKind.Displaced, false)]
    public void AppliesTo_TheCataloguesKinds(LieKind lie, TravellerKind kind, bool expected)
    {
        Assert.AreEqual(expected, LieKinds.AppliesTo(lie, kind));
    }

    [Test]
    public void For_KeepsTheDaysOrder_DropsWhatDoesNotFit_AndRepeats()
    {
        var day = new List<LieKind> { LieKind.DoctoredIdentity, LieKind.PoorPosingAsRich, LieKind.FalseOrigin, LieKind.DoctoredIdentity, LieKind.ForgedContract, LieKind.DebtorPosingAsTourist };
        CollectionAssert.AreEqual(new[] { LieKind.DoctoredIdentity, LieKind.PoorPosingAsRich, LieKind.DebtorPosingAsTourist }, LieKinds.For(day, TravellerKind.RichTourist));
        CollectionAssert.AreEqual(new[] { LieKind.DoctoredIdentity, LieKind.DebtorPosingAsTourist }, LieKinds.For(day, TravellerKind.PoorTourist));
        CollectionAssert.AreEqual(new[] { LieKind.FalseOrigin }, LieKinds.For(day, TravellerKind.Displaced));
        CollectionAssert.AreEqual(new[] { LieKind.ForgedContract }, LieKinds.For(day, TravellerKind.Labourer));
        CollectionAssert.IsEmpty(LieKinds.For(null, TravellerKind.Displaced));
    }

    [Test]
    public void TrueStatus_PoorPosingAsRich_HoldsAStandardAccount_EveryoneElseTheirKinds()
    {
        Assert.AreEqual(CitizenStatus.Standard, LieKinds.TrueStatus(LieKind.PoorPosingAsRich, CitizenStatus.Premium));
        Assert.AreEqual(CitizenStatus.Eligible, LieKinds.TrueStatus(LieKind.DebtorPosingAsTourist, CitizenStatus.Premium), "a debtor posing as a rich tourist (L4)");
        Assert.AreEqual(CitizenStatus.Eligible, LieKinds.TrueStatus(LieKind.DebtorPosingAsTourist, CitizenStatus.Standard), "a debtor posing as a poor tourist (L4)");
        Assert.AreEqual(CitizenStatus.Eligible, LieKinds.TrueStatus(LieKind.ForgedContract, CitizenStatus.Eligible));
        Assert.AreEqual(CitizenStatus.Premium, LieKinds.TrueStatus(LieKind.DoctoredIdentity, CitizenStatus.Premium));
        Assert.AreEqual(CitizenStatus.Premium, LieKinds.TrueStatus(null, CitizenStatus.Premium));
        Assert.AreEqual(CitizenStatus.Eligible, LieKinds.TrueStatus(LieKind.FalseOrigin, CitizenStatus.Eligible));
    }

    [Test]
    public void ThePlaceLies_AreTheFalseOriginAndTheFakeDisplaced_EveryOtherLieIsARecordLie()
    {
        foreach (LieKind lie in (LieKind[])System.Enum.GetValues(typeof(LieKind)))
        {
            bool place = lie == LieKind.FalseOrigin || lie == LieKind.FakeDisplaced;
            Assert.AreEqual(place, LieKinds.IsPlaceLie(lie), lie.ToString());
            Assert.AreEqual(!place, LieKinds.IsRecordLie(lie), lie.ToString());
        }
    }
}
