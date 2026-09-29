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

    /// <summary>
    /// The first half's room (days 7-15 B11, V10): a warning when the window
    /// (<see cref="Window"/> of <paramref name="queueSize"/>) holds fewer free
    /// slots than <paramref name="guarantees"/> (the day's guaranteed faulty
    /// travellers, Directives.Guarantees), once <paramref name="forcedSlots"/>
    /// (the slots a forced premade or an authored fault may stand in, each
    /// counted once; <see cref="Pick"/> skips them) are taken: a violator would
    /// be dropped. Forced slots past the window take no room. Empty when the
    /// room holds, as Generate World and the validator both read it.
    /// </summary>
    public static List<string> RoomProblems(string day, int queueSize, IEnumerable<int> forcedSlots, int guarantees)
    {
        var problems = new List<string>();
        int window = Window(queueSize);
        var taken = new HashSet<int>();
        foreach (int slot in forcedSlots ?? new int[0])
            if (slot >= 1 && slot <= window)
                taken.Add(slot);

        int free = window - taken.Count;
        if (free < guarantees)
            problems.Add($"Day '{day}': the first half of its queue ({window} slots) has {free} free once the forced slots [{string.Join(", ", taken)}] are taken, fewer than its {guarantees} guaranteed faulty travellers, so one would be dropped. Move a forced slot past slot {window}, or lengthen the queue.");
        return problems;
    }
}
