using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Citizen Records desktop app (redesign phase 2): under the agency's
/// printed name and programme, type a name or an agency number to get the
/// agency's record: the search index scoped to Records opens its best hit
/// (redesign phase 19, the PC spec's SE6: one matcher, so search and the
/// lookup find the same records; a whole number or name ranks first); the
/// status line reads the query, whether a record is on file and today's
/// date. A search result, a link or Back shows a record's row (Reveal). The
/// record's rows are listed generically, group by group (a group's title is a
/// heading line), a page at a time (PagedRowsWindow). A row that is evidence
/// is compare-clickable, keyed by its record (EvidencePicks.ForRecord), so
/// the traveller's own record can disprove their birth-date tell
/// (RecordMismatch; another person's record proves nothing); it lights while
/// its key is picked (AppRow). A smart link runs a lookup here and marks the
/// found record's row of its category (Reveal); a lookup the player runs is
/// announced (Searched) for the pane's history and the steps checklist's "a
/// record was looked up". The registry, the agency
/// block and the date are injected per day by GameManager via
/// InvestigationUIController. Each evidence row is marked with its key for
/// the keys, the copy and the pins (AppRow: "Aster Vale · Born"); a lookup
/// tells the Records tab (Looked), and a jump shows a record and its row
/// (Show).
/// </summary>
public sealed class CitizenRecordsWindowController : PagedRowsWindow
{
    [Header("Lookup")]
    /// <summary>Where the player types a name or a number.</summary>
    [SerializeField] private TMP_InputField searchInput;

    /// <summary>Runs the lookup (Enter in the field does too).</summary>
    [SerializeField] private Button searchButton;

    /// <summary>The status line: the idle hint, or the query's result and today's date.</summary>
    [SerializeField] private TMP_Text statusText;

    /// <summary>The agency's printed name and programme line over the lookup.</summary>
    [SerializeField] private TMP_Text agencyText;

    [Header("Compare")]
    [SerializeField] private CompareController compareController;

    /// <summary>One listed line of the shown record: a group's heading, or one of its rows.</summary>
    private readonly struct Line
    {
        public Line(string heading)
        {
            Heading = heading;
            Row = default;
        }

        public Line(RecordRow row)
        {
            Heading = null;
            Row = row;
        }

        /// <summary>The group's title for a heading line; null for a row.</summary>
        public string Heading { get; }

        /// <summary>The row of a row line.</summary>
        public RecordRow Row { get; }
    }

    /// <summary>The one source the lookup searches.</summary>
    private static readonly AppTab[] RecordsOnly = { AppTab.Records };

    private CitizenRegistry _registry;
    private CaseIndex _index;
    private CitizenRecord _current;

    /// <summary>Today's date in the agency's calendar (null when the agency block has no readable first date).</summary>
    private string _today;

    /// <summary>The shown record's lines, in order (empty when none is shown).</summary>
    private readonly List<Line> _lines = new();

    /// <summary>The row a link went to (its pick key; null: none), marked found.</summary>
    private string _foundKey;

    /// <summary>The lookup shown (trimmed; null: none).</summary>
    public string Query { get; private set; }

    /// <summary>Raised when the player runs a lookup of a name or number (SEARCH or Enter), whatever it found; not when a link runs one.</summary>
    public event System.Action Searched;

    /// <summary>The record shown, or null.</summary>
    public CitizenRecord Current => _current;

    /// <summary>Raised after a lookup (a record shown, or none on file).</summary>
    public event System.Action Looked;

    /// <inheritdoc />
    protected override void Awake()
    {
        base.Awake();

        if (searchButton != null)
            searchButton.onClick.AddListener(Search);

        if (searchInput != null)
            searchInput.onSubmit.AddListener(_ => Search());

        ShowIdle();
    }

    /// <summary>Sets the day's registry, the search index holding its rows (the lookup's matcher), the agency block the window prints and today's date, and resets the view.</summary>
    public void SetRegistry(CitizenRegistry registry, CaseIndex index, AgencyContent agency, string today)
    {
        _registry = registry;
        _index = index;
        _today = today;
        if (agencyText != null)
            agencyText.text = agency != null ? UiText.Format("records.agency", agency.name, agency.programme) : string.Empty;
        ShowIdle();
    }

    /// <summary>The player's lookup: the typed name or number (Searched tells the pane).</summary>
    public void Search()
    {
        string query = searchInput != null ? searchInput.text : null;
        Look(query);
        if (!string.IsNullOrWhiteSpace(query))
            Searched?.Invoke();
    }

