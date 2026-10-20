using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// LIGHT's shader seams: DeskAnime.shader's _PaintedHighlight and _InkRim
/// (STAMP sets them; declared in Properties and in the UnityPerMaterial
/// buffer, default 0 = off, read by the forward pass) and
/// Shaders/Include/DeskLight.hlsl (the desk's one light: the global tint,
/// level, key and lamp pool, DeskLight() falling back to 1 while nothing
/// publishes), which DeskAnime includes; DeskAnime samples no SSAO (BUG-16b);
/// HallDeepLayout.shader takes the hall's night knobs and fixtures as globals
/// (so the slots drawn with its material share the painting's night) with a
/// built-in night equal to the shipped knob; HallBakedLighting gates on no
/// material.
/// The text tests read the source; the "NeedsEditor" tests read the imported
/// shader and include, and run only inside Unity.
/// </summary>
public class LightSeamTests
{
    private const string DeskAnime = "Assets/Art/Office/DeskClean/DeskAnime.shader";
    private const string DeskLight = "Assets/Shaders/Include/DeskLight.hlsl";
    private static readonly string[] SeamProperties = { "_PaintedHighlight", "_InkRim" };

    [Test]
    public void DeskAnime_DeclaresTheSeamProperties_InPropertiesAndTheMaterialBuffer()
    {
        string source = File.ReadAllText(FromRoot(DeskAnime));
        string properties = Regex.Match(source, @"Properties\s*\{(.*?)\n\s*\}", RegexOptions.Singleline).Groups[1].Value;
        string buffer = Regex.Match(source, @"CBUFFER_START\(UnityPerMaterial\)(.*?)CBUFFER_END", RegexOptions.Singleline).Groups[1].Value;
        Assert.IsNotEmpty(properties, "the Properties block");
        Assert.IsNotEmpty(buffer, "the UnityPerMaterial buffer");
        foreach (string name in SeamProperties)
        {
            StringAssert.IsMatch(name + @"\(""[^""]*"",\s*Range\(0,\s*1\)\)\s*=\s*0\b", properties, $"{name} in Properties, default 0");
            StringAssert.IsMatch(@"\b" + name + @"\b", buffer, $"{name} in UnityPerMaterial");
            StringAssert.IsMatch(@"\*\s*" + name + @"\b|\b" + name + @"\s*\*", ForwardPass(source), $"{name} is read by the forward pass");
        }
    }

    [Test]
    public void DeskAnime_TakesTheDeskLight_AndNoSsao()
    {
        string source = File.ReadAllText(FromRoot(DeskAnime));
        string forward = ForwardPass(source);
        StringAssert.Contains("#include \"Assets/Shaders/Include/DeskLight.hlsl\"", forward);
        StringAssert.Contains("DeskLightAt(i.positionWS)", forward, "the energy comes from the desk light at the point (the lamp's pool)");
        StringAssert.DoesNotContain("_SCREEN_SPACE_OCCLUSION", source, "no SSAO keyword (BUG-16b)");
        StringAssert.DoesNotContain("GetScreenSpaceAmbientOcclusion", source);
    }

    [Test]
    public void DeskLight_DeclaresTheGlobals_AndFallsBackToOne_WhileNothingPublishes()
    {
        string source = File.ReadAllText(FromRoot(DeskLight));
        foreach (string global in new[] { "float4 _DeskLightTint;", "float _DeskLightLevel;", "float _DeskLightKey;", "float4 _DeskLampPool;", "float _DeskLampShare;" })
            StringAssert.Contains(global, source);
        StringAssert.Contains("return saturate(_DeskLightTint.a);", source, "published only while the tint's alpha is set");
        StringAssert.IsMatch(@"half3 DeskLight\(\)\s*\{\s*return lerp\(half3\(1, 1, 1\), _DeskLightTint\.rgb \* _DeskLightLevel, DeskLightPublished\(\)\);\s*\}", source);
        StringAssert.Contains("half3 DeskLightAt(float3 positionWS)", source);
    }

