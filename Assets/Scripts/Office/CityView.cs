using System;
using UnityEngine;
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
/// (HallBakedCycle, the lights' clock) and its rain. D, the right arrow, a
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
        panorama.material = _material;
        panorama.texture = Texture2D.whiteTexture;
        Texture painting = _material.GetTexture(MorningId);
        if (fitter != null && painting != null && painting.height > 0)
            fitter.aspectRatio = (float)painting.width / painting.height;
        if (matte != null && config != null)
            matte.color = config.cityMatte;
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
            if (!Mathf.Approximately(pan, _pan))
            {
                _pan = pan;
                _hall.SetPan(pan);
            }
        }
        if (_material == null || fade <= 0f)
            return;
        HallBakedCycle.Weights4 w = HallBakedCycle.Weights(_lights != null ? _lights.Hour : 12f);
        _material.SetVector(WeightsId, new Vector4(w.x, w.y, w.z, w.w));
        _material.SetFloat(RainId, _exterior != null ? _exterior.rain : 0f);
        _material.SetFloat(SecondsId, Time.time);
        _material.SetFloat(MotionId, !MotionPreference.Reduced && (_exterior == null || _exterior.animateCity) ? 1f : 0f);
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
