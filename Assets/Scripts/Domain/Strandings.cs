using System;
using System.Collections.Generic;

/// <summary>
/// What the desk saw of a traveller's Stranding Waiver (TC-310; the
/// traveller-types spec's S3): the standing that decides the clerk's fine
/// when the traveller is stranded. Serialized on the case: append only.
/// </summary>
public enum WaiverStanding
{
    /// <summary>No waiver was handed over (a traveller without one, or one never asked for it).</summary>
    None,

    /// <summary>A waiver whose signature row reads "UNSIGNED".</summary>
    Unsigned,

    /// <summary>A waiver number the traveller's Citizen Account never registered (a forged waiver is no waiver).</summary>
    Unregistered,

    /// <summary>A signed waiver whose number the account registered: the clerk is covered.</summary>
    Signed
}

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
/// present's Technology into the destination through the carries (S2), makes
/// the next morning's paper, and costs the clerk a fine when let through
/// without a valid signed waiver (S3). Pure: the draws, the fine rule and the
/// news lines are tested headless. The waiver's standing is read from the
/// papers by the phases that bring the waiver form (8 and 9) through
/// <see cref="Standing"/>; until then every traveller stands at None.
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
    /// What the desk saw of the waiver (the seam for the waiver form): None
    /// when no waiver was <paramref name="handedOver"/>, Unsigned when its
    /// <paramref name="signature"/> row is not a signature (blank or
    /// "UNSIGNED", whatever its case), Unregistered when its
    /// <paramref name="waiverNo"/> is not the account's
    /// <paramref name="registeredNo"/> (Values.Match; an account with no waiver
    /// registers none), else Signed.
    /// </summary>
    public static WaiverStanding Standing(bool handedOver, string signature, string waiverNo, string registeredNo)
    {
        if (!handedOver)
            return WaiverStanding.None;
        if (string.IsNullOrWhiteSpace(signature) || string.Equals(signature.Trim(), UnsignedMark, StringComparison.OrdinalIgnoreCase))
            return WaiverStanding.Unsigned;
        if (string.IsNullOrWhiteSpace(registeredNo) || !Values.Match(waiverNo, registeredNo))
            return WaiverStanding.Unregistered;
        return WaiverStanding.Signed;
    }

    /// <summary>The signature row's value of a waiver nobody signed.</summary>
    public const string UnsignedMark = "UNSIGNED";

    /// <summary>True when a stranding costs the clerk the fine (S3): no valid signed waiver was presented.</summary>
    public static bool Fined(WaiverStanding waiver) => waiver != WaiverStanding.Signed;

    /// <summary>The fine for one stranding: <paramref name="fine"/> cr (agency.strandFine; never below 0) when <see cref="Fined"/>, else 0.</summary>
    public static int Fine(WaiverStanding waiver, int fine) => Fined(waiver) ? Math.Max(0, fine) : 0;

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
