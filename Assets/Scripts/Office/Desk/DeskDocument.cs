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
/// corners (the travel documents spec, TD1, TD3). A dater's impression
/// (the desk machine spec §1: its outline word, red date and BY line,
/// painted at runtime) lands where Stamp puts it (StampSpots: centred where
/// the dater pressed it, anywhere on the page, or the next place in its
/// largest stamp area, a passport's visa page; TD4), the verdict's ink of
/// papers leaving unstamped (ShowVerdict) in its stamp area. Always
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
/// P0-A carve (docs/pending/CARVE.md): the paper's look is in
/// DeskDocument.Render.cs. Owner: DOC-HANDLE.
/// </summary>
public sealed partial class DeskDocument : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
{
    /// <summary>How far above the sheet each layer lies (metres toward the camera, never scaled with the paper: PaperLayers, the one stacking rule): the seal, the fills, the hover and pick quads, the lines, the photo, the texts, the verdict's ink.</summary>
    private const float SealLift = PaperLayers.Seal, FillLift = PaperLayers.Fill, HighlightLift = PaperLayers.Highlight, LineLift = PaperLayers.Line, PhotoLift = PaperLayers.Photo, TextLift = PaperLayers.Text, InkLift = PaperLayers.Ink;

    /// <summary>The sheet's scale in <paramref name="zone"/>: the reading size on the desk (at the art's reading share), the counter's share on the counter.</summary>
    private float ZoneScale(DeskZone zone) => (_config == null ? 1f
        : zone == DeskZone.Desk ? _reading * DeskZones.ReadingScale(Size.x, Size.y, _config.readingHeight, _config.paperSize.x / _config.paperSize.y)
        : _config.counterScale);

    /// <summary>A paper's header band as a share of its height from its top: the part the desk's spread keeps in view (PaperSpread).</summary>
    private const float HeaderShare = 0.2f;

    private readonly List<Rect> _keys = new List<Rect>();

    /// <summary>
    /// The parts of the paper the desk's spread keeps in view when papers land
    /// (PaperSpread; Saleh's playtest 2026-10-07: headers and photos visible at
    /// once): its header band, its photo and its visa box, as shares of the paper (x from its
    /// left, y from its top, both 0 to 1).
    /// </summary>
    public IReadOnlyList<Rect> KeyShares => _keys;

    /// <summary>The paper's size in metres once it lies in <paramref name="zone"/> and is read there (its sheet's scale in the reading view).</summary>
    public Vector2 SizeIn(DeskZone zone) => Size * ZoneScale(zone);

    /// <summary>The sheet's scale lying in <paramref name="zone"/> now: the desk's reading size only while the reading view is on (DeskPapers.ShownFullSize), else the counter's, its normal size.</summary>
    private float ShownScale(DeskZone zone) => ZoneScale(!_docked && DeskPapers.ShownFullSize(zone, _focused) ? DeskZone.Desk : DeskZone.Counter);

    /// <summary>The paper goes into a machine (<paramref name="docked"/>: its bed, pad or slot) or comes back out: docked, it shows at its normal size (it must fit the slot), out again at its zone's.</summary>
    public void SetDocked(bool docked)
    {
        if (_docked == docked)
            return;
        _docked = docked;
        EaseTo(ShownScale(Zone));
    }

    /// <summary>The paper's size as it shows now (metres, width by height: its sheet's scale).</summary>
    public Vector2 ShownSize => Size * _sizeNow;

    /// <summary>The paper's size docked in a machine (metres: its normal size, width by height).</summary>
    public Vector2 DockedSize => Size * ZoneScale(DeskZone.Counter);

    /// <summary>A dragged paper over a machine keeps at least <paramref name="height"/> metres over its place (the casing's top and a clearance; 0 away from every machine): it never passes through a casing while carried.</summary>
    public void SetCarryFloor(float height)
    {
        _carryFloor = Mathf.Max(0f, height);
        if (_liftPlaced)
            _lift.Target = Mathf.Max(_stackLift, _carryFloor);
    }

    /// <summary>The machine refused the paper: it shakes its "no" on its swing (PaperPhysics.Nudge, <paramref name="swingSpeed"/> degrees per second) before the desk carries it back.</summary>
    public void Shake(float swingSpeed) => _physics.Nudge(swingSpeed);


    /// <summary>True while the reading view is on (SetFocused): a paper on the desk shows at its reading size only then.</summary>
    private bool _focused;

