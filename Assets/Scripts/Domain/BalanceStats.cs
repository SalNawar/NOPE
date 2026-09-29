using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The balance report's statistics (redesign phase 23): the mean, the spread,
/// quantiles, and the rule the epilogue thresholds are set by. Pure, so the
/// numbers the report proposes are tested headless.
/// </summary>
public static class BalanceStats
{
    /// <summary>
    /// The quantile the epilogue thresholds are read at (the history-facts
    /// design's R14): each threshold is this quantile of perfect play's
    /// day-15 totals for its attribute, so about a third of perfect runs reach it.
    /// </summary>
    public const float EpilogueQuantile = 0.65f;

    /// <summary>The <paramref name="q"/>-quantile: the sorted value at floor(q x n), the largest past the end; 0 for no values.</summary>
    public static float Quantile(IEnumerable<float> values, float q)
    {
        List<float> sorted = values != null ? values.OrderBy(v => v).ToList() : new List<float>();
        if (sorted.Count == 0)
            return 0f;
        int index = (int)Math.Floor(q * sorted.Count);
        return sorted[Math.Max(0, Math.Min(sorted.Count - 1, index))];
    }

    /// <summary>The mean; 0 for no values.</summary>
    public static float Mean(IEnumerable<float> values)
    {
        List<float> v = values != null ? values.ToList() : new List<float>();
        return v.Count == 0 ? 0f : (float)v.Average(x => (double)x);
    }

    /// <summary>The population standard deviation; 0 for no values.</summary>
    public static float StandardDeviation(IEnumerable<float> values)
    {
        List<float> v = values != null ? values.ToList() : new List<float>();
        if (v.Count == 0)
            return 0f;
        double mean = v.Average(x => (double)x);
        return (float)Math.Sqrt(v.Sum(x => (x - mean) * (x - mean)) / v.Count);
    }

    /// <summary>An epilogue's threshold from perfect play's day-15 totals: the <see cref="EpilogueQuantile"/> quantile, rounded to a whole number (half away from zero); 0 for no values.</summary>
    public static int EpilogueThreshold(IEnumerable<float> totals) =>
        (int)Math.Round(Quantile(totals, EpilogueQuantile), MidpointRounding.AwayFromZero);
}
