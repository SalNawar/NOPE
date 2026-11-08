using System;
using NUnit.Framework;

/// <summary>
/// The readout line on the painted chip reader (PaintedReadout; desk v3
/// round 12's S1, the night of 2026-10-28, seam D): it stands on the top
/// face, inside it, on its near third, leant toward the chair, and its click
/// box covers the line above the body's padded box, so the line is clicked
/// before the body. Its type may fill the line, so it reads from the chair
/// (the code review's R3 of 2026-10-28: READY was about 21 x 3 px at 1080p).
/// The line is a VFD window (item 15 of 2026-11-07): the glass is the line,
/// the bezel runs round it, the text sits inside the glass, and the window
/// stands clear of the top face and of the painted card slot line.
/// </summary>
public class PaintedReadoutTests
{
    private static readonly PaintedReadout.Box Reader = new PaintedReadout.Box(0.79f, 1.01f, 1.08f, 1.16f, 0.27f, 0.43f);

    /// <summary>
    /// The painted chip reader as the bake lays it, in the painted desk's
    /// frame with y over the desk top (DeskObjects.fbx's Desk_ChipReader: x
    /// 0.84 to 1.10, y 1.105 to 1.194 in the world over the desk top at 1.069,
    /// z -0.532 to -0.27; desk_layout_v3.json's chipReader rect).
    /// </summary>
    private static readonly PaintedReadout.Box PaintedReader = new PaintedReadout.Box(0.84f, 1.10f, 0.036f, 0.125f, -0.532f, -0.27f);

    /// <summary>The office camera's vertical field of view at 16:9 and its pitch down (degrees; DeskLayoutSettings.officeFieldOfView, DeskHomeMap.OfficeEye's "58 degrees, 9 down").</summary>
    private const float OfficeFieldOfView = 58f, OfficePitch = 9f;

    /// <summary>The old reader's auto-size cap (OfficeSceneUIBuilder.Machines: fontSizeMax 0.12), which the painted line kept before R3.</summary>
    private const float OldFontSizeMax = 0.12f;

    [Test]
    public void TheLine_StandsOnTheTopFace_OnItsNearThird_InsideIt()
    {
        (float x, float y, float z) = PaintedReadout.LinePosition(Reader);
        (float nx, float ny, float nz) = PaintedReadout.NearEdge(Reader);
        (float fx, float fy, float fz) = PaintedReadout.FarEdge(Reader);
        (float width, float height) = PaintedReadout.LineSize(Reader);
        Assert.AreEqual(Reader.MaxY + PaintedReadout.Lift, ny, 1e-6f, "its near edge over the top face by the lift");
        Assert.Greater(fy, ny, "its far edge raised: the line leans toward the chair");
        Assert.AreEqual(Reader.CentreX, x, 1e-6f, "centred across");
        Assert.AreEqual(nx, fx, 1e-6f);
        Assert.AreEqual((ny + fy) / 2f, y, 1e-6f, "its centre half way up the lean");
        Assert.AreEqual((nz + fz) / 2f, z, 1e-6f);
        Assert.Less(z, Reader.CentreZ, "on the near half (toward the chair)");
        Assert.GreaterOrEqual(nz, Reader.MinZ, "its near edge over the face");
        Assert.LessOrEqual(fz, Reader.MaxZ, "its far edge over the face");
        Assert.LessOrEqual(width, Reader.Width, "no wider than the face");
        Assert.GreaterOrEqual(width, 0.6f * Reader.Width, "over half the face's width: a readable line, kept inside the body in the reading view (item 15: the leant window's parallax)");
        Assert.AreEqual(height, Distance((nx, ny, nz), (fx, fy, fz)), 1e-5f, "the line keeps its height up the lean");
    }

