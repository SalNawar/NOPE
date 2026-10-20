using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The camera logger's rule (GAME_FEEL_BIBLE G9 and gap 3): a move under a
/// tagged cause passes, an untagged move fails, an impulse over its pixel cap
/// fails; and the picture-shift measure gives sensible pixels. Pure; runs
/// outside Unity.
/// </summary>
public class QaCameraMovesTests
{
    private static List<QaCameraFrame> Frames(params float[] steps)
    {
        var frames = new List<QaCameraFrame>();
        for (int i = 0; i < steps.Length; i++)
            frames.Add(new QaCameraFrame(100 + i, i / 30f, steps[i]));
        return frames;
    }

    /// <summary>A yaw of <paramref name="degrees"/> about the up axis (built by hand: Quaternion.Euler is an engine call).</summary>
    private static Quaternion Yaw(float degrees)
    {
        float h = degrees * Mathf.Deg2Rad * 0.5f;
        return new Quaternion(0f, Mathf.Sin(h), 0f, Mathf.Cos(h));
    }

    [Test]
    public void StillCamera_Passes()
    {
        QaCameraReport r = QaCameraMoves.Classify(Frames(0f, 0.01f, 0f), new List<QaCameraTag>());
        Assert.IsTrue(r.Pass);
        Assert.AreEqual(0, r.MovedFrames);
    }

    [Test]
    public void UntaggedMove_Fails_AndNamesTheFrame()
    {
        QaCameraReport r = QaCameraMoves.Classify(Frames(0f, 3f, 0f), new List<QaCameraTag>());
        Assert.IsFalse(r.Pass);
        CollectionAssert.AreEqual(new[] { 101 }, r.UntaggedFrames);
    }

    [Test]
    public void ViewKeyBlend_CoversItsMove_PlusTheGrace()
    {
        var tags = new List<QaCameraTag> { new QaCameraTag { Cause = CameraMoveCause.ViewKey, Start = 1 / 30f, End = 3 / 30f } };
        QaCameraReport r = QaCameraMoves.Classify(Frames(0f, 5f, 5f, 2f, 0.5f), tags);
        Assert.IsTrue(r.Pass, r.Summary());
        Assert.AreEqual(4, r.FramesByCause[CameraMoveCause.ViewKey]);
    }

    [Test]
    public void MoveAfterTheWindowAndGrace_Fails()
    {
        var tags = new List<QaCameraTag> { new QaCameraTag { Cause = CameraMoveCause.DeskDropTilt, Start = 0f, End = 1 / 30f } };
        QaCameraReport r = QaCameraMoves.Classify(Frames(4f, 4f, 0f, 0f, 0f, 2f), tags);
        Assert.IsFalse(r.Pass);
        CollectionAssert.AreEqual(new[] { 105 }, r.UntaggedFrames);
    }

    [Test]
    public void Impulse_OverItsCap_Fails_UnderItsCap_Passes()
    {
        var over = new List<QaCameraTag> { new QaCameraTag { Cause = CameraMoveCause.Impulse, Start = 0f, End = 0.12f, CapPx = 1.5f, MaxOffsetPx = 2.4f } };
        var under = new List<QaCameraTag> { new QaCameraTag { Cause = CameraMoveCause.Impulse, Start = 0f, End = 0.12f, CapPx = 1.5f, MaxOffsetPx = 1.2f } };
        Assert.IsFalse(QaCameraMoves.Classify(Frames(1f, 1f), over).Pass);
        Assert.IsTrue(QaCameraMoves.Classify(Frames(1f, 1f), under).Pass);
    }

    [Test]
    public void ShiftPx_IsZeroForTheSamePose_AndGrowsWithAYaw()
    {
        var a = new QaCameraPose(Vector3.zero, Quaternion.identity, 40f, 16f / 9f);
        Assert.AreEqual(0f, QaCameraMoves.ShiftPx(a, a), 1e-3f);
        var b = new QaCameraPose(Vector3.zero, Yaw(1f), 40f, 16f / 9f);
        // A 1 degree yaw moves the centre by about 540 * tan(1) / tan(20) px at 1080p.
        float expected = 540f * Mathf.Tan(Mathf.Deg2Rad) / Mathf.Tan(20f * Mathf.Deg2Rad);
        Assert.AreEqual(expected, QaCameraMoves.ShiftPx(a, b), expected * 0.15f);
    }

    [Test]
    public void ShiftPx_SeesAZoom()
    {
        var a = new QaCameraPose(Vector3.zero, Quaternion.identity, 40f, 16f / 9f);
        var zoomed = new QaCameraPose(Vector3.zero, Quaternion.identity, 38f, 16f / 9f);
        Assert.Greater(QaCameraMoves.ShiftPx(a, zoomed), 5f);
    }

    [Test]
    public void To1080_ScalesBySceenHeight()
    {
        Assert.AreEqual(3f, QaCameraMoves.To1080(2f, 720f), 1e-4f);
    }
}
