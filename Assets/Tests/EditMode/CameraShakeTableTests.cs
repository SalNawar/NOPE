using System;
using NUnit.Framework;

/// <summary>
/// The game feel's shake table (GAME_FEEL_BIBLE §1.3; CAM): a row per moment
/// with its pixel cap, length and direction; the callers' knobs pick the row
/// (the stamp, the CITED slam, the breach and the destroyed timeline); a cap
/// in pixels at 1080p becomes the camera's move at the QA logger's reference
/// depth.
/// </summary>
public class CameraShakeTableTests
{
    private const float StampForce = 0.04f, CitationForce = 0.1f, BreachForce = 0.28f;

    [Test]
    public void EveryMoment_HasOneRow_AsTheBibleLists()
    {
        foreach (CameraShakeMoment moment in Enum.GetValues(typeof(CameraShakeMoment)))
            Assert.AreEqual(1, Array.FindAll(CameraShakeTable.Rows, r => r.Moment == moment).Length, moment.ToString());
        Assert.AreEqual(1.5f, CameraShakeTable.Row(CameraShakeMoment.Stamp).CapPx);
        Assert.AreEqual(2.5f, CameraShakeTable.Row(CameraShakeMoment.Detain).CapPx);
        Assert.AreEqual(2f, CameraShakeTable.Row(CameraShakeMoment.Breaker).CapPx);
        Assert.AreEqual(3f, CameraShakeTable.Row(CameraShakeMoment.Cited).CapPx);
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
    public void TheCallersKnobs_PickTheirRow()
    {
        Assert.AreEqual(CameraShakeMoment.Stamp, CameraShakeTable.Pick(StampForce, false, StampForce, CitationForce, BreachForce).Moment, "FeelHit.Stamp, the printed report");
        Assert.AreEqual(CameraShakeMoment.Cited, CameraShakeTable.Pick(CitationForce, false, StampForce, CitationForce, BreachForce).Moment, "FeelHit.Citation");
        Assert.AreEqual(CameraShakeMoment.Breach, CameraShakeTable.Pick(BreachForce, true, StampForce, CitationForce, BreachForce).Moment, "FeelHit.Breach");
        Assert.AreEqual(CameraShakeMoment.Cited, CameraShakeTable.Pick(BreachForce * 0.5f, false, StampForce, CitationForce, BreachForce).Moment, "FeelDirector.Hit(0.5): a heavy bump");
        Assert.AreEqual(CameraShakeMoment.Destroyed, CameraShakeTable.Pick(BreachForce * 2f, true, StampForce, CitationForce, BreachForce).Moment, "a rumble past the breach");
    }

    [Test]
    public void Metres_MovesThePictureByTheCap_AtTheReferenceDepth()
    {
        float m = CameraShakeTable.Metres(1.5f, 58f);
        Assert.AreEqual(1.5f / 540f * CameraShakeTable.ReferenceDepth * (float)Math.Tan(29.0 * Math.PI / 180.0), m, 1e-6f);
        Assert.Less(m, 0.004f, "a stamp moves the camera a few millimetres, not centimetres");
        Assert.AreEqual(0f, CameraShakeTable.Metres(0f, 58f));
        Assert.AreEqual(0f, CameraShakeTable.Metres(-2f, 58f));
        Assert.Greater(CameraShakeTable.Metres(1.5f, 70f), m, "a wider lens needs a bigger move for the same pixels");
    }
}
