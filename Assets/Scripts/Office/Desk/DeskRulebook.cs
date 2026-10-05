using System;
using System.Collections.Generic;
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
/// Build Office UI builds the card and its rows (a fixed number: more
/// directives than rows print only the first ones); the office binder lays it
/// on the desk.
/// </summary>
public sealed class DeskRulebook : MonoBehaviour
{
    /// <summary>The card's title ("TODAY'S RULES").</summary>
    [SerializeField] private TMP_Text title;

    /// <summary>The rows, top first: each a click box (Clickable, a collider) with its text as its child.</summary>
    [SerializeField] private Clickable[] rows = Array.Empty<Clickable>();

    /// <summary>The line printed when the day has no directive.</summary>
    [SerializeField] private TMP_Text none;

    private readonly List<int> _indices = new List<int>();
    private IReadOnlyList<TravelRuleSO> _rules = Array.Empty<TravelRuleSO>();

    /// <summary>Raised when a row is clicked: the directive's index in the day's list and the rule.</summary>
    public event Action<int, TravelRuleSO> RowClicked;

    /// <summary>The rows' clicks (the booth makes them live with the props).</summary>
    public IReadOnlyList<Clickable> Rows => rows;

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
        Show(_rules);
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
