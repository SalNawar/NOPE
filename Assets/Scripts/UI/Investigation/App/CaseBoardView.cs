using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// What the case board shows (CaseBoardPresenter builds it): the traveller,
/// the record the scanned papers name (or NO RECORD), the seen-before flag,
/// the rules check, the cross-check table and the record's FILE and SEEN
/// BEFORE groups. Runtime only.
/// </summary>
public sealed class CaseBoardModel
{
    /// <summary>The traveller at the desk (blank: nobody).</summary>
    public string Traveller = string.Empty;

    /// <summary>The scanned papers (none: the board asks for a scan).</summary>
    public List<BoardPaper> Scanned = new List<BoardPaper>();

    /// <summary>The record found (RecordLookup.Find), or null.</summary>
    public CitizenRecord Record;

    /// <summary>How it was found.</summary>
    public LookupBy By;

    /// <summary>The number or name looked up.</summary>
    public string Query;

    /// <summary>The flag of a recurring face ("SEEN BEFORE · DENIED 3 DAYS AGO"; blank: none).</summary>
    public string Flag = string.Empty;

    /// <summary>The rules check's rows (RulesCheck.Rows).</summary>
    public List<RuleCheckRow> Rules = new List<RuleCheckRow>();

    /// <summary>Today's directives, in the rows' indices (a row's rule is held from here).</summary>
    public IReadOnlyList<TravelRuleSO> RuleAssets = Array.Empty<TravelRuleSO>();

    /// <summary>The cross-check table's columns.</summary>
    public List<CrossColumn> Columns = new List<CrossColumn>();

    /// <summary>The cross-check table's rows (CrossCheck.Rows).</summary>
    public List<CrossRow> Cross = new List<CrossRow>();

    /// <summary>The record's closing groups (FILE, SEEN BEFORE), as the Records view prints them.</summary>
    public List<RecordGroup> File = new List<RecordGroup>();

    /// <summary>What the overlay can lay one over another: the scanned papers, the record, the traveller's face, the Seal Register's seals of the scanned papers' offices.</summary>
    public List<OverlaySource> Overlays = new List<OverlaySource>();
}

/// <summary>
/// The Investigation app's case board (the scanner app spec §2: "when a scan
/// lands, the app opens to that traveller's case board"): one scroll, top to
/// bottom, of the record the scanned papers name (a plate: on file, with a
/// link that opens it in the other pane, or the stamp-like NO RECORD), the
/// recurring face's flag, the rules check (a row per destination, date and
/// required paper, with its chip: VALID, EXPIRED, CLOSED, MISSING or WRONG
/// DATE; a failing row's click holds its rule against its value on the
/// workbench, so the finding is logged as any held rule's, or flags a
/// missing paper as the Papers menu does), the cross-check table (a row per
/// shared detail, a column per scanned paper and the record; a cell that
/// disagrees glows and its click compares it with its partner through the
/// one compare, so the finding, and any deviation it proves, is logged as a
/// player's pair is) and the traveller's FILE and SEEN BEFORE. Nothing here
/// decides: the board only points (the orchestrator's Decision: scanning
/// stays optional, everything here can be found by hand). The board slides
/// in, its cells pop as they fill, each glow ticks and a logged finding
/// thunks (BoardMotion on Track J's motion core; Sounds: inspect_link, evidence_pin).
/// Each pane has one; the façade fills them all (CaseBoardPresenter).
/// </summary>
public sealed class CaseBoardView : AppView
{
    [Header("Layout")]
    /// <summary>The scroll's content (a vertical layout the board fills).</summary>
    [SerializeField] private RectTransform content;

    [Header("Templates (inactive; cloned into the content)")]
    /// <summary>A section's band (its child "Text").</summary>
    [SerializeField] private GameObject sectionTemplate;

    /// <summary>A wrapping line of text.</summary>
    [SerializeField] private TMP_Text lineTemplate;

    /// <summary>A plate in the match colours (a record on file; its child "Text"; a Button).</summary>
    [SerializeField] private GameObject plateMatchTemplate;

