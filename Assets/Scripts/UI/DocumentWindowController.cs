using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders one visitor document as a flippable, multi-page window. Each field
/// is a clickable row (label + value) that registers with the CompareController;
/// the value is shown through DisplayText and compared as its canonical text.
/// Values in the claimed place's tongue (every place fact; never the name or
/// the date of birth) show untranslated from translation's first day, or,
/// with the region's Papers translator, flip into English letter by letter
/// from the scan (Reveal), rows staggered; a click on a row finishes the
/// whole document first; reopening never replays. Such a value takes the
/// row's width after its label and may wrap and shrink there (a script's
/// glyphs can be far wider than the English). An untranslated value shows in
/// the compare bar as the placeholder, while its evidence stays the
/// canonical value. Rows are cloned from <see cref="fieldRowTemplate"/> (a
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
    private CompareController _compare;
    private CaseTranslation _translation = CaseTranslation.None;

    /// <summary>When the scan revealed this document (Time.unscaledTime; NaN before; -infinity once finished by a click).</summary>
    private float _revealedAt = float.NaN;

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

    /// <summary>Binds a document (with the traveller's look for a photo document, and their translation) and renders its first page, not yet revealed.</summary>
    public void SetDocument(DocumentInstance doc, CompareController compare, TravellerLook look, CharacterArt art, CaseTranslation translation)
    {
        _doc = doc;
        _compare = compare;
        _translation = translation ?? CaseTranslation.None;
        _revealedAt = float.NaN;
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

    /// <summary>
    /// The written reveal point (the scan): the first time, a document with a
    /// value in the tongue starts flipping into English when the Papers
    /// translator is owned; later calls, and any other document, do nothing.
    /// </summary>
    public void Reveal()
    {
        if (!float.IsNaN(_revealedAt) || _doc == null || !_translation.Foreign || !_translation.PapersTranslated ||
            !_doc.fields.Exists(f => f != null && Translation.InTongue(f.category)))
            return;

        _revealedAt = Time.unscaledTime;
        Rebuild();
    }

    /// <summary>Only while values flip: advances them on the scan's clock (they keep flipping when the page or the window changes).</summary>
    private void Update()
    {
        if (_flips.Count == 0)
            return;

        float elapsed = Time.unscaledTime - _revealedAt;
        for (int i = _flips.Count - 1; i >= 0; i--)
            if (!_flips[i].Tick(elapsed))
                _flips.RemoveAt(i);
    }

    /// <summary>The skip: once revealed, every value of the document (on every page) shows its English at once.</summary>
    private void FinishFlips()
    {
        if (float.IsNaN(_revealedAt))
            return;

        _revealedAt = float.NegativeInfinity;
        foreach (TextFlip flip in _flips)
            flip.Complete();
        _flips.Clear();
    }

    /// <summary>The value takes whatever width the row's layout has left after its label (its glyphs' own width never counts) and may wrap onto a second line there.</summary>
    private static void FillRest(TMP_Text value)
    {
        if (!value.TryGetComponent(out LayoutElement element))
            element = value.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = 0f;
        element.flexibleWidth = RestWeight;
        value.textWrappingMode = TextWrappingModes.Normal;
    }

    /// <summary>The value's share of the row's spare width against the label's (the row's layout expands every child by at least 1).</summary>
    private const float RestWeight = 1000f;

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
        float elapsed = Time.unscaledTime - _revealedAt;
        int tongueRow = 0;
        foreach (DocumentField f in _doc.fields)
        {
            if (f == null || f.page != _page)
                continue;

            GameObject row = Instantiate(fieldRowTemplate, fieldRowsRoot);
            row.SetActive(true);
            _rows.Add(row);

            bool inTongue = Translation.InTongue(f.category);
            Reveal reveal = _translation.Field(f.category, elapsed, tongueRow);
            if (inTongue)
                tongueRow++;

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0 && texts[0] != null)
                texts[0].text = f.label;
            if (texts.Length > 1 && texts[1] != null)
            {
                // A value in a foreign tongue takes the row's width after the label, so wide glyphs shrink into it instead of squeezing the label.
                if (_translation.Foreign && inTongue)
                    FillRest(texts[1]);
                if (reveal.Kind == RevealKind.Flipping)
                {
                    var flip = new TextFlip();
                    flip.Show(texts[1], f.value, reveal, _translation);
                    if (flip.Running)
                        _flips.Add(flip);
                }
                else
                {
                    TextFlip.Write(texts[1], f.value, reveal, _translation);
                }
            }

            Image bg = row.GetComponent<Image>();
            Button btn = row.GetComponent<Button>();

            string docName = _doc.template != null ? _doc.template.displayName : UiText.Get("document.untitled");
            string label = UiText.Format("document.compareLabel", docName, f.label);
            string shown = _translation.Shown(inTongue, _translation.PapersTranslated, f.value);
            DocumentField field = f;

            if (btn != null && _compare != null)
                btn.onClick.AddListener(() =>
                {
                    FinishFlips();
                    _compare.Select(label, shown, bg, CompareEvidence.FromDocumentField(field));
                });
        }
    }
}
