using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// LIGHT's gate checks (FIX_PLAN LIGHT; QA-RIG's DemoGate finds them by
/// their <see cref="GateCheckAttribute"/>): the knobs and assets that keep
/// the one lighting model in place. Each returns one <see cref="GateResult"/>
/// with its measured number. They read assets and sources only (no scene, no
/// play), so they are Static checks and also run in the editor's EditMode
/// suite (LightChecksTests). The [BP] pass lines (the luma ladder, the
/// first-frame hour, the photo match, the GPU budget) are recipes in
/// docs/pending/LIGHT.md, measured on QA-RIG's rig.
/// </summary>
public static class LightChecks
{
    private const string Owner = "LIGHT";

    /// <summary>The hall's lighting knobs.</summary>
    public const string HallLighting = "Assets/Data/Config/HallLighting_Default.asset";

    /// <summary>The office renderer's SSAO feature (cut, BUG-16b).</summary>
    public const string ContactShading = "Assets/Settings/OfficeContactShading.asset";

    /// <summary>The painted hall's layer shader.</summary>
    public const string DeepLayout = "Assets/Shaders/HallDeepLayout.shader";

    /// <summary>The mat: the DeskSurface anchor, the blotter's 1.45 × 1.02 m (docs/SCENE_CONTRACT_GAMEPLAY.md). DeskLightLevel also checks the art's real mat at play.</summary>
    public const float MatWidth = 1.45f, MatDepth = 1.02f;

    /// <summary>BUG-16b: the blue-noise SSAO (re-seeded every frame in the player) is off.</summary>
    [GateCheck("light.ssao.cut", Owner)]
    public static GateResult SsaoIsCut()
    {
        var feature = AssetDatabase.LoadAssetAtPath<ScriptableRendererFeature>(ContactShading);
        bool active = feature != null && feature.isActive;
        return GateResult.Of("light.ssao.cut", Owner, !active, feature == null ? "no SSAO feature" : $"SSAO active: {active}",
            active ? $"{ContactShading} is active (BUG-16b: its noise crawls in the player)." : string.Empty);
    }

    /// <summary>
    /// BUG-14a: at full night with the lamp on, the whole mat reads on the
    /// image shown: after the office finish (the profile's exposure, Neutral
    /// tonemapping and contrast in the URP asset's grading mode,
    /// HallLightingSO.FinishModel) the mat's corners (the farthest from the
    /// lamp's pool) clear luma 55 by DeskLighting.MarginLuma, the pool's
    /// centre reaches 60, and the pool reads (centre to corners at least
    /// DeskLighting.PoolContrastLuma). DeskLighting is DeskLight.hlsl's maths;
    /// the vignette is a frame (the same factor by day) and is reported, not
    /// ruled on (docs/pending/LIGHT.md).
    /// </summary>
    [GateCheck("light.desk.readableAtNight", Owner)]
    public static GateResult DeskStaysReadableAtNight()
    {
        HallLightingSO knobs = Knobs();
        if (knobs == null)
            return GateResult.Fail("light.desk.readableAtNight", Owner, "no knobs", $"{HallLighting} is missing.");
        DeskLighting.Finish finish = knobs.FinishModel();
        bool pass = DeskLighting.ReadableAfterFinish(knobs.DeskKnobs, knobs.deskLampRadius, MatWidth, MatDepth, finish, out float centre, out float corners);
        DeskLighting.Light night = DeskLighting.FullNight(knobs.DeskKnobs);
        float rawCorners = DeskLighting.LumaAt(DeskLighting.DarkestOnMat(night, knobs.deskLampRadius, MatWidth, MatDepth));
        return GateResult.Of("light.desk.readableAtNight", Owner, pass,
            $"night mat luma after the finish (EV {finish.PostExposure:+0.0;-0.0}, contrast {finish.Contrast:0}, {(finish.Neutral ? "Neutral" : "no tonemap")}, {(finish.HdrGrading ? "HDR" : "LDR")} grading): centre {centre:0.0}, corners {corners:0.0} (before the finish {rawCorners:0.0}), pool step {centre - corners:0.0}",
            pass ? string.Empty : $"Needs the corners ≥ {DeskLighting.ReadableMatLuma + DeskLighting.MarginLuma:0}, the centre ≥ {DeskLighting.TargetMatLuma:0} and a pool step ≥ {DeskLighting.PoolContrastLuma:0}: retune deskNightFloor, deskLampLevel or deskLampRadius.");
    }