    /// <summary>
    /// The reading view turned on or off (DeskController): a paper lying on
    /// the desk eases to its reading size or back to its normal size; a
    /// carried paper keeps its size until it is dropped (Saleh's 1016a
    /// playtest: "documents should stay their size").
    /// </summary>
    public void SetFocused(bool focused)
    {
        if (_focused == focused)
            return;
        _focused = focused;
        if (drag == null || !drag.IsDragging)
            EaseTo(ShownScale(Zone));
    }

    /// <summary>The sheet eases from its size now to <paramref name="target"/>.</summary>
    private void EaseTo(float target)
    {
        _sizeFrom = _sizeNow;
        _sizeTarget = target;
        _sizeElapsed = 0f;
    }

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

    /// <summary>The paper's model and contact shadow (DeskPaperDepth, a child of the paper quad, in its unit space; Saleh's 1007d playtest: "too flat when you see them from the side"; 2026-10-17: "use canva and blender to make actual 3d documents").</summary>
    [SerializeField] private DeskPaperDepth depth;

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
    private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");

    private readonly List<SlotView> _slots = new List<SlotView>();
    private DeskConfigSO _config;
    private PlacedForm _form;

    /// <summary>The paper's size on the desk as a share of the desk's reading size (FormArt.reading; 1 off the art).</summary>
    private float _reading = 1f;

    /// <summary>The paper's metres per unit of its placed form: the form is laid out with its print unit 1 (as FormLayout.Check checks it: TextMeshPro measures a world-space text in metres taller than its glyphs once its size falls to a few millimetres, so a small paper laid out in metres would run past its page) and drawn this much smaller.</summary>
    private float _scale = 1f;

    /// <summary>The paper quad's own mesh (a unit quad), and the rounded one built for a card (destroyed with the paper).</summary>
    private Mesh _quadMesh, _shapedMesh;

    /// <summary>The watermark's material: the seal's, drawn after the fills (made for this paper alone).</summary>
    private Material _ownMarkMaterial;

    /// <summary>How many stamps the paper carries (each next one steps across its stamp area).</summary>
    private int _stamps;

    /// <summary>The daters' prints on the paper (it owns them: destroyed with it).</summary>
    private readonly List<Texture2D> _impressions = new List<Texture2D>();
    private Material _ownPaperMaterial;
    private MaterialPropertyBlock _block;
    private int _hoveredSlot = -1;

    /// <summary>The sheet's scale now, the scale it eases toward (the zone's) and where the ease started.</summary>
    private float _sizeNow = 1f, _sizeTarget = 1f, _sizeFrom = 1f, _sizeElapsed;

    /// <summary>The paper's feel (the game feel's desk layer): its lift off the desk (a spring toward SetLift's height), its tilt toward a drag's travel (degrees about the sheet's x and z) and a drop's squash (wider by the value).</summary>
    private Spring _lift, _tiltX, _tiltZ, _squash;

    /// <summary>True once the lift has a place (the first SetLift puts it there at once), and the drag's state last frame (a pick-up or a drop plays its cue and its squash).</summary>
    private bool _liftPlaced, _wasDragged;

    /// <summary>Where the paper was last frame (a drag's speed tilts it).</summary>
    private Vector3 _lastPosition;

    /// <summary>The sheet's own rotation (lying flat, its face up: the builder's), which the tilt turns in the paper's space.</summary>
    private Quaternion? _sheetRest;

    /// <summary>The paper carried along a way (a slide across the desk, or over a machine's casing into its dock and out: DockPath).</summary>
    private readonly DeskCarry _carry = new DeskCarry();

    /// <summary>The lift the stack gives the paper (SetLift), and the least lift a dragged paper keeps over a machine's casing under it (SetCarryFloor).</summary>
    private float _stackLift, _carryFloor;

    /// <summary>True while the paper lies docked in a machine (SetDocked): at its normal size, whatever the reading view.</summary>
    private bool _docked;

    /// <summary>The paper's drag (the builder wires it): the desk reaches it without a GetComponent each time.</summary>
    public DeskDraggable Drag => drag;

    /// <summary>The paper's index in the case (DeskController maps a drag or a click to it).</summary>
    public int Index { get; private set; }

    /// <summary>The paper's size in metres, its look's (the document design spec, D1): the desk's paper height times the look's scale, by the look's aspect; the desk's paper before it binds.</summary>
    public Vector2 Size { get; private set; }

    /// <summary>True while the paper slides or is carried into or out of a machine (it takes no input then).</summary>
    public bool IsSliding => _carry.Running;

