using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The Directives' rules (traveller types P3, P4, F7, §5.3-5.4; redesign
/// phases 9, 11 and 12): the closure types, a rule's first day and the
/// guarantee table; the PaperDates directive (a departure dated another day,
/// an expired Valid Until, in that order; unreadable dates skipped) and its
/// maker's draws; each type's decision table over CaseFacts, the fault a
/// broken rule is, the paper-set maker's variants and pick, the violation
/// roll, and the content checks Generate World and the validator share.
/// Today is 17 Mar 2150, day 4 of the agency calendar.
/// </summary>
public class DirectivesTests
{
    private static readonly DateTime Today = new DateTime(2150, 3, 17);

    private static string On(int daysFromToday) => AgencyCalendar.Write(Today.AddDays(daysFromToday));

    private static ScriptedRandom Script(params ScriptStep[] steps) => new ScriptedRandom(steps);
    private static ScriptStep V(float roll) => ScriptStep.Value(roll);
    private static ScriptStep R(int offset) => ScriptStep.Range(offset);

    private static readonly string[] RichSet = { Directives.Visa, Directives.Manifest };
    private static readonly string[] PoorSet = { Directives.Visa, Directives.Manifest, Directives.Waiver, "TC-416" };
    private static readonly string[] LabourSet = { Directives.Contract, Directives.Manifest, Directives.Waiver };
    private static readonly string[] DisplacedSet = { "TC-610", "TC-620", "TC-630" };

    /// <summary>Each kind's honest set of forms.</summary>
    private static string[] SetOf(TravellerKind kind) =>
        kind == TravellerKind.RichTourist ? RichSet : kind == TravellerKind.PoorTourist ? PoorSet : kind == TravellerKind.Labourer ? LabourSet : DisplacedSet;

    /// <summary>An honest traveller's facts: every form of the set carried, the classes of the kind, the waiver signed, the account Good, the destination open.</summary>
    private static CaseFacts Honest(TravellerKind kind, params string[] forms) => new CaseFacts
    {
        Kind = kind,
        ClosedDestination = false,
        VisaClass = kind == TravellerKind.RichTourist ? CitizenStatus.Premium : kind == TravellerKind.PoorTourist ? CitizenStatus.Standard : (CitizenStatus?)null,
        ManifestClass = forms.Contains(Directives.Manifest) ? (kind == TravellerKind.RichTourist ? TransponderClass.Premium : TransponderClass.Economy) : (TransponderClass?)null,
        Forms = forms,
        WaiverSigned = forms.Contains(Directives.Waiver),
        Frozen = false
    };

    // -----------------------------
    // The types, the first day and the guarantees
    // -----------------------------

    [TestCase(TravelRuleType.EraForbidden, true)]
    [TestCase(TravelRuleType.NationForbidden, true)]
    [TestCase(TravelRuleType.NationEraForbidden, true)]
    [TestCase(TravelRuleType.DressForDestination, false)]
    [TestCase(TravelRuleType.Procedure, false)]
    [TestCase(TravelRuleType.ReturnHome, false)]
    [TestCase(TravelRuleType.NoPresentGoods, false)]
    [TestCase(TravelRuleType.PaperDates, false)]
    [TestCase(TravelRuleType.PaperSet, false)]
    [TestCase(TravelRuleType.DebtStanding, false)]
    public void IsClosure_TheThreeForbiddenTypes(TravelRuleType type, bool expected)
    {
        Assert.AreEqual(expected, Directives.IsClosure(type));
    }

    [Test]
    public void FirstDay_TheSmallestDayListed_ZeroForNone()
    {
        Assert.AreEqual(0, Directives.FirstDay(null));
        Assert.AreEqual(0, Directives.FirstDay(new int[0]));
        Assert.AreEqual(5, Directives.FirstDay(new[] { 5, 6 }));
        Assert.AreEqual(5, Directives.FirstDay(new[] { 6, 5, 7 }), "unordered plans");
    }

    /// <summary>P4: a closure guarantees a violator every day; the procedures with a maker (the return home, no 2150 goods, the papers' dates, the paper set, the debt standing and dress) a breaker on their first day only; a procedure line never.</summary>
    [TestCase(TravelRuleType.EraForbidden, 3, 2, true)]
    [TestCase(TravelRuleType.NationForbidden, 2, 2, true)]
    [TestCase(TravelRuleType.NationEraForbidden, 6, 2, true)]
    [TestCase(TravelRuleType.ReturnHome, 5, 5, true)]
    [TestCase(TravelRuleType.ReturnHome, 6, 5, false)]
    [TestCase(TravelRuleType.ReturnHome, 5, 0, false, Description = "a rule no plan lists guarantees nothing")]
    [TestCase(TravelRuleType.NoPresentGoods, 4, 4, true)]
    [TestCase(TravelRuleType.NoPresentGoods, 5, 4, false)]
    [TestCase(TravelRuleType.PaperDates, 4, 4, true)]
    [TestCase(TravelRuleType.PaperDates, 6, 4, false)]
    [TestCase(TravelRuleType.PaperDates, 4, 0, false)]
    [TestCase(TravelRuleType.PaperSet, 2, 2, true)]
    [TestCase(TravelRuleType.PaperSet, 3, 2, false)]
    [TestCase(TravelRuleType.DebtStanding, 3, 3, true)]
    [TestCase(TravelRuleType.DebtStanding, 4, 3, false)]
    [TestCase(TravelRuleType.DressForDestination, 2, 2, true)]
    [TestCase(TravelRuleType.DressForDestination, 3, 2, false)]
    [TestCase(TravelRuleType.Procedure, 1, 1, false)]
    public void Guarantees_ClosuresEveryDay_ProceduresOnTheirFirstDay(TravelRuleType type, int today, int firstDay, bool expected)
    {
        Assert.AreEqual(expected, Directives.Guarantees(type, today, firstDay));
    }