    /// <summary>A plate in the difference colours (NO RECORD; its child "Text").</summary>
    [SerializeField] private GameObject plateDifferTemplate;

    /// <summary>A plate in the holding colours (the seen-before flag; its child "Text").</summary>
    [SerializeField] private GameObject plateHoldTemplate;

    /// <summary>A rules-check row: a Button with "Subject", "Value", "ChipValid" and "ChipFail" (each chip with its "Text").</summary>
    [SerializeField] private Button ruleRowTemplate;

    /// <summary>A row of the overlay (a horizontal layout, no graphic).</summary>
    [SerializeField] private RectTransform gridRowTemplate;

    /// <summary>A row of the cross-check table, responsive to the pane's width: its detail ("Label") over its cells ("Cells", a flow that wraps).</summary>
    [SerializeField] private RectTransform crossRowTemplate;

    /// <summary>A header cell (its child "Text").</summary>
    [SerializeField] private GameObject cellHeadTemplate;

    /// <summary>A cell that agrees (a Button, its child "Text").</summary>
    [SerializeField] private Button cellTemplate;

    /// <summary>A cell that disagrees: it glows (a Button, its child "Text").</summary>
    [SerializeField] private Button cellGlowTemplate;

    [Header("Overlay templates")]
    /// <summary>An overlay source chosen (a Button in the holding colours, its child "Text").</summary>
    [SerializeField] private Button cellPickTemplate;

    /// <summary>A line of the overlay: the two values one over the other ("Text" and "TextB", faded by the slider).</summary>
    [SerializeField] private GameObject overlayCellTemplate;

    /// <summary>A line whose values really differ: as a line, on a difference plate, with its "Shimmer".</summary>
    [SerializeField] private GameObject overlayShimmerTemplate;

    /// <summary>The fade slider (0: the first source, 1: the second).</summary>
    [SerializeField] private Slider fadeTemplate;

    [Header("Workbench")]
    /// <summary>The one compare a cell's pair goes through.</summary>
    [SerializeField] private CompareController compare;

    /// <summary>The workbench a failing rule is held on.</summary>
    [SerializeField] private MatchBoard board;

    [Header("Feel")]
    /// <summary>How far the board slides in from the right when a new scan fills it (desktop units).</summary>
    [SerializeField, Min(0f)] private float slideDistance = 80f;

    /// <summary>How long the slide and a cell's pop take (seconds).</summary>
    [SerializeField, Min(0.01f)] private float slideSeconds = 0.22f;

    private readonly List<GameObject> _built = new List<GameObject>();
    private readonly List<(TMP_Text a, TMP_Text b)> _faded = new List<(TMP_Text, TMP_Text)>();
    private CaseBoardModel _model = new CaseBoardModel();
    private int _scansShown;
    private int _overlayA = -1, _overlayB = -1;
    private float _fade = 0.5f;

    /// <summary>Raised when a MISSING row is clicked: the paper's request id, to flag missing (the Papers menu's flag; the façade's FlagMissing).</summary>
    public event Action<string> FlagRequested;

    /// <inheritdoc />
    public override AppTab Tab => AppTab.Board;

    /// <summary>The model shown (tests and probes read it).</summary>
    public CaseBoardModel Model => _model;

    private void Awake()
    {
        foreach (Component t in new Component[] { lineTemplate, ruleRowTemplate, gridRowTemplate, crossRowTemplate, cellTemplate, cellGlowTemplate, cellPickTemplate, fadeTemplate })
            if (t != null)
                t.gameObject.SetActive(false);
        foreach (GameObject t in new[] { sectionTemplate, plateMatchTemplate, plateDifferTemplate, plateHoldTemplate, cellHeadTemplate, overlayCellTemplate, overlayShimmerTemplate })
            if (t != null)
                t.SetActive(false);
    }

    /// <summary>Draws <paramref name="model"/> (null: nobody at the desk); a new scan slides the board in and pops its cells.</summary>
    public void Show(CaseBoardModel model)
    {
        _model = model ?? new CaseBoardModel();
        bool newScan = _model.Scanned.Count > _scansShown;
        _scansShown = _model.Scanned.Count;
        Draw(newScan && isActiveAndEnabled);
        if (newScan && isActiveAndEnabled && content != null)
            StartCoroutine(BoardMotion.SlideIn(content, new Vector2(slideDistance, 0f), slideSeconds));
    }

