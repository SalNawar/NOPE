using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The hall's swappable slots (Saleh 2026-10-07: "assets that you can easily swap as the variables shift, to
/// create unique combinations"): the variables read from the run (the Helix River's tier, the phase, today's
/// special, the famous travellers let through), the conditions, and the pick with its deterministic tie-breaks
/// and seeded alternates.
/// </summary>
public class HallSlotsTests
{
    private const float FiredAt = 60f, Warning = 10f, Critical = 3f;

    private static HallState State(string culture = null, StabilityTier tier = StabilityTier.Steady, HallPhase phase = HallPhase.Normal,
        HallEvent e = HallEvent.None, string strongest = null, string recent = null, params HallExhibit[] exhibits) =>
        new HallState { Culture = culture, Tier = tier, Phase = phase, Event = e, StrongestExhibit = strongest, RecentExhibit = recent, Exhibits = exhibits };

    private static HallVariantDef V(string id, string when, int priority = 0, int alternates = 1) =>
        new HallVariantDef { id = id, when = when, priority = priority, alternates = alternates };

    private static HallSlotDef Slot(string id, params HallVariantDef[] variants) =>
        new HallSlotDef { id = id, x = 10, y = 10, width = 20, height = 20, variants = variants.ToList() };

    // ---- the tier: the Helix River's own thresholds ----

    [TestCase(100f, StabilityTier.Steady)]
    [TestCase(90.01f, StabilityTier.Steady)]
    [TestCase(89.99f, StabilityTier.Strained, TestName = "Tier_StrainedOnceOxbowsPinchOff (calm under 0.75)")]
    [TestCase(70.01f, StabilityTier.Strained)]
    [TestCase(70f, StabilityTier.Breaching, TestName = "Tier_BreachingFromTheWarningLine (the glitch)")]
    [TestCase(63.01f, StabilityTier.Breaching)]
    [TestCase(63f, StabilityTier.Collapsing, TestName = "Tier_CollapsingInTheCriticalBand (the flicker)")]
    [TestCase(40f, StabilityTier.Collapsing)]
    public void Tier_FollowsTheHelixRiversThresholds(float stability, StabilityTier expected)
    {
        Assert.AreEqual(expected, HelixRiver.Tier(stability, FiredAt, Warning, Critical, new HelixRiverKnobs()));
    }

    [Test]
    public void Tier_NeverImprovesAsStabilityFalls()
    {
        StabilityTier last = StabilityTier.Steady;
        for (float s = 100f; s >= 40f; s -= 0.25f)
        {
            StabilityTier t = HelixRiver.Tier(s, FiredAt, Warning, Critical, null);
            Assert.GreaterOrEqual((int)t, (int)last, $"at {s}");
            last = t;
        }
    }

    [Test]
    public void Tier_StrainedLineIsTheRiversOxbowKnob()
    {
        var knobs = new HelixRiverKnobs { oxbowsFrom = 0.5f };
        Assert.AreEqual(StabilityTier.Steady, HelixRiver.Tier(85f, FiredAt, Warning, Critical, knobs));
        Assert.AreEqual(StabilityTier.Strained, HelixRiver.Tier(79f, FiredAt, Warning, Critical, knobs));
        Assert.AreEqual(HelixRiver.CalmOf(85f, FiredAt, knobs), new HelixRiver(knobs).Calm(85f, FiredAt), "one calm for the river and the hall");
    }

    // ---- phase and today's special ----

    [TestCase(1, HallPhase.Normal)]
    [TestCase(7, HallPhase.Normal)]
    [TestCase(8, HallPhase.Extended)]
    [TestCase(11, HallPhase.Extended)]
    [TestCase(12, HallPhase.Nights)]
    [TestCase(15, HallPhase.Nights)]
    public void Phase_FollowsTheShiftHoursRamp(int day, HallPhase expected)
    {
        Assert.AreEqual(expected, HallStates.PhaseOf(day, 8, 12));
    }

    [Test]
    public void Event_ReadsTheDayPlansRules()
    {
        var recall = new[] { TravelRuleType.OpenDestinations, TravelRuleType.TransponderRecall };
        Assert.AreEqual(HallEvent.Recall, HallStates.EventOf(recall, new[] { TravelRuleType.OpenDestinations }), "the recall's first day");
        Assert.AreEqual(HallEvent.None, HallStates.EventOf(recall, recall), "a recall already in force is no news");
        Assert.AreEqual(HallEvent.Ban, HallStates.EventOf(recall.Append(TravelRuleType.NationEraForbidden), recall), "a border closure");
        Assert.AreEqual(HallEvent.Ban, HallStates.EventOf(new[] { TravelRuleType.NationForbidden }, null));
        Assert.AreEqual(HallEvent.None, HallStates.EventOf(new[] { TravelRuleType.EraForbidden }, null), "an era range limit is no border closure");
        Assert.AreEqual(HallEvent.Return, HallStates.EventOf(recall.Append(TravelRuleType.ReturnHome).Append(TravelRuleType.NationForbidden), recall), "the return wins");
        Assert.AreEqual(HallEvent.None, HallStates.EventOf(null, null));
    }

