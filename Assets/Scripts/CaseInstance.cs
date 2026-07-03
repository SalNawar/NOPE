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

    /// <summary>Display name for the visitor (given name + role suffix).</summary>
    public string visitorDisplayName;

    /// <summary>The visitor's true given name (citizen-records lookup key).</summary>
    public string visitorGivenName;

    /// <summary>The visitor's TRUE date of birth (what the agency has on file).</summary>
    public string trueBirthDate;

    /// <summary>Short intro line for the case.</summary>
    public string introLine;

    // -----------------------------
    // Investigation (accept/deny)
    // -----------------------------

    /// <summary>Nation the visitor CLAIMS to be traveling to (shown to player).</summary>
    public NationSO claimedNation;

    /// <summary>Era the visitor CLAIMS to be traveling to (shown to player).</summary>
    public EraSO claimedEra;

    /// <summary>True if any document field is an anachronism for the claim.</summary>
    public bool isForged;

    /// <summary>True if the claimed destination is permitted by today's rules.</summary>
    public bool claimAllowedByRules = true;

    /// <summary>The visitor's stated travel claim line, for the UI banner.</summary>
    public string claimLine;

    /// <summary>
    /// The correct decision: accept only a genuine traveler whose destination
    /// is permitted today. Deny if forged OR the claim breaks a daily rule.
    /// </summary>
    public bool ShouldAccept => !isForged && claimAllowedByRules;

    /// <summary>Runtime documents built from templates.</summary>
    public readonly List<DocumentInstance> documents = new();

    /// <summary>All clue assets used by this case.</summary>
    public readonly List<ClueSO> usedClues = new();
}

/// <summary>
/// A runtime document assembled from a template, containing clue lines.
/// </summary>
public sealed class DocumentInstance
{
    /// <summary>Template this document was built from.</summary>
    public DocumentTemplateSO template;

    /// <summary>Pre-rendered text for quick prototype UI display.</summary>
    public string renderedText;

    /// <summary>Raw clue references inside this document.</summary>
    public readonly List<ClueSO> cluesInDoc = new();

    /// <summary>Structured, checkable fields (investigation feature).</summary>
    public readonly List<DocumentField> fields = new();

    /// <summary>Number of pages this document spans (1-based count).</summary>
    public int PageCount
    {
        get
        {
            int max = 1;
            foreach (DocumentField f in fields)
                if (f != null && f.page + 1 > max)
                    max = f.page + 1;
            return max;
        }
    }
}
