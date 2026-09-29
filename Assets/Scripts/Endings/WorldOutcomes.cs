using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gathers the world a run leaves (WorldOutcome, which WorldSummary writes
/// out) from the run and the content library: every place and the history,
/// the present (ContentLibrarySO.BuildPresent), each attribute's global total,
/// last night's dominant attribute of each place, and the one-shot history
/// rules and timeline triggers that fired. The Title's "world you made"
/// ending and the balance simulation's day-15 report both read it.
/// </summary>
public static class WorldOutcomes
{
    /// <summary>The run's world as it stands; an empty outcome when the run or the library is missing.</summary>
    public static WorldOutcome From(WorldState world, ContentLibrarySO lib)
    {
        var outcome = new WorldOutcome();
        if (world == null || lib == null)
            return outcome;

        List<AttributeSO> attributes = lib.Attributes.Where(a => a != null).ToList();
        outcome.Places = SiteWorldBuilder.Places(lib);
        outcome.History = world.history;
        outcome.NationName = id => lib.GetNationById(id) is NationSO n && !string.IsNullOrWhiteSpace(n.displayName) ? n.displayName : id;
        outcome.Present = lib.BuildPresent(world.history);
        outcome.Attributes = attributes.Select(a => new KeyValuePair<string, float>(a.displayName, world.timeline.GetScore(TimelineKeys.GlobalAttr(a)))).ToList();
        outcome.Dominant = attributes.SelectMany(a => lib.Profiles
                                         .Where(p => p != null && world.timeline.dominantKeys.Contains(TimelineKeys.Dominance(p, a)))
                                         .Select(p => new KeyValuePair<string, string>(a.displayName, p.displayName)))
                                     .ToList();
        outcome.Events = lib.Triggers.Where(t => t != null && t.oneShot && world.HasFlag(t.FiredFlag)).Select(t => t.displayName).ToList();
        return outcome;
    }
}
