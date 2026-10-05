using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// The rulebook on the desk (the desk-first redesign, item 11: a value is
/// held against "the rulebook/bulletin" at the desk as on the PC): a card
/// lying beside the mat that prints today's directives (the day's Directive
/// Memo, the PC's Rules), one row each, under its title. A click on a row
/// raises RowClicked with the directive's index in the day's list (the PC's
/// EntryKeys.Rule index) and the rule: DeskInspect holds it on the workbench
/// (MatchBoard.PickRule), so it is judged against a value like the PC's memo
/// row. It shows from the day the rulebook is introduced (Feature.Rulebook).
/// It lies tucked at the mat's edge (the desk-first polish, 2026-10-05: small,
/// so the papers own the desk view): a click on the card opens it to full
/// size where its rows take clicks; a click on the open card off its rows (its
/// title, a margin) tucks it again; each new day's rules start tucked.
/// Under the rules it lists the papers the traveller has not handed over
/// (Track C's MissingPapers: the day's papers menu, the same list as the PC's
/// Papers menu): a click on one flags it missing (PaperFlagged; Saleh: asking
/// for a document must be flagged first), which unlocks "Hand me your ..." on
/// the wheel; a flagged or not-carried paper says so.
/// Build Office UI builds the card, its click box and its rows (a fixed
/// number: more directives than rows print only the first ones); the office
/// binder lays it on the desk (Place).
/// </summary>
public sealed class DeskRulebook : MonoBehaviour
{
    /// <summary>The card's title ("TODAY'S RULES").</summary>
    [SerializeField] private TMP_Text title;

    /// <summary>The rows, top first: each a click box (Clickable, a collider) with its text as its child.</summary>
    [SerializeField] private Clickable[] rows = Array.Empty<Clickable>();

    /// <summary>The line printed when the day has no directive.</summary>
    [SerializeField] private TMP_Text none;

    /// <summary>The papers block's heading ("PAPERS NOT HANDED OVER"), shown while a paper can be flagged.</summary>
    [SerializeField] private TMP_Text papersTitle;

    /// <summary>The papers rows, top first: each a click box with its text as its child (a paper not handed over; a click flags it missing).</summary>
    [SerializeField] private Clickable[] paperRows = Array.Empty<Clickable>();

    /// <summary>The whole card's click box (opens the tucked card; on the open card, under the rows, tucks it).</summary>
    [SerializeField] private Clickable card;

    /// <summary>The card's size on the desk, metres (width, depth): the tucked card shrinks toward its outer near corner.</summary>
    [SerializeField] private Vector2 size = new Vector2(0.26f, 0.21f);

    /// <summary>The tucked card's scale (1: open).</summary>
    [SerializeField, Range(0.2f, 1f)] private float tuckedScale = 0.5f;

    /// <summary>The card box's height while tucked (metres): it rises above the rows so the whole card takes the click.</summary>
    private const float TuckedBoxHeight = 0.008f;

    /// <summary>The card box's height while open (metres): it lies under the rows, which win the click over it.</summary>
    private const float OpenBoxHeight = 0.0004f;

    private readonly List<int> _indices = new List<int>();
    private IReadOnlyList<TravelRuleSO> _rules = Array.Empty<TravelRuleSO>();
    private Vector3 _at;
    private Quaternion _rotation = Quaternion.identity;
    private bool _placed;
    private readonly List<string> _paperIds = new List<string>();

    /// <summary>Raised when a row is clicked: the directive's index in the day's list and the rule.</summary>
    public event Action<int, TravelRuleSO> RowClicked;

    /// <summary>Raised when a paper not handed over is clicked to flag it missing: its request's id (a FormRequest.Id).</summary>
    public event Action<string> PaperFlagged;

    /// <summary>The rows' clicks, the directives' and the papers' (the booth makes them live with the props).</summary>
    public IReadOnlyList<Clickable> Rows => rows.Concat(paperRows).ToArray();

    /// <summary>True while the card is open (full size, its rows taking clicks); false while tucked at the mat's edge.</summary>
    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (title != null)
            title.text = UiText.Get("desk.rulebook.title");
        if (none != null)
            none.text = UiText.Get("desk.rulebook.none");
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
        if (papersTitle != null)
            papersTitle.text = UiText.Get("desk.rulebook.papers");
        if (card != null)
            card.onClick.AddListener(() => SetOpen(!IsOpen));
        Show(_rules);
        ShowMissing(MissingPapers.None, null);
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
        if (papersTitle != null)
            papersTitle.gameObject.SetActive(_paperIds.Count > 0);
    }

    /// <summary>A paper row clicked: its paper is flagged missing (the controller ignores one flagged already).</summary>
    private void FlagPaper(int row)
    {
        if (row < _paperIds.Count)
            PaperFlagged?.Invoke(_paperIds[row]);
    }

    /// <summary>Lays the card on the desk (the office binder): open, it lies at <paramref name="at"/>; tucked, it shrinks toward its outer near corner from there.</summary>
    public void Place(Vector3 at, Quaternion rotation)
    {
        _at = at;
        _rotation = rotation;
        _placed = true;
        SetOpen(IsOpen);
    }

    /// <summary>Opens the card to full size (its rows take clicks) or tucks it at the mat's edge (the whole card takes the click that opens it).</summary>
    public void SetOpen(bool open)
    {
        IsOpen = open;
        if (_placed)
        {
            float s = open ? 1f : tuckedScale;
            Vector3 corner = new Vector3(-size.x, 0f, -size.y) * ((1f - s) / 2f);
            transform.SetPositionAndRotation(_at + _rotation * corner, _rotation);
            transform.localScale = new Vector3(s, 1f, s);
        }
        if (card != null && card.TryGetComponent(out BoxCollider box))
        {
            float h = open ? OpenBoxHeight : TuckedBoxHeight;
            box.center = new Vector3(0f, open ? -h / 2f : h / 2f, 0f);
            box.size = new Vector3(size.x, h, size.y);
        }
    }

    /// <summary>Prints today's directives (<paramref name="rules"/>, the day's list; a rule with no summary prints no row), one per row.</summary>
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
        SetOpen(false);
    }

    /// <summary>The world bounds of the row printing directive <paramref name="ruleIndex"/> (a match line meets it there); false when it prints no row or the card is hidden.</summary>
    public bool TryRowBounds(int ruleIndex, out Bounds bounds)
    {
        bounds = default;
        int row = _indices.IndexOf(ruleIndex);
        if (!isActiveAndEnabled || row < 0 || row >= rows.Length || rows[row] == null || !rows[row].TryGetComponent(out Collider box))
            return false;
        bounds = box.bounds;
        return true;
    }

    private void Click(int row)
    {
        if (row < _indices.Count)
            RowClicked?.Invoke(_indices[row], _rules[_indices[row]]);
    }
}
