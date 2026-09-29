using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

/// <summary>
/// The neutral world summary of the run's last day (2026-09-29, Saleh: "we
/// dont make judgements"): every history factor described as it stands, in
/// content order, never ranked; the place facts history rewrote and the
/// carries still on their way; and the words it writes with (WordKeys).
/// </summary>
public class WorldSummaryTests
{
    /// <summary>Words that record every key asked for.</summary>
    private sealed class RecordingWords : IPageWords
    {
        public readonly HashSet<string> Keys = new HashSet<string>();

        public string Get(string key, params object[] args)
        {
            Keys.Add(key);
            return key;
        }
    }

    /// <summary>The fixture's China-led world with a pending carry, two attributes, three dominant pairs and two events.</summary>
    private static WorldOutcome Outcome()
    {
        SiteWorld site = SiteFixture.World();
        site.History.leaderSinceDay = 2;
        site.History.pendingCarries.Add(new CarryRecord { fromNationId = "greece", fromEraId = "ancient", toNationId = "egypt", toEraId = "medieval", category = ClueCategory.Technology, value = "Klepsydra", day = 15 });
        return new WorldOutcome
        {
            Places = site.Places,
            History = site.History,
            NationName = site.NationName,
            Present = new PresentPlace("china", "future", "Shanghai Megacity (2150)", 2150, 2100, 2130, new[]
            {
                new KeyValuePair<ClueCategory, string>(ClueCategory.Currency, "Digital yuan"),
                new KeyValuePair<ClueCategory, string>(ClueCategory.Politics, "The Assembly")
            }),
            Attributes = new[] { new KeyValuePair<string, float>("Democracy", 12f), new KeyValuePair<string, float>("Art", 30.5f) },
            Dominant = new[]
            {
                new KeyValuePair<string, string>("Democracy", "Periclean Athens"),
                new KeyValuePair<string, string>("Art", "New Kingdom Egypt"),
                new KeyValuePair<string, string>("Art", "Florentine Republic")
            },
            Events = new[] { "Movable type reaches Florence", "Audit week" }
        };
    }

    [Test]
    public void Changes_EveryRevisedPlaceFact_InPlaceOrder_WithItsAuthoredValueAndDay()
    {
        SiteWorld site = SiteFixture.World();
        List<PlaceChange> changes = WorldSummary.Changes(site.Places, site.History);

        Assert.AreEqual(2, changes.Count);
        Assert.AreEqual("Periclean Athens (Ancient)", changes[0].Place, "Greece comes before Italy in the content");
        Assert.AreEqual(ClueCategory.Technology, changes[0].Category);
        Assert.AreEqual("Papyrus", changes[0].Value);
        Assert.AreEqual("Klepsydra", changes[0].Was);
        Assert.AreEqual(3, changes[0].Day);
        Assert.AreEqual("Florentine Republic (Medieval)", changes[1].Place);
        Assert.AreEqual("Chinese movable type press", changes[1].Value);
        Assert.AreEqual("Printing press", changes[1].Was);
        Assert.AreEqual(2, changes[1].Day);
    }

    [Test]
    public void Changes_AnEditBackToTheAuthoredValue_IsNoChange()
    {
        SiteWorld site = SiteFixture.World();
        site.History.factEdits.Add(new FactEdit("italy", "medieval", ClueCategory.Technology, "Printing press", 4, EditCause.Carry, "x"));
        Assert.IsFalse(WorldSummary.Changes(site.Places, site.History).Any(c => c.Place.StartsWith("Florentine")));
        CollectionAssert.IsEmpty(WorldSummary.Changes(site.Places, null));
        CollectionAssert.IsEmpty(WorldSummary.Changes(null, site.History));
    }

    [Test]
    public void Pending_TheCarriesStillOnTheirWay_WithThePlacesValueNow()
    {
        WorldOutcome o = Outcome();
        List<PlaceChange> pending = WorldSummary.Pending(o.Places, o.History);

        Assert.AreEqual(1, pending.Count);
        Assert.AreEqual("Mamluk Egypt (Medieval)", pending[0].Place);
        Assert.AreEqual("Klepsydra", pending[0].Value);
        Assert.AreEqual("Nilometer", pending[0].Was);
        Assert.AreEqual(15, pending[0].Day);
    }

