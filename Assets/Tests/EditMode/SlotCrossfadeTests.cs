using NUnit.Framework;

/// <summary>
/// The hall slots' crossfade (HALL-FX, BUG-71; SlotCrossfade): wherever both faces
/// cover, the painting under them never shows through at any moment of the fade,
/// whichever face draws on top, and each face only moves one way.
/// </summary>
public class SlotCrossfadeTests
{
    /// <summary>The painting's share in the composite of the two faces over it (top over bottom, "over" blending).</summary>
    private static float PaintingShows(float top, float bottom) => (1f - top) * (1f - bottom);

    [Test]
    public void ThePainting_NeverShowsThrough_WhereBothFacesCover()
    {
        float lastIn = 0f, lastOut = 1f;
        for (float f = 0f; f <= 1f; f += 0.01f)
        {
            float incoming = SlotCrossfade.Incoming(f), outgoing = SlotCrossfade.Outgoing(f);
            Assert.AreEqual(0f, PaintingShows(incoming, outgoing), 1e-6f, $"incoming on top, at {f}");
            Assert.AreEqual(0f, PaintingShows(outgoing, incoming), 1e-6f, $"outgoing on top, at {f}");
            Assert.GreaterOrEqual(incoming, lastIn, "the incoming face only comes in");
            Assert.LessOrEqual(outgoing, lastOut, "the outgoing face only goes");
            lastIn = incoming;
            lastOut = outgoing;
        }
        Assert.AreEqual(0f, SlotCrossfade.Incoming(0f));
        Assert.AreEqual(1f, SlotCrossfade.Outgoing(0f));
        Assert.AreEqual(1f, SlotCrossfade.Incoming(1f));
        Assert.AreEqual(0f, SlotCrossfade.Outgoing(1f));
        Assert.AreEqual(0.25f, PaintingShows(0.5f, 0.5f), 1e-6f, "the old Fade / 1 - Fade showed a quarter of the painting at mid-fade");
    }
}
