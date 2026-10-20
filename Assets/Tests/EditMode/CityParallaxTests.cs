using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The city view's parallax speed limit (BUG-69; GAME_FEEL_BIBLE H1 and
/// check 7): the pan follows its target on a spring whose nearest layer never
/// moves more than one pixel at 1080p a frame of the QA recorder's 30 fps,
/// at any screen size; it takes the same seconds at any frame rate, eases
/// into its target instead of stopping dead, and the reveal's sweep is kept
/// small enough to ease within the limit.
/// </summary>
public class CityParallaxTests
{
    private const float Span = 0.86f, Width = 2786f, Depth = 1.25f;

    private static float PxPerShare => Width / Span * Depth;

    [Test]
    public void MaxSpeed_IsOnePixelA30FpsFrame_OnTheNearestLayer_AtAnyScreenSize()
    {
        float speed = CityParallax.MaxSpeed(Span, Width, 1080f, Depth);
        Assert.AreEqual(30f, speed * PxPerShare, 1e-3f, "30 px a second on the traffic: 1 px a frame at 30 fps");
        Assert.AreEqual(speed, CityParallax.MaxSpeed(Span, Width * 720f / 1080f, 720f, Depth), 1e-7f, "the same pixels at 1080p on a 720p screen");
        Assert.AreEqual(CityParallax.MaxSpeed(Span, Width, 1080f, 1f), CityParallax.MaxSpeed(Span, Width, 1080f, 0.5f), "a layer never pans less than the painting");
        Assert.AreEqual(0f, CityParallax.MaxSpeed(0f, Width, 1080f, Depth));
        Assert.AreEqual(0f, CityParallax.MaxSpeed(Span, 0f, 1080f, Depth));
    }

    [Test]
    public void SweepShare_EasesWithinTheLimit_SoItNeverCrawls()
    {
        float speed = CityParallax.MaxSpeed(Span, Width, 1080f, Depth);
        float sweep = CityParallax.SweepShare(0.05f, 1.8f, speed);
        Assert.Less(sweep, 0.05f, "the knob's 0.05 would outrun 30 px a second over 1.8 s");
        Assert.AreEqual(CityParallax.SweepHeadroom * speed, 1.5f * sweep / 1.8f, 1e-6f, "the smoothstep's steepest slope at 80% of the limit");
        Assert.AreEqual(0.001f, CityParallax.SweepShare(0.001f, 1.8f, speed), "a small sweep is kept as it is");
        Assert.AreEqual(0f, CityParallax.SweepShare(0.05f, 0f, speed));
    }

    /// <summary>Plays the reveal (the sweep easing to 0 over 1.8 s) at <paramref name="fps"/>: the worst move in pixels at 1080p per 30 fps frame, the seconds until it settles, and its frames' steps.</summary>
    private static (float worst, float settled, List<float> steps) Reveal(float fps)
    {
        float speed = CityParallax.MaxSpeed(Span, Width, 1080f, Depth);
        float sweep = CityParallax.SweepShare(0.05f, 1.8f, speed);
        float dt = 1f / fps, shown = sweep, velocity = 0f, worst = 0f, settled = -1f;
        var steps = new List<float>();
        for (float t = dt; t < 8f; t += dt)
        {
            float u = System.Math.Min(1f, t / 1.8f);
            float target = sweep * (1f - u * u * (3f - 2f * u));
            float next = CityParallax.Follow(shown, ref velocity, target, speed, dt);
            float px = System.Math.Abs(next - shown) * PxPerShare;
            steps.Add(px);
            worst = System.Math.Max(worst, px * fps / 30f);
            if (settled < 0f && target == 0f && System.Math.Abs(next) * PxPerShare < 0.01f)
                settled = t;
            shown = next;
        }
        return (worst, settled, steps);
    }

    [Test]
    public void Follow_KeepsUnderOnePixelA30FpsFrame_AndTakesTheSameSecondsAtAnyFrameRate()
    {
        (float worst30, float settled30, _) = Reveal(30f);
        (float worst60, float settled60, _) = Reveal(60f);
        (float worst144, float settled144, _) = Reveal(144f);
        Assert.LessOrEqual(worst30, 1f + 1e-4f);
        Assert.LessOrEqual(worst60, 1f + 1e-4f);
        Assert.LessOrEqual(worst144, 1f + 1e-4f);
        Assert.Greater(settled30, 1.8f, "it settles after the sweep's 1.8 s");
        Assert.Less(settled30, 4f, "not a 3 to 7 s crawl at the limit");
        Assert.AreEqual(settled30, settled60, 0.5f, "the same seconds at 30 and 60 fps");
        Assert.AreEqual(settled30, settled144, 0.5f, "and at 144 fps");
    }

