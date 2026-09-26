using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders one visitor document as a multi-page window. Each field is a
/// clickable row (label + value, DocumentRows.OnPage written through
/// DocumentRowView) that registers with the CompareController
/// (EvidencePicks.ForField). Every value shows in English, as filled (the
/// redesign's F5: documents are never in a tongue). Rows are cloned from
/// <see cref="fieldRowTemplate"/> (a disabled row with two TMP texts — label
/// then value — an Image background, and a Button). A document whose template
/// shows a photo carries the traveller's photo on its first page (the rows
/// there leave room for it). Each row is marked with its key for the keys,
/// the copy and the pins (AppRow); the keys turn the pages (IPagedRows) and a
/// jump to a field shows its page (ShowField).
/// </summary>
public sealed class DocumentWindowController : MonoBehaviour, IPagedRows
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text pageText;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Transform fieldRowsRoot;
    [SerializeField] private GameObject fieldRowTemplate;

    /// <summary>The photo's frame on the first page (hidden on other pages and documents).</summary>
    [SerializeField] private GameObject photoBox;

    /// <summary>The traveller's photo inside the frame.</summary>
    [SerializeField] private TravellerPortraitView photo;

    /// <summary>Extra right padding of the rows on a photo page, so no row runs under the photo.</summary>
    [SerializeField] private float photoInset = 120f;

    private DocumentInstance _doc;

    /// <summary>The document's index in the case (its fields' pick keys).</summary>
    private int _index;
    private CompareController _compare;
    private int _page;
    private bool _showsPhoto;
    private VerticalLayoutGroup _rowsLayout;
    private RectOffset _rowsPadding;
    private readonly List<GameObject> _rows = new();

    private void Awake()
    {

        if (prevButton != null)
            prevButton.onClick.AddListener(() => ShowPage(_page - 1));

        if (nextButton != null)
            nextButton.onClick.AddListener(() => ShowPage(_page + 1));

        if (fieldRowTemplate != null)
            fieldRowTemplate.SetActive(false);
    }

    /// <summary>Binds document <paramref name="index"/> of the case (with the traveller's look for a photo document) and renders its first page.</summary>
    public void SetDocument(DocumentInstance doc, int index, CompareController compare, TravellerLook look, CharacterArt art)
    {
        _doc = doc;
        _index = index;
        _compare = compare;
        _page = 0;
        _showsPhoto = doc != null && doc.template != null && doc.template.showsPhoto && look != null;

        if (photo != null)
        {
            if (_showsPhoto)
                photo.Show(look, art);
            else
                photo.Clear();
        }

        if (titleText != null)
            titleText.text = doc != null && doc.template != null ? doc.template.displayName : UiText.Get("document.untitled");

        ShowPage(0);
    }

    /// <inheritdoc />
    public bool TurnPage(int direction)
    {
        if (_doc == null || _page + direction < 0 || _page + direction >= _doc.PageCount)
            return false;
        ShowPage(_page + direction);
        return true;
    }

    /// <summary>Shows the page field <paramref name="field"/> (its index in the document's list) is on.</summary>
    public void ShowField(int field)
    {
        if (_doc != null && _doc.fields != null && field >= 0 && field < _doc.fields.Count && _doc.fields[field] != null)
            ShowPage(_doc.fields[field].page);
    }

    /// <summary>Switches to a page (clamped) and rebuilds its rows.</summary>
    public void ShowPage(int page)
    {
        if (_doc == null)
            return;

        int pages = _doc.PageCount;
        _page = Mathf.Clamp(page, 0, pages - 1);

        if (pageText != null)
            pageText.text = UiText.Format("window.page", _page + 1, pages);

        if (prevButton != null)
            prevButton.interactable = _page > 0;

        if (nextButton != null)
            nextButton.interactable = _page < pages - 1;

        bool photoPage = _showsPhoto && _page == 0;
        if (photoBox != null)
            photoBox.SetActive(photoPage);
        LeaveRoomForPhoto(photoPage);

        Rebuild();
    }

    /// <summary>
    /// Pads the rows on the right by <see cref="photoInset"/> on a photo page,
    /// else restores the authored padding (read once: the window is set up
    /// while still inactive, before Awake runs).
    /// </summary>
    private void LeaveRoomForPhoto(bool photoPage)
    {
        if (_rowsLayout == null && fieldRowsRoot != null)
        {
            _rowsLayout = fieldRowsRoot.GetComponent<VerticalLayoutGroup>();
            if (_rowsLayout != null)
                _rowsPadding = new RectOffset(_rowsLayout.padding.left, _rowsLayout.padding.right, _rowsLayout.padding.top, _rowsLayout.padding.bottom);
        }

        if (_rowsLayout != null)
            _rowsLayout.padding = new RectOffset(_rowsPadding.left, _rowsPadding.right + (photoPage ? (int)photoInset : 0), _rowsPadding.top, _rowsPadding.bottom);
    }

    private void Rebuild()
    {
        // Hidden at once (Destroy waits for the frame's end), so the keys never find a row that is going.
        foreach (GameObject r in _rows)
            if (r != null)
            {
                r.SetActive(false);
                Destroy(r);
            }

        _rows.Clear();

        if (_doc == null || fieldRowsRoot == null || fieldRowTemplate == null)
            return;

        string docName = _doc.template != null ? _doc.template.displayName : UiText.Get("document.untitled");
        foreach (DocumentRow docRow in DocumentRows.OnPage(_doc.fields, _page))
        {
            GameObject row = Instantiate(fieldRowTemplate, fieldRowsRoot);
            row.SetActive(true);
            _rows.Add(row);

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            DocumentRowView.Write(texts.Length > 0 ? texts[0] : null, texts.Length > 1 ? texts[1] : null, docRow);

            Image bg = row.GetComponent<Image>();
            Button btn = row.GetComponent<Button>();
            ComparePick pick = EvidencePicks.ForField(_index, docRow, docName);
            AppRow.Mark(row, AppTab.Documents, pick.Key, pick.Label, texts.Length > 0 ? texts[0] : null, texts.Length > 1 ? texts[1] : null, btn);

            if (btn != null && _compare != null)
                btn.onClick.AddListener(() => _compare.Select(pick, new ImageHighlight(bg)));
        }
    }
}
