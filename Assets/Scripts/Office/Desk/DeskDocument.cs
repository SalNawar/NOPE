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
/// it at the paper's width, the same form as its scanned copy (FormView); each
/// text is a TextMeshPro cloned from one template, sized and inked by its
/// role (FormStyleSO); the strokes are FormPaint's, as on the PC: the boxes'
/// fills and the section bands are one mesh under the hover and pick quads,
/// and every outline, rule, barcode bar and checkbox one mesh over them, both
/// built once when the paper binds; the seal
/// is a faint quad behind the header, and the photo sits in its cell. Always
/// English: a paper never flips. A click raises Clicked with the button and
/// the slot under the pointer (FormLayout.SlotAt; DeskController routes it
/// through PaperClicks). While examined (held in the hand; PaperExaminer owns
/// the sheet's pose) the paper is evenly lit (its unlit examine material, the
/// photo in the examine tint) and the box under the pointer tints; a picked
/// box lights up (SlotHighlight). Slides are linear moves in Update, only
/// while one runs.
/// </summary>
public sealed class DeskDocument : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
{
    /// <summary>How far above the sheet each layer lies (metres toward the camera): the seal, the fills, the hover and pick quads, the lines, the photo, the texts.</summary>
    private const float SealLift = 0.0001f, FillLift = 0.0002f, HighlightLift = 0.0003f, LineLift = 0.0004f, PhotoLift = 0.0005f, TextLift = 0.0006f;

    /// <summary>The lying sheet: lifted by the stack, holding the paper, its collider and the printed form.</summary>
    [SerializeField] private Transform sheet;

    /// <summary>The photo's frame (a quad PhotoAspect by 1, scaled to the photo cell's height), shown only on a photo document.</summary>
    [SerializeField] private GameObject photoSlot;

    /// <summary>The traveller's photo inside the frame (crop sprites).</summary>
    [SerializeField] private LookSpriteStack photo;

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

    /// <summary>The paper quad (its material swaps to the examine material while held).</summary>
    [SerializeField] private Renderer paperQuad;

    /// <summary>The paper's unlit material while held in the hand (the paper's texture, evenly lit); optional.</summary>
    [SerializeField] private Material examineMaterial;

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

    private readonly List<SlotView> _slots = new List<SlotView>();
    private DeskConfigSO _config;
    private PlacedForm _form;
    private Material _ownPaperMaterial;
    private MaterialPropertyBlock _block;
    private int _hoveredSlot = -1;

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

    /// <summary>How many pickable boxes the paper prints.</summary>
    public int SlotCount => _slots.Count;

    /// <summary>Raised on a click while the paper takes input: the paper, true for a right click, and the box under the pointer while held (-1: none, or not held).</summary>
    public event Action<DeskDocument, bool, int> Clicked;

    /// <summary>Only while sliding: moves along the slide (its done callback on landing).</summary>
    private void Update()
    {
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
        if (config == null || doc == null || form == null || style == null || textTemplate == null)
            return;

        float width = config.paperSize.x;
        _form = FormLayout.Layout(form.Spec, form.Data, width, style.metrics, new TmpFormText(textTemplate));

        foreach (FormItem item in _form.Items)
        {
            switch (item.Kind)
            {
                case FormItemKind.Text:
                    Print(item);
                    break;
                case FormItemKind.Seal:
                    PlaceSeal(item.Rect);
                    break;
                case FormItemKind.Photo:
                    PlacePhoto(item.Rect);
                    break;
            }
        }
        var fillMesh = new MeshBuilder();
        var lineMesh = new MeshBuilder();
        foreach (FormQuad q in FormPaint.Quads(_form, style.Palette(), style.metrics))
            (q.Layer == FormPaintLayer.Fill ? fillMesh : lineMesh).Rect(Local(q.Rect), new Color(q.Colour.R, q.Colour.G, q.Colour.B, q.Colour.A));
        fillMesh.Apply(fills, FillLift);
        lineMesh.Apply(lines, LineLift);

        foreach (FormSlot s in _form.Slots)
        {
            if (s.Field < 0 || doc.fields == null || s.Field >= doc.fields.Count || doc.fields[s.Field] == null)
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
    /// Takes the paper into the hand or puts it back: while held it is evenly
    /// lit (the examine material; the photo in the examine tint instead of the
    /// room's tint), its sheet is the examiner's (SetLift waits), and the box
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

    /// <summary>A click while the paper takes input: Clicked with the button and, while held, the box under the pointer.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (click == null || !click.Interactable || eventData.button == PointerEventData.InputButton.Middle)
            return;

        int slot = IsExamined ? SlotUnder(eventData) : -1;
        Clicked?.Invoke(this, eventData.button == PointerEventData.InputButton.Right, slot);
    }

    /// <summary>While held, the box under the pointer tints.</summary>
    public void OnPointerMove(PointerEventData eventData) => SetHovered(IsExamined && click != null && click.Interactable ? SlotUnder(eventData) : -1);

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
        int formSlot = FormLayout.SlotAt(_form, local.x + _form.Width / 2f, _form.PageHeight / 2f - local.y);
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

    // ---------------- Printing ----------------

    /// <summary>Prints one text item: a clone of the template in its role's style and ink, over its rectangle.</summary>
    private void Print(FormItem item)
    {
        TextMeshPro text = Instantiate(textTemplate, textTemplate.transform.parent);
        text.name = item.Role.ToString();
        TmpFormText.Style(text, item.Role, item.Size);
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

    /// <summary>The seal: its quad over the header's seal rectangle, faint.</summary>
    private void PlaceSeal(FaceRect rect)
    {
        if (seal == null)
            return;
        Rect r = Local(rect);
        seal.transform.localPosition = new Vector3(r.center.x, r.center.y, -SealLift);
        seal.transform.localScale = new Vector3(r.width, r.height, 1f);
        _block ??= new MaterialPropertyBlock();
        seal.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, style.seal);
        seal.SetPropertyBlock(_block);
        seal.gameObject.SetActive(true);
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

    /// <summary>A form-space rectangle (metres from the paper's top-left, y down) in the sheet's local space (centre origin, y up).</summary>
    private Rect Local(FaceRect f) =>
        new Rect(f.XMin - _form.Width / 2f, _form.PageHeight / 2f - f.YMax, f.Width, f.Height);

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