    [Test]
    public void TheLine_FacesUpAndTowardTheChair_ItsTopAwayFromTheChair()
    {
        var (forward, up) = PaintedReadout.LineFacing();
        // A TextMeshPro shows on its -Z: the side it shows faces up and toward the chair (-z).
        Assert.Less(-forward.y, 1f, "not flat: leant");
        Assert.Greater(-forward.y, 0f, "it faces up");
        Assert.Less(-forward.z, 0f, "and toward the chair");
        Assert.AreEqual(PaintedReadout.Tilt, (float)(Math.Acos(-forward.y) * 180.0 / Math.PI), 1e-3f, "leant by Tilt");
        Assert.Greater(up.z, 0f, "its top edge away from the chair: it reads from the chair");
        Assert.AreEqual(0f, forward.x * up.x + forward.y * up.y + forward.z * up.z, 1e-6f, "forward and up are square");
        (_, float ny, float nz) = PaintedReadout.NearEdge(Reader);
        (_, float fy, float fz) = PaintedReadout.FarEdge(Reader);
        (_, float height) = PaintedReadout.LineSize(Reader);
        Assert.AreEqual(up.y, (fy - ny) / height, 1e-5f, "up runs from the near edge to the far edge");
        Assert.AreEqual(up.z, (fz - nz) / height, 1e-5f);
    }

    [Test]
    public void TheClickBox_CoversTheLine_AboveTheBodysPaddedBox()
    {
        (float cx, float cy, float cz) = PaintedReadout.BoxCentre(Reader);
        (float sx, float sy, float sz) = PaintedReadout.BoxSize(Reader);
        (float lx, _, float lz) = PaintedReadout.LinePosition(Reader);
        (_, float ny, float nz) = PaintedReadout.NearEdge(Reader);
        (_, float fy, float fz) = PaintedReadout.FarEdge(Reader);
        (float width, _) = PaintedReadout.LineSize(Reader);
        Assert.AreEqual(lx, cx, 1e-6f);
        Assert.AreEqual(lz, cz, 1e-6f);
        Assert.AreEqual(width, sx, 1e-6f, "the line's width");
        Assert.AreEqual(fz - nz, sz, 1e-6f, "the line's footprint along the depth");
        Assert.AreEqual(ny, cy - sy / 2f, 1e-6f, "it reaches down to the line's near edge");
        Assert.AreEqual(fy + PaintedReadout.BoxRise, cy + sy / 2f, 1e-6f, "and over its far edge by the margin");
        Assert.Greater(cy + sy / 2f - Reader.MaxY, DeskHomeMap.ClickPad, "its top over the body's padded box (DeskPainted.EnsureClickBox pads by ClickPad), so the ray meets the readout's box first");
    }

    /// <summary>
    /// The code review's R3 (2026-10-28): the line's type kept the old
    /// reader's cap, an em of 1.2 cm, about 3 px high from the chair at
    /// 1080p. The type may now fill the line's height, and the leant line
    /// shows taller from the chair than flat.
    /// </summary>
    [Test]
    public void TheLargestType_FillsTheLine_AndReadsFromTheChairAt1080p()
    {
        (float width, float height) = PaintedReadout.LineSize(PaintedReader);
        float size = PaintedReadout.FontSizeMax(PaintedReader);
        Assert.AreEqual(PaintedReadout.TextSize(PaintedReader).height, size * PaintedReadout.EmPerFontSize, 1e-6f, "the largest em is the text's height inside the glass");
        Assert.Greater(size * PaintedReadout.EmPerFontSize, 0.8f * height, "most of the line's height: the pad takes little");
        Assert.Greater(size, OldFontSizeMax * 5f, "far over the old reader's cap");

        float line = ScreenHeight(PaintedReadout.NearEdge(PaintedReader), PaintedReadout.FarEdge(PaintedReader));
        Assert.GreaterOrEqual(line, 32f, $"the line {line:0.0} px high from the chair at 1080p (the largest em; at least 32 px)");
        float old = line * OldFontSizeMax * PaintedReadout.EmPerFontSize / height;
        Assert.Less(old, 8f, $"the old cap's em {old:0.0} px: the review's smear");

        (float nx, float ny, float nz) = PaintedReadout.NearEdge(PaintedReader);
        float flat = ScreenHeight((nx, ny, nz), (nx, ny, nz + height));
        Assert.Greater(line, 1.4f * flat, $"leant {line:0.0} px against flat {flat:0.0} px: the lean shows the line taller from the chair");

        float wide = ScreenWidth((nx - width / 2f, ny, nz), (nx + width / 2f, ny, nz));
        Assert.GreaterOrEqual(wide, 80f, $"the line {wide:0} px wide from the chair at 1080p");
    }

