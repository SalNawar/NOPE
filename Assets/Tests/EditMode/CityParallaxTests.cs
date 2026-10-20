using NUnit.Framework;

/// <summary>
/// The city view's parallax speed limit (BUG-69; GAME_FEEL_BIBLE H1): the
/// pan follows its target by at most one pixel at 1080p a frame on its
/// nearest layer, at any screen size.
/// </summary>
public class CityParallaxTests
{
    [Test]
    public void MaxStep_IsOnePixelAt1080p_OnTheNearestLayer()
    {
        const float span = 0.86f, width = 3000f, depth = 1.25f;
        float step = CityParallax.MaxStep(span, width, 1080f, depth);
        Assert.AreEqual(1f, step / span * width * depth, 1e-4f, "one pixel on the traffic, panning 1.25x the painting");
        float small = CityParallax.MaxStep(span, width * 720f / 1080f, 720f, depth);
        Assert.AreEqual(step, small, 1e-7f, "the same pixel at 1080p on a 720p screen");
        Assert.AreEqual(CityParallax.MaxStep(span, width, 1080f, 1f), CityParallax.MaxStep(span, width, 1080f, 0.5f), "a layer never pans less than the painting");
        Assert.AreEqual(0f, CityParallax.MaxStep(0f, width, 1080f, depth));
        Assert.AreEqual(0f, CityParallax.MaxStep(span, 0f, 1080f, depth));
    }

    [Test]
    public void Step_FollowsByAtMostTheStep_AndLandsExactly()
    {
        Assert.AreEqual(0.01f, CityParallax.Step(0f, 0.5f, 0.01f), 1e-7f);
        Assert.AreEqual(-0.01f, CityParallax.Step(0f, -0.5f, 0.01f), 1e-7f);
        Assert.AreEqual(0.005f, CityParallax.Step(0f, 0.005f, 0.01f), "lands on a near target");
        Assert.AreEqual(0.3f, CityParallax.Step(0.3f, 0.9f, 0f), "no step holds it");
    }
}
