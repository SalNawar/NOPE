using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The anime hall's lights and dust, driven through a full day-night cycle
/// (docs/HALL_LIGHTING.md). It sits on the hall's HallLighting root (Tools >
/// TimeDesk > Add Anime Hall Hooks) with the HallBackdrop that draws the
/// painted layers through the URP 2D Renderer, so the Light2Ds light them.
/// Each frame it reads the hour (HallDayCycle.ShownHour: the cheat's forced
/// hour, a preview hour from its HallLightingSO, the shift clock's through
/// <see cref="SetClock"/> from AnimeHallShiftLink, in play before the clock
/// exists today's opening hour (BUG-25c: never a noon frame on load), else the
/// knobs' edit-mode hour), publishes its knobs as the one shared cycle
/// (HallDayCycle.Publish, BUG-25b), puts the hour on the solar day and sets
/// every HallLight under it: the global and sky
/// lights' colour and intensity, the sun shafts' colour, strength, turn and length,
/// and the night's stages as the hall darkens (Saleh's demo 1018a: "need
/// more lights to activate as it gets darker"; HallDayCycle.SwitchedOn at
/// each stage's evening in the knobs, each light at its own moment): the
/// desk lamp first (a filament's warm-up), the ceiling fixtures one after
/// another (a strike flicker), the gallery's mounted lamps, the lit signs (a
/// neon stutter), the open portals' night surge (<see cref="PortalNight"/>,
/// read by AnimeHallPortalLink) and the city's windows lighting one by one
/// (the global _HallCityLights the living city's shader reads); no
/// flicker with reduced motion. The screens and the PC's glow stay on,
/// fainter by day, and each portal ring's light follows its state
/// (<see cref="SetPortal"/>, from AnimeHallPortalLink). Its Plane child (the
/// Light2Ds, the pier shadows, the dust) follows the art's presentation, so
/// the lights stay on the painted fixtures if the art pans. The desk's 3D
/// lights cast no real-time shadows (BUG-14b), and the art's desk lamp spot
/// (aimed behind the AVAILABLE sign) stays dark: in play the rig makes the
/// desk's one light (DeskLightLevel: the work lamp's pool on the mat) and the
/// office finish (a global Volume with the knobs' profile, the office camera's
/// post-processing on, BUG-51). The dust's knobs
/// are applied each frame too; with reduced motion the dust is off (or thinned
/// by the knob). It gathers its lights (its own and the mounted lamps drawn
/// among the art's layers) once when enabled in play (every frame in edit
/// mode, so a duplicated light joins at once); a frame allocates nothing.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class HallLightingRig : MonoBehaviour
{
    /// <summary>The knobs (Assets/Data/Config/HallLighting_Default.asset).</summary>
    [SerializeField] private HallLightingSO settings;

    /// <summary>The art's presentation, whose pose the Plane follows.</summary>
    [SerializeField] private AnimeHallPresentation presentation;

    /// <summary>The child that carries the hall's Light2Ds, shadow casters and dust, in the presentation's space.</summary>
    [SerializeField] private Transform plane;

    /// <summary>The painted layers' 2D lit pass (on this object).</summary>
    [SerializeField] private HallBackdrop backdrop;

    /// <summary>The dust emitters.</summary>
    [SerializeField] private ParticleSystem[] dust = System.Array.Empty<ParticleSystem>();

    /// <summary>The highest portal number the rig keeps a state for.</summary>
    private const int MaxPortal = 8;

    private HallLight[] _lights = System.Array.Empty<HallLight>();
    private readonly bool[] _portalOpen = new bool[MaxPortal + 1];
    private readonly bool[] _portalReturns = new bool[MaxPortal + 1];
    private readonly float[] _portalNightSince = { -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f };
    private readonly float[] _portalNight = new float[MaxPortal + 1];
    private static readonly int CityLightsId = Shader.PropertyToID("_HallCityLights");
    private float _clockMinute;
    private bool _hasClock;
    private bool _reduced;
    private bool _lightsOn = true;
    private bool _dustPlaying = true;
    private bool _firstFrame = true;
    private float? _openingMinute;
    private bool _openingLooked;
    private GameObject _made;

    /// <summary>The knobs (null when unassigned).</summary>
    public HallLightingSO Settings => settings;

    /// <summary>The hour the hall shows now (HallDayCycle.ShownHour): the cheat menu's forced hour (DevToolsState.ForcedHour), else the preview hour, else the shift clock's, else in play today's opening hour, else the edit-mode hour.</summary>
    public float Hour => settings == null ? 12f
        : HallDayCycle.ShownHour(DevToolsState.ForcedHour, settings.previewHourOn, settings.previewHour,
                                 _hasClock ? _clockMinute : (float?)null, Application.isPlaying ? OpeningMinute() : null, settings.editModeHour);

    /// <summary>The art presentation's evening for <see cref="Hour"/> (0 by day, 1 at night; AnimeHallShiftLink hands it to SetTime).</summary>
    public float Evening => settings != null ? HallDayCycle.Evening(Hour, settings.Cycle) : 0f;

    /// <summary>The layer a gameplay drawing among the painted layers must be on to draw with them (the portal rings' effects); -1 when the 2D pass is off (they draw with the office camera).</summary>
    public int ArtLayer => backdrop != null && backdrop.Active ? backdrop.Layer : -1;

    /// <summary>The shift clock's minute of the day (AnimeHallShiftLink, each change); the clock's first minute counts as a first frame, so a lamp already on at that hour does not strike.</summary>
    public void SetClock(float minuteOfDay)
    {
        if (!_hasClock)
            _firstFrame = true;
        _clockMinute = minuteOfDay;
        _hasClock = true;
    }

    /// <summary>How far portal <paramref name="portal"/>'s night has come (0 by day; its surge past 1 once its night stage comes, then 1; 0 with the lighting off).</summary>
    public float PortalNight(int portal) => portal >= 0 && portal <= MaxPortal ? _portalNight[portal] : 0f;

    /// <summary>Portal <paramref name="portal"/>'s ring state (AnimeHallPortalLink): open (its light on), and whether it is the Return Gate (its colour).</summary>
    public void SetPortal(int portal, bool open, bool returnGate)
    {
        if (portal < 0 || portal > MaxPortal)
            return;
        _portalOpen[portal] = open;
        _portalReturns[portal] = returnGate;
    }

    private void OnEnable()
    {
        _openingLooked = false;
        Gather();
        _reduced = MotionPreference.Reduced;
        MotionPreference.Changed += OnMotionChanged;
        _firstFrame = true;
        if (Application.isPlaying)
            MakeDeskLightAndFinish();
    }

    private void OnDisable()
    {
        MotionPreference.Changed -= OnMotionChanged;
        if (backdrop != null)
            backdrop.SetOn(false, 1f);
        if (_made != null)
            Destroy(_made);
        _made = null;
    }

    private void OnMotionChanged() => _reduced = MotionPreference.Reduced;

    private void LateUpdate()
    {
        if (!Application.isPlaying)
            Gather();
        if (settings == null)
            return;

        FollowArt();
        HallDayCycle.Publish(settings.Cycle);
        bool on = settings.lightingOn;
        if (backdrop != null)
            backdrop.SetOn(on, settings.backdropResolution);
        if (on != _lightsOn || _firstFrame)
            Switch(on);
        if (!on)
        {
            System.Array.Clear(_portalNight, 0, _portalNight.Length);
            Shader.SetGlobalVector(CityLightsId, Vector4.zero);
            _firstFrame = false;
            return;
        }

        HallDayCycle.Settings cycle = settings.Cycle;
        float hour = Hour;
        float solar = HallDayCycle.SolarPosition(hour, cycle);
        float evening = HallDayCycle.Evening(hour, cycle);
        float arc = HallDayCycle.SunArc(hour, cycle);
        float dt = Application.isPlaying ? Time.deltaTime : 0f;

        foreach (HallLight l in _lights)
            if (l != null)
                Apply(l, hour, solar, evening, arc, cycle, dt);

        for (int p = 1; p <= MaxPortal; p++)
        {
            bool night = HallDayCycle.SwitchedOn(hour, settings.portalNightAt, p - 1, settings.portalNightStaggerMinutes, cycle);
            _portalNightSince[p] = Since(_portalNightSince[p], night, dt);
            _portalNight[p] = HallDayCycle.Surge(_portalNightSince[p], settings.portalNightSurgeSeconds, _reduced);
        }
        float span = Mathf.Max(1e-3f, settings.cityWindowsTo - settings.cityWindowsFrom);
        Shader.SetGlobalVector(CityLightsId, new Vector4(Mathf.Clamp01((evening - settings.cityWindowsFrom) / span), settings.cityWindowsGain, 0f, 0f));

        ApplyDust();
        _firstFrame = false;
    }

    /// <summary>A stage light's seconds since it switched on: negative while off, counting from 0 at its switch (or already past every warm-up on the first frame and in edit mode, so a hall opened at night does not blink).</summary>
    private float Since(float since, bool on, float dt)
    {
        if (!on)
            return -1f;
        if (since < 0f)
            return _firstFrame || !Application.isPlaying ? 999f : 0f;
        return since + dt;
    }

    /// <summary>One light at this hour.</summary>
    private void Apply(HallLight l, float hour, float solar, float evening, float arc, in HallDayCycle.Settings cycle, float dt)
    {
        switch (l.kind)
        {
            case HallLightKind.Global:
                Set(l, l.intensity * settings.globalIntensity.Evaluate(solar), settings.globalColour.Evaluate(solar));
                break;

            case HallLightKind.Sky:
                Set(l, l.intensity * settings.skyIntensity.Evaluate(solar), settings.skyColour.Evaluate(solar));
                break;

            case HallLightKind.Window:
                Set(l, l.intensity * settings.windowIntensity.Evaluate(solar), settings.windowColour.Evaluate(solar));
                l.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(settings.shaftAngleAtSunrise, settings.shaftAngleAtSunset, arc));
                float low = Mathf.Abs(arc - 0.5f) * 2f;
                l.transform.localScale = new Vector3(1f, Mathf.Lerp(1f, settings.shaftLowSunLength, low), 1f);
                if (l.Light2D != null)
                    l.Light2D.shadowsEnabled = settings.shaftShadows;
                break;

            case HallLightKind.Fixture:
                float level = l.Mounted
                    ? (HallDayCycle.SwitchedOn(hour, settings.galleryLampsOnAt, l.StageIndex, settings.galleryLampStaggerMinutes, cycle) ? 1f : 0f)
                    : HallDayCycle.FixtureLevel(hour, l.order, cycle);
                l.SinceOn = Since(l.SinceOn, level > 0f, dt);
                float flicker = HallDayCycle.Flicker(l.SinceOn, _reduced || !settings.fixtureFlicker);
                l.Lit = Mathf.Lerp(settings.fixtureOffShare, 1f, level) * flicker;
                Set(l, l.intensity * l.Lit, settings.fixtureColour);
                break;

            case HallLightKind.Screen:
                Set(l, l.intensity * Mathf.Lerp(settings.screenDayShare, 1f, evening));
                break;

            case HallLightKind.Sign:
                l.SinceOn = Since(l.SinceOn, HallDayCycle.SwitchedOn(hour, settings.signsOnAt, l.StageIndex, settings.signStaggerMinutes, cycle), dt);
                Set(l, l.intensity * Mathf.Lerp(settings.signDayShare, 1f, HallDayCycle.Neon(l.SinceOn, _reduced)));
                break;

            case HallLightKind.Portal:
                int portal = Mathf.Clamp(l.order, 0, MaxPortal);
                float target = _portalOpen[portal] ? 1f : 0f;
                float step = settings.portalFadeSeconds <= 0f || _reduced || !Application.isPlaying ? 1f : dt / settings.portalFadeSeconds;
                l.Share = Mathf.MoveTowards(l.Share, target, step);
                Set(l, l.intensity * l.Share, _portalReturns[portal] ? settings.returnGateLight : settings.portalOpenLight);
                break;

            case HallLightKind.DeskLamp:
                // In play the work lamp's light is the desk's one light (DeskLightLevel's pool on the mat): this spot,
                // aimed behind the sign, stays dark. In edit mode it is left as the art authored it.
                if (!Application.isPlaying)
                    break;
                NoShadows(l);
                Set(l, 0f);
                break;

            case HallLightKind.DeskScreen:
                NoShadows(l);
                Set(l, l.intensity * Mathf.Lerp(settings.deskScreenDayShare, 1f, evening));
                break;
        }
    }

    /// <summary>No real-time shadow from a desk light inside the cubicle (BUG-14b); in play only, so the art scene keeps its authored value if it is saved from the editor.</summary>
    private static void NoShadows(HallLight l)
    {
        if (Application.isPlaying && l.Light3D != null && l.Light3D.shadows != LightShadows.None)
            l.Light3D.shadows = LightShadows.None;
    }

    /// <summary>Today's opening minute (the day plan's shift, else the standard day), looked up once per enable; null without a run.</summary>
    private float? OpeningMinute()
    {
        if (_openingLooked)
            return _openingMinute;
        _openingLooked = true;
        _openingMinute = null;
        RunManager run = RunManager.Instance;
        if (run == null || run.World == null || run.Config == null)
            return null;
        GameConfigSO config = run.Config.gameConfig;
        DayPlanSO plan = run.GetCurrentDayPlan();
        _openingMinute = (plan != null ? plan.Shift(config) : GameConfigSO.Standard(config)).StartMinute;
        return _openingMinute;
    }

    /// <summary>
    /// In play: the desk's one light (DeskLightLevel, its lamp pool on the
    /// mat: the art's DeskSurface anchor) and the office finish (a global
    /// Volume with the knobs' profile; the office camera's post-processing on),
    /// made at run time under the rig and never saved. They live here, not in
    /// the gameplay layer's builder, because the builder's root has no
    /// registration seam LIGHT may use (docs/pending/LIGHT.md).
    /// </summary>
    private void MakeDeskLightAndFinish()
    {
        if (settings == null || _made != null)
            return;
        _made = new GameObject("Desk light and office finish (made at runtime)") { hideFlags = HideFlags.DontSave };
        _made.transform.SetParent(transform, false);

        OfficeSceneContractSO contract = OfficeScenes.Contract;
        ResolvedAnchor mat = OfficeAnchors.Resolve(gameObject.scene, contract != null ? contract.Spec(OfficeAnchorId.DeskSurface) : null, OfficeAnchorId.DeskSurface);
        Vector3 centre = mat.HasBounds ? new Vector3(mat.Bounds.center.x, mat.Bounds.max.y, mat.Bounds.center.z) : mat.Position;
        _made.AddComponent<AudioSource>().playOnAwake = false;
        _made.AddComponent<DeskLightLevel>().Configure(this, centre);

        if (settings.finishProfile != null)
        {
            Volume finish = _made.AddComponent<Volume>();
            finish.isGlobal = true;
            finish.sharedProfile = settings.finishProfile;
            Camera office = backdrop != null ? backdrop.Source : null;
            if (office != null)
                office.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        }
    }

    /// <summary>Writes a light's intensity (off below a trace), keeping its own colour.</summary>
    private static void Set(HallLight l, float intensity)
    {
        bool lit = intensity > 0.001f;
        if (l.Light2D != null)
        {
            l.Light2D.intensity = intensity;
            if (l.Light2D.enabled != lit)
                l.Light2D.enabled = lit;
        }
        if (l.Light3D != null)
        {
            l.Light3D.intensity = intensity;
            if (l.Light3D.enabled != lit)
                l.Light3D.enabled = lit;
        }
    }

    /// <summary>Writes a light's intensity and colour.</summary>
    private static void Set(HallLight l, float intensity, Color colour)
    {
        if (l.Light2D != null)
            l.Light2D.color = colour;
        if (l.Light3D != null)
            l.Light3D.color = colour;
        Set(l, intensity);
    }

    /// <summary>The dust's knobs, and its pause under reduced motion (or its thinning, by the knob).</summary>
    private void ApplyDust()
    {
        float share = _reduced ? settings.dustReducedMotionShare : 1f;
        bool play = share > 0f && settings.dustMaxParticles > 0;
        foreach (ParticleSystem ps in dust)
        {
            if (ps == null)
                continue;
            ParticleSystem.MainModule main = ps.main;
            main.maxParticles = Mathf.Max(0, Mathf.RoundToInt(settings.dustMaxParticles * share));
            main.startLifetime = new ParticleSystem.MinMaxCurve(settings.dustLifetime.x, settings.dustLifetime.y);
            main.startSize = new ParticleSystem.MinMaxCurve(settings.dustSize.x, settings.dustSize.y);
            main.startColor = settings.dustColour;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = settings.dustRate * share;
            ParticleSystem.NoiseModule noise = ps.noise;
            noise.strength = settings.dustDrift;
            ParticleSystem.VelocityOverLifetimeModule drift = ps.velocityOverLifetime;
            drift.x = new ParticleSystem.MinMaxCurve(-settings.dustDrift * 0.5f, settings.dustDrift * 0.5f);
            drift.y = new ParticleSystem.MinMaxCurve(-settings.dustDrift * 0.3f, settings.dustDrift * 0.4f);

            if (!Application.isPlaying)
                continue;
            if (play && (!_dustPlaying || !ps.isPlaying))
                ps.Play(false);
            else if (!play && _dustPlaying)
                ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        _dustPlaying = play;
    }

    /// <summary>Switches every light and the dust on or off (the knob's lightingOn).</summary>
    private void Switch(bool on)
    {
        _lightsOn = on;
        foreach (HallLight l in _lights)
        {
            if (l == null)
                continue;
            if (l.Light2D != null)
                l.Light2D.enabled = on;
            if (l.Light3D != null)
                l.Light3D.enabled = on;
            l.Lit = 0f;
        }
        foreach (ParticleSystem ps in dust)
            if (ps != null)
                ps.gameObject.SetActive(on);
        _dustPlaying = on;
    }

    /// <summary>The Plane takes the presentation's pose, so its lights stay on the painted fixtures when the art pans.</summary>
    private void FollowArt()
    {
        if (plane == null || presentation == null)
            return;
        Transform art = presentation.transform;
        plane.SetPositionAndRotation(art.position, art.rotation);
        Vector3 scale = art.lossyScale, parent = plane.parent != null ? plane.parent.lossyScale : Vector3.one;
        plane.localScale = new Vector3(scale.x / parent.x, scale.y / parent.y, scale.z / parent.z);
    }

    /// <summary>The HallLights under this object, and those drawn among the art's layers (the gallery's mounted lamps, children of the presentation): every fixture keeps the one schedule.</summary>
    private void Gather()
    {
        _lights = GetComponentsInChildren<HallLight>(true);
        if (presentation != null)
        {
            HallLight[] drawn = presentation.GetComponentsInChildren<HallLight>(true);
            if (drawn.Length > 0)
                _lights = _lights.Concat(drawn).Distinct().ToArray();
        }
        foreach (HallLight l in _lights)
            l.Bind();
        // Each night stage's lights in name order: their places in its switching order.
        HallLight[] byName = _lights.Where(l => l != null).OrderBy(l => l.name, System.StringComparer.Ordinal).ToArray();
        foreach (HallLightKind kind in new[] { HallLightKind.Fixture, HallLightKind.Sign })
        {
            int index = 0;
            foreach (HallLight l in byName)
                if (l.kind == kind && (kind != HallLightKind.Fixture || l.Mounted))
                    l.StageIndex = index++;
        }
    }
}


