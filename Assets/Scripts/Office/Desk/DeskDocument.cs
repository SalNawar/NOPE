using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A physical paper on the desk: a root on the desk plane carrying its
/// Clickable and DeskDraggable, and a Sheet child lying flat, lifted by the
/// paper's place in the stack (so the top paper is nearest the camera and wins
/// the raycast), with the lit paper quad and the collider. The paper prints
/// its document's form (redesign phase 4, PC spec FO1, §6.5): FormLayout places
/// it with its print unit 1, as the form check does, and the paper draws it
/// scaled to its width, the same form as its scanned copy (FormView); each
/// text is a TextMeshPro cloned from one template, sized and inked by its
/// role (FormStyleSO); the strokes are FormPaint's, as on the PC: the boxes'
/// fills and the section bands are one mesh under the hover and pick quads,
/// and every outline, rule, barcode bar and checkbox one mesh over them, both
/// built once when the paper binds; the seal
/// is a faint quad behind the header, or the issuing office's seal (its
/// outline in its ink, SealArt, its legend printed over it) in the header's
/// box, and the photo sits in its cell. Its form's look (the document design
/// spec, D1) sizes the paper (its height a share of the desk's paper, its
/// width by the look's aspect; its print by its width, FormLayout.PrintUnit),
/// tints it and colours its frame's bands. The
/// paper wears its kind's face and the photo frame its art when those files
/// exist (redesign phase 27, ArtSlots: the paper's placeholder and the grey
/// frame otherwise). A booklet prints its holder's nation's emblem and a
/// watermark its mark, faint over the boxes' fills; a card's paper has rounded
/// corners (the travel documents spec, TD1, TD3). A desk stamp's mark lands
/// where Stamp puts it (StampSpots: centred where a desk stamp pressed it,
/// anywhere on the page, or the next place in its largest stamp area, a
/// passport's visa page; TD4), the verdict's
/// (ShowVerdict) in its stamp area: the mark's art, else a code-drawn stamp
/// (a framed APPROVED or DENIED in green or red ink). Always
/// English: a paper never flips. Papers, Please's controls (Saleh
/// 2026-10-06): a left-click raises Clicked with the box under the pointer in
/// inspect mode (FormLayout.SlotAt; DeskController routes it through
/// PaperClicks); a right-click is no click (it backs out, ControlRules). The
/// paper's zone sizes it (SetZone: small on the counter, full size and evenly
/// lit on the desk, its unlit reading material and the photo in the reading
/// tint; DeskZones), easing between the two; in inspect mode every box is
/// tinted as comparable and the one under the pointer stronger; a picked
/// box lights up (SlotHighlight). A desk stamp prints where its die pressed
/// the passport (PagePoint, Stamp; DeskStampTray presses). Slides and resizes are moves
/// in Update, only while one runs.
/// </summary>
public sealed class DeskDocument : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
{
    /// <summary>How far above the sheet each layer lies (metres toward the camera): the seal, the fills, the hover and pick quads, the lines, the photo, the texts, the verdict's ink.</summary>
    private const float SealLift = 0.0001f, FillLift = 0.0002f, HighlightLift = 0.0003f, LineLift = 0.0004f, PhotoLift = 0.0005f, TextLift = 0.0006f, InkLift = 0.0007f;

    /// <summary>A code-drawn stamp's width over its height, its frame texture's size in pixels, its word's size as a share of its height, its ink's alpha, and its tilt in degrees (alternating with each mark).</summary>
    private const float StampAspect = 2.8f, StampWordShare = 0.5f, StampAlpha = 0.88f, StampTilt = 6f;

    /// <summary>The code-drawn stamp frame's texture size in pixels (width, height).</summary>
    private const int StampPixelsWide = 224, StampPixelsHigh = 80;

    /// <summary>The code-drawn stamps' inks: the approval's green and the denial's red.</summary>
    private static readonly Color ApprovedInk = new Color(0.12f, 0.47f, 0.23f), DeniedInk = new Color(0.7f, 0.15f, 0.12f);

    /// <summary>The code-drawn stamp's frame (a double ring, white on clear), painted once.</summary>
    private static Texture2D _stampFrame;

    /// <summary>The lying sheet: lifted by the stack, holding the paper, its collider and the printed form.</summary>
    [SerializeField] private Transform sheet;

    /// <summary>The photo's frame (a quad PhotoAspect by 1, scaled to the photo cell's height), shown only on a photo document.</summary>
    [SerializeField] private GameObject photoSlot;

    /// <summary>The traveller's photo inside the frame (crop sprites).</summary>
    [SerializeField] private LookSpriteStack photo;

    /// <summary>The photo frame's art quad over the photo (inactive until the photo frame's art, ArtSlots.PhotoFrame, is found; the grey frame behind is the fallback).</summary>
    [SerializeField] private Renderer photoFrame;

    /// <summary>The verdict's ink mark quad (a unit square, inactive until ShowVerdict finds the mark's art).</summary>
    [SerializeField] private Renderer inkMark;

    /// <summary>The text every printed word clones (its renderer off: it also measures the words).</summary>
    [SerializeField] private TextMeshPro textTemplate;

