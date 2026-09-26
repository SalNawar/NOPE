using TMPro;
using UnityEngine;

/// <summary>
/// The Investigation app's search (redesign phase 19; the PC spec's SE1-SE6):
/// the app owns the index (CaseIndex: its day layer set by DayReference, its
/// case layer by the presenters and emptied at each case's start and end),
/// the toolbar's search field (SearchBox, with phase 20's Ctrl+F and paste
/// chip: SearchFieldChip hands a pasted foreign clip in through SetChip) and
/// its results panel. A chosen hit goes through the panes' one navigation
/// (phase 18's LinkTarget, as a smart link does): the active pane shows its
/// tab and the item (the view turns to it, lifting a filter that hides it,
/// scrolling a form's box to the middle, recorded in the pane's history) with
/// the found outline, and the focus ring goes on its row (phase 20: Space
/// picks it next).
/// </summary>
public sealed partial class InvestigationApp
{
    [Header("Search")]
    /// <summary>The toolbar's search field's results and debounce.</summary>
    [SerializeField] private SearchBox searchBox;

    private readonly CaseIndex _index = new CaseIndex();

    /// <summary>The search index: its day layer (DayReference) and the case's layer (the presenters).</summary>
    public CaseIndex Index => _index;

    /// <summary>Searches with a pasted foreign clip (null removes it): only equal untranslated lines of its tongue match (SE5; the search field's chip, phase 20).</summary>
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

    /// <summary>A result was opened (SE4): its place in the active pane (as a link goes there), then the focus ring on its row.</summary>
    private void Jump(SearchHit hit)
    {
        IndexEntry e = hit.Entry;
        Open(TargetFor(e), false);
        SetRegion(AppRegion.PaneContent, 0, false);
        FocusRow(e.Key);
    }

    /// <summary>Where an index entry is, as the panes' navigation reads it: a paper or its field, a record (by its number) and its row, a book's row, a line, else its tab.</summary>
    private static LinkTarget TargetFor(IndexEntry e)
    {
        switch (e.Source)
        {
            case AppTab.Documents:
                return LinkTarget.ToRow(AppTab.Documents, e.Row >= 0 ? e.Key : null, e.Item);
            case AppTab.Records:
                if (EntryKeys.TryRecordRow(e.Key, out string id, out ClueCategory category))
                    return LinkTarget.ToRecords(id, category);
                return EntryKeys.TryRecordCard(e.Key, out id) ? LinkTarget.ToRecords(id) : LinkTarget.ToTab(AppTab.Records);
            case AppTab.Reference:
            case AppTab.Transcript:
                return LinkTarget.ToRow(e.Source, e.Key, e.Item);
            default:
                return LinkTarget.ToTab(e.Source);
        }
    }
}
