using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The Investigation app's Documents tab (the PC redesign AP5, AP6, §2.4): a
/// chip per paper of the case, in paper order, reading where the paper is
/// ("on the desk", "not handed over"; a scanned one reads its name alone and
/// is available), and the chosen paper's scanned copy. A paper not scanned
/// shows why instead (scan it on the desk, or ask the traveller for it); with
/// nothing chosen the view says what arrives here. Today each copy is drawn
/// by the scanned-page component (DocumentWindowController, one clone of the
/// page template per paper); phase 5's FormView takes its place.
/// CaseDocumentsPresenter fills it; nothing here opens or switches by itself.
/// </summary>
public sealed class DocumentsView : AppView
{
    /// <summary>The scanned page (inactive), cloned per paper of the case.</summary>
    [SerializeField] private DocumentWindowController pageTemplate;

    /// <summary>The line shown instead of a copy: nothing chosen, or a paper not scanned yet.</summary>
    [SerializeField] private TMP_Text hintText;

    private readonly List<DocumentWindowController> _pages = new List<DocumentWindowController>();
    private readonly List<string> _names = new List<string>();
    private readonly List<AppChip> _chips = new List<AppChip>();
    private CasePapers _papers = new CasePapers(0);
    private int _selected = -1;

    /// <inheritdoc />
    public override AppTab Tab => AppTab.Documents;

    /// <inheritdoc />
    public override IReadOnlyList<AppChip> Chips => _chips;

    /// <inheritdoc />
    public override int Selected => _selected;

    /// <summary>True when the page template is wired (a case's papers can be shown).</summary>
    public bool Ready => pageTemplate != null;

    /// <summary>True when the chosen paper's scanned copy shows (it is scanned), not the line saying why it cannot.</summary>
    public bool ShowsCopy => _selected >= 0 && _papers.State(_selected) == PaperState.Scanned;

    /// <summary>
    /// A new case: one scanned page per paper (hidden until chosen), bound to
    /// its document (a photo paper shows <paramref name="look"/>), the chips
    /// from <paramref name="papers"/>, nothing chosen.
    /// </summary>
    public void SetCase(IReadOnlyList<DocumentInstance> documents, CasePapers papers, CompareController compare, TravellerLook look, CharacterArt art)
    {
        Clear();
        _papers = papers ?? new CasePapers(0);
        if (pageTemplate != null && documents != null)
            for (int i = 0; i < documents.Count; i++)
            {
                DocumentWindowController page = Instantiate(pageTemplate, pageTemplate.transform.parent);
                page.gameObject.name = "Page_" + i;
                page.gameObject.SetActive(false);
                page.SetDocument(documents[i], i, compare, look, art);
                _pages.Add(page);
                DocumentInstance doc = documents[i];
                _names.Add(doc != null && doc.template != null ? doc.template.displayName : UiText.Get("document.untitled"));
            }
        Refresh();
    }

    /// <summary>The papers moved (handed over, scanned): the chips and the shown paper follow.</summary>
    public void Refresh()
    {
        _chips.Clear();
        for (int i = 0; i < _names.Count; i++)
            _chips.Add(Chip(i));
        ShowSelected();
        RaiseChipsChanged();
    }

    /// <summary>The case ended or a new one starts: the pages go, nothing is chosen.</summary>
    public void Clear()
    {
        foreach (DocumentWindowController page in _pages)
            if (page != null)
                Destroy(page.gameObject);
        _pages.Clear();
        _names.Clear();
        _chips.Clear();
        _papers = new CasePapers(0);
        _selected = -1;
        ShowSelected();
        RaiseChipsChanged();
    }

    /// <inheritdoc />
    public override void Select(int index)
    {
        _selected = index >= 0 && index < _pages.Count ? index : -1;
        ShowSelected();
        RaiseChipsChanged();
    }

    /// <summary>A paper's chip: its name when scanned (available), else where it is.</summary>
    private AppChip Chip(int index)
    {
        switch (_papers.State(index))
        {
            case PaperState.Scanned:
                return new AppChip(_names[index], true);
            case PaperState.OnDesk:
                return new AppChip(UiText.Format("app.chip.onDesk", _names[index]), false);
            default:
                return new AppChip(UiText.Format("app.chip.notHandedOver", _names[index]), false);
        }
    }

    /// <summary>Shows the chosen paper's copy when it is scanned, else the line saying why it cannot show.</summary>
    private void ShowSelected()
    {
        PaperState state = _selected >= 0 ? _papers.State(_selected) : PaperState.NotHandedOver;
        bool showCopy = _selected >= 0 && state == PaperState.Scanned;
        for (int i = 0; i < _pages.Count; i++)
            if (_pages[i] != null && _pages[i].gameObject.activeSelf != (showCopy && i == _selected))
                _pages[i].gameObject.SetActive(showCopy && i == _selected);

        if (hintText == null)
            return;
        hintText.gameObject.SetActive(!showCopy);
        hintText.text = _selected < 0 ? UiText.Get("app.doc.none")
            : state == PaperState.OnDesk ? UiText.Get("app.doc.scanHint")
            : UiText.Get("app.doc.askHint");
    }
}
