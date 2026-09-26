using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// The Temporal Times' pages (P spec IN3): today's front page is the morning
/// paper's lines (the first news line the lead, the other news under
/// "World", the notices boxed, the timeline standings from last night's
/// ranking and links to the back issues); a back issue shows its own day's
/// lines; the archive lists every issue on file, newest first. The site has
/// no lines of its own: whatever reaches the morning paper reaches it.
/// Paths: "" and "day-{today}" (the front page), "day-{n}" (a back issue), "archive".
/// </summary>
public static class NewsPages
{
    /// <summary>The path prefix of an issue ("day-3").</summary>
    private const string IssuePrefix = "day-";

    /// <summary>The archive page's path.</summary>
    private const string ArchivePath = "archive";

    /// <summary>The page at the address's path, or null when the site has none there.</summary>
    public static SitePage Page(SiteWorld world, SiteSpec site, SiteAddress address)
    {
        if (address.Path.Length == 0)
            return Front(world, site);
        if (address.Path == ArchivePath)
            return Archive(world, site);
        if (address.Path.StartsWith(IssuePrefix, StringComparison.Ordinal) &&
            int.TryParse(address.Path.Substring(IssuePrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int day))
        {
            if (day == world.Day)
                return Front(world, site);
            NewsIssue issue = NewsArchive.Find(world.NewsArchive, day);
            return issue != null && day < world.Day ? Issue(world, site, issue) : null;
        }
        return null;
    }

    /// <summary>An issue's address.</summary>
    public static string IssueAddress(SiteSpec site, int day) => Sites.Address(site.domain, IssuePrefix + day.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Today's front page: today's issue from the archive (a quiet page when
    /// none is on file), the standings (highest first, by nation name) and
    /// the earlier issues, newest first, with the archive's link.
    /// </summary>
    public static SitePage Front(SiteWorld world, SiteSpec site)
    {
        IPageWords w = world.Words;
        NewsIssue today = NewsArchive.Find(world.NewsArchive, world.Day) ?? new NewsIssue { day = world.Day };
        var page = new SitePage { Address = IssueAddress(site, world.Day), Title = site.name };
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Masthead, w.Get("site.news.masthead", site.name, world.Day)));
        AddLines(page, today, w);

        List<string> standings = (world.Ranking ?? Array.Empty<RankedScore>())
                                 .Select((r, i) => w.Get("site.news.standing", i + 1, world.NationName(r.id)))
                                 .ToList();
        if (standings.Count == 0)
            standings.Add(w.Get("site.news.noStandings"));
        page.Blocks.Add(PageBlock.Box(w.Get("site.news.standings"), standings));

        List<PageLink> back = Earlier(world)
                              .Select(i => new PageLink { Text = w.Get("site.news.issueLink", i.day), Address = IssueAddress(site, i.day) })
                              .ToList();
        back.Add(new PageLink { Text = w.Get("site.news.allIssues"), Address = Sites.Address(site.domain, ArchivePath) });
        page.Blocks.Add(PageBlock.Box(w.Get("site.news.archive"), Array.Empty<string>(), back));
        return page;
    }

    /// <summary>A back issue: its own lines, and the ways to today's edition and the archive.</summary>
    public static SitePage Issue(SiteWorld world, SiteSpec site, NewsIssue issue)
    {
        IPageWords w = world.Words;
        var page = new SitePage { Address = IssueAddress(site, issue.day), Title = w.Get("site.news.issueTitle", site.name, issue.day) };
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Masthead, w.Get("site.news.masthead", site.name, issue.day)));
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Note, w.Get("site.news.backIssue", issue.day)));
        AddLines(page, issue, w);
        page.Blocks.Add(PageBlock.LinkTo(w.Get("site.news.today"), Sites.Address(site.domain)));
        page.Blocks.Add(PageBlock.LinkTo(w.Get("site.news.allIssues"), Sites.Address(site.domain, ArchivePath)));
        return page;
    }

    /// <summary>The archive: every issue before today, newest first, each with its lead.</summary>
    public static SitePage Archive(SiteWorld world, SiteSpec site)
    {
        IPageWords w = world.Words;
        var page = new SitePage { Address = Sites.Address(site.domain, ArchivePath), Title = w.Get("site.news.archiveTitle") };
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Masthead, site.name));
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Headline, w.Get("site.news.archiveTitle")));
        List<NewsIssue> earlier = Earlier(world);
        if (earlier.Count == 0)
            page.Blocks.Add(PageBlock.Of(PageBlockKind.Paragraph, w.Get("site.news.noBackIssues")));
        foreach (NewsIssue i in earlier)
            page.Blocks.Add(PageBlock.LinkTo(w.Get("site.news.archiveRow", i.day, Lead(i, w)), IssueAddress(site, i.day)));
        page.Blocks.Add(PageBlock.LinkTo(w.Get("site.news.today"), Sites.Address(site.domain)));
        return page;
    }

    /// <summary>An issue's lead: its first news line, else the quiet-day line.</summary>
    private static string Lead(NewsIssue issue, IPageWords w) =>
        issue.news != null && issue.news.Count > 0 ? issue.news[0] : w.Get("site.news.quiet");

    /// <summary>The lead as the headline, the other news under World, and the notices boxed.</summary>
    private static void AddLines(SitePage page, NewsIssue issue, IPageWords w)
    {
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Headline, Lead(issue, w)));
        List<string> world = (issue.news ?? new List<string>()).Skip(1).ToList();
        if (world.Count > 0)
        {
            page.Blocks.Add(PageBlock.Of(PageBlockKind.Heading, w.Get("site.news.world")));
            foreach (string line in world)
                page.Blocks.Add(PageBlock.Of(PageBlockKind.Paragraph, line));
        }

        List<string> notices = (issue.briefing ?? new List<string>()).ToList();
        if (notices.Count == 0)
            notices.Add(w.Get("site.news.noNotices"));
        page.Blocks.Add(PageBlock.Box(w.Get("site.news.notices"), notices));
    }

    /// <summary>The issues before today on file, newest first.</summary>
    private static List<NewsIssue> Earlier(SiteWorld world) =>
        (world.NewsArchive ?? Array.Empty<NewsIssue>()).Where(i => i != null && i.day < world.Day).OrderByDescending(i => i.day).ToList();
}
