using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The in-game Internet's sites: listed from their first day (no gate),
/// addresses taken apart and written, what a typed name goes to, the start
/// page, the missing page, authored (Static) pages, the pc block's content
/// rules, and that every link reachable from the start page opens a page.
/// </summary>
public class SitesTests
{
    [Test]
    public void Listed_FromItsFirstDay_NotBefore()
    {
        var s = new SiteSpec { id = "x", fromDay = 3 };
        Assert.IsFalse(Sites.Listed(s, 2));
        Assert.IsTrue(Sites.Listed(s, 3));
        Assert.IsTrue(Sites.Listed(s, 9));
        Assert.IsTrue(Sites.Listed(new SiteSpec { fromDay = 0 }, 1), "a first day below 1 counts as 1");
        Assert.IsFalse(Sites.Listed(null, 5));

        var later = new SiteSpec { id = "later", fromDay = 4 };
        CollectionAssert.AreEqual(new[] { "news", "history" },
                                  Sites.ListedOn(new[] { SiteFixture.News, later, null, SiteFixture.Chronopedia }, 3).Select(x => x.id));
    }

    [Test]
    public void Address_WrittenAndTakenApart()
    {
        Assert.AreEqual("chronet://lineage/search?name=Ban%20Zhao&era=ancient",
                        Sites.Address("lineage", "search", ("name", " Ban Zhao "), ("country", ""), ("era", "ancient")));
        Assert.AreEqual("chronet://times.tc", Sites.Address("times.tc"));

        SiteAddress a = SiteAddress.Parse("CHRONET://Lineage/Search/?name=Ban%20Zhao&era=ancient&x=");
        Assert.AreEqual("lineage", a.Domain);
        Assert.AreEqual("search", a.Path);
        Assert.AreEqual("Ban Zhao", a.Get("name"));
        Assert.AreEqual("ancient", a.Get("era"));
        Assert.AreEqual("", a.Get("x"), "a blank value is absent");
        Assert.AreEqual("", a.Get("country"));

        SiteAddress bare = SiteAddress.Parse(" times.tc/day-3 ");
        Assert.AreEqual(("times.tc", "day-3"), (bare.Domain, bare.Path));
        SiteAddress article = SiteAddress.Parse("chronet://chronopedia/italy/medieval/");
        Assert.AreEqual(("chronopedia", "italy/medieval"), (article.Domain, article.Path));
        Assert.AreEqual("", SiteAddress.Parse(null).Domain);
        Assert.AreEqual("", SiteAddress.Parse("chronet://").Domain);
    }

    [Test]
    public void Resolve_ATypedNameIdOrDomain_GoesToTheSite()
    {
        var listed = new[] { SiteFixture.News, SiteFixture.Chronopedia };
        Assert.AreEqual("chronet://times.tc", Sites.Resolve("the temporal times", listed));
        Assert.AreEqual("chronet://times.tc", Sites.Resolve(" NEWS ", listed));
        Assert.AreEqual("chronet://chronopedia", Sites.Resolve("Chronopedia", listed));
        Assert.AreEqual("chronet://chronopedia", Sites.Resolve("chronopedia", listed));
        Assert.AreEqual(Sites.PortalAddress, Sites.Resolve("  ", listed));
        Assert.AreEqual("chronet://times.tc/day-2", Sites.Resolve("times.tc/day-2", listed));
        Assert.AreEqual("chronet://chronopedia/egypt/ancient", Sites.Resolve("chronet://chronopedia/egypt/ancient", listed));
        Assert.AreEqual("chronet://atlantis", Sites.Resolve("atlantis", listed), "an unknown name is tried as an address");
    }

    [Test]
    public void Portal_ATilePerListedSite()
    {
        SiteWorld w = SiteFixture.World();
        w.Sites = new List<SiteSpec> { SiteFixture.News, new SiteSpec { id = "later", name = "Later", domain = "later", fromDay = 9 }, SiteFixture.Lineage };
        SitePage page = Sites.Page(w, "chronet://home");
        Assert.IsTrue(page.Found);
        Assert.IsNull(page.SiteId);
        Assert.AreEqual(Sites.PortalAddress, page.Address);
        PageBlock tiles = page.Blocks.Single(b => b.Kind == PageBlockKind.Tiles);
        CollectionAssert.AreEqual(new[] { "The Temporal Times", "Lineage Archive" }, tiles.Links.Select(l => l.Text));
        CollectionAssert.AreEqual(new[] { "chronet://times.tc", "chronet://lineage" }, tiles.Links.Select(l => l.Address));
        Assert.AreEqual("Today's edition.", tiles.Links[0].Detail);
        Assert.AreEqual(Sites.PortalAddress, Sites.Page(w, "").Address, "a blank address is the start page");
    }

