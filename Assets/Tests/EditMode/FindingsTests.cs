using System;
using NUnit.Framework;

/// <summary>
/// The workbench's comparison rules (the PC workbench spec §4.2-§4.4):
/// FindingRules.Classify over two values (a proof always a difference),
/// AgainstToday over the calendar, RuleChecks over today's rules and
/// FindingLog's one entry per pair of values. Claim: Norvik, Medieval, by
/// Bjorn.
/// </summary>
public class FindingsTests
{
    private const string Nation = "norvik";
    private const string Era = "medieval";
    private const string Traveller = "Bjorn";
    private static readonly DateTime Today = new DateTime(2150, 9, 30);

    private static CompareEvidence Field(ClueCategory category, string value, bool tell = false, int document = 0) => new CompareEvidence
    {
        kind = EvidenceKind.DocumentField,
        category = category,
        value = value,
        isAnachronism = tell,
        document = document
    };

    private static CompareEvidence Book(ClueCategory category, string value, string nation = Nation, string era = Era) =>
        CompareEvidence.ForReferenceEntry(category, value, nation, era, nation + " (" + era + ")");

    private static CompareEvidence Record(ClueCategory category, string value, string owner = Traveller) =>
        CompareEvidence.ForRecordField(category, value, owner);

    private static FindingKind Classify(CompareEvidence a, CompareEvidence b) =>
        FindingRules.Classify(a, b, DiscrepancyLog.Prove(a, b, Nation, Era, Traveller), Nation, Era, Traveller);

    [Test]
    public void TwoDetails_AreDifferentDetails_AndNotLogged()
    {
        FindingKind kind = Classify(Field(ClueCategory.Name, "Bjorn"), Field(ClueCategory.BirthDate, "1 Apr 2097", document: 1));
        Assert.AreEqual(FindingKind.DifferentDetails, kind);
        Assert.IsFalse(FindingRules.IsLogged(kind));
        Assert.AreEqual(FindingLook.Info, FindingRules.Look(kind));
    }

    [Test]
    public void TheSameDetail_Matching_IsAMatch()
    {
        FindingKind kind = Classify(Field(ClueCategory.Destination, "Norvik (Medieval)"), Field(ClueCategory.Destination, "Norvik (Medieval)", document: 1));
        Assert.AreEqual(FindingKind.Match, kind);
        Assert.IsTrue(FindingRules.IsLogged(kind));
        Assert.IsFalse(FindingRules.IsDifference(kind));
    }

    [Test]
    public void AHonestPaper_AgainstTheClaimsBookRow_Matching_IsAMatch() =>
        Assert.AreEqual(FindingKind.Match, Classify(Field(ClueCategory.Technology, "Longship"), Book(ClueCategory.Technology, "Longship")));

    [Test]
    public void ATell_AgainstTheClaimsBookRow_IsAProvedDifference()
    {
        CompareEvidence tell = Field(ClueCategory.Technology, "Aqueduct", true), row = Book(ClueCategory.Technology, "Longship");
        Assert.IsNotNull(DiscrepancyLog.Prove(tell, row, Nation, Era, Traveller));
        Assert.AreEqual(FindingKind.Differs, Classify(tell, row));
        Assert.AreEqual(FindingKind.Differs, Classify(row, tell), "either order");
    }

    [Test]
    public void ATell_MatchingAnotherPlacesRow_BelongsElsewhere()
    {
        FindingKind kind = Classify(Field(ClueCategory.Technology, "Aqueduct", true), Book(ClueCategory.Technology, "Aqueduct", "latia", "ancient"));
        Assert.AreEqual(FindingKind.Elsewhere, kind);
        Assert.IsTrue(FindingRules.IsDifference(kind));
    }

    [Test]
    public void AnotherPlacesRow_ProvingNothing_IsNotTheClaimedPlace() =>
        Assert.AreEqual(FindingKind.OtherPlace, Classify(Field(ClueCategory.Technology, "Longship"), Book(ClueCategory.Technology, "Aqueduct", "latia", "ancient")));