    /// <summary>
    /// A smart link's or Back's lookup: <paramref name="query"/> typed and run
    /// (null keeps the lookup shown), then the found record's row of
    /// <paramref name="row"/> shown and marked found (null: the record's top,
    /// nothing marked).
    /// </summary>
    public void Reveal(string query, ClueCategory? row)
    {
        if (query != null)
        {
            if (searchInput != null)
                searchInput.SetTextWithoutNotify(query);
            Look(query);
        }

        int index = -1;
        if (row.HasValue && _current != null)
            for (int i = 0; i < _lines.Count && index < 0; i++)
                if (_lines[i].Heading == null && _lines[i].Row.IsEvidence && _lines[i].Row.Category == row.Value)
                    index = i;
        _foundKey = index >= 0 ? PickKeys.Record(row.Value, _current.Id) : null;
        if (index >= 0)
            ShowPageOf(index);
        else
            ShowPage(query != null ? 0 : Page);
    }

    /// <summary>Looks up a name or number and lists the record's rows (or says none is on file), from the first page.</summary>
    private void Look(string query)
    {
        Query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        _foundKey = null;
        _current = Find(query);
        _lines.Clear();
        if (_current != null)
            foreach (RecordGroup group in _current.Groups)
            {
                if (!string.IsNullOrWhiteSpace(group.Title))
                    _lines.Add(new Line(group.Title));
                foreach (RecordRow row in group.Rows)
                    _lines.Add(new Line(row));
            }

        if (statusText != null)
            statusText.text = string.IsNullOrWhiteSpace(query) ? UiText.Get("records.idle")
                : _current != null ? UiText.Format("records.onFile", query.Trim(), _today ?? string.Empty)
                : UiText.Format("records.noRecord", query.Trim(), _today ?? string.Empty);

        ShowPage(0);
        Looked?.Invoke();
    }

    /// <summary>The lookup's record: the search index scoped to Records, its best hit (SE6: search and the lookup find the same records); null when none matches.</summary>
    private CitizenRecord Find(string query)
    {
        IReadOnlyList<ResultGroup> found = _index != null ? _index.Search(SearchQuery.Parse(query), RecordsOnly, 1, AppTab.Records) : null;
        return found != null && found.Count > 0 ? RecordAt(found[0].Hits[0].Entry.Item) : null;
    }

    /// <summary>The registry's record at <paramref name="index"/>, or null.</summary>
    private CitizenRecord RecordAt(int index) =>
        _registry != null && index >= 0 && index < _registry.Records.Count ? _registry.Records[index] : null;

    /// <summary>A jump: looks up <paramref name="recordId"/> (its number, else its name) and shows the page of its row keyed <paramref name="rowKey"/> (null: the first page).</summary>
    public void Show(string recordId, string rowKey)
    {
        if (searchInput != null)
            searchInput.text = recordId ?? string.Empty;
        Search();
        if (rowKey == null || _current == null)
            return;
        for (int i = 0; i < _lines.Count; i++)
            if (_lines[i].Heading == null && _lines[i].Row.IsEvidence && PickKeys.Record(_lines[i].Row.Category, _current.Id) == rowKey)
            {
                ShowPageOf(i);
                return;
            }
    }

    private void ShowIdle()
    {
        _current = null;
        Query = null;
        _foundKey = null;
        _lines.Clear();
        if (statusText != null)
            statusText.text = UiText.Get("records.idle");
        if (searchInput != null)
            searchInput.text = string.Empty;
        ShowPage(0);
    }

    /// <inheritdoc />
    protected override int RowCount => _lines.Count;

    /// <summary>A heading line shows its group's title with no background and no click; a row line its label and value, compare-clickable only when it is evidence.</summary>
    protected override void FillRow(int index, GameObject row, TMP_Text[] texts, Image background, Button button)
    {
        Line line = _lines[index];
        bool heading = line.Heading != null;

        if (texts.Length > 0 && texts[0] != null)
        {
            texts[0].text = heading ? line.Heading : line.Row.Label;
            texts[0].fontStyle = heading ? FontStyles.Bold : FontStyles.Normal;
        }
        if (texts.Length > 1 && texts[1] != null)
            texts[1].text = heading ? string.Empty : line.Row.Value;

        if (background != null)
            background.enabled = !heading;

        bool pickable = !heading && line.Row.IsEvidence && !string.IsNullOrEmpty(line.Row.Value) && compareController != null;
        if (!heading && line.Row.IsEvidence && _current != null)
            AppRow.Mark(row, AppTab.Records, PickKeys.Record(line.Row.Category, _current.Id), UiText.Format("app.row.record", _current.FullName, line.Row.Label),
                        texts.Length > 0 ? texts[0] : null, texts.Length > 1 ? texts[1] : null, pickable ? button : null);
        if (button == null)
            return;
        button.enabled = pickable;
        if (!pickable)
            return;

        CitizenRecord record = _current;
        RecordRow picked = line.Row;
        ComparePick pick = EvidencePicks.ForRecord(record, picked);
        AppRow mark = row.GetComponent<AppRow>();
        if (mark != null)
        {
            mark.Bind(compareController, pick.Key);
            mark.SetFound(pick.Key == _foundKey);
        }
        button.onClick.AddListener(() => compareController.Select(pick, null));
    }
}
