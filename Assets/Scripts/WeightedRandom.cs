using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Small utility for weighted random selection.
/// </summary>
public static class WeightedRandom
{
    /// <summary>
    /// Picks one element from a list using a weight selector.
    /// Returns default(T) if list is null/empty or all weights are non-positive.
    /// </summary>
    public static T Pick<T>(IReadOnlyList<T> items, Func<T, float> weightSelector)
    {
        if (items == null || items.Count == 0 || weightSelector == null)
            return default;

        float total = 0f;

        for (int i = 0; i < items.Count; i++)
        {
            float w = Mathf.Max(0f, weightSelector(items[i]));
            total += w;
        }

        if (total <= 0f)
            return default;

        float roll = UnityEngine.Random.value * total;
        float acc = 0f;

        for (int i = 0; i < items.Count; i++)
        {
            float w = Mathf.Max(0f, weightSelector(items[i]));
            acc += w;

            if (roll <= acc)
                return items[i];
        }

        return items[items.Count - 1];
    }
}