    [Test]
    public void AnotherPersonsRecord_IsOtherPerson() =>
        Assert.AreEqual(FindingKind.OtherPerson, Classify(Field(ClueCategory.BirthDate, "1 Apr 2097"), Record(ClueCategory.BirthDate, "3 May 2090", "Astrid")));

    [Test]
    public void TheTravellersRecord_Differing_OnATell_IsAProvedDifference() =>
        Assert.AreEqual(FindingKind.Differs, Classify(Field(ClueCategory.BirthDate, "1 Apr 2097", true), Record(ClueCategory.BirthDate, "3 May 2090")));

    private static CompareEvidence Seal(string seal, string office, bool tell = false, int document = 0) =>
        CompareEvidence.FromDocumentField(new DocumentField { category = ClueCategory.Seal, label = "Issuing Seal", value = seal, isAnachronism = tell, issuer = office }, document);

    [Test]
    public void APapersSeal_AgainstItsOfficesRegisterSeal_Matching_IsAMatch()
    {
        CompareEvidence row = CompareEvidence.ForSealRow("Blue hexagon · VO", "visa", "Visa Office");
        Assert.AreEqual(FindingKind.Match, Classify(Seal("Blue hexagon · VO", "visa"), row));
        Assert.AreEqual(FindingKind.Match, Classify(row, Seal("Blue hexagon · VO", "visa")), "either order");
    }

    [Test]
    public void AForgedSeal_AgainstItsOfficesRegisterSeal_IsSealIncorrect_ADifference()
    {
        FindingKind kind = Classify(Seal("Red hexagon · VO", "visa", true), CompareEvidence.ForSealRow("Blue hexagon · VO", "visa", "Visa Office"));
        Assert.AreEqual(FindingKind.SealIncorrect, kind);
        Assert.AreEqual(2, DiscrepancyLog.Prove(Seal("Red hexagon · VO", "visa", true, document: 2), CompareEvidence.ForSealRow("Blue hexagon · VO", "visa", "Visa Office"), Nation, Era, Traveller).statementDocument,
                        "the proof names the paper the seal is on (the wheel's question: \"The seal on your <paper> ...\")");
        Assert.IsTrue(FindingRules.IsLogged(kind));
        Assert.IsTrue(FindingRules.IsDifference(kind));
        Assert.AreEqual("finding.link.SealIncorrect", FindingRules.LinkKey(kind));
    }

    [Test]
    public void ABorrowedSeal_AgainstTheOfficeItBelongsTo_IsSealIncorrect() =>
        Assert.AreEqual(FindingKind.SealIncorrect,
                        Classify(Seal("Green shield · LB", "visa", true), CompareEvidence.ForSealRow("Green shield · LB", "labour", "Labour Placement Bureau")));

    [Test]
    public void ASeal_AgainstAnotherOfficesSeal_IsAnotherOffice_NotLogged()
    {
        CompareEvidence labour = CompareEvidence.ForSealRow("Green shield · LB", "labour", "Labour Placement Bureau");
        Assert.AreEqual(FindingKind.OtherOffice, Classify(Seal("Blue hexagon · VO", "visa"), labour));
        Assert.AreEqual(FindingKind.OtherOffice, Classify(Seal("Blue hexagon · VO", "visa"), Seal("Green shield · LB", "labour", document: 1)),
                        "two papers of two offices: never a difference to deny on");
        Assert.IsFalse(FindingRules.IsLogged(FindingKind.OtherOffice));
        Assert.AreEqual(FindingKind.TwoTruths, Classify(CompareEvidence.ForSealRow("Blue hexagon · VO", "visa", "Visa Office"), labour));
    }

