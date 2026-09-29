using NUnit.Framework;

/// <summary>
/// Redesign phase 23 part 1b (Saleh: "stability should be much more resistant, let's have it x.xx and values
/// change slowly but can compound"): stability in hundredths, each change a share of the current value (a loss)
/// or of the gap to 100 (a gain), so changes compound.
/// </summary>
public class StabilityRulesTests
{
    [TestCase(100f, -5f, 0.005f, 97.5f, Description = "5 points at 0.5 % a point: 2.5 % of 100")]
    [TestCase(97.5f, -5f, 0.005f, 95.06f, Description = "the next loss is 2.5 % of 97.5 (95.0625), in hundredths")]
    [TestCase(50f, -15f, 0.005f, 46.25f, Description = "a premade's 5 + 10 points: 7.5 % of 50")]
    [TestCase(90f, 4f, 0.005f, 90.2f, Description = "a gain closes 2 % of the gap to 100")]
    [TestCase(99.99f, 50f, 0.005f, 99.99f, Description = "25 % of a 0.01 gap rounds away: never 100 by gains")]
    [TestCase(80f, 0f, 0.005f, 80f, Description = "no change")]
    [TestCase(80f, -5f, 0f, 80f, Description = "a rate of 0 moves nothing")]
    [TestCase(80f, -300f, 0.005f, 0f, Description = "a share past the whole takes it all, never below 0")]
    [TestCase(80f, 300f, 0.005f, 100f, Description = "and never above 100")]
    public void Apply_ASharePerPoint_OfTheValueOrTheGap(float current, float points, float rate, float expected)
    {
        Assert.AreEqual(expected, StabilityRules.Apply(current, points, rate), 0.0001f);
    }

    [Test]
    public void Apply_Compounds_EachLossSmallerThanTheOneBefore()
    {
        float s = 100f, lastLoss = float.MaxValue;
        for (int i = 0; i < 20; i++)
        {
            float next = StabilityRules.Apply(s, -5f, 0.005f);
            float loss = s - next;
            Assert.Less(loss, lastLoss + 0.0001f, $"loss {i + 1} ({loss}) is no bigger than the one before ({lastLoss})");
            Assert.AreEqual(StabilityRules.Round(next), next, "kept in hundredths");
            lastLoss = loss;
            s = next;
        }
        Assert.AreEqual(60.27f, s, 0.02f, "20 losses of 2.5 %: 100 x 0.975^20");
    }

    /// <summary>An effect's AddStability carries its own size as the share, in percent.</summary>
    [Test]
    public void ApplyPercent_TheShareItself()
    {
        Assert.AreEqual(90f, StabilityRules.ApplyPercent(100f, -10f), 0.0001f, "10 % of 100 lost");
        Assert.AreEqual(92f, StabilityRules.ApplyPercent(90f, 20f), 0.0001f, "20 % of the gap of 10 gained");
        Assert.AreEqual(StabilityRules.Apply(64f, -6f, 0.005f), StabilityRules.ApplyPercent(64f, -3f), 0.0001f, "Apply is ApplyPercent of rate x points");
    }

    [TestCase(97.435f, 97.44f)]
    [TestCase(97.434f, 97.43f)]
    [TestCase(-3f, 0f)]
    [TestCase(123.456f, 100f)]
    public void Round_ToHundredths_Within0And100(float value, float expected)
    {
        Assert.AreEqual(expected, StabilityRules.Round(value), 0.0001f);
    }

    [TestCase(97.43f, "97.43%")]
    [TestCase(100f, "100.00%")]
    [TestCase(0f, "0.00%")]
    [TestCase(60.2749f, "60.27%")]
    public void Format_TwoDecimalsAndThePercentSign(float value, string expected)
    {
        Assert.AreEqual(expected, StabilityRules.Format(value));
    }

    [TestCase(-2.5f, "-2.50")]
    [TestCase(0.2f, "+0.20")]
    [TestCase(0f, "0.00")]
    [TestCase(-0.004f, "0.00", Description = "under a hundredth reads as no change")]
    public void FormatChange_SignedTwoDecimals(float delta, string expected)
    {
        Assert.AreEqual(expected, StabilityRules.FormatChange(delta));
    }

    /// <summary>The office readout's tint, read against the firing line so it moves with it.</summary>
    [TestCase(90f, StabilityBand.Normal)]
    [TestCase(70.01f, StabilityBand.Normal)]
    [TestCase(70f, StabilityBand.Warning, Description = "within 10 points of the line at 60")]
    [TestCase(63.01f, StabilityBand.Warning)]
    [TestCase(63f, StabilityBand.Critical, Description = "within 3 points")]
    [TestCase(55f, StabilityBand.Critical, Description = "at or under the line")]
    public void Band_ByTheDistanceToTheFiringLine(float stability, StabilityBand expected)
    {
        Assert.AreEqual(expected, StabilityRules.Band(stability, 60f, 10f, 3f));
    }

    /// <summary>The save keeps stability as whole hundredths (9743 = 97.43 %), so it reads back exactly.</summary>
    [TestCase(97.43f, 9743)]
    [TestCase(100f, 10000)]
    [TestCase(95.0625f, 9506)]
    [TestCase(-4f, 0)]
    [TestCase(250f, 10000)]
    public void Hundredths_RoundTrip(float value, int hundredths)
    {
        Assert.AreEqual(hundredths, StabilityRules.ToHundredths(value));
        Assert.AreEqual(StabilityRules.Round(value), StabilityRules.FromHundredths(hundredths), 0.00001f);
        Assert.AreEqual(hundredths, StabilityRules.ToHundredths(StabilityRules.FromHundredths(hundredths)));
    }
}
