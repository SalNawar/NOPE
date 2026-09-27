using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// The Investigation app's zoom (the PC redesign KB5): the panes' content at
/// one of the levels (DesktopConfigSO: 100, 125 and 150 %). Ctrl+= and Ctrl+-
/// step to the next level that way (stopping at the ends); Ctrl+0 goes back
/// to the player's default (Settings' "Text size", saved as its number). Pure.
/// </summary>
public static class AppZoom
{
    /// <summary>The level with no levels given.</summary>
    public const int Normal = 100;

    /// <summary>The next level above (<paramref name="direction"/> 1) or below (-1) <paramref name="current"/>; the end level when there is none that way.</summary>
    public static int Step(IReadOnlyList<int> levels, int current, int direction)
    {
        if (levels == null || levels.Count == 0)
            return Normal;

        int best = direction >= 0 ? int.MaxValue : int.MinValue;
        foreach (int level in levels)
            if (direction >= 0 ? level > current && level < best : level < current && level > best)
                best = level;
        if (best != int.MaxValue && best != int.MinValue)
            return best;

        // Nothing further that way: the end level on that side.
        int end = levels[0];
        foreach (int level in levels)
            if (direction >= 0 ? level > end : level < end)
                end = level;
        return end;
    }

    /// <summary>A saved level read back: one of the levels, else the first level (the default).</summary>
    public static int Parse(string saved, IReadOnlyList<int> levels)
    {
        if (levels == null || levels.Count == 0)
            return Normal;
        if (int.TryParse(saved != null ? saved.Trim() : null, NumberStyles.Integer, CultureInfo.InvariantCulture, out int level))
            foreach (int known in levels)
                if (known == level)
                    return level;
        return levels[0];
    }

    /// <summary>The level as a scale factor (125 → 1.25).</summary>
    public static float Scale(int level) => level / 100f;
}
