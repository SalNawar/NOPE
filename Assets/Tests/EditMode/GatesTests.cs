using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The gate decision table (timeline triggers, interview questions, dialogs),
/// the snapshot it reads, the two readings of "DayAtLeast", and the run-flag
/// grammar. Snapshot under test: day 3, stability 40, flag "met_tesla",
/// upgrade "adv_scanner", counter "sent:tag:scientist" = 5, scores
/// "attr:egypt_ancient:science" = 12 and "nation:egypt" = 7, dominant
/// "egypt_ancient:science", supporting "egypt_ancient:art".
/// </summary>
public class GatesTests
{
    private static GateSnapshot Snap(int day = 3, float stability = 40f) => new GateSnapshot(
        day, stability,
        new[] { "met_tesla" },
        new[] { "adv_scanner" },
        new[] { new KeyValuePair<string, int>("sent:tag:scientist", 5) },
        new[] { new KeyValuePair<string, float>("attr:egypt_ancient:science", 12f), new KeyValuePair<string, float>("nation:egypt", 7f) },
        new[] { "egypt_ancient:science" },
        new[] { "egypt_ancient:art" });

    private static bool Passes(TriggerConditionType type, string key, float threshold, GateSnapshot s = null) =>
        Gates.Passes(new GateCondition(type, key, threshold), s ?? Snap());

    [Test]
    public void CounterAtLeast_ComparesTheCounter_ANullKeyReadsZero()
    {
        Assert.IsTrue(Passes(TriggerConditionType.CounterAtLeast, "sent:tag:scientist", 5f));
        Assert.IsFalse(Passes(TriggerConditionType.CounterAtLeast, "sent:tag:scientist", 6f));
        Assert.IsFalse(Passes(TriggerConditionType.CounterAtLeast, "sent:tag:artist", 1f), "a missing counter reads 0");
        Assert.IsTrue(Passes(TriggerConditionType.CounterAtLeast, null, 0f), "a null key reads 0, which passes threshold 0 (as today)");
        Assert.IsFalse(Passes(TriggerConditionType.CounterAtLeast, null, 1f));
    }

    [Test]
    public void FlagSet_And_FlagNotSet_ReadTheFlags()
    {
        Assert.IsTrue(Passes(TriggerConditionType.FlagSet, "met_tesla", 0f));
        Assert.IsFalse(Passes(TriggerConditionType.FlagSet, "met_edison", 0f));
        Assert.IsFalse(Passes(TriggerConditionType.FlagNotSet, "met_tesla", 0f));
        Assert.IsTrue(Passes(TriggerConditionType.FlagNotSet, "met_edison", 0f));
        Assert.IsFalse(Passes(TriggerConditionType.FlagSet, null, 0f), "a null key is never set");
        Assert.IsTrue(Passes(TriggerConditionType.FlagNotSet, null, 0f), "a null key is never set, so FlagNotSet passes (as today)");
    }

    [Test]
    public void AttributeScores_CompareTheScore_AndNeedAKey()
    {
        Assert.IsTrue(Passes(TriggerConditionType.AttributeScoreAtLeast, "attr:egypt_ancient:science", 12f));
        Assert.IsFalse(Passes(TriggerConditionType.AttributeScoreAtLeast, "attr:egypt_ancient:science", 12.5f));
        Assert.IsTrue(Passes(TriggerConditionType.AttributeScoreAtMost, "attr:egypt_ancient:science", 12f));
        Assert.IsFalse(Passes(TriggerConditionType.AttributeScoreAtMost, "attr:egypt_ancient:science", 11f));
        Assert.IsTrue(Passes(TriggerConditionType.AttributeScoreAtMost, "attr:egypt_ancient:art", 0f), "a missing score reads 0");
        Assert.IsFalse(Passes(TriggerConditionType.AttributeScoreAtLeast, null, -100f), "an unresolved reference never passes");
        Assert.IsFalse(Passes(TriggerConditionType.AttributeScoreAtMost, null, 100f), "an unresolved reference never passes");
    }

