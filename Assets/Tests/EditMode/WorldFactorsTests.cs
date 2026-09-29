using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

/// <summary>
/// The world's outcomes at the end of the demo (the endings spec E0-E2;
/// Saleh, 2026-09-29: "for now no ending, just end demo with those 4
/// outcomes"): each factor's question and its answer listed plainly, in
/// content order; the culture answered by the leading nation, the pull
/// factors by the run's pulls (one outcome, or a split as its own answer);
/// the content's checks, which refuse any world text that ranks or judges;
/// and today's world block.
/// </summary>
public class WorldFactorsTests
{
    private static WorldFactor F(string id, FactorAnswer answer, string foundAs) =>
        new WorldFactor { id = id, question = id + "?", answer = answer, foundAs = foundAs };

    private static WorldFactor P(string id, string statusQuo) =>
        new WorldFactor { id = id, question = id + "?", answer = FactorAnswer.Pulls, statusQuo = statusQuo, splitLine = "{a} and {b}", splitHeadline = "SPLIT: {a}, {b}" };

    private static WorldOutcome O(string factor, string id, string name) =>
        new WorldOutcome { factor = factor, id = id, name = name, headline = name + " takes over.", report = "2150 under " + name + "." };

    private static readonly List<WorldFactor> Four = new List<WorldFactor>
    {
        F("government", FactorAnswer.AsFound, "The Directorate"),
        F("future", FactorAnswer.AsFound, "The Credit Age"),
        F("money", FactorAnswer.AsFound, "The Debt"),
        F("culture", FactorAnswer.Leader, "None: the neutral Temporal Customs Zone"),
    };

    private static WorldContent Pulled() => new WorldContent
    {
        factors = new List<WorldFactor> { P("government", "directorate"), P("future", "credit_age"), F("culture", FactorAnswer.Leader, "None") },
        outcomes = new List<WorldOutcome>
        {
            O("government", "directorate", "The Directorate"), O("government", "monarchy", "Monarchy"), O("government", "democracy", "Democracy"),
            O("future", "credit_age", "The Credit Age"), O("future", "space_age", "The Space Age"),
        },
        roles = new List<WorldRole> { new WorldRole { archetype = "scientist", factor = "future", pull = 2f } }
    };

    private static FactorLead Lead(string factor, string outcome, string split = "") => new FactorLead { factor = factor, outcome = outcome, split = split };

