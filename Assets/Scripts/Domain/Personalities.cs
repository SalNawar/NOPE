using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// One personality of the cast (world_source.json personalities[]; the
/// personalities spec's PS1, §2): a way of being a person at the desk, never a
/// job, a wealth or a kind. Its lines are the voice rows naming its id; it is
/// never printed (PS4).
/// </summary>
[Serializable]
public sealed class Personality
{
    /// <summary>Stable id ("curt"): the voice rows name it.</summary>
    public string id;

    /// <summary>Its name ("Curt"): the debug panel shows it.</summary>
    public string name;

    /// <summary>Its weight in the draw, the same for every kind (PS2; 0 benches it).</summary>
    public float weight = 1f;

    /// <summary>For authors: the tone in one line (never shown in the game).</summary>
    public string note;

    /// <summary>The chance they refuse the desk's waiver pad, in character (the endings and strandings spec §7.3; Waivers.PadReply): 0 to 1.</summary>
    public float waiverRefusal;

    /// <summary>The chance they crack when the desk asks about a difference the clerk logged while they lie (Confrontations.Outcome; wave 5, lesson 3): 0 to 1; otherwise they double down.</summary>
    public float confess;

    /// <summary>The stranding fate their tilt multiplies (a StrandingFate's name; the spec's §6.2: GameConfigSO.strandingFateTilt); blank: none.</summary>
    public string strandingFate = string.Empty;

    /// <summary>The fate <see cref="strandingFate"/> names, or null (blank or unknown).</summary>
    public StrandingFate? StrandingTilt => Enum.TryParse(strandingFate, out StrandingFate fate) && Enum.IsDefined(typeof(StrandingFate), fate) ? fate : (StrandingFate?)null;
}

/// <summary>
/// The cast's rules (the personalities spec's PS1-PS2): one weighted draw
/// over the cast on the personality stream for a generated traveller
/// (Seeds.ForPersonality; a premade draws nothing, PS3), and the cast's
/// content rules, which Generate World and the content validator share. Pure.
/// </summary>
public static class Personalities
{
    /// <summary>
    /// A generated traveller's personality: one WeightedRandom.Pick over
    /// <paramref name="cast"/> by weight (a weight of 0 is never picked); none,
    /// with no draw, for an empty cast or one with no weight above 0.
    /// </summary>
    public static Personality Pick(IReadOnlyList<Personality> cast, IRandomSource rng) =>
        WeightedRandom.Pick(cast, p => p != null ? p.weight : 0f, rng);

    /// <summary>
    /// Every problem of the cast: an empty entry, a blank or repeated id, a
    /// blank name, a negative weight, a waiver refusal or a confess chance outside 0 to 1, a
    /// stranding fate that names no fate, and, for a cast that is not empty, no
    /// weight above 0. Empty when sound (an empty cast is sound: every
    /// traveller says the defaults).
    /// </summary>
    public static List<string> Problems(IReadOnlyList<Personality> cast)
    {
        var problems = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        bool any = false, positive = false;
        foreach (Personality p in cast ?? Array.Empty<Personality>())
        {
            any = true;
            if (p == null)
            {
                problems.Add("personalities: an entry is empty.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(p.id))
                problems.Add("personalities: a personality has a blank id.");
            else if (!ids.Add(p.id))
                problems.Add($"personalities: '{p.id}' is listed twice.");
            if (string.IsNullOrWhiteSpace(p.name))
                problems.Add($"personalities: '{p.id}' has a blank name.");
            if (p.weight < 0f)
                problems.Add($"personalities: '{p.id}' has a negative weight ({p.weight.ToString("0.###", CultureInfo.InvariantCulture)}).");
            if (!(p.waiverRefusal >= 0f && p.waiverRefusal <= 1f))
                problems.Add($"personalities: '{p.id}' has a waiverRefusal of {p.waiverRefusal.ToString("0.###", CultureInfo.InvariantCulture)}; it is a chance from 0 to 1.");
            if (!(p.confess >= 0f && p.confess <= 1f))
                problems.Add($"personalities: '{p.id}' has a confess chance of {p.confess.ToString("0.###", CultureInfo.InvariantCulture)}; it is a chance from 0 to 1.");
            if (!string.IsNullOrWhiteSpace(p.strandingFate) && p.StrandingTilt == null)
                problems.Add($"personalities: '{p.id}' names the stranding fate '{p.strandingFate}' ({string.Join(", ", Enum.GetNames(typeof(StrandingFate)))}).");
            positive |= p.weight > 0f;
        }

        if (any && !positive)
            problems.Add("personalities: no personality has a weight above 0, so none is ever drawn.");
        return problems;
    }
}
