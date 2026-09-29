using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// How the run's choices become the world's answers (the endings spec §4,
/// E2): pulls are never negative; an accepted traveller pulls their role's
/// factor toward the destination's leaning times their kind's scale; a famous
/// traveller's authored pulls stand instead; a denial pulls each "as you found
/// it" outcome; the lead needs more than the margin, a gap within it is a
/// split, a held lead survives a gap within the margin; the night's latch
/// reports only a change; an older save seeds once. Status-quo weight 6,
/// margin 2 unless stated.
/// </summary>
public class WorldPullsTests
{
    private static readonly PullFactor Government = new PullFactor("government", "directorate",
        new[] { "directorate", "democracy", "monarchy", "fascism", "communism", "theocracy", "technocracy", "corporate_board" });

    private static readonly PullFactor Future = new PullFactor("future", "credit_age",
        new[] { "credit_age", "nuclear", "cybernetic", "space_age", "naturalism", "anarchy" });

    private static readonly List<WorldRole> Roles = new List<WorldRole>
    {
        new WorldRole { archetype = "scientist", factor = "future", pull = 2f },
        new WorldRole { archetype = "diplomat", factor = "government", pull = 2f },
        new WorldRole { archetype = "soldier", factor = "government", pull = 2f },
        new WorldRole { archetype = "merchant", factor = "money", pull = 2f },
    };

    private static readonly List<OutcomeRef> MeijiNagoya = new List<OutcomeRef>
    {
        new OutcomeRef { factor = "government", outcome = "monarchy" },
        new OutcomeRef { factor = "future", outcome = "cybernetic" },
        new OutcomeRef { factor = "money", outcome = "company_towns" },
    };

    private static List<OutcomePull> P(string factor, params (string outcome, float amount)[] pulls) =>
        pulls.Select(p => new OutcomePull { factor = factor, outcome = p.outcome, amount = p.amount }).ToList();

    private static FactorLead Held(string outcome, string split = "", int since = 3) =>
        new FactorLead { factor = "government", outcome = outcome, split = split, sinceDay = since };

    [Test]
    public void Add_MergesOneEntryPerOutcome_AndNeverTakesANegativeOrZero()
    {
        var pulls = new List<OutcomePull>();
        Assert.IsTrue(WorldPulls.Add(pulls, "future", "nuclear", 2f));
        Assert.IsTrue(WorldPulls.Add(pulls, "future", "nuclear", 0.5f));
        Assert.IsFalse(WorldPulls.Add(pulls, "future", "nuclear", -3f));
        Assert.IsFalse(WorldPulls.Add(pulls, "future", "nuclear", 0f));
        Assert.IsFalse(WorldPulls.Add(pulls, "future", "nuclear", float.NaN));
        Assert.IsFalse(WorldPulls.Add(pulls, "", "nuclear", 1f));
        Assert.IsFalse(WorldPulls.Add(pulls, "future", " ", 1f));

        Assert.AreEqual(1, pulls.Count);
        Assert.AreEqual(2.5f, WorldPulls.Weight(pulls, "future", "nuclear"), 1e-5f);
        Assert.AreEqual(0f, WorldPulls.Weight(pulls, "future", "cybernetic"));
    }

    [Test]
    public void ForAccept_TheRolesFactor_TowardTheDestinationsLeaning_TimesTheKindsScale()
    {
        List<OutcomePull> pulls = WorldPulls.ForAccept(null, Roles, "scientist", MeijiNagoya, 1f);
        Assert.AreEqual(1, pulls.Count);
        Assert.AreEqual(("future", "cybernetic", 2f), (pulls[0].factor, pulls[0].outcome, pulls[0].amount));

        Assert.AreEqual(1f, WorldPulls.ForAccept(null, Roles, "scientist", MeijiNagoya, 0.5f)[0].amount, 1e-5f);
        Assert.AreEqual("monarchy", WorldPulls.ForAccept(null, Roles, "soldier", MeijiNagoya, 1f)[0].outcome);
    }