    [Test]
    public void IsRolled_ThePaperSetTheDebtStandingThePapersDatesAndTheRecall()
    {
        CollectionAssert.AreEquivalent(new[] { TravelRuleType.PaperSet, TravelRuleType.DebtStanding, TravelRuleType.PaperDates, TravelRuleType.TransponderRecall },
                                       System.Enum.GetValues(typeof(TravelRuleType)).Cast<TravelRuleType>().Where(Directives.IsRolled).ToList());
    }

    [Test]
    public void HasMaker_AndIsRolled_IncludeTheRecall()
    {
        Assert.IsTrue(Directives.HasMaker(TravelRuleType.TransponderRecall));
        Assert.IsTrue(Directives.IsRolled(TravelRuleType.TransponderRecall));
    }

    [TestCase(10, 10, true)]
    [TestCase(11, 10, false)]
    [TestCase(15, 10, false)]
    public void Guarantees_TheRecallOnItsFirstDayOnly(int today, int firstDay, bool expected)
    {
        Assert.AreEqual(expected, Directives.Guarantees(TravelRuleType.TransponderRecall, today, firstDay));
    }

    // ---- the Driftbox 3 recall (days 7-15 §6) ----

    private static readonly Directive Recall = new Directive(TravelRuleType.TransponderRecall, new[] { TravellerKind.PoorTourist, TravellerKind.Labourer }, transponder: "driftbox3");

    private static CaseFacts OnUnit(TravellerKind kind, string modelId)
    {
        CaseFacts facts = Honest(kind, SetOf(kind));
        facts.ManifestModelId = modelId;
        return facts;
    }

    [Test]
    public void Breaks_TransponderRecall_ARecalledModelOnTheManifest()
    {
        Assert.IsTrue(Directives.Breaks(Recall, OnUnit(TravellerKind.PoorTourist, "driftbox3")));
        Assert.AreEqual(DirectiveFault.RecalledTransponder, Directives.Fault(new[] { Recall }, OnUnit(TravellerKind.Labourer, "driftbox3")));
    }

    [Test]
    public void Breaks_TransponderRecall_AnotherModel()
    {
        Assert.IsFalse(Directives.Breaks(Recall, OnUnit(TravellerKind.PoorTourist, "ticktock")));
        Assert.AreEqual(DirectiveFault.None, Directives.Fault(new[] { Recall }, OnUnit(TravellerKind.RichTourist, "driftbox3")), "the rule is read for the Economy kinds only");
    }

    [Test]
    public void Breaks_TransponderRecall_NoManifestBreaksNothing()
    {
        Assert.IsFalse(Directives.Breaks(Recall, OnUnit(TravellerKind.PoorTourist, null)), "no manifest: the paper set catches it");
        Assert.IsFalse(Directives.Breaks(new Directive(TravelRuleType.TransponderRecall, null), OnUnit(TravellerKind.PoorTourist, "driftbox3")), "a recall naming no model recalls nothing");
    }

    [Test]
    public void FaultOf_TransponderRecall_IsRecalledTransponder()
    {
        Assert.AreEqual(DirectiveFault.RecalledTransponder, Directives.FaultOf(TravelRuleType.TransponderRecall));
    }

    [Test]
    public void CanBreak_TheRecall_AnEconomyKindCarryingAManifest()
    {
        Assert.IsTrue(Directives.CanBreak(TravelRuleType.TransponderRecall, TravellerKind.PoorTourist, PoorSet));
        Assert.IsTrue(Directives.CanBreak(TravelRuleType.TransponderRecall, TravellerKind.Labourer, LabourSet));
        Assert.IsFalse(Directives.CanBreak(TravelRuleType.TransponderRecall, TravellerKind.RichTourist, RichSet), "a Premium account travels Premium");
        Assert.IsFalse(Directives.CanBreak(TravelRuleType.TransponderRecall, TravellerKind.Displaced, DisplacedSet));
        Assert.IsFalse(Directives.CanBreak(TravelRuleType.TransponderRecall, TravellerKind.PoorTourist, new[] { Directives.Visa }), "no manifest to print the unit on");
    }

    [Test]
    public void ModelIdOf_ReadsThePrintedModel()
    {
        var models = new List<TransponderModel>
        {
            new TransponderModel { id = "driftbox3", model = "Driftbox 3", prefix = "DB", transponderClass = TransponderClass.Economy },
            new TransponderModel { id = "skiplite", model = "Skip Lite", prefix = "SL", transponderClass = TransponderClass.Economy }
        };
        Assert.AreEqual("driftbox3", RecordLies.ModelIdOf("Driftbox 3 · DB-00412", models));
        Assert.AreEqual("skiplite", RecordLies.ModelIdOf("Skip Lite · SL-99999", models));
        Assert.IsNull(RecordLies.ModelIdOf("TransponderId:none", models));
        Assert.IsNull(RecordLies.ModelIdOf(null, models));
    }

    [TestCase(TravelRuleType.EraForbidden, DirectiveFault.ClosedDestination)]
    [TestCase(TravelRuleType.NationForbidden, DirectiveFault.ClosedDestination)]
    [TestCase(TravelRuleType.NationEraForbidden, DirectiveFault.ClosedDestination)]
    [TestCase(TravelRuleType.PaperSet, DirectiveFault.IncompletePapers)]
    [TestCase(TravelRuleType.DebtStanding, DirectiveFault.FrozenAccount)]
    [TestCase(TravelRuleType.PaperDates, DirectiveFault.WrongDepartureDate)]
    [TestCase(TravelRuleType.NoPresentGoods, DirectiveFault.None)]
    [TestCase(TravelRuleType.DressForDestination, DirectiveFault.None)]
    [TestCase(TravelRuleType.ReturnHome, DirectiveFault.None)]
    [TestCase(TravelRuleType.Procedure, DirectiveFault.None)]
    public void FaultOf_EachType(TravelRuleType type, DirectiveFault fault)
    {
        Assert.AreEqual(fault, Directives.FaultOf(type));
    }

