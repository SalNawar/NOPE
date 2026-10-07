using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Looking left at the city (the desk-first redesign, Saleh 2026-10-05, item
/// 6: "player can look left for a view at the city"; 2026-10-07: "hook the
/// city background to transition smoothly when the player presses left, you
/// should be able to see the FULL picture" and "there should be a fade away
/// maybe, because the city is not actually visible from the hall"): A or the
/// left arrow (OfficeControls: Look), or the "◀ City" button at the office's
/// left edge, turns the hall toward its window wall (the art's own left pan,
/// AnimeHallPresentation.SetPan, over DeskConfigSO.citySeconds, eased) and,
/// from DeskConfigSO.cityFadeFrom of that turn, fades in (cityFadeSeconds)
/// the whole city panorama over the screen: the art's living city (the hall
/// windows' "NOPE/Hall Living City" material, its eight time and weather
/// paintings, depth map and moving atmosphere) drawn unmasked, fitted
/// whole inside the screen on the cityMatte colour, at the hall's hour
/// (HallBakedCycle, the lights' clock) and its rain. The city lives (Saleh
/// 2026-10-07: "why is the city not animated and no parallax when you switch
/// to it? GPT made the assets for it"): it comes into view with a depth
/// parallax sweep (the shader's depth reprojection, _CityPan, from
/// DeskConfigSO.cityParallax to 0 over citySettleSeconds, the near roofs
/// sweeping further than the sky; the painting cropped by as much each side),
/// then follows the pointer a little (cityLookParallax); its sky, airship,
/// bus and headlights move at a pace that reads full screen
/// (cityAtmospherePace, cityHeadlightSize) and the hall window's flying
/// traffic (HallCityExterior.lanes, the same vehicles on the same lanes)
/// flies over it (cityTrafficPace, cityTrafficScale), lit by the hour. D, the right arrow, a
/// right-click, Esc (ControlRules) or the "Desk ▶" button at the right edge
/// runs the same timeline back (CityLookTimeline: the fade first, then the
/// turn). Reduced Motion cuts both ways and holds the city still.
/// BoothCoordinator says when it may turn (BoothRules.NormalViewLive: the
/// normal view, no newsletter, wheel or PC) and returns it when the next
/// traveller steps up or the PC, the wheel or the reading view take over.
/// OfficeSceneBinder binds it to the hall; an art office without the hall's
/// living city leaves it off (its buttons hidden).
/// </summary>
public sealed class CityView : MonoBehaviour
{
    /// <summary>The desk tuning (the city view's timeline and matte).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The full-screen city layer (the matte and the panorama; its alpha is the fade; takes the clicks while the city shows).</summary>
    [SerializeField] private CanvasGroup screen;

    /// <summary>The full-screen matte behind the panorama (DeskConfigSO.cityMatte).</summary>
    [SerializeField] private Image matte;

    /// <summary>The panorama, fitted whole inside the screen (its fitter takes the paintings' aspect at bind).</summary>
    [SerializeField] private RawImage panorama;

    /// <summary>Keeps the panorama's aspect, fitted inside the screen.</summary>
    [SerializeField] private AspectRatioFitter fitter;

    /// <summary>"◀ City" at the office's left edge (shown while the view may turn).</summary>
    [SerializeField] private Button lookButton;

    /// <summary>"Desk ▶" at the office's right edge (shown while looking at the city).</summary>
    [SerializeField] private Button backButton;

    private const string LivingCityShader = "NOPE/Hall Living City";
    private static readonly int MasksId = Shader.PropertyToID("_Masks");
    private static readonly int RegionId = Shader.PropertyToID("_Region");
    private static readonly int PanId = Shader.PropertyToID("_CityPan");
    private static readonly int RainId = Shader.PropertyToID("_CityRain");
    private static readonly int SecondsId = Shader.PropertyToID("_CitySeconds");
    private static readonly int MotionId = Shader.PropertyToID("_CityMotion");
    private static readonly int WeightsId = Shader.PropertyToID("_StateWeights");
    private static readonly int MorningId = Shader.PropertyToID("_MorningClear");
    private static readonly int DepthId = Shader.PropertyToID("_CityDepthStrength");
    private static readonly int PaceId = Shader.PropertyToID("_AtmospherePace");
    private static readonly int HeadlightId = Shader.PropertyToID("_HeadlightSize");

    /// <summary>Where the flying traffic sits in the city's depth (0 the sky, 1 the nearest roof): its share of the parallax sweep.</summary>
    private const float TrafficDepth = 0.5f;

    /// <summary>How fast the pointer's look follows it (per second).</summary>
    private const float LookFollow = 4f;

    /// <summary>One vehicle of the hall window's traffic drawn over the panorama.</summary>
    private struct Vehicle
    {
        public RectTransform Rect;
        public Image Image;
        public HallCityExterior.Lane Lane;
    }

    private readonly List<Vehicle> _traffic = new List<Vehicle>();
    private float _crop;
    private float _shown;
    private float _look;
    private Vector2 _canvas;

    private AnimeHallPresentation _hall;
    private HallCityExterior _exterior;
    private HallLightingRig _lights;
    private Material _material;
    private bool _live;
    private float _clock;
    private float _pan = -1f;

    /// <summary>True while the view turns (or has turned) toward the city.</summary>
    public bool IsOn { get; private set; }

    /// <summary>Raised after the view turns to the city or back.</summary>
    public event Action Changed;

    private void Awake()
    {
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
        Draw(0f);
    }

    private void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);
    }

    /// <summary>
    /// Binds the view to the anime hall (the office binder, once the art
    /// office is bound): its presentation (the left pan), its exterior (the
    /// living city's material, rain and animation switch) and its lights (the
    /// hour; null: noon). The panorama draws a copy of the windows' material
    /// with no aperture mask. Without a living city the view stays off.
    /// </summary>
    public void Bind(AnimeHallPresentation hall, HallCityExterior exterior, HallLightingRig lights)
    {
        _hall = hall;
        _exterior = exterior;
        _lights = lights;
        Material living = LivingMaterial(exterior);
        if (living == null || panorama == null)
            return;
        _material = new Material(living) { name = "CityView panorama (runtime)", hideFlags = HideFlags.DontSave };
        _material.SetTexture(MasksId, Texture2D.whiteTexture);
        _material.SetFloat(RegionId, 1f);
        _material.SetFloat(PanId, 0f);
        _material.SetFloat(DepthId, exterior.depthStrength);
        if (config != null)
        {
            _material.SetFloat(PaceId, config.cityAtmospherePace);
            _material.SetFloat(HeadlightId, config.cityHeadlightSize);
            _crop = Mathf.Clamp(config.cityParallax + config.cityLookParallax, 0f, 0.45f);
        }
        panorama.material = _material;
        panorama.texture = Texture2D.whiteTexture;
        panorama.uvRect = new Rect(_crop, 0f, 1f - 2f * _crop, 1f);
        Texture painting = _material.GetTexture(MorningId);
        if (fitter != null && painting != null && painting.height > 0)
            fitter.aspectRatio = painting.width * (1f - 2f * _crop) / painting.height;
        if (matte != null && config != null)
            matte.color = config.cityMatte;
        BuildTraffic(exterior);
    }

    /// <summary>The hall window's flying traffic over the panorama: one image per lane's vehicle (its sprite, untinted but by the hour), placed each frame (DrawTraffic).</summary>
    private void BuildTraffic(HallCityExterior exterior)
    {
        foreach (Vehicle v in _traffic)
            if (v.Rect != null)
                Destroy(v.Rect.gameObject);
        _traffic.Clear();
        Sprite canvas = exterior.window != null ? exterior.window.sprite : null;
        if (canvas == null || exterior.lanes == null)
            return;
        _canvas = canvas.rect.size;
        foreach (HallCityExterior.Lane lane in exterior.lanes)
        {
            if (lane == null || lane.vehicle == null || lane.vehicle.sprite == null)
                continue;
            var go = new GameObject("Traffic " + lane.vehicle.name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panorama.transform, false);
            go.layer = panorama.gameObject.layer;
            var image = go.GetComponent<Image>();
            image.sprite = lane.vehicle.sprite;
            image.raycastTarget = false;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            _traffic.Add(new Vehicle { Rect = rect, Image = image, Lane = lane });
        }
    }

    /// <summary>Lets the view turn to the city (BoothCoordinator: BoothRules.NormalViewLive); false returns it.</summary>
    public void SetLive(bool live)
    {
        live &= _material != null;
        _live = live;
        if (!live)
            Return();
        Show();
    }

    /// <summary>Turns to the city (no-op while it may not turn or looks there already): A, the left arrow, "◀ City".</summary>
    public void Look() => Set(true);

    /// <summary>Turns back to the desk (no-op when not looking at the city): D, the right arrow, a right-click, Esc, "Desk ▶".</summary>
    public void Return() => Set(false);

    private void Set(bool on)
    {
        if (on == IsOn || (on && (_material == null || !_live)))
            return;
        IsOn = on;
        Show();
        Changed?.Invoke();
    }

    /// <summary>Advances the timeline (unscaled time) and draws it: the hall's pan, the panorama's fade, and the living city's hour, rain and motion while it shows.</summary>
    private void Update()
    {
        if (config == null)
            return;
        float total = CityLookTimeline.Total(config.citySeconds, config.cityFadeFrom, config.cityFadeSeconds);
        _clock = CityLookTimeline.Step(_clock, IsOn, Time.unscaledDeltaTime, total, MotionPreference.Reduced);
        Draw(_clock);
    }

    private void Draw(float clock)
    {
        float fade = config != null ? CityLookTimeline.Fade(clock, config.citySeconds, config.cityFadeFrom, config.cityFadeSeconds) : 0f;
        if (screen != null)
        {
            screen.alpha = fade;
            screen.blocksRaycasts = IsOn;
            bool shown = fade > 0f;
            if (screen.gameObject.activeSelf != shown)
                screen.gameObject.SetActive(shown);
        }
        if (_hall != null && config != null)
        {
            float pan = CityLookTimeline.Pan(clock, config.citySeconds);
            if (clock <= 0f)
                pan = 0f;
            if (pan != _pan) // exact: the ends (0 and 1) must land exactly
            {
                _pan = pan;
                _hall.SetPan(pan);
            }
        }
        if (_material == null || fade <= 0f)
        {
            _shown = 0f;
            return;
        }
        bool moving = !MotionPreference.Reduced && (_exterior == null || _exterior.animateCity);
        HallBakedCycle.Weights4 w = HallBakedCycle.Weights(_lights != null ? _lights.Hour : 12f);
        _material.SetVector(WeightsId, new Vector4(w.x, w.y, w.z, w.w));
        _material.SetFloat(RainId, _exterior != null ? _exterior.rain : 0f);
        _material.SetFloat(SecondsId, Time.time);
        _material.SetFloat(MotionId, moving ? 1f : 0f);

        // The depth parallax: the sweep as it comes into view, then the pointer's look.
        float dt = Time.unscaledDeltaTime;
        _shown += IsOn ? dt : 0f;
        float cityPan = 0f;
        if (moving && config != null)
        {
            Mouse mouse = Mouse.current;
            float x = mouse != null && UnityEngine.Screen.width > 0 ? Mathf.Clamp(mouse.position.ReadValue().x / UnityEngine.Screen.width * 2f - 1f, -1f, 1f) : 0f;
            _look = Mathf.Lerp(_look, x * config.cityLookParallax, 1f - Mathf.Exp(-dt * LookFollow));
            cityPan = config.cityParallax * CityLookTimeline.Sweep(_shown, config.citySettleSeconds) + _look;
        }
        _material.SetFloat(PanId, cityPan);
        DrawTraffic(cityPan, moving, w);
    }

    /// <summary>The traffic at its place on its lane (CityLookTimeline.Travel at cityTrafficPace), in the panorama's crop and its share of the parallax, sized cityTrafficScale times the hall window's, lit by the hour as the city is (the shader's light).</summary>
    private void DrawTraffic(float pan, bool moving, HallBakedCycle.Weights4 w)
    {
        if (_traffic.Count == 0 || _canvas.x <= 0f || _canvas.y <= 0f || config == null)
            return;
        Rect area = panorama.rectTransform.rect;
        float span = 1f - 2f * _crop;
        float shift = pan * (0.3f + 0.7f * (_exterior != null ? _exterior.depthStrength : 0f) * TrafficDepth);
        float total = w.x + w.y + w.z + w.w;
        Color light = total > 0f
            ? (new Color(0.95f, 0.97f, 1f) * w.x + Color.white * w.y + new Color(0.75f, 0.55f, 0.55f) * w.z + new Color(0.22f, 0.29f, 0.43f) * w.w) / total
            : Color.white;
        light.a = 1f;
        float seconds = moving ? Time.time * config.cityTrafficPace : 0f;
        foreach (Vehicle v in _traffic)
        {
            HallCityExterior.Lane lane = v.Lane;
            float along = CityLookTimeline.Travel(seconds, lane.phase, lane.speed, Mathf.Abs(lane.xEnd - lane.xStart));
            float u = Mathf.Lerp(lane.xStart, lane.xEnd, along) / _canvas.x;
            float x = (u - shift - _crop) / span, y = 1f - lane.yPixels / _canvas.y;
            Vector2 sprite = v.Image.sprite.rect.size;
            float width = lane.widthPixels / _canvas.x / span * area.width * config.cityTrafficScale;
            v.Rect.anchoredPosition = new Vector2(x * area.width, y * area.height);
            v.Rect.sizeDelta = new Vector2(width, sprite.x > 0f ? width * sprite.y / sprite.x : width);
            v.Rect.localScale = new Vector3(lane.xEnd < lane.xStart ? -1f : 1f, 1f, 1f);
            v.Image.color = light;
        }
    }

    /// <summary>The edge buttons: "◀ City" while the view may turn, "Desk ▶" while it looks at the city.</summary>
    private void Show()
    {
        if (lookButton != null && lookButton.gameObject.activeSelf != (_live && !IsOn))
            lookButton.gameObject.SetActive(_live && !IsOn);
        if (backButton != null && backButton.gameObject.activeSelf != IsOn)
            backButton.gameObject.SetActive(IsOn);
    }

    /// <summary>The hall windows' living city material (the first of the exterior's panels drawing it), or null.</summary>
    private static Material LivingMaterial(HallCityExterior exterior)
    {
        if (exterior == null || exterior.panelRenderers == null)
            return null;
        foreach (SpriteRenderer panel in exterior.panelRenderers)
            if (panel != null && panel.sharedMaterial != null && panel.sharedMaterial.shader.name == LivingCityShader)
                return panel.sharedMaterial;
        return null;
    }
}