    /// <summary>
    /// Saleh's playtest of 2026-11-07, item 15: the line is a VFD window.
    /// The glass is the line's size, the text sits inside it by the pad, the
    /// bezel runs round it a step behind, and the window stays over the
    /// face, past the painted card slot line and short of the lamp's rim.
    /// </summary>
    [Test]
    public void TheWindow_HoldsTheTextInItsGlass_InsideItsBezel_ClearOfTheFaceAndTheSlot()
    {
        (float width, float height) = PaintedReadout.LineSize(PaintedReader);
        (float tw, float th) = PaintedReadout.TextSize(PaintedReader);
        (float bw, float bh) = PaintedReadout.BezelSize(PaintedReader);
        Assert.AreEqual(width - 2f * PaintedReadout.WindowPad, tw, 1e-6f, "the text inset from the glass's sides");
        Assert.AreEqual(height - 2f * PaintedReadout.WindowPad, th, 1e-6f, "and from its near and far edges");
        Assert.AreEqual(width + 2f * PaintedReadout.Bezel, bw, 1e-6f, "the bezel round the glass");
        Assert.AreEqual(height + 2f * PaintedReadout.Bezel, bh, 1e-6f);
        Assert.Greater(PaintedReadout.BezelDepth, PaintedReadout.GlassDepth, "the bezel behind the glass, the glass behind the text");
        Assert.Greater(PaintedReadout.GlassDepth, 0f);
        // The bezel's near edge: Bezel down the lean and BezelDepth behind the plane from the line's near edge; still over the top face.
        double tilt = PaintedReadout.Tilt * Math.PI / 180.0;
        double bezelNear = PaintedReadout.Lift - PaintedReadout.Bezel * Math.Sin(tilt) - PaintedReadout.BezelDepth * Math.Cos(tilt);
        Assert.Greater(bezelNear, 0.0005, $"the bezel's near edge {bezelNear * 1000:0.00} mm over the face: nothing sinks into the painted body");
        // The painted card slot line lies on the near third of the top face (the feedback still 10.png: about 0.2 to 0.28 of the depth from the near edge).
        (_, _, float nz) = PaintedReadout.NearEdge(PaintedReader);
        Assert.Greater(nz - PaintedReader.MinZ, 0.29f * PaintedReader.Depth, "the window's near edge beyond the painted slot line: the slot stays in view under it");
        (_, _, float fz) = PaintedReadout.FarEdge(PaintedReader);
        Assert.Less(fz, PaintedReader.MaxZ - 0.1f * PaintedReader.Depth, "its far edge short of the far rim: the lamp's corner stays clear");
    }

    /// <summary>The screen height (px, 1920 x 1080) between two points of the painted desk's frame seen from the office eye (DeskHomeMap.OfficeEye).</summary>
    private static float ScreenHeight((float x, float y, float z) a, (float x, float y, float z) b) => Math.Abs(Screen(a).y - Screen(b).y);

    /// <summary>The screen width (px, 1920 x 1080) between two points seen from the office eye.</summary>
    private static float ScreenWidth((float x, float y, float z) a, (float x, float y, float z) b) => Math.Abs(Screen(a).x - Screen(b).x);

    /// <summary>A point of the painted desk's frame (x across, y over the desk top, z away from the chair) on a 1920 x 1080 screen from the office eye, pitched OfficePitch down.</summary>
    private static (float x, float y) Screen((float x, float y, float z) p)
    {
        (float ex, float ey, float eh) = DeskHomeMap.OfficeEye;
        double vx = p.x - ex, vy = p.y - eh, vz = p.z - ey;
        double pitch = OfficePitch * Math.PI / 180.0, half = Math.Tan(OfficeFieldOfView * Math.PI / 360.0);
        double ahead = -vy * Math.Sin(pitch) + vz * Math.Cos(pitch);
        double upward = vy * Math.Cos(pitch) + vz * Math.Sin(pitch);
        Assert.Greater(ahead, 0.0, "the point is in front of the eye");
        return ((float)(960.0 + 540.0 * vx / ahead / half), (float)(540.0 - 540.0 * upward / ahead / half));
    }

    private static float Distance((float x, float y, float z) a, (float x, float y, float z) b) =>
        (float)Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z));
}