    [TestCase("wanderer", Description = "a role that pulls nothing")]
    [TestCase("artist", Description = "artists feed the culture through influence")]
    [TestCase("", Description = "no archetype")]
    public void ForAccept_ARoleWithNoFactor_AddsNothing(string archetype)
    {
        CollectionAssert.IsEmpty(WorldPulls.ForAccept(null, Roles, archetype, MeijiNagoya, 1f));
    }

    [Test]
    public void ForAccept_ADestinationWithNoLeaningOnTheFactor_AddsNothing()
    {
        var ioannina = new List<OutcomeRef> { new OutcomeRef { factor = "government", outcome = "theocracy" } };
        CollectionAssert.IsEmpty(WorldPulls.ForAccept(null, Roles, "scientist", ioannina, 1f));
        CollectionAssert.IsEmpty(WorldPulls.ForAccept(null, Roles, "scientist", null, 1f));
        CollectionAssert.IsEmpty(WorldPulls.ForAccept(null, Roles, "scientist", MeijiNagoya, 0f), "a kind scaled to 0 pulls nothing");
    }

    [Test]
    public void ForAccept_AFamousTravellersAuthoredPulls_StandInsteadOfTheRole_Unscaled()
    {
        List<OutcomePull> gutenberg = P("money", ("commons", 5f)).Concat(P("future", ("anarchy", 3f))).ToList();
        List<OutcomePull> pulls = WorldPulls.ForAccept(gutenberg, Roles, "scientist", MeijiNagoya, 0.5f);

        CollectionAssert.AreEqual(new[] { "commons", "anarchy" }, pulls.Select(p => p.outcome).ToArray());
        CollectionAssert.AreEqual(new[] { 5f, 3f }, pulls.Select(p => p.amount).ToArray());
    }

    [Test]
    public void ForDenial_PullsEachFactorsStatusQuo()
    {
        List<OutcomePull> pulls = WorldPulls.ForDenial(new[] { Government, Future }, 0.5f);
        CollectionAssert.AreEqual(new[] { "directorate", "credit_age" }, pulls.Select(p => p.outcome).ToArray());
        Assert.IsTrue(pulls.All(p => p.amount == 0.5f));
        CollectionAssert.IsEmpty(WorldPulls.ForDenial(new[] { Government }, 0f));
    }

    [TestCase(TravellerKind.RichTourist, 0.5f)]
    [TestCase(TravellerKind.PoorTourist, 0.25f)]
    [TestCase(TravellerKind.Labourer, 1f)]
    [TestCase(TravellerKind.Displaced, 1.5f)]
    public void KindScale_ReadsTheKindsKnob(TravellerKind kind, float expected)
    {
        Assert.AreEqual(expected, WorldPulls.KindScale(kind, 0.5f, 0.25f, 1f, 1.5f));
        Assert.AreEqual(0f, WorldPulls.KindScale(kind, -1f, -1f, -1f, -1f), "never below 0");
    }

    [Test]
    public void Lead_EveryFactorStartsAsFound()
    {
        FactorLead lead = WorldPulls.Lead(Government, null, 6f, 2f, null);
        Assert.AreEqual("directorate", lead.outcome);
        Assert.IsFalse(lead.IsSplit);
    }

    [Test]
    public void Lead_NeedsMoreThanTheMargin_ElseASplitOfTheTopTwo()
    {
        List<OutcomePull> pulls = P("government", ("monarchy", 12f), ("democracy", 9f));
        Assert.AreEqual("monarchy", WorldPulls.Lead(Government, pulls, 6f, 2f, Held("democracy", "monarchy")).outcome);
        Assert.IsFalse(WorldPulls.Lead(Government, pulls, 6f, 2f, Held("democracy", "monarchy")).IsSplit);

        pulls = P("government", ("monarchy", 12f), ("democracy", 10f));
        FactorLead split = WorldPulls.Lead(Government, pulls, 6f, 2f, Held("democracy", "monarchy"));
        Assert.AreEqual(("monarchy", "democracy"), (split.outcome, split.split), "a gap of exactly the margin is a split");
    }

