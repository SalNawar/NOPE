using NUnit.Framework;

/// <summary>
/// Audit R1-003: every enum stored as an int in an asset or a save keeps its
/// values. Inserting or reordering a member would silently remap every
/// asset and save that stores it, so each is pinned here (append only).
/// </summary>
public class SerializedEnumsTests
{
    /// <summary>ClueCategory: stored in templates, books, places, effects, the config and saves (FactEdit, CarryRecord); phase 3 appends CitizenId, Destination, Incident, DepartureDate and Expiry; phase 6 AccountStatus, TransponderId, TransponderClass and Debt; phase 8 WaiverNo, Credit, Funds, PolicyNo and Signature; phase 9 Employer, Term and Wage.</summary>
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
        Assert.AreEqual(18, (int)ClueCategory.WaiverNo);
        Assert.AreEqual(19, (int)ClueCategory.Credit);
        Assert.AreEqual(20, (int)ClueCategory.Funds);
        Assert.AreEqual(21, (int)ClueCategory.PolicyNo);
        Assert.AreEqual(22, (int)ClueCategory.Signature);
        Assert.AreEqual(23, (int)ClueCategory.Employer);
        Assert.AreEqual(24, (int)ClueCategory.Term);
        Assert.AreEqual(25, (int)ClueCategory.Wage);
        Assert.AreEqual(26, System.Enum.GetValues(typeof(ClueCategory)).Length, "a new member is appended here too");
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

