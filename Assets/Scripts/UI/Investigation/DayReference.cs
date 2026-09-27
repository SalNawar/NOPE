using System.Collections.Generic;
using System.Text;
using TMPro;

/// <summary>
/// The investigation's day reference (the PC redesign RF1): today's travel
/// directives (the app's Rules tab text), facts (the Reference tab's
/// registers) and citizen registry (the Records tab), set once a day by
/// GameManager through the façade, and the reference books, built from the
/// library into the Reference tab on the first case. A case's claim puts its
/// row first in every register. Day sources: they work between travellers.
/// Plain C#; InvestigationUIController owns it.
/// </summary>
public sealed class DayReference
{
    private readonly TMP_Text _directivesText;
    private readonly CitizenRecordsWindowController _records;
    private readonly CompareController _compare;
    private readonly ReferenceView _books;

    private string _directives = string.Empty;

    /// <summary>Today's day number (set with the day's registry; the steps checklist lists a step from its first day).</summary>
    public int Day { get; private set; } = 1;

    /// <summary>The Rules tab's text, the Records tab's lookup, the compare (the book rows pick into it) and the Reference tab; any may be missing.</summary>
    public DayReference(TMP_Text directivesText, CitizenRecordsWindowController records, CompareController compare, ReferenceView books)
    {
        _directivesText = directivesText;
        _records = records;
        _compare = compare;
        _books = books;
    }

    /// <summary>Sets the day's travel directives and writes them into the Rules tab.</summary>
    public void SetDirectives(IReadOnlyList<TravelRuleSO> rules)
    {
        _directives = BuildDirectives(rules);
        ShowDirectives();
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
        if (_books != null)
            _books.SetFacts(facts, _compare);
    }

    /// <summary>Hands the day's citizen registry to the Records tab, with the agency block and today's date (<paramref name="day"/> in the agency's calendar) its extract prints.</summary>
    public void SetCitizenRegistry(CitizenRegistry registry, AgencyContent agency, int day)
    {
        Day = day;
        if (_records != null)
            _records.SetRegistry(registry, agency, agency != null ? AgencyCalendar.Today(agency.firstDate, day) : null);
    }

    /// <summary>The first time only: the Reference tab's books from the library.</summary>
    public void BuildBooks(ContentLibrarySO lib)
    {
        if (_books != null)
            _books.BuildBooks(lib);
    }

    /// <summary>A new case's claim: its row first in every register, "Claimed place only" on (AP8).</summary>
    public void SetClaim(CaseInstance inst)
    {
        if (_books != null)
            _books.SetClaim(inst != null && inst.claimedNation != null ? inst.claimedNation.id : null,
                            inst != null && inst.claimedEra != null ? inst.claimedEra.id : null);
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
