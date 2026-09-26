using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A physical paper on the desk: a root on the desk plane carrying its
/// Clickable and DeskDraggable, and a Sheet child lying flat, lifted by the
/// paper's place in the stack (so the top paper is nearest the camera and wins
/// the raycast), with the lit paper quad, the collider, the document's title
/// and, on a photo document, the traveller's photo. With a row template (piece
/// 10) the paper shows its document's whole face, like its scanned copy: every
/// field row in page order (DocumentRows.Ordered), a label over its value,
/// laid out by PaperFace, the values written through DocumentRowView in the
/// traveller's translation on the document's RevealClock (shared with the
/// scanned copy). Without one (a scene built before piece 10) it shows the
/// title and the photo only. A click raises Clicked with the button and the
/// row under the pointer (PaperFace.RowAt; DeskController routes it through
/// PaperClicks). While examined (held in the hand; PaperExaminer owns the
/// sheet's pose) the paper is evenly lit (its unlit examine material, the
/// photo in the examine tint) and the row under the pointer tints; a picked
/// row lights up (RowHighlight). Slides are linear moves and flips advance in
/// Update, only while either runs.
/// </summary>
public sealed class DeskDocument : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
{
    /// <summary>The lying sheet: lifted by the stack, holding the paper, its collider, texts and photo.</summary>
    [SerializeField] private Transform sheet;

    /// <summary>The document's title ("Travel Passport").</summary>
    [SerializeField] private TextMeshPro title;

    /// <summary>The photo's frame, shown only on a photo document.</summary>
    [SerializeField] private GameObject photoSlot;

    /// <summary>The traveller's photo inside the frame (crop sprites).</summary>
    [SerializeField] private LookSpriteStack photo;

    /// <summary>The inactive row cloned per field (children Label and Value, TextMeshPro, and Highlight, a quad); optional.</summary>
    [SerializeField] private GameObject rowTemplate;

    /// <summary>The paper quad (its material swaps to the examine material while held).</summary>
    [SerializeField] private Renderer paperQuad;

    /// <summary>The paper's unlit material while held in the hand (the paper's texture, evenly lit); optional.</summary>
    [SerializeField] private Material examineMaterial;

    /// <summary>The paper's click (hover outline, hand cursor and whether it takes input).</summary>
    [SerializeField] private Clickable click;

    /// <summary>The paper's drag.</summary>
    [SerializeField] private DeskDraggable drag;

    /// <summary>One field row on the paper.</summary>
    private sealed class RowView
    {
        public DocumentRow Row;
        public TMP_Text Label;
        public TMP_Text Value;
        public TMP_FontAsset OwnFont;
        public Material OwnMaterial;
        public Renderer Highlight;
        public bool Picked;
        public Color PickColour;
    }

    /// <summary>A row of this paper as a compare highlight: tints the row's quad while picked (over the hover tint); null-safe once the paper is gone.</summary>
    private sealed class PaperRowHighlight : ICompareHighlight
    {
        private readonly DeskDocument _paper;
        private readonly int _row;

        public PaperRowHighlight(DeskDocument paper, int row)
        {
            _paper = paper;
            _row = row;
        }

        public void Show(bool picked, Color colour)
        {
            if (_paper != null)
                _paper.SetPicked(_row, picked, colour);
        }
    }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private readonly List<RowView> _rows = new List<RowView>();
    private readonly List<TextFlip> _flips = new List<TextFlip>();
    private CaseTranslation _translation = CaseTranslation.None;
    private RevealClock _clock = new RevealClock();
    private DeskConfigSO _config;
    private FaceLayout _face;
    private Material _ownPaperMaterial;
    private MaterialPropertyBlock _block;
    private int _hoveredRow = -1;

    private Vector3 _slideFrom;
    private Vector3 _slideTo;
    private float _slideSeconds;
    private float _slideElapsed;
    private Action _slideDone;

    /// <summary>The paper's index in the case (DeskController maps a drag or a click to it).</summary>
    public int Index { get; private set; }

    /// <summary>True while the paper slides (it takes no input then).</summary>
    public bool IsSliding { get; private set; }

    /// <summary>True while the paper is held in the hand (PaperExaminer owns the sheet's pose).</summary>
    public bool IsExamined { get; private set; }

    /// <summary>The lying sheet (PaperExaminer poses it while the paper is held).</summary>
    public Transform Sheet => sheet;

    /// <summary>How many field rows the paper shows.</summary>
    public int RowCount => _rows.Count;

    /// <summary>Raised on a click while the paper takes input: the paper, true for a right click, and the row under the pointer while held (-1: none, or not held).</summary>
    public event Action<DeskDocument, bool, int> Clicked;

