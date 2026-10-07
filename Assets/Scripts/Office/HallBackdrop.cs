using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Draws the anime hall's painted layers through the URP 2D Renderer, so the
/// hall's Light2Ds (HallLightingRig) light them, while the office camera keeps
/// its Universal (forward) renderer for the preserved 3D desk (its NOPE/Desk
/// Anime shader has no 2D pass: under a 2D Renderer the desk would not draw
/// at all) and everything the gameplay draws over it (docs/HALL_LIGHTING.md,
/// "The renderer"). URP cannot stack a 2D camera and a forward one, so this
/// makes, at runtime and never saved, a camera with a 2D Renderer of its own
/// (see <see cref="renderer2D"/>) that sees only the HallBackdrop layer (the painted layers, the portal
/// effects, the dust and the Light2Ds live there), copies the office camera's
/// pose and lens each frame before any camera renders, and renders into a
/// texture of the screen's size; and a full-screen triangle under the office
/// camera (TimeDesk/HallBackdrop: drawn first, behind everything, only for that
/// camera) that shows it. The office camera never sees the layer. When the
/// pipeline has no 2D Renderer, or the lighting is switched off, the office
/// camera draws the layer itself (the painted layers' Sprite-Lit material draws
/// them unlit there), so the hall never goes missing.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class HallBackdrop : MonoBehaviour
{
    /// <summary>The office camera (the hall's player camera): its pose, lens and screen are copied.</summary>
    [SerializeField] private Camera source;

    /// <summary>TimeDesk/HallBackdrop (the full-screen draw of the lit hall).</summary>
    [SerializeField] private Shader composite;

    /// <summary>
    /// The 2D Renderer the camera renders with (Assets/Settings/HallRenderer2D.asset,
    /// in the pipeline's renderer list): its own, so its per-frame light and
    /// shadow texture tables are not rebuilt each time the PC's desktop cameras
    /// (on the default 2D Renderer, with other layer batches) render, which
    /// allocated every frame; null or unlisted: the pipeline's first 2D Renderer.
    /// </summary>
    [SerializeField] private ScriptableRendererData renderer2D;

    private static readonly int BackdropTex = Shader.PropertyToID("_BackdropTex");

    private Camera _camera;
    private RenderTexture _texture;
    private Material _material;
    private GameObject _screen;
    private MeshRenderer _screenRenderer;
    private Mesh _mesh;
    private bool _want;
    private float _scale = 1f;

    /// <summary>True while the 2D camera draws the hall (false: the office camera draws the layer unlit).</summary>
    public bool Active => _camera != null;

    /// <summary>The current lit painting, for the art's foreground seam colour match.</summary>
    public RenderTexture RenderedTexture => _texture;

    /// <summary>The layer the painted hall lives on (OfficeLayers.HallBackdrop; -1 when the project lacks it).</summary>
    public int Layer => OfficeLayers.HallBackdropLayer;

    /// <summary>Turns the 2D pass on or off (HallLightingRig, from its knobs), its texture at <paramref name="resolution"/> of the screen.</summary>
    public void SetOn(bool on, float resolution)
    {
        _scale = Mathf.Clamp(resolution, 0.25f, 1f);
        if (on == _want)
            return;
        _want = on;
        if (on)
            Build();
        else
            Teardown();
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginContextRendering += OnBeginContext;
        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginContextRendering -= OnBeginContext;
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        _want = false;
        Teardown();
    }

    /// <summary>Makes the 2D camera and the full-screen draw; without a 2D Renderer, a composite shader or the layer, lets the office camera draw the layer.</summary>
    private void Build()
    {
        Teardown();
        int layer = Layer;
        int renderer = Renderer2DIndex(renderer2D);
        if (source == null || layer < 0)
            return;
        if (renderer < 0 || composite == null)
        {
            if (Application.isPlaying)
                Debug.LogWarning($"[HallBackdrop] {(renderer < 0 ? "The render pipeline has no 2D Renderer" : "No TimeDesk/HallBackdrop shader")}: the hall draws unlit through the office camera. See docs/HALL_LIGHTING.md.", this);
            DrawLayerWithSource(true);
            return;
        }
        DrawLayerWithSource(false);

        var go = new GameObject("Hall backdrop camera (2D lights, made at runtime)") { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(transform, false);
        _camera = go.AddComponent<Camera>();
        _camera.cullingMask = 1 << layer;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.allowHDR = false;
        _camera.allowMSAA = false;
        _camera.useOcclusionCulling = false;
        UniversalAdditionalCameraData data = _camera.GetUniversalAdditionalCameraData();
        data.SetRenderer(renderer);
        data.renderPostProcessing = false;
        data.antialiasing = AntialiasingMode.None;
        data.requiresColorOption = CameraOverrideOption.Off;
        data.requiresDepthOption = CameraOverrideOption.Off;
        data.renderShadows = true;

        _material = new Material(composite) { name = "HallBackdrop (runtime)", hideFlags = HideFlags.DontSave };
        _mesh = new Mesh { name = "HallBackdrop triangle", hideFlags = HideFlags.DontSave };
        _mesh.SetVertices(new[] { new Vector3(-1f, -1f, 0f), new Vector3(3f, -1f, 0f), new Vector3(-1f, 3f, 0f) });
        _mesh.SetTriangles(new[] { 0, 1, 2 }, 0);
        _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);

        _screen = new GameObject("Hall backdrop (the lit hall, made at runtime)") { hideFlags = HideFlags.DontSave, layer = source.gameObject.layer };
        _screen.transform.SetParent(source.transform, false);
        _screen.AddComponent<MeshFilter>().sharedMesh = _mesh;
        _screenRenderer = _screen.AddComponent<MeshRenderer>();
        _screenRenderer.sharedMaterial = _material;
        _screenRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _screenRenderer.receiveShadows = false;
        _screenRenderer.lightProbeUsage = LightProbeUsage.Off;
        _screenRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        _screenRenderer.forceRenderingOff = true;
        Sync();
    }

    /// <summary>Destroys what Build made and gives the layer back to the office camera's culling as it was.</summary>
    private void Teardown()
    {
        Kill(_screen);
        Kill(_camera != null ? _camera.gameObject : null);
        Kill(_material);
        Kill(_mesh);
        if (_texture != null)
        {
            _texture.Release();
            Kill(_texture);
        }
        _screen = null;
        _screenRenderer = null;
        _camera = null;
        _material = null;
        _mesh = null;
        _texture = null;
        DrawLayerWithSource(!_want);
    }

    /// <summary>Adds the layer to the office camera's culling (<paramref name="draw"/>: the painted hall drawn unlit, the lighting off) or takes it out (the 2D camera draws it; the office camera must never draw it over its own full-screen draw).</summary>
    private void DrawLayerWithSource(bool draw)
    {
        int layer = Layer;
        if (source == null || layer < 0)
            return;
        if (draw)
            source.cullingMask |= 1 << layer;
        else
            source.cullingMask &= ~(1 << layer);
    }

    /// <summary>Before any camera renders this frame: the 2D camera takes the office camera's pose, lens and screen.</summary>
    private void OnBeginContext(ScriptableRenderContext context, List<Camera> cameras)
    {
        if (_camera != null)
            Sync();
    }

    /// <summary>The full-screen draw shows only in the office camera (not the scene view, not the desktop's cameras).</summary>
    private void OnBeginCamera(ScriptableRenderContext context, Camera camera)
    {
        if (_screenRenderer != null)
            _screenRenderer.forceRenderingOff = camera != source;
    }

    private void Sync()
    {
        if (source == null)
            return;
        Transform from = source.transform, to = _camera.transform;
        to.SetPositionAndRotation(from.position, from.rotation);
        _camera.fieldOfView = source.fieldOfView;
        _camera.nearClipPlane = source.nearClipPlane;
        _camera.farClipPlane = source.farClipPlane;
        _camera.orthographic = source.orthographic;
        _camera.orthographicSize = source.orthographicSize;
        _camera.backgroundColor = source.backgroundColor;
        _camera.depth = source.depth - 2f;

        int width = Mathf.Max(16, Mathf.RoundToInt(source.pixelWidth * _scale));
        int height = Mathf.Max(16, Mathf.RoundToInt(source.pixelHeight * _scale));
        if (_texture == null || _texture.width != width || _texture.height != height)
        {
            if (_texture != null)
            {
                _camera.targetTexture = null;
                _texture.Release();
                Kill(_texture);
            }
            _texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                name = "HallBackdrop (runtime)",
                hideFlags = HideFlags.DontSave,
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear,
            };
            _texture.Create();
            _camera.targetTexture = _texture;
            _material.SetTexture(BackdropTex, _texture);
        }
        _camera.aspect = source.aspect;
    }

    /// <summary>The index of <paramref name="wanted"/> in the pipeline's renderer list, else of its first 2D Renderer, or -1.</summary>
    private static int Renderer2DIndex(ScriptableRendererData wanted)
    {
        if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp))
            return -1;
        System.ReadOnlySpan<ScriptableRendererData> list = urp.rendererDataList;
        int first = -1;
        for (int i = 0; i < list.Length; i++)
        {
            if (wanted != null && ReferenceEquals(list[i], wanted))
                return i;
            if (first < 0 && list[i] is Renderer2DData)
                first = i;
        }
        return first;
    }

    private static void Kill(Object o)
    {
        if (o == null)
            return;
        if (Application.isPlaying)
            Destroy(o);
        else
            DestroyImmediate(o);
    }
}