    [Test]
    public void ADirective_AppliesToItsKinds_OrToEveryKindWhenNoneIsListed()
    {
        var labour = new Directive(TravelRuleType.PaperSet, new[] { TravellerKind.Labourer });
        Assert.IsTrue(labour.AppliesTo(TravellerKind.Labourer));
        Assert.IsFalse(labour.AppliesTo(TravellerKind.RichTourist));
        var all = new Directive(TravelRuleType.NationForbidden, null);
        foreach (TravellerKind kind in (TravellerKind[])System.Enum.GetValues(typeof(TravellerKind)))
            Assert.IsTrue(all.AppliesTo(kind), kind.ToString());
    }

    // -----------------------------
    // Breaks: the decision table
    // -----------------------------

    [TestCase(TravellerKind.RichTourist)]
    [TestCase(TravellerKind.PoorTourist)]
    [TestCase(TravellerKind.Labourer)]
    [TestCase(TravellerKind.Displaced)]
    public void AnHonestTraveller_BreaksNothing(TravellerKind kind)
    {
        CaseFacts facts = Honest(kind, SetOf(kind));
        foreach (TravelRuleType type in (TravelRuleType[])System.Enum.GetValues(typeof(TravelRuleType)))
            Assert.IsFalse(Directives.Breaks(type, facts), $"{kind} breaks {type}");
    }

    [Test]
    public void AClosure_BreaksOnAClosedDestination_ForEveryKind()
    {
        foreach (TravellerKind kind in (TravellerKind[])System.Enum.GetValues(typeof(TravellerKind)))
        {
            CaseFacts facts = Honest(kind, SetOf(kind));
            facts.ClosedDestination = true;
            Assert.IsTrue(Directives.Breaks(TravelRuleType.NationEraForbidden, facts), kind.ToString());
            Assert.IsTrue(Directives.Breaks(TravelRuleType.EraForbidden, facts), kind.ToString());
            Assert.IsTrue(Directives.Breaks(TravelRuleType.NationForbidden, facts), kind.ToString());
            Assert.IsFalse(Directives.Breaks(TravelRuleType.PaperSet, facts), $"{kind}: a closure is not a paper-set fault");
        }
    }

    [Test]
    public void ThePaperSet_APremiumVisa_NeedsAPremiumManifest()
    {
        CaseFacts facts = Honest(TravellerKind.RichTourist, RichSet);
        facts.ManifestClass = TransponderClass.Economy;
        Assert.IsTrue(Directives.Breaks(TravelRuleType.PaperSet, facts));

        facts.ManifestClass = null;
        Assert.IsFalse(Directives.Breaks(TravelRuleType.PaperSet, facts), "a manifest not handed over yet states nothing about its class");

        facts.VisaClass = null;
        facts.ManifestClass = TransponderClass.Economy;
        Assert.IsFalse(Directives.Breaks(TravelRuleType.PaperSet, facts), "without a visa there is no class to read against");
    }

    [Test]
    public void ThePaperSet_AStandardVisa_NeedsAnEconomyManifest_ASignedWaiver_AndAProof()
    {
        CaseFacts facts = Honest(TravellerKind.PoorTourist, PoorSet);
        Assert.IsFalse(Directives.Breaks(TravelRuleType.PaperSet, facts));

        facts.ManifestClass = TransponderClass.Premium;
        Assert.IsTrue(Directives.Breaks(TravelRuleType.PaperSet, facts), "a Premium unit on a Standard visa");
        facts.ManifestClass = TransponderClass.Economy;

        facts.WaiverSigned = false;
        Assert.IsTrue(Directives.Breaks(TravelRuleType.PaperSet, facts), "an unsigned or missing waiver");
        facts.WaiverSigned = true;

        facts.Forms = new[] { Directives.Visa, Directives.Manifest, Directives.Waiver };
        Assert.IsTrue(Directives.Breaks(TravelRuleType.PaperSet, facts), "no proof of means");
        foreach (string proof in Directives.Proofs)
        {
            facts.Forms = new[] { Directives.Visa, Directives.Manifest, Directives.Waiver, proof };
            Assert.IsFalse(Directives.Breaks(TravelRuleType.PaperSet, facts), $"{proof} is a proof of means");
        }
    }

    [Test]
    public void ThePaperSet_ALabourer_NeedsAContract_AnEconomyManifest_AndASignedWaiver()
    {
        CaseFacts facts = Honest(TravellerKind.Labourer, LabourSet);
        Assert.IsFalse(Directives.Breaks(TravelRuleType.PaperSet, facts));

        facts.Forms = new[] { Directives.Manifest, Directives.Waiver };
        Assert.IsTrue(Directives.Breaks(TravelRuleType.PaperSet, facts), "no contract");
        facts.Forms = LabourSet;

        facts.ManifestClass = TransponderClass.Premium;
        Assert.IsTrue(Directives.Breaks(TravelRuleType.PaperSet, facts), "a Premium unit");
        facts.ManifestClass = null;
        Assert.IsFalse(Directives.Breaks(TravelRuleType.PaperSet, facts), "a manifest not handed over yet states nothing");
        facts.ManifestClass = TransponderClass.Economy;

        facts.WaiverSigned = false;
        Assert.IsTrue(Directives.Breaks(TravelRuleType.PaperSet, facts), "an unsigned or missing waiver");

        CaseFacts displaced = Honest(TravellerKind.Displaced, DisplacedSet);
        displaced.WaiverSigned = false;
        Assert.IsFalse(Directives.Breaks(TravelRuleType.PaperSet, displaced), "the displaced have no paper set to read");
    }

