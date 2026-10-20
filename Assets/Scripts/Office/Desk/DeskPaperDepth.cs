using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The modelled body of one thing lying on the desk (Saleh 2026-10-17: "use
/// canva and blender to make actual 3d documents"): a paper, the rulebook
/// folder, a folder's tab. The models are Blender's
/// (Assets/Art/Office/Gameplay/Documents, made by tools/props/documents), each
/// a Top (the readable face, mapped so the face art lands where the flat quad
/// showed it, printed over by the paper's texts) and a Body (its sides, page
/// blocks, stitches and rims, in the edge atlas), in the face quad's unit
/// space, drawn in the cel shader TimeDesk/DocumentCel (no hour's light
/// crosses the text; the desk light's level and tint, DeskLight.hlsl, do). A
/// paper wears its kind's model when it binds (Wear: a sheet with a gentle
/// curl and a lifted corner, an open passport booklet, a thick card); the
/// folder and its tabs wear theirs from Build Office UI.
/// Under it lies a soft contact shadow in the cel kit's ink
/// (PaperDepth.Shadow): tight at rest, and while its drag holds it up wider,
/// softer and further to the bottom right, easing back when it is dropped
/// (DeskConfigSO's shadow knobs); hovered, it lifts part of the way (D15:
/// papers lift, HoverLift); darker the higher it lies in the papers' stack
/// (BUG-40c: DeskEvents.StackChanged, PaperDepth.RankStrength). A page turn
/// flicks its loose sheets (Flick: D3 retires "the folder never bends"), and
/// every bend it shows is reported (Bent) so the folder's printed rows ride it
/// (DeskRulebook). A shut cover hides the page it folded off (a booklet's visa
/// page, ClipAbove; the folder's left board, ClipLeftOf). Nothing here takes
/// a click: the click boxes are the face's own.
/// </summary>
public sealed class DeskPaperDepth : MonoBehaviour
{
    /// <summary>The face's mesh filter (a paper's quad): Wear gives it its kind's Top.</summary>
    [SerializeField] private MeshFilter face;

    /// <summary>The body's mesh filter (the model's sides, in the edge atlas): Wear gives it its kind's Body.</summary>
    [SerializeField] private MeshFilter body;

    /// <summary>The contact shadow (Paper_Shadow: blended over whatever lies under it).</summary>
    [SerializeField] private MeshFilter shadow;

    /// <summary>The thing's drag (optional): while it holds the thing up the shadow lifts with it.</summary>
    [SerializeField] private DeskDraggable drag;

    /// <summary>The papers' models (Top, then Body): a single sheet, an open passport booklet, a thick card.</summary>
    [SerializeField] private Mesh sheetTop, sheetBody, bookletTop, bookletBody, cardTop, cardBody;

    /// <summary>The booklet's baked shadows on itself (its page blocks on its covers, its pages into its gutter): the face's _ShadeMap while it wears the booklet.</summary>
    [SerializeField] private Texture2D bookletShade;

    private static readonly int ShadeMapId = Shader.PropertyToID("_ShadeMap");
    private static readonly int FlexId = Shader.PropertyToID("_Flex"), Flex2Id = Shader.PropertyToID("_Flex2"), FlexSizeId = Shader.PropertyToID("_FlexSize");
    private static readonly int ClipId = Shader.PropertyToID("_Clip");

    /// <summary>Raised with every bend the model shows (SetBend, a flick's every step; PaperBend.Flat when it lies flat again): the folder's printed rows ride it (DeskRulebook).</summary>
    public event Action<PaperBend> Bent;

    /// <summary>The face's size the bend shown is laid over (metres).</summary>
    public Vector2 BendSize => _bendSize;

    /// <summary>The model's renderers (its face's, its body's and its shadow's), found once.</summary>
    private Renderer[] _flexed;
    private MaterialPropertyBlock _flexBlock;
    private PaperBend _bendShown = PaperBend.Flat;

    /// <summary>How far the contact shadow reaches beside the thing at most (metres: its held spread and lean): it sinks under a bend by the slope over this (PaperDepth.shader).</summary>
    private float ShadowReach => _config != null ? Mathf.Max(_config.shadowHeldSpread + _config.shadowHeldOffset, _config.shadowSpread + _config.shadowOffset) : 0.05f;
    private Vector2 _bendSize;

    /// <summary>How far under the bottom of the thing the shadow lies (metres): clear of it for the depth buffer.</summary>
    private const float ShadowGap = 0.00005f;

    /// <summary>How long a hover lift eases up or down (seconds; GAME_FEEL_BIBLE's Balanced spring, about 90 ms).</summary>
    private const float HoverSeconds = 0.09f;

    private DeskConfigSO _config;
    private List<(float x, float y)> _outline = new List<(float x, float y)>();
    private Vector2 _size = Vector2.one;
    private float _thickness;
    private float _strength = 1f;
    private bool _built;
    private Mesh _shadowMesh;

    /// <summary>The shadow mesh's corners and colours (reused: a lift rebuilds it without allocating, BUG-65g).</summary>
    private readonly List<Vector3> _shadowVertices = new List<Vector3>();
    private readonly List<Color> _shadowColours = new List<Color>();

    /// <summary>How far the thing is held up now, 0 lying to 1 held (eases between them).</summary>
    private float _held;

    /// <summary>The share of a hold the hover lifts it by (0: not hovered; HoverLift).</summary>
    private float _hover;

    /// <summary>True from a hold until its drop has eased down (the drop keeps the config's pace).</summary>
    private bool _fromHold;

    /// <summary>Its contact shadow's darkness for its place in the papers' stack (PaperDepth.RankStrength).</summary>
    private float _rank = 1f;

    /// <summary>The paper this body belongs to (its rank comes with it), found once; null for the folder and its tabs.</summary>
    private DeskDocument _paper;

    /// <summary>A page turn's flick: its bend at its start, how long it lasts and how far through it is (seconds).</summary>
    private PaperBend _flick = PaperBend.Flat;
    private float _flickSeconds, _flickElapsed;

    private void Awake() => _paper = GetComponentInParent<DeskDocument>(true);

    private void OnEnable() => DeskEvents.StackChanged += HandleStackChanged;

    private void OnDisable() => DeskEvents.StackChanged -= HandleStackChanged;

    /// <summary>A paper's place in the stack changed: its shadow darkens with its rank (BUG-40c).</summary>
    private void HandleStackChanged(DeskDocument paper, int rank)
    {
        if (paper == null || paper != _paper)
            return;
        float strength = PaperDepth.RankStrength(rank);
        if (Mathf.Approximately(strength, _rank))
            return;
        _rank = strength;
        ShowShadow();
    }

    /// <summary>A paper takes its kind's model (<paramref name="kind"/>: a sheet, a booklet, a card; the folder wears its own from the builder).</summary>
    public void Wear(PaperKind kind)
    {
        (Mesh top, Mesh side) = kind == PaperKind.Booklet ? (bookletTop, bookletBody) : kind == PaperKind.Card ? (cardTop, cardBody) : (sheetTop, sheetBody);
        if (face != null && top != null)
            face.sharedMesh = top;
        if (body != null)
            body.sharedMesh = side;
        if (face != null && face.TryGetComponent(out Renderer faceRenderer))
        {
            var block = new MaterialPropertyBlock();
            faceRenderer.GetPropertyBlock(block);
            block.SetTexture(ShadeMapId, kind == PaperKind.Booklet && bookletShade != null ? bookletShade : Texture2D.whiteTexture);
            faceRenderer.SetPropertyBlock(block);
        }
    }

    /// <summary>
    /// Bends the model while it moves (PaperPhysics, PaperBend; Saleh's 1018a
    /// playtest: "all documents are stiff"): its flexible parts (loose sheets,
    /// pages; DocumentCel's vertex alpha) take <paramref name="bend"/> (its
    /// bow along its travel, its twist and its ripple) over a face of
    /// <paramref name="size"/> metres. Flat again with PaperBend.Flat.
    /// </summary>
    public void SetBend(PaperBend bend, Vector2 size)
    {
        if (bend.Equals(_bendShown) && size == _bendSize)
            return;
        _bendShown = bend;
        _bendSize = size;
        _flexBlock ??= new MaterialPropertyBlock();
        foreach (Renderer r in Flexed())
        {
            r.GetPropertyBlock(_flexBlock);
            _flexBlock.SetVector(FlexId, new Vector4(bend.DirX, bend.DirY, bend.Bow, bend.Lag));
            _flexBlock.SetVector(Flex2Id, new Vector4(bend.Twist, bend.Flutter, bend.Phase, bend.Waves));
            _flexBlock.SetVector(FlexSizeId, new Vector4(size.x, size.y, ShadowReach, 0f));
            r.SetPropertyBlock(_flexBlock);
        }
        Bent?.Invoke(bend);
    }

    /// <summary>
    /// A page turn (D3: the folder bends now; GAME_FEEL_BIBLE A3, a bow of at
    /// most 3 mm on the flip): the loose sheets take <paramref name="bend"/>
    /// at once and ease flat over <paramref name="seconds"/> (none under
    /// Reduced Motion), over a face of <paramref name="size"/> metres.
    /// </summary>
    public void Flick(PaperBend bend, Vector2 size, float seconds)
    {
        if (UiMotion.Amount.Still || seconds <= 0f)
            return;
        _flick = bend;
        _flickSeconds = seconds;
        _flickElapsed = 0f;
        SetBend(bend, size);
    }

    /// <summary>
    /// Hides the face and the body above <paramref name="y"/> (the face's unit
    /// space, up the page; DocumentCel's _Clip): a booklet's visa page while
    /// its cover is shut over the data page (PaperCover); a value past the
    /// face's top edge shows it whole.
    /// </summary>
    public void ClipAbove(float y) => Clip(new Vector4(0f, 1f, y, 0f));

    /// <summary>Hides the face and the body left of <paramref name="x"/> (the face's unit space, across): the folder's left board while it is shut over the right sheet (FolderCover); a value past the face's left edge shows it whole.</summary>
    public void ClipLeftOf(float x) => Clip(new Vector4(-1f, 0f, -x, 0f));

    /// <summary>DocumentCel's _Clip on the face and the body (never the shadow): cut where a x + b y > c.</summary>
    private void Clip(Vector4 line)
    {
        _flexBlock ??= new MaterialPropertyBlock();
        foreach (Renderer r in Flexed())
        {
            if (shadow != null && r.gameObject == shadow.gameObject)
                continue;
            r.GetPropertyBlock(_flexBlock);
            _flexBlock.SetVector(ClipId, line);
            r.SetPropertyBlock(_flexBlock);
        }
    }

    /// <summary>The hover lift (D15: a paper under the pointer lifts, Papers, Please-style, and no outline is drawn on it): <paramref name="share"/> of a hold's shadow lift (0 lying; InteractionFeedbackSO.paperHoverLift), eased like a hold.</summary>
    public void HoverLift(float share) => _hover = Mathf.Clamp01(share);

    /// <summary>The face's, the body's and the shadow's renderers (found once).</summary>
    private Renderer[] Flexed()
    {
        if (_flexed != null)
            return _flexed;
        var found = new List<Renderer>();
        if (face != null && face.TryGetComponent(out Renderer faceRenderer))
            found.Add(faceRenderer);
        if (body != null && body.TryGetComponent(out Renderer bodyRenderer))
            found.Add(bodyRenderer);
        if (shadow != null && shadow.TryGetComponent(out Renderer shadowRenderer))
            found.Add(shadowRenderer); // its contact shadow bends with it: the drooping face never dips through it
        _flexed = found.ToArray();
        return _flexed;
    }

    /// <summary>
    /// Lays the thing's contact shadow under <paramref name="outline"/> (the
    /// face's unit space; <paramref name="size"/> its metres), at the foot of
    /// its <paramref name="thickness"/>, from <paramref name="config"/>'s
    /// shadow knobs (<paramref name="strength"/> scales its darkness).
    /// </summary>
    public void Build(List<(float x, float y)> outline, float thickness, Vector2 size, DeskConfigSO config, float strength = 1f)
    {
        _config = config;
        _outline = outline ?? new List<(float x, float y)>();
        _size = size;
        _thickness = thickness;
        _strength = strength;
        _built = true;
        _held = drag != null && drag.IsDragging ? 1f : 0f;
        ShowShadow();
    }

    /// <summary>Only while something changes: eases the shadow toward held, hovered or lying (at once under Reduced Motion), and runs a page turn's flick flat.</summary>
    private void Update()
    {
        if (_flickSeconds > 0f)
        {
            _flickElapsed += Time.unscaledDeltaTime;
            float left = 1f - Mathf.Clamp01(_flickElapsed / _flickSeconds);
            PaperBend fading = left <= 0f ? PaperBend.Flat
                : new PaperBend(_flick.DirX, _flick.DirY, _flick.Bow * left * left, _flick.Lag, _flick.Twist * left * left, 0f, 0f, 0f);
            SetBend(fading, _bendSize);
            if (left <= 0f)
                _flickSeconds = 0f;
        }
        if (!_built || _config == null)
            return;
        bool held = drag != null && drag.IsDragging;
        float target = held ? 1f : _hover;
        if (Mathf.Approximately(_held, target))
            return;
        if (held)
            _fromHold = true;
        // a hold and its drop ease at the config's pace; a hover's lift at the Balanced spring's
        float seconds = UiMotion.Amount.Still ? 0f : _fromHold ? (target > _held ? _config.shadowLiftSeconds : _config.shadowDropSeconds) : HoverSeconds;
        _held = seconds > 0f ? Mathf.MoveTowards(_held, target, Time.unscaledDeltaTime / seconds) : target;
        if (!held && _held <= target + 1e-4f)
            _fromHold = false;
        ShowShadow();
    }

    /// <summary>The shadow at the hold now: between the config's lying and held spread, offset and darkness, darker by its rank in the stack.</summary>
    private void ShowShadow()
    {
        if (shadow == null || _config == null)
            return;
        float eased = _held * _held * (3f - 2f * _held);
        float spread = Mathf.Lerp(_config.shadowSpread, _config.shadowHeldSpread, eased);
        float offset = Mathf.Lerp(_config.shadowOffset, _config.shadowHeldOffset, eased);
        float alpha = Mathf.Clamp01(Mathf.Lerp(_config.shadowAlpha, _config.shadowHeldAlpha, eased) * _strength * _rank);
        DepthMesh mesh = PaperDepth.Shadow(_outline, _size.x, _size.y, _thickness + ShadowGap, spread,
                                           offset * PaperDepth.LeanRight, -offset * PaperDepth.LeanNear, alpha);
        _shadowVertices.Clear();
        foreach ((float x, float y, float z) v in mesh.Vertices)
            _shadowVertices.Add(new Vector3(v.x, v.y, v.z));
        _shadowColours.Clear();
        foreach (Rgba c in mesh.Colours)
            _shadowColours.Add(new Color(c.R, c.G, c.B, c.A));
        _shadowMesh ??= new Mesh { name = "PaperShadow" };
        _shadowMesh.Clear();
        _shadowMesh.SetVertices(_shadowVertices);
        _shadowMesh.SetColors(_shadowColours);
        _shadowMesh.SetTriangles(mesh.Triangles, 0);
        _shadowMesh.RecalculateBounds();
        shadow.sharedMesh = _shadowMesh;
    }

    private void OnDestroy()
    {
        if (_shadowMesh != null)
            Destroy(_shadowMesh);
    }
}
