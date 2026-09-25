using System.Collections.Generic;

/// <summary>
/// Today's world, built once per day by ContentLibrarySO.BuildToday: the
/// places travellers come from and their facts (history applied), so case
/// generation and the reference books share one list and one table.
/// </summary>
public sealed class TodaysWorld
{
    /// <summary>Creates today's world from its places (book order) and their facts.</summary>
    public TodaysWorld(IReadOnlyList<NationEraProfileSO> places, FactTable facts)
    {
        Places = places ?? new List<NationEraProfileSO>();
        Facts = facts ?? new FactTable();
    }

    /// <summary>Today's places in book order: the plan's eras and countries, and at most one Future place (the leader's).</summary>
    public IReadOnlyList<NationEraProfileSO> Places { get; }

    /// <summary>Today's facts with history applied (what papers, books, tells and answers read).</summary>
    public FactTable Facts { get; }
}