    /// <summary>The case ended: the board forgets its scans and its overlay.</summary>
    public void Clear()
    {
        _scansShown = 0;
        _overlayA = _overlayB = -1;
        Show(null);
    }

    /// <summary>
    /// Lays overlay source <paramref name="a"/> over <paramref name="b"/>
    /// (their indices in the model's overlays; a chip dropped on another, or
    /// two clicks): the lines both state, faded by the slider, shimmering only
    /// where the data really differ (OverlayCompare). The same source twice,
    /// or an index out of range, clears it.
    /// </summary>
    public void Overlay(int a, int b)
    {
        bool valid = a >= 0 && b >= 0 && a != b && a < _model.Overlays.Count && b < _model.Overlays.Count;
        _overlayA = valid ? a : -1;
        _overlayB = valid ? b : -1;
        Draw(false);
    }

    /// <summary>The overlay's lines shown (empty: none laid over).</summary>
    public List<OverlayRow> OverlayRows => _overlayA >= 0 && _overlayB >= 0 && _overlayA < _model.Overlays.Count && _overlayB < _model.Overlays.Count
        ? OverlayCompare.Rows(_model.Overlays[_overlayA], _model.Overlays[_overlayB])
        : new List<OverlayRow>();

    private void Draw(bool pop)
    {
        foreach (GameObject go in _built)
            if (go != null)
                Destroy(go);
        _built.Clear();
        _faded.Clear();
        if (content == null)
            return;

        if (_model.Scanned.Count == 0)
        {
            Line(UiText.Get(string.IsNullOrEmpty(_model.Traveller) ? "idle.waiting" : "board.noScan"));
            return;
        }

        // The record the papers name.
        switch (_model.By)
        {
            case LookupBy.Number:
            case LookupBy.Name:
                GameObject found = Plate(plateMatchTemplate, UiText.Format("board.record.found", _model.Record.FullName,
                                                                            string.IsNullOrWhiteSpace(_model.Record.Number) ? UiText.Get("board.record.noNumber") : _model.Record.Number));
                if (found != null && found.TryGetComponent(out Button open))
                    open.onClick.AddListener(OpenRecord);
                break;
            case LookupBy.NoRecord:
                Plate(plateDifferTemplate, UiText.Format("board.record.none", _model.Query));
                break;
            default:
                Line(UiText.Get("board.record.nothing"));
                break;
        }
        if (!string.IsNullOrWhiteSpace(_model.Flag))
            Plate(plateHoldTemplate, _model.Flag);

        // The rules check.
        Section(UiText.Get("board.section.rules"));
        if (_model.Rules.Count == 0)
            Line(UiText.Get("board.rules.none"));
        foreach (RuleCheckRow row in _model.Rules)
            RuleRow(row);

        // The cross-check table.
        Section(UiText.Get("board.section.cross"));
        if (_model.Cross.Count == 0)
            Line(UiText.Get("board.cross.none"));
        else
            Grid(pop);

        // The overlay.
        OverlaySection();

        // The file and the history.
        foreach (RecordGroup group in _model.File)
        {
            Section(group.Title);
            foreach (RecordRow row in group.Rows)
                Line(UiText.Format("board.file.line", row.Label, row.Value));
        }
    }

    /// <summary>A section's band.</summary>
    private void Section(string title)
    {
        GameObject band = Clone(sectionTemplate);
        if (band != null)
            SetText(band, title);
    }

    /// <summary>A wrapping line.</summary>
    private void Line(string text)
    {
        if (lineTemplate == null)
            return;
        GameObject line = Clone(lineTemplate.gameObject);
        line.GetComponent<TMP_Text>().text = text ?? string.Empty;
    }

    /// <summary>A plate reading <paramref name="text"/>.</summary>
    private GameObject Plate(GameObject template, string text)
    {
        GameObject plate = Clone(template);
        if (plate != null)
            SetText(plate, text);
        return plate;
    }

