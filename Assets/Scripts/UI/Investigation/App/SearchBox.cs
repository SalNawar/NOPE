using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Investigation app's search field (the PC redesign SE1, SE3, SE5): as
/// the player types, the results update after a short pause
/// (DesktopConfigSO's debounce) from two characters or one digit, grouped by
/// source in the tab order, each group capped (a chip or "Show all" filters
/// to one source); Enter opens the first hit (Ctrl+Enter: in the other pane),
/// and the keys' focus ring opens the hit it is on (OpenRow). A pasted foreign clip is a chip
/// (SetChip, from the keys' SearchFieldChip on the same field) that matches
/// only equal untranslated lines of its tongue. It reads the app's index
/// (CaseIndex) and fills the results panel (SearchResultsView); a chosen hit
/// is Opened for the app to jump to. The field lives in the search drawer
/// (the PC workbench spec IA9): Open shows the drawer from the right over a
/// dim, the field empty and focused, the quick-open panel (the pinned and
/// the recent items, InvestigationApp.Keys' lists) showing until something
/// is typed; typing turns it into the results; Close, a press on the dim,
/// Escape (CloseResults, once the field is empty), a jump or a chosen hit
/// closes the drawer.
/// </summary>
public sealed class SearchBox : MonoBehaviour
{
    /// <summary>The drawer (the dim and the panel), hidden while closed.</summary>
    [SerializeField] private GameObject drawer;

    /// <summary>The drawer's panel (a press inside it keeps the drawer open).</summary>
    [SerializeField] private RectTransform panel;

    /// <summary>The dim over the app (a press on it closes the drawer).</summary>
    [SerializeField] private Button dimButton;

    /// <summary>The drawer's Close.</summary>
    [SerializeField] private Button closeButton;

    /// <summary>The drawer's text field.</summary>
    [SerializeField] private TMP_InputField field;

    /// <summary>The results panel.</summary>
    [SerializeField] private SearchResultsView results;

    /// <summary>The desktop's knobs: the debounce and the hits per group.</summary>
    [SerializeField] private DesktopConfigSO config;

    /// <summary>The quick-open panel (Pinned and Recent), shown under the field while it is empty.</summary>
    [SerializeField] private GameObject quickOpen;

    private CaseIndex _index;
    private SearchChip? _chip;
    private AppTab? _only;
    private TMP_FontAsset _script;
    private float _due = -1f;
    private bool _wired;

    /// <summary>Raised when the player opens a hit (a click, Enter on the first or on the focused one): the hit, and true to open it in the other pane (Ctrl held).</summary>
    public event Action<SearchHit, bool> Opened;

    /// <summary>True while the drawer is open.</summary>
    public bool IsOpen => drawer != null && drawer.activeSelf;

    /// <summary>True while the quick-open panel shows.</summary>
    public bool QuickOpenShowing => quickOpen != null && quickOpen.activeInHierarchy;

    /// <summary>True for the drawer's panel and what is in it (a press there leaves the drawer open; a press on the dim does not).</summary>
    public bool IsPart(GameObject go) => go != null && panel != null && go.transform.IsChildOf(panel);

    /// <summary>The listed hits' rows (none while the panel is closed): the keys' Results region.</summary>
    public IReadOnlyList<Button> HitRows => results != null && results.IsOpen ? results.HitRows : (IReadOnlyList<Button>)Array.Empty<Button>();

    /// <summary>Opens the hit of <paramref name="row"/> (the focus ring's), in the other pane when <paramref name="otherPane"/>.</summary>
    public void OpenRow(Component row, bool otherPane)
    {
        if (results != null)
            results.Choose(row, otherPane);
    }

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
        if (dimButton != null)
            dimButton.onClick.AddListener(CloseResults);
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseResults);
        if (drawer != null)
            drawer.SetActive(false);
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

    /// <summary>Opens the drawer, its field empty and focused, the pinned and recent items showing.</summary>
    public void Open()
    {
        if (drawer == null)
            return;
        drawer.SetActive(true);
        drawer.transform.SetAsLastSibling();
        _chip = null;
        _only = null;
        if (field != null)
        {
            field.SetTextWithoutNotify(string.Empty);
            field.Select();
            field.ActivateInputField();
        }
        if (results != null)
            results.Hide();
        ShowQuick();
    }

    /// <summary>Sets the pasted foreign clip (null removes it; the drawer opens) and shows the results at once.</summary>
    public void SetChip(SearchChip? chip)
    {
        if (drawer != null && !drawer.activeSelf)
        {
            drawer.SetActive(true);
            drawer.transform.SetAsLastSibling();
        }
        _chip = chip;
        _only = null;
        ShowQuick();
        Run();
    }

    /// <summary>The current traveller's script font: an untranslated snippet is drawn in it (null: the text's own font).</summary>
    public void SetScript(TMP_FontAsset font) => _script = font;

    /// <summary>Closes the drawer (Close, the dim, Escape, a jump).</summary>
    public void CloseResults()
    {
        _due = -1f;
        enabled = false;
        if (results != null)
            results.Hide();
        if (field != null && field.isFocused)
            field.DeactivateInputField();
        if (drawer != null)
            drawer.SetActive(false);
    }

    /// <summary>The case ended: the results close, the clip goes, the typed text stays for the next traveller.</summary>
    public void EndCase()
    {
        _chip = null;
        CloseResults();
    }

    /// <summary>Typing: the results update after the pause (the filter goes back to All); the quick-open panel shows only while the field is empty.</summary>
    private void Typed()
    {
        ShowQuick();
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

    /// <summary>Enter: a pending search runs, then its first hit opens (Ctrl+Enter: in the other pane).</summary>
    private void OpenFirst()
    {
        if (_due >= 0f)
            Run();
        if (results != null && results.IsOpen && results.TryFirst(out SearchHit hit))
            Open(hit, SearchResultsView.CtrlHeld());
    }

    /// <summary>A hit is opened: the panel closes and the app jumps there (in the other pane when <paramref name="otherPane"/>).</summary>
    private void Open(SearchHit hit, bool otherPane)
    {
        CloseResults();
        Opened?.Invoke(hit, otherPane);
    }

    /// <summary>The quick-open panel shows while the drawer is open and the field holds no text and no pasted chip.</summary>
    private void ShowQuick() => SetQuick(IsOpen && field != null && field.text.Length == 0 && !_chip.HasValue);

    /// <summary>Shows or hides the quick-open panel.</summary>
    private void SetQuick(bool on)
    {
        if (quickOpen != null && quickOpen.activeSelf != on)
            quickOpen.SetActive(on);
    }
}
