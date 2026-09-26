using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The Investigation app's Documents tab (the PC redesign AP5, AP6, §2.4): a
/// chip per paper of the case, in paper order, reading where the paper is
/// ("on the desk", "not handed over"; a scanned one reads its name alone and
/// is available), and the chosen paper's scanned copy. A paper not scanned
/// shows why instead (scan it on the desk, or ask the traveller for it); with
/// nothing chosen the view says what arrives here. Each copy is the paper's
/// form (phase 5): a clone of the scanned-page template per paper
/// (DocumentWindowController over a FormView), drawn from the same
/// DocumentForm the desk paper prints; its fields link (SmartLinks). A link,
/// Back or a dock side reveals a paper's field (Reveal: the paper chosen, the
/// field scrolled to and outlined). Each pane has one; CaseDocumentsPresenter
/// fills them all; nothing here opens or switches by itself.
/// Its item is the chosen paper ("doc:0"); a jump shows a paper, or a
/// field's paper (IAppItems; the focus ring brings the box into view).
/// </summary>
public sealed class DocumentsView : AppView, IAppItems
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

    /// <inheritdoc />
    public string ItemKey => _selected >= 0 ? EntryKeys.Document(_selected) : null;

    /// <inheritdoc />
    public string ItemTitle => _selected >= 0 && _selected < _names.Count ? _names[_selected] : null;

    /// <inheritdoc />
    public bool Reveal(string key)
    {
        if (EntryKeys.TryDocument(key, out int paper) && paper < _pages.Count)
        {
            Select(paper);
            return true;
        }
        if (!EntryKeys.TryField(key, out paper, out _) || paper >= _pages.Count)
            return false;
        Select(paper);
        return true;
    }

    /// <summary>
    /// A new case: one scanned page per paper (hidden until chosen), bound to
    /// its document and drawing its paper's form (<paramref name="forms"/>, in
    /// paper order; a photo paper shows <paramref name="look"/>; the fields
    /// link by the case's <paramref name="claim"/>), the chips from
    /// <paramref name="papers"/>, nothing chosen.
    /// </summary>
    public void SetCase(IReadOnlyList<DocumentInstance> documents, IReadOnlyList<DocumentForm> forms, CasePapers papers, CompareController compare, TravellerLook look,
                        CharacterArt art, CaseClaim claim)
    {
        Clear();
        _papers = papers ?? new CasePapers(0);
        if (pageTemplate != null && documents != null)
            for (int i = 0; i < documents.Count; i++)
            {
                DocumentWindowController page = Instantiate(pageTemplate, pageTemplate.transform.parent);
                page.gameObject.name = "Page_" + i;
                page.gameObject.SetActive(false);
                page.SetDocument(documents[i], i, forms != null && i < forms.Count ? forms[i] : null, compare, look, art, claim);
                _pages.Add(page);
                _names.Add(documents[i] != null ? documents[i].DisplayName : UiText.Get("document.untitled"));
            }
        Refresh();
    }

    /// <summary>Paper <paramref name="index"/>'s copy arrived (its scan finished): its strip reads the time.</summary>
    public void MarkScanned(int index)
    {
        if (index >= 0 && index < _pages.Count && _pages[index] != null)
            _pages[index].MarkScanned();
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

    /// <summary>Chooses the target's paper (its item, else the paper its field key names) and outlines the field; no field: the outline clears.</summary>
    public override void Reveal(LinkTarget target)
    {
        int paper = target.Item;
        if (paper < 0 && PickKeys.TryField(target.Key, out int document, out _))
            paper = document;
        if (paper >= 0)
            Select(paper);
        if (_selected >= 0 && _selected < _pages.Count && _pages[_selected] != null)
            _pages[_selected].RevealField(target.Key);
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