    [Test]
    public void Lead_ATie_IsASplit_InContentOrder()
    {
        List<OutcomePull> pulls = P("government", ("theocracy", 10f), ("democracy", 10f));
        FactorLead split = WorldPulls.Lead(Government, pulls, 6f, 2f, Held("monarchy", "fascism"));
        Assert.AreEqual(("democracy", "theocracy"), (split.outcome, split.split));
    }

    [Test]
    public void Lead_AHeldLead_SurvivesAGapWithinTheMargin_AndFallsBeyondIt()
    {
        List<OutcomePull> pulls = P("government", ("monarchy", 10f), ("democracy", 12f));
        FactorLead kept = WorldPulls.Lead(Government, pulls, 6f, 2f, Held("monarchy"));
        Assert.AreEqual("monarchy", kept.outcome);
        Assert.AreEqual(3, kept.sinceDay, "the same answer keeps its day");

        pulls = P("government", ("monarchy", 10f), ("democracy", 12.5f));
        FactorLead passed = WorldPulls.Lead(Government, pulls, 6f, 2f, Held("monarchy"));
        Assert.AreEqual("democracy", passed.outcome);
        Assert.AreEqual(0, passed.sinceDay, "a new answer is dated by the caller");
    }

    [Test]
    public void Lead_NoHeldLead_ReadsAsFound_WithItsInertia()
    {
        Assert.AreEqual("directorate", WorldPulls.Lead(Government, P("government", ("monarchy", 8f)), 6f, 2f, null).outcome);
        Assert.AreEqual("monarchy", WorldPulls.Lead(Government, P("government", ("monarchy", 8.5f)), 6f, 2f, null).outcome);
    }

    [Test]
    public void Lead_IgnoresOutcomesTheFactorDoesNotList_AndOtherFactors()
    {
        List<OutcomePull> pulls = P("government", ("anarchy", 50f)).Concat(P("future", ("monarchy", 50f))).ToList();
        Assert.AreEqual("directorate", WorldPulls.Lead(Government, pulls, 6f, 2f, null).outcome);
    }

    [Test]
    public void Lead_TheConfigMayHoldTwelveGovernments()
    {
        string[] twelve = Government.Outcomes.Concat(new[] { "a", "b", "c", "d" }).ToArray();
        var factor = new PullFactor("government", "directorate", twelve);
        Assert.AreEqual("d", WorldPulls.Lead(factor, P("government", ("d", 20f)), 6f, 2f, null).outcome);
    }

    [Test]
    public void Latch_ReportsOnlyChanges_DatedByTheNight_AndSavesEveryFactor()
    {
        var leads = new List<FactorLead>();
        var pulls = new List<OutcomePull>();

        CollectionAssert.IsEmpty(WorldPulls.Latch(new[] { Government, Future }, pulls, leads, 6f, 2f, 2), "as the run found it: nothing to print");
        Assert.AreEqual(2, leads.Count);
        Assert.IsTrue(leads.All(l => l.sinceDay == 0));

        WorldPulls.Add(pulls, "future", "cybernetic", 9f);
        List<FactorLead> changed = WorldPulls.Latch(new[] { Government, Future }, pulls, leads, 6f, 2f, 3);
        Assert.AreEqual(1, changed.Count);
        Assert.AreEqual(("future", "cybernetic", 3), (changed[0].factor, changed[0].outcome, changed[0].sinceDay));
        Assert.AreEqual("cybernetic", leads.Single(l => l.factor == "future").outcome);

        CollectionAssert.IsEmpty(WorldPulls.Latch(new[] { Government, Future }, pulls, leads, 6f, 2f, 4), "no change, no news");
        Assert.AreEqual(3, leads.Single(l => l.factor == "future").sinceDay);
    }