    /// <summary>One rules-check row: its subject, value and chip; a failing row is clickable (logs, or flags the paper).</summary>
    private void RuleRow(RuleCheckRow row)
    {
        if (ruleRowTemplate == null)
            return;
        GameObject go = Clone(ruleRowTemplate.gameObject);
        Transform t = go.transform;
        SetText(t.Find("Subject"), Subject(row));
        SetText(t.Find("Value"), row.Value);
        Transform valid = t.Find("ChipValid"), fail = t.Find("ChipFail");
        if (valid != null)
            valid.gameObject.SetActive(!row.Failing);
        if (fail != null)
            fail.gameObject.SetActive(row.Failing);
        SetText(row.Failing ? fail : valid, UiText.Get("board.chip." + row.Chip));
        Button button = go.GetComponent<Button>();
        button.interactable = row.Failing;
        button.onClick.AddListener(() => RuleClicked(row));
    }

    /// <summary>What a rules-check row reads: the destination, a date's label and paper, or a required paper.</summary>
    private string Subject(RuleCheckRow row)
    {
        switch (row.Kind)
        {
            case RuleCheckKind.Destination: return UiText.Get("board.rule.destination");
            case RuleCheckKind.Paper: return UiText.Get("board.rule.paper");
            default:
                BoardPaper paper = _model.Scanned.Find(p => p.Document == row.Document);
                string label = paper != null && row.Field >= 0 && row.Field < paper.Fields.Count ? paper.Fields[row.Field].label : UiText.Category(row.Category);
                return UiText.Format("board.rule.date", label, row.Paper);
        }
    }

    /// <summary>
    /// A failing row clicked: a loggable one holds its rule on the workbench
    /// and then its value (MatchBoard.PickRule, then the compare: the rule is
    /// judged against it and the finding logged); a missing paper is flagged
    /// missing (its request on the wheel); a destination no scanned box names
    /// says so on the status line.
    /// </summary>
    private void RuleClicked(RuleCheckRow row)
    {
        if (!row.Failing)
            return;
        if (row.Kind == RuleCheckKind.Paper)
        {
            FlagRequested?.Invoke(row.RequestId);
            Sounds.Play(SoundCues.InspectLink);
            return;
        }
        if (!row.Loggable || board == null || compare == null || row.Rule >= _model.RuleAssets.Count)
            return;
        BoardPaper paper = _model.Scanned.Find(p => p.Document == row.Document);
        TravelRuleSO rule = _model.RuleAssets[row.Rule];
        if (paper == null || rule == null)
            return;
        int logged = board.Log.Count;
        board.Release();
        board.PickRule(row.Rule, rule);
        compare.Select(EvidencePicks.ForField(row.Document, new DocumentRow(row.Field, paper.Fields[row.Field]), paper.Name), null);
        if (board.Log.Count > logged)
            Sounds.Play(SoundCues.EvidencePin);
    }

    /// <summary>
    /// The cross-check table, a row per shared detail (its word over its
    /// cells, which wrap to the pane's width): a row whose sources agree is
    /// one cell, the value and how many sources state it; a row that
    /// disagrees is a cell per source that states it ("Entry Ticket: 4 Jul
    /// 2120"), the cells that differ glowing and clickable.
    /// </summary>
    private void Grid(bool pop)
    {
        int popped = 0;
        for (int r = 0; r < _model.Cross.Count; r++)
        {
            CrossRow row = _model.Cross[r];
            GameObject block = Clone(crossRowTemplate != null ? crossRowTemplate.gameObject : null);
            if (block == null)
                return;
            SetText(block.transform.Find("Label"), Detail(row));
            var line = (RectTransform)block.transform.Find("Cells");
            if (!row.Mismatch)
            {
                CrossCell first = row.Cells.First(x => x.Value.HasValue);
                GameObject agree = Cell(line, cellTemplate != null ? cellTemplate.gameObject : null,
                                        UiText.Format("board.cross.agree", first.Value.Value.Value, row.Cells.Count(x => x.Value.HasValue)));
                if (agree != null)
                {
                    agree.GetComponent<Button>().interactable = false;
                    if (pop)
                        StartCoroutine(BoardMotion.Pop(agree.transform, 0.03f * popped++, slideSeconds));
                }
                continue;
            }
            for (int c = 0; c < row.Cells.Count; c++)
            {
                CrossCell cell = row.Cells[c];
                if (!cell.Value.HasValue)
                    continue;
                Button template = cell.Glows ? cellGlowTemplate : cellTemplate;
                GameObject go = Cell(line, template != null ? template.gameObject : null, UiText.Format("board.cross.cell", _model.Columns[c].Title, cell.Value.Value.Value));
                if (go == null)
                    continue;
                Button button = go.GetComponent<Button>();
                button.interactable = cell.Glows;
                int rr = r, cc = c;
                button.onClick.AddListener(() => CellClicked(rr, cc));
                if (pop && cell.Value.HasValue)
                    StartCoroutine(BoardMotion.Pop(go.transform, 0.03f * popped++, slideSeconds));
                if (pop && cell.Glows)
                    Sounds.Play(SoundCues.InspectLink);
            }
        }
    }

