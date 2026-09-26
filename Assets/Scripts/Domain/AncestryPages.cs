using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>What the Lineage Archive holds besides the premades (world_source.json pc.ancestry).</summary>
[Serializable]
public sealed class AncestryContent
{
    /// <summary>True: every premade who is who they claim (an empty truePlace) has a card.</summary>
    public bool includePremades = true;

    /// <summary>Authored people of the past.</summary>
    public List<PersonEntry> people = new List<PersonEntry>();

    /// <summary>Relations between cards (premades and people), one direction per row.</summary>
    public List<RelationEntry> relations = new List<RelationEntry>();
}

/// <summary>One authored person of the past (pc.ancestry.people[]).</summary>
[Serializable]
public sealed class PersonEntry
{
    /// <summary>Stable id (unique among people and premades).</summary>
    public string id;

    /// <summary>Their name.</summary>
    public string name;

    /// <summary>When they were born, as written ("c. 1540 BCE"); blank = unknown.</summary>
    public string born;

    /// <summary>When they died, as written; blank = unknown.</summary>
    public string died;

    /// <summary>Their place's id ("egypt_ancient").</summary>
    public string place;

    /// <summary>Their occupation or a note.</summary>
    public string note;
}

/// <summary>One relation between two cards, shown on <see cref="person"/>'s card as "{kind}: {other}".</summary>
[Serializable]
public sealed class RelationEntry
{
    /// <summary>The card the relation is shown on (a person's or a premade's id).</summary>
    public string person;

    /// <summary>What the other is to them ("Mother", "Pupil").</summary>
    public string kind;

    /// <summary>The other card's id.</summary>
    public string other;
}

/// <summary>A premade as the Lineage Archive reads it (from the content library's premades).</summary>
public readonly struct PremadeInfo
{
    /// <summary>A premade's card data.</summary>
    public PremadeInfo(string id, string name, string birthDate, string placeId, string truePlaceId, string note)
    {
        Id = id;
        Name = name;
        BirthDate = birthDate;
        PlaceId = placeId;
        TruePlaceId = truePlaceId;
        Note = note;
    }

    /// <summary>The premade's id.</summary>
    public string Id { get; }

    /// <summary>Their name.</summary>
    public string Name { get; }

    /// <summary>Their registered birth date.</summary>
    public string BirthDate { get; }

    /// <summary>The place they claim.</summary>
    public string PlaceId { get; }

    /// <summary>Where they really come from; blank = they are who they claim.</summary>
    public string TruePlaceId { get; }

    /// <summary>Their Citizen Records note.</summary>
    public string Note { get; }
}

/// <summary>A person card of the Lineage Archive.</summary>
public sealed class PersonCard
{
    /// <summary>The card's id (a premade's or an authored person's).</summary>
    public string Id;

    /// <summary>Their name.</summary>
    public string Name;

    /// <summary>Born, as written; blank = unknown.</summary>
    public string Born;

    /// <summary>Died, as written; blank = unknown.</summary>
    public string Died;

    /// <summary>Their place's id.</summary>
    public string PlaceId;

    /// <summary>Their occupation or note.</summary>
    public string Note;

    /// <summary>Their relations, in authored order: what the other is to them, and the other card's id.</summary>
    public List<(string Kind, string PersonId)> Relations = new List<(string, string)>();
}

/// <summary>
/// The Lineage Archive (P spec IN5; traveller-types spec R4): person cards
/// of the premades who are who they claim (an impostor gets no card of the
/// real person unless one is authored, so no card contradicts the game) and
/// of authored people, a name search with country and era filters, and a
/// card per person (name, born, died, place linking to its Chronopedia
/// article, note, relations linking to other cards). Generated travellers get
/// no card: a card would either reveal a liar without proof or repeat the
/// registry. Cards are not compare-pickable.
/// Paths: "" and "search" (?name=&amp;country=&amp;era=), "person/{id}".
/// </summary>
public static class AncestryPages
{
    /// <summary>The search page's path.</summary>
    private const string SearchPath = "search";

