using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Which page builder serves a site (P spec IN2). Serialized as an int in the content library: append only.</summary>
public enum SiteKind
{
    /// <summary>The Temporal Times: today's front page and the back issues (NewsPages).</summary>
    News,

    /// <summary>Chronopedia: an article per place, the present and the revisions (HistoryPages).</summary>
    History,

    /// <summary>The Lineage Archive: person cards and their search (AncestryPages).</summary>
    Ancestry,

    /// <summary>Authored pages of blocks (world_source.json pc.pages).</summary>
    Static
}

/// <summary>One site of the in-game Internet (world_source.json pc.sites[], through Generate World).</summary>
[Serializable]
public sealed class SiteSpec
{
    /// <summary>Stable id ("news").</summary>
    public string id;

    /// <summary>The page builder that serves it.</summary>
    public SiteKind kind;

    /// <summary>The site's name ("The Temporal Times").</summary>
    public string name;

    /// <summary>Its address's domain ("times.tc": chronet://times.tc/...): lower-case letters, digits, dots and dashes.</summary>
    public string domain;

    /// <summary>The art key of its start-page tile glyph ("site_news").</summary>
    public string glyph;

    /// <summary>One line for its start-page tile.</summary>
    public string blurb;

    /// <summary>The first day it is listed (1 or more); every listed site is open (no upgrade gate).</summary>
    public int fromDay = 1;
}

/// <summary>The content library's PC block (world_source.json "pc", through Generate World): the sites, their authored pages and the Lineage Archive's people.</summary>
[Serializable]
public sealed class PcContent
{
    /// <summary>Every site, in start-page order.</summary>
    public List<SiteSpec> sites = new List<SiteSpec>();

    /// <summary>The Static sites' authored pages.</summary>
    public List<StaticPage> pages = new List<StaticPage>();

    /// <summary>What the Lineage Archive holds besides the premades.</summary>
    public AncestryContent ancestry = new AncestryContent();
}

/// <summary>One authored page of a Static site.</summary>
[Serializable]
public sealed class StaticPage
{
    /// <summary>The site's id.</summary>
    public string site;

    /// <summary>The page's path under the site ("" = the site's front page; "rules" = chronet://{domain}/rules).</summary>
    public string path = string.Empty;

    /// <summary>The page's title.</summary>
    public string title;

    /// <summary>Its blocks, top to bottom.</summary>
    public List<StaticBlock> blocks = new List<StaticBlock>();
}

/// <summary>One block of an authored page: a Headline, Heading, Paragraph, Note, Link or Box (Sites.StaticKinds).</summary>
[Serializable]
public sealed class StaticBlock
{
    /// <summary>What the block shows.</summary>
    public PageBlockKind kind;

    /// <summary>Its text (a Box's title, a Link's words).</summary>
    public string text;

    /// <summary>A Link's address (a site's name, domain or chronet:// address).</summary>
    public string address;

    /// <summary>A Box's lines.</summary>
    public List<string> lines = new List<string>();
}

/// <summary>
/// A chronet:// address taken apart: the domain (the site), the path inside
/// it and the query ("chronet://lineage/search?name=senen" is lineage,
/// search, name=senen). Not a URL parser: the domain and path are
/// lower-cased, the query values unescaped.
/// </summary>
public sealed class SiteAddress
{
    /// <summary>The site's domain ("" or Sites.PortalDomain for the start page).</summary>
    public string Domain = string.Empty;

    /// <summary>The path inside the site, without leading or trailing slashes ("" = the site's front page).</summary>
    public string Path = string.Empty;

    /// <summary>The query's values by key.</summary>
    public Dictionary<string, string> Query = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>A query value, or "" when absent.</summary>
    public string Get(string key) => key != null && Query.TryGetValue(key, out string v) ? v : string.Empty;

    /// <summary>Takes an address apart, with or without the scheme ("times.tc/day-3" works too); null or blank is the start page.</summary>
    public static SiteAddress Parse(string address)
    {
        var a = new SiteAddress();
        string s = (address ?? string.Empty).Trim();
        if (s.StartsWith(Sites.Scheme, StringComparison.OrdinalIgnoreCase))
            s = s.Substring(Sites.Scheme.Length);

        int q = s.IndexOf('?');
        string query = q >= 0 ? s.Substring(q + 1) : string.Empty;
        string place = (q >= 0 ? s.Substring(0, q) : s).Trim('/');
        int slash = place.IndexOf('/');
        a.Domain = (slash >= 0 ? place.Substring(0, slash) : place).Trim().ToLowerInvariant();
        a.Path = (slash >= 0 ? place.Substring(slash + 1) : string.Empty).Trim('/').Trim().ToLowerInvariant();

        foreach (string pair in query.Split('&'))
        {
            int eq = pair.IndexOf('=');
            if (eq <= 0)
                continue;
            string key = Uri.UnescapeDataString(pair.Substring(0, eq));
            string value = Uri.UnescapeDataString(pair.Substring(eq + 1).Replace('+', ' '));
            if (!string.IsNullOrWhiteSpace(value))
                a.Query[key] = value;
        }
        return a;
    }
}

