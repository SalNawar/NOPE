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
    /// <summary>Creates today's world from its places (book order), their facts and the present (null when the content has none).</summary>
    public TodaysWorld(IReadOnlyList<NationEraProfileSO> places, FactTable facts, PresentPlace present)
    {
        Places = places ?? new List<NationEraProfileSO>();
        Facts = facts ?? new FactTable();
        Present = present;
    }

    /// <summary>Today's places in book order: the plan's eras and countries, and at most one Future place (the leader's).</summary>
    public IReadOnlyList<NationEraProfileSO> Places { get; }

    /// <summary>Today's facts with history applied, the present's row last (what papers, books, tells and answers read).</summary>
    public FactTable Facts { get; }

    /// <summary>The present (Present.Choose): the leader's Future place, or the neutral present; null when the content has none.</summary>
    public PresentPlace Present { get; }
}