    [Test]
    public void TheDebtStanding_BreaksOnAFrozenAccount_ForCitizensOnly()
    {
        foreach (TravellerKind kind in (TravellerKind[])System.Enum.GetValues(typeof(TravellerKind)))
        {
            CaseFacts facts = Honest(kind, SetOf(kind));
            facts.Frozen = true;
            Assert.AreEqual(TravellerKinds.IsCitizen(kind), Directives.Breaks(TravelRuleType.DebtStanding, facts), kind.ToString());
        }
    }

    [Test]
    public void DressTheReturnHomeAndAProcedureLine_NeverBreakHere_AndNullFactsBreakNothing()
    {
        CaseFacts facts = Honest(TravellerKind.RichTourist, RichSet);
        facts.ClosedDestination = true;
        facts.Frozen = true;
        facts.WaiverSigned = false;
        Assert.IsFalse(Directives.Breaks(TravelRuleType.DressForDestination, facts), "a costume error is a deviation fault, proven against the Costume Guide");
        Assert.IsFalse(Directives.Breaks(TravelRuleType.ReturnHome, facts), "a false origin is a deviation fault, proven against the books");
        Assert.IsFalse(Directives.Breaks(TravelRuleType.NoPresentGoods, facts), "smuggling is a deviation fault, proven against the books");
        Assert.IsFalse(Directives.Breaks(TravelRuleType.Procedure, facts));
        Assert.IsFalse(Directives.Breaks(TravelRuleType.PaperSet, null));
    }

    /// <summary>The papers' dates over CaseFacts: read against today when the calendar counts it, the departure first; not read without a calendar.</summary>
    [Test]
    public void ThePapersDates_AreReadAgainstToday_OnlyWithACalendar()
    {
        CaseFacts facts = Honest(TravellerKind.RichTourist, RichSet);
        facts.Today = Today;
        facts.Departures = new[] { On(0) };
        facts.ValidUntils = new[] { On(10) };
        Assert.AreEqual(DirectiveFault.None, Directives.FaultOf(TravelRuleType.PaperDates, facts));
        facts.ValidUntils = new[] { On(-2) };
        Assert.AreEqual(DirectiveFault.ExpiredPaper, Directives.FaultOf(TravelRuleType.PaperDates, facts));
        Assert.IsTrue(Directives.Breaks(TravelRuleType.PaperDates, facts));
        facts.Departures = new[] { On(1) };
        Assert.AreEqual(DirectiveFault.WrongDepartureDate, Directives.FaultOf(TravelRuleType.PaperDates, facts), "the departure first");
        facts.Today = null;
        Assert.AreEqual(DirectiveFault.None, Directives.FaultOf(TravelRuleType.PaperDates, facts), "no calendar: the dates are not read");
    }

    [Test]
    public void Fault_IsTheFirstBrokenRuleThatAppliesToTheKind_InTheDaysOrder()
    {
        var rules = new List<Directive>
        {
            new Directive(TravelRuleType.Procedure, null),
            new Directive(TravelRuleType.PaperSet, new[] { TravellerKind.Labourer }),
            new Directive(TravelRuleType.DebtStanding, new[] { TravellerKind.RichTourist, TravellerKind.PoorTourist, TravellerKind.Labourer }),
            new Directive(TravelRuleType.NationEraForbidden, null)
        };

        CaseFacts frozenRich = Honest(TravellerKind.RichTourist, RichSet);
        frozenRich.Frozen = true;
        frozenRich.WaiverSigned = false;
        Assert.AreEqual(DirectiveFault.FrozenAccount, Directives.Fault(rules, frozenRich), "the labour paper set does not apply to a tourist");

        CaseFacts unsigned = Honest(TravellerKind.Labourer, LabourSet);
        unsigned.WaiverSigned = false;
        unsigned.Frozen = true;
        Assert.AreEqual(DirectiveFault.IncompletePapers, Directives.Fault(rules, unsigned), "the first rule broken, in the day's order");

        CaseFacts closed = Honest(TravellerKind.Displaced, DisplacedSet);
        closed.ClosedDestination = true;
        Assert.AreEqual(DirectiveFault.ClosedDestination, Directives.Fault(rules, closed));

        Assert.AreEqual(DirectiveFault.None, Directives.Fault(rules, Honest(TravellerKind.Labourer, LabourSet)));
        Assert.AreEqual(DirectiveFault.None, Directives.Fault(null, unsigned));
        Assert.AreEqual(DirectiveFault.None, Directives.Fault(rules, null));
    }

    // -----------------------------
    // The paper-set maker
    // -----------------------------

    [Test]
    public void PaperSetBreaks_EachKindsVariants_InTheMakersOrder()
    {
        CollectionAssert.AreEqual(new[] { PaperSetBreak.EconomyManifest }, Directives.PaperSetBreaks(TravellerKind.RichTourist, RichSet));
        CollectionAssert.AreEqual(new[] { PaperSetBreak.WaiverMissing, PaperSetBreak.WaiverUnsigned, PaperSetBreak.ProofMissing }, Directives.PaperSetBreaks(TravellerKind.PoorTourist, PoorSet));
        CollectionAssert.AreEqual(new[] { PaperSetBreak.WaiverMissing, PaperSetBreak.WaiverUnsigned }, Directives.PaperSetBreaks(TravellerKind.Labourer, LabourSet));
        CollectionAssert.IsEmpty(Directives.PaperSetBreaks(TravellerKind.Displaced, DisplacedSet));
    }

    [Test]
    public void PaperSetBreaks_OnlyWhatTheFormsCarriedAllow()
    {
        CollectionAssert.IsEmpty(Directives.PaperSetBreaks(TravellerKind.RichTourist, new[] { Directives.Visa }), "no manifest to ride the wrong unit on");
        CollectionAssert.IsEmpty(Directives.PaperSetBreaks(TravellerKind.Labourer, new[] { Directives.Contract, Directives.Manifest }), "no waiver to leave out or unsign (before phase 8's TC-310 joins the labourer's set)");
        CollectionAssert.AreEqual(new[] { PaperSetBreak.ProofMissing }, Directives.PaperSetBreaks(TravellerKind.PoorTourist, new[] { Directives.Visa, Directives.Manifest, "TC-415" }));
        CollectionAssert.IsEmpty(Directives.PaperSetBreaks(TravellerKind.PoorTourist, null));
    }