    /// <summary>
    /// The overlay section: a chip per source (a click chooses the first, a
    /// second click lays it over the other; a chip dropped on another does
    /// the same, OverlayChip), then, laid over, the fade slider and a line per
    /// detail both state, the two values one over the other.
    /// </summary>
    private void OverlaySection()
    {
        if (_model.Overlays.Count < 2)
            return;
        Section(UiText.Get("board.section.overlay"));
        bool laid = _overlayA >= 0 && _overlayB >= 0;
        Line(laid ? UiText.Format("board.overlay.over", _model.Overlays[_overlayA].Title, _model.Overlays[_overlayB].Title) : UiText.Get("board.overlay.hint"));
        GameObject sources = Clone(crossRowTemplate != null ? crossRowTemplate.gameObject : null);
        if (sources == null)
            return;
        SetText(sources.transform.Find("Label"), UiText.Get("board.overlay.sources"));
        var chips = (RectTransform)sources.transform.Find("Cells");
        for (int i = 0; i < _model.Overlays.Count; i++)
        {
            Button template = i == _overlayA || i == _overlayB ? cellPickTemplate : cellTemplate;
            GameObject chip = Cell(chips, template != null ? template.gameObject : null, _model.Overlays[i].Title);
            if (chip == null)
                continue;
            int source = i;
            Button button = chip.GetComponent<Button>();
            button.interactable = true;
            button.onClick.AddListener(() => OverlayClicked(source));
            chip.AddComponent<OverlayChip>().Set(this, source);
        }
        if (!laid)
            return;

        if (fadeTemplate != null)
        {
            GameObject fade = Clone(fadeTemplate.gameObject);
            Slider slider = fade.GetComponent<Slider>();
            slider.SetValueWithoutNotify(_fade);
            slider.onValueChanged.AddListener(Fade);
        }
        List<OverlayRow> rows = OverlayRows;
        if (rows.Count == 0)
            Line(UiText.Get("board.overlay.nothing"));
        foreach (OverlayRow row in rows)
        {
            RectTransform line = Row();
            Cell(line, cellHeadTemplate, row.Label);
            GameObject cell = Cell(line, row.Differs ? overlayShimmerTemplate : overlayCellTemplate, Shown(row.Category, row.A));
            if (cell == null)
                continue;
            Transform a = cell.transform.Find("Text"), b = cell.transform.Find("TextB");
            TMP_Text textA = a != null ? a.GetComponent<TMP_Text>() : null;
            TMP_Text textB = b != null ? b.GetComponent<TMP_Text>() : null;
            if (textB != null)
                textB.text = Shown(row.Category, row.B);
            _faded.Add((textA, textB));
            Transform shimmer = cell.transform.Find("Shimmer");
            if (row.Differs && shimmer != null && shimmer.TryGetComponent(out Graphic glint))
                StartCoroutine(BoardMotion.Shimmer(glint));
        }
        Fade(_fade);
    }

