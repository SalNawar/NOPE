// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// Special “legendary” visitor that can appear with a chance.
/// </summary>
[CreateAssetMenu(fileName = "Legendary_", menuName = "TimeDesk/Legendary", order = 12)]
public sealed class LegendarySO : ScriptableObject
{
    /// <summary>Display name for the legendary visitor.</summary>
    public string displayName = "Legendary Visitor";

    /// <summary>Earliest day this legendary can appear.</summary>
    [Min(1)]
    public int minDay = 1;

    /// <summary>Latest day this legendary can appear.</summary>
    [Min(1)]
    public int maxDay = 999;

    /// <summary>The correct/true era for this legendary case.</summary>
    public EraSO trueEra;

    /// <summary>If set, forces this blueprint when the legendary appears.</summary>
    public CaseBlueprintSO blueprintOverride;

    [Header("Timeline")]
    /// <summary>Archetype for this legendary (tags + default impacts).</summary>
    public ArchetypeSO archetype;

    /// <summary>Destination nation for timeline impacts (e.g., Tesla -> Germany).</summary>
    public NationSO nation;

    /// <summary>Authored timeline impacts (added on top of archetype defaults).</summary>
    public System.Collections.Generic.List<TimelineImpact> authoredImpacts = new();
}