    [Test]
    public void PickPaperSetBreak_DrawsOnlyAmongTwoOrMore()
    {
        var one = new ScriptedRandom();
        Assert.AreEqual(PaperSetBreak.EconomyManifest, Directives.PickPaperSetBreak(TravellerKind.RichTourist, RichSet, one));
        Assert.IsTrue(one.Done, "one variant: no draw");

        var two = new ScriptedRandom(R(1));
        Assert.AreEqual(PaperSetBreak.WaiverUnsigned, Directives.PickPaperSetBreak(TravellerKind.Labourer, LabourSet, two));
        Assert.IsTrue(two.Done);

        var none = new ScriptedRandom();
        Assert.AreEqual(PaperSetBreak.None, Directives.PickPaperSetBreak(TravellerKind.Displaced, DisplacedSet, none));
        Assert.IsTrue(none.Done);
        Assert.AreEqual(PaperSetBreak.None, Directives.PickPaperSetBreak(TravellerKind.RichTourist, RichSet, null));
    }

    [Test]
    public void CanBreak_ThePaperSetWithAVariant_TheDebtStandingAndDressForCitizens_NeverAClosureTheReturnHomeOrALine()
    {
        Assert.IsTrue(Directives.CanBreak(TravelRuleType.PaperSet, TravellerKind.RichTourist, RichSet));
        Assert.IsFalse(Directives.CanBreak(TravelRuleType.PaperSet, TravellerKind.Labourer, new[] { Directives.Contract, Directives.Manifest }));
        Assert.IsTrue(Directives.CanBreak(TravelRuleType.DebtStanding, TravellerKind.Labourer, LabourSet));
        Assert.IsFalse(Directives.CanBreak(TravelRuleType.DebtStanding, TravellerKind.Displaced, DisplacedSet));
        Assert.IsTrue(Directives.CanBreak(TravelRuleType.DressForDestination, TravellerKind.PoorTourist, PoorSet));
        Assert.IsFalse(Directives.CanBreak(TravelRuleType.DressForDestination, TravellerKind.Displaced, DisplacedSet));
        Assert.IsFalse(Directives.CanBreak(TravelRuleType.NationEraForbidden, TravellerKind.RichTourist, RichSet), "a closure's violator is made by place, not here");
        Assert.IsFalse(Directives.CanBreak(TravelRuleType.ReturnHome, TravellerKind.Displaced, DisplacedSet), "the return home's liar is made by the lie roll");
        Assert.IsFalse(Directives.CanBreak(TravelRuleType.NoPresentGoods, TravellerKind.RichTourist, RichSet), "a smuggler is made by the lie roll");
        Assert.IsTrue(Directives.CanBreak(TravelRuleType.PaperDates, TravellerKind.Displaced, DisplacedSet), "every kind prints a date");
        Assert.IsFalse(Directives.CanBreak(TravelRuleType.Procedure, TravellerKind.RichTourist, RichSet));
    }

    // -----------------------------
    // The violation roll
    // -----------------------------

    [Test]
    public void Roll_OneValueDraw_ThenTheRuleAmongTwoOrMore()
    {
        var honest = new ScriptedRandom(V(0.5f));
        Assert.AreEqual(-1, Directives.Roll(0.1f, 2, honest));
        Assert.IsTrue(honest.Done);

        var one = new ScriptedRandom(V(0.05f));
        Assert.AreEqual(0, Directives.Roll(0.1f, 1, one));
        Assert.IsTrue(one.Done, "one breakable rule: no rule draw");

        var two = new ScriptedRandom(V(0.05f), R(1));
        Assert.AreEqual(1, Directives.Roll(0.1f, 2, two));
        Assert.IsTrue(two.Done);
    }

    [Test]
    public void Roll_NothingBreakable_OrNoStream_IsHonestWithNoDraw()
    {
        var rng = new ScriptedRandom();
        Assert.AreEqual(-1, Directives.Roll(1f, 0, rng));
        Assert.IsTrue(rng.Done);
        Assert.AreEqual(-1, Directives.Roll(1f, 3, null));
    }

    [Test]
    public void Roll_AChanceOfZero_StillDrawsOnce_SoTheStreamNeverDependsOnTheKnob()
    {
        var rng = new ScriptedRandom(V(0f));
        Assert.AreEqual(-1, Directives.Roll(0f, 2, rng));
        Assert.IsTrue(rng.Done);
    }

    // -----------------------------
    // The content checks (Generate World and the validator share them)
    // -----------------------------

    private static Directives.RuleEntry Rule(string asset, TravelRuleType type, int firstDay, params TravellerKind[] kinds) => new Directives.RuleEntry(asset, type, kinds, firstDay);

    private static readonly TravellerKind[] Tourists = { TravellerKind.RichTourist, TravellerKind.PoorTourist };
    private static readonly TravellerKind[] Citizens = { TravellerKind.RichTourist, TravellerKind.PoorTourist, TravellerKind.Labourer };

