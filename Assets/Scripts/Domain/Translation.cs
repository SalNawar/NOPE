using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Translation's fixed rules (piece 9; speech only since the redesign's phase
/// 1: every document is filled in English, so only a traveller's speech is
/// ever in a tongue): the Speech translator's upgrade id, which speakers use
/// the claimed place's tongue, and the content problems. Evidence never reads
/// any of this: comparisons stay on the canonical values.
/// </summary>
public static class Translation
{
    /// <summary>
    /// The upgrade id of a pack's Speech translator, "tr_{pack}_spoken" (one
    /// grammar: the generator writes it, TranslationDay reads it). Piece 9's
    /// Papers translators ("tr_{pack}_written") are no longer made; an old save
    /// that owns one keeps a harmless id.
    /// </summary>
    public static string UpgradeId(string packId) => $"tr_{packId}_spoken";

    /// <summary>True for a line spoken in the place's tongue: the traveller's (the desk speaks English).</summary>
    public static bool InTongue(DialogSpeaker speaker) => speaker == DialogSpeaker.Traveller;

    /// <summary>
    /// Content problems (the generator and the validator both report them),
    /// one message per problem naming its ids: null rules; fromDay below 1; a
    /// blank or duplicate tongue id; a blank display name; a script not in
    /// <paramref name="scriptIds"/>; a pack that is neither blank nor a pack
    /// id; a foreign tongue without glyphs; a blank or duplicate pack id or
    /// name; a pack no tongue uses (its translator would buy nothing); a place
    /// (<paramref name="placeTongues"/>: place id and tongue id) whose tongue
    /// is blank or unknown; the key-word rule's problems (KeyWords.Problems).
    /// </summary>
    public static List<string> Problems(TranslationRules rules, IEnumerable<string> scriptIds,
                                        IEnumerable<KeyValuePair<string, string>> placeTongues)
    {
        var problems = new List<string>();
        if (rules == null)
        {
            problems.Add("translation: the section is missing.");
            return problems;
        }

        if (rules.fromDay < 1)
            problems.Add($"translation.fromDay is {rules.fromDay}; it must be at least 1.");

        var scripts = new HashSet<string>(scriptIds ?? Array.Empty<string>());
        var packIds = new HashSet<string>();
        var packNames = new HashSet<string>();
        foreach (TranslatorPack pack in rules.packs ?? new List<TranslatorPack>())
        {
            if (pack == null || string.IsNullOrWhiteSpace(pack.id))
            {
                problems.Add("translation.packs: a pack has a blank id.");
                continue;
            }
            if (!packIds.Add(pack.id))
                problems.Add($"translation.packs: the id '{pack.id}' is listed twice.");
            if (string.IsNullOrWhiteSpace(pack.displayName))
                problems.Add($"translation.packs: pack '{pack.id}' has a blank display name.");
            else if (!packNames.Add(pack.displayName))
                problems.Add($"translation.packs: two packs are named '{pack.displayName}'.");
        }

        var tongueIds = new HashSet<string>();
        var usedPacks = new HashSet<string>();
        foreach (Tongue tongue in rules.tongues ?? new List<Tongue>())
        {
            if (tongue == null || string.IsNullOrWhiteSpace(tongue.id))
            {
                problems.Add("translation.tongues: a tongue has a blank id.");
                continue;
            }
            if (!tongueIds.Add(tongue.id))
                problems.Add($"translation.tongues: the id '{tongue.id}' is listed twice.");
            if (string.IsNullOrWhiteSpace(tongue.displayName))
                problems.Add($"translation.tongues: tongue '{tongue.id}' has a blank display name.");
            if (!scripts.Contains(tongue.script ?? string.Empty))
                problems.Add($"translation.tongues: tongue '{tongue.id}' names the script '{tongue.script}', which translation.scripts does not list.");
            if (tongue.Native)
                continue;

            usedPacks.Add(tongue.pack);
            if (!packIds.Contains(tongue.pack))
                problems.Add($"translation.tongues: tongue '{tongue.id}' names the pack '{tongue.pack}', which translation.packs does not list.");
            if (string.IsNullOrWhiteSpace(tongue.glyphs))
                problems.Add($"translation.tongues: tongue '{tongue.id}' has a pack but no glyphs.");
        }

        foreach (string pack in packIds.Where(p => !usedPacks.Contains(p)))
            problems.Add($"translation.packs: no tongue uses the pack '{pack}', so its translators would buy nothing.");

        foreach (KeyValuePair<string, string> place in placeTongues ?? Array.Empty<KeyValuePair<string, string>>())
        {
            if (string.IsNullOrWhiteSpace(place.Value))
                problems.Add($"Place '{place.Key}' has no tongue.");
            else if (!tongueIds.Contains(place.Value))
                problems.Add($"Place '{place.Key}' names the tongue '{place.Value}', which translation.tongues does not list.");
        }

        problems.AddRange(KeyWords.Problems(rules.keyWords));
        return problems;
    }
}

/// <summary>
/// Today's translation (piece 9 R18), fixed at the start of the office day: a
/// tongue is foreign when it is known, not native and the day is at least
/// fromDay; it is translated when it is foreign and the day-start snapshot
/// owns its pack's Speech translator. Copies what it reads, so later changes
/// to the rules change nothing.
/// </summary>
public sealed class TranslationDay
{
    private readonly int _fromDay;
    private readonly GateSnapshot _dayStart;
    private readonly Dictionary<string, Tongue> _tongues = new Dictionary<string, Tongue>();
    private readonly Dictionary<string, TranslatorPack> _packs = new Dictionary<string, TranslatorPack>();

    /// <summary>The day's translation from the rules (null = nothing foreign) and the day-start snapshot (null = nothing foreign).</summary>
    public TranslationDay(TranslationRules rules, GateSnapshot dayStart)
    {
        _dayStart = dayStart;
        if (rules == null)
            return;

        _fromDay = rules.fromDay;
        foreach (Tongue t in rules.tongues ?? new List<Tongue>())
            if (t != null && !string.IsNullOrWhiteSpace(t.id) && !_tongues.ContainsKey(t.id))
                _tongues.Add(t.id, new Tongue { id = t.id, displayName = t.displayName, script = t.script, pack = t.pack, glyphs = t.glyphs });
        foreach (TranslatorPack p in rules.packs ?? new List<TranslatorPack>())
            if (p != null && !string.IsNullOrWhiteSpace(p.id) && !_packs.ContainsKey(p.id))
                _packs.Add(p.id, new TranslatorPack { id = p.id, displayName = p.displayName });
    }

    /// <summary>The tongue with this id, or null for a blank or unknown id.</summary>
    public Tongue TongueOf(string tongueId) =>
        !string.IsNullOrWhiteSpace(tongueId) && _tongues.TryGetValue(tongueId, out Tongue t) ? t : null;

    /// <summary>The pack that reads a tongue, or null for a native or unknown one.</summary>
    public TranslatorPack PackOf(Tongue tongue) =>
        tongue != null && !tongue.Native && _packs.TryGetValue(tongue.pack, out TranslatorPack p) ? p : null;

    /// <summary>True when the tongue is known, not native, and today is on or after fromDay.</summary>
    public bool Foreign(string tongueId)
    {
        Tongue t = TongueOf(tongueId);
        return t != null && !t.Native && _dayStart != null && _dayStart.Day >= _fromDay;
    }

    /// <summary>True when the tongue is foreign today and the day-start snapshot owns its pack's Speech translator.</summary>
    public bool Translated(string tongueId) =>
        Foreign(tongueId) && _dayStart.HasUpgrade(Translation.UpgradeId(TongueOf(tongueId).pack));
}