    [Test]
    public void HallDeepLayout_TakesTheNightKnobsAsGlobals_SoTheSlotsShareThePaintingsNight()
    {
        // BUG-25a: HallSlotsLink copies only the painting's states onto the slots' sprites (same material); the night
        // knobs and the fixtures are globals, so nothing needs copying, and no Properties default can hide them.
        string source = File.ReadAllText(FromRoot(HallDeepLayout));
        string properties = Regex.Match(source, @"Properties\s*\{(.*?)\n\s*\}", RegexOptions.Singleline).Groups[1].Value;
        Assert.IsNotEmpty(properties, "the Properties block");
        StringAssert.DoesNotMatch(@"_Hall|_NightTint|_PoolColour|_PoolStrength|_EmissionGain", properties);
        foreach (string global in new[] { "float _HallFixtureLevels[16];", "float4 _HallFixtureCenters[16];", "float4 _HallFixturePools[16];", "float _HallFixtureCount;",
                                          "float4 _HallNightTint,_HallPoolColour;float _HallPoolStrength,_HallEmissionGain;" })
            StringAssert.Contains(global, source);
        StringAssert.DoesNotContain("_ScheduledFixtureCount", source, "the old per-renderer count is gone");

        // Before the knobs are published the shader's built-in night is the shipped knob.
        string builtIn = Regex.Match(source, @"#define HALL_DEFAULT_NIGHT half3\(([^)]*)\)").Groups[1].Value;
        Match knob = Regex.Match(File.ReadAllText(FromRoot(HallLightingAsset)), @"hallNightTint: \{r: ([0-9.]+), g: ([0-9.]+), b: ([0-9.]+)");
        Assert.IsTrue(knob.Success, "hallNightTint in the asset");
        string[] parts = builtIn.Split(',');
        Assert.AreEqual(3, parts.Length, builtIn);
        for (int i = 0; i < 3; i++)
            Assert.AreEqual(float.Parse(knob.Groups[i + 1].Value, System.Globalization.CultureInfo.InvariantCulture),
                            float.Parse(parts[i].Trim(), System.Globalization.CultureInfo.InvariantCulture), 0.005f, $"channel {i}");
    }

    [Test]
    public void HallBakedLighting_DoesNotGateOnAMaterial()
    {
        // The art scene's material reference pointed at the deleted four-state shader's material (BUG-73h): no gate on it.
        string source = File.ReadAllText(FromRoot("Assets/Scripts/Office/HallBakedLighting.cs"));
        StringAssert.DoesNotContain("Material material", source);
        StringAssert.DoesNotContain("material == null", source);
        StringAssert.DoesNotContain("public void Configure(", source, "no caller since the four-state baker went");
    }

    private const string HallDeepLayout = "Assets/Shaders/HallDeepLayout.shader";
    private const string HallLightingAsset = "Assets/Data/Config/HallLighting_Default.asset";

    /// <summary>The DeskAnimeForward pass's text (from its name to the next pass).</summary>
    private static string ForwardPass(string source)
    {
        int start = source.IndexOf("Name \"DeskAnimeForward\"", System.StringComparison.Ordinal);
        int end = source.IndexOf("Name \"ShadowCaster\"", System.StringComparison.Ordinal);
        Assert.That(start >= 0 && end > start, "the forward pass");
        return source.Substring(start, end - start);
    }

    [Test, Category("NeedsEditor")]
    public void DeskAnime_ImportsWithTheSeamProperties()
    {
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(DeskAnime);
        Assert.IsNotNull(shader, DeskAnime);
        Assert.IsFalse(ShaderUtil.ShaderHasError(shader), "DeskAnime compiles");
        foreach (string name in SeamProperties)
        {
            int index = shader.FindPropertyIndex(name);
            Assert.GreaterOrEqual(index, 0, name);
            Assert.AreEqual(0f, shader.GetPropertyDefaultFloatValue(index), name);
        }
    }

    [Test, Category("NeedsEditor")]
    public void DeskLight_ImportsAsAShaderInclude()
    {
        Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(DeskLight), DeskLight);
    }

    /// <summary>A project file, found from this file's folder (so it is this checkout's under Unity and outside it alike).</summary>
    private static string FromRoot(string projectPath, [CallerFilePath] string here = "") =>
        Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", projectPath);
}
