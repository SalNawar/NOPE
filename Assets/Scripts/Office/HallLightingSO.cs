using UnityEngine;

/// <summary>
/// The anime hall's lights and dust (docs/HALL_LIGHTING.md), every knob read
/// each frame by HallLightingRig, so a change shows at once, in play mode too.
/// The time: the shift clock's hour (AnimeHallShiftLink), a preview hour when
/// <see cref="previewHourOn"/>, and <see cref="editModeHour"/> without a clock.
/// The day: sunrise, sunset and the twilights (HallDayCycle); the colour
/// gradients and intensity curves are read at the solar position (0 solar
/// midnight, 0.25 sunrise, 0.5 solar noon, 0.75 sunset), so moving sunrise
/// or sunset keeps them in step. Each light's own strength is its HallLight
/// component's intensity; these knobs scale it by the time of day. Created by
/// Tools > TimeDesk > Add Anime Hall Hooks (Assets/Data/Config/HallLighting_Default.asset).
/// </summary>
[CreateAssetMenu(fileName = "HallLighting_Default", menuName = "TimeDesk/Office/Hall Lighting")]
public sealed class HallLightingSO : ScriptableObject
{
    [Header("Switch")]
    /// <summary>False draws the hall as painted, unlit, as before the lights (the lights, the dust and the 2D backdrop camera off).</summary>
    public bool lightingOn = true;

    [Header("Time")]
    /// <summary>True shows <see cref="previewHour"/> instead of the shift clock (to preview night or dawn, which a shift never reaches).</summary>
    public bool previewHourOn;

    /// <summary>The hour shown while <see cref="previewHourOn"/> (0 to 24).</summary>
    [Range(0f, 24f)] public float previewHour = 22f;

    /// <summary>The hour shown without a shift clock (the hall open on its own, edit mode).</summary>
    [Range(0f, 24f)] public float editModeHour = 12f;

    [Header("The day (HallDayCycle)")]
    /// <summary>The hour the sun rises (the daylight half up).</summary>
    [Range(0f, 24f)] public float sunriseHour = 7f;

    /// <summary>The hour the sun sets (the daylight half down). 16:30 puts the dusk in the shift's last hour (it closes at 17:00).</summary>
    [Range(0f, 24f)] public float sunsetHour = 16.5f;

    /// <summary>How long dawn and dusk each last (hours), centred on sunrise and sunset.</summary>
    [Range(0.1f, 6f)] public float twilightHours = 1.5f;

    [Header("Global light (the whole hall, multiplied over the art)")]
    /// <summary>The hall's overall light colour along the solar day (x: solar position).</summary>
    public Gradient globalColour = Gradient(
        (0f, new Color(0.34f, 0.40f, 0.66f)), (0.25f, new Color(0.93f, 0.68f, 0.64f)), (0.32f, new Color(1f, 0.91f, 0.82f)),
        (0.5f, Color.white), (0.68f, new Color(1f, 0.93f, 0.82f)), (0.75f, new Color(0.98f, 0.64f, 0.48f)),
        (0.82f, new Color(0.50f, 0.46f, 0.74f)), (1f, new Color(0.34f, 0.40f, 0.66f)));

    /// <summary>The hall's overall light intensity along the solar day (x: solar position; 1 shows the art as painted).</summary>
    public AnimationCurve globalIntensity = Curve((0f, 0.40f), (0.2f, 0.42f), (0.25f, 0.6f), (0.32f, 0.8f), (0.5f, 0.88f), (0.68f, 0.85f), (0.75f, 0.65f), (0.82f, 0.45f), (1f, 0.40f));

    [Header("Sky (the exterior seen through the windows; no interior light reaches it)")]
    /// <summary>The sky's colour along the solar day (x: solar position).</summary>
    public Gradient skyColour = Gradient(
        (0f, new Color(0.24f, 0.30f, 0.58f)), (0.25f, new Color(1f, 0.68f, 0.60f)), (0.32f, new Color(0.96f, 0.96f, 1f)),
        (0.5f, Color.white), (0.68f, new Color(1f, 0.92f, 0.80f)), (0.75f, new Color(1f, 0.52f, 0.30f)),
        (0.81f, new Color(0.62f, 0.38f, 0.62f)), (1f, new Color(0.24f, 0.30f, 0.58f)));

