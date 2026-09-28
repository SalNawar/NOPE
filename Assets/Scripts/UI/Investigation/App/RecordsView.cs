using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Investigation app's Records tab (the PC redesign AP5, FO9, §2.5):
/// Citizen Records. Type a name or an agency number, and LOOK UP (or Enter)
/// opens the agency's record: the search index scoped to Records opens its
/// best hit (redesign phase 19, SE6: one matcher, so search and the lookup
/// find the same records; a whole number or name ranks first). The record
/// is drawn as its Record Extract (Form_RecordExtract, TC-901, on a FormPage
/// at the pane's width): the query line reads the lookup, whether a record
/// is on file and today's date; each group of the record is a numbered
/// section of boxes, two to a row (a value too long for half a row across
/// it); a box that is evidence is compare-clickable, keyed by its record
/// (EvidencePicks.ForRecord), so the traveller's own record can disprove
/// their birth-date tell (RecordMismatch; another person's record proves
/// nothing), and it lights while its key is picked (CM3); the rest are
/// shown only. A day source: it works between travellers. Its place in the
/// pane's history is its lookup (Spot); a smart link runs a lookup and marks
/// the found record's box of its category, scrolled to the middle (Reveal);
/// a lookup the player runs is a move the pane records (Searched); every
/// lookup tells the tab (Looked: the recent items, and the steps
/// checklist's "a record was looked up"). The registry, the agency block and
/// the date are injected per day by GameManager through the façade and
/// DayReference. Each evidence box is marked with its key for the keys, the
/// copy and the pins (AppRow: "Aster Vale · Born"). Each pane has one. Its
/// item is the record looked up ("rec:{id}", IAppItems).
/// </summary>
public sealed class RecordsView : AppView, IAppItems
{
    [Header("Lookup")]
    /// <summary>Where the player types a name or a number.</summary>
    [SerializeField] private TMP_InputField searchInput;

    /// <summary>Runs the lookup (Enter in the field does too).</summary>
    [SerializeField] private Button searchButton;

    [Header("The extract")]
    /// <summary>The Record Extract in its scroll.</summary>
    [SerializeField] private FormPage page;

    /// <summary>The Record Extract's page kind (Form_RecordExtract, TC-901).</summary>
    [SerializeField] private FormSpecSO extractForm;

    /// <summary>The compare the evidence boxes pick into.</summary>
    [SerializeField] private CompareController compareController;

    /// <summary>The one source the lookup searches.</summary>
    private static readonly AppTab[] RecordsOnly = { AppTab.Records };

    private readonly List<(FormSlot slot, Button button)> _armed = new List<(FormSlot, Button)>();
    private CitizenRegistry _registry;
    private CaseIndex _index;
    private AgencyContent _agency;
    private CitizenRecord _current;

    /// <summary>Today's date in the agency's calendar (null when the agency block has no readable first date).</summary>
    private string _today;

    /// <summary>The box a link went to (its pick key; null: none), marked found.</summary>
    private string _foundKey;

    /// <summary>The lookup shown (trimmed; null: none).</summary>
    public string Query { get; private set; }

    /// <summary>Raised when the player runs a lookup (LOOK UP or Enter), not when a link runs one.</summary>
    public event Action Searched;

    /// <summary>Raised after a lookup (a record shown, or none on file).</summary>
    public event Action Looked;

    /// <summary>The record shown, or null.</summary>
    public CitizenRecord Current => _current;

    /// <inheritdoc />
    public override AppTab Tab => AppTab.Records;

    /// <inheritdoc />
    public override LinkTarget Spot => LinkTarget.ToRecords(Query);

    /// <inheritdoc />
    public string ItemKey => _current != null ? EntryKeys.RecordCard(_current.Id) : null;

    /// <inheritdoc />
    public string ItemTitle => _current != null ? UiText.Format("app.item.record", _current.FullName) : null;

    /// <summary>The extract's form (null without a page).</summary>
    private FormView Form => page != null ? page.Form : null;

    private void Awake()
    {
        if (searchButton != null)
            searchButton.onClick.AddListener(Search);
        if (searchInput != null)
            searchInput.onSubmit.AddListener(_ => Search());
        if (Form != null)
        {
            Form.SlotClicked += Pick;
            page.Redrawn += MarkRows;
        }
    }

    private void OnDestroy()
    {
        if (Form != null)
        {
            Form.SlotClicked -= Pick;
            page.Redrawn -= MarkRows;
        }
    }

    /// <summary>Sets the day's registry, the search index holding its rows (the lookup's matcher), the agency block the extract prints and today's date, and resets the view.</summary>
    public void SetRegistry(CitizenRegistry registry, CaseIndex index, AgencyContent agency, string today)
    {
        _registry = registry;
        _index = index;
        _agency = agency;
        _today = today;
        ShowIdle();
    }

    /// <summary>The player's lookup: the typed name or number (Searched tells the pane).</summary>
    public void Search()
    {
        Look(searchInput != null ? searchInput.text : null);
        Searched?.Invoke();
    }

    /// <inheritdoc />
    public override bool Reveal(LinkTarget target) => Reveal(target.Query, target.RecordRow);

