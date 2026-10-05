using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Chronopedia: an article's facts are History.Resolve's, a revised one says
/// since which day and what it was; the index covers every place in the
/// world (only the leader's Future place); the present follows the leader;
/// Revisions lists every edit and pending carry, newest first.
/// </summary>
public class HistoryPagesTests
{
    private static SitePage Open(SiteWorld w, string address)
    {
        SitePage page = Sites.Page(w, address);
        Assert.IsTrue(page.Found, address);
        return page;
    }

    private static List<PageField> Infobox(SitePage page) => page.Blocks.Single(b => b.Kind == PageBlockKind.Fields).Fields;

    [Test]
    public void Article_FactsAsHistoryStands_RevisedOnesSayWhenAndWhat()
    {
        SiteWorld w = SiteFixture.World();
        SitePage page = Open(w, "chronet://chronopedia/italy/medieval");
        Assert.AreEqual("chronet://chronopedia/italy/medieval", page.Address);
        Assert.AreEqual("Florentine Republic", page.Title);
        Assert.AreEqual("Florentine Republic", page.Blocks.Single(b => b.Kind == PageBlockKind.Headline).Text);
        CollectionAssert.Contains(page.Blocks.Where(b => b.Kind == PageBlockKind.Note).Select(b => b.Text).ToList(), "site.history.subtitle(Italy|Medieval|1400)");
        Assert.AreEqual("Florence of the guilds.", page.Blocks.Single(b => b.Kind == PageBlockKind.Paragraph).Text, "the moment");

        List<PageField> box = Infobox(page);
        CollectionAssert.AreEqual(new[] { "category.Geography", "category.Politics", "category.Currency", "category.Language", "category.Technology", "category.Culture" },
                                  box.Select(f => f.Label), "capital, ruler, currency, language, technology, dress");
        PageField tech = box.Single(f => f.Label == "category.Technology");
        Assert.AreEqual(History.Resolve(w.History, "italy", "medieval", ClueCategory.Technology, "Printing press"), tech.Value);
        Assert.AreEqual("Chinese movable type press", tech.Value);
        Assert.AreEqual("site.history.revised(2|Printing press)", tech.Note);
        Assert.IsTrue(box.Where(f => f != tech).All(f => f.Note == null), "only the revised value has a note");
        Assert.IsTrue(box.All(f => string.IsNullOrEmpty(f.Address)), "values are not links (not compare-pickable either)");
        Assert.AreEqual("Florence", box[0].Value);
    }

    [Test]
    public void Article_AnEditBackToTheAuthoredValue_IsNotRevised()
    {
        SiteWorld w = SiteFixture.World();
        w.History.factEdits.Add(new FactEdit("italy", "medieval", ClueCategory.Technology, " printing press ", 4, EditCause.Carry, "x"));
        PageField tech = Infobox(Open(w, "chronet://chronopedia/italy/medieval")).Single(f => f.Label == "category.Technology");
        Assert.AreEqual(" printing press ", tech.Value);
        Assert.IsNull(tech.Note);
    }

    [Test]
    public void Article_LinksThePlacesPeople_TheRevisionsAndTheIndex()
    {
        SiteWorld w = SiteFixture.World();
        List<string> links = SiteFixture.Links(Open(w, "chronet://chronopedia/egypt/ancient")).ToList();
        CollectionAssert.AreEqual(new[] { "chronet://lineage/search?country=egypt&era=ancient", "chronet://chronopedia/revisions", "chronet://chronopedia" }, links);

        w.Sites = new List<SiteSpec> { SiteFixture.Chronopedia };
        CollectionAssert.AreEqual(new[] { "chronet://chronopedia/revisions", "chronet://chronopedia" }, SiteFixture.Links(Open(w, "chronet://chronopedia/egypt/ancient")).ToList(),
                                  "no link to an unlisted Lineage Archive");
    }

