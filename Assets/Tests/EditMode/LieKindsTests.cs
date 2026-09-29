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
    [TestCase(LieKind.Smuggling, TravellerKind.RichTourist, true)]
    [TestCase(LieKind.Smuggling, TravellerKind.PoorTourist, true)]
    [TestCase(LieKind.Smuggling, TravellerKind.Labourer, true)]
    [TestCase(LieKind.Smuggling, TravellerKind.Displaced, true)]
    [TestCase(LieKind.DebtorPosingAsTourist, TravellerKind.RichTourist, true)]
    [TestCase(LieKind.DebtorPosingAsTourist, TravellerKind.PoorTourist, true)]
    [TestCase(LieKind.DebtorPosingAsTourist, TravellerKind.Labourer, false)]
    [TestCase(LieKind.DebtorPosingAsTourist, TravellerKind.Displaced, false)]
    [TestCase(LieKind.ForgedContract, TravellerKind.Labourer, true)]
    [TestCase(LieKind.ForgedContract, TravellerKind.RichTourist, false)]
    [TestCase(LieKind.ForgedContract, TravellerKind.PoorTourist, false)]
    [TestCase(LieKind.ForgedContract, TravellerKind.Displaced, false)]
    [TestCase(LieKind.FakeWaiver, TravellerKind.PoorTourist, true)]
    [TestCase(LieKind.FakeWaiver, TravellerKind.Labourer, true)]
    [TestCase(LieKind.FakeWaiver, TravellerKind.RichTourist, false)]
    [TestCase(LieKind.FakeWaiver, TravellerKind.Displaced, false)]
    [TestCase(LieKind.ForgedProof, TravellerKind.PoorTourist, true)]
    [TestCase(LieKind.ForgedProof, TravellerKind.RichTourist, false)]
    [TestCase(LieKind.ForgedProof, TravellerKind.Labourer, false)]
    [TestCase(LieKind.ForgedProof, TravellerKind.Displaced, false)]
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

        var day3 = new List<LieKind> { LieKind.PoorPosingAsRich, LieKind.DoctoredIdentity, LieKind.FakeWaiver, LieKind.DebtorPosingAsTourist, LieKind.ForgedContract, LieKind.ForgedProof };
        CollectionAssert.AreEqual(new[] { LieKind.DoctoredIdentity, LieKind.FakeWaiver, LieKind.DebtorPosingAsTourist, LieKind.ForgedProof }, LieKinds.For(day3, TravellerKind.PoorTourist), "the poor tourist's four on day 3");
        CollectionAssert.AreEqual(new[] { LieKind.FakeWaiver, LieKind.ForgedContract }, LieKinds.For(day3, TravellerKind.Labourer), "the labourer's two");

        var day4 = new List<LieKind> { LieKind.FalseOrigin, LieKind.Smuggling };
        CollectionAssert.AreEqual(new[] { LieKind.FalseOrigin, LieKind.Smuggling }, LieKinds.For(day4, TravellerKind.Displaced), "the displaced draw between both");
        CollectionAssert.AreEqual(new[] { LieKind.Smuggling }, LieKinds.For(day4, TravellerKind.Labourer), "a citizen's only lie that day");
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
    public void ThePlaceLies_AreTheFalseOriginTheFakeDisplacedAndSmuggling_EveryOtherLieIsARecordLie()
    {
        foreach (LieKind lie in (LieKind[])System.Enum.GetValues(typeof(LieKind)))
        {
            bool place = lie == LieKind.FalseOrigin || lie == LieKind.FakeDisplaced || lie == LieKind.Smuggling;
            Assert.AreEqual(place, LieKinds.IsPlaceLie(lie), lie.ToString());
            Assert.AreEqual(!place, LieKinds.IsRecordLie(lie), lie.ToString());
        }
    }
}