    [Test]
    public void Page_UnknownOrUnlistedSite_IsTheMissingPage()
    {
        SiteWorld w = SiteFixture.World();
        SitePage missing = Sites.Page(w, "chronet://atlantis/maps");
        Assert.IsFalse(missing.Found);
        Assert.AreEqual("chronet://atlantis/maps", missing.Address);
        Assert.IsTrue(missing.Blocks.Any(b => b.Text == "site.notFound.body(chronet://atlantis/maps)"));
        CollectionAssert.Contains(SiteFixture.Links(missing).ToList(), Sites.PortalAddress);

        w.Sites = new List<SiteSpec> { new SiteSpec { id = "news", kind = SiteKind.News, name = "Times", domain = "times.tc", fromDay = 4 } };
        Assert.IsFalse(Sites.Page(w, "chronet://times.tc").Found, "a site before its first day is not open");
        w.Day = 4;
        Assert.IsTrue(Sites.Page(w, "chronet://times.tc").Found);
        Assert.AreEqual("news", Sites.Page(w, "chronet://times.tc").SiteId);
        Assert.IsFalse(Sites.Page(w, "chronet://times.tc/sports").Found, "a path the site does not have");
    }

    [Test]
    public void StaticSite_ServesItsAuthoredPages()
    {
        SiteWorld w = SiteFixture.World();
        var desk = new SiteSpec { id = "desk", kind = SiteKind.Static, name = "Desk Handbook", domain = "handbook.tc", fromDay = 1 };
        w.Sites = new List<SiteSpec> { SiteFixture.News, desk };
        w.StaticPages = new List<StaticPage>
        {
            new StaticPage
            {
                site = "desk", path = "", title = "Handbook",
                blocks = new List<StaticBlock>
                {
                    new StaticBlock { kind = PageBlockKind.Heading, text = "Welcome" },
                    new StaticBlock { kind = PageBlockKind.Box, text = "Rules", lines = new List<string> { "Stamp once." } },
                    new StaticBlock { kind = PageBlockKind.Link, text = "The rules", address = "handbook.tc/rules" },
                    new StaticBlock { kind = PageBlockKind.Link, text = "Today's paper", address = "The Temporal Times" },
                }
            },
            new StaticPage { site = "desk", path = "rules", title = "Rules", blocks = new List<StaticBlock> { new StaticBlock { kind = PageBlockKind.Paragraph, text = "Be kind." } } },
        };

        SitePage front = Sites.Page(w, "chronet://handbook.tc");
        Assert.IsTrue(front.Found);
        Assert.AreEqual("Handbook", front.Title);
        Assert.AreEqual(PageBlockKind.Masthead, front.Blocks[0].Kind);
        Assert.AreEqual("Desk Handbook", front.Blocks[0].Text);
        Assert.AreEqual("Stamp once.", front.Blocks.Single(b => b.Kind == PageBlockKind.Box).Lines.Single());
        CollectionAssert.AreEqual(new[] { "chronet://handbook.tc/rules", "chronet://times.tc" }, SiteFixture.Links(front).ToList());
        Assert.AreEqual("Be kind.", Sites.Page(w, "chronet://handbook.tc/rules").Blocks.Last().Text);
        Assert.IsFalse(Sites.Page(w, "chronet://handbook.tc/faq").Found);
    }

    [Test]
    public void Problems_CleanContent_HasNone()
    {
        var pc = new PcContent { sites = new List<SiteSpec> { SiteFixture.News, SiteFixture.Chronopedia, SiteFixture.Lineage } };
        CollectionAssert.IsEmpty(Sites.Problems(pc));
        CollectionAssert.IsEmpty(Sites.Problems(null));
    }

