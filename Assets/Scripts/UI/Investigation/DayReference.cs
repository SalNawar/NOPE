using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// The investigation's day reference (the PC redesign RF1): today's travel
/// directives (the Directives window's text, rewritten on every case), facts
/// (the reference books' rows) and citizen registry (handed to the Citizen
/// Records app), set once a day by GameManager through the façade, and the shelf of
/// reference-book windows, built from the library on the first case with a
/// desktop tile each, in two staggered rows of three. Plain C#;
/// InvestigationUIController owns it.
/// </summary>
public sealed class DayReference
{
    /// <summary>Where the book windows go and how they are laid out (the façade's serialized knobs).</summary>
    public struct BookShelf
    {
        /// <summary>The book window template (inactive).</summary>
        public ReferenceBookWindowController template;

        /// <summary>The window layer the book windows open on.</summary>
        public RectTransform windowLayer;

        /// <summary>Where the first book window opens.</summary>
        public Vector2 origin;

        /// <summary>The horizontal step between the three book windows of a row.</summary>
        public float columnStep;

        /// <summary>The offset from one row of book windows to the next.</summary>
        public Vector2 rowStep;
    }

    private readonly TMP_Text _directivesText;
    private readonly CitizenRecordsWindowController _records;
    private readonly CompareController _compare;
    private readonly DesktopTiles _tiles;
    private readonly BookShelf _shelf;

    private string _directives = string.Empty;
    private FactTable _facts;
    private bool _booksBuilt;

    /// <summary>The Directives window's text, the Citizen Records app, the compare (the book rows pick into it), the desktop's tiles and the book shelf's parts; any may be missing.</summary>
    public DayReference(TMP_Text directivesText, CitizenRecordsWindowController records, CompareController compare, DesktopTiles tiles, BookShelf shelf)
    {
        _directivesText = directivesText;
        _records = records;
        _compare = compare;
        _tiles = tiles;
        _shelf = shelf;
    }

    /// <summary>Sets the day's travel directives and writes them into the Directives window.</summary>
    public void SetDirectives(IReadOnlyList<TravelRuleSO> rules)
    {
        _directives = BuildDirectives(rules);
        ShowDirectives();
    }

    /// <summary>Writes today's directives into the Directives window (every case does).</summary>
    public void ShowDirectives()
    {
        if (_directivesText != null)
            _directivesText.text = _directives;
    }

    /// <summary>Sets today's facts (the reference books render these rows).</summary>
    public void SetFacts(FactTable facts) => _facts = facts;

    /// <summary>Hands the day's citizen registry to the Records app, with the agency block and today's date (<paramref name="day"/> in the agency's calendar) its extract prints.</summary>
    public void SetCitizenRegistry(CitizenRegistry registry, AgencyContent agency, int day)
    {
        if (_records != null)
            _records.SetRegistry(registry, agency, agency != null ? AgencyCalendar.Today(agency.firstDate, day) : null);
    }

    /// <summary>The first time only: one window per reference book of the library (hidden), each with a desktop tile.</summary>
    public void BuildBookShelf(ContentLibrarySO lib)
    {
        if (_booksBuilt)
            return;

        _booksBuilt = true;

        if (lib == null || _shelf.template == null || !_tiles.Ready || _shelf.windowLayer == null)
            return;

        int i = 0;
        foreach (ReferenceBookSO book in lib.ReferenceBooks)
        {
            if (book == null)
                continue;

            ReferenceBookWindowController win = Object.Instantiate(_shelf.template, _shelf.windowLayer);
            win.SetBook(book, _facts, _compare);
            if (win.transform is RectTransform rt)
                rt.anchoredPosition = _shelf.origin + new Vector2((i % 3) * _shelf.columnStep, 0f) + (i / 3) * _shelf.rowStep;
            win.gameObject.SetActive(false);

            if (win.TryGetComponent(out DesktopWindow chrome))
                _tiles.Add(book.displayName, chrome, false);
            i++;
        }
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
