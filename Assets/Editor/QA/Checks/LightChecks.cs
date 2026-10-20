using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// LIGHT's standing checks for the demo gate (FIX_PLAN LIGHT; QA-RIG's
/// DemoGate registers <see cref="Failures"/> once it lands): the knobs and
/// assets that keep the one lighting model in place. Each returns a message
/// per failure (none: green). They read assets and sources only (no scene,
/// no play), so they run in the editor's EditMode suite (LightChecksTests).
/// </summary>
public static class LightChecks
{
    /// <summary>The hall's lighting knobs.</summary>
    public const string HallLighting = "Assets/Data/Config/HallLighting_Default.asset";

    /// <summary>The office renderer's SSAO feature (cut, BUG-16b).</summary>
    public const string ContactShading = "Assets/Settings/OfficeContactShading.asset";

    /// <summary>The painted hall's layer shader.</summary>
    public const string DeepLayout = "Assets/Shaders/HallDeepLayout.shader";

    /// <summary>Every check's failures, in order (empty: all green).</summary>
    public static List<string> Failures()
    {
        var failures = new List<string>();
        failures.AddRange(SsaoIsCut());
        failures.AddRange(DeskStaysReadableAtNight());
        failures.AddRange(LampSwitchesOnAtTheDip());
        failures.AddRange(FinishHasBloomGradeAndVignette());
        failures.AddRange(EveryDiffuserCanLight());
        failures.AddRange(NoFourStateInstaller());
        return failures;
    }

    /// <summary>BUG-16b: the blue-noise SSAO (re-seeded every frame in the player) is off.</summary>
    public static IEnumerable<string> SsaoIsCut()
    {
        var feature = AssetDatabase.LoadAssetAtPath<ScriptableRendererFeature>(ContactShading);
        if (feature != null && feature.isActive)
            yield return $"LIGHT-SSAO: {ContactShading} is active (BUG-16b: its noise crawls in the player).";
    }

    /// <summary>BUG-14a: the desk's night level (floor plus lamp) keeps the mat readable.</summary>
    public static IEnumerable<string> DeskStaysReadableAtNight()
    {
        HallLightingSO knobs = Knobs();
        if (knobs == null)
        {
            yield return $"LIGHT-KNOBS: {HallLighting} is missing.";
            yield break;
        }
        float night = DeskLighting.NightLevel(knobs.DeskKnobs);
        if (night < DeskLighting.ReadableNightLevel)
            yield return $"LIGHT-NIGHT: the desk's night level {night:0.00} is below {DeskLighting.ReadableNightLevel:0.00} (the mat reads at about luma 55-60).";
    }

    /// <summary>BUG-14a: the work lamp switches on in the dusk (the afternoon dip), before the sunset.</summary>
    public static IEnumerable<string> LampSwitchesOnAtTheDip()
    {
        HallLightingSO knobs = Knobs();
        if (knobs == null)
            yield break;
        HallDayCycle.Settings cycle = knobs.Cycle;
        float dip = DeskLighting.LampOnHour(cycle, knobs.DeskKnobs);
        float from = HallDayCycle.HourAtEvening(0f, cycle), sunset = HallDayCycle.HourAtEvening(0.5f, cycle);
        if (dip < from - 1e-3f || dip > sunset + 1e-3f)
            yield return $"LIGHT-LAMP: the lamp switches on at {dip:0.00}, outside the dusk's first half ({from:0.00}-{sunset:0.00}).";
    }

    /// <summary>BUG-51: the office finish has tonemapping, a grade, bloom and a vignette.</summary>
    public static IEnumerable<string> FinishHasBloomGradeAndVignette()
    {
        HallLightingSO knobs = Knobs();
        VolumeProfile profile = knobs != null ? knobs.finishProfile : null;
        if (profile == null)
        {
            yield return "LIGHT-FINISH: HallLighting_Default has no finish profile (BUG-51).";
            yield break;
        }
        if (!profile.TryGet(out Tonemapping tonemapping) || !tonemapping.active)
            yield return "LIGHT-FINISH: no tonemapping.";
        if (!profile.TryGet(out ColorAdjustments grade) || !grade.active)
            yield return "LIGHT-FINISH: no grade (Color Adjustments).";
        if (!profile.TryGet(out Bloom bloom) || !bloom.active || bloom.intensity.value <= 0f)
            yield return "LIGHT-FINISH: no bloom (portals, signs and lit diffusers must glow).";
        if (!profile.TryGet(out Vignette vignette) || !vignette.active)
            yield return "LIGHT-FINISH: no vignette.";
    }

    /// <summary>BUG-25d: no diffuser is cut off by an old canvas' x limit.</summary>
    public static IEnumerable<string> EveryDiffuserCanLight()
    {
        string source = File.ReadAllText(DeepLayout);
        if (source.Contains("step(_CanvasSize.x*.36"))
            yield return "LIGHT-PIER: HallDeepLayout still cuts the emitters at x < 36% (the left pier's diffuser never lights).";
    }

    /// <summary>BUG-73h: the four-state installer (which overwrote the shipped hall's materials and saved its scene) and its shader are gone.</summary>
    public static IEnumerable<string> NoFourStateInstaller()
    {
        if (Shader.Find("NOPE/Hall Four State") != null)
            yield return "LIGHT-FOURSTATE: the unused NOPE/Hall Four State shader is back (19 fetches a fragment, BUG-65h).";
        if (File.Exists("Assets/Editor/TerminalArt/HallFourStateBaker.cs"))
            yield return "LIGHT-FOURSTATE: HallFourStateBaker can overwrite the shipped hall again (BUG-73h).";
    }

    private static HallLightingSO Knobs() => AssetDatabase.LoadAssetAtPath<HallLightingSO>(HallLighting);
}