    /// <summary>The paper's kind (its model: a sheet, a booklet, a card).</summary>
    public PaperKind Kind => _kind;

    /// <summary>Where the paper's way starts from: its root where it shows now, lifted by how far it is still held up over its place (a dropped paper is carried from where it hangs).</summary>
    public Vector3 CarriedFrom => transform.position + Vector3.up * Mathf.Max(0f, _lift.Value - _lift.Target);

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
        Feel(Time.unscaledDeltaTime);
        if (!Mathf.Approximately(_sizeNow, _sizeTarget))
        {
            _sizeElapsed += Time.deltaTime;
            float seconds = MotionPreference.Reduced ? 0f : resizeSeconds;
            float eased = seconds > 0f ? Mathf.Clamp01(_sizeElapsed / seconds) : 1f;
            _sizeNow = eased >= 1f ? _sizeTarget : Mathf.LerpUnclamped(_sizeFrom, _sizeTarget, UiMotion.Ease(eased, UiMotion.Knobs.deskMoveFeel, seconds));
            ShowSheetScale();
        }
        _carry.Step(transform, Time.deltaTime);
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
        foreach (Texture2D impression in _impressions)
            if (impression != null)
                Destroy(impression);
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
        _keys.Clear();
        _keys.Add(new Rect(0f, 0f, 1f, HeaderShare));
        if (config != null)
            Size = config.paperSize;
        if (config == null || doc == null || form == null || style == null || textTemplate == null)
            return;

        FormLook look = form.Spec.look ?? new FormLook();
        FormArt art = ArtLayout.IsArt(form.Spec) ? look.art : null;
        _reading = art != null && art.reading > 0f ? art.reading : 1f;
        FormPalette palette = look.Palette(style.Palette());
        float height = config.paperSize.y * look.Scale;
        Resize(new Vector2(height * look.AspectOr(style.metrics.aspect), height));
        FormData shown = UiText.InReadingLanguage(form.Data);
        ShowPaperArt(form.Data.FormNumber, form.Data.Issuer, string.IsNullOrEmpty(look.paper) ? (Color?)null : new Color(palette.Paper.R, palette.Paper.G, palette.Paper.B, 1f), art != null, art != null && art.Worded(shown));

        _stamps = 0;
        _scale = FormLayout.PrintUnit(form.Spec, Size.x, style.metrics);
        _form = FormLayout.Layout(form.Spec, shown, Size.x / _scale, style.metrics, new TmpFormText(textTemplate) { ScriptOf = shown.Translated ? _ => CultureThemeService.Instance != null ? CultureThemeService.Instance.CultureFont : null : (Func<string, TMP_FontAsset>)null });
        List<(float x, float y)> outline = ShapePaper(art != null ? art.corner * Size.x : PaperSilhouette.Corner(look.frame, _form.Width, _form.PageHeight, _form.Unit) * _scale);
        Color cover = EmblemArt.Ink(form.Data.Cover, new Color(palette.Accent.R, palette.Accent.G, palette.Accent.B, 1f));
        PaperKind kind = PaperEdge.KindOf(look.frame);
        Thickness = kind == PaperKind.Booklet ? config.bookletThickness : kind == PaperKind.Card ? config.cardThickness : config.paperThickness;
        Texture2D blank = art != null ? SlotArt.Texture(new[] { ArtSlots.PaperBlank(form.Data.FormNumber) }) : null;