    /// <summary>Only while sliding or flipping: moves along the slide (its done callback on landing) and advances the values' flips on the reveal's clock.</summary>
    private void Update()
    {
        if (_flips.Count > 0)
        {
            float elapsed = _clock.Elapsed(Time.unscaledTime);
            for (int i = _flips.Count - 1; i >= 0; i--)
                if (!_flips[i].Tick(elapsed))
                    _flips.RemoveAt(i);
        }

        if (!IsSliding)
            return;

        _slideElapsed += Time.deltaTime;
        float t = _slideSeconds > 0f ? Mathf.Clamp01(_slideElapsed / _slideSeconds) : 1f;
        transform.position = Vector3.Lerp(_slideFrom, _slideTo, t);
        if (t < 1f)
            return;

        IsSliding = false;
        Action done = _slideDone;
        _slideDone = null;
        done?.Invoke();
    }

    /// <summary>
    /// Shows a document: its index and canonical title and, with a row
    /// template, every field row laid out by PaperFace (at most its capacity)
    /// with the title and the photo moved to the face's places, the values in
    /// the traveller's translation on the document's reveal clock.
    /// </summary>
    public void Bind(int index, CaseDocument doc, CaseTranslation translation, RevealClock clock, DeskConfigSO config)
    {
        Index = index;
        _translation = translation ?? CaseTranslation.None;
        _clock = clock ?? new RevealClock();
        _config = config;
        if (title != null)
            title.text = doc != null ? doc.name : string.Empty;

        _rows.Clear();
        _flips.Clear();
        _face = null;
        if (rowTemplate == null || config == null || doc == null)
            return;

        float height = config.paperSize.y;
        IReadOnlyList<DocumentRow> ordered = DocumentRows.Ordered(doc.fields);
        FaceLayout face = PaperFace.Layout(ordered.Count, doc.showsPhoto, config.paperSize.x / height, config.face);
        _face = face;
        if (title != null)
            Place(title.rectTransform, face.Title, height);
        if (photoSlot != null && face.HasPhoto)
        {
            Vector3 at = photoSlot.transform.localPosition;
            photoSlot.transform.localPosition = new Vector3(face.Photo.CentreX * height, face.Photo.CentreY * height, at.z);
        }

        rowTemplate.SetActive(false);
        for (int i = 0; i < face.Rows.Count; i++)
        {
            GameObject row = Instantiate(rowTemplate, rowTemplate.transform.parent);
            row.name = $"Row_{i}";
            row.SetActive(true);
            var view = new RowView
            {
                Row = ordered[i],
                Label = row.transform.Find("Label").GetComponent<TMP_Text>(),
                Value = row.transform.Find("Value").GetComponent<TMP_Text>()
            };
            view.OwnFont = view.Value.font;
            view.OwnMaterial = view.Value.fontSharedMaterial;
            Place(view.Label.rectTransform, face.Rows[i].Label, height);
            Place(view.Value.rectTransform, face.Rows[i].Value, height);
            Transform highlight = row.transform.Find("Highlight");
            if (highlight != null)
            {
                FaceRect hit = face.Rows[i].Hit;
                highlight.localPosition = new Vector3(hit.CentreX * height, hit.CentreY * height, highlight.localPosition.z);
                highlight.localScale = new Vector3(hit.Width * height, hit.Height * height, 1f);
                view.Highlight = highlight.GetComponent<Renderer>();
            }
            _rows.Add(view);
            ApplyRowTint(i);
        }

        Refresh();
    }

    /// <summary>The document row shown as face row <paramref name="row"/> (callers pass a row from Clicked).</summary>
    public DocumentRow FieldRow(int row) => _rows[row].Row;

    /// <summary>Face row <paramref name="row"/> as a compare highlight.</summary>
    public ICompareHighlight RowHighlight(int row) => new PaperRowHighlight(this, row);

    /// <summary>
    /// Takes the paper into the hand or puts it back: while held it is evenly
    /// lit (the examine material; the photo in the examine tint instead of the
    /// room's tint), its sheet is the examiner's (SetLift waits), and the row
    /// under the pointer tints.
    /// </summary>
    public void SetExamined(bool examined)
    {
        if (IsExamined == examined)
            return;

        IsExamined = examined;
        if (paperQuad != null && examineMaterial != null)
        {
            if (examined)
                _ownPaperMaterial = paperQuad.sharedMaterial;
            paperQuad.sharedMaterial = examined ? examineMaterial : _ownPaperMaterial;
        }
        if (photo != null && _config != null)
            photo.SetTint(examined ? _config.examineTint : _config.travellerTint);
        if (!examined)
            SetHovered(-1);
    }

    /// <summary>A click while the paper takes input: Clicked with the button and, while held, the row under the pointer.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (click == null || !click.Interactable || eventData.button == PointerEventData.InputButton.Middle)
            return;

