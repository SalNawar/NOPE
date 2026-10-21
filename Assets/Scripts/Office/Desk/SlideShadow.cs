using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The contact shadow under a slider of the desk's slide-in (UNITY_PLAN 5.1:
/// "a decal or blob shadow at the home, not the main-light shadow alone";
/// 5.3: MachineSlideIn fades it in; render/slides.json's shadow cues), so a
/// lift reads as a lift and a track's base visibly leaves its well: a soft
/// shadow in the cel kit's ink (PaperDepth.Shadow, the papers' own contact
/// shadow and their material, Paper_Shadow), lying on the desk under the
/// object's rest footprint. A lift's stays there, fading and widening as it
/// rises; a track's travels with its base and presses at the peak (both from
/// ProximityAssist's pose: alpha, blur, widening). Its mesh is built once;
/// each frame only its alpha (vertex colours) and its size change, so it
/// allocates nothing. Drawn only while it shows.
/// </summary>
public sealed class SlideShadow : MonoBehaviour
{
    private static readonly List<(float x, float y)> Square = new List<(float x, float y)> { (-0.5f, -0.5f), (0.5f, -0.5f), (0.5f, 0.5f), (-0.5f, 0.5f) };

    private Mesh _mesh;
    private MeshRenderer _renderer;
    private Color[] _base = Array.Empty<Color>(), _shown = Array.Empty<Color>();
    private Vector2 _size;
    private float _spread, _alpha = -1f;

    /// <summary>
    /// A contact shadow under <paramref name="parent"/> in
    /// <paramref name="material"/> (the papers' Paper_Shadow) for a rest
    /// footprint of <paramref name="size"/> metres (the desk's x by its y)
    /// with a soft edge of <paramref name="spread"/> metres; hidden until
    /// shown.
    /// </summary>
    public static SlideShadow Make(Transform parent, string name, Material material, Vector2 size, float spread)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var shadow = go.AddComponent<SlideShadow>();
        shadow.Build(material, size, spread);
        return shadow;
    }

    /// <summary>
    /// Shows it this frame: centred on <paramref name="centre"/> (world, on
    /// the desk under the object) and squared to <paramref name="frame"/>, at
    /// <paramref name="alpha"/>, its soft edge <paramref name="spread"/>
    /// metres and its footprint <paramref name="widen"/> times its rest size;
    /// hidden at no alpha.
    /// </summary>
    public void Show(DeskFrame frame, Vector3 centre, float alpha, float spread, float widen)
    {
        bool shows = alpha > 1e-3f;
        if (_renderer.enabled != shows)
            _renderer.enabled = shows;
        if (!shows)
            return;
        if (Mathf.Abs(alpha - _alpha) > 1e-4f)
        {
            _alpha = alpha;
            for (int i = 0; i < _base.Length; i++)
            {
                Color c = _base[i];
                c.a *= alpha;
                _shown[i] = c;
            }
            _mesh.colors = _shown;
        }
        // Its edge grows by the spread over the one it was built with, its footprint by the widening.
        float kx = (_size.x * widen + 2f * spread) / Mathf.Max(1e-5f, _size.x + 2f * _spread);
        float ky = (_size.y * widen + 2f * spread) / Mathf.Max(1e-5f, _size.y + 2f * _spread);
        // Its local x along the desk's right, y along its forward, z down (PaperDepth's depth); its parent's scale is uniform.
        Transform t = transform;
        t.SetPositionAndRotation(centre, Quaternion.LookRotation(Vector3.down, frame.Forward));
        float k = t.parent != null ? Mathf.Max(1e-5f, t.parent.lossyScale.x) : 1f;
        t.localScale = new Vector3(_size.x * kx / k, _size.y * ky / k, 1f / k);
    }

    /// <summary>Hidden (its slider is off: MachineSlideIn's OnDisable).</summary>
    public void Hide()
    {
        if (_renderer != null)
            _renderer.enabled = false;
    }

    private void Build(Material material, Vector2 size, float spread)
    {
        _size = new Vector2(Mathf.Max(1e-3f, size.x), Mathf.Max(1e-3f, size.y));
        _spread = Mathf.Max(0f, spread);
        DepthMesh shape = PaperDepth.Shadow(Square, _size.x, _size.y, 0f, _spread, 0f, 0f, 1f);
        var vertices = new Vector3[shape.Vertices.Count];
        _base = new Color[shape.Colours.Count];
        _shown = new Color[_base.Length];
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = new Vector3(shape.Vertices[i].x, shape.Vertices[i].y, shape.Vertices[i].z);
        for (int i = 0; i < _base.Length; i++)
            _base[i] = new Color(shape.Colours[i].R, shape.Colours[i].G, shape.Colours[i].B, shape.Colours[i].A);
        _mesh = new Mesh { name = "SlideShadow" };
        _mesh.vertices = vertices;
        _mesh.colors = _base;
        _mesh.SetTriangles(shape.Triangles, 0);
        _mesh.RecalculateBounds();
        _mesh.MarkDynamic();
        gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
        _renderer = gameObject.AddComponent<MeshRenderer>();
        _renderer.sharedMaterial = material;
        _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _renderer.receiveShadows = false;
        _renderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (_mesh != null)
            Destroy(_mesh);
    }
}
