using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The balance report's statistics (redesign phase 23): the mean and
/// quantiles of the runs' wallets and counts. The epilogue thresholds' rule
/// and the spread it reported retired with the attribute epilogues
/// (2026-09-29). Pure, so the numbers are tested headless.
/// </summary>
public static class BalanceStats
{
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
}