    /// <summary>The sky's brightness along the solar day (1 shows the painted sky as painted).</summary>
    public AnimationCurve skyIntensity = Curve((0f, 0.34f), (0.2f, 0.36f), (0.25f, 0.8f), (0.32f, 1f), (0.68f, 1f), (0.75f, 0.95f), (0.81f, 0.6f), (1f, 0.34f));

    [Header("Windows (sun shafts through the glass)")]
    /// <summary>The shafts' colour along the solar day.</summary>
    public Gradient windowColour = Gradient(
        (0f, new Color(0.45f, 0.55f, 0.85f)), (0.25f, new Color(1f, 0.62f, 0.42f)), (0.35f, new Color(1f, 0.88f, 0.70f)),
        (0.5f, new Color(1f, 0.97f, 0.90f)), (0.66f, new Color(1f, 0.86f, 0.64f)), (0.75f, new Color(1f, 0.55f, 0.32f)), (1f, new Color(0.45f, 0.55f, 0.85f)));

    /// <summary>The shafts' strength along the solar day (x: solar position): strongest with a low sun, none at night.</summary>
    public AnimationCurve windowIntensity = Curve((0f, 0f), (0.23f, 0f), (0.29f, 0.9f), (0.4f, 0.7f), (0.5f, 0.55f), (0.62f, 0.75f), (0.72f, 1f), (0.78f, 0f), (1f, 0f));

    /// <summary>The shafts' turn at sunrise (degrees around the view axis); they turn to <see cref="shaftAngleAtSunset"/> along the sun's arc.</summary>
    [Range(-60f, 60f)] public float shaftAngleAtSunrise = -10f;

    /// <summary>The shafts' turn at sunset (degrees).</summary>
    [Range(-60f, 60f)] public float shaftAngleAtSunset = 8f;

    /// <summary>How much longer a shaft is with the sun on the horizon than at noon (1 = the same length).</summary>
    [Range(1f, 2.5f)] public float shaftLowSunLength = 1.3f;

    [Header("Interior fixtures (ceiling lights)")]
    /// <summary>The fixtures' light colour (a warm fluorescent).</summary>
    public Color fixtureColour = new Color(1f, 0.90f, 0.74f);

    /// <summary>The daylight below which a fixture starts to switch on (HallDayCycle.FixtureLevel).</summary>
    [Range(0f, 1f)] public float fixturesOnBelow = 0.6f;

    /// <summary>How much further the daylight falls while a fixture comes fully on.</summary>
    [Range(0.01f, 0.5f)] public float fixtureFadeBand = 0.1f;

    /// <summary>In-game minutes between one fixture switching and the next (their HallLight order).</summary>
    [Range(0f, 30f)] public float fixtureStaggerMinutes = 4f;

    /// <summary>The share of its strength a fixture gives while off (0: dark by day).</summary>
    [Range(0f, 1f)] public float fixtureOffShare = 0f;

    /// <summary>True: a fixture blinks for a moment as it strikes (never with reduced motion).</summary>
    public bool fixtureFlicker = true;

    [Header("Always on: screens, signs, the desk lamp and the PC")]
    /// <summary>The share of their strength the hall's screens (the board, the vending machine, the small displays) give by day (their glow hardly reads in full daylight); full at night.</summary>
    [Range(0f, 1f)] public float screenDayShare = 0.15f;

    /// <summary>The same for the lit signs (the department doors' panels, the alarm lamps).</summary>
    [Range(0f, 1f)] public float signDayShare = 0.1f;

    /// <summary>The same for the desk lamp (a 3D light on the preserved desk).</summary>
    [Range(0f, 1f)] public float deskLampDayShare = 0.35f;