    /// <summary>
    /// A smart link's, Back's or a jump's lookup: <paramref name="query"/>
    /// typed and run (null keeps the lookup shown), then the found record's
    /// box of <paramref name="row"/> outlined and scrolled to the middle
    /// (null: the record's top, nothing marked). False when the lookup finds
    /// no record, or the record has no such row.
    /// </summary>
    public bool Reveal(string query, ClueCategory? row)
    {
        if (query != null)
        {
            if (searchInput != null)
                searchInput.SetTextWithoutNotify(query);
            Look(query);
        }

        int flat = row.HasValue ? RecordExtractPage.RowOf(_current, row.Value) : -1;
        _foundKey = flat >= 0 ? PickKeys.Record(row.Value, _current.Id) : null;
        if (page != null)
        {
            if (flat >= 0)
                page.Reveal(SlotOfRow(flat));
            else if (query != null)
            {
                page.Reveal(-1);
                page.ScrollToTop();
            }
            else
                page.Reveal(-1);
        }
        return (query == null || _current != null) && (!row.HasValue || flat >= 0);
    }

    /// <summary>Looks up a name or number (the search index scoped to Records, its best hit: SE6, one matcher) and draws the record's extract (or says none is on file).</summary>
    private void Look(string query)
    {
        Query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        _foundKey = null;
        IReadOnlyList<ResultGroup> found = _index != null ? _index.Search(SearchQuery.Parse(query), RecordsOnly, 1, AppTab.Records) : null;
        Show(found != null && found.Count > 0 ? RecordAt(found[0].Hits[0].Entry.Item) : null);
    }

    /// <summary>The registry's record at <paramref name="index"/>, or null.</summary>
    private CitizenRecord RecordAt(int index) =>
        _registry != null && index >= 0 && index < _registry.Records.Count ? _registry.Records[index] : null;

    /// <summary>Draws <paramref name="record"/>'s extract (null: none on file) under the query line, from the top, and tells the tab.</summary>
    private void Show(CitizenRecord record)
    {
        _current = record;
        Draw();
        Looked?.Invoke();
    }

    /// <summary>No lookup: the extract shows its idle line alone.</summary>
    private void ShowIdle()
    {
        _current = null;
        Query = null;
        _foundKey = null;
        if (searchInput != null)
            searchInput.text = string.Empty;
        Draw();
    }

    /// <summary>The Record Extract: the query line, the record's groups as sections of boxes (the evidence ones pickable, lit by their keys), the found box outlined.</summary>
    private void Draw()
    {
        if (page == null || Form == null || extractForm == null)
            return;
        FormData data = extractForm.Page(_agency);
        data.Text = new Dictionary<string, string>
        {
            {
                RecordExtractPage.QuerySlot,
                Query == null ? UiText.Get("records.idle")
                : _current != null ? UiText.Format("records.onFile", Query, _today ?? string.Empty)
                : UiText.Format("records.noRecord", Query, _today ?? string.Empty)
            }
        };
        var groups = new List<FormGroup>();
        if (_current != null)
            foreach (RecordGroup group in _current.Groups)
            {
                var rows = new List<(string Label, string Value)>();
                foreach (RecordRow row in group.Rows)
                    rows.Add((row.Label, row.Value));
                groups.Add(new FormGroup(group.Title, rows));
            }
        data.Groups = groups;
        Form.Bind(compareController, slot => TryRow(slot, out RecordRow row) ? PickKeys.Record(row.Category, _current.Id) : null);
        page.Show(extractForm.form, data, slot => TryRow(slot, out RecordRow row) && RecordExtractPage.IsPickable(row) && compareController != null);
        MarkRows();
    }

    /// <summary>The record's row a slot shows (a box of the groups' block), false for any other slot.</summary>
    private bool TryRow(FormSlot slot, out RecordRow row)
    {
        row = default;
        return slot.Source == RecordExtractPage.GroupsSlot && RecordExtractPage.TryRow(_current, slot.Row, out row);
    }

    /// <summary>The slot of the record's row <paramref name="flat"/> (a box of the groups' block), or -1.</summary>
    private int SlotOfRow(int flat)
    {
        PlacedForm placed = page != null ? page.Placed : null;
        if (placed != null)
            for (int s = 0; s < placed.Slots.Count; s++)
                if (placed.Slots[s].Source == RecordExtractPage.GroupsSlot && placed.Slots[s].Row == flat)
                    return s;
        return -1;
    }

    /// <summary>Marks each evidence box with its key and title ("Aster Vale · Born") for the keys, the copy and the pins, and outlines the box a link went to; again after every redraw.</summary>
    private void MarkRows()
    {
        if (Form == null)
            return;
        Form.ArmedSlots(_armed);
        int found = -1;
        foreach ((FormSlot slot, Button button) in _armed)
        {
            if (!TryRow(slot, out RecordRow row))
                continue;
            string key = PickKeys.Record(row.Category, _current.Id);
            AppRow.Mark(button.gameObject, AppTab.Records, key, UiText.Format("app.row.record", _current.FullName, row.Label), row.Label, row.Value, button);
            if (key == _foundKey)
                found = slot.Index;
        }
        Form.MarkFound(found);
    }

    /// <summary>A box was clicked: its row goes into the compare (the box lights by its key).</summary>
    private void Pick(FormSlot slot)
    {
        if (compareController != null && TryRow(slot, out RecordRow row) && RecordExtractPage.IsPickable(row))
            compareController.Select(EvidencePicks.ForRecord(_current, row), null);
    }
}
