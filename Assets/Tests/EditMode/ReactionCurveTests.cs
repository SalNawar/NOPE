using System;
using NUnit.Framework;

/// <summary>The desk props' click reactions: every pose starts and ends at rest, and the peaks.</summary>
public class ReactionCurveTests
{
    private const float Tolerance = 0.0001f;
    private const float A = 0.12f;

    private static void AssertPose(ReactionPose expected, ReactionPose actual, string what)
    {
        Assert.AreEqual(expected.ScaleX, actual.ScaleX, Tolerance, what + " scale x");
        Assert.AreEqual(expected.ScaleY, actual.ScaleY, Tolerance, what + " scale y");
        Assert.AreEqual(expected.AngleDeg, actual.AngleDeg, Tolerance, what + " angle");
        Assert.AreEqual(expected.OffsetY, actual.OffsetY, Tolerance, what + " offset");
    }

    [Test]
    public void EveryKind_IsAtRestAtTheStartAndTheEnd()
    {
        foreach (ReactionKind kind in Enum.GetValues(typeof(ReactionKind)))
        {
            AssertPose(ReactionPose.Identity, ReactionCurve.Evaluate(kind, 0f, A), $"{kind} at 0");
            AssertPose(ReactionPose.Identity, ReactionCurve.Evaluate(kind, 1f, A), $"{kind} at 1");
        }
    }

    [Test]
    public void SquashPulseAndNudge_PeakHalfway()
    {
        AssertPose(new ReactionPose(1f + A, 1f - A, 0f, 0f), ReactionCurve.Evaluate(ReactionKind.Squash, 0.5f, A), "squash");
        AssertPose(new ReactionPose(1f + A, 1f + A, 0f, 0f), ReactionCurve.Evaluate(ReactionKind.Pulse, 0.5f, A), "pulse");
        AssertPose(new ReactionPose(1f, 1f, 0f, -A), ReactionCurve.Evaluate(ReactionKind.Nudge, 0.5f, A), "nudge");
    }

    [Test]
    public void Wobble_AtOneSixth_TurnsTwentyFiveTimesTheAmplitude()
    {
        // a * 30 * sin(pi / 2) * (1 - 1/6) = 25a degrees.
        AssertPose(new ReactionPose(1f, 1f, 25f * A, 0f), ReactionCurve.Evaluate(ReactionKind.Wobble, 1f / 6f, A), "wobble");
    }

    [Test]
    public void TimeIsClamped()
    {
        AssertPose(ReactionPose.Identity, ReactionCurve.Evaluate(ReactionKind.Squash, -1f, A), "before");
        AssertPose(ReactionPose.Identity, ReactionCurve.Evaluate(ReactionKind.Pulse, 3f, A), "after");
    }

    [Test]
    public void None_IsAlwaysAtRest()
    {
        foreach (float t in new[] { 0f, 0.25f, 0.5f, 0.9f, 1f })
            AssertPose(ReactionPose.Identity, ReactionCurve.Evaluate(ReactionKind.None, t, A), $"none at {t}");
    }

    [Test]
    public void TheKindInts_ArePinned_TheyAreSerializedOnDeskReactionSO()
    {
        Assert.AreEqual(0, (int)ReactionKind.None);
        Assert.AreEqual(1, (int)ReactionKind.Squash);
        Assert.AreEqual(2, (int)ReactionKind.Wobble);
        Assert.AreEqual(3, (int)ReactionKind.Nudge);
        Assert.AreEqual(4, (int)ReactionKind.Pulse);
    }
}
