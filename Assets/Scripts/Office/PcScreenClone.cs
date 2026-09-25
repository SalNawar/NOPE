using UnityEngine;

/// <summary>
/// The office PC's screen shows the desktop live: the clone camera renders the
/// desktop's layer into a render texture, and a planar-projection material
/// (TimeDesk/PlanarScreen) puts it on the PC's glass, fitted at the desktop's
/// 4:3 inside the glass (the glass's own UVs are not used, so any CRT works).
/// While the screen is off, the glass shows the art's own material again. The
/// office binder hands it the glass renderer (the contract's PCScreen anchor).
/// </summary>
public sealed class PcScreenClone : MonoBehaviour
{
    /// <summary>The camera that renders the desktop's layer into the render texture.</summary>
    [SerializeField] private Camera cloneCamera;

    /// <summary>The TimeDesk/PlanarScreen material (instanced for the bound glass).</summary>
    [SerializeField] private Material cloneMaterial;

    /// <summary>The desk tuning (the texture's size, the fill and the brightness).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The desktop's aspect (the canvas is 1440 x 1080).</summary>
    private const float DesktopAspect = 4f / 3f;

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int AxisUId = Shader.PropertyToID("_AxisU");
    private static readonly int AxisVId = Shader.PropertyToID("_AxisV");
    private static readonly int RectId = Shader.PropertyToID("_Rect");
    private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");

    private Renderer _glass;
    private Material[] _artMaterials;
    private Material[] _cloneMaterials;
    private Material _instance;
    private RenderTexture _texture;
    private bool _on = true;

    /// <summary>True once a glass is bound.</summary>
    public bool IsBound => _glass != null;

    /// <summary>The bound glass as the office camera sees it (valid once bound).</summary>
    public GlassFrame Frame { get; private set; }

    /// <summary>
    /// Puts the clone on submesh <paramref name="submesh"/> of <paramref name="glass"/>:
    /// the projection runs across the glass's two widest local axes, turned so
    /// the picture reads upright from <paramref name="viewer"/> (the office
    /// camera; GlassFrame). A glass without a mesh filter is ignored with a warning.
    /// </summary>
    public void Bind(Renderer glass, int submesh, Vector3 viewer)
    {
        MeshFilter filter = glass != null ? glass.GetComponent<MeshFilter>() : null;
        if (filter == null || filter.sharedMesh == null || cloneMaterial == null || cloneCamera == null)
        {
            Debug.LogWarning("[PcScreenClone] No glass mesh, clone material or clone camera: the office PC shows its own glass. Check the PCScreen anchor and run Tools > TimeDesk > Build Office UI.", this);
            return;
        }

        Mesh mesh = filter.sharedMesh;
        submesh = Mathf.Clamp(submesh, 0, mesh.subMeshCount - 1);
        Bounds local = mesh.GetSubMesh(submesh).bounds;
        GlassFrame frame = GlassFrame.Measure(glass.transform, local, viewer);
        Frame = frame;
        (float fitU, float fitV) = ScreenMapping.Fit(frame.Size.x, frame.Size.y, DesktopAspect);
        float fill = config != null ? config.cloneFill : 1f;

        Vector2Int resolution = config != null ? config.cloneResolution : new Vector2Int(1024, 768);
        _texture = new RenderTexture(Mathf.Max(resolution.x, 16), Mathf.Max(resolution.y, 16), 16, RenderTextureFormat.ARGB32)
        {
            name = "PcScreenClone",
            useMipMap = true,
            autoGenerateMips = true,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 4,
        };
        cloneCamera.targetTexture = _texture;

        _instance = new Material(cloneMaterial) { name = cloneMaterial.name + " (PC)" };
        _instance.SetTexture(BaseMapId, _texture);
        _instance.SetVector(AxisUId, frame.AxisU);
        _instance.SetVector(AxisVId, frame.AxisV);
        _instance.SetVector(RectId, new Vector4(Vector3.Dot(frame.LocalCentre, frame.AxisU), Vector3.Dot(frame.LocalCentre, frame.AxisV),
                                                frame.Size.x * fitU * fill, frame.Size.y * fitV * fill));
        _instance.SetFloat(BrightnessId, config != null ? config.cloneBrightness : 1f);

        _glass = glass;
        _artMaterials = glass.sharedMaterials;
        _cloneMaterials = (Material[])_artMaterials.Clone();
        if (submesh < _cloneMaterials.Length)
            _cloneMaterials[submesh] = _instance;
        Apply();
    }

    /// <summary>Shows the desktop on the glass (screen on) or the art's glass (screen off).</summary>
    public void SetOn(bool on)
    {
        _on = on;
        Apply();
    }

    private void Apply()
    {
        if (cloneCamera != null)
            cloneCamera.enabled = _on && _glass != null;
        if (_glass != null)
            _glass.sharedMaterials = _on ? _cloneMaterials : _artMaterials;
    }

    /// <summary>Gives the art its glass back and frees the texture and the material.</summary>
    private void OnDestroy()
    {
        if (_glass != null)
            _glass.sharedMaterials = _artMaterials;
        if (cloneCamera != null)
            cloneCamera.targetTexture = null;
        if (_texture != null)
        {
            _texture.Release();
            Destroy(_texture);
        }
        if (_instance != null)
            Destroy(_instance);
    }
}
