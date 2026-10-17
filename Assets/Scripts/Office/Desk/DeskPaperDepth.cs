using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The drawn depth of one thing lying on the desk (Saleh 2026-10-17, after
/// demo 1016a: "all documents need to feel 3d especially the folder"): a
/// paper, the rulebook folder, a folder's tab. Three runtime meshes under its
/// face, in its unit space (the face quad is scaled to the thing's size), in
/// the unlit TimeDesk/PaperDepth materials (no hour's light crosses the
/// text): its Edge, the side walls of its outline leaning out toward the
/// bottom right of the reading view in its kind's bands (PaperEdge: a sheet's
/// edge, a booklet's cover and pages, the folder's board, each ending in an
/// ink line), a single sheet's lifted corner leaning further; its Shadow, a
/// soft contact shadow in the cel kit's ink under it (PaperDepth.Shadow):
/// tight at rest, and while its drag holds it up wider, softer and further
/// to the bottom right, easing back when it is dropped (DeskConfigSO's
/// shadow knobs); and its Shade, the cel shading its owner draws on its face
/// (a sheet's curl, a booklet's gutter, the folder's spine and page edges).
/// Nothing here takes a click: the click boxes are the face's own.
/// </summary>
public sealed class DeskPaperDepth : MonoBehaviour
{
    /// <summary>The edge's side walls (Paper_Lip: opaque, writing depth, so the thing's own shadow never darkens them).</summary>
    [SerializeField] private MeshFilter edge;

    /// <summary>The contact shadow (Paper_Shadow: blended over whatever lies under it).</summary>
    [SerializeField] private MeshFilter shadow;

    /// <summary>The face's cel shading (Paper_Shade: over the print, under the texts); optional.</summary>
    [SerializeField] private MeshFilter shade;

    /// <summary>The thing's drag (optional): while it holds the thing up the shadow lifts with it.</summary>
    [SerializeField] private DeskDraggable drag;

    /// <summary>How far under the bottom of the edge the shadow lies (metres): clear of the edge's foot.</summary>
    private const float ShadowGap = 0.00005f;

    private DeskConfigSO _config;
    private List<(float x, float y)> _outline = new List<(float x, float y)>();
    private List<float> _lift;
    private Vector2 _size = Vector2.one;
    private float _thickness;
    private float _strength = 1f;
    private bool _built;
    private Mesh _edgeMesh, _shadowMesh, _shadeMesh;

    /// <summary>How far the thing is held up now, 0 lying to 1 held (eases between them).</summary>
    private float _held;

    /// <summary>
    /// Builds the thing's depth: the edge of <paramref name="outline"/> (the
    /// face's unit space; <paramref name="size"/> its metres) in the bands of
    /// <paramref name="kind"/> <paramref name="thickness"/> metres deep,
    /// coloured by <paramref name="tone"/>, its walls leaning by the config's
    /// edgeLean and further at a lifted corner (<paramref name="lift"/>,
    /// PaperDepth.CornerLift; null: none); its shadow (<paramref name="strength"/>
    /// scales its darkness); and <paramref name="faceShade"/> on its face (null: none).
    /// </summary>
    public void Build(List<(float x, float y)> outline, PaperKind kind, float thickness, Vector2 size, System.Func<EdgeTone, Color> tone,
                      List<float> lift, DepthMesh faceShade, DeskConfigSO config, float strength = 1f)
    {
        _config = config;
        _outline = outline ?? new List<(float x, float y)>();
        _lift = lift;
        _size = size;
        _thickness = thickness;
        _strength = strength;
        _built = true;
        if (edge != null)
        {
            var vertices = new List<(float x, float y, float z)>();
            var tones = new List<EdgeTone>();
            var triangles = new List<int>();
            float lean = config != null ? config.edgeLean : 0f;
            PaperEdge.Walls(_outline, PaperEdge.Bands(kind, thickness), vertices, tones, triangles, PaperDepth.Lean(lean, size.x, size.y), lift);
            _edgeMesh ??= new Mesh { name = "PaperEdge" };
            _edgeMesh.Clear();
            _edgeMesh.SetVertices(vertices.ConvertAll(v => new Vector3(v.x, v.y, v.z)));
            _edgeMesh.SetColors(tones.ConvertAll(t => tone(t)));
            _edgeMesh.SetTriangles(triangles, 0);
            _edgeMesh.RecalculateBounds();
            edge.sharedMesh = _edgeMesh;
        }
        if (shade != null)
            Apply(shade, faceShade ?? new DepthMesh(), ref _shadeMesh, "PaperShade");
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
        // From the edge's foot (its lean times its thickness out to the bottom right), so the shadow is never hidden under the edge.
        float offset = _config.edgeLean * _thickness + Mathf.Lerp(_config.shadowOffset, _config.shadowHeldOffset, eased);
        float alpha = Mathf.Lerp(_config.shadowAlpha, _config.shadowHeldAlpha, eased) * _strength;
        DepthMesh mesh = PaperDepth.Shadow(_outline, _size.x, _size.y, _thickness + ShadowGap, spread,
                                           offset * PaperDepth.LeanRight, -offset * PaperDepth.LeanNear, alpha, _lift);
        Apply(shadow, mesh, ref _shadowMesh, "PaperShadow");
    }

    /// <summary>Copies <paramref name="from"/> into <paramref name="filter"/>'s own mesh (made once).</summary>
    private static void Apply(MeshFilter filter, DepthMesh from, ref Mesh mesh, string name)
    {
        mesh ??= new Mesh { name = name };
        mesh.Clear();
        mesh.SetVertices(from.Vertices.ConvertAll(v => new Vector3(v.x, v.y, v.z)));
        mesh.SetColors(from.Colours.ConvertAll(Colour));
        mesh.SetTriangles(from.Triangles, 0);
        mesh.RecalculateBounds();
        filter.sharedMesh = mesh;
    }

    /// <summary>An engine-free colour as Unity's.</summary>
    private static Color Colour(Rgba c) => new Color(c.R, c.G, c.B, c.A);

    private void OnDestroy()
    {
        if (_edgeMesh != null)
            Destroy(_edgeMesh);
        if (_shadowMesh != null)
            Destroy(_shadowMesh);
        if (_shadeMesh != null)
            Destroy(_shadeMesh);
    }
}