    /// <summary>The mesh of the boxes' fills and the section bands (vertex colours; drawn under the hover and pick quads).</summary>
    [SerializeField] private MeshFilter fills;

    /// <summary>The mesh of every outline, rule, barcode bar, checkbox and the stamp area's dash (vertex colours; drawn over the quads, under the texts).</summary>
    [SerializeField] private MeshFilter lines;

    /// <summary>The seal's quad (a unit square), printed faintly behind the header.</summary>
    [SerializeField] private Renderer seal;

    /// <summary>The inactive quad cloned per pickable box: its hover tint and its pick highlight.</summary>
    [SerializeField] private Renderer highlightTemplate;

    /// <summary>The forms' sizes and colours.</summary>
    [SerializeField] private FormStyleSO style;

    /// <summary>The paper quad (its material swaps to the reading material while on the desk).</summary>
    [SerializeField] private Renderer paperQuad;

    /// <summary>The paper's unlit material while it lies on the desk, full size (the paper's texture, evenly lit); optional.</summary>
    [SerializeField] private Material examineMaterial;

    /// <summary>Every box's tint in inspect mode (Papers, Please: every comparable thing highlights); the box under the pointer takes the style's hover tint over it.</summary>
    [SerializeField] private Color inspectTint = new Color(1f, 0.78f, 0.1f, 0.34f);

    /// <summary>How long a change of size between the counter and the desk takes (seconds; a cut under Reduced Motion).</summary>
    [SerializeField, Min(0f)] private float resizeSeconds = 0.15f;

    /// <summary>The paper's click (hover outline, hand cursor and whether it takes input).</summary>
    [SerializeField] private Clickable click;

    /// <summary>The paper's drag.</summary>
    [SerializeField] private DeskDraggable drag;

    /// <summary>One pickable box: the field it shows, its quad and its pick.</summary>
    private sealed class SlotView
    {
        public DocumentRow Row;
        public Renderer Highlight;
        public bool Picked;
        public Color PickColour;

        /// <summary>A clear mistake the player found here (a logged difference): its box stays tinted in this colour for the case (alpha 0: not marked).</summary>
        public Color MarkColour;
    }

    /// <summary>A box of this paper as a compare highlight: tints its quad while picked (over the hover tint); null-safe once the paper is gone.</summary>
    private sealed class PaperSlotHighlight : ICompareHighlight
    {
        private readonly DeskDocument _paper;
        private readonly int _slot;

        public PaperSlotHighlight(DeskDocument paper, int slot)
        {
            _paper = paper;
            _slot = slot;
        }

        public void Show(bool picked, Color colour)
        {
            if (_paper != null)
                _paper.SetPicked(_slot, picked, colour);
        }
    }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    private readonly List<SlotView> _slots = new List<SlotView>();
    private DeskConfigSO _config;
    private PlacedForm _form;

    /// <summary>The paper's metres per unit of its placed form: the form is laid out with its print unit 1 (as FormLayout.Check checks it: TextMeshPro measures a world-space text in metres taller than its glyphs once its size falls to a few millimetres, so a small paper laid out in metres would run past its page) and drawn this much smaller.</summary>
    private float _scale = 1f;

    /// <summary>The paper quad's own mesh (a unit quad), and the rounded one built for a card (destroyed with the paper).</summary>
    private Mesh _quadMesh, _shapedMesh;

    /// <summary>The watermark's material: the seal's, drawn after the fills (made for this paper alone).</summary>
    private Material _ownMarkMaterial;

    /// <summary>How many stamps the paper carries (each next one steps across its stamp area).</summary>
    private int _stamps;
    private Material _ownPaperMaterial;
    private MaterialPropertyBlock _block;
    private int _hoveredSlot = -1;

    /// <summary>The sheet's scale now, the scale it eases toward (the zone's) and where the ease started.</summary>
    private float _sizeNow = 1f, _sizeTarget = 1f, _sizeFrom = 1f, _sizeElapsed;

    private Vector3 _slideFrom;
    private Vector3 _slideTo;
    private float _slideSeconds;
    private float _slideElapsed;
    private Action _slideDone;

    /// <summary>The paper's drag (the builder wires it): the desk reaches it without a GetComponent each time.</summary>
    public DeskDraggable Drag => drag;

    /// <summary>The paper's index in the case (DeskController maps a drag or a click to it).</summary>
    public int Index { get; private set; }

    /// <summary>The paper's size in metres, its look's (the document design spec, D1): the desk's paper height times the look's scale, by the look's aspect; the desk's paper before it binds.</summary>
    public Vector2 Size { get; private set; }

    /// <summary>True while the paper slides (it takes no input then).</summary>
    public bool IsSliding { get; private set; }

    /// <summary>The zone the paper is shown for (DeskController sets it as it lies and while it is dragged).</summary>
    public DeskZone Zone { get; private set; }

    /// <summary>True in inspect mode: every box is tinted and a click picks the box under the pointer.</summary>
    public bool Inspecting { get; private set; }

    /// <summary>The lying sheet (its scale is the zone's size).</summary>
    public Transform Sheet => sheet;

