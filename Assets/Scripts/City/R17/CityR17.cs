using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The r17 city, alive in the game (CITY-R17): the r17 look-dev (<c>E:\unity\NOPE-tools\city\live\r17.html</c>)
/// ported whole, as the city view's backdrop and the hall's windows.
/// <list type="bullet">
/// <item>The 11 parallax planes with the page's parallax, the 8 soft clouds at three depths (continuous drift, a
/// cross-dissolve between drawings, wraps hidden), the 3D traffic (the taxi, coupe, bus and truck meshes, toon shaded
/// with an ink hull, on the page's lanes with its car-following, banking and hidden spawns: <see cref="CityR17Traffic"/>,
/// instanced: <see cref="CityR17TrafficDraw"/>), the train on the viaduct, the 22 animated ad screens, the fire,
/// the smoke and the noon lamps, in the page's draw order (<see cref="CityR17Stage"/>).</item>
/// <item>Everything placed comes from the exported data (<c>Assets/Data/City/R17</c>); the art from
/// <c>Assets/Art/City/R17</c>; the switch and knobs from <see cref="CityR17ConfigSO"/> (Resources), so a new cut needs
/// no code.</item>
/// <item>Time: its own clock on unscaled time (each frame capped at 50 ms, as the page), never Time.time; the loops
/// on the page's 24 fps frames. Hidden (no city view, no window on screen), nothing steps or draws.</item>
/// <item>Comfort: no idle drift or dolly (sway stays off); the parallax is rate-capped (BUG-69); Reduced Motion
/// drops the parallax and holds the smoke and fire on one drawing, while the traffic, the train and the clouds keep
/// moving.</item>
/// <item>Budget: the CPU cost of a frame (sim and recording) is measured (<see cref="CpuMs"/>); the hall windows,
/// which only need a small view, redraw at <see cref="CityR17ConfigSO.windowFps"/> and halve that while over
/// <see cref="CityR17ConfigSO.budgetMs"/>.</item>
/// </list>
/// <see cref="Attach"/> returns null when the switch is off or the data fails, and the caller keeps its old path.
/// </summary>
[DisallowMultipleComponent]
public sealed class CityR17 : MonoBehaviour
{
    /// <summary>Where the config lives under Resources.</summary>
    public const string ConfigResource = "CityR17/CityR17Config";

    private static readonly int CityTexId = Shader.PropertyToID("_CityR17Tex");
    private static readonly int CityViewId = Shader.PropertyToID("_CityR17View");

    /// <summary>A config whose build failed is not retried (or logged again) by later binds.</summary>
    private static CityR17ConfigSO _failed;

    private CityR17ConfigSO _config;
    private CityR17Scene _scene;
    private CityR17Traffic _sim;
    private CityR17Stage _stage;
    private CityR17Look _look;
    private Material _present;
    private CityR17Windows _windows;
    private AnimeHallPresentation _hall;
    private CityR17Framing _framing;
    private int _screenW, _screenH;
    private bool _fill;
    private float _renderScale;
    private double _clock;
    private float _pending, _sinceRender, _windowFps;
    private Vector2 _pointer;
    private float _pointerIdle = 1e9f;
    private readonly System.Diagnostics.Stopwatch _watch = new System.Diagnostics.Stopwatch();

    /// <summary>True while the full-screen city view shows (the city view's fade is above 0).</summary>
    public bool ViewShown { get; set; }

    /// <summary>The rendered city (a linear-format texture holding gamma-space colour: show it through <see cref="PresentMaterial"/>).</summary>
    public RenderTexture Output => _stage?.Output;

    /// <summary>The material a UI image shows <see cref="Output"/> with (decodes it once).</summary>
    public Material PresentMaterial => _present;

    /// <summary>The output's aspect (width over height).</summary>
    public float Aspect => _framing.OutH > 0 ? (float)_framing.OutW / _framing.OutH : 21f / 9f;

    /// <summary>The CPU cost of the city's last frames (ms, smoothed): the sim and the recording.</summary>
    public float CpuMs { get; private set; }

    /// <summary>Instances of traffic drawn in the last rendered frame.</summary>
    public int VehiclesDrawn => _stage?.Traffic?.Drawn ?? 0;

    /// <summary>The sim (for checks and the debug readout).</summary>
    public CityR17Traffic Sim => _sim;

    /// <summary>True once built.</summary>
    public bool Ready => _stage != null;

    /// <summary>
    /// The r17 city on a host (the city view's object): built from the Resources config, or null when the switch is
    /// off, the config is missing, or its data fails (logged once; a city that failed is disabled and kept).
    /// </summary>
    public static CityR17 Attach(GameObject host)
    {
        if (host == null)
            return null;
        CityR17 existing = host.GetComponent<CityR17>();
        if (existing != null && existing.Ready)
            return existing;
        CityR17ConfigSO config = Resources.Load<CityR17ConfigSO>(ConfigResource);
        if (config == null || !config.live || config == _failed)
            return null;
        CityR17 city = existing != null ? existing : host.AddComponent<CityR17>();
        if (city.Build(config))
            return city;
        _failed = config;
        city.enabled = false;
        return null;
    }

