using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Investigation app's Transcript tab (the PC redesign AP5, AP7, FO9,
/// §2.7): the current traveller's interview as its Interview Record
/// (Form_InterviewRecord, TC-920, on a FormPage at the pane's width): the
/// head line (the traveller, the desk officer, the day), then a table of NO.,
/// SPEAKER and STATEMENT, one row per line of the runner's live, append-only
/// transcript. An answer's row is marked in NO. and is compare-clickable (the
/// same pick as the bubble's answer: EvidencePicks.ForAnswer, keyed by the
/// line's index), lit while its key is picked (CM3); an answer with a smart
/// link (SmartLinks.ForAnswer) has a ↗ that follows it through the pane. The
/// other rows are read only. "Answers only" hides them. The table follows the
/// newest line while the view is at the bottom; scrolled up, a "New line"
/// pill shows instead and a click on it goes to the bottom. Statements show
/// through DisplayText: from translation's first day the traveller's lines are
/// in their claimed place's tongue and show in English only with the region's
/// Speech translator (settled, never animated; TextFlip.Write sets the
/// script's font on the printed cell); an untranslated answer shows in the
/// dock as the placeholder, while its evidence stays the canonical value. A
/// case source: between travellers the pane shows the no-case state. A new
/// line badges the tab; nothing opens it. A dock side reveals its line
/// (Reveal: "Answers only" lifted when it hides the line, the row outlined
/// and scrolled to the middle). Each row is marked with its line key for the
/// keys, the copy and the pins (AppRow: "Nikias · line 7"; an untranslated
/// line keeps its tongue, so its copy is a foreign clip). Each pane has one;
/// InterviewPresenter fills them all. It has no items (IAppItems: nothing
/// to pin as an item).
/// </summary>
public sealed class TranscriptView : AppView, IAppItems
{
    /// <summary>The Interview Record in its scroll.</summary>
    [SerializeField] private FormPage page;

    /// <summary>The Interview Record's page kind (Form_InterviewRecord, TC-920).</summary>
    [SerializeField] private FormSpecSO interviewForm;

    /// <summary>"Answers only": on, the desk's lines and the traveller's other lines are hidden.</summary>
    [SerializeField] private Toggle answersOnly;

    /// <summary>"New line": shown when a line arrives while the player has scrolled up; a click goes to the bottom.</summary>
    [SerializeField] private Button newLineButton;

    private readonly List<(FormSlot slot, Button button)> _armed = new List<(FormSlot, Button)>();
    private readonly List<TextMeshProUGUI> _texts = new List<TextMeshProUGUI>();
    private readonly List<int> _lineOfRow = new List<int>();
    private IReadOnlyList<DialogLine> _lines = System.Array.Empty<DialogLine>();
    private string _deskName = string.Empty;
    private string _travellerName = string.Empty;
    private CompareController _compare;
    private CaseTranslation _translation = CaseTranslation.None;
    private AgencyContent _agency;
    private int _day = 1;

    /// <summary>The case's claim and record lookup (the answers' links).</summary>
    private CaseClaim _claim;
    private string _caseLookup;

    /// <summary>The line a link went to (its pick key; null: none), marked found.</summary>
    private string _foundKey;

    /// <inheritdoc />
    public override AppTab Tab => AppTab.Transcript;

    /// <inheritdoc />
    public string ItemKey => null;

    /// <inheritdoc />
    public string ItemTitle => null;

    /// <summary>The record's form (null without a page).</summary>
    private FormView Form => page != null ? page.Form : null;

    private void Awake()
    {
        if (Form != null)
        {
            Form.SlotClicked += Pick;
            Form.LinkClicked += Follow;
            page.Redrawn += MarkRows;
        }
        if (answersOnly != null)
            answersOnly.onValueChanged.AddListener(_ => Draw(page != null && page.AtBottom));
        if (newLineButton != null)
        {
            newLineButton.onClick.AddListener(() =>
            {
                if (page != null)
                    page.ScrollToBottom();
                ShowNewLine(false);
            });
            ShowNewLine(false);
        }
        if (page != null)
            page.Scrolled += () =>
            {
                if (page.AtBottom)
                    ShowNewLine(false);
            };
    }