    /// <summary>How many pickable boxes the paper prints.</summary>
    public int SlotCount => _slots.Count;

    /// <summary>Raised on a left-click while the paper takes input: the paper and, in inspect mode, the box under the pointer (-1: none, or not inspecting).</summary>
    public event Action<DeskDocument, int> Clicked;

    /// <summary>Only while one runs: eases the sheet toward its zone's size, and moves along the slide (its done callback on landing).</summary>
    private void Update()
    {
        if (!Mathf.Approximately(_sizeNow, _sizeTarget))
        {
            _sizeElapsed += Time.deltaTime;
            float seconds = MotionPreference.Reduced ? 0f : resizeSeconds;
            float eased = seconds > 0f ? Mathf.Clamp01(_sizeElapsed / seconds) : 1f;
            _sizeNow = eased >= 1f ? _sizeTarget : Mathf.Lerp(_sizeFrom, _sizeTarget, DeskZones.Ease(eased));
            if (sheet != null)
                sheet.localScale = Vector3.one * _sizeNow;
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

    /// <summary>The paper's printed meshes go with it (they were built for this paper alone).</summary>
    private void OnDestroy()
    {
        foreach (MeshFilter filter in new[] { fills, lines })
            if (filter != null && filter.sharedMesh != null)
                Destroy(filter.sharedMesh);
        if (_shapedMesh != null)
            Destroy(_shapedMesh);
        if (_ownMarkMaterial != null)
            Destroy(_ownMarkMaterial);
    }

    /// <summary>
    /// Prints a document: <paramref name="form"/>'s spec laid out at the
    /// paper's width with <paramref name="doc"/>'s content (its template's
    /// words, the serial, the fields), its texts, meshes, seal and one quad per
    /// pickable box; the photo moves to its cell. A paper without a form (a
    /// scene built before phase 4) prints nothing but its paper.
    /// </summary>
    public void Bind(int index, CaseDocument doc, DocumentForm form, DeskConfigSO config)
    {
        Index = index;
        _config = config;
        _slots.Clear();
        _form = null;
        if (config != null)
            Size = config.paperSize;
        if (config == null || doc == null || form == null || style == null || textTemplate == null)
            return;

        FormLook look = form.Spec.look ?? new FormLook();
        FormPalette palette = look.Palette(style.Palette());
        float height = config.paperSize.y * look.Scale;
        Resize(new Vector2(height * look.AspectOr(style.metrics.aspect), height));
        ShowPaperArt(form.Data.FormNumber, form.Data.Issuer, string.IsNullOrEmpty(look.paper) ? (Color?)null : new Color(palette.Paper.R, palette.Paper.G, palette.Paper.B, 1f));

        _stamps = 0;
        _scale = FormLayout.PrintUnit(form.Spec, Size.x, style.metrics);
        _form = FormLayout.Layout(form.Spec, form.Data, Size.x / _scale, style.metrics, new TmpFormText(textTemplate));
        ShapePaper(PaperSilhouette.Corner(look.frame, _form.Width, _form.PageHeight, _form.Unit) * _scale);
        Color cover = EmblemArt.Ink(form.Data.Cover, new Color(palette.Accent.R, palette.Accent.G, palette.Accent.B, 1f));

        foreach (FormItem item in _form.Items)
        {
            switch (item.Kind)
            {
                case FormItemKind.Text:
                    Print(item);
                    break;
                case FormItemKind.Seal:
                    PlaceSeal(item);
                    break;
                case FormItemKind.Photo:
                    PlacePhoto(item.Rect);
                    break;
                case FormItemKind.Emblem:
                    PlaceMark("Emblem", item.Rect, EmblemArt.Texture(item.Text), cover, TextLift);
                    break;
                case FormItemKind.Watermark:
                    bool isSeal = Seals.TryParse(item.Text, out Seal mark);
                    Color ink = isSeal ? SealArt.Ink(mark.Ink) : cover;
                    ink.a = EmblemArt.WatermarkAlpha;
                    Renderer watermark = PlaceMark("Watermark", item.Rect, isSeal ? SealArt.Texture(mark.Shape) : EmblemArt.Texture(item.Text), ink, FillLift);
                    OverTheFills(watermark);
                    break;
            }
        }
        var fillMesh = new MeshBuilder();
        var lineMesh = new MeshBuilder();
        foreach (FormQuad q in FormPaint.Quads(_form, palette, style.metrics))
            (q.Layer == FormPaintLayer.Fill ? fillMesh : lineMesh).Rect(Local(q.Rect), new Color(q.Colour.R, q.Colour.G, q.Colour.B, q.Colour.A));
        fillMesh.Apply(fills, FillLift);
        lineMesh.Apply(lines, LineLift);

        foreach (FormSlot s in _form.Slots)
        {
            if (s.Hidden || s.Field < 0 || doc.fields == null || s.Field >= doc.fields.Count || doc.fields[s.Field] == null)
                continue;
            var view = new SlotView { Row = new DocumentRow(s.Field, doc.fields[s.Field]) };
            if (highlightTemplate != null)
            {
                Renderer quad = Instantiate(highlightTemplate, highlightTemplate.transform.parent);
                quad.name = $"Slot_{_slots.Count}";
                quad.gameObject.SetActive(true);
                Rect r = Local(s.Hit);
                quad.transform.localPosition = new Vector3(r.center.x, r.center.y, -HighlightLift);
                quad.transform.localScale = new Vector3(r.width, r.height, 1f);
                view.Highlight = quad;
            }
            _slots.Add(view);
            ApplySlotTint(_slots.Count - 1);
        }
    }

    /// <summary>The document row box <paramref name="slot"/> shows (callers pass a slot from Clicked).</summary>
    public DocumentRow FieldAt(int slot) => _slots[slot].Row;

    /// <summary>Box <paramref name="slot"/> as a compare highlight.</summary>
    public ICompareHighlight SlotHighlight(int slot) => new PaperSlotHighlight(this, slot);

    /// <summary>
    /// Shows the paper for <paramref name="zone"/> (Papers, Please's counter and
    /// desk): on the counter small (DeskConfigSO.counterScale of its size), lit
    /// by the room; on the desk full size (DeskZones.ReadingScale to
    /// DeskConfigSO.readingHeight) and evenly lit (the reading material; the
    /// photo in the reading tint instead of the room's). The size eases there,
    /// or is set at once (<paramref name="instant"/>).
    /// </summary>
    public void SetZone(DeskZone zone, bool instant)
    {
        Zone = zone;
        float target = _config == null ? 1f
            : zone == DeskZone.Desk ? DeskZones.ReadingScale(Size.x, Size.y, _config.readingHeight, _config.paperSize.x / _config.paperSize.y)
            : _config.counterScale;
        _sizeFrom = _sizeNow;
        _sizeTarget = target;
        _sizeElapsed = 0f;
        if (instant)
        {
            _sizeNow = target;
            if (sheet != null)
                sheet.localScale = Vector3.one * target;
        }

        bool reading = zone == DeskZone.Desk;
        if (paperQuad != null && examineMaterial != null)
        {
            if (reading && paperQuad.sharedMaterial != examineMaterial)
                _ownPaperMaterial = paperQuad.sharedMaterial;
            if (reading || _ownPaperMaterial != null)
                paperQuad.sharedMaterial = reading ? examineMaterial : _ownPaperMaterial;
        }
        if (photo != null && _config != null)
            photo.SetTint(reading ? _config.readingTint : _config.travellerTint);
    }

    /// <summary>Inspect mode on or off (DeskController): every box tints as comparable and the box under the pointer stronger; off, no box tints but the picks and the marks.</summary>
    public void SetInspecting(bool inspecting)
    {
        if (Inspecting == inspecting)
            return;
        Inspecting = inspecting;
        if (!inspecting)
            SetHovered(-1);
        for (int i = 0; i < _slots.Count; i++)
            ApplySlotTint(i);
    }

    /// <summary>A left-click while the paper takes input: Clicked with, in inspect mode, the box under the pointer. Any other button is no click (a right-click backs out).</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (click == null || !click.Interactable || eventData.button != PointerEventData.InputButton.Left)
            return;

        Clicked?.Invoke(this, Inspecting ? SlotUnder(eventData) : -1);
    }

    /// <summary>In inspect mode, the box under the pointer tints.</summary>
    public void OnPointerMove(PointerEventData eventData) => SetHovered(Inspecting && click != null && click.Interactable ? SlotUnder(eventData) : -1);

    /// <summary>The box showing the document's row <paramref name="rowIndex"/> (DocumentRow.Index), or -1.</summary>
    public int SlotOfRow(int rowIndex)
    {
        for (int i = 0; i < _slots.Count; i++)
            if (_slots[i].Row.Index == rowIndex)
                return i;
        return -1;
    }

    /// <summary>Box <paramref name="slot"/>'s world bounds (where a match line meets it); false for no such box.</summary>
    public bool TryBoundsOf(int slot, out Bounds bounds)
    {
        bounds = default;
        if (slot < 0 || slot >= _slots.Count || _slots[slot].Highlight == null)
            return false;
        bounds = _slots[slot].Highlight.bounds;
        return true;
    }

    /// <summary>Marks box <paramref name="slot"/> as a clear mistake found (its tint stays for the case, under a pick or hover tint), or clears it with a clear colour.</summary>
    public void SetMarked(int slot, Color colour)
    {
        if (slot < 0 || slot >= _slots.Count)
            return;
        _slots[slot].MarkColour = colour;
        ApplySlotTint(slot);
    }

    /// <summary>A world point on the paper as a point on its page: from the page's top-left, y down, in the paper's metres (what Stamp takes).</summary>
    public Vector2 PagePoint(Vector3 world)
    {
        if (_form == null || sheet == null)
            return Vector2.zero;
        Vector3 local = sheet.InverseTransformPoint(world);
        return new Vector2(local.x + _form.Width * _scale / 2f, _form.PageHeight * _scale / 2f - local.y);
    }

    /// <summary>The pointer left the paper: no box tints.</summary>
    public void OnPointerExit(PointerEventData eventData) => SetHovered(-1);

    /// <summary>The pickable box under the pointer's hit on this paper (-1: none).</summary>
    private int SlotUnder(PointerEventData eventData)
    {
        if (_form == null || sheet == null)
            return -1;

        RaycastResult hit = eventData.pointerCurrentRaycast;
        if (hit.gameObject == null || !hit.gameObject.transform.IsChildOf(transform))
            hit = eventData.pointerPressRaycast;
        if (hit.gameObject == null || !hit.gameObject.transform.IsChildOf(transform))
            return -1;

        Vector3 local = sheet.InverseTransformPoint(hit.worldPosition);
        int formSlot = FormLayout.SlotAt(_form, local.x / _scale + _form.Width / 2f, _form.PageHeight / 2f - local.y / _scale);
        if (formSlot < 0)
            return -1;
        int field = _form.Slots[formSlot].Field;
        for (int i = 0; i < _slots.Count; i++)
            if (_slots[i].Row.Index == field)
                return i;
        return -1;
    }

    /// <summary>Marks a box picked (in <paramref name="colour"/>) or not.</summary>
    private void SetPicked(int slot, bool picked, Color colour)
    {
        if (slot < 0 || slot >= _slots.Count)
            return;
        _slots[slot].Picked = picked;
        _slots[slot].PickColour = colour;
        ApplySlotTint(slot);
    }

    /// <summary>Tints the hovered box (the old one back).</summary>
    private void SetHovered(int slot)
    {
        if (slot == _hoveredSlot)
            return;
        int old = _hoveredSlot;
        _hoveredSlot = slot;
        ApplySlotTint(old);
        ApplySlotTint(slot);
    }

    /// <summary>A box's quad colour: the pick's colour, else the hover tint while hovered, else clear.</summary>
    private void ApplySlotTint(int slot)
    {
        if (slot < 0 || slot >= _slots.Count || _slots[slot].Highlight == null)
            return;

        SlotView view = _slots[slot];
        Color colour = view.Picked ? view.PickColour
            : slot == _hoveredSlot && style != null ? style.hoverTint
            : view.MarkColour.a > 0f ? view.MarkColour
            : Inspecting ? inspectTint
            : Color.clear;
        _block ??= new MaterialPropertyBlock();
        view.Highlight.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, colour);
        view.Highlight.SetPropertyBlock(_block);
    }