    [Test]
    public void RuleProblems_NoneForTheShippedShapes()
    {
        CollectionAssert.IsEmpty(Directives.RuleProblems("Rule_NoAncientEgypt", TravelRuleType.NationEraForbidden, null, true, true));
        CollectionAssert.IsEmpty(Directives.RuleProblems("Rule_NoAncientEgypt", TravelRuleType.NationEraForbidden, new TravellerKind[0], true, false), "a closure's line is generated when blank");
        CollectionAssert.IsEmpty(Directives.RuleProblems("Rule_DressForDestination", TravelRuleType.DressForDestination, null, false, true));
        CollectionAssert.IsEmpty(Directives.RuleProblems("Rule_LeisureDepartures", TravelRuleType.Procedure, null, false, true));
        CollectionAssert.IsEmpty(Directives.RuleProblems("Rule_ReturnHome", TravelRuleType.ReturnHome, null, false, true));
        CollectionAssert.IsEmpty(Directives.RuleProblems("Rule_TouristPaperSet", TravelRuleType.PaperSet, Tourists, false, true));
        CollectionAssert.IsEmpty(Directives.RuleProblems("Rule_DebtStanding", TravelRuleType.DebtStanding, Citizens, false, true));
    }

    [Test]
    public void RuleProblems_NameEachBrokenShape()
    {
        StringAssert.Contains("lists none", Directives.RuleProblems("R", TravelRuleType.EraForbidden, Tourists, true, true).Single());
        List<string> procedure = Directives.RuleProblems("R", TravelRuleType.PaperSet, null, true, false);
        Assert.AreEqual(3, procedure.Count);
        StringAssert.Contains("names a country or era", procedure[0]);
        StringAssert.Contains("directive line", procedure[1]);
        StringAssert.Contains("lists no kinds", procedure[2]);
        StringAssert.Contains("lists no kinds", Directives.RuleProblems("R", TravelRuleType.DebtStanding, new TravellerKind[0], false, true).Single());
        CollectionAssert.IsEmpty(Directives.RuleProblems("R", TravelRuleType.DressForDestination, new TravellerKind[0], false, true), "dress is read for every 2150 citizen by its own rule");
    }

    private static List<(TravellerKind kind, IReadOnlyCollection<string> forms)> Day3Kinds() => new List<(TravellerKind, IReadOnlyCollection<string>)>
    {
        (TravellerKind.RichTourist, RichSet),
        (TravellerKind.Labourer, new[] { Directives.Contract, Directives.Manifest })
    };

    private static readonly List<(TravellerKind kind, IReadOnlyCollection<string> forms)> DisplacedOnly = new List<(TravellerKind, IReadOnlyCollection<string>)> { (TravellerKind.Displaced, DisplacedSet) };

    [Test]
    public void DayProblems_NoneForASoundDay()
    {
        var active = new List<Directives.RuleEntry>
        {
            Rule("Rule_NoMedievalChina", TravelRuleType.NationEraForbidden, 3),
            Rule("Rule_DressForDestination", TravelRuleType.DressForDestination, 2),
            Rule("Rule_LeisureDepartures", TravelRuleType.Procedure, 1),
            Rule("Rule_TouristPaperSet", TravelRuleType.PaperSet, 2, Tourists),
            Rule("Rule_DebtStanding", TravelRuleType.DebtStanding, 3, Citizens)
        };
        CollectionAssert.IsEmpty(Directives.DayProblems("DayPlan_Inv_Day3", 3, active, Day3Kinds()));
        CollectionAssert.IsEmpty(Directives.DayProblems("DayPlan_Inv_Day1", 1, null, null));
        var day5 = new List<Directives.RuleEntry> { Rule("Rule_ReturnHome", TravelRuleType.ReturnHome, 5), Rule("Rule_DressForDestination", TravelRuleType.DressForDestination, 2) };
        CollectionAssert.IsEmpty(Directives.DayProblems("DayPlan_Inv_Day5", 5, day5, DisplacedOnly), "the return home's liar is the day's displaced (its own check); dress past its first day plans nobody");
    }

    [Test]
    public void DayProblems_ARolledRuleNoKindCanBreak_AndAFirstDayGuaranteeNoKindCanBe()
    {
        var active = new List<Directives.RuleEntry>
        {
            Rule("Rule_LabourPaperSet", TravelRuleType.PaperSet, 3, TravellerKind.Labourer),
            Rule("Rule_DebtStanding", TravelRuleType.DebtStanding, 3, Citizens),
            Rule("Rule_DressForDestination", TravelRuleType.DressForDestination, 5)
        };
        List<string> problems = Directives.DayProblems("D", 5, active, DisplacedOnly);
        Assert.AreEqual(3, problems.Count, string.Join("\n", problems));
        StringAssert.Contains("'Rule_LabourPaperSet' (PaperSet), which none of its kinds can break", problems[0]);
        StringAssert.Contains("'Rule_DebtStanding' (DebtStanding), which none of its kinds can break", problems[1]);
        StringAssert.Contains("first day of the rule 'Rule_DressForDestination'", problems[2]);

        StringAssert.Contains("'Rule_LabourPaperSet' (PaperSet), which none of its kinds can break",
                              Directives.DayProblems("D", 3, new List<Directives.RuleEntry> { active[0] }, Day3Kinds()).Single(), "a labourer without a waiver has no paper-set variant to break");
    }

    // -----------------------------
    // The PaperDates directive and its maker (phase 11)
    // -----------------------------

    [Test]
    public void PaperDates_HonestPapers_DepartTodayAndHaveNotExpired()
    {
        Assert.AreEqual(DirectiveFault.None, Directives.PaperDates(new[] { On(0) }, new[] { On(3), On(365) }, Today));
        Assert.AreEqual(DirectiveFault.None, Directives.PaperDates(new[] { On(0), On(0) }, new[] { On(0) }, Today), "a paper valid until today is still valid");
        Assert.AreEqual(DirectiveFault.None, Directives.PaperDates(null, null, Today), "nothing printed");
        Assert.AreEqual(DirectiveFault.None, Directives.PaperDates(new string[0], new string[0], Today));
    }

    [Test]
    public void PaperDates_ADepartureOnAnotherDay_IsTheWrongDate_EitherWay()
    {
        Assert.AreEqual(DirectiveFault.WrongDepartureDate, Directives.PaperDates(new[] { On(1) }, null, Today), "tomorrow");
        Assert.AreEqual(DirectiveFault.WrongDepartureDate, Directives.PaperDates(new[] { On(-3) }, null, Today), "three days ago");
        Assert.AreEqual(DirectiveFault.WrongDepartureDate, Directives.PaperDates(new[] { On(0), On(2) }, null, Today), "any departure printed");
    }

