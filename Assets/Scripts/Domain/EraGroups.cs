using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>An era as the group rules read it: its id, the era it is a second moment of (blank: none) and whether it is the Future.</summary>
public readonly struct EraEntry
{
    /// <summary>An era <paramref name="id"/> grouped under <paramref name="group"/> (blank: its own group).</summary>
    public EraEntry(string id, string group, bool future)
    {
        Id = id;
        Group = group;
        Future = future;
    }

    /// <summary>The era's id.</summary>
    public string Id { get; }

    /// <summary>The era it belongs to (world_source.json eras[].group; blank: none, a main era).</summary>
    public string Group { get; }

    /// <summary>True for the Future.</summary>
    public bool Future { get; }
}

/// <summary>
/// Second moments of an era (Track E2, Saleh 2026-10-05: "add places for
/// them"): an era may name a main era as its group (eras[].group), so a
/// nation holds a second place of that era (Hellenistic Pella beside
/// Periclean Athens, both Ancient) under its own era id, which keys its facts,
/// art and routes as any place. The player reads the group: its name in the
/// place's label (the content gives the second moment the main era's name),
/// one Chronopedia column and one Lineage Archive chip per group. Pure.
/// </summary>
public static class EraGroups
{
    /// <summary>The era's group id: its group, or the era itself when it names none.</summary>
    public static string GroupOf(string id, string group) => string.IsNullOrWhiteSpace(group) ? id : group;

    /// <summary>
    /// What Generate World refuses and the validator reports: a group names
    /// another era of the list, a main era (one with no group of its own),
    /// never the Future; the Future names no group.
    /// </summary>
    public static List<string> Problems(IReadOnlyList<EraEntry> eras)
    {
        var problems = new List<string>();
        List<EraEntry> list = (eras ?? Array.Empty<EraEntry>()).ToList();
        foreach (EraEntry e in list.Where(e => !string.IsNullOrWhiteSpace(e.Group)))
        {
            string owner = $"Era '{e.Id}'";
            if (e.Future)
            {
                problems.Add($"{owner} is the Future and names the group '{e.Group}'; the Future has one place per nation, no second moment.");
                continue;
            }
            if (e.Group == e.Id)
            {
                problems.Add($"{owner} names itself as its group; leave the group blank for a main era.");
                continue;
            }
            EraEntry? main = list.Where(x => x.Id == e.Group).Select(x => (EraEntry?)x).FirstOrDefault();
            if (main == null)
                problems.Add($"{owner} names the group '{e.Group}', which is not an era.");
            else if (main.Value.Future)
                problems.Add($"{owner} names the Future as its group; a second moment belongs to a past era.");
            else if (!string.IsNullOrWhiteSpace(main.Value.Group))
                problems.Add($"{owner} names the group '{e.Group}', which is itself a second moment (of '{main.Value.Group}'); name the main era.");
        }
        return problems;
    }

    /// <summary>
    /// Whether a place is in a day's world (ContentLibrarySO.TodaysProfiles):
    /// a past place of a weighted era and an allowed nation, or the place of
    /// a premade the day forces or pools (an authored case brings its place's
    /// facts and book rows with it, Track E2: a famous traveller's second
    /// moment stands only on the days that send them). Never the Future.
    /// </summary>
    public static bool InTodaysWorld(bool isFuture, bool eraWeighted, bool nationAllowed, bool premadePlace) =>
        !isFuture && (premadePlace || (eraWeighted && nationAllowed));
}