    /// <summary>A value as the overlay prints it: a photo as "the photo" (its value is who it shows), anything else as it reads.</summary>
    private static string Shown(ClueCategory category, string value) => category == ClueCategory.Photo ? UiText.Get("board.overlay.photo") : value;

    /// <summary>The slider moved: the first source's values fade out as the second's fade in.</summary>
    private void Fade(float t)
    {
        _fade = Mathf.Clamp01(t);
        foreach ((TMP_Text a, TMP_Text b) in _faded)
        {
            if (a != null)
                a.alpha = 1f - _fade;
            if (b != null)
                b.alpha = _fade;
        }
    }

    /// <summary>A source's chip clicked: the first choice, or the second (laid over the first); the first again lets go.</summary>
    private void OverlayClicked(int source)
    {
        if (_overlayA >= 0 && _overlayB >= 0)
            _overlayA = _overlayB = -1;
        if (_overlayA < 0)
        {
            _overlayA = source;
            Draw(false);
        }
        else if (_overlayA == source)
        {
            _overlayA = -1;
            Draw(false);
        }
        else
            Overlay(_overlayA, source);
        Sounds.Play(SoundCues.InspectLink);
    }

    /// <summary>A row's detail: the first source's word for it ("Date of Birth").</summary>
    private static string Detail(CrossRow row)
    {
        foreach (CrossCell cell in row.Cells)
            if (cell.Value.HasValue && !string.IsNullOrWhiteSpace(cell.Value.Value.Label))
                return cell.Value.Value.Label;
        return UiText.Category(row.Category);
    }

    /// <summary>A glowing cell clicked: it and its partner go through the one compare, as two clicked values do (the finding, any deviation proven, logged).</summary>
    private void CellClicked(int r, int c)
    {
        if (compare == null || r >= _model.Cross.Count)
            return;
        CrossRow row = _model.Cross[r];
        CrossCell cell = row.Cells[c];
        if (!cell.Glows || cell.Partner < 0 || !cell.Value.HasValue || !row.Cells[cell.Partner].Value.HasValue)
            return;
        ComparePick? a = Pick(c, cell.Value.Value), b = Pick(cell.Partner, row.Cells[cell.Partner].Value.Value);
        if (!a.HasValue || !b.HasValue)
            return;
        int logged = board != null ? board.Log.Count : 0;
        if (board != null)
            board.Release();
        compare.Clear();
        compare.Select(a.Value, null);
        compare.Select(b.Value, null);
        if (board != null && board.Log.Count > logged)
            Sounds.Play(SoundCues.EvidencePin);
    }

    /// <summary>The compare pick of a cell's value: a scanned paper's field, or the record's row.</summary>
    private ComparePick? Pick(int column, CrossValue value)
    {
        CrossColumn source = _model.Columns[column];
        if (source.IsRecord)
            return _model.Record != null && RecordExtractPage.TryRow(_model.Record, value.Index, out RecordRow row) ? EvidencePicks.ForRecord(_model.Record, row) : (ComparePick?)null;
        BoardPaper paper = _model.Scanned.Find(p => p.Document == source.Document);
        return paper != null && value.Index < paper.Fields.Count
            ? EvidencePicks.ForField(paper.Document, new DocumentRow(value.Index, paper.Fields[value.Index]), paper.Name)
            : (ComparePick?)null;
    }

    /// <summary>The record plate's link: the record in the other pane (Records, looked up by the query), as a row's ↗ does.</summary>
    private void OpenRecord()
    {
        AppPane pane = GetComponentInParent<AppPane>();
        if (pane != null && !string.IsNullOrWhiteSpace(_model.Query))
            pane.FollowLink(LinkTarget.ToRecords(_model.Query));
    }

    /// <summary>A new row of the table.</summary>
    private RectTransform Row()
    {
        GameObject row = Clone(gridRowTemplate != null ? gridRowTemplate.gameObject : null);
        return row != null ? (RectTransform)row.transform : null;
    }

    /// <summary>A cell of <paramref name="row"/> from <paramref name="template"/> reading <paramref name="text"/>.</summary>
    private static GameObject Cell(RectTransform row, GameObject template, string text)
    {
        if (row == null || template == null)
            return null;
        GameObject cell = Instantiate(template, row, false);
        cell.SetActive(true);
        SetText(cell, text);
        return cell;
    }

