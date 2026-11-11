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
/// PaperClicks); a right-click is no click (it backs out, ControlRules).
/// Its size (DOC-HANDLE, BUG-1; Saleh's D1: "no need to enlarge the
/// documents"): one world size per zone, whatever the view (PaperSizes):
/// small on the counter, its real size on the desk (SetZone; the table in
/// DeskConfigSO.readClose at one common scale), easing
/// between the two only as a held paper's pointer crosses the counter's edge
/// (the held point stays under the cursor: DeskDraggable.GrabScale), and its
/// docked size in a machine until it is clear of it (SetDocked); the reading
/// view's tilt, a click and a drop never change it. In inspect mode the
/// paper under the pointer lifts toward the camera until its print reads
/// (ReadClose: the read-close lift, one paper at a time) and settles back
/// when the pointer leaves it. Held, it is drawn above
/// everything on the desk (HeldOnTop: moved toward the camera along its rays,
/// its picture unchanged), its swing pivoted on the held point; let go, it
/// rests exactly where it was released, settling as much as it was moving
/// (PaperPhysics); an ID card is rigid (Saleh's D3). On the desk it is evenly
/// lit (its unlit reading material and the photo in the reading tint;
/// DeskZones); in inspect mode every box is
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

    /// <summary>The sheet's world scale lying in (or held over) <paramref name="zone"/>: its real size on the desk (DeskConfigSO.readClose: the table's width at one common scale), the counter's share of that on the counter; no view enters it (PaperSizes.SheetScale).</summary>
    private float ZoneScale(DeskZone zone)
    {
        if (_config == null || Size.x <= 0f || Size.y <= 0f)
            return 1f;
        float rest = _config.readClose.RestWidth(_frame, Size.x / Size.y) / Size.x;
        return PaperSizes.SheetScale(zone, _config.counterScale * rest, rest);
    }

    /// <summary>The scale the sheet shows at once its ease ends: its docked scale while docked, else its zone's.</summary>
    private float TargetScale => _docked ? _dockScale : ZoneScale(Zone);

    /// <summary>The paper's normal docked scale (its counter size): a machine takes it at this size or smaller (DockFit).</summary>
    public float DockScale => ZoneScale(DeskZone.Counter);

    /// <summary>
    /// The paper goes into a machine (<paramref name="docked"/>: its bed, pad
    /// or slot) at <paramref name="scale"/> (DockFit: its counter size, or
    /// smaller to fit the pad; 0 or less: its counter size), easing there on
    /// its way in; or it is clear of the machine again (the desk calls it only
    /// once the paper has come back out: it keeps its docked size until then,
    /// BUG-24) and eases back to its zone's size.
    /// </summary>
    public void SetDocked(bool docked, float scale = 0f)
    {
        _docked = docked;
        if (docked)
            _dockScale = scale > 0f ? scale : DockScale;
        EaseTo(TargetScale);
    }

    /// <summary>True while the paper lies docked in a machine (SetDocked): its lift is its own thickness over the dock, not the stack's.</summary>
    public bool IsDocked => _docked;

    /// <summary>The paper's size as it shows now (metres, width by height: its sheet's scale).</summary>
    public Vector2 ShownSize => Size * _sizeNow;

    /// <summary>The paper's size lying in its zone (metres): the size it rests at, out of any machine, once a change of size has eased; the size a drop is judged at (DropResolver), never the size it is easing from or its docked size.</summary>
    public Vector2 ZoneSize => Size * ZoneScale(Zone);

    /// <summary>The paper's size docked in a machine (metres: its docked scale's, width by height).</summary>
    public Vector2 DockedSize => Size * (_docked ? _dockScale : DockScale);

    /// <summary>A refusal (a machine's "no", the counter before the verdict): the paper shakes in place, side to side (PaperPhysics.Shake, DeskConfigSO.refuseShakeDistance), where it lies; it never flies back.</summary>
    public void Shake()
    {
        if (_config != null)
            _physics.Shake(_config.refuseShakeDistance, _config.refuseShakeSeconds, _config.refuseShakeCycles);
    }

    /// <summary>The paper lands where it was carried (a hand-over's slap at the counter, BUG-49b): its ends curl up and settle and it hops, as a full-speed drop does (PaperPhysics.Land); the slap's sound is FEEDBACK's (DeskEvents.DocumentLanded).</summary>
    public void Land()
    {
        MotionKnobs knobs = UiMotion.Knobs;
        float hop = _physics.Land(UiMotion.Amount.Share, knobs);
        if (hop > 0f)
            _lift.Kick(knobs.Get(knobs.paperFeel).KickFor(hop));
        _feelDirty = true;
    }

    /// <summary>
    /// The four corners (world, on the desk plane) of the paper lying with its
    /// root at <paramref name="at"/>, at its zone's size and its rest turn (its
    /// jitter; no swing, tilt, shake or held lift), written into
    /// <paramref name="into"/> (4 long): its footprint where it will rest.
    /// </summary>
    public void RestCorners(Vector3 at, Vector3[] into)
    {
        Vector2 half = ZoneSize / 2f;
        Quaternion lying = transform.rotation * (_sheetRest ?? (sheet != null ? sheet.localRotation : Quaternion.identity));
        into[0] = at + lying * new Vector3(-half.x, -half.y, 0f);
        into[1] = at + lying * new Vector3(half.x, -half.y, 0f);
        into[2] = at + lying * new Vector3(-half.x, half.y, 0f);
        into[3] = at + lying * new Vector3(half.x, half.y, 0f);
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

    /// <summary>How long a change of size takes (seconds: a held paper crossing the counter's edge, a paper going into or out of a machine; a cut under Reduced Motion).</summary>
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

    /// <summary>The paper's frame (its look's): which row of the real-size table it takes (PaperRealSizes.Of).</summary>
    private FormFrame _frame;

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

    /// <summary>The sheet's scale now, the scale it eases toward and where the ease started.</summary>
    private float _sizeNow = 1f, _sizeTarget = 1f, _sizeFrom = 1f, _sizeElapsed;

    /// <summary>The sheet's scale when the running drag picked it up: the grab offset grows with the size from there (DeskDraggable.GrabScale).</summary>
    private float _sizeAtGrab = 1f;

    /// <summary>The paper's feel (the game feel's desk layer): its lift off the desk (a spring toward SetLift's height) and its tilt toward a drag's travel (degrees about the sheet's x and z).</summary>
    private Spring _lift, _tiltX, _tiltZ;

    /// <summary>True once the lift has a place (the first SetLift puts it there at once), the drag's state last frame (a pick-up or a drop plays its cue), and whether the sheet must be drawn again this frame (a size, a landing).</summary>
    private bool _liftPlaced, _wasDragged, _feelDirty;

    /// <summary>The held ratio the sheet was last drawn at (HeldOnTop: 1 when not held): a paper let go is drawn once more at 1.</summary>
    private float _shownRatio = 1f;

    /// <summary>The held point (root-local x, z from the paper's centre) its swing pivots on: kept after a drop until the swing has settled back.</summary>
    private Vector2 _pivot;

    /// <summary>Where the paper was last frame (a drag's speed tilts it).</summary>
    private Vector3 _lastPosition;

    /// <summary>The sheet's own rotation (lying flat, its face up: the builder's), which the tilt turns in the paper's space.</summary>
    private Quaternion? _sheetRest;

    /// <summary>The paper carried along a way (a slide across the desk, or over a machine's casing into its dock and out: DockPath).</summary>
    private readonly DeskCarry _carry = new DeskCarry();

    /// <summary>The lift the stack gives the paper (SetLift).</summary>
    private float _stackLift;

    /// <summary>True while the paper lies docked in a machine (SetDocked), and its scale there.</summary>
    private bool _docked;
    private float _dockScale = 1f;

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
    public Vector3 CarriedFrom => transform.position + Vector3.up * Mathf.Max(0f, _lift.Value - LiftPlace);

    /// <summary>Where the lift is going: the set-down's end while it eases (SetLift), else the spring's target.</summary>
    private float LiftPlace => _lowering ? _lowerTo : _lift.Target;

    /// <summary>The zone the paper is sized for (DeskController sets it as it lies, and as a held paper's pointer crosses the counter's edge).</summary>
    public DeskZone Zone { get; private set; }

    /// <summary>True in inspect mode: every box is tinted and a click picks the box under the pointer.</summary>
    public bool Inspecting { get; private set; }

    /// <summary>The lying sheet (its scale is the zone's size).</summary>
    public Transform Sheet => sheet;

    /// <summary>How many pickable boxes the paper prints.</summary>
    public int SlotCount => _slots.Count;

    /// <summary>Raised on a left-click while the paper takes input: the paper and, in inspect mode, the box under the pointer (-1: none, or not inspecting).</summary>
    public event Action<DeskDocument, int> Clicked;

    /// <summary>The paper is drawn again every time its drag moves it (after the move: the held paper's place toward the camera follows the root that frame), and a pick-up takes note of its size (Grabbed).</summary>
    private void Awake()
    {
        if (drag != null)
        {
            drag.Dragged += Followed;
            drag.DragBegan += Grabbed;
            drag.PressAtRest = AtRest;
        }
    }

    /// <summary>Its drag moved it: drawn again where it now is.</summary>
    private void Followed(DeskDraggable dragged, Vector3 point) => ShowFeel();

    /// <summary>
    /// Its drag began: the size it was grabbed at, which the grab offset grows
    /// from (DeskDraggable.GrabScale). Taken when the drag begins, before the
    /// desk sizes it for the zone under the pointer: Feel, in the next Update,
    /// took it after the size's first eased step, so a paper pulled from the
    /// counter grew its grab offset by 2.1 times while it grew 3.4 times, and
    /// the held point slid 4 cm off the pointer (FOLD-01 round 3). The new
    /// drag owns the root: a growth after an earlier release ends here
    /// (GrowRoundHeldPoint).
    /// </summary>
    private void Grabbed(DeskDraggable dragged)
    {
        _sizeAtGrab = _sizeNow;
        _grownFrom = null;
    }

    /// <summary>The grab scale the running (or the last) drag reaches once the size ends its ease: the size it eases to over the size it was grabbed at (PaperSizes.GrabRatio).</summary>
    public float SettledGrabScale => PaperSizes.GrabRatio(_sizeAtGrab, _sizeTarget);

    /// <summary>
    /// Where the paper's root lies once its size ends its ease, the point its
    /// drag holds kept where the drag's last follow put it
    /// (DeskDraggable.Regrown): where a drop is judged (FOLD-01 round 4, G4).
    /// Its root now when its size does not ease. Read at a drag's release.
    /// </summary>
    public Vector3 SettledRoot => drag != null ? drag.Regrown(transform.position, SettledGrabScale) : transform.position;

    /// <summary>The root where the drag let the paper go while its size still eased (GrowRoundHeldPoint), or null.</summary>
    private Vector3? _grownFrom;

    /// <summary>
    /// The paper was let go where it rests (DeskController) while its size
    /// still eases: its root moves with the rest of the ease, so the point the
    /// drag held stays where the pointer let it go, and the root ends at
    /// SettledRoot, where the drop was judged (FOLD-01 round 4, the round 3
    /// verifier's G4: the paper grew round its root, and the held point
    /// drifted 8.7 px, 39.5 px in the reading view that the drop brings).
    /// Nothing when the size has ended its ease.
    /// </summary>
    public void GrowRoundHeldPoint() =>
        _grownFrom = drag != null && !Mathf.Approximately(_sizeNow, _sizeTarget) ? transform.position : (Vector3?)null;

    /// <summary>After a release mid-ease (GrowRoundHeldPoint): the root goes where the grown offset puts it from the held point, and the swing pivots on that point; it ends with the ease, or as soon as a drag, a carry or a dock takes the paper.</summary>
    private void KeepHeldPointWhileGrowing()
    {
        if (_grownFrom == null)
            return;
        if (drag == null || drag.IsDragging || _carry.Running || _docked)
        {
            _grownFrom = null;
            return;
        }
        Vector3 from = _grownFrom.Value;
        transform.position = drag.Regrown(from, PaperSizes.GrabRatio(_sizeAtGrab, _sizeNow));
        Vector3 held = transform.InverseTransformDirection(drag.Regrown(from, 0f) - transform.position);
        _pivot = new Vector2(held.x, held.z);
        if (Mathf.Approximately(_sizeNow, _sizeTarget))
            _grownFrom = null;
    }

    /// <summary>Each frame: its feel, the ease of a change of size (the grab offset growing with it while held, and the root with it after a release mid-ease), and the carry along a way (its done callback on landing).</summary>
    private void Update()
    {
        if (!Mathf.Approximately(_sizeNow, _sizeTarget))
        {
            _sizeElapsed += Time.deltaTime;
            float seconds = MotionPreference.Reduced ? 0f : resizeSeconds;
            float eased = seconds > 0f ? Mathf.Clamp01(_sizeElapsed / seconds) : 1f;
            _sizeNow = eased >= 1f ? _sizeTarget : Mathf.LerpUnclamped(_sizeFrom, _sizeTarget, UiMotion.Ease(eased, UiMotion.Knobs.deskMoveFeel, seconds));
            _feelDirty = true;
        }
        KeepHeldPointWhileGrowing();
        StepReadClose(Time.unscaledDeltaTime);
        Feel(Time.unscaledDeltaTime);
        if (drag != null && drag.IsDragging)
            drag.GrabScale = PaperSizes.GrabRatio(_sizeAtGrab, _sizeNow);
        _carry.Step(transform, Time.deltaTime);
        if (_onTopEye != null)
            ShowFeel(); // carried above every prop (CarryOnTop): drawn toward the camera from where the carry put it this frame
    }

    /// <summary>The paper's printed meshes go with it (they were built for this paper alone).</summary>
    private void OnDestroy()
    {
        ReadTurns.Want(GetInstanceID(), false);
        ReadTurns.Leave(GetInstanceID());
        if (drag != null)
        {
            drag.Dragged -= Followed;
            drag.DragBegan -= Grabbed;
        }
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
    /// scene built before phase 4) prints nothing but its paper. An ID card is
    /// rigid (Saleh's D3: PaperPhysics.Rigid).
    /// </summary>
    public void Bind(int index, CaseDocument doc, DocumentForm form, DeskConfigSO config)
    {
        Index = index;
        _config = config;
        _slots.Clear();
        _form = null;
        _readNow = 0f;
        _pointerOn = false;
        _frame = FormFrame.Plain;
        if (config != null)
            Size = config.paperSize;
        if (config == null || doc == null || form == null || style == null || textTemplate == null)
            return;

        FormLook look = form.Spec.look ?? new FormLook();
        FormArt art = ArtLayout.IsArt(form.Spec) ? look.art : null;
        _frame = look.frame;
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
        _physics.Rigid = kind == PaperKind.Card;
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
    /// desk): on the counter at its counter size, lit by the room; on the desk
    /// evenly lit (the reading material; the photo in the reading tint instead
    /// of the room's) at its real size (DeskConfigSO.readClose), in every view
    /// (PaperSizes.SheetScale). The size eases there (a held paper crossing
    /// the counter's edge), or is set at once (<paramref name="instant"/>). A
    /// docked paper keeps its docked size until it is clear of its machine.
    /// </summary>
    public void SetZone(DeskZone zone, bool instant)
    {
        Zone = zone;
        float target = TargetScale;
        if (!Mathf.Approximately(target, _sizeTarget))
            EaseTo(target);
        if (instant)
        {
            _sizeNow = _sizeTarget = target;
            _feelDirty = true;
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

        int slot = Inspecting ? SlotUnder(eventData) : -1;
        if (slot >= 0)
            ReadTurns.Settle(GetInstanceID()); // a pick puts the paper down: the next target may lie under it
        Clicked?.Invoke(this, slot);
    }

    /// <summary>In inspect mode, the box under the pointer tints, and the paper asks for the read-close lift (not from the bottom band the lift keeps clear: there the lifted paper would leave the pointer and drop back under it again).</summary>
    public void OnPointerMove(PointerEventData eventData)
    {
        SetHovered(Inspecting && click != null && click.Interactable ? SlotUnder(eventData) : -1);
        _pointerOn = Inspecting && _config != null && eventData.position.y >= _config.readClose.bottomMargin * Screen.height;
        if (!_pointerOn)
        {
            ReadTurns.Leave(GetInstanceID());
            return;
        }
        if (eventData.enterEventCamera != null)
            _readEye = eventData.enterEventCamera;
        ReadTurns.Want(GetInstanceID(), true);
    }

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

    /// <summary>The pointer left the paper: no box tints, and a lifted paper settles back.</summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        SetHovered(-1);
        _pointerOn = false;
        ReadTurns.Leave(GetInstanceID());
    }

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
        _sheetRest = Quaternion.Euler(0f, share * _config.paperJitter, 0f) * _builtRest.Value;
        _feelDirty = true;
    }

    /// <summary>The paper's physics feel (Saleh 2026-10-17: "KEEP IT TOP DOWN BUT GIVE IT PHYSICS"; 1018a: "all documents are stiff"): its swing and bend while held, its settle and hop when let go, its refusal's shake.</summary>
    private readonly PaperPhysics _physics = new PaperPhysics();

    /// <summary>The paper's kind (its model: a sheet, a booklet, a card): how much it bends (MotionKnobs.sheetFlex, bookletFlex, cardFlex; a card is rigid).</summary>
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

    /// <summary>
    /// The photo's rectangle as a share of the paper, which DeskDocument.Render's
    /// PlacePhoto still records: its reader, the desk's spread of papers sent to
    /// the desk by a click, went with the click-to-read (Saleh's D1). On
    /// INTEGRATION's cleanup list (BUG-73c): DOC-LOOK drops the PlacePhoto
    /// line at its merge, then this list and Share go.
    /// </summary>
    private readonly List<Rect> _keys = new List<Rect>();

    /// <summary>A sheet-local rectangle (metres, centre origin, y up) as a share of the paper (x from its left, y from its top): see _keys.</summary>
    private Rect Share(Rect local) =>
        new Rect((local.xMin + Size.x / 2f) / Size.x, (Size.y / 2f - local.yMax) / Size.y, local.width / Size.x, local.height / Size.y);

    /// <summary>Lifts the sheet off the desk plane (its place in the stack, or a dragged paper's lift), in metres: the first place at once, then on the paper's spring (a picked-up paper rises, a dropped one settles), or at once when <paramref name="instant"/> (a reorder of the stack: the order changes in the same frame, BUG-40b) or without motion.</summary>
    public void SetLift(float height, bool instant = false)
    {
        if (sheet == null)
            return;
        _stackLift = height;
        if (!_liftPlaced || instant || UiMotion.Amount.Still)
        {
            _liftPlaced = true;
            _lowering = false;
            _lift.Snap(height);
            _settling = false;
            ShowFeel();
            return;
        }
        if (_lowering && Mathf.Approximately(height, _lowerTo))
            return; // already coming down there
        _settling = height < _lift.Target; // set down: its spring never sinks it below its place (the paper under it, the desk)
        if (_settling)
        {
            // Set down: its spring's target comes down on DeskLiftEase from where it shows, so the frame of the release moves its
            // picture under a pixel (FOLD-01 round 2, the verifier's F2; DOC-HANDLE: "≤1 px" at the release frame); the spring follows.
            _lowerFrom = Mathf.Max(_lift.Value, height);
            _lowerTo = height;
            _lowerElapsed = 0f;
            _lowering = true;
            _lift.Target = _lowerFrom;
            return;
        }
        _lowering = false;
        _lift.Target = height;
    }

    /// <summary>The set-down's ease of the lift's target (SetLift, Feel): from where, to where, how far along, whether it runs.</summary>
    private float _lowerFrom, _lowerTo, _lowerElapsed;
    private bool _lowering;

    /// <summary>
    /// The paper's feel (Saleh 2026-10-07: every touch reads as physical): a
    /// pick-up and a drop play their cues; while dragged the sheet tilts
    /// toward its travel by its speed (MotionKnobs.paperTiltPerSpeed, at most
    /// paperTiltMax), its leading edge dipping; dropped, it flutters back flat
    /// and settles as much as it was moving (PaperPhysics: no squash, no
    /// slide: it never changes size, the feel bible's G5); its lift follows
    /// SetLift on the same spring (paperFeel). Scaled by the Motion intensity;
    /// still under Reduced Motion. While held it is drawn every frame (its
    /// place toward the camera follows the camera); at rest nothing runs.
    /// </summary>
    private void Feel(float dt)
    {
        if (sheet == null || drag == null)
            return;
        bool dragged = drag.IsDragging;
        Vector3 position = transform.position;
        MotionKnobs knobs = UiMotion.Knobs;
        MotionAmount amount = UiMotion.Amount;
        bool picked = dragged && !_wasDragged, dropped = !dragged && _wasDragged;
        if (picked)
            _lastPosition = position; // its size at the grab: Grabbed
        if (dragged)
        {
            Vector3 held = transform.InverseTransformDirection(-drag.GrabOffset);
            _pivot = new Vector2(held.x, held.z);
        }
        float hop = _physics.Step(dt, Vector3.Dot(position, transform.right), Vector3.Dot(position, transform.forward), dragged, knobs, amount);
        if (picked || dropped)
        {
            _wasDragged = dragged;
            Sounds.Play(dragged ? SoundCues.PaperPickup : SoundCues.PaperDrop);
            if (dropped && !amount.Still)
            {
                // the flutter as it lands, as much as it was moving
                float settle = PaperPhysics.SettleShare(_physics.ReleaseSpeed, knobs.paperSettleSpeed);
                _tiltX.Kick(knobs.Get(knobs.paperFeel).KickFor(knobs.paperTiltMax * 0.25f * amount.Share * settle));
            }
        }
        if (hop > 0f)
            _lift.Kick(knobs.Get(knobs.paperFeel).KickFor(hop));
        float sagSeconds = _config != null ? _config.heldSagSeconds : 0f;
        _sagShare = amount.Still || sagSeconds <= 0f ? (dragged ? 1f : 0f) : Mathf.MoveTowards(_sagShare, dragged ? 1f : 0f, dt / sagSeconds);
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
        if (_lowering)
        {
            _lowerElapsed += dt;
            _lift.Target = DeskLiftEase.At(_lowerFrom, _lowerTo, _lowerElapsed);
            if (_lowerElapsed >= DeskLiftEase.Seconds)
                _lowering = false;
        }
        bool resting = _lift.AtRest && _tiltX.AtRest && _tiltZ.AtRest && !_physics.Moving && !_lowering && _readNow <= 0f; // lifted: drawn every frame, the view may move
        if (resting && !dragged && !_feelDirty && _shownRatio == 1f)
        {
            if (bent)
                ShowFeel(); // flat again: down onto its place
            return;
        }
        SpringTuning tuning = knobs.Get(knobs.paperFeel);
        _lift.Step(dt, tuning, knobs.settleValue * 0.01f, knobs.settleSpeed * 0.01f); // metres
        _tiltX.Step(dt, tuning, knobs.settleValue * 10f, knobs.settleSpeed * 10f); // degrees
        _tiltZ.Step(dt, tuning, knobs.settleValue * 10f, knobs.settleSpeed * 10f);
        ShowFeel();
    }

    /// <summary>True while the paper is being set down (its lift's target lowered): its spring's bounce stays above its place.</summary>
    private bool _settling;

    /// <summary>
    /// Draws the sheet: its lift (while set down, never below its place: the
    /// spring's undershoot sank it into the desk or the paper under it)
    /// raised by how far its tilt dips its lowest corner (PaperLayers.TiltDrop:
    /// a drag's lean or a drop's flutter), so no part of it ever goes through
    /// what lies under it (Saleh's 1007d playtest: "sometimes they clip or
    /// render with the table"); its swing pivoted on the held point and its
    /// refusal's shake; its size; and, while held, the whole sheet moved toward
    /// the camera along its rays and shrunk by the same ratio (HeldOnTop: drawn
    /// above every prop with its picture unchanged, BUG-8).
    /// </summary>
    private void ShowFeel()
    {
        if (sheet == null)
            return;
        _feelDirty = false;
        float lift = _settling ? Mathf.Max(_lift.Value, _lift.Target) : _lift.Value;
        float half = _sizeNow / 2f;
        lift += PaperLayers.TiltDrop(Size.x * half, Size.y * half, _tiltX.Value, _tiltZ.Value);
        // a bend's drooping ends never pass through what lies under the paper (a held one has its drag's lift of room; one just
        // let go, as much as its lift still stands over its place, so the release's frame does not lift a drooping paper at once:
        // FOLD-01 round 2, F2)
        bool held = drag != null && drag.IsDragging;
        float room = held ? (_config != null ? _config.dragLift : 0f) : Mathf.Max(0f, _lift.Value - LiftPlace);
        lift += Mathf.Max(0f, _bend.Lowest(Size.x, Size.y) - room);
        (float ox, float oz) = PaperPhysics.PivotOffset(_pivot.x, _pivot.y, _physics.Yaw);
        Vector3 local = new Vector3(ox + _physics.ShakeX, lift, oz);
        _sheetRest ??= sheet.localRotation;
        Quaternion turn = _tiltX.Value == 0f && _tiltZ.Value == 0f && _physics.Yaw == 0f
            ? _sheetRest.Value
            : Quaternion.Euler(0f, _physics.Yaw, 0f) * Quaternion.Euler(_tiltX.Value, 0f, _tiltZ.Value) * _sheetRest.Value;
        Camera eyeCamera = held ? drag.DragCamera : _onTopEye;
        float s = held ? HeldRatio() : _onTopEye != null ? _onTopRatio : 1f;
        if (s < 1f && eyeCamera != null)
        {
            Vector3 eye = transform.InverseTransformPoint(eyeCamera.transform.position);
            local = eye + (local - eye) * s;
        }
        else
            s = 1f;
        _shownRatio = s;
        sheet.localPosition = local;
        sheet.localRotation = turn;
        // The depth stays 1 at rest: the parts' heights over the sheet are metres (PaperLayers), so a paper read large never lifts its photo through the paper over it.
        sheet.localScale = new Vector3(_sizeNow * s, _sizeNow * s, s);
        if (_readNow <= 0f || _readEye == null || !ReadPose(_readEye, out Vector3 at, out float ratio, out float magnify))
            return;
        _restLocal = local;
        _restTurn = turn;
        _restScale = sheet.localScale;
        float e = DeskZones.Ease(_readNow);
        float grow = Mathf.LerpUnclamped(s, ratio * magnify, e);
        sheet.localPosition = Vector3.LerpUnclamped(local, transform.InverseTransformPoint(at), e);
        // lifted, the sheet faces the eye upright (a paper lying flat in the office view shows edge-on, so its print never reads)
        sheet.localRotation = Quaternion.SlerpUnclamped(turn, Quaternion.Inverse(transform.rotation) * Quaternion.LookRotation(_readEye.transform.forward, _readEye.transform.up), e);
        sheet.localScale = new Vector3(_sizeNow * grow, _sizeNow * grow, Mathf.LerpUnclamped(s, ratio, e));
    }

    /// <summary>The one paper on the desk lifted to read (ReadTurn, by instance id).</summary>
    private static readonly ReadTurn ReadTurns = new ReadTurn();

    /// <summary>True while the pointer is over the paper above the lift's bottom band in inspect mode (OnPointerMove, OnPointerExit).</summary>
    private bool _pointerOn;

    /// <summary>The camera the pointer sees the paper through (the lift goes toward it).</summary>
    private Camera _readEye;

    /// <summary>The lift's clock: 0 at rest, 1 lifted (linear; drawn through DeskZones.Ease).</summary>
    private float _readNow;

    /// <summary>The sheet's rest pose under the lift (root-local place, turn, scale), last drawn while lifted: a press on the lifted paper is taken where that point rests (AtRest).</summary>
    private Vector3 _restLocal, _restScale = Vector3.one;
    private Quaternion _restTurn = Quaternion.identity;

    /// <summary>True when the paper takes the read-close lift now: it holds the turn, inspect mode is on, the pointer is over it, it lies on the desk, takes input and is not dragged, carried or docked.</summary>
    private bool ReadWanted => _config != null && _readEye != null && ReadTurns.Lifted == GetInstanceID() && Inspecting && _pointerOn
        && click != null && click.Interactable && (drag == null || !drag.IsDragging) && !_carry.Running && !_docked && Zone == DeskZone.Desk;

    /// <summary>Each frame: the lift's clock moves toward lifted or rest over readClose.seconds (at once under Reduced Motion); a paper that no longer takes the lift gives up its turn.</summary>
    private void StepReadClose(float dt)
    {
        bool wanted = ReadWanted;
        if (!wanted)
            ReadTurns.Want(GetInstanceID(), false);
        float goal = wanted ? 1f : 0f;
        if (_readNow == goal)
            return;
        float seconds = MotionPreference.Reduced || _config == null ? 0f : _config.readClose.seconds;
        _readNow = seconds > 0f ? Mathf.MoveTowards(_readNow, goal, dt / seconds) : goal;
        _feelDirty = true;
    }

    /// <summary>
    /// The lifted pose seen through <paramref name="eye"/>, from the sheet as
    /// drawn now: the sheet turned to face the eye, moved toward it by
    /// <paramref name="ratio"/> (HeldOnTop: in front of every prop) and grown
    /// so it shows <paramref name="magnify"/> times its face-on size at rest
    /// (ReadClose.Magnify: its smallest printed value and label, measured on
    /// its texts, at the type bars, the paper inside the frame), its centre
    /// at <paramref name="at"/> (ReadClose.Clamp: no edge leaves the screen,
    /// the bottom band clear). False when the paper is behind the eye.
    /// </summary>
    private bool ReadPose(Camera eye, out Vector3 at, out float ratio, out float magnify)
    {
        at = default;
        ratio = magnify = 1f;
        Vector3 centre = eye.WorldToScreenPoint(sheet.position);
        float worldH = sheet.TransformVector(0f, Size.y, 0f).magnitude;
        if (centre.z <= 0f || worldH <= 0f || eye.orthographic)
            return false;
        // face-on at its rest depth: pixels per metre there
        float perMetre = eye.pixelHeight / (2f * Mathf.Tan(eye.fieldOfView * 0.5f * Mathf.Deg2Rad) * centre.z);
        float h = worldH * perMetre, w = sheet.TransformVector(Size.x, 0f, 0f).magnitude * perMetre;
        (float valueShare, float labelShare) = LeastCapShares(worldH);
        PaperRealSizes knobs = _config.readClose;
        float bottom = knobs.bottomMargin * Screen.height;
        magnify = ReadClose.Magnify(valueShare * h, labelShare * h, knobs.valueCapPx, knobs.labelCapPx, w, h, Screen.width, Screen.height, bottom, knobs.topCrop);
        (float x, float y) = ReadClose.Clamp(centre.x, centre.y, w * magnify / 2f, h * magnify / 2f, Screen.width, Screen.height, bottom);
        ratio = OnTopRatio(eye);
        Vector3 direction = eye.ScreenPointToRay(new Vector3(x, y, 0f)).direction;
        float along = Vector3.Dot(direction, eye.transform.forward);
        if (along <= 1e-4f)
            return false;
        at = eye.transform.position + direction * (centre.z * ratio / along);
        return true;
    }

    /// <summary>The cap height of the smallest value (a Value or a Hand text) and of the smallest label printed on the sheet, each as a share of the sheet's world height <paramref name="worldH"/> (0: none): the fonts' cap lines at the texts' sizes, as the reader sees them.</summary>
    private (float value, float label) LeastCapShares(float worldH)
    {
        float value = float.MaxValue, label = float.MaxValue;
        foreach (TMP_Text text in sheet.GetComponentsInChildren<TMP_Text>(false))
        {
            bool data = text.name == nameof(FormTextRole.Value) || text.name == nameof(FormTextRole.Hand);
            if ((!data && text.name != nameof(FormTextRole.Label)) || string.IsNullOrWhiteSpace(text.text) || text.font == null || text.font.faceInfo.pointSize <= 0f)
                continue;
            float capLocal = text.font.faceInfo.capLine / text.font.faceInfo.pointSize * text.fontSize * TmpFormText.WorldEmPerFontSize;
            float share = text.transform.TransformVector(0f, capLocal, 0f).magnitude / worldH;
            if (share <= 0f)
                continue;
            if (data)
                value = Mathf.Min(value, share);
            else
                label = Mathf.Min(label, share);
        }
        return (value == float.MaxValue ? 0f : value, label == float.MaxValue ? 0f : label);
    }

    /// <summary>Where a point the press met on the drawn paper lies at rest (DeskDraggable.PressAtRest): on a lifted paper, the same point of the sheet in its rest pose, so a drag begun on it holds it as at rest; else the point itself.</summary>
    private Vector3 AtRest(Vector3 world)
    {
        if (_readNow <= 0f || sheet == null)
            return world;
        Vector3 local = sheet.InverseTransformPoint(world);
        return transform.TransformPoint(_restLocal + _restTurn * Vector3.Scale(local, _restScale));
    }

    /// <summary>The held ratio now (HeldOnTop.Ratio): in front of every registered obstacle (DeskObstacles.MaxHeight) and of every swept box of the desk's slide-in at its peak (DeskLayout.SweptPeak: a machine that slides out after the drag began never passes through the paper; DESK-V3, DESK-HOMES 6), and at least DeskConfigSO.heldClearance over the desk, never nearer than heldLeastRatio; 1 without the drag's camera or the config.</summary>
    private float HeldRatio() => OnTopRatio(drag != null ? drag.DragCamera : null);

    /// <summary>The ratio toward <paramref name="eye"/> that draws the paper in front of every prop (as HeldRatio; the read-close lift's too); 1 without the eye or the config.</summary>
    private float OnTopRatio(Camera eye)
    {
        if (eye == null || _config == null)
            return 1f;
        float clear = Mathf.Max(Mathf.Max(DeskObstacles.Desk.MaxHeight, DeskLayout.SweptPeak()), _config.heldClearance);
        return HeldOnTop.Ratio(eye.transform.position.y - transform.position.y, clear, _config.heldLeastRatio);
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
    /// <paramref name="done"/>; a carry that replaces one still running calls
    /// that one's <paramref name="cut"/> (its machine lets go). It starts from
    /// where it shows (CarriedFrom): how far it still hangs over its place
    /// moves into its root.
    /// </summary>
    public void Carry(IReadOnlyList<Vector3> way, Quaternion facing, float seconds, Action done, Action cut = null)
    {
        if (_lowering)
        {
            _lift.Target = _lowerTo;
            _lowering = false;
        }
        float raised = Mathf.Max(0f, _lift.Value - _lift.Target);
        if (raised > 0f)
        {
            transform.position += Vector3.up * raised;
            _lift.Snap(_lift.Target);
            _settling = false;
            ShowFeel();
        }
        _carry.Start(transform, way, facing, seconds, done, cut);
    }

    /// <summary>
    /// Carries the paper as Carry does, drawn above every prop on the way as
    /// it was held (HeldOnTop: toward <paramref name="eye"/> by
    /// <paramref name="ratio"/>, its picture unchanged) until it lands: a
    /// paper let go over a machine or a prop slides along the desk to its
    /// rest beside it, over what it was let go on, never hopping over a
    /// casing toward the camera (FOLD-01 round 2, the verifier's F4). Without
    /// an eye it is drawn where it lies.
    /// </summary>
    public void CarryOnTop(IReadOnlyList<Vector3> way, Quaternion facing, float seconds, Camera eye, float ratio, Action done)
    {
        Carry(way, facing, seconds, () =>
        {
            _onTopEye = null;
            ShowFeel();
            done?.Invoke();
        }, () =>
        {
            _onTopEye = null;
            _feelDirty = true;
        });
        _onTopEye = eye;
        _onTopRatio = Mathf.Clamp01(ratio);
        ShowFeel();
    }

    /// <summary>The camera a paper carried to its rest is drawn toward and the ratio (CarryOnTop; null while not).</summary>
    private Camera _onTopEye;
    private float _onTopRatio = 1f;

    /// <summary>The paper's quad and its collider at <paramref name="size"/> (metres): the sheet the form is laid out on.</summary>
    private void Resize(Vector2 size)
    {
        Size = size;
        if (paperQuad != null)
            paperQuad.transform.localScale = new Vector3(size.x, size.y, 1f);
        if (sheet != null && sheet.TryGetComponent(out BoxCollider box))
            box.size = new Vector3(size.x, size.y, box.size.z);
    }

    /// <summary>A form-space rectangle (the placed form's units from the page's top-left, y down) in the sheet's local space (metres, centre origin, y up).</summary>
    private Rect Local(FaceRect f) =>
        new Rect((f.XMin - _form.Width / 2f) * _scale, (_form.PageHeight / 2f - f.YMax) * _scale, f.Width * _scale, f.Height * _scale);
}