/// <summary>
/// Everything the site pages read, gathered once when the browser shows a
/// page (the UI builds it from the run and the content library; the tests
/// build it by hand).
/// </summary>
public sealed class SiteWorld
{
    /// <summary>Today's day number (1-based).</summary>
    public int Day = 1;

    /// <summary>Every site (listed or not), in start-page order.</summary>
    public IReadOnlyList<SiteSpec> Sites = Array.Empty<SiteSpec>();

    /// <summary>The words the pages are written in.</summary>
    public IPageWords Words;

    /// <summary>The news back issues (WorldState.newsArchive), today's included.</summary>
    public IReadOnlyList<NewsIssue> NewsArchive = Array.Empty<NewsIssue>();

    /// <summary>Last night's influence ranking, highest first (HistoryState.ranking).</summary>
    public IReadOnlyList<RankedScore> Ranking = Array.Empty<RankedScore>();

    /// <summary>A nation's display name by id (the id itself when unknown).</summary>
    public Func<string, string> NationName = id => id;

    /// <summary>Every place of the content (Future places included), by country then era.</summary>
    public IReadOnlyList<PlaceInfo> Places = Array.Empty<PlaceInfo>();

    /// <summary>The run's history (the leader, the latched edits, the pending carries); null = none yet.</summary>
    public HistoryState History;

    /// <summary>The Lineage Archive's cards (AncestryPages.Cards).</summary>
    public IReadOnlyList<PersonCard> People = Array.Empty<PersonCard>();

    /// <summary>The Static sites' authored pages.</summary>
    public IReadOnlyList<StaticPage> StaticPages = Array.Empty<StaticPage>();

    /// <summary>A place by nation and era id, or null.</summary>
    public PlaceInfo Place(string nationId, string eraId) =>
        Places.FirstOrDefault(p => p != null && p.NationId == nationId && p.EraId == eraId);

    /// <summary>A place by its id ("egypt_ancient"), or null.</summary>
    public PlaceInfo PlaceById(string id) => Places.FirstOrDefault(p => p != null && p.Id == id);

    /// <summary>The first listed site of a kind today, or null (a page links to another site only while it is listed).</summary>
    public SiteSpec SiteOf(SiteKind kind) => global::Sites.ListedOn(Sites, Day).FirstOrDefault(s => s.kind == kind);
}

/// <summary>
/// The in-game Internet's rules (P spec IN1-IN2): which sites are listed
/// (from their first day; no upgrade gate), how an address or a typed name
/// finds a page, the start page and the missing page, and the content rules
/// Generate World and the validator apply to world_source.json's pc block.
/// </summary>
public static class Sites
{
    /// <summary>Every in-game address starts with this.</summary>
    public const string Scheme = "chronet://";

    /// <summary>The start page's domain (chronet://home); no site may take it.</summary>
    public const string PortalDomain = "home";

    /// <summary>The block kinds an authored page may use.</summary>
    public static readonly PageBlockKind[] StaticKinds =
        { PageBlockKind.Headline, PageBlockKind.Heading, PageBlockKind.Paragraph, PageBlockKind.Note, PageBlockKind.Link, PageBlockKind.Box };

    /// <summary>
    /// Every UI string key the page builders write with (world_source.json
    /// ui.strings; Generate World and the validator require each one). The
    /// infobox and Revisions labels are ClueLabels.Key's, checked with the categories.
    /// </summary>
    public static readonly string[] WordKeys =
    {
        "site.portal.title", "site.portal.masthead", "site.portal.intro", "site.notFound.title", "site.notFound.body", "site.home",
        "site.news.masthead", "site.news.issueTitle", "site.news.backIssue", "site.news.world", "site.news.notices", "site.news.noNotices",
        "site.news.standings", "site.news.standing", "site.news.noStandings", "site.news.archive", "site.news.issueLink", "site.news.allIssues",
        "site.news.today", "site.news.archiveTitle", "site.news.archiveRow", "site.news.noBackIssues", "site.news.quiet",
        "site.history.intro", "site.history.present", "site.history.revisions", "site.history.country", "site.history.none", "site.history.subtitle",
        "site.history.presentNote", "site.history.revised", "site.history.people", "site.history.index", "site.history.presentNone",
        "site.history.whyCarry", "site.history.whyRule", "site.history.whyPending", "site.history.noRevisions",
        "site.history.col.day", "site.history.col.place", "site.history.col.fact", "site.history.col.before", "site.history.col.after", "site.history.col.why",
        "site.lineage.intro", "site.lineage.country", "site.lineage.era", "site.lineage.all", "site.lineage.count", "site.lineage.hits", "site.lineage.noHits",
        "site.lineage.col.name", "site.lineage.col.born", "site.lineage.col.place",
        "site.lineage.field.name", "site.lineage.field.born", "site.lineage.field.died", "site.lineage.field.place", "site.lineage.field.note",
        "site.lineage.unknown", "site.lineage.relations", "site.lineage.relation", "site.lineage.back"
    };

