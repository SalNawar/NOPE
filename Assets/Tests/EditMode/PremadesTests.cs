using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>Premade scheduling: which slots hold a premade, who may roll, and the roll's draws.</summary>
public class PremadesTests
{
    [TestCase(true, true, false, PremadeSlot.Forced)]
    [TestCase(true, true, true, PremadeSlot.Forced)]
    [TestCase(true, false, false, PremadeSlot.None)]
    [TestCase(true, false, true, PremadeSlot.None)]
    [TestCase(false, false, false, PremadeSlot.Roll)]
    [TestCase(false, true, false, PremadeSlot.Roll)]
    [TestCase(false, false, true, PremadeSlot.None)]
    [TestCase(false, true, true, PremadeSlot.None)]
    public void SlotSource_DecisionTable(bool forcedHere, bool premadeStands, bool violatorSlot, PremadeSlot expected)
    {
        Assert.AreEqual(expected, Premades.SlotSource(forcedHere, premadeStands, violatorSlot));
    }

    [Test]
    public void SlotSource_AFailedConditionHoldsAnOrdinaryTraveller()
    {
        bool stands = Premades.Stands(true, false, false);
        Assert.IsFalse(stands);
        Assert.AreEqual(PremadeSlot.None, Premades.SlotSource(true, stands, false), "an ordinary traveller, never a roll");
    }

    [Test]
    public void SlotSource_ARepeatablePremadeStandsEveryListedDay()
    {
        Assert.IsTrue(Premades.Stands(false, false, true));
        Assert.IsTrue(Premades.Stands(false, true, true), "a repeatable premade is never kept out by a met flag");
        Assert.AreEqual(PremadeSlot.Forced, Premades.SlotSource(true, Premades.Stands(false, true, true), false));
    }

    [Test]
    public void SlotSource_AMetOncePerRunPremadeHoldsAnOrdinaryTraveller()
    {
        bool stands = Premades.Stands(true, true, true);
        Assert.IsFalse(stands);
        Assert.AreEqual(PremadeSlot.None, Premades.SlotSource(true, stands, false));
    }

    [Test]
    public void SlotSource_ConditionsAreReadOnlyForAForcedSlot()
    {
        Assert.AreEqual(PremadeSlot.Roll, Premades.SlotSource(false, false, false));
        Assert.AreEqual(PremadeSlot.Roll, Premades.SlotSource(false, true, false));
    }

    [Test]
    public void Appearance_TheFirstStandingEntryWins()
    {
        Assert.AreEqual(0, Premades.Appearance(new[] { true, true }));
        Assert.AreEqual(1, Premades.Appearance(new[] { false, true, true }), "an earlier entry whose conditions fail gives way to the next");
        Assert.AreEqual(2, Premades.Appearance(new[] { false, false, true }));
    }

    [TestCase(TravellerKind.Displaced, true)]
    [TestCase(TravellerKind.RichTourist, false)]
    [TestCase(TravellerKind.PoorTourist, false)]
    [TestCase(TravellerKind.Labourer, false)]
    public void IsFamous_OnlyTheDisplacedKind(TravellerKind kind, bool famous)
    {
        Assert.AreEqual(famous, Premades.IsFamous(kind));
    }

    [TestCase("slot line", "premade line", "slot line")]
    [TestCase("", "premade line", "premade line")]
    [TestCase("  ", "premade line", "premade line")]
    [TestCase(null, "premade line", "premade line")]
    [TestCase(null, null, null)]
    public void Voice_TheSlotsLineReplacesThePremadesOwn(string slot, string premade, string expected)
    {
        Assert.AreEqual(expected, Premades.Voice(slot, premade));
    }

    // ---- ForcedProblems (days 7-15 V2-V5) ----

    private static readonly string[] PoorSet = { Directives.Visa, Directives.Manifest, Directives.Waiver, "TC-416" };
    private static readonly string[] RichSet = { Directives.Visa, Directives.Manifest };
    private static readonly string[] DisplacedSet = { "TC-610", "TC-620", "TC-630" };