    private void OnDestroy()
    {
        if (Form != null)
        {
            Form.SlotClicked -= Pick;
            Form.LinkClicked -= Follow;
            page.Redrawn -= MarkRows;
        }
    }

    /// <summary>
    /// Shows a traveller's transcript (the runner's live, append-only list)
    /// from its newest line, in their translation, headed with
    /// <paramref name="agency"/>'s block and today's <paramref name="day"/>;
    /// the answers link by the case's <paramref name="claim"/> and record
    /// lookup (SmartLinks.CaseLookup).
    /// </summary>
    public void Bind(IReadOnlyList<DialogLine> transcript, string deskName, string travellerName, CompareController compare, CaseTranslation translation,
                     CaseClaim claim, string caseLookup, AgencyContent agency, int day)
    {
        _lines = transcript ?? System.Array.Empty<DialogLine>();
        _deskName = deskName ?? string.Empty;
        _travellerName = travellerName ?? string.Empty;
        _compare = compare;
        _translation = translation ?? CaseTranslation.None;
        _claim = claim;
        _caseLookup = caseLookup;
        _agency = agency;
        _day = day;
        _foundKey = null;
        Draw(true);
    }

    /// <summary>Lines were appended: the table follows them when the view is at the bottom, else the "New line" pill shows.</summary>
    public void Refresh()
    {
        bool follow = page == null || page.AtBottom;
        Draw(follow);
        if (!follow)
            ShowNewLine(true);
    }

    /// <inheritdoc />
    public override bool Reveal(LinkTarget target) => Reveal(target.Key);

    /// <summary>Lifts "Answers only" when it hides the line <paramref name="lineKey"/> names (a Line pick key), outlines the line's row and scrolls it to the middle; any other key (or null) only clears the mark. False for a line the transcript does not have.</summary>
    public bool Reveal(string lineKey)
    {
        bool mine = PickKeys.TryLine(lineKey, out int index) && index < _lines.Count;
        _foundKey = mine ? lineKey : null;
        if (mine && InterviewPage.RowOf(_lineOfRow, index) < 0 && answersOnly != null && answersOnly.isOn)
            answersOnly.isOn = false;
        if (page != null)
            page.Reveal(mine ? SlotOfRow(InterviewPage.RowOf(_lineOfRow, index)) : -1);
        return mine || lineKey == null;
    }

    /// <summary>The Interview Record from the lines (the answers alone with the toggle), the answers pickable and linked, the foreign lines in their script, the found row outlined; scrolled to the bottom when <paramref name="follow"/>.</summary>
    private void Draw(bool follow)
    {
        if (page == null || Form == null || interviewForm == null)
            return;
        FormData data = interviewForm.Page(_agency);
        string date = _agency != null ? AgencyCalendar.Today(_agency.firstDate, _day) : null;
        data.Text = new Dictionary<string, string> { { InterviewPage.HeadSlot, UiText.Format("form.interview.head", _travellerName, _deskName, date ?? string.Empty) } };
        data.Rows = new Dictionary<string, IReadOnlyList<string[]>>
        {
            { InterviewPage.RowsSlot, InterviewPage.Rows(_lines, answersOnly != null && answersOnly.isOn, _deskName, _travellerName, Shown, UiText.Get("form.interview.answerMark"), _lineOfRow) }
        };
        Form.Bind(_compare, slot => TryLine(slot, out int line) && _lines[line].IsAnswer ? PickKeys.Line(line) : null);
        page.Show(interviewForm.form, data, slot => TryLine(slot, out _), LinkHint);
        MarkRows();
        if (follow)
        {
            page.ScrollToBottom();
            ShowNewLine(false);
        }
    }

    /// <summary>A line as the record shows it (DisplayText: English, or its tongue's glyphs with the key words in English).</summary>
    private string Shown(DialogLine line)
    {
        SpeechTranslation speech = _translation.Speech;
        return DisplayText.For(line.Text, _translation.Line(line), speech.Timing, speech.ReducedMotion);
    }