    /// <summary>A card's path prefix.</summary>
    private const string PersonPrefix = "person/";

    /// <summary>
    /// Every card, premades first (in content order; only those with an empty
    /// true place, and only when the content includes premades), then the
    /// authored people; each with the relations authored for it.
    /// </summary>
    public static List<PersonCard> Cards(IEnumerable<PremadeInfo> premades, AncestryContent content)
    {
        var cards = new List<PersonCard>();
        if (content == null || content.includePremades)
            foreach (PremadeInfo m in premades ?? Enumerable.Empty<PremadeInfo>())
                if (!string.IsNullOrWhiteSpace(m.Id) && string.IsNullOrWhiteSpace(m.TruePlaceId))
                    cards.Add(new PersonCard { Id = m.Id, Name = m.Name, Born = m.BirthDate, PlaceId = m.PlaceId, Note = m.Note });

        foreach (PersonEntry p in content?.people ?? new List<PersonEntry>())
            if (p != null && !string.IsNullOrWhiteSpace(p.id))
                cards.Add(new PersonCard { Id = p.id, Name = p.name, Born = p.born, Died = p.died, PlaceId = p.place, Note = p.note });

        foreach (RelationEntry r in content?.relations ?? new List<RelationEntry>())
        {
            PersonCard card = r != null ? cards.FirstOrDefault(c => c.Id == r.person) : null;
            if (card != null && cards.Any(c => c.Id == r.other))
                card.Relations.Add((r.kind, r.other));
        }
        return cards;
    }