    /// <summary>LieKind: stored in DayPlanSO.lieKinds (world_source.json days[].lies).</summary>
    [Test]
    public void LieKind_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)LieKind.FalseOrigin);
        Assert.AreEqual(1, (int)LieKind.PoorPosingAsRich);
        Assert.AreEqual(2, (int)LieKind.DoctoredIdentity);
        Assert.AreEqual(3, (int)LieKind.FakeDisplaced);
        Assert.AreEqual(4, (int)LieKind.Smuggling);
        Assert.AreEqual(5, (int)LieKind.DebtorPosingAsTourist);
        Assert.AreEqual(6, (int)LieKind.ForgedContract);
        Assert.AreEqual(7, (int)LieKind.FakeWaiver);
        Assert.AreEqual(8, (int)LieKind.ForgedProof);
        Assert.AreEqual(9, System.Enum.GetValues(typeof(LieKind)).Length, "a new member is appended here too");
    }

    /// <summary>TravelRuleType: stored in TravelRuleSO.type (world_source.json rules[].type); phase 10 appends DressForDestination, phase 7 Procedure, phase 12 ReturnHome, phase 11 NoPresentGoods and PaperDates, phase 9 PaperSet and DebtStanding.</summary>
    [Test]
    public void TravelRuleType_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)TravelRuleType.EraForbidden);
        Assert.AreEqual(1, (int)TravelRuleType.NationForbidden);
        Assert.AreEqual(2, (int)TravelRuleType.NationEraForbidden);
        Assert.AreEqual(3, (int)TravelRuleType.DressForDestination);
        Assert.AreEqual(4, (int)TravelRuleType.Procedure);
        Assert.AreEqual(5, (int)TravelRuleType.ReturnHome);
        Assert.AreEqual(6, (int)TravelRuleType.NoPresentGoods);
        Assert.AreEqual(7, (int)TravelRuleType.PaperDates);
        Assert.AreEqual(8, (int)TravelRuleType.PaperSet);
        Assert.AreEqual(9, (int)TravelRuleType.DebtStanding);
        Assert.AreEqual(10, (int)TravelRuleType.TransponderRecall, "days 7-15: the Driftbox 3 recall");
        Assert.AreEqual(11, System.Enum.GetValues(typeof(TravelRuleType)).Length, "a new member is appended here too");
    }

    /// <summary>PlannedDirective: stored in DayPlanSO's forced slots (world_source.json days[].forced[].directive; days 7-15 B6).</summary>
    [Test]
    public void PlannedDirective_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)PlannedDirective.None);
        Assert.AreEqual(1, (int)PlannedDirective.EconomyManifest);
        Assert.AreEqual(2, (int)PlannedDirective.WaiverMissing);
        Assert.AreEqual(3, (int)PlannedDirective.WaiverUnsigned);
        Assert.AreEqual(4, (int)PlannedDirective.ProofMissing);
        Assert.AreEqual(5, (int)PlannedDirective.Frozen);
        Assert.AreEqual(6, (int)PlannedDirective.DepartureDate);
        Assert.AreEqual(7, (int)PlannedDirective.Expired);
        Assert.AreEqual(8, (int)PlannedDirective.Recalled);
        Assert.AreEqual(9, System.Enum.GetValues(typeof(PlannedDirective)).Length, "a new member is appended here too");
    }

    /// <summary>StorySection: stored in TimelineTriggerSO.section (world_source.json history.rules[].section; days 7-15 B10, Saleh's Q9).</summary>
    [Test]
    public void StorySection_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)StorySection.News);
        Assert.AreEqual(1, (int)StorySection.Desk);
        Assert.AreEqual(2, (int)StorySection.Return);
        Assert.AreEqual(3, System.Enum.GetValues(typeof(StorySection)).Length, "a new member is appended here too");
    }

    /// <summary>MissingFormVariant: stored in the content library's missing-form replies (interview.missingFormReplies).</summary>
    [Test]
    public void MissingFormVariant_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)MissingFormVariant.Honest);
        Assert.AreEqual(1, (int)MissingFormVariant.Missing);
        Assert.AreEqual(2, System.Enum.GetValues(typeof(MissingFormVariant)).Length, "a new member is appended here too");
    }

    /// <summary>TravellerKind: stored in CaseBlueprintSO.kind and the questions' WordingOverride.kinds.</summary>
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

    /// <summary>UpgradeVenue: stored in UpgradeSO.venue (the Orders app or Home; Saleh 2026-09-29, the portals spec v3 OR1).</summary>
    [Test]
    public void UpgradeVenue_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)UpgradeVenue.Orders);
        Assert.AreEqual(1, (int)UpgradeVenue.Home);
        Assert.AreEqual(2, System.Enum.GetValues(typeof(UpgradeVenue)).Length, "a new member is appended here too");
    }

    /// <summary>UpgradeBranch: stored in UpgradeSO.branch (the Orders tree's bands, the portals spec v3 OR2; then Home's five categories, the Home upgrades spec HU1).</summary>
    [Test]
    public void UpgradeBranch_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)UpgradeBranch.Desk);
        Assert.AreEqual(1, (int)UpgradeBranch.Interview);
        Assert.AreEqual(2, (int)UpgradeBranch.Portals);
        Assert.AreEqual(3, (int)UpgradeBranch.Contacts);
        Assert.AreEqual(4, (int)UpgradeBranch.Food);
        Assert.AreEqual(5, (int)UpgradeBranch.Housing);
        Assert.AreEqual(6, (int)UpgradeBranch.Security);
        Assert.AreEqual(7, (int)UpgradeBranch.Health);
        Assert.AreEqual(8, (int)UpgradeBranch.Comfort);
        Assert.AreEqual(9, System.Enum.GetValues(typeof(UpgradeBranch)).Length, "a new member is appended here too");
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

    /// <summary>The steps checklist's enums (redesign phase 21): stored in the content library's step sets (ContentLibrarySO.Pc.steps).</summary>
    [Test]
    public void StepEnums_KeepTheirSerializedInts()
    {
        Assert.AreEqual(0, (int)StepWhen.PapersReceived);
        Assert.AreEqual(1, (int)StepWhen.PaperRead);
        Assert.AreEqual(2, (int)StepWhen.Requested);
        Assert.AreEqual(3, (int)StepWhen.RulesViewed);
        Assert.AreEqual(4, (int)StepWhen.RecordViewed);
        Assert.AreEqual(5, (int)StepWhen.Compared);
        Assert.AreEqual(6, (int)StepWhen.Asked);
        Assert.AreEqual(7, (int)StepWhen.LookedAt);
        Assert.AreEqual(8, System.Enum.GetValues(typeof(StepWhen)).Length, "a new member is appended here too");

        Assert.AreEqual(0, (int)StatementKind.Any);
        Assert.AreEqual(1, (int)StatementKind.Field);
        Assert.AreEqual(2, (int)StatementKind.Answer);
        Assert.AreEqual(3, (int)StatementKind.Garment);
        Assert.AreEqual(4, System.Enum.GetValues(typeof(StatementKind)).Length, "a new member is appended here too");

        Assert.AreEqual(0, (int)TruthKind.Any);
        Assert.AreEqual(1, (int)TruthKind.Reference);
        Assert.AreEqual(2, (int)TruthKind.Record);
        Assert.AreEqual(3, (int)TruthKind.Paper);
        Assert.AreEqual(4, System.Enum.GetValues(typeof(TruthKind)).Length, "a new member is appended here too");

        Assert.AreEqual(0, (int)StepLink.None);
        Assert.AreEqual(1, (int)StepLink.Tab);
        Assert.AreEqual(2, (int)StepLink.PrimaryName);
        Assert.AreEqual(3, (int)StepLink.FirstUncheckedField);
        Assert.AreEqual(4, (int)StepLink.CostumeClaimed);
        Assert.AreEqual(5, System.Enum.GetValues(typeof(StepLink)).Length, "a new member is appended here too");
    }

    /// <summary>ReactionVerdict: stored in the reactions' rows (interview.reactions, interview.voices.reactions; the personalities spec's R2).</summary>
    [Test]
    public void ReactionVerdict_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)ReactionVerdict.Accepted);
        Assert.AreEqual(1, (int)ReactionVerdict.Denied);
        Assert.AreEqual(2, System.Enum.GetValues(typeof(ReactionVerdict)).Length, "a new member is appended here too");
    }

    /// <summary>ReactionIntent: stored in the reactions' rows (the personalities spec's R2).</summary>
    [Test]
    public void ReactionIntent_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)ReactionIntent.Honest);
        Assert.AreEqual(1, (int)ReactionIntent.Lying);
        Assert.AreEqual(2, System.Enum.GetValues(typeof(ReactionIntent)).Length, "a new member is appended here too");
    }
}
