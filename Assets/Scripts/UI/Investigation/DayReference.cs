using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The investigation's day reference (the PC redesign RF1): today's travel
/// directives (the app's Rules tab: the Directive Memo), facts (the
/// Reference tab's registers) and citizen registry (the Records tab: the
/// Record Extract) and the day in the agency's calendar (the Calendar tab),
/// set once a day by GameManager through the façade, and
/// the reference books, built from the library into the Reference tab on
/// the first case. A case's claim puts its row first in every register. Day
/// sources: they work between travellers. Each tab has one view per pane of
/// the app, and each is filled; the agency block and the day head every
/// page. Search's day layer (redesign phase 19, SE2) is rebuilt from them
/// whenever one arrives: every rule, every row of today's records, and every
/// Reference row of the books (once they are built). Plain C#;
/// InvestigationUIController owns it.
/// </summary>
public sealed class DayReference
{
    private readonly IReadOnlyList<RulesView> _rulesViews;
    private readonly IReadOnlyList<RecordsView> _records;
    private readonly CompareController _compare;
    private readonly IReadOnlyList<ReferenceView> _books;
    private readonly IReadOnlyList<CalendarView> _calendars;
    private readonly CaseIndex _index;

    private IReadOnlyList<TravelRuleSO> _rules;
    private FactTable _facts;
    private CitizenRegistry _registry;
    private ContentLibrarySO _library;
    private AgencyContent _agency;

    /// <summary>Today's day number (set with the day's registry; the steps checklist lists a step from its first day).</summary>
    public int Day { get; private set; } = 1;

    /// <summary>Raised after a lookup of a name or number in any Records tab (typed, or a link's or a jump's), whatever it found; not after a blank one (the steps checklist's "a record looked up").</summary>
    public event System.Action RecordLookedUp;

    /// <summary>The Rules tabs (one each per pane; null entries are skipped), the Records tabs' lookups (each announces its lookups here; each searches the index), the compare (the book rows pick into it), the Reference tabs, the Calendar tabs and search's index (its day layer).</summary>
    public DayReference(IReadOnlyList<RulesView> rulesViews, IReadOnlyList<RecordsView> records, CompareController compare,
                        IReadOnlyList<ReferenceView> books, IReadOnlyList<CalendarView> calendars, CaseIndex index)
    {
        _calendars = calendars ?? System.Array.Empty<CalendarView>();
        _rulesViews = rulesViews ?? System.Array.Empty<RulesView>();
        _records = records ?? System.Array.Empty<RecordsView>();
        _compare = compare;
        _books = books ?? System.Array.Empty<ReferenceView>();
        _index = index;
        foreach (RecordsView lookup in _records)
            if (lookup != null)
                lookup.Looked += () => LookedUp(lookup);
    }

    /// <summary>A Records tab looked something up: announced unless the lookup was blank.</summary>
    private void LookedUp(RecordsView lookup)
    {
        if (lookup.Query != null)
            RecordLookedUp?.Invoke();
    }

    /// <summary>Sets the day's travel directives and draws them into the Rules tab.</summary>
    public void SetDirectives(IReadOnlyList<TravelRuleSO> rules)
    {
        _rules = rules;
        ShowDirectives();
        IndexDay();
    }

    /// <summary>Draws today's directives into the Rules tab (every case does): the memo of the day, its closures linking into the first book once the books are built.</summary>
    public void ShowDirectives()
    {
        foreach (RulesView view in _rulesViews)
            if (view != null)
                view.Show(_rules, _agency, Day, FirstBook);
    }

    /// <summary>The first reference book's category (a directive's place links into it), or null before the books are built.</summary>
    private ClueCategory? FirstBook
    {
        get
        {
            ReferenceBookSO first = _library != null ? _library.ReferenceBooks.FirstOrDefault(b => b != null) : null;
            return first != null ? first.category : (ClueCategory?)null;
        }
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

    /// <summary>Hands the day's citizen registry to the Records tab (its lookup searches the index, which now holds the registry's rows), with the agency block and today's date (<paramref name="day"/> in the agency's calendar) every page prints.</summary>
    public void SetCitizenRegistry(CitizenRegistry registry, AgencyContent agency, int day)
    {
        _registry = registry;
        _agency = agency;
        Day = day;
        IndexDay();
        string today = agency != null ? AgencyCalendar.Today(agency.firstDate, day) : null;
        foreach (RecordsView records in _records)
            if (records != null)
                records.SetRegistry(registry, _index, agency, today);
        foreach (ReferenceView books in _books)
            if (books != null)
                books.SetAgency(agency, day);
        foreach (CalendarView calendar in _calendars)
            if (calendar != null)
                calendar.SetDay(agency, day);
        ShowDirectives();
    }

    /// <summary>The first time only: the Reference tab's books from the library (and their rows into search; the directives' places can link now).</summary>
    public void BuildBooks(ContentLibrarySO lib)
    {
        foreach (ReferenceView books in _books)
            if (books != null)
                books.BuildBooks(lib);
        if (_library == null && lib != null)
        {
            _library = lib;
            IndexDay();
            ShowDirectives();
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

    /// <summary>Search's day layer from what the day has so far: the rules ("Rule n"), the books' rows (titled "book · place"; the country's name and "revised" matched too) and the records' rows (titled as the keys title them, "name · label").</summary>
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
        if (_facts != null && _books.Any(b => b != null))
            day.AddRange(IndexEntries.BookRows(_books.First(b => b != null).Books.Select(b => (b.category, b.displayName)).ToList(), _facts, CountryName,
                                               UiText.Get("search.revised"), UiText.Get("search.title.row")));
        day.AddRange(IndexEntries.Records(_registry, UiText.Get("app.row.record")));
        _index.SetDay(day);
    }

    /// <summary>A nation's name ("Greece"), or null when the library does not know it.</summary>
    private string CountryName(string nationId)
    {
        NationSO nation = _library != null ? _library.GetNationById(nationId) : null;
        return nation != null ? nation.displayName : null;
    }
}
