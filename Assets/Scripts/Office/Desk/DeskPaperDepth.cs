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
/// space, drawn in the unlit cel shader TimeDesk/DocumentCel (no hour's light
/// crosses the text). A paper wears its kind's model when it binds (Wear: a
/// sheet with a gentle curl and a lifted corner, an open passport booklet, a
/// thick card); the folder and its tabs wear theirs from Build Office UI.
/// Under it lies a soft contact shadow in the cel kit's ink
/// (PaperDepth.Shadow): tight at rest, and while its drag holds it up wider,
/// softer and further to the bottom right, easing back when it is dropped
/// (DeskConfigSO's shadow knobs). Nothing here takes a click: the click boxes
/// are the face's own.
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

    /// <summary>How far under the bottom of the thing the shadow lies (metres): clear of it for the depth buffer.</summary>
    private const float ShadowGap = 0.00005f;

    private DeskConfigSO _config;
    private List<(float x, float y)> _outline = new List<(float x, float y)>();
    private Vector2 _size = Vector2.one;
    private float _thickness;
    private float _strength = 1f;
    private bool _built;
    private Mesh _shadowMesh;

    /// <summary>How far the thing is held up now, 0 lying to 1 held (eases between them).</summary>
    private float _held;

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

    /// <summary>Only while the hold changes: eases the shadow toward held or lying (at once under Reduced Motion).</summary>
    private void Update()
    {
        if (!_built || _config == null)
            return;
        float target = drag != null && drag.IsDragging ? 1f : 0f;
        if (Mathf.Approximately(_held, target))
            return;
        float seconds = UiMotion.Amount.Still ? 0f : (target > _held ? _config.shadowLiftSeconds : _config.shadowDropSeconds);
        _held = seconds > 0f ? Mathf.MoveTowards(_held, target, Time.unscaledDeltaTime / seconds) : target;
        ShowShadow();
    }

    /// <summary>The shadow at the hold now: between the config's lying and held spread, offset and darkness.</summary>
    private void ShowShadow()
    {
        if (shadow == null || _config == null)
            return;
        float eased = _held * _held * (3f - 2f * _held);
        float spread = Mathf.Lerp(_config.shadowSpread, _config.shadowHeldSpread, eased);
        float offset = Mathf.Lerp(_config.shadowOffset, _config.shadowHeldOffset, eased);
        float alpha = Mathf.Lerp(_config.shadowAlpha, _config.shadowHeldAlpha, eased) * _strength;
        DepthMesh mesh = PaperDepth.Shadow(_outline, _size.x, _size.y, _thickness + ShadowGap, spread,
                                           offset * PaperDepth.LeanRight, -offset * PaperDepth.LeanNear, alpha);
        _shadowMesh ??= new Mesh { name = "PaperShadow" };
        _shadowMesh.Clear();
        _shadowMesh.SetVertices(mesh.Vertices.ConvertAll(v => new Vector3(v.x, v.y, v.z)));
        _shadowMesh.SetColors(mesh.Colours.ConvertAll(c => new Color(c.R, c.G, c.B, c.A)));
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
