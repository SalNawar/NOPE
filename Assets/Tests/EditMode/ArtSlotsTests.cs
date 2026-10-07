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

    [Test]
    public void PaperFaces_OfAPassport_TryItsHoldersNationsPageFirst()
    {
        CollectionAssert.AreEqual(new[] { "Forms/paper_tc101_egypt", "Forms/paper_tc101", ArtSlots.AgencyFace }, ArtSlots.PaperFaces("TC-101", "egypt"));
        CollectionAssert.AreEqual(new[] { "Forms/paper_tc101", ArtSlots.AgencyFace }, ArtSlots.PaperFaces("TC-101", ""), "no nation: the kind's face");
    }

    [Test]
    public void Emblem_IsNamedByTheEmblemsKey()
    {
        Assert.AreEqual("Forms/emblem_wingedsun", ArtSlots.Emblem("WingedSun"));
        Assert.IsTrue(ArtSlots.OnDeskPaper(ArtSlots.Emblem("Crown")), "drawn on the desk papers: imported with mipmaps");
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
        Assert.AreEqual(8, slots.Count);
        Assert.AreEqual("Desktop/icon_orders", ArtSlots.DesktopIcon(DesktopAppIds.Orders));
        Assert.AreEqual("Desktop/icon_portals", ArtSlots.DesktopIcon(DesktopAppIds.Portals));
    }

    [TestCase("adv_scanner", "Orders/upgrade_adv_scanner")]
    [TestCase("repair_portal_02", "Orders/upgrade_repair_portal_02")]
    public void OrderIcon(string id, string expected)
    {
        Assert.AreEqual(expected, ArtSlots.OrderIcon(id));
    }

    [TestCase(UpgradeBranch.Desk, "Orders/branch_desk")]
    [TestCase(UpgradeBranch.Interview, "Orders/branch_interview")]
    [TestCase(UpgradeBranch.Portals, "Orders/branch_portals")]
    [TestCase(UpgradeBranch.Contacts, "Orders/branch_contacts")]
    public void OrderBranch(UpgradeBranch branch, string expected)
    {
        Assert.AreEqual(expected, ArtSlots.OrderBranch(branch));
    }

    [TestCase("house_air_filter", "Home/upgrade_house_air_filter")]
    [TestCase("House Water Purifier", "Home/upgrade_house_water_purifier")]
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
    public void PetSprite_KindAndLook_AndTheCorner()
    {
        Assert.AreEqual("Home/pet_dog_idle", ArtSlots.PetSprite(PetKind.Dog, PetLook.Idle));
        Assert.AreEqual("Home/pet_cat_sick", ArtSlots.PetSprite(PetKind.Cat, PetLook.Sick));
        Assert.AreEqual("Home/pet_corner", ArtSlots.PetCorner);
        Assert.AreEqual("Home/toy_toy_ball", ArtSlots.PetToy("toy_ball"));
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
    public void HallSlot_ByTheSlotAndTheVariantsFile_AndItKeepsTheHallsWidth()
    {
        Assert.AreEqual("Hall/Slots/13-flag-left-cloth/egypt_2", ArtSlots.HallSlot("13-flag-left-cloth", "egypt_2"));
        Assert.AreEqual("Hall/Slots/new-anomalies/breaching", ArtSlots.SlotOf(ArtSlots.AssetRoot + "Hall/Slots/new-anomalies/breaching.png"));
        Assert.AreEqual(4096, ArtSlots.MaxSide(ArtSlots.HallSlot("x", "y")), "the hall's 2172 px canvas is not downscaled");
        Assert.AreEqual(2048, ArtSlots.MaxSide(ArtSlots.SpeechBubble));
    }

    [Test]
    public void SliceShare_OnlyTheBubbleIsSliced()
    {
        Assert.AreEqual(0.25f, ArtSlots.SliceShare(ArtSlots.SpeechBubble));
        Assert.AreEqual(0f, ArtSlots.SliceShare(ArtSlots.SpeechBubbleTail));
        Assert.AreEqual(0f, ArtSlots.SliceShare(ArtSlots.BriefingPaper));
    }

    [Test]
    public void OnDeskPaper_TheFormsFolder()
    {
        Assert.IsTrue(ArtSlots.OnDeskPaper(ArtSlots.PhotoFrame));
        Assert.IsTrue(ArtSlots.OnDeskPaper(ArtSlots.AgencySeal));
        Assert.IsTrue(ArtSlots.OnDeskPaper(ArtSlots.PaperFaces("TC-620")[0]));
        Assert.IsFalse(ArtSlots.OnDeskPaper(ArtSlots.SpeechBubble));
        Assert.IsFalse(ArtSlots.OnDeskPaper(null));
    }
}
