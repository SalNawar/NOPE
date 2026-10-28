using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

    [TestCase(9 * 60, 17 * 60, HallPhase.Normal)]
    [TestCase(8 * 60, 16 * 60, HallPhase.Normal)]
    [TestCase(13 * 60, 21 * 60, HallPhase.Extended)]
    [TestCase(10 * 60, 18 * 60, HallPhase.Extended)]
    [TestCase(16 * 60, 24 * 60, HallPhase.Nights)]
    [TestCase(14 * 60, 24 * 60, HallPhase.Nights)]
    public void Phase_FollowsTheShiftHoursRamp(int open, int close, HallPhase expected)
    {
        // The day's desk hours (night shifts, by week in the three-week progression: days 1-7 09:00-17:00, 8-14 13:00-21:00, 15-21 16:00-24:00) against the standard day.
        Assert.AreEqual(expected, HallStates.PhaseOf(new ShiftHours(open, close), new ShiftHours(9 * 60, 17 * 60)));
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
        Assert.AreEqual(HallEvent.Return, HallStates.EventOf(recall.Append(TravelRuleType.ReturnHome), recall), "the return");
        Assert.AreEqual(HallEvent.Ban, HallStates.EventOf(recall.Append(TravelRuleType.ReturnHome).Append(TravelRuleType.NationForbidden), recall), "a closure wins over the standing return (R16)");
        Assert.AreEqual(HallEvent.None, HallStates.EventOf(null, null));
    }

    /// <summary>
    /// The code review's R16 (2026-10-28): the 21-day progression put the
    /// border closure (Rule_NoModernGermany) on day 20, inside the return's
    /// days 19 to 21. With Return tested first no day of the run was a Ban
    /// day, so the checkpoint slot (event=ban) never showed. Read from the
    /// content source as Generate World reads it.
    /// </summary>
    [Test]
    public void TheRunsDays_ReadTheirEvents_Day20IsTheBan()
    {
        Dictionary<int, HallEvent> events = RunEvents();
        Assert.AreEqual(HallEvent.Ban, events[20], "day 20's closure shows the checkpoint, over the standing return");
        Assert.AreEqual(HallEvent.Return, events[19], "the return's first day");
        Assert.AreEqual(HallEvent.Return, events[21]);
        Assert.AreEqual(HallEvent.Recall, events[17], "the recall's first day");
        Assert.AreEqual(1, events.Values.Count(e => e == HallEvent.Ban), "one Ban day in the run");
    }

    /// <summary>Each authored day's event (HallStates.EventOf on its plan's rule types and the day before's, as HallSlotsLink reads them), from world_source.json.</summary>
    private static Dictionary<int, HallEvent> RunEvents([CallerFilePath] string here = "")
    {
        const string source = "Assets/Data/World/world_source.json";
        string path = File.Exists(source) ? source : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", source);
        ContentNode root = ContentJson.Parse(File.ReadAllText(path));
        var types = new Dictionary<string, TravelRuleType>(StringComparer.Ordinal);
        foreach (ContentNode rule in root.Get("rules").Items)
            if (Enum.TryParse(rule.Get("type").Text, out TravelRuleType type))
                types[rule.Get("asset").Text] = type;
        var byDay = new Dictionary<int, List<TravelRuleType>>();
        foreach (ContentNode day in root.Get("days").Items)
            byDay[int.Parse(day.Get("day").Text)] = day.Get("rules").Items.Where(r => types.ContainsKey(r.Text)).Select(r => types[r.Text]).ToList();
        Assert.IsTrue(byDay.ContainsKey(21), "the run's 21 days are authored");
        return byDay.ToDictionary(d => d.Key, d => HallStates.EventOf(d.Value, byDay.TryGetValue(d.Key - 1, out List<TravelRuleType> before) ? before : null));
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
