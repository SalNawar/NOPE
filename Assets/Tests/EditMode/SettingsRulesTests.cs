using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>The PC basics' rules (BUG-62): volume in decibels, the resolutions offered, stepping, brightness as exposure.</summary>
public class SettingsRulesTests
{
    [TestCase(1f, 0f)]
    [TestCase(0.5f, -6.0206f)]
    [TestCase(0.1f, -20f)]
    [TestCase(0.0001f, -80f)]
    [TestCase(0f, -80f)]
    [TestCase(-1f, -80f)]
    [TestCase(2f, 0f, Description = "never louder than the mix")]
    public void Decibels_IsTwentyLogTen_ClampedToTheMixersRange(float share, float expected)
    {
        Assert.AreEqual(expected, SettingsRules.Decibels(share), 0.001f);
    }

    [Test]
    public void Decibels_NaN_IsSilent()
    {
        Assert.AreEqual(SettingsRules.SilentDecibels, SettingsRules.Decibels(float.NaN));
    }

    [Test]
    public void Resolutions_DistinctSmallestFirst_From720Up_WithTheCurrentOne()
    {
        var offered = new[]
        {
            new ScreenSize(1920, 1080), new ScreenSize(1920, 1080), new ScreenSize(1280, 720), new ScreenSize(800, 600),
            new ScreenSize(2560, 1080), new ScreenSize(1280, 1024), new ScreenSize(0, 0)
        };
        List<ScreenSize> sizes = SettingsRules.Resolutions(offered, new ScreenSize(1600, 900));
        CollectionAssert.AreEqual(new[] { new ScreenSize(1280, 720), new ScreenSize(1280, 1024), new ScreenSize(1600, 900), new ScreenSize(1920, 1080), new ScreenSize(2560, 1080) }, sizes);
        Assert.IsFalse(sizes.Any(s => s.Height < SettingsRules.SmallestHeight), "no size under 720p");
        CollectionAssert.AreEqual(new[] { new ScreenSize(1024, 600) }, SettingsRules.Resolutions(null, new ScreenSize(1024, 600)), "the current size is always offered");
    }

    [Test]
    public void IndexOf_ExactElseNearest()
    {
        var sizes = new List<ScreenSize> { new ScreenSize(1280, 720), new ScreenSize(1920, 1080), new ScreenSize(2560, 1440) };
        Assert.AreEqual(1, SettingsRules.IndexOf(sizes, new ScreenSize(1920, 1080)));
        Assert.AreEqual(1, SettingsRules.IndexOf(sizes, new ScreenSize(1920, 1200)), "nearest by pixel count");
        Assert.AreEqual(0, SettingsRules.IndexOf(sizes, new ScreenSize(640, 480)));
        Assert.AreEqual(0, SettingsRules.IndexOf(new List<ScreenSize>(), new ScreenSize(1920, 1080)));
    }

    [TestCase(1, 1, 3, 2)]
    [TestCase(2, 1, 3, 2, Description = "held at the end")]
    [TestCase(0, -1, 3, 0, Description = "held at the start")]
    [TestCase(0, 1, 0, 0)]
    public void Step_IsHeldAtTheEnds(int index, int step, int count, int expected)
    {
        Assert.AreEqual(expected, SettingsRules.Step(index, step, count));
    }

    [TestCase(0.5f, 0f, Description = "the middle is the game as painted")]
    [TestCase(0f, -1f)]
    [TestCase(1f, 1f)]
    [TestCase(0.75f, 0.5f)]
    [TestCase(-3f, -1f)]
    [TestCase(9f, 1f)]
    public void Exposure_MapsTheSliderToPlusMinusOneEv(float brightness, float expected)
    {
        Assert.AreEqual(expected, SettingsRules.Exposure(brightness), 0.0001f);
    }

    [TestCase(0f, 0)]
    [TestCase(0.806f, 81)]
    [TestCase(1f, 100)]
    [TestCase(1.5f, 100)]
    [TestCase(-0.2f, 0)]
    public void Percent_IsTheWholePercentShown(float share, int expected)
    {
        Assert.AreEqual(expected, SettingsRules.Percent(share));
    }
}
