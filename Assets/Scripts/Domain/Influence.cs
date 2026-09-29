using System;
using System.Collections.Generic;

/// <summary>A place by its ids (nation, era); equal by both ids (a portal's route compares with a traveller's destination by it).</summary>
public readonly struct PlaceRef : IEquatable<PlaceRef>
{
    /// <summary>The place's nation id.</summary>
    public readonly string NationId;

    /// <summary>The place's era id.</summary>
    public readonly string EraId;

    /// <summary>Creates a place reference.</summary>
    public PlaceRef(string nationId, string eraId)
    {
        NationId = nationId;
        EraId = eraId;
    }

    /// <summary>True when both ids match (ordinal).</summary>
    public bool Equals(PlaceRef other) => string.Equals(NationId, other.NationId, StringComparison.Ordinal) && string.Equals(EraId, other.EraId, StringComparison.Ordinal);

    /// <summary>True for an equal PlaceRef.</summary>
    public override bool Equals(object obj) => obj is PlaceRef other && Equals(other);

    /// <summary>A hash of both ids.</summary>
    public override int GetHashCode() => ((NationId ?? string.Empty).GetHashCode() * 397) ^ (EraId ?? string.Empty).GetHashCode();

    /// <summary>The place as "nation:era" (the content checks' messages name a route by it).</summary>
    public override string ToString() => NationId + ":" + EraId;

    /// <summary>Equal by both ids.</summary>
    public static bool operator ==(PlaceRef a, PlaceRef b) => a.Equals(b);

    /// <summary>Not equal by either id.</summary>
    public static bool operator !=(PlaceRef a, PlaceRef b) => !a.Equals(b);
}

/// <summary>
/// A nation's influence on the past: the attribute deltas stored for its
/// places outside the Future. Baselines, nation scores and global totals are
/// not influence (AddNationScore is not a lever).
/// </summary>
public static class Influence
{
    /// <summary>
    /// One entry per <paramref name="nationIds"/> entry, in that order (0 for a
    /// nation with no key; the caller ranks them). Each score key is parsed
    /// with ScoreKey: a ProfileAttr key belongs to
    /// <paramref name="placeOfProfile"/>(profileId) (an unknown profile is
    /// skipped), an AdHocAttr key to its parsed nation and era; every other
    /// kind is skipped, as are keys in <paramref name="excludedEraId"/> (the
    /// Future) and nations not listed. Null inputs count as empty.
    /// </summary>
    public static List<RankedScore> ByNation(IReadOnlyList<string> nationIds, IEnumerable<KeyValuePair<string, float>> scores,
                                             Func<string, PlaceRef?> placeOfProfile, string excludedEraId)
    {
        var result = new List<RankedScore>();
        if (nationIds == null)
            return result;

        var totals = new Dictionary<string, float>(StringComparer.Ordinal);
        foreach (string id in nationIds)
            if (id != null && !totals.ContainsKey(id))
                totals.Add(id, 0f);

        if (scores != null)
        {
            foreach (KeyValuePair<string, float> s in scores)
            {
                if (!ScoreKey.TryParse(s.Key, out ParsedScoreKey key))
                    continue;

                PlaceRef? place = key.kind == ScoreKeyKind.ProfileAttr ? placeOfProfile?.Invoke(key.profileId)
                    : key.kind == ScoreKeyKind.AdHocAttr ? new PlaceRef(key.nationId, key.eraId)
                    : (PlaceRef?)null;

                if (place == null || place.Value.NationId == null || string.Equals(place.Value.EraId, excludedEraId, StringComparison.Ordinal))
                    continue;

                if (totals.ContainsKey(place.Value.NationId))
                    totals[place.Value.NationId] += s.Value;
            }
        }

        foreach (string id in nationIds)
            result.Add(new RankedScore(id, id != null && totals.TryGetValue(id, out float total) ? total : 0f));
        return result;
    }
}