    [Test]
    public void Follow_EasesIntoItsTarget_NothingStoppingDead()
    {
        (_, float settled, List<float> steps) = Reveal(60f);
        int last = (int)(settled * 60f) - 2;
        Assert.Greater(last, 10);
        for (int i = last - 8; i < last; i++)
            Assert.LessOrEqual(steps[i + 1], steps[i] + 1e-4f, $"the steps shrink into the stop (frame {i})");
        Assert.Less(steps[last], 0.05f, "the last steps are a fraction of a pixel");
    }

    [Test]
    public void LookShare_FitsAnEdgeToEdgePointerMove_IntoTheSpring_SoItNeverCrawls()
    {
        float speed = CityParallax.MaxSpeed(Span, Width, 1080f, Depth);
        float look = CityParallax.LookShare(0.02f, speed);
        Assert.Less(look, 0.02f, "the knob's 0.02 a side would crawl at the limit");
        Assert.AreEqual(speed * CityParallax.SmoothSeconds, 2f * look, 1e-7f, "edge to edge: what the spring covers within the limit");
        Assert.AreEqual(10.5f, 2f * look * PxPerShare, 1e-3f, "10.5 px of traffic at 1080p edge to edge (30 px a second over 0.35 s)");
        Assert.AreEqual(0.0005f, CityParallax.LookShare(0.0005f, speed), "a small look is kept as it is");
        Assert.AreEqual(0f, CityParallax.LookShare(0.02f, 0f), "no limit, no look");
        Assert.AreEqual(0f, CityParallax.LookShare(0f, speed), "the knob off");
    }

    /// <summary>Plays the pointer crossing the screen with a look of <paramref name="look"/> a side at <paramref name="fps"/>: the worst move per 30 fps frame (px at 1080p), its fastest share of the limit and the seconds until it lies within 0.1 px of its target.</summary>
    private static (float worst, float fastest, float settled) Look(float look, float fps)
    {
        float speed = CityParallax.MaxSpeed(Span, Width, 1080f, Depth);
        float dt = 1f / fps, shown = -look, velocity = 0f, worst = 0f, fastest = 0f, settled = -1f;
        for (float t = dt; t < 12f && settled < 0f; t += dt)
        {
            float next = CityParallax.Follow(shown, ref velocity, look, speed, dt);
            float step = System.Math.Abs(next - shown);
            worst = System.Math.Max(worst, step * PxPerShare * fps / 30f);
            fastest = System.Math.Max(fastest, step / dt / speed);
            if (System.Math.Abs(look - next) * PxPerShare < 0.1f)
                settled = t;
            shown = next;
        }
        return (worst, fastest, settled);
    }

    [Test]
    public void Follow_TheFittedLook_EasesInAboutASecond_AtAnyFrameRate_WhereTheKnobsCrawls()
    {
        float speed = CityParallax.MaxSpeed(Span, Width, 1080f, Depth);
        float fitted = CityParallax.LookShare(0.02f, speed);
        foreach (float fps in new[] { 30f, 60f, 144f })
        {
            (float worst, float fastest, float settled) = Look(fitted, fps);
            Assert.LessOrEqual(worst, 1f + 1e-4f, $"at most 1 px a 30 fps frame at {fps} fps");
            Assert.Less(fastest, 0.8f, $"never at the limit at {fps} fps: an ease, not a crawl");
            Assert.Greater(settled, 0.8f, $"an ease, not a jump, at {fps} fps");
            Assert.Less(settled, 1.5f, $"lands in about 1.2 s at {fps} fps");
        }
        (_, float knobFastest, float knobSettled) = Look(0.02f, 30f);
        Assert.Greater(knobFastest, 0.9f, "the knob's own 0.02 a side runs at the limit");
        Assert.Greater(knobSettled, 5f, "and takes about 6.5 s (the crawl Needs Saleh 4 lists)");
    }

    [Test]
    public void Follow_APointerJumpEdgeToEdge_NeverOutrunsTheLimit_AndLandsOnTheTarget()
    {
        float speed = CityParallax.MaxSpeed(Span, Width, 1080f, Depth);
        foreach (float fps in new[] { 20f, 30f, 60f })
        {
            float dt = 1f / fps, shown = -0.02f, velocity = 0f;
            for (int i = 0; i < fps * 12f; i++)
            {
                float next = CityParallax.Follow(shown, ref velocity, 0.02f, speed, dt);
                Assert.LessOrEqual(System.Math.Abs(next - shown) * PxPerShare, 1f + 1e-4f, $"at most 1 px a frame at {fps} fps (a slow frame too)");
                shown = next;
            }
            Assert.AreEqual(0.02f, shown, 1e-6f, $"lands on the target at {fps} fps");
        }
        float held = 0f;
        Assert.AreEqual(0.3f, CityParallax.Follow(0.3f, ref held, 0.9f, 0f, 1f / 60f), "no limit holds it");
    }
}
