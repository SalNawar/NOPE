using TMPro;
using UnityEngine;

/// <summary>
/// The Investigation app's search (redesign phase 19; the PC spec's SE1-SE6):
/// the app owns the index (CaseIndex: its day layer set by DayReference, its
/// case layer by the presenters and emptied at each case's start and end),
/// the toolbar's search field (SearchBox; the keys' SearchFieldChip hands a
/// pasted untranslated line in through SetChip, and their Ctrl+F focuses the
/// field) and its results panel (the Escape chain's CloseResults closes it
/// before the field is cleared). ↓ in the field takes the focus ring into
/// the hits (the Results region; ↑ on the first goes back to the field).
/// A chosen hit jumps as a pin does (in the other pane while split for a
/// Ctrl+click or Ctrl+Enter, as a row's link does; phase 18)
/// (SmartLinks.ForEntry, then that pane's one navigation: the item
/// shown, a filter that hides the row lifted, a form's box scrolled to the
/// middle, the row marked found, the place recorded in the pane's history),
/// goes first in Recent and takes the focus ring (Space picks it next).
/// </summary>
public sealed partial class InvestigationApp
{
    [Header("Search")]
    /// <summary>The toolbar's search field's results and debounce.</summary>
    [SerializeField] private SearchBox searchBox;

    private readonly CaseIndex _index = new CaseIndex();

    /// <summary>The search index: its day layer (DayReference) and the case's layer (the presenters).</summary>
    public CaseIndex Index => _index;

    /// <summary>True while the results panel shows (Escape closes it before it clears the field).</summary>
    public bool ResultsOpen => searchBox != null && searchBox.ResultsOpen;

    /// <summary>True while the results panel lists hits (↓ in the field goes into them; Tab visits them).</summary>
    public bool ResultsListed => searchBox != null && searchBox.HitRows.Count > 0;

    /// <summary>Escape's CloseResults: the results panel closes; the field keeps its text and the keyboard (the ring on a hit goes back to it).</summary>
    public void CloseResults()
    {
        if (searchBox != null)
            searchBox.CloseResults();
        if (_region == AppRegion.Results)
            SetRegion(AppRegion.Search, 0, _ringOn);
    }

    /// <summary>Searches with a pasted foreign clip (null removes it): only equal untranslated lines of its tongue match (SE5; the search field's chip).</summary>
    public void SetChip(SearchChip? chip)
    {
        Init();
        if (searchBox != null)
            searchBox.SetChip(chip);
    }

    /// <summary>The current traveller's script font (their untranslated lines' snippets are drawn in it; null: the text's own font).</summary>
    public void SetSpeechScript(TMP_FontAsset font)
    {
        if (searchBox != null)
            searchBox.SetScript(font);
    }

    /// <summary>A press on the desktop outside the search field and its panels closes them (the palette: the PC UX redesign IA9).</summary>
    private void PressedForSearch(GameObject top)
    {
        if (searchBox != null && searchBox.ResultsOpen && !searchBox.IsPart(top))
            CloseResults();
    }

    /// <summary>Wires the search field to the index and the jumps (Init, once).</summary>
    private void InitSearch()
    {
        if (searchBox == null)
            return;
        searchBox.Bind(_index);
        searchBox.Opened += Jump;
    }

    /// <summary>A case starts or ends: the case layer empties and the results close.</summary>
    private void ResetSearchCase()
    {
        _index.EndCase();
        if (searchBox != null)
            searchBox.EndCase();
    }

    /// <summary>
    /// A result was opened (SE4): its place in the active pane, or the other
    /// one while split (<paramref name="otherPane"/>: Ctrl+click, Ctrl+Enter;
    /// it becomes the active one), as a pin's jump goes there (a rule or a
    /// deviation, which have no row yet, opens its tab), the hit first in
    /// Recent, the focus ring on its row; a hit whose item is gone says so.
    /// </summary>
    private void Jump(SearchHit hit, bool otherPane)
    {
        IndexEntry e = hit.Entry;
        if (window != null)
            window.Open();
        LinkTarget target = SmartLinks.ForEntry(e.Key, _papers);
        if (target.IsNone)
            target = LinkTarget.ToTab(e.Source);
        AppPane pane = otherPane && _split ? Other(ActivePane) : ActivePane;
        bool there = pane.Go(target);
        Activate(pane);
        if (!there)
        {
            Notice(UiText.Get("app.jump.gone"));
            return;
        }
        if (EntryKeys.TryRef(e.Key, out EntryRef entry))
        {
            _recent.Touch(entry, e.Title);
            DrawLists();
        }
        FocusRow(e.Key);
    }
}
