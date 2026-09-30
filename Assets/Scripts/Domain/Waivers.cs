using System;
using System.Collections.Generic;

/// <summary>
/// A traveller's answer to the desk's waiver pad (the endings and strandings
/// spec §7.3; Saleh's Q14 = A): the voice rows name it as their key
/// (interview.voices.waiverPad[].reply, interview.waiverPad.replies[].reply),
/// so its names are content. Runtime only: its numbers are not serialized.
/// </summary>
public enum WaiverPadReply
{
    /// <summary>They sign the blank; the desk files it (a valid signed waiver on file).</summary>
    Signs,

    /// <summary>They refuse, in character (their personality's refusal chance).</summary>
    Refuses,

    /// <summary>Their kind never carries a waiver (a Premium tourist, the displaced): they say they need none.</summary>
    NotNeeded,

    /// <summary>They already carry a signed waiver, and say so.</summary>
    AlreadySigned
}

/// <summary>
/// The desk's waiver pad as authored (world_source.json interview.waiverPad):
/// the entry every traveller of a day whose papers menu holds the Stranding
/// Waiver is offered, the desk's words, and the default replies (a voice with
/// no row of its own says these).
/// </summary>
[Serializable]
public sealed class WaiverPadWording
{
    /// <summary>The entry ("Waiver pad: sign here"), the same for every traveller (rule 3).</summary>
    public string label = string.Empty;

    /// <summary>What the desk says as it slides a blank from the pad across.</summary>
    public LineText prompt = new LineText();

    /// <summary>The default replies, one row per WaiverPadReply (the row's key names it).</summary>
    public List<VoiceLine> replies = new List<VoiceLine>();
}

/// <summary>
/// The Stranding Waiver's rules (the endings and strandings spec §7; Saleh's
/// answers Q14-Q16): a waiver is valid when it is signed and is the one the
/// traveller's account registers (its number and its unit), so a forged or
/// swapped one (L3) never counts; a traveller is waivered when any waiver of
/// theirs is valid (one they carried, or one signed at the desk from the pad);
/// and the traveller's answer to the pad. Pure.
/// </summary>
public static class Waivers
{
    /// <summary>
    /// True when <paramref name="paper"/> (a waiver's fields) is signed
    /// (Directives.IsSigned: a Signature box that is not blank or UNSIGNED)
    /// and its Waiver No. is <paramref name="registeredNo"/> (Values.Match; a
    /// blank registration never matches) and, when it prints a transponder,
    /// that is <paramref name="transponder"/>.
    /// </summary>
    public static bool IsValid(IReadOnlyList<DocumentField> paper, string registeredNo, string transponder)
    {
        if (paper == null || string.IsNullOrWhiteSpace(registeredNo) || !Directives.IsSigned(paper))
            return false;
        bool signature = false, number = false, unit = true;
        foreach (DocumentField f in paper)
        {
            if (f == null)
                continue;
            if (f.category == ClueCategory.Signature)
                signature = true;
            else if (f.category == ClueCategory.WaiverNo)
                number = Values.Match(f.value, registeredNo);
            else if (f.category == ClueCategory.TransponderId)
                unit = Values.Match(f.value, transponder);
        }
        return signature && number && unit;
    }

    /// <summary>True when any of <paramref name="waivers"/> is valid for the account (<see cref="IsValid"/>): a valid signed waiver was on file when the traveller left.</summary>
    public static bool OnFile(IEnumerable<IReadOnlyList<DocumentField>> waivers, string registeredNo, string transponder)
    {
        foreach (IReadOnlyList<DocumentField> paper in waivers ?? Array.Empty<IReadOnlyList<DocumentField>>())
            if (IsValid(paper, registeredNo, transponder))
                return true;
        return false;
    }

    /// <summary>
    /// The traveller's answer to the pad: NotNeeded when their kind carries no
    /// waiver (<paramref name="padFits"/> false: the desk has no waiver of
    /// theirs to fill), AlreadySigned when they carry a signed one, else one
    /// draw of <paramref name="rng"/> (their own stream, Seeds.ForWaiverSign)
    /// against <paramref name="refusal"/> (their personality's chance): Refuses
    /// under it, Signs otherwise. The draw is made whatever the answer, so it
    /// never depends on the papers.
    /// </summary>
    public static WaiverPadReply PadReply(bool padFits, bool carriesSigned, float refusal, IRandomSource rng)
    {
        float u = rng != null ? rng.Value() : 1f;
        if (!padFits)
            return WaiverPadReply.NotNeeded;
        if (carriesSigned)
            return WaiverPadReply.AlreadySigned;
        return u < refusal ? WaiverPadReply.Refuses : WaiverPadReply.Signs;
    }
}
