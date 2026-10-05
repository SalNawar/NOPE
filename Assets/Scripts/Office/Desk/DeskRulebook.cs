using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// The rulebook on the desk, Papers, Please's booklet (Saleh 2026-10-06: "the
/// rulebook is a booklet on the desk: drag it, click its tabs to turn pages;
/// it is comparable in inspect mode"). It lies beside the mat (the office
/// binder places it: Place) and moves by left-drag like a document
/// (DeskDraggable; dropped on the counter it slides back: it is the
/// office's, not the traveller's). Two tabs on its top edge turn its pages
/// (ShowPage): RULES prints today's directives (the day's Directive Memo,
/// the PC's Rules), one row each; PAPERS lists the papers the traveller has
/// not handed over (Track C's MissingPapers: the day's papers menu, the same
/// list as the PC's Papers menu), and a click on one flags it missing
/// (PaperFlagged; Saleh: asking for a document must be flagged first), which
/// unlocks "Hand me your ..." on the wheel; a flagged or not-carried paper
/// says so. A click on a rules row raises RowClicked with the directive's
/// index in the day's list (the PC's EntryKeys.Rule index) and the rule: in
/// inspect mode (SetInspecting tints the rows as comparable) DeskInspect holds
/// it on the workbench (MatchBoard.PickRule), so it is judged against a value
/// like the PC's memo row; outside inspect mode a row is only read.
/// Each new day opens on RULES. It shows from the day the rulebook is
/// introduced (Feature.Rulebook). Build Office UI builds the booklet, its
/// tabs, pages and rows (a fixed number: more directives than rows print
/// only the first ones).
/// </summary>
public sealed class DeskRulebook : MonoBehaviour
{
    /// <summary>The rules page's title ("TODAY'S RULES").</summary>
    [SerializeField] private TMP_Text title;

    /// <summary>The rules' rows, top first: each a click box (Clickable, a collider) with its text as its child.</summary>
    [SerializeField] private Clickable[] rows = Array.Empty<Clickable>();

    /// <summary>The line printed when the day has no directive.</summary>
    [SerializeField] private TMP_Text none;

    /// <summary>The papers page's heading ("PAPERS NOT HANDED OVER").</summary>
    [SerializeField] private TMP_Text papersTitle;

    /// <summary>The papers rows, top first: each a click box with its text as its child (a paper not handed over; a click flags it missing).</summary>
    [SerializeField] private Clickable[] paperRows = Array.Empty<Clickable>();

    /// <summary>The line printed when no paper is left to flag.</summary>
    [SerializeField] private TMP_Text papersNone;

    /// <summary>The booklet's own click box (its hover outline; the drag's proxy: a drag from anywhere on it moves it).</summary>
    [SerializeField] private Clickable card;

    /// <summary>The RULES page (its title, rows and none line).</summary>
    [SerializeField] private GameObject rulesPage;

    /// <summary>The PAPERS page (its heading, rows and none line).</summary>
    [SerializeField] private GameObject papersPage;

    /// <summary>The tabs on the top edge, RULES first: a click turns to its page.</summary>
    [SerializeField] private Clickable[] tabs = Array.Empty<Clickable>();

    /// <summary>The tabs' plates, as tabs (the open page's plate is the paper's, the other's darker).</summary>
    [SerializeField] private Renderer[] tabPlates = Array.Empty<Renderer>();

    /// <summary>The booklet's drag (left-drag moves it on the desk).</summary>
    [SerializeField] private DeskDraggable drag;

    /// <summary>The desk plane it moves on.</summary>
    [SerializeField] private DeskSurface surface;

    /// <summary>The counter (optional): the booklet dropped there goes back to where it was picked up.</summary>
    [SerializeField] private DeskCounter counter;

    /// <summary>The rows' ink, and their ink in inspect mode (comparable).</summary>
    [SerializeField] private Color rowInk = new Color(0.13f, 0.12f, 0.15f), inspectInk = new Color(0.62f, 0.38f, 0.02f);

    /// <summary>The open tab's plate and the other tab's.</summary>
    [SerializeField] private Color openTab = new Color(0.93f, 0.9f, 0.8f), closedTab = new Color(0.66f, 0.62f, 0.52f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private readonly List<int> _indices = new List<int>();
    private IReadOnlyList<TravelRuleSO> _rules = Array.Empty<TravelRuleSO>();
    private readonly List<string> _paperIds = new List<string>();
    private MaterialPropertyBlock _block;
    private bool _inspecting;

    /// <summary>Raised when a rules row is clicked in inspect mode: the directive's index in the day's list and the rule.</summary>
    public event Action<int, TravelRuleSO> RowClicked;

    /// <summary>Raised when a paper not handed over is clicked to flag it missing: its request's id (a FormRequest.Id).</summary>
    public event Action<string> PaperFlagged;

    /// <summary>Every click of the booklet (the rows, the papers' rows, the tabs, the booklet itself): the booth makes them live with the props.</summary>
    public IReadOnlyList<Clickable> Clicks => rows.Concat(paperRows).Concat(tabs).Append(card).Where(c => c != null).ToArray();

    /// <summary>The page shown: 0 RULES, 1 PAPERS.</summary>
    public int Page { get; private set; }

    private void Awake()
    {
        if (title != null)
            title.text = UiText.Get("desk.rulebook.title");
        if (none != null)
            none.text = UiText.Get("desk.rulebook.none");
        if (papersTitle != null)
            papersTitle.text = UiText.Get("desk.rulebook.papers");
        if (papersNone != null)
            papersNone.text = UiText.Get("desk.rulebook.papersNone");
        for (int i = 0; i < rows.Length; i++)
        {
            int row = i;
            if (rows[i] != null)
                rows[i].onClick.AddListener(() => Click(row));
        }
        for (int i = 0; i < paperRows.Length; i++)
        {
            int row = i;
            if (paperRows[i] != null)
                paperRows[i].onClick.AddListener(() => FlagPaper(row));
        }
        for (int i = 0; i < tabs.Length; i++)
        {
            int page = i;
            if (tabs[i] != null)
                tabs[i].onClick.AddListener(() => ShowPage(page));
        }
        if (drag != null)
        {
            drag.Init(surface);
            drag.DragEnded += Dropped;
        }
        Show(_rules);
        ShowMissing(MissingPapers.None, null);
    }

    private void OnDestroy()
    {
        if (drag != null)
            drag.DragEnded -= Dropped;
    }

    /// <summary>Lays the booklet on the desk at <paramref name="at"/>, turned to <paramref name="rotation"/> (the office binder).</summary>
    public void Place(Vector3 at, Quaternion rotation) => transform.SetPositionAndRotation(at, rotation);

    /// <summary>Turns to page <paramref name="page"/> (0 RULES, 1 PAPERS): its rows show, the other's hide; its tab looks open.</summary>
    public void ShowPage(int page)
    {
        Page = Mathf.Clamp(page, 0, 1);
        if (rulesPage != null)
            rulesPage.SetActive(Page == 0);
        if (papersPage != null)
            papersPage.SetActive(Page == 1);
        _block ??= new MaterialPropertyBlock();
        for (int i = 0; i < tabPlates.Length; i++)
        {
            if (tabPlates[i] == null)
                continue;
            tabPlates[i].GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, i == Page ? openTab : closedTab);
            tabPlates[i].SetPropertyBlock(_block);
        }
    }

