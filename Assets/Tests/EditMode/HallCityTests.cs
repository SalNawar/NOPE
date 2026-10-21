using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The living city's windows and traffic never toggle (HALL-FX, BUG-13; HallCity):
/// no window cell's moment is 0 over the whole grid, a window's light is a
/// function of the lit share (the shift clock's hour) alone and rises once,
/// the fade band follows the clock's pace and holds while the clock does, the
/// flyers fade at their lanes' ends, and the night's traffic is lit darker and
/// bluer than noon's.
/// </summary>
public class HallCityTests
{
    /// <summary>The shader's window grid: floor(uv * float2(240, 120)).</summary>
    private const int CellsAcross = 240, CellsDown = 120;

    [Test]
    public void TheWindowHash_IsNeverZero_OverTheWholeGrid_AndSpreads()
    {
        var count = new Dictionary<float, int>();
        int low = 0;
        for (int y = 0; y < CellsDown; y++)
            for (int x = 0; x < CellsAcross; x++)
            {
                float h = HallCity.WindowHash(x, y);
                Assert.Greater(h, 0f, $"cell ({x},{y}) would light before the first light (the 6 Hz blink of BUG-13)");
                Assert.Less(h, 1f, $"cell ({x},{y})");
                count[h] = count.TryGetValue(h, out int n) ? n + 1 : 1;
                if (h <= HallCity.HashFloor)
                    low++;
            }
        int most = 0;
        foreach (int n in count.Values)
            most = System.Math.Max(most, n);
        Assert.LessOrEqual(most, 2, "no crowd of cells shares one moment (the old hash gave up to 324 cells one moment: they lit together)");
        Assert.AreEqual(0, low, "none piled on the floor (the old hash had 600 cells at exactly 0)");
    }

    [Test]
    public void AWindowsLight_IsAFunctionOfTheLitShareAlone_RisingOnce()
    {
        const float hash = 0.4f, width = 0.003f;
        Assert.AreEqual(0f, HallCity.WindowLevel(0f, HallCity.HashFloor, width), "by day nothing is lit, not even the earliest window");
        Assert.AreEqual(0f, HallCity.WindowLevel(hash * HallCity.Spread - 0.0001f, hash, width), "before its moment");
        Assert.AreEqual(1f, HallCity.WindowLevel(hash * HallCity.Spread + width, hash, width), 1e-4f, "lit across the band");
        Assert.AreEqual(1f, HallCity.WindowLevel(0.9f, hash, width), "and stays lit");
        float last = 0f;
        for (float lit = 0f; lit <= 1f; lit += 0.0001f)
        {
            float level = HallCity.WindowLevel(lit, hash, width);
            Assert.GreaterOrEqual(level, last, $"it never dims as the evening goes on (at {lit})");
            last = level;
        }
        // Real time is no input: the same share always gives the same light (a frozen clock freezes every window).
        Assert.AreEqual(HallCity.WindowLevel(0.39f, hash, width), HallCity.WindowLevel(0.39f, hash, width));
    }

    [Test]
    public void TheFadeBand_FollowsTheClocksPace_AndHoldsWhileItHolds()
    {
        // The share runs 0.01 a second: a 0.4 s fade is 0.004 of it.
        float width = HallCity.MinFadeWidth, lit = 0.3f;
        for (int i = 0; i < 300; i++)
        {
            float next = lit + 0.01f / 30f;
            width = HallCity.NextFadeWidth(width, lit, next, 1f / 30f);
            lit = next;
        }
        Assert.AreEqual(0.01f * HallCity.FadeSeconds, width, 2e-4f, "about FadeSeconds of the clock's pace");

        float held = width;
        for (int i = 0; i < 300; i++)
            held = HallCity.NextFadeWidth(held, lit, lit, 1f / 30f);
        Assert.AreEqual(width, held, "a frozen clock (or a forced hour) holds the band: nothing on screen changes");

        float jumped = HallCity.NextFadeWidth(width, 0.1f, 0.9f, 1f / 30f);
        Assert.LessOrEqual(jumped, HallCity.MaxFadeWidth, "a jump of the hour never smears the windows");
        Assert.Less(jumped - width, 0.05f, "and the band eases toward its new width: no window pops");
    }

    [Test]
    public void AFlyer_FadesAtItsLanesEnds_SoTheWrapNeverPops()
    {
        Assert.AreEqual(0f, HallCity.LaneEndFade(0f, 0.06f), 1e-6f, "in at the start");
        Assert.AreEqual(0f, HallCity.LaneEndFade(1f, 0.06f), 1e-6f, "the wrap (1 is 0 again)");
        Assert.AreEqual(1f, HallCity.LaneEndFade(0.5f, 0.06f), 1e-6f);
        Assert.Less(HallCity.LaneEndFade(0.999f, 0.06f), 0.01f, "out at the end");
        Assert.AreEqual(1f, HallCity.LaneEndFade(0.3f, 0f), "no fade asked: no fade");
    }

    [Test]
    public void TheNightsTraffic_IsLitDarkerAndBluerThanNoons()
    {
        var (nr, ng, nb) = HallCity.HourLight(HallBakedCycle.Weights(22f));
        var (dr, dg, db) = HallCity.HourLight(HallBakedCycle.Weights(12f));
        float night = 0.299f * nr + 0.587f * ng + 0.114f * nb, noon = 0.299f * dr + 0.587f * dg + 0.114f * db;
        Assert.LessOrEqual(night, 0.5f * noon, "no daylight-bright car over the night city (CODE-69: mean luma at 22:00 at most half of noon's)");
        Assert.Greater(nb, nr, "the night's tint is blue, as the city's");
        Assert.AreEqual((1f, 1f, 1f), HallCity.HourLight(new HallBakedCycle.Weights4(0f, 0f, 0f, 0f)), "no weights: unlit as drawn");
    }
}
