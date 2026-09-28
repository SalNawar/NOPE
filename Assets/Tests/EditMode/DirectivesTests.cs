using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The Directives' rules (traveller types P3, P4, §5.3-5.4; redesign phase
/// 9): each type's decision table over CaseFacts, the fault a broken rule
/// is, the paper-set maker's variants and pick, the violation roll, and the
/// content checks Generate World and the validator share.
/// </summary>
public class DirectivesTests
{
    private static ScriptStep V(float roll) => ScriptStep.Value(roll);
    private static ScriptStep R(int offset) => ScriptStep.Range(offset);

    private static readonly string[] RichSet = { Directives.Visa, Directives.Manifest };
    private static readonly string[] PoorSet = { Directives.Visa, Directives.Manifest, Directives.Waiver, "TC-416" };
    private static readonly string[] LabourSet = { Directives.Contract, Directives.Manifest, Directives.Waiver };

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
    // The types
    // -----------------------------

    [TestCase(TravelRuleType.EraForbidden, true)]
    [TestCase(TravelRuleType.NationForbidden, true)]
    [TestCase(TravelRuleType.NationEraForbidden, true)]
    [TestCase(TravelRuleType.DressForDestination, false)]
    [TestCase(TravelRuleType.Procedure, false)]
    [TestCase(TravelRuleType.PaperSet, false)]
    [TestCase(TravelRuleType.DebtStanding, false)]
    public void IsClosure_TheFirstThreeTypes(TravelRuleType type, bool closure)
    {
        Assert.AreEqual(closure, Directives.IsClosure(type));
    }

    [Test]
    public void CanGuarantee_ThePaperSetTheDebtStandingAndDress_IsRolled_ThePaperSetAndTheDebtStanding()
    {
        CollectionAssert.AreEquivalent(new[] { TravelRuleType.PaperSet, TravelRuleType.DebtStanding, TravelRuleType.DressForDestination },
                                       System.Enum.GetValues(typeof(TravelRuleType)).Cast<TravelRuleType>().Where(Directives.CanGuarantee).ToList());
        CollectionAssert.AreEquivalent(new[] { TravelRuleType.PaperSet, TravelRuleType.DebtStanding },
                                       System.Enum.GetValues(typeof(TravelRuleType)).Cast<TravelRuleType>().Where(Directives.IsRolled).ToList());
    }

    [TestCase(TravelRuleType.EraForbidden, DirectiveFault.ClosedDestination)]
    [TestCase(TravelRuleType.NationForbidden, DirectiveFault.ClosedDestination)]
    [TestCase(TravelRuleType.NationEraForbidden, DirectiveFault.ClosedDestination)]
    [TestCase(TravelRuleType.PaperSet, DirectiveFault.IncompletePapers)]
    [TestCase(TravelRuleType.DebtStanding, DirectiveFault.FrozenAccount)]
    [TestCase(TravelRuleType.DressForDestination, DirectiveFault.None)]
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

    /// <summary>Each kind's honest set of forms.</summary>
    private static string[] SetOf(TravellerKind kind) =>
        kind == TravellerKind.RichTourist ? RichSet : kind == TravellerKind.PoorTourist ? PoorSet : kind == TravellerKind.Labourer ? LabourSet : new[] { "TC-610", "TC-620", "TC-630" };

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

        CaseFacts displaced = Honest(TravellerKind.Displaced, "TC-610");
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
    public void DressAndAProcedureLine_NeverBreakHere_AndNullFactsBreakNothing()
    {
        CaseFacts facts = Honest(TravellerKind.RichTourist, RichSet);
        facts.ClosedDestination = true;
        facts.Frozen = true;
        facts.WaiverSigned = false;
        Assert.IsFalse(Directives.Breaks(TravelRuleType.DressForDestination, facts), "a costume error is a deviation fault, proven against the Costume Guide");
        Assert.IsFalse(Directives.Breaks(TravelRuleType.Procedure, facts));
        Assert.IsFalse(Directives.Breaks(TravelRuleType.PaperSet, null));
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

        CaseFacts closed = Honest(TravellerKind.Displaced, "TC-610");
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
        CollectionAssert.IsEmpty(Directives.PaperSetBreaks(TravellerKind.Displaced, new[] { "TC-610", "TC-620", "TC-630" }));
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
        Assert.AreEqual(PaperSetBreak.None, Directives.PickPaperSetBreak(TravellerKind.Displaced, new[] { "TC-610" }, none));
        Assert.IsTrue(none.Done);
        Assert.AreEqual(PaperSetBreak.None, Directives.PickPaperSetBreak(TravellerKind.RichTourist, RichSet, null));
    }