    [Test]
    public void Index_EveryPlaceInTheWorld_OnlyTheLeadersFuture()
    {
        SiteWorld w = SiteFixture.World();
        SitePage page = Open(w, "chronet://chronopedia");
        PageBlock table = page.Blocks.Single(b => b.Kind == PageBlockKind.Table);
        CollectionAssert.AreEqual(new[] { "site.history.country", "Ancient", "Medieval", "Future" }, table.Columns);
        CollectionAssert.AreEqual(new[] { "Egypt", "Greece", "Italy", "China" }, table.Rows.Select(r => r[0].Text));

        List<PageCell> egypt = table.Rows[0];
        Assert.AreEqual(("New Kingdom Egypt", "chronet://chronopedia/egypt/ancient"), (egypt[1].Text, egypt[1].Address));
        Assert.AreEqual(("Mamluk Egypt", "chronet://chronopedia/egypt/medieval"), (egypt[2].Text, egypt[2].Address));
        Assert.AreEqual(("site.history.none", (string)null), (egypt[3].Text, egypt[3].Address), "Egypt does not lead: its Future is not in the world");
        Assert.AreEqual(("site.history.none", (string)null), (table.Rows[1][2].Text, table.Rows[1][2].Address), "no Medieval Greece in the content");
        Assert.AreEqual("chronet://chronopedia/china/future", table.Rows[3][3].Address);

        Assert.IsFalse(Sites.Page(w, "chronet://chronopedia/egypt/future").Found, "a Future place not in the world has no article");
        Assert.IsFalse(Sites.Page(w, "chronet://chronopedia/atlantis/ancient").Found);
        Assert.IsFalse(Sites.Page(w, "chronet://chronopedia/egypt").Found);
    }

    [Test]
    public void Present_FollowsTheLeader()
    {
        SiteWorld w = SiteFixture.World();
        SitePage present = Open(w, "chronet://chronopedia/present");
        Assert.AreEqual("chronet://chronopedia/present", present.Address);
        Assert.AreEqual("Shanghai Megacity", present.Title, "China leads: the present is its Future place");
        CollectionAssert.Contains(present.Blocks.Select(b => b.Text).ToList(), "site.history.presentNote");
        Assert.AreEqual("Maglev", Infobox(present).Single(f => f.Label == "category.Technology").Value);

        w.History.leaderId = "";
        present = Open(w, "chronet://chronopedia/present");
        Assert.AreEqual("site.history.present", present.Title);
        CollectionAssert.Contains(present.Blocks.Select(b => b.Text).ToList(), "site.history.presentNone");
        Assert.IsFalse(Sites.Page(w, "chronet://chronopedia/china/future").Found, "no leader: no Future place in the world");

        w.History = null;
        Assert.AreEqual("site.history.present", Open(w, "chronet://chronopedia/present").Title, "no history yet");
    }

    [Test]
    public void Present_2150Today_ABoxPerFactor_InWordsOnly_WithOrWithoutALeader()
    {
        SiteWorld w = SiteFixture.World();
        w.WorldToday = new List<OutcomeLine>
        {
            new OutcomeLine("government", "Who runs 2150?", "Monarchy", "A monarch reigns."),
            new OutcomeLine("culture", "Whose culture leads?", "China"),
        };

        foreach (string leader in new[] { "china", "" })
        {
            w.History.leaderId = leader;
            SitePage present = Open(w, "chronet://chronopedia/present");
            Assert.AreEqual("site.history.today", present.Blocks.Single(b => b.Kind == PageBlockKind.Heading && b.Text == "site.history.today").Text, leader);
            List<PageBlock> boxes = present.Blocks.Where(b => b.Kind == PageBlockKind.Box && b.Text != "site.history.changed").ToList();
            CollectionAssert.AreEqual(new[] { "Who runs 2150?", "Whose culture leads?" }, boxes.Select(b => b.Text).ToArray());
            CollectionAssert.AreEqual(new[] { "Monarchy", "A monarch reigns." }, boxes[0].Lines);
            CollectionAssert.AreEqual(new[] { "China" }, boxes[1].Lines, "no report: the answer alone");
        }

        w.WorldToday = new List<OutcomeLine>();
        Assert.IsFalse(Open(w, "chronet://chronopedia/present").Blocks.Any(b => b.Text == "site.history.today"), "no world factors: no section");
    }

    [Test]
    public void Present_2150Today_AnAnswerNamesTheTravellerItTracesTo()
    {
        SiteWorld w = SiteFixture.World();
        w.WorldToday = new List<OutcomeLine>
        {
            new OutcomeLine("government", "Who runs 2150?", "Monarchy", "A monarch reigns."),
            new OutcomeLine("money", "How does 2150 pay its way?", "The Debt"),
        };
        w.WorldTraces = new Dictionary<string, string> { { "government", "Traced to Desk 3: Cleopatra, day 6." } };
        List<PageBlock> boxes = Open(w, "chronet://chronopedia/present").Blocks.Where(b => b.Kind == PageBlockKind.Box && b.Text != "site.history.changed").ToList();
        CollectionAssert.AreEqual(new[] { "Monarchy", "A monarch reigns.", "Traced to Desk 3: Cleopatra, day 6." }, boxes[0].Lines);
        CollectionAssert.AreEqual(new[] { "The Debt" }, boxes[1].Lines, "untraced: no line");
    }