    /// <summary>A clone of <paramref name="template"/> at the content's end, shown.</summary>
    private GameObject Clone(GameObject template)
    {
        if (template == null)
            return null;
        GameObject go = Instantiate(template, content, false);
        go.SetActive(true);
        _built.Add(go);
        return go;
    }

    /// <summary>The text of <paramref name="host"/> (its child "Text", else its own).</summary>
    private static void SetText(GameObject host, string text) => SetText(host != null ? host.transform : null, text);

    private static void SetText(Transform host, string text)
    {
        if (host == null)
            return;
        Transform child = host.Find("Text");
        TMP_Text label = child != null ? child.GetComponent<TMP_Text>() : host.GetComponent<TMP_Text>();
        if (label != null)
            label.text = text ?? string.Empty;
    }
}

/// <summary>
/// The case board's motions (the scanner app spec's "Feel": the board slides
/// in, its cells pop, a real difference shimmers) on Track J's motion core:
/// unscaled time, each shape UiMotion.Ease's spring curve (calmer as the
/// player's Motion intensity falls, critically damped under Reduced Motion).
/// </summary>
public static class BoardMotion
{
    /// <summary>Slides <paramref name="target"/> in from <paramref name="offset"/> to where it is, over <paramref name="seconds"/>.</summary>
    public static IEnumerator SlideIn(RectTransform target, Vector2 offset, float seconds)
    {
        if (target == null)
            yield break;
        Vector2 home = target.anchoredPosition;
        for (float t = 0f; t < seconds && target != null; t += Time.unscaledDeltaTime)
        {
            target.anchoredPosition = home + offset * (1f - UiMotion.Ease(t / seconds, MotionFeel.Heavy, seconds));
            yield return null;
        }
        if (target != null)
            target.anchoredPosition = home;
    }

    /// <summary>A shimmer: <paramref name="glint"/>'s alpha breathes while it lives (a difference that really is one).</summary>
    public static IEnumerator Shimmer(Graphic glint)
    {
        while (glint != null)
        {
            Color c = glint.color;
            c.a = 0.15f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f));
            glint.color = c;
            yield return null;
        }
    }

    /// <summary>Pops <paramref name="target"/> from 80% to its size after <paramref name="delay"/>, over <paramref name="seconds"/>.</summary>
    public static IEnumerator Pop(Transform target, float delay, float seconds)
    {
        if (target == null)
            yield break;
        target.localScale = Vector3.one * 0.8f;
        for (float d = 0f; d < delay; d += Time.unscaledDeltaTime)
            yield return null;
        for (float t = 0f; t < seconds && target != null; t += Time.unscaledDeltaTime)
        {
            target.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, UiMotion.Ease(t / seconds, MotionFeel.Elastic, seconds));
            yield return null;
        }
        if (target != null)
            target.localScale = Vector3.one;
    }

}

/// <summary>
/// An overlay source's chip on the case board (the scanner app spec §2.5:
/// "drag one scan or photo onto another"): dragged and dropped on another
/// source's chip, the board lays the first over the second.
/// </summary>
public sealed class OverlayChip : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private CaseBoardView _board;
    private int _source;

    /// <summary>The board it lays over and the source it is.</summary>
    public void Set(CaseBoardView board, int source)
    {
        _board = board;
        _source = source;
    }

    /// <inheritdoc />
    public void OnBeginDrag(PointerEventData eventData) => transform.localScale = Vector3.one * 1.05f;

    /// <inheritdoc />
    public void OnDrag(PointerEventData eventData)
    {
        // Nothing moves: the drop on another chip decides.
    }

    /// <inheritdoc />
    public void OnEndDrag(PointerEventData eventData) => transform.localScale = Vector3.one;

    /// <inheritdoc />
    public void OnDrop(PointerEventData eventData)
    {
        OverlayChip dragged = eventData != null && eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<OverlayChip>() : null;
        if (dragged != null && dragged != this && _board != null)
            _board.Overlay(dragged._source, _source);
    }
}
