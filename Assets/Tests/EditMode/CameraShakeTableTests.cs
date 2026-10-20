using System;
using NUnit.Framework;

/// <summary>
/// The game feel's shake table (GAME_FEEL_BIBLE §1.3; CAM): a row per moment
/// with its pixel cap, length and direction; the shake calls that predate the
/// table read by their knobs, exactly (the stamp, the DETAIN press, the CITED
/// slam, the breach), every other call no shake ("Everything else: 0"); a cap
/// in pixels at 1080p becomes the camera's move at the nearest work's depth,
/// over the impulse shape's measured peak.
/// </summary>
public class CameraShakeTableTests
{
    /// <summary>MotionKnobs' defaults: the stamp's, the citation's and the breach's force and seconds, and FeelDirector.Hit's strengths.</summary>
    private const float Stamp = 0.04f, StampSeconds = 0.18f, Citation = 0.1f, CitationSeconds = 0.3f, Breach = 0.28f, BreachSeconds = 0.9f;
    private const float DetainHit = 0.5f, CitedHit = 0.25f, ApproveHit = 0.3f;

    private static readonly CameraShakeKnobs Knobs = new CameraShakeKnobs(Stamp, StampSeconds, Breach, BreachSeconds, DetainHit, CitedHit);

    /// <summary>A shake call as FeelDirector.Hit builds it (MotionKnobs: the breach's force times the strength, seconds lerped from the stamp's to the breach's, a rumble from 0.75).</summary>
    private static (float force, float seconds, bool rumble) Hit(float strength)
    {
        float s = Math.Min(1f, Math.Max(0f, strength));
        return (Breach * s, StampSeconds + (BreachSeconds - StampSeconds) * s, s >= 0.75f);
    }

    private static CameraShakeMoment? Legacy((float force, float seconds, bool rumble) call) =>
        CameraShakeTable.TryLegacy(call.force, call.seconds, call.rumble, Knobs, out CameraShakeMoment moment) ? moment : (CameraShakeMoment?)null;

    [Test]
    public void EveryMoment_HasOneRow_AsTheBibleLists()
    {
        foreach (CameraShakeMoment moment in Enum.GetValues(typeof(CameraShakeMoment)))
            Assert.AreEqual(1, Array.FindAll(CameraShakeTable.Rows, r => r.Moment == moment).Length, moment.ToString());
        Assert.AreEqual(1.5f, CameraShakeTable.Row(CameraShakeMoment.Stamp).CapPx);
        Assert.AreEqual(2.5f, CameraShakeTable.Row(CameraShakeMoment.Detain).CapPx);
        Assert.AreEqual(0.16f, CameraShakeTable.Row(CameraShakeMoment.Detain).Seconds, 1e-6f, "160 ms");
        Assert.AreEqual(2f, CameraShakeTable.Row(CameraShakeMoment.Breaker).CapPx);
        Assert.AreEqual(3f, CameraShakeTable.Row(CameraShakeMoment.Cited).CapPx);
        Assert.AreEqual(0.25f, CameraShakeTable.Row(CameraShakeMoment.Cited).Seconds, 1e-6f, "250 ms");
        Assert.AreEqual(6f, CameraShakeTable.Row(CameraShakeMoment.Breach).CapPx);
        Assert.AreEqual(8f, CameraShakeTable.Row(CameraShakeMoment.Destroyed).CapPx);
        Assert.AreEqual(0.12f, CameraShakeTable.Row(CameraShakeMoment.Stamp).Seconds, 1e-6f, "120 ms");
    }

    [Test]
    public void Bumps_GoDown_TheBreakerDownRight_TheBigOnesRumble()
    {
        foreach (CameraShakeRow row in CameraShakeTable.Rows)
        {
            Assert.AreEqual(1f, row.X * row.X + row.Y * row.Y, 1e-4f, row.Moment + ": a unit direction");
            Assert.Less(row.Y, 0f, row.Moment + ": down");
        }
        Assert.Greater(CameraShakeTable.Row(CameraShakeMoment.Breaker).X, 0f, "the breaker bumps down-right");
        Assert.AreEqual(0f, CameraShakeTable.Row(CameraShakeMoment.Stamp).X, "the stamp bumps straight down");
        Assert.IsTrue(CameraShakeTable.Row(CameraShakeMoment.Breach).Rumble);
        Assert.IsTrue(CameraShakeTable.Row(CameraShakeMoment.Destroyed).Rumble);
        Assert.IsFalse(CameraShakeTable.Row(CameraShakeMoment.Cited).Rumble);
    }