    [Test]
    public void APhoto_AgainstThePerson_MatchesTheirOwn_DiffersFromAStrangers()
    {
        CompareEvidence person = CompareEvidence.ForPerson("skin2|hair1");
        CompareEvidence own = Field(ClueCategory.Photo, "skin2|hair1");
        CompareEvidence stranger = Field(ClueCategory.Photo, "skin5|hair3", true);
        Assert.AreEqual(FindingKind.Match, Classify(own, person));
        Assert.AreEqual(FindingKind.Differs, Classify(person, stranger));
        Assert.IsNotNull(DiscrepancyLog.Prove(stranger, person, Nation, Era, Traveller), "a proof: logged as evidence");
        Assert.AreEqual(FindingKind.DifferentDetails, Classify(person, Field(ClueCategory.Name, "Bjorn")));
    }

    [Test]
    public void TwoTruths_AreNotLogged() =>
        Assert.AreEqual(FindingKind.TwoTruths, Classify(Book(ClueCategory.Technology, "Longship"), Book(ClueCategory.Technology, "Aqueduct", "latia", "ancient")));

    [Test]
    public void AnAnswer_AgainstAPaper_Differing_IsADifferenceWithoutProof()
    {
        CompareEvidence answer = CompareEvidence.ForAnswer(ClueCategory.Currency, "Denarius", true);
        CompareEvidence paper = Field(ClueCategory.Currency, "Penny");
        Assert.IsNull(DiscrepancyLog.Prove(answer, paper, Nation, Era, Traveller), "an answer against a paper hints, never proves");
        Assert.AreEqual(FindingKind.Differs, Classify(answer, paper));
    }

    [Test]
    public void Today_AgainstADeparture_IsTodayOrNot()
    {
        Assert.AreEqual(FindingKind.DepartsToday, FindingRules.AgainstToday(ClueCategory.DepartureDate, "30 Sep 2150", Today));
        Assert.AreEqual(FindingKind.NotToday, FindingRules.AgainstToday(ClueCategory.DepartureDate, "2 Oct 2150", Today));
        Assert.IsTrue(FindingRules.IsDifference(FindingKind.NotToday));
    }

    [Test]
    public void Today_AgainstAValidUntil_IsValidOrExpired()
    {
        Assert.AreEqual(FindingKind.StillValid, FindingRules.AgainstToday(ClueCategory.Expiry, "30 Sep 2150", Today));
        Assert.AreEqual(FindingKind.Expired, FindingRules.AgainstToday(ClueCategory.Expiry, "29 Sep 2150", Today));
    }

    [Test]
    public void Today_AgainstAnythingElse_OrAnUnreadableDate_IsADifferentDetail()
    {
        Assert.AreEqual(FindingKind.DifferentDetails, FindingRules.AgainstToday(ClueCategory.BirthDate, "30 Sep 2150", Today));
        Assert.AreEqual(FindingKind.DifferentDetails, FindingRules.AgainstToday(ClueCategory.DepartureDate, "soon", Today));
    }

    [Test]
    public void EveryKind_HasOneLook_AndOnlyTheLoggedOnesCount()
    {
        foreach (FindingKind kind in (FindingKind[])Enum.GetValues(typeof(FindingKind)))
        {
            Assert.AreEqual(FindingRules.Look(kind) != FindingLook.Info, FindingRules.IsLogged(kind), kind.ToString());
            Assert.AreEqual("finding.link." + kind, FindingRules.LinkKey(kind));
        }
    }

    [Test]
    public void RuleVerdicts_MapToFindings()
    {
        Assert.AreEqual(FindingKind.RuleMet, FindingRules.Of(RuleVerdict.Meets));
        Assert.AreEqual(FindingKind.RuleBroken, FindingRules.Of(RuleVerdict.Breaks));
        Assert.AreEqual(FindingKind.RuleNotAbout, FindingRules.Of(RuleVerdict.NotAbout));
        Assert.AreEqual(FindingKind.RuleByComparison, FindingRules.Of(RuleVerdict.ByComparison));
        Assert.AreEqual(FindingKind.RuleNotForTraveller, FindingRules.Of(RuleVerdict.NotForTraveller));
    }

    private static Finding F(FindingKind kind, string a, string b) => new Finding(kind, a, b, "A", "1", "B", "2", "Detail", null);

