using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Citizen Records desktop app (redesign phase 2): under the agency's
/// printed name and programme, type a name or an agency number to get the
/// agency's record (CitizenRegistry.Find: a whole number first); the status
/// line reads the query, whether a record is on file and today's date. The
/// record's rows are listed generically, group by group (a group's title is a
/// heading line), a page at a time (PagedRowsWindow). A row that is evidence
/// is compare-clickable, keyed by its record (EvidencePicks.ForRecord), so
/// the traveller's own record can disprove their birth-date tell
/// (RecordMismatch; another person's record proves nothing). The registry,
/// the agency block and the date are injected per day by GameManager via
/// InvestigationUIController. Each lookup is announced (Searched: the steps
/// checklist's "a record was looked up"), and a step's jump looks a record up
/// for the player (Lookup).
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

    private CitizenRegistry _registry;
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

    /// <summary>Sets the day's registry, the agency block the window prints and today's date, and resets the view.</summary>
    public void SetRegistry(CitizenRegistry registry, AgencyContent agency, string today)
    {
        _registry = registry;
        _today = today;
        if (agencyText != null)
            agencyText.text = agency != null ? UiText.Format("records.agency", agency.name, agency.programme) : string.Empty;
        ShowIdle();
    }

    /// <summary>Raised after each lookup of a name or number, whatever it found.</summary>
    public event Action Searched;

    /// <summary>Types <paramref name="query"/> into the lookup and runs it (a step's jump: the primary paper's record).</summary>
    public void Lookup(string query)
    {
        if (searchInput != null)
            searchInput.SetTextWithoutNotify(query ?? string.Empty);
        Search();
    }

    /// <summary>Looks up the typed name or number and lists the record's rows (or says none is on file).</summary>
    public void Search()
    {
        string query = searchInput != null ? searchInput.text : null;
        _current = _registry != null ? _registry.Find(query) : null;

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
        if (!string.IsNullOrWhiteSpace(query))
            Searched?.Invoke();
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
