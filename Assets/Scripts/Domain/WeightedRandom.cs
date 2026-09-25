using System;
using System.Collections.Generic;

/// <summary>
/// Small utility for weighted random selection (pure; the caller supplies the
/// random source, so picks can be seeded).
/// </summary>
public static class WeightedRandom
{
    /// <summary>
    /// Picks one element from a list using a weight selector. Items with a
    /// weight of zero or less are never picked. Returns default(T) if the list
    /// is null/empty, the selector or source is null, or all weights are non-positive.
    /// </summary>
    public static T Pick<T>(IReadOnlyList<T> items, Func<T, float> weightSelector, IRandomSource rng)
    {
        if (items == null || items.Count == 0 || weightSelector == null || rng == null)
            return default;

        float total = 0f;

        for (int i = 0; i < items.Count; i++)
            total += Math.Max(0f, weightSelector(items[i]));

        if (total <= 0f)
            return default;

        float roll = rng.Value() * total; // [0, total)
        float acc = 0f;
        int lastPositive = -1;

        for (int i = 0; i < items.Count; i++)
        {
            float w = Math.Max(0f, weightSelector(items[i]));
            if (w <= 0f)
                continue;

            lastPositive = i;
            acc += w;

            if (roll < acc)
                return items[i];
        }

        // Float rounding can leave roll == acc at the very end: take the last weighted item.
        return lastPositive >= 0 ? items[lastPositive] : default;
    }
}