    [Test]
    public void Sections_DescribeEveryFactor_InContentOrder_NeverRanked()
    {
        List<SummarySection> sections = WorldSummary.Sections(Outcome(), new SiteFixture.EchoWords());

        CollectionAssert.AreEqual(new[]
        {
            "ending.world.present", "ending.world.influenceHeading", "ending.world.attributesHeading",
            "ending.world.dominantHeading", "ending.world.changesHeading", "ending.world.eventsHeading"
        }, sections.Select(s => s.Heading).ToArray());

        CollectionAssert.AreEqual(new[]
        {
            "ending.world.leader(China|2)", "ending.world.presentPlace(Shanghai Megacity (2150))",
            "ending.world.fact(category.Currency|Digital yuan)", "ending.world.fact(category.Politics|The Assembly)"
        }, sections[0].Lines);
        CollectionAssert.AreEqual(new[] { "ending.world.influence(Egypt|4)", "ending.world.influence(Italy|1)", "ending.world.influence(China|9)" },
                                  sections[1].Lines, "the countries in content order, not by influence");
        CollectionAssert.AreEqual(new[] { "ending.world.attribute(Democracy|12)", "ending.world.attribute(Art|30.5)" }, sections[2].Lines);
        CollectionAssert.AreEqual(new[] { "ending.world.dominant(Democracy|Periclean Athens)", "ending.world.dominant(Art|New Kingdom Egypt, Florentine Republic)" },
                                  sections[3].Lines);
        CollectionAssert.AreEqual(new[]
        {
            "ending.world.change(Periclean Athens (Ancient)|category.Technology|Papyrus|Klepsydra|3)",
            "ending.world.change(Florentine Republic (Medieval)|category.Technology|Chinese movable type press|Printing press|2)",
            "ending.world.pending(Mamluk Egypt (Medieval)|category.Technology|Klepsydra|Nilometer|15)"
        }, sections[4].Lines);
        CollectionAssert.AreEqual(new[] { "Movable type reaches Florence", "Audit week" }, sections[5].Lines);
    }

    [Test]
    public void Sections_AnUntouchedWorld_SaysSoInEachSection()
    {
        List<SummarySection> sections = WorldSummary.Sections(new WorldOutcome { History = new HistoryState() }, new SiteFixture.EchoWords());

        CollectionAssert.AreEqual(new[]
        {
            "ending.world.noLeader", "ending.world.noInfluence", "ending.world.noAttributes",
            "ending.world.noDominant", "ending.world.noChanges", "ending.world.noEvents"
        }, sections.Select(s => string.Join("/", s.Lines)).ToArray());
        CollectionAssert.IsEmpty(WorldSummary.Sections(null, new SiteFixture.EchoWords()));
    }

    [Test]
    public void WordKeys_AreExactlyTheKeysTheSummaryWritesWith()
    {
        var words = new RecordingWords();
        WorldSummary.Sections(Outcome(), words);
        WorldSummary.Sections(new WorldOutcome { History = new HistoryState() }, words);

        List<string> used = words.Keys.Where(k => !k.StartsWith("category.")).OrderBy(k => k).ToList();
        List<string> unused = WorldSummary.WordKeys.Except(used).ToList(), unlisted = used.Except(WorldSummary.WordKeys).ToList();
        Assert.IsEmpty(unused, "listed but never written with: " + string.Join(", ", unused));
        Assert.IsEmpty(unlisted, "written with but not listed: " + string.Join(", ", unlisted));
        Assert.AreEqual(WorldSummary.WordKeys.Length, WorldSummary.WordKeys.Distinct().Count(), "no key listed twice");
    }

    /// <summary>Words that rank or judge a world; the summary's authored words use none of them.</summary>
    private static readonly string[] Judging =
        { "better", "worse", "best", "worst", "good", "bad", "triumph", "collapse", "golden", "success", "fail", "win", "lose", "score", "rank", "victory", "defeat" };

    /// <summary>The English UI strings of world_source.json by key.</summary>
    private static Dictionary<string, string> UiStrings([CallerFilePath] string here = "")
    {
        const string source = "Assets/Data/World/world_source.json";
        string path = File.Exists(source) ? source : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", source);
        return ContentJson.Parse(File.ReadAllText(path)).Get("ui").Get("strings").Items.ToDictionary(s => s.Get("key").Text, s => s.Get("text").Text);
    }

    [Test]
    public void TheAuthoredWords_ExistAndNeverJudge()
    {
        Dictionary<string, string> strings = UiStrings();
        foreach (string key in WorldSummary.WordKeys)
        {
            Assert.IsTrue(strings.TryGetValue(key, out string text) && !string.IsNullOrWhiteSpace(text), $"ui.strings has no '{key}'");
            string lower = text.ToLowerInvariant();
            foreach (string word in Judging)
                Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(lower, $@"\b{word}"), $"'{key}' judges the world ('{word}'): {text}");
        }
    }
}
