using TMPro;
using UnityEngine;

/// <summary>
/// The Investigation app's search (redesign phase 19; the PC spec's SE1-SE6;
/// the PC workbench spec IA9): the app owns the index (CaseIndex: its day
/// layer set by DayReference, its case layer by the presenters and emptied
/// at each case's start and end) and the search drawer (SearchBox: from the
/// right over a dim, opened by the shelf's Search button, Ctrl+K and
/// Ctrl+F; its field, the pinned and recent items before anything is typed,
/// the results grouped by source as the player types; the keys'
/// SearchFieldChip hands a pasted untranslated line in through SetChip).
/// Escape clears the field, then closes the drawer; a press on the dim closes
/// it. ↓ in the field takes the focus ring into the hits (the Results
/// region; ↑ on the first goes back to the field). A chosen hit opens on the
/// target side (SmartLinks.ForEntry, then that pane's one navigation: the
/// item shown, a filter that hides the row lifted, a form's box scrolled to
/// the middle, the row marked found, the place recorded in the pane's
/// history; a Ctrl+click or Ctrl+Enter on the other side), goes first in
/// Recent, closes the drawer and takes the focus ring (Space picks it next).
/// </summary>
public sealed partial class InvestigationApp
{
    [Header("Search")]
    /// <summary>The search drawer's field, results and debounce.</summary>
    [SerializeField] private SearchBox searchBox;

    private readonly CaseIndex _index = new CaseIndex();

    /// <summary>The search index: its day layer (DayReference) and the case's layer (the presenters).</summary>
    public CaseIndex Index => _index;

    /// <summary>True while the search drawer is open (Escape closes it, once its field is empty).</summary>
    public bool ResultsOpen => SearchOpen;

    /// <summary>True while the search drawer is open.</summary>
    public bool SearchOpen => searchBox != null && searchBox.IsOpen;

    /// <summary>True while the drawer lists hits (↓ in the field goes into them; Tab visits them).</summary>
    public bool ResultsListed => searchBox != null && searchBox.HitRows.Count > 0;

    /// <summary>Opens the search drawer, its field empty and ready (the shelf's Search button, Ctrl+K).</summary>
    public void OpenSearch()
    {
        Init();
        if (searchBox != null)
            searchBox.Open();
    }

    /// <summary>Escape's CloseResults (and a press on the dim): the drawer closes; the ring goes back to the panes.</summary>
    public void CloseResults()
    {
        if (searchBox != null)
            searchBox.CloseResults();
        if (_region == AppRegion.Search || _region == AppRegion.Results)
            SetRegion(AppFocus.Home(FocusState), 0, _ringOn);
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

    /// <summary>A press on the desktop outside the drawer's panel closes the drawer.</summary>
    private void PressedForSearch(GameObject top)
    {
        if (searchBox != null && searchBox.IsOpen && !searchBox.IsPart(top))
            CloseResults();
    }

    /// <summary>Wires the drawer to the index and the jumps (Init, once).</summary>
    private void InitSearch()
    {
        if (searchBox == null)
            return;
        searchBox.Bind(_index);
        searchBox.Opened += Jump;
    }

    /// <summary>A case starts or ends: the case layer empties and the drawer closes.</summary>
    private void ResetSearchCase()
    {
        _index.EndCase();
        if (searchBox != null)
            searchBox.EndCase();
    }

    /// <summary>
    /// A result was opened (SE4): its place on the target side, or the other
    /// one (<paramref name="otherPane"/>: Ctrl+click, Ctrl+Enter), as a pin's
    /// jump goes there (a rule or a deviation, which have no row, opens its
    /// source), the hit first in Recent, the focus ring on its row; a hit
    /// whose item is gone says so.
    /// </summary>
    private void Jump(SearchHit hit, bool otherPane)
    {
        IndexEntry e = hit.Entry;
        OpenWindow();
        LinkTarget target = SmartLinks.ForEntry(e.Key, _papers);
        if (target.IsNone)
            target = LinkTarget.ToTab(e.Source);
        if (otherPane && _split)
            SetTarget(Other(TargetPane));
        if (!OpenOnTarget(target))
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
