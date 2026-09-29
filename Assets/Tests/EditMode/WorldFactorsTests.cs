using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

/// <summary>
/// The world's outcomes at the end of the demo (the endings spec E0; Saleh,
/// 2026-09-29: "for now no ending, just end demo with those 4 outcomes"):
/// each factor's question and its answer listed plainly, in content order;
/// the culture answered by the leading nation, the rest as the run found
/// 2150; the content's checks; and today's world block.
/// </summary>
public class WorldFactorsTests
{
    private static WorldFactor F(string id, FactorAnswer answer, string foundAs) =>
        new WorldFactor { id = id, question = id + "?", answer = answer, foundAs = foundAs };

    private static readonly List<WorldFactor> Four = new List<WorldFactor>
    {
        F("government", FactorAnswer.AsFound, "The Directorate"),
        F("future", FactorAnswer.AsFound, "The Credit Age"),
        F("money", FactorAnswer.AsFound, "The Debt"),
        F("culture", FactorAnswer.Leader, "None: the neutral Temporal Customs Zone"),
    };

    [Test]
    public void Lines_AnswerEachFactor_InContentOrder()
    {
        List<OutcomeLine> lines = WorldFactors.Lines(Four, "Japan");

        CollectionAssert.AreEqual(new[] { "government", "future", "money", "culture" }, lines.Select(l => l.FactorId).ToArray());
        CollectionAssert.AreEqual(new[] { "government?", "future?", "money?", "culture?" }, lines.Select(l => l.Question).ToArray());
        CollectionAssert.AreEqual(new[] { "The Directorate", "The Credit Age", "The Debt", "Japan" }, lines.Select(l => l.Answer).ToArray());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void Lines_WithNoLeader_TheCultureReadsAsFound(string leader)
    {
        Assert.AreEqual("None: the neutral Temporal Customs Zone", WorldFactors.Lines(Four, leader).Last().Answer);
    }

    [Test]
    public void Lines_SkipNullsAndBlankIds_AndReadNothingFromNothing()
    {
        var factors = new List<WorldFactor> { null, F(" ", FactorAnswer.AsFound, "x"), F("money", FactorAnswer.AsFound, "The Debt") };
        CollectionAssert.AreEqual(new[] { "money" }, WorldFactors.Lines(factors, null).Select(l => l.FactorId).ToArray());
        CollectionAssert.IsEmpty(WorldFactors.Lines(null, "Japan"));
    }

    [Test]
    public void Problems_NoneForTheFour()
    {
        CollectionAssert.IsEmpty(new WorldContent { factors = Four }.Problems());
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

    /// <summary>Words that rank or judge a world; the outcomes' authored words use none of them.</summary>
    private static readonly string[] Judging =
        { "better", "worse", "best", "worst", "good", "bad", "triumph", "collapse", "golden", "success", "fail", "win", "lose", "score", "rank", "victory", "defeat", "retire" };

    /// <summary>world_source.json's "world" block.</summary>
    private static ContentNode WorldBlock([CallerFilePath] string here = "")
    {
        const string source = "Assets/Data/World/world_source.json";
        string path = File.Exists(source) ? source : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", source);
        return ContentJson.Parse(File.ReadAllText(path)).Get("world");
    }

    [Test]
    public void TodaysContent_TheFourFactors_AsTheRunFoundThem_NeverJudging()
    {
        List<ContentNode> factors = WorldBlock().Get("factors").Items.ToList();

        CollectionAssert.AreEqual(new[] { "government", "future", "money", "culture" }, factors.Select(f => f.Get("id").Text).ToArray());
        CollectionAssert.AreEqual(new[] { "AsFound", "AsFound", "AsFound", "Leader" }, factors.Select(f => f.Get("answer").Text).ToArray(),
                                  "only the culture moves today (the leader); E1 answers the others");
        foreach (ContentNode f in factors)
        {
            string text = (f.Get("question").Text + " " + f.Get("foundAs").Text).ToLowerInvariant();
            foreach (string word in Judging)
                Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(text, $@"\b{word}"), $"'{f.Get("id").Text}' judges the world ('{word}'): {text}");
        }
    }
}
