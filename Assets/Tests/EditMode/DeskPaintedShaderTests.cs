using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// The painted desk's shader (NOPE/Desk Painted, DESK-ART; UNITY_PLAN 4.4), read
/// as source (its compile is Unity's): it consumes LIGHT's DeskLight seam and
/// never redefines it, relights the baked paint by the hour's cel band against
/// the painted one, takes the main light's cast shadow, reads the R/G/B mask,
/// has the three passes DeskAnime has, and keeps every material knob in the
/// UnityPerMaterial buffer (SRP Batcher).
/// </summary>
public class DeskPaintedShaderTests
{
    private const string ShaderPath = "Assets/Art/Office/DeskPainted/Shaders/DeskPainted.shader";

    private static string Source([CallerFilePath] string here = "")
    {
        string path = File.Exists(ShaderPath) ? ShaderPath : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", ShaderPath);
        return File.ReadAllText(path);
    }

    [Test]
    public void ItConsumesLightsSeam_AndNeverRedefinesIt()
    {
        string source = Source();
        StringAssert.Contains("Shader \"NOPE/Desk Painted\"", source);
        StringAssert.Contains("#include \"Assets/Shaders/Include/DeskLight.hlsl\"", source);
        StringAssert.Contains("* DeskLight() *", source, "the seam's multiplier on the relit paint");
        StringAssert.DoesNotMatch(@"half3\s+DeskLight\s*\(\s*\)\s*\{", source, "LIGHT owns DeskLight()");
        StringAssert.DoesNotContain("_DeskLightTint;", source, "LIGHT owns the globals");
    }

    [Test]
    public void ItRelightsByTheHoursBandAgainstThePaintedOne_AndTakesTheCastShadow()
    {
        string source = Source();
        StringAssert.Contains("Band(nlHour, width) / max(Band(nlRef, width)", source);
        StringAssert.Contains("lerp(1.0h, ratio, _Relight * mask.g)", source, "the time-of-day mask scales the relight");
        StringAssert.Contains("hour.shadowAttenuation", source, "a slid or lifted tool casts onto the plate");
        StringAssert.Contains("paint * mask.r * _Glow", source, "the glow mask");
        StringAssert.Contains("_Locked * mask.b", source, "the locked-home desaturation");
        StringAssert.Contains("_Relight(\"Relight with the hour\", Range(0, 1)) = 0.7", source, "UNITY_PLAN 4.4: lerp(..., 0.7)");
    }

    [Test]
    public void ItHasTheThreePasses_AndEveryKnobInThePerMaterialBuffer()
    {
        string source = Source();
        StringAssert.Contains("\"LightMode\" = \"UniversalForwardOnly\"", source);
        StringAssert.Contains("\"LightMode\" = \"ShadowCaster\"", source);
        StringAssert.Contains("\"LightMode\" = \"DepthOnly\"", source);
        string buffer = Regex.Match(source, @"CBUFFER_START\(UnityPerMaterial\)(.*?)CBUFFER_END", RegexOptions.Singleline).Groups[1].Value;
        foreach (string knob in new[] { "_BaseMap_ST", "_BaseColor", "_ShadowTint", "_GlowColor", "_RefLightDir", "_Relight", "_ShadeValue", "_BandThreshold", "_BandSoftness", "_ShadowReception", "_Glow", "_Locked", "_LockedDim" })
            StringAssert.Contains(knob, buffer, knob + " in UnityPerMaterial");
    }
}