    /// <summary>The same for the PC screen's glow on the desk (a 3D light).</summary>
    [Range(0f, 1f)] public float deskScreenDayShare = 0.4f;

    [Header("Portal rings (their state: AnimeHallPortalLink)")]
    /// <summary>The light an open departure portal throws around its ring.</summary>
    public Color portalOpenLight = new Color(0.35f, 0.85f, 1f);

    /// <summary>The light the Return Gate throws around its ring.</summary>
    public Color returnGateLight = new Color(1f, 0.62f, 0.25f);

    /// <summary>Seconds a ring's light takes to come on or go off when its state changes (at once with reduced motion).</summary>
    [Range(0f, 5f)] public float portalFadeSeconds = 0.8f;

    [Header("The traveller (unlit art, tinted into the hall's light)")]
    /// <summary>Multiplies the traveller's tint (DeskConfigSO.travellerTint) along the solar day, so the figure sits in the hall's light; kept light enough that the face reads.</summary>
    public Gradient travellerShade = Gradient(
        (0f, new Color(0.62f, 0.66f, 0.82f)), (0.25f, new Color(0.95f, 0.84f, 0.82f)), (0.32f, Color.white), (0.68f, Color.white),
        (0.75f, new Color(1f, 0.86f, 0.78f)), (0.82f, new Color(0.72f, 0.70f, 0.86f)), (1f, new Color(0.62f, 0.66f, 0.82f)));

    [Header("Dust (slow motes, seen in the light)")]
    /// <summary>The most motes alive at once (per emitter).</summary>
    [Range(0, 200)] public int dustMaxParticles = 40;

    /// <summary>Motes born per second (per emitter).</summary>
    [Range(0f, 20f)] public float dustRate = 3f;

    /// <summary>A mote's life (seconds, min and max).</summary>
    public Vector2 dustLifetime = new Vector2(9f, 16f);

    /// <summary>A mote's size (metres on the hall's plane, min and max; a canvas pixel is 0.02 m).</summary>
    public Vector2 dustSize = new Vector2(0.05f, 0.11f);

    /// <summary>How fast the motes drift (metres a second).</summary>
    [Range(0f, 1f)] public float dustDrift = 0.08f;

    /// <summary>The motes' colour and opacity (lit by the hall's lights: bright in a shaft, faint in the dark).</summary>
    public Color dustColour = new Color(1f, 0.96f, 0.88f, 0.45f);

    /// <summary>The share of the dust kept with reduced motion (0: none).</summary>
    [Range(0f, 1f)] public float dustReducedMotionShare = 0f;

    [Header("Shadows")]
    /// <summary>True: the sun shafts cast the piers' shadows (ShadowCaster2D).</summary>
    public bool shaftShadows = true;

    [Header("The 2D backdrop (HallBackdrop)")]
    /// <summary>The lit hall's render size as a share of the screen's (1: pixel for pixel).</summary>
    [Range(0.5f, 1f)] public float backdropResolution = 1f;

    /// <summary>The day-night cycle's knobs for HallDayCycle (fixture threshold, band and stagger included).</summary>
    public HallDayCycle.Settings Cycle => new HallDayCycle.Settings(sunriseHour, sunsetHour, twilightHours, fixturesOnBelow, fixtureFadeBand, fixtureStaggerMinutes);

    private static Gradient Gradient(params (float t, Color c)[] keys)
    {
        var colours = new GradientColorKey[keys.Length];
        for (int i = 0; i < keys.Length; i++)
            colours[i] = new GradientColorKey(keys[i].c, keys[i].t);
        var g = new Gradient();
        g.SetKeys(colours, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }

    private static AnimationCurve Curve(params (float t, float v)[] keys)
    {
        var frames = new Keyframe[keys.Length];
        for (int i = 0; i < keys.Length; i++)
            frames[i] = new Keyframe(keys[i].t, keys[i].v);
        var curve = new AnimationCurve(frames);
        for (int i = 0; i < frames.Length; i++)
            curve.SmoothTangents(i, 0f);
        return curve;
    }
}