    [Test]
    public void Lines_AnswerEachFactor_InContentOrder()
    {
        List<OutcomeLine> lines = WorldFactors.Lines(new WorldContent { factors = Four }, "Japan", null);

        CollectionAssert.AreEqual(new[] { "government", "future", "money", "culture" }, lines.Select(l => l.FactorId).ToArray());
        CollectionAssert.AreEqual(new[] { "government?", "future?", "money?", "culture?" }, lines.Select(l => l.Question).ToArray());
        CollectionAssert.AreEqual(new[] { "The Directorate", "The Credit Age", "The Debt", "Japan" }, lines.Select(l => l.Answer).ToArray());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void Lines_WithNoLeader_TheCultureReadsAsFound(string leader)
    {
        Assert.AreEqual("None: the neutral Temporal Customs Zone", WorldFactors.Lines(new WorldContent { factors = Four }, leader, null).Last().Answer);
    }

    [Test]
    public void Lines_SkipNullsAndBlankIds_AndReadNothingFromNothing()
    {
        var factors = new List<WorldFactor> { null, F(" ", FactorAnswer.AsFound, "x"), F("money", FactorAnswer.AsFound, "The Debt") };
        CollectionAssert.AreEqual(new[] { "money" }, WorldFactors.Lines(new WorldContent { factors = factors }, null, null).Select(l => l.FactorId).ToArray());
        CollectionAssert.IsEmpty(WorldFactors.Lines(null, "Japan", null));
    }

    [Test]
    public void Lines_APullFactor_ReadsItsAnswer_ASplitThroughItsSplitLine_ElseAsFound()
    {
        WorldContent world = Pulled();
        List<OutcomeLine> lines = WorldFactors.Lines(world, "Greece", new[] { Lead("government", "monarchy", "democracy"), Lead("future", "space_age") });
        CollectionAssert.AreEqual(new[] { "Monarchy and Democracy", "The Space Age", "Greece" }, lines.Select(l => l.Answer).ToArray());

        CollectionAssert.AreEqual(new[] { "The Directorate", "The Credit Age", "None" }, WorldFactors.Lines(world, null, null).Select(l => l.Answer).ToArray(),
                                  "no answer yet: as the run found it");
    }

    [Test]
    public void Lines_APullAnswerCarriesItsReport_ASplitBothHalves_TheCultureNone()
    {
        List<OutcomeLine> lines = WorldFactors.Lines(Pulled(), "Greece", new[] { Lead("government", "monarchy", "democracy"), Lead("future", "space_age") });
        Assert.AreEqual("2150 under Monarchy. 2150 under Democracy.", lines[0].Report);
        Assert.AreEqual("2150 under The Space Age.", lines[1].Report);
        Assert.IsNull(lines[2].Report);
        Assert.AreEqual("2150 under The Directorate.", WorldFactors.Lines(Pulled(), null, null)[0].Report, "as found");
    }

    [Test]
    public void AnswersNow_ReadThePulls_WithTheHeldLeads()
    {
        WorldContent world = Pulled();
        var pulls = new List<OutcomePull> { new OutcomePull { factor = "future", outcome = "space_age", amount = 9f } };
        List<FactorLead> now = world.AnswersNow(pulls, null, 6f, 2f);
        CollectionAssert.AreEqual(new[] { "directorate", "space_age" }, now.Select(l => l.outcome).ToArray());

        now = world.AnswersNow(pulls, new[] { Lead("future", "credit_age") }, 6f, 2f);
        Assert.AreEqual("space_age", now[1].outcome, "9 passes the held 6 by more than 2");
        now = world.AnswersNow(pulls, new[] { Lead("future", "credit_age") }, 6f, 4f);
        Assert.AreEqual("credit_age", now[1].outcome, "within the margin the held answer stays");
    }

    [Test]
    public void Headline_TheOutcomesLine_OrTheSplitHeadline()
    {
        WorldContent world = Pulled();
        Assert.AreEqual("Monarchy takes over.", world.Headline(Lead("government", "monarchy")));
        Assert.AreEqual("SPLIT: Monarchy, Democracy", world.Headline(Lead("government", "monarchy", "democracy")));
        Assert.IsNull(world.Headline(Lead("government", "")));
    }

    [Test]
    public void Problems_NoneForTheFour_NorForPulledContent()
    {
        CollectionAssert.IsEmpty(new WorldContent { factors = Four }.Problems());
        CollectionAssert.IsEmpty(Pulled().Problems());
    }

    [Test]
    public void Problems_EmptyBlankAndRepeated()
    {
        Assert.AreEqual(1, new WorldContent { factors = new List<WorldFactor>() }.Problems().Count, "no factor");
        List<string> problems = new WorldContent
        {
            factors = new List<WorldFactor>
            {
                new WorldFactor { id = "", question = "q", foundAs = "a" },
                new WorldFactor { id = "future", question = "", foundAs = "" },
                F("future", FactorAnswer.AsFound, "The Credit Age"),
            }
        }.Problems();
        Assert.AreEqual(4, problems.Count, string.Join(" | ", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("no id")));
        Assert.IsTrue(problems.Any(p => p.Contains("no question")));
        Assert.IsTrue(problems.Any(p => p.Contains("no foundAs")));
        Assert.IsTrue(problems.Any(p => p.Contains("listed twice")));
    }

    [Test]
    public void Problems_APullFactor_NeedsItsStatusQuo_TwoOutcomes_AndBothSplitLines()
    {
        WorldContent world = Pulled();
        world.factors[0].statusQuo = "nobody";
        world.factors[1].splitLine = "no tokens";
        world.outcomes.RemoveAll(o => o.id == "space_age");
        List<string> problems = world.Problems();
        Assert.IsTrue(problems.Any(p => p.Contains("statusQuo 'nobody'")), string.Join(" | ", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("'future'") && p.Contains("at least two")));
        Assert.IsTrue(problems.Any(p => p.Contains("'future' needs a splitLine")));
    }

    [Test]
    public void Problems_OutcomesAndRoles()
    {
        WorldContent world = Pulled();
        world.outcomes.Add(O("culture", "japan", "Japan"));
        world.outcomes.Add(O("government", "monarchy", "Monarchy again"));
        world.outcomes.Add(new WorldOutcome { factor = "government", id = "blank" });
        world.roles.Add(new WorldRole { archetype = "soldier", factor = "government", pull = 0f });
        world.roles.Add(new WorldRole { archetype = "", factor = "money", pull = 1f });
        List<string> problems = world.Problems();
        string all = string.Join(" | ", problems);
        Assert.IsTrue(problems.Any(p => p.Contains("culture/japan") && p.Contains("no factor answered by pulls")), all);
        Assert.IsTrue(problems.Any(p => p.Contains("government/monarchy") && p.Contains("listed twice")), all);
        Assert.IsTrue(problems.Any(p => p.Contains("government/blank") && p.Contains("no name")), all);
        Assert.IsTrue(problems.Any(p => p.Contains("government/blank") && p.Contains("no headline")), all);
        Assert.IsTrue(problems.Any(p => p.Contains("government/blank") && p.Contains("no report")), all);
        Assert.IsTrue(problems.Any(p => p.Contains("'soldier' has pull 0")), all);
        Assert.IsTrue(problems.Any(p => p.Contains("no archetype")), all);
        Assert.IsTrue(problems.Any(p => p.Contains("pulls 'money'")), all);
    }

    [Test]
    public void RefProblems_AFactorOfPulls_OneOfItsOutcomes_AndAPositiveAmount()
    {
        WorldContent world = Pulled();
        CollectionAssert.IsEmpty(world.RefProblems("x", "future", "space_age", 3f));
        CollectionAssert.IsEmpty(world.RefProblems("x", "future", "space_age", null));
        Assert.AreEqual(1, world.RefProblems("x", "culture", "japan", null).Count, "the culture is the leader's, not pulled");
        Assert.AreEqual(1, world.RefProblems("x", "future", "monarchy", null).Count);
        Assert.AreEqual(1, world.RefProblems("x", "future", "space_age", -1f).Count, "nothing pushes against an outcome");
    }

    [TestCase("The best of all worlds")]
    [TestCase("2150 WINS")]
    [TestCase("a bad ending")]
    [TestCase("It failed.")]
    [TestCase("You retire.")]
    [TestCase("Approval 80 %")]
    public void Problems_RefuseWorldTextThatRanksOrJudges(string text)
    {
        WorldContent world = Pulled();
        world.outcomes[1].report = text;
        Assert.IsTrue(world.Problems().Any(p => p.Contains("judges the world") || p.Contains("percentage")), text);
    }

    [TestCase("Everything in 2150 goes into common stores.")]
    [TestCase("Every citizen knows which window to stand at.")]
    [TestCase("The goods train leaves at nine.")]
    public void Problems_WholeWordsOnly(string text)
    {
        WorldContent world = Pulled();
        world.outcomes[1].report = text;
        CollectionAssert.IsEmpty(world.Problems());
    }

    /// <summary>world_source.json's "world" block.</summary>
    private static ContentNode WorldBlock([CallerFilePath] string here = "")
    {
        const string source = "Assets/Data/World/world_source.json";
        string path = File.Exists(source) ? source : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", source);
        return ContentJson.Parse(File.ReadAllText(path)).Get("world");
    }

    [Test]
    public void TodaysContent_TheFourFactors_ThreeByPulls_TheCultureByTheLeader()
    {
        List<ContentNode> factors = WorldBlock().Get("factors").Items.ToList();

        CollectionAssert.AreEqual(new[] { "government", "future", "money", "culture" }, factors.Select(f => f.Get("id").Text).ToArray());
        CollectionAssert.AreEqual(new[] { "Pulls", "Pulls", "Pulls", "Leader" }, factors.Select(f => f.Get("answer").Text).ToArray());
        CollectionAssert.AreEqual(new[] { "directorate", "credit_age", "debt" }, factors.Take(3).Select(f => f.Get("statusQuo").Text).ToArray(),
                                  "each factor starts as the run found it");
    }

    [Test]
    public void TodaysContent_TheOutcomeLists_EightGovernmentsSixFuturesFiveWaysToPay()
    {
        List<ContentNode> outcomes = WorldBlock().Get("outcomes").Items.ToList();
        string[] Of(string factor) => outcomes.Where(o => o.Get("factor").Text == factor).Select(o => o.Get("id").Text).ToArray();

        CollectionAssert.AreEqual(new[] { "directorate", "democracy", "monarchy", "fascism", "communism", "theocracy", "technocracy", "corporate_board" }, Of("government"));
        CollectionAssert.AreEqual(new[] { "credit_age", "nuclear", "cybernetic", "space_age", "naturalism", "anarchy" }, Of("future"));
        CollectionAssert.AreEqual(new[] { "debt", "jubilee", "company_towns", "commons", "banking_houses" }, Of("money"));
    }

    [Test]
    public void TodaysContent_NoWorldTextRanksOrJudges()
    {
        ContentNode w = WorldBlock();
        var problems = new List<string>();
        foreach (ContentNode f in w.Get("factors").Items)
            problems.AddRange(WorldFactors.JudgingProblems(f.Get("id").Text, Text(f, "question"), Text(f, "foundAs"), Text(f, "splitLine"), Text(f, "splitHeadline")));
        foreach (ContentNode o in w.Get("outcomes").Items)
            problems.AddRange(WorldFactors.JudgingProblems(o.Get("factor").Text + "/" + o.Get("id").Text, Text(o, "name"), Text(o, "headline"), Text(o, "report")));
        CollectionAssert.IsEmpty(problems);
    }

    private static string Text(ContentNode node, string key) => node.Get(key)?.Text;
}