    private bool Build(CityR17ConfigSO config)
    {
        try
        {
            _config = config;
            if (config.sceneJson == null || config.trafficJson == null || config.vehiclesJson == null || config.artJson == null)
                throw new InvalidOperationException("the config lacks a data file");
            _scene = CityR17Scene.Parse(config.sceneJson.text);
            CityR17TrafficData traffic = CityR17TrafficData.Parse(config.trafficJson.text);
            CityR17Models models = CityR17Models.Parse(config.vehiclesJson.text);
            CityR17Art art = CityR17Art.Parse(config.artJson.text);
            _look = CityR17Look.For(_scene);
            _look.RateCapPx = config.parallaxRateCap;
            _stage = new CityR17Stage(config, _scene, art);
            Refit();
            _sim = new CityR17Traffic(traffic, models) { PlateW = _scene.PlateW, PlateH = _scene.PlateH };
            _sim.Prefill();
            _sim.Settle(_framing.PxPerPlate);
            _stage.Traffic = new CityR17TrafficDraw(_sim, traffic, models, config.vehicleShader, config.hullPush, config.glint);
            if (config.presentShader == null)
                throw new InvalidOperationException("the config lacks the present shader");
            _present = new Material(config.presentShader) { name = "City R17 present", hideFlags = HideFlags.DontSave };
            _windowFps = Mathf.Max(1f, config.windowFps);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("City R17: the city could not be built, so the old city stays (" + e.Message + ").\n" + e);
            _stage?.Dispose();
            _stage = null;
            return false;
        }
    }

    /// <summary>The hall whose windows look out on the city and whose turn moves its camera (null: none).</summary>
    public void SetHall(HallCityExterior exterior, AnimeHallPresentation hall)
    {
        _hall = hall;
        _windows = Ready ? CityR17Windows.Install(exterior, _config) : null;
    }

    /// <summary>Re-reads the screen and the framing knobs; resizes the targets when they changed.</summary>
    private void Refit()
    {
        _screenW = Screen.width;
        _screenH = Screen.height;
        _fill = _config.fillScreen;
        _renderScale = _config.renderScale;
        _framing = CityR17Framing.For(_scene, _screenW, _screenH, _fill, _renderScale);
        _stage.Resize(_framing);
    }

    private void LateUpdate()
    {
        if (_stage == null)
            return;
        float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
        bool windows = _windows != null && _windows.Visible;
        if (!ViewShown && !windows)
            return;   // nothing shows the city: it holds
        _watch.Restart();
        if (Screen.width != _screenW || Screen.height != _screenH || _fill != _config.fillScreen || !Mathf.Approximately(_renderScale, _config.renderScale))
            Refit();
        _stage.Traffic.SetKnobs(_config.hullPush, _config.glint);
        _look.RateCapPx = _config.parallaxRateCap;
        _clock += dt;
        _pending += dt;
        bool reduced = MotionPreference.Reduced;

        // the look: the pointer in the city view, the hall's turn in the windows
        double tx = 0, ty = 0;
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 p = mouse.position.ReadValue();
            _pointerIdle = (p - _pointer).sqrMagnitude > 0.25f ? 0f : _pointerIdle + dt;
            _pointer = p;
        }
        if (ViewShown)
        {
            bool inside = _pointer.x >= 0 && _pointer.y >= 0 && _pointer.x <= Screen.width && _pointer.y <= Screen.height;
            if (mouse != null && inside && _pointerIdle < _config.idleSeconds && Screen.width > 0 && Screen.height > 0)
            {
                tx = (_pointer.x / Screen.width - 0.5) * 2;
                ty = (0.5 - _pointer.y / Screen.height) * 2;
            }
        }
        else if (_hall != null)
            tx = (_hall.lookLeft - 0.5) * 2 * _config.windowParallax;
        _look.Step(dt, tx, ty, _framing.PxPerPlate, reduced);

        // the hall windows only need a small view: redraw them at the window rate (the sim catches up then)
        if (!ViewShown)
        {
            _sinceRender += dt;
            if (_sinceRender < 1f / _windowFps)
            {
                Measure(false);
                return;
            }
            _sinceRender = 0f;
        }
        _sim.Update(_pending, _framing.PxPerPlate);
        _pending = 0f;
        double pan = _framing.MaxPan > 0 ? _look.X * _framing.MaxPan : 0;
        var state = new CityR17Stage.FrameState
        {
            Seconds = _clock,
            ShiftX = _look.ShiftX(1),
            ShiftY = _look.ShiftY(1),
            Pan = pan,
            Reduced = reduced,
        };
        _stage.Render(state);
        Shader.SetGlobalTexture(CityTexId, _stage.Output);
        Shader.SetGlobalVector(CityViewId, _stage.View);
        Measure(true);
    }

    /// <summary>Smooths the frame's CPU cost and, with only the hall windows up, halves their rate while over budget.</summary>
    private void Measure(bool rendered)
    {
        if (!rendered)
            return;   // a frame that only stepped the look costs next to nothing; the budget is the rendered frames'
        float ms = (float)_watch.Elapsed.TotalMilliseconds;
        CpuMs = CpuMs <= 0 ? ms : Mathf.Lerp(CpuMs, ms, 0.1f);
        if (ViewShown)
            return;
        float target = Mathf.Max(1f, _config.windowFps);
        if (CpuMs > _config.budgetMs)
            _windowFps = Mathf.Max(12f, _windowFps * 0.5f);
        else if (_windowFps < target)
            _windowFps = Mathf.Min(target, _windowFps + 1f);
    }

    private void OnDestroy()
    {
        if (_windows != null)
            Release(_windows);
        _stage?.Dispose();
        _stage = null;
        if (_present != null)
            Release(_present);
    }

    /// <summary>Destroys an object the city made (at once outside play mode, where Destroy is refused).</summary>
    public static void Release(UnityEngine.Object o)
    {
        if (o == null)
            return;
        if (Application.isPlaying)
            Destroy(o);
        else
            DestroyImmediate(o);
    }
}