    /// <summary>Inspect mode on or off (BoothCoordinator): the rules' rows print in the comparable ink, and a click on one compares.</summary>
    public void SetInspecting(bool inspecting)
    {
        _inspecting = inspecting;
        foreach (Clickable row in rows)
            if (row != null && row.GetComponentInChildren<TMP_Text>(true) is TMP_Text text)
                text.color = inspecting ? inspectInk : rowInk;
    }

    /// <summary>Lists the traveller's papers not handed over (<paramref name="missing"/>'s open requests over <paramref name="papers"/>), one per row, each with where it stands: to flag, flagged (ask for it on the wheel) or not carried; nothing between travellers.</summary>
    public void ShowMissing(MissingPapers missing, CasePapers papers)
    {
        List<FormRequest> open = (missing ?? MissingPapers.None).Open(papers);
        _paperIds.Clear();
        for (int r = 0; r < paperRows.Length; r++)
        {
            if (paperRows[r] == null)
                continue;
            bool on = r < open.Count;
            paperRows[r].gameObject.SetActive(on);
            if (!on)
                continue;
            _paperIds.Add(open[r].Id);
            string key = missing.State(open[r].Id) switch
            {
                MissingPaperState.Flagged => "desk.rulebook.paperFlagged",
                MissingPaperState.NotCarried => "desk.rulebook.paperNotCarried",
                _ => "desk.rulebook.paperFlag"
            };
            if (paperRows[r].GetComponentInChildren<TMP_Text>(true) is TMP_Text text)
                text.text = UiText.Format(key, open[r].Label);
        }
        if (papersNone != null)
            papersNone.gameObject.SetActive(_paperIds.Count == 0);
    }

    /// <summary>Prints today's directives (<paramref name="rules"/>, the day's list; a rule with no summary prints no row), one per row, and opens on RULES.</summary>
    public void Show(IReadOnlyList<TravelRuleSO> rules)
    {
        _rules = rules ?? Array.Empty<TravelRuleSO>();
        _indices.Clear();
        for (int i = 0; i < _rules.Count; i++)
            if (_rules[i] != null && !string.IsNullOrWhiteSpace(_rules[i].Summary()))
                _indices.Add(i);
        for (int r = 0; r < rows.Length; r++)
        {
            if (rows[r] == null)
                continue;
            bool on = r < _indices.Count;
            rows[r].gameObject.SetActive(on);
            if (on && rows[r].GetComponentInChildren<TMP_Text>(true) is TMP_Text text)
                text.text = $"{r + 1}. {_rules[_indices[r]].Summary()}";
        }
        if (none != null)
            none.gameObject.SetActive(_indices.Count == 0);
        SetInspecting(_inspecting);
        ShowPage(0);
    }

    /// <summary>The world bounds of the row printing directive <paramref name="ruleIndex"/> (a match line meets it there); false when it prints no row, its page is closed or the booklet is hidden.</summary>
    public bool TryRowBounds(int ruleIndex, out Bounds bounds)
    {
        bounds = default;
        int row = _indices.IndexOf(ruleIndex);
        if (!isActiveAndEnabled || row < 0 || row >= rows.Length || rows[row] == null || !rows[row].gameObject.activeInHierarchy ||
            !rows[row].TryGetComponent(out Collider box))
            return false;
        bounds = box.bounds;
        return true;
    }

    /// <summary>The booklet dropped: on the counter it goes back to where it was picked up (it stays on the clerk's side).</summary>
    private void Dropped(DeskDraggable dragged, Vector3 released)
    {
        if (counter != null && counter.Contains(released))
            transform.position = dragged.PickUpPosition;
    }

    /// <summary>A paper row clicked: its paper is flagged missing (the controller ignores one flagged already).</summary>
    private void FlagPaper(int row)
    {
        if (row < _paperIds.Count)
            PaperFlagged?.Invoke(_paperIds[row]);
    }

    private void Click(int row)
    {
        if (row < _indices.Count)
            RowClicked?.Invoke(_indices[row], _rules[_indices[row]]);
    }
}
