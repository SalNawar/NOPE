using System.Collections.Generic;

/// <summary>
/// Today's world, built once per day by ContentLibrarySO.BuildToday: the
/// places travellers come from and their facts (history applied), with the
/// present's row (traveller types H1), so case generation and the reference
/// books share one list and one table; and the present itself, where 2150
/// citizens come from.
/// </summary>
public sealed class TodaysWorld
{
    /// <summary>Creates today's world from its places (book order), their facts, the present (null when the content has none) and the day's portals (null: none).</summary>
    public TodaysWorld(IReadOnlyList<NationEraProfileSO> places, FactTable facts, PresentPlace present, PortalDay portals)
    {
        Places = places ?? new List<NationEraProfileSO>();
        Facts = facts ?? new FactTable();
        Present = present;
        Portals = portals ?? PortalDay.None;
    }

    /// <summary>The day's portals, fixed at the day's start (the portals spec v3 RT3): the board, the rings, the Portals app and the departures read it; case generation never does.</summary>
    public PortalDay Portals { get; }

    /// <summary>Today's places (the destinations) in book order: the plan's eras and countries, never a Future place (History.IsDestination).</summary>
    public IReadOnlyList<NationEraProfileSO> Places { get; }

    /// <summary>Today's facts with history applied, the present's row last (what papers, books, tells and answers read).</summary>
    public FactTable Facts { get; }

    /// <summary>The present (Present.Choose): the leader's Future place, or the neutral present; null when the content has none.</summary>
    public PresentPlace Present { get; }
}
