using System;
using System.Collections.Generic;

/// <summary>
/// The desk rulebook's RULES page split into sheets (leftover 2026-10-08:
/// day 12's six rules only fitted the page at a size too small to read at
/// 720p): each sheet takes as many of the rules left, in order, as fit it
/// (<c>fits(start, count)</c>: the rows laid at the least readable size stay
/// on the page), at most <c>rowsPerSheet</c> and at least one; the next
/// sheet starts with the first that did not fit. Pure.
/// </summary>
public static class RuleSheets
{
    /// <summary>The first rule of each sheet for <paramref name="total"/> rules (one empty sheet for none).</summary>
    public static List<int> Starts(int total, int rowsPerSheet, Func<int, int, bool> fits)
    {
        var starts = new List<int> { 0 };
        if (total <= 0 || rowsPerSheet <= 0 || fits == null)
            return starts;
        int start = 0;
        while (true)
        {
            int count = Math.Min(rowsPerSheet, total - start);
            while (count > 1 && !fits(start, count))
                count--;
            start += count;
            if (start >= total)
                return starts;
            starts.Add(start);
        }
    }
}
