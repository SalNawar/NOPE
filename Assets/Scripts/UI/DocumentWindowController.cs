using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders one visitor document as a flippable, multi-page window. Each field
/// is a clickable row (label + value) that registers with the CompareController;
/// the value is shown through DisplayText and compared as its canonical text.
/// Rows are cloned from <see cref="fieldRowTemplate"/> (a disabled row with two
/// TMP texts — label then value — an Image background, and a Button). A
/// document whose template shows a photo carries the traveller's photo on its
/// first page (the rows there leave room for it).
/// </summary>
public sealed class DocumentWindowController : MonoBehaviour
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

    /// <summary>Binds a document (with the traveller's look for a photo document) and renders its first page.</summary>
    public void SetDocument(DocumentInstance doc, CompareController compare, TravellerLook look, CharacterArt art)
    {
        _doc = doc;
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
        foreach (GameObject r in _rows)
            if (r != null)
                Destroy(r);

        _rows.Clear();

        if (_doc == null || fieldRowsRoot == null || fieldRowTemplate == null)
            return;

        foreach (DocumentField f in _doc.fields)
        {
            if (f == null || f.page != _page)
                continue;

            GameObject row = Instantiate(fieldRowTemplate, fieldRowsRoot);
            row.SetActive(true);
            _rows.Add(row);

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0 && texts[0] != null)
                texts[0].text = f.label;
            if (texts.Length > 1 && texts[1] != null)
                texts[1].text = DisplayText.For(f.value, TextMedium.Written);

            Image bg = row.GetComponent<Image>();
            Button btn = row.GetComponent<Button>();

            string docName = _doc.template != null ? _doc.template.displayName : UiText.Get("document.untitled");
            string label = UiText.Format("document.compareLabel", docName, f.label);
            string value = f.value;
            DocumentField field = f;

            if (btn != null && _compare != null)
                btn.onClick.AddListener(() => _compare.Select(label, value, bg, CompareEvidence.FromDocumentField(field)));
        }
    }
}
