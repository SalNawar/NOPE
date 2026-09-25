using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
/// title and the photo only. Slides are linear moves and flips advance in
/// Update, only while either runs.
/// </summary>
public sealed class DeskDocument : MonoBehaviour
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

    /// <summary>The paper's click.</summary>
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
    }

    private readonly List<RowView> _rows = new List<RowView>();
    private readonly List<TextFlip> _flips = new List<TextFlip>();
    private CaseTranslation _translation = CaseTranslation.None;
    private RevealClock _clock = new RevealClock();

    private Vector3 _slideFrom;
    private Vector3 _slideTo;
    private float _slideSeconds;
    private float _slideElapsed;
    private Action _slideDone;

    /// <summary>The paper's index in the case (DeskController maps a drag or a click to it).</summary>
    public int Index { get; private set; }

    /// <summary>True while the paper slides (it takes no input then).</summary>
    public bool IsSliding { get; private set; }

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
        if (title != null)
            title.text = doc != null ? doc.name : string.Empty;

        _rows.Clear();
        _flips.Clear();
        if (rowTemplate == null || config == null || doc == null)
            return;

        float height = config.paperSize.y;
        IReadOnlyList<DocumentRow> ordered = DocumentRows.Ordered(doc.fields);
        FaceLayout face = PaperFace.Layout(ordered.Count, doc.showsPhoto, config.paperSize.x / height, config.face);
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
            }
            _rows.Add(view);
        }

        Refresh();
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

    /// <summary>Lifts the sheet off the desk plane (its place in the stack, or a held paper's lift), in metres.</summary>
    public void SetLift(float height)
    {
        if (sheet != null)
            sheet.localPosition = new Vector3(0f, height, 0f);
    }

    /// <summary>
    /// Lets the paper be dragged and clicked, or not. <paramref name="raycastable"/>
    /// false also takes it out of the raycast: a paper the office has put away
    /// (the frame open, the wheel open, a newsletter up) must let clicks through
    /// to what lies under it. A paper inert only for itself (sliding,
    /// scanning) stays raycastable and still covers what lies under it.
    /// </summary>
    public void SetLive(bool live, bool raycastable)
    {
        if (drag != null)
        {
            drag.enabled = live;
            drag.SetRaycastable(raycastable);
        }
        if (click != null)
            click.Interactable = live;
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
