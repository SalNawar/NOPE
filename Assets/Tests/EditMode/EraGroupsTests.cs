using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Track E2: an era may be a second moment of a main era (Hellenistic Pella
/// beside Periclean Athens): the group rules, today's world bringing an
/// authored case's place, Chronopedia's index and the Lineage Archive's
/// era filter reading the group.
/// </summary>
public class EraGroupsTests
{
    private static PlaceInfo Pella() =>
        new PlaceInfo("greece_ancient2", "greece", "Greece", "ancient2", "Ancient", 0, false, "Hellenistic Pella", -336, "Pella under Philip and Alexander.",
                      new[] { (ClueCategory.Geography, "Pella"), (ClueCategory.Currency, "Macedonian stater") }, "ancient");

    [Test]
    public void GroupOf_TheGroup_OrTheEraItself()
    {
        Assert.AreEqual("ancient", EraGroups.GroupOf("ancient2", "ancient"));
        Assert.AreEqual("ancient", EraGroups.GroupOf("ancient", null));
        Assert.AreEqual("ancient", EraGroups.GroupOf("ancient", " "));
    }

    [Test]
    public void Problems_AGroupIsAnotherPastMainEra_TheFutureHasNone()
    {
        var eras = new List<EraEntry>
        {
            new EraEntry("ancient", null, false), new EraEntry("ancient2", "ancient", false), new EraEntry("future", null, true),
        };
        CollectionAssert.IsEmpty(EraGroups.Problems(eras));

        List<string> bad = EraGroups.Problems(new List<EraEntry>
        {
            new EraEntry("ancient", null, false), new EraEntry("ancient2", "ancient", false), new EraEntry("future", null, true),
            new EraEntry("a", "a", false), new EraEntry("b", "atlantis", false), new EraEntry("c", "future", false),
            new EraEntry("d", "ancient2", false), new EraEntry("future2", "ancient", true),
        });
        Assert.AreEqual(5, bad.Count, string.Join("\n", bad));
        Assert.IsTrue(bad.Any(p => p.Contains("'a'") && p.Contains("itself")));
        Assert.IsTrue(bad.Any(p => p.Contains("'b'") && p.Contains("not an era")));
        Assert.IsTrue(bad.Any(p => p.Contains("'c'") && p.Contains("Future")));
        Assert.IsTrue(bad.Any(p => p.Contains("'d'") && p.Contains("second moment")));
        Assert.IsTrue(bad.Any(p => p.Contains("'future2'")));
        CollectionAssert.IsEmpty(EraGroups.Problems(null));
    }

    [Test]
    public void InTodaysWorld_AWeightedPlace_OrAnAuthoredCasesPlace_NeverTheFuture()
    {
        Assert.IsTrue(EraGroups.InTodaysWorld(false, true, true, false));
        Assert.IsFalse(EraGroups.InTodaysWorld(false, true, false, false), "a nation the day leaves out");
        Assert.IsFalse(EraGroups.InTodaysWorld(false, false, true, false), "an era the day does not weight");
        Assert.IsTrue(EraGroups.InTodaysWorld(false, false, false, true), "a premade's place comes with the premade");
        Assert.IsFalse(EraGroups.InTodaysWorld(true, true, true, true), "the Future is the present");
    }

    [Test]
    public void Chronopedia_ASecondMoment_SharesItsEraColumn_OnASecondRow()
    {
        SiteWorld w = SiteFixture.World();
        w.Places = SiteFixture.Places().Concat(new[] { Pella() }).ToList();
        PageBlock table = Sites.Page(w, "chronet://chronopedia").Blocks.Single(b => b.Kind == PageBlockKind.Table);
        CollectionAssert.AreEqual(new[] { "site.history.country", "Ancient", "Medieval", "Future" }, table.Columns, "one column per era group");
        List<string[]> rows = table.Rows.Select(r => r.Select(c => c.Text).ToArray()).ToList();
        int greece = rows.FindIndex(r => r[0] == "Greece");
        CollectionAssert.AreEqual(new[] { "Greece", "Periclean Athens", "site.history.none", "site.history.none" }, rows[greece]);
        CollectionAssert.AreEqual(new[] { "", "Hellenistic Pella", "", "" }, rows[greece + 1], "the second moment's row");
        Assert.AreEqual("chronet://chronopedia/greece/ancient2", table.Rows[greece + 1][1].Address);
        Assert.IsTrue(Sites.Page(w, "chronet://chronopedia/greece/ancient2").Found, "it has its own article");
    }

    [Test]
    public void LineageArchive_AnErasChipFindsItsSecondMoments()
    {
        SiteWorld w = SiteFixture.World();
        w.Places = SiteFixture.Places().Concat(new[] { Pella() }).ToList();
        var cards = new List<PersonCard> { new PersonCard { Id = "aristotle", Name = "Aristotle", PlaceId = "greece_ancient2" } };
        CollectionAssert.AreEqual(new[] { "Aristotle" }, AncestryPages.Search(cards, null, "greece", "ancient", w.PlaceById).Select(c => c.Name));
        CollectionAssert.AreEqual(new[] { "Aristotle" }, AncestryPages.Search(cards, null, null, "ancient2", w.PlaceById).Select(c => c.Name));
        SitePage page = Sites.Page(w, "chronet://lineage");
        PageBlock eras = page.Blocks.Single(b => b.Kind == PageBlockKind.Chips && b.Text == "site.lineage.era");
        Assert.AreEqual(1, eras.Links.Count(l => l.Text == "Ancient"), "one chip per era group");
    }
}
