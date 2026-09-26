using System;
using System.Collections.Generic;

/// <summary>
/// A paper's printed barcode (PC spec FO5): drawn by code from the serial,
/// decorative and not decodable. A guard (bar, space, bar) at each end and
/// bars of one to three modules between, spaced one to three, from an FNV-1a
/// hash of the serial, so the same serial always prints the same code and two
/// serials print two codes. Exactly <c>modules</c> wide.
/// </summary>
public static class Barcode
{
    /// <summary>The bars of <paramref name="serial"/>'s code, as (start, width) in modules, left to right; the last bar ends at <paramref name="modules"/>.</summary>
    public static IReadOnlyList<(int start, int width)> Bars(string serial, int modules)
    {
        modules = Math.Max(7, modules);
        var bars = new List<(int start, int width)> { (0, 1), (2, 1) };
        uint state = Hash(serial ?? string.Empty);
        int pos = 3;
        int end = modules - 3;
        while (true)
        {
            int space = 1 + (int)(Next(ref state) % 3u);
            int width = 1 + (int)(Next(ref state) % 3u);
            if (pos + space + width > end - 1)
                break;
            bars.Add((pos + space, width));
            pos += space + width;
        }
        bars.Add((modules - 3, 1));
        bars.Add((modules - 1, 1));
        return bars;
    }

    /// <summary>FNV-1a over the serial's characters.</summary>
    private static uint Hash(string text)
    {
        uint h = 2166136261u;
        foreach (char c in text)
        {
            h ^= c;
            h *= 16777619u;
        }
        return h == 0u ? 0x9E3779B9u : h;
    }

    /// <summary>One xorshift32 step.</summary>
    private static uint Next(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state;
    }
}
