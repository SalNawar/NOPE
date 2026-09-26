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
/// Each tab has one view per pane of the app, and each is filled. Plain C#;
/// InvestigationUIController owns it.
/// </summary>
public sealed class DayReference
{
    private readonly IReadOnlyList<TMP_Text> _directivesTexts;
    private readonly IReadOnlyList<CitizenRecordsWindowController> _records;
    private readonly CompareController _compare;
    private readonly IReadOnlyList<ReferenceView> _books;

    private string _directives = string.Empty;

    /// <summary>The Rules tabs' texts, the Records tabs' lookups, the compare (the book rows pick into it) and the Reference tabs (one each per pane; null entries are skipped).</summary>
    public DayReference(IReadOnlyList<TMP_Text> directivesTexts, IReadOnlyList<CitizenRecordsWindowController> records, CompareController compare,
                        IReadOnlyList<ReferenceView> books)
    {
        _directivesTexts = directivesTexts ?? System.Array.Empty<TMP_Text>();
        _records = records ?? System.Array.Empty<CitizenRecordsWindowController>();
        _compare = compare;
        _books = books ?? System.Array.Empty<ReferenceView>();
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
        foreach (TMP_Text text in _directivesTexts)
            if (text != null)
                text.text = _directives;
    }

    /// <summary>Sets today's facts (the Reference tab's registers render these rows).</summary>
    public void SetFacts(FactTable facts)
    {
        foreach (ReferenceView books in _books)
            if (books != null)
                books.SetFacts(facts, _compare);
    }

    /// <summary>Hands the day's citizen registry to the Records tab, with the agency block and today's date (<paramref name="day"/> in the agency's calendar) its extract prints.</summary>
    public void SetCitizenRegistry(CitizenRegistry registry, AgencyContent agency, int day)
    {
        string today = agency != null ? AgencyCalendar.Today(agency.firstDate, day) : null;
        foreach (CitizenRecordsWindowController records in _records)
            if (records != null)
                records.SetRegistry(registry, agency, today);
    }

    /// <summary>The first time only: the Reference tab's books from the library.</summary>
    public void BuildBooks(ContentLibrarySO lib)
    {
        foreach (ReferenceView books in _books)
            if (books != null)
                books.BuildBooks(lib);
    }

    /// <summary>A new case's claim: its row first in every register, "Claimed place only" on (AP8).</summary>
    public void SetClaim(CaseInstance inst)
    {
        CaseClaim claim = AppLinks.Claim(inst);
        foreach (ReferenceView books in _books)
            if (books != null)
                books.SetClaim(claim.NationId, claim.EraId);
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
