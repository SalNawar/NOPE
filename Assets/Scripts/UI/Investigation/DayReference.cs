using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;

/// <summary>
/// The investigation's day reference (the PC redesign RF1): today's travel
/// directives (the app's Rules tab text), facts (the Reference tab's
/// registers) and citizen registry (the Records tab), set once a day by
/// GameManager through the façade, and the reference books, built from the
/// library into the Reference tab on the first case. A case's claim puts its
/// row first in every register. Day sources: they work between travellers.
/// Search's day layer (redesign phase 19, SE2) is rebuilt from them whenever
/// one arrives: every rule, every row of today's records, and every Reference
/// row of the books (once they are built). Plain C#; InvestigationUIController
/// owns it.
/// </summary>
public sealed class DayReference
{
    private readonly TMP_Text _directivesText;
    private readonly CitizenRecordsWindowController _records;
    private readonly CompareController _compare;
    private readonly ReferenceView _books;
    private readonly CaseIndex _index;

    private string _directives = string.Empty;
    private IReadOnlyList<TravelRuleSO> _rules;
    private FactTable _facts;
    private CitizenRegistry _registry;
    private ContentLibrarySO _library;

    /// <summary>The Rules tab's text, the Records tab's lookup, the compare (the book rows pick into it), the Reference tab and search's index (its day layer); any may be missing.</summary>
    public DayReference(TMP_Text directivesText, CitizenRecordsWindowController records, CompareController compare, ReferenceView books, CaseIndex index)
    {
        _directivesText = directivesText;
        _records = records;
        _compare = compare;
        _books = books;
        _index = index;
    }

    /// <summary>Sets the day's travel directives and writes them into the Rules tab.</summary>
    public void SetDirectives(IReadOnlyList<TravelRuleSO> rules)
    {
        _rules = rules;
        _directives = BuildDirectives(rules);
        ShowDirectives();
        IndexDay();
    }

    /// <summary>Writes today's directives into the Rules tab (every case does).</summary>
    public void ShowDirectives()
    {
        if (_directivesText != null)
            _directivesText.text = _directives;
    }

    /// <summary>Sets today's facts (the Reference tab's registers render these rows).</summary>
    public void SetFacts(FactTable facts)
    {
        _facts = facts;
        if (_books != null)
            _books.SetFacts(facts, _compare);
        IndexDay();
    }

    /// <summary>Hands the day's citizen registry to the Records tab (its lookup searches the index, which now holds the registry's rows), with the agency block and today's date (<paramref name="day"/> in the agency's calendar) its extract prints.</summary>
    public void SetCitizenRegistry(CitizenRegistry registry, AgencyContent agency, int day)
    {
        _registry = registry;
        IndexDay();
        if (_records != null)
            _records.SetRegistry(registry, _index, agency, agency != null ? AgencyCalendar.Today(agency.firstDate, day) : null);
    }

    /// <summary>The first time only: the Reference tab's books from the library (and their rows into search).</summary>
    public void BuildBooks(ContentLibrarySO lib)
    {
        if (_books != null)
            _books.BuildBooks(lib);
        if (_library == null && lib != null)
        {
            _library = lib;
            IndexDay();
        }
    }

    /// <summary>A new case's claim: its row first in every register, "Claimed place only" on (AP8).</summary>
    public void SetClaim(CaseInstance inst)
    {
        if (_books != null)
            _books.SetClaim(inst != null && inst.claimedNation != null ? inst.claimedNation.id : null,
                            inst != null && inst.claimedEra != null ? inst.claimedEra.id : null);
    }

    /// <summary>Search's day layer from what the day has so far: the rules ("Rule n"), the books' rows (titled "book · place"; the country's name and "revised" matched too) and the records' rows ("name · label").</summary>
    private void IndexDay()
    {
        if (_index == null)
            return;
        var day = new List<IndexEntry>();
        int n = 0;
        foreach (TravelRuleSO rule in _rules ?? new List<TravelRuleSO>())
            if (rule != null)
            {
                day.Add(IndexEntries.Rule(n, UiText.Format("search.title.rule", n + 1), rule.Summary()));
                n++;
            }
        string rowTitle = UiText.Get("search.title.row");
        if (_books != null && _facts != null)
            day.AddRange(IndexEntries.BookRows(_books.Books.Select(b => (b.category, b.displayName)).ToList(), _facts, CountryName,
                                               UiText.Get("search.revised"), rowTitle));
        day.AddRange(IndexEntries.Records(_registry, rowTitle));
        _index.SetDay(day);
    }

    /// <summary>A nation's name ("Greece"), or null when the library does not know it.</summary>
    private string CountryName(string nationId)
    {
        NationSO nation = _library != null ? _library.GetNationById(nationId) : null;
        return nation != null ? nation.displayName : null;
    }

    private static string BuildDirectives(IReadOnlyList<TravelRuleSO> rules)
    {
        if (rules == null || rules.Count == 0)
            return UiText.Get("directives.none");

        var sb = new StringBuilder(UiText.Get("directives.header") + "\n");
        foreach (TravelRuleSO r in rules)
            if (r != null)
                sb.AppendLine(UiText.Format("list.bullet", r.Summary()));
        return sb.ToString();
    }
}