    /// <summary>Shows the traveller's photo in the frame (tinted into the room's light); a null look hides the frame (a document without a photo).</summary>
    public void ShowPhoto(TravellerLook look, CharacterArt art, Color tint)
    {
        if (photoSlot != null)
            photoSlot.SetActive(look != null && _form != null && HasPhotoItem());
        if (photo != null)
        {
            photo.Show(look, art);
            photo.SetTint(tint);
        }
    }

    /// <summary>The verdict's ink mark as the papers leave (redesign phase 27): Stamp at the next place of the form's stamp area.</summary>
    public void ShowVerdict(bool accepted) => Stamp(accepted);

    /// <summary>
    /// Presses a desk stamp on the paper (the travel documents spec, TD4;
    /// Papers, Please's stamps): APPROVED for <paramref name="approved"/>,
    /// else DENIED, centred at <paramref name="formPoint"/> (from the page's
    /// top-left, y down, in the paper's metres: where the stamp's die pressed,
    /// over the boxes too) and kept whole on the page (StampSpots.AtPoint) or,
    /// without one (the verdict's ink as the papers leave), at the next place of its largest
    /// stamp area (StampSpots.Next: a passport's visa page, a form's footer
    /// box). The mark is the art's (ArtSlots.VerdictMark) at its own aspect,
    /// else a code-drawn stamp: a double frame and the style's word
    /// (FormStyleSO.approvedStamp, deniedStamp) in green or red ink, tilted a
    /// few degrees. Returns the mark's place, from the page's top-left in the
    /// paper's metres (an empty one on a paper that prints no form).
    /// </summary>
    public FaceRect Stamp(bool approved, Vector2? formPoint = null)
    {
        if (inkMark == null || _form == null)
            return new FaceRect(0f, 0f, 0f, 0f);
        Texture2D art = SlotArt.Texture(new[] { ArtSlots.VerdictMark(approved) });
        float aspect = art != null && art.height > 0 ? (float)art.width / art.height : StampAspect;
        FaceRect place = formPoint.HasValue ? StampSpots.AtPoint(_form, formPoint.Value.x / _scale, formPoint.Value.y / _scale, aspect) : StampSpots.Next(_form, _stamps, aspect);
        float tilt = art != null ? 0f : (_stamps % 2 == 0 ? -StampTilt : StampTilt * 0.6f);
        _stamps++;

        Renderer mark = _stamps == 1 ? inkMark : Instantiate(inkMark, inkMark.transform.parent);
        mark.name = $"Stamp_{_stamps}";
        Rect r = Local(place);
        mark.transform.localPosition = new Vector3(r.center.x, r.center.y, -InkLift);
        mark.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
        mark.transform.localScale = new Vector3(r.width, r.height, 1f);
        Color ink = approved ? ApprovedInk : DeniedInk;
        ink.a = StampAlpha;
        _block ??= new MaterialPropertyBlock();
        mark.GetPropertyBlock(_block);
        _block.SetTexture(BaseMapId, art != null ? art : StampFrame());
        _block.SetColor(BaseColorId, art != null ? new Color(1f, 1f, 1f, ink.a / StampAlpha) : ink);
        mark.SetPropertyBlock(_block);
        mark.gameObject.SetActive(true);

        if (art == null && style != null)
        {
            TextMeshPro word = Instantiate(textTemplate, textTemplate.transform.parent);
            word.name = mark.name + "_Word";
            TmpFormText.Style(word, FormTextRole.Title, r.height * StampWordShare);
            word.text = approved ? style.approvedStamp : style.deniedStamp;
            word.color = ink;
            word.alignment = TextAlignmentOptions.Center;
            word.textWrappingMode = TextWrappingModes.NoWrap;
            word.rectTransform.sizeDelta = new Vector2(r.width, r.height);
            word.rectTransform.localPosition = new Vector3(r.center.x, r.center.y, -InkLift - 0.0001f);
            word.rectTransform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            word.GetComponent<MeshRenderer>().enabled = true;
        }
        return new FaceRect(place.XMin * _scale, place.YMin * _scale, place.XMax * _scale, place.YMax * _scale);
    }

