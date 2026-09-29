using System;
using System.Collections.Generic;

/// <summary>
/// Home's content block (the Home upgrades spec §5, HU3; written by Generate
/// World from world_source.json "home"): which house upgrade is the radio,
/// and the radio's lines, one of which the sleep panel plays each night the
/// house owns it. The house upgrades themselves are generated upgrade and
/// effect assets in the library's lists.
/// </summary>
[Serializable]
public sealed class HomeContent
{
    /// <summary>The id of the house upgrade whose ownership plays a radio line ("" for none).</summary>
    public string radioUpgrade = string.Empty;

    /// <summary>The radio's lines, in the order the nights play them.</summary>
    public List<string> radio = new();

    /// <summary>The radio's line on <paramref name="day"/>: the lines in order, day 1 the first, round again after the last (no random draw); "" without lines.</summary>
    public string RadioLine(int day)
    {
        if (radio == null || radio.Count == 0)
            return string.Empty;
        int i = (day - 1) % radio.Count;
        return radio[i < 0 ? i + radio.Count : i] ?? string.Empty;
    }

    /// <summary>What Generate World and the validator refuse: a blank radio line, and a radio upgrade that is none of <paramref name="houseUpgradeIds"/>. Empty when sound.</summary>
    public List<string> Problems(IEnumerable<string> houseUpgradeIds)
    {
        var problems = new List<string>();
        for (int i = 0; i < (radio?.Count ?? 0); i++)
            if (string.IsNullOrWhiteSpace(radio[i]))
                problems.Add($"home.radio[{i}] is blank: each is a line the sleep panel plays.");
        if (!string.IsNullOrEmpty(radioUpgrade) && !new HashSet<string>(houseUpgradeIds ?? Array.Empty<string>()).Contains(radioUpgrade))
            problems.Add($"home.radioUpgrade '{radioUpgrade}' is no house upgrade's id (home.upgrades).");
        return problems;
    }
}