    /// <summary>The start page's address.</summary>
    public static string PortalAddress => Scheme + PortalDomain;

    /// <summary>True when the site is listed on <paramref name="day"/>: from its fromDay (below 1 counts as 1).</summary>
    public static bool Listed(SiteSpec site, int day) => site != null && day >= Math.Max(1, site.fromDay);

    /// <summary>The sites listed on <paramref name="day"/>, in their order.</summary>
    public static List<SiteSpec> ListedOn(IEnumerable<SiteSpec> sites, int day) =>
        (sites ?? Enumerable.Empty<SiteSpec>()).Where(s => Listed(s, day)).ToList();

    /// <summary>An address: the domain, an optional path and the non-blank query values (escaped), in the given order.</summary>
    public static string Address(string domain, string path = null, params (string key, string value)[] query)
    {
        string address = Scheme + domain + (string.IsNullOrEmpty(path) ? string.Empty : "/" + path);
        string[] pairs = query.Where(p => !string.IsNullOrWhiteSpace(p.value))
                              .Select(p => Uri.EscapeDataString(p.key) + "=" + Uri.EscapeDataString(p.value.Trim()))
                              .ToArray();
        return pairs.Length == 0 ? address : address + "?" + string.Join("&", pairs);
    }

    /// <summary>
    /// What the address field goes to when the player types <paramref name="typed"/>:
    /// the start page for nothing; a listed site's front page for its name, id
    /// or domain (ignoring case); otherwise the text as an address.
    /// </summary>
    public static string Resolve(string typed, IEnumerable<SiteSpec> listed)
    {
        string t = (typed ?? string.Empty).Trim();
        if (t.Length == 0)
            return PortalAddress;

        foreach (SiteSpec s in listed ?? Enumerable.Empty<SiteSpec>())
            if (s != null && (Same(t, s.name) || Same(t, s.id) || Same(t, s.domain)))
                return Address(s.domain);

        return t.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase) ? t : Scheme + t;
    }

    /// <summary>
    /// The page at an address: the start page (chronet://home or a blank
    /// domain), a listed site's page from its kind's builder, else the
    /// missing page (an unknown or unlisted domain, or a path its site does not have).
    /// </summary>
    public static SitePage Page(SiteWorld world, string address)
    {
        SiteAddress a = SiteAddress.Parse(address);
        if (a.Domain.Length == 0 || a.Domain == PortalDomain)
            return Portal(world);

        SiteSpec site = ListedOn(world.Sites, world.Day).FirstOrDefault(s => Same(s.domain, a.Domain));
        SitePage page = null;
        if (site != null)
        {
            switch (site.kind)
            {
                case SiteKind.News: page = NewsPages.Page(world, site, a); break;
                case SiteKind.History: page = HistoryPages.Page(world, site, a); break;
                case SiteKind.Ancestry: page = AncestryPages.Page(world, site, a); break;
                case SiteKind.Static: page = Authored(world, site, a); break;
            }
        }

        if (page == null)
            return NotFound(world, address);
        page.SiteId = site.id;
        return page;
    }

    /// <summary>The start page: a tile per listed site (its name, blurb and glyph).</summary>
    public static SitePage Portal(SiteWorld world)
    {
        IPageWords w = world.Words;
        var page = new SitePage { Address = PortalAddress, Title = w.Get("site.portal.title") };
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Masthead, w.Get("site.portal.masthead")));
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Paragraph, w.Get("site.portal.intro")));
        page.Blocks.Add(new PageBlock
        {
            Kind = PageBlockKind.Tiles,
            Links = ListedOn(world.Sites, world.Day)
                    .Select(s => new PageLink { Text = s.name, Detail = s.blurb, Glyph = s.glyph, Address = Address(s.domain) })
                    .ToList()
        });
        return page;
    }

    /// <summary>The missing page: what was asked for, and the way back to the start page.</summary>
    public static SitePage NotFound(SiteWorld world, string address)
    {
        IPageWords w = world.Words;
        string shown = string.IsNullOrWhiteSpace(address) ? PortalAddress : address.Trim();
        var page = new SitePage { Address = shown, Title = w.Get("site.notFound.title"), Found = false };
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Headline, w.Get("site.notFound.title")));
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Paragraph, w.Get("site.notFound.body", shown)));
        page.Blocks.Add(PageBlock.LinkTo(w.Get("site.home"), PortalAddress));
        return page;
    }

    /// <summary>A Static site's authored page at the address's path, or null.</summary>
    private static SitePage Authored(SiteWorld world, SiteSpec site, SiteAddress a)
    {
        StaticPage source = world.StaticPages.FirstOrDefault(p => p != null && p.site == site.id && Same(p.path ?? string.Empty, a.Path));
        if (source == null)
            return null;

        var page = new SitePage { Address = Address(site.domain, a.Path), Title = source.title };
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Masthead, site.name));
        IReadOnlyList<SiteSpec> listed = ListedOn(world.Sites, world.Day);
        foreach (StaticBlock b in source.blocks ?? new List<StaticBlock>())
        {
            if (b == null)
                continue;
            if (b.kind == PageBlockKind.Link)
                page.Blocks.Add(PageBlock.LinkTo(b.text, Resolve(b.address, listed)));
            else if (b.kind == PageBlockKind.Box)
                page.Blocks.Add(PageBlock.Box(b.text, b.lines));
            else
                page.Blocks.Add(PageBlock.Of(b.kind, b.text));
        }
        return page;
    }

    /// <summary>
    /// The content rules of the pc block (Generate World and the validator):
    /// unique non-blank site ids and domains (lower-case letters, digits, dots
    /// and dashes; never the start page's), a name, a first day of 1 or more,
    /// a front page (path "") for every Static site; authored pages only for
    /// Static sites, one per path, of the Static block kinds, a Link with an
    /// address. Returns the problems (empty when clean).
    /// </summary>
    public static List<string> Problems(PcContent pc)
    {
        var problems = new List<string>();
        if (pc == null)
            return problems;

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var domains = new HashSet<string>(StringComparer.Ordinal);
        foreach (SiteSpec s in pc.sites ?? new List<SiteSpec>())
        {
            if (s == null)
                continue;
            string who = $"pc.sites '{s.id}'";
            if (string.IsNullOrWhiteSpace(s.id))
                problems.Add("A pc.sites entry has no id.");
            else if (!ids.Add(s.id))
                problems.Add($"{who}: the id is used twice.");
            if (string.IsNullOrWhiteSpace(s.name))
                problems.Add($"{who}: no name.");
            if (s.fromDay < 1)
                problems.Add($"{who}: fromDay {s.fromDay} (days start at 1).");
            if (string.IsNullOrWhiteSpace(s.domain) || s.domain.Any(c => !(c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '.' || c == '-')))
                problems.Add($"{who}: domain '{s.domain}' (lower-case letters, digits, dots and dashes only).");
            else if (s.domain == PortalDomain)
                problems.Add($"{who}: the domain '{PortalDomain}' is the start page's.");
            else if (!domains.Add(s.domain))
                problems.Add($"{who}: the domain '{s.domain}' is used twice.");
            if (s.kind == SiteKind.Static && !(pc.pages ?? new List<StaticPage>()).Any(p => p != null && p.site == s.id && string.IsNullOrEmpty(p.path)))
                problems.Add($"{who}: a Static site needs a front page (a pc.pages entry with path \"\").");
        }

        var paths = new HashSet<(string, string)>();
        foreach (StaticPage p in pc.pages ?? new List<StaticPage>())
        {
            if (p == null)
                continue;
            string who = $"pc.pages '{p.site}/{p.path}'";
            SiteSpec site = (pc.sites ?? new List<SiteSpec>()).FirstOrDefault(s => s != null && s.id == p.site);
            if (site == null)
                problems.Add($"{who}: no site '{p.site}'.");
            else if (site.kind != SiteKind.Static)
                problems.Add($"{who}: site '{p.site}' is a {site.kind} site; only Static sites have authored pages.");
            if (!paths.Add((p.site, (p.path ?? string.Empty).ToLowerInvariant())))
                problems.Add($"{who}: the path is used twice.");
            foreach (StaticBlock b in p.blocks ?? new List<StaticBlock>())
            {
                if (b == null)
                    continue;
                if (Array.IndexOf(StaticKinds, b.kind) < 0)
                    problems.Add($"{who}: a {b.kind} block (an authored page takes {string.Join(", ", StaticKinds)}).");
                else if (b.kind == PageBlockKind.Link && string.IsNullOrWhiteSpace(b.address))
                    problems.Add($"{who}: a Link block without an address.");
            }
        }
        return problems;
    }

    /// <summary>Trimmed, case-insensitive equality.</summary>
    private static bool Same(string a, string b) =>
        a != null && b != null && string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}
