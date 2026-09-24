/// <summary>
/// Where a day's guaranteed rule violators stand in the queue: distinct slots
/// in the first half (rounded up), so closing time rarely hides them. Pure and
/// seeded, so the same day always places them the same way.
/// </summary>
public static class ViolatorSlots
{
    /// <summary>
    /// Up to <paramref name="violators"/> distinct 1-based slots in the first
    /// half of a queue of <paramref name="queueSize"/>, in draw order (capped
    /// at the half; empty when either count is not positive).
    /// </summary>
    public static int[] Pick(int queueSize, int violators, IRandomSource rng)
    {
        if (queueSize <= 0 || violators <= 0 || rng == null)
            return new int[0];

        int window = (queueSize + 1) / 2;
        int count = violators < window ? violators : window;

        var pool = new int[window];
        for (int i = 0; i < window; i++)
            pool[i] = i + 1;

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
