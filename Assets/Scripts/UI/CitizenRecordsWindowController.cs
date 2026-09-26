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
/// date. A search result jumps to a record's row (Reveal). The
/// record's rows are listed generically, group by group (a group's title is a
/// heading line), a page at a time (PagedRowsWindow). A row that is evidence
/// is compare-clickable, keyed by its record (EvidencePicks.ForRecord), so
/// the traveller's own record can disprove their birth-date tell
/// (RecordMismatch; another person's record proves nothing). The registry,
/// the agency block and the date are injected per day by GameManager via
/// InvestigationUIController.
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

    /// <summary>Looks up the typed name or number (the index scoped to Records, its best hit) and lists the record's rows (or says none is on file).</summary>
    public void Search()
    {
        string query = searchInput != null ? searchInput.text : null;
        IReadOnlyList<ResultGroup> found = _index != null ? _index.Search(SearchQuery.Parse(query), RecordsOnly, 1, AppTab.Records) : null;
        Show(found != null && found.Count > 0 ? RecordAt(found[0].Hits[0].Entry.Item) : null, query);
    }

    /// <summary>
    /// A search result (redesign phase 19, SE4): record <paramref name="record"/>
    /// of the registry shown as its lookup by id would show it (the query its
    /// number, else its name), on the page of its row <paramref name="row"/>
    /// (in order across its groups; -1: its first row), which is returned as found.
    /// </summary>
    public FoundTarget Reveal(int record, int row)
    {
        CitizenRecord found = RecordAt(record);
        if (found == null)
            return default;
        if (searchInput != null)
            searchInput.SetTextWithoutNotify(found.Id);
        Show(found, found.Id);
        int rows = -1;
        for (int i = 0; i < _lines.Count; i++)
            if (_lines[i].Heading == null && ++rows == System.Math.Max(0, row))
                return ShowRowOf(i);
        return default;
    }

    /// <summary>The registry's record at <paramref name="index"/>, or null.</summary>
    private CitizenRecord RecordAt(int index) =>
        _registry != null && index >= 0 && index < _registry.Records.Count ? _registry.Records[index] : null;

    /// <summary>Lists <paramref name="record"/>'s rows (null: none on file) under the status line for <paramref name="query"/>, from the first page.</summary>
    private void Show(CitizenRecord record, string query)
    {
        _current = record;
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
    }

    private void ShowIdle()
    {
        _current = null;
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
        if (button == null)
            return;
        button.enabled = pickable;
        if (!pickable)
            return;

        CitizenRecord record = _current;
        RecordRow picked = line.Row;
        button.onClick.AddListener(() => compareController.Select(EvidencePicks.ForRecord(record, picked), new ImageHighlight(background)));
    }
}
