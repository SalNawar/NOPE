using System.Collections.Generic;

/// <summary>
/// A generated, playable case for the current shift.
/// Created at runtime from data assets (blueprints, clue library, etc.).
/// </summary>
public sealed class CaseInstance
{
    /// <summary>0-based case index used internally.</summary>
    public int caseIndex;

    /// <summary>The correct destination era.</summary>
    public EraSO trueEra;

    /// <summary>True if this case was generated as a legendary encounter.</summary>
    public bool isLegendary;

    /// <summary>Reference to the legendary source asset (if legendary).</summary>
    public LegendarySO legendarySource;

    /// <summary>Visitor archetype (drives default timeline impacts + tags).</summary>
    public ArchetypeSO archetype;

    /// <summary>Destination nation within the era (timeline impacts land here).</summary>
    public NationSO nation;

    /// <summary>Authored impact overrides from the blueprint/legendary (may be empty).</summary>
    public readonly List<TimelineImpact> authoredImpacts = new();

    /// <summary>Display name for the visitor.</summary>
    public string visitorDisplayName;

    /// <summary>Short intro line for the case.</summary>
    public string introLine;

    /// <summary>Runtime documents built from templates.</summary>
    public readonly List<DocumentInstance> documents = new();

    /// <summary>All clue assets used by this case.</summary>
    public readonly List<ClueSO> usedClues = new();
}

/// <summary>
/// A runtime document assembled from a template, containing clue