    [Test]
    public void CanBreak_ThePaperSetWithAVariant_TheDebtStandingAndDressForCitizens_NeverAClosureOrALine()
    {
        Assert.IsTrue(Directives.CanBreak(TravelRuleType.PaperSet, TravellerKind.RichTourist, RichSet));
        Assert.IsFalse(Directives.CanBreak(TravelRuleType.PaperSet, TravellerKind.Labourer, new[] { Directives.Contract, Directives.Manifest }));
        Assert.IsTrue(Directives.CanBreak(TravelRuleType.DebtStanding, TravellerKind.Labourer, LabourSet));
        Assert.IsFalse(Directives.CanBreak(TravelRuleType.DebtStanding, TravellerKind.Displaced, new[] { "TC-610" }));
        Assert.IsTrue(Directives.CanBreak(TravelRuleType.DressForDestination, TravellerKind.PoorTourist, PoorSet));
        Assert.IsFalse(Directives.CanBreak(TravelRuleType.DressForDestination, TravellerKind.Displaced, new[] { "TC-610" }));
        Assert.IsFalse(Directives.CanBreak(TravelRuleType.NationEraForbidden, TravellerKind.RichTourist, RichSet), "a closure's violator is made by place, not here");
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

    private static Directives.RuleEntry Rule(string asset, TravelRuleType type, params TravellerKind[] kinds) => new Directives.RuleEntry(asset, type, kinds);

    private static readonly TravellerKind[] Tourists = { TravellerKind.RichTourist, TravellerKind.PoorTourist };
    private static readonly TravellerKind[] Citizens = { TravellerKind.RichTourist, TravellerKind.PoorTourist, TravellerKind.Labourer };

    [Test]
    public void RuleProblems_NoneForTheShippedShapes()
    {
        CollectionAssert.IsEmpty(Directives.RuleProblems("Rule_NoAncientEgypt", TravelRuleType.NationEraForbidden, null, true, true));
        CollectionAssert.IsEmpty(Directives.RuleProblems("Rule_NoAncientEgypt", TravelRuleType.NationEraForbidden, new TravellerKind[0], true, false), "a closure's line is generated when blank");
        CollectionAssert.IsEmpty(Directives.RuleProblems("Rule_DressForDestination", TravelRuleType.DressForDestination, null, false, true));
        CollectionAssert.IsEmpty(Directives.RuleProblems("Rule_LeisureDepartures", TravelRuleType.Procedure, null, false, true));
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
        (TravellerKind.Labourer, new[] { Directives.Contract, Directives.Manifest }),
        (TravellerKind.Displaced, new[] { "TC-610", "TC-620", "TC-630" })
    };

    [Test]
    public void DayProblems_NoneForASoundDay()
    {
        var active = new List<Directives.RuleEntry>
        {
            Rule("Rule_NoMedievalChina", TravelRuleType.NationEraForbidden),
            Rule("Rule_DressForDestination", TravelRuleType.DressForDestination),
            Rule("Rule_LeisureDepartures", TravelRuleType.Procedure),
            Rule("Rule_TouristPaperSet", TravelRuleType.PaperSet, Tourists),
            Rule("Rule_DebtStanding", TravelRuleType.DebtStanding, Citizens)
        };
        CollectionAssert.IsEmpty(Directives.DayProblems("DayPlan_Inv_Day3", active, Day3Kinds(), new[] { "Rule_DebtStanding", "Rule_TouristPaperSet", "Rule_DressForDestination" }));
        CollectionAssert.IsEmpty(Directives.DayProblems("DayPlan_Inv_Day3", active, Day3Kinds(), null));
        CollectionAssert.IsEmpty(Directives.DayProblems("DayPlan_Inv_Day1", null, null, null));
    }

    [Test]
    public void DayProblems_ARolledRuleNoKindCanBreak_AndEachBrokenGuarantee()
    {
        var active = new List<Directives.RuleEntry>
        {
            Rule("Rule_NoMedievalChina", TravelRuleType.NationEraForbidden),
            Rule("Rule_LeisureDepartures", TravelRuleType.Procedure),
            Rule("Rule_LabourPaperSet", TravelRuleType.PaperSet, TravellerKind.Labourer),
            Rule("Rule_DebtStanding", TravelRuleType.DebtStanding, Citizens)
        };
        List<string> problems = Directives.DayProblems("D", active, Day3Kinds(), new[] { "Rule_NoMedievalChina", "Rule_LeisureDepartures", "Rule_LabourPaperSet", "Rule_Unlisted", "Rule_DebtStanding", "Rule_DebtStanding" });
        Assert.AreEqual(6, problems.Count, string.Join("\n", problems));
        StringAssert.Contains("'Rule_LabourPaperSet' (PaperSet), which none of its kinds can break", problems[0]);
        StringAssert.Contains("'Rule_NoMedievalChina' (NationEraForbidden); a closure is always guaranteed", problems[1]);
        StringAssert.Contains("'Rule_LeisureDepartures' (Procedure)", problems[2]);
        StringAssert.Contains("guarantees the rule 'Rule_LabourPaperSet' (PaperSet), which none of its kinds can break", problems[3]);
        StringAssert.Contains("'Rule_Unlisted', which is not among its rules", problems[4]);
        StringAssert.Contains("'Rule_DebtStanding' twice", problems[5]);
    }

    [Test]
    public void DayProblems_ADebtStandingWithOnlyTheDisplaced_CannotBeBroken()
    {
        var active = new List<Directives.RuleEntry> { Rule("Rule_DebtStanding", TravelRuleType.DebtStanding, Citizens) };
        var displacedOnly = new List<(TravellerKind kind, IReadOnlyCollection<string> forms)> { (TravellerKind.Displaced, new[] { "TC-610" }) };
        StringAssert.Contains("none of its kinds can break", Directives.DayProblems("D", active, displacedOnly, null).Single());
    }
}
