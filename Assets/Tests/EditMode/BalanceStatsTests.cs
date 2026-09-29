using NUnit.Framework;

/// <summary>Redesign phase 23: the balance report's statistics and the epilogue thresholds' rule (the history-facts design's R14).</summary>
public class BalanceStatsTests
{
    private static readonly float[] Ten = { 10, 1, 9, 2, 8, 3, 7, 4, 6, 5 };

    [TestCase(0f, 1f)]
    [TestCase(0.5f, 6f)]
    [TestCase(0.65f, 7f, Description = "floor(0.65 x 10) = the 7th smallest")]
    [TestCase(0.99f, 10f)]
    [TestCase(1f, 10f, Description = "past the end: the largest")]
    public void Quantile_TheSortedValueAtFloorQN(float q, float expected)
    {
        Assert.AreEqual(expected, BalanceStats.Quantile(Ten, q));
    }

    [Test]
    public void MeanAndStandardDeviation_OverThePopulation()
    {
        Assert.AreEqual(5.5f, BalanceStats.Mean(Ten), 1e-5f);
        Assert.AreEqual(2.8723f, BalanceStats.StandardDeviation(Ten), 1e-3f);
    }

    [Test]
    public void NoValues_ReadZero()
    {
        Assert.AreEqual(0f, BalanceStats.Quantile(new float[0], 0.5f));
        Assert.AreEqual(0f, BalanceStats.Mean(null));
        Assert.AreEqual(0f, BalanceStats.StandardDeviation(new float[0]));
        Assert.AreEqual(0, BalanceStats.EpilogueThreshold(null));
    }

    [Test]
    public void EpilogueThreshold_The65thPercentile_RoundedHalfAwayFromZero()
    {
        Assert.AreEqual(7, BalanceStats.EpilogueThreshold(Ten));
        Assert.AreEqual(12, BalanceStats.EpilogueThreshold(new[] { 11.5f, 11.5f, 11.5f }), "11.5 rounds up");
        Assert.AreEqual(-12, BalanceStats.EpilogueThreshold(new[] { -11.5f }), "and away from zero below it");
        Assert.AreEqual(0.65f, BalanceStats.EpilogueQuantile);
    }
}
