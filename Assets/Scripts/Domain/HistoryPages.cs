using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>A place as Chronopedia reads it: who, when, its moment and its authored facts (history is applied on the page).</summary>
public sealed class PlaceInfo
{
    /// <summary>A place's facts for its page.</summary>
    public PlaceInfo(string id, string nationId, string nationName, string eraId, string eraName, int eraOrder, bool isFuture,
                     string displayName, int year, string moment, IReadOnlyList<(ClueCategory Category, string Value)> facts)
    {
        Id = id;
        NationId = nationId;
        NationName = nationName;
        EraId = eraId;
        EraName = eraName;
        EraOrder = eraOrder;
        IsFuture = isFuture;
        DisplayName = displayName;
        Year = year;
        Moment = moment;
        Facts = facts ?? Array.Empty<(ClueCategory, string)>();
    }

    /// <summary>The place's id ("egypt_ancient").</summary>
    public string Id { get; }

    /// <summary>Its nation's id.</summary>
    public string NationId { get; }

    /// <summary>Its nation's name ("Egypt").</summary>
    public string NationName { get; }

    /// <summary>Its era's id.</summary>
    public string EraId { get; }

    /// <summary>Its era's name ("Ancient").</summary>
    public string EraName { get; }

    /// <summary>Its era's chronological position (0 = oldest).</summary>
    public int EraOrder { get; }

    /// <summary>True for a Future place (in the world only while its nation leads).</summary>
    public bool IsFuture { get; }

    /// <summary>The place's name at its moment ("New Kingdom Egypt").</summary>
    public string DisplayName { get; }

    /// <summary>Its moment's year (negative = BCE).</summary>
    public int Year { get; }

    /// <summary>Its moment, one paragraph (world_source.json places[].moment); blank = none.</summary>
    public string Moment { get; }

    /// <summary>Its authored facts (before history), one per category.</summary>
    public IReadOnlyList<(ClueCategory Category, string Value)> Facts { get; }

    /// <summary>"New Kingdom Egypt (Ancient)" (OriginLabels.Format).</summary>
    public string Label => OriginLabels.Format(DisplayName, EraName);

    /// <summary>The authored value of a category, or null.</summary>
    public string BaseValue(ClueCategory category)
    {
        foreach ((ClueCategory c, string v) in Facts)
            if (c == category)
                return v;
        return null;
    }
}

/// <summary>
/// Chronopedia's pages (P spec IN4): the index (every country in every
/// era), an article per place in the world (every past place and the
/// leader's Future place: History.InWorld), the present, and the Revisions
/// page. An article's facts are as history stands today (History.Resolve);
/// a revised one says since which day and what it was. Revisions lists every
/// latched edit (rules and carries) and every pending carry, newest first.
/// Values are not compare-pickable: the books stay the one evidence surface.
/// Paths: "" (the index), "{nation}/{era}", "present", "revisions".
/// </summary>
public static class HistoryPages
{
    /// <summary>The present's path.</summary>
    private const string PresentPath = "present";

    /// <summary>The Revisions page's path.</summary>
    private const string RevisionsPath = "revisions";

    /// <summary>An article's facts, in infobox order: capital, ruler, currency, language, technology, dress.</summary>
    private static readonly ClueCategory[] InfoboxOrder =
    {
        ClueCategory.Geography, ClueCategory.Politics, ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology, ClueCategory.Culture
    };

    /// <summary>The page at the address's path, or null when the site has none there (an unknown place, or a Future place not in the world).</summary>
    public static SitePage Page(SiteWorld world, SiteSpec site, SiteAddress address)
    {
        switch (address.Path)
        {
            case "": return Index(world, site);
            case PresentPath: return Present(world, site);
            case RevisionsPath: return Revisions(world, site);
        }

        string[] parts = address.Path.Split('/');
        PlaceInfo place = parts.Length == 2 ? world.Place(parts[0], parts[1]) : null;
        return place != null && InWorld(world, place) ? Article(world, site, place, false) : null;
    }

    /// <summary>A place's article address (chronet://chronopedia/egypt/ancient).</summary>
    public static string ArticleAddress(SiteSpec site, string nationId, string eraId) => Sites.Address(site.domain, nationId + "/" + eraId);

