using System;
using System.Collections.Generic;

/// <summary>
/// Who leads the timeline, with hysteresis: a unique top above the floor
/// starts leading; a leader stays until one unique challenger beats it by more
/// than the margin, or until it falls to the keep floor; ties never make or
/// change a leader.
/// </summary>
public static class NationLeader
{
    /// <summary>
    /// The leader's id, or "" for none. <paramref name="ranked"/> is highest
    /// first (ScoreRanking.Rank). An incumbent (non-blank, in the ranking,
    /// above the keep floor) stays unless the best other nation exceeds it by
    /// more than <paramref name="margin"/> and is strictly above the next other
    /// nation. Otherwise the top nation leads when it is strictly above
    /// <paramref name="floor"/> and strictly above the runner-up. A negative
    /// margin counts as 0; a keep floor above the floor counts as the floor.
    /// </summary>
    public static string Decide(string incumbentId, IReadOnlyList<RankedScore> ranked, float floor, float keepFloor, float margin)
    {
        if (ranked == null || ranked.Count == 0)
            return string.Empty;

        float keep = Math.Min(keepFloor, floor);
        float lead = Math.Max(0f, margin);

        int incumbent = -1;
        if (!string.IsNullOrWhiteSpace(incumbentId))
            for (int i = 0; i < ranked.Count && incumbent < 0; i++)
                if (string.Equals(ranked[i].id, incumbentId, StringComparison.Ordinal))
                    incumbent = i;

        if (incumbent >= 0 && ranked[incumbent].score > keep)
        {
            int best = -1, next = -1;
            for (int i = 0; i < ranked.Count; i++)
            {
                if (i == incumbent)
                    continue;
                if (best < 0)
                    best = i;
                else if (next < 0)
                    next = i;
            }

            bool challengerWins = best >= 0 &&
                                  ranked[best].score > ranked[incumbent].score + lead &&
                                  (next < 0 || ranked[best].score > ranked[next].score);
            return challengerWins ? ranked[best].id : ranked[incumbent].id;
        }

        RankedScore top = ranked[0];
        bool unique = ranked.Count == 1 || top.score > ranked[1].score;
        return top.score > floor && unique ? top.id : string.Empty;
    }
}