    [Test]
    public void TheLog_KeepsOneFindingPerPair_InEitherOrder()
    {
        var log = new FindingLog();
        Assert.IsTrue(log.Add(F(FindingKind.Match, "field:0:1", "field:1:1")));
        Assert.IsFalse(log.Add(F(FindingKind.Match, "field:1:1", "field:0:1")), "the same two values, the other way round");
        Assert.IsTrue(log.Add(F(FindingKind.Differs, "field:0:2", "book:Currency:norvik:medieval")));
        Assert.AreEqual(2, log.Count);
        Assert.IsNotNull(log.Find("book:Currency:norvik:medieval", "field:0:2"));
    }

    [Test]
    public void TheLog_RefusesNotes_AndCountsDifferences()
    {
        var log = new FindingLog();
        Assert.IsFalse(log.Add(F(FindingKind.DifferentDetails, "a", "b")));
        Assert.IsFalse(log.Add(null));
        Assert.IsFalse(log.HasDifference);
        log.Add(F(FindingKind.Match, "a", "b"));
        Assert.IsFalse(log.HasDifference, "a match is not a difference");
        log.Add(F(FindingKind.RuleBroken, "rule:0", "field:0:3"));
        log.Add(F(FindingKind.Expired, "date:today", "field:1:4"));
        Assert.IsTrue(log.HasDifference);
        Assert.AreEqual(2, log.Differences);
        Assert.AreEqual("rule:0", log.FirstDifference.KeyA);
        log.Clear();
        Assert.AreEqual(0, log.Count);
    }
}

/// <summary>Today's rules held against a value (RuleChecks.Check; the PC workbench spec §4.3).</summary>
public class RuleChecksTests
{
    private static readonly DateTime Today = new DateTime(2150, 9, 30);
    private static readonly TravellerKind[] Everyone = Array.Empty<TravellerKind>();

    private static CaseFacts Facts(TravellerKind kind = TravellerKind.PoorTourist) => new CaseFacts
    {
        Kind = kind,
        VisaClass = CitizenStatus.Standard,
        ManifestClass = TransponderClass.Economy,
        Forms = new[] { Directives.Visa, Directives.Manifest, Directives.Waiver, "TC-415" },
        WaiverSigned = true,
        Today = Today
    };

    [Test]
    public void AClosure_IsAboutTheDestination_AndBreaksOnTheClosedClaim()
    {
        var closure = new Directive(TravelRuleType.NationEraForbidden, Everyone, "norvik", "medieval");
        Assert.AreEqual(RuleVerdict.Breaks, RuleChecks.Check(closure, ClueCategory.Destination, "Norvik (Medieval)", Facts(), "norvik", "medieval"));
        Assert.AreEqual(RuleVerdict.Meets, RuleChecks.Check(closure, ClueCategory.Destination, "Latia (Ancient)", Facts(), "latia", "ancient"));
        Assert.AreEqual(RuleVerdict.NotAbout, RuleChecks.Check(closure, ClueCategory.Currency, "Penny", Facts(), "norvik", "medieval"));
    }

    [Test]
    public void ThePaperSet_BreaksOnAMissingWaiver_WhicheverSetDetailItIsHeldAgainst()
    {
        var rule = new Directive(TravelRuleType.PaperSet, Everyone);
        CaseFacts complete = Facts();
        Assert.AreEqual(RuleVerdict.Meets, RuleChecks.Check(rule, ClueCategory.AccountStatus, "Standard", complete, "n", "e"));
        CaseFacts missing = Facts();
        missing.Forms = new[] { Directives.Visa, Directives.Manifest, "TC-415" };
        missing.WaiverSigned = false;
        Assert.AreEqual(RuleVerdict.Breaks, RuleChecks.Check(rule, ClueCategory.AccountStatus, "Standard", missing, "n", "e"));
        Assert.AreEqual(RuleVerdict.Breaks, RuleChecks.Check(rule, ClueCategory.TransponderClass, "Economy", missing, "n", "e"));
        Assert.AreEqual(RuleVerdict.NotAbout, RuleChecks.Check(rule, ClueCategory.BirthDate, "1 Apr 2097", missing, "n", "e"));
    }

