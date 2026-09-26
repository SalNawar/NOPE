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
/// Each tab has one view per pane of the app, and each is filled.
/// Search's day layer (redesign phase 19, SE2) is rebuilt from them whenever
/// one arrives: every rule, every row of today's records, and every Reference
/// row of the books (once they are built). Plain C#; InvestigationUIController
/// owns it.
/// </summary>
public sealed class DayReference
{
    private readonly IReadOnlyList<TMP_Text> _directivesTexts;
    private readonly IReadOnlyList<CitizenRecordsWindowController> _records;
    private readonly CompareController _compare;
    private readonly IReadOnlyList<ReferenceView> _books;
    private readonly CaseIndex _index;

    private string _directives = string.Empty;
    private IReadOnlyList<TravelRuleSO> _rules;
    private FactTable _facts;
    private CitizenRegistry _registry;
    private ContentLibrarySO _library;

    /// <summary>The Rules tabs' texts, the Records tabs' lookups, the compare (the book rows pick into it), the Reference tabs (one each per pane; null entries are skipped) and search's index (its day layer; null: nothing indexed).</summary>
    public DayReference(IReadOnlyList<TMP_Text> directivesTexts, IReadOnlyList<CitizenRecordsWindowController> records, CompareController compare,
                        IReadOnlyList<ReferenceView> books, CaseIndex index)
    {
        _directivesTexts = directivesTexts ?? System.Array.Empty<TMP_Text>();
        _records = records ?? System.Array.Empty<CitizenRecordsWindowController>();
        _compare = compare;
        _books = books ?? System.Array.Empty<ReferenceView>();
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
        foreach (TMP_Text text in _directivesTexts)
            if (text != null)
                text.text = _directives;
    }

    /// <summary>Sets today's facts (the Reference tab's registers render these rows).</summary>
    public void SetFacts(FactTable facts)
    {
        _facts = facts;
        foreach (ReferenceView books in _books)
            if (books != null)
                books.SetFacts(facts, _compare);
        IndexDay();
    }

    /// <summary>Hands the day's citizen registry to the Records tab (its lookup searches the index, which now holds the registry's rows), with the agency block and today's date (<paramref name="day"/> in the agency's calendar) its extract prints.</summary>
    public void SetCitizenRegistry(CitizenRegistry registry, AgencyContent agency, int day)
    {
        _registry = registry;
        IndexDay();
        string today = agency != null ? AgencyCalendar.Today(agency.firstDate, day) : null;
        foreach (CitizenRecordsWindowController records in _records)
            if (records != null)
                records.SetRegistry(registry, _index, agency, today);
    }

    /// <summary>The first time only: the Reference tab's books from the library (and their rows into search).</summary>
    public void BuildBooks(ContentLibrarySO lib)
    {
        foreach (ReferenceView books in _books)
            if (books != null)
                books.BuildBooks(lib);
        if (_library == null && lib != null)
        {
            _library = lib;
            IndexDay();
        }
    }

    /// <summary>A new case's claim: its row first in every register, "Claimed place only" on (AP8).</summary>
    public void SetClaim(CaseInstance inst)
    {
        CaseClaim claim = AppLinks.Claim(inst);
        foreach (ReferenceView books in _books)
            if (books != null)
                books.SetClaim(claim.NationId, claim.EraId);
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
        ReferenceView books = _books.FirstOrDefault(b => b != null);
        if (books != null && _facts != null)
            day.AddRange(IndexEntries.BookRows(books.Books.Select(b => (b.category, b.displayName)).ToList(), _facts, CountryName,
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