    [Test]
    public void TheListedMoments_DrawTheirOwnRows()
    {
        Assert.AreEqual(CameraShakeMoment.Stamp, Legacy((Stamp, StampSeconds, false)), "FeelHit.Stamp: the dater lands");
        Assert.AreEqual(CameraShakeMoment.Detain, Legacy(Hit(DetainHit)), "DETAIN pressed: its own 2.5 px / 160 ms row, not CITED's");
        Assert.AreEqual(CameraShakeMoment.Cited, Legacy(Hit(CitedHit)), "the CITED slam on the slip: CITED's row (the 0.07 force no longer falls to the stamp's)");
        Assert.AreEqual(CameraShakeMoment.Breach, Legacy((Breach, BreachSeconds, true)), "FeelHit.Breach");
    }

    [Test]
    public void EverythingElse_DoesNotShake()
    {
        Assert.IsNull(Legacy(Hit(ApproveHit)), "the approval's hand-back (A10: n/a)");
        Assert.IsNull(Legacy(Hit(0.15f)), "the chip reader's insert (C2: n/a)");
        Assert.IsNull(Legacy(Hit(0.3f)), "the chip reader's fault (C2: n/a)");
        Assert.IsNull(Legacy((Citation, CitationSeconds, false)), "the citation's own shake at the verdict: the slip's slam carries the CITED moment, once");
        Assert.IsNull(Legacy(Hit(0.9f)), "any other strength");
        Assert.AreEqual(CameraShakeMoment.Breach, Legacy(Hit(1f)), "a full-strength hit is the breach's own call (the same force, seconds and rumble)");
        Assert.IsNull(Legacy((Stamp, StampSeconds, true)), "a rumble at the stamp's force is no listed moment");
    }

    [Test]
    public void AStrengthOfNothing_NamesNoMoment()
    {
        var off = new CameraShakeKnobs(Stamp, StampSeconds, Breach, BreachSeconds, 0f, 0f);
        Assert.IsFalse(CameraShakeTable.TryLegacy(0f, StampSeconds, false, off, out _), "detainHit and citedHit 0: no DETAIN or CITED row from a zero force");
    }

    [Test]
    public void Metres_MovesThePictureByTheCap_AtTheDepth_NeverDeeperThanTheLoggersReference()
    {
        float m = CameraShakeTable.Metres(1.5f, 58f);
        Assert.AreEqual(1.5f / 540f * CameraShakeTable.ReferenceDepth * (float)Math.Tan(29.0 * Math.PI / 180.0), m, 1e-6f);
        Assert.Less(m, 0.004f, "a stamp moves the camera a few millimetres, not centimetres");
        Assert.AreEqual(0f, CameraShakeTable.Metres(0f, 58f));
        Assert.AreEqual(0f, CameraShakeTable.Metres(-2f, 58f));
        Assert.Greater(CameraShakeTable.Metres(1.5f, 70f), m, "a wider lens needs a bigger move for the same pixels");
        Assert.AreEqual(m * 1.29f / 2f, CameraShakeTable.Metres(1.5f, 58f, 1.29f), 1e-6f, "papers 1.29 m away: a smaller move keeps them under the cap");
        Assert.AreEqual(m, CameraShakeTable.Metres(1.5f, 58f, 5f), 1e-7f, "never sized for anything deeper than the logger's reference");
    }

    [Test]
    public void Velocity_PeaksAtTheCapLessHeadroom_ThroughTheShapesPeak()
    {
        CameraShakeRow stamp = CameraShakeTable.Row(CameraShakeMoment.Stamp);
        float v = CameraShakeTable.Velocity(stamp, 1f, 47.5f, 1.4f);
        Assert.AreEqual(CameraShakeTable.Metres(stamp.CapPx * CameraShakeTable.Headroom, 47.5f, 1.4f), v * CameraShakeTable.BumpPeak, 1e-7f,
                        "the bump's largest offset (0.993 of its velocity) is 80% of the cap at the papers");
        CameraShakeRow breach = CameraShakeTable.Row(CameraShakeMoment.Breach);
        Assert.AreEqual(CameraShakeTable.Metres(breach.CapPx * CameraShakeTable.Headroom, 47.5f, 1.4f), CameraShakeTable.Velocity(breach, 1f, 47.5f, 1.4f) * CameraShakeTable.RumblePeak, 1e-7f);
        Assert.AreEqual(v / 2f, CameraShakeTable.Velocity(stamp, 0.5f, 47.5f, 1.4f), 1e-7f, "half the Motion intensity, half the shake");
        Assert.AreEqual(0f, CameraShakeTable.Velocity(stamp, 0f, 47.5f, 1.4f), "no intensity, no shake");
    }
}
