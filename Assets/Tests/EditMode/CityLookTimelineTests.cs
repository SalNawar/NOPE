using NUnit.Framework;

/// <summary>
/// The look at the city's timeline (Saleh 2026-10-07): the hall turns first,
/// from a share of the turn the matte veils the hall and then the panorama
/// comes up over it (never both at once: Saleh 2026-10-08, "it shows
/// everything doubled"), the clock's length is the later end, turning back
/// runs the same clock down (the city first), a cut (Reduced Motion) jumps to
/// either end, and the flying traffic's wrap is never in view.
/// </summary>
public class CityLookTimelineTests
{
    [Test]
    public void Total_IsTheLaterOfTheTurnAndTheFade()
    {
        Assert.AreEqual(0.7f + 0.6f * 0.5f, CityLookTimeline.Total(0.6f, 0.5f, 0.7f), 1e-5f);
        Assert.AreEqual(2f, CityLookTimeline.Total(2f, 0.1f, 0.5f), 1e-5f, "a long turn outlasts a short fade");
        Assert.AreEqual(0f, CityLookTimeline.Total(0f, 0.5f, 0f), 1e-5f);
    }

    [Test]
    public void TheTurnLeads_ThePanoramaFadesInFromItsShare()
    {
        const float turn = 0.6f, from = 0.5f, fade = 0.7f;
        Assert.AreEqual(0f, CityLookTimeline.Pan(0f, turn));
        Assert.AreEqual(0f, CityLookTimeline.Veil(0.29f, turn, from, fade), "no veil before half the turn");
        Assert.Greater(CityLookTimeline.Pan(0.29f, turn), 0.3f, "the hall is already turning");
        Assert.AreEqual(0.5f, CityLookTimeline.Veil(0.3f + 0.175f, turn, from, fade), 1e-5f, "eased: a quarter through the fade, the matte half over the hall");
        Assert.AreEqual(1f, CityLookTimeline.Pan(turn, turn));
        float total = CityLookTimeline.Total(turn, from, fade);
        Assert.AreEqual(1f, CityLookTimeline.Veil(total, turn, from, fade));
        Assert.AreEqual(1f, CityLookTimeline.Reveal(total, turn, from, fade));
        Assert.AreEqual(0.5f, CityLookTimeline.Reveal(0.3f + 0.525f, turn, from, fade), 1e-5f, "eased: three quarters through the fade, the city half up");
    }

    [Test]
    public void TheHallAndTheCity_NeverShowAtOnce()
    {
        const float turn = 0.6f, from = 0.5f, fade = 0.7f;
        float total = CityLookTimeline.Total(turn, from, fade);
        for (float t = 0f; t <= total; t += 0.01f)
            if (CityLookTimeline.Reveal(t, turn, from, fade) > 0f)
                Assert.AreEqual(1f, CityLookTimeline.Veil(t, turn, from, fade), 1e-6f, "the city comes up only over the whole matte (at " + t + " s)");
    }

    [Test]
    public void TurningBack_RunsTheSameClockDown_TheFadeFirst()
    {
        const float turn = 0.6f, from = 0.5f, fade = 0.7f;
        float total = CityLookTimeline.Total(turn, from, fade), t = total;
        t = CityLookTimeline.Step(t, false, 0.4f, total, false);
        Assert.Less(CityLookTimeline.Reveal(t, turn, from, fade), 1f, "the city fades out first");
        Assert.AreEqual(1f, CityLookTimeline.Pan(t, turn), "while the hall still looks left");
        t = CityLookTimeline.Step(t, false, 10f, total, false);
        Assert.AreEqual(0f, t);
        Assert.AreEqual(total, CityLookTimeline.Step(0f, true, 99f, total, false), "kept inside the clock");
    }

    [Test]
    public void TheSweep_StartsWhole_AndSettlesEased()
    {
        Assert.AreEqual(1f, CityLookTimeline.Sweep(0f, 1.6f), "the city starts turned with the hall");
        Assert.AreEqual(0.5f, CityLookTimeline.Sweep(0.8f, 1.6f), 1e-5f, "half way at half the settle");
        Assert.AreEqual(0f, CityLookTimeline.Sweep(1.6f, 1.6f));
        Assert.AreEqual(0f, CityLookTimeline.Sweep(9f, 1.6f), "settled");
        Assert.AreEqual(0f, CityLookTimeline.Sweep(0f, 0f), "no settle: no sweep");
    }

    [Test]
    public void Traffic_MovesAlongItsLane_AndWraps()
    {
        Assert.AreEqual(0.12f, CityLookTimeline.Travel(0f, 0.12f, 17f, 740f), 1e-5f, "starts at its phase");
        Assert.AreEqual(0.12f + 17f * 10f / 740f, CityLookTimeline.Travel(10f, 0.12f, 17f, 740f), 1e-4f);
        Assert.AreEqual(0.25f, CityLookTimeline.Travel(1f, 0.75f, 50f, 100f), 1e-5f, "wraps past its end");
        Assert.AreEqual(0.3f, CityLookTimeline.Travel(5f, 0.3f, 10f, 0f), 1e-5f, "a lane of no length stays put");
    }

    [Test]
    public void AFlyer_EntersAndLeavesOffTheFrame()
    {
        const float crop = 0.07f, depth = 1.25f, half = 0.0125f;
        float reach = CityLookTimeline.Reach(crop, depth);
        Assert.AreEqual(crop * 0.25f, reach, 1e-6f, "a layer panning further than the painting is seen past its edges");
        Assert.AreEqual(0f, CityLookTimeline.Reach(crop, 1f), "the painting's own pan stays inside it");
        // The view sees painting widths -reach..1+reach at the furthest pan: both ends of a pass are wholly outside.
        foreach (bool right in new[] { true, false })
            foreach (float along in new[] { 0f, 0.99999f })
            {
                float u = CityLookTimeline.Flight(along, right, half, reach);
                Assert.IsTrue(u + half <= -reach + 1e-4f || u - half >= 1f + reach - 1e-4f, "off the frame at " + along + (right ? " flying right" : " flying left") + ": " + u);
            }
        Assert.AreEqual(0.5f, CityLookTimeline.Flight(0.5f, true, half, reach), 1e-6f, "mid pass mid painting");
        Assert.Less(CityLookTimeline.Flight(0.1f, false, half, reach), 1f + reach + half, "flying left starts at the right");
        Assert.Greater(CityLookTimeline.Flight(0.1f, false, half, reach), CityLookTimeline.Flight(0.2f, false, half, reach));
        Assert.AreEqual(1f + 2f * (half + reach), CityLookTimeline.FlightLength(half, reach), 1e-6f);
    }

    [Test]
    public void ACut_JumpsToEitherEnd()
    {
        Assert.AreEqual(1.3f, CityLookTimeline.Step(0f, true, 0.01f, 1.3f, true));
        Assert.AreEqual(0f, CityLookTimeline.Step(1.3f, false, 0.01f, 1.3f, true));
        Assert.AreEqual(1f, CityLookTimeline.Reveal(0f, 0f, 0.5f, 0f), "no turn and no fade: the city at once");
        Assert.AreEqual(1f, CityLookTimeline.Veil(0f, 0f, 0.5f, 0f));
        Assert.AreEqual(1f, CityLookTimeline.Pan(0f, 0f));
    }
}
