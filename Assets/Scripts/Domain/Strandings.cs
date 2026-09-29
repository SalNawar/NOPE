using System;
using System.Collections.Generic;

/// <summary>
/// A traveller stranded in the past (the traveller-types spec's S1-S2),
/// reported in the next morning's paper (Strandings.Lines) and then cleared.
/// </summary>
[Serializable]
public sealed class StrandingRecord
{
    /// <summary>The traveller's display name.</summary>
    public string travellerName;

    /// <summary>The destination's label, where they are lost.</summary>
    public string placeLabel;

    /// <summary>The day of the shift that sent them.</summary>
    public int day;
}

/// <summary>
/// Cheap transponders fail (the traveller-types spec's S1-S3; redesign phase
/// 13b): at the shift's end each accepted traveller whose real transponder
/// (the account's, whatever the manifest claims) is Economy is rolled on the
/// day's stranding stream (<see cref="Seeds.ForStrandings"/>), in queue order,
/// one draw each; a stranded traveller does not come back, carries the
/// present's Technology into the destination through the carries (S2) and
/// makes the next morning's paper. A stranding fines nothing (redesign phase
/// 23: the clerk's only fine is the one wrong-decision penalty,
/// VerdictRules.WrongDecisionPenalty, so S3's fine is retired). Pure: the
/// draws and the news lines are tested headless.
/// </summary>
public static class Strandings
{
    /// <summary>
    /// The indices of the accepted travellers stranded tonight: one draw of
    /// <paramref name="rng"/> per traveller whose real class is Economy, in
    /// order, stranded when the draw falls under <paramref name="chance"/>
    /// (agency.strandChance); a Premium unit or no transponder (null: the
    /// displaced) draws nothing. Every Economy traveller draws, so a changed
    /// chance never shifts a later traveller's draw.
    /// </summary>
    public static List<int> Roll(IReadOnlyList<TransponderClass?> realClasses, float chance, IRandomSource rng)
    {
        var stranded = new List<int>();
        if (realClasses == null || rng == null)
            return stranded;

        for (int i = 0; i < realClasses.Count; i++)
        {
            if (realClasses[i] != TransponderClass.Economy)
                continue;
            if (rng.Value() < chance)
                stranded.Add(i);
        }
        return stranded;
    }

    /// <summary>
    /// The next morning's stranding lines (S2): one per record, in order,
    /// <paramref name="template"/> (news.stranded) with its {name} and {place}
    /// filled. None for a blank template or no records (null records skipped).
    /// </summary>
    public static List<string> Lines(string template, IReadOnlyList<StrandingRecord> strandings)
    {
        var lines = new List<string>();
        if (string.IsNullOrWhiteSpace(template) || strandings == null)
            return lines;

        foreach (StrandingRecord s in strandings)
            if (s != null)
                lines.Add(Interview.Fill(Interview.Fill(template, Interview.NameToken, s.travellerName), Interview.PlaceToken, s.placeLabel));
        return lines;
    }
}
