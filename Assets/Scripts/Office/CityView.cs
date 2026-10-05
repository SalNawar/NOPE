using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Looking left at the city (the desk-first redesign, Saleh 2026-10-05, item
/// 6: "player can look left for a view at the city"): A or the left arrow
/// (OfficeControls: Look), or the "◀ City" button at the office's left edge,
/// turns the view left (DeskConfigSO.cityYaw) to a window view over the 2150
/// city; D, the right arrow, a right-click, Esc (ControlRules) or the "Desk ▶"
/// button at the right edge turns back (Return). The
/// view is a gameplay-owned Cinemachine camera posed at bind from the art
/// office's camera (its place kept, its yaw turned, its pitch the knob's),
/// raised above the art camera's priority while on, with its own eased blend
/// (a cut under Reduced Motion), like the desk view. The art side's painting
/// (ArtSlots.CityView, a by-name slot) fills the view when it exists, tinted
/// toward the evening; until then (docs/ART_ASSET_LIST.md, "City view"), a code-drawn stand-in
/// stands out there: a sky and three layers of towers at growing distances
/// (CitySkyline), unlit, tinted from the day's palette to the evening's
/// along the hall's evening curve (CrowdPaletteBlend, the shift clock), the
/// towers' windows lighting up as the evening comes. BoothCoordinator says
/// when it may turn (BoothRules.NormalViewLive: the normal view, no
/// newsletter, wheel or PC) and returns it when the next traveller steps up
/// or the PC, the wheel or the reading view take over.
/// </summary>
public sealed class CityView : MonoBehaviour
{
    /// <summary>The desk tuning (the city view's knobs and the hall's evening curve).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The city view's camera (inactive until bound; priority 0 while the view is off).</summary>
    [SerializeField] private CinemachineCamera cityCamera;

    /// <summary>The stand-in city's root (its layers are made at bind, facing the city camera).</summary>
    [SerializeField] private Transform skyline;

    /// <summary>The unlit, transparent material the stand-in's layers share (tinted per layer by a property block).</summary>
    [SerializeField] private Material layerMaterial;

    /// <summary>"◀ City" at the office's left edge (shown while the view may turn).</summary>
    [SerializeField] private Button lookButton;

    /// <summary>"Desk ▶" at the office's right edge (shown while looking at the city).</summary>
    [SerializeField] private Button backButton;

    /// <summary>The towers' layers, far to near: their texture sizes, seeds and height ranges.</summary>
    private static readonly (int seed, float min, float max, float distance)[] Layers =
    {
        (2150, 0.35f, 0.85f, 1.8f), (2151, 0.25f, 0.65f, 1.35f), (2152, 0.15f, 0.45f, 1f)
    };

    private const int LayerPixelsWide = 512, LayerPixelsHigh = 128;
    private const int PriorityAboveOffice = 2;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    private CinemachineCamera _office;
    private int _onPriority;
    private bool _live;
    private float _lastEvening = -1f;
    private Renderer _sky;
    private Renderer _frame;
    private readonly Renderer[] _bodies = new Renderer[Layers.Length];
    private readonly Renderer[] _windows = new Renderer[Layers.Length];
    private MaterialPropertyBlock _block;
    private float _hideAt = -1f;
    private CinemachineCore.GetBlendOverrideDelegate _blend;
    private CinemachineCore.GetBlendOverrideDelegate _previous;

    /// <summary>True while the camera is (or is blending) toward the city.</summary>
    public bool IsOn { get; private set; }

    /// <summary>Raised after the view turns to the city or back.</summary>
    public event Action Changed;

    private void Awake()
    {
        _blend = Blend;
        if (lookButton != null)
        {
            lookButton.onClick.AddListener(Look);
            lookButton.gameObject.SetActive(false);
        }
        if (backButton != null)
        {
            backButton.onClick.AddListener(Return);
            backButton.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (CinemachineCore.GetBlendOverride == _blend)
            CinemachineCore.GetBlendOverride = _previous;
    }

    /// <summary>
    /// Poses the city camera from the art office's camera <paramref name="office"/>
    /// (its place, its yaw turned left by the knob, pitched by the knob, the
    /// lens copied) and stands the stand-in city out along that view (the
    /// office binder, once the art office is bound). The camera then waits at
    /// priority 0.
    /// </summary>
    public void Bind(CinemachineCamera office)
    {
        if (office == null || cityCamera == null || config == null)
            return;
        _office = office;
        Transform art = office.transform;
        Vector3 level = Vector3.ProjectOnPlane(art.forward, Vector3.up);
        if (level.sqrMagnitude < 1e-6f)
            level = Vector3.forward;
        Quaternion look = Quaternion.LookRotation(level.normalized, Vector3.up) * Quaternion.Euler(0f, -config.cityYaw, 0f) * Quaternion.Euler(config.cityPitch, 0f, 0f);
        cityCamera.transform.SetPositionAndRotation(art.position, look);
        cityCamera.Lens = office.Lens;
        _onPriority = office.Priority.Value + PriorityAboveOffice;
        cityCamera.Priority = IsOn ? _onPriority : 0;
        cityCamera.gameObject.SetActive(true);
        BuildCity(art.position, look, office.Lens.FieldOfView);
    }

    /// <summary>Lets the view turn to the city (BoothCoordinator: BoothRules.NormalViewLive); false returns it.</summary>
    public void SetLive(bool live)
    {
        live &= _office != null;
        _live = live;
        if (!live)
            Return();
        Show();
    }

    /// <summary>Turns to the city (no-op while it may not turn or looks there already): A, the left arrow, "◀ City".</summary>
    public void Look() => Set(true);

    /// <summary>Turns back to the desk (no-op when not looking at the city): D, the right arrow, a right-click, Esc, "Desk ▶".</summary>
    public void Return() => Set(false);

    /// <summary>The stand-in's light, and the stand-in hidden once the turn back ends.</summary>
    private void Update()
    {
        Light();
        if (_hideAt >= 0f && Time.unscaledTime >= _hideAt && skyline != null)
        {
            skyline.gameObject.SetActive(false);
            _hideAt = -1f;
        }
    }

    private void Set(bool on)
    {
        if (on == IsOn || (on && (_office == null || !_live)))
            return;
        IsOn = on;
        if (CinemachineCore.GetBlendOverride != _blend)
        {
            _previous = CinemachineCore.GetBlendOverride;
            CinemachineCore.GetBlendOverride = _blend;
        }
        cityCamera.Priority = on ? _onPriority : 0;
        // The city stands out there only while it is looked at (and through the turn back), so it never shows behind the hall's art from the desk.
        if (skyline != null && on)
            skyline.gameObject.SetActive(true);
        _hideAt = on ? -1f : Time.unscaledTime + (config != null ? config.citySeconds : 0f) + 0.1f;
        Show();
        Changed?.Invoke();
    }

    /// <summary>The edge buttons: "◀ City" while the view may turn, "Desk ▶" while it looks at the city.</summary>
    private void Show()
    {
        if (lookButton != null && lookButton.gameObject.activeSelf != (_live && !IsOn))
            lookButton.gameObject.SetActive(_live && !IsOn);
        if (backButton != null && backButton.gameObject.activeSelf != IsOn)
            backButton.gameObject.SetActive(IsOn);
    }

    /// <summary>The blend to or from the city camera: the knob's seconds, eased (a cut under Reduced Motion); any other blend is left to the hook before ours.</summary>
    private CinemachineBlendDefinition Blend(ICinemachineCamera from, ICinemachineCamera to, CinemachineBlendDefinition fallback, UnityEngine.Object owner)
    {
        if (cityCamera == null || (!ReferenceEquals(from, cityCamera) && !ReferenceEquals(to, cityCamera)))
            return _previous != null ? _previous(from, to, fallback, owner) : fallback;
        float seconds = MotionPreference.Reduced || config == null ? 0f : config.citySeconds;
        return seconds > 0f
            ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, seconds)
            : new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
    }

    /// <summary>
    /// The stand-in city out along the city view: the sky furthest, then the
    /// three layers of towers, each a quad facing the camera, sized to fill
    /// the view's height and a wide margin of its width at its distance
    /// (DeskConfigSO.cityDistance times the layer's), its bottom a little
    /// below the view's lower edge. Made once.
    /// </summary>
    private void BuildCity(Vector3 eye, Quaternion look, float fieldOfView)
    {
        if (skyline == null || layerMaterial == null || _sky != null)
            return;
        skyline.SetPositionAndRotation(eye, look);
        float tan = Mathf.Tan(Mathf.Max(10f, fieldOfView) * 0.5f * Mathf.Deg2Rad);

        Renderer Quad(string name, float distance, float heightShare, float bottomShare, Texture2D texture)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(skyline, false);
            float viewHeight = 2f * distance * tan;
            float height = viewHeight * heightShare;
            float width = viewHeight * 2.4f;
            float bottom = -viewHeight / 2f + viewHeight * bottomShare;
            go.transform.localPosition = new Vector3(0f, bottom + height / 2f, distance);
            go.transform.localScale = new Vector3(width, height, 1f);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = layerMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _block ??= new MaterialPropertyBlock();
            r.GetPropertyBlock(_block);
            _block.SetTexture(BaseMapId, texture);
            r.SetPropertyBlock(_block);
            return r;
        }

        float d = config.cityDistance;
        Texture2D painting = SlotArt.Texture(new[] { ArtSlots.CityView });
        if (painting != null)
        {
            // The art side's painting (ArtSlots.CityView) replaces the stand-in: one picture filling the view, tinted toward the evening.
            _sky = Quad("Painting", d * 1.5f, 1.15f, -0.075f, painting);
            float aspect = painting.height > 0 ? (float)painting.width / painting.height : 3.2f;
            Vector3 scale = _sky.transform.localScale;
            _sky.transform.localScale = new Vector3(scale.y * aspect, scale.y, 1f);
            _lastEvening = -1f;
            Light();
            skyline.gameObject.SetActive(IsOn);
            return;
        }
        _sky = Quad("Sky", d * 2.4f, 1.3f, -0.15f, SkyTexture());
        for (int i = 0; i < Layers.Length; i++)
        {
            CitySkyline.Layer layer = CitySkyline.Paint(LayerPixelsWide, LayerPixelsHigh, Layers[i].seed, Layers[i].min, Layers[i].max, 0.35f);
            float distance = d * Layers[i].distance;
            _bodies[i] = Quad("Towers" + (i + 1), distance, 0.75f, -0.05f, Mask(layer.Bodies, layer.Width, layer.Height, "Towers" + (i + 1)));
            _windows[i] = Quad("Windows" + (i + 1), distance * 0.999f, 0.75f, -0.05f, Mask(layer.Windows, layer.Width, layer.Height, "Windows" + (i + 1)));
        }
        _frame = Quad("WindowFrame", d * 0.12f, 1.4f, -0.2f, FrameTexture());
        _lastEvening = -1f;
        Light();
        skyline.gameObject.SetActive(IsOn);
    }

    /// <summary>The window's frame in front of the stand-in city: mullions, a transom and a sill (white; tinted the hall's dark frame colour).</summary>
    private static Texture2D FrameTexture()
    {
        const int w = 256, h = 128;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "CityWindowFrame", wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool mullion = Mathf.Abs(x % 85 - 0) < 3 || x >= w - 3;
                bool transom = Mathf.Abs(y - (int)(h * 0.78f)) < 2;
                bool sill = y < (int)(h * 0.1f);
                pixels[y * w + x] = new Color32(255, 255, 255, (byte)(mullion || transom || sill ? 255 : 0));
            }
        texture.SetPixels32(pixels);
        texture.Apply(true, true);
        return texture;
    }

    /// <summary>Tints the stand-in by the time of day: the sky and the towers from the day's palette to the evening's along the hall's evening curve, far layers paler, the windows lit as the evening comes (faint by day).</summary>
    private void Light()
    {
        if (_sky == null || config == null)
            return;
        IShiftProgress clock = ShiftClockDriver.Live;
        float evening = CrowdPaletteBlend.Evening(clock != null ? clock.Progress01 : 0f, config.hallEveningStartsAt, config.hallEveningFullAt);
        if (Mathf.Abs(evening - _lastEvening) < 0.002f)
            return;
        _lastEvening = evening;
        if (_bodies[0] == null)
        {
            Tint(_sky, Color.Lerp(Color.white, config.citySkyEvening, evening * 0.6f));
            return;
        }
        Tint(_sky, Color.Lerp(config.citySkyDay, config.citySkyEvening, evening));
        for (int i = 0; i < Layers.Length; i++)
        {
            float near = Layers.Length > 1 ? i / (float)(Layers.Length - 1) : 1f;
            Color day = Color.Lerp(config.cityFarDay, config.cityNearDay, near);
            Color dusk = Color.Lerp(config.cityFarEvening, config.cityNearEvening, near);
            Tint(_bodies[i], Color.Lerp(day, dusk, evening));
            Color window = config.cityWindowLight;
            window.a = Mathf.Lerp(0.12f, 1f, evening);
            Tint(_windows[i], window);
        }
        Tint(_frame, Color.Lerp(config.cityNearDay * 0.55f, config.cityNearEvening * 0.7f, evening) + new Color(0f, 0f, 0f, 1f));
    }

    private void Tint(Renderer r, Color colour)
    {
        if (r == null)
            return;
        r.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, colour);
        r.SetPropertyBlock(_block);
    }

    /// <summary>A white texture carrying a mask as its alpha (the layer's tint colours it).</summary>
    private static Texture2D Mask(byte[] alpha, int width, int height, string name)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = new Color32[alpha.Length];
        for (int i = 0; i < alpha.Length; i++)
            pixels[i] = new Color32(255, 255, 255, alpha[i]);
        texture.SetPixels32(pixels);
        texture.Apply(true, true);
        return texture;
    }

    /// <summary>The sky's gradient: lighter toward the horizon, white (the tint colours it).</summary>
    private static Texture2D SkyTexture()
    {
        const int h = 64;
        var texture = new Texture2D(1, h, TextureFormat.RGBA32, false) { name = "CitySky", wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < h; y++)
        {
            float shade = Mathf.Lerp(1f, 0.82f, y / (float)(h - 1));
            texture.SetPixel(0, y, new Color(shade, shade, shade, 1f));
        }
        texture.Apply(false, true);
        return texture;
    }
}
