using System.Collections.Generic;
using NUnit.Framework;

/// <summary>The office scene contract's naming and resolution order (OfficeContract).</summary>
public class OfficeContractTests
{
    [Test]
    public void AnchorName_IsThePrefixAndTheId()
    {
        Assert.AreEqual("Anchor_PCScreen", OfficeContract.AnchorName(OfficeAnchorId.PCScreen));
        Assert.AreEqual("Anchor_Traveller", OfficeContract.AnchorName(OfficeAnchorId.Traveller));
    }

    [Test]
    public void Candidates_PutTheExplicitAnchorFirst_ThenTheFallbacksInOrder_SkippingBlanks()
    {
        List<string> c = OfficeContract.Candidates(OfficeAnchorId.DeskSurface, new[] { " HybridOffice/Booth/Finish_Mat ", "", null, "DeskTop" });
        CollectionAssert.AreEqual(new[] { "Anchor_DeskSurface", "HybridOffice/Booth/Finish_Mat", "DeskTop" }, c);
    }

    [Test]
    public void Candidates_WithNoFallbacks_AreTheAnchorAlone()
    {
        CollectionAssert.AreEqual(new[] { "Anchor_Scanner" }, OfficeContract.Candidates(OfficeAnchorId.Scanner, null));
    }

    [TestCase("Anchor_Traveller", true)]
    [TestCase("NextLabel", true)]
    [TestCase("HybridOffice/Booth/Blender_Next", false)]
    [TestCase(null, false)]
    public void IsBareName_OnlyForANameWithoutASlash(string path, bool bare)
    {
        Assert.AreEqual(bare, OfficeContract.IsBareName(path));
    }

    [TestCase(0, false, AnchorSource.Anchor)]
    [TestCase(1, false, AnchorSource.Fallback)]
    [TestCase(3, true, AnchorSource.Fallback)]
    [TestCase(-1, true, AnchorSource.Default)]
    [TestCase(-1, false, AnchorSource.Missing)]
    public void SourceOf_TheAnchorThenFallbacksThenTheDefault(int index, bool hasDefault, AnchorSource source)
    {
        Assert.AreEqual(source, OfficeContract.SourceOf(index, hasDefault));
    }

    [TestCase("CRT2_Glass", true)]
    [TestCase("CRT_Study_Glass", true)]
    [TestCase("MonitorScreen", true)]
    [TestCase("SCREEN", true)]
    [TestCase("CRT_Study_Ivory", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void IsScreenName_MatchesGlassOrScreen_AnyCase(string name, bool screen)
    {
        Assert.AreEqual(screen, OfficeContract.IsScreenName(name));
    }
}
