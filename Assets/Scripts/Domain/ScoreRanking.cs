using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>One ranked score: an id (an attribute of a place, a nation) and its value. Public fields, so the saved ranking serializes with JsonUtility.</summary>
[Serializable]
public struct RankedScore
{
    /// <summary>What is ranked (an attribute id, a nation id).</summary>
    public string id;

    /// <summary>Its score.</summary>
    public float score;

    /// <summary>Creates an entry.</summary>
    public RankedScore(string id, float score)
    {
        this.id = id;
        this.score = score;
    }
}

/// <summary>
/// The one ranking of the timeline: per-place attribute tiers and the nation
/// leader both rank through it (design credited to PR #3).
/// </summary>
public static class ScoreRanking
{
    /// <summary>
    /// A new list, highest score first; equal scores keep their input order (a
    /// stable sort). Null gives an empty list; the input is not modified.
    /// </summary>
    public static List<RankedScore> Rank(IEnumerable<RankedScore> entries) =>
        entries == null ? new List<RankedScore>() : entries.OrderByDescending(e => e.score).ToList();
}

/// <summary>A place attribute's tier (dominance news and tier-gated conditions read it).</summary>
public enum DominanceTier
{
    /// <summary>Below the supporting tier.</summary>
    None,

    /// <summary>Just below the dominant tier.</summary>
    Supporting,

    /// <summary>At the top of the place's ranking.</summary>
    Dominant
}

/// <summary>A place's attribute tiers, ranked with <see cref="ScoreRanking"/>.</summary>
public static class DominanceTiers
{
    /// <summary>
    /// Ranks <paramref name="scores"/> (one per attribute of a place) with
    /// ScoreRanking: the first <paramref name="dominantCount"/> are Dominant,
    /// the next <paramref name="supportingCount"/> Supporting, the rest None.
    /// The result is aligned with the INPUT order (index i is the tier of
    /// scores[i]); ties keep input order. Negative counts count as 0; null
    /// gives an empty array.
    /// </summary>
    public static DominanceTier[] Classify(IReadOnlyList<RankedScore> scores, int dominantCount, int supportingCount)
    {
        if (scores == null)
            return Array.Empty<DominanceTier>();

        int dominant = Math.Max(0, dominantCount);
        int supporting = Math.Max(0, supportingCount);

        // Rank the input positions (the index rides in the id), so equal ids never confuse the alignment.
        List<RankedScore> ranked = ScoreRanking.Rank(scores.Select((s, i) => new RankedScore(i.ToString(CultureInfo.InvariantCulture), s.score)));
        var tiers = new DominanceTier[scores.Count];
        for (int rank = 0; rank < ranked.Count; rank++)
            tiers[int.Parse(ranked[rank].id, CultureInfo.InvariantCulture)] = rank < dominant ? DominanceTier.Dominant
                : rank < dominant + supporting ? DominanceTier.Supporting
                : DominanceTier.None;
        return tiers;
    }
}
