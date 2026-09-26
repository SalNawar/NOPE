using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The Temporal Times: today's front page is the morning paper (the lead is
/// the first news line, the rest under World, the notices boxed), with the
/// standings in ranking order and the back issues; a back issue shows its
/// own day; the archive lists issues newest first.
/// </summary>
public class NewsPagesTests
{
    private static SitePage Open(SiteWorld w, string address)
    {
        SitePage page = Sites.Page(w, address);
        Assert.IsTrue(page.Found, address);
        return page;
    }

    private static List<string> Texts(SitePage page, PageBlockKind kind) => page.Blocks.Where(b => b.Kind == kind).Select(b => b.Text).ToList();

    [Test]
    public void Front_IsTodaysPaper()
    {
        SiteWorld w = SiteFixture.World();
        SitePage page = Open(w, "chronet://times.tc");
        Assert.AreEqual("chronet://times.tc/day-3", page.Address, "the front page's address is today's issue");
        Assert.AreEqual("news", page.SiteId);
        Assert.AreEqual("site.news.masthead(The Temporal Times|3)", page.Blocks[0].Text);
        CollectionAssert.AreEqual(new[] { "HISTORY: Florentine printers set type the Chinese way." }, Texts(page, PageBlockKind.Headline), "the lead is the first news line");
        CollectionAssert.AreEqual(new[] { "site.news.world" }, Texts(page, PageBlockKind.Heading));
        CollectionAssert.AreEqual(new[] { "Robots is now DOMINANT in Showa Tokyo." }, Texts(page, PageBlockKind.Paragraph), "the other news goes under World");

        PageBlock notices = page.Blocks.Single(b => b.Kind == PageBlockKind.Box && b.Text == "site.news.notices");
        CollectionAssert.AreEqual(new[] { "site.news.noNotices" }, notices.Lines, "no briefing lines today");
        Assert.AreEqual(page.Address, Open(w, "chronet://times.tc/day-3").Address);
    }

    [Test]
    public void Front_StandingsFollowTheRanking()
    {
        SiteWorld w = SiteFixture.World();
        PageBlock standings = Open(w, "chronet://times.tc").Blocks.Single(b => b.Text == "site.news.standings");
        CollectionAssert.AreEqual(new[] { "site.news.standing(1|China)", "site.news.standing(2|Egypt)", "site.news.standing(3|Italy)" }, standings.Lines);

        w.Ranking = new List<RankedScore>();
        standings = Open(w, "chronet://times.tc").Blocks.Single(b => b.Text == "site.news.standings");
        CollectionAssert.AreEqual(new[] { "site.news.noStandings" }, standings.Lines);
    }

    [Test]
    public void Front_LinksTheBackIssues_NewestFirst()
    {
        PageBlock archive = Open(SiteFixture.World(), "chronet://times.tc").Blocks.Single(b => b.Text == "site.news.archive");
        CollectionAssert.AreEqual(new[] { "site.news.issueLink(2)", "site.news.issueLink(1)", "site.news.allIssues" }, archive.Links.Select(l => l.Text));
        CollectionAssert.AreEqual(new[] { "chronet://times.tc/day-2", "chronet://times.tc/day-1", "chronet://times.tc/archive" }, archive.Links.Select(l => l.Address));
    }

    [Test]
    public void Front_AQuietDay_HasAQuietHeadline()
    {
        SiteWorld w = SiteFixture.World(4);
        SitePage page = Open(w, "chronet://times.tc");
        CollectionAssert.AreEqual(new[] { "site.news.quiet" }, Texts(page, PageBlockKind.Headline), "no issue on file for day 4");
        CollectionAssert.IsEmpty(Texts(page, PageBlockKind.Heading), "no World section without news");
    }

    [Test]
    public void BackIssue_ShowsItsOwnDay()
    {
        SiteWorld w = SiteFixture.World();
        SitePage day2 = Open(w, "chronet://times.tc/day-2");
        Assert.AreEqual("chronet://times.tc/day-2", day2.Address);
        Assert.AreEqual("site.news.masthead(The Temporal Times|2)", day2.Blocks[0].Text);
        CollectionAssert.Contains(Texts(day2, PageBlockKind.Note), "site.news.backIssue(2)");
        CollectionAssert.AreEqual(new[] { "HISTORY: China now dominates the timeline." }, Texts(day2, PageBlockKind.Headline));
        CollectionAssert.AreEqual(new[] { "Desk officers may now ask about the capital." }, day2.Blocks.Single(b => b.Kind == PageBlockKind.Box).Lines);
        Assert.IsFalse(day2.Blocks.Any(b => b.Text == "site.news.standings"), "the standings are today's, not the issue's");
        CollectionAssert.AreEqual(new[] { "site.news.quiet" }, Texts(Open(w, "chronet://times.tc/day-1"), PageBlockKind.Headline), "day 1 had no news");

        Assert.IsFalse(Sites.Page(w, "chronet://times.tc/day-7").Found, "no issue on file");
        Assert.IsFalse(Sites.Page(w, "chronet://times.tc/day-x").Found);
    }

    [Test]
    public void Archive_ListsEarlierIssues_NewestFirst()
    {
        SiteWorld w = SiteFixture.World();
        SitePage page = Open(w, "chronet://times.tc/archive");
        List<PageBlock> links = page.Blocks.Where(b => b.Kind == PageBlockKind.Link).ToList();
        CollectionAssert.AreEqual(new[] { "site.news.archiveRow(2|HISTORY: China now dominates the timeline.)", "site.news.archiveRow(1|site.news.quiet)", "site.news.today" },
                                  links.Select(l => l.Text));
        CollectionAssert.AreEqual(new[] { "chronet://times.tc/day-2", "chronet://times.tc/day-1", "chronet://times.tc" }, links.Select(l => l.Address));

        w.NewsArchive = new List<NewsIssue>();
        CollectionAssert.Contains(Texts(Open(w, "chronet://times.tc/archive"), PageBlockKind.Paragraph), "site.news.noBackIssues");
    }
}
