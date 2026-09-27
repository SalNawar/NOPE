using NUnit.Framework;

/// <summary>
/// Audit R1-003: every enum stored as an int in an asset or a save keeps its
/// values. Inserting or reordering a member would silently remap every
/// asset and save that stores it, so each is pinned here (append only).
/// </summary>
public class SerializedEnumsTests
{
    /// <summary>ClueCategory: stored in templates, books, places, effects, the config and saves (FactEdit, CarryRecord); phase 3 appends CitizenId, Destination, Incident, DepartureDate and Expiry; phase 6 AccountStatus, TransponderId, TransponderClass and Debt.</summary>
    [Test]
    public void ClueCategory_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)ClueCategory.Language);
        Assert.AreEqual(1, (int)ClueCategory.Material);
        Assert.AreEqual(2, (int)ClueCategory.Politics);
        Assert.AreEqual(3, (int)ClueCategory.Technology);
        Assert.AreEqual(4, (int)ClueCategory.Currency);
        Assert.AreEqual(5, (int)ClueCategory.Geography);
        Assert.AreEqual(6, (int)ClueCategory.Culture);
        Assert.AreEqual(7, (int)ClueCategory.Name);
        Assert.AreEqual(8, (int)ClueCategory.BirthDate);
        Assert.AreEqual(9, (int)ClueCategory.CitizenId);
        Assert.AreEqual(10, (int)ClueCategory.Destination);
        Assert.AreEqual(11, (int)ClueCategory.Incident);
        Assert.AreEqual(12, (int)ClueCategory.DepartureDate);
        Assert.AreEqual(13, (int)ClueCategory.Expiry);
        Assert.AreEqual(14, (int)ClueCategory.AccountStatus);
        Assert.AreEqual(15, (int)ClueCategory.TransponderId);
        Assert.AreEqual(16, (int)ClueCategory.TransponderClass);
        Assert.AreEqual(17, (int)ClueCategory.Debt);
        Assert.AreEqual(18, System.Enum.GetValues(typeof(ClueCategory)).Length, "a new member is appended here too");
    }

    /// <summary>CitizenStatus: stored in the content library's account ranges (agency.accounts.statuses).</summary>
    [Test]
    public void CitizenStatus_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)CitizenStatus.Premium);
        Assert.AreEqual(1, (int)CitizenStatus.Standard);
        Assert.AreEqual(2, (int)CitizenStatus.Eligible);
        Assert.AreEqual(3, System.Enum.GetValues(typeof(CitizenStatus)).Length, "a new member is appended here too");
    }

    /// <summary>TransponderClass: stored in the content library's transponder models (agency.transponders).</summary>
    [Test]
    public void TransponderClass_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)TransponderClass.Premium);
        Assert.AreEqual(1, (int)TransponderClass.Economy);
        Assert.AreEqual(2, System.Enum.GetValues(typeof(TransponderClass)).Length, "a new member is appended here too");
    }

    /// <summary>TravellerKind: stored in CaseBlueprintSO.kind and DocumentTemplateSO.askableBy.</summary>
    [Test]
    public void TravellerKind_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)TravellerKind.RichTourist);
        Assert.AreEqual(1, (int)TravellerKind.PoorTourist);
        Assert.AreEqual(2, (int)TravellerKind.Labourer);
        Assert.AreEqual(3, (int)TravellerKind.Displaced);
        Assert.AreEqual(4, System.Enum.GetValues(typeof(TravellerKind)).Length, "a new member is appended here too");
    }

    /// <summary>LookSlot: stored in wardrobes and looks.</summary>
    [Test]
    public void LookSlot_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)LookSlot.Outfit);
        Assert.AreEqual(1, (int)LookSlot.Hair);
        Assert.AreEqual(2, (int)LookSlot.FacialHair);
        Assert.AreEqual(3, (int)LookSlot.Headwear);
        Assert.AreEqual(4, (int)LookSlot.Accessory);
        Assert.AreEqual(5, System.Enum.GetValues(typeof(LookSlot)).Length, "a new member is appended here too");
    }

    /// <summary>LookLayer: serialized arrays are indexed by it.</summary>
    [Test]
    public void LookLayer_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)LookLayer.HairBack);
        Assert.AreEqual(1, (int)LookLayer.Body);
        Assert.AreEqual(2, (int)LookLayer.Outfit);
        Assert.AreEqual(3, (int)LookLayer.Head);
        Assert.AreEqual(4, (int)LookLayer.FacialHair);
        Assert.AreEqual(5, (int)LookLayer.Hair);
        Assert.AreEqual(6, (int)LookLayer.Headwear);
        Assert.AreEqual(7, (int)LookLayer.Accessory);
        Assert.AreEqual(8, (int)LookLayer.Whole);
        Assert.AreEqual(9, System.Enum.GetValues(typeof(LookLayer)).Length, "a new member is appended here too");
    }

    /// <summary>TellChannel: stored in DayPlanSO.tellChannels.</summary>
    [Test]
    public void TellChannel_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)TellChannel.Papers);
        Assert.AreEqual(1, (int)TellChannel.Answer);
        Assert.AreEqual(2, (int)TellChannel.Appearance);
        Assert.AreEqual(3, System.Enum.GetValues(typeof(TellChannel)).Length, "a new member is appended here too");
    }

    /// <summary>OfficeAnchorId: stored by value in OfficeSceneContractSO.</summary>
    [Test]
    public void OfficeAnchorId_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)OfficeAnchorId.PCScreen);
        Assert.AreEqual(1, (int)OfficeAnchorId.PCPower);
        Assert.AreEqual(2, (int)OfficeAnchorId.DeskSurface);
        Assert.AreEqual(3, (int)OfficeAnchorId.Scanner);
        Assert.AreEqual(4, (int)OfficeAnchorId.Traveller);
        Assert.AreEqual(5, (int)OfficeAnchorId.HandOver);
        Assert.AreEqual(6, (int)OfficeAnchorId.NextSign);
        Assert.AreEqual(7, (int)OfficeAnchorId.Intercom);
        Assert.AreEqual(8, (int)OfficeAnchorId.Stamp);
        Assert.AreEqual(9, (int)OfficeAnchorId.Till);
        Assert.AreEqual(10, (int)OfficeAnchorId.StabilityMonitor);
        Assert.AreEqual(11, (int)OfficeAnchorId.Calendar);
        Assert.AreEqual(12, (int)OfficeAnchorId.Clock);
        Assert.AreEqual(13, (int)OfficeAnchorId.ReadoutDay);
        Assert.AreEqual(14, (int)OfficeAnchorId.ReadoutStability);
        Assert.AreEqual(15, (int)OfficeAnchorId.ReadoutCredits);
        Assert.AreEqual(16, (int)OfficeAnchorId.ReadoutClock);
        Assert.AreEqual(17, (int)OfficeAnchorId.ReadoutNext);
        Assert.AreEqual(18, (int)OfficeAnchorId.OfficeCamera);
        Assert.AreEqual(19, (int)OfficeAnchorId.OfficeVCam);
        Assert.AreEqual(20, (int)OfficeAnchorId.Calculator);
        Assert.AreEqual(21, (int)OfficeAnchorId.PenPot);
        Assert.AreEqual(22, (int)OfficeAnchorId.Stapler);
        Assert.AreEqual(23, System.Enum.GetValues(typeof(OfficeAnchorId)).Length, "a new member is appended here too");
    }

    /// <summary>DialogSpeaker: stored in ScriptLine.speaker.</summary>
    [Test]
    public void DialogSpeaker_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)DialogSpeaker.Desk);
        Assert.AreEqual(1, (int)DialogSpeaker.Traveller);
        Assert.AreEqual(2, System.Enum.GetValues(typeof(DialogSpeaker)).Length, "a new member is appended here too");
    }

    /// <summary>DialogChoiceKind: the wheel orders by it; append only.</summary>
    [Test]
    public void DialogChoiceKind_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)DialogChoiceKind.Normal);
        Assert.AreEqual(1, (int)DialogChoiceKind.Back);
        Assert.AreEqual(2, (int)DialogChoiceKind.Request);
        Assert.AreEqual(3, (int)DialogChoiceKind.Question);
        Assert.AreEqual(4, (int)DialogChoiceKind.Look);
        Assert.AreEqual(5, (int)DialogChoiceKind.Dialog);
        Assert.AreEqual(6, System.Enum.GetValues(typeof(DialogChoiceKind)).Length, "a new member is appended here too");
    }
}