    [Test]
    public void Latch_ASplitInEitherOrder_IsTheSameAnswer()
    {
        var leads = new List<FactorLead> { Held("democracy", "theocracy") };
        List<OutcomePull> pulls = P("government", ("theocracy", 11f), ("democracy", 10f));
        CollectionAssert.IsEmpty(WorldPulls.Latch(new[] { Government }, pulls, leads, 6f, 2f, 5));
        Assert.AreEqual(3, leads[0].sinceDay);
    }

    [Test]
    public void FromScores_SeedsAnOlderSaveOnce_OnlyPastDayOne_ByMagnitude()
    {
        var seeds = new Dictionary<string, string> { ["science"] = "future", ["democracy"] = "government" };
        var deltas = new List<(string, string, float)>
        {
            ("japan_industrial", "science", 3f), ("japan_industrial", "democracy", -2f), ("japan_industrial", "art", 5f),
            ("greece_earlymodern", "science", 4f)
        };
        IEnumerable<OutcomeRef> LeaningsOf(string place) => place == "japan_industrial" ? MeijiNagoya : new List<OutcomeRef>();

        var pulls = new List<OutcomePull>();
        Assert.IsFalse(WorldPulls.FromScores(pulls, null, 1, deltas, seeds, LeaningsOf), "a day-1 save has nothing to seed");
        Assert.IsFalse(WorldPulls.FromScores(pulls, new[] { Held("directorate") }, 5, deltas, seeds, LeaningsOf), "a run of this build has latched leads: never seeded");
        Assert.IsTrue(WorldPulls.FromScores(pulls, new FactorLead[0], 5, deltas, seeds, LeaningsOf));
        Assert.AreEqual(3f, WorldPulls.Weight(pulls, "future", "cybernetic"));
        Assert.AreEqual(2f, WorldPulls.Weight(pulls, "government", "monarchy"), "a magnitude, never a sign");
        Assert.AreEqual(2, pulls.Count, "art seeds nothing; a place with no leaning adds nothing");

        Assert.IsFalse(WorldPulls.FromScores(pulls, null, 6, deltas, seeds, LeaningsOf), "a later load finds pulls and skips the seed");
        Assert.AreEqual(3f, WorldPulls.Weight(pulls, "future", "cybernetic"));
    }

    [Test]
    public void SeedFactors_AnAttributeSharedWithAnotherFactorOrNoFactor_SeedsNothing()
    {
        var moves = new List<(string, string)>
        {
            ("scientist", "science"), ("diplomat", "democracy"), ("soldier", "democracy"),
            ("artist", "art"), ("merchant", "art"), ("wanderer", "art")
        };
        Dictionary<string, string> seeds = WorldPulls.SeedFactors(moves, Roles);
        CollectionAssert.AreEquivalent(new[] { "science", "democracy" }, seeds.Keys);
        Assert.AreEqual("future", seeds["science"]);
        Assert.AreEqual("government", seeds["democracy"]);
    }

    [Test]
    public void Words_TheNameOrTheSplitLine()
    {
        string Name(string id) => id == "monarchy" ? "Monarchy" : id == "democracy" ? "Democracy" : null;
        Assert.AreEqual("Monarchy", WorldPulls.Words(Held("monarchy"), Name, "Split: {a} and {b}"));
        Assert.AreEqual("Split: Monarchy and Democracy", WorldPulls.Words(Held("monarchy", "democracy"), Name, "Split: {a} and {b}"));
        Assert.AreEqual("Monarchy / Democracy", WorldPulls.Words(Held("monarchy", "democracy"), Name, ""));
        Assert.AreEqual("fascism", WorldPulls.Words(Held("fascism"), Name, ""), "an unnamed outcome reads its id");
        Assert.IsNull(WorldPulls.Words(null, Name, ""));
    }
}
