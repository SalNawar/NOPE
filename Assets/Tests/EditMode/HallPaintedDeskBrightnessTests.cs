using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// The hall's brightness under the painted desk (desk v3, DESK-V3 round 9):
/// measured against the approved painting with the desk matching it, the lit
/// hall read about 1.4x the painting's at 09:00 and 1.15 to 1.3x at 16:00, so
/// LIGHT's knob (HallLightingSO.paintedDeskBrightness) draws it at 0.72 and
/// 0.85 of its sRGB values while the painted desk is installed, and at 1
/// otherwise. Read as source and data (the curve's evaluation and the shaders'
/// compile are Unity's): each check fails when its part is reverted.
/// </summary>
public class HallPaintedDeskBrightnessTests
{
    private static string Read(string relative, [CallerFilePath] string here = "")
    {
        string path = File.Exists(relative) ? relative : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", relative);
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    [Test]
    public void TheKnobsAsset_DrawsTheHallAt072At0900_And085At1600()
    {
        string asset = Read("Assets/Data/Config/HallLighting_Default.asset");
        Match curve = Regex.Match(asset, @"\n  paintedDeskBrightness:\n(.*?)\n    m_PreInfinity: (\d+)\n    m_PostInfinity: (\d+)", RegexOptions.Singleline);
        Assert.IsTrue(curve.Success, "HallLighting_Default.asset carries the painted desk's brightness");
        MatchCollection keys = Regex.Matches(curve.Groups[1].Value, @"time: ([0-9.]+)\n\s+value: ([0-9.]+)");
        Assert.AreEqual(2, keys.Count, "two keys: 09:00 and 16:00, a line between");
        Assert.AreEqual(9f, F(keys[0].Groups[1].Value), 1e-4f);
        Assert.AreEqual(0.72f, F(keys[0].Groups[2].Value), 1e-4f, "09:00: the hall read 1.39x the painting's (1 / 1.39)");
        Assert.AreEqual(16f, F(keys[1].Groups[1].Value), 1e-4f);
        Assert.AreEqual(0.85f, F(keys[1].Groups[2].Value), 1e-4f, "16:00: the hall read 1.15 to 1.3x the painting's");
        Assert.AreEqual(curve.Groups[2].Value, curve.Groups[3].Value, "the hours outside keep their end's value on both sides");
    }

    [Test]
    public void TheKnobsDefault_IsTheAssetsLine()
    {
        string source = Read("Assets/Scripts/Office/HallLightingSO.cs");
        StringAssert.Contains("public AnimationCurve paintedDeskBrightness = AnimationCurve.Linear(9f, 0.72f, 16f, 0.85f);", source);
    }

    [Test]
    public void TheBrightness_AppliesOnlyUnderThePaintedDesk()
    {
        string rig = Read("Assets/Scripts/Office/HallLightingRig.cs");
        StringAssert.Contains("backdrop.Brightness = PaintedDesk && painted != null && painted.length > 0 ? painted.Evaluate(Hour) : 1f;", rig,
                              "the rig draws the hall at the knob's value for the hour only under the painted desk, else as lit");
        string binder = Read("Assets/Scripts/Office/OfficeSceneBinder.cs");
        StringAssert.Contains("lights.PaintedDesk = _painted;", binder, "the office binder says when the painted desk is installed");
    }

    [Test]
    public void TheFullScreenDraw_ScalesTheHallsSrgbValues_AndPassesItUntouchedAt1()
    {
        string shader = Read("Assets/Art/Office/Gameplay/Shaders/HallBackdrop.shader");
        StringAssert.Contains("_Brightness (\"Brightness on screen (the sRGB values' scale; 1: as lit)\", Range(0, 2)) = 1", shader, "a fresh material draws the hall as lit");
        string buffer = Regex.Match(shader, @"CBUFFER_START\(UnityPerMaterial\)(.*?)CBUFFER_END", RegexOptions.Singleline).Groups[1].Value;
        StringAssert.Contains("float _Brightness;", buffer, "the knob in UnityPerMaterial (SRP Batcher)");
        StringAssert.Contains("hall = SRGBToLinear(LinearToSRGB(hall) * _Brightness);", shader, "the scale is on what the screen shows (the review measured sRGB values), in linear colour space");
        StringAssert.Contains("if (_Brightness != 1.0)", shader, "at 1 the picture is not round-tripped");
        string backdrop = Read("Assets/Scripts/Office/HallBackdrop.cs");
        StringAssert.Contains("_material.SetFloat(BrightnessId, _brightness);", backdrop, "the runtime material takes the brightness when it is built and when it changes");
    }

    [Test]
    public void TheForegroundFloorsSeam_ContinuesTheHallAtTheSameBrightness()
    {
        string floor = Read("Assets/Scripts/Office/HallForegroundFloor.cs");
        StringAssert.Contains("Shader.SetGlobalFloat(BackdropBrightness,backdrop.Brightness);", floor, "set with the lit hall's texture");
        string shader = Read("Assets/Shaders/HallForegroundFloor.shader");
        StringAssert.Contains("colour=(half3)SRGBToLinear(LinearToSRGB((float3)colour)*_HallFloorBackdropBrightness);", shader);
        int branch = shader.IndexOf("if(_HallFloorHasBackdrop>0.5)", System.StringComparison.Ordinal);
        int scale = shader.IndexOf("_HallFloorBackdropBrightness!=1", System.StringComparison.Ordinal);
        Assert.That(branch, Is.GreaterThan(0));
        Assert.That(scale, Is.GreaterThan(branch), "only where the floor samples the lit hall (the global is set with its texture)");
    }

    private static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);
}
