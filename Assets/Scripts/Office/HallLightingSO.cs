using UnityEngine;
using UnityEngine.Rendering;

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
    [Range(0f, 24f)] public float sunriseHour = HallDayCycle.Defaults.SunriseHour;

    /// <summary>The hour the sun sets (the daylight half down). 16:30 puts the dusk in the shift's last hour (it closes at 17:00).</summary>
    [Range(0f, 24f)] public float sunsetHour = HallDayCycle.Defaults.SunsetHour;

    /// <summary>How long dawn and dusk each last (hours), centred on sunrise and sunset.</summary>
    [Range(0.1f, 6f)] public float twilightHours = HallDayCycle.Defaults.TwilightHours;

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
    [Range(0f, 1f)] public float fixturesOnBelow = HallDayCycle.Defaults.FixturesOnBelow;

    /// <summary>How much further the daylight falls while a fixture comes fully on.</summary>
    [Range(0.01f, 0.5f)] public float fixtureFadeBand = HallDayCycle.Defaults.FixtureFadeBand;

    /// <summary>In-game minutes between one fixture switching and the next (their HallLight order).</summary>
    [Range(0f, 30f)] public float fixtureStaggerMinutes = HallDayCycle.Defaults.FixtureStaggerMinutes;

    /// <summary>The share of its strength a fixture gives while off (0: dark by day).</summary>
    [Range(0f, 1f)] public float fixtureOffShare = 0f;

    /// <summary>True: a fixture blinks for a moment as it strikes (never with reduced motion).</summary>
    public bool fixtureFlicker = true;

    [Header("Screens, signs and the PC (the signs switch on at their night stage)")]
    /// <summary>The share of their strength the hall's screens (the board, the vending machine, the small displays) give by day (their glow hardly reads in full daylight); full at night.</summary>
    [Range(0f, 1f)] public float screenDayShare = 0.15f;

    /// <summary>The share of their strength the lit signs (the department doors' panels, the alarm lamps) give before their night stage (signsOnAt); full after it.</summary>
    [Range(0f, 1f)] public float signDayShare = 0.1f;

    /// <summary>The same for the PC screen's glow on the desk (a 3D light).</summary>
    [Range(0f, 1f)] public float deskScreenDayShare = 0.4f;

    [Header("Night stages (Saleh 1018a: \"need more lights to activate as it gets darker\"; the evening: 0 by day, 1 at night)")]
    /// <summary>The evening at which the desk's work lamp switches on (off before it): the afternoon dip (DeskLighting.LampOnHour), read by DeskLightLevel. 0.5 is the sunset (16:30 on the shipped cycle): the room has lost enough by then for the switch-on to show, since the lamp fills only what the room has lost.</summary>
    [Range(0f, 1f)] public float deskLampOnAt = 0.5f;

    /// <summary>Seconds the desk lamp's filament takes to warm up (a dim blink, then easing up; at once with reduced motion).</summary>
    [Range(0f, 3f)] public float deskLampWarmSeconds = 0.6f;

    /// <summary>The evening at which the gallery's mounted lamps switch on, after the ceiling fixtures (fixturesOnBelow), one after another.</summary>
    [Range(0f, 1f)] public float galleryLampsOnAt = 0.4f;

    /// <summary>In-game minutes between one gallery lamp and the next.</summary>
    [Range(0f, 30f)] public float galleryLampStaggerMinutes = 6f;

    /// <summary>The evening at which the lit signs switch from their day share to full, each with a neon stutter.</summary>
    [Range(0f, 1f)] public float signsOnAt = 0.55f;

    /// <summary>In-game minutes between one sign and the next.</summary>
    [Range(0f, 30f)] public float signStaggerMinutes = 3f;

    /// <summary>The evening at which the open portals' vortexes surge to their night brightness (PortalVortexTuning.nightBoost), one ring after another by number.</summary>
    [Range(0f, 1f)] public float portalNightAt = 0.7f;

    /// <summary>In-game minutes between one portal's night surge and the next.</summary>
    [Range(0f, 30f)] public float portalNightStaggerMinutes = 2f;

    /// <summary>Seconds a portal's night surge takes (past full, then settling; at once with reduced motion).</summary>
    [Range(0f, 5f)] public float portalNightSurgeSeconds = 1.2f;

    /// <summary>The evening at which the city's first windows light, and the one at which the last does (each window between, at its own moment, with a blink; the hall windows' and the city view's living city).</summary>
    [Range(0f, 1f)] public float cityWindowsFrom = 0.15f, cityWindowsTo = 0.95f;

    /// <summary>How bright a lit city window is added over the painting.</summary>
    [Range(0f, 2f)] public float cityWindowsGain = 0.9f;

    [Header("Portal rings (their state: AnimeHallPortalLink)")]
    /// <summary>The light an open departure portal throws around its ring.</summary>
    public Color portalOpenLight = new Color(0.35f, 0.85f, 1f);

    /// <summary>The light the Return Gate throws around its ring.</summary>
    public Color returnGateLight = new Color(1f, 0.62f, 0.25f);

    /// <summary>Seconds a ring's light takes to come on or go off when its state changes (at once with reduced motion).</summary>
    [Range(0f, 5f)] public float portalFadeSeconds = 0.8f;

    [Header("The desk: one light for papers, props, rack, traveller and photo (DeskLighting, DeskLightLevel)")]
    /// <summary>NOPE/Desk Anime's key light energy at the full day (the old noon daylight's: 0.7 times its warm white), which the desk light's level scales.</summary>
    [Range(0.1f, 2f)] public float deskKeyAtDay = 0.68f;

    /// <summary>The room's light on the desk at night as a share of the full day's, without the lamp (with the lamp's pool it keeps the whole mat readable after the finish: DeskLighting.ReadableAfterFinish).</summary>
    [Range(0f, 1f)] public float deskNightFloor = 0.55f;

    /// <summary>The room's colour on the desk at night (white by day).</summary>
    public Color deskNightTint = new Color(0.78f, 0.84f, 1f);

    /// <summary>What the work lamp adds to the desk's level, warm and full, at the pool's centre; it fills at most what the room has lost (the level never passes the full day's 1), so floor + lamp = 1 puts the pool's centre at the drawn level at night.</summary>
    [Range(0f, 1f)] public float deskLampLevel = 0.45f;

    /// <summary>The work lamp's colour (a warm filament).</summary>
    public Color deskLampColour = new Color(1f, 0.82f, 0.6f);

    /// <summary>The lamp pool's radius on the desk (metres), centred on the mat; its share and its warm colour are full out to a quarter of it and fall off to nothing at the edge (1.35: the 1.45 x 1.02 m mat's corners keep about 0.47 of the lamp, so the pool reads and the corners stay readable).</summary>
    [Range(0.1f, 2f)] public float deskLampRadius = 1.35f;

    [Header("The hall at night (NOPE/Hall Deep Layout, through HallBakedLighting)")]
    /// <summary>The painted hall's tint at full night, away from the lamp pools.</summary>
    public Color hallNightTint = new Color(0.2f, 0.27f, 0.43f);

    /// <summary>How strongly a lit fixture's pool lights the floor and the wall around it (the fixture's colour times its lit share).</summary>
    [Range(0f, 2f)] public float hallPoolStrength = 0.35f;

    /// <summary>The lit ceiling diffusers' glow (1: as painted; above 1 they glow past white, which the finish's bloom picks up).</summary>
    [Range(0f, 4f)] public float hallEmissionGain = 2.5f;

    [Header("The finish (one global Volume: tonemapping, a light grade, bloom, vignette)")]
    /// <summary>The office's post-processing profile (Assets/Settings/OfficeFinishProfile.asset), put on a global Volume at play with the office camera's post-processing on (null: none).</summary>
    public VolumeProfile finishProfile;

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

    /// <summary>The desk light's knobs for DeskLighting.</summary>
    public DeskLighting.Knobs DeskKnobs => new DeskLighting.Knobs(deskNightFloor, Rgb(deskNightTint), deskLampLevel, Rgb(deskLampColour), deskLampOnAt);

    /// <summary>
    /// The office finish as DeskLighting's readable rule models it: the
    /// profile's Color Adjustments (post exposure, contrast) and Neutral
    /// tonemapping, in the active URP asset's grading mode (none when there
    /// is no profile).
    /// </summary>
    public DeskLighting.Finish FinishModel()
    {
        if (finishProfile == null)
            return DeskLighting.Finish.None;
        float exposure = 0f, contrast = 0f;
        if (finishProfile.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments grade) && grade.active)
        {
            exposure = grade.postExposure.overrideState ? grade.postExposure.value : 0f;
            contrast = grade.contrast.overrideState ? grade.contrast.value : 0f;
        }
        bool neutral = finishProfile.TryGet(out UnityEngine.Rendering.Universal.Tonemapping tonemapping) && tonemapping.active
                       && tonemapping.mode.value == UnityEngine.Rendering.Universal.TonemappingMode.Neutral;
        bool hdr = GraphicsSettings.currentRenderPipeline is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urp
                   && urp.colorGradingMode == UnityEngine.Rendering.Universal.ColorGradingMode.HighDynamicRange;
        return new DeskLighting.Finish(exposure, contrast, neutral, hdr);
    }

    private static Rgba Rgb(Color c) => new Rgba(c.r, c.g, c.b, c.a);

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