    [Test]
    public void TheDebtStanding_BreaksOnAFrozenAccount()
    {
        var rule = new Directive(TravelRuleType.DebtStanding, Everyone);
        CaseFacts frozen = Facts();
        frozen.Frozen = true;
        Assert.AreEqual(RuleVerdict.Breaks, RuleChecks.Check(rule, ClueCategory.Debt, "125,000 cr", frozen, "n", "e"));
        Assert.AreEqual(RuleVerdict.Meets, RuleChecks.Check(rule, ClueCategory.AccountStatus, "Standard", Facts(), "n", "e"));
        Assert.AreEqual(RuleVerdict.NotAbout, RuleChecks.Check(rule, ClueCategory.Destination, "x", frozen, "n", "e"));
    }

    [Test]
    public void ThePapersDates_ReadTheValueHeldAgainstThem()
    {
        var rule = new Directive(TravelRuleType.PaperDates, Everyone);
        Assert.AreEqual(RuleVerdict.Meets, RuleChecks.Check(rule, ClueCategory.DepartureDate, "30 Sep 2150", Facts(), "n", "e"));
        Assert.AreEqual(RuleVerdict.Breaks, RuleChecks.Check(rule, ClueCategory.DepartureDate, "1 Oct 2150", Facts(), "n", "e"));
        Assert.AreEqual(RuleVerdict.Meets, RuleChecks.Check(rule, ClueCategory.Expiry, "12 Dec 2150", Facts(), "n", "e"));
        Assert.AreEqual(RuleVerdict.Breaks, RuleChecks.Check(rule, ClueCategory.Expiry, "1 Sep 2150", Facts(), "n", "e"));
        Assert.AreEqual(RuleVerdict.NotAbout, RuleChecks.Check(rule, ClueCategory.Expiry, "whenever", Facts(), "n", "e"));
    }

    [Test]
    public void ARecall_BreaksOnTheRecalledModel()
    {
        var rule = new Directive(TravelRuleType.TransponderRecall, Everyone, transponder: "tt6");
        CaseFacts recalled = Facts();
        recalled.ManifestModelId = "tt6";
        Assert.AreEqual(RuleVerdict.Breaks, RuleChecks.Check(rule, ClueCategory.TransponderId, "TT-6 · 67947", recalled, "n", "e"));
        Assert.AreEqual(RuleVerdict.Meets, RuleChecks.Check(rule, ClueCategory.TransponderId, "TT-9 · 12345", Facts(), "n", "e"));
    }

    [Test]
    public void ARuleWithoutAPredicate_IsCheckedByComparing()
    {
        foreach (TravelRuleType type in new[] { TravelRuleType.DressForDestination, TravelRuleType.Procedure, TravelRuleType.ReturnHome, TravelRuleType.NoPresentGoods })
            Assert.AreEqual(RuleVerdict.ByComparison, RuleChecks.Check(new Directive(type, Everyone), ClueCategory.Culture, "Toga", Facts(), "n", "e"), type.ToString());
    }

    [Test]
    public void ARuleForAnotherKind_IsNotForTheTraveller()
    {
        var labourers = new Directive(TravelRuleType.PaperSet, new[] { TravellerKind.Labourer });
        Assert.AreEqual(RuleVerdict.NotForTraveller, RuleChecks.Check(labourers, ClueCategory.AccountStatus, "Standard", Facts(TravellerKind.RichTourist), "n", "e"));
    }
}

/// <summary>The guided steps (CaseGuide; the PC workbench spec IA2, IA3).</summary>
public class CaseGuideTests
{
    [Test]
    public void TheSteps_ArePapersRecordsBooksRulesDecision() =>
        CollectionAssert.AreEqual(new[] { GuideStage.Papers, GuideStage.Records, GuideStage.Books, GuideStage.Rules, GuideStage.Decision }, CaseGuide.Stages);

