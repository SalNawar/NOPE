using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A small world for the Internet's page tests: words that echo their key
/// and arguments ("site.news.standing(1|China)"), three sites, four
/// countries' places (two Future ones), three premades (one an impostor) and
/// two authored people.
/// </summary>
public static class SiteFixture
{
    /// <summary>Words that echo: the key, then the arguments in brackets.</summary>
    public sealed class EchoWords : IPageWords
    {
        /// <inheritdoc />
        public string Get(string key, params object[] args) =>
            args == null || args.Length == 0 ? key : $"{key}({string.Join("|", args)})";
    }

    public static SiteSpec News => new SiteSpec { id = "news", kind = SiteKind.News, name = "The Temporal Times", domain = "times.tc", glyph = "site_news", blurb = "Today's edition.", fromDay = 1 };
    public static SiteSpec Chronopedia => new SiteSpec { id = "history", kind = SiteKind.History, name = "Chronopedia", domain = "chronopedia", glyph = "site_history", blurb = "Every place.", fromDay = 1 };
    public static SiteSpec Lineage => new SiteSpec { id = "ancestry", kind = SiteKind.Ancestry, name = "Lineage Archive", domain = "lineage", glyph = "site_ancestry", blurb = "Citizens of the past.", fromDay = 1 };

    private static (ClueCategory, string)[] Facts(string capital, string ruler, string currency, string language, string technology, string dress) => new[]
    {
        (ClueCategory.Currency, currency), (ClueCategory.Language, language), (ClueCategory.Technology, technology),
        (ClueCategory.Geography, capital), (ClueCategory.Politics, ruler), (ClueCategory.Culture, dress)
    };

    /// <summary>Egypt, Greece and Italy in the Ancient and Medieval eras (Greece Medieval missing), plus China's and Egypt's Future places.</summary>
    public static List<PlaceInfo> Places() => new List<PlaceInfo>
    {
        new PlaceInfo("egypt_ancient", "egypt", "Egypt", "ancient", "Ancient", 0, false, "New Kingdom Egypt", -1470, "Thebes under Hatshepsut.",
                      Facts("Thebes", "Pharaoh Hatshepsut", "Deben", "Middle Egyptian", "Papyrus", "wesekh collar")),
        new PlaceInfo("egypt_medieval", "egypt", "Egypt", "medieval", "Medieval", 1, false, "Mamluk Egypt", 1340, "",
                      Facts("Cairo", "Sultan al-Nasir", "Dinar", "Arabic", "Nilometer", "turban")),
        new PlaceInfo("egypt_future", "egypt", "Egypt", "future", "Future", 5, true, "Nile Arcology", 2150, "Solar arcologies.",
                      Facts("New Thebes", "The Council", "Solar credit", "Nile Standard", "Arcology", "sun veil")),
        new PlaceInfo("greece_ancient", "greece", "Greece", "ancient", "Ancient", 0, false, "Periclean Athens", -440, "Athens under Pericles.",
                      Facts("Athens", "Pericles", "Drachma", "Attic Greek", "Klepsydra", "chiton")),
        new PlaceInfo("italy_medieval", "italy", "Italy", "medieval", "Medieval", 1, false, "Florentine Republic", 1400, "Florence of the guilds.",
                      Facts("Florence", "The Signoria", "Florin", "Tuscan", "Printing press", "lucco")),
        new PlaceInfo("china_future", "china", "China", "future", "Future", 5, true, "Shanghai Megacity", 2150, "A megacity capital.",
                      Facts("Shanghai", "The Assembly", "Digital yuan", "Mandarin", "Maglev", "smart silk")),
    };

    /// <summary>Senenmut and Aspasia are who they claim; Socrates is an impostor from Italy.</summary>
    public static List<PremadeInfo> Premades() => new List<PremadeInfo>
    {
        new PremadeInfo("senenmut", "Senenmut", "14 Mar 1505 BCE", "egypt_ancient", "", "Steward of the Pharaoh's household."),
        new PremadeInfo("socrates", "Socrates", "6 Jun 470 BCE", "greece_ancient", "italy_medieval", "Philosopher."),
        new PremadeInfo("aspasia", "Aspasia", "12 Sep 470 BCE", "greece_ancient", "", "Teacher of rhetoric."),
    };

    /// <summary>Hatnefer (Senenmut's mother) and Neferure; Senenmut's card lists his mother.</summary>
    public static AncestryContent Ancestry() => new AncestryContent
    {
        includePremades = true,
        people = new List<PersonEntry>
        {
            new PersonEntry { id = "hatnefer", name = "Hatnefer", born = "c. 1540 BCE", died = "", place = "egypt_ancient", note = "Mother of Senenmut." },
            new PersonEntry { id = "neferure", name = "Neferure", born = "c. 1505 BCE", died = "c. 1480 BCE", place = "egypt_ancient", note = "Daughter of Hatshepsut." },
        },
        relations = new List<RelationEntry>
        {
            new RelationEntry { person = "senenmut", kind = "Mother", other = "hatnefer" },
            new RelationEntry { person = "hatnefer", kind = "Son", other = "senenmut" },
            new RelationEntry { person = "neferure", kind = "Tutor", other = "senenmut" },
        }
    };

    /// <summary>Day 3 with the three sites, two back issues and today's, a China-led history with one rule edit and one carry, and the cards.</summary>
    public static SiteWorld World(int day = 3)
    {
        var history = new HistoryState
        {
            leaderId = "china",
            ranking = new List<RankedScore> { new RankedScore("china", 9f), new RankedScore("egypt", 4f), new RankedScore("italy", 1f) },
            factEdits = new List<FactEdit>
            {
                new FactEdit("italy", "medieval", ClueCategory.Technology, "Chinese movable type press", 2, EditCause.Rule, "Trigger: Movable type reaches Florence"),
                new FactEdit("greece", "ancient", ClueCategory.Technology, "Papyrus", 3, EditCause.Carry, "New Kingdom Egypt (Ancient)"),
            },
        };
        var archive = new List<NewsIssue>
        {
            new NewsIssue { day = 1, briefing = new List<string> { "Welcome, desk officer." }, news = new List<string>() },
            new NewsIssue { day = 2, briefing = new List<string> { "Desk officers may now ask about the capital." }, news = new List<string> { "HISTORY: China now dominates the timeline." } },
            new NewsIssue { day = 3, briefing = new List<string>(), news = new List<string> { "HISTORY: Florentine printers set type the Chinese way.", "Robots is now DOMINANT in Showa Tokyo." } },
        };
        List<PlaceInfo> places = Places();
        return new SiteWorld
        {
            Day = day,
            Sites = new List<SiteSpec> { News, Chronopedia, Lineage },
            Words = new EchoWords(),
            NewsArchive = archive,
            Ranking = history.ranking,
            NationName = id => places.FirstOrDefault(p => p.NationId == id)?.NationName ?? id,
            Places = places,
            History = history,
            People = AncestryPages.Cards(Premades(), Ancestry()),
        };
    }
}