    [Test]
    public void Present_HistorysLatestChanges_NewestFirst_ThenTheLinkToRevisions()
    {
        SiteWorld w = SiteFixture.World();
        w.History.factEdits.Add(new FactEdit("egypt", "ancient", ClueCategory.Geography, "Thebes, the Garden City", 7, EditCause.Rule, History.TriggerSourcePrefix + "Hatshepsut's garden city"));
        SitePage present = Open(w, "chronet://chronopedia/present");
        PageBlock box = present.Blocks.Single(b => b.Kind == PageBlockKind.Box && b.Text == "site.history.changed");
        CollectionAssert.AreEqual(new[]
        {
            "site.history.changedRow(7|New Kingdom Egypt (Ancient)|category.Geography|Thebes, the Garden City|Hatshepsut's garden city)",
            "site.history.changedRow(3|Periclean Athens (Ancient)|category.Technology|Papyrus|site.history.whyCarry(New Kingdom Egypt (Ancient)))",
            "site.history.changedRow(2|Florentine Republic (Medieval)|category.Technology|Chinese movable type press|Movable type reaches Florence)"
        }, box.Lines, "newest first; a rule's name without the trigger prefix, a carry's home");
        int at = present.Blocks.IndexOf(box);
        Assert.AreEqual("chronet://chronopedia/revisions", present.Blocks[at + 1].Address, "then the link to every revision");

        for (int day = 8; day < 8 + HistoryPages.PresentChanges; day++)
            w.History.factEdits.Add(new FactEdit("egypt", "ancient", ClueCategory.Currency, "Coin " + day, day, EditCause.Rule, "r"));
        box = Open(w, "chronet://chronopedia/present").Blocks.Single(b => b.Kind == PageBlockKind.Box && b.Text == "site.history.changed");
        Assert.AreEqual(HistoryPages.PresentChanges, box.Lines.Count, "the newest few; Revisions lists them all");

        w.History = new HistoryState();
        Assert.IsFalse(Open(w, "chronet://chronopedia/present").Blocks.Any(b => b.Text == "site.history.changed"), "history unchanged: no box");
    }

    [Test]
    public void Revisions_EveryEditAndPendingCarry_NewestFirst()
    {
        SiteWorld w = SiteFixture.World();
        w.History.factEdits.Add(new FactEdit("italy", "medieval", ClueCategory.Technology, "Papyrus", 3, EditCause.Carry, "New Kingdom Egypt (Ancient)"));
        w.History.pendingCarries.Add(new CarryRecord
        {
            fromNationId = "greece", fromEraId = "ancient", toNationId = "egypt", toEraId = "ancient",
            category = ClueCategory.Technology, value = "Klepsydra", day = 3
        });

        SitePage page = Open(w, "chronet://chronopedia/revisions");
        PageBlock table = page.Blocks.Single(b => b.Kind == PageBlockKind.Table);
        CollectionAssert.AreEqual(new[] { "site.history.col.day", "site.history.col.place", "site.history.col.fact", "site.history.col.before", "site.history.col.after", "site.history.col.why" },
                                  table.Columns);
        List<string[]> rows = table.Rows.Select(r => r.Select(c => c.Text).ToArray()).ToList();
        CollectionAssert.AreEqual(new[] { "3", "New Kingdom Egypt (Ancient)", "category.Technology", "Papyrus", "Klepsydra", "site.history.whyPending(Periclean Athens (Ancient))" }, rows[0],
                                  "the pending carry, recorded last");
        CollectionAssert.AreEqual(new[] { "3", "Florentine Republic (Medieval)", "category.Technology", "Chinese movable type press", "Papyrus", "site.history.whyCarry(New Kingdom Egypt (Ancient))" }, rows[1],
                                  "a later edit's before is the earlier edit's value");
        CollectionAssert.AreEqual(new[] { "3", "Periclean Athens (Ancient)", "category.Technology", "Klepsydra", "Papyrus", "site.history.whyCarry(New Kingdom Egypt (Ancient))" }, rows[2]);
        CollectionAssert.AreEqual(new[] { "2", "Florentine Republic (Medieval)", "category.Technology", "Printing press", "Chinese movable type press", "site.history.whyRule(Trigger: Movable type reaches Florence)" }, rows[3],
                                  "the first edit's before is the authored value");
        Assert.AreEqual("chronet://chronopedia/italy/medieval", table.Rows[3][1].Address, "the place links to its article");

        w.History = new HistoryState();
        CollectionAssert.Contains(Open(w, "chronet://chronopedia/revisions").Blocks.Select(b => b.Text).ToList(), "site.history.noRevisions");
    }
}
