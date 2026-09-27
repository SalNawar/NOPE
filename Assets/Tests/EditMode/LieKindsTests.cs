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
    public void AppliesTo_TheCataloguesKinds(LieKind lie, TravellerKind kind, bool expected)
    {
        Assert.AreEqual(expected, LieKinds.AppliesTo(lie, kind));
    }

    [Test]
    public void For_KeepsTheDaysOrder_DropsWhatDoesNotFit_AndRepeats()
    {
        var day = new List<LieKind> { LieKind.DoctoredIdentity, LieKind.PoorPosingAsRich, LieKind.FalseOrigin, LieKind.DoctoredIdentity };
        CollectionAssert.AreEqual(new[] { LieKind.DoctoredIdentity, LieKind.PoorPosingAsRich }, LieKinds.For(day, TravellerKind.RichTourist));
        CollectionAssert.AreEqual(new[] { LieKind.DoctoredIdentity }, LieKinds.For(day, TravellerKind.PoorTourist));
        CollectionAssert.AreEqual(new[] { LieKind.FalseOrigin }, LieKinds.For(day, TravellerKind.Displaced));
        CollectionAssert.IsEmpty(LieKinds.For(day, TravellerKind.Labourer));
        CollectionAssert.IsEmpty(LieKinds.For(null, TravellerKind.Displaced));
    }

    [Test]
    public void TrueStatus_PoorPosingAsRich_HoldsAStandardAccount_EveryoneElseTheirKinds()
    {
        Assert.AreEqual(CitizenStatus.Standard, LieKinds.TrueStatus(LieKind.PoorPosingAsRich, CitizenStatus.Premium));
        Assert.AreEqual(CitizenStatus.Premium, LieKinds.TrueStatus(LieKind.DoctoredIdentity, CitizenStatus.Premium));
        Assert.AreEqual(CitizenStatus.Premium, LieKinds.TrueStatus(null, CitizenStatus.Premium));
        Assert.AreEqual(CitizenStatus.Eligible, LieKinds.TrueStatus(LieKind.FalseOrigin, CitizenStatus.Eligible));
    }

    [Test]
    public void OnlyTheFalseOrigin_IsAPlaceLie()
    {
        foreach (LieKind lie in (LieKind[])System.Enum.GetValues(typeof(LieKind)))
            Assert.AreEqual(lie != LieKind.FalseOrigin, LieKinds.IsRecordLie(lie), lie.ToString());
    }
}