    [Test]
    public void DominanceTiers_ReadTheTierKeys_AndNeedAKey()
    {
        Assert.IsTrue(Passes(TriggerConditionType.AttributeIsDominant, "egypt_ancient:science", 0f));
        Assert.IsFalse(Passes(TriggerConditionType.AttributeIsDominant, "egypt_ancient:art", 0f));
        Assert.IsTrue(Passes(TriggerConditionType.AttributeIsSupporting, "egypt_ancient:art", 0f));
        Assert.IsFalse(Passes(TriggerConditionType.AttributeIsSupporting, "egypt_ancient:science", 0f));
        Assert.IsFalse(Passes(TriggerConditionType.AttributeIsDominant, null, 0f));
        Assert.IsFalse(Passes(TriggerConditionType.AttributeIsSupporting, null, 0f));
    }

    [Test]
    public void NationScoreAtLeast_ComparesTheNationScore_AndNeedsAKey()
    {
        Assert.IsTrue(Passes(TriggerConditionType.NationScoreAtLeast, "nation:egypt", 7f));
        Assert.IsFalse(Passes(TriggerConditionType.NationScoreAtLeast, "nation:egypt", 8f));
        Assert.IsFalse(Passes(TriggerConditionType.NationScoreAtLeast, null, -100f));
    }

    [Test]
    public void DayAtLeast_And_StabilityAtMost_ReadTheSnapshot()
    {
        Assert.IsTrue(Passes(TriggerConditionType.DayAtLeast, null, 3f));
        Assert.IsFalse(Passes(TriggerConditionType.DayAtLeast, null, 4f));
        Assert.IsTrue(Passes(TriggerConditionType.StabilityAtMost, null, 40f));
        Assert.IsFalse(Passes(TriggerConditionType.StabilityAtMost, null, 39.9f));
    }

    [Test]
    public void UpgradeOwned_ReadsUpgrades_NeverTheUnlockFlag()
    {
        Assert.IsTrue(Passes(TriggerConditionType.UpgradeOwned, "adv_scanner", 0f));
        Assert.IsFalse(Passes(TriggerConditionType.UpgradeOwned, "archive_access", 0f));
        Assert.IsFalse(Passes(TriggerConditionType.UpgradeOwned, null, 0f));

        var flagOnly = new GateSnapshot(1, 100f, new[] { "upgrade:archive_access" }, null, null, null, null, null);
        Assert.IsFalse(Passes(TriggerConditionType.UpgradeOwned, "archive_access", 0f, flagOnly), "the 'upgrade:x' flag alone is not ownership");
    }

    [Test]
    public void AnUnknownConditionType_NeverPasses()
    {
        Assert.IsFalse(Passes((TriggerConditionType)99, "met_tesla", 0f));
    }

    [Test]
    public void ANullSnapshot_PassesNothing()
    {
        Assert.IsFalse(Gates.Passes(new GateCondition(TriggerConditionType.FlagNotSet, "x", 0f), null));
        Assert.IsFalse(Gates.AllPass(new[] { new GateCondition(TriggerConditionType.DayAtLeast, null, 0f) }, null));
    }

    [Test]
    public void AllPass_TrueForNoConditions_FalseAsSoonAsOneFails()
    {
        Assert.IsTrue(Gates.AllPass(null, Snap()));
        Assert.IsTrue(Gates.AllPass(new GateCondition[0], Snap()));

        var dayAndFlag = new[]
        {
            new GateCondition(TriggerConditionType.DayAtLeast, null, 2f),
            new GateCondition(TriggerConditionType.FlagSet, "met_tesla", 0f)
        };
        Assert.IsTrue(Gates.AllPass(dayAndFlag, Snap()));
        Assert.IsFalse(Gates.AllPass(dayAndFlag, Snap(day: 1)));

        var oneFails = new[]
        {
            new GateCondition(TriggerConditionType.DayAtLeast, null, 2f),
            new GateCondition(TriggerConditionType.FlagSet, "met_edison", 0f)
        };
        Assert.IsFalse(Gates.AllPass(oneFails, Snap()));
    }

