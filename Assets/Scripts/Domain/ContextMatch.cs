using System.Collections.Generic;

/// <summary>
/// How well a content row's context fits a traveller (the personalities
/// spec's V3): the kinds the row names (none: any kind) and the era it names
/// (blank: any era). A named kind scores 2 and a named era 1, summed, so the
/// row that says more about who speaks wins; a row naming another kind or
/// another era never matches. The one matcher of the questions' answer
/// overrides (InterviewQuestion.AnswerFor) and of the voice rows (phase V2).
/// Pure.
/// </summary>
public static class ContextMatch
{
    /// <summary>The score of a row that does not match: below every match's.</summary>
    public const int NoMatch = -1;

    /// <summary>What named kinds add to a matching row's score.</summary>
    public const int KindsScore = 2;

    /// <summary>What a named era adds to a matching row's score.</summary>
    public const int EraScore = 1;

    /// <summary>
    /// The score of a row naming <paramref name="kinds"/> (null or empty: any
    /// kind) and <paramref name="era"/> (blank: any era) for a traveller of
    /// <paramref name="kind"/> claiming <paramref name="eraId"/>: 0 for a row
    /// naming neither, plus <see cref="KindsScore"/> when it names the kind,
    /// plus <see cref="EraScore"/> when it names the era; <see cref="NoMatch"/>
    /// when it names other kinds or another era (a named era never matches a
    /// traveller with none).
    /// </summary>
    public static int Score(IReadOnlyList<TravellerKind> kinds, string era, TravellerKind kind, string eraId)
    {
        int score = 0;
        if (kinds != null && kinds.Count > 0)
        {
            if (!Contains(kinds, kind))
                return NoMatch;
            score += KindsScore;
        }

        if (!string.IsNullOrWhiteSpace(era))
        {
            if (!string.Equals(era, eraId, System.StringComparison.Ordinal))
                return NoMatch;
            score += EraScore;
        }

        return score;
    }

    private static bool Contains(IReadOnlyList<TravellerKind> kinds, TravellerKind kind)
    {
        for (int i = 0; i < kinds.Count; i++)
            if (kinds[i] == kind)
                return true;
        return false;
    }
}