    [Test]
    public void PaperDates_AValidUntilBeforeToday_IsExpired()
    {
        Assert.AreEqual(DirectiveFault.ExpiredPaper, Directives.PaperDates(new[] { On(0) }, new[] { On(-1) }, Today), "yesterday");
        Assert.AreEqual(DirectiveFault.ExpiredPaper, Directives.PaperDates(null, new[] { On(30), On(-30) }, Today), "any Valid Until printed");
    }

    [Test]
    public void PaperDates_TheDepartureIsReadFirst_AndUnreadableDatesAreSkipped()
    {
        Assert.AreEqual(DirectiveFault.WrongDepartureDate, Directives.PaperDates(new[] { On(1) }, new[] { On(-1) }, Today), "both wrong: the departure names the fault");
        Assert.AreEqual(DirectiveFault.None, Directives.PaperDates(new[] { "DepartureDate:none" }, new[] { "Expiry:none", null, "" }, Today), "placeholders are no fault");
        Assert.AreEqual(DirectiveFault.ExpiredPaper, Directives.PaperDates(new[] { "DepartureDate:none" }, new[] { On(-5) }, Today));
    }

    [Test]
    public void PlanDateFault_OneDrawOverTheDatesPrinted_TheDepartureFirst()
    {
        PaperDatePlan plan = Directives.PlanDateFault(true, 2, Script(R(0)));
        Assert.AreEqual(PaperDateFault.Departure, plan.Fault);
        Assert.AreEqual(-1, plan.ExpiryIndex);

        plan = Directives.PlanDateFault(true, 2, Script(R(1)));
        Assert.AreEqual(PaperDateFault.Expiry, plan.Fault);
        Assert.AreEqual(0, plan.ExpiryIndex, "the first expiring form");

        plan = Directives.PlanDateFault(true, 2, Script(R(2)));
        Assert.AreEqual((PaperDateFault.Expiry, 1), (plan.Fault, plan.ExpiryIndex), "the second");

        plan = Directives.PlanDateFault(false, 1, Script(R(0)));
        Assert.AreEqual((PaperDateFault.Expiry, 0), (plan.Fault, plan.ExpiryIndex), "no departure printed: the draw is over the expiring forms");

        plan = Directives.PlanDateFault(true, 0, Script(R(0)));
        Assert.AreEqual(PaperDateFault.Departure, plan.Fault);
    }

    [Test]
    public void Closes_EachClosureType_ByIds()
    {
        Assert.IsTrue(new Directive(TravelRuleType.EraForbidden, null, null, "modern").Closes("japan", "modern"));
        Assert.IsFalse(new Directive(TravelRuleType.EraForbidden, null, null, "modern").Closes("japan", "industrial"));
        Assert.IsTrue(new Directive(TravelRuleType.NationForbidden, null, "japan", null).Closes("japan", "ancient"));
        Assert.IsFalse(new Directive(TravelRuleType.NationForbidden, null, "japan", null).Closes("china", "ancient"));
        Assert.IsTrue(new Directive(TravelRuleType.NationEraForbidden, null, "egypt", "ancient").Closes("egypt", "ancient"));
        Assert.IsFalse(new Directive(TravelRuleType.NationEraForbidden, null, "egypt", "ancient").Closes("egypt", "medieval"));
        Assert.IsFalse(new Directive(TravelRuleType.PaperSet, null, "egypt", "ancient").Closes("egypt", "ancient"), "a procedure closes nothing");
    }

    [Test]
    public void Plan_MapsEachPlannedDirectiveToItsRuleAndVariant()
    {
        (PlannedDirective planned, TravelRuleType rule, PaperSetBreak paper, PaperDateFault date)[] table =
        {
            (PlannedDirective.EconomyManifest, TravelRuleType.PaperSet, PaperSetBreak.EconomyManifest, PaperDateFault.None),
            (PlannedDirective.WaiverMissing, TravelRuleType.PaperSet, PaperSetBreak.WaiverMissing, PaperDateFault.None),
            (PlannedDirective.WaiverUnsigned, TravelRuleType.PaperSet, PaperSetBreak.WaiverUnsigned, PaperDateFault.None),
            (PlannedDirective.ProofMissing, TravelRuleType.PaperSet, PaperSetBreak.ProofMissing, PaperDateFault.None),
            (PlannedDirective.Frozen, TravelRuleType.DebtStanding, PaperSetBreak.None, PaperDateFault.None),
            (PlannedDirective.DepartureDate, TravelRuleType.PaperDates, PaperSetBreak.None, PaperDateFault.Departure),
            (PlannedDirective.Expired, TravelRuleType.PaperDates, PaperSetBreak.None, PaperDateFault.Expiry)
        };
        foreach ((PlannedDirective planned, TravelRuleType rule, PaperSetBreak paper, PaperDateFault date) in table)
        {
            DirectivePlan plan = Directives.Plan(planned);
            Assert.IsTrue(plan.IsFault, planned.ToString());
            Assert.AreEqual(rule, plan.Rule, planned.ToString());
            Assert.AreEqual(paper, plan.PaperBreak, planned.ToString());
            Assert.AreEqual(date, plan.DateFault, planned.ToString());
            Assert.IsTrue(Directives.IsRolled(plan.Rule), $"{planned}: its rule has a maker the pinned variant drives");
        }
    }

    [Test]
    public void Plan_NoneIsNoFault()
    {
        DirectivePlan plan = Directives.Plan(PlannedDirective.None);
        Assert.IsFalse(plan.IsFault);
        Assert.AreEqual(PaperSetBreak.None, plan.PaperBreak);
        Assert.AreEqual(PaperDateFault.None, plan.DateFault);
    }