    [Test]
    public void GateSnapshot_CopiesItsInputs()
    {
        var flags = new List<string> { "a" };
        var upgrades = new List<string> { "u" };
        var counters = new List<KeyValuePair<string, int>> { new KeyValuePair<string, int>("c", 1) };
        var scores = new List<KeyValuePair<string, float>> { new KeyValuePair<string, float>("s", 1f) };
        var dominant = new List<string> { "d" };
        var supporting = new List<string> { "p" };
        var s = new GateSnapshot(2, 50f, flags, upgrades, counters, scores, dominant, supporting);

        flags.Clear(); flags.Add("b");
        upgrades.Clear();
        counters.Clear(); counters.Add(new KeyValuePair<string, int>("c", 9));
        scores.Clear();
        dominant.Clear();
        supporting.Clear();

        Assert.AreEqual(2, s.Day);
        Assert.AreEqual(50f, s.Stability);
        Assert.IsTrue(s.HasFlag("a"));
        Assert.IsFalse(s.HasFlag("b"));
        Assert.IsTrue(s.HasUpgrade("u"));
        Assert.AreEqual(1, s.Counter("c"));
        Assert.AreEqual(1f, s.Score("s"));
        Assert.IsTrue(s.IsDominant("d"));
        Assert.IsTrue(s.IsSupporting("p"));
    }

    [Test]
    public void GateSnapshot_NullOrBlankArguments_ReadFalseOrZero()
    {
        var s = new GateSnapshot(1, 100f, null, null, null, null, null, null);
        Assert.IsFalse(s.HasFlag(null));
        Assert.IsFalse(s.HasFlag(""));
        Assert.IsFalse(s.HasUpgrade(" "));
        Assert.AreEqual(0, s.Counter(null));
        Assert.AreEqual(0f, s.Score(""));
        Assert.IsFalse(s.IsDominant(null));
        Assert.IsFalse(s.IsSupporting(null));
    }

    [Test]
    public void DayAtLeast_TheNightlyResolveAndTheDayStartReadDifferentDays()
    {
        // A trigger announcing a day-3 question fires the night of day 2
        // (the nightly resolve snapshots before day++)...
        var unlockTrigger = new GateCondition(TriggerConditionType.DayAtLeast, null, Gates.UnlockNight(3));
        Assert.IsTrue(Gates.Passes(unlockTrigger, Snap(day: 2)));
        Assert.IsFalse(Gates.Passes(unlockTrigger, Snap(day: 1)));

        // ...while the question itself is askable from the start of day 3.
        var question = new GateCondition(TriggerConditionType.DayAtLeast, null, 3f);
        Assert.IsFalse(Gates.Passes(question, Snap(day: 2)));
        Assert.IsTrue(Gates.Passes(question, Snap(day: 3)));

        Assert.AreEqual(1, Gates.UnlockNight(2));
    }

    [Test]
    public void DayOnly_TrueOnlyWhenEveryConditionIsADayGate()
    {
        Assert.IsTrue(Gates.DayOnly(null));
        Assert.IsTrue(Gates.DayOnly(new GateCondition[0]));
        Assert.IsTrue(Gates.DayOnly(new[] { new GateCondition(TriggerConditionType.DayAtLeast, null, 2f) }));
        Assert.IsFalse(Gates.DayOnly(new[]
        {
            new GateCondition(TriggerConditionType.DayAtLeast, null, 2f),
            new GateCondition(TriggerConditionType.UpgradeOwned, "interview_protocols", 0f)
        }));
        Assert.IsFalse(Gates.DayOnly(new[] { new GateCondition(TriggerConditionType.FlagSet, "rumour_calculators", 0f) }));
    }

    [Test]
    public void FlagKeys_KeepTheFormatSavesHold()
    {
        Assert.AreEqual("trig:x:fired", FlagKeys.TriggerFired("x"));
        Assert.AreEqual("dlg:x:done", FlagKeys.DialogDone("x"));
    }

