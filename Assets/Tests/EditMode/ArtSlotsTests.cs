using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The art slots found by name (redesign phase 27): the lookup rule (the
/// first candidate found, else the fallback), each slot's name from the
/// game's ids, the family bands and the importer's reading of a path.
/// </summary>
public class ArtSlotsTests
{
    /// <summary>A fake Resources: the slots that "exist" load as their own name.</summary>
    private static System.Func<string, string> Present(params string[] slots)
    {
        var set = new HashSet<string>(slots);
        return s => set.Contains(s) ? s : null;
    }

    [Test]
    public void First_MissingFallsBack()
    {
        Assert.IsNull(ArtSlots.First(new[] { ArtSlots.SpeechBubble }, Present()));
    }

    [Test]
    public void First_PresentIsUsed()
    {
        Assert.AreEqual(ArtSlots.SpeechBubble, ArtSlots.First(new[] { ArtSlots.SpeechBubble }, Present(ArtSlots.SpeechBubble)));
    }

    [Test]
    public void First_TakesTheFirstFoundInOrder()
    {
        IReadOnlyList<string> faces = ArtSlots.PaperFaces("TC-610");
        Assert.AreEqual("Forms/paper_tc610", ArtSlots.First(faces, Present("Forms/paper_tc610", ArtSlots.AgencyFace)));
        Assert.AreEqual(ArtSlots.AgencyFace, ArtSlots.First(faces, Present(ArtSlots.AgencyFace)), "a kind without its face takes the agency's");
        Assert.IsNull(ArtSlots.First(faces, Present("Forms/paper_tc620")), "another kind's face is never borrowed");
    }

    [Test]
    public void First_SkipsEmptyCandidatesAndNulls()
    {
        Assert.AreEqual("a", ArtSlots.First(new[] { null, "", "a" }, Present("a")));
        Assert.IsNull(ArtSlots.First(null, Present("a")));
        Assert.IsNull(ArtSlots.First<string>(new[] { "a" }, null));
    }

    [Test]
    public void PaperFaces_WithoutAFormNumberTryTheAgencyFaceOnly()
    {
        CollectionAssert.AreEqual(new[] { ArtSlots.AgencyFace }, ArtSlots.PaperFaces(""));
        CollectionAssert.AreEqual(new[] { ArtSlots.AgencyFace }, ArtSlots.PaperFaces(null));
    }

    [TestCase("investigation", "Desktop/icon_investigation")]
    [TestCase("citizen_account", "Desktop/icon_citizen_account")]
    public void DesktopIcon(string appId, string expected)
    {
        Assert.AreEqual(expected, ArtSlots.DesktopIcon(appId));
    }

    [Test]
    public void DesktopIcon_EveryAppHasItsOwnSlot()
    {
        var slots = new HashSet<string>();
        foreach (string id in DesktopAppIds.DefaultOrder)
            Assert.IsTrue(slots.Add(ArtSlots.DesktopIcon(id)), id);
        Assert.AreEqual(6, slots.Count);
    }

    [TestCase("adv_scanner", "Home/upgrade_adv_scanner")]
    [TestCase("tr_near_east_spoken", "Home/upgrade_tr_near_east_spoken")]
    public void UpgradeIcon(string id, string expected)
    {
        Assert.AreEqual(expected, ArtSlots.UpgradeIcon(id));
    }

    [TestCase(ClueCategory.Currency, "Investigation/refbook_cover_currency")]
    [TestCase(ClueCategory.Language, "Investigation/refbook_cover_language")]
    [TestCase(ClueCategory.Technology, "Investigation/refbook_cover_technology")]
    [TestCase(ClueCategory.Geography, "Investigation/refbook_cover_capital")]
    [TestCase(ClueCategory.Politics, "Investigation/refbook_cover_ruler")]
    [TestCase(ClueCategory.Culture, "Investigation/refbook_cover_culture")]
    public void BookCover_ByTheAssetListsBookIds(ClueCategory category, string expected)
    {
        Assert.AreEqual(expected, ArtSlots.BookCover(category));
    }

    [Test]
    public void VerdictMark()
    {
        Assert.AreEqual("Forms/stamp_accept", ArtSlots.VerdictMark(true));
        Assert.AreEqual("Forms/stamp_deny", ArtSlots.VerdictMark(false));
    }

    [TestCase(0, 0)]
    [TestCase(3, 0)]
    [TestCase(4, 1)]
    [TestCase(7, 1)]
    [TestCase(8, 2)]
    [TestCase(10, 2)]
    [TestCase(-3, 0, Description = "below 0 is the best band")]
    [TestCase(15, 2, Description = "above the cap is the worst band")]
    public void FamilyBand_ThirdsOfTheCap(int condition, int band)
    {
        Assert.AreEqual(band, ArtSlots.FamilyBand(condition, 10));
    }

    [Test]
    public void FamilyBand_ACapBelowOneCountsAsOne()
    {
        Assert.AreEqual(0, ArtSlots.FamilyBand(0, 0));
        Assert.AreEqual(1, ArtSlots.FamilyBand(1, 0));
        Assert.AreEqual(1, ArtSlots.FamilyBand(5, -2));
    }

    [Test]
    public void FamilyPortrait_MemberAndBand()
    {
        Assert.AreEqual("Home/family_partner_well", ArtSlots.FamilyPortrait("Partner", 0, 10));
        Assert.AreEqual("Home/family_kid_ill", ArtSlots.FamilyPortrait("Kid", 5, 10));
        Assert.AreEqual("Home/family_kid_grave", ArtSlots.FamilyPortrait("Kid", 10, 10));
    }

    [TestCase("TC-610", "tc610")]
    [TestCase("Citizen Account", "citizen_account")]
    [TestCase("  Partner  ", "partner")]
    [TestCase("a__b", "a_b")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void Key(string name, string expected)
    {
        Assert.AreEqual(expected, ArtSlots.Key(name));
    }

    [TestCase("Assets/Art/UI/Resources/Office/speech_bubble.png", "Office/speech_bubble")]
    [TestCase("Assets/Art/UI/Resources/Forms/paper_tc610.png", "Forms/paper_tc610")]
    [TestCase("Assets/Art/Home/home_bg.png", null)]
    [TestCase(null, null)]
    public void SlotOf(string path, string expected)
    {
        Assert.AreEqual(expected, ArtSlots.SlotOf(path));
    }

    [Test]
    public void SliceShare_OnlyTheBubbleAndTheTitleFacesAreSliced()
    {
        Assert.AreEqual(0.25f, ArtSlots.SliceShare(ArtSlots.SpeechBubble));
        Assert.AreEqual(0.25f, ArtSlots.SliceShare(ArtSlots.TitleButton));
        Assert.AreEqual(0.25f, ArtSlots.SliceShare(ArtSlots.TitleButtonHover));
        Assert.AreEqual(0f, ArtSlots.SliceShare(ArtSlots.SpeechBubbleTail));
        Assert.AreEqual(0f, ArtSlots.SliceShare(ArtSlots.BriefingPaper));
    }

    [Test]
    public void OnDeskPaper_TheFormsFolder()
    {
        Assert.IsTrue(ArtSlots.OnDeskPaper(ArtSlots.PhotoFrame));
        Assert.IsTrue(ArtSlots.OnDeskPaper(ArtSlots.AgencySeal));
        Assert.IsTrue(ArtSlots.OnDeskPaper(ArtSlots.VerdictMark(true)));
        Assert.IsTrue(ArtSlots.OnDeskPaper(ArtSlots.PaperFaces("TC-620")[0]));
        Assert.IsFalse(ArtSlots.OnDeskPaper(ArtSlots.SpeechBubble));
        Assert.IsFalse(ArtSlots.OnDeskPaper(null));
    }
}