    // ---- the exhibits ----

    private static readonly Dictionary<string, string> Nations = new Dictionary<string, string>
    {
        { "cleopatra", "egypt" }, { "ramesses", "egypt" }, { "socrates", "greece" }, { "kurosawa", "japan" }, { "ghost", "" }
    };

    [Test]
    public void Exhibits_CountAcceptedPremadesByNation_StrongestThenMostRecent()
    {
        var flags = new List<string>
        {
            "premade:socrates:met", "premade:socrates:accepted", "premade:cleopatra:accepted", "trig:x:fired",
            "premade:kurosawa:denied", "premade:ramesses:accepted", "premade:ghost:accepted", "premade:kurosawa:accepted"
        };
        List<HallExhibit> exhibits = HallStates.ExhibitsOf(flags, id => Nations.TryGetValue(id, out string n) ? n : null, out string recent, out string strongest);
        CollectionAssert.AreEqual(new[] { "egypt", "japan", "greece" }, exhibits.Select(e => e.Nation), "egypt twice; japan accepted after greece");
        CollectionAssert.AreEqual(new[] { 2, 1, 1 }, exhibits.Select(e => e.Count));
        Assert.AreEqual("japan", recent);
        Assert.AreEqual("egypt", strongest);
    }

    [Test]
    public void Exhibits_NoneWithoutAcceptedPremades()
    {
        List<HallExhibit> exhibits = HallStates.ExhibitsOf(new[] { "premade:socrates:denied" }, id => Nations[id], out string recent, out string strongest);
        Assert.IsEmpty(exhibits);
        Assert.IsNull(recent);
        Assert.IsNull(strongest);
    }

    // ---- conditions ----

    [Test]
    public void Conditions_MatchEachVariable()
    {
        HallState s = State("egypt", StabilityTier.Breaching, HallPhase.Extended, HallEvent.Ban, "greece", "japan",
            new HallExhibit("greece", 2), new HallExhibit("japan", 1));
        Assert.IsTrue(HallConditions.Matches("", s));
        Assert.IsTrue(HallConditions.Matches("culture=egypt", s));
        Assert.IsFalse(HallConditions.Matches("culture=neutral", s));
        Assert.IsTrue(HallConditions.Matches("culture!=neutral", s));
        Assert.IsTrue(HallConditions.Matches("tier>=strained", s));
        Assert.IsTrue(HallConditions.Matches("tier>=breaching", s));
        Assert.IsFalse(HallConditions.Matches("tier>=collapsing", s));
        Assert.IsTrue(HallConditions.Matches("tier<=breaching", s));
        Assert.IsTrue(HallConditions.Matches("phase=extended", s));
        Assert.IsFalse(HallConditions.Matches("phase>=nights", s));
        Assert.IsTrue(HallConditions.Matches("event=ban", s));
        Assert.IsTrue(HallConditions.Matches("exhibit=greece", s));
        Assert.IsTrue(HallConditions.Matches("recent=japan", s));
        Assert.IsTrue(HallConditions.Matches("exhibit:japan", s));
        Assert.IsFalse(HallConditions.Matches("exhibit:egypt", s));
        Assert.IsTrue(HallConditions.Matches(" phase>=extended & culture=egypt ", s));
        Assert.IsFalse(HallConditions.Matches("phase>=extended & culture=japan", s));
    }

    [Test]
    public void Conditions_NeutralAndNoExhibit()
    {
        HallState s = State();
        Assert.IsTrue(HallConditions.Matches("culture=neutral", s));
        Assert.IsTrue(HallConditions.Matches("exhibit=none", s));
        Assert.IsTrue(HallConditions.Matches("recent=none", s));
        Assert.IsTrue(HallConditions.Matches("event=none & tier=steady & phase=normal", s));
    }