        foreach (FormItem item in _form.Items)
        {
            switch (item.Kind)
            {
                case FormItemKind.Text:
                    Print(item, art);
                    break;
                case FormItemKind.Patch:
                    PlacePatch(item.Rect, blank);
                    break;
                case FormItemKind.Seal:
                    PlaceSeal(item);
                    break;
                case FormItemKind.Photo:
                    PlacePhoto(item.Rect);
                    break;
                case FormItemKind.StampArea:
                    _keys.Add(Share(Local(item.Rect))); // the visa box: the spread keeps it clear for the stamp
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
        BuildDepth(outline, kind);
        Jitter(Seed(index, form.Data.FormNumber));

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
    /// desk): on the counter at its normal size (DeskConfigSO.counterScale of
    /// its size), lit by the room; on the desk evenly lit (the reading
    /// material; the photo in the reading tint instead of the room's), at full
    /// size (DeskZones.ReadingScale to DeskConfigSO.readingHeight) while the
    /// reading view is on, else at its normal size (ShownScale). The size eases
    /// there, or is set at once (<paramref name="instant"/>); a paper on its
    /// art reads at its art's share of that (FormArt.reading).
    /// </summary>
    public void SetZone(DeskZone zone, bool instant)
    {
        Zone = zone;
        float target = ShownScale(zone);
        EaseTo(target);
        if (instant)
        {
            _sizeNow = target;
            ShowSheetScale();
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

    /// <summary>Box <paramref name="slot"/>'s rectangle in the sheet's own plane (its local x and y; the desk machines' UV marks lie over it); false for no such box.</summary>
    public bool TryLocalRectOf(int slot, out Rect rect)
    {
        rect = default;
        if (slot < 0 || slot >= _slots.Count || _slots[slot].Highlight == null)
            return false;
        Transform quad = _slots[slot].Highlight.transform;
        Vector3 a = sheet.InverseTransformPoint(quad.TransformPoint(new Vector3(-0.5f, -0.5f, 0f)));
        Vector3 b = sheet.InverseTransformPoint(quad.TransformPoint(new Vector3(0.5f, 0.5f, 0f)));
        rect = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        return true;
    }

    /// <summary>The font the paper prints in (its text template's): the UV layer's ghosts are written in it.</summary>
    public TMP_FontAsset TextFont => textTemplate != null ? textTemplate.font : null;

    /// <summary>Marks box <paramref name="slot"/> as a clear mistake found (its tint stays for the case, under a pick or hover tint), or clears it with a clear colour.</summary>
    public void SetMarked(int slot, Color colour)
    {
        if (slot < 0 || slot >= _slots.Count)
            return;
        _slots[slot].MarkColour = colour;
        ApplySlotTint(slot);
    }

    /// <summary>True when box <paramref name="slot"/> carries a mark (SetMarked: a logged difference, the analysis's flag).</summary>
    public bool Marked(int slot) => slot >= 0 && slot < _slots.Count && _slots[slot].MarkColour.a > 0f;

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

    /// <summary>The paper's thickness on the desk (metres; DeskConfigSO's for its kind: a sheet, a booklet, a card, as deep as its model): its shadow's depth, and its room in the stack (DeskController, PaperLayers.Lifts).</summary>
    public float Thickness { get; private set; } = 0.0007f;

    /// <summary>
    /// Turns the paper a little on the desk, as if dropped by hand: its own
    /// share (by <paramref name="seed"/>) of DeskConfigSO.paperJitter degrees
    /// about the desk's up, fixed for the paper. The sheet turns, its click
    /// box with it, so what is clicked is what shows.
    /// </summary>
    private void Jitter(int seed)
    {
        if (sheet == null || _config == null)
            return;
        _builtRest ??= _sheetRest ?? sheet.localRotation;
        uint mixed = unchecked((uint)seed * 2654435761u);
        float share = (mixed >> 12) % 2001u / 1000f - 1f;
        _restYaw = share * _config.paperJitter;
        Rest();
    }

    /// <summary>The paper's turn on the desk (degrees about its up): its own jitter, then each drop's spin (PaperPhysics: it lies where its momentum left it), kept within twice the jitter either way of square.</summary>
    private float _restYaw;

    /// <summary>Lays the sheet at its turn (_restYaw) under its tilt.</summary>
    private void Rest()
    {
        if (sheet == null || _builtRest == null)
            return;
        _sheetRest = Quaternion.Euler(0f, _restYaw, 0f) * _builtRest.Value;
        sheet.localRotation = Quaternion.Euler(0f, _physics.Yaw, 0f) * Quaternion.Euler(_tiltX.Value, 0f, _tiltZ.Value) * _sheetRest.Value;
    }

    /// <summary>The paper's physics feel (Saleh 2026-10-17: "KEEP IT TOP DOWN BUT GIVE IT PHYSICS"; 1018a: "all documents are stiff"): its swing and bend while held, its slide, spin, hop and wobble when let go.</summary>
    private readonly PaperPhysics _physics = new PaperPhysics();

    /// <summary>The paper's kind (its model: a sheet, a booklet, a card): how much it bends (MotionKnobs.sheetFlex, bookletFlex, cardFlex).</summary>
    private PaperKind _kind;

    /// <summary>The bend drawn now (PaperBend: flat at rest).</summary>
    private PaperBend _bend = PaperBend.Flat;

    /// <summary>The printed parts' rest poses on the sheet (each part, its place, its turn and its centre on the face, metres) while the paper bends; empty while it lies flat.</summary>
    private readonly List<(Transform part, Vector3 position, Quaternion rotation, Vector2 centre)> _printRest = new List<(Transform, Vector3, Quaternion, Vector2)>();

    /// <summary>How far the printed parts stand off the bent face while it bends (metres toward the viewer): a part's flat piece over a curve stays above it.</summary>
    private const float BentPrintLift = 0.0003f;

    /// <summary>The paper's own number for its look (its index and its form's number: the same paper lies the same way every time).</summary>
    private static int Seed(int index, string formNumber)
    {
        unchecked
        {
            int h = 17 + index * 7919;
            foreach (char c in formNumber ?? string.Empty)
                h = h * 31 + c;
            return h & int.MaxValue;
        }
    }

    /// <summary>The sheet's rotation as the builder made it (Jitter turns it from there).</summary>
    private Quaternion? _builtRest;

    /// <summary>Lifts the sheet off the desk plane (its place in the stack, or a dragged paper's lift), in metres: the first place at once, then on the paper's spring (a picked-up paper rises, a dropped one settles; a cut without motion).</summary>
    public void SetLift(float height)
    {
        if (sheet == null)
            return;
        _stackLift = height;
        height = Mathf.Max(height, _carryFloor);
        if (!_liftPlaced || UiMotion.Amount.Still)
        {
            _liftPlaced = true;
            _lift.Snap(height);
            _settling = false;
            ShowFeel();
            return;
        }
        _settling = height < _lift.Target; // set down: its spring never sinks it below its place (the paper under it, the desk)
        _lift.Target = height;
    }

    /// <summary>
    /// The paper's feel (Saleh 2026-10-07: every touch reads as physical): a
    /// pick-up and a drop play their cues; while dragged the sheet tilts
    /// toward its travel by its speed (MotionKnobs.paperTiltPerSpeed, at most
    /// paperTiltMax), its leading edge dipping; dropped, it squashes softly
    /// wider (paperDropSquash) and flutters back flat; its lift follows SetLift
    /// on the same spring (paperFeel). Scaled by the Motion intensity; still
    /// under Reduced Motion. Nothing runs while it rests.
    /// </summary>
    private void Feel(float dt)
    {
        if (sheet == null || drag == null)
            return;
        bool dragged = drag.IsDragging;
        Vector3 position = transform.position;
        MotionKnobs knobs = UiMotion.Knobs;
        MotionAmount amount = UiMotion.Amount;
        if (dragged != _wasDragged)
        {
            _wasDragged = dragged;
            Sounds.Play(dragged ? SoundCues.PaperPickup : SoundCues.PaperDrop);
            if (!dragged && !amount.Still)
            {
                SpringTuning paper = knobs.Get(knobs.paperFeel);
                _squash.Kick(paper.KickFor(knobs.paperDropSquash * amount.Share));
                _tiltX.Kick(paper.KickFor(knobs.paperTiltMax * 0.25f * amount.Share)); // the flutter as it lands
            }
        }
        // The physics feel on top of where the desk puts it (the desk's drop sizing and the scanner's snap decide the place).
        (float spin, float hop) = _physics.Step(dt, Vector3.Dot(position, transform.right), Vector3.Dot(position, transform.forward), dragged, knobs, amount);
        if (spin != 0f && _config != null)
        {
            float most = Mathf.Max(_config.paperJitter * 2f, 0.5f);
            _restYaw = Mathf.Clamp(_restYaw + spin, -most, most);
            Rest();
        }
        if (hop > 0f)
            _lift.Kick(knobs.Get(knobs.paperFeel).KickFor(hop));
        bool bent = ShowBend(BendNow(knobs));
        if (dragged && dt > 0f && !amount.Still)
        {
            Vector3 travel = transform.InverseTransformDirection(position - _lastPosition) / dt;
            float most = knobs.paperTiltMax * amount.Share;
            _tiltX.Target = Mathf.Clamp(travel.z * knobs.paperTiltPerSpeed * amount.Share, -most, most);
            _tiltZ.Target = Mathf.Clamp(-travel.x * knobs.paperTiltPerSpeed * amount.Share, -most, most);
        }
        else
            _tiltX.Target = _tiltZ.Target = 0f;
        _lastPosition = position;
        if (_lift.AtRest && _tiltX.AtRest && _tiltZ.AtRest && _squash.AtRest && !_physics.Moving)
        {
            if (bent)
                ShowFeel(); // flat again: down onto its place
            return;
        }
        SpringTuning tuning = knobs.Get(knobs.paperFeel);
        _lift.Step(dt, tuning, knobs.settleValue * 0.01f, knobs.settleSpeed * 0.01f); // metres
        _tiltX.Step(dt, tuning, knobs.settleValue * 10f, knobs.settleSpeed * 10f); // degrees
        _tiltZ.Step(dt, tuning, knobs.settleValue * 10f, knobs.settleSpeed * 10f);
        _squash.Step(dt, tuning, knobs.settleValue, knobs.settleSpeed);
        ShowFeel();
    }

    /// <summary>True while the paper is being set down (its lift's target lowered): its spring's bounce stays above its place.</summary>
    private bool _settling;

    /// <summary>
    /// Draws the lift and the tilt on the sheet, and its scale. The height is
    /// its lift (while set down, never below its place: the spring's
    /// undershoot sank it into the desk or the paper under it) raised by how
    /// far its tilt dips its lowest corner (PaperLayers.TiltDrop: a drag's lean
    /// or a drop's flutter), so no part of it ever goes through what lies under
    /// it (Saleh's 1007d playtest: "sometimes they clip or render with the table").
    /// </summary>
    private void ShowFeel()
    {
        float lift = _settling ? Mathf.Max(_lift.Value, _lift.Target) : _lift.Value;
        float half = _sizeNow * (1f + _squash.Value) / 2f;
        lift += PaperLayers.TiltDrop(Size.x * half, Size.y * half, _tiltX.Value, _tiltZ.Value);
        // a bend's drooping ends never pass through what lies under the paper (a held one has its drag's lift of room)
        float room = drag != null && drag.IsDragging && _config != null ? _config.dragLift : 0f;
        lift += Mathf.Max(0f, _bend.Lowest(Size.x, Size.y) - room);
        sheet.localPosition = new Vector3(_physics.OffsetX, lift, _physics.OffsetZ);
        _sheetRest ??= sheet.localRotation;
        sheet.localRotation = _tiltX.Value == 0f && _tiltZ.Value == 0f && _physics.Yaw == 0f
            ? _sheetRest.Value
            : Quaternion.Euler(0f, _physics.Yaw, 0f) * Quaternion.Euler(_tiltX.Value, 0f, _tiltZ.Value) * _sheetRest.Value;
        ShowSheetScale();
    }

    /// <summary>The sheet's scale: its zone's size, wider in its own plane (its x and y: it lies on its back) by a drop's squash.</summary>
    private void ShowSheetScale()
    {
        if (sheet == null)
            return;
        float wide = 1f + _squash.Value;
        // The depth stays 1: the parts' heights over the sheet are metres (PaperLayers), so a paper read large never lifts its photo through the paper over it.
        sheet.localScale = new Vector3(_sizeNow * wide, _sizeNow * wide, 1f);
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

    /// <summary>
    /// Carries the paper along <paramref name="way"/> (world points after
    /// where it is: DockPath, over a machine's casing, onto its bed or pad,
    /// into its slot and out) in <paramref name="seconds"/>, turning it to
    /// lie at <paramref name="facing"/> (DeskCarry), then calls
    /// <paramref name="done"/>. It starts from where it shows
    /// (CarriedFrom): how far it still hangs over its place moves into its root.
    /// </summary>
    public void Carry(IReadOnlyList<Vector3> way, Quaternion facing, float seconds, Action done)
    {
        float raised = Mathf.Max(0f, _lift.Value - _lift.Target);
        if (raised > 0f)
        {
            transform.position += Vector3.up * raised;
            _lift.Snap(_lift.Target);
            _settling = false;
            ShowFeel();
        }
        _carry.Start(transform, way, facing, seconds, done);
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

    /// <summary>A sheet-local rectangle (metres, centre origin, y up) as a share of the paper (x from its left, y from its top).</summary>
    private Rect Share(Rect local) =>
        new Rect((local.xMin + Size.x / 2f) / Size.x, (Size.y / 2f - local.yMax) / Size.y, local.width / Size.x, local.height / Size.y);

    /// <summary>A form-space rectangle (the placed form's units from the page's top-left, y down) in the sheet's local space (metres, centre origin, y up).</summary>
    private Rect Local(FaceRect f) =>
        new Rect((f.XMin - _form.Width / 2f) * _scale, (_form.PageHeight / 2f - f.YMax) * _scale, f.Width * _scale, f.Height * _scale);
}