    private static readonly Directive[] DaySevenRules =
    {
        new Directive(TravelRuleType.NationEraForbidden, null, "egypt", "ancient"),
        new Directive(TravelRuleType.PaperSet, new[] { TravellerKind.RichTourist, TravellerKind.PoorTourist }),
        new Directive(TravelRuleType.DebtStanding, new[] { TravellerKind.RichTourist, TravellerKind.PoorTourist, TravellerKind.Labourer })
    };

    private static readonly LieKind[] AllLies = (LieKind[])System.Enum.GetValues(typeof(LieKind));

    private static ForcedCheck Pell(int slot = 5, string id = "pell_1", PlannedDirective directive = PlannedDirective.WaiverUnsigned, params string[] conditions) => new ForcedCheck
    {
        Slot = slot, Id = id, Premade = "pell", Kind = TravellerKind.PoorTourist, Forms = PoorSet, OncePerRun = false,
        Directive = directive, Dialog = "dlg_pell_1", ConditionKeys = conditions, Conditions = conditions.Length
    };

    private static ForcedDayCheck Day(int day, IReadOnlyList<ForcedCheck> forced, LieKind[] lies = null, Directive[] rules = null, string[] pooled = null) => new ForcedDayCheck
    {
        Asset = $"DayPlan_Inv_Day{day}", Day = day, Lies = lies ?? AllLies, Rules = rules ?? DaySevenRules, Pooled = pooled ?? new string[0], Forced = forced
    };

    private static readonly string[] PremadeIds = { "pell", "rook", "senenmut", "auditor" };
    private static readonly string[] DialogIds = { "dlg_pell_1", "dlg_pell_2" };