    /// <summary>
    /// The index: a row per country (in content order), a column per era (in
    /// order), each cell the place's article, or a dash for a Future place not
    /// in the world; with the links to the present and the Revisions.
    /// </summary>
    public static SitePage Index(SiteWorld world, SiteSpec site)
    {
        IPageWords w = world.Words;
        var page = new SitePage { Address = Sites.Address(site.domain), Title = site.name };
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Masthead, site.name));
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Paragraph, w.Get("site.history.intro")));
        page.Blocks.Add(PageBlock.LinkTo(w.Get("site.history.present"), Sites.Address(site.domain, PresentPath)));
        page.Blocks.Add(PageBlock.LinkTo(w.Get("site.history.revisions"), Sites.Address(site.domain, RevisionsPath)));

        List<PlaceInfo> places = world.Places.Where(p => p != null).ToList();
        var eras = places.GroupBy(p => p.EraId).Select(g => g.First()).OrderBy(p => p.EraOrder).ToList();
        var table = new PageBlock { Kind = PageBlockKind.Table };
        table.Columns.Add(w.Get("site.history.country"));
        table.Columns.AddRange(eras.Select(e => e.EraName));
        foreach (string nation in places.Select(p => p.NationId).Distinct())
        {
            var row = new List<PageCell> { new PageCell { Text = places.First(p => p.NationId == nation).NationName } };
            foreach (PlaceInfo era in eras)
            {
                PlaceInfo p = world.Place(nation, era.EraId);
                row.Add(p != null && InWorld(world, p)
                    ? new PageCell { Text = p.DisplayName, Address = ArticleAddress(site, p.NationId, p.EraId) }
                    : new PageCell { Text = w.Get("site.history.none") });
            }
            table.Rows.Add(row);
        }
        page.Blocks.Add(table);
        return page;
    }

    /// <summary>
    /// A place's article: its name, era and year, its moment, and the
    /// infobox of its facts as history stands (a revised value notes the day
    /// its edit applies from and the authored value); with links to the place's
    /// people in the Lineage Archive (while it is listed), the Revisions and
    /// the index. <paramref name="asPresent"/> is the present's page (its own address and a note).
    /// </summary>
    public static SitePage Article(SiteWorld world, SiteSpec site, PlaceInfo place, bool asPresent)
    {
        IPageWords w = world.Words;
        string address = asPresent ? Sites.Address(site.domain, PresentPath) : ArticleAddress(site, place.NationId, place.EraId);
        var page = new SitePage { Address = address, Title = place.DisplayName };
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Masthead, site.name));
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Headline, place.DisplayName));
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Note, w.Get("site.history.subtitle", place.NationName, place.EraName, BirthDates.FormatYear(place.Year))));
        if (asPresent)
            page.Blocks.Add(PageBlock.Of(PageBlockKind.Note, w.Get("site.history.presentNote")));
        if (!string.IsNullOrWhiteSpace(place.Moment))
            page.Blocks.Add(PageBlock.Of(PageBlockKind.Paragraph, place.Moment));

        var box = new PageBlock { Kind = PageBlockKind.Fields };
        foreach (ClueCategory c in InfoboxOrder)
        {
            string baseValue = place.BaseValue(c);
            if (baseValue == null)
                continue;
            FactEdit edit = History.LatestEdit(world.History, place.NationId, place.EraId, c);
            bool revised = History.IsRevised(world.History, place.NationId, place.EraId, c, baseValue);
            box.Fields.Add(new PageField
            {
                Label = w.Get(ClueLabels.Key(c)),
                Value = History.Resolve(world.History, place.NationId, place.EraId, c, baseValue),
                Note = revised && edit != null ? w.Get("site.history.revised", edit.sinceDay, baseValue) : null
            });
        }
        page.Blocks.Add(box);

        SiteSpec lineage = world.SiteOf(SiteKind.Ancestry);
        if (lineage != null)
            page.Blocks.Add(PageBlock.LinkTo(w.Get("site.history.people", place.DisplayName), AncestryPages.SearchAddress(lineage, null, place.NationId, place.EraId)));
        page.Blocks.Add(PageBlock.LinkTo(w.Get("site.history.revisions"), Sites.Address(site.domain, RevisionsPath)));
        page.Blocks.Add(PageBlock.LinkTo(w.Get("site.history.index"), Sites.Address(site.domain)));
        return page;
    }

    /// <summary>The present: the leader's Future place's article, or, with no leader, the unsettled present.</summary>
    public static SitePage Present(SiteWorld world, SiteSpec site)
    {
        string leader = History.FutureNation(world.History);
        PlaceInfo present = leader != null ? world.Places.FirstOrDefault(p => p != null && p.IsFuture && p.NationId == leader) : null;
        if (present != null)
            return Article(world, site, present, true);

        IPageWords w = world.Words;
        var page = new SitePage { Address = Sites.Address(site.domain, PresentPath), Title = w.Get("site.history.present") };
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Masthead, site.name));
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Headline, w.Get("site.history.present")));
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Paragraph, w.Get("site.history.presentNone")));
        page.Blocks.Add(PageBlock.LinkTo(w.Get("site.history.index"), Sites.Address(site.domain)));
        return page;
    }

    /// <summary>
    /// Revisions: a row per latched edit (the day it applies from, the place,
    /// the fact, the value before it and after it, and why: the rule or the
    /// carry's home) and per pending carry (the day it was recorded, and
    /// "pending"), newest first (by day, later records first within a day).
    /// </summary>
    public static SitePage Revisions(SiteWorld world, SiteSpec site)
    {
        IPageWords w = world.Words;
        var page = new SitePage { Address = Sites.Address(site.domain, RevisionsPath), Title = w.Get("site.history.revisions") };
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Masthead, site.name));
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Headline, w.Get("site.history.revisions")));

        var rows = new List<(int day, List<PageCell> cells)>();
        List<FactEdit> edits = world.History?.factEdits ?? new List<FactEdit>();
        for (int i = 0; i < edits.Count; i++)
        {
            FactEdit e = edits[i];
            if (e == null || string.IsNullOrWhiteSpace(e.value))
                continue;
            FactEdit earlier = edits.Take(i).LastOrDefault(x => x != null && !string.IsNullOrWhiteSpace(x.value) && x.nationId == e.nationId && x.eraId == e.eraId && x.category == e.category);
            string before = earlier != null ? earlier.value : world.Place(e.nationId, e.eraId)?.BaseValue(e.category) ?? string.Empty;
            string why = e.cause == EditCause.Carry ? w.Get("site.history.whyCarry", e.source) : w.Get("site.history.whyRule", e.source);
            rows.Add((e.sinceDay, Row(world, site, e.sinceDay, e.nationId, e.eraId, e.category, before, e.value, why)));
        }

        foreach (CarryRecord r in world.History?.pendingCarries ?? new List<CarryRecord>())
        {
            if (r == null)
                continue;
            PlaceInfo to = world.Place(r.toNationId, r.toEraId);
            string before = History.Resolve(world.History, r.toNationId, r.toEraId, r.category, to?.BaseValue(r.category) ?? string.Empty);
            string from = world.Place(r.fromNationId, r.fromEraId)?.Label ?? $"{r.fromNationId}_{r.fromEraId}";
            rows.Add((r.day, Row(world, site, r.day, r.toNationId, r.toEraId, r.category, before, r.value, w.Get("site.history.whyPending", from))));
        }

        if (rows.Count == 0)
        {
            page.Blocks.Add(PageBlock.Of(PageBlockKind.Paragraph, w.Get("site.history.noRevisions")));
        }
        else
        {
            var table = new PageBlock { Kind = PageBlockKind.Table };
            table.Columns.AddRange(new[] { "day", "place", "fact", "before", "after", "why" }.Select(c => w.Get("site.history.col." + c)));
            rows.Reverse();
            table.Rows.AddRange(rows.OrderByDescending(r => r.day).Select(r => r.cells));
            page.Blocks.Add(table);
        }

        page.Blocks.Add(PageBlock.LinkTo(w.Get("site.history.index"), Sites.Address(site.domain)));
        return page;
    }

    /// <summary>One Revisions row; the place links to its article while the place is in the world.</summary>
    private static List<PageCell> Row(SiteWorld world, SiteSpec site, int day, string nationId, string eraId, ClueCategory category,
                                      string before, string after, string why)
    {
        PlaceInfo place = world.Place(nationId, eraId);
        return new List<PageCell>
        {
            new PageCell { Text = day.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            new PageCell
            {
                Text = place?.Label ?? $"{nationId}_{eraId}",
                Address = place != null && InWorld(world, place) ? ArticleAddress(site, nationId, eraId) : null
            },
            new PageCell { Text = world.Words.Get(ClueLabels.Key(category)) },
            new PageCell { Text = before },
            new PageCell { Text = after },
            new PageCell { Text = why }
        };
    }

    /// <summary>True when the place is in today's world (every past place; a Future place only for the leader).</summary>
    private static bool InWorld(SiteWorld world, PlaceInfo p) => History.InWorld(p.IsFuture, p.NationId, History.FutureNation(world.History));
}
