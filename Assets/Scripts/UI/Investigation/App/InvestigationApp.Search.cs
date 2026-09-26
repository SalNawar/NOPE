using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The Investigation app's search (redesign phase 19; the PC spec's SE1-SE6):
/// the app owns the index (CaseIndex: its day layer set by DayReference, its
/// case layer by the presenters and emptied at each case's start and end),
/// the toolbar's search field (SearchBox) and its results panel. A chosen
/// hit opens the app on its tab and shows the item (the view turns to it,
/// lifting a filter that hides it, and scrolls it to the middle), gives its
/// row the keyboard focus (Space picks it next) and flashes it (FoundMark,
/// removed by the next click or navigation). Ctrl+F and the clipboard's
/// paste (redesign phase 20) come in through FocusSearch, SetQuery and
/// SetChip.
/// </summary>
public sealed partial class InvestigationApp
{
    [Header("Search")]
    /// <summary>The toolbar's search field.</summary>
    [SerializeField] private SearchBox searchBox;

    /// <summary>The found flash (inactive), cloned over what a result opens.</summary>
    [SerializeField] private FoundMark foundTemplate;

    private readonly CaseIndex _index = new CaseIndex();
    private FoundMark _found;

    /// <summary>The search index: its day layer (DayReference) and the case's layer (the presenters).</summary>
    public CaseIndex Index => _index;

    /// <summary>Puts the keyboard into the search field (Ctrl+F, redesign phase 20).</summary>
    public void FocusSearch()
    {
        Init();
        if (searchBox != null)
            searchBox.Focus();
    }

    /// <summary>Searches for <paramref name="text"/> at once (a paste into the search field, redesign phase 20).</summary>
    public void SetQuery(string text)
    {
        Init();
        if (searchBox != null)
            searchBox.SetQuery(text);
    }

    /// <summary>Searches with a pasted foreign clip (null removes it): only equal untranslated lines of its tongue match (SE5; the clipboard of redesign phase 20).</summary>
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
        if (searchBox != null)
        {
            searchBox.Bind(_index);
            searchBox.Opened += Jump;
        }
        if (pane != null)
            pane.Shown += _ => ClearFound();
    }

    /// <summary>A case starts or ends: the case layer empties, the results close and the found flash goes.</summary>
    private void ResetSearchCase()
    {
        _index.EndCase();
        if (searchBox != null)
            searchBox.EndCase();
        ClearFound();
    }

    /// <summary>
    /// A result was opened (SE4): the app on its tab (the active pane; the
    /// second pane is redesign phase 18's), the item shown and flashed, its
    /// row focused.
    /// </summary>
    private void Jump(SearchHit hit)
    {
        IndexEntry e = hit.Entry;
        if (window != null)
            window.Open();
        pane.Show(e.Source);
        FoundTarget found = Reveal(e);
        ClearFound();
        _found = FoundMark.Place(foundTemplate, found.Rect, MotionPreference.Reduced);
        if (found.Focus != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(found.Focus.gameObject);
    }

    /// <summary>The entry's item shown by its tab's view, and what is found there (the whole view for the Report and the Rules, which have no rows yet).</summary>
    private FoundTarget Reveal(IndexEntry e)
    {
        IAppView view = pane.View(e.Source);
        var component = view as Component;
        if (component == null)
            return default;
        switch (e.Source)
        {
            case AppTab.Documents:
                return view is DocumentsView documents ? documents.Reveal(e.Item, e.Row) : default;
            case AppTab.Records:
                CitizenRecordsWindowController records = component.GetComponent<CitizenRecordsWindowController>();
                return records != null ? records.Reveal(e.Item, e.Row) : default;
            case AppTab.Reference:
                return view is ReferenceView reference ? reference.Reveal(e.Item, e.Key) : default;
            case AppTab.Transcript:
                TranscriptWindowController transcript = component.GetComponent<TranscriptWindowController>();
                return transcript != null ? transcript.RevealLine(e.Item) : default;
            default:
                return new FoundTarget((RectTransform)component.transform, null);
        }
    }

    /// <summary>The found flash goes (the next navigation).</summary>
    private void ClearFound()
    {
        if (_found != null)
            Destroy(_found.gameObject);
        _found = null;
    }
}
