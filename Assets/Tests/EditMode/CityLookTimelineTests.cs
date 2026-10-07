using NUnit.Framework;

/// <summary>
/// The look at the city's timeline (Saleh 2026-10-07): the hall turns first,
/// the panorama fades in from a share of the turn, the clock's length is the
/// later end, turning back runs the same clock down (the fade first), and a
/// cut (Reduced Motion) jumps to either end.
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
        Assert.AreEqual(0f, CityLookTimeline.Fade(0.29f, turn, from, fade), "no fade before half the turn");
        Assert.Greater(CityLookTimeline.Pan(0.29f, turn), 0.3f, "the hall is already turning");
        Assert.AreEqual(0.5f, CityLookTimeline.Fade(0.3f + 0.35f, turn, from, fade), 1e-5f, "eased: half way through the fade, half opaque");
        Assert.AreEqual(1f, CityLookTimeline.Pan(turn, turn));
        Assert.AreEqual(1f, CityLookTimeline.Fade(CityLookTimeline.Total(turn, from, fade), turn, from, fade));
    }

    [Test]
    public void TurningBack_RunsTheSameClockDown_TheFadeFirst()
    {
        const float turn = 0.6f, from = 0.5f, fade = 0.7f;
        float total = CityLookTimeline.Total(turn, from, fade), t = total;
        t = CityLookTimeline.Step(t, false, 0.4f, total, false);
        Assert.Less(CityLookTimeline.Fade(t, turn, from, fade), 1f, "the city fades out first");
        Assert.AreEqual(1f, CityLookTimeline.Pan(t, turn), "while the hall still looks left");
        t = CityLookTimeline.Step(t, false, 10f, total, false);
        Assert.AreEqual(0f, t);
        Assert.AreEqual(total, CityLookTimeline.Step(0f, true, 99f, total, false), "kept inside the clock");
    }

    [Test]
    public void ACut_JumpsToEitherEnd()
    {
        Assert.AreEqual(1.3f, CityLookTimeline.Step(0f, true, 0.01f, 1.3f, true));
        Assert.AreEqual(0f, CityLookTimeline.Step(1.3f, false, 0.01f, 1.3f, true));
        Assert.AreEqual(1f, CityLookTimeline.Fade(0f, 0f, 0.5f, 0f), "no turn and no fade: the city at once");
        Assert.AreEqual(1f, CityLookTimeline.Pan(0f, 0f));
    }
}