    [Test]
    public void Conditions_ABadClauseNeverMatches_AndIsReported()
    {
        HallState s = State("egypt");
        Assert.IsFalse(HallConditions.Matches("colour=egypt", s));
        Assert.IsFalse(HallConditions.Matches("tier>=wobbly", s));
        Assert.IsFalse(HallConditions.Matches("event>=ban", s), "events are not ordered");
        Assert.IsFalse(HallConditions.Matches("culture>=egypt", s));
        Assert.IsEmpty(HallConditions.Problems("culture=egypt & tier>=strained & exhibit:greece", new[] { "egypt", "greece" }));
        Assert.AreEqual(1, HallConditions.Problems("culture=atlantis", new[] { "egypt" }).Count, "an unknown nation");
        Assert.AreEqual(2, HallConditions.Problems("colour=red & phase=dusk", null).Count);
    }

    // ---- the pick ----

    [Test]
    public void Pick_HighestPriorityWins_TheFirstAmongEquals()
    {
        HallSlotDef slot = Slot("posters", V("plain", ""), V("egypt", "culture=egypt", 10), V("nights", "phase=nights", 20), V("nights_egypt", "phase=nights & culture=egypt", 20));
        Assert.AreEqual("plain", HallSlotPick.Pick(slot, State(), 1).File);
        Assert.AreEqual("egypt", HallSlotPick.Pick(slot, State("egypt"), 1).File);
        Assert.AreEqual("nights", HallSlotPick.Pick(slot, State("egypt", phase: HallPhase.Nights), 1).File, "listed first among priority 20");
        Assert.AreEqual("nights", HallSlotPick.Pick(slot, State("japan", phase: HallPhase.Nights), 1).File);
    }

    [Test]
    public void Pick_FallbackElseNothing()
    {
        HallSlotDef slot = Slot("banners", V("egypt", "culture=egypt"), V("plain", "culture=atlantis"));
        Assert.IsFalse(HallSlotPick.Pick(slot, State("japan"), 1).Shows, "no variant: the painting as it is");
        slot.fallback = "plain";
        Assert.AreEqual("plain", HallSlotPick.Pick(slot, State("japan"), 1).File);
        Assert.AreEqual("egypt", HallSlotPick.Pick(slot, State("egypt"), 1).Variant);
    }

    [Test]
    public void Pick_AlternatesAreSeededPerRun_ReplaysMatch_RunsDiffer()
    {
        HallSlotDef slot = Slot("13-flag-left-cloth", V("egypt", "culture=egypt", 0, 3));
        HallState egypt = State("egypt");
        var files = new HashSet<string>();
        for (int run = 1; run <= 40; run++)
        {
            HallPick pick = HallSlotPick.Pick(slot, egypt, run);
            Assert.AreEqual("egypt", pick.Variant);
            StringAssert.IsMatch("^egypt_[123]$", pick.File);
            Assert.AreEqual(pick.File, HallSlotPick.Pick(slot, egypt, run).File, "a replay shows the same banner");
            files.Add(pick.File);
        }
        Assert.AreEqual(3, files.Count, "runs differ: every alternate shows in some run");
    }

    [Test]
    public void Problems_NameEachSlotsMistake()
    {
        var slots = new List<HallSlotDef>
        {
            Slot("a", V("x", "culture=egypt")),
            Slot("a", V("x", ""), V("x", "tier>=nope"), V("y", "", 0, 0)),
            new HallSlotDef { id = "off", x = 2160, y = 0, width = 20, height = 10, fallback = "missing" },
        };
        List<string> problems = HallSlotPick.Problems(slots, 2172, 724, new[] { "egypt" });
        Assert.That(problems, Has.Some.Contains("a: the id is used twice"));
        Assert.That(problems, Has.Some.Contains("a/x: the variant id is used twice"));
        Assert.That(problems, Has.Some.Contains("a/x: 'tier>=nope' does not parse"));
        Assert.That(problems, Has.Some.Contains("a/y: alternates must be 1 or more"));
        Assert.That(problems, Has.Some.Contains("off: region"));
        Assert.That(problems, Has.Some.Contains("off: the fallback 'missing'"));
        Assert.IsEmpty(HallSlotPick.Problems(new[] { Slot("ok", V("egypt", "culture=egypt")) }, 2172, 724, new[] { "egypt" }));
    }

    [Test]
    public void SameAs_ComparesEveryVariable()
    {
        Assert.IsTrue(State("egypt").SameAs(State("egypt")));
        Assert.IsFalse(State("egypt").SameAs(State("japan")));
        Assert.IsFalse(State().SameAs(State(tier: StabilityTier.Strained)));
        Assert.IsFalse(State(exhibits: new HallExhibit("egypt", 1)).SameAs(State(exhibits: new HallExhibit("egypt", 2))));
        Assert.AreEqual("culture=neutral tier=steady phase=normal event=none exhibit=none recent=none", State().ToString());
    }
}