    [Test]
    public void Problems_EveryRule()
    {
        var pc = new PcContent
        {
            sites = new List<SiteSpec>
            {
                SiteFixture.News,
                new SiteSpec { id = "news", kind = SiteKind.News, name = "Again", domain = "again.tc", fromDay = 1 },
                new SiteSpec { id = "", kind = SiteKind.News, name = "", domain = "Bad Domain", fromDay = 0 },
                new SiteSpec { id = "copy", kind = SiteKind.History, name = "Copy", domain = "times.tc", fromDay = 1 },
                new SiteSpec { id = "home", kind = SiteKind.History, name = "Home", domain = "home", fromDay = 1 },
                new SiteSpec { id = "bare", kind = SiteKind.Static, name = "Bare", domain = "bare.tc", fromDay = 1 },
            },
            pages = new List<StaticPage>
            {
                new StaticPage { site = "ghost", path = "" },
                new StaticPage { site = "news", path = "x" },
                new StaticPage { site = "bare", path = "a", blocks = new List<StaticBlock> { new StaticBlock { kind = PageBlockKind.Table }, new StaticBlock { kind = PageBlockKind.Link, text = "go" } } },
                new StaticPage { site = "bare", path = "A" },
            }
        };
        List<string> p = Sites.Problems(pc);
        void Has(string fragment) => Assert.IsTrue(p.Any(x => x.Contains(fragment)), $"expected a problem with '{fragment}' in [{string.Join(" | ", p)}]");
        Has("'news': the id is used twice");
        Has("has no id");
        Has("no name");
        Has("fromDay 0");
        Has("domain 'Bad Domain'");
        Has("the domain 'times.tc' is used twice");
        Has("the domain 'home' is the start page's");
        Has("'bare': a Static site needs a front page");
        Has("no site 'ghost'");
        Has("site 'news' is a News site");
        Has("a Table block");
        Has("a Link block without an address");
        Has("'bare/A': the path is used twice");
        Assert.AreEqual(13, p.Count, string.Join(" | ", p));
    }

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

    [Test]
    public void WordKeys_AreExactlyTheKeysThePagesWriteWith()
    {
        var words = new RecordingWords();
        void Visit(SiteWorld w, params string[] addresses)
        {
            w.Words = words;
            foreach (string a in addresses)
                Sites.Page(w, a);
        }

        SiteWorld full = SiteFixture.World();
        full.History.pendingCarries.Add(new CarryRecord { fromNationId = "greece", fromEraId = "ancient", toNationId = "egypt", toEraId = "ancient", category = ClueCategory.Technology, value = "Klepsydra", day = 3 });
        Visit(full, "chronet://home", "chronet://atlantis", "chronet://times.tc", "chronet://times.tc/day-2", "chronet://times.tc/archive",
              "chronet://chronopedia", "chronet://chronopedia/italy/medieval", "chronet://chronopedia/present", "chronet://chronopedia/revisions",
              "chronet://lineage", "chronet://lineage/search?name=sen", "chronet://lineage/search?name=zzz", "chronet://lineage/person/hatnefer");
        SiteWorld quiet = SiteFixture.World(4);
        quiet.Ranking = new List<RankedScore>();
        quiet.NewsArchive = new List<NewsIssue>();
        quiet.History = new HistoryState();
        Visit(quiet, "chronet://times.tc", "chronet://times.tc/archive", "chronet://chronopedia/present", "chronet://chronopedia/revisions");

        List<string> used = words.Keys.Where(k => !k.StartsWith("category.")).OrderBy(k => k).ToList();
        List<string> unused = Sites.WordKeys.Except(used).ToList(), unlisted = used.Except(Sites.WordKeys).ToList();
        Assert.IsEmpty(unused, "listed but never written with: " + string.Join(", ", unused));
        Assert.IsEmpty(unlisted, "written with but not listed: " + string.Join(", ", unlisted));
        Assert.AreEqual(Sites.WordKeys.Length, Sites.WordKeys.Distinct().Count(), "no key listed twice");
    }

    [Test]
    public void EveryLinkFromTheStartPage_OpensAPage()
    {
        SiteWorld w = SiteFixture.World();
        var seen = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(Sites.PortalAddress);
        while (queue.Count > 0)
        {
            string address = queue.Dequeue();
            if (!seen.Add(address))
                continue;
            SitePage page = Sites.Page(w, address);
            Assert.IsTrue(page.Found, $"{address} opens a page");
            Assert.AreEqual(page.Address, Sites.Page(w, page.Address).Address, $"{address}: its page's own address {page.Address} opens the same page");
            foreach (string link in SiteFixture.Links(page))
                queue.Enqueue(link);
        }
        Assert.GreaterOrEqual(seen.Count, 30, "the crawl reaches the news, every article in the world, the present, the revisions, every search and card");
        Assert.IsTrue(seen.Contains("chronet://chronopedia/china/future"), "the leader's Future place");
        Assert.IsFalse(seen.Contains("chronet://chronopedia/egypt/future"), "no other Future place");
        Assert.IsTrue(seen.Contains("chronet://lineage/person/hatnefer"));
        Assert.IsFalse(seen.Contains("chronet://lineage/person/socrates"), "no card of an impostor");
    }
}