    /// <summary>The transcript line a slot's row shows; false for any other slot.</summary>
    private bool TryLine(FormSlot slot, out int line)
    {
        line = slot.Source == InterviewPage.RowsSlot && slot.Row >= 0 && slot.Row < _lineOfRow.Count ? _lineOfRow[slot.Row] : -1;
        return line >= 0 && line < _lines.Count;
    }

    /// <summary>The slot of table row <paramref name="row"/>, or -1.</summary>
    private int SlotOfRow(int row)
    {
        PlacedForm placed = row >= 0 && page != null ? page.Placed : null;
        if (placed != null)
            for (int s = 0; s < placed.Slots.Count; s++)
                if (placed.Slots[s].Source == InterviewPage.RowsSlot && placed.Slots[s].Row == row)
                    return s;
        return -1;
    }

    /// <summary>An answer's link (SmartLinks.ForAnswer); None for any other row.</summary>
    private LinkTarget Link(FormSlot slot) =>
        TryLine(slot, out int line) && _lines[line].IsAnswer ? SmartLinks.ForAnswer(_lines[line].Category, _claim, _caseLookup) : LinkTarget.None;

    /// <summary>The ↗'s hover hint of an answer with a link, or null (no ↗).</summary>
    private string LinkHint(FormSlot slot)
    {
        LinkTarget link = Link(slot);
        return link.IsNone || !TryLine(slot, out int line) ? null : AppLinks.Hint(link, _lines[line].Category);
    }

    /// <summary>
    /// Marks each row with its line key and title ("Nikias · line 7") for the
    /// keys, the copy and the pins (only an answer's button picks; an
    /// untranslated line keeps its tongue), writes each foreign statement in
    /// its script (TextFlip.Write), and outlines the row a link went to;
    /// again after every redraw.
    /// </summary>
    private void MarkRows()
    {
        if (Form == null)
            return;
        Form.ArmedSlots(_armed);
        int found = -1;
        foreach ((FormSlot slot, Button button) in _armed)
        {
            if (!TryLine(slot, out int line))
                continue;
            DialogLine dialog = _lines[line];
            string speaker = dialog.Speaker == DialogSpeaker.Desk ? _deskName : _travellerName;
            string key = PickKeys.Line(line);
            button.interactable = dialog.IsAnswer;
            AppRow marked = AppRow.Mark(button.gameObject, AppTab.Transcript, key, UiText.Format("app.row.line", speaker, line + 1), speaker, Shown(dialog), button);
            marked.SetLink(Link(slot));
            if (_translation.Untranslated(dialog))
            {
                marked.MarkUntranslated(_translation.TongueId, _translation.TongueName, dialog.Text);
                Form.TextsOf(slot.Index, _texts);
                if (_texts.Count > 2)
                    TextFlip.Write(_texts[2], Form.Font, Form.FontMaterial, dialog.Text, _translation.Line(dialog), _translation);
            }
            if (dialog.IsAnswer && key == _foundKey)
                found = slot.Index;
        }
        Form.MarkFound(found);
    }

    /// <summary>Shows or hides the "New line" pill.</summary>
    private void ShowNewLine(bool on)
    {
        if (newLineButton != null && newLineButton.gameObject.activeSelf != on)
            newLineButton.gameObject.SetActive(on);
    }

    /// <summary>An answer's row was clicked: its fact goes into the compare as the traveller's statement.</summary>
    private void Pick(FormSlot slot)
    {
        if (_compare != null && TryLine(slot, out int line) && _lines[line].IsAnswer)
            _compare.Select(EvidencePicks.ForAnswer(line, _lines[line], _translation), null);
    }

    /// <summary>A ↗ was clicked: the answer's link, through the pane this record is in (LK2).</summary>
    private void Follow(FormSlot slot)
    {
        LinkTarget link = Link(slot);
        AppPane pane = GetComponentInParent<AppPane>();
        if (!link.IsNone && pane != null)
            pane.FollowLink(link);
    }
}
