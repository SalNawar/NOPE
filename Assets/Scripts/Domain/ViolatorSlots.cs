using System.Collections.Generic;

/// <summary>
/// Where a day's guaranteed rule violators stand in the queue: distinct slots
/// in the first half (rounded up), so closing time rarely hides them. Pure and
/// seeded, so the same day always places them the same way.
/// </summary>
public static class ViolatorSlots
{
    /// <summary>How many leading slots can hold a guaranteed violator (the first half, rounded up).</summary>
    public static int Window(int queueSize) => queueSize <= 0 ? 0 : (queueSize + 1) / 2;

    /// <summary>
    /// Up to <paramref name="violators"/> distinct 1-based slots in the first
    /// half of a queue of <paramref name="queueSize"/> minus
    /// <paramref name="excludedSlots"/> (the forced premades' slots), in draw
    /// order (capped by that pool; empty when either count is not positive).
    /// With no exclusions the pool, the draws and the result are the original.
    /// </summary>
    public static int[] Pick(int queueSize, int violators, IRandomSource rng, ICollection<int> excludedSlots = null)
    {
        if (queueSize <= 0 || violators <= 0 || rng == null)
            return new int[0];

        var open = new List<int>();
        for (int slot = 1; slot <= Window(queueSize); slot++)
            if (excludedSlots == null || !excludedSlots.Contains(slot))
                open.Add(slot);

        int window = open.Count;
        int count = violators < window ? violators : window;
        int[] pool = open.ToArray();

        // Partial Fisher-Yates: the first `count` entries become the picks.
        for (int i = 0; i < count; i++)
        {
            int j = rng.Range(i, window);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        var slots = new int[count];
        System.Array.Copy(pool, slots, count);
        return slots;
    }
}
