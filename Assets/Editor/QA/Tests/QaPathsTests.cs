using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The P-* drag paths (FIX_PLAN capture vocabulary) have the shapes their
/// names promise: speeds, sway, release points, a hover before the press and
/// a real grab. Pure; runs outside Unity.
/// </summary>
public class QaPathsTests
{
    private static readonly Vector2 Screen1080 = new Vector2(1920f, 1080f);

    private static float MaxHeldSpeed(QaPath p) =>
        Enumerable.Range(p.PressIndex + 1, p.ReleaseIndex - p.PressIndex - 1).Select(p.SpeedAt).DefaultIfEmpty(0f).Max();

    [Test]
    public void EveryPath_HoversFirst_ThenPresses_ThenReleases()
    {
        foreach (QaPathKind kind in System.Enum.GetValues(typeof(QaPathKind)))
        {
            QaPath p = QaPaths.Drag(kind, new Vector2(400f, 300f), new Vector2(900f, 600f), Screen1080);
            Assert.AreEqual(QaPaths.HoverFrames, p.PressIndex, kind.ToString());
            Assert.Greater(p.ReleaseIndex, p.PressIndex + QaPaths.GrabHoldFrames, kind.ToString());
            Assert.IsTrue(p.Samples.Take(p.PressIndex).All(s => !s.Down), kind + " hovers with the button up");
            Assert.IsTrue(p.Samples.Skip(p.ReleaseIndex).All(s => !s.Down), kind + " stays released");
            Assert.AreEqual(p.Start, p.Samples[p.PressIndex + QaPaths.GrabHoldFrames].Position, kind + " holds still after the press (a real grab)");
        }
    }

    [Test]
    public void PlainFastSlowWobbleFlickOnto_EndExactlyOnTheTarget()
    {
        var to = new Vector2(1500f, 200f);
        foreach (QaPathKind kind in new[] { QaPathKind.Plain, QaPathKind.Fast, QaPathKind.Slow, QaPathKind.Wobble, QaPathKind.Flick, QaPathKind.Onto })
            Assert.Less((QaPaths.Drag(kind, new Vector2(300f, 800f), to, Screen1080).End - to).magnitude, 1e-3f, kind.ToString());
    }

    [Test]
    public void Fast_MovesAtLeast2000PxPerSecond()
    {
        QaPath p = QaPaths.Drag(QaPathKind.Fast, new Vector2(100f, 500f), new Vector2(1800f, 500f), Screen1080);
        var moving = Enumerable.Range(p.PressIndex + QaPaths.GrabHoldFrames + 1, p.ReleaseIndex - p.PressIndex - QaPaths.GrabHoldFrames - 1)
                               .Select(p.SpeedAt).Where(v => v > 0f).ToList();
        Assert.IsNotEmpty(moving);
        Assert.GreaterOrEqual(moving.Min(), 2000f);
    }

    [Test]
    public void Slow_NeverExceeds150PxPerSecond()
    {
        QaPath p = QaPaths.Drag(QaPathKind.Slow, new Vector2(100f, 500f), new Vector2(400f, 600f), Screen1080);
        Assert.LessOrEqual(MaxHeldSpeed(p), 150f);
    }

    [Test]
    public void Wobble_SwaysAbout30PxOffTheLine()
    {
        Vector2 from = new Vector2(200f, 540f), to = new Vector2(1200f, 540f);
        QaPath p = QaPaths.Drag(QaPathKind.Wobble, from, to, Screen1080);
        float maxOff = p.Samples.Max(s => Mathf.Abs(s.Position.y - 540f));
        Assert.That(maxOff, Is.InRange(25f, 30.001f));
    }

    [Test]
    public void Diag_EndsOnThe45DegreeLineThroughTheStart()
    {
        Vector2 from = new Vector2(500f, 500f);
        QaPath p = QaPaths.Drag(QaPathKind.Diag, from, new Vector2(900f, 300f), Screen1080);
        Vector2 d = p.End - from;
        Assert.AreEqual(Mathf.Abs(d.x), Mathf.Abs(d.y), 1e-3f);
        Assert.Greater(d.x, 0f);
        Assert.Less(d.y, 0f);
    }

    [Test]
    public void Flick_IsReleasedWhileMovingAbout1500PxPerSecond()
    {
        QaPath p = QaPaths.Drag(QaPathKind.Flick, new Vector2(200f, 300f), new Vector2(1400f, 700f), Screen1080);
        Assert.That(p.SpeedAt(p.ReleaseIndex), Is.InRange(1350f, 1650f), "the release frame still moves");
        Assert.That(p.SpeedAt(p.ReleaseIndex - 1), Is.InRange(1200f, 1700f), "and the frame before it was at speed");
    }

    [Test]
    public void Edge_Releases5PxInsideTheNearestEdge()
    {
        QaPath p = QaPaths.Drag(QaPathKind.Edge, new Vector2(900f, 500f), new Vector2(1880f, 520f), Screen1080);
        Assert.AreEqual(1920f - 5f, p.End.x, 1e-3f);
        Assert.AreEqual(520f, p.End.y, 1e-3f);
    }

    [Test]
    public void Off_ReleasesOutsideTheWindow()
    {
        QaPath p = QaPaths.Drag(QaPathKind.Off, new Vector2(900f, 500f), new Vector2(950f, 1060f), Screen1080);
        Assert.Greater(p.End.y, 1080f);
        Assert.IsTrue(p.Samples[p.ReleaseIndex - 1].Down, "released straight from the move, no still hold");
    }

    [Test]
    public void Click_PressesAndReleasesInPlace()
    {
        QaPath c = QaPaths.Click(new Vector2(10f, 20f));
        Assert.IsTrue(c.Samples.All(s => s.Position == new Vector2(10f, 20f)));
        Assert.Greater(c.ReleaseIndex, c.PressIndex);
    }

    [Test]
    public void ParseKind_AcceptsBothSpellings()
    {
        Assert.AreEqual(QaPathKind.Fast, QaRunner.ParseKind("P-FAST"));
        Assert.AreEqual(QaPathKind.Wobble, QaRunner.ParseKind("wobble"));
        Assert.AreEqual(QaPathKind.Plain, QaRunner.ParseKind(""));
    }
}
