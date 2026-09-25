using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders one visitor document as a flippable, multi-page window. Each field
/// is a clickable row (label + value, DocumentRows.OnPage written through
/// DocumentRowView) that registers with the CompareController
/// (EvidencePicks.ForField); the value is shown through DisplayText and
/// compared as its canonical text. Values in the claimed place's tongue
/// (every place fact; never the name or the date of birth) show untranslated
/// from translation's first day, or, with the region's Papers translator,
/// flip into English letter by letter from the document's reveal (its
/// RevealClock, shared with its desk paper, started by the first sighting of
/// either: InvestigationUIController, then Refresh), rows staggered; a click
/// on a row finishes the whole document (both surfaces) first; reopening
/// never replays. Such a
/// value takes the row's width after its label and may wrap and shrink there
/// (a script's glyphs can be far wider than the English). An untranslated
/// value shows in the compare bar as the placeholder, while its evidence
/// stays the canonical value. Rows are cloned from <see cref="fieldRowTemplate"/> (a
/// disabled row with two TMP texts — label then value — an Image background,
/// and a Button). A document whose template shows a photo carries the
/// traveller's photo on its first page (the rows there leave room for it).
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

    /// <summary>The document's index in the case (its fields' pick keys).</summary>
    private int _index;
    private CompareController _compare;
    private CaseTranslation _translation = CaseTranslation.None;

    /// <summary>The document's written reveal (shared with its desk paper; never null).</summary>
    private RevealClock _clock = new RevealClock();

    /// <summary>The current page's value texts still flipping.</summary>
    private readonly List<TextFlip> _flips = new();
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

    /// <summary>Binds document <paramref name="index"/> of the case (with the traveller's look for a photo document, their translation and the document's reveal clock) and renders its first page.</summary>
    public void SetDocument(DocumentInstance doc, int index, CompareController compare, TravellerLook look, CharacterArt art, CaseTranslation translation, RevealClock clock)
    {
        _doc = doc;
        _index = index;
        _compare = compare;
        _translation = translation ?? CaseTranslation.None;
        _clock = clock ?? new RevealClock();
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

    /// <summary>Redraws the current page from the reveal clock (a sighting started it).</summary>
    public void Refresh() => Rebuild();

    /// <summary>Only while values flip: advances them on the reveal's clock (they keep flipping when the page or the window changes).</summary>
    private void Update()
    {
        if (_flips.Count == 0)
            return;

        float elapsed = _clock.Elapsed(Time.unscaledTime);
        for (int i = _flips.Count - 1; i >= 0; i--)
            if (!_flips[i].Tick(elapsed))
                _flips.RemoveAt(i);
    }

    /// <summary>The skip: once revealed, every value of the document (on every page) shows its English at once.</summary>
    private void FinishFlips()
    {
        _clock.Finish();
        foreach (TextFlip flip in _flips)
            flip.Complete();
        _flips.Clear();
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
        _flips.Clear();

        if (_doc == null || fieldRowsRoot == null || fieldRowTemplate == null)
            return;

        // A row's flip is delayed by the rows in the tongue above it on this page (rows that never flip take no time).
        float now = Time.unscaledTime;
        string docName = _doc.template != null ? _doc.template.displayName : UiText.Get("document.untitled");
        foreach (DocumentRow docRow in DocumentRows.OnPage(_doc.fields, _page))
        {
            GameObject row = Instantiate(fieldRowTemplate, fieldRowsRoot);
            row.SetActive(true);
            _rows.Add(row);

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            DocumentRowView.Write(texts.Length > 0 ? texts[0] : null, texts.Length > 1 ? texts[1] : null, docRow, _translation, _clock, now, _flips);

            Image bg = row.GetComponent<Image>();
            Button btn = row.GetComponent<Button>();
            ComparePick pick = EvidencePicks.ForField(_index, docRow, docName, _translation);

            if (btn != null && _compare != null)
                btn.onClick.AddListener(() =>
                {
                    FinishFlips();
                    _compare.Select(pick, new ImageHighlight(bg));
                });
        }
    }
}
