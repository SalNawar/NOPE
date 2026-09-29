using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gathers what the Internet's pages read (SiteWorld) from the run and the
/// content library: the sites and authored pages (ContentLibrarySO.Pc), the
/// news back issues and the ranking (WorldState), every place with its
/// authored facts and moment, the history, and the Lineage Archive's cards
/// (the premades and the authored people). The pages write with the UI
/// string tables (UiText). Built each time the browser opens, so it always
/// shows the day as it stands.
/// </summary>
public static class SiteWorldBuilder
{
    /// <summary>The pages' words: the UI string tables through UiText.</summary>
    private sealed class UiPageWords : IPageWords
    {
        /// <inheritdoc />
        public string Get(string key, params object[] args) => UiText.Format(key, args);
    }

    /// <summary>The one words instance.</summary>
    private static readonly IPageWords Words = new UiPageWords();

    /// <summary>The run's Internet (a world with no sites when the library or the run is missing).</summary>
    public static SiteWorld Build(ContentLibrarySO lib, WorldState world)
    {
        var w = new SiteWorld { Words = Words, Day = world != null ? world.day : 1 };
        if (lib == null || world == null)
            return w;

        PcContent pc = lib.Pc;
        w.Sites = pc.sites;
        w.StaticPages = pc.pages;
        w.NewsArchive = world.newsArchive;
        w.History = world.history;
        w.Ranking = world.history != null ? world.history.ranking : new List<RankedScore>();
        w.NationName = id => lib.GetNationById(id) is NationSO n && !string.IsNullOrWhiteSpace(n.displayName) ? n.displayName : id;
        w.Places = Places(lib);
        w.People = AncestryPages.Cards(lib.Legendaries.Where(l => l != null).Select(l => new PremadeInfo(
            l.id, l.displayName, l.birthDate, lib.GetProfile(l.nation, l.trueEra)?.id, l.truePlace != null ? l.truePlace.id : null, l.recordNote)), pc.ancestry);
        return w;
    }

    /// <summary>Every place with a nation and an era, by the library's nation order then era order.</summary>
    private static List<PlaceInfo> Places(ContentLibrarySO lib)
    {
        var nationOrder = new Dictionary<NationSO, int>();
        for (int i = 0; i < lib.Nations.Count; i++)
            if (lib.Nations[i] != null)
                nationOrder[lib.Nations[i]] = i;

        return lib.Profiles
                  .Where(p => p != null && p.nation != null && p.era != null)
                  .OrderBy(p => nationOrder.TryGetValue(p.nation, out int n) ? n : int.MaxValue)
                  .ThenBy(p => p.era.order)
                  .Select(p => new PlaceInfo(p.id, p.nation.id, p.nation.displayName, p.era.id, p.era.displayName, p.era.order, p.era.isFuture,
                                             p.displayName, p.year, p.moment,
                                             (p.facts ?? new List<ProfileFact>()).Where(f => f != null).Select(f => (f.category, f.value)).ToList()))
                  .ToList();
    }
}