    [Test]
    public void NextAndBack_StopAtTheEnds_AndAnyStepCanBeGoneTo()
    {
        var guide = new CaseGuide();
        guide.Reset();
        Assert.IsTrue(guide.IsFirst);
        Assert.IsFalse(guide.Back());
        Assert.IsTrue(guide.Next());
        Assert.AreEqual(GuideStage.Records, guide.Current);
        Assert.AreEqual(2, guide.Number);
        Assert.IsTrue(guide.Go(GuideStage.Decision));
        Assert.IsTrue(guide.IsLast);
        Assert.IsFalse(guide.Next());
        Assert.IsFalse(guide.Go(GuideStage.Decision), "already there");
        Assert.IsTrue(guide.WasLeft(GuideStage.Papers) && guide.WasLeft(GuideStage.Records));
        Assert.IsFalse(guide.WasLeft(GuideStage.Books), "never visited");
        guide.Reset();
        Assert.AreEqual(GuideStage.Papers, guide.Current);
        Assert.IsFalse(guide.WasLeft(GuideStage.Papers));
    }

    private static StepSpec Spec(string id, StepWhen when, TruthKind truth = TruthKind.Any) => new StepSpec { id = id, when = when, truth = truth };

    [Test]
    public void EachChecklistItem_BelongsToOneStep()
    {
        Assert.AreEqual(GuideStage.Papers, CaseGuide.StageOf(Spec("papers", StepWhen.PapersReceived)));
        Assert.AreEqual(GuideStage.Papers, CaseGuide.StageOf(Spec("read", StepWhen.PaperRead)));
        Assert.AreEqual(GuideStage.Papers, CaseGuide.StageOf(Spec("questions", StepWhen.Asked)));
        Assert.AreEqual(GuideStage.Papers, CaseGuide.StageOf(Spec("returnOrder", StepWhen.Compared, TruthKind.Paper)));
        Assert.AreEqual(GuideStage.Records, CaseGuide.StageOf(Spec("identity", StepWhen.Compared, TruthKind.Record)));
        Assert.AreEqual(GuideStage.Records, CaseGuide.StageOf(Spec("standing", StepWhen.RecordViewed)));
        Assert.AreEqual(GuideStage.Books, CaseGuide.StageOf(Spec("facts", StepWhen.Compared, TruthKind.Reference)));
        Assert.AreEqual(GuideStage.Rules, CaseGuide.StageOf(Spec("rules", StepWhen.RulesViewed)));
    }

    [Test]
    public void AStep_IsDone_WhenItsChecksAre_OrOnceLeftWithoutChecks()
    {
        var steps = new[] { Spec("papers", StepWhen.PapersReceived), Spec("identity", StepWhen.Compared, TruthKind.Record), Spec("read", StepWhen.PaperRead) };
        var states = new[] { new StepState("papers", true, 2, 2, false), new StepState("identity", false, 0, 1, false), new StepState("read", true, 2, 2, false) };
        StageChecks papers = CaseGuide.Checks(GuideStage.Papers, steps, states);
        Assert.AreEqual(2, papers.Total);
        Assert.AreEqual(2, papers.Done);
        StageChecks records = CaseGuide.Checks(GuideStage.Records, steps, states);
        Assert.AreEqual(1, records.Total);
        Assert.AreEqual(0, records.Done);

        var guide = new CaseGuide();
        guide.Reset();
        Assert.IsTrue(guide.IsDone(GuideStage.Papers, papers));
        Assert.IsFalse(guide.IsDone(GuideStage.Records, records));
        StageChecks books = CaseGuide.Checks(GuideStage.Books, steps, states);
        Assert.AreEqual(0, books.Total);
        Assert.IsFalse(guide.IsDone(GuideStage.Books, books), "not visited yet");
        guide.Go(GuideStage.Books);
        guide.Go(GuideStage.Rules);
        Assert.IsTrue(guide.IsDone(GuideStage.Books, books), "left with no checks of its own");
        guide.Go(GuideStage.Decision);
        guide.Go(GuideStage.Papers);
        Assert.IsFalse(guide.IsDone(GuideStage.Decision, new StageChecks(0, 0)), "the decision is never done");
    }
}