    [Test]
    public void PickPaperSetBreak_APinnedVariantDrawsNothing()
    {
        var rng = new ScriptedRandom();
        Assert.AreEqual(PaperSetBreak.WaiverUnsigned, Directives.PickPaperSetBreak(TravellerKind.PoorTourist, PoorSet, rng, PaperSetBreak.WaiverUnsigned));
        Assert.IsTrue(rng.Done, "the slot's authoring chose it: no draw");
    }

    [Test]
    public void PickPaperSetBreak_APinnedVariantThatCannotShowIsNone()
    {
        var rng = new ScriptedRandom();
        Assert.AreEqual(PaperSetBreak.None, Directives.PickPaperSetBreak(TravellerKind.RichTourist, RichSet, rng, PaperSetBreak.WaiverUnsigned), "a rich tourist carries no waiver");
        Assert.AreEqual(PaperSetBreak.None, Directives.PickPaperSetBreak(TravellerKind.Labourer, LabourSet, rng, PaperSetBreak.ProofMissing), "a labourer carries no proof of means");
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void PlanDateFault_APinnedDepartureDrawsNothing()
    {
        var rng = Script();
        PaperDatePlan plan = Directives.PlanDateFault(true, 2, rng, PaperDateFault.Departure);
        Assert.AreEqual((PaperDateFault.Departure, -1), (plan.Fault, plan.ExpiryIndex));
        Assert.IsTrue(rng.Done);
        Assert.AreEqual(PaperDateFault.None, Directives.PlanDateFault(false, 2, Script(), PaperDateFault.Departure).Fault, "no departure printed: nothing to falsify");
    }

    [Test]
    public void PlanDateFault_APinnedExpiryDrawsOnlyTheForm()
    {
        PaperDatePlan plan = Directives.PlanDateFault(true, 2, Script(R(1)), PaperDateFault.Expiry);
        Assert.AreEqual((PaperDateFault.Expiry, 1), (plan.Fault, plan.ExpiryIndex), "the draw is over the expiring forms only, never the departure");
        Assert.AreEqual(PaperDateFault.None, Directives.PlanDateFault(true, 0, Script(), PaperDateFault.Expiry).Fault, "nothing expires: nothing to falsify");
    }

    [Test]
    public void PlanDateFault_NothingPrinted_OrNoStream_IsNone_WithNoDraw()
    {
        Assert.AreEqual(PaperDateFault.None, Directives.PlanDateFault(false, 0, Script()).Fault);
        Assert.AreEqual(PaperDateFault.None, Directives.PlanDateFault(false, -2, Script()).Fault);
        Assert.AreEqual(PaperDateFault.None, Directives.PlanDateFault(true, 1, null).Fault);
    }

    [Test]
    public void OffsetDeparture_OneDraw_OneToThreeDaysEitherWay_NeverToday()
    {
        var seen = new System.Collections.Generic.List<int>();
        for (int pick = 0; pick < Directives.DepartureOffsetMaxDays * 2; pick++)
        {
            DateTime date = Directives.OffsetDeparture(Today, Script(R(pick)));
            int offset = (date - Today).Days;
            Assert.AreNotEqual(0, offset, $"pick {pick}");
            Assert.LessOrEqual(Math.Abs(offset), Directives.DepartureOffsetMaxDays, $"pick {pick}");
            seen.Add(offset);
        }
        CollectionAssert.AreEqual(new[] { -3, -2, -1, 1, 2, 3 }, seen, "every offset once, in draw order");
        Assert.AreEqual(DirectiveFault.WrongDepartureDate, Directives.PaperDates(new[] { AgencyCalendar.Write(Directives.OffsetDeparture(Today, Script(R(5)))) }, null, Today), "the maker's date breaks the directive");
    }

    [Test]
    public void ExpiredValidUntil_OneDraw_OneToThirtyDaysAgo()
    {
        Assert.AreEqual(Today.AddDays(-1), Directives.ExpiredValidUntil(Today, Script(R(0))));
        Assert.AreEqual(Today.AddDays(-30), Directives.ExpiredValidUntil(Today, Script(R(29))));
        Assert.AreEqual(Today.AddDays(-30), Directives.ExpiredValidUntil(Today, Script(R(99))), "the draw is clamped to the range");
        Assert.AreEqual(DirectiveFault.ExpiredPaper, Directives.PaperDates(null, new[] { AgencyCalendar.Write(Directives.ExpiredValidUntil(Today, Script(R(0)))) }, Today), "the maker's date breaks the directive");
    }

    [Test]
    public void IsSigned_ABlankOrUnsignedSignatureBox_IsUnsigned_TheNumberIsNotRead()
    {
        DocumentField Box(ClueCategory category, string value) => new DocumentField { category = category, value = value };
        Assert.IsTrue(Directives.IsSigned(new[] { Box(ClueCategory.WaiverNo, "SW-204817"), Box(ClueCategory.Signature, "Mara") }));
        Assert.IsFalse(Directives.IsSigned(new[] { Box(ClueCategory.WaiverNo, "SW-204817"), Box(ClueCategory.Signature, Directives.UnsignedMark) }));
        Assert.IsFalse(Directives.IsSigned(new[] { Box(ClueCategory.Signature, " unsigned ") }), "any case, trimmed");
        Assert.IsFalse(Directives.IsSigned(new[] { Box(ClueCategory.Signature, " ") }), "a blank box");
        Assert.IsTrue(Directives.IsSigned(new[] { Box(ClueCategory.WaiverNo, "SW-000001"), Box(ClueCategory.Signature, "Mara") }), "a number the account never registered is L3's forgery, read by the records");
        Assert.IsTrue(Directives.IsSigned(new[] { Box(ClueCategory.Name, "Mara") }), "no Signature box states nothing unsigned");
        Assert.IsTrue(Directives.IsSigned(null));
    }
}