    private static GateSnapshot LeaderSnap(string leaderId) => new GateSnapshot(
        3, 40f, null, null, null,
        new[] { new KeyValuePair<string, float>("attrTotal:art", 20f), new KeyValuePair<string, float>("attrTotal:democracy", -6f) },
        null, null, leaderId);

    [Test]
    public void NationIsLeader_ReadsTheLeader_AndNeedsAKey()
    {
        Assert.IsTrue(Passes(TriggerConditionType.NationIsLeader, "china", 0f, LeaderSnap("china")));
        Assert.IsFalse(Passes(TriggerConditionType.NationIsLeader, "japan", 0f, LeaderSnap("china")), "another nation");
        Assert.IsFalse(Passes(TriggerConditionType.NationIsLeader, "china", 0f, LeaderSnap(null)), "no leader");
        Assert.IsFalse(Passes(TriggerConditionType.NationIsLeader, "china", 0f, LeaderSnap("")), "no leader (blank)");
        Assert.IsFalse(Passes(TriggerConditionType.NationIsLeader, null, 0f, LeaderSnap("china")), "an unresolved reference never passes");
        Assert.IsNull(Snap().LeaderId, "the leader is an optional trailing argument");
    }

    [Test]
    public void GlobalAttr_ComparesTheTotal_AMissingTotalReadsZero()
    {
        GateSnapshot s = LeaderSnap(null);
        Assert.IsTrue(Passes(TriggerConditionType.GlobalAttrAtLeast, "attrTotal:art", 20f, s));
        Assert.IsFalse(Passes(TriggerConditionType.GlobalAttrAtLeast, "attrTotal:art", 20.5f, s));
        Assert.IsTrue(Passes(TriggerConditionType.GlobalAttrAtMost, "attrTotal:democracy", -6f, s));
        Assert.IsFalse(Passes(TriggerConditionType.GlobalAttrAtMost, "attrTotal:democracy", -6.5f, s));
        Assert.IsTrue(Passes(TriggerConditionType.GlobalAttrAtMost, "attrTotal:science", 0f, s), "a missing total reads 0");
        Assert.IsFalse(Passes(TriggerConditionType.GlobalAttrAtMost, "attrTotal:science", -6f, s), "the Democratic Collapse trap: at -6 nothing fires before any send");
        Assert.IsFalse(Passes(TriggerConditionType.GlobalAttrAtLeast, null, -100f, s), "an unresolved reference never passes");
        Assert.IsFalse(Passes(TriggerConditionType.GlobalAttrAtMost, null, 100f, s), "an unresolved reference never passes");
    }

    [Test]
    public void TriggerConditionType_KeepsItsSerializedInts()
    {
        Assert.AreEqual(0, (int)TriggerConditionType.CounterAtLeast);
        Assert.AreEqual(1, (int)TriggerConditionType.FlagSet);
        Assert.AreEqual(2, (int)TriggerConditionType.FlagNotSet);
        Assert.AreEqual(3, (int)TriggerConditionType.AttributeScoreAtLeast);
        Assert.AreEqual(4, (int)TriggerConditionType.AttributeScoreAtMost);
        Assert.AreEqual(5, (int)TriggerConditionType.AttributeIsDominant);
        Assert.AreEqual(6, (int)TriggerConditionType.AttributeIsSupporting);
        Assert.AreEqual(7, (int)TriggerConditionType.NationScoreAtLeast);
        Assert.AreEqual(8, (int)TriggerConditionType.DayAtLeast);
        Assert.AreEqual(9, (int)TriggerConditionType.StabilityAtMost);
        Assert.AreEqual(10, (int)TriggerConditionType.UpgradeOwned);
        Assert.AreEqual(11, (int)TriggerConditionType.NationIsLeader);
        Assert.AreEqual(12, (int)TriggerConditionType.GlobalAttrAtLeast, "the retuned Art Renaissance and Science Boom assets store 12");
        Assert.AreEqual(13, (int)TriggerConditionType.GlobalAttrAtMost, "the retuned Democratic Collapse asset stores 13");
    }
}
