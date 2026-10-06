using UnityEngine;

/// <summary>
/// The anime hall's lights and dust, driven through a full day-night cycle
/// (docs/HALL_LIGHTING.md). It sits on the hall's HallLighting root (Tools >
/// TimeDesk > Add Anime Hall Hooks) with the HallBackdrop that draws the
/// painted layers through the URP 2D Renderer, so the Light2Ds light them.
/// Each frame it reads the hour (a preview hour from its HallLightingSO, the
/// shift clock's through <see cref="SetClock"/> from AnimeHallShiftLink, or
/// the knobs' edit-mode hour without a clock), puts it on the solar day
/// (HallDayCycle) and sets every HallLight under it: the global and sky
/// lights' colour and intensity, the sun shafts' colour, strength, turn and length,
/// the fixtures switching on at dusk one after another (a strike flicker
/// unless reduced motion), the screens, signs and desk lights always on
/// (fainter by day), and each portal ring's light following its state
/// (<see cref="SetPortal"/>, from AnimeHallPortalLink). Its Plane child (the
/// Light2Ds, the pier shadows, the dust) follows the art's presentation, so
/// the lights stay on the painted fixtures if the art pans. The dust's knobs
/// are applied each frame too; with reduced motion the dust is off (or thinned
/// by the knob). It gathers its lights once when enabled in play (every
/// frame in edit mode, so a duplicated light joins at once); a frame allocates nothing.
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
    private float _clockMinute;
    private bool _hasClock;
    private bool _reduced;
    private bool _lightsOn = true;
    private bool _dustPlaying = true;
    private bool _firstFrame = true;

    /// <summary>The knobs (null when unassigned).</summary>
    public HallLightingSO Settings => settings;

    /// <summary>The hour the hall shows now: the cheat menu's forced hour (DevToolsState.ForcedHour), else the preview hour, else the shift clock's, else the edit-mode hour.</summary>
    public float Hour => settings == null ? 12f
        : DevToolsState.ForcedHour.HasValue ? DevToolsState.ForcedHour.Value
        : settings.previewHourOn ? settings.previewHour
        : _hasClock ? HallDayCycle.Hour(_clockMinute)
        : settings.editModeHour;

    /// <summary>The art presentation's evening for <see cref="Hour"/> (0 by day, 1 at night; AnimeHallShiftLink hands it to SetTime).</summary>
    public float Evening => settings != null ? HallDayCycle.Evening(Hour, settings.Cycle) : 0f;

    /// <summary>What the traveller's tint is multiplied by at <see cref="Hour"/> (white without knobs).</summary>
    public Color TravellerShade => settings != null ? settings.travellerShade.Evaluate(HallDayCycle.SolarPosition(Hour, settings.Cycle)) : Color.white;

    /// <summary>The layer a gameplay drawing among the painted layers must be on to draw with them (the portal rings' effects); -1 when the 2D pass is off (they draw with the office camera).</summary>
    public int ArtLayer => backdrop != null && backdrop.Active ? backdrop.Layer : -1;

    /// <summary>The shift clock's minute of the day (AnimeHallShiftLink, each change).</summary>
    public void SetClock(float minuteOfDay)
    {
        _clockMinute = minuteOfDay;
        _hasClock = true;
    }

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
        Gather();
        _reduced = MotionPreference.Reduced;
        MotionPreference.Changed += OnMotionChanged;
        _firstFrame = true;
    }

    private void OnDisable()
    {
        MotionPreference.Changed -= OnMotionChanged;
        if (backdrop != null)
            backdrop.SetOn(false, 1f);
    }

    private void OnMotionChanged() => _reduced = MotionPreference.Reduced;

    private void LateUpdate()
    {
        if (!Application.isPlaying)
            Gather();
        if (settings == null)
            return;

        FollowArt();
        bool on = settings.lightingOn;
        if (backdrop != null)
            backdrop.SetOn(on, settings.backdropResolution);
        if (on != _lightsOn || _firstFrame)
            Switch(on);
        if (!on)
        {
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

        ApplyDust();
        _firstFrame = false;
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
                float level = HallDayCycle.FixtureLevel(hour, l.order, cycle);
                if (level <= 0f)
                    l.SinceOn = -1f;
                else if (l.SinceOn < 0f)
                    l.SinceOn = _firstFrame || !Application.isPlaying ? HallDayCycle.FlickerSeconds : 0f;
                else
                    l.SinceOn += dt;
                float flicker = HallDayCycle.Flicker(l.SinceOn, _reduced || !settings.fixtureFlicker);
                Set(l, l.intensity * Mathf.Lerp(settings.fixtureOffShare, 1f, level) * flicker, settings.fixtureColour);
                break;

            case HallLightKind.Screen:
                Set(l, l.intensity * Mathf.Lerp(settings.screenDayShare, 1f, evening));
                break;

            case HallLightKind.Sign:
                Set(l, l.intensity * Mathf.Lerp(settings.signDayShare, 1f, evening));
                break;

            case HallLightKind.Portal:
                int portal = Mathf.Clamp(l.order, 0, MaxPortal);
                float target = _portalOpen[portal] ? 1f : 0f;
                float step = settings.portalFadeSeconds <= 0f || _reduced || !Application.isPlaying ? 1f : dt / settings.portalFadeSeconds;
                l.Share = Mathf.MoveTowards(l.Share, target, step);
                Set(l, l.intensity * l.Share, _portalReturns[portal] ? settings.returnGateLight : settings.portalOpenLight);
                break;

            case HallLightKind.DeskLamp:
                Set(l, l.intensity * Mathf.Lerp(settings.deskLampDayShare, 1f, evening));
                break;

            case HallLightKind.DeskScreen:
                Set(l, l.intensity * Mathf.Lerp(settings.deskScreenDayShare, 1f, evening));
                break;
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

    /// <summary>The HallLights under this object.</summary>
    private void Gather()
    {
        _lights = GetComponentsInChildren<HallLight>(true);
        foreach (HallLight l in _lights)
            l.Bind();
    }
}
