using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The Investigation app's search field (the PC redesign SE1, SE3, SE5): as
/// the player types, the results update after a short pause
/// (DesktopConfigSO's debounce) from two characters or one digit, grouped by
/// source in the tab order, each group capped (a chip or "Show all" filters
/// to one source); Enter opens the first hit. A pasted foreign clip is a chip
/// (SetChip, from the clipboard of redesign phase 20) that matches only equal
/// untranslated lines of its tongue. It reads the app's index (CaseIndex) and
/// fills the results panel (SearchResultsView); a chosen hit is Opened for
/// the app to jump to.
/// </summary>
public sealed class SearchBox : MonoBehaviour
{
    /// <summary>The toolbar's text field.</summary>
    [SerializeField] private TMP_InputField field;

    /// <summary>The results panel.</summary>
    [SerializeField] private SearchResultsView results;

    /// <summary>The desktop's knobs: the debounce and the hits per group.</summary>
    [SerializeField] private DesktopConfigSO config;

    private CaseIndex _index;
    private SearchChip? _chip;
    private AppTab? _only;
    private TMP_FontAsset _script;
    private float _due = -1f;
    private bool _wired;

    /// <summary>Raised when the player opens a hit (a click, or Enter on the first).</summary>
    public event Action<SearchHit> Opened;

    /// <summary>Searches <paramref name="index"/> (the app's); wires the field and the panel once.</summary>
    public void Bind(CaseIndex index)
    {
        _index = index;
        if (_wired)
            return;
        _wired = true;
        if (field != null)
        {
            field.onValueChanged.AddListener(_ => Typed());
            field.onSubmit.AddListener(_ => OpenFirst());
        }
        if (results != null)
        {
            results.Chosen += Open;
            results.Filtered += source =>
            {
                _only = source;
                Run();
            };
        }
        enabled = false;
    }

    /// <summary>Puts the keyboard into the field (Ctrl+F).</summary>
    public void Focus()
    {
        if (field == null || !field.gameObject.activeInHierarchy)
            return;
        field.Select();
        field.ActivateInputField();
    }

    /// <summary>Sets the typed text (a paste) and shows its results at once.</summary>
    public void SetQuery(string text)
    {
        if (field != null)
            field.SetTextWithoutNotify(text ?? string.Empty);
        _only = null;
        Run();
    }

    /// <summary>Sets the pasted foreign clip (null removes it) and shows the results at once.</summary>
    public void SetChip(SearchChip? chip)
    {
        _chip = chip;
        _only = null;
        Run();
    }

    /// <summary>The current traveller's script font: an untranslated snippet is drawn in it (null: the text's own font).</summary>
    public void SetScript(TMP_FontAsset font) => _script = font;

    /// <summary>The case ended: the results close, the clip goes, the typed text stays for the next traveller.</summary>
    public void EndCase()
    {
        _chip = null;
        _due = -1f;
        enabled = false;
        if (results != null)
            results.Hide();
    }

    /// <summary>Typing: the results update after the pause (the filter goes back to All).</summary>
    private void Typed()
    {
        _only = null;
        _due = Time.unscaledTime + (config != null ? config.searchDebounceSeconds : 0.15f);
        enabled = true;
    }

    /// <summary>Runs the pending search once its pause is over.</summary>
    private void Update()
    {
        if (_due >= 0f && Time.unscaledTime >= _due)
            Run();
    }

    /// <summary>
    /// Searches now: the panel shows the groups (every source's for the
    /// chips; the filtered source's in full), or hides while the query is too
    /// short (SearchQuery.IsSearchable).
    /// </summary>
    private void Run()
    {
        _due = -1f;
        enabled = false;
        if (results == null || _index == null)
            return;
        string typed = field != null ? field.text : string.Empty;
        SearchQuery q = SearchQuery.Parse(typed, _chip);
        if (!q.IsSearchable)
        {
            results.Hide();
            return;
        }
        IReadOnlyList<AppTab> order = TabOrder.Default;
        IReadOnlyList<ResultGroup> all = _index.Search(q, order, config != null ? config.searchPerGroup : 5, null);
        IReadOnlyList<ResultGroup> shown = _only.HasValue ? _index.Search(q, order, int.MaxValue, _only) : all;
        results.Show(typed, _chip.HasValue, all, shown, _only, _script);
    }

    /// <summary>Enter: a pending search runs, then its first hit opens.</summary>
    private void OpenFirst()
    {
        if (_due >= 0f)
            Run();
        if (results != null && results.IsOpen && results.TryFirst(out SearchHit hit))
            Open(hit);
    }

    /// <summary>A hit is opened: the panel closes and the app jumps there.</summary>
    private void Open(SearchHit hit)
    {
        if (results != null)
            results.Hide();
        Opened?.Invoke(hit);
    }
}
