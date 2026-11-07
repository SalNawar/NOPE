using System.Collections.Generic;

/// <summary>
/// The run's famous draw (D12, premades-52): each day the run seats up to
/// the day plan's famousPerDay famous travellers from the day's pool, the
/// ones not met this run, picked on the day's famous stream
/// (Seeds.ForFamous), each in a slot no forced appearance holds. The pool's
/// per-slot roll and the authored forced slots stay as they are, so every
/// premade with art is reachable in a run without moving a story beat.
/// </summary>
public static class FamousDraw
{
    /// <summary>The most famous travellers one day's draw seats.</summary>
    public const int MaxPerDay = 2;

    /// <summary>
    /// The day's drawn premade ids: up to <paramref name="perDay"/> (clamped
    /// to 0..MaxPerDay) distinct ids of <paramref name="pool"/> that are not
    /// in <paramref name="unavailable"/> (met this run, or standing today in
    /// a forced slot), each one Range over the ids still eligible, in pool
    /// order; fewer when fewer are eligible.
    /// </summary>
    public static List<string> Pick(IReadOnlyList<string> pool, ICollection<string> unavailable, int perDay, IRandomSource rng)
    {
        var eligible = new List<string>();
        if (pool != null)
            foreach (string id in pool)
                if (!string.IsNullOrWhiteSpace(id) && (unavailable == null || !unavailable.Contains(id)) && !eligible.Contains(id))
                    eligible.Add(id);
        int count = perDay < 0 ? 0 : perDay > MaxPerDay ? MaxPerDay : perDay;
        var picked = new List<string>();
        while (picked.Count < count && eligible.Count > 0)
        {
            int i = rng.Range(0, eligible.Count);
            picked.Add(eligible[i]);
            eligible.RemoveAt(i);
        }
        return picked;
    }
}