    /// <summary>The plan's afternoon dip (FIX_PLAN's H3, 16:45): the lamp is on by then.</summary>
    public const float AfternoonDipHour = 16.75f;

    /// <summary>
    /// BUG-14a: the work lamp switches on in the dusk, by the plan's afternoon
    /// dip (H3, 16:45), and visibly: the pool's centre gains at least
    /// DeskLighting.SwitchOnLuma after the finish as it comes on (the lamp
    /// fills only what the room has lost, so a switch-on in full daylight
    /// would not show).
    /// </summary>
    [GateCheck("light.desk.lampAtTheDip", Owner)]
    public static GateResult LampSwitchesOnAtTheDip()
    {
        HallLightingSO knobs = Knobs();
        if (knobs == null)
            return GateResult.Fail("light.desk.lampAtTheDip", Owner, "no knobs", $"{HallLighting} is missing.");
        HallDayCycle.Settings cycle = knobs.Cycle;
        DeskLighting.Knobs desk = knobs.DeskKnobs;
        float dip = DeskLighting.LampOnHour(cycle, desk);
        float from = HallDayCycle.HourAtEvening(0f, cycle);
        DeskLighting.Finish finish = knobs.FinishModel();
        float off = DeskLighting.GradedLuma(DeskLighting.LuminanceAt(DeskLighting.Evaluate(dip, cycle, desk, 0f), 1f), finish);
        float on = DeskLighting.GradedLuma(DeskLighting.LuminanceAt(DeskLighting.Evaluate(dip, cycle, desk, 1f), 1f), finish);
        bool inTime = dip >= from - 1e-3f && dip <= AfternoonDipHour + 1e-3f;
        bool visible = on - off >= DeskLighting.SwitchOnLuma;
        return GateResult.Of("light.desk.lampAtTheDip", Owner, inTime && visible,
            $"lamp on at {Clock(dip)} (dusk from {Clock(from)}, dip {Clock(AfternoonDipHour)}); pool centre {off:0.0} -> {on:0.0}",
            !inTime ? "The lamp switches on outside the dusk or after the afternoon dip (H3)."
            : !visible ? $"The switch-on lifts the pool's centre by under {DeskLighting.SwitchOnLuma:0} luma: switch it on later (deskLampOnAt), when the room has lost more." : string.Empty);
    }

    /// <summary>BUG-51: the office finish has tonemapping, a grade, bloom and a vignette.</summary>
    [GateCheck("light.finish.profile", Owner)]
    public static GateResult FinishHasBloomGradeAndVignette()
    {
        HallLightingSO knobs = Knobs();
        VolumeProfile profile = knobs != null ? knobs.finishProfile : null;
        if (profile == null)
            return GateResult.Fail("light.finish.profile", Owner, "no profile", "HallLighting_Default has no finish profile (BUG-51).");
        string missing = string.Empty;
        if (!profile.TryGet(out Tonemapping tonemapping) || !tonemapping.active)
            missing += " tonemapping";
        if (!profile.TryGet(out ColorAdjustments grade) || !grade.active)
            missing += " grade";
        bool bloomOn = profile.TryGet(out Bloom bloom) && bloom.active && bloom.intensity.value > 0f;
        if (!bloomOn)
            missing += " bloom";
        if (!profile.TryGet(out Vignette vignette) || !vignette.active)
            missing += " vignette";
        return GateResult.Of("light.finish.profile", Owner, missing.Length == 0,
            bloomOn ? $"bloom threshold {bloom.threshold.value:0.00}, intensity {bloom.intensity.value:0.00}" : "no bloom",
            missing.Length == 0 ? string.Empty : "Missing:" + missing);
    }

