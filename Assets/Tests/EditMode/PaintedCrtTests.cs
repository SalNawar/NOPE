using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// The painted PC's CRT (desk v3, round 12; the concept-vs-game review, items
/// 1 and 2, and Saleh's crops of the monitor): the curved glass set into the
/// bezel, the recess that closes the casing's open front (the hall showed
/// through it), the picture filling the glass (no tablet border), the CRT
/// glass's scanlines, and the desk view of the screen (one readable status
/// line, the clock and date in a readout strip, the old clock plate hidden,
/// the full desktop unchanged with the PC open). The geometry is checked as
/// numbers (CrtFace is pure); the wiring as source text (Unity runs the rest:
/// DESK-V3.md, R12).
/// </summary>
public class PaintedCrtTests
{
    private static string Read(string relative, [CallerFilePath] string here = "")
    {
        string path = File.Exists(relative) ? relative : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", relative);
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    private static readonly CrtFace.Shape Shape = CrtFace.Painted;

    [Test]
    public void TheDome_FacesTheChair_ItsRimInsideTheBezel_ItsCentreProud()
    {
        (CrtFace.Vertex[] v, int[] t) = CrtFace.Dome(Shape);
        Assert.Greater(t.Length, 0);
        for (int i = 0; i < t.Length; i += 3)
        {
            CrtFace.Vertex a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
            float z = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
            Assert.Less(z, 0f, "every triangle is a front face toward the chair (-z), so the clone's culled shader draws it");
        }
        Assert.AreEqual(Shape.RimDepth - Shape.Bulge, v[0].Z, 1e-6f, "the centre stands Bulge in front of the rim");
        float rim = v.Skip(v.Length - 64).Max(x => x.Z);
        Assert.AreEqual(Shape.RimDepth, rim, 1e-6f);
        Assert.Greater(Shape.RimDepth, CrtFace.BezelFaceDepth, "the rim sits under the bezel's face: the glass is set into the casing, not floating on it");
        Assert.Less(v[0].Z, 0f, "the middle of the glass stands proud of the old screen face: a CRT's curve");
    }

    [Test]
    public void TheDome_HasRoundedCorners_InsideItsRectangle()
    {
        (CrtFace.Vertex[] v, _) = CrtFace.Dome(Shape);
        float hw = Shape.Width / 2f, hh = Shape.Height / 2f, r = Shape.Corner;
        foreach (CrtFace.Vertex p in v)
        {
            Assert.LessOrEqual(Math.Abs(p.X), hw + 1e-5f);
            Assert.LessOrEqual(Math.Abs(p.Y), hh + 1e-5f);
            float dx = Math.Abs(p.X) - (hw - r), dy = Math.Abs(p.Y) - (hh - r);
            if (dx > 0f && dy > 0f)
                Assert.LessOrEqual(Math.Sqrt(dx * dx + dy * dy), r + 1e-4f, "a corner point lies on its rounded corner, not the square one");
        }
        Assert.That(v.Min(p => p.U), Is.EqualTo(0f).Within(0.01f));
        Assert.That(v.Max(p => p.U), Is.EqualTo(1f).Within(0.01f));
    }

    [Test]
    public void TheVisibleGlass_IsThePaintingsGlass()
    {
        float t = CrtFace.VisibleT(Shape);
        Assert.That(Shape.Width * t, Is.EqualTo(0.36f).Within(0.01f), "the bezel's face meets the dome at the painted glass's 36 cm");
        Assert.That(Shape.Height * t, Is.EqualTo(0.29f).Within(0.01f), "and its 29 cm");
    }

    [Test]
    public void ThePicture_FillsTheGlass_At4By3_WithACrtsOverscan()
    {
        const float fill = 0.92f, overscan = 0.02f;
        (float w, float h) = CrtFace.PictureBounds(Shape, fill, overscan);
        Assert.That(w / h, Is.EqualTo(4f / 3f).Within(1e-4f), "the bounds are 4:3, so the clone's fit takes all of them");
        Assert.GreaterOrEqual(w * fill, Shape.Width * (1f + overscan) - 1e-5f, "no black border at the sides");
        Assert.GreaterOrEqual(h * fill, Shape.Height * (1f + overscan) - 1e-5f, "no black border at the top or the bottom");
        (float sx, float sy) = CrtFace.VisibleShare(Shape, fill, overscan);
        Assert.That(sx, Is.InRange(0.8f, 0.95f), "the desk view keeps its content in the middle that shows");
        Assert.That(sy, Is.InRange(0.85f, 0.99f));
    }

    [Test]
    public void TheRecess_ClosesTheCasingsOpenFront_BehindTheBezel()
    {
        // Desk_PC.fbx (round 12): the casing's front opening x +-0.23, y -0.205 to 0.195, its rim 2.7 cm behind the screen face; the bezel slab's back 2.4 cm.
        Assert.GreaterOrEqual(CrtFace.RecessHalfWidth, 0.23f);
        Assert.LessOrEqual(CrtFace.RecessBottom, -0.205f);
        Assert.GreaterOrEqual(CrtFace.RecessTop, 0.195f);
        Assert.That(CrtFace.RecessDepth, Is.InRange(0.024f, 0.027f), "between the bezel's back and the casing's rim");
    }

    [Test]
    public void TheBinder_FitsTheCrt_BeforeTheCloneMeasuresIt_AndShowsTheDeskView()
    {
        string binder = Read("Assets/Scripts/Office/OfficeSceneBinder.cs");
        int fit = binder.IndexOf("PaintedCrt.Fit(paintedDesk.Screen", StringComparison.Ordinal);
        int bind = binder.IndexOf("screenClone.Bind(paintedDesk.Screen, 0, office);", StringComparison.Ordinal);
        Assert.That(fit, Is.GreaterThan(0));
        Assert.That(bind, Is.GreaterThan(fit), "the glass's bounds are the clone's to measure, so the face is fitted first");
        StringAssert.Contains("PaintedCrtReadout.Create(DesktopCanvas(), frame, visible);", binder);
        StringAssert.Contains("if (BindCalendarOnReadout(calendar, viewer))", binder, "the calendar's click lies on the readout's date, the old plate hidden");
    }

    [Test]
    public void TheDeskView_HidesWithThePcOpen_AndTheOldPlate_AndWritesOnlyOnAChange()
    {
        string readout = Read("Assets/Scripts/Office/Desk/PaintedCrtReadout.cs");
        StringAssert.Contains("bool shown = _frame == null || !_frame.IsOpen;", readout, "the full desktop, unchanged, with the PC open");
        StringAssert.Contains("_oldPlate.SetActive(false);", readout, "no floating clock box over the glass");
        StringAssert.Contains("if (!ReferenceEquals(status, _shownStatus))", readout);
        StringAssert.Contains("if (minute != _shownMinute)", readout);
        StringAssert.Contains("if (day != _shownDay)", readout);
        StringAssert.Contains("_status = Text(band.transform, \"Status\", new Rect(0.04f, 0.08f, 0.92f, 0.84f), font, 132f", readout,
                              "the status line at up to 132 of the desktop's 1080 (about 20 px on the 1080p glass), not the desktop's 80");
    }

    [Test]
    public void TheGlass_HasItsMaterial_InResources_OnTheCrtShader_WithScanlinesThatNeverMoire()
    {
        string shader = Read("Assets/Art/Office/DeskPainted/Shaders/DeskCrtGlass.shader");
        StringAssert.Contains("Shader \"NOPE/Desk CRT Glass\"", shader);
        StringAssert.Contains("float perPixel = fwidth(lines);", shader, "the lines fade out where they would be finer than about two pixels");
        StringAssert.Contains("Blend DstColor Zero", shader);
        string guid = Regex.Match(Read("Assets/Art/Office/DeskPainted/Shaders/DeskCrtGlass.shader.meta"), @"guid: ([0-9a-f]{32})").Groups[1].Value;
        string material = Read("Assets/Art/Office/DeskPainted/Resources/DeskCrtGlass.mat");
        StringAssert.Contains("m_Shader: {fileID: 4800000, guid: " + guid + ", type: 3}", material, "the material is on the CRT shader (and in Resources, so a build has it)");
        StringAssert.Contains("Resources.Load<Material>(GlassMaterial)", Read("Assets/Scripts/Office/Desk/PaintedCrt.cs"));
    }
}