    /// <summary>The code-drawn stamp's frame: a thick outer rectangle and a hairline inside it, white on clear (tinted by the ink), painted once.</summary>
    private static Texture2D StampFrame()
    {
        if (_stampFrame != null)
            return _stampFrame;
        _stampFrame = new Texture2D(StampPixelsWide, StampPixelsHigh, TextureFormat.RGBA32, true) { name = "StampFrame", wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[StampPixelsWide * StampPixelsHigh];
        for (int y = 0; y < StampPixelsHigh; y++)
            for (int x = 0; x < StampPixelsWide; x++)
            {
                int edge = Mathf.Min(Mathf.Min(x, StampPixelsWide - 1 - x), Mathf.Min(y, StampPixelsHigh - 1 - y));
                bool inked = edge < 7 || (edge >= 11 && edge < 13);
                pixels[y * StampPixelsWide + x] = new Color32(255, 255, 255, (byte)(inked ? 255 : 0));
            }
        _stampFrame.SetPixels32(pixels);
        _stampFrame.Apply(true, true);
        return _stampFrame;
    }

    /// <summary>A mark of the form named <paramref name="name"/> (an emblem, a watermark): a clone of the seal's quad over <paramref name="rect"/> showing <paramref name="texture"/> in <paramref name="ink"/>, <paramref name="lift"/> over the sheet; nothing without a texture or a seal quad.</summary>
    private Renderer PlaceMark(string name, FaceRect rect, Texture2D texture, Color ink, float lift)
    {
        if (seal == null || texture == null)
            return null;
        Renderer mark = Instantiate(seal, seal.transform.parent);
        mark.name = name;
        Rect r = Local(rect);
        mark.transform.localPosition = new Vector3(r.center.x, r.center.y, -lift);
        mark.transform.localScale = new Vector3(r.width, r.height, 1f);
        _block ??= new MaterialPropertyBlock();
        mark.GetPropertyBlock(_block);
        _block.SetTexture(BaseMapId, texture);
        _block.SetColor(BaseColorId, ink);
        mark.SetPropertyBlock(_block);
        mark.gameObject.SetActive(true);
        return mark;
    }

    /// <summary>A watermark (the travel documents spec, TD1) drawn just after the fills' layer (its own copy of the seal's material, one render queue later), so the boxes' fills never hide it; the lines and texts print over it.</summary>
    private void OverTheFills(Renderer mark)
    {
        if (mark == null || fills == null || !fills.TryGetComponent(out Renderer fillRenderer) || fillRenderer.sharedMaterial == null)
            return;
        _ownMarkMaterial ??= new Material(mark.sharedMaterial) { name = "Paper_Watermark", renderQueue = fillRenderer.sharedMaterial.renderQueue + 1 };
        mark.sharedMaterial = _ownMarkMaterial;
    }

    /// <summary>
    /// The paper's outline (the travel documents spec, TD1): a card's corners
    /// rounded by <paramref name="corner"/> (metres) in a mesh of its own (a fan
    /// round the centre, PaperSilhouette), every other paper the quad's own
    /// unit square. The mesh is in the quad's unit space (the quad is scaled
    /// to the paper's size), so its corners are round once scaled.
    /// </summary>
    private void ShapePaper(float corner)
    {
        if (paperQuad == null || !paperQuad.TryGetComponent(out MeshFilter filter))
            return;
        if (_quadMesh == null)
            _quadMesh = filter.sharedMesh;
        if (corner <= 0f || Size.x <= 0f || Size.y <= 0f)
        {
            filter.sharedMesh = _quadMesh;
            return;
        }
        List<(float x, float y)> outline = PaperSilhouette.Outline(Size.x, Size.y, corner);
        var vertices = new List<Vector3> { Vector3.zero };
        var uvs = new List<Vector2> { new Vector2(0.5f, 0.5f) };
        foreach ((float x, float y) in outline)
        {
            vertices.Add(new Vector3(x / Size.x, y / Size.y, 0f));
            uvs.Add(new Vector2(x / Size.x + 0.5f, y / Size.y + 0.5f));
        }
        // The fan faces the way the quad's own triangles do (its winding and its normal), so the camera sees its front.
        Vector3[] quad = _quadMesh.vertices;
        int[] tris = _quadMesh.triangles;
        bool counterClockwise = tris.Length < 3 || Vector3.Cross(quad[tris[1]] - quad[tris[0]], quad[tris[2]] - quad[tris[0]]).z > 0f;
        Vector3 normal = _quadMesh.normals.Length > 0 ? _quadMesh.normals[0] : Vector3.back;
        var triangles = new List<int>();
        for (int i = 1; i <= outline.Count; i++)
            triangles.AddRange(counterClockwise ? new[] { 0, i, i % outline.Count + 1 } : new[] { 0, i % outline.Count + 1, i });
        if (_shapedMesh == null)
            _shapedMesh = new Mesh { name = "PaperShape" };
        _shapedMesh.Clear();
        _shapedMesh.SetVertices(vertices);
        _shapedMesh.SetUVs(0, uvs);
        _shapedMesh.SetNormals(vertices.ConvertAll(_ => normal));
        _shapedMesh.SetTriangles(triangles, 0);
        _shapedMesh.RecalculateBounds();
        filter.sharedMesh = _shapedMesh;
    }

    /// <summary>Lifts the sheet off the desk plane (its place in the stack, or a dragged paper's lift), in metres.</summary>
    public void SetLift(float height)
    {
        if (sheet != null)
            sheet.localPosition = new Vector3(0f, height, 0f);
    }

    /// <summary>
    /// Lets the paper be clicked (<paramref name="clickable"/>) and dragged
    /// (<paramref name="draggable"/>), or not. <paramref name="raycastable"/>
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

    // ---------------- Printing ----------------

    /// <summary>Prints one text item: a clone of the template in its role's style and ink, over its rectangle, shrunk just enough that its widest word fits its box (TmpFormText.WordFit, measured at the layout's scale), as on the PC.</summary>
    private void Print(FormItem item)
    {
        TextMeshPro text = Instantiate(textTemplate, textTemplate.transform.parent);
        text.name = item.Role.ToString();
        float fit = TmpFormText.WordFit(textTemplate, item.Text, item.Role, item.Size, item.Rect.Width);
        TmpFormText.Style(text, item.Role, item.Size * _scale * fit);
        text.text = item.Text;
        text.color = style.Ink(item.Role);
        text.alignment = item.Align == FormTextAlign.Right ? TextAlignmentOptions.TopRight
            : item.Align == FormTextAlign.Centre ? TextAlignmentOptions.Top
            : TextAlignmentOptions.TopLeft;
        Rect r = Local(item.Rect);
        text.rectTransform.sizeDelta = new Vector2(r.width, r.height);
        text.rectTransform.localPosition = new Vector3(r.center.x, r.center.y, -TextLift);
        text.GetComponent<MeshRenderer>().enabled = true;
    }

    /// <summary>The paper's quad and its collider at <paramref name="size"/> (metres): the sheet the form is laid out on.</summary>
    private void Resize(Vector2 size)
    {
        Size = size;
        if (paperQuad != null)
            paperQuad.transform.localScale = new Vector3(size.x, size.y, 1f);
        if (sheet != null && sheet.TryGetComponent(out BoxCollider box))
            box.size = new Vector3(size.x, size.y, box.size.z);
    }

    /// <summary>
    /// The paper's art when delivered (redesign phase 27, ArtSlots): its kind's
    /// face by <paramref name="formNumber"/> (a passport's for its holder's
    /// nation, <paramref name="issuer"/>, first), else the agency's plain face, on
    /// the paper quad (on the counter or the desk: the block outlives the
    /// reading material's swap); without a face, the look's paper <paramref name="tint"/> (the
    /// document design spec, D1) on a plain sheet; the photo frame's art over
    /// the photo; the agency seal's on the seal. A missing file keeps the
    /// placeholder paper, the grey frame and the code-drawn ring.
    /// </summary>
    private void ShowPaperArt(string formNumber, string issuer, Color? tint)
    {
        _block ??= new MaterialPropertyBlock();
        Texture2D face = paperQuad != null ? SlotArt.Texture(ArtSlots.PaperFaces(formNumber, issuer)) : null;
        if (face != null)
        {
            paperQuad.GetPropertyBlock(_block);
            _block.SetTexture(BaseMapId, face);
            paperQuad.SetPropertyBlock(_block);
        }
        else if (paperQuad != null && tint != null)
        {
            paperQuad.GetPropertyBlock(_block);
            _block.SetTexture(BaseMapId, Texture2D.whiteTexture);
            _block.SetColor(BaseColorId, tint.Value);
            paperQuad.SetPropertyBlock(_block);
        }

        Texture2D frame = photoFrame != null ? SlotArt.Texture(new[] { ArtSlots.PhotoFrame }) : null;
        if (frame != null)
        {
            photoFrame.GetPropertyBlock(_block);
            _block.SetTexture(BaseMapId, frame);
            photoFrame.SetPropertyBlock(_block);
            photoFrame.gameObject.SetActive(true);
        }

        Texture2D mark = seal != null ? SlotArt.Texture(new[] { ArtSlots.AgencySeal }) : null;
        if (mark != null)
        {
            seal.GetPropertyBlock(_block);
            _block.SetTexture(BaseMapId, mark);
            seal.SetPropertyBlock(_block);
        }
    }

    /// <summary>
    /// The seal: its quad over the header's seal rectangle, faint (the
    /// agency's), or, for a seal the form names (the issuing office's, the
    /// document design spec D4; its value Seals.Describe's words), its
    /// outline (SealArt) in its ink with its legend printed in the middle.
    /// </summary>
    private void PlaceSeal(FormItem item)
    {
        if (seal == null)
            return;
        Rect r = Local(item.Rect);
        seal.transform.localPosition = new Vector3(r.center.x, r.center.y, -SealLift);
        seal.transform.localScale = new Vector3(r.width, r.height, 1f);
        _block ??= new MaterialPropertyBlock();
        seal.GetPropertyBlock(_block);
        bool office = Seals.TryParse(item.Text, out Seal mark);
        if (office)
        {
            _block.SetTexture(BaseMapId, SealArt.Texture(mark.Shape));
            _block.SetColor(BaseColorId, SealArt.Ink(mark.Ink));
        }
        else
        {
            _block.SetColor(BaseColorId, style.seal);
        }
        seal.SetPropertyBlock(_block);
        seal.gameObject.SetActive(true);
        if (office)
            PrintLegend(r, mark);
    }

    /// <summary>A seal's legend: a clone of the text template in bold capitals and the seal's ink, centred in <paramref name="r"/> (the seal's local rectangle).</summary>
    private void PrintLegend(Rect r, Seal mark)
    {
        TextMeshPro text = Instantiate(textTemplate, textTemplate.transform.parent);
        text.name = "SealLegend";
        TmpFormText.Style(text, FormTextRole.Title, r.height * SealArt.LegendShare);
        text.text = mark.Legend;
        text.color = SealArt.Ink(mark.Ink);
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.sizeDelta = new Vector2(r.width, r.height);
        text.rectTransform.localPosition = new Vector3(r.center.x, r.center.y, -TextLift);
        text.GetComponent<MeshRenderer>().enabled = true;
    }

    /// <summary>The photo frame over the photo's rectangle (its quad is PhotoAspect by 1, so it scales by the height).</summary>
    private void PlacePhoto(FaceRect rect)
    {
        if (photoSlot == null)
            return;
        Rect r = Local(rect);
        photoSlot.transform.localPosition = new Vector3(r.center.x, r.center.y, -PhotoLift);
        photoSlot.transform.localScale = new Vector3(r.height, r.height, 1f);
    }

    /// <summary>True when the placed form prints a photo.</summary>
    private bool HasPhotoItem()
    {
        foreach (FormItem item in _form.Items)
            if (item.Kind == FormItemKind.Photo)
                return true;
        return false;
    }

    /// <summary>A form-space rectangle (the placed form's units from the page's top-left, y down) in the sheet's local space (metres, centre origin, y up).</summary>
    private Rect Local(FaceRect f) =>
        new Rect((f.XMin - _form.Width / 2f) * _scale, (_form.PageHeight / 2f - f.YMax) * _scale, f.Width * _scale, f.Height * _scale);

    /// <summary>Collects coloured quads (FormPaint's) in the sheet's plane into one mesh.</summary>
    private sealed class MeshBuilder
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color> _colours = new List<Color>();
        private readonly List<int> _triangles = new List<int>();
        private float _z;

        /// <summary>A filled rectangle.</summary>
        public void Rect(Rect r, Color colour)
        {
            if (r.width <= 0f || r.height <= 0f)
                return;
            int v = _vertices.Count;
            _vertices.Add(new Vector3(r.xMin, r.yMin, 0f));
            _vertices.Add(new Vector3(r.xMin, r.yMax, 0f));
            _vertices.Add(new Vector3(r.xMax, r.yMax, 0f));
            _vertices.Add(new Vector3(r.xMax, r.yMin, 0f));
            for (int i = 0; i < 4; i++)
                _colours.Add(colour);
            _triangles.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
        }

        /// <summary>Puts the quads into <paramref name="filter"/>'s mesh, <paramref name="lift"/> above the sheet.</summary>
        public void Apply(MeshFilter filter, float lift)
        {
            if (filter == null)
                return;
            _z = -lift;
            for (int i = 0; i < _vertices.Count; i++)
                _vertices[i] = new Vector3(_vertices[i].x, _vertices[i].y, _z);
            var mesh = new Mesh { name = filter.name };
            mesh.SetVertices(_vertices);
            mesh.SetColors(_colours);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
        }
    }
}