    /// <summary>
    /// The cards whose name contains <paramref name="name"/> (trimmed, ignoring
    /// case; blank = every name) and whose place is of the country and era
    /// given (blank = any; a card whose place is unknown only matches no
    /// filter), sorted by name.
    /// </summary>
    public static List<PersonCard> Search(IEnumerable<PersonCard> cards, string name, string nationId, string eraId, Func<string, PlaceInfo> placeById)
    {
        string q = (name ?? string.Empty).Trim();
        return (cards ?? Enumerable.Empty<PersonCard>())
               .Where(c => c != null && (q.Length == 0 || (c.Name ?? string.Empty).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
               .Where(c =>
               {
                   if (string.IsNullOrWhiteSpace(nationId) && string.IsNullOrWhiteSpace(eraId))
                       return true;
                   PlaceInfo p = placeById(c.PlaceId);
                   return p != null && (string.IsNullOrWhiteSpace(nationId) || p.NationId == nationId) && (string.IsNullOrWhiteSpace(eraId) || p.EraId == eraId);
               })
               .OrderBy(c => c.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
               .ToList();
    }

    /// <summary>The page at the address's path, or null when the site has none there.</summary>
    public static SitePage Page(SiteWorld world, SiteSpec site, SiteAddress address)
    {
        if (address.Path.Length == 0 || address.Path == SearchPath)
            return SearchPage(world, site, address.Get("name"), address.Get("country"), address.Get("era"));
        if (address.Path.StartsWith(PersonPrefix, StringComparison.Ordinal))
        {
            string id = address.Path.Substring(PersonPrefix.Length);
            PersonCard card = world.People.FirstOrDefault(c => c != null && string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));
            return card != null ? Card(world, site, card) : null;
        }
        return null;
    }

    /// <summary>The search page's address with its name and filters (blank ones left out).</summary>
    public static string SearchAddress(SiteSpec site, string name, string nationId, string eraId) =>
        Sites.Address(site.domain, SearchPath, ("name", name), ("country", nationId), ("era", eraId));

    /// <summary>A card's address.</summary>
    public static string CardAddress(SiteSpec site, string id) => Sites.Address(site.domain, PersonPrefix + id);

    /// <summary>
    /// The search page: the country and era chips (each keeps the other
    /// filters and the name; "All" clears its own), the number of records and
    /// a row per hit (the name linking to the card, the born date, the place
    /// linking to its article).
    /// </summary>
    public static SitePage SearchPage(SiteWorld world, SiteSpec site, string name, string nationId, string eraId)
    {
        IPageWords w = world.Words;
        var page = new SitePage { Address = SearchAddress(site, name, nationId, eraId), Title = site.name };
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Masthead, site.name));
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Paragraph, w.Get("site.lineage.intro")));

        List<PlaceInfo> places = world.Places.Where(p => p != null && !p.IsFuture).ToList();
        var countries = new PageBlock { Kind = PageBlockKind.Chips, Text = w.Get("site.lineage.country") };
        countries.Links.Add(Chip(w.Get("site.lineage.all"), SearchAddress(site, name, null, eraId), string.IsNullOrWhiteSpace(nationId)));
        foreach (PlaceInfo p in places.GroupBy(p => p.NationId).Select(g => g.First()))
            countries.Links.Add(Chip(p.NationName, SearchAddress(site, name, p.NationId, eraId), p.NationId == nationId));
        page.Blocks.Add(countries);

        var eras = new PageBlock { Kind = PageBlockKind.Chips, Text = w.Get("site.lineage.era") };
        eras.Links.Add(Chip(w.Get("site.lineage.all"), SearchAddress(site, name, nationId, null), string.IsNullOrWhiteSpace(eraId)));
        foreach (PlaceInfo p in places.GroupBy(p => p.EraId).Select(g => g.First()).OrderBy(p => p.EraOrder))
            eras.Links.Add(Chip(p.EraName, SearchAddress(site, name, nationId, p.EraId), p.EraId == eraId));
        page.Blocks.Add(eras);

        List<PersonCard> hits = Search(world.People, name, nationId, eraId, world.PlaceById);
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Note, string.IsNullOrWhiteSpace(name) ? w.Get("site.lineage.count", hits.Count) : w.Get("site.lineage.hits", hits.Count, name.Trim())));
        if (hits.Count == 0)
        {
            page.Blocks.Add(PageBlock.Of(PageBlockKind.Paragraph, w.Get("site.lineage.noHits")));
            return page;
        }

        var table = new PageBlock { Kind = PageBlockKind.Table };
        table.Columns.AddRange(new[] { "name", "born", "place" }.Select(c => w.Get("site.lineage.col." + c)));
        foreach (PersonCard c in hits)
        {
            PlaceInfo place = world.PlaceById(c.PlaceId);
            table.Rows.Add(new List<PageCell>
            {
                new PageCell { Text = c.Name, Address = CardAddress(site, c.Id) },
                new PageCell { Text = Or(c.Born, w) },
                new PageCell { Text = place?.Label ?? Or(c.PlaceId, w), Address = ArticleOf(world, place) }
            });
        }
        page.Blocks.Add(table);
        return page;
    }

    /// <summary>A person's card: name, born, died, place (linking to its article), note, then the relations (each linking to its card) and the way back to the search.</summary>
    public static SitePage Card(SiteWorld world, SiteSpec site, PersonCard card)
    {
        IPageWords w = world.Words;
        PlaceInfo place = world.PlaceById(card.PlaceId);
        var page = new SitePage { Address = CardAddress(site, card.Id), Title = card.Name };
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Masthead, site.name));
        page.Blocks.Add(PageBlock.Of(PageBlockKind.Headline, card.Name));
        page.Blocks.Add(new PageBlock
        {
            Kind = PageBlockKind.Fields,
            Fields = new List<PageField>
            {
                new PageField { Label = w.Get("site.lineage.field.name"), Value = card.Name },
                new PageField { Label = w.Get("site.lineage.field.born"), Value = Or(card.Born, w) },
                new PageField { Label = w.Get("site.lineage.field.died"), Value = Or(card.Died, w) },
                new PageField { Label = w.Get("site.lineage.field.place"), Value = place?.Label ?? Or(card.PlaceId, w), Address = ArticleOf(world, place) },
                new PageField { Label = w.Get("site.lineage.field.note"), Value = Or(card.Note, w) }
            }
        });

        if (card.Relations.Count > 0)
        {
            page.Blocks.Add(PageBlock.Of(PageBlockKind.Heading, w.Get("site.lineage.relations")));
            foreach ((string kind, string other) in card.Relations)
            {
                PersonCard o = world.People.FirstOrDefault(c => c != null && c.Id == other);
                if (o != null)
                    page.Blocks.Add(PageBlock.LinkTo(w.Get("site.lineage.relation", kind, o.Name), CardAddress(site, o.Id)));
            }
        }

        page.Blocks.Add(PageBlock.LinkTo(w.Get("site.lineage.back"), Sites.Address(site.domain)));
        return page;
    }

    /// <summary>
    /// The content rules of pc.ancestry (Generate World and the validator):
    /// every person has a unique id (not a premade's), a name that is no
    /// traveller's (<paramref name="travellerNames"/>: the places' given names
    /// and the premades' names, ignoring case: a card must never describe
    /// someone at the desk) and a known place; every relation names two known
    /// cards and a kind. Returns the problems (empty when clean).
    /// </summary>
    public static List<string> Problems(AncestryContent content, ICollection<string> premadeIds, ICollection<string> placeIds, IEnumerable<string> travellerNames)
    {
        var problems = new List<string>();
        if (content == null)
            return problems;

        var ids = new HashSet<string>(premadeIds ?? Array.Empty<string>(), StringComparer.Ordinal);
        var taken = new HashSet<string>((travellerNames ?? Array.Empty<string>()).Where(n => n != null).Select(n => n.Trim()), StringComparer.OrdinalIgnoreCase);
        foreach (PersonEntry p in content.people ?? new List<PersonEntry>())
        {
            if (p == null)
                continue;
            string who = $"pc.ancestry.people '{p.id}'";
            if (string.IsNullOrWhiteSpace(p.id))
                problems.Add("A pc.ancestry.people entry has no id.");
            else if (!ids.Add(p.id))
                problems.Add($"{who}: the id is used twice (people and premades share ids).");
            if (string.IsNullOrWhiteSpace(p.name))
                problems.Add($"{who}: no name.");
            else if (taken.Contains(p.name.Trim()))
                problems.Add($"{who}: '{p.name}' is a traveller's name (a place's given name or a premade's); a card would describe someone at the desk.");
            if (string.IsNullOrWhiteSpace(p.place) || placeIds == null || !placeIds.Contains(p.place))
                problems.Add($"{who}: unknown place '{p.place}'.");
        }

        foreach (RelationEntry r in content.relations ?? new List<RelationEntry>())
        {
            if (r == null)
                continue;
            string who = $"pc.ancestry.relations '{r.person}' -> '{r.other}'";
            if (string.IsNullOrWhiteSpace(r.person) || !ids.Contains(r.person))
                problems.Add($"{who}: unknown person '{r.person}'.");
            if (string.IsNullOrWhiteSpace(r.other) || !ids.Contains(r.other))
                problems.Add($"{who}: unknown person '{r.other}'.");
            if (string.IsNullOrWhiteSpace(r.kind))
                problems.Add($"{who}: no kind.");
        }
        return problems;
    }

    /// <summary>A filter chip.</summary>
    private static PageLink Chip(string text, string address, bool active) => new PageLink { Text = text, Address = address, Active = active };

    /// <summary>The value, or "Unknown" when blank.</summary>
    private static string Or(string value, IPageWords w) => string.IsNullOrWhiteSpace(value) ? w.Get("site.lineage.unknown") : value;

    /// <summary>The place's Chronopedia article while Chronopedia is listed and the place is in the world; else null.</summary>
    private static string ArticleOf(SiteWorld world, PlaceInfo place)
    {
        SiteSpec history = world.SiteOf(SiteKind.History);
        return history != null && place != null && History.InWorld(place.IsFuture, place.NationId, History.FutureNation(world.History))
            ? HistoryPages.ArticleAddress(history, place.NationId, place.EraId)
            : null;
    }
}
