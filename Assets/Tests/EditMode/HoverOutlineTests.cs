using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// The hover outline (the review of the desk v3 capture, S2 11; the code
/// review of round 6): one rim for every object that fades in and out over
/// about four frames (HoverFade; a cut under Reduced Motion), never
/// depth-tested against the desk and never over the object itself, thin parts
/// included: TimeDesk/HoverHull masks the silhouette into the stencil, then
/// draws the pushed-out hull where the mask is not, with no depth test and no
/// pull toward the camera. The shader's tests read its source; Unity's
/// EditMode suite compiles it (DeskV3InstallTests).
/// </summary>
public class HoverOutlineTests
{
    private const float Frame = 1f / 60f, Fade = 0.07f;

    [Test]
    public void TheRim_FadesInOverAboutFourFrames_AndOutFromWhereItStood_NeverPopping()
    {
        float shown = 0f;
        int frames = 0;
        while (shown < 1f && frames < 100)
        {
            float next = HoverFade.Step(shown, 1f, Frame, Fade, false);
            Assert.Greater(next, shown, $"frame {frames}: it grows");
            Assert.LessOrEqual(next - shown, Frame / Fade + 1e-6f, $"frame {frames}: by at most a frame's share");
            shown = next;
            frames++;
        }
        Assert.AreEqual(5, frames, "70 ms at 60 fps: in on the 5th frame (4.2 frames), never at once");
        Assert.AreEqual(1f, HoverFade.Step(1f, 1f, Frame, Fade, false), "held at full");

        float leaving = 0.6f;
        float after = HoverFade.Step(leaving, 0f, Frame, Fade, false);
        Assert.AreEqual(0.6f - Frame / Fade, after, 1e-5f, "the rim left fades out from where it stood");
        for (int i = 0; i < 10; i++)
            after = HoverFade.Step(after, 0f, Frame, Fade, false);
        Assert.AreEqual(0f, after, "gone, never below");
    }

    [Test]
    public void UnderReducedMotion_OrWithNoFadeTime_TheRimIsACut()
    {
        Assert.AreEqual(1f, HoverFade.Step(0f, 1f, Frame, Fade, true), "Reduced Motion: on at once");
        Assert.AreEqual(0f, HoverFade.Step(0.7f, 0f, Frame, Fade, true), "and off at once");
        Assert.AreEqual(1f, HoverFade.Step(0.2f, 1f, Frame, 0f, false), "no fade time: a cut");
        Assert.AreEqual(0.3f, HoverFade.Step(0.3f, 1f, 0f, Fade, false), "no time passed: as it stood");
        Assert.AreEqual(1f, HoverFade.Step(0.99f, 1f, 1f, Fade, false), "a long frame: never past full");
    }

    [Test]
    public void TheHull_IsNeverDepthTested_AndDrawsOnlyWhereTheMaskIsNot_WithNoPullTowardTheCamera()
    {
        string source = Source("Assets/Art/Office/Gameplay/Shaders/HoverHull.shader");
        string pass = Regex.Match(source, @"Pass\s*\{(.*?)HLSLPROGRAM", RegexOptions.Singleline).Groups[1].Value;
        Assert.IsNotEmpty(pass, "the pass's render state");
        StringAssert.IsMatch(@"ZTest\s+Always", pass, "never depth-tested against the desk under it (the felt and the shell cut the old rim)");
        StringAssert.IsMatch(@"ZWrite\s+Off", pass);
        StringAssert.IsMatch(@"Cull\s+Off", pass, "both faces: an open mesh's silhouette is masked and rimmed too");
        StringAssert.IsMatch(@"ColorMask\s+\[_ColorMask\]", pass, "the mask writes no colour");
        string stencil = Regex.Match(pass, @"Stencil\s*\{(.*?)\}", RegexOptions.Singleline).Groups[1].Value;
        StringAssert.IsMatch(@"Ref\s+8\b", stencil, "its own stencil bit, value 8 (bit 3: one of the user bits 0 to 3 URP never writes)");
        StringAssert.IsMatch(@"ReadMask\s+8\b", stencil);
        StringAssert.IsMatch(@"WriteMask\s+8\b", stencil);
        StringAssert.IsMatch(@"Comp\s+\[_StencilComp\]", stencil, "the hull tests the bit (NotEqual), the mask always passes");
        StringAssert.IsMatch(@"Pass\s+\[_StencilPass\]", stencil, "the mask sets the bit (Replace), the hull keeps it");
        StringAssert.IsMatch(@"_StencilComp\s*\(""[^""]*"",\s*Float\)\s*=\s*6", source, "the material's default is the hull's NotEqual");
        StringAssert.DoesNotContain("_Pull", source, "no pull toward the camera (it filled parts thinner than it with the rim's colour)");
        StringAssert.IsMatch(@"if\s*\(_Mask\s*>\s*0\.5\)\s*\{\s*output\.positionCS\s*=\s*TransformWorldToHClip\(positionWS\);\s*return output;", source, "the mask is the mesh as it is, never pushed out");
        StringAssert.IsMatch(@"_Mask\s*\(""[^""]*"",\s*Float\)\s*=\s*0", source, "the material's default is the hull");
    }

    /// <summary>A file of the project, from the tests' working folder or this file's.</summary>
    private static string Source(string relative, [CallerFilePath] string here = "") =>
        File.ReadAllText(File.Exists(relative) ? relative : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", relative));
}
