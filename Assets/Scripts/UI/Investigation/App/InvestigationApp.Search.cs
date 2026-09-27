using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Investigation app's search (redesign phase 19; the PC spec's SE1-SE6):
/// the app owns the index (CaseIndex: its day layer set by DayReference, its
/// case layer by the presenters and emptied at each case's start and end),
/// the toolbar's search field (SearchBox; the keys' SearchFieldChip hands a
/// pasted untranslated line in through SetChip, and their Ctrl+F focuses the
/// field) and its results panel (the Escape chain's CloseResults closes it
/// before the field is cleared). A chosen hit jumps as a pin does (the
/// view's IAppItems.Reveal: the app on its tab, the item shown, a filter that
/// hides the row lifted), goes first in Recent, puts the focus ring on its
/// row (Space picks it next), scrolls the row to the middle of its page and
/// flashes it (FoundMark, gone at the next click or navigation).
/// </summary>
public sealed partial class InvestigationApp
{
    [Header("Search")]
    /// <summary>The toolbar's search field's results and debounce.</summary>
    [SerializeField] private SearchBox searchBox;

    /// <summary>The found flash (inactive), cloned over what a result opens.</summary>
    [SerializeField] private FoundMark foundTemplate;

    private readonly CaseIndex _index = new CaseIndex();
    private FoundMark _found;

    /// <summary>The search index: its day layer (DayReference) and the case's layer (the presenters).</summary>
    public CaseIndex Index => _index;

    /// <summary>True while the results panel shows (Escape closes it before it clears the field).</summary>
    public bool ResultsOpen => searchBox != null && searchBox.ResultsOpen;

    /// <summary>Escape's CloseResults: the results panel closes; the field keeps its text and the keyboard.</summary>
    public void CloseResults()
    {
        if (searchBox != null)
            searchBox.CloseResults();
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
    /// A result was opened (SE4): the app on its tab, the item shown (as a
    /// pin's jump shows it), the hit first in Recent, the focus ring on its
    /// row, the row in the middle of its page, flashed; a source without rows
    /// (the Report, the Rules) shows and flashes whole.
    /// </summary>
    private void Jump(SearchHit hit)
    {
        IndexEntry e = hit.Entry;
        if (window != null)
            window.Open();
        pane.Show(e.Source);
        if (pane.View(e.Source) is IAppItems items && !items.Reveal(e.Key))
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
        ClearFound();
        AppRow row = FocusedRow();
        RectTransform target = row != null && row.Key == e.Key ? (RectTransform)row.transform : ViewRect(e.Source);
        Centre(target);
        _found = FoundMark.Place(foundTemplate, target, MotionPreference.Reduced);
    }

    /// <summary>The tab's view as a rect (the whole view flashes when the hit has no row), or null.</summary>
    private RectTransform ViewRect(AppTab tab) => pane.View(tab) is Component view ? (RectTransform)view.transform : null;

    /// <summary>
    /// The row scrolled to the middle of its view's own scroll (a scanned
    /// copy's page; FoundFlash.CentredScroll); a row in no scroll of its own
    /// is already in view (the ring brought it there).
    /// </summary>
    private static void Centre(RectTransform target)
    {
        ScrollRect scroll = target != null ? target.GetComponentInParent<ScrollRect>() : null;
        if (scroll == null || scroll.content == null || scroll.GetComponent<PaneZoom>() != null)
            return;
        RectTransform viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
        Bounds row = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.content, target);
        scroll.StopMovement();
        scroll.verticalNormalizedPosition = FoundFlash.CentredScroll(scroll.content.rect.height, viewport.rect.height, scroll.content.rect.yMax - row.center.y);
    }

    /// <summary>The found flash goes (the next navigation).</summary>
    private void ClearFound()
    {
        if (_found != null)
            Destroy(_found.gameObject);
        _found = null;
    }
}