    /// <summary>BUG-25d: no diffuser is cut off by an old canvas' x limit.</summary>
    [GateCheck("light.hall.everyDiffuser", Owner)]
    public static GateResult EveryDiffuserCanLight()
    {
        bool cut = File.ReadAllText(DeepLayout).Contains("step(_CanvasSize.x*.36");
        return GateResult.Of("light.hall.everyDiffuser", Owner, !cut, cut ? "x < 36% cut present" : "no x cut",
            cut ? "HallDeepLayout still cuts the emitters at x < 36% (the left pier's diffuser never lights)." : string.Empty);
    }

    /// <summary>
    /// BUG-25a: the hall's night knobs are shader globals, so the slots drawn
    /// with the painting's material take its night (none in the Properties
    /// block, where a material value would hide them), and the shader's
    /// built-in night (before the knobs are published) is the shipped knob.
    /// </summary>
    [GateCheck("light.hall.nightIsShared", Owner)]
    public static GateResult HallNightIsShared()
    {
        string source = File.ReadAllText(DeepLayout);
        string properties = Regex.Match(source, @"Properties\s*\{(.*?)\n\s*\}", RegexOptions.Singleline).Groups[1].Value;
        bool hidden = Regex.IsMatch(properties, @"_Hall(NightTint|PoolColour|PoolStrength|EmissionGain|Fixture)");
        Match builtIn = Regex.Match(source, @"#define HALL_DEFAULT_NIGHT half3\(([^)]*)\)");
        HallLightingSO knobs = Knobs();
        // HallBakedLighting publishes the knob's linear value (the project is in linear colour space): compare with that.
        bool same = builtIn.Success && knobs != null && SameColour(builtIn.Groups[1].Value, knobs.hallNightTint.linear);
        return GateResult.Of("light.hall.nightIsShared", Owner, !hidden && same,
            $"built-in night ({builtIn.Groups[1].Value}), knob {(knobs != null ? knobs.hallNightTint.ToString() : "missing")} = linear {(knobs != null ? knobs.hallNightTint.linear.ToString("F4") : "missing")}",
            hidden ? "A night knob is in HallDeepLayout's Properties: the slots would draw with the material's value." : same ? string.Empty : "HallDeepLayout's built-in night differs from hallNightTint.");
    }

    /// <summary>BUG-73h: the four-state installer (which overwrote the shipped hall's materials and saved its scene) and its shader are gone.</summary>
    [GateCheck("light.hall.fourStateGone", Owner)]
    public static GateResult NoFourStateInstaller()
    {
        bool shader = Shader.Find("NOPE/Hall Four State") != null;
        bool baker = File.Exists("Assets/Editor/TerminalArt/HallFourStateBaker.cs");
        return GateResult.Of("light.hall.fourStateGone", Owner, !shader && !baker, $"shader: {shader}, baker: {baker}",
            shader || baker ? "The four-state shader (19 fetches a fragment, BUG-65h) or its installer (BUG-73h) is back." : string.Empty);
    }

    private static HallLightingSO Knobs() => AssetDatabase.LoadAssetAtPath<HallLightingSO>(HallLighting);

    private static string Clock(float hour)
    {
        int minutes = Mathf.RoundToInt(hour * 60f);
        return $"{minutes / 60:00}:{minutes % 60:00}";
    }

    private static bool SameColour(string hlsl, Color c)
    {
        string[] parts = hlsl.Split(',');
        if (parts.Length != 3)
            return false;
        float[] want = { c.r, c.g, c.b };
        for (int i = 0; i < 3; i++)
            if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) || Mathf.Abs(v - want[i]) > 0.002f)
                return false;
        return true;
    }
}