        int row = IsExamined ? RowUnder(eventData) : -1;
        Clicked?.Invoke(this, eventData.button == PointerEventData.InputButton.Right, row);
    }

    /// <summary>While held, the row under the pointer tints.</summary>
    public void OnPointerMove(PointerEventData eventData) => SetHovered(IsExamined && click != null && click.Interactable ? RowUnder(eventData) : -1);

    /// <summary>The pointer left the paper: no row tints.</summary>
    public void OnPointerExit(PointerEventData eventData) => SetHovered(-1);

    /// <summary>The face row under the pointer's hit on this paper (-1: none).</summary>
    private int RowUnder(PointerEventData eventData)
    {
        if (_face == null || sheet == null || _config == null)
            return -1;

        RaycastResult hit = eventData.pointerCurrentRaycast;
        if (hit.gameObject == null || !hit.gameObject.transform.IsChildOf(transform))
            hit = eventData.pointerPressRaycast;
        if (hit.gameObject == null || !hit.gameObject.transform.IsChildOf(transform))
            return -1;

        Vector3 local = sheet.InverseTransformPoint(hit.worldPosition);
        float height = _config.paperSize.y;
        return PaperFace.RowAt(_face, local.x / height, local.y / height);
    }

    /// <summary>Marks a face row picked (in <paramref name="colour"/>) or not.</summary>
    private void SetPicked(int row, bool picked, Color colour)
    {
        if (row < 0 || row >= _rows.Count)
            return;
        _rows[row].Picked = picked;
        _rows[row].PickColour = colour;
        ApplyRowTint(row);
    }

    /// <summary>Tints the hovered row (the old one back).</summary>
    private void SetHovered(int row)
    {
        if (row == _hoveredRow)
            return;
        int old = _hoveredRow;
        _hoveredRow = row;
        ApplyRowTint(old);
        ApplyRowTint(row);
    }

    /// <summary>A row's quad colour: the pick's colour, else the hover tint while hovered, else clear.</summary>
    private void ApplyRowTint(int row)
    {
        if (row < 0 || row >= _rows.Count || _rows[row].Highlight == null)
            return;

        RowView view = _rows[row];
        Color colour = view.Picked ? view.PickColour
            : row == _hoveredRow && _config != null ? _config.rowHoverTint
            : Color.clear;
        _block ??= new MaterialPropertyBlock();
        view.Highlight.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, colour);
        view.Highlight.SetPropertyBlock(_block);
    }

    /// <summary>Rewrites the values from the document's reveal clock (a sighting started it, or a row click finished it); flips that still run advance in Update.</summary>
    public void Refresh()
    {
        _flips.Clear();
        float now = Time.unscaledTime;
        foreach (RowView view in _rows)
        {
            // Each write starts from the value's own font, so a flip remembers it (not the script's).
            view.Value.font = view.OwnFont;
            view.Value.fontSharedMaterial = view.OwnMaterial;
            DocumentRowView.Write(view.Label, view.Value, view.Row, _translation, _clock, now, _flips);
        }
    }

    /// <summary>Shows the traveller's photo in the frame (tinted into the room's light); a null look hides the frame (a document without a photo).</summary>
    public void ShowPhoto(TravellerLook look, CharacterArt art, Color tint)
    {
        if (photoSlot != null)
            photoSlot.SetActive(look != null);
        if (photo != null)
        {
            photo.Show(look, art);
            photo.SetTint(tint);
        }
    }

    /// <summary>Lifts the sheet off the desk plane (its place in the stack, or a dragged paper's lift), in metres; ignored while the paper is held in the hand (the examiner owns the sheet).</summary>
    public void SetLift(float height)
    {
        if (sheet != null && !IsExamined)
            sheet.localPosition = new Vector3(0f, height, 0f);
    }

    /// <summary>
    /// Lets the paper be clicked (<paramref name="clickable"/>) and dragged
    /// (<paramref name="draggable"/>), or not: a held paper beside the open
    /// frame takes clicks but no drag (BoothRules.HeldDragOutLive). <paramref name="raycastable"/>
    /// false also takes it out of the raycast: a paper the office has put away
    /// (the frame open, the wheel open, a newsletter up) must let clicks through
    /// to what lies under it. A paper inert only for itself (sliding,
    /// scanning) stays raycastable and still covers what lies under it.
    /// </summary>
    public void SetLive(bool clickable, bool draggable, bool raycastable)
    {
        if (drag != null)
        {
            drag.enabled = draggable;
            drag.SetRaycastable(raycastable);
        }
        if (click != null)
            click.Interactable = clickable;
    }

    /// <summary>Slides the paper to a world point in <paramref name="seconds"/> (a linear move), then calls <paramref name="done"/>.</summary>
    public void SlideTo(Vector3 target, float seconds, Action done)
    {
        _slideFrom = transform.position;
        _slideTo = target;
        _slideSeconds = seconds;
        _slideElapsed = 0f;
        _slideDone = done;
        IsSliding = true;
    }

    /// <summary>Centres a text on a face rectangle (the paper's height is 1) and sizes it to it, in the sheet's metres.</summary>
    private static void Place(RectTransform text, FaceRect rect, float height)
    {
        text.localPosition = new Vector3(rect.CentreX * height, rect.CentreY * height, text.localPosition.z);
        text.sizeDelta = new Vector2(rect.Width * height, rect.Height * height);
    }
}