    private static (List<string> errors, List<string> warnings) Check(params ForcedDayCheck[] days)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        Premades.ForcedProblems(days, PremadeIds, DialogIds, errors, warnings);
        return (errors, warnings);
    }

    [Test]
    public void ForcedProblems_TheBeatsAreSound()
    {
        (List<string> errors, List<string> warnings) = Check(
            Day(7, new[] { Pell() }),
            Day(10, new[] { Pell(5, "pell_2", PlannedDirective.WaiverMissing, FlagKeys.PremadeVerdict("pell", false)), Pell(5, "pell_2_first", PlannedDirective.WaiverMissing, FlagKeys.PremadeVerdict("pell", true)) }));
        CollectionAssert.IsEmpty(errors);
        CollectionAssert.IsEmpty(warnings);
    }

    [Test]
    public void ForcedProblems_ALieThatDoesNotFitTheKind()
    {
        ForcedCheck pell = Pell(directive: PlannedDirective.None);
        pell.Lie = LieKind.ForgedContract;
        (List<string> errors, _) = Check(Day(7, new[] { pell }));
        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains("ForgedContract", errors[0]);
    }

    [Test]
    public void ForcedProblems_ALieNotEnabledThatDay_IsAWarning()
    {
        ForcedCheck pell = Pell(directive: PlannedDirective.None);
        pell.Lie = LieKind.ForgedProof;
        (List<string> errors, List<string> warnings) = Check(Day(7, new[] { pell }, new[] { LieKind.DoctoredIdentity }));
        CollectionAssert.IsEmpty(errors);
        Assert.AreEqual(1, warnings.Count);
        StringAssert.Contains("ForgedProof", warnings[0]);
    }

    [Test]
    public void ForcedProblems_ADirectiveWhoseRuleIsNotActiveThatDay()
    {
        (List<string> errors, _) = Check(Day(7, new[] { Pell() }, rules: new[] { DaySevenRules[0] }));
        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains("PaperSet", errors[0]);
    }

    [Test]
    public void ForcedProblems_ADirectiveVariantTheKindCannotShow()
    {
        ForcedCheck rich = Pell();
        rich.Kind = TravellerKind.RichTourist;
        rich.Forms = RichSet;
        (List<string> errors, _) = Check(Day(7, new[] { rich }));
        Assert.AreEqual(1, errors.Count, "a rich tourist carries no waiver to leave unsigned");
        ForcedCheck displaced = Pell(directive: PlannedDirective.Frozen);
        displaced.Kind = TravellerKind.Displaced;
        displaced.Forms = DisplacedSet;
        Assert.AreEqual(1, Check(Day(7, new[] { displaced }, rules: DaySevenRules.Concat(new[] { new Directive(TravelRuleType.DebtStanding, null) }).ToArray())).errors.Count, "the displaced hold no account to freeze");
    }

    [Test]
    public void ForcedProblems_ALieAndADirectiveTogether()
    {
        ForcedCheck pell = Pell();
        pell.Lie = LieKind.FakeWaiver;
        (List<string> errors, _) = Check(Day(7, new[] { pell }));
        Assert.AreEqual(1, errors.Count, "a premade carries at most one fault");
        StringAssert.Contains("one fault", errors[0]);
    }

    [Test]
    public void ForcedProblems_AFaultOnAPremadeWithATruePlace()
    {
        ForcedCheck socrates = Pell(directive: PlannedDirective.None);
        socrates.Kind = TravellerKind.Displaced;
        socrates.Forms = DisplacedSet;
        socrates.HasTruePlace = true;
        socrates.Lie = LieKind.FalseOrigin;
        (List<string> errors, _) = Check(Day(7, new[] { socrates }));
        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains("true place", errors[0]);
    }

    [Test]
    public void ForcedProblems_AFaultOnAPremadeBoundForAClosedPlace()
    {
        ForcedCheck pell = Pell();
        pell.ClosedPlace = true;
        (List<string> errors, _) = Check(Day(7, new[] { pell }));
        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains("closed", errors[0]);
    }

    [Test]
    public void ForcedProblems_AConditionNamingAnUnknownPremade()
    {
        (List<string> errors, _) = Check(Day(7, new[] { Pell(5, "pell_1", PlannedDirective.WaiverUnsigned, FlagKeys.PremadeVerdict("pel", true)) }));
        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains("'pel'", errors[0]);
    }

    [Test]
    public void ForcedProblems_AVerdictConditionOnAPremadeThatStandsOnNoEarlierDay_IsAWarning()
    {
        (List<string> errors, List<string> warnings) = Check(Day(7, new[] { Pell(5, "pell_1", PlannedDirective.WaiverUnsigned, FlagKeys.PremadeVerdict("rook", true)) }));
        CollectionAssert.IsEmpty(errors);
        Assert.AreEqual(1, warnings.Count, "rook stands on no day before day 7");
        CollectionAssert.IsEmpty(Check(Day(7, new[] { Pell(5, "pell_1", PlannedDirective.WaiverUnsigned, FlagKeys.PremadeVerdict("senenmut", true)) }, pooled: new string[0]),
                                       Day(8, new[] { Pell(4, "pell_x", PlannedDirective.WaiverUnsigned, FlagKeys.PremadeVerdict("senenmut", true)) }, pooled: new[] { "senenmut" })).errors);
    }

    [Test]
    public void ForcedProblems_AlternativesNeedIdsAndAConditionOnEveryEntryButTheLast()
    {
        (List<string> errors, _) = Check(Day(10, new[] { Pell(5, "", PlannedDirective.WaiverMissing, FlagKeys.PremadeVerdict("pell", false)), Pell(5, "pell_2_first", PlannedDirective.WaiverMissing) }));
        Assert.AreEqual(1, errors.Count, "an entry of a slot with alternatives has no id");

        (errors, _) = Check(Day(10, new[] { Pell(5, "pell_2", PlannedDirective.WaiverMissing), Pell(5, "pell_2_first", PlannedDirective.WaiverMissing) }));
        Assert.AreEqual(1, errors.Count, "the first entry always stands, so the second never does");
        StringAssert.Contains("'pell_2'", errors[0]);

        (errors, _) = Check(Day(10, new[] { Pell(5, "twin"), Pell(6, "twin") }));
        Assert.IsTrue(errors.Any(e => e.Contains("'twin'")), "an id is unique in the day");
    }

    [Test]
    public void ForcedProblems_APremadeInTwoSlotsOfADay()
    {
        (List<string> errors, _) = Check(Day(7, new[] { Pell(5, "a"), Pell(9, "b") }));
        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains("two slots", errors[0]);
    }

    [Test]
    public void ForcedProblems_AnUnknownDialog()
    {
        ForcedCheck pell = Pell();
        pell.Dialog = "dlg_nowhere";
        Assert.AreEqual(1, Check(Day(7, new[] { pell })).errors.Count);
    }

    [Test]
    public void ForcedProblems_AOncePerRunPremadeForcedOnTwoDays_IsAWarning()
    {
        ForcedCheck Rook(int day) => new ForcedCheck { Slot = 7, Id = $"rook{day}", Premade = "rook", Kind = TravellerKind.Labourer, Forms = PoorSet, OncePerRun = true, ConditionKeys = new string[0] };
        (List<string> errors, List<string> warnings) = Check(Day(11, new[] { Rook(11) }), Day(12, new[] { Rook(12) }));
        CollectionAssert.IsEmpty(errors);
        Assert.AreEqual(1, warnings.Count);
        StringAssert.Contains("'rook'", warnings[0]);
    }

    [Test]
    public void ForcedProblems_ARepeatablePremadeForcedAgainWithoutACondition_IsAWarning()
    {
        (List<string> errors, List<string> warnings) = Check(Day(7, new[] { Pell() }), Day(10, new[] { Pell(5, "pell_2") }));
        CollectionAssert.IsEmpty(errors);
        Assert.AreEqual(1, warnings.Count);
        StringAssert.Contains("'pell'", warnings[0]);
    }

    [Test]
    public void Appearance_NoneStandingIsAnOrdinaryTraveller()
    {
        Assert.AreEqual(-1, Premades.Appearance(new[] { false, false }));
        Assert.AreEqual(-1, Premades.Appearance(new bool[0]));
        Assert.AreEqual(-1, Premades.Appearance(null));
    }

    [TestCase(false, false, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, true, false)]
    public void IsRollable_NotMetAndNameFree(bool met, bool nameTaken, bool expected)
    {
        Assert.AreEqual(expected, Premades.IsRollable(met, nameTaken));
    }

    /// <summary>The famous arrive on day 6 (traveller types K3, section 2.3): its forced slots, 8 and 11 of a queue of 14, are in the second half, outside the guaranteed faulty travellers' window (ViolatorSlots.Window).</summary>
    [TestCase(8)]
    [TestCase(11)]
    public void Day6ForcedSlots_AreOutsideTheViolatorWindow(int slot)
    {
        Assert.Greater(slot, ViolatorSlots.Window(14));
    }

    [Test]
    public void Roll_NoCandidates_IsMinusOne_WithNoDraw()
    {
        var rng = new ScriptedRandom();
        Assert.AreEqual(-1, Premades.Roll(1f, 0, false, rng));
        Assert.AreEqual(-1, Premades.Roll(1f, 0, true, rng));
        Assert.AreEqual(0, rng.Draws);
    }

    [Test]
    public void Roll_AValueAtOrAboveTheChance_IsMinusOne_AfterOneDraw()
    {
        var rng = new ScriptedRandom(ScriptStep.Value(0.05f));
        Assert.AreEqual(-1, Premades.Roll(0.05f, 3, false, rng));
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void Roll_AValueBelowTheChance_PicksWithOneRange()
    {
        var rng = new ScriptedRandom(ScriptStep.Value(0.01f), ScriptStep.Range(2));
        Assert.AreEqual(2, Premades.Roll(0.05f, 3, false, rng));
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void Roll_TheCheat_SkipsTheChanceDraw()
    {
        var rng = new ScriptedRandom(ScriptStep.Range(1));
        Assert.AreEqual(1, Premades.Roll(0f, 3, true, rng));
        Assert.IsTrue(rng.Done);
    }
}
